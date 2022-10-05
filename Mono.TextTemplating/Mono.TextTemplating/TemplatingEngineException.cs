// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Mono.TextTemplating
{
	[Serializable]
	class TemplatingEngineException : Exception
	{
		public TemplatingEngineException () { }
		public TemplatingEngineException (string message) : base (message) { }
		public TemplatingEngineException (string message, Exception inner) : base (message, inner) { }
		protected TemplatingEngineException (SerializationInfo info, StreamingContext context) : base (info, context) { }
	}
}
