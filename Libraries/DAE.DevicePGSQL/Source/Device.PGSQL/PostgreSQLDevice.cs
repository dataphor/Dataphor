using System;
using System.Globalization;
using System.IO;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Language.PGSQL;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using ColumnExpression = Alphora.Dataphor.DAE.Language.SQL.ColumnExpression;
using DropIndexStatement = Alphora.Dataphor.DAE.Language.PGSQL.DropIndexStatement;
using SelectStatement = Alphora.Dataphor.DAE.Language.SQL.SelectStatement;

namespace Alphora.Dataphor.Device.PGSQL
{
    #region Device

    public class PostgreSQLDevice : SQLDevice
    {
        public const string CPostgreSQLBinaryScalarType = "PostgreSQLDevice.PostgreSQLBinary";

        public static string CEnsureDatabase =
            @"
if not exists (select * from sysdatabases where name = '{0}')
	create database {0}
			";

        protected string FApplicationName = "Dataphor Server";
        protected string FDatabaseName = String.Empty;

        private int FMajorVersion = 8; // default to SQL Server 2000 (pending detection)
        protected string FServerName = String.Empty;
        protected bool FShouldDetermineVersion = true;
        protected bool FShouldEnsureDatabase = true;
        protected bool FShouldEnsureOperators = true;
        protected bool FShouldReconcileRowGUIDCol;
        protected bool FUseIntegratedSecurity;

        public PostgreSQLDevice(int AID, string AName, int AResourceManagerID)
            : base(AID, AName, AResourceManagerID)
        {
            IsOrderByInContext = false;
            // T-SQL allows items in the order by list to reference the column aliases used in the query
            UseParametersForCursors = true;
            ShouldNormalizeWhitespace = false;
        }

        public int MajorVersion
        {
            get { return FMajorVersion; }
            set { FMajorVersion = value; }
        }

        public bool IsPostgreSQL70
        {
            set
            {
                if (value)
                    FMajorVersion = 7;
            }
            get { return FMajorVersion == 7; }
        }

        public bool IsAccess { set; get; }

        /// <value>Indicates whether the device should auto-determine the version of the target system.</value>
        public bool ShouldDetermineVersion
        {
            get { return FShouldDetermineVersion; }
            set { FShouldDetermineVersion = value; }
        }

        /// <value>Indicates whether the device should create the database if it does not already exist.</value>
        public bool ShouldEnsureDatabase
        {
            // maybe check and throw to see if it is access because can't create (at least I don't think so) an access database;
            get { return FShouldEnsureDatabase; }
            set { FShouldEnsureDatabase = value; }
        }

        /// <value>Indicates whether the device should reconcile columns that are marked as ROWGUIDCOL in SQLServer.</value>
        public bool ShouldReconcileRowGUIDCol
        {
            get { return FShouldReconcileRowGUIDCol; }
            set { FShouldReconcileRowGUIDCol = value; }
        }

        /// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>
        /// <remarks>The value of this property is only valid if IsPostgreSQL70 is false.</remarks>
        public bool ShouldEnsureOperators
        {
            get { return FShouldEnsureOperators; }
            set { FShouldEnsureOperators = value; }
        }

        public string ServerName
        {
            get { return FServerName; }
            set { FServerName = value == null ? String.Empty : value; }
        }

        public string DatabaseName
        {
            get { return FDatabaseName; }
            set { FDatabaseName = value == null ? String.Empty : value; }
        }

        public string ApplicationName
        {
            get { return FApplicationName; }
            set { FApplicationName = value == null ? "Dataphor Server" : value; }
        }

        public bool UseIntegratedSecurity
        {
            get { return FUseIntegratedSecurity; }
            set { FUseIntegratedSecurity = value; }
        }

        protected override void SetMaxIdentifierLength()
        {
            FMaxIdentifierLength = 128;
        }

        protected override void InternalStarted(ServerProcess AProcess)
        {
            string LOnExecuteConnectStatement = OnExecuteConnectStatement;
            OnExecuteConnectStatement = null;
            try
            {
                if (ShouldDetermineVersion)
                    DetermineVersion(AProcess);

                base.InternalStarted(AProcess);

                InitializeDatabase(AProcess);
            }
            finally
            {
                OnExecuteConnectStatement = LOnExecuteConnectStatement;
            }
        }

        protected override void RegisterSystemObjectMaps(ServerProcess AProcess)
        {
            base.RegisterSystemObjectMaps(AProcess);

            // Perform system type and operator mapping registration
            using (Stream LStream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.d4"))
            {
#if USEISTRING
				RunScript(AProcess, String.Format(new StreamReader(LStream).ReadToEnd(), Name, IsCaseSensitive.ToString().ToLower(), IsPostgreSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
#else
                RunScript(AProcess,
                          String.Format(new StreamReader(LStream).ReadToEnd(), Name, "false",
                                        IsPostgreSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
#endif
            }
        }

        protected void EnsureDatabase(ServerProcess AProcess)
        {
            string LDatabaseName = DatabaseName;
            DatabaseName = "master";
            try
            {
                var LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
                try
                {
                    LDeviceSession.Connection.Execute(String.Format(CEnsureDatabase, LDatabaseName));
                }
                finally
                {
                    Disconnect(LDeviceSession);
                }
            }
            finally
            {
                DatabaseName = LDatabaseName;
            }
        }

        protected void DetermineVersion(ServerProcess AProcess)
        {
            string LDatabaseName = DatabaseName;
            DatabaseName = "master";
            try
            {
                var LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
                try
                {
                    SQLCursor LCursor = LDeviceSession.Connection.Open("exec xp_msver");
                    try
                    {
                        string LVersion = String.Empty;
                        while (LCursor.Next())
                            if (Convert.ToString(LCursor[1]) == "ProductVersion")
                            {
                                LVersion = Convert.ToString(LCursor[3]);
                                break;
                            }

                        if (LVersion.Length > 0)
                            FMajorVersion = Convert.ToInt32(LVersion.Substring(0, LVersion.IndexOf('.')));
                    }
                    finally
                    {
                        LCursor.Command.Connection.Close(LCursor);
                    }
                }
                finally
                {
                    Disconnect(LDeviceSession);
                }
            }
            finally
            {
                DatabaseName = LDatabaseName;
            }
        }

        protected void InitializeDatabase(ServerProcess AProcess)
        {
            // access checks should go here maybe?

            // Create the database, if necessary
            if (ShouldEnsureDatabase)
                EnsureDatabase(AProcess);

            // Run the initialization script, if specified
            if ((!IsPostgreSQL70) && ShouldEnsureOperators)
                EnsureOperators(AProcess);
        }

        protected void EnsureOperators(ServerProcess AProcess)
        {
            // no access
            var LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
            try
            {
                SQLConnection LConnection = LDeviceSession.Connection;
                if (!LConnection.InTransaction)
                    LConnection.BeginTransaction(SQLIsolationLevel.Serializable);
                try
                {
                    using (SQLCommand LCommand = LConnection.CreateCommand(false))
                    {
                        using (Stream LStream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.sql"))
                        {
                            foreach (
                                string LBatch in SQLUtility.ProcessBatches(new StreamReader(LStream).ReadToEnd(), "go"))
                            {
                                LCommand.Statement = LBatch;
                                LCommand.Execute();
                            }
                        }
                    }
                    LConnection.CommitTransaction();
                }
                catch
                {
                    LConnection.RollbackTransaction();
                    throw;
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
            return new PostgreSQLDeviceSession(this, AServerProcess, ADeviceSessionInfo);
        }

        protected override Statement TranslateDropIndex(TableVar ATableVar, Key AKey)
        {
            var LStatement = new DropIndexStatement();
            LStatement.TableSchema = MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
            LStatement.TableName = ToSQLIdentifier(ATableVar);
            LStatement.IndexSchema = MetaData.GetTag(AKey.MetaData, "Storage.Schema", String.Empty);
            LStatement.IndexName = GetIndexName(LStatement.TableName, AKey);
            return LStatement;
        }

        protected override Statement TranslateDropIndex(TableVar ATableVar, Order AOrder)
        {
            var LStatement = new DropIndexStatement();
            LStatement.TableSchema = MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
            LStatement.TableName = ToSQLIdentifier(ATableVar);
            LStatement.IndexSchema = MetaData.GetTag(AOrder.MetaData, "Storage.Schema", String.Empty);
            LStatement.IndexName = GetIndexName(LStatement.TableName, AOrder);
            return LStatement;
        }

        public override TableSpecifier GetDummyTableSpecifier()
        {
            var LSelectExpression = new SelectExpression();
            LSelectExpression.SelectClause = new SelectClause();
            LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
            return new TableSpecifier(LSelectExpression, "dummy1");
        }

        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(ServerProcess AProcess, string ATableName, string AColumnName,
                                                 string ADomainName)
        {
            switch (ADomainName.ToLower())
            {
                case "bit":
                case "tinyint":
                case "smallint":
                case "int":
                case "integer":
                case "bigint":
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                case "datetime":
                case "smalldatetime":
                case "money":
                case "smallmoney":
                case "uniqueidentifier":
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                case "text":
                case "ntext":
                case "image":
                case "binary":
                case "varbinary":
                case "timestamp":
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
                case "bit":
                    return AProcess.DataTypes.SystemBoolean;
                case "tinyint":
                    return AProcess.DataTypes.SystemByte;
                case "smallint":
                    return AProcess.DataTypes.SystemShort;
                case "int":
                case "integer":
                    return AProcess.DataTypes.SystemInteger;
                case "bigint":
                    return AProcess.DataTypes.SystemLong;
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                    return AProcess.DataTypes.SystemDecimal;
                case "datetime":
                case "smalldatetime":
                    return AProcess.DataTypes.SystemDateTime;
                case "money":
                case "smallmoney":
                    return AProcess.DataTypes.SystemMoney;
                case "uniqueidentifier":
                    return AProcess.DataTypes.SystemGuid;
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
#if USEISTRING
					return IsCaseSensitive ? AProcess.DataTypes.SystemString : AProcess.DataTypes.SystemIString;
#else
                    return AProcess.DataTypes.SystemString;
#endif
#if USEISTRING
				case "text":
				case "ntext": return (ScalarType)(IsCaseSensitive ? AProcess.ServerSession.Server.Catalog[CSQLTextScalarType] : AProcess.ServerSession.Server.Catalog[CSQLITextScalarType]);
#else
                case "text":
                case "ntext":
                    return (ScalarType)Compiler.ResolveCatalogIdentifier(AProcess.Plan, CSQLTextScalarType, true);
#endif
                case "binary":
                case "timestamp":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
                    return (ScalarType)Compiler.ResolveCatalogIdentifier(AProcess.Plan, CPostgreSQLBinaryScalarType, true);
                case "varbinary":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
                    return AProcess.DataTypes.SystemBinary;
                case "image":
                    return AProcess.DataTypes.SystemBinary;
                default:
                    throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
            }
        }

        // Emitter
        protected override SQLTextEmitter InternalCreateEmitter()
        {
            return new TSQLTextEmitter();
        }

        protected override string GetDeviceTablesExpression(TableVar ATableVar)
        {
            return
                DeviceTablesExpression != String.Empty
                    ?
                        base.GetDeviceTablesExpression(ATableVar)
                    :
                        String.Format
                            (
                            (
                                !IsAccess
                                    ?
                                        @"
									select 
											su.name as TableSchema,
											so.name as TableName, 
											sc.name as ColumnName, 
											sc.colorder as OrdinalPosition,								
											so.name as TableTitle, 
											sc.name as ColumnTitle, 
											snt.name as NativeDomainName, 
											st.name as DomainName,
											convert(integer, sc.length) as Length,
											sc.isnullable as IsNullable,
											case when snt.name in ('text', 'ntext', 'image') then 1 else 0 end as IsDeferred
										from sysobjects as so
											join sysusers as su on so.uid = su.uid
											join syscolumns as sc on sc.id = so.id 
											join systypes as st on st.xusertype = sc.xusertype
											join systypes as snt on st.xtype = snt.xusertype
										where (so.xtype = 'U' or so.xtype = 'V')
											and OBJECTPROPERTY(so.id, 'IsMSShipped') = 0
											{0}
											{1}
										order by so.name, sc.colid
								"
                                    :
                                        @"
									select
											Name as TableSchema,
											Name as TableName,
											uhh column name

								
								"
                            ),
                            ATableVar == null
                                ? String.Empty
                                : String.Format("and so.name = '{0}'", ToSQLIdentifier(ATableVar)),
                            FShouldReconcileRowGUIDCol
                                ? String.Empty
                                : "and COLUMNPROPERTY(so.id, sc.name, 'IsRowGUIDCol') = 0"
                            );
        }

        protected override string GetDeviceIndexesExpression(TableVar ATableVar)
        {
            return
                DeviceIndexesExpression != String.Empty
                    ?
                        base.GetDeviceIndexesExpression(ATableVar)
                    :
                        String.Format
                            (
                            @"
							select 
									su.name as TableSchema,
									so.name as TableName, 
									si.name as IndexName, 
									sc.name as ColumnName, 
									sik.keyno as OrdinalPosition,
									INDEXPROPERTY(so.id, si.name, 'IsUnique') as IsUnique,
									{0} /* if ServerVersion >= 8.0, otherwise 0 as IsDescending */
								from sysobjects as so
									join sysusers as su on so.uid = su.uid
									join sysindexes as si on si.id = so.id
									left join sysobjects as sno on sno.name = si.name
									left join sysconstraints as sn on sn.constid = sno.id
									join sysindexkeys as sik on sik.id = so.id and sik.indid = si.indid
									join syscolumns as sc on sc.id = so.id and sc.colid = sik.colid
								where (so.xtype = 'U' or so.xtype = 'V')
									and OBJECTPROPERTY(so.id, 'isMSShipped') = 0
									and INDEXPROPERTY(so.id, si.name, 'IsStatistics') = 0
									{1} /* if ServerVersion >= 8.0, otherwise empty string */
									{2}
								order by so.name, si.indid, sik.keyno
						",
                            IsPostgreSQL70
                                ? "0 as IsDescending"
                                : "INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') as IsDescending",
                            IsPostgreSQL70
                                ? String.Empty
                                : "and INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') is not null",
                            ATableVar == null
                                ? String.Empty
                                : String.Format("and so.name = '{0}'", ToSQLIdentifier(ATableVar))
                            );
        }

        protected override string GetDeviceForeignKeysExpression(TableVar ATableVar)
        {
            return
                DeviceForeignKeysExpression != String.Empty
                    ?
                        base.GetDeviceForeignKeysExpression(ATableVar)
                    :
                        String.Format
                            (
                            @"
							select 
									su.name as ConstraintSchema,
									so.name as ConstraintName,
									ssu.name as SourceTableSchema,
									sso.name as SourceTableName,
									ssc.name as SourceColumnName,
									tsu.name as TargetTableSchema,
									tso.name as TargetTableName,
									tsc.name as TargetColumnName,
									keyno OrdinalPosition
								from sysforeignkeys as sfk
									join sysobjects as so on sfk.constid = so.id
									join sysusers as su on su.uid = so.uid
									join sysobjects as sso on sfk.fkeyid = sso.id
									join sysusers as ssu on ssu.uid = sso.uid
									join syscolumns as ssc on ssc.colid = sfk.fkey and ssc.id = sfk.fkeyid
									join sysobjects as tso on sfk.rkeyid = tso.id
									join sysusers as tsu on tsu.uid = tso.uid
									join syscolumns as tsc on tsc.colid = sfk.rkey and tsc.id = sfk.rkeyid
								where 1 = 1
									{0}
								order by ConstraintSchema, ConstraintName, OrdinalPosition
						",
                            ATableVar == null ? String.Empty : "and so.name = '" + ToSQLIdentifier(ATableVar) + "'"
                            );
        }

        public override void DetermineCursorBehavior(Plan APlan, TableNode ATableNode)
        {
            base.DetermineCursorBehavior(APlan, ATableNode);
            // TODO: This will actually only be static if the ADOConnection is used because the DotNet providers do not support a method for obtaining a static cursor.
        }

        // ServerName		

        public override SelectStatement TranslateOrder(DevicePlan ADevicePlan, TableNode ANode,
                                                       SelectStatement AStatement)
        {
            if (AStatement.Modifiers == null)
                AStatement.Modifiers = new LanguageModifiers();
            AStatement.Modifiers.Add(new LanguageModifier("OptimizerHints", "option (fast 1)"));
            return base.TranslateOrder(ADevicePlan, ANode, AStatement);
        }
    }

    public class PostgreSQLDeviceSession : SQLDeviceSession
    {
        public PostgreSQLDeviceSession(PostgreSQLDevice ADevice, ServerProcess AServerProcess,
                                  DeviceSessionInfo ADeviceSessionInfo)
            : base(ADevice, AServerProcess, ADeviceSessionInfo)
        {
        }

        public new PostgreSQLDevice Device
        {
            get { return (PostgreSQLDevice)base.Device; }
        }

        protected override SQLConnection InternalCreateConnection()
        {
            // ConnectionClass:
            //  ODBCConnection
            //  OLEDBConnection (default)
            //  ADOConnection 
            //  PostgreSQLConnection
            // ConnectionStringBuilderClass
            // PostgreSQLOLEDBConnectionStringBuilder (default) (use with ADOConnection and OLEDBConnection)
            // PostgreSQLADODotNetConnectionStringBuilder (use with PostgreSQLConnection)
            // PostgreSQLODBCConnectionStringBuilder (use with ODBCConnection)

            /*
				Connectivity Implementations with PostgreSQL:
					ADOConnection ->
						When we use ADO, it is fully functional, but thread locks when we attempt to use it concurrently.
						If we switch providers to MSDASQL, it doesn't work either because the Command and Recordset objects
						are not being released.  The connection complains that too many recordsets are open, even though no
						recordsets are open.  Marshal.ReleaseComObject doesn't help either.  I suspect that the thread locking
						problem we are seeing when we use ADO is related to this same issue.
						
					PostgreSQLConnection ->
						When we use the native managed provider (SqlClient), performance drops by a factor of 2.
						There are also issues with binary, varbinary, and image data types.
						
					OLEDBConnection ->
						The OLEDB managed provider seems to resolve the thread locking issues, but it has yet to be probed for
						full functionality.  As of right now, this if the only supported connectivity implementation for the 
						PostgreSQLDevice.  This may change.
						
					There are also several mono providers which we can try if we run out of options in the Microsoft space.
					
				BTR 12/20/2004 ->
					Changed the default to use the native managed provider on the assumption that this is faster because less
					layers are involved. This assumption did not hold up under transaction processing tests prior to service pack 1,
					but we are hoping that service pack 1 has improved performance of this provider. We still need to run tp tests
					with the provider.
					
				BTR 12/20/2004 ->
					Later that same day...
					When using the SqlClient on Jeff's machine (not sure whether it was sp1 or not, need to check) we were getting
					non-deterministic behavior (The connection would close unexpectedly after an open and cause Dataphor to consider
					the connection a transaction failure). This behavior does not occur with the OleDbConnection so we switched back
					to it. More later...
					
				BTR 12/21/2004 ->
					SqlClient using .NET 1.1 with sp1 is confirmed, the behavior is unpredictable, same codebase with the OleDbConnection
					works fine, so we are switching to that provider until further notice.
			*/

            var LClassDefinition =
                new ClassDefinition
                (
                    Device.ConnectionClass == String.Empty
                        ?
#if USEADOCONNECTION
						"ADOConnection.ADOConnection" 
#else
#if USEOLEDBCONNECTION
						"Connection.OLEDBConnection"
#else
 "Connection.PostgreSQLConnection"
#endif
#endif
 : Device.ConnectionClass
                );

            var LBuilderClass =
                new ClassDefinition
                (
                    Device.ConnectionStringBuilderClass == String.Empty
                        ?
#if USEADOCONNECTION
						"PostgreSQLDevice.PostgreSQLOLEDBConnectionStringBuilder"
#else
#if USEOLEDBCONNECTION
						"PostgreSQLDevice.PostgreSQLOLEDBConnectionStringBuilder"
#else
 "PostgreSQLDevice.PostgreSQLADODotNetConnectionStringBuilder"
#endif
#endif
 : Device.ConnectionStringBuilderClass
                    );

            var LConnectionStringBuilder =
                (ConnectionStringBuilder)ServerProcess.Plan.Catalog.ClassLoader.CreateObject
                (
                    LBuilderClass,
                    new object[] { }
                );

            var LTags = new Tags();
            LTags.AddOrUpdate("ServerName", Device.ServerName);
            LTags.AddOrUpdate("DatabaseName", Device.DatabaseName);
            LTags.AddOrUpdate("ApplicationName", Device.ApplicationName);
            if (Device.UseIntegratedSecurity)
                LTags.AddOrUpdate("IntegratedSecurity", "true");
            else
            {
                LTags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
                LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);
            }

            LTags = LConnectionStringBuilder.Map(LTags);
            Device.GetConnectionParameters(LTags, DeviceSessionInfo);
            string LConnectionString = SQLDevice.TagsToString(LTags);
            return
                (SQLConnection)ServerProcess.Plan.Catalog.ClassLoader.CreateObject
                (
                    LClassDefinition,
                    new object[] { LConnectionString }
                );
        }
    }

    #endregion

    #region Types

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

        public override string ToLiteral(object AValue)
        {
            if (AValue == null)
                return String.Format("cast(null as {0})", DomainName());

            return (bool)AValue ? "1" : "0";
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            if (AValue is bool)
                return (bool)AValue;
            else
                return (int)AValue == 0 ? false : true;
        }

        public override object FromScalar(object AValue)
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
    /// PostgreSQL type : tinyint
    /// D4 Type : Byte
    /// </summary>
    public class PostgreSQLByte : SQLScalarType
    {
        public PostgreSQLByte(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            // According to the docs for the SQLOLEDB provider this is supposed to come back as a byte, but
            // it is coming back as a short, I don't know why, maybe interop?
            return Convert.ToByte(AValue);
        }

        public override object FromScalar(object AValue)
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
    /// PostgreSQL type : money
    /// D4 Type : Money
    /// </summary>
    public class PostgreSQLMoney : SQLScalarType
    {
        public PostgreSQLMoney(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            return Convert.ToDecimal(AValue);
        }

        public override object FromScalar(object AValue)
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
    /// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
    /// The ADO connectivity layer seems to be rounding datetime values to the nearest second, even though the server is capable of storing greater precision
    /// </summary>
    public class PostgreSQLDateTime : SQLScalarType
    {
        public const string CDateTimeFormat = "yyyy/MM/dd HH:mm:ss";

        public static readonly DateTime Accuracy = new DateTime((long)(TimeSpan.TicksPerMillisecond * 3.33));
        public static readonly DateTime MinValue = new DateTime(1753, 1, 1);

        private string FDateTimeFormat = CDateTimeFormat;

        public PostgreSQLDateTime(int AID, string AName) : base(AID, AName) { }

        public string DateTimeFormat
        {
            get { return FDateTimeFormat; }
            set { FDateTimeFormat = value; }
        }

        public override string ToLiteral(object AValue)
        {
            if (AValue == null)
                return String.Format("cast(null as {0})", DomainName());

            DateTime LValue = (DateTime)AValue;
            if (LValue == DateTime.MinValue)
                LValue = MinValue;
            if (LValue < MinValue)
                throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

            return String.Format("'{0}'", LValue.ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo));
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            var LDateTime = (DateTime)AValue;
            // If the value is equal to the device's zero date, set it to Dataphor's zero date
            if (LDateTime == MinValue)
                LDateTime = DateTime.MinValue;
            long LTicks = LDateTime.Ticks;
            return new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond));
        }

        public override object FromScalar(object AValue)
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
    /// PostgreSQL type : datetime
    /// D4 Type : Date
    /// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
    /// </summary>
    public class PostgreSQLDate : SQLScalarType
    {
        public const string CDateFormat = "yyyy/MM/dd";

        private string FDateFormat = CDateFormat;

        public PostgreSQLDate(int AID, string AName) : base(AID, AName) { }

        public string DateFormat
        {
            get { return FDateFormat; }
            set { FDateFormat = value; }
        }

        public override string ToLiteral(object AValue)
        {
            if (AValue == null)
                return String.Format("cast(null as {0})", DomainName());

            DateTime LValue = (DateTime)AValue;
            // If the value is equal to Dataphor's zero date (Jan, 1, 0001), set it to the device's zero date
            if (LValue == DateTime.MinValue)
                LValue = PostgreSQLDateTime.MinValue;
            if (LValue < PostgreSQLDateTime.MinValue)
                throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

            return String.Format("'{0}'", LValue.ToString(DateFormat, DateTimeFormatInfo.InvariantInfo));
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            var LDateTime = (DateTime)AValue;
            // If the value is equal to the Device's zero date, set it to Dataphor's zero date
            if (LDateTime == PostgreSQLDateTime.MinValue)
                LDateTime = DateTime.MinValue;
            long LTicks = LDateTime.Ticks;
            return new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay));
        }

        public override object FromScalar(object AValue)
        {
            DateTime LValue = (DateTime)AValue;
            // If the value is equal to Dataphor's zero date (Jan, 1, 0001), set it to the device's zero date
            if (LValue == DateTime.MinValue)
                LValue = PostgreSQLDateTime.MinValue;
            if (LValue < PostgreSQLDateTime.MinValue)
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
    /// PostgreSQL type : datetime
    /// D4 Type : SQLTime
    /// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
    /// </summary>
    public class PostgreSQLTime : SQLScalarType
    {
        public const string CTimeFormat = "HH:mm:ss";

        private string FTimeFormat = CTimeFormat;

        public PostgreSQLTime(int AID, string AName) : base(AID, AName) { }

        public string TimeFormat
        {
            get { return FTimeFormat; }
            set { FTimeFormat = value; }
        }

        public override string ToLiteral(object AValue)
        {
            if (AValue == null)
                return String.Format("cast(null as {0})", DomainName());

            // Added 1899 years, so that a time can actually be stored. 
            // Adding 1899 years puts it at the year 1900
            // which is stored as zero in PostgreSQL.
            // this year value of 1900 may make some translation easier.
            DateTime LValue = ((DateTime)AValue).AddYears(1899);
            if (LValue < PostgreSQLDateTime.MinValue)
                throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

            return String.Format("'{0}'", LValue.ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo));
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            return new DateTime(((DateTime)AValue).Ticks % TimeSpan.TicksPerDay);
        }

        public override object FromScalar(object AValue)
        {
            // Added 1899 years, so that a time can actually be stored. 
            // Adding 1899 years puts it at the year 1900
            // which is stored as zero in PostgreSQL.
            // this year value of 1900 may make some translation easier.
            DateTime LValue = ((DateTime)AValue).AddYears(1899);
            if (LValue < PostgreSQLDateTime.MinValue)
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
    /// PostgreSQL type : uniqueidentifier
    /// D4 type : Guid
    /// TSQL comparison operators for the TSQL uniqueidentifier data type use string semantics, not hexadecimal
    /// </summary>
    public class PostgreSQLGuid : SQLScalarType
    {
        public PostgreSQLGuid(int AID, string AName) : base(AID, AName) { }

        public override string ToLiteral(object AValue)
        {
            if (AValue == null)
                return String.Format("cast(null as {0})", DomainName());

            return String.Format("'{0}'", (Guid)AValue);
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            if (AValue is string)
                return new Guid((string)AValue);
            else
                return (Guid)AValue;
        }

        public override object FromScalar(object AValue)
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
            return "image";
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
            return "image";
        }
    }

    /// <summary>
    /// PostgreSQL type : binary(Storage.Length)
    /// D4 Type : PostgreSQLBinary
    /// </summary>
    public class PostgreSQLPostgreSQLBinary : SQLScalarType
    {
        public PostgreSQLPostgreSQLBinary(int AID, string AName) : base(AID, AName) { }

        public override string ToLiteral(object AValue)
        {
            if (AValue == null)
                return String.Format("cast(null as {0})", DomainName());

            return String.Format("'{0}'", Convert.ToBase64String((byte[])AValue));
        }

        public override object ToScalar(IServerProcess AProcess, object AValue)
        {
            return (byte[])AValue;
        }

        public override object FromScalar(object AValue)
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

    #endregion

}
