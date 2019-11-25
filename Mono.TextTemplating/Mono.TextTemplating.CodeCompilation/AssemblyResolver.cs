using System.IO;

namespace Mono.TextTemplating.CodeCompilation
{
	//attempt to resolve refs into the runtime dir if the host didn't already do so
	static class AssemblyResolver
	{
		internal static string Resolve (RuntimeInfo runtime, string reference)
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
	}
}
