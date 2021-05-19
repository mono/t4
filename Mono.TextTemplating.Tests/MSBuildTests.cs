// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;

using Mono.TextTemplating.Build;

using Xunit;

#if NET5_0 || NET472

namespace Mono.TextTemplating.Tests
{
	public class MSBuildExecutionTests : IClassFixture<MSBuildFixture>
	{
		Project LoadTestProject (string name, [CallerMemberName] string testName = null)
		{
			var asmDir = Environment.CurrentDirectory;
			var srcDir = Path.Combine (asmDir, "MSBuildTestCases", name);

			var destDir = Path.Combine (asmDir, "test-output", testName ?? name);

			void DeleteIfExists(string p)
			{
				if (Directory.Exists (p))
					Directory.Delete (p, true);
			}

			DeleteIfExists (destDir);
			CopyDirectory (srcDir, destDir);

			// these might exist if someone has been editing these projects in situ
			// but they can break or invalidate our test results, so remove them
			DeleteIfExists (Path.Combine(destDir, "bin"));
			DeleteIfExists (Path.Combine(destDir, "obj"));

			string buildTargetsProjectDir = Path.GetFullPath (Path.Combine (asmDir, "..", "..", "..", "..", "Mono.TextTemplating.Build"));

			//reference this so xunit shadow copies it and we don't lock it
			string buildTasksPath = typeof (TextTransform).Assembly.Location;

			var engine = new ProjectCollection ();
			engine.SetGlobalProperty ("TemplatingTargetsPath", buildTargetsProjectDir);
			engine.SetGlobalProperty ("TextTransformTaskAssembly", buildTasksPath);
			return engine.LoadProject (Path.Combine (destDir, name + ".csproj"));
		}

		[Fact]
		public void TransformExplicitWithArguments ()
		{
			var proj = LoadTestProject ("TransformTemplates");
			var instance = proj.CreateProjectInstance ();
			var logger = new ListLogger ();
			var success = instance.Build ("TransformTemplates", new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

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
			var logger = new ListLogger ();

			RestoreProject (proj, logger);

			var instance = proj.CreateProjectInstance ();
			var success = instance.Build (new string[] { "Build" }, new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

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
			var logger = new ListLogger ();

			RestoreProject (proj, logger);

			var instance = proj.CreateProjectInstance ();
			var success = instance.Build (new string[] { "Build" }, new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

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
			var instance = proj.CreateProjectInstance ();
			var logger = new ListLogger ();
			var success = instance.Build ("TransformTemplates", new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

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
			var logger = new ListLogger ();

			RestoreProject (proj, logger);

			var instance = proj.CreateProjectInstance ();
			var success = instance.Build (new string[] { "Build" }, new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

			var generated = Path.Combine (proj.DirectoryPath, "obj", "Debug", "netstandard2.0", "TextTransform", "foo.cs");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("//--------", File.ReadAllText (generated));

			Assert.Empty (instance.GetItems ("GeneratedTemplates"));
			Assert.Equal (generated, Assert.Single (instance.GetItems ("PreprocessedTemplates")).GetMetadataValue ("FullPath"));
		}

		[Fact]
		public void PreprocessOnDesignTimeBuild ()
		{
			var proj = LoadTestProject ("PreprocessTemplate");
			proj.SetProperty ("DesignTimeBuild", "true");
			proj.SetProperty ("SkipCompilerExecution", "true");
			var logger = new ListLogger ();

			RestoreProject (proj, logger);

			var instance = proj.CreateProjectInstance ();
			var success = instance.Build (new string[] { "CoreCompile" }, new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

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
			var logger = new ListLogger ();

			RestoreProject (proj, logger);

			var fooGenerated = Path.Combine (proj.DirectoryPath, "foo.txt");
			var fooTemplate = Path.Combine (proj.DirectoryPath, "foo.tt");
			var barGenerated = Path.Combine (proj.DirectoryPath, "bar.txt");
			var barTemplate = Path.Combine (proj.DirectoryPath, "bar.tt");
			var includeFile = Path.Combine (proj.DirectoryPath, "helper.ttinclude");

			void ExecuteAndValidate()
			{
				var instance = proj.CreateProjectInstance ();
				var success = instance.Build (new string[] { "TransformTemplates" }, new[] { logger });

				AssertNoErrors (logger.Errors);
				AssertNoWarnings (logger.Warnings);
				Assert.True (success);

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

		void RestoreProject (Project project, ListLogger logger)
		{
			project.SetGlobalProperty ("MSBuildRestoreSessionId", Guid.NewGuid ().ToString ("D"));
			var instance = project.CreateProjectInstance ();

			var success = instance.Build (new string[] { "Restore" }, new[] { logger });

			AssertNoErrors (logger.Errors);
			AssertNoWarnings (logger.Warnings);
			Assert.True (success);

			// removing this property forces the project to re-evaluate next time a ProjectInstance is created
			// which is needed for other targets to pick up the Restore outputs
			project.RemoveGlobalProperty ("MSBuildRestoreSessionId");
		}

		void CopyDirectory (string src, string dest) => CopyDirectory (new DirectoryInfo (src), new DirectoryInfo (dest));

		void CopyDirectory (DirectoryInfo src, DirectoryInfo dest)
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

		class ListLogger : ILogger
		{
			public List<BuildEventArgs> Errors { get; } = new List<BuildEventArgs> ();
			public List<BuildWarningEventArgs> Warnings { get; } = new List<BuildWarningEventArgs> ();

			public LoggerVerbosity Verbosity { get; set; }
			public string Parameters { get; set; }

			public void Initialize (IEventSource eventSource)
			{
				eventSource.ErrorRaised += EventSource_ErrorRaised;
				eventSource.WarningRaised += EventSource_WarningRaised;
			}

			void EventSource_WarningRaised (object sender, BuildWarningEventArgs e) => Warnings.Add (e);

			void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e) => Errors.Add (e);

			public void Shutdown ()
			{
			}
		}

		static void AssertNoErrors (List<BuildEventArgs> list)
		{
			if (list.Count == 0) {
				return;
			}

			Assert.Null (list[0].Message);
			Assert.Empty (list);
		}

		static void AssertNoWarnings (List<BuildWarningEventArgs> list)
		{
			if (list.Count == 0) {
				return;
			}

			Assert.Null (list[0].Message);
			Assert.Empty (list);
		}
	}

	class MSBuildFixture
	{
		public MSBuildFixture ()
		{
			MSBuildTestHelpers.RegisterMSBuildAssemblies ();
		}
	}
}

#endif