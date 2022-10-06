// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

using Mono.TextTemplating.Build;

using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class MSBuildTestBase : IClassFixture<MSBuildFixture>, IDisposable
	{
		readonly List<Action> disposers = new ();

		protected Project LoadTestProject (string name, [CallerMemberName] string testName = null, bool createBinLog = false)
		{
			var asmDir = Environment.CurrentDirectory;
			var srcDir = Path.Combine (asmDir, "TestCases", name);

			var destDir = Path.Combine (asmDir, "test-output", testName ?? name);

			void DeleteIfExists (string p)
			{
				if (Directory.Exists (p))
					Directory.Delete (p, true);
			}

			DeleteIfExists (destDir);
			CopyDirectory (srcDir, destDir);

			// these might exist if someone has been editing these projects in situ
			// but they can break or invalidate our test results, so remove them
			DeleteIfExists (Path.Combine (destDir, "bin"));
			DeleteIfExists (Path.Combine (destDir, "obj"));

			string buildTargetsProjectDir = Path.GetFullPath (Path.Combine (asmDir, "..", "..", "..", "..", "Mono.TextTemplating.Build"));

			//reference this so xunit shadow copies it and we don't lock it
			string buildTasksPath = typeof (TextTransform).Assembly.Location;

			var engine = new ProjectCollection ();

			if (createBinLog) {
				var binLogger = CreateBinLogger (testName);
				engine.RegisterLogger (binLogger);
			}

			engine.SetGlobalProperty ("ImportDirectoryBuildProps", "false");
			engine.SetGlobalProperty ("TemplatingTargetsPath", buildTargetsProjectDir);
			engine.SetGlobalProperty ("TextTransformTaskAssembly", buildTasksPath);

			var project = engine.LoadProject (Path.Combine (destDir, name + ".csproj"));

			disposers.Add (() => {
				engine.UnloadAllProjects ();
				engine.UnregisterAllLoggers ();
			});

			return project;
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

		protected static void RestoreProject (Project project)
		{
			project.SetGlobalProperty ("MSBuildRestoreSessionId", Guid.NewGuid ().ToString ("D"));

			CreateAndBuildInstance (project, "Restore");

			// removing this property forces the project to re-evaluate next time a ProjectInstance is created
			// which is needed for other targets to pick up the Restore outputs
			project.RemoveGlobalProperty ("MSBuildRestoreSessionId");
		}


		/// <summary>
		/// Asserts that the build is successful and there are no errors or warnings
		/// </summary>
		protected static ProjectInstance CreateAndBuildInstance (Project project, string target)
		{
			var instance = project.CreateProjectInstance ();
			var success = instance.Build (target, project.ProjectCollection.Loggers.Append (new ErrorLogger (assertEmpty: true)));

			Assert.True (success);

			return instance;
		}

		static ILogger CreateBinLogger (string testName) => new BinaryLogger { Parameters = $"LogFile=binlogs/{testName}.binlog" };

		#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
		public void Dispose ()
		{
			foreach (var disposer in disposers) {
				disposer ();
			}
		}
		#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

		class ErrorLogger : ILogger
		{
			readonly bool assertEmpty;

			public ErrorLogger (bool assertEmpty) => this.assertEmpty = assertEmpty;

			public List<BuildEventArgs> ErrorsAndWarnings { get; } = new List<BuildEventArgs> ();

			public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Minimal;

			public string Parameters { get; set; }

			public void Initialize (IEventSource eventSource)
			{
				eventSource.ErrorRaised += EventSource_ErrorRaised;
				eventSource.WarningRaised += EventSource_WarningRaised;
			}

			void EventSource_WarningRaised (object sender, BuildWarningEventArgs e) => ErrorsAndWarnings.Add (e);

			void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e) => ErrorsAndWarnings.Add (e);

			public void Shutdown ()
			{
				if (assertEmpty) {
					Assert.Empty (ErrorsAndWarnings.Select (e => e.Message));
				}
			}
		}
	}

	class MSBuildFixture
	{
		public MSBuildFixture () => MSBuildTestHelpers.RegisterMSBuildAssemblies ();
	}
}