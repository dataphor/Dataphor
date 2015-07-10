/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 121100: "The referenced column ({0}) does not exist in the data set."</summary>
			ColumnNotFound = 121100,

			/// <summary>Error code 121101: "DataSession must be inactive to perform this operation."</summary>
			DataSessionActive = 121101,

			/// <summary>Error code 121102: "Cannot perform operation on dataset in {0} state."</summary>
			IncorrectState = 121102,

			/// <summary>Error code 121103: "Bookmark not found."</summary>
			BookmarkNotFound = 121103,
			
			/// <summary>Error code 121104: "Record not found."</summary>
			RecordNotFound = 121104,
			
			/// <summary>Error code 121106: "Field ({0}) not found."</summary>
			FieldForColumnNotFound = 121106,
			
			/// <summary>Error code 121107: "Cannot perform operation on a read-only DataSet."</summary>
			IsReadOnly = 121107,
			
			/// <summary>Error code 121108: "A DataSession object is necessary to perform this operation."</summary>
			SessionMissing = 121108,
			
			/// <summary>Error code 121109: "Cannot perform this operation on an active DataSet."</summary>
			Active = 121109,
			
			/// <summary>Error code 121110: "Cannot perform this operation on a inactive DataSet."</summary>
			NotActive = 121110,
			
			/// <summary>Error code 121111: "DataSet cannot directly or indirectly link to itself."</summary>
			CircularLink = 121111,

			/// <summary>Error code 121112: "Cannot convert field to type ({0})."</summary>
			CannotConvertFromType = 121112,

			/// <summary>Error code 121113: "Cannot perform this operation on an empty dataset."</summary>
			EmptyDataSet = 121113,

			/// <summary>Error code 121114: "Current Row has no value for Column ({0})."</summary>
			NoValue = 121114,
			
			/// <summary>Error code 121115: "A process must be started on the session to perform this operation."</summary>
			ProcessMissing = 121115,

			/// <summary>Error code 121116: "Cannot perform this operation on an invalid (no master row) DataSet."</summary>
			DataSetInvalid = 121116,

			/// <summary>Error code 121117: "DataSession must be active to perform this operation."</summary>
			DataSessionInactive = 121117,

			/// <summary>Error code 121118: "Session Container." </summary>
			SessionContainer = 121118,
			
			/// <summary>Error code 121119: "Session Name {0} already exists." </summary>
			SessionExists = 121119,

			/// <summary>Error code 121120: "Session Name {0} not found." </summary>
			SessionNotFound = 121120,

			/// <summary>Error code 121121: "Invalid SQL Statement.  Only Select statements are allowed in data sets."</summary>
			InvalidSQLStatement = 121121,

			/// <summary>Error code 121122: "Open state must be Browse, Insert, or Edit."</summary>
			InvalidOpenState = 121122,
			
			/// <summary>Error code 121123: "Invalid expression result type: "{0}"."</summary>
			InvalidResultType = 121123,

			/// <summary>Error code 121124: "A value is required for column ({0})."</summary>
			ColumnRequired = 121124,

			/// <summary>Error code 121125: "Cannot convert object type ({0}) to a native type."</summary>
			InvalidParamType = 121125,
			
			/// <summary>Error code 121126: "Old value is only available during a change or validate event."</summary>
			OldValueNotAvailable = 121126,
			
			/// <summary>Error code 121127: "Original value is only available when a dataset is in edit state."</summary>
			OriginalValueNotAvailable = 121127,
			
			/// <summary>Error code 121128: "A valid server alias is required to establish a server connection."</summary>
			ServerAliasRequired = 121128,
			
			/// <summary>Error code 121129: "Alias '{0}' not found."</summary>
			AliasNotFound = 121129,
			
			/// <summary>Error code 121130: "Alias '{0}' has already been added to this connection factory."</summary>
			DuplicateAlias = 121130,
			
			/// <summary>Error code 121131: "No aliases configured for this connection factory."</summary>
			NoAliasesConfigured = 121131,
			
			/// <summary>Error code 121132: "An alias name is required to establish a connection."</summary>
			NoServerAliasSpecified = 121132,
			
			/// <summary>Error code 121133: "No alias configuration has been loaded for this alias manager. Use the Load() method to load a configuration before attempting to use the alias manager."</summary>
			AliasConfigurationNotLoaded = 121133,
			
			/// <summary>Error code 121134: "Dataset was opened write-only and cannot be used to edit or delete rows, because these operations require reading."</summary>
			IsWriteOnly = 121134,
			
			/// <summary>Error code 121135: "An error occurred attempting to connect to the listener on "{0}"." </summary>
			CouldNotDeterminePortNumber = 121135,

			/// <summary>Error code 121136: "An error occurred attempting to communicate with the server."</summary>
			CommunicationFailure = 121136
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Client.ClientException", typeof(ClientException).Assembly);

		// Constructors
		public ClientException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ClientException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ClientException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ClientException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ClientException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ClientException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ClientException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ClientException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	}
}
