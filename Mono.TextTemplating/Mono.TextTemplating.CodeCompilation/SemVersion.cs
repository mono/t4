//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mono.TextTemplating.CodeCompilation
{
	public struct SemVersion : IComparable, IComparable<SemVersion>, IEquatable<SemVersion>
	{
        public static SemVersion Zero { get; } = new SemVersion(0,0,0, null, null, "0.0.0");

		static readonly Regex SemVerRegex = new (
			@"(?<Major>0|(?:[1-9]\d*))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))?(?:\-(?<PreRelease>[0-9A-Z\.-]+))?(?:\+(?<Meta>[0-9A-Z\.-]+))?)?",
			RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase
		);


		public int Major { get; }
		public int Minor { get; }
		public int Patch { get; }
		public string PreRelease { get; }
		public string Meta { get; }
        public bool IsPreRelease { get; }
        public bool HasMeta { get; }
        public string VersionString { get; }

        public SemVersion (int major, int minor, int patch, string preRelease = null, string meta = null) :
	        this (major, minor, patch, preRelease, meta, null)
        {
        }

        SemVersion (int major, int minor, int patch, string preRelease, string meta, string versionString)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
			IsPreRelease = !string.IsNullOrEmpty (preRelease);
			HasMeta = !string.IsNullOrEmpty (meta);
			PreRelease = IsPreRelease ? preRelease : null;
			Meta = HasMeta ? meta : null;

			if (!string.IsNullOrEmpty (versionString)) {
				VersionString = versionString;
			} else {
				var sb = new StringBuilder ();
				sb.AppendFormat (CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch);

				if (IsPreRelease) {
					sb.AppendFormat (CultureInfo.InvariantCulture, "-{0}", PreRelease);
				}

				if (HasMeta) {
					sb.AppendFormat (CultureInfo.InvariantCulture, "+{0}", Meta);
				}

				VersionString = sb.ToString ();
			}
		}

		public static bool TryParse (string version, out SemVersion semVersion)
		{
			semVersion = Zero;

			if (string.IsNullOrEmpty(version)) {
				return false;
			}

			var match = SemVerRegex.Match (version);
			if (!match.Success) {
				return false;
			}

			if (!int.TryParse (
				    match.Groups["Major"].Value,
				    NumberStyles.Integer,
				    CultureInfo.InvariantCulture,
				    out var major) ||
			    !int.TryParse (
				    match.Groups["Minor"].Value,
				    NumberStyles.Integer,
				    CultureInfo.InvariantCulture,
				    out var minor) ||
			    !int.TryParse (
				    match.Groups["Patch"].Value,
				    NumberStyles.Integer,
				    CultureInfo.InvariantCulture,
				    out var patch)) {
				return false;
			}

			semVersion = new SemVersion (
				major,
				minor,
				patch,
				match.Groups["PreRelease"]?.Value,
				match.Groups["Meta"]?.Value,
				version);

			return true;
		}

		

		public bool Equals (SemVersion other)
		{
			return Major == other.Major
			       && Minor == other.Minor
			       && Patch == other.Patch
			       && string.Equals(PreRelease, other.PreRelease, StringComparison.OrdinalIgnoreCase)
			       && string.Equals(Meta, other.Meta, StringComparison.OrdinalIgnoreCase);
		}

		public int CompareTo (SemVersion other)
		{
			if (Equals(other))
			{
				return 0;
			}

			if (Major > other.Major) {
				return 1;
			}

			if (Major < other.Major) {
				return -1;
			}

			if (Minor > other.Minor) {
				return 1;
			}

			if (Minor < other.Minor) {
				return -1;
			}

			if (Patch > other.Patch) {
				return 1;
			}

			if (Patch < other.Patch) {
				return -1;
			}

			return StringComparer.InvariantCultureIgnoreCase.Compare (PreRelease, other.PreRelease) switch {
				1 => 1,
				-1 => -1,
				_ => StringComparer.InvariantCultureIgnoreCase.Compare (Meta, other.Meta)
			};
		}

		public int CompareTo (object obj) => (obj is SemVersion semVersion)? CompareTo (semVersion) : -1;

		public override bool Equals (object obj) => (obj is SemVersion semVersion) && Equals (semVersion);

		public override int GetHashCode ()
		{
			unchecked {
				var hashCode = Major;
				hashCode = (hashCode * 397) ^ Minor;
				hashCode = (hashCode * 397) ^ Patch;
				hashCode = (hashCode * 397) ^ (PreRelease != null ? StringComparer.OrdinalIgnoreCase.GetHashCode (PreRelease) : 0);
				hashCode = (hashCode * 397) ^ (Meta != null ? StringComparer.OrdinalIgnoreCase.GetHashCode (Meta) : 0);
				return hashCode;
			}
		}

		public override string ToString () => VersionString;

		public static bool operator > (SemVersion left, SemVersion right) => left.CompareTo (right) == 1;
		public static bool operator < (SemVersion left, SemVersion right) => left.CompareTo (right) == -1;
		public static bool operator >= (SemVersion left, SemVersion right) => left.CompareTo (right) >= 0;
		public static bool operator <= (SemVersion left, SemVersion right) => left.CompareTo (right) <= 0;
		public static bool operator == (SemVersion left, SemVersion right) => left.Equals (right);
		public static bool operator != (SemVersion left, SemVersion right) => !(left == right);
	}
}