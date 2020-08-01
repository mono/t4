// 
// ParameterDirectiveProcessor.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using Mono.TextTemplating;
#if NET45
using Mono.TextTemplating.CodeCompilation;
#endif

namespace Mono.VisualStudio.TextTemplating
{
	public sealed class ParameterDirectiveProcessor : DirectiveProcessor, IRecognizeHostSpecific
	{
		CodeDomProvider provider;
		
		bool hostSpecific;
		readonly List<CodeStatement> postStatements = new List<CodeStatement> ();
		readonly List<CodeTypeMember> members = new List<CodeTypeMember> ();
		
		public override void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			base.StartProcessingRun (languageProvider, templateContents, errors);
			provider = languageProvider;
			postStatements.Clear ();
			members.Clear ();
		}
		
		public override void FinishProcessingRun ()
		{
			var statement = new CodeConditionStatement (
				new CodeBinaryOperatorExpression (
					new CodePropertyReferenceExpression (
						new CodePropertyReferenceExpression (new CodeThisReferenceExpression (), "Errors"), "HasErrors"),
					CodeBinaryOperatorType.ValueEquality,
					new CodePrimitiveExpression (false)),
				postStatements.ToArray ());
			
			postStatements.Clear ();
			postStatements.Add (statement);
		}
		
		public override string GetClassCodeForProcessingRun ()
		{
			return TemplatingEngine.GenerateIndentedClassCode (provider, members);
		}
		
		public override string[] GetImportsForProcessingRun ()
		{
			return null;
		}
		
		public override string GetPostInitializationCodeForProcessingRun ()
		{
			return TemplatingEngine.IndentSnippetText (provider, StatementsToCode (postStatements), "            ");
		}
		
		public override string GetPreInitializationCodeForProcessingRun ()
		{
			return null;
		}
		
		string StatementsToCode (List<CodeStatement> statements)
		{
			var options = new CodeGeneratorOptions ();
			using (var sw = new StringWriter ()) {
				foreach (var statement in statements)
					provider.GenerateCodeFromStatement (statement, sw, options);
				return sw.ToString ();
			}
		}
		
		public override string[] GetReferencesForProcessingRun ()
		{
			return null;
		}
		
		public override bool IsDirectiveSupported (string directiveName)
		{
			return directiveName == "parameter";
		}

		static readonly Dictionary<string, string> BuiltinTypesMap = new Dictionary<string, string> {
			{ "bool", "System.Boolean" },
			{ "byte", "System.Byte" },
			{ "sbyte", "System.SByte" },
			{ "char", "System.Char" },
			{ "decimal", "System.Decimal" },
			{ "double", "System.Double" },
			{ "float ", "System.Single" },
			{ "int", "System.Int32" },
			{ "uint", "System.UInt32" },
			{ "long", "System.Int64" },
			{ "ulong", "System.UInt64" },
			{ "object", "System.Object" },
			{ "short", "System.Int16" },
			{ "ushort", "System.UInt16" },
			{ "string", "System.String" }
		};

		public static string MapTypeName (string typeName)
		{
			if (string.IsNullOrEmpty (typeName)) {
				return "System.String";
			}
			if (BuiltinTypesMap.TryGetValue (typeName, out string mappedType)) {
				return mappedType;
			}
			return typeName;
		}

		public override void ProcessDirective (string directiveName, IDictionary<string, string> arguments)
		{
			if (!arguments.TryGetValue ("name", out string name) || string.IsNullOrEmpty (name)) {
				throw new DirectiveProcessorException ("Parameter directive has no name argument");
			}

			arguments.TryGetValue ("type", out string type);
			type = MapTypeName (type);
			
			string fieldName = "_" + name + "Field";
			var typeRef = new CodeTypeReference (type);
			var thisRef = new CodeThisReferenceExpression ();
			var fieldRef = new CodeFieldReferenceExpression (thisRef, fieldName);
			
			var property = new CodeMemberProperty () {
				Name = name,
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				HasGet = true,
				HasSet = false,
				Type = typeRef
			};
			property.GetStatements.Add (new CodeMethodReturnStatement (fieldRef));
			members.Add (new CodeMemberField (typeRef, fieldName));
			members.Add (property);
			
			var valRef = new CodeVariableReferenceExpression ("data");
			var namePrimitive = new CodePrimitiveExpression (name);
			var sessionRef = new CodePropertyReferenceExpression (thisRef, "Session");
#if FEATURE_APPDOMAINS
			var callContextTypeRefExpr = new CodeTypeReferenceExpression ("System.Runtime.Remoting.Messaging.CallContext");
#endif
			var nullPrim = new CodePrimitiveExpression (null);

			bool hasAcquiredCheck = hostSpecific
#if FEATURE_APPDOMAINS
				|| true;
#endif
				;

			string acquiredName = "_" + name + "Acquired";
			var acquiredVariable = new CodeVariableDeclarationStatement (typeof (bool), acquiredName, new CodePrimitiveExpression (false));
			var acquiredVariableRef = new CodeVariableReferenceExpression (acquiredVariable.Name);
			if (hasAcquiredCheck) {
				postStatements.Add (acquiredVariable);
			}

			//checks the local called "data" can be cast and assigned to the field, and if successful, sets acquiredVariable to true
			var checkCastThenAssignVal = new CodeConditionStatement (
				new CodeMethodInvokeExpression (
					new CodeTypeOfExpression (typeRef), "IsAssignableFrom", new CodeMethodInvokeExpression (valRef, "GetType")),
				hasAcquiredCheck
					? new CodeStatement[] {
						new CodeAssignStatement (fieldRef, new CodeCastExpression (typeRef, valRef)),
						new CodeAssignStatement (acquiredVariableRef, new CodePrimitiveExpression (true)),
					}
					: new CodeStatement [] {
						new CodeAssignStatement (fieldRef, new CodeCastExpression (typeRef, valRef)),
					}
					,
				new CodeStatement[] {
					new CodeExpressionStatement (new CodeMethodInvokeExpression (thisRef, "Error",
					new CodePrimitiveExpression ("The type '" + type + "' of the parameter '" + name + 
						"' did not match the type passed to the template"))),
				});
			
			//tries to gets the value from the session
			var checkSession = new CodeConditionStatement (
				new CodeBinaryOperatorExpression (NotNull (sessionRef), CodeBinaryOperatorType.BooleanAnd,
					new CodeMethodInvokeExpression (sessionRef, "ContainsKey", namePrimitive)),
				new CodeVariableDeclarationStatement (typeof (object), "data", new CodeIndexerExpression (sessionRef, namePrimitive)),
				checkCastThenAssignVal);
			
			this.postStatements.Add (checkSession);
			
			//if acquiredVariable is false, tries to gets the value from the host
			if (hostSpecific) {
				var hostRef = new CodePropertyReferenceExpression (thisRef, "Host");
				var checkHost = new CodeConditionStatement (
					BooleanAnd (IsFalse (acquiredVariableRef), NotNull (hostRef)),
					new CodeVariableDeclarationStatement (typeof (string), "data",
						new CodeMethodInvokeExpression (hostRef, "ResolveParameterValue", nullPrim, nullPrim,  namePrimitive)),
					new CodeConditionStatement (
						NotNull (valRef),
						checkCastThenAssignVal));
				
				this.postStatements.Add (checkHost);
			}

#if FEATURE_APPDOMAINS
#if NET45
			if (Settings.RuntimeKind != RuntimeKind.NetCore) {
#endif
				//if acquiredVariable is false, tries to gets the value from the call context
				var checkCallContext = new CodeConditionStatement (
					IsFalse (acquiredVariableRef),
					new CodeVariableDeclarationStatement (typeof (object), "data",
						new CodeMethodInvokeExpression (callContextTypeRefExpr, "LogicalGetData", namePrimitive)),
					new CodeConditionStatement (NotNull (valRef), checkCastThenAssignVal));

				this.postStatements.Add (checkCallContext);
#if NET45
			}
#endif
#endif
		}
		
		static CodeBinaryOperatorExpression NotNull (CodeExpression reference)
		{
			return new CodeBinaryOperatorExpression (reference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression (null));
		}
		
		static CodeBinaryOperatorExpression IsFalse (CodeExpression expr)
		{
			return new CodeBinaryOperatorExpression (expr, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression (false));
		}
		
		static CodeBinaryOperatorExpression BooleanAnd (CodeExpression expr1, CodeExpression expr2)
		{
			return new CodeBinaryOperatorExpression (expr1, CodeBinaryOperatorType.BooleanAnd, expr2);
		}
		
		void IRecognizeHostSpecific.SetProcessingRunIsHostSpecific (bool hostSpecific)
		{
			this.hostSpecific = hostSpecific;
		}

		public bool RequiresProcessingRunIsHostSpecific {
			get { return false; }
		}
	}
}

