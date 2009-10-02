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
        public const string CMSSQLBinaryScalarType = "MSSQLDevice.MSSQLBinary";

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

        public MSSQLDevice(int AID, string AName) : base(AID, AName)
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

        public bool IsMSSQL70
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
        /// <remarks>The value of this property is only valid if IsMSSQL70 is false.</remarks>
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
				RunScript(AProcess, String.Format(new StreamReader(LStream).ReadToEnd(), Name, IsCaseSensitive.ToString().ToLower(), IsMSSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
#else
                RunScript(AProcess,
                          String.Format(new StreamReader(LStream).ReadToEnd(), Name, "false",
                                        IsMSSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
#endif
            }
        }

        protected void EnsureDatabase(ServerProcess AProcess)
        {
            string LDatabaseName = DatabaseName;
            DatabaseName = "master";
            try
            {
                var LDeviceSession = (SQLDeviceSession) Connect(AProcess, AProcess.ServerSession.SessionInfo);
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
                var LDeviceSession = (SQLDeviceSession) Connect(AProcess, AProcess.ServerSession.SessionInfo);
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
            if ((!IsMSSQL70) && ShouldEnsureOperators)
                EnsureOperators(AProcess);
        }

        protected void EnsureOperators(ServerProcess AProcess)
        {
            // no access
            var LDeviceSession = (SQLDeviceSession) Connect(AProcess, AProcess.ServerSession.SessionInfo);
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
            return new MSSQLDeviceSession(this, AServerProcess, ADeviceSessionInfo);
        }

        protected override Statement TranslateDropIndex(SQLDevicePlan APlan, TableVar ATableVar, Key AKey)
        {
            var LStatement = new DropIndexStatement();
            LStatement.TableSchema = MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
            LStatement.TableName = ToSQLIdentifier(ATableVar);
            LStatement.IndexSchema = MetaData.GetTag(AKey.MetaData, "Storage.Schema", String.Empty);
            LStatement.IndexName = GetIndexName(LStatement.TableName, AKey);
            return LStatement;
        }

        protected override Statement TranslateDropIndex(SQLDevicePlan APlan, TableVar ATableVar, Order AOrder)
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
        public override bool ShouldIncludeColumn(Plan APlan, string ATableName, string AColumnName,
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
        public override ScalarType FindScalarType(Plan APlan, string ADomainName, int ALength, MetaData AMetaData)
        {
            switch (ADomainName.ToLower())
            {
                case "bit":
                    return APlan.DataTypes.SystemBoolean;
                case "tinyint":
                    return APlan.DataTypes.SystemByte;
                case "smallint":
                    return APlan.DataTypes.SystemShort;
                case "int":
                case "integer":
                    return APlan.DataTypes.SystemInteger;
                case "bigint":
                    return APlan.DataTypes.SystemLong;
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                    return APlan.DataTypes.SystemDecimal;
                case "datetime":
                case "smalldatetime":
                    return APlan.DataTypes.SystemDateTime;
                case "money":
                case "smallmoney":
                    return APlan.DataTypes.SystemMoney;
                case "uniqueidentifier":
                    return APlan.DataTypes.SystemGuid;
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
#if USEISTRING
					return IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString;
#else
                    return APlan.DataTypes.SystemString;
#endif
#if USEISTRING
				case "text":
				case "ntext": return (ScalarType)(IsCaseSensitive ? APlan.ServerSession.Server.Catalog[CSQLTextScalarType] : APlan.ServerSession.Server.Catalog[CSQLITextScalarType]);
#else
                case "text":
                case "ntext":
                    return (ScalarType) Compiler.ResolveCatalogIdentifier(APlan, CSQLTextScalarType, true);
#endif
                case "binary":
                case "timestamp":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
                    return (ScalarType) Compiler.ResolveCatalogIdentifier(APlan, CMSSQLBinaryScalarType, true);
                case "varbinary":
                    AMetaData.Tags.Add(new Tag("Storage.Length", ALength.ToString()));
                    return APlan.DataTypes.SystemBinary;
                case "image":
                    return APlan.DataTypes.SystemBinary;
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
                            IsMSSQL70
                                ? "0 as IsDescending"
                                : "INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') as IsDescending",
                            IsMSSQL70
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

    public class MSSQLDeviceSession : SQLDeviceSession
    {
        public MSSQLDeviceSession(MSSQLDevice ADevice, ServerProcess AServerProcess,
                                  DeviceSessionInfo ADeviceSessionInfo)
            : base(ADevice, AServerProcess, ADeviceSessionInfo)
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
	                    "Connection.MSSQLConnection"
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

            var LConnectionStringBuilder = 
				(ConnectionStringBuilder)ServerProcess.CreateObject
				(
					LBuilderClass, 
					new object[] {}
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
                (SQLConnection)ServerProcess.CreateObject
                (
					LClassDefinition, 
					new object[] { LConnectionString }
				);
        }
    }

    #endregion 
    
}