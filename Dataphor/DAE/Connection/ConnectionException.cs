/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Connection
{
	/// <summary>The base exception class for all exceptions thrown by the Connection classes. </summary>
	public class ConnectionException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 127100: "There is already a transaction in progress for this connection."</summary>
			TransactionInProgress = 127100,

			/// <summary>Error code 127101: "There is no transaction in progress for this connection."</summary>
			NoTransactionInProgress = 127101,

			/// <summary>Error code 127102: "Deferred read overflow."</summary>
			DeferredOverflow = 127102,
			
			/// <summary>Error code 127103: "Source data type "{0}" does not support deferred reading."</summary>
			NonDeferredDataType = 127103,

			/// <summary>Error code 127104: "Unknown SQL data type class "{0}"."</summary>
			UnknownSQLDataType = 127104,

			/// <summary>Error code 127105: "Exceptions occurred while executing SQL command."</summary>
			SQLException = 127105,
			
			/// <summary>Error code 127106: "Unable to convert deferred stream value."</summary>
			UnableToConvertDeferredStreamValue = 127106,
			
			/// <summary>Error code 127107: "Connection is busy."</summary>
			ConnectionBusy = 127107,
			
			/// <summary>Error code 127108: "Connectivity implementation does not support output parameters."</summary>
			OutputParametersNotSupported = 127108,

			/// <summary>Error code 127109: "Unsupported updateable call."</summary>
			UnsupportedUpdateableCall = 127109,

			/// <summary>Error code 127110: "Unsupported searchable call."</summary>
			UnsupportedSearchableCall = 127110,
			
			/// <summary>Error code 127120: "String value exceeds length ({1}) of parameter "{0}"."</summary>
			StringParameterOverflow = 127120,
			
			/// <summary>Error code 127121: "Connection is closed."</summary>
			ConnectionClosed = 127121,

			/// <summary>Error code 127122: "Unable to open cursor."</summary>
			UnableToOpenCursor = 127122,
			
			/// <summary>Error code 127123: "Provider invariant name cannot be set for this connection because the provider factory has already been constructed."</summary>
			ProviderFactoryAlreadyConstructed = 127123,
			
			/// <summary>Error code 127124: "Provider invariant name must be specified for the generic connection."</summary>
			ProviderInvariantNameRequired = 127124,
		}
		
		// Resource manager for this exception class.
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Connection.ConnectionException", typeof(ConnectionException).Assembly);

		// Constructors
		public ConnectionException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ConnectionException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ConnectionException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ConnectionException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ConnectionException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ConnectionException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ConnectionException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ConnectionException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}

		public ConnectionException(Codes errorCode, string statement) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) 
		{
			_statement = statement;
		}

		public ConnectionException(Codes errorCode, string statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) 
		{
			_statement = statement;
		}
		
		public ConnectionException(Codes errorCode, Exception innerException, string statement) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) 
		{
			_statement = statement;
		}
		
		public ConnectionException(Codes errorCode, Exception innerException, string statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) 
		{
			_statement = statement;
		}
		
		public ConnectionException(Codes errorCode, ErrorSeverity severity, string statement) : base(_resourceManager, (int)errorCode, severity, null, null) 
		{
			_statement = statement;
		}
		
		public ConnectionException(Codes errorCode, ErrorSeverity severity, string statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) 
		{
			_statement = statement;
		}
		
		public ConnectionException(Codes errorCode, ErrorSeverity severity, Exception innerException, string statement) : base(_resourceManager, (int)errorCode, severity, innerException, null) 
		{
			_statement = statement;
		}
		
		public ConnectionException(Codes errorCode, ErrorSeverity severity, Exception innerException, string statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) 
		{
			_statement = statement;
		}
		
		protected ConnectionException(ResourceManager resourceManager, int errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(resourceManager, errorCode, severity, innerException, paramsValue) {}
		
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Statement", _statement);
		}

		private string _statement;
		public string Statement { get { return _statement; } }
		
		public override string GetDetails()
		{
			if (_statement != String.Empty)
				return String.Format(@"Exceptions occurred while executing SQL command: ""{0}""", _statement);
				
			return base.GetDetails();
		}
		
		public ConnectionException(ErrorSeverity severity, int code, string message, string details, string serverContext, string statement, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
			_statement = statement;
		}
	}
}