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
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Server.Device.FrontendDeviceException", typeof(FrontendDeviceException).Assembly);

		// Constructors
		public FrontendDeviceException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public FrontendDeviceException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public FrontendDeviceException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public FrontendDeviceException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public FrontendDeviceException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public FrontendDeviceException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public FrontendDeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public FrontendDeviceException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	}
}
