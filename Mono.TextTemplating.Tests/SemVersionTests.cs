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

using System.Linq;
using Mono.TextTemplating.CodeCompilation;
using NUnit.Framework;

namespace Mono.TextTemplating.Tests
{
	[TestFixture]
	public class SemVersionTests
	{
		[TestCase ("2.1.801", 2, 1, 801, null, null)]
		[TestCase ("2.1.12", 2, 1, 12, null, null)]
		[TestCase ("2.1.700", 2, 1, 700, null, null)]
		[TestCase ("2.1.604", 2, 1, 604, null, null)]
		[TestCase ("2.1.11", 2, 1, 11, null, null)]
		[TestCase ("2.1.603", 2, 1, 603, null, null)]
		[TestCase ("2.1.10", 2, 1, 10, null, null)]
		[TestCase ("2.1.602", 2, 1, 602, null, null)]
		[TestCase ("2.1.9", 2, 1, 9, null, null)]
		[TestCase ("2.1.8", 2, 1, 8, null, null)]
		[TestCase ("2.1.7", 2, 1, 7, null, null)]
		[TestCase ("2.1.502", 2, 1, 502, null, null)]
		[TestCase ("2.1.600-preview", 2, 1, 600, "preview", null)]
		[TestCase ("2.1.6", 2, 1, 6, null, null)]
		[TestCase ("2.1.5", 2, 1, 5, null, null)]
		[TestCase ("2.1.4", 2, 1, 4, null, null)]
		[TestCase ("2.1.3", 2, 1, 3, null, null)]
		[TestCase ("2.1.2", 2, 1, 2, null, null)]
		[TestCase ("2.1.1", 2, 1, 1, null, null)]
		[TestCase ("2.1.0", 2, 1, 0, null, null)]
		[TestCase ("2.1.0-rc1", 2, 1, 0, "rc1", null)]
		[TestCase ("2.1.0-preview2", 2, 1, 0, "preview2", null)]
		[TestCase ("2.1.0-preview1", 2, 1, 0, "preview1", null)]
		[TestCase ("3.0.0-preview9", 3, 0, 0, "preview9", null)]
		[TestCase ("3.0.0-preview8+build.1", 3, 0, 0, "preview8", "build.1")]
        [TestCase ("3.0.0-preview8+build.2", 3, 0, 0, "preview8", "build.2")]
		[TestCase ("3.0.0-preview7", 3, 0, 0, "preview7", null)]
		[TestCase ("3.0.0-preview6", 3, 0, 0, "preview6", null)]
		[TestCase ("3.0.0-preview5", 3, 0, 0, "preview5", null)]
		[TestCase ("3.0.0-preview4", 3, 0, 0, "preview4", null)]
		[TestCase ("3.0.0-preview3", 3, 0, 0, "preview3", null)]
		[TestCase ("3.0.0-preview2", 3, 0, 0, "preview2", null)]
		[TestCase ("3.0.0-preview1", 3, 0, 0, "preview1", null)]
		public void TryParse (string version, int expectMajor, int expectMinor, int expectPatch, string expectPreRelease, string expectedMeta)
		{
			var parsed = SemVersion.TryParse (version, out SemVersion semVersion);
            Assert.True(parsed);
			Assert.AreEqual(version, semVersion.VersionString, nameof(semVersion.VersionString));
			Assert.AreEqual (expectMajor, semVersion.Major, nameof (semVersion.Major));
			Assert.AreEqual (expectMinor, semVersion.Minor, nameof (semVersion.Minor));
			Assert.AreEqual (expectPatch, semVersion.Patch, nameof (semVersion.Patch));
			Assert.AreEqual (expectPreRelease, semVersion.PreRelease, nameof (semVersion.PreRelease));
			Assert.AreEqual (expectedMeta, semVersion.Meta, nameof (semVersion.Meta));
		}

		[Test]
		public void Compare()
		{
			var given = new[] {
				"3.0.0-preview9",
				"3.0.0-preview8",
				"3.0.0-preview7",
				"3.0.0-preview6",
				"3.0.0-preview5",
				"3.0.0-preview4",
				"3.0.0-preview3",
				"3.0.0-preview2",
				"3.0.0-preview1+build.4",
				"3.0.0-preview1+build.3",
				"3.0.0-preview1+build.2",
				"3.0.0-preview1+build.1",
				"3.0.0-preview1",
				"2.2.6",
				"2.2.300",
				"2.2.204",
				"2.2.5",
				"2.2.203",
				"2.2.4",
				"2.2.202",
				"2.2.3",
				"2.2.2",
				"2.2.103",
				"2.2.1",
				"2.2.101",
				"2.2.200-preview"
			};

			var expect = new[] {
	            "2.2.1",
	            "2.2.2",
	            "2.2.3",
	            "2.2.4",
	            "2.2.5",
	            "2.2.6",
	            "2.2.101",
	            "2.2.103",
	            "2.2.200-preview",
	            "2.2.202",
	            "2.2.203",
	            "2.2.204",
	            "2.2.300",
	            "3.0.0-preview1",
	            "3.0.0-preview1+build.1",
	            "3.0.0-preview1+build.2",
	            "3.0.0-preview1+build.3",
	            "3.0.0-preview1+build.4",
	            "3.0.0-preview2",
	            "3.0.0-preview3",
	            "3.0.0-preview4",
	            "3.0.0-preview5",
	            "3.0.0-preview6",
	            "3.0.0-preview7",
	            "3.0.0-preview8",
	            "3.0.0-preview9"
			};

			var actual = given
				.Select (
					version => SemVersion.TryParse (version, out var semVersion) ? semVersion : SemVersion.Zero)
				.OrderBy (semVersion => semVersion)
				.Select(semVersion => semVersion.VersionString)
				.ToArray ();

			Assert.AreEqual (expect, actual);
		}
	}
}
