// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

			foreach (var asmFile in assemblyFiles) {
				if (asmName.Name == Path.GetFileNameWithoutExtension (asmFile)) {
					return Assembly.LoadFrom (asmFile);
				}
			}

			var path = resolveAssemblyReference (asmName.Name + ".dll");
			if (File.Exists (path)) {
				return Assembly.LoadFrom (path);
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
