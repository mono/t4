// 
// Tokeniser.cs
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
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,s
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace Mono.TextTemplating
{
	public class Tokeniser
	{
		private State nextState = State.Content;
		private Location nextStateLocation;
		private Location nextStateTagStartLocation;

		public Tokeniser (string fileName, string content)
		{
			State = State.Content;
			Content = content;
			Location = nextStateLocation = nextStateTagStartLocation = new Location (fileName, 1, 1);
		}

		public bool Advance ()
		{
			Value = null;
			State = nextState;
			Location = nextStateLocation;
			TagStartLocation = nextStateTagStartLocation;
			if (nextState == State.EOF)
				return false;
			nextState = GetNextStateAndCurrentValue ();
			return true;
		}

		private State GetNextStateAndCurrentValue ()
		{
			switch (State) {
			case State.Block:
			case State.Expression:
			case State.Helper:
				return GetBlockEnd ();

			case State.Directive:
				return NextStateInDirective ();

			case State.Content:
				return NextStateInContent ();

			case State.DirectiveName:
				return GetDirectiveName ();

			case State.DirectiveValue:
				return GetDirectiveValue ();

			default:
				throw new InvalidOperationException ("Unexpected state '" + State + "'");
			}
		}

		private State GetBlockEnd ()
		{
			var start = Position;
			for (; Position < Content.Length; Position++) {
				var c = Content[Position];
				nextStateTagStartLocation = nextStateLocation;
				nextStateLocation = nextStateLocation.AddCol ();
				if (c == '\r') {
					if (Position + 1 < Content.Length && Content[Position + 1] == '\n')
						Position++;
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (c == '>' && Content[Position - 1] == '#' && Content[Position - 2] != '\\') {
					Value = Content.Substring (start, Position - start - 1);
					Position++;
					TagEndLocation = nextStateLocation;

					//skip newlines directly after blocks, unless they're expressions
					if (State != State.Expression && (Position += IsNewLine ()) > 0) nextStateLocation = nextStateLocation.AddLine ();
					return State.Content;
				}
			}

			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}

		private State GetDirectiveName ()
		{
			var start = Position;
			for (; Position < Content.Length; Position++) {
				var c = Content[Position];
				if (!char.IsLetterOrDigit (c)) {
					Value = Content.Substring (start, Position - start);
					return State.Directive;
				}

				nextStateLocation = nextStateLocation.AddCol ();
			}

			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}

		private State GetDirectiveValue ()
		{
			var start = Position;
			int delimiter = '\0';
			for (; Position < Content.Length; Position++) {
				var c = Content[Position];
				nextStateLocation = nextStateLocation.AddCol ();
				if (c == '\r') {
					if (Position + 1 < Content.Length && Content[Position + 1] == '\n')
						Position++;
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine ();
				}

				if (delimiter == '\0') {
					if (c == '\'' || c == '"') {
						start = Position;
						delimiter = c;
					} else if (!char.IsWhiteSpace (c)) {
						throw new ParserException ("Unexpected character '" + c + "'. Expecting attribute value.", nextStateLocation);
					}

					continue;
				}

				if (c == delimiter) {
					Value = Content.Substring (start + 1, Position - start - 1);
					Position++;
					return State.Directive;
				}
			}

			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}

		private State NextStateInContent ()
		{
			var start = Position;
			for (; Position < Content.Length; Position++) {
				var c = Content[Position];
				nextStateTagStartLocation = nextStateLocation;
				nextStateLocation = nextStateLocation.AddCol ();
				if (c == '\r') {
					if (Position + 1 < Content.Length && Content[Position + 1] == '\n')
						Position++;
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (c == '<' && Position + 2 < Content.Length && Content[Position + 1] == '#') {
					TagEndLocation = nextStateLocation;
					var type = Content[Position + 2];
					if (type == '@') {
						nextStateLocation = nextStateLocation.AddCols (2);
						Value = Content.Substring (start, Position - start);
						Position += 3;
						return State.Directive;
					}

					if (type == '=') {
						nextStateLocation = nextStateLocation.AddCols (2);
						Value = Content.Substring (start, Position - start);
						Position += 3;
						return State.Expression;
					}

					if (type == '+') {
						nextStateLocation = nextStateLocation.AddCols (2);
						Value = Content.Substring (start, Position - start);
						Position += 3;
						return State.Helper;
					}

					Value = Content.Substring (start, Position - start);
					nextStateLocation = nextStateLocation.AddCol ();
					Position += 2;
					return State.Block;
				}
			}

			//EOF is only valid when we're in content
			Value = Content.Substring (start);
			return State.EOF;
		}

		private int IsNewLine ()
		{
			var found = 0;

			if (Position < Content.Length && Content[Position] == '\r') found++;
			if (Position + found < Content.Length && Content[Position + found] == '\n') found++;
			return found;
		}

		private State NextStateInDirective ()
		{
			for (; Position < Content.Length; Position++) {
				var c = Content[Position];
				if (c == '\r') {
					if (Position + 1 < Content.Length && Content[Position + 1] == '\n')
						Position++;
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine ();
				} else if (char.IsLetter (c)) {
					return State.DirectiveName;
				} else if (c == '=') {
					nextStateLocation = nextStateLocation.AddCol ();
					Position++;
					return State.DirectiveValue;
				} else if (c == '#' && Position + 1 < Content.Length && Content[Position + 1] == '>') {
					Position += 2;
					TagEndLocation = nextStateLocation.AddCols (2);
					nextStateLocation = nextStateLocation.AddCols (3);

					//skip newlines directly after directives
					if ((Position += IsNewLine ()) > 0) nextStateLocation = nextStateLocation.AddLine ();

					return State.Content;
				} else if (!char.IsWhiteSpace (c)) {
					throw new ParserException ("Directive ended unexpectedly with character '" + c + "'", nextStateLocation);
				} else {
					nextStateLocation = nextStateLocation.AddCol ();
				}
			}

			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}

		public State State { get; private set; }

		public int Position { get; private set; }

		public string Content { get; }

		public string Value { get; private set; }

		public Location Location { get; private set; }
		public Location TagStartLocation { get; private set; }
		public Location TagEndLocation { get; private set; }
	}

	public enum State
	{
		Content = 0,
		Directive,
		Expression,
		Block,
		Helper,
		DirectiveName,
		DirectiveValue,
		Name,
		EOF
	}

	public class ParserException : Exception
	{
		public ParserException (string message, Location location) : base (message) => Location = location;

		public Location Location { get; }
	}
}