/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Resources;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.Oracle;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Device.Oracle
{

	/*
		Data Type Mapping ->
		
			DAE Type	|	Oracle Type														|	Translation Handler
			------------|---------------------------------------------------------------------------------------------
			Boolean		|	decimal(1, 0)													|	OracleBoolean
			Byte		|   decimal(3, 0)													|	OracleByte
			SByte		|	decimal(3, 0)													|	OracleSByte
			Short		|	decimal(5, 0)													|	OracleShort
			UShort		|	decimal(5, 0)													|	OracleUShort
			Integer		|	decimal(10, 0)													|	OracleInteger
			UInteger	|	decimal(10, 0)													|	OracleUInteger
			Long		|	decimal(20, 0)													|	OracleLong
			ULong		|	decimal(20, 0)													|	SQLULong
			Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SQLDecimal
			SQLDateTime	|	datetime														|	SQLDateTime
			TimeSpan	|	decimal(20, 0)													|	OracleTimeSpan
			Date		|	datetime														|	SQLDate
			SQLTime		|	datetime														|	SQLTime
			Money		|	decimal(28, 8)													|	SQLMoney
			Guid		|	char(24)														|	SQLGuid
			String		|	varchar2(Storage.Length)										|	OracleString
			//IString		|	varchar2(Storage.Length)										|	OracleString
			SQLText		|	clob															|	SQLText
			//SQLIText	|	clob															|	SQLText
			Binary		|	blob															|	SQLBinary
	*/

	/// <summary>
	/// Oracle type : number(20, 0)
	/// D4 type : System.TimeSpan
	/// </summary>
	public class OracleTimeSpan : SQLScalarType
	{
		public OracleTimeSpan(int AID, string AName) : base(AID, AName) { }

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return new TimeSpan(Convert.ToInt64(AValue));
		}

		public override object FromScalar(object AValue)
		{
			return ((TimeSpan)AValue).Ticks;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "number(20, 0)";
		}
	}

	public class OracleBoolean : SQLScalarType
	{
		public OracleBoolean(int AID, string AName) : base(AID, AName) { }

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToBoolean(AValue);
		}

		public override object FromScalar(object AValue)
		{
			return ((bool)AValue ? 1.0 : 0.0);
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLNumericType(1, 0);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "number(1, 0)";
		}
	}

	public class OracleInteger : SQLScalarType
	{
		public OracleInteger(int AID, string AName)
			: base(AID, AName)
		{
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt32(AValue);
		}

		public override object FromScalar(object AValue)
		{
			return (decimal)(int)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLNumericType(10, 0);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "number(10, 0)";
		}
	}

#if UseUnsignedIntegers
	public class OracleUInteger : SQLScalarType
	{
		public OracleUInteger() : base(){}

		public override Scalar ToScalar(IValueManager AManager, object AValue)
		{
			return Scalar.FromUInt32(AProcess, Convert.ToUInt32((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.ToUInt32();
		}

		public override SQLType GetSQLType(ScalarType AScalarType, D4.MetaData AMetaData)
		{
			return new SQLNumericType(10, 0);
		}

		public override string DomainName(TableVarColumn AColumn)
		{
			return "number(10, 0)";
		}
	}
#endif

	public class OracleShort : SQLScalarType
	{
		public OracleShort(int AID, string AName) : base(AID, AName) { }

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt16((decimal)AValue);
		}

		public override object FromScalar(object AValue)
		{
			return (decimal)(short)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLNumericType(5, 0);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "number(5, 0)";
		}
	}

#if UseUnsignedIntegers
	public class OracleUShort : SQLScalarType
	{
		public OracleUShort() : base(){}

		public override Scalar ToScalar(IValueManager AManager, object AValue)
		{
			return Scalar.FromUInt16(AProcess, Convert.ToUInt16((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.ToUInt16();
		}

		public override SQLType GetSQLType(ScalarType AScalarType, D4.MetaData AMetaData)
		{
			return new SQLNumericType(5, 0);
		}

		public override string DomainName(TableVarColumn AColumn)
		{
			return "number(5, 0)";
		}
	}
#endif

	public class OracleByte : SQLScalarType
	{
		public OracleByte(int AID, string AName) : base(AID, AName) { }

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToByte((decimal)AValue);
		}

		public override object FromScalar(object AValue)
		{
			return (decimal)(byte)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLNumericType(3, 0);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "number(3, 0)";
		}
	}

#if UseUnsignedIntegers
	public class OracleSByte : SQLScalarType
	{
		public OracleSByte() : base(){}

		public override Scalar ToScalar(IValueManager AManager, object AValue)
		{
			return Scalar.FromSByte(AProcess, Convert.ToSByte((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.ToSByte();
		}

		public override SQLType GetSQLType(ScalarType AScalarType, D4.MetaData AMetaData)
		{
			return new SQLNumericType(3, 0);
		}

		public override string DomainName(TableVarColumn AColumn)
		{
			return "number(3, 0)";
		}
	}
#endif

	public class OracleLong : SQLScalarType
	{
		public OracleLong(int AID, string AName) : base(AID, AName) { }

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt64((decimal)AValue);
		}

		public override object FromScalar(object AValue)
		{
			return (decimal)(long)AValue;
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "number(20, 0)";
		}
	}

	public class OracleString : SQLString
	{
		public OracleString(int AID, string AName) : base(AID, AName) { }

		/*
			Oracle cannot distinguish between an empty string and a null once the empty string has been inserted into a table.
			To get around this problem, we translate all empty strings to blank strings of length 1.
		*/

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if ((AValue is string) && ((string)AValue == " "))
				return "";
			else
				return AValue;
		}

		public override object FromScalar(object AValue)
		{
			string LValue = (string)AValue;
			if (LValue == String.Empty)
				return " ";
			else
				return LValue;
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return String.Format("varchar2({0})", GetLength(AMetaData));
		}
	}

	public class OracleSQLText : SQLScalarType
	{
		public OracleSQLText(int AID, string AName) : base(AID, AName) { }

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			if ((AValue is string) && ((string)AValue == " "))
				return "";
			else
				return AValue;
		}

		public override object FromScalar(object AValue)
		{
			string LValue = (string)AValue;
			if (LValue == String.Empty)
				return " ";
			else
				return LValue;
		}

		public override Stream GetStreamAdapter(IValueManager AManager, Stream AStream)
		{
			using (var LReader = new StreamReader(AStream))
			{
				string LValue = LReader.ReadToEnd();
				if (LValue == " ")
					LValue = String.Empty;
				Conveyor LConveyor = AManager.GetConveyor(ScalarType);
				var LStream = new MemoryStream(LConveyor.GetSize(LValue));
				LStream.SetLength(LStream.GetBuffer().Length);
				LConveyor.Write(LValue, LStream.GetBuffer(), 0);
				return LStream;
			}
		}

		public override SQLType GetSQLType(MetaData AMetaData)
		{
			return new SQLTextType();
		}

		protected override string InternalNativeDomainName(MetaData AMetaData)
		{
			return "clob";
		}
	}
}