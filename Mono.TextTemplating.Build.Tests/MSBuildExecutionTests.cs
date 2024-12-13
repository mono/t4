// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;

namespace Mono.TextTemplating.Tests
{
	// MSBuild relies on changing the current working directory to the project directory so we need to run tests serially
	[CollectionDefinition (nameof (MSBuildExecutionTests), DisableParallelization = true)]
	public class MSBuildExecutionTests : IClassFixture<MSBuildFixture>
	{
		[Theory]
		[InlineData ("TransformTemplates", "foo.txt", "Hello 2019!")]
		[InlineData ("TransformTemplateFromRelativePath", "Nested/Template/Folder/foo.txt", "Hello 2024!")]
		[InlineData ("TransformTemplateWithExtension", "foo.html", "<h1>Hello 2024!</h1>")]
		public void TransformExplicitWithArguments (string projectName, string expectedFilePath, string expectedText)
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject (projectName);

			var instance = project.Build ("TransformTemplates");

			var generated = project.DirectoryPath[expectedFilePath].AssertTextStartsWith (expectedText);

			instance.AssertSingleItem ("GeneratedTemplates", withFullPath: generated);
			instance.AssertNoItems ("PreprocessedTemplates");
		}

		[Theory]
		[InlineData ("TransformTemplates", "foo.txt", "Hello 2019!")]
		[InlineData ("TransformTemplateFromRelativePath", "Nested/Template/Folder/foo.txt", "Hello 2024!")]
		[InlineData ("TransformTemplateWithExtension", "foo.html", "<h1>Hello 2024!</h1>")]
		public void TransformOnBuild (string projectName, string expectedFilePath, string expectedText)
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject (projectName)
				.WithProperty ("TransformOnBuild", "true");

			project.Restore ();

			var instance = project.Build ("Build");

			var generatedFilePath = project.DirectoryPath[expectedFilePath].AssertTextStartsWith (expectedText);

			instance.AssertSingleItem ("GeneratedTemplates", withFullPath: generatedFilePath);
			instance.AssertNoItems ("PreprocessedTemplates");
		}

		[Theory]
		[InlineData ("TransformTemplates", "foo.txt")]
		[InlineData ("TransformTemplateFromRelativePath", "Nested/Template/Folder/foo.txt")]
		[InlineData ("TransformTemplateWithExtension", "foo.html")]
		public void TransformOnBuildDisabled (string projectName, string expectedFilePath)
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject (projectName);

			project.Restore ();

			var instance = project.Build ("Build");

			project.DirectoryPath[expectedFilePath].AssertFileExists (false);

			instance.AssertNoItems ("GeneratedTemplates", "PreprocessedTemplates");
		}

		[Fact]
		public void TransformMetadata ()
		{
			// Arrange
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("TransformTemplateMetadata");

			var outputDirectory = project.DirectoryPath["Demo/Output/OutputDirectory.txt"];
			var outputFilePath = project.DirectoryPath["Demo/LegacyOutput/OutputFilePath.txt"];
			var outputFileName = project.DirectoryPath["Demo/OutputFileNameTest"];
			var outputDirectoryAndOutputFileName = project.DirectoryPath["Demo/Output/OutputDirectoryAndFileNameTest.log"];

			// Act
			var instance = project.Build ("TransformTemplates");

			// Assert
			Assert.Multiple (() => {
				outputDirectory.AssertTextStartsWith ("Hello Metadata OutputDirectory 2024!");
				outputFilePath.AssertTextStartsWith ("Hello Metadata OutputFilePath 2024!");
				outputFileName.AssertTextStartsWith ("Hello Metadata OutputFileName 2024!");
				outputDirectoryAndOutputFileName.AssertTextStartsWith ("Hello Metadata OutputDirectory and OutputFileName 2024!");
			});

			instance.AssertNoItems ("PreprocessedTemplates");
		}

		[Theory]
		[InlineData (
			"PreprocessTemplate",
			"foo.cs",
			new string[] {
				"namespace PreprocessTemplate {",
				"public partial class foo : fooBase {"
			}
		)]
		[InlineData (
			"PreprocessTemplateFromRelativePath",
			"Nested/Template/Folder/foo.cs",
			new string[] {
				"namespace PreprocessTemplateFromRelativePath.Nested.Template.Folder {",
				"public partial class foo : fooBase {"
			}
		)]
		[InlineData (
			"PreprocessTemplateWithExtension",
			"foo.g.cs",
			new string[] {
				"namespace PreprocessTemplateWithExtension {",
				"public partial class foo : fooBase {"
			}
		)]
		public void PreprocessLegacy (string projectName, string expectedFilePath, string[] expectedContents)
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject (projectName)
				.WithProperty ("UseLegacyT4Preprocessing", "true");

			var instance = project.Build ("TransformTemplates");

			var generatedFilePath = project.DirectoryPath[expectedFilePath]
				.AssertContainsText
				(
					StringComparison.Ordinal,
					expectedContents
				);

			instance.AssertSingleItem ("PreprocessedTemplates", generatedFilePath);
			instance.AssertNoItems ("GeneratedTemplates");
		}

		[Theory]
		[InlineData (
			"PreprocessTemplate",
			"TextTransform/foo.cs",
			"PreprocessTemplate.foo"
		)]
		[InlineData (
			"PreprocessTemplateFromRelativePath",
			"TextTransform/Nested/Template/Folder/foo.cs",
			"PreprocessTemplateFromRelativePath.Nested.Template.Folder.foo"
		)]
		[InlineData (
			"PreprocessTemplateWithExtension",
			"TextTransform/foo.g.cs",
			"PreprocessTemplateWithExtension.foo"
		)]
		public void PreprocessOnBuild (string projectName, string expectedFilePath, string expectedType)
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject (projectName);

			project.Restore ();

			var instance = project.Build ("Build");
			var objDir = project.DirectoryPath["obj", "Debug", "netstandard2.0"];

			var generatedFilePath = instance.GetIntermediateDirFile (expectedFilePath)
				.AssertTextStartsWith ("//--------");

			instance.AssertSingleItem ("PreprocessedTemplates", generatedFilePath);
			instance.AssertNoItems ("GeneratedTemplates");

			instance.GetTargetPath ()
				.AssertFileName ($"{projectName}.dll")
				.AssertAssemblyContainsType (expectedType);
		}

		[Theory]
		[InlineData (
			"PreprocessTemplate",
			"TextTransform/foo.cs"
		)]
		[InlineData (
			"PreprocessTemplateFromRelativePath",
			"TextTransform/Nested/Template/Folder/foo.cs"
		)]
		[InlineData (
			"PreprocessTemplateWithExtension",
			"TextTransform/foo.g.cs"
		)]
		public void PreprocessOnDesignTimeBuild (string projectName, string expectedFilePath)
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject (projectName)
				.WithProperty ("DesignTimeBuild", "true")
				.WithProperty ("SkipCompilerExecution", "true");

			project.Restore ();

			var instance = project.Build ("CoreCompile");

			var generatedFilePath = instance.GetIntermediateDirFile (expectedFilePath)
				.AssertTextStartsWith ("//--------");

			instance.AssertSingleItem ("PreprocessedTemplates", generatedFilePath);
			instance.AssertNoItems ("GeneratedTemplates");
		}

		[Fact]
		public void PreprocessLegacyMetadata ()
		{
			// Arrange
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("PreprocessTemplateMetadata")
				.WithProperty ("UseLegacyT4Preprocessing", "true");

			var outputDirectory = project.DirectoryPath["Demo/Output/OutputDirectory.cs"];
			var outputFilePath = project.DirectoryPath["Demo/LegacyOutput/OutputFilePath.cs"];
			var outputFileName = project.DirectoryPath["Demo/OutputFileNameTest.cs"];
			var outputFileNameAndOutputDirectory = project.DirectoryPath["Demo/Output/OutputDirectoryAndFileNameTest.g.cs"];

			// Act
			var instance = project.Build ("TransformTemplates");

			// Assert
			Assert.Multiple (() => {
				outputDirectory.AssertContainsText
				(
					StringComparison.Ordinal,
					"namespace PreprocessTemplateMetadata.Demo.Output {",
					"partial class OutputDirectory"
				);

				outputFilePath.AssertContainsText
				(
					StringComparison.Ordinal,
					"namespace PreprocessTemplateMetadata.Demo.LegacyOutput {",
					"partial class OutputFilePath"
				);

				outputFileName.AssertContainsText
					(
						StringComparison.Ordinal,
						"namespace PreprocessTemplateMetadata.Demo {",
						"partial class OutputFileNameTest"
					);

				outputFileNameAndOutputDirectory.AssertContainsText
					(
						StringComparison.Ordinal,
						"namespace PreprocessTemplateMetadata.Demo.Output {",
						"partial class OutputDirectoryAndFileNameTest"
					);
			});

			instance.AssertNoItems ("GeneratedTemplates");
		}

		[Fact]
		public void IncrementalTransform ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("TransformWithInclude");

			project.Restore ();

			var fooGenerated = project.DirectoryPath["foo.txt"];
			var fooTemplate = project.DirectoryPath["foo.tt"];
			var barGenerated = project.DirectoryPath["bar.txt"];
			var barTemplate = project.DirectoryPath["bar.tt"];
			var includeFile = project.DirectoryPath["helper.ttinclude"];

			void ExecuteAndValidate ()
			{
				var instance = project.Build ("TransformTemplates");

				instance.GetItems ("GeneratedTemplates").AssertPaths (fooGenerated, barGenerated);
				instance.AssertNoItems ("PreprocessedTemplates");
				fooGenerated.AssertFileExists ();
			}

			ExecuteAndValidate ();

			fooGenerated.AssertTextStartsWith ("Helper says Hello 2019!");
			var fooWriteTime = new WriteTimeTracker (fooGenerated);
			var barWriteTime = new WriteTimeTracker (barGenerated);

			void AssertNoopBuild ()
			{
				ExecuteAndValidate ();
				fooWriteTime.AssertSame ();
				barWriteTime.AssertSame ();
			}

			AssertNoopBuild ();

			// check touching a template causes rebuild of that file only
			WriteTimeTracker.SetWriteTimeNewerThan (barWriteTime, barTemplate);
			ExecuteAndValidate ();
			fooWriteTime.AssertSame ();
			barWriteTime.AssertChanged ();

			AssertNoopBuild ();

			// check touching the include causes rebuild of the file that uses it
			WriteTimeTracker.SetWriteTimeNewerThan (fooWriteTime, includeFile);
			ExecuteAndValidate ();
			fooWriteTime.AssertChanged ();
			barWriteTime.AssertSame ();

			AssertNoopBuild ();

			// check changing a parameter causes rebuild of both files
			project.Project.GetItems ("T4Argument").Single (i => i.UnevaluatedInclude == "Year").SetMetadataValue ("Value", "2021");
			ExecuteAndValidate ();
			fooGenerated.AssertTextStartsWith ("Helper says Hello 2021!");
			fooWriteTime.AssertChanged ();
			barWriteTime.AssertChanged ();

			AssertNoopBuild ();
		}
	}
}