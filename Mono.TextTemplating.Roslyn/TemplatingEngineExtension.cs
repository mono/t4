using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating.Roslyn
{
  public static class TemplatingEngineExtension
  {
      public static void UseInProcessCompiler(this TemplatingEngine engine)
      {
          engine.SetCompilerFunc(() => new RoslynCodeCompiler(RuntimeInfo.GetRuntime()));
      }
  }
}
