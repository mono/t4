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
using Mono.Options;

namespace Mono.TextTemplating
{
	class TextTransform
	{
		static OptionSet optionSet;

		public static int Main (string [] args)
		{
			try {
				return MainInternal (args);
			} catch (Exception e) {
				Console.Error.WriteLine (e);
				return -1;
			}
		}

		static int MainInternal (string [] args)
		{
			if (args.Length == 0) {
				ShowHelp (true);
			}

			var generator = new TemplateGenerator ();
			string outputFile = null, inputFile = null;
			var directives = new List<string> ();
			var parameters = new List<string> ();
			string preprocess = null;

			optionSet = new OptionSet () {
				{
					"o=|out=",
					"Name or path of the output {<file>}. Defaults to the input filename with its " +
					"extension changed to `.txt'.",
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
					(s) => preprocess = s
				},
				{ "dp=", "Directive processor (name!class!assembly)", s => directives.Add (s) },
				{ "a=|arg=", "Parameters (name=value) or ([processorName!][directiveName!]name!value)", s => parameters.Add (s) },
				{ "h|?|help", "Show help", s => ShowHelp (false) },
		//		{ "k=,", "Session {key},{value} pairs", (s, t) => session.Add (s, t) },
			};

			var remainingArgs = optionSet.Parse (args);

			if (remainingArgs.Count != 1) {
				Console.Error.WriteLine ("No input file specified.");
				return -1;
			}
			inputFile = remainingArgs [0];

			if (!File.Exists (inputFile)) {
				Console.Error.WriteLine ("Input file '{0}' does not exist.", inputFile);
				return -1;
			}

			if (string.IsNullOrEmpty (outputFile)) {
				outputFile = inputFile;
				if (Path.HasExtension (outputFile)) {
					var dir = Path.GetDirectoryName (outputFile);
					var fn = Path.GetFileNameWithoutExtension (outputFile);
					outputFile = Path.Combine (dir, fn + ".txt");
				} else {
					outputFile = outputFile + ".txt";
				}
			}

			foreach (var par in parameters) {
				if (!generator.TryAddParameter (par)) {
					Console.Error.WriteLine ("Parameter has incorrect format: {0}", par);
					return -1;
				}
			}

			foreach (var dir in directives) {
				var split = dir.Split ('!');

				if (split.Length != 3) {
					Console.Error.WriteLine ("Directive must have 3 values: {0}", dir);
					return -1;
				}

				for (int i = 0; i < 3; i++) {
					string s = split [i];
					if (string.IsNullOrEmpty (s)) {
						string kind = i == 0 ? "name" : (i == 1 ? "class" : "assembly");
						Console.Error.WriteLine ("Directive has missing {0} value: {1}", kind, dir);
						return -1;
					}
				}

				generator.AddDirectiveProcessor (split [0], split [1], split [2]);
			}

			if (preprocess == null) {
				generator.ProcessTemplate (inputFile, outputFile);
				if (generator.Errors.HasErrors) {
					Console.WriteLine ("Processing '{0}' failed.", inputFile);
				}
			} else {
				string className = preprocess;
				string classNamespace = null;
				int s = preprocess.LastIndexOf ('.');
				if (s > 0) {
					classNamespace = preprocess.Substring (0, s);
					className = preprocess.Substring (s + 1);
				}

				generator.PreprocessTemplate (inputFile, className, classNamespace, outputFile, System.Text.Encoding.UTF8,
					out string language, out string [] references);
				if (generator.Errors.HasErrors) {
					Console.Write ("Preprocessing '{0}' into class '{1}.{2}' failed.", inputFile, classNamespace, className);
				}
			}

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

			return generator.Errors.HasErrors ? -1 : 0;
		}

		static void ShowHelp (bool concise)
		{
			var name = Path.GetFileNameWithoutExtension (Assembly.GetExecutingAssembly ().Location);
			Console.WriteLine ("T4 text template processor", name);
			Console.WriteLine ("Usage: {0} [options] input-file", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ("Options:");
				optionSet.WriteOptionDescriptions (Console.Out);
			}
			Console.WriteLine ();
			Environment.Exit (0);
		}
	}
}