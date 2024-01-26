// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK
#nullable enable annotations
#else
#nullable enable
#endif

using System;
using System.Linq;

namespace Mono.TextTemplating.CodeCompilation;

static class CSharpLangVersionHelper
{
	public static bool HasLangVersionArg (string args) =>
		!string.IsNullOrEmpty(args)
			&& (args.IndexOf ("langversion", StringComparison.OrdinalIgnoreCase) > -1)
			&& ProcessArgumentBuilder.TryParse (args, out var parsedArgs)
			&& parsedArgs.Any (IsLangVersionArg);

	public static bool IsLangVersionArg (string arg) =>
		(arg[0] == '-' || arg[0] == '/')
		&& arg.IndexOf ("langversion", StringComparison.OrdinalIgnoreCase) == 1;

	public static string? GetLangVersionArg (CodeCompilerArguments arguments, RuntimeInfo runtime)
	{
		// Arguments.LangVersion takes precedence over -langversion in arguments.AdditionalArguments.
		// This behavior should match that of CscCodeCompiler.CompileFile and RoslynCodeCompiler.CompileFileInternal
		if (!string.IsNullOrWhiteSpace (arguments.LangVersion)) {
			return $"-langversion:{arguments.LangVersion}";
		}

		if (HasLangVersionArg (arguments.AdditionalArguments)) {
			return null;
		}

		// Default to the highest language version supported by the runtime
		// as we may be using a csc from a newer runtime where "latest" language
		// features depend on new APIs that aren't available on the current runtime.
		// If we were unable to determine the supported language version for the runtime,
		// its MaxSupportedLangVersion will default to "Latest" so its language features
		// are available before we add a language version mapping for that runtime version.
		return $"-langversion:{ToString (runtime.CscMaxLangVersion)}";
	}

	//https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history
	public static CSharpLangVersion FromNetCoreSdkVersion (SemVersion sdkVersion)
		=> sdkVersion switch {
			{ Major: 9 } => CSharpLangVersion.v13_0,
			{ Major: 8 } => CSharpLangVersion.v12_0,
			{ Major: 7 } => CSharpLangVersion.v11_0,
			{ Major: 6 } => CSharpLangVersion.v10_0,
			{ Major: 5 } => CSharpLangVersion.v9_0,
			{ Major: 3 } => CSharpLangVersion.v8_0,
			{ Major: 2, Minor: >= 1 } => CSharpLangVersion.v7_3,
			{ Major: 2, Minor: >= 0 } => CSharpLangVersion.v7_1,
			{ Major: 1 } => CSharpLangVersion.v7_1,
			// for unknown versions, always fall through to "Latest" so we don't break the
			// ability to use new C# versions as they are released
			_ => CSharpLangVersion.Latest
		};

	public static string ToString (CSharpLangVersion version) => version switch {
		CSharpLangVersion.v5_0 => "5",
		CSharpLangVersion.v6_0 => "6",
		CSharpLangVersion.v7_0 => "7",
		CSharpLangVersion.v7_1 => "7.1",
		CSharpLangVersion.v7_2 => "7.2",
		CSharpLangVersion.v7_3 => "7.3",
		CSharpLangVersion.v8_0 => "8.0",
		CSharpLangVersion.v9_0 => "9.0",
		CSharpLangVersion.v10_0 => "10.0",
		CSharpLangVersion.v11_0 => "11.0",
		CSharpLangVersion.v12_0 => "12.0",
		CSharpLangVersion.v13_0 => "13.0",
		CSharpLangVersion.Latest => "latest",
		_ => throw new ArgumentException ($"Not a valid value: '{version}'", nameof (version))
	};
}
