// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Mono.TextTemplating.CodeCompilation;

using CodeCompiler = Mono.TextTemplating.CodeCompilation.CodeCompiler;

namespace Mono.TextTemplating
{
	class RoslynCodeCompiler : CodeCompiler
	{
		readonly RuntimeInfo runtime;

		public RoslynCodeCompiler (RuntimeInfo runtime)
		{
			this.runtime = runtime;
		}

		public override async Task<CodeCompilerResult> CompileFile (
			CodeCompilerArguments arguments,
			TextWriter log,
			CancellationToken token)
		{
			var references = new List<MetadataReference> ();
			foreach (var assemblyReference in AssemblyResolver.GetResolvedReferences (runtime, arguments.AssemblyReferences)) {
				references.Add (MetadataReference.CreateFromFile (assemblyReference));
			}

			var syntaxTrees = new List<SyntaxTree> ();
			foreach (var sourceFile in arguments.SourceFiles) {
				using var stream = File.OpenRead (sourceFile);
				var sourceText = SourceText.From (stream, Encoding.UTF8);
				syntaxTrees.Add (CSharpSyntaxTree.ParseText (sourceText));
			}

			var compilation = CSharpCompilation.Create (
				"GeneratedTextTransformation",
				syntaxTrees,
				references,
				new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary)
			);

			EmitOptions emitOptions = null;
			if (arguments.Debug) {
				emitOptions = new EmitOptions (debugInformationFormat: DebugInformationFormat.Embedded);
			}

			using var fs = File.OpenWrite (arguments.OutputPath);
			EmitResult result = compilation.Emit (fs, options: emitOptions);

			if (result.Success) {
				return new CodeCompilerResult {
					Output = new List<string> (),
					Success = true,
					Errors = new List<CodeCompilerError> ()
				};
			}

			var failures = result.Diagnostics.Where (x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);
			var errors = failures.Select (x => {
				var location = x.Location.GetMappedLineSpan ();
				var startLinePosition = location.StartLinePosition;
				var endLinePosition = location.EndLinePosition;
				return new CodeCompilerError {
					Message = x.GetMessage (),
					Column = startLinePosition.Character,
					Line = startLinePosition.Line,
					EndLine = endLinePosition.Line,
					EndColumn = endLinePosition.Character,
					IsError = x.Severity == DiagnosticSeverity.Error,
					Origin = location.Path
				};
			}).ToList ();

			return new CodeCompilerResult {
				Success = false,
				Output = new List<string> (),
				Errors = errors
			};
		}
	}
}
