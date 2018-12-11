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

			foreach (var p in properties) {
				var session = generator.CreateSession ();
				session[p.Key] = p.Value;
			}

			foreach (var par in parameters) {
				if (!generator.TryAddParameter (par)) {
					Console.Error.WriteLine ("Parameter has incorrect format: {0}", par);
					return 1;
				}
			}

			if (!AddDirectiveProcessors (generator, directives))
				return 1;

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

			string outputContent;
			if (preprocessClassName == null) {
				generator.ProcessTemplate (inputFile, inputContent, ref outputFile, out outputContent);
			} else {
				generator.Preprocess (preprocessClassName, inputFile, inputContent, out outputContent);
			}

			if (generator.Errors.HasErrors) {
				Console.Error.WriteLine ("Processing '{0}' failed.", inputFile);
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
				if (err.FileName != null) {
					Console.Error.Write (err);
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
				if (err.FileName != null || err.Line > 0) {
					Console.Error.Write (": ");
				}
				Console.Error.Write (err.IsWarning ? "WARNING: " : "ERROR: ");
				Console.Error.WriteLine (err.ErrorText);
			}
		}

		static void ShowHelp (bool concise)
		{
			var name = Path.GetFileNameWithoutExtension (Assembly.GetExecutingAssembly ().Location);
			Console.WriteLine ("T4 text template processor");
			Console.WriteLine ("Usage: {0} [options] input-file", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				Console.WriteLine ();
				optionSet.WriteOptionDescriptions (Console.Out);
			}
			Console.WriteLine ();
			Console.WriteLine ("TextTransform.exe compatibility options (deprecated):");
			Console.WriteLine ();
			compatOptionSet.WriteOptionDescriptions (Console.Out);
			Console.WriteLine ();
			Environment.Exit (0);
		}
	}
}