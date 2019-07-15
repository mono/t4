// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Mono.TextTemplating.Build;
using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class MSBuildTests : IClassFixture<MSBuildFixture>
	{
		Project LoadTestProject (string name)
		{
			var asmDir = Environment.CurrentDirectory;
			var srcDir = Path.Combine (asmDir, "MSBuildTestCases", name);

			var destDir = Path.Combine (asmDir, "test-output", name);

			if (Directory.Exists (destDir)) {
				Directory.Delete (destDir, true);
			}

			CopyDirectory (srcDir, destDir);

			string configName = Path.GetFileName (asmDir);
			string buildTargetsProjectDir = Path.GetFullPath (Path.Combine (asmDir, "..", "..", "..", "Mono.TextTemplating.Build"));

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

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
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

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
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

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
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

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
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

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
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

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
			Assert.True (success);

			var generated = Path.Combine (proj.DirectoryPath, "obj", "Debug", "netstandard2.0", "TextTransform", "foo.cs");
			Assert.True (File.Exists (generated));
			Assert.StartsWith ("//--------", File.ReadAllText (generated));

			Assert.Empty (instance.GetItems ("GeneratedTemplates"));
			Assert.Equal (generated, Assert.Single (instance.GetItems ("PreprocessedTemplates")).GetMetadataValue ("FullPath"));
		}

		void RestoreProject (Project project, ListLogger logger)
		{
			project.SetGlobalProperty ("MSBuildRestoreSessionId", Guid.NewGuid ().ToString ("D"));
			var instance = project.CreateProjectInstance ();

			var success = instance.Build (new string[] { "Restore" }, new[] { logger });

			Assert.Empty (logger.Errors);
			Assert.Empty (logger.Warnings);
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
	}

	class MSBuildFixture
	{
		public MSBuildFixture ()
		{
			MSBuildTestHelpers.RegisterMSBuildAssemblies ();
		}
	}
}
