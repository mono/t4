// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class MSBuildExecutionTests : MSBuildTestBase
	{
		[Fact]
		public void TransformExplicitWithArguments ()
		{
			var proj = LoadTestProject ("TransformTemplates");

			var instance = CreateAndBuildInstance (proj, "TransformTemplates");

			var generated = Path.Combine (proj.DirectoryPath, "foo.txt");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("Hello 2019!", File.ReadAllText (generated));

			Assert.Equal (generated, Assert.Single (instance.GetItems ("GeneratedTemplates")).GetMetadataValue ("FullPath"));
			Assert.Empty (instance.GetItems ("PreprocessedTemplates"));
		}

		[Fact]
		public void TransformOnBuild ()
		{
			var proj = LoadTestProject ("TransformTemplates");
			proj.SetProperty ("TransformOnBuild", "true");

			RestoreProject (proj);

			var instance = CreateAndBuildInstance (proj, "Build");

			var generated = Path.Combine (proj.DirectoryPath, "foo.txt");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("Hello 2019!", File.ReadAllText (generated));

			Assert.Equal (generated, Assert.Single (instance.GetItems ("GeneratedTemplates")).GetMetadataValue ("FullPath"));
			Assert.Empty (instance.GetItems ("PreprocessedTemplates"));
		}

		[Fact]
		public void TransformOnBuildDisabled ()
		{
			var proj = LoadTestProject ("TransformTemplates");

			RestoreProject (proj);

			var instance = CreateAndBuildInstance (proj, "Build");

			var generated = Path.Combine (proj.DirectoryPath, "foo.txt");
			Assert.False (File.Exists (generated));

			Assert.Empty (instance.GetItems ("GeneratedTemplates"));
			Assert.Empty (instance.GetItems ("PreprocessedTemplates"));
		}

		[Fact]
		public void PreprocessLegacy ()
		{
			var proj = LoadTestProject ("PreprocessTemplate");
			proj.SetProperty ("UseLegacyT4Preprocessing", "true");

			var instance = CreateAndBuildInstance (proj, "TransformTemplates");

			var generated = Path.Combine (proj.DirectoryPath, "foo.cs");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("//--------", File.ReadAllText (generated));

			Assert.Empty (instance.GetItems ("GeneratedTemplates"));
			Assert.Equal (generated, Assert.Single (instance.GetItems ("PreprocessedTemplates")).GetMetadataValue ("FullPath"));
		}

		[Fact]
		public void PreprocessOnBuild ()
		{
			var proj = LoadTestProject ("PreprocessTemplate");

			RestoreProject (proj);

			var instance = CreateAndBuildInstance (proj, "Build");

			var generated = Path.Combine (proj.DirectoryPath, "obj", "Debug", "netstandard2.0", "TextTransform", "foo.cs");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("//--------", File.ReadAllText (generated));

			Assert.Empty (instance.GetItems ("GeneratedTemplates"));
			Assert.Equal (generated, Assert.Single (instance.GetItems ("PreprocessedTemplates")).GetMetadataValue ("FullPath"));

			var dll = Path.Combine (proj.DirectoryPath, "bin", "Debug", "netstandard2.0", "PreprocessTemplate.dll");
			Assert.True (File.Exists (dll));

			// context: "Should MetadataLoadContext consider System.Private.CoreLib as a core assembly name?"
			// https://github.com/dotnet/runtime/issues/41921
			var coreAssembly = typeof (object).Assembly;
			var resolver = new System.Reflection.PathAssemblyResolver (new string[] { coreAssembly.Location });
			var loader = new System.Reflection.MetadataLoadContext (resolver, coreAssemblyName: coreAssembly.GetName ().Name);
			// make sure we don't lock the file
			var asm = loader.LoadFromByteArray (File.ReadAllBytes (dll));

			Assert.NotNull (asm.GetType ("PreprocessTemplate.foo"));
		}

		[Fact]
		public void PreprocessOnDesignTimeBuild ()
		{
			var proj = LoadTestProject ("PreprocessTemplate");
			proj.SetProperty ("DesignTimeBuild", "true");
			proj.SetProperty ("SkipCompilerExecution", "true");

			RestoreProject (proj);

			var instance = CreateAndBuildInstance (proj, "CoreCompile");

			var generated = Path.Combine (proj.DirectoryPath, "obj", "Debug", "netstandard2.0", "TextTransform", "foo.cs");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("//--------", File.ReadAllText (generated));

			Assert.Empty (instance.GetItems ("GeneratedTemplates"));
			Assert.Equal (generated, Assert.Single (instance.GetItems ("PreprocessedTemplates")).GetMetadataValue ("FullPath"));
		}

		[Fact]
		public void IncrementalTransform ()
		{
			var proj = LoadTestProject ("TransformWithInclude");

			RestoreProject (proj);

			var fooGenerated = Path.Combine (proj.DirectoryPath, "foo.txt");
			var fooTemplate = Path.Combine (proj.DirectoryPath, "foo.tt");
			var barGenerated = Path.Combine (proj.DirectoryPath, "bar.txt");
			var barTemplate = Path.Combine (proj.DirectoryPath, "bar.tt");
			var includeFile = Path.Combine (proj.DirectoryPath, "helper.ttinclude");

			void ExecuteAndValidate()
			{
				var instance = CreateAndBuildInstance (proj, "TransformTemplates");

				var generatedItems = instance.GetItems ("GeneratedTemplates");
				Assert.Collection(generatedItems, a => Assert.Equal(fooGenerated, a.GetMetadataValue ("FullPath")), b => Assert.Equal (barGenerated, b.GetMetadataValue ("FullPath")));
				Assert.Empty (instance.GetItems ("PreprocessedTemplates"));
				Assert.True (File.Exists (fooGenerated));
			}

			ExecuteAndValidate ();

			Assert.StartsWith ("Helper says Hello 2019!", File.ReadAllText (fooGenerated));
			var fooWriteTime = File.GetLastWriteTime (fooGenerated);
			var barWriteTime = File.GetLastWriteTime (barGenerated);

			ExecuteAndValidate ();
			var fooWriteTimeAfterNoChange = File.GetLastWriteTime (fooGenerated);
			var barWriteTimeAfterNoChange = File.GetLastWriteTime (barGenerated);
			Assert.Equal (fooWriteTime, fooWriteTimeAfterNoChange);
			Assert.Equal (barWriteTime, barWriteTimeAfterNoChange);

			// check touching a template causes rebuild of that file only
			File.SetLastWriteTime (fooTemplate, DateTime.Now);
			ExecuteAndValidate ();
			fooWriteTime = File.GetLastWriteTime (fooGenerated);
			barWriteTime = File.GetLastWriteTime (barGenerated);
			Assert.True (fooWriteTime > fooWriteTimeAfterNoChange);
			Assert.True (barWriteTime == barWriteTimeAfterNoChange);

			ExecuteAndValidate ();
			fooWriteTimeAfterNoChange = File.GetLastWriteTime (fooGenerated);
			barWriteTimeAfterNoChange = File.GetLastWriteTime (barGenerated);
			Assert.Equal (fooWriteTime, fooWriteTimeAfterNoChange);
			Assert.Equal (barWriteTime, barWriteTimeAfterNoChange);

			// check touching the include causes rebuild of the file that uses it
			File.SetLastWriteTime (includeFile, DateTime.Now);
			ExecuteAndValidate ();
			fooWriteTime = File.GetLastWriteTime (fooGenerated);
			barWriteTime = File.GetLastWriteTime (barGenerated);
			Assert.True (fooWriteTime > fooWriteTimeAfterNoChange);
			Assert.True (barWriteTime == barWriteTimeAfterNoChange);

			ExecuteAndValidate ();
			fooWriteTimeAfterNoChange = File.GetLastWriteTime (fooGenerated);
			barWriteTimeAfterNoChange = File.GetLastWriteTime (barGenerated);
			Assert.Equal (fooWriteTime, fooWriteTimeAfterNoChange);
			Assert.Equal (barWriteTime, barWriteTimeAfterNoChange);

			// check changing a parameter causes rebuild of both files
			File.SetLastWriteTime (includeFile, DateTime.Now);
			var yearArg = proj.GetItems ("T4Argument").Single (i => i.UnevaluatedInclude == "Year");
			yearArg.SetMetadataValue ("Value", "2021");
			ExecuteAndValidate ();
			Assert.StartsWith ("Helper says Hello 2021!", File.ReadAllText (fooGenerated));
			fooWriteTime = File.GetLastWriteTime (fooGenerated);
			barWriteTime = File.GetLastWriteTime (barGenerated);
			Assert.True (fooWriteTime > fooWriteTimeAfterNoChange);
			Assert.True (barWriteTime > barWriteTimeAfterNoChange);

			ExecuteAndValidate ();
			fooWriteTimeAfterNoChange = File.GetLastWriteTime (fooGenerated);
			barWriteTimeAfterNoChange = File.GetLastWriteTime (barGenerated);
			Assert.Equal (fooWriteTime, fooWriteTimeAfterNoChange);
			Assert.Equal (barWriteTime, barWriteTimeAfterNoChange);
		}
	}
}