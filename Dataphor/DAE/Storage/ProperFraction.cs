/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Globalization;

namespace Alphora.Dataphor.DAE.Storage
{
	[TypeConverter(typeof(ProperFractionConverter))]
	public struct ProperFraction
	{
		private static readonly decimal CPercentageScalar = 100m / UInt32.MaxValue;

		public ProperFraction(decimal ADecimal)
		{
			FValue = (uint)(ADecimal * (decimal)UInt32.MaxValue);
		}

		public ProperFraction(uint AValue)
		{
			FValue = AValue;
		}

		private uint FValue;

		/// <summary> Fraction represented as a percentage. </summary>
		public decimal Percent
		{
			get { return FValue * CPercentageScalar; }
			set { FValue = (uint)(Math.Min(100m, Math.Max(0m, value)) / (decimal)CPercentageScalar); }
		}

		/// <summary> Decimal form of the fraction (between 0 and 1). </summary>
		public decimal Decimal
		{
			get { return (decimal)FValue / (decimal)UInt32.MaxValue; }
			set { FValue = (uint)(value * (decimal)UInt32.MaxValue); }
		}

		/// <summary> Number between 0 and (2^32 - 1) internally representing the proper fraction. </summary>
		public uint Value
		{
			get { return FValue; }
			set { FValue = value; }
		}

		public static decimal operator *(decimal AValue, ProperFraction AFraction)
		{
			return AFraction.Decimal * AValue;
		}

		public static int operator *(int AValue, ProperFraction AFraction)
		{
			return (int)(((long)AValue * (long)AFraction.Value) / (long)UInt32.MaxValue);
		}

		public static uint operator *(uint AValue, ProperFraction AFraction)
		{
			return (uint)(((long)AValue * (long)AFraction.Value) / (long)UInt32.MaxValue);
		}
	}

	public class ProperFractionConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext AContext, Type ASourceType)
		{
			if (ASourceType == typeof(string))
				return true;
			else
				return base.CanConvertFrom(AContext, ASourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext AContext, Type ATargetType)
		{
			if (ATargetType == typeof(string))
				return true;
			else
				return base.CanConvertTo(AContext, ATargetType);
		}

		public override object ConvertFrom(ITypeDescriptorContext AContext, CultureInfo ACulture, object AValue)
		{
			if (AValue is string)
			{
				string LValue = ((string)AValue).Trim();
				if (LValue.Length > 0)
				{
					ProperFraction LResult = new ProperFraction();
					char LFirst = LValue[0];
					if (LFirst == '%')
						LResult.Percent = Convert.ToDecimal(LValue.Substring(1));
					else
						LResult.Decimal =  Convert.ToDecimal(LValue.Substring(1));
					return LResult;
				}
			}
			return base.ConvertFrom(AContext, ACulture, AValue);
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, CultureInfo ACulture, object AValue, Type ATargetType)
		{
			if (ATargetType == typeof(string))
				return Convert.ToString(((ProperFraction)AValue).Decimal);
			else
				return base.ConvertTo(AContext, ACulture, AValue, ATargetType);
		}
	}
}