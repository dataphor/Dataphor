/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

namespace Alphora.Dataphor.DAE.Device.MySQL
{
	using System;
	using System.IO;
	using System.Text;
	using System.Reflection;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Data;
	using System.Data.SqlTypes;
	using System.Data.SqlClient;
	using System.Resources;

	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Schema;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using MySQL = Alphora.Dataphor.DAE.Language.MySQL;
	using Alphora.Dataphor.DAE.Compiling;
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
		
			DAE Type	|	MySQL Type														|	Translation Handler
			------------|---------------------------------------------------------------------------------------------
			Boolean		|	bit																|	MySQLBoolean
			Byte		|   tinyint															|	MySQLByte
			SByte		|	smallint														|	SQLSByte
			Short		|	smallint														|	SQLShort
			UShort		|	integer															|	SQLUShort
			Integer		|	integer															|	SQLInteger
			UInteger	|	bigint															|	SQLUInteger
			Long		|	bigint															|	SQLLong
			ULong		|	decimal(20, 0)													|	SQLULong
			Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SQLDecimal
			SQLDateTime	|	datetime														|	MySQLDateTime
			Date		|	datetime														|	MySQLDate
			SQLTime		|	datetime														|	MySQLTime
			TimeSpan	|	bigint															|	SQLTimeSpan
			Money		|	decimal(28, 8)													|	SQLMoney
			Guid		|	char(24)														|	SQLGuid
			String		|	varchar(Storage.Length)											|	SQLString
			//IString		|	varchar(Storage.Length)											|	SQLString
			SQLText		|	text															|	MySQLText
			//SQLIText	|	text															|	MySQLText
			Binary		|	blob															|	SQLBinary
	*/

    #region Device

    public class MySQLDevice : SQLDevice
    {        
		public MySQLDevice(int iD, string name) : base(iD, name){}

		protected override void RegisterSystemObjectMaps(ServerProcess process)
		{
			base.RegisterSystemObjectMaps(process);

			// Perform system type and operator mapping registration
			#if USEISTRING
			RunScript(AProcess, String.Format(new ResourceManager("SystemCatalog", GetType().Assembly).GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
			#else
			RunScript(process, String.Format(new ResourceManager("SystemCatalog", GetType().Assembly).GetString("SystemObjectMaps"), Name, "false"));
			#endif
		}
		
		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new MySQLDeviceSession(this, serverProcess, deviceSessionInfo);
		}

        public override TableSpecifier GetDummyTableSpecifier()
        {
			SelectExpression selectExpression = new SelectExpression();
			selectExpression.SelectClause = new SelectClause();
			selectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
			return new TableSpecifier(selectExpression, "dummy1");
        }
        
        // ShouldIncludeColumn
        public override bool ShouldIncludeColumn(Plan plan, string tableName, string columnName, string domainName)
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
				case "double":
				case "real":
				case "datetime":
				case "date":
				case "time":
				case "char":
				case "varchar":
				case "nchar":
				case "nvarchar":
				case "text":
				case "ntext":
				case "blob": 
				case "timestamp": return true;
				default: return false;
			}
        }
        
		// FindScalarType
        public override ScalarType FindScalarType(Plan plan, string domainName, int length, D4.MetaData metaData)
        {
			switch (domainName.ToLower())
			{
				case "bit": return plan.DataTypes.SystemBoolean;
				case "tinyint": return plan.DataTypes.SystemByte;
				case "smallint": return plan.DataTypes.SystemShort;
				case "int": 
				case "integer": return plan.DataTypes.SystemInteger;
				case "bigint": return plan.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
				case "double":
				case "float": 
				case "real": return plan.DataTypes.SystemDecimal;
				case "datetime": return plan.DataTypes.SystemDateTime;
				case "date": return plan.DataTypes.SystemDate;
				case "time":  return plan.DataTypes.SystemTime;
				case "timestamp": return plan.DataTypes.SystemDateTime;
				case "char":
				case "varchar":
				case "nchar":
				case "nvarchar": 
					metaData.Tags.Add(new D4.Tag("Storage.Length", length.ToString()));
					#if USEISTRING
					return IsCaseSensitive ? APlan.DataTypes.SystemString : APlan.DataTypes.SystemIString;
					#else
					return plan.DataTypes.SystemString;
					#endif
				case "text":
				#if USEISTRING
				case "ntext": return (ScalarType)(IsCaseSensitive ? APlan.Catalog[CSQLTextScalarType] : APlan.Catalog[CSQLITextScalarType]);
				#else
				case "ntext": return (ScalarType)Compiler.ResolveCatalogIdentifier(plan, SQLTextScalarType, true);
				#endif
				case "blob": return plan.DataTypes.SystemBinary;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, domainName);
			}
        }
        
		// Emitter
		protected override SQLTextEmitter InternalCreateEmitter() { return new MySQL.MySQLTextEmitter(); }
		
		protected override string GetDeviceTablesExpression(TableVar tableVar)
		{
			throw new Exception("MySQL Device does not support schema import");
		}
		
		protected override string GetDeviceIndexesExpression(TableVar tableVar)
		{
			throw new Exception("MySQL device does not support schema import");
		}
		
        public override void DetermineCursorBehavior(Plan plan, TableNode tableNode)
        {
			base.DetermineCursorBehavior(plan, tableNode);
			// TODO: This will actually only be static if the ADOConnection is used because the DotNet providers do not support a method for obtaining a static cursor.
        }

		// ServerName		
		protected string _serverName = String.Empty;
		public string ServerName
		{
			get { return _serverName; }
			set { _serverName = value == null ? String.Empty : value; }
		}
		
		// DatabaseName		
		protected string _databaseName = String.Empty;
		public string DatabaseName 
		{ 
			get { return _databaseName; } 
			set { _databaseName = value == null ? String.Empty : value; } 
		}
	}
	
    public class MySQLDeviceSession : SQLDeviceSession
    {
		public MySQLDeviceSession(MySQLDevice device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo){}
		
		public new MySQLDevice Device { get { return (MySQLDevice)base.Device; } }

		protected override SQLConnection InternalCreateConnection()
		{
			// ConnectionClass:
				// MySQLConnection (default)
			// ConnectionStringBuilderClass
				// MySQLConnectionStringBuilder (default)

			D4.ClassDefinition classDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						"MySQLConnection.MySQLConnection" : 
						Device.ConnectionClass
				);

			D4.ClassDefinition builderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						"MySQLDevice.MySQLConnectionStringBuilder" :
						Device.ConnectionStringBuilderClass
				);

			ConnectionStringBuilder connectionStringBuilder = (ConnectionStringBuilder)ServerProcess.CreateObject(builderClass, new object[]{});
			
			D4.Tags tags = new D4.Tags();
			tags.AddOrUpdate("ServerName", Device.ServerName);
			tags.AddOrUpdate("DatabaseName", Device.DatabaseName);
			tags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			tags.AddOrUpdate("Password", DeviceSessionInfo.Password);

			tags = connectionStringBuilder.Map(tags);
			Device.GetConnectionParameters(tags, DeviceSessionInfo);
			string connectionString = SQLDevice.TagsToString(tags);				
			return (SQLConnection)ServerProcess.CreateObject(classDefinition, new object[]{connectionString});
		}
    }

    #endregion

    #region Connection Builder

    /// <summary>
	/// This class is the tag translator for MySQL
	/// </summary>
	public class MySQLConnectionStringBuilder : ConnectionStringBuilder
	{
		public MySQLConnectionStringBuilder()
		{
			_legend.AddOrUpdate("ServerName", "Data source");
			_legend.AddOrUpdate("DatabaseName", "Database");
			_legend.AddOrUpdate("UserName", "user id");
			_legend.AddOrUpdate("Password", "password");
		}
		
		public override D4.Tags Map(D4.Tags tags)
		{
			D4.Tags localTags = base.Map(tags);
            D4.Tag tag = localTags.GetTag("IntegratedSecurity");
            if (tag != D4.Tag.None)
            {
                localTags.Remove(tag);
                localTags.AddOrUpdate("Integrated Security", "SSPI");
            }
			return localTags;
		}
	}

    #endregion

    #region Types

    /// <summary>
    /// MySQL type : bit
    ///	D4 type : System.Boolean
    /// 0 = false
    /// 1 = true
    /// </summary>
    public class MySQLBoolean : SQLScalarType
    {
		public MySQLBoolean(int iD, string name) : base(iD, name) {}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			if (tempValue is bool)
				return (bool)tempValue;
			else 
				return (int)tempValue == 0 ? false : true;
		}
		
		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return (bool)tempValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLBooleanType();
		}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "bit";
		}
    }

	/// <summary>
	/// MySQL type : tinyint
	/// D4 type : System.Byte
	/// </summary>
    public class MySQLByte : SQLScalarType
    {
		public MySQLByte(int iD, string name) : base(iD, name) {}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			// According to the docs for the SQLOLEDB provider this is supposed to come back as a byte, but
			// it is coming back as a short, I don't know why, maybe interop?
			return Convert.ToByte((short)tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return (byte)tempValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLIntegerType(1);
		}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "tinyint";
		}
    }
    
	/// <summary>
	/// MySQL type : datetime
	/// D4 Type : DateTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// The ADO connectivity layer seems to be rounding datetime values to the nearest second, even though the server is capable of storing greater precision
	/// </summary>
    public class MySQLDateTime : SQLScalarType
    {
		public static readonly DateTime MinValue = new DateTime(1753, 1, 1);
		public static readonly DateTime Accuracy = new DateTime((long)(TimeSpan.TicksPerMillisecond * 3.33));

		public MySQLDateTime(int iD, string name) : base(iD, name) {}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return (DateTime)tempValue;
		}
		
		public override object FromScalar(IValueManager manager, object tempValue)
		{
			DateTime localTempValue = (DateTime)tempValue;
			if (localTempValue < MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, localTempValue.ToString());
			return localTempValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLDateTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "datetime";
		}
    }

	/// <summary>
	/// MySQL type : datetime
	/// D4 Type : Date
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
    public class MySQLDate : SQLScalarType
    {
		public MySQLDate(int iD, string name) : base(iD, name) {}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return (DateTime)tempValue;
		}
		
		public override object FromScalar(IValueManager manager, object tempValue)
		{
			DateTime localTempValue = (DateTime)tempValue;
			if (localTempValue < MySQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, localTempValue.ToString());
			return localTempValue;
		}
		
		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLDateType();
		}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "datetime";
		}
    }

	/// <summary>
	/// MySQL type : datetime
	/// D4 Type : SQLTime
	/// TSQL datetime values are specified for the range DateTime(1753, 1, 1) to DateTime(9999, 12, 31, 12, 59, 59, 997), accurate to 3.33 milliseconds
	/// </summary>
    public class MySQLTime : SQLScalarType
    {
		public MySQLTime(int iD, string name) : base(iD, name) {}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return (DateTime)tempValue;
		}
		
		public override object FromScalar(IValueManager manager, object tempValue)
		{
			DateTime localTempValue = (DateTime)tempValue;
			if (localTempValue < MySQLDateTime.MinValue)
				throw new SQLException(SQLException.Codes.ValueOutOfRange, ScalarType.Name, localTempValue.ToString());
			return new DateTime(1, 1, 1, localTempValue.Hour, localTempValue.Minute, localTempValue.Second, localTempValue.Millisecond);
		}
		
		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "datetime";
		}
    }

	/// <summary>
	/// MySQL type : text
	/// D4 Type : SQLText, SQLIText
	/// </summary>
    public class MySQLText : SQLText
    {
		public MySQLText(int iD, string name) : base(iD, name) {}
		//public MySQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MySQLText(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "text";
		}
    }
    
    /// <summary>
    /// MySQL type : image
    /// D4 type : Binary
    /// </summary>
    public class MySQLBinary : SQLBinary
    {
		public MySQLBinary(int iD, string name) : base(iD, name) {}
		//public MySQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public MySQLBinary(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

        protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "image";
		}
    }

    #endregion

    #region Operators

    //	public class MySQLToday : SQLDeviceOperator
//	{
//		public MySQLToday() : base(){}
//		public MySQLToday(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToday(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			return new CallExpression("Round", new Expression[]{new CallExpression("GetDate", new Expression[]{}), new ValueExpression(0), new ValueExpression(1)});
//		}
//	}
//
//	public class MySQLCopy : SQLDeviceOperator
//	{
//		public MySQLCopy() : base(){}
//		public MySQLCopy(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLCopy(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return 
//				new CallExpression
//				(
//					"Substring", 
//					new Expression[]
//					{
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false), 
//						new BinaryExpression
//						(
//							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
//							"iAddition",
//							new ValueExpression(1, LexerToken.Integer)
//						), 
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[2], false)
//					}
//				);
//		}
//	}
//
//	// Pos(ASubString, AString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
//	public class MySQLPos : SQLDeviceOperator
//	{
//		public MySQLPos() : base(){}
//		public MySQLPos(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLPos(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return 
//				new BinaryExpression
//				(
//					new CaseExpression
//					(
//						new CaseItemExpression[]
//						{
//							new CaseItemExpression
//							(
//								new BinaryExpression
//								(
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//									"iEqual",
//									new ValueExpression(String.Empty, LexerToken.String)
//								),
//								new ValueExpression(1, LexerToken.Integer)
//							)
//						},
//						new CaseElseExpression
//						(
//							new CallExpression
//							(
//								"CharIndex",
//								new Expression[]
//								{
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
//								}
//							)
//						)
//					),
//					"iSubtraction",
//					new ValueExpression(1, LexerToken.Integer)
//				);
//		}
//	}
//
//	// IndexOf(AString, ASubString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
//	public class MySQLIndexOf : SQLDeviceOperator
//	{
//		public MySQLIndexOf() : base(){}
//		public MySQLIndexOf(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLIndexOf(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return 
//				new BinaryExpression
//				(
//					new CaseExpression
//					(
//						new CaseItemExpression[]
//						{
//							new CaseItemExpression
//							(
//								new BinaryExpression
//								(
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
//									"iEqual",
//									new ValueExpression(String.Empty, LexerToken.String)
//								),
//								new ValueExpression(1, LexerToken.Integer)
//							)
//						},
//						new CaseElseExpression
//						(
//							new CallExpression
//							(
//								"CharIndex",
//								new Expression[]
//								{
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//								}
//							)
//						)
//					),
//					"iSubtraction",
//					new ValueExpression(1, LexerToken.Integer)
//				);
//		}
//	}
//
//	// CompareText(ALeftValue, ARightValue) ::= case when Upper(ALeftValue) = Upper(ARightValue) then 0 when Upper(ALeftValue) < Upper(ARightValue) then -1 else 1 end
//	public class MySQLCompareText : SQLDeviceOperator
//	{
//		public MySQLCompareText() : base(){}
//		public MySQLCompareText(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLCompareText(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return 
//				new CaseExpression
//				(
//					new CaseItemExpression[]
//					{
//						new CaseItemExpression
//						(
//							new BinaryExpression
//							(
//								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)}),
//								"iEqual",
//								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)})
//							), 
//							new ValueExpression(0)
//						),
//						new CaseItemExpression
//						(
//							new BinaryExpression
//							(
//								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)}),
//								"iLess",
//								new CallExpression("Upper", new Expression[]{LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)})
//							),
//							new ValueExpression(-1)
//						)
//					},
//					new CaseElseExpression(new ValueExpression(1))
//				);
//		}
//	}
//	
//	// ToString(AValue) ::= Convert(varchar, AValue)
//	public class MySQLToString : SQLDeviceOperator
//	{
//		public MySQLToString() : base(){}
//		public MySQLToString(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToString(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("varchar"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	public class MySQLToBit : SQLDeviceOperator
//	{
//		public MySQLToBit() : base(){}
//		public MySQLToBit(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToBit(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("bit"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	public class MySQLToTinyInt : SQLDeviceOperator
//	{
//		public MySQLToTinyInt() : base(){}
//		public MySQLToTinyInt(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToTinyInt(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("tinyint"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//
//	// ToByte(AValue) ::= convert(tinyint, AValue & (power(2, 8) - 1))	
//	public class MySQLToByte : SQLDeviceOperator
//	{
//		public MySQLToByte() : base(){}
//		public MySQLToByte(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToByte(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("tinyint"),
//						new BinaryExpression
//						(
//							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//							"iBitwiseAnd",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new ValueExpression(2, LexerToken.Integer),
//										new ValueExpression(8, LexerToken.Integer)
//									}
//								),
//								"iSubtraction",
//								new ValueExpression(1, LexerToken.Integer)
//							)
//						)
//					}
//				);
//		}
//	}
//
//	public class MySQLToSmallInt : SQLDeviceOperator
//	{
//		public MySQLToSmallInt() : base(){}
//		public MySQLToSmallInt(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToSmallInt(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("smallint"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	// ToSByte(AValue) ::= convert(smallint, ((AValue & (power(2, 8) - 1) & ~power(2, 7)) - (power(2, 7) & AValue)))
//	public class MySQLToSByte : SQLDeviceOperator
//	{
//		public MySQLToSByte() : base(){}
//		public MySQLToSByte(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToSByte(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("smallint"),
//						new BinaryExpression
//						(
//							new BinaryExpression
//							(
//								new BinaryExpression
//								(
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//									"iBitwiseAnd",
//									new BinaryExpression
//									(
//										new CallExpression
//										(
//											"Power",
//											new Expression[]
//											{
//												new ValueExpression(2, LexerToken.Integer),
//												new ValueExpression(8, LexerToken.Integer)
//											}
//										),
//										"iSubtraction",
//										new ValueExpression(1, LexerToken.Integer)
//									)
//								),
//								"iBitwiseAnd",
//								new UnaryExpression
//								(
//									"iBitwiseNot",
//									new CallExpression
//									(
//										"Power",
//										new Expression[]
//										{
//											new ValueExpression(2, LexerToken.Integer),
//											new ValueExpression(7, LexerToken.Integer)
//										}
//									)
//								)
//							),
//							"iSubtraction",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new ValueExpression(2, LexerToken.Integer),
//										new ValueExpression(7, LexerToken.Integer)
//									}
//								),
//								"iBitwiseAnd",
//								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//							)
//						)
//					}
//				);
//		}
//	}
//
//	// ToShort(AValue) ::= convert(smallint, ((AValue & (power(2, 16) - 1) & ~power(2, 15)) - (power(2, 15) & AValue)))
//	public class MySQLToShort : SQLDeviceOperator
//	{
//		public MySQLToShort() : base(){}
//		public MySQLToShort(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToShort(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("smallint"),
//						new BinaryExpression
//						(
//							new BinaryExpression
//							(
//								new BinaryExpression
//								(
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//									"iBitwiseAnd",
//									new BinaryExpression
//									(
//										new CallExpression
//										(
//											"Power",
//											new Expression[]
//											{
//												new ValueExpression(2, LexerToken.Integer),
//												new ValueExpression(16, LexerToken.Integer)
//											}
//										),
//										"iSubtraction",
//										new ValueExpression(1, LexerToken.Integer)
//									)
//								),
//								"iBitwiseAnd",
//								new UnaryExpression
//								(
//									"iBitwiseNot",
//									new CallExpression
//									(
//										"Power",
//										new Expression[]
//										{
//											new ValueExpression(2, LexerToken.Integer),
//											new ValueExpression(15, LexerToken.Integer)
//										}
//									)
//								)
//							),
//							"iSubtraction",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new ValueExpression(2, LexerToken.Integer),
//										new ValueExpression(15, LexerToken.Integer)
//									}
//								),
//								"iBitwiseAnd",
//								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//							)
//						)
//					}
//				);
//		}
//	}
//
//	public class MySQLToInt : SQLDeviceOperator
//	{
//		public MySQLToInt() : base(){}
//		public MySQLToInt(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToInt(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("int"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	// ToUShort(AValue) ::= convert(int, AValue & (power(2, 16) - 1))	
//	public class MySQLToUShort : SQLDeviceOperator
//	{
//		public MySQLToUShort() : base(){}
//		public MySQLToUShort(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToUShort(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("int"),
//						new BinaryExpression
//						(
//							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//							"iBitwiseAnd",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new ValueExpression(2, LexerToken.Integer),
//										new ValueExpression(16, LexerToken.Integer)
//									}
//								),
//								"iSubtraction",
//								new ValueExpression(1, LexerToken.Integer)
//							)
//						)
//					}
//				);
//		}
//	}
//	
//	// ToInteger(AValue) ::= convert(int, ((AValue & ((power(convert(bigint, 2), 32) - 1) & ~(power(convert(bigint, 2), 31)) - (power(convert(bigint, 2), 31) & AValue)))
//	public class MySQLToInteger : SQLDeviceOperator
//	{
//		public MySQLToInteger() : base(){}
//		public MySQLToInteger(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToInteger(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("int"),
//						new BinaryExpression
//						(
//							new BinaryExpression
//							(
//								new BinaryExpression
//								(
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//									"iBitwiseAnd",
//									new BinaryExpression
//									(
//										new CallExpression
//										(
//											"Power",
//											new Expression[]
//											{
//												new CallExpression
//												(
//													"Convert",
//													new Expression[]
//													{
//														new IdentifierExpression("bigint"),
//														new ValueExpression(2, LexerToken.Integer),
//													}
//												),
//												new ValueExpression(32, LexerToken.Integer)
//											}
//										),
//										"iSubtraction",
//										new ValueExpression(1, LexerToken.Integer)
//									)
//								),
//								"iBitwiseAnd",
//								new UnaryExpression
//								(
//									"iBitwiseNot",
//									new CallExpression
//									(
//										"Power",
//										new Expression[]
//										{
//											new CallExpression
//											(
//												"Convert",
//												new Expression[]
//												{
//													new IdentifierExpression("bigint"),
//													new ValueExpression(2, LexerToken.Integer)
//												}
//											),
//											new ValueExpression(31, LexerToken.Integer)
//										}
//									)
//								)
//							),
//							"iSubtraction",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new CallExpression
//										(
//											"Convert",
//											new Expression[]
//											{
//												new IdentifierExpression("bigint"),
//												new ValueExpression(2, LexerToken.Integer)
//											}
//										),
//										new ValueExpression(31, LexerToken.Integer)
//									}
//								),
//								"iBitwiseAnd",
//								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//							)
//						)
//					}
//				);
//		}
//	}
//
//	public class MySQLToBigInt : SQLDeviceOperator
//	{
//		public MySQLToBigInt() : base(){}
//		public MySQLToBigInt(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToBigInt(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("bigint"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	// ToUInteger(AValue) ::= convert(bigint, AValue & (power(convert(bigint, 2), 32) - 1))	
//	public class MySQLToUInteger : SQLDeviceOperator
//	{
//		public MySQLToUInteger() : base(){}
//		public MySQLToUInteger(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToUInteger(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("bigint"),
//						new BinaryExpression
//						(
//							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//							"iBitwiseAnd",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new CallExpression
//										(
//											"Convert",
//											new Expression[]
//											{
//												new IdentifierExpression("bigint"),
//												new ValueExpression(2, LexerToken.Integer)
//											}
//										),
//										new ValueExpression(32, LexerToken.Integer)
//									}
//								),
//								"iSubtraction",
//								new ValueExpression(1, LexerToken.Integer)
//							)
//						)
//					}
//				);
//		}
//	}
//	
//	// ToLong(AValue) ::= convert(bigint, ((AValue & ((power(2, 64) * 1) - 1) & ~power(2, 63)) - (power(2, 63) & AValue)))
//	public class MySQLToLong : SQLDeviceOperator
//	{
//		public MySQLToLong() : base(){}
//		public MySQLToLong(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToLong(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("bigint"),
//						new BinaryExpression
//						(
//							new BinaryExpression
//							(
//								new BinaryExpression
//								(
//									LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//									"iBitwiseAnd",
//									new BinaryExpression
//									(
//										new BinaryExpression
//										(
//											new CallExpression
//											(
//												"Power",
//												new Expression[]
//												{
//													new ValueExpression(2, LexerToken.Integer),
//													new ValueExpression(64, LexerToken.Integer)
//												}
//											),
//											"iMultiplication",
//											new ValueExpression(1, LexerToken.Integer)
//										),
//										"iSubtraction",
//										new ValueExpression(1, LexerToken.Integer)
//									)
//								),
//								"iBitwiseAnd",
//								new UnaryExpression
//								(
//									"iBitwiseNot",
//									new CallExpression
//									(
//										"Power",
//										new Expression[]
//										{
//											new ValueExpression(2, LexerToken.Integer),
//											new ValueExpression(63, LexerToken.Integer)
//										}
//									)
//								)
//							),
//							"iSubtraction",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new ValueExpression(2, LexerToken.Integer),
//										new ValueExpression(63, LexerToken.Integer)
//									}
//								),
//								"iBitwiseAnd",
//								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//							)
//						)
//					}
//				);
//		}
//	}
//
//	public class MySQLToDecimal20 : SQLDeviceOperator
//	{
//		public MySQLToDecimal20() : base(){}
//		public MySQLToDecimal20(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToDecimal20(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("decimal(20, 0)"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	public class MySQLToDecimal288 : SQLDeviceOperator
//	{
//		public MySQLToDecimal288() : base(){}
//		public MySQLToDecimal288(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToDecimal288(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("decimal(28, 8)"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	// ToULong(AValue) ::= convert(decimal(20, 0), AValue & (power(2, 64) - 1))	
//	public class MySQLToULong : SQLDeviceOperator
//	{
//		public MySQLToULong() : base(){}
//		public MySQLToULong(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToULong(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("decimal(20, 0)"),
//						new BinaryExpression
//						(
//							LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//							"iBitwiseAnd",
//							new BinaryExpression
//							(
//								new CallExpression
//								(
//									"Power",
//									new Expression[]
//									{
//										new ValueExpression(2, LexerToken.Integer),
//										new ValueExpression(64, LexerToken.Integer)
//									}
//								),
//								"iSubtraction",
//								new ValueExpression(1, LexerToken.Integer)
//							)
//						)
//					}
//				);
//		}
//	}
//	
//	public class MySQLToDecimal : SQLDeviceOperator
//	{
//		public MySQLToDecimal() : base(){}
//		public MySQLToDecimal(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToDecimal(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("decimal"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	public class MySQLToMoney : SQLDeviceOperator
//	{
//		public MySQLToMoney() : base(){}
//		public MySQLToMoney(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToMoney(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("money"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	public class MySQLToUniqueIdentifier : SQLDeviceOperator
//	{
//		public MySQLToUniqueIdentifier() : base(){}
//		public MySQLToUniqueIdentifier(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLToUniqueIdentifier(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Convert",
//					new Expression[]
//					{
//						new IdentifierExpression("uniqueidentifier"),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
//					}
//				);
//		}
//	}
//	
//	// Class to put all of the static math operators that will be reused.
//	public class MySQLMath
//	{
//		public static Expression Truncate(Expression AExpression)
//		{
//			return new CallExpression("Round", new Expression[]{AExpression, new ValueExpression(0), new ValueExpression(1)});
//		}
//
//		public static Expression Frac(Expression AExpression, Expression AExpressionCopy)// note that it takes two different refrences to the same value
//		{
//			Expression LRounded = new CallExpression("Round", new Expression[]{AExpressionCopy, new ValueExpression(0), new ValueExpression(1)});
//			return new BinaryExpression(AExpression, "iSubtraction", LRounded);
//		}
//	}
//
//	public class MySQLTimeSpan
//	{
//
//		public static Expression ReadMillisecond(Expression AValue)
//		{
//			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
//			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
//			Expression LFromFrac = MySQLMath.Frac(LToFrac, LToFracCopy);
//			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(1000));
//			return MySQLMath.Truncate(LToTrunc);
//		}
//
//		public static Expression ReadSecond(Expression AValue)
//		{
//			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
//			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
//			Expression LFromFrac = MySQLMath.Frac(LToFrac, LToFracCopy);
//			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
//			return MySQLMath.Truncate(LToTrunc);
//		}
//
//		public static Expression ReadMinute(Expression AValue)
//		{
//			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
//			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
//			Expression LFromFrac = MySQLMath.Frac(LToFrac, LToFracCopy);
//			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
//			return MySQLMath.Truncate(LToTrunc);
//		}
//
//		public static Expression ReadHour(Expression AValue)
//		{
//			Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
//			Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
//			Expression LFromFrac = MySQLMath.Frac(LToFrac, LToFracCopy);
//			Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(24));
//			return MySQLMath.Truncate(LToTrunc);
//		}
//
//		public static Expression ReadDay(Expression AValue)
//		{
//			Expression LToTrunc = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
//			return MySQLMath.Truncate(LToTrunc);
//		}
//	}
//
//	public class MySQLDateTimeFunctions
//	{
//		public static Expression WriteMonth(Expression ADateTime, Expression ADateTimeCopy, Expression APart)
//		{
//			string LPartString = "mm";
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), ADateTimeCopy});
//			Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, ADateTime});
//
//		}
//
//		public static Expression WriteDay(Expression ADateTime, Expression ADateTimeCopy, Expression APart)//pass the DateTime twice
//		{
//			string LPartString = "dd";
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), ADateTimeCopy});
//			Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, ADateTime});
//		}
//		public static Expression WriteYear(Expression ADateTime, Expression ADateTimeCopy, Expression APart)//pass the DateTime twice
//		{
//			string LPartString = "yyyy";
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), ADateTimeCopy});
//			Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, ADateTime});
//		}
//
//
//	}
//
//
//	// Operators that MySQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.
//
//	// Math
//	public class MySQLPower : SQLDeviceOperator
//	{
//		public MySQLPower() : base(){}
//		public MySQLPower(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLPower(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			return
//				new CallExpression
//				(
//					"Power",
//					new Expression[]
//					{
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
//						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
//					}
//				);
//		}
//	}
//
//	public class MySQLTruncate : SQLDeviceOperator
//	{
//		public MySQLTruncate() : base(){}
//		public MySQLTruncate(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTruncate(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLMath.Truncate(LValue);
//		}
//	}
//
//	public class MySQLFrac : SQLDeviceOperator
//	{
//		public MySQLFrac() : base(){}
//		public MySQLFrac(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLFrac(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LValueCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLMath.Frac(LValue, LValueCopy);
//		}
//	}
//
//	public class MySQLLogB : SQLDeviceOperator
//	{
//		public MySQLLogB() : base(){}
//		public MySQLLogB(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLLogB(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LBase = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			LValue = new CallExpression("Log", new Expression[]{LValue});
//			LBase = new CallExpression("Log", new Expression[]{LBase});
//			return new BinaryExpression(LValue, "iDivision", LBase);
//		}
//	}
//
//	// TimeSpan
//	public class MySQLTimeSpanReadMillisecond: SQLDeviceOperator
//	{
//		public MySQLTimeSpanReadMillisecond() : base(){}
//		public MySQLTimeSpanReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLTimeSpan.ReadMillisecond(LValue);
//		}
//	}
//
//	public class MySQLTimeSpanReadSecond: SQLDeviceOperator
//	{
//		public MySQLTimeSpanReadSecond() : base(){}
//		public MySQLTimeSpanReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLTimeSpan.ReadSecond(LValue);
//		}
//	}
//
//	public class MySQLTimeSpanReadMinute: SQLDeviceOperator
//	{
//		public MySQLTimeSpanReadMinute() : base(){}
//		public MySQLTimeSpanReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLTimeSpan.ReadMinute(LValue);
//		}
//	}
//
//	public class MySQLTimeSpanReadHour: SQLDeviceOperator
//	{
//		public MySQLTimeSpanReadHour() : base(){}
//		public MySQLTimeSpanReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLTimeSpan.ReadHour(LValue);
//		}
//	}
//
//	public class MySQLTimeSpanReadDay: SQLDeviceOperator
//	{
//		public MySQLTimeSpanReadDay() : base(){}
//		public MySQLTimeSpanReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return MySQLTimeSpan.ReadDay(LValue);
//		}
//	}
//
//	public class MySQLTimeSpanWriteMillisecond : SQLDeviceOperator
//	{
//		public MySQLTimeSpanWriteMillisecond() : base(){}
//		public MySQLTimeSpanWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LFromPart = MySQLTimeSpan.ReadMillisecond(LTimeSpanCopy);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
//			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000));
//			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
//		}
//	}
//
//	public class MySQLTimeSpanWriteSecond : SQLDeviceOperator
//	{
//		public MySQLTimeSpanWriteSecond() : base(){}
//		public MySQLTimeSpanWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LFromPart = MySQLTimeSpan.ReadSecond(LTimeSpanCopy);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
//			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000000));
//			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
//		}
//	}
//
//	public class MySQLTimeSpanWriteMinute : SQLDeviceOperator
//	{
//		public MySQLTimeSpanWriteMinute() : base(){}
//		public MySQLTimeSpanWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LFromPart = MySQLTimeSpan.ReadMinute(LTimeSpanCopy);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
//			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(600000000));
//			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
//		}
//	}
//
//	public class MySQLTimeSpanWriteHour : SQLDeviceOperator
//	{
//		public MySQLTimeSpanWriteHour() : base(){}
//		public MySQLTimeSpanWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LFromPart = MySQLTimeSpan.ReadHour(LTimeSpanCopy);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
//			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(36000000000));
//			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
//		}
//	}
//
//	public class MySQLTimeSpanWriteDay : SQLDeviceOperator
//	{
//		public MySQLTimeSpanWriteDay() : base(){}
//		public MySQLTimeSpanWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLTimeSpanWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LFromPart = MySQLTimeSpan.ReadDay(LTimeSpanCopy);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
//			LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(864000000000));
//			return new BinaryExpression(LTimeSpan, "iAddition", LPart);
//		}
//	}
//
//
//	public class MySQLAddMonths : SQLDeviceOperator
//	{
//		public MySQLAddMonths() : base(){}
//		public MySQLAddMonths(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLAddMonths(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression("mm", LexerToken.Symbol), LMonths, LDateTime});
//		}
//	}
//
//	public class MySQLAddYears : SQLDeviceOperator
//	{
//		public MySQLAddYears() : base(){}
//		public MySQLAddYears(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLAddYears(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression("yyyy", LexerToken.Symbol), LMonths, LDateTime});
//		}
//	}
//
//	public class MySQLDayOfWeek : SQLDeviceOperator
//	{
//		public MySQLDayOfWeek() : base(){}
//		public MySQLDayOfWeek(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDayOfWeek(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return new CallExpression("DatePart", new Expression[]{new ValueExpression("dw", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDayOfYear : SQLDeviceOperator
//	{
//		public MySQLDayOfYear() : base(){}
//		public MySQLDayOfYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDayOfYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return new CallExpression("DatePart", new Expression[]{new ValueExpression("dy", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeReadHour : SQLDeviceOperator
//	{
//		public MySQLDateTimeReadHour() : base(){}
//		public MySQLDateTimeReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return new CallExpression("DatePart", new Expression[]{new ValueExpression("hh", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeReadMinute : SQLDeviceOperator
//	{
//		public MySQLDateTimeReadMinute() : base(){}
//		public MySQLDateTimeReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return new CallExpression("DatePart", new Expression[]{new ValueExpression("mi", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeReadSecond : SQLDeviceOperator
//	{
//		public MySQLDateTimeReadSecond() : base(){}
//		public MySQLDateTimeReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return new CallExpression("DatePart", new Expression[]{new ValueExpression("ss", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeReadMillisecond : SQLDeviceOperator
//	{
//		public MySQLDateTimeReadMillisecond() : base(){}
//		public MySQLDateTimeReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			return new CallExpression("DatePart", new Expression[]{new ValueExpression("ms", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeWriteMillisecond : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteMillisecond() : base(){}
//		public MySQLDateTimeWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			string LPartString = "ms";
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LDateTime});
//			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeWriteSecond : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteSecond() : base(){}
//		public MySQLDateTimeWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			string LPartString = "ss";
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LDateTime});
//			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeWriteMinute : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteMinute() : base(){}
//		public MySQLDateTimeWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			string LPartString = "mi";
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LDateTime});
//			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeWriteHour : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteHour() : base(){}
//		public MySQLDateTimeWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			string LPartString = "hh";
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			Expression LOldPart = new CallExpression("DatePart", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LDateTime});
//			LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression(LPartString, LexerToken.Symbol), LParts, LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeWriteDay : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteDay() : base(){}
//		public MySQLDateTimeWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			return MySQLDateTimeFunctions.WriteDay(LDateTime, LDateTimeCopy, LPart);
//		}
//	}
//
//	public class MySQLDateTimeWriteMonth : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteMonth() : base(){}
//		public MySQLDateTimeWriteMonth(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteMonth(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			return MySQLDateTimeFunctions.WriteMonth(LDateTime, LDateTimeCopy, LPart);
//		}
//	}
//
//	public class MySQLDateTimeWriteYear : SQLDeviceOperator
//	{
//		public MySQLDateTimeWriteYear() : base(){}
//		public MySQLDateTimeWriteYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeWriteYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
//			return MySQLDateTimeFunctions.WriteYear(LDateTime, LDateTimeCopy, LPart);
//		}
//	}
//
//	public class MySQLDateTimeDatePart : SQLDeviceOperator
//	{
//		public MySQLDateTimeDatePart() : base(){}
//		public MySQLDateTimeDatePart(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeDatePart(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LFromConvert = new CallExpression("Convert", new Expression[]{new ValueExpression("Float", LexerToken.Symbol), LDateTime});
//			Expression LFromMath = new CallExpression("Floor", new Expression[]{LFromConvert});
//			return new CallExpression("Convert", new Expression[]{new ValueExpression("DateTime", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeTimePart : SQLDeviceOperator
//	{
//		public MySQLDateTimeTimePart() : base(){}
//		public MySQLDateTimeTimePart(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeTimePart(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
//			Expression LFromConvert = new CallExpression("Convert", new Expression[]{new ValueExpression("Float", LexerToken.Symbol), LDateTime});
//			Expression LFromConvertCopy = new CallExpression("Convert", new Expression[]{new ValueExpression("Float", LexerToken.Symbol), LDateTime});
//			Expression LFromMath = MySQLMath.Frac(LFromConvert, LFromConvertCopy);
//			return new CallExpression("Convert", new Expression[]{new ValueExpression("DateTime", LexerToken.Symbol), LDateTime});
//		}
//	}
//
//	public class MySQLDateTimeSelector : SQLDeviceOperator
//	{
//		public MySQLDateTimeSelector() : base(){}
//		public MySQLDateTimeSelector(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
//		public MySQLDateTimeSelector(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
//
//		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
//		{
//			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
//			Expression[] LArguments = new Expression[]{new ValueExpression(1), new ValueExpression(1), new ValueExpression(1), new ValueExpression(0), new ValueExpression(0), new ValueExpression(0), new ValueExpression(0)};
//			for (int i = 0; i < APlanNode.Nodes.Count; i++)
//			{
//				LArguments[i] = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[i], false);
//			}
//			Expression LYear = MySQLDateTimeFunctions.WriteYear(new ValueExpression("'10/3/1980'", LexerToken.Symbol),new ValueExpression("'10/3/1980'", LexerToken.Symbol), LArguments[0]); //the date being passed doesn't matter, it just has to be the dame date with no time refrence
//			Expression LYearCopy = MySQLDateTimeFunctions.WriteYear(new ValueExpression("'10/3/1980'", LexerToken.Symbol),new ValueExpression("'10/3/1980'", LexerToken.Symbol), LArguments[0]);
//			Expression LMonth = MySQLDateTimeFunctions.WriteMonth(LYear, LYearCopy, LArguments[1]);
//			LYear = MySQLDateTimeFunctions.WriteYear(new ValueExpression("'10/3/1980'", LexerToken.Symbol),new ValueExpression("'10/3/1980'", LexerToken.Symbol), LArguments[0]);
//			LYearCopy = MySQLDateTimeFunctions.WriteYear(new ValueExpression("'10/3/1980'", LexerToken.Symbol),new ValueExpression("'10/3/1980'", LexerToken.Symbol), LArguments[0]);
//			Expression LMonthCopy = MySQLDateTimeFunctions.WriteMonth(LYear, LYearCopy, LArguments[1]);
//			Expression LDay = MySQLDateTimeFunctions.WriteDay(LMonth, LMonthCopy, LArguments[2]);
//			Expression LHour = new CallExpression("DateAdd", new Expression[]{new ValueExpression("hh", LexerToken.Symbol), LArguments[3], LDay});
//			Expression LMinute = new CallExpression("DateAdd", new Expression[]{new ValueExpression("mi", LexerToken.Symbol), LArguments[4], LHour});
//			Expression LSecond = new CallExpression("DateAdd", new Expression[]{new ValueExpression("ss", LexerToken.Symbol), LArguments[4], LMinute});
//			return new CallExpression("DateAdd", new Expression[]{new ValueExpression("ms", LexerToken.Symbol), LArguments[4], LSecond});
//		}
//	}
//	
	public class EnsureOperatorDDL
	{
		public EnsureOperatorDDL(string dropStatement, string createStatement) : base()
		{
			DropStatement = dropStatement;
			CreateStatement = createStatement;
		}
		
		public string DropStatement;
		public string CreateStatement;
	}
	
	public class MySQLDDL
	{
		public static string CEnsureDatabase =
			@"create database if not exists {0}";
			
		public static ArrayList EnsureOperatorDDLCommands = new ArrayList();
		
		static MySQLDDL()
		{
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_Trunc'))
//				drop function DAE_Trunc
//					",
//					@"
//			create function DAE_Trunc(@Value decimal(28,8))
//			returns decimal(28,8)
//			begin
//				return Round(@Value,0,1)
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_Frac'))
//				drop function DAE_Frac
//					",
//					@"
//			create function DAE_Frac(@Value decimal(28,8))
//			returns decimal(28,8)
//			begin
//				return (@Value - Round(@Value,0,1))
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_LogB'))
//				drop function DAE_LogB
//					",
//					@"
//			create function DAE_LogB(@Value decimal(28,8), @Base decimal(28,8))
//			returns decimal(28,8)
//			begin
//				return (Log(@Value) / Log(@Base))
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_Factorial'))
//				drop function DAE_Factorial
//					",
//					@"
//			create function DAE_Factorial(@Value int)
//			returns int
//			begin
//			declare @LReturnVal int;
//			declare @i int;
//			set @LReturnVal= 1;
//			set @i = 1;
//			while (@i <= @Value)
//			begin
//				set @LReturnVal= @LReturnVal * @i;
//				set @i = @i + 1;
//			end;
//			return @LReturnVal;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSReadMillisecond'))
//				drop function DAE_TSReadMillisecond
//					",
//					@"
//			create function DAE_TSReadMillisecond(@ATimeSpan bigint)
//			returns integer
//			begin
//				return dbo.Trunc(dbo.Frac(@ATimeSpan / (10000.0 * 1000)) * 1000);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSReadSecond'))
//				drop function DAE_TSReadSecond
//					",
//					@"
//			create function DAE_TSReadSecond(@ATimeSpan bigint)
//			returns integer
//			begin
//				return dbo.Trunc(dbo.Frac(@ATimeSpan / (10000000.0 * 60)) * 60);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSReadMinute'))
//				drop function DAE_TSReadMinute
//					",
//					@"
//			create function DAE_TSReadMinute(@ATimeSpan bigint)
//			returns integer
//			begin
//				return dbo.Trunc(dbo.Frac(@ATimeSpan / (600000000.0 * 60)) * 60);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSReadHour'))
//				drop function DAE_TSReadHour
//					",
//					@"
//			create function DAE_TSReadHour(@ATimeSpan bigint)
//			returns integer
//			begin
//				return dbo.Trunc(dbo.Frac(@ATimeSpan / (36000000000.0 * 24)) * 24);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSReadDay'))
//				drop function DAE_TSReadDay
//					",
//					@"
//			create function DAE_TSReadDay(@ATimeSpan bigint)
//			returns integer
//			begin
//				return dbo.Trunc(@ATimeSpan / 864000000000.0);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteMillisecond'))
//				drop function DAE_TSWriteMillisecond
//					",
//					@"
//			create function DAE_TSWriteMillisecond(@ATimeSpan bigint, @APart int)
//			returns bigint
//			begin
//				return @ATimeSpan + (@APart - dbo.TSReadMillisecond(@ATimeSpan)) * 10000;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteSecond'))
//				drop function DAE_TSWriteSecond
//					",
//					@"
//			create function DAE_TSWriteSecond(@ATimeSpan bigint, @APart int)
//			returns bigint
//			begin
//				return @ATimeSpan + (@APart - dbo.TSReadSecond(@ATimeSpan) ) * 10000000;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteMinute'))
//				drop function DAE_TSWriteMinute
//					",
//					@"
//			create function DAE_TSWriteMinute(@ATimeSpan bigint, @APart int)
//			returns bigint
//			begin
//				return @ATimeSpan + (@APart - dbo.TSReadMinute(@ATimeSpan) ) * 600000000;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteHour'))
//				drop function DAE_TSWriteHour
//					",
//					@"
//			create function DAE_TSWriteHour(@ATimeSpan bigint, @APart int)
//			returns bigint
//			begin
//				return @ATimeSpan + (@APart - dbo.TSReadHour(@ATimeSpan) ) * 36000000000;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteDay'))
//				drop function DAE_TSWriteDay
//					",
//					@"
//			create function DAE_TSWriteDay(@ATimeSpan bigint, @APart int)
//			returns bigint
//			begin
//				return @ATimeSpan + (@APart - dbo.TSReadDay(@ATimeSpan) ) * 864000000000;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_AddMonths'))
//				drop function DAE_AddMonths
//					",
//					@"
//			create function DAE_AddMonths(@ADate datetime, @AMonths int)
//			returns datetime
//			begin
//				return DateAdd(mm, @AMonths, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_AddYears'))
//				drop function DAE_AddYears
//					",
//					@"
//			create function DAE_AddYears(@ADate datetime, @AYears int)
//			returns datetime
//			begin
//				return DateAdd(yyyy, @AYears, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DayOfWeek'))
//				drop function DAE_DayOfWeek
//					",
//					@"
//			create function DAE_DayOfWeek(@ADate datetime)
//			returns int
//			begin
//				return DatePart(dw, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DayOfYear'))
//				drop function DAE_DayOfYear
//					",
//					@"
//			create function DAE_DayOfYear(@ADate datetime)
//			returns int
//			begin
//				return DatePart(dy, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DaysInMonth'))
//				drop function DAE_DaysInMonth
//					",
//					@"
//			create function DAE_DaysInMonth(@Year int, @Month int)
//			returns int
//			begin
//				declare @Date datetime;
//				set @Date = '10/01/1980';
//				set @Date = dbo.DTWriteYear(@Date, @Year);
//				set @Date = dbo.DTWriteMonth(@Date, @Month);
//				return DateDiff(dd, @Date, DateAdd(mm, 1, @Date));
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_IsLeapYear'))
//				drop function DAE_IsLeapYear
//					",
//					@"
//			create function DAE_IsLeapYear(@Year int)
//			returns int
//			begin
//				declare @Date1 datetime;
//				declare @Date2 datetime;
//				set @Date1 = '2/28/1980';
//				set @Date1 = dbo.DTWriteYear(@Date1, @Year);
//				set @Date2 = '3/1/1980';
//				set @Date2 = dbo.DTWriteYear(@Date2, @Year);
//				return DateDiff(dd, @Date1, @date2) - 1;
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTReadHour'))
//				drop function DAE_DTReadHour
//					",
//					@"
//			create function DAE_DTReadHour(@ADate datetime)
//			returns int
//			begin
//				return DatePart(hh, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTReadMinute'))
//				drop function DAE_DTReadMinute
//					",
//					@"
//			create function DAE_DTReadMinute(@ADate datetime)
//			returns int
//			begin
//				return DatePart(mi, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTReadSecond'))
//				drop function DAE_DTReadSecond
//					",
//					@"
//			create function DAE_DTReadSecond(@ADate datetime)
//			returns int
//			begin
//				return DatePart(ss, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTReadMillisecond'))
//				drop function DAE_DTReadMillisecond
//					",
//					@"
//			create function DAE_DTReadMillisecond(@ADate datetime)
//			returns int
//			begin
//				return DatePart(ms, @ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMillisecond'))
//				drop function DAE_DTWriteMillisecond
//					",
//					@"
//			create function DAE_DTWriteMillisecond(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(ms,@APart - DatePart(ms,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteSecond'))
//				drop function DAE_DTWriteSecond
//					",
//					@"
//			create function DAE_DTWriteSecond(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(ss,@APart - DatePart(ss,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMinute'))
//				drop function DAE_DTWriteMinute
//					",
//					@"
//			create function DAE_DTWriteMinute(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(mi,@APart - DatePart(mi,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteHour'))
//				drop function DAE_DTWriteHour
//					",
//					@"
//			create function DAE_DTWriteHour(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(hh,@APart - DatePart(hh,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteDay'))
//				drop function DAE_DTWriteDay
//					",
//					@"
//			create function DAE_DTWriteDay(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(dd,@APart - DatePart(dd,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMonth'))
//				drop function DAE_DTWriteMonth
//					",
//					@"
//			create function DAE_DTWriteMonth(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(mm,@APart - DatePart(mm,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMillisecond'))
//				drop function DAE_DTWriteMillisecond
//					",
//					@"
//			create function DAE_DTWriteMillisecond(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(ms,@APart - DatePart(ms,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteYear'))
//				drop function DAE_DTWriteYear
//					",
//					@"
//			create function DAE_DTWriteYear(@ADate datetime, @APart int)
//			returns datetime
//			begin
//				return DateAdd(yyyy,@APart - DatePart(yyyy,@ADate),@ADate);
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DateTimeSelector'))
//				drop function DAE_DateTimeSelector
//					",
//					@"
//			create function DAE_DateTimeSelector(@Year int, @Month int = 0, @Day int = 0, @Hour int = 0, @Minute int = 0, @Second int = 0, @Millisecond int = 0)
//			returns datetime
//			begin
//				return DateAdd(ms, @Millisecond, DateAdd(ss, @Second, DateAdd(mi, @Minute, DateAdd(hh, @Hour, dbo.DTWriteDay(dbo.DTWriteMonth(dbo.DTWriteYear('1/1/1900', @Year), @Month), @Day)))))
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTDatePart'))
//				drop function DAE_DTDatePart
//					",
//					@"
//			create function DAE_DTDatePart(@ADateTime datetime)
//			returns datetime
//			begin
//				return Convert( DateTime, Floor ( Convert( Float, @ADateTime ) ) );
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTTimePart'))
//				drop function DAE_DTTimePart
//					",
//					@"
//			create function DAE_DTTimePart(@ADateTime datetime)
//			returns datetime
//			begin
//				return Convert( DateTime, dbo.Frac ( Convert( Float, @ADateTime ) ) );
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_DTTimeSpan'))
//				drop function DAE_DTTimeSpan
//					",
//					@"
//			create function DAE_DTTimeSpan (@ADateTime datetime)
//			returns bigint
//			begin
//				declare @LRefDate datetime;
//				set @LRefDate = '01/01/2000';
//				return 10000 * (1000 * (DateDiff(ss, @LRefDate, @ADateTime) + 63082281600) + DatePart(ms, @ADateTime));
//			end
//					"
//				)
//			);
//
//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//			if exists (select * from sysobjects where id = Object_ID('DAE_TSDateTime'))
//				drop function DAE_TSDateTime
//					",
//					@"
//			create function DAE_TSDateTime (@ATimeSpan bigint)
//			returns datetime
//			begin
//				declare @temptime bigint;
//				set @Temptime = (@ATimeSpan - 630822816000000000) / 10000000;
//				declare @temptime2 bigint;
//				set @temptime2 = dbo.Frac((@AtimeSpan - 630822816000000000) / 10000000.0) * 1000;
//				return DateAdd(ms, @temptime2, DateAdd(ss, @Temptime ,'1/01/2000'));
//			end
//					"
//				)
//			);
		}
    }
    #endregion
}
