using System;
using System.IO;

namespace Mono.TextTemplating.CodeCompilation
{
    public class DefaultCodeCompilationContext : ICodeCompilationContext
    {
        public string CompilerSearchPath { get; } = Path.GetDirectoryName (typeof (object).Assembly.Location);

    }
}