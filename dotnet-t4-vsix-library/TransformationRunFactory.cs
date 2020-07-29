using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Mono.TextTemplating;

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

		public abstract IDebugTransformationRun CreateTransformationRun (Type runnerType, ParsedTemplate template, Func<AssemblyLoadContext, AssemblyName, Assembly> resolver);

		public abstract string RunTransformation (IDebugTransformationRun transformationRun);
#if FEATURE_APPDOMAINS
		public override object InitializeLifetimeService ()
		{
			return null;
		}
#endif
	}
}
