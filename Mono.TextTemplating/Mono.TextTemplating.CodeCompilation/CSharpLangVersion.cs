// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK
#nullable enable annotations
#else
#nullable enable
#endif

namespace Mono.TextTemplating.CodeCompilation;

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
	v10_0,
	v11_0,
	v12_0,
	v13_0,
	Latest = 1024 // make sure value doesn't change as we add new C# versions
}
