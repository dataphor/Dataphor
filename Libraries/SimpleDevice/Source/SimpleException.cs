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
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.Simple.SimpleDeviceException", typeof(SimpleDeviceException).Assembly);

		// Constructors
		public SimpleDeviceException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public SimpleDeviceException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public SimpleDeviceException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public SimpleDeviceException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public SimpleDeviceException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public SimpleDeviceException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public SimpleDeviceException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public SimpleDeviceException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public SimpleDeviceException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}