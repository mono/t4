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

using Microsoft.VisualStudio.TextTemplating;
using System.CodeDom.Compiler;

namespace Mono.TextTemplating
{
	class ToolTemplateGenerator : TemplateGenerator, ITextTemplatingSessionHost
	{
		public ToolTemplateGenerator ()
		{
			Refs.Add (typeof (CompilerErrorCollection).Assembly.Location);
		}

		public ITextTemplatingSession Session { get; set; }

		public ITextTemplatingSession CreateSession ()
		{
			return Session = new ToolTemplateSession (this);
		}

		public void Preprocess (string className, string inputFile, string inputContent, out string outputContent)
		{
			string classNamespace = null;
			int s = className.LastIndexOf ('.');
			if (s > 0) {
				classNamespace = className.Substring (0, s);
				className = className.Substring (s + 1);
			}

			PreprocessTemplate (inputFile, className, classNamespace, inputContent, out string language, out string [] references, out outputContent);
		}
	}
}
