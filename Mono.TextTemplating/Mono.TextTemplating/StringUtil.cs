using System;
using System.ComponentModel;
using System.Reflection;

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

		public static TType GetValue<TType>(this PropertyInfo property, object @object, object[] index)
		{
			if (property.GetValue(@object, index) is TType success) {
				return success;
			}
			return default;
		}


		public static bool TryParse<TEnum> (this string value, out TEnum @enum)
			where TEnum: struct
		{
#if NET35
			if (Enum.Parse (typeof (TEnum), value) is TEnum success) {
				@enum = success;

				return true;
			}

			@enum = default;

			return false;
#else
			return Enum.TryParse (value, out @enum);
#endif
		}

	}
}
