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
    public class BooleanAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)AArgument1);
		}
    }
    
    // BooleanAsStringReadAccessorNode
    public class BooleanAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((bool)AArgument1).ToString().ToLower();
		}
    }
    
    // BooleanAsStringWriteAccessorNode
    public class BooleanAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)AArgument2);
		}
    }   

    // BooleanAsDisplayStringSelectorNode
    public class BooleanAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)AArgument1);
		}
    }
    
    // BooleanAsDisplayStringReadAccessorNode
    public class BooleanAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((bool)AArgument1).ToString();
		}
    }
    
    // BooleanAsDisplayStringWriteAccessorNode
    public class BooleanAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)AArgument2);
		}
    }   

    // ByteAsStringSelectorNode
    public class ByteAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToByte((string)AArgument1);
		}
    }
    
    // ByteAsStringReadAccessorNode
    public class ByteAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((byte)AArgument1).ToString();
		}
    }
    
    // ByteAsStringWriteAccessorNode
    public class ByteAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToByte((string)AArgument2);
		}
    }   

    // ShortAsStringSelectorNode
    public class ShortAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToInt16((string)AArgument1);
		}
    }
    
    // ShortAsStringReadAccessorNode
    public class ShortAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((short)AArgument1).ToString();
		}
    }
    
    // ShortAsStringWriteAccessorNode
    public class ShortAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToInt16((string)AArgument2);
		}
    }   

    // IntegerAsStringSelectorNode
    public class IntegerAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToInt32((string)AArgument1);
		}
    }
    
    // IntegerAsStringReadAccessorNode
    public class IntegerAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((int)AArgument1).ToString();
		}
    }
    
    // IntegerAsStringWriteAccessorNode
    public class IntegerAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToInt32((string)AArgument2);
		}
    }   

    // LongAsStringSelectorNode
    public class LongAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToInt64((string)AArgument1);
		}
    }
    
    // LongAsStringReadAccessorNode
    public class LongAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((long)AArgument1).ToString();
		}
    }
    
    // LongAsStringWriteAccessorNode
    public class LongAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToInt64((string)AArgument2);
		}
    }   

    // DecimalAsStringSelectorNode
    public class DecimalAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Convert.ToDecimal((string)AArgument1);
		}
    }
    
    // DecimalAsStringReadAccessorNode
    public class DecimalAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((decimal)AArgument1).ToString();
		}
    }
    
    // DecimalAsStringWriteAccessorNode
    public class DecimalAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Convert.ToDecimal((string)AArgument2);
		}
    }   

    // GuidAsStringSelectorNode
    public class GuidAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return new Guid((string)AArgument1);
		}
    }
    
    // GuidAsStringReadAccessorNode
    public class GuidAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((Guid)AArgument1).ToString();
		}
    }
    
    // GuidAsStringWriteAccessorNode
    public class GuidAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return new Guid((string)AArgument2);
		}
    }   

    // MoneyAsStringSelectorNode
    public class MoneyAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Decimal.Parse((string)AArgument1, System.Globalization.NumberStyles.Currency);
		}
    }
    
    // MoneyAsStringReadAccessorNode
    public class MoneyAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((decimal)AArgument1).ToString();
		}
    }
    
    // MoneyAsDisplayStringReadAccessorNode
    public class MoneyAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((decimal)AArgument1).ToString("C");
		}
    }
    
    // MoneyAsStringWriteAccessorNode
    public class MoneyAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return Decimal.Parse((string)AArgument2, System.Globalization.NumberStyles.Currency);
		}
    }   

    // DateTimeAsStringSelectorNode
    public class DateTimeAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return DateTime.Parse((string)AArgument1);
		}
    }
    
    // DateTimeAsStringReadAccessorNode
    public class DateTimeAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((DateTime)AArgument1).ToString("G");
		}
    }
    
    // DateTimeAsStringWriteAccessorNode
    public class DateTimeAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return DateTime.Parse((string)AArgument2);
		}
    }   

    // DateAsStringSelectorNode
    public class DateAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return DateTime.Parse((string)AArgument1).Date;
		}
    }
    
    // DateAsStringReadAccessorNode
    public class DateAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((DateTime)AArgument1).ToString("d");
		}
    }
    
    // DateAsStringWriteAccessorNode
    public class DateAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return DateTime.Parse((string)AArgument2).Date;
		}
    }   

    // TimeSpanAsStringSelectorNode
    public class TimeSpanAsStringSelectorNode : UnaryInstructionNode
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

		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return StringToTimeSpan((string)AArgument1);
		}
    }
    
    // TimeSpanAsStringReadAccessorNode
    public class TimeSpanAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return TimeSpanAsStringSelectorNode.TimeSpanToString(((TimeSpan)AArgument1));
		}
    }
    
    // TimeSpanAsStringWriteAccessorNode
    public class TimeSpanAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return TimeSpanAsStringSelectorNode.StringToTimeSpan((string)AArgument2);
		}
    }   

    // TimeAsStringSelectorNode
    public class TimeAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return new DateTime(DateTime.Parse((string)AArgument1).TimeOfDay.Ticks);
		}
    }
    
    // TimeAsStringReadAccessorNode
    public class TimeAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return ((DateTime)AArgument1).ToString("T");
		}
    }
    
    // TimeAsStringWriteAccessorNode
    public class TimeAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			return new DateTime(DateTime.Parse((string)AArgument2).TimeOfDay.Ticks);
		}
    }   

    // BinaryAsDisplayStringSelectorNode
    public class BinaryAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Binary");
		}
    }
    
    // BinaryAsDisplayStringReadAccessorNode
    public class BinaryAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Strings.Get("BinaryAsDisplayStringReadAccessorNode.BinaryData");
		}
    }
    
    // BinaryAsDisplayStringWriteAccessorNode
    public class BinaryAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Binary");
		}
    }   

    // GraphicAsDisplayStringSelectorNode
    public class GraphicAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Graphic");
		}
    }
    
    // GraphicAsDisplayStringReadAccessorNode
    public class GraphicAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			return Strings.Get("GraphicAsDisplayStringReadAccessorNode.GraphicData");
		}
    }
    
    // GraphicAsDisplayStringWriteAccessorNode
    public class GraphicAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Graphic");
		}
    }   
}