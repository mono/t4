// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using System.Reflection;

namespace Mono.TextTemplating.CodeDomBuilder;

static class CodeTypeExtensions
{
	public static CodeTypeDeclaration WithTypeAttributesReplaced (this CodeTypeDeclaration type, TypeAttributes attributes)
	{
		type.TypeAttributes = attributes;
		return type;
	}

	public static CodeTypeDeclaration WithTypeAttributesReplaced (this CodeTypeDeclaration type, TypeAttributes attributesToRemove, TypeAttributes attributesToAdd)
		=> type.WithTypeAttributesReplaced ((type.TypeAttributes & ~attributesToRemove) | attributesToAdd);

	public static CodeTypeDeclaration WithTypeAttributesAdded (this CodeTypeDeclaration type, TypeAttributes attributesToAdd)
		=> type.WithTypeAttributesReplaced (type.TypeAttributes | attributesToAdd);

	public static CodeTypeDeclaration WithTypeAttributesRemoved (this CodeTypeDeclaration type, TypeAttributes attributesToRemove)
		=> type.WithTypeAttributesReplaced (type.TypeAttributes & ~attributesToRemove);

	public static CodeTypeDeclaration WithVisibility (this CodeTypeDeclaration type, TypeAttributes visibility)
		=> type.WithTypeAttributesReplaced (TypeAttributes.VisibilityMask, visibility);

	// KEEP IN SYNC.
	// CodeMemberExtensions contains a number of extension methods to set member attributes
	// This section mirrors the member attributes section in CodeMemberExtensions, with all the same methods.
	// Although CodeTypeDeclaration subclasses CodeTypeMember, only AsNew() is applicable to it.
	// So this makes them either do what would be expected, or throw an exception if they're not valid.
	// The ones that are not valid are also marked as Obsolete so they can be comple time checked.
	const string NotValidOnType = "This CodeMemberExtensions extension method is not valid on CodeTypeDeclaration";
	static CodeTypeDeclaration ThrowNotValid () => throw new InvalidOperationException (NotValidOnType);

	[Obsolete(NotValidOnType)]
	public static CodeTypeDeclaration AsPrivate (this CodeTypeDeclaration type) => ThrowNotValid ();
	public static CodeTypeDeclaration AsPublic (this CodeTypeDeclaration type) => type.WithVisibility(TypeAttributes.Public);
	[Obsolete (NotValidOnType)]
	public static CodeTypeDeclaration AsProtected (this CodeTypeDeclaration type) => ThrowNotValid ();
	public static CodeTypeDeclaration AsInternal (this CodeTypeDeclaration type) => type.WithVisibility (TypeAttributes.NotPublic);
	[Obsolete (NotValidOnType)]
	public static CodeTypeDeclaration AsInternalProtected (this CodeTypeDeclaration type) => ThrowNotValid ();

	[Obsolete (NotValidOnType)]
	public static CodeTypeDeclaration AsVirtual (this CodeTypeDeclaration type) => ThrowNotValid ();
	public static CodeTypeDeclaration AsAbstract (this CodeTypeDeclaration type) => type.WithTypeAttributesAdded (TypeAttributes.Abstract);
	public static CodeTypeDeclaration AsSealed (this CodeTypeDeclaration type) => type.WithTypeAttributesAdded (TypeAttributes.Sealed);
	public static CodeTypeDeclaration AsNew (this CodeTypeDeclaration member) => member.AsNew<CodeTypeDeclaration> ();
	[Obsolete (NotValidOnType)]
	public static CodeTypeDeclaration AsOverride (this CodeTypeDeclaration type) => ThrowNotValid ();
	// END SYNCED SECTION

	public static CodeTypeDeclaration AsNestedPublic (this CodeTypeDeclaration type) => type.WithVisibility (TypeAttributes.NestedPublic);
	public static CodeTypeDeclaration AsNestedProtected (this CodeTypeDeclaration type) => type.WithVisibility (TypeAttributes.NestedFamily);
	public static CodeTypeDeclaration AsNestedProtectedInternal (this CodeTypeDeclaration type) => type.WithVisibility (TypeAttributes.NestedFamORAssem);

	public static CodeTypeDeclaration AsPartial (this CodeTypeDeclaration type)
	{
		type.IsPartial = true;
		return type;
	}

	public static CodeMemberProperty AddProperty (this CodeTypeDeclaration type, string propertyName, CodeTypeReference propertyType)
		=> type.AddMember (Declare.Property (propertyName, propertyType));

	public static CodeMemberProperty AddProperty<T> (this CodeTypeDeclaration type, string propertyName)
		=> type.AddProperty (propertyName, TypeReference<T>.Global);

	public static CodeMemberProperty AddPropertyGetSet (this CodeTypeDeclaration type, string propertyName, CodeMemberField backingField)
		=> type.AddProperty (propertyName, backingField.Type)
			   .WithGetSet (Expression.This.Field (backingField));

	public static CodeMemberProperty AddPropertyGetOnly (this CodeTypeDeclaration type, string propertyName, CodeMemberField backingField)
		=> type.AddProperty (propertyName, backingField.Type)
			   .WithGet (Expression.This.Field (backingField));

	public static CodeMemberField AddField<T> (this CodeTypeDeclaration type, string fieldName, CodeExpression initExpression = null)
		=> AddField (type, fieldName, TypeReference<T>.Global, initExpression);

	public static CodeMemberField AddField (this CodeTypeDeclaration type, string fieldName, CodeTypeReference fieldType, CodeExpression init = null)
	{
		var field = Declare.Field (fieldName, fieldType);
		if (init != null) {
			field.InitExpression = init;
		}
		return type.AddMember (field);
	}

	public static CodeMemberMethod AddMethod (this CodeTypeDeclaration type, string methodName)
		=> type.AddMember (Declare.Method (methodName));

	public static CodeTypeDeclaration WithMember<T> (this CodeTypeDeclaration type, T member) where T : CodeTypeMember
	{
		type.Members.Add (member);
		return type;
	}

	public static T AddMember<T> (this CodeTypeDeclaration type, T member) where T : CodeTypeMember
	{
		type.Members.Add (member);
		return member;
	}
	public static CodeSnippetTypeMember AddSnippetMember (this CodeTypeDeclaration type, string value, CodeLinePragma location = null)
		=> type.AddMember (IndentHelpers.CreateSnippetMember (value, location));

	public static CodeTypeDeclaration WithReference (this CodeTypeDeclaration typeDeclaration, out CodeTypeReference typeReference)
	{
		typeReference = new CodeTypeReference (typeDeclaration.Name);
		return typeDeclaration;
	}

	public static CodeTypeDeclaration Inherits (this CodeTypeDeclaration typeDeclaration, CodeTypeReference baseClass)
	{
		typeDeclaration.BaseTypes.Add (baseClass);
		return typeDeclaration;
	}
}
