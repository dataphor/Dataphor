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
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Streams.ConveyorException", typeof(ConveyorException).Assembly);

		// Constructors
		public ConveyorException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public ConveyorException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public ConveyorException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public ConveyorException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public ConveyorException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public ConveyorException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public ConveyorException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public ConveyorException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
	}
}