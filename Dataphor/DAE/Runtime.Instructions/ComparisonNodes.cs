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
    public class ScalarEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				throw new RuntimeException(RuntimeException.Codes.ValueEncountered, this);
		}
    }
    
	/// <remarks> operator iEqual(byte, byte) : bool </remarks>
    public class ByteEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						AArguments[0].Value.AsByte ==
						AArguments[1].Value.AsByte
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(sbyte, sbyte) : bool </remarks>
    public class SByteEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsSByte() ==
						AArguments[1].Value.AsSByte()
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(short, short) : bool </remarks>
    public class ShortEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt16 ==
						AArguments[1].Value.AsInt16
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(ushort, ushort) : bool </remarks>
    public class UShortEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt16() ==
						AArguments[1].Value.AsUInt16()
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(integer, integer) : bool </remarks>
    public class IntegerEqualNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt32 ==
						AArguments[1].Value.AsInt32
					)
				);
		}
		*/
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			#if NILPROPOGATION
			if ((LLeftValue == null) || LLeftValue.IsNil || (LRightValue == null) || LRightValue.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.AsInt32 == LRightValue.AsInt32));
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(uinteger, uinteger) : bool </remarks>
    public class UIntegerEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt32() ==
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(long, long) : bool </remarks>
    public class LongEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt64 ==
						AArguments[1].Value.AsInt64
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iEqual(ulong, ulong) : bool </remarks>
    public class ULongEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt64() ==
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iEqual(Guid, Guid) : bool </remarks>
    public class GuidEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsGuid ==
						AArguments[1].Value.AsGuid
					)
				);
		}
    }
    
    /// <remarks> operator iEqual(bool, bool) : bool </remarks>
    public class BooleanEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsBoolean ==
						AArguments[1].Value.AsBoolean
					)
				);
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iEqual(double, double) : bool </remarks>
    public class DoubleEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsDouble() ==
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iEqual(decimal, decimal) : bool </remarks>
    public class DecimalEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal ==
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iEqual(string, string) : bool </remarks>
	public class StringEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							false
						) == 
						0
					)
				);
		}
	}

	#if USEISTRING
	public class IStringEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							true
						) == 
						0
					)
				);
		}
	}
	#endif

	/// <remarks> operator iEqual(money, money) : bool </remarks>
	public class MoneyEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal ==
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}
	
	/// <remarks> operator iEqual(Error, Error) : Boolean; </remarks>
	public class ErrorEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsException.Message ==
						AArguments[1].Value.AsException.Message
					)
				);
		}
	}
	
	/// <remarks> operator iNotEqual(byte, byte) : bool </remarks>
    public class ByteNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsByte !=
						AArguments[1].Value.AsByte
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(sbyte, sbyte) : bool </remarks>
    public class SByteNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsSByte() !=
						AArguments[1].Value.AsSByte()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iNotEqual(short , short ) : bool </remarks>
    public class ShortNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt16 !=
						AArguments[1].Value.AsInt16
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(ushort , ushort ) : bool </remarks>
    public class UShortNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt16() !=
						AArguments[1].Value.AsUInt16()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iNotEqual(integer, integer) : bool </remarks>
    public class IntegerNotEqualNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt32 !=
						AArguments[1].Value.AsInt32
					)
				);
		}
		*/
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			if ((LLeftValue == null) || (LRightValue == null))
				return new DataVar(FDataType, null);
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.AsInt32 != LRightValue.AsInt32));
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(uinteger, uinteger) : bool </remarks>
    public class UIntegerNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt32() !=
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iNotEqual(long, long) : bool </remarks>
    public class LongNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt64 !=
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iNotEqual(ulong, ulong) : bool </remarks>
    public class ULongNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt64() !=
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iNotEqual(Guid, Guid) : bool </remarks>
    public class GuidNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsGuid !=
						AArguments[1].Value.AsGuid
					)
				);
		}
    }

    /// <remarks> operator iNotEqual(bool, bool) : bool </remarks>
    public class BooleanNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsBoolean !=
						AArguments[1].Value.AsBoolean
					)
				);
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iNotEqual(double, double) : bool </remarks>
    public class DoubleNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsDouble() !=
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iNotEqual(decimal, decimal) : bool </remarks>
    public class DecimalNotEqualNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal !=
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iNotEqual(string, string) : bool </remarks>
	public class StringNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							false
						) !=
						0
					)
				);
		}
	}

	#if USEISTRING
	public class IStringNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							true
						) !=
						0
					)
				);
		}
	}
	#endif

	/// <remarks> operator iNotEqual(money, money) : bool </remarks>
	public class MoneyNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal !=
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iGreater(bool, bool) : bool </remarks>
    public class BooleanGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsBoolean ? !AArguments[1].Value.AsBoolean : false
					)
				);
		}
    }

	/// <remarks> operator iGreater(byte, byte) : bool </remarks>
    public class ByteGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsByte >
						AArguments[1].Value.AsByte
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(sbyte, sbyte) : bool </remarks>
    public class SByteGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsSByte() >
						AArguments[1].Value.AsSByte()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iGreater(short, short) : bool </remarks>
    public class ShortGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt16 >
						AArguments[1].Value.AsInt16
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(ushort, ushort) : bool </remarks>
    public class UShortGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt16() >
						AArguments[1].Value.AsUInt16()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iGreater(integer, integer) : bool </remarks>
    public class IntegerGreaterNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt32 >
						AArguments[1].Value.AsInt32
					)
				);
		}
		*/

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			#if NILPROPOGATION
			if ((LLeftValue == null) || LLeftValue.IsNil || (LRightValue == null) || LRightValue.IsNil)
				return new DataVar(FDataType, null);
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.AsInt32 > LRightValue.AsInt32));
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(uinteger, uinteger) : bool </remarks>
    public class UIntegerGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt32() >
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iGreater(long, long) : bool </remarks>
    public class LongGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt64 >
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iGreater(ulong, ulong) : bool </remarks>
    public class ULongGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsUInt64() >
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iGreater(Guid, Guid) : bool </remarks>
    public class GuidGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsGuid.CompareTo(AArguments[1].Value.AsGuid) > 0)
				);
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iGreater(double, double) : bool </remarks>
    public class DoubleGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AArguments[0].Value.AsDouble() >
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iGreater(decimal, decimal) : bool </remarks>
    public class DecimalGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal >
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iGreater(string, string) : bool </remarks>
	public class StringGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							false
						) >
						0
					)
				);
		}
	}

	#if USEISTRING
	public class IStringGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							true
						) >
						0
					)
				);
		}
	}
	#endif

	/// <remarks> operator iGreater(money, money) : bool </remarks>
	public class MoneyGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal.CompareTo(AArguments[1].Value.AsDecimal) > 0
					)
				);
		}
	}

	/// <remarks> operator iLess(bool, bool) : bool </remarks>
    public class BooleanLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						!AArguments[0].Value.AsBoolean ? 
						AArguments[1].Value.AsBoolean : 
						false
					)
				);
		}
    }

    /// <remarks> operator iLess(byte, byte) : bool </remarks>
    public class ByteLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsByte <
						AArguments[1].Value.AsByte
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(sbyte, sbyte) : bool </remarks>
    public class SByteLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsSByte() <
						AArguments[1].Value.AsSByte()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLess(short, short) : bool </remarks>
    public class ShortLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt16 <
						AArguments[1].Value.AsInt16
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(ushort, ushort) : bool </remarks>
    public class UShortLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt16() <
						AArguments[1].Value.AsUInt16()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLess(integer, integer) : bool </remarks>
    public class IntegerLessNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt32 <
						AArguments[1].Value.AsInt32
					)
				);
		}
		*/

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			#if NILPROPOGATION
			if ((LLeftValue == null) || (LRightValue == null))
				return new DataVar(FDataType, null);
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.AsInt32 < LRightValue.AsInt32));
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(uinteger, uinteger) : bool </remarks>
    public class UIntegerLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt32() <
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLess(long, long) : bool </remarks>
    public class LongLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt64 <
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLess(ulong, ulong) : bool </remarks>
    public class ULongLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt64() <
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLess(Guid, Guid) : bool </remarks>
    public class GuidLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsGuid.CompareTo(AArguments[1].Value.AsGuid) < 0				
					)
				);
		}
    }
    
	#if USEDOUBLE
	/// <remarks> operator iLess(double, double) : bool </remarks>
    public class DoubleLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsDouble() <
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
	#endif

    /// <remarks> operator iLess(decimal, decimal) : bool </remarks>
    public class DecimalLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal <
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iLess(string, string) : bool </remarks>
	public class StringLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							false
						) <
						0
					)
				);
		}
	}

	#if USEISTRING
	public class IStringLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							true
						) <
						0
					)
				);
		}
	}
	#endif

	/// <remarks> operator iLess(money, money) : bool </remarks>
	public class MoneyLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal.CompareTo(AArguments[1].Value.AsDecimal) < 0
					)
				);
		}
	}

	/// <remarks> operator iInclusiveGreater(bool, bool) : bool </remarks>
    public class BooleanInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				bool LLeftValue = AArguments[0].Value.AsBoolean;
				bool LRightValue = AArguments[1].Value.AsBoolean;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue || LLeftValue ? true : false)
				);
			}
		}
    }

    /// <remarks> operator iInclusiveGreater(byte, byte) : bool </remarks>
    public class ByteInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsByte >=
						AArguments[1].Value.AsByte
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(sbyte, sbyte) : bool </remarks>
    public class SByteInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsSByte() >=
						AArguments[1].Value.AsSByte()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(short, short) : bool </remarks>
    public class ShortInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt16 >=
						AArguments[1].Value.AsInt16
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(ushort, ushort) : bool </remarks>
    public class UShortInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt16() >=
						AArguments[1].Value.AsUInt16()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(integer, integer) : bool </remarks>
    public class IntegerInclusiveGreaterNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt32 >=
						AArguments[1].Value.AsInt32
					)
				);
		}
		*/
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			#if NILPROPOGATION
			if ((LLeftValue == null) || (LRightValue == null))
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.AsInt32 >= LRightValue.AsInt32));
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(uinteger, uinteger) : bool </remarks>
    public class UIntegerInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt32() >=
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(long, long) : bool </remarks>
    public class LongInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt64 >=
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveGreater(ulong, ulong) : bool </remarks>
    public class ULongInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsUInt64() >=
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(Guid, Guid) : bool </remarks>
    public class GuidInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsGuid.CompareTo(AArguments[1].Value.AsGuid) >= 0
					)
				);
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iInclusiveGreater(double, double) : bool </remarks>
    public class DoubleInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess,
						AArguments[0].Value.AsDouble() >=
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveGreater(decimal, decimal) : bool </remarks>
    public class DecimalInclusiveGreaterNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal >=
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iInclusiveGreater(string, string) : bool </remarks>
	public class StringInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							false
						) >=
						0
					)
				);
		}
	}

	#if USEISTRING
	public class IStringInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							true
						) >=
						0
					)
				);
		}
	}
	#endif

	/// <remarks> operator iInclusiveGreater(money, money) : bool </remarks>
	public class MoneyInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal.CompareTo(AArguments[1].Value.AsDecimal) >= 0
					)
				);
		}
	}

	/// <remarks> operator iInclusiveLess(bool, bool) : bool </remarks>
    public class BooleanInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				bool LLeftValue = AArguments[0].Value.AsBoolean;
				bool LRightValue = AArguments[1].Value.AsBoolean;
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue || !LLeftValue ? true : false));
			}
		}
    }

    /// <remarks> operator iInclusiveLess(byte, byte) : bool </remarks>
    public class ByteInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsByte <=
						AArguments[1].Value.AsByte
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveLess(sbyte, sbyte) : bool </remarks>
    public class SByteInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess, 
						AArguments[0].Value.AsSByte() <=
						AArguments[1].Value.AsSByte()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(short, short) : bool </remarks>
    public class ShortInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt16 <=
						AArguments[1].Value.AsInt16
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveLess(ushort, ushort) : bool </remarks>
    public class UShortInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess, 
						AArguments[0].Value.AsUInt16() <=
						AArguments[1].Value.AsUInt16()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(integer, integer) : bool </remarks>
    public class IntegerInclusiveLessNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt32 <=
						AArguments[1].Value.AsInt32
					)
				);
		}
		*/
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			#if NILPROPOGATION
			if ((LLeftValue == null) || (LRightValue == null))
				return new DataVar(FDataType, null);
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.AsInt32 <= LRightValue.AsInt32));
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iInclusiveLess(uinteger, uinteger) : bool </remarks>
    public class UIntegerInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess, 
						AArguments[0].Value.AsUInt32() <=
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(long, long) : bool </remarks>
    public class LongInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsInt64 <=
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers
    /// <remarks> operator iInclusiveLess(ulong, ulong) : bool </remarks>
    public class ULongInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess, 
						AArguments[0].Value.AsUInt64() <=
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(Guid, Guid) : bool </remarks>
    public class GuidInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsGuid.CompareTo(AArguments[1].Value.AsGuid) <= 0				
					)
				);
		}
    }

	#if USEDOUBLE
    /// <remarks> operator iInclusiveLess(double, double) : bool </remarks>
    public class DoubleInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					Scalar.FromBoolean
					(
						AProcess, 
						AArguments[0].Value.AsDouble() <=
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iInclusiveLess(decimal, decimal) : bool </remarks>
    public class DecimalInclusiveLessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal <=
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iInclusiveLess(string, string) : bool </remarks>
	public class StringInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							false
						) <=
						0
					)
				);
		}
	}
    
	#if USEISTRING
	public class IStringInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						String.Compare
						(
							AArguments[0].Value.AsString,
							AArguments[1].Value.AsString,
							true
						) <=
						0
					)
				);
		}
	}
	#endif
    
	/// <remarks> operator iInclusiveLess(money, money) : bool </remarks>
	public class MoneyInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						AArguments[0].Value.AsDecimal.CompareTo(AArguments[1].Value.AsDecimal) <= 0
					)
				);
		}
	}
    
	/// <remarks> operator iCompare(bool, bool) : integer </remarks>
    public class BooleanCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				bool LLeftValue = AArguments[0].Value.AsBoolean;
				bool LRightValue = AArguments[1].Value.AsBoolean;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue ? 1 : -1)
				);
			}
		}
    }

	/// <remarks> operator iCompare(byte, byte) : integer </remarks>
    public class ByteCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				byte LLeftValue = AArguments[0].Value.AsByte;
				byte LRightValue = AArguments[1].Value.AsByte;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(sbyte, sbyte) : integer </remarks>
    public class SByteCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				sbyte LLeftValue = AArguments[0].Value.AsSByte();
				sbyte LRightValue = AArguments[1].Value.AsSByte();
				return new DataVar
				(
					FDataType,
					Scalar.FromInt32(AProcess, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(short, short) : integer </remarks>
    public class ShortCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				short LLeftValue = AArguments[0].Value.AsInt16;
				short LRightValue = AArguments[1].Value.AsInt16;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(ushort, ushort) : integer </remarks>
    public class UShortCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				ushort LLeftValue = AArguments[0].Value.AsUInt16();
				ushort LRightValue = AArguments[1].Value.AsUInt16();
				return new DataVar
				(
					FDataType,
					Scalar.FromInt32(AProcess, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(int, int) : integer </remarks>
    public class IntegerCompareNode : InstructionNode
    {
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				int LLeftValue = AArguments[0].Value.AsInt32;
				int LRightValue = AArguments[1].Value.AsInt32;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
		*/
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataValue LLeftValue = Nodes[0].Execute(AProcess).Value;
			DataValue LRightValue = Nodes[1].Execute(AProcess).Value;
			#if NILPROPOGATION
			if ((LLeftValue == null) || LLeftValue.IsNil || (LRightValue == null) || LRightValue.IsNil)
				return new DataVar(FDataType, null);
			#endif
			{
				int LLeft = LLeftValue.AsInt32;
				int LRight = LRightValue.AsInt32;
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeft == LRight ? 0 : LLeft > LRight ? 1 : -1));
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(uint, uint) : integer </remarks>
    public class UIntegerCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				uint LLeftValue = AArguments[0].Value.AsUInt32();
				uint LRightValue = AArguments[1].Value.AsUInt32();
				return new DataVar
				(
					FDataType,
					Scalar.FromInt32(AProcess, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(long, long) : integer </remarks>
    public class LongCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				long LLeftValue = AArguments[0].Value.AsInt64;
				long LRightValue = AArguments[1].Value.AsInt64;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iCompare(ulong, ulong) : integer </remarks>
    public class ULongCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				ulong LLeftValue = AArguments[0].Value.AsUInt64();
				ulong LRightValue = AArguments[1].Value.AsUInt64();
				return new DataVar
				(
					FDataType,
					Scalar.FromInt32(AProcess, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(Guid, Guid) : integer </remarks>
    public class GuidCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				Guid LLeftValue = AArguments[0].Value.AsGuid;
				Guid LRightValue = AArguments[1].Value.AsGuid;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue.CompareTo(LRightValue))
				);
			}
		}
    }

	/// <remarks> operator iCompare(decimal, decimal) : integer </remarks>
    public class DecimalCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				decimal LLeftValue = AArguments[0].Value.AsDecimal;
				decimal LRightValue = AArguments[1].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }

	/// <remarks> operator iCompare(money, money) : integer </remarks>
    public class MoneyCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				decimal LLeftValue = AArguments[0].Value.AsDecimal;
				decimal LRightValue = AArguments[1].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
    }

	/// <remarks> operator iCompare(string, string) : integer </remarks>
    public class StringCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				string LLeftValue = AArguments[0].Value.AsString;
				string LRightValue = AArguments[1].Value.AsString;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, String.Compare(LLeftValue, LRightValue))
				);
			}
		}
    }

	#if USEISTRING
    public class IStringCompareNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				string LLeftValue = AArguments[0].Value.AsString;
				string LRightValue = AArguments[1].Value.AsString;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, String.Compare(LLeftValue, LRightValue, true))
				);
			}
		}
    }
    #endif

	/// <remarks> operator iCompare(TimeSpan, TimeSpan) : integer </remarks>
	public class TimeSpanCompareNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				TimeSpan LLeftValue = AArguments[0].Value.AsTimeSpan;
				TimeSpan LRightValue = AArguments[1].Value.AsTimeSpan;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
	}

	public class DateTimeCompareNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
	}

	public class DateCompareNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
	}

	public class TimeCompareNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue == LRightValue ? 0 : LLeftValue > LRightValue ? 1 : -1)
				);
			}
		}
	}

	/// <remarks> operator Max(byte, byte) : byte </remarks>
	public class ByteMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsByte));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsByte));
			else
			{
				byte LLeftValue = AArguments[0].Value.AsByte;
				byte LRightValue = AArguments[1].Value.AsByte;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Short, Short) : Short </remarks>
	public class ShortMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsInt16));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt16));
			else
			{
				short LLeftValue = AArguments[0].Value.AsInt16;
				short LRightValue = AArguments[1].Value.AsInt16;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Integer, Integer) : Integer </remarks>
	public class IntegerMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsInt32));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt32));
			else
			{
				int LLeftValue = AArguments[0].Value.AsInt32;
				int LRightValue = AArguments[1].Value.AsInt32;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Long, Long) : Long </remarks>
	public class LongMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsInt64));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt64));
			else
			{
				long LLeftValue = AArguments[0].Value.AsInt64;
				long LRightValue = AArguments[1].Value.AsInt64;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Decimal, Decimal) : Decimal </remarks>
	public class DecimalMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDecimal));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal));
			else
			{
				decimal LLeftValue = AArguments[0].Value.AsDecimal;
				decimal LRightValue = AArguments[1].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(TimeSpan, TimeSpan) : TimeSpan </remarks>
	public class TimeSpanMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsTimeSpan));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan));
			else
			{
				TimeSpan LLeftValue = AArguments[0].Value.AsTimeSpan;
				TimeSpan LRightValue = AArguments[1].Value.AsTimeSpan;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(DateTime, DateTime) : DateTime </remarks>
	public class DateTimeMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDateTime));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime));
			else
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Date, Date) : Date </remarks>
	public class DateMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDateTime));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime));
			else
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Time, Time) : Time </remarks>
	public class TimeMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDateTime));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime));
			else
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Max(Money, Money) : Money </remarks>
	public class MoneyMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDecimal));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal));
			else
			{
				decimal LLeftValue = AArguments[0].Value.AsDecimal;
				decimal LRightValue = AArguments[1].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue > LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(byte, byte) : byte </remarks>
	public class ByteMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsByte));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsByte));
			else
			{
				byte LLeftValue = AArguments[0].Value.AsByte;
				byte LRightValue = AArguments[1].Value.AsByte;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Short, Short) : Short </remarks>
	public class ShortMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsInt16));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt16));
			else
			{
				short LLeftValue = AArguments[0].Value.AsInt16;
				short LRightValue = AArguments[1].Value.AsInt16;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Integer, Integer) : Integer </remarks>
	public class IntegerMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsInt32));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt32));
			else
			{
				int LLeftValue = AArguments[0].Value.AsInt32;
				int LRightValue = AArguments[1].Value.AsInt32;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Long, Long) : Long </remarks>
	public class LongMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsInt64));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt64));
			else
			{
				long LLeftValue = AArguments[0].Value.AsInt64;
				long LRightValue = AArguments[1].Value.AsInt64;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Decimal, Decimal) : Decimal </remarks>
	public class DecimalMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDecimal));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal));
			else
			{
				decimal LLeftValue = AArguments[0].Value.AsDecimal;
				decimal LRightValue = AArguments[1].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(DateTime, DateTime) : DateTime </remarks>
	public class DateTimeMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDateTime));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime));
			else
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(TimeSpan, TimeSpan) : TimeSpan </remarks>
	public class TimeSpanMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsTimeSpan));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan));
			else
			{
				TimeSpan LLeftValue = AArguments[0].Value.AsTimeSpan;
				TimeSpan LRightValue = AArguments[1].Value.AsTimeSpan;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Date, Date) : Date </remarks>
	public class DateMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDateTime));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime));
			else
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Time, Time) : Time </remarks>
	public class TimeMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDateTime));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime));
			else
			{
				DateTime LLeftValue = AArguments[0].Value.AsDateTime;
				DateTime LRightValue = AArguments[1].Value.AsDateTime;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator Min(Money, Money) : Money </remarks>
	public class MoneyMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsDecimal));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal));
			else
			{
				decimal LLeftValue = AArguments[0].Value.AsDecimal;
				decimal LRightValue = AArguments[1].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, LLeftValue < LRightValue ? LLeftValue : LRightValue)
				);
			}
		}
	}

	/// <remarks> operator iBetween(boolean, boolean, boolean) : integer </remarks>
    public class BooleanBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null) || (AArguments[2].Value == null))
				return new DataVar(FDataType, null);
			else
			{
				bool LValue = AArguments[0].Value.AsBoolean;
				bool LLowerBound = AArguments[1].Value.AsBoolean;
				bool LUpperBound = AArguments[2].Value.AsBoolean;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue || (LValue == LLowerBound)) && (!LValue || (LValue == LUpperBound)))
				);
			}
		}
    }

	/// <remarks> operator iBetween(byte, byte, byte) : integer </remarks>
    public class ByteBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				byte LValue = AArguments[0].Value.AsByte;
				byte LLowerBound = AArguments[1].Value.AsByte;
				byte LUpperBound = AArguments[2].Value.AsByte;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(sbyte, sbyte) : integer </remarks>
    public class SByteBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				sbyte LValue = AArguments[0].Value.AsSByte();
				sbyte LLowerBound = AArguments[1].Value.AsSByte();
				sbyte LUpperBound = AArguments[2].Value.AsSByte();
				return new DataVar
				(
					FDataType,
					Scalar.FromBoolean(AProcess, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(short, short) : integer </remarks>
    public class ShortBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				short LValue = AArguments[0].Value.AsInt16;
				short LLowerBound = AArguments[1].Value.AsInt16;
				short LUpperBound = AArguments[2].Value.AsInt16;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(ushort, ushort) : integer </remarks>
    public class UShortBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				ushort LValue = AArguments[0].Value.AsUInt16();
				ushort LLowerBound = AArguments[1].Value.AsUInt16();
				ushort LUpperBound = AArguments[2].Value.AsUInt16();
				return new DataVar
				(
					FDataType,
					Scalar.FromBoolean(AProcess, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(integer, integer) : integer </remarks>
    public class IntegerBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				int LValue = AArguments[0].Value.AsInt32;
				int LLowerBound = AArguments[1].Value.AsInt32;
				int LUpperBound = AArguments[2].Value.AsInt32;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(uinteger, uinteger) : integer </remarks>
    public class UIntegerBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				uint LValue = AArguments[0].Value.AsUInt32();
				uint LLowerBound = AArguments[1].Value.AsUInt32();
				uint LUpperBound = AArguments[2].Value.AsUInt32();
				return new DataVar
				(
					FDataType,
					Scalar.FromBoolean(AProcess, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(long, long) : integer </remarks>
    public class LongBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				long LValue = AArguments[0].Value.AsInt64;
				long LLowerBound = AArguments[1].Value.AsInt64;
				long LUpperBound = AArguments[2].Value.AsInt64;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBetween(ulong, ulong) : integer </remarks>
    public class ULongBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				ulong LValue = AArguments[0].Value.AsUInt64();
				ulong LLowerBound = AArguments[1].Value.AsUInt64();
				ulong LUpperBound = AArguments[2].Value.AsUInt64();
				return new DataVar
				(
					FDataType,
					Scalar.FromBoolean(AProcess, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(Guid, Guid) : integer </remarks>
    public class GuidBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				Guid LValue = AArguments[0].Value.AsGuid;
				Guid LLowerBound = AArguments[1].Value.AsGuid;
				Guid LUpperBound = AArguments[2].Value.AsGuid;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue.CompareTo(LLowerBound) >= 0) && (LValue.CompareTo(LUpperBound) <= 0))
				);
			}
		}
    }

	/// <remarks> operator iBetween(decimal, decimal) : integer </remarks>
    public class DecimalBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				decimal LValue = AArguments[0].Value.AsDecimal;
				decimal LLowerBound = AArguments[1].Value.AsDecimal;
				decimal LUpperBound = AArguments[2].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }

	/// <remarks> operator iBetween(money, money) : integer </remarks>
    public class MoneyBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				decimal LValue = AArguments[0].Value.AsDecimal;
				decimal LLowerBound = AArguments[1].Value.AsDecimal;
				decimal LUpperBound = AArguments[2].Value.AsDecimal;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }

	/// <remarks> operator iBetween(string, string) : integer </remarks>
    public class StringBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				string LValue = AArguments[0].Value.AsString;
				string LLowerBound = AArguments[1].Value.AsString;
				string LUpperBound = AArguments[2].Value.AsString;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (String.Compare(LValue, LLowerBound, false) >= 0) && (String.Compare(LValue, LUpperBound, false) <= 0))
				);
			}
		}
    }

	#if USEISTRING
    public class IStringBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				string LValue = AArguments[0].Value.AsString;
				string LLowerBound = AArguments[1].Value.AsString;
				string LUpperBound = AArguments[2].Value.AsString;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (String.Compare(LValue, LLowerBound, true) >= 0) && (String.Compare(LValue, LUpperBound, true) <= 0))
				);
			}
		}
    }
    #endif

	/// <remarks> operator iBetween(TimeSpan, TimeSpan) : integer </remarks>
    public class TimeSpanBetweenNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || AArguments[2].Value == null || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				TimeSpan LValue = AArguments[0].Value.AsTimeSpan;
				TimeSpan LLowerBound = AArguments[1].Value.AsTimeSpan;
				TimeSpan LUpperBound = AArguments[2].Value.AsTimeSpan;
				return new DataVar
				(
					FDataType,
					new Scalar(AProcess, (Schema.ScalarType)FDataType, (LValue >= LLowerBound) && (LValue <= LUpperBound))
				);
			}
		}
    }
}
