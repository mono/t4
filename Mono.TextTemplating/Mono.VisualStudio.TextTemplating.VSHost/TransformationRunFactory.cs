using System;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating.VSHost
{
	public abstract class TransformationRunFactory :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IDebugTransformationRunFactory
	{
		public const string TransformationRunFactoryPrefix = "TransformationRunFactoryService";
		public const string TransformationRunFactorySuffix = nameof (TransformationRunFactory);
		/// <summary>
		/// Create the transformation runner
		/// </summary>
		/// <param name="runnerType"></param>
		/// <param name="template"></param>
		/// <param name="resolver"></param>
		/// <returns></returns>
		/// <remarks>
		/// abstracted, just because I am uncertain on how this would run on multiple platforms. Also visual studio classes may be required to pull of correctly.
		/// </remarks>
		public abstract IDebugTransformationRun CreateTransformationRun (Type runnerType, ParsedTemplate template, ResolveEventHandler resolver);

		public abstract string RunTransformation (IDebugTransformationRun transformationRun);
#if FEATURE_APPDOMAINS
		public override object InitializeLifetimeService ()
		{
			return null;
		}
#endif
	}
}
