using System;

namespace Mono.TextTemplating
{
	static class StringUtil
	{
#if !NETCOREAPP2_1_OR_GREATER
		// required for CA2249
		public static bool Contains (this string str, char c) => str.IndexOf (c) > -1;
#endif
	}
}
