// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_APPDOMAINS

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Xunit;

namespace Mono.TextTemplating.Tests;

public class AppDomainTests : AssemblyLoadTests<SnapshotSet<string>>
{
	protected override TemplateGenerator CreateGenerator ([CallerMemberName] string testName = null) => CreateGeneratorWithAppDomain (testName: testName);
	protected override SnapshotSet<string> GetInitialState () => Snapshot.LoadedAssemblies ();
	protected override void VerifyFinalState (SnapshotSet<string> state)
	{
		(var added, var removed) = state.GetChanges ();
		added = added.Where (a => !a.StartsWith ("System.Configuration,", StringComparison.Ordinal));
		Assert.Empty (added);
		Assert.Empty (removed);
	}

	[Fact]
	public async Task BadAppDomain ()
	{
		var testDir = TestDataPath.Get (nameof (LoadOpenApiDll));
		var badGen = CreateGeneratorWithAppDomain (testDir, testDir);

		badGen.ReferencePaths.Add (PackagePath.Microsoft_OpenApi_1_2_3.Combine ("lib", "netstandard2.0"));

		var templateText = await testDir["LoadOpenApiDll.tt"].ReadAllTextNormalizedAsync ();

		await Assert.ThrowsAnyAsync<TemplatingEngineException> (() => badGen.ProcessTemplateAsync ("LoadOpenApiDll.tt", templateText, null));
	}

	static TestTemplateGeneratorWithAppDomain CreateGeneratorWithAppDomain (
		string basePath = null, string relativeSearchPath = null, bool shadowCopy = false,
		[CallerMemberName] string testName = null
		)
	{
		if (basePath is null) {
			// need to be able to resolve Mono.TextTemplating to load CompiledTemplate, which will resolve other assemblies via the host.
			basePath = AppDomain.CurrentDomain.BaseDirectory;
			if (relativeSearchPath is not null) {
				throw new ArgumentException ($"{nameof(relativeSearchPath)} must be null if {nameof(basePath)} is null", nameof (relativeSearchPath));
			}
			relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
		}
		return new (AppDomain.CreateDomain (
			$"Template Test - {testName ?? "(unknown)"}",
			null,
			basePath,
			relativeSearchPath,
			shadowCopy)
		);
	}

	class TestTemplateGeneratorWithAppDomain : TemplateGenerator
	{
		public TestTemplateGeneratorWithAppDomain (AppDomain appDomain) => AppDomain = appDomain;

		public AppDomain AppDomain { get; private set; }
		public override AppDomain ProvideTemplatingAppDomain (string content) => AppDomain;
	}
}

#endif