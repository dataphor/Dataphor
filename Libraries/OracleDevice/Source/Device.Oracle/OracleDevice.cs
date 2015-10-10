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
using Alphora.Dataphor.DAE.Compiling;

namespace Alphora.Dataphor.DAE.Device.Oracle
{
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

        protected string _hostName = String.Empty;
        protected bool _shouldEnsureOperators = true;

        public OracleDevice(int iD, string name) : base(iD, name)
        {
            UseStatementTerminator = false;
            SupportsNestedCorrelation = false;
            SupportsOrderByNullsFirstLast = true;
            IsOrderByInContext = false;
        }

        public string HostName
        {
            get { return _hostName; }
            set { _hostName = value == null ? String.Empty : value; }
        }

        /// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>
        /// <remarks>The value of this property is only valid if IsMSSQL70 is false.</remarks>
        public bool ShouldEnsureOperators
        {
            get { return _shouldEnsureOperators; }
            set { _shouldEnsureOperators = value; }
        }

        protected override void SetMaxIdentifierLength()
        {
            _maxIdentifierLength = 30; // this is the max identifier length in Oracle all the time
        }

        protected override void InternalStarted(ServerProcess process)
        {
            base.InternalStarted(process);

            if (ShouldEnsureOperators)
                EnsureOperators(process);
        }

        protected override void RegisterSystemObjectMaps(ServerProcess process)
        {
            base.RegisterSystemObjectMaps(process);

            // Perform system type and operator mapping registration
            var resourceManager = new ResourceManager("SystemCatalog", GetType().Assembly);
#if USEISTRING
			RunScript(AProcess, String.Format(resourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
#else
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.d4"))
            {
                RunScript(process, String.Format(new StreamReader(stream).ReadToEnd(), Name, "false"));
            }
            //RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, "false"));
#endif
        }

        protected void EnsureOperators(ServerProcess process)
        {
            // no access
            var deviceSession = (SQLDeviceSession)Connect(process, process.ServerSession.SessionInfo);
            try
            {
                SQLConnection connection = deviceSession.Connection;
                if (!connection.InTransaction)
                    connection.BeginTransaction(SQLIsolationLevel.Serializable);
                try
                {
                    using (SQLCommand command = connection.CreateCommand(false))
                    {
                        using (Stream stream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.sql"))
                        {
                            foreach (
                                string batch in SQLUtility.ProcessBatches(new StreamReader(stream).ReadToEnd(), @"\"))
                            {
                                command.Statement = batch;
                                command.Execute();
                            }
                        }
                    }
                    connection.CommitTransaction();
                }
                catch
                {
                    connection.RollbackTransaction();
                    throw;
                }
            }
            finally
            {
                Disconnect(deviceSession);
            }

        }

        protected override DeviceSession InternalConnect(ServerProcess serverProcess,
                                                         DeviceSessionInfo deviceSessionInfo)
        {
            return new OracleDeviceSession(this, serverProcess, deviceSessionInfo);
        }

        // Emitter
        protected override SQLTextEmitter InternalCreateEmitter()
        {
            return new OracleTextEmitter();
        }

        // HostName

        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(Plan plan, string tableName, string columnName,
                                                 string domainName)
        {
            switch (domainName.ToLower())
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
        public override ScalarType FindScalarType(Plan plan, string domainName, int length, MetaData metaData)
        {
            switch (domainName.ToLower())
            {
                case "smallint":
                    return plan.DataTypes.SystemShort;
                case "int":
                case "integer":
                case "number":
                    return plan.DataTypes.SystemInteger;
                case "bigint":
                    return plan.DataTypes.SystemLong;
                case "decimal":
                case "numeric":
                case "float":
                    return plan.DataTypes.SystemDecimal;
                case "date":
                    return plan.DataTypes.SystemDateTime;
                case "money":
                    return plan.DataTypes.SystemMoney;
                case "char":
                case "varchar":
                case "varchar2":
                case "nchar":
                case "nvarchar":
                    metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
#if USEISTRING
					return IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString;
#else
                    return plan.DataTypes.SystemString;
#endif
#if USEISTRING
				case "clob": return (ScalarType)(IsCaseSensitive ? APlan.Catalog[CSQLTextScalarType] : APlan.Catalog[CSQLITextScalarType]);
#else
                case "clob":
                    return (ScalarType)Compiler.ResolveCatalogIdentifier(plan, SQLTextScalarType, true);
#endif
                case "blob":
                    return plan.DataTypes.SystemBinary;
                default:
                    throw new SQLException(SQLException.Codes.UnsupportedImportType, domainName);
            }
        }

        protected override bool IsReservedWord(string word)
        {
            for (int i = 0; i < FOracleReservedWords.Length; i++)
            {
                if (word.ToUpper() == FOracleReservedWords[i].ToUpper())
                    return true;
            }
            return false;
        }

        public override TableSpecifier GetDummyTableSpecifier()
        {
            return new TableSpecifier(new TableExpression("DUAL"));
        }

        protected override string GetDeviceTablesExpression(TableVar tableVar)
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
									join all_tables t on c.owner = t.owner and c.table_name = t.table_name
								where 1 = 1 {0} {1}
								order by c.owner, c.table_name, c.column_id
						"
                        :
                            DeviceTablesExpression,
                    Schema == String.Empty ? String.Empty : String.Format("and c.owner = '{0}'", Schema),
                    tableVar == null
                        ? String.Empty
                        : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(tableVar).ToUpper())
                    );
        }

        protected override string GetDeviceIndexesExpression(TableVar tableVar)
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
                    tableVar == null
                        ? String.Empty
                        : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(tableVar).ToUpper())
                    );
        }

        protected override string GetDeviceForeignKeysExpression(TableVar tableVar)
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
                    tableVar == null
                        ? String.Empty
                        : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(tableVar).ToUpper())
                    );
        }
    }

    public class OracleDeviceSession : SQLDeviceSession
    {
        public OracleDeviceSession(SQLDevice device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
            : base(device, serverProcess, deviceSessionInfo)
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

            var classDefinition =
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
            var builderClass =
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
            var connectionStringBuilder =
                (ConnectionStringBuilder)
                ServerProcess.CreateObject(builderClass, new object[] {});

            var tags = new Tags();
            tags.AddOrUpdate("HostName", Device.HostName);
            tags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
            tags.AddOrUpdate("Password", DeviceSessionInfo.Password);

            tags = connectionStringBuilder.Map(tags);
            Device.GetConnectionParameters(tags, DeviceSessionInfo);
            string connectionString = SQLDevice.TagsToString(tags);

            return
                (SQLConnection)
                ServerProcess.CreateObject(classDefinition, new object[] {connectionString});
        }
    }
}
