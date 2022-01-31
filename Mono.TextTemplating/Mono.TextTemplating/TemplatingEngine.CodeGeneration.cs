// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualStudio.TextTemplating;

using Mono.TextTemplating.CodeDomBuilder;

namespace Mono.TextTemplating
{
	partial class TemplatingEngine
	{
		public static CodeCompileUnit GenerateCompileUnit (ITextTemplatingEngineHost host, string content, ParsedTemplate pt, TemplateSettings settings)
		{
			ProcessDirectives (content, settings, pt.Errors);

			//prep the compile unit
			var ccu = new CodeCompileUnit ();
			var namespac = ccu.AddNamespace (settings.Namespace.NullIfEmpty ());

			foreach (string ns in settings.Imports.Union (host.StandardImports))
				namespac.AddImport (new CodeNamespaceImport (ns));

			var baseInfo = TemplateBaseTypeInfo.FromSettings (settings);

			var type = namespac.AddClass (settings.Name)
				.AsPartial ()
				.Inherits (baseInfo.Reference)
				.WithVisibility (settings.InternalVisibility? TypeAttributes.NotPublic : TypeAttributes.Public);

			if (baseInfo.Declaration is not null) {
				namespac.AddType (baseInfo.Declaration);
			}

			GenerateTransformMethod (type, settings, pt, host.TemplateFile, baseInfo.HasVirtualTransformMethod);

			//class code and attributes from processors
			foreach (var processor in settings.DirectiveProcessors.Values) {
				if (processor.GetClassCodeForProcessingRun ().NullIfEmpty() is string classCode) {
					type.AddSnippetMember (classCode);
				}
				processor.GetTemplateClassCustomAttributes ()?.AddTo (type);
			}

			//generate the Host property if needed
			if (settings.HostSpecific && !settings.HostPropertyOnBase) {
				GenerateHostProperty (type, settings.HostType);
			}

			GenerateInitializationMethod (type, settings, baseInfo.HasVirtualInitializeMethod);

			return ccu;
		}


		static void GenerateTransformMethod (CodeTypeDeclaration templateType, TemplateSettings settings, ParsedTemplate pt, string templateFile, bool isOverride)
		{
			string baseDirectory = Path.GetDirectoryName (templateFile);

			var transformMeth = Declare.Method ("TransformText").Returns<string> ().AsVirtual ();

			if (isOverride) {
				transformMeth.AsOverride ();
			}

			transformMeth.WithStatements (Expression.This.SetProperty ("GenerationEnvironment", Expression.Null));

			CodeExpression toStringHelper = settings.IsPreprocessed
				? Expression.This.Property ("ToStringHelper")
				: TypeReference.Global (typeof (ToStringHelper)).AsExpression ();

			//method references that will need to be used multiple times
			var writeMeth = Expression.This.Method ("Write");
			var toStringMeth = toStringHelper.Method ("ToStringWithCulture");
			bool helperMode = false;

			//build the code from the segments
			foreach (TemplateSegment seg in pt.Content) {
				CodeStatement st = null;
				CodeLinePragma location = null;
				if (!settings.NoLinePragmas) {
					var f = seg.StartLocation.FileName ?? templateFile;
					if (!string.IsNullOrEmpty (f)) {
						// FIXME: we need to know where the output file will be to make this work properly
						if (settings.RelativeLinePragmas) {
							f = FileUtil.AbsoluteToRelativePath (baseDirectory, f);
						} else {
							f = Path.GetFullPath (f);
						}
					}
					location = new CodeLinePragma (f, seg.StartLocation.Line);
				}
				switch (seg.Type) {
				case SegmentType.Block:
					if (helperMode)
						//TODO: are blocks permitted after helpers?
						pt.LogError ("Blocks are not permitted after helpers", seg.TagStartLocation);
					st = Statement.Snippet (seg.Text);
					break;
				case SegmentType.Expression:
					st = writeMeth.Invoke (toStringMeth.Invoke (Expression.Snippet (seg.Text))).AsStatement ();
					break;
				case SegmentType.Content:
					st = writeMeth.Invoke (Expression.Primitive (seg.Text)).AsStatement ();
					break;
				case SegmentType.Helper:
					if (!string.IsNullOrEmpty (seg.Text))
						templateType.AddSnippetMember (seg.Text, location);
					helperMode = true;
					break;
				default:
					throw new InvalidOperationException ();
				}
				if (st != null) {
					if (helperMode) {
						//convert the statement into a snippet member and attach it to the top level type
						//TODO: is there a way to do this for languages that use indentation for blocks, e.g. python?
						using (var writer = new StringWriter ()) {
							settings.Provider.GenerateCodeFromStatement (st, writer, null);
							var text = writer.ToString ();
							if (!string.IsNullOrEmpty (text))
								templateType.AddSnippetMember (text, location);
						}
					} else {
						st.LinePragma = location;
						transformMeth.Statements.Add (st);
						continue;
					}
				}
			}

			transformMeth.WithStatements (Statement.Return (Expression.This.Property ("GenerationEnvironment").InvokeMethod ("ToString")));

			templateType.AddMember (transformMeth);
		}

		static void ProcessDirectives (string content, TemplateSettings settings, CompilerErrorCollection errors)
		{
			foreach (var processor in settings.DirectiveProcessors.Values) {
				processor.StartProcessingRun (settings.Provider, content, errors);
			}

			foreach (var dt in settings.CustomDirectives) {
				var processor = settings.DirectiveProcessors[dt.ProcessorName];
				processor.ProcessDirective (dt.Directive.Name, dt.Directive.Attributes);
			}

			foreach (var processor in settings.DirectiveProcessors.Values) {
				processor.FinishProcessingRun ();

				var imports = processor.GetImportsForProcessingRun ();
				if (imports != null)
					settings.Imports.UnionWith (imports);
				var references = processor.GetReferencesForProcessingRun ();
				if (references != null)
					settings.Assemblies.UnionWith (references);
			}
		}

		static void GenerateHostProperty (CodeTypeDeclaration type, Type hostType)
		{
			hostType ??= typeof (ITextTemplatingEngineHost);

			type.AddPropertyGetSet ("Host", type.AddField ("hostValue", TypeReference.Global (hostType)));
		}

		static void GenerateInitializationMethod (CodeTypeDeclaration type, TemplateSettings settings, bool isOverride)
		{
			var initializeMeth = Declare.Method ("Initialize").Returns (TypeReference.Void).AsVirtual ();

			if (isOverride) {
				initializeMeth.AsOverride ();
			}

			//if preprocessed, pass the extension and encoding to the host
			if (settings.IsPreprocessed && settings.HostSpecific) {
				var hostProp = Expression.This.Property ("Host");
				var statements = new List<CodeStatement> ();

				if (!string.IsNullOrEmpty (settings.Extension)) {
					statements.Add (hostProp.InvokeMethod ("SetFileExtension", Expression.Primitive (settings.Extension)).AsStatement ());
				}

				if (settings.Encoding != null) {
					statements.Add (
						hostProp.InvokeMethod ("SetOutputEncoding",
							// FIXME: this should be Global but that changes codegen output
							TypeReference.Default (typeof (Encoding)).Method ("GetEncoding")
								.Invoke (Expression.Primitive (settings.Encoding.CodePage), Expression.True))
						.AsStatement ());
				}

				if (statements.Count > 0) {
					initializeMeth.WithStatements (
						Statement.If (hostProp.IsNotNull (),
							Then: statements.ToArray ())
					);
				}
			}

			//pre-init code from processors
			foreach (var processor in settings.DirectiveProcessors.Values) {
				string code = processor.GetPreInitializationCodeForProcessingRun ();
				if (code != null)
					initializeMeth.Statements.Add (new CodeSnippetStatement (code));
			}

			if (isOverride) {
				initializeMeth.WithStatements (Expression.Base.InvokeMethod ("Initialize"));
			}

			//post-init code from processors
			foreach (var processor in settings.DirectiveProcessors.Values) {
				string code = processor.GetPostInitializationCodeForProcessingRun ();
				if (code != null)
					initializeMeth.Statements.Add (new CodeSnippetStatement (code));
			}

			type.Members.Add (initializeMeth);
		}
	}
}
