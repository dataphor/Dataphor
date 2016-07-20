/*
	Alphora Dataphor
	© Copyright 2000-2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;
using ColumnExpression = Alphora.Dataphor.DAE.Language.SQL.ColumnExpression;

/*
	Connectivity ->
		Uses the CacheClient wrapped in the Cache.Connection library by default.
		The Cache system must have the %Service_callin enabled. See the Cache documentation for more
		The InterSystems.Data.CacheClient.dll seems to be the only required dependency (all the rest is E/F related)

	Syntax ->
		Cache SQL is very similar to T-SQL and has many features to support compatibility with and migration from MSSQL.
		However, there are enough features that it does not support, or supports slightly differently to warrant its own device.
		This device was based on the MSSQLDevice, with the primary initial differences being:
			Addition of a PortNumber property to support the port number aspect of connectivity
			Difference device reconciliation expressions, see reconciliation for more detail

	Reconciliation ->
		Cache supports an SQL-based access mode to its underlying structures, but it does not seem to have a complete exposure of
		the data dictionary for that access. What exposure it does have is loosely based on the MSSQL (or perhaps Sybase) system
		tables, but it is incomplete:
			- Incomplete exposure of indexes, many indexes don't have column information, there may be assumptions here that would help resolve some of it
			- No support for indicating a descending index (not sure if this is an underlying limitation, or just a limitation of the exposure)
			- No support for foreign keys
			- No declaration of columns for primary keys (again, there may be some assumptions that we could take advantage of, but the tables don't report indexes for keys in general)
		Another option is to use the connectivity layer's schema information, which appears to work well except for
			- loss of typing information, the underlying database type is mapped to C# (could augment with the catalog tables though)
			- Key declaration is incorrect, the reported key information appears to be derived from the base table(s) involved in the query, 
				but the impact of operations is not accounted for and the result is incorrect key information. This may not matter for schema mining though

	Transactions ->
		Cache does not support the Serializable isolation level, and will throw an exception if it is used
		Cache also does not really support read committed or repeatable read:
			A delete, even in an uncommitted transaction, will always be visible
			Aggregate operations (including distinct) will always be based on the raw data, regardless of transactions
		This means that in general, read uncommitted is the only isolation level that Cache reliably supports
*/

namespace Alphora.Dataphor.DAE.Device.CacheSQL
{
	#region Device

	/// <summary>
	/// SQL-Based Device for Intersystems Cache DBMS
	/// </summary>
	public class CacheSQLDevice : SQLDevice
	{
		public const string MSSQLBinaryScalarType = "SQLDevice.MSSQLBinary";

		protected string _applicationName = "Dataphor Server";
		protected string _databaseName = String.Empty;
		protected string _serverName = String.Empty;
		protected string _portNumber = "1972";

		public CacheSQLDevice(int iD, string name) : base(iD, name)
		{
			UseStatementTerminator = false;
			SupportsOrderByNullsFirstLast = false;
			IsOrderByInContext = false;
			// T-SQL allows items in the order by list to reference the column aliases used in the query
			UseParametersForCursors = true;
			ShouldNormalizeWhitespace = false;
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

		public string PortNumber
		{
			get { return _portNumber; }
			set { _portNumber = value == null ? "1972" : value; }
		}

		protected override void SetMaxIdentifierLength()
		{
			_maxIdentifierLength = 128;
		}

		protected override void RegisterSystemObjectMaps(ServerProcess process)
		{
			base.RegisterSystemObjectMaps(process);

			// Perform system type and operator mapping registration
			using (Stream stream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.SystemCatalog.d4"))
			{
				RunScript(process, String.Format(new StreamReader(stream).ReadToEnd(), Name));
			}
		}

		protected override DeviceSession InternalConnect(ServerProcess serverProcess,
														 DeviceSessionInfo deviceSessionInfo)
		{
			return new CacheSQLDeviceSession(this, serverProcess, deviceSessionInfo);
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
				case "%library.boolean":
				case "tinyint":
				case "%library.tinyint":
				case "smallint":
				case "%library.smallint":
				case "int":
				case "integer":
				case "%library.integer":
				case "bigint":
				case "%library.bigint":
				case "decimal":
				case "numeric":
				case "%library.numer":
				case "float":
				case "%library.float":
				case "real":
				case "%library.double":
				case "datetime":
				case "smalldatetime":
				case "%library.datetime":
				case "money":
				case "smallmoney":
				case "%library.currency":
				case "uniqueidentifier":
				case "%library.uniqueidentifier":
				case "char":
				case "varchar":
				case "nchar":
				case "nvarchar":
				case "%library.string":
				case "text":
				case "ntext":
				case "%stream.globalcharacter":
				case "image":
				case "%string.globalbinary":
				case "binary":
				case "varbinary":
				case "%library.binary":
				case "timestamp":
				case "%library.timestamp":
				case "date": // 2008 (majorVersion >= 10)
				case "%library.date":
				case "time": // 2008
				case "%library.time":
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
				case "%library.boolean":
					return plan.DataTypes.SystemBoolean;
				case "tinyint":
				case "%library.tinyint":
					return plan.DataTypes.SystemByte;
				case "smallint":
				case "%library.smallint":
					return plan.DataTypes.SystemShort;
				case "int":
				case "integer":
				case "%library.integer":
					return plan.DataTypes.SystemInteger;
				case "bigint":
				case "%library.bigint":
					return plan.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
				case "float":
				case "real":
				case "%library.numeric":
				case "%library.float":
				case "%library.double":
					return plan.DataTypes.SystemDecimal;
				case "date":
				case "%library.date":
					return plan.DataTypes.SystemDate;
				case "time":
				case "%library.time":
					return plan.DataTypes.SystemTime;
				case "datetime":
				case "smalldatetime":
				//case "datetime2":
				//case "datetimeoffset":
				case "%library.datetime":
					return plan.DataTypes.SystemDateTime;
				case "money":
				case "smallmoney":
				case "%library.currency":
					return plan.DataTypes.SystemMoney;
				case "uniqueidentifier":
				case "%library.uniqueidentifier":
					return plan.DataTypes.SystemGuid;
				case "char":
				case "varchar":
				case "nchar":
				case "nvarchar":
				case "%library.string":
					metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
					return plan.DataTypes.SystemString;
				case "text":
				case "ntext":
				case "%stream.globalcharacter":
					return (ScalarType) Compiler.ResolveCatalogIdentifier(plan, SQLTextScalarType, true);
				case "binary":
				case "%library.binary":
					metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
					return (ScalarType) Compiler.ResolveCatalogIdentifier(plan, MSSQLBinaryScalarType, true);
				case "timestamp":
				case "%library.timestamp":
					return plan.DataTypes.SystemDateTime;
				case "varbinary":
					metaData.Tags.Add(new Tag("Storage.Length", length.ToString()));
					return plan.DataTypes.SystemBinary;
				case "image":
				case "%stream.globalbinary":
					return plan.DataTypes.SystemBinary;
				default:
					throw new SQLException(SQLException.Codes.UnsupportedImportType, domainName);
			}
		}

		protected override string GetDeviceTablesExpression(TableVar tableVar)
		{
			return
				DeviceTablesExpression != String.Empty
					? base.GetDeviceTablesExpression(tableVar)
					:
						String.Format
						(
							@"
select 
		so.""schema"" as TableSchema,
		so.name as TableName, 
		sc.name as ColumnName, 
		sc.colid as OrdinalPosition,
		so.name as TableTitle, 
		sc.name as ColumnTitle, 
		snt.name as NativeDomainName, 
		st.usertype as DomainName,
		convert(integer, sc.length) as Length,
		convert(bit, 0) as IsNullable,
		case when snt.name in ('%Library.Text', '%Library.BinaryStream') then 1 else 0 end as IsDeferred
	from %TSQL_sys.objects as so
		join %TSQL_sys.columns as sc on sc.id = so.id 
		join %TSQL_sys.types as st on st.usertype = sc.usertype
		join %TSQL_sys.types as snt on st.usertype = snt.name
	where (so.type = 'U' or so.type = 'V')
		{0}
		{1}
	order by so.""schema"", so.name, sc.colid
							",
							Schema == String.Empty
								? String.Empty
								: String.Format(@"and so.""schema"" = '{0}'", Schema), // Don't use ToSQLIdentifier, Schema is a string and will be expressed in the target dialect
							tableVar == null
								? String.Empty
								: String.Format("and so.name = '{0}'", ToSQLIdentifier(tableVar))
							);
		}

		protected override string GetDeviceIndexesExpression(TableVar tableVar)
		{
			return
				DeviceIndexesExpression != String.Empty
					? base.GetDeviceIndexesExpression(tableVar)
					:
						String.Format
						(
							@"
select 
		TableSchema,
		TableName,
		IndexName,
		ColumnName,
		OrdinalPosition,
		IsUnique,
		IsDescending
	from
	(
		select		
				so.""schema"" as TableSchema,
				so.name as TableName, 
				si.name as IndexName, 
				Coalesce(sc1.name, sc2.name, sc3.name, sc4.name, sc5.name, sc6.name, sc7.name, sc8.name) as ColumnName, 
				Coalesce
				(
					case when sk1.id is null then null else 1 end,
					case when sk2.id is null then null else 2 end,
					case when sk3.id is null then null else 3 end,
					case when sk4.id is null then null else 4 end,
					case when sk5.id is null then null else 5 end,
					case when sk6.id is null then null else 6 end,
					case when sk7.id is null then null else 7 end,
					case when sk8.id is null then null else 8 end
				) OrdinalPosition,
				case when (si.status # 4) > 1 then 1 else 0 end IsUnique,
				0 as IsDescending
			from %TSQL_sys.objects as so
				join %TSQL_sys.indexes as si on si.id = so.id
				left join %TSQL_sys.keys sk1 on si.id = sk1.id
					left join %TSQL_sys.columns sc1 on sc1.id = so.id and sc1.colid = sk1.key1
				left join %TSQL_sys.keys sk2 on si.id = sk2.id
					left join %TSQL_sys.columns sc2 on sc2.id = so.id and sc2.colid = sk2.key2
				left join %TSQL_sys.keys sk3 on si.id = sk3.id
					left join %TSQL_sys.columns sc3 on sc3.id = so.id and sc3.colid = sk3.key3
				left join %TSQL_sys.keys sk4 on si.id = sk4.id
					left join %TSQL_sys.columns sc4 on sc4.id = so.id and sc4.colid = sk4.key4
				left join %TSQL_sys.keys sk5 on si.id = sk5.id
					left join %TSQL_sys.columns sc5 on sc5.id = so.id and sc5.colid = sk5.key5
				left join %TSQL_sys.keys sk6 on si.id = sk6.id
					left join %TSQL_sys.columns sc6 on sc6.id = so.id and sc6.colid = sk6.key6
				left join %TSQL_sys.keys sk7 on si.id = sk7.id
					left join %TSQL_sys.columns sc7 on sc7.id = so.id and sc7.colid = sk7.key7
				left join %TSQL_sys.keys sk8 on si.id = sk8.id
					left join %TSQL_sys.columns sc8 on sc8.id = so.id and sc8.colid = sk8.key8
			where (so.type = 'U' or so.type = 'V')
				{0}
				{1}
	) T
	where ColumnName is not null
	order by TableSchema, TableName, IndexName, OrdinalPosition
							",
							Schema == String.Empty
								? String.Empty
								: String.Format(@"and so.""schema"" = '{0}'", Schema), // Don't use ToSQLIdentifier, Schema will already be expressed in the target dialect
							tableVar == null
								? String.Empty
								: String.Format("and so.name = '{0}'", ToSQLIdentifier(tableVar))
						);
		}

		protected override string GetDeviceForeignKeysExpression(TableVar tableVar)
		{
			return 
				DeviceForeignKeysExpression != String.Empty
					? base.GetDeviceForeignKeysExpression(tableVar)
					: String.Empty; // Cannot find a way to get foreign key information from the Cache SQL dictionaries...
		}

		public override void DetermineCursorBehavior(Plan plan, TableNode tableNode)
		{
			base.DetermineCursorBehavior(plan, tableNode);
			// TODO: This will actually only be static if the ADOConnection is used because the DotNet providers do not support a method for obtaining a static cursor.
		}
	}

	public class CacheSQLDeviceSession : SQLDeviceSession
	{
		public CacheSQLDeviceSession(CacheSQLDevice device, ServerProcess serverProcess,
								  DeviceSessionInfo deviceSessionInfo)
			: base(device, serverProcess, deviceSessionInfo)
		{
		}

		public new CacheSQLDevice Device
		{
			get { return (CacheSQLDevice) base.Device; }
		}

		protected override SQLConnection InternalCreateConnection()
		{
			var classDefinition =
				new ClassDefinition
				(
					Device.ConnectionClass == String.Empty
						? "CacheConnection.CacheConnection"
						: Device.ConnectionClass
				);

			var builderClass =
				new ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty
						? "CacheConnection.CacheConnectionStringBuilder"
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
			tags.AddOrUpdate("PortNumber", Device.PortNumber);
			tags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			tags.AddOrUpdate("Password", DeviceSessionInfo.Password);

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
