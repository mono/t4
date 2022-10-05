// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

static class CodeMemberPropertyExtensions
{
	public static CodeMemberProperty WithReference (this CodeMemberProperty property, out CodePropertyReferenceExpression reference)
	{
		reference = Expression.This.Property (property);
		return property;
	}

	public static CodeMemberProperty WithGet (this CodeMemberProperty property, CodeFieldReferenceExpression backingFieldRef)
		=> property.WithGet (backingFieldRef.Return ());

	public static CodeMemberProperty WithSet (this CodeMemberProperty property, CodeExpression backingFieldRef)
		=> property.WithSet (backingFieldRef.Assign (Expression.PropertySetValue));

	public static CodeMemberProperty WithGetSet (this CodeMemberProperty property, CodeFieldReferenceExpression backingFieldRef)
		=> property.WithGet (backingFieldRef.Return ())
				   .WithSet (backingFieldRef.Assign (Expression.PropertySetValue));

	public static CodeMemberProperty WithGet (this CodeMemberProperty property, CodeStatement statement)
	{
		property.GetStatements.Add (statement);
		return property;
	}

	public static CodeMemberProperty WithGet (this CodeMemberProperty property, params CodeStatement[] statements)
	{
		property.GetStatements.AddRange (statements);
		return property;
	}

	public static CodeMemberProperty WithSet (this CodeMemberProperty property, CodeStatement statement)
	{
		property.SetStatements.Add (statement);
		return property;
	}

	public static CodeMemberProperty WithSet (this CodeMemberProperty property, params CodeStatement[] statements)
	{
		property.SetStatements.AddRange (statements);
		return property;
	}

	public static CodeMemberProperty WithGetLazyInitialize (this CodeMemberProperty property, CodeMemberField field, CodeExpression init)
		=> property.WithGetLazyInitialize (field.AsReference (Expression.This), init);

	public static CodeMemberProperty WithGetLazyInitialize (this CodeMemberProperty property, CodeFieldReferenceExpression fieldRef, CodeExpression init)
		=> property.WithGet (
			Statement.If (fieldRef.IsNull (), Then: fieldRef.Assign (init)),
			Statement.Return (fieldRef));

	public static CodeMemberProperty WithSetIgnoresNull (this CodeMemberProperty property, CodeFieldReferenceExpression fieldRef)
		=> property.WithSet (
			Statement.If (Expression.PropertySetValue.IsNotNull (),
				Then: fieldRef.Assign (Expression.PropertySetValue)));
}
