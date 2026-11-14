/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE.Device;

namespace Alphora.Dataphor.DAE.Device.Memory
{
	public class MemoryDeviceException : DeviceException
	{
		public new enum Codes : int
		{
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.Memory.MemoryDeviceException", typeof(MemoryDeviceException).Assembly);

		// Constructors
		public MemoryDeviceException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public MemoryDeviceException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public MemoryDeviceException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public MemoryDeviceException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public MemoryDeviceException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public MemoryDeviceException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public MemoryDeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public MemoryDeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	}
}