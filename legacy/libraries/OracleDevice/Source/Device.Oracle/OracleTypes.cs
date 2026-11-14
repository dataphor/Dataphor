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
		public OracleTimeSpan(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return new TimeSpan(Convert.ToInt64(tempValue));
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return ((TimeSpan)tempValue).Ticks;
		}

		public override SQLType GetSQLType(MetaData metaData)
		{
			return new SQLNumericType(20, 0);
		}

		protected override string InternalNativeDomainName(MetaData metaData)
		{
			return "number(20, 0)";
		}
	}

	public class OracleBoolean : SQLScalarType
	{
		public OracleBoolean(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return Convert.ToBoolean(tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return ((bool)tempValue ? 1.0 : 0.0);
		}

		public override SQLType GetSQLType(MetaData metaData)
		{
			return new SQLNumericType(1, 0);
		}

		protected override string InternalNativeDomainName(MetaData metaData)
		{
			return "number(1, 0)";
		}
	}

	public abstract class OracleWholeNumberType : SQLScalarType
	{
		public OracleWholeNumberType(int iD, string name)
			: base(iD, name)
		{
		}

		public abstract byte GetPrecision(MetaData metaData);

		public override SQLType GetSQLType(MetaData metaData)
		{
			return new SQLNumericType(GetPrecision(metaData), 0);
		}

		protected override string InternalNativeDomainName(MetaData metaData)
		{
			return String.Format("number({0}, 0)", GetPrecision(metaData));
		}
	}

	public class OracleInteger : OracleWholeNumberType
	{
		public OracleInteger(int iD, string name)
			: base(iD, name)
		{
		}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return Convert.ToInt32(tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return (decimal)(int)tempValue;
		}

		public override byte GetPrecision(MetaData metaData)
		{
			return Byte.Parse(GetTag("Storage.Precision", "10", metaData));
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

	public class OracleShort : OracleWholeNumberType
	{
		public OracleShort(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return Convert.ToInt16((decimal)tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return (decimal)(short)tempValue;
		}

		public override byte GetPrecision(MetaData metaData)
		{
			return Byte.Parse(GetTag("Storage.Precision", "5", metaData));
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

	public class OracleByte : OracleWholeNumberType
	{
		public OracleByte(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return Convert.ToByte((decimal)tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return (decimal)(byte)tempValue;
		}

		public override byte GetPrecision(MetaData metaData)
		{
			return Byte.Parse(GetTag("Storage.Precision", "3", metaData));
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

	public class OracleLong : OracleWholeNumberType
	{
		public OracleLong(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return Convert.ToInt64((decimal)tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return (decimal)(long)tempValue;
		}

		public override byte GetPrecision(MetaData metaData)
		{
			return Byte.Parse(GetTag("Storage.Precision", "20", metaData));
		}
	}

	public class OracleString : SQLString
	{
		public OracleString(int iD, string name) : base(iD, name) { }

		/*
			Oracle cannot distinguish between an empty string and a null once the empty string has been inserted into a table.
			To get around this problem, we translate all empty strings to blank strings of length 1.
		*/

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			if ((tempValue is string) && ((string)tempValue == " "))
				return "";
			else
				return tempValue;
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			string localTempValue = (string)tempValue;
			if (localTempValue == String.Empty)
				return " ";
			else
				return localTempValue;
		}

		protected override string InternalNativeDomainName(MetaData metaData)
		{
			return String.Format("varchar2({0})", GetLength(metaData));
		}
	}

	public class OracleSQLText : SQLScalarType
	{
		public OracleSQLText(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			if ((tempValue is string) && ((string)tempValue == " "))
				return "";
			else
				return tempValue;
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			string localTempValue = (string)tempValue;
			if (localTempValue == String.Empty)
				return " ";
			else
				return localTempValue;
		}

		public override Stream GetStreamAdapter(IValueManager manager, Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				string tempValue = reader.ReadToEnd();
				if (tempValue == " ")
					tempValue = String.Empty;
				IConveyor conveyor = manager.GetConveyor(ScalarType);
				var localStream = new MemoryStream(conveyor.GetSize(tempValue));
				localStream.SetLength(localStream.GetBuffer().Length);
				conveyor.Write(tempValue, localStream.GetBuffer(), 0);
				return localStream;
			}
		}

		public override SQLType GetSQLType(MetaData metaData)
		{
			return new SQLTextType();
		}

		protected override string InternalNativeDomainName(MetaData metaData)
		{
			return "clob";
		}
	}
}