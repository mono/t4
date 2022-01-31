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
using System.ComponentModel;
using System.IO;

using Mono.TextTemplating.CodeDomBuilder;

namespace Microsoft.VisualStudio.TextTemplating
{
	public sealed class ParameterDirectiveProcessor : DirectiveProcessor, IRecognizeHostSpecific
	{
		CodeDomProvider provider;
		
		bool hostSpecific;
		readonly List<CodeStatement> postStatements = new ();
		readonly List<CodeTypeMember> members = new ();
		
		public override void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			base.StartProcessingRun (languageProvider, templateContents, errors);
			provider = languageProvider;
			postStatements.Clear ();
			members.Clear ();
		}
		
		public override void FinishProcessingRun ()
		{
			var statement = Statement.If (
				Expression.This.Property ("Errors").Property ("HasErrors").IsEqualValue (Expression.False),
				Then: postStatements.ToArray ()
			);
			
			postStatements.Clear ();
			postStatements.Add (statement);
		}
		
		public override string GetClassCodeForProcessingRun ()
		{
			return IndentHelpers.GenerateIndentedClassCode (provider, members);
		}
		
		public override string[] GetImportsForProcessingRun ()
		{
			return null;
		}
		
		public override string GetPostInitializationCodeForProcessingRun ()
		{
			return IndentHelpers.IndentSnippetText (provider, StatementsToCode (postStatements), "            ");
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

			arguments.TryGetValue ("type", out string typeName);
			typeName = MapTypeName (typeName);
			var type = TypeReference.Default (typeName);

			members.Add (Declare.Field ($"_{name}Field", type).WithReference (out var fieldRef));
			members.Add (Declare.Property (name, type).WithGet (fieldRef));

			var data = Expression.Variable ("data");
			var namePrimitive = Expression.Primitive (name);
			var session = Expression.This.Property ("Session");

#if FEATURE_APPDOMAINS
			bool hasAcquiredCheck = true;
			var callContextType = TypeReference.Default ("System.Runtime.Remoting.Messaging.CallContext");
#else
			bool hasAcquiredCheck = hostSpecific;
#endif
			var acquiredVariable = Declare.Variable<bool> ($"_{name}Acquired", Expression.False, out var acquiredVariableRef);
			if (hasAcquiredCheck) {
				postStatements.Add (acquiredVariable);
			}

			//checks the local called "data" can be cast and assigned to the field, and if successful, sets acquiredVariable to true
			var checkCastThenAssignVal = Statement.If (
				Expression.TypeOf (type).InvokeMethod ("IsAssignableFrom", data.InvokeMethod ("GetType")),
				Then: hasAcquiredCheck ?
					new CodeStatement[] {
						Statement.Assign (fieldRef, Expression.Cast (type, data)),
						Statement.Assign (acquiredVariableRef, Expression.True)
					} :
					new CodeStatement[] {
						Statement.Assign (fieldRef, Expression.Cast (type, data)),
					},
				Else: new[] {
					Statement.Expression (Expression.This.InvokeMethod ("Error",
						Expression.Primitive ($"The type '{typeName}' of the parameter '{name}' did not match the type passed to the template")))
				});

			//tries to gets the value from the session
			postStatements.Add (Statement.If (session.IsNotNull ().And (session.InvokeMethod ("ContainsKey", namePrimitive)),
				Then: new CodeStatement[] {
					Statement.DeclareVariable<object> (data.VariableName, session.Index (namePrimitive), out _),
					checkCastThenAssignVal
				}));

			if (hostSpecific) {
				var convertAndAssign = typeName == "System.String"?
					new CodeStatement[] {
						Statement.Assign (fieldRef, data)
					} :
					new CodeStatement[] {
						Declare.Variable<TypeConverter> ("dataTypeConverter",
							TypeReference.Default<TypeDescriptor> ().InvokeMethod ("GetConverter", Expression.TypeOf (type)), out var converter),
						Statement.If (
							converter.IsNotNull ().And (converter.InvokeMethod ("CanConvertFrom", Expression.TypeOf<string> ())),
							Then: new[] {
								Statement.Assign (fieldRef, Expression.Cast (type, converter.InvokeMethod ("ConvertFromString", data))),
							},
							Else: new[] {
								Statement.Expression (Expression.This.InvokeMethod ("Error",
									Expression.Primitive ($"The host parameter '{name}' could not be converted to the type '{type}' specified in the template")))
							}
						)
					};

				// try to acquire parameter value from host
				var host = Expression.This.Property ("Host");
				postStatements.Add (
					Statement.If (acquiredVariableRef.IsFalse ().And (host.IsNotNull ()),
					Then: new CodeStatement[] {
						// if the host uses SpecificHostType, this may only be accessible via the interface
						Statement.DeclareVariable<string> (data.VariableName,
							host.Cast<ITextTemplatingEngineHost> ().InvokeMethod ("ResolveParameterValue", Expression.Null, Expression.Null,  namePrimitive), out _),
						Statement.If (data.IsNotNull (),
							Then: convertAndAssign)
					}));
			}

#if FEATURE_APPDOMAINS
			// try to acquire parameter value from call context
			postStatements.Add (
				Statement.If (acquiredVariableRef.IsFalse (),
				Then: new CodeStatement[] {
					Declare.Variable<object> (data.VariableName, callContextType.InvokeMethod ("LogicalGetData", namePrimitive), out _),
					Statement.If (data.IsNotNull (),
						Then: checkCastThenAssignVal)
				}));
#endif
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

