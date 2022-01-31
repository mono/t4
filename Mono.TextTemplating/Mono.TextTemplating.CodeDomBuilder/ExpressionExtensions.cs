// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

static class ExpressionExtensions
{
	public static CodeTypeReferenceExpression AsExpression (this CodeTypeReference typeReference) => new (typeReference);

	public static CodeFieldReferenceExpression AsReference (this CodeMemberField field, CodeExpression target) => new (target, field.Name);
	public static CodePropertyReferenceExpression AsReference (this CodeMemberProperty property, CodeExpression target) => new (target, property.Name);
	public static CodeMethodReferenceExpression AsReference (this CodeMemberMethod method, CodeExpression target) => new (target, method.Name);

	public static CodeFieldReferenceExpression OnThis (this CodeMemberField field) => field.AsReference (Expression.This);
	public static CodePropertyReferenceExpression OnThis (this CodeMemberProperty property) => property.AsReference (Expression.This);
	public static CodeMethodReferenceExpression OnThis (this CodeMemberMethod method) => method.AsReference (Expression.This);
	public static CodeArgumentReferenceExpression OnThis (this CodeParameterDeclarationExpression parameter) => new (parameter.Name);

	public static CodeFieldReferenceExpression Field (this CodeExpression target, string fieldName) => new (target, fieldName);
	public static CodeFieldReferenceExpression Field (this CodeExpression target, CodeMemberField field) => Field (target, field.Name);
	public static CodeFieldReferenceExpression Field (this CodeTypeReference type, string fieldName) => new (type.AsExpression (), fieldName);

	public static CodeAssignStatement SetField (this CodeExpression target, string fieldName, CodeExpression value) => target.Field (fieldName).Assign (value);
	public static CodeAssignStatement SetField (this CodeTypeReference type, string fieldName, CodeExpression value) => type.Field (fieldName).Assign (value);

	public static CodePropertyReferenceExpression Property (this CodeExpression target, string propertyName) => new (target, propertyName);
	public static CodePropertyReferenceExpression Property (this CodeExpression target, CodeMemberProperty property) => Property (target, property.Name);
	public static CodePropertyReferenceExpression Property (this CodeTypeReference type, string propertyName) => new (type.AsExpression (), propertyName);

	public static CodeAssignStatement SetProperty (this CodeExpression target, string propertyName, CodeExpression value) => target.Property (propertyName).Assign (value);
	public static CodeAssignStatement SetProperty (this CodeTypeReference type, string propertyName, CodeExpression value) => type.Property (propertyName).Assign (value);

	public static CodeMethodReferenceExpression Method (this CodeExpression target, string methodName) => new (target, methodName);
	public static CodeMethodReferenceExpression Method (this CodeExpression target, CodeMemberMethod method) => Method (target, method.Name);
	public static CodeMethodReferenceExpression Method (this CodeTypeReference type, string methodName) => new (type.AsExpression (), methodName);

	public static CodeMethodInvokeExpression InvokeMethod (this CodeExpression target, string methodName, params CodeExpression[] arguments) => new (target, methodName, arguments);
	public static CodeMethodInvokeExpression InvokeMethod (this CodeTypeReference target, string methodName, params CodeExpression[] arguments) => new (target.AsExpression (), methodName, arguments);

	public static CodeBinaryOperatorExpression IsNotNull (this CodeExpression reference) => new (reference, CodeBinaryOperatorType.IdentityInequality, Expression.Null);
	public static CodeBinaryOperatorExpression IsEqual (this CodeExpression reference, CodeExpression value) => new (reference, CodeBinaryOperatorType.IdentityEquality, value);
	public static CodeBinaryOperatorExpression IsEqualValue (this CodeExpression reference, CodeExpression value) => new (reference, CodeBinaryOperatorType.ValueEquality, value);
	public static CodeBinaryOperatorExpression IsNull (this CodeExpression reference) => reference.IsEqual (Expression.Null);
	public static CodeBinaryOperatorExpression IsFalse (this CodeExpression valueExpression) => valueExpression.IsEqualValue (Expression.False);
	public static CodeBinaryOperatorExpression And (this CodeExpression leftExpr, CodeExpression rightExpr) => new (leftExpr, CodeBinaryOperatorType.BooleanAnd, rightExpr);
	public static CodeBinaryOperatorExpression Subtract (this CodeExpression leftExpr, CodeExpression rightExpr) => new (leftExpr, CodeBinaryOperatorType.Subtract, rightExpr);
	public static CodeBinaryOperatorExpression Add (this CodeExpression leftExpr, CodeExpression rightExpr) => new (leftExpr, CodeBinaryOperatorType.Add, rightExpr);

	public static CodeExpressionStatement AsStatement (this CodeExpression expression) => new (expression);

	// this also exists on Expression w/ reversed args. intent is to support both Expression.Cast(type, expr) and expr.Cast(type)
	public static CodeCastExpression Cast (this CodeExpression expression, CodeTypeReference type) => new (type, expression);
}
