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
    public class SByteNegateNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromSByte((sbyte)-AArguments[0].Value.AsSByte()));
			
		}
    }
    #endif
    
	/// <remarks> operator iNegate(short) : short </remarks>
    public class ShortNegateNode : InstructionNode
    {
		public ShortNegateNode() : base()
		{
			ShouldEmitIL = true;
		}

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
			AGenerator.Emit(OpCodes.Newobj, typeof(Scalar).GetConstructor(new Type[] { typeof(ServerProcess), typeof(Schema.ScalarType), typeof(object) }));
			
			AGenerator.MarkLabel(LCreateResult);
			
			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (short)-AArguments[0].Value.AsInt16));
		}
    }
    
	/// <remarks> operator iNegate(int) : int </remarks>
    public class IntegerNegateNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, -AArguments[0].Value.AsInt32));
		}
    }
    
	/// <remarks> operator iNegate(long) : long </remarks>
    public class LongNegateNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, -AArguments[0].Value.AsInt64));
		}
    }

	#if USEDOUBLE    
	/// <remarks> operator iNegate(double) : double </remarks>    
    public class DoubleNegateNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, Scalar.FromDouble(-AArguments[0].Value.AsDouble()));
		}
    }
    #endif
	
	/// <remarks> operator iNegate(decimal) : decimal </remarks>    
    public class DecimalNegateNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, -AArguments[0].Value.AsDecimal));
		}
    }
    /// <remarks> operator iPower(byte, byte) : byte </remarks>
    public class BytePowerNode : InstructionNode
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
						(byte)Math.Pow
						(
							AArguments[0].Value.AsByte, 
							AArguments[1].Value.AsByte
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(sbyte, sbyte) : sbyte </remarks>
    public class SBytePowerNode : InstructionNode
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
						AProcess,
						(sbyte)Math.Pow
						(
							AArguments[0].Value.AsSByte(), 
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif
    
    /// <remarks> operator iPower(short, short) : short </remarks>
    public class ShortPowerNode : InstructionNode
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
						(short)Math.Pow
						(
							AArguments[0].Value.AsInt16, 
							AArguments[1].Value.AsInt16
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(ushort, ushort) : ushort </remarks>
    public class UShortPowerNode : InstructionNode
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
						(ushort)Math.Pow
						(
							AArguments[0].Value.AsUInt16(), 
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif
    
    /// <remarks> operator iPower(integer, integer) : integer </remarks>
    public class IntegerPowerNode : InstructionNode
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
						(int)Math.Pow
						(
							AArguments[0].Value.AsInt32, 
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerPowerNode : InstructionNode
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
						(uint)Math.Pow
						(
							AArguments[0].Value.AsUInt32(), 
							AArguments[1].Value.AsUInt32()
						)
					)
				);
		}
    }
    #endif
    
    /// <remarks> operator iPower(long, long) : long </remarks>
    public class LongPowerNode : InstructionNode
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
						(long)Math.Pow
						(
							AArguments[0].Value.AsInt64, 
							AArguments[1].Value.AsInt64
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
    /// <remarks> operator iPower(ulong, ulong) : ulong </remarks>
    public class ULongPowerNode : InstructionNode
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
						(ulong)Math.Pow
						(
							AArguments[0].Value.AsUInt64(), 
							AArguments[1].Value.AsUInt64()
						)
					)
				);
		}
    }
    #endif

	#if USEDOUBLE    
    /// <remarks> operator iPower(double, double) : double </remarks> 
    public class DoublePowerNode : InstructionNode
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
					Scalar.FromDouble
					(
						Math.Pow
						(
							AArguments[0].Value.AsDouble(), 
							AArguments[1].Value.AsDouble()
						)
					)
				);
		}
    }
    #endif
    
    /// <remarks> operator iPower(decimal, decimal) : decimal </remarks>
    public class DecimalPowerNode : InstructionNode
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
						(decimal)Math.Pow
						(
							(double)AArguments[0].Value.AsDecimal, 
							(double)AArguments[1].Value.AsDecimal
						)
					)
				);
		}
    }

	/// <remarks> operator iMultiplication(byte, byte) : byte </remarks>    
    public class ByteMultiplicationNode : InstructionNode
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
						checked
						(
							(byte)
							(
								AArguments[0].Value.AsByte *
								AArguments[1].Value.AsByte
							)
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(sbyte, sbyte) : sbyte </remarks>    
    public class SByteMultiplicationNode : InstructionNode
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
							AArguments[0].Value.AsSByte() *
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(short, short) : short </remarks>    
    public class ShortMultiplicationNode : InstructionNode
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
						checked
						(
							(short)
							(
								AArguments[0].Value.AsInt16 *
								AArguments[1].Value.AsInt16
							)
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(ushort, ushort) : ushort </remarks>    
    public class UShortMultiplicationNode : InstructionNode
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
							AArguments[0].Value.AsUInt16() *
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(integer, integer) : integer </remarks>    
    public class IntegerMultiplicationNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt32 *
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(uinteger, uinteger) : uinteger </remarks>    
    public class UIntegerMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsUInt32() *
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(long, long) : long </remarks>    
    public class LongMultiplicationNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt64 *
							AArguments[1].Value.AsInt64
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMultiplication(ulong, ulong) : ulong </remarks>    
    public class ULongMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsUInt64() *
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

	#if USEDOUBLE    
	/// <remarks> operator iMultiplication(double, double) : double </remarks>    
    public class DoubleMultiplicationNode : InstructionNode
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
					Scalar.FromDouble
					(
						AArguments[0].Value.AsDouble() *
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMultiplication(decimal, decimal) : decimal </remarks>    
	public class DecimalMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsDecimal *
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iMultiplication(money, decimal) : money </remarks>    
	public class MoneyDecimalMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsDecimal *
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iMultiplication(decimal, money) : money </remarks>    
	public class DecimalMoneyMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsDecimal *
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iMultiplication(money, integer) : money </remarks>    
	public class MoneyIntegerMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsDecimal *
						AArguments[1].Value.AsInt32
					)
				);
		}
	}

	/// <remarks> operator iMultiplication(integer, money) : money </remarks>    
	public class IntegerMoneyMultiplicationNode : InstructionNode
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
						AArguments[0].Value.AsInt32 *
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iDivision(byte, byte) : decimal</remarks>    
    public class ByteDivisionNode : InstructionNode
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
						(
							(decimal)AArguments[0].Value.AsByte /
							(decimal)AArguments[1].Value.AsByte
						)
					)
				);
		}
    }
    
	/// <remarks> operator iDiv(byte, byte) : byte</remarks>    
    public class ByteDivNode : InstructionNode
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
						checked
						(
							(byte)
							(
								AArguments[0].Value.AsByte /
								AArguments[1].Value.AsByte
							)
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(sbyte, sbyte) : decimal</remarks>    
    public class SByteDivisionNode : InstructionNode
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
					Scalar.FromDecimal
					(
						(
							(decimal)AArguments[0].Value.AsSByte() /
							(decimal)AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }

	/// <remarks> operator iDiv(sbyte, sbyte) : sbyte </remarks>    
    public class SByteDivNode : InstructionNode
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
							AArguments[0].Value.AsSByte() /
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iDivision(short, short) : decimal</remarks>    
    public class ShortDivisionNode : InstructionNode
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
						(
							(decimal)AArguments[0].Value.AsInt16 /
							(decimal)AArguments[1].Value.AsInt16
						)
					)
				);
		}
    }

	/// <remarks> operator iDiv(short, short) : short </remarks>    
    public class ShortDivNode : InstructionNode
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
						checked
						(
							(short)
							(
								AArguments[0].Value.AsInt16 /
								AArguments[1].Value.AsInt16
							)
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(ushort, ushort) : decimal</remarks>    
    public class UShortDivisionNode : InstructionNode
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
					Scalar.FromDecimal
					(
						(
							(decimal)AArguments[0].Value.AsUInt16() /
							(decimal)AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

	#if UseUnsignedIntegers	
	/// <remarks> operator iDiv(ushort, ushort) : ushort </remarks>    
    public class UShortDivNode : InstructionNode
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
							AArguments[0].Value.AsUInt16() /
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iDivision(integer, integer) : decimal</remarks>    
    public class IntegerDivisionNode : InstructionNode
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
						(decimal)AArguments[0].Value.AsInt32 /
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	/// <remarks> operator iDiv(integer, integer) : integer </remarks>    
    public class IntegerDivNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt32 /
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(uinteger, uinteger) : decimal</remarks>    
    public class UIntegerDivisionNode : InstructionNode
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
					Scalar.FromDecimal
					(
						(decimal)AArguments[0].Value.AsUInt32() /
						(decimal)AArguments[1].Value.AsUInt32()
					)
				);
		}
    }

	/// <remarks> operator iDiv(uinteger, uinteger) : uinteger </remarks>    
    public class UIntegerDivNode : InstructionNode
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
						AArguments[0].Value.AsUInt32() /
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iDivision(long, long) : decimal</remarks>    
    public class LongDivisionNode : InstructionNode
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
						(decimal)AArguments[0].Value.AsInt64 /
						(decimal)AArguments[1].Value.AsInt64
					)
				);
		}
    }

	/// <remarks> operator iDiv(long, long) : long </remarks>    
    public class LongDivNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt64 /
							AArguments[1].Value.AsInt64
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iDivision(ulong, ulong) : decimal</remarks>    
    public class ULongDivisionNode : InstructionNode
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
					Scalar.FromDecimal
					(
						(decimal)AArguments[0].Value.AsUInt64() /
						(decimal)AArguments[1].Value.AsUInt64()
					)
				);
		}
    }

	/// <remarks> operator iDiv(ulong, ulong) : ulong</remarks>    
    public class ULongDivNode : InstructionNode
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
						AArguments[0].Value.AsUInt64() /
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

	#if USEDOUBLE
	/// <remarks> operator iDivision(double, double) : double </remarks>    
    public class DoubleDivisionNode : InstructionNode
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
					Scalar.FromDouble
					(
						AArguments[0].Value.AsDouble() /
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iDivision(decimal, decimal) : decimal </remarks>    
    public class DecimalDivisionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal /
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iDiv(decimal, decimal) : decimal </remarks>    
    public class DecimalDivNode : InstructionNode
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
						(decimal)Math.Floor
						(
							checked
							(
								(double)
								(
									AArguments[0].Value.AsDecimal /
									AArguments[1].Value.AsDecimal
								)
							)
						)
					)
				);
		}
    }

	/// <remarks> operator iDivision(Money, Money) : Decimal </remarks>    
    public class MoneyDivisionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal /
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iDivision(money, decimal) : money </remarks>    
	public class MoneyDecimalDivisionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal /
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iDivision(decimal, money) : money </remarks>    
	public class DecimalMoneyDivisionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal /
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iDivision(money, integer) : money </remarks>    
	public class MoneyIntegerDivisionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal /
						AArguments[1].Value.AsInt32
					)
				);
		}
	}

	/// <remarks> operator iDivision(integer, money) : money </remarks>    
	public class IntegerMoneyDivisionNode : InstructionNode
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
						AArguments[0].Value.AsInt32 /
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}
	
	/// <remarks> operator iMod(byte, byte) : byte </remarks>    
    public class ByteModNode : InstructionNode
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
						(byte)
						(
							AArguments[0].Value.AsByte %
							AArguments[1].Value.AsByte
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(sbyte, sbyte) : sbyte </remarks>    
    public class SByteModNode : InstructionNode
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
							AArguments[0].Value.AsSByte() %
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMod(short, short) : short </remarks>    
    public class ShortModNode : InstructionNode
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
						(short)
						(
							AArguments[0].Value.AsInt16 %
							AArguments[1].Value.AsInt16
						)
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(ushort, ushort) : ushort </remarks>    
    public class UShortModNode : InstructionNode
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
							AArguments[0].Value.AsUInt16() %
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMod(integer, integer) : integer </remarks>    
    public class IntegerModNode : InstructionNode
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
						AArguments[0].Value.AsInt32 %
						AArguments[1].Value.AsInt32
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(uinteger, uinteger) : uinteger </remarks>    
    public class UIntegerModNode : InstructionNode
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
						AArguments[0].Value.AsUInt32() %
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMod(long, long) : long </remarks>    
    public class LongModNode : InstructionNode
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
						AArguments[0].Value.AsInt64 %
						AArguments[1].Value.AsInt64
					)
				);
		}
    }

	#if UseUnsignedIntegers	
	/// <remarks> operator iMod(ulong, ulong) : ulong </remarks>    
    public class ULongModNode : InstructionNode
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
						AArguments[0].Value.AsUInt64() %
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif

	#if USEDOUBLE
	/// <remarks> operator iMod(double, double) : double </remarks>    
    public class DoubleModNode : InstructionNode
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
					Scalar.FromDouble
					(
						AArguments[0].Value.AsDouble() %
						AArguments[1].Value.AsDouble()
					)
				);
		}
    }
    #endif

	/// <remarks> operator iMod(decimal, decimal) : decimal </remarks>    
    public class DecimalModNode : InstructionNode
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
						AArguments[0].Value.AsDecimal %
						AArguments[1].Value.AsDecimal
					)
				);
		}
    }

	/// <remarks> operator iAddition(string, string) : string </remarks>    
	public class StringAdditionNode : InstructionNode
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
						AArguments[0].Value.AsString +
						AArguments[1].Value.AsString
					)
				);
		}
	}
    
	/// <remarks> operator iAddition(byte, byte) : byte </remarks>    
	public class ByteAdditionNode : InstructionNode
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
						checked
						(
							(byte)
							(
								AArguments[0].Value.AsByte +
								AArguments[1].Value.AsByte
							)
						)
					)
				);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(sbyte, sbyte) : sbyte </remarks>    
	public class SByteAdditionNode : InstructionNode
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
							AArguments[0].Value.AsSByte() +
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(short, short) : short </remarks>    
	public class ShortAdditionNode : InstructionNode
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
						checked
						(
							(short)
							(
								AArguments[0].Value.AsInt16 +
								AArguments[1].Value.AsInt16
							)
						)
					)
				);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(ushort, ushort) : ushort </remarks>    
	public class UShortAdditionNode : InstructionNode
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
							AArguments[0].Value.AsUInt16() +
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(integer, integer) : integer </remarks>    
	public class IntegerAdditionNode : InstructionNode
	{
		/*
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt32 + AArguments[1].Value.AsInt32));
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
			else
			#endif
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						checked
						(
							LLeftValue.AsInt32 +
							LRightValue.AsInt32
						)
					)
				);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(uinteger, uinteger) : uinteger </remarks>    
	public class UIntegerAdditionNode : InstructionNode
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
						AArguments[0].Value.AsUInt32() +
						AArguments[1].Value.AsUInt32()
					)
				);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(long, long) : long </remarks>    
	public class LongAdditionNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt64 +
							AArguments[1].Value.AsInt64
						)
					)
				);
		}
	}
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iAddition(ulong, ulong) : ulong </remarks>    
	public class ULongAdditionNode : InstructionNode
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
						AArguments[0].Value.AsUInt64() +
						AArguments[1].Value.AsUInt64()
					)
				);
		}
	}
	#endif

	#if USEDOUBLE
	/// <remarks> operator iAddition(double, double) : double </remarks>    
	public class DoubleAdditionNode : InstructionNode
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
					Scalar.FromDouble
					(
						AArguments[0].Value.AsDouble() +
						AArguments[1].Value.AsDouble()
					)
				);
		}
	}
	#endif
    
	/// <remarks> operator iAddition(decimal, decimal) : decimal </remarks>    
	public class DecimalAdditionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal +
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}
    
	/// <remarks> operator iAddition(money, money) : money </remarks>    
	public class MoneyAdditionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal +
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}
    
	/// <remarks> operator iSubtraction(byte, byte) : byte </remarks>
    public class ByteSubtractionNode : InstructionNode
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
						checked
						(
							(byte)
							(
								AArguments[0].Value.AsByte -
								AArguments[1].Value.AsByte
							)
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iSubtraction(sbyte, sbyte) : sbyte </remarks>
    public class SByteSubtractionNode : InstructionNode
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
							AArguments[0].Value.AsSByte() -
							AArguments[1].Value.AsSByte()
						)
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iSubtraction(short, short) : short </remarks>
    public class ShortSubtractionNode : InstructionNode
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
						checked
						(
							(short)
							(
								AArguments[0].Value.AsInt16 -
								AArguments[1].Value.AsInt16
							)
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iSubtraction(ushort, ushort) : ushort </remarks>
    public class UShortSubtractionNode : InstructionNode
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
							AArguments[0].Value.AsUInt16() -
							AArguments[1].Value.AsUInt16()
						)
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iSubtraction(integer, integer) : integer </remarks>
    public class IntegerSubtractionNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt32 -
							AArguments[1].Value.AsInt32
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers	
	/// <remarks> operator iSubtraction(uinteger, uinteger) : uinteger </remarks>
    public class UIntegerSubtractionNode : InstructionNode
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
						AArguments[0].Value.AsUInt32() -
						AArguments[1].Value.AsUInt32()
					)
				);
		}
    }
    #endif
    
	/// <remarks> operator iSubtraction(long, long) : long </remarks>
    public class LongSubtractionNode : InstructionNode
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
						checked
						(
							AArguments[0].Value.AsInt64 -
							AArguments[1].Value.AsInt64
						)
					)
				);
		}
    }
    
	#if UseUnsignedIntegers
	/// <remarks> operator iSubtraction(ulong, ulong) : ulong </remarks>
    public class ULongSubtractionNode : InstructionNode
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
						AArguments[0].Value.AsUInt64() -
						AArguments[1].Value.AsUInt64()
					)
				);
		}
    }
    #endif
    
	#if USEDOUBLE
	/// <remarks> operator iSubtraction(double, double) : double </remarks>
	public class DoubleSubtractionNode : InstructionNode
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
					Scalar.FromDouble
					(
						AArguments[0].Value.AsDouble() -
						AArguments[1].Value.AsDouble()
					)
				);
		}
	}
	#endif
    
	/// <remarks> operator iSubtraction(decimal, decimal) : decimal </remarks>
	public class DecimalSubtractionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal -
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}

	/// <remarks> operator iSubtraction(money, money) : money </remarks>    
	public class MoneySubtractionNode : InstructionNode
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
						AArguments[0].Value.AsDecimal -
						AArguments[1].Value.AsDecimal
					)
				);
		}
	}
}
