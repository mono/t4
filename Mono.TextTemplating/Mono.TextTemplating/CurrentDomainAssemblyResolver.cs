// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !FEATURE_ASSEMBLY_LOAD_CONTEXT

using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	class CurrentDomainAssemblyResolver : IDisposable
	{
		readonly ITextTemplatingEngineHost host;
		readonly string[] assemblyFiles;
		bool disposed;

		public CurrentDomainAssemblyResolver (ITextTemplatingEngineHost host, string[] assemblyFiles)
		{
			this.host = host;
			this.assemblyFiles = assemblyFiles;
		}

		Assembly ResolveReferencedAssemblies (object sender, ResolveEventArgs args)
		{
			AssemblyName asmName = new AssemblyName (args.Name);
			foreach (var asmFile in assemblyFiles) {
				if (asmName.Name == Path.GetFileNameWithoutExtension (asmFile))
					return Assembly.LoadFrom (asmFile);
			}

			var path = host.ResolveAssemblyReference (asmName.Name + ".dll");
			if (File.Exists (path))
				return Assembly.LoadFrom (path);

			return null;
		}

		public void RegisterForCurrentDomain ()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveReferencedAssemblies;
		}

		public void Dispose ()
		{
			if (!disposed) {
				AppDomain.CurrentDomain.AssemblyResolve -= ResolveReferencedAssemblies;
				disposed = true;
			}
		}
	}
}

#endif