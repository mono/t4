// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

static class Reference
{
	public static CodeTypeReference Type<T> () => new (typeof (T));
}

static class Expression
{
	public static CodePrimitiveExpression Null { get; } = new (null);
	public static CodePrimitiveExpression False { get; } = new (false);
	public static CodePrimitiveExpression True { get; } = new (true);

	public static CodePropertySetValueReferenceExpression PropertySetValue { get; } = new ();
	public static CodeThisReferenceExpression This { get; } = new ();
	public static CodeBaseReferenceExpression Base { get; } = new ();

	public static CodeFieldReferenceExpression StringEmpty { get; } = TypeReference.String.Field ("Empty");

	public static CodePrimitiveExpression NameOf (this CodeArgumentReferenceExpression argument) => new (argument.ParameterName);
	public static CodePrimitiveExpression Primitive (object value) => new (value);
	public static CodeVariableReferenceExpression Variable (string variableName) => new (variableName);
	public static CodeObjectCreateExpression New (this CodeTypeReference createType, params CodeExpression[] arguments) => new (createType, arguments);
	public static CodeObjectCreateExpression New<T> (params CodeExpression[] arguments) => new (TypeReference<T>.Global, arguments);
	public static CodeMethodInvokeExpression Invoke (this CodeMethodReferenceExpression method, params CodeExpression[] arguments) => new (method, arguments);
	public static CodeBinaryOperatorExpression Operate (this CodeExpression leftExpr, CodeBinaryOperatorType opType, CodeExpression rightExpr) => new (leftExpr, opType, rightExpr);
	public static CodeIndexerExpression Index (this CodeExpression target, params CodeExpression[] indices) => new (target, indices);

	public static CodeTypeReferenceExpression Type<T> () => new (TypeReference<T>.Global);

	public static CodeTypeOfExpression TypeOf (CodeTypeReference type) => new (type);
	public static CodeTypeOfExpression TypeOf<T> () => new (TypeReference<T>.Global);
	public static CodeTypeOfExpression TypeOf<T> (CodeTypeReference type) => new (type);

	public static CodeCastExpression Cast<T> (this CodeExpression expression) => new (TypeReference<T>.Global, expression);

	// this also exists on ExpressionExtensions w/ reversed args. intent is to support both Expression.Cast(type, expr) and expr.Cast(type)
	public static CodeCastExpression Cast (CodeTypeReference type, CodeExpression expression) => new (type, expression);

	public static CodeArrayCreateExpression Array (CodeTypeReference type, int size) => new (type, size);
	public static CodeArrayCreateExpression Array (CodeTypeReference type, params CodeExpression[] initializers) => new (type, initializers);
	public static CodeArrayCreateExpression Array<T> (int size) => new (TypeReference<T>.Global, size);
	public static CodeArrayCreateExpression Array<T> (params CodeExpression[] initializers) => new (TypeReference<T>.Global, initializers);

	internal static CodeSnippetExpression Snippet (string expressionText) => new (expressionText);
}
