/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Device
{
	// The base exception class for all exceptions thrown by the Device classes.
	public class DeviceException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 114100: "Unsupported translation "{0}"."</summary>
			UnsupportedTranslation = 114100,

			/// <summary>Error code 114102: "There is already a transaction in progress for this device."</summary>
			TransactionInProgress = 114102,

			/// <summary>Error code 114103: "There is no transaction in progress for this device."</summary>
			NoTransactionInProgress = 114103,

			/// <summary>Error code 114105: "Memory device only supports expressions."</summary>
			ExpressionDevice = 114105,

			/// <summary>Error code 114106: "Row could not be located."</summary>
			RowCouldNotBeLocated = 114106,

			/// <summary>Error code 114110: "Device "{0}" cannot perform requested operation "{1}"."</summary>
			InvalidExecuteRequest = 114110,
			
			/// <summary>Error code 114111: "Connection failure with device ""{0}"". All active transactions have been rolled back."</summary>
			ConnectionFailure = 114111,
			
			/// <summary>Error code 114112: "Transaction failure on device ""{0}"". All active transactions have been rolled back."</summary>
			TransactionFailure = 114112,
			
			/// <summary>Error code 114113: "Maximum physical row count ({0}) exceeded for table "{1}" in device "{2}"; to enable unlimited rows (with possible performance implications) use: alter device {2} alter class {{ alter "MaxRowCount" = "-1" }};"</summary>
			MaxRowCountExceeded = 114113,

            /// <summary>Error code 114114: "Internal error: Device plan is not prepared for this plan."</summary>
            UnpreparedDevicePlan = 114114
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.DeviceException", typeof(DeviceException).Assembly);

		// Constructors
		public DeviceException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public DeviceException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public DeviceException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public DeviceException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public DeviceException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public DeviceException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public DeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public DeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		protected DeviceException(ResourceManager resourceManager, int errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(resourceManager, errorCode, severity, innerException, paramsValue) {}
		
		public DeviceException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}