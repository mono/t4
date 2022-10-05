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
}

#endif