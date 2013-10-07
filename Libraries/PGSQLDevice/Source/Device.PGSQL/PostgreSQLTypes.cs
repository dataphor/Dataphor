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

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			DateTime LDateTime = (DateTime)AValue;
			return new DateTime(1, 1, 1, LDateTime.Hour, LDateTime.Minute, LDateTime.Second, LDateTime.Millisecond);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
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

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
            return String.Format("bytea({0})", GetLength(AMetaData));
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