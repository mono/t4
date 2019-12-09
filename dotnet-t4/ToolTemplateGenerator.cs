//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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

using System.CodeDom.Compiler;

using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	class ToolTemplateGenerator : TemplateGenerator
	{
		public ToolTemplateGenerator ()
		{
			Refs.Add (typeof (CompilerErrorCollection).Assembly.Location);
		}

		protected override ITextTemplatingSession CreateSession () => new ToolTemplateSession (this);

		public string PreprocessTemplate (
			ParsedTemplate pt,
			string inputFile,
			string inputContent,
			string className,
			TemplateSettings settings = null)
		{
			TemplateFile = inputFile;
			string classNamespace = null;
			int s = className.LastIndexOf ('.');
			if (s > 0) {
				classNamespace = className.Substring (0, s);
				className = className.Substring (s + 1);
			}

			return Engine.PreprocessTemplate (pt, inputContent, this, className, classNamespace, out string language, out string [] references, settings);
		}

		public string ProcessTemplate (
			ParsedTemplate pt,
			string inputFile,
			string inputContent,
			ref string outputFile,
			TemplateSettings settings = null)
		{
			TemplateFile = inputFile;
			OutputFile = outputFile;
			using (var compiled = Engine.CompileTemplate (pt, inputContent, this, settings)) {
				var result = compiled?.Process ();
				outputFile = OutputFile;
				return result;
			}
		}
	}
}
