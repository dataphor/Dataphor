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
	using System.Reflection.Emit;
	using System.Collections.Generic;
	using Sigil;

	public sealed class BooleanUtility
	{
		public static object Not(object tempValue)
		{
			#if NILPROPOGATION
			if (tempValue == null)
				return null;
			else
			#endif
				return !((bool)tempValue);
		}

		public static object And(object leftValue, object rightValue)
		{
			#if NILPROPOGATION
			if ((leftValue == null))
			{
				if ((rightValue != null) && !(bool)rightValue)
					return false;
				else
					return null;
			}
			else
			{
				if ((bool)leftValue)
					if (rightValue == null)
						return null;
					else
						return (bool)rightValue;
				else
					return false;
			}
			#else
			return (bool)ALeftValue && (bool)ARightValue;
			#endif
		}
		
		public static object Or(object leftValue, object rightValue)
		{
			#if NILPROPOGATION
			if (leftValue == null)
			{
				if ((rightValue != null) && (bool)rightValue)
					return true;
				else
					return null;
			}
			else
			{
				if ((bool)leftValue)
					return true;
				else if (rightValue == null)
					return null;
				else
					return (bool)rightValue;
			}
			#else
			return new Scalar(AProcess, ADataType, ALeftValue.AsBoolean || ARightValue.AsBoolean);
			#endif
		}

		public static object Xor(object leftValue, object rightValue)
		{
			return 
				Or
				(
					And
					(
						leftValue, 
						Not(rightValue)
					), 
					And
					(
						Not(leftValue), 
						rightValue
					)
				);
		}		
	}

	/// <remarks>operator System.iNot(System.Boolean) : System.Boolean</remarks>	
	public class BooleanNotNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			return BooleanUtility.Not(argument);
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.Not();
		}
	}

	/// <remarks> 
	/// operator System.iAnd(System.Boolean, System.Boolean) : System.Boolean
	/// Be aware!!! D4 does NOT do short circuit evaluation...
	/// </remarks>
	public class BooleanAndNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			return BooleanUtility.And(argument1, argument2);
		}

		public override bool CanEmitIL => true;

		public override ArgumentEmissionStyle ArgumentEmissionStyle
		{
			get { return ArgumentEmissionStyle.PhysicalInLocals; }
		}

		//AND(A, B)
		//			B
		//		F	U	T
		//	F	F	F	F
		//A	U	F	U	U
		//	T	F	U	T

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			if (!Nodes[0].IsNilable && !Nodes[1].IsNilable)
			{
				// Optimization, use an actual AND rather than branching if neither operand is nilable
				m.IL.LoadLocal(arguments[0]);
				m.IL.LoadLocal(arguments[1]);
				m.IL.And();
			}
			else
			{
				var nullLabel = m.IL.DefineLabel();
				var endLabel = m.IL.DefineLabel();

				var rightDecides = m.IL.DefineLabel();
				var rightFalse = m.IL.DefineLabel();
				m.IL.LoadLocal(arguments[0]);
				if (Nodes[0].IsNilable)
				{
					m.IL.BranchIfFalse(rightFalse);
					m.IL.LoadLocal(arguments[0]);
					m.IL.UnboxAny(typeof(bool));
				}
				m.IL.BranchIfTrue(rightDecides);
				m.IL.LoadConstant(0);
				m.NativeToPhysical(this);
				m.IL.Branch(endLabel);

				m.IL.MarkLabel(rightFalse);
				m.IL.LoadLocal(arguments[1]);
				if (Nodes[1].IsNilable)
				{
					m.IL.BranchIfFalse(nullLabel);
					m.IL.LoadLocal(arguments[1]);
					m.IL.UnboxAny(typeof(bool));
				}
				m.IL.BranchIfTrue(nullLabel);
				m.IL.LoadConstant(0);
				m.NativeToPhysical(this);
				m.IL.Branch(endLabel);

				m.IL.MarkLabel(rightDecides);
				m.IL.LoadLocal(arguments[1]);
				if (Nodes[1].IsNilable)
				{
					m.IL.BranchIfFalse(nullLabel);
					m.IL.LoadLocal(arguments[1]);
					m.IL.UnboxAny(typeof(bool));
				}

				m.NativeToPhysical(this);
				m.IL.Branch(endLabel);
				m.IL.MarkLabel(nullLabel);
				m.IL.LoadNull();

				m.IL.MarkLabel(endLabel);
				m.IL.Nop();
			}
		}

		protected override void EmitPostInstruction(NativeMethod m)
		{
			// Already in physical type
		}
	}

	/// <remarks>operator System.iOr(System.Boolean, System.Boolean) : System.Boolean</remarks>
	public class BooleanOrNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			return BooleanUtility.Or(argument1, argument2);
		}

		public override bool CanEmitIL => true;

		public override ArgumentEmissionStyle ArgumentEmissionStyle
		{
			get { return ArgumentEmissionStyle.PhysicalInLocals; }
		}

		//OR(A, B)
		//			B
		//		F	U	T
		//	F	F	U	T
		//A	U	U	U	T
		//	T	T	T	T

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			if (!IsNilable)
			{
				// Optimization, use an actual OR rather than branching if neither operand is nilable
				m.IL.LoadLocal(arguments[0]);
				m.IL.LoadLocal(arguments[1]);
				m.IL.Or();
			}
			else
			{
				var nullLabel = m.IL.DefineLabel();
				var endLabel = m.IL.DefineLabel();

				var rightDecides = m.IL.DefineLabel();
				var rightTrue = m.IL.DefineLabel();
				m.IL.LoadLocal(arguments[0]);
				if (Nodes[0].IsNilable)
				{
					m.IL.BranchIfFalse(rightTrue);
					m.IL.LoadLocal(arguments[0]);
					m.IL.UnboxAny(typeof(bool));
				}
				m.IL.BranchIfFalse(rightDecides);
				m.IL.LoadConstant(1);
				m.NativeToPhysical(this);
				m.IL.Branch(endLabel);

				m.IL.MarkLabel(rightTrue);
				m.IL.LoadLocal(arguments[1]);
				if (Nodes[1].IsNilable)
				{
					m.IL.BranchIfFalse(nullLabel);
					m.IL.LoadLocal(arguments[1]);
					m.IL.UnboxAny(typeof(bool));
				}
				m.IL.BranchIfFalse(nullLabel);
				m.IL.LoadConstant(1);
				m.NativeToPhysical(this);
				m.IL.Branch(endLabel);

				m.IL.MarkLabel(rightDecides);
				m.IL.LoadLocal(arguments[1]);
				if (Nodes[1].IsNilable)
				{
					m.IL.BranchIfFalse(nullLabel);
					m.IL.LoadLocal(arguments[1]);
					m.IL.UnboxAny(typeof(bool));
				}

				m.NativeToPhysical(this);
				m.IL.Branch(endLabel);
				m.IL.MarkLabel(nullLabel);
				m.IL.LoadNull();

				m.IL.MarkLabel(endLabel);
				m.IL.Nop();
			}
		}

        protected override void EmitPostInstruction(NativeMethod m)
        {
            // Already in physical type
        }
    }

    /// <remarks>operator System.iXor(System.Boolean, System.Boolean) : System.Boolean</remarks>
    public class BooleanXorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			return BooleanUtility.Xor(argument1, argument2);
		}

		public override bool CanEmitIL => true;

		//XOR(A, B)
		//			B
		//		F	U	T
		//	F	F	U	T
		//A	U	U	U	U
		//	T	T	U	F
		
		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Xor();
		}
	}
}
