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
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class ProcessingTests
	{
		[Fact]
		public async Task TemplateGeneratorTest ()
		{
			using var ctx = new TrackingSynchronizationContext ();

			var gen = new TemplateGenerator ();
			await gen.ProcessTemplateAsync (null, "<#@ template language=\"C#\" #>", null);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());

			ctx.AssertMaxCallCount (1);
		}

		[Fact]
		public async Task CSharp9Records ()
		{
			string template = "<#+ public record Foo(string bar); #>";
			var gen = new TemplateGenerator ();
			string outputName = null;
			await gen.ProcessTemplateAsync (null, template, outputName);

			CompilerError firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ();
#if NET5_0
			Assert.Null (firstError);
#else
			Assert.NotNull (firstError);
#endif
		}

#if !NET472
		[Fact]
		public async Task SetLangVersionViaAttribute ()
		{
			string template = "<#@ template langversion='5' #><#+ public int Foo { get; } = 5; #>";
			var gen = new TemplateGenerator ();
			await gen.ProcessTemplateAsync (null, template, null);

			CompilerError firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ();

			Assert.NotNull (firstError);
			Assert.True (gen.Errors.OfType<CompilerError> ().All (c => c.ErrorText.Contains ("not available in C# 5")));
		}

		[Fact]
		public async Task SetLangVersionViaAttributeInProcess ()
		{
			string template = "<#@ template langversion='5' #><#+ public int Foo { get; } = 5; #>";
			var gen = new TemplateGenerator ();
			gen.UseInProcessCompiler ();
			await gen.ProcessTemplateAsync (null, template, null);

			CompilerError firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ();
			Assert.NotNull (firstError);
			Assert.True (gen.Errors.OfType<CompilerError> ().All (c => c.ErrorText.Contains ("not available in C# 5")));
		}

		[Fact]
		public async Task SetLangVersionViaAdditionalArgs ()
		{
			string template = "<#@ template compilerOptions='-langversion:5' #><#+ public int Foo { get; } = 5; #>";
			var gen = new TemplateGenerator ();
			await gen.ProcessTemplateAsync (null, template, null);

			CompilerError firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ();
			Assert.NotNull (firstError);
			Assert.True (gen.Errors.OfType<CompilerError> ().All (c => c.ErrorText.Contains ("not available in C# 5")));
		}

		[Fact]
		public async Task SetLangVersionViaAdditionalArgsInProcess ()
		{
			string template = "<#@ template compilerOptions='-langversion:5' #><#+ public int Foo { get; } = 5; #>";
			var gen = new TemplateGenerator ();
			gen.UseInProcessCompiler ();
			await gen.ProcessTemplateAsync (null, template, null);

			CompilerError firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ();
			Assert.NotNull (firstError);
			Assert.Contains ("not available in C# 5", firstError.ErrorText);
		}
#endif

		[Fact]
		public async Task ImportReferencesTest ()
		{
			var gen = new TemplateGenerator ();
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Uri).Assembly.Location));
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Enumerable).Assembly.Location));
			await gen.ProcessTemplateAsync (null, "<#@ assembly name=\"System.dll\" #>\n<#@ assembly name=\"System.Core.dll\" #>", null);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		}

		[Fact]
		public async Task InProcessCompilerTest ()
		{
			using var ctx = new TrackingSynchronizationContext ();

			var gen = new TemplateGenerator ();
			gen.UseInProcessCompiler ();
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Uri).Assembly.Location));
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Enumerable).Assembly.Location));
			await gen.ProcessTemplateAsync (null, "<#@ assembly name=\"System.dll\" #>\n<#@ assembly name=\"System.Core.dll\" #>", null);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());

			ctx.AssertMaxCallCount (1);
		}

		[Fact]
		public async Task InProcessCompilerDebugTest ()
		{
			var gen = new TemplateGenerator ();
			gen.UseInProcessCompiler ();
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Uri).Assembly.Location));
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Enumerable).Assembly.Location));
			await gen.ProcessTemplateAsync (null, "<#@ template debug=\"true\" #><#@ assembly name=\"System.dll\" #>\n<#@ assembly name=\"System.Core.dll\" #>", null);
			Assert.Null (gen.Errors.OfType<CompilerError> ().FirstOrDefault ());
		}

		[Fact]
		public async Task IncludeFileThatDoesNotExistTest ()
		{
			var gen = new TemplateGenerator ();
			await gen.ProcessTemplateAsync (null, "<#@ include file=\"none.tt\" #>", null);
			Assert.StartsWith ("Could not read included file 'none.tt'", gen.Errors.OfType<CompilerError> ().First ().ErrorText);
		}
	}
}
