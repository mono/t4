// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_ASSEMBLY_LOAD_CONTEXT

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	class TemplateAssemblyLoadContext : AssemblyLoadContext
	{
		readonly string[] templateAssemblyFiles;
		readonly ITextTemplatingEngineHost host;
		readonly Assembly hostAssembly;

		readonly AssemblyName codeDomAsmName;
		readonly AssemblyName textTemplatingAsmName;
		readonly AssemblyName hostAsmName;

		public TemplateAssemblyLoadContext (string[] templateAssemblyFiles, ITextTemplatingEngineHost host)
#if NETCOREAPP3_0_OR_GREATER
			: base (isCollectible: true)
#endif
		{
			this.templateAssemblyFiles = templateAssemblyFiles;
			this.host = host;
			hostAssembly = host.GetType ().Assembly;

			codeDomAsmName = typeof (CompilerErrorCollection).Assembly.GetName ();
			textTemplatingAsmName = typeof (TemplateGenerator).Assembly.GetName ();
			hostAsmName = hostAssembly.GetName ();
		}

		protected override Assembly Load (AssemblyName assemblyName)
		{
			// CodeDom and TextTemplating MUST come from the same context as the host as we need to be able to reflect and cast
			// this is an issue with MSBuild which loads tasks in their own load contexts
			if (assemblyName.Name == codeDomAsmName.Name) {
				return typeof (CompilerErrorCollection).Assembly;
			}
			if (assemblyName.Name == textTemplatingAsmName.Name) {
				return typeof (TemplateGenerator).Assembly;
			}

			// the host may be a custom host, and must also be in the same context
			if (assemblyName.Name == hostAsmName.Name) {
				return hostAssembly;
			}

			for (int i = 0; i < templateAssemblyFiles.Length; i++) {
				var asmFile = templateAssemblyFiles[i];
				if (asmFile is null) {
					continue;
				}
				if (MemoryExtensions.Equals (assemblyName.Name, Path.GetFileNameWithoutExtension (asmFile.AsSpan()), StringComparison.OrdinalIgnoreCase)) {
					// if the file doesn't exist, fall through to host.ResolveAssemblyReference
					if (File.Exists (asmFile)) {
						return LoadFromAssemblyPath (asmFile);
					} else {
						// null out the missing file so we don't check it exists again
						templateAssemblyFiles[i] = null;
						break;
					}
				}
			}

			var path = host.ResolveAssemblyReference (assemblyName.Name + ".dll");
			if (File.Exists (path)) {
				return LoadFromAssemblyPath (path);
			}

			return null;
		}
	}
}

#endif