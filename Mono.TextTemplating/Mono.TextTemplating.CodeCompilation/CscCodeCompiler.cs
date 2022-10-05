//
// CodeCompiler.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2018 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.TextTemplating.CodeCompilation
{
	class CscCodeCompiler : CodeCompiler
	{
		readonly RuntimeInfo runtime;

		public CscCodeCompiler (RuntimeInfo runtime)
		{
			this.runtime = runtime;
		}

		static StreamWriter CreateTempTextFile (string extension, out string path)
		{
			path = null;
			Exception ex = null;
			try {
				var tempDir = Path.GetTempPath ();
				Directory.CreateDirectory (tempDir);

				//this is how msbuild does it...
				path = Path.Combine (tempDir, $"tmp{Guid.NewGuid ():N}{extension}");
				if (!File.Exists (path)) {
					return File.CreateText (path);
				}
 			} catch (Exception e) {
				ex = e;
			}
			throw new TemplatingEngineException ("Failed to create temp file", ex);
		}

		/// <summary>
		/// Compiles the file.
		/// </summary>
		/// <returns>The file.</returns>
		/// <param name="arguments">Arguments.</param>
		/// <param name="token">Token.</param>
		public override async Task<CodeCompilerResult> CompileFile (CodeCompilerArguments arguments, TextWriter log, CancellationToken token)
		{
			string rspPath;
			StreamWriter rsp;
			if (arguments.TempDirectory != null) {
				rspPath = Path.Combine (arguments.TempDirectory, "response.rsp");
				rsp = File.CreateText (rspPath);
			} else {
				rsp = CreateTempTextFile (".rsp", out rspPath);
			}

			using (rsp) {
				rsp.WriteLine ("-target:library");

				if (arguments.Debug) {
					rsp.WriteLine ("-debug");
				}

				var langVersionArg = CSharpLangVersionHelper.GetLangVersionArg (arguments, runtime);
				if (langVersionArg != null) {
					rsp.WriteLine (langVersionArg);
				}

				foreach (var reference in AssemblyResolver.GetResolvedReferences (runtime, arguments.AssemblyReferences)) {
					rsp.Write ("-r:");
					rsp.Write ("\"");
					rsp.Write (reference);
					rsp.WriteLine ("\"");
				}

				rsp.Write ("-out:");
				rsp.Write ("\"");
				rsp.Write (arguments.OutputPath);
				rsp.WriteLine ("\"");

				if (arguments.AdditionalArguments != null) {
					rsp.WriteLine (arguments.AdditionalArguments);
				}

				//in older versions of csc, these must come last
				foreach (var file in arguments.SourceFiles) {
					rsp.Write ("\"");
					rsp.Write (file);
					rsp.WriteLine ("\"");
				}
			}

			var psi = new System.Diagnostics.ProcessStartInfo (runtime.CscPath) {
				Arguments = $"-nologo -noconfig \"@{rspPath}\"",
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};

			if (log != null) {
				log.WriteLine ($"{psi.FileName} {psi.Arguments}");
			}

			if (runtime.Kind == RuntimeKind.NetCore) {
				psi.Arguments = $"\"{psi.FileName}\" {psi.Arguments}";
				psi.FileName = Path.GetFullPath (Path.Combine (runtime.RuntimeDir, "..", "..", "..", "dotnet"));
			}

			var stdout = new StringWriter ();
			var stderr = new StringWriter ();

			TextWriter outWriter = stderr, errWriter = stderr;
			if (log != null) {
				outWriter = new SplitOutputWriter (log, outWriter);
				errWriter = new SplitOutputWriter (log, errWriter);
			}

			var process = ProcessUtils.StartProcess (psi, outWriter, errWriter, token);

			var result = await process.ConfigureAwait (false);

			var outputList = new List<string> ();
			var errors = new List<CodeCompilerError> ();

			void ConsumeOutput (string s)
			{
				using var sw = new StringReader (s);
				string line;
				while ((line = sw.ReadLine ()) != null) {
					outputList.Add (line);
					var err = MSBuildErrorParser.TryParseLine (line);
					if (err != null) {
						errors.Add (err);
					}
				}
			}

			ConsumeOutput (stdout.ToString ());
			ConsumeOutput (stderr.ToString ());

			if (log != null) {
				log.WriteLine ($"{psi.FileName} {psi.Arguments}");
			}

			return new CodeCompilerResult {
				Success = result == 0,
				Errors = errors,
				ExitCode = result,
				Output = outputList,
				ResponseFile = rspPath
			};
		}

		//we know that ProcessUtils.StartProcess only uses WriteLine and Write(string)
		class SplitOutputWriter : TextWriter
		{
			readonly TextWriter a;
			readonly TextWriter b;

			public SplitOutputWriter (TextWriter a, TextWriter b)
			{
				this.a = a;
				this.b = b;
			}

			public override Encoding Encoding => Encoding.UTF8;

			public override void WriteLine ()
			{
				a.WriteLine ();
				b.WriteLine ();
			}

			public override void Write (string value)
			{
				a.Write (value);
				b.Write (value);
			}
		}
	}
}
