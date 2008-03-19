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
	using Alphora.Dataphor.DAE.Streams;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	// TODO: Nil handling in aggregate operators

	// TimeSpan Selectors
	/// <remarks>operator System.TimeSpan.Ticks(ATicks : Long) : TimeSpan;</remarks>
	public class SystemTimeSpanTicksSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsInt64)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.Milliseconds(AMilliseconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMillisecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromMilliseconds((double)AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.Seconds(ASeconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanSecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromSeconds((double)AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.Minutes(AMinutes : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMinutesSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromMinutes((double)AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.Hours(AMinutes : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanHoursSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromHours((double)AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.Days(ADays : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanDaysSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromDays((double)AArguments[0].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer, AMilliseconds : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanMillisecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil ||
				(AArguments[3].Value == null) || AArguments[3].Value.IsNil ||
				(AArguments[4].Value == null) || AArguments[4].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							AArguments[3].Value.AsInt32
						).Add(TimeSpan.FromTicks((long)(TimeSpan.TicksPerMillisecond * (double)AArguments[4].Value.AsDecimal)))
					)
				);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanSecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil ||
				(AArguments[3].Value == null) || AArguments[3].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							AArguments[3].Value.AsInt32,
							0
						)
					)
				);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer, AMinutes : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanMinutesSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							0,
							0
						)
					)
				);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer, AHours : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanHoursSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							0,
							0,
							0
						)
					)
				);
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpan(ADays : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanDaysSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							AArguments[0].Value.AsInt32,
							0,
							0,
							0,
							0
						)
					)
				);
		}
	}

	// TimeSpan Accessors
	/// <remarks>operator System.TimeSpan.TicksReadTicks(AValue : TimeSpan) : Long;</remarks>
	public class SystemTimeSpanTicksReadTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Ticks));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TicksWriteTicks(AValue : TimeSpan, ATicks : Long) : TimeSpan;</remarks>
	public class SystemTimeSpanTicksWriteTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[1].Value.AsInt64)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.MillisecondsReadMilliseconds(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanMillisecondsReadMillisecondsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)AArguments[0].Value.AsTimeSpan.TotalMilliseconds));
		}
	}
	
	/// <remarks>operator System.TimeSpan.MillisecondsWriteMilliseconds(AValue : TimeSpan, AMilliseconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMillisecondsWriteMillisecondsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromTicks((long)(AArguments[1].Value.AsDecimal * TimeSpan.TicksPerMillisecond))));
		}
	}
	
	/// <remarks>operator System.TimeSpan.SecondsReadSeconds(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanSecondsReadSecondsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)AArguments[0].Value.AsTimeSpan.TotalSeconds));
		}
	}
	
	/// <remarks>operator System.TimeSpan.SecondsWriteSeconds(AValue : TimeSpan, ASeconds : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanSecondsWriteSecondsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromSeconds((double)AArguments[1].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.MinutesReadMinutes(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanMinutesReadMinutesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)AArguments[0].Value.AsTimeSpan.TotalMinutes));
		}
	}
	
	/// <remarks>operator System.TimeSpan.MinutesWriteMinutes(AValue : TimeSpan, AMinutes : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanMinutesWriteMinutesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromMinutes((double)AArguments[1].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.HoursReadHours(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanHoursReadHoursNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)AArguments[0].Value.AsTimeSpan.TotalHours));
		}
	}
	
	/// <remarks>operator System.TimeSpan.HoursWriteHours(AValue : TimeSpan, AHours : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanHoursWriteHoursNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromHours((double)AArguments[1].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.DaysReadDays(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanDaysReadDaysNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)AArguments[0].Value.AsTimeSpan.TotalDays));
		}
	}
	
	/// <remarks>operator System.TimeSpan.DaysWriteDays(AValue : TimeSpan, ADays : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanDaysWriteDaysNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpan.FromDays((double)AArguments[1].Value.AsDecimal)));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadDay(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadDayNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Days));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteDay(AValue : TimeSpan, ADay : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteDayNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				TimeSpan LTimeSpan = AArguments[0].Value.AsTimeSpan;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							LTimeSpan.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTimeSpan.Days) * 
									TimeSpan.TicksPerDay
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadHour(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadHourNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Hours));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteHour(AValue : TimeSpan, AHour : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteHourNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				TimeSpan LTimeSpan = AArguments[0].Value.AsTimeSpan;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							LTimeSpan.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTimeSpan.Hours) * 
									TimeSpan.TicksPerHour
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadMinute(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadMinuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Minutes));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteMinute(AValue : TimeSpan, AMinute : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteMinuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				TimeSpan LTimeSpan = AArguments[0].Value.AsTimeSpan;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							LTimeSpan.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTimeSpan.Minutes) * 
									TimeSpan.TicksPerMinute
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadSecond(AValue : TimeSpan) : Integer;</remarks>
	public class SystemTimeSpanTimeSpanReadSecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Seconds));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteSecond(AValue : TimeSpan, ASecond : Integer) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteSecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				TimeSpan LTimeSpan = AArguments[0].Value.AsTimeSpan;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							LTimeSpan.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTimeSpan.Seconds) * 
									TimeSpan.TicksPerSecond
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanReadMillisecond(AValue : TimeSpan) : Decimal;</remarks>
	public class SystemTimeSpanTimeSpanReadMillisecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Ticks % TimeSpan.TicksPerSecond / (decimal)TimeSpan.TicksPerMillisecond));
		}
	}
	
	/// <remarks>operator System.TimeSpan.TimeSpanWriteMillisecond(AValue : TimeSpan, AMillisecond : Decimal) : TimeSpan;</remarks>
	public class SystemTimeSpanTimeSpanWriteMillisecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new TimeSpan
						(
							(long)
							(
								(AArguments[0].Value.AsTimeSpan.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) +
								(AArguments[1].Value.AsDecimal * TimeSpan.TicksPerMillisecond)
							)					
						)
					)
				);
		}
	}

	// DateTime Selectors
	/// <remarks>operator System.DateTime.Ticks(ATicks : Long) : DateTime;</remarks>
	public class SystemDateTimeTicksSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsInt64;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer, AMilliseconds : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil ||
				(AArguments[3].Value == null) || AArguments[3].Value.IsNil ||
				(AArguments[4].Value == null) || AArguments[4].Value.IsNil ||
				(AArguments[5].Value == null) || AArguments[5].Value.IsNil ||
				(AArguments[6].Value == null) || AArguments[6].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							AArguments[3].Value.AsInt32,
							AArguments[4].Value.AsInt32,
							AArguments[5].Value.AsInt32
						).AddTicks((long)(AArguments[6].Value.AsDecimal * TimeSpan.TicksPerMillisecond))
					)
				);
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer, AHours : Integer, AMinutes : Integer, ASeconds : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeSecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil ||
				(AArguments[3].Value == null) || AArguments[3].Value.IsNil ||
				(AArguments[4].Value == null) || AArguments[4].Value.IsNil ||
				(AArguments[5].Value == null) || AArguments[5].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							AArguments[3].Value.AsInt32,
							AArguments[4].Value.AsInt32,
							AArguments[5].Value.AsInt32
						)
					)
				);
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer, AHours : Integer, AMinutes : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeMinutesSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil ||
				(AArguments[3].Value == null) || AArguments[3].Value.IsNil ||
				(AArguments[4].Value == null) || AArguments[4].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							AArguments[3].Value.AsInt32,
							AArguments[4].Value.AsInt32,
							0,
							0
						)
					)
				);
		}
	}
	
	/// <remarks>operator System.DateTime.DateTime(AYears : Integer, AMonths : Integer, ADays : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeDaysSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32
						)
					)
				);
		}
	}
	
	// Explicit Cast Operators
	/// <remarks>operator System.DateTime.DateTime(ATimeSpan : TimeSpan) : DateTime;</remarks>
	public class SystemDateTimeDateTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsTimeSpan.Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}

	// DateTime Accessors
	/// <remarks>operator System.DateTime.TicksReadTicks(AValue : DateTime) : Long;</remarks>
	public class SystemDateTimeTicksReadTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Ticks));
		}
	}
	
	/// <remarks>operator System.DateTime.TicksWriteTicks(AValue : DateTime, ATicks : Long) : DateTime;</remarks>
	public class SystemDateTimeTicksWriteTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[1].Value.AsInt64;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadYear(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadYearNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Year));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteYear(AValue : DateTime, AYear : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteYearNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							AArguments[1].Value.AsInt32, 
							LDateTime.Month, 
							LDateTime.Day
						).AddTicks(LDateTime.TimeOfDay.Ticks)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadMonth(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadMonthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Month));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteMonth(AValue : DateTime, AMonth : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteMonthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LDateTime.Year,
							AArguments[1].Value.AsInt32, 
							LDateTime.Day
						).AddTicks(LDateTime.TimeOfDay.Ticks)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadDay(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadDayNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Day));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteDay(AValue : DateTime, ADay : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteDayNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LDateTime.Year,
							LDateTime.Month, 
							AArguments[1].Value.AsInt32
						).AddTicks(LDateTime.TimeOfDay.Ticks)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadHour(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadHourNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Hour));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteHour(AValue : DateTime, AHour : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteHourNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LDateTime.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LDateTime.Hour) * 
									TimeSpan.TicksPerHour
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadMinute(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadMinuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Minute));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteMinute(AValue : DateTime, AMinute : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteMinuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LDateTime.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LDateTime.Minute) *
									TimeSpan.TicksPerMinute
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadSecond(AValue : DateTime) : Integer;</remarks>
	public class SystemDateTimeDateTimeReadSecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Second));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteSecond(AValue : DateTime, ASecond : Integer) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteSecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LDateTime.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LDateTime.Second) *
									TimeSpan.TicksPerSecond
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeReadMillisecond(AValue : DateTime) : Decimal;</remarks>
	public class SystemDateTimeDateTimeReadMillisecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Ticks % TimeSpan.TicksPerSecond / (decimal)TimeSpan.TicksPerMillisecond));
		}
	}
	
	/// <remarks>operator System.DateTime.DateTimeWriteMillisecond(AValue : DateTime, AMillisecond : Decimal) : DateTime;</remarks>
	public class SystemDateTimeDateTimeWriteMillisecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LDateTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							(LDateTime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) +
							(long)(AArguments[1].Value.AsDecimal * TimeSpan.TicksPerMillisecond)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.Date.Ticks(ATicks : Long) : Date;</remarks>
	public class SystemDateTicksSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsInt64;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay))));
			}
		}
	}

	/// <remarks>operator System.Date.TicksReadTicks(AValue : Date) : Long;</remarks>
	public class SystemDateTicksReadTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Ticks));
		}
	}
	
	/// <remarks>operator System.Date.TicksWriteTicks(AValue : Date, ATicks : Long) : Date;</remarks>
	public class SystemDateTicksWriteTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[1].Value.AsInt64;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay))));
			}
		}
	}
	
	/// <remarks>operator System.Time.Ticks(ATicks : Long) : Time;</remarks>
	public class SystemTimeTicksSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsInt64 % TimeSpan.TicksPerDay;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}

	/// <remarks>operator System.Time.Time(AHours : Integer, AMinutes : Integer, ASeconds : Integer, AMilliseconds : Integer) : Time;</remarks>
	public class SystemTimeTimeMillisecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil ||
				(AArguments[3].Value == null) || AArguments[3].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							1,
							1,
							1,
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32
						).AddTicks((long)(AArguments[3].Value.AsDecimal * TimeSpan.TicksPerMillisecond) )
					)
				);
		}
	}
	
	/// <remarks>operator System.Time.Time(AHours : Integer, AMinutes : Integer, ASeconds : Integer) : Time;</remarks>
	public class SystemTimeTimeSecondsSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil ||
				(AArguments[2].Value == null) || AArguments[2].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							1,
							1,
							1,
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							AArguments[2].Value.AsInt32,
							0
						)
					)
				);
		}
	}
	
	/// <remarks>operator System.Time.Time(AHours : Integer, AMinutes : Integer) : Time;</remarks>
	public class SystemTimeTimeMinutesSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if 
			(
				(AArguments[0].Value == null) || AArguments[0].Value.IsNil ||
				(AArguments[1].Value == null) || AArguments[1].Value.IsNil
			)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar
				(
					DataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							1,
							1,
							1,
							AArguments[0].Value.AsInt32,
							AArguments[1].Value.AsInt32,
							0,
							0
						)
					)
				);
		}
	}
	
	// Time Accessors
	/// <remarks>operator System.Time.TicksReadTicks(AValue : Time) : Long;</remarks>
	public class SystemTimeTicksReadTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Ticks));
		}
	}
	
	/// <remarks>operator System.Time.TicksWriteTicks(AValue : Time, ATicks : Long) : Time;</remarks>
	public class SystemTimeTicksWriteTicksNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[1].Value.AsInt64 % TimeSpan.TicksPerDay;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadHour(AValue : Time) : Integer;</remarks>
	public class SystemTimeTimeReadHourNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Hour));
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteHour(AValue : Time, AHour : Integer) : Time;</remarks>
	public class SystemTimeTimeWriteHourNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LTime.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTime.Hour) *
									TimeSpan.TicksPerHour
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadMinute(AValue : Time) : Integer;</remarks>
	public class SystemTimeTimeReadMinuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Minute));
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteMinute(AValue : Time, AMinute : Integer) : Time;</remarks>
	public class SystemTimeTimeWriteMinuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LTime.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTime.Minute) *
									TimeSpan.TicksPerMinute
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadSecond(AValue : Time) : Integer;</remarks>
	public class SystemTimeTimeReadSecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Second));
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteSecond(AValue : Time, ASecond : Integer) : Time;</remarks>
	public class SystemTimeTimeWriteSecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							LTime.Ticks +
								(
									(AArguments[1].Value.AsInt32 - LTime.Second) *
									TimeSpan.TicksPerSecond
								)
						)
					)
				);
			}
		}
	}
	
	/// <remarks>operator System.Time.TimeReadMillisecond(AValue : Time) : Decimal;</remarks>
	public class SystemTimeTimeReadMillisecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Ticks % TimeSpan.TicksPerSecond / (decimal)TimeSpan.TicksPerMillisecond ));
		}
	}
	
	/// <remarks>operator System.Time.TimeWriteMillisecond(AValue : Time, AMillisecond : Decimal) : Time;</remarks>
	public class SystemTimeTimeWriteMillisecondNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LTime = AArguments[0].Value.AsDateTime;
				return new DataVar
				(
					DataType, 
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						new DateTime
						(
							(LTime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) +
							(long)(AArguments[1].Value.AsDecimal * TimeSpan.TicksPerMillisecond)
						)
					)
				);
			}
		}
	}

	// Conversions		
	/// <remarks>operator ToTimeSpan(AString : String) : TimeSpan;</remarks>
	public class StringToTimeSpanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpanAsStringSelectorNode.StringToTimeSpan(AArguments[0].Value.AsString)));
		}
	}

	/// <remarks>operator ToDateTime(AString : string) : DateTime;</remarks>
	public class StringToDateTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = DateTime.Parse(AArguments[0].Value.AsString).Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}

	/// <remarks>operator ToDate(AString : string) : Date;</remarks>
	public class StringToDateNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Parse(AArguments[0].Value.AsString).Date));
		}
	}
	
	/// <remarks>operator ToTime(AString : string) : Time;</remarks>
	public class StringToTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = DateTime.Parse("1/1/0001 " + AArguments[0].Value.AsString).Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}

	/// <remarks>operator ToString(ATimeSpan : TimeSpan) : String;</remarks>
	public class TimeSpanToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, TimeSpanAsStringSelectorNode.TimeSpanToString(AArguments[0].Value.AsTimeSpan)));
		}
	}
	
	/// <remarks>operator ToString(ADateTime : DateTime) : String;</remarks>
	public class DateTimeToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.ToString("G")));
		}
	}
	
	/// <remarks>operator ToString(ATime : Time) : String;</remarks>
	public class TimeToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.ToString("T")));
		}
	}

	/// <remarks>operator ToString(ADate : Date) : String;</remarks>
	public class DateToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.ToString("d")));
		}
	}
	
	/// <remarks>operator DatePart(ADateTime : DateTime) : DateTime;</remarks>
	public class DatePartNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.Date));
		}
	}
	
	/// <remarks>operator TimePart(ADateTime : DateTime) : DateTime;</remarks>
	public class TimePartNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(AArguments[0].Value.AsDateTime.TimeOfDay.Ticks)));
		}
	}

	// Miscellaneous
	/// <remarks>operator DateTime() : DateTime;</remarks>
	public class DateTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LTicks;
			if (AProcess.InTransaction)
				LTicks = AProcess.RootTransaction.StartTime.Ticks;
			else
				LTicks = DateTime.Now.Ticks;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
		}
	}
	
	/// <remarks>operator ActualDateTime() : DateTime;</remarks>
	public class ActualDateTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LTicks = DateTime.Now.Ticks;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
		}
	}
	
	/// <remarks>operator Date() : Date;</remarks>
	public class DateNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.InTransaction)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.RootTransaction.StartTime.Date));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Today));
		}
	}
	
	/// <remarks>operator ActualDate() : Date;</remarks>
	public class ActualDateNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Today));
		}
	}
	
	/// <remarks>operator Time() : Time; </remarks>
	public class TimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LTicks;
			if (AProcess.InTransaction)
				LTicks = AProcess.RootTransaction.StartTime.TimeOfDay.Ticks;
			else
				LTicks = DateTime.Now.Ticks;
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
		}
	}

	/// <remarks>operator ActualTime() : Time; </remarks>
	public class ActualTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LTicks = DateTime.Now.Ticks;
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
		}
	}

	/// <remarks>operator DayOfWeek(ADateTime : DateTime) : Integer;</remarks>
	public class DayOfWeekNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (int)AArguments[0].Value.AsDateTime.DayOfWeek));
		}
	}

	/// <remarks>operator DayOfYear(ADateTime : DateTime) : Integer;</remarks>
	public class DayOfYearNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.DayOfYear));
		}
	}

	/// <remarks>operator DaysInMonth(AYear : Integer, AMonth : Integer) : Integer;</remarks>
	public class DaysInMonthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.DaysInMonth(AArguments[0].Value.AsInt32, AArguments[1].Value.AsInt32)));
		}
	}

	/// <remarks>operator IsLeapYear(AYear : Integer) : Boolean;</remarks>
	public class IsLeapYearNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.IsLeapYear(AArguments[0].Value.AsInt32)));
		}
	}
	
	/// <remarks>operator Duration(ATimeSpan : TimeSpan) : TimeSpan;</remarks>
	public class DurationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan.Duration()));
		}
	}

	/// <remarks>operator AddMonths(ADateTime : DateTime, AMonths : integer) : DateTime;</remarks>
	public class DateTimeAddMonthsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.AddMonths(AArguments[1].Value.AsInt32)));
		}
	}	
	
	/// <remarks>operator MonthsBetween(AStartDateTime : DateTime, AEndDateTime : DateTime) : Integer;</remarks>
	public class DateTimeMonthsBetweenNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LStartDate = AArguments[0].Value.AsDateTime;
				DateTime LEndDate = AArguments[1].Value.AsDateTime;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((LEndDate.Year - LStartDate.Year) * 12) + (LEndDate.Month - LStartDate.Month)));
			}
		}
	}
	
	/// <remarks>operator AddYears(ADateTime : DateTime, AYears : integer) : DateTime;</remarks>
	public class DateTimeAddYearsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.AddYears(AArguments[1].Value.AsInt32)));
		}
	}

	/// <remarks>operator YearsBetween(AStartDateTime : DateTime, AEndDateTime : DateTime) : Integer;</remarks>
	public class DateTimeYearsBetweenNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				DateTime LStartDate = AArguments[0].Value.AsDateTime;
				DateTime LEndDate = AArguments[1].Value.AsDateTime;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (LEndDate.Year - LStartDate.Year)));
			}
		}
	}
	
	/// <remarks>operator AddMonths(ADate : Date, AMonths : integer) : Date;</remarks>
	public class DateAddMonthsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.AddMonths(AArguments[1].Value.AsInt32)));
		}
	}	
	
	/// <remarks>operator AddYears(ADate : Date, AYears : integer) : Date;</remarks>
	public class DateAddYearsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime.AddYears(AArguments[1].Value.AsInt32)));
		}
	}

	// Arithmetic		
	/// <remarks>operator iNegate(AValue : TimeSpan) : TimeSpan;</remarks>
	public class TimeSpanNegateNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, -AArguments[0].Value.AsTimeSpan));
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : TimeSpan, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class TimeSpanAdditionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan + AArguments[1].Value.AsTimeSpan));
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : DateTime, ARightValue : TimeSpan) : DateTime;</remarks>
	public class DateTimeTimeSpanAdditionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsDateTime.Ticks + AArguments[1].Value.AsTimeSpan.Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : Date, ARightValue : TimeSpan) : Date;</remarks>
	public class DateTimeSpanAdditionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsDateTime.Ticks + AArguments[1].Value.AsTimeSpan.Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay))));
			}
		}
	}
	
	/// <remarks>operator iAddition(ALeftValue : TimeSpan, ARightValue : TimeSpan) : DateTime;</remarks>
	public class TimeTimeSpanAdditionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = (AArguments[0].Value.AsDateTime.Ticks + AArguments[1].Value.AsTimeSpan.Ticks) % TimeSpan.TicksPerDay;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : TimeSpan, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class TimeSpanSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan - AArguments[1].Value.AsTimeSpan));
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : DateTime, ARightValue : DateTime) : TimeSpan;</remarks>
	public class DateTimeSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsDateTime.Ticks - AArguments[1].Value.AsDateTime.Ticks)));
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Date, ARightValue : Date) : TimeSpan;</remarks>
	public class DateSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsDateTime.Ticks - AArguments[1].Value.AsDateTime.Ticks)));
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Time, ARightValue : Time) : TimeSpan;</remarks>
	public class TimeSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsDateTime.Ticks - AArguments[1].Value.AsDateTime.Ticks)));
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : DateTime, ARightValue : TimeSpan) : DateTime;</remarks>
	public class DateTimeTimeSpanSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsDateTime.Ticks - AArguments[1].Value.AsTimeSpan.Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Date, ARightValue : TimeSpan) : Date;</remarks>
	public class DateTimeSpanSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = AArguments[0].Value.AsDateTime.Ticks - AArguments[1].Value.AsTimeSpan.Ticks;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay))));
			}
		}
	}
	
	/// <remarks>operator iSubtraction(ALeftValue : Time, ARightValue : TimeSpan) : DateTime;</remarks>
	public class TimeTimeSpanSubtractionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
			{
				long LTicks = ((AArguments[0].Value.AsDateTime.Ticks + TimeSpan.TicksPerDay) - (AArguments[1].Value.AsTimeSpan.Ticks % TimeSpan.TicksPerDay)) % TimeSpan.TicksPerDay;
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond))));
			}
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : TimeSpan, ARightValue : Integer) : TimeSpan;</remarks>
	public class TimeSpanIntegerMultiplicationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsTimeSpan.Ticks * AArguments[1].Value.AsInt32)));
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : Integer, ARightValue : TimeSpan) : DateTIme;</remarks>
	public class IntegerTimeSpanMultiplicationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsInt32 * AArguments[1].Value.AsTimeSpan.Ticks)));
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : TimeSpan, ARightValue : Decimal) : TimeSpan;</remarks>
	public class TimeSpanDecimalMultiplicationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(Convert.ToInt64(AArguments[0].Value.AsTimeSpan.Ticks * AArguments[1].Value.AsDecimal))));
		}
	}
	
	/// <remarks>operator iMultiplication(ALeftValue : Decimal, ARightValue : TimeSpan) : DateTIme;</remarks>
	public class DecimalTimeSpanMultiplicationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(Convert.ToInt64(AArguments[0].Value.AsDecimal * AArguments[1].Value.AsTimeSpan.Ticks))));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : TimeSpan, ARightValue : Integer) : TimeSpan;</remarks>
	public class TimeSpanIntegerDivisionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsTimeSpan.Ticks / AArguments[1].Value.AsInt32)));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : Integer, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class IntegerTimeSpanDivisionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(AArguments[0].Value.AsInt32 / AArguments[1].Value.AsTimeSpan.Ticks)));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : TimeSpan, ARightValue : Decimal) : TimeSpan;</remarks>
	public class TimeSpanDecimalDivisionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(Convert.ToInt64(AArguments[0].Value.AsTimeSpan.Ticks / AArguments[1].Value.AsDecimal))));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : Decimal, ARightValue : TimeSpan) : TimeSpan;</remarks>
	public class DecimalTimeSpanDivisionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan(Convert.ToInt64(AArguments[0].Value.AsDecimal / AArguments[1].Value.AsTimeSpan.Ticks))));
		}
	}
	
	/// <remarks>operator iDivision(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Decimal;</remarks>
	public class TimeSpanTimeSpanDivisionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (decimal)(AArguments[0].Value.AsTimeSpan.Ticks) / (decimal)(AArguments[1].Value.AsTimeSpan.Ticks)));
		}
	}
	
	/// <remarks>operator iEqual(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan == AArguments[1].Value.AsTimeSpan));
		}
	}
	
	public class DateTimeEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime == AArguments[1].Value.AsDateTime));
		}
	}
	
	public class DateEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime == AArguments[1].Value.AsDateTime));
		}
	}
	
	public class TimeEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime == AArguments[1].Value.AsDateTime));
		}
	}
	
	/// <remarks>operator iNotEqual(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan != AArguments[1].Value.AsTimeSpan));
		}
	}
	
	public class DateTimeNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime != AArguments[1].Value.AsDateTime));
		}
	}
	
	public class DateNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime != AArguments[1].Value.AsDateTime));
		}
	}
	
	public class TimeNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime != AArguments[1].Value.AsDateTime));
		}
	}
	
	/// <remarks>operator iGreater(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan > AArguments[1].Value.AsTimeSpan));
		}
	}
	
	public class DateTimeGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime > AArguments[1].Value.AsDateTime));
		}
	}
	
	public class DateGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime > AArguments[1].Value.AsDateTime));
		}
	}
	
	public class TimeGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime > AArguments[1].Value.AsDateTime));
		}
	}
	
	/// <remarks>operator iInclusiveGreater(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan >= AArguments[1].Value.AsTimeSpan));
		}
	}
	
	public class DateTimeInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime >= AArguments[1].Value.AsDateTime));
		}
	}
	
	public class DateInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime >= AArguments[1].Value.AsDateTime));
		}
	}
	
	public class TimeInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime >= AArguments[1].Value.AsDateTime));
		}
	}
	
	/// <remarks>operator iLess(ALeftValue : DateTime, ARightValue : DateTime) : Boolean;</remarks>
	public class DateTimeLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime < AArguments[1].Value.AsDateTime));
		}
	}
	
	/// <remarks>operator iLess(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan < AArguments[1].Value.AsTimeSpan));
		}
	}

	public class DateLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime < AArguments[1].Value.AsDateTime));
		}
	}
	
	public class TimeLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime < AArguments[1].Value.AsDateTime));
		}
	}
	
	/// <remarks>operator iInclusiveLess(ALeftValue : TimeSpan, ARightValue : TimeSpan) : Boolean;</remarks>
	public class TimeSpanInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsTimeSpan <= AArguments[1].Value.AsTimeSpan));
		}
	}

	public class DateTimeInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime <= AArguments[1].Value.AsDateTime));
		}
	}

	public class DateInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime <= AArguments[1].Value.AsDateTime));
		}
	}

	public class TimeInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(DataType);
			else
			#endif
				return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsDateTime <= AArguments[1].Value.AsDateTime));
		}
	}

	public class TimeSpanInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, null);
			return null;
		}
	}
    
	public class DateTimeInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemDateTime, null);
			return null;
		}
	}
    
	public class DateInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemDate, null);
			return null;
		}
	}
    
	public class TimeSpanSumAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				AProcess.Context[1].Value = 
					new Scalar
					(
						AProcess, 
						AProcess.DataTypes.SystemTimeSpan, 
						AProcess.Context[1].Value.IsNil ? 
							AProcess.Context[0].Value.AsTimeSpan : 
							(AProcess.Context[1].Value.AsTimeSpan + AProcess.Context[0].Value.AsTimeSpan)
					);
			return null;
		}
	}
	
	public class TimeSpanMinInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, null);
			return null;
		}
	}
	
	public class DateTimeMinInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemDateTime, null);
			return null;
		}
	}

	public class TimeMinInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTime, null);
			return null;
		}
	}

	public class DateMinInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemDate, null);
			return null;
		}
	}
	
	public class TimeSpanMinAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsTimeSpan < AProcess.Context[1].Value.AsTimeSpan))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class DateTimeMinAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsDateTime < AProcess.Context[1].Value.AsDateTime))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}

	public class TimeMinAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsDateTime < AProcess.Context[1].Value.AsDateTime))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class DateMinAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsDateTime < AProcess.Context[1].Value.AsDateTime))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class TimeSpanMaxInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, null);
			return null;
		}
	}
	
	public class DateTimeMaxInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemDateTime, null);
			return null;
		}
	}
	
	public class TimeMaxInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTime, null);
			return null;
		}
	}
	
	public class DateMaxInitializationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context[0].Value = new Scalar(AProcess, AProcess.DataTypes.SystemDate, null);
			return null;
		}
	}
	
	public class TimeSpanMaxAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsTimeSpan > AProcess.Context[1].Value.AsTimeSpan))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class DateTimeMaxAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsDateTime > AProcess.Context[1].Value.AsDateTime))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class TimeMaxAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsDateTime > AProcess.Context[1].Value.AsDateTime))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class DateMaxAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
				if (AProcess.Context[1].Value.IsNil || (AProcess.Context[0].Value.AsDateTime > AProcess.Context[1].Value.AsDateTime))
					AProcess.Context[1].Value = AProcess.Context[0].Value.Copy();
			return null;
		}
	}
    
	public class TimeSpanAvgInitializationNode : PlanNode
	{
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.Push(new DataVar("LCounter", APlan.Catalog.DataTypes.SystemInteger));
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context.Push(new DataVar("LCounter", AProcess.Plan.Catalog.DataTypes.SystemInteger, new Scalar(AProcess, AProcess.DataTypes.SystemInteger, 0)));
			AProcess.Context[1].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, TimeSpan.Zero);
			return null;
		}
	}
	
	public class TimeSpanAvgAggregationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if ((AProcess.Context[0].Value != null) && !AProcess.Context[0].Value.IsNil)
			{
				AProcess.Context[1].Value = new Scalar(AProcess, AProcess.DataTypes.SystemInteger, checked(AProcess.Context[1].Value.AsInt32 + 1));
				AProcess.Context[2].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, AProcess.Context[2].Value.AsTimeSpan + AProcess.Context[0].Value.AsTimeSpan);
			}
			return null;
		}
	}
    
	public class TimeSpanAvgFinalizationNode : PlanNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			if (AProcess.Context[0].Value.AsInt32 == 0)
				AProcess.Context[1].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, null);
			else
				AProcess.Context[1].Value = new Scalar(AProcess, AProcess.DataTypes.SystemTimeSpan, new TimeSpan(AProcess.Context[1].Value.AsTimeSpan.Ticks / AProcess.Context[0].Value.AsInt32));
			return null;
		}
	}
}
