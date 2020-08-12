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
using System.Linq;
using System.Text;
using System.Globalization;

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
			throw new Exception ("Failed to create temp file", ex);
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

				//in older versions of csc, these must come last
				foreach (var file in arguments.SourceFiles) {
					rsp.Write ("\"");
					rsp.Write (file);
					rsp.WriteLine ("\"");
				}
			}

			var psi = new System.Diagnostics.ProcessStartInfo (runtime.CscPath) {
				Arguments = $"-nologo -noconfig \"@{rspPath}\" {arguments.AdditionalArguments}",
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};

			if (runtime.Kind == RuntimeKind.NetCore) {
				psi.Arguments = $"\"{psi.FileName}\" {psi.Arguments}";
				psi.FileName = Path.GetFullPath (Path.Combine (runtime.RuntimeDir, "..", "..", "..", "dotnet"));
			}

			if (log != null)
			{
				log.WriteLine($"{psi.FileName} {psi.Arguments}");
				log.WriteLine ("-------------------------------------------------------------------------------");
			}

			using (var stdout = new StringWriter (new StringBuilder(), CultureInfo.CurrentCulture))
			using (var stderr = new StringWriter (new StringBuilder (), CultureInfo.CurrentCulture))
			using (TextWriter outWriter = log != null ? new SplitOutputWriter (log, stderr) : (TextWriter)stderr)
			using (TextWriter errWriter = log != null ? new SplitOutputWriter (log, stderr) : (TextWriter)stderr) {

				var process = ProcessUtils.StartProcess (psi, outWriter, errWriter, token);

				int result = -1;

				if (!token.IsCancellationRequested) {
					result = await process.ConfigureAwait (false);
				}

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

				if (log != null) {
					log.WriteLine ();
					log.WriteLine ();
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
		}

		//we know that ProcessUtils.StartProcess only uses WriteLine and Write(string)
		class SplitOutputWriter : TextWriter
		{
			readonly TextWriter logWriter;
			readonly TextWriter errorWriter;

			public SplitOutputWriter (TextWriter a, TextWriter b)
			{
				this.logWriter = a;
				this.errorWriter = b;
			}

			public override Encoding Encoding => Encoding.UTF8;

			public override void WriteLine ()
			{
				logWriter.WriteLine ();
				errorWriter.WriteLine ();
			}

			public override void WriteLine (string value)
			{
				logWriter.WriteLine (value);
				errorWriter.WriteLine (value);
			}

			public override void Write (string value)
			{
				logWriter.Write (value);
				errorWriter.Write (value);
			}
		}
	}
}
