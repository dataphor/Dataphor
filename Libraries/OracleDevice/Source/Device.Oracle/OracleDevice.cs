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

    #region Device

    public class OracleDevice : SQLDevice
    {
        private readonly string[] FOracleReservedWords =
            {
                "abort", "accept", "array", "arraylen", "assert", "assign", "at", "authorization", "avg",
                "base_table", "begin", "binary_integer", "body", "boolean", "case", "char_base", "close",
                "clusters", "colauth", "commit", "constraint", "crash", "currval", "cursor", "database",
                "data_base", "dba", "debugoff", "debugon", "decalare", "definition", "delay", "digits",
                "digits", "dispose", "do", "elsif", "end", "entry", "exception", "exception_init", "exit",
                "false", "fetch", "form", "function", "generic", "goto", "if", "indexes", "indicator",
                "interface", "limited", "loop", "max", "min", "minus", "mislabel", "mod", "natural",
                "number_base", "naturaln", "new", "nextval", "open", "others", "out", "package", "partition",
                "pls_integer", "positive", "positiven", "pragma", "private", "procedure", "raise", "range",
                "real", "record", "ref", "release", "remr", "return", "reverse", "rollback", "rownum",
                "rowtype", "run", "savepoint", "schema", "seperate", "space", "sql", "sqlcode", "sqlerrm",
                "statement", "stddev", "subtype", "sum", "tabauth", "tables", "task", "terminate", "true",
                "type", "use", "variance", "views", "when", "while", "work", "write", "xor"
            };

        protected string FHostName = String.Empty;
        protected bool FShouldEnsureOperators = true;

        public OracleDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID)
        {
            UseStatementTerminator = false;
            SupportsNestedCorrelation = false;
            IsOrderByInContext = false;
        }

        public string HostName
        {
            get { return FHostName; }
            set { FHostName = value == null ? String.Empty : value; }
        }

        /// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>
        /// <remarks>The value of this property is only valid if IsMSSQL70 is false.</remarks>
        public bool ShouldEnsureOperators
        {
            get { return FShouldEnsureOperators; }
            set { FShouldEnsureOperators = value; }
        }

        protected override void SetMaxIdentifierLength()
        {
            FMaxIdentifierLength = 30; // this is the max identifier length in Oracle all the time
        }

        protected override void InternalStarted(ServerProcess AProcess)
        {
            base.InternalStarted(AProcess);

            if (ShouldEnsureOperators)
                EnsureOperators(AProcess);
        }

        protected override void RegisterSystemObjectMaps(ServerProcess AProcess)
        {
            base.RegisterSystemObjectMaps(AProcess);

            // Perform system type and operator mapping registration
            var LResourceManager = new ResourceManager("SystemCatalog", GetType().Assembly);
#if USEISTRING
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
#else
            using (Stream LStream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.d4"))
            {
                RunScript(AProcess, String.Format(new StreamReader(LStream).ReadToEnd(), Name, "false"));
            }
            //RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, "false"));
#endif
        }

        protected void EnsureOperators(ServerProcess AProcess)
        {
            var LDeviceSession = (SQLDeviceSession) Connect(AProcess, AProcess.ServerSession.SessionInfo);
            try
            {
                EnsureOperatorDDL LDDL;
                for (int LIndex = 0; LIndex < OracleDDL.EnsureOperatorDDLCommands.Count; LIndex++)
                {
                    LDDL = OracleDDL.EnsureOperatorDDLCommands[LIndex] as EnsureOperatorDDL;
                    LDeviceSession.Connection.Execute(LDDL.CreateStatement);
                }
            }
            finally
            {
                Disconnect(LDeviceSession);
            }
        }

        protected override DeviceSession InternalConnect(ServerProcess AServerProcess,
                                                         DeviceSessionInfo ADeviceSessionInfo)
        {
            return new OracleDeviceSession(this, AServerProcess, ADeviceSessionInfo);
        }

        // Emitter
        protected override SQLTextEmitter InternalCreateEmitter()
        {
            return new OracleTextEmitter();
        }

        // HostName

        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(ServerProcess AProcess, string ATableName, string AColumnName,
                                                 string ADomainName)
        {
            switch (ADomainName.ToLower())
            {
                case "smallint":
                case "int":
                case "integer":
                case "number":
                case "bigint":
                case "decimal":
                case "numeric":
                case "float":
                case "date":
                case "money":
                case "char":
                case "varchar":
                case "varchar2":
                case "nchar":
                case "nvarchar":
                case "clob":
                case "blob":
                    return true;
                default:
                    return false;
            }
        }

        // FindScalarType
        public override ScalarType FindScalarType(ServerProcess AProcess, string ADomainName, int ALength,
                                                  MetaData AMetaData)
        {
            switch (ADomainName.ToLower())
            {
                case "smallint":
                    return AProcess.DataTypes.SystemShort;
                case "int":
                case "integer":
                case "number":
                    return AProcess.DataTypes.SystemInteger;
                case "bigint":
                    return AProcess.DataTypes.SystemLong;
                case "decimal":
                case "numeric":
                case "float":
                    return AProcess.DataTypes.SystemDecimal;
                case "date":
                    return AProcess.DataTypes.SystemDateTime;
                case "money":
                    return AProcess.DataTypes.SystemMoney;
                case "char":
                case "varchar":
                case "varchar2":
                case "nchar":
                case "nvarchar":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
#if USEISTRING
					return IsCaseSensitive ? AProcess.DataTypes.SystemString : AProcess.DataTypes.SystemIString;
#else
                    return AProcess.DataTypes.SystemString;
#endif
#if USEISTRING
				case "clob": return (ScalarType)(IsCaseSensitive ? AProcess.Plan.Catalog[CSQLTextScalarType] : AProcess.Plan.Catalog[CSQLITextScalarType]);
#else
                case "clob":
                    return (ScalarType) Compiler.ResolveCatalogIdentifier(AProcess.Plan, CSQLTextScalarType, true);
#endif
                case "blob":
                    return AProcess.DataTypes.SystemBinary;
                default:
                    throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
            }
        }

        protected override bool IsReservedWord(string AWord)
        {
            for (int i = 0; i < FOracleReservedWords.Length; i++)
            {
                if (AWord.ToUpper() == FOracleReservedWords[i].ToUpper())
                    return true;
            }
            return false;
        }

        public override TableSpecifier GetDummyTableSpecifier()
        {
            return new TableSpecifier(new TableExpression("DUAL"));
        }

        protected override string GetDeviceTablesExpression(TableVar ATableVar)
        {
            return
                String.Format
                    (
                    DeviceTablesExpression == String.Empty
                        ?
                            @"
							select
									c.owner as TableSchema, 
									c.table_name as TableName,
									c.column_name as ColumnName,
									c.column_id as OrdinalPosition,
									c.table_name as TableTitle,
									c.column_name as ColumnTitle,
									c.data_type as NativeDomainName,
									c.data_type as DomainName,
									NVL(c.char_col_decl_length, 0) as Length,
									case when c.nullable = 'N' then 0 else 1 end as IsNullable,
									case when c.data_type in ('BLOB', 'CLOB') then 1 else 0 end as IsDeferred
								from all_tab_columns c
								where 1 = 1 {0} {1}
								order by c.owner, c.table_name, c.column_id
						"
                        :
                            DeviceTablesExpression,
                    Schema == String.Empty ? String.Empty : String.Format("and c.owner = '{0}'", Schema),
                    ATableVar == null
                        ? String.Empty
                        : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
                    );
        }

        protected override string GetDeviceIndexesExpression(TableVar ATableVar)
        {
            return
                String.Format
                    (
                    DeviceIndexesExpression == String.Empty
                        ?
                            @"
							select
									i.table_owner as TableSchema,
									i.table_name as TableName,
									i.index_name as IndexName,
									c.column_name as ColumnName,
									c.column_position as OrdinalPosition,
									case when i.uniqueness = 'UNIQUE' then 1 else 0 end as IsUnique,
									case when c.descend = 'DESC' then 1 else 0 end as IsDescending
								from all_indexes i, all_ind_columns c
								where i.owner = c.index_owner
									and i.index_name = c.index_name
									and i.index_type = 'NORMAL'
									{0}
									{1}
								order by i.table_owner, i.table_name, i.index_name, c.column_position
						"
                        :
                            DeviceIndexesExpression,
                    Schema == String.Empty ? String.Empty : String.Format("and i.table_owner = '{0}'", Schema),
                    ATableVar == null
                        ? String.Empty
                        : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
                    );
        }

        protected override string GetDeviceForeignKeysExpression(TableVar ATableVar)
        {
            return
                String.Format
                    (
                    DeviceForeignKeysExpression == String.Empty
                        ?
                            @"
							select 
									c.owner as ConstraintSchema, 
									c.constraint_name as ConstraintName,
									c.owner as SourceTableSchema,
									c.table_name as SourceTableName, 
									cc.column_name as SourceColumnName,
									c.r_owner as TargetTableSchema,
									ic.table_name as TargetTableName,
									ic.column_name as TargetColumnName,
									cc.position as OrdinalPosition
								from all_constraints c, all_cons_columns cc, all_ind_columns ic
								where c.owner = cc.owner
									and c.constraint_name = cc.constraint_name
									and c.r_owner = ic.table_owner
									and c.r_constraint_name = ic.index_name
									and cc.position = ic.column_position
									and c.constraint_type = 'R'
									{0}
									{1}
								order by c.owner, c.constraint_name, cc.position
						"
                        :
                            DeviceForeignKeysExpression,
                    Schema == String.Empty ? String.Empty : String.Format("and c.owner = '{0}'", Schema),
                    ATableVar == null
                        ? String.Empty
                        : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
                    );
        }
    }

    public class OracleDeviceSession : SQLDeviceSession
    {
        public OracleDeviceSession(SQLDevice ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
            : base(ADevice, AServerProcess, ADeviceSessionInfo)
        {
        }

        public new OracleDevice Device
        {
            get { return (OracleDevice) base.Device; }
        }

        protected override SQLConnection InternalCreateConnection()
        {
            // ConnectionClass:
            //  ODBCConnection
            //  OLEDBConnection
            //  ADOConnection 
            //  OracleConnection (default)
            // ConnectionStringBuilderClass
            // OracleConnectionStringBuilder (default)
            // OracleODBCConnectionStringBuilder
            // OracleOLEDConnectionStringBuilder

            var LClassDefinition =
                new ClassDefinition
                    (
                    Device.ConnectionClass == String.Empty
                        ?
#if USEODP
						"OracleConnection.OracleConnection" :
#else
                    "MSOracleConnection.OracleConnection"
                        :
#endif
                    Device.ConnectionClass
                    );
            var LBuilderClass =
                new ClassDefinition
                    (
                    Device.ConnectionStringBuilderClass == String.Empty
                        ?
#if USEODP
						"OracleDevice.OracleConnectionStringBuilder" :
#else
                    "OracleDevice.OracleConnectionStringBuilder"
                        :
#endif
                    Device.ConnectionStringBuilderClass
                    );
            var LConnectionStringBuilder =
                (ConnectionStringBuilder)
                ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LBuilderClass, new object[] {});

            var LTags = new Tags();
            LTags.AddOrUpdate("HostName", Device.HostName);
            LTags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
            LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);

            LTags = LConnectionStringBuilder.Map(LTags);
            Device.GetConnectionParameters(LTags, DeviceSessionInfo);
            string LConnectionString = SQLDevice.TagsToString(LTags);

            return
                (SQLConnection)
                ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LClassDefinition, new object[] {LConnectionString});
        }
    }

    #endregion

    

    #region Types

    /// <summary>
    /// Oracle type : number(20, 0)
    /// D4 type : System.TimeSpan
    /// </summary>
    public class OracleTimeSpan : SQLScalarType
    {
        public OracleTimeSpan(int AID, string AName) : base(AID, AName) { }

        public override object ToScalar(IServerProcess AProcess, object AValue)
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

        public override object ToScalar(IServerProcess AProcess, object AValue)
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
        public OracleInteger(int AID, string AName) : base(AID, AName)
        {
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
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

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
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

        public override object ToScalar(IServerProcess AProcess, object AValue)
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

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
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

        public override object ToScalar(IServerProcess AProcess, object AValue)
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

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
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

        public override object ToScalar(IServerProcess AProcess, object AValue)
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

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            if ((AValue is string) && ((string) AValue == " "))
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

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            if ((AValue is string) && ((string) AValue == " "))
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

        public override Stream GetStreamAdapter(IServerProcess AProcess, Stream AStream)
        {
            using (var LReader = new StreamReader(AStream))
            {
                string LValue = LReader.ReadToEnd();
                if (LValue == " ")
                    LValue = String.Empty;
                Conveyor LConveyor = ScalarType.GetConveyor(AProcess);
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

    #endregion
}