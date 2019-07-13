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
		public void TransformOnBuild ()
		{
			var proj = LoadTestProject ("TransformTemplates");
			var instance = proj.CreateProjectInstance ();
			var logger = new ListLogger ();
			var success = instance.Build ("TransformTemplates", new[] { logger });

			Assert.True (success);
			Assert.True (logger.Errors.Count == 0);
			Assert.True (logger.Warnings.Count == 0);
			Assert.True (File.Exists (Path.Combine (proj.DirectoryPath, "foo.txt")));
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
