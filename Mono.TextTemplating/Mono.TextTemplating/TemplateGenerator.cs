// 
// TemplatingHost.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	public class TemplateGenerator :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
		ITextTemplatingEngineHost
	{
		static readonly Dictionary<string, string> KnownAssemblies = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase)
		{
			{ "System.Core.dll", typeof(System.Linq.Enumerable).Assembly.Location },
			{ "System.Data.dll", typeof(System.Data.DataTable).Assembly.Location },
			{ "System.Linq.dll", typeof(System.Linq.Enumerable).Assembly.Location },
			{ "System.Xml.dll", typeof(System.Xml.XmlAttribute).Assembly.Location },
			{ "System.Xml.Linq.dll", typeof(System.Xml.Linq.XDocument).Assembly.Location },

			{ "System.Core", typeof(System.Linq.Enumerable).Assembly.Location },
			{ "System.Data", typeof(System.Data.DataTable).Assembly.Location },
			{ "System.Linq", typeof(System.Linq.Enumerable).Assembly.Location },
			{ "System.Xml", typeof(System.Xml.XmlAttribute).Assembly.Location },
			{ "System.Xml.Linq", typeof(System.Xml.Linq.XDocument).Assembly.Location }
		};

		//re-usable
		TemplatingEngine engine;
		
		//per-run variables
		Encoding encoding;

		//host properties for consumers to access
		public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection ();
		public List<string> Refs { get; } = new List<string> ();
		public List<string> Imports { get; } = new List<string> ();
		public List<string> IncludePaths { get; } = new List<string> ();
		public List<string> ReferencePaths { get; } = new List<string> ();
		public string OutputFile { get; protected set; }
		public string TemplateFile { get; protected set; }
		public bool UseRelativeLinePragmas { get; set; }
		
		public TemplateGenerator ()
		{
			Refs.Add (typeof (TextTransformation).Assembly.Location);
			Refs.Add (typeof(Uri).Assembly.Location);
			Refs.Add (typeof (File).Assembly.Location);
			Refs.Add (typeof (StringReader).Assembly.Location);
			Imports.Add ("System");
		}
		
		public CompiledTemplate CompileTemplate (string content)
		{
			if (string.IsNullOrEmpty (content))
				throw new ArgumentNullException (nameof (content));

			Errors.Clear ();
			encoding = Encoding.UTF8;
			
			return Engine.CompileTemplate (content, this);
		}
		
		protected TemplatingEngine Engine {
			get {
				if (engine == null)
					engine = new TemplatingEngine ();
				return engine;
			}
		}
		
		public bool ProcessTemplate (string inputFile, string outputFile)
		{
			if (string.IsNullOrEmpty (inputFile))
				throw new ArgumentNullException (nameof (inputFile));
			if (string.IsNullOrEmpty (outputFile))
				throw new ArgumentNullException (nameof (outputFile));
			
			string content;
			try {
				content = File.ReadAllText (inputFile);
			} catch (IOException ex) {
				Errors.Clear ();
				AddError ("Could not read input file '" + inputFile + "':\n" + ex);
				return false;
			}

			ProcessTemplate (inputFile, content, ref outputFile, out var output);

			try {
				if (!Errors.HasErrors)
					File.WriteAllText (outputFile, output, encoding);
			} catch (IOException ex) {
				AddError ("Could not write output file '" + outputFile + "':\n" + ex);
			}
			
			return !Errors.HasErrors;
		}
		
		public bool ProcessTemplate (string inputFileName, string inputContent, ref string outputFileName, out string outputContent)
		{
			Errors.Clear ();
			encoding = Encoding.UTF8;

			OutputFile = outputFileName;
			TemplateFile = inputFileName;
			outputContent = Engine.ProcessTemplate (inputContent, this);
			outputFileName = OutputFile;
			
			return !Errors.HasErrors;
		}
		
		public bool PreprocessTemplate (string inputFile, string className, string classNamespace, 
			string outputFile, Encoding encoding, out string language, out string[] references)
		{
			language = null;
			references = null;

			if (string.IsNullOrEmpty (inputFile))
				throw new ArgumentNullException (nameof (inputFile));
			if (string.IsNullOrEmpty (outputFile))
				throw new ArgumentNullException (nameof (outputFile));
			
			string content;
			try {
				content = File.ReadAllText (inputFile);
			} catch (IOException ex) {
				Errors.Clear ();
				AddError ("Could not read input file '" + inputFile + "':\n" + ex);
				return false;
			}
			
			string output;
			PreprocessTemplate (inputFile, className, classNamespace, content, out language, out references, out output);
			
			try {
				if (!Errors.HasErrors)
					File.WriteAllText (outputFile, output, encoding);
			} catch (IOException ex) {
				AddError ("Could not write output file '" + outputFile + "':\n" + ex);
			}
			
			return !Errors.HasErrors;
		}
		
		public bool PreprocessTemplate (string inputFileName, string className, string classNamespace, string inputContent, 
			out string language, out string[] references, out string outputContent)
		{
			Errors.Clear ();
			encoding = Encoding.UTF8;

			TemplateFile = inputFileName;
			outputContent = Engine.PreprocessTemplate (inputContent, this, className, classNamespace, out language, out references);
			
			return !Errors.HasErrors;
		}
		
		CompilerError AddError (string error)
		{
			var err = new CompilerError { ErrorText = error };
			Errors.Add (err);
			return err;
		}
		
		#region Virtual members
		
		public virtual object GetHostOption (string optionName)
		{
			switch (optionName) {
			case "UseRelativeLinePragmas":
				return UseRelativeLinePragmas;
			}
			return null;
		}
		
		public virtual AppDomain ProvideTemplatingAppDomain (string content)
		{
			return null;
		}
		
		protected virtual string ResolveAssemblyReference (string assemblyReference)
		{
			if (System.IO.Path.IsPathRooted (assemblyReference))
 				return assemblyReference;
 			foreach (string referencePath in ReferencePaths) {
 				var path = System.IO.Path.Combine (referencePath, assemblyReference);
 				if (System.IO.File.Exists (path))
 					return path;
 			}

			var assemblyName = new AssemblyName(assemblyReference);
			if (assemblyName.Version != null)//Load via GAC and return full path
				return Assembly.Load (assemblyName).Location;

			if (KnownAssemblies.TryGetValue (assemblyReference, out string mappedAssemblyReference)) {
				return mappedAssemblyReference;
			}

			if (!assemblyReference.EndsWith (".dll", StringComparison.OrdinalIgnoreCase) && !assemblyReference.EndsWith (".exe", StringComparison.OrdinalIgnoreCase))
				return assemblyReference + ".dll";
			return assemblyReference;
		}
		
		protected virtual string ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			var key = new ParameterKey (processorName, directiveId, parameterName);
			if (parameters.TryGetValue (key, out var value))
				return value;
			if (processorName != null || directiveId != null)
				return ResolveParameterValue (null, null, parameterName);
			return null;
		}
		
		protected virtual Type ResolveDirectiveProcessor (string processorName)
		{
			if (!directiveProcessors.TryGetValue (processorName, out KeyValuePair<string, string> value))
				throw new Exception (string.Format ("No directive processor registered as '{0}'", processorName));
			var asmPath = ResolveAssemblyReference (value.Value);
			if (asmPath == null)
				throw new Exception (string.Format ("Could not resolve assembly '{0}' for directive processor '{1}'", value.Value, processorName));
			var asm = Assembly.LoadFrom (asmPath);
			return asm.GetType (value.Key, true);
		}
		
		protected virtual string ResolvePath (string path)
		{
			path = Environment.ExpandEnvironmentVariables (path);
			if (Path.IsPathRooted (path))
				return path;
			var dir = Path.GetDirectoryName (TemplateFile);
			var test = Path.Combine (dir, path);
			if (File.Exists (test) || Directory.Exists (test))
				return test;
			return path;
		}
		
		#endregion
		
		readonly Dictionary<ParameterKey,string> parameters = new Dictionary<ParameterKey, string> ();
		readonly Dictionary<string,KeyValuePair<string,string>> directiveProcessors = new Dictionary<string, KeyValuePair<string,string>> ();
		
		public void AddDirectiveProcessor (string name, string klass, string assembly)
		{
			directiveProcessors.Add (name, new KeyValuePair<string,string> (klass,assembly));
		}
		
		public void AddParameter (string processorName, string directiveName, string parameterName, string value)
		{
			parameters.Add (new ParameterKey (processorName, directiveName, parameterName), value);
		}

		/// <summary>
		/// Parses a parameter and adds it.
		/// </summary>
		/// <returns>Whether the parameter was parsed successfully.</returns>
		/// <param name="unparsedParameter">Parameter in name=value or processor!directive!name!value format.</param>
		public bool TryAddParameter (string unparsedParameter)
		{
			if (TryParseParameter (unparsedParameter, out string processor, out string directive, out string name, out string value)) {
				AddParameter (processor, directive, name, value);
				return true;
			}
			return false;
		}

		internal static bool TryParseParameter (string parameter, out string processor, out string directive, out string name, out string value)
		{
			processor = directive = name = value = "";

			int start = 0;
			int end = parameter.IndexOfAny (new [] { '=', '!' });
			if (end < 0)
				return false;

			//simple format n=v
			if (parameter [end] == '=') {
				name = parameter.Substring (start, end);
				value = parameter.Substring (end + 1);
				return !string.IsNullOrEmpty (name);
			}

			//official format, p!d!n!v
			processor = parameter.Substring (start, end);

			start = end + 1;
			end = parameter.IndexOf ('!', start);
			if (end < 0) {
				//unlike official version, we allow you to omit processor/directive
				name = processor;
				value = parameter.Substring (start);
				processor = "";
				return !string.IsNullOrEmpty (name);
			}

			directive = parameter.Substring (start, end - start);


			start = end + 1;
			end = parameter.IndexOf ('!', start);
			if (end < 0) {
				//we also allow you just omit the processor
				name = directive;
				directive = processor;
				value = parameter.Substring (start);
				processor = "";
				return !string.IsNullOrEmpty (name);
			}

			name = parameter.Substring (start, end - start);
			value = parameter.Substring (end + 1);

			return !string.IsNullOrEmpty (name);
		}
		
		protected virtual bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			content = "";
			location = ResolvePath (requestFileName);
			
			if (location == null || !File.Exists (location)) {
				foreach (string path in IncludePaths) {
					string f = Path.Combine (path, requestFileName);
					if (File.Exists (f)) {
						location = f;
						break;
					}
				}
			}
			
			if (location == null)
				return false;
			
			try {
				content = File.ReadAllText (location);
				return true;
			} catch (IOException ex) {
				AddError ("Could not read included file '" + location + "':\n" + ex);
			}
			return false;
		}
		
		#region Explicit ITextTemplatingEngineHost implementation
		
		bool ITextTemplatingEngineHost.LoadIncludeText (string requestFileName, out string content, out string location)
		{
			return LoadIncludeText (requestFileName, out content, out location);
		}
		
		void ITextTemplatingEngineHost.LogErrors (CompilerErrorCollection errors)
		{
			this.Errors.AddRange (errors);
		}
		
		string ITextTemplatingEngineHost.ResolveAssemblyReference (string assemblyReference)
		{
			return ResolveAssemblyReference (assemblyReference);
		}
		
		string ITextTemplatingEngineHost.ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			return ResolveParameterValue (directiveId, processorName, parameterName);
		}
		
		Type ITextTemplatingEngineHost.ResolveDirectiveProcessor (string processorName)
		{
			return ResolveDirectiveProcessor (processorName);
		}
		
		string ITextTemplatingEngineHost.ResolvePath (string path)
		{
			return ResolvePath (path);
		}
		
		void ITextTemplatingEngineHost.SetFileExtension (string extension)
		{
			extension = extension.TrimStart ('.');
			if (Path.HasExtension (OutputFile)) {
				OutputFile = Path.ChangeExtension (OutputFile, extension);
			} else {
				OutputFile = OutputFile + "." + extension;
			}
		}
		
		void ITextTemplatingEngineHost.SetOutputEncoding (Encoding encoding, bool fromOutputDirective)
		{
			this.encoding = encoding;
		}
		
		IList<string> ITextTemplatingEngineHost.StandardAssemblyReferences {
			get { return Refs; }
		}
		
		IList<string> ITextTemplatingEngineHost.StandardImports {
			get { return Imports; }
		}
		
		
		#endregion
		
		struct ParameterKey : IEquatable<ParameterKey>
		{
			public ParameterKey (string processorName, string directiveName, string parameterName)
			{
				this.processorName = processorName ?? "";
				this.directiveName = directiveName ?? "";
				this.parameterName = parameterName ?? "";
				unchecked {
					hashCode = this.processorName.GetHashCode ()
						^ this.directiveName.GetHashCode ()
						^ this.parameterName.GetHashCode ();
				}
			}
			
			string processorName, directiveName, parameterName;
			readonly int hashCode;
			
			public override bool Equals (object obj)
			{
				return obj is ParameterKey && Equals ((ParameterKey)obj);
			}
			
			public bool Equals (ParameterKey other)
			{
				return processorName == other.processorName && directiveName == other.directiveName && parameterName == other.parameterName;
			}
			
			public override int GetHashCode ()
			{
				return hashCode;
			}
		}

		/// <summary>
		/// If non-null, the template's Host property will be the full type of this host.
		/// </summary>
		public virtual Type SpecificHostType { get { return null; } }

		/// <summary>
		/// Gets any additional directive processors to be included in the processing run.
		/// </summary>
		public virtual IEnumerable<IDirectiveProcessor> GetAdditionalDirectiveProcessors ()
		{
			yield break;
		}
	}
}
