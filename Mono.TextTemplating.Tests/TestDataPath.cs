// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Mono.TextTemplating.Tests;

struct TestDataPath
{
	public static TestDataPath ProjectRoot { get; } = new TestDataPath (Path.GetFullPath (Path.Combine ("..", "..", "..")));
	public static TestDataPath TestOutputRoot { get; } = new TestDataPath (Path.GetFullPath ("test-cases"));
	public static TestDataPath TestCasesRoot { get; } = ProjectRoot.Combine ("TestCases");

	public static TestDataPath CreateOutputDir ([CallerMemberName] string testName = null)
	{
		var outputDir = TestOutputRoot.Combine (testName ?? throw new ArgumentNullException (nameof (testName)));

		outputDir.DeleteIfExists ();
		Directory.CreateDirectory (outputDir);

		return outputDir;
	}

	public static TestDataPath GetTestCase ([CallerMemberName] string testName = null)
		=> TestCasesRoot.Combine (testName ?? throw new ArgumentNullException (nameof (testName)));

	readonly string path;
	public TestDataPath (string path) => this.path = TrimEndingDirectorySeparator (path);

	public TestDataPath Combine (string path) => new (Path.Combine (this.path, path));
	public TestDataPath Combine (string path1, string path2) => new (Path.Combine (path, path1, path2));
	public TestDataPath Combine (string path1, string path2, string path3) => new (Path.Combine (path, path1, path2, path3));
	public TestDataPath Combine (params string[] paths) => new (Path.Combine (path, Path.Combine (paths)));

	public TestDataPath this[string path] => Combine (path);
	public TestDataPath this[string path1, string path2] => Combine (path1, path2);
	public TestDataPath this[string path1, string path2, string path3] => Combine (path1, path2, path3);
	public TestDataPath this[params string[] paths] => Combine (paths);

	public static implicit operator string (TestDataPath path) => path.path;

#if NETCOREAPP2_0_OR_GREATER
	public Task<string> ReadAllTextAsync () => File.ReadAllTextAsync (path);
	public async Task<string> ReadAllTextNormalizedAsync () => (await ReadAllTextAsync ().ConfigureAwait (false)).NormalizeNewlines ();
#else
	public Task<string> ReadAllTextAsync () => Task.FromResult (ReadAllText ());
	public Task<string> ReadAllTextNormalizedAsync () => Task.FromResult (ReadAllText ().NormalizeNewlines ());
#endif

	public string ReadAllText () => File.ReadAllText (path);
	public string ReadAllTextNormalized () => File.ReadAllText (path).NormalizeNewlines ();

	public TestDataPath AssertFileExists (bool exists = true)
	{
		if (exists) {
			Assert.True (File.Exists (path), $"File '{path}' does not exist");
		} else {
			Assert.False (File.Exists (path), $"File '{path}' should not exist");
		}
		return this;
	}

	public TestDataPath AssertDirectoryExists (bool exists = true)
	{
		if (exists) {
			Assert.True (Directory.Exists (path), $"Directory '{path}' does not exist");
		} else {
			Assert.False (Directory.Exists (path), $"Directory '{path}' should not exist");
		}
		return this;
	}

	/// <summary>
	/// Assert that the file exists and starts with the given value
	/// </summary>
	public TestDataPath AssertTextStartsWith (string value, StringComparison comparison = StringComparison.Ordinal)
	{
		AssertFileExists ();
		var text = File.ReadAllText (path);
		Assert.StartsWith (value, text, comparison);
		return this;
	}

	static string TrimEndingDirectorySeparator (string path)
#if NETCOREAPP3_0_OR_GREATER
		=> Path.TrimEndingDirectorySeparator (path);
#else
	{
		var trimmed = path.TrimEnd (Path.DirectorySeparatorChar);
		if (trimmed.Length == path.Length && Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar) {
			return trimmed.TrimEnd (Path.DirectorySeparatorChar);
		}
		return trimmed;
	}
#endif

	public void CopyDirectoryTo (string dest) => CopyDirectory (new DirectoryInfo (path), new DirectoryInfo (dest));

	static void CopyDirectory (DirectoryInfo src, DirectoryInfo dest)
	{
		dest.Create ();

		foreach (var fsi in src.EnumerateFileSystemInfos ()) {
			if (fsi is DirectoryInfo d) {
				CopyDirectory (d, new DirectoryInfo (Path.Combine (dest.FullName, d.Name)));
			} else {
				var f = (FileInfo)fsi;
				File.Copy (f.FullName, Path.Combine (dest.FullName, f.Name));
			}
		}
	}

	public bool DeleteIfExists ()
	{
		if (Directory.Exists (path)) {
			Directory.Delete (path, true);
			return true;
		}

		if (File.Exists (path)) {
			File.Delete (path);
			return true;
		}

		return false;
	}

	public TestDataPath AssertFileName (string expectedName)
	{
		Assert.Equal (expectedName, Path.GetFileName (path));
		return this;
	}

	public DateTime GetLastWriteTime () => File.GetLastWriteTime (path);

	public void AssertWriteTimeEquals (DateTime expected) => Assert.Equal (expected, GetLastWriteTime ());

	public DateTime AssertWriteTimeNewerThan (DateTime previousWriteTime)
	{
		var newWriteTime = GetLastWriteTime ();
		Assert.True (newWriteTime > previousWriteTime);
		return newWriteTime;
	}
}

sealed class WriteTimeTracker
{
	readonly TestDataPath file;
	DateTime lastWriteTime;
	public WriteTimeTracker (TestDataPath file) => lastWriteTime = (this.file = file).GetLastWriteTime ();
	public void AssertChanged () => lastWriteTime = file.AssertWriteTimeNewerThan (lastWriteTime);
	public void AssertSame () => file.AssertWriteTimeEquals (lastWriteTime);
}

static class StringNormalizationExtensions
{
	public static string NormalizeNewlines (this string s, string newLine = "\n") => s.Replace ("\r\n", "\n").Replace ("\n", newLine);

	public static string NormalizeEscapedNewlines (this string s, string escapedNewline = "\\n") => s.Replace ("\\r\\n", "\\n").Replace ("\\n", escapedNewline);
}
