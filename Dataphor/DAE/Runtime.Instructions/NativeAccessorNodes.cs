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
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)argument1);
		}
    }
    
    // BooleanAsStringReadAccessorNode
    public class BooleanAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((bool)argument1).ToString().ToLower();
		}
    }
    
    // BooleanAsStringWriteAccessorNode
    public class BooleanAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)argument2);
		}
    }   

    // BooleanAsDisplayStringSelectorNode
    public class BooleanAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)argument1);
		}
    }
    
    // BooleanAsDisplayStringReadAccessorNode
    public class BooleanAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((bool)argument1).ToString();
		}
    }
    
    // BooleanAsDisplayStringWriteAccessorNode
    public class BooleanAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToBoolean((string)argument2);
		}
    }   

    // ByteAsStringSelectorNode
    public class ByteAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToByte((string)argument1);
		}
    }
    
    // ByteAsStringReadAccessorNode
    public class ByteAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((byte)argument1).ToString();
		}
    }
    
    // ByteAsStringWriteAccessorNode
    public class ByteAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToByte((string)argument2);
		}
    }   

    // ShortAsStringSelectorNode
    public class ShortAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToInt16((string)argument1);
		}
    }
    
    // ShortAsStringReadAccessorNode
    public class ShortAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((short)argument1).ToString();
		}
    }
    
    // ShortAsStringWriteAccessorNode
    public class ShortAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToInt16((string)argument2);
		}
    }   

    // IntegerAsStringSelectorNode
    public class IntegerAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToInt32((string)argument1);
		}
    }
    
    // IntegerAsStringReadAccessorNode
    public class IntegerAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((int)argument1).ToString();
		}
    }
    
    // IntegerAsStringWriteAccessorNode
    public class IntegerAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToInt32((string)argument2);
		}
    }   

    // LongAsStringSelectorNode
    public class LongAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToInt64((string)argument1);
		}
    }
    
    // LongAsStringReadAccessorNode
    public class LongAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((long)argument1).ToString();
		}
    }
    
    // LongAsStringWriteAccessorNode
    public class LongAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToInt64((string)argument2);
		}
    }   

    // DecimalAsStringSelectorNode
    public class DecimalAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Convert.ToDecimal((string)argument1);
		}
    }
    
    // DecimalAsStringReadAccessorNode
    public class DecimalAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((decimal)argument1).ToString();
		}
    }
    
    // DecimalAsStringWriteAccessorNode
    public class DecimalAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Convert.ToDecimal((string)argument2);
		}
    }   

    // GuidAsStringSelectorNode
    public class GuidAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return new Guid((string)argument1);
		}
    }
    
    // GuidAsStringReadAccessorNode
    public class GuidAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((Guid)argument1).ToString();
		}
    }
    
    // GuidAsStringWriteAccessorNode
    public class GuidAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return new Guid((string)argument2);
		}
    }   

    // MoneyAsStringSelectorNode
    public class MoneyAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Decimal.Parse((string)argument1, System.Globalization.NumberStyles.Currency);
		}
    }
    
    // MoneyAsStringReadAccessorNode
    public class MoneyAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((decimal)argument1).ToString("F");
		}
    }
    
    // MoneyAsDisplayStringReadAccessorNode
    public class MoneyAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((decimal)argument1).ToString("C");
		}
    }
    
    // MoneyAsStringWriteAccessorNode
    public class MoneyAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return Decimal.Parse((string)argument2, System.Globalization.NumberStyles.Currency);
		}
    }   

    // DateTimeAsStringSelectorNode
    public class DateTimeAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return DateTime.Parse((string)argument1);
		}
    }
    
    // DateTimeAsStringReadAccessorNode
    public class DateTimeAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((DateTime)argument1).ToString("G");
		}
    }
    
    // DateTimeAsStringWriteAccessorNode
    public class DateTimeAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return DateTime.Parse((string)argument2);
		}
    }   

    // DateAsStringSelectorNode
    public class DateAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return DateTime.Parse((string)argument1).Date;
		}
    }
    
    // DateAsStringReadAccessorNode
    public class DateAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((DateTime)argument1).ToString("d");
		}
    }
    
    // DateAsStringWriteAccessorNode
    public class DateAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return DateTime.Parse((string)argument2).Date;
		}
    }   

    // TimeSpanAsStringSelectorNode
    public class TimeSpanAsStringSelectorNode : UnaryInstructionNode
    {
		public static TimeSpan StringToTimeSpan(string value)
		{
			try
			{
				return TimeSpan.Parse(value);
			}
			catch
			{
				return CustomParse(value);
			}
		}

		public static TimeSpan CustomParse(string value)
		{ 
			bool isNegative = false;
			int weeks = 0;
			int days = 0;
			int hours = 0;
			int minutes = 0;
			int seconds = 0;
			int milliseconds = 0;
			int nanoseconds = 0;
			long ticks = 0;
			bool hasWeeks = false;
			bool hasDays = false;
			bool hasHours = false;
			bool hasMinutes = false;
			bool hasSeconds = false;
			bool hasMilliseconds = false;
			bool hasNanoseconds = false;
			 
			System.Text.StringBuilder stringValue = new System.Text.StringBuilder(value.ToLower());
			
			stringValue.Replace("ms","x");
			stringValue.Replace("mil","x");
			int i = 0;
			while (i < stringValue.Length)
			{
				if (stringValue[i] == ' ')                       
					stringValue.Remove(i,1);
				else if (stringValue[i] == '-')                  
				{
					if (i == 0 && isNegative == false)
					{
						isNegative = true;
						stringValue.Remove(i,1);
					}
					else
						throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
				}
				else if (i == 0 && ((int)stringValue[i] < 48 || (int)stringValue[i] > 57))
					throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
				else if ((int)stringValue[i] >= 48 && (int)stringValue[i] <= 57)
					i++;
				else if ((int)stringValue[i] < 48 || (int)stringValue[i] > 57)
				{
					if ((((int)stringValue[i - 1]) >= 48) && (((int)stringValue[i - 1]) <= 57))
					{
						if 
						(
							stringValue[i] == 'w' || stringValue[i] == 'd' || stringValue[i] == 'h' || 
							stringValue[i] == 'm' || stringValue[i] == 's' || stringValue[i] == 'n' ||
							stringValue[i] == 'x'
						)
							i++;	 
						else
							throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	 
					}
					else
						stringValue.Remove(i,1);
				}
			}
			
			if 
			(
				(stringValue.Length == 0) ||
				(
					stringValue[stringValue.Length - 1] != 'w' && stringValue[stringValue.Length - 1] != 'd' &&
					stringValue[stringValue.Length - 1] != 'h' && stringValue[stringValue.Length - 1] != 'm' &&
					stringValue[stringValue.Length - 1] != 's' && stringValue[stringValue.Length - 1] != 'x' &&
					stringValue[stringValue.Length - 1] != 'n'
				)
			)
				throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	 

			i = 0;
			while (stringValue.Length > 0)
			{
				if (((int)stringValue[i] >= 48) && ((int)stringValue[i] <= 57))
					i++;
				else
				{
					switch (stringValue[i])
					{
						case 'w':
							if (!(hasWeeks))
							{
								weeks = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								hasWeeks = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
							break;
						case 'd':
							if (!(hasDays))
							{	 
								days = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								hasDays = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
							break;
						case 'h':
							if (!(hasHours))
							{	
								hours = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								hasHours = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	 
							break;
						case 'm':
							if (!(hasMinutes))
							{	 
								minutes = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								hasMinutes = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	 
							break;
						case 's':
							if (!(hasSeconds))
							{	
								seconds = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								hasSeconds = true;
							} 
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
							break;
						case 'x':
							if (!(hasMilliseconds))
							{
								milliseconds = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								hasMilliseconds = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
							break;
						case 'n':
							if (!(hasNanoseconds))
							{
								nanoseconds = Convert.ToInt32(stringValue.ToString().Substring(0,i));
								if (nanoseconds % 100 != 0)
									throw new ConveyorException(ConveyorException.Codes.InvalidNanosecondArgument, value);
								hasNanoseconds = true;
							}
							else
								throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument, value);	
							break;
					}
					stringValue.Remove(0,i + 1);
					i = 0;
				}
			}
			ticks = 
				weeks * TimeSpan.TicksPerDay * 7 +
				days * TimeSpan.TicksPerDay +
				hours * TimeSpan.TicksPerHour +
				minutes * TimeSpan.TicksPerMinute + 
				seconds * TimeSpan.TicksPerSecond +
				milliseconds * TimeSpan.TicksPerMillisecond +
				nanoseconds / 100;

			return isNegative ? (new TimeSpan(ticks)).Negate() : new TimeSpan(ticks);
		}

		public static string TimeSpanToString(TimeSpan value)
		{
			const string CWeeks = "wks ";
			const string CDays = "days ";
			const string CHours = "hrs ";
			const string CMinutes = "min ";
			const string CSeconds = "sec ";
			const string CMilliseconds = "mil ";
			const string CNanoseconds = "nan ";
			String timeSpan = String.Empty;
			bool left = false;
			int weeks = value.Days / 7;
			int days = value.Days % 7;
			int hours = value.Hours;
			int minutes = value.Minutes;
			int seconds = value.Seconds;
			int milseconds = value.Milliseconds;
			long nanoseconds = ((value.Ticks % 10000) * 100);
 
			if (weeks != 0)
			{
				left = true;
				timeSpan = timeSpan + weeks.ToString() + CWeeks;
			}
			if (left)
			{
				if (days + hours + minutes + seconds + milseconds + nanoseconds != 0)
				{
					days = System.Math.Abs(days);
					timeSpan = timeSpan + days.ToString() + CDays;
				}
			}
			else 
				if (days != 0)
				{
					left = true;
					timeSpan = timeSpan + days.ToString() + CDays;
				}
			if (left)
			{
				if (hours + minutes + seconds + milseconds + nanoseconds != 0)
				{
					hours = System.Math.Abs(hours);
					timeSpan = timeSpan + hours.ToString() + CHours;
				}
			}
			else 
				if (hours != 0)
				{
					left = true;
					timeSpan = timeSpan + hours.ToString() + CHours;
				}
			if (left)
			{
				if (minutes + seconds + milseconds + nanoseconds != 0)
				{
					minutes = System.Math.Abs(minutes);
					timeSpan = timeSpan + minutes.ToString() + CMinutes;
				}
			}
			else 
				if (minutes != 0)
				{ 
					left = true;
					timeSpan = timeSpan + minutes.ToString() + CMinutes; 
				}
			if (left)
			{
				if (seconds + milseconds + nanoseconds != 0)
				{
					seconds = System.Math.Abs(seconds);
					timeSpan = timeSpan + seconds.ToString() + CSeconds;
				}
			}
			else 
				if (seconds != 0)
				{
					left = true;
					timeSpan = timeSpan + seconds.ToString() + CSeconds;	 
				}
			if (left)
			{
				if (milseconds + nanoseconds != 0)
				{
					milseconds = System.Math.Abs(milseconds);
					timeSpan = timeSpan + milseconds.ToString() + CMilliseconds;
				}
			}
			else 
				if (milseconds != 0)
				{
					left = true;
					timeSpan = timeSpan + milseconds.ToString() + CMilliseconds;
				}

			if (left && (nanoseconds != 0))
			{
				nanoseconds = System.Math.Abs(nanoseconds);
				timeSpan = timeSpan + nanoseconds.ToString() + CNanoseconds;
			}
			else if (!(left) && (nanoseconds == 0))
				timeSpan = timeSpan + nanoseconds.ToString();
			else if (nanoseconds != 0)
			{
				left = true;
				timeSpan = timeSpan + nanoseconds.ToString() + CNanoseconds;
			}
			return timeSpan;
		}

		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return StringToTimeSpan((string)argument1);
		}
    }
    
    // TimeSpanAsStringReadAccessorNode
    public class TimeSpanAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return TimeSpanAsStringSelectorNode.TimeSpanToString(((TimeSpan)argument1));
		}
    }
    
    // TimeSpanAsStringWriteAccessorNode
    public class TimeSpanAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return TimeSpanAsStringSelectorNode.StringToTimeSpan((string)argument2);
		}
    }   

    // TimeAsStringSelectorNode
    public class TimeAsStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return new DateTime(DateTime.Parse((string)argument1).TimeOfDay.Ticks);
		}
    }
    
    // TimeAsStringReadAccessorNode
    public class TimeAsStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ((DateTime)argument1).ToString("T");
		}
    }
    
    // TimeAsStringWriteAccessorNode
    public class TimeAsStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			return new DateTime(DateTime.Parse((string)argument2).TimeOfDay.Ticks);
		}
    }   

    // BinaryAsDisplayStringSelectorNode
    public class BinaryAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Binary");
		}
    }
    
    // BinaryAsDisplayStringReadAccessorNode
    public class BinaryAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Strings.Get("BinaryAsDisplayStringReadAccessorNode.BinaryData");
		}
    }
    
    // BinaryAsDisplayStringWriteAccessorNode
    public class BinaryAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Binary");
		}
    }   

    // GraphicAsDisplayStringSelectorNode
    public class GraphicAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Graphic");
		}
    }
    
    // GraphicAsDisplayStringReadAccessorNode
    public class GraphicAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return Strings.Get("GraphicAsDisplayStringReadAccessorNode.GraphicData");
		}
    }
    
    // GraphicAsDisplayStringWriteAccessorNode
    public class GraphicAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsString", "System.Graphic");
		}
    }   

    // ErrorAsDisplayStringSelectorNode
    public class ErrorAsDisplayStringSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsDisplayString", "System.Error");
		}
    }
    
    // ErrorAsDisplayStringReadAccessorNode
    public class ErrorAsDisplayStringReadAccessorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			return ExceptionUtility.BriefDescription((Exception)argument1);
		}
    }
    
    // ErrorAsDisplayStringWriteAccessorNode
    public class ErrorAsDisplayStringWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			throw new RuntimeException(RuntimeException.Codes.ReadOnlyRepresentation, "AsDisplayString", "System.Error");
		}
    }   
}