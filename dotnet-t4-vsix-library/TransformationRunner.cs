using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating
{
	public class TransformationRunner :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IDebugTransformationRun
	{
		private Assembly assembly;
		private TemplateSettings settings;
		private ITextTemplatingSession session;
		private ITextTemplatingEngineHost host;
		private static Regex linePattern;


		public CompilerErrorCollection Errors { get; private set; }

		public string PerformTransformation ()
		{
			string errorOutput = Resources.ErrorOutput;

			if (assembly == null) {
				LogError (Resources.ErrorInitializingTransformationObject, false);

				return errorOutput;
			}

			object result = null;

			try {
				result = CreateTextTransformation (settings, host, assembly, session);

				throw new NotImplementedException ();
			}
			finally {
				if (result is IDisposable disposable) {
					disposable.Dispose ();
				}
				assembly = null;
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
							if (TextDebugTemplateEngine.IsCriticalException(hostException)) {
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
						if (TextDebugTemplateEngine.IsCriticalException (sessionException)) {
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
				if (TextDebugTemplateEngine.IsCriticalException (instantiatingException)) {
					throw;
				}
				LogError (Resources.ExceptionInstantiatingTransformationObject + string.Format(CultureInfo.CurrentCulture, Resources.Exception, instantiatingException), false);
				success = null;
			}
			return success;
		}

		internal bool PrepareTransformation (ITextTemplatingSession session, ParsedTemplate template, ITextTemplatingEngineHost host)
		{
			throw new NotImplementedException ();
		}

		internal void PreLoadAssemblies (string[] assemblies)
		{
			throw new NotImplementedException ();
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
