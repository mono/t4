using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.VisualStudio.TextTemplating
{
	public abstract class TransformationRunFactory :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IDebugTransformationRunFactory
	{
		public const string TransformationRunFactoryPrefix = "TransformationRunFactoryService";
		public const string TransformationRunFactorySuffix = nameof (TransformationRunFactory);

		public abstract IDebugTransformationRun CreateTransformationRun (Type t, string content, ResolveEventHandler resolver);

		public abstract string RunTransformation (IDebugTransformationRun transformationRun);
#if FEATURE_APPDOMAINS
		public override object InitializeLifetimeService ()
		{
			return null;
		}
#endif
	}
}
