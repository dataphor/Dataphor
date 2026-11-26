using System;
using System.Globalization;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Device.PGSQL
{

	/// <summary>
	/// PostgreSQL type : bit
	///	D4 Type : Boolean
	/// 0 = false
	/// 1 = true
	/// </summary>
	public class PostgreSQLBoolean : SQLScalarType
	{
		public PostgreSQLBoolean(int AID, string AName)
			: base(AID, AName)
		{
		}

		//public PostgreSQLBoolean(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public PostgreSQLBoolean(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

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
			return "boolean";
		}
	}

	/// <summary>
	/// PostgreSQL type : tinyint
	/// D4 Type : Byte
	/// </summary>
	public class PostgreSQLByte : SQLScalarType
	{
		public PostgreSQLByte(int AID, string AName)
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
            return "smallint";
		}
	}

	/// <summary>
	/// PostgreSQL type : money
	/// D4 Type : Money
	/// </summary>
	public class PostgreSQLMoney : SQLScalarType
	{
		public PostgreSQLMoney(int AID, string AName)
			: base(AID, AName)
		{
			// PostgreSQL has no implicit conversion between money and numeric, and no money <> numeric operator at all, so a
			// Money literal translated as a literal makes the whole statement fail: SQLDevice.FromScalar emits
			// ValueExpression(ParameterFromScalar(...)) for a literal of a type that does not ask for a parameter, which for
			// this type is a bare decimal - "where ("T1"."Salary" > 1000.0)" against a money column, which PostgreSQL rejects
			// with "operator does not exist: money > numeric". Asking for a parameter instead sends the value as
			// NpgsqlDbType.Money, which compares. (ToLiteral is not the fix: it is only consulted for cursor key parameters,
			// not for a literal operand of a translated expression.)
			// Reproductions: the Money cases of the device-coverage corpus (LivePGSQLDeviceTest); the generated text is pinned
			// by tests/goldens/devices/PGSQL/Query.Restrict.Comparison.sql, whose pre-fix form was SQL no PostgreSQL server
			// accepts.
			UseParametersForLiterals = true;
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
	/// PostgreSQL type : datetime
	/// D4 Type : DateTime	
	/// </summary>
	public class PostgreSQLDateTime : SQLScalarType
	{
		public const string CDateTimeFormat = "yyyy/MM/dd HH:mm:ss";

        public PostgreSQLDateTime(int AID, string AName) : base(AID, AName) { }
		
		private string FDateTimeFormat = CDateTimeFormat;
		public string DateTimeFormat
		{
			set { FDateTimeFormat = value; }
			get { return FDateTimeFormat;}
		}
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
			
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}
	   
	    public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}
		
		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
            return "timestamp";
		}     
	}

	/// <summary>
	/// PostgreSQL type : datetime
	/// D4 Type : Date
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
	public class PostgreSQLDate : SQLScalarType
	{
		public const string CDateFormat = "yyyy/MM/dd";

        public PostgreSQLDate(int AID, string AName) : base(AID, AName) { }

		private string FDateFormat = CDateFormat;
		public string DateFormat
		{
			set { FDateFormat = value; }
			get { return FDateFormat;}
		}
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
			
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(DateFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		/// <remarks>
		/// The current driver hands a PostgreSQL date back as a DateOnly, where the Npgsql 2.0.4 this was written against
		/// handed back a DateTime - so the straight cast this used to be threw "Unable to cast object of type
		/// 'System.DateOnly' to type 'System.DateTime'" the first time a Date column was read into the engine. D4's Date is
		/// a DateTime at midnight, which is what DateOnly carries plus the implied time. Both shapes are accepted because the
		/// driver's mapping is the driver's to change, and a device that reads only one of them breaks on the next change.
		/// Reproduction: the Date ordering cases of the device-coverage corpus (LivePGSQLDeviceTest).
		/// </remarks>
		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue is DateOnly dateOnly)
				return dateOnly.ToDateTime(TimeOnly.MinValue);
			return (DateTime)AValue;
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLDateType();
		}
		
		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "date";
		}
	}

	/// <summary>
	/// PostgreSQL type : datetime
	/// D4 Type : SQLTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
	public class PostgreSQLTime : SQLScalarType
	{
		public const string CTimeFormat = "HH:mm:ss";
		
		public PostgreSQLTime(int AID, string AName) : base(AID, AName) {}

		private string FTimeFormat = CTimeFormat;
		public string TimeFormat
		{
			set { FTimeFormat = value; }
			get { return FTimeFormat; }
		}
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
			
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		/// <remarks>
		/// The current driver carries converters for a PostgreSQL time only to TimeOnly and TimeSpan, where the Npgsql 2.0.4
		/// this was written against took and gave a DateTime - so both directions had to move. D4's Time is a DateTime on
		/// 0001-01-01 whose date part is ignored, which is what a TimeOnly is plus that implied date. The DateTime branch is
		/// kept, including its truncation to milliseconds, for a deployment that names a connection class of its own: it is
		/// what this operator did before, and nothing here can know what such a class hands back.
		/// Reproduction: the Time cases of the device-coverage corpus (LivePGSQLDeviceTest), which failed with "Writing
		/// values of 'System.DateTime' is not supported for parameters having NpgsqlDbType 'Time'".
		/// </remarks>
		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue is TimeOnly timeOnly)
				return new DateTime(1, 1, 1).Add(timeOnly.ToTimeSpan());
			// PostgreSQL's time is confined to one day, so adding the span to the base date cannot carry into another date -
			// except for the literal '24:00:00' the type admits, which lands on 0001-01-02 and is as close as this
			// representation comes to it.
			if (AValue is TimeSpan timeSpan)
				return new DateTime(1, 1, 1).Add(timeSpan);
			DateTime LDateTime = (DateTime)AValue;
			return new DateTime(1, 1, 1, LDateTime.Hour, LDateTime.Minute, LDateTime.Second, LDateTime.Millisecond);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			// The whole time of day rather than its hours, minutes, seconds and milliseconds: PostgreSQL's time holds
			// microseconds, so truncating here would make a value fail to compare equal to itself after a round trip.
			return TimeOnly.FromTimeSpan(((DateTime)AValue).TimeOfDay);
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLTimeType();
		}
		
		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "time";
		}
	}

	/// <summary>
	/// PostgreSQL type : uniqueidentifier
	/// D4 type : Guid
	/// TSQL comparison operators for the TSQL uniqueidentifier data type use string semantics, not hexadecimal
	/// </summary>
	public class PostgreSQLGuid : SQLScalarType
	{
		public PostgreSQLGuid(int AID, string AName) : base(AID, AName) { }

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
			return "uuid";
		}
	}

	/// <summary>
	/// PostgreSQL type : text
	/// D4 Type : SQLText, SQLIText
	/// </summary>
	public class PostgreSQLText : SQLText
	{
		public PostgreSQLText(int AID, string AName) : base(AID, AName) { }

		//public PostgreSQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public PostgreSQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "text";
		}
	}

	/// <summary>
	/// PostgreSQL type : image
	/// D4 type : Binary
	/// </summary>
	public class PostgreSQLBinary : SQLBinary
	{
		public PostgreSQLBinary(int AID, string AName) : base(AID, AName) { }

		//public PostgreSQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public PostgreSQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
            return "bytea";
		}
	}

	/// <summary>
	/// PostgreSQL type : image
	/// D4 type : Graphic
	/// </summary>
	public class PostgreSQLGraphic : SQLGraphic
	{
		public PostgreSQLGraphic(int AID, string AName) : base(AID, AName) { }

		//public PostgreSQLGraphic(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public PostgreSQLGraphic(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
            return "bytea";
		}
	}

	/// <summary>
	/// PostgreSQL type : binary(Storage.Length)
	/// D4 Type : MSSQLBinary
	/// </summary>
	public class PostgreSQLMSSQLBinary : SQLScalarType
	{
        public PostgreSQLMSSQLBinary(int AID, string AName) : base(AID, AName) { }

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

		/// <remarks>
		/// No length. PostgreSQL's bytea takes no type modifier at all - "create table t (x bytea(30))" is rejected with
		/// "type modifier is not allowed for type bytea" (measured 2026-11-26 against PostgreSQL 18.7) - so the length this
		/// used to emit made every create-table statement carrying an SQLDevice.MSSQLBinary column invalid, and the Storage.Length
		/// tag is now carried only by the SQLByteArrayType above, where it bounds the parameter rather than the column.
		/// Nothing in the repository reached it: the device-coverage corpus's MSSQLBinary cases are named for the SQL Server
		/// device's own scalar type and so are not applicable to this device, which is why a golden-only pass could not see it
		/// either. No deployment can have such a column, because creating one never succeeded.
		/// Reproduction: LivePGSQLDeviceTest.TheDeviceCreatesATableWithAByteArrayColumn.
		/// </remarks>
		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
            return "bytea";
		}
	}

	public class PostgreSQLString : SQLString
	{
		public PostgreSQLString(int id, string name) : base(id, name) { }

		protected override string InternalNativeDomainName(MetaData metaData)
		{
			return String.Format("character varying({0})", GetLength(metaData).ToString());
		}
	}
}