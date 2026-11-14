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

	/// <remarks> operator iBitwiseNot(byte) : byte </remarks>
    public class ByteBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (byte)~(byte)argument1;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(sbyte) : sbyte </remarks>
    public class SByteBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)~(sbyte)AArgument1;
		}
    }
    #endif
    
	/// <remarks> operator iBitwiseNot(short) : short </remarks>
    public class ShortBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (short)~(short)argument1;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(ushort) : ushort </remarks>
    public class UShortBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)~(ushort)AArgument1;
		}
    }
    #endif
    
	/// <remarks> operator iBitwiseNot(int) : int </remarks>
    public class IntegerBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ~(int)argument1;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(uint) : uint </remarks>
    public class UIntegerBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ~(uint)AArgument1;
		}
    }
    #endif
    
	/// <remarks> operator iBitwiseNot(long) : long </remarks>
    public class LongBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ~(long)argument1;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(ulong) : ulong </remarks>
    public class ULongBitwiseNotNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ~(ulong)AArgument1;
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(byte, byte) : byte </remarks>
    public class ByteBitwiseAndNode : BinaryInstructionNode
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
						(byte)argument1 &
						(byte)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(sbyte, sbyte) : sbyte </remarks>
    public class SByteBitwiseAndNode : BinaryInstructionNode
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
						(sbyte)AArgument1() &
						(sbyte)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(short, short) : short </remarks>
    public class ShortBitwiseAndNode : BinaryInstructionNode
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
						(short)argument1 &
						(short)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(ushort, ushort) : ushort </remarks>
    public class UShortBitwiseAndNode : BinaryInstructionNode
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
						(ushort)AArgument1() &
						(ushort)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(integer, integer) : integer </remarks>
    public class IntegerBitwiseAndNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 & (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerBitwiseAndNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (uint)AArgument1() & (uint)AArgument2;
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(long, long) : long </remarks>
    public class LongBitwiseAndNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 & (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(ulong, ulong) : ulong </remarks>
    public class ULongBitwiseAndNode : BinaryInstructionNode
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
						(ulong)AArgument1() &
						(ulong)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(byte, byte) : byte </remarks>
    public class ByteBitwiseOrNode : BinaryInstructionNode
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
						(byte)argument1 |
						(byte)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(sbyte, sbyte) : sbyte </remarks>
    public class SByteBitwiseOrNode : BinaryInstructionNode
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
						(byte)(sbyte)AArgument1() |
						(byte)(sbyte)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(short, short) : short </remarks>
    public class ShortBitwiseOrNode : BinaryInstructionNode
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
						(ushort)(short)argument1 |
						(ushort)(short)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(ushort, ushort) : ushort </remarks>
    public class UShortBitwiseOrNode : BinaryInstructionNode
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
						(ushort)AArgument1() |
						(ushort)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(integer, integer) : integer </remarks>
    public class IntegerBitwiseOrNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 | (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerBitwiseOrNode : BinaryInstructionNode
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
						(uint)AArgument1() |
						(uint)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(long, long) : long </remarks>
    public class LongBitwiseOrNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 | (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(ulong, ulong) : ulong </remarks>
    public class ULongBitwiseOrNode : BinaryInstructionNode
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
						(ulong)AArgument1() |
						(ulong)AArgument2()
					);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(byte, byte) : byte </remarks>
    public class ByteBitwiseXorNode : BinaryInstructionNode
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
						(byte)argument1 ^
						(byte)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(sbyte, sbyte) : sbyte </remarks>
    public class SByteBitwiseXorNode : BinaryInstructionNode
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
						(sbyte)AArgument1() ^
						(sbyte)AArgument2()
					);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(short, short) : short </remarks>
    public class ShortBitwiseXorNode : BinaryInstructionNode
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
						(short)argument1 ^
						(short)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(ushort, ushort) : ushort </remarks>
    public class UShortBitwiseXorNode : BinaryInstructionNode
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
						(ushort)AArgument1() ^
						(ushort)AArgument2()
					);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(integer, integer) : integer </remarks>
    public class IntegerBitwiseXorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 ^ (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerBitwiseXorNode : BinaryInstructionNode
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
						(uint)AArgument1() ^
						(uint)AArgument2()
					);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(long, long) : long </remarks>
    public class LongBitwiseXorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 ^ (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(ulong, ulong) : ulong </remarks>
    public class ULongBitwiseXorNode : BinaryInstructionNode
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
						(ulong)AArgument1() ^
						(ulong)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(byte, integer) : byte </remarks>
    public class ByteShiftLeftNode : BinaryInstructionNode
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
						(byte)argument1 <<
						(int)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLeftShift(sbyte, integer) : sbyte </remarks>
    public class SByteShiftLeftNode : BinaryInstructionNode
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
						(sbyte)AArgument1() <<
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(short, integer) : short </remarks>
    public class ShortShiftLeftNode : BinaryInstructionNode
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
						(short)argument1 <<
						(int)argument2
					);
		}
    }

	#if UseUnsignedIntegers
    /// <remarks> operator iLeftShift(ushort, integer) : ushort </remarks>
    public class UShortShiftLeftNode : BinaryInstructionNode
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
						(ushort)AArgument1() <<
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(integer, integer) : integer </remarks>
    public class IntegerShiftLeftNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 << (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLeftShift(uinteger, integer) : uinteger </remarks>
    public class UIntegerShiftLeftNode : BinaryInstructionNode
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
						(uint)AArgument1 <<
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(long, integer) : long </remarks>
    public class LongShiftLeftNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 << (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLeftShift(ulong, integer) : ulong </remarks>
    public class ULongShiftLeftNode : BinaryInstructionNode
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
						(ulong)AArgument1() <<
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator >>(byte, integer) : byte </remarks>
    public class ByteShiftRightNode : BinaryInstructionNode
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
						(byte)argument1 >>
						(int)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(sbyte, integer) : sbyte </remarks>
    public class SByteShiftRightNode : BinaryInstructionNode
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
						(sbyte)AArgument1() >>
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator >>(short, integer) : short </remarks>
    public class ShortShiftRightNode : BinaryInstructionNode
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
						(short)argument1 >>
						(int)argument2
					);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(ushort, integer) : ushort </remarks>
    public class UShortShiftRightNode : BinaryInstructionNode
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
						(ushort)AArgument1() >>
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator >>(integer, integer) : integer </remarks>
    public class IntegerShiftRightNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 >> (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(uinteger, integer) : uinteger </remarks>
    public class UIntegerShiftRightNode : BinaryInstructionNode
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
						(uint)AArgument1() >>
						(int)AArgument2
					);
		}
    }
    #endif

    /// <remarks> operator >>(long, integer) : long </remarks>
    public class LongShiftRightNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 >> (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(ulong, integer) : ulong </remarks>
    public class ULongShiftRightNode : BinaryInstructionNode
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
						(ulong)AArgument1 >>
						(int)AArgument2
					);
		}
    }
    #endif
}
