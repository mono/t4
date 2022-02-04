//
// Engine.cs
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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CSharp;
using Microsoft.VisualStudio.TextTemplating;

using Mono.TextTemplating.CodeCompilation;

namespace Mono.TextTemplating
{
	public partial class TemplatingEngine :
#if FEATURE_APPDOMAINS
		MarshalByRefObject,
#endif
#pragma warning disable 618
		ITextTemplatingEngine
#pragma warning restore 618
	{
		Func<RuntimeInfo,CodeCompilation.CodeCompiler> createCompilerFunc;
		CodeCompilation.CodeCompiler cachedCompiler;

		internal void SetCompilerFunc (Func<RuntimeInfo,CodeCompilation.CodeCompiler> createCompiler)
		{
			cachedCompiler = null;
			createCompilerFunc = createCompiler;
		}

		CodeCompilation.CodeCompiler GetOrCreateCompiler ()
		{
			if (cachedCompiler == null) {
				var runtime = RuntimeInfo.GetRuntime ();
				if (runtime.Error != null) {
					throw new TemplatingEngineException (runtime.Error);
				}
				cachedCompiler = createCompilerFunc?.Invoke (runtime) ?? new CscCodeCompiler (runtime);
			}
			return cachedCompiler;
		}

		[Obsolete("Use ProcessTemplateAsync")]
		public string ProcessTemplate (string content, ITextTemplatingEngineHost host)
		{
			return ProcessTemplateAsync (content, host).Result;
		}

		public async Task<string> ProcessTemplateAsync (string content, ITextTemplatingEngineHost host, CancellationToken token = default)
		{
			using var tpl = await CompileTemplateAsync (content, host, token).ConfigureAwait (false);
			return tpl?.Process ();
		}

		public async Task<string> ProcessTemplateAsync (ParsedTemplate pt, string content, TemplateSettings settings, ITextTemplatingEngineHost host, CancellationToken token = default)
		{
			var tpl = await CompileTemplateAsync (pt, content, host, settings, token).ConfigureAwait (false);
			using (tpl?.template) {
				return tpl?.template.Process ();
			}
		}

		public string PreprocessTemplate (string content, ITextTemplatingEngineHost host, string className,
			string classNamespace, out string language, out string [] references)
		{
			if (content == null)
				throw new ArgumentNullException (nameof (content));
			if (host == null)
				throw new ArgumentNullException (nameof (host));
			if (className == null)
				throw new ArgumentNullException (nameof (className));
			language = null;
			references = null;

			var pt = ParsedTemplate.FromTextInternal (content, host);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			return PreprocessTemplateInternal (pt, content, host, className, classNamespace, out language, out references);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Performance", "CA1822:Mark members as static", Justification = "API compat")]
		public string PreprocessTemplate (ParsedTemplate pt, string content, TemplateSettings settings, ITextTemplatingEngineHost host, out string language, out string[] references)
		{
			if (pt is null) throw new ArgumentNullException (nameof (pt));
			if (string.IsNullOrEmpty (content)) throw new ArgumentException ($"'{nameof (content)}' cannot be null or empty.", nameof (content));
			if (settings is null) throw new ArgumentNullException (nameof (settings));
			if (host is null) throw new ArgumentNullException (nameof (host));

			return PreprocessTemplateInternal (pt, content, settings, host, out language, out references);
		}

		[Obsolete("Use TemplateGenerator")]
		public string PreprocessTemplate (ParsedTemplate pt, string content, ITextTemplatingEngineHost host, string className,
			string classNamespace, out string language, out string [] references, TemplateSettings settings = null)
		{
			if (content == null)
				throw new ArgumentNullException (nameof (content));
			if (pt == null)
				throw new ArgumentNullException (nameof (pt));
			if (host == null)
				throw new ArgumentNullException (nameof (host));
			if (className == null)
				throw new ArgumentNullException (nameof (className));

			return PreprocessTemplateInternal (pt, content, host, className, classNamespace, out language, out references, settings);
		}

		static string PreprocessTemplateInternal (ParsedTemplate pt, string content, ITextTemplatingEngineHost host, string className,
			string classNamespace, out string language, out string[] references, TemplateSettings settings = null)
		{

			settings ??= GetSettings (host, pt);

			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				language = null;
				references = null;
				return null;
			}

			if (className != null) {
				settings.Name = className;
			}
			if (classNamespace != null) {
				settings.Namespace = classNamespace;
			}

			return PreprocessTemplateInternal (pt, content, settings, host, out language, out references);
		}

		internal static string PreprocessTemplateInternal (ParsedTemplate pt, string content, TemplateSettings settings, ITextTemplatingEngineHost host, out string language, out string[] references)
		{
			settings.IncludePreprocessingHelpers = string.IsNullOrEmpty (settings.Inherits);
			settings.IsPreprocessed = true;
			language = settings.Language;

			var ccu = GenerateCompileUnit (host, content, pt, settings);
			references = ProcessReferences (host, pt, settings).ToArray ();

			host.LogErrors (pt.Errors);
			if (pt.Errors.HasErrors) {
				return null;
			}

			var options = new CodeGeneratorOptions ();
			using var sw = new StringWriter ();
			settings.Provider.GenerateCodeFromCompileUnit (ccu, sw, options);
			return sw.ToString ();
		}

		[Obsolete("Use CompileTemplateAsync")]
		public CompiledTemplate CompileTemplate (string content, ITextTemplatingEngineHost host)
			=> CompileTemplateAsync (content, host, CancellationToken.None).Result;

		public async Task<CompiledTemplate> CompileTemplateAsync (string content, ITextTemplatingEngineHost host, CancellationToken token)
		{
			if (content == null)
				throw new ArgumentNullException (nameof (content));
			if (host == null)
				throw new ArgumentNullException (nameof (host));

			var pt = ParsedTemplate.FromTextInternal (content, host);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}

			var tpl = await CompileTemplateInternal (pt, content, host, null, token).ConfigureAwait (false);
			return tpl?.template;
		}

		[Obsolete("Use CompileTemplateAsync")]
		public CompiledTemplate CompileTemplate (
			ParsedTemplate pt,
			string content,
			ITextTemplatingEngineHost host,
			TemplateSettings settings = null)
			=> CompileTemplate (pt, content, host, out var _, settings);

		[Obsolete("Use CompileTemplateAsync")]
		public CompiledTemplate CompileTemplate (
			ParsedTemplate pt,
			string content,
			ITextTemplatingEngineHost host,
			out string[] references,
			TemplateSettings settings = null)
		{
			var result = CompileTemplateAsync (pt, content, host, settings, CancellationToken.None).Result;
			references = result?.references;
			return result?.template;
		}

		public Task<(CompiledTemplate template, string[] references)?> CompileTemplateAsync (
			ParsedTemplate pt,
			string content,
			ITextTemplatingEngineHost host,
			TemplateSettings settings = null,
			CancellationToken token = default)
		{
			if (pt == null)
				throw new ArgumentNullException (nameof (pt));
			if (host == null)
				throw new ArgumentNullException (nameof (host));

			return CompileTemplateInternal (pt, content, host, settings, token);
		}

		async Task<(CompiledTemplate template, string[] references)?> CompileTemplateInternal (
			ParsedTemplate pt,
			string content,
			ITextTemplatingEngineHost host,
			TemplateSettings settings,
			CancellationToken token
			)
		{

			settings ??= GetSettings (host, pt);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}

			if (!string.IsNullOrEmpty (settings.Extension)) {
				host.SetFileExtension (settings.Extension);
			}
			if (settings.Encoding != null) {
				//FIXME: when is this called with false?
				host.SetOutputEncoding (settings.Encoding, true);
			}

			var ccu = GenerateCompileUnit (host, content, pt, settings);
			var references = ProcessReferences (host, pt, settings);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}

			(var results, var assembly) = await CompileCode (references, settings, ccu, token).ConfigureAwait (false);
			if (results.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				host.LogErrors (results.Errors);
				return null;
			}

#if FEATURE_APPDOMAINS
			var domain = host.ProvideTemplatingAppDomain (content);
			var templateClassFullName = string.Concat(settings.Namespace, ".", settings.Name);
			if (domain != null) {
				var type = typeof(CompiledTemplate);
				var obj = domain.CreateInstanceFromAndUnwrap (type.Assembly.Location,
					type.FullName,
					new object[] { host, results, templateClassFullName, settings.Culture, references.ToArray () });

				return ((CompiledTemplate)obj, references);
			}
#endif

			return (new CompiledTemplate (host, assembly, settings.GetFullName (), settings.Culture, references), references);
		}

		async Task<(CompilerResults, CompiledAssemblyData)> CompileCode (IEnumerable<string> references, TemplateSettings settings, CodeCompileUnit ccu, CancellationToken token)
		{
			string sourceText;
			var genOptions = new CodeGeneratorOptions ();
			using (var sw = new StringWriter ()) {
				settings.Provider.GenerateCodeFromCompileUnit (ccu, sw, genOptions);
				sourceText = sw.ToString ();
			}

			CompiledAssemblyData compiledAssembly = null;

			// this may throw, so do it before writing source files
			var compiler = GetOrCreateCompiler ();

			// GetTempFileName guarantees that the returned file name is unique, but
			// there are no equivalent for directories, so we create a directory
			// based on the file name, which *should* be unique as long as the file
			// exists.
			var tempFile = Path.GetTempFileName ();
			var tempFolder = tempFile + "dir";
			Directory.CreateDirectory (tempFolder);

			if (settings.Log != null) {
				settings.Log.WriteLine ($"Generating code in '{tempFolder}'");
			}

			var sourceFilename = Path.Combine (tempFolder, settings.Name + "." + settings.Provider.FileExtension);
			File.WriteAllText (sourceFilename, sourceText);

			var args = new CodeCompilerArguments ();
			args.AssemblyReferences.AddRange (references);
			args.Debug = settings.Debug;
			args.SourceFiles.Add (sourceFilename);

			if (settings.CompilerOptions != null) {
				args.AdditionalArguments = settings.CompilerOptions;
			}

			args.OutputPath = Path.Combine (tempFolder, settings.Name + ".dll");
			args.TempDirectory = tempFolder;
			args.LangVersion = settings.LangVersion;

			var result = await compiler.CompileFile (args, settings.Log, token).ConfigureAwait (false);

			var r = new CompilerResults (new TempFileCollection ());
			r.TempFiles.AddFile (sourceFilename, false);

			if (result.ResponseFile != null) {
				r.TempFiles.AddFile (result.ResponseFile, false);
			}

			r.NativeCompilerReturnValue = result.ExitCode;
			r.Output.AddRange (result.Output.ToArray ());
			r.Errors.AddRange (result.Errors.Select (e => new CompilerError (e.Origin ?? "", e.Line, e.Column, e.Code, e.Message) { IsWarning = !e.IsError }).ToArray ());

			if (result.Success) {
				r.TempFiles.AddFile (args.OutputPath, args.Debug);

				// load the assembly in memory so we can fully clean our temporary folder
				// NOTE: we do NOT assembly.load it here, as it will likely need to be loaded
				// into a different AssemblyLoadContext or AppDomain
				byte[] assembly = File.ReadAllBytes (args.OutputPath);
				byte[] debugSymbols = null;

				if (args.Debug) {
					var symbolsPath = Path.ChangeExtension (args.OutputPath, ".pdb");
					// if the symbols are embedded the symbols file doesn't exist
					if (File.Exists(symbolsPath)) {
						r.TempFiles.AddFile (symbolsPath, true);
						debugSymbols = File.ReadAllBytes (symbolsPath);
					}
				}

				compiledAssembly = new CompiledAssemblyData (assembly, debugSymbols);
			} else if (!r.Errors.HasErrors) {
				r.Errors.Add (new CompilerError (null, 0, 0, null, $"The compiler exited with code {result.ExitCode}"));
			}

			if (!args.Debug && !r.Errors.HasErrors) {
				r.TempFiles.Delete ();
				// we can delete our temporary file after our temporary folder is deleted.
				Directory.Delete (tempFolder);
				File.Delete (tempFile);
			}

			return (r, compiledAssembly);
		}

		static string [] ProcessReferences (ITextTemplatingEngineHost host, ParsedTemplate pt, TemplateSettings settings)
		{
			var resolved = new Dictionary<string, string> ();

			foreach (string assem in settings.Assemblies.Union (host.StandardAssemblyReferences)) {
				if (resolved.ContainsValue (assem))
					continue;

				string resolvedAssem = host.ResolveAssemblyReference (assem);
				if (!string.IsNullOrEmpty (resolvedAssem)) {
					var assemblyName = resolvedAssem;
					if (File.Exists (resolvedAssem))
						assemblyName = AssemblyName.GetAssemblyName (resolvedAssem).FullName;
					resolved [assemblyName] = resolvedAssem;
				} else {
					pt.LogError ("Could not resolve assembly reference '" + assem + "'");
					return null;
				}
			}
			return resolved.Values.ToArray ();
		}

		public static TemplateSettings GetSettings (ITextTemplatingEngineHost host, ParsedTemplate pt)
		{
			var settings = new TemplateSettings ();

			bool relativeLinePragmas = host.GetHostOption ("UseRelativeLinePragmas") as bool? ?? false;

			foreach (Directive dt in pt.Directives) {
				switch (dt.Name.ToLowerInvariant ()) {
				case "template":
					string val = dt.Extract ("language");
					if (val != null)
						settings.Language = val;
					if (dt.Extract ("langversion") is string langVersion)
						settings.LangVersion = langVersion;
					val = dt.Extract ("debug");
					if (val != null)
						settings.Debug = string.Equals (val, "true", StringComparison.OrdinalIgnoreCase);
					val = dt.Extract ("inherits");
					if (val != null)
						settings.Inherits = val;
					val = dt.Extract ("culture");
					if (val != null) {
						var culture = System.Globalization.CultureInfo.GetCultureInfo (val);
						if (culture == null)
							pt.LogWarning ("Could not find culture '" + val + "'", dt.StartLocation);
						else
							settings.Culture = culture;
					}
					val = dt.Extract ("hostspecific");
					if (val != null) {
						if (string.Equals (val, "trueFromBase", StringComparison.OrdinalIgnoreCase)) {
							settings.HostPropertyOnBase = true;
							settings.HostSpecific = true;
						} else {
							settings.HostSpecific = string.Equals (val, "true", StringComparison.OrdinalIgnoreCase);
						}
					}
					val = dt.Extract ("CompilerOptions");
					if (val != null) {
						settings.CompilerOptions = val;
					}
					val = dt.Extract ("relativeLinePragmas");
					if (val != null) {
						relativeLinePragmas = string.Equals (val, "true", StringComparison.OrdinalIgnoreCase);
					}
					val = dt.Extract ("linePragmas");
					if (val != null) {
						settings.NoLinePragmas = string.Equals (val, "false", StringComparison.OrdinalIgnoreCase);
					}
					val = dt.Extract ("visibility");
					if (val != null) {
						settings.InternalVisibility = string.Equals (val, "internal", StringComparison.OrdinalIgnoreCase);
					}
					break;

				case "assembly":
					string name = dt.Extract ("name");
					if (name == null)
						pt.LogError ("Missing name attribute in assembly directive", dt.StartLocation);
					else
						settings.Assemblies.Add (name);
					break;

				case "import":
					string namespac = dt.Extract ("namespace");
					if (namespac == null)
						pt.LogError ("Missing namespace attribute in import directive", dt.StartLocation);
					else
						settings.Imports.Add (namespac);
					break;

				case "output":
					settings.Extension = dt.Extract ("extension");
					string encoding = dt.Extract ("encoding");
					if (encoding != null)
						settings.Encoding = Encoding.GetEncoding (encoding);
					break;

				case "include":
					throw new InvalidOperationException ("Include is handled in the parser");

				case "parameter":
					AddDirective (settings, host, nameof (ParameterDirectiveProcessor), dt);
					continue;

				default:
					string processorName = dt.Extract ("Processor");
					if (processorName == null)
						throw new InvalidOperationException ("Custom directive '" + dt.Name + "' does not specify a processor");

					AddDirective (settings, host, processorName, dt);
					continue;
				}
				ComplainExcessAttributes (dt, pt);
			}

			if (host is TemplateGenerator gen) {
				settings.HostType = gen.SpecificHostType;
				foreach (var processor in gen.GetAdditionalDirectiveProcessors ()) {
					settings.DirectiveProcessors [processor.GetType ().FullName] = processor;
				}
			}

			if (settings.HostType != null) {
				settings.Assemblies.Add (settings.HostType.Assembly.Location);
			}

			//initialize the custom processors
			foreach (var kv in settings.DirectiveProcessors) {
				kv.Value.Initialize (host);

				IRecognizeHostSpecific hs;
				if (settings.HostSpecific || (
				        !((IDirectiveProcessor)kv.Value).RequiresProcessingRunIsHostSpecific &&
				        ((hs = kv.Value as IRecognizeHostSpecific) == null || !hs.RequiresProcessingRunIsHostSpecific)))
					continue;

				settings.HostSpecific = true;
				pt.LogWarning ("Directive processor '" + kv.Key + "' requires hostspecific=true, forcing on.");
			}

			foreach (var kv in settings.DirectiveProcessors) {
				kv.Value.SetProcessingRunIsHostSpecific (settings.HostSpecific);
				if (kv.Value is IRecognizeHostSpecific hs)
					hs.SetProcessingRunIsHostSpecific (settings.HostSpecific);
			}

			if (settings.Name == null)
				settings.Name = "GeneratedTextTransformation";
			if (settings.Namespace == null)
				settings.Namespace = $"{typeof (TextTransformation).Namespace}{new Random ().Next ():x}";

			//resolve the CodeDOM provider
			if (string.IsNullOrEmpty (settings.Language)) {
				settings.Language = "C#";
			}

			if (settings.Language == "C#v3.5") {
				pt.LogWarning ("The \"C#3.5\" Language attribute in template directives is deprecated, use the langversion attribute instead");
				settings.Provider = new CSharpCodeProvider (new Dictionary<string, string> {
					{ "CompilerVersion", "v3.5" }
				});
			} else {
				settings.Provider = CodeDomProvider.CreateProvider (settings.Language);
			}

			if (settings.Provider == null) {
				pt.LogError ("A provider could not be found for the language '" + settings.Language + "'");
				return settings;
			}

			settings.RelativeLinePragmas = relativeLinePragmas;

			return settings;
		}

		static void AddDirective (TemplateSettings settings, ITextTemplatingEngineHost host, string processorName, Directive directive)
		{
			if (!settings.DirectiveProcessors.TryGetValue (processorName, out IDirectiveProcessor processor)) {
				switch (processorName) {
				case "ParameterDirectiveProcessor":
					processor = new ParameterDirectiveProcessor ();
					break;
				default:
					Type processorType = host.ResolveDirectiveProcessor (processorName);
					processor = (IDirectiveProcessor)Activator.CreateInstance (processorType);
					break;
				}
				settings.DirectiveProcessors[processorName] = processor;
			}

			if (!processor.IsDirectiveSupported (directive.Name))
				throw new InvalidOperationException ("Directive processor '" + processorName + "' does not support directive '" + directive.Name + "'");

			settings.CustomDirectives.Add (new CustomDirective (processorName, directive));
		}

		static bool ComplainExcessAttributes (Directive dt, ParsedTemplate pt)
		{
			if (dt.Attributes.Count == 0)
				return false;
			var sb = new StringBuilder ("Unknown attributes ");
			bool first = true;
			foreach (string key in dt.Attributes.Keys) {
				if (!first) {
					sb.Append (", ");
				} else {
					first = false;
				}
				sb.Append (key);
			}
			sb.Append (" found in ");
			sb.Append (dt.Name);
			sb.Append (" directive.");
			pt.LogWarning (sb.ToString (), dt.StartLocation);
			return false;
		}
	}
}