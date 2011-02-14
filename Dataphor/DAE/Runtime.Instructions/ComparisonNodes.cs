/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System; 
	using System.IO;
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

	/// <remarks> operator iEqual(scalar, scalar) : Boolean </remarks>
    public class ScalarEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				throw new RuntimeException(RuntimeException.Codes.ValueEncountered);
		}
    }
    
	/// <remarks> operator iEqual(byte, byte) : bool </remarks>
    public class ByteEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (byte)argument1 == (byte)argument2;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(sbyte, sbyte) : bool </remarks>
    public class SByteEqualNode : BinaryInstructionNode
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
						(sbyte)AArgument1() ==
						(sbyte)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(short, short) : bool </remarks>
    public class ShortEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (short)argument1 == (short)argument2;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(ushort, ushort) : bool </remarks>
    public class UShortEqualNode : BinaryInstructionNode
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
						(ushort)AArgument1() ==
						(ushort)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(integer, integer) : bool </remarks>
    public class IntegerEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 == (int)argument2;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(uinteger, uinteger) : bool </remarks>
    public class UIntegerEqualNode : BinaryInstructionNode
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
						(uint)AArgument1() ==
						(uint)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(long, long) : bool </remarks>
    public class LongEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 == (long)argument2;
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(ulong, ulong) : bool </remarks>
    public class ULongEqualNode : BinaryInstructionNode
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
						(ulong)AArgument1() ==
						(ulong)AArgument2()
					);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(Guid, Guid) : bool </remarks>
    public class GuidEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (Guid)argument1 == (Guid)argument2;
		}
    }
    
    /// <remarks> operator iEqual(bool, bool) : bool </remarks>
    public class BooleanEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (bool)argument1 == (bool)argument2;
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iEqual(double, double) : bool </remarks>
    public class DoubleEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (double)AArgument1() == (double)AArgument2;
		}
    }
    #endif

    /// <remarks> operator iEqual(decimal, decimal) : bool </remarks>
    public class DecimalEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 == (decimal)argument2;
		}
    }

	/// <remarks> operator iEqual(string, string) : bool </remarks>
	public class StringEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return String.Equals((string)argument1, (string)argument2, StringComparison.Ordinal);
		}
	}

	#if USEISTRING
	public class IStringEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, true) == 0;
		}
	}
	#endif

	/// <remarks> operator iEqual(money, money) : bool </remarks>
	public class MoneyEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 == (decimal)argument2;
		}
	}
	
	/// <remarks> operator iEqual(Error, Error) : Boolean; </remarks>
	public class ErrorEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((Exception)argument1).Message == ((Exception)argument2).Message;
		}
	}
	
	/// <remarks> operator iNotEqual(byte, byte) : bool </remarks>
    public class ByteNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (byte)argument1 != (byte)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(sbyte, sbyte) : bool </remarks>
    public class SByteNotEqualNode : BinaryInstructionNode
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
						(sbyte)AArgument1() !=
						(sbyte)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iNotEqual(short , short ) : bool </remarks>
    public class ShortNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (short)argument1 != (short)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(ushort , ushort ) : bool </remarks>
    public class UShortNotEqualNode : BinaryInstructionNode
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
						(ushort)AArgument1() !=
						(ushort)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iNotEqual(integer, integer) : bool </remarks>
    public class IntegerNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 != (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(uinteger, uinteger) : bool </remarks>
    public class UIntegerNotEqualNode : BinaryInstructionNode
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
						(uint)AArgument1() !=
						(uint)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iNotEqual(long, long) : bool </remarks>
    public class LongNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 != (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(ulong, ulong) : bool </remarks>
    public class ULongNotEqualNode : BinaryInstructionNode
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
						(ulong)AArgument1() !=
						(ulong)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iNotEqual(Guid, Guid) : bool </remarks>
    public class GuidNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (Guid)argument1 != (Guid)argument2;
		}
    }

    /// <remarks> operator iNotEqual(bool, bool) : bool </remarks>
    public class BooleanNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (bool)argument1 != (bool)argument2;
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iNotEqual(double, double) : bool </remarks>
    public class DoubleNotEqualNode : BinaryInstructionNode
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
						(double)AArgument1() !=
						(double)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iNotEqual(decimal, decimal) : bool </remarks>
    public class DecimalNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 != (decimal)argument2;
		}
    }

	/// <remarks> operator iNotEqual(string, string) : bool </remarks>
	public class StringNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return !String.Equals((string)argument1, (string)argument2, StringComparison.Ordinal);
		}
	}

	#if USEISTRING
	public class IStringNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, true) != 0;
		}
	}
	#endif

	/// <remarks> operator iNotEqual(money, money) : bool </remarks>
	public class MoneyNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 != (decimal)argument2;
		}
	}

	/// <remarks> operator iGreater(bool, bool) : bool </remarks>
    public class BooleanGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (bool)argument1 ? !(bool)argument2 : false;
		}
    }

	/// <remarks> operator iGreater(byte, byte) : bool </remarks>
    public class ByteGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (byte)argument1 > (byte)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(sbyte, sbyte) : bool </remarks>
    public class SByteGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (sbyte)AArgument1() > (sbyte)AArgument2;
		}
    }
    #endif

	/// <remarks> operator iGreater(short, short) : bool </remarks>
    public class ShortGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (short)argument1 > (short)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(ushort, ushort) : bool </remarks>
    public class UShortGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ushort)AArgument1() > (ushort)AArgument2;
		}
    }
    #endif

	/// <remarks> operator iGreater(integer, integer) : bool </remarks>
    public class IntegerGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 > (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(uinteger, uinteger) : bool </remarks>
    public class UIntegerGreaterNode : BinaryInstructionNode
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
						(uint)AArgument1() >
						(uint)AArgument2()
					);
		}
    }
    #endif

	/// <remarks> operator iGreater(long, long) : bool </remarks>
    public class LongGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 > (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(ulong, ulong) : bool </remarks>
    public class ULongGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ulong)AArgument1() > (ulong)AArgument2();
		}
    }
    #endif

	/// <remarks> operator iGreater(Guid, Guid) : bool </remarks>
    public class GuidGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((Guid)argument1).CompareTo((Guid)argument2) > 0;
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iGreater(double, double) : bool </remarks>
    public class DoubleGreaterNode : BinaryInstructionNode
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
						(double)AArgument1() >
						(double)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iGreater(decimal, decimal) : bool </remarks>
    public class DecimalGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 > (decimal)argument2;
		}
    }

	/// <remarks> operator iGreater(string, string) : bool </remarks>
	public class StringGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)argument1, (string)argument2, StringComparison.Ordinal) > 0;
		}
	}

	#if USEISTRING
	public class IStringGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, true) > 0;
		}
	}
	#endif

	/// <remarks> operator iGreater(money, money) : bool </remarks>
	public class MoneyGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((decimal)argument1).CompareTo((decimal)argument2) > 0;
		}
	}

	/// <remarks> operator iLess(bool, bool) : bool </remarks>
    public class BooleanLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return !(bool)argument1 ? (bool)argument2 : false;
		}
    }

    /// <remarks> operator iLess(byte, byte) : bool </remarks>
    public class ByteLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (byte)argument1 < (byte)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(sbyte, sbyte) : bool </remarks>
    public class SByteLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (sbyte)AArgument1() < (sbyte)AArgument2
		}
    }
    #endif

    /// <remarks> operator iLess(short, short) : bool </remarks>
    public class ShortLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (short)argument1 < (short)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(ushort, ushort) : bool </remarks>
    public class UShortLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ushort)AArgument1() < (ushort)AArgument2;
		}
    }
    #endif

    /// <remarks> operator iLess(integer, integer) : bool </remarks>
    public class IntegerLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 < (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(uinteger, uinteger) : bool </remarks>
    public class UIntegerLessNode : BinaryInstructionNode
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
						(uint)AArgument1() <
						(uint)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iLess(long, long) : bool </remarks>
    public class LongLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 < (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(ulong, ulong) : bool </remarks>
    public class ULongLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ulong)AArgument1() < (ulong)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iLess(Guid, Guid) : bool </remarks>
    public class GuidLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((Guid)argument1).CompareTo((Guid)argument2) < 0;
		}
    }
    
	#if USEDOUBLE
	/// <remarks> operator iLess(double, double) : bool </remarks>
    public class DoubleLessNode : BinaryInstructionNode
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
						(double)AArgument1() <
						(double)AArgument2()
					);
		}
    }
	#endif

    /// <remarks> operator iLess(decimal, decimal) : bool </remarks>
    public class DecimalLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 < (decimal)argument2;
		}
    }

	/// <remarks> operator iLess(string, string) : bool </remarks>
	public class StringLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)argument1, (string)argument2, StringComparison.Ordinal) < 0;
		}
	}

	#if USEISTRING
	public class IStringLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, true) < 0;
		}
	}
	#endif

	/// <remarks> operator iLess(money, money) : bool </remarks>
	public class MoneyLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((decimal)argument1).CompareTo((decimal)argument2) < 0;
		}
	}

	/// <remarks> operator iInclusiveGreater(bool, bool) : bool </remarks>
    public class BooleanInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				bool leftValue = (bool)argument1;
				bool rightValue = (bool)argument2;
				return leftValue == rightValue || leftValue ? true : false;
			}
		}
    }

    /// <remarks> operator iInclusiveGreater(byte, byte) : bool </remarks>
    public class ByteInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (byte)argument1 >= (byte)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(sbyte, sbyte) : bool </remarks>
    public class SByteInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (sbyte)AArgument1() >= (sbyte)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(short, short) : bool </remarks>
    public class ShortInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (short)argument1 >= (short)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(ushort, ushort) : bool </remarks>
    public class UShortInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ushort)AArgument1() >= (ushort)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(integer, integer) : bool </remarks>
    public class IntegerInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 >= (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(uinteger, uinteger) : bool </remarks>
    public class UIntegerInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (uint)AArgument1() >= (uint)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(long, long) : bool </remarks>
    public class LongInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 >= (long)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(ulong, ulong) : bool </remarks>
    public class ULongInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ulong)AArgument1() >= (ulong)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(Guid, Guid) : bool </remarks>
    public class GuidInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((Guid)argument1).CompareTo((Guid)argument2) >= 0;
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iInclusiveGreater(double, double) : bool </remarks>
    public class DoubleInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (double)AArgument1() >= (double)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(decimal, decimal) : bool </remarks>
    public class DecimalInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 >= (decimal)argument2;
		}
    }

	/// <remarks> operator iInclusiveGreater(string, string) : bool </remarks>
	public class StringInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)argument1, (string)argument2, StringComparison.Ordinal) >= 0;
		}
	}

	#if USEISTRING
	public class IStringInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, true) >= 0;
		}
	}
	#endif

	/// <remarks> operator iInclusiveGreater(money, money) : bool </remarks>
	public class MoneyInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((decimal)argument1).CompareTo((decimal)argument2) >= 0;
		}
	}

	/// <remarks> operator iInclusiveLess(bool, bool) : bool </remarks>
    public class BooleanInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				bool leftValue = (bool)argument1;
				bool rightValue = (bool)argument2;
				return leftValue == rightValue || !leftValue ? true : false;
			}
		}
    }

    /// <remarks> operator iInclusiveLess(byte, byte) : bool </remarks>
    public class ByteInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (byte)argument1 <= (byte)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveLess(sbyte, sbyte) : bool </remarks>
    public class SByteInclusiveLessNode : BinaryInstructionNode
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
						AProcess, 
						(sbyte)AArgument1() <=
						(sbyte)AArgument2()
					);
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(short, short) : bool </remarks>
    public class ShortInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (short)argument1 <= (short)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveLess(ushort, ushort) : bool </remarks>
    public class UShortInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ushort)AArgument1() <= (ushort)AArgument2;
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(integer, integer) : bool </remarks>
    public class IntegerInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (int)argument1 <= (int)argument2;
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveLess(uinteger, uinteger) : bool </remarks>
    public class UIntegerInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (uint)AArgument1() <= (uint)AArgument2;
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(long, long) : bool </remarks>
    public class LongInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (long)argument1 <= (long)argument2;
		}
    }

	#if UseUnsignedIntegers
    /// <remarks> operator iInclusiveLess(ulong, ulong) : bool </remarks>
    public class ULongInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (ulong)AArgument1() <= (ulong)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(Guid, Guid) : bool </remarks>
    public class GuidInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((Guid)argument1).CompareTo((Guid)argument2) <= 0;
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iInclusiveLess(double, double) : bool </remarks>
    public class DoubleInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (double)AArgument1() <= (double)AArgument2();
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(decimal, decimal) : bool </remarks>
    public class DecimalInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return (decimal)argument1 <= (decimal)argument2;
		}
    }

	/// <remarks> operator iInclusiveLess(string, string) : bool </remarks>
	public class StringInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)argument1, (string)argument2, StringComparison.Ordinal) <= 0;
		}
	}
    
	#if USEISTRING
	public class IStringInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, true) <= 0;
		}
	}
	#endif
    
	/// <remarks> operator iInclusiveLess(money, money) : bool </remarks>
	public class MoneyInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
				return ((decimal)argument1).CompareTo((decimal)argument2) <= 0;
		}
	}
    
	/// <remarks> operator iCompare(bool, bool) : integer </remarks>
    public class BooleanCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				bool leftValue = (bool)argument1;
				bool rightValue = (bool)argument2;
				return leftValue == rightValue ? 0 : leftValue ? 1 : -1;
			}
		}
    }

	/// <remarks> operator iCompare(byte, byte) : integer </remarks>
    public class ByteCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				byte leftValue = (byte)argument1;
				byte rightValue = (byte)argument2;
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(sbyte, sbyte) : integer </remarks>
    public class SByteCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				sbyte LLeftValue = (sbyte)AArgument1();
				sbyte LRightValue = (sbyte)AArgument2();
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(short, short) : integer </remarks>
    public class ShortCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				short leftValue = (short)argument1;
				short rightValue = (short)argument2;
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(ushort, ushort) : integer </remarks>
    public class UShortCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				ushort LLeftValue = (ushort)AArgument1();
				ushort LRightValue = (ushort)AArgument2();
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(int, int) : integer </remarks>
    public class IntegerCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				int leftValue = (int)argument1;
				int rightValue = (int)argument2;
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(uint, uint) : integer </remarks>
    public class UIntegerCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				uint LLeftValue = (uint)AArgument1();
				uint LRightValue = (uint)AArgument2();
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(long, long) : integer </remarks>
    public class LongCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				long leftValue = (long)argument1;
				long rightValue = (long)argument2;
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(ulong, ulong) : integer </remarks>
    public class ULongCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				ulong LLeftValue = (ulong)AArgument1();
				ulong LRightValue = (ulong)AArgument2();
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(Guid, Guid) : integer </remarks>
    public class GuidCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				Guid leftValue = (Guid)argument1;
				Guid rightValue = (Guid)argument2;
				return leftValue.CompareTo(rightValue);
			}
		}
    }

	/// <remarks> operator iCompare(decimal, decimal) : integer </remarks>
    public class DecimalCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				decimal leftValue = (decimal)argument1;
				decimal rightValue = (decimal)argument2;
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
    }

	/// <remarks> operator iCompare(money, money) : integer </remarks>
    public class MoneyCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				decimal leftValue = (decimal)argument1;
				decimal rightValue = (decimal)argument2;
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
    }

	/// <remarks> operator iCompare(string, string) : integer </remarks>
    public class StringCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				string leftValue = (string)argument1;
				string rightValue = (string)argument2;
				return String.Compare(leftValue, rightValue, StringComparison.Ordinal);
			}
		}
    }

	#if USEISTRING
    public class IStringCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				string LLeftValue = (string)AArgument1;
				string LRightValue = (string)AArgument2;
				return String.Compare(LLeftValue, LRightValue, true);
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(TimeSpan, TimeSpan) : integer </remarks>
	public class TimeSpanCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				TimeSpan leftValue = ((TimeSpan)argument1);
				TimeSpan rightValue = ((TimeSpan)argument2);
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
	}

	public class DateTimeCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
	}

	public class DateCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
	}

	public class TimeCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			else
			#endif
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue == rightValue ? 0 : leftValue > rightValue ? 1 : -1;
			}
		}
	}

	/// <remarks> operator Max(byte, byte) : byte </remarks>
	public class ByteMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (byte)argument2;
			else if (argument2 == null)
				return (byte)argument1;
			else
			{
				byte leftValue = (byte)argument1;
				byte rightValue = (byte)argument2;
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Short, Short) : Short </remarks>
	public class ShortMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (short)argument2;
			else if (argument2 == null)
				return (short)argument1;
			else
			{
				short leftValue = (short)argument1;
				short rightValue = (short)argument2;
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Integer, Integer) : Integer </remarks>
	public class IntegerMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (int)argument2;
			else if (argument2 == null)
				return (int)argument1;
			else
			{
				int leftValue = (int)argument1;
				int rightValue = (int)argument2;
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Long, Long) : Long </remarks>
	public class LongMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (long)argument2;
			else if (argument2 == null)
				return (long)argument1;
			else
			{
				long leftValue = (long)argument1;
				long rightValue = (long)argument2;
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Decimal, Decimal) : Decimal </remarks>
	public class DecimalMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (decimal)argument2;
			else if (argument2 == null)
				return (decimal)argument1;
			else
			{
				decimal leftValue = (decimal)argument1;
				decimal rightValue = (decimal)argument2;
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(TimeSpan, TimeSpan) : TimeSpan </remarks>
	public class TimeSpanMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((TimeSpan)argument2);
			else if (argument2 == null)
				return ((TimeSpan)argument1);
			else
			{
				TimeSpan leftValue = ((TimeSpan)argument1);
				TimeSpan rightValue = ((TimeSpan)argument2);
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(DateTime, DateTime) : DateTime </remarks>
	public class DateTimeMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((DateTime)argument2);
			else if (argument2 == null)
				return ((DateTime)argument1);
			else
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Date, Date) : Date </remarks>
	public class DateMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((DateTime)argument2);
			else if (argument2 == null)
				return ((DateTime)argument1);
			else
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Time, Time) : Time </remarks>
	public class TimeMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((DateTime)argument2);
			else if (argument2 == null)
				return ((DateTime)argument1);
			else
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Max(Money, Money) : Money </remarks>
	public class MoneyMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (decimal)argument2;
			else if (argument2 == null)
				return (decimal)argument1;
			else
			{
				decimal leftValue = (decimal)argument1;
				decimal rightValue = (decimal)argument2;
				return leftValue > rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(byte, byte) : byte </remarks>
	public class ByteMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (byte)argument2;
			else if (argument2 == null)
				return (byte)argument1;
			else
			{
				byte leftValue = (byte)argument1;
				byte rightValue = (byte)argument2;
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Short, Short) : Short </remarks>
	public class ShortMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (short)argument2;
			else if (argument2 == null)
				return (short)argument1;
			else
			{
				short leftValue = (short)argument1;
				short rightValue = (short)argument2;
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Integer, Integer) : Integer </remarks>
	public class IntegerMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (int)argument2;
			else if (argument2 == null)
				return (int)argument1;
			else
			{
				int leftValue = (int)argument1;
				int rightValue = (int)argument2;
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Long, Long) : Long </remarks>
	public class LongMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (long)argument2;
			else if (argument2 == null)
				return (long)argument1;
			else
			{
				long leftValue = (long)argument1;
				long rightValue = (long)argument2;
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Decimal, Decimal) : Decimal </remarks>
	public class DecimalMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (decimal)argument2;
			else if (argument2 == null)
				return (decimal)argument1;
			else
			{
				decimal leftValue = (decimal)argument1;
				decimal rightValue = (decimal)argument2;
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(DateTime, DateTime) : DateTime </remarks>
	public class DateTimeMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((DateTime)argument2);
			else if (argument2 == null)
				return ((DateTime)argument1);
			else
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(TimeSpan, TimeSpan) : TimeSpan </remarks>
	public class TimeSpanMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((TimeSpan)argument2);
			else if (argument2 == null)
				return ((TimeSpan)argument1);
			else
			{
				TimeSpan leftValue = ((TimeSpan)argument1);
				TimeSpan rightValue = ((TimeSpan)argument2);
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Date, Date) : Date </remarks>
	public class DateMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((DateTime)argument2);
			else if (argument2 == null)
				return ((DateTime)argument1);
			else
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Time, Time) : Time </remarks>
	public class TimeMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return ((DateTime)argument2);
			else if (argument2 == null)
				return ((DateTime)argument1);
			else
			{
				DateTime leftValue = ((DateTime)argument1);
				DateTime rightValue = ((DateTime)argument2);
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator Min(Money, Money) : Money </remarks>
	public class MoneyMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if (argument1 == null)
				if (argument2 == null)
					return null;
				else
					return (decimal)argument2;
			else if (argument2 == null)
				return (decimal)argument1;
			else
			{
				decimal leftValue = (decimal)argument1;
				decimal rightValue = (decimal)argument2;
				return leftValue < rightValue ? leftValue : rightValue;
			}
		}
	}

	/// <remarks> operator iBetween(boolean, boolean, boolean) : integer </remarks>
    public class BooleanBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				bool tempValue = (bool)argument1;
				bool lowerBound = (bool)argument2;
				bool upperBound = (bool)argument3;
				return (tempValue || (tempValue == lowerBound)) && (!tempValue || (tempValue == upperBound));
			}
		}
    }

	/// <remarks> operator iBetween(byte, byte, byte) : integer </remarks>
    public class ByteBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				byte tempValue = (byte)argument1;
				byte lowerBound = (byte)argument2;
				byte upperBound = (byte)argument3;
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(sbyte, sbyte, sbyte) : integer </remarks>
    public class SByteBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				sbyte LValue = (sbyte)AArgument1();
				sbyte LLowerBound = (sbyte)AArgument2();
				sbyte LUpperBound = (sbyte)AArgument3();
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(short, short, short) : integer </remarks>
    public class ShortBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				short tempValue = (short)argument1;
				short lowerBound = (short)argument2;
				short upperBound = (short)argument3;
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(ushort, ushort, ushort) : integer </remarks>
    public class UShortBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				ushort LValue = (ushort)AArgument1();
				ushort LLowerBound = (ushort)AArgument2();
				ushort LUpperBound = (ushort)AArgument3();
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(integer, integer, integer) : integer </remarks>
    public class IntegerBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				int tempValue = (int)argument1;
				int lowerBound = (int)argument2;
				int upperBound = (int)argument3;
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(uinteger, uinteger, uinteger) : integer </remarks>
    public class UIntegerBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				uint LValue = (uint)AArgument1();
				uint LLowerBound = (uint)AArgument2();
				uint LUpperBound = (uint)AArgument3();
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(long, long, long) : integer </remarks>
    public class LongBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				long tempValue = (long)argument1;
				long lowerBound = (long)argument2;
				long upperBound = (long)argument3;
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(ulong, ulong, ulong) : integer </remarks>
    public class ULongBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				ulong LValue = (ulong)AArgument1();
				ulong LLowerBound = (ulong)AArgument2();
				ulong LUpperBound = (ulong)AArgument3();
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(Guid, Guid, Guid) : integer </remarks>
    public class GuidBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				Guid tempValue = (Guid)argument1;
				Guid lowerBound = (Guid)argument2;
				Guid upperBound = (Guid)argument3;
				return (tempValue.CompareTo(lowerBound) >= 0) && (tempValue.CompareTo(upperBound) <= 0);
			}
		}
    }

	/// <remarks> operator iBetween(decimal, decimal, decimal) : integer </remarks>
    public class DecimalBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				decimal tempValue = (decimal)argument1;
				decimal lowerBound = (decimal)argument2;
				decimal upperBound = (decimal)argument3;
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }

	/// <remarks> operator iBetween(money, money, money) : integer </remarks>
    public class MoneyBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				decimal tempValue = (decimal)argument1;
				decimal lowerBound = (decimal)argument2;
				decimal upperBound = (decimal)argument3;
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }

	/// <remarks> operator iBetween(string, string, string) : integer </remarks>
    public class StringBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				string tempValue = (string)argument1;
				string lowerBound = (string)argument2;
				string upperBound = (string)argument3;
				return (String.Compare(tempValue, lowerBound, StringComparison.Ordinal) >= 0) && (String.Compare(tempValue, upperBound, StringComparison.Ordinal) <= 0);
			}
		}
    }

	#if USEISTRING
    public class IStringBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				string LValue = (string)AArgument1;
				string LLowerBound = (string)AArgument2;
				string LUpperBound = (string)AArgument3;
				return (String.Compare(LValue, LLowerBound, true) >= 0) && (String.Compare(LValue, LUpperBound, true) <= 0);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(TimeSpan, TimeSpan, TimeSpan) : integer </remarks>
    public class TimeSpanBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null) || (argument3 == null))
				return null;
			else
			#endif
			{
				TimeSpan tempValue = ((TimeSpan)argument1);
				TimeSpan lowerBound = ((TimeSpan)argument2);
				TimeSpan upperBound = ((TimeSpan)argument3);
				return (tempValue >= lowerBound) && (tempValue <= upperBound);
			}
		}
    }
}
