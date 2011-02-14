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
	public class IndexException : DAEException
	{
		public enum Codes 
		{
			/// <summary>Error code 117100: "Internal Index Error: Unable to compare keys."</summary>
			UnableToCompareKeys = 117100,

			/// <summary>Error code 117101: "Internal Index Error: "Unable to copy key."</summary>
			UnableToCopyKey = 117101,

			/// <summary>Error code 117102: "Internal Index Error: "Unable to copy data."</summary>
			UnableToCopyData = 117102,

			/// <summary>Error code 117103: "Internal Index Error: "Unable to dispose key."</summary>
			UnableToDisposeKey = 117103,

			/// <summary>Error code 117104: "Internal Index Error: "Unable to dispose data."</summary>
			UnableToDisposeData = 117104,

			/// <summary>Error code 117105: "Internal Index Error: "Duplicate key violation."</summary>
			DuplicateKey = 117105,

			/// <summary>Error code 117106: "Internal Index Error: "Duplicate routing key."</summary>
			DuplicateRoutingKey = 117106,

			/// <summary>Error code 117107: "Internal Index Error: "Key not found."</summary>
			KeyNotFound = 117107
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Runtime.Data.IndexException", typeof(IndexException).Assembly);

		// Constructors
		public IndexException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, null, null) {}
		public IndexException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, null, paramsValue) {}
		public IndexException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, innerException, null) {}
		public IndexException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.System, innerException, paramsValue) {}
		public IndexException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public IndexException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public IndexException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public IndexException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public IndexException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}