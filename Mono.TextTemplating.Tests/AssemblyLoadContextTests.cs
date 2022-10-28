// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER

using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Xunit;

namespace Mono.TextTemplating.Tests;

public class AssemblyLoadContextTests : AssemblyLoadTests<(SnapshotSet<string> assembliesInDefaultContext, SnapshotSet<AssemblyLoadContext> allContexts)>
{
	protected override (SnapshotSet<string> assembliesInDefaultContext, SnapshotSet<AssemblyLoadContext> allContexts) GetInitialState () => (
			Snapshot.LoadedAssemblies (),
			Snapshot.AssemblyLoadContexts ()
		);

	protected override void VerifyFinalState ((SnapshotSet<string> assembliesInDefaultContext, SnapshotSet<AssemblyLoadContext> allContexts) state)
	{
		state.assembliesInDefaultContext.AssertUnchanged ();

		// ensure unloadable contexts are collected
		for (int i = 0; i < 10; i++) {
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
		}

		state.allContexts.AssertUnchanged ();
	}

	/// Issue #143: System.Text.Json is a framework assembly on .NET Core 3.0 and does not need to be specified by absolute path
	[Fact]
	public async Task LoadSystemTextJson ()
	{
		string template = "<#@ assembly name=\"System.Text.Json\" #><#=System.Text.Json.JsonValueKind.Array.ToString()#>";

		var gen = new TemplateGenerator ();
		(_, string content, _) = await gen.ProcessTemplateAsync (null, template, null);

		CompilerError firstError = gen.Errors.OfType<CompilerError> ().FirstOrDefault ();
		Assert.Null (firstError);

		Assert.Equal ("Array", content);
	}
}

#endif