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
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.Catalog.CatalogException", typeof(CatalogException).Assembly);

		// Constructors
		public CatalogException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public CatalogException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public CatalogException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public CatalogException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public CatalogException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public CatalogException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public CatalogException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public CatalogException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	}
}