// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom.Compiler;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Mono.TextTemplating.Tests;

public abstract class AssemblyLoadTests<T> : StatefulTest<T>
{
	protected virtual TemplateGenerator CreateGenerator ([CallerMemberName] string testName = null) => new ();
	protected virtual void CleanupGenerator (TemplateGenerator generator) { }

	[Fact]
	public async Task LoadOpenApiDll ()
	{
		var testDir = TestDataPath.GetTestCase ();
		var gen = CreateGenerator ();
		gen.ReferencePaths.Add (PackagePath.Microsoft_OpenApi_1_2_3.Combine ("lib", "netstandard2.0").AssertDirectoryExists ());

		var templatePath = testDir["LoadOpenApiDll.tt"];
		var templateText = await templatePath.ReadAllTextNormalizedAsync ();

		var expectedOutputPath = testDir["LoadOpenApiDll.yaml"];
		var expectedOutputText = await expectedOutputPath.ReadAllTextNormalizedAsync ();

		var state = GetInitialState ();

		var result = await gen.ProcessTemplateAsync (templatePath, templateText, null);

		Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		Assert.Equal (expectedOutputText, result.content);
		Assert.Equal (expectedOutputPath, result.fileName);

		CleanupGenerator (gen);
		VerifyFinalState (state);
	}

	[Fact]
	public async Task LoadOpenApiReadersDll ()
	{
		var testDir = TestDataPath.GetTestCase (nameof (LoadOpenApiDll));
		var gen = CreateGenerator ();
		gen.ReferencePaths.Add (PackagePath.Microsoft_OpenApi_1_2_3.Combine ("lib", "netstandard2.0").AssertDirectoryExists ());
		gen.ReferencePaths.Add (PackagePath.Microsoft_OpenApi_Readers_1_2_3.Combine ("lib", "netstandard2.0").AssertDirectoryExists ());
		gen.ReferencePaths.Add (PackagePath.SharpYaml_1_6_5.Combine ("lib", "netstandard2.0").AssertDirectoryExists ());

		var templatePath = testDir["LoadOpenApiReaders.tt"];
		var templateText = await templatePath.ReadAllTextNormalizedAsync ();

		var state = GetInitialState ();

		var result = await gen.ProcessTemplateAsync (templatePath, templateText, null);

		Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		Assert.Equal ("Example", result.content);

		CleanupGenerator (gen);
		VerifyFinalState (state);
	}

	[FactExceptOnMono ("Mono incorrectly resolves the assembly if it has been loaded in a different AppDomain")]
	public async Task MissingTransitiveReference ()
	{
		var gen = CreateGenerator ();
		gen.ReferencePaths.Add (PackagePath.Microsoft_OpenApi_1_2_3.Combine ("lib", "netstandard2.0").AssertDirectoryExists ());
		gen.ReferencePaths.Add (PackagePath.Microsoft_OpenApi_Readers_1_2_3.Combine ("lib", "netstandard2.0").AssertDirectoryExists ());

		var testDir = TestDataPath.GetTestCase (nameof (LoadOpenApiDll));
		var templatePath = testDir["LoadOpenApiReaders.tt"];
		var templateText = await templatePath.ReadAllTextNormalizedAsync ();

		await gen.ProcessTemplateAsync (templatePath, templateText, null);

		var firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ()?.ErrorText;
		Assert.Contains ("FileNotFoundException: Could not load file or assembly 'SharpYaml, Version=1.6.5.0", firstError);

		CleanupGenerator (gen);
	}
}

public class FactExceptOnMonoAttribute : FactAttribute
{
	public FactExceptOnMonoAttribute (string reason)
	{
		if (IsRunningOnMono) {
			Skip = reason;
		}
	}

	public static bool IsRunningOnMono { get; } = System.Type.GetType ("Mono.Runtime") != null;
}