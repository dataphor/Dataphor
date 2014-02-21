/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;
using System.Runtime.Serialization;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Language
{
	/// <summary>Indicates a syntactic error encountered during the lexical and syntactic analysis phases of compilation.</summary>
	/// <remarks>
	/// The SyntaxException is used to return the line number and position of the lexical analyzer when an error is encountered
	/// during lexical analysis or parsing.  Any exception encountered during these phases will be wrapped with an exception
	/// of this type.  Only the parser should throw exceptions of this type.
	/// </remarks>
	public class SyntaxException : DAEException, ILocatorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 103100: "Syntax error near "{0}"."</summary>
			SyntaxError = 103100
		}
			
		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Language.SyntaxException", typeof(SyntaxException).Assembly);

		public SyntaxException(Lexer lexer, Exception inner) : base(_resourceManager, (int)Codes.SyntaxError, ErrorSeverity.Application, inner, lexer[1, false].Token)
		{
			_line = lexer[1, false].Line;
			_linePos = lexer[1, false].LinePos;
			_tokenType = lexer[1, false].Type;
			_token = lexer[1, false].Token;
		}
		
		public SyntaxException(ErrorSeverity severity, int code, string message, string details, string serverContext, int line, int linePos, TokenType tokenType, string token, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
			_line = line;
			_linePos = linePos;
			_tokenType = tokenType;
			_token = token;
		}
			
		private int _line;
		public int Line 
		{ 
			get { return _line; } 
			set { _line = value; }
		}
			
		private int _linePos;
		public int LinePos 
		{ 
			get { return _linePos; } 
			set { _linePos = value; }
		}
			
		private TokenType _tokenType;
		public TokenType TokenType { get { return _tokenType; } }
			
		private string _token;
		public string Token { get { return _token; } }

		private string _locator;
		public string Locator 
		{ 
			get { return _locator; }
			set { _locator = value; }
		}
	}
}