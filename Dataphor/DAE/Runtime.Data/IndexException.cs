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
	[Serializable]
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
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Runtime.Data.IndexException", typeof(IndexException).Assembly);

		// Constructors
		public IndexException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, null, null) {}
		public IndexException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, null, AParams) {}
		public IndexException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, AInnerException, null) {}
		public IndexException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.System, AInnerException, AParams) {}
		public IndexException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public IndexException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public IndexException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public IndexException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public IndexException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}