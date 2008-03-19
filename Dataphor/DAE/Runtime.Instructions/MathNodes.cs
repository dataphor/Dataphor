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
	public class DecimalFloorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Decimal.Floor(AArguments[0].Value.AsDecimal)));
		}
	}
	
	// operator Truncate(AValue : decimal) : decimal;
	// operator Truncate(AValue : Money) : Money;
	public class DecimalTruncateNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Decimal.Truncate(AArguments[0].Value.AsDecimal)));
		}
	}

	// operator Ceiling(AValue : decimal) : decimal;
	// operator Ceiling(AValue : Money) : Money;
	public class DecimalCeilingNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			decimal LArgument = AArguments[0].Value.AsDecimal;
			decimal LResult = Decimal.Floor(LArgument);
			if (LResult != LArgument)
				LResult += 1m;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LResult));
		}
	}

	// operator Round(AValue : Decimal, ADigits : Integer) : Decimal;
	// operator Round(AValue : Decimal) : Decimal
	// operator Round(AValue : Money, ADigits : Integer) : Money;
	// operator Round(AValue : Money) : Money;
	//TODO: I'm changing this from NewFangledIEEEBankersRound to YeOldElementarySchoolKidsRound (Sadly, mostly to provide continuity with other DBMSs).  We eventually need to have both.
	public class DecimalRoundNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || ((AArguments.Length > 1) && ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)))
				return new DataVar(FDataType, null);
			#endif
			
			int LDigits = AArguments.Length > 1 ? AArguments[1].Value.AsInt32 : 2;
			decimal LValue = AArguments[0].Value.AsDecimal;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)(Math.Floor((double)LValue * Math.Pow(10.0, (double)LDigits) + 0.5) / Math.Pow(10.0, (double)LDigits))));
		}
	}

	// operator Frac(AValue : decimal) : decimal;	
	// operator Frac(AValue : Money) : Money;	
	public class DecimalFracNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			decimal LArgument = AArguments[0].Value.AsDecimal;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LArgument - Decimal.Truncate(LArgument)));
		}
	}

	// operator Abs(AValue : decimal) : decimal;	
	// operator Abs(AValue : Money) : Money;	
	public class DecimalAbsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Math.Abs(AArguments[0].Value.AsDecimal)));
		}
	}
	
	// operator Abs(AValue : integer) : integer;
	public class IntegerAbsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Math.Abs(AArguments[0].Value.AsInt32)));
		}
	}


	// System Math Operators
	//todo:find (or make) log (decimal) : decimal
	// operator Log(AValue : decimal, ABase : decimal) : decimal;
	public class DecimalLogNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			decimal LValue = AArguments[0].Value.AsDecimal;
			decimal LBase = AArguments[1].Value.AsDecimal;
			return new DataVar(FDataType, new Scalar(AProcess,  AProcess.DataTypes.SystemDecimal, (decimal)Math.Log((double)LValue, (double)LBase) ));
		}
	}
	// operator Ln(AValue : decimal) : decimal;
	public class DecimalLnNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemDecimal, (decimal)Math.Log((double)AArguments[0].Value.AsDecimal)));
		}
	}

	// operator Log10(AValue : decimal) : decimal;
	public class DecimalLog10Node : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemDecimal, (decimal)Math.Log10((double)AArguments[0].Value.AsDecimal)));
		}
	}

	// operator Exp(AValue : decimal) : decimal;
	public class DecimalExpNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemDecimal, (decimal)Math.Exp((double)AArguments[0].Value.AsDecimal)));
		}
	}

	// operator Factorial(AValue : integer) : integer;
	public class IntegerFactorialNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			int LReturnVal = 1;
			int LValue = AArguments[0].Value.AsInt32;

			for (int i = 2; i <= LValue; i++)
			{
				checked
				{
					LReturnVal *= i;
				} //larger than 12 will overflow.
			}
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemInteger, LReturnVal));
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (typeof(SeedNode))
			{
				if (AArguments.Length == 0)
					FRandom = new Random();
				else
					FRandom = new Random(AArguments[0].Value.AsInt32);
			}
			return null;
		}
	}

	// operator Random() : Decimal
	public class DecimalRandomNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemDecimal, SeedNode.NextDecimal()));
		}
	}
												
	// operator Random(const ACount : Integer) : Integer
	public class IntegerRandomNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemInteger, SeedNode.Next(0, AArguments[0].Value.AsInt32)));
		}
	}

	// operator Random(const ALowerBound : Integer, const AUpperBound : Integer) : Integer;	
	public class IntegerIntegerRandomNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemInteger, SeedNode.Next(AArguments[0].Value.AsInt32, AArguments[1].Value.AsInt32 + 1)));
		}
	}
}

