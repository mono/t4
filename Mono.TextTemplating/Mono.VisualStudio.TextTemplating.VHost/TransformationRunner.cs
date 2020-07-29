using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating.VHost
{
	public class TransformationRunner :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IDebugTransformationRun
	{
		CompiledTemplate compiledTemplate;
		TemplateSettings settings;
		ITextTemplatingEngineHost host;

		public CompilerErrorCollection Errors { get; private set; }

		public virtual string PerformTransformation ()
		{
			string errorOutput = VsTemplatingErrorResources.ErrorOutput;

			if (compiledTemplate == null) {
				LogError (VsTemplatingErrorResources.ErrorInitializingTransformationObject, false);

				return errorOutput;
			}

			object transform = null;

			try {
				transform = CreateTextTransformation (settings, host, compiledTemplate.Assembly);

				return compiledTemplate.Process (transform);
			}
			catch (Exception ex) {
				if (TemplatingEngine.IsCriticalException(ex)) {
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

		protected virtual object CreateTextTransformation(TemplateSettings settings, ITextTemplatingEngineHost host, Assembly assembly) {
			object success = null;
			try {
				Type type;

				var result = assembly.CreateInstance (settings.GetFullName ());

				if (result != null) {
					type = result.GetType ();

					if (settings.HostPropertyOnBase) {
						try {
							PropertyInfo property = type.GetProperty ("Host");

							if (property != null) {
								property?.SetValue (result, host, null);
							}
							else {
								LogError (string.Format(CultureInfo.CurrentCulture, VsTemplatingErrorResources.HostPropertyNotFound, settings.HostType.Name), false);
							}	
						}
						catch(Exception hostException) {
							if (TemplatingEngine.IsCriticalException(hostException)) {
								throw;
							}
							LogError (string.Format (CultureInfo.CurrentCulture, VsTemplatingErrorResources.ExceptionSettingHost, settings.GetFullName ()), false);
						}
					}

					try {
						if (host is ITextTemplatingSessionHost sessionHost &&
							sessionHost.Session != null) {
							PropertyInfo property = GetDerivedProperty (type, nameof (TextTransformation.Session));

							property?.SetValue (result, sessionHost.Session, null);
						}
						else {
							throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, VsTemplatingErrorResources.SessionHostSessionNotInitialized, settings.GetFullName()));
						}
					}
					catch (Exception sessionException) {
						if (TemplatingEngine.IsCriticalException (sessionException)) {
							throw;
						}
						LogError (string.Format (CultureInfo.CurrentCulture, VsTemplatingErrorResources.ExceptionSettingSession, sessionException), false);
					}
					success = result;

				} else {
					LogError (VsTemplatingErrorResources.ExceptionInstantiatingTransformationObject, false);
				}
			}
			catch(Exception instantiatingException) {
				if (TemplatingEngine.IsCriticalException (instantiatingException)) {
					throw;
				}
				LogError (VsTemplatingErrorResources.ExceptionInstantiatingTransformationObject + string.Format(CultureInfo.CurrentCulture, VsTemplatingErrorResources.Exception, instantiatingException), false);
				success = null;
			}
			return success;
		}

		public virtual bool PrepareTransformation (ParsedTemplate template, string content, ITextTemplatingEngineHost host, TemplateSettings settings)
		{
			this.host = host ?? throw new ArgumentNullException (nameof (host));
			this.settings = settings ?? throw new ArgumentNullException (nameof (settings));

			try {
				this.settings.Assemblies.Add (base.GetType ().Assembly.Location);
				this.settings.Assemblies.Add (typeof (ITextTemplatingEngineHost).Assembly.Location);
				this.compiledTemplate = LocateAssembly (template, content);
			}
			catch(Exception ex) {
				if (TemplatingEngine.IsCriticalException (ex)) {
					throw;
				}
				LogError (string.Format (CultureInfo.CurrentCulture, VsTemplatingErrorResources.Exception, ex), false);
			}
			return this.compiledTemplate != null;
		}

		CompiledTemplate LocateAssembly (ParsedTemplate template, string content)
		{
			CompiledTemplate compiledTemplate = null;

			if (settings.CachedTemplates) {
				compiledTemplate = CompiledTemplateCache.Find (settings.GetFullName ());
			}
			if (compiledTemplate == null) {
				compiledTemplate = Compile (template, content);
				if (settings.CachedTemplates && compiledTemplate != null) {
					CompiledTemplateCache.Insert (settings.GetFullName (), compiledTemplate);
				}
			}
			return compiledTemplate;
		}

		CompiledTemplate Compile (ParsedTemplate template, string content)
		{
			CompiledTemplate compiledTemplate = null;

			if (host is ITextTemplatingComponents Component &&
				Component.Engine is TemplatingEngine engine) {
				compiledTemplate = engine.CompileTemplate (template, content, host, settings);
				// do we want to dispose the appdomain resolver in compiled template in favor of the transformation runner?
				//compiledTemplate?.Dispose ();
			}

			return compiledTemplate;
		}

		public virtual void PreLoadAssemblies (IEnumerable<string> assemblies)
		{
			try {
				//TODO:: investigate preloading assemblies with the AssemblyLoadContext
			}catch(Exception ex) {
				if (TemplatingEngine.IsCriticalException (ex)) {
					throw;
				}
			}
		}

		protected Assembly AttemptAssemblyLoad(AssemblyName assembly)
		{
			try {
				return Assembly.LoadFrom (assembly.CodeBase);
			}
			catch(Exception ex) {
				LogError (string.Format (CultureInfo.CurrentCulture, VsTemplatingErrorResources.AssemblyLoadError, assembly.Name, ex), false);
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
