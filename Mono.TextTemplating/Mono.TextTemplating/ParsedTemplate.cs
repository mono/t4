//
// Template.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	public class ParsedTemplate
	{
		readonly List<ISegment> importedHelperSegments = new ();
		readonly string rootFileName;

		public ParsedTemplate (string rootFileName)
		{
			this.rootFileName = rootFileName;
		}

		public List<ISegment> RawSegments { get; } = new List<ISegment> ();

		public IEnumerable<Directive> Directives {
			get {
				foreach (ISegment seg in RawSegments) {
					if (seg is Directive dir)
						yield return dir;
				}
			}
		}

		public IEnumerable<TemplateSegment> Content {
			get {
				foreach (ISegment seg in RawSegments) {
					if (seg is TemplateSegment ts)
						yield return ts;
				}
			}
		}

		public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection ();

		// this is deprecated to prevent accidentally passing a host without the TemplateFile property set
		[Obsolete("Use TemplateGenerator.ParseTemplate")]
		public static ParsedTemplate FromText (string content, ITextTemplatingEngineHost host) => FromText (content, host);

		internal static ParsedTemplate FromTextInternal (string content, ITextTemplatingEngineHost host)
		{
			var filePath = host.TemplateFile;
			var template = new ParsedTemplate (filePath);
			try {
				template.Parse (host, new HashSet<string> (StringComparer.OrdinalIgnoreCase), new Tokeniser (filePath, content), true);
			} catch (ParserException ex) {
				template.LogError (ex.Message, ex.Location);
			}
			return template;
		}

		// this is deprecated to prevent accidentally passing a host without the TemplateFile property set
		[Obsolete("Use TemplateGenerator.ParseTemplate")]
		public void Parse (ITextTemplatingEngineHost host, Tokeniser tokeniser) => Parse (host, new HashSet<string>(StringComparer.OrdinalIgnoreCase), tokeniser, true);

		[Obsolete("Should not have been public")]
		public void ParseWithoutIncludes (Tokeniser tokeniser) => Parse (null, null, tokeniser, false);

		void Parse (ITextTemplatingEngineHost host, HashSet<string> includedFiles, Tokeniser tokeniser, bool parseIncludes) => Parse (host, includedFiles, tokeniser, parseIncludes, false);

		void Parse (ITextTemplatingEngineHost host, HashSet<string> includedFiles, Tokeniser tokeniser, bool parseIncludes, bool isImport)
		{
			bool skip = false;
			bool addToImportedHelpers = false;
			while ((skip || tokeniser.Advance ()) && tokeniser.State != State.EOF) {
				skip = false;
				ISegment seg = null;
				switch (tokeniser.State) {
				case State.Block:
					if (!string.IsNullOrEmpty (tokeniser.Value))
						seg = new TemplateSegment (SegmentType.Block, tokeniser.Value, tokeniser.Location);
					break;
				case State.Content:
					if (!string.IsNullOrEmpty (tokeniser.Value))
						seg = new TemplateSegment (SegmentType.Content, tokeniser.Value, tokeniser.Location);
					break;
				case State.Expression:
					if (!string.IsNullOrEmpty (tokeniser.Value))
						seg = new TemplateSegment (SegmentType.Expression, tokeniser.Value, tokeniser.Location);
					break;
				case State.Helper:
					addToImportedHelpers = isImport;
					if (!string.IsNullOrEmpty (tokeniser.Value))
						seg = new TemplateSegment (SegmentType.Helper, tokeniser.Value, tokeniser.Location);
					break;
				case State.Directive:
					Directive directive = null;
					string attName = null;
					while (!skip && tokeniser.Advance ()) {
						switch (tokeniser.State) {
						case State.DirectiveName:
							if (directive == null) {
								directive = new Directive (tokeniser.Value, tokeniser.Location) {
									TagStartLocation = tokeniser.TagStartLocation
								};
								if (!parseIncludes || !string.Equals (directive.Name, "include", StringComparison.OrdinalIgnoreCase))
									RawSegments.Add (directive);
							} else
								attName = tokeniser.Value;
							break;
						case State.DirectiveValue:
							if (attName != null && directive != null)
								directive.Attributes[attName] = tokeniser.Value;
							else
								LogError ("Directive value without name", tokeniser.Location);
							attName = null;
							break;
						case State.Directive:
							if (directive != null)
								directive.EndLocation = tokeniser.TagEndLocation;
							break;
						default:
							skip = true;
							break;
						}
					}
					if (parseIncludes && directive != null && string.Equals (directive.Name, "include", StringComparison.OrdinalIgnoreCase))
						Import (host, includedFiles, directive, Path.GetDirectoryName (tokeniser.Location.FileName));
					break;
				default:
					throw new InvalidOperationException ();
				}
				if (seg != null) {
					seg.TagStartLocation = tokeniser.TagStartLocation;
					seg.EndLocation = tokeniser.TagEndLocation;
					if (addToImportedHelpers)
						importedHelperSegments.Add (seg);
					else
						RawSegments.Add (seg);
				}
			}
			if (!isImport) {
				AppendAnyImportedHelperSegments ();
			}
		}

		static string FixWindowsPath (string path) => Path.DirectorySeparatorChar == '/'? path.Replace('\\', '/') : path;

		void Import (ITextTemplatingEngineHost host, HashSet<string> includedFiles, Directive includeDirective, string relativeToDirectory)
		{
			if (!includeDirective.Attributes.TryGetValue ("file", out string rawFilename)) {
				LogError ("Include directive has no file attribute", includeDirective.StartLocation);
				return;
			}

			string fileName = FixWindowsPath (rawFilename);

			bool once = false;
			if (includeDirective.Attributes.TryGetValue ("once", out var onceStr)) {
				if (!bool.TryParse (onceStr, out once)) {
					LogError ($"Include once attribute has unknown value '{onceStr}'", includeDirective.StartLocation);
				}
			}

			//try to resolve path relative to the file that included it
			if (relativeToDirectory != null && !Path.IsPathRooted (fileName)) {
				string possible = Path.Combine (relativeToDirectory, fileName);
				if (File.Exists (possible)) {
					fileName = Path.GetFullPath (possible);
				}
			}

			if (host.LoadIncludeText (fileName, out string content, out string resolvedName)) {
				// unfortunately we can't use the once check to avoid actually reading the file
				// as the host resolves the filename and reads the file in a single call
				if (!includedFiles.Add (resolvedName) && once) {
					return;
				}
				Parse (host, includedFiles, new Tokeniser (resolvedName, content), true, true);
			} else {
				LogError ($"Could not resolve include file '{rawFilename}'.", includeDirective.StartLocation);
			}
		}

		void AppendAnyImportedHelperSegments ()
		{
			RawSegments.AddRange (importedHelperSegments);
			importedHelperSegments.Clear ();
		}

		void LogError (string message, Location location, bool isWarning)
		{
			var err = new CompilerError {
				ErrorText = message
			};
			if (location.FileName != null) {
				err.Line = location.Line;
				err.Column = location.Column;
				err.FileName = location.FileName ?? string.Empty;
			} else {
				err.FileName = rootFileName ?? string.Empty;
			}
			err.IsWarning = isWarning;
			Errors.Add (err);
		}

		public void LogError (string message) => LogError (message, Location.Empty, false);

		public void LogWarning (string message) => LogError (message, Location.Empty, true);

		public void LogError (string message, Location location) => LogError (message, location, false);

		public void LogWarning (string message, Location location) => LogError (message, location, true);
	}

	public interface ISegment
	{
		Location StartLocation { get; }
		Location EndLocation { get; set; }
		Location TagStartLocation {get; set; }
	}

	public class TemplateSegment : ISegment
	{
		public TemplateSegment (SegmentType type, string text, Location start)
		{
			Type = type;
			StartLocation = start;
			Text = text;
		}

		public SegmentType Type { get; private set; }
		public string Text { get; private set; }
		public Location TagStartLocation { get; set; }
		public Location StartLocation { get; private set; }
		public Location EndLocation { get; set; }
	}

	public class Directive : ISegment
	{
		public Directive (string name, Location start)
		{
			Name = name;
			Attributes = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			StartLocation = start;
		}

		public string Name { get; private set; }
		public Dictionary<string,string> Attributes { get; private set; }
		public Location TagStartLocation { get; set; }
		public Location StartLocation { get; private set; }
		public Location EndLocation { get; set; }

		public string Extract (string key)
		{
			if (!Attributes.TryGetValue (key, out var value))
				return null;
			Attributes.Remove (key);
			return value;
		}
	}

	public enum SegmentType
	{
		Block,
		Expression,
		Content,
		Helper
	}

	public struct Location : IEquatable<Location>
	{
		public Location (string fileName, int line, int column) : this()
		{
			FileName = fileName;
			Column = column;
			Line = line;
		}

		public int Line { get; private set; }
		public int Column { get; private set; }
		public string FileName { get; private set; }

		public static Location Empty => new (null, -1, -1);

		public Location AddLine () => new (FileName, Line + 1, 1);

		public Location AddCol () => AddCols (1);

		public Location AddCols (int number) => new (FileName, Line, Column + number);

		public override string ToString () => $"[{FileName} ({Line},{Column})]";

		public bool Equals (Location other)
			=> other.Line == Line && other.Column == Column && other.FileName == FileName;

		public override bool Equals (object obj) => obj is Location loc && Equals (loc);

		public override int GetHashCode () => (FileName, Line, Column).GetHashCode ();

		public static bool operator == (Location left, Location right) => left.Equals (right);

		public static bool operator != (Location left, Location right) => !(left == right);
	}
}
