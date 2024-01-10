// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !FEATURE_ASSEMBLY_LOAD_CONTEXT

using System;
using System.IO;
using System.Reflection;
namespace Mono.TextTemplating
{
	class CurrentDomainAssemblyResolver : IDisposable
	{
		readonly Func<string, string> resolveAssemblyReference;
		readonly string[] assemblyFiles;
		bool disposed;

		public CurrentDomainAssemblyResolver (string[] assemblyFiles, Func<string,string> resolveAssemblyReference)
		{
			this.resolveAssemblyReference = resolveAssemblyReference;
			this.assemblyFiles = assemblyFiles;

			AppDomain.CurrentDomain.AssemblyResolve += ResolveReferencedAssemblies;
		}

		Assembly ResolveReferencedAssemblies (object sender, ResolveEventArgs args)
		{
			var asmName = new AssemblyName (args.Name);

			// The list of assembly files referenced by the template may contain reference assemblies,
			// which will fail to load. Letting the host attempt to resolve the assembly first
			// gives it an opportunity to resolve runtime assemblies.
			var path = resolveAssemblyReference (asmName.Name + ".dll");
			if (File.Exists (path)) {
				return Assembly.LoadFrom (path);
			}

			foreach (var asmFile in assemblyFiles) {
				if (asmName.Name == Path.GetFileNameWithoutExtension (asmFile)) {
					return Assembly.LoadFrom (asmFile);
				}
			}

			return null;
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