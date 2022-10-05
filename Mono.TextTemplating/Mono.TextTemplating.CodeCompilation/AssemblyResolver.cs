// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mono.TextTemplating.CodeCompilation
{
	//attempt to resolve refs into the runtime dir if the host didn't already do so
	static class AssemblyResolver
	{
		public static IEnumerable<string> GetResolvedReferences (RuntimeInfo runtime, List<string> references)
		{
			var asmFileNames = new HashSet<string> (StringComparer.OrdinalIgnoreCase);

			var disableRefAsms = Environment.GetEnvironmentVariable ("T4_DEBUG_DISABLE_REF_ASMS") is string s && s switch {
				"true" => true,
				"y" => true,
				"yes" => true,
				"false" => false,
				"n" => false,
				"no" => false,
				_ => throw new TemplatingEngineException ($"T4_DEBUG_DISABLE_REF_ASMS env var has unknown value '{s}'")
			};

			// if we have .NET Core reference assemblies, just reference all of them
			// as that's the behavior expected in .NET Core project files
			if (runtime.Kind == RuntimeKind.NetCore)
			{
				if (runtime.RefAssembliesDir != null && !disableRefAsms)
				{
					foreach (var asm in Directory.EnumerateFiles(runtime.RefAssembliesDir, "*.dll")) {
						asmFileNames.Add (Path.GetFileName (asm));
						yield return asm;
					}
				}
				else
				{
					foreach (var knownFxAsm in KnownNet50RefAssemblyNames.Value) {
						var resolved = Path.Combine (runtime.RuntimeDir, knownFxAsm);
						if (File.Exists (resolved)) {
							asmFileNames.Add (knownFxAsm);
							yield return resolved;
						}
					}
					//because we're referencing the impl not the ref asms, we end up having to ref internals
					var corlib = "System.Private.CoreLib.dll";
					var resolvedCorlib = Path.Combine (runtime.RuntimeDir, corlib);
					if (File.Exists (resolvedCorlib)) {
						asmFileNames.Add (corlib);
						yield return resolvedCorlib;
					}
				}

				// .NET Core doesn't include CompilerErrorCollection in the ref assemblies
				// so use the one we have loaded
				yield return typeof (System.CodeDom.Compiler.CompilerErrorCollection).Assembly.Location;
			}
			else
			{
				// on full framework, automatically reference all the assemblies that an sdk-style net472 csproj would reference by default
				foreach (var asm in Net472DefaultAssemblyRefs.Value) {
					var asmPath = Path.Combine (runtime.RuntimeDir, asm);
					if (File.Exists (asmPath)) {
						asmFileNames.Add (asm);
						yield return asmPath;
					}
				}

				//also reference all the facades as MSBuild does this too
				foreach (var asm in Net472FacadeNames.Value) {
					var asmPath = Path.Combine (runtime.RuntimeFacadesDir, asm);
					if (File.Exists (asmPath)) {
						asmFileNames.Add (asm);
						yield return asmPath;
					}
				}
			}

			foreach (var reference in references) {
				if (!asmFileNames.Contains(Path.GetFileName(reference))) {
					var asm = Resolve (runtime, reference);
					yield return asm;
				}
			}
		}

		static string Resolve (RuntimeInfo runtime, string reference)
		{
			if (Path.IsPathRooted (reference) || File.Exists (reference)) {
				return reference;
			}

			var resolved = Path.Combine (runtime.RuntimeDir, reference);
			if (File.Exists (resolved)) {
				return resolved;
			}

			if (runtime.RuntimeFacadesDir != null && runtime.RuntimeFacadesDir != runtime.RuntimeDir) {
				resolved = Path.Combine (runtime.RuntimeFacadesDir, reference);
				if (File.Exists (resolved)) {
					return resolved;
				}
			}

			return reference;
		}

		static readonly Lazy<string[]> KnownNet50RefAssemblyNames = new (() => new[] {
			"Microsoft.CSharp.dll",
			"Microsoft.VisualBasic.Core.dll",
			"Microsoft.VisualBasic.dll",
			"Microsoft.Win32.Primitives.dll",
			"mscorlib.dll",
			"netstandard.dll",
			"System.AppContext.dll",
			"System.Buffers.dll",
			"System.Collections.Concurrent.dll",
			"System.Collections.dll",
			"System.Collections.Immutable.dll",
			"System.Collections.NonGeneric.dll",
			"System.Collections.Specialized.dll",
			"System.ComponentModel.Annotations.dll",
			"System.ComponentModel.DataAnnotations.dll",
			"System.ComponentModel.dll",
			"System.ComponentModel.EventBasedAsync.dll",
			"System.ComponentModel.Primitives.dll",
			"System.ComponentModel.TypeConverter.dll",
			"System.Configuration.dll",
			"System.Console.dll",
			"System.Core.dll",
			"System.Data.Common.dll",
			"System.Data.DataSetExtensions.dll",
			"System.Data.dll",
			"System.Diagnostics.Contracts.dll",
			"System.Diagnostics.Debug.dll",
			"System.Diagnostics.DiagnosticSource.dll",
			"System.Diagnostics.FileVersionInfo.dll",
			"System.Diagnostics.Process.dll",
			"System.Diagnostics.StackTrace.dll",
			"System.Diagnostics.TextWriterTraceListener.dll",
			"System.Diagnostics.Tools.dll",
			"System.Diagnostics.TraceSource.dll",
			"System.Diagnostics.Tracing.dll",
			"System.dll",
			"System.Drawing.dll",
			"System.Drawing.Primitives.dll",
			"System.Dynamic.Runtime.dll",
			"System.Formats.Asn1.dll",
			"System.Globalization.Calendars.dll",
			"System.Globalization.dll",
			"System.Globalization.Extensions.dll",
			"System.IO.Compression.Brotli.dll",
			"System.IO.Compression.dll",
			"System.IO.Compression.FileSystem.dll",
			"System.IO.Compression.ZipFile.dll",
			"System.IO.dll",
			"System.IO.FileSystem.dll",
			"System.IO.FileSystem.DriveInfo.dll",
			"System.IO.FileSystem.Primitives.dll",
			"System.IO.FileSystem.Watcher.dll",
			"System.IO.IsolatedStorage.dll",
			"System.IO.MemoryMappedFiles.dll",
			"System.IO.Pipes.dll",
			"System.IO.UnmanagedMemoryStream.dll",
			"System.Linq.dll",
			"System.Linq.Expressions.dll",
			"System.Linq.Parallel.dll",
			"System.Linq.Queryable.dll",
			"System.Memory.dll",
			"System.Net.dll",
			"System.Net.Http.dll",
			"System.Net.Http.Json.dll",
			"System.Net.HttpListener.dll",
			"System.Net.Mail.dll",
			"System.Net.NameResolution.dll",
			"System.Net.NetworkInformation.dll",
			"System.Net.Ping.dll",
			"System.Net.Primitives.dll",
			"System.Net.Requests.dll",
			"System.Net.Security.dll",
			"System.Net.ServicePoint.dll",
			"System.Net.Sockets.dll",
			"System.Net.WebClient.dll",
			"System.Net.WebHeaderCollection.dll",
			"System.Net.WebProxy.dll",
			"System.Net.WebSockets.Client.dll",
			"System.Net.WebSockets.dll",
			"System.Numerics.dll",
			"System.Numerics.Vectors.dll",
			"System.ObjectModel.dll",
			"System.Reflection.DispatchProxy.dll",
			"System.Reflection.dll",
			"System.Reflection.Emit.dll",
			"System.Reflection.Emit.ILGeneration.dll",
			"System.Reflection.Emit.Lightweight.dll",
			"System.Reflection.Extensions.dll",
			"System.Reflection.Metadata.dll",
			"System.Reflection.Primitives.dll",
			"System.Reflection.TypeExtensions.dll",
			"System.Resources.Reader.dll",
			"System.Resources.ResourceManager.dll",
			"System.Resources.Writer.dll",
			"System.Runtime.CompilerServices.Unsafe.dll",
			"System.Runtime.CompilerServices.VisualC.dll",
			"System.Runtime.dll",
			"System.Runtime.Extensions.dll",
			"System.Runtime.Handles.dll",
			"System.Runtime.InteropServices.dll",
			"System.Runtime.InteropServices.RuntimeInformation.dll",
			"System.Runtime.Intrinsics.dll",
			"System.Runtime.Loader.dll",
			"System.Runtime.Numerics.dll",
			"System.Runtime.Serialization.dll",
			"System.Runtime.Serialization.Formatters.dll",
			"System.Runtime.Serialization.Json.dll",
			"System.Runtime.Serialization.Primitives.dll",
			"System.Runtime.Serialization.Xml.dll",
			"System.Security.Claims.dll",
			"System.Security.Cryptography.Algorithms.dll",
			"System.Security.Cryptography.Csp.dll",
			"System.Security.Cryptography.Encoding.dll",
			"System.Security.Cryptography.Primitives.dll",
			"System.Security.Cryptography.X509Certificates.dll",
			"System.Security.dll",
			"System.Security.Principal.dll",
			"System.Security.SecureString.dll",
			"System.ServiceModel.Web.dll",
			"System.ServiceProcess.dll",
			"System.Text.Encoding.CodePages.dll",
			"System.Text.Encoding.dll",
			"System.Text.Encoding.Extensions.dll",
			"System.Text.Encodings.Web.dll",
			"System.Text.Json.dll",
			"System.Text.RegularExpressions.dll",
			"System.Threading.Channels.dll",
			"System.Threading.dll",
			"System.Threading.Overlapped.dll",
			"System.Threading.Tasks.Dataflow.dll",
			"System.Threading.Tasks.dll",
			"System.Threading.Tasks.Extensions.dll",
			"System.Threading.Tasks.Parallel.dll",
			"System.Threading.Thread.dll",
			"System.Threading.ThreadPool.dll",
			"System.Threading.Timer.dll",
			"System.Transactions.dll",
			"System.Transactions.Local.dll",
			"System.ValueTuple.dll",
			"System.Web.dll",
			"System.Web.HttpUtility.dll",
			"System.Windows.dll",
			"System.Xml.dll",
			"System.Xml.Linq.dll",
			"System.Xml.ReaderWriter.dll",
			"System.Xml.Serialization.dll",
			"System.Xml.XDocument.dll",
			"System.Xml.XmlDocument.dll",
			"System.Xml.XmlSerializer.dll",
			"System.Xml.XPath.dll",
			"System.Xml.XPath.XDocument.dll",
			"WindowsBase.dll"
		});

		static readonly Lazy<string[]> Net472DefaultAssemblyRefs = new (() => new[] {
			"mscorlib.dll",
			"System.dll",
			"System.Core.dll",
			"System.Data.dll",
			"System.Drawing.dll",
			"System.IO.Compression.FileSystem.dll",
			"Systaem.Numerics.dll",
			"Systaem.Runtime.Serialization.dll",
			"Systaem.Xml.dll",
			"Systaem.Xml.Linq.dll"
		});

		static readonly Lazy<string[]> Net472FacadeNames = new (() => new[] {
			"Microsoft.Win32.Primitives.dll",
			"netstandard.dll",
			"System.AppContext.dll",
			"System.Collections.Concurrent.dll",
			"System.Collections.dll",
			"System.Collections.NonGeneric.dll",
			"System.Collections.Specialized.dll",
			"System.ComponentModel.Annotations.dll",
			"System.ComponentModel.dll",
			"System.ComponentModel.EventBasedAsync.dll",
			"System.ComponentModel.Primitives.dll",
			"System.ComponentModel.TypeConverter.dll",
			"System.Console.dll",
			"System.Data.Common.dll",
			"System.Diagnostics.Contracts.dll",
			"System.Diagnostics.Debug.dll",
			"System.Diagnostics.FileVersionInfo.dll",
			"System.Diagnostics.Process.dll",
			"System.Diagnostics.StackTrace.dll",
			"System.Diagnostics.TextWriterTraceListener.dll",
			"System.Diagnostics.Tools.dll",
			"System.Diagnostics.TraceSource.dll",
			"System.Drawing.Primitives.dll",
			"System.Dynamic.Runtime.dll",
			"System.Globalization.Calendars.dll",
			"System.Globalization.dll",
			"System.Globalization.Extensions.dll",
			"System.IO.Compression.ZipFile.dll",
			"System.IO.dll",
			"System.IO.FileSystem.dll",
			"System.IO.FileSystem.DriveInfo.dll",
			"System.IO.FileSystem.Primitives.dll",
			"System.IO.FileSystem.Watcher.dll",
			"System.IO.IsolatedStorage.dll",
			"System.IO.MemoryMappedFiles.dll",
			"System.IO.Pipes.dll",
			"System.IO.UnmanagedMemoryStream.dll",
			"System.Linq.dll",
			"System.Linq.Expressions.dll",
			"System.Linq.Parallel.dll",
			"System.Linq.Queryable.dll",
			"System.Net.Http.Rtc.dll",
			"System.Net.NameResolution.dll",
			"System.Net.NetworkInformation.dll",
			"System.Net.Ping.dll",
			"System.Net.Primitives.dll",
			"System.Net.Requests.dll",
			"System.Net.Security.dll",
			"System.Net.Sockets.dll",
			"System.Net.WebHeaderCollection.dll",
			"System.Net.WebSockets.Client.dll",
			"System.Net.WebSockets.dll",
			"System.ObjectModel.dll",
			"System.Reflection.dll",
			"System.Reflection.Emit.dll",
			"System.Reflection.Emit.ILGeneration.dll",
			"System.Reflection.Emit.Lightweight.dll",
			"System.Reflection.Extensions.dll",
			"System.Reflection.Primitives.dll",
			"System.Resources.Reader.dll",
			"System.Resources.ResourceManager.dll",
			"System.Resources.Writer.dll",
			"System.Runtime.CompilerServices.VisualC.dll",
			"System.Runtime.dll",
			"System.Runtime.Extensions.dll",
			"System.Runtime.Handles.dll",
			"System.Runtime.InteropServices.dll",
			"System.Runtime.InteropServices.RuntimeInformation.dll",
			"System.Runtime.InteropServices.WindowsRuntime.dll",
			"System.Runtime.Numerics.dll",
			"System.Runtime.Serialization.Formatters.dll",
			"System.Runtime.Serialization.Json.dll",
			"System.Runtime.Serialization.Primitives.dll",
			"System.Runtime.Serialization.Xml.dll",
			"System.Security.Claims.dll",
			"System.Security.Cryptography.Algorithms.dll",
			"System.Security.Cryptography.Csp.dll",
			"System.Security.Cryptography.Encoding.dll",
			"System.Security.Cryptography.Primitives.dll",
			"System.Security.Cryptography.X509Certificates.dll",
			"System.Security.Principal.dll",
			"System.Security.SecureString.dll",
			"System.ServiceModel.Duplex.dll",
			"System.ServiceModel.Http.dll",
			"System.ServiceModel.NetTcp.dll",
			"System.ServiceModel.Primitives.dll",
			"System.ServiceModel.Security.dll",
			"System.Text.Encoding.dll",
			"System.Text.Encoding.Extensions.dll",
			"System.Text.RegularExpressions.dll",
			"System.Threading.dll",
			"System.Threading.Overlapped.dll",
			"System.Threading.Tasks.dll",
			"System.Threading.Tasks.Parallel.dll",
			"System.Threading.Thread.dll",
			"System.Threading.ThreadPool.dll",
			"System.Threading.Timer.dll",
			"System.ValueTuple.dll",
			"System.Xml.ReaderWriter.dll",
			"System.Xml.XDocument.dll",
			"System.Xml.XmlDocument.dll",
			"System.Xml.XmlSerializer.dll",
			"System.Xml.XPath.dll",
			"System.Xml.XPath.XDocument.dll"
		});
	}
}
