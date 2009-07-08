/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.D4
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Specialized;
	using System.ComponentModel.Design.Serialization;

    using Alphora.Dataphor;
    using Alphora.Dataphor.DAE.Language;
	using System.ComponentModel;
    
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
		public TableIdentifierExpression(string AIdentifier) : base(AIdentifier){}
    }
    
    public class ColumnIdentifierExpression : IdentifierExpression
    {
		public ColumnIdentifierExpression() : base(){}
		public ColumnIdentifierExpression(string AIdentifier) : base(AIdentifier){}
    }
    
    public class ServerIdentifierExpression : IdentifierExpression
    {
		public ServerIdentifierExpression() : base(){}
		public ServerIdentifierExpression(string AIdentifier) : base(AIdentifier){}
    }
    
    public class VariableIdentifierExpression : IdentifierExpression
    {
		public VariableIdentifierExpression() : base(){}
		public VariableIdentifierExpression(string AIdentifier) : base(AIdentifier){}
    }
    
	public abstract class D4Statement : Statement{}

    public abstract class D4DMLStatement : D4Statement {}

	// Verified against DataphorMachine - BTR - 11/24/2001
    public class SelectStatement : D4DMLStatement
    {
		public SelectStatement() : base(){}
		public SelectStatement(CursorDefinition ACursorDefinition) : base()
		{
			FCursorDefinition = ACursorDefinition;
		}
		
        // CursorDefinition
        protected CursorDefinition FCursorDefinition;
        public CursorDefinition CursorDefinition
        {
            get { return FCursorDefinition; }
            set { FCursorDefinition = value; }
        }
    }
    
    public class InsertStatement : D4DMLStatement
    {
		public InsertStatement() : base(){}
		public InsertStatement(Expression ASourceExpression, Expression ATarget) : base()
		{
			FSourceExpression = ASourceExpression;
			FTarget = ATarget;
		}
		
        // SourceExpression
        protected Expression FSourceExpression;
        public Expression SourceExpression
        {
            get { return FSourceExpression; }
            set { FSourceExpression = value; }
        }
        
        // Target
        protected Expression FTarget;
        public Expression Target
        {
			get { return FTarget; }
			set { FTarget = value; }
        }
    }
    
    public class UpdateColumnExpression : Expression
    {
		public UpdateColumnExpression() : base(){}
		public UpdateColumnExpression(Expression ATarget, Expression AExpression) : base()
		{
			Target = ATarget;
			Expression = AExpression;
		}
		
        // Target
        protected Expression FTarget;
        public Expression Target
        {
            get { return FTarget; }
            set { FTarget = value; }
        }

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
    }
    
    public class UpdateColumnExpressions : Expressions
    {
		protected override void Validate(object AItem)
		{
			if (!(AItem is UpdateColumnExpression))
				throw new LanguageException(LanguageException.Codes.UpdateColumnExpressionContainer);
			base.Validate(AItem);
		}
		
		public new UpdateColumnExpression this[int AIndex]
		{
			get { return (UpdateColumnExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
    }
    
    public class UpdateStatement : D4DMLStatement
    {
		public UpdateStatement() : base(){}
		public UpdateStatement(Expression ATarget)
		{
			FTarget = ATarget;
		}
		
		public UpdateStatement(Expression ATarget, UpdateColumnExpression[] AColumns)
		{
			FTarget = ATarget;
			FColumns.AddRange(AColumns);
		}
		
		public UpdateStatement(Expression ATarget, UpdateColumnExpression[] AColumns, Expression ACondition)
		{
			FTarget = ATarget;
			FColumns.AddRange(AColumns);
			FCondition = ACondition;
		}
		
        // Target
        protected Expression FTarget;
        public Expression Target
        {
			get { return FTarget; }
			set { FTarget = value; }
        }
        
        // Columns
        protected UpdateColumnExpressions FColumns = new UpdateColumnExpressions();
        public UpdateColumnExpressions Columns { get { return FColumns; } }
        
        // Condition
        protected Expression FCondition;
        public Expression Condition
        {
			get { return FCondition; }
			set { FCondition = value; }
		}
    }
    
    public class DeleteStatement : D4DMLStatement
    {
		public DeleteStatement() : base(){}
		public DeleteStatement(Expression ATarget) : base()
		{
			FTarget = ATarget;
		}
		
        // Target
        protected Expression FTarget;
        public Expression Target
        {
			get { return FTarget; }
			set { FTarget = value; }
        }
    }
    
    public class RestrictExpression : Expression
    {
		public RestrictExpression() : base(){}
		public RestrictExpression(Expression AExpression, Expression ACondition)
		{
			FExpression = AExpression;
			FCondition = ACondition;
		}
		
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

        // Condition
        protected Expression FCondition;
        public Expression Condition
        {
            get { return FCondition; }
            set { FCondition = value; }
        }
    }

    public class OnExpression : Expression
    {		
		public OnExpression() : base(){}
		public OnExpression(Expression AExpression, string AServerName)
		{
			FExpression = AExpression;
			FServerName = AServerName;
		}
		
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

        // ServerName
        protected string FServerName = String.Empty;
        public string ServerName
        {
            get { return FServerName; }
            set { FServerName = value == null ? String.Empty : value; }
        }
    }
    
    public class AsExpression : Expression
    {
		public AsExpression() : base(){}
		public AsExpression(Expression AExpression, TypeSpecifier ATypeSpecifier) : base()
		{
			FExpression = AExpression;
			FTypeSpecifier = ATypeSpecifier;
		}
		
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

        // TypeSpecifier
        protected TypeSpecifier FTypeSpecifier;
        public TypeSpecifier TypeSpecifier
        {
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
        }
    }
    
    public class IsExpression : Expression
    {
		public IsExpression() : base(){}
		public IsExpression(Expression AExpression, TypeSpecifier ATypeSpecifier) : base()
		{
			FExpression = AExpression;
			FTypeSpecifier = ATypeSpecifier;
		}
		
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

        // TypeSpecifier
        protected TypeSpecifier FTypeSpecifier;
        public TypeSpecifier TypeSpecifier
        {
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
        }
    }
    
    public class ColumnExpression : Expression
    {
		public ColumnExpression() : base(){}
		public ColumnExpression(string AColumnName) : base()
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
    
	public class ColumnExpressions : Expressions
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is ColumnExpression))
				throw new LanguageException(LanguageException.Codes.ColumnExpressionContainer);
			base.Validate(AItem);
		}
		
		public new ColumnExpression this[int AIndex]
		{
			get { return (ColumnExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class ProjectExpression : Expression
    {
        public ProjectExpression() : base() {}
        public ProjectExpression(Expression AExpression, string[] AColumnNames) : base()
        {
			FExpression = AExpression;
			for (int LIndex = 0; LIndex < AColumnNames.Length; LIndex++)
				FColumns.Add(new ColumnExpression(AColumnNames[LIndex]));
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Columns
        protected ColumnExpressions FColumns = new ColumnExpressions();
        public ColumnExpressions Columns { get { return FColumns; } }
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
		public NamedColumnExpression(Expression AExpression, string AColumnAlias) : base()
		{
			ColumnAlias = AColumnAlias;
			FExpression = AExpression;
		}
		
		public NamedColumnExpression(Expression AExpression, string AColumnAlias, MetaData AMetaData) : base()
		{
			ColumnAlias = AColumnAlias;
			FExpression = AExpression;
			FMetaData = AMetaData;
		}
		
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

		// ColumnAlias
		protected string FColumnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return FColumnAlias; }
			set { FColumnAlias = value == null ? String.Empty : value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
    }
    
	public class NamedColumnExpressions : Expressions
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is NamedColumnExpression))
				throw new LanguageException(LanguageException.Codes.NamedColumnExpressionContainer);
			base.Validate(AItem);
		}
		
		public new NamedColumnExpression this[int AIndex]
		{
			get { return (NamedColumnExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class ExtendExpression : Expression
    {
		public ExtendExpression() : base(){}
		public ExtendExpression(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		public ExtendExpression(Expression AExpression, NamedColumnExpression[] AExpressions) : base()
		{
			FExpression = AExpression;
			FExpressions.AddRange(AExpressions);
		}

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Expressions
        protected NamedColumnExpressions FExpressions = new NamedColumnExpressions();
        public NamedColumnExpressions Expressions { get { return FExpressions; } }
    }
    
	public class SpecifyExpression : Expression
    {
		public SpecifyExpression() : base(){}
		public SpecifyExpression(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		public SpecifyExpression(Expression AExpression, NamedColumnExpression[] AExpressions) : base()
		{
			FExpression = AExpression;
			FExpressions.AddRange(AExpressions);
		}

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Expressions
        protected NamedColumnExpressions FExpressions = new NamedColumnExpressions();
        public NamedColumnExpressions Expressions { get { return FExpressions; } }
    }
    
    public class ListSelectorExpression : Expression
    {
        public ListSelectorExpression() : base(){}
        public ListSelectorExpression(Expression[] AExpressions) : base()
        {
			FExpressions.AddRange(AExpressions);
        }
        
        public ListSelectorExpression(TypeSpecifier ATypeSpecifier, Expression[] AExpressions) : base()
        {
			FTypeSpecifier = ATypeSpecifier;
			FExpressions.AddRange(AExpressions);
        }
        
        // TypeSpecifier
        protected TypeSpecifier FTypeSpecifier;
        public TypeSpecifier TypeSpecifier
        {
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
        }

        // Expressions
        protected Expressions FExpressions = new Expressions();
        public Expressions Expressions { get { return FExpressions; } }
    }
    
    public class CursorDefinition : Expression
    {
		public CursorDefinition() : base(){}
		public CursorDefinition(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		public CursorDefinition(Expression AExpression, CursorCapability ACapabilities) : base()
		{
			FExpression = AExpression;
			FCapabilities = ACapabilities;
		}
		
		public CursorDefinition(Expression AExpression, CursorCapability ACapabilities, CursorIsolation AIsolation) : base()
		{
			FExpression = AExpression;
			FCapabilities = ACapabilities;
			FIsolation = AIsolation;
		}
		
		public CursorDefinition(Expression AExpression, CursorCapability ACapabilities, CursorIsolation AIsolation, CursorType ACursorType) : base()
		{
			FExpression = AExpression;
			FCapabilities = ACapabilities;
			FIsolation = AIsolation;
			FCursorType = ACursorType;
		}
		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
		
		protected CursorCapability FCapabilities = 0;
		public CursorCapability Capabilities
		{
			get { return FCapabilities; }
			set { FCapabilities = value; }
		}

		protected CursorIsolation FIsolation = CursorIsolation.None;
		public CursorIsolation Isolation
		{
			get { return FIsolation; }
			set { FIsolation = value; }
		}
		
		protected bool FSpecifiesType;
		public bool SpecifiesType
		{
			get { return FSpecifiesType; }
			set { FSpecifiesType = value; }
		}
		
		protected CursorType FCursorType = CursorType.Dynamic;
		public CursorType CursorType
		{
			get { return FCursorType; }
			set { FCursorType = value; }
		}
    }
    
    public class CursorSelectorExpression : Expression
    {
        public CursorSelectorExpression() : base(){}
        public CursorSelectorExpression(CursorDefinition ACursorDefinition) : base()
        {
			FCursorDefinition = ACursorDefinition;
        }

        // CursorDefinition
        protected CursorDefinition FCursorDefinition;
        public CursorDefinition CursorDefinition
        {
            get { return FCursorDefinition; }
            set { FCursorDefinition = value; }
        }
    }
    
    public class ForEachStatement : Statement 
    {
		protected bool FIsAllocation;
		/// <summary>Indicates whether the variable exists in the current stack window, or should be allocated by the statement</summary>
		public bool IsAllocation
		{
			get { return FIsAllocation; }
			set { FIsAllocation = value; }
		}
		
		protected string FVariableName = String.Empty;
		/// <summary>The name of the variable that will receive the value for each successive iteration. If variable name is empty, this is a row foreach statement.</summary>
		public string VariableName
		{
			get { return FVariableName; }
			set { FVariableName = value == null ? String.Empty : value; }
		}
		
		protected CursorDefinition FExpression;
		/// <summary>The list or cursor to be iterated over.</summary>
		public CursorDefinition Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}

		protected Statement FStatement;
		/// <summary>The iterative statement to be executed.</summary>
		public Statement Statement
		{
			get { return FStatement; }
			set { FStatement = value; }
		}
    }
    
    public class ColumnExtractorExpression : Expression
    {		
		public ColumnExtractorExpression() : base(){}
		public ColumnExtractorExpression(string AColumnName, Expression AExpression) : base()
		{
			FColumns.Add(new ColumnExpression(AColumnName));
			FExpression = AExpression;
		}
		
		protected ColumnExpressions FColumns = new ColumnExpressions();
		public ColumnExpressions Columns { get { return FColumns; } }
		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}

        // HasByClause
        protected bool FHasByClause;
        public bool HasByClause
        {
			get { return FHasByClause; }
			set { FHasByClause = value; }
		}
        
        // OrderColumns
        protected OrderColumnDefinitions FOrderColumns = new OrderColumnDefinitions();
        public OrderColumnDefinitions OrderColumns { get { return FOrderColumns; } }
	}
    
    public class RowExtractorExpressionBase : Expression
    {
		public RowExtractorExpressionBase() : base(){}
		public RowExtractorExpressionBase(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
    }
    
    public class RowExtractorExpression : RowExtractorExpressionBase
    {
		public RowExtractorExpression() : base(){}
		public RowExtractorExpression(Expression AExpression) : base(AExpression){}
    }
    
    public class EntryExtractorExpression : RowExtractorExpressionBase
    {
		public EntryExtractorExpression() : base(){}
		public EntryExtractorExpression(Expression AExpression) : base(AExpression){}
    }
    
    public class RowSelectorExpressionBase : Expression
    {
		public RowSelectorExpressionBase() : base(){}
		public RowSelectorExpressionBase(NamedColumnExpression[] AExpressions) : base()
		{
			FExpressions.AddRange(AExpressions);
		}

        // TypeSpecifier
        protected TypeSpecifier FTypeSpecifier;
        public TypeSpecifier TypeSpecifier
        {
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}

        // Expressions
        protected NamedColumnExpressions FExpressions = new NamedColumnExpressions();
        public NamedColumnExpressions Expressions { get { return FExpressions; } }
    }
    
    public class RowSelectorExpression : RowSelectorExpressionBase
    {
		public RowSelectorExpression() : base(){}
		public RowSelectorExpression(NamedColumnExpression[] AColumns) : base(AColumns){}
    }
    
    public class EntrySelectorExpression : RowSelectorExpressionBase
    {
		public EntrySelectorExpression() : base(){}
		public EntrySelectorExpression(NamedColumnExpression[] AColumns) : base(AColumns){}
    }
    
    public class TableSelectorExpressionBase : Expression
    {
		public TableSelectorExpressionBase() : base(){}
		public TableSelectorExpressionBase(Expression[] AExpressions) : base()
		{
			FExpressions.AddRange(AExpressions);
		}
		
		public TableSelectorExpressionBase(Expression[] AExpressions, KeyDefinition[] AKeyDefinitions) : base()
		{
			FExpressions.AddRange(AExpressions);
			FKeys.AddRange(AKeyDefinitions);
		}
		
        // TypeSpecifier
        protected TypeSpecifier FTypeSpecifier;
        public TypeSpecifier TypeSpecifier
        {
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}

        // Expressions
        protected Expressions FExpressions = new Expressions();
        public Expressions Expressions { get { return FExpressions; } }
        
        // Keys
        protected KeyDefinitions FKeys = new KeyDefinitions();
        public KeyDefinitions Keys { get { return FKeys; } }
    }
    
	public class TableSelectorExpression : TableSelectorExpressionBase
    {
		public TableSelectorExpression() : base(){}
		public TableSelectorExpression(Expression[] AExpressions) : base(AExpressions){}
		public TableSelectorExpression(Expression[] AExpressions, KeyDefinition[] AKeys) : base(AExpressions, AKeys){}
    }
    
	public class PresentationSelectorExpression : TableSelectorExpressionBase
    {
		public PresentationSelectorExpression() : base(){}
		public PresentationSelectorExpression(Expression[] AExpressions) : base(AExpressions){}
		public PresentationSelectorExpression(Expression[] AExpressions, KeyDefinition[] AKeys) : base(AExpressions, AKeys){}
    }
    
    public class RenameColumnExpression : Expression, IMetaData
    {
		public RenameColumnExpression() : base(){}
		public RenameColumnExpression(string AColumnName, string AColumnAlias) : base()
		{
			FColumnName = AColumnName;
			FColumnAlias = AColumnAlias;
		}
		
		public RenameColumnExpression(string AColumnName, string AColumnAlias, MetaData AMetaData) : base()
		{
			FColumnName = AColumnName;
			FColumnAlias = AColumnAlias;
			FMetaData = AMetaData;
		}
		
		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}
		
		// ColumnAlias
		protected string FColumnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return FColumnAlias; }
			set { FColumnAlias = value == null ? String.Empty : value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
    }
    
	public class RenameColumnExpressions : Expressions
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is RenameColumnExpression))
				throw new LanguageException(LanguageException.Codes.RenameColumnExpressionContainer);
			base.Validate(AItem);
		}
		
		public new RenameColumnExpression this[int AIndex]
		{
			get { return (RenameColumnExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class RenameExpression : Expression
    {
		public RenameExpression() : base(){}
		public RenameExpression(Expression AExpression, RenameColumnExpression[] AColumns) : base()
		{
			FExpression = AExpression;
			FExpressions.AddRange(AColumns);
		}
		
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Expressions
        protected RenameColumnExpressions FExpressions = new RenameColumnExpressions();
        public RenameColumnExpressions Expressions { get { return FExpressions; } }
    }
    
    public class RenameAllExpression : Expression, IMetaData
    {
		public RenameAllExpression() : base(){}
		public RenameAllExpression(Expression AExpression, string AIdentifier) : base()
		{
			FExpression = AExpression;
			Identifier = AIdentifier;
		}
		
		public RenameAllExpression(Expression AExpression, string AIdentifier, MetaData AMetaData) : base()
		{
			FExpression = AExpression;
			Identifier = AIdentifier;
			MetaData = FMetaData;
		}

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }

        // Identifier
        protected string FIdentifier = String.Empty;
        public string Identifier
        {
            get { return FIdentifier; }
            set { FIdentifier = value == null ? String.Empty : value; }
        }
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
    }
    
	#if USENOTIFYLISTFORMETADATA
    public class AdornColumnExpression : Expression, IMetaData, IAlterMetaData, INotifyListItem
    #else
    public class AdornColumnExpression : Expression, IMetaData, IAlterMetaData
    #endif
    {
		public AdornColumnExpression() : base()
		{
			#if USENOTIFYLISTFORMETADATA
			FConstraints.OnChanged += new ListEventHandler(ListChanged);
			#endif
		}
		
		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set 
			{ 
				if (FColumnName != value)
				{
					FColumnName = value == null ? String.Empty : value; 
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
		
		// ChangeNilable
		private bool FChangeNilable;
		public bool ChangeNilable
		{
			get { return FChangeNilable; }
			set { FChangeNilable = value; }
		}
		
		// IsNilable
		private bool FIsNilable;
		public bool IsNilable
		{
			get { return FIsNilable; }
			set { FIsNilable = value; }
		}

		// Default		
		protected DefaultDefinition FDefault;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.DefaultDefinitionEdit,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		public DefaultDefinition Default
		{
			get { return FDefault; }
			set 
			{ 
				if (FDefault != value)
				{
					#if USENOTIFYLISTFORMETADATA
					if (FDefault != null)
						FDefault.OnChanged -= new ListItemEventHandler(ItemChanged);
					#endif
					FDefault = value; 
					#if USENOTIFYLISTFORMETADATA
					if (FDefault != null)
						FDefault.OnChanged += new ListItemEventHandler(ItemChanged);
					Changed();
					#endif
				}
			}
		}

		// Constraints
		protected ConstraintDefinitions FConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions Constraints
		{
			get { return FConstraints; }
			set 
			{ 
				if (FConstraints != value)
				{
					#if USENOTIFYLISTFORMETADATA
					if (FConstraints != null)
						FConstraints.OnChanged -= new ListEventHandler(ListChanged);
					#endif
					FConstraints = value;
					#if USENOTIFYLISTFORMETADATA
					if (FConstraints != null)
						FConstraints.OnChanged += new ListEventHandler(ListChanged);
					Changed();
					#endif
				}
			}
		}
		
        // MetaData
        protected MetaData FMetaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
		public MetaData MetaData
        {
			get { return FMetaData; }
			set 
			{ 
				if (FMetaData != value)
				{
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged -= new ListItemEventHandler(ItemChanged);
					#endif
					FMetaData = value; 
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged += new ListItemEventHandler(ItemChanged);
					Changed();
					#endif
				}
			}
        }
        
		protected AlterMetaData FAlterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
		}

		#if USENOTIFYLISTFORMETADATA
        public event ListItemEventHandler OnChanged;
        private void Changed()
        {
			if (OnChanged != null)
				OnChanged(this);
        }
        
        private void ItemChanged(object ASender)
        {
			Changed();
        }
        
        private void ListChanged(object ASender, object AItem)
        {
			Changed();
        }
        #endif
    }
    
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.AdornColumnExpressionEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
	public class AdornColumnExpressions : Expressions
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AdornColumnExpression))
				throw new LanguageException(LanguageException.Codes.AdornColumnExpressionContainer);
			base.Validate(AItem);
		}
		
		public new AdornColumnExpression this[int AIndex]
		{
			get { return (AdornColumnExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AdornExpression : Expression, IMetaData, IAlterMetaData
	{
		public AdornExpression()
		{
			FAlterOrders = new AlterOrderDefinitions();
			FDropOrders = new DropOrderDefinitions();
			FKeys = new KeyDefinitions();
			FAlterKeys = new AlterKeyDefinitions();
			FDropKeys = new DropKeyDefinitions();
			FAlterReferences = new AlterReferenceDefinitions();
			FDropReferences = new DropReferenceDefinitions();
		}

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Expressions
        protected AdornColumnExpressions FExpressions = new AdornColumnExpressions();
        public AdornColumnExpressions Expressions { get { return FExpressions; } }
        
        // Constraints
        protected CreateConstraintDefinitions FConstraints = new CreateConstraintDefinitions();
        public CreateConstraintDefinitions Constraints { get { return FConstraints; } }
        
        // Orders
        protected OrderDefinitions FOrders = new OrderDefinitions();
        public OrderDefinitions Orders { get { return FOrders; } }

        // AlterOrders
        protected AlterOrderDefinitions FAlterOrders = new AlterOrderDefinitions();
        public AlterOrderDefinitions AlterOrders { get { return FAlterOrders; } }

        // DropOrders
        protected DropOrderDefinitions FDropOrders = new DropOrderDefinitions();
        public DropOrderDefinitions DropOrders { get { return FDropOrders; } }

        // Keys
        protected KeyDefinitions FKeys = new KeyDefinitions();
        public KeyDefinitions Keys { get { return FKeys; } }
        
        // AlterKeys
        protected AlterKeyDefinitions FAlterKeys = new AlterKeyDefinitions();
        public AlterKeyDefinitions AlterKeys { get { return FAlterKeys; } }

        // DropKeys
        protected DropKeyDefinitions FDropKeys = new DropKeyDefinitions();
        public DropKeyDefinitions DropKeys { get { return FDropKeys; } }

		// References
		protected ReferenceDefinitions FReferences = new ReferenceDefinitions();
		public ReferenceDefinitions References { get { return FReferences; } }

        // AlterReferences
        protected AlterReferenceDefinitions FAlterReferences = new AlterReferenceDefinitions();
        public AlterReferenceDefinitions AlterReferences { get { return FAlterReferences; } }

        // DropReferences
        protected DropReferenceDefinitions FDropReferences = new DropReferenceDefinitions();
        public DropReferenceDefinitions DropReferences { get { return FDropReferences; } }

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
		}

		protected AlterMetaData FAlterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
		}
	}
    
	public class RedefineExpression : Expression
	{
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Expressions
        protected NamedColumnExpressions FExpressions = new NamedColumnExpressions();
        public NamedColumnExpressions Expressions { get { return FExpressions; } }
	}
    
	public class RemoveExpression : Expression
    {
        public RemoveExpression() : base(){}
        public RemoveExpression(Expression AExpression, ColumnExpression[] AColumns)
        {
			Expression = AExpression;
			FColumns.AddRange(AColumns);
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Columns
        protected ColumnExpressions FColumns = new ColumnExpressions();
        public ColumnExpressions Columns { get { return FColumns; } }
    }
    
    public class AggregateColumnExpression : Expression, IMetaData
    {
        public AggregateColumnExpression() : base(){}

        // AggregateOperator
        protected string FAggregateOperator = String.Empty;
        public string AggregateOperator
        {
            get { return FAggregateOperator; }
            set { FAggregateOperator = value == null ? String.Empty : value; }
        }
        
        // Distinct
        protected bool FDistinct;
        public bool Distinct
        {
			get { return FDistinct; }
			set { FDistinct = value; }
        }

        // Columns
        protected ColumnExpressions FColumns = new ColumnExpressions();
        public ColumnExpressions Columns { get { return FColumns; } }

        // HasByClause
        protected bool FHasByClause;
        public bool HasByClause
        {
			get { return FHasByClause; }
			set { FHasByClause = value; }
		}
        
        // OrderColumns
        protected OrderColumnDefinitions FOrderColumns = new OrderColumnDefinitions();
        public OrderColumnDefinitions OrderColumns { get { return FOrderColumns; } }

        // ColumnAlias
        protected string FColumnAlias = String.Empty;
        public string ColumnAlias
        {
            get { return FColumnAlias; }
            set { FColumnAlias = value == null ? String.Empty : value; }
        }
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
    }
    
	public class AggregateColumnExpressions : Expressions
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AggregateColumnExpression))
				throw new LanguageException(LanguageException.Codes.AggregateColumnExpressionContainer);
			base.Validate(AItem);
		}
		
		public new AggregateColumnExpression this[int AIndex]
		{
			get { return (AggregateColumnExpression)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class AggregateExpression : Expression
    {
        public AggregateExpression() : base(){}
        public AggregateExpression(Expression AExpression, ColumnExpression[] AByColumns, AggregateColumnExpression[] AComputeColumns) : base()
        {
			FExpression = AExpression;
			FByColumns.AddRange(AByColumns);
			FComputeColumns.AddRange(AComputeColumns);
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // ByColumns
        protected ColumnExpressions FByColumns = new ColumnExpressions();
        public ColumnExpressions ByColumns { get { return FByColumns; } }
        
        // ComputeColumns
        protected AggregateColumnExpressions FComputeColumns = new AggregateColumnExpressions();
        public AggregateColumnExpressions ComputeColumns { get { return FComputeColumns; } }
    }
    
	public abstract class BaseOrderExpression : Expression
    {
        public BaseOrderExpression() : base(){}
        public BaseOrderExpression(Expression AExpression, OrderColumnDefinition[] AColumns) : base()
        {
			FExpression = AExpression;
            FColumns.AddRange(AColumns);
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Columns
        protected OrderColumnDefinitions FColumns = new OrderColumnDefinitions();
        public OrderColumnDefinitions Columns { get { return FColumns; } }
    }
    
    public class OrderExpression : BaseOrderExpression
    {
		public OrderExpression() : base() {}
		public OrderExpression(Expression AExpression, OrderColumnDefinitions AColumns) : base()
		{
			Expression = AExpression;
			Columns.AddRange(AColumns);
		}
		
		// SequenceColumn
        protected IncludeColumnExpression FSequenceColumn;
        public IncludeColumnExpression SequenceColumn
        {
			get { return FSequenceColumn; }
			set { FSequenceColumn = value; }
        }
    }
    
    public class BrowseExpression : BaseOrderExpression{}
    
    public class D4IndexerExpression : IndexerExpression
    {
		protected bool FHasByClause;
		public bool HasByClause
		{
			get { return FHasByClause; }
			set { FHasByClause = value; }
		}
		
		protected KeyColumnDefinitions FByClause = new KeyColumnDefinitions();
		public KeyColumnDefinitions ByClause { get { return FByClause; } }
    }
    
    public class QuotaExpression : Expression
    {
        public QuotaExpression() : base(){}
        public QuotaExpression(Expression AExpression, Expression AQuota, OrderColumnDefinition[] AColumns) : base()
        {
			FExpression = AExpression;
			FQuota = AQuota;
			FColumns.AddRange(AColumns);
        }

        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // Quota
        protected Expression FQuota;
        public Expression Quota
        {
			get { return FQuota; }
			set { FQuota = value; }
        }
        
        // HasByClause
        protected bool FHasByClause;
        public bool HasByClause
        {
			get { return FHasByClause; }
			set { FHasByClause = value; }
		}
        
        // Columns
        protected OrderColumnDefinitions FColumns = new OrderColumnDefinitions();
        public OrderColumnDefinitions Columns { get { return FColumns; } }
    }
    
    public class ExplodeColumnExpression : Expression
    {
        public ExplodeColumnExpression() : base(){}
        public ExplodeColumnExpression(string AColumnName) : base()
        {
            FColumnName = AColumnName;
        }
        
        // ColumnName
        protected string FColumnName = String.Empty;
        public string ColumnName
        {
            get { return FColumnName; }
            set { FColumnName = value == null ? String.Empty : value; }
        }
    }
    
    public class IncludeColumnExpression : Expression, IMetaData
    {
		public IncludeColumnExpression() : base(){}
		public IncludeColumnExpression(string AColumnAlias) : base()
		{
			ColumnAlias = AColumnAlias;
		}
		
		public IncludeColumnExpression(string AColumnAlias, MetaData AMetaData)
		{
			ColumnAlias = AColumnAlias;
			FMetaData = AMetaData;
		}
		
		// ColumnAlias
		protected string FColumnAlias = String.Empty;
		public string ColumnAlias
		{
			get { return FColumnAlias; }
			set { FColumnAlias = value == null ? String.Empty : value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
    }
    
    public class ExplodeExpression : Expression
    {
        public ExplodeExpression() : base(){}
        public ExplodeExpression(Expression AExpression, Expression AByExpression, Expression ARootExpression) : base()
        {
			FExpression = AExpression;
			FByExpression = AByExpression;
			FRootExpression = ARootExpression;
        }
        
        // Expression
        protected Expression FExpression;
        public Expression Expression
        {
            get { return FExpression; }
            set { FExpression = value; }
        }
        
        // ByExpression
        protected Expression FByExpression;
        public Expression ByExpression
        {
            get { return FByExpression; }
            set { FByExpression = value; }
        }
        
        // RootExpression
        protected Expression FRootExpression;
        public Expression RootExpression
        {
            get { return FRootExpression; }
            set { FRootExpression = value; }
        }
        
        // HasOrderByClause
        protected bool FHasOrderByClause;
        public bool HasOrderByClause
        {
			get { return FHasOrderByClause; }
			set { FHasOrderByClause = value; }
		}
        
        // OrderColumns
        protected OrderColumnDefinitions FOrderColumns = new OrderColumnDefinitions();
        public OrderColumnDefinitions OrderColumns { get { return FOrderColumns; } }

        // LevelColumn
        protected IncludeColumnExpression FLevelColumn;
        public IncludeColumnExpression LevelColumn
        {
            get { return FLevelColumn; }
            set { FLevelColumn = value; }
        }

        // SequenceColumn
        protected IncludeColumnExpression FSequenceColumn;
        public IncludeColumnExpression SequenceColumn
        {
            get { return FSequenceColumn; }
            set { FSequenceColumn = value; }
        }
    }
    
    public abstract class BinaryTableExpression : Expression
    {
		public BinaryTableExpression() : base(){}
		public BinaryTableExpression(Expression ALeftExpression, Expression ARightExpression)
		{
			FLeftExpression = ALeftExpression;
			FRightExpression = ARightExpression;
		}
		
        // LeftExpression
        protected Expression FLeftExpression;
        public Expression LeftExpression
        {
            get { return FLeftExpression; }
            set { FLeftExpression = value; }
        }
        
        // RightExpression
        protected Expression FRightExpression;
        public Expression RightExpression
        {
            get { return FRightExpression; }
            set { FRightExpression = value; }
        }
    }
    
    public class UnionExpression : BinaryTableExpression
    {
		public UnionExpression() : base(){}
		public UnionExpression(Expression ALeftExpression, Expression ARightExpression)
		{
			FLeftExpression = ALeftExpression;
			FRightExpression = ARightExpression;
		}
    }
    
    public class IntersectExpression : BinaryTableExpression
    {
		public IntersectExpression() : base(){}
		public IntersectExpression(Expression ALeftExpression, Expression ARightExpression)
		{
			FLeftExpression = ALeftExpression;
			FRightExpression = ARightExpression;
		}
    }
    
    public class DifferenceExpression : BinaryTableExpression
    {
		public DifferenceExpression() : base(){}
		public DifferenceExpression(Expression ALeftExpression, Expression ARightExpression)
		{
			FLeftExpression = ALeftExpression;
			FRightExpression = ARightExpression;
		}
    }
    
    public class ProductExpression : BinaryTableExpression
    {
		public ProductExpression() : base(){}
		public ProductExpression(Expression ALeftExpression, Expression ARightExpression)
		{
			FLeftExpression = ALeftExpression;
			FRightExpression = ARightExpression;
		}
    }
    
    public class DivideExpression : BinaryTableExpression
    {
		public DivideExpression() : base(){}
		public DivideExpression(Expression ALeftExpression, Expression ARightExpression)
		{
			FLeftExpression = ALeftExpression;
			FRightExpression = ARightExpression;
		}
    }
    
    public class ConditionedBinaryTableExpression : BinaryTableExpression
    {
		private Expression FCondition;
		public Expression Condition
		{
			get { return FCondition; }
			set { FCondition = value; }
		}
    }
    
    public class HavingExpression : ConditionedBinaryTableExpression
    {
		public HavingExpression() : base() { }
		public HavingExpression(Expression ALeftExpression, Expression ARightExpression, Expression ACondition)
		{
			LeftExpression = ALeftExpression;
			RightExpression = ARightExpression;
			Condition = ACondition;
		}
    }
    
    public class WithoutExpression : ConditionedBinaryTableExpression
    {
		public WithoutExpression() : base() { }
		public WithoutExpression(Expression ALeftExpression, Expression ARightExpression, Expression ACondition)
		{
			LeftExpression = ALeftExpression;
			RightExpression = ARightExpression;
			Condition = ACondition;
		}
    }
    
	public enum JoinCardinality { OneToOne, OneToMany, ManyToOne, ManyToMany }
		
    public abstract class JoinExpression : ConditionedBinaryTableExpression
    {
        // IsLookup
		protected bool FIsLookup;
		public bool IsLookup
		{
			get { return FIsLookup; }
			set { FIsLookup = value; }
		}
		
		// This is not used by the compiler, it is set by statement emission of a compiled plan and used by the application transaction emitter.
		protected JoinCardinality FCardinality;
		public JoinCardinality Cardinality
		{
			get { return FCardinality; }
			set { FCardinality = value; }
		}
		
		// IsDetailLookup
		protected bool FIsDetailLookup;
		public bool IsDetailLookup
		{
			get { return FIsDetailLookup; }
			set { FIsDetailLookup = value; }
		}
    }
    
    public class InnerJoinExpression : JoinExpression{}
    
    public abstract class OuterJoinExpression : JoinExpression
    {
		public OuterJoinExpression() : base(){}
		
        // RowExistsColumn
        protected IncludeColumnExpression FRowExistsColumn;
        public IncludeColumnExpression RowExistsColumn
        {
            get { return FRowExistsColumn; }
            set { FRowExistsColumn = value; }
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
		public VariableStatement(string AVariableName, TypeSpecifier ATypeSpecifier) : base()
		{
			VariableName = new IdentifierExpression(AVariableName);
			FTypeSpecifier = ATypeSpecifier;
		}
		
		public VariableStatement(string AVariableName, TypeSpecifier ATypeSpecifier, Expression AExpression) : base()
		{
			VariableName = new IdentifierExpression(AVariableName);
			FTypeSpecifier = ATypeSpecifier;
			FExpression = AExpression;
		}
		
		// VariableName
		protected IdentifierExpression FVariableName;
		public IdentifierExpression VariableName
		{
			get { return FVariableName; }
			set { FVariableName = value; }
		}
		
		// TypeSpecifier
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
		
		// Expression
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class ExpressionStatement : Statement
	{
		public ExpressionStatement() : base(){}
		public ExpressionStatement(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class AssignmentStatement : Statement
	{
		public AssignmentStatement() : base(){}
		public AssignmentStatement(Expression ATarget, Expression AExpression)
		{
			FTarget = ATarget;
			FExpression = AExpression;
		}
		
		// Target
		protected Expression FTarget;
		public Expression Target
		{
			get { return FTarget; }
			set { FTarget = value; }
		}
		
		// Expression
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public abstract class CreateTableVarStatement : D4Statement, IMetaData
	{
		// TableVarName
		protected string FTableVarName = String.Empty;
		public string TableVarName
		{
			get { return FTableVarName; }
			set { FTableVarName = value == null ? String.Empty : value; }
		}
		
		// IsSession
		protected bool FIsSession = false;
		public bool IsSession
		{
			get { return FIsSession; }
			set { FIsSession = value; }
		}
		
		// Keys
		protected KeyDefinitions FKeys = new KeyDefinitions();
		public KeyDefinitions Keys { get { return FKeys; } }

		// References
		protected ReferenceDefinitions FReferences = new ReferenceDefinitions();
		public ReferenceDefinitions References { get { return FReferences; } }

		// Constraints
		protected CreateConstraintDefinitions FConstraints = new CreateConstraintDefinitions();
		public CreateConstraintDefinitions Constraints { get { return FConstraints; } }
		
		// Orders
		protected OrderDefinitions FOrders = new OrderDefinitions();
		public OrderDefinitions Orders { get { return FOrders; } }

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}

	public class CreateTableStatement : CreateTableVarStatement
	{		
		// DeviceName
		protected IdentifierExpression FDeviceName;
		public IdentifierExpression DeviceName
		{
			get { return FDeviceName; }
			set { FDeviceName = value; }
		}
		
		// Columns
		protected ColumnDefinitions FColumns = new ColumnDefinitions();
		public ColumnDefinitions Columns { get { return FColumns; } }

		// FromExpression		
		protected Expression FFromExpression;
		public Expression FromExpression
		{
			get { return FFromExpression; }
			set { FFromExpression = value; }
		}
	}
	
	public class CreateViewStatement : CreateTableVarStatement
	{		
		// Expression		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class AccessorBlock : D4Statement
	{
		private ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
		}
		
		private Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
		
		private Statement FBlock;
		public Statement Block
		{
			get { return FBlock; }
			set { FBlock = value; }
		}
		
		public bool IsD4Implemented()
		{
			return (FExpression != null) || (FBlock != null);
		}
	}
	
	public class AlterAccessorBlock : D4Statement
	{
		private AlterClassDefinition FAlterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return FAlterClassDefinition; }
			set { FAlterClassDefinition = value; }
		}
		
		private Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
		
		private Statement FBlock;
		public Statement Block
		{
			get { return FBlock; }
			set { FBlock = value; }
		}
	}
	
	public abstract class PropertyDefinitionBase : D4Statement
	{
		public PropertyDefinitionBase() : base() {}
		public PropertyDefinitionBase(string APropertyName) : base()
		{
			PropertyName = APropertyName;
		}
		
		// PropertyName		
		private string FPropertyName = String.Empty;
		public string PropertyName
		{
			get { return FPropertyName; }
			set { FPropertyName = value == null ? String.Empty : value; }
		}
	}
	
	public class PropertyDefinition : PropertyDefinitionBase, IMetaData
	{
		public PropertyDefinition() : base() {}
		public PropertyDefinition(string APropertyName) : base(APropertyName){}
		public PropertyDefinition(string APropertyName, TypeSpecifier APropertyType) : base(APropertyName)
		{
			FPropertyType = APropertyType;
		}
		
		// PropertyType
		protected TypeSpecifier FPropertyType;
		public TypeSpecifier PropertyType
		{
			get { return FPropertyType; }
			set { FPropertyType = value; }
		}
		
		// ReadAccessorBlock
		private AccessorBlock FReadAccessorBlock;
		public AccessorBlock ReadAccessorBlock
		{
			get { return FReadAccessorBlock; }
			set { FReadAccessorBlock = value; }
		}
		
		// WriteAccessorBlock
		private AccessorBlock FWriteAccessorBlock;
		public AccessorBlock WriteAccessorBlock
		{
			get { return FWriteAccessorBlock; }
			set { FWriteAccessorBlock = value; }
		}
		
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class PropertyDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is PropertyDefinition))
				throw new LanguageException(LanguageException.Codes.PropertyDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new PropertyDefinition this[int AIndex]
		{
			get { return (PropertyDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterPropertyDefinition : PropertyDefinitionBase, IAlterMetaData
	{
		public AlterPropertyDefinition() : base() {} 
		public AlterPropertyDefinition(string APropertyName) : base(APropertyName){}
		
		// PropertyType
		protected TypeSpecifier FPropertyType;
		public TypeSpecifier PropertyType
		{
			get { return FPropertyType; }
			set { FPropertyType = value; }
		}
		
		// ReadAccessorBlock
		private AlterAccessorBlock FReadAccessorBlock;
		public AlterAccessorBlock ReadAccessorBlock
		{
			get { return FReadAccessorBlock; }
			set { FReadAccessorBlock = value; }
		}
		
		// WriteAccessorBlock
		private AlterAccessorBlock FWriteAccessorBlock;
		public AlterAccessorBlock WriteAccessorBlock
		{
			get { return FWriteAccessorBlock; }
			set { FWriteAccessorBlock = value; }
		}

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterPropertyDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterPropertyDefinition))
				throw new LanguageException(LanguageException.Codes.AlterPropertyDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterPropertyDefinition this[int AIndex]
		{
			get { return (AlterPropertyDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropPropertyDefinition : PropertyDefinitionBase
	{
		public DropPropertyDefinition() : base() {}
		public DropPropertyDefinition(string APropertyName) : base(APropertyName){}
	}
	
	public class DropPropertyDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropPropertyDefinition))
				throw new LanguageException(LanguageException.Codes.DropPropertyDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropPropertyDefinition this[int AIndex]
		{
			get { return (DropPropertyDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public abstract class RepresentationDefinitionBase : D4Statement
	{
		public RepresentationDefinitionBase() : base() {}
		public RepresentationDefinitionBase(string ARepresentationName) : base()
		{
			RepresentationName = ARepresentationName;
		}
		
		private string FRepresentationName = String.Empty;
		public string RepresentationName
		{
			get { return FRepresentationName; }
			set { FRepresentationName = value == null ? String.Empty : value; }
		}
	}
	
	public class RepresentationDefinition : RepresentationDefinitionBase, IMetaData
	{
		public RepresentationDefinition() : base() {}
		public RepresentationDefinition(string ARepresentationName) : base(ARepresentationName) {}
		
		private bool FIsGenerated;
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}
		
		public bool HasD4ImplementedComponents()
		{
			if ((FSelectorAccessorBlock != null) && FSelectorAccessorBlock.IsD4Implemented())
				return true;
			
			foreach (PropertyDefinition LProperty in FProperties)
				if 
				(
					((LProperty.ReadAccessorBlock != null) && LProperty.ReadAccessorBlock.IsD4Implemented()) || 
					((LProperty.WriteAccessorBlock != null) && LProperty.WriteAccessorBlock.IsD4Implemented())
				)
					return true;
					
			return false;
		}
		
		private PropertyDefinitions FProperties = new PropertyDefinitions();
		public PropertyDefinitions Properties { get { return FProperties; } }
		
		// SelectorAccessorBlock
		private AccessorBlock FSelectorAccessorBlock;
		public AccessorBlock SelectorAccessorBlock
		{
			get { return FSelectorAccessorBlock; }
			set { FSelectorAccessorBlock = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class RepresentationDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is RepresentationDefinition))
				throw new LanguageException(LanguageException.Codes.RepresentationDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new RepresentationDefinition this[int AIndex]
		{
			get { return (RepresentationDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Compare(AName, this[LIndex].RepresentationName) == 0)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class AlterRepresentationDefinition : RepresentationDefinitionBase, IAlterMetaData
	{
		public AlterRepresentationDefinition() : base() {}
		public AlterRepresentationDefinition(string ARepresentationName) : base(ARepresentationName) {}

		// CreateProperties		
		private PropertyDefinitions FCreateProperties = new PropertyDefinitions();
		public PropertyDefinitions CreateProperties { get { return FCreateProperties; } }

		// AlterProperties		
		private AlterPropertyDefinitions FAlterProperties = new AlterPropertyDefinitions();
		public AlterPropertyDefinitions AlterProperties { get { return FAlterProperties; } }

		// DropProperties		
		private DropPropertyDefinitions FDropProperties = new DropPropertyDefinitions();
		public DropPropertyDefinitions DropProperties { get { return FDropProperties; } }

		// SelectorAccessorBlock
		private AlterAccessorBlock FSelectorAccessorBlock;
		public AlterAccessorBlock SelectorAccessorBlock
		{
			get { return FSelectorAccessorBlock; }
			set { FSelectorAccessorBlock = value; }
		}

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterRepresentationDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterRepresentationDefinition))
				throw new LanguageException(LanguageException.Codes.AlterRepresentationDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterRepresentationDefinition this[int AIndex]
		{
			get { return (AlterRepresentationDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropRepresentationDefinition : RepresentationDefinitionBase
	{
		public DropRepresentationDefinition() : base() {}
		public DropRepresentationDefinition(string AName) : base(AName) {}
	}
	
	public class DropRepresentationDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropRepresentationDefinition))
				throw new LanguageException(LanguageException.Codes.DropRepresentationDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropRepresentationDefinition this[int AIndex]
		{
			get { return (DropRepresentationDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public abstract class SpecialDefinitionBase : D4Statement
	{
		public SpecialDefinitionBase() : base() {}
		public SpecialDefinitionBase(string AName) : base()
		{
			Name = AName;
		}
		
		// Name
		protected string FName = String.Empty;
		public string Name
		{
			get { return FName; }
			set { FName = value == null ? String.Empty : value; }
		}
	}
	
	public class SpecialDefinition : SpecialDefinitionBase, IMetaData
	{
		// Value
		protected Expression FValue;
		public Expression Value
		{
			get { return FValue; }
			set { FValue = value; }
		}

		private bool FIsGenerated;
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class SpecialDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is SpecialDefinition))
				throw new LanguageException(LanguageException.Codes.SpecialDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new SpecialDefinition this[int AIndex]
		{
			get { return (SpecialDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Compare(AName, this[LIndex].Name) == 0)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
    
	public class AlterSpecialDefinition : SpecialDefinitionBase, IAlterMetaData
	{
		// Value
		protected Expression FValue;
		public Expression Value
		{
			get { return FValue; }
			set { FValue = value; }
		}

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterSpecialDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterSpecialDefinition))
				throw new LanguageException(LanguageException.Codes.AlterSpecialDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterSpecialDefinition this[int AIndex]
		{
			get { return (AlterSpecialDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class DropSpecialDefinition : SpecialDefinitionBase
	{
		public DropSpecialDefinition() : base() {}
		public DropSpecialDefinition(string AName) : base(AName) {}
	}
	
	public class DropSpecialDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropSpecialDefinition))
				throw new LanguageException(LanguageException.Codes.DropSpecialDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropSpecialDefinition this[int AIndex]
		{
			get { return (DropSpecialDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
    public class ScalarTypeNameDefinition : D4Statement
    {
		public ScalarTypeNameDefinition() : base() {}
		public ScalarTypeNameDefinition(string AScalarTypeName) : base()
		{
			ScalarTypeName = AScalarTypeName;
		}
		
		// ScalarTypeName
		protected string FScalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value == null ? String.Empty : value; }
		}
    }
    
	public class ScalarTypeNameDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is ScalarTypeNameDefinition))
				throw new LanguageException(LanguageException.Codes.ScalarTypeNameDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new ScalarTypeNameDefinition this[int AIndex]
		{
			get { return (ScalarTypeNameDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}

	public class CreateScalarTypeStatement : D4Statement, IMetaData
	{
		// constructor
		public CreateScalarTypeStatement() : base(){}
		
		// ScalarTypeName
		protected string FScalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value == null ? String.Empty : value; }
		}
		
		// LikeScalarTypeName
		protected string FLikeScalarTypeName = String.Empty;
		public string LikeScalarTypeName
		{
			get { return FLikeScalarTypeName; }
			set { FLikeScalarTypeName = value; }
		}

		// ParentScalarTypes
		protected ScalarTypeNameDefinitions FParentScalarTypes = new ScalarTypeNameDefinitions();
		public ScalarTypeNameDefinitions ParentScalarTypes { get { return FParentScalarTypes; } }

		// Representations
		protected RepresentationDefinitions FRepresentations = new RepresentationDefinitions();
		public RepresentationDefinitions Representations { get { return FRepresentations; } }

		// Default		
		protected DefaultDefinition FDefault;
		public DefaultDefinition Default
		{
			get { return FDefault; }
			set { FDefault = value; }
		}

		// Constraints
		protected ConstraintDefinitions FConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions Constraints
		{
			get { return FConstraints; }
			set { FConstraints = value; }
		}
		
		// Specials
		protected SpecialDefinitions FSpecials = new SpecialDefinitions();
		public SpecialDefinitions Specials { get { return FSpecials; } }
		
		// ClassDefinition		
		protected ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class CreateReferenceStatement : ReferenceDefinition
	{
		// TableVarName
		protected string FTableVarName = String.Empty;
		public string TableVarName
		{
			get { return FTableVarName; }
			set { FTableVarName = value == null ? String.Empty : value; }
		}

		// IsSession
		protected bool FIsSession = false;
		public bool IsSession
		{
			get { return FIsSession; }
			set { FIsSession = value; }
		}
	}
	
	public class AlterReferenceStatement : AlterReferenceDefinition{}
	
	public class DropReferenceStatement : DropReferenceDefinition{}
	
	public class OperatorBlock : D4Statement
	{
		// ClassDefinition		
		protected ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
		}
		
		// Block
		protected Statement FBlock;
		public Statement Block
		{
			get { return FBlock; }
			set { FBlock = value; }
		}
	}
	
	public class AlterOperatorBlock : D4Statement
	{
		// AlterClassDefinition		
		protected AlterClassDefinition FAlterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return FAlterClassDefinition; }
			set { FAlterClassDefinition = value; }
		}
		
		// Block
		protected Statement FBlock;
		public Statement Block
		{
			get { return FBlock; }
			set { FBlock = value; }
		}
	}
	
	public abstract class CreateOperatorStatementBase : D4Statement, IMetaData
	{
		// OperatorName
		protected string FOperatorName = String.Empty;
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value == null ? String.Empty : value; }
		}
		
		// IsSession
		protected bool FIsSession;
		public bool IsSession
		{
			get { return FIsSession; }
			set { FIsSession = value; }
		}

		// FormalParameters
		protected FormalParameters FFormalParameters = new FormalParameters();
		public FormalParameters FormalParameters { get { return FFormalParameters; } }
		
		// ReturnType
		protected TypeSpecifier FReturnType;
		public TypeSpecifier ReturnType
		{
			get { return FReturnType; }
			set { FReturnType = value; }
		}
		
		// IsReintroduced
		protected bool FIsReintroduced;
		public bool IsReintroduced
		{
			get { return FIsReintroduced; }
			set { FIsReintroduced = value; }
		}

		// IsAbstract
		protected bool FIsAbstract;
		public bool IsAbstract
		{
			get { return FIsAbstract; }
			set { FIsAbstract = value; }
		}

		// IsVirtual
		protected bool FIsVirtual;
		public bool IsVirtual
		{
			get { return FIsVirtual; }
			set { FIsVirtual = value; }
		}

		// IsOverride
		protected bool FIsOverride;
		public bool IsOverride
		{
			get { return FIsOverride; }
			set { FIsOverride = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class CreateOperatorStatement : CreateOperatorStatementBase
	{
		// Block
		protected OperatorBlock FBlock = new OperatorBlock();
		public OperatorBlock Block { get { return FBlock; } }
	}

	public class CreateAggregateOperatorStatement : CreateOperatorStatementBase
	{
		// Initialization
		protected OperatorBlock FInitialization = new OperatorBlock();
		public OperatorBlock Initialization { get { return FInitialization; } }
		
		// Aggregation		
		protected OperatorBlock FAggregation = new OperatorBlock();
		public OperatorBlock Aggregation { get { return FAggregation; } }
		
		// Finalization
		protected OperatorBlock FFinalization = new OperatorBlock();
		public OperatorBlock Finalization { get { return FFinalization; } }
	}
	
	public class OperatorSpecifier : D4Statement
	{
		public OperatorSpecifier() : base(){}
		public OperatorSpecifier(string AOperatorName, FormalParameterSpecifier[] AFormalParameterSpecifiers) : base()
		{
			OperatorName = AOperatorName;
			FFormalParameterSpecifiers.AddRange(AFormalParameterSpecifiers);
		}
		
		public OperatorSpecifier(string AOperatorName, FormalParameterSpecifiers AFormalParameterSpecifiers) : base()
		{
			OperatorName = AOperatorName;
			FFormalParameterSpecifiers.AddRange(AFormalParameterSpecifiers);
		}
		
		// OperatorName
		protected string FOperatorName = String.Empty;
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value == null ? String.Empty : value; }
		}

		// FormalParameterSpecifiers
		protected FormalParameterSpecifiers FFormalParameterSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers FormalParameterSpecifiers { get { return FFormalParameterSpecifiers; } }
	}
	
	public abstract class AlterOperatorStatementBase : D4Statement, IAlterMetaData
	{
		// OperatorSpecifier
		protected OperatorSpecifier FOperatorSpecifier;
		public OperatorSpecifier OperatorSpecifier
		{
			get { return FOperatorSpecifier; }
			set { FOperatorSpecifier = value; }
		}
		
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterOperatorStatement : AlterOperatorStatementBase
	{
		// Block
		protected AlterOperatorBlock FBlock = new AlterOperatorBlock();
		public AlterOperatorBlock Block { get { return FBlock; } }
	}
	
	public class AlterAggregateOperatorStatement : AlterOperatorStatementBase
	{
		// Initialization
		protected AlterOperatorBlock FInitialization = new AlterOperatorBlock();
		public AlterOperatorBlock Initialization { get { return FInitialization; } }
		
		// Aggregation		
		protected AlterOperatorBlock FAggregation = new AlterOperatorBlock();
		public AlterOperatorBlock Aggregation { get { return FAggregation; } }
		
		// Finalization
		protected AlterOperatorBlock FFinalization = new AlterOperatorBlock();
		public AlterOperatorBlock Finalization { get { return FFinalization; } }
	}
	
	public class CreateServerStatement : D4Statement, IMetaData
	{
		// ServerName
		protected string FServerName = String.Empty;
		public string ServerName
		{
			get { return FServerName; }
			set { FServerName = value == null ? String.Empty : value; }
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
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class AlterServerStatement : D4Statement, IAlterMetaData
	{
		// ServerName
		protected string FServerName = String.Empty;
		public string ServerName
		{
			get { return FServerName; }
			set { FServerName = value == null ? String.Empty : value; }
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
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public abstract class DeviceMapItem : D4Statement{}
	
	public class DeviceScalarTypeMapBase : DeviceMapItem
	{
		public DeviceScalarTypeMapBase() : base() {}
		public DeviceScalarTypeMapBase(string AScalarTypeName) : base()
		{
			ScalarTypeName = AScalarTypeName;
		}
		
		// ScalarTypeName
		protected string FScalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	public class DeviceScalarTypeMap : DeviceScalarTypeMapBase, IMetaData
	{
		public DeviceScalarTypeMap() : base() {}
		public DeviceScalarTypeMap(string AScalarTypeName) : base(AScalarTypeName) {}

		public DeviceScalarTypeMap(string AScalarTypeName, bool AIsGenerated) : base(AScalarTypeName)
		{
			FIsGenerated = AIsGenerated;
		}
		
		private bool FIsGenerated;
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}
		
		// ClassDefinition		
		protected ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class DeviceScalarTypeMaps : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DeviceScalarTypeMap))
				throw new LanguageException(LanguageException.Codes.DeviceScalarTypeMapContainer);
			base.Validate(AItem);
		}
		
		public new DeviceScalarTypeMap this[int AIndex]
		{
			get { return (DeviceScalarTypeMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterDeviceScalarTypeMap : DeviceScalarTypeMapBase, IAlterMetaData
	{
		// AlterClassDefinition		
		protected AlterClassDefinition FAlterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return FAlterClassDefinition; }
			set { FAlterClassDefinition = value; }
		}

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterDeviceScalarTypeMaps : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterDeviceScalarTypeMap))
				throw new LanguageException(LanguageException.Codes.AlterDeviceScalarTypeMapContainer);
			base.Validate(AItem);
		}
		
		public new AlterDeviceScalarTypeMap this[int AIndex]
		{
			get { return (AlterDeviceScalarTypeMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropDeviceScalarTypeMap : DeviceScalarTypeMapBase{}
	
	public class DropDeviceScalarTypeMaps : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropDeviceScalarTypeMap))
				throw new LanguageException(LanguageException.Codes.DropDeviceScalarTypeMapContainer);
			base.Validate(AItem);
		}
		
		public new DropDeviceScalarTypeMap this[int AIndex]
		{
			get { return (DropDeviceScalarTypeMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DeviceOperatorMapBase : DeviceMapItem
	{
		// OperatorSpecifier
		protected OperatorSpecifier FOperatorSpecifier;
		public OperatorSpecifier OperatorSpecifier
		{
			get { return FOperatorSpecifier; }
			set { FOperatorSpecifier = value; }
		}
	}
	
	public class DeviceOperatorMap : DeviceOperatorMapBase, IMetaData
	{
		// ClassDefinition		
		protected ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class DeviceOperatorMaps : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DeviceOperatorMap))
				throw new LanguageException(LanguageException.Codes.DeviceOperatorMapContainer);
			base.Validate(AItem);
		}
		
		public new DeviceOperatorMap this[int AIndex]
		{
			get { return (DeviceOperatorMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterDeviceOperatorMap : DeviceOperatorMapBase, IAlterMetaData
	{
		// AlterClassDefinition		
		protected AlterClassDefinition FAlterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return FAlterClassDefinition; }
			set { FAlterClassDefinition = value; }
		}

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}

	public class AlterDeviceOperatorMaps : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterDeviceOperatorMap))
				throw new LanguageException(LanguageException.Codes.AlterDeviceOperatorMapContainer);
			base.Validate(AItem);
		}
		
		public new AlterDeviceOperatorMap this[int AIndex]
		{
			get { return (AlterDeviceOperatorMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropDeviceOperatorMap : DeviceOperatorMapBase{}
	
	public class DropDeviceOperatorMaps : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropDeviceOperatorMap))
				throw new LanguageException(LanguageException.Codes.DropDeviceOperatorMapContainer);
			base.Validate(AItem);
		}
		
		public new DropDeviceOperatorMap this[int AIndex]
		{
			get { return (DropDeviceOperatorMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DeviceStoreDefinitionBase : DeviceMapItem
	{
		// StoreName
		protected string FStoreName;
		public string StoreName
		{
			get { return FStoreName; }
			set { FStoreName = value; }
		}
	}
	
	public class DeviceStoreDefinition : DeviceStoreDefinitionBase, IMetaData
	{
		// Expression
		private Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
		
		// ClusteredDefault
		private bool FClusteredDefault;
		public bool ClusteredDefault
		{
			get { return FClusteredDefault; }
			set { FClusteredDefault = value; }
		}

		// ClusteredIndexDefinition
		private IndexDefinition FClusteredIndexDefinition;
		public IndexDefinition ClusteredIndexDefinition
		{
			get { return FClusteredIndexDefinition; }
			set { FClusteredIndexDefinition = value; }
		}
		
		// IndexesDefault
		private bool FIndexesDefault;
		public bool IndexesDefault
		{
			get { return FIndexesDefault; }
			set { FIndexesDefault = value; }
		}

		// IndexDefinitions
		private IndexDefinitions FIndexDefinitions = new IndexDefinitions();
		public IndexDefinitions IndexDefinitions { get { return FIndexDefinitions; } }

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class DeviceStoreDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DeviceStoreDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "DeviceStoreDefinition");
			base.Validate(AItem);
		}
		
		public new DeviceStoreDefinition this[int AIndex]
		{
			get { return (DeviceStoreDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterDeviceStoreDefinition : DeviceStoreDefinitionBase, IAlterMetaData
	{
		// Expression
		private Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
		
		// ClusteredDefault
		private bool FClusteredDefault;
		public bool ClusteredDefault
		{
			get { return FClusteredDefault; }
			set { FClusteredDefault = value; }
		}

		// ClusteredIndexDefinition
		private IndexDefinition FClusteredIndexDefinition;
		public IndexDefinition ClusteredIndexDefinition
		{
			get { return FClusteredIndexDefinition; }
			set { FClusteredIndexDefinition = value; }
		}
		
		// IndexesDefault
		private bool FIndexesDefault;
		public bool IndexesDefault
		{
			get { return FIndexesDefault; }
			set { FIndexesDefault = value; }
		}

		// IndexDefinitions
		private IndexDefinitions FCreateIndexDefinitions = new IndexDefinitions();
		public IndexDefinitions CreateIndexDefinitions { get { return FCreateIndexDefinitions; } }

		// AlterIndexDefinitions
		private AlterIndexDefinitions FAlterIndexDefinitions = new AlterIndexDefinitions();
		public AlterIndexDefinitions AlterIndexDefinitions { get { return FAlterIndexDefinitions; } }

		// DropIndexDefinitions
		private DropIndexDefinitions FDropIndexDefinitions = new DropIndexDefinitions();
		public DropIndexDefinitions DropIndexDefinitions { get { return FDropIndexDefinitions; } }

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}

	public class AlterDeviceStoreDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterDeviceStoreDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "AlterDeviceStoreDefinition");
			base.Validate(AItem);
		}
		
		public new AlterDeviceStoreDefinition this[int AIndex]
		{
			get { return (AlterDeviceStoreDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropDeviceStoreDefinition : DeviceStoreDefinitionBase{}
	
	public class DropDeviceStoreDefinitions : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropDeviceStoreDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "DropDeviceStoreDefinition");
			base.Validate(AItem);
		}
		
		public new DropDeviceStoreDefinition this[int AIndex]
		{
			get { return (DropDeviceStoreDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public abstract class IndexDefinitionBase : D4Statement
	{
		// Columns
		protected IndexColumnDefinitions FColumns = new IndexColumnDefinitions();
		public IndexColumnDefinitions Columns { get { return FColumns; } }
	}
	
	public class IndexDefinition : IndexDefinitionBase, IMetaData
	{
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class IndexDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is IndexDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "IndexDefinition");
			base.Validate(AItem);
		}
		
		public new IndexDefinition this[int AIndex]
		{
			get { return (IndexDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class AlterIndexDefinition : IndexDefinitionBase, IAlterMetaData
	{
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterIndexDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterIndexDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "AlterIndexDefinition");
			base.Validate(AItem);
		}
		
		public new AlterIndexDefinition this[int AIndex]
		{
			get { return (AlterIndexDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class DropIndexDefinition : IndexDefinitionBase{}
	
	public class DropIndexDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropIndexDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "DropIndexDefinition");
			base.Validate(AItem);
		}
		
		public new DropIndexDefinition this[int AIndex]
		{
			get { return (DropIndexDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class IndexColumnDefinition : ColumnDefinitionBase
	{
		public IndexColumnDefinition() : base(){}
		public IndexColumnDefinition(string AColumnName, bool AAscending) : base(AColumnName)
		{
			FAscending = AAscending;
		}
		
		public IndexColumnDefinition(string AColumnName, bool AAscending, SortDefinition ASort) : base(AColumnName)
		{
			FAscending = AAscending;
			FSort = ASort;
		}
		
		protected bool FAscending = true;
		public bool Ascending
		{
			get { return FAscending; }
			set { FAscending = value; }
		}
		
		protected SortDefinition FSort;
		public SortDefinition Sort
		{
			get { return FSort; }
			set { FSort = value; }
		}
	}

	public class IndexColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is IndexColumnDefinition))
				throw new LanguageException(LanguageException.Codes.InvalidContainer, "IndexColumnDefinition");
			base.Validate(AItem);
		}
		
		public new IndexColumnDefinition this[int AIndex]
		{
			get { return (IndexColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
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
		private bool FReconcileModeSet;
		public bool ReconcileModeSet { get { return FReconcileModeSet; } }
		
		// ReconcileMode
		private ReconcileMode FReconcileMode;
		public ReconcileMode ReconcileMode
		{
			get { return FReconcileMode; }
			set 
			{
				FReconcileMode = value; 
				FReconcileModeSet = true;
			}
		}
		
		// ReconcileMasterSet
		private bool FReconcileMasterSet;
		public bool ReconcileMasterSet { get { return FReconcileMasterSet; } }
		
		// ReconcileMaster
		private ReconcileMaster FReconcileMaster;
		public ReconcileMaster ReconcileMaster
		{
			get { return FReconcileMaster; }
			set 
			{ 
				FReconcileMaster = value; 
				FReconcileMasterSet = true;
			}
		}
	}
	
	public class CreateDeviceStatement : D4Statement, IMetaData
	{
		// DeviceName
		protected string FDeviceName = String.Empty;
		public string DeviceName
		{
			get { return FDeviceName; }
			set { FDeviceName = value == null ? String.Empty : value; }
		}
		
		// DeviceScalarTypeMaps
		private DeviceScalarTypeMaps FDeviceScalarTypeMaps = new DeviceScalarTypeMaps();
		public DeviceScalarTypeMaps DeviceScalarTypeMaps { get { return FDeviceScalarTypeMaps; } }

		// DeviceOperatorMaps
		private DeviceOperatorMaps FDeviceOperatorMaps = new DeviceOperatorMaps();
		public DeviceOperatorMaps DeviceOperatorMaps { get { return FDeviceOperatorMaps; } }
		
		// DeviceStoreDefinitions
		private DeviceStoreDefinitions FDeviceStoreDefinitions = new DeviceStoreDefinitions();
		public DeviceStoreDefinitions DeviceStoreDefinitions { get { return FDeviceStoreDefinitions; } }
		
		// ReconciliationSettings
		private ReconciliationSettings FReconciliationSettings;
		public ReconciliationSettings ReconciliationSettings
		{
			get { return FReconciliationSettings; }
			set { FReconciliationSettings = value; }
		}
		
		// ClassDefinition		
		protected ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
		{
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class AlterDeviceStatement : D4Statement, IAlterMetaData
	{
		// DeviceName
		protected string FDeviceName = String.Empty;
		public string DeviceName
		{
			get { return FDeviceName; }
			set { FDeviceName = value == null ? String.Empty : value; }
		}
		
		// CreateDeviceScalarTypeMaps
		private DeviceScalarTypeMaps FCreateDeviceScalarTypeMaps = new DeviceScalarTypeMaps();
		public DeviceScalarTypeMaps CreateDeviceScalarTypeMaps { get { return FCreateDeviceScalarTypeMaps; } }

		// AlterDeviceScalarTypeMaps
		private AlterDeviceScalarTypeMaps FAlterDeviceScalarTypeMaps = new AlterDeviceScalarTypeMaps();
		public AlterDeviceScalarTypeMaps AlterDeviceScalarTypeMaps { get { return FAlterDeviceScalarTypeMaps; } }

		// DropDeviceScalarTypeMaps
		private DropDeviceScalarTypeMaps FDropDeviceScalarTypeMaps = new DropDeviceScalarTypeMaps();
		public DropDeviceScalarTypeMaps DropDeviceScalarTypeMaps { get { return FDropDeviceScalarTypeMaps; } }

		// CreateDeviceOperatorMaps
		private DeviceOperatorMaps FCreateDeviceOperatorMaps = new DeviceOperatorMaps();
		public DeviceOperatorMaps CreateDeviceOperatorMaps { get { return FCreateDeviceOperatorMaps; } }
		
		// AlterDeviceOperatorMaps
		private AlterDeviceOperatorMaps FAlterDeviceOperatorMaps = new AlterDeviceOperatorMaps();
		public AlterDeviceOperatorMaps AlterDeviceOperatorMaps { get { return FAlterDeviceOperatorMaps; } }

		// DropDeviceOperatorMaps
		private DropDeviceOperatorMaps FDropDeviceOperatorMaps = new DropDeviceOperatorMaps();
		public DropDeviceOperatorMaps DropDeviceOperatorMaps { get { return FDropDeviceOperatorMaps; } }

		// CreateDeviceStoreDefinitions
		private DeviceStoreDefinitions FCreateDeviceStoreDefinitions = new DeviceStoreDefinitions();
		public DeviceStoreDefinitions CreateDeviceStoreDefinitions { get { return FCreateDeviceStoreDefinitions; } }
		
		// AlterDeviceStoreDefinitions
		private AlterDeviceStoreDefinitions FAlterDeviceStoreDefinitions = new AlterDeviceStoreDefinitions();
		public AlterDeviceStoreDefinitions AlterDeviceStoreDefinitions { get { return FAlterDeviceStoreDefinitions; } }

		// DropDeviceStoreDefinitions
		private DropDeviceStoreDefinitions FDropDeviceStoreDefinitions = new DropDeviceStoreDefinitions();
		public DropDeviceStoreDefinitions DropDeviceStoreDefinitions { get { return FDropDeviceStoreDefinitions; } }

		// ReconciliationSettings
		private ReconciliationSettings FReconciliationSettings;
		public ReconciliationSettings ReconciliationSettings
		{
			get { return FReconciliationSettings; }
			set { FReconciliationSettings = value; }
		}
		
		// AlterClassDefinition		
		protected AlterClassDefinition FAlterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return FAlterClassDefinition; }
			set { FAlterClassDefinition = value; }
		}
        
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}

	public abstract class AlterTableVarStatement : D4Statement, IAlterMetaData
	{
		// TableVarName
		protected string FTableVarName = String.Empty;
		public string TableVarName
		{
			get { return FTableVarName; }
			set { FTableVarName = value == null ? String.Empty : value; }
		}
		
		// CreateKeys
		private KeyDefinitions FCreateKeys = new KeyDefinitions();
		public KeyDefinitions CreateKeys { get { return FCreateKeys; } }

		// AlterKeys
		private AlterKeyDefinitions FAlterKeys = new AlterKeyDefinitions();
		public AlterKeyDefinitions AlterKeys { get { return FAlterKeys; } }

		// DropKeys
		private DropKeyDefinitions FDropKeys = new DropKeyDefinitions();
		public DropKeyDefinitions DropKeys { get { return FDropKeys; } }

		// CreateOrders
		private OrderDefinitions FCreateOrders = new OrderDefinitions();
		public OrderDefinitions CreateOrders { get { return FCreateOrders; } }

		// AlterOrders
		private AlterOrderDefinitions FAlterOrders = new AlterOrderDefinitions();
		public AlterOrderDefinitions AlterOrders { get { return FAlterOrders; } }

		// DropOrders
		private DropOrderDefinitions FDropOrders = new DropOrderDefinitions();
		public DropOrderDefinitions DropOrders { get { return FDropOrders; } }

		// CreateReferences
		private ReferenceDefinitions FCreateReferences = new ReferenceDefinitions();
		public ReferenceDefinitions CreateReferences { get { return FCreateReferences; } }

		// AlterReferences
		private AlterReferenceDefinitions FAlterReferences = new AlterReferenceDefinitions();
		public AlterReferenceDefinitions AlterReferences { get { return FAlterReferences; } }

		// DropReferences
		private DropReferenceDefinitions FDropReferences = new DropReferenceDefinitions();
		public DropReferenceDefinitions DropReferences { get { return FDropReferences; } }

		// CreateConstraints
		private CreateConstraintDefinitions FCreateConstraints = new CreateConstraintDefinitions();
		public CreateConstraintDefinitions CreateConstraints { get { return FCreateConstraints; } }

		// AlterConstraints
		private AlterConstraintDefinitions FAlterConstraints = new AlterConstraintDefinitions();
		public AlterConstraintDefinitions AlterConstraints { get { return FAlterConstraints; } }

		// DropConstraints
		private DropConstraintDefinitions FDropConstraints = new DropConstraintDefinitions();
		public DropConstraintDefinitions DropConstraints { get { return FDropConstraints; } }

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterTableStatement : AlterTableVarStatement
	{
		// CreateColumns
		private ColumnDefinitions FCreateColumns = new ColumnDefinitions();
		public ColumnDefinitions CreateColumns { get { return FCreateColumns; } }

		// AlterColumns
		private AlterColumnDefinitions FAlterColumns = new AlterColumnDefinitions();
		public AlterColumnDefinitions AlterColumns { get { return FAlterColumns; } }

		// DropColumns
		private DropColumnDefinitions FDropColumns = new DropColumnDefinitions();
		public DropColumnDefinitions DropColumns { get { return FDropColumns; } }
	}
	
	public class AlterViewStatement : AlterTableVarStatement{}
	
	public class AlterScalarTypeStatement : D4Statement, IAlterMetaData
	{
		// ScalarTypeName
		protected string FScalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value == null ? String.Empty : value; }
		}

		// CreateRepresentations
		private RepresentationDefinitions FCreateRepresentations = new RepresentationDefinitions();
		public RepresentationDefinitions CreateRepresentations { get { return FCreateRepresentations; } }

		// AlterRepresentations
		private AlterRepresentationDefinitions FAlterRepresentations = new AlterRepresentationDefinitions();
		public AlterRepresentationDefinitions AlterRepresentations { get { return FAlterRepresentations; } }

		// DropRepresentations
		private DropRepresentationDefinitions FDropRepresentations = new DropRepresentationDefinitions();
		public DropRepresentationDefinitions DropRepresentations { get { return FDropRepresentations; } }

		// CreateConstraints
		private ConstraintDefinitions FCreateConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions CreateConstraints { get { return FCreateConstraints; } }

		// AlterConstraints
		private AlterConstraintDefinitions FAlterConstraints = new AlterConstraintDefinitions();
		public AlterConstraintDefinitions AlterConstraints { get { return FAlterConstraints; } }

		// DropConstraints
		private DropConstraintDefinitions FDropConstraints = new DropConstraintDefinitions();
		public DropConstraintDefinitions DropConstraints { get { return FDropConstraints; } }

		// CreateSpecials
		private SpecialDefinitions FCreateSpecials = new SpecialDefinitions();
		public SpecialDefinitions CreateSpecials { get { return FCreateSpecials; } }

		// AlterSpecials
		private AlterSpecialDefinitions FAlterSpecials = new AlterSpecialDefinitions();
		public AlterSpecialDefinitions AlterSpecials { get { return FAlterSpecials; } }

		// DropSpecials
		private DropSpecialDefinitions FDropSpecials = new DropSpecialDefinitions();
		public DropSpecialDefinitions DropSpecials { get { return FDropSpecials; } }
		
		// Default		
		protected DefaultDefinitionBase FDefault;
		public DefaultDefinitionBase Default
		{
			get { return FDefault; }
			set { FDefault = value; }
		}
		
		// AlterClassDefinition		
		protected AlterClassDefinition FAlterClassDefinition;
		public AlterClassDefinition AlterClassDefinition
		{
			get { return FAlterClassDefinition; }
			set { FAlterClassDefinition = value; }
		}
        
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	#if USENOTIFYLISTFORMETADATA
	public class ColumnDefinitionBase : D4Statement, INotifyListItem
	#else
	public class ColumnDefinitionBase : D4Statement
	#endif
	{
		public ColumnDefinitionBase() : base(){}
		public ColumnDefinitionBase(string AColumnName) : base()
		{
			FColumnName = AColumnName;
		}
		
		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set 
			{ 
				if (FColumnName != value)
				{
					FColumnName = value == null ? String.Empty : value; 
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
		
		#if USENOTIFYLISTFORMETADATA
		public event ListItemEventHandler OnChanged;
		protected void Changed()
		{
			if (OnChanged != null)
				OnChanged(this);
		}
		#endif
	}
	
	public class ColumnDefinition : ColumnDefinitionBase, IMetaData
	{
		public ColumnDefinition() : base(){}
		public ColumnDefinition(string AColumnName, TypeSpecifier ATypeSpecifier) : base(AColumnName)
		{
			FTypeSpecifier = ATypeSpecifier;
		}
		
		// TypeSpecifier
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
		
		// Default		
		protected DefaultDefinition FDefault;
		public DefaultDefinition Default
		{
			get { return FDefault; }
			set { FDefault = value; }
		}

		// Constraints
		protected ConstraintDefinitions FConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions Constraints
		{
			get { return FConstraints; }
			set { FConstraints = value; }
		}
		
		// IsNilable
		protected bool FIsNilable;
		public bool IsNilable
		{
			get { return FIsNilable; }
			set { FIsNilable = value; } 
		}
		
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class ColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is ColumnDefinition))
				throw new LanguageException(LanguageException.Codes.ColumnDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new ColumnDefinition this[int AIndex]
		{
			get { return (ColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class AlterColumnDefinition : ColumnDefinitionBase, IAlterMetaData
	{
		// TypeSpecifier
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
		
		// Default		
		protected DefaultDefinitionBase FDefault;
		public DefaultDefinitionBase Default
		{
			get { return FDefault; }
			set { FDefault = value; }
		}

		// CreateConstraints
		private ConstraintDefinitions FCreateConstraints = new ConstraintDefinitions();
		public ConstraintDefinitions CreateConstraints { get { return FCreateConstraints; } }

		// AlterConstraints
		private AlterConstraintDefinitions FAlterConstraints = new AlterConstraintDefinitions();
		public AlterConstraintDefinitions AlterConstraints { get { return FAlterConstraints; } }

		// DropConstraints
		private DropConstraintDefinitions FDropConstraints = new DropConstraintDefinitions();
		public DropConstraintDefinitions DropConstraints { get { return FDropConstraints; } }
		
		// ChangeNilable
		private bool FChangeNilable;
		public bool ChangeNilable
		{
			get { return FChangeNilable; }
			set { FChangeNilable = value; }
		}
		
		// IsNilable
		private bool FIsNilable;
		public bool IsNilable
		{
			get { return FIsNilable; }
			set { FIsNilable = value; }
		}

        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterColumnDefinition))
				throw new LanguageException(LanguageException.Codes.AlterColumnDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterColumnDefinition this[int AIndex]
		{
			get { return (AlterColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class DropColumnDefinition : ColumnDefinitionBase
	{
		public DropColumnDefinition() : base() { }
		public DropColumnDefinition(string AColumnName) : base(AColumnName) { }
	}
	
	public class DropColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropColumnDefinition))
				throw new LanguageException(LanguageException.Codes.DropColumnDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropColumnDefinition this[int AIndex]
		{
			get { return (DropColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class KeyColumnDefinition : ColumnDefinitionBase
	{
		public KeyColumnDefinition() : base(){}
		public KeyColumnDefinition(string AColumnName) : base(AColumnName){}
	}

	public class KeyColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is KeyColumnDefinition))
				throw new LanguageException(LanguageException.Codes.KeyColumnDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new KeyColumnDefinition this[int AIndex]
		{
			get { return (KeyColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class ReferenceColumnDefinition : ColumnDefinitionBase
	{
		public ReferenceColumnDefinition() : base(){}
		public ReferenceColumnDefinition(string AColumnName) : base(AColumnName){}
	}

	public class ReferenceColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is ReferenceColumnDefinition))
				throw new LanguageException(LanguageException.Codes.ReferenceColumnDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new ReferenceColumnDefinition this[int AIndex]
		{
			get { return (ReferenceColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class OrderColumnDefinition : ColumnDefinitionBase
	{
		public OrderColumnDefinition() : base(){}
		public OrderColumnDefinition(string AColumnName, bool AAscending) : base(AColumnName)
		{
			FAscending = AAscending;
		}
		
		public OrderColumnDefinition(string AColumnName, bool AAscending, SortDefinition ASort) : base(AColumnName)
		{
			FAscending = AAscending;
			FSort = ASort;
		}
		
		public OrderColumnDefinition(string AColumnName, bool AAscending, bool AIncludeNils) : base(AColumnName)
		{
			FAscending = AAscending;
			FIncludeNils = AIncludeNils;
		}
		
		public OrderColumnDefinition(string AColumnName, bool AAscending, bool AIncludeNils, SortDefinition ASort) : base(AColumnName)
		{
			FAscending = AAscending;
			FIncludeNils = AIncludeNils;
			FSort = ASort;
		}
		
		protected bool FAscending = true;
		public bool Ascending
		{
			get { return FAscending; }
			set 
			{ 
				if (FAscending != value)
				{
					FAscending = value; 
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
		
		protected bool FIncludeNils = false;
		public bool IncludeNils
		{
			get { return FIncludeNils; }
			set 
			{ 
				if (FIncludeNils != value)
				{
					FIncludeNils = value; 
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
		
		protected SortDefinition FSort;
		public SortDefinition Sort
		{
			get { return FSort; }
			set
			{
				if (FSort != value)
				{
					FSort = value;
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
	}

	public class OrderColumnDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is OrderColumnDefinition))
				throw new LanguageException(LanguageException.Codes.OrderColumnDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new OrderColumnDefinition this[int AIndex]
		{
			get { return (OrderColumnDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public abstract class KeyDefinitionBase : D4Statement
	{
		// Columns
		protected KeyColumnDefinitions FColumns = new KeyColumnDefinitions();
		public KeyColumnDefinitions Columns {  get { return FColumns; } }
	}

    [Editor("Alphora.Dataphor.DAE.Client.Controls.Design.KeyEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
	public class KeyDefinition : KeyDefinitionBase, IMetaData
	{
        // MetaData
        protected MetaData FMetaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.KeyDefinitionsEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
	public class KeyDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is KeyDefinition))
				throw new LanguageException(LanguageException.Codes.KeyDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new KeyDefinition this[int AIndex]
		{
			get { return (KeyDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterKeyDefinition : KeyDefinitionBase, IAlterMetaData
	{
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterKeyDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterKeyDefinition))
				throw new LanguageException(LanguageException.Codes.AlterKeyDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterKeyDefinition this[int AIndex]
		{
			get { return (AlterKeyDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropKeyDefinition : KeyDefinitionBase{}
	
	public class DropKeyDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropKeyDefinition))
				throw new LanguageException(LanguageException.Codes.DropKeyDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropKeyDefinition this[int AIndex]
		{
			get { return (DropKeyDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public abstract class ReferenceDefinitionBase : D4Statement
	{
		// ReferenceName
		protected string FReferenceName = String.Empty;
		public string ReferenceName
		{
			get { return FReferenceName; }
			set { FReferenceName = value == null ? String.Empty : value; }
		}
	}
    
	public class ReferenceDefinition : ReferenceDefinitionBase, IMetaData
	{
		// constructor
		public ReferenceDefinition() : base()
		{
			FColumns = new ReferenceColumnDefinitions();
		}
		
		// Columns
		protected ReferenceColumnDefinitions FColumns;
		public ReferenceColumnDefinitions Columns { get { return FColumns; } }

		// ReferencesDefinition		
		protected ReferencesDefinition FReferencesDefinition;
		public ReferencesDefinition ReferencesDefinition
		{
			get { return FReferencesDefinition; }
			set { FReferencesDefinition = value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class ReferenceDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is ReferenceDefinition))
				throw new LanguageException(LanguageException.Codes.ReferenceDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new ReferenceDefinition this[int AIndex]
		{
			get { return (ReferenceDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterReferenceDefinition : ReferenceDefinitionBase, IAlterMetaData
	{
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterReferenceDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterReferenceDefinition))
				throw new LanguageException(LanguageException.Codes.AlterReferenceDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterReferenceDefinition this[int AIndex]
		{
			get { return (AlterReferenceDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropReferenceDefinition : ReferenceDefinitionBase{}
	
	public class DropReferenceDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropReferenceDefinition))
				throw new LanguageException(LanguageException.Codes.DropReferenceDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropReferenceDefinition this[int AIndex]
		{
			get { return (DropReferenceDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public enum ReferenceAction {Require, Cascade, Clear, Set}
    
	public class ReferencesDefinition : D4Statement
	{
		// constructor
		public ReferencesDefinition() : base()
		{
			FColumns = new ReferenceColumnDefinitions();
		}
		
		// Columns
		protected ReferenceColumnDefinitions FColumns;
		public ReferenceColumnDefinitions Columns { get { return FColumns; } }

		// TableVarName
		protected string FTableVarName = String.Empty;
		public string TableVarName
		{
			get { return FTableVarName; }
			set { FTableVarName = value == null ? String.Empty : value; }
		}
		
		// UpdateReferenceAction
		protected ReferenceAction FUpdateReferenceAction;
		public ReferenceAction UpdateReferenceAction
		{
			get { return FUpdateReferenceAction; }
			set { FUpdateReferenceAction = value; }
		}
		
		// UpdateReferenceExpressions
		protected Expressions FUpdateReferenceExpressions = new Expressions();
		public Expressions UpdateReferenceExpressions { get { return FUpdateReferenceExpressions; } }
		
		// DeleteReferenceAction
		protected ReferenceAction FDeleteReferenceAction;
		public ReferenceAction DeleteReferenceAction
		{
			get { return FDeleteReferenceAction; }
			set { FDeleteReferenceAction = value; }
		}

		// DeleteReferenceExpressions
		protected Expressions FDeleteReferenceExpressions = new Expressions();
		public Expressions DeleteReferenceExpressions { get { return FDeleteReferenceExpressions; } }
	}
	
	public abstract class OrderDefinitionBase : D4Statement
	{
		// Columns
		protected OrderColumnDefinitions FColumns = new OrderColumnDefinitions();
		public OrderColumnDefinitions Columns { get { return FColumns; } }
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.OrderEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
	public class OrderDefinition : OrderDefinitionBase, IMetaData
	{
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.OrderDefinitionsEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
	public class OrderDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is OrderDefinition))
				throw new LanguageException(LanguageException.Codes.OrderDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new OrderDefinition this[int AIndex]
		{
			get { return (OrderDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class AlterOrderDefinition : OrderDefinitionBase, IAlterMetaData
	{
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterOrderDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterOrderDefinition))
				throw new LanguageException(LanguageException.Codes.AlterOrderDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterOrderDefinition this[int AIndex]
		{
			get { return (AlterOrderDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class DropOrderDefinition : OrderDefinitionBase{}
	
	public class DropOrderDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropOrderDefinition))
				throw new LanguageException(LanguageException.Codes.DropOrderDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropOrderDefinition this[int AIndex]
		{
			get { return (DropOrderDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	#if USENOTIFYLISTFORMETADATA
	public abstract class ConstraintDefinitionBase : D4Statement, INotifyListItem
	#else
	public abstract class ConstraintDefinitionBase : D4Statement
	#endif
	{
		// ConstraintName
		protected string FConstraintName = String.Empty;
		public string ConstraintName
		{
			get { return FConstraintName; }
			set 
			{ 
				if (FConstraintName != value)
				{
					FConstraintName = value == null ? String.Empty : value; 
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
		
		#if USENOTIFYLISTFORMETADATA
		public event ListItemEventHandler OnChanged;
		protected void Changed()
		{
			if (OnChanged != null)
				OnChanged(this);
		}
		#endif
	}
	
	public class CreateConstraintDefinition : ConstraintDefinitionBase, IMetaData
	{
		public CreateConstraintDefinition() : base() {}
		public CreateConstraintDefinition(string AName, MetaData AMetaData)
		{
			FConstraintName = AName;
			FMetaData = AMetaData;
			#if USENOTIFYLISTFORMETADATA
			if (FMetaData != null)
				FMetaData.OnChanged += new ListItemEventHandler(ItemChanged);
			#endif
		}

		private bool FIsGenerated;
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
        public MetaData MetaData
        {
			get { return FMetaData; }
			set 
			{ 
				if (FMetaData != value)
				{
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged -= new ListItemEventHandler(ItemChanged);
					#endif
					FMetaData = value; 
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged += new ListItemEventHandler(ItemChanged);
					Changed();
					#endif
				}
			}
        }
        
		#if USENOTIFYLISTFORMETADATA
        private void ItemChanged(object ASender)
        {
			Changed();
		}
		#endif
	}
	
	public class ConstraintDefinition : CreateConstraintDefinition
	{
		public ConstraintDefinition() : base(){}
		public ConstraintDefinition(string AName, Expression AExpression, MetaData AMetaData) : base(AName, AMetaData)
		{
			FExpression = AExpression;
		}
		
		// Expression		
		protected Expression FExpression;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Expression Expression
		{
			get { return FExpression; }
			set 
			{ 
				if (FExpression != value)
				{
					FExpression = value;
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}

		private string ExpressionToString(Expression AExpression)
		{
			if (AExpression == null)
				return String.Empty;
			else
			{
				D4TextEmitter LEmitter = new D4TextEmitter();
				return LEmitter.Emit(AExpression);
			}
		}

		private Expression StringToExpression(string AValue)
		{
			Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
			return LParser.ParseExpression(AValue);
		}

		[Description("D4 constraint expression.")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.D4ExpressionEmitEdit,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		public string ExpressionString
		{
			get { return FExpression != null ? ExpressionToString(FExpression) : String.Empty; }

			set
			{
				if (ExpressionString != value)
					Expression = value == String.Empty ? null : StringToExpression(value);
			}
		}
   	}
	
	[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.ConstraintsEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
	public class ConstraintDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is ConstraintDefinition))
				throw new LanguageException(LanguageException.Codes.ConstraintDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new ConstraintDefinition this[int AIndex]
		{
			get { return (ConstraintDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Compare(AName, this[LIndex].ConstraintName) == 0)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
    
	public class CreateConstraintDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is CreateConstraintDefinition))
				throw new LanguageException(LanguageException.Codes.ConstraintDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new CreateConstraintDefinition this[int AIndex]
		{
			get { return (CreateConstraintDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class CreateConstraintStatement : ConstraintDefinition
	{
		// IsSession
		protected bool FIsSession = false;
		public bool IsSession
		{
			get { return FIsSession; }
			set { FIsSession = value; }
		}
	}

	public class TransitionConstraintDefinition : CreateConstraintDefinition
	{
		public TransitionConstraintDefinition() : base() {}
		
		// OnInsertExpression		
		protected Expression FOnInsertExpression;
		public Expression OnInsertExpression
		{
			get { return FOnInsertExpression; }
			set 
			{ 
				if (FOnInsertExpression != value)
				{
					FOnInsertExpression = value;
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}

		// OnUpdateExpression		
		protected Expression FOnUpdateExpression;
		public Expression OnUpdateExpression
		{
			get { return FOnUpdateExpression; }
			set 
			{ 
				if (FOnUpdateExpression != value)
				{
					FOnUpdateExpression = value;
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}

		// OnDeleteExpression		
		protected Expression FOnDeleteExpression;
		public Expression OnDeleteExpression
		{
			get { return FOnDeleteExpression; }
			set 
			{ 
				if (FOnDeleteExpression != value)
				{
					FOnDeleteExpression = value;
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
	}
	
	public class AlterConstraintDefinitionBase : ConstraintDefinitionBase, IAlterMetaData
	{
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class AlterConstraintDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is AlterConstraintDefinitionBase))
				throw new LanguageException(LanguageException.Codes.AlterConstraintDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new AlterConstraintDefinitionBase this[int AIndex]
		{
			get { return (AlterConstraintDefinitionBase)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class AlterConstraintDefinition : AlterConstraintDefinitionBase
	{
		// Expression		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class AlterConstraintStatement : AlterConstraintDefinition {}
	
	public class AlterTransitionConstraintDefinitionItemBase : D4Statement {}
	
	public class AlterTransitionConstraintDefinitionCreateItem : AlterTransitionConstraintDefinitionItemBase
	{
		public AlterTransitionConstraintDefinitionCreateItem() : base() {}
		public AlterTransitionConstraintDefinitionCreateItem(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		// Expression		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class AlterTransitionConstraintDefinitionAlterItem : AlterTransitionConstraintDefinitionItemBase
	{
		public AlterTransitionConstraintDefinitionAlterItem() : base() {}
		public AlterTransitionConstraintDefinitionAlterItem(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		// Expression		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class AlterTransitionConstraintDefinitionDropItem : AlterTransitionConstraintDefinitionItemBase {}
	
	public class AlterTransitionConstraintDefinition : AlterConstraintDefinitionBase
	{
		private AlterTransitionConstraintDefinitionItemBase FOnInsert;
		public AlterTransitionConstraintDefinitionItemBase OnInsert
		{
			get { return FOnInsert; }
			set { FOnInsert = value; }
		}
		
		private AlterTransitionConstraintDefinitionItemBase FOnUpdate;
		public AlterTransitionConstraintDefinitionItemBase OnUpdate
		{
			get { return FOnUpdate; }
			set { FOnUpdate = value; }
		}
		
		private AlterTransitionConstraintDefinitionItemBase FOnDelete;
		public AlterTransitionConstraintDefinitionItemBase OnDelete
		{
			get { return FOnDelete; }
			set { FOnDelete = value; }
		}
	}
    
	public class DropConstraintDefinition : ConstraintDefinitionBase
	{
		public DropConstraintDefinition() : base(){}
		public DropConstraintDefinition(string AConstraintName) : base()
		{
			ConstraintName = AConstraintName;
		}

		// IsTransition
		protected bool FIsTransition;
		public bool IsTransition
		{
			get { return FIsTransition; }
			set { FIsTransition = value; }
		}
	}

	public class DropConstraintDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is DropConstraintDefinition))
				throw new LanguageException(LanguageException.Codes.DropConstraintDefinitionContainer);
			base.Validate(AItem);
		}
		
		public new DropConstraintDefinition this[int AIndex]
		{
			get { return (DropConstraintDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class DropConstraintStatement : DropConstraintDefinition {}
	
	public class CreateConversionStatement : D4Statement, IMetaData
	{
		public CreateConversionStatement() : base() {}
		
		private TypeSpecifier FSourceScalarTypeName;
		public TypeSpecifier SourceScalarTypeName
		{
			get { return FSourceScalarTypeName; }
			set { FSourceScalarTypeName = value; }
		}
		
		private TypeSpecifier FTargetScalarTypeName;
		public TypeSpecifier TargetScalarTypeName
		{
			get { return FTargetScalarTypeName; }
			set { FTargetScalarTypeName = value; }
		}
		
		private IdentifierExpression FOperatorName;
		public IdentifierExpression OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value; }
		}
		
		private bool FIsNarrowing = true;
		public bool IsNarrowing
		{
			get { return FIsNarrowing; }
			set { FIsNarrowing = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set 
			{ 
				if (FMetaData != value)
				{
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged -= new ListItemEventHandler(ItemChanged);
					#endif
					FMetaData = value; 
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged += new ListItemEventHandler(ItemChanged);
					Changed();
					#endif
				}
			}
        }
        
		#if USENOTIFYLISTFORMETADATA
        private void ItemChanged(object ASender)
        {
			Changed();
		}

        public event ListItemEventHandler OnChanged;
        private void Changed()
        {
			if (OnChanged != null)
				OnChanged(this);
        }
		#endif
	}
	
	public class DropConversionStatement : D4Statement
	{
		public DropConversionStatement() : base() {}
		
		private TypeSpecifier FSourceScalarTypeName;
		public TypeSpecifier SourceScalarTypeName
		{
			get { return FSourceScalarTypeName; }
			set { FSourceScalarTypeName = value; }
		}
		
		private TypeSpecifier FTargetScalarTypeName;
		public TypeSpecifier TargetScalarTypeName
		{
			get { return FTargetScalarTypeName; }
			set { FTargetScalarTypeName = value; }
		}
	}
	
	public class CreateRoleStatement : D4Statement, IMetaData
	{
		// RoleName
		protected string FRoleName;
		public string RoleName
		{
			get { return FRoleName; }
			set { FRoleName = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class AlterRoleStatement : D4Statement, IAlterMetaData
	{
		// RoleName
		protected string FRoleName;
		public string RoleName
		{
			get { return FRoleName; }
			set { FRoleName = value; }
		}

		protected AlterMetaData FAlterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
		}
	}
	
	public class DropRoleStatement : D4Statement
	{
		// RoleName
		protected string FRoleName;
		public string RoleName
		{
			get { return FRoleName; }
			set { FRoleName = value; }
		}
	}
	
	public class CreateRightStatement : D4Statement
	{
		// RightName
		protected string FRightName;
		public string RightName
		{
			get { return FRightName; }
			set { FRightName = value; }
		}
	}
	
	public class DropRightStatement : D4Statement
	{
		// RightName
		protected string FRightName;
		public string RightName
		{
			get { return FRightName; }
			set { FRightName = value; }
		}
	}
	
	public abstract class SortDefinitionBase : D4Statement {}
	
	public class SortDefinition : SortDefinitionBase, IMetaData
	{
		public SortDefinition() : base() {}
		public SortDefinition(Expression AExpression)
		{
			FExpression = AExpression;
		}
		
		// Expression
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
        
        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class CreateSortStatement : SortDefinition
	{
		public CreateSortStatement() : base() {}
		public CreateSortStatement(string AScalarTypeName, Expression AExpression) : base(AExpression)
		{
			FScalarTypeName = AScalarTypeName;
		}
		
		// ScalarTypeName
		protected string FScalarTypeName;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value; }
		}
	}
	
	public class AlterSortDefinition : SortDefinitionBase, IAlterMetaData
	{
		public AlterSortDefinition() : base() {}
		public AlterSortDefinition(Expression AExpression)
		{
			FExpression = AExpression;
		}
		
		// Expression
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}

		protected AlterMetaData FAlterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
		}
	}

	public class AlterSortStatement : AlterSortDefinition
	{
		public AlterSortStatement() : base() {}
		public AlterSortStatement(string AScalarTypeName, Expression AExpression) : base(AExpression)
		{
			FScalarTypeName = AScalarTypeName;
		}
		
		// ScalarTypeName
		protected string FScalarTypeName;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value; }
		}
	}
	
	public class DropSortDefinition : SortDefinitionBase {}
	
	public class DropSortStatement : DropSortDefinition
	{
		public DropSortStatement() : base() {}
		public DropSortStatement(string AScalarTypeName) : base()
		{
			FScalarTypeName = AScalarTypeName;
		}
		
		public DropSortStatement(string AScalarTypeName, bool AIsUnique) : base()
		{
			FScalarTypeName = AScalarTypeName;
			FIsUnique = AIsUnique;
		}
		
		// ScalarTypeName
		protected string FScalarTypeName;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value; }
		}
		
		private bool FIsUnique;
		public bool IsUnique
		{
			get { return FIsUnique; }
			set { FIsUnique = value; }
		}
	}

	public abstract class DefaultDefinitionBase : D4Statement {}

	#if USENOTIFYLISTFORMETADATA
	public class DefaultDefinition : DefaultDefinitionBase, IMetaData, INotifyListItem
	#else
	public class DefaultDefinition : DefaultDefinitionBase, IMetaData
	#endif
	{
		public DefaultDefinition() : base(){}
		public DefaultDefinition(Expression AExpression, MetaData AMetaData)
		{
			FExpression = AExpression;
			FMetaData = AMetaData;
			#if USENOTIFYLISTFORMETADATA
			if (FMetaData != null)
				FMetaData.OnChanged += new ListItemEventHandler(ItemChanged);
			#endif
		}
		
		// Expression		
		protected Expression FExpression;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public Expression Expression
		{
			get { return FExpression; }
			set 
			{ 
				if (FExpression != value)
				{
					FExpression = value; 
					#if USENOTIFYLISTFORMETADATA
					Changed();
					#endif
				}
			}
		}
		
		private bool FIsGenerated;
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}
        
		private string ExpressionToString(Expression AExpression)
		{
			if (AExpression == null)
				return String.Empty;
			else
			{
				D4TextEmitter LEmitter = new D4TextEmitter();
				return LEmitter.Emit(AExpression);
			}
		}

		private Expression StringToExpression(string AValue)
		{
			Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
			return LParser.ParseExpression(AValue);
		}

		[Description("Default expression. For example value = 0")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.D4ExpressionEmitEdit,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
		public string ExpressionString
		{
			get { return FExpression != null ? ExpressionToString(FExpression) : String.Empty; }

			set
			{
				if (ExpressionString != value)
					Expression = value == String.Empty ? null : StringToExpression(value);
			}
		}
        
        // MetaData
        protected MetaData FMetaData;
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MetaDataEditor,Alphora.Dataphor.DAE.Client.Controls",typeof(System.Drawing.Design.UITypeEditor))]
		public MetaData MetaData
        {
			get { return FMetaData; }
			set 
			{ 
				if (FMetaData != value)
				{
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged -= new ListItemEventHandler(ItemChanged);
					#endif
					FMetaData = value; 
					#if USENOTIFYLISTFORMETADATA
					if (FMetaData != null)
						FMetaData.OnChanged += new ListItemEventHandler(ItemChanged);
					Changed();
					#endif
				}
			}
        }
        
		#if USENOTIFYLISTFORMETADATA
        public event ListItemEventHandler OnChanged;
        private void Changed()
        {
			if (OnChanged != null)
				OnChanged(this);
        }
        
        private void ItemChanged(object ASender)
        {
			Changed();
        }
        #endif
	}

	public class AlterDefaultDefinition : DefaultDefinitionBase, IAlterMetaData
	{
		// Expression		
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
        
        // AlterMetaData
        protected AlterMetaData FAlterMetaData;
        public AlterMetaData AlterMetaData
        {
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
        }
	}
	
	public class DropDefaultDefinition : DefaultDefinitionBase{}
	
	public class ClassDefinition : D4Statement, ICloneable
	{
		// constructor
		public ClassDefinition() : base(){}
		public ClassDefinition(string AClassName) : base()
		{
			ClassName = AClassName;
		}
		
		public ClassDefinition(string AClassName, ClassAttributeDefinition[] AAttributes) : base()
		{
			ClassName = AClassName;
			FAttributes.AddRange(AAttributes);
		}
		
		// ClassName
		protected string FClassName = String.Empty;
		public string ClassName
		{
			get { return FClassName; }
			set 
			{ 
				if (FClassName != value)
					FClassName = value == null ? String.Empty : value;
			}
		}

		// Attributes
		protected ClassAttributeDefinitions FAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions Attributes { get { return FAttributes; } }
		
		public virtual object Clone()
		{
			ClassDefinition LClassDefinition = new ClassDefinition(FClassName);
			foreach (ClassAttributeDefinition LAttribute in FAttributes)
				LClassDefinition.Attributes.Add(LAttribute.Clone());
			return LClassDefinition;
		}
	}
	
	public class AlterClassDefinition : D4Statement
	{
		private string FClassName = string.Empty;
		public string ClassName
		{
			get { return FClassName; }
			set { FClassName = value == null ? String.Empty : value; }
		}
		
		private ClassAttributeDefinitions FCreateAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions CreateAttributes { get { return FCreateAttributes; } }

		private ClassAttributeDefinitions FAlterAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions AlterAttributes { get { return FAlterAttributes; } }

		private ClassAttributeDefinitions FDropAttributes = new ClassAttributeDefinitions();
		public ClassAttributeDefinitions DropAttributes { get { return FDropAttributes; } }
	}
	
	public class ClassAttributeDefinitions : Statements
	{
		protected override void Validate(object AItem)
		{
			ClassAttributeDefinition LAttribute = AItem as ClassAttributeDefinition;
			if (LAttribute == null)
				throw new LanguageException(LanguageException.Codes.ClassAttributeDefinitionContainer);
			if (IndexOf(LAttribute.AttributeName) >= 0)
				throw new LanguageException(LanguageException.Codes.DuplicateAttributeDefinition, LAttribute.AttributeName);
			base.Validate(AItem);
		}
		
		public new ClassAttributeDefinition this[int AIndex]
		{
			get { return (ClassAttributeDefinition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public ClassAttributeDefinition this[string AName]
		{
			get
			{
				int LIndex = IndexOf(AName);
				if (LIndex >= 0)
					return this[LIndex];
				else
					throw new LanguageException(LanguageException.Codes.ClassAttributeNotFound, AName);
			}
			set
			{
				int LIndex = IndexOf(AName);
				if (LIndex >= 0)
					this[LIndex] = value;
				else
					throw new LanguageException(LanguageException.Codes.ClassAttributeNotFound, AName);
			}
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (String.Compare(this[LIndex].AttributeName, AName, true) == 0)
					return LIndex;
			return -1;
		}
	}
    
	public class ClassAttributeDefinition : D4Statement, ICloneable
	{
		// constructor
		public ClassAttributeDefinition() : base(){}
		public ClassAttributeDefinition(string AName, string AValue)
		{
			AttributeName = AName;
			AttributeValue = AValue;
		}

		// AttributeName
		protected string FAttributeName = String.Empty;
		public string AttributeName
		{
			get { return FAttributeName; }
			set { FAttributeName = value == null ? String.Empty : value; }
		}
		
		// AttributeValue
		protected string FAttributeValue = String.Empty;
		public string AttributeValue
		{
			get { return FAttributeValue; }
			set { FAttributeValue = value == null ? String.Empty : value; }
		}
		
		public virtual object Clone()
		{
			ClassAttributeDefinition LAttribute = new ClassAttributeDefinition();
			LAttribute.AttributeName = FAttributeName;
			LAttribute.AttributeValue = FAttributeValue;
			return LAttribute;
		}
	}

	public class NamedTypeSpecifiers : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is NamedTypeSpecifier))
				throw new LanguageException(LanguageException.Codes.NamedTypeSpecifierContainer);
			base.Validate(AItem);
		}
		
		public new NamedTypeSpecifier this[int AIndex]
		{
			get { return (NamedTypeSpecifier)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class NamedTypeSpecifier : D4Statement
	{
		// Identifier
		protected string FIdentifier = String.Empty;
		public string Identifier
		{
			get { return FIdentifier; }
			set { FIdentifier = value == null ? String.Empty : value; }
		}
		
		// TypeSpecifier		
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
	}
	
	public class FormalParameters : NamedTypeSpecifiers
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is FormalParameter))
				throw new LanguageException(LanguageException.Codes.FormalParameterContainer);
			base.Validate(AItem);
		}
		
		public new FormalParameter this[int AIndex]
		{
			get { return (FormalParameter)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class FormalParameter : NamedTypeSpecifier
	{
		protected Modifier FModifier;
		public Modifier Modifier
		{
			get { return FModifier; }
			set { FModifier = value; }
		}
	}
	
	public abstract class TypeSpecifier : D4Statement
	{
		// IsGeneric
		private bool FIsGeneric;
		public bool IsGeneric
		{
			get { return FIsGeneric; }
			set { FIsGeneric = value; }
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
		public ScalarTypeSpecifier(string AScalarTypeName) : base()
		{
			FScalarTypeName = AScalarTypeName;
		}
		
		// ScalarTypeName
		protected string FScalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	public class RowTypeSpecifier : TypeSpecifier
	{
		// constructor
		public RowTypeSpecifier() : base()
		{
			FColumns = new NamedTypeSpecifiers();
		}
		
		// Columns
		protected NamedTypeSpecifiers FColumns;
		public NamedTypeSpecifiers Columns { get { return FColumns; } }
	}
	
	public class TableTypeSpecifier : TypeSpecifier
	{
		// constructor
		public TableTypeSpecifier() : base()
		{
			FColumns = new NamedTypeSpecifiers();
		}

		// Columns
		protected NamedTypeSpecifiers FColumns;
		public NamedTypeSpecifiers Columns { get { return FColumns; } }
	}
	
	public class TypeOfTypeSpecifier : TypeSpecifier
	{
		public TypeOfTypeSpecifier() : base(){}
		public TypeOfTypeSpecifier(Expression AExpression) : base()
		{
			FExpression = AExpression;
		}
		
		// Expression
		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
	}
	
	public class ListTypeSpecifier : TypeSpecifier
	{
		public ListTypeSpecifier() : base(){}
		public ListTypeSpecifier(TypeSpecifier ATypeSpecifier) : base()
		{
			FTypeSpecifier = ATypeSpecifier;
		}

		// TypeSpecifier		
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
	}
	
	public class FormalParameterSpecifier : D4Statement
	{
		public FormalParameterSpecifier() : base(){}
		public FormalParameterSpecifier(Modifier AModifier, TypeSpecifier ATypeSpecifier) : base()
		{
			FModifier = AModifier;
			FTypeSpecifier = ATypeSpecifier;
		}
		
		protected Modifier FModifier;
		public Modifier Modifier
		{
			get { return FModifier; }
			set { FModifier = value; }
		}
		
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
	}
	
	public class FormalParameterSpecifiers : Statements
	{										
		protected override void Validate(object AItem)
		{
			if (!(AItem is FormalParameterSpecifier))
				throw new LanguageException(LanguageException.Codes.FormalParameterSpecifierContainer);
			base.Validate(AItem);
		}
		
		public new FormalParameterSpecifier this[int AIndex]
		{
			get { return (FormalParameterSpecifier)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class OperatorTypeSpecifier : TypeSpecifier
	{
		protected FormalParameterSpecifiers FTypeSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers TypeSpecifiers { get { return FTypeSpecifiers; } }
	}

	public class CursorTypeSpecifier : TypeSpecifier
	{
		public CursorTypeSpecifier() : base(){}
		public CursorTypeSpecifier(TypeSpecifier ATypeSpecifier) : base()
		{
			FTypeSpecifier = ATypeSpecifier;
		}

		// TypeSpecifier		
		protected TypeSpecifier FTypeSpecifier;
		public TypeSpecifier TypeSpecifier
		{
			get { return FTypeSpecifier; }
			set { FTypeSpecifier = value; }
		}
	}

	public abstract class DropObjectStatement : D4Statement
	{
		public DropObjectStatement() : base(){}
		public DropObjectStatement(string AObjectName)
		{
			FObjectName = AObjectName;
		}
		
		// ObjectName
		protected string FObjectName = String.Empty;
		public string ObjectName
		{
			get { return FObjectName; }
			set { FObjectName = value == null ? String.Empty : value; }
		}
	}

	public class DropTableStatement : DropObjectStatement
	{
		public DropTableStatement() : base(){}
		public DropTableStatement(string AObjectName) : base(AObjectName){}
	}
	
	public class DropViewStatement : DropObjectStatement
	{
		public DropViewStatement() : base(){}
		public DropViewStatement(string AObjectName) : base(AObjectName){}
	}

	public class DropScalarTypeStatement : DropObjectStatement
	{
		public DropScalarTypeStatement() : base(){}
		public DropScalarTypeStatement(string AObjectName) : base(AObjectName){}
	}
	
	public class DropOperatorStatement : DropObjectStatement
	{
		public DropOperatorStatement() : base(){}
		public DropOperatorStatement(string AObjectName) : base(AObjectName){}

		// FormalParameterSpecifiers
		protected FormalParameterSpecifiers FFormalParameterSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers FormalParameterSpecifiers { get { return FFormalParameterSpecifiers; } }
	}

	public class DropServerStatement : DropObjectStatement
	{
		public DropServerStatement() : base(){}
		public DropServerStatement(string AObjectName) : base(AObjectName){}
	}

	public class DropDeviceStatement : DropObjectStatement
	{
		public DropDeviceStatement() : base(){}
		public DropDeviceStatement(string AObjectName) : base(AObjectName){}
	}
	
	public abstract class EventSourceSpecifier : D4Statement
	{
		public EventSourceSpecifier() : base(){}
	}
	
	public class ObjectEventSourceSpecifier : EventSourceSpecifier
	{
		public ObjectEventSourceSpecifier() : base(){}
		public ObjectEventSourceSpecifier(string AObjectName) : base()
		{
			ObjectName = AObjectName;
		}
		
		// ObjectName
		protected string FObjectName = String.Empty;
		public string ObjectName
		{
			get { return FObjectName; }
			set { FObjectName = value == null ? String.Empty : value; }
		}
	}
	
	public class ColumnEventSourceSpecifier : EventSourceSpecifier
	{
		public ColumnEventSourceSpecifier() : base(){}
		public ColumnEventSourceSpecifier(string ATableVarName, string AColumnName) : base()
		{
			TableVarName = ATableVarName;
			ColumnName = AColumnName;
		}

		// TableVarName
		protected string FTableVarName = String.Empty;
		public string TableVarName
		{
			get { return FTableVarName; }
			set { FTableVarName = value == null ? String.Empty : value; }
		}

		// ColumnName
		protected string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}
	}
	
	public class ScalarTypeEventSourceSpecifier : EventSourceSpecifier
	{
		// ScalarTypeName
		protected string FScalarTypeName = String.Empty;
		public string ScalarTypeName
		{
			get { return FScalarTypeName; }
			set { FScalarTypeName = value == null ? String.Empty : value; }
		}
	}
	
	[Flags] 
	public enum EventType { BeforeInsert = 1, AfterInsert = 2, BeforeUpdate = 4, AfterUpdate = 8, BeforeDelete = 16, AfterDelete = 32, Default = 64, Validate = 128, Change = 256 }
	
	public class EventSpecifier : D4Statement 
	{
		protected EventType FEventType;
		public EventType EventType
		{
			get { return FEventType; }
			set { FEventType = value; }
		}
	}
	
	public abstract class AttachStatementBase : D4Statement
	{
		// OperatorName
		protected string FOperatorName;
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value; }
		}
		
		// EventSourceSpecifier
		protected EventSourceSpecifier FEventSourceSpecifier;
		public EventSourceSpecifier EventSourceSpecifier
		{
			get { return FEventSourceSpecifier; }
			set { FEventSourceSpecifier = value; }
		}

		// EventSpecifier
		protected EventSpecifier FEventSpecifier;
		public EventSpecifier EventSpecifier
		{
			get { return FEventSpecifier; }
			set { FEventSpecifier = value; }
		}
	}
	
	public class AttachStatement : AttachStatementBase, IMetaData
	{
		// BeforeOperatorNames
		protected StringCollection FBeforeOperatorNames = new StringCollection();
		public StringCollection BeforeOperatorNames { get { return FBeforeOperatorNames; } }
		
		// IsGenerated
		private bool FIsGenerated;
		public bool IsGenerated
		{
			get { return FIsGenerated; }
			set { FIsGenerated = value; }
		}

        // MetaData
        protected MetaData FMetaData;
        public MetaData MetaData
        {
			get { return FMetaData; }
			set { FMetaData = value; }
        }
	}
	
	public class InvokeStatement : AttachStatementBase
	{
		// BeforeOperatorNames
		protected StringCollection FBeforeOperatorNames = new StringCollection();
		public StringCollection BeforeOperatorNames { get { return FBeforeOperatorNames; } }
	}

	public class DetachStatement : AttachStatementBase {}
	
    public class RightSpecifier : D4Statement
    {
		public RightSpecifier() : base() {}
		public RightSpecifier(string ARightName) : base()
		{
			RightName = ARightName;
		}
		
		// RightName
		protected string FRightName = String.Empty;
		public string RightName
		{
			get { return FRightName; }
			set { FRightName = value == null ? String.Empty : value; }
		}
    }
    
	public class RightSpecifiers : Statements
	{
		protected override void Validate(object AItem)
		{
			if (!(AItem is RightSpecifier))
				throw new LanguageException(LanguageException.Codes.StatementContainer);
			base.Validate(AItem);
		}
		
		public new RightSpecifier this[int AIndex]
		{
			get { return (RightSpecifier)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}

	public class CatalogObjectSpecifier : D4Statement
	{
		// ObjectName
		protected string FObjectName = String.Empty;
		public string ObjectName
		{
			get { return FObjectName; }
			set { FObjectName = value == null ? String.Empty : value; }
		}
		
		private bool FIsOperator;
		public bool IsOperator 
		{ 
			get { return FIsOperator; } 
			set { FIsOperator = value; } 
		}

		// FormalParameterSpecifiers
		protected FormalParameterSpecifiers FFormalParameterSpecifiers = new FormalParameterSpecifiers();
		public FormalParameterSpecifiers FormalParameterSpecifiers { get { return FFormalParameterSpecifiers; } }
	}
	
	public enum GranteeType { User, Group, Role }

	public enum RightSpecifierType { All, Usage, List }
	
	public class RightStatementBase : D4Statement
	{
		private RightSpecifierType FRightType;
		public RightSpecifierType RightType
		{
			get { return FRightType; }
			set { FRightType = value; }
		}
		
		private RightSpecifiers FRights = new RightSpecifiers();
		public RightSpecifiers Rights { get { return FRights; } }

		private CatalogObjectSpecifier FTarget;
		public CatalogObjectSpecifier Target
		{
			get { return FTarget; }
			set { FTarget = value; }
		}

		private GranteeType FGranteeType;
		public GranteeType GranteeType
		{
			get { return FGranteeType; }
			set { FGranteeType = value; }
		}		
		
		private string FGrantee;
		public string Grantee
		{
			get { return FGrantee; }
			set { FGrantee = value; }
		}
		
		private bool FIsInherited;
		public bool IsInherited
		{
			get { return FIsInherited; }
			set { FIsInherited = value; }
		}
		
		private bool FApplyRecursively;
		public bool ApplyRecursively
		{
			get { return FApplyRecursively; }
			set { FApplyRecursively = value; }
		}
		
		private bool FIncludeUsers;
		public bool IncludeUsers
		{
			get { return FIncludeUsers; }
			set { FIncludeUsers = value; }
		}
	}
	
	public class GrantStatement : RightStatementBase {}
	public class RevokeStatement : RightStatementBase {}
	public class RevertStatement : RightStatementBase {}
}
