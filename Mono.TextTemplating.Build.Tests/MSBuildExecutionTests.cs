// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class MSBuildExecutionTests : IClassFixture<MSBuildFixture>
	{
		[Fact]
		public void TransformExplicitWithArguments ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("TransformTemplates");

			var instance = project.Build ("TransformTemplates");

			var generated = project.DirectoryPath["foo.txt"].AssertTextStartsWith ("Hello 2019!");

			instance.AssertSingleItem ("GeneratedTemplates", withFullPath: generated);
			instance.AssertNoItems ("PreprocessedTemplates");
		}

		[Fact]
		public void TransformOnBuild ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("TransformTemplates")
				.WithProperty ("TransformOnBuild", "true");

			project.Restore ();

			var instance = project.Build ("Build");

			var generatedFilePath = project.DirectoryPath["foo.txt"].AssertTextStartsWith("Hello 2019!");

			instance.AssertSingleItem ("GeneratedTemplates", withFullPath: generatedFilePath);
			instance.AssertNoItems ("PreprocessedTemplates");
		}

		[Fact]
		public void TransformOnBuildDisabled ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("TransformTemplates");

			project.Restore ();

			var instance = project.Build ("Build");

			project.DirectoryPath["foo.txt"].AssertFileExists (false);

			instance.AssertNoItems ("GeneratedTemplates", "PreprocessedTemplates");
		}

		[Fact]
		public void PreprocessLegacy ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("PreprocessTemplate")
				.WithProperty ("UseLegacyT4Preprocessing", "true");

			var instance = project.Build ("TransformTemplates");

			var generatedFilePath = project.DirectoryPath["foo.cs"].AssertTextStartsWith ("//--------");

			instance.AssertSingleItem ("PreprocessedTemplates", generatedFilePath);
			instance.AssertNoItems ("GeneratedTemplates");
		}

		[Fact]
		public void PreprocessOnBuild ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("PreprocessTemplate");

			project.Restore ();

			var instance = project.Build ("Build");
			var objDir = project.DirectoryPath["obj", "Debug", "netstandard2.0"];

			var generatedFilePath = instance.GetIntermediateDirFile ("TextTransform", "foo.cs")
				.AssertTextStartsWith ("//--------");

			instance.AssertSingleItem ("PreprocessedTemplates", generatedFilePath);
			instance.AssertNoItems ("GeneratedTemplates");

			instance.GetTargetPath ()
				.AssertFileName ("PreprocessTemplate.dll")
				.AssertAssemblyContainsType ("PreprocessTemplate.foo");
		}

		[Fact]
		public void PreprocessOnDesignTimeBuild ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("PreprocessTemplate")
				.WithProperty ("DesignTimeBuild", "true")
				.WithProperty ("SkipCompilerExecution", "true");

			project.Restore ();

			var instance = project.Build ("CoreCompile");

			var generatedFilePath = instance.GetIntermediateDirFile ("TextTransform", "foo.cs")
				.AssertTextStartsWith ("//--------");

			instance.AssertSingleItem ("PreprocessedTemplates", generatedFilePath);
			instance.AssertNoItems ("GeneratedTemplates");
		}

		[Fact]
		public void IncrementalTransform ()
		{
			using var ctx = new MSBuildTestContext ();
			var project = ctx.LoadTestProject ("TransformWithInclude");

			project.Restore ();

			var fooGenerated = project.DirectoryPath ["foo.txt"];
			var fooTemplate = project.DirectoryPath ["foo.tt"];
			var barGenerated = project.DirectoryPath ["bar.txt"];
			var barTemplate = project.DirectoryPath ["bar.tt"];
			var includeFile = project.DirectoryPath ["helper.ttinclude"];

			void ExecuteAndValidate()
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
			File.SetLastWriteTime (fooTemplate, DateTime.Now);
			ExecuteAndValidate ();
			fooWriteTime.AssertChanged ();
			barWriteTime.AssertSame ();

			AssertNoopBuild ();

			// check touching the include causes rebuild of the file that uses it
			File.SetLastWriteTime (includeFile, DateTime.Now);
			ExecuteAndValidate ();
			fooWriteTime.AssertChanged ();
			barWriteTime.AssertSame ();

			AssertNoopBuild ();

			// check changing a parameter causes rebuild of both files
			File.SetLastWriteTime (includeFile, DateTime.Now);
			project.Project.GetItems ("T4Argument").Single (i => i.UnevaluatedInclude == "Year").SetMetadataValue ("Value", "2021");
			ExecuteAndValidate ();
			fooGenerated.AssertTextStartsWith ("Helper says Hello 2021!");
			fooWriteTime.AssertChanged ();
			barWriteTime.AssertChanged ();

			AssertNoopBuild ();
		}
	}
}