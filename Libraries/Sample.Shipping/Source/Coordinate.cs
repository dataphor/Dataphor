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
		
		public unsafe override int GetSize(object AValue)
		{
			return sizeof(Coordinate);
		}

		public unsafe override object Read(byte[] ABuffer, int AOffset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
			{
				return *((Coordinate*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int AOffset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
			{
				*((Coordinate*)LBufferPtr) = (Coordinate)AValue;
			}
		}
	}
	
/*
	// operator Degree(ADegrees : integer, AMinutes : integer, ASeconds : decimal) : Degree;
	public class DegreeSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null) || (AArguments[2].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, CoordinateUtility.GetDegree(AArguments[0].Value.AsInt32, AArguments[1].Value.AsInt32, AArguments[2].Value.AsDecimal)));
		}
	}
	
	// operator ReadDegreesPart(ADegree : Degree) : integer;
	public class DegreesPartReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, CoordinateUtility.GetDegrees(((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree))));
			}
		}
	}
	
	// operator WriteDegreesPart(ADegree : Degree, AValue : integer) : Degree;
	public class DegreesPartWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				decimal LValue = ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, CoordinateUtility.GetDegree(((Scalar)AArguments[1].Value).ToInt32(), CoordinateUtility.GetMinutes(LValue), CoordinateUtility.GetSeconds(LValue))));
			}
		}
	}
	
	// operator ReadMinutesPart(ADegree : Degree) : integer;
	public class MinutesPartReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromInt32(AProcess, CoordinateUtility.GetMinutes(((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree))));
			}
		}
	}
	
	// operator WriteMinutesPart(ADegree : Degree, AValue : integer) : Degree;
	public class MinutesPartWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				decimal LValue = ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, CoordinateUtility.GetDegree(CoordinateUtility.GetDegrees(LValue), ((Scalar)AArguments[1].Value).ToInt32(), CoordinateUtility.GetSeconds(LValue))));
			}
		}
	}
	
	// operator ReadSecondsPart(ADegree : Degree) : decimal;
	public class SecondsPartReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.GetSeconds(((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree))));
			}
		}
	}
	
	// operator WriteSecondsPart(ADegree : Degree, AValue : decimal) : Degree;
	public class SecondsPartWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				decimal LValue = ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, CoordinateUtility.GetDegree(CoordinateUtility.GetDegrees(LValue), CoordinateUtility.GetMinutes(LValue), ((Scalar)AArguments[1].Value).ToDecimal())));
			}
		}
	}
	
	// operator Degrees(ADegrees : decimal) : Degree;
	public class DegreesSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadDegrees(ADegree : Degree) : decimal;
	public class DegreesReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				Scalar LDegree = (Scalar)AArguments[0].Value;
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((DegreeConveyor)LDegree.Conveyor).GetValue(LDegree)));
			}
		}
	}
	
	// operator WriteDegrees(ADegree : Degree, AValue : decimal) : Degree;
	public class DegreesWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[1].Value).ToDecimal()));
		}
	}
*/
	
	// operator Coordinate(ALatitude : Degree, ALongitude : Degree) : Coordinate;
	public class CoordinateSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Coordinate(AArguments[0].Value.AsDecimal, AArguments[1].Value.AsDecimal)));
		}
	}
	
	// operator ReadLatitude(ACoordinate : Coordinate) : Degree;
	public class LatitudeReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Coordinate)AArguments[0].Value.AsNative).Latitude));
		}
	}
	
	// operator WriteLatitude(ACoordinate : Coordinate, ALatitude : Degree) : Coordinate;
	public class LatitudeWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Coordinate(AArguments[1].Value.AsDecimal, ((Coordinate)AArguments[0].Value.AsNative).Longitude)));
		}
	}
	
	// operator ReadLongitude(ACoordinate : Coordinate) : Degree;
	public class LongitudeReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Coordinate)AArguments[0].Value.AsNative).Longitude));
		}
	}
	
	// operator WriteLongitude(ACoordinate : Coordinate, ALongitude : Degree) : Coordinate;
	public class LongitudeWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Coordinate(((Coordinate)AArguments[0].Value.AsNative).Latitude, AArguments[1].Value.AsDecimal)));
		}
	}
	
	// operator iCompare(ACoordinate1 : Coordinate, ACoordinate2 : Coordinate) : integer;
	public class CoordinateCompare : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Coordinate)AArguments[0].Value.AsNative).CompareTo((Coordinate)AArguments[1].Value.AsNative)));
		}
	}
	
/*
	// operator Miles(AMiles : decimal) : Distance;
	public class MilesSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((Scalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadMiles(ADistance : Distance) : decimal;
	public class MilesReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((Scalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator WriteMiles(ADistance : Distance, AMiles : decimal) : Distance;
	public class MilesWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((Scalar)AArguments[1].Value).ToDecimal()));
		}
	}
	
	// operator Kilometers(AKilometers : decimal) : Distance;
	public class KilometersSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.KMToMiles(((Scalar)AArguments[0].Value).ToDecimal())));
		}
	}
	
	// operator ReadKilometers(ADistance : Distance) : decimal;
	public class KilometersReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.MilesToKM(((Scalar)AArguments[0].Value).ToDecimal())));
		}
	}
	
	// operator WriteKilometers(ADistance : Distance, AKilometers : decimal) : Distance;
	public class KilometersWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.KMToMiles(((Scalar)AArguments[1].Value).ToDecimal())));
		}
	}
	
	// operator Distance(AFromCoordinate : Coordinate, AToCoordinate : Coordinate) : Distance
	public class DistanceNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
			{
				Scalar LValue1 = (Scalar)AArguments[0].Value;
				Scalar LValue2 = (Scalar)AArguments[1].Value;
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, CoordinateUtility.CalculateDistance(((CoordinateConveyor)LValue1.Conveyor).GetValue(LValue1), ((CoordinateConveyor)LValue2.Conveyor).GetValue(LValue2))));
			}
		}
	}
	
	// operator Percent(APercent : decimal) : Degree;
	public class PercentSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadPercent(APercent : Percent) : decimal;
	public class PercentReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[0].Value).ToDecimal()));
			}
		}
	}
	
	// operator WritePercent(APercent : Percent, AValue : decimal) : Percent;
	public class PercentWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[1].Value).ToDecimal()));
		}
	}
	
	// operator Percent(AQuotient : decimal, ADivisor : decimal) : Percent;
	public class PercentNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, (((Scalar)AArguments[0].Value).ToDecimal() / ((Scalar)AArguments[1].Value).ToDecimal()) * 100m));
		}
	}
	
	// operator iMultiplication(AValue : decimal, APercent : Percent) : decimal;
	public class DecimalPercentMultiplicationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, ((Scalar)AArguments[0].Value).ToDecimal() * (((Scalar)AArguments[1].Value).ToDecimal() / 100m)));
		}
	}
	
	// operator iMultiplication(APercent : Percent, ADecimal : decimal) : decimal;
	public class PercentDecimalMultiplicationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, Scalar.FromDecimal(AProcess, (((Scalar)AArguments[0].Value).ToDecimal() / 100m) * ((Scalar)AArguments[1].Value).ToDecimal()));
		}
	}
	
	// operator DollarsPerMile(AAmount : money) : ShippingRate;
	public class DollarsPerMileSelector : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[0].Value).ToDecimal()));
		}
	}
	
	// operator ReadRate(ARate : ShippingRate) : money;
	public class RateReadAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].Value == null)
				return new DataVar(FDataType);
			else
			{
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[0].Value).ToDecimal()));
			}
		}
	}
	
	// operator WriteRate(ARate : ShippingRate, AValue : money) : ShippingRate;
	public class RateWriteAccessor : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[1].Value == null)
				return new DataVar(FDataType);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Scalar)AArguments[1].Value).ToDecimal()));
		}
	}
*/
	
	public sealed class CoordinateUtility
	{
		public const string CInvalidCoordinateFormat = @"Coordinate string must be of the form: DDD MM'SS.SS""/DDD MM'SS.SS"".";
		
		public static decimal GetDegree(int ADegrees, int AMinutes, decimal ASeconds)
		{
			return (decimal)ADegrees + ((decimal)AMinutes / 60m) + (ASeconds / 3600m);
		}
		
		public static int GetDegrees(decimal AValue)
		{
			return (int)Decimal.Truncate(AValue);
		}

		public static int GetMinutes(decimal AValue)
		{
			return (int)Decimal.Truncate((AValue - Decimal.Truncate(AValue)) * 60m);
		}

		public static decimal GetSeconds(decimal AValue)
		{
			decimal LDecimalPart = AValue - Decimal.Truncate(AValue);
			return Decimal.Round((LDecimalPart - ((decimal)GetMinutes(AValue) / 60m)) * 3600m, 2);
		}
		
		public static string CoordinateToString(Coordinate ACoordinate)
		{
			return 
				String.Format
				(
					@"{0}/{1}", 
					new object[]
					{
						DegreeToString(ACoordinate.Latitude),
						DegreeToString(ACoordinate.Longitude)
					}
				);
		}
		
		public static string DegreeToString(decimal ADegree)
		{
			return
				String.Format
				(
					@"{0} {1}'{2}""",
					new object[]
					{
						GetDegrees(ADegree),
						GetMinutes(ADegree),
						GetSeconds(ADegree)
					}
				);
		}
		
		public static Coordinate StringToCoordinate(string AValue)
		{
			string[] LValues = AValue.Split('/');
			if (LValues.Length != 2)
				throw new Exception(CInvalidCoordinateFormat);
				
			return new Coordinate(StringToDegree(LValues[0]), StringToDegree(LValues[1]));
		}
		
		public static Decimal StringToDegree(string AValue)
		{
			int LFirstIndex = AValue.IndexOf(" ");
			if (LFirstIndex < 0)
				throw new Exception(CInvalidCoordinateFormat);

			int LDegrees = Int32.Parse(AValue.Substring(0, LFirstIndex));
			int LSecondIndex = AValue.IndexOf("'", LFirstIndex);
			if (LSecondIndex < 0)
				throw new Exception(CInvalidCoordinateFormat);

			LFirstIndex++;
			int LMinutes = Int32.Parse(AValue.Substring(LFirstIndex, LSecondIndex - LFirstIndex));
			LFirstIndex = LSecondIndex;
			LSecondIndex = AValue.IndexOf("\"", LFirstIndex);
			if (LSecondIndex < 0)
				throw new Exception(CInvalidCoordinateFormat);
			
			LFirstIndex++;
			decimal LSeconds = Decimal.Parse(AValue.Substring(LFirstIndex, LSecondIndex - LFirstIndex));
			return GetDegree(LDegrees, LMinutes, LSeconds);
		}
				
		public static decimal MilesToKM(decimal AMiles)
		{
			return (AMiles * 1.609m);
		}

		public static decimal KMToMiles(decimal AKM)
		{
			return (AKM * 0.621m);
		}
		
		public static decimal CalculateDistance(Coordinate AFromCoordinate, Coordinate AToCoordinate)
		{
			decimal LDeltaLatitude = AToCoordinate.Latitude - AFromCoordinate.Latitude;
			decimal LDeltaLongitude = AToCoordinate.Longitude - AFromCoordinate.Latitude;
			decimal LDistance = (decimal)Math.Sqrt((double)(LDeltaLatitude * LDeltaLatitude + LDeltaLongitude * LDeltaLongitude));
			return KMToMiles(LDistance / 0.008987m);
		}
	}
}
