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

namespace Alphora.Dataphor.DAE.Device.Simple
{
	[Serializable]
	public class SimpleDeviceException : DeviceException
	{
		// Error Code Base: 203
		public new enum Codes : int
		{
			/// <summary>Error code 203100: "Object "{0}" is not a simple device."</summary>
			SimpleDeviceExpected = 203100,
			
			/// <summary>Error code 203101: "Object "{0}" is not a table variable."</summary>
			TableVarExpected = 203101,
			
			/// <summary>Error code 203102: "Header not found for table variable "{0}"."</summary>
			SimpleDeviceHeaderNotFound = 203102
		}

		// Resource manager for this exception class.
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException", typeof(SimpleDeviceException).Assembly);

		// Constructors
		public SimpleDeviceException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public SimpleDeviceException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public SimpleDeviceException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public SimpleDeviceException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public SimpleDeviceException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public SimpleDeviceException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public SimpleDeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public SimpleDeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	}
}