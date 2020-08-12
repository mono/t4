// 
// TextTransformation.cs
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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Mono.VisualStudio.TextTemplating
{
	public abstract class TextTransformation : IDisposable
	{
		Stack<int> indents;
		string currentIndent = string.Empty;
		CompilerErrorCollection errors;
		StringBuilder builder;
		bool endsWithNewline;
		
		public TextTransformation ()
		{
		}
		
		public virtual void Initialize ()
		{
		}
		
		public abstract string TransformText ();

#pragma warning disable CA2227 // Collection properties should be read only
		public virtual IDictionary<string, object> Session { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

		#region Errors

		public void Error (string message)
		{
			Errors.Add (new CompilerError ("", 0, 0, "", message));
		}
		
		public void Warning (string message)
		{
			Errors.Add (new CompilerError ("", 0, 0, "", message) { IsWarning = true });
		}
		
		protected internal CompilerErrorCollection Errors {
			get {
				if (errors == null)
					errors = new CompilerErrorCollection ();
				return errors;
			}
		}
		
		Stack<int> Indents {
			get {
				if (indents == null)
					indents = new Stack<int> ();
				return indents;
			}
		}
		
		#endregion
		
		#region Indents
		
		public string PopIndent ()
		{
			if (Indents.Count == 0) {
				return "";
			}
			int lastPos = currentIndent.Length - Indents.Pop ();
			string last = currentIndent.Substring (lastPos);
			currentIndent = currentIndent.Substring (0, lastPos);
			return last; 
		}
		
		public void PushIndent (string indent)
		{
			if (indent == null) {
				throw new ArgumentNullException (nameof (indent));
			}
			Indents.Push (indent.Length);
			currentIndent += indent;
		}
		
		public void ClearIndent ()
		{
			currentIndent = string.Empty;
			Indents.Clear ();
		}
		
		public string CurrentIndent {
			get { return currentIndent; }
		}
		
		#endregion
		
		#region Writing
		
		protected StringBuilder GenerationEnvironment {
			get {
				if (builder == null)
					builder = new StringBuilder ();
				return builder;
			}
			set {
				builder = value;
			}
		}
		
		public void Write (string textToAppend)
		{
			if (string.IsNullOrEmpty (textToAppend))
				return;
			
			if ((GenerationEnvironment.Length == 0 || endsWithNewline) && CurrentIndent.Length > 0) {
				GenerationEnvironment.Append (CurrentIndent);
			}
			endsWithNewline = false;
			
			char last = textToAppend[textToAppend.Length-1];
			if (last == '\n' || last == '\r') {
				endsWithNewline = true;
			}
			
			if (CurrentIndent.Length == 0) {
				GenerationEnvironment.Append (textToAppend);
				return;
			}
			
			//insert CurrentIndent after every newline (\n, \r, \r\n)
			//but if there's one at the end of the string, ignore it, it'll be handled next time thanks to endsWithNewline
			int lastNewline = 0;
			for (int i = 0; i < textToAppend.Length - 1; i++) {
				char c = textToAppend[i];
				if (c == '\r') {
					if (textToAppend[i + 1] == '\n') {
						i++;
						if (i == textToAppend.Length - 1)
							break;
					}
				} else if (c != '\n') {
					continue;
				}
				i++;
				int len = i - lastNewline;
				if (len > 0) {
					GenerationEnvironment.Append (textToAppend, lastNewline, i - lastNewline);
				}
				GenerationEnvironment.Append (CurrentIndent);
				lastNewline = i;
			}
			if (lastNewline > 0)
				GenerationEnvironment.Append (textToAppend, lastNewline, textToAppend.Length - lastNewline);
			else
				GenerationEnvironment.Append (textToAppend);
		}
		
		public void Write (string format, params object[] args)
		{
			Write (string.Format (CultureInfo.InvariantCulture, format, args));
		}
		
		public void WriteLine (string textToAppend)
		{
			Write (textToAppend);
			GenerationEnvironment.AppendLine ();
			endsWithNewline = true;
		}
		
		public void WriteLine (string format, params object[] args)
		{
			WriteLine (string.Format (CultureInfo.InvariantCulture, format, args));
		}

		#endregion
		
		#region Dispose
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		protected virtual void Dispose (bool disposing)
		{
		}
		
		~TextTransformation ()
		{
			Dispose (false);
		}

		internal static void AddRequiredReferences (IList<string> standardAssemblies)
		{
			if (standardAssemblies == null) {
				throw new ArgumentNullException (nameof (standardAssemblies));
			}

			string codeDom = typeof (CompilerErrorCollection).Assembly.Location;

			if (!standardAssemblies.Any (x => x.Equals (codeDom, StringComparison.CurrentCultureIgnoreCase))) {
				standardAssemblies.Add (codeDom);
			}
		}

		#endregion

	}
}
