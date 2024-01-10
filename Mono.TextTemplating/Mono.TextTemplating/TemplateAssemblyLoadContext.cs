// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_ASSEMBLY_LOAD_CONTEXT

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	class TemplateAssemblyLoadContext : AssemblyLoadContext
	{
		readonly AssemblyLoadContext hostAssemblyLoadContext;

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
			hostAssembly = host.GetType ().Assembly;

			codeDomAsmName = typeof (CompilerErrorCollection).Assembly.GetName ();
			textTemplatingAsmName = typeof (TemplateGenerator).Assembly.GetName ();
			hostAsmName = hostAssembly.GetName ();

			// the parent load context is the context that loaded the host assembly
			this.hostAssemblyLoadContext = AssemblyLoadContext.GetLoadContext (hostAssembly);

			this.templateAssemblyFiles = templateAssemblyFiles;
			this.host = host;

			this.Resolving += ResolveAssembly;
		}

		// Load order is as follows:
		//
		// First, the Load(AssemblyName) override is called. Our impl of this ensures that the CodeDom and TextTemplating
		// and other host assemblies are loaded from the host AssemblyLoadContext, so that we can interchange types.
		//
		// For assemblies that are not handled by Load(AssemblyName), the runtime next attempts to resolve them
		// from AssemblyLoadContext.Default, which may load assemblies into AssemblyLoadContext.Default via
		// assembly probing. This is where runtime assemblies wil be loaded.
		//
		// Finally, if the runtime fails to resolve the assembly, the Resolving event is raised, which we handle
		// to resolve assemblies explicitly referenced by the template. The priority of this event is equivalent
		// to AppDomain.AssemblyResolve, so using this matches the behavior of the AppDomain codepath.
		//
		// See https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/loading-managed#algorithm

		protected override Assembly Load (AssemblyName assemblyName)
		{
			// The CodeDom and TextTemplating assemblies in the template load context MUST be the same as the
			// ones used by the host as we need to be able to reflect and cast and interchange types.
			// We cannot rely on falling back to resolving them from AssemblyLoadContext.Default, as this fails
			// in cases such as running the templating engine in an MSBuild task, as MSBuild loads tasks in
			// their own load contexts.
			if (assemblyName.Name == codeDomAsmName.Name) {
				return typeof (CompilerErrorCollection).Assembly;
			}
			if (assemblyName.Name == textTemplatingAsmName.Name) {
				return typeof (TemplateGenerator).Assembly;
			}

			// The host may be a custom host, and must also be in the same context, so that host-specific
			// templates can access the host instance.
			if (assemblyName.Name == hostAsmName.Name) {
				return hostAssembly;
			}

			// Resolve any more assemblies from the parent context that we can. There may be a custom host,
			// and it may expose types from other assemblies that may also need to be interchangeable.
			// Technically this loops makes the explicit checks above redundant but it's better
			// to be absolutely clear about what we're doing and why.
			var fromParent = hostAssemblyLoadContext.Assemblies.FirstOrDefault (a => a.GetName ().Name == assemblyName.Name);
			if (fromParent is not null) {
				return fromParent;
			}

			// let the runtime resolve from AssemblyLoadContext.Default
			return null;
		}

		Assembly ResolveAssembly (AssemblyLoadContext context, AssemblyName assemblyName)
		{
			// The list of assembly files referenced by the template may contain reference assemblies,
			// which will fail to load. Letting the host attempt to resolve the assembly first
			// gives it an opportunity to resolve runtime assemblies.
			var path = host.ResolveAssemblyReference (assemblyName.Name + ".dll");
			if (File.Exists (path)) {
				return LoadFromAssemblyPath (path);
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

			return null;
		}
	}
}

#endif