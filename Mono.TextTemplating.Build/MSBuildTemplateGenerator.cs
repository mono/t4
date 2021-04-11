// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom.Compiler;

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

		protected override bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			// TODO: record the filename
			return base.LoadIncludeText (requestFileName, out content, out location);
		}
	}
}
