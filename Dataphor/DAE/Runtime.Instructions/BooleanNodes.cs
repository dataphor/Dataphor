/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System; 
	using System.Reflection;

	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public sealed class BooleanUtility
	{
		public static DataValue Not(ServerProcess AProcess, Schema.IScalarType ADataType, DataValue AValue)
		{
			#if NILPROPOGATION
			if ((AValue == null) || AValue.IsNil)
				return null;
			else
			#endif
				return new Scalar(AProcess, ADataType, !AValue.AsBoolean);
		}

		public static DataValue And(ServerProcess AProcess, Schema.IScalarType ADataType, DataValue ALeftValue, DataValue ARightValue)
		{
			#if NILPROPOGATION
			if ((ALeftValue == null) || ALeftValue.IsNil)
			{
				if ((ARightValue != null) && !ARightValue.IsNil && !ARightValue.AsBoolean)
					return new Scalar(AProcess, ADataType, false);
				else
					return null;
			}
			else
			{
				if (ALeftValue.AsBoolean)
					if ((ARightValue == null) || ARightValue.IsNil)
						return null;
					else
						return new Scalar(AProcess, ADataType, ARightValue.AsBoolean);
				else
					return new Scalar(AProcess, ADataType, false);
			}
			#else
			return new Scalar(AProcess, ADataType, ALeftValue.AsBoolean && ARightValue.AsBoolean);
			#endif
		}
		
		public static DataValue Or(ServerProcess AProcess, Schema.IScalarType ADataType, DataValue ALeftValue, DataValue ARightValue)
		{
			#if NILPROPOGATION
			if ((ALeftValue == null) || ALeftValue.IsNil)
			{
				if ((ARightValue != null) && !ARightValue.IsNil && ARightValue.AsBoolean)
					return new Scalar(AProcess, ADataType, true);
				else
					return null;
			}
			else
			{
				if (ALeftValue.AsBoolean)
					return new Scalar(AProcess, ADataType, true);
				else if ((ARightValue == null) || ARightValue.IsNil)
					return null;
				else
					return new Scalar(AProcess, ADataType, ARightValue.AsBoolean);
			}
			#else
			return new Scalar(AProcess, ADataType, ALeftValue.AsBoolean || ARightValue.AsBoolean);
			#endif
		}

		public static DataValue Xor(ServerProcess AProcess, Schema.IScalarType ADataType, DataValue ALeftValue, DataValue ARightValue)
		{
			return 
				Or
				(
					AProcess, 
					ADataType, 
					And
					(
						AProcess, 
						ADataType, 
						ALeftValue, 
						Not(AProcess, ADataType, ARightValue)
					), 
					And
					(
						AProcess, 
						ADataType, 
						Not(AProcess, ADataType, ALeftValue), 
						ARightValue
					)
				);
		}		
	}

	/// <remarks>operator System.iNot(System.Boolean) : System.Boolean</remarks>    
    public class BooleanNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, BooleanUtility.Not(AProcess, (Schema.IScalarType)FDataType, AArguments[0].Value));
		}
    }

    /// <remarks> 
    /// operator System.iAnd(System.Boolean, System.Boolean) : System.Boolean
    /// Be aware!!! D4 does NOT do short circuit evaluation...
    /// </remarks>
    public class BooleanAndNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, BooleanUtility.And(AProcess, (Schema.IScalarType)FDataType, AArguments[0].Value, AArguments[1].Value));
		}
    }

    /// <remarks>operator System.iOr(System.Boolean, System.Boolean) : System.Boolean</remarks>
    public class BooleanOrNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, BooleanUtility.Or(AProcess, (Schema.IScalarType)FDataType, AArguments[0].Value, AArguments[1].Value));
		}
    }

    /// <remarks>operator System.iXor(System.Boolean, System.Boolean) : System.Boolean</remarks>
    public class BooleanXorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, BooleanUtility.Xor(AProcess, (Schema.IScalarType)FDataType, AArguments[0].Value, AArguments[1].Value));
		}
    }
}
