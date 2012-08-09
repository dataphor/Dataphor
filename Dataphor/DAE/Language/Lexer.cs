/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.Language
{
    public enum TokenType { Unknown, Symbol, Nil, Boolean, Integer, Hex, Float, Decimal, Money, String, EOF, Error }

	public class LexerToken
	{
		public TokenType Type;
		public string Token;
		public int Line;
		public int LinePos;
		public Exception Error;

		/// <summary> Returns the currently active TokenType as a symbol. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a symbol. </remarks>
		public string AsSymbol
		{
			get
			{
				CheckType(TokenType.Symbol);
				return Token;
			}
		}

		/// <summary> Returns the currently active TokenType as a string. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a string. </remarks>
		public string AsString
		{
			get
			{
				CheckType(TokenType.String);
				return Token;
			}
		}

		/// <summary> Returns the currently active TokenType as a boolean value. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a boolean. </remarks>
		public bool AsBoolean
		{
			get
			{
				CheckType(TokenType.Boolean);
				return Token[0].Equals('t') || Token[0].Equals('T');
			}
		}

		/// <summary> Returns the currently active TokenType as an integer value. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not an integer. </remarks>
		public long AsInteger
		{
			get
			{
				CheckType(TokenType.Integer);
				return Int64.Parse(Token, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);
			}
		}
		
		public decimal AsDecimal
		{
			get
			{
				CheckType(TokenType.Decimal);
				return Decimal.Parse(Token, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		public decimal AsMoney
		{
			get
			{
				CheckType(TokenType.Money);
				return Decimal.Parse(Token, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a float value. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a float. </remarks>
		public double AsFloat
		{
			get
			{
				CheckType(TokenType.Float);
				return Double.Parse(Token, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a money value. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a money literal. </remarks>
		public long AsHex
		{
			get
			{
				CheckType(TokenType.Hex);
				return Int64.Parse(Token, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Ensures that the current TokenType is of the given type.  </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if it is not. </remarks>
		public void CheckType(TokenType token)
		{
			if (Type == TokenType.Error)
				throw Error;
			if (Type != token)
				throw new LexerException(LexerException.Codes.TokenExpected, Enum.GetName(typeof(TokenType), token));
		}

		/// <summary> Ensures that the current TokenType is a symbol equal to the given symbol.  </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if it is not. </remarks>
		public void CheckSymbol(string symbol)
		{
			if ((Type != TokenType.Symbol) || !String.Equals(Token, symbol, StringComparison.Ordinal))
				throw new LexerException(LexerException.Codes.SymbolExpected, symbol);
		}
	}

	/// <summary> General purpose lexical analyzer suitable for tokenizing statements of Dataphor and SQL. </summary>
	/// <remarks>
	///		Possible tokens are defined by the <see cref="LexerToken"/> enum.
    ///     Whitespace is defined by the <see cref="Char.IsWhiteSpace"/> method.  See the file BNF.txt for a
    ///		formal definition of the lexer.  The lexer is initially on a "crack" so the current (0th) entry 
	///		is not valid until NextToken() is invoked.
    /// </remarks>
    public class Lexer
    {
		private const int LookAheadCount = 3;

		/// <remarks> It is an error to access the current TokenType until <see cref="NextToken"/> has been called. </remarks>
        public Lexer(string input)
        {
			_tokenizer = new Tokenizer(input);
			for (int i = 0; i < LookAheadCount; i++)
				_tokens[i] = new LexerToken();
			for (int i = 0; i < (LookAheadCount - 1); i++)
				if (!ReadNext(i))
					break;
        }

		private Tokenizer _tokenizer;

		/// <summary> Buffer of tokens (clock allocation). </summary>
		private LexerToken[] _tokens = new LexerToken[LookAheadCount];
		/// <summary> Current position within the token buffer of the current token. </summary>
		/// <remarks> Initialized in the constructor to last position so that the lexer starts on a "crack". </remarks>
		private int _currentIndex = LookAheadCount - 1;

		/// <summary> Reads the next token into the specified location within the buffer. </summary>
		/// <returns> True if the read token type is not EOF. </returns>
		private bool ReadNext(int index)
		{
			LexerToken token = _tokens[index];
			try
			{
				token.Type = _tokenizer.NextToken();
				token.Token = _tokenizer.Token;
			}
			catch (Exception exception)
			{
				token.Type = TokenType.Error;
				token.Error = exception;
			}
			token.Line = _tokenizer.Line;
			token.LinePos = _tokenizer.LinePos;
			return (token.Type != TokenType.EOF) && (token.Type != TokenType.Error);
		}

		/// <summary> The token a specific number of tokens ahead of the current token. </summary>
		public LexerToken this[int index, bool checkActive]
		{
			get
			{
				System.Diagnostics.Debug.Assert(index < LookAheadCount, "Lexer look ahead attempt exceeds maximum");
				LexerToken token = _tokens[(_currentIndex + index) % LookAheadCount];
				if (checkActive && (token.Type == TokenType.Unknown))
					throw new LexerException(LexerException.Codes.NoActiveToken);
				return token;
			}
		}

		public LexerToken this[int index]
		{
			get
			{
				return this[index, true];
			}
		}

		/// <summary>Advances the current token.</summary>
		/// <returns>Returns the now active token.</returns>
		public LexerToken NextToken()
		{
			ReadNext(_currentIndex);
			_currentIndex = (_currentIndex + 1) % LookAheadCount;
			LexerToken token = _tokens[_currentIndex];
			if (token.Type == TokenType.EOF)
				throw new LexerException(LexerException.Codes.UnexpectedEOF);
			if (token.Type == TokenType.Error)
				throw token.Error;
			return token;
		}

		/// <summary> Gets the symbol the specified number of tokens ahead without advancing the current token. </summary>
		/// <remarks> If the token is not a symbol, returns an empty string. </remarks>
		public LexerToken PeekToken(int count)
		{
			LexerToken token = this[count];
			if (token.Type == TokenType.Error)
				throw token.Error;
			return token;
		}

		/// <summary> Gets the symbol the specified number of tokens ahead without advancing the current token. </summary>
		/// <remarks> If the token is not a symbol, returns an empty string. </remarks>
		public string PeekTokenSymbol(int count)
		{
			LexerToken token = this[count];
			if (token.Type == TokenType.Symbol)
				return token.Token;
			else if (token.Type == TokenType.Error)
				throw token.Error;
			else
				return String.Empty;
		}
    }

    public class Tokenizer
    {
		// Boolean constants
		public const string True = "true";
		public const string False = "false";
		public const string Nil = "nil";

		public Tokenizer(string input)
			: base()
		{
			_input = input;
			ReadNext();
		}

		private string _input;
		private int _pos = -1;

		private Char _current;
		public Char Current { get { return _current; } }

		private Char _next;
		public Char Next
		{
			get
			{
				if (_nextEOF)
					throw new LexerException(LexerException.Codes.UnexpectedEOF);
				return _next;
			}
		}

		private bool _nextEOF;
		public bool NextEOF { get { return _nextEOF; } }

		public Char Peek
		{
			get
			{
				if (PeekEOF)
					throw new LexerException(LexerException.Codes.UnexpectedEOF);
				return _input[_pos + 1];
			}
		}

		public bool PeekEOF
		{
			get
			{
				return _pos + 1 >= _input.Length;
			}
		}

		private int _line = 1;
		private int _linePos = 1;
		private int _tokenLine = 1;
		private int _tokenLinePos = 1;

		/// <summary> The line number (one-based) of the beginning of the last read token. </summary>
		public int Line { get { return _tokenLine; } }
		
		/// <summary> The offset position (one-based) of the beginning of the last read token. </summary>
		public int LinePos { get { return _tokenLinePos; } }

		private TokenType _tokenType;
		public TokenType TokenType { get { return _tokenType; } }

		private string _token;
		public string Token
		{
			get { return _token; }
		}

		private StringBuilder _builder = new StringBuilder();

		/// <summary> Pre-reads the next TokenType (does not affect current). </summary>
		private void ReadNext()
		{
			_pos++;
			_nextEOF = _pos >= _input.Length;
			if (!_nextEOF) 
				_next = _input[_pos];
		}

		public void Advance()
		{
			_current = Next;
			ReadNext();
			if (_current == '\n')
			{
				_line++;
				_linePos = 1;
			}
			else
				_linePos++;
		}

		/// <summary> Skips any whitespace. </summary>
		public void SkipWhiteSpace()
		{
			while (!_nextEOF && Char.IsWhiteSpace(_next))
				Advance();
		}

		/// <summary> Skips all comments and whitepace. </summary>
		public void SkipComments()
		{
			SkipWhiteSpace();
			while (!PeekEOF) // There is only the possibility of a comment if there are at least two characters
			{
				if (SkipLineComments())
					continue;

				// Skip block comments
				if ((_next == '/') && !PeekEOF && (Peek == '*'))
				{
					Advance();
					Advance();
					int blockCommentDepth = 1;
					while (blockCommentDepth > 0)
					{
						if (SkipLineComments())		// Line comments may be \\* block delimiters, so ignore them inside block comments
							continue;
						if (PeekEOF)
							throw new LexerException(LexerException.Codes.UnterminatedComment);
						Char peek = Peek;
						if ((_next == '/') && (peek == '*'))
						{
							blockCommentDepth++;
							Advance();
						}
						else if ((_next == '*') && (peek == '/'))
						{
							blockCommentDepth--;
							Advance();
						}
						Advance();
					}
					SkipWhiteSpace();
					continue;
				}
				break;
			}
		}

		/// <remarks> Used internally by SkipComments(). </remarks>
		/// <returns> True if comment parsing should continue. </return>
		private bool SkipLineComments()
		{
			Char peek = Peek;
			if ((_next == '/') && (peek == '/'))
			{
				Advance();
				Advance();
				while (!_nextEOF && (_next != '\n'))
					Advance();
				if (!_nextEOF)
				{
					Advance();
					SkipWhiteSpace();
					return true;
				}
			}
			return false;
		}

		private bool IsSymbol(char charValue)
		{
			switch (charValue)
			{
				case '.':
				case ',':
				case ';':
				case '?':
				case ':':
				case '(':
				case ')':
				case '{':
				case '}':
				case '[':
				case ']':
				case '*':
				case '/':
				case '~':
				case '&':
				case '|':
				case '^':
				case '+':
				case '-':
				case '>':
				case '<':
				case '=':
					return true;
				default:
					return false;
			}
		}

		public TokenType NextToken()
		{
			SkipComments();

			// Clear the TokenType
			_builder.Length = 0;
			_token = String.Empty;
			_tokenLine = _line;
			_tokenLinePos = _linePos;

			if (!_nextEOF)
			{
				if (Char.IsLetter(_next) || (_next == '_'))			// Identifiers
				{
					Advance();
					_builder.Append(_current);
					while (!_nextEOF && (Char.IsLetterOrDigit(_next) || (_next == '_')))
					{
						Advance();
						_builder.Append(_current);
					}
					_token = _builder.ToString();
					if (String.Equals(_token, True, StringComparison.OrdinalIgnoreCase) || String.Equals(_token, False, StringComparison.OrdinalIgnoreCase))
						_tokenType = TokenType.Boolean;
					else if (String.Equals(_token, Nil, StringComparison.OrdinalIgnoreCase))
						_tokenType = TokenType.Nil;
					else
						_tokenType = TokenType.Symbol;
				}
				else if (IsSymbol(_next))							// Symbols
				{
					_tokenType = TokenType.Symbol;
					Advance();
					_builder.Append(_current);
					switch (_current)
					{
						case ':':
							if (!_nextEOF && (_next == '='))
							{
								Advance();
								_builder.Append(_current);
							}
							break;

						case '?':
							if (Next == '=')
							{
								Advance();
								_builder.Append(_current);
							}
							else
								throw new LexerException(LexerException.Codes.IllegalInputCharacter, _next);
							break;

						case '*':
							if (!_nextEOF && (_next == '*'))
							{
								Advance();
								_builder.Append(_current);
							}
							break;
							
						case '>':
							if (!_nextEOF)
								switch (_next)
								{
									case '=':
									case '>':
										Advance();
										_builder.Append(_current);
									break;
								}
							break;

						case '<':
							if (!_nextEOF)
								switch (_next)
								{
									case '=':
									case '>':
									case '<':
										Advance();
										_builder.Append(_current);
									break;
								}
							break;
					}
					_token = _builder.ToString();
				}
				else if	(Char.IsDigit(_next) || (_next == '$'))		// Numbers
				{
					Advance();

					bool digitSatisfied = false;	// at least one digit required

					if (_current == '$')
						_tokenType = TokenType.Money;
					else
					{
						if ((_current == '0') && (!_nextEOF && ((_next == 'x') || (_next == 'X'))))
						{
							_tokenType = TokenType.Hex;
							Advance();
						}
						else
						{
							digitSatisfied = true;
							_builder.Append(_current);
							_tokenType = TokenType.Integer;
						}
					}

					bool done = false;
					bool hitPeriod = false;
					bool hitScalar = false;

					while (!done && !_nextEOF)
					{
						switch (_next)
						{
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
								Advance();
								digitSatisfied = true;
								_builder.Append(_current);
								break;

							case 'A':
							case 'a':
							case 'B':
							case 'b':
							case 'C':
							case 'c':
								if (_tokenType != TokenType.Hex)
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								Advance();
								digitSatisfied = true;
								_builder.Append(_current);
								break;
								
							case 'D':
							case 'd':
								Advance();
								if (_tokenType == TokenType.Hex)
								{
									digitSatisfied = true;
									_builder.Append(_current);
								}
								else if (_tokenType != TokenType.Money)
								{
									_tokenType = TokenType.Decimal;
									done = true;
								}
								else
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								break;

							case 'E':
							case 'e':
								Advance();
								if (_tokenType == TokenType.Hex)
								{
									digitSatisfied = true;
									_builder.Append(_current);
								}
								else if (!hitScalar && digitSatisfied)
								{
									hitScalar = true;
									digitSatisfied = false;
									_tokenType = TokenType.Float;
									_builder.Append(_current);
									if ((_next == '-') || (_next == '+'))
									{
										Advance();
										_builder.Append(_current);
									}
								}
								else
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								break;

							case 'f':
							case 'F':
								Advance();
								if (_tokenType == TokenType.Hex)
								{
									digitSatisfied = true;
									_builder.Append(_current);
								}
								else if (_tokenType != TokenType.Money)
								{
									_tokenType = TokenType.Float;
									done = true;
								}
								else
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								break;

							case '.':
								if (!hitPeriod)
								{
									Advance();
									if (_tokenType == TokenType.Hex)
										throw new LexerException(LexerException.Codes.InvalidNumericValue);
									if (_tokenType == TokenType.Integer)
										_tokenType = TokenType.Decimal;
									hitPeriod = true;
									digitSatisfied = false;
									_builder.Append(_current);
								}
								else
									done = true;	// a dot, not a point
								break;

							default:
								done = true;
								break;
						}
					}
					if (!digitSatisfied)
						throw new LexerException(LexerException.Codes.InvalidNumericValue);
					_token = _builder.ToString();
				}	
				else if ((_next == '\'') || (_next == '"'))
				{
					_tokenType = TokenType.String;
					Advance();
					char quoteChar = _current;
					while (true)
					{
						if (_nextEOF)
							throw new LexerException(LexerException.Codes.UnterminatedString);	// Better error than EOF
						Advance();
						if (_current == quoteChar)
							if (!_nextEOF && (_next == quoteChar))
								Advance();
							else
								break;
						_builder.Append(_current);
					}
					_token = _builder.ToString();
				}
				else
					throw new LexerException(LexerException.Codes.IllegalInputCharacter, _next); 
			}
			else
				_tokenType = TokenType.EOF;

			return _tokenType;
		}
    }
}

