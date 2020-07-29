using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.VisualStudio.TextTemplating
{
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Loader;
	using System.Runtime.Serialization;
	using System.Threading;
	using Microsoft.Extensions.DependencyModel;
	using Microsoft.Extensions.DependencyModel.Resolution;
	using Mono.TextTemplating;

	public class TextDebugTemplateEngine
		: TemplatingEngine
		, IDebugTextTemplatingEngine
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

			ITextTemplatingSession session = new TextTemplatingSession ();
			ParsedTemplate pt = ParsedTemplate.FromText (content, host);

			InitializeSessionWithHostData (host, session);

			IDebugTransformationRun run = null;
			try {
				if (pt.Errors.HasErrors) {
					return null;
				}
				TemplateSettings settings = GetSettings (host, pt);

				settings.Debug = true;

				settings.ApplyTo (session);

				run = CompileAndPrepareRun (pt, host, session, runFactory, settings);
			} catch(Exception ex) {
				if (IsCriticalException(ex)) {
					throw;
				}
				pt.LogError (string.Format(CultureInfo.CurrentCulture, Resources.ExceptionProcessingTemplate, ex), new Location (session.TemplateFile, -1, -1));
			}
			finally {
				session.IncludeStack.Clear ();
				host.LogErrors (pt.Errors);
			}

			return run;
		}

		void InitializeSessionWithHostData (ITextTemplatingEngineHost host, ITextTemplatingSession session)
		{
			try {
				session.TemplateFile = host.TemplateFile;
			} catch(NotImplementedException) {
				session.TemplateFile = string.Empty;
			}

			session.IncludeStack.Push (session.TemplateFile);

			if (host is ITextTemplatingSessionHost sessionHost) {
				session.UserTransformationSession = sessionHost;
			}
		}

		IDebugTransformationRun CompileAndPrepareRun (ParsedTemplate template, ITextTemplatingEngineHost host, ITextTemplatingSession session, IDebugTransformationRunFactory runFactory, TemplateSettings settings) 
		{
			TransformationRunner runner = null;
			bool success = false;

			try {
				try {
					if (runFactory.CreateTransformationRun (typeof (TransformationRunner), template, null) is TransformationRunner theRunner) {
						runner = theRunner;
					}
				}
				catch (Exception ex) {
					if (IsCriticalException (ex)) {
						throw;
					}
				}
				if (runner != null && !runner.Errors.HasErrors) {
					string[] assemblies = ProcessReferences (host, template, settings);
					if (!template.Errors.HasErrors) {
						runner.PreLoadAssemblies (assemblies);

						try {
							success = runner.PrepareTransformation (session, template, settings.HostSpecific ? host : null);
						}
						catch (SerializationException) {
							template.LogError (Resources.SessionHostMarshalError, new Location (session.TemplateFile, -1, -1));
							throw;
						}
					}
				}
			}
			catch(Exception ex) {
				if (IsCriticalException (ex)) {
					throw;
				}
				template.LogError (ex.ToString (), new Location (session.TemplateFile, -1, -1));
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
