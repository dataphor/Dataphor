/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class ScanException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 110100: "Cannot perform this operation on a closed scan."</summary>
			ScanInactive = 110100,

			/// <summary>Error code 110101: "Scan has no active row."</summary>
			NoActiveRow = 110101,

			/// <summary>Error code 110102: "Row could not be located using clustered index."</summary>
			ClusteredRowNotFound = 110102
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Runtime.Data.ScanException", typeof(ScanException).Assembly);

		// Constructors
		public ScanException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, null, null) {}
		public ScanException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, null, paramsValue) {}
		public ScanException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, innerException, null) {}
		public ScanException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, innerException, paramsValue) {}
		public ScanException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ScanException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ScanException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ScanException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public ScanException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}