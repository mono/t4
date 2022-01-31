// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.TextTemplating
{
	static class StringUtil
	{
#if !NETCOREAPP2_1_OR_GREATER
		// required for CA2249
		public static bool Contains (this string str, char c) => str.IndexOf (c) > -1;
#endif
		public static bool IsNullOrWhiteSpace (this string value) => string.IsNullOrWhiteSpace (value);
		public static bool IsNullOrEmpty (this string value) => string.IsNullOrEmpty (value);

		public static string NullIfEmpty (this string value) => (value is null || value.Length == 0)? null : value;
	}
}
