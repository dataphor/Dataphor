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
		public static object Not(object AValue)
		{
			#if NILPROPOGATION
			if (AValue == null)
				return null;
			else
			#endif
				return !((bool)AValue);
		}

		public static object And(object ALeftValue, object ARightValue)
		{
			#if NILPROPOGATION
			if ((ALeftValue == null))
			{
				if ((ARightValue != null) && !(bool)ARightValue)
					return false;
				else
					return null;
			}
			else
			{
				if ((bool)ALeftValue)
					if (ARightValue == null)
						return null;
					else
						return (bool)ARightValue;
				else
					return false;
			}
			#else
			return (bool)ALeftValue && (bool)ARightValue;
			#endif
		}
		
		public static object Or(object ALeftValue, object ARightValue)
		{
			#if NILPROPOGATION
			if (ALeftValue == null)
			{
				if ((ARightValue != null) && (bool)ARightValue)
					return true;
				else
					return null;
			}
			else
			{
				if ((bool)ALeftValue)
					return true;
				else if (ARightValue == null)
					return null;
				else
					return (bool)ARightValue;
			}
			#else
			return new Scalar(AProcess, ADataType, ALeftValue.AsBoolean || ARightValue.AsBoolean);
			#endif
		}

		public static object Xor(object ALeftValue, object ARightValue)
		{
			return 
				Or
				(
					And
					(
						ALeftValue, 
						Not(ARightValue)
					), 
					And
					(
						Not(ALeftValue), 
						ARightValue
					)
				);
		}		
	}

	/// <remarks>operator System.iNot(System.Boolean) : System.Boolean</remarks>    
    public class BooleanNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument)
		{
			return BooleanUtility.Not(AArgument);
		}
    }

    /// <remarks> 
    /// operator System.iAnd(System.Boolean, System.Boolean) : System.Boolean
    /// Be aware!!! D4 does NOT do short circuit evaluation...
    /// </remarks>
    public class BooleanAndNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			return BooleanUtility.And(AArgument1, AArgument2);
		}
    }

    /// <remarks>operator System.iOr(System.Boolean, System.Boolean) : System.Boolean</remarks>
    public class BooleanOrNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			return BooleanUtility.Or(AArgument1, AArgument2);
		}
    }

    /// <remarks>operator System.iXor(System.Boolean, System.Boolean) : System.Boolean</remarks>
    public class BooleanXorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			return BooleanUtility.Xor(AArgument1, AArgument2);
		}
    }
}
