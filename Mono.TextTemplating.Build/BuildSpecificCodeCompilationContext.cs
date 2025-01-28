using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating.Build
{
    public sealed class BuildSpecificCodeCompilationContext : ICodeCompilationContext
    {
        
        readonly string _compilerSearchPath;
        public BuildSpecificCodeCompilationContext (string compilerSearchPath)
        {
            _compilerSearchPath = compilerSearchPath;
        }

        public string CompilerSearchPath => _compilerSearchPath;
    }
}