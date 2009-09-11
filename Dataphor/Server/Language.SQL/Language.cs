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
		
		protected bool FIsDistinct;
		public bool IsDistinct
		{
			get { return FIsDistinct; }
			set { FIsDistinct = value; }
		}
		
		protected bool FIsRowLevel;
		public bool IsRowLevel
		{
			get { return FIsRowLevel; }
			set { FIsRowLevel = value; }
		}
    }
    
    public class UserExpression : Expression
    {
		public UserExpression() : base() {}
		public UserExpression(string ATranslationString, Expression[] AArguments) : base()
		{
			FTranslationString = ATranslationString;
			FExpressions.AddRange(AArguments);
		}
		
		private string FTranslationString = String.Empty;
		public string TranslationString
		{
			get { return FTranslationString; }
			set { FTranslationString = value == null ? String.Empty : value; }
		}
        
        // Expressions
        protected Expressions FExpressions = new Expressions();
        public Expressions Expressions { get { return FExpressions; } }
	}
    
    public class QueryParameterExpression : Expression
    {
		public QueryParameterExpression() : base(){}
		public QueryParameterExpression(string AParameterName) : base()
		{
			ParameterName = AParameterName;
		}
		
        // ParameterName
        protected string FParameterName = String.Empty;
        public string ParameterName
        {
            get { return FParameterName; }
            set { FParameterName = value; }
        }
    }
    
    public abstract class FieldExpression : Expression
    {
		public FieldExpression() : base(){}
		public FieldExpression(string AFieldName) : base()
		{
			FieldName = AFieldName;
		}
		
        // FieldName
        protected string FFieldName = String.Empty;
        public string FieldName
        {
            get { return FFieldName; }
            set { FFieldName = value; }
        }
    }
    
    public class InsertFieldExpression : FieldExpression
    {	
		public InsertFieldExpression(string AFieldName) : base(AFieldName){}
    }
    
    public class UpdateFieldExpression : FieldExpression
    {
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
    }
    
    public class OrderFieldExpression : QualifiedFieldExpression
    {
        public OrderFieldExpression() : base()
        {
            FAscending = true;
        }
        
        public OrderFieldExpression(string AFieldName, string ATableAlias, bool AAscending) : base(AFieldName, ATableAlias)
        {
			FAscending = AAscending;
        }
        
        // Ascending
        protected bool FAscending;
        public bool Ascending
        {
            get { return FAscending; }
            set { FAscending = value; }
        }
    }

    public class QualifiedFieldExpression : FieldExpression
    {
		public QualifiedFieldExpression() : base(){}
		public QualifiedFieldExpression(string AFieldName) : base(AFieldName){}
		public QualifiedFieldExpression(string AFieldName, string ATableAlias) : base(AFieldName)
		{
			TableAlias = ATableAlias;
		}
		
        // TableAlias        
        protected string FTableAlias = String.Empty;
        public string TableAlias
        {
            get { return FTableAlias; }
            set { FTableAlias = value; }
        }
    }
    
    public class ColumnExpression : Expression
    {
		public ColumnExpression() : base(){}
		public ColumnExpression(Expression AExpression) : base()
		{
			Expression = AExpression;
		}
		
		public ColumnExpression(Expression AExpression, string AColumnAlias) : base()
		{
			Expression = AExpression;
			ColumnAlias = AColumnAlias;
		}

        // ColumnAlias
        protected string FColumnAlias = String.Empty;
        public string ColumnAlias
        {
            get { return FColumnAlias; }
            set { FColumnAlias = value; }
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
    }
    
    public class CastExpression : Expression
    {
		public CastExpression() : base(){}
		public CastExpression(Expression AExpression, string ADomainName) : base()
		{
			FExpression = AExpression;
			FDomainName = ADomainName;
		}

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
		
        // DomainName
		protected string FDomainName = String.Empty;
		public string DomainName
		{
			get { return FDomainName; }
			set { FDomainName = value; }
		}
    }
    
    public abstract class Clause : Statement{}
    
    public class ColumnExpressions : Expressions
    {
        public ColumnExpressions() : base(){}
        
        public new ColumnExpression this[int AIndex]
        {
            get { return (ColumnExpression)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
        
        public ColumnExpression this[string AColumnAlias]
        {
			get { return this[IndexOf(AColumnAlias)]; }
			set { base[IndexOf(AColumnAlias)] = value; }
		}
		
		public int IndexOf(string AColumnAlias)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Compare(this[LIndex].ColumnAlias, AColumnAlias) == 0)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AColumnAlias)
		{
			return IndexOf(AColumnAlias) >= 0;
		}
    }
    
    public class SelectClause : Clause
    {
        public SelectClause() : base()
        {
            FColumns = new ColumnExpressions();
        }

        // Distinct        
        protected bool FDistinct;
        public bool Distinct
        {
            get { return FDistinct; }
            set { FDistinct = value; }
        }
        
        // NonProject        
        protected bool FNonProject;
        public bool NonProject
        {
            get { return FNonProject; }
            set { FNonProject = value; }
        }
        
        // Columns        
        protected ColumnExpressions FColumns;
        public ColumnExpressions Columns { get { return FColumns; } }
    }
    
    public class TableExpression : Expression
    {
		public TableExpression() : base(){}
		public TableExpression(string ATableName) : base()
		{
			TableName = ATableName;
		}

		public TableExpression(string ATableSchema, string ATableName) : base()
		{
			TableSchema = ATableSchema;
			TableName = ATableName;
		}

		// TableSchema
		protected string FTableSchema = String.Empty;
		public virtual string TableSchema
		{
			get { return FTableSchema; }
			set { FTableSchema = value == null ? String.Empty : value; }
		}

		// TableName
        protected string FTableName = String.Empty;
        public virtual string TableName
        {
            get { return FTableName; }
            set { FTableName = value == null ? String.Empty : value; }
        }
    }
    
    public class JoinClause : Clause
    {
        public JoinClause() : base()
        {
            FJoinType = JoinType.Inner;
        }
        
        public JoinClause(AlgebraicFromClause AFromClause, JoinType AJoinType, Expression AJoinExpression)
        {
			FromClause = AFromClause;
			JoinType = AJoinType;
			JoinExpression = AJoinExpression;
        }

        // FromClause
        protected AlgebraicFromClause FFromClause;
        public AlgebraicFromClause FromClause
        {
            get { return FFromClause; }
            set { FFromClause = value; }
        }
 
        // JoinType
        protected JoinType FJoinType;
        public JoinType JoinType
        {
            get { return FJoinType; }
            set { FJoinType = value; }
        }
        
        // JoinExpression
        protected Expression FJoinExpression;
        public Expression JoinExpression
        {
            get { return FJoinExpression; }
            set { FJoinExpression = value; }
        }
    }
    
    public class JoinClauses : Statements
    {
        public JoinClauses() : base(){}
        
        public new JoinClause this[int AIndex]
        {
            get { return (JoinClause)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public abstract class FromClause : Clause 
    {
		public abstract bool HasJoins();
    }
    
    public class TableSpecifier : Expression
    {
		public TableSpecifier() : base(){}
		public TableSpecifier(Expression AExpression)
		{
			FTableExpression = AExpression;
		}
		
		public TableSpecifier(Expression AExpression, string AAlias)
		{
			FTableExpression = AExpression;
			FTableAlias = AAlias;
		}
		
        // TableExpression
        protected Expression FTableExpression;
        public Expression TableExpression
        {
            get { return FTableExpression; }
            set { FTableExpression = value; }
        }
        
        // TableAlias
        protected string FTableAlias = String.Empty;
        public string TableAlias
        {
            get { return FTableAlias; }
            set { FTableAlias = value; }
        }
    }
    
    public class TableSpecifiers : Expressions
    {
        public TableSpecifiers() : base(){}
        
        public new TableSpecifier this[int AIndex]
        {
            get { return (TableSpecifier)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class CalculusFromClause : FromClause
    {
		public CalculusFromClause() : base(){}
		public CalculusFromClause(TableSpecifier ATableSpecifier) : base()
		{
			FTableSpecifiers.Add(ATableSpecifier);
		}
		
		public CalculusFromClause(TableSpecifier[] ATableSpecifiers) : base()
		{
			FTableSpecifiers.AddRange(ATableSpecifiers);
		}
		
		protected TableSpecifiers FTableSpecifiers = new TableSpecifiers();
		public TableSpecifiers TableSpecifiers { get { return FTableSpecifiers; } }
		
		public override bool HasJoins()
		{
			return FTableSpecifiers.Count > 1;
		}
    }
    
    public class AlgebraicFromClause : FromClause
    {    
        // constructor
        public AlgebraicFromClause() : base(){}
        public AlgebraicFromClause(TableSpecifier ASpecifier) : base()
        {
			FTableSpecifier = ASpecifier;
        }
        
        // TableSpecifier
        protected TableSpecifier FTableSpecifier;
        public TableSpecifier TableSpecifier
        {
			get { return FTableSpecifier; }
			set { FTableSpecifier = value; }
		}

		// ParentJoin        
        protected internal JoinClause FParentJoin;
        public JoinClause ParentJoin { get { return FParentJoin; } }

        // Joins
        protected JoinClauses FJoins = new JoinClauses();
        public JoinClauses Joins { get { return FJoins; } }
        
		public override bool HasJoins()
		{
			return FJoins.Count > 0;
		}
        
        // FindTableAlias
        public string FindTableAlias(string ATableName)
        {
            string LResult = string.Empty;
            if 
                (
                    (FTableSpecifier.TableExpression is TableExpression) && 
                    (String.Compare(((TableExpression)FTableSpecifier.TableExpression).TableName, ATableName, true) == 0)
                )
            {
                LResult = FTableSpecifier.TableAlias;
            }
            else
                foreach (JoinClause LJoin in FJoins)
                {
                    LResult = ((AlgebraicFromClause)LJoin.FromClause).FindTableAlias(ATableName);
                    if (LResult != string.Empty)
                        break;
                }
            return LResult;
        }
    }
    
    public class FilterClause : Clause
    {
		public FilterClause() : base(){}
		public FilterClause(Expression AExpression) : base()
		{
			Expression = AExpression;
		}
		
        // Expression;
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
    }
    
    public class WhereClause : FilterClause
    {
		public WhereClause() : base(){}
		public WhereClause(Expression AExpression) : base(AExpression){}
    }
    
    public class HavingClause : FilterClause
    {
		public HavingClause() : base(){}
		public HavingClause(Expression AExpression) : base(AExpression){}
    }
    
    public class GroupClause : Clause
    {
        public GroupClause() : base()
        {
            FColumns = new Expressions();
        }
        
        // Columns
        protected Expressions FColumns;
        public Expressions Columns { get { return FColumns; } }
    }
    
    public class OrderFieldExpressions : Expressions
    {
        public OrderFieldExpressions() : base(){}
        
        public new OrderFieldExpression this[int AIndex]
        {
            get { return (OrderFieldExpression)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class OrderClause : Clause
    {
		public OrderClause() : base()
        {
            FColumns = new OrderFieldExpressions();
        }

        // Columns        
        protected OrderFieldExpressions FColumns;
        public OrderFieldExpressions Columns { get { return FColumns; } }
    }
    
    public class SelectExpression : Expression
    {        
        // SelectClause
        protected SelectClause FSelectClause;
        public SelectClause SelectClause
        {
            get { return FSelectClause; }
            set { FSelectClause = value; }
        }
        
        // FromClause
        protected FromClause FFromClause;
        public FromClause FromClause
        {
            get { return FFromClause; }
            set { FFromClause = value; }
        }
        
        // WhereClause
        protected WhereClause FWhereClause;
        public WhereClause WhereClause
        {
            get { return FWhereClause; }
            set { FWhereClause = value; }
        }
        
        // GroupClause
        protected GroupClause FGroupClause;
        public GroupClause GroupClause
        {
            get { return FGroupClause; }
            set { FGroupClause = value; }
        }
        
        // HavingClause
        protected HavingClause FHavingClause;
        public HavingClause HavingClause
        {
            get { return FHavingClause; }
            set { FHavingClause = value; }
        }
    }
    
    public enum TableOperator { Union, Intersect, Difference }
    
    public class TableOperatorExpression : Expression
    {
		public TableOperatorExpression() : base(){}
		public TableOperatorExpression(TableOperator AOperator, SelectExpression ASelectExpression) : base()
		{
			FTableOperator = AOperator;
			FSelectExpression = ASelectExpression;
		}
		
		public TableOperatorExpression(TableOperator AOperator, bool ADistinct, SelectExpression ASelectExpression) : base()
		{
			FTableOperator = AOperator;
			FDistinct = ADistinct;
			FSelectExpression = ASelectExpression;
		}
		
		// TableOperator
		protected TableOperator FTableOperator;
		public TableOperator TableOperator
		{
			get { return FTableOperator; }
			set { FTableOperator = value; }
		}

        // Distinct
        protected bool FDistinct = true;
        public bool Distinct
        {
            get { return FDistinct; }
            set { FDistinct = value; }
        }

        // SelectExpression
        protected SelectExpression FSelectExpression;
        public SelectExpression SelectExpression
        {
            get { return FSelectExpression; }
            set { FSelectExpression = value; }
        }
    }
    
    public class TableOperatorExpressions : Expressions
    {
        public TableOperatorExpressions() : base(){}
        
        public new TableOperatorExpression this[int AIndex]
        {
            get { return (TableOperatorExpression)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class QueryExpression : Expression
    {        
        public QueryExpression() : base()
        {
            FTableOperators = new TableOperatorExpressions();
        }
        
        // SelectExpression
        protected SelectExpression FSelectExpression;
        public SelectExpression SelectExpression
        {
            get { return FSelectExpression; }
            set { FSelectExpression = value; }
        }
        
        // TableOperators
        protected TableOperatorExpressions FTableOperators;
        public TableOperatorExpressions TableOperators { get { return FTableOperators; } }

		// Indicates whether the given query expression could safely be extended with another table operator expression of the given table operator        
        public bool IsCompatibleWith(TableOperator ATableOperator)
        {
			foreach (TableOperatorExpression LTableOperatorExpression in FTableOperators)
				if (LTableOperatorExpression.TableOperator != ATableOperator)
					return false;
			return true;
        }
    }
    
    public class SelectStatement : Statement
    {        
        // QueryExpression
        protected QueryExpression FQueryExpression;
        public QueryExpression QueryExpression
        {
            get { return FQueryExpression; }
            set { FQueryExpression = value; }
        }

        // OrderClause
        protected OrderClause FOrderClause;
        public OrderClause OrderClause
        {
            get { return FOrderClause; }
            set { FOrderClause = value; }
        }
    }
    
    public class InsertFieldExpressions : Expressions
    {
        public InsertFieldExpressions() : base(){}
        
        public new InsertFieldExpression this[int AIndex]
        {
            get { return (InsertFieldExpression)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class InsertClause : Clause
    {        
        public InsertClause() : base()
        {
            FColumns = new InsertFieldExpressions();
        }
        
        // Columns
        protected InsertFieldExpressions FColumns;
        public InsertFieldExpressions Columns { get { return FColumns; } }

        // TableExpression
        protected TableExpression FTableExpression;
        public TableExpression TableExpression
        {
            get { return FTableExpression; }
            set { FTableExpression = value; }
        }
    }
    
    public class ValuesExpression : Expression
    {
        public ValuesExpression() : base()
        {
            FExpressions = new Expressions();
        }
 
        // Expressions
        protected Expressions FExpressions;
        public Expressions Expressions { get { return FExpressions; } }
    }
    
    public class InsertStatement : Statement
    {        
        // InsertClause
        protected InsertClause FInsertClause;
        public InsertClause InsertClause
        {
            get { return FInsertClause; }
            set { FInsertClause = value; }
        }

        // Values
        protected Expression FValues;
        public Expression Values
        {
            get { return FValues; }
            set { FValues = value; }
        }
    }

    public class UpdateFieldExpressions : Expressions
    {
        public UpdateFieldExpressions() : base(){}
        
        public new UpdateFieldExpression this[int AIndex]
        {
            get { return (UpdateFieldExpression)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class UpdateClause : Clause
    {        
        public UpdateClause() : base()
        {
            FColumns = new UpdateFieldExpressions();
        }
        
        // Columns
        protected UpdateFieldExpressions FColumns;
        public UpdateFieldExpressions Columns { get { return FColumns; } }
        
        // TableAlias
        protected internal string FTableAlias = String.Empty;
        public string TableAlias { get { return FTableAlias; } }

        // TableExpression
        protected TableExpression FTableExpression;
        public TableExpression TableExpression
        {
            get { return FTableExpression; }
            set { FTableExpression = value; }
        }
    }
    
    public class UpdateStatement : Statement
    {
        // UpdateClause
        protected UpdateClause FUpdateClause;
        public UpdateClause UpdateClause
        {
            get { return FUpdateClause; }
            set { FUpdateClause = value; }
        }
        
        // FromClause
        protected FromClause FFromClause;
        public FromClause FromClause
        {
            get { return FFromClause; }
            set { FFromClause = value; }
        }
        
        // WhereClause
        protected WhereClause FWhereClause;
        public WhereClause WhereClause
        {
            get { return FWhereClause; }
            set { FWhereClause = value; }
        }
    }
    
    public class DeleteClause : Clause
    {        
        // TableAlias
        protected internal string FTableAlias = String.Empty;
        public string TableAlias { get { return FTableAlias; } }

        // TableExpression
        protected TableExpression FTableExpression;
        public TableExpression TableExpression
        {
            get { return FTableExpression; }
            set { FTableExpression = value; }
        }
    }

    public class DeleteStatement : Statement
    {
        // DeleteClause
        protected DeleteClause FDeleteClause;
        public DeleteClause DeleteClause
        {
            get { return FDeleteClause; }
            set { FDeleteClause = value; }
        }
        
        // FromClause
        protected FromClause FFromClause;
        public FromClause FromClause
        {
            get { return FFromClause; }
            set { FFromClause = value; }
        }
        
        // WhereClause
        protected WhereClause FWhereClause;
        public WhereClause WhereClause
        {
            get { return FWhereClause; }
            set { FWhereClause = value; }
        }
    }
    
    public class ConstraintValueExpression : Expression{}
    
    public class ConstraintRecordValueExpression : Expression
    {
        // ColumnName
        protected string FColumnName = String.Empty;
        public string ColumnName
        {
            get { return FColumnName; }
            set { FColumnName = value; }
        }
    }
    
    public class CreateTableStatement : Statement
    {
		// TableSchema
		protected string FTableSchema = String.Empty;
		public string TableSchema
		{
			get { return FTableSchema; }
			set { FTableSchema = value == null ? String.Empty : value; }
		}
		
		// TableName
		protected string FTableName = String.Empty;
		public string TableName
		{
			get { return FTableName; }
			set { FTableName = value == null ? String.Empty : value; }
		}
		
		// Columns
		protected ColumnDefinitions FColumns = new ColumnDefinitions();
		public ColumnDefinitions Columns { get { return FColumns; } }
    }
    
    public class AlterTableStatement : Statement
    {
		// TableSchema
		protected string FTableSchema = String.Empty;
		public string TableSchema
		{
			get { return FTableSchema; }
			set { FTableSchema = value == null ? String.Empty : value; }
		}
		
		// TableName
		protected string FTableName = String.Empty;
		public string TableName
		{
			get { return FTableName; }
			set { FTableName = value == null ? String.Empty : value; }
		}
		
		// AddColumns
		protected ColumnDefinitions FAddColumns = new ColumnDefinitions();
		public ColumnDefinitions AddColumns { get { return FAddColumns; } }
		
		// AlterColumns
		protected AlterColumnDefinitions FAlterColumns = new AlterColumnDefinitions();
		public AlterColumnDefinitions AlterColumns { get { return FAlterColumns; } }
		
		// DropColumns
		protected DropColumnDefinitions FDropColumns = new DropColumnDefinitions();
		public DropColumnDefinitions DropColumns { get { return FDropColumns; } }
    }
    
    public class DropTableStatement : Statement
    {
		// TableSchema
		protected string FTableSchema = String.Empty;
		public string TableSchema
		{
			get { return FTableSchema; }
			set { FTableSchema = value == null ? String.Empty : value; }
		}
		
		// TableName
		protected string FTableName = String.Empty;
		public string TableName
		{
			get { return FTableName; }
			set { FTableName = value == null ? String.Empty : value; }
		}
    }
    
    public class CreateIndexStatement : Statement
    {
		// IndexSchema
		protected string FIndexSchema = String.Empty;
		public string IndexSchema
		{
			get { return FIndexSchema; }
			set { FIndexSchema = value == null ? String.Empty : value; }
		}
		
		// IndexName
		protected string FIndexName = String.Empty;
		public string IndexName
		{
			get { return FIndexName; }
			set { FIndexName = value == null ? String.Empty : value; }
		}
		
		// IsUnique
		protected bool FIsUnique;
		public bool IsUnique
		{
			get { return FIsUnique; }
			set { FIsUnique = value; }
		}
		
		// IsUnique
		protected bool FIsClustered;
		public bool IsClustered
		{
			get { return FIsClustered; }
			set { FIsClustered = value; }
		}
		
		// TableSchema
		protected string FTableSchema = String.Empty;
		public string TableSchema
		{
			get { return FTableSchema; }
			set { FTableSchema = value == null ? String.Empty : value; }
		}

		// TableName
		protected string FTableName = String.Empty;
		public string TableName
		{
			get { return FTableName; }
			set { FTableName = value == null ? String.Empty : value; }
		}

		// Columns
		protected OrderColumnDefinitions FColumns = new OrderColumnDefinitions();
		public OrderColumnDefinitions Columns { get { return FColumns; } }
    }
    
    public class DropIndexStatement : Statement
    {
		// IndexSchema
		protected string FIndexSchema = String.Empty;
		public string IndexSchema
		{
			get { return FIndexSchema; }
			set { FIndexSchema = value == null ? String.Empty : value; }
		}
		
		// IndexName
		protected string FIndexName = String.Empty;
		public string IndexName
		{
			get { return FIndexName; }
			set { FIndexName = value == null ? String.Empty : value; }
		}
    }
    
    public class ColumnDefinition : Statement
    {
		public ColumnDefinition() : base(){}
		public ColumnDefinition(string AColumnName, string ADomainName) : base()
		{
			ColumnName = AColumnName;
			DomainName = ADomainName;
		}
		
		public ColumnDefinition(string AColumnName, string ADomainName, bool AIsNullable) : base()
		{
			ColumnName = AColumnName;
			DomainName = ADomainName;
			FIsNullable = AIsNullable;
		}
		
		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}

		// DomainName
		protected string FDomainName = String.Empty;
		public string DomainName
		{
			get { return FDomainName; }
			set { FDomainName = value == null ? String.Empty : value; }
		}

		// IsNullable
		protected bool FIsNullable = false;
		public bool IsNullable
		{
			get { return FIsNullable; }
			set { FIsNullable = value; }
		}
    }

    public class ColumnDefinitions : Statements
    {
        public ColumnDefinitions() : base(){}
        
        public new ColumnDefinition this[int AIndex]
        {
            get { return (ColumnDefinition)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class AlterColumnDefinition : Statement
    {
		public AlterColumnDefinition() : base() {}
		public AlterColumnDefinition(string AColumnName) : base()
		{
			FColumnName = AColumnName;
		}
		
		public AlterColumnDefinition(string AColumnName, bool AIsNullable)
		{
			FColumnName = AColumnName;
			FAlterNullable = true;
			FIsNullable = AIsNullable;
		}

		public AlterColumnDefinition(string AColumnName, string ADomainName)
		{
			FColumnName = AColumnName;
			FDomainName = ADomainName;
		}
		
		public AlterColumnDefinition(string AColumnName, string ADomainName, bool AIsNullable)
		{
			FColumnName = AColumnName;
			FDomainName = ADomainName;
			FAlterNullable = true;
			FIsNullable = AIsNullable;
		}

		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}

		// DomainName
		/// <summary>Null domain name indicates no change to the domain of the column</summary>
		protected string FDomainName = null;
		public string DomainName
		{
			get { return FDomainName; }
			set { FDomainName = value; }
		}
		
		// AlterNullable
		protected bool FAlterNullable = false;
		public bool AlterNullable
		{
			get { return FAlterNullable; }
			set { FAlterNullable = value; }
		}

		// IsNullable
		protected bool FIsNullable = false;
		public bool IsNullable
		{
			get { return FIsNullable; }
			set { FIsNullable = value; }
		}
    }
    
    public class AlterColumnDefinitions : Statements
    {
        public AlterColumnDefinitions() : base(){}
        
        public new AlterColumnDefinition this[int AIndex]
        {
            get { return (AlterColumnDefinition)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class DropColumnDefinition : Statement
    {
		public DropColumnDefinition() : base(){}
		public DropColumnDefinition(string AColumnName) : base()
		{
			ColumnName = AColumnName;
		}
		
		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}
    }

    public class DropColumnDefinitions : Statements
    {
        public DropColumnDefinitions() : base(){}
        
        public new DropColumnDefinition this[int AIndex]
        {
            get { return (DropColumnDefinition)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class OrderColumnDefinition : Statement
    {
		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}

		// Ascending
		protected bool FAscending;
		public bool Ascending
		{
			get { return FAscending; }
			set { FAscending = value; }
		}
    }

    public class OrderColumnDefinitions : Statements
    {
        public OrderColumnDefinitions() : base(){}
        
        public new OrderColumnDefinition this[int AIndex]
        {
            get { return (OrderColumnDefinition)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
    }
    
    public class Batch : Statement
    {
		private Statements FStatements = new Statements();
		public Statements Statements { get { return FStatements; } }
    }
}

