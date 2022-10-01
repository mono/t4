// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating;

partial class CompiledTemplate
{
	sealed class TemplateAssemblyContext : IDisposable
	{
#if FEATURE_ASSEMBLY_LOAD_CONTEXT
		readonly TemplateAssemblyLoadContext templateContext;
		public TemplateAssemblyContext (ITextTemplatingEngineHost host, string[] referenceAssemblyFiles) => templateContext = new (referenceAssemblyFiles, host);
		public Assembly LoadAssemblyFile (string assemblyPath) => templateContext.LoadFromAssemblyPath (assemblyPath);
		public Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData) => assemblyData.LoadInAssemblyLoadContext (templateContext);
		public void Dispose () { }
#else
		readonly CurrentDomainAssemblyResolver assemblyResolver;
		public TemplateAssemblyContext (ITextTemplatingEngineHost host, string[] referenceAssemblyFiles) => assemblyResolver = new (referenceAssemblyFiles, host.ResolveAssemblyReference);
		public Assembly LoadAssemblyFile (string assemblyPath) => Assembly.LoadFile (assemblyPath);
		public Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData) => assemblyData.LoadInCurrentAppDomain ();
		public void Dispose () => assemblyResolver.Dispose ();
#endif
	}
}
