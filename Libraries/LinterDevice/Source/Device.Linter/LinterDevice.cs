/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Device.Linter
{
	using System;
	using System.Resources;

	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Connection;
	
	/*
		Data Type Mapping ->
		
			DAE Type	|	Oracle Type														|	Translation Handler
			------------|---------------------------------------------------------------------------------------------
			Boolean		|	integer (0 or 1)												|	SQLBoolean
			Byte		|   smallint														|	SQLByte
			SByte		|	smallint														|	SQLSByte
			Short		|	smallint														|	SQLShort
			UShort		|	integer															|	SQLUShort
			Integer		|	integer															|	SQLInteger
			UInteger	|	bigint															|	SQLUInteger
			Long		|	bigint															|	SQLLong
			ULong		|	decimal(20, 0)													|	SQLULong
			Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SQLDecimal
			SQLDateTime	|	datetime														|	SQLDateTime
			TimeSpan	|	bigint															|	SQLTimeSpan
			Money		|	decimal(28, 8)													|	SQLMoney
			Guid		|	char(24)														|	SQLGuid
			String		|	varchar(Storage.Length)											|	SQLString
			//IString		|	varchar(Storage.Length)											|	SQLString
			SQLText		|	clob															|	SQLText
			//SQLIText	|	clob															|	SQLText
			Binary		|	blob															|	SQLImage
			
		Catalog Description ->
			
			This device uses the ODBC catalog views to select the catalog information.  These expressions should
			work against any ODBC compliant source.
			
			The script to create these views in the Linter database is catalog.sql in the dict subdirectory of the
			Linter installation.
	*/

	public class LinterDevice : SQLDevice
	{
		public LinterDevice(int iD, string name) : base(iD, name){}
		
		protected override void RegisterSystemObjectMaps(ServerProcess process)
		{
			base.RegisterSystemObjectMaps(process);

			// Perform system type and operator mapping registration
			ResourceManager resourceManager = new ResourceManager("SystemCatalog", GetType().Assembly);
			#if USEISTRING
			RunScript(AProcess, String.Format(resourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
			#else
			RunScript(process, String.Format(resourceManager.GetString("SystemObjectMaps"), Name, "false"));
			#endif
		}

		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new LinterDeviceSession(this, serverProcess, deviceSessionInfo);
		}

		// DataSource		
		protected string _dataSource = String.Empty;
		public string DataSource
		{
			get { return _dataSource; }
			set { _dataSource = value == null ? String.Empty : value; }
		}
		
		// ShouldIncludeColumn
		public override bool ShouldIncludeColumn(Plan plan, string tableName, string columnName, string domainName)
		{
			switch (domainName.ToLower())
			{
				case "byte":
				case "smallint":
				case "int":
				case "integer":
				case "bigint":
				case "decimal":
				case "numeric":
				case "float": 
				case "date": 
				case "money": 
				case "char":
				case "character":
				case "varchar":
				case "nchar":
				case "nvarchar":
				case "clob": 
				case "blob": return true;
				default: return false;
			}
		}
		
		// FindScalarType
        public override ScalarType FindScalarType(Plan plan, string domainName, int length, D4.MetaData metaData)
        {
			switch (domainName.ToLower())
			{
				case "byte": return plan.DataTypes.SystemByte;
				case "smallint": return plan.DataTypes.SystemShort;
				case "int":
				case "integer": return plan.DataTypes.SystemInteger;
				case "bigint": return plan.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
				case "double":
				case "float": return plan.DataTypes.SystemDecimal;
				case "date": return plan.DataTypes.SystemDateTime;
				case "money": return plan.DataTypes.SystemMoney;
				case "char":
				case "character":
				case "varchar":
				case "nchar":
				case "nvarchar": 
					metaData.Tags.Add(new D4.Tag("Storage.Length", length.ToString()));
					#if USEISTRING
					return IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString;
					#else
					return plan.DataTypes.SystemString;
					#endif
				#if USEISTRING
				case "clob": return (ScalarType)(IsCaseSensitive ? APlan.Catalog[CSQLTextScalarType] : APlan.Catalog[CSQLITextScalarType]);
				#else
				case "clob": return (ScalarType)Compiler.ResolveCatalogIdentifier(plan, SQLTextScalarType, true);
				#endif
				case "blob": return plan.DataTypes.SystemBinary;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, domainName);
			}
        }

		override protected SQLTextEmitter InternalCreateEmitter() { return new DAE.Language.Linter.LinterTextEmitter();	}
        
        public override TableSpecifier GetDummyTableSpecifier()
        {
			SelectExpression selectExpression = new SelectExpression();
			selectExpression.SelectClause = new SelectClause();
			selectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy"));
			selectExpression.FromClause = new CalculusFromClause(new TableSpecifier(new TableExpression("tables")));
			selectExpression.WhereClause = 
				new WhereClause
				(
					new BinaryExpression
					(
						new BinaryExpression
						(
							new QualifiedFieldExpression("table_name"), 
							"iEqual", 
							new ValueExpression("TABLES")
						), 
						"iAnd", 
						new BinaryExpression
						(
							new QualifiedFieldExpression("table_type"), 
							"iEqual", 
							new ValueExpression("VIEW")
						)
					)
				);
			return new TableSpecifier(selectExpression, "dummy");
        }
        
        protected override string GetDeviceTablesExpression(TableVar tableVar)
        {
			return
				String.Format
				(
					@"
						select
								t.table_schem as ""TableSchema"",
								t.table_name as ""TableName"",
								c.column_name as ""ColumnName"",
								c.ordinal_position as ""OrdinalPosition"",
								t.table_name as ""TableTitle"",
								c.column_name as ""ColumnTitle"",
								c.type_name as ""NativeDomainName"",
								c.type_name as ""DomainName"",
								c.column_size as ""Length"",
								case c.nullable when 0 then 0 else 1 end as ""IsNullable"",
								case when c.type_name in ('BLOB', 'CLOB') then 1 else 0 end as ""IsDeferred""
							from
								tables t join columns c
								on t.table_name = c.table_name
							where (t.table_type = 'TABLE' or t.table_type = 'VIEW') {0}
							order by t.table_name, c.ordinal_position
					",
					tableVar == null ? String.Empty : String.Format("and t.table_name = '{0}'", ToSQLIdentifier(tableVar))
				);
        }
        
        protected override string GetDeviceIndexesExpression(TableVar tableVar)
        {
			return
				String.Format
				(
					@"
						select
								t.table_schem as ""TableSchema"",
								t.table_name as ""TableName"",
								i.index_name as ""IndexName"",
								i.column_name as ""ColumnName"",
								c.ordinal_position as ""OrdinalPosition"",
								case i.non_unique when 0 then 1 else 0 end as ""IsUnique"",
								case i.asc_or_desc when 'D' then 1 else 0 end as ""IsDescending""
							from tables t join tablestatistics i on t.table_name = i.table_name
							where t.table_type = 'TABLE' and i.index_name is not null {0}
							order by t.table_name, i.index_name, i.ordinal_position;
					",
					tableVar == null ? String.Empty : String.Format("and t.table_name = '{0}'", ToSQLIdentifier(tableVar))
				);
        }
	}
	
	public class LinterDeviceSession : SQLDeviceSession
	{
		public LinterDeviceSession(LinterDevice device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo){}
		
		public new LinterDevice Device { get { return (LinterDevice)base.Device; } }
		
		protected override SQLConnection InternalCreateConnection()
		{
			// ConnectionClass:
				//  ODBCConnection (default)
			// ConnectionStringBuilderClass
				// LinterADOConnectionStringBuilder (default)

			D4.ClassDefinition classDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						"ODBCConnection.ODBCConnection" : 
						Device.ConnectionClass
				);
			D4.ClassDefinition builderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						"LinterDevice.LinterODBCConnectionStringBuilder" :
						Device.ConnectionStringBuilderClass
				);
			ConnectionStringBuilder connectionStringBuilder = (ConnectionStringBuilder)ServerProcess.CreateObject(builderClass, new object[]{});

			D4.Tags tags = new D4.Tags();
			tags.AddOrUpdate("DataSource", Device.DataSource);
			tags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			tags.AddOrUpdate("Password", DeviceSessionInfo.Password);
				
			tags = connectionStringBuilder.Map(tags);
			Device.GetConnectionParameters(tags, DeviceSessionInfo);
			string connectionString = SQLDevice.TagsToString(tags);
				
			return (SQLConnection)ServerProcess.CreateObject(classDefinition, new object[]{connectionString});
		}
	}

	public class LinterODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public LinterODBCConnectionStringBuilder()
		{
			_legend.AddOrUpdate("DataSource", "DSN");
			_legend.AddOrUpdate("UserName", "UID");
			_legend.AddOrUpdate("Password", "PWD");
		}
	}
}