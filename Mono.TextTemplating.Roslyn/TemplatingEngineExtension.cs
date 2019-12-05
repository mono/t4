// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating
{
	public static class RoslynTemplatingEngineExtensions
	{
		public static void UseInProcessCompiler (this TemplatingEngine engine)
		{
			engine.SetCompilerFunc (() => new RoslynCodeCompiler (RuntimeInfo.GetRuntime ()));
		}
	}
}
