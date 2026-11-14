/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Device;

namespace Alphora.Dataphor.DAE.Device.SQL
{
	[Serializable]
	public class SQLException : DeviceException
	{
		// Error Code Base: 126
		public new enum Codes : int
		{
			/// <summary>Error code 126100: "Source expression type cannot be determined."</summary>
			InvalidStatementClass = 126100,

			/// <summary>Error code 126101: "Unimplemented: MSSQLAggregate.Translate."</summary>
			UnimplementedAggregateTranslate = 126101,

			/// <summary>Error code 126102: "Unimplemented: MSSQLJoin.Translate for natural joins."</summary>
			UnimplementedTranslateForNaturalJoins = 126102,

			/// <summary>Error code 126103: "Data type "{0}" cannot be imported."</summary>
			UnsupportedImportType = 126103,
			
			/// <summary>Error code 126107: "Unable to open deferred read stream for column "{0}"."</summary>
			DeferredDataNotFound = 126107,
			
			/// <summary>Error code 126108: "Column map not found for index (0)."</summary>
			ColumnMapNotFound = 126108,
			
			/// <summary>Error code 126110: "Catalog reconciliation not supported for device "{0}"."</summary>
			CatalogReconciliationNotSupported = 126110,
			
			/// <summary>Error code 126111: "No current join context."</summary>
			NoCurrentJoinContext = 126111,
			
			/// <summary>Error code 126112: "Connection not found."</summary>
			ConnectionNotFound = 126112,
			
			/// <summary>Error code 126113: "{0} value ({1}) is out of range for this device.";</summary>
			ValueOutOfRange = 126113,

			/// <summary>Error code 126114: "No current Query context."</summary>
			NoCurrentQueryContext = 126114,
			
			/// <summary>Error code 126115: "SQL device expected."</summary>
			SQLDeviceExpected = 126115,

			/// <summary>Error code 126116: "An error occurred while trying to read device table list.  The DeviceTablesExpression attribute of the device may be incorrect."</summary>
			ErrorReadingDeviceTables = 126116,

			/// <summary>Error code 126117: "An error occurred while trying to read device index list.  The DeviceIndexesExpression attribute of the device may be incorrect."</summary>
			ErrorReadingDeviceIndexes = 126117,
			
			/// <summary>Error code 126118: "An error occurred while trying to read device foreign key list.  The DeviceForeignKeysExpression attribute of the device may be incorrect."</summary>
			ErrorReadingDeviceForeignKeys = 126118,
			
			/// <summary>Error code 126119: "Argument "{0}" must be evaluated at compile-time.  Use a literal expression as the argument, or use a dynamic execution operator to perform the operation."</summary>
			ArgumentMustBeLiteral = 126119,
			
			/// <summary>Error code 126120: "Device "{0}" is already executing the connection statement for this process."</summary>
			AlreadyExecutingConnectionStatement = 126120,
			
			/// <summary>Error code 126121: "Deferred access columns cannot be retrieved in this context."</summary>
			InvalidDeferredContext = 126121,
			
			/// <summary>Error code 126122: "Unable to retrieve result set description from target system."</summary>
			SchemaRetrievalFailed = 126122,
			
			/// <summary>Error code 126123: "The query is supported by device "{0}", which is not an SQL device."</summary>
			QuerySupportedByNonSQLDevice = 126123,
			
			/// <summary>Error code 126124: "The query is supported by device "{0}", not the given device "{1}"."</summary>
			QuerySupportedByDifferentDevice = 126124,
			
			/// <summary>Error code 126125: "The query is not supported by any device."</summary>
			QueryUnsupported = 126125
		}

		// Resource manager for this exception class.
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.SQL.SQLException", typeof(SQLException).Assembly);

		// Constructors
		public SQLException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public SQLException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public SQLException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public SQLException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public SQLException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public SQLException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public SQLException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public SQLException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		protected SQLException(ResourceManager resourceManager, int errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(resourceManager, errorCode, severity, innerException, paramsValue) {}
	}
}