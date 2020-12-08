//
// TextTemplatingSessionTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Reflection;
using Microsoft.VisualStudio.TextTemplating;
using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class TextTemplatingSessionTests
	{
		#if FEATURE_APPDOMAINS
		[Fact]
		public void AppDomainSerializationTest ()
		{
			var guid = Guid.NewGuid ();
			var appDomain = AppDomain.CreateDomain ("TextTemplatingSessionSerializationTestAppDomain");

			var session = (TextTemplatingSession)appDomain.CreateInstanceFromAndUnwrap (
				typeof(TextTemplatingSession).Assembly.Location,
				typeof(TextTemplatingSession).FullName,
				false,
				BindingFlags.Public | BindingFlags.Instance,
				null,
				new object[] { guid },
				null,
				null);

			Assert.Equal (guid, session.Id);
		}
		#endif

		class TestHost : TemplateGenerator { }

		[Fact]
		public void TestCustomHost ()
		{
			var gen = new TestHost ();
			var outFilename = "test.txt";
			var success = gen.ProcessTemplate (
				"test.tt",
				"<#@ template hostspecific=\"true\" #><#=Host.GetType().Name#>",
				ref outFilename,
				out var outContent
				);
			Assert.True (success);
			Assert.Equal ("TestHost", outContent);
		}

		[Fact]
		public void HostSpecificNonStringParameter ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""System.Int32"" #>
<#=TestParam + 3#>";

			var gen = new TestHost ();
			gen.AddParameter (null, null, "TestParam", "5");
			var outFilename = "test.txt";
			var success = gen.ProcessTemplate ("test.tt", template, ref outFilename, out var outContent);
			Assert.True (success);
			Assert.Equal ("8", outContent);
		}

		[Fact]
		public void HostSpecificStringParameter ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""string"" #>
Hello <#=TestParam#>!";

			var gen = new TestHost ();
			gen.AddParameter (null, null, "TestParam", "World");
			var outFilename = "test.txt";
			var success = gen.ProcessTemplate ("test.tt", template, ref outFilename, out var outContent);
			Assert.True (success);
			Assert.Equal ("Hello World!", outContent);
		}
	}
}

