/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Device.SAS
{
	using System;
	using System.Text;
	using System.Resources;
	using System.Globalization;

	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using SAS = Alphora.Dataphor.DAE.Language.SAS;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Schema;	
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	
	/*
		Data Type Mapping ->
		
			DAE Type	|	SAS SQL Type													|	Translation Handler
			------------|---------------------------------------------------------------------------------------------
			Boolean		|	integer															|	SASBoolean
			Byte		|   smallint														|	SASByte
			Short		|	smallint														|	SASShort
			Integer		|	integer															|	SASInteger
			Long		|	decimal(20, 0)													|	SASLong
			Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SASDecimal
			TimeSpan	|	decimal(20, 0)													|	SASTimeSpan
			SQLDateTime	|	date															|	SASDateTime
			Date		|	date															|	SASDate
			Time		|	date															|	SASTime
			Money		|	decimal(28, 8)													|	SASMoney
			Guid		|	char(24)														|	SASGuid
			String		|	varchar(Storage.Length)											|	SASString
			//IString		|	varchar(Storage.Length)											|	SASString
	*/

	
	public class SASDevice : SQLDevice
	{
		public SASDevice(int AID, string AName) : base(AID, AName)
		{
			_supportsTransactions = false;
			UseTransactions = false; // The SAS CLI does not support explicit transactions
			UseQuotedIdentifiers = false;
		}
		
		protected override void SetMaxIdentifierLength()
		{
			_maxIdentifierLength = 32; // this is the max identifier length in SAS all the time
		}
		
		protected override void RegisterSystemObjectMaps(ServerProcess AProcess)
		{
			base.RegisterSystemObjectMaps(AProcess);

			// Perform system type and operator mapping registration
			ResourceManager LResourceManager = new ResourceManager("SystemCatalog", typeof(SASDevice).Assembly);
			#if USEISTRING
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
			#else
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, "false"));
			#endif
		}

		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new SASDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

		// Emitter
		protected override SQLTextEmitter InternalCreateEmitter() { return new SAS.SASTextEmitter(); }

		// ServerID
		protected string FServerID = String.Empty;
		public string ServerID
		{
			get { return FServerID; }
			set { FServerID = value == null ? String.Empty : value; }
		}
		
		// Location
		private string FLocation;
		public string Location
		{
			get { return FLocation; }
			set { FLocation = value; }
		}

		// ShouldIncludeColumn
		public override bool ShouldIncludeColumn(Plan APlan, string ATableName, string AColumnName, string ADomainName)
		{
			switch (ADomainName.ToLower())
			{
				case "char":
				case "num": return true;
				default: return false;
			}
		}

		// FindScalarType
		public override ScalarType FindScalarType(Plan APlan, string ADomainName, int ALength, D4.MetaData AMetaData)
        {
			switch (ADomainName.ToLower())
			{
				case "char" : 
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
					#if USEISTRING
					return IsCaseSensitive ? AProcess.DataTypes.SystemString : AProcess.DataTypes.SystemIString;
					#else
					return APlan.DataTypes.SystemString;
					#endif
				case "num" : return APlan.DataTypes.SystemDecimal;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
			}
        }

		private string[] FSASReservedWords = 
		{
			"AS", "GROUP", "LEFT", "UNION",
			"CASE", "HAVING", "ON", "USER",
			"EXCEPT", "INNER", "ORDER", "WHEN",
			"FROM", "INTERSECT", "OUTER", "WHERE", 
			"FULL", "JOIN", "RIGHT"
		};
																						
		protected override bool IsReservedWord(string AWord)
		{
			for (int i = 0; i < FSASReservedWords.Length; i++)
			{
				if (AWord.ToUpper() == FSASReservedWords[i].ToUpper())
					return true;
			}
			return false;
		}

        public override TableSpecifier GetDummyTableSpecifier()
        {
			// select 0 as DUMMY from DICTIONARY.OPTIONS where OPTNAME = 'BUFNO'
			SelectExpression LSelectExpression = new SelectExpression();
			LSelectExpression.SelectClause = new SelectClause();
			LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "DUMMY"));
			LSelectExpression.FromClause = new AlgebraicFromClause(new TableSpecifier(new TableExpression("DICTIONARY", "OPTIONS")));
			//LSelectExpression.FromClause = new CalculusFromClause(new TableSpecifier[]{new TableSpecifier(new TableExpression("DICTIONARY", "OPTIONS"))});
			LSelectExpression.WhereClause = new WhereClause(new BinaryExpression(new QualifiedFieldExpression("OPTNAME"), "iEqual", new ValueExpression("BUFNO")));
			return new TableSpecifier(LSelectExpression, "DUMMY");
        }
        
        protected override string GetDeviceTablesExpression(TableVar ATableVar)
        {
			return
				String.Format
				(
					DeviceTablesExpression == String.Empty ?
						@"
select
	C.libname as TableSchema,
	C.memname as TableName,
	C.name as ColumnName,
	C.npos as OrdinalPosition,
	T.memlabel as TableTitle,
	C.label as ColumnTitle,
	C.type as NativeDomainName,
	C.type as DomainName,
	C.length as Length,
	0 as IsNullable,
	0 as IsDeferred
from DICTIONARY.COLUMNS as C, DICTIONARY.TABLES as T
where C.libname = '{0}'
	and C.libname = T.libname
	and C.memname = T.memname
	{1}
order by C.libname, C.memname, C.npos
						" :
						DeviceTablesExpression,
					Schema,
					ATableVar == null ? String.Empty : String.Format("and c.memname = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
				);
        }
        
        protected override string GetDeviceIndexesExpression(TableVar ATableVar)
        {
			return
				String.Format
				(
					DeviceIndexesExpression == String.Empty ?
						@"
select
	libname as TableSchema,
	memname as TableName,
	indxname as IndexName,
	name as ColumnName,
	indxpos as OrdinalPosition,
	case when unique = 'yes' then 1 else 0 end as IsUnique,
	0 as IsDescending
from DICTIONARY.INDEXES
where libname = '{0}'
	{1}
order by libname, memname, indxname, indxpos
						" :
						DeviceIndexesExpression,
					Schema,
					ATableVar == null ? String.Empty : String.Format("and c.memname = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
				);
        }
        
		protected override string GetIndexName(string ATableName, Key AKey)
		{
			if ((AKey.MetaData != null) && AKey.MetaData.Tags.Contains("Storage.Name"))
				return AKey.MetaData.Tags["Storage.Name"].Value;
			else
			{
				StringBuilder LIndexName = new StringBuilder();
				if (AKey.Columns.Count == 1)
					LIndexName.Append(ToSQLIdentifier(AKey.Columns[0]));
				else
				{
					LIndexName.AppendFormat("UIDX_{0}", ATableName);
					foreach (TableVarColumn LColumn in AKey.Columns)
						LIndexName.AppendFormat("_{0}", (LColumn.Name));
				}
				return EnsureValidIdentifier(LIndexName.ToString());
			}
		}
        
		protected override string GetIndexName(string ATableName, Order AOrder)
		{
			if ((AOrder.MetaData != null) && AOrder.MetaData.Tags.Contains("Storage.Name"))
				return AOrder.MetaData.Tags["Storage.Name"].Value;
			else
			{
				StringBuilder LIndexName = new StringBuilder();
				if (AOrder.Columns.Count == 1)
					LIndexName.Append(ToSQLIdentifier(AOrder.Columns[0].Column));
				else
				{
					LIndexName.AppendFormat("IDX_{0}", ATableName);
					foreach (OrderColumn LColumn in AOrder.Columns)
						LIndexName.AppendFormat("_{0}", (LColumn.Column.Name));
				}
				return EnsureValidIdentifier(LIndexName.ToString());
			}
		}
	}

	// This interface is defined here because it has more to do with the runtime than the connectivity layer. 
	// It could be moved if we abstract ADO functionality to a common device.
	public interface IADOFilterLiteralBuilder
	{
		string ToLiteral(IValueManager AManager, object AValue);
	}
	
	public class SASDeviceSession : SQLDeviceSession
	{
		public SASDeviceSession(SQLDevice ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		public new SASDevice Device { get { return (SASDevice)base.Device; } }
		
		protected override SQLConnection InternalCreateConnection()
		{
			// ConnectionClass:
				//  ODBCConnection
				//  OLEDBConnection
				//  ADOConnection (default)
				//  SASConnection
			// ConnectionStringBuilderClass
				// SASADOConnectionStringBuilder (default)
				//can use others too

			D4.ClassDefinition LClassDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						"ADOConnection.ADOConnection" : 
						Device.ConnectionClass
				);
			D4.ClassDefinition LBuilderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						"SASDevice.SASOLEDBConnectionStringBuilder" :
						Device.ConnectionStringBuilderClass
				);
			ConnectionStringBuilder LConnectionStringBuilder = (ConnectionStringBuilder)ServerProcess.CreateObject(LBuilderClass, new object[]{});
				
			D4.Tags LTags = new D4.Tags();
			LTags.AddOrUpdate("ServerID", Device.ServerID);
			LTags.AddOrUpdate("Location", Device.Location);
			LTags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);

            LTags = LConnectionStringBuilder.Map(LTags);				
			Device.GetConnectionParameters(LTags, DeviceSessionInfo);
			string LConnectionString = SQLDevice.TagsToString(LTags);

			SQLConnection LConnection = (SQLConnection)ServerProcess.CreateObject(LClassDefinition, new object[]{LConnectionString});
			LConnection.DefaultUseParametersForCursors = false;
			return LConnection;
		}

		#if UPDATEWITHCURSORS
		protected override void InternalInsertRow(Schema.TableVar ATableVar, Row ARow)
		{
			using (SQLCommand LCommand = Connection.CreateCommand(false))
			{
				string LTableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Device.Schema);
				string LTableName = Device.ToSQLIdentifier(ATableVar);
				if (LTableSchema != String.Empty)
					LTableName = String.Format("{0}.{1}", LTableSchema, LTableName);
				if (Device.CommandTimeout >= 0)
					LCommand.CommandTimeout = Device.CommandTimeout;
				LCommand.Statement = LTableName;
				LCommand.CommandType = SQLCommandType.Table;
				LCommand.LockType = SQLLockType.Pessimistic;
				SQLCursor LCursor = LCommand.Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
				try
				{
					SQLScalarType LScalarType;
					Schema.TableVarColumn LColumn;
					string[] LNames = new string[ARow.DataType.Columns.Count];
					object[] LValues = new object[ARow.DataType.Columns.Count];
					for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
					{
						LColumn = ATableVar.Columns[ARow.DataType.Columns[LIndex].Name];
						LScalarType = (SQLScalarType)Device.DeviceScalarTypes[LColumn.DataType];
						LNames[LIndex] = Device.ToSQLIdentifier(LColumn);
						LValues[LIndex] = ARow.HasValue(LIndex) ? LScalarType.ParameterFromScalar(ARow[LIndex]) : null;
					}

					LCursor.Insert(LNames, LValues);
				}
				finally
				{
					LCommand.Close(LCursor);
				}
			}
		}

		protected override void InternalUpdateRow(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			using (SQLCommand LCommand = Connection.CreateCommand(false))
			{
				string LTableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Device.Schema);
				string LTableName = Device.ToSQLIdentifier(ATableVar);
				if (LTableSchema != String.Empty)
					LTableName = String.Format("{0}.{1}", LTableSchema, LTableName);
				if (Device.CommandTimeout >= 0)
					LCommand.CommandTimeout = Device.CommandTimeout;
				LCommand.Statement = LTableName;
				LCommand.CommandType = SQLCommandType.Table;
				LCommand.LockType = SQLLockType.Pessimistic;
				SQLCursor LCursor = LCommand.Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
				try
				{
					SQLScalarType LScalarType;
					Schema.TableVarColumn LColumn;
					Schema.Key LKey = Program.FindClusteringKey(ATableVar);
					#if USESEEKTOUPDATE
					object[] LKeyValues = new object[LKey.Columns.Count];
					for (int LIndex = 0; LIndex < LKeyValues.Length; LIndex++)
					{
						LColumn = ATableVar.Columns[LKey.Columns[LIndex].Name];
						LScalarType = (SQLScalarType)Device.DeviceScalarTypes[LColumn.DataType];
						LKeyValues[LIndex] = AOldRow.HasValue(LColumn.Name) ? LScalarType.ParameterFromScalar(AOldRow[LColumn.Name]) : null;
					}
					
					if (!LCursor.FindKey(LKeyValues))
						throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound);
					#else
					StringBuilder LFilter = new StringBuilder();
					IADOFilterLiteralBuilder LBuilder;
					for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
					{
						LColumn = ATableVar.Columns[LKey.Columns[LIndex].Name];
						LScalarType = (SQLScalarType)Device.DeviceScalarTypes[LColumn.DataType];
						LBuilder = LScalarType as IADOFilterLiteralBuilder;
						if (LBuilder == null)
							throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);

						if (LIndex > 0)
							LFilter.Append(" and ");

						if (AOldRow.HasValue(LColumn.Name))
						
							LFilter.AppendFormat("[{0}] = {1}", Device.ToSQLIdentifier(LColumn), LBuilder.ToLiteral((IScalar)AOldRow[LColumn.Name]));
						else
							throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound);
					}

					if (!LCursor.SetFilter(LFilter.ToString()))
						throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound);
					#endif	
					
					string[] LNames = new string[ANewRow.DataType.Columns.Count];
					object[] LValues = new object[ANewRow.DataType.Columns.Count];
					for (int LIndex = 0; LIndex < ANewRow.DataType.Columns.Count; LIndex++)
					{
						LColumn = ATableVar.Columns[ANewRow.DataType.Columns[LIndex].Name];
						LScalarType = (SQLScalarType)Device.DeviceScalarTypes[LColumn.DataType];
						LNames[LIndex] = Device.ToSQLIdentifier(LColumn);
						LValues[LIndex] = ANewRow.HasValue(LIndex) ? LScalarType.ParameterFromScalar(ANewRow[LIndex]) : null;
					}

					LCursor.Update(LNames, LValues);
				}
				finally
				{
					LCommand.Close(LCursor);
				}
			}
		}

		protected override void InternalDeleteRow(Schema.TableVar ATableVar, Row ARow)
		{
			using (SQLCommand LCommand = Connection.CreateCommand(false))
			{
				string LTableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Device.Schema);
				string LTableName = Device.ToSQLIdentifier(ATableVar);
				if (LTableSchema != String.Empty)
					LTableName = String.Format("{0}.{1}", LTableSchema, LTableName);
				if (Device.CommandTimeout >= 0)
					LCommand.CommandTimeout = Device.CommandTimeout;
				LCommand.Statement = LTableName;
				LCommand.CommandType = SQLCommandType.Table;
				LCommand.LockType = SQLLockType.Pessimistic;
				SQLCursor LCursor = LCommand.Open(SQLCursorType.Dynamic, SQLIsolationLevel.Serializable);
				try
				{
					SQLScalarType LScalarType;
					Schema.TableVarColumn LColumn;
					Schema.Key LKey = Program.FindClusteringKey(ATableVar);
					
					#if USESEEKTOUPDATE
					object[] LKeyValues = new object[LKey.Columns.Count];
					for (int LIndex = 0; LIndex < LKeyValues.Length; LIndex++)
					{
						LColumn = ATableVar.Columns[LKey.Columns[LIndex].Name];
						LScalarType = (SQLScalarType)Device.DeviceScalarTypes[LColumn.DataType];
						LKeyValues[LIndex] = ARow.HasValue(LColumn.Name) ? LScalarType.ParameterFromScalar(ARow[LColumn.Name]) : null;
					}
					if (!LCursor.FindKey(LKeyValues))
						throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound);
					#else
					StringBuilder LFilter = new StringBuilder();
					IADOFilterLiteralBuilder LBuilder;
					for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
					{
						LColumn = ATableVar.Columns[LKey.Columns[LIndex].Name];
						LScalarType = (SQLScalarType)Device.DeviceScalarTypes[LColumn.DataType];
						LBuilder = LScalarType as IADOFilterLiteralBuilder;
						if (LBuilder == null)
							throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);

						if (LIndex > 0)
							LFilter.Append(" and ");

						if (ARow.HasValue(LColumn.Name))
							LFilter.AppendFormat("[{0}] = {1}", Device.ToSQLIdentifier(LColumn), LBuilder.ToLiteral((IScalar)ARow[LColumn.Name]));
						else
							throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound);
					}

					if (!LCursor.SetFilter(LFilter.ToString()))
						throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound);
					#endif
					
						
					LCursor.Delete();
				}
				finally
				{
					LCommand.Close(LCursor);
				}
			}
		}
		#endif
	}

	public class SASOLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public SASOLEDBConnectionStringBuilder()
		{
			_parameters.AddOrUpdate("Provider", "sas.SHAREProvider.1");
			_legend.AddOrUpdate("ServerID", "Data source");
			_legend.AddOrUpdate("UserName", "user id");
			_legend.AddOrUpdate("Password", "password");
		}
	}

    public class SASRetrieve : SQLDeviceOperator
    {
		public SASRetrieve(int AID, string AName) : base(AID, AName) {}
		//public SASRetrieve(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SASRetrieve(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			TableVar LTableVar = ((TableVarNode)APlanNode).TableVar;

			if (LTableVar is BaseTableVar)
			{
				SQLRangeVar LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
				foreach (Schema.TableVarColumn LColumn in LTableVar.Columns)
					LRangeVar.Columns.Add(new SQLRangeVarColumn(LColumn, LRangeVar.Name, LDevicePlan.Device.ToSQLIdentifier(LColumn), LDevicePlan.Device.ToSQLIdentifier(LColumn.Name)));
				LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
				SelectExpression LSelectExpression = new SelectExpression();
				LSelectExpression.FromClause = new AlgebraicFromClause(new TableSpecifier(new TableExpression(D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema), LDevicePlan.Device.ToSQLIdentifier(LTableVar)), LRangeVar.Name));
				//LSelectExpression.FromClause = new CalculusFromClause(new TableSpecifier(new TableExpression(D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema), LDevicePlan.Device.ToSQLIdentifier(LTableVar)), LRangeVar.Name));
				LSelectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn LColumn in LTableVar.Columns)
					LSelectExpression.SelectClause.Columns.Add(LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());
				
				LSelectExpression.SelectClause.Distinct = 
					(LTableVar.Keys.Count == 1) && 
					Convert.ToBoolean(D4.MetaData.GetTag(LTableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));
				
				return LSelectExpression;
			}
			else
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
    }

	public class SASBoolean : SQLScalarType, IADOFilterLiteralBuilder
	{
		public SASBoolean(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToBoolean(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((bool)AValue ? 1 : 0);
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(2);
		}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "smallint";
		}

		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((bool)AValue ? 1 : 0).ToString();
		}
	}

	public class SASByte : SQLScalarType, IADOFilterLiteralBuilder
	{
		public SASByte(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToByte(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (byte)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(2);
		}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "smallint";
		}

		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((byte)AValue).ToString();
		}
	}

	public class SASShort : SQLScalarType, IADOFilterLiteralBuilder
	{
		public SASShort(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt16(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (short)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(2);
		}

		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "smallint";
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((short)AValue).ToString();
		}
	}

	public class SASInteger : SQLScalarType, IADOFilterLiteralBuilder
	{
		public SASInteger(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			 return Convert.ToInt32(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (int)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLIntegerType(4);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "integer";
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((int)AValue).ToString();
		}
	}

	public class SASLong : SQLScalarType, IADOFilterLiteralBuilder
	{
		public SASLong(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToInt64(AValue);
		}

		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (long)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "decimal(20, 0)";
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((long)AValue).ToString();
		}
	}

	/// <summary>
	/// SQL type : decimal(28, 8)
	/// D4 type : System.Decimal
	/// </summary>
    public class SASDecimal : SQLScalarType, IADOFilterLiteralBuilder
    {
		public SASDecimal(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToDecimal(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (decimal)AValue;
		}
		
		public byte GetPrecision(D4.MetaData AMetaData)
		{
			return Byte.Parse(GetTag("Storage.Precision", "28", AMetaData));
		}
		
		public byte GetScale(D4.MetaData AMetaData)
		{
			return Byte.Parse(GetTag("Storage.Scale", "8", AMetaData));
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(GetPrecision(AMetaData), GetScale(AMetaData));
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return 
				String.Format
				(
					"decimal({0}, {1})", 
					GetPrecision(AMetaData).ToString(),
					GetScale(AMetaData).ToString()
				);
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((decimal)AValue).ToString();
		}
    }

	/// <summary>
	/// SQL type : bigint
	/// D4 type : System.TimeSpan
	/// </summary>    
    public class SASTimeSpan : SQLScalarType, IADOFilterLiteralBuilder
    {
		public SASTimeSpan(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return new TimeSpan(Convert.ToInt64(AValue));
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((TimeSpan)AValue).Ticks;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "numeric(20, 0)";
		}

		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((TimeSpan)AValue).Ticks.ToString();
		}
    }
    
	/// <summary>
	/// SQL type : date
	/// D4 type : SQLDevice.SQLDateTime
	/// </summary>
    public class SASDateTime : SQLScalarType, IADOFilterLiteralBuilder
    {
		public const string CDateTimeFormat = "dd/MMM/yyyy hh:mm:ss tt";
		
		public SASDateTime(int AID, string AName) : base(AID, AName) {}
		
		private string FDateTimeFormat = CDateTimeFormat;
		public string DateTimeFormat
		{
			get { return FDateTimeFormat; }
			set { FDateTimeFormat = value; }
		}
		
		// SAS stores datetime values as a numeric value representing the number of seconds since Jan 1, 1960.
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("'{0}'", ((DateTime)AValue).ToString(DateTimeFormat, DateTimeFormatInfo.InvariantInfo));
		}
		
		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return new DateTime(1960, 1, 1).AddSeconds(Convert.ToDouble(AValue));
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((DateTime)AValue).Subtract(new DateTime(1960, 1, 1)).TotalSeconds;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date FORMAT=DATETIME18. INFORMAT=DATETIME18.";
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("#{0}#", ((DateTime)AValue).ToString(DateTimeFormat));
		}
    }

	/// <summary>
	/// SQL type : date
	/// D4 type : System.Date
	/// </summary>
    public class SASDate : SQLScalarType, IADOFilterLiteralBuilder
    {
		public const string CDateFormat = "dd/MMM/yyyy";
		
		public SASDate(int AID, string AName) : base(AID, AName) {}
		
		private string FDateFormat = CDateFormat;
		public string DateFormat
		{
			get { return FDateFormat; }
			set { FDateFormat = value; }
		}
		
		// SAS stores Date values as a numeric value representing the number of days since January 1, 1960.
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("'{0}'", ((DateTime)AValue).ToString(DateFormat, DateTimeFormatInfo.InvariantInfo));
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return new DateTime(1960, 1, 1).AddDays(Convert.ToDouble(AValue));
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((DateTime)AValue).Subtract(new DateTime(1960, 1, 1)).TotalDays;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLDateType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date FORMAT=DATE9. INFORMAT=DATE9.";
		}

		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("#{0}#", ((DateTime)AValue).ToString(DateFormat));
		}
    }

	/// <summary>
	/// SQL type : date
	/// D4 type : SQLDevice.SQLTime
	/// </summary>
    public class SASTime : SQLScalarType, IADOFilterLiteralBuilder
    {
		public const string CTimeFormat = "HH:mm:ss";
		
		public SASTime(int AID, string AName) : base(AID, AName) {}
		
		private string FTimeFormat = CTimeFormat;
		public string TimeFormat
		{
			get { return FTimeFormat; }
			set { FTimeFormat = value; }
		}
		
		// SAS stores time values in the same way as a date time (as near as I can tell, the docs don't actually say).
		
		public override string ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("'{0}'", ((DateTime)AValue).ToString(TimeFormat, DateTimeFormatInfo.InvariantInfo));
		}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			DateTime LDateTime = new DateTime(1960, 1, 1).AddSeconds(Convert.ToDouble(AValue));
			return new DateTime(1, 1, 1, LDateTime.Hour, LDateTime.Minute, LDateTime.Second, LDateTime.Millisecond);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return ((DateTime)AValue).Subtract(new DateTime(1960, 1, 1)).TotalSeconds;
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTimeType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "date FORMAT=TIME8. INFORMAT=TIME8.";
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("#{0}#", ((DateTime)AValue).ToString(TimeFormat));
		}
    }

	/// <summary>
	/// SQL type : decimal(28, 8)
	/// D4 type : System.Money
	/// </summary>
    public class SASMoney : SQLScalarType, IADOFilterLiteralBuilder
    {
		public SASMoney(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return Convert.ToDecimal(AValue);
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (decimal)AValue;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(28, 8);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "decimal(28, 8)";
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return ((decimal)AValue).ToString();
		}
    }

    public class SASGuid : SQLGuid, IADOFilterLiteralBuilder
    {
		public SASGuid(int AID, string AName) : base(AID, AName) {}
		//public SASGuid(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		//public SASGuid(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("'{0}'", FromScalar(AManager, AValue));
		}
    }

	/// <summary>
	/// SQL type : varchar(StaticByteSize)
	/// D4 type : System.String | System.IString
	/// </summary>
    public class SASString : SQLScalarType, IADOFilterLiteralBuilder
    {
		public SASString(int AID, string AName) : base(AID, AName) {}

		public override object ToScalar(IValueManager AManager, object AValue)
		{
			return (string)AValue;
		}
		
		public override object FromScalar(IValueManager AManager, object AValue)
		{
			return (string)AValue;
		}
		
		protected string GetLength(D4.MetaData AMetaData, ScalarType AScalarType)
		{
			return D4.MetaData.GetTag(AMetaData, "Storage.Length", D4.MetaData.GetTag(AScalarType.MetaData, "Storage.Length", "20"));
		}
		
		public int GetLength(D4.MetaData AMetaData)
		{
			return Int32.Parse(GetTag("Storage.Length", "20", AMetaData));
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLStringType(GetLength(AMetaData));
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return String.Format("varchar({0})", GetLength(AMetaData).ToString());
		}
		
		string IADOFilterLiteralBuilder.ToLiteral(IValueManager AManager, object AValue)
		{
			return String.Format("'{0}'", ((string)AValue).Replace("'", "''"));
		}
    }
    
	#if UseUnsignedIntegers
	public class SASUInteger : SQLUInteger
	{
		public SASUInteger() : base(){}
		public SASUInteger(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		public SASUInteger(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		public override Scalar ToScalar(IValueManager AManager, object AValue)
		{
			return Scalar.FromUInt32(AProcess, Convert.ToUInt32((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.ToUInt32();
		}

		public override SQLType GetSQLType(ScalarType AScalarType, D4.MetaData AMetaData)
		{
			return new SQLNumericType(10, 0);
		}

		public override string DomainName(TableVarColumn AColumn)
		{
			return "number(10, 0)";
		}
	}
	#endif

	#if UseUnsignedIntegers
	public class SASUShort : SQLUShort
	{
		public SASUShort() : base(){}
		public SASUShort(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		public SASUShort(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		public override Scalar ToScalar(IValueManager AManager, object AValue)
		{
			return Scalar.FromUInt16(AProcess, Convert.ToUInt16((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.ToUInt16();
		}

		public override SQLType GetSQLType(ScalarType AScalarType, D4.MetaData AMetaData)
		{
			return new SQLNumericType(5, 0);
		}

		public override string DomainName(TableVarColumn AColumn)
		{
			return "number(5, 0)";
		}
	}
	#endif

	#if UseUnsignedIntegers
	public class SASSByte : SQLSByte
	{
		public SASSByte() : base(){}
		public SASSByte(ScalarType AScalarType, D4.ClassDefinition AClassDefinition) : base(AScalarType, AClassDefinition){}
		public SASSByte(ScalarType AScalarType, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AScalarType, AClassDefinition, AIsSystem){}

		public override Scalar ToScalar(IValueManager AManager, object AValue)
		{
			return Scalar.FromSByte(AProcess, Convert.ToSByte((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.ToSByte();
		}

		public override SQLType GetSQLType(ScalarType AScalarType, D4.MetaData AMetaData)
		{
			return new SQLNumericType(3, 0);
		}

		public override string DomainName(TableVarColumn AColumn)
		{
			return "number(3, 0)";
		}
	}
	#endif
}