using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating
{
	public static class CompiledTemplateCache
	{
		public static Dictionary<string, CompiledTemplateRecord> compiledTemplates = new Dictionary<string, CompiledTemplateRecord> (0x23);
		public static DateTime lastUse;

		public static CompiledTemplate Find(string fullClassName)
		{
			CompiledTemplate compiledTemplate = null;
			Dictionary<string, CompiledTemplateRecord> compiledTemplates = CompiledTemplateCache.compiledTemplates;
			lock (compiledTemplates) {
				lastUse = DateTime.Now;
				if (CompiledTemplateCache.compiledTemplates.TryGetValue(fullClassName, out CompiledTemplateRecord record)) {
					compiledTemplate = record.CompiledTemplate;
					record.LastUse = lastUse;
				}
			}
			return compiledTemplate;
		}

		public static void Insert (string classFullName, CompiledTemplate compiledTemplate)
		{
			Dictionary<string, CompiledTemplateRecord> assemblies = CompiledTemplateCache.compiledTemplates;
			lock (assemblies) {
				CompiledTemplateCache.compiledTemplates[classFullName] = new CompiledTemplateRecord (compiledTemplate);
				lastUse = DateTime.Now;
			}
		}
	}
}
