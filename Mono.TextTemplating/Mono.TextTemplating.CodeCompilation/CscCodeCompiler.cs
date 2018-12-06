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
using System.Threading;
using System.Threading.Tasks;

namespace Mono.TextTemplating.CodeCompilation
{
	class CscCodeCompiler : CodeCompiler
	{
		readonly string cscPath;

		public CscCodeCompiler (string cscPath)
		{
			this.cscPath = cscPath;
		}

		static StreamWriter CreateTempTextFile (string extension, out string path)
		{
			path = null;
			Exception ex = null;
			try {
				var tempDir = Path.GetTempPath ();
				Directory.CreateDirectory (tempDir);

				//this is how msbuild does it...
				path = Path.Combine ($"tmp{Guid.NewGuid ():N}{extension}");
				if (!File.Exists (path)) {
					return File.CreateText (path);
				}
 			} catch (Exception e) {
				ex = e;
			}
			throw new Exception ("Failed to create temp file", ex);
		}

		public override async Task<CodeCompilerResult> CompileFile (CodeCompilerArguments arguments, CancellationToken token)
		{
			string rspPath;
			using (var rsp = CreateTempTextFile (".rsp", out rspPath)) {
				rsp.WriteLine ("-target:library");

				if (arguments.Debug) {
					rsp.WriteLine ("-debug");
				}

				foreach (var reference in arguments.AssemblyReferences) {
					rsp.Write ("-r:");
					rsp.WriteLine (reference);
				}

				foreach (var file in arguments.SourceFiles) {
					rsp.WriteLine (file);
				}

				rsp.Write ("-out:");
				rsp.WriteLine (arguments.OutputPath);
			}

			var psi = new System.Diagnostics.ProcessStartInfo (cscPath) {
				Arguments = $"-nologo -noconfig \"@{rspPath}\" {arguments.AdditionalArguments}",
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};

			var stdout = new StringWriter ();
			var stderr = new StringWriter ();
			var process = ProcessUtils.StartProcess (psi, stdout, stderr, token);

			var result = await process;

			var outputList = new List<string> ();
			var errors = new List<CodeCompilerError> ();

			void ConsumeOutput (string s)
			{
				using (var sw = new StringReader (s)) {
					string line;
					while ((line = sw.ReadLine ()) != null) {
						outputList.Add (line);
						var err = MSBuildErrorParser.TryParseLine (line);
						if (err != null) {
							errors.Add (err);
						}
					}
				}
			}

			ConsumeOutput (stdout.ToString ());
			ConsumeOutput (stderr.ToString ());

			return new CodeCompilerResult {
				Success = result == 0,
				Errors = errors,
				ExitCode = result,
				Output = outputList
			};
		}
	}
}
