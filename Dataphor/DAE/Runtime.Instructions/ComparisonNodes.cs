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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				throw new RuntimeException(RuntimeException.Codes.ValueEncountered, AProgram.GetCurrentLocation());
		}
    }
    
	/// <remarks> operator iEqual(byte, byte) : bool </remarks>
    public class ByteEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (byte)AArgument1 == (byte)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (short)AArgument1 == (short)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (int)AArgument1 == (int)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (long)AArgument1 == (long)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (Guid)AArgument1 == (Guid)AArgument2;
		}
    }
    
    /// <remarks> operator iEqual(bool, bool) : bool </remarks>
    public class BooleanEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (bool)AArgument1 == (bool)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 == (decimal)AArgument2;
		}
    }

	/// <remarks> operator iEqual(string, string) : bool </remarks>
	public class StringEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, false) == 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 == (decimal)AArgument2;
		}
	}
	
	/// <remarks> operator iEqual(Error, Error) : Boolean; </remarks>
	public class ErrorEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((Exception)AArgument1).Message == ((Exception)AArgument2).Message;
		}
	}
	
	/// <remarks> operator iNotEqual(byte, byte) : bool </remarks>
    public class ByteNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (byte)AArgument1 != (byte)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (short)AArgument1 != (short)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (int)AArgument1 != (int)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (long)AArgument1 != (long)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (Guid)AArgument1 != (Guid)AArgument2;
		}
    }

    /// <remarks> operator iNotEqual(bool, bool) : bool </remarks>
    public class BooleanNotEqualNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (bool)AArgument1 != (bool)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 != (decimal)AArgument2;
		}
    }

	/// <remarks> operator iNotEqual(string, string) : bool </remarks>
	public class StringNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, false) != 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 != (decimal)AArgument2;
		}
	}

	/// <remarks> operator iGreater(bool, bool) : bool </remarks>
    public class BooleanGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (bool)AArgument1 ? !(bool)AArgument2 : false;
		}
    }

	/// <remarks> operator iGreater(byte, byte) : bool </remarks>
    public class ByteGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (byte)AArgument1 > (byte)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (short)AArgument1 > (short)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (int)AArgument1 > (int)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (long)AArgument1 > (long)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((Guid)AArgument1).CompareTo((Guid)AArgument2) > 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 > (decimal)AArgument2;
		}
    }

	/// <remarks> operator iGreater(string, string) : bool </remarks>
	public class StringGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, false) > 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((decimal)AArgument1).CompareTo((decimal)AArgument2) > 0;
		}
	}

	/// <remarks> operator iLess(bool, bool) : bool </remarks>
    public class BooleanLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return !(bool)AArgument1 ? (bool)AArgument2 : false;
		}
    }

    /// <remarks> operator iLess(byte, byte) : bool </remarks>
    public class ByteLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (byte)AArgument1 < (byte)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (short)AArgument1 < (short)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (int)AArgument1 < (int)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (long)AArgument1 < (long)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((Guid)AArgument1).CompareTo((Guid)AArgument2) < 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 < (decimal)AArgument2;
		}
    }

	/// <remarks> operator iLess(string, string) : bool </remarks>
	public class StringLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, false) < 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((decimal)AArgument1).CompareTo((decimal)AArgument2) < 0;
		}
	}

	/// <remarks> operator iInclusiveGreater(bool, bool) : bool </remarks>
    public class BooleanInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				bool LLeftValue = (bool)AArgument1;
				bool LRightValue = (bool)AArgument2;
				return LLeftValue == LRightValue || LLeftValue ? true : false;
			}
		}
    }

    /// <remarks> operator iInclusiveGreater(byte, byte) : bool </remarks>
    public class ByteInclusiveGreaterNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (byte)AArgument1 >= (byte)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (short)AArgument1 >= (short)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (int)AArgument1 >= (int)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (long)AArgument1 >= (long)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((Guid)AArgument1).CompareTo((Guid)AArgument2) >= 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 >= (decimal)AArgument2;
		}
    }

	/// <remarks> operator iInclusiveGreater(string, string) : bool </remarks>
	public class StringInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, false) >= 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((decimal)AArgument1).CompareTo((decimal)AArgument2) >= 0;
		}
	}

	/// <remarks> operator iInclusiveLess(bool, bool) : bool </remarks>
    public class BooleanInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				bool LLeftValue = (bool)AArgument1;
				bool LRightValue = (bool)AArgument2;
				return LLeftValue == LRightValue || !LLeftValue ? true : false;
			}
		}
    }

    /// <remarks> operator iInclusiveLess(byte, byte) : bool </remarks>
    public class ByteInclusiveLessNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (byte)AArgument1 <= (byte)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (short)AArgument1 <= (short)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (int)AArgument1 <= (int)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (long)AArgument1 <= (long)AArgument2;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((Guid)AArgument1).CompareTo((Guid)AArgument2) <= 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return (decimal)AArgument1 <= (decimal)AArgument2;
		}
    }

	/// <remarks> operator iInclusiveLess(string, string) : bool </remarks>
	public class StringInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return String.Compare((string)AArgument1, (string)AArgument2, false) <= 0;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
				return ((decimal)AArgument1).CompareTo((decimal)AArgument2) <= 0;
		}
	}
    
	/// <remarks> operator iCompare(bool, bool) : integer </remarks>
    public class BooleanCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				bool LLeftValue = (bool)AArgument1;
				bool LRightValue = (bool)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue ? 1 : -1;
			}
		}
    }

	/// <remarks> operator iCompare(byte, byte) : integer </remarks>
    public class ByteCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				byte LLeftValue = (byte)AArgument1;
				byte LRightValue = (byte)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				short LLeftValue = (short)AArgument1;
				short LRightValue = (short)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				int LLeftValue = (int)AArgument1;
				int LRightValue = (int)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				long LLeftValue = (long)AArgument1;
				long LRightValue = (long)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				Guid LLeftValue = (Guid)AArgument1;
				Guid LRightValue = (Guid)AArgument2;
				return LLeftValue.CompareTo(LRightValue);
			}
		}
    }

	/// <remarks> operator iCompare(decimal, decimal) : integer </remarks>
    public class DecimalCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				decimal LLeftValue = (decimal)AArgument1;
				decimal LRightValue = (decimal)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
    }

	/// <remarks> operator iCompare(money, money) : integer </remarks>
    public class MoneyCompareNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				decimal LLeftValue = (decimal)AArgument1;
				decimal LRightValue = (decimal)AArgument2;
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
    }

	/// <remarks> operator iCompare(string, string) : integer </remarks>
    public class StringCompareNode : BinaryInstructionNode
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
				return String.Compare(LLeftValue, LRightValue);
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				TimeSpan LLeftValue = ((TimeSpan)AArgument1);
				TimeSpan LRightValue = ((TimeSpan)AArgument2);
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
	}

	public class DateTimeCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
	}

	public class DateCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
	}

	public class TimeCompareNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			else
			#endif
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1;
			}
		}
	}

	/// <remarks> operator Max(byte, byte) : byte </remarks>
	public class ByteMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (byte)AArgument2;
			else if (AArgument2 == null)
				return (byte)AArgument1;
			else
			{
				byte LLeftValue = (byte)AArgument1;
				byte LRightValue = (byte)AArgument2;
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Short, Short) : Short </remarks>
	public class ShortMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (short)AArgument2;
			else if (AArgument2 == null)
				return (short)AArgument1;
			else
			{
				short LLeftValue = (short)AArgument1;
				short LRightValue = (short)AArgument2;
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Integer, Integer) : Integer </remarks>
	public class IntegerMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (int)AArgument2;
			else if (AArgument2 == null)
				return (int)AArgument1;
			else
			{
				int LLeftValue = (int)AArgument1;
				int LRightValue = (int)AArgument2;
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Long, Long) : Long </remarks>
	public class LongMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (long)AArgument2;
			else if (AArgument2 == null)
				return (long)AArgument1;
			else
			{
				long LLeftValue = (long)AArgument1;
				long LRightValue = (long)AArgument2;
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Decimal, Decimal) : Decimal </remarks>
	public class DecimalMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (decimal)AArgument2;
			else if (AArgument2 == null)
				return (decimal)AArgument1;
			else
			{
				decimal LLeftValue = (decimal)AArgument1;
				decimal LRightValue = (decimal)AArgument2;
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(TimeSpan, TimeSpan) : TimeSpan </remarks>
	public class TimeSpanMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((TimeSpan)AArgument2);
			else if (AArgument2 == null)
				return ((TimeSpan)AArgument1);
			else
			{
				TimeSpan LLeftValue = ((TimeSpan)AArgument1);
				TimeSpan LRightValue = ((TimeSpan)AArgument2);
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(DateTime, DateTime) : DateTime </remarks>
	public class DateTimeMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((DateTime)AArgument2);
			else if (AArgument2 == null)
				return ((DateTime)AArgument1);
			else
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Date, Date) : Date </remarks>
	public class DateMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((DateTime)AArgument2);
			else if (AArgument2 == null)
				return ((DateTime)AArgument1);
			else
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Time, Time) : Time </remarks>
	public class TimeMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((DateTime)AArgument2);
			else if (AArgument2 == null)
				return ((DateTime)AArgument1);
			else
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Max(Money, Money) : Money </remarks>
	public class MoneyMaxNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (decimal)AArgument2;
			else if (AArgument2 == null)
				return (decimal)AArgument1;
			else
			{
				decimal LLeftValue = (decimal)AArgument1;
				decimal LRightValue = (decimal)AArgument2;
				return LLeftValue > LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(byte, byte) : byte </remarks>
	public class ByteMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (byte)AArgument2;
			else if (AArgument2 == null)
				return (byte)AArgument1;
			else
			{
				byte LLeftValue = (byte)AArgument1;
				byte LRightValue = (byte)AArgument2;
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Short, Short) : Short </remarks>
	public class ShortMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (short)AArgument2;
			else if (AArgument2 == null)
				return (short)AArgument1;
			else
			{
				short LLeftValue = (short)AArgument1;
				short LRightValue = (short)AArgument2;
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Integer, Integer) : Integer </remarks>
	public class IntegerMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (int)AArgument2;
			else if (AArgument2 == null)
				return (int)AArgument1;
			else
			{
				int LLeftValue = (int)AArgument1;
				int LRightValue = (int)AArgument2;
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Long, Long) : Long </remarks>
	public class LongMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (long)AArgument2;
			else if (AArgument2 == null)
				return (long)AArgument1;
			else
			{
				long LLeftValue = (long)AArgument1;
				long LRightValue = (long)AArgument2;
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Decimal, Decimal) : Decimal </remarks>
	public class DecimalMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (decimal)AArgument2;
			else if (AArgument2 == null)
				return (decimal)AArgument1;
			else
			{
				decimal LLeftValue = (decimal)AArgument1;
				decimal LRightValue = (decimal)AArgument2;
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(DateTime, DateTime) : DateTime </remarks>
	public class DateTimeMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((DateTime)AArgument2);
			else if (AArgument2 == null)
				return ((DateTime)AArgument1);
			else
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(TimeSpan, TimeSpan) : TimeSpan </remarks>
	public class TimeSpanMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((TimeSpan)AArgument2);
			else if (AArgument2 == null)
				return ((TimeSpan)AArgument1);
			else
			{
				TimeSpan LLeftValue = ((TimeSpan)AArgument1);
				TimeSpan LRightValue = ((TimeSpan)AArgument2);
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Date, Date) : Date </remarks>
	public class DateMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((DateTime)AArgument2);
			else if (AArgument2 == null)
				return ((DateTime)AArgument1);
			else
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Time, Time) : Time </remarks>
	public class TimeMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return ((DateTime)AArgument2);
			else if (AArgument2 == null)
				return ((DateTime)AArgument1);
			else
			{
				DateTime LLeftValue = ((DateTime)AArgument1);
				DateTime LRightValue = ((DateTime)AArgument2);
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator Min(Money, Money) : Money </remarks>
	public class MoneyMinNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			if (AArgument1 == null)
				if (AArgument2 == null)
					return null;
				else
					return (decimal)AArgument2;
			else if (AArgument2 == null)
				return (decimal)AArgument1;
			else
			{
				decimal LLeftValue = (decimal)AArgument1;
				decimal LRightValue = (decimal)AArgument2;
				return LLeftValue < LRightValue ? LLeftValue : LRightValue;
			}
		}
	}

	/// <remarks> operator iBetween(boolean, boolean, boolean) : integer </remarks>
    public class BooleanBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				bool LValue = (bool)AArgument1;
				bool LLowerBound = (bool)AArgument2;
				bool LUpperBound = (bool)AArgument3;
				return (LValue || (LValue == LLowerBound)) && (!LValue || (LValue == LUpperBound));
			}
		}
    }

	/// <remarks> operator iBetween(byte, byte, byte) : integer </remarks>
    public class ByteBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				byte LValue = (byte)AArgument1;
				byte LLowerBound = (byte)AArgument2;
				byte LUpperBound = (byte)AArgument3;
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				short LValue = (short)AArgument1;
				short LLowerBound = (short)AArgument2;
				short LUpperBound = (short)AArgument3;
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				int LValue = (int)AArgument1;
				int LLowerBound = (int)AArgument2;
				int LUpperBound = (int)AArgument3;
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				long LValue = (long)AArgument1;
				long LLowerBound = (long)AArgument2;
				long LUpperBound = (long)AArgument3;
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				Guid LValue = (Guid)AArgument1;
				Guid LLowerBound = (Guid)AArgument2;
				Guid LUpperBound = (Guid)AArgument3;
				return (LValue.CompareTo(LLowerBound) >= 0) && (LValue.CompareTo(LUpperBound) <= 0);
			}
		}
    }

	/// <remarks> operator iBetween(decimal, decimal, decimal) : integer </remarks>
    public class DecimalBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				decimal LValue = (decimal)AArgument1;
				decimal LLowerBound = (decimal)AArgument2;
				decimal LUpperBound = (decimal)AArgument3;
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }

	/// <remarks> operator iBetween(money, money, money) : integer </remarks>
    public class MoneyBetweenNode : TernaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				decimal LValue = (decimal)AArgument1;
				decimal LLowerBound = (decimal)AArgument2;
				decimal LUpperBound = (decimal)AArgument3;
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }

	/// <remarks> operator iBetween(string, string, string) : integer </remarks>
    public class StringBetweenNode : TernaryInstructionNode
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
				return (String.Compare(LValue, LLowerBound, false) >= 0) && (String.Compare(LValue, LUpperBound, false) <= 0);
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null) || (AArgument3 == null))
				return null;
			else
			#endif
			{
				TimeSpan LValue = ((TimeSpan)AArgument1);
				TimeSpan LLowerBound = ((TimeSpan)AArgument2);
				TimeSpan LUpperBound = ((TimeSpan)AArgument3);
				return (LValue >= LLowerBound) && (LValue <= LUpperBound);
			}
		}
    }
}
