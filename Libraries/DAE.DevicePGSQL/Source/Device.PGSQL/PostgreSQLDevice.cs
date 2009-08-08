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
    

    public class PostgreSQLDevice : SQLDevice
    {
        public const string CPostgreSQLBinaryScalarType = "PostgreSQLDevice.PostgreSQLBinary";

        public static string CEnsureDatabase =
			@"
if not exists (select * from pg_database where datname = '{0}')
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
                                        false.ToString().ToLower(), false.ToString().ToLower()));
#endif
            }
        }

        protected void EnsureDatabase(ServerProcess AProcess)
        {
            string LDatabaseName = DatabaseName;
            DatabaseName = "postgres";
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
			DatabaseName = "postgres";
            try
            {
                var LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
                try
                {
					SQLCursor LCursor = LDeviceSession.Connection.Open("SELECT version()");
                    try
                    {
                        string LVersion = String.Empty;
						LVersion  = ((string) LCursor[0]).Split(' ')[1];                    	                        

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
			if (ShouldEnsureOperators)
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
                            "INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') as IsDescending",
                            "and INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') is not null",
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


            var LClassDefinition =
                new ClassDefinition
                (
                    Device.ConnectionClass == String.Empty
                        ?"Connection.PostgreSQLConnection": Device.ConnectionClass);

            var LBuilderClass =
                new ClassDefinition
                (
                    Device.ConnectionStringBuilderClass == String.Empty
                        ?
 "PostgreSQLDevice.PostgreSQLADODotNetConnectionStringBuilder": Device.ConnectionStringBuilderClass
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

    
}
