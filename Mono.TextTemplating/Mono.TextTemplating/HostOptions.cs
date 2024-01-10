// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TextTemplating;

static class HostOptionExtensions
{
	const string DisableAlcOptionName = "DisableAssemblyLoadContext";

	static bool IsOptionTrue (this ITextTemplatingEngineHost host, string optionName) =>
		host.GetHostOption(optionName) is string optionVal
			&& (optionVal == "1" || optionVal.Equals("true", StringComparison.OrdinalIgnoreCase));

	public static bool IsAssemblyLoadContextDisabled (this ITextTemplatingEngineHost host) => host.IsOptionTrue (DisableAlcOptionName);
}