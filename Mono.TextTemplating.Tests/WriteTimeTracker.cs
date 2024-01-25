// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Mono.TextTemplating.Tests;

sealed class WriteTimeTracker
{
	readonly TestDataPath file;
	DateTime lastWriteTime;
	public WriteTimeTracker (TestDataPath file) => lastWriteTime = (this.file = file).GetLastWriteTime ();
	public void AssertChanged () => lastWriteTime = file.AssertWriteTimeNewerThan (lastWriteTime);
	public void AssertSame () => file.AssertWriteTimeEquals (lastWriteTime);

	public DateTime WaitUntilLaterNow () => LaterNowThan (lastWriteTime);

	static DateTime GetNewestWriteTime (IEnumerable<WriteTimeTracker> trackers)
	{
		DateTime newest = DateTime.MinValue;
		foreach (var tracker in trackers) {
			if (newest < tracker.lastWriteTime) {
				newest = tracker.lastWriteTime;
			}
		}
		return newest;
	}

	public static void SetWriteTimeNewerThan (IEnumerable<WriteTimeTracker> trackers, string filePath)
		=> SetWriteTimeNewerThan (GetNewestWriteTime (trackers), filePath);

	public static void SetWriteTimeNewerThan (WriteTimeTracker tracker, string filePath)
		=> SetWriteTimeNewerThan (tracker.lastWriteTime, filePath);

	public static void SetWriteTimeNewerThan (IEnumerable<WriteTimeTracker> trackers, params string[] filePaths)
		=> SetWriteTimeNewerThan (GetNewestWriteTime (trackers), filePaths);

	public static void SetWriteTimeNewerThan (WriteTimeTracker tracker, params string[] filePaths)
		=> SetWriteTimeNewerThan (tracker.lastWriteTime, filePaths);

	/// <summary>
	/// Waits until `DateTime.Now` is newer than `time`, then return this newer value.
	/// </summary>
	static DateTime LaterNowThan (DateTime time)
	{
		DateTime now;
		while ((now = DateTime.Now) <= time) {
			Thread.Sleep (10);
		}
		return now;
	}

	static void SetWriteTimeNewerThan (DateTime time, string filePath) => File.SetLastWriteTime (filePath, LaterNowThan (time));

	static void SetWriteTimeNewerThan (DateTime time, params string[] filePaths)
	{
		var now = LaterNowThan (time);
		foreach (var file in filePaths) {
			File.SetLastWriteTime (file, now);
		}
	}
}
