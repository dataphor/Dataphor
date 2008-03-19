/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Language
{
	/// <summary>Indicates a syntactic error encountered during the lexical and syntactic analysis phases of compilation.</summary>
	/// <remarks>
	/// The SyntaxException is used to return the line number and position of the lexical analyzer when an error is encountered
	/// during lexical analysis or parsing.  Any exception encountered during these phases will be wrapped with an exception
	/// of this type.  Only the parser should throw exceptions of this type.
	/// </remarks>
	[Serializable]
	public class SyntaxException : DAEException, ILocatedException
	{
		public enum Codes : int
		{
			/// <summary>Error code 103100: "Syntax error near "{0}"."</summary>
			SyntaxError = 103100
		}
			
		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Language.SyntaxException", typeof(SyntaxException).Assembly);

		public SyntaxException(Lexer ALexer, Exception AInner) : base(FResourceManager, (int)Codes.SyntaxError, ErrorSeverity.Application, AInner, ALexer[0].Token)
		{
			FLine = ALexer[0, false].Line;
			FLinePos = ALexer[0, false].LinePos;
			FTokenType = ALexer[0, false].Type;
			FToken = ALexer[0, false].Token;
		}
			
		public SyntaxException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext)
		{
			FLine = AInfo.GetInt32("Line");
			FLinePos = AInfo.GetInt32("LinePos");
			FTokenType = (TokenType)AInfo.GetValue("Type", typeof(TokenType));
			FToken = AInfo.GetString("Token");
		}
		
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext)
		{
			base.GetObjectData(AInfo, AContext);
			AInfo.AddValue("Line", FLine);
			AInfo.AddValue("LinePos", FLinePos);
			AInfo.AddValue("Type", FTokenType);
			AInfo.AddValue("Token", FToken);
		}

		private int FLine;
		public int Line 
		{ 
			get { return FLine; } 
			set { FLine = value; }
		}
			
		private int FLinePos;
		public int LinePos 
		{ 
			get { return FLinePos; } 
			set { FLinePos = value; }
		}
			
		private TokenType FTokenType;
		public TokenType TokenType { get { return FTokenType; } }
			
		private string FToken;
		public string Token { get { return FToken; } }
	}
}