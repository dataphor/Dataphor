/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Resources;	   
using System.Globalization;

namespace Alphora.Dataphor.DAE.Device.DB2
{
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using DB2 = Alphora.Dataphor.DAE.Language.DB2;
	
	
	/*
		Data Type Mapping ->
		
			DAE Type	|	DB2 Type															|	Translation Handler
			------------|---------------------------------------------------------------------------------------------
			Boolean		|	integer (0 or 1)													|	SQLBoolean
			Byte		|   smallint															|	SQLByte
			SByte		|	smallint															|	SQLSByte
			Short		|	smallint															|	SQLShort
			UShort		|	integer																|	SQLUShort
			Integer		|	integer																|	SQLInteger
			UInteger	|	bigint																|	SQLUInteger
			Long		|	numeric(20, 0)														|	DB2Long
			ULong		|	decimal(20, 0)														|	SQLULong
			Decimal		|	decimal(Storage.Precision, Storage.Scale)							|	SQLDecimal
			TimeSpan	|	numeric(20, 0)														|	DB2TimeSpan
			SQLDateTime	|	timestamp															|	DB2DateTime
			Date		|	date																|	SQLDate
			Time		|	time																|	DB2Time
			Money		|	decimal(28, 8)														|	SQLMoney
			Guid		|	char(24)															|	SQLGuid
			String		|	varchar(Storage.Length)												|	SQLString
			//IString		|	varchar(Storage.Length)												|	SQLString
			SQLText		|	clob(1M)															|	DB2Text
			//SQLIText	|	clob(1M)															|	DB2Text
			Binary		|	blob(1G)															|	SQLBinary
			
	*/
	public class DB2Device : SQLDevice
	{
		public DB2Device(int AID, string AName) : base(AID, AName) {}

		protected override void RegisterSystemObjectMaps(ServerProcess AProcess)
		{
			base.RegisterSystemObjectMaps(AProcess);

			// Perform system scalar type and operator mapping registration
			ResourceManager LResourceManager = new ResourceManager("SystemCatalog", GetType().Assembly);
			#if USEISTRING
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
			#else
			using (Stream LStream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.d4"))
			{
				RunScript(AProcess, String.Format(new StreamReader(LStream).ReadToEnd(), Name, "false", (FProduct == DB2.DB2TextEmitter.ISeries).ToString().ToLower()));
			}
			#endif
		}	

		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new DB2DeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		protected override SQLTextEmitter InternalCreateEmitter() { return new DB2.DB2TextEmitter(); }

		protected override string GetDeviceTablesExpression(TableVar ATableVar)
		{
			switch (Product)
			{
				default:
				case DB2.DB2TextEmitter.UDB:
					return
						String.Format
						(
							DeviceTablesExpression == String.Empty ?
								@"
									select
										c.tabschema as TableSchema,
										c.tabname as TableName,
										c.colname as ColumnName,
										c.colno as OrdinalPosition,
										c.tabname as TableTitle,
										c.colname as ColumnTitle,
										c.typename as NativeDomainName,
										c.typename as DomainName,
										c.length as Length,
										case when c.nulls = 'Y' then 1 else 0 end as IsNullable,
										case when c.typename in ('CLOB', 'BLOB') then 1 else 0 end as IsDeferred
									from syscat.columns c join syscat.tables t on c.tabschema = t.tabschema and c.tabname = t.tabname
									where (t.type = 'T' or t.type = 'V')
										and t.tabschema = {0}
										{1} 
									order by TableSchema, TableName, OrdinalPosition;
								" :
								DeviceTablesExpression,
							Schema == String.Empty ? "current schema" : String.Format(@"'{0}'", Schema),
							ATableVar == null ? String.Empty : String.Format("and t.tabname = '{0}'", ToSQLIdentifier(ATableVar))
						);

				case DB2.DB2TextEmitter.ISeries:
					return
						String.Format
						(
							DeviceTablesExpression == String.Empty ?
								@"select C.TABLE_SCHEMA as ""TableSchema"",
										C.TABLE_NAME as ""TableName"",
										C.COLUMN_NAME as ""ColumnName"",
										C.ORDINAL_POSITION as ""OrdinalPosition"",
										case when T.TABLE_TEXT is null then T.TABLE_NAME else T.TABLE_TEXT end as ""TableTitle"",
										case when C.COLUMN_TEXT is null then C.COLUMN_NAME else C.COLUMN_TEXT end as ""ColumnTitle"",
										C.DATA_TYPE as ""NativeDomainName"",
										C.DATA_TYPE as ""DomainName"",
										C.LENGTH as ""Length"",
										case when C.IS_NULLABLE = 'Y' then 1 else 0 end as ""IsNullable"",
										case when C.DATA_TYPE in ('BLOB', 'CLOB') then 1 else 0 end as ""IsDeferred""
									from QSYS2.SYSTABLES as T 
										join QSYS2.SYSCOLUMNS as C 
											on T.TABLE_SCHEMA = C.TABLE_SCHEMA 
												and T.TABLE_NAME = C.TABLE_NAME
									where T.TABLE_TYPE in ('T', 'P')
										and T.TABLE_SCHEMA = '{0}'
										{1}
									order by ""TableSchema"", ""TableName"", ""OrdinalPosition""
								" :
								DeviceTablesExpression,
							Schema,
							ATableVar == null ? String.Empty : String.Format("and \"TableName\" = '{0}'", ToSQLIdentifier(ATableVar))
						);
			}
		}
		
		protected override string GetDeviceIndexesExpression(TableVar ATableVar)
		{
			switch (Product)
			{
				default:
				case DB2.DB2TextEmitter.UDB: // UDB is the default
					return 
						String.Format
						(
							DeviceIndexesExpression == String.Empty ?
								@"select i.tabschema as TableSchema,
											i.tabname as TableName,
											i.indname as IndexName,
											ic.colname as ColumnName,
											ic.colseq as OrdinalPosition,
											case when i.uniquerule in ('P', 'U') then 1 else 0 end as IsUnique,
											case when ic.colorder = 'D' then 1 else 0 end as IsDescending
										from syscat.indexes i join syscat.tables t on i.tabschema = t.tabschema and i.tabname = t.tabname
											join syscat.indexcoluse ic on i.indschema = ic.indschema and i.indname = ic.indname
										where (t.type = 'T' or t.type = 'V')
											and t.tabschema = {0}
											{1}
										order by TableName, i.iid, OrdinalPosition;
								" :
								DeviceIndexesExpression,
							Schema == String.Empty ? "current schema" : String.Format("'{0}'", Schema),
							ATableVar == null ? String.Empty : String.Format("and t.tabname = '{0}'", ToSQLIdentifier(ATableVar))
						);

				case DB2.DB2TextEmitter.ISeries:
					return 
						String.Format
						(
							DeviceIndexesExpression == String.Empty ?
								@"
									select 
										I.TABLE_SCHEMA as ""TableSchema"",
										I.TABLE_NAME as ""TableName"",
										I.INDEX_NAME as ""IndexName"",
										K.COLUMN_NAME as ""ColumnName"",
										K.ORDINAL_POSITION as ""OrdinalPosition"",
										case when I.IS_UNIQUE = 'U' then 1 else 0 end as ""IsUnique"",
										case when K.ORDERING = 'A' then 0 else 1 end as ""IsDescending""
									from QSYS2.SYSTABLES T
										join QSYS2.SYSINDEXES I
											on T.TABLE_SCHEMA = I.TABLE_SCHEMA
												and T.TABLE_NAME = I.TABLE_NAME
										join QSYS2.SYSKEYS K
											on I.INDEX_SCHEMA = K.INDEX_SCHEMA
												and I.INDEX_NAME = K.INDEX_NAME
									where T.TABLE_TYPE in ('T', 'P')
										and T.TABLE_SCHEMA = '{0}'
										{1}
									union
									select
										K.TABLE_SCHEMA as ""TableSchema"",
										K.TABLE_NAME as ""TableName"",
										K.CONSTRAINT_NAME as ""IndexName"",
										C.COLUMN_NAME as ""ColumnName"",
										C.ORDINAL_POSITION as ""OrdinalPosition"",
										1 as ""IsUnique"",
										0 as ""IsDescending""
									from QSYS2.SYSTABLES T
										join QSYS2.SYSCST K 
											on T.TABLE_SCHEMA = K.TABLE_SCHEMA
												and T.TABLE_NAME = K.TABLE_NAME
										join QSYS2.SYSKEYCST C 
											on K.CONSTRAINT_SCHEMA = C.CONSTRAINT_SCHEMA
												and K.CONSTRAINT_NAME = C.CONSTRAINT_NAME
									where T.TABLE_TYPE in ('T', 'P')
										and K.CONSTRAINT_TYPE = 'PRIMARY KEY'
										and T.TABLE_SCHEMA = '{0}'
										{1}
									order by ""TableSchema"", ""TableName"", ""OrdinalPosition""
								" :
								DeviceIndexesExpression,
							Schema,
							ATableVar == null ? String.Empty : String.Format("and \"TableName\" = '{0}'", ToSQLIdentifier(ATableVar))
						);
			}
		}

		protected override void SetMaxIdentifierLength()
		{
			_maxIdentifierLength = 30;
		}

		private int CMaxIndexNameLength = 18;

		protected override string GetIndexName(string ATableName, Order AOrder)
		{
			return EnsureValidIdentifier(base.GetIndexName(ATableName, AOrder), CMaxIndexNameLength);
		}

		protected override string GetIndexName(string ATableName, Key AKey)
		{
			return EnsureValidIdentifier(base.GetIndexName(ATableName, AKey), CMaxIndexNameLength);
		}
		
		protected string FDataSource = String.Empty;
		public string DataSource
		{
			get { return FDataSource; }
			set { FDataSource = value == null ? String.Empty : value; }
		}
		
		private string FProduct;
		public string Product
		{
			get { return FProduct; }
			set
			{
				FProduct = value;
				// iSeries CAE does not use statement terminators
				// V4R4 does not support nested from clause or sub-selects in the select clause
				if (FProduct == DB2.DB2TextEmitter.ISeries)
				{
					UseStatementTerminator = false;
					SupportsNestedFrom = false;
					SupportsSubSelectInSelectClause = false;
					IsOrderByInContext = false;
					UseValuesClauseInInsert = false;
				}
				else if (FProduct == DB2.DB2TextEmitter.UDB)
				{
					UseTransactions = false;
				}
			}
		}
		
		public override TableSpecifier GetDummyTableSpecifier()
        {
			if (Product == "iSeries")
			{
				SelectExpression LSelectExpression = new SelectExpression();

				LSelectExpression.SelectClause = new SelectClause();
				LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
				LSelectExpression.FromClause = 
					new CalculusFromClause
					(
						new TableSpecifier
						(
							new TableExpression("QSYS2", "SYSTABLES"), 
							"dummy1"
						)
					);
				LSelectExpression.WhereClause = 
					new WhereClause
					(
						new BinaryExpression
						(
							new BinaryExpression
							( 
								new QualifiedFieldExpression("TABLE_OWNER"), 
								"iEqual",
                                new ValueExpression("QSYS", TokenType.String)
							), 
							"iAnd",
							new BinaryExpression
							(
								new QualifiedFieldExpression("TABLE_NAME"),
								"iEqual",
                                new ValueExpression("SYSTABLES", TokenType.String)
							)
						)
					);
							
								
				return new TableSpecifier(LSelectExpression, "dummy1");
			}
			
		    else
				return new TableSpecifier(new TableExpression("SYSIBM", "SYSDUMMY1"));
        }
        
		// FindScalarType
        public override ScalarType FindScalarType(Plan APlan, string ADomainName, int ALength, D4.MetaData AMetaData)
        {
			switch (ADomainName.ToLower().Trim())
			{
				case "smallint": return APlan.DataTypes.SystemShort;
				case "int":
				case "integer": return APlan.DataTypes.SystemInteger;
				case "bigint": return APlan.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
                case "double":
				case "float": return APlan.DataTypes.SystemDecimal;
				case "date": return APlan.DataTypes.SystemDate;
				case "time": return APlan.DataTypes.SystemTime;
				case "timestmp": // note that this is not a correct mapping.  This is a hack for Keystone's AS400 machine.
				case "timestamp": return (ScalarType)APlan.DataTypes.SystemDateTime;
				case "money": return APlan.DataTypes.SystemMoney;
				case "char":
                case "character": 
				case "varchar": 
				case "long varchar":
				case "nchar":
				case "nvarchar": 
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
					#if USEISTRING
					return (IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString);
					#else
					return APlan.DataTypes.SystemString;
					#endif
				#if USEISTRING
				case "clob": return (ScalarType)(IsCaseSensitive ? APlan.Catalog[CSQLTextScalarType] : APlan.Catalog[CSQLITextScalarType]);
				#else
				case "clob": return (ScalarType)Compiler.ResolveCatalogIdentifier(APlan, SQLTextScalarType, true);
				#endif
				case "blob": return APlan.DataTypes.SystemBinary;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
			}
        }
        
        public override string GetParameterMarker(SQLScalarType AScalarType, D4.MetaData AMetaData)
        {
			return String.Format("cast(? as {0})", AScalarType.ParameterDomainName(AMetaData));
        }
	}
	
	public class DB2DeviceSession : SQLDeviceSession
	{
		public DB2DeviceSession(DB2Device ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		public new DB2Device Device { get { return (DB2Device)base.Device; } }

		protected override SQLConnection InternalCreateConnection()
		{
			// ConnectionClass:
				//  ADOConnection
				//  DB2Connection (default)
				//	ODBCConnection
				
			// ConnectionStringBuilderClass:
				//  DB2OLEDBConnectionStringBuilder
				//  DB2DB2ConnectionStringBuilder (default)
				//	DB2ODBCConnectionStringBuilder
			
			/*
			 * Note that when this device is connected via ODBC, and HY011 error can occur
			 * If this happens, ensure that Asynchronus query executes and cursor holding are both disabled
			 * these properties are set in the transaction tab of the advanced settings of the IBM ODBC prog
			 */

			D4.ClassDefinition LClassDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						Device.Product == DB2.DB2TextEmitter.UDB ?
							"DB2Connection.DB2Connection" :
							"DB2400Connection.DB2400Connection" :
						//"ODBCConnection.ODBCConnection" : 
						Device.ConnectionClass
				);

			D4.ClassDefinition LBuilderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						Device.Product == DB2.DB2TextEmitter.UDB ?
							"DB2Device.DB2DB2ConnectionStringBuilder" :
							"DB2Device.DB2DB2400ConnectionStringBuilder" :
						//"DB2Device.DB2ODBCConnectionStringBuilder" :
						Device.ConnectionStringBuilderClass
				);
			ConnectionStringBuilder LConnectionStringBuilder = (ConnectionStringBuilder)ServerProcess.CreateObject(LBuilderClass, new object[]{});

			D4.Tags LTags = new D4.Tags();
			LTags.AddOrUpdate("DataSource", Device.DataSource);
			LTags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);
			if (Device.Schema != String.Empty)
				LTags.AddOrUpdate("Database", Device.Schema);

			LTags = LConnectionStringBuilder.Map(LTags);
			Device.GetConnectionParameters(LTags, DeviceSessionInfo);
			string LConnectionString = SQLDevice.TagsToString(LTags);
				
			return (SQLConnection)ServerProcess.CreateObject(LClassDefinition, new object[]{LConnectionString});
		}
		
		protected override void InternalVerifyInsertStatement(TableVar ATable, IRow ARow, InsertStatement AStatement) 
		{
			SelectExpression LSelectExpression = AStatement.Values as SelectExpression;
			if (LSelectExpression != null)
			{
				if (LSelectExpression.FromClause is CalculusFromClause)
				{
					SelectExpression LNestedSelectExpression = ((CalculusFromClause)LSelectExpression.FromClause).TableSpecifiers[0].TableExpression as SelectExpression;
					if (LNestedSelectExpression != null)
					{
						LSelectExpression.FromClause = new CalculusFromClause(((CalculusFromClause)LNestedSelectExpression.FromClause).TableSpecifiers[0]);
						if (LSelectExpression.WhereClause == null)
							LSelectExpression.WhereClause = LNestedSelectExpression.WhereClause;
						else
						{
							LSelectExpression.WhereClause.Expression =
								new BinaryExpression(LSelectExpression.WhereClause.Expression, "iAnd", LNestedSelectExpression.WhereClause.Expression);
						}
					}
				}
			}
		}
	}
	
	public class DB2DB2ConnectionStringBuilder : ConnectionStringBuilder
	{
		public DB2DB2ConnectionStringBuilder()
		{
			_legend.AddOrUpdate("DataSource", "Server");
			_legend.AddOrUpdate("UserName", "User ID");
			_legend.AddOrUpdate("Password", "Password");
		}
	}
	
	public class DB2DB2400ConnectionStringBuilder : ConnectionStringBuilder
	{
		public DB2DB2400ConnectionStringBuilder()
		{
			_legend.AddOrUpdate("DataSource", "DataSource");
			_legend.AddOrUpdate("Database", "DefaultCollection");
			_legend.AddOrUpdate("UserName", "User ID");
			_legend.AddOrUpdate("Password", "Password");
		}
	}

	public class DB2ODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public DB2ODBCConnectionStringBuilder()
		{
			_legend.AddOrUpdate("DataSource", "DSN");
			_legend.AddOrUpdate("UserName", "UID");
			_legend.AddOrUpdate("Password", "PWD");
		}
	}

	public class DB2OLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public DB2OLEDBConnectionStringBuilder()
		{
			_parameters.AddOrUpdate("Provider", "MSDASQL");
			_legend.AddOrUpdate("DataSource", "DSN");
			_legend.AddOrUpdate("UserName", "UID");
			_legend.AddOrUpdate("Password", "PWD");
		}
	}

	/// <summary>
	/// DB2 type : blob(1G)
	/// D4 type : System.Binary
	/// </summary>
	public class DB2Binary : SQLBinary
	{
		public DB2Binary(int AID, string AName) : base(AID, AName) {}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "blob(1G)";
		}
	}
	
	/// <summary>
	/// DB2 type : blob(1G)
	/// D4 type : System.Graphic
	/// </summary>
	public class DB2Graphic : SQLGraphic
	{
		public DB2Graphic(int AID, string AName) : base(AID, AName) {}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "blob(1G)";
		}
	}
	
	/// <summary>
	/// DB2 type : vargraphic
	/// D4 type : System.Binary
	/// </summary>
	public class DB2400Binary : SQLBinary
	{
		public DB2400Binary(int AID, string AName) : base(AID, AName) {}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "blob(1G)";
		}
	}

	/// <summary>
	/// DB2 type : vargraphic
	/// D4 type : System.Graphic
	/// </summary>
	public class DB2400Graphic : SQLGraphic
	{
		public DB2400Graphic(int AID, string AName) : base(AID, AName) {}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "vargraphic";
		}
	}

	/// <summary>
	/// DB2 type : clob(1M)
	/// D4 type : SQLDevice.SQLText | SQLDevice.SQLIText
	/// </summary>
	public class DB2Text : SQLText
	{
		public DB2Text(int AID, string AName) : base(AID, AName) {}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "clob(1M)";
		}
	}

	// CompareText(ALeftValue, ARightValue) ::= case when UCase(ALeftValue) = UCase(ARightValue) then 0 when UCase(ALeftValue) < UCase(ARightValue) then -1 else 1 end
	public class DB2CompareText : SQLDeviceOperator
	{
		public DB2CompareText(int AID, string AName) : base(AID, AName) {}

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
								new CallExpression("UCase", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)}),
								"iEqual",
								new CallExpression("UCase", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)})
							), 
							new ValueExpression(0)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression("UCase", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)}),
								"iLess",
								new CallExpression("UCase", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)})
							),
							new ValueExpression(-1)
						)
					},
					new CaseElseExpression(new ValueExpression(1))
				);
		}
	}

	// DB2 uses 1 based strings.
	public class DB2Copy : SQLDeviceOperator
	{
		public DB2Copy(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new CallExpression
				(
					"SubStr", 
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

	// IndexOf(AString, ASubString) ::= case when ASubstring = '' then 1 else PosStr(AString, ASubstring) end - 1
	public class DB2IndexOf : SQLDeviceOperator
	{
		public DB2IndexOf(int AID, string AName) : base(AID, AName) {}

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

	// IndexOf(ASubString, AString) ::= case when ASubstring = '' then 1 else PosStr(AString, ASubstring) end - 1
	public class DB2Pos : SQLDeviceOperator
	{
		public DB2Pos(int AID, string AName) : base(AID, AName) {}

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

	public class DB2ToBooleanInt : SQLDeviceOperator
	{
		public DB2ToBooleanInt(int AID, string AName) : base(AID, AName) {}

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
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
								"iEqual",
								new ValueExpression(0, TokenType.Integer)
							),
							new ValueExpression(0, TokenType.Integer)
						)
					},
					new CaseElseExpression
					(
                        new ValueExpression(1, TokenType.Integer)
					)
				);
		}
	}

	public class DB2ToBooleanString : SQLDeviceOperator
	{
		public DB2ToBooleanString(int AID, string AName) : base(AID, AName) {}

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
								new CallExpression
								(
									"LCase",
									new Expression[]
									{
										LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
									}
								),
								"iEqual",
								new ValueExpression("true", TokenType.String)
							),
							new ValueExpression(1, TokenType.Integer)
						)
					},
					new CaseElseExpression
					(
						new CaseExpression
						(
							new CaseItemExpression[]
							{
								new CaseItemExpression
								(
									new BinaryExpression
									(
										new CallExpression
										(
											"LCase",
											new Expression[]
											{
												LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
											}
										),
										"iEqual",
										new ValueExpression("false", TokenType.String)
									),
									new ValueExpression(0, TokenType.Integer)
								)
							},
							new CaseElseExpression
							(
								new CallExpression
								(
									"Integer",
									new Expression[]
									{
										new ValueExpression("", TokenType.String)
									}
								)
							)
						)
					)
				);
		}
	}

	public class DB2ToStringBoolean : SQLDeviceOperator
	{
		public DB2ToStringBoolean(int AID, string AName) : base(AID, AName) {}

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
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
								"iEqual",
								new ValueExpression(0, TokenType.Integer)
							),
							new ValueExpression("false", TokenType.String)
						)
					},
					new CaseElseExpression
					(
                        new ValueExpression("true", TokenType.String)
					)
				);
		}
	}

	public class DB2Long : SQLScalarType
	{
		public DB2Long(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt64(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (decimal)(long)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(8);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "BigInt";
		}
	}
	
//	public class DB2400Long : SQLScalarType
//	{
//		public DB2400Long() : base(){}
//
//		public override Scalar ToScalar(IValueManager AManager, object AValue)
//		{
//			return new Scalar(AProcess, ScalarType, Convert.ToInt64(AValue));
//		}
//
//		public override object FromScalar(Scalar AValue)
//		{
//			return (Double)AValue.AsInt64;
//		}
//
//		public override SQLType GetSQLType(D4.MetaData AMetaData)
//		{
//			//returning SQLNumericType is the correct implementation, however at the current time the translation layer
//			//will not support Decimal therefore we are sending them as a Double which is accomplished by returning SQLFloatType.
//			
//			//return new SQLNumericType(20,0));
//			return new SQLFloatType(2);
//		}
//
//		public override string DomainName(D4.MetaData AMetaData)
//		{
//			return "numeric(20,0)";
//		}
//	}
//	
//	/// <summary>
//	/// SQL type : decimal(28, 8)
//	/// D4 type : System.Decimal
//	/// </summary>
//	public class DB2400Decimal : SQLScalarType
//	{
//		public DB2400Decimal() : base(){}
//
//		public override Scalar ToScalar(IValueManager AManager, object AValue)
//		{
//			// TODO: This should be a decimal cast but the ADOConnection is returning an integer value as the result of evaluating Avg(Integer) when the result is a whole number
//			return new Scalar(AProcess, ScalarType, Convert.ToDecimal(AValue)); 
//		}
//		
//		public override object FromScalar(Scalar AValue)
//		{
//			return (double)AValue.AsDecimal;
//		}
//
//		public byte GetPrecision(D4.MetaData AMetaData)
//		{
//			return Byte.Parse(GetTag("Storage.Precision", "28", AMetaData));
//		}
//		
//		public byte GetScale(D4.MetaData AMetaData)
//		{
//			return Byte.Parse(GetTag("Storage.Scale", "8", AMetaData));
//		}
//		
//		public override SQLType GetSQLType(D4.MetaData AMetaData)
//		{
//			//returning SQLNumericType is the correct implementation, however at the current time the translation layer
//			//will not support Decimal therefore we are sending them as a Double which is accomplished by returning SQLFloatType.
//			
//			//return new SQLNumericType(GetPrecision(AMetaData), GetScale(AMetaData));
//			return new SQLFloatType(2);
//		}
//		
//		public override string DomainName(D4.MetaData AMetaData)
//		{
//			return String.Format("Decimal({0}, {1})", GetPrecision(AMetaData), GetScale(AMetaData)); 
//		}
//	}
//	
//	/// <summary>
//	/// SQL type : decimal(28, 8)
//	/// D4 type : System.Money
//	/// </summary>
//	public class DB2400Money : SQLScalarType
//	{
//		public DB2400Money() : base(){}
//
//		public override Scalar ToScalar(IValueManager AManager, object AValue)
//		{
//			// TODO: This should be a decimal cast but the ADOConnection is returning an integer value as the result of evaluating Avg(Integer) when the result is a whole number
//			return new Scalar(AProcess, ScalarType, Convert.ToDecimal(AValue)); 
//		}
//		
//		public override object FromScalar(Scalar AValue)
//		{
//			return (double)AValue.AsDecimal;
//		}
//
//		public byte GetPrecision(D4.MetaData AMetaData)
//		{
//			return Byte.Parse(GetTag("Storage.Precision", "28", AMetaData));
//		}
//		
//		public byte GetScale(D4.MetaData AMetaData)
//		{
//			return Byte.Parse(GetTag("Storage.Scale", "8", AMetaData));
//		}
//		
//		public override SQLType GetSQLType(D4.MetaData AMetaData)
//		{
//			//returning SQLNumericType is the correct implementation, however at the current time the translation layer
//			//will not support Decimal therefore we are sending them as a Double which is accomplished by returning SQLFloatType.
//			
//			//return new SQLNumericType(GetPrecision(AMetaData), GetScale(AMetaData));
//			return new SQLFloatType(2);
//		}
//		
//		public override string DomainName(D4.MetaData AMetaData)
//		{
//			return String.Format("Decimal({0}, {1})", GetPrecision(AMetaData), GetScale(AMetaData)); 
//		}
//	}
	
	/// <summary>
	/// DB2 type : numeric(20, 0)
	/// D4 type : System.TimeSpan
	/// </summary>
	public class DB2TimeSpan : SQLScalarType
	{
		public DB2TimeSpan(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{										  
			return new TimeSpan(Convert.ToInt64(AValue));
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (TimeSpan)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(8);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "BigInt";
		}
	}
	
//	/// <summary>
//	/// DB2 type : numeric(20, 0)
//	/// D4 type : System.TimeSpan
//	/// </summary>
//	public class DB2400TimeSpan : SQLScalarType
//	{
//		public DB2400TimeSpan() : base(){}
//
//		public override Scalar ToScalar(IValueManager AManager, object AValue)
//		{										  
//			return new Scalar(AProcess, ScalarType, new TimeSpan(Convert.ToInt64(AValue)));
//		}
//		
//		public override object FromScalar(Scalar AValue)
//		{
//			return AValue.AsTimeSpan.Ticks;
//		}
//
//		public override SQLType GetSQLType(D4.MetaData AMetaData)
//		{
//			//returning SQLNumericType is the correct implementation, however at the current time the translation layer
//			//will not support Decimal therefore we are sending them as a Double which is accomplished by returning SQLFloatType.
//				
//			//return new SQLNumericType(20,0));
//			return new SQLFloatType(2);
//		}
//		
//		public override string DomainName(D4.MetaData AMetaData)
//		{
//			return "numeric(20, 0)";
//		}
//	}

	/// <summary> 
	///	DB2 type : TimeStamp
	/// D4 type : SQLDate
	///</summary>	
	public class DB2Date : SQLDate
	{
		public const string CDB2DateFormat = "MM/dd/yyyy";
		
		public DB2Date(int AID, string AName) : base(AID, AName)
		{
			DateFormat = CDB2DateFormat;
		}
	}

	/// <summary>
	/// DB2 type : 
	/// D4 type : SQLDate
	/// </summary>
	public class DB2400Date : SQLScalarType
	{
		public const string CDateFormat = "MM/dd/yyyy";
		
		public DB2400Date(int AID, string AName) : base(AID, AName)  {}

		private string FDateFormat = CDateFormat;
		public string DateFormat
		{
			set { FDateFormat = value; }
			get { return FDateFormat;}
		}
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(DateFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date";
		}
		
		public override object ParameterToScalar(IValueManager AManager, object AValue)
		{
			return DateTime.ParseExact((string)AValue, DateFormat, DateTimeFormatInfo.InvariantInfo);
		}
		
		public override object ParameterFromScalar(IValueManager AManager, object AValue)
		{						
			return ((DateTime)AValue).ToString(DateFormat);
		}

		public override SQLType GetSQLParameterType(D4.MetaData AMetaData)
		{
			return new SQLStringType(10);
		}
		
		public override string ParameterDomainName(D4.MetaData AMetaData)
		{
			return "varchar(10)";
		}
	}

	/// <summary>
	/// DB2 type : timestamp
	/// D4 type : SQLDateTime
	/// </summary>
	public class DB2DateTime : SQLScalarType
	{
		public const string CDateTimeFormat = "yyyy-MM-dd HH24:mm:ss";
		
		public DB2DateTime(int AID, string AName) : base(AID, AName) {}
		
		private string FDateTimeFormat = CDateTimeFormat;
		public string DateTimeFormat
		{
			get { return FDateTimeFormat; }
			set { FDateTimeFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "timestamp";
		}
	}

	/// <summary>
	/// DB2 type : timestamp
	/// D4 type : SQLDateTime
	/// </summary>
	public class DB2400DateTime : SQLScalarType
	{
		public const string CDateTimeFormat = "yyyy-MM-dd-HH.mm.ss";
	
		public DB2400DateTime(int AID, string AName) : base(AID, AName) {}
		
		private string FDateTimeFormat = CDateTimeFormat;
		public string DateTimeFormat
		{
			get { return FDateTimeFormat; }
			set { FDateTimeFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "timestamp";
		}
		
		public override object ParameterToScalar(IValueManager AManager, object AValue)
		{
			return 
				DateTime.ParseExact
				(
					(string)AValue, 
					DateTimeFormat, 
					DateTimeFormatInfo.InvariantInfo
				);
		}
		
		public override object ParameterFromScalar(IValueManager AManager, object AValue)
		{
			return ((DateTime)AValue).ToString(DateTimeFormat);
		}

		public override SQLType GetSQLParameterType(D4.MetaData AMetaData)
		{
			return new SQLStringType(26);
		}
		
		public override string ParameterDomainName(D4.MetaData AMetaData)
		{
			return "varchar(26)";
		}
	}
	
	/// <summary>
	/// SQL type : time
	/// D4 type : Time
	/// </summary>
    public class DB2Time : SQLScalarType
    {
		public const string CTimeFormat = "HH:mm:ss";

		public DB2Time(int AID, string AName) : base(AID, AName) {}
		
		private string FTimeFormat = CTimeFormat;
		public string TimeFormat
		{
			get { return FTimeFormat; }
			set { FTimeFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			DateTime LDateTime = new DateTime(((TimeSpan)AValue).Ticks);
			return new DateTime(1, 1, 1, LDateTime.Hour, LDateTime.Minute, LDateTime.Second, LDateTime.Millisecond);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return new TimeSpan(((DateTime)AValue).Ticks);
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "time";
		}
    }
    
	/// <summary>
	/// SQL type : time
	/// D4 type : Time
	/// </summary>
	public class DB2400Time : SQLScalarType
	{
		public const string CTimeFormat = "HH:mm:ss";

		public DB2400Time(int AID, string AName) : base(AID, AName) {}

		private string FTimeFormat = CTimeFormat;
		public string TimeFormat
		{
			get { return FTimeFormat; }
			set { FTimeFormat = value; }
		}

		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return String.Format("cast(null as {0})", DomainName());
				
			return String.Format("cast('{0}' as {1})", ((DateTime)AValue).ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo), DomainName());
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			DateTime LDateTime = (DateTime)AValue;
			return new DateTime(1, 1, 1, LDateTime.Hour, LDateTime.Minute, LDateTime.Second, LDateTime.Millisecond);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (DateTime)AValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "time";
		}
		
		public override object ParameterToScalar(IValueManager AManager, object AValue)
		{
			return DateTime.ParseExact((string)AValue, TimeFormat, DateTimeFormatInfo.InvariantInfo);
		}
		
		public override object ParameterFromScalar(IValueManager AManager, object AValue)
		{
			return ((DateTime)AValue).ToString(TimeFormat);
		}

		public override SQLType GetSQLParameterType(D4.MetaData AMetaData)
		{
			return new SQLStringType(14);
		}
		
		public override string ParameterDomainName(D4.MetaData AMetaData)
		{
			return "varchar(14)";
		}
	}
}
