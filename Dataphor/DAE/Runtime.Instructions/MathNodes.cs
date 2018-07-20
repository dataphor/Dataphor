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
	using Sigil;

	// operator Floor(AValue : decimal) : decimal;
	// operator Floor(AValue : Money) : Money;
	public class DecimalFloorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return Decimal.Floor((decimal)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Decimal).GetMethod("Floor", new[] { typeof(decimal) }));
		}
	}

	// operator Truncate(AValue : decimal) : decimal;
	// operator Truncate(AValue : Money) : Money;
	public class DecimalTruncateNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return Decimal.Truncate((decimal)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Decimal).GetMethod("Truncate", new[] { typeof(decimal) }));
		}
	}

	// operator Ceiling(AValue : decimal) : decimal;
	// operator Ceiling(AValue : Money) : Money;
	public class DecimalCeilingNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return StaticExecute((decimal)argument1);
		}

		public static decimal StaticExecute(decimal argument)
		{
			decimal result = Decimal.Floor(argument);
			if (result != argument)
				result += 1m;
			return result;
		}
	}

	// operator Round(AValue : Decimal, ADigits : Integer) : Decimal;
	// operator Round(AValue : Decimal) : Decimal
	// operator Round(AValue : Money, ADigits : Integer) : Money;
	// operator Round(AValue : Money) : Money;
	//TODO: I'm changing this from NewFangledIEEEBankersRound to YeOldElementarySchoolKidsRound (Sadly, mostly to provide continuity with other DBMSs).  We eventually need to have both.
	public class DecimalRoundNode : InstructionNode
	{
		public const int DefaultDigits = 2;

		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || ((arguments.Length > 1) && arguments[1] == null))
				return null;
			#endif

			int digits = arguments.Length > 1 ? (int)arguments[1] : DefaultDigits;
			return StaticExecute((decimal)arguments[0], digits);
		}

		public static decimal StaticExecute(decimal tempValue, int digits)
		{
			return (decimal)(Math.Floor((double)tempValue * Math.Pow(10.0, (double)digits) + 0.5) / Math.Pow(10.0, (double)digits));
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			if (Nodes.Count == 1)
				m.IL.LoadConstant(DefaultDigits);
			EmitCallStaticExecute(m);
		}
	}

	// operator Frac(AValue : decimal) : decimal;	
	// operator Frac(AValue : Money) : Money;	
	public class DecimalFracNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			decimal argument = (decimal)argument1;
			return argument - Decimal.Truncate(argument);
		}

		public override bool CanEmitIL => true;

		public override ArgumentEmissionStyle ArgumentEmissionStyle => ArgumentEmissionStyle.NativeInLocals;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.LoadLocal(arguments[0]);
			m.IL.LoadLocal(arguments[0]);
			m.IL.Call(typeof(Decimal).GetMethod("Truncate", new[] { typeof(decimal) }));
			m.IL.Call(typeof(Decimal).GetMethod("op_Subtraction", new[] { typeof(decimal), typeof(decimal) }));
		}
	}

	// operator Abs(AValue : decimal) : decimal;	
	// operator Abs(AValue : Money) : Money;	
	public class DecimalAbsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return Math.Abs((decimal)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Math).GetMethod("Abs", new[] { typeof(decimal) }));
		}
	}

	// operator Abs(AValue : integer) : integer;
	public class IntegerAbsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return Math.Abs((int)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Math).GetMethod("Abs", new[] { typeof(int) }));
		}
	}

	// System Math Operators
	//todo:find (or make) log (decimal) : decimal
	// operator Log(AValue : decimal, ABase : decimal) : decimal;
	public class DecimalLogNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif
			
			return (decimal)Math.Log((double)(decimal)argument1, (double)(decimal)argument2);
		}

		public override bool CanEmitIL => true;

		protected override void EmitPostArgument(NativeMethod m, int index)
		{
			m.IL.Call(MethodInfoStatics.DecimalToDoubleConversion);
			m.IL.Convert<double>();
		}

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Math).GetMethod("Log", new[] { typeof(double), typeof(double) }));
			m.IL.Call(typeof(Decimal).GetMethod("op_Explicit", new[] { typeof(double) }));
		}
	}

	// operator Ln(AValue : decimal) : decimal;
	public class DecimalLnNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return (decimal)Math.Log((double)(decimal)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitPostArgument(NativeMethod m, int index)
		{
			m.IL.Call(MethodInfoStatics.DecimalToDoubleConversion);
			m.IL.Convert<double>();
		}

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Math).GetMethod("Log", new[] { typeof(double) }));
			m.IL.Call(typeof(Decimal).GetMethod("op_Explicit", new[] { typeof(double) }));
		}
	}

	// operator Log10(AValue : decimal) : decimal;
	public class DecimalLog10Node : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return (decimal)Math.Log10((double)(decimal)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitPostArgument(NativeMethod m, int index)
		{
			m.IL.Call(MethodInfoStatics.DecimalToDoubleConversion);
			m.IL.Convert<double>();
		}

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Math).GetMethod("Log10", new[] { typeof(double) }));
			m.IL.Call(typeof(Decimal).GetMethod("op_Explicit", new[] { typeof(double) }));
		}
	}

	// operator Exp(AValue : decimal) : decimal;
	public class DecimalExpNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return (decimal)Math.Exp((double)(decimal)argument1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitPostArgument(NativeMethod m, int index)
		{
			m.IL.Call(MethodInfoStatics.DecimalToDoubleConversion);
			m.IL.Convert<double>();
		}

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(Math).GetMethod("Exp", new[] { typeof(double) }));
			m.IL.Call(typeof(Decimal).GetMethod("op_Explicit", new[] { typeof(double) }));
		}
	}

	// operator Factorial(AValue : integer) : integer;
	public class IntegerFactorialNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return StaticExecute((int)argument1);
		}

		public static int StaticExecute(int tempValue)
		{
			int returnVal = 1;

			for (int i = 2; i <= tempValue; i++)
			{
				checked
				{
					returnVal *= i;
				} //larger than 12 will overflow.
			}
			return returnVal;
		}
	}

	public class SeedNode : InstructionNode
	{
		private static Random _random;
		public static decimal NextDecimal()
		{
			lock (typeof(SeedNode))
			{
				if (_random == null)
					_random = new Random();
				return (decimal)_random.NextDouble();
			}
		}
		
		public static int Next(int lowerBound, int upperBound)
		{
			lock (typeof(SeedNode))
			{
				if (_random == null)
					_random = new Random();
				return _random.Next(lowerBound, upperBound);
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 0)
				StaticExecute();
			else
				StaticExecute((int)arguments[0]);
			return null;
		}

		public static void StaticExecute()
		{
			lock (typeof(SeedNode))
			{
				_random = new Random();
			}
		}

		public static void StaticExecute(int seed)
		{
			lock (typeof(SeedNode))
			{
				_random = new Random(seed);
			}
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			if (Nodes.Count == 0)
				m.IL.Call(GetType().GetMethod("StaticExecute", new Type[0]));
			else
				m.IL.Call(GetType().GetMethod("StaticExecute", new[] { typeof(int) }));
			m.IL.LoadNull();
		}
	}

	// operator Random() : Decimal
	public class DecimalRandomNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			return SeedNode.NextDecimal();
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.Call(typeof(SeedNode).GetMethod("NextDecimal", new Type[0]));
		}
	}

	// operator Random(const ACount : Integer) : Integer
	public class IntegerRandomNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return SeedNode.Next(0, (int)argument1);
		}

		public override bool CanEmitIL => true;

		public override ArgumentEmissionStyle ArgumentEmissionStyle => ArgumentEmissionStyle.NativeInLocals;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.LoadConstant(0);
			m.IL.LoadLocal(arguments[0]);
			m.IL.Call(typeof(SeedNode).GetMethod("Next", new[] { typeof(int), typeof(int) }));
		}
	}

	// operator Random(const ALowerBound : Integer, const AUpperBound : Integer) : Integer;	
	public class IntegerIntegerRandomNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif
			
			return SeedNode.Next((int)argument1, (int)argument2 + 1);
		}

		public override bool CanEmitIL => true;

		protected override void EmitInstructionOperation(NativeMethod m, Local[] arguments)
		{
			m.IL.LoadConstant(1);
			m.IL.Add();
			m.IL.Call(typeof(SeedNode).GetMethod("Next", new[] { typeof(int), typeof(int) }));
		}
	}
}

