// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextTemplating.CodeCompilation
{
	enum CSharpLangVersion
	{
		v5_0,
		v6_0,
		v7_0,
		v7_1,
		v7_2,
		v7_3,
		v8_0,
		v9_0,
		Latest
	}

	static class CSharpLangVersionHelper
	{
		public static CSharpLangVersion GetBestSupportedLangVersion (RuntimeInfo runtime, CSharpLangVersion? compilerLangVersion = null)
			=> (CSharpLangVersion)Math.Min ((int)(compilerLangVersion ?? runtime.MaxSupportedLangVersion), (int) (runtime switch {
				{ Kind: RuntimeKind.NetCore, Version.Major: > 5 } => CSharpLangVersion.Latest,
				{ Kind: RuntimeKind.NetCore, Version.Major: 5 } => CSharpLangVersion.v9_0,
				{ Kind: RuntimeKind.NetCore, Version.Major: 3 } => CSharpLangVersion.v8_0,
				_ => CSharpLangVersion.v7_3,
			}));

		static bool HasLangVersionArg (string args) =>
			!string.IsNullOrEmpty(args)
				&& (args.IndexOf ("langversion", StringComparison.OrdinalIgnoreCase) > -1)
				&& ProcessArgumentBuilder.TryParse (args, out var parsedArgs)
				&& parsedArgs.Any (a => a.IndexOf ("langversion", StringComparison.OrdinalIgnoreCase) == 1);

		static string ToString (CSharpLangVersion version) => version switch {
			CSharpLangVersion.v5_0 => "5",
			CSharpLangVersion.v6_0 => "6",
			CSharpLangVersion.v7_0 => "7",
			CSharpLangVersion.v7_1 => "7.1",
			CSharpLangVersion.v7_2 => "7.2",
			CSharpLangVersion.v7_3 => "7.3",
			CSharpLangVersion.v8_0 => "8.0",
			CSharpLangVersion.v9_0 => "9.0",
			CSharpLangVersion.Latest => "latest",
			_ => throw new ArgumentException ($"Not a valid value: '{version}'", nameof (version))
		};

		public static string GetLangVersionArg (CodeCompilerArguments arguments, RuntimeInfo runtime)
		{
			if (!string.IsNullOrWhiteSpace (arguments.LangVersion)) {
				return $"-langversion:{arguments.LangVersion}";
			}

			if (HasLangVersionArg (arguments.AdditionalArguments)) {
				return null;
			}

			return $"-langversion:{ToString(GetBestSupportedLangVersion(runtime))}";
		}

		public static CSharpLangVersion? FromRoslynPackageVersion (string roslynPackageVersion)
			=> SemVersion.TryParse (roslynPackageVersion, out var version)
				? version switch {
					{ Major: > 3 } => CSharpLangVersion.v9_0,
					{ Major: 3, Minor: >= 8 } => CSharpLangVersion.v9_0,
					{ Major: 3, Minor: >= 3 } => CSharpLangVersion.v8_0,
					// ignore 8.0 preview support in 3.0-3.2
					{ Major: 2, Minor: >= 8 } => CSharpLangVersion.v7_3,
					{ Major: 2, Minor: >= 6 } => CSharpLangVersion.v7_2,
					{ Major: 2, Minor: >= 3 } => CSharpLangVersion.v7_1,
					{ Major: 2 } => CSharpLangVersion.v7_0,
					_ => CSharpLangVersion.v6_0
				}
				: null;

		//https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history
		public static CSharpLangVersion FromNetCoreSdkVersion (SemVersion sdkVersion)
			=> sdkVersion switch {
				{ Major: >= 5 } => CSharpLangVersion.v9_0,
				{ Major: 3 } => CSharpLangVersion.v8_0,
				{ Major: 2, Minor: >= 1 } => CSharpLangVersion.v7_3,
				{ Major: 2, Minor: >= 0 } => CSharpLangVersion.v7_1,
				_ => CSharpLangVersion.v7_0
			};
	}
}
