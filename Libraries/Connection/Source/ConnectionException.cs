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
	[Serializable]
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
		}
		
		// Resource manager for this exception class.
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Connection.ConnectionException", typeof(ConnectionException).Assembly);

		// Constructors
		public ConnectionException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public ConnectionException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public ConnectionException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public ConnectionException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}

		public ConnectionException(Codes AErrorCode, string AStatement) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) 
		{
			FStatement = AStatement;
		}

		public ConnectionException(Codes AErrorCode, string AStatement, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) 
		{
			FStatement = AStatement;
		}
		
		public ConnectionException(Codes AErrorCode, Exception AInnerException, string AStatement) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) 
		{
			FStatement = AStatement;
		}
		
		public ConnectionException(Codes AErrorCode, Exception AInnerException, string AStatement, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) 
		{
			FStatement = AStatement;
		}
		
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, string AStatement) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) 
		{
			FStatement = AStatement;
		}
		
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, string AStatement, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) 
		{
			FStatement = AStatement;
		}
		
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, string AStatement) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) 
		{
			FStatement = AStatement;
		}
		
		public ConnectionException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, string AStatement, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) 
		{
			FStatement = AStatement;
		}
		
		protected ConnectionException(ResourceManager AResourceManager, int AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(AResourceManager, AErrorCode, ASeverity, AInnerException, AParams) {}
		
		public ConnectionException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) 
		{
			FStatement = AInfo.GetString("Statement");
		}
		
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext)
		{
			base.GetObjectData(AInfo, AContext);
			AInfo.AddValue("Statement", FStatement);
		}

		private string FStatement;
		public string Statement { get { return FStatement; } }
		
		public override string GetDetails()
		{
			if (FStatement != String.Empty)
				return String.Format(@"Exceptions occurred while executing SQL command: ""{0}""", FStatement);
				
			return base.GetDetails();
		}
	}
}