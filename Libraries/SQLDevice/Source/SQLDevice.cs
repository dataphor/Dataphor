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
	
	[Flags]
	public enum ReconcileOptions 
	{ 
		None = 0,
		ShouldReconcileColumns = 1, 
		ShouldDropTables = 2,
		ShouldDropColumns = 4, 
		ShouldDropKeys = 8, 
		ShouldDropOrders = 16, 
		All = ShouldReconcileColumns | ShouldDropTables | ShouldDropColumns | ShouldDropKeys | ShouldDropOrders
	}
	
	public abstract class SQLDevice : Device
	{
		public const string CSQLDateTimeScalarType = "SQLDevice.SQLDateTime";
		public const string CSQLTimeScalarType = "SQLDevice.SQLTime";
		public const string CSQLTextScalarType = "SQLDevice.SQLText";
		public const string CSQLITextScalarType = "SQLDevice.SQLIText";

		public SQLDevice(int AID, string AName) : base(AID, AName)
		{
			SetMaxIdentifierLength();
			FSupportsTransactions = true;
		}

		// Schema
		private string FSchema = String.Empty;
		public string Schema
		{
			get { return FSchema; }
			set { FSchema = value == null ? String.Empty : value; }
		}

		private bool FUseStatementTerminator = true;
		public bool UseStatementTerminator
		{
			get { return FUseStatementTerminator; }
			set { FUseStatementTerminator = value; }
		}
		
		private bool FUseParametersForCursors = false;
		public bool UseParametersForCursors
		{
			get { return FUseParametersForCursors; }
			set { FUseParametersForCursors = value; }
		}
		
		private bool FShouldNormalizeWhitespace = true;
		public bool ShouldNormalizeWhitespace
		{
			get { return FShouldNormalizeWhitespace; }
			set { FShouldNormalizeWhitespace = value; }
		}

		private bool FUseQuotedIdentifiers = true;
		public bool UseQuotedIdentifiers
		{
			set { FUseQuotedIdentifiers = value;}
			get { return FUseQuotedIdentifiers;}
		}
		
		private bool FUseTransactions = true;
		/// <summary>Indicates whether or not to use transactions through the CLI of the target system.</summary>
		public bool UseTransactions
		{
			set { FUseTransactions = value; }
			get { return FUseTransactions; }
		}
		
		private bool FUseQualifiedNames = false;
		/// <summary>Indicates whether or not to use the fully qualified name of an object in D4 to produce the storage name for the object.</summary>
		/// <remarks>Qualifiers will be replaced with underscores in the resulting storage name.</remarks>
		public bool UseQualifiedNames
		{
			get { return FUseQualifiedNames; }
			set { FUseQualifiedNames = value; }
		}
		
		private bool FSupportsAlgebraicFromClause = true;
		/// <summary>Indicates whether or not the dialect supports an algebraic from clause.</summary>
		public bool SupportsAlgebraicFromClause
		{
			get { return FSupportsAlgebraicFromClause; }
			set { FSupportsAlgebraicFromClause = value; }
		}
		
		private string FOnExecuteConnectStatement;
		/// <summary>A D4 expression denoting an SQL statement in the target dialect to be executed on all new execute connections.</summary>
		/// <remarks>The statement will be executed in its own transaction, not the transactional context of the spawning process.</remarks>
		public string OnExecuteConnectStatement
		{
			get { return FOnExecuteConnectStatement; }
			set { FOnExecuteConnectStatement = value; }
		}
		
		private string FOnBrowseConnectStatement;
		/// <summary>A D4 expression denoting an SQL statement in the target dialect to be executed on all new browse connections.</summary>
		/// <remarks>The statement will be executed in its own transaction, not the transactional context of the spawning process.</remarks>
		public string OnBrowseConnectStatement
		{
			get { return FOnBrowseConnectStatement; }
			set { FOnBrowseConnectStatement = value; }
		}
		
		private bool FSupportsSubSelectInSelectClause = true;
		public bool SupportsSubSelectInSelectClause
		{
			get { return FSupportsSubSelectInSelectClause; }
			set { FSupportsSubSelectInSelectClause = value; }
		}
		
		private bool FSupportsSubSelectInWhereClause = true;
		public bool SupportsSubSelectInWhereClause
		{
			get { return FSupportsSubSelectInWhereClause; }
			set { FSupportsSubSelectInWhereClause = value; }
		}
		
		private bool FSupportsSubSelectInGroupByClause = true;
		public bool SupportsSubSelectInGroupByClause
		{
			get { return FSupportsSubSelectInGroupByClause; }
			set { FSupportsSubSelectInGroupByClause = value; }
		}
		
		private bool FSupportsSubSelectInHavingClause = true;
		public bool SupportsSubSelectInHavingClause
		{
			get { return FSupportsSubSelectInHavingClause; }
			set { FSupportsSubSelectInHavingClause = value; }
		}
		
		private bool FSupportsSubSelectInOrderByClause = true;
		public bool SupportsSubSelectInOrderByClause
		{
			get { return FSupportsSubSelectInOrderByClause; }
			set { FSupportsSubSelectInOrderByClause = value; }
		}
		
		// True if the device supports nesting in the from clause.
		// This is also used to determine whether an extend whose source has introduced columns (an add following another add), will nest the resulting expression, or use replacement referencing to avoid the nesting.
		private bool FSupportsNestedFrom = true;
		public bool SupportsNestedFrom
		{
			get { return FSupportsNestedFrom; }
			set { FSupportsNestedFrom = value; }
		}

		private bool FSupportsNestedCorrelation = true;
		public bool SupportsNestedCorrelation
		{
			get { return FSupportsNestedCorrelation; }
			set { FSupportsNestedCorrelation = value; }
		}

		// True if the device supports the use of expressions in the order by clause		
		private bool FSupportsOrderByExpressions = false;
		public bool SupportsOrderByExpressions
		{
			get { return FSupportsOrderByExpressions; }
			set { FSupportsOrderByExpressions = value; }
		}

		// True if the order by clause is processed as part of the query context 
		// If this is false the order must be specified in terms of the result set columns, rather than the range variable columns within the query
		private bool FIsOrderByInContext = true;
		public bool IsOrderByInContext
		{
			get { return FIsOrderByInContext; }
			set { FIsOrderByInContext = value; }
		}

		// True if insert statements should be constructed using a values clause, false to use a select expression		
		private bool FUseValuesClauseInInsert = true;
		public bool UseValuesClauseInInsert
		{
			get { return FUseValuesClauseInInsert; }
			set { FUseValuesClauseInInsert = value; }
		}
		
		private int FCommandTimeout = -1;
		/// <summary>The amount of time in seconds to wait before timing out waiting for a command to complete.</summary>
		/// <remarks>
		/// The default value for this property is -1, indicating that the default timeout value for the connectivity 
		/// implementation used by the device should be used. Beyond that, the interpretation of this value depends on
		/// the connectivity implementation used by the device. For most implementations, a value of 0 indicates an
		/// infinite timeout.
		/// </remarks>
		public int CommandTimeout
		{
			get { return FCommandTimeout; }
			set { FCommandTimeout = value; }
		}

		#if USEISTRING		
		private bool FIsCaseSensitive;
		public bool IsCaseSensitive
		{
			get { return FIsCaseSensitive; }
			set { FIsCaseSensitive = value; }
		}
		#endif
		
		public virtual ErrorSeverity ExceptionOccurred(Exception AException)
		{
			return ErrorSeverity.Application;
		}

		protected int FMaxIdentifierLength = int.MaxValue;
		public int MaxIdentifierLength
		{
			get { return FMaxIdentifierLength; }
			set { FMaxIdentifierLength = value; }
		}
		
		protected virtual void SetMaxIdentifierLength() {}

		// verify that all types in the given table are mapped into this device
        public override void CheckSupported(Plan APlan, TableVar ATableVar)
        {
			// verify that the types of all columns have type maps
			foreach (Schema.Column LColumn in ATableVar.DataType.Columns)
				if (!(LColumn.DataType is Schema.ScalarType) || (ResolveDeviceScalarType(APlan, (Schema.ScalarType)LColumn.DataType) == null))
					if (Compiler.CouldGenerateDeviceScalarTypeMap(APlan, this, (Schema.ScalarType)LColumn.DataType))
					{
						D4.AlterDeviceStatement LStatement = new D4.AlterDeviceStatement();
						LStatement.DeviceName = Name;
						// BTR 1/18/2007 -> This really should be being marked as generated, however doing so
						// changes the dependency reporting for scalar type maps, and causes some catalog dependency errors,
						// so I cannot justify making this change in this version. Perhaps at some point, but not now...
						LStatement.CreateDeviceScalarTypeMaps.Add(new D4.DeviceScalarTypeMap(LColumn.DataType.Name));
						APlan.ExecuteNode(Compiler.BindNode(APlan, Compiler.CompileAlterDeviceStatement(APlan, LStatement)));
						ResolveDeviceScalarType(APlan, (Schema.ScalarType)LColumn.DataType); // Reresolve to attach a dependency to the generated map
					}
					else
						throw new SchemaException(SchemaException.Codes.UnsupportedScalarType, ATableVar.Name, Name, LColumn.DataType.Name, LColumn.Name);
					
			foreach (Schema.Key LKey in ATableVar.Keys)
				foreach (Schema.TableVarColumn LColumn in LKey.Columns)
					if (!SupportsComparison(APlan, LColumn.DataType))
						throw new SchemaException(SchemaException.Codes.UnsupportedKeyType, ATableVar.Name, Name, LColumn.DataType.Name, LColumn.Name);
        }								   
        
        public override D4.ClassDefinition GetDefaultOperatorClassDefinition(D4.MetaData AMetaData)
        {
			if ((AMetaData != null) && (AMetaData.Tags.Contains("Storage.TranslationString")))
				return 
					new D4.ClassDefinition
					(
						"SQLDevice.SQLUserOperator", 
						new D4.ClassAttributeDefinition[]
						{
							new D4.ClassAttributeDefinition("TranslationString", AMetaData.Tags["Storage.TranslationString"].Value),
							new D4.ClassAttributeDefinition("ContextLiteralParameterIndexes", D4.MetaData.GetTag(AMetaData, "Storage.ContextLiteralParameterIndexes", ""))
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
		
		protected override void InternalRegister(ServerProcess AProcess)
		{
			base.InternalRegister(AProcess);
			RegisterSystemObjectMaps(AProcess);
		}
		
		protected void RunScript(ServerProcess AProcess, string AScript)
		{
			// Note that this is also used to load the internal system catalog.
			IServerScript LScript = ((IServerProcess)AProcess).PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareScript(LScript);
			}
		}
		
		protected virtual void RegisterSystemObjectMaps(ServerProcess AProcess) {}
		
		// Emitter
		public virtual TextEmitter Emitter
		{
			get
			{
				SQLTextEmitter LEmitter = InternalCreateEmitter();
				LEmitter.UseStatementTerminator = UseStatementTerminator;
				LEmitter.UseQuotedIdentifiers = UseQuotedIdentifiers;					
				return LEmitter;
			}
		}

		protected virtual SQLTextEmitter InternalCreateEmitter()
		{
			return new SQLTextEmitter();
		}

		// Prepare		
		protected override DevicePlan CreateDevicePlan(Plan APlan, PlanNode APlanNode)
		{
			return new SQLDevicePlan(APlan, this, APlanNode);
		}
		
		protected override DevicePlanNode InternalPrepare(DevicePlan APlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)APlan;
			LDevicePlan.DevicePlanNode = new SQLDevicePlanNode(APlanNode);
			if (!((APlanNode is TableNode) || (APlanNode.DataType is Schema.IRowType)))
			{
				LDevicePlan.PushScalarContext();
				LDevicePlan.CurrentQueryContext().IsSelectClause = true;
			}
			try
			{
				if ((APlanNode.DataType != null) && APlanNode.DataType.Is(APlan.Plan.Catalog.DataTypes.SystemBoolean))
					LDevicePlan.EnterContext(true);
				try
				{
					LDevicePlan.DevicePlanNode.Statement = Translate(LDevicePlan, APlanNode);

					if (LDevicePlan.IsSupported)
					{
						if (APlanNode.DataType != null)
						{
							if (APlanNode.DataType.Is(APlan.Plan.Catalog.DataTypes.SystemBoolean))
							{
								LDevicePlan.DevicePlanNode.Statement =
									new CaseExpression
									(
										new CaseItemExpression[]
										{
											new CaseItemExpression
											(
												(Expression)LDevicePlan.DevicePlanNode.Statement,
												new ValueExpression(1)
											)
										},
										new CaseElseExpression(new ValueExpression(0))
									);
							}
							
							// Ensure that the statement is a unary select expression for the device Cursors...
							if ((APlanNode is TableNode) && (LDevicePlan.DevicePlanNode.Statement is QueryExpression) && (((QueryExpression)LDevicePlan.DevicePlanNode.Statement).TableOperators.Count > 0))
								LDevicePlan.DevicePlanNode.Statement = NestQueryExpression(LDevicePlan, ((TableNode)APlanNode).TableVar, LDevicePlan.DevicePlanNode.Statement);

							if (!(LDevicePlan.DevicePlanNode.Statement is SelectStatement))
							{
								if (!(LDevicePlan.DevicePlanNode.Statement is QueryExpression))
								{
									if (!(LDevicePlan.DevicePlanNode.Statement is SelectExpression))
									{
										SelectExpression LSelectExpression = new SelectExpression();
										LSelectExpression.SelectClause = new SelectClause();
										LSelectExpression.SelectClause.Columns.Add(new ColumnExpression((Expression)LDevicePlan.DevicePlanNode.Statement, "dummy1"));
										LSelectExpression.FromClause = new CalculusFromClause(GetDummyTableSpecifier());
										LDevicePlan.DevicePlanNode.Statement = LSelectExpression;
									}

									QueryExpression LQueryExpression = new QueryExpression();
									LQueryExpression.SelectExpression = (SelectExpression)LDevicePlan.DevicePlanNode.Statement;
									LDevicePlan.DevicePlanNode.Statement = LQueryExpression;
								}

								SelectStatement LSelectStatement = new SelectStatement();
								LSelectStatement.QueryExpression = (QueryExpression)LDevicePlan.DevicePlanNode.Statement;
								LDevicePlan.DevicePlanNode.Statement = LSelectStatement;
							}
							
							if (APlanNode is TableNode)
							{
								LDevicePlan.DevicePlanNode.Statement = TranslateOrder(LDevicePlan, (TableNode)APlanNode, (SelectStatement)LDevicePlan.DevicePlanNode.Statement);
								if (!LDevicePlan.IsSupported)
									return null;
							}
						}
								
						return LDevicePlan.DevicePlanNode;
					}

					return null;
				}
				finally
				{
					if ((APlanNode.DataType != null) && APlanNode.DataType.Is(APlan.Plan.Catalog.DataTypes.SystemBoolean))
						LDevicePlan.ExitContext();
				}
			}
			finally
			{
				if (!(APlanNode is TableNode))
					LDevicePlan.PopScalarContext();
			}
		}

		public virtual void DetermineCursorBehavior(Plan APlan, TableNode ATableNode)
		{
			ATableNode.RequestedCursorType = APlan.CursorContext.CursorType;
			ATableNode.CursorType = APlan.CursorContext.CursorType;
			ATableNode.CursorCapabilities = 
				CursorCapability.Navigable | 
				(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
			ATableNode.CursorIsolation = APlan.CursorContext.CursorIsolation;
			
			// Ensure that the node has an order that is a superset of some key
			Schema.Key LClusteringKey = Compiler.FindClusteringKey(APlan, ATableNode.TableVar);
			if ((ATableNode.Order == null) && (LClusteringKey.Columns.Count > 0))
				ATableNode.Order = Compiler.OrderFromKey(APlan, LClusteringKey);

			if (ATableNode.Order != null)
			{
				// Ensure that the order is unique
				bool LOrderUnique = false;	
				Schema.OrderColumn LNewColumn;

				foreach (Schema.Key LKey in ATableNode.TableVar.Keys)
					if (Compiler.OrderIncludesKey(APlan, ATableNode.Order, LKey))
					{
						LOrderUnique = true;
						break;
					}

				if (!LOrderUnique)
					foreach (Schema.TableVarColumn LColumn in Compiler.FindClusteringKey(APlan, ATableNode.TableVar).Columns)
						if (!ATableNode.Order.Columns.Contains(LColumn.Name))
						{
							LNewColumn = new Schema.OrderColumn(LColumn, true);
							LNewColumn.Sort = Compiler.GetSort(APlan, LColumn.DataType);
							LNewColumn.IsDefaultSort = true;
							ATableNode.Order.Columns.Add(LNewColumn);
						}
						else 
						{
							if (!System.Object.ReferenceEquals(ATableNode.Order.Columns[LColumn.Name].Sort, ((ScalarType)ATableNode.Order.Columns[LColumn.Name].Column.DataType).UniqueSort))
								ATableNode.Order.Columns[LColumn.Name].Sort = Compiler.GetUniqueSort(APlan, (ScalarType)LColumn.DataType);
						}
			}
		}

		/// <summary>
		/// This method returns a valid identifier for the specific SQL system in which it is called.  If overloaded, it must be completely deterministic.
		/// </summary>
		/// <param name="AIdentifier">The identifier to check</param>
		/// <returns>A valid identifier, based as close as possible to AIdentifier</returns>
		public string EnsureValidIdentifier(string AIdentifier)
		{
			return EnsureValidIdentifier(AIdentifier, FMaxIdentifierLength);
		}

		public virtual unsafe string EnsureValidIdentifier(string AIdentifier, int AMaxLength)
		{
			// first check to see if it is not reserved
			if (IsReservedWord(AIdentifier))
			{
				AIdentifier += "_DAE";
			}
			
			// Replace all double underscores with triple underscores
			AIdentifier = AIdentifier.Replace("__", "___");

			// Replace all qualifiers with double underscores
			AIdentifier = AIdentifier.Replace(".", "__");

			// then check to see if it has a name longer than AMaxLength characters
			if (AIdentifier.Length > AMaxLength)
			{
				byte[] LByteArray = new byte[4];
				fixed (byte* LArray = &LByteArray[0])
				{
					*((int*)LArray) = AIdentifier.GetHashCode();
				}
				string LString = Convert.ToBase64String(LByteArray);
				AIdentifier = AIdentifier.Substring(0, AMaxLength - LString.Length) + LString;
			}

			// replace invalid characters
			AIdentifier = AIdentifier.Replace("+", "_");
			AIdentifier = AIdentifier.Replace("/", "_");
			AIdentifier = AIdentifier.Replace("=", "_");
			return AIdentifier;
		}

		protected virtual bool IsReservedWord(string AWord) { return false; }

		public string ToSQLIdentifier(string AIdentifier)
		{
			return EnsureValidIdentifier(AIdentifier);
		}
		
		public virtual string ToSQLIdentifier(string AIdentifier, D4.MetaData AMetaData)
		{
			return D4.MetaData.GetTag(AMetaData, "Storage.Name", ToSQLIdentifier(AIdentifier));
		}
		
		//public virtual string ToSQLIdentifier(string AIdentifier, D4.MetaData AMetaData)
		public virtual string ToSQLIdentifier(Schema.Object AObject)
		{
			if ((AObject is CatalogObject) && (((CatalogObject)AObject).Library != null) && !UseQualifiedNames)
				return ToSQLIdentifier(DAE.Schema.Object.RemoveQualifier(AObject.Name, ((CatalogObject)AObject).Library.Name), AObject.MetaData);
			else
				return ToSQLIdentifier(AObject.Name, AObject.MetaData);
		}

		public virtual string ConvertNonIdentifierCharacter(char AInvalidChar)
		{
			switch (AInvalidChar)
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
				default : return Convert.ToUInt16(AInvalidChar).ToString();
			}
		}

		public virtual string FromSQLIdentifier(string AIdentifier)
		{
			StringBuilder LIdentifier = new StringBuilder();
			for (int LIndex = 0; LIndex < AIdentifier.Length; LIndex++)
				if (!Char.IsLetterOrDigit(AIdentifier[LIndex]) && (AIdentifier[LIndex] != '_'))
					LIdentifier.Append(ConvertNonIdentifierCharacter(AIdentifier[LIndex]));
				else
					LIdentifier.Append(AIdentifier[LIndex]);
			AIdentifier = LIdentifier.ToString();

			if (D4.ReservedWords.Contains(AIdentifier) || ((AIdentifier.Length > 0) && Char.IsDigit(AIdentifier[0])))
				AIdentifier = String.Format("_{0}", AIdentifier);
			return AIdentifier;
		}
		
		public virtual SelectExpression FindSelectExpression(Statement AStatement)
		{
			if (AStatement is QueryExpression)
				return ((QueryExpression)AStatement).SelectExpression;
			else if (AStatement is SelectExpression)
				return (SelectExpression)AStatement;
			else
				throw new SQLException(SQLException.Codes.InvalidStatementClass);
		}

		/// <remarks> In some contexts (such as an add) it is desireable to nest contexts to avoid duplicating entire 
		/// extend expressions.  Such contexts should pass true to ANestIfSupported.  Theoretically, each such context
		/// could detect references to the introduced columns before nesting, but for now their presence is used. </remarks>
		public virtual SelectExpression EnsureUnarySelectExpression(SQLDevicePlan ADevicePlan, TableVar ATableVar, Statement AStatement, bool ANestIfSupported)
		{
			if ((AStatement is QueryExpression) && (((QueryExpression)AStatement).TableOperators.Count > 0))
				return NestQueryExpression(ADevicePlan, ATableVar, AStatement);
			else if (ANestIfSupported && ADevicePlan.Device.SupportsNestedFrom && (ADevicePlan.CurrentQueryContext().AddedColumns.Count > 0))
			{
				ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The query is being nested to avoid unnecessary repetition of column expressions."));
				return NestQueryExpression(ADevicePlan, ATableVar, AStatement);
			}
			else
				return FindSelectExpression(AStatement);
		}

		public virtual SelectExpression NestQueryExpression(SQLDevicePlan ADevicePlan, TableVar ATableVar, Statement AStatement)
		{
			if (!ADevicePlan.Device.SupportsNestedFrom)
			{
				ADevicePlan.IsSupported = false;
				ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the device does not support nesting in the from clause."));
			}
			
			if (((ADevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasCorrelation) != 0) && !ADevicePlan.Device.SupportsNestedCorrelation)
			{
				ADevicePlan.IsSupported = false;
				ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the device does not support nested correlation."));
			}
				
			SQLRangeVar LRangeVar = new SQLRangeVar(ADevicePlan.GetNextTableAlias());
			foreach (TableVarColumn LColumn in ATableVar.Columns)
			{
				SQLRangeVarColumn LNestedRangeVarColumn = ADevicePlan.CurrentQueryContext().GetRangeVarColumn(LColumn.Name);
				SQLRangeVarColumn LRangeVarColumn = new SQLRangeVarColumn(LColumn, LRangeVar.Name, LNestedRangeVarColumn.Alias);
				LRangeVar.Columns.Add(LRangeVarColumn);
			}
			
			ADevicePlan.PopQueryContext();
			ADevicePlan.PushQueryContext();
			
			ADevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
			SelectExpression LSelectExpression = ADevicePlan.Device.FindSelectExpression(AStatement);

			SelectExpression LNewSelectExpression = new SelectExpression();
			if (LSelectExpression.FromClause is AlgebraicFromClause)
				LNewSelectExpression.FromClause = new AlgebraicFromClause(new TableSpecifier((Expression)AStatement, LRangeVar.Name));
			else
				LNewSelectExpression.FromClause = new CalculusFromClause(new TableSpecifier((Expression)AStatement, LRangeVar.Name));
				
			LNewSelectExpression.SelectClause = new SelectClause();
			foreach (TableVarColumn LColumn in ATableVar.Columns)
				LNewSelectExpression.SelectClause.Columns.Add(ADevicePlan.CurrentQueryContext().GetRangeVarColumn(LColumn.Name).GetColumnExpression());

			return LNewSelectExpression;
		}
		
		protected virtual Statement FromScalar(SQLDevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLScalarType LScalarType = (SQLScalarType)ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType);
			
			if (APlanNode.IsLiteral && APlanNode.IsDeterministic && !LScalarType.UseParametersForLiterals)
			{
				object LValue = ADevicePlan.Plan.EvaluateLiteralArgument(APlanNode, APlanNode.Description);
				
				Expression LValueExpression = null;
				if (LValue == null)
					LValueExpression = new CastExpression(new ValueExpression(null, TokenType.Nil), LScalarType.ParameterDomainName());
				else
					LValueExpression = new ValueExpression(LScalarType.ParameterFromScalar(ADevicePlan.Plan.ValueManager, LValue));
				
				if (APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean) && ADevicePlan.IsBooleanContext())
					return new BinaryExpression(new ValueExpression(1), "iEqual", LValueExpression);
				else
					return LValueExpression;
			}
			else
			{
				string LParameterName = String.Format("P{0}", ADevicePlan.DevicePlanNode.PlanParameters.Count + 1);
				SQLPlanParameter LPlanParameter = 
					new SQLPlanParameter
					(
						new SQLParameter
						(
							LParameterName, 
							LScalarType.GetSQLParameterType(), 
							null, 
							SQLDirection.In, 
							GetParameterMarker(LScalarType)
						), 
						APlanNode
					);
				ADevicePlan.DevicePlanNode.PlanParameters.Add(LPlanParameter);
				ADevicePlan.CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasParameters;
				if (APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean) && ADevicePlan.IsBooleanContext())
					return new BinaryExpression(new ValueExpression(1), "iEqual", new QueryParameterExpression(LParameterName));
				else
					return new QueryParameterExpression(LParameterName);
			}
		}

		public string GetParameterMarker(SQLScalarType AScalarType, TableVarColumn AColumn)
		{
			return GetParameterMarker(AScalarType, AColumn.MetaData);
		}

		public string GetParameterMarker(SQLScalarType AScalarType)
		{
			return GetParameterMarker(AScalarType, (D4.MetaData)null);
		}		
		
		public virtual string GetParameterMarker(SQLScalarType AScalarType, D4.MetaData AMetaData)
		{
			return null;
		}
		
		protected virtual Statement TranslateStackReference(SQLDevicePlan ADevicePlan, StackReferenceNode ANode)
		{
			return FromScalar(ADevicePlan, new StackReferenceNode(ANode.DataType, ANode.Location - ADevicePlan.Stack.Count));
		}
		
		protected virtual Statement TranslateStackColumnReference(SQLDevicePlan ADevicePlan, StackColumnReferenceNode ANode)
		{
			// If this is referencing an item on the device plan stack, translate as an identifier,
			// otherwise, translate as a query parameter
			if (ANode.Location < ADevicePlan.Stack.Count)
			{
				Expression LExpression = new QualifiedFieldExpression();
				SQLRangeVarColumn LRangeVarColumn = null;

				if (DAE.Schema.Object.Qualifier(ANode.Identifier) == Keywords.Left)
					LRangeVarColumn = ADevicePlan.CurrentJoinContext().LeftQueryContext.FindRangeVarColumn(DAE.Schema.Object.Dequalify(ANode.Identifier));
				else if (DAE.Schema.Object.Qualifier(ANode.Identifier) == Keywords.Right)
					LRangeVarColumn = ADevicePlan.CurrentJoinContext().RightQueryContext.FindRangeVarColumn(DAE.Schema.Object.Dequalify(ANode.Identifier));
				else
					LRangeVarColumn = ADevicePlan.FindRangeVarColumn(ANode.Identifier, false);

				if (LRangeVarColumn == null)
				{
					ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the reference to column ""{0}"" is out of context.", ANode.Identifier), ANode));
					ADevicePlan.IsSupported = false;
				}
				else
					LExpression = LRangeVarColumn.GetExpression();
				
				if (ADevicePlan.IsBooleanContext())
					return new BinaryExpression(LExpression, "iEqual", new ValueExpression(1)); // <> 0 is more robust, but = 1 is potentially more efficient

				return LExpression;
			}
			else
			{
				#if USECOLUMNLOCATIONBINDING
				return FromScalar(ADevicePlan, new StackColumnReferenceNode(ANode.Identifier, ANode.DataType, ANode.Location - ADevicePlan.Stack.Count, ANode.ColumnLocation));
				#else
				return FromScalar(ADevicePlan, new StackColumnReferenceNode(ANode.Identifier, ANode.DataType, ANode.Location - ADevicePlan.Stack.Count));
				#endif
			}
		}

		protected virtual Statement TranslateExtractColumnNode(SQLDevicePlan ADevicePlan, ExtractColumnNode ANode)
		{
			StackReferenceNode LSourceNode = ANode.Nodes[0] as StackReferenceNode;
			if (LSourceNode != null)
			{
				if (LSourceNode.Location < ADevicePlan.Stack.Count)
				{
					Expression LExpression = new QualifiedFieldExpression();
					SQLRangeVarColumn LRangeVarColumn = null;
					
					if (DAE.Schema.Object.EnsureUnrooted(LSourceNode.Identifier) == Keywords.Left)
						LRangeVarColumn = ADevicePlan.CurrentJoinContext().LeftQueryContext.FindRangeVarColumn(ANode.Identifier);
					else if (DAE.Schema.Object.EnsureUnrooted(LSourceNode.Identifier) == Keywords.Right)
						LRangeVarColumn = ADevicePlan.CurrentJoinContext().RightQueryContext.FindRangeVarColumn(ANode.Identifier);
					else
						LRangeVarColumn = ADevicePlan.FindRangeVarColumn(DAE.Schema.Object.Qualify(ANode.Identifier, LSourceNode.Identifier), false);

					if (LRangeVarColumn == null)
					{
						ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the reference to column ""{0}"" is out of context.", ANode.Identifier), ANode));
						ADevicePlan.IsSupported = false;
					}
					else
						LExpression = LRangeVarColumn.GetExpression();
					
					if (ADevicePlan.IsBooleanContext())
						return new BinaryExpression(LExpression, "iEqual", new ValueExpression(1)); // <> 0 is more robust, but = 1 is potentially more efficient

					return LExpression;
				}
				else
					return FromScalar(ADevicePlan, new StackColumnReferenceNode(ANode.Identifier, ANode.DataType, LSourceNode.Location - ADevicePlan.Stack.Count));
			}
			else
			{
				Statement LStatement = Translate(ADevicePlan, ANode.Nodes[0]);
				if (ADevicePlan.IsSupported)
				{
					SelectExpression LExpression = FindSelectExpression(LStatement);
					ColumnExpression LColumnExpression = LExpression.SelectClause.Columns[ToSQLIdentifier(((Schema.IRowType)ANode.Nodes[0].DataType).Columns[ANode.Identifier].Name)];
					LExpression.SelectClause.Columns.Clear();
					LExpression.SelectClause.Columns.Add(LColumnExpression);

					if (ANode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean))
						if (!ADevicePlan.IsBooleanContext())
							LStatement =
								new CaseExpression
								(
									new CaseItemExpression[]
									{
										new CaseItemExpression
										(
											(Expression)LStatement,
											new ValueExpression(1)
										)
									}, 
									new CaseElseExpression(new ValueExpression(0))
								);
						else
							LStatement = new BinaryExpression((Expression)LStatement, "iEqual", new ValueExpression(1));
				}

				return LStatement;
			}
		}
        
		protected virtual Statement TranslateExtractRowNode(SQLDevicePlan ADevicePlan, ExtractRowNode ANode)
		{
			// Row extraction cannot be supported unless each column in the row being extracted has a map for equality comparison. Otherwise, the SQL Server
			// will complain that the 'blob column' cannot be used the given context (such as a subquery).
			foreach (Schema.Column LColumn in ((Schema.IRowType)ANode.DataType).Columns)
				if (!SupportsEqual(ADevicePlan.Plan, LColumn.DataType))
				{
					ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support equality comparison for values of type ""{0}"" for column ""{1}"".", LColumn.DataType.Name, LColumn.Name), ANode));
					ADevicePlan.IsSupported = false;
					return new CallExpression();
				}
				
			return TranslateExpression(ADevicePlan, ANode.Nodes[0], false);
		}
        
		protected virtual Statement TranslateValueNode(SQLDevicePlan ADevicePlan, ValueNode ANode)
		{
			if (ANode.DataType.IsGeneric && ((ANode.Value == null) || (ANode.Value == DBNull.Value)))	
				return new ValueExpression(null);
			return FromScalar(ADevicePlan, ANode);
		}
		
		protected virtual Statement TranslateAsNode(SQLDevicePlan ADevicePlan, AsNode ANode)
		{
			return new CastExpression((Expression)Translate(ADevicePlan, ANode.Nodes[0]), ((SQLScalarType)ADevicePlan.Device.ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)ANode.DataType)).ParameterDomainName());
		}
        
		public virtual Expression TranslateExpression(SQLDevicePlan ADevicePlan, PlanNode ANode, bool AIsBooleanContext)
		{
			ADevicePlan.EnterContext(AIsBooleanContext);
			try
			{
				if (!AIsBooleanContext && ANode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean))
				{	
					// case when <expression> then 1 else 0 end
					ADevicePlan.EnterContext(true);
					try
					{
						return
							new CaseExpression
							(
								new CaseItemExpression[]
								{
									new CaseItemExpression
									(
										(Expression)Translate(ADevicePlan, ANode), 
										new ValueExpression(1)
									)
								}, 
								new CaseElseExpression(new ValueExpression(0))
							);
					}
					finally
					{
						ADevicePlan.ExitContext();
					}
				}
				else
					return (Expression)Translate(ADevicePlan, ANode);
			}
			finally
			{
				ADevicePlan.ExitContext();
			}
		}
		
		protected virtual Statement TranslateConditionNode(SQLDevicePlan ADevicePlan, ConditionNode ANode)
		{
			return
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							TranslateExpression(ADevicePlan, ANode.Nodes[0], true),
							TranslateExpression(ADevicePlan, ANode.Nodes[1], false)
						)
					},
					new CaseElseExpression(TranslateExpression(ADevicePlan, ANode.Nodes[2], false))
				);
		}
		
		protected virtual Statement TranslateConditionedCaseNode(SQLDevicePlan ADevicePlan, ConditionedCaseNode ANode)
		{
			CaseExpression LCaseExpression = new CaseExpression();
			foreach (ConditionedCaseItemNode LNode in ANode.Nodes)
			{
				if (LNode.Nodes.Count == 2)
					LCaseExpression.CaseItems.Add
					(
						new CaseItemExpression
						(
							TranslateExpression(ADevicePlan, LNode.Nodes[0], true),
							TranslateExpression(ADevicePlan, LNode.Nodes[1], false)
						)
					);
				else
					LCaseExpression.ElseExpression = new CaseElseExpression(TranslateExpression(ADevicePlan, LNode.Nodes[0], false));
			}
			return LCaseExpression;
		}
		
		protected virtual Statement TranslateSelectedConditionedCaseNode(SQLDevicePlan ADevicePlan, SelectedConditionedCaseNode ANode)
		{
			CaseExpression LCaseExpression = new CaseExpression();
			LCaseExpression.Expression = TranslateExpression(ADevicePlan, ANode.Nodes[0], false);
			for (int LIndex = 2; LIndex < ANode.Nodes.Count; LIndex++)
			{
				ConditionedCaseItemNode LNode = (ConditionedCaseItemNode)ANode.Nodes[LIndex];
				if (LNode.Nodes.Count == 2)
					LCaseExpression.CaseItems.Add
					(
						new CaseItemExpression
						(
							TranslateExpression(ADevicePlan, LNode.Nodes[0], false),
							TranslateExpression(ADevicePlan, LNode.Nodes[1], false)
						)
					);
				else
					LCaseExpression.ElseExpression = new CaseElseExpression(TranslateExpression(ADevicePlan, LNode.Nodes[0], false));
			}
			return LCaseExpression; 
		}
        
		public virtual TableSpecifier GetDummyTableSpecifier()
		{
			SelectExpression LSelectExpression = new SelectExpression();
			LSelectExpression.SelectClause = new SelectClause();
			LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(0), "dummy1"));
			return new TableSpecifier(LSelectExpression, "dummy1");
		}
		
		protected virtual Statement TranslateListNode(SQLDevicePlan ADevicePlan, ListNode ANode)
		{
			if (!ADevicePlan.CurrentQueryContext().IsListContext)
			{
				ADevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(@"Plan is not supported because the device only supports list expressions as the right argument to an invocation of the membership operator (in).", ANode));
				ADevicePlan.IsSupported = false;
				return new CallExpression();
			}
			
			ListExpression LListExpression = new ListExpression();
			foreach (PlanNode LNode in ANode.Nodes)
				LListExpression.Expressions.Add(TranslateExpression(ADevicePlan, LNode, false));
			return LListExpression;
		}
		
		protected virtual Statement TranslateRowSelectorNode(SQLDevicePlan ADevicePlan, RowSelectorNode ANode)
		{
			SelectExpression LSelectExpression = new SelectExpression();
			LSelectExpression.SelectClause = new SelectClause();
			if (FSupportsAlgebraicFromClause)
				LSelectExpression.FromClause = new AlgebraicFromClause(GetDummyTableSpecifier());
			else
				LSelectExpression.FromClause = new CalculusFromClause(GetDummyTableSpecifier());
			ADevicePlan.PushScalarContext();
			try
			{
				for (int LIndex = 0; LIndex < ANode.DataType.Columns.Count; LIndex++)
				{
					SQLRangeVarColumn LRangeVarColumn = ADevicePlan.FindRangeVarColumn(ANode.DataType.Columns[LIndex].Name, false);
					if (LRangeVarColumn != null)
					{
						LRangeVarColumn.Expression = TranslateExpression(ADevicePlan, ANode.Nodes[LIndex], false);
						LSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
					}
					else
						LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(TranslateExpression(ADevicePlan, ANode.Nodes[LIndex], false), ToSQLIdentifier(ANode.DataType.Columns[LIndex].Name)));
				}
				
				ADevicePlan.CurrentQueryContext().ParentContext.ReferenceFlags |= ADevicePlan.CurrentQueryContext().ReferenceFlags;
			}
			finally
			{
				ADevicePlan.PopScalarContext();
			}

			return LSelectExpression;
		}
        
		protected virtual Statement TranslateTableSelectorNode(SQLDevicePlan ADevicePlan, TableSelectorNode ANode)
		{
			QueryExpression LQueryExpression = new QueryExpression();
			ADevicePlan.CurrentQueryContext().RangeVars.Add(new SQLRangeVar(ADevicePlan.GetNextTableAlias()));
			SQLRangeVar LRangeVar = new SQLRangeVar(ADevicePlan.GetNextTableAlias());
			foreach (TableVarColumn LColumn in ANode.TableVar.Columns)
				ADevicePlan.CurrentQueryContext().AddedColumns.Add(new SQLRangeVarColumn(LColumn, String.Empty, ADevicePlan.Device.ToSQLIdentifier(LColumn), ADevicePlan.Device.ToSQLIdentifier(LColumn.Name)));
			
			foreach (PlanNode LNode in ANode.Nodes)
			{
				Statement LStatement = Translate(ADevicePlan, LNode);
				if (ADevicePlan.IsSupported)
				{
					SelectExpression LSelectExpression = ADevicePlan.Device.FindSelectExpression(LStatement);
					if (LQueryExpression.SelectExpression == null)
						LQueryExpression.SelectExpression = LSelectExpression;
					else
						LQueryExpression.TableOperators.Add(new TableOperatorExpression(TableOperator.Union, false, LSelectExpression));
				}
			}
			return LQueryExpression;
		}
        
		protected virtual Statement TranslateInsertNode(SQLDevicePlan ADevicePlan, InsertNode ANode)
		{
			if (ADevicePlan.Plan.ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			InsertStatement LStatement = new InsertStatement();
			LStatement.InsertClause = new InsertClause();
			LStatement.InsertClause.TableExpression = new TableExpression();
			BaseTableVar LTarget = (BaseTableVar)((TableVarNode)ANode.Nodes[1]).TableVar;
			LStatement.InsertClause.TableExpression.TableSchema = D4.MetaData.GetTag(LTarget.MetaData, "Storage.Schema", Schema);
			LStatement.InsertClause.TableExpression.TableName = ToSQLIdentifier(LTarget);
			foreach (Column LColumn in ((TableNode)ANode.Nodes[0]).DataType.Columns)
				LStatement.InsertClause.Columns.Add(new InsertFieldExpression(ToSQLIdentifier(LColumn.Name)));
			LStatement.Values = TranslateExpression(ADevicePlan, ANode.Nodes[0], false);	
			return LStatement;
		}
        
		protected virtual Statement TranslateUpdateNode(SQLDevicePlan ADevicePlan, UpdateNode ANode)
		{														
			if (ADevicePlan.Plan.ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			BaseTableVar LTarget = ((BaseTableVar)((TableVarNode)ANode.Nodes[0]).TableVar);
			UpdateStatement LStatement = new UpdateStatement();
			LStatement.UpdateClause = new UpdateClause();
			LStatement.UpdateClause.TableExpression = new TableExpression();
			LStatement.UpdateClause.TableExpression.TableSchema = D4.MetaData.GetTag(LTarget.MetaData, "Storage.Schema", Schema);
			LStatement.UpdateClause.TableExpression.TableName = ToSQLIdentifier(LTarget);

			ADevicePlan.Stack.Push(new Symbol(String.Empty, LTarget.DataType.RowType));
			try
			{
				for (int LIndex = 1; LIndex < ANode.Nodes.Count; LIndex++)
				{
					UpdateFieldExpression LFieldExpression = new UpdateFieldExpression();
					UpdateColumnNode LColumnNode = (UpdateColumnNode)ANode.Nodes[LIndex];
					#if USECOLUMNLOCATIONBINDING
					LFieldExpression.FieldName = ToSQLIdentifier(LTarget.DataType.Columns[LColumnNode.ColumnLocation].Name);
					#else
					LFieldExpression.FieldName = ToSQLIdentifier(LTarget.DataType.Columns[LColumnNode.ColumnName].Name);
					#endif
					LFieldExpression.Expression = TranslateExpression(ADevicePlan, LColumnNode.Nodes[0], false);
					LStatement.UpdateClause.Columns.Add(LFieldExpression);
				}
			}
			finally
			{
				ADevicePlan.Stack.Pop();
			}

			return LStatement;
		}
        
		protected virtual Statement TranslateDeleteNode(SQLDevicePlan ADevicePlan, DeleteNode ANode)
		{
			if (ADevicePlan.Plan.ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			DeleteStatement LStatement = new DeleteStatement();
			LStatement.DeleteClause = new DeleteClause();
			LStatement.DeleteClause.TableExpression = new TableExpression();
			BaseTableVar LTarget = (BaseTableVar)((TableVarNode)ANode.Nodes[0]).TableVar;
			LStatement.DeleteClause.TableExpression.TableSchema = D4.MetaData.GetTag(LTarget.MetaData, "Storage.Schema", Schema);
			LStatement.DeleteClause.TableExpression.TableName = ToSQLIdentifier(LTarget);
			return LStatement;
		}
        
		protected virtual string GetIndexName(string ATableName, Key AKey)
		{
			if ((AKey.MetaData != null) && AKey.MetaData.Tags.Contains("Storage.Name"))
				return AKey.MetaData.Tags["Storage.Name"].Value;
			else
			{
				StringBuilder LIndexName = new StringBuilder();
				LIndexName.AppendFormat("UIDX_{0}", ATableName);
				foreach (TableVarColumn LColumn in AKey.Columns)
					LIndexName.AppendFormat("_{0}", (LColumn.Name));
				return EnsureValidIdentifier(LIndexName.ToString());
			}
		}
        
		protected virtual string GetIndexName(string ATableName, Order AOrder)
		{
			if ((AOrder.MetaData != null) && AOrder.MetaData.Tags.Contains("Storage.Name"))
				return AOrder.MetaData.Tags["Storage.Name"].Value;
			else
			{
				StringBuilder LIndexName = new StringBuilder();
				LIndexName.AppendFormat("IDX_{0}", ATableName);
				foreach (OrderColumn LColumn in AOrder.Columns)
					LIndexName.AppendFormat("_{0}", (LColumn.Column.Name));
				return EnsureValidIdentifier(LIndexName.ToString());
			}
		}
        
		protected virtual Statement TranslateCreateIndex(SQLDevicePlan APlan, TableVar ATableVar, Key AKey)
		{
			CreateIndexStatement LIndex = new CreateIndexStatement();
			LIndex.IsUnique = !AKey.IsSparse;
			LIndex.IsClustered = AKey.Equals(Compiler.FindClusteringKey(APlan.Plan, ATableVar));
			LIndex.TableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
			LIndex.TableName = ToSQLIdentifier(ATableVar);
			LIndex.IndexSchema = D4.MetaData.GetTag(AKey.MetaData, "Storage.Schema", String.Empty);
			LIndex.IndexName = GetIndexName(LIndex.TableName, AKey);
			OrderColumnDefinition LColumnDefinition;
			foreach (TableVarColumn LColumn in AKey.Columns)
			{
				LColumnDefinition = new OrderColumnDefinition();
				LColumnDefinition.ColumnName = ToSQLIdentifier(LColumn);
				LColumnDefinition.Ascending = true;
				LIndex.Columns.Add(LColumnDefinition);
			}
			return LIndex;
		}
        
		protected virtual Statement TranslateDropIndex(SQLDevicePlan APlan, TableVar ATableVar, Key AKey)
		{
			DropIndexStatement LStatement = new DropIndexStatement();
			LStatement.IndexSchema = D4.MetaData.GetTag(AKey.MetaData, "Storage.Schema", String.Empty);
			LStatement.IndexName = GetIndexName(ToSQLIdentifier(ATableVar), AKey);
			return LStatement;
		}
        
		protected virtual Statement TranslateCreateIndex(SQLDevicePlan APlan, TableVar ATableVar, Order AOrder)
		{
			CreateIndexStatement LIndex = new CreateIndexStatement();
			LIndex.IsClustered = Convert.ToBoolean(D4.MetaData.GetTag(AOrder.MetaData, "DAE.IsClustered", "false"));
			LIndex.TableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
			LIndex.TableName = ToSQLIdentifier(ATableVar);
			LIndex.IndexSchema = D4.MetaData.GetTag(AOrder.MetaData, "Storage.Schema", String.Empty);
			LIndex.IndexName = GetIndexName(LIndex.TableName, AOrder);
			OrderColumnDefinition LColumnDefinition;
			foreach (OrderColumn LColumn in AOrder.Columns)
			{
				LColumnDefinition = new OrderColumnDefinition();
				LColumnDefinition.ColumnName = ToSQLIdentifier(LColumn.Column);
				LColumnDefinition.Ascending = LColumn.Ascending;
				LIndex.Columns.Add(LColumnDefinition);
			}
			return LIndex;
		}
        
		protected virtual Statement TranslateDropIndex(SQLDevicePlan APlan, TableVar ATableVar, Order AOrder)
		{
			DropIndexStatement LStatement = new DropIndexStatement();
			LStatement.IndexSchema = D4.MetaData.GetTag(AOrder.MetaData, "Storage.Schema", String.Empty);
			LStatement.IndexName = GetIndexName(ToSQLIdentifier(ATableVar), AOrder);
			return LStatement;
		}
        
		protected virtual Statement TranslateCreateTable(SQLDevicePlan APlan, TableVar ATableVar)
		{
			Batch LBatch = new Batch();
			CreateTableStatement LStatement = new CreateTableStatement();
			LStatement.TableSchema = D4.MetaData.GetTag(ATableVar.MetaData, "Storage.Schema", Schema);
			LStatement.TableName = ToSQLIdentifier(ATableVar);
			foreach (TableVarColumn LColumn in ATableVar.Columns)
			{
				if (Convert.ToBoolean(D4.MetaData.GetTag(LColumn.MetaData, "Storage.ShouldReconcile", "true")))
				{
					SQLScalarType LSQLScalarType = (SQLScalarType)ResolveDeviceScalarType(APlan.Plan, (Schema.ScalarType)LColumn.DataType);
					if (LSQLScalarType == null)
						throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, LColumn.DataType.ToString());
					LStatement.Columns.Add
					(
						new ColumnDefinition
						(
							ToSQLIdentifier(LColumn), 
							LSQLScalarType.DomainName(LColumn), 
							LColumn.IsNilable
						)
					);
				}
			}
			LBatch.Statements.Add(LStatement);

			foreach (Key LKey in ATableVar.Keys)
				if (Convert.ToBoolean(D4.MetaData.GetTag(LKey.MetaData, "Storage.ShouldReconcile", (LKey.Columns.Count > 0).ToString())))
					LBatch.Statements.Add(TranslateCreateIndex(APlan, ATableVar, LKey));

			foreach (Order LOrder in ATableVar.Orders)
				if (Convert.ToBoolean(D4.MetaData.GetTag(LOrder.MetaData, "Storage.ShouldReconcile", (LOrder.Columns.Count > 0).ToString())))
					LBatch.Statements.Add(TranslateCreateIndex(APlan, ATableVar, LOrder));
			
			return LBatch;
		}
        
		protected virtual Statement TranslateCreateTableNode(SQLDevicePlan ADevicePlan, CreateTableNode ANode)
		{
			return TranslateCreateTable(ADevicePlan, ANode.Table);
		}
		
		protected bool AltersStorageTags(D4.AlterMetaData AAlterMetaData)
		{
			if (AAlterMetaData == null)
				return false;
				
			for (int LIndex = 0; LIndex < AAlterMetaData.DropTags.Count; LIndex++)
				if (AAlterMetaData.DropTags[LIndex].Name.IndexOf("Storage") == 0)
					return true;
					
			for (int LIndex = 0; LIndex < AAlterMetaData.AlterTags.Count; LIndex++)
				if (AAlterMetaData.AlterTags[LIndex].Name.IndexOf("Storage") == 0)
					return true;
					
			for (int LIndex = 0; LIndex < AAlterMetaData.CreateTags.Count; LIndex++)
				if (AAlterMetaData.CreateTags[LIndex].Name.IndexOf("Storage") == 0)
					return true;
					
			return false;
		}
        
		protected virtual Statement TranslateAlterTableNode(SQLDevicePlan ADevicePlan, AlterTableNode ANode)
		{
			Batch LBatch = new Batch();
			string LTableSchema = D4.MetaData.GetTag(ANode.TableVar.MetaData, "Storage.Schema", Schema);
			string LTableName = ToSQLIdentifier(ANode.TableVar);
			if (ANode.AlterTableStatement.DropColumns.Count > 0)
			{
				foreach (D4.DropColumnDefinition LColumn in ANode.AlterTableStatement.DropColumns)
				{
					TableVarColumn LTableVarColumn = null;
					SchemaLevelDropColumnDefinition LSchemaLevelDropColumnDefinition = LColumn as SchemaLevelDropColumnDefinition;
					if (LSchemaLevelDropColumnDefinition != null)
						LTableVarColumn = LSchemaLevelDropColumnDefinition.Column;
					else
						LTableVarColumn = ANode.TableVar.Columns[ANode.TableVar.Columns.IndexOfName(LColumn.ColumnName)];
						
					if (Convert.ToBoolean(D4.MetaData.GetTag(LTableVarColumn.MetaData, "Storage.ShouldReconcile", "true")))
					{
						AlterTableStatement LStatement = new AlterTableStatement();
						LStatement.TableSchema = LTableSchema;
						LStatement.TableName = LTableName;
						LStatement.DropColumns.Add(new DropColumnDefinition(ToSQLIdentifier(LTableVarColumn)));
						LBatch.Statements.Add(LStatement);
					}
				}
			}
			
			if (ANode.AlterTableStatement.AlterColumns.Count > 0)
			{
				foreach (D4.AlterColumnDefinition LAlterColumnDefinition in ANode.AlterTableStatement.AlterColumns)
				{
					TableVarColumn LColumn = ANode.TableVar.Columns[LAlterColumnDefinition.ColumnName];
					if (Convert.ToBoolean(D4.MetaData.GetTag(LColumn.MetaData, "Storage.ShouldReconcile", "true")))
					{
						// The assumption being made here is that the type of the column in the table var has already been changed to the new type, the presence of the type specifier is just to indicate that the alter should be performed
						// This assumption will likely need to be revisited when (if) we actually start supporting changing the type of a column
						if (LAlterColumnDefinition.ChangeNilable || (LAlterColumnDefinition.TypeSpecifier != null) || AltersStorageTags(LAlterColumnDefinition.AlterMetaData))
						{
							AlterTableStatement LStatement = new AlterTableStatement();
							LStatement.TableSchema = LTableSchema;
							LStatement.TableName = LTableName;
							LStatement.AlterColumns.Add(new AlterColumnDefinition(ToSQLIdentifier(LColumn), ((SQLScalarType)ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)LColumn.DataType)).DomainName(LColumn), LColumn.IsNilable));
							LBatch.Statements.Add(LStatement);
						}
					}
				}
			}
			
			if (ANode.AlterTableStatement.CreateColumns.Count > 0)
			{
				foreach (D4.ColumnDefinition LColumnDefinition in ANode.AlterTableStatement.CreateColumns)
				{
					TableVarColumn LColumn = ANode.TableVar.Columns[LColumnDefinition.ColumnName];
					if (Convert.ToBoolean(D4.MetaData.GetTag(LColumn.MetaData, "Storage.ShouldReconcile", "true")))
					{
						AlterTableStatement LStatement = new AlterTableStatement();
						LStatement.TableSchema = LTableSchema;
						LStatement.TableName = LTableName;
						LStatement.AddColumns.Add(new ColumnDefinition(ToSQLIdentifier(LColumn), ((SQLScalarType)ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)LColumn.DataType)).DomainName(LColumn), true));
						LBatch.Statements.Add(LStatement);
					}
				}
			}
			
			foreach (D4.DropKeyDefinition LKeyDefinition in ANode.AlterTableStatement.DropKeys)
			{
				Schema.Key LKey = null;
				SchemaLevelDropKeyDefinition LSchemaLevelDropKeyDefinition = LKeyDefinition as SchemaLevelDropKeyDefinition;
				if (LSchemaLevelDropKeyDefinition != null)
					LKey = LSchemaLevelDropKeyDefinition.Key;
				else
					LKey = Compiler.FindKey(ADevicePlan.Plan, ANode.TableVar, LKeyDefinition);

				if (Convert.ToBoolean(D4.MetaData.GetTag(LKey.MetaData, "Storage.ShouldReconcile", (LKey.Columns.Count > 0).ToString())))
					LBatch.Statements.Add(TranslateDropIndex(ADevicePlan, ANode.TableVar, LKey));
			}
				
			foreach (D4.DropOrderDefinition LOrderDefinition in ANode.AlterTableStatement.DropOrders)
			{
				Schema.Order LOrder = null;
				SchemaLevelDropOrderDefinition LSchemaLevelDropOrderDefinition = LOrderDefinition as SchemaLevelDropOrderDefinition;
				if (LSchemaLevelDropOrderDefinition != null)
					LOrder = LSchemaLevelDropOrderDefinition.Order;
				else
					LOrder = Compiler.FindOrder(ADevicePlan.Plan, ANode.TableVar, LOrderDefinition);

				if (Convert.ToBoolean(D4.MetaData.GetTag(LOrder.MetaData, "Storage.ShouldReconcile", (LOrder.Columns.Count > 0).ToString())))
					LBatch.Statements.Add(TranslateDropIndex(ADevicePlan, ANode.TableVar, LOrder));
			}
				
			foreach (D4.KeyDefinition LKeyDefinition in ANode.AlterTableStatement.CreateKeys)
			{
				Schema.Key LKey = Compiler.CompileKeyDefinition(ADevicePlan.Plan, ANode.TableVar, LKeyDefinition);
				if (Convert.ToBoolean(D4.MetaData.GetTag(LKey.MetaData, "Storage.ShouldReconcile", (LKey.Columns.Count > 0).ToString())))
					LBatch.Statements.Add(TranslateCreateIndex(ADevicePlan, ANode.TableVar, LKey));
			}
				
			foreach (D4.OrderDefinition LOrderDefinition in ANode.AlterTableVarStatement.CreateOrders)
			{
				Schema.Order LOrder = Compiler.CompileOrderDefinition(ADevicePlan.Plan, ANode.TableVar, LOrderDefinition, false);
				if (Convert.ToBoolean(D4.MetaData.GetTag(LOrder.MetaData, "Storage.ShouldReconcile", (LOrder.Columns.Count > 0).ToString())))
					LBatch.Statements.Add(TranslateCreateIndex(ADevicePlan, ANode.TableVar, LOrder));
			}

			return LBatch;
		}
        
		protected virtual Statement TranslateDropTableNode(DevicePlan ADevicePlan, DropTableNode ANode)
		{
			DropTableStatement LStatement = new DropTableStatement();
			LStatement.TableSchema = D4.MetaData.GetTag(ANode.Table.MetaData, "Storage.Schema", Schema);
			LStatement.TableName = ToSQLIdentifier(ANode.Table);
			return LStatement;
		}
        
		public virtual SelectStatement TranslateOrder(DevicePlan ADevicePlan, TableNode ANode, SelectStatement AStatement)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if ((ANode.Order != null) && (ANode.Order.Columns.Count > 0))
			{
				AStatement.OrderClause = new OrderClause();
				SQLRangeVarColumn LRangeVarColumn;
				foreach (OrderColumn LColumn in ANode.Order.Columns)
				{
					if (!LColumn.IsDefaultSort)
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(@"Plan is not supported because the order uses expression-based sorting.", ANode));
						LDevicePlan.IsSupported = false;
						break;
					}
					
					if (!LDevicePlan.Device.SupportsComparison(ADevicePlan.Plan, LColumn.Column.DataType))
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support comparison of values of type ""{0}"" for column ""{1}"".", LColumn.Column.DataType.Name, LColumn.Column.Name), ANode));
						LDevicePlan.IsSupported = false;
						break;
					}

					OrderFieldExpression LFieldExpression = new OrderFieldExpression();
					LRangeVarColumn = LDevicePlan.GetRangeVarColumn(LColumn.Column.Name, true);
					if (IsOrderByInContext)
					{
						if (LRangeVarColumn.Expression != null)
							Error.Fail("Expressions in the order by clause are not implemented.");
						LFieldExpression.FieldName = LRangeVarColumn.ColumnName;
						LFieldExpression.TableAlias = LRangeVarColumn.TableAlias;
					}
					else
						LFieldExpression.FieldName = LRangeVarColumn.Alias == String.Empty ? LRangeVarColumn.ColumnName : LRangeVarColumn.Alias;
					LFieldExpression.Ascending = LColumn.Ascending;
					AStatement.OrderClause.Columns.Add(LFieldExpression);
				}
			}
			
			return AStatement;
		}

		private bool SupportsOperator(Plan APlan, Schema.Operator AOperator)
		{
			// returns true if there exists a ScalarTypeOperator corresponding to the Operator for this InstructionNode
			return ResolveDeviceOperator(APlan, AOperator) != null;
		}

		private bool SupportsOperands(InstructionNodeBase ANode)
		{
			if ((ANode.DataType is Schema.ITableType) || (ANode.DataType is Schema.IScalarType) || (ANode.DataType is Schema.IRowType))
				return true;
			
			return false;
		}
		
		public bool SupportsEqual(Plan APlan, Schema.IDataType ADataType)
		{
			if (SupportsComparison(APlan, ADataType))
				return true;
				
			Schema.Signature LSignature = new Schema.Signature(new SignatureElement[]{new SignatureElement(ADataType), new SignatureElement(ADataType)});
			OperatorBindingContext LContext = new OperatorBindingContext(null, "iEqual", APlan.NameResolutionPath, LSignature, true);
			Compiler.ResolveOperator(APlan, LContext);
			if (LContext.Operator != null)
			{
				Schema.DeviceOperator LDeviceOperator = ResolveDeviceOperator(APlan, LContext.Operator);
				if (LDeviceOperator != null)
				{
					APlan.AttachDependency(LDeviceOperator);
					return true;
				}
			}
			return false;
		}
		
		public bool SupportsComparison(Plan APlan, Schema.IDataType ADataType)
		{
			Schema.Signature LSignature = new Schema.Signature(new SignatureElement[]{new SignatureElement(ADataType), new SignatureElement(ADataType)});
			OperatorBindingContext LContext = new OperatorBindingContext(null, "iCompare", APlan.NameResolutionPath, LSignature, true);
			Compiler.ResolveOperator(APlan, LContext);
			if (LContext.Operator != null)
			{
				Schema.DeviceOperator LDeviceOperator = ResolveDeviceOperator(APlan, LContext.Operator);
				if (LDeviceOperator != null)
				{
					APlan.AttachDependency(LDeviceOperator);
					return true;
				}
			}
			return false;
		}
		
		protected bool IsTruthValued(DeviceOperator ADeviceOperator)
		{
			SQLDeviceOperator LDeviceOperator = ADeviceOperator as SQLDeviceOperator;
			return (LDeviceOperator != null) && LDeviceOperator.IsTruthValued;
		}

		// Translate
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			bool LScalarContext = false;
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if (((APlanNode is TableNode) || (APlanNode.DataType is Schema.IRowType)) && (LDevicePlan.CurrentQueryContext().IsScalarContext))
			{
				bool LSupportsSubSelect = LDevicePlan.IsSubSelectSupported();
				if (!LSupportsSubSelect)
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LDevicePlan.GetSubSelectNotSupportedReason(), APlanNode));
				LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsSubSelect;
				LDevicePlan.CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasSubSelectExpressions;
				LDevicePlan.PushQueryContext();
				LScalarContext = true;
			}  
			try
			{
				InstructionNodeBase LInstructionNode = APlanNode as InstructionNodeBase;
				if ((LInstructionNode != null) && (LInstructionNode.DataType != null) && (LInstructionNode.Operator != null))
				{
					bool LSupportsOperator = SupportsOperator(ADevicePlan.Plan, LInstructionNode.Operator) && SupportsOperands(LInstructionNode);
					if (!LSupportsOperator)
					{
						if 
						(
							(LDevicePlan.CurrentQueryContext().IsScalarContext) 
								&& (LInstructionNode.DataType is Schema.ScalarType) 
								&& (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)LInstructionNode.DataType) != null) 
								&& LInstructionNode.IsFunctional 
								&& LInstructionNode.IsRepeatable 
								&& LInstructionNode.IsContextLiteral(LDevicePlan.Stack.Count == 0 ? 0 : LDevicePlan.Stack.Count - 1)
						)
						{
							ServerStatementPlan LPlan = new ServerStatementPlan(ADevicePlan.Plan.ServerProcess);
							try
							{
								LPlan.Plan.PushStatementContext(LDevicePlan.Plan.StatementContext);
								LPlan.Plan.PushSecurityContext(LDevicePlan.Plan.SecurityContext);
								LPlan.Plan.PushCursorContext(LDevicePlan.Plan.CursorContext);
								if (LDevicePlan.Plan.InRowContext)
									LPlan.Plan.EnterRowContext();
								for (int LIndex = LDevicePlan.Plan.Symbols.Count - 1; LIndex >= 0; LIndex--)
									LPlan.Plan.Symbols.Push(LDevicePlan.Plan.Symbols.Peek(LIndex));
								try
								{
									PlanNode LNode = Compiler.CompileExpression(LPlan.Plan, new D4.Parser(true).ParseExpression(LInstructionNode.EmitStatementAsString()));
									LNode = Compiler.OptimizeNode(LPlan.Plan, LNode);
									LNode.InternalDetermineBinding(LPlan.Plan); // Don't use the compiler bind here because we already know a determine device call on the top level node will fail
									APlanNode.CouldSupport = true; // Set this to indicate that support could be provided if it would be beneficial to do so
									return FromScalar(LDevicePlan, LNode);
								}
								finally
								{
									while (LPlan.Plan.Symbols.Count > 0)
										LPlan.Plan.Symbols.Pop();
								}
							}
							finally
							{
								LPlan.Dispose();
							}
						}
						else
						{
							LDevicePlan.IsSupported = false;
							LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not contain an operator map for operator ""{0}"" with signature ""{1}"".", LInstructionNode.Operator.OperatorName, LInstructionNode.Operator.Signature.ToString()), APlanNode));
						}
					}
					else
					{
						TableNode LTableNode = APlanNode as TableNode;
						if (LTableNode != null)
							DetermineCursorBehavior(LDevicePlan.Plan, LTableNode);
						DeviceOperator LDeviceOperator = ResolveDeviceOperator(ADevicePlan.Plan, LInstructionNode.Operator);
						if (LDevicePlan.IsBooleanContext() && APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean) && !IsTruthValued(LDeviceOperator))
							return new BinaryExpression((Expression)LDeviceOperator.Translate(LDevicePlan, APlanNode), "iEqual", new ValueExpression(1));
						else
							return LDeviceOperator.Translate(LDevicePlan, APlanNode);
					}
					return new CallExpression();
				}
				else if ((APlanNode is AggregateCallNode) && (APlanNode.DataType != null) && ((AggregateCallNode)APlanNode).Operator != null)
				{
					AggregateCallNode LAggregateCallNode = (AggregateCallNode)APlanNode;
					bool LSupportsOperator = SupportsOperator(ADevicePlan.Plan, LAggregateCallNode.Operator);
					if (!LSupportsOperator)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have an operator map for the operator ""{0}"" with signature ""{1}"".", LAggregateCallNode.Operator.OperatorName, LAggregateCallNode.Operator.Signature.ToString()), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsOperator;
					if (LDevicePlan.IsSupported)
					{
						DeviceOperator LDeviceOperator = ResolveDeviceOperator(ADevicePlan.Plan, LAggregateCallNode.Operator);
						if (LDevicePlan.IsBooleanContext() && APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean) && !IsTruthValued(LDeviceOperator))
							return new BinaryExpression((Expression)LDeviceOperator.Translate(LDevicePlan, APlanNode), "iEqual", new ValueExpression(1));
						else
							return LDeviceOperator.Translate(LDevicePlan, APlanNode);
					}
					return new CallExpression();
				}
				else if (APlanNode is StackReferenceNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"" of variable reference ""{1}"".", APlanNode.DataType.Name, ((StackReferenceNode)APlanNode).Identifier), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						return TranslateStackReference((SQLDevicePlan)LDevicePlan, (StackReferenceNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is StackColumnReferenceNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"" of column reference ""{1}"".", APlanNode.DataType.Name, ((StackColumnReferenceNode)APlanNode).Identifier), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						return TranslateStackColumnReference((SQLDevicePlan)LDevicePlan, (StackColumnReferenceNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is ExtractColumnNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"" of column reference ""{1}"".", APlanNode.DataType.Name, ((ExtractColumnNode)APlanNode).Identifier), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						return TranslateExtractColumnNode((SQLDevicePlan)LDevicePlan, (ExtractColumnNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is ExtractRowNode)
				{
					return TranslateExtractRowNode(LDevicePlan, (ExtractRowNode)APlanNode);
				}
				else if (APlanNode is ValueNode)
				{
					bool LSupportsDataType = ((APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null)) || (APlanNode.DataType is Schema.GenericType);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", APlanNode.DataType.Name), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						return TranslateValueNode(LDevicePlan, (ValueNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is AsNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", APlanNode.DataType.Name), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						return TranslateAsNode(LDevicePlan, (AsNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is ConditionNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", APlanNode.DataType.Name), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						if (LDevicePlan.IsBooleanContext() && APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean))
							return new BinaryExpression((Expression)TranslateConditionNode(LDevicePlan, (ConditionNode)APlanNode), "iEqual", new ValueExpression(1));
						else
							return TranslateConditionNode(LDevicePlan, (ConditionNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is ConditionedCaseNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", APlanNode.DataType.Name), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						if (LDevicePlan.IsBooleanContext() && APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean))
							return new BinaryExpression((Expression)TranslateConditionedCaseNode(LDevicePlan, (ConditionedCaseNode)APlanNode), "iEqual", new ValueExpression(1));
						else
							return TranslateConditionedCaseNode(LDevicePlan, (ConditionedCaseNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is SelectedConditionedCaseNode)
				{
					bool LSupportsDataType = (APlanNode.DataType is Schema.ScalarType) && (ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType) != null);
					if (!LSupportsDataType)
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not have a type map for the type ""{0}"".", APlanNode.DataType.Name), APlanNode));
					LDevicePlan.IsSupported = LDevicePlan.IsSupported && LSupportsDataType;
					if (LDevicePlan.IsSupported)
						if (LDevicePlan.IsBooleanContext() && APlanNode.DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean))
							return new BinaryExpression((Expression)TranslateSelectedConditionedCaseNode(LDevicePlan, (SelectedConditionedCaseNode)APlanNode), "iEqual", new ValueExpression(1));
						else
							return TranslateSelectedConditionedCaseNode(LDevicePlan, (SelectedConditionedCaseNode)APlanNode);
					return new CallExpression();
				}
				else if (APlanNode is ConditionedCaseItemNode)
				{
					// Let the support walk up to the parent CaseNode for translation
					return new CallExpression();
				}
				else if (APlanNode is ListNode)
				{
					return TranslateListNode(LDevicePlan, (ListNode)APlanNode);
				}
				else if (APlanNode is TableSelectorNode)
				{
					DetermineCursorBehavior(LDevicePlan.Plan, (TableNode)APlanNode);
					return TranslateTableSelectorNode(LDevicePlan, (TableSelectorNode)APlanNode);
				}
				else if (APlanNode is RowSelectorNode)
					return TranslateRowSelectorNode(LDevicePlan, (RowSelectorNode)APlanNode);
				#if TranslateModifications
				else if (APlanNode is InsertNode)
					return TranslateInsertNode(LDevicePlan, (InsertNode)APlanNode);
				else if (APlanNode is UpdateNode)
					return TranslateUpdateNode(LDevicePlan, (UpdateNode)APlanNode);
				else if (APlanNode is DeleteNode)
					return TranslateDeleteNode(LDevicePlan, (DeleteNode)APlanNode);
				#endif
				else if (APlanNode is CreateTableNode)
					return TranslateCreateTableNode(LDevicePlan, (CreateTableNode)APlanNode);
				else if (APlanNode is AlterTableNode)
					return TranslateAlterTableNode(LDevicePlan, (AlterTableNode)APlanNode);
				else if (APlanNode is DropTableNode)
					return TranslateDropTableNode(LDevicePlan, (DropTableNode)APlanNode);
				else
				{
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support translation for nodes of type ""{0}"".", APlanNode.GetType().Name)));
					LDevicePlan.IsSupported = false;
					return new CallExpression();
				}
			}
			finally
			{
				if (LScalarContext)
					LDevicePlan.PopQueryContext();
			}
		}
        
		public Batch DeviceReconciliationScript(ServerProcess AProcess, ReconcileOptions AOptions)
		{
			Catalog LServerCatalog = GetServerCatalog(AProcess, null);
			return DeviceReconciliationScript(AProcess, LServerCatalog, GetDeviceCatalog(AProcess, LServerCatalog), AOptions);
		}
		
		public Batch DeviceReconciliationScript(ServerProcess AProcess, TableVar ATableVar, ReconcileOptions AOptions)
		{
			Catalog LServerCatalog = GetServerCatalog(AProcess, ATableVar);
			return DeviceReconciliationScript(AProcess, LServerCatalog, GetDeviceCatalog(AProcess, LServerCatalog, ATableVar), AOptions);
		}

		/// <summary>Produces a script to reconcile the given device catalog to the given server catalog, with the specified options.</summary>        
        public Batch DeviceReconciliationScript(ServerProcess AProcess, Catalog AServerCatalog, Catalog ADeviceCatalog, ReconcileOptions AOptions)
        {
			Batch LBatch = new Batch();
			foreach (Schema.Object LObject in AServerCatalog)
			{
				Schema.BaseTableVar LBaseTableVar = LObject as Schema.BaseTableVar;
				if (LBaseTableVar != null)
				{
					if (Convert.ToBoolean(D4.MetaData.GetTag(LBaseTableVar.MetaData, "Storage.ShouldReconcile", "true")))
					{	
						AlterTableNode LAlterTableNode = new AlterTableNode();
						using (Plan LPlan = new Plan(AProcess))
						{
							using (SQLDevicePlan LDevicePlan = new SQLDevicePlan(LPlan, this, LAlterTableNode))
							{
								int LObjectIndex = ADeviceCatalog.IndexOf(LBaseTableVar.Name);
								if (LObjectIndex < 0)
									LBatch.Statements.Add(TranslateCreateTable(LDevicePlan, LBaseTableVar));
								else
								{
									// Compile and translate the D4.AlterTableStatement returned from ReconcileTable and add it to LBatch
									bool LReconciliationRequired;
									D4.AlterTableStatement LD4AlterTableStatement = ReconcileTable(LPlan, LBaseTableVar, (Schema.TableVar)ADeviceCatalog[LObjectIndex], AOptions, out LReconciliationRequired);
									if (LReconciliationRequired)
									{
										LAlterTableNode.AlterTableStatement = LD4AlterTableStatement;
										LAlterTableNode.DetermineDevice(LDevicePlan.Plan);
										LBatch.Statements.Add(TranslateAlterTableNode(LDevicePlan, LAlterTableNode));
									}
								}
							}
						}
					}
				}
			}
			
			if ((AOptions & ReconcileOptions.ShouldDropTables) != 0)
			{
				foreach (Schema.Object LObject in ADeviceCatalog)
				{
					Schema.BaseTableVar LBaseTableVar = LObject as Schema.BaseTableVar;
					if ((LBaseTableVar != null) && !AServerCatalog.Contains(LBaseTableVar.Name))
					{
						DropTableNode LDropTableNode = new DropTableNode(LBaseTableVar);
						using (Plan LPlan = new Plan(AProcess))
						{
							using (SQLDevicePlan LDevicePlan = new SQLDevicePlan(LPlan, this, LDropTableNode))
							{
								LBatch.Statements.Add(TranslateDropTableNode(LDevicePlan, new DropTableNode(LBaseTableVar)));
							}
						}
					}
				}
			}
			
			return LBatch;
        }
        
		public override ErrorList Reconcile(ServerProcess AProcess, Catalog AServerCatalog, Catalog ADeviceCatalog)
		{
			ErrorList LErrors = base.Reconcile(AProcess, AServerCatalog, ADeviceCatalog);

			if ((ReconcileMaster == D4.ReconcileMaster.Server) || (ReconcileMaster == D4.ReconcileMaster.Both))
				foreach (Schema.Object LObject in AServerCatalog)
					if (LObject is Schema.BaseTableVar)
					{
						try
						{
							Schema.BaseTableVar LTableVar = (Schema.BaseTableVar)LObject;
							if (Convert.ToBoolean(D4.MetaData.GetTag(LTableVar.MetaData, "Storage.ShouldReconcile", "true")))
							{
								int LObjectIndex = ADeviceCatalog.IndexOf(LTableVar.Name);
								if (LObjectIndex < 0)
									CreateTable(AProcess, LTableVar, D4.ReconcileMaster.Server);
								else
									ReconcileTable(AProcess, LTableVar, (Schema.BaseTableVar)ADeviceCatalog[LObjectIndex], D4.ReconcileMaster.Server);
							}
						}
						catch (Exception LException)
						{
							LErrors.Add(LException);
						}
					}
			
			if ((ReconcileMaster == D4.ReconcileMaster.Device) || (ReconcileMaster == D4.ReconcileMaster.Both))
				foreach (Schema.Object LObject in ADeviceCatalog)
					if (LObject is Schema.BaseTableVar)
					{
						try
						{
							Schema.BaseTableVar LTableVar = (Schema.BaseTableVar)LObject;
							if (Convert.ToBoolean(D4.MetaData.GetTag(LTableVar.MetaData, "Storage.ShouldReconcile", "true")))
							{
								int LObjectIndex = AServerCatalog.IndexOf(LTableVar.Name);
								if ((LObjectIndex < 0) || (AServerCatalog[LObjectIndex].Library == null)) // Library will be null if this table was created only to specify a name for the table to reconcile
									CreateTable(AProcess, LTableVar, D4.ReconcileMaster.Device);
								else
									ReconcileTable(AProcess, (Schema.BaseTableVar)AServerCatalog[LObjectIndex], LTableVar, D4.ReconcileMaster.Device);
							}
						}
						catch (Exception LException)
						{
							LErrors.Add(LException);
						}
					}
					else if (LObject is Schema.Reference)
					{
						try
						{
							Schema.Reference LReference = (Schema.Reference)LObject;
							if (Convert.ToBoolean(D4.MetaData.GetTag(LReference.MetaData, "Storage.ShouldReconcile", "true")))
							{
								int LObjectIndex = AServerCatalog.IndexOf(LReference.Name);
								if (LObjectIndex < 0)
									CreateReference(AProcess, LReference, D4.ReconcileMaster.Device);
								else
									ReconcileReference(AProcess, (Schema.Reference)AServerCatalog[LObjectIndex], LReference, D4.ReconcileMaster.Device);
							}
						}
						catch (Exception LException)
						{
							LErrors.Add(LException);
						}
					}
					
			return LErrors;
		}
        
		public override void CreateTable(ServerProcess AProcess, TableVar ATableVar, D4.ReconcileMaster AMaster)
		{
			if (AMaster == D4.ReconcileMaster.Server)
			{
				using (Plan LPlan = new Plan(AProcess))
				{
					using (SQLDevicePlan LDevicePlan = new SQLDevicePlan(LPlan, this, null))
					{
						Statement LStatement = TranslateCreateTable(LDevicePlan, ATableVar);
						SQLDeviceSession LDeviceSession = (SQLDeviceSession)LPlan.DeviceConnect(this);
						{
							Batch LBatch = LStatement as Batch;
							if (LBatch != null)
							{
								foreach (Statement LSingleStatement in LBatch.Statements)
									LDeviceSession.Connection.Execute(Emitter.Emit(LSingleStatement));
							}
							else
								LDeviceSession.Connection.Execute(Emitter.Emit(LStatement));
						}
					}
				}
			}
			else if (AMaster == D4.ReconcileMaster.Device)
			{
				// Add the TableVar to the Catalog
				// Note that this does not call the usual CreateTable method because there is no need to request device storage.
				Plan LPlan = new Plan(AProcess);
				try
				{
					LPlan.PlanCatalog.Add(ATableVar);
					try
					{
						LPlan.PushCreationObject(ATableVar);
						try
						{
							CheckSupported(LPlan, ATableVar);
							if (!AProcess.ServerSession.Server.IsEngine)
								Compiler.CompileTableVarKeyConstraints(LPlan, ATableVar);
						}
						finally
						{
							LPlan.PopCreationObject();
						}
					}
					finally
					{
						LPlan.PlanCatalog.Remove(ATableVar);
					}
				}
				finally
				{
					LPlan.Dispose();
				}

				AProcess.CatalogDeviceSession.InsertCatalogObject(ATableVar);
			}
		}
		
		public virtual void CreateReference(ServerProcess AProcess, Reference AReference, D4.ReconcileMaster AMaster)
		{
			if (AMaster == D4.ReconcileMaster.Server)
			{
				Error.Fail("Reconciliation of foreign keys to the target device is not yet implemented");
			}
			else if (AMaster == D4.ReconcileMaster.Device)
			{
				using (Plan LPlan = new Plan(AProcess))
				{
					Program LProgram = new Program(AProcess);
					LProgram.Code = Compiler.CompileCreateReferenceStatement(LPlan, AReference.EmitStatement(D4.EmitMode.ForCopy));
					LProgram.Execute(null);
				}
			}
		}
		
		public class SchemaLevelDropColumnDefinition : D4.DropColumnDefinition
		{
			public SchemaLevelDropColumnDefinition(Schema.TableVarColumn AColumn) : base()
			{
				FColumn = AColumn;
			}
			
			private Schema.TableVarColumn FColumn;
			public Schema.TableVarColumn Column { get { return FColumn; } }
		}
		
		public class SchemaLevelDropKeyDefinition : D4.DropKeyDefinition
		{
			public SchemaLevelDropKeyDefinition(Schema.Key AKey) : base()
			{
				FKey = AKey;
			}

			private Schema.Key FKey;
			public Schema.Key Key { get { return FKey; } }
		}
		
		public class SchemaLevelDropOrderDefinition : D4.DropOrderDefinition
		{
			public SchemaLevelDropOrderDefinition(Schema.Order AOrder) : base()
			{
				FOrder = AOrder;
			}
			
			private Schema.Order FOrder;
			public Schema.Order Order { get { return FOrder; } }
		}
		
		public virtual D4.AlterTableStatement ReconcileTable(Plan APlan, TableVar ASourceTableVar, TableVar ATargetTableVar, ReconcileOptions AOptions, out bool AReconciliationRequired)
		{
			AReconciliationRequired = false;
			D4.AlterTableStatement LStatement = new D4.AlterTableStatement();
			LStatement.TableVarName = ATargetTableVar.Name;
			
			// Ensure ASourceTableVar.Columns is a subset of ATargetTableVar.Columns
			foreach (Schema.TableVarColumn LColumn in ASourceTableVar.Columns)
				if (Convert.ToBoolean(D4.MetaData.GetTag(LColumn.MetaData, "Storage.ShouldReconcile", "true")))
				{
					int LTargetIndex = ColumnIndexFromNativeColumnName(ATargetTableVar, ToSQLIdentifier(LColumn));
					if (LTargetIndex < 0)
					{
						// Add the column to the target table var
						LStatement.CreateColumns.Add(LColumn.EmitStatement(D4.EmitMode.ForCopy));
						AReconciliationRequired = true;
					}
					else
					{
						if ((AOptions & ReconcileOptions.ShouldReconcileColumns) != 0)
						{
							Schema.TableVarColumn LTargetColumn = ATargetTableVar.Columns[LTargetIndex];
							
							// Type of the column (Needs to be based on domain name because the reconciliation process will only create the column with the D4 type map for the native domain)
							SQLScalarType LSourceType = (SQLScalarType)ResolveDeviceScalarType(APlan, (Schema.ScalarType)LColumn.DataType);
							SQLScalarType LTargetType = (SQLScalarType)ResolveDeviceScalarType(APlan, (Schema.ScalarType)LTargetColumn.DataType);
							bool LDomainsDifferent = 
								(LSourceType.DomainName(LColumn) != LTargetType.DomainName(LTargetColumn))
									|| (LSourceType.NativeDomainName(LColumn) != LTargetType.NativeDomainName(LTargetColumn));

							// Nilability of the column
							bool LNilabilityDifferent = LColumn.IsNilable != LTargetColumn.IsNilable;
							
							if (LDomainsDifferent || LNilabilityDifferent)
							{
								D4.AlterColumnDefinition LAlterColumnDefinition = new D4.AlterColumnDefinition();
								LAlterColumnDefinition.ColumnName = LTargetColumn.Name;
								LAlterColumnDefinition.ChangeNilable = LNilabilityDifferent;
								LAlterColumnDefinition.IsNilable = LColumn.IsNilable;
								if (LDomainsDifferent)
									LAlterColumnDefinition.TypeSpecifier = LColumn.DataType.EmitSpecifier(D4.EmitMode.ForCopy);
								LStatement.AlterColumns.Add(LAlterColumnDefinition);
								AReconciliationRequired = true;
							}
						}
					}
				}
			
			// Ensure ATargetTableVar.Columns is a subset of ASourceTableVar.Columns
			if ((AOptions & ReconcileOptions.ShouldDropColumns) != 0)
				foreach (Schema.TableVarColumn LColumn in ATargetTableVar.Columns)
					if (!ASourceTableVar.Columns.ContainsName(LColumn.Name))
					{
						LStatement.DropColumns.Add(new SchemaLevelDropColumnDefinition(LColumn));
						AReconciliationRequired = true;
					}
				
			// Ensure All keys and orders have supporting indexes
			// TODO: Replace an imposed key if a new key is added
			foreach (Schema.Key LKey in ASourceTableVar.Keys)
				if (Convert.ToBoolean(D4.MetaData.GetTag(LKey.MetaData, "Storage.ShouldReconcile", "true")))
					if (!ATargetTableVar.Keys.Contains(LKey))
					{
						// Add the key to the target table var
						LStatement.CreateKeys.Add(LKey.EmitStatement(D4.EmitMode.ForCopy));
						AReconciliationRequired = true;
					}
					else
					{
						// TODO: Key level reconciliation
					}

			// Ensure ATargetTableVar.Keys is a subset of ASourceTableVar.Keys
			if ((AOptions & ReconcileOptions.ShouldDropKeys) != 0)
				foreach (Schema.Key LKey in ATargetTableVar.Keys)
					if (!ASourceTableVar.Keys.Contains(LKey) && !Convert.ToBoolean(D4.MetaData.GetTag(LKey.MetaData, "Storage.IsImposedKey", "false")))
					{
						LStatement.DropKeys.Add(new SchemaLevelDropKeyDefinition(LKey));
						AReconciliationRequired = true;
					}
				
			foreach (Schema.Order LOrder in ASourceTableVar.Orders)
				if (Convert.ToBoolean(D4.MetaData.GetTag(LOrder.MetaData, "Storage.ShouldReconcile", "true")))
					if (!(ATargetTableVar.Orders.Contains(LOrder)))
					{
						// Add the key to the target table var
						LStatement.CreateOrders.Add(LOrder.EmitStatement(D4.EmitMode.ForCopy));
						AReconciliationRequired = true;
					}
					else
					{
						// TODO: Order level reconciliation
					}
			
			// Ensure ATargetTableVar.Orders is a subset of ASourceTableVar.Orders
			if ((AOptions & ReconcileOptions.ShouldDropOrders) != 0)
				foreach (Schema.Order LOrder in ATargetTableVar.Orders)
					if (!ASourceTableVar.Orders.Contains(LOrder))
					{
						LStatement.DropOrders.Add(new SchemaLevelDropOrderDefinition(LOrder));
						AReconciliationRequired = true;
					}

			return LStatement;
		}
        
		public override void ReconcileTable(ServerProcess AProcess, TableVar AServerTableVar, TableVar ADeviceTableVar, D4.ReconcileMaster AMaster)
		{
			if ((AMaster == D4.ReconcileMaster.Server) || (AMaster == D4.ReconcileMaster.Both))
			{
				using (Plan LPlan = new Plan(AProcess))
				{
					bool LReconciliationRequired;
					D4.AlterTableStatement LStatement = ReconcileTable(LPlan, AServerTableVar, ADeviceTableVar, ReconcileOptions.All, out LReconciliationRequired);
					if (LReconciliationRequired)
					{
						D4.ReconcileMode LSaveMode = ReconcileMode;
						try
						{
							ReconcileMode = D4.ReconcileMode.None; // turn off reconciliation to avoid a command being re-issued to the target system
							Program LProgram = new Program(AProcess);
							LProgram.Code = Compiler.Compile(LPlan, LStatement);
							LPlan.CheckCompiled();
							LProgram.Start(null);
							try
							{
								LProgram.DeviceExecute(this, LProgram.Code);
							}
							finally
							{
								LProgram.Stop(null);
							}
						}
						finally
						{
							ReconcileMode = LSaveMode;
						}
					}
				}
			}
			
			if ((AMaster == D4.ReconcileMaster.Device) || (AMaster == D4.ReconcileMaster.Both))
			{
				using (Plan LPlan = new Plan(AProcess))
				{
					bool LReconciliationRequired;
					D4.AlterTableStatement LStatement = ReconcileTable(LPlan, ADeviceTableVar, AServerTableVar, ReconcileOptions.None, out LReconciliationRequired);
					if (LReconciliationRequired)
					{
						D4.ReconcileMode LSaveMode = ReconcileMode;
						try
						{
							ReconcileMode = D4.ReconcileMode.None; // turn off reconciliation to avoid a command being re-issued to the target system
							Program LProgram = new Program(AProcess);
							LProgram.Code = Compiler.Compile(LPlan, LStatement);
							LPlan.CheckCompiled();
							LProgram.Execute(null);
						}
						finally
						{
							ReconcileMode = LSaveMode;
						}
					}
				}
			}
		}
		
		public virtual void ReconcileReference(ServerProcess AProcess, Reference AServerReference, Reference ADeviceReference, D4.ReconcileMaster AMaster)
		{
			if ((AMaster == D4.ReconcileMaster.Server) || (AMaster == D4.ReconcileMaster.Both))
			{
				// TODO: Reference reconciliation
			}
			
			if ((AMaster == D4.ReconcileMaster.Device) || (AMaster == D4.ReconcileMaster.Both))
			{
				// TODO: Reference reconciliation
			}
		}
        
		public virtual bool ShouldIncludeColumn(Plan APlan, string ATableName, string AColumnName, string ADomainName)
		{
			return true;
		}
        
		public virtual ScalarType FindScalarType(Plan APlan, string ADomainName, int ALength, D4.MetaData AMetaData)
		{
			throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomainName);
		}
		
		public virtual ScalarType FindScalarType(Plan APlan, SQLDomain ADomain)
		{
			if (ADomain.Type.Equals(typeof(bool))) return APlan.DataTypes.SystemBoolean;
			else if (ADomain.Type.Equals(typeof(byte))) return APlan.DataTypes.SystemByte;
			else if (ADomain.Type.Equals(typeof(short))) return APlan.DataTypes.SystemShort;
			else if (ADomain.Type.Equals(typeof(int))) return APlan.DataTypes.SystemInteger;
			else if (ADomain.Type.Equals(typeof(long))) return APlan.DataTypes.SystemLong;
			else if (ADomain.Type.Equals(typeof(decimal))) return APlan.DataTypes.SystemDecimal;
			else if (ADomain.Type.Equals(typeof(float))) return APlan.DataTypes.SystemDecimal;
			else if (ADomain.Type.Equals(typeof(double))) return APlan.DataTypes.SystemDecimal;
			else if (ADomain.Type.Equals(typeof(string))) return APlan.DataTypes.SystemString;
			else if (ADomain.Type.Equals(typeof(byte[]))) return APlan.DataTypes.SystemBinary;
			else if (ADomain.Type.Equals(typeof(Guid))) return APlan.DataTypes.SystemGuid;
			else if (ADomain.Type.Equals(typeof(DateTime))) return APlan.DataTypes.SystemDateTime;
			else if (ADomain.Type.Equals(typeof(TimeSpan))) return APlan.DataTypes.SystemTimeSpan;
			else throw new SQLException(SQLException.Codes.UnsupportedImportType, ADomain.Type.Name);
		}
        
		private void ConfigureTableVar(Plan APlan, TableVar ATableVar, Objects AColumns, Catalog ACatalog)
		{
			if (ATableVar != null)
			{
				ATableVar.DataType = new TableType();

				foreach (TableVarColumn LColumn in AColumns)
				{
					ATableVar.Columns.Add(LColumn);
					ATableVar.DataType.Columns.Add(LColumn.Column);
				}
				
				ACatalog.Add(ATableVar);
			}
		}
		
		private void AttachKeyOrOrder(Plan APlan, TableVar ATableVar, ref Key AKey, ref Order AOrder)
		{
			if (AKey != null)
			{
				if (!ATableVar.Keys.Contains(AKey) && (AKey.Columns.Count > 0))
					ATableVar.Keys.Add(AKey);
				AKey = null;
			}
			else if (AOrder != null)
			{
				if (!ATableVar.Orders.Contains(AOrder) && (AOrder.Columns.Count > 0))
					ATableVar.Orders.Add(AOrder);
				AOrder = null;
			}
		}
		
		private string FDeviceTablesExpression = String.Empty;
		public string DeviceTablesExpression
		{
			get { return FDeviceTablesExpression; }
			set { FDeviceTablesExpression = value == null ? String.Empty : value; }
		}
		
		protected virtual string GetDeviceTablesExpression(TableVar ATableVar)
		{
			return
				String.Format
				(
					DeviceTablesExpression,
					ATableVar == null ? String.Empty : ToSQLIdentifier(ATableVar)
				);
		}
		
		protected virtual string GetTitleFromName(string AObjectName)
		{
			StringBuilder LColumnTitle = new StringBuilder();
			bool LFirstChar = true;
			for (int LIndex = 0; LIndex < AObjectName.Length; LIndex++)
				if (Char.IsLetterOrDigit(AObjectName, LIndex))
					if (LFirstChar)
					{
						LFirstChar = false;
						LColumnTitle.Append(Char.ToUpper(AObjectName[LIndex]));
					}
					else
						LColumnTitle.Append(Char.ToLower(AObjectName[LIndex]));
				else
					if (!LFirstChar)
					{
						LColumnTitle.Append(" ");
						LFirstChar = true;
					}

			return LColumnTitle.ToString();
		}
		
		protected string GetServerTableName(Plan APlan, Catalog AServerCatalog, string ADeviceTableName)
		{
			string LServerTableName = FromSQLIdentifier(ADeviceTableName);
			List<string> LNames = new List<string>();
			int LIndex = AServerCatalog.IndexOf(LServerTableName, LNames);
			if ((LIndex >= 0) && (AServerCatalog[LIndex].Library != null) && (D4.MetaData.GetTag(AServerCatalog[LIndex].MetaData, "Storage.Name", ADeviceTableName) == ADeviceTableName))
				LServerTableName = AServerCatalog[LIndex].Name;
			else
			{
				// if LNames is populated, all but one of them must have a Storage.Name tag specifying a different name for the table
				bool LFound = false;
				foreach (string LName in LNames)
				{
					TableVar LTableVar = (TableVar)AServerCatalog[AServerCatalog.IndexOfName(LName)];
					if (LTableVar.Library != null)
						if (D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Name", ADeviceTableName) == ADeviceTableName)
						{
							LServerTableName = LTableVar.Name;
							LFound = true;
						}
				}
				
				// search for a table with ADeviceTableName as it's Storage.Name tag
				if (!LFound)
					foreach (TableVar LTableVar in AServerCatalog)
						if (LTableVar.Library != null)
							if (D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Name", "") == ADeviceTableName)
							{
								LServerTableName = LTableVar.Name;
								LFound = true;
								break;
							}
					
				if (!LFound)
					LServerTableName = DAE.Schema.Object.Qualify(LServerTableName, APlan.CurrentLibrary.Name);
			}

			return LServerTableName;
		}
		
		public int ColumnIndexFromNativeColumnName(TableVar AExistingTableVar, string ANativeColumnName)
		{
			if (AExistingTableVar != null)
				for (int LIndex = 0; LIndex < AExistingTableVar.Columns.Count; LIndex++)
					if (D4.MetaData.GetTag(AExistingTableVar.Columns[LIndex].MetaData, "Storage.Name", "") == ANativeColumnName)
						return LIndex;
			return -1;
		}
		
		public unsafe virtual void GetDeviceTables(Plan APlan, Catalog AServerCatalog, Catalog ADeviceCatalog, TableVar ATableVar)
		{
			string LDeviceTablesExpression = GetDeviceTablesExpression(ATableVar);
			if (LDeviceTablesExpression != String.Empty)
			{
				SQLCursor LCursor = ((SQLDeviceSession)APlan.DeviceConnect(this)).Connection.Open(LDeviceTablesExpression);
				try
				{
					string LTableName = String.Empty;
					BaseTableVar LTableVar = null;
					BaseTableVar LExistingTableVar = null;
					Objects LColumns = new Objects();
					
					while (LCursor.Next())
					{
						if (LTableName != (string)LCursor[1])
						{
							ConfigureTableVar(APlan, LTableVar, LColumns, ADeviceCatalog);

							LTableName = (string)LCursor[1];
							
							// Search for a table with this name unqualified in the server catalog
							string LTableTitle = (string)LCursor[4];
							LTableVar = new BaseTableVar(GetServerTableName(APlan, AServerCatalog, LTableName), null);
							LTableVar.Owner = APlan.User;
							LTableVar.Library = APlan.CurrentLibrary;
							LTableVar.Device = this;
							LTableVar.MetaData = new D4.MetaData();
							LTableVar.MetaData.Tags.Add(new D4.Tag("Storage.Name", LTableName, true));
							LTableVar.MetaData.Tags.Add(new D4.Tag("Storage.Schema", (string)LCursor[0], true));
							
							// if this table is already present in the server catalog, use StorageNames to map the columns
							int LExistingTableIndex = AServerCatalog.IndexOfName(LTableVar.Name);
							if (LExistingTableIndex >= 0)
								LExistingTableVar = AServerCatalog[LExistingTableIndex] as BaseTableVar;
							else
								LExistingTableVar = null;
							
							if (LTableTitle == LTableName)
								LTableTitle = GetTitleFromName(LTableName);
							LTableVar.MetaData.Tags.Add(new D4.Tag("Frontend.Title", LTableTitle));
							LTableVar.AddDependency(this);
							LColumns = new Objects();
						}
						
						string LNativeColumnName = (string)LCursor[2];
						string LColumnName = FromSQLIdentifier(LNativeColumnName);
						string LColumnTitle = (string)LCursor[5];
						string LNativeDomainName = (string)LCursor[6];
						string LDomainName = (string)LCursor[7];
						int LLength = Convert.ToInt32(LCursor[8]);
						
						int LExistingColumnIndex = ColumnIndexFromNativeColumnName(LExistingTableVar, LNativeColumnName);
						if (LExistingColumnIndex >= 0)
							LColumnName = LExistingTableVar.Columns[LExistingColumnIndex].Name;
						
						if (ShouldIncludeColumn(APlan, LTableName, LColumnName, LNativeDomainName))
						{
							D4.MetaData LMetaData = new D4.MetaData();
							TableVarColumn LColumn =
								new TableVarColumn
								(
									new Column(LColumnName, FindScalarType(APlan, LNativeDomainName, LLength, LMetaData)),
									LMetaData, 
									TableVarColumnType.Stored
								);
								
							LColumn.MetaData.Tags.Add(new D4.Tag("Storage.Name", LNativeColumnName));
							if (LNativeDomainName != LDomainName)
								LColumn.MetaData.Tags.Add(new D4.Tag("Storage.DomainName", LDomainName));
							
							if (LColumn.MetaData.Tags.Contains("Storage.Length"))
							{
								LColumn.MetaData.Tags.Add(new D4.Tag("Frontend.Width", Math.Min(Math.Max(3, LLength), 40).ToString()));
								if (LLength >= 0) // A (n)varchar(max) column in Microsoft SQL Server will reconcile with length -1
									LColumn.Constraints.Add(Compiler.CompileTableVarColumnConstraint(APlan, LTableVar, LColumn, new D4.ConstraintDefinition("LengthValid", new BinaryExpression(new CallExpression("Length", new Expression[]{new IdentifierExpression(D4.Keywords.Value)}), D4.Instructions.InclusiveLess, new ValueExpression(LLength)), null)));
							}
							
							if (LColumnTitle == LNativeColumnName)
								LColumnTitle = GetTitleFromName(LNativeColumnName);
								
							LColumn.MetaData.Tags.AddOrUpdate("Frontend.Title", LColumnTitle);
								
							if (Convert.ToInt32(LCursor[9]) != 0)
								LColumn.IsNilable = true;

							if (Convert.ToInt32(LCursor[10]) != 0)
								LColumn.MetaData.Tags.AddOrUpdate("Storage.Deferred", "true");
								
							LColumns.Add(LColumn);
							LTableVar.Dependencies.Ensure((Schema.ScalarType)LColumn.DataType);
						}
					}	// while

					ConfigureTableVar(APlan, LTableVar, LColumns, ADeviceCatalog);
				}
				catch (Exception E)
				{
					throw new SQLException(SQLException.Codes.ErrorReadingDeviceTables, E);
				}
				finally
				{
					LCursor.Command.Connection.Close(LCursor);
				}
			}
		}

		private string FDeviceIndexesExpression = String.Empty;
		public string DeviceIndexesExpression
		{
			get { return FDeviceIndexesExpression; }
			set { FDeviceIndexesExpression = value == null ? String.Empty : value; }
		}
		
		protected virtual string GetDeviceIndexesExpression(TableVar ATableVar)
		{
			return
				String.Format
				(
					DeviceIndexesExpression,
					(ATableVar == null) ? String.Empty : ToSQLIdentifier(ATableVar)
				);
		}
		
		public virtual void GetDeviceIndexes(Plan APlan, Catalog AServerCatalog, Catalog ADeviceCatalog, TableVar ATableVar)
		{
			string LDeviceIndexesExpression = GetDeviceIndexesExpression(ATableVar);
			if (LDeviceIndexesExpression != String.Empty)
			{
				SQLCursor LCursor = ((SQLDeviceSession)APlan.DeviceConnect(this)).Connection.Open(LDeviceIndexesExpression);
				try
				{
					string LTableName = String.Empty;
					string LIndexName = String.Empty;
					BaseTableVar LTableVar = null;
					Key LKey = null;
					Order LOrder = null;
					OrderColumn LOrderColumn;
					int LColumnIndex = -1;
					bool LShouldIncludeIndex = true;
					
					while (LCursor.Next())
					{
						if (LTableName != (string)LCursor[1])
						{
							if ((LTableVar != null) && LShouldIncludeIndex)
								AttachKeyOrOrder(APlan, LTableVar, ref LKey, ref LOrder);
							else
							{
								LKey = null;
								LOrder = null;
							}
							
							LTableName = (string)LCursor[1];
							LTableVar = (BaseTableVar)ADeviceCatalog[GetServerTableName(APlan, AServerCatalog, LTableName)];
							LIndexName = (string)LCursor[2];
							D4.MetaData LMetaData = new D4.MetaData();
							LMetaData.Tags.Add(new D4.Tag("Storage.Name", LIndexName, true));
							if (Convert.ToInt32(LCursor[5]) != 0)
								LKey = new Key(LMetaData);
							else
								LOrder = new Order(LMetaData);
							LShouldIncludeIndex = true;
						}
						
						if (LIndexName != (string)LCursor[2])
						{
							if (LShouldIncludeIndex)
								AttachKeyOrOrder(APlan, LTableVar, ref LKey, ref LOrder);
							else
							{
								LKey = null;
								LOrder = null;
							}

							LIndexName = (string)LCursor[2];
							D4.MetaData LMetaData = new D4.MetaData();
							LMetaData.Tags.Add(new D4.Tag("Storage.Name", LIndexName, true));
							if (Convert.ToInt32(LCursor[5]) != 0)
								LKey = new Key(LMetaData);
							else
								LOrder = new Order(LMetaData);
							LShouldIncludeIndex = true;
						}
						
						if (LKey != null)
						{
							LColumnIndex = ColumnIndexFromNativeColumnName(LTableVar, (string)LCursor[3]);
							if (LColumnIndex >= 0)
								LKey.Columns.Add(LTableVar.Columns[LColumnIndex]);
							else
								LShouldIncludeIndex = false;
						}
						else
						{
							LColumnIndex = ColumnIndexFromNativeColumnName(LTableVar, (string)LCursor[3]);
							if (LColumnIndex >= 0)
							{
								LOrderColumn = new OrderColumn(LTableVar.Columns[LColumnIndex], Convert.ToInt32(LCursor[6]) == 0);
								LOrderColumn.Sort = Compiler.GetSort(APlan, LOrderColumn.Column.DataType);
								LOrderColumn.IsDefaultSort = true;
								LOrder.Columns.Add(LOrderColumn);
							}
							else
								LShouldIncludeIndex = false;
						}
					}
					
					if (LShouldIncludeIndex)
						AttachKeyOrOrder(APlan, LTableVar, ref LKey, ref LOrder);
				}
				catch (Exception E)
				{
					throw new SQLException(SQLException.Codes.ErrorReadingDeviceIndexes, E);
				}
				finally
				{
					LCursor.Command.Connection.Close(LCursor);
				}
			}
		}

		private string FDeviceForeignKeysExpression = String.Empty;
		public string DeviceForeignKeysExpression
		{
			get { return FDeviceForeignKeysExpression; }
			set { FDeviceForeignKeysExpression = value == null ? String.Empty : value; }
		}
		
		protected virtual string GetDeviceForeignKeysExpression(TableVar ATableVar)
		{
			return
				String.Format
				(
					DeviceForeignKeysExpression,
					(ATableVar == null) ? String.Empty : ToSQLIdentifier(ATableVar)
				);
		}
		
		public virtual void GetDeviceForeignKeys(Plan APlan, Catalog AServerCatalog, Catalog ADeviceCatalog, TableVar ATableVar)
		{
			string LDeviceForeignKeysExpression = GetDeviceForeignKeysExpression(ATableVar);
			if (LDeviceForeignKeysExpression != String.Empty)
			{
				SQLCursor LCursor = ((SQLDeviceSession)APlan.DeviceConnect(this)).Connection.Open(LDeviceForeignKeysExpression);
				try
				{
					string LConstraintName = String.Empty;
					TableVar LSourceTableVar = null;
					TableVar LTargetTableVar = null;
					Reference LReference = null;
					int LColumnIndex;
					bool LShouldIncludeReference = true;
					
					while (LCursor.Next())
					{
						if (LConstraintName != (string)LCursor[1])
						{
							if ((LReference != null) && LShouldIncludeReference)
							{
								ADeviceCatalog.Add(LReference);
								LReference = null;
							}
							
							LConstraintName = (string)LCursor[1];
							string LSourceTableName = GetServerTableName(APlan, AServerCatalog, (string)LCursor[3]);
							string LTargetTableName = GetServerTableName(APlan, AServerCatalog, (string)LCursor[6]);
							if (ADeviceCatalog.Contains(LSourceTableName))
								LSourceTableVar = (TableVar)ADeviceCatalog[LSourceTableName];
							else
								LSourceTableVar = null;
								
							if (ADeviceCatalog.Contains(LTargetTableName))
								LTargetTableVar = (TableVar)ADeviceCatalog[LTargetTableName];
							else
								LTargetTableVar = null;
							
							LShouldIncludeReference = (LSourceTableVar != null) && (LTargetTableVar != null);
							if (LShouldIncludeReference)
							{
								D4.MetaData LMetaData = new D4.MetaData();
								LMetaData.Tags.Add(new D4.Tag("Storage.Name", LConstraintName, true));
								LMetaData.Tags.Add(new D4.Tag("Storage.Schema", (string)LCursor[0], true));
								LMetaData.Tags.Add(new D4.Tag("DAE.Enforced", "false", true));
								LReference = new Schema.Reference(DAE.Schema.Object.Qualify(FromSQLIdentifier(LConstraintName), DAE.Schema.Object.Qualifier(LSourceTableVar.Name)));
								LReference.MergeMetaData(LMetaData);
								LReference.Owner = APlan.User;
								LReference.Library = APlan.CurrentLibrary;
								LReference.SourceTable = LSourceTableVar;
								LReference.TargetTable = LTargetTableVar;
								LReference.Enforced = false;
							}
						}
						
						if (LReference != null)
						{
							LColumnIndex = LSourceTableVar.Columns.IndexOf(FromSQLIdentifier((string)LCursor[4]));
							if (LColumnIndex >= 0)
								LReference.SourceKey.Columns.Add(LSourceTableVar.Columns[LColumnIndex]);
							else
								LShouldIncludeReference = false;
								
							LColumnIndex = LTargetTableVar.Columns.IndexOf(FromSQLIdentifier((string)LCursor[7]));
							if (LColumnIndex >= 0)
								LReference.TargetKey.Columns.Add(LTargetTableVar.Columns[LColumnIndex]);
							else
								LShouldIncludeReference = false;
						}
					}
					
					if ((LReference != null) && LShouldIncludeReference)
						ADeviceCatalog.Add(LReference);
				}
				catch (Exception E)
				{
					throw new SQLException(SQLException.Codes.ErrorReadingDeviceForeignKeys, E);
				}
				finally
				{
					LCursor.Command.Connection.Close(LCursor);
				}
			}
		}

		public override Catalog GetDeviceCatalog(ServerProcess AProcess, Catalog AServerCatalog, TableVar ATableVar)
		{
			Catalog LCatalog = base.GetDeviceCatalog(AProcess, AServerCatalog, ATableVar);

			using (Plan LPlan = new Plan(AProcess))
			{
				GetDeviceTables(LPlan, AServerCatalog, LCatalog, ATableVar);
				GetDeviceIndexes(LPlan, AServerCatalog, LCatalog, ATableVar);
				GetDeviceForeignKeys(LPlan, AServerCatalog, LCatalog, ATableVar);
				
				// Impose a key on each table if one is not defined by the device
				foreach (Schema.Object LObject in LCatalog)
					if ((LObject is TableVar) && (((TableVar)LObject).Keys.Count == 0))
					{
						TableVar LTableVar = (TableVar)LObject;
						Key LKey = new Key();
						foreach (TableVarColumn LColumn in LTableVar.Columns)
							if (!Convert.ToBoolean(D4.MetaData.GetTag(LColumn.MetaData, "Storage.Deferred", "false")))
								LKey.Columns.Add(LColumn);
						LKey.IsGenerated = true;
						LKey.MetaData = new D4.MetaData();
						LKey.MetaData.Tags.Add(new D4.Tag("Storage.IsImposedKey", "true"));
						LTableVar.Keys.Add(LKey);
					}
			}

			return LCatalog;
		}

		private string FConnectionClass = String.Empty;
		public string ConnectionClass
		{
			get { return FConnectionClass; }
			set { FConnectionClass = value == null ? String.Empty : value; }
		}

		private string FConnectionStringBuilderClass = String.Empty;
		public string ConnectionStringBuilderClass
		{
			get { return FConnectionStringBuilderClass; }
			set { FConnectionStringBuilderClass = value == null ? String.Empty : value; }
		}

		public void GetConnectionParameters (D4.Tags ATags, Schema.DeviceSessionInfo ADeviceSessionInfo)
		{
			StringToTags(ConnectionParameters, ATags);
			StringToTags(ADeviceSessionInfo.ConnectionParameters, ATags);
		}
		
		public static string TagsToString(Language.D4.Tags ATags)
		{
			StringBuilder LResult = new StringBuilder();
			#if USEHASHTABLEFORTAGS
			foreach (D4.Tag LTag in ATags)
			{
			#else
			D4.Tag LTag;
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
			{
				LTag = ATags[LIndex];
			#endif
				if (LResult.Length > 0)
					LResult.Append(";");
				LResult.AppendFormat("{0}={1}", LTag.Name, LTag.Value);
			}
			return LResult.ToString();
		}

		public static void StringToTags(String AString, Language.D4.Tags ATags)
		{
			if (AString != null && AString != "")
			{
				string[] LArray = AString.Split(';');
				for (int LIndex = 0; LIndex < LArray.Length; LIndex++)
				{
					string[] LTempArray = LArray[LIndex].Split('=');
					if (LTempArray.Length != 2)
						throw new Schema.SchemaException(DAE.Schema.SchemaException.Codes.InvalidConnectionString);
					ATags.AddOrUpdate(LTempArray[0], LTempArray[1]);
				}
			}
		}

		/// <summary>Determines the default buffer size for SQLDeviceCursors created for this device.</summary>
		/// <remarks>A value of 0 for this property indicates that the SQLDeviceCursors should be disconnected, i.e. the entire result set will be cached by the device on open.</remarks>
		private int FConnectionBufferSize = SQLDeviceCursor.CDefaultBufferSize;
		public int ConnectionBufferSize
		{
			get { return FConnectionBufferSize; }
			set { FConnectionBufferSize = value; }
		}
	}
	
	public class SQLConnectionHeader : Disposable
	{
		public SQLConnectionHeader(SQLConnection AConnection) : base() 
		{
			FConnection = AConnection;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FConnection != null)
			{
				FConnection.Dispose();
				FConnection = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		private SQLConnection FConnection;
		public SQLConnection Connection { get { return FConnection; } }
		
		private SQLDeviceCursor FDeviceCursor;
		public SQLDeviceCursor DeviceCursor 
		{ 
			get { return FDeviceCursor; } 
			set { FDeviceCursor = value; }
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
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].Connection.State == SQLConnectionState.Idle)
				{
					Move(LIndex, Count - 1);
					return this[Count - 1];
				}
			return null;
		}
		
		public int IndexOfConnection(SQLConnection AConnection)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (System.Object.ReferenceEquals(this[LIndex].Connection, AConnection))
					return LIndex;
			return -1;
		}
	}
	
	public class SQLJoinContext : System.Object
	{
		public SQLJoinContext(SQLQueryContext ALeftQueryContext, SQLQueryContext ARightQueryContext) : base()
		{
			LeftQueryContext = ALeftQueryContext;
			RightQueryContext = ARightQueryContext;
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
		public SQLRangeVarColumn(TableVarColumn ATableVarColumn, string ATableAlias, string AColumnName)
		{
			FTableVarColumn = ATableVarColumn;
			TableAlias = ATableAlias;
			ColumnName = AColumnName;
			Alias = ColumnName;
		}
		
		public SQLRangeVarColumn(TableVarColumn ATableVarColumn, string ATableAlias, string AColumnName, string AAlias)
		{
			FTableVarColumn = ATableVarColumn;
			TableAlias = ATableAlias;
			ColumnName = AColumnName;
			Alias = AAlias;
		}
		
		public SQLRangeVarColumn(TableVarColumn ATableVarColumn, Expression AExpression, string AAlias)
		{
			FTableVarColumn = ATableVarColumn;
			FExpression = AExpression;
			Alias = AAlias;
		}
		
		private TableVarColumn FTableVarColumn;
		/// <summary>The table var column in the D4 expression.  This is the unique identifier for the range var column.</summary>
		public TableVarColumn TableVarColumn { get { return FTableVarColumn; } }
		
		private string FTableAlias = String.Empty;
		/// <summary>The name of the range var containing the column in the current query context.  If this is empty, the expression will be specified, and vice versa.</summary>
		public string TableAlias
		{
			get { return FTableAlias; }
			set { FTableAlias = value == null ? String.Empty : value; }
		}
		
		private string FColumnName = String.Empty;
		/// <summary>The name of the column in the table in the target system.  If the column name is specified, the expression will be null, and vice versa.</summary>
		public string ColumnName
		{
			get { return FColumnName; }
			set 
			{ 
				FColumnName = value == null ? String.Empty : value; 
				if (FColumnName != String.Empty)
					FExpression = null;
			}
		}
		
		private Expression FExpression;
		/// <summary>Expression is the expression used to define this column. Will be null if this is a base column reference.</summary>
		public Expression Expression
		{
			get { return FExpression; }
			set 
			{ 
				FExpression = value; 
				if (FExpression != null)
				{
					FTableAlias = String.Empty;
					FColumnName = String.Empty;
				}
			}
		}
		
		private string FAlias = String.Empty;
		/// <summary>The alias name for this column in the current query context, if specified.</summary>
		public string Alias
		{
			get { return FAlias; }
			set { FAlias = value == null ? String.Empty : value; }
		}
		
		// ReferenceFlags
		private SQLReferenceFlags FReferenceFlags;
		public SQLReferenceFlags ReferenceFlags
		{
			get { return FReferenceFlags; }
			set { FReferenceFlags = value; }
		}

		// GetExpression - returns an expression suitable for use in a where clause or other referencing context
		public Expression GetExpression()
		{
			if (FExpression == null)
				return new QualifiedFieldExpression(FColumnName, FTableAlias);
			else
				return FExpression;
		}
		
		// GetColumnExpression - returns a ColumnExpression suitable for use in a select list
		public ColumnExpression GetColumnExpression()
		{
			return new ColumnExpression(GetExpression(), FAlias);
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
		public SQLRangeVarColumn this[string AColumnName] { get { return this[IndexOf(AColumnName)]; } }
		
		public int IndexOf(string AColumnName)
		{
			if (AColumnName.IndexOf(Keywords.Qualifier) == 0)
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (Schema.Object.NamesEqual(this[LIndex].TableVarColumn.Name, AColumnName))
						return LIndex;
			}
			else
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (this[LIndex].TableVarColumn.Name == AColumnName)
						return LIndex;
			}
			return -1;
		}
		
		public bool Contains(string AColumnName)
		{
			return IndexOf(AColumnName) >= 0;
		}
	}
	
	public class SQLRangeVar : System.Object
	{
		public SQLRangeVar(string AName)
		{
		   FName = AName;
		}

		// Name
		private string FName;
		public string Name { get { return FName; } }

		// Columns
		private SQLRangeVarColumns FColumns = new SQLRangeVarColumns();
		public SQLRangeVarColumns Columns { get { return FColumns; } }
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
		
		public SQLQueryContext(SQLQueryContext AParentContext) : base()
		{
			FParentContext = AParentContext;
		}
		
		public SQLQueryContext(SQLQueryContext AParentContext, bool AIsNestedFrom) : base()
		{
			FParentContext = AParentContext;
			FIsNestedFrom = AIsNestedFrom;
		}

		public SQLQueryContext(SQLQueryContext AParentContext, bool AIsNestedFrom, bool AIsScalarContext) : base()
		{
			FParentContext = AParentContext;
			FIsNestedFrom = AIsNestedFrom;
			FIsScalarContext = AIsScalarContext;
		}

		private SQLRangeVars FRangeVars = new SQLRangeVars();
		public SQLRangeVars RangeVars { get { return FRangeVars; } }
		
		private SQLRangeVarColumns FAddedColumns = new SQLRangeVarColumns();
		public SQLRangeVarColumns AddedColumns { get { return FAddedColumns; } }

		private SQLQueryContext FParentContext;
		public SQLQueryContext ParentContext { get { return FParentContext; } }

		private bool FIsNestedFrom;
		public bool IsNestedFrom { get { return FIsNestedFrom; } }
		
		private bool FIsScalarContext;
		public bool IsScalarContext { get { return FIsScalarContext; } }

		// True if this query context is an aggregate expression		
		private bool FIsAggregate;
		public bool IsAggregate 
		{ 
			get { return FIsAggregate; }
			set { FIsAggregate = value; }
		}
		
		// True if this query context contains computed columns
		private bool FIsExtension;
		public bool IsExtension
		{
			get { return FIsExtension; }
			set { FIsExtension = value; }
		}
		
		private bool FIsSelectClause;
		public bool IsSelectClause
		{
			get { return FIsSelectClause; }
			set { FIsSelectClause = value; }
		}

		private bool FIsWhereClause;
		public bool IsWhereClause
		{
			get { return FIsWhereClause; }
			set { FIsWhereClause = value; }
		}
		
		private bool FIsGroupByClause;
		public bool IsGroupByClause
		{
			get { return FIsGroupByClause; }
			set { FIsGroupByClause = value; }
		}

		private bool FIsHavingClause;
		public bool IsHavingClause
		{
			get { return FIsHavingClause; }
			set { FIsHavingClause = value; }
		}
		
		private bool FIsListContext;
		public bool IsListContext
		{
			get { return FIsListContext; }
			set { FIsListContext = value; }
		}

		// ReferenceFlags - These flags are set when the appropriate type of reference has taken place in this context
		private SQLReferenceFlags FReferenceFlags;
		public SQLReferenceFlags ReferenceFlags
		{
			get { return FReferenceFlags; }
			set { FReferenceFlags = value; }
		}
		
		public void ResetReferenceFlags()
		{
			FReferenceFlags = SQLReferenceFlags.None;
		}
		
		public SQLRangeVarColumn GetRangeVarColumn(string AIdentifier)
		{
			SQLRangeVarColumn LColumn = FindRangeVarColumn(AIdentifier);
			if (LColumn == null)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AIdentifier);
			return LColumn;
		}
		
		public SQLRangeVarColumn FindRangeVarColumn(string AIdentifier)
		{
			if (FAddedColumns.Contains(AIdentifier))
				return FAddedColumns[AIdentifier];
				
			foreach (SQLRangeVar LRangeVar in FRangeVars)
				if (LRangeVar.Columns.Contains(AIdentifier))
					return LRangeVar.Columns[AIdentifier];
				
			return null;
		}
		
		public void ProjectColumns(Schema.TableVarColumns AColumns)
		{
			for (int LIndex = FAddedColumns.Count - 1; LIndex >= 0; LIndex--)
				if (!AColumns.Contains(FAddedColumns[LIndex].TableVarColumn))
					FAddedColumns.RemoveAt(LIndex);
				
			foreach (SQLRangeVar LRangeVar in FRangeVars)
				for (int LIndex = LRangeVar.Columns.Count - 1; LIndex >= 0; LIndex--)
					if (!AColumns.Contains(LRangeVar.Columns[LIndex].TableVarColumn))
						LRangeVar.Columns.RemoveAt(LIndex);
		}
		
		public void RemoveColumn(SQLRangeVarColumn AColumn)
		{
			if (FAddedColumns.Contains(AColumn))
				FAddedColumns.Remove(AColumn);
			
			foreach (SQLRangeVar LRangeVar in FRangeVars)
				if (LRangeVar.Columns.Contains(AColumn))
					LRangeVar.Columns.Remove(AColumn);
		}
		
		public SQLRangeVarColumn RenameColumn(SQLDevicePlan ADevicePlan, Schema.TableVarColumn AOldColumn, Schema.TableVarColumn ANewColumn)
		{
			SQLRangeVarColumn LOldColumn = GetRangeVarColumn(AOldColumn.Name);
			SQLRangeVarColumn LNewColumn;
			if (LOldColumn.ColumnName != String.Empty)
				LNewColumn = new SQLRangeVarColumn(ANewColumn, LOldColumn.TableAlias, LOldColumn.ColumnName);
			else
				LNewColumn = new SQLRangeVarColumn(ANewColumn, LOldColumn.Expression, LOldColumn.Alias);
			
			RemoveColumn(LOldColumn);
			FAddedColumns.Add(LNewColumn);
			
			LNewColumn.ReferenceFlags = LOldColumn.ReferenceFlags;
			LNewColumn.Alias = ADevicePlan.Device.ToSQLIdentifier(ANewColumn.Name);
			
			return LNewColumn;
		}
	}

    public class SQLDevicePlan : DevicePlan
    {
		public SQLDevicePlan(Plan APlan, SQLDevice ADevice, PlanNode APlanNode) : base(APlan, ADevice, APlanNode) 
		{
			if ((APlanNode != null) && (APlanNode.DeviceNode != null))
				FDevicePlanNode = (SQLDevicePlanNode)APlanNode.DeviceNode;
		}
		
		public new SQLDevice Device { get { return (SQLDevice)base.Device; } }

		// SQLQueryContexts
		private SQLQueryContext FQueryContext = new SQLQueryContext();
		public void PushQueryContext(bool AIsNestedFrom)
		{
			FQueryContext = new SQLQueryContext(FQueryContext, AIsNestedFrom, false);
		}

		public void PushQueryContext()
		{
			PushQueryContext(false);
		}
	
		public void PopQueryContext()
		{
			FQueryContext = FQueryContext.ParentContext;
		}

		public SQLQueryContext CurrentQueryContext()
		{
			if (FQueryContext == null)
				throw new SQLException(SQLException.Codes.NoCurrentQueryContext);
			return FQueryContext;
		}
		
		public void PushScalarContext()
		{
			FQueryContext = new SQLQueryContext(FQueryContext, false, true);
		}

		public void PopScalarContext()
		{
			PopQueryContext();
		}

		// Internal stack used for translation		
		private Symbols FStack = new Symbols();
		public Symbols Stack { get { return FStack; } }
		
		// JoinContexts (only used for natural join translation)
		private SQLJoinContexts FJoinContexts = new SQLJoinContexts();
		public void PushJoinContext(SQLJoinContext AJoinContext)
		{
			FJoinContexts.Add(AJoinContext);
		}
		
		public void PopJoinContext()
		{
			FJoinContexts.RemoveAt(FJoinContexts.Count - 1);
		}
		
		// This will return null if the join being translated is not a natural join
		public SQLJoinContext CurrentJoinContext()
		{
			if (FJoinContexts.Count == 0)
				throw new SQLException(SQLException.Codes.NoCurrentJoinContext);
			return FJoinContexts[FJoinContexts.Count - 1];
		}
		
		public bool HasJoinContext()
		{
			return (CurrentJoinContext() != null);
		}
		
		// IsSubSelectSupported() - returns true if a subselect is supported in the current context
		public bool IsSubSelectSupported()
		{
			SQLQueryContext LContext = CurrentQueryContext();
			return 
				LContext.IsScalarContext &&
				(
					!(LContext.IsSelectClause || LContext.IsWhereClause || LContext.IsGroupByClause || LContext.IsHavingClause) ||
					(LContext.IsSelectClause && Device.SupportsSubSelectInSelectClause) ||
					(LContext.IsWhereClause && Device.SupportsSubSelectInWhereClause) ||
					(LContext.IsGroupByClause && Device.SupportsSubSelectInGroupByClause) ||
					(LContext.IsHavingClause && Device.SupportsSubSelectInHavingClause)
				);
		}
		
		public string GetSubSelectNotSupportedReason()
		{
			SQLQueryContext LContext = CurrentQueryContext();
			if (LContext.IsSelectClause && !Device.SupportsSubSelectInSelectClause)
				return "Plan is not supported because the device does not support sub-selects in the select clause.";
			if (LContext.IsWhereClause && !Device.SupportsSubSelectInWhereClause)
				return "Plan is not supported because the device does not support sub-selects in the where clause.";
			if (LContext.IsGroupByClause && !Device.SupportsSubSelectInGroupByClause)
				return "Plan is not supported because the device does not support sub-selects in the group by clause.";
			if (LContext.IsHavingClause && !Device.SupportsSubSelectInHavingClause)
				return "Plan is not supported because the device does not support sub-selects in the having clause.";
			return String.Empty;
		}
		
		// IsBooleanContext
		private ArrayList FContexts = new ArrayList();
		public bool IsBooleanContext()
		{
			return (FContexts.Count > 0) && ((bool)FContexts[FContexts.Count - 1]);
		}
		
		public void EnterContext(bool AIsBooleanContext)
		{
			FContexts.Add(AIsBooleanContext);
		}
		
		public void ExitContext()
		{
			FContexts.RemoveAt(FContexts.Count - 1);
		}
		
		private int FCounter = 0;
		private List<string> FTableAliases = new List<string>();
		public string GetNextTableAlias()
		{
			FCounter++;
			string LTableAlias = String.Format("T{0}", FCounter.ToString());
			FTableAliases.Add(LTableAlias);
			return LTableAlias;
		}

		private SQLDevicePlanNode FDevicePlanNode;		
		public SQLDevicePlanNode DevicePlanNode
		{
			get { return FDevicePlanNode; }
			set { FDevicePlanNode = value; }
		}
		
		public SQLRangeVarColumn GetRangeVarColumn(string AIdentifier, bool ACurrentContextOnly)
		{
			SQLRangeVarColumn LColumn = FindRangeVarColumn(AIdentifier, ACurrentContextOnly);
			if (LColumn == null)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AIdentifier);
			return LColumn;
		}
		
		public SQLRangeVarColumn FindRangeVarColumn(string AIdentifier, bool ACurrentContextOnly)
		{
			SQLRangeVarColumn LColumn = null;
			bool LInCurrentContext = true;
			SQLQueryContext LQueryContext = CurrentQueryContext();
			while (LQueryContext != null)
			{
				LColumn = LQueryContext.FindRangeVarColumn(AIdentifier);
				if (LColumn != null)
					break;

				if (LQueryContext.IsScalarContext)
					LQueryContext = LQueryContext.ParentContext;
				else
				{
					if (ACurrentContextOnly || (LQueryContext.IsNestedFrom && !Device.SupportsNestedCorrelation))
						break;
					
					LQueryContext = LQueryContext.ParentContext;
					LInCurrentContext = false;
				}
			}

			if (LColumn != null)
			{
				if (!LInCurrentContext)
					CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasCorrelation;
					
				CurrentQueryContext().ReferenceFlags |= LColumn.ReferenceFlags;
			}
				
			return LColumn;
		}
    }
    
    public class SQLDevicePlanNode : DevicePlanNode
    {
		public SQLDevicePlanNode(PlanNode APlanNode) : base(APlanNode) {}
		
		private Statement FStatement;
		public Statement Statement
		{
			get { return FStatement; }
			set { FStatement = value; }
		}

		// Parameters
		private SQLPlanParameters FPlanParameters = new SQLPlanParameters();
		public SQLPlanParameters PlanParameters { get { return FPlanParameters; } }
    }
    
	public class SQLPlanParameter  : System.Object
	{
		public SQLPlanParameter(SQLParameter ASQLParameter, PlanNode APlanNode) : base()
		{	
			FSQLParameter = ASQLParameter;
			FPlanNode = APlanNode; 
		}
		
		private SQLParameter FSQLParameter;
		public SQLParameter SQLParameter { get { return FSQLParameter; } }

		private PlanNode FPlanNode;
		public PlanNode PlanNode { get { return FPlanNode; } }
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
		public SQLDeviceSession(SQLDevice ADevice, ServerProcess AProcess, Schema.DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AProcess, ADeviceSessionInfo) {}
		
		protected override void Dispose(bool ADisposing)
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
					if (FStreams != null)
					{
						DestroyStreams();
						FStreams = null;
					}
				}
				finally
				{
					try
					{
						if (FBrowsePool != null)
						{
							if (Device.UseTransactions)
							{
								for (int LIndex = 0; LIndex < FBrowsePool.Count; LIndex++)
									try
									{
										if (FBrowsePool[LIndex].Connection.InTransaction)
											FBrowsePool[LIndex].Connection.CommitTransaction();
									}
									catch {}
							}

							FBrowsePool.Dispose();
							FBrowsePool = null;
						}
					}
					finally
					{
						try
						{
							if (FExecutePool != null)
							{
								FExecutePool.Dispose();
								FExecutePool = null;
							}
						}
						finally
						{
							base.Dispose(ADisposing); // Call the base here to clean up any outstanding transactions
						}
					}
				}
			}
		}
		
		public new SQLDevice Device { get { return (SQLDevice)base.Device; } }

		protected abstract SQLConnection InternalCreateConnection();
		protected SQLConnection CreateConnection()
		{
			SQLConnection LConnection = InternalCreateConnection();
			LConnection.DefaultCommandTimeout = Device.CommandTimeout;
			LConnection.DefaultUseParametersForCursors = Device.UseParametersForCursors;
			LConnection.DefaultShouldNormalizeWhitespace = Device.ShouldNormalizeWhitespace;
			return LConnection;
		}
		
		private SQLConnectionPool FBrowsePool = new SQLConnectionPool();
		private SQLConnectionPool FExecutePool = new SQLConnectionPool();
		
		public SQLConnection Connection { get { return RequestConnection(false).Connection; } }
		
		private bool FExecutingConnectionStatement;
		
		protected virtual void ExecuteConnectStatement(SQLConnectionHeader AConnectionHeader, bool AIsBrowseCursor)
		{
			if (!ServerProcess.IsLoading())
			{
				if (!FExecutingConnectionStatement)
				{
					FExecutingConnectionStatement = true;
					try
					{
						string LExecuteStatementExpression = AIsBrowseCursor ? Device.OnBrowseConnectStatement : Device.OnExecuteConnectStatement;
						if (LExecuteStatementExpression != null)
						{
							string LExecuteStatement = null;
							IServerExpressionPlan LPlan = ((IServerProcess)ServerProcess).PrepareExpression(LExecuteStatementExpression, null);
							try
							{
								LExecuteStatement = ((Scalar)LPlan.Evaluate(null)).AsString;
							}
							finally
							{
								((IServerProcess)ServerProcess).UnprepareExpression(LPlan);
							}
							
							AConnectionHeader.Connection.Execute(LExecuteStatement);
							
							if (AConnectionHeader.Connection.InTransaction)
							{
								AConnectionHeader.Connection.CommitTransaction();
								AConnectionHeader.Connection.BeginTransaction(FIsolationLevel);
							}
						}
					}
					finally
					{
						FExecutingConnectionStatement = false;
					}
				}
				else
					throw new SQLException(SQLException.Codes.AlreadyExecutingConnectionStatement, Device.Name);
			}
		}
		
		public virtual SQLConnectionHeader RequestConnection(bool AIsBrowseCursor)
		{
			SQLConnectionHeader LConnectionHeader = AIsBrowseCursor ? FBrowsePool.AvailableConnectionHeader() : FExecutePool.AvailableConnectionHeader();

			// Ensure Transaction Started
			if (Device.UseTransactions && (Transactions.Count > 0) && !FTransactionStarted)
				FTransactionStarted = true;
			
			if (LConnectionHeader == null)
			{
				SQLConnection LConnection = AddConnection(AIsBrowseCursor);
				if (LConnection != null)
				{
					LConnectionHeader = new SQLConnectionHeader(LConnection);
					if (AIsBrowseCursor)
						FBrowsePool.Add(LConnectionHeader);
					else
						FExecutePool.Add(LConnectionHeader);
					try
					{
						ExecuteConnectStatement(LConnectionHeader, AIsBrowseCursor);
					}
					catch
					{
						if (AIsBrowseCursor)
							FBrowsePool.Remove(LConnectionHeader);
						else
							FExecutePool.Remove(LConnectionHeader);
						LConnectionHeader.Dispose();
						throw;
					}
				}
			}

			if (LConnectionHeader == null)
			{
				// Perform a Cursor switch here to free up a connection on the transaction
				// This means that all connections in the execute pool are currently supporting open cursors.
				// Select the connection victim (the first connection in the execute pool)
				LConnectionHeader = FExecutePool[0];
				
				// Move the connection to the back of the list for round-robin pool replacement
				FExecutePool.Move(0, FExecutePool.Count - 1);

				if (LConnectionHeader.DeviceCursor != null)				
					LConnectionHeader.DeviceCursor.ReleaseConnection(LConnectionHeader);
			}
			
			// If the connection is closed, throw out the connection
			if (!LConnectionHeader.Connection.IsConnectionValid())
			{
				if (AIsBrowseCursor)
					FBrowsePool.Remove(LConnectionHeader);
				else
					FExecutePool.Remove(LConnectionHeader);

				LConnectionHeader.Dispose();
				LConnectionHeader = RequestConnection(AIsBrowseCursor);
			}
			
			// Ensure the connection has an active transaction
			if (Device.UseTransactions && (Transactions.Count > 0) && !AIsBrowseCursor && !LConnectionHeader.Connection.InTransaction)
				LConnectionHeader.Connection.BeginTransaction(FIsolationLevel);
			
			return LConnectionHeader;
		}
		
		public void ReleaseConnection(SQLConnection AConnection)
		{
			ReleaseConnection(AConnection, false);
		}
		
		public virtual void ReleaseConnection(SQLConnection AConnection, bool ADisposing)
		{
			int LIndex = FBrowsePool.IndexOfConnection(AConnection);
			if (LIndex >= 0)
				FBrowsePool[LIndex].DeviceCursor.ReleaseConnection(FBrowsePool[LIndex], ADisposing);
			else
			{
				LIndex = FExecutePool.IndexOfConnection(AConnection);
				if (LIndex >= 0)
					FExecutePool[LIndex].DeviceCursor.ReleaseConnection(FExecutePool[LIndex], ADisposing);
				else
					throw new SQLException(SQLException.Codes.ConnectionNotFound);
			}
		}

		// This override allows for the implementation of transaction binding across connections such as distributed transactions or session binding.		
		protected virtual SQLConnection AddConnection(bool AIsBrowseCursor)
		{
			if (AIsBrowseCursor)
			{
				SQLConnection LConnection = CreateConnection();
				if (Device.UseTransactions && (Transactions.Count > 0))
					LConnection.BeginTransaction(SQLIsolationLevel.ReadUncommitted);
				return LConnection;
			}
			else
			{
				if (FExecutePool.Count == 0)
				{
					SQLConnection LConnection = CreateConnection();
					if (Device.UseTransactions && (Transactions.Count > 0) && FTransactionStarted)
						LConnection.BeginTransaction(FIsolationLevel);
					return LConnection;
				}
				else
					return null;
			}
		}
		
		public static SQLIsolationLevel IsolationLevelToSQLIsolationLevel(IsolationLevel AIsolationLevel)
		{
			switch (AIsolationLevel)
			{
				case IsolationLevel.Browse : return SQLIsolationLevel.ReadUncommitted;
				case IsolationLevel.CursorStability : return SQLIsolationLevel.ReadCommitted;
				case IsolationLevel.Isolated : return SQLIsolationLevel.Serializable;
			}
			
			return SQLIsolationLevel.Serializable;
		}
		
		// BeginTransaction
		private SQLIsolationLevel FIsolationLevel;
		private bool FTransactionStarted;
		protected override void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			if (Device.UseTransactions && (Transactions.Count == 1)) // If this is the first transaction
			{
				FIsolationLevel = IsolationLevelToSQLIsolationLevel(AIsolationLevel);

				// Transaction starting is deferred until actually necessary
				FTransactionStarted = false;
				FTransactionFailure = false;
			}
		}
		
		private bool FTransactionFailure;
		/// <summary> Indicates whether the DBMS-side transaction has been rolled-back. </summary>
		public bool TransactionFailure
		{
			get { return FTransactionFailure; }
			set { FTransactionFailure = value; }
		}
		
		protected override bool IsTransactionFailure(Exception AException)
		{
			return FTransactionFailure;
		}
		
		protected override void InternalPrepareTransaction() {}

		// CommitTransaction
		protected override void InternalCommitTransaction()
		{
			if (Device.UseTransactions && (Transactions.Count == 1) && FTransactionStarted)
			{
				for (int LIndex = 0; LIndex < FExecutePool.Count; LIndex++)
				{
					if (FExecutePool[LIndex].DeviceCursor != null)
						FExecutePool[LIndex].DeviceCursor.ReleaseConnection(FExecutePool[LIndex], true);

					try
					{
						if (FExecutePool[LIndex].Connection.InTransaction)
							FExecutePool[LIndex].Connection.CommitTransaction();
					}
					catch
					{
						FTransactionFailure = FExecutePool[LIndex].Connection.TransactionFailure;
						throw;
					}
				}
				FTransactionStarted = false;
			}
		}
		
		// RollbackTransaction
		protected override void InternalRollbackTransaction()
		{
			if (Device.UseTransactions && (Transactions.Count == 1) && FTransactionStarted)
			{
				for (int LIndex = 0; LIndex < FExecutePool.Count; LIndex++)
				{
					if (FExecutePool[LIndex].DeviceCursor != null)
						FExecutePool[LIndex].DeviceCursor.ReleaseConnection(FExecutePool[LIndex], true);
						
					try
					{
						if (FExecutePool[LIndex].Connection.InTransaction)
							FExecutePool[LIndex].Connection.RollbackTransaction();
					}
					catch
					{
						FTransactionFailure = FExecutePool[LIndex].Connection.TransactionFailure;
					}
				}
				FTransactionStarted = false;
			}
		}

		protected virtual SQLTable CreateSQLTable(Program AProgram, TableNode ANode, SelectStatement ASelectStatement, SQLParameters AParameters, bool AIsAggregate)
		{
			return new SQLTable(this, AProgram, ANode, ASelectStatement, AParameters, AIsAggregate);
		}
		
        protected void SetParameterValueLength(object ANativeParamValue, SQLPlanParameter APlanParameter)
        {
            string LStringParamValue = ANativeParamValue as String;
            SQLStringType LParameterType = APlanParameter.SQLParameter.Type as SQLStringType;
            if ((LParameterType != null) && (LStringParamValue != null) && (LParameterType.Length != LStringParamValue.Length))
                LParameterType.Length = LStringParamValue.Length <= 20 ? 20 : LStringParamValue.Length;
        }

		protected void PrepareSQLParameters(SQLDevicePlan ADevicePlan, Program AProgram, bool AIsCursor, SQLParameters AParameters)
		{
			object LParamValue;
			object LNativeParamValue;
            SQLScalarType LParamType;
			foreach (SQLPlanParameter LPlanParameter in ADevicePlan.DevicePlanNode.PlanParameters)
			{
				LParamValue = LPlanParameter.PlanNode.Execute(AProgram);
				LParamType = (SQLScalarType)Device.ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)LPlanParameter.PlanNode.DataType);
                LNativeParamValue = (LParamValue == null) ? null : LParamType.ParameterFromScalar(AProgram.ValueManager, LParamValue);
                if (LNativeParamValue != null)
                    SetParameterValueLength(LNativeParamValue, LPlanParameter);

				AParameters.Add
				(
					new SQLParameter
					(
						LPlanParameter.SQLParameter.Name, 
						LPlanParameter.SQLParameter.Type, 
						LNativeParamValue,
						LPlanParameter.SQLParameter.Direction,
						LPlanParameter.SQLParameter.Marker,
						Device.UseParametersForCursors && AIsCursor ? null : LParamType.ToLiteral(AProgram.ValueManager, LParamValue)
					)
				);
			}
		}

		// Execute
		protected override object InternalExecute(Program AProgram, Schema.DevicePlan ADevicePlan)
		{
			SQLDevicePlan LPlan = (SQLDevicePlan)ADevicePlan;			
			LPlan.DevicePlanNode = (SQLDevicePlanNode)LPlan.Node.DeviceNode;
			if (LPlan.Node is DMLNode)
			{
				SQLConnectionHeader LHeader = RequestConnection(false);                             
				try
				{
					using (SQLCommand LCommand = LHeader.Connection.CreateCommand(false))
					{
						LCommand.Statement = Device.Emitter.Emit(LPlan.DevicePlanNode.Statement);						
						PrepareSQLParameters(LPlan, AProgram, false, LCommand.Parameters);
						LCommand.Execute();
						return null;
					}
				}
				catch
				{
					FTransactionFailure = LHeader.Connection.TransactionFailure;
					throw;
				}
			}
			else if (LPlan.Node is DDLNode)
			{
				SQLConnectionHeader LHeader = RequestConnection(false);
				try
				{
					if ((!ServerProcess.IsLoading()) && ((Device.ReconcileMode & D4.ReconcileMode.Command) != 0))
					{
						Statement LDDLStatement = LPlan.DevicePlanNode.Statement;
						if (LDDLStatement is Batch)
						{
							// If this is a batch, split it into separate statements for execution.  This is required because the Oracle ODBC driver does
							// not allow multiple statements to be executed at a time.
							using (SQLCommand LCommand = LHeader.Connection.CreateCommand(false))
							{
								foreach (Statement LStatement in ((Batch)LDDLStatement).Statements)
								{
									string LStatementString = Device.Emitter.Emit(LStatement);
									if (LStatementString != String.Empty)
									{
										LCommand.Statement = LStatementString;
										LCommand.Execute();
									}
								}
							}
						}
						else
						{
							string LStatement = Device.Emitter.Emit(LPlan.DevicePlanNode.Statement);
							if (LStatement != String.Empty)
							{
								using (SQLCommand LCommand = LHeader.Connection.CreateCommand(false))
								{
									LCommand.Statement = LStatement;									
									PrepareSQLParameters(LPlan, AProgram, false, LCommand.Parameters);
									LCommand.Execute();
								}
							}
						}
					}
					return null;
				}
				catch
				{
					FTransactionFailure = LHeader.Connection.TransactionFailure;
					throw;
				}
			}
			else
			{
				if (LPlan.Node is TableNode)
				{
					SQLParameters LParameters = new SQLParameters();
					PrepareSQLParameters(LPlan, AProgram, true, LParameters);
					SQLTable LTable = CreateSQLTable(AProgram, (TableNode)LPlan.Node, (SelectStatement)LPlan.DevicePlanNode.Statement, LParameters, LPlan.CurrentQueryContext().IsAggregate);
					try
					{
						LTable.Open();
						return LTable;
					}
					catch
					{
						LTable.Dispose();
						throw;
					}
				}
				else
				{
					SQLConnectionHeader LHeader = RequestConnection(false);
					try
					{
						using (SQLCommand LCommand = LHeader.Connection.CreateCommand(true))
						{
							LCommand.Statement = Device.Emitter.Emit(LPlan.DevicePlanNode.Statement);							
							PrepareSQLParameters(LPlan, AProgram, true, LCommand.Parameters);
							SQLCursor LCursor = LCommand.Open(SQLCursorType.Dynamic, IsolationLevelToSQLIsolationLevel(ServerProcess.CurrentIsolationLevel())); //SQLIsolationLevel.ReadCommitted);
							try
							{
								if (LCursor.Next())
								{
									if (LPlan.Node.DataType is Schema.IScalarType)
									{
										if (LCursor.IsNull(0))
											return null;
										else
											return Device.ResolveDeviceScalarType(ADevicePlan.Plan, (ScalarType)LPlan.Node.DataType).ToScalar(ServerProcess.ValueManager, LCursor[0]);	
									}
									else
									{
										Row LRow = new Row(AProgram.ValueManager, (Schema.IRowType)LPlan.Node.DataType);
										for (int LIndex = 0; LIndex < LRow.DataType.Columns.Count; LIndex++)
											if (!LCursor.IsNull(LIndex))
												LRow[LIndex] = Device.ResolveDeviceScalarType(ADevicePlan.Plan, (ScalarType)LRow.DataType.Columns[LIndex].DataType).ToScalar(ServerProcess.ValueManager, LCursor[LIndex]);
												
										if (LCursor.Next())
											throw new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression);

										return LRow;
									}
								}
								else
									return null;
							}
							finally
							{
								LCommand.Close(LCursor);
							}
						}
					}
					catch
					{
						FTransactionFailure = LHeader.Connection.TransactionFailure;
						throw;
					}
				}
			}
		}

		// InsertRow
		protected virtual void InternalVerifyInsertStatement(TableVar ATable, Row ARow, InsertStatement AStatement) {}
        protected override void InternalInsertRow(Program AProgram, TableVar ATable, Row ARow, BitArray AValueFlags)
        {
			SQLConnectionHeader LHeader = RequestConnection(false);
			try
			{
				using (SQLCommand LCommand = LHeader.Connection.CreateCommand(false))
				{
					InsertStatement LInsertStatement = new InsertStatement();
					LInsertStatement.InsertClause = new InsertClause();
					LInsertStatement.InsertClause.TableExpression = new TableExpression();
					LInsertStatement.InsertClause.TableExpression.TableSchema = D4.MetaData.GetTag(ATable.MetaData, "Storage.Schema", Device.Schema);
					LInsertStatement.InsertClause.TableExpression.TableName = Device.ToSQLIdentifier(ATable);
					if (Device.UseValuesClauseInInsert)
					{
						ValuesExpression LValues = new ValuesExpression();
						LInsertStatement.Values = LValues;
						string LColumnName;
						string LParameterName;
						Schema.TableVarColumn LColumn;
						SQLScalarType LScalarType;
						for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
						{
							LColumn = ATable.Columns[ATable.Columns.IndexOfName(ARow.DataType.Columns[LIndex].Name)];
							LColumnName = Device.ToSQLIdentifier(LColumn);
							LParameterName = String.Format("P{0}", LIndex.ToString());
							LInsertStatement.InsertClause.Columns.Add(new InsertFieldExpression(LColumnName));
							LValues.Expressions.Add(new QueryParameterExpression(LParameterName));
							LScalarType = (SQLScalarType)Device.ResolveDeviceScalarType(AProgram.Plan, (Schema.ScalarType)LColumn.DataType);
							LCommand.Parameters.Add
							(
								new SQLParameter
								(
									LParameterName,
									LScalarType.GetSQLParameterType(LColumn),
									ARow.HasValue(LIndex) ? LScalarType.ParameterFromScalar(AProgram.ValueManager, ARow[LIndex]) : null,
									SQLDirection.In, 
									Device.GetParameterMarker(LScalarType, LColumn)
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
						SelectExpression LSelectExpression = new SelectExpression();
						LInsertStatement.Values = LSelectExpression;
						LSelectExpression.FromClause = new CalculusFromClause(Device.GetDummyTableSpecifier());
						LSelectExpression.SelectClause = new SelectClause();
						string LColumnName;
						string LParameterName;
						Schema.TableVarColumn LColumn;
						SQLScalarType LScalarType;
						for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
						{
							LColumn = ATable.Columns[ATable.Columns.IndexOfName(ARow.DataType.Columns[LIndex].Name)];
							LColumnName = Device.ToSQLIdentifier(LColumn);
							LParameterName = String.Format("P{0}", LIndex.ToString());
							LInsertStatement.InsertClause.Columns.Add(new InsertFieldExpression(LColumnName));
							LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new QueryParameterExpression(LParameterName), LColumnName));
							LScalarType = (SQLScalarType)Device.ResolveDeviceScalarType(AProgram.Plan, (Schema.ScalarType)LColumn.DataType);
							LCommand.Parameters.Add
							(
								new SQLParameter
								(
									LParameterName,
									LScalarType.GetSQLParameterType(LColumn),
									ARow.HasValue(LIndex) ? LScalarType.ParameterFromScalar(AProgram.ValueManager, ARow[LIndex]) : null,
									SQLDirection.In,
									Device.GetParameterMarker(LScalarType, LColumn)
								)
							);
						}
						
					}
					
					InternalVerifyInsertStatement(ATable, ARow, LInsertStatement);
					if (Device.CommandTimeout >= 0)
						LCommand.CommandTimeout = Device.CommandTimeout;
					LCommand.Statement = Device.Emitter.Emit(LInsertStatement);
					LCommand.Execute();
				}
			}
			catch
			{
				FTransactionFailure = LHeader.Connection.TransactionFailure;
				throw;
			}
        }
        
        // UpdateRow
        protected virtual void InternalVerifyUpdateStatement(TableVar ATable, Row AOldRow, Row ANewRow, UpdateStatement AStatement) {}
        protected override void InternalUpdateRow(Program AProgram, TableVar ATable, Row AOldRow, Row ANewRow, BitArray AValueFlags)
        {
			UpdateStatement LStatement = new UpdateStatement();
			LStatement.UpdateClause = new UpdateClause();
			LStatement.UpdateClause.TableExpression = new TableExpression();
			LStatement.UpdateClause.TableExpression.TableSchema = D4.MetaData.GetTag(ATable.MetaData, "Storage.Schema", Device.Schema);
			LStatement.UpdateClause.TableExpression.TableName = Device.ToSQLIdentifier(ATable);

			SQLConnectionHeader LHeader = RequestConnection(false);
			try
			{
				using (SQLCommand LCommand = LHeader.Connection.CreateCommand(false))
				{
					string LColumnName;
					string LParameterName;
					Schema.TableVarColumn LColumn;
					SQLScalarType LScalarType;			
					for (int LIndex = 0; LIndex < ANewRow.DataType.Columns.Count; LIndex++)
					{
						if ((AValueFlags == null) || AValueFlags[LIndex])
						{
							LColumn = ATable.Columns[LIndex];
							LColumnName = Device.ToSQLIdentifier(LColumn);
							LParameterName = String.Format("P{0}", LIndex.ToString());
							UpdateFieldExpression LExpression = new UpdateFieldExpression();
							LExpression.FieldName = LColumnName;
							LExpression.Expression = new QueryParameterExpression(LParameterName);
							LScalarType = (SQLScalarType)Device.ResolveDeviceScalarType(AProgram.Plan, (Schema.ScalarType)LColumn.DataType);
							LCommand.Parameters.Add
							(
								new SQLParameter
								(
									LParameterName, 
									LScalarType.GetSQLParameterType(LColumn), 
									ANewRow.HasValue(LIndex) ? LScalarType.ParameterFromScalar(AProgram.ValueManager, ANewRow[LIndex]) : null, 
									SQLDirection.In, 
									Device.GetParameterMarker(LScalarType, LColumn)
								)
							);
							LStatement.UpdateClause.Columns.Add(LExpression);
						}
					}
					
					if (LStatement.UpdateClause.Columns.Count > 0)
					{
						Schema.Key LClusteringKey = AProgram.FindClusteringKey(ATable);
						if (LClusteringKey.Columns.Count > 0)
						{
							int LRowIndex;
							LStatement.WhereClause = new WhereClause();
							for (int LIndex = 0; LIndex < LClusteringKey.Columns.Count; LIndex++)
							{
								LColumn = LClusteringKey.Columns[LIndex];
								LRowIndex = AOldRow.DataType.Columns.IndexOfName(LColumn.Name);
								LColumnName = Device.ToSQLIdentifier(LColumn);
								LParameterName = String.Format("P{0}", (LIndex + ANewRow.DataType.Columns.Count).ToString());
								LScalarType = (SQLScalarType)Device.ResolveDeviceScalarType(AProgram.Plan, (Schema.ScalarType)LColumn.DataType);
								LCommand.Parameters.Add
								(
									new SQLParameter
									(
										LParameterName, 
										LScalarType.GetSQLParameterType(LColumn), 
										AOldRow.HasValue(LRowIndex) ? LScalarType.ParameterFromScalar(AProgram.ValueManager, AOldRow[LRowIndex]) : null, 
										SQLDirection.In, 
										Device.GetParameterMarker(LScalarType, LColumn)
									)
								);
								Expression LExpression = 
									new BinaryExpression
									(
										new QualifiedFieldExpression(LColumnName),
										"iEqual",
										new QueryParameterExpression(LParameterName)
									);
									
								if (LStatement.WhereClause.Expression == null)
									LStatement.WhereClause.Expression = LExpression;
								else
									LStatement.WhereClause.Expression = new BinaryExpression(LStatement.WhereClause.Expression, "iAnd", LExpression);
							}
						}
						
						InternalVerifyUpdateStatement(ATable, AOldRow, ANewRow, LStatement);
						if (Device.CommandTimeout >= 0)
							LCommand.CommandTimeout = Device.CommandTimeout;
						LCommand.Statement = Device.Emitter.Emit(LStatement);
						LCommand.Execute();
					}
				}
			}
			catch
			{
				FTransactionFailure = LHeader.Connection.TransactionFailure;
				throw;
			}
        }
        
        // DeleteRow
        protected virtual void InternalVerifyDeleteStatement(TableVar ATable, Row ARow, DeleteStatement AStatement) {}
        protected override void InternalDeleteRow(Program AProgram, TableVar ATable, Row ARow)
        {
			DeleteStatement LStatement = new DeleteStatement();
			LStatement.DeleteClause = new DeleteClause();
			LStatement.DeleteClause.TableExpression = new TableExpression();
			LStatement.DeleteClause.TableExpression.TableSchema = D4.MetaData.GetTag(ATable.MetaData, "Storage.Schema", Device.Schema);
			LStatement.DeleteClause.TableExpression.TableName = Device.ToSQLIdentifier(ATable);

			SQLConnectionHeader LHeader = RequestConnection(false);
			try
			{			
				using (SQLCommand LCommand = LHeader.Connection.CreateCommand(false))
				{
					Schema.TableVarColumn LColumn;
					string LColumnName;
					string LParameterName;
					SQLScalarType LScalarType;
					
					Schema.Key LClusteringKey = AProgram.FindClusteringKey(ATable);
					if (LClusteringKey.Columns.Count > 0)
					{
						int LRowIndex;
						LStatement.WhereClause = new WhereClause();
						for (int LIndex = 0; LIndex < LClusteringKey.Columns.Count; LIndex++)
						{
							LColumn = LClusteringKey.Columns[LIndex];					
							LRowIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
							LColumnName = Device.ToSQLIdentifier(LColumn);
							LParameterName = String.Format("P{0}", LIndex.ToString());
							LScalarType = (SQLScalarType)Device.ResolveDeviceScalarType(AProgram.Plan, (Schema.ScalarType)LColumn.DataType);
							LCommand.Parameters.Add
							(
								new SQLParameter
								(
									LParameterName, 
									LScalarType.GetSQLParameterType(LColumn), 
									ARow.HasValue(LRowIndex) ? LScalarType.ParameterFromScalar(AProgram.ValueManager, ARow[LRowIndex]) : null, 
									SQLDirection.In, 
									Device.GetParameterMarker(LScalarType, LColumn)
								)
							);
							Expression LExpression = 
								new BinaryExpression
								(
									new QualifiedFieldExpression(LColumnName),
									"iEqual",
									new QueryParameterExpression(LParameterName)
								);
								
							if (LStatement.WhereClause.Expression == null)
								LStatement.WhereClause.Expression = LExpression;
							else
								LStatement.WhereClause.Expression = new BinaryExpression(LStatement.WhereClause.Expression, "iAnd", LExpression);
						}
					}

					InternalVerifyDeleteStatement(ATable, ARow, LStatement);
					if (Device.CommandTimeout >= 0)
						LCommand.CommandTimeout = Device.CommandTimeout;
					LCommand.Statement = Device.Emitter.Emit(LStatement);
					LCommand.Execute();			
				}
			}
			catch
			{
				FTransactionFailure = LHeader.Connection.TransactionFailure;
				throw;
			}
        }

		// IStreamProvider
		private SQLStreamHeaders FStreams = new SQLStreamHeaders();
		
		private SQLStreamHeader GetStreamHeader(StreamID AStreamID)
		{
			SQLStreamHeader LStreamHeader = FStreams[AStreamID];
			if (LStreamHeader == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, AStreamID.ToString());
			return LStreamHeader;
		}
		
		protected void DestroyStreams()
		{
			foreach (StreamID LStreamID in FStreams.Keys)
				FStreams[LStreamID].Dispose();
			FStreams.Clear();
		}
		
		public virtual void Create
		(
			StreamID AStreamID, 
			string AColumnName, 
			Schema.DeviceScalarType ADeviceScalarType, 
			string AStatement, 
			SQLParameters AParameters, 
			SQLCursorType ACursorType, 
			SQLIsolationLevel AIsolationLevel
		)
		{
			FStreams.Add(new SQLStreamHeader(AStreamID, this, AColumnName, ADeviceScalarType, AStatement, AParameters, ACursorType, AIsolationLevel));
		}
		
		public virtual Stream Open(StreamID AStreamID)
		{
			try
			{
				return GetStreamHeader(AStreamID).GetSourceStream();
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}
		}
		
		public virtual void Close(StreamID AStreamID)
		{
			// no action to perform
		}
		
		public virtual void Destroy(StreamID AStreamID)
		{
			SQLStreamHeader LStreamHeader = GetStreamHeader(AStreamID);
			FStreams.Remove(AStreamID);
			LStreamHeader.Dispose();
		}
		
		public virtual void Reassign(StreamID AOldStreamID, StreamID ANewStreamID)
		{
			SQLStreamHeader LStreamHeader = GetStreamHeader(AOldStreamID);
			FStreams.Remove(AOldStreamID);
			LStreamHeader.StreamID = ANewStreamID;
			FStreams.Add(LStreamHeader);
		}
		
		// SQLExecute
		public void SQLExecute(string AStatement, SQLParameters AParameters)
		{
			SQLConnectionHeader LHeader = RequestConnection(false);
			try
			{
				LHeader.Connection.Execute(AStatement, AParameters);
			}
			catch (Exception LException)
			{
				FTransactionFailure = LHeader.Connection.TransactionFailure;
				throw WrapException(LException);
			}
		}
	}
	
	public class SQLStreamHeader : Disposable
	{
		public SQLStreamHeader
		(
			StreamID AStreamID, 
			SQLDeviceSession ADeviceSession, 
			string AColumnName, 
			Schema.DeviceScalarType ADeviceScalarType, 
			string AStatement, 
			SQLParameters AParameters, 
			SQLCursorType ACursorType, 
			SQLIsolationLevel AIsolationLevel
		) : base()
		{
			FStreamID = AStreamID;
			FDeviceSession = ADeviceSession;
			FColumnName = AColumnName;
			FDeviceScalarType = ADeviceScalarType;
			FStatement = AStatement;
			FParameters = AParameters;
			FCursorType = ACursorType;
			FIsolationLevel = AIsolationLevel;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			CloseSource();
			FDeviceScalarType = null;
			FDeviceSession = null;
			FParameters = null;
			FStreamID = StreamID.Null;
			base.Dispose(ADisposing);
		}
		
		// The stream manager id for this stream
		private StreamID FStreamID;
		public StreamID StreamID 
		{ 
			get { return FStreamID; } 
			set { FStreamID = value; }
		}
		
		// The device session supporting this stream
		private SQLDeviceSession FDeviceSession;
		
		// The name of the column from which this stream originated
		private string FColumnName;
		
		// The device scalar type map for the data type of this stream
		private Schema.DeviceScalarType FDeviceScalarType;
		
		// The statement used to retrieve the data for the stream
		private string FStatement;
		
		// The parameters used to retrieve the data for the stream
		private SQLParameters FParameters;
		
		// The cursor type used for the open request
		private SQLCursorType FCursorType;
		
		// The isolation level used for the open request
		private SQLIsolationLevel FIsolationLevel;

		// A DeferredWriteStream built on the DeferredStream obtained from a cursor based on the expression
		//	<FTable.Node> where KeyColumns = KeyRowValues over { KeyColumns, DataColumn }
		private DeferredWriteStream FSourceStream;
		public Stream GetSourceStream()
		{
			OpenSource();
			return FSourceStream;
		}
		
		// If the underlying value cannot be opened deferred from the connectivity layer, it is stored in this non-native scalar
		private Scalar FSourceValue;

		// The Cursor used to access the deferred stream		
		private SQLCursor FCursor;
		
		public void OpenSource()
		{
			if (FSourceStream == null)
			{
				SQLConnectionHeader LHeader = FDeviceSession.RequestConnection(false);
				try
				{
					FCursor = LHeader.Connection.Open(FStatement, FParameters, FCursorType, FIsolationLevel, SQLCommandBehavior.Default);
					try
					{
						if (FCursor.Next())
							if (FCursor.IsDeferred(FCursor.ColumnCount - 1))
								FSourceStream = new DeferredWriteStream(FDeviceScalarType.GetStreamAdapter(FDeviceSession.ServerProcess.ValueManager, FCursor.OpenDeferredStream(FCursor.ColumnCount - 1)));
							else
							{
								FSourceValue = new Scalar(FDeviceSession.ServerProcess.ValueManager, FDeviceScalarType.ScalarType);
								FSourceValue.AsNative = FDeviceScalarType.ToScalar(FDeviceSession.ServerProcess.ValueManager, FCursor[FCursor.ColumnCount - 1]);
								FSourceStream = new DeferredWriteStream(FSourceValue.OpenStream());
							}
						else
							throw new SQLException(SQLException.Codes.DeferredDataNotFound, FColumnName);
					}
					finally
					{
						FCursor.Command.Connection.Close(FCursor);
						FCursor = null;
					}
				}
				catch
				{
					FDeviceSession.TransactionFailure = LHeader.Connection.TransactionFailure;
					throw;
				}
			}
		}
		
		public void CloseSource()
		{
			try
			{
				if (FSourceStream != null)
				{
					FSourceStream.Close();
					FSourceStream = null;
				}
			}
			finally
			{
				if (FSourceValue != null)
				{
					FSourceValue.Dispose();
					FSourceValue = null;
				}
			}
		}
	}
	
	public class SQLStreamHeaders : Hashtable
	{
		public SQLStreamHeaders() : base(){}
		
		public SQLStreamHeader this[StreamID AStreamID] { get { return (SQLStreamHeader)base[AStreamID]; } }
		
		public void Add(SQLStreamHeader AStream)
		{
			Add(AStream.StreamID, AStream);
		}
		
		public void Remove(SQLStreamHeader AStream)
		{
			Remove(AStream.StreamID);
		}
	}
	
	public class ColumnMap
	{
		public ColumnMap(Column AColumn, ColumnExpression AExpression, int AIndex)
		{
			FColumn = AColumn;
			FColumnExpression = AExpression;
			FIndex = AIndex;
		}
		
		// The Column instance in DataType.Columns
		private Column FColumn;
		public Column Column { get { return FColumn; } }
		
		// The index of FColumn in DataType.Columns
		private int FIndex;
		public int Index { get { return FIndex; } }
		
		// The column expression in the translated statement
		private ColumnExpression FColumnExpression;
		public ColumnExpression ColumnExpression { get { return FColumnExpression; } }
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
		public ColumnMap ColumnMapByIndex(int AIndex)
		{
			foreach (ColumnMap LColumnMap in this)
				if (LColumnMap.Index == AIndex)
					return LColumnMap;
					
			throw new SQLException(SQLException.Codes.ColumnMapNotFound, AIndex);
		}
	}
	
	public class SQLTableColumn
	{
		public SQLTableColumn(Schema.TableVarColumn AColumn, Schema.DeviceScalarType AScalarType) : base()
		{
			Column = AColumn;
			ScalarType = AScalarType;
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
		public SQLTable(SQLDeviceSession ADeviceSession, Program AProgram, TableNode ATableNode, SelectStatement AStatement, SQLParameters AParameters, bool AIsAggregate) : base(ATableNode, AProgram)
		{
			FDeviceSession = ADeviceSession;
			FParameters = AParameters;
			FStatement = new SelectStatement();
			FStatement.Modifiers = AStatement.Modifiers;
			FIsAggregate = AIsAggregate;
			FStatement.QueryExpression = new QueryExpression();
			FStatement.QueryExpression.SelectExpression = new SelectExpression();
			SelectClause LSelectClause = new SelectClause();
			SelectClause LGivenSelectClause = AStatement.QueryExpression.SelectExpression.SelectClause;
			LSelectClause.Distinct = LGivenSelectClause.Distinct;
			LSelectClause.NonProject = LGivenSelectClause.NonProject;
			LSelectClause.Columns.AddRange(LGivenSelectClause.Columns);
			FStatement.QueryExpression.SelectExpression.SelectClause = LSelectClause;
			FStatement.QueryExpression.SelectExpression.FromClause = AStatement.QueryExpression.SelectExpression.FromClause;
			FStatement.QueryExpression.SelectExpression.WhereClause = AStatement.QueryExpression.SelectExpression.WhereClause;
			FStatement.QueryExpression.SelectExpression.GroupClause = AStatement.QueryExpression.SelectExpression.GroupClause;
			FStatement.QueryExpression.SelectExpression.HavingClause = AStatement.QueryExpression.SelectExpression.HavingClause;
			FStatement.OrderClause = AStatement.OrderClause;
			
			foreach (TableVarColumn LColumn in Node.TableVar.Columns)
			{
				SQLTableColumn LSQLColumn = new SQLTableColumn(LColumn, (DeviceScalarType)FDeviceSession.Device.ResolveDeviceScalarType(AProgram.Plan, (Schema.ScalarType)LColumn.DataType));
				FSQLColumns.Add(LSQLColumn);
				if (LSQLColumn.IsDeferred)
					FHasDeferredData = true;
			}
		}
		
		protected override void Dispose(bool ADisposing)
		{
			Close();
			FDeviceSession = null;
			base.Dispose(ADisposing);
		}
		
		protected SQLDeviceSession FDeviceSession;
		public SQLDeviceSession DeviceSession { get { return FDeviceSession; } }
		
		protected SQLDeviceCursor FCursor;
		protected SQLParameters FParameters;
		protected SelectStatement FStatement;
		protected bool FIsAggregate;
		protected ColumnMaps FMainColumns = new ColumnMaps();
		protected ColumnMaps FDeferredColumns = new ColumnMaps();
		protected SQLTableColumns FSQLColumns = new SQLTableColumns();
		protected bool FHasDeferredData;
		protected bool FBOF;
		protected bool FEOF;
		
		public static SQLCursorType CursorTypeToSQLCursorType(DAE.CursorType ACursorType)
		{
			switch (ACursorType)
			{
				case DAE.CursorType.Static: return SQLCursorType.Static;
				default: return SQLCursorType.Dynamic;
			}
		}

		public static SQLLockType CursorCapabilitiesToSQLLockType(CursorCapability ACapabilities, IsolationLevel AIsolation)
		{
			return (((ACapabilities & CursorCapability.Updateable) != 0) && (AIsolation == IsolationLevel.Isolated)) ? SQLLockType.Pessimistic : SQLLockType.ReadOnly;
		}
		
		public static IsolationLevel CursorIsolationToIsolationLevel(CursorIsolation ACursorIsolation, IsolationLevel AIsolation)
		{
			switch (ACursorIsolation)
			{
				case CursorIsolation.Chaos:
				case CursorIsolation.Browse: return IsolationLevel.Browse;
				case CursorIsolation.CursorStability:
				case CursorIsolation.Isolated: return IsolationLevel.Isolated;
				default: return (AIsolation == IsolationLevel.Browse) ? IsolationLevel.Browse : IsolationLevel.Isolated;
			}
		}

		public static SQLIsolationLevel CursorIsolationToSQLIsolationLevel(CursorIsolation ACursorIsolation, IsolationLevel AIsolation)
		{
			return SQLDeviceSession.IsolationLevelToSQLIsolationLevel(CursorIsolationToIsolationLevel(ACursorIsolation, AIsolation));
		}
		
		private SQLDeviceCursor GetMainCursor()
		{
			SelectExpression LSelectExpression = FStatement.QueryExpression.SelectExpression;

			if (FHasDeferredData)
			{
				for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
					if (FSQLColumns[LIndex].IsDeferred)
					{
						ColumnMap LColumnMap = 
							new ColumnMap
							(
								DataType.Columns[LIndex], 
								LSelectExpression.SelectClause.Columns[LIndex], 
								LIndex
							);
							
						LSelectExpression.SelectClause.Columns[LIndex] = 
							new ColumnExpression
							(
								new CaseExpression
								(
									new CaseItemExpression[]
									{
										new CaseItemExpression
										(
											new UnaryExpression("iIsNull", LColumnMap.ColumnExpression.Expression),
											new ValueExpression(0)
										)
									},
									new CaseElseExpression(new ValueExpression(1))
								),
								LColumnMap.ColumnExpression.ColumnAlias
							);
							
						FDeferredColumns.Add(LColumnMap);
					}
					else
						FMainColumns.Add(new ColumnMap(DataType.Columns[LIndex], LSelectExpression.SelectClause.Columns[LIndex], LIndex));
			}
			
			SQLParameters LParameters = new SQLParameters();
			LParameters.AddRange(FParameters);
			
			SQLParameters LKeyParameters = new SQLParameters();
			int[] LKeyIndexes = new int[Node.Order == null ? 0 : Node.Order.Columns.Count];
			SQLScalarType[] LKeyTypes = new SQLScalarType[LKeyIndexes.Length];
			TableVarColumn LKeyColumn;
			for (int LIndex = 0; LIndex < LKeyIndexes.Length; LIndex++)
			{
				LKeyColumn = Node.Order.Columns[LIndex].Column;
				LKeyIndexes[LIndex] = DataType.Columns.IndexOfName(LKeyColumn.Name);
				LKeyTypes[LIndex] = (SQLScalarType)FSQLColumns[LKeyIndexes[LIndex]].ScalarType;
				LKeyParameters.Add
				(
					new SQLParameter
					(
						LSelectExpression.SelectClause.Columns[LKeyIndexes[LIndex]].ColumnAlias, 
						LKeyTypes[LIndex].GetSQLParameterType(LKeyColumn),
						null,
						SQLDirection.In,
						DeviceSession.Device.GetParameterMarker(LKeyTypes[LIndex], LKeyColumn)
					)
				);
			}
			
			SelectStatement LStatement = new SelectStatement();
			LStatement.Modifiers = FStatement.Modifiers;
			LStatement.QueryExpression = new QueryExpression();
			LStatement.QueryExpression.SelectExpression = new SelectExpression();
			LStatement.QueryExpression.SelectExpression.SelectClause = FStatement.QueryExpression.SelectExpression.SelectClause;
			LStatement.QueryExpression.SelectExpression.FromClause = FStatement.QueryExpression.SelectExpression.FromClause;
			if (FStatement.QueryExpression.SelectExpression.WhereClause != null)
				LStatement.QueryExpression.SelectExpression.WhereClause = new WhereClause(FStatement.QueryExpression.SelectExpression.WhereClause.Expression);
			LStatement.QueryExpression.SelectExpression.GroupClause = FStatement.QueryExpression.SelectExpression.GroupClause;
			LStatement.QueryExpression.SelectExpression.HavingClause = FStatement.QueryExpression.SelectExpression.HavingClause;
			LStatement.OrderClause = FStatement.OrderClause;

			return 
				new SQLDeviceCursor
				(
					FDeviceSession, 
					LStatement, 
					FIsAggregate, 
					LParameters, 
					LKeyIndexes, 
					LKeyTypes, 
					LKeyParameters, 
					CursorCapabilitiesToSQLLockType(Capabilities, CursorIsolationToIsolationLevel(Isolation, DeviceSession.ServerProcess.CurrentIsolationLevel())), 
					CursorTypeToSQLCursorType(Node.RequestedCursorType), 
					CursorIsolationToSQLIsolationLevel(Isolation, DeviceSession.ServerProcess.CurrentIsolationLevel())
				);
		}
		
		// Returns a SQLCommand for accessing the deferred read data for the given column index.  
		// The deferred column is guaranteed to be the last column in the Cursor opened from the command.
		private void GetDeferredStatement(Row AKey, int AColumnIndex, out string AStatement, out SQLParameters AParameters)
		{
			AParameters = new SQLParameters();
			AParameters.AddRange(FParameters);

			SelectStatement LStatement = new SelectStatement();
			LStatement.Modifiers = FStatement.Modifiers;
			LStatement.QueryExpression = new QueryExpression();
			LStatement.QueryExpression.SelectExpression = new SelectExpression();
			LStatement.QueryExpression.SelectExpression.SelectClause = new SelectClause();
			Expression LKeyCondition = null;
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
			{
				ColumnMap LColumnMap = FMainColumns.ColumnMapByIndex(DataType.Columns.IndexOfName(AKey.DataType.Columns[LIndex].Name));
				SQLScalarType LScalarType = (SQLScalarType)FSQLColumns[LColumnMap.Index].ScalarType;
				TableVarColumn LTableVarColumn = Node.TableVar.Columns[AKey.DataType.Columns[LIndex].Name];
				LStatement.QueryExpression.SelectExpression.SelectClause.Columns.Add(LColumnMap.ColumnExpression);
				SQLParameter LParameter = 
					new SQLParameter
					(
						LColumnMap.ColumnExpression.ColumnAlias, 
						LScalarType.GetSQLParameterType(LTableVarColumn), 
						AKey.HasValue(LIndex) ? 
							LScalarType.ParameterFromScalar(Manager, AKey[LIndex]) : 
							null,
						SQLDirection.In,
						DeviceSession.Device.GetParameterMarker(LScalarType, LTableVarColumn),
						DeviceSession.Device.UseParametersForCursors ? null : LScalarType.ToLiteral(Manager, AKey[LIndex])
					);
				AParameters.Add(LParameter);
				Expression LCondition =	
					new BinaryExpression
					(
						LColumnMap.ColumnExpression.Expression,
						"iEqual",
						new QueryParameterExpression(LParameter.Name)
					);
					
				if (LKeyCondition != null)
					LKeyCondition = new BinaryExpression(LKeyCondition, "iAnd", LCondition);
				else
					LKeyCondition = LCondition;
			}
			
			LStatement.QueryExpression.SelectExpression.SelectClause.Columns.Add(FDeferredColumns.ColumnMapByIndex(AColumnIndex).ColumnExpression);
			LStatement.QueryExpression.SelectExpression.FromClause = FStatement.QueryExpression.SelectExpression.FromClause;
			if ((LKeyCondition != null) || (FStatement.QueryExpression.SelectExpression.WhereClause != null))
			{
				LStatement.QueryExpression.SelectExpression.WhereClause = new WhereClause();
				if ((LKeyCondition != null) && (FStatement.QueryExpression.SelectExpression.WhereClause != null))
					LStatement.QueryExpression.SelectExpression.WhereClause.Expression = new BinaryExpression(FStatement.QueryExpression.SelectExpression.WhereClause.Expression, "iAnd", LKeyCondition);
				else if (LKeyCondition != null)
					LStatement.QueryExpression.SelectExpression.WhereClause.Expression = LKeyCondition;
				else
					LStatement.QueryExpression.SelectExpression.WhereClause.Expression = FStatement.QueryExpression.SelectExpression.WhereClause.Expression;
			}
				
			LStatement.QueryExpression.SelectExpression.GroupClause = FStatement.QueryExpression.SelectExpression.GroupClause;
			LStatement.QueryExpression.SelectExpression.HavingClause = FStatement.QueryExpression.SelectExpression.HavingClause;
			
			AStatement = FDeviceSession.Device.Emitter.Emit(LStatement);
		}
		
		protected override void InternalOpen()
		{
			if (!FDeviceSession.ServerProcess.IsOpeningInsertCursor)
			{
				FCursor = GetMainCursor();
				FBOF = true;
				FEOF = !FCursor.Next();
			}
			else
			{
				FBOF = true;
				FEOF = true;
			}
		}
		
		protected override void InternalClose()
		{
			if (FCursor != null)
			{
				FCursor.Dispose();
				FCursor = null;
			}
		}
		
		protected void InternalSelect(Row ARow, bool AAllowDeferred)
		{
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
			{
				int LColumnIndex = DataType.Columns.IndexOfName(ARow.DataType.Columns[LIndex].Name);
				if (LColumnIndex >= 0)
				{
					if (FSQLColumns[LColumnIndex].IsDeferred)
					{
						if (!AAllowDeferred)
							throw new SQLException(SQLException.Codes.InvalidDeferredContext);
							
						if ((int)FCursor[LColumnIndex] == 0)
							ARow.ClearValue(LIndex);
						else
						{
							// set up a deferred read stream using the device session as the provider
							StreamID LStreamID = Program.ServerProcess.Register(FDeviceSession);
							string LStatement;
							SQLParameters LParameters;
							GetDeferredStatement(InternalGetKey(), LColumnIndex, out LStatement, out LParameters);
							FDeviceSession.Create
							(
								LStreamID, 
								DataType.Columns[LColumnIndex].Name, 
								FSQLColumns[LColumnIndex].ScalarType, 
								LStatement,
								LParameters,
								CursorTypeToSQLCursorType(Node.RequestedCursorType), 
								CursorIsolationToSQLIsolationLevel(Isolation, DeviceSession.ServerProcess.CurrentIsolationLevel())
							);
							ARow[LIndex] = LStreamID;
						}
					}
					else
					{
						if (FCursor.IsNull(LColumnIndex))
							ARow.ClearValue(LIndex);
						else
							ARow[LIndex] = FSQLColumns[LColumnIndex].ScalarType.ToScalar(Manager, FCursor[LColumnIndex]);
					}
				}
			}
		}
		
		protected override void InternalSelect(Row ARow)
		{
			InternalSelect(ARow, true);
		}
		
		protected override Row InternalGetKey()
		{
			Row LRow = new Row(Manager, new RowType(Program.FindClusteringKey(Node.TableVar).Columns));
			InternalSelect(LRow, false);
			return LRow;
		}
		
		protected override bool InternalNext()
		{
			if (FBOF)
			{
				FBOF = FEOF;
			}
			else
			{
				FEOF = !FCursor.Next();
				FBOF = FBOF && FEOF;
			}
			return !FEOF;
		}
		
		protected override bool InternalBOF()
		{
			return FBOF;
		}
		
		protected override bool InternalEOF()
		{
			return FEOF;
		}
	}

    public static class SQLDeviceUtility
    {
		public static SQLDevice ResolveSQLDevice(Plan APlan, string ADeviceName)
		{
			Device LDevice = Compiler.ResolveCatalogIdentifier(APlan, ADeviceName, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);

			SQLDevice LSQLDevice = LDevice as SQLDevice;
			if (LSQLDevice == null)
				throw new SQLException(SQLException.Codes.SQLDeviceExpected);

			return LSQLDevice;
		}

		public static ReconcileOptions ResolveReconcileOptions(ListValue AList)
		{
			StringBuilder LList = new StringBuilder();
			for (int LIndex = 0; LIndex < AList.Count(); LIndex++)
			{
				if (LIndex > 0)
					LList.Append(", ");
				LList.Append((string)AList[LIndex]);
			}
			
			return (ReconcileOptions)Enum.Parse(typeof(ReconcileOptions), LList.ToString());
		}
	}
    
	// operator D4ToSQL(AQuery : System.String) : System.String;
    // operator D4ToSQL(ADeviceName : System.Name, AQuery : System.String) : System.String;
    public class D4ToSQLNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LStatementString;
			Schema.Device LDevice = null;
			SQLDevice LSQLDevice = null;
			if (AArguments.Length == 1)
				LStatementString = (string)AArguments[0];
			else
			{
				LSQLDevice = SQLDeviceUtility.ResolveSQLDevice(AProgram.Plan, (string)AArguments[0]);
				LDevice = LSQLDevice;
				LStatementString = (string)AArguments[1];
			}

			string LSQLQuery = String.Empty;
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				ParserMessages LParserMessages = new ParserMessages();
				Statement LStatement = new D4.Parser().ParseStatement(LStatementString, LParserMessages);
				LPlan.Messages.AddRange(LParserMessages);
				PlanNode LNode = Compiler.Compile(LPlan, LStatement);
				if (LPlan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));

				if (LNode is FrameNode)
					LNode = LNode.Nodes[0];
				if ((LNode is ExpressionStatementNode) || (LNode is CursorNode))
					LNode = LNode.Nodes[0];
					
				if (LDevice == null)
					LDevice = LNode.Device;
					
				if ((LDevice != null) && LNode.DeviceSupported)
				{
					if (LSQLDevice == null)
						LSQLDevice = LDevice as SQLDevice;
						
					if (LSQLDevice == null)
						throw new SQLException(SQLException.Codes.QuerySupportedByNonSQLDevice, LDevice.Name);

					if (LNode.Device == LSQLDevice)
						LSQLQuery = LSQLDevice.Emitter.Emit(((SQLDevicePlan)LPlan.GetDevicePlan(LNode)).DevicePlanNode.Statement);
					else
						throw new SQLException(SQLException.Codes.QuerySupportedByDifferentDevice, LNode.Device.Name, LSQLDevice.Name);
				}
				else
					throw new SQLException(SQLException.Codes.QueryUnsupported);
			}
			finally
			{
				LPlan.Dispose();
			}

			return LSQLQuery;
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
		private static bool IsValidIdentifierCharacter(char AChar)
		{
			return (AChar == '_') || Char.IsLetterOrDigit(AChar);
		}

		private static bool HandleParameter(Plan APlan, SQLDevice ADevice, SQLParameters AParams, PlanNode[] AConversionNodes, Schema.IRowType AInRowType, Schema.IRowType AOutRowType, string AParameterName)
		{
			int LInIndex = AInRowType != null ? AInRowType.Columns.IndexOf(AParameterName) : -1;
			int LOutIndex = AOutRowType != null ? AOutRowType.Columns.IndexOf(AParameterName) : -1;
			if ((LInIndex >= 0) || (LOutIndex >= 0))
			{
				Schema.ScalarType LValueType;
				
				if (LOutIndex >= 0)
					LValueType = (Schema.ScalarType)AOutRowType.Columns[LOutIndex].DataType;
				else
					LValueType = (Schema.ScalarType)AInRowType.Columns[LInIndex].DataType;

				if (LInIndex >= 0)
				{
					//if (AInValues.HasValue(LInIndex))
					//	LValue = (Scalar)AInValues[LInIndex];

					if (!AInRowType.Columns[LInIndex].DataType.Equals(LValueType))
					{
						ValueNode LValueNode = new DAE.Runtime.Instructions.ValueNode();
						//LValueNode.Value = LValue;
						LValueNode.DataType = LValueType;
						PlanNode LSourceNode = LValueNode;

						if (!AInRowType.Columns[LInIndex].DataType.Is(LValueType))
						{
							ConversionContext LContext = Compiler.FindConversionPath(APlan, AInRowType.Columns[LInIndex].DataType, LValueType);
							Compiler.CheckConversionContext(APlan, LContext);
							LSourceNode = Compiler.ConvertNode(APlan, LSourceNode, LContext);
						}
						
						LSourceNode = Compiler.Upcast(APlan, LSourceNode, LValueType);
						AConversionNodes[LInIndex] = LSourceNode;
							
						//LValue = (Scalar)Compiler.Upcast(AProcess.Plan, LSourceNode, LValueType).Execute(AProcess).Value;
					}
				}
				
				SQLScalarType LScalarType = ADevice.ResolveDeviceScalarType(APlan, LValueType) as SQLScalarType;
				if (LScalarType == null)
					throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, LValueType.Name);

				AParams.Add(new SQLParameter(AParameterName, LScalarType.GetSQLParameterType(), null, ((LInIndex >= 0) && (LOutIndex >= 0)) ? SQLDirection.InOut : LInIndex >= 0 ? SQLDirection.In : SQLDirection.Out, ADevice.GetParameterMarker(LScalarType)));
				return true;  // return true if the parameter was handled
			}
			return false; // return false since the parameter was not handled
		}
		
		public static SQLParameters PrepareParameters(Plan APlan, SQLDevice ADevice, string AStatement, Schema.IRowType AInRowType, Schema.IRowType AOutRowType, PlanNode[] AConversionNodes)
		{
			SQLParameters LParameters = new SQLParameters();
			StringBuilder LParameterName = null;
			bool LInParameter = false;
			char LQuoteChar = '\0';
			for (int LIndex = 0; LIndex < AStatement.Length; LIndex++)
			{
				if (LInParameter && !IsValidIdentifierCharacter(AStatement[LIndex]))
				{
					if (HandleParameter(APlan, ADevice, LParameters, AConversionNodes, AInRowType, AOutRowType, LParameterName.ToString()))
						LInParameter = false;
				}
					
				switch (AStatement[LIndex])
				{
					case '@' :
						if (LQuoteChar == '\0') // if not inside of a string
						{
							LParameterName = new StringBuilder();
							LInParameter = true;
						}
					break;
					
					case '\'' :
					case '"' :
						if (LQuoteChar != '\0')
						{
							if (((LIndex + 1) >= AStatement.Length) || (AStatement[LIndex + 1] != LQuoteChar))
								LQuoteChar = '\0';
						}
						else
							LQuoteChar = AStatement[LIndex];
					break;
					default:
						if (LInParameter)
							LParameterName.Append(AStatement[LIndex]);
					break;
				}
			}
			
			// handle the param if it's the last thing on the statement
			if (LInParameter)
				HandleParameter(APlan, ADevice, LParameters, AConversionNodes, AInRowType, AOutRowType, LParameterName.ToString());

			return LParameters;
		}
		
		private static void SetValueNode(PlanNode APlanNode, DataValue AValue)
		{
			if (APlanNode is ValueNode)
				((ValueNode)APlanNode).Value = AValue.IsNil ? null : AValue.AsNative;
			else
				SetValueNode(APlanNode.Nodes[0], AValue);
		}
		
		public static void GetParameters(Program AProgram, SQLDevice ADevice, SQLParameters AParameters, Row AInValues, PlanNode[] AConversionNodes)
		{
			for (int LIndex = 0; LIndex < AParameters.Count; LIndex++)
			{
				switch (AParameters[LIndex].Direction)
				{
					case SQLDirection.InOut :
					case SQLDirection.In :
						int LInIndex = AInValues.DataType.Columns.IndexOf(AParameters[LIndex].Name);
						PlanNode LConversionNode = AConversionNodes[LInIndex];
						Schema.ScalarType LValueType = (Schema.ScalarType)(LConversionNode == null ? AInValues.DataType.Columns[LInIndex].DataType : LConversionNode.DataType);
						SQLScalarType LScalarType = ADevice.ResolveDeviceScalarType(AProgram.Plan, LValueType) as SQLScalarType;
						if (LScalarType == null)
							throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, LValueType.Name);

						if (AInValues.HasValue(LInIndex))
							if (LConversionNode == null)
								AParameters[LIndex].Value = LScalarType.ParameterFromScalar(AProgram.ValueManager, AInValues[LInIndex]);
							else
							{
								SetValueNode(LConversionNode, AInValues.GetValue(LInIndex));
								AParameters[LIndex].Value = LScalarType.ParameterFromScalar(AProgram.ValueManager, LConversionNode.Execute(AProgram));
							}
					break;
				}
			}
		}
		
		public static void SetParameters(Program AProgram, SQLDevice ADevice, SQLParameters AParameters, Row AOutValues)
		{
			for (int LIndex = 0; LIndex < AParameters.Count; LIndex++)
				switch (AParameters[LIndex].Direction)
				{
					case SQLDirection.InOut :
					case SQLDirection.Out :
						int LOutIndex = AOutValues.DataType.Columns.IndexOf(AParameters[LIndex].Name);
						Schema.ScalarType LValueType = (Schema.ScalarType)AOutValues.DataType.Columns[LOutIndex].DataType;
						SQLScalarType LScalarType = ADevice.ResolveDeviceScalarType(AProgram.Plan, LValueType) as SQLScalarType;
						if (LScalarType == null)
							throw new SchemaException(SchemaException.Codes.DeviceScalarTypeNotFound, LValueType.Name);

						if (AParameters[LIndex].Value != null)
							AOutValues[LOutIndex] = LScalarType.ParameterToScalar(AProgram.ValueManager, AParameters[LIndex].Value);
						else
							AOutValues.ClearValue(LOutIndex);
					break;
				}
		}

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
				string LDeviceName = String.Empty;
				string LStatement = String.Empty;
				Row LInValues = null;
				Row LOutValues = null;
				
				if (Operator.Operands[0].DataType.Is(Compiler.ResolveCatalogIdentifier(AProgram.Plan, "System.Name") as IDataType))
				{
					LDeviceName = (string)AArguments[0];
					LStatement = (string)AArguments[1];
					if (AArguments.Length >= 3)
						LInValues = (Row)AArguments[2];
					if (AArguments.Length == 4)
						LOutValues = (Row)AArguments[3];
				}
				else
				{
					LDeviceName = AProgram.Plan.DefaultDeviceName;
					LStatement = (string)AArguments[0];
					if (AArguments.Length >= 2)
						LInValues = (Row)AArguments[1];
					if (AArguments.Length == 3)
						LOutValues = (Row)AArguments[2];
				}

				SQLDevice LSQLDevice = SQLDeviceUtility.ResolveSQLDevice(AProgram.Plan, LDeviceName);
				SQLDeviceSession LDeviceSession = AProgram.DeviceConnect(LSQLDevice) as SQLDeviceSession;				
				PlanNode[] LConversionNodes = LInValues == null ? new PlanNode[0] : new PlanNode[LInValues.DataType.Columns.Count];
				SQLParameters LParameters = PrepareParameters(AProgram.Plan, LSQLDevice, LStatement, LInValues == null ? null : LInValues.DataType, LOutValues == null ? null : LOutValues.DataType, LConversionNodes);
				GetParameters(AProgram, LSQLDevice, LParameters, LInValues, LConversionNodes);
				LDeviceSession.SQLExecute(LStatement, LParameters);
				SetParameters(AProgram, LSQLDevice, LParameters, LOutValues);
				return null;
			}
			finally
			{
				AProgram.Statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			
			// determine the table type from the CLI call
			string LDeviceName = String.Empty;
			FStatement = String.Empty;
			Schema.RowType LInRowType = null;
			Schema.RowType LOutRowType = null;
			string LTableType = String.Empty;
			string LKeyDefinition = String.Empty;
			
			if (APlan.IsEngine && (Modifiers != null))
			{
				LTableType = LanguageModifiers.GetModifier(Modifiers, "TableType", LTableType);
				LKeyDefinition = LanguageModifiers.GetModifier(Modifiers, "KeyInfo", LKeyDefinition);
			}

			// ADeviceName and AStatement must be literal
			if (Nodes[0].DataType.Is(APlan.DataTypes.SystemName))
			{
				// NOTE: We are deliberately not using APlan.ExecuteLiteralArgument here because we want to throw the SQLException, not the CompilerException.
				if (!Nodes[0].IsLiteral)
					throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "ADeviceName");
				LDeviceName = (string)APlan.ExecuteNode(Nodes[0]);
					
				if (!Nodes[1].IsLiteral)
					throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AStatement");
				FStatement = (string)APlan.ExecuteNode(Nodes[1]);
				
				if (Nodes.Count >= 3)
				{
					if (Nodes[2].DataType.Is(APlan.DataTypes.SystemString))
					{
						if (!Nodes[2].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						LKeyDefinition = (string)APlan.ExecuteNode(Nodes[2]);
					}
					else
						LInRowType = Nodes[2].DataType as Schema.RowType;
				}	
					
				if (Nodes.Count >= 4)
				{
					if (Nodes[3].DataType.Is(APlan.DataTypes.SystemString))
					{
						if (!Nodes[3].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						LKeyDefinition = (string)APlan.ExecuteNode(Nodes[3]);
					}
					else
						LOutRowType = Nodes[3].DataType as Schema.RowType;
				}
				
				if (Nodes.Count == 5)
				{
					if (!Nodes[4].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					LKeyDefinition = (string)APlan.ExecuteNode(Nodes[4]);
				}
				else if (Nodes.Count == 6)
				{
					if (!Nodes[4].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "ATableType");
					LTableType = (string)APlan.ExecuteNode(Nodes[4]);
					
					if (!Nodes[5].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					LKeyDefinition = (string)APlan.ExecuteNode(Nodes[5]);
				}
			}
			else
			{
				LDeviceName = APlan.DefaultDeviceName;
				if (!Nodes[0].IsLiteral)
					throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AStatement");
				FStatement = (string)APlan.ExecuteNode(Nodes[0]);
				
				if (Nodes.Count >= 2)
				{
					if (Nodes[1].DataType.Is(APlan.DataTypes.SystemString))
					{
						if (!Nodes[1].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						LKeyDefinition = (string)APlan.ExecuteNode(Nodes[1]);
					}
					else
						LInRowType = Nodes[1].DataType as Schema.RowType;
				}	
					
				if (Nodes.Count >= 3)
				{
					if (Nodes[2].DataType.Is(APlan.DataTypes.SystemString))
					{
						if (!Nodes[2].IsLiteral)
							throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
						LKeyDefinition = (string)APlan.ExecuteNode(Nodes[2]);
					}
					else
						LOutRowType = Nodes[2].DataType as Schema.RowType;
				}
				
				if (Nodes.Count == 4)
				{
					if (!Nodes[3].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					LKeyDefinition = (string)APlan.ExecuteNode(Nodes[3]);
				}
				else if (Nodes.Count == 5)
				{
					if (!Nodes[3].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "ATableType");
					LTableType = (string)APlan.ExecuteNode(Nodes[3]);
					
					if (!Nodes[4].IsLiteral)
						throw new SQLException(SQLException.Codes.ArgumentMustBeLiteral, "AKeyInfo");
					LKeyDefinition = (string)APlan.ExecuteNode(Nodes[4]);
				}
			}
			
			CursorCapabilities = APlan.CursorContext.CursorCapabilities;
			CursorIsolation = APlan.CursorContext.CursorIsolation;
			CursorType = APlan.CursorContext.CursorType;

			SQLDeviceSession LDeviceSession = null;
			if (!APlan.IsEngine)
			{			
				FSQLDevice = SQLDeviceUtility.ResolveSQLDevice(APlan, LDeviceName);
				LDeviceSession = APlan.DeviceConnect(FSQLDevice) as SQLDeviceSession;
				FConversionNodes = LInRowType == null ? new PlanNode[0] : new PlanNode[LInRowType.Columns.Count];
				FParameters = SQLExecuteNode.PrepareParameters(APlan, FSQLDevice, FStatement, LInRowType, LOutRowType, FConversionNodes);
			}
			
			if (LTableType == String.Empty)
			{
				SQLCursor LCursor = 
					LDeviceSession.Connection.Open
					(
						FStatement, 
						FParameters, 
						SQLTable.CursorTypeToSQLCursorType(CursorType), 
						SQLIsolationLevel.ReadUncommitted,
						SQLCommandBehavior.SchemaOnly | (LKeyDefinition == String.Empty ? SQLCommandBehavior.KeyInfo : SQLCommandBehavior.Default)
					);
				try
				{
					bool LMessageReported = false;
					SQLTableSchema LSchema = null;
					try
					{
						LSchema = LCursor.Schema;
					}
					catch (Exception LException)
					{
						// An error here should be ignored, just get as much schema information as possible from the actual cursor...
						// Not as efficient, but the warning lets them know that.
						APlan.Messages.Add(new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.Warning, LException, "Compile-time schema retrieval failed, attempting run-time schema retrieval."));
						LMessageReported = true;
					}
					
					if ((LSchema == null) || (LSchema.Columns.Count == 0))
					{
						if (!LMessageReported)
							APlan.Messages.Add(new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.Warning, "Compile-time schema retrieval failed, attempting run-time schema retrieval."));

						if (LCursor != null)
						{
							LCursor.Dispose();
							LCursor = null;
						}
						
						LCursor = 
							LDeviceSession.Connection.Open
							(
								FStatement,
								FParameters,
								SQLTable.CursorTypeToSQLCursorType(CursorType),
								SQLIsolationLevel.ReadUncommitted,
								SQLCommandBehavior.Default
							);
							
						LSchema = LCursor.Schema;
					}
					
					FDataType = new Schema.TableType();
					FTableVar = new Schema.ResultTableVar(this);
					FTableVar.Owner = APlan.User;
					FSQLColumns = new SQLTableColumns();
					foreach (SQLColumn LSQLColumn in LSchema.Columns)
					{
						Schema.Column LColumn = new Schema.Column(LSQLColumn.Name, FSQLDevice.FindScalarType(APlan, LSQLColumn.Domain));
						DataType.Columns.Add(LColumn);
						FTableVar.Columns.Add(new Schema.TableVarColumn(LColumn, Schema.TableVarColumnType.Stored));
					}
					
					FSQLDevice.CheckSupported(APlan, FTableVar);
					foreach (Schema.TableVarColumn LTableVarColumn in FTableVar.Columns)
						FSQLColumns.Add(new SQLTableColumn(LTableVarColumn, (Schema.DeviceScalarType)FSQLDevice.ResolveDeviceScalarType(APlan, (Schema.ScalarType)LTableVarColumn.Column.DataType)));
						
					if (LKeyDefinition == String.Empty)
					{
						foreach (SQLIndex LSQLIndex in LSchema.Indexes)
						{
							if (LSQLIndex.IsUnique)
							{
								Schema.Key LKey = new Schema.Key();
								foreach (SQLIndexColumn LSQLIndexColumn in LSQLIndex.Columns)
									LKey.Columns.Add(FTableVar.Columns[LSQLIndexColumn.Name]);
									
								FTableVar.Keys.Add(LKey);
							}
						}
					}
					else
					{
						FTableVar.Keys.Add(Compiler.CompileKeyDefinition(APlan, FTableVar, new D4.Parser().ParseKeyDefinition(LKeyDefinition)));
					}
				}
				finally
				{
					LCursor.Dispose();
				}
			}
			else
			{
				FDataType = Compiler.CompileTypeSpecifier(APlan, new D4.Parser().ParseTypeSpecifier(LTableType)) as Schema.TableType;
				if (FDataType == null)
					throw new CompilerException(CompilerException.Codes.TableTypeExpected);
				FTableVar = new Schema.ResultTableVar(this);
				FTableVar.Owner = APlan.User;
				FSQLColumns = new SQLTableColumns();

				foreach (Schema.Column LColumn in DataType.Columns)
					TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

				if (!APlan.IsEngine)
				{				
					FSQLDevice.CheckSupported(APlan, FTableVar);
					foreach (Schema.TableVarColumn LTableVarColumn in FTableVar.Columns)
						FSQLColumns.Add(new SQLTableColumn(LTableVarColumn, (Schema.DeviceScalarType)FSQLDevice.ResolveDeviceScalarType(APlan, (Schema.ScalarType)LTableVarColumn.Column.DataType)));
				}
					
				FTableVar.Keys.Add(Compiler.CompileKeyDefinition(APlan, FTableVar, new D4.Parser().ParseKeyDefinition(LKeyDefinition)));
			}
			
			Compiler.EnsureKey(APlan, FTableVar);
			
			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
				
			if (!APlan.IsEngine)
			{
				if (Modifiers == null)
					Modifiers = new LanguageModifiers();
				D4.D4TextEmitter LEmitter = new D4.D4TextEmitter();
				Modifiers.AddOrUpdate("TableType", LEmitter.Emit(FTableVar.DataType.EmitSpecifier(D4.EmitMode.ForCopy)));
				Modifiers.AddOrUpdate("KeyInfo", LEmitter.Emit(Compiler.FindClusteringKey(APlan, FTableVar).EmitStatement(D4.EmitMode.ForCopy)));
			}
		}

		private SQLDevice FSQLDevice;
		private string FStatement;		
		private PlanNode[] FConversionNodes;
		private SQLParameters FParameters;
		private SQLTableColumns FSQLColumns;
		
		public override object InternalExecute(Program AProgram)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
				Row LInValues = null;
				Row LOutValues = null;
				
				if (Nodes[0].DataType.Is(Compiler.ResolveCatalogIdentifier(AProgram.Plan, "System.Name", true) as IDataType))
				{
					if ((Nodes.Count >= 3) && (Nodes[2].DataType is Schema.IRowType))
						LInValues = (Row)Nodes[2].Execute(AProgram);
					
					if ((Nodes.Count >= 4) && (Nodes[3].DataType is Schema.IRowType))
						LOutValues = (Row)Nodes[3].Execute(AProgram);
				}
				else
				{
					if ((Nodes.Count >= 2) && (Nodes[1].DataType is Schema.IRowType))
						LInValues = (Row)Nodes[1].Execute(AProgram);
						
					if ((Nodes.Count == 3) && (Nodes[2].DataType is Schema.IRowType))
						LOutValues = (Row)Nodes[2].Execute(AProgram);
				}

				SQLDeviceSession LDeviceSession = AProgram.DeviceConnect(FSQLDevice) as SQLDeviceSession;				
				SQLExecuteNode.GetParameters(AProgram, FSQLDevice, FParameters, LInValues, FConversionNodes);
				
				LocalTable LResult = new LocalTable(this, AProgram);
				try
				{
					LResult.Open();

					// Populate the result
					Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
					try
					{
						LRow.ValuesOwned = false;
						using 
						(
							SQLCursor LCursor = 
								LDeviceSession.Connection.Open
								(
									FStatement, 
									FParameters, 
									SQLTable.CursorTypeToSQLCursorType(CursorType), 
									SQLTable.CursorIsolationToSQLIsolationLevel(CursorIsolation, AProgram.ServerProcess.CurrentIsolationLevel()), 
									SQLCommandBehavior.Default
								)
						)
						{
							SQLExecuteNode.SetParameters(AProgram, FSQLDevice, FParameters, LOutValues);
							
							while (LCursor.Next())
							{
								for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
								{
									if (LCursor.IsNull(LIndex))
										LRow.ClearValue(LIndex);
									else
										LRow[LIndex] = FSQLColumns[LIndex].ScalarType.ToScalar(AProgram.ValueManager, LCursor[LIndex]);
								}
								
								LResult.Insert(LRow);
							}
						}
					}
					finally
					{
						LRow.Dispose();
					}
					
					LResult.First();
					
					return LResult;
				}
				catch
				{
					LResult.Dispose();
					throw;
				}
			}
			finally
			{
				AProgram.Statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
		}
    }
    
    // operator AvailableTables() : table { Name : Name, StorageName : String };
    // operator AvailableTables(const ADeviceName : System.Name) : table { Name : Name, StorageName : String };
    public class AvailableTablesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("StorageName", APlan.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateAvailableTables(Program AProgram, SQLDevice ADevice, Table ATable, Row ARow)
		{
			Schema.Catalog LServerCatalog = ADevice.GetServerCatalog(AProgram.ServerProcess, null);
			Schema.Catalog LCatalog = new Schema.Catalog();
			ADevice.GetDeviceTables(AProgram.Plan, LServerCatalog, LCatalog, null);
			foreach (Schema.Object LObject in LCatalog)
			{
				Schema.BaseTableVar LTableVar = LObject as Schema.BaseTableVar;
				if (LTableVar != null)
				{
					ARow[0] = LTableVar.Name;
					ARow[1] = LTableVar.MetaData.Tags["Storage.Name"].Value;
					ATable.Insert(ARow);
				}
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			string LDeviceName = (Nodes.Count == 0) ? AProgram.Plan.DefaultDeviceName : (string)Nodes[0].Execute(AProgram);
			SQLDevice LSQLDevice = SQLDeviceUtility.ResolveSQLDevice(AProgram.Plan, LDeviceName);
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					PopulateAvailableTables(AProgram, LSQLDevice, LResult, LRow);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    // operator AvailableReferences() : table { Name : Name, StorageName : String };
    // operator AvailableReferences(const ADeviceName : System.Name) : table { Name : Name, StorageName : String };
    public class AvailableReferencesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("StorageName", APlan.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateAvailableReferences(Program AProgram, SQLDevice ADevice, Table ATable, Row ARow)
		{
			Schema.Catalog LServerCatalog = ADevice.GetServerCatalog(AProgram.ServerProcess, null);
			Schema.Catalog LCatalog = new Schema.Catalog();
			ADevice.GetDeviceForeignKeys(AProgram.Plan, LServerCatalog, LCatalog, null);
			foreach (Schema.Object LObject in LCatalog)
			{
				Schema.Reference LReference = LObject as Schema.Reference;
				if (LReference != null)
				{
					ARow[0] = LReference.Name;
					ARow[1] = LReference.MetaData.Tags["Storage.Name"].Value;
					ATable.Insert(ARow);
				}
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			string LDeviceName = (Nodes.Count == 0) ? AProgram.Plan.DefaultDeviceName : (string)Nodes[0].Execute(AProgram);
			SQLDevice LSQLDevice = SQLDeviceUtility.ResolveSQLDevice(AProgram.Plan, LDeviceName);
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					PopulateAvailableReferences(AProgram, LSQLDevice, LResult, LRow);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    // operator DeviceReconciliationScript(const ADeviceName : Name) : String;
    // operator DeviceReconciliationScript(const ADeviceName : Name, const AOptions : list(String)) : String;
    // operator DeviceReconciliationScript(const ADeviceName : Name, const ATableName : Name, const AOptions : list(String)) : String;
    public class DeviceReconciliationScriptNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SQLDevice LSQLDevice = SQLDeviceUtility.ResolveSQLDevice(AProgram.Plan, (string)AArguments[0]);
			string LTableName = String.Empty;
			ReconcileOptions LOptions = ReconcileOptions.All;
			switch (AArguments.Length)
			{
				case 2 :
					if (Operator.Operands[1].DataType.Is(AProgram.DataTypes.SystemName))
						LTableName = (string)AArguments[1];
					else
						LOptions = SQLDeviceUtility.ResolveReconcileOptions(AArguments[1] as ListValue);
				break;
				
				case 3 :
					LTableName = (string)AArguments[1];
					LOptions = SQLDeviceUtility.ResolveReconcileOptions(AArguments[2] as ListValue);
				break;
			}
			
			Batch LBatch;
			if (LTableName == String.Empty)
				LBatch = LSQLDevice.DeviceReconciliationScript(AProgram.ServerProcess, LOptions);
			else
				LBatch = LSQLDevice.DeviceReconciliationScript(AProgram.ServerProcess, Compiler.ResolveCatalogIdentifier(AProgram.Plan, LTableName, true) as Schema.TableVar, LOptions);
				
			return LSQLDevice.Emitter.Emit(LBatch);
		}
    }
}

