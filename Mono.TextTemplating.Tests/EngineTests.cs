//
// EngineTests.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2016 Xamarin Inc.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mono.TextTemplating.Tests
{
	[TestClass]
	public class EngineTests
	{
		#pragma warning disable 414
		static object [] ParameterParsingCases = {
			new object [] { "foo=bar",             true,  "",     "",    "foo", "bar"     },
			new object [] { "a=b",                 true,  "",     "",    "a",   "b"       },
			new object [] { "a=b=c",               true,  "",     "",    "a",   "b=c"     },
			new object [] { "!!c!d",               true,  "",     "",    "c",   "d"       },
			new object [] { "!!!",                 false, "",     "",    "",    ""        },
			new object [] { "a=",                  true,  "",     "",    "a",   ""        },
			new object [] { "=",                   false, "",     "",    "",    ""        },
			new object [] { "",                    false, "",     "",    "",    ""        },
			new object [] { "!",                   false, "",     "",    "",    ""        },
			new object [] { "a!",                  true,  "",     "",    "a",   ""        },
			new object [] { "!b!c!d",              true,  "",     "b",   "c",   "d"       },
			new object [] { "a!b!c!d",             true,  "a",    "b",   "c",   "d"       },
			new object [] { "a=b!c!d!e",           true,  "",     "",    "a",   "b!c!d!e" },
			new object [] { "a!b!c!d!e",           true,  "a",    "b",   "c",   "d!e"     },
			new object [] { "foo!bar!baz!wibb!le", true,  "foo", "bar", "baz",  "wibb!le" },
		};
#pragma warning restore 414

		[TestMethod]
		public void ParameterParsingTest()
		{
			foreach ( object[] parameters in ParameterParsingCases )
			{
				ParameterParsing( (string)parameters[0], (bool)parameters[ 1 ], (string)parameters[ 2 ], (string)parameters[ 3 ], (string)parameters[ 4 ], (string)parameters[ 5 ] );
			}
		}

		public void ParameterParsing (
			string parameter, bool valid,
			string expectedProcessor, string expectedDirective,
			string expectedName, string expectedValue)
		{
			string processor, directive, name, value;
			var success = TemplateGenerator.TryParseParameter (parameter, out processor, out directive, out name, out value);

			Assert.AreEqual (valid, success);
			Assert.AreEqual (expectedProcessor, processor);
			Assert.AreEqual (expectedDirective, directive);
			Assert.AreEqual (expectedName, name);
			Assert.AreEqual (expectedValue, value);
		}
	}
}
