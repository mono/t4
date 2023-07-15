// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// imported from dotnet/runtime
// see notes on individual methods

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Mono.TextTemplating;

#nullable enable

static class TempSubdirectoryHelper
{
	/// <summary>
	/// Create e temporary subdirectory in the system temp directory.
	/// On .NET 7+, calls <see cref="Directory.CreateTempSubdirectory"/>. Otherwise, it uses an implementation based on the one in from .NET 7.
	/// </summary>
	public static DirectoryInfo Create (string? prefix = default)
#if NETCOREAPP7_0_OR_GREATER
		=> Directory.CreateTempSubdirectory(prefix);
}
#else
	{
		if (ClassLibsImpl is not null) {
			return ClassLibsImpl (prefix);
		}

		if (prefix is string p && p.IndexOfAny (DirectorySeparatorChars) > -1) {
			throw new ArgumentException ("Prefix may not contain directory separators", nameof (prefix));
		}

		string path = isWindows ? CreateTempSubdirectoryCoreWindows (prefix) : CreateTempSubdirectoryCoreUnix (prefix);
		return new DirectoryInfo (path);
	}

	static readonly Func<string?, DirectoryInfo>? ClassLibsImpl;
	static readonly bool isWindows = Path.DirectorySeparatorChar == '\\';
	static readonly char[] DirectorySeparatorChars;

	static TempSubdirectoryHelper ()
	{
		if (typeof (Directory).GetMethod ("CreateTempSubdirectory", BindingFlags.Static | BindingFlags.Public) is MethodInfo fxMethod) {
			ClassLibsImpl = (Func<string?, DirectoryInfo>) fxMethod.CreateDelegate (typeof(Func<string?, DirectoryInfo>));
		}

		DirectorySeparatorChars = Path.AltDirectorySeparatorChar == Path.DirectorySeparatorChar
			? new[] { Path.DirectorySeparatorChar }
			: new[] { Path.DirectorySeparatorChar };
	}

	// copy of https://github.com/dotnet/runtime/blob/eb6f712d68f75add00f17a144838c1a64a3c3a47/src/libraries/System.Private.CoreLib/src/System/IO/Directory.Unix.cs#L27
	// modified to remove deps on new/internal framework APIs
	static unsafe string CreateTempSubdirectoryCoreUnix (string? prefix)
	{
		// mkdtemp takes a char* and overwrites the XXXXXX with six characters
		// that'll result in a unique directory name.
		string tempPath = Path.GetTempPath ();
		int tempPathByteCount = Encoding.UTF8.GetByteCount (tempPath);
		int prefixByteCount = prefix is not null ? Encoding.UTF8.GetByteCount (prefix) : 0;
		int totalByteCount = tempPathByteCount + prefixByteCount + 6 + 1;

		byte[] path = new byte[totalByteCount];
		int pos = Encoding.UTF8.GetBytes (tempPath, 0, tempPath.Length, path, 0);
		if (prefix is not null) {
			pos += Encoding.UTF8.GetBytes (prefix, 0, prefix.Length, path, pos);
		}
		for(int i = 0; i < 6; i++) {
			path[pos + i] = (byte)'X';
		}
		path[pos + 6] = 0;

		// Create the temp directory.
		fixed (byte* pPath = path) {
			if (libc_mkdtemp (pPath) is null) {
				Unix_ThrowIOExceptionForLastError ();
			}
		}

		// 'path' is now the name of the directory
		Debug.Assert (path[path.Length-1] == 0);
		return Encoding.UTF8.GetString (path, 0, path.Length - 1); // trim off the trailing '\0'
	}

	[DllImport ("libc", SetLastError = true, EntryPoint = "mkdtemp")]
	static unsafe extern byte* libc_mkdtemp (byte* path);

	static void Unix_ThrowIOExceptionForLastError ()
	{
		var error = Marshal.GetLastWin32Error ();
		throw new IOException ("error");
	}

	// copy of https://github.com/dotnet/runtime/blob/eb6f712d68f75add00f17a144838c1a64a3c3a47/src/libraries/System.Private.CoreLib/src/System/IO/Directory.Windows.cs#L16
	// modified to remove deps on new/internal framework APIs
	static unsafe string CreateTempSubdirectoryCoreWindows (string? prefix)
	{
		StringBuilder builder = new StringBuilder (MaxShortPath);
		var tempRoot = Path.GetTempPath ();
		builder.Append (tempRoot);

		// ensure the base TEMP directory exists
		Directory.CreateDirectory (tempRoot);

		builder.Append (prefix);

		const int RandomFileNameLength = 12; // 12 == 8 + 1 (for period) + 3
		int initialTempPathLength = builder.Length;
		builder.EnsureCapacity (initialTempPathLength + RandomFileNameLength);

		// For generating random file names
		// 8 random bytes provides 12 chars in our encoding for the 8.3 name.
		const int RandomKeyLength = 8;
		byte* pKey = stackalloc byte[RandomKeyLength];

		// to avoid an infinite loop, only try as many as GetTempFileNameW will create
		const int MaxAttempts = ushort.MaxValue;
		int attempts = 0;
		while (attempts < MaxAttempts) {
			builder.Length = initialTempPathLength;
			builder.Append (Path.GetRandomFileName ());
			var path = builder.ToString ();

			bool directoryCreated = Kernel32.CreateDirectory (path, null);
			if (!directoryCreated) {
				// in the off-chance that the directory already exists, try again
				int error = Marshal.GetLastWin32Error ();
				if (error == ERROR_ALREADY_EXISTS) {
					builder.Length = initialTempPathLength;
					attempts++;
					continue;
				}

				ThrowExceptionForWin32Error (error);
			}

			return path;
		}

		throw new IOException (IO_MaxAttemptsReached);
	}

	static void ThrowExceptionForWin32Error(int error)
	{
		// this is not ideal but we don't have access to anything better
		Marshal.ThrowExceptionForHR (MakeHRFromErrorCode (error));
	}

	const int MaxShortPath = 260;
	const int ERROR_ALREADY_EXISTS = 0xB7;
	const string IO_MaxAttemptsReached = "Reached maximum directory creation attempts";

	// https://github.com/dotnet/runtime/blob/cbc8695ae0c8c2c2d1ac1fc4546d81e0967ef716/src/libraries/Common/src/System/IO/Win32Marshal.cs#L84
	static int MakeHRFromErrorCode (int errorCode)
	{
		// Don't convert it if it is already an HRESULT
		if ((0xFFFF0000 & errorCode) != 0)
			return errorCode;

		return unchecked(((int)0x80070000) | errorCode);
	}

	// https://github.com/dotnet/runtime/blob/cbc8695ae0c8c2c2d1ac1fc4546d81e0967ef716/src/libraries/Common/src/Interop/Windows/Kernel32/Interop.CreateDirectory.cs#L15
	internal static class Kernel32
	{
		[DllImport ("kernel32.dll", EntryPoint = "CreateDirectoryW", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		static unsafe extern bool CreateDirectoryPrivate (
			string path,
			SECURITY_ATTRIBUTES* lpSecurityAttributes);

		internal static unsafe bool CreateDirectory (string path, SECURITY_ATTRIBUTES* lpSecurityAttributes)
		{
			// We always want to add for CreateDirectory to get around the legacy 248 character limitation
			path = PathInternal.EnsureExtendedPrefix (path);
			return CreateDirectoryPrivate (path, lpSecurityAttributes);
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;
			internal IntPtr lpSecurityDescriptor;
			internal BOOL bInheritHandle;
		}

		internal enum BOOL : int
		{
			FALSE = 0,
			TRUE = 1,
		}
	}

	// https://github.com/dotnet/runtime/blob/34bec269ff0b02224f318792c02f0254025b43bf/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L94
	class PathInternal
	{
		internal static string EnsureExtendedPrefix (string path)
		{
			if (IsPartiallyQualified (path) || IsDevice (path))
				return path;

			// Given \\server\share in longpath becomes \\?\UNC\server\share
			if (path.StartsWith (UncPathPrefix, StringComparison.OrdinalIgnoreCase))
				return path.Insert (2, UncExtendedPrefixToInsert);

			return ExtendedPathPrefix + path;
		}

		internal static bool IsPartiallyQualified (string path)
		{
			if (path.Length < 2) {
				// It isn't fixed, it must be relative.  There is no way to specify a fixed
				// path with one character (or less).
				return true;
			}

			if (IsDirectorySeparator (path[0])) {
				// There is no valid way to specify a relative path with two initial slashes or
				// \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
				return !(path[1] == '?' || IsDirectorySeparator (path[1]));
			}

			// The only way to specify a fixed path that doesn't begin with two slashes
			// is the drive, colon, slash format- i.e. C:\
			return !((path.Length >= 3)
				&& (path[1] == Path.VolumeSeparatorChar)
				&& IsDirectorySeparator (path[2])
				// To match old behavior we'll check the drive character for validity as the path is technically
				// not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
				&& IsValidDriveChar (path[0]));
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		internal static bool IsDirectorySeparator (char c)
		{
			return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
		}

		internal static bool IsValidDriveChar (char value)
		{
			return (uint)((value | 0x20) - 'a') <= (uint)('z' - 'a');
		}

		internal static bool IsDevice (string path)
		{
			return IsExtended (path)
				||
				(
					path.Length >= DevicePrefixLength
					&& IsDirectorySeparator (path[0])
					&& IsDirectorySeparator (path[1])
					&& (path[2] == '.' || path[2] == '?')
					&& IsDirectorySeparator (path[3])
				);
		}

		internal static bool IsExtended (string path)
		{
			// While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
			// Skipping of normalization will *only* occur if back slashes ('\') are used.
			return path.Length >= DevicePrefixLength
				&& path[0] == '\\'
				&& (path[1] == '\\' || path[1] == '?')
				&& path[2] == '?'
				&& path[3] == '\\';
		}

		internal const string ExtendedPathPrefix = @"\\?\";
		internal const string UncPathPrefix = @"\\";
		internal const string UncExtendedPrefixToInsert = @"?\UNC\";
		internal const string UncExtendedPathPrefix = @"\\?\UNC\";
		internal const int DevicePrefixLength = 4;
	}
}
#endif