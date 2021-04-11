// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MessagePack;

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

		public bool PreprocessOnly { get; set; }
		public bool UseLegacyPreprocessingMode { get; set; }

		[Required]
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

			Directory.CreateDirectory (IntermediateDirectory);

			var lastSession = LoadSession (IntermediateDirectory);

			var session = new TemplateSessionInfo {
				IntermediateDirectory = IntermediateDirectory,
				DefaultNamespace = DefaultNamespace
			};

			success &= AddParameters (session);
			success &= AddDirectiveProcessors (session);

			if (!success) {
				return false;
			}


			if (IncludePaths != null) {
				session.IncludePaths = new List<string> (IncludePaths.Select (i => i.ItemSpec));
			}

			if (ReferencePaths != null) {
				session.ReferencePaths = new List<string> (ReferencePaths.Select (i => i.ItemSpec));
			}

			if (AssemblyReferences != null) {
				session.AssemblyReferences = new List<string> (AssemblyReferences.Select (i => i.ItemSpec));
			}

			if (PreprocessTemplates != null) {
				session.PreprocessTemplates = new List<PreprocessedTemplateInfo> ();
				foreach (var ppt in PreprocessTemplates) {
					string inputFile = ppt.ItemSpec;
					string outputFile;
					if (UseLegacyPreprocessingMode) {
						outputFile = Path.ChangeExtension (inputFile, ".cs");
					} else {
						//FIXME: this could cause collisions. generate a path based on relative path and link metadata
						outputFile = Path.Combine (IntermediateDirectory, Path.ChangeExtension (inputFile, ".cs"));
					}
					session.PreprocessTemplates.Add (new PreprocessedTemplateInfo {
						InputFile = inputFile,
						OutputFile = outputFile
					});
				}
			}

			if (TransformTemplates != null) {
				session.TransformTemplates = new List<TransformTemplateInfo> ();
				foreach (var tt in TransformTemplates) {
					string inputFile = tt.ItemSpec;
					string outputFile = Path.ChangeExtension (inputFile, ".txt");
					session.TransformTemplates.Add (new TransformTemplateInfo {
						InputFile = inputFile,
						OutputFile = outputFile
					});
				}
			}

			var processor = new TextTransformProcessor (Log);
			processor.Process (lastSession, session, PreprocessOnly);

			if (session.TransformTemplates != null) {
				TransformTemplateOutput = new ITaskItem[session.TransformTemplates.Count];
				for (int i = 0; i < session.TransformTemplates.Count; i++) {
					TransformTemplateOutput[i] = new TaskItem (session.TransformTemplates[i].OutputFile);
				}
			}

			if (session.PreprocessTemplates != null) {
				PreprocessedTemplateOutput = new ITaskItem[session.PreprocessTemplates.Count];
				for (int i = 0; i < session.PreprocessTemplates.Count; i++) {
					PreprocessedTemplateOutput[i] = new TaskItem (session.PreprocessTemplates[i].OutputFile);
				}
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

		bool AddParameters (TemplateSessionInfo sessionInfo)
		{
			bool success = true;

			if (ParameterValues == null) {
				return true;
			}

			sessionInfo.Parameters = new List<ParameterInfo> ();

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

				sessionInfo.Parameters.Add (new ParameterInfo {
					Processor = processorName,
					Directive = directiveName,
					Name = paramName,
					Value = paramVal
				});
			}

			return success;
		}

		bool AddDirectiveProcessors (TemplateSessionInfo sessionInfo)
		{
			if (DirectiveProcessors == null) {
				return true;
			}

			sessionInfo.DirectiveProcessors = new List<DirectiveProcessorInfo> ();

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

					sessionInfo.DirectiveProcessors.Add (new DirectiveProcessorInfo {
						Name = name,
						Class = className,
						Assembly = assembly
					});
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

				sessionInfo.DirectiveProcessors.Add (new DirectiveProcessorInfo {
					Name = split[0],
					Class = split[1],
					Assembly = split[2]
				});
			}

			return !hasErrors;
		}

		TemplateSessionInfo LoadSession (string path)
		{
			return null;
		}
	}
}
