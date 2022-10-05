// 
// Test.cs
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
using System.IO;
using System.Runtime.CompilerServices;

using Xunit;

namespace Mono.TextTemplating.Tests
{
	public class ParsingTests
	{
		public const string ParseSample1 =
@"<#@ template language=""C#v3.5"" #>
Line One
Line Two
<#
var foo = 5;
#>
Line Three <#= bar #>
Line Four
<#+ 
var s = ""baz \\#>"";
#>
";

		[Fact]
		public void TokenTest ()
		{
			string tf = "test.input";
			var tk = new Tokeniser (tf, ParseSample1.NormalizeNewlines ());

			//line 1
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 1, 1), tk.Location);
			Assert.Equal (State.Content, tk.State);
			Assert.Equal ("", tk.Value);
			Assert.True (tk.Advance ());
			Assert.Equal (State.Directive, tk.State);
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 1, 5), tk.Location);
			Assert.Equal (State.DirectiveName, tk.State);
			Assert.Equal ("template", tk.Value);
			Assert.True (tk.Advance ());
			Assert.Equal (State.Directive, tk.State);
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 1, 14), tk.Location);
			Assert.Equal (State.DirectiveName, tk.State);
			Assert.Equal ("language", tk.Value);
			Assert.True (tk.Advance ());
			Assert.Equal (State.Directive, tk.State);
			Assert.True (tk.Advance ());
			Assert.Equal (State.DirectiveValue, tk.State);
			Assert.Equal (new Location (tf, 1, 23), tk.Location);
			Assert.Equal ("C#v3.5", tk.Value);
			Assert.True (tk.Advance ());
			Assert.Equal (State.Directive, tk.State);

			//line 2, 3
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 2, 1), tk.Location);
			Assert.Equal (State.Content, tk.State);
			Assert.Equal ("Line One\nLine Two\n", tk.Value);

			//line 4, 5, 6
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 4, 1), tk.TagStartLocation);
			Assert.Equal (new Location (tf, 4, 3), tk.Location);
			Assert.Equal (new Location (tf, 6, 3), tk.TagEndLocation);
			Assert.Equal (State.Block, tk.State);
			Assert.Equal ("\nvar foo = 5;\n", tk.Value);

			//line 7
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 7, 1), tk.Location);
			Assert.Equal (State.Content, tk.State);
			Assert.Equal ("Line Three ", tk.Value);
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 7, 12), tk.TagStartLocation);
			Assert.Equal (new Location (tf, 7, 15), tk.Location);
			Assert.Equal (new Location (tf, 7, 22), tk.TagEndLocation);
			Assert.Equal (State.Expression, tk.State);
			Assert.Equal (" bar ", tk.Value);

			//line 8
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 7, 22), tk.Location);
			Assert.Equal (State.Content, tk.State);
			Assert.Equal ("\nLine Four\n", tk.Value);

			//line 9, 10, 11
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 9, 1), tk.TagStartLocation);
			Assert.Equal (new Location (tf, 9, 4), tk.Location);
			Assert.Equal (new Location (tf, 11, 3), tk.TagEndLocation);
			Assert.Equal (State.Helper, tk.State);
			Assert.Equal (" \nvar s = \"baz \\\\#>\";\n", tk.Value);

			//line 12
			Assert.True (tk.Advance ());
			Assert.Equal (new Location (tf, 12, 1), tk.Location);
			Assert.Equal (State.Content, tk.State);
			Assert.Equal ("", tk.Value);

			//EOF
			Assert.False (tk.Advance ());
			Assert.Equal (new Location (tf, 12, 1), tk.Location);
			Assert.Equal (State.EOF, tk.State);
		}

		[Fact]
		public void ParseTest ()
		{
			string tf = "test.input";

			var pt = ParsedTemplate.FromTextInternal (
				ParseSample1.NormalizeNewlines (),
				new DummyHost { TemplateFile = tf }
			);

			Assert.Empty (pt.Errors);
			var content = new List<TemplateSegment> (pt.Content);
			var dirs = new List<Directive> (pt.Directives);

			Assert.Single (dirs);
			Assert.Equal (6, content.Count);

			Assert.Equal ("template", dirs[0].Name);
			Assert.Single (dirs[0].Attributes);
			Assert.Equal ("C#v3.5", dirs[0].Attributes["language"]);
			Assert.Equal (new Location (tf, 1, 1), dirs[0].TagStartLocation);
			Assert.Equal (new Location (tf, 1, 34), dirs[0].EndLocation);

			Assert.Equal ("Line One\nLine Two\n", content[0].Text);
			Assert.Equal ("\nvar foo = 5;\n", content[1].Text);
			Assert.Equal ("Line Three ", content[2].Text);
			Assert.Equal (" bar ", content[3].Text);
			Assert.Equal ("\nLine Four\n", content[4].Text);
			Assert.Equal (" \nvar s = \"baz \\\\#>\";\n", content[5].Text);

			Assert.Equal (SegmentType.Content, content[0].Type);
			Assert.Equal (SegmentType.Block, content[1].Type);
			Assert.Equal (SegmentType.Content, content[2].Type);
			Assert.Equal (SegmentType.Expression, content[3].Type);
			Assert.Equal (SegmentType.Content, content[4].Type);
			Assert.Equal (SegmentType.Helper, content[5].Type);

			Assert.Equal (new Location (tf, 4, 1), content[1].TagStartLocation);
			Assert.Equal (new Location (tf, 7, 12), content[3].TagStartLocation);
			Assert.Equal (new Location (tf, 9, 1), content[5].TagStartLocation);

			Assert.Equal (new Location (tf, 2, 1), content[0].StartLocation);
			Assert.Equal (new Location (tf, 4, 3), content[1].StartLocation);
			Assert.Equal (new Location (tf, 7, 1), content[2].StartLocation);
			Assert.Equal (new Location (tf, 7, 15), content[3].StartLocation);
			Assert.Equal (new Location (tf, 7, 22), content[4].StartLocation);
			Assert.Equal (new Location (tf, 9, 4), content[5].StartLocation);

			Assert.Equal (new Location (tf, 6, 3), content[1].EndLocation);
			Assert.Equal (new Location (tf, 7, 22), content[3].EndLocation);
			Assert.Equal (new Location (tf, 11, 3), content[5].EndLocation);
		}

		const string IncludeSample =
@"One
<#@ include file=""foo.ttinclude"" #>
Two
<#@ include file=""bar.ttinclude"" #>
Three
<#@ include file=""foo.ttinclude"" once=""true"" #>
Four
<#@ include file=""bar.ttinclude"" #>
Five
";

		const string FooIncludeName = "foo.ttinclude";
		const string FooInclude = "Foo\n";
		const string BarIncludeName = "bar.ttinclude";
		const string BarInclude = "Bar\n";

		[Fact]
		public void IncludeOnceTest ()
		{
			var host = new DummyHost ();
			host.Locations.Add (FooIncludeName, FooIncludeName);
			host.Contents.Add (FooIncludeName, FooInclude.NormalizeNewlines ());
			host.Locations.Add (BarIncludeName, BarIncludeName);
			host.Contents.Add (BarIncludeName, BarInclude.NormalizeNewlines ());

			var pt = ParsedTemplate.FromTextInternal (IncludeSample.NormalizeNewlines (), host);

			Assert.Empty (pt.Errors);
			var content = new List<TemplateSegment> (pt.Content);
			var dirs = new List<Directive> (pt.Directives);

			Assert.Empty (dirs);
			Assert.Collection (content,
				t => Assert.Equal ("One\n", t.Text),
				t => Assert.Equal ("Foo\n", t.Text),
				t => Assert.Equal ("Two\n", t.Text),
				t => Assert.Equal ("Bar\n", t.Text),
				t => Assert.Equal ("Three\n", t.Text),
				t => Assert.Equal ("Four\n", t.Text),
				t => Assert.Equal ("Bar\n", t.Text),
				t => Assert.Equal ("Five\n", t.Text)
			);
		}

		[Fact]
		public void RelativeInclude ()
		{
			var testFile = TestDataPath.Get ().Combine ("RelativeInclude.tt");
			var host = new TemplateGenerator ();
			var pt = host.ParseTemplate (testFile, File.ReadAllText (testFile));
			Assert.Collection (pt.Content, c => Assert.Equal("Hello", c.Text));
		}
	}
}
