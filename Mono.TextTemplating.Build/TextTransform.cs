// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Build
{
	// parameter values:
	// VS TextTemplatingFileGenerator tool: all MSBuild variables from the project, but not T4ParameterValue items
	// VS MSBuild targets: T4ParameterValue items, but not arbitrary MSBuild properties (ironically)
	// maybe we should add some common ones by default?
	public class TextTransform : Task
	{
		public string DefaultNamespace { get; set; }
		public ITaskItem [] PreprocessTemplates { get; set; }
		public ITaskItem [] TransformTemplates { get; set; }
		public ITaskItem [] IncludePaths { get; set; }
		public ITaskItem [] DirectiveProcessors { get; set; }
		public ITaskItem [] AssemblyReferences { get; set; }
		public ITaskItem [] ReferencePaths { get; set; }

		public ITaskItem [] ParameterValues { get; set; }

		public bool IsDesignTime { get; set; }
		public bool UseLegacyPreprocessingMode { get; set; }

		public string IntermediateDirectory { get; set; }

		[Output]
		public ITaskItem [] RequiredAssemblies { get; set; }

		[Output]
		public ITaskItem [] TransformTemplateOutput { get; set; }

		[Output]
		public ITaskItem [] PreprocessedTemplateOutput { get; set; }

		public override bool Execute ()
		{
			bool success = true;

			var generator = new MSBuildTemplateGenerator ();

			success &= AddParameters (generator, out var parsedParameters);
			success &= AddDirectiveProcessors (generator);

			if (!success) {
				return false;
			}

			AddIncludePaths (generator);

			ParsedTemplate LoadTemplate (string filename, out string inputContent)
			{
				if (!File.Exists (filename)) {
					Log.LogError ("Template file '{0}' does not exist", filename);
					success = false;
					inputContent = null;
					return null;
				}

				try {
					inputContent = File.ReadAllText (filename);
				}
				catch (IOException ex) {
					Log.LogErrorFromException (ex, true, true, filename);
					success = false;
					inputContent = null;
					return null;
				}

				return ParsedTemplate.FromText (inputContent, generator);
			}

			void WriteOutput (string outputFile, string outputContent, Encoding encoding)
			{
				try {
					File.WriteAllText (outputFile, outputContent, encoding ?? new UTF8Encoding (encoderShouldEmitUTF8Identifier: false));
				}
				catch (IOException ex) {
					Log.LogErrorFromException (ex, true, true, outputFile);
					success = false;
				}
			}

			if (TransformTemplates != null && !IsDesignTime) {
				AddReferencePaths (generator);
				success &= AddReferences (generator);

				if (!success) {
					return false;
				}

				foreach (var transform in TransformTemplates) {
					string inputFile = transform.ItemSpec;
					string outputFile = Path.ChangeExtension (inputFile, ".txt");
					var pt = LoadTemplate (inputFile, out var inputContent);
					TemplateSettings settings = TemplatingEngine.GetSettings (generator, pt);
					AddCoercedSessionParameters (generator, pt, parsedParameters);

					if (LogAndClear (pt.Errors, transform.ItemSpec)) {
						success = false;
						continue;
					}

					var outputContent = generator.ProcessTemplate (pt, inputFile, inputContent, ref outputFile, settings);

					if (LogAndClear (generator.Errors, inputFile)) {
						success = false;
						continue;
					}

					WriteOutput (outputFile, outputContent, settings.Encoding);
				}
			}

			if (PreprocessTemplates != null) {
				foreach (var preprocess in PreprocessTemplates) {
					string inputFile = preprocess.ItemSpec;

					string outputFile;

					if (UseLegacyPreprocessingMode) {
						outputFile = Path.ChangeExtension (inputFile, ".cs");
					} else {
						//FIXME: this could cause collisions. generate a path based on relative path and link metadata
						outputFile = Path.Combine (IntermediateDirectory, Path.ChangeExtension (inputFile, ".cs"));
					}

					var pt = LoadTemplate (inputFile, out var inputContent);
					TemplateSettings settings = TemplatingEngine.GetSettings (generator, pt);
					if (settings.Namespace == null) {
						settings.Namespace = DefaultNamespace;
					}

					if (LogAndClear (pt.Errors, preprocess.ItemSpec)) {
						success = false;
						continue;
					}

					//FIXME: escaping
					//FIXME: namespace name based on relative path and link metadata
					string preprocessClassName = Path.GetFileNameWithoutExtension (inputFile);

					var outputContent = generator.PreprocessTemplate (pt, inputFile, inputContent, preprocessClassName, settings);

					if (LogAndClear (generator.Errors, inputFile)) {
						success = false;
						continue;
					}

					WriteOutput (outputFile, outputContent, settings.Encoding);
				}
			}

			if (LogAndClear (generator.Errors, null)) {
				success = false;
			}

			//TODO
			//IntermediateDirectory
			//RequiredAssemblies
			//GeneratedTemplates
			//PreprocessedTemplates
			//settings.Debug
			//settings.Log
			//metadata to override output name, class name and namespace

			return success;
		}

		bool LogAndClear (CompilerErrorCollection errors, string file)
		{
			bool hasErrors = false;

			foreach (CompilerError err in errors) {
				if (err.IsWarning) {
					Log.LogWarning (null, err.ErrorNumber, null, err.FileName ?? file, err.Line, err.Column, 0, 0, err.ErrorText);
				} else {
					hasErrors = true;
					Log.LogError (null, err.ErrorNumber, null, err.FileName ?? file, err.Line, err.Column, 0, 0, err.ErrorText);
				}
			}

			errors.Clear ();

			return hasErrors;
		}

		void AddReferencePaths (MSBuildTemplateGenerator generator)
		{
			if (AssemblyReferences == null) {
				return;
			}

			foreach (var path in ReferencePaths) {
				generator.ReferencePaths.Add (path.ItemSpec);
			}
		}

		//pre-resolve the refs and add them to the standard refs, which themselves are already resolved
		//this means the templates don't all have to re-resolve them
		bool AddReferences (MSBuildTemplateGenerator generator)
		{
			if (AssemblyReferences == null) {
				return true;
			}

			bool success = true;
			var host = (ITextTemplatingEngineHost)generator;

			foreach (var reference in AssemblyReferences) {
				var resolved = host.ResolveAssemblyReference (reference.ItemSpec);
				if (resolved == null) {
					Log.LogError ("Could not resolve T4 assembly reference '{0}'", reference.ItemSpec);
					success = false;
				} else {
					generator.Refs.Add (reference.ItemSpec);
				}
			}
			return success;
		}

		void AddIncludePaths (MSBuildTemplateGenerator generator)
		{
			if (IncludePaths == null) {
				return;
			}

			foreach (var path in IncludePaths) {
				generator.IncludePaths.Add (path.ItemSpec);
			}
		}

		bool AddParameters (MSBuildTemplateGenerator generator, out Dictionary<string, string> parsedParameters)
		{
			bool success = true;

			parsedParameters = new Dictionary<string, string> ();

			if (ParameterValues == null) {
				return true;
			}

			foreach (var par in ParameterValues) {
				string paramName = par.ItemSpec;

				string paramVal = par.GetMetadata ("Value");
				string processorName, directiveName;

				if (!string.IsNullOrEmpty (paramVal)) {
					processorName = par.GetMetadata ("Processor");
					directiveName = par.GetMetadata ("Directive");
				}
				else if (!TemplateGenerator.TryParseParameter (paramName, out processorName, out directiveName, out paramName, out paramVal)) {
					Log.LogError ("Parameter does not have Value metadata or encoded value: {0}", par);
					success = false;
					continue;
				}

				generator.AddParameter (processorName, directiveName, paramName, paramVal);

				if (string.IsNullOrEmpty (directiveName) && string.IsNullOrEmpty (processorName)) {
					parsedParameters.Add (paramName, paramVal);
				}
			}

			return success;
		}

		static void AddCoercedSessionParameters (MSBuildTemplateGenerator generator, ParsedTemplate pt, Dictionary<string, string> properties)
		{
			if (properties.Count == 0) {
				return;
			}

			var session = generator.GetOrCreateSession ();

			foreach (var p in properties) {
				var directive = pt.Directives.FirstOrDefault (d =>
					d.Name == "parameter" &&
					d.Attributes.TryGetValue ("name", out string attVal) &&
					attVal == p.Key);

				if (directive != null) {
					directive.Attributes.TryGetValue ("type", out string typeName);
					var mappedType = ParameterDirectiveProcessor.MapTypeName (typeName);
					if (mappedType != "System.String") {
						if (ConvertType (mappedType, p.Value, out object converted)) {
							session[p.Key] = converted;
							continue;
						}

						pt.Errors.Add (
							new CompilerError (
								null, 0, 0, null,
								$"Could not convert property '{p.Key}'='{p.Value}' to parameter type '{typeName}'"
							)
						);
					}
				}
				session[p.Key] = p.Value;
			}
		}

		static bool ConvertType (string typeName, string value, out object converted)
		{
			converted = null;
			try {
				var type = Type.GetType (typeName);
				if (type == null) {
					return false;
				}
				Type stringType = typeof (string);
				if (type == stringType) {
					return true;
				}
				var converter = System.ComponentModel.TypeDescriptor.GetConverter (type);
				if (converter == null || !converter.CanConvertFrom (stringType)) {
					return false;
				}
				converted = converter.ConvertFromString (value);
				return true;
			}
			catch {
			}
			return false;
		}

		bool AddDirectiveProcessors (TemplateGenerator generator)
		{
			if (DirectiveProcessors == null) {
				return true;
			}

			bool hasErrors = false;

			foreach (var dirItem in DirectiveProcessors) {

				var name = dirItem.ItemSpec;
				var className = dirItem.GetMetadata ("Class");

				if (className != null) {
					var assembly = dirItem.GetMetadata ("Assembly") ?? dirItem.GetMetadata ("Codebase");
					if (string.IsNullOrEmpty (assembly)) {
						Log.LogError ("Directive '{0}' is missing 'Assembly' metadata", name);
						hasErrors = true;
					}
					generator.AddDirectiveProcessor (name, className, assembly);
					continue;
				}

				var split = name.Split ('!');
				if (split.Length != 3) {
					Log.LogError ("Directive must have 3 values: {0}", name);
					hasErrors = true;
					continue;
				}

				for (int i = 0; i < 3; i++) {
					string s = split[i];
					if (string.IsNullOrEmpty (s)) {
						string kind = i == 0 ? "name" : (i == 1 ? "class" : "assembly");
						Log.LogError ("Directive has missing {0} value: {1}", kind, name);
						hasErrors = true;
						continue;
					}
				}

				generator.AddDirectiveProcessor (split[0], split[1], split[2]);
			}

			return !hasErrors;
		}
	}
}
