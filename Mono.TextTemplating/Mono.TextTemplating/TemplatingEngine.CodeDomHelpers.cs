// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

using Mono.TextTemplating.CodeDomBuilder;

namespace Mono.TextTemplating
{
	// NOTE: these should not have been public, but keep them here so as not to break API
	partial class TemplatingEngine
	{
		/// <summary>
		/// An implementation of CodeDomProvider.GenerateCodeFromMember that works on Mono.
		/// </summary>
		[Obsolete ("Should not have been public")]
		public static void GenerateCodeFromMembers (CodeDomProvider provider, CodeGeneratorOptions options, StringWriter sw, IEnumerable<CodeTypeMember> members)
			=> IndentHelpers.GenerateCodeFromMembers (provider, options, sw, members);

		[Obsolete ("Should not have been public")]
		public static string GenerateIndentedClassCode (CodeDomProvider provider, params CodeTypeMember[] members)
			=> IndentHelpers.GenerateIndentedClassCode (provider, members);

		[Obsolete ("Should not have been public")]
		public static string GenerateIndentedClassCode (CodeDomProvider provider, IEnumerable<CodeTypeMember> members)
			=> IndentHelpers.GenerateIndentedClassCode (provider, members);

		[Obsolete ("Should not have been public")]
		public static string IndentSnippetText (CodeDomProvider provider, string text, string indent)
			=> IndentHelpers.IndentSnippetText (provider, text, indent);


		[Obsolete ("Should not have been public")]
		public static string IndentSnippetText (string text, string indent)
			=> IndentHelpers.IndentSnippetText (text, indent);
	}
}
