// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using MessagePack;

namespace Mono.TextTemplating.Build
{
	[MessagePackObject (keyAsPropertyName: true)]
	class TemplateSessionInfo
	{
		public string DefaultNamespace { get; set; }
		public string IntermediateDirectory { get; set; }
		public List<string> IncludePaths { get; set; }
		public List<DirectiveProcessorInfo> DirectiveProcessors { get; set; }
		public List<string> AssemblyReferences { get; set; }
		public List<string> ReferencePaths { get; set; }
		public List<PreprocessedTemplateInfo> PreprocessTemplates { get; set; }
		public List<TransformTemplateInfo> TransformTemplates { get; set; }
		public List<ParameterInfo> Parameters { get; set; }

		internal (List<TransformTemplateInfo> transforms, List<PreprocessedTemplateInfo> preprocessed) GetStaleAndNewTemplates (TemplateSessionInfo previousSession, bool preprocessOnly, Func<string, DateTime> getFileWriteTime)
		{
			bool regenTransform, regenPreprocessed;

			if (previousSession == null) {
				regenTransform = regenPreprocessed = true;
			} else {
				(regenTransform, regenPreprocessed) = CompareSessions (previousSession, this);
			}

			List<TransformTemplateInfo> staleOrNewTransforms;
			if (preprocessOnly) {
				// if not transforming, re-use all transform values so they get cached appropriately
				TransformTemplates = previousSession?.TransformTemplates;
				staleOrNewTransforms = null;
			} else {
				if (regenTransform || TransformTemplates == null) {
					staleOrNewTransforms = TransformTemplates;
				} else {
					staleOrNewTransforms = new List<TransformTemplateInfo> ();
					var previousTransforms = previousSession.TransformTemplates.ToDictionary (t => t.InputFile);

					foreach (var t in TransformTemplates) {
						if (previousTransforms.TryGetValue (t.InputFile, out var pt) && !pt.IsStale (getFileWriteTime)) {
							// if it's up to date, use the values from the previous run
							t.Dependencies = pt.Dependencies;
							t.References = pt.Dependencies;
							t.OutputFile = pt.OutputFile;
						} else {
							staleOrNewTransforms.Add (t);
						}
					}
				}
			}

			List<PreprocessedTemplateInfo> staleOrNewPreprocessed;
			if (regenTransform || PreprocessTemplates == null) {
				staleOrNewPreprocessed = PreprocessTemplates;
			} else {
				staleOrNewPreprocessed = new List<PreprocessedTemplateInfo> ();
				var previousPreprocessed = previousSession.PreprocessTemplates.ToDictionary (t => t.InputFile);

				foreach (var t in PreprocessTemplates) {
					if (previousPreprocessed.TryGetValue (t.InputFile, out var pt) && !pt.IsStale (getFileWriteTime)) {
						// if it's up to date, use the values from the previous run
						t.Dependencies = pt.Dependencies;
						t.References = pt.Dependencies;
						t.OutputFile = pt.OutputFile;
					} else {
						staleOrNewPreprocessed.Add (t);
					}
				}
			}

			return (staleOrNewTransforms, staleOrNewPreprocessed);
		}

		// many of these comparisons could be case insensitive or order independent but let's keep it simple
		// minimizing incremental rebuild when case or order changes is not something we care about
		static (bool regenTransform, bool regenPreprocessed) CompareSessions (TemplateSessionInfo lastSession, TemplateSessionInfo session)
		{
			(bool, bool) regenAll = (true, true);
			(bool, bool) regenTransforms = (true, false);

			if (lastSession == null) {
				return regenAll;
			}

			if (lastSession.DefaultNamespace != session.DefaultNamespace) {
				return regenAll;
			}

			if (lastSession.IntermediateDirectory != session.IntermediateDirectory) {
				return regenAll;
			}

			if (!ListsEqual (lastSession.AssemblyReferences, session.AssemblyReferences)) {
				// references only affect transformed templates
				return regenTransforms;
			}

			if (!ListsEqual (lastSession.ReferencePaths, session.ReferencePaths)) {
				// however, reference paths may affect the "requiredreferences" of preprocessed templates as well
				return regenAll;
			}

			if (!ListsEqual (lastSession.IncludePaths, session.IncludePaths)) {
				return regenAll;
			}

			if (!ListsEqual (lastSession.DirectiveProcessors, session.DirectiveProcessors)) {
				return regenAll;
			}

			if (!ListsEqual (lastSession.Parameters, session.Parameters)) {
				// parameters can affect includes and references in precoressed templates
				return regenAll;
			}

			return (false, false);
		}

		static bool ListsEqual<T> (List<T> a, List<T> b) where T : IEquatable<T>
		{
			if (a == null && b == null) {
				return true;
			}

			if (a?.Count != b?.Count) {
				return false;
			}

			for (int i = 0; i < a.Count; i++) {
				if (a[i].Equals (b[i])) {
					return false;
				}
			}

			return true;
		}
	}

	[MessagePackObject (keyAsPropertyName: true)]
	class DirectiveProcessorInfo : IEquatable<DirectiveProcessorInfo>
	{
		public string Name { get; set; }
		public string Class { get; set; }
		public string Assembly { get; set; }

		public bool Equals (DirectiveProcessorInfo other)
			=> Name == other?.Name && Class == other.Name && Assembly == other?.Assembly;
	}

	[MessagePackObject (keyAsPropertyName: true)]
	class ParameterInfo : IEquatable<ParameterInfo>
	{
		public string Processor { get; set; }
		public string Directive { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }

		public bool Equals (ParameterInfo other)
			=> Processor == other?.Processor && Directive == other.Directive && Name == other?.Name && Value == other?.Value;
	}

	// TODO: cache warnings
	[MessagePackObject (keyAsPropertyName: true)]
	class TransformTemplateInfo
	{
		public string InputFile { get; set; }
		public string OutputFile { get; set; }
		public List<string> Dependencies { get; set; }
		public List<string> References { get; set; }

		public bool IsStale (Func<string,DateTime> getFileWriteTime)
		{
			var outputTime = getFileWriteTime (OutputFile);
			if (getFileWriteTime (InputFile) > outputTime) {
				return true;
			}
			foreach (var dep in Dependencies) {
				if (getFileWriteTime (dep) > outputTime) {
					return true;
				}
			}
			foreach (var reference in References) {
				if (getFileWriteTime (reference) > outputTime) {
					return true;
				}
			}
			return false;
		}
	}

	// TODO: cache warnings
	[MessagePackObject (keyAsPropertyName: true)]
	class PreprocessedTemplateInfo
	{
		public string InputFile { get; set; }
		public string OutputFile { get; set; }
		public List<string> Dependencies { get; set; }
		public List<string> References { get; set; }

		public bool IsStale (Func<string, DateTime> getFileWriteTime)
		{
			var outputTime = getFileWriteTime (OutputFile);
			if (getFileWriteTime (InputFile) > outputTime) {
				return true;
			}
			foreach (var dep in Dependencies) {
				if (getFileWriteTime (dep) > outputTime) {
					return true;
				}
			}
			// don't check references, for preprocessed templates they're not used by
			// the generator, they're just text values to be returned
			return false;
		}
	}
}
