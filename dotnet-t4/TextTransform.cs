// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Options;
using System.Linq;
using Microsoft.VisualStudio.TextTemplating;
using System.CodeDom.Compiler;

namespace Mono.TextTemplating
{
	class TextTransform
	{
		static OptionSet optionSet, compatOptionSet;

		public static int Main (string [] args)
		{
			try {
				return MainInternal (args);
			}
			catch (Exception e) {
				Console.Error.WriteLine (e);
				return -1;
			}
		}

		static int MainInternal (string [] args)
		{
			if (args.Length == 0) {
				ShowHelp (true);
			}

			var generator = new ToolTemplateGenerator ();
			string outputFile = null, inputFile = null;
			var directives = new List<string> ();
			var parameters = new List<string> ();
			var properties = new Dictionary<string,string> ();
			string preprocessClassName = null;

			optionSet = new OptionSet {
				{
					"o=|out=",
					"Name or path of the output {<file>}. Defaults to the input filename with its " +
					"extension changed to `.txt'. Use `-' to output to stdout.",
					s => outputFile = s
				},
				{
					"r=",
					"Name or path of an {<assembly>} reference. Assemblies will be resolved from the " +
					"framework and the include folders",
					s => generator.Refs.Add (s)
				},
				{
					"u=|using=",
					"Import a {<namespace>}' statement with a `using",
					s => generator.Imports.Add (s)
				},
				{
					"I=",
					"Search {<directory>} when resolving file includes",
					s => generator.IncludePaths.Add (s)
				},
				{
					"P=",
					"Search {<directory>} when resolving assembly references",
					s => generator.ReferencePaths.Add (s)
				},
				{
					"c=|class=",
					"Preprocess the template into class {<name>}",
					(s) => preprocessClassName = s
				},
				{
					"p:=",
					"Add a {<name>}={<value>} key-value pair to the template's `Session' " +
					"dictionary. These can also be accessed using strongly typed " +
					"properties declared with `<#@ parameter name=\"<name>\" type=\"<type>\" #> " +
					"directives.",
					(k,v) => properties[k]=v
				},
				{
					"h|?|help",
					"Show help",
					s => ShowHelp (false)
				}
			};

			compatOptionSet = new OptionSet {
				{
					"dp=",
					"Directive processor (name!class!assembly)",
					s => directives.Add (s)
				},
				{
					"a=",
					"Parameters (name=value) or ([processorName!][directiveName!]name!value)",
					s => parameters.Add (s)
				},
			};

			var remainingArgs = optionSet.Parse (args);
			remainingArgs = compatOptionSet.Parse (args);

			string inputContent = null;
			if (remainingArgs.Count != 1) {
				if (Console.IsInputRedirected) {
					inputContent = Console.In.ReadToEnd ();
				} else {
					Console.Error.WriteLine ("No input file specified.");
					return 1;
				}
			} else {
				inputFile = remainingArgs [0];
				if (!File.Exists (inputFile)) {
					Console.Error.WriteLine ("Input file '{0}' does not exist.", inputFile);
					return 1;
				}
			}

			bool writeToStdout = outputFile == "-";
			if (!writeToStdout && string.IsNullOrEmpty (outputFile)) {
				outputFile = inputFile;
				if (Path.HasExtension (outputFile)) {
					var dir = Path.GetDirectoryName (outputFile);
					var fn = Path.GetFileNameWithoutExtension (outputFile);
					outputFile = Path.Combine (dir, fn + ".txt");
				} else {
					outputFile = outputFile + ".txt";
				}
			}

			if (inputFile != null) {
				try {
					inputContent = File.ReadAllText (inputFile);
				}
				catch (IOException ex) {
					Console.Error.WriteLine ("Could not read input file '" + inputFile + "':\n" + ex);
					return 1;
				}
			}

			if (inputContent.Length == 0) {
				Console.Error.WriteLine ("Input is empty");
				return 1;
			}

			foreach (var par in parameters) {
				if (!generator.TryAddParameter (par)) {
					Console.Error.WriteLine ("Parameter has incorrect format: {0}", par);
					return 1;
				}
			}

			if (!AddDirectiveProcessors (generator, directives)) {
				return 1;
			}

			var pt = ParsedTemplate.FromText (inputContent, generator);

			if (pt.Errors.Count > 0) {
				generator.Errors.AddRange (pt.Errors);
			}

			string outputContent = null;
			if (!generator.Errors.HasErrors) {
				AddCoercedSessionParameters (generator, pt, properties);
			}

			if (!generator.Errors.HasErrors) {
				if (preprocessClassName == null) {
					outputContent = generator.ProcessTemplate (pt, inputFile, inputContent, ref outputFile);
				} else {
					outputContent = generator.PreprocessTemplate (pt, inputFile, inputContent, preprocessClassName);
				}
			}

			if (generator.Errors.HasErrors) {
				Console.Error.WriteLine (inputFile == null ? "Processing failed." : $"Processing '{inputFile}' failed.");
			}

			try {
				if (!generator.Errors.HasErrors) {
					if (writeToStdout) {
						Console.WriteLine (outputContent);
					} else {
						File.WriteAllText (outputFile, outputContent, Encoding.UTF8);
					}
				}
			}
			catch (IOException ex) {
				Console.Error.WriteLine ("Could not write output file '" + outputFile + "':\n" + ex);
				return 1;
			}

			LogErrors (generator);

			return generator.Errors.HasErrors ? 1 : 0;
		}

		static void AddCoercedSessionParameters (ToolTemplateGenerator generator, ParsedTemplate pt, Dictionary<string, string> properties)
		{
			if (properties.Count == 0) {
				return;
			}

			var session = generator.CreateSession ();

			foreach (var p in properties) {
				var directive = pt.Directives.FirstOrDefault (d =>
					d.Name == "parameter" &&
					d.Attributes.TryGetValue ("name", out string attVal) &&
					attVal == p.Key);

				if (directive != null) {
					directive.Attributes.TryGetValue ("type", out string typeName);
					var mappedType = ParameterDirectiveProcessor.MapTypeName (typeName);
					if (mappedType != "System.String") {
						if (ConvertType (mappedType, p.Value, out object converted)) {
							session [p.Key] = converted;
							continue;
						}

						generator.Errors.Add (
							new CompilerError (
								null, 0, 0, null,
								$"Could not convert property '{p.Key}'='{p.Value}' to parameter type '{typeName}'"
							)
						);
					}
				}
				session [p.Key] = p.Value;
			}
		}

		static bool ConvertType (string typeName, string value, out object converted)
		{
			converted = null;
			try {
				var type = Type.GetType (typeName);
				if (type == null) {
					return false;
				}
				Type stringType = typeof (string);
				if (type == stringType) {
					return true;
				}
				var converter = System.ComponentModel.TypeDescriptor.GetConverter (type);
				if (converter == null || !converter.CanConvertFrom (stringType)) {
					return false;
				}
				converted = converter.ConvertFromString (value);
				return true;
			}
			catch {
			}
			return false;
		}

		static bool AddDirectiveProcessors (TemplateGenerator generator, List<string> directives)
		{
			foreach (var dir in directives) {
				var split = dir.Split ('!');

				if (split.Length != 3) {
					Console.Error.WriteLine ("Directive must have 3 values: {0}", dir);
					return false;
				}

				for (int i = 0; i < 3; i++) {
					string s = split [i];
					if (string.IsNullOrEmpty (s)) {
						string kind = i == 0 ? "name" : (i == 1 ? "class" : "assembly");
						Console.Error.WriteLine ("Directive has missing {0} value: {1}", kind, dir);
						return false;
					}
				}

				generator.AddDirectiveProcessor (split [0], split [1], split [2]);
			}
			return true;
		}

		static void LogErrors (TemplateGenerator generator)
		{
			foreach (System.CodeDom.Compiler.CompilerError err in generator.Errors) {
				var oldColor = Console.ForegroundColor;
				Console.ForegroundColor = err.IsWarning? ConsoleColor.Yellow : ConsoleColor.Red;
				if (!string.IsNullOrEmpty (err.FileName)) {
					Console.Error.Write (err.FileName);
				}
				if (err.Line > 0) {
					Console.Error.Write ("(");
					Console.Error.Write (err.Line);
					if (err.Column > 0) {
						Console.Error.Write (",");
						Console.Error.Write (err.Column);
					}
					Console.Error.Write (")");
				}
				if (!string.IsNullOrEmpty (err.FileName) || err.Line > 0) {
					Console.Error.Write (": ");
				}
				Console.Error.Write (err.IsWarning ? "WARNING: " : "ERROR: ");
				Console.Error.WriteLine (err.ErrorText);
				Console.ForegroundColor = oldColor;
			}
		}

		static void ShowHelp (bool concise)
		{
			var name = Path.GetFileNameWithoutExtension (Assembly.GetExecutingAssembly ().Location);
			Console.WriteLine ("T4 text template processor version {0}", ThisAssembly.AssemblyInformationalVersion);
			Console.WriteLine ("Usage: {0} [options] input-file", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				Console.WriteLine ();
				optionSet.WriteOptionDescriptions (Console.Out);
				Console.WriteLine ();
				Console.WriteLine ("TextTransform.exe compatibility options (deprecated):");
				Console.WriteLine ();
				compatOptionSet.WriteOptionDescriptions (Console.Out);
				Console.WriteLine ();
				Environment.Exit (0);
			}
		}
	}
}