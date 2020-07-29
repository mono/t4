using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating
{
	public class TransformationRunner :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IDebugTransformationRun
	{
		static HashSet<string> shadowCopyPaths = null;
		static object shadowCopySync = new object ();
		CompiledTemplate compiledTemplate;
		TemplateSettings settings;
		ITextTemplatingSession session;
		ITextTemplatingEngineHost host;

		public CompilerErrorCollection Errors { get; private set; }

		public string PerformTransformation ()
		{
			string errorOutput = Resources.ErrorOutput;

			if (compiledTemplate == null) {
				LogError (Resources.ErrorInitializingTransformationObject, false);

				return errorOutput;
			}

			object transform = null;

			try {
				transform = CreateTextTransformation (settings, host, compiledTemplate.Assembly, session);

				return compiledTemplate.Process (transform);
			}
			catch (Exception ex) {
				if (DebugTextTemplateEngine.IsCriticalException(ex)) {
					throw;
				}
				LogError (ex.ToString (), false);
			}
			finally {
				if (transform is IDisposable disposable) {
					disposable.Dispose ();
				}
				compiledTemplate?.Dispose ();
				compiledTemplate = null;
				host = null;
			}

			return errorOutput;
		}

		PropertyInfo GetDerivedProperty (Type transformType, string propertyName)
		{
			while(transformType != typeof(object) && transformType != null) {
				PropertyInfo property = transformType.GetProperty (propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				if (property != null) {
					return property;
				}
				transformType = transformType.BaseType;
			}
			return null;
		}

		protected virtual object CreateTextTransformation(TemplateSettings settings, ITextTemplatingEngineHost host, Assembly assembly, ITextTemplatingSession session) {
			object result = null;
			object success = null;
			try {
				Type type;

				result = assembly.CreateInstance (settings.GetFullName ());

				if (result != null) {
					type = result.GetType ();

					if (settings.HostPropertyOnBase) {
						try {
							PropertyInfo property = type.GetProperty ("Host");

							if (property != null) {
								property?.SetValue (result, host, null);
							}
							else {
								LogError (string.Format(CultureInfo.CurrentCulture, Resources.HostPropertyNotFound, settings.HostType.Name), false);
							}	
						}
						catch(Exception hostException) {
							if (DebugTextTemplateEngine.IsCriticalException(hostException)) {
								throw;
							}
							LogError (string.Format (CultureInfo.CurrentCulture, Resources.ExceptionSettingHost, settings.GetFullName ()), false);
						}
					}

					try {
						PropertyInfo property = GetDerivedProperty (type, nameof (TextTransformation.Session));

						property?.SetValue (result, session, null);
					}
					catch(Exception sessionException) {
						if (DebugTextTemplateEngine.IsCriticalException (sessionException)) {
							throw;
						}
						LogError (string.Format (CultureInfo.CurrentCulture, Resources.ExceptionSettingSession, settings.GetFullName ()), false);
					}
					success = result;

				} else {
					LogError (Resources.ExceptionInstantiatingTransformationObject, false);
				}
			}
			catch(Exception instantiatingException) {
				if (DebugTextTemplateEngine.IsCriticalException (instantiatingException)) {
					throw;
				}
				LogError (Resources.ExceptionInstantiatingTransformationObject + string.Format(CultureInfo.CurrentCulture, Resources.Exception, instantiatingException), false);
				success = null;
			}
			return success;
		}

		internal bool PrepareTransformation (ITextTemplatingSession session, ParsedTemplate template, string content, ITextTemplatingEngineHost host, TemplateSettings settings)
		{
			this.session = session ?? throw new ArgumentNullException (nameof (session));
			this.host = host ?? throw new ArgumentNullException (nameof (host));
			this.settings = settings ?? throw new ArgumentNullException (nameof (settings));

			try {
				this.settings.Assemblies.Add (base.GetType ().Assembly.Location);
				this.settings.Assemblies.Add (typeof (ITextTemplatingEngineHost).Assembly.Location);
				this.compiledTemplate = LocateAssembly (session, template, content);
			}
			catch(Exception ex) {
				if (DebugTextTemplateEngine.IsCriticalException (ex)) {
					throw;
				}
				LogError (string.Format (CultureInfo.CurrentCulture, Resources.Exception, ex), false);
			}
			return this.compiledTemplate != null;
		}

		CompiledTemplate LocateAssembly (ITextTemplatingSession session, ParsedTemplate template, string content)
		{
			CompiledTemplate compiledTemplate = null;

			if (session.CachedTemplates) {
				compiledTemplate = CompiledTemplateCache.Find (session.ClassFullName);
			}
			if (compiledTemplate == null) {
				compiledTemplate = Compile (template, content);
				if (session.CachedTemplates && compiledTemplate != null) {
					CompiledTemplateCache.Insert (session.ClassFullName, compiledTemplate);
				}
			}
			return compiledTemplate;
		}

		CompiledTemplate Compile (ParsedTemplate template, string content)
		{
			CompiledTemplate compiledTemplate = null;

			if (host is ITextTemplatingComponents Component &&
				Component.Engine is DebugTextTemplateEngine engine) {
				compiledTemplate = engine.CompileTemplate (template, content, host, settings);
			}

			return compiledTemplate;
		}

		internal void PreLoadAssemblies (IEnumerable<string> assemblies)
		{
			try {
				//TODO:: investigate preloading assemblies with the AssemblyLoadContext
			}catch(Exception ex) {
				if (DebugTextTemplateEngine.IsCriticalException (ex)) {
					throw;
				}
			}
		}

		[Obsolete]
		void LoadExplicitAssemblyReferences (IEnumerable<string> references)
		{
			references = (from referenceAssembly in references
						  where !string.IsNullOrEmpty (referenceAssembly) && File.Exists (referenceAssembly)
						  select referenceAssembly).ToList ();

			List<string> source = new List<string> ();

			if (AppDomain.CurrentDomain.ShadowCopyFiles) {
				foreach(string reference in references) {
					string referenceDir = Path.GetDirectoryName (reference);
					if (string.IsNullOrEmpty (referenceDir)) {
						string currentDir = Directory.GetCurrentDirectory ();
						if (File.Exists(Path.Combine(currentDir, reference))) {
							referenceDir = currentDir;
						}
					}
					source.Add (referenceDir);
				}
				EnsureShadowCopyPaths (source.Distinct (StringComparer.OrdinalIgnoreCase));
			}
		}

		[Obsolete]
		void EnsureShadowCopyPaths (IEnumerable<string> paths)
		{
			if (AppDomain.CurrentDomain.ShadowCopyFiles) {
				string path = string.Empty;
				object shadowCopySync = TransformationRunner.shadowCopySync;
				lock (shadowCopySync) {
					if (shadowCopyPaths == null) {
						shadowCopyPaths = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
					}
					foreach (string str2 in paths) {
						if (!shadowCopyPaths.Contains (str2)) {
							shadowCopyPaths.Add (str2);
						}
					}
					path = string.Join (";", shadowCopyPaths.ToArray<string> ());
				}
				AppDomain.CurrentDomain.SetShadowCopyPath (path);
			}
		}

		protected Assembly AttemptAssemblyLoad(string assemblyName)
		{
			try {
				return Assembly.LoadFrom (assemblyName);
			}
			catch(Exception ex) {
				LogError (string.Format (CultureInfo.CurrentCulture, Resources.AssemblyLoadError, assemblyName) + string.Format (CultureInfo.CurrentCulture, Resources.Exception, ex), false);
				return null;
			}
		}

		public void ClearErrors()
		{
			Errors.Clear ();
		}

		protected void LogError(string message, bool isWarning)
		{
			CompilerError error = new CompilerError () {
				ErrorText = message,
				IsWarning = isWarning
			};

			Errors.Add (error);
		}

		protected void LogError(string message, bool isWarning, string filename, int line, int column)
		{
			CompilerError error = new CompilerError () {
				ErrorText = message,
				IsWarning = isWarning,
				FileName = filename,
				Line = line,
				Column = column
			};

			Errors.Add (error);
		}
	}
}
