using System;

namespace Mono.TextTemplating
{
	internal static class StringUtil
	{
		public static Boolean IsNullOrWhiteSpace (String value)
		{
			if (value == null) return true;

			for (int i = 0; i < value.Length; i++) {
				if (!Char.IsWhiteSpace (value[i])) return false;
			}

			return true;
		}
	}
}
