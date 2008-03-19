/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE.Device;

namespace Alphora.Dataphor.Frontend.Server.Device
{
	[Serializable]
	public class FrontendDeviceException : DeviceException
	{
		public new enum Codes : int
		{
			/// <summary>Error code 202100: "Designer "{0}" is associated with document types and cannot be modified or removed."</summary>
			DesignerIsAssociatedWithDocumentTypes = 202100,

			/// <summary>Error code 202101: "Modification of table "{0}" is not supported."</summary>
			UnsupportedUpdate = 202101,
			
			/// <summary>Error code 202102: "Document type "{0}" not found."</summary>
			DocumentTypeNotFound = 202102,
			
			/// <summary>Error code 202103: "Designer "{0}" not found."</summary>
			DesignerNotFound = 202103,

			/// <summary>Error code 202104: "Cannot copy or move document ({0}: {1}) to itself."</summary>
			CannotCopyDocumentToSelf = 202104
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Server.Device.FrontendDeviceException", typeof(FrontendDeviceException).Assembly);

		// Constructors
		public FrontendDeviceException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public FrontendDeviceException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public FrontendDeviceException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public FrontendDeviceException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public FrontendDeviceException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public FrontendDeviceException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public FrontendDeviceException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public FrontendDeviceException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public FrontendDeviceException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}
