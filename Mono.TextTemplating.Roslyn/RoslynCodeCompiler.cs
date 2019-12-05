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


			var source = File.ReadAllText (arguments.SourceFiles.Single ());
			var syntaxTree = CSharpSyntaxTree.ParseText (source);

			var compilation = CSharpCompilation.Create (
				"GeneratedTextTransformation",
				new List<SyntaxTree> {syntaxTree},
				references,
				new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary)
			);

			var pdbFilePath = Path.ChangeExtension(arguments.OutputPath, "pdb");

			EmitResult result;
			using (var fs = File.OpenWrite (arguments.OutputPath)) {
				using (var symbolsStream = File.OpenWrite(pdbFilePath)) {
					var emitOptions = new EmitOptions(
						debugInformationFormat: DebugInformationFormat.PortablePdb,
						pdbFilePath: pdbFilePath);


					var embeddedTexts = new List<EmbeddedText> {
						EmbeddedText.FromSource(
							arguments.SourceFiles.Single(),
							SourceText.From(source, Encoding.UTF8)),
					};

					result = compilation.Emit(
						fs,
						symbolsStream,
						embeddedTexts: embeddedTexts,
						options: emitOptions);
				}
			}

			if (result.Success) {
				return new CodeCompilerResult {
					Output = new List<string> (),
					Success = true,
					Errors = new List<CodeCompilerError> ()
				};
			}

			var failures = result.Diagnostics.Where (x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error);

			return new CodeCompilerResult {
				Success = false,
				Output = new List<string> (),
				Errors = failures.Select (
					x => new CodeCompilerError { Message = x.GetMessage () }).ToList (),
			};
		}
	}
}