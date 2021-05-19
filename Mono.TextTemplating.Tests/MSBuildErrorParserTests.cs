//
// (C) 2013 Xamarin Inc.
// Copyright (c) Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xunit;

// this class is adapted from MonoTests.Microsoft.Build.Utilities.ToolTaskTest
// from mono/mcs/class/Microsoft.Build.Utilities/Test/Microsoft.Build.Utilities/ToolTaskTest.cs
namespace Mono.TextTemplating.Tests
{
	public class MSBuildErrorParserTests
	{
		void TestErrorParsing (string lineText, LogEvent expected)
		{
			var result = CodeCompilation.MSBuildErrorParser.TryParseLine (lineText);
			if (expected == null) {
				Assert.Null (result);
				return;
			}

			Assert.NotNull (result);
			Assert.Equal (expected.Origin, result.Origin);
			Assert.Equal (expected.Line, result.Line);
			Assert.Equal (expected.Column, result.Column);
			Assert.Equal (expected.EndLine, result.EndLine);
			Assert.Equal (expected.EndColumn, result.EndColumn);
			Assert.Equal (expected.IsError, result.IsError);
			Assert.Equal (expected.Subcategory ?? "", result.Subcategory);
			Assert.Equal (expected.Code, result.Code);
			Assert.Equal (expected.Message ?? "", result.Message);
		}

		[Fact]
		public void NoColon () => TestErrorParsing ("error   CS66", null);

		[Fact]
		public void Minimal () => TestErrorParsing ("error   CS66 : ", new LogEvent {
			IsError = true,
			Code = "CS66"
		});

		[Fact]
		public void InvalidCategory () => TestErrorParsing (
			"pineapple   CS66 : ",
			null
		);

		[Fact]
		public void CaseInsensitivity () => TestErrorParsing (
			"ERROR  CS66 : ",
			new LogEvent {
				IsError = true,
				Code = "CS66"
			}
		);

		[Fact]
		public void EmptyOrigin () => TestErrorParsing (
			": error  CS66 : ",
			new LogEvent {
				IsError = true,
				Code = "CS66"
			}
		);

		[Fact]
		public void BlankOrigin () => TestErrorParsing (
			"     : error  CS66 : ",
			new LogEvent {
				IsError = true,
				Code = "CS66"
			}
		);

		[Fact]
		public void NoOriginButErrorLikeMessage () => TestErrorParsing (
			"error   CS66 : error in 'hello:thing'",
			new LogEvent {
				IsError = true,
				Code = "CS66",
				Message = "error in 'hello:thing'",
			}
		);

		[Fact]
		public void Whitespace () => TestErrorParsing (
			"   C:\\class.cs   (23,344)  :    error   CS66   : blah    ",
			new LogEvent {
				Origin = "C:\\class.cs",
				Line = 23,
				Column = 344,
				IsError = true,
				Code = "CS66",
				Message = "blah",
			}
		);

		[Fact]
		public void RangeLineCol () => TestErrorParsing (
			"class1.cs(16,4): error CS0152: The label `case 1:' already occurs in this switch statement",
			new LogEvent {
				Origin = "class1.cs",
				Line = 16,
				Column = 4,
				IsError = true,
				Code = "CS0152",
				Message = "The label `case 1:' already occurs in this switch statement",
			}
		);

		[Fact]
		public void RangeLineColCol () => TestErrorParsing (
			"class1.cs(16,4-56): error X: blah",
			new LogEvent {
				Origin = "class1.cs",
				Line = 16,
				Column = 4,
				EndColumn = 56,
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void RangeLineColLineCol () => TestErrorParsing (
			"class1.cs(16,4,56,7): error X: blah",
			new LogEvent {
				Origin = "class1.cs",
				Line = 16,
				Column = 4,
				EndLine = 56,
				EndColumn = 7,
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void RangeLineLine () => TestErrorParsing (
			"class1.cs(1-77): error X: blah",
			new LogEvent {
				Origin = "class1.cs",
				Line = 1,
				EndLine = 77,
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void BadRangeTooManyDashes () => TestErrorParsing (
			"class1.cs(1-77-89): error X: blah",
			new LogEvent {
				Origin = "class1.cs",
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void BadRangePunctuation () => TestErrorParsing (
			"class1.cs(1&77-89): error X: blah",
			new LogEvent {
				Origin = "class1.cs(1&77-89)",
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void BadRangeAlpha () => TestErrorParsing (
			"class1.cs(ASDF): error X: blah",
			new LogEvent {
				Origin = "class1.cs(ASDF)",
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void BadRangeAlphaNumeric () => TestErrorParsing (
			"class1.cs(12AA45): error X: blah",
			new LogEvent {
				Origin = "class1.cs(12AA45)",
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void BadRangeLineLineColCol () => TestErrorParsing (
			"class1.cs(1-77,89-56): error X: blah",
			new LogEvent {
				Origin = "class1.cs",
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void BadRangeThreeCommas () => TestErrorParsing (
			"class1.cs(1,77,89): error X: blah",
			new LogEvent {
				Origin = "class1.cs",
				IsError = true,
				Code = "X",
				Message = "blah",
			}
		);

		[Fact]
		public void RangeZero () => TestErrorParsing (
			"class1.cs(0): error X:",
			new LogEvent {
				Origin = "class1.cs",
				IsError = true,
				Code = "X",
			}
		);

		[Fact]
		public void BadRangeOverflowCol () => TestErrorParsing (
			"class1.cs(2,1234567890192929293833838380): error X:",
			new LogEvent {
				Origin = "class1.cs",
				Line = 2,
				IsError = true,
				Code = "X",
			}
		);

		[Fact]
		public void BadRangeOverflowBeforeValues () => TestErrorParsing (
			"class1.cs(2,1234567890192929293833838380,5,7): error X:",
			new LogEvent {
				Line = 2,
				EndLine = 5,
				EndColumn = 7,
				Origin = "class1.cs",
				IsError = true,
				Code = "X",
			}
		);

		[Fact]
		public void LotsOfColons () => TestErrorParsing (
			"c:\\foo error XXX: fatal error YYY : error blah : thing",
			new LogEvent {
				Origin = "c:\\foo error XXX",
				IsError = true,
				Subcategory = "fatal",
				Code = "YYY",
				Message = "error blah : thing",
			}
		);

		[Fact]
		public void MSExample1 () => TestErrorParsing (
			"Main.cs(17,20): warning CS0168: The variable 'foo' is declared but never used",
			new LogEvent {
				Origin = "Main.cs",
				Line = 17,
				Column = 20,
				Code = "CS0168",
				Message = "The variable 'foo' is declared but never used",
			}
		);

		[Fact]
		public void MSExample2 () => TestErrorParsing (
			"C:\\dir1\\foo.resx(2) : error BC30188: Declaration expected.",
			new LogEvent {
				Origin = "C:\\dir1\\foo.resx",
				Line = 2,
				IsError = true,
				Code = "BC30188",
				Message = "Declaration expected.",
			}
		);

		[Fact]
		public void MSExample3 () => TestErrorParsing (
			"cl : Command line warning D4024 : unrecognized source file type 'foo.cs', object file assumed",
			new LogEvent {
				Origin = "cl",
				Subcategory = "Command line",
				Code = "D4024",
				Message = "unrecognized source file type 'foo.cs', object file assumed",
			}
		);

		[Fact]
		public void MSExample4 () => TestErrorParsing (
			"error CS0006: Metadata file 'System.dll' could not be found.",
			new LogEvent {
				IsError = true,
				Code = "CS0006",
				Message = "Metadata file 'System.dll' could not be found.",
			}
		);

		[Fact]
		public void MSExample5 () => TestErrorParsing (
			"C:\\sourcefile.cpp(134) : error C2143: syntax error : missing ';' before '}'",
			new LogEvent {
				Origin = "C:\\sourcefile.cpp",
				Line = 134,
				IsError = true,
				Code = "C2143",
				Message = "syntax error : missing ';' before '}'",
			}
		);

		[Fact]
		public void MSExample6 () => TestErrorParsing (
			"LINK : fatal error LNK1104: cannot open file 'somelib.lib'",
			new LogEvent {
				Origin = "LINK",
				Subcategory = "fatal",
				IsError = true,
				Code = "LNK1104",
				Message = "cannot open file 'somelib.lib'",
			}
		);

		[Fact]
		public void ParensInFilename () => TestErrorParsing (
			"/foo (bar)/baz/Component1.fs(3,5): error FS0201: Namespaces cannot contain values.",
			new LogEvent {
				Origin = "/foo (bar)/baz/Component1.fs",
				Line = 3,
				Column = 5,
				IsError = true,
				Code = "FS0201",
				Message = "Namespaces cannot contain values.",
			}
		);

		[Fact]
		public void SubcategoryNoOrigin () => TestErrorParsing (
			"fatal error XXX: stuff.",
			new LogEvent {
				IsError = true,
				Subcategory = "fatal",
				Code = "XXX",
				Message = "stuff.",
			}
		);

		[Fact]
		public void LocationNoOrigin () => TestErrorParsing (
			"(10,14): error CS1009: Unrecognized escape sequence",
			new LogEvent {
				IsError = true,
				Code = "CS1009",
				Message = "Unrecognized escape sequence",
				Line = 10,
				Column = 14
			}
		);

		public class LogEvent
		{
			public string Origin { get; set; }
			public int Line { get; set; }
			public int Column { get; set; }
			public int EndLine { get; set; }
			public int EndColumn { get; set; }
			public string Subcategory { get; set; }
			public bool IsError { get; set; }
			public string Code { get; set; }
			public string Message { get; set; }
		}
	}
}
