/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Streams
{
	public class ConveyorException : DAEException 
	{
		public enum Codes : int
		{
			/// <summary>Error code 111100: "Attempt to access data in an uninitialized scalar."</summary>
			UninitializedScalar = 111100,

			/// <summary>Error code 111101: "Attempt to access data in an uninitialized stream."</summary>
			UninitializedStream = 111101,
		
			/// <summary>Error code 111102: "Input string was not in correct format:  {0}"</summary>
			InvalidStringArgument = 111102,

			/// <summary>Error code 111103: "Nanoseconds must be in multiples of 100:  {0}"</summary>
			InvalidNanosecondArgument = 111103
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Streams.ConveyorException", typeof(ConveyorException).Assembly);

		// Constructors
		public ConveyorException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ConveyorException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ConveyorException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ConveyorException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ConveyorException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ConveyorException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ConveyorException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ConveyorException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public ConveyorException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}