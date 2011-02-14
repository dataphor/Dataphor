/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;

	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	/*
		Conversion Operators ->
				\ B \ B \ S \ S \ U \ I \ U \ L \ U \ D \ S \ G \ D \ T \ M \
				 \ o \ y \ B \ h \ S \ n \ I \ o \ L \ e \ t \ u \ a \ i \ o \
				  \ o \ t \ y \ o \ h \ t \ n \ n \ o \ c \ r \ i \ t \ m \ n \
				   \ l \ e \ t \ r \ o \ e \ t \ g \ n \ i \ i \ d \ e \ e \ e \
				    \ e \   \ e \ t \ r \ g \ e \   \ g \ m \ n \   \ T \ S \ y \ 
				     \ a \   \   \   \ t \ e \ g \   \   \ a \ g \   \ i \ p \   \
				      \ n \   \   \   \   \ r \ e \   \   \ l \   \   \ m \ a \   \
				       \   \   \   \   \   \   \ r \   \   \   \   \   \ e \ n \   \
						|	|	|	|	|	|	|	|	|	|	|	|	|	|	|	|			
			Boolean		| X	| E	| E	| E | E	| E	| E	| E	| E	| .	| E	| . | . | .	| .	|
			Byte		| E	| X	| E	| E	| E	| E	| E	| E	| E	| .	| E	| .	| .	| .	| .	|
			SByte		| E	| E	| X | E	| E	| E	| E	| E	| E	| .	| E	| .	| .	| .	| .	|
			Short		| E	| E	| E	| X | E	| E	| E	| E	| E	| .	| E	| .	| .	| .	| .	|
			UShort		| E	| E	| E	| E	| X	| E	| E	| E	| E	| .	| E	| .	| .	| .	| .	|
			Integer		| E	| E	| E	| E	| E	| X	| E	| E	| E	| E	| E	| .	| .	| . | E	|
			UInteger	| E	| E	| E	| E	| E	| E	| X	| E	| E	| E	| E	| .	| .	| .	| E	|
			Long		| E	| E	| E	| E	| E	| E	| E	| X	| E	| E	| E	| .	| .	| .	| E	|
			ULong		| E	| E	| E	| E	| E	| E	| E	| E	| X	| E	| E	| .	| .	| .	| E	|
			Decimal		| .	| .	| .	| .	| .	| E	| E	| E	| E	| X	| E	| .	| .	| .	| E	|
			String		| E	| E	| E	| E	| E	| E	| E	| E	| E	| E	| X	| E	| E	| E	| E	|
			Guid		| .	| .	| .	| .	| .	| .	| .	| .	| .	| .	| E	| X	| .	| .	| .	|
			DateTime	| .	| .	| .	| .	| .	| .	| .	| .	| .	| .	| E	| .	| X	| .	| .	|
			TimeSpan	| .	| .	| .	| .	| .	| .	| .	| .	| .	| .	| E	| .	| .	| X	| .	|
			Money		| . | .	| .	| .	| .	| E	| E	| E	| E	| E	| E	| .	| .	| .	| X	|

			Legend ->
				. - Not Supported
				I - Implicit Conversion
				E - Explicit Conversion Required
				X - Not Applicable			
			
	*/
	
	/// <remarks> operator ToString(System.Generic) : System.String </remarks>
	public class ObjectToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return argument1.ToString();
		}
	}
	
	/// <remarks> operator BooleanToString(System.Boolean) : System.String </remarks>
	public class BooleanToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (bool)argument1 ? Keywords.True : Keywords.False;
		}
	}
	
	/// <remarks> operator StringToBoolean(System.String) : System.Boolean </remarks>
	public class StringToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToBoolean((string)argument1);
		}
	}
	
	/// <remarks> operator ByteToString(System.Byte) : System.String </remarks>
	public class ByteToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((byte)argument1).ToString();
		}
	}
	
	/// <remarks> operator StringToByte(System.String) : System.Byte </remarks>
	public class StringToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToByte((string)argument1);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator SByteToString(System.SByte) : System.String </remarks>
	public class SByteToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((sbyte)AArgument1).ToString();
		}
	}
	
	/// <remarks> operator StringToSByte(System.String) : System.SByte </remarks>
	public class StringToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToSByte((string)AArgument1);
		}
	}
	#endif
	
	/// <remarks> operator ShortToString(System.Short) : System.String </remarks>
	public class ShortToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((short)argument1).ToString();
		}
	}
	
	/// <remarks> operator StringToShort(System.String) : System.Short </remarks>
	public class StringToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToInt16((string)argument1);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UShortToString(System.UShort) : System.String </remarks>
	public class UShortToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((ushort)AArgument1).ToString();
		}
	}
	
	/// <remarks> operator StringToUShort(System.String) : System.UShort </remarks>
	public class StringToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToUInt16((string)AArgument1);
		}
	}
	#endif
	
	/// <remarks> operator IntegerToString(System.Integer) : System.String </remarks>
	public class IntegerToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((int)argument1).ToString();
		}
	}
	
	/// <remarks> operator StringToInteger(System.String) : System.Integer </remarks>
	public class StringToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToInt32((string)argument1);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UIntegerToString(System.UInteger) : System.String </remarks>
	public class UIntegerToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((uint)AArgument1).ToString();
		}
	}
	
	/// <remarks> operator StringToUInteger(System.String) : System.UInteger </remarks>
	public class StringToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToUInt32((string)AArgument1);
		}
	}
	#endif
	
	/// <remarks> operator LongToString(System.Long) : System.String </remarks>
	public class LongToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((long)argument1).ToString();
		}
	}
	
	/// <remarks> operator StringToLong(System.String) : System.Long </remarks>
	public class StringToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToInt64((string)argument1);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator ULongToString(System.ULong) : System.String </remarks>
	public class ULongToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((ulong)AArgument1).ToString();
		}
	}
	
	/// <remarks> operator StringToULong(System.String) : System.ULong </remarks>
	public class StringToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToUInt64((string)AArgument1);
		}
	}
	#endif
	
    /// <remarks> operator ToString(AGuid : Guid) : string </remarks>
    public class GuidToStringNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((Guid)argument1).ToString();
		}
    }

    /// <remarks> operator ToGuid(AString : string) : Guid </remarks>
	public class StringToGuidNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return new Guid((string)argument1);
		}
	}

	/// <remarks> operator ByteToBoolean(System.Byte) : System.Boolean </remarks>
	public class ByteToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (byte)argument1 == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToByte(System.Boolean) : System.Byte </remarks>
	public class BooleanToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (byte)((bool)argument1 ? 1 : 0);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator SByteToBoolean(System.SByte) : System.Boolean </remarks>
	public class SByteToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)AArgument1() == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToSByte(System.Boolean) : System.SByte </remarks>
	public class BooleanToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)((bool)AArgument1 ? 1 : 0);
		}
	}
	#endif
	
	/// <remarks> operator ShortToBoolean(System.Short) : System.Boolean </remarks>
	public class ShortToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (short)argument1 == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToShort(System.Boolean) : System.Short </remarks>
	public class BooleanToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (short)((bool)argument1 ? 1 : 0);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UShortToBoolean(System.UShort) : System.Boolean </remarks>
	public class UShortToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)AArgument1() == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToUShort(System.Boolean) : System.UShort </remarks>
	public class BooleanToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)((bool)AArgument1 ? 1 : 0);
		}
	}
	#endif
	
	/// <remarks> operator IntegerToBoolean(System.Integer) : System.Boolean </remarks>
	public class IntegerToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (int)argument1 == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToInteger(System.Boolean) : System.Integer </remarks>
	public class BooleanToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (bool)argument1 ? 1 : 0;
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UIntegerToBoolean(System.UInteger) : System.Boolean </remarks>
	public class UIntegerToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)AArgument1() == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToUInteger(System.Boolean) : System.UInteger </remarks>
	public class BooleanToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)((bool)AArgument1 ? 1 : 0);
		}
	}
	#endif
	
	/// <remarks> operator LongToBoolean(System.Long) : System.Boolean </remarks>
	public class LongToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (long)argument1 == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToLong(System.Boolean) : System.Long </remarks>
	public class BooleanToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (long)((bool)argument1 ? 1 : 0);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator ULongToBoolean(System.ULong) : System.Boolean </remarks>
	public class ULongToBooleanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)AArgument1() == 0 ? false : true;
		}
	}
	
	/// <remarks> operator BooleanToULong(System.Boolean) : System.ULong </remarks>
	public class BooleanToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)((bool)AArgument1 ? 1 : 0);
		}
	}
	#endif

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToSByte(System.Byte) : System.SByte </remarks>
	public class ByteToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(byte)AArgument1;
		}
	}
	
	/// <remarks> operator SByteToByte(System.SByte) : System.Byte </remarks>
	public class SByteToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (byte)(sbyte)AArgument1;
		}
	}
	#endif
	
	/// <remarks> operator ByteToShort(System.Byte) : System.Short </remarks>
	public class ByteToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (short)(byte)argument1;
		}
	}
	
	/// <remarks> operator ShortToByte(System.Short) : System.Byte </remarks>
	public class ShortToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				short tempValue = (short)argument1;
				if ((tempValue < Byte.MinValue) || (tempValue > Byte.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Byte values", tempValue.ToString()));
				return (byte)tempValue;
			}			
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToUShort(System.Byte) : System.UShort </remarks>
	public class ByteToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)(byte)AArgument1;
		}
	}
	
	/// <remarks> operator UShortToByte(System.UShort) : System.Byte </remarks>
	public class UShortToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (byte)(ushort)AArgument1;
		}
	}
	#endif

	/// <remarks> operator ByteToInteger(System.Byte) : System.Integer </remarks>
	public class ByteToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (int)(byte)argument1;
		}
	}
	
	/// <remarks> operator IntegerToByte(System.Integer) : System.Byte </remarks>
	public class IntegerToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				int tempValue = (int)argument1;
				if ((tempValue < Byte.MinValue) || (tempValue > Byte.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Byte values", tempValue.ToString()));
				return (byte)tempValue;
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToUInteger(System.Byte) : System.UInteger </remarks>
	public class ByteToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(byte)AArgument1;
		}
	}
	
	/// <remarks> operator UIntegerToByte(System.UInteger) : System.Byte </remarks>
	public class UIntegerToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (byte)(uint)AArgument1;
		}
	}
	#endif

	/// <remarks> operator ByteToLong(System.Byte) : System.Long </remarks>
	public class ByteToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (long)(byte)argument1;
		}
	}
	
	/// <remarks> operator LongToByte(System.Long) : System.Byte </remarks>
	public class LongToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				long tempValue = (long)argument1;
				if ((tempValue < Byte.MinValue) || (tempValue > Byte.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Byte values", tempValue.ToString()));
				return (byte)tempValue;
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToULong(System.Byte) : System.ULong </remarks>
	public class ByteToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(byte)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToByte(System.ULong) : System.Byte </remarks>
	public class ULongToByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (byte)(ulong)AArgument1;
		}
	}
	#endif

	#if UseUnsignedIntegers	
	/// <remarks> operator SByteToShort(System.SByte) : System.Short </remarks>
	public class SByteToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (short)(sbyte)AArgument1;
		}
	}
	
	/// <remarks> operator ShortToSByte(System.Short) : System.SByte </remarks>
	public class ShortToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(short)AArgument1;
		}
	}

	/// <remarks> operator SByteToUShort(System.SByte) : System.UShort </remarks>
	public class SByteToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)AArgument1;
		}
	}
	
	/// <remarks> operator UShortToSByte(System.UShort) : System.SByte </remarks>
	public class UShortToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(ushort)AArgument1;
		}
	}

	/// <remarks> operator SByteToInteger(System.SByte) : System.Integer </remarks>
	public class SByteToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (int)(sbyte)AArgument1;
		}
	}
	
	/// <remarks> operator IntegerToSByte(System.Integer) : System.SByte </remarks>
	public class IntegerToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(int)AArgument1;
		}
	}

	/// <remarks> operator SByteToUInteger(System.SByte) : System.UInteger </remarks>
	public class SByteToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(sbyte)AArgument1;
		}
	}
	
	/// <remarks> operator UIntegerToSByte(System.UInteger) : System.SByte </remarks>
	public class UIntegerToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(uint)AArgument1;
		}
	}

	/// <remarks> operator SByteToLong(System.SByte) : System.Long </remarks>
	public class SByteToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (long)(sbyte)AArgument1;
		}
	}
	
	/// <remarks> operator LongToSByte(System.Long) : System.SByte </remarks>
	public class LongToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(long)AArgument1;
		}
	}

	/// <remarks> operator SByteToULong(System.SByte) : System.ULong </remarks>
	public class SByteToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(sbyte)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToSByte(System.ULong) : System.SByte </remarks>
	public class ULongToSByteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (sbyte)(ulong)AArgument1;
		}
	}
	#endif

	#if UseUnsignedIntegers	
	/// <remarks> operator ShortToUShort(System.Short) : System.UShort </remarks>
	public class ShortToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)(short)AArgument1;
		}
	}
	
	/// <remarks> operator UShortToShort(System.UShort) : System.Short </remarks>
	public class UShortToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (short)(ushort)AArgument1;
		}
	}
	#endif

	/// <remarks> operator ShortToInteger(System.Short) : System.Integer </remarks>
	public class ShortToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (int)(short)argument1;
		}
	}
	
	/// <remarks> operator IntegerToShort(System.Integer) : System.Short </remarks>
	public class IntegerToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				int tempValue = (int)argument1;
				if ((tempValue < Int16.MinValue) || (tempValue > Int16.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Short values", tempValue.ToString()));
				return (short)tempValue;
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ShortToUInteger(System.Short) : System.UInteger </remarks>
	public class ShortToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(short)AArgument1;
		}
	}
	
	/// <remarks> operator UIntegerToShort(System.UInteger) : System.Short </remarks>
	public class UIntegerToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (short)(uint)AArgument1;
		}
	}
	#endif

	/// <remarks> operator ShortToLong(System.Short) : System.Long </remarks>
	public class ShortToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (long)(short)argument1;
		}
	}
	
	/// <remarks> operator LongToShort(System.Long) : System.Short </remarks>
	public class LongToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				long tempValue = (long)argument1;
				if ((tempValue < Int16.MinValue) || (tempValue > Int16.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Short values", tempValue.ToString()));
				return (short)tempValue;
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ShortToULong(System.Short) : System.ULong </remarks>
	public class ShortToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(short)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToShort(System.ULong) : System.Short </remarks>
	public class ULongToShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (short)(ulong)AArgument1;
		}
	}

	/// <remarks> operator UShortToInteger(System.UShort) : System.Integer </remarks>
	public class UShortToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (int)(ushort)AArgument1;
		}
	}
	
	/// <remarks> operator IntegerToUShort(System.Integer) : System.UShort </remarks>
	public class IntegerToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)(int)AArgument1;
		}
	}

	/// <remarks> operator UShortToUInteger(System.UShort) : System.UInteger </remarks>
	public class UShortToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(ushort)AArgument1;
		}
	}
	
	/// <remarks> operator UIntegerToUShort(System.UInteger) : System.UShort </remarks>
	public class UIntegerToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)(uint)AArgument1;
		}
	}

	/// <remarks> operator UShortToLong(System.UShort) : System.Long </remarks>
	public class UShortToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)AArgument1;
		}
	}
	
	/// <remarks> operator LongToUShort(System.Long) : System.UShort </remarks>
	public class LongToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)(long)AArgument1;
		}
	}

	/// <remarks> operator UShortToULong(System.UShort) : System.ULong </remarks>
	public class UShortToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(ushort)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToUShort(System.ULong) : System.UShort </remarks>
	public class ULongToUShortNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ushort)(ulong)AArgument1;
		}
	}

	/// <remarks> operator IntegerToUInteger(System.Integer) : System.UInteger </remarks>
	public class IntegerToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(int)AArgument1;
		}
	}
	
	/// <remarks> operator UIntegerToInteger(System.UInteger) : System.Integer </remarks>
	public class UIntegerToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (int)(uint)AArgument1;
		}
	}
	#endif

	/// <remarks> operator IntegerToLong(System.Integer) : System.Long </remarks>
	public class IntegerToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (long)(int)argument1;
		}
	}
	
	/// <remarks> operator LongToInteger(System.Long) : System.Integer </remarks>
	public class LongToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				long tempValue = (long)argument1;
				if ((tempValue < Int32.MinValue) || (tempValue > Int32.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Integer values", tempValue.ToString()));
				return (int)tempValue;
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator IntegerToULong(System.Integer) : System.ULong </remarks>
	public class IntegerToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(int)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToInteger(System.ULong) : System.Integer </remarks>
	public class ULongToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (int)(ulong)AArgument1;
		}
	}

	/// <remarks> operator UIntegerToLong(System.UInteger) : System.Long </remarks>
	public class UIntegerToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (long)(uint)AArgument1;
		}
	}
	
	/// <remarks> operator LongToUInteger(System.Long) : System.UInteger </remarks>
	public class LongToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(long)AArgument1;
		}
	}

	/// <remarks> operator UIntegerToULong(System.UInteger) : System.ULong </remarks>
	public class UIntegerToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(uint)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToUInteger(System.ULong) : System.UInteger </remarks>
	public class ULongToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (uint)(ulong)AArgument1;
		}
	}

	/// <remarks> operator LongToULong(System.Long) : System.ULong </remarks>
	public class LongToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (ulong)(long)AArgument1;
		}
	}
	
	/// <remarks> operator ULongToLong(System.ULong) : System.Long </remarks>
	public class ULongToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (long)(ulong)AArgument1;
		}
	}
	#endif

	#if USEDOUBLE
	/// <remarks> operator DoubleToString(System.Double) : System.String </remarks>
	public class DoubleToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return Convert.ToString((double)AArgument1);
		}
	}
	
	/// <remarks> operator StringToDouble(System.String) : System.Double </remarks>
	public class StringToDoubleNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return Convert.ToDouble((string)AArgument1);
		}
	}
	
	/// <remarks> operator DoubleToInteger(System.Double) : System.Integer </remarks>
	public class DoubleToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return Convert.ToInt32((double)AArgument1());
		}
	}
	
	/// <remarks> operator IntegerToDouble(System.Integer) : System.Double </remarks>
	public class IntegerToDoubleNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return Convert.ToDouble((int)AArgument1);
		}
	}
	#endif
	
	/// <remarks> operator DecimalToString(System.Decimal) : System.String </remarks>
	public class DecimalToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((decimal)argument1).ToString("0.#############################");
		}
	}
	
	/// <remarks> operator MoneyToString(System.Decimal) : System.String </remarks>
	public class MoneyToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return ((decimal)argument1).ToString("C");
		}
	}
	
	/// <remarks> operator StringToDecimal(System.String) : System.Decimal </remarks>
	public class StringToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToDecimal((string)argument1);
		}
	}
	
	/// <remarks> operator StringToMoney(System.String) : System.Decimal </remarks>
	public class StringToMoneyNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Decimal.Parse((string)argument1, System.Globalization.NumberStyles.Currency);
		}
	}
	
	/// <remarks> operator DecimalToInteger(System.Decimal) : System.Integer </remarks>
	public class DecimalToIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				decimal tempValue = (decimal)argument1;
				if ((tempValue < Int32.MinValue) || (tempValue > Int32.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Integer values", tempValue.ToString()));
				return Convert.ToInt32(tempValue);
			}
		}
	}
	
	/// <remarks> operator IntegerToDecimal(System.Integer) : System.Decimal </remarks>
	public class IntegerToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToDecimal((int)argument1);
		}
	}
	
	#if USEDOUBLE
	/// <remarks> operator DecimalToDouble(System.Decimal) : System.Double </remarks>
	public class DecimalToDoubleNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return Convert.ToDouble((decimal)AArgument1);
		}
	}
	
	/// <remarks> operator DoubleToDecimal(System.Double) : System.Decimal </remarks>
	public class DoubleToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return Convert.ToDecimal((double)AArgument1);
		}
	}
	#endif

	public class DecimalToMoneyNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return (decimal)argument1;
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator DecimalToUInteger(System.Decimal) : System.UInteger </remarks>
	public class DecimalToUIntegerNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToUInt32((decimal)AArgument1);
		}
	}
	
	/// <remarks> operator UIntegerToDecimal(System.UInteger) : System.Decimal </remarks>
	public class UIntegerToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToDecimal((uint)AArgument1);
		}
	}
	#endif
	
	/// <remarks> operator DecimalToLong(System.Decimal) : System.Long </remarks>
	public class DecimalToLongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				decimal tempValue = (decimal)argument1;
				if ((tempValue < Int64.MinValue) || (tempValue > Int64.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(@" The value ({0}) is outside the range of System.Long", tempValue.ToString()));
				return Convert.ToInt64(tempValue);
			}
		}
	}

	/// <remarks> operator LongToDecimal(System.Long) : System.Decimal </remarks>
	public class LongToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Convert.ToDecimal((long)argument1);
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator DecimalToULong(System.Decimal) : System.ULong </remarks>
	public class DecimalToULongNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToUInt64((decimal)AArgument1);
		}
	}

	/// <remarks> operator ULongToDecimal(System.ULong) : System.Decimal </remarks>
	public class ULongToDecimalNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Convert.ToDecimal((ulong)AArgument1);
		}
	}
	#endif
	
	/// <remarks> operator System.TimeSpan(System.DateTime) : System.TimeSpan; </remarks>
	public class DateTimeToTimeSpanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return new TimeSpan(((DateTime)argument1).Ticks);
		}
	}
	
	/// <remarks> operator System.DateTime(System.TimeSpan) : System.DateTime; </remarks>
	public class TimeSpanToDateTimeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return new DateTime(((TimeSpan)argument1).Ticks);
		}
	}
	
	#if USEISTRING
	/// <remarks> operator IStringToString(System.IString) : System.String </remarks>
	public class IStringToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return ((string)AArgument1).ToUpper();
		}
	}
	
	/// <remarks> operator StringToIString(System.String) : System.IString </remarks>
	public class StringToIStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return (string)AArgument1;
		}
	}
	#endif
}
