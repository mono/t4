// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;

namespace Mono.TextTemplating.CodeDomBuilder;

// extension methods where there aren't enough to warrant a specific class
static class CodeDomExtensions
{
	public static CodeMemberField WithReference (this CodeMemberField field, out CodeFieldReferenceExpression reference)
	{
		reference = Expression.This.Field (field);
		return field;
	}

	public static CodeNamespace AddNamespace (this CodeCompileUnit ccu, CodeNamespace @namespace)
	{
		ccu.Namespaces.Add (@namespace);
		return @namespace;
	}

	public static CodeNamespace AddNamespace (this CodeCompileUnit ccu, string namespaceName = null)
		=> ccu.AddNamespace (namespaceName == null ? Declare.Namespace () : Declare.Namespace (namespaceName));

	public static CodeNamespaceImport AddImport (this CodeNamespace ns, CodeNamespaceImport namespaceToImport)
	{
		ns.Imports.Add (namespaceToImport);
		return namespaceToImport;
	}

	public static CodeNamespaceImport AddImport (this CodeNamespace ns, string namespaceToImport)
		=> ns.AddImport (new CodeNamespaceImport (namespaceToImport));

	public static CodeTypeDeclaration AddType (this CodeNamespace ns, CodeTypeDeclaration type)
	{
		ns.Types.Add (type);
		return type;
	}

	public static CodeTypeDeclaration AddClass (this CodeNamespace ns, string className) => ns.AddType (Declare.Class (className));
}
