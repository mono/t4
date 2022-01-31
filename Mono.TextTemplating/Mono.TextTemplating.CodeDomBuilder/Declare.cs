// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;
using System.Reflection;

namespace Mono.TextTemplating.CodeDomBuilder;

static class Declare
{
	const TypeAttributes DefaultTypeAttributes = TypeAttributes.NotPublic;

	const MemberAttributes DefaultPropertyAttributes = MemberAttributes.Public | MemberAttributes.Final;
	const MemberAttributes DefaultFieldAttributes = MemberAttributes.Private;
	const MemberAttributes DefaultMethodAttributes = MemberAttributes.Public | MemberAttributes.Final;
	const MemberAttributes DefaultEventAttributes = MemberAttributes.Public | MemberAttributes.Final;
	public static CodeNamespace Namespace () => new ();
	public static CodeNamespace Namespace (string namespaceName) => new (namespaceName);

	public static CodeTypeDeclaration Class (string typeName)
		=> new (typeName) { IsClass = true, TypeAttributes = DefaultTypeAttributes };

	public static CodeMemberMethod Method (string methodName)
		=> new () { Name = methodName, Attributes = DefaultMethodAttributes };

	public static CodeMemberProperty Property<T> (string propertyName) => Property (propertyName, TypeReference<T>.Global);
	public static CodeMemberProperty Property (string propertyName, CodeTypeReference propertyType)
		=> new () { Name = propertyName, Attributes = DefaultPropertyAttributes, Type = propertyType };

	public static CodeMemberField Field<T> (string fieldName) => Field (fieldName, TypeReference<T>.Global);
	public static CodeMemberField Field (string fieldName, CodeTypeReference fieldType)
		=> new () { Name = fieldName, Type = fieldType, Attributes = DefaultFieldAttributes };

	public static CodeMemberEvent Event (string eventName, CodeTypeReference eventType)
		=> new () { Name = eventName, Type = eventType, Attributes = DefaultEventAttributes };
	public static CodeMemberEvent Event<T> (string eventName)
		=> Event (eventName, TypeReference<T>.Global);

	public static CodeParameterDeclarationExpression Parameter (string parameterName, CodeTypeReference parameterType) => new (parameterType, parameterName);
	public static CodeParameterDeclarationExpression Parameter<T> (string parameterName) => new (TypeReference<T>.Global, parameterName);

	public static CodeAttributeDeclaration Attribute (CodeTypeReference attributeType) => new (attributeType);
	public static CodeAttributeDeclaration Attribute<T> () => new (TypeReference<T>.Global);
	public static CodeAttributeDeclaration Attribute (CodeTypeReference attributeType, params CodeAttributeArgument[] arguments) => new (attributeType, arguments);
	public static CodeAttributeDeclaration Attribute<T> (params CodeAttributeArgument[] arguments) => new (TypeReference<T>.Global, arguments);

	public static CodeVariableDeclarationStatement Variable<T> (string variableName, CodeExpression initExpression, out CodeVariableReferenceExpression variableReference)
		=> Statement.DeclareVariable<T> (variableName, initExpression, out variableReference);
	public static CodeVariableDeclarationStatement Variable (string variableName, CodeTypeReference variableType, CodeExpression initExpression, out CodeVariableReferenceExpression variableReference)
		=> Statement.DeclareVariable (variableType, variableName, initExpression, out variableReference);
}
