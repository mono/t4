using System;

namespace Mono.VisualStudio.TextTemplating.VHost
{
	public class DebugTemplateEventArgs : EventArgs
	{
		public string TemplateOutput { get; set; }
		public bool Succeeded { get; set; }
	}
}
