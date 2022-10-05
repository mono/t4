// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Mono.TextTemplating
{
	[Serializable]
	class RoslynCodeCompilerException : Exception
	{
		public RoslynCodeCompilerException () { }
		public RoslynCodeCompilerException (string message) : base (message) { }
		public RoslynCodeCompilerException (string message, Exception inner) : base (message, inner) { }
		protected RoslynCodeCompilerException (SerializationInfo info, StreamingContext context) : base (info, context) { }
	}
}