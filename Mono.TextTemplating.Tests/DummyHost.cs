// 
// IncludeFileProviderHost.cs
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
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Tests
{

	public class DummyHost : ITextTemplatingEngineHost
	{
		public Dictionary<string, string> Locations { get; } = new ();
		public Dictionary<string, string> Contents { get; } = new ();
		public Dictionary<string, object> HostOptions { get; } = new ();
		public CompilerErrorCollection Errors { get; } = new ();
		public Dictionary<string, Type> DirectiveProcessors { get; } = new ();

		readonly List<string> standardAssemblyReferences = new ();
		readonly List<string> standardImports = new ();

		public virtual object GetHostOption (string optionName)
		{
			HostOptions.TryGetValue (optionName, out var option);
			return option;
		}

		public virtual bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			content = null;
			return Locations.TryGetValue (requestFileName, out location)
				&& Contents.TryGetValue (location, out content);
		}

		public virtual void LogErrors (CompilerErrorCollection errors) => Errors.AddRange (errors);

		public virtual AppDomain ProvideTemplatingAppDomain (string content) => null;

		public virtual string ResolveAssemblyReference (string assemblyReference) => throw new NotImplementedException ();

		public virtual Type ResolveDirectiveProcessor (string processorName)
		{
			DirectiveProcessors.TryGetValue (processorName, out Type t);
			return t;
		}

		public virtual string ResolveParameterValue (string directiveId, string processorName, string parameterName) => throw new NotImplementedException ();

		public virtual string ResolvePath (string path) => throw new NotImplementedException ();

		public virtual void SetFileExtension (string extension) => throw new NotImplementedException ();

		public virtual void SetOutputEncoding (System.Text.Encoding encoding, bool fromOutputDirective) => throw new NotImplementedException ();

		public virtual IList<string> StandardAssemblyReferences => standardAssemblyReferences;

		public virtual IList<string> StandardImports => standardImports;

		public virtual string TemplateFile { get; set; }
	}
}
