/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USECONNECTIONPOOLING
#define USESQLOLEDB
//#define USEOLEDBCONNECTION
//#define USEADOCONNECTION

using System;
using System.Globalization;
using System.IO;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Language.TSQL;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using ColumnExpression = Alphora.Dataphor.DAE.Language.SQL.ColumnExpression;
using DropIndexStatement = Alphora.Dataphor.DAE.Language.TSQL.DropIndexStatement;
using SelectStatement = Alphora.Dataphor.DAE.Language.SQL.SelectStatement;

namespace Alphora.Dataphor.DAE.Device.MSSQL
{
    /*
        D4 to SQL translation ->
		
            Retrieve - select expression with single table in the from clause
            Restrict - adds a where clause to the expression, if it exists, conjoins to the existing clause
            Project - if there is already a select list, push the entire expression to a nested from clause
            Extend - add the extend columns to the current select list
            Rename - should translate to aliasing in most cases
            Aggregate - if there is already a select list, push the entire expression to a nested from clause
            Union - add the right side of the expression to a union expression (always distinct)
            Difference - unsupported
            Join (all flavors) - add the right side of the expression to a nested from clause if necessary 
            Order - skip unless this is the outer node
            Browse - unsupported
            CreateTable - direct translation
            AlterTable - direct translation
            DropTable - direct translation			       
    */

    #region Device

    public class MSSQLDevice : SQLDevice
    {
        public const string MSSQLBinaryScalarType = "SQLDevice.MSSQLBinary";

        public static string CEnsureDatabase =
            @"
if not exists (select * from sysdatabases where name = '{0}')
	create database {0}
			";

        protected string _applicationName = "Dataphor Server";
        protected string _databaseName = String.Empty;

        private int _majorVersion = 8; // default to SQL Server 2000 (pending detection)
        protected string _serverName = String.Empty;
        protected bool _shouldDetermineVersion = true;
        protected bool _shouldEnsureDatabase = true;
        protected bool _shouldEnsureOperators = true;
        protected bool _shouldReconcileRowGUIDCol;
        protected bool _useIntegratedSecurity;

        public MSSQLDevice(int iD, string name) : base(iD, name)
        {
            SupportsOrderByNullsFirstLast = false;
            IsOrderByInContext = false;
            // T-SQL allows items in the order by list to reference the column aliases used in the query
            UseParametersForCursors = true;
            ShouldNormalizeWhitespace = false;
        }

		// SQL Server Versions:
			// 7 - 7.0
			// 2000 - 8
			// 2005 - 9
			// 2008 - 10
			// 2008 R2 - 10.50
			// 2012 - 11
			// 2014 - 12
        public int MajorVersion
        {
            get { return _majorVersion; }
            set { _majorVersion = value; }
        }

        public bool IsMSSQL70
        {
            set
            {
                if (value)
                    _majorVersion = 7;
            }
            get { return _majorVersion == 7; }
        }

        public bool IsAccess { set; get; }

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

        /// <value>Indicates whether the device should reconcile columns that are marked as ROWGUIDCOL in SQLServer.</value>
        public bool ShouldReconcileRowGUIDCol
        {
            get { return _shouldReconcileRowGUIDCol; }
            set { _shouldReconcileRowGUIDCol = value; }
        }

        /// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>
        /// <remarks>The value of this property is only valid if IsMSSQL70 is false.</remarks>
        public bool ShouldEnsureOperators
        {
            get { return _shouldEnsureOperators; }
            set { _shouldEnsureOperators = value; }
        }

        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value == null ? String.Empty : value; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = value == null ? String.Empty : value; }
        }

        public string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value == null ? "Dataphor Server" : value; }
        }

        public bool UseIntegratedSecurity
        {
            get { return _useIntegratedSecurity; }
            set { _useIntegratedSecurity = value; }
        }

        protected override void SetMaxIdentifierLength()
        {
            _maxIdentifierLength = 128;
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
#if USEISTRING
				RunScript(AProcess, String.Format(new StreamReader(stream).ReadToEnd(), Name, IsCaseSensitive.ToString().ToLower(), IsMSSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
#else
                RunScript(process,
                          String.Format(new StreamReader(stream).ReadToEnd(), Name, "false",
                                        IsMSSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
#endif
            }
        }

        protected void EnsureDatabase(ServerProcess process)
        {
            string databaseName = DatabaseName;
            DatabaseName = "master";
            try
            {
                var deviceSession = (SQLDeviceSession) Connect(process, process.ServerSession.SessionInfo);
                try
                {
                    deviceSession.Connection.Execute(String.Format(CEnsureDatabase, databaseName));
                }
                finally
                {
                    Disconnect(deviceSession);
                }
            }
            finally
            {
                DatabaseName = databaseName;
            }
        }

        protected void DetermineVersion(ServerProcess process)
        {
            string databaseName = DatabaseName;
            DatabaseName = "master";
            try
            {
                var deviceSession = (SQLDeviceSession) Connect(process, process.ServerSession.SessionInfo);
                try
                {
                    SQLCursor cursor = deviceSession.Connection.Open("select serverproperty('productversion')");
                    try
                    {
                        string version = String.Empty;
                        if (cursor.Next())
                        {
                            version = Convert.ToString(cursor[0]);
                            if (version.Length > 0)
                                _majorVersion = Convert.ToInt32(version.Substring(0, version.IndexOf('.')));
                        }
                    }
                    finally
                    {
                        cursor.Command.Connection.Close(cursor);
                    }
                }
                finally
                {
                    Disconnect(deviceSession);
                }
            }
            finally
            {
                DatabaseName = databaseName;
            }
        }

        protected void InitializeDatabase(ServerProcess process)
        {
            // access checks should go here maybe?

            // Create the database, if necessary
            if (ShouldEnsureDatabase)
                EnsureDatabase(process);

            // Run the initialization script, if specified
            if ((!IsMSSQL70) && ShouldEnsureOperators)
                EnsureOperators(process);
        }

        protected void EnsureOperators(ServerProcess process)
        {
            // no access
            var deviceSession = (SQLDeviceSession) Connect(process, process.ServerSession.SessionInfo);
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
                                string batch in SQLUtility.ProcessBatches(new StreamReader(stream).ReadToEnd(), "go"))
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

		protected override string TooManyRowsOperator()
		{
			return "dbo.DAE_TooManyRows";
		}

        protected override DeviceSession InternalConnect(ServerProcess serverProcess,
                                                         DeviceSessionInfo deviceSessionInfo)
        {
            return new MSSQLDeviceSession(this, serverProcess, deviceSessionInfo);
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
            var selectExpression = new SelectExpression();
            selectExpression.SelectClause = new SelectClause();
            selectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
            return new TableSpecifier(selectExpression, "dummy1");
        }

        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(Plan plan, string tableName, string columnName,
                                                 string domainName)
        {
            switch (domainName.ToLower())
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
				case "date": // 2008 (majorVersion >= 10)
				case "time": // 2008
				//case "datetime2": // 2008
				//case "datetimeoffset": // 2008
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
                case "bit":
                    return plan.DataTypes.SystemBoolean;
                case "tinyint":
                    return plan.DataTypes.SystemByte;
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
				case "time":
					return plan.DataTypes.SystemTime;
                case "datetime":
                case "smalldatetime":
				//case "datetime2":
				//case "datetimeoffset":
                    return plan.DataTypes.SystemDateTime;
                case "money":
                case "smallmoney":
                    return plan.DataTypes.SystemMoney;
                case "uniqueidentifier":
                    return plan.DataTypes.SystemGuid;
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
#if USEISTRING
					return IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString;
#else
                    return plan.DataTypes.SystemString;
#endif
#if USEISTRING
				case "text":
				case "ntext": return (ScalarType)(IsCaseSensitive ? APlan.ServerSession.Server.Catalog[CSQLTextScalarType] : APlan.ServerSession.Server.Catalog[CSQLITextScalarType]);
#else
                case "text":
                case "ntext":
                    return (ScalarType) Compiler.ResolveCatalogIdentifier(plan, SQLTextScalarType, true);
#endif
                case "binary":
                case "timestamp":
                    metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
                    return (ScalarType) Compiler.ResolveCatalogIdentifier(plan, MSSQLBinaryScalarType, true);
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
                DeviceTablesExpression != String.Empty
                    ?
                        base.GetDeviceTablesExpression(tableVar)
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
                            tableVar == null
                                ? String.Empty
                                : String.Format("and so.name = '{0}'", ToSQLIdentifier(tableVar)),
                            _shouldReconcileRowGUIDCol
                                ? String.Empty
                                : "and COLUMNPROPERTY(so.id, sc.name, 'IsRowGUIDCol') = 0"
                            );
        }

        protected override string GetDeviceIndexesExpression(TableVar tableVar)
        {
            return
                DeviceIndexesExpression != String.Empty
                    ?
                        base.GetDeviceIndexesExpression(tableVar)
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
                            IsMSSQL70
                                ? "0 as IsDescending"
                                : "INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') as IsDescending",
                            IsMSSQL70
                                ? String.Empty
                                : "and INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') is not null",
                            tableVar == null
                                ? String.Empty
                                : String.Format("and so.name = '{0}'", ToSQLIdentifier(tableVar))
                            );
        }

        protected override string GetDeviceForeignKeysExpression(TableVar tableVar)
        {
            return
                DeviceForeignKeysExpression != String.Empty
                    ?
                        base.GetDeviceForeignKeysExpression(tableVar)
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
                            tableVar == null ? String.Empty : "and so.name = '" + ToSQLIdentifier(tableVar) + "'"
                            );
        }

        public override void DetermineCursorBehavior(Plan plan, TableNode tableNode)
        {
            base.DetermineCursorBehavior(plan, tableNode);
            // TODO: This will actually only be static if the ADOConnection is used because the DotNet providers do not support a method for obtaining a static cursor.
        }

        // ServerName		

        public override SelectStatement TranslateOrder(DevicePlan devicePlan, TableNode node, SelectStatement statement, bool inContextOrderBy)
        {
            if (statement.Modifiers == null)
                statement.Modifiers = new LanguageModifiers();
            statement.Modifiers.Add(new LanguageModifier("OptimizerHints", "option (fast 1)"));
            return base.TranslateOrder(devicePlan, node, statement, inContextOrderBy);
        }
    }

    public class MSSQLDeviceSession : SQLDeviceSession
    {
        public MSSQLDeviceSession(MSSQLDevice device, ServerProcess serverProcess,
                                  DeviceSessionInfo deviceSessionInfo)
            : base(device, serverProcess, deviceSessionInfo)
        {
        }

        public new MSSQLDevice Device
        {
            get { return (MSSQLDevice) base.Device; }
        }

        protected override SQLConnection InternalCreateConnection()
        {
            // ConnectionClass:
            //  ODBCConnection
            //  OLEDBConnection (default)
            //  ADOConnection 
            //  MSSQLConnection
            // ConnectionStringBuilderClass
            // MSSQLOLEDBConnectionStringBuilder (default) (use with ADOConnection and OLEDBConnection)
            // MSSQLADODotNetConnectionStringBuilder (use with MSSQLConnection)
            // MSSQLODBCConnectionStringBuilder (use with ODBCConnection)

            /*
				Connectivity Implementations with MSSQL:
					ADOConnection ->
						When we use ADO, it is fully functional, but thread locks when we attempt to use it concurrently.
						If we switch providers to MSDASQL, it doesn't work either because the Command and Recordset objects
						are not being released.  The connection complains that too many recordsets are open, even though no
						recordsets are open.  Marshal.ReleaseComObject doesn't help either.  I suspect that the thread locking
						problem we are seeing when we use ADO is related to this same issue.
						
					MSSQLConnection ->
						When we use the native managed provider (SqlClient), performance drops by a factor of 2.
						There are also issues with binary, varbinary, and image data types.
						
					OLEDBConnection ->
						The OLEDB managed provider seems to resolve the thread locking issues, but it has yet to be probed for
						full functionality.  As of right now, this if the only supported connectivity implementation for the 
						MSSQLDevice.  This may change.
						
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

            var classDefinition =
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
	                    "Connection.MSSQLConnection"
						#endif
						#endif
	                    : Device.ConnectionClass
				);

            var builderClass =
                new ClassDefinition
				(
                    Device.ConnectionStringBuilderClass == String.Empty
                        ?
						#if USEADOCONNECTION
						"MSSQLDevice.MSSQLOLEDBConnectionStringBuilder"
						#else
						#if USEOLEDBCONNECTION
						"MSSQLDevice.MSSQLOLEDBConnectionStringBuilder"
						#else
	                    "MSSQLDevice.MSSQLADODotNetConnectionStringBuilder"
						#endif
						#endif
						: Device.ConnectionStringBuilderClass
                    );

            var connectionStringBuilder = 
				(ConnectionStringBuilder)ServerProcess.CreateObject
				(
					builderClass, 
					new object[] {}
				);

            var tags = new Tags();
            tags.AddOrUpdate("ServerName", Device.ServerName);
            tags.AddOrUpdate("DatabaseName", Device.DatabaseName);
            tags.AddOrUpdate("ApplicationName", Device.ApplicationName);
            if (Device.UseIntegratedSecurity)
                tags.AddOrUpdate("IntegratedSecurity", "true");
            else
            {
                tags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
                tags.AddOrUpdate("Password", DeviceSessionInfo.Password);
            }

            tags = connectionStringBuilder.Map(tags);
            Device.GetConnectionParameters(tags, DeviceSessionInfo);
            string connectionString = SQLDevice.TagsToString(tags);
            return
                (SQLConnection)ServerProcess.CreateObject
                (
					classDefinition, 
					new object[] { connectionString }
				);
        }
    }

    #endregion 
    
}
