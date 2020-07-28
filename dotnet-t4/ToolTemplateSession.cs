//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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
using Mono.VisualStudio.TextTemplating;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mono.TextTemplating
{
	class ToolTemplateSession : ITextTemplatingSession
	{
		readonly Dictionary<string, object> session = new Dictionary<string, object> ();
		readonly ToolTemplateGenerator toolTemplateGenerator;

		public ToolTemplateSession (ToolTemplateGenerator toolTemplateGenerator)
		{
			this.toolTemplateGenerator = toolTemplateGenerator;
		}

		public object this [string key] {
			get => session [key];
			set => session [key] = value;
		}

		public Guid Id { get; } = Guid.NewGuid ();
		public ICollection<string> Keys => session.Keys;
		public ICollection<object> Values => session.Values;
		public int Count => session.Count;
		public bool IsReadOnly => false;
		public void Add (string key, object value) => session.Add (key, value);
		public void Add (KeyValuePair<string, object> item) => session.Add (item.Key, item.Value);
		public void Clear () => session.Clear ();
		public bool Contains (KeyValuePair<string, object> item) => session.TryGetValue (item.Key, out object v) && item.Value == v;
		public bool ContainsKey (string key) => session.ContainsKey (key);
		public bool Remove (string key) => session.Remove (key);
		public bool Remove (KeyValuePair<string, object> item) => Contains(item) && session.Remove (item.Key);
		public bool Equals (ITextTemplatingSession other) => other != null && Id == other.Id;
		public bool Equals (Guid other) => Id.Equals (other);
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator () => session.GetEnumerator ();
		public bool TryGetValue (string key, out object value) => session.TryGetValue (key, out value);
		IEnumerator IEnumerable.GetEnumerator () => session.GetEnumerator ();

		public void CopyTo (KeyValuePair<string, object> [] array, int arrayIndex)
		{
			foreach (var v in session) {
				array [arrayIndex++] = v;
			}
		}

		public void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException ();
		}

	}
}
