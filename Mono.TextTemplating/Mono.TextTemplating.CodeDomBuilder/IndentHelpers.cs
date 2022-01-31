// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.CSharp;

namespace Mono.TextTemplating.CodeDomBuilder;

static class IndentHelpers
{
	public static string GenerateIndentedClassCode (this CodeDomProvider provider, params CodeTypeMember[] members)
		 => GenerateIndentedClassCode (provider, (IEnumerable<CodeTypeMember>)members);

	public static string GenerateIndentedClassCode (this CodeDomProvider provider, IEnumerable<CodeTypeMember> members)
	{
		var options = new CodeGeneratorOptions ();
		using (var sw = new StringWriter ()) {
			GenerateCodeFromMembers (provider, options, sw, members);
			return IndentSnippetText (provider, sw.ToString (), "        ");
		}
	}

	public static CodeSnippetTypeMember CreateSnippetMember (string value, CodeLinePragma location = null)
	{
		//HACK: workaround for code generator not indenting first line of member snippet when inserting into class
		const string indent = "\n        ";
		if (!char.IsWhiteSpace (value[0]))
			value = indent + value;

		return new CodeSnippetTypeMember (value) { LinePragma = location };
	}

	public static string IndentSnippetText (this CodeDomProvider provider, string text, string indent)
	{
		if (provider is CSharpCodeProvider)
			return IndentSnippetText (text, indent);
		return text;
	}

	public static string IndentSnippetText (string text, string indent)
	{
		var builder = new StringBuilder (text.Length);
		builder.Append (indent);
		int lastNewline = 0;
		for (int i = 0; i < text.Length - 1; i++) {
			char c = text[i];
			if (c == '\r') {
				if (text[i + 1] == '\n') {
					i++;
					if (i == text.Length - 1)
						break;
				}
			} else if (c != '\n' || text[i + 1] == '\n') {
				continue;
			}
			i++;
			int len = i - lastNewline;
			if (len > 0) {
				builder.Append (text, lastNewline, i - lastNewline);
			}
			builder.Append (indent);
			lastNewline = i;
		}
		if (lastNewline > 0)
			builder.Append (text, lastNewline, text.Length - lastNewline);
		else
			builder.Append (text);
		return builder.ToString ();
	}

	/// <summary>
	/// An implementation of CodeDomProvider.GenerateCodeFromMember that works on Mono.
	/// </summary>
	public static void GenerateCodeFromMembers (this CodeDomProvider provider, CodeGeneratorOptions options, StringWriter sw, IEnumerable<CodeTypeMember> members)
	{
		if (!useMonoHack) {
			foreach (CodeTypeMember member in members)
				provider.GenerateCodeFromMember (member, sw, options);
			return;
		}

#pragma warning disable 0618
		var generator = (CodeGenerator)provider.CreateGenerator ();
#pragma warning restore 0618
		var dummy = new CodeTypeDeclaration ("Foo");

		foreach (CodeTypeMember member in members) {
			if (member is CodeMemberField f) {
				initializeCodeGenerator (generator, sw, options);
				cgFieldGen.Invoke (generator, new object[] { f });
				continue;
			}
			if (member is CodeMemberProperty p) {
				initializeCodeGenerator (generator, sw, options);
				cgPropGen.Invoke (generator, new object[] { p, dummy });
				continue;
			}
			if (member is CodeMemberMethod m) {
				initializeCodeGenerator (generator, sw, options);
				cgMethGen.Invoke (generator, new object[] { m, dummy });
				continue;
			}
		}
	}

	//HACK: older versions of Mono don't implement GenerateCodeFromMember
	// We have a workaround via reflection. First attempt to reflect the members we need to work around it.
	// If they don't exist, we should be running on a version where it's fixed.
	static readonly bool useMonoHack = InitializeMonoHack ();
	static MethodInfo cgFieldGen, cgPropGen, cgMethGen;
	static Action<CodeGenerator, StringWriter, CodeGeneratorOptions> initializeCodeGenerator;

	static bool InitializeMonoHack ()
	{
		if (Type.GetType ("Mono.Runtime") == null) {
			return false;
		}

		var cgType = typeof (CodeGenerator);

		var cgInit = cgType.GetMethod ("InitOutput", BindingFlags.NonPublic | BindingFlags.Instance);
		if (cgInit != null) {
			initializeCodeGenerator = new Action<CodeGenerator, StringWriter, CodeGeneratorOptions> ((generator, sw, options) => {
				cgInit.Invoke (generator, new object[] { sw, options });
			});
		} else {
			var cgOptions = cgType.GetField ("options", BindingFlags.NonPublic | BindingFlags.Instance);
			var cgOutput = cgType.GetField ("output", BindingFlags.NonPublic | BindingFlags.Instance);

			if (cgOptions == null || cgOutput == null) {
				return false;
			}

			initializeCodeGenerator = new Action<CodeGenerator, StringWriter, CodeGeneratorOptions> ((generator, sw, options) => {
				var output = new IndentedTextWriter (sw);
				cgOptions.SetValue (generator, options);
				cgOutput.SetValue (generator, output);
			});
		}

		cgFieldGen = cgType.GetMethod ("GenerateField", BindingFlags.NonPublic | BindingFlags.Instance);
		cgPropGen = cgType.GetMethod ("GenerateProperty", BindingFlags.NonPublic | BindingFlags.Instance);
		cgMethGen = cgType.GetMethod ("GenerateMethod", BindingFlags.NonPublic | BindingFlags.Instance);

		if (cgFieldGen == null || cgPropGen == null || cgMethGen == null) {
			return false;
		}

		return true;
	}
}
