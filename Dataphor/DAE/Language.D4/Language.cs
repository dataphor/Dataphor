/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define ALLOWARBITRARYAGGREGATEEXPRESSIONS

namespace Alphora.Dataphor.DAE.Language.D4
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using Language;
	
	/*
		Class Hierarchy ->

			System.Object
				|- MetaData
				|- AlterMetaData
				|- Tag
						
			Language.Statement
				|- ExpressionStatement
				|- VariableStatement
				|- AssignmentStatement
				|- D4Statement
				|	|- D4DMLStatement
				|	|	|- SelectStatement
				|	|	|- InsertStatement
				|	|	|- UpdateStatement
				|	|	|- DeleteStatement
				|	|- CreateTableVarStatement
				|	|	|- CreateTableStatement
				|	|	|- CreateViewStatement
				|	|- CreateScalarTypeStatement
				|	|- CreateOperatorStatementBase
				|	|	|- CreateOperatorStatement
				|	|	|- CreateAggregateOperatorStatement
				|	|- CreateServerStatement
				|	|- CreateDeviceStatement
				|	|- CreateConversionStatement
				|	|- AlterTableVarStatement
				|	|	|- AlterTableStatement
				|	|	|- AlterViewStatement
				|	|- AlterScalarTypeStatement
				|	|- AlterOperatorStatementBase
				|	|	|- AlterOperatorStatement
				|	|	|- AlterAggregateOperatorStatement
				|	|- AlterServerStatement
				|	|- AlterDeviceStatement
				|	|- DropObjectStatement
				|	|	|- DropTableStatement
				|	|	|- DropViewStatement
				|	|	|- DropScalarTypeStatement
				|	|	|- DropOperatorStatement
				|	|	|- DropServerStatement
				|	|	|- DropDeviceStatement
				|	|- DropConversionStatement
				|	|- AttachStatementBase
				|	|	|- AttachStatement
				|	|	|- DetachStatement
				|	|- RightStatementBase
				|	|	|- GrantStatement
				|	|	|- RevokeStatement
				|	|	|- RevertStatement
				|	|- CatalogObjectSpecifier
				|	|- ScalarTypeNameDefinition
				|	|- RepresentationDefinitionBase
				|	|	|- RepresentationDefinition
				|	|	|- AlterRepresentationDefinition
				|	|	|- DropRepresentationDefinition
				|	|- PropertyDefinitionBase
				|	|	|- PropertyDefinition
				|	|	|- AlterPropertyDefinition
				|	|	|- DropPropertyDefinition
				|	|- SpecialDefinitionBase
				|	|	|- SpecialDefinition
				|	|	|- AlterSpecialDefiniton
				|	|	|- DropSpecialDefinition
				|	|- SortDefinitionBase
				|	|	|- SortDefinition
				|	|	|	|- CreateSortStatement
				|	|	|- AlterSortDefinition
				|	|	|	|- AlterSortStatement
				|	|	|- DropSortDefinition
				|	|	|	|- DropSortStatement
				|	|- ColumnDefinitionBase
				|	|	|- ColumnDefinition
				|	|	|- AlterColumnDefinition
				|	|	|- DropColumnDefinition
				|	|	|- KeyColumnDefinition
				|	|	|- ReferenceColumnDefinition
				|	|	|- OrderColumnDefinition
				|	|	|- IndexColumnDefinition
				|	|- KeyDefinitionBase
				|	|	|- KeyDefinition
				|	|	|- AlterKeyDefinition
				|	|	|- DropKeyDefinition
				|	|- ReferenceDefinitionBase
				|	|	|- ReferenceDefinition
				|	|	|	|- CreateReferenceStatement
				|	|	|- AlterReferenceDefinition
				|	|	|	|- AlterReferenceStatement
				|	|	|- DropReferenceDefinition
				|	|	|	|- DropReferenceStatement
				|	|- ReferencesDefinition
				|	|- OrderDefinitionBase
				|	|	|- OrderDefinition
				|	|	|- AlterOrderDefinition
				|	|	|- DropOrderDefinition
				|	|- IndexDefinitionBase
				|	|	|- IndexDefinition
				|	|	|- AlterIndexDefinition
				|	|	|- DropIndexDefinition
				|	|- ConstraintDefinitionBase
				|	|	|- ConstraintDefinition
				|	|	|	|- CreateConstraintStatement
				|	|	|- AlterConstraintDefinition
				|	|	|	|- AlterConstraintStatement
				|	|	|- DropConstraintDefinition
				|	|	|	|- DropConstraintStatement
				|	|- DefaultDefinitionBase
				|	|	|- DefaultDefinition
				|	|	|- AlterDefaultDefinition
				|	|	|- DropDefaultDefinition
				|	|- ClassDefinition
				|	|- AlterClassDefinition
				|	|- ClassAttributeDefinition
				|	|- OperatorSpecifier
				|	|- NamedTypeSpecifier
				|	|	|- FormalParameter
				|	|- FormalParameterSpecifier
				|	|- TypeSpecifier
				|	|	|- GenericTypeSpecifier
				|	|	|- ScalarTypeSpecifier
				|	|	|- RowTypeSpecifier
				|	|	|- TableTypeSpecifier
				|	|	|- ListTypeSpecifier
				|	|	|- CursorTypeSpecifier
				|	|	|- OperatorTypeSpecifier
				|	|	|- TypeOfTypeSpecifier
				|	|- DeviceMapItem
				|	|	|- DeviceScalarTypeMapBase
				|	|	|	|- DeviceScalarTypeMap
				|	|	|	|- AlterDeviceScalarTypeMap
				|	|	|	|- DropDeviceScalarTypeMap
				|	|	|- DeviceOperatorMapBase
				|	|	|	|- DeviceOperatorMap
				|	|	|	|- AlterDeviceOperatorMap
				|	|	|	|- DropDeviceOperatorMap
				|	|	|- DeviceStoreDefinitionBase
				|	|	|	|- DeviceStoreDefinition
				|	|	|	|- AlterDeviceStoreDefinition
				|	|	|	|- DropDeviceStoreDefinition
				|	|- EventSourceSpecifier
				|	|	|- ObjectEventSourceSpecifier
				|	|	|- ColumnEventSourceSpecifier
				|	|- EventSpecifier
				|	|	|- ModificationEventSpecifier
				|	|	|- ProposableEventSpecifier
				
			Language.Expression
				|- Language.IdentifierExpression
				|	|- TableIdentifierExpression
				|	|- ColumnIdentifierExpression
				|	|- ServerIdentifierExpression
				|	|- VariableIdentifierExpression
				|- AdornExpression
				|- AdornColumnExpression
				|- UpdateColumnExpression
				|- RedefineExpression
				|- OnExpression
				|- AsExpression
				|- AsExpression
				|- IsExpression
				|- TableSelectorExpressionBase
				|	|- TableSelectorExpression
				|	|- PresentationSelectorExpression
				|- RowSelectorExpressionBase
				|	|- RowSelectorExpression
				|	|- EntrySelectorExpression
				|- RowExtractorExpressionBase
				|	|- RowExtractorExpression
				|	|- EntryExtractorExpression
				|- ColumnExtractorExpression
				|- ListSelectorExpression
				|- CursorSelectorExpression
				|- CursorDefinition
				|- RestrictExpression
				|- ColumnExpression
				|- ProjectExpression
				|- RemoveExpression
				|- NamedColumnExpression
				|- ExtendExpression
				|- RenameColumnExpression
				|- RenameExpression
				|- RenameAllExpression
				|- AggregateColumnExpression
				|- AggregateExpression
				|- BaseOrderExpression
				|	|- OrderExpression
				|	|- BrowseExpression
				|- QuotaExpression
				|- ExplodeColumnExpression
				|- IncludeColumnExpression
				|- ExplodeExpression
				|- BinaryTableExpression
				|	|- UnionExpression
				|	|- IntersectExpression
				|	|- DifferenceExpression
				|	|- ProductExpression
				|	|- DivideExpression
				|	|- SemiExpression
				|	|	|- HavingExpression
				|	|	|- WithoutExpression
				|	|- JoinExpression
				|	|	|- InnerJoinExpression
				|	|	|- OuterJoinExpression
				|	|	|	|- LeftOuterJoinExpression
				|	|	|	|- RightOuterJoinExpression
		
	*/
	
	public enum EmitMode 
	{ 
		/// <summary>ForCopy indicates that the emission is to be used to make a copy of the object.</summary>
		ForCopy, 
		
		/// <summary>ForStorage indicates that the emission is to be used to serialize the object in the persistent catalog store.</summary>
		ForStorage,
		
		/// <summary>ForRemote indicates that the emission is to be used to transmit the object to a client-side catalog cache.</summary>
		ForRemote
	}

	/// <remarks>The IdentifierExpression descendents are only used by the CLI to provide a more detailed ParseTree when requested.</remarks>
	public class TableIdentifierExpression : IdentifierExpression
	{
		public TableIdentifierExpression() : base(){}
		public TableIdentifierExpression(string identifier) : base(identifier){}
	}
	
	public class ColumnIdentifierExpression : IdentifierExpression
	{
		public ColumnIdentifierExpression() : base(){}
		public ColumnIdentifierExpression(string identifier) : base(identifier){}
	}
	
	public class ServerIdentifierExpression : IdentifierExpression
	{
		public ServerIdentifierExpression() : base(){}
		public ServerIdentifierExpression(string identifier) : base(identifier){}
	}
	
	public class VariableIdentifierExpression : IdentifierExpression
	{
		public VariableIdentifierExpression() : base(){}
		public VariableIdentifierExpression(string identifier) : base(identifier){}
	}
	
	public abstract class D4Statement : Statement{}

	public abstract class D4DMLStatement : D4Statement {}

	// Verified against DataphorMachine - BTR - 11/24/2001
	public class SelectStatement : D4DMLStatement
	{
		public SelectStatement() : base(){}
		public SelectStatement(CursorDefinition cursorDefinition) : base()
		{
			_cursorDefinition = cursorDefinition;
		}
		
		// CursorDefinition
		protected CursorDefinition _cursorDefinition;
		public CursorDefinition CursorDefinition
		{
			get { return _cursorDefinition; }
			set { _cursorDefinition = value; }
		}
	}
	
	public class InsertStatement : D4DMLStatement
	{
		public InsertStatement() : base(){}
		public InsertStatement(Expression sourceExpression, Expression target) : base()
		{
			_sourceExpression = sourceExpression;
			_target = target;
		}
		
		// SourceExpression
		protected Expression _sourceExpression;
		public Expression SourceExpression
		{
			get { return _sourceExpression; }
			set { _sourceExpression = value; }
		}
		
		// Target
		protected Expression _target;
		public Expression Target
		{
			get { return _target; }
			set { _target = value; }
		}
	}
	
	public class UpdateColumnExpression : Expression
	{
		public UpdateColumnExpression() : base(){}
		public UpdateColumnExpression(Expression target, Expression expression) : base()
		{
			Target = target;
			Expression = expression;
		}
		
		// Target
		protected Expression _target;
		public Expression Target
		{
			get { return _target; }
			set { _target = value; }
		}

		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class UpdateColumnExpressions : Expressions
	{
		protected override void Validate(object item)
		{
			if (!(item is UpdateColumnExpression))
				throw new LanguageException(LanguageException.Codes.UpdateColumnExpressionContainer);
			base.Validate(item);
		}
		
		public new UpdateColumnExpression this[int index]
		{
			get { return (UpdateColumnExpression)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class UpdateStatement : D4DMLStatement
	{
		public UpdateStatement() : base(){}
		public UpdateStatement(Expression target)
		{
			_target = target;
		}
		
		public UpdateStatement(Expression target, UpdateColumnExpression[] columns)
		{
			_target = target;
			_columns.AddRange(columns);
		}
		
		public UpdateStatement(Expression target, UpdateColumnExpression[] columns, Expression condition)
		{
			_target = target;
			_columns.AddRange(columns);
			_condition = condition;
		}
		
		// Target
		protected Expression _target;
		public Expression Target
		{
			get { return _target; }
			set { _target = value; }
		}
		
		// Columns
		protected UpdateColumnExpressions _columns = new UpdateColumnExpressions();
		public UpdateColumnExpressions Columns { get { return _columns; } }
		
		// Condition
		protected Expression _condition;
		public Expression Condition
		{
			get { return _condition; }
			set { _condition = value; }
		}
	}
	
	public class DeleteStatement : D4DMLStatement
	{
		public DeleteStatement() : base(){}
		public DeleteStatement(Expression target) : base()
		{
			_target = target;
		}
		
		// Target
		protected Expression _target;
		public Expression Target
		{
			get { return _target; }
			set { _target = value; }
		}
	}
	
	public class RestrictExpression : Expression
	{
		public RestrictExpression() : base(){}
		public RestrictExpression(Expression expression, Expression condition)
		{
			_expression = expression;
			_condition = condition;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// Condition
		protected Expression _condition;
		public Expression Condition
		{
			get { return _condition; }
			set { _condition = value; }
		}
	}

	public class OnExpression : Expression
	{		
		public OnExpression() : base(){}
		public OnExpression(Expression expression, string serverName)
		{
			_expression = expression;
			_serverName = serverName;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// ServerName
		protected string _serverName = String.Empty;
		public string ServerName
		{
			get { return _serverName; }
			set { _serverName = value == null ? String.Empty : value; }
		}
	}
	
	public class AsExpression : Expression
	{
		public AsExpression() : base(){}
		public AsExpression(Expression expression, TypeSpecifier typeSpecifier) : base()
		{
			_expression = expression;
			_typeSpecifier = typeSpecifier;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
	}
	
	public class IsExpression : Expression
	{
		public IsExpression() : base(){}
		public IsExpression(Expression expression, TypeSpecifier typeSpecifier) : base()
		{
			_expression = expression;
			_typeSpecifier = typeSpecifier;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
	}
	
	public class ColumnExpression : Expression
	{
		public ColumnExpression() : base(){}
		public ColumnExpression(string columnName) : base()
		{
			ColumnName = columnName;
		}
		
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}
	}
	
	public class ColumnExpressions : Expressions
	{
		protected override void Validate(object item)
		{
			if (!(item is ColumnExpression))
				throw new LanguageException(LanguageException.Codes.ColumnExpressionContainer);
			base.Validate(item);
		}
		
		public new ColumnExpression this[int index]
		{
			get { return (ColumnExpression)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class ProjectExpression : Expression
	{
		public ProjectExpression() : base() {}
		public ProjectExpression(Expression expression, string[] columnNames) : base()
		{
			_expression = expression;
			for (int index = 0; index < columnNames.Length; index++)
				_columns.Add(new ColumnExpression(columnNames[index]));
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Columns
		protected ColumnExpressions _columns = new ColumnExpressions();
		public ColumnExpressions Columns { get { return _columns; } }
	}

	#if CALCULESQUE    
	public class NamedExpression : Expression
	{
		public NamedExpression() : base() {}
		public NamedExpression(Expression AExpression, string AName) : base()
		{
			FExpression = AExpression;
			FName = AName;
		}

		// Expression
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}

		// Name
		protected string FName = String.Empty;
		public string Name
		{
			get { return FName; }
			set { FName = value == null ? String.Empty : value; }
		}
	}
	#endif
	
	public class NamedColumnExpression : Expression, IMetaData
	{
		public NamedColumnExpression() : base(){}
		public NamedColumnExpression(Expression expression, string columnAlias) : base()
		{
			ColumnAlias = columnAlias;
			_expression = expression;
		}
		
		public NamedColumnExpression(Expression expression, string columnAlias, MetaData metaData) : base()
		{
			ColumnAlias = columnAlias;
			_expression = expression;
			_metaData = metaData;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// ColumnAlias
		protected string _columnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return _columnAlias; }
			set { _columnAlias = value == null ? String.Empty : value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class NamedColumnExpressions : Expressions
	{
		protected override void Validate(object item)
		{
			if (!(item is NamedColumnExpression))
				throw new LanguageException(LanguageException.Codes.NamedColumnExpressionContainer);
			base.Validate(item);
		}
		
		public new NamedColumnExpression this[int index]
		{
			get { return (NamedColumnExpression)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class ExtendExpression : Expression
	{
		public ExtendExpression() : base(){}
		public ExtendExpression(Expression expression) : base()
		{
			_expression = expression;
		}
		
		public ExtendExpression(Expression expression, NamedColumnExpression[] expressions) : base()
		{
			_expression = expression;
			_expressions.AddRange(expressions);
		}

		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Expressions
		protected NamedColumnExpressions _expressions = new NamedColumnExpressions();
		public NamedColumnExpressions Expressions { get { return _expressions; } }
	}
	
	public class SpecifyExpression : Expression
	{
		public SpecifyExpression() : base(){}
		public SpecifyExpression(Expression expression) : base()
		{
			_expression = expression;
		}
		
		public SpecifyExpression(Expression expression, NamedColumnExpression[] expressions) : base()
		{
			_expression = expression;
			_expressions.AddRange(expressions);
		}

		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Expressions
		protected NamedColumnExpressions _expressions = new NamedColumnExpressions();
		public NamedColumnExpressions Expressions { get { return _expressions; } }
	}
	
	public class ListSelectorExpression : Expression
	{
		public ListSelectorExpression() : base(){}
		public ListSelectorExpression(Expression[] expressions) : base()
		{
			_expressions.AddRange(expressions);
		}
		
		public ListSelectorExpression(TypeSpecifier typeSpecifier, Expression[] expressions) : base()
		{
			_typeSpecifier = typeSpecifier;
			_expressions.AddRange(expressions);
		}
		
		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}

		// Expressions
		protected Expressions _expressions = new Expressions();
		public Expressions Expressions { get { return _expressions; } }
	}
	
	public class CursorDefinition : Expression
	{
		public CursorDefinition() : base(){}
		public CursorDefinition(Expression expression) : base()
		{
			_expression = expression;
		}
		
		public CursorDefinition(Expression expression, CursorCapability capabilities) : base()
		{
			_expression = expression;
			_capabilities = capabilities;
		}
		
		public CursorDefinition(Expression expression, CursorCapability capabilities, CursorIsolation isolation) : base()
		{
			_expression = expression;
			_capabilities = capabilities;
			_isolation = isolation;
		}
		
		public CursorDefinition(Expression expression, CursorCapability capabilities, CursorIsolation isolation, CursorType cursorType) : base()
		{
			_expression = expression;
			_capabilities = capabilities;
			_isolation = isolation;
			_cursorType = cursorType;
		}
		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		protected CursorCapability _capabilities = 0;
		public CursorCapability Capabilities
		{
			get { return _capabilities; }
			set { _capabilities = value; }
		}

		protected CursorIsolation _isolation = CursorIsolation.None;
		public CursorIsolation Isolation
		{
			get { return _isolation; }
			set { _isolation = value; }
		}
		
		protected bool _specifiesType;
		public bool SpecifiesType
		{
			get { return _specifiesType; }
			set { _specifiesType = value; }
		}
		
		protected CursorType _cursorType = CursorType.Dynamic;
		public CursorType CursorType
		{
			get { return _cursorType; }
			set { _cursorType = value; }
		}
	}
	
	public class CursorSelectorExpression : Expression
	{
		public CursorSelectorExpression() : base(){}
		public CursorSelectorExpression(CursorDefinition cursorDefinition) : base()
		{
			_cursorDefinition = cursorDefinition;
		}

		// CursorDefinition
		protected CursorDefinition _cursorDefinition;
		public CursorDefinition CursorDefinition
		{
			get { return _cursorDefinition; }
			set { _cursorDefinition = value; }
		}
	}
	
	public class ForEachStatement : Statement 
	{
		protected bool _isAllocation;
		/// <summary>Indicates whether the variable exists in the current stack window, or should be allocated by the statement</summary>
		public bool IsAllocation
		{
			get { return _isAllocation; }
			set { _isAllocation = value; }
		}
		
		protected string _variableName = String.Empty;
		/// <summary>The name of the variable that will receive the value for each successive iteration. If variable name is empty, this is a row foreach statement.</summary>
		public string VariableName
		{
			get { return _variableName; }
			set { _variableName = value == null ? String.Empty : value; }
		}
		
		protected CursorDefinition _expression;
		/// <summary>The list or cursor to be iterated over.</summary>
		public CursorDefinition Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		protected Statement _statement;
		/// <summary>The iterative statement to be executed.</summary>
		public Statement Statement
		{
			get { return _statement; }
			set { _statement = value; }
		}
	}
	
	public class ColumnExtractorExpression : Expression
	{		
		public ColumnExtractorExpression() : base(){}
		public ColumnExtractorExpression(string columnName, Expression expression) : base()
		{
			_columns.Add(new ColumnExpression(columnName));
			_expression = expression;
		}
		
		protected ColumnExpressions _columns = new ColumnExpressions();
		public ColumnExpressions Columns { get { return _columns; } }
		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// HasByClause
		protected bool _hasByClause;
		public bool HasByClause
		{
			get { return _hasByClause; }
			set { _hasByClause = value; }
		}
		
		// OrderColumns
		protected OrderColumnDefinitions _orderColumns = new OrderColumnDefinitions();
		public OrderColumnDefinitions OrderColumns { get { return _orderColumns; } }
	}
	
	public class RowExtractorExpressionBase : Expression
	{
		public RowExtractorExpressionBase() : base(){}
		public RowExtractorExpressionBase(Expression expression) : base()
		{
			_expression = expression;
		}
		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class RowExtractorExpression : RowExtractorExpressionBase
	{
		public RowExtractorExpression() : base(){}
		public RowExtractorExpression(Expression expression) : base(expression){}
	}
	
	public class EntryExtractorExpression : RowExtractorExpressionBase
	{
		public EntryExtractorExpression() : base(){}
		public EntryExtractorExpression(Expression expression) : base(expression){}
	}
	
	public class RowSelectorExpressionBase : Expression
	{
		public RowSelectorExpressionBase() : base(){}
		public RowSelectorExpressionBase(NamedColumnExpression[] expressions) : base()
		{
			_expressions.AddRange(expressions);
		}

		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}

		// Expressions
		protected NamedColumnExpressions _expressions = new NamedColumnExpressions();
		public NamedColumnExpressions Expressions { get { return _expressions; } }
	}
	
	public class RowSelectorExpression : RowSelectorExpressionBase
	{
		public RowSelectorExpression() : base(){}
		public RowSelectorExpression(NamedColumnExpression[] columns) : base(columns){}
	}
	
	public class EntrySelectorExpression : RowSelectorExpressionBase
	{
		public EntrySelectorExpression() : base(){}
		public EntrySelectorExpression(NamedColumnExpression[] columns) : base(columns){}
	}
	
	public class TableSelectorExpressionBase : Expression
	{
		public TableSelectorExpressionBase() : base(){}
		public TableSelectorExpressionBase(Expression[] expressions) : base()
		{
			_expressions.AddRange(expressions);
		}
		
		public TableSelectorExpressionBase(Expression[] expressions, KeyDefinition[] keyDefinitions) : base()
		{
			_expressions.AddRange(expressions);
			_keys.AddRange(keyDefinitions);
		}
		
		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}

		// Expressions
		protected Expressions _expressions = new Expressions();
		public Expressions Expressions { get { return _expressions; } }
		
		// Keys
		protected KeyDefinitions _keys = new KeyDefinitions();
		public KeyDefinitions Keys { get { return _keys; } }
	}
	
	public class TableSelectorExpression : TableSelectorExpressionBase
	{
		public TableSelectorExpression() : base(){}
		public TableSelectorExpression(Expression[] expressions) : base(expressions){}
		public TableSelectorExpression(Expression[] expressions, KeyDefinition[] keys) : base(expressions, keys){}
	}
	
	public class PresentationSelectorExpression : TableSelectorExpressionBase
	{
		public PresentationSelectorExpression() : base(){}
		public PresentationSelectorExpression(Expression[] expressions) : base(expressions){}
		public PresentationSelectorExpression(Expression[] expressions, KeyDefinition[] keys) : base(expressions, keys){}
	}
	
	public class RenameColumnExpression : Expression, IMetaData
	{
		public RenameColumnExpression() : base(){}
		public RenameColumnExpression(string columnName, string columnAlias) : base()
		{
			_columnName = columnName;
			_columnAlias = columnAlias;
		}
		
		public RenameColumnExpression(string columnName, string columnAlias, MetaData metaData) : base()
		{
			_columnName = columnName;
			_columnAlias = columnAlias;
			_metaData = metaData;
		}
		
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}
		
		// ColumnAlias
		protected string _columnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return _columnAlias; }
			set { _columnAlias = value == null ? String.Empty : value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class RenameColumnExpressions : Expressions
	{
		protected override void Validate(object item)
		{
			if (!(item is RenameColumnExpression))
				throw new LanguageException(LanguageException.Codes.RenameColumnExpressionContainer);
			base.Validate(item);
		}
		
		public new RenameColumnExpression this[int index]
		{
			get { return (RenameColumnExpression)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class RenameExpression : Expression
	{
		public RenameExpression() : base(){}
		public RenameExpression(Expression expression, RenameColumnExpression[] columns) : base()
		{
			_expression = expression;
			_expressions.AddRange(columns);
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Expressions
		protected RenameColumnExpressions _expressions = new RenameColumnExpressions();
		public RenameColumnExpressions Expressions { get { return _expressions; } }
	}
	
	public class RenameAllExpression : Expression, IMetaData
	{
		public RenameAllExpression() : base(){}
		public RenameAllExpression(Expression expression, string identifier) : base()
		{
			_expression = expression;
			Identifier = identifier;
		}
		
		public RenameAllExpression(Expression expression, string identifier, MetaData metaData) : base()
		{
			_expression = expression;
			Identifier = identifier;
			MetaData = _metaData;
		}

		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		// Identifier
		protected string _identifier = String.Empty;
		public string Identifier
		{
			get { return _identifier; }
			set { _identifier = value == null ? String.Empty : value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class AdornColumnExpression : Expression, IMetaData, IAlterMetaData
	{
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set 
			{ 
				if (_columnName != value)
				{
					_columnName = value == null ? String.Empty : value; 
				}
			}
		}
		
		// ChangeNilable
		private bool _changeNilable;
		public bool ChangeNilable
		{
			get { return _changeNilable; }
			set { _changeNilable = value; }
		}
		
		// IsNilable
		private bool _isNilable;
		public bool IsNilable
		{
			get { return _isNilable; }
			set { _isNilable = value; }
		}

		// Default		
		protected DefaultDefinition _default;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.DefaultDefinitionEdit,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public DefaultDefinition Default
		{
			get { return _default; }
			set { _default = value; }
		}

		// Constraints
		protected ConstraintDefinitions _constraints = new ConstraintDefinitions();
		public ConstraintDefinitions Constraints
		{
			get { return _constraints; }
			set { _constraints = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
		
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.AdornColumnExpressionEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
	public class AdornColumnExpressions : Expressions
	{
		protected override void Validate(object item)
		{
			if (!(item is AdornColumnExpression))
				throw new LanguageException(LanguageException.Codes.AdornColumnExpressionContainer);
			base.Validate(item);
		}
		
		public new AdornColumnExpression this[int index]
		{
			get { return (AdornColumnExpression)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AdornExpression : Expression, IMetaData, IAlterMetaData
	{
		public AdornExpression()
		{
			_alterOrders = new AlterOrderDefinitions();
			_dropOrders = new DropOrderDefinitions();
			_keys = new KeyDefinitions();
			_alterKeys = new AlterKeyDefinitions();
			_dropKeys = new DropKeyDefinitions();
			_alterReferences = new AlterReferenceDefinitions();
			_dropReferences = new DropReferenceDefinitions();
		}

		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Expressions
		protected AdornColumnExpressions _expressions = new AdornColumnExpressions();
		public AdornColumnExpressions Expressions { get { return _expressions; } }
		
		// Constraints
		protected CreateConstraintDefinitions _constraints = new CreateConstraintDefinitions();
		public CreateConstraintDefinitions Constraints { get { return _constraints; } }
		
		// Orders
		protected OrderDefinitions _orders = new OrderDefinitions();
		public OrderDefinitions Orders { get { return _orders; } }

		// AlterOrders
		protected AlterOrderDefinitions _alterOrders = new AlterOrderDefinitions();
		public AlterOrderDefinitions AlterOrders { get { return _alterOrders; } }

		// DropOrders
		protected DropOrderDefinitions _dropOrders = new DropOrderDefinitions();
		public DropOrderDefinitions DropOrders { get { return _dropOrders; } }

		// Keys
		protected KeyDefinitions _keys = new KeyDefinitions();
		public KeyDefinitions Keys { get { return _keys; } }
		
		// AlterKeys
		protected AlterKeyDefinitions _alterKeys = new AlterKeyDefinitions();
		public AlterKeyDefinitions AlterKeys { get { return _alterKeys; } }

		// DropKeys
		protected DropKeyDefinitions _dropKeys = new DropKeyDefinitions();
		public DropKeyDefinitions DropKeys { get { return _dropKeys; } }

		// References
		protected ReferenceDefinitions _references = new ReferenceDefinitions();
		public ReferenceDefinitions References { get { return _references; } }

		// AlterReferences
		protected AlterReferenceDefinitions _alterReferences = new AlterReferenceDefinitions();
		public AlterReferenceDefinitions AlterReferences { get { return _alterReferences; } }

		// DropReferences
		protected DropReferenceDefinitions _dropReferences = new DropReferenceDefinitions();
		public DropReferenceDefinitions DropReferences { get { return _dropReferences; } }

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}

		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class RedefineExpression : Expression
	{
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Expressions
		protected NamedColumnExpressions _expressions = new NamedColumnExpressions();
		public NamedColumnExpressions Expressions { get { return _expressions; } }
	}
	
	public class RemoveExpression : Expression
	{
		public RemoveExpression() : base(){}
		public RemoveExpression(Expression expression, ColumnExpression[] columns)
		{
			Expression = expression;
			_columns.AddRange(columns);
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Columns
		protected ColumnExpressions _columns = new ColumnExpressions();
		public ColumnExpressions Columns { get { return _columns; } }
	}
	
	public class AggregateColumnExpression : Expression, IMetaData
	{
		public AggregateColumnExpression() : base(){}

		// AggregateOperator
		protected string _aggregateOperator = String.Empty;
		public string AggregateOperator
		{
			get { return _aggregateOperator; }
			set { _aggregateOperator = value == null ? String.Empty : value; }
		}
		
		// Distinct
		protected bool _distinct;
		public bool Distinct
		{
			get { return _distinct; }
			set { _distinct = value; }
		}

		#if ALLOWARBITRARYAGGREGATEEXPRESSIONS
		// Arguments
		protected Expressions _arguments = new Expressions();
		public Expressions Arguments { get { return _arguments; } }
		#endif

		// Columns
		protected ColumnExpressions _columns = new ColumnExpressions();
		public ColumnExpressions Columns { get { return _columns; } }

		// HasByClause
		protected bool _hasByClause;
		public bool HasByClause
		{
			get { return _hasByClause; }
			set { _hasByClause = value; }
		}
		
		// OrderColumns
		protected OrderColumnDefinitions _orderColumns = new OrderColumnDefinitions();
		public OrderColumnDefinitions OrderColumns { get { return _orderColumns; } }

		// ColumnAlias
		protected string _columnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return _columnAlias; }
			set { _columnAlias = value == null ? String.Empty : value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class AggregateColumnExpressions : Expressions
	{
		protected override void Validate(object item)
		{
			if (!(item is AggregateColumnExpression))
				throw new LanguageException(LanguageException.Codes.AggregateColumnExpressionContainer);
			base.Validate(item);
		}
		
		public new AggregateColumnExpression this[int index]
		{
			get { return (AggregateColumnExpression)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AggregateExpression : Expression
	{
		public AggregateExpression() : base(){}
		public AggregateExpression(Expression expression, ColumnExpression[] byColumns, AggregateColumnExpression[] computeColumns) : base()
		{
			_expression = expression;
			_byColumns.AddRange(byColumns);
			_computeColumns.AddRange(computeColumns);
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// ByColumns
		protected ColumnExpressions _byColumns = new ColumnExpressions();
		public ColumnExpressions ByColumns { get { return _byColumns; } }
		
		// ComputeColumns
		protected AggregateColumnExpressions _computeColumns = new AggregateColumnExpressions();
		public AggregateColumnExpressions ComputeColumns { get { return _computeColumns; } }
	}
	
	public abstract class BaseOrderExpression : Expression
	{
		public BaseOrderExpression() : base(){}
		public BaseOrderExpression(Expression expression, OrderColumnDefinition[] columns) : base()
		{
			_expression = expression;
			_columns.AddRange(columns);
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Columns
		protected OrderColumnDefinitions _columns = new OrderColumnDefinitions();
		public OrderColumnDefinitions Columns { get { return _columns; } }
	}
	
	public class OrderExpression : BaseOrderExpression
	{
		public OrderExpression() : base() {}
		public OrderExpression(Expression expression, OrderColumnDefinition[] columns) : base()
		{
			Expression = expression;
			Columns.AddRange(columns);
		}
		public OrderExpression(Expression expression, OrderColumnDefinitions columns) : base()
		{
			Expression = expression;
			Columns.AddRange(columns);
		}
		
		// SequenceColumn
		protected IncludeColumnExpression _sequenceColumn;
		public IncludeColumnExpression SequenceColumn
		{
			get { return _sequenceColumn; }
			set { _sequenceColumn = value; }
		}
	}
	
	public class BrowseExpression : BaseOrderExpression{}
	
	public class D4IndexerExpression : IndexerExpression
	{
		protected bool _hasByClause;
		public bool HasByClause
		{
			get { return _hasByClause; }
			set { _hasByClause = value; }
		}
		
		protected KeyColumnDefinitions _byClause = new KeyColumnDefinitions();
		public KeyColumnDefinitions ByClause { get { return _byClause; } }
	}
	
	public class QuotaExpression : Expression
	{
		public QuotaExpression() : base(){}
		public QuotaExpression(Expression expression, Expression quota, OrderColumnDefinition[] columns) : base()
		{
			_expression = expression;
			_quota = quota;
			_columns.AddRange(columns);
		}

		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// Quota
		protected Expression _quota;
		public Expression Quota
		{
			get { return _quota; }
			set { _quota = value; }
		}
		
		// HasByClause
		protected bool _hasByClause;
		public bool HasByClause
		{
			get { return _hasByClause; }
			set { _hasByClause = value; }
		}
		
		// Columns
		protected OrderColumnDefinitions _columns = new OrderColumnDefinitions();
		public OrderColumnDefinitions Columns { get { return _columns; } }
	}
	
	public class ExplodeColumnExpression : Expression
	{
		public ExplodeColumnExpression() : base(){}
		public ExplodeColumnExpression(string columnName) : base()
		{
			_columnName = columnName;
		}
		
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}
	}
	
	public class IncludeColumnExpression : Expression, IMetaData
	{
		public IncludeColumnExpression() : base(){}
		public IncludeColumnExpression(string columnAlias) : base()
		{
			ColumnAlias = columnAlias;
		}
		
		public IncludeColumnExpression(string columnAlias, MetaData metaData)
		{
			ColumnAlias = columnAlias;
			_metaData = metaData;
		}
		
		// ColumnAlias
		protected string _columnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return _columnAlias; }
			set { _columnAlias = value == null ? String.Empty : value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class ExplodeExpression : Expression
	{
		public ExplodeExpression() : base(){}
		public ExplodeExpression(Expression expression, Expression byExpression, Expression rootExpression) : base()
		{
			_expression = expression;
			_byExpression = byExpression;
			_rootExpression = rootExpression;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// ByExpression
		protected Expression _byExpression;
		public Expression ByExpression
		{
			get { return _byExpression; }
			set { _byExpression = value; }
		}
		
		// RootExpression
		protected Expression _rootExpression;
		public Expression RootExpression
		{
			get { return _rootExpression; }
			set { _rootExpression = value; }
		}
		
		// HasOrderByClause
		protected bool _hasOrderByClause;
		public bool HasOrderByClause
		{
			get { return _hasOrderByClause; }
			set { _hasOrderByClause = value; }
		}
		
		// OrderColumns
		protected OrderColumnDefinitions _orderColumns = new OrderColumnDefinitions();
		public OrderColumnDefinitions OrderColumns { get { return _orderColumns; } }

		// LevelColumn
		protected IncludeColumnExpression _levelColumn;
		public IncludeColumnExpression LevelColumn
		{
			get { return _levelColumn; }
			set { _levelColumn = value; }
		}

		// SequenceColumn
		protected IncludeColumnExpression _sequenceColumn;
		public IncludeColumnExpression SequenceColumn
		{
			get { return _sequenceColumn; }
			set { _sequenceColumn = value; }
		}
	}
	
	public abstract class BinaryTableExpression : Expression
	{
		public BinaryTableExpression() : base(){}
		public BinaryTableExpression(Expression leftExpression, Expression rightExpression)
		{
			_leftExpression = leftExpression;
			_rightExpression = rightExpression;
		}
		
		// LeftExpression
		protected Expression _leftExpression;
		public Expression LeftExpression
		{
			get { return _leftExpression; }
			set { _leftExpression = value; }
		}
		
		// RightExpression
		protected Expression _rightExpression;
		public Expression RightExpression
		{
			get { return _rightExpression; }
			set { _rightExpression = value; }
		}
	}
	
	public class UnionExpression : BinaryTableExpression
	{
		public UnionExpression() : base(){}
		public UnionExpression(Expression leftExpression, Expression rightExpression)
		{
			_leftExpression = leftExpression;
			_rightExpression = rightExpression;
		}
	}
	
	public class IntersectExpression : BinaryTableExpression
	{
		public IntersectExpression() : base(){}
		public IntersectExpression(Expression leftExpression, Expression rightExpression)
		{
			_leftExpression = leftExpression;
			_rightExpression = rightExpression;
		}
	}
	
	public class DifferenceExpression : BinaryTableExpression
	{
		public DifferenceExpression() : base(){}
		public DifferenceExpression(Expression leftExpression, Expression rightExpression)
		{
			_leftExpression = leftExpression;
			_rightExpression = rightExpression;
		}
	}
	
	public class ProductExpression : BinaryTableExpression
	{
		public ProductExpression() : base(){}
		public ProductExpression(Expression leftExpression, Expression rightExpression)
		{
			_leftExpression = leftExpression;
			_rightExpression = rightExpression;
		}
	}
	
	public class DivideExpression : BinaryTableExpression
	{
		public DivideExpression() : base(){}
		public DivideExpression(Expression leftExpression, Expression rightExpression)
		{
			_leftExpression = leftExpression;
			_rightExpression = rightExpression;
		}
	}
	
	public class ConditionedBinaryTableExpression : BinaryTableExpression
	{
		private Expression _condition;
		public Expression Condition
		{
			get { return _condition; }
			set { _condition = value; }
		}
	}
	
	public class HavingExpression : ConditionedBinaryTableExpression
	{
		public HavingExpression() : base() { }
		public HavingExpression(Expression leftExpression, Expression rightExpression, Expression condition)
		{
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
			Condition = condition;
		}
	}
	
	public class WithoutExpression : ConditionedBinaryTableExpression
	{
		public WithoutExpression() : base() { }
		public WithoutExpression(Expression leftExpression, Expression rightExpression, Expression condition)
		{
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
			Condition = condition;
		}
	}
	
	public enum JoinCardinality { OneToOne, OneToMany, ManyToOne, ManyToMany }
		
	public abstract class JoinExpression : ConditionedBinaryTableExpression
	{
		// IsLookup
		protected bool _isLookup;
		public bool IsLookup
		{
			get { return _isLookup; }
			set { _isLookup = value; }
		}
		
		// This is not used by the compiler, it is set by statement emission of a compiled plan and used by the application transaction emitter.
		protected JoinCardinality _cardinality;
		public JoinCardinality Cardinality
		{
			get { return _cardinality; }
			set { _cardinality = value; }
		}
		
		// IsDetailLookup
		protected bool _isDetailLookup;
		public bool IsDetailLookup
		{
			get { return _isDetailLookup; }
			set { _isDetailLookup = value; }
		}
	}
	
	public class InnerJoinExpression : JoinExpression{}
	
	public abstract class OuterJoinExpression : JoinExpression
	{
		public OuterJoinExpression() : base(){}
		
		// RowExistsColumn
		protected IncludeColumnExpression _rowExistsColumn;
		public IncludeColumnExpression RowExistsColumn
		{
			get { return _rowExistsColumn; }
			set { _rowExistsColumn = value; }
		}
	}
	
	public class LeftOuterJoinExpression : OuterJoinExpression{}
	
	public class RightOuterJoinExpression : OuterJoinExpression{}

	#if USEGREATDIVIDE
	// this is actually a "great" divide, a ternary table operator
	public class DivideExpression : TableExpression
	{
		public const string CTableExpressionContainer = @"Divide expression ""{0}"" may only contain table expressions";
		public const string CTernaryTableExpressionContainer = @"Divide expression ""{0}"" may only contain three table expressions";
		public const string CDividendMustBeSetFirst = "Dividend must be set first";
		public const string CDividendAndDivisorMustBeSetFirst = "Dividend and divisor must be set first";
		
		protected override void ChildValidate(object ASender, object AItem)
		{
			if (!(AItem is TableExpression))
				throw new LanguageException(LanguageException.Codes.TableExpressionContainer);
			if ((FDividend != null) && (FDivisor != null) && (FMediator != null))
				throw new LanguageException(LanguageException.Codes.TernaryTableExpressionContainer);
			base.ChildValidate(ASender, AItem);
		}
		
		protected override void ChildAdding(object ASender, object AItem)
		{
			base.ChildAdding(ASender, AItem);
			if (FDividend != null)
				FDividend = (TableExpression)AItem;
			else if (FDivisor != null)
				FDivisor = (TableExpression)AItem;
			else if (FMediator != null)
				FMediator = (TableExpression)AItem;
		}
		
		protected override void ChildRemoving(object ASender, object AItem)
		{
			if (AItem == FDividend)
				FDividend = null;
			else if (AItem == FDivisor)
				FDivisor = null;
			else if (AItem == FMediator)
				FMediator = null;
			base.ChildRemoving(ASender, AItem);
		}
		
		public override void Process(Machine AMachine)
		{
			if (FDividend == null)
				throw new LanguageException(LanguageException.Codes.TableExpressionExpected);
			if (FDivisor == null)
				throw new LanguageException(LanguageException.Codes.TableExpressionExpected);
			if (FMediator == null)
				throw new LanguageException(LanguageException.Codes.TableExpressionExpected);
			AMachine.Push(FDividend);
			AMachine.Push(FDivisor);
			AMachine.Push(FMediator);
			AMachine.Execute("iDivide");
		}
		
		// Dividend
		protected TableExpression FDividend;
		public TableExpression Dividend
		{
			get
			{
				return FDividend;
			}
			set
			{
				if (FDividend != null)
					FDividend.Parent = null;
				if (value != null)
					value.Parent = this;
			}
		}
		
		// Divisor
		protected TableExpression FDivisor;
		public TableExpression Divisor
		{
			get
			{
				return FDivisor;
			}
			set
			{
				if (FDividend == null)
					throw new LanguageException(LanguageException.Codes.DividendMustBeSetFirst);
				if (FDivisor != null)
					FDivisor.Parent = null;
				if (value != null)
					value.Parent = this;
			}
		}
		
		// Mediator
		protected TableExpression FMediator;
		public TableExpression Mediator
		{
			get
			{
				return FMediator;
			}
			set
			{
				if ((FDividend == null) || (FDivisor == null))
					throw new LanguageException(LanguageException.Codes.DividendAndDivisorMustBeSetFirst);
				if (FMediator != null)
					FMediator.Parent = null;
				if (value != null)
					value.Parent = this;
			}
		}
	}
	#endif

	public class VariableStatement : Statement
	{
		public VariableStatement() : base(){}
		public VariableStatement(string variableName, TypeSpecifier typeSpecifier) : base()
		{
			VariableName = new IdentifierExpression(variableName);
			_typeSpecifier = typeSpecifier;
		}
		
		public VariableStatement(string variableName, TypeSpecifier typeSpecifier, Expression expression) : base()
		{
			VariableName = new IdentifierExpression(variableName);
			_typeSpecifier = typeSpecifier;
			_expression = expression;
		}
		
		// VariableName
		protected IdentifierExpression _variableName;
		public IdentifierExpression VariableName
		{
			get { return _variableName; }
			set { _variableName = value; }
		}
		
		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class ExpressionStatement : Statement
	{
		public ExpressionStatement() : base(){}
		public ExpressionStatement(Expression expression) : base()
		{
			_expression = expression;
		}
		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class AssignmentStatement : Statement
	{
		public AssignmentStatement() : base(){}
		public AssignmentStatement(Expression target, Expression expression)
		{
			_target = target;
			_expression = expression;
		}
		
		// Target
		protected Expression _target;
		public Expression Target
		{
			get { return _target; }
			set { _target = value; }
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public abstract class CreateTableVarStatement : D4Statement, IMetaData
	{
		// TableVarName
		protected string _tableVarName = String.Empty;
		public string TableVarName
		{
			get { return _tableVarName; }
			set { _tableVarName = value == null ? String.Empty : value; }
		}
		
		// IsSession
		protected bool _isSession = false;
		public bool IsSession
		{
			get { return _isSession; }
			set { _isSession = value; }
		}
		
		// Keys
		protected KeyDefinitions _keys = new KeyDefinitions();
		public KeyDefinitions Keys { get { return _keys; } }

		// References
		protected ReferenceDefinitions _references = new ReferenceDefinitions();
		public ReferenceDefinitions References { get { return _references; } }

		// Constraints
		protected CreateConstraintDefinitions _constraints = new CreateConstraintDefinitions();
		public CreateConstraintDefinitions Constraints { get { return _constraints; } }
		
		// Orders
		protected OrderDefinitions _orders = new OrderDefinitions();
		public OrderDefinitions Orders { get { return _orders; } }

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}

	public class CreateTableStatement : CreateTableVarStatement
	{		
		// DeviceName
		protected IdentifierExpression _deviceName;
		public IdentifierExpression DeviceName
		{
			get { return _deviceName; }
			set { _deviceName = value; }
		}
		
		// Columns
		protected ColumnDefinitions _columns = new ColumnDefinitions();
		public ColumnDefinitions Columns { get { return _columns; } }

		// FromExpression		
		protected Expression _fromExpression;
		public Expression FromExpression
		{
			get { return _fromExpression; }
			set { _fromExpression = value; }
		}
	}
	
	public class CreateViewStatement : CreateTableVarStatement
	{		
		// Expression		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class AccessorBlock : D4Statement
	{
		private ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return _classDefinition; }
			set { _classDefinition = value; }
		}
		
		private Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		private Statement _block;
		public Statement Block
		{
			get { return _block; }
			set { _block = value; }
		}
		
		public bool IsD4Implemented()
		{
			return (_expression != null) || (_block != null);
		}
	}
	
	public class AlterAccessorBlock : D4Statement
	{
		private AlterClassDefinition _alterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return _alterClassDefinition; }
			set { _alterClassDefinition = value; }
		}
		
		private Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		private Statement _block;
		public Statement Block
		{
			get { return _block; }
			set { _block = value; }
		}
	}
	
	public abstract class PropertyDefinitionBase : D4Statement
	{
		public PropertyDefinitionBase() : base() {}
		public PropertyDefinitionBase(string propertyName) : base()
		{
			PropertyName = propertyName;
		}
		
		// PropertyName		
		private string _propertyName = String.Empty;
		public string PropertyName
		{
			get { return _propertyName; }
			set { _propertyName = value == null ? String.Empty : value; }
		}
	}
	
	public class PropertyDefinition : PropertyDefinitionBase, IMetaData
	{
		public PropertyDefinition() : base() {}
		public PropertyDefinition(string propertyName) : base(propertyName){}
		public PropertyDefinition(string propertyName, TypeSpecifier propertyType) : base(propertyName)
		{
			_propertyType = propertyType;
		}
		
		// PropertyType
		protected TypeSpecifier _propertyType;
		public TypeSpecifier PropertyType
		{
			get { return _propertyType; }
			set { _propertyType = value; }
		}
		
		// ReadAccessorBlock
		private AccessorBlock _readAccessorBlock;
		public AccessorBlock ReadAccessorBlock
		{
			get { return _readAccessorBlock; }
			set { _readAccessorBlock = value; }
		}
		
		// WriteAccessorBlock
		private AccessorBlock _writeAccessorBlock;
		public AccessorBlock WriteAccessorBlock
		{
			get { return _writeAccessorBlock; }
			set { _writeAccessorBlock = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class PropertyDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is PropertyDefinition))
				throw new LanguageException(LanguageException.Codes.PropertyDefinitionContainer);
			base.Validate(item);
		}
		
		public new PropertyDefinition this[int index]
		{
			get { return (PropertyDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterPropertyDefinition : PropertyDefinitionBase, IAlterMetaData
	{
		public AlterPropertyDefinition() : base() {} 
		public AlterPropertyDefinition(string propertyName) : base(propertyName){}
		
		// PropertyType
		protected TypeSpecifier _propertyType;
		public TypeSpecifier PropertyType
		{
			get { return _propertyType; }
			set { _propertyType = value; }
		}
		
		// ReadAccessorBlock
		private AlterAccessorBlock _readAccessorBlock;
		public AlterAccessorBlock ReadAccessorBlock
		{
			get { return _readAccessorBlock; }
			set { _readAccessorBlock = value; }
		}
		
		// WriteAccessorBlock
		private AlterAccessorBlock _writeAccessorBlock;
		public AlterAccessorBlock WriteAccessorBlock
		{
			get { return _writeAccessorBlock; }
			set { _writeAccessorBlock = value; }
		}

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterPropertyDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is AlterPropertyDefinition))
				throw new LanguageException(LanguageException.Codes.AlterPropertyDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterPropertyDefinition this[int index]
		{
			get { return (AlterPropertyDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropPropertyDefinition : PropertyDefinitionBase
	{
		public DropPropertyDefinition() : base() {}
		public DropPropertyDefinition(string propertyName) : base(propertyName){}
	}
	
	public class DropPropertyDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DropPropertyDefinition))
				throw new LanguageException(LanguageException.Codes.DropPropertyDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropPropertyDefinition this[int index]
		{
			get { return (DropPropertyDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public abstract class RepresentationDefinitionBase : D4Statement
	{
		public RepresentationDefinitionBase() : base() {}
		public RepresentationDefinitionBase(string representationName) : base()
		{
			RepresentationName = representationName;
		}
		
		private string _representationName = String.Empty;
		public string RepresentationName
		{
			get { return _representationName; }
			set { _representationName = value == null ? String.Empty : value; }
		}
	}
	
	public class RepresentationDefinition : RepresentationDefinitionBase, IMetaData
	{
		public RepresentationDefinition() : base() {}
		public RepresentationDefinition(string representationName) : base(representationName) {}
		
		private bool _isGenerated;
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}
		
		public bool HasD4ImplementedComponents()
		{
			if ((_selectorAccessorBlock != null) && _selectorAccessorBlock.IsD4Implemented())
				return true;
			
			foreach (PropertyDefinition property in _properties)
				if 
				(
					((property.ReadAccessorBlock != null) && property.ReadAccessorBlock.IsD4Implemented()) || 
					((property.WriteAccessorBlock != null) && property.WriteAccessorBlock.IsD4Implemented())
				)
					return true;
					
			return false;
		}
		
		private PropertyDefinitions _properties = new PropertyDefinitions();
		public PropertyDefinitions Properties { get { return _properties; } }
		
		// SelectorAccessorBlock
		private AccessorBlock _selectorAccessorBlock;
		public AccessorBlock SelectorAccessorBlock
		{
			get { return _selectorAccessorBlock; }
			set { _selectorAccessorBlock = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class RepresentationDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is RepresentationDefinition))
				throw new LanguageException(LanguageException.Codes.RepresentationDefinitionContainer);
			base.Validate(item);
		}
		
		public new RepresentationDefinition this[int index]
		{
			get { return (RepresentationDefinition)base[index]; }
			set { base[index] = value; }
		}
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Compare(name, this[index].RepresentationName) == 0)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class AlterRepresentationDefinition : RepresentationDefinitionBase, IAlterMetaData
	{
		public AlterRepresentationDefinition() : base() {}
		public AlterRepresentationDefinition(string representationName) : base(representationName) {}

		// CreateProperties		
		private PropertyDefinitions _createProperties = new PropertyDefinitions();
		public PropertyDefinitions CreateProperties { get { return _createProperties; } }

		// AlterProperties		
		private AlterPropertyDefinitions _alterProperties = new AlterPropertyDefinitions();
		public AlterPropertyDefinitions AlterProperties { get { return _alterProperties; } }

		// DropProperties		
		private DropPropertyDefinitions _dropProperties = new DropPropertyDefinitions();
		public DropPropertyDefinitions DropProperties { get { return _dropProperties; } }

		// SelectorAccessorBlock
		private AlterAccessorBlock _selectorAccessorBlock;
		public AlterAccessorBlock SelectorAccessorBlock
		{
			get { return _selectorAccessorBlock; }
			set { _selectorAccessorBlock = value; }
		}

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterRepresentationDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is AlterRepresentationDefinition))
				throw new LanguageException(LanguageException.Codes.AlterRepresentationDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterRepresentationDefinition this[int index]
		{
			get { return (AlterRepresentationDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropRepresentationDefinition : RepresentationDefinitionBase
	{
		public DropRepresentationDefinition() : base() {}
		public DropRepresentationDefinition(string name) : base(name) {}
	}
	
	public class DropRepresentationDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DropRepresentationDefinition))
				throw new LanguageException(LanguageException.Codes.DropRepresentationDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropRepresentationDefinition this[int index]
		{
			get { return (DropRepresentationDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public abstract class SpecialDefinitionBase : D4Statement
	{
		public SpecialDefinitionBase() : base() {}
		public SpecialDefinitionBase(string name) : base()
		{
			Name = name;
		}
		
		// Name
		protected string _name = String.Empty;
		public string Name
		{
			get { return _name; }
			set { _name = value == null ? String.Empty : value; }
		}
	}
	
	public class SpecialDefinition : SpecialDefinitionBase, IMetaData
	{
		// Value
		protected Expression _value;
		public Expression Value
		{
			get { return _value; }
			set { _value = value; }
		}

		private bool _isGenerated;
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class SpecialDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is SpecialDefinition))
				throw new LanguageException(LanguageException.Codes.SpecialDefinitionContainer);
			base.Validate(item);
		}
		
		public new SpecialDefinition this[int index]
		{
			get { return (SpecialDefinition)base[index]; }
			set { base[index] = value; }
		}
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Compare(name, this[index].Name) == 0)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class AlterSpecialDefinition : SpecialDefinitionBase, IAlterMetaData
	{
		// Value
		protected Expression _value;
		public Expression Value
		{
			get { return _value; }
			set { _value = value; }
		}

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterSpecialDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterSpecialDefinition))
				throw new LanguageException(LanguageException.Codes.AlterSpecialDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterSpecialDefinition this[int index]
		{
			get { return (AlterSpecialDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropSpecialDefinition : SpecialDefinitionBase
	{
		public DropSpecialDefinition() : base() {}
		public DropSpecialDefinition(string name) : base(name) {}
	}
	
	public class DropSpecialDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropSpecialDefinition))
				throw new LanguageException(LanguageException.Codes.DropSpecialDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropSpecialDefinition this[int index]
		{
			get { return (DropSpecialDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class ScalarTypeNameDefinition : D4Statement
	{
		public ScalarTypeNameDefinition() : base() {}
		public ScalarTypeNameDefinition(string scalarTypeName) : base()
		{
			ScalarTypeName = scalarTypeName;
		}
		
		// ScalarTypeName
		protected string _scalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	public class ScalarTypeNameDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is ScalarTypeNameDefinition))
				throw new LanguageException(LanguageException.Codes.ScalarTypeNameDefinitionContainer);
			base.Validate(item);
		}
		
		public new ScalarTypeNameDefinition this[int index]
		{
			get { return (ScalarTypeNameDefinition)base[index]; }
			set { base[index] = value; }
		}
	}

	public class CreateScalarTypeStatement : D4Statement, IMetaData
	{
		// constructor
		public CreateScalarTypeStatement() : base(){}
		
		// ScalarTypeName
		protected string _scalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value == null ? String.Empty : value; }
		}

		// FromClassDefinition
		protected ClassDefinition _fromClassDefinition;
		public ClassDefinition FromClassDefinition
		{
			get { return _fromClassDefinition; }
			set { _fromClassDefinition = value; }
		}
		
		// LikeScalarTypeName
		protected string _likeScalarTypeName = String.Empty;
		public string LikeScalarTypeName
		{
			get { return _likeScalarTypeName; }
			set { _likeScalarTypeName = value; }
		}

		// ParentScalarTypes
		protected ScalarTypeNameDefinitions _parentScalarTypes = new ScalarTypeNameDefinitions();
		public ScalarTypeNameDefinitions ParentScalarTypes { get { return _parentScalarTypes; } }

		// Representations
		protected RepresentationDefinitions _representations = new RepresentationDefinitions();
		public RepresentationDefinitions Representations { get { return _representations; } }

		// Default		
		protected DefaultDefinition _default;
		public DefaultDefinition Default
		{
			get { return _default; }
			set { _default = value; }
		}

		// Constraints
		protected ConstraintDefinitions _constraints = new ConstraintDefinitions();
		public ConstraintDefinitions Constraints
		{
			get { return _constraints; }
			set { _constraints = value; }
		}
		
		// Specials
		protected SpecialDefinitions _specials = new SpecialDefinitions();
		public SpecialDefinitions Specials { get { return _specials; } }
		
		// ClassDefinition		
		protected ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return _classDefinition; }
			set { _classDefinition = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class CreateReferenceStatement : ReferenceDefinition
	{
		// TableVarName
		protected string _tableVarName = String.Empty;
		public string TableVarName
		{
			get { return _tableVarName; }
			set { _tableVarName = value == null ? String.Empty : value; }
		}

		// IsSession
		protected bool _isSession = false;
		public bool IsSession
		{
			get { return _isSession; }
			set { _isSession = value; }
		}
	}
	
	public class AlterReferenceStatement : AlterReferenceDefinition{}
	
	public class DropReferenceStatement : DropReferenceDefinition{}
	
	public class OperatorBlock : D4Statement
	{
		// ClassDefinition		
		protected ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return _classDefinition; }
			set { _classDefinition = value; }
		}
		
		// Block
		protected Statement _block;
		public Statement Block
		{
			get { return _block; }
			set { _block = value; }
		}
	}
	
	public class AlterOperatorBlock : D4Statement
	{
		// AlterClassDefinition		
		protected AlterClassDefinition _alterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return _alterClassDefinition; }
			set { _alterClassDefinition = value; }
		}
		
		// Block
		protected Statement _block;
		public Statement Block
		{
			get { return _block; }
			set { _block = value; }
		}
	}
	
	public class SourceStatement : Statement, IMetaData
	{
		private string _source;
		public string Source
		{
			get { return _source; }
			set { _source = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public abstract class CreateOperatorStatementBase : D4Statement, IMetaData
	{
		// OperatorName
		protected string _operatorName = String.Empty;
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value == null ? String.Empty : value; }
		}
		
		// IsSession
		protected bool _isSession;
		public bool IsSession
		{
			get { return _isSession; }
			set { _isSession = value; }
		}

		// FormalParameters
		protected FormalParameters _formalParameters = new FormalParameters();
		public FormalParameters FormalParameters { get { return _formalParameters; } }
		
		// ReturnType
		protected TypeSpecifier _returnType;
		public TypeSpecifier ReturnType
		{
			get { return _returnType; }
			set { _returnType = value; }
		}
		
		// IsReintroduced
		protected bool _isReintroduced;
		public bool IsReintroduced
		{
			get { return _isReintroduced; }
			set { _isReintroduced = value; }
		}

		// IsAbstract
		protected bool _isAbstract;
		public bool IsAbstract
		{
			get { return _isAbstract; }
			set { _isAbstract = value; }
		}

		// IsVirtual
		protected bool _isVirtual;
		public bool IsVirtual
		{
			get { return _isVirtual; }
			set { _isVirtual = value; }
		}

		// IsOverride
		protected bool _isOverride;
		public bool IsOverride
		{
			get { return _isOverride; }
			set { _isOverride = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class CreateOperatorStatement : CreateOperatorStatementBase
	{
		// Block
		protected OperatorBlock _block = new OperatorBlock();
		public OperatorBlock Block { get { return _block; } }
	}

	public class CreateAggregateOperatorStatement : CreateOperatorStatementBase
	{
		// Initialization
		protected OperatorBlock _initialization = new OperatorBlock();
		public OperatorBlock Initialization { get { return _initialization; } }
		
		// Aggregation		
		protected OperatorBlock _aggregation = new OperatorBlock();
		public OperatorBlock Aggregation { get { return _aggregation; } }
		
		// Finalization
		protected OperatorBlock _finalization = new OperatorBlock();
		public OperatorBlock Finalization { get { return _finalization; } }
	}
	
	public class OperatorSpecifier : D4Statement
	{
		public OperatorSpecifier() : base(){}
		public OperatorSpecifier(string operatorName, FormalParameterSpecifier[] formalParameterSpecifiers) : base()
		{
			OperatorName = operatorName;
			_formalParameterSpecifiers.AddRange(formalParameterSpecifiers);
		}
		
		public OperatorSpecifier(string operatorName, FormalParameterSpecifiers formalParameterSpecifiers) : base()
		{
			OperatorName = operatorName;
			_formalParameterSpecifiers.AddRange(formalParameterSpecifiers);
		}
		
		// OperatorName
		protected string _operatorName = String.Empty;
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value == null ? String.Empty : value; }
		}

		// FormalParameterSpecifiers
		protected FormalParameterSpecifiers _formalParameterSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers FormalParameterSpecifiers { get { return _formalParameterSpecifiers; } }
	}
	
	public abstract class AlterOperatorStatementBase : D4Statement, IAlterMetaData
	{
		// OperatorSpecifier
		protected OperatorSpecifier _operatorSpecifier;
		public OperatorSpecifier OperatorSpecifier
		{
			get { return _operatorSpecifier; }
			set { _operatorSpecifier = value; }
		}
		
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterOperatorStatement : AlterOperatorStatementBase
	{
		// Block
		protected AlterOperatorBlock _block = new AlterOperatorBlock();
		public AlterOperatorBlock Block { get { return _block; } }
	}
	
	public class AlterAggregateOperatorStatement : AlterOperatorStatementBase
	{
		// Initialization
		protected AlterOperatorBlock _initialization = new AlterOperatorBlock();
		public AlterOperatorBlock Initialization { get { return _initialization; } }
		
		// Aggregation		
		protected AlterOperatorBlock _aggregation = new AlterOperatorBlock();
		public AlterOperatorBlock Aggregation { get { return _aggregation; } }
		
		// Finalization
		protected AlterOperatorBlock _finalization = new AlterOperatorBlock();
		public AlterOperatorBlock Finalization { get { return _finalization; } }
	}
	
	public class CreateServerStatement : D4Statement, IMetaData
	{
		// ServerName
		protected string _serverName = String.Empty;
		public string ServerName
		{
			get { return _serverName; }
			set { _serverName = value == null ? String.Empty : value; }
		}

/*		
		// HostName
		protected string FHostName = String.Empty;
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value == null ? String.Empty : value; }
		}
		
		// InstanceName
		protected string FInstanceName = String.Empty;
		public string InstanceName
		{
			get { return FInstanceName; }
			set { FInstanceName = value == null ? String.Empty : value; }
		}
		
		// OverridePortNumber
		protected int FOverridePortNumber;
		public int OverridePortNumber
		{
			get { return FOverridePortNumber; }
			set { FOverridePortNumber = value; }
		}
*/
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class AlterServerStatement : D4Statement, IAlterMetaData
	{
		// ServerName
		protected string _serverName = String.Empty;
		public string ServerName
		{
			get { return _serverName; }
			set { _serverName = value == null ? String.Empty : value; }
		}
		
/*
		// HostName will be null if there is no change to the host
		protected string FHostName;
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value; }
		}
		
		// InstanceName will be null if there is no change to the host
		protected string FInstanceName;
		public string InstanceName
		{
			get { return FInstanceName; }
			set { FInstanceName = value; }
		}
		
		// OverridePortNumber will be null if there is no change to the host
		protected int? FOverridePortNumber;
		public int? OverridePortNumber
		{
			get { return FOverridePortNumber; }
			set { FOverridePortNumber = value; }
		}
*/
		
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public abstract class DeviceMapItem : D4Statement{}
	
	public class DeviceScalarTypeMapBase : DeviceMapItem
	{
		public DeviceScalarTypeMapBase() : base() {}
		public DeviceScalarTypeMapBase(string scalarTypeName) : base()
		{
			ScalarTypeName = scalarTypeName;
		}
		
		// ScalarTypeName
		protected string _scalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	public class DeviceScalarTypeMap : DeviceScalarTypeMapBase, IMetaData
	{
		public DeviceScalarTypeMap() : base() {}
		public DeviceScalarTypeMap(string scalarTypeName) : base(scalarTypeName) {}

		public DeviceScalarTypeMap(string scalarTypeName, bool isGenerated) : base(scalarTypeName)
		{
			_isGenerated = isGenerated;
		}
		
		private bool _isGenerated;
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}
		
		// ClassDefinition		
		protected ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return _classDefinition; }
			set { _classDefinition = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class DeviceScalarTypeMaps : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DeviceScalarTypeMap))
				throw new LanguageException(LanguageException.Codes.DeviceScalarTypeMapContainer);
			base.Validate(item);
		}
		
		public new DeviceScalarTypeMap this[int index]
		{
			get { return (DeviceScalarTypeMap)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterDeviceScalarTypeMap : DeviceScalarTypeMapBase, IAlterMetaData
	{
		// AlterClassDefinition		
		protected AlterClassDefinition _alterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return _alterClassDefinition; }
			set { _alterClassDefinition = value; }
		}

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterDeviceScalarTypeMaps : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is AlterDeviceScalarTypeMap))
				throw new LanguageException(LanguageException.Codes.AlterDeviceScalarTypeMapContainer);
			base.Validate(item);
		}
		
		public new AlterDeviceScalarTypeMap this[int index]
		{
			get { return (AlterDeviceScalarTypeMap)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropDeviceScalarTypeMap : DeviceScalarTypeMapBase{}
	
	public class DropDeviceScalarTypeMaps : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DropDeviceScalarTypeMap))
				throw new LanguageException(LanguageException.Codes.DropDeviceScalarTypeMapContainer);
			base.Validate(item);
		}
		
		public new DropDeviceScalarTypeMap this[int index]
		{
			get { return (DropDeviceScalarTypeMap)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DeviceOperatorMapBase : DeviceMapItem
	{
		// OperatorSpecifier
		protected OperatorSpecifier _operatorSpecifier;
		public OperatorSpecifier OperatorSpecifier
		{
			get { return _operatorSpecifier; }
			set { _operatorSpecifier = value; }
		}
	}
	
	public class DeviceOperatorMap : DeviceOperatorMapBase, IMetaData
	{
		// ClassDefinition		
		protected ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return _classDefinition; }
			set { _classDefinition = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class DeviceOperatorMaps : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DeviceOperatorMap))
				throw new LanguageException(LanguageException.Codes.DeviceOperatorMapContainer);
			base.Validate(item);
		}
		
		public new DeviceOperatorMap this[int index]
		{
			get { return (DeviceOperatorMap)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterDeviceOperatorMap : DeviceOperatorMapBase, IAlterMetaData
	{
		// AlterClassDefinition		
		protected AlterClassDefinition _alterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return _alterClassDefinition; }
			set { _alterClassDefinition = value; }
		}

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}

	public class AlterDeviceOperatorMaps : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is AlterDeviceOperatorMap))
				throw new LanguageException(LanguageException.Codes.AlterDeviceOperatorMapContainer);
			base.Validate(item);
		}
		
		public new AlterDeviceOperatorMap this[int index]
		{
			get { return (AlterDeviceOperatorMap)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropDeviceOperatorMap : DeviceOperatorMapBase{}
	
	public class DropDeviceOperatorMaps : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DropDeviceOperatorMap))
				throw new LanguageException(LanguageException.Codes.DropDeviceOperatorMapContainer);
			base.Validate(item);
		}
		
		public new DropDeviceOperatorMap this[int index]
		{
			get { return (DropDeviceOperatorMap)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DeviceStoreDefinitionBase : DeviceMapItem
	{
		// StoreName
		protected string _storeName;
		public string StoreName
		{
			get { return _storeName; }
			set { _storeName = value; }
		}
	}
	
	public class DeviceStoreDefinition : DeviceStoreDefinitionBase, IMetaData
	{
		// Expression
		private Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// ClusteredDefault
		private bool _clusteredDefault;
		public bool ClusteredDefault
		{
			get { return _clusteredDefault; }
			set { _clusteredDefault = value; }
		}

		// ClusteredIndexDefinition
		private IndexDefinition _clusteredIndexDefinition;
		public IndexDefinition ClusteredIndexDefinition
		{
			get { return _clusteredIndexDefinition; }
			set { _clusteredIndexDefinition = value; }
		}
		
		// IndexesDefault
		private bool _indexesDefault;
		public bool IndexesDefault
		{
			get { return _indexesDefault; }
			set { _indexesDefault = value; }
		}

		// IndexDefinitions
		private IndexDefinitions _indexDefinitions = new IndexDefinitions();
		public IndexDefinitions IndexDefinitions { get { return _indexDefinitions; } }

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class DeviceStoreDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DeviceStoreDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "DeviceStoreDefinition");
			base.Validate(item);
		}
		
		public new DeviceStoreDefinition this[int index]
		{
			get { return (DeviceStoreDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterDeviceStoreDefinition : DeviceStoreDefinitionBase, IAlterMetaData
	{
		// Expression
		private Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// ClusteredDefault
		private bool _clusteredDefault;
		public bool ClusteredDefault
		{
			get { return _clusteredDefault; }
			set { _clusteredDefault = value; }
		}

		// ClusteredIndexDefinition
		private IndexDefinition _clusteredIndexDefinition;
		public IndexDefinition ClusteredIndexDefinition
		{
			get { return _clusteredIndexDefinition; }
			set { _clusteredIndexDefinition = value; }
		}
		
		// IndexesDefault
		private bool _indexesDefault;
		public bool IndexesDefault
		{
			get { return _indexesDefault; }
			set { _indexesDefault = value; }
		}

		// IndexDefinitions
		private IndexDefinitions _createIndexDefinitions = new IndexDefinitions();
		public IndexDefinitions CreateIndexDefinitions { get { return _createIndexDefinitions; } }

		// AlterIndexDefinitions
		private AlterIndexDefinitions _alterIndexDefinitions = new AlterIndexDefinitions();
		public AlterIndexDefinitions AlterIndexDefinitions { get { return _alterIndexDefinitions; } }

		// DropIndexDefinitions
		private DropIndexDefinitions _dropIndexDefinitions = new DropIndexDefinitions();
		public DropIndexDefinitions DropIndexDefinitions { get { return _dropIndexDefinitions; } }

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}

	public class AlterDeviceStoreDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is AlterDeviceStoreDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "AlterDeviceStoreDefinition");
			base.Validate(item);
		}
		
		public new AlterDeviceStoreDefinition this[int index]
		{
			get { return (AlterDeviceStoreDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropDeviceStoreDefinition : DeviceStoreDefinitionBase{}
	
	public class DropDeviceStoreDefinitions : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is DropDeviceStoreDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "DropDeviceStoreDefinition");
			base.Validate(item);
		}
		
		public new DropDeviceStoreDefinition this[int index]
		{
			get { return (DropDeviceStoreDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public abstract class IndexDefinitionBase : D4Statement
	{
		// Columns
		protected IndexColumnDefinitions _columns = new IndexColumnDefinitions();
		public IndexColumnDefinitions Columns { get { return _columns; } }
	}
	
	public class IndexDefinition : IndexDefinitionBase, IMetaData
	{
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class IndexDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is IndexDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "IndexDefinition");
			base.Validate(item);
		}
		
		public new IndexDefinition this[int index]
		{
			get { return (IndexDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterIndexDefinition : IndexDefinitionBase, IAlterMetaData
	{
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterIndexDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterIndexDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "AlterIndexDefinition");
			base.Validate(item);
		}
		
		public new AlterIndexDefinition this[int index]
		{
			get { return (AlterIndexDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropIndexDefinition : IndexDefinitionBase{}
	
	public class DropIndexDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropIndexDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "DropIndexDefinition");
			base.Validate(item);
		}
		
		public new DropIndexDefinition this[int index]
		{
			get { return (DropIndexDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class IndexColumnDefinition : ColumnDefinitionBase
	{
		public IndexColumnDefinition() : base(){}
		public IndexColumnDefinition(string columnName, bool ascending) : base(columnName)
		{
			_ascending = ascending;
		}
		
		public IndexColumnDefinition(string columnName, bool ascending, SortDefinition sort) : base(columnName)
		{
			_ascending = ascending;
			_sort = sort;
		}
		
		protected bool _ascending = true;
		public bool Ascending
		{
			get { return _ascending; }
			set { _ascending = value; }
		}
		
		protected SortDefinition _sort;
		public SortDefinition Sort
		{
			get { return _sort; }
			set { _sort = value; }
		}
	}

	public class IndexColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is IndexColumnDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "IndexColumnDefinition");
			base.Validate(item);
		}
		
		public new IndexColumnDefinition this[int index]
		{
			get { return (IndexColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	/// <remarks>
	///		Determines when schema reconciliation takes place.<br/>
	///		Startup indicates that a full catalog reconciliation is to take place on device startup.<br/>
	///		Command indicates that reconciliation should be performed in response to DDL statement execution.<br/>
	///		Automatic indicates that reconciliation should be performed when schema objects are requested.<br/>
	/// </remarks>	
	[Flags]
	public enum ReconcileMode {None = 0, Startup = 1, Command = 2, Automatic = 4}

	/// <remarks>
	///		Determines which catalog should be designated as master for the purpose of schema reconciliation.<br/>
	///		Server indicates that the Dataphor server should be considered master.<br/>
	///		Device indicates that the Device should be considered master.<br/>
	///		Both indicates that each catalog should be upgraded to contain the other.<br/>
	/// </remarks>
	public enum ReconcileMaster { Server, Device, Both }

	public class ReconciliationSettings : D4Statement
	{
		// ReconcileModeSet
		private bool _reconcileModeSet;
		public bool ReconcileModeSet { get { return _reconcileModeSet; } }
		
		// ReconcileMode
		private ReconcileMode _reconcileMode;
		public ReconcileMode ReconcileMode
		{
			get { return _reconcileMode; }
			set 
			{
				_reconcileMode = value; 
				_reconcileModeSet = true;
			}
		}
		
		// ReconcileMasterSet
		private bool _reconcileMasterSet;
		public bool ReconcileMasterSet { get { return _reconcileMasterSet; } }
		
		// ReconcileMaster
		private ReconcileMaster _reconcileMaster;
		public ReconcileMaster ReconcileMaster
		{
			get { return _reconcileMaster; }
			set 
			{ 
				_reconcileMaster = value; 
				_reconcileMasterSet = true;
			}
		}
	}
	
	public class CreateDeviceStatement : D4Statement, IMetaData
	{
		// DeviceName
		protected string _deviceName = String.Empty;
		public string DeviceName
		{
			get { return _deviceName; }
			set { _deviceName = value == null ? String.Empty : value; }
		}
		
		// DeviceScalarTypeMaps
		private DeviceScalarTypeMaps _deviceScalarTypeMaps = new DeviceScalarTypeMaps();
		public DeviceScalarTypeMaps DeviceScalarTypeMaps { get { return _deviceScalarTypeMaps; } }

		// DeviceOperatorMaps
		private DeviceOperatorMaps _deviceOperatorMaps = new DeviceOperatorMaps();
		public DeviceOperatorMaps DeviceOperatorMaps { get { return _deviceOperatorMaps; } }
		
		// DeviceStoreDefinitions
		private DeviceStoreDefinitions _deviceStoreDefinitions = new DeviceStoreDefinitions();
		public DeviceStoreDefinitions DeviceStoreDefinitions { get { return _deviceStoreDefinitions; } }
		
		// ReconciliationSettings
		private ReconciliationSettings _reconciliationSettings;
		public ReconciliationSettings ReconciliationSettings
		{
			get { return _reconciliationSettings; }
			set { _reconciliationSettings = value; }
		}
		
		// ClassDefinition		
		protected ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return _classDefinition; }
			set { _classDefinition = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class AlterDeviceStatement : D4Statement, IAlterMetaData
	{
		// DeviceName
		protected string _deviceName = String.Empty;
		public string DeviceName
		{
			get { return _deviceName; }
			set { _deviceName = value == null ? String.Empty : value; }
		}
		
		// CreateDeviceScalarTypeMaps
		private DeviceScalarTypeMaps _createDeviceScalarTypeMaps = new DeviceScalarTypeMaps();
		public DeviceScalarTypeMaps CreateDeviceScalarTypeMaps { get { return _createDeviceScalarTypeMaps; } }

		// AlterDeviceScalarTypeMaps
		private AlterDeviceScalarTypeMaps _alterDeviceScalarTypeMaps = new AlterDeviceScalarTypeMaps();
		public AlterDeviceScalarTypeMaps AlterDeviceScalarTypeMaps { get { return _alterDeviceScalarTypeMaps; } }

		// DropDeviceScalarTypeMaps
		private DropDeviceScalarTypeMaps _dropDeviceScalarTypeMaps = new DropDeviceScalarTypeMaps();
		public DropDeviceScalarTypeMaps DropDeviceScalarTypeMaps { get { return _dropDeviceScalarTypeMaps; } }

		// CreateDeviceOperatorMaps
		private DeviceOperatorMaps _createDeviceOperatorMaps = new DeviceOperatorMaps();
		public DeviceOperatorMaps CreateDeviceOperatorMaps { get { return _createDeviceOperatorMaps; } }
		
		// AlterDeviceOperatorMaps
		private AlterDeviceOperatorMaps _alterDeviceOperatorMaps = new AlterDeviceOperatorMaps();
		public AlterDeviceOperatorMaps AlterDeviceOperatorMaps { get { return _alterDeviceOperatorMaps; } }

		// DropDeviceOperatorMaps
		private DropDeviceOperatorMaps _dropDeviceOperatorMaps = new DropDeviceOperatorMaps();
		public DropDeviceOperatorMaps DropDeviceOperatorMaps { get { return _dropDeviceOperatorMaps; } }

		// CreateDeviceStoreDefinitions
		private DeviceStoreDefinitions _createDeviceStoreDefinitions = new DeviceStoreDefinitions();
		public DeviceStoreDefinitions CreateDeviceStoreDefinitions { get { return _createDeviceStoreDefinitions; } }
		
		// AlterDeviceStoreDefinitions
		private AlterDeviceStoreDefinitions _alterDeviceStoreDefinitions = new AlterDeviceStoreDefinitions();
		public AlterDeviceStoreDefinitions AlterDeviceStoreDefinitions { get { return _alterDeviceStoreDefinitions; } }

		// DropDeviceStoreDefinitions
		private DropDeviceStoreDefinitions _dropDeviceStoreDefinitions = new DropDeviceStoreDefinitions();
		public DropDeviceStoreDefinitions DropDeviceStoreDefinitions { get { return _dropDeviceStoreDefinitions; } }

		// ReconciliationSettings
		private ReconciliationSettings _reconciliationSettings;
		public ReconciliationSettings ReconciliationSettings
		{
			get { return _reconciliationSettings; }
			set { _reconciliationSettings = value; }
		}
		
		// AlterClassDefinition		
		protected AlterClassDefinition _alterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return _alterClassDefinition; }
			set { _alterClassDefinition = value; }
		}
		
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}

	public abstract class AlterTableVarStatement : D4Statement, IAlterMetaData
	{
		// TableVarName
		protected string _tableVarName = String.Empty;
		public string TableVarName
		{
			get { return _tableVarName; }
			set { _tableVarName = value == null ? String.Empty : value; }
		}
		
		// CreateKeys
		private KeyDefinitions _createKeys = new KeyDefinitions();
		public KeyDefinitions CreateKeys { get { return _createKeys; } }

		// AlterKeys
		private AlterKeyDefinitions _alterKeys = new AlterKeyDefinitions();
		public AlterKeyDefinitions AlterKeys { get { return _alterKeys; } }

		// DropKeys
		private DropKeyDefinitions _dropKeys = new DropKeyDefinitions();
		public DropKeyDefinitions DropKeys { get { return _dropKeys; } }

		// CreateOrders
		private OrderDefinitions _createOrders = new OrderDefinitions();
		public OrderDefinitions CreateOrders { get { return _createOrders; } }

		// AlterOrders
		private AlterOrderDefinitions _alterOrders = new AlterOrderDefinitions();
		public AlterOrderDefinitions AlterOrders { get { return _alterOrders; } }

		// DropOrders
		private DropOrderDefinitions _dropOrders = new DropOrderDefinitions();
		public DropOrderDefinitions DropOrders { get { return _dropOrders; } }

		// CreateReferences
		private ReferenceDefinitions _createReferences = new ReferenceDefinitions();
		public ReferenceDefinitions CreateReferences { get { return _createReferences; } }

		// AlterReferences
		private AlterReferenceDefinitions _alterReferences = new AlterReferenceDefinitions();
		public AlterReferenceDefinitions AlterReferences { get { return _alterReferences; } }

		// DropReferences
		private DropReferenceDefinitions _dropReferences = new DropReferenceDefinitions();
		public DropReferenceDefinitions DropReferences { get { return _dropReferences; } }

		// CreateConstraints
		private CreateConstraintDefinitions _createConstraints = new CreateConstraintDefinitions();
		public CreateConstraintDefinitions CreateConstraints { get { return _createConstraints; } }

		// AlterConstraints
		private AlterConstraintDefinitions _alterConstraints = new AlterConstraintDefinitions();
		public AlterConstraintDefinitions AlterConstraints { get { return _alterConstraints; } }

		// DropConstraints
		private DropConstraintDefinitions _dropConstraints = new DropConstraintDefinitions();
		public DropConstraintDefinitions DropConstraints { get { return _dropConstraints; } }

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterTableStatement : AlterTableVarStatement
	{
		// CreateColumns
		private ColumnDefinitions _createColumns = new ColumnDefinitions();
		public ColumnDefinitions CreateColumns { get { return _createColumns; } }

		// AlterColumns
		private AlterColumnDefinitions _alterColumns = new AlterColumnDefinitions();
		public AlterColumnDefinitions AlterColumns { get { return _alterColumns; } }

		// DropColumns
		private DropColumnDefinitions _dropColumns = new DropColumnDefinitions();
		public DropColumnDefinitions DropColumns { get { return _dropColumns; } }
	}
	
	public class AlterViewStatement : AlterTableVarStatement{}
	
	public class AlterScalarTypeStatement : D4Statement, IAlterMetaData
	{
		// ScalarTypeName
		protected string _scalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value == null ? String.Empty : value; }
		}

		// CreateRepresentations
		private RepresentationDefinitions _createRepresentations = new RepresentationDefinitions();
		public RepresentationDefinitions CreateRepresentations { get { return _createRepresentations; } }

		// AlterRepresentations
		private AlterRepresentationDefinitions _alterRepresentations = new AlterRepresentationDefinitions();
		public AlterRepresentationDefinitions AlterRepresentations { get { return _alterRepresentations; } }

		// DropRepresentations
		private DropRepresentationDefinitions _dropRepresentations = new DropRepresentationDefinitions();
		public DropRepresentationDefinitions DropRepresentations { get { return _dropRepresentations; } }

		// CreateConstraints
		private ConstraintDefinitions _createConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions CreateConstraints { get { return _createConstraints; } }

		// AlterConstraints
		private AlterConstraintDefinitions _alterConstraints = new AlterConstraintDefinitions();
		public AlterConstraintDefinitions AlterConstraints { get { return _alterConstraints; } }

		// DropConstraints
		private DropConstraintDefinitions _dropConstraints = new DropConstraintDefinitions();
		public DropConstraintDefinitions DropConstraints { get { return _dropConstraints; } }

		// CreateSpecials
		private SpecialDefinitions _createSpecials = new SpecialDefinitions();
		public SpecialDefinitions CreateSpecials { get { return _createSpecials; } }

		// AlterSpecials
		private AlterSpecialDefinitions _alterSpecials = new AlterSpecialDefinitions();
		public AlterSpecialDefinitions AlterSpecials { get { return _alterSpecials; } }

		// DropSpecials
		private DropSpecialDefinitions _dropSpecials = new DropSpecialDefinitions();
		public DropSpecialDefinitions DropSpecials { get { return _dropSpecials; } }
		
		// Default		
		protected DefaultDefinitionBase _default;
		public DefaultDefinitionBase Default
		{
			get { return _default; }
			set { _default = value; }
		}
		
		// AlterClassDefinition		
		protected AlterClassDefinition _alterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return _alterClassDefinition; }
			set { _alterClassDefinition = value; }
		}
		
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class ColumnDefinitionBase : D4Statement
	{
		public ColumnDefinitionBase() : base(){}
		public ColumnDefinitionBase(string columnName) : base()
		{
			_columnName = columnName;
		}
		
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set 
			{ 
				if (_columnName != value)
				{
					_columnName = value == null ? String.Empty : value; 
				}
			}
		}
	}
	
	public class ColumnDefinition : ColumnDefinitionBase, IMetaData
	{
		public ColumnDefinition() : base(){}
		public ColumnDefinition(string columnName, TypeSpecifier typeSpecifier) : base(columnName)
		{
			_typeSpecifier = typeSpecifier;
		}
		
		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
		
		// Default		
		protected DefaultDefinition _default;
		public DefaultDefinition Default
		{
			get { return _default; }
			set { _default = value; }
		}

		// Constraints
		protected ConstraintDefinitions _constraints = new ConstraintDefinitions();
		public ConstraintDefinitions Constraints
		{
			get { return _constraints; }
			set { _constraints = value; }
		}
		
		// IsNilable
		protected bool _isNilable;
		public bool IsNilable
		{
			get { return _isNilable; }
			set { _isNilable = value; } 
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class ColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is ColumnDefinition))
				throw new LanguageException(LanguageException.Codes.ColumnDefinitionContainer);
			base.Validate(item);
		}
		
		public new ColumnDefinition this[int index]
		{
			get { return (ColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterColumnDefinition : ColumnDefinitionBase, IAlterMetaData
	{
		// TypeSpecifier
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
		
		// Default		
		protected DefaultDefinitionBase _default;
		public DefaultDefinitionBase Default
		{
			get { return _default; }
			set { _default = value; }
		}

		// CreateConstraints
		private ConstraintDefinitions _createConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions CreateConstraints { get { return _createConstraints; } }

		// AlterConstraints
		private AlterConstraintDefinitions _alterConstraints = new AlterConstraintDefinitions();
		public AlterConstraintDefinitions AlterConstraints { get { return _alterConstraints; } }

		// DropConstraints
		private DropConstraintDefinitions _dropConstraints = new DropConstraintDefinitions();
		public DropConstraintDefinitions DropConstraints { get { return _dropConstraints; } }
		
		// ChangeNilable
		private bool _changeNilable;
		public bool ChangeNilable
		{
			get { return _changeNilable; }
			set { _changeNilable = value; }
		}
		
		// IsNilable
		private bool _isNilable;
		public bool IsNilable
		{
			get { return _isNilable; }
			set { _isNilable = value; }
		}

		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterColumnDefinition))
				throw new LanguageException(LanguageException.Codes.AlterColumnDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterColumnDefinition this[int index]
		{
			get { return (AlterColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropColumnDefinition : ColumnDefinitionBase
	{
		public DropColumnDefinition() : base() { }
		public DropColumnDefinition(string columnName) : base(columnName) { }
	}
	
	public class DropColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropColumnDefinition))
				throw new LanguageException(LanguageException.Codes.DropColumnDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropColumnDefinition this[int index]
		{
			get { return (DropColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class KeyColumnDefinition : ColumnDefinitionBase
	{
		public KeyColumnDefinition() : base(){}
		public KeyColumnDefinition(string columnName) : base(columnName){}
	}

	public class KeyColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is KeyColumnDefinition))
				throw new LanguageException(LanguageException.Codes.KeyColumnDefinitionContainer);
			base.Validate(item);
		}
		
		public new KeyColumnDefinition this[int index]
		{
			get { return (KeyColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class ReferenceColumnDefinition : ColumnDefinitionBase
	{
		public ReferenceColumnDefinition() : base(){}
		public ReferenceColumnDefinition(string columnName) : base(columnName){}
	}

	public class ReferenceColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is ReferenceColumnDefinition))
				throw new LanguageException(LanguageException.Codes.ReferenceColumnDefinitionContainer);
			base.Validate(item);
		}
		
		public new ReferenceColumnDefinition this[int index]
		{
			get { return (ReferenceColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class OrderColumnDefinition : ColumnDefinitionBase
	{
		public OrderColumnDefinition() : base(){}
		public OrderColumnDefinition(string columnName, bool ascending) : base(columnName)
		{
			_ascending = ascending;
		}
		
		public OrderColumnDefinition(string columnName, bool ascending, SortDefinition sort) : base(columnName)
		{
			_ascending = ascending;
			_sort = sort;
		}
		
		public OrderColumnDefinition(string columnName, bool ascending, bool includeNils) : base(columnName)
		{
			_ascending = ascending;
			_includeNils = includeNils;
		}
		
		public OrderColumnDefinition(string columnName, bool ascending, bool includeNils, SortDefinition sort) : base(columnName)
		{
			_ascending = ascending;
			_includeNils = includeNils;
			_sort = sort;
		}
		
		protected bool _ascending = true;
		public bool Ascending
		{
			get { return _ascending; }
			set { _ascending = value; }
		}
		
		protected bool _includeNils = false;
		public bool IncludeNils
		{
			get { return _includeNils; }
			set { _includeNils = value; }
		}
		
		protected SortDefinition _sort;
		public SortDefinition Sort
		{
			get { return _sort; }
			set { _sort = value; }
		}
	}

	public class OrderColumnDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is OrderColumnDefinition))
				throw new LanguageException(LanguageException.Codes.OrderColumnDefinitionContainer);
			base.Validate(item);
		}
		
		public new OrderColumnDefinition this[int index]
		{
			get { return (OrderColumnDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public abstract class KeyDefinitionBase : D4Statement
	{
		// Columns
		protected KeyColumnDefinitions _columns = new KeyColumnDefinitions();
		public KeyColumnDefinitions Columns {  get { return _columns; } }
	}

	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.KeyEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
	public class KeyDefinition : KeyDefinitionBase, IMetaData
	{
		// MetaData
		protected MetaData _metaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.KeyDefinitionsEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
	public class KeyDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is KeyDefinition))
				throw new LanguageException(LanguageException.Codes.KeyDefinitionContainer);
			base.Validate(item);
		}
		
		public new KeyDefinition this[int index]
		{
			get { return (KeyDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterKeyDefinition : KeyDefinitionBase, IAlterMetaData
	{
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterKeyDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterKeyDefinition))
				throw new LanguageException(LanguageException.Codes.AlterKeyDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterKeyDefinition this[int index]
		{
			get { return (AlterKeyDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropKeyDefinition : KeyDefinitionBase{}
	
	public class DropKeyDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropKeyDefinition))
				throw new LanguageException(LanguageException.Codes.DropKeyDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropKeyDefinition this[int index]
		{
			get { return (DropKeyDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public abstract class ReferenceDefinitionBase : D4Statement
	{
		// ReferenceName
		protected string _referenceName = String.Empty;
		public string ReferenceName
		{
			get { return _referenceName; }
			set { _referenceName = value == null ? String.Empty : value; }
		}
	}
	
	public class ReferenceDefinition : ReferenceDefinitionBase, IMetaData
	{
		// constructor
		public ReferenceDefinition() : base()
		{
			_columns = new ReferenceColumnDefinitions();
		}
		
		// Columns
		protected ReferenceColumnDefinitions _columns;
		public ReferenceColumnDefinitions Columns { get { return _columns; } }

		// ReferencesDefinition		
		protected ReferencesDefinition _referencesDefinition;
		public ReferencesDefinition ReferencesDefinition
		{
			get { return _referencesDefinition; }
			set { _referencesDefinition = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class ReferenceDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is ReferenceDefinition))
				throw new LanguageException(LanguageException.Codes.ReferenceDefinitionContainer);
			base.Validate(item);
		}
		
		public new ReferenceDefinition this[int index]
		{
			get { return (ReferenceDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterReferenceDefinition : ReferenceDefinitionBase, IAlterMetaData
	{
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterReferenceDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterReferenceDefinition))
				throw new LanguageException(LanguageException.Codes.AlterReferenceDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterReferenceDefinition this[int index]
		{
			get { return (AlterReferenceDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropReferenceDefinition : ReferenceDefinitionBase{}
	
	public class DropReferenceDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropReferenceDefinition))
				throw new LanguageException(LanguageException.Codes.DropReferenceDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropReferenceDefinition this[int index]
		{
			get { return (DropReferenceDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public enum ReferenceAction {Require, Cascade, Clear, Set}
	
	public class ReferencesDefinition : D4Statement
	{
		// constructor
		public ReferencesDefinition() : base()
		{
			_columns = new ReferenceColumnDefinitions();
		}
		
		// Columns
		protected ReferenceColumnDefinitions _columns;
		public ReferenceColumnDefinitions Columns { get { return _columns; } }

		// TableVarName
		protected string _tableVarName = String.Empty;
		public string TableVarName
		{
			get { return _tableVarName; }
			set { _tableVarName = value == null ? String.Empty : value; }
		}
		
		// UpdateReferenceAction
		protected ReferenceAction _updateReferenceAction;
		public ReferenceAction UpdateReferenceAction
		{
			get { return _updateReferenceAction; }
			set { _updateReferenceAction = value; }
		}
		
		// UpdateReferenceExpressions
		protected Expressions _updateReferenceExpressions = new Expressions();
		public Expressions UpdateReferenceExpressions { get { return _updateReferenceExpressions; } }
		
		// DeleteReferenceAction
		protected ReferenceAction _deleteReferenceAction;
		public ReferenceAction DeleteReferenceAction
		{
			get { return _deleteReferenceAction; }
			set { _deleteReferenceAction = value; }
		}

		// DeleteReferenceExpressions
		protected Expressions _deleteReferenceExpressions = new Expressions();
		public Expressions DeleteReferenceExpressions { get { return _deleteReferenceExpressions; } }
	}
	
	public abstract class OrderDefinitionBase : D4Statement
	{
		// Columns
		protected OrderColumnDefinitions _columns = new OrderColumnDefinitions();
		public OrderColumnDefinitions Columns { get { return _columns; } }
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.OrderEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
	public class OrderDefinition : OrderDefinitionBase, IMetaData
	{
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.OrderDefinitionsEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
	public class OrderDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is OrderDefinition))
				throw new LanguageException(LanguageException.Codes.OrderDefinitionContainer);
			base.Validate(item);
		}
		
		public new OrderDefinition this[int index]
		{
			get { return (OrderDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterOrderDefinition : OrderDefinitionBase, IAlterMetaData
	{
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterOrderDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterOrderDefinition))
				throw new LanguageException(LanguageException.Codes.AlterOrderDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterOrderDefinition this[int index]
		{
			get { return (AlterOrderDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropOrderDefinition : OrderDefinitionBase{}
	
	public class DropOrderDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropOrderDefinition))
				throw new LanguageException(LanguageException.Codes.DropOrderDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropOrderDefinition this[int index]
		{
			get { return (DropOrderDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public abstract class ConstraintDefinitionBase : D4Statement
	{
		// ConstraintName
		protected string _constraintName = String.Empty;
		public string ConstraintName
		{
			get { return _constraintName; }
			set 
			{ 
				if (_constraintName != value)
				{
					_constraintName = value == null ? String.Empty : value; 
				}
			}
		}
	}
	
	public class CreateConstraintDefinition : ConstraintDefinitionBase, IMetaData
	{
		public CreateConstraintDefinition() : base() {}
		public CreateConstraintDefinition(string name, MetaData metaData)
		{
			_constraintName = name;
			_metaData = metaData;
		}

		private bool _isGenerated;
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class ConstraintDefinition : CreateConstraintDefinition
	{
		public ConstraintDefinition() : base(){}
		public ConstraintDefinition(string name, Expression expression, MetaData metaData) : base(name, metaData)
		{
			_expression = expression;
		}
		
		// Expression		
		protected Expression _expression;
		[Browsable(false)]
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		private string ExpressionToString(Expression expression)
		{
			if (expression == null)
				return String.Empty;
			else
			{
				D4TextEmitter emitter = new D4TextEmitter();
				return emitter.Emit(expression);
			}
		}

		private Expression StringToExpression(string tempValue)
		{
			Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
			return parser.ParseExpression(tempValue);
		}

		[Description("D4 constraint expression.")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.D4ExpressionEmitEdit,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public string ExpressionString
		{
			get { return _expression != null ? ExpressionToString(_expression) : String.Empty; }

			set
			{
				if (ExpressionString != value)
					Expression = value == String.Empty ? null : StringToExpression(value);
			}
		}
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.ConstraintsEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
	public class ConstraintDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is ConstraintDefinition))
				throw new LanguageException(LanguageException.Codes.ConstraintDefinitionContainer);
			base.Validate(item);
		}
		
		public new ConstraintDefinition this[int index]
		{
			get { return (ConstraintDefinition)base[index]; }
			set { base[index] = value; }
		}

		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Compare(name, this[index].ConstraintName) == 0)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class CreateConstraintDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is CreateConstraintDefinition))
				throw new LanguageException(LanguageException.Codes.ConstraintDefinitionContainer);
			base.Validate(item);
		}
		
		public new CreateConstraintDefinition this[int index]
		{
			get { return (CreateConstraintDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class CreateConstraintStatement : ConstraintDefinition
	{
		// IsSession
		protected bool _isSession = false;
		public bool IsSession
		{
			get { return _isSession; }
			set { _isSession = value; }
		}
	}

	public class TransitionConstraintDefinition : CreateConstraintDefinition
	{
		public TransitionConstraintDefinition() : base() {}
		
		// OnInsertExpression		
		protected Expression _onInsertExpression;
		public Expression OnInsertExpression
		{
			get { return _onInsertExpression; }
			set { _onInsertExpression = value; }
		}

		// OnUpdateExpression		
		protected Expression _onUpdateExpression;
		public Expression OnUpdateExpression
		{
			get { return _onUpdateExpression; }
			set { _onUpdateExpression = value; }
		}

		// OnDeleteExpression		
		protected Expression _onDeleteExpression;
		public Expression OnDeleteExpression
		{
			get { return _onDeleteExpression; }
			set { _onDeleteExpression = value; }
		}
	}
	
	public class AlterConstraintDefinitionBase : ConstraintDefinitionBase, IAlterMetaData
	{
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class AlterConstraintDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is AlterConstraintDefinitionBase))
				throw new LanguageException(LanguageException.Codes.AlterConstraintDefinitionContainer);
			base.Validate(item);
		}
		
		public new AlterConstraintDefinitionBase this[int index]
		{
			get { return (AlterConstraintDefinitionBase)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class AlterConstraintDefinition : AlterConstraintDefinitionBase
	{
		// Expression		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class AlterConstraintStatement : AlterConstraintDefinition {}
	
	public class AlterTransitionConstraintDefinitionItemBase : D4Statement {}
	
	public class AlterTransitionConstraintDefinitionCreateItem : AlterTransitionConstraintDefinitionItemBase
	{
		public AlterTransitionConstraintDefinitionCreateItem() : base() {}
		public AlterTransitionConstraintDefinitionCreateItem(Expression expression) : base()
		{
			_expression = expression;
		}
		
		// Expression		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class AlterTransitionConstraintDefinitionAlterItem : AlterTransitionConstraintDefinitionItemBase
	{
		public AlterTransitionConstraintDefinitionAlterItem() : base() {}
		public AlterTransitionConstraintDefinitionAlterItem(Expression expression) : base()
		{
			_expression = expression;
		}
		
		// Expression		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class AlterTransitionConstraintDefinitionDropItem : AlterTransitionConstraintDefinitionItemBase {}
	
	public class AlterTransitionConstraintDefinition : AlterConstraintDefinitionBase
	{
		private AlterTransitionConstraintDefinitionItemBase _onInsert;
		public AlterTransitionConstraintDefinitionItemBase OnInsert
		{
			get { return _onInsert; }
			set { _onInsert = value; }
		}
		
		private AlterTransitionConstraintDefinitionItemBase _onUpdate;
		public AlterTransitionConstraintDefinitionItemBase OnUpdate
		{
			get { return _onUpdate; }
			set { _onUpdate = value; }
		}
		
		private AlterTransitionConstraintDefinitionItemBase _onDelete;
		public AlterTransitionConstraintDefinitionItemBase OnDelete
		{
			get { return _onDelete; }
			set { _onDelete = value; }
		}
	}
	
	public class DropConstraintDefinition : ConstraintDefinitionBase
	{
		public DropConstraintDefinition() : base(){}
		public DropConstraintDefinition(string constraintName) : base()
		{
			ConstraintName = constraintName;
		}

		// IsTransition
		protected bool _isTransition;
		public bool IsTransition
		{
			get { return _isTransition; }
			set { _isTransition = value; }
		}
	}

	public class DropConstraintDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is DropConstraintDefinition))
				throw new LanguageException(LanguageException.Codes.DropConstraintDefinitionContainer);
			base.Validate(item);
		}
		
		public new DropConstraintDefinition this[int index]
		{
			get { return (DropConstraintDefinition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class DropConstraintStatement : DropConstraintDefinition {}
	
	public class CreateConversionStatement : D4Statement, IMetaData
	{
		public CreateConversionStatement() : base() {}
		
		private TypeSpecifier _sourceScalarTypeName;
		public TypeSpecifier SourceScalarTypeName
		{
			get { return _sourceScalarTypeName; }
			set { _sourceScalarTypeName = value; }
		}
		
		private TypeSpecifier _targetScalarTypeName;
		public TypeSpecifier TargetScalarTypeName
		{
			get { return _targetScalarTypeName; }
			set { _targetScalarTypeName = value; }
		}
		
		private IdentifierExpression _operatorName;
		public IdentifierExpression OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value; }
		}
		
		private bool _isNarrowing = true;
		public bool IsNarrowing
		{
			get { return _isNarrowing; }
			set { _isNarrowing = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class DropConversionStatement : D4Statement
	{
		public DropConversionStatement() : base() {}
		
		private TypeSpecifier _sourceScalarTypeName;
		public TypeSpecifier SourceScalarTypeName
		{
			get { return _sourceScalarTypeName; }
			set { _sourceScalarTypeName = value; }
		}
		
		private TypeSpecifier _targetScalarTypeName;
		public TypeSpecifier TargetScalarTypeName
		{
			get { return _targetScalarTypeName; }
			set { _targetScalarTypeName = value; }
		}
	}
	
	public class CreateRoleStatement : D4Statement, IMetaData
	{
		// RoleName
		protected string _roleName;
		public string RoleName
		{
			get { return _roleName; }
			set { _roleName = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class AlterRoleStatement : D4Statement, IAlterMetaData
	{
		// RoleName
		protected string _roleName;
		public string RoleName
		{
			get { return _roleName; }
			set { _roleName = value; }
		}

		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class DropRoleStatement : D4Statement
	{
		// RoleName
		protected string _roleName;
		public string RoleName
		{
			get { return _roleName; }
			set { _roleName = value; }
		}
	}
	
	public class CreateRightStatement : D4Statement
	{
		// RightName
		protected string _rightName;
		public string RightName
		{
			get { return _rightName; }
			set { _rightName = value; }
		}
	}
	
	public class DropRightStatement : D4Statement
	{
		// RightName
		protected string _rightName;
		public string RightName
		{
			get { return _rightName; }
			set { _rightName = value; }
		}
	}
	
	public abstract class SortDefinitionBase : D4Statement {}
	
	public class SortDefinition : SortDefinitionBase, IMetaData
	{
		public SortDefinition() : base() {}
		public SortDefinition(Expression expression)
		{
			_expression = expression;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class CreateSortStatement : SortDefinition
	{
		public CreateSortStatement() : base() {}
		public CreateSortStatement(string scalarTypeName, Expression expression) : base(expression)
		{
			_scalarTypeName = scalarTypeName;
		}
		
		// ScalarTypeName
		protected string _scalarTypeName;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value; }
		}
	}
	
	public class AlterSortDefinition : SortDefinitionBase, IAlterMetaData
	{
		public AlterSortDefinition() : base() {}
		public AlterSortDefinition(Expression expression)
		{
			_expression = expression;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}

		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}

	public class AlterSortStatement : AlterSortDefinition
	{
		public AlterSortStatement() : base() {}
		public AlterSortStatement(string scalarTypeName, Expression expression) : base(expression)
		{
			_scalarTypeName = scalarTypeName;
		}
		
		// ScalarTypeName
		protected string _scalarTypeName;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value; }
		}
	}
	
	public class DropSortDefinition : SortDefinitionBase {}
	
	public class DropSortStatement : DropSortDefinition
	{
		public DropSortStatement() : base() {}
		public DropSortStatement(string scalarTypeName) : base()
		{
			_scalarTypeName = scalarTypeName;
		}
		
		public DropSortStatement(string scalarTypeName, bool isUnique) : base()
		{
			_scalarTypeName = scalarTypeName;
			_isUnique = isUnique;
		}
		
		// ScalarTypeName
		protected string _scalarTypeName;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value; }
		}
		
		private bool _isUnique;
		public bool IsUnique
		{
			get { return _isUnique; }
			set { _isUnique = value; }
		}
	}

	public abstract class DefaultDefinitionBase : D4Statement {}

	public class DefaultDefinition : DefaultDefinitionBase, IMetaData
	{
		public DefaultDefinition() : base(){}
		public DefaultDefinition(Expression expression, MetaData metaData)
		{
			_expression = expression;
			_metaData = metaData;
		}
		
		// Expression		
		protected Expression _expression;
		[Browsable(false)]
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		private bool _isGenerated;
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}
		
		private string ExpressionToString(Expression expression)
		{
			if (expression == null)
				return String.Empty;
			else
			{
				D4TextEmitter emitter = new D4TextEmitter();
				return emitter.Emit(expression);
			}
		}

		private Expression StringToExpression(string tempValue)
		{
			Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
			return parser.ParseExpression(tempValue);
		}

		[Description("Default expression. For example value = 0")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.D4ExpressionEmitEdit,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public string ExpressionString
		{
			get { return _expression != null ? ExpressionToString(_expression) : String.Empty; }

			set
			{
				if (ExpressionString != value)
					Expression = value == String.Empty ? null : StringToExpression(value);
			}
		}
		
		// MetaData
		protected MetaData _metaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}

	public class AlterDefaultDefinition : DefaultDefinitionBase, IAlterMetaData
	{
		// Expression		
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		// AlterMetaData
		protected AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
	}
	
	public class DropDefaultDefinition : DefaultDefinitionBase{}
	
	public class ClassDefinition : D4Statement
	{
		// constructor
		public ClassDefinition() : base(){}
		public ClassDefinition(string className) : base()
		{
			ClassName = className;
		}
		
		public ClassDefinition(string className, ClassAttributeDefinition[] attributes) : base()
		{
			ClassName = className;
			_attributes.AddRange(attributes);
		}
		
		// ClassName
		protected string _className = String.Empty;
		public string ClassName
		{
			get { return _className; }
			set 
			{ 
				if (_className != value)
					_className = value == null ? String.Empty : value;
			}
		}

		// Attributes
		protected ClassAttributeDefinitions _attributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions Attributes { get { return _attributes; } }
		
		public virtual object Clone()
		{
			ClassDefinition classDefinition = new ClassDefinition(_className);
			foreach (ClassAttributeDefinition attribute in _attributes)
				classDefinition.Attributes.Add(attribute.Clone());
			return classDefinition;
		}
	}
	
	public class AlterClassDefinition : D4Statement
	{
		private string _className = string.Empty;
		public string ClassName
		{
			get { return _className; }
			set { _className = value == null ? String.Empty : value; }
		}
		
		private ClassAttributeDefinitions _createAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions CreateAttributes { get { return _createAttributes; } }

		private ClassAttributeDefinitions _alterAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions AlterAttributes { get { return _alterAttributes; } }

		private ClassAttributeDefinitions _dropAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions DropAttributes { get { return _dropAttributes; } }
	}
	
	public class ClassAttributeDefinitions : Statements
	{
		protected override void Validate(object item)
		{
			ClassAttributeDefinition attribute = item as ClassAttributeDefinition;
			if (attribute == null)
				throw new LanguageException(LanguageException.Codes.ClassAttributeDefinitionContainer);
			if (IndexOf(attribute.AttributeName) >= 0)
				throw new LanguageException(LanguageException.Codes.DuplicateAttributeDefinition, attribute.AttributeName);
			base.Validate(item);
		}
		
		public new ClassAttributeDefinition this[int index]
		{
			get { return (ClassAttributeDefinition)base[index]; }
			set { base[index] = value; }
		}
		
		public ClassAttributeDefinition this[string name]
		{
			get
			{
				int index = IndexOf(name);
				if (index >= 0)
					return this[index];
				else
					throw new LanguageException(LanguageException.Codes.ClassAttributeNotFound, name);
			}
			set
			{
				int index = IndexOf(name);
				if (index >= 0)
					this[index] = value;
				else
					throw new LanguageException(LanguageException.Codes.ClassAttributeNotFound, name);
			}
		}
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Equals(this[index].AttributeName, name, StringComparison.OrdinalIgnoreCase))
					return index;
			return -1;
		}
	}
	
	public class ClassAttributeDefinition : D4Statement
	{
		// constructor
		public ClassAttributeDefinition() : base(){}
		public ClassAttributeDefinition(string name, string tempValue)
		{
			AttributeName = name;
			AttributeValue = tempValue;
		}

		// AttributeName
		protected string _attributeName = String.Empty;
		public string AttributeName
		{
			get { return _attributeName; }
			set { _attributeName = value == null ? String.Empty : value; }
		}
		
		// AttributeValue
		protected string _attributeValue = String.Empty;
		public string AttributeValue
		{
			get { return _attributeValue; }
			set { _attributeValue = value == null ? String.Empty : value; }
		}
		
		public virtual object Clone()
		{
			ClassAttributeDefinition attribute = new ClassAttributeDefinition();
			attribute.AttributeName = _attributeName;
			attribute.AttributeValue = _attributeValue;
			return attribute;
		}
	}

	public class NamedTypeSpecifiers : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is NamedTypeSpecifier))
				throw new LanguageException(LanguageException.Codes.NamedTypeSpecifierContainer);
			base.Validate(item);
		}
		
		public new NamedTypeSpecifier this[int index]
		{
			get { return (NamedTypeSpecifier)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class NamedTypeSpecifier : D4Statement
	{
		// Identifier
		protected string _identifier = String.Empty;
		public string Identifier
		{
			get { return _identifier; }
			set { _identifier = value == null ? String.Empty : value; }
		}
		
		// TypeSpecifier		
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
	}
	
	public class FormalParameters : NamedTypeSpecifiers
	{
		protected override void Validate(object item)
		{
			if (!(item is FormalParameter))
				throw new LanguageException(LanguageException.Codes.FormalParameterContainer);
			base.Validate(item);
		}
		
		public new FormalParameter this[int index]
		{
			get { return (FormalParameter)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class FormalParameter : NamedTypeSpecifier
	{
		protected Modifier _modifier;
		public Modifier Modifier
		{
			get { return _modifier; }
			set { _modifier = value; }
		}
	}
	
	public abstract class TypeSpecifier : D4Statement
	{
		// IsGeneric
		private bool _isGeneric;
		public bool IsGeneric
		{
			get { return _isGeneric; }
			set { _isGeneric = value; }
		}
	}
	
	public class GenericTypeSpecifier : TypeSpecifier
	{
		public GenericTypeSpecifier() : base()
		{
			IsGeneric = true;
		}
	}

	public class ScalarTypeSpecifier : TypeSpecifier
	{
		public ScalarTypeSpecifier() : base(){}
		public ScalarTypeSpecifier(string scalarTypeName) : base()
		{
			_scalarTypeName = scalarTypeName;
		}
		
		// ScalarTypeName
		protected string _scalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	public class RowTypeSpecifier : TypeSpecifier
	{
		// constructor
		public RowTypeSpecifier() : base()
		{
			_columns = new NamedTypeSpecifiers();
		}
		
		// Columns
		protected NamedTypeSpecifiers _columns;
		public NamedTypeSpecifiers Columns { get { return _columns; } }
	}
	
	public class TableTypeSpecifier : TypeSpecifier
	{
		// constructor
		public TableTypeSpecifier() : base()
		{
			_columns = new NamedTypeSpecifiers();
		}

		// Columns
		protected NamedTypeSpecifiers _columns;
		public NamedTypeSpecifiers Columns { get { return _columns; } }
	}
	
	public class TypeOfTypeSpecifier : TypeSpecifier
	{
		public TypeOfTypeSpecifier() : base(){}
		public TypeOfTypeSpecifier(Expression expression) : base()
		{
			_expression = expression;
		}
		
		// Expression
		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
	}
	
	public class ListTypeSpecifier : TypeSpecifier
	{
		public ListTypeSpecifier() : base(){}
		public ListTypeSpecifier(TypeSpecifier typeSpecifier) : base()
		{
			_typeSpecifier = typeSpecifier;
		}

		// TypeSpecifier		
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
	}
	
	public class FormalParameterSpecifier : D4Statement
	{
		public FormalParameterSpecifier() : base(){}
		public FormalParameterSpecifier(Modifier modifier, TypeSpecifier typeSpecifier) : base()
		{
			_modifier = modifier;
			_typeSpecifier = typeSpecifier;
		}
		
		protected Modifier _modifier;
		public Modifier Modifier
		{
			get { return _modifier; }
			set { _modifier = value; }
		}
		
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
	}
	
	public class FormalParameterSpecifiers : Statements
	{										
		protected override void Validate(object item)
		{
			if (!(item is FormalParameterSpecifier))
				throw new LanguageException(LanguageException.Codes.FormalParameterSpecifierContainer);
			base.Validate(item);
		}
		
		public new FormalParameterSpecifier this[int index]
		{
			get { return (FormalParameterSpecifier)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class OperatorTypeSpecifier : TypeSpecifier
	{
		protected FormalParameterSpecifiers _typeSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers TypeSpecifiers { get { return _typeSpecifiers; } }
	}

	public class CursorTypeSpecifier : TypeSpecifier
	{
		public CursorTypeSpecifier() : base(){}
		public CursorTypeSpecifier(TypeSpecifier typeSpecifier) : base()
		{
			_typeSpecifier = typeSpecifier;
		}

		// TypeSpecifier		
		protected TypeSpecifier _typeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return _typeSpecifier; }
			set { _typeSpecifier = value; }
		}
	}

	public abstract class DropObjectStatement : D4Statement
	{
		public DropObjectStatement() : base(){}
		public DropObjectStatement(string objectName)
		{
			_objectName = objectName;
		}
		
		// ObjectName
		protected string _objectName = String.Empty;
		public string ObjectName
		{
			get { return _objectName; }
			set { _objectName = value == null ? String.Empty : value; }
		}
	}

	public class DropTableStatement : DropObjectStatement
	{
		public DropTableStatement() : base(){}
		public DropTableStatement(string objectName) : base(objectName){}
	}
	
	public class DropViewStatement : DropObjectStatement
	{
		public DropViewStatement() : base(){}
		public DropViewStatement(string objectName) : base(objectName){}
	}

	public class DropScalarTypeStatement : DropObjectStatement
	{
		public DropScalarTypeStatement() : base(){}
		public DropScalarTypeStatement(string objectName) : base(objectName){}
	}
	
	public class DropOperatorStatement : DropObjectStatement
	{
		public DropOperatorStatement() : base(){}
		public DropOperatorStatement(string objectName) : base(objectName){}

		// FormalParameterSpecifiers
		protected FormalParameterSpecifiers _formalParameterSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers FormalParameterSpecifiers { get { return _formalParameterSpecifiers; } }
	}

	public class DropServerStatement : DropObjectStatement
	{
		public DropServerStatement() : base(){}
		public DropServerStatement(string objectName) : base(objectName){}
	}

	public class DropDeviceStatement : DropObjectStatement
	{
		public DropDeviceStatement() : base(){}
		public DropDeviceStatement(string objectName) : base(objectName){}
	}
	
	public abstract class EventSourceSpecifier : D4Statement
	{
		public EventSourceSpecifier() : base(){}
	}
	
	public class ObjectEventSourceSpecifier : EventSourceSpecifier
	{
		public ObjectEventSourceSpecifier() : base(){}
		public ObjectEventSourceSpecifier(string objectName) : base()
		{
			ObjectName = objectName;
		}
		
		// ObjectName
		protected string _objectName = String.Empty;
		public string ObjectName
		{
			get { return _objectName; }
			set { _objectName = value == null ? String.Empty : value; }
		}
	}
	
	public class ColumnEventSourceSpecifier : EventSourceSpecifier
	{
		public ColumnEventSourceSpecifier() : base(){}
		public ColumnEventSourceSpecifier(string tableVarName, string columnName) : base()
		{
			TableVarName = tableVarName;
			ColumnName = columnName;
		}

		// TableVarName
		protected string _tableVarName = String.Empty;
		public string TableVarName
		{
			get { return _tableVarName; }
			set { _tableVarName = value == null ? String.Empty : value; }
		}

		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}
	}
	
	public class ScalarTypeEventSourceSpecifier : EventSourceSpecifier
	{
		// ScalarTypeName
		protected string _scalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return _scalarTypeName; }
			set { _scalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	[Flags] 
	public enum EventType { BeforeInsert = 1, AfterInsert = 2, BeforeUpdate = 4, AfterUpdate = 8, BeforeDelete = 16, AfterDelete = 32, Default = 64, Validate = 128, Change = 256 }
	
	public class EventSpecifier : D4Statement 
	{
		protected EventType _eventType;
		public EventType EventType
		{
			get { return _eventType; }
			set { _eventType = value; }
		}
	}
	
	public abstract class AttachStatementBase : D4Statement
	{
		// OperatorName
		protected string _operatorName;
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value; }
		}
		
		// EventSourceSpecifier
		protected EventSourceSpecifier _eventSourceSpecifier;
		public EventSourceSpecifier EventSourceSpecifier
		{
			get { return _eventSourceSpecifier; }
			set { _eventSourceSpecifier = value; }
		}

		// EventSpecifier
		protected EventSpecifier _eventSpecifier;
		public EventSpecifier EventSpecifier
		{
			get { return _eventSpecifier; }
			set { _eventSpecifier = value; }
		}
	}
	
	public class AttachStatement : AttachStatementBase, IMetaData
	{
		// BeforeOperatorNames
		protected List<string> _beforeOperatorNames = new List<string>();
		public List<string> BeforeOperatorNames { get { return _beforeOperatorNames; } }
		
		// IsGenerated
		private bool _isGenerated;
		public bool IsGenerated
		{
			get { return _isGenerated; }
			set { _isGenerated = value; }
		}

		// MetaData
		protected MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
	}
	
	public class InvokeStatement : AttachStatementBase
	{
		// BeforeOperatorNames
		protected List<string> _beforeOperatorNames = new List<string>();
		public List<string> BeforeOperatorNames { get { return _beforeOperatorNames; } }
	}

	public class DetachStatement : AttachStatementBase {}
	
	public class RightSpecifier : D4Statement
	{
		public RightSpecifier() : base() {}
		public RightSpecifier(string rightName) : base()
		{
			RightName = rightName;
		}
		
		// RightName
		protected string _rightName = String.Empty;
		public string RightName
		{
			get { return _rightName; }
			set { _rightName = value == null ? String.Empty : value; }
		}
	}
	
	public class RightSpecifiers : Statements
	{
		protected override void Validate(object item)
		{
			if (!(item is RightSpecifier))
				throw new LanguageException(LanguageException.Codes.StatementContainer);
			base.Validate(item);
		}
		
		public new RightSpecifier this[int index]
		{
			get { return (RightSpecifier)base[index]; }
			set { base[index] = value; }
		}
	}

	public class CatalogObjectSpecifier : D4Statement
	{
		// ObjectName
		protected string _objectName = String.Empty;
		public string ObjectName
		{
			get { return _objectName; }
			set { _objectName = value == null ? String.Empty : value; }
		}
		
		private bool _isOperator;
		public bool IsOperator 
		{ 
			get { return _isOperator; } 
			set { _isOperator = value; } 
		}

		// FormalParameterSpecifiers
		protected FormalParameterSpecifiers _formalParameterSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers FormalParameterSpecifiers { get { return _formalParameterSpecifiers; } }
	}
	
	public enum GranteeType { User, Group, Role }

	public enum RightSpecifierType { All, Usage, List }
	
	public class RightStatementBase : D4Statement
	{
		private RightSpecifierType _rightType;
		public RightSpecifierType RightType
		{
			get { return _rightType; }
			set { _rightType = value; }
		}
		
		private RightSpecifiers _rights = new RightSpecifiers();
		public RightSpecifiers Rights { get { return _rights; } }

		private CatalogObjectSpecifier _target;
		public CatalogObjectSpecifier Target
		{
			get { return _target; }
			set { _target = value; }
		}

		private GranteeType _granteeType;
		public GranteeType GranteeType
		{
			get { return _granteeType; }
			set { _granteeType = value; }
		}		
		
		private string _grantee;
		public string Grantee
		{
			get { return _grantee; }
			set { _grantee = value; }
		}
		
		private bool _isInherited;
		public bool IsInherited
		{
			get { return _isInherited; }
			set { _isInherited = value; }
		}
		
		private bool _applyRecursively;
		public bool ApplyRecursively
		{
			get { return _applyRecursively; }
			set { _applyRecursively = value; }
		}
		
		private bool _includeUsers;
		public bool IncludeUsers
		{
			get { return _includeUsers; }
			set { _includeUsers = value; }
		}
	}
	
	public class GrantStatement : RightStatementBase {}
	public class RevokeStatement : RightStatementBase {}
	public class RevertStatement : RightStatementBase {}
}
