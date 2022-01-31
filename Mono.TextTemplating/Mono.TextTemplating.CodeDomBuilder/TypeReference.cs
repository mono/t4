// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

static class TypeReference
{
	public static CodeTypeReference String => TypeReference<string>.Global;

	public static CodeTypeReference Void { get; } = Global (typeof (void));

	public static CodeTypeReference ParamArrayAttribute => TypeReference<System.ParamArrayAttribute>.Global;

	public static CodeTypeReference Global<T> () => new (typeof (T), CodeTypeReferenceOptions.GlobalReference);
	public static CodeTypeReference Global (System.Type type) => new (type, CodeTypeReferenceOptions.GlobalReference);
	public static CodeTypeReference Global (string type) => new (type, CodeTypeReferenceOptions.GlobalReference);

	public static CodeTypeReference Default<T> () => new (typeof (T));
	public static CodeTypeReference Default (System.Type type) => new (type);
	public static CodeTypeReference Default (string type) => new (type);
}

static class TypeReference<T>
{
	public static CodeTypeReference Global { get; } = TypeReference.Global<T> ();
}
