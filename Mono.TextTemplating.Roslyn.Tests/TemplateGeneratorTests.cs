using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Mono.TextTemplating.Roslyn.Tests
{
	[TestFixture]
	public class TemplateGeneratorTests
	{
		[Test]
		public void TestProcessTemplate()
		{
			const string inputFile = "Template.tt";
			const string resourceName = "Mono.TextTemplating.Roslyn.Tests.Template.tt";

			var generator = new TemplateGenerator();
			generator.UseInProcessCompiler();

			Assembly assembly = typeof(TemplateGeneratorTests).Assembly;
			Stream stream = assembly.GetManifestResourceStream(resourceName);

			if (stream == null) {
				throw new NullReferenceException($"Stream was null: {resourceName}");
			}

			var inputContent = new StreamReader(stream).ReadToEnd();
			string outputFilename = null;
			generator.ProcessTemplate(inputFile, inputContent, ref outputFilename, out _);
		}
	}
}