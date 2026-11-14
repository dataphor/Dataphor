/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Device.SQL
{
	using System;
	using System.IO;
	using System.Globalization;

	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Streams;
	
    public abstract class SQLScalarType : DeviceScalarType
    {
		public SQLScalarType(int AID, string AName) : base(AID, AName) {}
		
		public virtual string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			object LValue = FromScalar(AManager, AValue);
			
			string LString = LValue as string;
			if (LString != null)
				return String.Format("cast('{0}' as {1})", LString.Replace("'", "''"), DomainName());

			return String.Format("cast('{0}' as {1})", LValue.ToString(), DomainName());
		}
		
		public virtual object ParameterToScalar(IValueManager AManager, object AValue)
		{
			return ToScalar(AManager, AValue);
		}
		
		public virtual object ParameterFromScalar(IValueManager AManager, object AValue)
		{
			return FromScalar(AManager, AValue);
		}
		
		public virtual Stream GetParameterStreamAdapter(IValueManager AManager, Stream AStream)
		{
			return GetStreamAdapter(AManager, AStream);
		}
		
		public abstract SQLType GetSQLType(D4.MetaData AMetaData);

		public SQLType GetSQLType()
		{
			return GetSQLType((D4.MetaData)null);
		}
		
		public SQLType GetSQLType(TableVarColumn AColumn)
		{
			return GetSQLType(AColumn.MetaData);
		}
		
		public virtual SQLType GetSQLParameterType(D4.MetaData AMetaData)
		{
			return GetSQLType(AMetaData);
		}
		
		public SQLType GetSQLParameterType()
		{
			return GetSQLParameterType((D4.MetaData)null);
		}
		
		public SQLType GetSQLParameterType(TableVarColumn AColumn)
		{
			return GetSQLParameterType(AColumn.MetaData);
		}
		
		public string GetTag(string ATagName, string ADefaultValue)
		{
			return GetTag(ATagName, ADefaultValue, null);
		}
		
		public string GetTag(string ATagName, string ADefaultValue, D4.MetaData AMetaData)
		{
			D4.Tag LTag = GetTag(ATagName, AMetaData);
			if (LTag != D4.Tag.None)
				return LTag.Value;
			return ADefaultValue;
		}
		
		public D4.Tag GetTag(string ATagName)
		{
			return GetTag(ATagName, (D4.MetaData)null);
		}
		
		public D4.Tag GetTag(string ATagName, D4.MetaData AMetaData)
		{
			D4.Tag LTag = D4.MetaData.GetTag(AMetaData, ATagName);
			if (LTag == D4.Tag.None)
			{	
				LTag = D4.MetaData.GetTag(MetaData, ATagName);
				if (LTag == D4.Tag.None)
					LTag = D4.MetaData.GetTag(ScalarType.MetaData, ATagName);
			}
			return LTag;
		}
		
		protected abstract string InternalNativeDomainName(D4.MetaData AMetaData);
				
		public string NativeDomainName(D4.MetaData AMetaData)
		{
			D4.Tag LNativeDomainName = GetTag("Storage.NativeDomainName", AMetaData);
			if (LNativeDomainName != D4.Tag.None)
				return LNativeDomainName.Value;
			return InternalNativeDomainName(AMetaData);
		}
		
		public string NativeDomainName()
		{
			return NativeDomainName((D4.MetaData)null);
		}
		
		public string NativeDomainName(TableVarColumn AColumn)
		{
			return NativeDomainName(AColumn.MetaData);
		}
		
		public string DomainName(D4.MetaData AMetaData)
		{
			D4.Tag LDomainName = D4.MetaData.GetTag(AMetaData, "Storage.DomainName");
			if (LDomainName != D4.Tag.None)
				return LDomainName.Value;
			LDomainName = GetTag("Storage.Name");
			if (LDomainName != D4.Tag.None)
				return LDomainName.Value;
			return NativeDomainName(AMetaData);
		}
		
		public string DomainName()
		{
			return DomainName((D4.MetaData)null);
		}
		
		public string DomainName(TableVarColumn AColumn)
		{
			return DomainName(AColumn.MetaData);
		}
		
		public virtual string ParameterDomainName(D4.MetaData AMetaData)
		{
			return DomainName(AMetaData);
		}
		
		public string ParameterDomainName()
		{
			return ParameterDomainName((D4.MetaData)null);
		}
		
		public string ParameterDomainName(TableVarColumn AColumn)
		{
			return ParameterDomainName(AColumn.MetaData);
		}
		
		private bool FUseParametersForLiterals = false;
		/// <summary>Determines whether or not literal sub-expressions of this type should be translated as literals, or using a parameter.</summary>
		public bool UseParametersForLiterals
		{
			get { return FUseParametersForLiterals; }
			set { FUseParametersForLiterals = value; }
		}
    }
	
	// DataTypes

    /// <summary>
    /// SQL type : integer
    /// 0 = false
    /// 1 = true
    /// </summary>
    public class SQLBoolean : SQLScalarType
    {
		public SQLBoolean(int AID, string AName) : base(AID, AName) {}
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", (bool)AValue ? "1" : "0", DomainName());
		}
		
		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue is bool)
				return AValue;
			return Convert.ToBoolean(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (bool)AValue ? 1 : 0;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(4);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "integer";
		}
    }

	/// <summary>
	/// SQL type : smallint
	/// 0-255 maps directly to a native byte
	/// </summary>
    public class SQLByte : SQLScalarType
    {
		public SQLByte(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToByte(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt16((byte)AValue);
		}
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", ((byte)AValue).ToString(NumberFormatInfo.InvariantInfo), DomainName());
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(2);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "smallint";
		}
    }

	#if UseUnsignedIntegers    
	/// <summary>
	/// SQL type : smallint
	/// D4 type : System.Byte
	/// </summary>
    public class SQLSByte : SQLScalarType
    {
		public SQLSByte() : base(){}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (sbyte)(short)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (short)(sbyte)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(2);
		}
		
		public override string DomainName(D4.MetaData AMetaData)
		{
			return "smallint";
		}
    }
    #endif
    
	/// <summary>
	/// SQL type : smallint
	/// D4 type : System.Short
	/// </summary>
    public class SQLShort : SQLScalarType
    {
		public SQLShort(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			//AS400 is returning AValue as Int32. Cannot cast as short so it is necessary to explicitly convert AValue to an Int16.
			return Convert.ToInt16(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (short)AValue;
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", ((short)AValue).ToString(NumberFormatInfo.InvariantInfo), DomainName());
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(2);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "smallint";
		}
    }
    
    #if UseUnsignedIntegers
	/// <summary>
	/// SQL type : integer
	/// D4 type : System.UShort
	/// </summary>
    public class SQLUShort : SQLScalarType
    {
		public SQLUShort() : base(){}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (ushort)((int)AValue));
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (int)(uint)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(4);
		}
		
		public override string DomainName(D4.MetaData AMetaData)
		{
			return "integer";
		}
    }
    #endif
    
	/// <summary>
	/// SQL type : integer
	/// D4 type : System.Integer
	/// </summary>
    public class SQLInteger : SQLScalarType
    {
		public SQLInteger(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt32(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (int)AValue;
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return ((int)AValue).ToString(NumberFormatInfo.InvariantInfo);
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(4);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "integer";
		}
    }
    
    #if UseUnsignedIntegers
	/// <summary>
	/// SQL type : bigint
	/// D4 type : System.UInteger
	/// </summary>
    public class SQLUInteger : SQLScalarType
    {
		public SQLUInteger() : base(){}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			// According to the documentation, as well as the type parameter of the ADO field, this
			// value should be being returned as a uint, however it is coming back as a decimal.
			if (AValue is long)
				return (uint)(long)AValue;
			else
				return Convert.ToUInt32((decimal)AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (long)(uint)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(8);
		}
		
		public override string DomainName(D4.MetaData AMetaData)
		{
			return "bigint";
		}
    }
    #endif

	/// <summary>
	/// SQL type : bigint
	/// D4 type : System.Long
	/// </summary>
    public class SQLLong : SQLScalarType
    {
		public SQLLong(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			// translation problem from ADO, documentation says this will be a long, but it is in fact a decimal
			return Convert.ToInt64(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (long)AValue;
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", ((long)AValue).ToString(NumberFormatInfo.InvariantInfo), DomainName());
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(8);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "bigint";
		}
    }
    
    #if UseUnsignedIntegers
	/// <summary>
	/// SQL type : decimal(20, 0)
	/// D4 type : System.ULong
	/// </summary>
    public class SQLULong : SQLScalarType
    {
		public SQLULong() : base(){}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToUInt64((decimal)AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToDecimal((ulong)AValue);
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}
		
		public override string DomainName(D4.MetaData AMetaData)
		{
			return "decimal(20, 0)";
		}
    }
    #endif
    
	/// <summary>
	/// SQL type : decimal(28, 8)
	/// D4 type : System.Decimal
	/// </summary>
    public class SQLDecimal : SQLScalarType
    {
		public SQLDecimal(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			// TODO: This should be a decimal cast but the ADOConnection is returning an integer value as the result of evaluating Avg(Integer) when the result is a whole number
			return Convert.ToDecimal(AValue); 
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (decimal)AValue;
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", ((decimal)AValue).ToString(NumberFormatInfo.InvariantInfo), DomainName());
		}
		
		public byte GetPrecision(D4.MetaData AMetaData)
		{
			return Byte.Parse(GetTag("Storage.Precision", "28", AMetaData));
		}
		
		public byte GetScale(D4.MetaData AMetaData)
		{
			return Byte.Parse(GetTag("Storage.Scale", "8", AMetaData));
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(GetPrecision(AMetaData), GetScale(AMetaData));
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return 
				String.Format
				(
					"decimal({0}, {1})", 
					GetPrecision(AMetaData).ToString(),
					GetScale(AMetaData).ToString()
				);
		}
    }

	/// <summary>
	/// SQL type : date
	/// D4 type : SQLDevice.SQLDateTime
	/// </summary>
    public class SQLDateTime : SQLScalarType
    {
		public const string CDateTimeFormat = "yyyy/MM/dd HH:mm:ss";

		public SQLDateTime(int AID, string AName) : base(AID, AName) {}
		
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
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date";
		}
    }

	/// <summary>
	/// SQL type : bigint
	/// D4 type : System.TimeSpan
	/// </summary>    
    public class SQLTimeSpan : SQLScalarType
    {
		public SQLTimeSpan(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			// ADO translation error: docs say this should be a long, in fact it is a decimal
			return new TimeSpan(Convert.ToInt64(AValue));
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((TimeSpan)AValue).Ticks;
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", ((TimeSpan)AValue).Ticks.ToString(NumberFormatInfo.InvariantInfo), DomainName());
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(8);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "bigint";
		}
    }
    
	/// <summary>
	/// SQL type : date
	/// D4 type : System.Date
	/// </summary>
    public class SQLDate : SQLScalarType
    {
		public const string CDateFormat = "yyyy/MM/dd";
		
		public SQLDate(int AID, string AName) : base(AID, AName) {}

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
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date";
		}
    }

	/// <summary>
	/// SQL type : date
	/// D4 type : SQLDevice.SQLTime
	/// </summary>
    public class SQLTime : SQLScalarType
    {
		public const string CTimeFormat = "yyyy/MM/dd HH:mm:ss";
		
		public SQLTime(int AID, string AName) : base(AID, AName) {}

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
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTimeType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date";
		}
    }

	/// <summary>
	/// SQL type : decimal(28, 8)
	/// D4 type : System.Money
	/// </summary>
    public class SQLMoney : SQLScalarType
    {
		public SQLMoney(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToDecimal(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (decimal)AValue;
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast({0} as {1})", ((decimal)AValue).ToString(NumberFormatInfo.InvariantInfo), DomainName());
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(28, 8);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "decimal(28, 8)";
		}
    }

	/// <summary>
	/// SQL type : char(36)
	/// D4 type : System.Guid
	/// </summary>
    public class SQLGuid : SQLScalarType
    {
		public SQLGuid(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return new Guid((string)AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((Guid)AValue).ToString();
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLStringType(36);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "char(36)";
		}
    }

	/// <summary>
	/// SQL type : varchar(Storage.Length)
	/// D4 type : System.String | System.IString
	/// </summary>
    public class SQLString : SQLScalarType
    {
		public SQLString(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (string)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (String)AValue;
		}
		
		protected int GetLength(D4.MetaData AMetaData)
		{
			return Int32.Parse(GetTag("Storage.Length", "20", AMetaData));
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLStringType(GetLength(AMetaData));
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return String.Format("varchar({0})", GetLength(AMetaData).ToString());
		}
    }

	public class SQLText : SQLScalarType
    {
		public SQLText(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (string)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (string)AValue;
		}
		
		public override Stream GetStreamAdapter(IValueManager AManager, Stream AStream)
		{
			using (StreamReader LReader = new StreamReader(AStream))
			{
				string LValue = LReader.ReadToEnd();
				Streams.IConveyor LConveyor = AManager.GetConveyor(ScalarType);
				MemoryStream LStream = new MemoryStream(LConveyor.GetSize(LValue));
				LStream.SetLength(LStream.GetBuffer().Length);
				LConveyor.Write(LValue, LStream.GetBuffer(), 0);
				return LStream;
			}
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTextType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "clob";
		}
    }
    
    /// <summary>
    /// SQL type : blob
    /// D4 type : System.Binary
    /// </summary>
    public class SQLBinary : SQLScalarType
    {
		public const int CStreamAllocationThreshold = 32767;
		
		public SQLBinary(int AID, string AName) : base(AID, AName) {}
		
		private byte[] GetNativeValue(IValueManager AManager, object AValue)
		{
			if (AValue is byte[])
				return (byte[])AValue;
				
			using (Scalar LScalar = new Scalar(AManager, ScalarType, (StreamID)AValue))
			{
				return LScalar.AsByteArray;
			}
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", Convert.ToBase64String(GetNativeValue(AManager, AValue)), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return null;
				
			byte[] LValue = (byte[])AValue;
			if (LValue.Length >= CStreamAllocationThreshold)
			{
				using (Scalar LScalar = new Scalar(AManager, this.ScalarType, AManager.StreamManager.Allocate()))
				{
					LScalar.AsByteArray = (byte[])AValue;
					return LScalar.StreamID;
				}
			}
			
			return LValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return GetNativeValue(AManager, AValue);
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLBinaryType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "blob";
		}
    }

    /// <summary>
    /// SQL type : blob
    /// D4 type : System.Graphic
    /// </summary>
    public class SQLGraphic : SQLScalarType
    {
		public const int CStreamAllocationThreshold = 32767;
		
		public SQLGraphic(int AID, string AName) : base(AID, AName) {}

		private byte[] GetNativeValue(IValueManager AManager, object AValue)
		{
			if (AValue is byte[])
				return (byte[])AValue;
				
			using (Scalar LScalar = new Scalar(AManager, ScalarType, (StreamID)AValue))
			{
				return LScalar.AsByteArray;
			}
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", Convert.ToBase64String(GetNativeValue(AManager, AValue)), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return null;
				
			byte[] LValue = (byte[])AValue;
			if (LValue.Length >= CStreamAllocationThreshold)
			{
				using (Scalar LScalar = new Scalar(AManager, this.ScalarType, AManager.StreamManager.Allocate()))
				{
					LScalar.AsByteArray = (byte[])AValue;
					return LScalar.StreamID;
				}
			}

			return LValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return GetNativeValue(AManager, AValue);
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLBinaryType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "blob";
		}
    }

	/// <summary>
	/// SQL type : char(40)
	/// D4 type : System.VersionNumber
	/// </summary>
    public class SQLVersionNumber : SQLScalarType
    {
		public SQLVersionNumber(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return StringToVersionNumber((string)AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return VersionNumberToString((VersionNumber)AValue);
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLStringType(40);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "char(40)";
		}

		public string VersionNumberToString(VersionNumber AValue)
		{
			return 
				String.Format
				(
					"{0}{1}{2}{3}", 
					AValue.Major == -1 ? "**********" : AValue.Major.ToString().PadLeft(10, '0'),
					AValue.Minor == -1 ? "**********" : AValue.Minor.ToString().PadLeft(10, '0'),
					AValue.Revision == -1 ? "**********" : AValue.Revision.ToString().PadLeft(10, '0'),
					AValue.Build == -1 ? "**********" : AValue.Build.ToString().PadLeft(10, '0')
				);
		}
		
		public VersionNumber StringToVersionNumber(string AValue)
		{
			return
				new VersionNumber
				(
					AValue[0] == '*' ? -1 : Int32.Parse(AValue.Substring(0, 10)),
					AValue[10] == '*' ? -1 : Int32.Parse(AValue.Substring(10, 10)),
					AValue[20] == '*' ? -1 : Int32.Parse(AValue.Substring(20, 10)),
					AValue[30] == '*' ? -1 : Int32.Parse(AValue.Substring(30, 10))
				);
		}
    }
}

