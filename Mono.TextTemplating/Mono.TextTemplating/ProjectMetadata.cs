using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.TextTemplating
{
	public class ProjectMetadata
	{
		public Dictionary<string, string> Metadatas = new Dictionary<string, string> ();

		public string ProjectDir => Metadatas[nameof (ProjectDir)];
		public string ProjectFileName => Metadatas[nameof (ProjectFileName)];
	}
}
