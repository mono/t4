// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using NUnit.Framework;

namespace Mono.TextTemplating.Build
{
	[TestFixture]
	public class MSBuildTests
	{
		[OneTimeSetUp]
		public void SetUp ()
		{
			MSBuildTestHelpers.RegisterMSBuildAssemblies ();
		}

		Project LoadTestProject (string name)
		{
			var asmLoc = typeof (MSBuildTests).Assembly.Location;
			var asmDir = Path.GetDirectoryName (asmLoc);
			var srcDir = Path.Combine (asmDir, "MSBuildTestCase", name);

			var destDir = Path.Combine (asmDir, "test-output", name);

			if (Directory.Exists (destDir)) {
				Directory.Delete (destDir, true);
			}

			CopyDirectory (srcDir, destDir);

			var engine = new ProjectCollection ();
			engine.SetGlobalProperty ("TemplatingTargetsPath", "");
			return engine.LoadProject (Path.Combine (destDir, name + ".csproj"));
		}

		[Test]
		public void TransformOnBuild ()
		{
			var proj = LoadTestProject ("TransformTemplates");
			var instance = proj.CreateProjectInstance ();
			var result = instance.Build ("TransformTemplates", Enumerable.Empty<ILogger> ());
			Assert.IsTrue (result);
			Assert.IsTrue (File.Exists (Path.Combine (proj.DirectoryPath, "foo.txt")));
		}

		void CopyDirectory (string src, string dest) => CopyDirectory (new DirectoryInfo (src), new DirectoryInfo (dest));

		void CopyDirectory (DirectoryInfo src, DirectoryInfo dest)
		{
			dest.Create ();

			foreach (var fsi in src.EnumerateFileSystemInfos ()) {
				if (fsi is DirectoryInfo d) {
					CopyDirectory (d, new DirectoryInfo (Path.Combine (dest.FullName, d.Name)));
				} else {
					var f = (FileInfo) fsi;
					File.Copy (f.FullName, Path.Combine (dest.FullName, f.Name));
				}
			}
		}
	}
}
