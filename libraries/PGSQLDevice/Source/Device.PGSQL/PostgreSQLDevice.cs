using System;
using System.IO;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Language.PGSQL;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using ColumnExpression = Alphora.Dataphor.DAE.Language.SQL.ColumnExpression;
using DropIndexStatement = Alphora.Dataphor.DAE.Language.PGSQL.DropIndexStatement;
using SelectStatement = Alphora.Dataphor.DAE.Language.SQL.SelectStatement;

namespace Alphora.Dataphor.DAE.Device.PGSQL
{
    
    public class PostgreSQLDevice : SQLDevice
    {
        public const string PostgreSQLBinaryScalarType = "SQLDevice.PostgreSQLBinary";

        public static string CEnsureDatabase =
			@"select count(*) from pg_database where datname = '{0}'";
        public static string CCreateDatabase =
            @"create database ""{0}""";
        
        protected string _database = "postgres"; // default to stock 'postgres' database
        protected string _port = "5432";

        // TODO: The major version for Postgres needs to reflect its first
        // 2 integers; eg, 9.4 is a major version with its own file format,
        // while 9 is simply a marketing umbrella not suitable for our purposes.
        private int _majorVersion = 9; // default to Postgresql 9 (pending detection)
        protected string _server = "127.0.0.1"; // default to localhost
        protected bool _shouldDetermineVersion = false;
        protected bool _shouldEnsureDatabase = true;
        protected bool _shouldEnsureOperators = true;
        protected bool _shouldReconcileRowGUIDCol;        
        protected string _searchPath;
        

        public PostgreSQLDevice(int iD, string name)
            : base(iD, name)
        {
            SupportsOrderByNullsFirstLast = true;
            IsOrderByInContext = false;
            // T-SQL allows items in the order by list to reference the column aliases used in the query
            UseParametersForCursors = true;
            ShouldNormalizeWhitespace = false;
        }

        public int MajorVersion
        {
            get { return _majorVersion; }
            set { _majorVersion = value; }
        }       

        /// <value>Indicates whether the device should auto-determine the version of the target system.</value>
        public bool ShouldDetermineVersion
        {
            get { return _shouldDetermineVersion; }
            set { _shouldDetermineVersion = value; }
        }

        /// <value>Indicates whether the device should create the database if it does not already exist.</value>
        public bool ShouldEnsureDatabase
        {
            // maybe check and throw to see if it is access because can't create (at least I don't think so) an access database;
            get { return _shouldEnsureDatabase; }
            set { _shouldEnsureDatabase = value; }
        }

       

        /// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>       
        public bool ShouldEnsureOperators
        {
            get { return _shouldEnsureOperators; }
            set { _shouldEnsureOperators = value; }
        }

        public string Server
        {
            get { return _server; }
            set { _server = value ?? String.Empty; }
        }

        public string Database
        {
            get { return _database; }
            set { _database = value ?? String.Empty; }
        }
        
        public string SearchPath
        {
            get { return _searchPath; }
            set { _searchPath = value; }
        }

        public string Port
        {
            get { return _port; }
            set { _port = value; }
        }


        protected override void SetMaxIdentifierLength()
        {
            _maxIdentifierLength = 63; // Anything above 63 would be truncated
        }

        protected override void InternalStarted(ServerProcess process)
        {
            string onExecuteConnectStatement = OnExecuteConnectStatement;
            OnExecuteConnectStatement = null;
            try
            {
                if (ShouldDetermineVersion)
                    DetermineVersion(process);

                base.InternalStarted(process);

                InitializeDatabase(process);
            }
            finally
            {
                OnExecuteConnectStatement = onExecuteConnectStatement;
            }
        }

        protected override void RegisterSystemObjectMaps(ServerProcess process)
        {
            base.RegisterSystemObjectMaps(process);

            // Perform system type and operator mapping registration
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.d4"))
            {
                if (stream != null)
                {
                    var script = new StreamReader(stream).ReadToEnd();
                    RunScript
					(
						process,
                        String.Format(script, Name, "false", false.ToString().ToLower(), false.ToString().ToLower())
					);
                }
            }
        }

		/// <summary>
		/// Runs <paramref name="action"/> over a device session pointed at the stock <c>postgres</c> database, which every
		/// PostgreSQL installation has, and hands it the name of the device's own database.
		/// </summary>
		/// <remarks>
		/// Both callers need the server without going through the device's own database: EnsureDatabase because it is about to
		/// create it, and DetermineVersion because the start sequence runs it first and the database may therefore not be there
		/// yet - asked of a database that does not exist, either fails with PostgreSQL 3D000 and takes the device's start with
		/// it. The Database property is what the connection string is built from, so the swap has to enclose the session as
		/// well as the statement; it used to enclose only the statement here, which worked only because a device session opens
		/// its connection lazily. The SQL Server device does the same thing with master, in the same two places.
		/// </remarks>
		protected void ExecuteAgainstStockDatabase(ServerProcess process, Action<SQLDeviceSession, string> action)
		{
			var database = Database;
			Database = "postgres";
			try
			{
				var deviceSession = (SQLDeviceSession)Connect(process, process.ServerSession.SessionInfo);
				try
				{
					action(deviceSession, database);
				}
				finally
				{
					Disconnect(deviceSession);
				}
			}
			finally
			{
				Database = database;
			}
		}

		protected void EnsureDatabase(ServerProcess process)
		{
			ExecuteAgainstStockDatabase
			(
				process,
				(deviceSession, database) =>
				{
					// Detect if the database exists
					bool exists;
					SQLCursor cursor = deviceSession.Connection.Open(String.Format(CEnsureDatabase, database));
					try
					{
						cursor.Next();
						exists = (long)cursor[0] > 0;
					}
					finally
					{
						cursor.Command.Connection.Close(cursor);
					}

					// If not, attempt to create it
					if (!exists)
						deviceSession.Connection.Execute(String.Format(CCreateDatabase, database));
				}
			);
		}

		/// <remarks>
		/// Against the stock postgres database rather than the device's own; see ExecuteAgainstStockDatabase for why. Nothing
		/// could see that this asked the wrong database while ShouldDetermineVersion defaulted off and nothing in the
		/// repository connected; LivePGSQLDeviceTest.TheDeviceReadsTheServerVersion turns it on and is the reproduction.
		/// </remarks>
		protected void DetermineVersion(ServerProcess process)
		{
			ExecuteAgainstStockDatabase
			(
				process,
				(deviceSession, database) =>
				{
					// The device's own database name is not needed here; only the connection had to avoid it.
					SQLCursor cursor = deviceSession.Connection.Open("SELECT version()");
					try
					{
						// Guarded, as the SQL Server device's guards it: version() answering nothing, or answering something
						// without a space in it, would otherwise come back as an index out of range from inside a device start.
						if (cursor.Next())
						{
							var rawVersion = Convert.ToString(cursor[0]);
							var words = rawVersion.Split(' ');
							var version = words.Length > 1 ? words[1] : String.Empty;

							if (version.IndexOf('.') > 0)
								_majorVersion = Convert.ToInt32(version.Substring(0, version.IndexOf('.')));
						}
					}
					finally
					{
						cursor.Command.Connection.Close(cursor);
					}
				}
			);
		}

        protected void InitializeDatabase(ServerProcess process)
        {
            // access checks should go here maybe?

            // Create the database, if necessary
            if (ShouldEnsureDatabase)
                EnsureDatabase(process);

            // Run the initialization script, if specified
			if (ShouldEnsureOperators)
                EnsureOperators(process);
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
                            if (stream != null)
                            {
                                var systemCatalog = new StreamReader(stream).ReadToEnd();
                                if (systemCatalog.Length > 0)
                                {
                                    var batches = SQLUtility.ProcessBatches(systemCatalog, @"\");
                                    foreach (string batch in batches)
                                    {
                                        command.Statement = batch;
                                        command.Execute();
                                    }
                                }
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
            return new PostgreSQLDeviceSession(this, serverProcess, deviceSessionInfo);
        }

        protected override Statement TranslateDropIndex(SQLDevicePlan plan, TableVar tableVar, Key key)
        {
            var statement = new DropIndexStatement();
            statement.TableSchema = MetaData.GetTag(tableVar.MetaData, "Storage.Schema", Schema);
            statement.TableName = ToSQLIdentifier(tableVar);
            statement.IndexSchema = MetaData.GetTag(key.MetaData, "Storage.Schema", String.Empty);
            statement.IndexName = GetIndexName(statement.TableName, key);
            return statement;
        }

        protected override Statement TranslateDropIndex(SQLDevicePlan plan, TableVar tableVar, Order order)
        {
            var statement = new DropIndexStatement();
            statement.TableSchema = MetaData.GetTag(tableVar.MetaData, "Storage.Schema", Schema);
            statement.TableName = ToSQLIdentifier(tableVar);
            statement.IndexSchema = MetaData.GetTag(order.MetaData, "Storage.Schema", String.Empty);
            statement.IndexName = GetIndexName(statement.TableName, order);
            return statement;
        }

        public override TableSpecifier GetDummyTableSpecifier()
        {
			var selectExpression = new Language.PGSQL.SelectExpression();
			selectExpression.SelectClause = new SelectClause();
			selectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
			return new TableSpecifier(selectExpression, "dummy1");
        }

        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(Plan plan, string tableName, string columnName, string domainName)
        {
            switch (domainName.ToLower())
            {
                case "boolean":                
                case "smallint":
                case "int":
                case "integer":
                case "bigint":
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                case "datetime":
				case "date":
                case "smalldatetime":
                case "money":
                case "smallmoney":
                case "uniqueidentifier":
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
				case "character varying":
                case "text":
                case "ntext":
                case "image":
                case "binary":
                case "varbinary":
                case "timestamp":
				case "timestamp without time zone":
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
                case "boolean":
                    return plan.DataTypes.SystemBoolean;                
                case "smallint":
                    return plan.DataTypes.SystemShort;
                case "int":
                case "integer":
                    return plan.DataTypes.SystemInteger;
                case "bigint":
                    return plan.DataTypes.SystemLong;
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                    return plan.DataTypes.SystemDecimal;
				case "date":
					return plan.DataTypes.SystemDate;
				// TODO: Support time type
				//case "time":
				//    return plan.DataTypes.SystemTime;
				case "datetime":
                case "smalldatetime":
				case "timestamp without time zone":
                    return plan.DataTypes.SystemDateTime;
                case "money":
                case "smallmoney":
                    return plan.DataTypes.SystemMoney;
                case "uniqueidentifier":
                    return plan.DataTypes.SystemGuid;
                case "char":
                case "varchar":
                case "nchar":
				case "character varying":
                case "nvarchar":
                    metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
                    return plan.DataTypes.SystemString;
                case "text":
                case "ntext":
                    return (ScalarType)Compiler.ResolveCatalogIdentifier(plan, SQLTextScalarType, true);
                case "binary":
                case "timestamp":
                    metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
                    return (ScalarType)Compiler.ResolveCatalogIdentifier(plan, PostgreSQLBinaryScalarType, true);
                case "varbinary":
                    metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
                    return plan.DataTypes.SystemBinary;
                case "image":
                    return plan.DataTypes.SystemBinary;
                default:
                    throw new SQLException(SQLException.Codes.UnsupportedImportType, domainName);
            }
        }

        // Emitter
        protected override SQLTextEmitter InternalCreateEmitter()
        {
            return new TSQLTextEmitter();
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
									table_schema as TableSchema,
									table_name as TableName,
									column_name as ColumnName,
									ordinal_position as OrdinalPosition,
									table_name as TableTitle,
									column_name as ColumnTitle,
									data_type as NativeDomainName,
									data_type as DomainName,
									case 
										when character_maximum_length is not null then character_maximum_length
										when numeric_precision is not null then numeric_precision
										when datetime_precision is not null then datetime_precision
										else 0
									end as Length,
									case when is_nullable = 'NO' then 0 else 1 end as IsNullable,
									case when data_type in ('text', 'bytea') then 1 else 0 end as IsDeferred
								from information_schema.columns
								where table_schema not in ('information_schema','pg_catalog','pg_toast')
									{0} {1}
								order by table_schema, table_name, ordinal_position
							"
						:
							DeviceTablesExpression,
					Schema == String.Empty ? String.Empty : String.Format("and table_schema = '{0}'", Schema),
					tableVar == null
						? String.Empty
						: String.Format("and table_name = '{0}'", ToSQLIdentifier(tableVar).ToLower())
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
										tn.nspname as TableSchema,
										tc.relname as TableName,
										ic.relname as IndexName,
										ca.attname as ColumnName,
										ca.attnum as OrdinalPosition,
										i.indisunique as IsUnique,
										case when i.indoption[0]=3  then TRUE else FALSE end as IsDescending
									from pg_index i 
										join pg_class tc on tc.oid = i.indrelid
										join pg_class ic on ic.oid = i.indexrelid
										join pg_attribute ca on ca.attrelid = ic.oid
										join pg_namespace tn on tn.oid = tc.relnamespace
									where tn.nspname not in ('information_schema','pg_catalog','pg_toast')
									{0}
									{1}
									order by tn.nspname, tc.relname, ic.relname, ca.attnum
							"
						:
							DeviceIndexesExpression,
					Schema == String.Empty ? String.Empty : String.Format("and tn.nspname = '{0}'", Schema),
					tableVar == null
						? String.Empty
						: String.Format("and tc.relname = '{0}'", ToSQLIdentifier(tableVar).ToLower())
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
									tc.table_schema as ConstraintSchema,
									tc.constraint_name as ConstraintName, 
									tc.table_name as SourceTableName, 
									kcu.column_name as SourceColumnName, 
									ccu.table_schema as TargetTableSchema,
									ccu.table_name as TargetTableName,
									ccu.column_name as TargetColumnName,
									c.ordinal_position as OrdinalPosition
								from information_schema.table_constraints tc 
									join information_schema.key_column_usage kcu on tc.constraint_name = kcu.constraint_name
									join information_schema.constraint_column_usage ccu on ccu.constraint_name = tc.constraint_name
									join information_schema.columns c on
										c.table_schema = ccu.table_schema
										and ccu.table_name = c.table_name
										and c.column_name = ccu.column_name
								where constraint_type = 'FOREIGN KEY'
									and c.table_schema not in ('information_schema','pg_catalog','pg_toast')
									{0}
									{1}
								order by tc.table_schema, tc.constraint_name, c.ordinal_position
							"
						:
							DeviceForeignKeysExpression,
					Schema == String.Empty ? String.Empty : String.Format("and tc.table_schema = '{0}'", Schema),
					tableVar == null
						? String.Empty
						: String.Format("and tc.table_name = '{0}'", ToSQLIdentifier(tableVar).ToLower())
				);
        }

        // NOTE: "option (fast 1)" is a T-SQL hint, carried over from MSSQLDevice, and PostgreSQL has no such syntax. It
        // reaches no statement: only the T-SQL emitter writes a statement's OptimizerHints modifier, and this device's
        // emitter (Language.PGSQL.TSQLTextEmitter, which does not derive from it) does not - measured 2026-11-25, the
        // ordered query goldens under tests/goldens/devices/PGSQL carry no hint. So the modifier is set and dropped. If
        // this emitter ever learns to write modifiers, this line starts generating SQL no PostgreSQL server accepts;
        // remove the override then rather than adding a PostgreSQL hint, which the device has never had.
        public override SelectStatement TranslateOrder(DevicePlan devicePlan, TableNode node, SelectStatement statement, bool inContextOrderBy)
        {
            if (statement.Modifiers == null)
                statement.Modifiers = new LanguageModifiers();
            statement.Modifiers.Add(new LanguageModifier("OptimizerHints", "option (fast 1)"));
            return base.TranslateOrder(devicePlan, node, statement, inContextOrderBy);
        }
    }    
    
}
