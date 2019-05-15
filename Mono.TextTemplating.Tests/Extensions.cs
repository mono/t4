//
// Extensions.cs
//
// Author:
//       Atif Aziz
//
// Copyright (c) 2019 Atif Aziz
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

namespace Mono.TextTemplating.Tests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	static class Extensions
	{
		public static IEnumerable<string> SplitIntoLines(this string s)
		{
			if (s == null) throw new ArgumentNullException (nameof (s));

			return Iterator ();

			IEnumerable<string> Iterator ()
			{
				using (var reader = new StringReader (s)) {
					string line;
					while ((line = reader.ReadLine ()) != null) {
						yield return line;
					}
				}
			}
		}

		public static string RenormalizeLineEndingsTo(this string s, string eol)
		{
			if (s == null) throw new ArgumentNullException (nameof(s));

			var sb = new StringBuilder ();
			foreach (var line in s.SplitIntoLines ()) {
				sb.Append (line).Append (eol);
			}
			return sb.ToString ();
		}
	}
}
