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

		protected override bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			bool result = base.LoadIncludeText (requestFileName, out content, out location);
			if (result) {
				IncludedFiles.Add (location);
			}
			return result;
		}

		protected override string ResolveAssemblyReference (string assemblyReference)
		{
			var resolved = base.ResolveAssemblyReference (assemblyReference);
			CapturedReferences.Add (resolved);
			return resolved;
		}

		public List<string> IncludedFiles { get; } = new List<string> ();

		public List<string> CapturedReferences { get; } = new List<string> ();

		public void Reset ()
		{
			ClearSession ();
			IncludedFiles.Clear ();
			CapturedReferences.Clear ();
			Errors.Clear ();
		}
	}
}
