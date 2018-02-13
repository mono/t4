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
using System.Text;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization;
using System.CodeDom;

namespace Microsoft.VisualStudio.TextTemplating
{
	public interface IRecognizeHostSpecific
	{
		void SetProcessingRunIsHostSpecific (bool hostSpecific);
		bool RequiresProcessingRunIsHostSpecific { get; }
	}
	
	public interface ITextTemplatingEngine
	{
		string ProcessTemplate (string content, ITextTemplatingEngineHost host);
		string PreprocessTemplate (string content, ITextTemplatingEngineHost host, string className, 
			string classNamespace, out string language, out string[] references);
	}
	
	public interface ITextTemplatingEngineHost
	{
		object GetHostOption (string optionName);
		bool LoadIncludeText (string requestFileName, out string content, out string location);
		void LogErrors (CompilerErrorCollection errors);
		AppDomain ProvideTemplatingAppDomain (string content);
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
		IEquatable<ITextTemplatingSession>, IEquatable<Guid>, IDictionary<string, object>, ISerializable
	{
		Guid Id { get; }
	}
	
	public interface ITextTemplatingSessionHost	
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
