using System;
using Mono.VisualStudio.TextTemplating;
using Mono.VisualStudio.TextTemplating.VSHost;

namespace Mono.TextTemplating
{
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Threading;

	public partial class TemplatingEngine
		: IProcessTextTemplatingEngine
	{
		public IProcessTransformationRun PrepareTransformationRun (string content, ITextTemplatingEngineHost host, IProcessTransformationRunFactory runFactory, bool debugging = false)
		{
			if (content == null) {
				throw new ArgumentNullException (nameof(content));
			}
			if (host == null) {
				throw new ArgumentNullException (nameof (host));
			}
			if (runFactory == null) {
				throw new ArgumentNullException (nameof (runFactory));
			}

			if (host is ITextTemplatingSessionHost sessionHost) {
				if (sessionHost.Session == null) {
					sessionHost.Session = sessionHost.CreateSession ();
				}
			}

			ParsedTemplate pt = ParsedTemplate.FromText (content, host);

			IProcessTransformationRun run = null;

			try {
				if (pt.Errors.HasErrors) {
					return null;
				}
				TemplateSettings settings = GetSettings (host, pt);

				settings.Debug = debugging;

				run = CompileAndPrepareRun (pt, content, host, runFactory, settings);
			} catch(Exception ex) {
				if (IsCriticalException(ex)) {
					throw;
				}
				pt.LogError (string.Format(CultureInfo.CurrentCulture, VsTemplatingErrorResources.ExceptionProcessingTemplate, ex), new Location (host.TemplateFile, -1, -1));
			}
			finally {
				host.LogErrors (pt.Errors);
			}

			return run;
		}

		protected virtual IProcessTransformationRun CompileAndPrepareRun (ParsedTemplate pt, string content, ITextTemplatingEngineHost host, IProcessTransformationRunFactory runFactory, TemplateSettings settings) 
		{
			TransformationRunner runner = null;
			bool success = false;

			if (pt == null) {
				throw new ArgumentNullException (nameof (pt));
			}

			if (host == null) {
				throw new ArgumentNullException (nameof (host));
			}

			if (runFactory == null) {
				throw new ArgumentNullException (nameof (runFactory));
			}

			if (settings == null) {
				throw new ArgumentNullException (nameof (settings));
			}

			Assembly ResolveReferencedAssemblies (object sender, ResolveEventArgs args)
			{
				AssemblyName asmName = new AssemblyName (args.Name);
				foreach (var asmFile in settings.Assemblies) {
					if (asmName.Name == Path.GetFileNameWithoutExtension (asmFile))
						return Assembly.LoadFrom (asmFile);
				}

				var path = host.ResolveAssemblyReference (asmName.Name);

				if (File.Exists (path)) {
					return Assembly.LoadFrom (path);
				}

				return null;
			}

			try {
				try {
					if (runFactory.CreateTransformationRun (typeof (TransformationRunner), pt, new ResolveEventHandler(ResolveReferencedAssemblies)) is TransformationRunner theRunner) {
						runner = theRunner;
					}
				}
				catch (Exception ex) {
					if (IsCriticalException (ex)) {
						throw;
					}
				}
				if (runner != null && !runner.Errors.HasErrors) {
					ProcessReferences (host, pt, settings);
					if (!pt.Errors.HasErrors) {
						//with app domains dissappearing we may not be able to pre load anything
						//runner.PreLoadAssemblies (settings.Assemblies);
						try {
							success = runner.PrepareTransformation (pt, content, settings.HostSpecific ? host : null, settings);
						}
						catch (SerializationException) {
							pt.LogError (VsTemplatingErrorResources.SessionHostMarshalError, new Location (host.TemplateFile, -1, -1));
							throw;
						}
					}
				}
			}
			catch(Exception ex) {
				if (IsCriticalException (ex)) {
					throw;
				}
				pt.LogError (ex.ToString (), new Location (host.TemplateFile, -1, -1));
			}
			finally {
				if (runner != null) {
					pt.Errors.AddRange (runner.Errors);
					runner.ClearErrors ();
				}
			}

			return success ? runner : null;
		}

		public static bool IsCriticalException(Exception e)
		{
			return ((e is StackOverflowException) || ((e is OutOfMemoryException) || ((e is ThreadAbortException) || ((e.InnerException != null) && IsCriticalException (e.InnerException)))));
		}
	}
}
