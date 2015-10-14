/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.SQL
{
    using System;
    using System.Collections;

    using Alphora.Dataphor.DAE.Language;
    
    /*
    
		QueryLanguage Hierarchy ->
		
			Language.Statement
				|- Language.Expression
				|	|- Language.CallExpression
				|	|	|- AggregateCallExpression
				|	|- FieldExpression
				|	|	|- InsertFieldExpression
				|	|	|- UpdateFieldExpression
				|	|	|- OrderFieldExpression
				|	|	|- QualifiedFieldExpression
				|	|- ColumnExpression
				|	|- CastExpression
				|	|- TableExpression
				|	|- TableSpecifier
				|	|- SelectExpression
				|	|- TableOperatorExpression
				|	|- QueryExpression
				|	|- ValuesExpression
				|	|- ConstraintValueExpression
				|	|- ConstraintRecordValueExpression
				|	|- QueryParameterExpression
				|- Clause
				|	|- SelectClause
				|	|- JoinClause
				|	|- FromClause
				|	|	|- AlgebraicFromClause
				|	|	|- CalculusFromClause
				|	|- FilterClause
				|	|	|- WhereClause
				|	|	|- HavingClause
				|	|- GroupClause
				|	|- OrderClause
				|	|- UpdateClause
				|	|- DeleteClause
				|	|- InsertClause
				|- SelectStatement
				|- InsertStatement				
				|- UpdateStatement
				|- DeleteStatement
				|- CreateTableStatement
				|- ColumnDefinition
				|- AlterTableStatement
				|- DropTableStatement
				|- CreateIndexStatement
				|- OrderColumnDefinition
				|- AlterIndexStatement
				|- DropIndexStatement

			Language.Statements
				|- Language.Expressions
				|	|- ColumnExpressions
				|	|- OrderFieldExpressions
				|	|- InsertFieldExpressions
				|	|- UnionExpressions		
				|	|- UpdateFieldExpressions						
				|- JoinClauses
				|- ColumnDefinitions
				|- OrderColumnDefinitions
				  
	*/

    public enum JoinType {Cross, Inner, Left, Right, Full};
    public enum AggregationType {None, Count, Sum, Min, Max, Avg};
    
    public class AggregateCallExpression : CallExpression
    {
		public AggregateCallExpression() : base(){}
		
		protected bool _isDistinct;
		public bool IsDistinct
		{
			get { return _isDistinct; }
			set { _isDistinct = value; }
		}
		
		protected bool _isRowLevel;
		public bool IsRowLevel
		{
			get { return _isRowLevel; }
			set { _isRowLevel = value; }
		}
    }
    
    public class UserExpression : Expression
    {
		public UserExpression() : base() {}
		public UserExpression(string translationString, Expression[] arguments) : base()
		{
			_translationString = translationString;
			_expressions.AddRange(arguments);
		}
		
		private string _translationString = String.Empty;
		public string TranslationString
		{
			get { return _translationString; }
			set { _translationString = value == null ? String.Empty : value; }
		}
        
        // Expressions
        protected Expressions _expressions = new Expressions();
        public Expressions Expressions { get { return _expressions; } }
	}
    
    public class QueryParameterExpression : Expression
    {
		public QueryParameterExpression() : base(){}
		public QueryParameterExpression(string parameterName) : base()
		{
			ParameterName = parameterName;
		}
		
        // ParameterName
        protected string _parameterName = String.Empty;
        public string ParameterName
        {
            get { return _parameterName; }
            set { _parameterName = value; }
        }
    }
    
    public abstract class FieldExpression : Expression
    {
		public FieldExpression() : base(){}
		public FieldExpression(string fieldName) : base()
		{
			FieldName = fieldName;
		}
		
        // FieldName
        protected string _fieldName = String.Empty;
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }
    }
    
    public class InsertFieldExpression : FieldExpression
    {	
		public InsertFieldExpression(string fieldName) : base(fieldName){}
    }
    
    public class UpdateFieldExpression : FieldExpression
    {
        // Expression
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
    }
    
    public class OrderFieldExpression : QualifiedFieldExpression
    {
        public OrderFieldExpression() : base()
        {
            _ascending = true;
            _nullsFirst = null;
        }
        
        public OrderFieldExpression(string fieldName, string tableAlias, bool ascending, bool? nullsFirst) : base(fieldName, tableAlias)
        {
			_ascending = ascending;
			_nullsFirst = nullsFirst;
        }
        
        // Ascending
        protected bool _ascending;
        public bool Ascending
        {
            get { return _ascending; }
            set { _ascending = value; }
        }

        // NullsFirst
        protected bool? _nullsFirst;
        public bool? NullsFirst
        {
            get { return _nullsFirst; }
            set { _nullsFirst = value; }
        }
    }

    public class QualifiedFieldExpression : FieldExpression
    {
		public QualifiedFieldExpression() : base(){}
		public QualifiedFieldExpression(string fieldName) : base(fieldName){}
		public QualifiedFieldExpression(string fieldName, string tableAlias) : base(fieldName)
		{
			TableAlias = tableAlias;
		}
		
        // TableAlias        
        protected string _tableAlias = String.Empty;
        public string TableAlias
        {
            get { return _tableAlias; }
            set { _tableAlias = value; }
        }
    }
    
    public class ColumnExpression : Expression
    {
		public ColumnExpression() : base(){}
		public ColumnExpression(Expression expression) : base()
		{
			Expression = expression;
		}
		
		public ColumnExpression(Expression expression, string columnAlias) : base()
		{
			Expression = expression;
			ColumnAlias = columnAlias;
		}

        // ColumnAlias
        protected string _columnAlias = String.Empty;
        public string ColumnAlias
        {
            get { return _columnAlias; }
            set { _columnAlias = value; }
        }
        
        // Expression
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
    }
    
    public class CastExpression : Expression
    {
		public CastExpression() : base(){}
		public CastExpression(Expression expression, string domainName) : base()
		{
			_expression = expression;
			_domainName = domainName;
		}

        // Expression
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
		
        // DomainName
		protected string _domainName = String.Empty;
		public string DomainName
		{
			get { return _domainName; }
			set { _domainName = value; }
		}
    }
    
    public abstract class Clause : Statement{}
    
    public class ColumnExpressions : Expressions
    {
        public ColumnExpressions() : base(){}
        
        public new ColumnExpression this[int index]
        {
            get { return (ColumnExpression)(base[index]); }
            set { base[index] = value; }
        }
        
        public ColumnExpression this[string columnAlias]
        {
			get { return this[IndexOf(columnAlias)]; }
			set { base[IndexOf(columnAlias)] = value; }
		}
		
		public int IndexOf(string columnAlias)
		{
			for (int index = 0; index < Count; index++)
				if (String.Compare(this[index].ColumnAlias, columnAlias) == 0)
					return index;
			return -1;
		}
		
		public bool Contains(string columnAlias)
		{
			return IndexOf(columnAlias) >= 0;
		}
    }
    
    public class SelectClause : Clause
    {
        public SelectClause() : base()
        {
            _columns = new ColumnExpressions();
        }

        // Distinct        
        protected bool _distinct;
        public bool Distinct
        {
            get { return _distinct; }
            set { _distinct = value; }
        }
        
        // NonProject        
        protected bool _nonProject;
        public bool NonProject
        {
            get { return _nonProject; }
            set { _nonProject = value; }
        }
        
        // Columns        
        protected ColumnExpressions _columns;
        public ColumnExpressions Columns { get { return _columns; } }
    }
    
    public class TableExpression : Expression
    {
		public TableExpression() : base(){}
		public TableExpression(string tableName) : base()
		{
			TableName = tableName;
		}

		public TableExpression(string tableSchema, string tableName) : base()
		{
			TableSchema = tableSchema;
			TableName = tableName;
		}

		// TableSchema
		protected string _tableSchema = String.Empty;
		public virtual string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}

		// TableName
        protected string _tableName = String.Empty;
        public virtual string TableName
        {
            get { return _tableName; }
            set { _tableName = value == null ? String.Empty : value; }
        }
    }
    
    public class JoinClause : Clause
    {
        public JoinClause() : base()
        {
            _joinType = JoinType.Inner;
        }
        
        public JoinClause(AlgebraicFromClause fromClause, JoinType joinType, Expression joinExpression)
        {
			FromClause = fromClause;
			JoinType = joinType;
			JoinExpression = joinExpression;
        }

        // FromClause
        protected AlgebraicFromClause _fromClause;
        public AlgebraicFromClause FromClause
        {
            get { return _fromClause; }
            set { _fromClause = value; }
        }
 
        // JoinType
        protected JoinType _joinType;
        public JoinType JoinType
        {
            get { return _joinType; }
            set { _joinType = value; }
        }
        
        // JoinExpression
        protected Expression _joinExpression;
        public Expression JoinExpression
        {
            get { return _joinExpression; }
            set { _joinExpression = value; }
        }
    }
    
    public class JoinClauses : Statements
    {
        public JoinClauses() : base(){}
        
        public new JoinClause this[int index]
        {
            get { return (JoinClause)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public abstract class FromClause : Clause 
    {
		public abstract bool HasJoins();
    }
    
    public class TableSpecifier : Expression
    {
		public TableSpecifier() : base(){}
		public TableSpecifier(Expression expression)
		{
			_tableExpression = expression;
		}
		
		public TableSpecifier(Expression expression, string alias)
		{
			_tableExpression = expression;
			_tableAlias = alias;
		}
		
        // TableExpression
        protected Expression _tableExpression;
        public Expression TableExpression
        {
            get { return _tableExpression; }
            set { _tableExpression = value; }
        }
        
        // TableAlias
        protected string _tableAlias = String.Empty;
        public string TableAlias
        {
            get { return _tableAlias; }
            set { _tableAlias = value; }
        }
    }
    
    public class TableSpecifiers : Expressions
    {
        public TableSpecifiers() : base(){}
        
        public new TableSpecifier this[int index]
        {
            get { return (TableSpecifier)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class CalculusFromClause : FromClause
    {
		public CalculusFromClause() : base(){}
		public CalculusFromClause(TableSpecifier tableSpecifier) : base()
		{
			_tableSpecifiers.Add(tableSpecifier);
		}
		
		public CalculusFromClause(TableSpecifier[] tableSpecifiers) : base()
		{
			_tableSpecifiers.AddRange(tableSpecifiers);
		}
		
		protected TableSpecifiers _tableSpecifiers = new TableSpecifiers();
		public TableSpecifiers TableSpecifiers { get { return _tableSpecifiers; } }
		
		public override bool HasJoins()
		{
			return _tableSpecifiers.Count > 1;
		}
    }
    
    public class AlgebraicFromClause : FromClause
    {    
        // constructor
        public AlgebraicFromClause() : base(){}
        public AlgebraicFromClause(TableSpecifier specifier) : base()
        {
			_tableSpecifier = specifier;
        }
        
        // TableSpecifier
        protected TableSpecifier _tableSpecifier;
        public TableSpecifier TableSpecifier
        {
			get { return _tableSpecifier; }
			set { _tableSpecifier = value; }
		}

		// ParentJoin        
        protected internal JoinClause _parentJoin;
        public JoinClause ParentJoin { get { return _parentJoin; } }

        // Joins
        protected JoinClauses _joins = new JoinClauses();
        public JoinClauses Joins { get { return _joins; } }
        
		public override bool HasJoins()
		{
			return _joins.Count > 0;
		}
        
        // FindTableAlias
        public string FindTableAlias(string tableName)
        {
            string result = string.Empty;
            if 
                (
                    (_tableSpecifier.TableExpression is TableExpression) && 
                    (String.Compare(((TableExpression)_tableSpecifier.TableExpression).TableName, tableName, true) == 0)
                )
            {
                result = _tableSpecifier.TableAlias;
            }
            else
                foreach (JoinClause join in _joins)
                {
                    result = ((AlgebraicFromClause)join.FromClause).FindTableAlias(tableName);
                    if (result != string.Empty)
                        break;
                }
            return result;
        }
    }
    
    public class FilterClause : Clause
    {
		public FilterClause() : base(){}
		public FilterClause(Expression expression) : base()
		{
			Expression = expression;
		}
		
        // Expression;
        protected Expression _expression;
        public Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }
    }
    
    public class WhereClause : FilterClause
    {
		public WhereClause() : base(){}
		public WhereClause(Expression expression) : base(expression){}
    }
    
    public class HavingClause : FilterClause
    {
		public HavingClause() : base(){}
		public HavingClause(Expression expression) : base(expression){}
    }
    
    public class GroupClause : Clause
    {
        public GroupClause() : base()
        {
            _columns = new Expressions();
        }
        
        // Columns
        protected Expressions _columns;
        public Expressions Columns { get { return _columns; } }
    }
    
    public class OrderFieldExpressions : Expressions
    {
        public OrderFieldExpressions() : base(){}
        
        public new OrderFieldExpression this[int index]
        {
            get { return (OrderFieldExpression)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class OrderClause : Clause
    {
		public OrderClause() : base()
        {
            _columns = new OrderFieldExpressions();
        }

        // Columns        
        protected OrderFieldExpressions _columns;
        public OrderFieldExpressions Columns { get { return _columns; } }
    }
    
    public class SelectExpression : Expression
    {        
        // SelectClause
        protected SelectClause _selectClause;
        public SelectClause SelectClause
        {
            get { return _selectClause; }
            set { _selectClause = value; }
        }
        
        // FromClause
        protected FromClause _fromClause;
        public FromClause FromClause
        {
            get { return _fromClause; }
            set { _fromClause = value; }
        }
        
        // WhereClause
        protected WhereClause _whereClause;
        public WhereClause WhereClause
        {
            get { return _whereClause; }
            set { _whereClause = value; }
        }
        
        // GroupClause
        protected GroupClause _groupClause;
        public GroupClause GroupClause
        {
            get { return _groupClause; }
            set { _groupClause = value; }
        }
        
        // HavingClause
        protected HavingClause _havingClause;
        public HavingClause HavingClause
        {
            get { return _havingClause; }
            set { _havingClause = value; }
        }
    }
    
    public enum TableOperator { Union, Intersect, Difference }
    
    public class TableOperatorExpression : Expression
    {
		public TableOperatorExpression() : base(){}
		public TableOperatorExpression(TableOperator operatorValue, SelectExpression selectExpression) : base()
		{
			_tableOperator = operatorValue;
			_selectExpression = selectExpression;
		}
		
		public TableOperatorExpression(TableOperator operatorValue, bool distinct, SelectExpression selectExpression) : base()
		{
			_tableOperator = operatorValue;
			_distinct = distinct;
			_selectExpression = selectExpression;
		}
		
		// TableOperator
		protected TableOperator _tableOperator;
		public TableOperator TableOperator
		{
			get { return _tableOperator; }
			set { _tableOperator = value; }
		}

        // Distinct
        protected bool _distinct = true;
        public bool Distinct
        {
            get { return _distinct; }
            set { _distinct = value; }
        }

        // SelectExpression
        protected SelectExpression _selectExpression;
        public SelectExpression SelectExpression
        {
            get { return _selectExpression; }
            set { _selectExpression = value; }
        }
    }
    
    public class TableOperatorExpressions : Expressions
    {
        public TableOperatorExpressions() : base(){}
        
        public new TableOperatorExpression this[int index]
        {
            get { return (TableOperatorExpression)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class QueryExpression : Expression
    {        
        public QueryExpression() : base()
        {
            _tableOperators = new TableOperatorExpressions();
        }
        
        // SelectExpression
        protected SelectExpression _selectExpression;
        public SelectExpression SelectExpression
        {
            get { return _selectExpression; }
            set { _selectExpression = value; }
        }
        
        // TableOperators
        protected TableOperatorExpressions _tableOperators;
        public TableOperatorExpressions TableOperators { get { return _tableOperators; } }

		// Indicates whether the given query expression could safely be extended with another table operator expression of the given table operator        
        public bool IsCompatibleWith(TableOperator tableOperator)
        {
			foreach (TableOperatorExpression tableOperatorExpression in _tableOperators)
				if (tableOperatorExpression.TableOperator != tableOperator)
					return false;
			return true;
        }
    }
    
    public class SelectStatement : Statement
    {        
        // QueryExpression
        protected QueryExpression _queryExpression;
        public QueryExpression QueryExpression
        {
            get { return _queryExpression; }
            set { _queryExpression = value; }
        }

        // OrderClause
        protected OrderClause _orderClause;
        public OrderClause OrderClause
        {
            get { return _orderClause; }
            set { _orderClause = value; }
        }
    }
    
    public class InsertFieldExpressions : Expressions
    {
        public InsertFieldExpressions() : base(){}
        
        public new InsertFieldExpression this[int index]
        {
            get { return (InsertFieldExpression)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class InsertClause : Clause
    {        
        public InsertClause() : base()
        {
            _columns = new InsertFieldExpressions();
        }
        
        // Columns
        protected InsertFieldExpressions _columns;
        public InsertFieldExpressions Columns { get { return _columns; } }

        // TableExpression
        protected TableExpression _tableExpression;
        public TableExpression TableExpression
        {
            get { return _tableExpression; }
            set { _tableExpression = value; }
        }
    }
    
    public class ValuesExpression : Expression
    {
        public ValuesExpression() : base()
        {
            _expressions = new Expressions();
        }
 
        // Expressions
        protected Expressions _expressions;
        public Expressions Expressions { get { return _expressions; } }
    }
    
    public class InsertStatement : Statement
    {        
        // InsertClause
        protected InsertClause _insertClause;
        public InsertClause InsertClause
        {
            get { return _insertClause; }
            set { _insertClause = value; }
        }

        // Values
        protected Expression _values;
        public Expression Values
        {
            get { return _values; }
            set { _values = value; }
        }
    }

    public class UpdateFieldExpressions : Expressions
    {
        public UpdateFieldExpressions() : base(){}
        
        public new UpdateFieldExpression this[int index]
        {
            get { return (UpdateFieldExpression)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class UpdateClause : Clause
    {        
        public UpdateClause() : base()
        {
            _columns = new UpdateFieldExpressions();
        }
        
        // Columns
        protected UpdateFieldExpressions _columns;
        public UpdateFieldExpressions Columns { get { return _columns; } }
        
        // TableAlias
        protected internal string _tableAlias = String.Empty;
        public string TableAlias { get { return _tableAlias; } }

        // TableExpression
        protected TableExpression _tableExpression;
        public TableExpression TableExpression
        {
            get { return _tableExpression; }
            set { _tableExpression = value; }
        }
    }
    
    public class UpdateStatement : Statement
    {
        // UpdateClause
        protected UpdateClause _updateClause;
        public UpdateClause UpdateClause
        {
            get { return _updateClause; }
            set { _updateClause = value; }
        }
        
        // FromClause
        protected FromClause _fromClause;
        public FromClause FromClause
        {
            get { return _fromClause; }
            set { _fromClause = value; }
        }
        
        // WhereClause
        protected WhereClause _whereClause;
        public WhereClause WhereClause
        {
            get { return _whereClause; }
            set { _whereClause = value; }
        }
    }
    
    public class DeleteClause : Clause
    {        
        // TableAlias
        protected internal string _tableAlias = String.Empty;
        public string TableAlias { get { return _tableAlias; } }

        // TableExpression
        protected TableExpression _tableExpression;
        public TableExpression TableExpression
        {
            get { return _tableExpression; }
            set { _tableExpression = value; }
        }
    }

    public class DeleteStatement : Statement
    {
        // DeleteClause
        protected DeleteClause _deleteClause;
        public DeleteClause DeleteClause
        {
            get { return _deleteClause; }
            set { _deleteClause = value; }
        }
        
        // FromClause
        protected FromClause _fromClause;
        public FromClause FromClause
        {
            get { return _fromClause; }
            set { _fromClause = value; }
        }
        
        // WhereClause
        protected WhereClause _whereClause;
        public WhereClause WhereClause
        {
            get { return _whereClause; }
            set { _whereClause = value; }
        }
    }
    
    public class ConstraintValueExpression : Expression{}
    
    public class ConstraintRecordValueExpression : Expression
    {
        // ColumnName
        protected string _columnName = String.Empty;
        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        }
    }
    
    public class CreateTableStatement : Statement
    {
		// TableSchema
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}
		
		// TableName
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
		
		// Columns
		protected ColumnDefinitions _columns = new ColumnDefinitions();
		public ColumnDefinitions Columns { get { return _columns; } }
    }
    
    public class AlterTableStatement : Statement
    {
		// TableSchema
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}
		
		// TableName
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
		
		// AddColumns
		protected ColumnDefinitions _addColumns = new ColumnDefinitions();
		public ColumnDefinitions AddColumns { get { return _addColumns; } }
		
		// AlterColumns
		protected AlterColumnDefinitions _alterColumns = new AlterColumnDefinitions();
		public AlterColumnDefinitions AlterColumns { get { return _alterColumns; } }
		
		// DropColumns
		protected DropColumnDefinitions _dropColumns = new DropColumnDefinitions();
		public DropColumnDefinitions DropColumns { get { return _dropColumns; } }
    }
    
    public class DropTableStatement : Statement
    {
		// TableSchema
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}
		
		// TableName
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
    }
    
    public class CreateIndexStatement : Statement
    {
		// IndexSchema
		protected string _indexSchema = String.Empty;
		public string IndexSchema
		{
			get { return _indexSchema; }
			set { _indexSchema = value == null ? String.Empty : value; }
		}
		
		// IndexName
		protected string _indexName = String.Empty;
		public string IndexName
		{
			get { return _indexName; }
			set { _indexName = value == null ? String.Empty : value; }
		}
		
		// IsUnique
		protected bool _isUnique;
		public bool IsUnique
		{
			get { return _isUnique; }
			set { _isUnique = value; }
		}
		
		// IsUnique
		protected bool _isClustered;
		public bool IsClustered
		{
			get { return _isClustered; }
			set { _isClustered = value; }
		}
		
		// TableSchema
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}

		// TableName
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}

		// Columns
		protected OrderColumnDefinitions _columns = new OrderColumnDefinitions();
		public OrderColumnDefinitions Columns { get { return _columns; } }
    }
    
    public class DropIndexStatement : Statement
    {
		// IndexSchema
		protected string _indexSchema = String.Empty;
		public string IndexSchema
		{
			get { return _indexSchema; }
			set { _indexSchema = value == null ? String.Empty : value; }
		}
		
		// IndexName
		protected string _indexName = String.Empty;
		public string IndexName
		{
			get { return _indexName; }
			set { _indexName = value == null ? String.Empty : value; }
		}
    }
    
    public class ColumnDefinition : Statement
    {
		public ColumnDefinition() : base(){}
		public ColumnDefinition(string columnName, string domainName) : base()
		{
			ColumnName = columnName;
			DomainName = domainName;
		}
		
		public ColumnDefinition(string columnName, string domainName, bool isNullable) : base()
		{
			ColumnName = columnName;
			DomainName = domainName;
			_isNullable = isNullable;
		}
		
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		// DomainName
		protected string _domainName = String.Empty;
		public string DomainName
		{
			get { return _domainName; }
			set { _domainName = value == null ? String.Empty : value; }
		}

		// IsNullable
		protected bool _isNullable = false;
		public bool IsNullable
		{
			get { return _isNullable; }
			set { _isNullable = value; }
		}
    }

    public class ColumnDefinitions : Statements
    {
        public ColumnDefinitions() : base(){}
        
        public new ColumnDefinition this[int index]
        {
            get { return (ColumnDefinition)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class AlterColumnDefinition : Statement
    {
		public AlterColumnDefinition() : base() {}
		public AlterColumnDefinition(string columnName) : base()
		{
			_columnName = columnName;
		}
		
		public AlterColumnDefinition(string columnName, bool isNullable)
		{
			_columnName = columnName;
			_alterNullable = true;
			_isNullable = isNullable;
		}

		public AlterColumnDefinition(string columnName, string domainName)
		{
			_columnName = columnName;
			_domainName = domainName;
		}
		
		public AlterColumnDefinition(string columnName, string domainName, bool isNullable)
		{
			_columnName = columnName;
			_domainName = domainName;
			_alterNullable = true;
			_isNullable = isNullable;
		}

		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		// DomainName
		/// <summary>Null domain name indicates no change to the domain of the column</summary>
		protected string _domainName = null;
		public string DomainName
		{
			get { return _domainName; }
			set { _domainName = value; }
		}
		
		// AlterNullable
		protected bool _alterNullable = false;
		public bool AlterNullable
		{
			get { return _alterNullable; }
			set { _alterNullable = value; }
		}

		// IsNullable
		protected bool _isNullable = false;
		public bool IsNullable
		{
			get { return _isNullable; }
			set { _isNullable = value; }
		}
    }
    
    public class AlterColumnDefinitions : Statements
    {
        public AlterColumnDefinitions() : base(){}
        
        public new AlterColumnDefinition this[int index]
        {
            get { return (AlterColumnDefinition)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class DropColumnDefinition : Statement
    {
		public DropColumnDefinition() : base(){}
		public DropColumnDefinition(string columnName) : base()
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

    public class DropColumnDefinitions : Statements
    {
        public DropColumnDefinitions() : base(){}
        
        public new DropColumnDefinition this[int index]
        {
            get { return (DropColumnDefinition)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class OrderColumnDefinition : Statement
    {
		// ColumnName
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		// Ascending
		protected bool _ascending;
		public bool Ascending
		{
			get { return _ascending; }
			set { _ascending = value; }
		}
    }

    public class OrderColumnDefinitions : Statements
    {
        public OrderColumnDefinitions() : base(){}
        
        public new OrderColumnDefinition this[int index]
        {
            get { return (OrderColumnDefinition)(base[index]); }
            set { base[index] = value; }
        }
    }
    
    public class Batch : Statement
    {
		private Statements _statements = new Statements();
		public Statements Statements { get { return _statements; } }
    }
}

