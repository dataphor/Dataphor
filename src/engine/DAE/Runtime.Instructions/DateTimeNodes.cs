/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

using System;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Compiling.Visitors;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// TODO: Nil handling in aggregate operators

	// TimeSpan Selectors
	/// <remarks>operator System.TimeSpan.Ticks(ATicks : Long) : TimeSpan;</remarks>
	public class SystemTimeSpanTicksSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return new TimeSpan((long)argument);
		}
	}
	
	/// <remarks>operator System.TimeSpan.Milliseconds(AMilliseconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMillisecondsSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return TimeSpan.FromMilliseconds((double)(decimal)argument);
		}
	}
	
	/// <remarks>operator System.TimeSpan.Seconds(ASeconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanSecondsSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return TimeSpan.FromSeconds((double)(decimal)argument);
		}
	}
	
	/// <remarks>operator System.TimeSpan.Minutes(AMinutes : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMinutesSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return TimeSpan.FromMinutes((double)(decimal)argument);
		}
	}
	
	/// <remarks>operator System.TimeSpan.Hours(AMinutes : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanHoursSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return TimeSpan.FromHours((double)(decimal)argument);
		}
	}
	
	/// <remarks>operator System.TimeSpan.Days(ADays : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanDaysSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return TimeSpan.FromDays((double)(decimal)argument);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer, AMilliseconds : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanMillisecondsSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null) ||
				(arguments[3] == null) ||
				(arguments[4] == null)
			)
				return null;
			else
			#endif
				return 
					new TimeSpan
					(
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2],
						(int)arguments[3]
					).Add(TimeSpan.FromTicks((long)(TimeSpan.TicksPerMillisecond * (double)(decimal)arguments[4])));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanSecondsSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null) ||
				(arguments[3] == null)
			)
				return null;
			else
			#endif
				return 
					new TimeSpan
					(
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2],
						(int)arguments[3],
						0
					);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer, AMinutes : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanMinutesSelectorNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if 
			(
				(argument1 == null) ||
				(argument2 == null) ||
				(argument3 == null)
			)
				return null;
			else
			#endif
				return 
					new TimeSpan
					(
						(int)argument1,
						(int)argument2,
						(int)argument3,
						0,
						0
					);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanHoursSelectorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if 
			(
				(argument1 == null) ||
				(argument2 == null)
			)
				return null;
			else
			#endif
				return 
					new TimeSpan
					(
						(int)argument1,
						(int)argument2,
						0,
						0,
						0
					);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanDaysSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if 
			(
				(argument == null)
			)
				return null;
			else
			#endif
				return 
					new TimeSpan
					(
						(int)argument,
						0,
						0,
						0,
						0
					);
		}
	}

	// TimeSpan Accessors
	/// <remarks>operator System.TimeSpan.TicksReadTicks(AValue : TimeSpan) : Long;</remarks>
	public class SystemTimeSpanTicksReadTicksNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return ((TimeSpan)argument).Ticks;
		}
	}
	
	/// <remarks>operator System.TimeSpan.TicksWriteTicks(AValue : TimeSpan, ATicks : Long) : TimeSpan;</remarks>
	public class SystemTimeSpanTicksWriteTicksNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			else
			#endif
				return new TimeSpan((long)argument2);
		}
	}
	
	/// <remarks>operator System.TimeSpan.MillisecondsReadMilliseconds(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanMillisecondsReadMillisecondsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			else
			#endif
				return (decimal)((TimeSpan)argument).TotalMilliseconds;
		}
	}
	
	/// <remarks>operator System.TimeSpan.MillisecondsWriteMilliseconds(AValue : TimeSpan, AMilliseconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMillisecondsWriteMillisecondsNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
				return TimeSpan.FromTicks((long)((decimal)argument2 * TimeSpan.TicksPerMillisecond));
		}
	}
	
	/// <remarks>operator System.TimeSpan.SecondsReadSeconds(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanSecondsReadSecondsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return (decimal)((TimeSpan)argument).TotalSeconds;
		}
	}
	
	/// <remarks>operator System.TimeSpan.SecondsWriteSeconds(AValue : TimeSpan, ASeconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanSecondsWriteSecondsNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
				return TimeSpan.FromSeconds((double)(decimal)argument2);
		}
	}
	
	/// <remarks>operator System.TimeSpan.MinutesReadMinutes(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanMinutesReadMinutesNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return (decimal)((TimeSpan)argument).TotalMinutes;
		}
	}
	
	/// <remarks>operator System.TimeSpan.MinutesWriteMinutes(AValue : TimeSpan, AMinutes : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMinutesWriteMinutesNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
				return TimeSpan.FromMinutes((double)(decimal)argument2);
		}
	}
	
	/// <remarks>operator System.TimeSpan.HoursReadHours(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanHoursReadHoursNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return (decimal)((TimeSpan)argument).TotalHours;
		}
	}
	
	/// <remarks>operator System.TimeSpan.HoursWriteHours(AValue : TimeSpan, AHours : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanHoursWriteHoursNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
				return TimeSpan.FromHours((double)(decimal)argument2);
		}
	}
	
	/// <remarks>operator System.TimeSpan.DaysReadDays(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanDaysReadDaysNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return (decimal)((TimeSpan)argument).TotalDays;
		}
	}
	
	/// <remarks>operator System.TimeSpan.DaysWriteDays(AValue : TimeSpan, ADays : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanDaysWriteDaysNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
				return TimeSpan.FromDays((double)(decimal)argument2);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadDay(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadDayNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((TimeSpan)argument).Days;
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteDay(AValue : TimeSpan, ADay : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteDayNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				TimeSpan timeSpan = (TimeSpan)argument1;
				return 
					new TimeSpan
					(
						timeSpan.Ticks +
							(
								((int)argument2 - timeSpan.Days) * 
								TimeSpan.TicksPerDay
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadHour(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadHourNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((TimeSpan)argument).Hours;
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteHour(AValue : TimeSpan, AHour : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteHourNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				TimeSpan timeSpan = (TimeSpan)argument1;
				return 
					new TimeSpan
					(
						timeSpan.Ticks +
							(
								((int)argument2 - timeSpan.Hours) * 
								TimeSpan.TicksPerHour
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadMinute(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadMinuteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((TimeSpan)argument).Minutes;
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteMinute(AValue : TimeSpan, AMinute : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteMinuteNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				TimeSpan timeSpan = (TimeSpan)argument1;
				return 
					new TimeSpan
					(
						timeSpan.Ticks +
							(
								((int)argument2 - timeSpan.Minutes) * 
								TimeSpan.TicksPerMinute
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadSecond(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadSecondNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((TimeSpan)argument).Seconds;
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteSecond(AValue : TimeSpan, ASecond : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteSecondNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				TimeSpan timeSpan = (TimeSpan)argument1;
				return 
					new TimeSpan
					(
						timeSpan.Ticks +
							(
								((int)argument2 - timeSpan.Seconds) * 
								TimeSpan.TicksPerSecond
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadMillisecond(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanTimeSpanReadMillisecondNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((TimeSpan)argument).Ticks % TimeSpan.TicksPerSecond / (decimal)TimeSpan.TicksPerMillisecond;
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteMillisecond(AValue : TimeSpan, AMillisecond : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteMillisecondNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return 
					new TimeSpan
					(
						(long)
						(
							(((TimeSpan)argument1).Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) +
							((decimal)argument2 * TimeSpan.TicksPerMillisecond)
						)					
					);
		}
	}

	// DateTime Selectors
	/// <remarks>operator System.DateTime.Ticks(ATicks : Long) : DateTime;</remarks>
	public class SystemDateTimeTicksSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
			{
				long ticks = (long)argument;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer, AMilliseconds : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null) ||
				(arguments[3] == null) ||
				(arguments[4] == null) ||
				(arguments[5] == null) ||
				(arguments[6] == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2],
						(int)arguments[3],
						(int)arguments[4],
						(int)arguments[5]
					).AddTicks((long)((decimal)arguments[6] * TimeSpan.TicksPerMillisecond));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeSecondsSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null) ||
				(arguments[3] == null) ||
				(arguments[4] == null) ||
				(arguments[5] == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2],
						(int)arguments[3],
						(int)arguments[4],
						(int)arguments[5]
					);
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer, AHours : Integer, AMinutes : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeMinutesSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null) ||
				(arguments[3] == null) ||
				(arguments[4] == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2],
						(int)arguments[3],
						(int)arguments[4],
						0,
						0
					);
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeDaysSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2]
					);
		}
	}
	
	// Explicit Cast Operators
	/// <remarks>operator System.DateTime.DateTime(ATimeSpan : TimeSpan) : DateTime;</remarks>
	public class SystemDateTimeDateTimeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
			{
				long ticks = ((TimeSpan)argument).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}

	// DateTime Accessors
	/// <remarks>operator System.DateTime.TicksReadTicks(AValue : DateTime) : Long;</remarks>
	public class SystemDateTimeTicksReadTicksNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Ticks;
		}
	}
	
	/// <remarks>operator System.DateTime.TicksWriteTicks(AValue : DateTime, ATicks : Long) : DateTime;</remarks>
	public class SystemDateTimeTicksWriteTicksNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = (long)argument2;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadYear(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadYearNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Year;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteYear(AValue : DateTime, AYear : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteYearNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						(int)argument2, 
						dateTime.Month, 
						dateTime.Day
					).AddTicks(dateTime.TimeOfDay.Ticks);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadMonth(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadMonthNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Month;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteMonth(AValue : DateTime, AMonth : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteMonthNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						dateTime.Year,
						(int)argument2, 
						dateTime.Day
					).AddTicks(dateTime.TimeOfDay.Ticks);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadDay(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadDayNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Day;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteDay(AValue : DateTime, ADay : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteDayNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						dateTime.Year,
						dateTime.Month, 
						(int)argument2
					).AddTicks(dateTime.TimeOfDay.Ticks);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadHour(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadHourNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Hour;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteHour(AValue : DateTime, AHour : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteHourNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						dateTime.Ticks +
							(
								((int)argument2 - dateTime.Hour) * 
								TimeSpan.TicksPerHour
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadMinute(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadMinuteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Minute;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteMinute(AValue : DateTime, AMinute : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteMinuteNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						dateTime.Ticks +
							(
								((int)argument2 - dateTime.Minute) *
								TimeSpan.TicksPerMinute
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadSecond(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadSecondNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Second;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteSecond(AValue : DateTime, ASecond : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteSecondNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						dateTime.Ticks +
							(
								((int)argument2 - dateTime.Second) *
								TimeSpan.TicksPerSecond
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadMillisecond(AValue : DateTime) : Decimal;</remarks>
	public class SystemDateTimeDateTimeReadMillisecondNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Ticks % TimeSpan.TicksPerSecond / (decimal)TimeSpan.TicksPerMillisecond;
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteMillisecond(AValue : DateTime, AMillisecond : Decimal) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteMillisecondNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime dateTime = (DateTime)argument1;
				return 
					new DateTime
					(
						(dateTime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) +
						(long)((decimal)argument2 * TimeSpan.TicksPerMillisecond)
					);
			}
		}
	}
	
	/// <remarks>operator System.Date.Ticks(ATicks : Long) : Date;</remarks>
	public class SystemDateTicksSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
			{
				long ticks = (long)argument;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerDay));
			}
		}
	}

	/// <remarks>operator System.Date.TicksReadTicks(AValue : Date) : Long;</remarks>
	public class SystemDateTicksReadTicksNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Ticks;
		}
	}
	
	/// <remarks>operator System.Date.TicksWriteTicks(AValue : Date, ATicks : Long) : Date;</remarks>
	public class SystemDateTicksWriteTicksNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = (long)argument2;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerDay));
			}
		}
	}

	/// <remarks>operator System.Time.Ticks(ATicks : Long) : Time;</remarks>
	public class SystemTimeTicksSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
			{
				long ticks = (long)argument % TimeSpan.TicksPerDay;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}

	/// <remarks>operator System.Time.Time(AHours : Integer, AMinutes : Integer, ASeconds : Integer, AMilliseconds : Integer) : Time;</remarks>
	public class SystemTimeTimeMillisecondsSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if 
			(
				(arguments[0] == null) ||
				(arguments[1] == null) ||
				(arguments[2] == null) ||
				(arguments[3] == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						1,
						1,
						1,
						(int)arguments[0],
						(int)arguments[1],
						(int)arguments[2]
					).AddTicks((long)((decimal)arguments[3] * TimeSpan.TicksPerMillisecond));
		}
	}
	
	/// <remarks>operator System.Time.Time(AHours : Integer, AMinutes : Integer, ASeconds : Integer) : Time;</remarks>
	public class SystemTimeTimeSecondsSelectorNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
			#if NILPROPOGATION
			if 
			(
				(argument1 == null) ||
				(argument2 == null) ||
				(argument3 == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						1,
						1,
						1,
						(int)argument1,
						(int)argument2,
						(int)argument3,
						0
					);
		}
	}
	
	/// <remarks>operator System.Time.Time(AHours : Integer, AMinutes : Integer) : Time;</remarks>
	public class SystemTimeTimeMinutesSelectorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if 
			(
				(argument1 == null) ||
				(argument2 == null)
			)
				return null;
			else
			#endif
				return 
					new DateTime
					(
						1,
						1,
						1,
						(int)argument1,
						(int)argument2,
						0,
						0
					);
		}
	}
	
	// Time Accessors
	/// <remarks>operator System.Time.TicksReadTicks(AValue : Time) : Long;</remarks>
	public class SystemTimeTicksReadTicksNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Ticks;
		}
	}
	
	/// <remarks>operator System.Time.TicksWriteTicks(AValue : Time, ATicks : Long) : Time;</remarks>
	public class SystemTimeTicksWriteTicksNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = (long)argument2 % TimeSpan.TicksPerDay;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadHour(AValue : Time) : Integer;</remarks>
	public class SystemTimeTimeReadHourNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Hour;
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteHour(AValue : Time, AHour : Integer) : Time;</remarks>
	public class SystemTimeTimeWriteHourNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime time = (DateTime)argument1;
				return 
					new DateTime
					(
						time.Ticks +
							(
								((int)argument2 - time.Hour) *
								TimeSpan.TicksPerHour
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadMinute(AValue : Time) : Integer;</remarks>
	public class SystemTimeTimeReadMinuteNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Minute;
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteMinute(AValue : Time, AMinute : Integer) : Time;</remarks>
	public class SystemTimeTimeWriteMinuteNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime time = (DateTime)argument1;
				return 
					new DateTime
					(
						time.Ticks +
							(
								((int)argument2 - time.Minute) *
								TimeSpan.TicksPerMinute
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadSecond(AValue : Time) : Integer;</remarks>
	public class SystemTimeTimeReadSecondNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Second;
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteSecond(AValue : Time, ASecond : Integer) : Time;</remarks>
	public class SystemTimeTimeWriteSecondNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime time = (DateTime)argument1;
				return 
					new DateTime
					(
						time.Ticks +
							(
								((int)argument2 - time.Second) *
								TimeSpan.TicksPerSecond
							)
					);
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadMillisecond(AValue : Time) : Decimal;</remarks>
	public class SystemTimeTimeReadMillisecondNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Ticks % TimeSpan.TicksPerSecond / (decimal)TimeSpan.TicksPerMillisecond;
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteMillisecond(AValue : Time, AMillisecond : Decimal) : Time;</remarks>
	public class SystemTimeTimeWriteMillisecondNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime time = (DateTime)argument1;
				return 
					new DateTime
					(
						(time.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) +
						(long)((decimal)argument2 * TimeSpan.TicksPerMillisecond)
					);
			}
		}
	}

	// Conversions		
	/// <remarks>operator ToTimeSpan(AString : String) : TimeSpan;</remarks>
	public class StringToTimeSpanNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return TimeSpanAsStringSelectorNode.StringToTimeSpan((String)argument);
		}
	}

	/// <remarks>operator ToDateTime(AString : string) : DateTime;</remarks>
	public class StringToDateTimeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
			{
				long ticks = DateTime.Parse((String)argument).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}

	/// <remarks>operator ToDate(AString : string) : Date;</remarks>
	public class StringToDateNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return DateTime.Parse((String)argument).Date;
		}
	}
	
	/// <remarks>operator ToTime(AString : string) : Time;</remarks>
	public class StringToTimeNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
			{
				long ticks = DateTime.Parse("1/1/0001 " + (String)argument).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}

	/// <remarks>operator ToString(ATimeSpan : TimeSpan) : String;</remarks>
	public class TimeSpanToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return TimeSpanAsStringSelectorNode.TimeSpanToString((TimeSpan)argument);
		}
	}
	
	/// <remarks>operator ToString(ADateTime : DateTime) : String;</remarks>
	public class DateTimeToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).ToString("G");
		}
	}
	
	/// <remarks>operator ToString(ATime : Time) : String;</remarks>
	public class TimeToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).ToString("T");
		}
	}

	/// <remarks>operator ToString(ADate : Date) : String;</remarks>
	public class DateToStringNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).ToString("d");
		}
	}
	
	/// <remarks>operator DatePart(ADateTime : DateTime) : DateTime;</remarks>
	public class DatePartNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).Date;
		}
	}
	
	/// <remarks>operator TimePart(ADateTime : DateTime) : DateTime;</remarks>
	public class TimePartNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return new DateTime(((DateTime)argument).TimeOfDay.Ticks);
		}
	}

	// Miscellaneous
	/// <remarks>operator DateTime() : DateTime;</remarks>
	public class DateTimeNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			long ticks;
			if (program.ServerProcess.InTransaction)
				ticks = program.ServerProcess.RootTransaction.StartTime.Ticks;
			else
				ticks = DateTime.Now.Ticks;
			return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
		}
	}
	
	/// <remarks>operator ActualDateTime() : DateTime;</remarks>
	public class ActualDateTimeNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			long ticks = DateTime.Now.Ticks;
			return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
		}
	}
	
	/// <remarks>operator Date() : Date;</remarks>
	public class DateNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			if (program.ServerProcess.InTransaction)
				return program.ServerProcess.RootTransaction.StartTime.Date;
			else
				return DateTime.Today;
		}
	}
	
	/// <remarks>operator ActualDate() : Date;</remarks>
	public class ActualDateNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			return DateTime.Today;
		}
	}
	
	/// <remarks>operator Time() : Time; </remarks>
	public class TimeNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			long ticks;
			if (program.ServerProcess.InTransaction)
				ticks = program.ServerProcess.RootTransaction.StartTime.TimeOfDay.Ticks;
			else
				ticks = DateTime.Now.Ticks;
			return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
		}
	}

	/// <remarks>operator ActualTime() : Time; </remarks>
	public class ActualTimeNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			long ticks = DateTime.Now.Ticks;
			return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
		}
	}

	/// <remarks>operator DayOfWeek(ADateTime : DateTime) : Integer;</remarks>
	public class DayOfWeekNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return (int)((DateTime)argument).DayOfWeek;
		}
	}

	/// <remarks>operator DayOfYear(ADateTime : DateTime) : Integer;</remarks>
	public class DayOfYearNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((DateTime)argument).DayOfYear;
		}
	}

	/// <remarks>operator DaysInMonth(AYear : Integer, AMonth : Integer) : Integer;</remarks>
	public class DaysInMonthNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return DateTime.DaysInMonth((int)argument1, (int)argument2);
		}
	}

	/// <remarks>operator IsLeapYear(AYear : Integer) : Boolean;</remarks>
	public class IsLeapYearNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return DateTime.IsLeapYear((int)argument);
		}
	}
	
	/// <remarks>operator Duration(ATimeSpan : TimeSpan) : TimeSpan;</remarks>
	public class DurationNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return ((TimeSpan)argument).Duration();
		}
	}

	/// <remarks>operator AddMonths(ADateTime : DateTime, AMonths : integer) : DateTime;</remarks>
	public class DateTimeAddMonthsNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return ((DateTime)argument1).AddMonths((int)argument2);
		}
	}	
	
	/// <remarks>operator MonthsBetween(AStartDateTime : DateTime, AEndDateTime : DateTime) : Integer;</remarks>
	public class DateTimeMonthsBetweenNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime startDate = (DateTime)argument1;
				DateTime endDate = (DateTime)argument2;
				return ((endDate.Year - startDate.Year) * 12) + (endDate.Month - startDate.Month);
			}
		}
	}
	
	/// <remarks>operator AddYears(ADateTime : DateTime, AYears : integer) : DateTime;</remarks>
	public class DateTimeAddYearsNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return ((DateTime)argument1).AddYears((int)argument2);
		}
	}

	/// <remarks>operator YearsBetween(AStartDateTime : DateTime, AEndDateTime : DateTime) : Integer;</remarks>
	public class DateTimeYearsBetweenNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				DateTime startDate = (DateTime)argument1;
				DateTime endDate = (DateTime)argument2;
				return (endDate.Year - startDate.Year);
			}
		}
	}
	
	/// <remarks>operator AddMonths(ADate : Date, AMonths : integer) : Date;</remarks>
	public class DateAddMonthsNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return ((DateTime)argument1).AddMonths((int)argument2);
		}
	}	
	
	/// <remarks>operator AddYears(ADate : Date, AYears : integer) : Date;</remarks>
	public class DateAddYearsNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return ((DateTime)argument1).AddYears((int)argument2);
		}
	}

	// Arithmetic		
	/// <remarks>operator iNegate(AValue : TimeSpan) : TimeSpan;</remarks>
	public class TimeSpanNegateNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if ((argument == null))
				return null;
			else
			#endif
				return -(TimeSpan)argument;
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : TimeSpan, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class TimeSpanAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 + (TimeSpan)argument2;
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : DateTime, ARightValue : TimeSpan) : DateTime;</remarks>
	public class DateTimeTimeSpanAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = ((DateTime)argument1).Ticks + ((TimeSpan)argument2).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : Date, ARightValue : TimeSpan) : Date;</remarks>
	public class DateTimeSpanAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = ((DateTime)argument1).Ticks + ((TimeSpan)argument2).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerDay));
			}
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : Time, ARightValue : TimeSpan) : DateTime;</remarks>
	public class TimeTimeSpanAdditionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = (((DateTime)argument1).Ticks + ((TimeSpan)argument2).Ticks) % TimeSpan.TicksPerDay;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : TimeSpan, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class TimeSpanSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 - (TimeSpan)argument2;
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : DateTime, ARightValue : DateTime) : TimeSpan;</remarks>
	public class DateTimeSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(((DateTime)argument1).Ticks - ((DateTime)argument2).Ticks);
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Date, ARightValue : Date) : TimeSpan;</remarks>
	public class DateSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(((DateTime)argument1).Ticks - ((DateTime)argument2).Ticks);
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Time, ARightValue : Time) : TimeSpan;</remarks>
	public class TimeSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(((DateTime)argument1).Ticks - ((DateTime)argument2).Ticks);
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : DateTime, ARightValue : TimeSpan) : DateTime;</remarks>
	public class DateTimeTimeSpanSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = ((DateTime)argument1).Ticks - ((TimeSpan)argument2).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Date, ARightValue : TimeSpan) : Date;</remarks>
	public class DateTimeSpanSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = ((DateTime)argument1).Ticks - ((TimeSpan)argument2).Ticks;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerDay));
			}
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Time, ARightValue : TimeSpan) : DateTime;</remarks>
	public class TimeTimeSpanSubtractionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
			{
				long ticks = ((((DateTime)argument1).Ticks + TimeSpan.TicksPerDay) - (((TimeSpan)argument2).Ticks % TimeSpan.TicksPerDay)) % TimeSpan.TicksPerDay;
				return new DateTime(ticks - (ticks % TimeSpan.TicksPerSecond));
			}
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : TimeSpan, ARightValue : Integer) : TimeSpan;</remarks>
	public class TimeSpanIntegerMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(((TimeSpan)argument1).Ticks * (int)argument2);
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : Integer, ARightValue : TimeSpan) : DateTime;</remarks>
	public class IntegerTimeSpanMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan((int)argument1 * ((TimeSpan)argument2).Ticks);
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : TimeSpan, ARightValue : Decimal) : TimeSpan;</remarks>
	public class TimeSpanDecimalMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(Convert.ToInt64(((TimeSpan)argument1).Ticks * (decimal)argument2));
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : Decimal, ARightValue : TimeSpan) : DateTime;</remarks>
	public class DecimalTimeSpanMultiplicationNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(Convert.ToInt64((decimal)argument1 * ((TimeSpan)argument2).Ticks));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : TimeSpan, ARightValue : Integer) : TimeSpan;</remarks>
	public class TimeSpanIntegerDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(((TimeSpan)argument1).Ticks / (int)argument2);
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : Integer, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class IntegerTimeSpanDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan((int)argument1 / ((TimeSpan)argument2).Ticks);
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : TimeSpan, ARightValue : Decimal) : TimeSpan;</remarks>
	public class TimeSpanDecimalDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(Convert.ToInt64(((TimeSpan)argument1).Ticks / (decimal)argument2));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : Decimal, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class DecimalTimeSpanDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return new TimeSpan(Convert.ToInt64((decimal)argument1 / ((TimeSpan)argument2).Ticks));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Decimal;</remarks>
	public class TimeSpanTimeSpanDivisionNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (decimal)((TimeSpan)argument1).Ticks / (decimal)((TimeSpan)argument2).Ticks;
		}
	}
	
	/// <remarks>operator iEqual(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 == (TimeSpan)argument2;
		}
	}
	
	public class DateTimeEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 == (DateTime)argument2;
		}
	}
	
	public class DateEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 == (DateTime)argument2;
		}
	}
	
	public class TimeEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 == (DateTime)argument2;
		}
	}
	
	/// <remarks>operator iNotEqual(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 != (TimeSpan)argument2;
		}
	}
	
	public class DateTimeNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 != (DateTime)argument2;
		}
	}
	
	public class DateNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 != (DateTime)argument2;
		}
	}
	
	public class TimeNotEqualNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 != (DateTime)argument2;
		}
	}
	
	/// <remarks>operator iGreater(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 > (TimeSpan)argument2;
		}
	}
	
	public class DateTimeGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 > (DateTime)argument2;
		}
	}
	
	public class DateGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 > (DateTime)argument2;
		}
	}
	
	public class TimeGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 > (DateTime)argument2;
		}
	}
	
	/// <remarks>operator iInclusiveGreater(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 >= (TimeSpan)argument2;
		}
	}
	
	public class DateTimeInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 >= (DateTime)argument2;
		}
	}
	
	public class DateInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 >= (DateTime)argument2;
		}
	}
	
	public class TimeInclusiveGreaterNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 >= (DateTime)argument2;
		}
	}
	
	/// <remarks>operator iLess(ALeftValue : DateTime, ARightValue : DateTime) : Boolean;</remarks>
	public class DateTimeLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 < (DateTime)argument2;
		}
	}
	
	/// <remarks>operator iLess(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 < (TimeSpan)argument2;
		}
	}

	public class DateLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 < (DateTime)argument2;
		}
	}
	
	public class TimeLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 < (DateTime)argument2;
		}
	}
	
	/// <remarks>operator iInclusiveLess(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (TimeSpan)argument1 <= (TimeSpan)argument2;
		}
	}

	public class DateTimeInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 <= (DateTime)argument2;
		}
	}

	public class DateInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 <= (DateTime)argument2;
		}
	}

	public class TimeInclusiveLessNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
			#endif
				return (DateTime)argument1 <= (DateTime)argument2;
		}
	}

	public class TimeSpanInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
    
	public class DateTimeInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
    
	public class DateInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
    
	public class TimeSpanSumAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				program.Stack[1] = 
					(program.Stack[1] == null) ? 
						(TimeSpan)program.Stack[0] : 
						((TimeSpan)program.Stack[1] + (TimeSpan)program.Stack[0]);
			return null;
		}
	}
	
	public class TimeSpanMinInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
	
	public class DateTimeMinInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}

	public class TimeMinInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}

	public class DateMinInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
	
	public class TimeSpanMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((TimeSpan)program.Stack[0] < (TimeSpan)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class DateTimeMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((DateTime)program.Stack[0] < (DateTime)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}

	public class TimeMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((DateTime)program.Stack[0] < (DateTime)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class DateMinAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((DateTime)program.Stack[0] < (DateTime)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class TimeSpanMaxInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
	
	public class DateTimeMaxInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
	
	public class TimeMaxInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
	
	public class DateMaxInitializationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			program.Stack[0] = null;
			return null;
		}
	}
	
	public class TimeSpanMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((TimeSpan)program.Stack[0] > (TimeSpan)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class DateTimeMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((DateTime)program.Stack[0] > (DateTime)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class TimeMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((DateTime)program.Stack[0] > (DateTime)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class DateMaxAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
				if ((program.Stack[1] == null) || ((DateTime)program.Stack[0] > (DateTime)program.Stack[1]))
					program.Stack[1] = program.Stack[0];
			return null;
		}
	}
    
	public class TimeSpanAvgInitializationNode : PlanNode
	{
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.Push(new Symbol("LCounter", plan.DataTypes.SystemInteger));
		}
		
		public override object InternalExecute(Program program)
		{
			program.Stack.Push(0);
			program.Stack[1] = TimeSpan.Zero;
			return null;
		}
	}
	
	public class TimeSpanAvgAggregationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if (program.Stack[0] != null)
			{
				program.Stack[1] = checked((int)program.Stack[1] + 1);
				program.Stack[2] = (TimeSpan)program.Stack[2] + (TimeSpan)program.Stack[0];
			}
			return null;
		}
	}
    
	public class TimeSpanAvgFinalizationNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			if ((int)program.Stack[0] == 0)
				program.Stack[1] = null;
			else
				program.Stack[1] = new TimeSpan(((TimeSpan)program.Stack[1]).Ticks / (int)program.Stack[0]);
			return null;
		}
	}
}
