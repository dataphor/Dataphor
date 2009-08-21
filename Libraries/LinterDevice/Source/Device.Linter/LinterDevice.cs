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
		public LinterDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID){}
		
		protected override void RegisterSystemObjectMaps(ServerProcess AProcess)
		{
			base.RegisterSystemObjectMaps(AProcess);

			// Perform system type and operator mapping registration
			ResourceManager LResourceManager = new ResourceManager("SystemCatalog", GetType().Assembly);
			#if USEISTRING
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
			#else
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, "false"));
			#endif
		}

		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new LinterDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

		// DataSource		
		protected string FDataSource = String.Empty;
		public string DataSource
		{
			get { return FDataSource; }
			set { FDataSource = value == null ? String.Empty : value; }
		}
		
		// ShouldIncludeColumn
		public override bool ShouldIncludeColumn(ServerProcess AProcess, string ATableName, string AColumnName, string ADomainName)
		{
			switch (ADomainName.ToLower())
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
        public override ScalarType FindScalarType(Plan APlan, string ADomainName, int ALength, D4.MetaData AMetaData)
        {
			switch (ADomainName.ToLower())
			{
				case "byte": return APlan.DataTypes.SystemByte;
				case "smallint": return APlan.DataTypes.SystemShort;
				case "int":
				case "integer": return APlan.DataTypes.SystemInteger;
				case "bigint": return APlan.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
				case "double":
				case "float": return APlan.DataTypes.SystemDecimal;
				case "date": return APlan.DataTypes.SystemDateTime;
				case "money": return APlan.DataTypes.SystemMoney;
				case "char":
				case "character":
				case "varchar":
				case "nchar":
				case "nvarchar": 
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
					#if USEISTRING
					return IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString;
					#else
					return APlan.DataTypes.SystemString;
					#endif
				#if USEISTRING
				case "clob": return (ScalarType)(IsCaseSensitive ? APlan.Catalog[CSQLTextScalarType] : APlan.Catalog[CSQLITextScalarType]);
				#else
				case "clob": return (ScalarType)Compiler.ResolveCatalogIdentifier(APlan, CSQLTextScalarType, true);
				#endif
				case "blob": return APlan.DataTypes.SystemBinary;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
			}
        }

		override protected SQLTextEmitter InternalCreateEmitter() { return new DAE.Language.Linter.LinterTextEmitter();	}
        
        public override TableSpecifier GetDummyTableSpecifier()
        {
			SelectExpression LSelectExpression = new SelectExpression();
			LSelectExpression.SelectClause = new SelectClause();
			LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy"));
			LSelectExpression.FromClause = new CalculusFromClause(new TableSpecifier(new TableExpression("tables")));
			LSelectExpression.WhereClause = 
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
			return new TableSpecifier(LSelectExpression, "dummy");
        }
        
        protected override string GetDeviceTablesExpression(TableVar ATableVar)
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
					ATableVar == null ? String.Empty : String.Format("and t.table_name = '{0}'", ToSQLIdentifier(ATableVar))
				);
        }
        
        protected override string GetDeviceIndexesExpression(TableVar ATableVar)
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
					ATableVar == null ? String.Empty : String.Format("and t.table_name = '{0}'", ToSQLIdentifier(ATableVar))
				);
        }
	}
	
	public class LinterDeviceSession : SQLDeviceSession
	{
		public LinterDeviceSession(LinterDevice ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		public new LinterDevice Device { get { return (LinterDevice)base.Device; } }
		
		protected override SQLConnection InternalCreateConnection()
		{
			// ConnectionClass:
				//  ODBCConnection (default)
			// ConnectionStringBuilderClass
				// LinterADOConnectionStringBuilder (default)

			D4.ClassDefinition LClassDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						"ODBCConnection.ODBCConnection" : 
						Device.ConnectionClass
				);
			D4.ClassDefinition LBuilderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						"LinterDevice.LinterODBCConnectionStringBuilder" :
						Device.ConnectionStringBuilderClass
				);
			ConnectionStringBuilder LConnectionStringBuilder = (ConnectionStringBuilder)ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LBuilderClass, new object[]{});

			D4.Tags LTags = new D4.Tags();
			LTags.AddOrUpdate("DataSource", Device.DataSource);
			LTags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);
				
			LTags = LConnectionStringBuilder.Map(LTags);
			Device.GetConnectionParameters(LTags, DeviceSessionInfo);
			string LConnectionString = SQLDevice.TagsToString(LTags);
				
			return (SQLConnection)ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LClassDefinition, new object[]{LConnectionString});
		}
	}

	public class LinterODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public LinterODBCConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("DataSource", "DSN");
			FLegend.AddOrUpdate("UserName", "UID");
			FLegend.AddOrUpdate("Password", "PWD");
		}
	}
}