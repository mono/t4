using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.TextTemplating.CodeCompilation;
using CodeCompiler = Mono.TextTemplating.CodeCompilation.CodeCompiler;

namespace Mono.TextTemplating.Roslyn
{
	class RoslynCodeCompiler : CodeCompiler
	{
		readonly RuntimeInfo _runtime;

		public RoslynCodeCompiler (RuntimeInfo runtime)
		{
			_runtime = runtime;
		}

		public  override async Task<CodeCompilerResult> CompileFile (
			CodeCompilerArguments arguments,
			TextWriter log,
			CancellationToken token)
		{
			var references = new List<MetadataReference> ();
			foreach (var assemblyReference in arguments.AssemblyReferences) {
				var argumentsAssemblyReference = assemblyReference;
				var path = AssemblyResolver.Resolve(_runtime, argumentsAssemblyReference);
				references.Add (MetadataReference.CreateFromFile (path));
			}

			references.Add (MetadataReference.CreateFromFile (typeof(object).Assembly.Location));
			references.Add (MetadataReference.CreateFromFile (typeof(Enumerable).Assembly.Location));
			references.Add (MetadataReference.CreateFromFile (typeof(string).Assembly.Location));
			references.Add (MetadataReference.CreateFromFile (typeof(Console).Assembly.Location));
			references.Add (MetadataReference.CreateFromFile (typeof(IntPtr).Assembly.Location));
			references.Add (MetadataReference.CreateFromFile (typeof(AssemblyTargetedPatchBandAttribute).Assembly.Location));
			references.Add (MetadataReference.CreateFromFile (Assembly.Load ("netstandard, Version=2.0.0.0").Location));

			var source = File.ReadAllText (arguments.SourceFiles.Single ());
			var syntaxTree = CSharpSyntaxTree.ParseText (source);

			var compilation = CSharpCompilation.Create (
				"GeneratedTextTransformation",
				new List<SyntaxTree> {syntaxTree},
				references,
				new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary)
			);

			EmitResult result;
			using (var fs = File.OpenWrite (arguments.OutputPath)) {
				result = compilation.Emit (fs);
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
					x => new CodeCompilerError {Message = x.GetMessage ()}).ToList (),
			};
		}
	}
}