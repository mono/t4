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
	sealed class MSBuildTestContext : IDisposable
	{
		static readonly string buildTargetsProjectDir = TestDataPath.ProjectRoot["..", "Mono.TextTemplating.Build"];

		public MSBuildTestContext ([CallerMemberName] string testName = null, bool createBinLog = false)
		{
			OutputDir = TestDataPath.CreateOutputDir (testName);

			//reference this so xunit shadow copies it and we don't lock it
			string buildTasksPath = typeof (TextTransform).Assembly.Location;

			var globalProps = new Dictionary<string, string> {
				{ "ImportDirectoryBuildProps", "false" },
				{ "TemplatingTargetsPath", buildTargetsProjectDir },
				{ "TextTransformTaskAssembly", buildTasksPath }
			};

			Engine = new ProjectCollection (globalProps);

			if (createBinLog) {
				var binLogger = CreateBinLogger (testName);
				Engine.RegisterLogger (binLogger);
			}
		}

		public TestDataPath OutputDir { get; }

		public ProjectCollection Engine { get; }

		static ILogger CreateBinLogger (string testName) => new BinaryLogger { Parameters = $"LogFile=binlogs/{testName}.binlog" };

		public void Dispose ()
		{
			Engine.UnloadAllProjects ();
			Engine.UnregisterAllLoggers ();
		}

		public MSBuildTestProject LoadTestProject (string projectName = null, [CallerMemberName] string testName = null)
		{
			projectName ??= testName ?? throw new ArgumentNullException (nameof (projectName));

			var testCaseDir = TestDataPath.GetTestCase (projectName);

			testCaseDir.CopyDirectoryTo (OutputDir);

			var project = new MSBuildTestProject (this, Engine.LoadProject (OutputDir[projectName + ".csproj"]));

			// project output may exist if someone has been editing these projects in situ
			// so if they were copied over, delete them so they don't pollute the tests
			project.CleanOutput ();

			return project;
		}
	}

	class MSBuildTestProject
	{
		public MSBuildTestProject (MSBuildTestContext context, Project project)
		{
			this.context = context;
			Project = project;
		}

		readonly MSBuildTestContext context;

		public Project Project { get; }

		public TestDataPath DirectoryPath => new (Project.DirectoryPath);
		public TestDataPath ProjectPath => new (Project.FullPath);

		public void Restore ()
		{
			Project.SetGlobalProperty ("MSBuildRestoreSessionId", Guid.NewGuid ().ToString ("D"));

			Build ("Restore");

			// removing this property forces the project to re-evaluate next time a ProjectInstance is created
			// which is needed for other targets to pick up the Restore outputs
			Project.RemoveGlobalProperty ("MSBuildRestoreSessionId");
		}

		/// <summary>
		/// Asserts that the build is successful and there are no errors or warnings
		/// </summary>
		public ProjectInstance Build (string target)
		{
			var instance = Project.CreateProjectInstance ();
			var success = instance.Build (target, context.Engine.Loggers.Append (new MSBuildTestErrorLogger (assertEmpty: true)));

			Assert.True (success);

			return instance;
		}

		public void CleanOutput ()
		{
			ProjectPath["bin"].DeleteIfExists ();
			ProjectPath["obj"].DeleteIfExists ();
		}

		public MSBuildTestProject WithProperty (string name, string value)
		{
			Project.SetProperty (name, value);
			return this;
		}
	}

	class MSBuildTestErrorLogger : ILogger
	{
		readonly bool assertEmpty;

		public MSBuildTestErrorLogger (bool assertEmpty) => this.assertEmpty = assertEmpty;

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

	static class MSBuildTestExtensions
	{
		public static void AssertNoItems (this ProjectInstance instance, string itemName) => Assert.Empty (instance.GetItems (itemName));

		public static void AssertNoItems (this ProjectInstance instance, params string[] itemNames)
		{
			foreach (var itemName in itemNames) {
				instance.AssertNoItems (itemName);
			}
		}

		public static void AssertSingleItem (this ProjectInstance instance, string itemName, string withFullPath)
			=> instance.AssertSingleItem (itemName, withMetadata: "FullPath", withFullPath);

		public static void AssertSingleItem (this ProjectInstance instance, string itemName, string withMetadata, string metadataValue)
			=> Assert.Equal (
				metadataValue,
				Assert.Single (instance.GetItems (itemName)).GetMetadataValue (withMetadata)
			);

		public static void AssertPaths (this ICollection<ProjectItemInstance> items, params string[] expectedFullPaths)
		{
			var actualPaths = items.Select (item => item.GetMetadataValue ("FullPath")).ToHashSet ();
			foreach (var expectedPath in expectedFullPaths) {
				if (!actualPaths.Remove (expectedPath)) {
					throw new Xunit.Sdk.ContainsException (expectedPath, actualPaths);
				}
			}
			Assert.Empty (actualPaths);
		}

		public static TestDataPath AssertAssemblyContainsType (this TestDataPath assemblyPath, string typeName)
		{
			assemblyPath.AssertFileExists ();

			// context: "Should MetadataLoadContext consider System.Private.CoreLib as a core assembly name?"
			// https://github.com/dotnet/runtime/issues/41921
			var coreAssembly = typeof (object).Assembly;
			var resolver = new System.Reflection.PathAssemblyResolver (new string[] { coreAssembly.Location });
			var loader = new System.Reflection.MetadataLoadContext (resolver, coreAssemblyName: coreAssembly.GetName ().Name);

			// make sure we don't lock the file
			var asm = loader.LoadFromByteArray (File.ReadAllBytes (assemblyPath));

			Assert.NotNull (asm.GetType (typeName));

			return assemblyPath;
		}

		static string ToNativePath (string path) => path?.Replace ('\\', Path.DirectorySeparatorChar);

		public static TestDataPath GetPathProperty (this ProjectInstance instance, string propertyName)
		{
			var path = ToNativePath (instance.GetPropertyValue (propertyName));
			Assert.NotEmpty (path);
			return new TestDataPath (Path.Combine (instance.Directory, path));
		}

		public static TestDataPath GetIntermediateDir (this ProjectInstance instance) => instance.GetPathProperty ("IntermediateOutputPath");
		public static TestDataPath GetTargetDir (this ProjectInstance instance) => instance.GetPathProperty ("TargetDir");
		public static TestDataPath GetTargetPath (this ProjectInstance instance) => instance.GetPathProperty ("TargetPath");

		public static TestDataPath GetTargetDirFile (this ProjectInstance instance, params string[] paths)
			=> GetTargetDir (instance).Combine (paths);

		public static TestDataPath GetIntermediateDirFile (this ProjectInstance instance, params string[] paths)
			=> GetIntermediateDir (instance).Combine (paths);
	}

	class MSBuildFixture
	{
		public MSBuildFixture () => MSBuildTestHelpers.RegisterMSBuildAssemblies ();
	}
}