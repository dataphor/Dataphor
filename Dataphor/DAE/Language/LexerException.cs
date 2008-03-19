/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Language
{
	/// <summary>Indicates an exception encoutered during lexical analysis.</summary>
	/// <remarks>
	/// The LexerException is thrown whenever the lexical analyzer encouters an error state.
	/// Only the lexer should throw exceptions of this type.
	/// </remarks>
	[Serializable]
	public class LexerException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 102100: "Unterminated string constant."</summary>
			UnterminatedString = 102100,

			/// <summary>Error code 102101: "Invalid numeric value."</summary>
			InvalidNumericValue = 102101,

			/// <summary>Error code 102102: "Illegal character "{0}" in input string."</summary>
			IllegalInputCharacter = 102102,

			/// <summary>Error code 102103: "{0} expected."</summary>
			TokenExpected = 102103,

			/// <summary>Error code 102104: ""{0}" expected."</summary>
			SymbolExpected = 102104,

			/// <summary>Error code 102105: "No active token."</summary>
			NoActiveToken = 102105,
		
			/// <summary>Error code 102106: "Unexpected end-of-file."</summary>
			UnexpectedEOF = 102106,
			
			/// <summary>Error code 102107: "Unterminated comment."</summary>
			UnterminatedComment = 102107
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Language.LexerException", typeof(LexerException).Assembly);

		// Constructors
		public LexerException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public LexerException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public LexerException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public LexerException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public LexerException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public LexerException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public LexerException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public LexerException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public LexerException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}