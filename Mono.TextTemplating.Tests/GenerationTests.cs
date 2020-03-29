// 
// GenerationTests.cs
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
using System.IO;
using Microsoft.VisualStudio.TextTemplating;
using System.Linq;
using System.CodeDom.Compiler;
using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class GenerationTests
	{	
		[Fact]
		public void TemplateGeneratorTest ()
		{
			var gen = new TemplateGenerator ();
			string tmp = null;
			gen.ProcessTemplate (null, "<#@ template language=\"C#\" #>", ref tmp, out tmp);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		}

		[Fact]
		public void ImportReferencesTest ()
		{
			var gen = new TemplateGenerator ();
			string tmp = null;
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Uri).Assembly.Location));
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (System.Linq.Enumerable).Assembly.Location));
			gen.ProcessTemplate (null, "<#@ assembly name=\"System.dll\" #>\n<#@ assembly name=\"System.Core.dll\" #>", ref tmp, out tmp);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		}

		[Fact]
		public void InProcessCompilerTest ()
		{
			var gen = new TemplateGenerator ();
			gen.UseInProcessCompiler ();
			string tmp = null;
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Uri).Assembly.Location));
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (System.Linq.Enumerable).Assembly.Location));
			gen.ProcessTemplate (null, "<#@ assembly name=\"System.dll\" #>\n<#@ assembly name=\"System.Core.dll\" #>", ref tmp, out tmp);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		}

		[Fact]
		public void IncludeFileThatDoesNotExistTest ()
		{
			var gen = new TemplateGenerator ();
			string tmp = null;
			gen.ProcessTemplate (null, "<#@ include file=\"none.tt\" #>", ref tmp, out tmp);
			Assert.StartsWith ("Could not read included file 'none.tt'", gen.Errors.OfType<CompilerError> ().First ().ErrorText);
		}

		[Fact]
		public void Generate ()
		{
			string Input = ParsingTests.ParseSample1.NormalizeNewlines ();
			string Output = OutputSample1.NormalizeEscapedNewlines ();
			GenerateOutput (Input, Output, "\n");
		}
		
		[Fact]
		public void GenerateMacNewlines ()
		{
			string MacInput = ParsingTests.ParseSample1.NormalizeNewlines ("\r");
			string MacOutput = OutputSample1.NormalizeEscapedNewlines ("\\r");
			GenerateOutput (MacInput, MacOutput, "\r");
		}
		
		[Fact]
		public void GenerateWindowsNewlines ()
		{
			string WinInput = ParsingTests.ParseSample1.NormalizeNewlines ("\r\n");
			string WinOutput = OutputSample1.NormalizeEscapedNewlines ("\\r\\n");
			GenerateOutput (WinInput, WinOutput, "\r\n");
		}

		[Fact]
		public void DefaultLanguage ()
		{
			var host = new DummyHost ();
			string template = @"<#= DateTime.Now #>";
			var pt = ParsedTemplate.FromText (template, host);
			Assert.Empty (host.Errors);
			TemplateSettings settings = TemplatingEngine.GetSettings (host, pt);
			Assert.Equal ("C#", settings.Language);
		}
		
		//NOTE: we set the newline property on the code generator so that the whole files has matching newlines,
		// in order to match the newlines in the verbatim code blocks
		void GenerateOutput (string input, string expectedOutput, string newline)
		{
			var host = new DummyHost ();
			string nameSpaceName = "Microsoft.VisualStudio.TextTemplating4f504ca0";
			string code = GenerateCode (host, input, nameSpaceName, newline);
			Assert.Empty (host.Errors);

			var generated = TemplatingEngineHelper.CleanCodeDom (code, newline);
			expectedOutput = TemplatingEngineHelper.CleanCodeDom (expectedOutput, newline);
			Assert.Equal (expectedOutput, generated);
		}
		
		#region Helpers
		
		string GenerateCode (ITextTemplatingEngineHost host, string content, string name, string generatorNewline)
		{
			var pt = ParsedTemplate.FromText (content, host);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			TemplateSettings settings = TemplatingEngine.GetSettings (host, pt);
			if (name != null)
				settings.Namespace = name;
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var ccu = TemplatingEngine.GenerateCompileUnit (host, content, pt, settings);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var opts = new CodeGeneratorOptions ();
			using (var writer = new System.IO.StringWriter ()) {
				writer.NewLine = generatorNewline;
				settings.Provider.GenerateCodeFromCompileUnit (ccu, writer, opts);
				return writer.ToString ();
			}
		}

		#endregion

		#region Expected output strings

		public static string OutputSample1 =
@"
namespace Microsoft.VisualStudio.TextTemplating4f504ca0 {
    
    
    public partial class GeneratedTextTransformation : global::Microsoft.VisualStudio.TextTemplating.TextTransformation {
        
        
        #line 9 """"

var s = ""baz \\#>"";

        #line default
        #line hidden
        
        public override string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 2 """"
            this.Write(""Line One\nLine Two\n"");
            
            #line default
            #line hidden
            
            #line 4 """"

var foo = 5;

            
            #line default
            #line hidden
            
            #line 7 """"
            this.Write(""Line Three "");
            
            #line default
            #line hidden
            
            #line 7 """"
            this.Write(global::Microsoft.VisualStudio.TextTemplating.ToStringHelper.ToStringWithCulture( bar ));
            
            #line default
            #line hidden
            
            #line 7 """"
            this.Write(""\nLine Four\n"");
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        public override void Initialize() {
            base.Initialize();
        }
    }
}
";
		#endregion
	}

	static class StringNormalizationExtensions
	{
		public static string NormalizeNewlines (this string s, string newLine = "\n") => s.Replace ("\r\n", "\n").Replace ("\n", newLine);

		public static string NormalizeEscapedNewlines (this string s, string escapedNewline = "\\n") => s.Replace ("\\r\\n", "\\n").Replace ("\\n", escapedNewline);
	}
}
