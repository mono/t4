namespace Mono.TextTemplating.CodeCompilation
{
    public interface ICodeCompilationContext
    {
        /// <summary>
        /// The directory to search for the compiler in.
        /// </summary>
        string CompilerSearchPath { get; }
    }
}