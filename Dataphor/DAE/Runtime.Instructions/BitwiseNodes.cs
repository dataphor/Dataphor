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
    public class ByteBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemByte, (byte)~AArguments[0].Value.AsByte));
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(sbyte) : sbyte </remarks>
    public class SByteBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)~AArguments[0].Value.AsSByte()));
		}
    }
    #endif
    
	/// <remarks> operator iBitwiseNot(short) : short </remarks>
    public class ShortBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemShort, (short)~AArguments[0].Value.AsInt16));
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(ushort) : ushort </remarks>
    public class UShortBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt16((ushort)~AArguments[0].Value.AsUInt16()));
		}
    }
    #endif
    
	/// <remarks> operator iBitwiseNot(int) : int </remarks>
    public class IntegerBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemInteger, ~AArguments[0].Value.AsInt32));
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(uint) : uint </remarks>
    public class UIntegerBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt32(~AArguments[0].Value.AsUInt32()));
		}
    }
    #endif
    
	/// <remarks> operator iBitwiseNot(long) : long </remarks>
    public class LongBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemLong, ~AArguments[0].Value.AsInt64));
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseNot(ulong) : ulong </remarks>
    public class ULongBitwiseNotNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromUInt64(~AArguments[0].Value.AsUInt64()));
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(byte, byte) : byte </remarks>
    public class ByteBitwiseAndNode : InstructionNode
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
						AProcess.DataTypes.SystemByte, 
						(byte)
						(
							AArguments[0].Value.AsByte &
							AArguments[1].Value.AsByte
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(sbyte, sbyte) : sbyte </remarks>
    public class SByteBitwiseAndNode : InstructionNode
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
					Scalar.FromSByte
					(
						(sbyte)
						(
							AArguments[0].Value.AsSByte() &
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(short, short) : short </remarks>
    public class ShortBitwiseAndNode : InstructionNode
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
						AProcess.DataTypes.SystemShort, 
						(short)
						(
							AArguments[0].Value.AsInt16 &
							AArguments[1].Value.AsInt16
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(ushort, ushort) : ushort </remarks>
    public class UShortBitwiseAndNode : InstructionNode
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
					Scalar.FromUInt16
					(
						(ushort)
						(
							AArguments[0].Value.AsUInt16() &
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(integer, integer) : integer </remarks>
    public class IntegerBitwiseAndNode : InstructionNode
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
						AProcess.DataTypes.SystemInteger, 
						AArguments[0].Value.AsInt32 &
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerBitwiseAndNode : InstructionNode
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
					Scalar.FromUInt32
					(
						AArguments[0].Value.AsUInt32() &
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iBitwiseAnd(long, long) : long </remarks>
    public class LongBitwiseAndNode : InstructionNode
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
						AProcess.DataTypes.SystemLong, 
						AArguments[0].Value.AsInt64 &
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iBitwiseAnd(ulong, ulong) : ulong </remarks>
    public class ULongBitwiseAndNode : InstructionNode
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
					Scalar.FromUInt64
					(
						AArguments[0].Value.AsUInt64() &
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(byte, byte) : byte </remarks>
    public class ByteBitwiseOrNode : InstructionNode
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
						AProcess.DataTypes.SystemByte, 
						(byte)
						(
							AArguments[0].Value.AsByte |
							AArguments[1].Value.AsByte
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(sbyte, sbyte) : sbyte </remarks>
    public class SByteBitwiseOrNode : InstructionNode
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
					Scalar.FromSByte
					(
						(sbyte)
						(
							(byte)AArguments[0].Value.AsSByte() |
							(byte)AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(short, short) : short </remarks>
    public class ShortBitwiseOrNode : InstructionNode
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
						AProcess.DataTypes.SystemShort, 
						(short)
						(
							(ushort)AArguments[0].Value.AsInt16 |
							(ushort)AArguments[1].Value.AsInt16
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(ushort, ushort) : ushort </remarks>
    public class UShortBitwiseOrNode : InstructionNode
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
					Scalar.FromUInt16
					(
						(ushort)
						(
							AArguments[0].Value.AsUInt16() |
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(integer, integer) : integer </remarks>
    public class IntegerBitwiseOrNode : InstructionNode
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
						AProcess.DataTypes.SystemInteger, 
						AArguments[0].Value.AsInt32 |
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerBitwiseOrNode : InstructionNode
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
					Scalar.FromUInt32
					(
						AArguments[0].Value.AsUInt32() |
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iBitwiseOr(long, long) : long </remarks>
    public class LongBitwiseOrNode : InstructionNode
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
						AProcess.DataTypes.SystemLong, 
						AArguments[0].Value.AsInt64 |
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iBitwiseOr(ulong, ulong) : ulong </remarks>
    public class ULongBitwiseOrNode : InstructionNode
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
					Scalar.FromUInt64
					(
						AArguments[0].Value.AsUInt64() |
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(byte, byte) : byte </remarks>
    public class ByteBitwiseXorNode : InstructionNode
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
						AProcess.DataTypes.SystemByte, 
						(byte)
						(
							AArguments[0].Value.AsByte ^
							AArguments[1].Value.AsByte
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(sbyte, sbyte) : sbyte </remarks>
    public class SByteBitwiseXorNode : InstructionNode
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
					Scalar.FromSByte
					(
						(sbyte)
						(
							AArguments[0].Value.AsSByte() ^
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(short, short) : short </remarks>
    public class ShortBitwiseXorNode : InstructionNode
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
						AProcess.DataTypes.SystemShort, 
						(short)
						(
							AArguments[0].Value.AsInt16 ^
							AArguments[1].Value.AsInt16
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(ushort, ushort) : ushort </remarks>
    public class UShortBitwiseXorNode : InstructionNode
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
					Scalar.FromUInt16
					(
						(ushort)
						(
							AArguments[0].Value.AsUInt16() ^
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(integer, integer) : integer </remarks>
    public class IntegerBitwiseXorNode : InstructionNode
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
						AProcess.DataTypes.SystemInteger, 
						AArguments[0].Value.AsInt32 ^
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerBitwiseXorNode : InstructionNode
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
					Scalar.FromUInt32
					(
						AArguments[0].Value.AsUInt32() ^
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

    /// <remarks>operator iBitwiseXor(long, long) : long </remarks>
    public class LongBitwiseXorNode : InstructionNode
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
						AProcess.DataTypes.SystemLong, 
						AArguments[0].Value.AsInt64 ^
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks>operator iBitwiseXor(ulong, ulong) : ulong </remarks>
    public class ULongBitwiseXorNode : InstructionNode
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
					Scalar.FromUInt64
					(
						AArguments[0].Value.AsUInt64() ^
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(byte, integer) : byte </remarks>
    public class ByteShiftLeftNode : InstructionNode
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
						AProcess.DataTypes.SystemByte, 
						(byte)
						(
							AArguments[0].Value.AsByte <<
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLeftShift(sbyte, integer) : sbyte </remarks>
    public class SByteShiftLeftNode : InstructionNode
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
					Scalar.FromSByte
					(
						(sbyte)
						(
							AArguments[0].Value.AsSByte() <<
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(short, integer) : short </remarks>
    public class ShortShiftLeftNode : InstructionNode
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
						AProcess.DataTypes.SystemShort,
						(short)
						(
							AArguments[0].Value.AsInt16 <<
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers
    /// <remarks> operator iLeftShift(ushort, integer) : ushort </remarks>
    public class UShortShiftLeftNode : InstructionNode
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
					Scalar.FromUInt16
					(
						(ushort)
						(
							AArguments[0].Value.AsUInt16() <<
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(integer, integer) : integer </remarks>
    public class IntegerShiftLeftNode : InstructionNode
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
						AProcess.DataTypes.SystemInteger, 
						AArguments[0].Value.AsInt32 <<
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLeftShift(uinteger, integer) : uinteger </remarks>
    public class UIntegerShiftLeftNode : InstructionNode
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
					Scalar.FromUInt32
					(
						AArguments[0].Value.AsUInt32() <<
						AArguments[1].Value.AsInt32
					)
				);
		}
    }
    #endif

    /// <remarks> operator iLeftShift(long, integer) : long </remarks>
    public class LongShiftLeftNode : InstructionNode
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
						AProcess.DataTypes.SystemLong, 
						AArguments[0].Value.AsInt64 <<
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator iLeftShift(ulong, integer) : ulong </remarks>
    public class ULongShiftLeftNode : InstructionNode
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
					Scalar.FromUInt64
					(
						AArguments[0].Value.AsUInt64() <<
						AArguments[1].Value.AsInt32
					)
				);
		}
    }
    #endif

    /// <remarks> operator >>(byte, integer) : byte </remarks>
    public class ByteShiftRightNode : InstructionNode
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
						AProcess.DataTypes.SystemByte, 
						(byte)
						(
							AArguments[0].Value.AsByte >>
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(sbyte, integer) : sbyte </remarks>
    public class SByteShiftRightNode : InstructionNode
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
					Scalar.FromSByte
					(
						(sbyte)
						(
							AArguments[0].Value.AsSByte() >>
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }
    #endif

    /// <remarks> operator >>(short, integer) : short </remarks>
    public class ShortShiftRightNode : InstructionNode
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
						AProcess.DataTypes.SystemShort, 
						(short)
						(
							AArguments[0].Value.AsInt16 >>
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(ushort, integer) : ushort </remarks>
    public class UShortShiftRightNode : InstructionNode
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
					Scalar.FromUInt16
					(
						(ushort)
						(
							AArguments[0].Value.AsUInt16() >>
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }
    #endif

    /// <remarks> operator >>(integer, integer) : integer </remarks>
    public class IntegerShiftRightNode : InstructionNode
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
						AProcess.DataTypes.SystemInteger, 
						AArguments[0].Value.AsInt32 >>
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(uinteger, integer) : uinteger </remarks>
    public class UIntegerShiftRightNode : InstructionNode
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
					Scalar.FromUInt32
					(
						AArguments[0].Value.AsUInt32() >>
						AArguments[1].Value.AsInt32
					)
				);
		}
    }
    #endif

    /// <remarks> operator >>(long, integer) : long </remarks>
    public class LongShiftRightNode : InstructionNode
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
						AProcess.DataTypes.SystemLong, 
						AArguments[0].Value.AsInt64 >>
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
    /// <remarks> operator >>(ulong, integer) : ulong </remarks>
    public class ULongShiftRightNode : InstructionNode
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
					Scalar.FromUInt64
					(
						AArguments[0].Value.AsUInt64() >>
						AArguments[1].Value.AsInt32
					)
				);
		}
    }
    #endif
}
