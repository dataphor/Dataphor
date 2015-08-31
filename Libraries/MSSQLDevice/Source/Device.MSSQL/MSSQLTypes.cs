/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USECONNECTIONPOOLING
#define USESQLOLEDB
//#define USEOLEDBCONNECTION
//#define USEADOCONNECTION

using System;
using System.Globalization;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Device.MSSQL
{
	/*
			
        Data type mapping ->
		
            DAE Type	|	TSQL Type														|	Translation Handler
            ------------|---------------------------------------------------------------------------------------------
            Boolean		|	bit																|	MSSQLBoolean
            Byte		|   tinyint															|	MSSQLByte
            SByte		|	smallint														|	SQLSByte
            Short		|	smallint														|	SQLShort
            UShort		|	integer															|	SQLUShort
            Integer		|	integer															|	SQLInteger
            UInteger	|	bigint															|	SQLUInteger
            Long		|	bigint															|	SQLLong
            ULong		|	decimal(20, 0)													|	SQLULong
            Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SQLDecimal
            TimeSpan	|	bigint															|	SQLTimeSpan
            DateTime	|	datetime														|	MSSQLDateTime
            Date		|	datetime														|	MSSQLDate
            Time		|	datetime														|	MSSQLTime
            Money		|	money															|	MSSQLMoney
            Guid		|	uniqueidentifier												|	MSSQLGuid
            String		|	varchar(Storage.Length)											|	SQLString
            Binary		|	image															|	MSSQLBinary
            SQLText		|	text															|	MSSQLText
            MSSQLBinary |	binary(Storage.Length)											|	MSSQLMSSQLBinary

    */

	// TODO: Support for date and time:
		// The current mapping for System.Date uses MSSQLDate, which uses a native representation in the target system as datetime
		// The current mapping for System.Time uses MSSQLTime, which uses a native representation in the target system as datetime
		// We need a solution that would use the new date and time types, as well as potentially maintain functionality for systems that still use the datetime type
		// For now, if the underlying system uses a date/time type, the reconciliation will accept that type as a valid type mapping for System.Date/System.Time

	// TODO: Support for datetime2
		// The new datetime2 type is still a datetime, but has greater scale and precision, so we need an MSSQLDateTime2 mapping to support it

	// TODO: Support for datetimeoffset type
		// We should look at supporting this type directly in D4? There is a C# DateTimeOffset type as well
		// We should actually just change our DateTime to use DateTimeOffset (exposing a timezone offset property), although I'm not sure how that would map in other systems

	// TODO: Support for varchar(max) and varbinary(max)
	// TODO: Support for xml type?

	/// <summary>
	/// MSSQL type : bit
	///	D4 Type : Boolean
	/// 0 = false
	/// 1 = true
	/// </summary>
	public class MSSQLBoolean : SQLScalarType
	{
		public MSSQLBoolean(int AID, string AName)
			: base(AID, AName)
		{
		}

		//public MSSQLBoolean(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLBoolean(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());

			return String.Format("cast({0} as {1})", (bool)AValue ? "1" : "0", DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue is bool)
				return (bool)AValue;
			else
				return (int)AValue == 0 ? false : true;
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (bool)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLBooleanType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "bit";
		}
	}

	/// <summary>
	/// MSSQL type : tinyint
	/// D4 Type : Byte
	/// </summary>
	public class MSSQLByte : SQLScalarType
	{
		public MSSQLByte(int AID, string AName)
			: base(AID, AName)
		{
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			// According to the docs for the SQLOLEDB provider this is supposed to come back as a byte, but
			// it is coming back as a short, I don't know why, maybe interop?
			return Convert.ToByte(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (byte)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLIntegerType(1);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "tinyint";
		}
	}

	/// <summary>
	/// MSSQL type : money
	/// D4 Type : Money
	/// </summary>
	public class MSSQLMoney : SQLScalarType
	{
		public MSSQLMoney(int AID, string AName)
			: base(AID, AName)
		{
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToDecimal(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (decimal)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLMoneyType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "money";
		}
	}

	/// <summary>
	/// MSSQL type : datetime
	/// D4 Type : DateTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// The ADO connectivity layer seems to be rounding datetime values to the nearest second, even though the server is capable of storing greater precision
	/// </summary>
	public class MSSQLDateTime : SQLScalarType
	{
		public const string CDateTimeFormat = "yyyy/MM/dd HH:mm:ss";

		public static readonly DateTime Accuracy = new DateTime((long)(TimeSpan.TicksPerMillisecond * 3.33));
		public static readonly DateTime MinValue = new DateTime(1753, 1, 1);

		private string FDateTimeFormat = CDateTimeFormat;

		public MSSQLDateTime(int AID, string AName) : base(AID, AName) { }

		public string DateTimeFormat
		{
			get { return FDateTimeFormat; }
			set { FDateTimeFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());

			DateTime LValue = (DateTime)AValue;
			if (LValue == DateTime.MinValue)
				LValue = MinValue;
			if (LValue < MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

			return String.Format("cast('{0}' as {1})", LValue.ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			var LDateTime = (DateTime)AValue;
			// If the value is equal to the device's zero date, set it to Dataphor's zero date
			if (LDateTime == MinValue)
				LDateTime = DateTime.MinValue;
			long LTicks = LDateTime.Ticks;
			return new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond));
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			DateTime LValue = (DateTime)AValue;
			// If the value is equal to Dataphor's zero date, set it to the Device's zero date
			if (LValue == DateTime.MinValue)
				LValue = MinValue;
			if (LValue < MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());
			return LValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "datetime";
		}
	}

	/// <summary>
	/// MSSQL type : datetime
	/// D4 Type : Date
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
	public class MSSQLDate : SQLScalarType
	{
		public const string CDateFormat = "yyyy/MM/dd";

		private string FDateFormat = CDateFormat;

		public MSSQLDate(int AID, string AName) : base(AID, AName) { }

		public string DateFormat
		{
			get { return FDateFormat; }
			set { FDateFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());

			DateTime LValue = (DateTime)AValue;
			// If the value is equal to Dataphor's zero date (Jan, 1, 0001), set it to the device's zero date
			if (LValue == DateTime.MinValue)
				LValue = MSSQLDateTime.MinValue;
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

			return String.Format("cast('{0}' as {1})", LValue.ToString(DateFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			var LDateTime = (DateTime)AValue;
			// If the value is equal to the Device's zero date, set it to Dataphor's zero date
			if (LDateTime == MSSQLDateTime.MinValue)
				LDateTime = DateTime.MinValue;
			long LTicks = LDateTime.Ticks;
			return new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay));
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			DateTime LValue = (DateTime)AValue;
			// If the value is equal to Dataphor's zero date (Jan, 1, 0001), set it to the device's zero date
			if (LValue == DateTime.MinValue)
				LValue = MSSQLDateTime.MinValue;
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());
			return LValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLDateType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "datetime";
		}
	}

	/// <summary>
	/// MSSQL type : datetime
	/// D4 Type : SQLTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
	public class MSSQLTime : SQLScalarType
	{
		public const string CTimeFormat = "HH:mm:ss";

		private string FTimeFormat = CTimeFormat;

		public MSSQLTime(int AID, string AName) : base(AID, AName) { }

		public string TimeFormat
		{
			get { return FTimeFormat; }
			set { FTimeFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());

			// Added 1899 years, so that a time can actually be stored. 
			// Adding 1899 years puts it at the year 1900
			// which is stored as zero in MSSQL.
			// this year value of 1900 may make some translation easier.
			DateTime LValue = ((DateTime)AValue).AddYears(1899);
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

			return String.Format("cast('{0}' as {1})", LValue.ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return new DateTime(((DateTime)AValue).Ticks % TimeSpan.TicksPerDay);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			// Added 1899 years, so that a time can actually be stored. 
			// Adding 1899 years puts it at the year 1900
			// which is stored as zero in MSSQL.
			// this year value of 1900 may make some translation easier.
			DateTime LValue = ((DateTime)AValue).AddYears(1899);
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());
			return LValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLTimeType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "datetime";
		}
	}

	/// <summary>
	/// MSSQL type : uniqueidentifier
	/// D4 type : Guid
	/// TSQL comparison operators for the TSQL uniqueidentifier data type use string semantics, not hexadecimal
	/// </summary>
	public class MSSQLGuid : SQLScalarType
	{
		public MSSQLGuid(int AID, string AName) : base(AID, AName) { }

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());

			return String.Format("cast('{0}' as {1})", (Guid)AValue, DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue is string)
				return new Guid((string)AValue);
			else
				return (Guid)AValue;
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (Guid)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLGuidType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "uniqueidentifier";
		}
	}

	/// <summary>
	/// MSSQL type : text
	/// D4 Type : SQLText, SQLIText
	/// </summary>
	public class MSSQLText : SQLText
	{
		public MSSQLText(int AID, string AName) : base(AID, AName) { }

		//public MSSQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "text";
		}
	}

	/// <summary>
	/// MSSQL type : image
	/// D4 type : Binary
	/// </summary>
	public class MSSQLBinary : SQLBinary
	{
		public MSSQLBinary(int AID, string AName) : base(AID, AName) { }

		//public MSSQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "image";
		}
	}

	/// <summary>
	/// MSSQL type : image
	/// D4 type : Graphic
	/// </summary>
	public class MSSQLGraphic : SQLGraphic
	{
		public MSSQLGraphic(int AID, string AName) : base(AID, AName) { }

		//public MSSQLGraphic(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLGraphic(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "image";
		}
	}

	/// <summary>
	/// MSSQL type : binary(Storage.Length)
	/// D4 Type : MSSQLBinary
	/// </summary>
	public class MSSQLMSSQLBinary : SQLScalarType
	{
		public MSSQLMSSQLBinary(int AID, string AName) : base(AID, AName) { }

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());

			return String.Format("cast('{0}' as {1})", Convert.ToBase64String((byte[])AValue), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (byte[])AValue;
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (byte[])AValue;
		}

		protected int GetLength(MetaData AMetaData)
		{
			return Int32.Parse(GetTag("Storage.Length", "30", AMetaData));
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLByteArrayType(GetLength(AMetaData));
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return String.Format("binary({0})", GetLength(AMetaData)); // todo: what about varbiniary?
		}
	}
}