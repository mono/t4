// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_ASSEMBLY_LOAD_CONTEXT
using System.IO;
using System.Runtime.Loader;
#endif

using System;
using System.Reflection;

namespace Mono.TextTemplating.CodeCompilation
{
	class CompiledAssemblyData
	{
		public byte[] Assembly { get; }
		public byte[] DebugSymbols { get; }

		public CompiledAssemblyData (byte[] assembly, byte[] debugSymbols)
		{
			Assembly = assembly ?? throw new System.ArgumentNullException (nameof (assembly));
			DebugSymbols = debugSymbols;
		}

#if FEATURE_ASSEMBLY_LOAD_CONTEXT
		public Assembly LoadInAssemblyLoadContext (AssemblyLoadContext loadContext)
		{
			if (DebugSymbols != null) {
				return loadContext.LoadFromStream (new MemoryStream (Assembly), new MemoryStream (DebugSymbols));
			} else {
				return loadContext.LoadFromStream (new MemoryStream (Assembly));
			}
		}
#else
		public Assembly LoadInCurrentAppDomain ()
		{
			if (DebugSymbols != null) {
				return System.Reflection.Assembly.Load (Assembly, DebugSymbols);
			} else {
				return System.Reflection.Assembly.Load (Assembly);
			}
		}
#endif
	}
}
