// 
// DirectiveProcessor.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.CodeDom;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating
{
	public abstract class DirectiveProcessor : IDirectiveProcessor
	{
		protected ITextTemplatingEngineHost Host { get; private set; }
		protected TemplateSettings Settings { get; private set; }

		CompilerErrorCollection errors;

		protected DirectiveProcessor ()
		{
		}
		
		public virtual void Initialize (ITextTemplatingEngineHost host, TemplateSettings settings)
		{
			this.Host = host ?? throw new ArgumentNullException (nameof (host));
			this.Settings = settings ?? throw new ArgumentNullException (nameof (settings));
		}
		
		public virtual void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			if (languageProvider == null) {
				throw new ArgumentNullException (nameof (languageProvider));
			}

			this.errors = errors;
		}
		
		public abstract void FinishProcessingRun ();
		public abstract string GetClassCodeForProcessingRun ();
		public abstract string[] GetImportsForProcessingRun ();
		public abstract string GetPostInitializationCodeForProcessingRun ();
		public abstract string GetPreInitializationCodeForProcessingRun ();
		public abstract string[] GetReferencesForProcessingRun ();
		public abstract bool IsDirectiveSupported (string directiveName);
		public abstract void ProcessDirective (string directiveName, IDictionary<string, string> arguments);

		public virtual CodeAttributeDeclarationCollection GetTemplateClassCustomAttributes ()
		{
			return null;
		}

		CompilerErrorCollection IDirectiveProcessor.Errors { get { return errors; } }

		void IDirectiveProcessor.SetProcessingRunIsHostSpecific (bool hostSpecific)
		{
		}

		bool IDirectiveProcessor.RequiresProcessingRunIsHostSpecific {
			get { return false; }
		}
	}
}
