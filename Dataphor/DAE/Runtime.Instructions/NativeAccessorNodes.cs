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

    // BooleanAsStringSelectorNode
    public class BooleanAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToBoolean(AArguments[0].Value.AsString)));
		}
    }
    
    // BooleanAsStringReadAccessorNode
    public class BooleanAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsBoolean.ToString().ToLower()));
		}
    }
    
    // BooleanAsStringWriteAccessorNode
    public class BooleanAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToBoolean(AArguments[1].Value.AsString)));
		}
    }   

    // BooleanAsDisplayStringSelectorNode
    public class BooleanAsDisplayStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToBoolean(AArguments[0].Value.AsString)));
		}
    }
    
    // BooleanAsDisplayStringReadAccessorNode
    public class BooleanAsDisplayStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsBoolean.ToString()));
		}
    }
    
    // BooleanAsDisplayStringWriteAccessorNode
    public class BooleanAsDisplayStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToBoolean(AArguments[1].Value.AsString)));
		}
    }   

    // ByteAsStringSelectorNode
    public class ByteAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToByte(AArguments[0].Value.AsString)));
		}
    }
    
    // ByteAsStringReadAccessorNode
    public class ByteAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsByte.ToString()));
		}
    }
    
    // ByteAsStringWriteAccessorNode
    public class ByteAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToByte(AArguments[1].Value.AsString)));
		}
    }   

    // ShortAsStringSelectorNode
    public class ShortAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt16(AArguments[0].Value.AsString)));
		}
    }
    
    // ShortAsStringReadAccessorNode
    public class ShortAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt16.ToString()));
		}
    }
    
    // ShortAsStringWriteAccessorNode
    public class ShortAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt16(AArguments[1].Value.AsString)));
		}
    }   

    // IntegerAsStringSelectorNode
    public class IntegerAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt32(AArguments[0].Value.AsString)));
		}
    }
    
    // IntegerAsStringReadAccessorNode
    public class IntegerAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt32.ToString()));
		}
    }
    
    // IntegerAsStringWriteAccessorNode
    public class IntegerAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt32(AArguments[1].Value.AsString)));
		}
    }   

    // LongAsStringSelectorNode
    public class LongAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt64(AArguments[0].Value.AsString)));
		}
    }
    
    // LongAsStringReadAccessorNode
    public class LongAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsInt64.ToString()));
		}
    }
    
    // LongAsStringWriteAccessorNode
    public class LongAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToInt64(AArguments[1].Value.AsString)));
		}
    }   

    // DecimalAsStringSelectorNode
    public class DecimalAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToDecimal(AArguments[0].Value.AsString)));
		}
    }
    
    // DecimalAsStringReadAccessorNode
    public class DecimalAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal.ToString()));
		}
    }
    
    // DecimalAsStringWriteAccessorNode
    public class DecimalAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Convert.ToDecimal(AArguments[1].Value.AsString)));
		}
    }   

    // GuidAsStringSelectorNode
    public class GuidAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Guid(AArguments[0].Value.AsString)));
		}
    }
    
    // GuidAsStringReadAccessorNode
    public class GuidAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsGuid.ToString()));
		}
    }
    
    // GuidAsStringWriteAccessorNode
    public class GuidAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Guid(AArguments[1].Value.AsString)));
		}
    }   

    // MoneyAsStringSelectorNode
    public class MoneyAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Decimal.Parse(AArguments[0].Value.AsString, System.Globalization.NumberStyles.Currency)));
		}
    }
    
    // MoneyAsStringReadAccessorNode
    public class MoneyAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal.ToString()));
		}
    }
    
    // MoneyAsDisplayStringReadAccessorNode
    public class MoneyAsDisplayStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDecimal.ToString("C")));
		}
    }
    
    // MoneyAsStringWriteAccessorNode
    public class MoneyAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Decimal.Parse(AArguments[1].Value.AsString, System.Globalization.NumberStyles.Currency)));
		}
    }   

    // DateTimeAsStringSelectorNode
    public class DateTimeAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Parse(AArguments[0].Value.AsString)));
		}
    }
    
    // DateTimeAsStringReadAccessorNode
    public class DateTimeAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.ToString("G")));
		}
    }
    
    // DateTimeAsStringWriteAccessorNode
    public class DateTimeAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Parse(AArguments[1].Value.AsString)));
		}
    }   

    // DateAsStringSelectorNode
    public class DateAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Parse(AArguments[0].Value.AsString).Date));
		}
    }
    
    // DateAsStringReadAccessorNode
    public class DateAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.ToString("d")));
		}
    }
    
    // DateAsStringWriteAccessorNode
    public class DateAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Parse(AArguments[1].Value.AsString).Date));
		}
    }   

    // TimeSpanAsStringSelectorNode
    public class TimeSpanAsStringSelectorNode : InstructionNode
    {
		public static TimeSpan StringToTimeSpan(string AValue)
		{
			try
			{
				return TimeSpan.Parse(AValue);
			}
			catch
			{
				return CustomParse(AValue);
			}
		}

		public static TimeSpan CustomParse(string AValue)
		{ 
			bool LIsNegative = false;
			int LWeeks = 0;
			int LDays = 0;
			int LHours = 0;
			int LMinutes = 0;
			int LSeconds = 0;
			int LMilliseconds = 0;
			int LNanoseconds = 0;
			long LTicks = 0;
			bool LHasWeeks = false;
			bool LHasDays = false;
			bool LHasHours = false;
			bool LHasMinutes = false;
			bool LHasSeconds = false;
			bool LHasMilliseconds = false;
			bool LHasNanoseconds = false;
			 
			System.Text.StringBuilder LString = new System.Text.StringBuilder(AValue.ToLower());
			
			LString.Replace("ms","x");
			LString.Replace("mil","x");
			int i = 0;
			while (i < LString.Length)
			{
				if (LString[i] == ' ')                       
					LString.Remove(i,1);
				else if (LString[i] == '-')                  
				{
					if (i == 0 && LIsNegative == false)
					{
						LIsNegative = true;
						LString.Remove(i,1);
					}
					else
						throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
				}
				else if (i == 0 && ((int)LString[i] < 48 || (int)LString[i] > 57))
					throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
				else if ((int)LString[i] >= 48 && (int)LString[i] <= 57)
					i++;
				else if ((int)LString[i] < 48 || (int)LString[i] > 57)
				{
					if ((((int)LString[i - 1]) >= 48) && (((int)LString[i - 1]) <= 57))
					{
						if 
						(
							LString[i] == 'w' || LString[i] == 'd' || LString[i] == 'h' || 
							LString[i] == 'm' || LString[i] == 's' || LString[i] == 'n' ||
							LString[i] == 'x'
						)
							i++;	 
						else
							throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	 
					}
					else
						LString.Remove(i,1);
				}
			}
			
			if 
			(
				(LString.Length == 0) ||
				(
					LString[LString.Length - 1] != 'w' && LString[LString.Length - 1] != 'd' &&
					LString[LString.Length - 1] != 'h' && LString[LString.Length - 1] != 'm' &&
					LString[LString.Length - 1] != 's' && LString[LString.Length - 1] != 'x' &&
					LString[LString.Length - 1] != 'n'
				)
			)
				throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	 

			i = 0;
			while (LString.Length > 0)
			{
				if (((int)LString[i] >= 48) && ((int)LString[i] <= 57))
					i++;
				else
				{
					switch (LString[i])
					{
						case 'w':
							if (!(LHasWeeks))
							{
								LWeeks = Convert.ToInt32(LString.ToString().Substring(0,i));
								LHasWeeks = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
							break;
						case 'd':
							if (!(LHasDays))
							{	 
								LDays = Convert.ToInt32(LString.ToString().Substring(0,i));
								LHasDays = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
							break;
						case 'h':
							if (!(LHasHours))
							{	
								LHours = Convert.ToInt32(LString.ToString().Substring(0,i));
								LHasHours = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	 
							break;
						case 'm':
							if (!(LHasMinutes))
							{	 
								LMinutes = Convert.ToInt32(LString.ToString().Substring(0,i));
								LHasMinutes = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	 
							break;
						case 's':
							if (!(LHasSeconds))
							{	
								LSeconds = Convert.ToInt32(LString.ToString().Substring(0,i));
								LHasSeconds = true;
							} 
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
							break;
						case 'x':
							if (!(LHasMilliseconds))
							{
								LMilliseconds = Convert.ToInt32(LString.ToString().Substring(0,i));
								LHasMilliseconds = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
							break;
						case 'n':
							if (!(LHasNanoseconds))
							{
								LNanoseconds = Convert.ToInt32(LString.ToString().Substring(0,i));
								if (LNanoseconds % 100 != 0)
									throw new ConveyorException(ConveyorException.Codes.InvalidNanosecondArgument, AValue);
								LHasNanoseconds = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, AValue);	
							break;
					}
					LString.Remove(0,i + 1);
					i = 0;
				}
			}
			LTicks = 
				LWeeks * TimeSpan.TicksPerDay * 7 +
				LDays * TimeSpan.TicksPerDay +
				LHours * TimeSpan.TicksPerHour +
				LMinutes * TimeSpan.TicksPerMinute + 
				LSeconds * TimeSpan.TicksPerSecond +
				LMilliseconds * TimeSpan.TicksPerMillisecond +
				LNanoseconds / 100;

			return LIsNegative ? (new TimeSpan(LTicks)).Negate() : new TimeSpan(LTicks);
		}

		public static string TimeSpanToString(TimeSpan AValue)
		{
			const string CWeeks = "wks ";
			const string CDays = "days ";
			const string CHours = "hrs ";
			const string CMinutes = "min ";
			const string CSeconds = "sec ";
			const string CMilliseconds = "mil ";
			const string CNanoseconds = "nan ";
			String LTimeSpan = String.Empty;
			bool LLeft = false;
			int LWeeks = AValue.Days / 7;
			int LDays = AValue.Days % 7;
			int LHours = AValue.Hours;
			int LMinutes = AValue.Minutes;
			int LSeconds = AValue.Seconds;
			int LMilseconds = AValue.Milliseconds;
			long LNanoseconds = ((AValue.Ticks % 10000) * 100);
 
			if (LWeeks != 0)
			{
				LLeft = true;
				LTimeSpan = LTimeSpan + LWeeks.ToString() + CWeeks;
			}
			if (LLeft)
			{
				if (LDays + LHours + LMinutes + LSeconds + LMilseconds + LNanoseconds != 0)
				{
					LDays = System.Math.Abs(LDays);
					LTimeSpan = LTimeSpan + LDays.ToString() + CDays;
				}
			}
			else 
				if (LDays != 0)
				{
					LLeft = true;
					LTimeSpan = LTimeSpan + LDays.ToString() + CDays;
				}
			if (LLeft)
			{
				if (LHours + LMinutes + LSeconds + LMilseconds + LNanoseconds != 0)
				{
					LHours = System.Math.Abs(LHours);
					LTimeSpan = LTimeSpan + LHours.ToString() + CHours;
				}
			}
			else 
				if (LHours != 0)
				{
					LLeft = true;
					LTimeSpan = LTimeSpan + LHours.ToString() + CHours;
				}
			if (LLeft)
			{
				if (LMinutes + LSeconds + LMilseconds + LNanoseconds != 0)
				{
					LMinutes = System.Math.Abs(LMinutes);
					LTimeSpan = LTimeSpan + LMinutes.ToString() + CMinutes;
				}
			}
			else 
				if (LMinutes != 0)
				{ 
					LLeft = true;
					LTimeSpan = LTimeSpan + LMinutes.ToString() + CMinutes; 
				}
			if (LLeft)
			{
				if (LSeconds + LMilseconds + LNanoseconds != 0)
				{
					LSeconds = System.Math.Abs(LSeconds);
					LTimeSpan = LTimeSpan + LSeconds.ToString() + CSeconds;
				}
			}
			else 
				if (LSeconds != 0)
				{
					LLeft = true;
					LTimeSpan = LTimeSpan + LSeconds.ToString() + CSeconds;	 
				}
			if (LLeft)
			{
				if (LMilseconds + LNanoseconds != 0)
				{
					LMilseconds = System.Math.Abs(LMilseconds);
					LTimeSpan = LTimeSpan + LMilseconds.ToString() + CMilliseconds;
				}
			}
			else 
				if (LMilseconds != 0)
				{
					LLeft = true;
					LTimeSpan = LTimeSpan + LMilseconds.ToString() + CMilliseconds;
				}

			if (LLeft && (LNanoseconds != 0))
			{
				LNanoseconds = System.Math.Abs(LNanoseconds);
				LTimeSpan = LTimeSpan + LNanoseconds.ToString() + CNanoseconds;
			}
			else if (!(LLeft) && (LNanoseconds == 0))
				LTimeSpan = LTimeSpan + LNanoseconds.ToString();
			else if (LNanoseconds != 0)
			{
				LLeft = true;
				LTimeSpan = LTimeSpan + LNanoseconds.ToString() + CNanoseconds;
			}
			return LTimeSpan;
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, StringToTimeSpan(AArguments[0].Value.AsString)));
		}
    }
    
    // TimeSpanAsStringReadAccessorNode
    public class TimeSpanAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpanAsStringSelectorNode.TimeSpanToString(AArguments[0].Value.AsTimeSpan)));
		}
    }
    
    // TimeSpanAsStringWriteAccessorNode
    public class TimeSpanAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpanAsStringSelectorNode.StringToTimeSpan(AArguments[1].Value.AsString)));
		}
    }   

    // TimeAsStringSelectorNode
    public class TimeAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(DateTime.Parse(AArguments[0].Value.AsString).TimeOfDay.Ticks)));
		}
    }
    
    // TimeAsStringReadAccessorNode
    public class TimeAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.ToString("T")));
		}
    }
    
    // TimeAsStringWriteAccessorNode
    public class TimeAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(DateTime.Parse(AArguments[1].Value.AsString).TimeOfDay.Ticks)));
		}
    }   

    // BinaryAsDisplayStringSelectorNode
    public class BinaryAsDisplayStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Binary");
		}
    }
    
    // BinaryAsDisplayStringReadAccessorNode
    public class BinaryAsDisplayStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Strings.Get("BinaryAsDisplayStringReadAccessorNode.BinaryData")));
		}
    }
    
    // BinaryAsDisplayStringWriteAccessorNode
    public class BinaryAsDisplayStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Binary");
		}
    }   

    // GraphicAsDisplayStringSelectorNode
    public class GraphicAsDisplayStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Graphic");
		}
    }
    
    // GraphicAsDisplayStringReadAccessorNode
    public class GraphicAsDisplayStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Strings.Get("GraphicAsDisplayStringReadAccessorNode.GraphicData")));
		}
    }
    
    // GraphicAsDisplayStringWriteAccessorNode
    public class GraphicAsDisplayStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Graphic");
		}
    }   
}