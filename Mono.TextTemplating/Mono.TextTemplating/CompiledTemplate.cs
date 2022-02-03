// 
// CompiledTemplate.cs
//  
// Author:
//       Nathan Baulch <nathan.baulch@gmail.com>
// 
// Copyright (c) 2009 Nathan Baulch
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.IO;

using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating
{
	public sealed class CompiledTemplate :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		IDisposable
	{
		ITextTemplatingEngineHost host;
		object textTransformation;
		readonly CultureInfo culture;

#if FEATURE_ASSEMBLY_LOAD_CONTEXT
		readonly TemplateAssemblyLoadContext templateContext;
#else
		readonly CurrentDomainAssemblyResolver assemblyResolver;
#endif

		[Obsolete ("Should not have been public")]
		public CompiledTemplate (ITextTemplatingEngineHost host, CompilerResults results, string fullName, CultureInfo culture, string[] assemblyFiles)
			: this (host, culture, assemblyFiles)
		{
			Assembly assembly = results.PathToAssembly != null
				? LoadAssemblyFile (results.PathToAssembly)
				: results.CompiledAssembly;
			InitializeTemplate (assembly, fullName);
		}


		public CompiledTemplate (ITextTemplatingEngineHost host, string templateAssemblyFile, string fullName, CultureInfo culture, string[] referenceAssemblyFiles)
			: this (host, culture, referenceAssemblyFiles)
		{
			var assembly = LoadAssemblyFile (templateAssemblyFile);
			InitializeTemplate (assembly, fullName);
		}

		internal CompiledTemplate (ITextTemplatingEngineHost host, CompiledAssemblyData compiledAssembly, string fullName, CultureInfo culture, string[] referenceAssemblyFiles)
			: this (host, culture, referenceAssemblyFiles)
		{
			var assembly = LoadInMemoryAssembly (compiledAssembly);
			InitializeTemplate (assembly, fullName);
		}

		CompiledTemplate (ITextTemplatingEngineHost host, CultureInfo culture, string[] referenceAssemblyFiles)
		{
			this.host = host;
			this.culture = culture;

#if FEATURE_ASSEMBLY_LOAD_CONTEXT
			templateContext = new TemplateAssemblyLoadContext (referenceAssemblyFiles, host);
#else
			assemblyResolver = new (host, referenceAssemblyFiles);
#endif
		}

#if FEATURE_ASSEMBLY_LOAD_CONTEXT
		Assembly LoadAssemblyFile (string assemblyPath) => templateContext.LoadFromAssemblyPath (assemblyPath);
		Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData) => assemblyData.LoadInAssemblyLoadContext (templateContext);
#else
		static Assembly LoadAssemblyFile (string assemblyPath)  => Assembly.LoadFile (assemblyPath);
		static Assembly LoadInMemoryAssembly (CompiledAssemblyData assemblyData) => assemblyData.LoadInCurrentAppDomain ();
#endif

		void InitializeTemplate (Assembly assembly, string fullName)
		{
			//MS Templating Engine does not care about the type itself
			//it only requires the expected members to be on the compiled type 
			Type transformType = assembly.GetType (fullName);
			textTransformation = Activator.CreateInstance (transformType);

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
		}

		public string Process ()
		{
			var ttType = textTransformation.GetType ();

			var errorProp = ttType.GetProperty ("Errors", BindingFlags.Instance | BindingFlags.NonPublic);
			if (errorProp == null)
				throw new ArgumentException ("Template must have 'Errors' property");
			var errorMethod = ttType.GetMethod ("Error", new Type [] { typeof (string) });
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

			var initMethod = ttType.GetMethod ("Initialize");
			var transformMethod = ttType.GetMethod ("TransformText");

			if (initMethod == null) {
				errorMethod.Invoke (textTransformation, new object [] { "Error running transform: no method Initialize()" });
			} else if (transformMethod == null) {
				errorMethod.Invoke (textTransformation, new object [] { "Error running transform: no method TransformText()" });
			} else try {
					initMethod.Invoke (textTransformation, null);
					output = (string)transformMethod.Invoke (textTransformation, null);
				} catch (Exception ex) {
					errorMethod.Invoke (textTransformation, new object [] { "Error running transform: " + ex });
				}

			host.LogErrors (errors);

			ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;
			return output;
		}

		public void Dispose ()
		{
			host = null;
#if !FEATURE_ASSEMBLY_LOAD_CONTEXT
			assemblyResolver.Dispose ();
#endif
		}
	}
}
