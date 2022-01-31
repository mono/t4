// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

static class CodeMemberExtensions
{
	public static T WithMemberAttributesReplaced<T> (this T member, MemberAttributes attributes)
		where T : CodeTypeMember
	{
		member.Attributes = attributes;
		return member;
	}

	public static T WithMemberAttributesReplaced<T> (this T member, MemberAttributes attributesToRemove, MemberAttributes attributesToAdd) where T : CodeTypeMember
		=> member.WithMemberAttributesReplaced ((member.Attributes & ~attributesToRemove) | attributesToAdd);

	public static T WithMemberAttributesAdded<T> (this T member, MemberAttributes attributesToAdd) where T : CodeTypeMember
		=> member.WithMemberAttributesReplaced (member.Attributes | attributesToAdd);

	public static T WithMemberAttributesRemoved<T> (this T member, MemberAttributes attrbutesToRemove) where T : CodeTypeMember
		=> member.WithMemberAttributesReplaced (member.Attributes & ~attrbutesToRemove);

	public static T WithVisibility<T> (this T member, MemberAttributes visibilityAttributes) where T : CodeTypeMember
		=> member.WithMemberAttributesReplaced (MemberAttributes.AccessMask, visibilityAttributes & MemberAttributes.AccessMask);

	// KEEP IN SYNC.
	// Most of these don't apply to CodeTypeDeclaration, even though it's a subclass of CodeTypeMember, so they're
	// mirrored in CodeTypeExtensions with a version makes them either do what would be expected, or throw an
	// exception if they're not valid.
	public static T AsPrivate<T> (this T member) where T : CodeTypeMember => member.WithVisibility (MemberAttributes.Private);
	public static T AsPublic<T> (this T member) where T : CodeTypeMember => member.WithVisibility (MemberAttributes.Public);
	public static T AsProtected<T> (this T member) where T : CodeTypeMember => member.WithVisibility (MemberAttributes.Family);
	public static T AsInternal<T> (this T member) where T : CodeTypeMember => member.WithVisibility (MemberAttributes.Assembly);
	public static T AsInternalProtected<T> (this T member) where T : CodeTypeMember => member.WithVisibility (MemberAttributes.FamilyOrAssembly);

	public static T AsVirtual<T> (this T member) where T : CodeTypeMember => member.WithMemberAttributesRemoved (MemberAttributes.Final);
	public static T AsAbstract<T> (this T member) where T : CodeTypeMember => member.WithMemberAttributesRemoved (MemberAttributes.Abstract);
	public static T AsSealed<T> (this T member) where T : CodeTypeMember => member.WithMemberAttributesAdded (MemberAttributes.Final);
	public static T AsNew<T> (this T member) where T : CodeTypeMember => member.WithMemberAttributesAdded (MemberAttributes.New);
	public static T AsOverride<T> (this T member) where T : CodeTypeMember => member.WithMemberAttributesAdded (MemberAttributes.Override);
	// END SYNCED SECTION

	public static T AddTo<T> (this T member, CodeTypeDeclaration type)
		where T : CodeTypeMember
		=> type.AddMember (member);

	public static CodeAttributeDeclaration AddAttribute<T> (this T member, CodeAttributeDeclaration attribute) where T : CodeTypeMember
	{
		(member.CustomAttributes ??= new ()).Add (attribute);
		return attribute;
	}

	public static T WithAttribute<T> (this T member, CodeAttributeDeclaration attribute) where T : CodeTypeMember
	{
		(member.CustomAttributes ??= new ()).Add (attribute);
		return member;
	}

	public static T WithAttributes<T> (this T member, params CodeAttributeDeclaration[] attributes) where T : CodeTypeMember
	{
		(member.CustomAttributes ??= new ()).AddRange (attributes);
		return member;
	}

	public static T WithAttributes<T> (this T member, CodeAttributeDeclarationCollection attributes) where T : CodeTypeMember
	{
		(member.CustomAttributes ??= new ()).AddRange (attributes);
		return member;
	}

	public static void AddAttributes<T> (this T member, params CodeAttributeDeclaration[] attributes) where T : CodeTypeMember
		=> member.WithAttributes (attributes);

	public static void AddAttributes<T> (this T member, CodeAttributeDeclarationCollection attributes) where T : CodeTypeMember
		=> member.WithAttributes (attributes);

	public static CodeAttributeDeclaration AddAttribute<T> (this T member, CodeTypeReference attributeType) where T : CodeTypeMember
		=> member.AddAttribute (Declare.Attribute (attributeType));

	public static CodeAttributeDeclaration AddAttribute<T> (this T member, CodeTypeReference attributeType, params CodeAttributeArgument[] arguments) where T : CodeTypeMember
		=> member.AddAttribute (Declare.Attribute (attributeType, arguments));

	public static CodeAttributeDeclaration WithAttribute<TMember,TAttribute> (this TMember member) where TMember : CodeTypeMember
		=> member.AddAttribute (Declare.Attribute<TAttribute> ());

	public static CodeAttributeDeclaration WithAttribute<TMember, TAttribute> (this TMember member, params CodeAttributeArgument[] arguments) where TMember : CodeTypeMember
		=> member.AddAttribute (Declare.Attribute<TAttribute> (arguments));

	public static void AddTo<TMember> (this CodeAttributeDeclarationCollection attributes, TMember member) where TMember : CodeTypeMember
		=>member.WithAttributes (attributes);
}
