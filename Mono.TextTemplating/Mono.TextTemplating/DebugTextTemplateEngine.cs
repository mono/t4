using System;
using Mono.VisualStudio.TextTemplating;
using Mono.VisualStudio.TextTemplating.VHost;

namespace Mono.TextTemplating
{
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Threading;

	public partial class TemplatingEngine
		: IDebugTextTemplatingEngine
	{
		public IDebugTransformationRun PrepareTransformationRun (string content, ITextTemplatingEngineHost host, IDebugTransformationRunFactory runFactory)
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

			IDebugTransformationRun run = null;

			try {
				if (pt.Errors.HasErrors) {
					return null;
				}
				TemplateSettings settings = GetSettings (host, pt);

				settings.Debug = true;

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

		protected virtual IDebugTransformationRun CompileAndPrepareRun (ParsedTemplate template, string content, ITextTemplatingEngineHost host, IDebugTransformationRunFactory runFactory, TemplateSettings settings) 
		{
			TransformationRunner runner = null;
			bool success = false;

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
					if (runFactory.CreateTransformationRun (typeof (TransformationRunner), template, new ResolveEventHandler(ResolveReferencedAssemblies)) is TransformationRunner theRunner) {
						runner = theRunner;
					}
				}
				catch (Exception ex) {
					if (IsCriticalException (ex)) {
						throw;
					}
				}
				if (runner != null && !runner.Errors.HasErrors) {
					ProcessReferences (host, template, settings);
					if (!template.Errors.HasErrors) {
						runner.PreLoadAssemblies (settings.Assemblies);

						try {
							success = runner.PrepareTransformation (template, content, settings.HostSpecific ? host : null, settings);
						}
						catch (SerializationException) {
							template.LogError (VsTemplatingErrorResources.SessionHostMarshalError, new Location (host.TemplateFile, -1, -1));
							throw;
						}
					}
				}
			}
			catch(Exception ex) {
				if (IsCriticalException (ex)) {
					throw;
				}
				template.LogError (ex.ToString (), new Location (host.TemplateFile, -1, -1));
			}
			finally {
				if (runner != null) {
					template.Errors.AddRange (runner.Errors);
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
