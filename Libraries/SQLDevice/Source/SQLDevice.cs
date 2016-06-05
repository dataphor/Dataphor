/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Alphora.Dataphor.DAE.Device.SQL
{
	using System.Collections.Generic;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling.Visitors;
	
	/*
		Meta Data tags controlling SQL translation and reconciliation ->
		
			Storage.Name -> indicates that the object on which it appears should be identified by the value of the tag when referenced within the device
			Storage.DomainName -> indicates that the column on which it appears should be declared to be of the given domain name. This can be used to specify a user-defined domain in the target system without having to declare a type in D4
			Storage.NativeDomainName -> used to indicate the underlying native domain name for a given column, type, or type map. This tag is used to manage device reconciliation.
			Storage.Schema -> indicates that the object on which it appears should be schema qualified by the value of the tag when referenced within the device
			Storage.Length -> indicates the storage length for translated scalar types
			Storage.Precision -> indicates the storage precision for exact numeric data in translated scalar types
			Storage.Scale -> indicates the storage scale for exact numeric data in translated scalar types
			Storage.Deferred -> indicates that the values for the column or scalar type on which it appears should be read entirely as overflow
			Storage.Enforced -> indicates that the constraint on which it appears is enforced by the device and should not be enforced by the DAE // Deprecated, replaced by DAE.Enforced (opposite semantics)
			Storage.IsImposedKey -> indicates that the key on which it appears was imposed by catalog reconciliation and must be ensured with a distinct clause in select statements referencing the owning table
			Storage.IsClustered -> Indicates that the key on which it appears should be defined as a clustered index // Deprecated, replaced by DAE.IsClustered, same semantics
			Storage.ShouldReconcile -> Indicates that the object on which it appears should be reconciled with the target system.

		Basic Data type mapping ->
			These data type mappings are provided for by the SQLScalarType descendent classes in this namespace.
			They are not registered by the base SQLDevice.  It is up to each device to decide whether the mapping
			is appropriate and take the necessary action to establish the mapping.
		
			DAE Type	|	ANSI SQL Type													|	Translation Handler
			------------|-------------------------------------------------------------------|------------------------
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
			DateTime	|	date															|	SQLDateTime
			TimeSpan	|	bigint															|	SQLTimeSpan
			Date		|	date															|	SQLDate
			Time		|	time															|	SQLTime
			Money		|	decimal(28, 8)													|	SQLMoney
			Guid		|	char(36)														|	SQLGuid
			String		|	varchar(Storage.Length)											|	SQLString
			SQLDateTime	|	date															|	SQLDateTime
			SQLText		|	clob															|	SQLText
			Binary		|	blob															|	SQLBinary

		D4 to SQL translation ->
		
			Retrieve - select expression with single table in the from clause
			Restrict - adds a where clause to the expression, if it exists, conjoins to the existing clause
			Project - if there is already a select list, push the entire expression to a nested from clause
			Extend - add the extend columns to the current select list
			Rename - should translate to aliasing in most cases
			Adorn - ignored
			Aggregate - if there is already a select list, push the entire expression to a nested from clause
			Union - add the right side of the expression to a union expression (always distinct)
			Difference - unsupported
			Join (all flavors) - add the right side of the expression to a nested from clause if necessary 
			Order - skip unless this is the outer node
			Browse - unsupported
			CreateTable - direct translation
			AlterTable - direct translation
			DropTable - direct translation
			
		Basic Operator Mapping ->

			Arithmetic operators ->
				+ - * / % ** (-)
			
			Comparison operators ->
				= <> > >= < <= ?= like between matches
			
			Logical operators ->
				and or xor not
			
			Bitwise operators ->
				& | ^ ~ << >>
			
			Existential operators ->
				exists in
			
			Relational operators ->
				retrieve restrict project rename extend aggregate order union join
			 
			Aggregate operators ->
				sum min max avg count all any
			
			String operators ->
				Length Copy Pos Upper Lower
				
			Guid operators ->
				NewGuid
			
			Conversion operators ->
				ToXXX
			
			Datetime operators ->
				Date Time Now Today DayOfMonth DayOfWeek DayOfYear DaysInMonth IsLeapYear AddMonths AddYears
			
			Null handling operators ->
				IsNull IfNull
			
			Conditional operators ->
				when case
				
		Catalog Importing ->
		
			Catalog description expects that the GetDeviceTablesExpression return result sets in the following form:
			
				// This result is expected to be ordered by TableSchema, TableName, OrdinalPosition (order in the table)
				TableSchema varchar(128),
				TableName varchar(128),
				ColumnName varchar(128),
				OrdinalPosition integer,
				TableTitle varchar(128), // if available, else default to TableName
				ColumnTitle varchar(128), // if available, else default to ColumnName
				NativeDomainName varchar(128), // this should be the native scalar type name
				DomainName varchar(128), // user-defined domain name, if available (nil if the column uses a native domain name)
				Length integer, // fixed length column size
				IsNullable integer, // 0 = false, 1 = true
				IsDeferred integer, // 0 = false, 1 = true, indicates whether deferred read should be used to access data in this column
			
			Catalog description expects that the GetDeviceIndexesExpression return result sets in the following form:

				// This result is expected to be ordered by TableSchema, TableName, IndexName, OrdinalPosition (order in the index)
				TableSchema varchar(128),
				TableName varchar(128),
				IndexName varchar(128),
				ColumnName varchar(128),
				OrdinalPosition integer,
				IsUnique integer, // 0 = false, 1 = true
				IsDescending integer, // 0 = false, 1 = true

			Catalog description expects that the GetDeviceForeignKeysExpression return result sets in the following form:
			
				// This result is expected to be ordered by ConstraintSchema, ConstraintName, OrdinalPosition
				ConstraintSchema varchar(128),
				ConstraintName varchar(128),
				SourceTableSchema varchar(128),
				SourceTableName varchar(128),
				SourceColumnName varchar(128),
				TargetTableSchema varchar(128),
				TargetTableName varchar(128),
				TargetColumnName varchar(128)
				OrdinalPosition integer

			These expressions are requested by the default implementation of GetDeviceCatalog.  This behavior can be overridden
			as necessary by descendent devices to provide support for database specific catalog features.
	*/
	
	public abstract class SQLDevice : Device
	{
		public const string SQLDateTimeScalarType = "SQLDevice.SQLDateTime";
		public const string SQLTimeScalarType = "SQLDevice.SQLTime";
		public const string SQLTextScalarType = "SQLDevice.SQLText";
		public const string SQLITextScalarType = "SQLDevice.SQLIText";

		public SQLDevice(int iD, string name) : base(iD, name)
		{
			SetMaxIdentifierLength();
			_supportsTransactions = true;
		}

		// Schema
		private string _schema = String.Empty;
		public string Schema
		{
			get { return _schema; }
			set { _schema = value == null ? String.Empty : value; }
		}

		private bool _useStatementTerminator = true;
		public bool UseStatementTerminator
		{
			get { return _useStatementTerminator; }
			set { _useStatementTerminator = value; }
		}
		
		private bool _useParametersForCursors = false;
		public bool UseParametersForCursors
		{
			get { return _useParametersForCursors; }
			set { _useParametersForCursors = value; }
		}
		
		private bool _shouldNormalizeWhitespace = true;
		public bool ShouldNormalizeWhitespace
		{
			get { return _shouldNormalizeWhitespace; }
			set { _shouldNormalizeWhitespace = value; }
		}

		private bool _useQuotedIdentifiers = true;
		public bool UseQuotedIdentifiers
		{
			set { _useQuotedIdentifiers = value;}
			get { return _useQuotedIdentifiers;}
		}
		
		private bool _useTransactions = true;
		/// <summary>Indicates whether or not to use transactions through the CLI of the target system.</summary>
		public bool UseTransactions
		{
			set { _useTransactions = value; }
			get { return _useTransactions; }
		}
		
		private bool _useQualifiedNames = false;
		/// <summary>Indicates whether or not to use the fully qualified name of an object in D4 to produce the storage name for the object.</summary>
		/// <remarks>Qualifiers will be replaced with underscores in the resulting storage name.</remarks>
		public bool UseQualifiedNames
		{
			get { return _useQualifiedNames; }
			set { _useQualifiedNames = value; }
		}
		
		private bool _supportsAlgebraicFromClause = true;
		/// <summary>Indicates whether or not the dialect supports an algebraic from clause.</summary>
		public bool SupportsAlgebraicFromClause
		{
			get { return _supportsAlgebraicFromClause; }
			set { _supportsAlgebraicFromClause = value; }
		}
		
		private string _onExecuteConnectStatement;
		/// <summary>A D4 expression denoting an SQL statement in the target dialect to be executed on all new execute connections.</summary>
		/// <remarks>The statement will be executed in its own transaction, not the transactional context of the spawning process.</remarks>
		public string OnExecuteConnectStatement
		{
			get { return _onExecuteConnectStatement; }
			set { _onExecuteConnectStatement = value; }
		}
		
		private string _onBrowseConnectStatement;
		/// <summary>A D4 expression denoting an SQL statement in the target dialect to be executed on all new browse connections.</summary>
		/// <remarks>The statement will be executed in its own transaction, not the transactional context of the spawning process.</remarks>
		public string OnBrowseConnectStatement
		{
			get { return _onBrowseConnectStatement; }
			set { _onBrowseConnectStatement = value; }
		}
		
		private bool _supportsSubSelectInSelectClause = true;
		public bool SupportsSubSelectInSelectClause
		{
			get { return _supportsSubSelectInSelectClause; }
			set { _supportsSubSelectInSelectClause = value; }
		}
		
		private bool _supportsSubSelectInWhereClause = true;
		public bool SupportsSubSelectInWhereClause
		{
			get { return _supportsSubSelectInWhereClause; }
			set { _supportsSubSelectInWhereClause = value; }
		}
		
		private bool _supportsSubSelectInGroupByClause = true;
		public bool SupportsSubSelectInGroupByClause
		{
			get { return _supportsSubSelectInGroupByClause; }
			set { _supportsSubSelectInGroupByClause = value; }
		}
		
		private bool _supportsSubSelectInHavingClause = true;
		public bool SupportsSubSelectInHavingClause
		{
			get { return _supportsSubSelectInHavingClause; }
			set { _supportsSubSelectInHavingClause = value; }
		}
		
		private bool _supportsSubSelectInOrderByClause = true;
		public bool SupportsSubSelectInOrderByClause
		{
			get { return _supportsSubSelectInOrderByClause; }
			set { _supportsSubSelectInOrderByClause = value; }
		}
		
		// True if the device supports nesting in the from clause.
		// This is also used to determine whether an extend whose source has introduced columns (an add following another add), will nest the resulting expression, or use replacement referencing to avoid the nesting.
		private bool _supportsNestedFrom = true;
		public bool SupportsNestedFrom
		{
			get { return _supportsNestedFrom; }
			set { _supportsNestedFrom = value; }
		}

		private bool _supportsNestedCorrelation = true;
		public bool SupportsNestedCorrelation
		{
			get { return _supportsNestedCorrelation; }
			set { _supportsNestedCorrelation = value; }
		}

		// True if the device supports the use of expressions in the order by clause		
		private bool _supportsOrderByExpressions = false;
		public bool SupportsOrderByExpressions
		{
			get { return _supportsOrderByExpressions; }
			set { _supportsOrderByExpressions = value; }
		}

		// True if the device supports the ISO standard syntax "NULLS FIRST/LAST" in the order by clause
		private bool _supportsOrderByNullsFirstLast = false;
		public bool SupportsOrderByNullsFirstLast
		{
			get { return _supportsOrderByNullsFirstLast; }
			set { _supportsOrderByNullsFirstLast = value; }
		}

		// True if the order by clause is processed as part of the query context 
		// If this is false the order must be specified in terms of the result set columns, rather than the range variable columns within the query
		private bool _isOrderByInContext = true;
		public bool IsOrderByInContext
		{
			get { return _isOrderByInContext; }
			set { _isOrderByInContext = value; }
		}

		// True if insert statements should be constructed using a values clause, false to use a select expression		
		private bool _useValuesClauseInInsert = true;
		public bool UseValuesClauseInInsert
		{
			get { return _useValuesClauseInInsert; }
			set { _useValuesClauseInInsert = value; }
		}
		
		private int _commandTimeout = -1;
		/// <summary>The amount of time in seconds to wait before timing out waiting for a command to complete.</summary>
		/// <remarks>
		/// The default value for this property is -1, indicating that the default timeout value for the connectivity 
		/// implementation used by the device should be used. Beyond that, the interpretation of this value depends on
		/// the connectivity implementation used by the device. For most implementations, a value of 0 indicates an
		/// infinite timeout.
		/// </remarks>
		public int CommandTimeout
		{
			get { return _commandTimeout; }
			set { _commandTimeout = value; }
		}

		#if USEISTRING		
		private bool FIsCaseSensitive;
		public bool IsCaseSensitive
		{
			get { return FIsCaseSensitive; }
			set { FIsCaseSensitive = value; }
		}
		#endif
		
		public virtual ErrorSeverity ExceptionOccurred(Exception exception)
		{
			return ErrorSeverity.Application;
		}

		protected int _maxIdentifierLength = int.MaxValue;
		public int MaxIdentifierLength
		{
			get { return _maxIdentifierLength; }
			set { _maxIdentifierLength = value; }
		}
		
		protected virtual void SetMaxIdentifierLength() {}

		// verify that all types in the given table are mapped into this device
        public override void CheckSupported(Plan plan, TableVar tableVar)
        {
			// verify that the types of all columns have type maps
			foreach (Schema.Column column in tableVar.DataType.Columns)
				if (!(column.DataType is Schema.ScalarType) || (ResolveDeviceScalarType(plan, (Schema.ScalarType)column.DataType) == null))
					if (Compiler.CouldGenerateDeviceScalarTypeMap(plan, this, (Schema.ScalarType)column.DataType))
					{
						D4.AlterDeviceStatement statement = new D4.AlterDeviceStatement();
						statement.DeviceName = Name;
						// BTR 1/18/2007 -> This really should be being marked as generated, however doing so
						// changes the dependency reporting for scalar type maps, and causes some catalog dependency errors,
						// so I cannot justify making this change in this version. Perhaps at some point, but not now...
						statement.CreateDeviceScalarTypeMaps.Add(new D4.DeviceScalarTypeMap(column.DataType.Name));
						plan.ExecuteNode(Compiler.Compile(plan, statement));
						ResolveDeviceScalarType(plan, (Schema.ScalarType)column.DataType); // Reresolve to attach a dependency to the generated map
					}
					else
						throw new SchemaException(SchemaException.Codes.UnsupportedScalarType, tableVar.Name, Name, column.DataType.Name, column.Name);
					
			foreach (Schema.Key key in tableVar.Keys)
				foreach (Schema.TableVarColumn column in key.Columns)
					if (!SupportsComparison(plan, column.DataType))
						throw new SchemaException(SchemaException.Codes.UnsupportedKeyType, tableVar.Name, Name, column.DataType.Name, column.Name);
        }								   
        
        public override D4.ClassDefinition GetDefaultOperatorClassDefinition(D4.MetaData metaData)
        {
			if ((metaData != null) && (metaData.Tags.Contains("Storage.TranslationString")))
				return 
					new D4.ClassDefinition
					(
						"SQLDevice.SQLUserOperator", 
						new D4.ClassAttributeDefinition[]
						{
							new D4.ClassAttributeDefinition("TranslationString", metaData.Tags["Storage.TranslationString"].Value),
							new D4.ClassAttributeDefinition("ContextLiteralParameterIndexes", D4.MetaData.GetTag(metaData, "Storage.ContextLiteralParameterIndexes", ""))
						}
					);
			return null;
        }
        
        public override D4.ClassDefinition GetDefaultSelectorClassDefinition()
        {
			return new D4.ClassDefinition("SQLDevice.SQLScalarSelector");
        }
        
        public override D4.ClassDefinition GetDefaultReadAccessorClassDefinition()
        {
			return new D4.ClassDefinition("SQLDevice.SQLScalarReadAccessor");
        }
        
        public override D4.ClassDefinition GetDefaultWriteAccessorClassDefinition()
        {
			return new D4.ClassDefinition("SQLDevice.SQLScalarWriteAccessor");
        }
        
		public override DeviceCapability Capabilities 
		{ 
			get 
			{ 
				return 
					DeviceCapability.RowLevelInsert | 
					DeviceCapability.RowLevelUpdate | 
					DeviceCapability.RowLevelDelete;
			}
		}
		
		protected override void InternalRegister(ServerProcess process)
		{
			base.InternalRegister(process);
			RegisterSystemObjectMaps(process);
		}
		
		protected void RunScript(ServerProcess process, string script)
		{
			// Note that this is also used to load the internal system catalog.
			IServerScript localScript = ((IServerProcess)process).PrepareScript(script);
			try
			{
				localScript.Execute(null);
			}
			finally
			{
				((IServerProcess)process).UnprepareScript(localScript);
			}
		}
		
		protected virtual void RegisterSystemObjectMaps(ServerProcess process) {}
		
		// Emitter
		public virtual TextEmitter Emitter
		{
			get
			{
				SQLTextEmitter emitter = InternalCreateEmitter();
				emitter.UseStatementTerminator = UseStatementTerminator;
				emitter.UseQuotedIdentifiers = UseQuotedIdentifiers;					
				return emitter;
			}
		}

		protected virtual SQLTextEmitter InternalCreateEmitter()
		{
			return new SQLTextEmitter();
		}

		// Prepare		
		protected override DevicePlan CreateDevicePlan(Plan plan, PlanNode planNode)
		{
			return new SQLDevicePlan(plan, this, planNode);
		}
		
		protected override DevicePlanNode InternalPrepare(DevicePlan plan, PlanNode planNode)
		{
			SQLDevicePlan devicePlan = (SQLDevicePlan)plan;

			if (planNode is TableNode)
			{
				devicePlan.DevicePlanNode = new TableSQLDevicePlanNode(planNode);
			}
			else if (planNode.DataType is Schema.IRowType)
			{
				var rowDeviceNode = new RowSQLDevicePlanNode(planNode);

				var rowType = (Schema.IRowType)planNode.DataType;
				for (var index = 0; index < rowType.Columns.Count; index++)
				{
					if (rowType.Columns[index].DataType is IScalarType)
					{
						rowDeviceNode.MappedTypes.Add((SQLScalarType)ResolveDeviceScalarType(devicePlan.Plan, (ScalarType)rowType.Columns[index].DataType));
					}
					else
					{
						rowDeviceNode.MappedTypes.Add(null); // The expression will not be supported anyway, so report nothing for the type map.
					}
				}

				devicePlan.DevicePlanNode = rowDeviceNode;
			}
			else if (planNode.DataType is Schema.IScalarType)
			{
				var scalarDeviceNode = new ScalarSQLDevicePlanNode(planNode);
				scalarDeviceNode.MappedType = (SQLScalarType)ResolveDeviceScalarType(devicePlan.Plan, (ScalarType)planNode.DataType);
				devicePlan.DevicePlanNode = scalarDeviceNode;
			}
			else
			{
				devicePlan.DevicePlanNode = new SQLDevicePlanNode(planNode);
			}

			if (!((planNode is TableNode) || (planNode.DataType is Schema.IRowType)))
			{
				devicePlan.PushScalarContext();
				devicePlan.CurrentQueryContext().IsSelectClause = true;
			}
			try
			{
				if ((planNode.DataType != null) && planNode.DataType.Is(plan.Plan.Catalog.DataTypes.SystemBoolean))
					devicePlan.EnterContext(true);
				try
				{
					devicePlan.DevicePlanNode.Statement = Translate(devicePlan, planNode);

					if (devicePlan.IsSupported)
					{
						if (planNode.DataType != null)
						{
							if (planNode.DataType.Is(plan.Plan.Catalog.DataTypes.SystemBoolean))
							{
								devicePlan.DevicePlanNode.Statement =
									new CaseExpression
									(
										new CaseItemExpression[]
										{
											new CaseItemExpression
											(
												(Expression)devicePlan.DevicePlanNode.Statement,
												new ValueExpression(1)
											)
										},
										new CaseElseExpression(new ValueExpression(0))
									);
							}
							
							// Ensure that the statement is a unary select expression for the device Cursors...
							if ((planNode is TableNode) && (devicePlan.DevicePlanNode.Statement is QueryExpression) && (((QueryExpression)devicePlan.DevicePlanNode.Statement).TableOperators.Count > 0))
								devicePlan.DevicePlanNode.Statement = NestQueryExpression(devicePlan, ((TableNode)planNode).TableVar, devicePlan.DevicePlanNode.Statement);

							if (!(devicePlan.DevicePlanNode.Statement is SelectStatement))
							{
								if (!(devicePlan.DevicePlanNode.Statement is QueryExpression))
								{
									if (!(devicePlan.DevicePlanNode.Statement is SelectExpression))
									{
										SelectExpression selectExpression = new SelectExpression();
										selectExpression.SelectClause = new SelectClause();
										selectExpression.SelectClause.Columns.Add(new ColumnExpression((Expression)devicePlan.DevicePlanNode.Statement, "dummy1"));
										selectExpression.FromClause = new CalculusFromClause(GetDummyTableSpecifier());
										devicePlan.DevicePlanNode.Statement = selectExpression;
									}

									QueryExpression queryExpression = new QueryExpression();
									queryExpression.SelectExpression = (SelectExpression)devicePlan.DevicePlanNode.Statement;
									devicePlan.DevicePlanNode.Statement = queryExpression;
								}

								SelectStatement selectStatement = new SelectStatement();
								selectStatement.QueryExpression = (QueryExpression)devicePlan.DevicePlanNode.Statement;
								devicePlan.DevicePlanNode.Statement = selectStatement;
							}
							
							if (planNode is TableNode)
							{
								devicePlan.DevicePlanNode.Statement = TranslateOrder(devicePlan, (TableNode)planNode, (SelectStatement)devicePlan.DevicePlanNode.Statement, false);
								((TableSQLDevicePlanNode)devicePlan.DevicePlanNode).IsAggregate = devicePlan.CurrentQueryContext().IsAggregate;
								if (!devicePlan.IsSupported)
									return null;
							}
						}
								
						return devicePlan.DevicePlanNode;
					}

					return null;
				}
				finally
				{
					if ((planNode.DataType != null) && planNode.DataType.Is(plan.Plan.Catalog.DataTypes.SystemBoolean))
						devicePlan.ExitContext();
				}
			}
			finally
			{
				// TODO: This condition is different than the push context above, is this intentional?
				if (!(planNode is TableNode))
					devicePlan.PopScalarContext();
			}
		}

		public virtual void DetermineCursorBehavior(Plan plan, TableNode tableNode)
		{
			tableNode.RequestedCursorType = plan.CursorContext.CursorType;
			tableNode.CursorType = plan.CursorContext.CursorType;
			tableNode.CursorCapabilities = 
				CursorCapability.Navigable | 
				(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) |
				(plan.CursorContext.CursorCapabilities & CursorCapability.Elaborable);
			tableNode.CursorIsolation = plan.CursorContext.CursorIsolation;
			
			// Ensure that the node has an order that is a superset of some key
			Schema.Key clusteringKey = Compiler.FindClusteringKey(plan, tableNode.TableVar);
			if ((tableNode.Order == null) && (clusteringKey.Columns.Count > 0))
				tableNode.Order = Compiler.OrderFromKey(plan, clusteringKey);

			if (tableNode.Order != null)
			{
				// Ensure that the order is unique
				bool orderUnique = false;	
				Schema.OrderColumn newColumn;

				foreach (Schema.Key key in tableNode.TableVar.Keys)
					if (Compiler.OrderIncludesKey(plan, tableNode.Order, key))
					{
						orderUnique = true;
						break;
					}

				if (!orderUnique)
					foreach (Schema.TableVarColumn column in Compiler.FindClusteringKey(plan, tableNode.TableVar).Columns)
						if (!tableNode.Order.Columns.Contains(column.Name))
						{
							newColumn = new Schema.OrderColumn(column, true);
							newColumn.Sort = Compiler.GetSort(plan, column.DataType);
							newColumn.IsDefaultSort = true;
							tableNode.Order.Columns.Add(newColumn);
						}
						else 
						{
							if (!System.Object.ReferenceEquals(tableNode.Order.Columns[column.Name].Sort, ((ScalarType)tableNode.Order.Columns[column.Name].Column.DataType).UniqueSort))
								tableNode.Order.Columns[column.Name].Sort = Compiler.GetUniqueSort(plan, (ScalarType)column.DataType);
						}
			}
		}

		/// <summary>
		/// This method returns a valid identifier for the specific SQL system in which it is called.  If overloaded, it must be completely deterministic.
		/// </summary>
		/// <param name="identifier">The identifier to check</param>
		/// <returns>A valid identifier, based as close as possible to AIdentifier</returns>
		public string EnsureValidIdentifier(string identifier)
		{
			return EnsureValidIdentifier(identifier, _maxIdentifierLength);
		}

		public virtual unsafe string EnsureValidIdentifier(string identifier, int maxLength)
		{
			// first check to see if it is not reserved
			if (IsReservedWord(identifier))
			{
				identifier += "_DAE";
			}
			
			// Replace all double underscores with triple underscores
			identifier = identifier.Replace("__", "___");

			// Replace all qualifiers with double underscores
			identifier = identifier.Replace(".", "__");

			// then check to see if it has a name longer than AMaxLength characters
			if (identifier.Length > maxLength)
			{
				byte[] byteArray = new byte[4];
				fixed (byte* array = &byteArray[0])
				{
					*((int*)array) = identifier.GetHashCode();
				}
				string stringValue = Convert.ToBase64String(byteArray);
				identifier = identifier.Substring(0, maxLength - stringValue.Length) + stringValue;
			}

			// replace invalid characters
			identifier = identifier.Replace("+", "_");
			identifier = identifier.Replace("/", "_");
			identifier = identifier.Replace("=", "_");
			return identifier;
		}

		protected virtual bool IsReservedWord(string word) { return false; }

		public string ToSQLIdentifier(string identifier)
		{
			return EnsureValidIdentifier(identifier);
		}
		
		public virtual string ToSQLIdentifier(string identifier, D4.MetaData metaData)
		{
			return D4.MetaData.GetTag(metaData, "Storage.Name", ToSQLIdentifier(identifier));
		}
		
		//public virtual string ToSQLIdentifier(string AIdentifier, D4.MetaData AMetaData)
		public virtual string ToSQLIdentifier(Schema.Object objectValue)
		{
			if ((objectValue is CatalogObject) && (((CatalogObject)objectValue).Library != null) && !UseQualifiedNames)
				return ToSQLIdentifier(DAE.Schema.Object.RemoveQualifier(objectValue.Name, ((CatalogObject)objectValue).Library.Name), objectValue.MetaData);
			else
				return ToSQLIdentifier(objectValue.Name, objectValue.MetaData);
		}

		public virtual string ConvertNonIdentifierCharacter(char invalidChar)
		{
			switch (invalidChar)
			{
				case '#' : return "POUND";
				case '~' : return "TILDE";
				case '%' : return "PERCENT";
				case '^' : return "CARET";
				case '&' : return "AMP";
				case '(' : return "LPAR";
				case ')' : return "RPAR";
				case '-' : return "HYPHEN";
				case '{' : return "LBRACE";
				case '}' : return "RBRACE";
				case '\'' : return "APOS";
				case '.' : return "PERIOD";
				case '\\' : return "BSLASH";
				case '/' : return "FSLASH";
				case '`' : return "ACCENT";
				default : return Convert.ToUInt16(invalidChar).ToString();
			}
		}

		public virtual string FromSQLIdentifier(string identifier)
		{
			StringBuilder localIdentifier = new StringBuilder();
			for (int index = 0; index < identifier.Length; index++)
				if (!Char.IsLetterOrDigit(identifier[index]) && (identifier[index] != '_'))
					localIdentifier.Append(ConvertNonIdentifierCharacter(identifier[index]));
				else
					localIdentifier.Append(identifier[index]);
			identifier = localIdentifier.ToString();

			if (D4.ReservedWords.Contains(identifier) || ((identifier.Length > 0) && Char.IsDigit(identifier[0])))
				identifier = String.Format("_{0}", identifier);
			return identifier;
		}
		
		public virtual SelectExpression FindSelectExpression(Statement statement)
		{
			if (statement is QueryExpression)
				return ((QueryExpression)statement).SelectExpression;
			else if (statement is SelectExpression)
				return (SelectExpression)statement;
			else
				throw new SQLException(SQLException.Codes.InvalidStatementClass);
		}

		/// <remarks> In some contexts (such as an add) it is desireable to nest contexts to avoid duplicating entire 
		/// extend expressions.  Such contexts should pass true to ANestIfSupported.  Theoretically, each such context
		/// could detect references to the introduced columns before nesting, but for now their presence is used. </remarks>
		public virtual SelectExpression EnsureUnarySelectExpression(SQLDevicePlan devicePlan, TableVar tableVar, Statement statement, bool nestIfSupported)
		{
			if ((statement is QueryExpression) && (((QueryExpression)statement).TableOperators.Count > 0))
				return NestQueryExpression(devicePlan, tableVar, statement);
			else if (nestIfSupported && devicePlan.Device.SupportsNestedFrom && (devicePlan.CurrentQueryContext().AddedColumns.Count > 0))
			{
				devicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The query is being nested to avoid unnecessary repetition of column expressions."));
				return NestQueryExpression(devicePlan, tableVar, statement);
			}
			else
				return FindSelectExpression(statement);
		}

		public virtual SelectExpression NestQueryExpression(SQLDevicePlan devicePlan, TableVar tableVar, Statement statement)
		{
			if (!devicePlan.Device.SupportsNestedFrom)
			{
				devicePlan.IsSupported = false;
				devicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the device does not support nesting in the from clause."));
			}
			
			if (((devicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasCorrelation) != 0) && !devicePlan.Device.SupportsNestedCorrelation)
			{
				devicePlan.IsSupported = false;
				devicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the device does not support nested correlation."));
			}
				
			SQLRangeVar rangeVar = new SQLRangeVar(devicePlan.GetNextTableAlias());
			foreach (TableVarColumn column in tableVar.Columns)
			{
				SQLRangeVarColumn nestedRangeVarColumn = devicePlan.CurrentQueryContext().GetRangeVarColumn(column.Name);
				SQLRangeVarColumn rangeVarColumn = new SQLRangeVarColumn(column, rangeVar.Name, nestedRangeVarColumn.Alias);
				rangeVar.Columns.Add(rangeVarColumn);
			}
			
			devicePlan.PopQueryContext();
			devicePlan.PushQueryContext();
			
			devicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
			SelectExpression selectExpression = devicePlan.Device.FindSelectExpression(statement);

			SelectExpression newSelectExpression = new SelectExpression();
			if (selectExpression.FromClause is AlgebraicFromClause)
				newSelectExpression.FromClause = new AlgebraicFromClause(new TableSpecifier((Expression)statement, rangeVar.Name));
			else
				newSelectExpression.FromClause = new CalculusFromClause(new TableSpecifier((Expression)statement, rangeVar.Name));
				
			newSelectExpression.SelectClause = new SelectClause();
			foreach (TableVarColumn column in tableVar.Columns)
				newSelectExpression.SelectClause.Columns.Add(devicePlan.CurrentQueryContext().GetRangeVarColumn(column.Name).GetColumnExpression());

			return newSelectExpression;
		}
		
		protected virtual Statement FromScalar(SQLDevicePlan devicePlan, PlanNode planNode)
		{
			SQLScalarType scalarType = (SQLScalarType)ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType);
			
			if (planNode.IsLiteral && planNode.IsDeterministic && !scalarType.UseParametersForLiterals)
			{
				object tempValue = devicePlan.Plan.EvaluateLiteralArgument(planNode, planNode.Description);
				
				Expression valueExpression = null;
				if (tempValue == null)
					valueExpression = new CastExpression(new ValueExpression(null, TokenType.Nil), scalarType.ParameterDomainName());
				else
					valueExpression = new ValueExpression(scalarType.ParameterFromScalar(devicePlan.Plan.ValueManager, tempValue));
				
				if (planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean) && devicePlan.IsBooleanContext())
					return new BinaryExpression(new ValueExpression(1), "iEqual", valueExpression);
				else
					return valueExpression;
			}
			else
			{
				string parameterName = String.Format("P{0}", devicePlan.DevicePlanNode.PlanParameters.Count + 1);
				SQLPlanParameter planParameter = 
					new SQLPlanParameter
					(
						new SQLParameter
						(
							parameterName, 
							scalarType.GetSQLParameterType(), 
							null, 
							SQLDirection.In, 
							GetParameterMarker(scalarType)
						), 
						planNode,
						scalarType
					);
				devicePlan.DevicePlanNode.PlanParameters.Add(planParameter);
				devicePlan.CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasParameters;
				if (planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean) && devicePlan.IsBooleanContext())
					return new BinaryExpression(new ValueExpression(1), "iEqual", new QueryParameterExpression(parameterName));
				else
					return new QueryParameterExpression(parameterName);
			}
		}

		public string GetParameterMarker(SQLScalarType scalarType, TableVarColumn column)
		{
			return GetParameterMarker(scalarType, column.MetaData);
		}

		public string GetParameterMarker(SQLScalarType scalarType)
		{
			return GetParameterMarker(scalarType, (D4.MetaData)null);
		}		
		
		public virtual string GetParameterMarker(SQLScalarType scalarType, D4.MetaData metaData)
		{
			return null;
		}
		
		protected virtual Statement TranslateStackReference(SQLDevicePlan devicePlan, StackReferenceNode node)
		{
			return FromScalar(devicePlan, new StackReferenceNode(node.DataType, node.Location - devicePlan.Stack.Count));
		}
		
		protected virtual Statement TranslateStackColumnReference(SQLDevicePlan devicePlan, StackColumnReferenceNode node)
		{
			// If this is referencing an item on the device plan stack, translate as an identifier,
			// otherwise, translate as a query parameter
			if (node.Location < devicePlan.Stack.Count)
			{
				Expression expression = new QualifiedFieldExpression();
				SQLRangeVarColumn rangeVarColumn = null;

				if (DAE.Schema.Object.Qualifier(node.Identifier) == Keywords.Left)
					rangeVarColumn = devicePlan.CurrentJoinContext().LeftQueryContext.FindRangeVarColumn(DAE.Schema.Object.Dequalify(node.Identifier));
				else if (DAE.Schema.Object.Qualifier(node.Identifier) == Keywords.Right)
					rangeVarColumn = devicePlan.CurrentJoinContext().RightQueryContext.FindRangeVarColumn(DAE.Schema.Object.Dequalify(node.Identifier));
				else
					rangeVarColumn = devicePlan.FindRangeVarColumn(node.Identifier, false);

				if (rangeVarColumn == null)
				{
					devicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the reference to column ""{0}"" is out of context.", node.Identifier), node));
					devicePlan.IsSupported = false;
				}
				else
					expression = rangeVarColumn.GetExpression();
				
				if (devicePlan.IsBooleanContext())
					return new BinaryExpression(expression, "iEqual", new ValueExpression(1)); // <> 0 is more robust, but = 1 is potentially more efficient

				return expression;
			}
			else
			{
				#if USECOLUMNLOCATIONBINDING
				return FromScalar(ADevicePlan, new StackColumnReferenceNode(ANode.Identifier, ANode.DataType, ANode.Location - ADevicePlan.Stack.Count, ANode.ColumnLocation));
				#else
				return FromScalar(devicePlan, new StackColumnReferenceNode(node.Identifier, node.DataType, node.Location - devicePlan.Stack.Count));
				#endif
			}
		}

		protected virtual Statement TranslateExtractColumnNode(SQLDevicePlan devicePlan, ExtractColumnNode node)
		{
			StackReferenceNode sourceNode = node.Nodes[0] as StackReferenceNode;
			if (sourceNode != null)
			{
				if (sourceNode.Location < devicePlan.Stack.Count)
				{
					Expression expression = new QualifiedFieldExpression();
					SQLRangeVarColumn rangeVarColumn = null;
					
					if (DAE.Schema.Object.EnsureUnrooted(sourceNode.Identifier) == Keywords.Left)
						rangeVarColumn = devicePlan.CurrentJoinContext().LeftQueryContext.FindRangeVarColumn(node.Identifier);
					else if (DAE.Schema.Object.EnsureUnrooted(sourceNode.Identifier) == Keywords.Right)
						rangeVarColumn = devicePlan.CurrentJoinContext().RightQueryContext.FindRangeVarColumn(node.Identifier);
					else
						rangeVarColumn = devicePlan.FindRangeVarColumn(DAE.Schema.Object.Qualify(node.Identifier, sourceNode.Identifier), false);

					if (rangeVarColumn == null)
					{
						devicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the reference to column ""{0}"" is out of context.", node.Identifier), node));
						devicePlan.IsSupported = false;
					}
					else
						expression = rangeVarColumn.GetExpression();
					
					if (devicePlan.IsBooleanContext())
						return new BinaryExpression(expression, "iEqual", new ValueExpression(1)); // <> 0 is more robust, but = 1 is potentially more efficient

					return expression;
				}
				else
					return FromScalar(devicePlan, new StackColumnReferenceNode(node.Identifier, node.DataType, sourceNode.Location - devicePlan.Stack.Count));
			}
			else
			{
				Statement statement = Translate(devicePlan, node.Nodes[0]);
				if (devicePlan.IsSupported)
				{
					SelectExpression expression = FindSelectExpression(statement);
					ColumnExpression columnExpression = expression.SelectClause.Columns[ToSQLIdentifier(((Schema.IRowType)node.Nodes[0].DataType).Columns[node.Identifier].Name)];
					expression.SelectClause.Columns.Clear();
					expression.SelectClause.Columns.Add(columnExpression);

					if (node.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean))
						if (!devicePlan.IsBooleanContext())
							statement =
								new CaseExpression
								(
									new CaseItemExpression[]
									{
										new CaseItemExpression
										(
											(Expression)statement,
											new ValueExpression(1)
										)
									}, 
									new CaseElseExpression(new ValueExpression(0))
								);
						else
							statement = new BinaryExpression((Expression)statement, "iEqual", new ValueExpression(1));
				}

				return statement;
			}
		}

		protected virtual string TooManyRowsOperator()
		{
			return "DAE_TooManyRows";
		}

		protected virtual Statement TranslateExtractRowNode(SQLDevicePlan devicePlan, ExtractRowNode node)
		{
			// Row extraction cannot be supported unless each column in the row being extracted has a map for equality comparison. Otherwise, the SQL Server
			// will complain that the 'blob column' cannot be used the given context (such as a subquery).
			foreach (Schema.Column column in ((Schema.IRowType)node.DataType).Columns)
				if (!SupportsEqual(devicePlan.Plan, column.DataType))
				{
					devicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support equality comparison for values of type ""{0}"" for column ""{1}"".", column.DataType.Name, column.Name), node));
					devicePlan.IsSupported = false;
					return new CallExpression();
				}

			// If the indexer is based on a key, Row extraction is translated as the expression
			// Otherwise, row extraction is translated as the expression, plus a where clause that counts the result and calls DAE_TooManyRows if the count is > 1
			// DAE_TooManyRows throws a TOO_MANY_ROWS exception, which is caught by the SQL Device and converted to an Application 105199, just as if it had been evaluated by the DAE
			// Note that in the case of MSSQL Server, the error is thrown by attempting to cast the message to an int.

			if (node.IsSingleton)
			{
				return TranslateExpression(devicePlan, node.Nodes[0], false);
			}
			else
			{
				Expression countExpression = TranslateExpression(devicePlan, node.Nodes[0], false);
				SelectExpression countSelectExpression = NestQueryExpression(devicePlan, ((TableNode)node.Nodes[0]).TableVar, countExpression);

				countSelectExpression.SelectClause = new SelectClause();
				AggregateCallExpression countCallExpression = new AggregateCallExpression();
				countCallExpression.Identifier = "Count";
				countCallExpression.Expressions.Add(new QualifiedFieldExpression("*"));
				countSelectExpression.SelectClause.Columns.Add(new ColumnExpression(countCallExpression));

				Expression extractionCondition =
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
										countSelectExpression,
										"iGreater",
										new ValueExpression(1)
									),
									new CallExpression(TooManyRowsOperator(), new Expression[] { new ValueExpression(1) })
								)
							},
							new CaseElseExpression(new ValueExpression(1))
						),
						"iEqual",
						new ValueExpression(1)
					);

				// Reset the query context and retranslate the source
				devicePlan.PopQueryContext();
				devicePlan.PushQueryContext();

				Expression sourceExpression = TranslateExpression(devicePlan, node.Nodes[0], false);
				SelectExpression selectExpression = EnsureUnarySelectExpression(devicePlan, ((TableNode)node.Nodes[0]).TableVar, sourceExpression, false);

				if (selectExpression.WhereClause == null)
					selectExpression.WhereClause = new WhereClause();

				if (selectExpression.WhereClause.Expression == null)
					selectExpression.WhereClause.Expression = extractionCondition;
				else
					selectExpression.WhereClause.Expression = new BinaryExpression(selectExpression.WhereClause.Expression, "iAnd", extractionCondition);

				return selectExpression;
			}
		}

		protected virtual Statement TranslateValueNode(SQLDevicePlan devicePlan, ValueNode node)
		{
			if (node.DataType.IsGeneric && ((node.Value == null) || (node.Value == DBNull.Value)))	
				return new ValueExpression(null);
			return FromScalar(devicePlan, node);
		}
		
		protected virtual Statement TranslateAsNode(SQLDevicePlan devicePlan, AsNode node)
		{
			return new CastExpression((Expression)Translate(devicePlan, node.Nodes[0]), ((SQLScalarType)devicePlan.Device.ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)node.DataType)).ParameterDomainName());
		}

		public virtual Expression TranslateExpression(SQLDevicePlan devicePlan, PlanNode node, bool isBooleanContext)
		{
			devicePlan.EnterContext(isBooleanContext);
			try
			{
				if (!isBooleanContext && node.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean))
				{	
					// case when <expression> then 1 else 0 end
					devicePlan.EnterContext(true);
					try
					{
						return
							new CaseExpression
							(
								new CaseItemExpression[]
								{
									new CaseItemExpression
									(
										(Expression)Translate(devicePlan, node), 
										new ValueExpression(1)
									)
								}, 
								new CaseElseExpression(new ValueExpression(0))
							);
					}
					finally
					{
						devicePlan.ExitContext();
					}
				}
				else
					return (Expression)Translate(devicePlan, node);
			}
			finally
			{
				devicePlan.ExitContext();
			}
		}
		
		protected virtual Statement TranslateConditionNode(SQLDevicePlan devicePlan, ConditionNode node)
		{
			return
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							TranslateExpression(devicePlan, node.Nodes[0], true),
							TranslateExpression(devicePlan, node.Nodes[1], false)
						)
					},
					new CaseElseExpression(TranslateExpression(devicePlan, node.Nodes[2], false))
				);
		}
		
		protected virtual Statement TranslateConditionedCaseNode(SQLDevicePlan devicePlan, ConditionedCaseNode node)
		{
			CaseExpression caseExpression = new CaseExpression();
			foreach (ConditionedCaseItemNode localNode in node.Nodes)
			{
				if (localNode.Nodes.Count == 2)
					caseExpression.CaseItems.Add
					(
						new CaseItemExpression
						(
							TranslateExpression(devicePlan, localNode.Nodes[0], true),
							TranslateExpression(devicePlan, localNode.Nodes[1], false)
						)
					);
				else
					caseExpression.ElseExpression = new CaseElseExpression(TranslateExpression(devicePlan, localNode.Nodes[0], false));
			}
			return caseExpression;
		}
		
		protected virtual Statement TranslateSelectedConditionedCaseNode(SQLDevicePlan devicePlan, SelectedConditionedCaseNode node)
		{
			CaseExpression caseExpression = new CaseExpression();
			caseExpression.Expression = TranslateExpression(devicePlan, node.Nodes[0], false);
			for (int index = 2; index < node.Nodes.Count; index++)
			{
				ConditionedCaseItemNode localNode = (ConditionedCaseItemNode)node.Nodes[index];
				if (localNode.Nodes.Count == 2)
					caseExpression.CaseItems.Add
					(
						new CaseItemExpression
						(
							TranslateExpression(devicePlan, localNode.Nodes[0], false),
							TranslateExpression(devicePlan, localNode.Nodes[1], false)
						)
					);
				else
					caseExpression.ElseExpression = new CaseElseExpression(TranslateExpression(devicePlan, localNode.Nodes[0], false));
			}
			return caseExpression; 
		}
        
		public virtual TableSpecifier GetDummyTableSpecifier()
		{
			SelectExpression selectExpression = new SelectExpression();
			selectExpression.SelectClause = new SelectClause();
			selectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
			return new TableSpecifier(selectExpression, "dummy1");
		}
		
		protected virtual Statement TranslateListNode(SQLDevicePlan devicePlan, ListNode node)
		{
			if (!devicePlan.CurrentQueryContext().IsListContext)
			{
				devicePlan.TranslationMessages.Add(new Schema.TranslationMessage(@"Plan is not supported because the device only supports list expressions as the right argument to an invocation of the membership operator (in).", node));
				devicePlan.IsSupported = false;
				return new CallExpression();
			}
			
			ListExpression listExpression = new ListExpression();
			foreach (PlanNode localNode in node.Nodes)
				listExpression.Expressions.Add(TranslateExpression(devicePlan, localNode, false));
			return listExpression;
		}
		
		protected virtual Statement TranslateRowSelectorNode(SQLDevicePlan devicePlan, RowSelectorNode node)
		{
			SelectExpression selectExpression = new SelectExpression();
			selectExpression.SelectClause = new SelectClause();
			if (_supportsAlgebraicFromClause)
				selectExpression.FromClause = new AlgebraicFromClause(GetDummyTableSpecifier());
			else
				selectExpression.FromClause = new CalculusFromClause(GetDummyTableSpecifier());
			devicePlan.PushScalarContext();
			try
			{
				for (int index = 0; index < node.DataType.Columns.Count; index++)
				{
					SQLRangeVarColumn rangeVarColumn = devicePlan.FindRangeVarColumn(node.DataType.Columns[index].Name, false);
					if (rangeVarColumn != null)
					{
						rangeVarColumn.Expression = TranslateExpression(devicePlan, node.Nodes[index], false);
						selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
					}
					else
						selectExpression.SelectClause.Columns.Add(new ColumnExpression(TranslateExpression(devicePlan, node.Nodes[index], false), ToSQLIdentifier(node.DataType.Columns[index].Name)));
				}
				
				devicePlan.CurrentQueryContext().ParentContext.ReferenceFlags |= devicePlan.CurrentQueryContext().ReferenceFlags;
			}
			finally
			{
				devicePlan.PopScalarContext();
			}

			return selectExpression;
		}
        
		protected virtual Statement TranslateTableSelectorNode(SQLDevicePlan devicePlan, TableSelectorNode node)
		{
			QueryExpression queryExpression = new QueryExpression();
			devicePlan.CurrentQueryContext().RangeVars.Add(new SQLRangeVar(devicePlan.GetNextTableAlias()));
			SQLRangeVar rangeVar = new SQLRangeVar(devicePlan.GetNextTableAlias());
			foreach (TableVarColumn column in node.TableVar.Columns)
				devicePlan.CurrentQueryContext().AddedColumns.Add(new SQLRangeVarColumn(column, String.Empty, devicePlan.Device.ToSQLIdentifier(column), devicePlan.Device.ToSQLIdentifier(column.Name)));
			
			foreach (PlanNode localNode in node.Nodes)
			{
				Statement statement = Translate(devicePlan, localNode);
				if (devicePlan.IsSupported)
				{
					SelectExpression selectExpression = devicePlan.Device.FindSelectExpression(statement);
					if (queryExpression.SelectExpression == null)
						queryExpression.SelectExpression = selectExpression;
					else
						queryExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Union, false, selectExpression));
				}
			}
			return queryExpression;
		}
        
		protected virtual Statement TranslateInsertNode(SQLDevicePlan devicePlan, InsertNode node)
		{
			if (devicePlan.Plan.ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			InsertStatement statement = new InsertStatement();
			statement.InsertClause = new InsertClause();
			statement.InsertClause.TableExpression = new TableExpression();
			BaseTableVar target = (BaseTableVar)((TableVarNode)node.Nodes[1]).TableVar;
			statement.InsertClause.TableExpression.TableSchema = D4.MetaData.GetTag(target.MetaData, "Storage.Schema", Schema);
			statement.InsertClause.TableExpression.TableName = ToSQLIdentifier(target);
			foreach (Column column in ((TableNode)node.Nodes[0]).DataType.Columns)
				statement.InsertClause.Columns.Add(new InsertFieldExpression(ToSQLIdentifier(column.Name)));
			statement.Values = TranslateExpression(devicePlan, node.Nodes[0], false);	
			return statement;
		}
        
		protected virtual Statement TranslateUpdateNode(SQLDevicePlan devicePlan, UpdateNode node)
		{														
			if (devicePlan.Plan.ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			BaseTableVar target = ((BaseTableVar)((TableVarNode)node.Nodes[0]).TableVar);
			UpdateStatement statement = new UpdateStatement();
			statement.UpdateClause = new UpdateClause();
			statement.UpdateClause.TableExpression = new TableExpression();
			statement.UpdateClause.TableExpression.TableSchema = D4.MetaData.GetTag(target.MetaData, "Storage.Schema", Schema);
			statement.UpdateClause.TableExpression.TableName = ToSQLIdentifier(target);

			devicePlan.Stack.Push(new Symbol(String.Empty, target.DataType.RowType));
			try
			{
				for (int index = 1; index < node.Nodes.Count; index++)
				{
					UpdateFieldExpression fieldExpression = new UpdateFieldExpression();
					UpdateColumnNode columnNode = (UpdateColumnNode)node.Nodes[index];
					#if USECOLUMNLOCATIONBINDING
					fieldExpression.FieldName = ToSQLIdentifier(target.DataType.Columns[columnNode.ColumnLocation].Name);
					#else
					fieldExpression.FieldName = ToSQLIdentifier(target.DataType.Columns[columnNode.ColumnName].Name);
					#endif
					fieldExpression.Expression = TranslateExpression(devicePlan, columnNode.Nodes[0], false);
					statement.UpdateClause.Columns.Add(fieldExpression);
				}
			}
			finally
			{
				devicePlan.Stack.Pop();
			}

			return statement;
		}
        
		protected virtual Statement TranslateDeleteNode(SQLDevicePlan devicePlan, DeleteNode node)
		{
			if (devicePlan.Plan.ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			DeleteStatement statement = new DeleteStatement();
			statement.DeleteClause = new DeleteClause();
			statement.DeleteClause.TableExpression = new TableExpression();
			BaseTableVar target = (BaseTableVar)((TableVarNode)node.Nodes[0]).TableVar;
			statement.DeleteClause.TableExpression.TableSchema = D4.MetaData.GetTag(target.MetaData, "Storage.Schema", Schema);
			statement.DeleteClause.TableExpression.TableName = ToSQLIdentifier(target);
			return statement;
		}
        
		protected virtual string GetIndexName(string tableName, Key key)
		{
			if ((key.MetaData != null) && key.MetaData.Tags.Contains("Storage.Name"))
				return key.MetaData.Tags["Storage.Name"].Value;
			else
			{
				StringBuilder indexName = new StringBuilder();
				indexName.AppendFormat("UIDX_{0}", tableName);
				foreach (TableVarColumn column in key.Columns)
					indexName.AppendFormat("_{0}", (column.Name));
				return EnsureValidIdentifier(indexName.ToString());
			}
		}
        
		protected virtual string GetIndexName(string tableName, Order order)
		{
			if ((order.MetaData != null) && order.MetaData.Tags.Contains("Storage.Name"))
				return order.MetaData.Tags["Storage.Name"].Value;
			else
			{
				StringBuilder indexName = new StringBuilder();
				indexName.AppendFormat("IDX_{0}", tableName);
				foreach (OrderColumn column in order.Columns)
					indexName.AppendFormat("_{0}", (column.Column.Name));
				return EnsureValidIdentifier(indexName.ToString());
			}
		}
        
		protected virtual Statement TranslateCreateIndex(SQLDevicePlan plan, TableVar tableVar, Key key)
		{
			CreateIndexStatement index = new CreateIndexStatement();
			index.IsUnique = !key.IsSparse;
			index.IsClustered = key.Equals(Compiler.FindClusteringKey(plan.Plan, tableVar));
			index.TableSchema = D4.MetaData.GetTag(tableVar.MetaData, "Storage.Schema", Schema);
			index.TableName = ToSQLIdentifier(tableVar);
			index.IndexSchema = D4.MetaData.GetTag(key.MetaData, "Storage.Schema", String.Empty);
			index.IndexName = GetIndexName(index.TableName, key);
			OrderColumnDefinition columnDefinition;
			foreach (TableVarColumn column in key.Columns)
			{
				columnDefinition = new OrderColumnDefinition();
				columnDefinition.ColumnName = ToSQLIdentifier(column);
				columnDefinition.Ascending = true;
				index.Columns.Add(columnDefinition);
			}
			return index;
		}
        
		protected virtual Statement TranslateDropIndex(SQLDevicePlan plan, TableVar tableVar, Key key)
		{
			DropIndexStatement statement = new DropIndexStatement();
			statement.IndexSchema = D4.MetaData.GetTag(key.MetaData, "Storage.Schema", String.Empty);
			statement.IndexName = GetIndexName(ToSQLIdentifier(tableVar), key);
			return statement;
		}
        
		protected virtual Statement TranslateCreateIndex(SQLDevicePlan plan, TableVar tableVar, Order order)
		{
			CreateIndexStatement index = new CreateIndexStatement();
			index.IsClustered = Convert.ToBoolean(D4.MetaData.GetTag(order.MetaData, "DAE.IsClustered", "false"));
			index.TableSchema = D4.MetaData.GetTag(tableVar.MetaData, "Storage.Schema", Schema);
			index.TableName = ToSQLIdentifier(tableVar);
			index.IndexSchema = D4.MetaData.GetTag(order.MetaData, "Storage.Schema", String.Empty);
			index.IndexName = GetIndexName(index.TableName, order);
			OrderColumnDefinition columnDefinition;
			foreach (OrderColumn column in order.Columns)
			{
				columnDefinition = new OrderColumnDefinition();
				columnDefinition.ColumnName = ToSQLIdentifier(column.Column);
				columnDefinition.Ascending = column.Ascending;
				index.Columns.Add(columnDefinition);
			}
			return index;
		}
        
		protected virtual Statement TranslateDropIndex(SQLDevicePlan plan, TableVar tableVar, Order order)
		{
			DropIndexStatement statement = new DropIndexStatement();
			statement.IndexSchema = D4.MetaData.GetTag(order.MetaData, "Storage.Schema", String.Empty);
			statement.IndexName = GetIndexName(ToSQLIdentifier(tableVar), order);
			return statement;
		}
        
		protected virtual Statement TranslateCreateTable(SQLDevicePlan plan, TableVar tableVar)
		{
			Batch batch = new Batch();
			CreateTableStatement statement = new CreateTableStatement();
			statement.TableSchema = D4.MetaData.GetTag(tableVar.MetaData, "Storage.Schema", Schema);
			statement.TableName = ToSQLIdentifier(tableVar);
			foreach (TableVarColumn column in tableVar.Columns)
			{
				if (Convert.ToBoolean(D4.MetaData.GetTag(column.MetaData, "Storage.ShouldReconcile", "true")))
				{
					SQLScalarType sQLScalarType = (SQLScalarType)ResolveDeviceScalarType(plan.Plan, (Schema.ScalarType)column.DataType);
					if (sQLScalarType == null)
						throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, column.DataType.ToString());
					statement.Columns.Add
					(
						new ColumnDefinition
						(
							ToSQLIdentifier(column), 
							sQLScalarType.DomainName(column), 
							column.IsNilable
						)
					);
				}
			}
			batch.Statements.Add(statement);

			foreach (Key key in tableVar.Keys)
				if (Convert.ToBoolean(D4.MetaData.GetTag(key.MetaData, "Storage.ShouldReconcile", (key.Columns.Count > 0).ToString())))
					batch.Statements.Add(TranslateCreateIndex(plan, tableVar, key));

			foreach (Order order in tableVar.Orders)
				if (Convert.ToBoolean(D4.MetaData.GetTag(order.MetaData, "Storage.ShouldReconcile", (order.Columns.Count > 0).ToString())))
					batch.Statements.Add(TranslateCreateIndex(plan, tableVar, order));
			
			return batch;
		}
        
		protected virtual Statement TranslateCreateTableNode(SQLDevicePlan devicePlan, CreateTableNode node)
		{
			return TranslateCreateTable(devicePlan, node.Table);
		}
		
		protected bool AltersStorageTags(D4.AlterMetaData alterMetaData)
		{
			if (alterMetaData == null)
				return false;
				
			for (int index = 0; index < alterMetaData.DropTags.Count; index++)
				if (alterMetaData.DropTags[index].Name.IndexOf("Storage") == 0)
					return true;
					
			for (int index = 0; index < alterMetaData.AlterTags.Count; index++)
				if (alterMetaData.AlterTags[index].Name.IndexOf("Storage") == 0)
					return true;
					
			for (int index = 0; index < alterMetaData.CreateTags.Count; index++)
				if (alterMetaData.CreateTags[index].Name.IndexOf("Storage") == 0)
					return true;
					
			return false;
		}
        
		protected virtual Statement TranslateAlterTableNode(SQLDevicePlan devicePlan, AlterTableNode node)
		{
			Batch batch = new Batch();
			string tableSchema = D4.MetaData.GetTag(node.TableVar.MetaData, "Storage.Schema", Schema);
			string tableName = ToSQLIdentifier(node.TableVar);
			if (node.AlterTableStatement.DropColumns.Count > 0)
			{
				foreach (D4.DropColumnDefinition column in node.AlterTableStatement.DropColumns)
				{
					TableVarColumn tableVarColumn = null;
					SchemaLevelDropColumnDefinition schemaLevelDropColumnDefinition = column as SchemaLevelDropColumnDefinition;
					if (schemaLevelDropColumnDefinition != null)
						tableVarColumn = schemaLevelDropColumnDefinition.Column;
					else
						tableVarColumn = node.TableVar.Columns[node.TableVar.Columns.IndexOfName(column.ColumnName)];
						
					if (Convert.ToBoolean(D4.MetaData.GetTag(tableVarColumn.MetaData, "Storage.ShouldReconcile", "true")))
					{
						AlterTableStatement statement = new AlterTableStatement();
						statement.TableSchema = tableSchema;
						statement.TableName = tableName;
						statement.DropColumns.Add(new DropColumnDefinition(ToSQLIdentifier(tableVarColumn)));
						batch.Statements.Add(statement);
					}
				}
			}
			
			if (node.AlterTableStatement.AlterColumns.Count > 0)
			{
				foreach (D4.AlterColumnDefinition alterColumnDefinition in node.AlterTableStatement.AlterColumns)
				{
					TableVarColumn column = node.TableVar.Columns[alterColumnDefinition.ColumnName];
					if (Convert.ToBoolean(D4.MetaData.GetTag(column.MetaData, "Storage.ShouldReconcile", "true")))
					{
						// The assumption being made here is that the type of the column in the table var has already been changed to the new type, the presence of the type specifier is just to indicate that the alter should be performed
						// This assumption will likely need to be revisited when (if) we actually start supporting changing the type of a column
						if (alterColumnDefinition.ChangeNilable || (alterColumnDefinition.TypeSpecifier != null) || AltersStorageTags(alterColumnDefinition.AlterMetaData))
						{
							AlterTableStatement statement = new AlterTableStatement();
							statement.TableSchema = tableSchema;
							statement.TableName = tableName;
							statement.AlterColumns.Add(new AlterColumnDefinition(ToSQLIdentifier(column), ((SQLScalarType)ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)column.DataType)).DomainName(column), column.IsNilable));
							batch.Statements.Add(statement);
						}
					}
				}
			}
			
			if (node.AlterTableStatement.CreateColumns.Count > 0)
			{
				foreach (D4.ColumnDefinition columnDefinition in node.AlterTableStatement.CreateColumns)
				{
					TableVarColumn column = node.TableVar.Columns[columnDefinition.ColumnName];
					if (Convert.ToBoolean(D4.MetaData.GetTag(column.MetaData, "Storage.ShouldReconcile", "true")))
					{
						AlterTableStatement statement = new AlterTableStatement();
						statement.TableSchema = tableSchema;
						statement.TableName = tableName;
						statement.AddColumns.Add(new ColumnDefinition(ToSQLIdentifier(column), ((SQLScalarType)ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)column.DataType)).DomainName(column), true));
						batch.Statements.Add(statement);
					}
				}
			}
			
			foreach (D4.DropKeyDefinition keyDefinition in node.AlterTableStatement.DropKeys)
			{
				Schema.Key key = null;
				SchemaLevelDropKeyDefinition schemaLevelDropKeyDefinition = keyDefinition as SchemaLevelDropKeyDefinition;
				if (schemaLevelDropKeyDefinition != null)
					key = schemaLevelDropKeyDefinition.Key;
				else
					key = Compiler.FindKey(devicePlan.Plan, node.TableVar, keyDefinition);

				if (Convert.ToBoolean(D4.MetaData.GetTag(key.MetaData, "Storage.ShouldReconcile", (key.Columns.Count > 0).ToString())))
					batch.Statements.Add(TranslateDropIndex(devicePlan, node.TableVar, key));
			}
				
			foreach (D4.DropOrderDefinition orderDefinition in node.AlterTableStatement.DropOrders)
			{
				Schema.Order order = null;
				SchemaLevelDropOrderDefinition schemaLevelDropOrderDefinition = orderDefinition as SchemaLevelDropOrderDefinition;
				if (schemaLevelDropOrderDefinition != null)
					order = schemaLevelDropOrderDefinition.Order;
				else
					order = Compiler.FindOrder(devicePlan.Plan, node.TableVar, orderDefinition);

				if (Convert.ToBoolean(D4.MetaData.GetTag(order.MetaData, "Storage.ShouldReconcile", (order.Columns.Count > 0).ToString())))
					batch.Statements.Add(TranslateDropIndex(devicePlan, node.TableVar, order));
			}
				
			foreach (D4.KeyDefinition keyDefinition in node.AlterTableStatement.CreateKeys)
			{
				Schema.Key key = Compiler.CompileKeyDefinition(devicePlan.Plan, node.TableVar, keyDefinition);
				if (Convert.ToBoolean(D4.MetaData.GetTag(key.MetaData, "Storage.ShouldReconcile", (key.Columns.Count > 0).ToString())))
					batch.Statements.Add(TranslateCreateIndex(devicePlan, node.TableVar, key));
			}
				
			foreach (D4.OrderDefinition orderDefinition in node.AlterTableVarStatement.CreateOrders)
			{
				Schema.Order order = Compiler.CompileOrderDefinition(devicePlan.Plan, node.TableVar, orderDefinition, false);
				if (Convert.ToBoolean(D4.MetaData.GetTag(order.MetaData, "Storage.ShouldReconcile", (order.Columns.Count > 0).ToString())))
					batch.Statements.Add(TranslateCreateIndex(devicePlan, node.TableVar, order));
			}

			return batch;
		}
        
		protected virtual Statement TranslateDropTableNode(DevicePlan devicePlan, DropTableNode node)
		{
			DropTableStatement statement = new DropTableStatement();
			statement.TableSchema = D4.MetaData.GetTag(node.Table.MetaData, "Storage.Schema", Schema);
			statement.TableName = ToSQLIdentifier(node.Table);
			return statement;
		}
        
        /// <summary>
        /// Translates an order by clause based on the Order of the given node
        /// </summary>
        /// <param name="devicePlan">The device plan being translated</param>
        /// <param name="node">The node providing the definition of the order</param>
        /// <param name="statement">The current translated select statement</param>
        /// <param name="inContextOrderBy">Indicates that this is an order by in an expression context and is therefore allowed to reference range variables, regardless of the device OrderByInContext setting.</param>
        /// <returns>A select statement with an appropriate order by clause</returns>
		public virtual SelectStatement TranslateOrder(DevicePlan devicePlan, TableNode node, SelectStatement statement, bool inContextOrderBy)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			if ((node.Order != null) && (node.Order.Columns.Count > 0))
			{
				statement.OrderClause = new OrderClause();
				SQLRangeVarColumn rangeVarColumn;
				foreach (OrderColumn column in node.Order.Columns)
				{
					if (!column.IsDefaultSort)
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(@"Plan is not supported because the order uses expression-based sorting.", node));
						localDevicePlan.IsSupported = false;
						break;
					}
					
					if (!localDevicePlan.Device.SupportsComparison(devicePlan.Plan, column.Column.DataType))
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support comparison of values of type ""{0}"" for column ""{1}"".", column.Column.DataType.Name, column.Column.Name), node));
						localDevicePlan.IsSupported = false;
						break;
					}

					OrderFieldExpression fieldExpression = new OrderFieldExpression();
					rangeVarColumn = localDevicePlan.GetRangeVarColumn(column.Column.Name, true);
					if (IsOrderByInContext || inContextOrderBy)
					{
						if (rangeVarColumn.Expression != null)
						{
							QualifiedFieldExpression qualifiedFieldExpression = rangeVarColumn.Expression as QualifiedFieldExpression;
							if (qualifiedFieldExpression == null)
							{
								throw new NotImplementedException("Expressions within an order by are not implemented.");
							}

							fieldExpression.FieldName = qualifiedFieldExpression.FieldName;
							fieldExpression.TableAlias = qualifiedFieldExpression.TableAlias;
						}
						else
						{
							fieldExpression.FieldName = rangeVarColumn.ColumnName;
							fieldExpression.TableAlias = rangeVarColumn.TableAlias;
						}
					}
					else
						fieldExpression.FieldName = rangeVarColumn.Alias == String.Empty ? rangeVarColumn.ColumnName : rangeVarColumn.Alias;
					fieldExpression.Ascending = column.Ascending;
					// NOTE: Only include the NULLS FIRST/LAST clause if it is required by the underlying system; it has significant performance implications
					if (column.Column.IsNilable)
					{
						// TODO: Respect the IncludeNils option in the order column
						// TODO: Use the system-level NullSortBehavior to determine whether data coming from this device will be sorted differently
						// TODO: If different behavior is required and the underlying system doesn't support it (or we don't want to use the underlying system's built-in support for performance reasons) add a buffer for nulls to the device cursor
						if (SupportsOrderByNullsFirstLast)
						{
							fieldExpression.NullsFirst = column.Ascending;
						}
					}
					statement.OrderClause.Columns.Add(fieldExpression);
				}
			}
			
			return statement;
		}

		private bool SupportsOperator(Plan plan, Schema.Operator operatorValue)
		{
			// returns true if there exists a ScalarTypeOperator corresponding to the Operator for this InstructionNode
			return ResolveDeviceOperator(plan, operatorValue) != null;
		}

		private bool SupportsOperands(InstructionNodeBase node)
		{
			if ((node.DataType is Schema.ITableType) || (node.DataType is Schema.IScalarType) || (node.DataType is Schema.IRowType))
				return true;
			
			return false;
		}
		
		public bool SupportsEqual(Plan plan, Schema.IDataType dataType)
		{
			if (SupportsComparison(plan, dataType))
				return true;
				
			Schema.Signature signature = new Schema.Signature(new SignatureElement[]{new SignatureElement(dataType), new SignatureElement(dataType)});
			OperatorBindingContext context = new OperatorBindingContext(null, "iEqual", plan.NameResolutionPath, signature, true);
			Compiler.ResolveOperator(plan, context);
			if (context.Operator != null)
			{
				Schema.DeviceOperator deviceOperator = ResolveDeviceOperator(plan, context.Operator);
				if (deviceOperator != null)
				{
					plan.AttachDependency(deviceOperator);
					return true;
				}
			}
			return false;
		}
		
		public bool SupportsComparison(Plan plan, Schema.IDataType dataType)
		{
			Schema.Signature signature = new Schema.Signature(new SignatureElement[]{new SignatureElement(dataType), new SignatureElement(dataType)});
			OperatorBindingContext context = new OperatorBindingContext(null, "iCompare", plan.NameResolutionPath, signature, true);
			Compiler.ResolveOperator(plan, context);
			if (context.Operator != null)
			{
				Schema.DeviceOperator deviceOperator = ResolveDeviceOperator(plan, context.Operator);
				if (deviceOperator != null)
				{
					plan.AttachDependency(deviceOperator);
					return true;
				}
			}
			return false;
		}
		
		protected bool IsTruthValued(DeviceOperator deviceOperator)
		{
			SQLDeviceOperator localDeviceOperator = deviceOperator as SQLDeviceOperator;
			return (localDeviceOperator != null) && localDeviceOperator.IsTruthValued;
		}

		// Translate
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			bool scalarContext = false;
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			if (((planNode is TableNode) || (planNode.DataType is Schema.IRowType)) && (localDevicePlan.CurrentQueryContext().IsScalarContext))
			{
				bool supportsSubSelect = localDevicePlan.IsSubSelectSupported();
				if (!supportsSubSelect)
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(localDevicePlan.GetSubSelectNotSupportedReason(), planNode));
				localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsSubSelect;
				localDevicePlan.CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasSubSelectExpressions;
				localDevicePlan.PushQueryContext();
				scalarContext = true;
			}  
			try
			{
				InstructionNodeBase instructionNode = planNode as InstructionNodeBase;
				if ((instructionNode != null) && (instructionNode.DataType != null) && (instructionNode.Operator != null))
				{
					bool supportsOperator = SupportsOperator(devicePlan.Plan, instructionNode.Operator) && SupportsOperands(instructionNode);
					if (!supportsOperator)
					{
						if 
						(
							(localDevicePlan.CurrentQueryContext().IsScalarContext) 
								&& (instructionNode.DataType is Schema.ScalarType) 
								&& (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)instructionNode.DataType) != null) 
								&& instructionNode.IsFunctional 
								&& instructionNode.IsRepeatable 
								&& instructionNode.IsContextLiteral(localDevicePlan.Stack.Count == 0 ? 0 : localDevicePlan.Stack.Count - 1)
						)
						{
							ServerStatementPlan plan = new ServerStatementPlan(devicePlan.Plan.ServerProcess);
							try
							{
								plan.Plan.PushStatementContext(localDevicePlan.Plan.StatementContext);
								plan.Plan.PushSecurityContext(localDevicePlan.Plan.SecurityContext);
								plan.Plan.PushCursorContext(localDevicePlan.Plan.CursorContext);
								if (localDevicePlan.Plan.InRowContext)
									plan.Plan.EnterRowContext();
								for (int index = localDevicePlan.Plan.Symbols.Count - 1; index >= 0; index--)
									plan.Plan.Symbols.Push(localDevicePlan.Plan.Symbols.Peek(index));
								try
								{
									PlanNode node = Compiler.CompileExpression(plan.Plan, new D4.Parser(true).ParseExpression(instructionNode.EmitStatementAsString()));
									// Perform binding on the children, but not on the root
									// A binding on the root would result in another attempt to support this expression, but we already know it's not supported, and
									// the device associative code would kick in again, resulting in stack overflow
									foreach (var childNode in node.Nodes)
										Compiler.OptimizeNode(plan.Plan, childNode);
									planNode.CouldSupport = true; // Set this to indicate that support could be provided if it would be beneficial to do so
									return FromScalar(localDevicePlan, node);
								}
								finally
								{
									while (plan.Plan.Symbols.Count > 0)
										plan.Plan.Symbols.Pop();
								}
							}
							finally
							{
								plan.Dispose();
							}
						}
						else
						{
							localDevicePlan.IsSupported = false;
							localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not contain an operator map for operator ""{0}"" with signature ""{1}"".", instructionNode.Operator.OperatorName, instructionNode.Operator.Signature.ToString()), planNode));
						}
					}
					else
					{
						TableNode tableNode = planNode as TableNode;
						if (tableNode != null)
							DetermineCursorBehavior(localDevicePlan.Plan, tableNode);
						DeviceOperator deviceOperator = ResolveDeviceOperator(devicePlan.Plan, instructionNode.Operator);
						if (localDevicePlan.IsBooleanContext() && planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean) && !IsTruthValued(deviceOperator))
							return new BinaryExpression((Expression)deviceOperator.Translate(localDevicePlan, planNode), "iEqual", new ValueExpression(1));
						else
							return deviceOperator.Translate(localDevicePlan, planNode);
					}
					return new CallExpression();
				}
				else if ((planNode is AggregateCallNode) && (planNode.DataType != null) && ((AggregateCallNode)planNode).Operator != null)
				{
					AggregateCallNode aggregateCallNode = (AggregateCallNode)planNode;
					bool supportsOperator = SupportsOperator(devicePlan.Plan, aggregateCallNode.Operator);
					if (!supportsOperator)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have an operator map for the operator ""{0}"" with signature ""{1}"".", aggregateCallNode.Operator.OperatorName, aggregateCallNode.Operator.Signature.ToString()), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsOperator;
					if (localDevicePlan.IsSupported)
					{
						DeviceOperator deviceOperator = ResolveDeviceOperator(devicePlan.Plan, aggregateCallNode.Operator);
						if (localDevicePlan.IsBooleanContext() && planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean) && !IsTruthValued(deviceOperator))
							return new BinaryExpression((Expression)deviceOperator.Translate(localDevicePlan, planNode), "iEqual", new ValueExpression(1));
						else
							return deviceOperator.Translate(localDevicePlan, planNode);
					}
					return new CallExpression();
				}
				else if (planNode is StackReferenceNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"" of variable reference ""{1}"".", planNode.DataType.Name, ((StackReferenceNode)planNode).Identifier), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						return TranslateStackReference((SQLDevicePlan)localDevicePlan, (StackReferenceNode)planNode);
					return new CallExpression();
				}
				else if (planNode is StackColumnReferenceNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"" of column reference ""{1}"".", planNode.DataType.Name, ((StackColumnReferenceNode)planNode).Identifier), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						return TranslateStackColumnReference((SQLDevicePlan)localDevicePlan, (StackColumnReferenceNode)planNode);
					return new CallExpression();
				}
				else if (planNode is ExtractColumnNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"" of column reference ""{1}"".", planNode.DataType.Name, ((ExtractColumnNode)planNode).Identifier), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						return TranslateExtractColumnNode((SQLDevicePlan)localDevicePlan, (ExtractColumnNode)planNode);
					return new CallExpression();
				}
				else if (planNode is ExtractRowNode)
				{
					return TranslateExtractRowNode(localDevicePlan, (ExtractRowNode)planNode);
				}
				else if (planNode is ValueNode)
				{
					bool supportsDataType = ((planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null)) || (planNode.DataType is Schema.GenericType);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", planNode.DataType.Name), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						return TranslateValueNode(localDevicePlan, (ValueNode)planNode);
					return new CallExpression();
				}
				else if (planNode is AsNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", planNode.DataType.Name), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						return TranslateAsNode(localDevicePlan, (AsNode)planNode);
					return new CallExpression();
				}
				else if (planNode is ConditionNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", planNode.DataType.Name), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						if (localDevicePlan.IsBooleanContext() && planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean))
							return new BinaryExpression((Expression)TranslateConditionNode(localDevicePlan, (ConditionNode)planNode), "iEqual", new ValueExpression(1));
						else
							return TranslateConditionNode(localDevicePlan, (ConditionNode)planNode);
					return new CallExpression();
				}
				else if (planNode is ConditionedCaseNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", planNode.DataType.Name), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						if (localDevicePlan.IsBooleanContext() && planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean))
							return new BinaryExpression((Expression)TranslateConditionedCaseNode(localDevicePlan, (ConditionedCaseNode)planNode), "iEqual", new ValueExpression(1));
						else
							return TranslateConditionedCaseNode(localDevicePlan, (ConditionedCaseNode)planNode);
					return new CallExpression();
				}
				else if (planNode is SelectedConditionedCaseNode)
				{
					bool supportsDataType = (planNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType) != null);
					if (!supportsDataType)
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", planNode.DataType.Name), planNode));
					localDevicePlan.IsSupported = localDevicePlan.IsSupported && supportsDataType;
					if (localDevicePlan.IsSupported)
						if (localDevicePlan.IsBooleanContext() && planNode.DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean))
							return new BinaryExpression((Expression)TranslateSelectedConditionedCaseNode(localDevicePlan, (SelectedConditionedCaseNode)planNode), "iEqual", new ValueExpression(1));
						else
							return TranslateSelectedConditionedCaseNode(localDevicePlan, (SelectedConditionedCaseNode)planNode);
					return new CallExpression();
				}
				else if (planNode is ConditionedCaseItemNode)
				{
					// Let the support walk up to the parent CaseNode for translation
					return new CallExpression();
				}
				else if (planNode is ListNode)
				{
					return TranslateListNode(localDevicePlan, (ListNode)planNode);
				}
				else if (planNode is TableSelectorNode)
				{
					DetermineCursorBehavior(localDevicePlan.Plan, (TableNode)planNode);
					return TranslateTableSelectorNode(localDevicePlan, (TableSelectorNode)planNode);
				}
				else if (planNode is RowSelectorNode)
					return TranslateRowSelectorNode(localDevicePlan, (RowSelectorNode)planNode);
				#if TranslateModifications
				else if (APlanNode is InsertNode)
					return TranslateInsertNode(localDevicePlan, (InsertNode)APlanNode);
				else if (APlanNode is UpdateNode)
					return TranslateUpdateNode(localDevicePlan, (UpdateNode)APlanNode);
				else if (APlanNode is DeleteNode)
					return TranslateDeleteNode(localDevicePlan, (DeleteNode)APlanNode);
				#endif
				else if (planNode is CreateTableNode)
					return TranslateCreateTableNode(localDevicePlan, (CreateTableNode)planNode);
				else if (planNode is AlterTableNode)
					return TranslateAlterTableNode(localDevicePlan, (AlterTableNode)planNode);
				else if (planNode is DropTableNode)
					return TranslateDropTableNode(localDevicePlan, (DropTableNode)planNode);
				else
				{
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support translation for nodes of type ""{0}"".", planNode.GetType().Name)));
					localDevicePlan.IsSupported = false;
					return new CallExpression();
				}
			}
			finally
			{
				if (scalarContext)
					localDevicePlan.PopQueryContext();
			}
		}
        
		public Batch DeviceReconciliationScript(ServerProcess process, ReconcileOptions options)
		{
			Catalog serverCatalog = GetServerCatalog(process, null);
			return DeviceReconciliationScript(process, serverCatalog, GetDeviceCatalog(process, serverCatalog), options);
		}
		
		public Batch DeviceReconciliationScript(ServerProcess process, TableVar tableVar, ReconcileOptions options)
		{
			Catalog serverCatalog = GetServerCatalog(process, tableVar);
			return DeviceReconciliationScript(process, serverCatalog, GetDeviceCatalog(process, serverCatalog, tableVar), options);
		}

		/// <summary>Produces a script to reconcile the given device catalog to the given server catalog, with the specified options.</summary>        
        public Batch DeviceReconciliationScript(ServerProcess process, Catalog serverCatalog, Catalog deviceCatalog, ReconcileOptions options)
        {
			Batch batch = new Batch();
			foreach (Schema.Object objectValue in serverCatalog)
			{
				Schema.BaseTableVar baseTableVar = objectValue as Schema.BaseTableVar;
				if (baseTableVar != null)
				{
					if (Convert.ToBoolean(D4.MetaData.GetTag(baseTableVar.MetaData, "Storage.ShouldReconcile", "true")))
					{	
						AlterTableNode alterTableNode = new AlterTableNode();
						using (Plan plan = new Plan(process))
						{
							using (SQLDevicePlan devicePlan = new SQLDevicePlan(plan, this, alterTableNode))
							{
								int objectIndex = deviceCatalog.IndexOf(baseTableVar.Name);
								if (objectIndex < 0)
									batch.Statements.Add(TranslateCreateTable(devicePlan, baseTableVar));
								else
								{
									// Compile and translate the D4.AlterTableStatement returned from ReconcileTable and add it to LBatch
									bool reconciliationRequired;
									D4.AlterTableStatement d4AlterTableStatement = ReconcileTable(plan, baseTableVar, (Schema.TableVar)deviceCatalog[objectIndex], options, out reconciliationRequired);
									if (reconciliationRequired)
									{
										alterTableNode.AlterTableStatement = d4AlterTableStatement;
										alterTableNode.DeterminePotentialDevice(devicePlan.Plan);
										alterTableNode.DetermineDevice(devicePlan.Plan);
										alterTableNode.DetermineAccessPath(devicePlan.Plan);
										batch.Statements.Add(TranslateAlterTableNode(devicePlan, alterTableNode));
									}
								}
							}
						}
					}
				}
			}
			
			if ((options & ReconcileOptions.ShouldDropTables) != 0)
			{
				foreach (Schema.Object objectValue in deviceCatalog)
				{
					Schema.BaseTableVar baseTableVar = objectValue as Schema.BaseTableVar;
					if ((baseTableVar != null) && !serverCatalog.Contains(baseTableVar.Name))
					{
						DropTableNode dropTableNode = new DropTableNode(baseTableVar);
						using (Plan plan = new Plan(process))
						{
							using (SQLDevicePlan devicePlan = new SQLDevicePlan(plan, this, dropTableNode))
							{
								batch.Statements.Add(TranslateDropTableNode(devicePlan, new DropTableNode(baseTableVar)));
							}
						}
					}
				}
			}
			
			return batch;
        }

		protected override void CreateServerTableInDevice(ServerProcess process, TableVar tableVar)
		{
			using (Plan plan = new Plan(process))
			{
				using (SQLDevicePlan devicePlan = new SQLDevicePlan(plan, this, null))
				{
					Statement statement = TranslateCreateTable(devicePlan, tableVar);
					SQLDeviceSession deviceSession = (SQLDeviceSession)plan.DeviceConnect(this);
					{
						Batch batch = statement as Batch;
						if (batch != null)
						{
							foreach (Statement singleStatement in batch.Statements)
								deviceSession.Connection.Execute(Emitter.Emit(singleStatement));
						}
						else
							deviceSession.Connection.Execute(Emitter.Emit(statement));
					}
				}
			}
		}
        
		public class SchemaLevelDropColumnDefinition : D4.DropColumnDefinition
		{
			public SchemaLevelDropColumnDefinition(Schema.TableVarColumn column) : base()
			{
				_column = column;
			}
			
			private Schema.TableVarColumn _column;
			public Schema.TableVarColumn Column { get { return _column; } }
		}
		
		public class SchemaLevelDropKeyDefinition : D4.DropKeyDefinition
		{
			public SchemaLevelDropKeyDefinition(Schema.Key key) : base()
			{
				_key = key;
			}

			private Schema.Key _key;
			public Schema.Key Key { get { return _key; } }
		}
		
		public class SchemaLevelDropOrderDefinition : D4.DropOrderDefinition
		{
			public SchemaLevelDropOrderDefinition(Schema.Order order) : base()
			{
				_order = order;
			}
			
			private Schema.Order _order;
			public Schema.Order Order { get { return _order; } }
		}
		
		protected override D4.AlterTableStatement ReconcileTable(Plan plan, TableVar sourceTableVar, TableVar targetTableVar, ReconcileOptions options, out bool reconciliationRequired)
		{
			reconciliationRequired = false;
			D4.AlterTableStatement statement = new D4.AlterTableStatement();
			statement.TableVarName = targetTableVar.Name;
			
			// Ensure ASourceTableVar.Columns is a subset of ATargetTableVar.Columns
			foreach (Schema.TableVarColumn column in sourceTableVar.Columns)
				if (Convert.ToBoolean(D4.MetaData.GetTag(column.MetaData, "Storage.ShouldReconcile", "true")))
				{
					int targetIndex = ColumnIndexFromNativeColumnName(targetTableVar, ToSQLIdentifier(column));
					if (targetIndex < 0)
					{
						// Add the column to the target table var
						statement.CreateColumns.Add(column.EmitStatement(D4.EmitMode.ForCopy));
						reconciliationRequired = true;
					}
					else
					{
						if ((options & ReconcileOptions.ShouldReconcileColumns) != 0)
						{
							Schema.TableVarColumn targetColumn = targetTableVar.Columns[targetIndex];
							
							// Type of the column (Needs to be based on domain name because the reconciliation process will only create the column with the D4 type map for the native domain)
							SQLScalarType sourceType = (SQLScalarType)ResolveDeviceScalarType(plan, (Schema.ScalarType)column.DataType);
							SQLScalarType targetType = (SQLScalarType)ResolveDeviceScalarType(plan, (Schema.ScalarType)targetColumn.DataType);
							bool domainsDifferent = 
								(sourceType.DomainName(column) != targetType.DomainName(targetColumn))
									|| (sourceType.NativeDomainName(column) != targetType.NativeDomainName(targetColumn));

							// Nilability of the column
							bool nilabilityDifferent = column.IsNilable != targetColumn.IsNilable;
							
							if (domainsDifferent || nilabilityDifferent)
							{
								D4.AlterColumnDefinition alterColumnDefinition = new D4.AlterColumnDefinition();
								alterColumnDefinition.ColumnName = targetColumn.Name;
								alterColumnDefinition.ChangeNilable = nilabilityDifferent;
								alterColumnDefinition.IsNilable = column.IsNilable;
								if (domainsDifferent)
									alterColumnDefinition.TypeSpecifier = column.DataType.EmitSpecifier(D4.EmitMode.ForCopy);
								statement.AlterColumns.Add(alterColumnDefinition);
								reconciliationRequired = true;
							}
						}
					}
				}
			
			// Ensure ATargetTableVar.Columns is a subset of ASourceTableVar.Columns
			if ((options & ReconcileOptions.ShouldDropColumns) != 0)
				foreach (Schema.TableVarColumn column in targetTableVar.Columns)
					if (!sourceTableVar.Columns.ContainsName(column.Name))
					{
						statement.DropColumns.Add(new SchemaLevelDropColumnDefinition(column));
						reconciliationRequired = true;
					}
				
			// Ensure All keys and orders have supporting indexes
			// TODO: Replace an imposed key if a new key is added
			foreach (Schema.Key key in sourceTableVar.Keys)
				if (Convert.ToBoolean(D4.MetaData.GetTag(key.MetaData, "Storage.ShouldReconcile", "true")))
					if (!targetTableVar.Keys.Contains(key) && (!key.IsSparse || !targetTableVar.Orders.Contains(CreateOrderFromSparseKey(plan, key))))
					{
						// Add the key to the target table var
						statement.CreateKeys.Add(key.EmitStatement(D4.EmitMode.ForCopy));
						reconciliationRequired = true;
					}
					else
					{
						// TODO: Key level reconciliation
					}

			// Ensure ATargetTableVar.Keys is a subset of ASourceTableVar.Keys
			if ((options & ReconcileOptions.ShouldDropKeys) != 0)
				foreach (Schema.Key key in targetTableVar.Keys)
					if (!sourceTableVar.Keys.Contains(key) && !Convert.ToBoolean(D4.MetaData.GetTag(key.MetaData, "Storage.IsImposedKey", "false")))
					{
						statement.DropKeys.Add(new SchemaLevelDropKeyDefinition(key));
						reconciliationRequired = true;
					}
				
			foreach (Schema.Order order in sourceTableVar.Orders)
				if (Convert.ToBoolean(D4.MetaData.GetTag(order.MetaData, "Storage.ShouldReconcile", "true")))
					if (!(targetTableVar.Orders.Contains(order)))
					{
						// Add the order to the target table var
						statement.CreateOrders.Add(order.EmitStatement(D4.EmitMode.ForCopy));
						reconciliationRequired = true;
					}
					else
					{
						// TODO: Order level reconciliation
					}
			
			// Ensure ATargetTableVar.Orders is a subset of ASourceTableVar.Orders
			if ((options & ReconcileOptions.ShouldDropOrders) != 0)
				foreach (Schema.Order order in targetTableVar.Orders)
					if (!sourceTableVar.Orders.Contains(order) && !sourceTableVar.Keys.Contains(CreateSparseKeyFromOrder(order)))
					{
						statement.DropOrders.Add(new SchemaLevelDropOrderDefinition(order));
						reconciliationRequired = true;
					}

			return statement;
		}

		private Schema.Order CreateOrderFromSparseKey(Plan plan, Schema.Key key )
		{
			return Compiler.OrderFromKey(plan, key);
		}

		private Schema.Key CreateSparseKeyFromOrder(Schema.Order order)
		{
			TableVarColumn[] orderColumns = new TableVarColumn[order.Columns.Count];
			for (int i = 0; i < order.Columns.Count; i++)
				orderColumns[i] = order.Columns[i].Column;
			Schema.Key key = new Schema.Key(orderColumns);
			key.IsSparse = true;
			return key;
		}
        
		public virtual bool ShouldIncludeColumn(Plan plan, string tableName, string columnName, string domainName)
		{
			return true;
		}
        
		public virtual ScalarType FindScalarType(Plan plan, string domainName, int length, D4.MetaData metaData)
		{
			throw new SQLException(SQLException.Codes.UnsupportedImportType, domainName);
		}
		
		public virtual ScalarType FindScalarType(Plan plan, SQLDomain domain)
		{
			if (domain.Type.Equals(typeof(bool))) return plan.DataTypes.SystemBoolean;
			else if (domain.Type.Equals(typeof(byte))) return plan.DataTypes.SystemByte;
			else if (domain.Type.Equals(typeof(short))) return plan.DataTypes.SystemShort;
			else if (domain.Type.Equals(typeof(int))) return plan.DataTypes.SystemInteger;
			else if (domain.Type.Equals(typeof(long))) return plan.DataTypes.SystemLong;
			else if (domain.Type.Equals(typeof(decimal))) return plan.DataTypes.SystemDecimal;
			else if (domain.Type.Equals(typeof(float))) return plan.DataTypes.SystemDecimal;
			else if (domain.Type.Equals(typeof(double))) return plan.DataTypes.SystemDecimal;
			else if (domain.Type.Equals(typeof(string))) return plan.DataTypes.SystemString;
			else if (domain.Type.Equals(typeof(byte[]))) return plan.DataTypes.SystemBinary;
			else if (domain.Type.Equals(typeof(Guid))) return plan.DataTypes.SystemGuid;
			else if (domain.Type.Equals(typeof(DateTime))) return plan.DataTypes.SystemDateTime;
			else if (domain.Type.Equals(typeof(TimeSpan))) return plan.DataTypes.SystemTimeSpan;
			else throw new SQLException(SQLException.Codes.UnsupportedImportType, domain.Type.Name);
		}
        
		private void ConfigureTableVar(Plan plan, TableVar tableVar, Objects columns, Catalog catalog)
		{
			if (tableVar != null)
			{
				tableVar.DataType = new TableType();

				foreach (TableVarColumn column in columns)
				{
					tableVar.Columns.Add(column);
					tableVar.DataType.Columns.Add(column.Column);
				}
				
				catalog.Add(tableVar);
			}
		}
		
		private void AttachKeyOrOrder(Plan plan, TableVar tableVar, ref Key key, ref Order order)
		{
			if (key != null)
			{
				if (!tableVar.Keys.Contains(key) && (key.Columns.Count > 0))
					tableVar.Keys.Add(key);
				key = null;
			}
			else if (order != null)
			{
				if (!tableVar.Orders.Contains(order) && (order.Columns.Count > 0))
					tableVar.Orders.Add(order);
				order = null;
			}
		}
		
		private string _deviceTablesExpression = String.Empty;
		public string DeviceTablesExpression
		{
			get { return _deviceTablesExpression; }
			set { _deviceTablesExpression = value == null ? String.Empty : value; }
		}
		
		protected virtual string GetDeviceTablesExpression(TableVar tableVar)
		{
			return
				String.Format
				(
					DeviceTablesExpression,
					tableVar == null ? String.Empty : ToSQLIdentifier(tableVar)
				);
		}
		
		protected virtual string GetTitleFromName(string objectName)
		{
			StringBuilder columnTitle = new StringBuilder();
			bool firstChar = true;
			for (int index = 0; index < objectName.Length; index++)
				if (Char.IsLetterOrDigit(objectName, index))
					if (firstChar)
					{
						firstChar = false;
						columnTitle.Append(Char.ToUpper(objectName[index]));
					}
					else
						columnTitle.Append(Char.ToLower(objectName[index]));
				else
					if (!firstChar)
					{
						columnTitle.Append(" ");
						firstChar = true;
					}

			return columnTitle.ToString();
		}
		
		protected string GetServerTableName(Plan plan, Catalog serverCatalog, string deviceTableName)
		{
			string serverTableName = FromSQLIdentifier(deviceTableName);
			List<string> names = new List<string>();
			int index = serverCatalog.IndexOf(serverTableName, names);
			if ((index >= 0) && (serverCatalog[index].Library != null) && (D4.MetaData.GetTag(serverCatalog[index].MetaData, "Storage.Name", deviceTableName) == deviceTableName))
				serverTableName = serverCatalog[index].Name;
			else
			{
				// if LNames is populated, all but one of them must have a Storage.Name tag specifying a different name for the table
				bool found = false;
				foreach (string name in names)
				{
					TableVar tableVar = (TableVar)serverCatalog[serverCatalog.IndexOfName(name)];
					if (tableVar.Library != null)
						if (D4.MetaData.GetTag(tableVar.MetaData, "Storage.Name", deviceTableName) == deviceTableName)
						{
							serverTableName = tableVar.Name;
							found = true;
						}
				}
				
				// search for a table with ADeviceTableName as it's Storage.Name tag
				if (!found)
					foreach (TableVar tableVar in serverCatalog)
						if (tableVar.Library != null)
							if (D4.MetaData.GetTag(tableVar.MetaData, "Storage.Name", "") == deviceTableName)
							{
								serverTableName = tableVar.Name;
								found = true;
								break;
							}
					
				if (!found)
					serverTableName = DAE.Schema.Object.Qualify(serverTableName, plan.CurrentLibrary.Name);
			}

			return serverTableName;
		}
		
		public int ColumnIndexFromNativeColumnName(TableVar existingTableVar, string nativeColumnName)
		{
			if (existingTableVar != null)
				for (int index = 0; index < existingTableVar.Columns.Count; index++)
					if (D4.MetaData.GetTag(existingTableVar.Columns[index].MetaData, "Storage.Name", "") == nativeColumnName)
						return index;
			return -1;
		}
		
		public unsafe virtual void GetDeviceTables(Plan plan, Catalog serverCatalog, Catalog deviceCatalog, TableVar tableVar)
		{
			string deviceTablesExpression = GetDeviceTablesExpression(tableVar);
			if (deviceTablesExpression != String.Empty)
			{
				SQLCursor cursor = ((SQLDeviceSession)plan.DeviceConnect(this)).Connection.Open(deviceTablesExpression);
				try
				{
					string tableName = String.Empty;
					BaseTableVar localTableVar = null;
					BaseTableVar existingTableVar = null;
					Objects columns = new Objects();
					
					while (cursor.Next())
					{
						if (tableName != (string)cursor[1])
						{
							ConfigureTableVar(plan, localTableVar, columns, deviceCatalog);

							tableName = (string)cursor[1];
							
							// Search for a table with this name unqualified in the server catalog
							string tableTitle = (string)cursor[4];
							localTableVar = new BaseTableVar(GetServerTableName(plan, serverCatalog, tableName), null);
							localTableVar.Owner = plan.User;
							localTableVar.Library = plan.CurrentLibrary;
							localTableVar.Device = this;
							localTableVar.MetaData = new D4.MetaData();
							localTableVar.MetaData.Tags.Add(new D4.Tag("Storage.Name", tableName, true));
							localTableVar.MetaData.Tags.Add(new D4.Tag("Storage.Schema", (string)cursor[0], true));
							
							// if this table is already present in the server catalog, use StorageNames to map the columns
							int existingTableIndex = serverCatalog.IndexOfName(localTableVar.Name);
							if (existingTableIndex >= 0)
								existingTableVar = serverCatalog[existingTableIndex] as BaseTableVar;
							else
								existingTableVar = null;
							
							if (tableTitle == tableName)
								tableTitle = GetTitleFromName(tableName);
							localTableVar.MetaData.Tags.Add(new D4.Tag("Frontend.Title", tableTitle));
							localTableVar.AddDependency(this);
							columns = new Objects();
						}
						
						string nativeColumnName = (string)cursor[2];
						string columnName = FromSQLIdentifier(nativeColumnName);
						string columnTitle = (string)cursor[5];
						string nativeDomainName = (string)cursor[6];
						string domainName = (string)cursor[7];
						int length = Convert.ToInt32(cursor[8]);
						
						int existingColumnIndex = ColumnIndexFromNativeColumnName(existingTableVar, nativeColumnName);
						if (existingColumnIndex >= 0)
							columnName = existingTableVar.Columns[existingColumnIndex].Name;
						
						if (ShouldIncludeColumn(plan, tableName, columnName, nativeDomainName))
						{
							D4.MetaData metaData = new D4.MetaData();
							TableVarColumn column =
								new TableVarColumn
								(
									new Column(columnName, FindScalarType(plan, nativeDomainName, length, metaData)),
									metaData, 
									TableVarColumnType.Stored
								);
								
							if (column.MetaData == null)
								column.MetaData = new D4.MetaData();
							column.MetaData.Tags.Add(new D4.Tag("Storage.Name", nativeColumnName));
							if (nativeDomainName != domainName)
								column.MetaData.Tags.Add(new D4.Tag("Storage.DomainName", domainName));
							
							if (column.MetaData.Tags.Contains("Storage.Length"))
							{
								column.MetaData.Tags.Add(new D4.Tag("Frontend.Width", Math.Min(Math.Max(3, length), 40).ToString()));
								if (length >= 0) // A (n)varchar(max) column in Microsoft SQL Server will reconcile with length -1
									column.Constraints.Add(Compiler.CompileTableVarColumnConstraint(plan, localTableVar, column, new D4.ConstraintDefinition("LengthValid", new BinaryExpression(new CallExpression("Length", new Expression[]{new IdentifierExpression(D4.Keywords.Value)}), D4.Instructions.InclusiveLess, new ValueExpression(length)), null)));
							}
							
							if (columnTitle == nativeColumnName)
								columnTitle = GetTitleFromName(nativeColumnName);
								
							column.MetaData.Tags.AddOrUpdate("Frontend.Title", columnTitle);
								
							if (Convert.ToInt32(cursor[9]) != 0)
								column.IsNilable = true;

							if (Convert.ToInt32(cursor[10]) != 0)
								column.MetaData.Tags.AddOrUpdate("Storage.Deferred", "true");
								
							columns.Add(column);
							localTableVar.Dependencies.Ensure((Schema.ScalarType)column.DataType);
						}
					}	// while

					ConfigureTableVar(plan, localTableVar, columns, deviceCatalog);
				}
				catch (Exception E)
				{
					throw new SQLException(SQLException.Codes.ErrorReadingDeviceTables, E);
				}
				finally
				{
					cursor.Command.Connection.Close(cursor);
				}
			}
		}

		private string _deviceIndexesExpression = String.Empty;
		public string DeviceIndexesExpression
		{
			get { return _deviceIndexesExpression; }
			set { _deviceIndexesExpression = value == null ? String.Empty : value; }
		}
		
		protected virtual string GetDeviceIndexesExpression(TableVar tableVar)
		{
			return
				String.Format
				(
					DeviceIndexesExpression,
					(tableVar == null) ? String.Empty : ToSQLIdentifier(tableVar)
				);
		}
		
		public virtual void GetDeviceIndexes(Plan plan, Catalog serverCatalog, Catalog deviceCatalog, TableVar tableVar)
		{
			string deviceIndexesExpression = GetDeviceIndexesExpression(tableVar);
			if (deviceIndexesExpression != String.Empty)
			{
				SQLCursor cursor = ((SQLDeviceSession)plan.DeviceConnect(this)).Connection.Open(deviceIndexesExpression);
				try
				{
					string tableName = String.Empty;
					string indexName = String.Empty;
					BaseTableVar localTableVar = null;
					Key key = null;
					Order order = null;
					OrderColumn orderColumn;
					int columnIndex = -1;
					bool shouldIncludeIndex = true;
					
					while (cursor.Next())
					{
						if (tableName != (string)cursor[1])
						{
							if ((localTableVar != null) && shouldIncludeIndex)
								AttachKeyOrOrder(plan, localTableVar, ref key, ref order);
							else
							{
								key = null;
								order = null;
							}
							
							tableName = (string)cursor[1];
							localTableVar = (BaseTableVar)deviceCatalog[GetServerTableName(plan, serverCatalog, tableName)];
							indexName = (string)cursor[2];
							D4.MetaData metaData = new D4.MetaData();
							metaData.Tags.Add(new D4.Tag("Storage.Name", indexName, true));
							if (Convert.ToInt32(cursor[5]) != 0)
								key = new Key(metaData);
							else
								order = new Order(metaData);
							shouldIncludeIndex = true;
						}
						
						if (indexName != (string)cursor[2])
						{
							if (shouldIncludeIndex)
								AttachKeyOrOrder(plan, localTableVar, ref key, ref order);
							else
							{
								key = null;
								order = null;
							}

							indexName = (string)cursor[2];
							D4.MetaData metaData = new D4.MetaData();
							metaData.Tags.Add(new D4.Tag("Storage.Name", indexName, true));
							if (Convert.ToInt32(cursor[5]) != 0)
								key = new Key(metaData);
							else
								order = new Order(metaData);
							shouldIncludeIndex = true;
						}
						
						if (key != null)
						{
							columnIndex = ColumnIndexFromNativeColumnName(localTableVar, (string)cursor[3]);
							if (columnIndex >= 0)
								key.Columns.Add(localTableVar.Columns[columnIndex]);
							else
								shouldIncludeIndex = false;
						}
						else
						{
							columnIndex = ColumnIndexFromNativeColumnName(localTableVar, (string)cursor[3]);
							if (columnIndex >= 0)
							{
								orderColumn = new OrderColumn(localTableVar.Columns[columnIndex], Convert.ToInt32(cursor[6]) == 0);
								orderColumn.Sort = Compiler.GetSort(plan, orderColumn.Column.DataType);
								orderColumn.IsDefaultSort = true;
								order.Columns.Add(orderColumn);
							}
							else
								shouldIncludeIndex = false;
						}
					}
					
					if (shouldIncludeIndex)
						AttachKeyOrOrder(plan, localTableVar, ref key, ref order);
				}
				catch (Exception E)
				{
					throw new SQLException(SQLException.Codes.ErrorReadingDeviceIndexes, E);
				}
				finally
				{
					cursor.Command.Connection.Close(cursor);
				}
			}
		}

		private string _deviceForeignKeysExpression = String.Empty;
		public string DeviceForeignKeysExpression
		{
			get { return _deviceForeignKeysExpression; }
			set { _deviceForeignKeysExpression = value == null ? String.Empty : value; }
		}
		
		protected virtual string GetDeviceForeignKeysExpression(TableVar tableVar)
		{
			return
				String.Format
				(
					DeviceForeignKeysExpression,
					(tableVar == null) ? String.Empty : ToSQLIdentifier(tableVar)
				);
		}
		
		public virtual void GetDeviceForeignKeys(Plan plan, Catalog serverCatalog, Catalog deviceCatalog, TableVar tableVar)
		{
			string deviceForeignKeysExpression = GetDeviceForeignKeysExpression(tableVar);
			if (deviceForeignKeysExpression != String.Empty)
			{
				SQLCursor cursor = ((SQLDeviceSession)plan.DeviceConnect(this)).Connection.Open(deviceForeignKeysExpression);
				try
				{
					string constraintName = String.Empty;
					TableVar sourceTableVar = null;
					TableVar targetTableVar = null;
					Reference reference = null;
					int columnIndex;
					bool shouldIncludeReference = true;
					
					while (cursor.Next())
					{
						if (constraintName != (string)cursor[1])
						{
							if ((reference != null) && shouldIncludeReference)
							{
								deviceCatalog.Add(reference);
								reference = null;
							}
							
							constraintName = (string)cursor[1];
							string sourceTableName = GetServerTableName(plan, serverCatalog, (string)cursor[3]);
							string targetTableName = GetServerTableName(plan, serverCatalog, (string)cursor[6]);
							if (deviceCatalog.Contains(sourceTableName))
								sourceTableVar = (TableVar)deviceCatalog[sourceTableName];
							else
								sourceTableVar = null;
								
							if (deviceCatalog.Contains(targetTableName))
								targetTableVar = (TableVar)deviceCatalog[targetTableName];
							else
								targetTableVar = null;
							
							shouldIncludeReference = (sourceTableVar != null) && (targetTableVar != null);
							if (shouldIncludeReference)
							{
								D4.MetaData metaData = new D4.MetaData();
								metaData.Tags.Add(new D4.Tag("Storage.Name", constraintName, true));
								metaData.Tags.Add(new D4.Tag("Storage.Schema", (string)cursor[0], true));
								metaData.Tags.Add(new D4.Tag("DAE.Enforced", "false", true));
								reference = new Schema.Reference(DAE.Schema.Object.Qualify(FromSQLIdentifier(constraintName), DAE.Schema.Object.Qualifier(sourceTableVar.Name)));
								reference.MergeMetaData(metaData);
								reference.Owner = plan.User;
								reference.Library = plan.CurrentLibrary;
								reference.SourceTable = sourceTableVar;
								reference.TargetTable = targetTableVar;
								reference.Enforced = false;
							}
						}
						
						if (reference != null)
						{
							columnIndex = sourceTableVar.Columns.IndexOf(FromSQLIdentifier((string)cursor[4]));
							if (columnIndex >= 0)
								reference.SourceKey.Columns.Add(sourceTableVar.Columns[columnIndex]);
							else
								shouldIncludeReference = false;
								
							columnIndex = targetTableVar.Columns.IndexOf(FromSQLIdentifier((string)cursor[7]));
							if (columnIndex >= 0)
								reference.TargetKey.Columns.Add(targetTableVar.Columns[columnIndex]);
							else
								shouldIncludeReference = false;
						}
					}
					
					if ((reference != null) && shouldIncludeReference)
						deviceCatalog.Add(reference);
				}
				catch (Exception E)
				{
					throw new SQLException(SQLException.Codes.ErrorReadingDeviceForeignKeys, E);
				}
				finally
				{
					cursor.Command.Connection.Close(cursor);
				}
			}
		}

		public override Catalog GetDeviceCatalog(ServerProcess process, Catalog serverCatalog, TableVar tableVar)
		{
			Catalog catalog = base.GetDeviceCatalog(process, serverCatalog, tableVar);

			using (Plan plan = new Plan(process))
			{
				GetDeviceTables(plan, serverCatalog, catalog, tableVar);
				GetDeviceIndexes(plan, serverCatalog, catalog, tableVar);
				GetDeviceForeignKeys(plan, serverCatalog, catalog, tableVar);
				
				// Impose a key on each table if one is not defined by the device
				foreach (Schema.Object objectValue in catalog)
					if ((objectValue is TableVar) && (((TableVar)objectValue).Keys.Count == 0))
					{
						TableVar localTableVar = (TableVar)objectValue;
						Key key = new Key();
						foreach (TableVarColumn column in localTableVar.Columns)
							if (!Convert.ToBoolean(D4.MetaData.GetTag(column.MetaData, "Storage.Deferred", "false")))
								key.Columns.Add(column);
						key.IsGenerated = true;
						key.MetaData = new D4.MetaData();
						key.MetaData.Tags.Add(new D4.Tag("Storage.IsImposedKey", "true"));
						localTableVar.Keys.Add(key);
					}
			}

			return catalog;
		}

		private string _connectionClass = String.Empty;
		public string ConnectionClass
		{
			get { return _connectionClass; }
			set { _connectionClass = value == null ? String.Empty : value; }
		}

		private string _connectionStringBuilderClass = String.Empty;
		public string ConnectionStringBuilderClass
		{
			get { return _connectionStringBuilderClass; }
			set { _connectionStringBuilderClass = value == null ? String.Empty : value; }
		}

		public void GetConnectionParameters (D4.Tags tags, Schema.DeviceSessionInfo deviceSessionInfo)
		{
			StringToTags(ConnectionParameters, tags);
			StringToTags(deviceSessionInfo.ConnectionParameters, tags);
		}
		
		public static string TagsToString(Language.D4.Tags tags)
		{
			StringBuilder result = new StringBuilder();
			#if USEHASHTABLEFORTAGS
			foreach (D4.Tag tag in ATags)
			{
			#else
			D4.Tag tag;
			for (int index = 0; index < tags.Count; index++)
			{
				tag = tags[index];
			#endif
				if (result.Length > 0)
					result.Append(";");
				result.AppendFormat("{0}={1}", tag.Name, tag.Value);
			}
			return result.ToString();
		}

		public static void StringToTags(String stringValue, Language.D4.Tags tags)
		{
			if (stringValue != null && stringValue != "")
			{
				string[] array = stringValue.Split(';');
				for (int index = 0; index < array.Length; index++)
				{
					string[] tempArray = array[index].Split('=');
					if (tempArray.Length != 2)
						throw new Schema.SchemaException(DAE.Schema.SchemaException.Codes.InvalidConnectionString);
					tags.AddOrUpdate(tempArray[0], tempArray[1]);
				}
			}
		}

		/// <summary>Determines the default buffer size for SQLDeviceCursors created for this device.</summary>
		/// <remarks>A value of 0 for this property indicates that the SQLDeviceCursors should be disconnected, i.e. the entire result set will be cached by the device on open.</remarks>
		private int _connectionBufferSize = SQLDeviceCursor.DefaultBufferSize;
		public int ConnectionBufferSize
		{
			get { return _connectionBufferSize; }
			set { _connectionBufferSize = value; }
		}
	}
	
	public class SQLConnectionHeader : Disposable
	{
		public SQLConnectionHeader(SQLConnection connection) : base() 
		{
			_connection = connection;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_connection != null)
			{
				_connection.Dispose();
				_connection = null;
			}
			
			base.Dispose(disposing);
		}
		
		private SQLConnection _connection;
		public SQLConnection Connection { get { return _connection; } }
		
		private SQLDeviceCursor _deviceCursor;
		public SQLDeviceCursor DeviceCursor 
		{ 
			get { return _deviceCursor; } 
			set { _deviceCursor = value; }
		}
	}

	#if USETYPEDLIST	
	public class SQLConnectionPool : DisposableTypedList
	{
		public SQLConnectionPool() : base(typeof(SQLConnectionHeader)) {}
		
		public new SQLConnectionHeader this[int AIndex]
		{
			get { return (SQLConnectionHeader)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class SQLConnectionPool : DisposableList<SQLConnectionHeader>
	{
	#endif
		/// <returns>Returns the first avaiable connection in the pool.  Will return null, if there are no available connections.</returns>
		public SQLConnectionHeader AvailableConnectionHeader()
		{
			for (int index = 0; index < Count; index++)
				if (this[index].Connection.State == SQLConnectionState.Idle)
				{
					Move(index, Count - 1);
					return this[Count - 1];
				}
			return null;
		}
		
		public int IndexOfConnection(SQLConnection connection)
		{
			for (int index = 0; index < Count; index++)
				if (System.Object.ReferenceEquals(this[index].Connection, connection))
					return index;
			return -1;
		}
	}
	
	public class SQLJoinContext : System.Object
	{
		public SQLJoinContext(SQLQueryContext leftQueryContext, SQLQueryContext rightQueryContext) : base()
		{
			LeftQueryContext = leftQueryContext;
			RightQueryContext = rightQueryContext;
		}

		public SQLQueryContext LeftQueryContext;
		public SQLQueryContext RightQueryContext;
	}
	
	#if USETYPEDLIST
	public class SQLJoinContexts : TypedList
	{
		public SQLJoinContexts() : base(typeof(SQLJoinContext), /* AllowNulls */ true) {}
		
		public new SQLJoinContext this[int AIndex]
		{
			get { return (SQLJoinContext)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class SQLJoinContexts : BaseList<SQLJoinContext> { }
	#endif
	
	[Flags]
	public enum SQLReferenceFlags 
	{ 
		None = 0, 
		HasAggregateExpressions = 1, // Indicates that the expression contains aggregate expressions in the outer-most query context
		HasSubSelectExpressions = 2, // Indicates that the expression contains subselect expressions in the outer-most query context
		HasCorrelation = 4, // Indicates that the expression contains a correlation (references a query-context outside the outer-most query context of the expression)
		HasParameters = 8 // Indicates that the expression contains parameters and is therefore not a literal in the SQL statement
	}
	
	public class SQLRangeVarColumn : System.Object
	{
		public SQLRangeVarColumn(TableVarColumn tableVarColumn, string tableAlias, string columnName)
		{
			_tableVarColumn = tableVarColumn;
			TableAlias = tableAlias;
			ColumnName = columnName;
			Alias = ColumnName;
		}
		
		public SQLRangeVarColumn(TableVarColumn tableVarColumn, string tableAlias, string columnName, string alias)
		{
			_tableVarColumn = tableVarColumn;
			TableAlias = tableAlias;
			ColumnName = columnName;
			Alias = alias;
		}
		
		public SQLRangeVarColumn(TableVarColumn tableVarColumn, Expression expression, string alias)
		{
			_tableVarColumn = tableVarColumn;
			_expression = expression;
			Alias = alias;
		}
		
		private TableVarColumn _tableVarColumn;
		/// <summary>The table var column in the D4 expression.  This is the unique identifier for the range var column.</summary>
		public TableVarColumn TableVarColumn { get { return _tableVarColumn; } }
		
		private string _tableAlias = String.Empty;
		/// <summary>The name of the range var containing the column in the current query context.  If this is empty, the expression will be specified, and vice versa.</summary>
		public string TableAlias
		{
			get { return _tableAlias; }
			set { _tableAlias = value == null ? String.Empty : value; }
		}
		
		private string _columnName = String.Empty;
		/// <summary>The name of the column in the table in the target system.  If the column name is specified, the expression will be null, and vice versa.</summary>
		public string ColumnName
		{
			get { return _columnName; }
			set 
			{ 
				_columnName = value == null ? String.Empty : value; 
				if (_columnName != String.Empty)
					_expression = null;
			}
		}
		
		private Expression _expression;
		/// <summary>Expression is the expression used to define this column. Will be null if this is a base column reference.</summary>
		public Expression Expression
		{
			get { return _expression; }
			set 
			{ 
				_expression = value; 
				if (_expression != null)
				{
					_tableAlias = String.Empty;
					_columnName = String.Empty;
				}
			}
		}
		
		private string _alias = String.Empty;
		/// <summary>The alias name for this column in the current query context, if specified.</summary>
		public string Alias
		{
			get { return _alias; }
			set { _alias = value == null ? String.Empty : value; }
		}
		
		// ReferenceFlags
		private SQLReferenceFlags _referenceFlags;
		public SQLReferenceFlags ReferenceFlags
		{
			get { return _referenceFlags; }
			set { _referenceFlags = value; }
		}

		// GetExpression - returns an expression suitable for use in a where clause or other referencing context
		public Expression GetExpression()
		{
			if (_expression == null)
				return new QualifiedFieldExpression(_columnName, _tableAlias);
			else
				return _expression;
		}
		
		// GetColumnExpression - returns a ColumnExpression suitable for use in a select list
		public ColumnExpression GetColumnExpression()
		{
			return new ColumnExpression(GetExpression(), _alias);
		}
	}
	
	#if USETYPEDLIST
	public class SQLRangeVarColumns : TypedList
	{
		public SQLRangeVarColumns() : base(typeof(SQLRangeVarColumn)) {}
		
		public new SQLRangeVarColumn this[int AIndex]
		{
			get { return (SQLRangeVarColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
	#else
	public class SQLRangeVarColumns : BaseList<SQLRangeVarColumn>
	{
	#endif
		public SQLRangeVarColumn this[string columnName] { get { return this[IndexOf(columnName)]; } }
		
		public int IndexOf(string columnName)
		{
			if (columnName.IndexOf(Keywords.Qualifier) == 0)
			{
				for (int index = 0; index < Count; index++)
					if (Schema.Object.NamesEqual(this[index].TableVarColumn.Name, columnName))
						return index;
			}
			else
			{
				for (int index = 0; index < Count; index++)
					if (this[index].TableVarColumn.Name == columnName)
						return index;
			}
			return -1;
		}
		
		public bool Contains(string columnName)
		{
			return IndexOf(columnName) >= 0;
		}
	}
	
	public class SQLRangeVar : System.Object
	{
		public SQLRangeVar(string name)
		{
		   _name = name;
		}

		// Name
		private string _name;
		public string Name { get { return _name; } }

		// Columns
		private SQLRangeVarColumns _columns = new SQLRangeVarColumns();
		public SQLRangeVarColumns Columns { get { return _columns; } }
	}

	#if USETYPEDLIST
	public class SQLRangeVars : TypedList
	{
		public SQLRangeVars() : base(typeof(SQLRangeVar)) {}

		public new SQLRangeVar this[int AIndex]
		{
			get { return (SQLRangeVar)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class SQLRangeVars : BaseList<SQLRangeVar> { }
	#endif
	
	public class SQLQueryContext : System.Object
	{
		public SQLQueryContext() : base() {}
		
		public SQLQueryContext(SQLQueryContext parentContext) : base()
		{
			_parentContext = parentContext;
		}
		
		public SQLQueryContext(SQLQueryContext parentContext, bool isNestedFrom) : base()
		{
			_parentContext = parentContext;
			_isNestedFrom = isNestedFrom;
		}

		public SQLQueryContext(SQLQueryContext parentContext, bool isNestedFrom, bool isScalarContext) : base()
		{
			_parentContext = parentContext;
			_isNestedFrom = isNestedFrom;
			_isScalarContext = isScalarContext;
		}

		private SQLRangeVars _rangeVars = new SQLRangeVars();
		public SQLRangeVars RangeVars { get { return _rangeVars; } }
		
		private SQLRangeVarColumns _addedColumns = new SQLRangeVarColumns();
		public SQLRangeVarColumns AddedColumns { get { return _addedColumns; } }

		private SQLQueryContext _parentContext;
		public SQLQueryContext ParentContext { get { return _parentContext; } }

		private bool _isNestedFrom;
		public bool IsNestedFrom { get { return _isNestedFrom; } }
		
		private bool _isScalarContext;
		public bool IsScalarContext { get { return _isScalarContext; } }

		// True if this query context is an aggregate expression		
		private bool _isAggregate;
		public bool IsAggregate 
		{ 
			get { return _isAggregate; }
			set { _isAggregate = value; }
		}
		
		// True if this query context contains computed columns
		private bool _isExtension;
		public bool IsExtension
		{
			get { return _isExtension; }
			set { _isExtension = value; }
		}
		
		private bool _isSelectClause;
		public bool IsSelectClause
		{
			get { return _isSelectClause; }
			set { _isSelectClause = value; }
		}

		private bool _isWhereClause;
		public bool IsWhereClause
		{
			get { return _isWhereClause; }
			set { _isWhereClause = value; }
		}
		
		private bool _isGroupByClause;
		public bool IsGroupByClause
		{
			get { return _isGroupByClause; }
			set { _isGroupByClause = value; }
		}

		private bool _isHavingClause;
		public bool IsHavingClause
		{
			get { return _isHavingClause; }
			set { _isHavingClause = value; }
		}
		
		private bool _isListContext;
		public bool IsListContext
		{
			get { return _isListContext; }
			set { _isListContext = value; }
		}

		// ReferenceFlags - These flags are set when the appropriate type of reference has taken place in this context
		private SQLReferenceFlags _referenceFlags;
		public SQLReferenceFlags ReferenceFlags
		{
			get { return _referenceFlags; }
			set { _referenceFlags = value; }
		}
		
		public void ResetReferenceFlags()
		{
			_referenceFlags = SQLReferenceFlags.None;
		}
		
		public SQLRangeVarColumn GetRangeVarColumn(string identifier)
		{
			SQLRangeVarColumn column = FindRangeVarColumn(identifier);
			if (column == null)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, identifier);
			return column;
		}
		
		public SQLRangeVarColumn FindRangeVarColumn(string identifier)
		{
			if (_addedColumns.Contains(identifier))
				return _addedColumns[identifier];
				
			foreach (SQLRangeVar rangeVar in _rangeVars)
				if (rangeVar.Columns.Contains(identifier))
					return rangeVar.Columns[identifier];
				
			return null;
		}
		
		public void ProjectColumns(Schema.TableVarColumns columns)
		{
			for (int index = _addedColumns.Count - 1; index >= 0; index--)
				if (!columns.Contains(_addedColumns[index].TableVarColumn))
					_addedColumns.RemoveAt(index);
				
			foreach (SQLRangeVar rangeVar in _rangeVars)
				for (int index = rangeVar.Columns.Count - 1; index >= 0; index--)
					if (!columns.Contains(rangeVar.Columns[index].TableVarColumn))
						rangeVar.Columns.RemoveAt(index);
		}
		
		public void RemoveColumn(SQLRangeVarColumn column)
		{
			if (_addedColumns.Contains(column))
				_addedColumns.Remove(column);
			
			foreach (SQLRangeVar rangeVar in _rangeVars)
				if (rangeVar.Columns.Contains(column))
					rangeVar.Columns.Remove(column);
		}

		/// <summary>
		/// Renames the given column and returns the new column.
		/// </summary>
		/// <param name="devicePlan">The device plan supporting the translation.</param>
		/// <param name="oldColumn">The column to be renamed.</param>
		/// <param name="newColumn">The new column.</param>
		/// <returns>The renamed SQLRangeVarColumn instance.</returns>
		/// <remarks>
		/// Note that this method removes the old column from the query context and DOES NOT add the new column.
		/// The returned column must be added to the query context by the caller. This is done because a rename
		/// operation may in general contain name exchanges, so the columns must all be removed and then re-added
		/// to the query context after all columns have been renamed to avoid the possibility of a subsequent
		/// column rename finding the newly added column for a rename in the same operation.
		/// </remarks>
		public SQLRangeVarColumn RenameColumn(SQLDevicePlan devicePlan, Schema.TableVarColumn oldColumn, Schema.TableVarColumn newColumn)
		{
			SQLRangeVarColumn localOldColumn = GetRangeVarColumn(oldColumn.Name);
			SQLRangeVarColumn localNewColumn;
			if (localOldColumn.ColumnName != String.Empty)
				localNewColumn = new SQLRangeVarColumn(newColumn, localOldColumn.TableAlias, localOldColumn.ColumnName);
			else
				localNewColumn = new SQLRangeVarColumn(newColumn, localOldColumn.Expression, localOldColumn.Alias);
			
			RemoveColumn(localOldColumn);
			
			localNewColumn.ReferenceFlags = localOldColumn.ReferenceFlags;
			localNewColumn.Alias = devicePlan.Device.ToSQLIdentifier(newColumn.Name);
			
			return localNewColumn;
		}
	}

    public class SQLDevicePlan : DevicePlan
    {
		public SQLDevicePlan(Plan plan, SQLDevice device, PlanNode planNode) : base(plan, device, planNode) 
		{
			if ((planNode != null) && (planNode.DeviceNode != null))
				_devicePlanNode = (SQLDevicePlanNode)planNode.DeviceNode;
		}
		
		public new SQLDevice Device { get { return (SQLDevice)base.Device; } }

		// SQLQueryContexts
		private SQLQueryContext _queryContext = new SQLQueryContext();
		public void PushQueryContext(bool isNestedFrom)
		{
			_queryContext = new SQLQueryContext(_queryContext, isNestedFrom, false);
		}

		public void PushQueryContext()
		{
			PushQueryContext(false);
		}
	
		public void PopQueryContext()
		{
			_queryContext = _queryContext.ParentContext;
		}

		public SQLQueryContext CurrentQueryContext()
		{
			if (_queryContext == null)
				throw new SQLException(SQLException.Codes.NoCurrentQueryContext);
			return _queryContext;
		}
		
		public void PushScalarContext()
		{
			_queryContext = new SQLQueryContext(_queryContext, false, true);
		}

		public void PopScalarContext()
		{
			PopQueryContext();
		}

		// Internal stack used for translation		
		private Symbols _stack = new Symbols();
		public Symbols Stack { get { return _stack; } }
		
		// JoinContexts (only used for natural join translation)
		private SQLJoinContexts _joinContexts = new SQLJoinContexts();
		public void PushJoinContext(SQLJoinContext joinContext)
		{
			_joinContexts.Add(joinContext);
		}
		
		public void PopJoinContext()
		{
			_joinContexts.RemoveAt(_joinContexts.Count - 1);
		}
		
		// This will return null if the join being translated is not a natural join
		public SQLJoinContext CurrentJoinContext()
		{
			if (_joinContexts.Count == 0)
				throw new SQLException(SQLException.Codes.NoCurrentJoinContext);
			return _joinContexts[_joinContexts.Count - 1];
		}
		
		public bool HasJoinContext()
		{
			return (CurrentJoinContext() != null);
		}
		
		// IsSubSelectSupported() - returns true if a subselect is supported in the current context
		public bool IsSubSelectSupported()
		{
			SQLQueryContext context = CurrentQueryContext();
			return 
				context.IsScalarContext &&
				(
					!(context.IsSelectClause || context.IsWhereClause || context.IsGroupByClause || context.IsHavingClause) ||
					(context.IsSelectClause && Device.SupportsSubSelectInSelectClause) ||
					(context.IsWhereClause && Device.SupportsSubSelectInWhereClause) ||
					(context.IsGroupByClause && Device.SupportsSubSelectInGroupByClause) ||
					(context.IsHavingClause && Device.SupportsSubSelectInHavingClause)
				);
		}
		
		public string GetSubSelectNotSupportedReason()
		{
			SQLQueryContext context = CurrentQueryContext();
			if (context.IsSelectClause && !Device.SupportsSubSelectInSelectClause)
				return "Plan is not supported because the device does not support sub-selects in the select clause.";
			if (context.IsWhereClause && !Device.SupportsSubSelectInWhereClause)
				return "Plan is not supported because the device does not support sub-selects in the where clause.";
			if (context.IsGroupByClause && !Device.SupportsSubSelectInGroupByClause)
				return "Plan is not supported because the device does not support sub-selects in the group by clause.";
			if (context.IsHavingClause && !Device.SupportsSubSelectInHavingClause)
				return "Plan is not supported because the device does not support sub-selects in the having clause.";
			return String.Empty;
		}
		
		// IsBooleanContext
		private ArrayList _contexts = new ArrayList();
		public bool IsBooleanContext()
		{
			return (_contexts.Count > 0) && ((bool)_contexts[_contexts.Count - 1]);
		}
		
		public void EnterContext(bool isBooleanContext)
		{
			_contexts.Add(isBooleanContext);
		}
		
		public void ExitContext()
		{
			_contexts.RemoveAt(_contexts.Count - 1);
		}
		
		private int _counter = 0;
		private List<string> _tableAliases = new List<string>();
		public string GetNextTableAlias()
		{
			_counter++;
			string tableAlias = String.Format("T{0}", _counter.ToString());
			_tableAliases.Add(tableAlias);
			return tableAlias;
		}

		private SQLDevicePlanNode _devicePlanNode;		
		public SQLDevicePlanNode DevicePlanNode
		{
			get { return _devicePlanNode; }
			set { _devicePlanNode = value; }
		}
		
		public SQLRangeVarColumn GetRangeVarColumn(string identifier, bool currentContextOnly)
		{
			SQLRangeVarColumn column = FindRangeVarColumn(identifier, currentContextOnly);
			if (column == null)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, identifier);
			return column;
		}
		
		public SQLRangeVarColumn FindRangeVarColumn(string identifier, bool currentContextOnly)
		{
			SQLRangeVarColumn column = null;
			bool inCurrentContext = true;
			SQLQueryContext queryContext = CurrentQueryContext();
			while (queryContext != null)
			{
				column = queryContext.FindRangeVarColumn(identifier);
				if (column != null)
					break;

				if (queryContext.IsScalarContext)
					queryContext = queryContext.ParentContext;
				else
				{
					if (currentContextOnly || (queryContext.IsNestedFrom && !Device.SupportsNestedCorrelation))
						break;
					
					queryContext = queryContext.ParentContext;
					inCurrentContext = false;
				}
			}

			if (column != null)
			{
				if (!inCurrentContext)
					CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasCorrelation;
					
				CurrentQueryContext().ReferenceFlags |= column.ReferenceFlags;
			}
				
			return column;
		}
    }
    
    public class SQLDevicePlanNode : DevicePlanNode
    {
		public SQLDevicePlanNode(PlanNode planNode) : base(planNode) {}
		
		private Statement _statement;
		public Statement Statement
		{
			get { return _statement; }
			set { _statement = value; }
		}

		// Parameters
		private SQLPlanParameters _planParameters = new SQLPlanParameters();
		public SQLPlanParameters PlanParameters { get { return _planParameters; } }
    }

	public class TableSQLDevicePlanNode : SQLDevicePlanNode
	{
		public TableSQLDevicePlanNode(PlanNode planNode) : base(planNode) {}

		private bool _isAggregate;
		public bool IsAggregate
		{
			get { return _isAggregate; }
			set { _isAggregate = value; }
		}
	}

	public class ScalarSQLDevicePlanNode : SQLDevicePlanNode
	{
		public ScalarSQLDevicePlanNode(PlanNode planNode) : base(planNode) {}

		private SQLScalarType _mappedType;
		public SQLScalarType MappedType
		{
			get { return _mappedType; }
			set { _mappedType = value; }
		}
	}

	public class RowSQLDevicePlanNode : SQLDevicePlanNode
	{
		public RowSQLDevicePlanNode(PlanNode planNode) : base(planNode) {}
		
		private List<SQLScalarType> _mappedTypes = new List<SQLScalarType>();
		public List<SQLScalarType> MappedTypes { get { return _mappedTypes; } }
	}
    
	public class SQLPlanParameter  : System.Object
	{
		public SQLPlanParameter(SQLParameter sQLParameter, PlanNode planNode, SQLScalarType scalarType) : base()
		{	
			_sQLParameter = sQLParameter;
			_planNode = planNode; 
			_scalarType = scalarType;
		}
		
		private SQLParameter _sQLParameter;
		public SQLParameter SQLParameter { get { return _sQLParameter; } }

		private PlanNode _planNode;
		public PlanNode PlanNode { get { return _planNode; } }

		private SQLScalarType _scalarType;
		public SQLScalarType ScalarType { get { return _scalarType; } }
	}

	#if USETYPEDLIST
	public class SQLPlanParameters : TypedList
	{
		public SQLPlanParameters() : base(typeof(SQLPlanParameter)){}
		
		public new SQLPlanParameter this[int AIndex]
		{
			get { return (SQLPlanParameter)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class SQLPlanParameters : BaseList<SQLPlanParameter> { }
	#endif

	public abstract class SQLDeviceSession : DeviceSession, IStreamProvider
	{
		public SQLDeviceSession(SQLDevice device, ServerProcess process, Schema.DeviceSessionInfo deviceSessionInfo) : base(device, process, deviceSessionInfo) {}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (Transactions != null)
					EnsureTransactionsRolledback();
			}
			finally
			{
				try
				{
					if (_streams != null)
					{
						DestroyStreams();
						_streams = null;
					}
				}
				finally
				{
					try
					{
						if (_browsePool != null)
						{
							if (Device.UseTransactions)
							{
								for (int index = 0; index < _browsePool.Count; index++)
									try
									{
										if (_browsePool[index].Connection.InTransaction)
											_browsePool[index].Connection.CommitTransaction();
									}
									catch {}
							}

							_browsePool.Dispose();
							_browsePool = null;
						}
					}
					finally
					{
						try
						{
							if (_executePool != null)
							{
								_executePool.Dispose();
								_executePool = null;
							}
						}
						finally
						{
							base.Dispose(disposing); // Call the base here to clean up any outstanding transactions
						}
					}
				}
			}
		}
		
		public new SQLDevice Device { get { return (SQLDevice)base.Device; } }

		protected abstract SQLConnection InternalCreateConnection();
		protected SQLConnection CreateConnection()
		{
			SQLConnection connection = InternalCreateConnection();
			connection.DefaultCommandTimeout = Device.CommandTimeout;
			connection.DefaultUseParametersForCursors = Device.UseParametersForCursors;
			connection.DefaultShouldNormalizeWhitespace = Device.ShouldNormalizeWhitespace;
			return connection;
		}
		
		private SQLConnectionPool _browsePool = new SQLConnectionPool();
		private SQLConnectionPool _executePool = new SQLConnectionPool();
		
		public SQLConnection Connection { get { return RequestConnection(false).Connection; } }
		
		private bool _executingConnectionStatement;
		
		protected virtual void ExecuteConnectStatement(SQLConnectionHeader connectionHeader, bool isBrowseCursor)
		{
			if (!ServerProcess.IsLoading())
			{
				if (!_executingConnectionStatement)
				{
					_executingConnectionStatement = true;
					try
					{
						string executeStatementExpression = isBrowseCursor ? Device.OnBrowseConnectStatement : Device.OnExecuteConnectStatement;
						if (executeStatementExpression != null)
						{
							string executeStatement = null;
							IServerExpressionPlan plan = ((IServerProcess)ServerProcess).PrepareExpression(executeStatementExpression, null);
							try
							{
								executeStatement = ((IScalar)plan.Evaluate(null)).AsString;
							}
							finally
							{
								((IServerProcess)ServerProcess).UnprepareExpression(plan);
							}
							
							connectionHeader.Connection.Execute(executeStatement);
							
							if (connectionHeader.Connection.InTransaction)
							{
								connectionHeader.Connection.CommitTransaction();
								connectionHeader.Connection.BeginTransaction(_isolationLevel);
							}
						}
					}
					finally
					{
						_executingConnectionStatement = false;
					}
				}
				else
					throw new SQLException(SQLException.Codes.AlreadyExecutingConnectionStatement, Device.Name);
			}
		}
		
		public virtual SQLConnectionHeader RequestConnection(bool isBrowseCursor)
		{
			SQLConnectionHeader connectionHeader = isBrowseCursor ? _browsePool.AvailableConnectionHeader() : _executePool.AvailableConnectionHeader();

			// Ensure Transaction Started
			if (Device.UseTransactions && (Transactions.Count > 0) && !_transactionStarted)
				_transactionStarted = true;
			
			if (connectionHeader == null)
			{
				SQLConnection connection = AddConnection(isBrowseCursor);
				if (connection != null)
				{
					connectionHeader = new SQLConnectionHeader(connection);
					if (isBrowseCursor)
						_browsePool.Add(connectionHeader);
					else
						_executePool.Add(connectionHeader);
					try
					{
						ExecuteConnectStatement(connectionHeader, isBrowseCursor);
					}
					catch
					{
						if (isBrowseCursor)
							_browsePool.Remove(connectionHeader);
						else
							_executePool.Remove(connectionHeader);
						connectionHeader.Dispose();
						throw;
					}
				}
			}

			if (connectionHeader == null)
			{
				// Perform a Cursor switch here to free up a connection on the transaction
				// This means that all connections in the execute pool are currently supporting open cursors.
				// Select the connection victim (the first connection in the execute pool)
				connectionHeader = _executePool[0];
				
				// Move the connection to the back of the list for round-robin pool replacement
				_executePool.Move(0, _executePool.Count - 1);

				if (connectionHeader.DeviceCursor != null)				
					connectionHeader.DeviceCursor.ReleaseConnection(connectionHeader);
			}
			
			// If the connection is closed, throw out the connection
			if (!connectionHeader.Connection.IsConnectionValid())
			{
				if (isBrowseCursor)
					_browsePool.Remove(connectionHeader);
				else
					_executePool.Remove(connectionHeader);

				connectionHeader.Dispose();
				connectionHeader = RequestConnection(isBrowseCursor);
			}
			
			// Ensure the connection has an active transaction
			if (Device.UseTransactions && (Transactions.Count > 0) && !isBrowseCursor && !connectionHeader.Connection.InTransaction)
				connectionHeader.Connection.BeginTransaction(_isolationLevel);
			
			return connectionHeader;
		}
		
		public void ReleaseConnection(SQLConnection connection)
		{
			ReleaseConnection(connection, false);
		}
		
		public virtual void ReleaseConnection(SQLConnection connection, bool disposing)
		{
			int index = _browsePool.IndexOfConnection(connection);
			if (index >= 0)
			{
				var header = _browsePool[index];
				if (header.DeviceCursor != null)
					header.DeviceCursor.ReleaseConnection(header, disposing);
					
				if (header.Connection.InTransaction)
					header.Connection.CommitTransaction();
					
				header.Dispose(); // Always release browse connections
			}
			else
			{
				index = _executePool.IndexOfConnection(connection);
				if (index >= 0)
				{
					var header = _executePool[index];
					if (header.DeviceCursor != null)
						header.DeviceCursor.ReleaseConnection(header, disposing);
						
					if (!Device.UseTransactions || Transactions.Count == 0)
						header.Dispose();
				}
				else
					throw new SQLException(SQLException.Codes.ConnectionNotFound);
			}
		}

		// This override allows for the implementation of transaction binding across connections such as distributed transactions or session binding.		
		protected virtual SQLConnection AddConnection(bool isBrowseCursor)
		{
			if (isBrowseCursor)
			{
				SQLConnection connection = CreateConnection();
				if (Device.UseTransactions && (Transactions.Count > 0))
					connection.BeginTransaction(SQLIsolationLevel.ReadUncommitted);
				return connection;
			}
			else
			{
				if (_executePool.Count == 0)
				{
					SQLConnection connection = CreateConnection();
					if (Device.UseTransactions && (Transactions.Count > 0) && _transactionStarted)
						connection.BeginTransaction(_isolationLevel);
					return connection;
				}
				else
					return null;
			}
		}
		
		public static SQLIsolationLevel IsolationLevelToSQLIsolationLevel(IsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
				case IsolationLevel.Browse : return SQLIsolationLevel.ReadUncommitted;
				case IsolationLevel.CursorStability : return SQLIsolationLevel.ReadCommitted;
				case IsolationLevel.Isolated : return SQLIsolationLevel.Serializable;
			}
			
			return SQLIsolationLevel.Serializable;
		}
		
		// BeginTransaction
		private SQLIsolationLevel _isolationLevel;
		private bool _transactionStarted;
		protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
		{
			if (Device.UseTransactions && (Transactions.Count == 1)) // If this is the first transaction
			{
				_isolationLevel = IsolationLevelToSQLIsolationLevel(isolationLevel);

				// Transaction starting is deferred until actually necessary
				_transactionStarted = false;
				_transactionFailure = false;
			}
		}
		
		private bool _transactionFailure;
		/// <summary> Indicates whether the DBMS-side transaction has been rolled-back. </summary>
		public bool TransactionFailure
		{
			get { return _transactionFailure; }
			set { _transactionFailure = value; }
		}
		
		protected override bool IsTransactionFailure(Exception exception)
		{
			return _transactionFailure;
		}
		
		protected override void InternalPrepareTransaction() {}

		// CommitTransaction
		protected override void InternalCommitTransaction()
		{
			if (Device.UseTransactions && (Transactions.Count == 1) && _transactionStarted)
			{
				for (int index = _executePool.Count - 1; index >= 0; index--)
				{
					var header = _executePool[index];
					
					if (header.DeviceCursor != null)
						header.DeviceCursor.ReleaseConnection(header, true);
						
					try
					{
						if (header.Connection.InTransaction)
							header.Connection.CommitTransaction();
					}
					catch
					{
						_transactionFailure = header.Connection.TransactionFailure;
						throw;
					}

					// Dispose the connection to release it back to the server
					try
					{
						_executePool.DisownAt(index).Dispose();
					}
					catch
					{
						// Ignore errors disposing the connection, as long as it removed from the execute pool, we are good
					}
				}

				_transactionStarted = false;
			}
		}
		
		// RollbackTransaction
		protected override void InternalRollbackTransaction()
		{
			if (Device.UseTransactions && (Transactions.Count == 1) && _transactionStarted)
			{
				for (int index = _executePool.Count - 1; index >= 0; index--)
				{
					var header = _executePool[index];
					
					if (header.DeviceCursor != null)
						header.DeviceCursor.ReleaseConnection(header, true);
						
					try
					{
						if (header.Connection.InTransaction)
							header.Connection.RollbackTransaction();
					}
					catch
					{
						_transactionFailure = header.Connection.TransactionFailure;
						// Don't rethrow, we do not care if there was an issue rolling back on the server, there's nothing we can do about it here
					}
					
					// Dispose the connection to release it back to the server
					try
					{
						_executePool.DisownAt(index).Dispose();
					}
					catch
					{
						// Ignore errors disposing the connection, as long as it is removed from the execute pool, we are good
					}
				}
			
				for (int index = 0; index < _executePool.Count; index++)
				{
					if (_executePool[index].DeviceCursor != null)
						_executePool[index].DeviceCursor.ReleaseConnection(_executePool[index], true);
						
					try
					{
						if (_executePool[index].Connection.InTransaction)
							_executePool[index].Connection.RollbackTransaction();
					}
					catch
					{
						_transactionFailure = _executePool[index].Connection.TransactionFailure;
					}
				}
				_transactionStarted = false;
			}
		}

		protected virtual SQLTable CreateSQLTable(Program program, TableNode node, SelectStatement selectStatement, SQLParameters parameters, bool isAggregate)
		{
			return new SQLTable(this, program, node, selectStatement, parameters, isAggregate);
		}
		
        protected void SetParameterValueLength(object nativeParamValue, SQLPlanParameter planParameter)
        {
            string stringParamValue = nativeParamValue as String;
            SQLStringType parameterType = planParameter.SQLParameter.Type as SQLStringType;
            if ((parameterType != null) && (stringParamValue != null) && (parameterType.Length != stringParamValue.Length))
                parameterType.Length = stringParamValue.Length <= 20 ? 20 : stringParamValue.Length;
        }

		protected void PrepareSQLParameters(Program program, SQLDevicePlanNode devicePlanNode, bool isCursor, SQLParameters parameters)
		{
			object paramValue;
			object nativeParamValue;
			foreach (SQLPlanParameter planParameter in devicePlanNode.PlanParameters)
			{
				paramValue = planParameter.PlanNode.Execute(program);
                nativeParamValue = (paramValue == null) ? null : planParameter.ScalarType.ParameterFromScalar(program.ValueManager, paramValue);
                if (nativeParamValue != null)
                    SetParameterValueLength(nativeParamValue, planParameter);

				parameters.Add
				(
					new SQLParameter
					(
						planParameter.SQLParameter.Name, 
						planParameter.SQLParameter.Type, 
						nativeParamValue,
						planParameter.SQLParameter.Direction,
						planParameter.SQLParameter.Marker,
						Device.UseParametersForCursors && isCursor ? null : planParameter.ScalarType.ToLiteral(program.ValueManager, paramValue)
					)
				);
			}
		}

		// Execute
		protected override object InternalExecute(Program program, PlanNode planNode)
		{
            if (planNode.DeviceNode == null)
            {
                throw new DeviceException(DeviceException.Codes.UnpreparedDevicePlan, ErrorSeverity.System);
            }

			var devicePlanNode = (SQLDevicePlanNode)planNode.DeviceNode;
			if (planNode is DMLNode)
			{
				SQLConnectionHeader header = RequestConnection(false);                             
				try
				{
					using (SQLCommand command = header.Connection.CreateCommand(false))
					{
						command.Statement = Device.Emitter.Emit(devicePlanNode.Statement);						
						PrepareSQLParameters(program, devicePlanNode, false, command.Parameters);
						command.Execute();
						return null;
					}
				}
				catch
				{
					_transactionFailure = header.Connection.TransactionFailure;
					throw;
				}
			}
			else if (planNode is DDLNode)
			{
				SQLConnectionHeader header = RequestConnection(false);
				try
				{
					if ((!ServerProcess.IsLoading()) && ((Device.ReconcileMode & D4.ReconcileMode.Command) != 0))
					{
						Statement dDLStatement = devicePlanNode.Statement;
						if (dDLStatement is Batch)
						{
							// If this is a batch, split it into separate statements for execution.  This is required because the Oracle ODBC driver does
							// not allow multiple statements to be executed at a time.
							using (SQLCommand command = header.Connection.CreateCommand(false))
							{
								foreach (Statement statement in ((Batch)dDLStatement).Statements)
								{
									string statementString = Device.Emitter.Emit(statement);
									if (statementString != String.Empty)
									{
										command.Statement = statementString;
										command.Execute();
									}
								}
							}
						}
						else
						{
							string statement = Device.Emitter.Emit(devicePlanNode.Statement);
							if (statement != String.Empty)
							{
								using (SQLCommand command = header.Connection.CreateCommand(false))
								{
									command.Statement = statement;									
									PrepareSQLParameters(program, devicePlanNode, false, command.Parameters);
									command.Execute();
								}
							}
						}
					}
					return null;
				}
				catch
				{
					_transactionFailure = header.Connection.TransactionFailure;
					throw;
				}
			}
			else
			{
				if (planNode is TableNode)
				{
					SQLParameters parameters = new SQLParameters();
					PrepareSQLParameters(program, devicePlanNode, true, parameters);
					SQLTable table = CreateSQLTable(program, (TableNode)planNode, (SelectStatement)devicePlanNode.Statement, parameters, ((TableSQLDevicePlanNode)planNode.DeviceNode).IsAggregate);
					try
					{
						table.Open();
						return table;
					}
					catch
					{
						table.Dispose();
						throw;
					}
				}
				else
				{
					SQLConnectionHeader header = RequestConnection(false);
					try
					{
						using (SQLCommand command = header.Connection.CreateCommand(true))
						{
							command.Statement = Device.Emitter.Emit(devicePlanNode.Statement);							
							PrepareSQLParameters(program, devicePlanNode, true, command.Parameters);
							SQLCursor cursor = command.Open(SQLCursorType.Dynamic, IsolationLevelToSQLIsolationLevel(ServerProcess.CurrentIsolationLevel())); //SQLIsolationLevel.ReadCommitted);
							try
							{
								if (cursor.Next())
								{
									if (planNode.DataType is Schema.IScalarType)
									{
										if (cursor.IsNull(0))
											return null;
										else
											return ((ScalarSQLDevicePlanNode)planNode.DeviceNode).MappedType.ToScalar(ServerProcess.ValueManager, cursor[0]);
									}
									else
									{
										Row row = new Row(program.ValueManager, (Schema.IRowType)planNode.DataType);
										var rowDeviceNode = planNode.DeviceNode as RowSQLDevicePlanNode;
										for (int index = 0; index < row.DataType.Columns.Count; index++)
											if (!cursor.IsNull(index))
												row[index] = rowDeviceNode.MappedTypes[index].ToScalar(ServerProcess.ValueManager, cursor[index]);
												
										if (cursor.Next())
											throw new RuntimeException(RuntimeException.Codes.InvalidRowExtractorExpression);

										return row;
									}
								}
								else
									return null;
							}
							finally
							{
								command.Close(cursor);
							}
						}
					}
					catch
					{
						_transactionFailure = header.Connection.TransactionFailure;
						throw;
					}
				}
			}
		}

		// InsertRow
		protected virtual void InternalVerifyInsertStatement(TableVar table, IRow row, InsertStatement statement) {}
        protected override void InternalInsertRow(Program program, TableVar table, IRow row, BitArray valueFlags)
        {
			SQLConnectionHeader header = RequestConnection(false);
			try
			{
				using (SQLCommand command = header.Connection.CreateCommand(false))
				{
					InsertStatement insertStatement = new InsertStatement();
					insertStatement.InsertClause = new InsertClause();
					insertStatement.InsertClause.TableExpression = new TableExpression();
					insertStatement.InsertClause.TableExpression.TableSchema = D4.MetaData.GetTag(table.MetaData, "Storage.Schema", Device.Schema);
					insertStatement.InsertClause.TableExpression.TableName = Device.ToSQLIdentifier(table);
					if (Device.UseValuesClauseInInsert)
					{
						ValuesExpression values = new ValuesExpression();
						insertStatement.Values = values;
						string columnName;
						string parameterName;
						Schema.TableVarColumn column;
						SQLScalarType scalarType;
						for (int index = 0; index < row.DataType.Columns.Count; index++)
						{
							column = table.Columns[table.Columns.IndexOfName(row.DataType.Columns[index].Name)];
							columnName = Device.ToSQLIdentifier(column);
							parameterName = String.Format("P{0}", index.ToString());
							insertStatement.InsertClause.Columns.Add(new InsertFieldExpression(columnName));
							values.Expressions.Add(new QueryParameterExpression(parameterName));
							scalarType = (SQLScalarType)Device.ResolveDeviceScalarType(program.Plan, (Schema.ScalarType)column.DataType);
							command.Parameters.Add
							(
								new SQLParameter
								(
									parameterName,
									scalarType.GetSQLParameterType(column),
									row.HasValue(index) ? scalarType.ParameterFromScalar(program.ValueManager, row[index]) : null,
									SQLDirection.In, 
									Device.GetParameterMarker(scalarType, column)
								)
							);					
						}
					}
					else
					{
						// DB2/400 does not allow typed parameter markers in values clauses
						// We original removed the functionality from the insert statement, but now replaced it for consistency, and use
						// the UseValuesClauseInInsert switch instead. This is because we have to be able to take advantage of the
						// parameter markers capability to force a cast client side in order to use strings to pass values
						// across the CLI, but convert those strings to different types on the target system. This is to work around
						// an issue with passing decimals through the CLI.
						SelectExpression selectExpression = new SelectExpression();
						insertStatement.Values = selectExpression;
						selectExpression.FromClause = new CalculusFromClause(Device.GetDummyTableSpecifier());
						selectExpression.SelectClause = new SelectClause();
						string columnName;
						string parameterName;
						Schema.TableVarColumn column;
						SQLScalarType scalarType;
						for (int index = 0; index < row.DataType.Columns.Count; index++)
						{
							column = table.Columns[table.Columns.IndexOfName(row.DataType.Columns[index].Name)];
							columnName = Device.ToSQLIdentifier(column);
							parameterName = String.Format("P{0}", index.ToString());
							insertStatement.InsertClause.Columns.Add(new InsertFieldExpression(columnName));
							selectExpression.SelectClause.Columns.Add(new ColumnExpression(new QueryParameterExpression(parameterName), columnName));
							scalarType = (SQLScalarType)Device.ResolveDeviceScalarType(program.Plan, (Schema.ScalarType)column.DataType);
							command.Parameters.Add
							(
								new SQLParameter
								(
									parameterName,
									scalarType.GetSQLParameterType(column),
									row.HasValue(index) ? scalarType.ParameterFromScalar(program.ValueManager, row[index]) : null,
									SQLDirection.In,
									Device.GetParameterMarker(scalarType, column)
								)
							);
						}
						
					}
					
					InternalVerifyInsertStatement(table, row, insertStatement);
					if (Device.CommandTimeout >= 0)
						command.CommandTimeout = Device.CommandTimeout;
					command.Statement = Device.Emitter.Emit(insertStatement);
					command.Execute();
				}
			}
			catch
			{
				_transactionFailure = header.Connection.TransactionFailure;
				throw;
			}
        }
        
        // UpdateRow
        protected virtual void InternalVerifyUpdateStatement(TableVar table, IRow oldRow, IRow newRow, UpdateStatement statement) {}
        protected override void InternalUpdateRow(Program program, TableVar table, IRow oldRow, IRow newRow, BitArray valueFlags)
        {
			UpdateStatement statement = new UpdateStatement();
			statement.UpdateClause = new UpdateClause();
			statement.UpdateClause.TableExpression = new TableExpression();
			statement.UpdateClause.TableExpression.TableSchema = D4.MetaData.GetTag(table.MetaData, "Storage.Schema", Device.Schema);
			statement.UpdateClause.TableExpression.TableName = Device.ToSQLIdentifier(table);

			SQLConnectionHeader header = RequestConnection(false);
			try
			{
				using (SQLCommand command = header.Connection.CreateCommand(false))
				{
					string columnName;
					string parameterName;
					Schema.TableVarColumn column;
					SQLScalarType scalarType;			
					for (int index = 0; index < newRow.DataType.Columns.Count; index++)
					{
						if ((valueFlags == null) || valueFlags[index])
						{
							column = table.Columns[index];
							columnName = Device.ToSQLIdentifier(column);
							parameterName = String.Format("P{0}", index.ToString());
							UpdateFieldExpression expression = new UpdateFieldExpression();
							expression.FieldName = columnName;
							expression.Expression = new QueryParameterExpression(parameterName);
							scalarType = (SQLScalarType)Device.ResolveDeviceScalarType(program.Plan, (Schema.ScalarType)column.DataType);
							command.Parameters.Add
							(
								new SQLParameter
								(
									parameterName, 
									scalarType.GetSQLParameterType(column), 
									newRow.HasValue(index) ? scalarType.ParameterFromScalar(program.ValueManager, newRow[index]) : null, 
									SQLDirection.In, 
									Device.GetParameterMarker(scalarType, column)
								)
							);
							statement.UpdateClause.Columns.Add(expression);
						}
					}
					
					if (statement.UpdateClause.Columns.Count > 0)
					{
						Schema.Key clusteringKey = program.FindClusteringKey(table);
						if (clusteringKey.Columns.Count > 0)
						{
							int rowIndex;
							statement.WhereClause = new WhereClause();
							for (int index = 0; index < clusteringKey.Columns.Count; index++)
							{
								column = clusteringKey.Columns[index];
								rowIndex = oldRow.DataType.Columns.IndexOfName(column.Name);
								columnName = Device.ToSQLIdentifier(column);
								parameterName = String.Format("P{0}", (index + newRow.DataType.Columns.Count).ToString());
								scalarType = (SQLScalarType)Device.ResolveDeviceScalarType(program.Plan, (Schema.ScalarType)column.DataType);
								command.Parameters.Add
								(
									new SQLParameter
									(
										parameterName, 
										scalarType.GetSQLParameterType(column), 
										oldRow.HasValue(rowIndex) ? scalarType.ParameterFromScalar(program.ValueManager, oldRow[rowIndex]) : null, 
										SQLDirection.In, 
										Device.GetParameterMarker(scalarType, column)
									)
								);
								Expression expression = 
									new BinaryExpression
									(
										new QualifiedFieldExpression(columnName),
										"iEqual",
										new QueryParameterExpression(parameterName)
									);
									
								if (statement.WhereClause.Expression == null)
									statement.WhereClause.Expression = expression;
								else
									statement.WhereClause.Expression = new BinaryExpression(statement.WhereClause.Expression, "iAnd", expression);
							}
						}
						
						InternalVerifyUpdateStatement(table, oldRow, newRow, statement);
						if (Device.CommandTimeout >= 0)
							command.CommandTimeout = Device.CommandTimeout;
						command.Statement = Device.Emitter.Emit(statement);
						command.Execute();
					}
				}
			}
			catch
			{
				_transactionFailure = header.Connection.TransactionFailure;
				throw;
			}
        }
        
        // DeleteRow
        protected virtual void InternalVerifyDeleteStatement(TableVar table, IRow row, DeleteStatement statement) {}
        protected override void InternalDeleteRow(Program program, TableVar table, IRow row)
        {
			DeleteStatement statement = new DeleteStatement();
			statement.DeleteClause = new DeleteClause();
			statement.DeleteClause.TableExpression = new TableExpression();
			statement.DeleteClause.TableExpression.TableSchema = D4.MetaData.GetTag(table.MetaData, "Storage.Schema", Device.Schema);
			statement.DeleteClause.TableExpression.TableName = Device.ToSQLIdentifier(table);

			SQLConnectionHeader header = RequestConnection(false);
			try
			{			
				using (SQLCommand command = header.Connection.CreateCommand(false))
				{
					Schema.TableVarColumn column;
					string columnName;
					string parameterName;
					SQLScalarType scalarType;
					
					Schema.Key clusteringKey = program.FindClusteringKey(table);
					if (clusteringKey.Columns.Count > 0)
					{
						int rowIndex;
						statement.WhereClause = new WhereClause();
						for (int index = 0; index < clusteringKey.Columns.Count; index++)
						{
							column = clusteringKey.Columns[index];					
							rowIndex = row.DataType.Columns.IndexOfName(column.Name);
							columnName = Device.ToSQLIdentifier(column);
							parameterName = String.Format("P{0}", index.ToString());
							scalarType = (SQLScalarType)Device.ResolveDeviceScalarType(program.Plan, (Schema.ScalarType)column.DataType);
							command.Parameters.Add
							(
								new SQLParameter
								(
									parameterName, 
									scalarType.GetSQLParameterType(column), 
									row.HasValue(rowIndex) ? scalarType.ParameterFromScalar(program.ValueManager, row[rowIndex]) : null, 
									SQLDirection.In, 
									Device.GetParameterMarker(scalarType, column)
								)
							);
							Expression expression = 
								new BinaryExpression
								(
									new QualifiedFieldExpression(columnName),
									"iEqual",
									new QueryParameterExpression(parameterName)
								);
								
							if (statement.WhereClause.Expression == null)
								statement.WhereClause.Expression = expression;
							else
								statement.WhereClause.Expression = new BinaryExpression(statement.WhereClause.Expression, "iAnd", expression);
						}
					}

					InternalVerifyDeleteStatement(table, row, statement);
					if (Device.CommandTimeout >= 0)
						command.CommandTimeout = Device.CommandTimeout;
					command.Statement = Device.Emitter.Emit(statement);
					command.Execute();			
				}
			}
			catch
			{
				_transactionFailure = header.Connection.TransactionFailure;
				throw;
			}
        }

		// IStreamProvider
		private SQLStreamHeaders _streams = new SQLStreamHeaders();
		
		private SQLStreamHeader GetStreamHeader(StreamID streamID)
		{
			SQLStreamHeader streamHeader = _streams[streamID];
			if (streamHeader == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, streamID.ToString());
			return streamHeader;
		}
		
		protected void DestroyStreams()
		{
			foreach (StreamID streamID in _streams.Keys)
				_streams[streamID].Dispose();
			_streams.Clear();
		}
		
		public virtual void Create
		(
			StreamID streamID, 
			string columnName, 
			Schema.DeviceScalarType deviceScalarType, 
			string statement, 
			SQLParameters parameters, 
			SQLCursorType cursorType, 
			SQLIsolationLevel isolationLevel
		)
		{
			_streams.Add(new SQLStreamHeader(streamID, this, columnName, deviceScalarType, statement, parameters, cursorType, isolationLevel));
		}
		
		public virtual Stream Open(StreamID streamID)
		{
			try
			{
				return GetStreamHeader(streamID).GetSourceStream();
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
		}
		
		public virtual void Close(StreamID streamID)
		{
			// no action to perform
		}
		
		public virtual void Destroy(StreamID streamID)
		{
			SQLStreamHeader streamHeader = GetStreamHeader(streamID);
			_streams.Remove(streamID);
			streamHeader.Dispose();
		}
		
		public virtual void Reassign(StreamID oldStreamID, StreamID newStreamID)
		{
			SQLStreamHeader streamHeader = GetStreamHeader(oldStreamID);
			_streams.Remove(oldStreamID);
			streamHeader.StreamID = newStreamID;
			_streams.Add(streamHeader);
		}
		
		// SQLExecute
		public void SQLExecute(string statement, SQLParameters parameters)
		{
			SQLConnectionHeader header = RequestConnection(false);
			try
			{
				header.Connection.Execute(statement, parameters);
			}
			catch (Exception exception)
			{
				_transactionFailure = header.Connection.TransactionFailure;
				throw WrapException(exception);
			}
		}
	}
	
	public class SQLStreamHeader : Disposable
	{
		public SQLStreamHeader
		(
			StreamID streamID, 
			SQLDeviceSession deviceSession, 
			string columnName, 
			Schema.DeviceScalarType deviceScalarType, 
			string statement, 
			SQLParameters parameters, 
			SQLCursorType cursorType, 
			SQLIsolationLevel isolationLevel
		) : base()
		{
			_streamID = streamID;
			_deviceSession = deviceSession;
			_columnName = columnName;
			_deviceScalarType = deviceScalarType;
			_statement = statement;
			_parameters = parameters;
			_cursorType = cursorType;
			_isolationLevel = isolationLevel;
		}
		
		protected override void Dispose(bool disposing)
		{
			CloseSource();
			_deviceScalarType = null;
			_deviceSession = null;
			_parameters = null;
			_streamID = StreamID.Null;
			base.Dispose(disposing);
		}
		
		// The stream manager id for this stream
		private StreamID _streamID;
		public StreamID StreamID 
		{ 
			get { return _streamID; } 
			set { _streamID = value; }
		}
		
		// The device session supporting this stream
		private SQLDeviceSession _deviceSession;
		
		// The name of the column from which this stream originated
		private string _columnName;
		
		// The device scalar type map for the data type of this stream
		private Schema.DeviceScalarType _deviceScalarType;
		
		// The statement used to retrieve the data for the stream
		private string _statement;
		
		// The parameters used to retrieve the data for the stream
		private SQLParameters _parameters;
		
		// The cursor type used for the open request
		private SQLCursorType _cursorType;
		
		// The isolation level used for the open request
		private SQLIsolationLevel _isolationLevel;

		// A DeferredWriteStream built on the DeferredStream obtained from a cursor based on the expression
		//	<FTable.Node> where KeyColumns = KeyRowValues over { KeyColumns, DataColumn }
		private DeferredWriteStream _sourceStream;
		public Stream GetSourceStream()
		{
			OpenSource();
			return _sourceStream;
		}
		
		// If the underlying value cannot be opened deferred from the connectivity layer, it is stored in this non-native scalar
		private Scalar _sourceValue;

		// The Cursor used to access the deferred stream		
		private SQLCursor _cursor;
		
		public void OpenSource()
		{
			if (_sourceStream == null)
			{
				SQLConnectionHeader header = _deviceSession.RequestConnection(false);
				try
				{
					_cursor = header.Connection.Open(_statement, _parameters, _cursorType, _isolationLevel, SQLCommandBehavior.Default);
					try
					{
						if (_cursor.Next())
							if (_cursor.IsDeferred(_cursor.ColumnCount - 1))
								_sourceStream = new DeferredWriteStream(_deviceScalarType.GetStreamAdapter(_deviceSession.ServerProcess.ValueManager, _cursor.OpenDeferredStream(_cursor.ColumnCount - 1)));
							else
							{
								_sourceValue = new Scalar(_deviceSession.ServerProcess.ValueManager, _deviceScalarType.ScalarType);
								_sourceValue.AsNative = _deviceScalarType.ToScalar(_deviceSession.ServerProcess.ValueManager, _cursor[_cursor.ColumnCount - 1]);
								_sourceStream = new DeferredWriteStream(_sourceValue.OpenStream());
							}
						else
							throw new SQLException(SQLException.Codes.DeferredDataNotFound, _columnName);
					}
					finally
					{
						_cursor.Command.Connection.Close(_cursor);
						_cursor = null;
					}
				}
				catch
				{
					_deviceSession.TransactionFailure = header.Connection.TransactionFailure;
					throw;
				}
			}
		}
		
		public void CloseSource()
		{
			try
			{
				if (_sourceStream != null)
				{
					_sourceStream.Close();
					_sourceStream = null;
				}
			}
			finally
			{
				if (_sourceValue != null)
				{
					_sourceValue.Dispose();
					_sourceValue = null;
				}
			}
		}
	}
	
	public class SQLStreamHeaders : Hashtable
	{
		public SQLStreamHeaders() : base(){}
		
		public SQLStreamHeader this[StreamID streamID] { get { return (SQLStreamHeader)base[streamID]; } }
		
		public void Add(SQLStreamHeader stream)
		{
			Add(stream.StreamID, stream);
		}
		
		public void Remove(SQLStreamHeader stream)
		{
			Remove(stream.StreamID);
		}
	}
	
	public class ColumnMap
	{
		public ColumnMap(Column column, ColumnExpression expression, int index)
		{
			_column = column;
			_columnExpression = expression;
			_index = index;
		}
		
		// The Column instance in DataType.Columns
		private Column _column;
		public Column Column { get { return _column; } }
		
		// The index of FColumn in DataType.Columns
		private int _index;
		public int Index { get { return _index; } }
		
		// The column expression in the translated statement
		private ColumnExpression _columnExpression;
		public ColumnExpression ColumnExpression { get { return _columnExpression; } }
	}
	
	#if USETYPEDLIST
	public class ColumnMaps : TypedList
	{
		public ColumnMaps() : base(typeof(ColumnMap)){}
		
		public new ColumnMap this[int AIndex] 
		{ 
			get { return (ColumnMap)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}

	#else
	public class ColumnMaps : BaseList<ColumnMap>
	{
	#endif	
		// Returns the ColumnMap with the given ColumnIndex into DataType.Columns
		public ColumnMap ColumnMapByIndex(int index)
		{
			foreach (ColumnMap columnMap in this)
				if (columnMap.Index == index)
					return columnMap;
					
			throw new SQLException(SQLException.Codes.ColumnMapNotFound, index);
		}
	}
	
	public class SQLTableColumn
	{
		public SQLTableColumn(Schema.TableVarColumn column, Schema.DeviceScalarType scalarType) : base()
		{
			Column = column;
			ScalarType = scalarType;
			IsDeferred = Convert.ToBoolean(D4.MetaData.GetTag(Column.MetaData, "Storage.Deferred", "false"));
		}
		
		public Schema.TableVarColumn Column;
		public Schema.DeviceScalarType ScalarType;
		public bool IsDeferred;
	}
	
	#if USETYPEDLIST
	public class SQLTableColumns : TypedList
	{
		public SQLTableColumns() : base(typeof(SQLTableColumn)){}
		
		public new SQLTableColumn this[int AIndex]
		{
			get { return (SQLTableColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class SQLTableColumns : BaseList<SQLTableColumn> { }
	#endif
	
	public class SQLTable : Table
	{
		public SQLTable(SQLDeviceSession deviceSession, Program program, TableNode tableNode, SelectStatement statement, SQLParameters parameters, bool isAggregate) : base(tableNode, program)
		{
			_deviceSession = deviceSession;
			_parameters = parameters;
			_statement = new SelectStatement();
			_statement.Modifiers = statement.Modifiers;
			_isAggregate = isAggregate;
			_statement.QueryExpression = new QueryExpression();
			_statement.QueryExpression.SelectExpression = new SelectExpression();
			SelectClause selectClause = new SelectClause();
			SelectClause givenSelectClause = statement.QueryExpression.SelectExpression.SelectClause;
			selectClause.Distinct = givenSelectClause.Distinct;
			selectClause.NonProject = givenSelectClause.NonProject;
			selectClause.Columns.AddRange(givenSelectClause.Columns);
			_statement.QueryExpression.SelectExpression.SelectClause = selectClause;
			_statement.QueryExpression.SelectExpression.FromClause = statement.QueryExpression.SelectExpression.FromClause;
			_statement.QueryExpression.SelectExpression.WhereClause = statement.QueryExpression.SelectExpression.WhereClause;
			_statement.QueryExpression.SelectExpression.GroupClause = statement.QueryExpression.SelectExpression.GroupClause;
			_statement.QueryExpression.SelectExpression.HavingClause = statement.QueryExpression.SelectExpression.HavingClause;
			_statement.OrderClause = statement.OrderClause;
			
			foreach (TableVarColumn column in Node.TableVar.Columns)
			{
				SQLTableColumn sQLColumn = new SQLTableColumn(column, (DeviceScalarType)_deviceSession.Device.ResolveDeviceScalarType(program.Plan, (Schema.ScalarType)column.DataType));
				_sQLColumns.Add(sQLColumn);
				if (sQLColumn.IsDeferred)
					_hasDeferredData = true;
			}
		}
		
		protected override void Dispose(bool disposing)
		{
			Close();
			_deviceSession = null;
			base.Dispose(disposing);
		}
		
		protected SQLDeviceSession _deviceSession;
		public SQLDeviceSession DeviceSession { get { return _deviceSession; } }
		
		protected SQLDeviceCursor _cursor;
		protected SQLParameters _parameters;
		protected SelectStatement _statement;
		protected bool _isAggregate;
		protected ColumnMaps _mainColumns = new ColumnMaps();
		protected ColumnMaps _deferredColumns = new ColumnMaps();
		protected SQLTableColumns _sQLColumns = new SQLTableColumns();
		protected bool _hasDeferredData;
		protected bool _bOF;
		protected bool _eOF;
		
		public static SQLCursorType CursorTypeToSQLCursorType(DAE.CursorType cursorType)
		{
			switch (cursorType)
			{
				case DAE.CursorType.Static: return SQLCursorType.Static;
				default: return SQLCursorType.Dynamic;
			}
		}

		public static SQLLockType CursorCapabilitiesToSQLLockType(CursorCapability capabilities, IsolationLevel isolation)
		{
			return (((capabilities & CursorCapability.Updateable) != 0) && (isolation == IsolationLevel.Isolated)) ? SQLLockType.Pessimistic : SQLLockType.ReadOnly;
		}
		
		public static IsolationLevel CursorIsolationToIsolationLevel(CursorIsolation cursorIsolation, IsolationLevel isolation)
		{
			switch (cursorIsolation)
			{
				case CursorIsolation.Chaos:
				case CursorIsolation.Browse: return IsolationLevel.Browse;
				case CursorIsolation.CursorStability:
				case CursorIsolation.Isolated: return IsolationLevel.Isolated;
				default: return (isolation == IsolationLevel.Browse) ? IsolationLevel.Browse : IsolationLevel.Isolated;
			}
		}

		public static SQLIsolationLevel CursorIsolationToSQLIsolationLevel(CursorIsolation cursorIsolation, IsolationLevel isolation)
		{
			return SQLDeviceSession.IsolationLevelToSQLIsolationLevel(CursorIsolationToIsolationLevel(cursorIsolation, isolation));
		}
		
		private SQLDeviceCursor GetMainCursor()
		{
			SelectExpression selectExpression = _statement.QueryExpression.SelectExpression;

			if (_hasDeferredData)
			{
				for (int index = 0; index < DataType.Columns.Count; index++)
					if (_sQLColumns[index].IsDeferred)
					{
						ColumnMap columnMap = 
							new ColumnMap
							(
								DataType.Columns[index], 
								selectExpression.SelectClause.Columns[index], 
								index
							);
							
						selectExpression.SelectClause.Columns[index] = 
							new ColumnExpression
							(
								new CaseExpression
								(
									new CaseItemExpression[]
									{
										new CaseItemExpression
										(
											new UnaryExpression("iIsNull", columnMap.ColumnExpression.Expression),
											new ValueExpression(0)
										)
									},
									new CaseElseExpression(new ValueExpression(1))
								),
								columnMap.ColumnExpression.ColumnAlias
							);
							
						_deferredColumns.Add(columnMap);
					}
					else
						_mainColumns.Add(new ColumnMap(DataType.Columns[index], selectExpression.SelectClause.Columns[index], index));
			}
			
			SQLParameters parameters = new SQLParameters();
			parameters.AddRange(_parameters);
			
			SQLParameters keyParameters = new SQLParameters();
			int[] keyIndexes = new int[Node.Order == null ? 0 : Node.Order.Columns.Count];
			SQLScalarType[] keyTypes = new SQLScalarType[keyIndexes.Length];
			TableVarColumn keyColumn;
			for (int index = 0; index < keyIndexes.Length; index++)
			{
				keyColumn = Node.Order.Columns[index].Column;
				keyIndexes[index] = DataType.Columns.IndexOfName(keyColumn.Name);
				keyTypes[index] = (SQLScalarType)_sQLColumns[keyIndexes[index]].ScalarType;
				keyParameters.Add
				(
					new SQLParameter
					(
						selectExpression.SelectClause.Columns[keyIndexes[index]].ColumnAlias, 
						keyTypes[index].GetSQLParameterType(keyColumn),
						null,
						SQLDirection.In,
						DeviceSession.Device.GetParameterMarker(keyTypes[index], keyColumn)
					)
				);
			}
			
			SelectStatement statement = new SelectStatement();
			statement.Modifiers = _statement.Modifiers;
			statement.QueryExpression = new QueryExpression();
			statement.QueryExpression.SelectExpression = new SelectExpression();
			statement.QueryExpression.SelectExpression.SelectClause = _statement.QueryExpression.SelectExpression.SelectClause;
			statement.QueryExpression.SelectExpression.FromClause = _statement.QueryExpression.SelectExpression.FromClause;
			if (_statement.QueryExpression.SelectExpression.WhereClause != null)
				statement.QueryExpression.SelectExpression.WhereClause = new WhereClause(_statement.QueryExpression.SelectExpression.WhereClause.Expression);
			statement.QueryExpression.SelectExpression.GroupClause = _statement.QueryExpression.SelectExpression.GroupClause;
			statement.QueryExpression.SelectExpression.HavingClause = _statement.QueryExpression.SelectExpression.HavingClause;
			statement.OrderClause = _statement.OrderClause;

			return 
				new SQLDeviceCursor
				(
					_deviceSession, 
					statement, 
					_isAggregate, 
					parameters, 
					keyIndexes, 
					keyTypes, 
					keyParameters, 
					CursorCapabilitiesToSQLLockType(Capabilities, CursorIsolationToIsolationLevel(Isolation, DeviceSession.ServerProcess.CurrentIsolationLevel())), 
					CursorTypeToSQLCursorType(Node.RequestedCursorType), 
					CursorIsolationToSQLIsolationLevel(Isolation, DeviceSession.ServerProcess.CurrentIsolationLevel())
				);
		}
		
		// Returns a SQLCommand for accessing the deferred read data for the given column index.  
		// The deferred column is guaranteed to be the last column in the Cursor opened from the command.
		private void GetDeferredStatement(IRow key, int columnIndex, out string statement, out SQLParameters parameters)
		{
			parameters = new SQLParameters();
			parameters.AddRange(_parameters);

			SelectStatement localStatement = new SelectStatement();
			localStatement.Modifiers = _statement.Modifiers;
			localStatement.QueryExpression = new QueryExpression();
			localStatement.QueryExpression.SelectExpression = new SelectExpression();
			localStatement.QueryExpression.SelectExpression.SelectClause = new SelectClause();
			Expression keyCondition = null;
			for (int index = 0; index < key.DataType.Columns.Count; index++)
			{
				ColumnMap columnMap = _mainColumns.ColumnMapByIndex(DataType.Columns.IndexOfName(key.DataType.Columns[index].Name));
				SQLScalarType scalarType = (SQLScalarType)_sQLColumns[columnMap.Index].ScalarType;
				TableVarColumn tableVarColumn = Node.TableVar.Columns[key.DataType.Columns[index].Name];
				localStatement.QueryExpression.SelectExpression.SelectClause.Columns.Add(columnMap.ColumnExpression);
				SQLParameter parameter = 
					new SQLParameter
					(
						columnMap.ColumnExpression.ColumnAlias, 
						scalarType.GetSQLParameterType(tableVarColumn), 
						key.HasValue(index) ? 
							scalarType.ParameterFromScalar(Manager, key[index]) : 
							null,
						SQLDirection.In,
						DeviceSession.Device.GetParameterMarker(scalarType, tableVarColumn),
						DeviceSession.Device.UseParametersForCursors ? null : scalarType.ToLiteral(Manager, key[index])
					);
				parameters.Add(parameter);
				Expression condition =	
					new BinaryExpression
					(
						columnMap.ColumnExpression.Expression,
						"iEqual",
						new QueryParameterExpression(parameter.Name)
					);
					
				if (keyCondition != null)
					keyCondition = new BinaryExpression(keyCondition, "iAnd", condition);
				else
					keyCondition = condition;
			}
			
			localStatement.QueryExpression.SelectExpression.SelectClause.Columns.Add(_deferredColumns.ColumnMapByIndex(columnIndex).ColumnExpression);
			localStatement.QueryExpression.SelectExpression.FromClause = _statement.QueryExpression.SelectExpression.FromClause;
			if ((keyCondition != null) || (_statement.QueryExpression.SelectExpression.WhereClause != null))
			{
				localStatement.QueryExpression.SelectExpression.WhereClause = new WhereClause();
				if ((keyCondition != null) && (_statement.QueryExpression.SelectExpression.WhereClause != null))
					localStatement.QueryExpression.SelectExpression.WhereClause.Expression = new BinaryExpression(_statement.QueryExpression.SelectExpression.WhereClause.Expression, "iAnd", keyCondition);
				else if (keyCondition != null)
					localStatement.QueryExpression.SelectExpression.WhereClause.Expression = keyCondition;
				else
					localStatement.QueryExpression.SelectExpression.WhereClause.Expression = _statement.QueryExpression.SelectExpression.WhereClause.Expression;
			}
				
			localStatement.QueryExpression.SelectExpression.GroupClause = _statement.QueryExpression.SelectExpression.GroupClause;
			localStatement.QueryExpression.SelectExpression.HavingClause = _statement.QueryExpression.SelectExpression.HavingClause;
			
			statement = _deviceSession.Device.Emitter.Emit(localStatement);
		}
		
		protected override void InternalOpen()
		{
			if (!_deviceSession.ServerProcess.IsOpeningInsertCursor)
			{
				_cursor = GetMainCursor();
				_bOF = true;
				_eOF = !_cursor.Next();
			}
			else
			{
				_bOF = true;
				_eOF = true;
			}
		}
		
		protected override void InternalClose()
		{
			if (_cursor != null)
			{
				_cursor.Dispose();
				_cursor = null;
			}
		}
		
		protected void InternalSelect(IRow row, bool allowDeferred)
		{
			for (int index = 0; index < row.DataType.Columns.Count; index++)
			{
				int columnIndex = DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
				if (columnIndex >= 0)
				{
					if (_sQLColumns[columnIndex].IsDeferred)
					{
						if (!allowDeferred)
							throw new SQLException(SQLException.Codes.InvalidDeferredContext);
							
						if ((int)_cursor[columnIndex] == 0)
							row.ClearValue(index);
						else
						{
							// set up a deferred read stream using the device session as the provider
							StreamID streamID = Program.ServerProcess.Register(_deviceSession);
							string statement;
							SQLParameters parameters;
							GetDeferredStatement(InternalGetKey(), columnIndex, out statement, out parameters);
							_deviceSession.Create
							(
								streamID, 
								DataType.Columns[columnIndex].Name, 
								_sQLColumns[columnIndex].ScalarType, 
								statement,
								parameters,
								CursorTypeToSQLCursorType(Node.RequestedCursorType), 
								CursorIsolationToSQLIsolationLevel(Isolation, DeviceSession.ServerProcess.CurrentIsolationLevel())
							);
							row[index] = streamID;
						}
					}
					else
					{
						if (_cursor.IsNull(columnIndex))
							row.ClearValue(index);
						else
							row[index] = _sQLColumns[columnIndex].ScalarType.ToScalar(Manager, _cursor[columnIndex]);
					}
				}
			}
		}
		
		protected override void InternalSelect(IRow row)
		{
			InternalSelect(row, true);
		}
		
		protected override IRow InternalGetKey()
		{
			Row row = new Row(Manager, new RowType(Program.FindClusteringKey(Node.TableVar).Columns));
			InternalSelect(row, false);
			return row;
		}
		
		protected override bool InternalNext()
		{
			if (_bOF)
			{
				_bOF = _eOF;
			}
			else
			{
				_eOF = !_cursor.Next();
				_bOF = _bOF && _eOF;
			}
			return !_eOF;
		}
		
		protected override bool InternalBOF()
		{
			return _bOF;
		}
		
		protected override bool InternalEOF()
		{
			return _eOF;
		}
	}

    public static class SQLDeviceUtility
    {
		public static SQLDevice ResolveSQLDevice(Plan plan, string deviceName)
		{
			Device device = Compiler.ResolveCatalogIdentifier(plan, deviceName, true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);

			SQLDevice sQLDevice = device as SQLDevice;
			if (sQLDevice == null)
				throw new SQLException(SQLException.Codes.SQLDeviceExpected);

			return sQLDevice;
		}

		public static ReconcileOptions ResolveReconcileOptions(ListValue list)
		{
			StringBuilder localList = new StringBuilder();
			for (int index = 0; index < list.Count(); index++)
			{
				if (index > 0)
					localList.Append(", ");
				localList.Append((string)list[index]);
			}
			
			return (ReconcileOptions)Enum.Parse(typeof(ReconcileOptions), localList.ToString());
		}
	}
    
	// operator D4ToSQL(AQuery : System.String) : System.String;
    // operator D4ToSQL(ADeviceName : System.Name, AQuery : System.String) : System.String;
    public class D4ToSQLNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string statementString;
			Schema.Device device = null;
			SQLDevice sQLDevice = null;
			if (arguments.Length == 1)
				statementString = (string)arguments[0];
			else
			{
				sQLDevice = SQLDeviceUtility.ResolveSQLDevice(program.Plan, (string)arguments[0]);
				device = sQLDevice;
				statementString = (string)arguments[1];
			}

			string sQLQuery = String.Empty;
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				ParserMessages parserMessages = new ParserMessages();
				Statement statement = new D4.Parser().ParseStatement(statementString, parserMessages);
				plan.Messages.AddRange(parserMessages);
				PlanNode node = Compiler.Compile(plan, statement);
				if (plan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, plan.Messages.ToString(CompilerErrorLevel.NonFatal));

				if (node is FrameNode)
					node = node.Nodes[0];
				if ((node is ExpressionStatementNode) || (node is CursorNode))
					node = node.Nodes[0];
					
				if (device == null)
					device = node.Device;
					
				if ((device != null) && node.DeviceSupported)
				{
					if (sQLDevice == null)
						sQLDevice = device as SQLDevice;
						
					if (sQLDevice == null)
						throw new SQLException(SQLException.Codes.QuerySupportedByNonSQLDevice, device.Name);

					if (node.Device == sQLDevice)
						sQLQuery = sQLDevice.Emitter.Emit(((SQLDevicePlanNode)node.DeviceNode).Statement);
					else
						throw new SQLException(SQLException.Codes.QuerySupportedByDifferentDevice, node.Device.Name, sQLDevice.Name);
				}
				else
					throw new SQLException(SQLException.Codes.QueryUnsupported);
			}
			finally
			{
				plan.Dispose();
			}

			return sQLQuery;
		}
    }

	// operator SQLExecute(const AStatement : String);
	// operator SQLExecute(const AStatement : String, const AInValues : row);
	// operator SQLExecute(const AStatement : String, const AInValues : row, var AOutValues : row);
	// operator SQLExecute(const ADeviceName : Name, const AStatement : String);
	// operator SQLExecute(const ADeviceName : Name, const AStatement : String, const AInValues : row);
	// operator SQLExecute(const ADeviceName : Name, const AStatement : String, const AInValues : row, var AOutValues : row);    
    public class SQLExecuteNode : InstructionNode
    {
		private static bool IsValidIdentifierCharacter(char charValue)
		{
			return (charValue == '_') || Char.IsLetterOrDigit(charValue);
		}

		private static bool HandleParameter(Plan plan, SQLDevice device, SQLParameters paramsValue, PlanNode[] conversionNodes, Schema.IRowType inRowType, Schema.IRowType outRowType, string parameterName)
		{
			int inIndex = inRowType != null ? inRowType.Columns.IndexOf(parameterName) : -1;
			int outIndex = outRowType != null ? outRowType.Columns.IndexOf(parameterName) : -1;
			if ((inIndex >= 0) || (outIndex >= 0))
			{
				Schema.ScalarType valueType;
				
				if (outIndex >= 0)
					valueType = (Schema.ScalarType)outRowType.Columns[outIndex].DataType;
				else
					valueType = (Schema.ScalarType)inRowType.Columns[inIndex].DataType;

				if (inIndex >= 0)
				{
					//if (AInValues.HasValue(LInIndex))
					//	LValue = (IScalar)AInValues[LInIndex];

					if (!inRowType.Columns[inIndex].DataType.Equals(valueType))
					{
						ValueNode valueNode = new DAE.Runtime.Instructions.ValueNode();
						//LValueNode.Value = LValue;
						valueNode.DataType = valueType;
						PlanNode sourceNode = valueNode;

						if (!inRowType.Columns[inIndex].DataType.Is(valueType))
						{
							ConversionContext context = Compiler.FindConversionPath(plan, inRowType.Columns[inIndex].DataType, valueType);
							Compiler.CheckConversionContext(plan, context);
							sourceNode = Compiler.ConvertNode(plan, sourceNode, context);
						}
						
						sourceNode = Compiler.Upcast(plan, sourceNode, valueType);
						conversionNodes[inIndex] = sourceNode;
							
						//LValue = (IScalar)Compiler.Upcast(AProcess.Plan, LSourceNode, LValueType).Execute(AProcess).Value;
					}
				}
				
				SQLScalarType scalarType = device.ResolveDeviceScalarType(plan, valueType) as SQLScalarType;
				if (scalarType == null)
					throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, valueType.Name);

				paramsValue.Add(new SQLParameter(parameterName, scalarType.GetSQLParameterType(), null, ((inIndex >= 0) && (outIndex >= 0)) ? SQLDirection.InOut : inIndex >= 0 ? SQLDirection.In : SQLDirection.Out, device.GetParameterMarker(scalarType)));
				return true;  // return true if the parameter was handled
			}
			return false; // return false since the parameter was not handled
		}
		
		public static SQLParameters PrepareParameters(Plan plan, SQLDevice device, string statement, Schema.IRowType inRowType, Schema.IRowType outRowType, PlanNode[] conversionNodes)
		{
			SQLParameters parameters = new SQLParameters();
			StringBuilder parameterName = null;
			bool inParameter = false;
			char quoteChar = '\0';
			for (int index = 0; index < statement.Length; index++)
			{
				if (inParameter && !IsValidIdentifierCharacter(statement[index]))
				{
					if (HandleParameter(plan, device, parameters, conversionNodes, inRowType, outRowType, parameterName.ToString()))
						inParameter = false;
				}
					
				switch (statement[index])
				{
					case '@' :
						if (quoteChar == '\0') // if not inside of a string
						{
							parameterName = new StringBuilder();
							inParameter = true;
						}
					break;
					
					case '\'' :
					case '"' :
						if (quoteChar != '\0')
						{
							if (((index + 1) >= statement.Length) || (statement[index + 1] != quoteChar))
								quoteChar = '\0';
						}
						else
							quoteChar = statement[index];
					break;
					default:
						if (inParameter)
							parameterName.Append(statement[index]);
					break;
				}
			}
			
			// handle the param if it's the last thing on the statement
			if (inParameter)
				HandleParameter(plan, device, parameters, conversionNodes, inRowType, outRowType, parameterName.ToString());

			return parameters;
		}
		
		private static void SetValueNode(PlanNode planNode, IDataValue tempValue)
		{
			if (planNode is ValueNode)
				((ValueNode)planNode).Value = tempValue.IsNil ? null : tempValue.AsNative;
			else
				SetValueNode(planNode.Nodes[0], tempValue);
		}
		
		public static void GetParameters(Program program, SQLDevice device, SQLParameters parameters, IRow inValues, PlanNode[] conversionNodes)
		{
			for (int index = 0; index < parameters.Count; index++)
			{
				switch (parameters[index].Direction)
				{
					case SQLDirection.InOut :
					case SQLDirection.In :
						int inIndex = inValues.DataType.Columns.IndexOf(parameters[index].Name);
						PlanNode conversionNode = conversionNodes[inIndex];
						Schema.ScalarType valueType = (Schema.ScalarType)(conversionNode == null ? inValues.DataType.Columns[inIndex].DataType : conversionNode.DataType);
						SQLScalarType scalarType = device.ResolveDeviceScalarType(program.Plan, valueType) as SQLScalarType;
						if (scalarType == null)
							throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, valueType.Name);

						if (inValues.HasValue(inIndex))
                        {
							if (conversionNode == null)
								parameters[index].Value = scalarType.ParameterFromScalar(program.ValueManager, inValues[inIndex]);
							else
							{
								SetValueNode(conversionNode, inValues.GetValue(inIndex));
								parameters[index].Value = scalarType.ParameterFromScalar(program.ValueManager, conversionNode.Execute(program));
							}
                        }
                        else
                            parameters[index].Value = null;
					break;
				}
			}
		}
		
		public static void SetParameters(Program program, SQLDevice device, SQLParameters parameters, IRow outValues)
		{
			for (int index = 0; index < parameters.Count; index++)
				switch (parameters[index].Direction)
				{
					case SQLDirection.InOut :
					case SQLDirection.Out :
						int outIndex = outValues.DataType.Columns.IndexOf(parameters[index].Name);
						Schema.ScalarType valueType = (Schema.ScalarType)outValues.DataType.Columns[outIndex].DataType;
						SQLScalarType scalarType = device.ResolveDeviceScalarType(program.Plan, valueType) as SQLScalarType;
						if (scalarType == null)
							throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, valueType.Name);

						if (parameters[index].Value != null)
							outValues[outIndex] = scalarType.ParameterToScalar(program.ValueManager, parameters[index].Value);
						else
							outValues.ClearValue(outIndex);
					break;
				}
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			long startTicks = TimingUtility.CurrentTicks;
			try
			{
				string deviceName = String.Empty;
				string statement = String.Empty;
				IRow inValues = null;
				IRow outValues = null;
				
				if (Operator.Operands[0].DataType.Is(Compiler.ResolveCatalogIdentifier(program.Plan, "System.Name") as IDataType))
				{
					deviceName = (string)arguments[0];
					statement = (string)arguments[1];
					if (arguments.Length >= 3)
						inValues = (IRow)arguments[2];
					if (arguments.Length == 4)
						outValues = (IRow)arguments[3];
				}
				else
				{
					deviceName = program.Plan.DefaultDeviceName;
					statement = (string)arguments[0];
					if (arguments.Length >= 2)
						inValues = (IRow)arguments[1];
					if (arguments.Length == 3)
						outValues = (IRow)arguments[2];
				}

				SQLDevice sQLDevice = SQLDeviceUtility.ResolveSQLDevice(program.Plan, deviceName);
				SQLDeviceSession deviceSession = program.DeviceConnect(sQLDevice) as SQLDeviceSession;				
				PlanNode[] conversionNodes = inValues == null ? new PlanNode[0] : new PlanNode[inValues.DataType.Columns.Count];
				SQLParameters parameters = PrepareParameters(program.Plan, sQLDevice, statement, inValues == null ? null : inValues.DataType, outValues == null ? null : outValues.DataType, conversionNodes);
				GetParameters(program, sQLDevice, parameters, inValues, conversionNodes);
				deviceSession.SQLExecute(statement, parameters);
				SetParameters(program, sQLDevice, parameters, outValues);
				return null;
			}
			finally
			{
				program.Statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
		}
    }

	// ADeviceName, AStatement, AKeyInfo, and ATableType must be literals (evaluable at compile-time)
	// operator SQLQuery(const AStatement : String) : table
	// operator SQLQuery(const AStatement : String, const AKeyInfo : String) : table
	// operator SQLQuery(const AStatement : String, const AInValues : row) : table
	// operator SQLQuery(const AStatement : String, const AInValues : row, const AKeyInfo : String) : table
	// operator SQLQuery(const AStatement : String, const AInValues : row, var AOutValues : row) : table
	// operator SQLQuery(const AStatement : String, const AInValues : row, var AOutValues : row, const AKeyInfo : String) : table
	// operator SQLQuery(const AStatement : String, const AInValues : row, var AOutValues : row, const ATableType : String, const AKeyInfo : String) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AKeyInfo : String) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : row) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : row, const AKeyInfo : String) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : row, var AOutValues : row) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : row, var AOutValues : row, const AKeyInfo : String) : table
	// operator SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : row, var AOutValues : row, const ATableType : String, const AKeyInfo : String) : table
    public class SQLQueryNode : TableNode
    {
		public SQLQueryNode()
		{
			ShouldSupport = false;
		}

		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			
			// determine the table type from the CLI call
			string deviceName = String.Empty;
			_statement = String.Empty;
			Schema.RowType inRowType = null;
			Schema.RowType outRowType = null;
			string tableType = String.Empty;
			string keyDefinition = String.Empty;
			
			if (plan.IsEngine && (Modifiers != null))
			{
				tableType = LanguageModifiers.GetModifier(Modifiers, "TableType", tableType);
				keyDefinition = LanguageModifiers.GetModifier(Modifiers, "KeyInfo", keyDefinition);
			}

			// ADeviceName and AStatement must be literal
			if (Nodes[0].DataType.Is(plan.DataTypes.SystemName))
			{
				// NOTE: We are deliberately not using APlan.ExecuteLiteralArgument here because we want to throw the SQLException, not the CompilerException.
				if (!Nodes[0].IsLiteral)
					throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "ADeviceName");
				deviceName = (string)plan.ExecuteNode(Nodes[0]);
					
				if (!Nodes[1].IsLiteral)
					throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AStatement");
				_statement = (string)plan.ExecuteNode(Nodes[1]);
				
				if (Nodes.Count >= 3)
				{
					if (Nodes[2].DataType.Is(plan.DataTypes.SystemString))
					{
						if (!Nodes[2].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						keyDefinition = (string)plan.ExecuteNode(Nodes[2]);
					}
					else
						inRowType = Nodes[2].DataType as Schema.RowType;
				}	
					
				if (Nodes.Count >= 4)
				{
					if (Nodes[3].DataType.Is(plan.DataTypes.SystemString))
					{
						if (!Nodes[3].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						keyDefinition = (string)plan.ExecuteNode(Nodes[3]);
					}
					else
						outRowType = Nodes[3].DataType as Schema.RowType;
				}
				
				if (Nodes.Count == 5)
				{
					if (!Nodes[4].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					keyDefinition = (string)plan.ExecuteNode(Nodes[4]);
				}
				else if (Nodes.Count == 6)
				{
					if (!Nodes[4].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "ATableType");
					tableType = (string)plan.ExecuteNode(Nodes[4]);
					
					if (!Nodes[5].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					keyDefinition = (string)plan.ExecuteNode(Nodes[5]);
				}
			}
			else
			{
				deviceName = plan.DefaultDeviceName;
				if (!Nodes[0].IsLiteral)
					throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AStatement");
				_statement = (string)plan.ExecuteNode(Nodes[0]);
				
				if (Nodes.Count >= 2)
				{
					if (Nodes[1].DataType.Is(plan.DataTypes.SystemString))
					{
						if (!Nodes[1].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						keyDefinition = (string)plan.ExecuteNode(Nodes[1]);
					}
					else
						inRowType = Nodes[1].DataType as Schema.RowType;
				}	
					
				if (Nodes.Count >= 3)
				{
					if (Nodes[2].DataType.Is(plan.DataTypes.SystemString))
					{
						if (!Nodes[2].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						keyDefinition = (string)plan.ExecuteNode(Nodes[2]);
					}
					else
						outRowType = Nodes[2].DataType as Schema.RowType;
				}
				
				if (Nodes.Count == 4)
				{
					if (!Nodes[3].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					keyDefinition = (string)plan.ExecuteNode(Nodes[3]);
				}
				else if (Nodes.Count == 5)
				{
					if (!Nodes[3].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "ATableType");
					tableType = (string)plan.ExecuteNode(Nodes[3]);
					
					if (!Nodes[4].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					keyDefinition = (string)plan.ExecuteNode(Nodes[4]);
				}
			}
			
			CursorCapabilities = plan.CursorContext.CursorCapabilities;
			CursorIsolation = plan.CursorContext.CursorIsolation;
			CursorType = plan.CursorContext.CursorType;

			SQLDeviceSession deviceSession = null;
			if (!plan.IsEngine)
			{			
				_sQLDevice = SQLDeviceUtility.ResolveSQLDevice(plan, deviceName);
				deviceSession = plan.DeviceConnect(_sQLDevice) as SQLDeviceSession;
				_conversionNodes = inRowType == null ? new PlanNode[0] : new PlanNode[inRowType.Columns.Count];
				_planParameters = SQLExecuteNode.PrepareParameters(plan, _sQLDevice, _statement, inRowType, outRowType, _conversionNodes);
			}
			
			if (tableType == String.Empty)
			{
				SQLCursor cursor = 
					deviceSession.Connection.Open
					(
						_statement, 
						_planParameters, 
						SQLTable.CursorTypeToSQLCursorType(CursorType), 
						SQLIsolationLevel.ReadUncommitted,
						SQLCommandBehavior.SchemaOnly | (keyDefinition == String.Empty ? SQLCommandBehavior.KeyInfo : SQLCommandBehavior.Default)
					);
				try
				{
					bool messageReported = false;
					SQLTableSchema schema = null;
					try
					{
						schema = cursor.Schema;
					}
					catch (Exception exception)
					{
						// An error here should be ignored, just get as much schema information as possible from the actual cursor...
						// Not as efficient, but the warning lets them know that.
						plan.Messages.Add(new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.Warning, exception, "Compile-time schema retrieval failed, attempting run-time schema retrieval."));
						messageReported = true;
					}
					
					if ((schema == null) || (schema.Columns.Count == 0))
					{
						if (!messageReported)
							plan.Messages.Add(new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.Warning, "Compile-time schema retrieval failed, attempting run-time schema retrieval."));

						if (cursor != null)
						{
							cursor.Dispose();
							cursor = null;
						}
						
						cursor = 
							deviceSession.Connection.Open
							(
								_statement,
								_planParameters,
								SQLTable.CursorTypeToSQLCursorType(CursorType),
								SQLIsolationLevel.ReadUncommitted,
								SQLCommandBehavior.Default
							);
							
						schema = cursor.Schema;
					}
					
					_dataType = new Schema.TableType();
					_tableVar = new Schema.ResultTableVar(this);
					_tableVar.Owner = plan.User;
					_sQLColumns = new SQLTableColumns();
					foreach (SQLColumn sQLColumn in schema.Columns)
					{
						Schema.Column column = new Schema.Column(sQLColumn.Name, _sQLDevice.FindScalarType(plan, sQLColumn.Domain));
						DataType.Columns.Add(column);
						_tableVar.Columns.Add(new Schema.TableVarColumn(column, Schema.TableVarColumnType.Stored));
					}
					
					_sQLDevice.CheckSupported(plan, _tableVar);
					foreach (Schema.TableVarColumn tableVarColumn in _tableVar.Columns)
						_sQLColumns.Add(new SQLTableColumn(tableVarColumn, (Schema.DeviceScalarType)_sQLDevice.ResolveDeviceScalarType(plan, (Schema.ScalarType)tableVarColumn.Column.DataType)));
						
					if (keyDefinition == String.Empty)
					{
						foreach (SQLIndex sQLIndex in schema.Indexes)
						{
							if (sQLIndex.IsUnique)
							{
								Schema.Key key = new Schema.Key();
								foreach (SQLIndexColumn sQLIndexColumn in sQLIndex.Columns)
									key.Columns.Add(_tableVar.Columns[sQLIndexColumn.Name]);
									
								_tableVar.Keys.Add(key);
							}
						}
					}
					else
					{
						_tableVar.Keys.Add(Compiler.CompileKeyDefinition(plan, _tableVar, new D4.Parser().ParseKeyDefinition(keyDefinition)));
					}
				}
				finally
				{
					cursor.Dispose();
				}
			}
			else
			{
				_dataType = Compiler.CompileTypeSpecifier(plan, new D4.Parser().ParseTypeSpecifier(tableType)) as Schema.TableType;
				if (_dataType == null)
					throw new CompilerException(CompilerException.Codes.TableTypeExpected);
				_tableVar = new Schema.ResultTableVar(this);
				_tableVar.Owner = plan.User;
				_sQLColumns = new SQLTableColumns();

				foreach (Schema.Column column in DataType.Columns)
					TableVar.Columns.Add(new Schema.TableVarColumn(column));

				if (!plan.IsEngine)
				{				
					_sQLDevice.CheckSupported(plan, _tableVar);
					foreach (Schema.TableVarColumn tableVarColumn in _tableVar.Columns)
						_sQLColumns.Add(new SQLTableColumn(tableVarColumn, (Schema.DeviceScalarType)_sQLDevice.ResolveDeviceScalarType(plan, (Schema.ScalarType)tableVarColumn.Column.DataType)));
				}
					
				_tableVar.Keys.Add(Compiler.CompileKeyDefinition(plan, _tableVar, new D4.Parser().ParseKeyDefinition(keyDefinition)));
			}
			
			Compiler.EnsureKey(plan, _tableVar);
			
			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
				
			if (!plan.IsEngine)
			{
				if (Modifiers == null)
					Modifiers = new LanguageModifiers();
				D4.D4TextEmitter emitter = new D4.D4TextEmitter();
				Modifiers.AddOrUpdate("TableType", emitter.Emit(_tableVar.DataType.EmitSpecifier(D4.EmitMode.ForCopy)));
				Modifiers.AddOrUpdate("KeyInfo", emitter.Emit(Compiler.FindClusteringKey(plan, _tableVar).EmitStatement(D4.EmitMode.ForCopy)));
			}
		}

		private SQLDevice _sQLDevice;
		private string _statement;		
		private PlanNode[] _conversionNodes;
		private SQLParameters _planParameters;
		private SQLTableColumns _sQLColumns;

		public override object InternalExecute(Program program)
		{
			var parameters = new SQLParameters();
			foreach (var parameter in _planParameters)
			{
				parameters.Add(new SQLParameter(parameter.Name, parameter.Type, parameter.Value, parameter.Direction, parameter.Marker, parameter.Literal));
			}

			long startTicks = TimingUtility.CurrentTicks;
			try
			{
				IRow inValues = null;
				IRow outValues = null;
				
				if (Nodes[0].DataType.Is(Compiler.ResolveCatalogIdentifier(program.Plan, "System.Name", true) as IDataType))
				{
					if ((Nodes.Count >= 3) && (Nodes[2].DataType is Schema.IRowType))
						inValues = (IRow)Nodes[2].Execute(program);
					
					if ((Nodes.Count >= 4) && (Nodes[3].DataType is Schema.IRowType))
						outValues = (IRow)Nodes[3].Execute(program);
				}
				else
				{
					if ((Nodes.Count >= 2) && (Nodes[1].DataType is Schema.IRowType))
						inValues = (IRow)Nodes[1].Execute(program);
						
					if ((Nodes.Count == 3) && (Nodes[2].DataType is Schema.IRowType))
						outValues = (IRow)Nodes[2].Execute(program);
				}

				SQLDeviceSession deviceSession = program.DeviceConnect(_sQLDevice) as SQLDeviceSession;				
				SQLExecuteNode.GetParameters(program, _sQLDevice, parameters, inValues, _conversionNodes);
				
				LocalTable result = new LocalTable(this, program);
				try
				{
					result.Open();

					// Populate the result
					Row row = new Row(program.ValueManager, result.DataType.RowType);
					try
					{
						row.ValuesOwned = false;
						using 
						(
							SQLCursor cursor = 
								deviceSession.Connection.Open
								(
									_statement, 
									parameters, 
									SQLTable.CursorTypeToSQLCursorType(CursorType), 
									SQLTable.CursorIsolationToSQLIsolationLevel(CursorIsolation, program.ServerProcess.CurrentIsolationLevel()), 
									SQLCommandBehavior.Default
								)
						)
						{
							SQLExecuteNode.SetParameters(program, _sQLDevice, parameters, outValues);
							
							while (cursor.Next())
							{
								for (int index = 0; index < DataType.Columns.Count; index++)
								{
									if (cursor.IsNull(index))
										row.ClearValue(index);
									else
										row[index] = _sQLColumns[index].ScalarType.ToScalar(program.ValueManager, cursor[index]);
								}
								
								result.Insert(row);
							}
						}
					}
					finally
					{
						row.Dispose();
					}
					
					result.First();
					
					return result;
				}
				catch
				{
					result.Dispose();
					throw;
				}
			}
			finally
			{
				program.Statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
		}
    }
    
    // operator AvailableTables() : table { Name : Name, StorageName : String };
    // operator AvailableTables(const ADeviceName : System.Name) : table { Name : Name, StorageName : String };
    public class AvailableTablesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("StorageName", plan.DataTypes.SystemString));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateAvailableTables(Program program, SQLDevice device, Table table, Row row)
		{
			Schema.Catalog serverCatalog = device.GetServerCatalog(program.ServerProcess, null);
			Schema.Catalog catalog = new Schema.Catalog();
			device.GetDeviceTables(program.Plan, serverCatalog, catalog, null);
			foreach (Schema.Object objectValue in catalog)
			{
				Schema.BaseTableVar tableVar = objectValue as Schema.BaseTableVar;
				if (tableVar != null)
				{
					row[0] = tableVar.Name;
					row[1] = tableVar.MetaData.Tags["Storage.Name"].Value;
					table.Insert(row);
				}
			}
		}
		
		public override object InternalExecute(Program program)
		{
			string deviceName = (Nodes.Count == 0) ? program.Plan.DefaultDeviceName : (string)Nodes[0].Execute(program);
			SQLDevice sQLDevice = SQLDeviceUtility.ResolveSQLDevice(program.Plan, deviceName);
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					PopulateAvailableTables(program, sQLDevice, result, row);
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }
    
    // operator AvailableReferences() : table { Name : Name, StorageName : String };
    // operator AvailableReferences(const ADeviceName : System.Name) : table { Name : Name, StorageName : String };
    public class AvailableReferencesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("StorageName", plan.DataTypes.SystemString));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateAvailableReferences(Program program, SQLDevice device, Table table, Row row)
		{
			Schema.Catalog serverCatalog = device.GetServerCatalog(program.ServerProcess, null);
			Schema.Catalog catalog = new Schema.Catalog();
			device.GetDeviceForeignKeys(program.Plan, serverCatalog, catalog, null);
			foreach (Schema.Object objectValue in catalog)
			{
				Schema.Reference reference = objectValue as Schema.Reference;
				if (reference != null)
				{
					row[0] = reference.Name;
					row[1] = reference.MetaData.Tags["Storage.Name"].Value;
					table.Insert(row);
				}
			}
		}
		
		public override object InternalExecute(Program program)
		{
			string deviceName = (Nodes.Count == 0) ? program.Plan.DefaultDeviceName : (string)Nodes[0].Execute(program);
			SQLDevice sQLDevice = SQLDeviceUtility.ResolveSQLDevice(program.Plan, deviceName);
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					PopulateAvailableReferences(program, sQLDevice, result, row);
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
    }
    
    // operator DeviceReconciliationScript(const ADeviceName : Name) : String;
    // operator DeviceReconciliationScript(const ADeviceName : Name, const AOptions : list(String)) : String;
    // operator DeviceReconciliationScript(const ADeviceName : Name, const ATableName : Name, const AOptions : list(String)) : String;
    public class DeviceReconciliationScriptNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			SQLDevice sQLDevice = SQLDeviceUtility.ResolveSQLDevice(program.Plan, (string)arguments[0]);
			string tableName = String.Empty;
			ReconcileOptions options = ReconcileOptions.All;
			switch (arguments.Length)
			{
				case 2 :
					if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemName))
						tableName = (string)arguments[1];
					else
						options = SQLDeviceUtility.ResolveReconcileOptions(arguments[1] as ListValue);
				break;
				
				case 3 :
					tableName = (string)arguments[1];
					options = SQLDeviceUtility.ResolveReconcileOptions(arguments[2] as ListValue);
				break;
			}
			
			Batch batch;
			if (tableName == String.Empty)
				batch = sQLDevice.DeviceReconciliationScript(program.ServerProcess, options);
			else
				batch = sQLDevice.DeviceReconciliationScript(program.ServerProcess, Compiler.ResolveCatalogIdentifier(program.Plan, tableName, true) as Schema.TableVar, options);
				
			return sQLDevice.Emitter.Emit(batch);
		}
    }
}

