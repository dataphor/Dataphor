/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Device.Oracle
{
	using System;
	using System.Resources;
	using System.IO;
	using System.Collections;

	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Schema;	
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Oracle = Alphora.Dataphor.DAE.Language.Oracle;

	/*
		Data Type Mapping ->
		
			DAE Type	|	Oracle Type														|	Translation Handler
			------------|---------------------------------------------------------------------------------------------
			Boolean		|	decimal(1, 0)													|	OracleBoolean
			Byte		|   decimal(3, 0)													|	OracleByte
			SByte		|	decimal(3, 0)													|	OracleSByte
			Short		|	decimal(5, 0)													|	OracleShort
			UShort		|	decimal(5, 0)													|	OracleUShort
			Integer		|	decimal(10, 0)													|	OracleInteger
			UInteger	|	decimal(10, 0)													|	OracleUInteger
			Long		|	decimal(20, 0)													|	OracleLong
			ULong		|	decimal(20, 0)													|	SQLULong
			Decimal		|	decimal(Storage.Precision, Storage.Scale)						|	SQLDecimal
			SQLDateTime	|	datetime														|	SQLDateTime
			TimeSpan	|	decimal(20, 0)													|	OracleTimeSpan
			Date		|	datetime														|	SQLDate
			SQLTime		|	datetime														|	SQLTime
			Money		|	decimal(28, 8)													|	SQLMoney
			Guid		|	char(24)														|	SQLGuid
			String		|	varchar2(Storage.Length)										|	OracleString
			//IString		|	varchar2(Storage.Length)										|	OracleString
			SQLText		|	clob															|	SQLText
			//SQLIText	|	clob															|	SQLText
			Binary		|	blob															|	SQLBinary
	*/

	
	public class OracleDevice : SQLDevice
	{
		public OracleDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID)
		{
			UseStatementTerminator = false;
			SupportsNestedCorrelation = false;
			IsOrderByInContext = false;
		}
		
		protected override void SetMaxIdentifierLength()
		{
			FMaxIdentifierLength = 30; // this is the max identifier length in Oracle all the time
		}

		protected override void InternalStarted(ServerProcess AProcess)
		{
			base.InternalStarted(AProcess);

			if (ShouldEnsureOperators)
				EnsureOperators(AProcess);
		}
		
		protected override void RegisterSystemObjectMaps(ServerProcess AProcess)
		{
			base.RegisterSystemObjectMaps(AProcess);

			// Perform system type and operator mapping registration
			ResourceManager LResourceManager = new ResourceManager("SystemCatalog", GetType().Assembly);
			#if USEISTRING
			RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, IsCaseSensitive.ToString().ToLower()));
			#else
			using (Stream LStream = GetType().Assembly.GetManifestResourceStream("SystemCatalog.d4"))
			{
				RunScript(AProcess, String.Format(new StreamReader(LStream).ReadToEnd(), Name, "false"));
			}
			//RunScript(AProcess, String.Format(LResourceManager.GetString("SystemObjectMaps"), Name, "false"));
			#endif
		}

		protected void EnsureOperators(ServerProcess AProcess)
		{
			SQLDeviceSession LDeviceSession = (SQLDeviceSession)Connect(AProcess, AProcess.ServerSession.SessionInfo);
			try
			{
				EnsureOperatorDDL LDDL;
				for (int LIndex = 0; LIndex < OracleDDL.EnsureOperatorDDLCommands.Count; LIndex++)
				{
					LDDL = OracleDDL.EnsureOperatorDDLCommands[LIndex] as EnsureOperatorDDL;
					LDeviceSession.Connection.Execute(LDDL.CreateStatement);
				}
			}
			finally
			{
				Disconnect(LDeviceSession);
			}
		}
		
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new OracleDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

		// Emitter
		protected override SQLTextEmitter InternalCreateEmitter() { return new Oracle.OracleTextEmitter(); }

		// HostName
		protected string FHostName = String.Empty;
		public string HostName
		{
			get { return FHostName; }
			set { FHostName= value == null ? String.Empty : value; }
		}
		
		// ShouldIncludeColumn
		public override bool ShouldIncludeColumn(ServerProcess AProcess, string ATableName, string AColumnName, string ADomainName)
		{
			switch (ADomainName.ToLower())
			{
				case "smallint":
				case "int":
				case "integer": 
				case "number":
				case "bigint":
				case "decimal":
				case "numeric":
				case "float":
				case "date":
				case "money":
				case "char":
				case "varchar":
				case "varchar2":
				case "nchar":
				case "nvarchar":
				case "clob":
				case "blob": return true;
				default: return false;

			}
		}

		// FindScalarType
		public override ScalarType FindScalarType(ServerProcess AProcess, string ADomainName, int ALength, D4.MetaData AMetaData)
        {
			switch (ADomainName.ToLower())
			{
				case "smallint": return AProcess.DataTypes.SystemShort;
				case "int":
				case "integer": 
				case "number": return AProcess.DataTypes.SystemInteger;
				case "bigint": return AProcess.DataTypes.SystemLong;
				case "decimal":
				case "numeric":
				case "float": return AProcess.DataTypes.SystemDecimal;
				case "date": return AProcess.DataTypes.SystemDateTime;
				case "money": return AProcess.DataTypes.SystemMoney;
				case "char":
				case "varchar":
				case "varchar2":
				case "nchar":
				case "nvarchar": 
					AMetaData.Tags.Add(new D4.Tag("Storage.Length", ALength.ToString()));
					#if USEISTRING
					return IsCaseSensitive ? AProcess.DataTypes.SystemString : AProcess.DataTypes.SystemIString;
					#else
					return AProcess.DataTypes.SystemString;
					#endif
				#if USEISTRING
				case "clob": return (ScalarType)(IsCaseSensitive ? AProcess.Plan.Catalog[CSQLTextScalarType] : AProcess.Plan.Catalog[CSQLITextScalarType]);
				#else
				case "clob": return (ScalarType)D4.Compiler.ResolveCatalogIdentifier(AProcess.Plan, CSQLTextScalarType, true);
				#endif
				case "blob": return AProcess.DataTypes.SystemBinary;
				default: throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
			}
        }

		private string[] FOracleReservedWords = 
		{
			"abort", "accept", "array", "arraylen", "assert", "assign", "at", "authorization", "avg",
			"base_table", "begin", "binary_integer", "body", "boolean", "case", "char_base","close", 
			"clusters", "colauth", "commit", "constraint", "crash", "currval", "cursor", "database", 
			"data_base", "dba", "debugoff", "debugon", "decalare", "definition", "delay", "digits", 
			"digits", "dispose", "do", "elsif", "end", "entry", "exception", "exception_init", "exit",
			"false", "fetch", "form", "function", "generic", "goto", "if", "indexes", "indicator",
			"interface", "limited", "loop", "max", "min", "minus",	"mislabel", "mod", "natural",
			"number_base", "naturaln", "new", "nextval", "open", "others", "out", "package", "partition",
			"pls_integer", "positive", "positiven", "pragma", "private", "procedure", "raise", "range",
			"real", "record", "ref", "release", "remr", "return", "reverse", "rollback", "rownum",
			"rowtype", "run", "savepoint", "schema", "seperate", "space", "sql", "sqlcode", "sqlerrm",
			"statement", "stddev", "subtype", "sum", "tabauth", "tables", "task", "terminate", "true",
			"type", "use", "variance", "views", "when", "while", "work", "write", "xor"
		};
																						
		protected override bool IsReservedWord(string AWord)
		{
			for (int i = 0; i < FOracleReservedWords.Length; i++)
			{
				if (AWord.ToUpper() == FOracleReservedWords[i].ToUpper())
					return true;
			}
			return false;
		}

        public override TableSpecifier GetDummyTableSpecifier()
        {
			return new TableSpecifier(new TableExpression("DUAL"));
        }
        
        protected override string GetDeviceTablesExpression(TableVar ATableVar)
        {
			return
				String.Format
				(
					DeviceTablesExpression == String.Empty ?
						@"
							select
									c.owner as TableSchema, 
									c.table_name as TableName,
									c.column_name as ColumnName,
									c.column_id as OrdinalPosition,
									c.table_name as TableTitle,
									c.column_name as ColumnTitle,
									c.data_type as NativeDomainName,
									c.data_type as DomainName,
									NVL(c.char_col_decl_length, 0) as Length,
									case when c.nullable = 'N' then 0 else 1 end as IsNullable,
									case when c.data_type in ('BLOB', 'CLOB') then 1 else 0 end as IsDeferred
								from all_tab_columns c
								where 1 = 1 {0} {1}
								order by c.owner, c.table_name, c.column_id
						" :
						DeviceTablesExpression,
					Schema == String.Empty ? String.Empty : String.Format("and c.owner = '{0}'", Schema),
					ATableVar == null ? String.Empty : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
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
									i.table_owner as TableSchema,
									i.table_name as TableName,
									i.index_name as IndexName,
									c.column_name as ColumnName,
									c.column_position as OrdinalPosition,
									case when i.uniqueness = 'UNIQUE' then 1 else 0 end as IsUnique,
									case when c.descend = 'DESC' then 1 else 0 end as IsDescending
								from all_indexes i, all_ind_columns c
								where i.owner = c.index_owner
									and i.index_name = c.index_name
									and i.index_type = 'NORMAL'
									{0}
									{1}
								order by i.table_owner, i.table_name, i.index_name, c.column_position
						" :
						DeviceIndexesExpression,
					Schema == String.Empty ? String.Empty : String.Format("and i.table_owner = '{0}'", Schema),
					ATableVar == null ? String.Empty : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
				);
        }
        
		protected override string GetDeviceForeignKeysExpression(TableVar ATableVar)
		{
			return
				String.Format
				(
					DeviceForeignKeysExpression == String.Empty ?
						@"
							select 
									c.owner as ConstraintSchema, 
									c.constraint_name as ConstraintName,
									c.owner as SourceTableSchema,
									c.table_name as SourceTableName, 
									cc.column_name as SourceColumnName,
									c.r_owner as TargetTableSchema,
									ic.table_name as TargetTableName,
									ic.column_name as TargetColumnName,
									cc.position as OrdinalPosition
								from all_constraints c, all_cons_columns cc, all_ind_columns ic
								where c.owner = cc.owner
									and c.constraint_name = cc.constraint_name
									and c.r_owner = ic.table_owner
									and c.r_constraint_name = ic.index_name
									and cc.position = ic.column_position
									and c.constraint_type = 'R'
									{0}
									{1}
								order by c.owner, c.constraint_name, cc.position
						" :
						DeviceForeignKeysExpression,
					Schema == String.Empty ? String.Empty : String.Format("and c.owner = '{0}'", Schema),
					ATableVar == null ? String.Empty : String.Format("and c.table_name = '{0}'", ToSQLIdentifier(ATableVar).ToUpper())
				);
		}

		protected bool FShouldEnsureOperators = true;
		/// <value>Indicates whether the device should create the DAE support operators if they do not already exist.</value>
		/// <remarks>The value of this property is only valid if IsMSSQL70 is false.</remarks>
		public bool ShouldEnsureOperators
		{
			get { return FShouldEnsureOperators; }
			set { FShouldEnsureOperators = value; }
		}
	}
	
	public class OracleDeviceSession : SQLDeviceSession
	{
		public OracleDeviceSession(SQLDevice ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		public new OracleDevice Device { get { return (OracleDevice)base.Device; } }
		
		protected override SQLConnection InternalCreateConnection()
		{
			// ConnectionClass:
				//  ODBCConnection
				//  OLEDBConnection
				//  ADOConnection 
				//  OracleConnection (default)
			// ConnectionStringBuilderClass
				// OracleConnectionStringBuilder (default)
				// OracleODBCConnectionStringBuilder
				// OracleOLEDConnectionStringBuilder

			D4.ClassDefinition LClassDefinition = 
				new D4.ClassDefinition
				(
					Device.ConnectionClass == String.Empty ? 
						#if USEODP
						"OracleConnection.OracleConnection" :
						#else
						"MSOracleConnection.OracleConnection" :
						#endif
						Device.ConnectionClass
				);
			D4.ClassDefinition LBuilderClass = 
				new D4.ClassDefinition
				(
					Device.ConnectionStringBuilderClass == String.Empty ?
						#if USEODP
						"OracleDevice.OracleConnectionStringBuilder" :
						#else
						"OracleDevice.OracleConnectionStringBuilder" :
						#endif
						Device.ConnectionStringBuilderClass
				);
			ConnectionStringBuilder LConnectionStringBuilder = (ConnectionStringBuilder)ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LBuilderClass, new object[]{});
				
			D4.Tags LTags = new D4.Tags();
			LTags.AddOrUpdate("HostName", Device.HostName);
			LTags.AddOrUpdate("UserName", DeviceSessionInfo.UserName);
			LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);

            LTags = LConnectionStringBuilder.Map(LTags);				
			Device.GetConnectionParameters(LTags, DeviceSessionInfo);
			string LConnectionString = SQLDevice.TagsToString(LTags);

			return (SQLConnection)ServerProcess.Plan.Catalog.ClassLoader.CreateObject(LClassDefinition, new object[]{LConnectionString});
		}
	}

	public class OracleOLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleOLEDBConnectionStringBuilder()
		{
			FParameters.AddOrUpdate("Provider", "MSDAORA");
			FLegend.AddOrUpdate("HostName", "Data source");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
		}
	}

	// for the Oracle.NET data provider
	public class OracleConnectionStringBuilder : ConnectionStringBuilder 
	{
		public OracleConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "Data Source");
			FLegend.AddOrUpdate("UserName", "User Id");
		}
	}

	public class OracleODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public OracleODBCConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "DSN");
			FLegend.AddOrUpdate("UserName", "UID");
			FLegend.AddOrUpdate("Password", "PWD");
		}
	}

    public class OracleRetrieve : SQLDeviceOperator
    {
		public OracleRetrieve(int AID, string AName) : base(AID, AName) {}
		
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
				Oracle.SelectExpression LSelectExpression = new Oracle.SelectExpression();
				LSelectExpression.OptimizerHints = "FIRST_ROWS(20)";
				LSelectExpression.FromClause = 
					new AlgebraicFromClause
					(
						new TableSpecifier
						(
							new TableExpression
							(
								D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema), 
								LDevicePlan.Device.ToSQLIdentifier(LTableVar)
							), 
							LRangeVar.Name
						)
					);
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

    public class OracleJoin : SQLDeviceOperator
    {
		public OracleJoin(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			JoinNode LJoinNode = (JoinNode)APlanNode;
			JoinType LJoinType;
			if ((((LJoinNode.Nodes[2] is ValueNode) && (((ValueNode)LJoinNode.Nodes[2]).DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean)) && ((bool)((ValueNode)LJoinNode.Nodes[2]).Value))))
				LJoinType = JoinType.Cross;
			else if (LJoinNode is LeftOuterJoinNode)
				LJoinType = JoinType.Left;
			else if (LJoinNode is RightOuterJoinNode)
				LJoinType = JoinType.Right;
			else
				LJoinType = JoinType.Inner;
				
			bool LHasOuterColumnExpressions = false;

			LDevicePlan.PushQueryContext();
			Statement LLeftStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			SelectExpression LLeftSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LLeftStatement, false);
			TableVar LLeftTableVar = ((TableNode)APlanNode.Nodes[0]).TableVar;
			for (int LIndex = 0; LIndex < LJoinNode.LeftKey.Columns.Count; LIndex++)
				if (LDevicePlan.GetRangeVarColumn(LJoinNode.LeftKey.Columns[LIndex].Name, true).Expression != null)
				{
					LHasOuterColumnExpressions = true;
					break;
				}

			if (LHasOuterColumnExpressions || LDevicePlan.CurrentQueryContext().IsAggregate || LLeftSelectExpression.SelectClause.Distinct)
			{
				string LNestingReason = "The left argument to the join operator must be nested because ";
				if (LHasOuterColumnExpressions)
					LNestingReason += "the join is to be performed on columns which are introduced as expressions in the current context.";
				else if (LDevicePlan.CurrentQueryContext().IsAggregate)
					LNestingReason += "it contains aggregation.";
				else
					LNestingReason += "it contains a distinct specification.";
				LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LNestingReason, APlanNode));
				LLeftStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LLeftTableVar, LLeftStatement);
				LLeftSelectExpression = LDevicePlan.Device.FindSelectExpression(LLeftStatement);
			}
			SQLQueryContext LLeftContext = LDevicePlan.CurrentQueryContext();
			LDevicePlan.PopQueryContext();
			
			LDevicePlan.PushQueryContext();
			Statement LRightStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			SelectExpression LRightSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[1]).TableVar, LRightStatement, false);
			TableVar LRightTableVar = ((TableNode)APlanNode.Nodes[1]).TableVar;
			LHasOuterColumnExpressions = false;
			for (int LIndex = 0; LIndex < LJoinNode.RightKey.Columns.Count; LIndex++)
				if (LDevicePlan.GetRangeVarColumn(LJoinNode.RightKey.Columns[LIndex].Name, true).Expression != null)
				{
					LHasOuterColumnExpressions = true;
					break;
				}

			if (LHasOuterColumnExpressions || LDevicePlan.CurrentQueryContext().IsAggregate || LRightSelectExpression.SelectClause.Distinct)
			{
				string LNestingReason = "The right argument to the join operator must be nested because ";
				if (LHasOuterColumnExpressions)
					LNestingReason += "the join is to be performed on columns which are introduced as expressions in the current context.";
				else if (LDevicePlan.CurrentQueryContext().IsAggregate)
					LNestingReason += "it contains aggregation.";
				else
					LNestingReason += "it contains a distinct specification.";
				LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LNestingReason, APlanNode));
				LRightStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LRightTableVar, LRightStatement);
				LRightSelectExpression = LDevicePlan.Device.FindSelectExpression(LRightStatement);
			}
			SQLQueryContext LRightContext = LDevicePlan.CurrentQueryContext();
			LDevicePlan.PopQueryContext();
			
			// Merge the query contexts
			LDevicePlan.CurrentQueryContext().RangeVars.AddRange(LLeftContext.RangeVars);
			LDevicePlan.CurrentQueryContext().AddedColumns.AddRange(LLeftContext.AddedColumns);
			LDevicePlan.CurrentQueryContext().RangeVars.AddRange(LRightContext.RangeVars);
			LDevicePlan.CurrentQueryContext().AddedColumns.AddRange(LRightContext.AddedColumns);

			// Merge the from clauses
			CalculusFromClause LLeftFromClause = (CalculusFromClause)LLeftSelectExpression.FromClause;
			CalculusFromClause LRightFromClause = (CalculusFromClause)LRightSelectExpression.FromClause;
			foreach (TableSpecifier LTableSpecifier in LRightFromClause.TableSpecifiers)
				LLeftFromClause.TableSpecifiers.Add(LTableSpecifier);

			LDevicePlan.PushJoinContext(new SQLJoinContext(LLeftContext, LRightContext));
			try
			{
				if (LJoinType != JoinType.Cross)
				{
					Expression LJoinCondition = null;
						
					for (int LIndex = 0; LIndex < LJoinNode.LeftKey.Columns.Count; LIndex++)
					{
						SQLRangeVarColumn LLeftColumn = LDevicePlan.CurrentJoinContext().LeftQueryContext.GetRangeVarColumn(LJoinNode.LeftKey.Columns[LIndex].Name);
						SQLRangeVarColumn LRightColumn = LDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(LJoinNode.RightKey.Columns[LIndex].Name);
						Expression LLeftExpression = LLeftColumn.GetExpression();
						Expression LRightExpression = LRightColumn.GetExpression();
						if (LJoinType == JoinType.Right)
						{
							QualifiedFieldExpression LFieldExpression = (QualifiedFieldExpression)LLeftExpression;
							LLeftExpression = new Oracle.OuterJoinFieldExpression(LFieldExpression.FieldName, LFieldExpression.TableAlias);
						}
						else if (LJoinType == JoinType.Left)
						{
							QualifiedFieldExpression LFieldExpression = (QualifiedFieldExpression)LRightExpression;
							LRightExpression = new Oracle.OuterJoinFieldExpression(LFieldExpression.FieldName, LFieldExpression.TableAlias);
						}

						Expression LEqualExpression = 
							new BinaryExpression
							(
								LLeftExpression,
								"iEqual",
								LRightExpression
							);
							
						if (LJoinCondition != null)
							LJoinCondition = new BinaryExpression(LJoinCondition, "iAnd", LEqualExpression);
						else
							LJoinCondition = LEqualExpression;
					}
					
					if (LLeftSelectExpression.WhereClause == null)
						LLeftSelectExpression.WhereClause = new WhereClause(LJoinCondition);
					else
						LLeftSelectExpression.WhereClause.Expression = new BinaryExpression(LLeftSelectExpression.WhereClause.Expression, "iAnd", LJoinCondition);
						
					OuterJoinNode LOuterJoinNode = LJoinNode as OuterJoinNode;
					if ((LOuterJoinNode != null) && (LOuterJoinNode.RowExistsColumnIndex >= 0))
					{
						TableVarColumn LRowExistsColumn = LOuterJoinNode.TableVar.Columns[LOuterJoinNode.RowExistsColumnIndex];
						CaseExpression LCaseExpression = new CaseExpression();
						CaseItemExpression LCaseItem = new CaseItemExpression();
						if (LOuterJoinNode is LeftOuterJoinNode)
							LCaseItem.WhenExpression = new UnaryExpression("iIsNull", LDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(LOuterJoinNode.RightKey.Columns[0].Name).GetExpression());
						else
							LCaseItem.WhenExpression = new UnaryExpression("iIsNull", LDevicePlan.CurrentJoinContext().LeftQueryContext.GetRangeVarColumn(LOuterJoinNode.LeftKey.Columns[0].Name).GetExpression());
						LCaseItem.ThenExpression = new ValueExpression(0);
						LCaseExpression.CaseItems.Add(LCaseItem);
						LCaseExpression.ElseExpression = new CaseElseExpression(new ValueExpression(1));
						SQLRangeVarColumn LRangeVarColumn = new SQLRangeVarColumn(LRowExistsColumn, LCaseExpression, LDevicePlan.Device.ToSQLIdentifier(LRowExistsColumn));
						LDevicePlan.CurrentQueryContext().AddedColumns.Add(LRangeVarColumn);
						LLeftSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
					}
				}
				
				// Build select clause
				LLeftSelectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn LColumn in ((TableNode)APlanNode).TableVar.Columns)
					LLeftSelectExpression.SelectClause.Columns.Add(LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());
					
				// Merge where clauses
				if (LRightSelectExpression.WhereClause != null)
					if (LLeftSelectExpression.WhereClause == null)
						LLeftSelectExpression.WhereClause = LRightSelectExpression.WhereClause;
					else
						LLeftSelectExpression.WhereClause.Expression = new BinaryExpression(LLeftSelectExpression.WhereClause.Expression, "iAnd", LRightSelectExpression.WhereClause.Expression);
				
				return LLeftStatement;
			}
			finally
			{
				LDevicePlan.PopJoinContext();
			}
		}
    }

	/// <summary>
	/// Oracle type : number(20, 0)
	/// D4 type : System.TimeSpan
	/// </summary>
	public class OracleTimeSpan : SQLScalarType
	{
		public OracleTimeSpan(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{										  
			return new Scalar(AProcess, ScalarType, new TimeSpan(Convert.ToInt64(AValue)));
		}
		
		public override object FromScalar(Scalar AValue)
		{
			return AValue.AsTimeSpan.Ticks;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "number(20, 0)";
		}
	}

	public class OracleBoolean : SQLScalarType
	{
		public OracleBoolean(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, Convert.ToBoolean(AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (AValue.AsBoolean ? 1.0 : 0.0);
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(1, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "number(1, 0)";
		}
	}

	public class OracleInteger : SQLScalarType
	{
		public OracleInteger(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			 return new Scalar(AProcess, ScalarType, Convert.ToInt32(AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.AsInt32;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(10,0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "number(10, 0)";
		}
	}

	#if UseUnsignedIntegers
	public class OracleUInteger : SQLScalarType
	{
		public OracleUInteger() : base(){}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
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

	public class OracleShort : SQLScalarType
	{
		public OracleShort(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, Convert.ToInt16((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.AsInt16;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(5, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "number(5, 0)";
		}
	}

	#if UseUnsignedIntegers
	public class OracleUShort : SQLScalarType
	{
		public OracleUShort() : base(){}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
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

	public class OracleByte : SQLScalarType
	{
		public OracleByte(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, Convert.ToByte((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.AsByte;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(3, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "number(3, 0)";
		}
	}

	#if UseUnsignedIntegers
	public class OracleSByte : SQLScalarType
	{
		public OracleSByte() : base(){}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
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

	public class OracleLong : SQLScalarType
	{
		public OracleLong(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, Convert.ToInt64((decimal)AValue));
		}

		public override object FromScalar(Scalar AValue)
		{
			return (decimal)AValue.AsInt64;
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLNumericType(20, 0);
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "number(20, 0)";
		}
	}
	
	public class OracleString : SQLString
	{
		public OracleString(int AID, string AName) : base(AID, AName) {}
		
		/*
			Oracle cannot distinguish between an empty string and a null once the empty string has been inserted into a table.
			To get around this problem, we translate all empty strings to blank strings of length 1.
		*/

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			if ((AValue is string) && ((string)AValue == " "))
				return new Scalar(AProcess, ScalarType, "");
			else
				return new Scalar(AProcess, ScalarType, (string)AValue);
		}
		
		public override object FromScalar(Scalar AValue)
		{
			string LValue = AValue.AsString;
			if (LValue == String.Empty)
				return " ";
			else
				return LValue;
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return String.Format("varchar2({0})", GetLength(AMetaData));
		}
	}
	
	public class OracleSQLText : SQLScalarType
	{
		public OracleSQLText(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			if ((AValue is string) && ((string)AValue == " "))
				return new Scalar(AProcess, ScalarType, "");
			else
				return new Scalar(AProcess, ScalarType, (string)AValue);
		}
		
		public override object FromScalar(Scalar AValue)
		{
			string LValue = AValue.AsString;
			if (LValue == String.Empty)
				return " ";
			else
				return LValue;
		}
		
		public override Stream GetStreamAdapter(IServerProcess AProcess, Stream AStream)
		{
			using (StreamReader LReader = new StreamReader(AStream))
			{
				string LValue = LReader.ReadToEnd();
				if (LValue == " ")
					LValue = String.Empty;
				Streams.Conveyor LConveyor = ScalarType.GetConveyor(AProcess);
				MemoryStream LStream = new MemoryStream(LConveyor.GetSize(LValue));
				LStream.SetLength(LStream.GetBuffer().Length);
				LConveyor.Write(LValue, LStream.GetBuffer(), 0);
				return LStream;
			}
		}
		
		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLTextType();
		}

        protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "clob";
		}
	}

	public class EnsureOperatorDDL
	{
		public EnsureOperatorDDL(string ACreateStatement) : base()
		{
			CreateStatement = ACreateStatement;
		}
		
		public string CreateStatement;
	}
	
	public class OracleDDL
	{
		public static ArrayList EnsureOperatorDDLCommands = new ArrayList();
		
		static OracleDDL()
		{
			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_Frac (AValue IN NUMBER)
  RETURN NUMBER
  IS
    LReturnVal NUMBER(28, 8);
  BEGIN
    LReturnVal := AValue - TRUNC(AValue, 0);
    RETURN(LReturnVal);
  END;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_LogB (AValue in NUMBER, ABase in NUMBER)
  RETURN NUMBER
  IS
    LReturnVal NUMBER(28, 8);
  BEGIN
    LReturnVal := LN(AValue) / LN(ABase);
    RETURN (LReturnVal);
  END;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_Random
  RETURN NUMBER
  IS
    LReturnVal NUMBER(28,8);
  BEGIN
    LReturnVal := DAE_Frac(ABS(DBMS_RANDOM.RANDOM) / 100000000);
    DBMS_RANDOM.TERMINATE;
    return LReturnVal;
  END;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_Factorial(AValue in int)
  return number
  is 
    LReturnVal NUMBER(28,8);
  begin
    LReturnVal := 1;
    for i in 1..AValue loop
      LReturnVal := LReturnVal * i;
    end loop;
    return LReturnVal;
  end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSReadSecond(ATimeSpan in number)
	return number
	is 
		LReturnVal number(20,0);
	begin
		LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (10000000 * 60)) * 60);
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSReadMinute(ATimeSpan in number)
	return number
	is 
		LReturnVal number(20,0);
	begin
		LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (600000000 * 60)) * 60);
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSReadHour(ATimeSpan in number)
	return number
	is 
		LReturnVal number(20,0);
	begin
		LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (36000000000 * 24)) * 24);
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSReadDay(ATimeSpan in number)
	return number
	is 
		LReturnVal number(20,0);
	begin
		LReturnVal := TRUNC(ATimeSpan / 864000000000);
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSWriteMillisecond(ATimeSpan in number, APart int)
	return number
	is
		LReturnVal number(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMillisecond(ATimeSpan)) * 10000;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSWriteSecond(ATimeSpan in number, APart int)
	return number
	is
		LReturnVal number(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMillisecond(ATimeSpan)) * 10000000;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSWriteMinute(ATimeSpan in number, APart int)
	return number
	is
		LReturnVal number(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMillisecond(ATimeSpan)) * 600000000;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSWriteHour(ATimeSpan in number, APart int)
	return number
	is
		LReturnVal number(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMillisecond(ATimeSpan)) * 36000000000;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSWriteDay(ATimeSpan in number, APart int)
	return number
	is
		LReturnVal number(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMillisecond(ATimeSpan)) * 864000000000;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_AddYears(ADateTime in date, AYears in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := ADD_MONTHS(ADateTime, AYears * 12);
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_Today
	return date
	is
		LReturnVal date;
	begin
		LReturnVal := TRUNC(SysDate);
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DaysInMonth(AYear in integer, AMonth in int)
	return integer
	is
		LReturnVal int;
	begin
		/*LDate := To_Date(1 + AMonth * 100 + AYear * 10000,'YYYY MM DD');cannot create variables*/
		LReturnVal := Last_Day(To_Date(1 + AMonth * 100 + AYear * 10000,'YYYY MM DD')) - To_Date(1 + AMonth * 100 + AYear * 10000,'YYYY MM DD') + 1;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_IsLeapYear(AYear in int)
	return int
	is 
		LReturnVal int;
	begin
		LReturnVal := To_Date('01/03/' || To_Char(AYear),'dd/mm/yyyy') - To_Date('28/02/' || To_Char(AYear),'dd/mm/yyyy') - 1;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTReadDay(ADateTime in date)
	return int
	is
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'dd');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTReadMonth(ADateTime in date)
	return int
	is
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'mm');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTReadYear(ADateTime in date)
	return int
	is
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'yyyy');
		return LReturnVal;
	end;
					"
				)
			);

//			EnsureOperatorDDLCommands.Add
//			(
//				new EnsureOperatorDDL
//				(
//					@"
//create or replace function DAE_DTReadHour(ADateTime in date)
//	return int
//	is
//		LReturnVal int;
//	begin
//		LReturnVal := To_Char(ADateTime, 'hh');
//		return LReturnVal;
//	end;
//					"
//				)
//			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTReadMinute(ADateTime in date)
	return int
	is
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'mi');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTReadSecond(ADateTime in date)
	return int
	is
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTReadMillisecond(ADateTime in date)
	return int
	is
		LReturnVal int;
	begin
		LReturnVal := 0;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DayOfYear(ADateTime in date)
	return int
	is 
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'ddd');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DayOfWeek(ADateTime in date)
	return int
	is 
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'd');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteMillisecond(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := ADateTime;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteSecond(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(APart), 'yyyy/mm/dd hh24:mi:ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteMinute(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(APart) || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteHour(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(APart) || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteDay(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(APart) || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteMonth(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(APart) || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTWriteYear(ADateTime in date, APart in int)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := To_Date(To_Char(APart) || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_TSDateTime(ATimeSpan in number)
	return date
	is 
		LReturnVal Date;
	begin
		LReturnVal := round((ATimeSpan - 630822816000000000)/864000000000,0) + To_Date(20000101 * 100000 + round(mod(ATimeSpan/10000000 , 86400),0), 'yyyy dd mm sssss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DTTimeSpan(ADateTime in date)
	return number
	is 
		LReturnVal number;
	begin
		LReturnVal := 631139040000000000 + ((ADateTime - To_Date('01-JAN-2001')) * 86400 + to_char(ADateTime,'sssss')) * 10000000;
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector1(AYear in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date(AYear * 10000 + 0101, 'yyyy mm dd');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector2(AYear in int, AMonth in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date(AYear * 10000 + AMonth * 100 + 01, 'yyyy mm dd');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector3(AYear in int, AMonth in int, ADay in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date(AYear * 10000 + AMonth * 100 + ADay, 'yyyy mm dd');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector4(AYear in int, AMonth in int, ADay in int, AHour in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date((AYear * 1000000 + AMonth * 10000 + ADay * 100 + AHour), 'yyyy mm dd hh24');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector5(AYear in int, AMonth in int, ADay in int, AHour in int, AMinute in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date((AYear * 100000000 + AMonth * 1000000 + ADay * 10000 + AHour * 100 + AMinute), 'yyyy mm dd hh24 mi');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector6(AYear in int, AMonth in int, ADay in int, AHour in int, AMinute in int, ASecond in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date((AYear * 10000000000 + AMonth * 100000000 + ADay * 1000000 + AHour * 10000 + AMinute * 100 + ASecond), 'yyyy mm dd hh24 mi ss');
		return LReturnVal;
	end;
					"
				)
			);

			EnsureOperatorDDLCommands.Add
			(
				new EnsureOperatorDDL
				(
					@"
create or replace function DAE_DateTimeSelector7(AYear in int, AMonth in int, ADay in int, AHour in int, AMinute in int, ASecond in int, AMillisecond in int)
	return date
	is 
		LReturnVal date;
	begin
		LReturnVal := to_Date((AYear * 10000000000 + AMonth * 100000000 + ADay * 1000000 + AHour * 10000 + AMinute * 100 + ASecond), 'yyyy mm dd hh24 mi ss');
		return LReturnVal;
	end;
					"
				)
			);
		}
	}
}