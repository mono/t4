using System;

namespace Mono.TextTemplating
{
	static class StringUtil
	{
		public static bool IsNullOrWhiteSpace (this string value) => string.IsNullOrWhiteSpace (value);
		public static bool IsNullOrEmpty (this string value) => string.IsNullOrEmpty (value);

		public static string NullIfEmpty (this string value) => (value is null || value.Length == 0)? null : value;
	}
}
