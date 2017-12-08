//
// FileUtil.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

namespace Mono.TextTemplating
{
	static class FileUtil
	{
		public static string AbsoluteToRelativePath( string absPath, string relTo )
		{
			string[] absDirs = absPath.Split( '\\' );
			string[] relDirs = relTo.Split( '\\' );

			// Get the shortest of the two paths
			int len = absDirs.Length < relDirs.Length ? absDirs.Length :
			relDirs.Length;

			// Use to determine where in the loop we exited
			int lastCommonRoot = -1;
			int index;

			// Find common root
			for ( index = 0; index < len; index++ )
			{
				if ( absDirs[ index ] == relDirs[ index ] ) lastCommonRoot = index;
				else break;
			}

			// If we didn't find a common prefix then throw
			if ( lastCommonRoot == -1 )
			{
				throw new ArgumentException( "Paths do not have a common base" );
			}

			// Build up the relative path
			StringBuilder relativePath = new StringBuilder();

			// Add on the ..
			for ( index = lastCommonRoot + 1; index < absDirs.Length; index++ )
			{
				if ( absDirs[ index ].Length > 0 ) relativePath.Append( "..\\" );
			}

			// Add on the folders
			for ( index = lastCommonRoot + 1; index < relDirs.Length - 1; index++ )
			{
				relativePath.Append( relDirs[ index ] + "\\" );
			}
			relativePath.Append( relDirs[ relDirs.Length - 1 ] );

			return relativePath.ToString();
		}
	}
}
