// 
// EncodingHelper.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Text;
using System.IO;

namespace Mono.VisualStudio.TextTemplating
{
	public static class EncodingHelper
	{
		public static Encoding GetEncoding (string filePath)
		{
			var bom = new byte[4];
			using (FileStream stream = File.OpenRead (filePath)) {
				stream.Read (bom, 0, bom.Length);
			}

			if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) { return Encoding.UTF7; }
			if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) { return Encoding.UTF8; }
			if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) { return Encoding.UTF32; } //UTF-32LE
			if (bom[0] == 0xff && bom[1] == 0xfe) { return Encoding.Unicode; } //UTF-16LE
			if (bom[0] == 0xfe && bom[1] == 0xff) { return Encoding.BigEndianUnicode; } //UTF-16BE
			if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) { return new UTF32Encoding (true, true); } //UTF-32BE

			return null;
		}
	}
}

