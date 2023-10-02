// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Build
{
	sealed class MSBuildTemplateSession : ITextTemplatingSession
	{
		readonly Dictionary<string, object> session = new Dictionary<string, object> ();
		readonly MSBuildTemplateGenerator toolTemplateGenerator;

		public MSBuildTemplateSession (MSBuildTemplateGenerator toolTemplateGenerator)
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
