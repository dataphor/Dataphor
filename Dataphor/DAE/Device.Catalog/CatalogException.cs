/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE.Device;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	[Serializable]
	public class CatalogException : DeviceException
	{
		public new enum Codes : int
		{
			/// <summary>Error code 115100: "System catalog does not support ad hoc updates."</summary>
			ReadOnlyDevice = 115100,

			/// <summary>Error code 115101: "Catalog header not found for table variable "{0}"."</summary>
			CatalogHeaderNotFound = 115101,
			
			/// <summary>Error code 115102: "System catalog does not support updates to table "{0}"."</summary>
			UnsupportedUpdate = 115102,
			
			/// <summary>Error code 115103: "Catalog store is shared and cannot be updated."</summary>
			SharedCatalogStore = 115103,
			
			/// <summary>Error code 115104: "Catalog store cursor has no current row."</summary>
			CursorHasNoCurrentRow = 115104,
			
			/// <summary>Error code 115105: "Store settings cannot be changed once the catalog store has been initialized."</summary>
			CatalogStoreInitialized = 115105,
			
			/// <summary>Error code 115106: "Catalog store class name required."</summary>
			CatalogStoreClassNameRequired = 115106,
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.Catalog.CatalogException", typeof(CatalogException).Assembly);

		// Constructors
		public CatalogException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public CatalogException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public CatalogException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public CatalogException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public CatalogException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public CatalogException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public CatalogException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public CatalogException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public CatalogException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}