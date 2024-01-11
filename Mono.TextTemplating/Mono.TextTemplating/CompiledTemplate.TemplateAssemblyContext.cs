// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating;

partial class CompiledTemplate
{
	/// <summary>
	/// Abstracts over loading assemblies into an AssemblyLoadContext or AppDomain
	/// and resolving assemblies from the host.
	/// </summary>
	abstract class TemplateAssemblyContext : IDisposable
	{
		public abstract Assembly LoadAssemblyFile (string assemblyPath);
		public abstract Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData);
		public virtual void Dispose () { }

		[SuppressMessage ("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Conditionally compiled version of this returns multiple concrete types")]

		public static TemplateAssemblyContext Create (ITextTemplatingEngineHost host, string[] referenceAssemblyFiles)
		{
#if FEATURE_ASSEMBLY_LOAD_CONTEXT
			if (!host.IsAssemblyLoadContextDisabled ()) {
				return new AssemblyLoadContextTemplateAssemblyContext (host, referenceAssemblyFiles);
			}
#endif
			return new CurrentAppDomainTemplateAssemblyContext (host, referenceAssemblyFiles);
		}
	}

#if FEATURE_ASSEMBLY_LOAD_CONTEXT
	sealed class AssemblyLoadContextTemplateAssemblyContext : TemplateAssemblyContext
	{
		readonly TemplateAssemblyLoadContext templateContext;
		public AssemblyLoadContextTemplateAssemblyContext (ITextTemplatingEngineHost host, string[] referenceAssemblyFiles)
			=> templateContext = new (referenceAssemblyFiles, host);
		public override Assembly LoadAssemblyFile (string assemblyPath) => templateContext.LoadFromAssemblyPath (assemblyPath);
		public override Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData) => assemblyData.LoadInAssemblyLoadContext (templateContext);
	}
#endif

	sealed class CurrentAppDomainTemplateAssemblyContext : TemplateAssemblyContext
	{
		readonly CurrentDomainAssemblyResolver assemblyResolver;
		public CurrentAppDomainTemplateAssemblyContext (ITextTemplatingEngineHost host, string[] referenceAssemblyFiles)
			=> assemblyResolver = new (referenceAssemblyFiles, host.ResolveAssemblyReference);
		public override Assembly LoadAssemblyFile (string assemblyPath) => Assembly.LoadFile (assemblyPath);
		public override Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData) => assemblyData.LoadInCurrentAppDomain ();
		public override void Dispose ()
		{
			base.Dispose ();
			assemblyResolver.Dispose ();
		}
	}
}
