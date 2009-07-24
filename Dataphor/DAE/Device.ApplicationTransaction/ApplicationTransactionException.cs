/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Device;

namespace Alphora.Dataphor.DAE.Device.ApplicationTransaction
{
	[Serializable]
	public class ApplicationTransactionException : DeviceException
	{
		public new enum Codes : int
		{
			/// <summary>Error Code 125100: "Invalid application transaction id "{0}"."</summary>
			InvalidApplicationTransactionID = 125100,
			
			/// <summary>Error Code 125101: "Unknown application transaction operation type "{0}"."</summary>
			UnknownOperation = 125101,
			
			/// <summary>Error Code 125102: "TableMap container."</summary>
			TableMapContainer = 125102,
			
			/// <summary>Error Code 125103: "TableMap not found for table "{0}"."</summary>
			TableMapNotFound = 125103,
			
			/// <summary>Error Code 125104: "Application transaction id "{0}" is already being populated."</summary>
			SourceAlreadyPopulating = 125104,
			
			/// <summary>Error Code 125105: "Application transaction id "{0}" has been closed and cannot be further manipulated."</summary>
			ApplicationTransactionClosed = 125105,
			
			/// <summary>Error Code 125106: "This process is already participating in an application transaction."</summary>
			ProcessAlreadyParticipating = 125106,
			
			/// <summary>Error Code 125107: "This process not participating in an application transaction."</summary>
			ProcessNotParticipating = 125107,
			
			/// <summary>Error Code 125108: "Table variable "{0}" is already an application transaction table variable for table variable "{1}"."</summary>
			AlreadyApplicationTransactionVariable = 125108,
			
			/// <summary>Error Code 125109: "Table variable "{0}" is participating in at least one application transaction and cannot be altered or dropped."</summary>
			TableVariableParticipating = 125109,
			
			/// <summary>Error Code 125110: "Operator "{0}" is participating in at least one application transaction and cannot be altered or dropped."</summary>
			OperatorParticipating = 125110
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Device.ApplicationTransaction.ApplicationTransactionException", typeof(ApplicationTransactionException).Assembly);

		// Constructors
		public ApplicationTransactionException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public ApplicationTransactionException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public ApplicationTransactionException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public ApplicationTransactionException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public ApplicationTransactionException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public ApplicationTransactionException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public ApplicationTransactionException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public ApplicationTransactionException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public ApplicationTransactionException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}

