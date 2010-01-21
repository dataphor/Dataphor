/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Device;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;

#if !SILVERLIGHT
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Connection;
#endif

namespace Alphora.Dataphor.DAE.Contracts
{
	public static class DataphorFaultUtility
	{
		public static DataphorFault ExceptionToFault(DataphorException AException)
		{
			DataphorFault LFault = new DataphorFault();
			
			LFault.ExceptionClassName = AException.GetType().Name;
			LFault.Code = AException.Code;
			LFault.Severity = AException.Severity;
			LFault.Message = AException.Message;
			LFault.Details = AException.Details;
			LFault.ServerContext = AException.ServerContext;
			if (AException.InnerException != null)
				LFault.InnerFault = ExceptionToFault((DataphorException)AException.InnerException);
				
			#if !SILVERLIGHT
			// Under Silverlight, a ConnectionException will come back as a DataphorException
			// The statement is still present in the Details.
			ConnectionException LConnectionException = AException as ConnectionException;
			if (LConnectionException != null)
			{
				LFault.Statement = LConnectionException.Statement;
			}
			#endif
			
			SyntaxException LSyntaxException = AException as SyntaxException;
			if (LSyntaxException != null)
			{
				LFault.Locator = LSyntaxException.Locator;
				LFault.Line = LSyntaxException.Line;
				LFault.LinePos = LSyntaxException.LinePos;
				LFault.Token = LSyntaxException.Token;
				LFault.TokenType = LSyntaxException.TokenType;
			}
			
			CompilerException LCompilerException = AException as CompilerException;
			if (LCompilerException != null)
			{
				LFault.Locator = LCompilerException.Locator;
				LFault.Line = LCompilerException.Line;
				LFault.LinePos = LCompilerException.LinePos;
				LFault.ErrorLevel = LCompilerException.ErrorLevel;
			}
			
			RuntimeException LRuntimeException = AException as RuntimeException;
			if (LRuntimeException != null)
			{
				LFault.Locator = LRuntimeException.Locator;
				LFault.Line = LRuntimeException.Line;
				LFault.LinePos = LRuntimeException.LinePos;
				LFault.Context = LRuntimeException.Context;
			}
			
			return LFault;
		}
		
		public static List<DataphorFault> ExceptionsToFaults(IEnumerable<Exception> AExceptions)
		{
			var LResult = new List<DataphorFault>();

			foreach (Exception LException in AExceptions)
			{
				DataphorException LDataphorException = LException as DataphorException;
				if (LDataphorException == null)
					LDataphorException = new DataphorException(LException);
				LResult.Add(ExceptionToFault(LDataphorException));
			}
			
			return LResult;
		}
		
		public static DataphorException FaultToException(DataphorFault AFault)
		{
			switch (AFault.ExceptionClassName)
			{
				case "BaseException" : return new BaseException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "BOPException" : return new BOPException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "CompilerException" : return new CompilerException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.ErrorLevel, AFault.Line, AFault.LinePos, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault)) { Locator = AFault.Locator };
				case "RuntimeException" : return new RuntimeException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.Locator, AFault.Line, AFault.LinePos, AFault.Context, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				#if !SILVERLIGHT
				case "ConnectionException" : return new ConnectionException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.Statement, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "StoreException" : return new StoreException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				#endif
				case "DeviceException" : 
				case "ApplicationTransactionException" :
				case "CatalogException" :
				case "MemoryDeviceException" :
				case "SimpleDeviceException" :
				case "SQLException" :
				case "FrontendDeviceException" : return new DeviceException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "LanguageException" : return new LanguageException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "LexerException" : return new LexerException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "ParserException" : return new ParserException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "SyntaxException" : return new SyntaxException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.Line, AFault.LinePos, AFault.TokenType, AFault.Token, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault)) { Locator = AFault.Locator };
				case "IndexException" : return new IndexException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "ScanException" : return new ScanException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "SchemaException" : return new SchemaException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "ServerException" : return new ServerException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "ConveyorException" : return new ConveyorException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				case "StreamsException" : return new StreamsException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
				default : return new DataphorException(AFault.Severity, AFault.Code, AFault.Message, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
			}
		}
		
		public static List<Exception> FaultsToExceptions(IEnumerable<DataphorFault> LFaults)
		{
			var LResult = new List<Exception>();

			foreach (DataphorFault LFault in LFaults)
				LResult.Add(FaultToException(LFault));

			return LResult;
		}
	}
}
