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
		static string Resolve (RuntimeInfo runtime, string reference)
		{
			if (Path.IsPathRooted (reference) || File.Exists (reference)) {
				return reference;
			}

			var resolved = Path.Combine (runtime.RuntimeDir, reference);
			if (File.Exists (resolved)) {
				return resolved;
			}

			if (runtime.Kind != RuntimeKind.NetCore) {
				resolved = Path.Combine (runtime.RuntimeDir, "Facades", reference);
				if (File.Exists (resolved)) {
					return resolved;
				}
			}

			return reference;
		}

		public static IEnumerable<string> GetResolvedReferences (RuntimeInfo runtime, List<string> references)
		{
			var asmFileNames = new HashSet<string> (
				references.Select (Path.GetFileName),
				StringComparer.OrdinalIgnoreCase
			);

			IEnumerable<string> GetImplicitReferences ()
			{
				yield return "mscorlib.dll";
				yield return "netstandard.dll";

				if (runtime.Kind == RuntimeKind.NetCore) {
					yield return "System.Runtime.dll";
					//because we're referencing the impl not the ref asms, we end up
					//having to ref internals
					yield return "System.Private.CoreLib.dll";
				}
			}

			foreach (var asm in GetImplicitReferences ()) {
				if (!asmFileNames.Contains (asm)) {
					var asmPath = Path.Combine (runtime.RuntimeDir, asm);
					if (File.Exists (asmPath)) {
						yield return Path.Combine (runtime.RuntimeDir, asm);
					}
				}
			}

			foreach (var reference in references) {
				var asm = Resolve (runtime, reference);
				yield return asm;
			}
		}
	}
}
