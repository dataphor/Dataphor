/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

namespace Alphora.Dataphor.DAE.Store
{
	[Serializable]
	public class StoreException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 130100: "Maximum number of connections to the catalog store exceeded."</summary>
			MaximumConnectionsExceeded = 130100,

			/// <summary>Error code 130101: "Store cursor has no current row."</summary>
			CursorHasNoCurrentRow = 130101,
			
			/// <summary>Error code 130102: "Store settings cannot be changed once the store has been initialized."</summary>
			StoreInitialized = 130102,
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Store.StoreException", typeof(StoreException).Assembly);

		// Constructors
		public StoreException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public StoreException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public StoreException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public StoreException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public StoreException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public StoreException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public StoreException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public StoreException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public StoreException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}