// 
// ITextTemplatingEngineHost.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if NETSTANDARD
using System.Runtime.Loader;
#endif
using System.Runtime.Serialization;
using System.Text;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating
{
	using Mono.VisualStudio.TextTemplating.VSHost;

	public interface IRecognizeHostSpecific
	{
		void SetProcessingRunIsHostSpecific (bool hostSpecific);
		bool RequiresProcessingRunIsHostSpecific { get; }
	}

	public interface ITextTemplatingService
		: ITextTemplatingEngineHost
		, ITextTemplatingSessionHost
		, ITextTemplatingComponents
		, IProcessTextTemplating
		, ITextTemplating
	{
		new IProcessTextTemplatingEngine Engine { get; }
	}

	public interface IProcessTextTemplating
		: ITextTemplating
	{
		event EventHandler<ProcessTemplateEventArgs> TransformProcessCompleted;
		void ProcessTemplateAsync (string inputFilename, string content, ITextTemplatingCallback callback, object hierarchy, bool debugging = false);
	}

	public interface ITextTemplating
	{
		void BeginErrorSession ();
		bool EndErrorSession ();
		string PreprocessTemplate (string inputFile, string content, ITextTemplatingCallback callback, string className, string classNamespace, out string[] references);
		string ProcessTemplate (string inputFile, string content, ITextTemplatingCallback callback = null, object hierarchy = null);
	}

	public interface IProcessTransformationRun
	{
		string PerformTransformation ();

		CompilerErrorCollection Errors { get; }
	}

	public interface IProcessTransformationRunFactory
	{
		IProcessTransformationRun CreateTransformationRun (Type runnerType, ParsedTemplate pt, ResolveEventHandler resolver);

		string RunTransformation (IProcessTransformationRun transformationRun);
	}

	public interface IProcessTextTemplatingEngine
		: ITextTemplatingEngine
	{
		IProcessTransformationRun PrepareTransformationRun (string content, ITextTemplatingEngineHost host, IProcessTransformationRunFactory runFactory, bool debugging = false);

		CompiledTemplate CompileTemplate (ParsedTemplate pt, string content, ITextTemplatingEngineHost host, TemplateSettings settings = null);
	}

	public interface ITextTemplatingEngine
	{
		string ProcessTemplate (string content, ITextTemplatingEngineHost host);
		string PreprocessTemplate (string content, ITextTemplatingEngineHost host, string className,
			string classNamespace, out string language, out string [] references);
	}

	public interface ITextTemplatingComponents
	{
		ITextTemplatingEngineHost Host { get; }

		ITextTemplatingEngine Engine { get; }

		string TemplateFile { get; set; }

		ITextTemplatingCallback Callback { get; set; }

		object Hierarchy { get; set; }
	}

	public interface ITextTemplatingCallback
	{
		bool Errors { get; set; }
		string Extension { get; }
		void ErrorCallback (bool warning, string message, int line, int column);
		void SetFileExtension (string extension);
		void SetOutputEncoding (Encoding encoding, bool fromOutputDirective);
		ITextTemplatingCallback DeepCopy ();

		Encoding OutputEncoding { get; }
	}

	public interface ITextTemplatingEngineHost
	{
		object GetHostOption (string optionName);
		bool LoadIncludeText (string requestFileName, out string content, out string location);
		void LogErrors (CompilerErrorCollection errors);
//FIXME: this break binary compat
#if FEATURE_APPDOMAINS
		AppDomain ProvideTemplatingAppDomain (string content);
#endif
		string ResolveAssemblyReference (string assemblyReference);
		Type ResolveDirectiveProcessor (string processorName);
		string ResolveParameterValue (string directiveId, string processorName, string parameterName);
		string ResolvePath (string path);
		void SetFileExtension (string extension);
		void SetOutputEncoding (Encoding encoding, bool fromOutputDirective);
		IList<string> StandardAssemblyReferences { get; }
		IList<string> StandardImports { get; }
		string TemplateFile { get; }
	}

	public interface ITextTemplatingSession :
		IEquatable<ITextTemplatingSession>, IEquatable<Guid>, IDictionary<string, Object>,
		ICollection<KeyValuePair<string, Object>>,
		IEnumerable<KeyValuePair<string, Object>>,
		IEnumerable, ISerializable
	{
		Guid Id { get; }
	}
	
	public interface ITextTemplatingSessionHost
		: ISerializable
	{
		ITextTemplatingSession CreateSession ();
		ITextTemplatingSession Session { get; set; }
	}

	public interface IDirectiveProcessor
	{
		CompilerErrorCollection Errors { get; }
		bool RequiresProcessingRunIsHostSpecific { get; }

		void FinishProcessingRun ();
		string GetClassCodeForProcessingRun ();
		string[] GetImportsForProcessingRun ();
		string GetPostInitializationCodeForProcessingRun ();
		string GetPreInitializationCodeForProcessingRun ();
		string[] GetReferencesForProcessingRun ();
		CodeAttributeDeclarationCollection GetTemplateClassCustomAttributes ();  //TODO
		void Initialize (ITextTemplatingEngineHost host);
		bool IsDirectiveSupported (string directiveName);
		void ProcessDirective (string directiveName, IDictionary<string, string> arguments);
		void SetProcessingRunIsHostSpecific (bool hostSpecific);
		void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors);
	}
}
