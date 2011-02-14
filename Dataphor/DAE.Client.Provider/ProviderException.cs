/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	[Serializable]
	public class ProviderException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 123100: "Cannot perform this operation on a connected Connection."</summary>
			ConnectionConnected = 123100,

			/// <summary>Error code 123101: "Cannot read from a cursor which is either EOF or BOF."</summary>
			CursorEOForBOF = 123101,
			
			/// <summary>Error code 123102: "Unimplemented Operation."</summary>
			Unimplemented = 123102,
			
			/// <summary>Error code 123103: "DAECommand: Unsupported command type ({0}). Use CommandType Text instead."</summary>
			UnsupportedCommandType = 123103,
			
			/// <summary>Error code 123104: "DAECommand: Cannot perform this operation on a prepared command."</summary>
			CommandPrepared = 123104,
			
			/// <summary>Error code 123107: "DAECommand: Connection is required and is not specified."</summary>
			ConnectionRequired = 123107,
			
			/// <summary>Error code 123108: "DAEParameterCollection: List only accepts DAEParameter objects."</summary>
			DAECommandParameterList = 123108,

			/// <summary>Error code 123109: "DAEConnection: Active connection is required to start a transaction."</summary>
			BeginTransactionFailed = 123109,

			/// <summary> DAETransaction: {0} failed DAEConnection was lost. </summary>
			ConnectionLost = 123110,

			/// <summary>Error code 123111: "DAEConnection: No alias specified."</summary>
			NoAliasSpecified = 123111,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Client.Provider.ProviderException", typeof(ProviderException).Assembly);

		// Constructors
		public ProviderException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ProviderException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ProviderException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ProviderException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ProviderException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ProviderException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ProviderException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ProviderException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public ProviderException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}
	}
}
