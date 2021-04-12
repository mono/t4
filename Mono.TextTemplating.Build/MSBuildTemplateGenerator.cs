// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom.Compiler;
using System.Collections.Generic;

using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Build
{
	class MSBuildTemplateGenerator : TemplateGenerator
	{
		public MSBuildTemplateGenerator ()
		{
			Refs.Add (typeof (CompilerErrorCollection).Assembly.Location);
		}

		protected override ITextTemplatingSession CreateSession () => new MSBuildTemplateSession (this);

		public string PreprocessTemplate (
			ParsedTemplate pt,
			string inputFile,
			string inputContent,
			string className,
			out string[] references,
			TemplateSettings settings = null)
		{
			TemplateFile = inputFile;
			string classNamespace = null;
			int s = className.LastIndexOf ('.');
			if (s > 0) {
				classNamespace = className.Substring (0, s);
				className = className.Substring (s + 1);
			}

			return Engine.PreprocessTemplate (pt, inputContent, this, className, classNamespace, out string language, out references, settings);
		}

		public string ProcessTemplate (
			ParsedTemplate pt,
			string inputFile,
			string inputContent,
			ref string outputFile,
			out string[] references,
			TemplateSettings settings = null)
		{
			TemplateFile = inputFile;
			OutputFile = outputFile;
			using (var compiled = Engine.CompileTemplate (pt, inputContent, this, out references, settings)) {
				var result = compiled?.Process ();
				outputFile = OutputFile;
				return result;
			}
		}

		protected override bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			bool result = base.LoadIncludeText (requestFileName, out content, out location);
			if (result) {
				IncludedFiles.Add (location);
			}
			return result;
		}

		public List<string> IncludedFiles { get; } = new List<string> ();

		public void Reset ()
		{
			ClearSession ();
			IncludedFiles.Clear ();
			Errors.Clear ();
		}
	}
}
