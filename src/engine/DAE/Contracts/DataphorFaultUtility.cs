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
		public static DataphorFault ExceptionToFault(Exception exception)
		{
			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException == null)
				dataphorException = new DataphorException(exception);

			return ExceptionToFault(dataphorException);
		}

		public static DataphorFault ExceptionToFault(DataphorException exception)
		{
			DataphorFault fault = new DataphorFault();
			
			fault.ExceptionClassName = exception.GetType().Name;
			fault.Code = exception.Code;
			fault.Severity = exception.Severity;
			fault.Message = exception.Message;
			fault.Details = exception.Details;
			fault.ServerContext = exception.ServerContext;
			if (exception.InnerException != null)
				fault.InnerFault = ExceptionToFault(exception.InnerException);
				
			#if !SILVERLIGHT
			// Under Silverlight, a ConnectionException will come back as a DataphorException
			// The statement is still present in the Details.
			ConnectionException connectionException = exception as ConnectionException;
			if (connectionException != null)
			{
				fault.Statement = connectionException.Statement;
			}
			#endif
			
			SyntaxException syntaxException = exception as SyntaxException;
			if (syntaxException != null)
			{
				fault.Locator = syntaxException.Locator;
				fault.Line = syntaxException.Line;
				fault.LinePos = syntaxException.LinePos;
				fault.Token = syntaxException.Token;
				fault.TokenType = syntaxException.TokenType;
			}
			
			CompilerException compilerException = exception as CompilerException;
			if (compilerException != null)
			{
				fault.Locator = compilerException.Locator;
				fault.Line = compilerException.Line;
				fault.LinePos = compilerException.LinePos;
				fault.ErrorLevel = compilerException.ErrorLevel;
			}
			
			RuntimeException runtimeException = exception as RuntimeException;
			if (runtimeException != null)
			{
				fault.Locator = runtimeException.Locator;
				fault.Line = runtimeException.Line;
				fault.LinePos = runtimeException.LinePos;
				fault.Context = runtimeException.Context;
			}
			
			return fault;
		}
		
		public static List<DataphorFault> ExceptionsToFaults(IEnumerable<Exception> exceptions)
		{
			var result = new List<DataphorFault>();

			foreach (Exception exception in exceptions)
			{
				DataphorException dataphorException = exception as DataphorException;
				if (dataphorException == null)
					dataphorException = new DataphorException(exception);
				result.Add(ExceptionToFault(dataphorException));
			}
			
			return result;
		}
		
		public static DataphorException FaultToException(DataphorFault fault)
		{
			switch (fault.ExceptionClassName)
			{
				case "BaseException" : return new BaseException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "BOPException" : return new BOPException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "CompilerException" : return new CompilerException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.ErrorLevel, fault.Line, fault.LinePos, fault.InnerFault == null ? null : FaultToException(fault.InnerFault)) { Locator = fault.Locator };
				case "RuntimeException" : return new RuntimeException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.Locator, fault.Line, fault.LinePos, fault.Context, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				#if !SILVERLIGHT
				case "ConnectionException" : return new ConnectionException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.Statement, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "StoreException" : return new StoreException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				#endif
				case "DeviceException" : 
				case "ApplicationTransactionException" :
				case "CatalogException" :
				case "MemoryDeviceException" :
				case "SimpleDeviceException" :
				case "SQLException" :
				case "FrontendDeviceException" : return new DeviceException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "LanguageException" : return new LanguageException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "LexerException" : return new LexerException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "ParserException" : return new ParserException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "SyntaxException" : return new SyntaxException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.Line, fault.LinePos, fault.TokenType, fault.Token, fault.InnerFault == null ? null : FaultToException(fault.InnerFault)) { Locator = fault.Locator };
				case "IndexException" : return new IndexException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "ScanException" : return new ScanException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "SchemaException" : return new SchemaException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "ServerException" : return new ServerException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "ConveyorException" : return new ConveyorException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				case "StreamsException" : return new StreamsException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
				default : return new DataphorException(fault.Severity, fault.Code, fault.Message, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
			}
		}
		
		public static List<Exception> FaultsToExceptions(IEnumerable<DataphorFault> LFaults)
		{
			var result = new List<Exception>();

			foreach (DataphorFault fault in LFaults)
				result.Add(FaultToException(fault));

			return result;
		}
	}
}
