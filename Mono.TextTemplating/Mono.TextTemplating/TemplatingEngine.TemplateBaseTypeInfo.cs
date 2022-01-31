// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using Mono.TextTemplating.CodeDomBuilder;

namespace Mono.TextTemplating;

partial class TemplatingEngine
{
	// represents information that is known about the base class of the template
	// and contains the logic for generating the base class
	class TemplateBaseTypeInfo
	{
		public bool HasVirtualTransformMethod { get; private set; }
		public bool HasVirtualInitializeMethod { get; private set; }
		public CodeTypeDeclaration Declaration { get; private set; }
		public CodeTypeReference Reference { get; private set; }

		public static TemplateBaseTypeInfo FromSettings (TemplateSettings settings)
		{
			if (!string.IsNullOrEmpty (settings.Inherits)) {
				return new TemplateBaseTypeInfo {
					Reference = new CodeTypeReference (settings.Inherits),
					HasVirtualTransformMethod = true,
					HasVirtualInitializeMethod = true
				};
			}

			if (settings.IncludePreprocessingHelpers) {
				var generatedBaseType = GenerateBaseClassWithPreprocessingHelpers (settings.Name, settings.Provider);
				return new TemplateBaseTypeInfo {
					Reference = new CodeTypeReference (generatedBaseType.Name),
					Declaration = generatedBaseType,
					HasVirtualTransformMethod = false,
					HasVirtualInitializeMethod = false
				};
			}

			return new TemplateBaseTypeInfo {
				Reference = TypeReference<Microsoft.VisualStudio.TextTemplating.TextTransformation>.Global,
				HasVirtualTransformMethod = true,
				HasVirtualInitializeMethod = true
			};
		}

		static CodeTypeDeclaration GenerateBaseClassWithPreprocessingHelpers (string templateName, CodeDomProvider provider)
		{
			var baseClass = new CodeTypeDeclaration (templateName + "Base");
			GenerateProcessingHelpers (baseClass);
			AddToStringHelper (baseClass, provider);
			return baseClass;
		}

		static void GenerateProcessingHelpers (CodeTypeDeclaration type)
		{
			var builderFieldDef = type.AddField<StringBuilder> ("builder").WithReference (out var builder);
			var sessionFieldDef = type.AddField<IDictionary<string, object>> ("session");

			type.AddPropertyGetSet ("Session", sessionFieldDef).AsVirtual ();

			type.AddProperty<StringBuilder> ("GenerationEnvironment")
				.WithSet (builder)
				.WithGetLazyInitialize (builder, builderFieldDef.Type.New ());

			AddErrorHelpers (type);
			AddIndentHelpers (type);
			AddWriteHelpers (type);
		}

		static void AddErrorHelpers (CodeTypeDeclaration type)
		{
			var minusOne = Expression.Primitive (-1);

			var errors = type.AddProperty<CompilerErrorCollection> ("Errors").AsProtected ()
				.WithGetLazyInitialize (
					type.AddField<CompilerErrorCollection> ("errors"),
					init: Expression.New<CompilerErrorCollection> ())
				.OnThis ();

			type.AddMethod ("Error")
				.WithParameter<string> ("message", out var paramErrorMessage)
				.WithStatements (
					errors.InvokeMethod (
						"Add",
						Expression.New<CompilerError> (Expression.Null, minusOne, minusOne, Expression.Null, paramErrorMessage)
				));

			type.AddMethod ("Warning")
				.WithParameter ("message", TypeReference.String, out var paramWarningMessage)
				.WithStatements (
					Statement.DeclareVariable<CompilerError> ("val",
						Expression.New<CompilerError> (Expression.Null, minusOne, minusOne, Expression.Null, paramWarningMessage),
						out var val),
					val.SetProperty ("IsWarning", Expression.True),
					errors.InvokeMethod ("Add", val).AsStatement ()
				);
		}

		static void AddIndentHelpers (CodeTypeDeclaration type)
		{
			var zero = Expression.Primitive (0);

			type.AddPropertyGetOnly ("CurrentIndent",
				type.AddField ("currentIndent", TypeReference.String, init: Expression.StringEmpty)
					.WithReference (out var currentIndent));

			type.AddProperty<Stack<int>> ("Indents").AsPrivate ()
				.WithGetLazyInitialize (
					type.AddField<Stack<int>> ("indents"),
					Expression.New<Stack<int>> ())
				.WithReference (out var indents);

			type.AddMethod ("PopIndent").Returns<string> ()
				.WithStatements (
					Statement.If (indents.Property ("Count").IsEqualValue (zero),
						Then: Expression.StringEmpty.Return ()),
					Statement.DeclareVariable<int> ("lastPos",
						currentIndent.Property ("Length").Subtract (indents.InvokeMethod ("Pop")),
						out var lastPosRef),
					Statement.DeclareVariable (TypeReference.String, "last",
						currentIndent.InvokeMethod ("Substring", lastPosRef),
						out var lastRef),
					currentIndent.Assign (currentIndent.InvokeMethod ("Substring", zero, lastPosRef)),
					lastRef.Return ()
				);

			type.AddMethod ("PushIndent")
				.WithParameter ("indent", TypeReference.String, out var paramIndent)
				.WithStatements (
					indents.InvokeMethod ("Push", paramIndent.Property ("Length")).AsStatement (),
					currentIndent.Assign (currentIndent.Add (paramIndent))
				);

			type.AddMethod ("ClearIndent")
				.WithStatements (
					currentIndent.Assign (Expression.StringEmpty),
					indents.InvokeMethod ("Clear").AsStatement ()
				);
		}

		static void AddWriteHelpers (CodeTypeDeclaration type)
		{
			var generationEnvironment = Expression.This.Property ("GenerationEnvironment");
			var currentIndent = Expression.This.Field ("currentIndent");

			var textToAppendParamDef = Declare.Parameter<string> ("textToAppend").WithReference (out var textToAppend);
			var formatParamDef = Declare.Parameter<string> ("format").WithReference (out var format);
			var argsParamDef = Declare.Parameter<object[]> ("args").Params ().WithReference (out var args);

			type.AddMethod ("Write").WithParameter (textToAppendParamDef)
				.WithStatements (
					generationEnvironment.InvokeMethod ("Append", textToAppend));

			type.AddMethod ("Write").WithParameters (formatParamDef, argsParamDef)
				.WithStatements (
					generationEnvironment.InvokeMethod ("AppendFormat", format, args));

			type.AddMethod ("WriteLine").WithParameter (textToAppendParamDef)
				.WithStatements (
					generationEnvironment.InvokeMethod ("Append", currentIndent).AsStatement (),
					generationEnvironment.InvokeMethod ("AppendLine", textToAppend).AsStatement ()
				);

			type.AddMethod ("WriteLine").WithParameters (formatParamDef, argsParamDef)
				.WithStatements (
					generationEnvironment.InvokeMethod ("Append", currentIndent).AsStatement (),
					generationEnvironment.InvokeMethod ("AppendFormat", format, args).AsStatement (),
					generationEnvironment.InvokeMethod ("AppendLine").AsStatement ()
				);
		}

		static void AddToStringHelper (CodeTypeDeclaration type, CodeDomProvider provider)
		{
			var helperClass = Declare.Class ("ToStringInstanceHelper")
				.AsNestedPublic ()
				.WithReference (out var helperClassType);

			helperClass.AddField<IFormatProvider> ("formatProvider",
					TypeReference<System.Globalization.CultureInfo>.Global.Property ("InvariantCulture"))
				.WithReference (out var formatProvider);

			helperClass.AddProperty<IFormatProvider> ("FormatProvider")
				.WithGet (formatProvider)
				.WithSetIgnoresNull (formatProvider);

			helperClass.AddMethod ("ToStringWithCulture")
				.Returns<string> ()
				.WithParameter<object> ("objectToConvert", out var objectToConvert)
				.WithStatements (
					objectToConvert.ThrowIfNull (),
					Declare.Variable<Type> ("type", objectToConvert.InvokeMethod ("GetType"), out var objType),
					Declare.Variable<Type> ("iConvertibleType", Expression.TypeOf<IConvertible> (), out var iConvertibleType),
					Statement.If (iConvertibleType.InvokeMethod ("IsAssignableFrom", objType),
						Then: Statement.Return (objectToConvert.Cast<IConvertible> ().InvokeMethod ("ToString", formatProvider))),
					Declare.Variable<System.Reflection.MethodInfo> ("methInfo",
						objType.InvokeMethod ("GetMethod", Expression.Primitive ("ToString"), Expression.Array<Type> (iConvertibleType)),
						out var methInfoLocalRef),
					Statement.If (methInfoLocalRef.IsNotNull (),
						Then: Statement.Return (Expression.Cast<string> (
							methInfoLocalRef.InvokeMethod ("Invoke", objectToConvert, Expression.Array<object> (formatProvider))))),
					Statement.Return (objectToConvert.InvokeMethod ("ToString"))
				);

			var helperFieldName = provider.CreateValidIdentifier ("_toStringHelper");

			type.AddPropertyGetOnly ("ToStringHelper",
				type.AddField (helperFieldName, helperClassType, Expression.New (helperClassType)));

			type.AddMember (helperClass);
		}
	}
}
