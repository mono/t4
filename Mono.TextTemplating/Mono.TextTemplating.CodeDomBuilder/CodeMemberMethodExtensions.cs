// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

static class CodeMemberMethodExtensions
{
	public static CodeMemberMethod Returns (this CodeMemberMethod method, CodeTypeReference returnType)
	{
		method.ReturnType = returnType;
		return method;
	}

	public static CodeMemberMethod Returns<T> (this CodeMemberMethod method) => method.Returns (TypeReference<T>.Global);

	public static CodeMemberMethod WithParameter (this CodeMemberMethod method, CodeParameterDeclarationExpression parameter)
	{
		method.Parameters.Add (parameter);
		return method;
	}

	public static CodeMemberMethod WithParameters (this CodeMemberMethod method, params CodeParameterDeclarationExpression[] parameters)
	{
		method.Parameters.AddRange (parameters);
		return method;
	}

	public static CodeMemberMethod WithParameter (this CodeMemberMethod method, CodeParameterDeclarationExpression parameter, out CodeArgumentReferenceExpression parameterRef)
	{
		method.Parameters.Add (parameter);
		parameterRef = parameter.OnThis ();
		return method;
	}

	public static CodeMemberMethod WithParameter (this CodeMemberMethod method, string parameterName, CodeTypeReference parameterType, out CodeArgumentReferenceExpression parameterRef)
		=> method.WithParameter (Declare.Parameter (parameterName, parameterType), out parameterRef);

	public static CodeMemberMethod WithParameter<T> (this CodeMemberMethod method, string parameterName, out CodeArgumentReferenceExpression parameterRef)
		=> method.WithParameter (Declare.Parameter<T> (parameterName), out parameterRef);

	public static CodeParameterDeclarationExpression WithAttribute (this CodeParameterDeclarationExpression parameter, CodeAttributeDeclaration attribute)
	{
		parameter.CustomAttributes.Add (attribute);
		return parameter;
	}

	public static CodeParameterDeclarationExpression Params (this CodeParameterDeclarationExpression parameter)
		=> parameter.WithAttribute (Declare.Attribute (TypeReference.ParamArrayAttribute));

	public static CodeParameterDeclarationExpression WithReference (this CodeParameterDeclarationExpression parameter, out CodeArgumentReferenceExpression parameterRef)
	{
		parameterRef = parameter.OnThis ();
		return parameter;
	}

	public static CodeMemberMethod WithStatements (this CodeMemberMethod method, CodeExpression statement)
	{
		method.Statements.Add (statement);
		return method;
	}

	public static CodeMemberMethod WithStatements (this CodeMemberMethod method, CodeStatement statement)
	{
		method.Statements.Add (statement);
		return method;
	}

	public static CodeMemberMethod WithStatements (this CodeMemberMethod method, params CodeStatement[] statements)
	{
		method.Statements.AddRange (statements);
		return method;
	}
}
