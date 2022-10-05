// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Xunit;

namespace Mono.TextTemplating.Tests;

/// <summary>
/// SynchronizationContext that tracks how many times it is used. Can be used to validate that async code does not use the SynchronizationContext.
/// </summary>
sealed class TrackingSynchronizationContext : SynchronizationContext, IDisposable
{
	readonly SynchronizationContext inner;
	int callCount;

	public int CallCount => callCount;

	public TrackingSynchronizationContext ()
	{
		inner = Current;
		SetSynchronizationContext (this);
	}

	public override void Post (SendOrPostCallback d, object state)
	{
		Interlocked.Increment (ref callCount);

		if (inner is not null) {
			inner.Post (d, state);
		} else {
			d (state);
		}
	}

	public override void Send (SendOrPostCallback d, object state)
	{
		Interlocked.Increment (ref callCount);

		if (inner is not null) {
			inner.Send (d, state);
		} else {
			d (state);
		}
	}

	public void Dispose ()
	{
		SetSynchronizationContext (inner);
	}

	/// <summary>
	/// Asserts that the sync context was not used to resume tasks continuations more than the expected number of times.
	/// This is a maximum, not an exact value, because continuations will shortcircuit the sync context if the task is already completed.
	/// </summary>
	public void AssertMaxCallCount (int expectedMaximum) => Assert.True (callCount <= expectedMaximum);
}
