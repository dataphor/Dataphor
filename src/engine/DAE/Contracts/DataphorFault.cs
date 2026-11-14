/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.Serialization;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Compiling;

namespace Alphora.Dataphor.DAE.Contracts
{
	[DataContract]
	public class DataphorFault
	{
		[DataMember]
		public string ExceptionClassName;
		
		[DataMember]
		public int Code;
		
		[DataMember]
		public ErrorSeverity Severity;
		
		[DataMember]
		public string Message;
		
		[DataMember]
		public string Details;
		
		[DataMember]
		public string ServerContext;
		
		[DataMember]
		public DataphorFault InnerFault;
		
		// ILocatedException
		[DataMember]
		public int Line;
		
		[DataMember]
		public int LinePos;
		
		// SyntaxException
		[DataMember]
		public TokenType TokenType;
		
		[DataMember]
		public string Token;
		 
		// CompilerException
		[DataMember]
		public CompilerErrorLevel ErrorLevel;
		
		// RuntimeException
		[DataMember]
		public string Locator;
		
		[DataMember]
		public string Context;
		
		// ConnectionException
		[DataMember]
		public string Statement;
	}
}
