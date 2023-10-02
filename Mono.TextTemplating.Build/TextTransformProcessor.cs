// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Build
{
	static class TextTransformProcessor
	{
		public static bool Process (TaskLoggingHelper taskLog, TemplateBuildState previousBuildState, TemplateBuildState buildState, bool preprocessOnly)
		{
			(var transforms, var preprocessed) = buildState.GetStaleAndNewTemplates (previousBuildState, preprocessOnly, new WriteTimeCache ().GetWriteTime, taskLog);

			if ((transforms == null || transforms.Count == 0) && (preprocessed == null || preprocessed.Count == 0)) {
				return true;
			}

			IEnumerable<(string templateFile, Action<MSBuildTemplateGenerator> generate)> GetTransformActions ()
			{
				if (transforms == null) {
					yield break;
				}

				var parameterMap = buildState.Parameters?.ToDictionary (p => p.Name, p => p.Value);

				foreach (var transform in transforms) {
					yield return (transform.InputFile, (generator) => {
						string inputFile = transform.InputFile;
						string outputFile = Path.ChangeExtension (inputFile, ".txt");
						var pt = LoadTemplate (generator, inputFile, out var inputContent);
						TemplateSettings settings = TemplatingEngine.GetSettings (generator, pt);

						if (parameterMap != null) {
							AddCoercedSessionParameters (generator, pt, parameterMap);
						}

						generator.Errors.AddRange (pt.Errors);
						if (generator.Errors.HasErrors) {
							return;
						}

						string outputContent;
						(outputFile, outputContent) = generator.ProcessTemplateAsync (pt, inputFile, inputContent, outputFile, settings).Result;

						if (generator.Errors.HasErrors) {
							return;
						}

						transform.OutputFile = outputFile;
						transform.Dependencies = new List<string> (generator.IncludedFiles);
						transform.Dependencies.AddRange (generator.CapturedReferences);

						WriteOutput (generator, outputFile, outputContent, settings.Encoding);
					}
					);
				};
			}

			IEnumerable<(string templateFile, Action<MSBuildTemplateGenerator> generate)> GetPreprocessedActions ()
			{
				if (preprocessed == null) {
					yield break;
				}

				foreach (var preprocess in preprocessed) {
					yield return (preprocess.InputFile, (generator) => {

						string inputFile = preprocess.InputFile;

						var pt = LoadTemplate (generator, inputFile, out var inputContent);
						TemplateSettings settings = TemplatingEngine.GetSettings (generator, pt);

						// FIXME: make these configurable, take relative path into account
						settings.Namespace = buildState.DefaultNamespace;
						settings.Name = Path.GetFileNameWithoutExtension (preprocess.InputFile);

						generator.Errors.AddRange (pt.Errors);
						if (generator.Errors.HasErrors) {
							return;
						}

						//FIXME: escaping
						//FIXME: namespace name based on relative path and link metadata
						string preprocessClassName = Path.GetFileNameWithoutExtension (inputFile);
						settings.Name = preprocessClassName;

						var outputContent = generator.PreprocessTemplate (pt, inputFile, inputContent, settings, out var references);

						if (generator.Errors.HasErrors) {
							return;
						}

						preprocess.Dependencies = new List<string> (generator.IncludedFiles);
						preprocess.References = new List<string> (references);

						WriteOutput (generator, preprocess.OutputFile, outputContent, settings.Encoding);
					}
					);
				}
			}

			var templateErrorSets = new ConcurrentQueue<(string filename, CompilerErrorCollection errors)> ();

			Parallel.ForEach (
				GetTransformActions ().Concat (GetPreprocessedActions ()),
				() => CreateGenerator (buildState),
				(action, state, generator) => {
					try {
						action.generate (generator);
					}
					catch (Exception ex) {
						generator.Errors.Add (new CompilerError (null, -1, -1, null, $"Internal error: {ex}"));
					}
					templateErrorSets.Enqueue ((action.templateFile, new CompilerErrorCollection (generator.Errors)));
					generator.Reset ();
					return generator;
				},
				(generator) => { }
			);

			bool hasErrors = false;

			foreach (var templateErrorGroup in templateErrorSets) {
				hasErrors |= LogErrors (taskLog, templateErrorGroup.filename, templateErrorGroup.errors);
			}

			return !hasErrors;
		}

		static bool LogErrors (TaskLoggingHelper taskLog, string filename, CompilerErrorCollection errors)
		{
			bool hasErrors = false;

			foreach (CompilerError err in errors) {
				if (err.IsWarning) {
					taskLog.LogWarning (null, err.ErrorNumber, null, err.FileName ?? filename, err.Line, err.Column, 0, 0, err.ErrorText);
				} else {
					hasErrors = true;
					taskLog.LogError (null, err.ErrorNumber, null, err.FileName ?? filename, err.Line, err.Column, 0, 0, err.ErrorText);
				}
			}

			return hasErrors;
		}

		static ParsedTemplate LoadTemplate (MSBuildTemplateGenerator generator, string filename, out string inputContent)
		{
			if (!File.Exists (filename)) {
				generator.Errors.Add (new CompilerError (filename, -1, -1, null, $"Template file '{filename}' does not exist"));
				inputContent = null;
				return null;
			}

			try {
				inputContent = File.ReadAllText (filename);
			}
			catch (IOException ex) {
				generator.Errors.Add (new CompilerError (filename, -1, -1, null, $"Internal error: {ex}"));
				inputContent = null;
				return null;
			}

			return generator.ParseTemplate (filename, inputContent);
		}

		static void WriteOutput (MSBuildTemplateGenerator generator, string outputFile, string outputContent, Encoding encoding)
		{
			try {
				File.WriteAllText (outputFile, outputContent, encoding ?? new UTF8Encoding (encoderShouldEmitUTF8Identifier: false));
			}
			catch (IOException ex) {
				generator.Errors.Add (new CompilerError (outputFile, -1, -1, null, $"Internal error: {ex}"));
			}
		}

		static MSBuildTemplateGenerator CreateGenerator (TemplateBuildState buildState)
		{
			var generator = new MSBuildTemplateGenerator ();
			if (buildState.ReferencePaths != null) {
				generator.ReferencePaths.AddRange (buildState.ReferencePaths);
			}
			if (buildState.AssemblyReferences != null) {
				generator.Refs.AddRange (buildState.AssemblyReferences);
			}
			if (buildState.IncludePaths != null) {
				generator.IncludePaths.AddRange (buildState.IncludePaths);
			}
			if (buildState.DirectiveProcessors != null) {
				foreach (var dp in buildState.DirectiveProcessors) {
					generator.AddDirectiveProcessor (dp.Name, dp.Class, dp.Assembly);
				}
			}
			if (buildState.Parameters != null) {
				foreach (var par in buildState.Parameters) {
					generator.AddParameter (par.Processor, par.Directive, par.Name, par.Value);
				}
			}

			return generator;
		}

		sealed class WriteTimeCache
		{
			public DateTime? GetWriteTime (string filepath)
			{
				if (!writeTimeCache.TryGetValue (filepath, out var value)) {
					writeTimeCache.Add (filepath, value = File.Exists(filepath)? File.GetLastWriteTime (filepath) : null);
				}
				return value;
			}
			readonly Dictionary<string, DateTime?> writeTimeCache = new ();
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
	}
}
