// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.TextTemplating;

/// <summary>
/// Implemented by directives that support code generation options.
/// Internal for now, until we have a better idea of what the API should look like.
/// </summary>
interface ISupportCodeGenerationOptions
{
	void SetCodeGenerationOptions (CodeGenerationOptions options);
}
