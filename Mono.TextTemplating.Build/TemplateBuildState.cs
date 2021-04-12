// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using MessagePack;

namespace Mono.TextTemplating.Build
{
	// messagepack requires this to be public
	[MessagePackObject]
	public class TemplateBuildState
	{
		public static int CURRENT_FORMAT_VERSION = 0;

		[Key (0)]
		public int FormatVersion { get; set; } = CURRENT_FORMAT_VERSION;
		[Key (1)]
		public string DefaultNamespace { get; set; }
		[Key (2)]
		public string IntermediateDirectory { get; set; }
		[Key (3)]
		public List<string> IncludePaths { get; set; }
		[Key (4)]
		public List<DirectiveProcessor> DirectiveProcessors { get; set; }
		[Key (5)]
		public List<string> AssemblyReferences { get; set; }
		[Key (6)]
		public List<string> ReferencePaths { get; set; }
		[Key (7)]
		public List<PreprocessedTemplate> PreprocessTemplates { get; set; }
		[Key (8)]
		public List<TransformTemplate> TransformTemplates { get; set; }
		[Key (9)]
		public List<Parameter> Parameters { get; set; }

		internal (List<TransformTemplate> transforms, List<PreprocessedTemplate> preprocessed) GetStaleAndNewTemplates (
			TemplateBuildState previousBuildState, bool preprocessOnly, Func<string, DateTime> getFileWriteTime
			)
		{
			bool regenTransform, regenPreprocessed;

			if (previousBuildState == null) {
				regenTransform = regenPreprocessed = true;
			} else {
				(regenTransform, regenPreprocessed) = CompareSessions (previousBuildState, this);
			}

			List<TransformTemplate> staleOrNewTransforms;
			if (preprocessOnly) {
				// if not transforming, re-use all transform values so they get cached appropriately
				TransformTemplates = previousBuildState?.TransformTemplates;
				staleOrNewTransforms = null;
			} else {
				if (regenTransform || TransformTemplates == null) {
					staleOrNewTransforms = TransformTemplates;
				} else {
					staleOrNewTransforms = new List<TransformTemplate> ();
					var previousTransforms = previousBuildState.TransformTemplates.ToDictionary (t => t.InputFile);

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

			List<PreprocessedTemplate> staleOrNewPreprocessed;
			if (regenTransform || PreprocessTemplates == null) {
				staleOrNewPreprocessed = PreprocessTemplates;
			} else {
				staleOrNewPreprocessed = new List<PreprocessedTemplate> ();
				var previousPreprocessed = previousBuildState.PreprocessTemplates.ToDictionary (t => t.InputFile);

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
		static (bool regenTransform, bool regenPreprocessed) CompareSessions (TemplateBuildState lastSession, TemplateBuildState session)
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

		[MessagePackObject]
		public class DirectiveProcessor : IEquatable<DirectiveProcessor>
		{
			[Key (0)]
			public string Name { get; set; }
			[Key (1)]
			public string Class { get; set; }
			[Key (2)]
			public string Assembly { get; set; }

			public bool Equals (DirectiveProcessor other)
				=> Name == other?.Name && Class == other.Name && Assembly == other?.Assembly;
		}

		[MessagePackObject]
		public class Parameter : IEquatable<Parameter>
		{
			[Key (0)]
			public string Processor { get; set; }
			[Key (1)]
			public string Directive { get; set; }
			[Key (2)]
			public string Name { get; set; }
			[Key (3)]
			public string Value { get; set; }

			public bool Equals (Parameter other)
				=> Processor == other?.Processor && Directive == other.Directive && Name == other?.Name && Value == other?.Value;
		}

		// TODO: cache warnings
		[MessagePackObject]
		public class TransformTemplate
		{
			[Key (0)]
			public string InputFile { get; set; }
			[Key (1)]
			public string OutputFile { get; set; }
			[Key (2)]
			public List<string> Dependencies { get; set; }
			[Key (3)]
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
				foreach (var reference in References) {
					if (getFileWriteTime (reference) > outputTime) {
						return true;
					}
				}
				return false;
			}
		}

		// TODO: cache warnings
		[MessagePackObject]
		public class PreprocessedTemplate
		{
			[Key (0)]
			public string InputFile { get; set; }
			[Key (1)]
			public string OutputFile { get; set; }
			[Key (2)]
			public List<string> Dependencies { get; set; }
			[Key (3)]
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
}
