// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using CBExpression = Mono.TextTemplating.CodeDomBuilder.Expression;

namespace Mono.TextTemplating.CodeDomBuilder;

static class Statement
{
	public static CodeConditionStatement If (CodeExpression condition, params CodeStatement[] Then) => new (condition, Then);
	public static CodeConditionStatement If (CodeExpression condition, CodeStatement[] Then, CodeStatement[] Else) => new (condition, Then, Else);

	public static CodeMethodReturnStatement Return (this CodeExpression returnExpression) => new (returnExpression);

	public static CodeAssignStatement Assign (this CodeExpression target, CodeExpression value) => new (target, value);

	public static CodeVariableDeclarationStatement DeclareVariable (CodeTypeReference variableType, string variableName, CodeExpression initExpression, out CodeVariableReferenceExpression variableReference)
	{
		variableReference = new CodeVariableReferenceExpression (variableName);
		return new (variableType, variableName, initExpression);
	}

	public static CodeVariableDeclarationStatement DeclareVariable<T> (string variableName, CodeExpression initExpression, out CodeVariableReferenceExpression variableReference)
		=> DeclareVariable (TypeReference<T>.Global, variableName, initExpression, out variableReference);

	public static CodeExpressionStatement Expression (CodeExpression expression) => new (expression);

	public static CodeStatement Throw<T> (params CodeExpression[] exceptionArgs)
		=> new CodeThrowExceptionStatement (new CodeObjectCreateExpression (TypeReference<T>.Global, exceptionArgs));

	public static CodeStatement ThrowIfNull (this CodeArgumentReferenceExpression argument)
		=> If (argument.IsNull (),
			Then: Throw<ArgumentNullException> (CBExpression.Primitive (argument.ParameterName)));

	public static CodeStatement ThrowIfNull (this CodeArgumentReferenceExpression argument, string message)
		=> If (argument.IsNull (),
			Then: Throw<ArgumentNullException> (CBExpression.Primitive (argument.ParameterName), CBExpression.Primitive (message)));

	public static CodeSnippetStatement Snippet (string statementText) => new (statementText);
}
