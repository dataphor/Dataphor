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

	/// <remarks> operator BooleanToString(System.Boolean) : System.String </remarks>
	public class BooleanToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsBoolean ? Keywords.True : Keywords.False));
		}
	}
	
	/// <remarks> operator StringToBoolean(System.String) : System.Boolean </remarks>
	public class StringToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToBoolean(AArguments[0].Value.AsString)));
		}
	}
	
	/// <remarks> operator ByteToString(System.Byte) : System.String </remarks>
	public class ByteToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsByte.ToString()));
		}
	}
	
	/// <remarks> operator StringToByte(System.String) : System.Byte </remarks>
	public class StringToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToByte(AArguments[0].Value.AsString)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator SByteToString(System.SByte) : System.String </remarks>
	public class SByteToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromString(AProcess, AArguments[0].Value.AsSByte().ToString()));
		}
	}
	
	/// <remarks> operator StringToSByte(System.String) : System.SByte </remarks>
	public class StringToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte(Convert.ToSByte(AArguments[0].Value.AsString)));
		}
	}
	#endif
	
	/// <remarks> operator ShortToString(System.Short) : System.String </remarks>
	public class ShortToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt16.ToString()));
		}
	}
	
	/// <remarks> operator StringToShort(System.String) : System.Short </remarks>
	public class StringToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt16(AArguments[0].Value.AsString)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UShortToString(System.UShort) : System.String </remarks>
	public class UShortToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromString(AProcess, AArguments[0].Value.AsUInt16().ToString()));
		}
	}
	
	/// <remarks> operator StringToUShort(System.String) : System.UShort </remarks>
	public class StringToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16(Convert.ToUInt16(AArguments[0].Value.AsString)));
		}
	}
	#endif
	
	/// <remarks> operator IntegerToString(System.Integer) : System.String </remarks>
	public class IntegerToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt32.ToString()));
		}
	}
	
	/// <remarks> operator StringToInteger(System.String) : System.Integer </remarks>
	public class StringToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt32(AArguments[0].Value.AsString)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UIntegerToString(System.UInteger) : System.String </remarks>
	public class UIntegerToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromString(AProcess, AArguments[0].Value.AsUInt32().ToString()));
		}
	}
	
	/// <remarks> operator StringToUInteger(System.String) : System.UInteger </remarks>
	public class StringToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32(Convert.ToUInt32(AArguments[0].Value.AsString)));
		}
	}
	#endif
	
	/// <remarks> operator LongToString(System.Long) : System.String </remarks>
	public class LongToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt64.ToString()));
		}
	}
	
	/// <remarks> operator StringToLong(System.String) : System.Long </remarks>
	public class StringToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt64(AArguments[0].Value.AsString)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator ULongToString(System.ULong) : System.String </remarks>
	public class ULongToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromString(AProcess, AArguments[0].Value.AsUInt64().ToString()));
		}
	}
	
	/// <remarks> operator StringToULong(System.String) : System.ULong </remarks>
	public class StringToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64(Convert.ToUInt64(AArguments[0].Value.AsString)));
		}
	}
	#endif
	
    /// <remarks> operator ToString(AGuid : Guid) : string </remarks>
    public class GuidToStringNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsGuid.ToString()));
		}
    }

    /// <remarks> operator ToGuid(AString : string) : Guid </remarks>
	public class StringToGuidNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Guid(AArguments[0].Value.AsString)));
		}
	}

	/// <remarks> operator ByteToBoolean(System.Byte) : System.Boolean </remarks>
	public class ByteToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsByte == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToByte(System.Boolean) : System.Byte </remarks>
	public class BooleanToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (byte)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator SByteToBoolean(System.SByte) : System.Boolean </remarks>
	public class SByteToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromBoolean(AProcess, AArguments[0].Value.AsSByte() == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToSByte(System.Boolean) : System.SByte </remarks>
	public class BooleanToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	#endif
	
	/// <remarks> operator ShortToBoolean(System.Short) : System.Boolean </remarks>
	public class ShortToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt16 == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToShort(System.Boolean) : System.Short </remarks>
	public class BooleanToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (short)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UShortToBoolean(System.UShort) : System.Boolean </remarks>
	public class UShortToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromBoolean(AProcess, AArguments[0].Value.AsUInt16() == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToUShort(System.Boolean) : System.UShort </remarks>
	public class BooleanToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	#endif
	
	/// <remarks> operator IntegerToBoolean(System.Integer) : System.Boolean </remarks>
	public class IntegerToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt32 == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToInteger(System.Boolean) : System.Integer </remarks>
	public class BooleanToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsBoolean ? 1 : 0));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator UIntegerToBoolean(System.UInteger) : System.Boolean </remarks>
	public class UIntegerToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromBoolean(AProcess, AArguments[0].Value.AsUInt32() == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToUInteger(System.Boolean) : System.UInteger </remarks>
	public class BooleanToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	#endif
	
	/// <remarks> operator LongToBoolean(System.Long) : System.Boolean </remarks>
	public class LongToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt64 == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToLong(System.Boolean) : System.Long </remarks>
	public class BooleanToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (long)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator ULongToBoolean(System.ULong) : System.Boolean </remarks>
	public class ULongToBooleanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromBoolean(AProcess, AArguments[0].Value.AsUInt64() == 0 ? false : true));
		}
	}
	
	/// <remarks> operator BooleanToULong(System.Boolean) : System.ULong </remarks>
	public class BooleanToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)(AArguments[0].Value.AsBoolean ? 1 : 0)));
		}
	}
	#endif

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToSByte(System.Byte) : System.SByte </remarks>
	public class ByteToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator SByteToByte(System.SByte) : System.Byte </remarks>
	public class SByteToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromByte(AProcess, (byte)AArguments[0].Value.AsSByte()));
		}
	}
	#endif
	
	/// <remarks> operator ByteToShort(System.Byte) : System.Short </remarks>
	public class ByteToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (short)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator ShortToByte(System.Short) : System.Byte </remarks>
	public class ShortToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				short LValue = AArguments[0].Value.AsInt16;
				if ((LValue < Byte.MinValue) || (LValue > Byte.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Byte values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (byte)LValue));
			}			
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToUShort(System.Byte) : System.UShort </remarks>
	public class ByteToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator UShortToByte(System.UShort) : System.Byte </remarks>
	public class UShortToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromByte(AProcess, (byte)AArguments[0].Value.AsUInt16()));
		}
	}
	#endif

	/// <remarks> operator ByteToInteger(System.Byte) : System.Integer </remarks>
	public class ByteToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (int)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator IntegerToByte(System.Integer) : System.Byte </remarks>
	public class IntegerToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				int LValue = AArguments[0].Value.AsInt32;
				if ((LValue < Byte.MinValue) || (LValue > Byte.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Byte values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (byte)LValue));
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToUInteger(System.Byte) : System.UInteger </remarks>
	public class ByteToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator UIntegerToByte(System.UInteger) : System.Byte </remarks>
	public class UIntegerToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromByte(AProcess, (byte)AArguments[0].Value.AsUInt32()));
		}
	}
	#endif

	/// <remarks> operator ByteToLong(System.Byte) : System.Long </remarks>
	public class ByteToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (long)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator LongToByte(System.Long) : System.Byte </remarks>
	public class LongToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				long LValue = AArguments[0].Value.AsInt64;
				if ((LValue < Byte.MinValue) || (LValue > Byte.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Byte values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (byte)LValue));
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ByteToULong(System.Byte) : System.ULong </remarks>
	public class ByteToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsByte));
		}
	}
	
	/// <remarks> operator ULongToByte(System.ULong) : System.Byte </remarks>
	public class ULongToByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromByte(AProcess, (byte)AArguments[0].Value.AsUInt64()));
		}
	}
	#endif

	#if UseUnsignedIntegers	
	/// <remarks> operator SByteToShort(System.SByte) : System.Short </remarks>
	public class SByteToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt16(AProcess, (short)AArguments[0].Value.AsSByte()));
		}
	}
	
	/// <remarks> operator ShortToSByte(System.Short) : System.SByte </remarks>
	public class ShortToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsInt16));
		}
	}

	/// <remarks> operator SByteToUShort(System.SByte) : System.UShort </remarks>
	public class SByteToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsSByte()));
		}
	}
	
	/// <remarks> operator UShortToSByte(System.UShort) : System.SByte </remarks>
	public class UShortToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsUInt16()));
		}
	}

	/// <remarks> operator SByteToInteger(System.SByte) : System.Integer </remarks>
	public class SByteToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, (int)AArguments[0].Value.AsSByte()));
		}
	}
	
	/// <remarks> operator IntegerToSByte(System.Integer) : System.SByte </remarks>
	public class IntegerToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsInt32));
		}
	}

	/// <remarks> operator SByteToUInteger(System.SByte) : System.UInteger </remarks>
	public class SByteToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsSByte()));
		}
	}
	
	/// <remarks> operator UIntegerToSByte(System.UInteger) : System.SByte </remarks>
	public class UIntegerToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsUInt32()));
		}
	}

	/// <remarks> operator SByteToLong(System.SByte) : System.Long </remarks>
	public class SByteToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt64(AProcess, (long)AArguments[0].Value.AsSByte()));
		}
	}
	
	/// <remarks> operator LongToSByte(System.Long) : System.SByte </remarks>
	public class LongToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsInt64));
		}
	}

	/// <remarks> operator SByteToULong(System.SByte) : System.ULong </remarks>
	public class SByteToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsSByte()));
		}
	}
	
	/// <remarks> operator ULongToSByte(System.ULong) : System.SByte </remarks>
	public class ULongToSByteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)AArguments[0].Value.AsUInt64()));
		}
	}
	#endif

	#if UseUnsignedIntegers	
	/// <remarks> operator ShortToUShort(System.Short) : System.UShort </remarks>
	public class ShortToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsInt16));
		}
	}
	
	/// <remarks> operator UShortToShort(System.UShort) : System.Short </remarks>
	public class UShortToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt16(AProcess, (short)AArguments[0].Value.AsUInt16()));
		}
	}
	#endif

	/// <remarks> operator ShortToInteger(System.Short) : System.Integer </remarks>
	public class ShortToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (int)AArguments[0].Value.AsInt16));
		}
	}
	
	/// <remarks> operator IntegerToShort(System.Integer) : System.Short </remarks>
	public class IntegerToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				int LValue = AArguments[0].Value.AsInt32;
				if ((LValue < Int16.MinValue) || (LValue > Int16.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Short values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (short)LValue));
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ShortToUInteger(System.Short) : System.UInteger </remarks>
	public class ShortToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsInt16));
		}
	}
	
	/// <remarks> operator UIntegerToShort(System.UInteger) : System.Short </remarks>
	public class UIntegerToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt16(AProcess, (short)AArguments[0].Value.AsUInt32()));
		}
	}
	#endif

	/// <remarks> operator ShortToLong(System.Short) : System.Long </remarks>
	public class ShortToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (long)AArguments[0].Value.AsInt16));
		}
	}
	
	/// <remarks> operator LongToShort(System.Long) : System.Short </remarks>
	public class LongToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				long LValue = AArguments[0].Value.AsInt64;
				if ((LValue < Int16.MinValue) || (LValue > Int16.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Short values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (short)LValue));
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator ShortToULong(System.Short) : System.ULong </remarks>
	public class ShortToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsInt16));
		}
	}
	
	/// <remarks> operator ULongToShort(System.ULong) : System.Short </remarks>
	public class ULongToShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt16(AProcess, (short)AArguments[0].Value.AsUInt64()));
		}
	}

	/// <remarks> operator UShortToInteger(System.UShort) : System.Integer </remarks>
	public class UShortToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, (int)AArguments[0].Value.AsUInt16()));
		}
	}
	
	/// <remarks> operator IntegerToUShort(System.Integer) : System.UShort </remarks>
	public class IntegerToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsInt32));
		}
	}

	/// <remarks> operator UShortToUInteger(System.UShort) : System.UInteger </remarks>
	public class UShortToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsUInt16()));
		}
	}
	
	/// <remarks> operator UIntegerToUShort(System.UInteger) : System.UShort </remarks>
	public class UIntegerToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsUInt32()));
		}
	}

	/// <remarks> operator UShortToLong(System.UShort) : System.Long </remarks>
	public class UShortToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt64(AProcess, (long)AArguments[0].Value.AsUInt16()));
		}
	}
	
	/// <remarks> operator LongToUShort(System.Long) : System.UShort </remarks>
	public class LongToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsInt64));
		}
	}

	/// <remarks> operator UShortToULong(System.UShort) : System.ULong </remarks>
	public class UShortToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsUInt16()));
		}
	}
	
	/// <remarks> operator ULongToUShort(System.ULong) : System.UShort </remarks>
	public class ULongToUShortNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)AArguments[0].Value.AsUInt64()));
		}
	}

	/// <remarks> operator IntegerToUInteger(System.Integer) : System.UInteger </remarks>
	public class IntegerToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsInt32));
		}
	}
	
	/// <remarks> operator UIntegerToInteger(System.UInteger) : System.Integer </remarks>
	public class UIntegerToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, (int)AArguments[0].Value.AsUInt32()));
		}
	}
	#endif

	/// <remarks> operator IntegerToLong(System.Integer) : System.Long </remarks>
	public class IntegerToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (long)AArguments[0].Value.AsInt32));
		}
	}
	
	/// <remarks> operator LongToInteger(System.Long) : System.Integer </remarks>
	public class LongToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				long LValue = AArguments[0].Value.AsInt64;
				if ((LValue < Int32.MinValue) || (LValue > Int32.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Integer values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (int)LValue));
			}
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator IntegerToULong(System.Integer) : System.ULong </remarks>
	public class IntegerToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsInt32));
		}
	}
	
	/// <remarks> operator ULongToInteger(System.ULong) : System.Integer </remarks>
	public class ULongToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, (int)AArguments[0].Value.AsUInt64()));
		}
	}

	/// <remarks> operator UIntegerToLong(System.UInteger) : System.Long </remarks>
	public class UIntegerToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt64(AProcess, (long)AArguments[0].Value.AsUInt32()));
		}
	}
	
	/// <remarks> operator LongToUInteger(System.Long) : System.UInteger </remarks>
	public class LongToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsInt64));
		}
	}

	/// <remarks> operator UIntegerToULong(System.UInteger) : System.ULong </remarks>
	public class UIntegerToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsUInt32()));
		}
	}
	
	/// <remarks> operator ULongToUInteger(System.ULong) : System.UInteger </remarks>
	public class ULongToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32((uint)AArguments[0].Value.AsUInt64()));
		}
	}

	/// <remarks> operator LongToULong(System.Long) : System.ULong </remarks>
	public class LongToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64((ulong)AArguments[0].Value.AsInt64));
		}
	}
	
	/// <remarks> operator ULongToLong(System.ULong) : System.Long </remarks>
	public class ULongToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromInt64(AProcess, (long)AArguments[0].Value.AsUInt64()));
		}
	}
	#endif

	#if USEDOUBLE
	/// <remarks> operator DoubleToString(System.Double) : System.String </remarks>
	public class DoubleToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, Scalar.FromString(AProcess, Convert.ToString(AArguments[0].Value.AsDouble())));
		}
	}
	
	/// <remarks> operator StringToDouble(System.String) : System.Double </remarks>
	public class StringToDoubleNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, Scalar.FromDouble(Convert.ToDouble(AArguments[0].Value.AsString)));
		}
	}
	
	/// <remarks> operator DoubleToInteger(System.Double) : System.Integer </remarks>
	public class DoubleToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, Scalar.FromInt32(AProcess, Convert.ToInt32(AArguments[0].Value.AsDouble())));
		}
	}
	
	/// <remarks> operator IntegerToDouble(System.Integer) : System.Double </remarks>
	public class IntegerToDoubleNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, Scalar.FromDouble(Convert.ToDouble(AArguments[0].Value.AsInt32)));
		}
	}
	#endif
	
	/// <remarks> operator DecimalToString(System.Decimal) : System.String </remarks>
	public class DecimalToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal.ToString("0.#############################")));
		}
	}
	
	/// <remarks> operator MoneyToString(System.Decimal) : System.String </remarks>
	public class MoneyToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal.ToString("C")));
		}
	}
	
	/// <remarks> operator StringToDecimal(System.String) : System.Decimal </remarks>
	public class StringToDecimalNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToDecimal(AArguments[0].Value.AsString)));
		}
	}
	
	/// <remarks> operator StringToMoney(System.String) : System.Decimal </remarks>
	public class StringToMoneyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Decimal.Parse(AArguments[0].Value.AsString, System.Globalization.NumberStyles.Currency)));
		}
	}
	
	/// <remarks> operator DecimalToInteger(System.Decimal) : System.Integer </remarks>
	public class DecimalToIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				decimal LValue = AArguments[0].Value.AsDecimal;
				if ((LValue < Int32.MinValue) || (LValue > Int32.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(" The value ({0}) is outside the range of System.Integer values", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt32(LValue)));
			}
		}
	}
	
	/// <remarks> operator IntegerToDecimal(System.Integer) : System.Decimal </remarks>
	public class IntegerToDecimalNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToDecimal(AArguments[0].Value.AsInt32)));
		}
	}
	
	#if USEDOUBLE
	/// <remarks> operator DecimalToDouble(System.Decimal) : System.Double </remarks>
	public class DecimalToDoubleNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, Scalar.FromDouble(Convert.ToDouble(AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks> operator DoubleToDecimal(System.Double) : System.Decimal </remarks>
	public class DoubleToDecimalNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, Scalar.FromDecimal(AProcess, Convert.ToDecimal(AArguments[0].Value.AsDouble())));
		}
	}
	#endif

	public class DecimalToMoneyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal));
		}
	}

	#if UseUnsignedIntegers	
	/// <remarks> operator DecimalToUInteger(System.Decimal) : System.UInteger </remarks>
	public class DecimalToUIntegerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32(Convert.ToUInt32(AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks> operator UIntegerToDecimal(System.UInteger) : System.Decimal </remarks>
	public class UIntegerToDecimalNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, Convert.ToDecimal(AArguments[0].Value.AsUInt32())));
		}
	}
	#endif
	
	/// <remarks> operator DecimalToLong(System.Decimal) : System.Long </remarks>
	public class DecimalToLongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				decimal LValue = AArguments[0].Value.AsDecimal;
				if ((LValue < Int64.MinValue) || (LValue > Int64.MaxValue))
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, String.Format(@" The value ({0}) is outside the range of System.Long", LValue.ToString()));
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt64(LValue)));
			}
		}
	}

	/// <remarks> operator LongToDecimal(System.Long) : System.Decimal </remarks>
	public class LongToDecimalNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToDecimal(AArguments[0].Value.AsInt64)));
		}
	}
	
	#if UseUnsignedIntegers	
	/// <remarks> operator DecimalToULong(System.Decimal) : System.ULong </remarks>
	public class DecimalToULongNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64(Convert.ToUInt64(AArguments[0].Value.AsDecimal)));
		}
	}

	/// <remarks> operator ULongToDecimal(System.ULong) : System.Decimal </remarks>
	public class ULongToDecimalNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, Convert.ToDecimal(AArguments[0].Value.AsUInt64())));
		}
	}
	#endif
	
	/// <remarks> operator System.TimeSpan(System.DateTime) : System.TimeSpan; </remarks>
	public class DateTimeToTimeSpanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsDateTime.Ticks)));
		}
	}
	
	/// <remarks> operator System.DateTime(System.TimeSpan) : System.DateTime; </remarks>
	public class TimeSpanToDateTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(AArguments[0].Value.AsTimeSpan.Ticks)));
		}
	}
	
	#if USEISTRING
	/// <remarks> operator IStringToString(System.IString) : System.String </remarks>
	public class IStringToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString.ToUpper()));
		}
	}
	
	/// <remarks> operator StringToIString(System.String) : System.IString </remarks>
	public class StringToIStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsString));
		}
	}
	#endif
}
