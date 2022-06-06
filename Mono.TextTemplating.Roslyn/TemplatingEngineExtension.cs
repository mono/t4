// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating
{
	public static class RoslynTemplatingEngineExtensions
	{
		public static void UseInProcessCompiler (this TemplatingEngine engine)
		{
			engine.SetCompilerFunc ((RuntimeInfo r) => new RoslynCodeCompiler (r));

			RuntimeInfo.ThrowOnMissingDotNetCoreSdkDirectory = false;
		}

		public static void UseInProcessCompiler (this TemplateGenerator generator)
		{
			generator.Engine.UseInProcessCompiler ();
		}
	}
}
