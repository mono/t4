using System;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating.VSHost
{
	public abstract class TransformationRunFactory :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IProcessTransformationRunFactory
	{
		public const string TransformationRunFactoryPrefix = "TransformationRunFactoryService";
		public const string TransformationRunFactorySuffix = nameof (TransformationRunFactory);
		/// <summary>
		/// Create the transformation runner
		/// </summary>
		/// <param name="runnerType"></param>
		/// <param name="pt"></param>
		/// <param name="resolver"></param>
		/// <returns></returns>
		/// <remarks>
		/// abstracted, just because I am uncertain on how this would run on multiple platforms. Also visual studio classes may be required to pull of correctly.
		/// </remarks>
		public abstract IProcessTransformationRun CreateTransformationRun (Type runnerType, ParsedTemplate pt, ResolveEventHandler resolver);

		public abstract string RunTransformation (IProcessTransformationRun transformationRun);
#if FEATURE_APPDOMAINS
		public override object InitializeLifetimeService ()
		{
			return null;
		}
#endif
	}
}
