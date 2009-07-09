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
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Client.Provider.ProviderException", typeof(ProviderException).Assembly);

		// Constructors
		public ProviderException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public ProviderException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public ProviderException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public ProviderException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public ProviderException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public ProviderException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public ProviderException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public ProviderException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public ProviderException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}
