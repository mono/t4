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
using System.Threading.Tasks;

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

		public class CustomHost : TemplateGenerator {
			public int TestProperty {get; set; }
		}

		[Fact]
		public async Task TestCustomHost ()
		{
			var gen = new CustomHost { TestProperty = 3 };
			gen.Refs.Add(typeof(CustomHost).Assembly.Location);
			gen.Imports.Add("Mono.TextTemplating.Tests");

			var outFilename = "test.txt";
			var result = await gen.ProcessTemplateAsync (
				"test.tt",
				"<#@ template hostspecific=\"true\" #><#= ((TextTemplatingSessionTests.CustomHost)Host).TestProperty * 5 #>",
				outFilename
				);
			Assert.True (result.success);
			Assert.Equal ("15", result.content);
		}

		public class CustomHostWithSpecificHostType : TemplateGenerator {
			public int TestProperty {get; set; }
			public override Type SpecificHostType => typeof(CustomHostWithSpecificHostType);
		}

		[Fact]
		public async Task TestCustomHostWithSpecificHostType ()
		{
			var gen = new CustomHostWithSpecificHostType { TestProperty = 3 };
			gen.Refs.Add(typeof(CustomHostWithSpecificHostType).Assembly.Location);
			gen.Imports.Add("Mono.TextTemplating.Tests");

			var outFilename = "test.txt";
			var result = await gen.ProcessTemplateAsync (
				"test.tt",
				"<#@ template hostspecific=\"true\" #><#= Host.TestProperty * 5 #>",
				outFilename
				);
			Assert.True (result.success);
			Assert.Equal ("15", result.content);
		}

		public abstract class TestBaseClassWithSpecificHostType : TextTransformation
		{
			public CustomHostWithSpecificHostType Host { get; set; }
			public int TestProperty => Host.TestProperty;
		}

		[Fact]
		public async Task TestCustomBaseClassWithSpecificHostType ()
		{
			var gen = new CustomHostWithSpecificHostType { TestProperty = 17 };
			gen.Refs.Add(typeof(CustomHostWithSpecificHostType).Assembly.Location);
			gen.Imports.Add("Mono.TextTemplating.Tests");

			var outFilename = "test.txt";
			var result = await gen.ProcessTemplateAsync (
				"test.tt",
				"<#@ template hostspecific=\"trueFromBase\" inherits=\"TextTemplatingSessionTests.TestBaseClassWithSpecificHostType\" #><#= TestProperty * 2 #>",
				outFilename
				);
			Assert.True (result.success);
			Assert.Equal ("34", result.content);
		}


		[Fact]
		public async Task HostSpecificNonStringParameter ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""System.Int32"" #>
<#=TestParam + 3#>";

			var gen = new TemplateGenerator ();
			gen.AddParameter (null, null, "TestParam", "5");
			var outFilename = "test.txt";
			var result = await gen.ProcessTemplateAsync ("test.tt", template, outFilename);
			Assert.True (result.success);
			Assert.Equal ("8", result.content);
		}

		[Fact]
		public async Task HostSpecificStringParameter ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""string"" #>
Hello <#=TestParam#>!";

			var gen = new TemplateGenerator ();
			gen.AddParameter (null, null, "TestParam", "World");
			var outFilename = "test.txt";
			var result = await gen.ProcessTemplateAsync ("test.tt", template, outFilename);
			Assert.True (result.success);
			Assert.Equal ("Hello World!", result.content);
		}

		// check the generated parameters can access the host via SpecificHostType
		[Fact]
		public async Task HostSpecificStringParameterWithSpecificHostType ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""string"" #>
Hello <#=TestParam#>!";

			var gen = new CustomHostWithSpecificHostType ();
			gen.AddParameter (null, null, "TestParam", "World");
			var outFilename = "test.txt";
			var result = await gen.ProcessTemplateAsync ("test.tt", template, outFilename);
			Assert.True (result.success);
			Assert.Equal ("Hello World!", result.content);
		}
	}
}

