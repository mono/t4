using System;
using System.IO;

namespace Mono.TextTemplating
{
	static class StringUtil
	{
		public static bool IsNullOrWhiteSpace(string value)
    	{
        	if (value == null) return true;
        	return string.IsNullOrEmpty(value.Trim());
    	}
	}
}
