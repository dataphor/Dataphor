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
	using System.Reflection.Emit;

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

	#if UseUnsignedIntegers	
	/// <remarks> operator iNegate(sbyte) : sbyte </remarks>
    public class SByteNegateNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument)
		{
			#if NILPROPOGATION
			if (AArgument == null)
				return null;
			else
			#endif
				return (sbyte)-(sbyte)AArgument;
			
		}
    }
    #endif
    
	/// <remarks> operator iNegate(short) : short </remarks>
    public class ShortNegateNode : UnaryInstructionNode
    {
		public ShortNegateNode() : base()
		{
		}

/*
		protected override void EmitInstructionIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath, LocalBuilder AArguments)
		{
			Label LNil = AGenerator.DefineLabel();
			Label LNotNil = AGenerator.DefineLabel();
			Label LCreateResult = AGenerator.DefineLabel();
			
			LocalBuilder LThis = EmitThis(APlan, AGenerator, AExecutePath);
			LocalBuilder LValue = AGenerator.DeclareLocal(typeof(DataValue));
			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("FDataType", BindingFlags.Instance | BindingFlags.NonPublic));
			
			AGenerator.Emit(OpCodes.Ldloc, AArguments);
			AGenerator.Emit(OpCodes.Ldc_I4_0);
			AGenerator.Emit(OpCodes.Ldelem_Ref, typeof(DataVar));
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Stloc, LValue);
			
			AGenerator.Emit(OpCodes.Ldloc, LValue);
			AGenerator.Emit(OpCodes.Brfalse, LNil);
			
			AGenerator.Emit(OpCodes.Ldloc, LValue);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("IsNil").GetGetMethod());
			AGenerator.Emit(OpCodes.Brfalse, LNil);
			
			AGenerator.Emit(OpCodes.Br, LNotNil);
			
			AGenerator.MarkLabel(LNil);
			
			AGenerator.Emit(OpCodes.Ldnull);
			AGenerator.Emit(OpCodes.Br, LCreateResult);
			
			AGenerator.MarkLabel(LNotNil);
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("FDataType", BindingFlags.Instance | BindingFlags.NonPublic));
			AGenerator.Emit(OpCodes.Castclass, typeof(Schema.ScalarType));
			
			AGenerator.Emit(OpCodes.Ldloc, LValue);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("AsInt16").GetGetMethod());
			
			AGenerator.Emit(OpCodes.Neg);
			AGenerator.Emit(OpCodes.Conv_I2); // According to the docs this isnt necessary?
			AGenerator.Emit(OpCodes.Box, typeof(Int16));
			AGenerator.Emit(OpCodes.Newobj, typeof(IScalar).GetConstructor(new Type[] { typeof(ServerProcess), typeof(Schema.ScalarType), typeof(object) }));
			
			AGenerator.MarkLabel(LCreateResult);
			
			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
		}
*/
		
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return (short)-(short)argument; // Another cast is required because the result of negating a short is an int.
		}
    }
    
	/// <remarks> operator iNegate(int) : int </remarks>
    public class IntegerNegateNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return -(int)argument;
		}
    }
    
	/// <remarks> operator iNegate(long) : long </remarks>
    public class LongNegateNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return -(long)argument;
		}
    }

	#if USEDOUBLE    
	/// <remarks> operator iNegate(double) : double </remarks>    
    public class DoubleNegateNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument)
		{
			#if NILPROPOGATION
			if (AArgument == null)
				return null;
			else
			#endif
				return -(double)AArgument;
		}
    }
    #endif
	
	/// <remarks> operator iNegate(decimal) : decimal </remarks>    
    public class DecimalNegateNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return -(decimal)argument;
		}
    }

    /// <remarks> operator iPower(byte, byte) : byte </remarks>
    public class BytePowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return
					(byte)Math.Pow
					(
						(byte)argument1, 
						(byte)argument2
					);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(sbyte, sbyte) : sbyte </remarks>
    public class SBytePowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(sbyte)Math.Pow
					(
						(sbyte)AArgument1, 
						(sbyte)AArgument2
					);
		}
    }
    #endif
    
    /// <remarks> operator iPower(short, short) : short </remarks>
    public class ShortPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(short)Math.Pow
					(
						(short)argument1, 
						(short)argument2
					);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(ushort, ushort) : ushort </remarks>
    public class UShortPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ushort)Math.Pow
					(
						(ushort)AArgument1, 
						(ushort)AArgument2
					);
		}
    }
    #endif
    
    /// <remarks> operator iPower(integer, integer) : integer </remarks>
    public class IntegerPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(int)Math.Pow
					(
						(int)argument1, 
						(int)argument2
					);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(uint)Math.Pow
					(
						(uint)AArgument1(), 
						(uint)AArgument2()
					);
		}
    }
    #endif
    
    /// <remarks> operator iPower(long, long) : long </remarks>
    public class LongPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(long)Math.Pow
					(
						(long)argument1, 
						(long)argument2
					);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(ulong, ulong) : ulong </remarks>
    public class ULongPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ulong)Math.Pow
					(
						(ulong)AArgument1(), 
						(ulong)AArgument2()
					);
		}
    }
    #endif

	#if USEDOUBLE    
    /// <remarks> operator iPower(double, double) : double </remarks> 
    public class DoublePowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					Math.Pow
					(
						(double)AArgument1(), 
						(double)AArgument2()
					);
		}
    }
    #endif
    
    /// <remarks> operator iPower(decimal, decimal) : decimal </remarks>
    public class DecimalPowerNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(decimal)Math.Pow
					(
						(double)(decimal)argument1, 
						(double)(decimal)argument2
					);
		}
    }

	/// <remarks> operator iMultiplication(byte, byte) : byte </remarks>    
    public class ByteMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(byte)
						(
							(byte)argument1 *
							(byte)argument2
						)
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(sbyte, sbyte) : sbyte </remarks>    
    public class SByteMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(sbyte)
					(
						(sbyte)AArgument1 *
						(sbyte)AArgument2
					);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(short, short) : short </remarks>    
    public class ShortMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(short)
						(
							(short)argument1 *
							(short)argument2
						)
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(ushort, ushort) : ushort </remarks>    
    public class UShortMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ushort)
					(
						(ushort)AArgument1() *
						(ushort)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(integer, integer) : integer </remarks>    
    public class IntegerMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(int)argument1 *
						(int)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(uinteger, uinteger) : uinteger </remarks>    
    public class UIntegerMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return
					(uint)
					(
						(uint)AArgument1 *
						(uint)AArgument2
					)
		}
    }
    #endif

	/// <remarks> operator iMultiplication(long, long) : long </remarks>    
    public class LongMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(long)argument1 *
						(long)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(ulong, ulong) : ulong </remarks>    
    public class ULongMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(ulong)AArgument1 *
						(ulong)AArgument2
					);
		}
    }
    #endif

	#if USEDOUBLE    
	/// <remarks> operator iMultiplication(double, double) : double </remarks>    
    public class DoubleMultiplicationNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(double)AArgument1 *
						(double)AArgument2
					);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(decimal, decimal) : decimal </remarks>    
	public class DecimalMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 * (decimal)argument2;
		}
	}

	/// <remarks> operator iMultiplication(money, decimal) : money </remarks>    
	public class MoneyDecimalMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 * (decimal)argument2;
		}
	}

	/// <remarks> operator iMultiplication(decimal, money) : money </remarks>    
	public class DecimalMoneyMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 * (decimal)argument2;
		}
	}

	/// <remarks> operator iMultiplication(money, integer) : money </remarks>    
	public class MoneyIntegerMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 * (int)argument2;
		}
	}

	/// <remarks> operator iMultiplication(integer, money) : money </remarks>    
	public class IntegerMoneyMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 * (decimal)argument2;
		}
	}

	/// <remarks> operator iDivision(byte, byte) : decimal</remarks>    
    public class ByteDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(
						(decimal)(byte)argument1 /
						(decimal)(byte)argument2
					);
		}
    }
    
	/// <remarks> operator iDiv(byte, byte) : byte</remarks>    
    public class ByteDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(byte)
						(
							(byte)argument1 /
							(byte)argument2
						)
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(sbyte, sbyte) : decimal</remarks>    
    public class SByteDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(decimal)(sbyte)AArgument1() /
						(decimal)(sbyte)AArgument2()
					);
		}
    }

	/// <remarks> operator iDiv(sbyte, sbyte) : sbyte </remarks>    
    public class SByteDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(sbyte)
					(
						(sbyte)AArgument1() /
						(sbyte)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iDivision(short, short) : decimal</remarks>    
    public class ShortDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(
						(decimal)(short)argument1 /
						(decimal)(short)argument2
					);
		}
    }

	/// <remarks> operator iDiv(short, short) : short </remarks>    
    public class ShortDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(short)
						(
							(short)argument1 /
							(short)argument2
						)
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(ushort, ushort) : decimal</remarks>    
    public class UShortDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(decimal)(ushort)AArgument1() /
						(decimal)(ushort)AArgument2()
					);
		}
    }
    #endif

	#if UseUnsignedIntegers	
	/// <remarks> operator iDiv(ushort, ushort) : ushort </remarks>    
    public class UShortDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ushort)
					(
						(ushort)AArgument1() /
						(ushort)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iDivision(integer, integer) : decimal</remarks>    
    public class IntegerDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)(int)argument1 / (int)argument2;
		}
    }

	/// <remarks> operator iDiv(integer, integer) : integer </remarks>    
    public class IntegerDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(int)argument1 /
						(int)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(uinteger, uinteger) : decimal</remarks>    
    public class UIntegerDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(decimal)(uint)AArgument1() /
						(decimal)(uint)AArgument2()
					);
		}
    }

	/// <remarks> operator iDiv(uinteger, uinteger) : uinteger </remarks>    
    public class UIntegerDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(uint)AArgument1() /
						(uint)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iDivision(long, long) : decimal</remarks>    
    public class LongDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)(long)argument1 / (decimal)(long)argument2;
		}
    }

	/// <remarks> operator iDiv(long, long) : long </remarks>    
    public class LongDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return
					checked
					(
						(long)argument1 /
						(long)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(ulong, ulong) : decimal</remarks>    
    public class ULongDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(decimal)(ulong)AArgument1() /
						(decimal)(ulong)AArgument2()
					);
		}
    }

	/// <remarks> operator iDiv(ulong, ulong) : ulong</remarks>    
    public class ULongDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(ulong)AArgument1() /
						(ulong)AArgument2()
					);
		}
    }
    #endif

	#if USEDOUBLE
	/// <remarks> operator iDivision(double, double) : double </remarks>    
    public class DoubleDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(double)AArgument1() /
						(double)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iDivision(decimal, decimal) : decimal </remarks>    
    public class DecimalDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 / (decimal)argument2;
		}
    }

	/// <remarks> operator iDiv(decimal, decimal) : decimal </remarks>    
    public class DecimalDivNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(decimal)Math.Floor
					(
						checked
						(
							(double)
							(
								(decimal)argument1 /
								(decimal)argument2
							)
						)
					);
		}
    }

	/// <remarks> operator iDivision(Money, Money) : Decimal </remarks>    
    public class MoneyDivisionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 / (decimal)argument2;
		}
    }

	/// <remarks> operator iDivision(money, decimal) : money </remarks>    
	public class MoneyDecimalDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 / (decimal)argument2;
		}
	}

	/// <remarks> operator iDivision(decimal, money) : money </remarks>    
	public class DecimalMoneyDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 / (decimal)argument2;
		}
	}

	/// <remarks> operator iDivision(money, integer) : money </remarks>    
	public class MoneyIntegerDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 / (int)argument2;
		}
	}

	/// <remarks> operator iDivision(integer, money) : money </remarks>    
	public class IntegerMoneyDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 / (decimal)argument2;
		}
	}
	
	/// <remarks> operator iMod(byte, byte) : byte </remarks>    
    public class ByteModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(byte)
					(
						(byte)argument1 %
						(byte)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(sbyte, sbyte) : sbyte </remarks>    
    public class SByteModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(sbyte)
					(
						(sbyte)AArgument1() %
						(sbyte)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iMod(short, short) : short </remarks>    
    public class ShortModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					(short)
					(
						(short)argument1 %
						(short)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(ushort, ushort) : ushort </remarks>    
    public class UShortModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ushort)
					(
						(ushort)AArgument1() %
						(ushort)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iMod(integer, integer) : integer </remarks>    
    public class IntegerModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 % (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(uinteger, uinteger) : uinteger </remarks>    
    public class UIntegerModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (uint)AArgument1 % (uint)AArgument2;
		}
    }
    #endif

	/// <remarks> operator iMod(long, long) : long </remarks>    
    public class LongModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 % (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(ulong, ulong) : ulong </remarks>    
    public class ULongModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ulong)AArgument1() % (ulong)AArgument2;
		}
    }
    #endif

	#if USEDOUBLE
	/// <remarks> operator iMod(double, double) : double </remarks>    
    public class DoubleModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (double)AArgument1() % (double)AArgument2;
		}
    }
    #endif

	/// <remarks> operator iMod(decimal, decimal) : decimal </remarks>    
    public class DecimalModNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 % (decimal)argument2;
		}
    }

	/// <remarks> operator iAddition(string, string) : string </remarks>    
	public class StringAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (string)argument1 + (string)argument2;
		}
	}
    
	/// <remarks> operator iAddition(byte, byte) : byte </remarks>    
	public class ByteAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(byte)
						(
							(byte)argument1 +
							(byte)argument2
						)
					);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(sbyte, sbyte) : sbyte </remarks>    
	public class SByteAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(sbyte)
					(
						(sbyte)AArgument1() +
						(sbyte)AArgument2()
					);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(short, short) : short </remarks>    
	public class ShortAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(short)
						(
							(short)argument1 +
							(short)argument2
						)
					);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(ushort, ushort) : ushort </remarks>    
	public class UShortAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ushort)
					(
						(ushort)AArgument1() +
						(ushort)AArgument2()
					);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(integer, integer) : integer </remarks>    
	public class IntegerAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return checked((int)argument1 + (int)argument2);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(uinteger, uinteger) : uinteger </remarks>    
	public class UIntegerAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(uint)AArgument1() +
						(uint)AArgument2()
					);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(long, long) : long </remarks>    
	public class LongAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(long)argument1 +
						(long)argument2
					);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(ulong, ulong) : ulong </remarks>    
	public class ULongAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(ulong)AArgument1() +
						(ulong)AArgument2()
					);
		}
	}
	#endif

	#if USEDOUBLE
	/// <remarks> operator iAddition(double, double) : double </remarks>    
	public class DoubleAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(double)AArgument1() +
						(double)AArgument2()
					);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(decimal, decimal) : decimal </remarks>    
	public class DecimalAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 + (decimal)argument2;
		}
	}
    
	/// <remarks> operator iAddition(money, money) : money </remarks>    
	public class MoneyAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 + (decimal)argument2;
		}
	}
    
	/// <remarks> operator iSubtraction(byte, byte) : byte </remarks>
    public class ByteSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(byte)
						(
							(byte)argument1 -
							(byte)argument2
						)
					);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iSubtraction(sbyte, sbyte) : sbyte </remarks>
    public class SByteSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(sbyte)
					(
						(sbyte)AArgument1() -
						(sbyte)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iSubtraction(short, short) : short </remarks>
    public class ShortSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(short)
						(
							(short)argument1 -
							(short)argument2
						)
					);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iSubtraction(ushort, ushort) : ushort </remarks>
    public class UShortSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(ushort)
					(
						(ushort)AArgument1() -
						(ushort)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iSubtraction(integer, integer) : integer </remarks>
    public class IntegerSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(int)argument1 -
						(int)argument2
					);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iSubtraction(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(uint)AArgument1() -
						(uint)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iSubtraction(long, long) : long </remarks>
    public class LongSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return 
					checked
					(
						(long)argument1 -
						(long)argument2
					);
		}
    }
    
	#if UseUnsignedIntegers
	/// <remarks> operator iSubtraction(ulong, ulong) : ulong </remarks>
    public class ULongSubtractionNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(ulong)AArgument1() -
						(ulong)AArgument2()
					);
		}
    }
    #endif
    
	#if USEDOUBLE
	/// <remarks> operator iSubtraction(double, double) : double </remarks>
	public class DoubleSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return 
					(
						(double)AArgument1() -
						(double)AArgument2()
					);
		}
	}
	#endif
    
	/// <remarks> operator iSubtraction(decimal, decimal) : decimal </remarks>
	public class DecimalSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 - (decimal)argument2;
		}
	}

	/// <remarks> operator iSubtraction(money, money) : money </remarks>    
	public class MoneySubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 - (decimal)argument2;
		}
	}
}
