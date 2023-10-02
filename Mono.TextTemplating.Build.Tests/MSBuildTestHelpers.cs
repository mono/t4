// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// based on MonoDevelop.MSBuild.Tests.MSBuildTestHelpers from  MonoDevelop.MSBuildEditor
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Build.Locator;
using Xunit;

// based on MonoDevelop.MSBuild.Tests.MSBuildTestHelpers from  MonoDevelop.MSBuildEditor
namespace Mono.TextTemplating.Tests
{
	static class MSBuildTestHelpers
	{
		static bool registeredAssemblies;

		public static void RegisterMSBuildAssemblies ()
		{
			if (registeredAssemblies) {
				return;
			}
			registeredAssemblies = true;

			if (Platform.IsWindows || !Platform.IsMono) {
				MSBuildLocator.RegisterDefaults ();
				return;
			}

			if (Platform.IsMac) {
				MSBuildLocator.RegisterMSBuildPath (
					"/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/msbuild/Current/bin"
				);
				return;
			}

			var msbuildInPath = FindInPath ("msbuild");
			if (msbuildInPath != null) {
				//attempt to read the msbuild.dll location from the launch script
				//FIXME: handle quoting in the script
				Console.WriteLine ("Found msbuild script in PATH: {0}", msbuildInPath);
#pragma warning disable CA1861 // Avoid constant arrays as arguments
				var tokens = File.ReadAllText (msbuildInPath).Split (new [] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
#pragma warning restore CA1861 // Avoid constant arrays as arguments
				var filename = tokens.FirstOrDefault (t => t.EndsWith ("MSBuild.dll", StringComparison.OrdinalIgnoreCase));
				if (filename != null && File.Exists (filename)) {
					var dir = Path.GetDirectoryName (filename);
					MSBuildLocator.RegisterMSBuildPath (dir);
					Console.WriteLine ("Discovered MSBuild from launch script: {0}", dir);
					return;
				}
			}

			foreach (var dir in GetPossibleMSBuildDirectoriesLinux ()) {
				if (File.Exists (Path.Combine (dir, "MSBuild.dll"))) {
					MSBuildLocator.RegisterMSBuildPath (dir);
					Console.WriteLine ("Discovered MSBuild at well known location: {0}", dir);
					return;
				}
			}

			Assert.Fail ("Could not find MSBuild");
		}

		static IEnumerable<string> GetPossibleMSBuildDirectoriesLinux ()
		{
			yield return "/usr/lib/mono/msbuild/Current/bin";
			yield return "/usr/lib/mono/msbuild/15.0/bin";
		}

		static string FindInPath (string name)
		{
			var pathEnv = Environment.GetEnvironmentVariable ("PATH");
			if (pathEnv == null) {
				return null;
			}

			var paths = pathEnv.Split (new [] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var path in paths) {
				var possible = Path.Combine (path, name);
				if (File.Exists (possible)) {
					return possible;
				}
			}

			return null;
		}
	}
}
