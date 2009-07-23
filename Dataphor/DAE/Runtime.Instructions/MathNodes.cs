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
	
	// operator Floor(AValue : decimal) : decimal;
	// operator Floor(AValue : Money) : Money;
	public class DecimalFloorNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return Decimal.Floor((decimal)AArgument1);
		}
	}
	
	// operator Truncate(AValue : decimal) : decimal;
	// operator Truncate(AValue : Money) : Money;
	public class DecimalTruncateNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return Decimal.Truncate((decimal)AArgument1);
		}
	}

	// operator Ceiling(AValue : decimal) : decimal;
	// operator Ceiling(AValue : Money) : Money;
	public class DecimalCeilingNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			decimal LArgument = (decimal)AArgument1;
			decimal LResult = Decimal.Floor(LArgument);
			if (LResult != LArgument)
				LResult += 1m;
			return LResult;
		}
	}

	// operator Round(AValue : Decimal, ADigits : Integer) : Decimal;
	// operator Round(AValue : Decimal) : Decimal
	// operator Round(AValue : Money, ADigits : Integer) : Money;
	// operator Round(AValue : Money) : Money;
	//TODO: I'm changing this from NewFangledIEEEBankersRound to YeOldElementarySchoolKidsRound (Sadly, mostly to provide continuity with other DBMSs).  We eventually need to have both.
	public class DecimalRoundNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || ((AArguments.Length > 1) && AArguments[1] == null))
				return null;
			#endif
			
			int LDigits = AArguments.Length > 1 ? (int)AArguments[1] : 2;
			decimal LValue = (decimal)AArguments[0];
			return (decimal)(Math.Floor((double)LValue * Math.Pow(10.0, (double)LDigits) + 0.5) / Math.Pow(10.0, (double)LDigits));
		}
	}

	// operator Frac(AValue : decimal) : decimal;	
	// operator Frac(AValue : Money) : Money;	
	public class DecimalFracNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			decimal LArgument = (decimal)AArgument1;
			return LArgument - Decimal.Truncate(LArgument);
		}
	}

	// operator Abs(AValue : decimal) : decimal;	
	// operator Abs(AValue : Money) : Money;	
	public class DecimalAbsNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return Math.Abs((decimal)AArgument1);
		}
	}
	
	// operator Abs(AValue : integer) : integer;
	public class IntegerAbsNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return Math.Abs((int)AArgument1);
		}
	}

	// System Math Operators
	//todo:find (or make) log (decimal) : decimal
	// operator Log(AValue : decimal, ABase : decimal) : decimal;
	public class DecimalLogNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif
			
			return (decimal)Math.Log((double)(decimal)AArgument1, (double)(decimal)AArgument2);
		}
	}

	// operator Ln(AValue : decimal) : decimal;
	public class DecimalLnNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return (decimal)Math.Log((double)(decimal)AArgument1);
		}
	}

	// operator Log10(AValue : decimal) : decimal;
	public class DecimalLog10Node : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return (decimal)Math.Log10((double)(decimal)AArgument1);
		}
	}

	// operator Exp(AValue : decimal) : decimal;
	public class DecimalExpNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return (decimal)Math.Exp((double)(decimal)AArgument1);
		}
	}

	// operator Factorial(AValue : integer) : integer;
	public class IntegerFactorialNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			int LReturnVal = 1;
			int LValue = (int)AArgument1;

			for (int i = 2; i <= LValue; i++)
			{
				checked
				{
					LReturnVal *= i;
				} //larger than 12 will overflow.
			}
			return LReturnVal;
		}
	}

	public class SeedNode : InstructionNode
	{
		private static Random FRandom;
		public static decimal NextDecimal()
		{
			lock (typeof(SeedNode))
			{
				if (FRandom == null)
					FRandom = new Random();
				return (decimal)FRandom.NextDouble();
			}
		}
		
		public static int Next(int ALowerBound, int AUpperBound)
		{
			lock (typeof(SeedNode))
			{
				if (FRandom == null)
					FRandom = new Random();
				return FRandom.Next(ALowerBound, AUpperBound);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			lock (typeof(SeedNode))
			{
				if (AArguments.Length == 0)
					FRandom = new Random();
				else
					FRandom = new Random((int)AArguments[0]);
			}
			return null;
		}
	}

	// operator Random() : Decimal
	public class DecimalRandomNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			return SeedNode.NextDecimal();
		}
	}
												
	// operator Random(const ACount : Integer) : Integer
	public class IntegerRandomNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return SeedNode.Next(0, (int)AArgument1);
		}
	}

	// operator Random(const ALowerBound : Integer, const AUpperBound : Integer) : Integer;	
	public class IntegerIntegerRandomNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif
			
			return SeedNode.Next((int)AArgument1, (int)AArgument2 + 1);
		}
	}
}

