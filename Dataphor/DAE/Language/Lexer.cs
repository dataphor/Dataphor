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
		public void CheckType(TokenType AToken)
		{
			if (Type == TokenType.Error)
				throw Error;
			if (Type != AToken)
				throw new LexerException(LexerException.Codes.TokenExpected, Enum.GetName(typeof(TokenType), AToken));
		}

		/// <summary> Ensures that the current TokenType is a symbol equal to the given symbol.  </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if it is not. </remarks>
		public void CheckSymbol(string ASymbol)
		{
			if ((Type != TokenType.Symbol) || !String.Equals(Token, ASymbol, StringComparison.Ordinal))
				throw new LexerException(LexerException.Codes.SymbolExpected, ASymbol);
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
		private const int CLookAheadCount = 3;

		/// <remarks> It is an error to access the current TokenType until <see cref="NextToken"/> has been called. </remarks>
        public Lexer(string AInput)
        {
			FTokenizer = new Tokenizer(AInput);
			for (int i = 0; i < CLookAheadCount; i++)
				FTokens[i] = new LexerToken();
			for (int i = 0; i < (CLookAheadCount - 1); i++)
				if (!ReadNext(i))
					break;
        }

		private Tokenizer FTokenizer;

		/// <summary> Buffer of tokens (clock allocation). </summary>
		private LexerToken[] FTokens = new LexerToken[CLookAheadCount];
		/// <summary> Current position within the token buffer of the current token. </summary>
		/// <remarks> Initialized in the constructor to last position so that the lexer starts on a "crack". </remarks>
		private int FCurrentIndex = CLookAheadCount - 1;

		/// <summary> Reads the next token into the specified location within the buffer. </summary>
		/// <returns> True if the read token type is not EOF. </returns>
		private bool ReadNext(int AIndex)
		{
			LexerToken LToken = FTokens[AIndex];
			try
			{
				LToken.Type = FTokenizer.NextToken();
				LToken.Token = FTokenizer.Token;
			}
			catch (Exception LException)
			{
				LToken.Type = TokenType.Error;
				LToken.Error = LException;
			}
			LToken.Line = FTokenizer.Line;
			LToken.LinePos = FTokenizer.LinePos;
			return (LToken.Type != TokenType.EOF) && (LToken.Type != TokenType.Error);
		}

		/// <summary> The token a specific number of tokens ahead of the current token. </summary>
		public LexerToken this[int AIndex, bool ACheckActive]
		{
			get
			{
				System.Diagnostics.Debug.Assert(AIndex < CLookAheadCount, "Lexer look ahead attempt exceeds maximum");
				LexerToken LToken = FTokens[(FCurrentIndex + AIndex) % CLookAheadCount];
				if (ACheckActive && (LToken.Type == TokenType.Unknown))
					throw new LexerException(LexerException.Codes.NoActiveToken);
				return LToken;
			}
		}

		public LexerToken this[int AIndex]
		{
			get
			{
				return this[AIndex, true];
			}
		}

		/// <summary>Advances the current token.</summary>
		/// <returns>Returns the now active token.</returns>
		public LexerToken NextToken()
		{
			ReadNext(FCurrentIndex);
			FCurrentIndex = (FCurrentIndex + 1) % CLookAheadCount;
			LexerToken LToken = FTokens[FCurrentIndex];
			if (LToken.Type == TokenType.EOF)
				throw new LexerException(LexerException.Codes.UnexpectedEOF);
			if (LToken.Type == TokenType.Error)
				throw LToken.Error;
			return LToken;
		}

		/// <summary> Gets the symbol the specified number of tokens ahead without advancing the current token. </summary>
		/// <remarks> If the token is not a symbol, returns an empty string. </remarks>
		public LexerToken PeekToken(int ACount)
		{
			LexerToken LToken = this[ACount];
			if (LToken.Type == TokenType.Error)
				throw LToken.Error;
			return LToken;
		}

		/// <summary> Gets the symbol the specified number of tokens ahead without advancing the current token. </summary>
		/// <remarks> If the token is not a symbol, returns an empty string. </remarks>
		public string PeekTokenSymbol(int ACount)
		{
			LexerToken LToken = this[ACount];
			if (LToken.Type == TokenType.Symbol)
				return LToken.Token;
			else if (LToken.Type == TokenType.Error)
				throw LToken.Error;
			else
				return String.Empty;
		}
    }

    public class Tokenizer
    {
		// Boolean constants
		public const string CTrue = "true";
		public const string CFalse = "false";
		public const string CNil = "nil";

		public Tokenizer(string AInput)
			: base()
		{
			FInput = AInput;
			ReadNext();
		}

		private string FInput;
		private int FPos = -1;

		private Char FCurrent;
		public Char Current { get { return FCurrent; } }

		private Char FNext;
		public Char Next
		{
			get
			{
				if (FNextEOF)
					throw new LexerException(LexerException.Codes.UnexpectedEOF);
				return FNext;
			}
		}

		private bool FNextEOF;
		public bool NextEOF { get { return FNextEOF; } }

		public Char Peek
		{
			get
			{
				if (PeekEOF)
					throw new LexerException(LexerException.Codes.UnexpectedEOF);
				return FInput[FPos + 1];
			}
		}

		public bool PeekEOF
		{
			get
			{
				return FPos + 1 >= FInput.Length;
			}
		}

		private int FLine = 1;
		private int FLinePos = 1;
		private int FTokenLine = 1;
		private int FTokenLinePos = 1;

		/// <summary> The line number (one-based) of the beginning of the last read token. </summary>
		public int Line { get { return FTokenLine; } }
		
		/// <summary> The offset position (one-based) of the beginning of the last read token. </summary>
		public int LinePos { get { return FTokenLinePos; } }

		private TokenType FTokenType;
		public TokenType TokenType { get { return FTokenType; } }

		private string FToken;
		public string Token
		{
			get { return FToken; }
		}

		private StringBuilder FBuilder = new StringBuilder();

		/// <summary> Pre-reads the next TokenType (does not affect current). </summary>
		private void ReadNext()
		{
			FPos++;
			FNextEOF = FPos >= FInput.Length;
			if (!FNextEOF) 
				FNext = FInput[FPos];
		}

		public void Advance()
		{
			FCurrent = Next;
			ReadNext();
			if (FCurrent == '\n')
			{
				FLine++;
				FLinePos = 1;
			}
			else
				FLinePos++;
		}

		/// <summary> Skips any whitespace. </summary>
		public void SkipWhiteSpace()
		{
			while (!FNextEOF && Char.IsWhiteSpace(FNext))
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
				if ((FNext == '/') && !PeekEOF && (Peek == '*'))
				{
					Advance();
					Advance();
					int LBlockCommentDepth = 1;
					while (LBlockCommentDepth > 0)
					{
						if (SkipLineComments())		// Line comments may be \\* block delimiters, so ignore them inside block comments
							continue;
						if (PeekEOF)
							throw new LexerException(LexerException.Codes.UnterminatedComment);
						Char LPeek = Peek;
						if ((FNext == '/') && (LPeek == '*'))
						{
							LBlockCommentDepth++;
							Advance();
						}
						else if ((FNext == '*') && (LPeek == '/'))
						{
							LBlockCommentDepth--;
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
			Char LPeek = Peek;
			if ((FNext == '/') && (LPeek == '/'))
			{
				Advance();
				Advance();
				while (!FNextEOF && (FNext != '\n'))
					Advance();
				if (!FNextEOF)
				{
					Advance();
					SkipWhiteSpace();
					return true;
				}
			}
			return false;
		}

		private bool IsSymbol(char AChar)
		{
			switch (AChar)
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
			FBuilder.Length = 0;
			FToken = String.Empty;
			FTokenLine = FLine;
			FTokenLinePos = FLinePos;

			if (!FNextEOF)
			{
				if (Char.IsLetter(FNext) || (FNext == '_'))			// Identifiers
				{
					Advance();
					FBuilder.Append(FCurrent);
					while (!FNextEOF && (Char.IsLetterOrDigit(FNext) || (FNext == '_')))
					{
						Advance();
						FBuilder.Append(FCurrent);
					}
					FToken = FBuilder.ToString();
					if (String.Equals(FToken, CTrue, StringComparison.OrdinalIgnoreCase) || String.Equals(FToken, CFalse, StringComparison.OrdinalIgnoreCase))
						FTokenType = TokenType.Boolean;
					else if (String.Equals(FToken, CNil, StringComparison.OrdinalIgnoreCase))
						FTokenType = TokenType.Nil;
					else
						FTokenType = TokenType.Symbol;
				}
				else if (IsSymbol(FNext))							// Symbols
				{
					FTokenType = TokenType.Symbol;
					Advance();
					FBuilder.Append(FCurrent);
					switch (FCurrent)
					{
						case ':':
							if (!FNextEOF && (FNext == '='))
							{
								Advance();
								FBuilder.Append(FCurrent);
							}
							break;

						case '?':
							if (Next == '=')
							{
								Advance();
								FBuilder.Append(FCurrent);
							}
							else
								throw new LexerException(LexerException.Codes.IllegalInputCharacter, FNext);
							break;

						case '*':
							if (!FNextEOF && (FNext == '*'))
							{
								Advance();
								FBuilder.Append(FCurrent);
							}
							break;
							
						case '>':
							if (!FNextEOF)
								switch (FNext)
								{
									case '=':
									case '>':
										Advance();
										FBuilder.Append(FCurrent);
									break;
								}
							break;

						case '<':
							if (!FNextEOF)
								switch (FNext)
								{
									case '=':
									case '>':
									case '<':
										Advance();
										FBuilder.Append(FCurrent);
									break;
								}
							break;
					}
					FToken = FBuilder.ToString();
				}
				else if	(Char.IsDigit(FNext) || (FNext == '$'))		// Numbers
				{
					Advance();

					bool LDigitSatisfied = false;	// at least one digit required

					if (FCurrent == '$')
						FTokenType = TokenType.Money;
					else
					{
						if ((FCurrent == '0') && (!FNextEOF && ((FNext == 'x') || (FNext == 'X'))))
						{
							FTokenType = TokenType.Hex;
							Advance();
						}
						else
						{
							LDigitSatisfied = true;
							FBuilder.Append(FCurrent);
							FTokenType = TokenType.Integer;
						}
					}

					bool LDone = false;
					bool LHitPeriod = false;
					bool LHitScalar = false;

					while (!LDone && !FNextEOF)
					{
						switch (FNext)
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
								LDigitSatisfied = true;
								FBuilder.Append(FCurrent);
								break;

							case 'A':
							case 'a':
							case 'B':
							case 'b':
							case 'C':
							case 'c':
								if (FTokenType != TokenType.Hex)
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								Advance();
								LDigitSatisfied = true;
								FBuilder.Append(FCurrent);
								break;
								
							case 'D':
							case 'd':
								Advance();
								if (FTokenType == TokenType.Hex)
								{
									LDigitSatisfied = true;
									FBuilder.Append(FCurrent);
								}
								else if (FTokenType != TokenType.Money)
								{
									FTokenType = TokenType.Decimal;
									LDone = true;
								}
								else
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								break;

							case 'E':
							case 'e':
								Advance();
								if (FTokenType == TokenType.Hex)
								{
									LDigitSatisfied = true;
									FBuilder.Append(FCurrent);
								}
								else if (!LHitScalar && LDigitSatisfied)
								{
									LHitScalar = true;
									LDigitSatisfied = false;
									FTokenType = TokenType.Float;
									FBuilder.Append(FCurrent);
									if ((FNext == '-') || (FNext == '+'))
									{
										Advance();
										FBuilder.Append(FCurrent);
									}
								}
								else
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								break;

							case 'f':
							case 'F':
								Advance();
								if (FTokenType == TokenType.Hex)
								{
									LDigitSatisfied = true;
									FBuilder.Append(FCurrent);
								}
								else if (FTokenType != TokenType.Money)
								{
									FTokenType = TokenType.Float;
									LDone = true;
								}
								else
									throw new LexerException(LexerException.Codes.InvalidNumericValue);
								break;

							case '.':
								if (!LHitPeriod)
								{
									Advance();
									if (FTokenType == TokenType.Hex)
										throw new LexerException(LexerException.Codes.InvalidNumericValue);
									if (FTokenType == TokenType.Integer)
										FTokenType = TokenType.Decimal;
									LHitPeriod = true;
									LDigitSatisfied = false;
									FBuilder.Append(FCurrent);
								}
								else
									LDone = true;	// a dot, not a point
								break;

							default:
								LDone = true;
								break;
						}
					}
					if (!LDigitSatisfied)
						throw new LexerException(LexerException.Codes.InvalidNumericValue);
					FToken = FBuilder.ToString();
				}	
				else if ((FNext == '\'') || (FNext == '"'))
				{
					FTokenType = TokenType.String;
					Advance();
					char LQuoteChar = FCurrent;
					while (true)
					{
						if (FNextEOF)
							throw new LexerException(LexerException.Codes.UnterminatedString);	// Better error than EOF
						Advance();
						if (FCurrent == LQuoteChar)
							if (!FNextEOF && (FNext == LQuoteChar))
								Advance();
							else
								break;
						FBuilder.Append(FCurrent);
					}
					FToken = FBuilder.ToString();
				}
				else
					throw new LexerException(LexerException.Codes.IllegalInputCharacter, FNext); 
			}
			else
				FTokenType = TokenType.EOF;

			return FTokenType;
		}
    }
}

