/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language
{
	using System;
	using System.Resources;
	
	using Alphora.Dataphor.DAE;

	[Serializable]
	public class MachineException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 108100: "Invalid instruction "{0}"."</summary>
			InvalidInstruction = 108100,
			
			/// <summary>Error code 108102: "Unknown value type "{0}"."</summary>
			UnknownValueType = 108102,

			/// <summary>Error code 108103: "Alter table statement may contain new columns, or drop columns, but not both."</summary>
			InvalidAlterTableStatement = 108103,

			/// <summary>Error code 108104: "Unimplemented: SQLMachine.iAvg()."</summary>
			UnimplementedAvg = 108104,

			/// <summary>Error code 108105: "Unimplemented: SQLMachine.iCount()."</summary>
			UnimplementedCount = 108105,

			/// <summary>Error code 108106: "Unimplemented: SQLMachine.iSum()."</summary>
			UnimplementedSum = 108106,

			/// <summary>Error code 108107: "Unimplemented: SQLMachine.iMax()."</summary>
			UnimplementedMax = 108107,

			/// <summary>Error code 108108: "Unimplemented: SQLMachine.iMin()."</summary>
			UnimplementedMin = 108108,

			/// <summary>Error code 108109: "Unimplemented: SQLMachine.iGetValue()."</summary>
			UnimplementedGetValue = 108109,

			/// <summary>Error code 108110: "Unimplemented: SQLMachine.iClearValue()."</summary>
			UnimplementedClearValue = 108110,

			/// <summary>Error code 108111: "Unimplemented: SQLMachine.iSetValue()."</summary>
			UnimplementedSetValue = 108111,

			/// <summary>Error code 108112: "Unimplemented: SQLMachine.iGetRecordValue()."</summary>
			UnimplementedGetRecordValue = 108112,

			/// <summary>Error code 108113: "Unimplemented: SQLMachine.iSetRecord()."</summary>
			UnimplementedSetRecord = 108113,

			/// <summary>Error code 108114: "Unimplemented: SQLMachine.iClearRecord()."</summary>
			UnimplementedClearRecord = 108114,

			/// <summary>Error code 108115: "Unimplemented: SQLMachine.iDistinct()"</summary>
			UnimplementedDistinct = 108115
		}
		
		// const string telling the DataphorException class where to find these error messages.
		private static readonly ResourceBaseName CErrorMessageBaseName = new ResourceBaseName("Alphora.Dataphor.DAE.Language.MachineException");

		// Constructors
		public MachineException(Codes AErrorCode) : base((int)AErrorCode, CErrorMessageBaseName) {}
		public MachineException(Codes AErrorCode, params object[] AParams) : base((int)AErrorCode, CErrorMessageBaseName, AParams) {}
		public MachineException(Codes AErrorCode, Exception AInnerException) : base((int)AErrorCode, CErrorMessageBaseName, AInnerException) {}
		public MachineException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base((int)AErrorCode, CErrorMessageBaseName, AInnerException, AParams) {}
		public MachineException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base (AInfo, AContext) {}
	}
}