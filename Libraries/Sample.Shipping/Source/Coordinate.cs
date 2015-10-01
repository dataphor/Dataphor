/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Shipping
{
	using System;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	/// <summary> Geographical coordinate host implementation. </summary>
	public struct Coordinate
	{
		public Coordinate(decimal ALatitude, decimal ALongitude)
		{
			Latitude = ALatitude;
			Longitude = ALongitude;
		}

		public decimal Latitude;
		public decimal Longitude;
		
		public override string ToString()
		{
			return CoordinateUtility.CoordinateToString(this);
		}
		
		public static Coordinate Parse(string AValue)
		{
			return CoordinateUtility.StringToCoordinate(AValue);
		}
		
		public int CompareTo(Coordinate ACoordinate)
		{
			int LResult = Latitude.CompareTo(ACoordinate.Latitude);
			if (LResult == 0)
				return Longitude.CompareTo(ACoordinate.Longitude);
			else
				return LResult;
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is Coordinate) && (((Coordinate)AObject).Latitude == Latitude) && (((Coordinate)AObject).Longitude == Longitude);
		}
		
		public override int GetHashCode()
		{
			return Latitude.GetHashCode() ^ Longitude.GetHashCode();
		}
	};
	
	public class CoordinateConveyor : Conveyor
	{
		public CoordinateConveyor() : base() {}
		
		public unsafe override int GetSize(object tempValue)
		{
			return sizeof(Coordinate);
		}

		public unsafe override object Read(byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = &(buffer[offset]))
			{
				return *((Coordinate*)bufferPtr);
			}
		}

		public unsafe override void Write(object tempValue, byte[] buffer, int offset)
		{
			fixed (byte* bufferPtr = &(buffer[offset]))
			{
				*((Coordinate*)bufferPtr) = (Coordinate)tempValue;
			}
		}
	}
	
/*
	// operator Degree(ADegrees : integer, AMinutes : integer, ASeconds : decimal) : Degree;
	public class DegreeSelector : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null) || (AArguments[2].Value == null))
				return new DataVar(FDataType);
			else
				return CoordinateUtility.GetDegree((int)AArguments[0], (int)AArguments[1], (decimal)AArguments[2])));
		}
	}
	
	// operator ReadDegreesPart(ADegree : Degree) : integer;
	public class DegreesPartReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				IScalar LDegree = (IScalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, CoordinateUtility.GetDegrees(((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree))));
			}
		}
	}
	
	// operator WriteDegreesPart(ADegree : Degree, AValue : integer) : Degree;
	public class DegreesPartWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				IScalar LDegree = (IScalar)AArguments[0].Value;
				decimal LValue = ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree);
				return CoordinateUtility.GetDegree(((IScalar)AArguments[1].Value).ToInt32(), CoordinateUtility.GetMinutes(LValue), CoordinateUtility.GetSeconds(LValue))));
			}
		}
	}
	
	// operator ReadMinutesPart(ADegree : Degree) : integer;
	public class MinutesPartReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				IScalar LDegree = (IScalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, CoordinateUtility.GetMinutes(((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree))));
			}
		}
	}
	
	// operator WriteMinutesPart(ADegree : Degree, AValue : integer) : Degree;
	public class MinutesPartWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				IScalar LDegree = (IScalar)AArguments[0].Value;
				decimal LValue = ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree);
				return CoordinateUtility.GetDegree(CoordinateUtility.GetDegrees(LValue), ((Scalar)AArguments[1].Value).ToInt32(), CoordinateUtility.GetSeconds(LValue))));
			}
		}
	}
	
	// operator ReadSecondsPart(ADegree : Degree) : decimal;
	public class SecondsPartReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				IScalar LDegree = (IScalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.GetSeconds(((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree))));
			}
		}
	}
	
	// operator WriteSecondsPart(ADegree : Degree, AValue : decimal) : Degree;
	public class SecondsPartWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				IScalar LDegree = (IScalar)AArguments[0].Value;
				decimal LValue = ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree);
				return CoordinateUtility.GetDegree(CoordinateUtility.GetDegrees(LValue), CoordinateUtility.GetMinutes(LValue), ((IScalar)AArguments[1].Value).ToDecimal())));
			}
		}
	}
	
	// operator Degrees(ADegrees : decimal) : Degree;
	public class DegreesSelector : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return ((IScalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadDegrees(ADegree : Degree) : decimal;
	public class DegreesReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (IScalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree)));
			}
		}
	}
	
	// operator WriteDegrees(ADegree : Degree, AValue : decimal) : Degree;
	public class DegreesWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return ((IScalar)AArguments[1].Value).ToDecimal()));
		}
	}
*/
	
	// operator Coordinate(ALatitude : Degree, ALongitude : Degree) : Coordinate;
	public class CoordinateSelector : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
				return new Coordinate((decimal)argument1, (decimal)argument2);
		}
	}
	
	// operator ReadLatitude(ACoordinate : Coordinate) : Degree;
	public class LatitudeReadAccessor : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			if (argument1 == null)
				return null;
			else
				return ((Coordinate)argument1).Latitude;
		}
	}
	
	// operator WriteLatitude(ACoordinate : Coordinate, ALatitude : Degree) : Coordinate;
	public class LatitudeWriteAccessor : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
				return new Coordinate((decimal)argument2, ((Coordinate)argument1).Longitude);
		}
	}
	
	// operator ReadLongitude(ACoordinate : Coordinate) : Degree;
	public class LongitudeReadAccessor : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			if (argument1 == null)
				return null;
			else
				return ((Coordinate)argument1).Longitude;
		}
	}
	
	// operator WriteLongitude(ACoordinate : Coordinate, ALongitude : Degree) : Coordinate;
	public class LongitudeWriteAccessor : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
				return new Coordinate(((Coordinate)argument1).Latitude, (decimal)argument2);
		}
	}
	
	// operator iCompare(ACoordinate1 : Coordinate, ACoordinate2 : Coordinate) : integer;
	public class CoordinateCompare : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			if ((argument1 == null) || (argument2 == null))
				return null;
			else
				return ((Coordinate)argument1).CompareTo((Coordinate)argument2);
		}
	}
	
/*
	// operator Miles(AMiles : decimal) : Distance;
	public class MilesSelector : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((IScalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadMiles(ADistance : Distance) : decimal;
	public class MilesReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((IScalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator WriteMiles(ADistance : Distance, AMiles : decimal) : Distance;
	public class MilesWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((IScalar)AArguments[1].Value).ToDecimal()));
		}
	}
	
	// operator Kilometers(AKilometers : decimal) : Distance;
	public class KilometersSelector : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.KMToMiles(((IScalar)AArguments[0].Value).ToDecimal())));
		}
	}
	
	// operator ReadKilometers(ADistance : Distance) : decimal;
	public class KilometersReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.MilesToKM(((IScalar)AArguments[0].Value).ToDecimal())));
		}
	}
	
	// operator WriteKilometers(ADistance : Distance, AKilometers : decimal) : Distance;
	public class KilometersWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.KMToMiles(((IScalar)AArguments[1].Value).ToDecimal())));
		}
	}
	
	// operator Distance(AFromCoordinate : Coordinate, AToCoordinate : Coordinate) : Distance
	public class DistanceNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				Scalar LValue1 = (IScalar)AArguments[0].Value;
				Scalar LValue2 = (IScalar)AArguments[1].Value;
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.CalculateDistance(((CoordinateConveyor)LValue1.Conveyor).GetValue(LValue1), ((CoordinateConveyor)LValue2.Conveyor).GetValue(LValue2))));
			}
		}
	}
	
	// operator Percent(APercent : decimal) : Degree;
	public class PercentSelector : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return ((IScalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadPercent(APercent : Percent) : decimal;
	public class PercentReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				return ((IScalar)AArguments[0].Value).ToDecimal()));
			}
		}
	}
	
	// operator WritePercent(APercent : Percent, AValue : decimal) : Percent;
	public class PercentWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return ((IScalar)AArguments[1].Value).ToDecimal()));
		}
	}
	
	// operator Percent(AQuotient : decimal, ADivisor : decimal) : Percent;
	public class PercentNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, (((IScalar)AArguments[0].Value).ToDecimal() / ((Scalar)AArguments[1].Value).ToDecimal()) * 100m));
		}
	}
	
	// operator iMultiplication(AValue : decimal, APercent : Percent) : decimal;
	public class DecimalPercentMultiplicationNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((IScalar)AArguments[0].Value).ToDecimal() * (((Scalar)AArguments[1].Value).ToDecimal() / 100m)));
		}
	}
	
	// operator iMultiplication(APercent : Percent, ADecimal : decimal) : decimal;
	public class PercentDecimalMultiplicationNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, (((IScalar)AArguments[0].Value).ToDecimal() / 100m) * ((Scalar)AArguments[1].Value).ToDecimal()));
		}
	}
	
	// operator DollarsPerMile(AAmount : money) : ShippingRate;
	public class DollarsPerMileSelector : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return ((IScalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadRate(ARate : ShippingRate) : money;
	public class RateReadAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				return ((IScalar)AArguments[0].Value).ToDecimal()));
			}
		}
	}
	
	// operator WriteRate(ARate : ShippingRate, AValue : money) : ShippingRate;
	public class RateWriteAccessor : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return ((IScalar)AArguments[1].Value).ToDecimal()));
		}
	}
*/
	
	public sealed class CoordinateUtility
	{
		public const string InvalidCoordinateFormat = @"Coordinate string must be of the form: DDD MM'SS.SS""/DDD MM'SS.SS"".";
		
		public static decimal GetDegree(int degrees, int minutes, decimal seconds)
		{
			return (decimal)degrees + ((decimal)minutes / 60m) + (seconds / 3600m);
		}
		
		public static int GetDegrees(decimal tempValue)
		{
			return (int)Decimal.Truncate(tempValue);
		}

		public static int GetMinutes(decimal tempValue)
		{
			return (int)Decimal.Truncate((tempValue - Decimal.Truncate(tempValue)) * 60m);
		}

		public static decimal GetSeconds(decimal tempValue)
		{
			decimal decimalPart = tempValue - Decimal.Truncate(tempValue);
			return Decimal.Round((decimalPart - ((decimal)GetMinutes(tempValue) / 60m)) * 3600m, 2);
		}
		
		public static string CoordinateToString(Coordinate coordinate)
		{
			return 
				String.Format
				(
					@"{0}/{1}", 
					new object[]
					{
						DegreeToString(coordinate.Latitude),
						DegreeToString(coordinate.Longitude)
					}
				);
		}
		
		public static string DegreeToString(decimal degree)
		{
			return
				String.Format
				(
					@"{0} {1}'{2}""",
					new object[]
					{
						GetDegrees(degree),
						GetMinutes(degree),
						GetSeconds(degree)
					}
				);
		}
		
		public static Coordinate StringToCoordinate(string tempValue)
		{
			string[] values = tempValue.Split('/');
			if (values.Length != 2)
				throw new Exception(InvalidCoordinateFormat);
				
			return new Coordinate(StringToDegree(values[0]), StringToDegree(values[1]));
		}
		
		public static Decimal StringToDegree(string tempValue)
		{
			int firstIndex = tempValue.IndexOf(" ");
			if (firstIndex < 0)
				throw new Exception(InvalidCoordinateFormat);

			int degrees = Int32.Parse(tempValue.Substring(0, firstIndex));
			int secondIndex = tempValue.IndexOf("'", firstIndex);
			if (secondIndex < 0)
				throw new Exception(InvalidCoordinateFormat);

			firstIndex++;
			int minutes = Int32.Parse(tempValue.Substring(firstIndex, secondIndex - firstIndex));
			firstIndex = secondIndex;
			secondIndex = tempValue.IndexOf("\"", firstIndex);
			if (secondIndex < 0)
				throw new Exception(InvalidCoordinateFormat);
			
			firstIndex++;
			decimal seconds = Decimal.Parse(tempValue.Substring(firstIndex, secondIndex - firstIndex));
			return GetDegree(degrees, minutes, seconds);
		}
				
		public static decimal MilesToKM(decimal miles)
		{
			return (miles * 1.609m);
		}

		public static decimal KMToMiles(decimal kM)
		{
			return (kM * 0.621m);
		}
		
		public static decimal CalculateDistance(Coordinate fromCoordinate, Coordinate toCoordinate)
		{
			decimal deltaLatitude = toCoordinate.Latitude - fromCoordinate.Latitude;
			decimal deltaLongitude = toCoordinate.Longitude - fromCoordinate.Latitude;
			decimal distance = (decimal)Math.Sqrt((double)(deltaLatitude * deltaLatitude + deltaLongitude * deltaLongitude));
			return KMToMiles(distance / 0.008987m);
		}
	}
}
