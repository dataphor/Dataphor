/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USECONNECTIONPOOLING
#define USESQLOLEDB
//#define USEOLEDBCONNECTION
//#define USEADOCONNECTION

namespace Alphora.Dataphor.DAE.Device.MSSQL
{
	using System;
	using System.IO;
	using System.Text;
	using System.Globalization;
	using System.Reflection;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Data;
	using System.Data.SqlTypes;
	using System.Data.SqlClient;
	using System.Resources;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Schema;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using TSQL = Alphora.Dataphor.DAE.Language.TSQL;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Server;

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
			
        Data type mapping ->
		
            DAE Type	|	TSQL Type														|	Translation Handler
            ------------|---------------------------------------------------------------------------------------------
            Boolean		|	bit																|	MSSQLBoolean
            Byte		|   tinyint															|	MSSQLByte
            SByte		|	smallint														|	SQLSByte
            Short		|	smallint														|	SQLShort
            UShort		|	integer															|	SQLUShort
            Integer		|	integer															|	SQLInteger
            UInteger	|	bigint															|	SQLUInteger
            Long		|	bigint															|	SQLLong
            ULong		|	decimal(20, 0)													|	SQLULong
            Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SQLDecimal
            TimeSpan	|	bigint															|	SQLTimeSpan
            DateTime	|	datetime														|	MSSQLDateTime
            Date		|	datetime														|	MSSQLDate
            Time		|	datetime														|	MSSQLTime
            Money		|	money															|	MSSQLMoney
            Guid		|	uniqueidentifier												|	MSSQLGuid
            String		|	varchar(Storage.Length)											|	SQLString
            Binary		|	image															|	MSSQLBinary
            SQLText		|	text															|	MSSQLText
            MSSQLBinary |	binary(Storage.Length)											|	MSSQLMSSQLBinary
    */

    #region Device

    public class MSSQLDevice : SQLDevice
    {        
		public const string CMSSQLBinaryScalarType = "MSSQLDevice.MSSQLBinary";
		
		public MSSQLDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID)
		{
			IsOrderByInContext = false; // T-SQL allows items in the order by list to reference the column aliases used in the query
			UseParametersForCursors = true;
			ShouldNormalizeWhitespace = false;
		}

		private int FMajorVersion = 8;	// default to SQL Server 2000 (pending detection)
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

		private bool FIsAccess = false;
		public bool IsAccess
		{
			set { FIsAccess = value; }
			get { return FIsAccess;}
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
				RunScript(AProcess, String.Format(new StreamReader(LStream).ReadToEnd(), Name, "false", IsMSSQL70.ToString().ToLower(), IsAccess.ToString().ToLower()));
				#endif
			}
		}
		
		protected bool FShouldDetermineVersion = true;
		/// <value>Indicates whether the device should auto-determine the version of the target system.</value>
		public bool ShouldDetermineVersion
		{
			get { return FShouldDetermineVersion; }
			set { FShouldDetermineVersion = value; }
		}
		
		public static string CEnsureDatabase =
			@"
if not exists (select * from sysdatabases where name = '{0}')
	create database {0}
			";
			
		protected bool FShouldEnsureDatabase = true;
		/// <value>Indicates whether the device should create the database if it does not already exist.</value>
		public bool ShouldEnsureDatabase
		{
			// maybe check and throw to see if it is access because can't create (at least I don't think so) an access database;
			get { return FShouldEnsureDatabase; }
			set { FShouldEnsureDatabase = value; }
		}
		
		protected bool FShouldReconcileRowGUIDCol;
		/// <value>Indicates whether the device should reconcile columns that are marked as ROWGUIDCOL in SQLServer.</value>
		public bool ShouldReconcileRowGUIDCol
		{
			get { return FShouldReconcileRowGUIDCol; }
			set { FShouldReconcileRowGUIDCol = value; }
		}
		
		protected void EnsureDatabase(ServerProcess AProcess)
		{
			string LDatabaseName = DatabaseName;
			DatabaseName = "master";
			try
			{
				SQLDeviceSession LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
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
		
		protected bool FShouldEnsureOperators = true;
		/// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>
		/// <remarks>The value of this property is only valid if IsMSSQL70 is false.</remarks>
		public bool ShouldEnsureOperators
		{
			get { return FShouldEnsureOperators; }
			set { FShouldEnsureOperators = value; }
		}
		
		protected void DetermineVersion(ServerProcess AProcess)
		{
			string LDatabaseName = DatabaseName;
			DatabaseName = "master";
			try
			{
				SQLDeviceSession LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
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
			SQLDeviceSession LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
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
							foreach (string LBatch in SQLUtility.ProcessBatches(new StreamReader(LStream).ReadToEnd(), "go"))
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
		
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new MSSQLDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

        protected override Statement TranslateDropIndex(TableVar ATableVar, Key AKey)
        {
			TSQL.DropIndexStatement LStatement = new TSQL.DropIndexStatement();
			LStatement.TableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
			LStatement.TableName = ToSQLIdentifier(ATableVar);
			LStatement.IndexSchema = D4.MetaData.GetTag(AKey.MetaData, "Storage.Schema", String.Empty);
			LStatement.IndexName = GetIndexName(LStatement.TableName, AKey);
			return LStatement;
        }
        
        protected override Statement TranslateDropIndex(TableVar ATableVar, Order AOrder)
        {
			TSQL.DropIndexStatement LStatement = new TSQL.DropIndexStatement();
			LStatement.TableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
			LStatement.TableName = ToSQLIdentifier(ATableVar);
			LStatement.IndexSchema = D4.MetaData.GetTag(AOrder.MetaData, "Storage.Schema", String.Empty);
			LStatement.IndexName = GetIndexName(LStatement.TableName, AOrder);
			return LStatement;
        }
        
        public override TableSpecifier GetDummyTableSpecifier()
        {
			SelectExpression LSelectExpression = new SelectExpression();
			LSelectExpression.SelectClause = new SelectClause();
			LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
			return new TableSpecifier(LSelectExpression, "dummy1");
        }
        
        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(ServerProcess AProcess, string ATableName, string AColumnName, string ADomainName)
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
				case "timestamp": return true;
				default: return false;
			}
        }
        
		// FindScalarType
        public override ScalarType FindScalarType(ServerProcess AProcess, string ADomainName, int ALength, D4.MetaData AMetaData)
        {
			switch (ADomainName.ToLower())
			{
				case "bit": return AProcess.DataTypes.SystemBoolean;
				case "tinyint": return AProcess.DataTypes.SystemByte;
				case "smallint": return AProcess.DataTypes.SystemShort;
				case "int": 
				case "integer": return AProcess.DataTypes.SystemInteger;
				case "bigint": return AProcess.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
				case "float": 
				case "real": return AProcess.DataTypes.SystemDecimal;
				case "datetime":
				case "smalldatetime": return AProcess.DataTypes.SystemDateTime;
				case "money":
				case "smallmoney": return AProcess.DataTypes.SystemMoney;
				case "uniqueidentifier": return AProcess.DataTypes.SystemGuid;
				case "char":
				case "varchar":
				case "nchar":
				case "nvarchar": 
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
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
				case "ntext": return (ScalarType)D4.Compiler.ResolveCatalogIdentifier(AProcess.Plan, CSQLTextScalarType, true);
				#endif
				case "binary": 
				case "timestamp":
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
					return (ScalarType)D4.Compiler.ResolveCatalogIdentifier(AProcess.Plan, CMSSQLBinaryScalarType, true);
				case "varbinary":
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
					return AProcess.DataTypes.SystemBinary;
				case "image": return AProcess.DataTypes.SystemBinary;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
			}
        }
        
		// Emitter
		override protected SQLTextEmitter InternalCreateEmitter() { return new TSQL.TSQLTextEmitter();	}
		
		protected override string GetDeviceTablesExpression(TableVar ATableVar)
		{
			return
				DeviceTablesExpression != String.Empty ?
					base.GetDeviceTablesExpression(ATableVar) :
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
						ATableVar == null ? String.Empty : String.Format("and so.name = '{0}'", ToSQLIdentifier(ATableVar)),
						FShouldReconcileRowGUIDCol ? String.Empty : "and COLUMNPROPERTY(so.id, sc.name, 'IsRowGUIDCol') = 0"
					);
		}
		
		protected override string GetDeviceIndexesExpression(TableVar ATableVar)
		{
			return
				DeviceIndexesExpression != String.Empty ?
					base.GetDeviceIndexesExpression(ATableVar) :
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
						IsMSSQL70 ? "0 as IsDescending" : "INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') as IsDescending",
						IsMSSQL70 ? String.Empty : "and INDEXKEY_PROPERTY(so.id, si.indid, sik.keyno, 'IsDescending') is not null",
						ATableVar == null ? String.Empty : String.Format("and so.name = '{0}'", ToSQLIdentifier(ATableVar))
					);
		}
		
		protected override string GetDeviceForeignKeysExpression(TableVar ATableVar)
		{
			return
				DeviceForeignKeysExpression != String.Empty ?
					base.GetDeviceForeignKeysExpression(ATableVar) :
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
		protected string FServerName = String.Empty;
		public string ServerName
		{
			get { return FServerName; }
			set { FServerName = value == null ? String.Empty : value; }
		}
		
		// DatabaseName		
		protected string FDatabaseName = String.Empty;
		public string DatabaseName 
		{ 
			get { return FDatabaseName; } 
			set { FDatabaseName = value == null ? String.Empty : value; } 
		}
		
		// ApplicationName
		protected string FApplicationName = "Dataphor Server";
		public string ApplicationName
		{
			get { return FApplicationName; }
			set { FApplicationName = value == null ? "Dataphor Server" : value; }
		}
		
		// UseIntegratedSecurity
		protected bool FUseIntegratedSecurity;
		public bool UseIntegratedSecurity
		{
			get { return FUseIntegratedSecurity; }
			set { FUseIntegratedSecurity = value; }
		}
		
		public override SelectStatement TranslateOrder(DevicePlan ADevicePlan, TableNode ANode, SelectStatement AStatement)
		{
			if (AStatement.Modifiers == null)
				AStatement.Modifiers = new LanguageModifiers();
			AStatement.Modifiers.Add(new LanguageModifier("OptimizerHints", "option (fast 1)"));
			return base.TranslateOrder(ADevicePlan, ANode, AStatement);
		}
	}
	
    public class MSSQLDeviceSession : SQLDeviceSession
    {
		public MSSQLDeviceSession(MSSQLDevice ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		public new MSSQLDevice Device { get { return (MSSQLDevice)base.Device; } }

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

			D4.ClassDefinition LClassDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						#if USEADOCONNECTION
						"ADOConnection.ADOConnection" : 
						#else
						#if USEOLEDBCONNECTION
						"Connection.OLEDBConnection" :
						#else
						"Connection.MSSQLConnection" :
						#endif
						#endif
						Device.ConnectionClass
				);
			D4.ClassDefinition LBuilderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						#if USEADOCONNECTION
						"MSSQLDevice.MSSQLOLEDBConnectionStringBuilder" :
						#else
						#if USEOLEDBCONNECTION
						"MSSQLDevice.MSSQLOLEDBConnectionStringBuilder" :
						#else
						"MSSQLDevice.MSSQLADODotNetConnectionStringBuilder"	:
						#endif
						#endif
						Device.ConnectionStringBuilderClass
				);
			ConnectionStringBuilder LConnectionStringBuilder = (ConnectionStringBuilder)ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LBuilderClass, new object[]{});
			
			D4.Tags LTags = new D4.Tags();
			LTags.AddOrUpdate("ServerName",Device.ServerName);
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
			return (SQLConnection)ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LClassDefinition, new object[]{LConnectionString});
		}
    }

    #endregion
    #region Connection String Builder

    /// <summary>
	/// This class is the tag translator for ADO
	/// </summary>
	public class MSSQLOLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLOLEDBConnectionStringBuilder()
		{
			#if USESQLOLEDB
			FParameters.AddOrUpdate("Provider", "SQLOLEDB");
			#else
			FParameters.AddOrUpdate("Provider", "MSDASQL");
			#endif

			#if !USECONNECTIONPOOLING
			FParameters.AddOrUpdate("OLE DB Services", "-2"); // Turn off OLEDB resource pooling
			#endif
			FLegend.AddOrUpdate("ServerName", "Data source");
			FLegend.AddOrUpdate("DatabaseName", "initial catalog");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
			FLegend.AddOrUpdate("ApplicationName", "app name");
		}
		
		public override D4.Tags Map(D4.Tags ATags)
		{
			D4.Tags LTags = base.Map(ATags);
			D4.Tag LTag = LTags.GetTag("IntegratedSecurity");
			if (LTag != null)
			{
				LTags.Remove(LTag);
				LTags.AddOrUpdate("Integrated Security", "SSPI");
			}
			return LTags;
		}
	}

	public class MSSQLADODotNetConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLADODotNetConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("ServerName", "server");
			FLegend.AddOrUpdate("DatabaseName", "database");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
			FLegend.AddOrUpdate("IntegratedSecurity", "integrated security");
			FLegend.AddOrUpdate("ApplicationName", "application name");
		}
	}

	public class MSSQLODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public MSSQLODBCConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("ServerName", "DSN");
			FLegend.AddOrUpdate("DatabaseName", "Database");
			FLegend.AddOrUpdate("UserName", "UID");
			FLegend.AddOrUpdate("Password", "PWD");
			FLegend.AddOrUpdate("ApplicationName", "APPNAME");
		}
		
		public override D4.Tags Map(D4.Tags ATags)
		{
			D4.Tags LTags = base.Map(ATags);
			D4.Tag LTag = LTags.GetTag("IntegratedSecurity");
			if (LTag != null)
			{
				LTags.Remove(LTag.Name);
				LTags.AddOrUpdate("Trusted_Connection", "Yes");
			}
			return LTags;
		}
	}

	public class AccessConnectionStringBuilder : ConnectionStringBuilder
	{
		public AccessConnectionStringBuilder()
		{
			//FLegend.Add

		}

		public override D4.Tags Map(D4.Tags ATags)
		{
			D4.Tags LTags = base.Map(ATags);
			return LTags;
		}
    }

    #endregion

    #region Types

    /// <summary>
    /// MSSQL type : bit
    ///	D4 Type : Boolean
    /// 0 = false
    /// 1 = true
    /// </summary>
    public class MSSQLBoolean : SQLScalarType
    {
		public MSSQLBoolean(int AID, string AName) : base(AID, AName) {}
		//public MSSQLBoolean(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLBoolean(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}
		
		public override string ToLiteral(Scalar AValue)
		{
			if ((AValue == null) || AValue.IsNil)
				return String.Format("cast(null as {0})", DomainName());
				
			return AValue.AsBoolean ? "1" : "0";
		}
		
		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			if (AValue is bool)
				return new Scalar(AProcess, ScalarType, (bool)AValue);
			else 
				return new Scalar(AProcess, ScalarType, (int)AValue == 0 ? false : true);
		}
		
		public override object FromScalar(Scalar AValue)
		{
			return AValue.AsBoolean;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLBooleanType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "bit";
		}
    }

	/// <summary>
	/// MSSQL type : tinyint
	/// D4 Type : Byte
	/// </summary>
    public class MSSQLByte : SQLScalarType
    {
		public MSSQLByte(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			// According to the docs for the SQLOLEDB provider this is supposed to come back as a byte, but
			// it is coming back as a short, I don't know why, maybe interop?
			return new Scalar(AProcess, ScalarType, Convert.ToByte(AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return AValue.AsByte;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(1);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "tinyint";
		}
    }
    
	/// <summary>
	/// MSSQL type : money
	/// D4 Type : Money
	/// </summary>
    public class MSSQLMoney : SQLScalarType
    {
		public MSSQLMoney(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, Convert.ToDecimal(AValue));
		}
		
		public override object FromScalar(Scalar AValue)
		{
			return AValue.AsDecimal;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLMoneyType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "money";
		}
    }
    
	/// <summary>
	/// MSSQL type : datetime
	/// D4 Type : DateTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// The ADO connectivity layer seems to be rounding datetime values to the nearest second, even though the server is capable of storing greater precision
	/// </summary>
    public class MSSQLDateTime : SQLScalarType
    {
		public const string CDateTimeFormat = "yyyy/MM/dd HH:mm:ss";
		
		public static readonly DateTime MinValue = new DateTime(1753, 1, 1);
		public static readonly DateTime Accuracy = new DateTime((long)(TimeSpan.TicksPerMillisecond * 3.33));

		public MSSQLDateTime(int AID, string AName) : base(AID, AName) {}

		private string FDateTimeFormat = CDateTimeFormat;
		public string DateTimeFormat
		{
			get { return FDateTimeFormat; }
			set { FDateTimeFormat = value; }
		}

		public override string ToLiteral(Scalar AValue)
		{
			if ((AValue == null) || AValue.IsNil)
				return String.Format("cast(null as {0})", DomainName());

			DateTime LValue = AValue.AsDateTime;				
			if (LValue == DateTime.MinValue)
				LValue = MinValue;
			if (LValue < MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

			return String.Format("'{0}'", LValue.ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo));
		}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			DateTime LDateTime = (DateTime)AValue;
			// If the value is equal to the device's zero date, set it to Dataphor's zero date
			if (LDateTime == MinValue)
				LDateTime = DateTime.MinValue;
			long LTicks = LDateTime.Ticks;
			return new Scalar(AProcess, ScalarType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerSecond)));
		}
		
		public override object FromScalar(Scalar AValue)
		{
			DateTime LValue = AValue.AsDateTime;
			// If the value is equal to Dataphor's zero date, set it to the Device's zero date
			if (LValue == DateTime.MinValue)
				LValue = MinValue;
			if (LValue < MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());
			return LValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "datetime";
		}
    }

	/// <summary>
	/// MSSQL type : datetime
	/// D4 Type : Date
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
    public class MSSQLDate : SQLScalarType
    {
		public const string CDateFormat = "yyyy/MM/dd";
		
		public MSSQLDate(int AID, string AName) : base(AID, AName) {}

		private string FDateFormat = CDateFormat;
		public string DateFormat
		{
			get { return FDateFormat; }
			set { FDateFormat = value; }
		}

		public override string ToLiteral(Scalar AValue)
		{
			if ((AValue == null) || AValue.IsNil)
				return String.Format("cast(null as {0})", DomainName());
				
			DateTime LValue = AValue.AsDateTime;
			// If the value is equal to Dataphor's zero date (Jan, 1, 0001), set it to the device's zero date
			if (LValue == DateTime.MinValue)
				LValue = MSSQLDateTime.MinValue;
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

			return String.Format("'{0}'", LValue.ToString(DateFormat, DateTimeFormatInfo.InvariantInfo));
		}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			DateTime LDateTime = (DateTime)AValue;
			// If the value is equal to the Device's zero date, set it to Dataphor's zero date
			if (LDateTime == MSSQLDateTime.MinValue)
				LDateTime = DateTime.MinValue;
			long LTicks = LDateTime.Ticks;
			return new Scalar(AProcess, ScalarType, new DateTime(LTicks - (LTicks % TimeSpan.TicksPerDay)));
		}
		
		public override object FromScalar(Scalar AValue)
		{
			DateTime LValue = AValue.AsDateTime;
			// If the value is equal to Dataphor's zero date (Jan, 1, 0001), set it to the device's zero date
			if (LValue == DateTime.MinValue)
				LValue = MSSQLDateTime.MinValue;
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());
			return LValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "datetime";
		}
    }

	/// <summary>
	/// MSSQL type : datetime
	/// D4 Type : SQLTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
    public class MSSQLTime : SQLScalarType
    {
		public const string CTimeFormat = "HH:mm:ss";
		
		public MSSQLTime(int AID, string AName) : base(AID, AName) {}

		private string FTimeFormat = CTimeFormat;
		public string TimeFormat
		{
			get { return FTimeFormat; }
			set { FTimeFormat = value; }
		}

		public override string ToLiteral(Scalar AValue)
		{
			if ((AValue == null) || AValue.IsNil)
				return String.Format("cast(null as {0})", DomainName());
				
			// Added 1899 years, so that a time can actually be stored. 
			// Adding 1899 years puts it at the year 1900
			// which is stored as zero in MSSQL.
			// this year value of 1900 may make some translation easier.
			DateTime LValue = AValue.AsDateTime.AddYears(1899); 
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());

			return String.Format("'{0}'", LValue.ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo));
		}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, new DateTime(((DateTime)AValue).Ticks % TimeSpan.TicksPerDay ));
		}
		
		public override object FromScalar(Scalar AValue)
		{
			// Added 1899 years, so that a time can actually be stored. 
			// Adding 1899 years puts it at the year 1900
			// which is stored as zero in MSSQL.
            // this year value of 1900 may make some translation easier.
			DateTime LValue = AValue.AsDateTime.AddYears(1899); 
			if (LValue < MSSQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, LValue.ToString());
			return LValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTimeType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "datetime";
		}
    }

	/// <summary>
	/// MSSQL type : uniqueidentifier
	/// D4 type : Guid
	/// TSQL comparison operators for the TSQL uniqueidentifier data type use string semantics, not hexadecimal
	/// </summary>
    public class MSSQLGuid : SQLScalarType
    {
		public MSSQLGuid(int AID, string AName) : base(AID, AName) {}
		
		public override string ToLiteral(Scalar AValue)
		{
			if ((AValue == null) || AValue.IsNil)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("'{0}'", AValue.AsGuid.ToString());
		}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			if (AValue is string)
				return new Scalar(AProcess, ScalarType, new Guid((string)AValue));
			else
				return new Scalar(AProcess, ScalarType, (Guid)AValue);
		}
		
		public override object FromScalar(Scalar AValue)
		{
			return AValue.AsGuid;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLGuidType();
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "uniqueidentifier";
		}
    }
    
	/// <summary>
	/// MSSQL type : text
	/// D4 Type : SQLText, SQLIText
	/// </summary>
    public class MSSQLText : SQLText
    {
		public MSSQLText(int AID, string AName) : base(AID, AName) {}
		//public MSSQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "text";
		}
    }
    
    /// <summary>
    /// MSSQL type : image
    /// D4 type : Binary
    /// </summary>
    public class MSSQLBinary : SQLBinary
    {
		public MSSQLBinary(int AID, string AName) : base(AID, AName) {}
		//public MSSQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "image";
		}
    }
    
    /// <summary>
    /// MSSQL type : image
    /// D4 type : Graphic
    /// </summary>
    public class MSSQLGraphic : SQLGraphic
    {
		public MSSQLGraphic(int AID, string AName) : base(AID, AName) {}
		//public MSSQLGraphic(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MSSQLGraphic(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "image";
		}
    }
    
	/// <summary>
	/// MSSQL type : binary(Storage.Length)
	/// D4 Type : MSSQLBinary
	/// </summary>
	public class MSSQLMSSQLBinary : SQLScalarType
	{
		public MSSQLMSSQLBinary(int AID, string AName) : base(AID, AName) {}

		public override string ToLiteral(Scalar AValue)
		{
			if ((AValue == null) || AValue.IsNil)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("'{0}'", AValue.AsString);
		}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, (byte[])AValue);
		}
		
		public override object FromScalar(Scalar AValue)
		{
			return AValue.AsByteArray;
		}
		
		protected int GetLength(D4.MetaData AMetaData)
		{
			return Int32.Parse(GetTag("Storage.Length", "30", AMetaData));
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLByteArrayType(GetLength(AMetaData));
		}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return String.Format("binary({0})", GetLength(AMetaData).ToString()); // todo: what about varbiniary?
		}
	}

    public class MSSQLRetrieve : SQLDeviceOperator
    {
		public MSSQLRetrieve(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			TableVarNode LTableVarNode = (TableVarNode)APlanNode;
			TableVar LTableVar = LTableVarNode.TableVar;

			if (LTableVar is BaseTableVar)
			{
				SQLRangeVar LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
				foreach (TableVarColumn LColumn in LTableVar.Columns)
					LRangeVar.Columns.Add(new SQLRangeVarColumn(LColumn, LRangeVar.Name, LDevicePlan.Device.ToSQLIdentifier(LColumn), LDevicePlan.Device.ToSQLIdentifier(LColumn.Name)));
				LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
				SelectExpression LSelectExpression = new SelectExpression();
				// TODO: Load-time binding resolution of updlock optimizer hint: The current assumption is that if no cursor isolation level is specified and the cursor is updatable then udpate locks should be taken. 
				// If we had a load-time binding step then the decision to take update locks could be deferred until we are certain that the query will run in an isolated transaction.
				LSelectExpression.FromClause = 
					new AlgebraicFromClause
					(
						new TableSpecifier
						(
							new TSQL.TableExpression
							(
								D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema),
								LDevicePlan.Device.ToSQLIdentifier(LTableVar), 
								#if USEFASTFIRSTROW
								(
									LTableVarNode.Supports(CursorCapability.Updateable) && 
									(
										(SQLTable.CursorIsolationToIsolationLevel(LTableVarNode.CursorIsolation, ADevicePlan.Plan.ServerProcess.CurrentIsolationLevel()) == DAE.IsolationLevel.Isolated)
									) ? 
									"(fastfirstrow, updlock)" : 
									"(fastfirstrow)"
								)
								#else
								(
									LTableVarNode.Supports(CursorCapability.Updateable) && 
									(
										(SQLTable.CursorIsolationToIsolationLevel(LTableVarNode.CursorIsolation, ADevicePlan.Plan.ServerProcess.CurrentIsolationLevel()) == DAE.IsolationLevel.Isolated)
									) ? 
									"(updlock)" : 
									""
								)
								#endif
							),
							LRangeVar.Name
						)
					);

				LSelectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn LColumn in LTableVar.Columns)
					LSelectExpression.SelectClause.Columns.Add(LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());

				LSelectExpression.SelectClause.Distinct = 
					(LTableVar.Keys.Count == 1) && 
					Convert.ToBoolean(D4.MetaData.GetTag(LTableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false")); // TODO: Fix this in the DB2 and base SQL devices !!!
				
				return LSelectExpression;
			}
			else
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
    }

	public class MSSQLToday : SQLDeviceOperator
	{
		public MSSQLToday(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			return new CallExpression("Round", new Expression[]{new CallExpression("GetDate", new Expression[]{}), new ValueExpression(0), new ValueExpression(1)});
		}
	}

	public class MSSQLSubString : SQLDeviceOperator
	{
		public MSSQLSubString(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new CallExpression
				(
					"Substring", 
					new Expression[]
					{
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false), 
						new BinaryExpression
						(
							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
							"iAddition",
							new ValueExpression(1, TokenType.Integer)
						), 
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[2], false)
					}
				);
		}
	}

	// Pos(ASubString, AString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
	public class MSSQLPos : SQLDeviceOperator
	{
		public MSSQLPos(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new BinaryExpression
				(
					new CaseExpression
					(
						new CaseItemExpression[]
						{
							new CaseItemExpression
							(
								new BinaryExpression
								(
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
									"iEqual",
									new ValueExpression(String.Empty, TokenType.String)
								),
								new ValueExpression(1, TokenType.Integer)
							)
						},
						new CaseElseExpression
						(
							new CallExpression
							(
								"CharIndex",
								new Expression[]
								{
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
								}
							)
						)
					),
					"iSubtraction",
					new ValueExpression(1, TokenType.Integer)
				);
		}
	}

	// IndexOf(AString, ASubString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
	public class MSSQLIndexOf : SQLDeviceOperator
	{
		public MSSQLIndexOf(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new BinaryExpression
				(
					new CaseExpression
					(
						new CaseItemExpression[]
						{
							new CaseItemExpression
							(
								new BinaryExpression
								(
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
									"iEqual",
									new ValueExpression(String.Empty, TokenType.String)
								),
								new ValueExpression(1, TokenType.Integer)
							)
						},
						new CaseElseExpression
						(
							new CallExpression
							(
								"CharIndex",
								new Expression[]
								{
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
								}
							)
						)
					),
					"iSubtraction",
					new ValueExpression(1, TokenType.Integer)
				);
		}
	}

	// CompareText(ALeftValue, ARightValue) ::= case when Upper(ALeftValue) = Upper(ARightValue) then 0 when Upper(ALeftValue) < Upper(ARightValue) then -1 else 1 end
	public class MSSQLCompareText : SQLDeviceOperator
	{
		public MSSQLCompareText(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)}),
								"iEqual",
								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)})
							), 
							new ValueExpression(0)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)}),
								"iLess",
								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)})
							),
							new ValueExpression(-1)
						)
					},
					new CaseElseExpression(new ValueExpression(1))
				);
		}
    }
    #endregion


    #region Instruction Nodes

    public class MSSQLMSSQLBinaryCompareNode : InstructionNode
	{
		public static int Compare(Scalar ALeftValue, Scalar ARightValue)
		{
			Stream LLeftStream = ALeftValue.IsNative ? new MemoryStream(ALeftValue.AsByteArray, 0, ALeftValue.AsByteArray.Length, false, true) : ALeftValue.OpenStream();
			try
			{
				Stream LRightStream = ARightValue.IsNative ? new MemoryStream(ARightValue.AsByteArray, 0, ARightValue.AsByteArray.Length, false, true) : ARightValue.OpenStream();
				try
				{
					int LLeftByte;
					int LRightByte;

					while (true)
					{
						LLeftByte = LLeftStream.ReadByte();
						LRightByte = LRightStream.ReadByte();
						
						if (LLeftByte != LRightByte)
							break;
						
						if (LLeftByte == -1)
							break;

						if (LRightByte == -1)
							break;
					}
					
					return LLeftByte == LRightByte ? 0 : LLeftByte > LRightByte ? 1 : -1;
				}
				finally
				{
					LRightStream.Close();
				}
			}
			finally
			{
				LLeftStream.Close();
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value)));
		}
	}
	
	public class MSSQLMSSQLBinaryEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, MSSQLMSSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value) == 0));
		}
	}
	
	public class MSSQLMSSQLBinaryNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, MSSQLMSSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value) != 0));
		}
	}
	
	public class MSSQLMSSQLBinaryLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, MSSQLMSSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value) < 0));
		}
	}
	
	public class MSSQLMSSQLBinaryInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, MSSQLMSSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value) <= 0));
		}
	}
	
	public class MSSQLMSSQLBinaryGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, MSSQLMSSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value) > 0));
		}
	}
	
	public class MSSQLMSSQLBinaryInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || (AArguments[1].Value == null))
				return new DataVar(FDataType, null);
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, MSSQLMSSQLBinaryCompareNode.Compare((Scalar)AArguments[0].Value, (Scalar)AArguments[1].Value) >= 0));
		}
    }
    #endregion

    #region Operators


    // ToString(AValue) ::= Convert(varchar, AValue)
	public class MSSQLToString : SQLDeviceOperator
	{
		public MSSQLToString(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("varchar"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	public class MSSQLToBit : SQLDeviceOperator
	{
		public MSSQLToBit(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("bit"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	public class MSSQLToTinyInt : SQLDeviceOperator
	{
		public MSSQLToTinyInt(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("tinyint"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}

	// ToByte(AValue) ::= convert(tinyint, AValue & (power(2, 8) - 1))	
	public class MSSQLToByte : SQLDeviceOperator
	{
		public MSSQLToByte(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("tinyint"),
						new BinaryExpression
						(
							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(8, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}

	public class MSSQLToSmallInt : SQLDeviceOperator
	{
		public MSSQLToSmallInt(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("smallint"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	// ToSByte(AValue) ::= convert(smallint, ((AValue & (power(2, 8) - 1) & ~power(2, 7)) - (power(2, 7) & AValue)))
	public class MSSQLToSByte : SQLDeviceOperator
	{
		public MSSQLToSByte(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("smallint"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new CallExpression
										(
											"Power",
											new Expression[]
											{
												new ValueExpression(2, TokenType.Integer),
												new ValueExpression(8, TokenType.Integer)
											}
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new ValueExpression(2, TokenType.Integer),
											new ValueExpression(7, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(7, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	// ToShort(AValue) ::= convert(smallint, ((AValue & (power(2, 16) - 1) & ~power(2, 15)) - (power(2, 15) & AValue)))
	public class MSSQLToShort : SQLDeviceOperator
	{
		public MSSQLToShort(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("smallint"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new CallExpression
										(
											"Power",
											new Expression[]
											{
												new ValueExpression(2, TokenType.Integer),
												new ValueExpression(16, TokenType.Integer)
											}
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new ValueExpression(2, TokenType.Integer),
											new ValueExpression(15, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(15, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	public class MSSQLToInt : SQLDeviceOperator
	{
		public MSSQLToInt(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("int"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	// ToUShort(AValue) ::= convert(int, AValue & (power(2, 16) - 1))	
	public class MSSQLToUShort : SQLDeviceOperator
	{
		public MSSQLToUShort(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("int"),
						new BinaryExpression
						(
							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(16, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}
	
	// ToInteger(AValue) ::= convert(int, ((AValue & ((power(convert(bigint, 2), 32) - 1) & ~(power(convert(bigint, 2), 31)) - (power(convert(bigint, 2), 31) & AValue)))
	public class MSSQLToInteger : SQLDeviceOperator
	{
		public MSSQLToInteger(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("int"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new CallExpression
										(
											"Power",
											new Expression[]
											{
												new CallExpression
												(
													"Convert",
													new Expression[]
													{
														new IdentifierExpression("bigint"),
														new ValueExpression(2, TokenType.Integer),
													}
												),
												new ValueExpression(32, TokenType.Integer)
											}
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new CallExpression
											(
												"Convert",
												new Expression[]
												{
													new IdentifierExpression("bigint"),
													new ValueExpression(2, TokenType.Integer)
												}
											),
											new ValueExpression(31, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new CallExpression
										(
											"Convert",
											new Expression[]
											{
												new IdentifierExpression("bigint"),
												new ValueExpression(2, TokenType.Integer)
											}
										),
										new ValueExpression(31, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	public class MSSQLToBigInt : SQLDeviceOperator
	{
		public MSSQLToBigInt(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("bigint"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	// ToUInteger(AValue) ::= convert(bigint, AValue & (power(convert(bigint, 2), 32) - 1))	
	public class MSSQLToUInteger : SQLDeviceOperator
	{
		public MSSQLToUInteger(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("bigint"),
						new BinaryExpression
						(
							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new CallExpression
										(
											"Convert",
											new Expression[]
											{
												new IdentifierExpression("bigint"),
												new ValueExpression(2, TokenType.Integer)
											}
										),
										new ValueExpression(32, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}
	
	// ToLong(AValue) ::= convert(bigint, ((AValue & ((power(2, 64) * 1) - 1) & ~power(2, 63)) - (power(2, 63) & AValue)))
	public class MSSQLToLong : SQLDeviceOperator
	{
		public MSSQLToLong(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("bigint"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new BinaryExpression
										(
											new CallExpression
											(
												"Power",
												new Expression[]
												{
													new ValueExpression(2, TokenType.Integer),
													new ValueExpression(64, TokenType.Integer)
												}
											),
											"iMultiplication",
											new ValueExpression(1, TokenType.Integer)
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new ValueExpression(2, TokenType.Integer),
											new ValueExpression(63, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(63, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	public class MSSQLToDecimal20 : SQLDeviceOperator
	{
		public MSSQLToDecimal20(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("decimal(20, 0)"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	public class MSSQLToDecimal288 : SQLDeviceOperator
	{
		public MSSQLToDecimal288(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("decimal(28, 8)"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	// ToULong(AValue) ::= convert(decimal(20, 0), AValue & (power(2, 64) - 1))	
	public class MSSQLToULong : SQLDeviceOperator
	{
		public MSSQLToULong(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("decimal(20, 0)"),
						new BinaryExpression
						(
							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(64, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}
	
	public class MSSQLToDecimal : SQLDeviceOperator
	{
		public MSSQLToDecimal(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("decimal"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	public class MSSQLToMoney : SQLDeviceOperator
	{
		public MSSQLToMoney(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("money"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	public class MSSQLToUniqueIdentifier : SQLDeviceOperator
	{
		public MSSQLToUniqueIdentifier(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("uniqueidentifier"),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
					}
				);
		}
	}
	
	// Class to put all of the static math operators that will be reused.
	public class MSSQLMath
	{
		public static Expression Truncate(Expression AExpression)
		{
			return new CallExpression("Round", new Expression[]{AExpression, new ValueExpression(0), new ValueExpression(1)});
		}

		public static Expression Frac(Expression AExpression, Expression AExpressionCopy)// note that it takes two different refrences to the same value
		{
			Expression LRounded = new CallExpression("Round", new Expression[]{AExpressionCopy, new ValueExpression(0), new ValueExpression(1)});
			return new BinaryExpression(AExpression, "iSubtraction", LRounded);
		}
	}

	public class MSSQLTimeSpan
	{
		public static Expression ReadMillisecond(Expression AValue)
		{
			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
			Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(1000));
			return MSSQLMath.Truncate(LToTrunc);
		}

		public static Expression ReadSecond(Expression AValue)
		{
			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
			Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
			return MSSQLMath.Truncate(LToTrunc);
		}

		public static Expression ReadMinute(Expression AValue)
		{
			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
			Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
			return MSSQLMath.Truncate(LToTrunc);
		}

		public static Expression ReadHour(Expression AValue)
		{
			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
			Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(24));
			return MSSQLMath.Truncate(LToTrunc);
		}

		public static Expression ReadDay(Expression AValue)
		{
			Expression LToTrunc = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
			return MSSQLMath.Truncate(LToTrunc);
		}
	}

	public class MSSQLDateTimeFunctions
	{
		public static Expression WriteMonth(Expression ADateTime, Expression ADateTimeCopy, Expression APart)
		{
			string LPartString = "mm";
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), ADateTimeCopy});
			Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime});
		}

		public static Expression WriteDay(Expression ADateTime, Expression ADateTimeCopy, Expression APart)//pass the DateTime twice
		{
			string LPartString = "dd";
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), ADateTimeCopy});
			Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime});
		}

		public static Expression WriteYear(Expression ADateTime, Expression ADateTimeCopy, Expression APart)//pass the DateTime twice
		{
			string LPartString = "yyyy";
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), ADateTimeCopy});
			Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime});
		}
	}

	// Operators that MSSQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.

	// Math
	public class MSSQLPower : SQLDeviceOperator
	{
		public MSSQLPower(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CallExpression
				(
					"Power",
					new Expression[]
					{
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
					}
				);
		}
	}

	public class MSSQLTruncate : SQLDeviceOperator
	{
		public MSSQLTruncate(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLMath.Truncate(LValue);
		}
	}

	public class MSSQLFrac : SQLDeviceOperator
	{
		public MSSQLFrac(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LValueCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLMath.Frac(LValue, LValueCopy);
		}
	}

	public class MSSQLLogB : SQLDeviceOperator
	{
		public MSSQLLogB(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LBase = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			LValue = new CallExpression("Log", new Expression[]{LValue});
			LBase = new CallExpression("Log", new Expression[]{LBase});
			return new BinaryExpression(LValue, "iDivision", LBase);
		}
	}

	// TimeSpan
	public class MSSQLTimeSpanReadMillisecond: SQLDeviceOperator
	{
		public MSSQLTimeSpanReadMillisecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadMillisecond(LValue);
		}
	}

	public class MSSQLTimeSpanReadSecond: SQLDeviceOperator
	{
		public MSSQLTimeSpanReadSecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadSecond(LValue);
		}
	}

	public class MSSQLTimeSpanReadMinute: SQLDeviceOperator
	{
		public MSSQLTimeSpanReadMinute(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadMinute(LValue);
		}
	}

	public class MSSQLTimeSpanReadHour: SQLDeviceOperator
	{
		public MSSQLTimeSpanReadHour(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadHour(LValue);
		}
	}

	public class MSSQLTimeSpanReadDay: SQLDeviceOperator
	{
		public MSSQLTimeSpanReadDay(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadDay(LValue);
		}
	}

	public class MSSQLTimeSpanWriteMillisecond : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteMillisecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LFromPart = MSSQLTimeSpan.ReadMillisecond(LTimeSpanCopy);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000));
			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
		}
	}

	public class MSSQLTimeSpanWriteSecond : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteSecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LFromPart = MSSQLTimeSpan.ReadSecond(LTimeSpanCopy);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000000));
			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
		}
	}

	public class MSSQLTimeSpanWriteMinute : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteMinute(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LFromPart = MSSQLTimeSpan.ReadMinute(LTimeSpanCopy);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(600000000));
			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
		}
	}

	public class MSSQLTimeSpanWriteHour : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteHour(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LFromPart = MSSQLTimeSpan.ReadHour(LTimeSpanCopy);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(36000000000));
			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
		}
	}

	public class MSSQLTimeSpanWriteDay : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteDay(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LFromPart = MSSQLTimeSpan.ReadDay(LTimeSpanCopy);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(864000000000));
			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
		}
	}


	public class MSSQLAddMonths : SQLDeviceOperator
	{
		public MSSQLAddMonths(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression("mm", TokenType.Symbol), LMonths, LDateTime});
		}
	}

	public class MSSQLAddYears : SQLDeviceOperator
	{
		public MSSQLAddYears(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression("yyyy", TokenType.Symbol), LMonths, LDateTime});
		}
	}

    public class MSSQLDayOfWeek : SQLDeviceOperator // TODO: do for removal as replaced with Storage.TranslationString in SystemCatalog.d4
	{
		public MSSQLDayOfWeek(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new CallExpression("DatePart", new Expression[]{new ValueExpression("dw", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDayOfYear : SQLDeviceOperator
	{
		public MSSQLDayOfYear(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new CallExpression("DatePart", new Expression[]{new ValueExpression("dy", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDateTimeReadHour : SQLDeviceOperator
	{
		public MSSQLDateTimeReadHour(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new CallExpression("DatePart", new Expression[]{new ValueExpression("hh", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDateTimeReadMinute : SQLDeviceOperator
	{
		public MSSQLDateTimeReadMinute(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new CallExpression("DatePart", new Expression[]{new ValueExpression("mi", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDateTimeReadSecond : SQLDeviceOperator
	{
		public MSSQLDateTimeReadSecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new CallExpression("DatePart", new Expression[]{new ValueExpression("ss", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDateTimeReadMillisecond : SQLDeviceOperator
	{
		public MSSQLDateTimeReadMillisecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new CallExpression("DatePart", new Expression[]{new ValueExpression("ms", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDateTimeWriteMillisecond : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteMillisecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			string LPartString = "ms";
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
		}
	}

	public class MSSQLDateTimeWriteSecond : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteSecond(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			string LPartString = "ss";
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
		}
	}

	public class MSSQLDateTimeWriteMinute : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteMinute(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			string LPartString = "mi";
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
		}
	}

	public class MSSQLDateTimeWriteHour : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteHour(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			string LPartString = "hh";
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
		}
	}

	public class MSSQLDateTimeWriteDay : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteDay(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			return MSSQLDateTimeFunctions.WriteDay(LDateTime, LDateTimeCopy, LPart);
		}
	}

	public class MSSQLDateTimeWriteMonth : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteMonth(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			return MSSQLDateTimeFunctions.WriteMonth(LDateTime, LDateTimeCopy, LPart);
		}
	}

	public class MSSQLDateTimeWriteYear : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteYear(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			return MSSQLDateTimeFunctions.WriteYear(LDateTime, LDateTimeCopy, LPart);
		}
	}

	public class MSSQLDateTimeDatePart : SQLDeviceOperator
	{
		public MSSQLDateTimeDatePart(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LFromConvert = new CallExpression("Convert", new Expression[]{new ValueExpression("Float", TokenType.Symbol), LDateTime});
			Expression LFromMath = new CallExpression("Floor", new Expression[]{LFromConvert});
			return new CallExpression("Convert", new Expression[]{new ValueExpression("DateTime", TokenType.Symbol), LDateTime});
		}
	}

	public class MSSQLDateTimeTimePart : SQLDeviceOperator
	{
		public MSSQLDateTimeTimePart(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			Expression LFromConvert = new CallExpression("Convert", new Expression[]{new ValueExpression("Float", TokenType.Symbol), LDateTime});
			Expression LFromConvertCopy = new CallExpression("Convert", new Expression[]{new ValueExpression("Float", TokenType.Symbol), LDateTime});
			Expression LFromMath = MSSQLMath.Frac(LFromConvert, LFromConvertCopy);
			return new CallExpression("Convert", new Expression[]{new ValueExpression("DateTime", TokenType.Symbol), LDateTime});
		}
	}

	
	
    /// <summary>
    ///  DateTime selector is done by constructing a string representation of the value and converting it to a datetime using style 121 (ODBC Canonical)	
	///  Convert(DateTime, Convert(VarChar, AYear) + '-' + Convert(VarChar, AMonth) + '-' + Convert(VarChar, ADay) + ' ' + Convert(VarChar, AHours) + ':' + Convert(VarChar, AMinutes) + ':' + Convert(VarChar, ASeconds) + '.' + Convert(VarChar, AMilliseconds), 121)
    /// </summary>
    public class MSSQLDateTimeSelector : SQLDeviceOperator
	{
		public MSSQLDateTimeSelector(int AID, string AName) : base(AID, AName) {}
		
		public static Expression DateTimeSelector(Expression AYear, Expression AMonth, Expression ADay, Expression AHours, Expression AMinutes, Expression ASeconds, Expression AMilliseconds)
		{
			Expression LExpression = new BinaryExpression(new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), AYear}), "+", new ValueExpression("-"));
			LExpression = new BinaryExpression(LExpression, "+", new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), AMonth}));
			LExpression = new BinaryExpression(LExpression, "+", new ValueExpression("-"));
			LExpression = new BinaryExpression(LExpression, "+", new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), ADay}));
			if (AHours != null)
			{
				LExpression = new BinaryExpression(LExpression, "+", new ValueExpression(" "));
				LExpression = new BinaryExpression(LExpression, "+", new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), AHours}));
				LExpression = new BinaryExpression(LExpression, "+", new ValueExpression(":"));
				LExpression = new BinaryExpression(LExpression, "+", new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), AMinutes}));
				if (ASeconds != null)
				{
					LExpression = new BinaryExpression(LExpression, "+", new ValueExpression(":"));
					LExpression = new BinaryExpression(LExpression, "+", new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), ASeconds}));
					if (AMilliseconds != null)
					{
						LExpression = new BinaryExpression(LExpression, "+", new ValueExpression("."));
						LExpression = new BinaryExpression(LExpression, "+", new CallExpression("Convert", new Expression[]{new ValueExpression("VarChar", TokenType.Symbol), AMilliseconds}));
					}
				}
			}
			
			return new CallExpression("Convert", new Expression[]{new ValueExpression("DateTime", TokenType.Symbol), LExpression, new ValueExpression(121)});
		}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression[] LArguments = new Expression[APlanNode.Nodes.Count];
			for (int LIndex = 0; LIndex < APlanNode.Nodes.Count; LIndex++)
				LArguments[LIndex] = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[LIndex], false);
			switch (APlanNode.Nodes.Count)
			{
				case 7: return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], LArguments[3], LArguments[4], LArguments[5], LArguments[6]);
				case 6: return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], LArguments[3], LArguments[4], LArguments[5], null);
				case 5: return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], LArguments[3], LArguments[4], null, null);
				case 3: return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], null, null, null, null);
				default: return null;
			}
		}
    }
    #endregion
}
