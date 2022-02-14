// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Mono.TextTemplating.Tests;

public abstract class Snapshot
{
	public abstract void AssertUnchanged ();

#if FEATURE_APPDOMAINS
	public static SnapshotSet<string> LoadedAssemblies (AppDomain context = null) => new (() => GetNames ((context ?? AppDomain.CurrentDomain).GetAssemblies ()));
#elif NETCOREAPP3_0_OR_GREATER
	public static SnapshotSet<string> LoadedAssemblies (AssemblyLoadContext context = null) => new (() => GetNames ((context ?? AssemblyLoadContext.Default).Assemblies));
	public static SnapshotSet<AssemblyLoadContext> AssemblyLoadContexts () => new (() => AssemblyLoadContext.All);
#endif

	static IEnumerable<string> GetNames (IEnumerable<Assembly> assemblies)
	{
		var names = assemblies.Select (a => a.FullName);
		if (!System.Diagnostics.Debugger.IsAttached) {
			return names;
		}
		return names.Where (a => !a.StartsWith ("Microsoft.VisualStudio.Debugger", StringComparison.Ordinal));
	}
}
public class SnapshotSet<TItem> : Snapshot
{
	readonly Func<IEnumerable<TItem>> getCurrent;
	readonly HashSet<TItem> initial;

	public SnapshotSet (Func<IEnumerable<TItem>> getCurrent)
	{
		this.getCurrent = getCurrent;
		initial = getCurrent ().ToHashSet ();
	}

	public override void AssertUnchanged ()
	{
		(var added, var removed) = GetChanges ();
		Assert.Empty (added);
		Assert.Empty (removed);
	}

	public (IEnumerable<TItem> added, IEnumerable<TItem> removed) GetChanges ()
	{
		var current = getCurrent ().ToHashSet ();
		return (
			current.Except (initial),
			initial.Except (current)
		);
	}
}

public class AggregateSnapshot : Snapshot
{
	readonly Snapshot[] snapshots;
	public AggregateSnapshot (params Snapshot[] snapshots) => this.snapshots = snapshots;
	public override void AssertUnchanged ()
	{
		foreach (var snapshot in snapshots) {
			snapshot.AssertUnchanged ();
		}
	}
}

// these test process state so cannot be run in parallel with other tests
[CollectionDefinition (nameof (StatefulTests), DisableParallelization = true)]
public class StatefulTests { }

[Collection (nameof (StatefulTests))]
public abstract class StatefulTest<T>
{
	protected abstract T GetInitialState ();
	protected abstract void VerifyFinalState (T state);
}