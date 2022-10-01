// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating;

partial class CompiledTemplate
{
	class TemplateProcessor : MarshalByRefObject
	{
		public string CreateAndProcess (ITextTemplatingEngineHost host, CompiledAssemblyData templateAssemblyData, string templateAssemblyFile, string fullName, CultureInfo culture, string[] referencedAssemblyFiles)
		{
			using var context = new TemplateAssemblyContext (host, referencedAssemblyFiles);

			Assembly assembly = templateAssemblyData is not null
				? context.LoadInMemoryAssembly (templateAssemblyData)
				: context.LoadAssemblyFile (templateAssemblyFile);

			// MS Templating Engine does not care about the type itself
			// it only requires the expected members to be on the compiled type
			// so we don't try to cast it, we invoke via reflection instead
			// TODO: could we use additional codegen to collapse all the init work to a single method?
			Type transformType = assembly.GetType (fullName);
			object textTransformation = Activator.CreateInstance (transformType);

			//set the host property if it exists
			Type hostType = null;
			if (host is TemplateGenerator gen) {
				hostType = gen.SpecificHostType;
			}
			var hostProp = transformType.GetProperty ("Host", hostType ?? typeof (ITextTemplatingEngineHost));
			if (hostProp != null && hostProp.CanWrite)
				hostProp.SetValue (textTransformation, host, null);

			if (host is ITextTemplatingSessionHost sessionHost) {
				//FIXME: should we create a session if it's null?
				var sessionProp = transformType.GetProperty ("Session", typeof (IDictionary<string, object>));
				sessionProp.SetValue (textTransformation, sessionHost.Session, null);
			}

			var errorProp = transformType.GetProperty ("Errors", BindingFlags.Instance | BindingFlags.NonPublic);
			if (errorProp == null)
				throw new ArgumentException ("Template must have 'Errors' property");
			var errorMethod = transformType.GetMethod ("Error", new Type[] { typeof (string) });
			if (errorMethod == null) {
				throw new ArgumentException ("Template must have 'Error(string message)' method");
			}

			var errors = (CompilerErrorCollection)errorProp.GetValue (textTransformation, null);
			errors.Clear ();

			//set the culture
			if (culture != null)
				ToStringHelper.FormatProvider = culture;
			else
				ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;

			string output = null;

			var initMethod = transformType.GetMethod ("Initialize");
			var transformMethod = transformType.GetMethod ("TransformText");

			if (initMethod == null) {
				errorMethod.Invoke (textTransformation, new object[] { "Error running transform: no method Initialize()" });
			} else if (transformMethod == null) {
				errorMethod.Invoke (textTransformation, new object[] { "Error running transform: no method TransformText()" });
			} else try {
					initMethod.Invoke (textTransformation, null);
					output = (string)transformMethod.Invoke (textTransformation, null);
				}
				catch (Exception ex) {
					errorMethod.Invoke (textTransformation, new object[] { "Error running transform: " + ex });
				}

			host.LogErrors (errors);

			ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;
			return output;
		}
	}
}
