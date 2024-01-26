//
// FrameworkHelpers.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2018 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Mono.TextTemplating.CodeCompilation
{
	enum RuntimeKind
	{
		NetCore,
		NetFramework,
		Mono
	}

	class RuntimeInfo
	{
		RuntimeInfo (RuntimeKind kind) => Kind = kind;

		static RuntimeInfo FromError (RuntimeKind kind, string error) => new (kind) { Error = error };

		public RuntimeKind Kind { get; private set; }
		public string Error { get; private set; }
		public string RuntimeDir { get; private set; }

		// may be null, as this is not a problem when the optional in-process compiler is used
		public string CscPath { get; private set; }

		/// <summary>
		/// Maximum C# language version supported by C# compiler in <see cref="CscPath"/>.
		/// </summary>
		public CSharpLangVersion CscMaxLangVersion { get; private set; }

		public bool IsValid => Error == null;
		public Version Version { get; private set; }

		public string RefAssembliesDir { get; private set; }
		public string RuntimeFacadesDir { get; internal set; }

		public static RuntimeInfo GetRuntime ()
		{
			if (Type.GetType ("Mono.Runtime") != null)
			{
				return GetMonoRuntime ();
			}
			else if (RuntimeInformation.FrameworkDescription.StartsWith (".NET Framework", StringComparison.OrdinalIgnoreCase))
			{
				return GetNetFrameworkRuntime ();
			}
			else
			{
				return GetDotNetCoreSdk ();
			}
		}

		static RuntimeInfo GetMonoRuntime ()
		{
			var runtimeDir = Path.GetDirectoryName (typeof (object).Assembly.Location);
			var csc = Path.Combine (runtimeDir, "csc.exe");
			if (!File.Exists (csc)) {
				return FromError (RuntimeKind.Mono, "Could not find csc in host Mono installation" );
			}

			return new RuntimeInfo (RuntimeKind.Mono) {
				CscPath = csc,
				RuntimeDir = runtimeDir,
				RuntimeFacadesDir = Path.Combine (runtimeDir, "Facades"),
				// we don't really care about the version if it's not .net core
				Version = new Version ("4.7.2"),
				//if mono has csc at all, we know it at least supports 6.0
				CscMaxLangVersion = CSharpLangVersion.v6_0
			};
		}

		static RuntimeInfo GetNetFrameworkRuntime ()
		{
			var runtimeDir = Path.GetDirectoryName (typeof (object).Assembly.Location);
			var csc = Path.Combine (runtimeDir, "csc.exe");
			if (!File.Exists (csc)) {
				return FromError (RuntimeKind.NetFramework, "Could not find csc in host .NET Framework installation");
			}
			return new RuntimeInfo (RuntimeKind.NetFramework) {
				CscPath = csc,
				RuntimeDir = runtimeDir,
				RuntimeFacadesDir = runtimeDir,
				// we don't really care about the version if it's not .net core
				Version = new Version ("4.7.2"),
				CscMaxLangVersion = CSharpLangVersion.v5_0
			};
		}

		static RuntimeInfo GetDotNetCoreSdk ()
		{
			static bool DotnetRootIsValid (string root) => !string.IsNullOrEmpty (root) && (File.Exists (Path.Combine (root, "dotnet")) || File.Exists (Path.Combine (root, "dotnet.exe")));

			// the runtime dir is used when probing for DOTNET_ROOT
			// and as a fallback in case we cannot locate reference assemblies
			var runtimeDir = Path.GetDirectoryName (typeof (object).Assembly.Location);

			var dotnetRoot = Environment.GetEnvironmentVariable ("DOTNET_ROOT");

			if (!DotnetRootIsValid (dotnetRoot)) {
				// this will work if runtimeDir is $DOTNET_ROOT/shared/Microsoft.NETCore.App/5.0.0
				dotnetRoot = Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (runtimeDir)));

				if (!DotnetRootIsValid (dotnetRoot)) {
					return FromError (RuntimeKind.NetCore, "Could not locate .NET root directory from running app. It can be set explicitly via the `DOTNET_ROOT` environment variable.");
				}
			}

			var hostVersion = Environment.Version;

			// fallback for .NET Core < 3.1, which always returned 4.0.x
			if (hostVersion.Major == 4)
			{
				// this will work if runtimeDir is $DOTNET_ROOT/shared/Microsoft.NETCore.App/5.0.0
				var versionPathComponent = Path.GetFileName (runtimeDir);
				if (SemVersion.TryParse (versionPathComponent, out var hostSemVersion)) {
					hostVersion = new Version (hostSemVersion.Major, hostSemVersion.Minor, hostSemVersion.Patch);
				}
				else {
					return FromError (RuntimeKind.NetCore, "Could not determine host runtime version");
				}
			}

			// find the highest available C# compiler. we don't load it in process, so its runtime doesn't matter.
			static string MakeCscPath (string d) => Path.Combine (d, "Roslyn", "bincore", "csc.dll");
			var sdkDir = FindHighestVersionedDirectory (Path.Combine (dotnetRoot, "sdk"), d => File.Exists (MakeCscPath (d)), out var sdkVersion);

			// it's okay if cscPath is null as we may be using the in-process compiler
			string cscPath = sdkDir == null ? null : MakeCscPath (sdkDir);
			var maxCSharpVersion = CSharpLangVersionHelper.FromNetCoreSdkVersion (sdkVersion);

			// it's ok if this is null, we may be running on an older SDK that didn't support packs
			//in which case we fall back to resolving from the runtime dir
			var refAssembliesDir = FindHighestVersionedDirectory (
				Path.Combine (dotnetRoot, "packs", "Microsoft.NETCore.App.Ref"),
				d => File.Exists (Path.Combine (d, $"net{hostVersion.Major}.{hostVersion.Minor}", "System.Runtime.dll")),
				out _
			);

			return new RuntimeInfo (RuntimeKind.NetCore) { RuntimeDir = runtimeDir, RefAssembliesDir = refAssembliesDir, CscPath = MakeCscPath (sdkDir), CscMaxLangVersion = maxCSharpVersion, Version = hostVersion };
		}

		static string FindHighestVersionedDirectory (string parentFolder, Func<string, bool> validate, out SemVersion bestVersion)
		{
			string bestMatch = null;
			bestVersion = SemVersion.Zero;
			foreach (var dir in Directory.EnumerateDirectories (parentFolder)) {
				var name = Path.GetFileName (dir);
				if (SemVersion.TryParse (name, out var version) && version.Major >= 0) {
					if (version > bestVersion && (validate == null || validate (dir))) {
						bestVersion = version;
						bestMatch = dir;
					}
				}
			}
			return bestMatch;
		}
	}
}
