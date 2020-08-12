using System;
using System.Reflection;
using Mono.TextTemplating;

namespace Mono.VisualStudio.TextTemplating
{
	public class CompiledTemplateRecord
	{
		readonly CompiledTemplate  compiledTemplate;

		public CompiledTemplate CompiledTemplate => compiledTemplate;
		public DateTime LastUse { get; set; }

		public CompiledTemplateRecord(CompiledTemplate compiledTemplate)
		{
			this.compiledTemplate = compiledTemplate;
			LastUse = DateTime.Now;
		}
	}
}
