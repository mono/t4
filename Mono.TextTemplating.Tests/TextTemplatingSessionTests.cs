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
using NUnit.Framework;

namespace Mono.TextTemplating.Tests
{
	[TestFixture]
	public class TextTemplatingSessionTests
	{
		#if FEATURE_APPDOMAINS
		[Test]
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

			Assert.AreEqual (guid, session.Id);
		}
		#endif

		public class CustomHost : TemplateGenerator {
			public int TestProperty {get; set; }
		}

		[Test]
		public void TestCustomHost ()
		{
			var gen = new CustomHost { TestProperty = 3 };
			gen.Refs.Add(typeof(CustomHost).Assembly.Location);
			gen.Imports.Add("Mono.TextTemplating.Tests");

			var outFilename = "test.txt";
			var success = gen.ProcessTemplate (
				"test.tt",
				"<#@ template hostspecific=\"true\" #><#= ((TextTemplatingSessionTests.CustomHost)Host).TestProperty * 5 #>",
				ref outFilename,
				out var outContent
				);
			Assert.True (success);
			Assert.AreEqual ("15", outContent);
		}

		public class CustomHostWithSpecificHostType : TemplateGenerator {
			public int TestProperty {get; set; }
			public override Type SpecificHostType => typeof(CustomHostWithSpecificHostType);
		}

		[Test]
		public void TestCustomHostWithSpecificHostType ()
		{
			var gen = new CustomHostWithSpecificHostType { TestProperty = 3 };
			gen.Refs.Add(typeof(CustomHostWithSpecificHostType).Assembly.Location);
			gen.Imports.Add("Mono.TextTemplating.Tests");

			var outFilename = "test.txt";
			var success = gen.ProcessTemplate (
				"test.tt",
				"<#@ template hostspecific=\"true\" #><#= Host.TestProperty * 5 #>",
				ref outFilename,
				out var outContent
				);
			Assert.True (success);
			Assert.AreEqual ("15", outContent);
		}

		public abstract class TestBaseClassWithSpecificHostType : Microsoft.VisualStudio.TextTemplating.TextTransformation
		{
			public TextTemplatingSessionTests.CustomHostWithSpecificHostType Host { get; set; }
			public int TestProperty => Host.TestProperty;
		}

		[Test]
		public void TestCustomBaseClassWithSpecificHostType ()
		{
			var gen = new CustomHostWithSpecificHostType { TestProperty = 17 };
			gen.Refs.Add(typeof(CustomHostWithSpecificHostType).Assembly.Location);
			gen.Imports.Add("Mono.TextTemplating.Tests");

			var outFilename = "test.txt";
			var success = gen.ProcessTemplate (
				"test.tt",
				"<#@ template hostspecific=\"trueFromBase\" inherits=\"TextTemplatingSessionTests.TestBaseClassWithSpecificHostType\" #><#= TestProperty * 2 #>",
				ref outFilename,
				out var outContent
				);
			Assert.True (success);
			Assert.AreEqual ("34", outContent);
		}


		[Test]
		public void HostSpecificNonStringParameter ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""System.Int32"" #>
<#=TestParam + 3#>";

			var gen = new TemplateGenerator ();
			gen.AddParameter (null, null, "TestParam", "5");
			var outFilename = "test.txt";
			var success = gen.ProcessTemplate ("test.tt", template, ref outFilename, out var outContent);
			Assert.True (success);
			Assert.AreEqual ("8", outContent);
		}

		[Test]
		public void HostSpecificStringParameter ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""string"" #>
Hello <#=TestParam#>!";

			var gen = new TemplateGenerator ();
			gen.AddParameter (null, null, "TestParam", "World");
			var outFilename = "test.txt";
			var success = gen.ProcessTemplate ("test.tt", template, ref outFilename, out var outContent);
			Assert.True (success);
			Assert.AreEqual ("Hello World!", outContent);
		}

		// check the generated parameters can access the host via SpecificHostType
		[Test]
		public void HostSpecificStringParameterWithSpecificHostType ()
		{
			string template =
@"<#@ template language=""C#"" hostspecific=""true"" #>
<#@ parameter name=""TestParam"" type=""string"" #>
Hello <#=TestParam#>!";

			var gen = new CustomHostWithSpecificHostType ();
			gen.AddParameter (null, null, "TestParam", "World");
			var outFilename = "test.txt";
			var success = gen.ProcessTemplate ("test.tt", template, ref outFilename, out var outContent);
			Assert.True (success);
			Assert.AreEqual ("Hello World!", outContent);
		}
	}
}

