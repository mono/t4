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
	readonly string path;
	public TestDataPath (string path) => this.path = path;

	public static TestDataPath Get ([CallerMemberName] string testName = null)
		=> new (Path.Combine (Environment.CurrentDirectory, "TestCases", testName));

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

	public TestDataPath AssertFileExists ()
	{
		Assert.True (File.Exists (path), $"File '{path}' does not exists");
		return this;
	}

	public TestDataPath AssertDirectoryExists ()
	{
		Assert.True (Directory.Exists (path), $"Directory '{path}' does not exists");
		return this;
	}
}
