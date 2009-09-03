/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USEOPERATORRESOLUTIONCACHE
#define USECONVERSIONPATHCACHE
#define USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
//#define DISALLOWAMBIGUOUSNAMES

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Compiling
{
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;

	// TODO: Scan isolation levels...
    // TODO: table nesting operators
	// TODO: Expression Transformation Optimization Engine
	// TODO: Concurrency, Security, and Transaction Management
    // TODO: Allow aggregate operator invocation on one-column tables, defaulting to that column for the aggregation
	// TODO: Implement specialization and generalization by constraint
	// TODO: Reflection and full support for generic parameters
    // TODO: Preprocessor Directives (Conditional Compilation)
	// TODO: aliases... { named row or table headers }
	// TODO: in operator resolved incorrectly (value in { 'YES', 'NO' } )
	// TODO: Fussy / Rogue / Breakpoint nodes (UniqueRequired, UniqueDestroyed)
	// TODO: Detection of Union keys ? (sources both contain distinct "literal" values in the same column)
	// TODO: RequiredOrder / ResultingOrder
	// TODO: Left Right selection on join (cardinality estimation)
	// TODO: IsLiteral and restriction key determinaton
	// TODO: SEMIJOIN and SEMIMINUS ???
	// TODO: Accelarator -> Optimizer included node to improve performance
	// TODO: Relation-valued attributes
	// TODO: Functional Dependency Tracking in type inference
	// TODO: virtual invocation...
	// TODO: Replace all foreach usages with for loops (studies show a for loop is up to twice as fast as the equivalent foreach invocation)
	// TODO: Attach / Detach Clauses in create table statements
	// TODO: Create Event Handler statement
	// TODO: Create Event statement

    /*
		Decomposition ->
			Resolution of all operators and verification of syntactic correctness
			
		Transformation ->
			Validation of semantic correctness, simplification and optimization
			All transformation could be done through calls to a transformation rules processing engine.
			If the operators of the system could provide information about properties which they possess,
			(i.e. idempotence, associativity, etc.) transformation could be done generically, and even
			user-defined operators would be potentially optimizable...
			
		Compilation ->
			Determination of an optimal execution plan
			
		Dataphor Execution Engine ->
			Catalog, Stack, Arguments
			
			Execution flows down the PlanTree through the Execute node.
			The Plan object is passed to the Execute node, and contains the Stack.
			Arguments are prepared by the InstructionNode class for virtual invocation.
			
			How do we get everything to work the same way, using the same mechanism for all variables
			and allowing catalog and stack referenced variables to be used orthogonally to
			parameter modifiers and assignment statements.
			
			update v set {c.x := c.x + 5};
			updatenode
				table : DataValue(Node, APlan)
    */
    
    /*
		Armstrongs Axioms ->
		
			A -> A
			A -> B & A -> C => A -> B U C
			A -> B U C => A -> B & A -> C
			B <= A => A -> B
			A -> B => A U C -> B
			A -> B & B -> C => A -> C
			A -> B & C -> D => A U C -> B U D
		
		General Unification Theorem ->
		
			A -> B & C -> D => A U (C - B) -> B U D
			
		Propositional Logic Transformation Rules ->
		
			p & p == p
			p | p == true
			p & false == false
			p | false == p
			p & true == p
			p | true == true
			p & ~p == false
			p | ~p == true
			p & (p | q) == p
			p | (p & q) == p
			p & (q & r) == (p & q) & r
			p | (q | r) == (p | q) | r
			p & (q | r) == (p & q) | (p & r)
			p | (q & r) == (p | q) & (p | r)
			~(p & q) = ~p | ~q
			~(p | q) = ~p & ~q
			
		Arithmetic Transformation Rules ->
		
			Idempotence
				a unary operator f is idempotent iff f(a) == a
				a binary operator f is idempotent iff f(a, a) == a
		
			Distributivity
				a unary operator f distributes over a binary operator g iff f(g(a, b)) == g(f(a), f(b))
				a binary operator f distributes over a binary operator g iff f(a, g(b, c)) == g(f(a, b), f(a, c))

			Associativity
				a binary operator f is associative iff f(a, f(b, c)) == f(f(a, b), c)
				
			Commutativity
				a binary operator f is commutative iff f(a, b) == f(b, a)
				
			Transitivity
				a binary operator f is transitive iff f(a, b) & f(b, c) => f(a, c)
					This fact can be used to implement predicate transitive closure (Date, 1994)
		
		Relational Transformation Rules ->
		
			R where p & q == R where p where q 
			R where p | q == (R where p) union (R where q)
			R where p where q == R where q where p
			R over a over b == R over b (iff R over a includes a candidate key of R)
			R where p over a == R over a where p (iff p refers only to attributes in a)
			R join S == S join R
			(R join S) where p == (R where p) join S (iff p refers only to attributes in R)
			R union S == S union R
			(R union S) where p == (R where p) union (S where p)
			(R minus S) where p == (R where p) minus (S where p)
			(R union S) over a == (R over a) union (S over a)
			(R join S) over a == (R over a) join (S over a) (iff a includes all the attributes being joined)
			(R union S) union T == R union (S union T)
			(R join S) join T == R join (S join T)
			(R join S) where p == (R join S) (iff p defines an equi join on R and S)
			
		Possible Semantic Optimizations ->
		
			(R join S) over a == S over a (iff a is a subset of H(S) and a foreign key exists to restrict R over a to a subset of S over a)

		Keys and References ->
			when defined on base tables, insert, update and delete constraints are used
			to enforce the keys and references.  Otherwise, the constraints are expressed
			as a database level constraint. 
			
			As an aside, what does it mean to have a cascade delete on a reference defined
			on a view when the deletion occurs in a table on which the view is defined?
			With no way to obtain the data that would result from the view to actually
			cause the deletion, it must be assumed that the cascade delete does not occur
			unless the deletion is performed on the view itself, in which case the catalog
			constraints are deferred until commit.
			
			the basic form of the expressions is ->
				key (database constraint) ->
					count(table over {keys}) = count(table over all {keys})
				key insert validation ->
					not(exists(table where keys = values))
				key update validation ->
					oldvalues = newvalues or not(exists(table where keys = newvalues))
					
				reference (database constraint) ->
					not(exists(source over {keys} minus (target over {keys})))
					alternative without the minus (so translation to SQL-based DBMSs is supported ->
					not(exists(source where not(exists(target where target.keys = source.keys))))
			
				reference source insert validation ->
					exists(target where keys = values)
				reference source update validation ->
					oldvalues = newvalues or exists(target where keys = newvalues)
				reference target update validation ->
					oldvalues = newvalues or not(exists(source where keys = oldvalues))
				reference target delete validation ->
					not(exists(source where keys = values))
				reference target cascade update ->
					update source where keys = oldvalues set keys = newvalues
				reference target cascade delete ->
					delete source where keys = oldvalues
				reference target clear update or delete ->					
					update source where keys = oldvalues set keys = null
				reference target set update or delete ->
					update source where keys = oldvalues set keys = values
					
		meta data inference rules ->
		
			scalar types ->
				meta data is inherited from the parent scalar types in the order in which they are listed
				
			columns ->
				meta data is inherited from the column type

		catalog locking for D4 statements ->
			
			create statements and all select statements must acquire shared locks on the objects they
			reference as they are encountered during compilation.  This follows the same design as that
			used to determine and attach dependencies.

			alter statements must acquire exclusive locks on the object being modified, as well
			as all dependents recursively prior to any modification being performed.
			
			drop statements must acquire an exclusive lock on the object being dropped, but need
			not acquire locks on dependents, as dependents cannot exist if the object is to be dropped.
				
	*/
	
	/*
		Compilation of a Dataphor statement as expressed in a parse tree generated by the DataParser.
		PlanNode is the base class for all nodes in the plan tree. The plan tree can be thought of as
		a Dataphor executable.  Following compilation, all types in the tree will be known and all
		identifiers will be bound.
		
		Compilation involves the determination of
			Types (including table and row types and all that that entails)
			Location Binding
			Retrieve Binding (Device Partitioning)
			Modify Binding (Who will perform the update (if the result of the expression is a cursor))
			
		Decomposition ->
			DetermineDataType
			DetermineCharacteristics

		Transformation ->
			Optimize, compiler controlled analysis, Supports
			
		Binding ->
			DetermineBinding 
			DetermineDevice
			DetermineCursorBehavior
			DetermineModifyBinding
	*/    
	[Flags]
	public enum NameBindingFlags { Local = 1, Global = 2, Default = 3 }
	
	public class NameBindingContext : System.Object
	{
		public NameBindingContext(string AIdentifier, Schema.NameResolutionPath AResolutionPath) : base()
		{
			FIdentifier = AIdentifier;
			FResolutionPath = AResolutionPath;
		}
		
		public NameBindingContext(string AIdentifier, Schema.NameResolutionPath AResolutionPath, NameBindingFlags AFlags) : this(AIdentifier, AResolutionPath)
		{
			FBindingFlags = AFlags;
		}
		
		private string FIdentifier;
		/// <summary>The identifier being resolved for.</summary>
		public string Identifier { get { return FIdentifier; } }
		
		private NameBindingFlags FBindingFlags = NameBindingFlags.Default;
		/// <summary>Indicates whether the identifier is to be resolved locally, globally, or both.</summary>
		public NameBindingFlags BindingFlags { get { return FBindingFlags; } set { FBindingFlags = value; } }
		
		[Reference]
		private Schema.NameResolutionPath FResolutionPath;
		/// <summary>The resolution path used to resolve the identifier.</summary>
		public Schema.NameResolutionPath ResolutionPath { get { return FResolutionPath; } }

		/// <summary>The schema object which the identifier resolved to if the resolution was successful, null otherwise.</summary>		
		[Reference]
		public Schema.Object Object;
		
		private StringCollection FNames = new StringCollection();
		/// <summary>The list of names from the namespace which the identifier matches.</summary>
		public StringCollection Names { get { return FNames; } }

		/// <summary>Returns true if the identifier could not be resolved because it matched multiple names in the namespace.</summary>
		public bool IsAmbiguous { get { return FNames.Count > 1; } }
		
		/// <summary>Sets the binding data for this context to the binding data of the given context.</summary>
		public void SetBindingDataFromContext(NameBindingContext AContext)
		{
			Object = AContext.Object;
			FNames.Clear();
			foreach (string LString in AContext.Names)
				FNames.Add(LString);
		}
	}
	
	public class OperatorBindingContext : System.Object
	{
		public OperatorBindingContext(Statement AStatement, string AOperatorName, Schema.NameResolutionPath AResolutionPath, Schema.Signature ACallSignature, bool AIsExact) : base()
		{
			FStatement = AStatement;
			FOperatorNameContext = new NameBindingContext(AOperatorName, AResolutionPath);
			if (ACallSignature == null)
				Error.Fail(String.Format("Call signature null in operator binding context for operator {0}", AOperatorName));
			FCallSignature = ACallSignature;
			FIsExact = AIsExact;
		}
		
		private Statement FStatement;
		/// <summary>Gets the statement which originated the binding request.</summary>
		public Statement Statement { get { return FStatement; } }
		
		private NameBindingContext FOperatorNameContext;
		/// <summary>Gets the name binding context used to resolve the operator name.</summary>
		public NameBindingContext OperatorNameContext { get { return FOperatorNameContext; } }

		/// <summary>Gets the operator name being resolved for.</summary>		
		public string OperatorName { get { return FOperatorNameContext.Identifier; } }
		
		/// <summary>Gets the name resolution path being used to perform the resolution.</summary>
		public Schema.NameResolutionPath ResolutionPath { get { return FOperatorNameContext.ResolutionPath; } }
		
		private Schema.Signature FCallSignature;
		/// <summary>Gets the signature of the call being resolved for.</summary>
		public Schema.Signature CallSignature { get { return FCallSignature; } }
		
		private bool FIsExact;
		/// <summary>Indicates that the resolution must be exact. (No casting or conversion)</summary>
		public bool IsExact { get { return FIsExact; } }
		
		public override bool Equals(object AObject)
		{
			OperatorBindingContext LObject = AObject as OperatorBindingContext;
			return 
				(LObject != null) && 
				LObject.OperatorName.Equals(OperatorName) && 
				LObject.CallSignature.Equals(CallSignature) && 
				(LObject.IsExact == IsExact) &&
				(LObject.ResolutionPath == ResolutionPath);
		}

		public override int GetHashCode()
		{
			return OperatorName.GetHashCode() ^ CallSignature.GetHashCode() ^ IsExact.GetHashCode() ^ ResolutionPath.GetHashCode();
		}

		/// <summary>Indicates that the operator name was resolved, not necessarily correctly (it could be ambiguous)</summary>
		public bool IsOperatorNameResolved { get { return FOperatorNameContext.IsAmbiguous || (FOperatorNameContext.Object != null); } }
		
		/// <summary>The operator resolved, if a successful resolution is possible, null otherwise.</summary>
		[Reference]
		public Schema.Operator Operator;
		
		private OperatorMatches FMatches = new OperatorMatches();
		/// <summary>All the possible matches found for the calling signature.</summary>
		public OperatorMatches Matches { get { return FMatches; } }
		
		/// <summary>Sets the binding data for this context to the binding data of the given context.</summary>
		public void SetBindingDataFromContext(OperatorBindingContext AContext)
		{
			FOperatorNameContext.SetBindingDataFromContext(AContext.OperatorNameContext);
			Operator = AContext.Operator;
			FMatches.Clear();
			FMatches.AddRange(AContext.Matches);
		}
		
		public void MergeBindingDataFromContext(OperatorBindingContext AContext)
		{
			FOperatorNameContext.SetBindingDataFromContext(AContext.OperatorNameContext);
			foreach (OperatorMatch LMatch in AContext.Matches)
				if (!FMatches.Contains(LMatch))
					FMatches.Add(LMatch);

			if (FMatches.IsExact || (!FIsExact && FMatches.IsPartial))
				Operator = FMatches.Match.Signature.Operator;
			else
				Operator = null;
		}
	}
	
	public class ConversionContext : System.Object
	{
		public ConversionContext(Schema.IDataType ASourceType, Schema.IDataType ATargetType) : base()
		{
			FSourceType = ASourceType;
			FTargetType = ATargetType;
			FCanConvert = (FSourceType is Schema.IGenericType) || (FTargetType is Schema.IGenericType) || FSourceType.Is(FTargetType);
		}
		
		[Reference]
		private Schema.IDataType FSourceType;
		public Schema.IDataType SourceType { get { return FSourceType; } }
		
		[Reference]
		private Schema.IDataType FTargetType;
		public Schema.IDataType TargetType { get { return FTargetType; } }
		
		private bool FCanConvert;
		public virtual bool CanConvert 
		{ 
			get { return FCanConvert; } 
			set { FCanConvert = value; }
		}
		
		public virtual int NarrowingScore { get { return 0; } }
		
		public virtual int PathLength { get { return 0; } }
	}
	
	public class TableConversionContext : ConversionContext
	{
		public TableConversionContext(Schema.ITableType ASourceType, Schema.ITableType ATargetType) : base(ASourceType, ATargetType)
		{
			FSourceType = ASourceType;
			FTargetType = ATargetType;
		}
		
		[Reference]
		private Schema.ITableType FSourceType;
		public new Schema.ITableType SourceType { get { return FSourceType; } }

		[Reference]
		private Schema.ITableType FTargetType;
		public new Schema.ITableType TargetType { get { return FTargetType; } }
		
		private Hashtable FColumnConversions = new Hashtable();
		public Hashtable ColumnConversions { get { return FColumnConversions; } }
		
		public override int NarrowingScore
		{
			get
			{
				int LNarrowingScore = 0;
				foreach (DictionaryEntry LEntry in ColumnConversions)
					LNarrowingScore += ((ConversionContext)LEntry.Value).NarrowingScore;
				return LNarrowingScore;
			}
		}
		
		public override int PathLength
		{
			get
			{
				int LPathLength = 0;
				foreach (DictionaryEntry LEntry in ColumnConversions)
					LPathLength += ((ConversionContext)LEntry.Value).PathLength;
				return LPathLength;
			}
		}
	}
	
	public class RowConversionContext : ConversionContext
	{
		public RowConversionContext(Schema.IRowType ASourceType, Schema.IRowType ATargetType) : base(ASourceType, ATargetType)
		{
			FSourceType = ASourceType;
			FTargetType = ATargetType;
		}
		
		[Reference]
		private Schema.IRowType FSourceType;
		public new Schema.IRowType SourceType { get { return FSourceType; } }

		[Reference]
		private Schema.IRowType FTargetType;
		public new Schema.IRowType TargetType { get { return FTargetType; } }
		
		private Hashtable FColumnConversions = new Hashtable();
		public Hashtable ColumnConversions { get { return FColumnConversions; } }

		public override int NarrowingScore
		{
			get
			{
				int LNarrowingScore = 0;
				foreach (DictionaryEntry LEntry in ColumnConversions)
					LNarrowingScore += ((ConversionContext)LEntry.Value).NarrowingScore;
				return LNarrowingScore;
			}
		}
		
		public override int PathLength
		{
			get
			{
				int LPathLength = 0;
				foreach (DictionaryEntry LEntry in ColumnConversions)
					LPathLength += ((ConversionContext)LEntry.Value).PathLength;
				return LPathLength;
			}
		}
	}
	
	public class ScalarConversionContext : ConversionContext
	{
		public ScalarConversionContext(Schema.ScalarType ASourceType, Schema.ScalarType ATargetType) : base(ASourceType, ATargetType)
		{
			FSourceType = ASourceType;
			FTargetType = ATargetType;
		}
		
		[Reference]
		private Schema.ScalarType FSourceType;
		public new Schema.ScalarType SourceType { get { return FSourceType; } }

		[Reference]
		private Schema.ScalarType FTargetType;
		public new Schema.ScalarType TargetType { get { return FTargetType; } }
		
		private Schema.ScalarConversionPath FCurrentPath = new Schema.ScalarConversionPath();
		public Schema.ScalarConversionPath CurrentPath { get { return FCurrentPath; } }
		
		private Schema.ScalarConversionPaths FPaths = new Schema.ScalarConversionPaths();
		public Schema.ScalarConversionPaths Paths { get { return FPaths; } }
		
		/// <summary>Returns true if ASourceType is ATargetType or there is only one conversion path with the best narrowing score, false otherwise.</summary>
		public override bool CanConvert 
		{ 
			get { return FSourceType.IsGeneric || FTargetType.IsGeneric || FPaths.CanConvert; } 
			set { }
		}
		
		/// <summary>Contains the set of conversion paths with the current best narrowing score.</summary>
		public Schema.ScalarConversionPathList BestPaths { get { return FPaths.BestPaths; } }
		
		/// <summary>Returns the single conversion path with the best narrowing score, null if there are multiple paths with the same score.</summary>
		public Schema.ScalarConversionPath BestPath { get { return FPaths.BestPath; } }
		
		public override int NarrowingScore { get { return BestPath == null ? ((CanConvert && FPaths.Count == 0) ? 0 : Int32.MinValue) : BestPath.NarrowingScore; } }
		
		public override int PathLength { get { return BestPath == null ? ((CanConvert && FPaths.Count == 0) ? 0 : Int32.MaxValue) : BestPath.Count; } }
	}
	
	public abstract class Compiler : Object
	{
		public const string CIsSpecialOperatorName = @"IsSpecial";
		public const string CIsNilOperatorName = @"IsNil";
		public const string CIsSpecialComparerPrefix = @"Is";
		public const string CReadAccessorName = @"Read";
		public const string CWriteAccessorName = @"Write";
		
		// Compile overloads which take a string as input
		public static PlanNode Compile(Plan APlan, string AStatement)
		{
			return Compile(APlan, AStatement, null, false);
		}
		
		public static PlanNode Compile(Plan APlan, string AStatement, DataParams AParams)
		{
			return Compile(APlan, AStatement, AParams, false);
		}
		
		public static PlanNode Compile(Plan APlan, string AStatement, bool AIsCursor)
		{
			return Compile(APlan, AStatement, null, AIsCursor);
		}

		// Main compile method with string input which all overloads that take strings call		
		public static PlanNode Compile(Plan APlan, string AStatement, DataParams AParams, bool AIsCursor)
		{
			Statement LStatement;
			if (APlan.Language == QueryLanguage.RealSQL)
				LStatement = new RealSQL.Compiler().Compile(new RealSQL.Parser().ParseStatement(AStatement));
			else
				LStatement = AIsCursor ? new Parser().ParseCursorDefinition(AStatement) : new Parser().ParseScript(AStatement, null);
			return Compile(APlan, LStatement, AParams, AIsCursor);
		}

		// Compile overloads which take a syntax tree as input
		public static PlanNode Compile(Plan APlan, Statement AStatement)
		{
			return Compile(APlan, AStatement, null, false);
		}
		
		public static PlanNode Compile(Plan APlan, Statement AStatement, DataParams AParams)
		{
			return Compile(APlan, AStatement, AParams, false);
		}
		
		public static PlanNode Compile(Plan APlan, Statement AStatement, bool AIsCursor)
		{
			return Compile(APlan, AStatement, null, AIsCursor);
		}

		public static PlanNode Compile(Plan APlan, Statement AStatement, DataParams AParams, bool AIsCursor)
		{
			return Compile(APlan, AStatement, AParams, AIsCursor, null);
		}

		// Main compile method which all overloads eventually call		
		public static PlanNode Compile(Plan APlan, Statement AStatement, DataParams AParams, bool AIsCursor, SourceContext ASourceContext)
		{
			// Prepare plan timers
			long LStartTicks = TimingUtility.CurrentTicks;
			
			// Prepare the stack for compilation with the given context
			if (AParams != null)
				foreach (DataParam LParam in AParams)
					APlan.Symbols.Push(new Symbol(LParam.Name, LParam.DataType));
			
			PlanNode LNode = null;
			APlan.PushSourceContext(ASourceContext);
			try
			{
				try
				{
					if (AIsCursor)
						LNode = CompileCursor(APlan, AStatement is Expression ? (Expression)AStatement : ((SelectStatement)AStatement).CursorDefinition);
					else
					{
						APlan.Symbols.PushFrame();
						try
						{
							LNode = CompileStatement(APlan, AStatement);
							APlan.ReportProcessSymbols();
						}
						finally
						{
							APlan.Symbols.PopFrame();
						}
					}
				}
				catch (Exception LException)
				{
					if (!(LException is CompilerException) || (((CompilerException)LException).Code != (int)CompilerException.Codes.NonFatalErrors))
						APlan.Messages.Add(LException);
				}
			}
			finally
			{
				APlan.PopSourceContext();
			}
			
			APlan.Statistics.CompileTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));

			if (!APlan.Messages.HasErrors)
			{
				long LStartSubTicks = TimingUtility.CurrentTicks;
				LNode = Optimize(APlan, LNode);
				APlan.Statistics.OptimizeTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartSubTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
				LStartSubTicks = TimingUtility.CurrentTicks;
				LNode = Bind(APlan, LNode);
				APlan.Statistics.BindingTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartSubTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			}

			APlan.Statistics.PrepareTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			return LNode;
		}

		public static PlanNode Optimize(Plan APlan, PlanNode APlanNode)
		{
			// This method is here for consistency with the Bind phase method, and for future expansion.
			return OptimizeNode(APlan, APlanNode);
		}
		
		public static PlanNode OptimizeNode(Plan APlan, PlanNode APlanNode)
		{
			try
			{
				if (APlan.ShouldEmitIL)
					APlanNode.EmitIL(APlan, false);
			}
			catch (Exception LException)
			{
				APlan.Messages.Add(LException);
				//throw new CompilerException(CompilerException.Codes.OptimizationError, LException);
			}

			return APlanNode;
		}
		
		public static PlanNode Bind(Plan APlan, PlanNode APlanNode)
		{
			APlan.Symbols.PushFrame();
			try
			{
				return BindNode(APlan, APlanNode);
			}
			finally
			{
				APlan.Symbols.PopFrame();
			}
		}
		
		public static PlanNode BindNode(Plan APlan, PlanNode APlanNode)
		{
			try
			{
				if (!APlan.IsRepository)
					APlanNode.DetermineBinding(APlan);
			}
			catch (Exception LException)
			{
				APlan.Messages.Add(LException);
				//throw new CompilerException(CompilerException.Codes.BindingError, LException);
			}
			
			return APlanNode;
		}

		#if USESTATEMENTENTRIES		
		public delegate PlanNode CompileStatementCallback(Plan APlan, Statement AStatement);
		
		public static Hashtable StatementEntries;
		
		static Compiler()
		{
			StatementEntries = new Hashtable();
			StatementEntries.Add("Block", new CompileStatementCallback(CompileBlock));
			StatementEntries.Add("DelimitedBlock", new CompileStatementCallback(CompileDelimitedBlock));
			StatementEntries.Add("IfStatement", new CompileStatementCallback(CompileIfStatement));
			StatementEntries.Add("CaseStatement", new CompileStatementCallback(CompileCaseStatement));
			StatementEntries.Add("WhileStatement", new CompileStatementCallback(CompileWhileStatement));
			StatementEntries.Add("ForEachStatement", new CompileStatementCallback(CompileForEachStatement));
			StatementEntries.Add("DoWhileStatement", new CompileStatementCallback(CompileDoWhileStatement));
			StatementEntries.Add("BreakStatement", new CompileStatementCallback(CompileBreakStatement));
			StatementEntries.Add("ContinueStatement", new CompileStatementCallback(CompileContinueStatement));
			StatementEntries.Add("ExitStatement", new CompileStatementCallback(CompileExitStatement));
			StatementEntries.Add("VariableStatement", new CompileStatementCallback(CompileVariableStatement));
			StatementEntries.Add("AssignmentStatement", new CompileStatementCallback(CompileAssignmentStatement));
			StatementEntries.Add("RaiseStatement", new CompileStatementCallback(CompileRaiseStatement));
			StatementEntries.Add("TryFinallyStatement", new CompileStatementCallback(CompileTryFinallyStatement));
			StatementEntries.Add("TryExceptStatement", new CompileStatementCallback(CompileTryExceptStatement));
			StatementEntries.Add("ExpressionStatement", new CompileStatementCallback(CompileExpressionStatement));
			StatementEntries.Add("SelectStatement", new CompileStatementCallback(CompileExpression));
			StatementEntries.Add("InsertStatement", new CompileStatementCallback(CompileInsertStatement));
			StatementEntries.Add("UpdateStatement", new CompileStatementCallback(CompileUpdateStatement));
			StatementEntries.Add("DeleteStatement", new CompileStatementCallback(CompileDeleteStatement));
			StatementEntries.Add("CreateTableStatement", new CompileStatementCallback(CompileCreateTableStatement));
			StatementEntries.Add("CreateViewStatement", new CompileStatementCallback(CompileCreateViewStatement));
			StatementEntries.Add("CreateScalarTypeStatement", new CompileStatementCallback(CompileCreateScalarTypeStatement));
			StatementEntries.Add("CreateOperatorStatement", new CompileStatementCallback(CompileCreateOperatorStatement));
			StatementEntries.Add("CreateAggregateOperatorStatement", new CompileStatementCallback(CompileCreateAggregateOperatorStatement));
			StatementEntries.Add("CreateConstraintStatement", new CompileStatementCallback(CompileCreateConstraintStatement));
			StatementEntries.Add("CreateReferenceStatement", new CompileStatementCallback(CompileCreateReferenceStatement));
			StatementEntries.Add("CreateDeviceStatement", new CompileStatementCallback(CompileCreateDeviceStatement));
			StatementEntries.Add("CreateServerStatement", new CompileStatementCallback(CompileCreateServerStatement));
			StatementEntries.Add("CreateSortStatement", new CompileStatementCallback(CompileCreateSortStatement));
			StatementEntries.Add("CreateConversionStatement", new CompileStatementCallback(CompileCreateConversionStatement));
			StatementEntries.Add("CreateRoleStatement", new CompileStatementCallback(CompileCreateRoleStatement));
			StatementEntries.Add("CreateRightStatement", new CompileStatementCallback(CompileCreateRightStatement));
			StatementEntries.Add("AlterTableStatement", new CompileStatementCallback(CompileAlterTableStatement));
			StatementEntries.Add("AlterViewStatement", new CompileStatementCallback(CompileAlterViewStatement));
			StatementEntries.Add("AlterScalarTypeStatement", new CompileStatementCallback(CompileAlterScalarTypeStatement));
			StatementEntries.Add("AlterOperatorStatement", new CompileStatementCallback(CompileAlterOperatorStatement));
			StatementEntries.Add("AlterAggregateOperatorStatement", new CompileStatementCallback(CompileAlterAggregateOperatorStatement));
			StatementEntries.Add("AlterConstraintStatement", new CompileStatementCallback(CompileAlterConstraintStatement));
			StatementEntries.Add("AlterReferenceStatement", new CompileStatementCallback(CompileAlterReferenceStatement));
			StatementEntries.Add("AlterDeviceStatement", new CompileStatementCallback(CompileAlterDeviceStatement));
			StatementEntries.Add("AlterServerStatement", new CompileStatementCallback(CompileAlterServerStatement));
			StatementEntries.Add("AlterSortStatement", new CompileStatementCallback(CompileAlterSortStatement));
			StatementEntries.Add("AlterRoleStatement", new CompileStatementCallback(CompileAlterRoleStatement));
			StatementEntries.Add("DropTableStatement", new CompileStatementCallback(CompileDropTableStatement));
			StatementEntries.Add("DropViewStatement", new CompileStatementCallback(CompileDropViewStatement));
			StatementEntries.Add("DropScalarTypeStatement", new CompileStatementCallback(CompileDropScalarTypeStatement));
			StatementEntries.Add("DropOperatorStatement", new CompileStatementCallback(CompileDropOperatorStatement));
			StatementEntries.Add("DropConstraintStatement", new CompileStatementCallback(CompileDropConstraintStatement));
			StatementEntries.Add("DropReferenceStatement", new CompileStatementCallback(CompileDropReferenceStatement));
			StatementEntries.Add("DropDeviceStatement", new CompileStatementCallback(CompileDropDeviceStatement));
			StatementEntries.Add("DropServerStatement", new CompileStatementCallback(CompileDropServerStatement));
			StatementEntries.Add("DropSortStatement", new CompileStatementCallback(CompileDropSortStatement));
			StatementEntries.Add("DropConversionStatement", new CompileStatementCallback(CompileDropConversionStatement));
			StatementEntries.Add("DropRoleStatement", new CompileStatementCallback(CompileDropRoleStatement));
			StatementEntries.Add("DropRightStatement", new CompileStatementCallback(CompileDropRightStatement));
			StatementEntries.Add("AttachStatement", new CompileStatementCallback(CompileAttachStatement));
			StatementEntries.Add("DetachStatement", new CompileStatementCallback(CompileDetachStatement));
			StatementEntries.Add("GrantStatement", new CompileStatementCallback(CompileSecurityStatement));
			StatementEntries.Add("RevokeStatement", new CompileStatementCallback(CompileSecurityStatement));
			StatementEntries.Add("RevertStatement", new CompileStatementCallback(CompileSecurityStatement));
			StatementEntries.Add("EmptyStatement", new CompileStatementCallback(CompileEmptyStatement));
		}
		
		public static PlanNode InternalCompileStatement(Plan APlan, Statement AStatement)
		{
			CompileStatementCallback LRoutine = StatementEntries[AStatement.GetType().Name] as CompileStatementCallback;
			if (LRoutine != null)
				return LRoutine(APlan, AStatement);
			throw new CompilerException(CompilerException.Codes.UnknownStatementClass, AStatement, AStatement.GetType().FullName);
		}
		#else
		public static PlanNode InternalCompileStatement(Plan APlan, Statement AStatement)
		{
			APlan.PushStatement(AStatement);
			try
			{
				try
				{
					switch (AStatement.GetType().Name)
					{
						case "Block" : return CompileBlock(APlan, AStatement);
						case "DelimitedBlock" : return CompileDelimitedBlock(APlan, AStatement);
						case "IfStatement" : return CompileIfStatement(APlan, AStatement);
						case "CaseStatement" : return CompileCaseStatement(APlan, AStatement);
						case "WhileStatement" : return CompileWhileStatement(APlan, AStatement);
						case "ForEachStatement" : return CompileForEachStatement(APlan, AStatement);
						case "DoWhileStatement" : return CompileDoWhileStatement(APlan, AStatement);
						case "BreakStatement" : return CompileBreakStatement(APlan, AStatement);
						case "ContinueStatement" : return CompileContinueStatement(APlan, AStatement);
						case "ExitStatement" : return CompileExitStatement(APlan, AStatement);
						case "VariableStatement" : return CompileVariableStatement(APlan, AStatement);
						case "AssignmentStatement" : return CompileAssignmentStatement(APlan, AStatement);
						case "RaiseStatement" : return CompileRaiseStatement(APlan, AStatement);
						case "TryFinallyStatement" : return CompileTryFinallyStatement(APlan, AStatement);
						case "TryExceptStatement" : return CompileTryExceptStatement(APlan, AStatement);
						case "ExpressionStatement" : return CompileExpressionStatement(APlan, AStatement);
						case "SelectStatement" : return CompileExpression(APlan, AStatement);
						case "InsertStatement" : return CompileInsertStatement(APlan, AStatement);
						case "UpdateStatement" : return CompileUpdateStatement(APlan, AStatement);
						case "DeleteStatement" : return CompileDeleteStatement(APlan, AStatement);
						case "CreateTableStatement" : return CompileCreateTableStatement(APlan, AStatement);
						case "CreateViewStatement" : return CompileCreateViewStatement(APlan, AStatement);
						case "CreateScalarTypeStatement" : return CompileCreateScalarTypeStatement(APlan, AStatement);
						case "CreateOperatorStatement" : return CompileCreateOperatorStatement(APlan, AStatement);
						case "CreateAggregateOperatorStatement" : return CompileCreateAggregateOperatorStatement(APlan, AStatement);
						case "CreateConstraintStatement" : return CompileCreateConstraintStatement(APlan, AStatement);
						case "CreateReferenceStatement" : return CompileCreateReferenceStatement(APlan, AStatement);
						case "CreateDeviceStatement" : return CompileCreateDeviceStatement(APlan, AStatement);
						case "CreateServerStatement" : return CompileCreateServerStatement(APlan, AStatement);
						case "CreateSortStatement" : return CompileCreateSortStatement(APlan, AStatement);
						case "CreateConversionStatement" : return CompileCreateConversionStatement(APlan, AStatement);
						case "CreateRoleStatement" : return CompileCreateRoleStatement(APlan, AStatement);
						case "CreateRightStatement" : return CompileCreateRightStatement(APlan, AStatement);
						case "AlterTableStatement" : return CompileAlterTableStatement(APlan, AStatement);
						case "AlterViewStatement" : return CompileAlterViewStatement(APlan, AStatement);
						case "AlterScalarTypeStatement" : return CompileAlterScalarTypeStatement(APlan, AStatement);
						case "AlterOperatorStatement" : return CompileAlterOperatorStatement(APlan, AStatement);
						case "AlterAggregateOperatorStatement" : return CompileAlterAggregateOperatorStatement(APlan, AStatement);
						case "AlterConstraintStatement" : return CompileAlterConstraintStatement(APlan, AStatement);
						case "AlterReferenceStatement" : return CompileAlterReferenceStatement(APlan, AStatement);
						case "AlterDeviceStatement" : return CompileAlterDeviceStatement(APlan, AStatement);
						case "AlterServerStatement" : return CompileAlterServerStatement(APlan, AStatement);
						case "AlterSortStatement" : return CompileAlterSortStatement(APlan, AStatement);
						case "AlterRoleStatement" : return CompileAlterRoleStatement(APlan, AStatement);
						case "DropTableStatement" : return CompileDropTableStatement(APlan, AStatement);
						case "DropViewStatement" : return CompileDropViewStatement(APlan, AStatement);
						case "DropScalarTypeStatement" : return CompileDropScalarTypeStatement(APlan, AStatement);
						case "DropOperatorStatement" : return CompileDropOperatorStatement(APlan, AStatement);
						case "DropConstraintStatement" : return CompileDropConstraintStatement(APlan, AStatement);
						case "DropReferenceStatement" : return CompileDropReferenceStatement(APlan, AStatement);
						case "DropDeviceStatement" : return CompileDropDeviceStatement(APlan, AStatement);
						case "DropServerStatement" : return CompileDropServerStatement(APlan, AStatement);
						case "DropSortStatement" : return CompileDropSortStatement(APlan, AStatement);
						case "DropConversionStatement" : return CompileDropConversionStatement(APlan, AStatement);
						case "DropRoleStatement" : return CompileDropRoleStatement(APlan, AStatement);
						case "DropRightStatement" : return CompileDropRightStatement(APlan, AStatement);
						case "AttachStatement" : return CompileAttachStatement(APlan, AStatement);
						case "InvokeStatement" : return CompileInvokeStatement(APlan, AStatement);
						case "DetachStatement" : return CompileDetachStatement(APlan, AStatement);
						case "GrantStatement" : return CompileSecurityStatement(APlan, AStatement);
						case "RevokeStatement" : return CompileSecurityStatement(APlan, AStatement);
						case "RevertStatement" : return CompileSecurityStatement(APlan, AStatement);
						case "EmptyStatement" : return CompileEmptyStatement(APlan, AStatement);
						default : throw new CompilerException(CompilerException.Codes.UnknownStatementClass, AStatement, AStatement.GetType().FullName);
					}
				}
				catch (CompilerException LException)
				{
					if ((LException.Line == -1) && (AStatement.Line != -1))
					{
						LException.Line = AStatement.Line;
						LException.LinePos = AStatement.LinePos;
					}
					throw;
				}
				catch (Exception LException)
				{
					if (!(LException is DataphorException))
						throw new CompilerException(CompilerException.Codes.InternalError, ErrorSeverity.System, CompilerErrorLevel.NonFatal, AStatement, LException);
						
					if (!(LException is ILocatedException))
						throw new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.NonFatal, AStatement, LException, LException.Message);
					throw;
				}
			}
			finally
			{
				APlan.PopStatement();
			}
		}
		#endif
		
		public static PlanNode CompileStatement(Plan APlan, Statement AStatement)
		{
			if (AStatement is Expression)
				return CompileExpression(APlan, (Expression)AStatement);
			else
			{
				try
				{
					return InternalCompileStatement(APlan, AStatement);
				}
				catch (Exception LException)
				{
					if (!(LException is CompilerException) || ((((CompilerException)LException).Code != (int)CompilerException.Codes.NonFatalErrors) && (((CompilerException)LException).Code != (int)CompilerException.Codes.FatalErrors)))
						APlan.Messages.Add(LException);	
				}
				
				if (APlan.Messages.HasFatalErrors)
					throw new CompilerException(CompilerException.Codes.FatalErrors);
				
				return new NoOpNode();
			}
		}
		
		public static PlanNode CompileBlock(Plan APlan, Statement AStatement)
		{																	
			BlockNode LNode = new BlockNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			foreach (Statement LStatement in ((Block)AStatement).Statements)
				LNode.Nodes.Add(CompileStatement(APlan, LStatement));
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileDelimitedBlock(Plan APlan, Statement AStatement)
		{
			FrameNode LFrameNode = new FrameNode();
			LFrameNode.SetLineInfo(AStatement.LineInfo);
			APlan.Symbols.PushFrame();
			try
			{
				DelimitedBlockNode LNode = new DelimitedBlockNode();
				LNode.SetLineInfo(AStatement.LineInfo);
				foreach (Statement LStatement in ((DelimitedBlock)AStatement).Statements)
					LNode.Nodes.Add(CompileStatement(APlan, LStatement));
				LNode.DetermineCharacteristics(APlan);
				LFrameNode.Nodes.Add(CompileDeallocateFrameVariablesNode(APlan, LNode));
				return LFrameNode;
			}
			finally
			{
				APlan.Symbols.PopFrame();
			}
		}
		
		protected static PlanNode CompileExpressionStatement(Plan APlan, Statement AStatement)
		{
			ExpressionStatementNode LNode = new ExpressionStatementNode(CompileExpression(APlan, ((ExpressionStatement)AStatement).Expression, true));
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.DetermineCharacteristics(APlan);
			#if ALLOWSTATEMENTSASEXPRESSIONS
			if ((LNode.Nodes[0].DataType != null) && !LNode.Nodes[0].DataType.Equals(APlan.DataTypes.SystemScalar) && LNode.Nodes[0].IsFunctional && !APlan.SuppressWarnings)
			#else
			if ((LNode.Nodes[0].DataType != null) && LNode.Nodes[0].IsFunctional && !APlan.SuppressWarnings)
			#endif
				APlan.Messages.Add(new CompilerException(CompilerException.Codes.ExpressionStatement, CompilerErrorLevel.Warning, AStatement));
			return LNode;
		}
		
		protected static PlanNode CompileIfStatement(Plan APlan, Statement AStatement)
		{
			IfStatement LStatement = (IfStatement)AStatement;
			PlanNode LIfNode = CompileBooleanExpression(APlan, LStatement.Expression);
			PlanNode LTrueNode = CompileFrameNode(APlan, LStatement.TrueStatement);
			PlanNode LFalseNode = null;
			if (LStatement.FalseStatement != null)
				LFalseNode = CompileFrameNode(APlan, LStatement.FalseStatement);
			PlanNode LNode = EmitIfNode(APlan, AStatement, LIfNode, LTrueNode, LFalseNode);
			return LNode;
		}
		
		protected static PlanNode EmitIfNode(Plan APlan, Statement AStatement, PlanNode ACondition, PlanNode ATrueStatement, PlanNode AFalseStatement)
		{
			IfNode LNode = new IfNode();
			if (AStatement != null)
				LNode.SetLineInfo(AStatement.LineInfo);
			else
				LNode.IsBreakable = false;
			LNode.Nodes.Add(ACondition);
			LNode.Nodes.Add(ATrueStatement);
			if (AFalseStatement != null)
				LNode.Nodes.Add(AFalseStatement);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileCaseItemStatement(Plan APlan, CaseStatement AStatement, int AIndex)
		{
			PlanNode LWhenNode = CompileExpression(APlan, AStatement.CaseItems[AIndex].WhenExpression);
			if (AStatement.Expression != null)
			{
				PlanNode LCompareNode = CompileExpression(APlan, AStatement.Expression);
				LWhenNode = EmitBinaryNode(APlan, AStatement.Expression, LCompareNode, Instructions.Equal, LWhenNode);
			}
				
			PlanNode LThenNode;
			if (AStatement.CaseItems[AIndex].ThenStatement.GetType().Name == "Block")
				LThenNode = CompileStatement(APlan, new DelimitedBlock());	 // An empty statement is not a valid true or false statement
			else
				LThenNode = CompileStatement(APlan, AStatement.CaseItems[AIndex].ThenStatement);
			PlanNode LElseNode;
			if (AIndex >= AStatement.CaseItems.Count - 1)
			{
				if (AStatement.ElseStatement != null)
					if (AStatement.ElseStatement.GetType().Name == "Block")
						LElseNode = CompileStatement(APlan, new DelimitedBlock());
					else
						LElseNode = CompileStatement(APlan, AStatement.ElseStatement);
				else
					LElseNode = null;
			}
			else
				LElseNode = CompileCaseItemStatement(APlan, AStatement, AIndex + 1);

			return EmitIfNode(APlan, AStatement.CaseItems[AIndex], LWhenNode, LThenNode, LElseNode);
		}
		
		// case [<expression>] when <expression> then <statement> ... else <statement> end;
		// if <expression> then <statement> else <statement>
		// if <expression> = <expression> then <statement> else <statement>
		protected static PlanNode CompileCaseStatement(Plan APlan, Statement AStatement)
		{
			//return CompileCaseItemStatement(APlan, (CaseStatement)AStatement, 0);
			
			CaseStatement LStatement = (CaseStatement)AStatement;
			if (LStatement.Expression != null)
			{
				SelectedCaseNode LNode = new SelectedCaseNode();
				LNode.SetLineInfo(AStatement.LineInfo);
				
				PlanNode LSelectorNode = CompileExpression(APlan, LStatement.Expression);
				PlanNode LEqualNode = null;
				APlan.Symbols.Push(new Symbol(LSelectorNode.DataType));
				try
				{
					LNode.Nodes.Add(LSelectorNode);
					
					foreach (CaseItemStatement LCaseItemStatement in LStatement.CaseItems)
					{
						CaseItemNode LCaseItemNode = new CaseItemNode();
						LCaseItemNode.SetLineInfo(LCaseItemStatement.LineInfo);
						PlanNode LWhenNode = CompileTypedExpression(APlan, LCaseItemStatement.WhenExpression, LSelectorNode.DataType);
						LCaseItemNode.Nodes.Add(LWhenNode);
						APlan.Symbols.Push(new Symbol(LWhenNode.DataType));
						try
						{
							if (LEqualNode == null)
							{
								LEqualNode = EmitBinaryNode(APlan, new StackReferenceNode(LSelectorNode.DataType, 1, true), Instructions.Equal, new StackReferenceNode(LWhenNode.DataType, 0, true));
								LNode.Nodes.Add(LEqualNode);
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}

						LCaseItemNode.Nodes.Add(CompileStatement(APlan, LCaseItemStatement.ThenStatement));
						LCaseItemNode.DetermineCharacteristics(APlan);
						LNode.Nodes.Add(LCaseItemNode);
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
				
				if (LStatement.ElseStatement != null)
				{	
					CaseItemNode LCaseItemNode = new CaseItemNode();
					LCaseItemNode.SetLineInfo(LStatement.ElseStatement.LineInfo);
					LCaseItemNode.Nodes.Add(CompileStatement(APlan, LStatement.ElseStatement));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
		
				LNode.DetermineCharacteristics(APlan);		
				return LNode;
			}
			else
			{
				CaseNode LNode = new CaseNode();
				LNode.SetLineInfo(AStatement.LineInfo);
				
				foreach (CaseItemStatement LCaseItemStatement in LStatement.CaseItems)
				{
					CaseItemNode LCaseItemNode = new CaseItemNode();
					LCaseItemNode.SetLineInfo(LCaseItemStatement.LineInfo);
					LCaseItemNode.Nodes.Add(CompileBooleanExpression(APlan, LCaseItemStatement.WhenExpression));
					LCaseItemNode.Nodes.Add(CompileStatement(APlan, LCaseItemStatement.ThenStatement));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
				
				if (LStatement.ElseStatement != null)
				{
					CaseItemNode LCaseItemNode = new CaseItemNode();
					LCaseItemNode.SetLineInfo(LStatement.ElseStatement.LineInfo);
					LCaseItemNode.Nodes.Add(CompileStatement(APlan, LStatement.ElseStatement));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
				
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
		}
		
		protected static PlanNode CompileWhileStatement(Plan APlan, Statement AStatement)
		{
			WhileStatement LStatement = (WhileStatement)AStatement;
			WhileNode LNode = new WhileNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			PlanNode LConditionNode = CompileBooleanExpression(APlan, LStatement.Condition);
			LNode.Nodes.Add(LConditionNode);
			APlan.EnterLoop();
			try
			{
				LNode.Nodes.Add(CompileFrameNode(APlan, LStatement.Statement));
			}
			finally
			{
				APlan.ExitLoop();
			}
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileForEachStatement(Plan APlan, Statement AStatement)
		{
			ForEachNode LNode = new ForEachNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.Statement = (ForEachStatement)AStatement;
			PlanNode LExpression = CompileCursorDefinition(APlan, LNode.Statement.Expression);
			APlan.EnterLoop();
			try
			{
				LNode.Nodes.Add(LExpression);
				
				if (LExpression.DataType is Schema.ICursorType)
				{
					LNode.VariableType = ((Schema.ICursorType)LExpression.DataType).TableType.RowType;
				}
				else if (LExpression.DataType is Schema.IListType)
				{
					if (LNode.Statement.VariableName == String.Empty)
						throw new CompilerException(CompilerException.Codes.ForEachVariableNameRequired, LNode.Statement);
					LNode.VariableType = ((Schema.IListType)LExpression.DataType).ElementType;
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidForEachStatement, LNode.Statement);
					
				if (LNode.Statement.VariableName == String.Empty)
					APlan.EnterRowContext();
				try
				{
					if ((LNode.Statement.VariableName == String.Empty) || LNode.Statement.IsAllocation)
					{
						if (LNode.Statement.VariableName != String.Empty)
						{
							StringCollection LNames = new StringCollection();
							if (!APlan.Symbols.IsValidVariableIdentifier(LNode.Statement.VariableName, LNames))
							{
								#if DISALLOWAMBIGUOUSNAMES
								if (Schema.Object.NamesEqual(LNames[0], LStatement.VariableName.Identifier))
									if (String.Compare(LNames[0], LStatement.VariableName.Identifier) == 0)
										throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier);
									else
										throw new CompilerException(CompilerException.Codes.CreatingHiddenIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier, LNames[0]);
								else
									throw new CompilerException(CompilerException.Codes.CreatingHidingIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier, LNames[0]);
								#else
								throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, LNode.Statement, LNode.Statement.VariableName);
								#endif
							}
						}
						APlan.Symbols.Push(new Symbol(LNode.Statement.VariableName, LNode.VariableType));
					}
					else
					{
						int LColumnIndex;
						LNode.Location = ResolveVariableIdentifier(APlan, LNode.Statement.VariableName, out LColumnIndex);
						if (LNode.Location < 0)
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, LNode.Statement, LNode.Statement.VariableName);
							
						if (LColumnIndex >= 0)
							throw new CompilerException(CompilerException.Codes.InvalidColumnReference, LNode.Statement);
							
						if (!LNode.VariableType.Is(APlan.Symbols.Peek(LNode.Location).DataType))
							throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, LNode.Statement, LNode.VariableType.Name, APlan.Symbols.Peek(LNode.Location).DataType.Name);
					}
					try
					{
						LNode.Nodes.Add(CompileStatement(APlan, LNode.Statement.Statement));
					}
					finally
					{
						if ((LNode.Statement.VariableName == String.Empty) || LNode.Statement.IsAllocation)
							APlan.Symbols.Pop();
					}
				}
				finally
				{
					if (LNode.Statement.VariableName == String.Empty)
						APlan.ExitRowContext();
				}
			}
			finally
			{
				APlan.ExitLoop();
			}
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileDoWhileStatement(Plan APlan, Statement AStatement)
		{
			DoWhileStatement LStatement = (DoWhileStatement)AStatement;
			DoWhileNode LNode = new DoWhileNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			PlanNode LConditionNode = CompileBooleanExpression(APlan, LStatement.Condition);
			APlan.EnterLoop();
			try
			{
				LNode.Nodes.Add(CompileFrameNode(APlan, LStatement.Statement));
			}
			finally
			{
				APlan.ExitLoop();
			}
			LNode.Nodes.Add(LConditionNode);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileExitStatement(Plan APlan, Statement AStatement)
		{
			ExitNode LNode = new ExitNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileBreakStatement(Plan APlan, Statement AStatement)
		{
			if (!APlan.InLoop)
				throw new CompilerException(CompilerException.Codes.NoLoop, AStatement);
			BreakNode LNode = new BreakNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileContinueStatement(Plan APlan, Statement AStatement)
		{
			if (!APlan.InLoop)
				throw new CompilerException(CompilerException.Codes.NoLoop, AStatement);
			ContinueNode LNode = new ContinueNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileRaiseStatement(Plan APlan, Statement AStatement)
		{
			RaiseStatement LStatement = (RaiseStatement)AStatement;
			RaiseNode LNode = new RaiseNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			if (LStatement.Expression != null)
			{
				PlanNode LPlanNode = CompileExpression(APlan, LStatement.Expression);
				if (!LPlanNode.DataType.Is(APlan.DataTypes.SystemError))
					throw new CompilerException(CompilerException.Codes.ErrorExpressionExpected, AStatement);
				LNode.Nodes.Add(LPlanNode);
			}
			else
			{
				if (!APlan.InErrorContext)
					throw new CompilerException(CompilerException.Codes.InvalidRaiseContext, AStatement);
			}
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileTryFinallyStatement(Plan APlan, Statement AStatement)
		{
			TryFinallyStatement LStatement = (TryFinallyStatement)AStatement;
			TryFinallyNode LNode = new TryFinallyNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.Nodes.Add(CompileFrameNode(APlan, LStatement.TryStatement));
			LNode.Nodes.Add(CompileFrameNode(APlan, LStatement.FinallyStatement));
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode CompileTryExceptStatement(Plan APlan, Statement AStatement)
		{
			TryExceptStatement LStatement = (TryExceptStatement)AStatement;
			TryExceptNode LNode = new TryExceptNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.Nodes.Add(CompileFrameNode(APlan, LStatement.TryStatement));

			APlan.EnterErrorContext();
			try
			{
				foreach (GenericErrorHandler LHandler in LStatement.ErrorHandlers)
				{
					ErrorHandlerNode LErrorNode = new ErrorHandlerNode();
					LErrorNode.SetLineInfo(LHandler.LineInfo);
					if (LHandler is SpecificErrorHandler)
						LErrorNode.ErrorType = (Schema.IScalarType)CompileTypeSpecifier(APlan, new ScalarTypeSpecifier(((SpecificErrorHandler)LHandler).ErrorTypeName));
					else
					{
						LErrorNode.ErrorType = APlan.DataTypes.SystemError;
						LErrorNode.IsGeneric = true;
					}
					APlan.AttachDependency((Schema.ScalarType)LErrorNode.ErrorType);
					APlan.Symbols.PushFrame();
					try
					{
						if (LHandler is ParameterizedErrorHandler)
						{
							LErrorNode.VariableName = ((ParameterizedErrorHandler)LHandler).VariableName;
							APlan.Symbols.Push(new Symbol(LErrorNode.VariableName, LErrorNode.ErrorType));
						}
						LErrorNode.Nodes.Add(CompileStatement(APlan, LHandler.Statement));
						LErrorNode.DetermineCharacteristics(APlan);
						LNode.Nodes.Add(CompileDeallocateFrameVariablesNode(APlan, LErrorNode));
					}
					finally
					{
						APlan.Symbols.PopFrame();
					}
				}
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
			finally
			{
				APlan.ExitErrorContext();
			}
		}
		
		public static PlanNode EmitTableToTableValueNode(Plan APlan, TableNode ATableNode)
		{
			PlanNode LNode = new TableToTableValueNode(ATableNode);
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EnsureTableValueNode(Plan APlan, PlanNode APlanNode)
		{
			TableNode LNode = APlanNode as TableNode;
			if (LNode != null)
				return EmitTableToTableValueNode(APlan, LNode);
			return APlanNode;
		}

		public static TableNode EmitTableValueToTableNode(Plan APlan, PlanNode APlanNode)
		{
			TableNode LNode = new TableValueToTableNode();
			LNode.Nodes.Add(APlanNode);
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static TableNode EnsureTableNode(Plan APlan, PlanNode APlanNode)
		{
			TableNode LTableNode = APlanNode as TableNode;
			if (LTableNode == null)
				return EmitTableValueToTableNode(APlan, APlanNode);
			return LTableNode;
		}
		
		protected static PlanNode CompileVariableStatement(Plan APlan, Statement AStatement)
		{
			VariableStatement LStatement = (VariableStatement)AStatement;
			StringCollection LNames = new StringCollection();
			if (!APlan.Symbols.IsValidVariableIdentifier(LStatement.VariableName.Identifier, LNames))
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(LNames[0], LStatement.VariableName.Identifier))
					if (String.Compare(LNames[0], LStatement.VariableName.Identifier) == 0)
						throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier);
					else
						throw new CompilerException(CompilerException.Codes.CreatingHiddenIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier, LNames[0]);
				else
					throw new CompilerException(CompilerException.Codes.CreatingHidingIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier, LNames[0]);
				#else
				throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier);
				#endif
			}

			#if WARNWHENCURSORTYPEVARIABLEINSCOPE			
			if (APlan.Symbols.HasCursorTypeVariables())
				APlan.Messages.Add(new CompilerException(CompilerException.Codes.CursorTypeVariableInScope, CompilerErrorLevel.Warning, LStatement));
			#endif
			
			if (LStatement.TypeSpecifier != null)
			{
				Schema.IDataType LVariableType = CompileTypeSpecifier(APlan, LStatement.TypeSpecifier);
				VariableNode LNode = new VariableNode();
				LNode.SetLineInfo(LStatement.LineInfo);
				LNode.VariableName = LStatement.VariableName.Identifier;
				LNode.VariableType = LVariableType;
				if (LStatement.Expression != null)
				{
					APlan.Symbols.Push(new Symbol(String.Empty, APlan.DataTypes.SystemGeneric));
					try
					{
						LNode.Nodes.Add(EnsureTableValueNode(APlan, CompileTypedExpression(APlan, LStatement.Expression, LNode.VariableType)));
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				LNode.DetermineCharacteristics(APlan);
				APlan.Symbols.Push(new Symbol(LNode.VariableName, LNode.VariableType));
				return LNode;
			}
			else if (LStatement.Expression != null)
			{
				PlanNode LValueNode;
				APlan.Symbols.Push(new Symbol(String.Empty, APlan.DataTypes.SystemGeneric));
				try
				{
					LValueNode = EnsureTableValueNode(APlan, CompileExpression(APlan, LStatement.Expression));
				}
				finally
				{
					APlan.Symbols.Pop();
				}
				if (LValueNode.DataType == null)
					throw new CompilerException(CompilerException.Codes.ExpressionExpected, LStatement.Expression);

				VariableNode LNode = new VariableNode();
				LNode.SetLineInfo(LStatement.LineInfo);
				LNode.VariableName = LStatement.VariableName.Identifier;
				LNode.VariableType = LValueNode.DataType;
				LNode.Nodes.Add(LValueNode);
				LNode.DetermineCharacteristics(APlan);
				APlan.Symbols.Push(new Symbol(LNode.VariableName, LNode.VariableType));
				return LNode;
			}
			else
				throw new CompilerException(CompilerException.Codes.TypeSpecifierExpected, LStatement);
		}
		
		public static PlanNode EmitCatalogIdentiferNode(Plan APlan, Statement AStatement, string AIdentifier)
		{
			return EmitCatalogIdentifierNode(APlan, AStatement, new NameBindingContext(AIdentifier, APlan.NameResolutionPath));
		}
		
		public static PlanNode EmitCatalogIdentifierNode(Plan APlan, Statement AStatement, NameBindingContext AContext)
		{
			ResolveCatalogIdentifier(APlan, AContext);
			if (AContext.Object is Schema.TableVar)
			{
				APlan.AttachDependency(AContext.Object);
				return EmitTableVarNode(APlan, AStatement, AContext.Identifier, (Schema.TableVar)AContext.Object);
			}
			return null;
		}
		
		public static PlanNode EmitIdentifierNode(Plan APlan, string AIdentifier)
		{
			return EmitIdentifierNode(APlan, new EmptyStatement(), AIdentifier);
		}
		
		public static PlanNode EmitIdentifierNode(Plan APlan, Statement AStatement, string AIdentifier)
		{
			NameBindingContext LContext = new NameBindingContext(AIdentifier, APlan.NameResolutionPath);
			PlanNode LNode = EmitIdentifierNode(APlan, AStatement, LContext);
			if (LNode == null)
				if (LContext.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, APlan.CurrentStatement(), AIdentifier, ExceptionUtility.StringsToCommaList(LContext.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, APlan.CurrentStatement(), AIdentifier);
			return LNode;
		}
		
		public static PlanNode EmitIdentifierNode(Plan APlan, NameBindingContext AContext)
		{
			return EmitIdentifierNode(APlan, new EmptyStatement(), AContext);
		}
		
		// When an identifier is encountered ->
		//	The stack is searched for a Symbol with the name specified by the identifier
		//		StackReferenceNode location bound to the entry found in the stack.
		//  The stack is searched for a Symbol with a RowType descendant containing a column with the name specified by the identifier
		//		StackColumnReferenceNode location bound to the entry found in the stack.
		//	The heap is searched for a Schema.Object bound to the name specified by the identifier
		//		If the object found is a base table variable
		//			BaseTableVarNode based on the base table variable
		//		An exception is thrown indicating that the object referenced is not a valid expression term
		//  The server catalog is searched for a Schema.Object bound to the name specified by the identifier, prefaced with the DefaultNameSpace
		//  The server catalog is searched for a Schema.Object bound to the name specified by the identifier	
		//		If the object found is a base table variable 
		//			BaseTableVarNode based on the base table variable found in the catalog
		//		If the object found is a virtual table variable
		//			The plan tree for the virtual table variable is copied into this plan
		//		An exception is thrown indicating that the object referenced is not a valid expression term
		//  A null reference is returned to the calling context indicating the identifier could not be resolved
		public static PlanNode EmitIdentifierNode(Plan APlan, Statement AStatement, NameBindingContext AContext)
		{
			if ((AContext.BindingFlags & NameBindingFlags.Local) != 0)
			{
				int LColumnIndex;
				int LIndex = ResolveVariableIdentifier(APlan, AContext.Identifier, out LColumnIndex, AContext.Names);
				if (LIndex >= 0)
				{
					if (LColumnIndex >= 0)
					{
						Schema.IRowType LRowType = (Schema.IRowType)APlan.Symbols[LIndex].DataType;
						#if USECOLUMNLOCATIONBINDING
						return new StackColumnReferenceNode(LRowType.Columns[LColumnIndex].Name, LRowType.Columns[LColumnIndex].DataType, LIndex, LColumnIndex);
						#else
						return new StackColumnReferenceNode(Schema.Object.IsRooted(AContext.Identifier) ? AContext.Identifier : Schema.Object.EnsureRooted(LRowType.Columns[LColumnIndex].Name), LRowType.Columns[LColumnIndex].DataType, LIndex);
						#endif
					}
						
					return new StackReferenceNode(Schema.Object.IsRooted(AContext.Identifier) ? AContext.Identifier : Schema.Object.EnsureRooted(APlan.Symbols[LIndex].Name), APlan.Symbols[LIndex].DataType, LIndex);
				}
			}
			
			if ((AContext.BindingFlags & NameBindingFlags.Global) != 0)
			{
				if (AContext.Names.Count == 0)
				{
					PlanNode LNode = EmitCatalogIdentifierNode(APlan, AStatement, AContext);
					if (LNode != null)
						return LNode;
				}
				
				if (AContext.Names.Count == 0)
				{
					// If the identifier is unresolved, and there is a default device, attempt an automatic reconciliation				
					if ((APlan.DefaultDeviceName != String.Empty) && (!APlan.InLoadingContext()))
					{
						Schema.Device LDevice = GetDefaultDevice(APlan, false);
						if (LDevice != null)
						{
							Schema.BaseTableVar LTableVar = new Schema.BaseTableVar(AContext.Identifier, new Schema.TableType(), LDevice);
							APlan.CheckDeviceReconcile(LTableVar);
							return EmitCatalogIdentifierNode(APlan, AStatement, AContext);
						}
					}
				}
			}

			// If the identifier could not be resolved, return a null reference
			return null;
		}
		
		public static PlanNode EmitTableVarNode(Plan APlan, Schema.TableVar ATableVar)
		{
			return EmitTableVarNode(APlan, new EmptyStatement(), ATableVar.Name, ATableVar);
		}
		
		public static PlanNode EmitTableVarNode(Plan APlan, string AIdentifier, Schema.TableVar ATableVar)
		{
			return EmitTableVarNode(APlan, new EmptyStatement(), AIdentifier, ATableVar);
		}
		
		public static PlanNode EmitTableVarNode(Plan APlan, Statement AStatement, string AIdentifier, Schema.TableVar ATableVar)
		{
			APlan.SetIsLiteral(false);
			if (ATableVar is Schema.BaseTableVar)
				return EmitBaseTableVarNode(APlan, AStatement, AIdentifier, (Schema.BaseTableVar)ATableVar);
			else
				return EmitDerivedTableVarNode(APlan, AStatement, (Schema.DerivedTableVar)ATableVar);
		}
		
		public static int ResolveVariableIdentifier(Plan APlan, string AIdentifier, out int AColumnIndex)
		{
			StringCollection LNames = new StringCollection();
			int LIndex = ResolveVariableIdentifier(APlan, AIdentifier, out AColumnIndex, LNames);
			if (LIndex < 0)
				if (LNames.Count > 0)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, APlan.CurrentStatement(), AIdentifier, ExceptionUtility.StringsToCommaList(LNames));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, APlan.CurrentStatement(), AIdentifier);
			return LIndex;
		}
		
		/// <summary> Returns the index of a data object on the stack, -1 if unable to resolve. </summary>
		/// <param name="AColumnIndex"> If the variable resolves to a column reference, AColumnIndex will contain the column index, -1 otherwise </param>
		public static int ResolveVariableIdentifier(Plan APlan, string AIdentifier, out int AColumnIndex, StringCollection ANames)
		{
			return APlan.Symbols.ResolveVariableIdentifier(AIdentifier, out AColumnIndex, ANames);
		}
		
		public static Schema.Object ResolveCatalogObjectSpecifier(Plan APlan, string ASpecifier)
		{
			return ResolveCatalogObjectSpecifier(APlan, ASpecifier, true);
		}
		
		public static Schema.Object ResolveCatalogObjectSpecifier(Plan APlan, string ASpecifier, bool AMustResolve)
		{
			CatalogObjectSpecifier LSpecifier = new Parser().ParseCatalogObjectSpecifier(ASpecifier);
			if (!LSpecifier.IsOperator)
				return Compiler.ResolveCatalogIdentifier(APlan, LSpecifier.ObjectName, AMustResolve);
			else
				return Compiler.ResolveOperatorSpecifier(APlan, new OperatorSpecifier(LSpecifier.ObjectName, LSpecifier.FormalParameterSpecifiers), AMustResolve);
		}
																				
		public static Schema.Object ResolveCatalogIdentifier(Plan APlan, string AIdentifier)
		{
			return ResolveCatalogIdentifier(APlan, AIdentifier, true);
		}

		public static Schema.Object ResolveCatalogIdentifier(Plan APlan, string AIdentifier, bool AMustResolve)
		{
			//long LStartTicks = TimingUtility.CurrentTicks;
			NameBindingContext LContext = new NameBindingContext(AIdentifier, APlan.NameResolutionPath);
			ResolveCatalogIdentifier(APlan, LContext);
			
			if (AMustResolve && (LContext.Object == null))
				if (LContext.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, APlan.CurrentStatement(), AIdentifier, ExceptionUtility.StringsToCommaList(LContext.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, APlan.CurrentStatement(), AIdentifier);
			//APlan.Accumulator += (TimingUtility.CurrentTicks - LStartTicks);
			return LContext.Object;
		}
		
		public static Schema.Object ResolveCatalogIdentifier(Plan APlan, IdentifierExpression AExpression)
		{
			//long LStartTicks = TimingUtility.CurrentTicks;
			NameBindingContext LContext = new NameBindingContext(AExpression.Identifier, APlan.NameResolutionPath);
			ResolveCatalogIdentifier(APlan, LContext);
			
			if (LContext.Object == null)
				if (LContext.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, AExpression, AExpression.Identifier, ExceptionUtility.StringsToCommaList(LContext.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AExpression, AExpression.Identifier);
			//APlan.Accumulator += (TimingUtility.CurrentTicks - LStartTicks);
			return LContext.Object;
		}
		
		/// <summary>Attempts to resolve the given name binding context.</summary>
		/// <remarks>
		/// This is the primary catalog identifier resolution procedure.  This procedure will not throw an error if it is unable to
		/// resolve the identifier.  If the identifier is ambiguous, the IsAmbiguous flag will be set in the NameBindingContext.
		/// </remarks>
		public static void ResolveCatalogIdentifier(Plan APlan, NameBindingContext AContext)
		{
			try
			{
				// if this process is part of an application transaction, search for an application transaction variable named AIdentifier
				if ((APlan.ApplicationTransactionID != Guid.Empty) && (!APlan.InLoadingContext()))
				{
					ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
					try
					{
						if (!LTransaction.IsGlobalContext && !LTransaction.IsLookup)
						{
							int LIndex = LTransaction.TableMaps.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
							if (AContext.IsAmbiguous)
								return;
								
							if (LIndex >= 0)
							{
								NameBindingContext LContext = new NameBindingContext(Schema.Object.EnsureRooted(LTransaction.TableMaps[LIndex].TableVar.Name), APlan.NameResolutionPath);
								ResolveCatalogIdentifier(APlan, LContext);
								if (LContext.Object != null)
								{
									AContext.SetBindingDataFromContext(LContext);
									return;
								}
							}
						}
					}
					finally
					{
						Monitor.Exit(LTransaction);
					}
				}
				
				// search for a session table variable named AIdentifier
				lock (APlan.PlanSessionObjects)
				{
					int LIndex = APlan.PlanSessionObjects.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
					if (AContext.IsAmbiguous)
						return;
						
					if (LIndex >= 0)
					{
						NameBindingContext LContext = new NameBindingContext(Schema.Object.EnsureRooted(((Schema.SessionObject)APlan.PlanSessionObjects[LIndex]).GlobalName), APlan.NameResolutionPath);
						ResolveCatalogIdentifier(APlan, LContext);
						if (LContext.IsAmbiguous)
							return;
							
						if (LContext.Object != null)
						{
							AContext.SetBindingDataFromContext(LContext);
							return;
						}
					}
				}
				
				lock (APlan.SessionObjects)
				{
					int LIndex = APlan.SessionObjects.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
					if (LIndex >= 0)
					{
						NameBindingContext LContext = new NameBindingContext(Schema.Object.EnsureRooted(((Schema.SessionObject)APlan.SessionObjects[LIndex]).GlobalName), APlan.NameResolutionPath);
						ResolveCatalogIdentifier(APlan, LContext);
						if (LContext.IsAmbiguous)
							return;
							
						if (LContext.Object != null)
						{
							AContext.SetBindingDataFromContext(LContext);
							return;
						}
					}
				}
							
				lock (APlan.PlanCatalog)
				{				
					int LIndex = APlan.PlanCatalog.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
					if (AContext.IsAmbiguous)
						return;
						
					if (LIndex >= 0)
					{
						AContext.Object = APlan.PlanCatalog[LIndex];
						APlan.AcquireCatalogLock(AContext.Object, LockMode.Shared);
						return;
					}
				}

				lock (APlan.Catalog)
				{
					AContext.Object = APlan.CatalogDeviceSession.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
					//int LIndex = APlan.Catalog.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
					if (AContext.IsAmbiguous)
						return;
						
					//if (LIndex >= 0)
					if (AContext.Object != null)
					{
						//AContext.Object = APlan.Catalog[LIndex];
						APlan.AcquireCatalogLock(AContext.Object, LockMode.Shared);
						return;
					}
				}
			}
			finally
			{
				// Reinfer view references
				Schema.DerivedTableVar LDerivedTableVar = AContext.Object as Schema.DerivedTableVar;
				if ((LDerivedTableVar != null) && LDerivedTableVar.ShouldReinferReferences)
					ReinferViewReferences(APlan, LDerivedTableVar);

				// AT Variable Enlistment
				Schema.TableVar LTableVar = AContext.Object as Schema.TableVar;
				if ((LTableVar != null) && (APlan.ApplicationTransactionID != Guid.Empty) && (!APlan.InLoadingContext()))
				{
					ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
					try
					{
						if (!LTableVar.IsATObject)
						{
							if (LTableVar.ShouldTranslate && !LTransaction.IsGlobalContext && !LTransaction.IsLookup)
								AContext.Object = LTransaction.AddTableVar(APlan.ServerProcess, LTableVar);
						}
						else if (!APlan.ServerProcess.InAddingTableVar) // This check prevents EnsureTableVar from being called while the map is still being created
							LTransaction.EnsureATTableVarMapped(APlan.ServerProcess, LTableVar);
					}
					finally
					{
						Monitor.Exit(LTransaction);
					}
				}
			}
		}
		
		protected static Schema.Property ResolvePropertyReference(Plan APlan, ref Schema.ScalarType AScalarType, string AIdentifier, out int ARepresentationIndex, out int APropertyIndex)
		{
			Schema.Representation LRepresentation;
			for (int LRepresentationIndex = 0; LRepresentationIndex < AScalarType.Representations.Count; LRepresentationIndex++)
			{
				LRepresentation = AScalarType.Representations[LRepresentationIndex];
				int LPropertyIndex = LRepresentation.Properties.IndexOf(AIdentifier);
				if (LPropertyIndex >= 0)
				{
					ARepresentationIndex = LRepresentationIndex;
					APropertyIndex = LPropertyIndex;
					return LRepresentation.Properties[LPropertyIndex];
				}
			}

			#if USETYPEINHERITANCE
			Schema.Property LProperty;
			for (int LIndex = 0; LIndex < AScalarType.ParentTypes.Count; LIndex++)
			{
				Schema.ScalarType LParentType = (Schema.ScalarType)AScalarType.ParentTypes[LIndex];
				LProperty = ResolvePropertyReference(APlan, ref LParentType, AIdentifier, out ARepresentationIndex, out APropertyIndex);
				if (LProperty != null)
				{
					AScalarType = LParentType;
					return LProperty;
				}
			}			
			#endif

			ARepresentationIndex = -1;
			APropertyIndex = -1;			
			return null;
		}
		
		protected static PlanNode CompilePropertyReference(Plan APlan, PlanNode APlanNode, IdentifierExpression AExpression)
		{
			int LPropertyIndex;
			int LRepresentationIndex;
			Schema.Property LProperty;
			Schema.ScalarType LScalarType = (Schema.ScalarType)APlanNode.DataType;
			LProperty = ResolvePropertyReference(APlan, ref LScalarType, AExpression.Identifier, out LRepresentationIndex, out LPropertyIndex);
			if (LProperty != null)
			{
				if (!APlan.IsAssignmentTarget)
					return EmitPropertyReadNode(APlan, APlanNode, LScalarType, LProperty);
				else
				{
					PropertyReferenceNode LNode = new PropertyReferenceNode();
					LNode.ScalarType = LScalarType;
					LNode.DataType = LProperty.DataType;
					LNode.RepresentationIndex = LRepresentationIndex;
					LNode.PropertyIndex = LPropertyIndex;
					LNode.Nodes.Add(APlanNode);
					LNode.DetermineCharacteristics(APlan);
					return LNode;
				}
			}
			
			return null;
		}
		
		public static PlanNode EmitPropertyReadNode(Plan APlan, PlanNode APlanNode, Schema.ScalarType AScalarType, Schema.Property AProperty)
		{
			AProperty.ResolveReadAccessor(APlan.CatalogDeviceSession);
			PlanNode LNode = BuildCallNode(APlan, null, AProperty.ReadAccessor, new PlanNode[]{APlanNode});
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode EmitPropertyReadNode(Plan APlan, PropertyReferenceNode ANode)
		{
			Schema.Property LProperty = ANode.ScalarType.Representations[ANode.RepresentationIndex].Properties[ANode.PropertyIndex];
			return EmitPropertyReadNode(APlan, ANode.Nodes[0], ANode.ScalarType, LProperty);
		}
		
		protected static PlanNode CompileDotInvocation(Plan APlan, PlanNode APlanNode, CallExpression AExpression)
		{
			PlanNode[] LNodes = new PlanNode[AExpression.Expressions.Count + 1];
			LNodes[0] = APlanNode;
			CallExpression LBindingExpression = new CallExpression();
			LBindingExpression.Line = AExpression.Line;
			LBindingExpression.LinePos = AExpression.LinePos;
			LBindingExpression.Identifier = AExpression.Identifier;
			LBindingExpression.Modifiers = AExpression.Modifiers;
			ValueExpression LThisExpression = new ValueExpression("");
			LThisExpression.Line = AExpression.Line;
			LThisExpression.LinePos = AExpression.LinePos;
			LBindingExpression.Expressions.Add(LThisExpression);
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				LNodes[LIndex + 1] = CompileExpression(APlan, AExpression.Expressions[LIndex]);
				LBindingExpression.Expressions.Add(AExpression.Expressions[LIndex]);
			}
			OperatorBindingContext LContext = new OperatorBindingContext(LBindingExpression, AExpression.Identifier, APlan.NameResolutionPath, SignatureFromArguments(LNodes), false);
			PlanNode LPlanNode = EmitCallNode(APlan, LContext, LNodes);
			if (LPlanNode == null)
			{
				LNodes[0] = EmitParameterNode(APlan, Modifier.Var, LNodes[0]);
				OperatorBindingContext LSubContext = new OperatorBindingContext(LBindingExpression, AExpression.Identifier, APlan.NameResolutionPath, SignatureFromArguments(LNodes), false);
				LPlanNode = EmitCallNode(APlan, LSubContext, LNodes);
				if (LPlanNode == null)
					CheckOperatorResolution(APlan, LContext); // Throw the error based on the original signature
			}
			return LPlanNode;
		}
		
		protected static PlanNode CompileDotOperator(Plan APlan, PlanNode APlanNode, Expression AExpression)
		{
			if ((APlanNode.DataType is Schema.IScalarType) && (AExpression is IdentifierExpression))
			{
				return CompilePropertyReference(APlan, APlanNode, (IdentifierExpression)AExpression);
			}
			else if ((APlanNode.DataType is Schema.IRowType) && (AExpression is IdentifierExpression))
			{
				ColumnExpression LColumnExpression = new ColumnExpression(Schema.Object.EnsureRooted(((IdentifierExpression)AExpression).Identifier));
				int LIndex = ((Schema.IRowType)APlanNode.DataType).Columns.IndexOfName(LColumnExpression.ColumnName);
				if (LIndex >= 0)
				{
					LColumnExpression.Line = AExpression.Line;
					LColumnExpression.LinePos = AExpression.LinePos;
					return EmitColumnExtractorNode(APlan, AExpression, LColumnExpression, APlanNode);
				}
				else
					return null;
			}
			#if ALLOWIMPLICITROWEXTRACTOR
			else if ((APlanNode.DataType is Schema.ITableType) && (AExpression is IdentifierExpression))
			{
				ColumnExpression LColumnExpression = new ColumnExpression(Schema.Object.EnsureRooted(((IdentifierExpression)AExpression).Identifier));
				int LIndex = ((Schema.ITableType)APlanNode.DataType).Columns.IndexOfName(LColumnExpression.ColumnName);
				if (LIndex >= 0)
				{
					LColumnExpression.Line = AExpression.Line;
					LColumnExpression.LinePos = AExpression.LinePos;
					return EmitColumnExtractorNode(APlan, AExpression, LColumnExpression, EmitRowExtractorNode(APlan, AExpression, APlanNode));
				}
				else
					return null;
			}
			#endif
			else if (AExpression is CallExpression)
			{
				return CompileDotInvocation(APlan, APlanNode, (CallExpression)AExpression);
			}
			else if (AExpression is QualifierExpression)
			{
				QualifierExpression LQualifierExpression = (QualifierExpression)AExpression;
				PlanNode LNode = CompileDotOperator(APlan, APlanNode, LQualifierExpression.LeftExpression);
				if (LNode == null) 
					if (LQualifierExpression.LeftExpression is IdentifierExpression)
						return CompileDotOperator(APlan, APlanNode, CollapseQualifierExpression(APlan, (IdentifierExpression)LQualifierExpression.LeftExpression, LQualifierExpression.RightExpression));
					else
						return null;
				else
					return CompileDotOperator(APlan, LNode, LQualifierExpression.RightExpression);
			}
			else
				return null;
		}
		
		protected static Expression CollapseQualifierExpression(Plan APlan, IdentifierExpression AIdentifierExpression, Expression AExpression)
		{
			if (AExpression is IdentifierExpression)
			{
				IdentifierExpression LExpression = new IdentifierExpression();
				LExpression.Line = AIdentifierExpression.Line;
				LExpression.LinePos = AIdentifierExpression.LinePos;
				LExpression.Modifiers = AExpression.Modifiers;
				LExpression.Identifier = Schema.Object.Qualify(((IdentifierExpression)AExpression).Identifier, AIdentifierExpression.Identifier);
				return LExpression;
			}
			else if (AExpression is CallExpression)
			{
				CallExpression LCallExpression = (CallExpression)AExpression;
				CallExpression LExpression = new CallExpression();
				LExpression.Line = AIdentifierExpression.Line;
				LExpression.LinePos = AIdentifierExpression.LinePos;
				LExpression.Modifiers = AExpression.Modifiers;
				LExpression.Identifier = Schema.Object.Qualify(LCallExpression.Identifier, AIdentifierExpression.Identifier);
				LExpression.Expressions.AddRange(LCallExpression.Expressions);
				return LExpression;
			}
			else if ((AExpression is ColumnExtractorExpression) && (((ColumnExtractorExpression)AExpression).Columns.Count == 1))
			{
				ColumnExtractorExpression LColumnExtractorExpression = (ColumnExtractorExpression)AExpression;
				ColumnExtractorExpression LExpression = new ColumnExtractorExpression();
				LExpression.Line = LColumnExtractorExpression.Line;
				LExpression.LinePos = LColumnExtractorExpression.LinePos;
				LExpression.Modifiers = AExpression.Modifiers;
				LExpression.Columns.Add(new ColumnExpression(Schema.Object.Qualify(LColumnExtractorExpression.Columns[0].ColumnName, AIdentifierExpression.Identifier)));
				LExpression.Expression = LColumnExtractorExpression.Expression;
				return LExpression;
			}
			else if (AExpression is QualifierExpression)
			{
				QualifierExpression LQualifierExpression = (QualifierExpression)AExpression;
				QualifierExpression LExpression = new QualifierExpression();
				LExpression.Line = LQualifierExpression.Line;
				LExpression.LinePos = LQualifierExpression.LinePos;
				LExpression.Modifiers = AExpression.Modifiers;
				LExpression.LeftExpression = CollapseQualifierExpression(APlan, AIdentifierExpression, LQualifierExpression.LeftExpression);
				LExpression.RightExpression = LQualifierExpression.RightExpression;
				return LExpression;
			}
			else
				throw new CompilerException(CompilerException.Codes.UnableToCollapseQualifierExpression, AExpression);
		}
		
		protected static Expression CollapseColumnExtractorExpression(Expression AExpression)
		{
			// if the right side of the qualifier expression is a column extractor, the left side must be an identifier
			QualifierExpression LQualifierExpression = AExpression as QualifierExpression;
			if (LQualifierExpression != null)
			{
				Expression LRightExpression = CollapseColumnExtractorExpression(LQualifierExpression.RightExpression);
				ColumnExtractorExpression LColumnExtractorExpression = LRightExpression as ColumnExtractorExpression;
				IdentifierExpression LIdentifierExpression = LQualifierExpression.LeftExpression as IdentifierExpression;
				if ((LIdentifierExpression != null) && (LColumnExtractorExpression != null))
				{
					LColumnExtractorExpression.Columns[0].ColumnName = Schema.Object.Qualify(LColumnExtractorExpression.Columns[0].ColumnName, LIdentifierExpression.Identifier);
					return LColumnExtractorExpression;
				}
			}
			
			return AExpression;
		}
		
		protected static PlanNode CompileQualifierExpression(Plan APlan, QualifierExpression AExpression)
		{
			return CompileQualifierExpression(APlan, AExpression, false);
		}
		
		//	Left Side Possibilities ->
		//		IdentifierExpression
		//		Any other expression
		//
		//	Right Side Possibilities ->
		//		IdentifierExpression
		//		CallExpression
		//		QualifierExpression
		//		<error>
		//		
		//	Identifier Resolution ->
		//		The stack is searched for a symbol name bound to the given identifier
		//		The stack is searched for a symbol with a RowType value containing a column named the given identifier
		//		The heap is searched for a SchemaObject named Identifier
		//		The catalog is searched for a SchemaObject named DefaultNameSpace.Identifier
		//		The catalog is searched for a SchemaObject named Identifier
		//		
		//	Invocation Resolution ->
		//		The catalog is searched for an operator named DefaultNameSpace.Identifier with the given signature
		//		The catalog is searched for an operator named Identifier with the given signature
		//		
		//	Dot Qualifier Compilation ->
		//		if the left side is an identifier
		//			if the left side cannot be resolved
		//				if the left side can be combined with the right side
		//					combine (change the parse tree) and resume compilation one level up
		//				else
		//					throw (unable to resolve or ambiguous name)
		//			else
		//				compile the left side
		//		else
		//			compile the left side
		//			
		//		if the left side is a scalar and the right side is an identifier
		//			if the right side is a component of a possrep of the type of the left side
		//				if this expression is the left subtree of an assignment statement
		//					invoke the write accessor of the component, passing the left side of the dot, and the right side of the assignment
		//				else
		//					invoke the read accessor of the component, passing the left side of the dot
		//			else
		//				throw (unable to resolve property reference)
		//		else if the right side is an operator invocation
		//			attempt to resolve using the signature of the invocation prepended with the type of the left side
		//		else
		//			throw (unable to resolve or ambiguous name)
		//	
		protected static PlanNode CompileQualifierExpression(Plan APlan, QualifierExpression AExpression, bool AIsStatementContext)
		{
			PlanNode LNode;
			PlanNode LLeftNode;

			if (AExpression.RightExpression.Modifiers == null)
				AExpression.RightExpression.Modifiers = AExpression.Modifiers;
				
			Schema.Object LCurrentCreationObject = APlan.CurrentCreationObject();
			Schema.Object LDummyCreationObject = null;
			if (LCurrentCreationObject != null)
			{
				Schema.CatalogObject LCurrentCatalogCreationObject = LCurrentCreationObject as Schema.CatalogObject;
				if (LCurrentCatalogCreationObject != null)
				{
					if (LCurrentCatalogCreationObject is Schema.Operator)
					{
						Schema.Operator LDummyOperator = new Schema.Operator(LCurrentCreationObject.Name);
						LDummyOperator.SessionObjectName = LCurrentCatalogCreationObject.SessionObjectName;
						LDummyOperator.SourceOperatorName = ((Schema.Operator)LCurrentCreationObject).SourceOperatorName;
						LDummyOperator.Library = LCurrentCreationObject.Library;
						LDummyOperator.IsGenerated = LCurrentCreationObject.IsGenerated;
						LDummyCreationObject = LDummyOperator;
					}
					else
					{
						Schema.BaseTableVar LDummyCatalogCreationObject = new Schema.BaseTableVar(LCurrentCreationObject.Name);
						LDummyCatalogCreationObject.SessionObjectName = LCurrentCatalogCreationObject.SessionObjectName;
						if (LCurrentCreationObject is Schema.TableVar)
							LDummyCatalogCreationObject.SourceTableName = ((Schema.TableVar)LCurrentCreationObject).SourceTableName;
						LDummyCatalogCreationObject.Library = LCurrentCreationObject.Library;
						LDummyCatalogCreationObject.IsGenerated = LCurrentCreationObject.IsGenerated;
						LDummyCreationObject = LDummyCatalogCreationObject;
					}
				}
				else
				{
					LDummyCreationObject = new Schema.TableVarColumn(new Schema.Column(LCurrentCreationObject.Name, APlan.DataTypes.SystemScalar));
				}
			}
			bool LObjectPopped = false;
			if (LDummyCreationObject != null)
				APlan.PushCreationObject(LDummyCreationObject); // Push a dummy object to track dependencies hit for the left side of the expression
			try
			{
				if (AExpression.LeftExpression is IdentifierExpression)
				{
					LLeftNode = EmitIdentifierNode(APlan, AExpression.LeftExpression, new NameBindingContext(((IdentifierExpression)AExpression.LeftExpression).Identifier, APlan.NameResolutionPath, NameBindingFlags.Local));
					if (LLeftNode == null)
					{
						QualifierExpression LExpression = AExpression;
						while ((LLeftNode == null) && ((LExpression.RightExpression is IdentifierExpression) || ((LExpression.RightExpression is QualifierExpression) && (((QualifierExpression)LExpression.RightExpression).LeftExpression is IdentifierExpression))))
						{
							Expression LCollapsedExpression = CollapseQualifierExpression(APlan, (IdentifierExpression)LExpression.LeftExpression, LExpression.RightExpression);
							IdentifierExpression LCollapsedIdentifier = LCollapsedExpression as IdentifierExpression;
							if (LCollapsedIdentifier != null)
							{
								LLeftNode = EmitIdentifierNode(APlan, LCollapsedIdentifier, new NameBindingContext(LCollapsedIdentifier.Identifier, APlan.NameResolutionPath, NameBindingFlags.Local));
								if (LLeftNode != null)
								{
									if ((LDummyCreationObject != null) && LDummyCreationObject.HasDependencies())
										APlan.AttachDependencies(LDummyCreationObject.Dependencies);
									return LLeftNode;
								}
								else
									break;
							}
							
							LExpression = (QualifierExpression)LCollapsedExpression;
							LCollapsedIdentifier = LExpression.LeftExpression as IdentifierExpression;
							if (LCollapsedIdentifier != null)
							{
								LLeftNode = EmitIdentifierNode(APlan, LCollapsedIdentifier, new NameBindingContext(LCollapsedIdentifier.Identifier, APlan.NameResolutionPath, NameBindingFlags.Local));
								if (LLeftNode != null)
									AExpression = LExpression;
							}
						}
						
						if (LLeftNode == null)
						{
							LLeftNode = EmitIdentifierNode(APlan, AExpression.LeftExpression, new NameBindingContext(((IdentifierExpression)AExpression.LeftExpression).Identifier, APlan.NameResolutionPath, NameBindingFlags.Global));
							if (LLeftNode == null)
							{
								if (LDummyCreationObject != null)
								{
									APlan.PopCreationObject();
									LObjectPopped = true;
								}
								return CompileExpression(APlan, CollapseQualifierExpression(APlan, (IdentifierExpression)AExpression.LeftExpression, AExpression.RightExpression), AIsStatementContext);
							}
						}
					}
				}
				else
					LLeftNode = CompileExpression(APlan, AExpression.LeftExpression, AIsStatementContext);
					
				LNode = CompileDotOperator(APlan, LLeftNode, AExpression.RightExpression);
			}
			finally
			{
				if ((LDummyCreationObject != null) && !LObjectPopped)
					APlan.PopCreationObject();
			}

			if (LNode == null) 
				if (AExpression.LeftExpression is IdentifierExpression)
					return CompileExpression(APlan, CollapseQualifierExpression(APlan, (IdentifierExpression)AExpression.LeftExpression, AExpression.RightExpression), AIsStatementContext);
				else
					throw new CompilerException(CompilerException.Codes.InvalidQualifier, AExpression, new D4TextEmitter().Emit(AExpression.RightExpression));
			else
			{
				if (LDummyCreationObject != null) 
				{
					if (LDummyCreationObject.HasDependencies())
						APlan.AttachDependencies(LDummyCreationObject.Dependencies);
					Schema.Operator LDummyCreationOperator = LDummyCreationObject as Schema.Operator;
					if (LDummyCreationOperator != null)
					{
						APlan.SetIsLiteral(LDummyCreationOperator.IsLiteral);
						APlan.SetIsFunctional(LDummyCreationOperator.IsFunctional);
						APlan.SetIsDeterministic(LDummyCreationOperator.IsDeterministic);
						APlan.SetIsRepeatable(LDummyCreationOperator.IsRepeatable);
						APlan.SetIsNilable(LDummyCreationOperator.IsNilable);
					}
				}
				
				return LNode;                                     
			}
		}
		
		// V := <expression> ::=
		// var LV := <expression>;
		// delete V;
		// insert LV into V;
		
		protected static PlanNode EmitTableAssignmentNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, TableNode ATargetNode)
		{
			if (!(ASourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, AStatement, ASourceNode.DataType.Name, ATargetNode.DataType.Name);
				
			//Schema.BaseTableVar LTempTableVar;
			FrameNode LFrameNode = new FrameNode();
			LFrameNode.SetLineInfo(AStatement.LineInfo);
			BlockNode LBlockNode = new BlockNode();
			LBlockNode.SetLineInfo(AStatement.LineInfo);
			LFrameNode.Nodes.Add(LBlockNode);
			APlan.Symbols.PushFrame();
			try
			{
				VariableNode LNode = new VariableNode(Schema.Object.NameFromGuid(Guid.NewGuid()), ASourceNode.DataType);
				LNode.SetLineInfo(AStatement.LineInfo);
				LNode.Nodes.Add(EnsureTableValueNode(APlan, ASourceNode));
				LNode.DetermineCharacteristics(APlan);
				LBlockNode.Nodes.Add(LNode);
				APlan.Symbols.Push(new Symbol(LNode.VariableName, LNode.VariableType));
				
				// Delete the data from the target variable
				DeleteNode LDeleteNode = new DeleteNode();
				LDeleteNode.SetLineInfo(AStatement.LineInfo);
				LDeleteNode.Nodes.Add(ATargetNode);
				LDeleteNode.DetermineDataType(APlan);
				LDeleteNode.DetermineCharacteristics(APlan);
				LBlockNode.Nodes.Add(LDeleteNode);
				
				// Insert the data from the temporary variable into the target variable
				InsertNode LInsertNode = new InsertNode();
				LInsertNode.SetLineInfo(AStatement.LineInfo);
				LInsertNode.Nodes.Add(EnsureTableNode(APlan, EmitIdentifierNode(APlan, LNode.VariableName)));
				LInsertNode.Nodes.Add(EmitInsertConditionNode(APlan, ATargetNode));
				LInsertNode.DetermineDataType(APlan);
				LInsertNode.DetermineCharacteristics(APlan);
				LBlockNode.Nodes.Add(LInsertNode);
				
				LBlockNode.DetermineCharacteristics(APlan);
				LFrameNode.DetermineCharacteristics(APlan);
				return LFrameNode;
			}
			finally
			{
				APlan.Symbols.PopFrame();
			}
		}
		
		protected static PlanNode EmitStackReferenceAssignmentNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, StackReferenceNode ATargetNode)
		{
			if (!ASourceNode.DataType.Is(ATargetNode.DataType))
			{
				ConversionContext LContext = FindConversionPath(APlan, ASourceNode.DataType, ATargetNode.DataType);
				CheckConversionContext(APlan, LContext);
				ASourceNode = ConvertNode(APlan, ASourceNode, LContext);
			}
			
			if (ASourceNode is TableNode)
				ASourceNode = EmitTableToTableValueNode(APlan, (TableNode)ASourceNode);

			AssignmentNode LNode = new AssignmentNode(ATargetNode, Upcast(APlan, ASourceNode, ATargetNode.DataType));
			if (AStatement != null)
				LNode.SetLineInfo(AStatement.LineInfo);
			else
				LNode.IsBreakable = false;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		protected static PlanNode EmitColumnExtractorAssignmentNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, ExtractColumnNode ATargetNode)
		{
			// If the column extractor's source is an indexer, we need to get to the underlying table node, or the update will compile as a row variable update, i/o a restricted table update
			PlanNode LTargetNode = ATargetNode.Nodes[0];
			string LIdentifier = ATargetNode.Identifier;
			RowRenameNode LRowRenameNode = LTargetNode as RowRenameNode;
			if ((LRowRenameNode != null) && !LRowRenameNode.ShouldEmit)
			{
				// If there is a row rename that is part of a table indexer, undo the rename
				int LColumnIndex = LRowRenameNode.DataType.Columns.IndexOfName(LIdentifier);
				LTargetNode = LTargetNode.Nodes[0];
				LIdentifier = Schema.Object. EnsureRooted(((Schema.RowType)LTargetNode.DataType).Columns[LColumnIndex].Name);
			}
			if (LTargetNode is ExtractRowNode)
				LTargetNode = LTargetNode.Nodes[0];

			DelimitedBlockNode LBlockNode = new DelimitedBlockNode();
			LBlockNode.SetLineInfo(AStatement.LineInfo);
			VariableNode LVariableNode = new VariableNode(Schema.Object.GetUniqueName(), ASourceNode.DataType);
			LVariableNode.SetLineInfo(AStatement.LineInfo);
			LVariableNode.Nodes.Add(ASourceNode);
			LVariableNode.DetermineCharacteristics(APlan);
			LBlockNode.Nodes.Add(LVariableNode);
			APlan.Symbols.Push(new Symbol(LVariableNode.VariableName, LVariableNode.VariableType));

			UpdateStatement LStatement = new UpdateStatement((Expression)LTargetNode.EmitStatement(EmitMode.ForCopy), new UpdateColumnExpression[]{new UpdateColumnExpression(new IdentifierExpression(LIdentifier), new IdentifierExpression(LVariableNode.VariableName))});
			LStatement.Line = AStatement.Line;
			LStatement.LinePos = AStatement.LinePos;
			LBlockNode.Nodes.Add(CompileUpdateStatement(APlan, LStatement));
			return LBlockNode;
		}
		
		public static PlanNode EmitPropertyWriteNode(Plan APlan, Statement AStatement, Schema.Property AProperty, PlanNode ASourceNode, PlanNode ATargetNode)
		{
			AProperty.ResolveWriteAccessor(APlan.CatalogDeviceSession);
			PlanNode LNode = BuildCallNode(APlan, AStatement, AProperty.WriteAccessor, new PlanNode[]{ATargetNode, ASourceNode});
			LNode.Nodes.Clear();
			if (!ASourceNode.DataType.Is(AProperty.DataType))
			{
				ConversionContext LContext = FindConversionPath(APlan, ASourceNode.DataType, AProperty.DataType);
				CheckConversionContext(APlan, LContext);
				ASourceNode = ConvertNode(APlan, ASourceNode, LContext);
			}
			
			if (ATargetNode is PropertyReferenceNode)
			{
				LNode.Nodes.Add(Upcast(APlan, EmitPropertyReadNode(APlan, (PropertyReferenceNode)ATargetNode), AProperty.Representation.ScalarType));
				LNode.Nodes.Add(Upcast(APlan, ASourceNode, AProperty.DataType));
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				return EmitPropertyReferenceAssignmentNode(APlan, AStatement, LNode, (PropertyReferenceNode)ATargetNode); 
			}
			else if (ATargetNode is StackReferenceNode)
			{
				LNode.Nodes.Add(Upcast(APlan, ATargetNode, AProperty.Representation.ScalarType));
				LNode.Nodes.Add(Upcast(APlan, ASourceNode, AProperty.DataType));
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				AssignmentNode LAssignmentNode = new AssignmentNode(ATargetNode, Upcast(APlan, LNode, ATargetNode.DataType));
				if (AStatement != null)
					LAssignmentNode.SetLineInfo(AStatement.LineInfo);
				else
					LAssignmentNode.IsBreakable = false;
				LAssignmentNode.DetermineDataType(APlan);
				LAssignmentNode.DetermineCharacteristics(APlan);
				return LAssignmentNode;
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, AStatement);
		}
		
		protected static PlanNode EmitPropertyReferenceAssignmentNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, PropertyReferenceNode ATargetNode)
		{
			Schema.Property LProperty = ATargetNode.ScalarType.Representations[ATargetNode.RepresentationIndex].Properties[ATargetNode.PropertyIndex];
			return EmitPropertyWriteNode(APlan, AStatement, LProperty, ASourceNode, ATargetNode.Nodes[0]);
		}
		
		protected static PlanNode EmitAssignmentNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, PlanNode ATargetNode)
		{
			if (ATargetNode is TableNode)
				return EmitTableAssignmentNode(APlan, AStatement, ASourceNode, (TableNode)ATargetNode);
			else if (ATargetNode is StackReferenceNode)
				return EmitStackReferenceAssignmentNode(APlan, AStatement, ASourceNode, (StackReferenceNode)ATargetNode);
			else if (ATargetNode is ExtractColumnNode)
				return EmitColumnExtractorAssignmentNode(APlan, AStatement, ASourceNode, (ExtractColumnNode)ATargetNode);
			else if (ATargetNode is PropertyReferenceNode)
				return EmitPropertyReferenceAssignmentNode(APlan, AStatement, ASourceNode, (PropertyReferenceNode)ATargetNode);
			else
				throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, AStatement);
		}
		
		//	Assignment Statement ->
		//
		//		variable assignment ->
		//			The left hand side directly references a variable ->
		//				if the Variable is a table type ->
		//					the assignment is compiled to an insert statement of the form
		//						V := <expression> ::=
		//							var temp;
		//							insert <expression> into temp;
		//							delete v;
		//							insert temp into v;
		//				else
		//					the assignment statement uses an AssignmentNode
		//
		//		property assignment ->
		//			The left hand side references a property of a scalar,
		//			so the assignment statement is compiled to an assignment of the form
		//				V.X := <expression> ::= V := SetX(V, <expression>)
		protected static PlanNode CompileAssignmentStatement(Plan APlan, Statement AStatement)
		{
			AssignmentStatement LStatement = (AssignmentStatement)AStatement;
			PlanNode LTargetNode = null;
			APlan.PushStatementContext(new StatementContext(StatementType.Assignment));
			try
			{
				LTargetNode = CompileExpression(APlan, LStatement.Target);
			}
			finally
			{
				APlan.PopStatementContext();
			}
			
			//	LTargetNode must be one of the following ->
			//		StackReferenceNode
			//		ExtractColumnNode
			//		TableNode
			//		PropertyReferenceNode
			PlanNode LSourceNode = CompileTypedExpression(APlan, LStatement.Expression, LTargetNode.DataType, true);
			return EmitAssignmentNode(APlan, AStatement, LSourceNode, LTargetNode);
		}
		
		//	Source BNF -> insert <source expression> into <qualified identifier>
		//
		//  InsertNode
		//		Nodes[0] = SourceNode
		//		Nodes[1] = TargetNode
		//			
		//	Default Execution Behavior ->
		//		for each row in SourceNode
		//			insert into the TableVariable
		public static PlanNode CompileInsertStatement(Plan APlan, Statement AStatement)
		{
			InsertStatement LStatement = (InsertStatement)AStatement;

			if (LStatement.SourceExpression is TableSelectorExpression)
			{
				PlanNode LSourceNode = CompileTableSelectorExpression(APlan, (TableSelectorExpression)LStatement.SourceExpression);

				PlanNode LBlockNode = (LSourceNode.NodeCount > 1 ? (PlanNode)new DelimitedBlockNode() : (PlanNode)new BlockNode());
				LBlockNode.SetLineInfo(LStatement.LineInfo);
				foreach (PlanNode LRowNode in LSourceNode.Nodes)
				{
					InsertStatement LInsertStatement = new InsertStatement();
					LInsertStatement.SetLineInfo(LStatement.LineInfo);
					LInsertStatement.Modifiers = LStatement.Modifiers;
					LInsertStatement.SourceExpression = (Expression)LRowNode.EmitStatement(EmitMode.ForCopy);
					LInsertStatement.Target = LStatement.Target;
					LBlockNode.Nodes.Add(CompileInsertStatement(APlan, LInsertStatement));
				}
				return LBlockNode;
			}
			else
			{
				InsertNode LNode = new InsertNode();
				LNode.SetLineInfo(LStatement.LineInfo);
				LNode.Modifiers = LStatement.Modifiers;
				PlanNode LSourceNode;
				PlanNode LTargetNode;

				// Prepare the source node
				LSourceNode = CompileExpression(APlan, LStatement.SourceExpression);
				
				// Prepare the target node
				APlan.PushStatementContext(new StatementContext(StatementType.Insert));
				try
				{
					LTargetNode = CompileExpression(APlan, LStatement.Target);
					if (!(LTargetNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.InvalidUpdateTarget, LStatement.Target);
					
					// Make sure the target node is static to avoid the overhead of refreshing after each insert
					if (LTargetNode is TableNode)
						((TableNode)LTargetNode).CursorType = CursorType.Static;
				}
				finally
				{
					APlan.PopStatementContext();
				}

				Schema.IDataType LConversionTargetType;
				
				if (LSourceNode.DataType is Schema.IRowType)
					LConversionTargetType = ((Schema.ITableType)LTargetNode.DataType).RowType;
				else
				{
					if (!(LSourceNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableOrRowExpressionExpected, LStatement.SourceExpression);

					TableNode LTableNode = EnsureTableNode(APlan, LSourceNode);
						
					if (LTableNode.CursorType != CursorType.Static)
						LSourceNode = EmitCopyNode(APlan, LTableNode);
					else
						LSourceNode = LTableNode;
					
					LConversionTargetType = LTargetNode.DataType;
				}
				
				if (Boolean.Parse(LanguageModifiers.GetModifier(LNode.Modifiers, "InsertedUpdate", "false")))
				{
					// if this is an inserted update, then the source expression will have old and new columns,
					// and the target type must be made to look the same
					Schema.TableType LTableType = LConversionTargetType as Schema.TableType;
					if (LTableType != null)
					{
						Schema.TableType LNewTableType = new Schema.TableType();
						foreach (Schema.Column LColumn in LTableType.Columns)
							LNewTableType.Columns.Add(LColumn.Copy(Keywords.Old));
						foreach (Schema.Column LColumn in LTableType.Columns)
							LNewTableType.Columns.Add(LColumn.Copy(Keywords.New));
						LConversionTargetType = LNewTableType;
					}
					else
					{
						Schema.RowType LRowType = (Schema.RowType)LConversionTargetType;
						Schema.RowType LNewRowType = new Schema.RowType();
						foreach (Schema.Column LColumn in LRowType.Columns)
							LNewRowType.Columns.Add(LColumn.Copy(Keywords.Old));
						foreach (Schema.Column LColumn in LRowType.Columns)
							LNewRowType.Columns.Add(LColumn.Copy(Keywords.New));
						LConversionTargetType = LNewRowType;
					}
				}

				// Verify that all the columns in the source row or table type are assignable to the columns of the target table
				// insert a redefine node if necessary
				if (!LSourceNode.DataType.Is(LConversionTargetType))
				{
					ConversionContext LContext = FindConversionPath(APlan, LSourceNode.DataType, LConversionTargetType, true);
					CheckConversionContext(APlan, LContext);
					LSourceNode = ConvertNode(APlan, LSourceNode, LContext);
				}
				
				LNode.Nodes.Add(Upcast(APlan, LSourceNode, LTargetNode.DataType));
				LNode.Nodes.Add(EmitInsertConditionNode(APlan, LTargetNode));
			
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
		}
		
		public static UpdateColumnNode EmitUpdateColumnNode(Plan APlan, UpdateColumnExpression AExpression, PlanNode ASourceNode)
		{
			PlanNode LColumnTargetNode;
			UpdateColumnNode LColumnNode;
			APlan.PushStatementContext(new StatementContext(StatementType.Assignment));
			try
			{
				LColumnTargetNode = CompileExpression(APlan, AExpression.Target);
			}
			finally
			{
				APlan.PopStatementContext();
			}

			if (LColumnTargetNode is StackColumnReferenceNode)
			{
				LColumnNode = new UpdateColumnNode();
				#if USECOLUMNLOCATIONBINDING
				LColumnNode.ColumnLocation = ((StackColumnReferenceNode)LColumnTargetNode).ColumnLocation;
				#else
				LColumnNode.ColumnName = ((StackColumnReferenceNode)LColumnTargetNode).Identifier;
				#endif
				LColumnNode.DataType = LColumnTargetNode.DataType;
				
				LColumnNode.Nodes.Add(EmitTypedNode(APlan, ASourceNode, LColumnNode.DataType));
				LColumnNode.DetermineCharacteristics(APlan);
			}
			else if (LColumnTargetNode is PropertyReferenceNode)
			{
				PropertyReferenceNode LPropertyReference = (PropertyReferenceNode)LColumnTargetNode;
				Schema.Property LProperty = LPropertyReference.ScalarType.Representations[LPropertyReference.RepresentationIndex].Properties[LPropertyReference.PropertyIndex];
				PlanNode LWriteNode = BuildCallNode(APlan, null, LProperty.WriteAccessor, new PlanNode[]{LColumnTargetNode.Nodes[0], EmitTypedNode(APlan, ASourceNode, LProperty.DataType)});
				if (!(LColumnTargetNode.Nodes[0] is StackColumnReferenceNode))
					throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, AExpression);
				LColumnNode = new UpdateColumnNode();
				#if USECOLUMNLOCATIONBINDING
				LColumnNode.ColumnLocation = ((StackColumnReferenceNode)LColumnTargetNode.Nodes[0]).ColumnLocation;
				#else
				LColumnNode.ColumnName = ((StackColumnReferenceNode)LColumnTargetNode.Nodes[0]).Identifier;
				#endif
				LColumnNode.DataType = LColumnTargetNode.Nodes[0].DataType;

				LColumnNode.Nodes.Add(LWriteNode);
				LColumnNode.DetermineCharacteristics(APlan);
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, AExpression);

			return LColumnNode;
		}
		
		public static UpdateColumnNode CompileUpdateColumnExpression(Plan APlan, UpdateColumnExpression AExpression)
		{
			return EmitUpdateColumnNode(APlan, AExpression, CompileExpression(APlan, AExpression.Expression));
		}
		
		// Source BNF -> update <expression> set <update expression commalist> [where <expression>]
		//
		// If the update affects a key and the key affecting update is not context literal, the update must be translated as follows:
		//	update T set { <update list> } where <condition> ::=
		//		var LV := T where <condition> redefine { <update list> };
		//		delete T where <condition>
		//		insert LV into T;
		//
		// UpdateNode
		//		Nodes[0] = TargetNode
		//		Nodes[1..Count - 1] = UpdateColumnPlanNodes
		//
		// Default Execution Behavior ->
		//		for each row in the target expression
		//			push the old row
		//			if the row meets the where condition
		//				evaluate each update column expression
		//			pop the old row
		//			update the target table to the new row values
		//
		public static PlanNode CompileUpdateStatement(Plan APlan, Statement AStatement)
		{
			UpdateStatement LStatement = (UpdateStatement)AStatement;
			PlanNode LTargetNode;
			APlan.PushStatementContext(new StatementContext(StatementType.Update));
			try
			{
				LTargetNode = CompileExpression(APlan, LStatement.Target);
			}
			finally
			{
				APlan.PopStatementContext();
			}

			if (LTargetNode.DataType is Schema.ITableType)
			{
				StackReferenceNode LStackReferenceNode = LTargetNode as StackReferenceNode;
				if (LStackReferenceNode != null)
					LStackReferenceNode.ByReference = true;

				LTargetNode = EnsureTableNode(APlan, LTargetNode);
				UpdateNode LNode = new UpdateNode();
				LNode.SetLineInfo(LStatement.LineInfo);
				LNode.Modifiers = LStatement.Modifiers;
				TableNode LTargetTableNode = (TableNode)LTargetNode;

				Schema.TableVar LTargetTableVar = LTargetTableNode.TableVar;

				LNode.TargetNode = LTargetNode;

				if (LStatement.Condition != null)
				{
					LTargetNode = EmitUpdateConditionNode(APlan, LTargetNode, LStatement.Condition);
					LNode.ConditionNode = LTargetNode.Nodes[1];
					LNode.Nodes.Add(LTargetNode);
				}
				else
					LNode.Nodes.Add(LTargetNode);

				Schema.Key LAffectedKey = null;
				APlan.EnterRowContext();
				try
				{
					APlan.Symbols.Push(new Symbol(LTargetTableVar.DataType.RowType));
					try
					{
						UpdateColumnNode LColumnNode;
						foreach (UpdateColumnExpression LExpression in LStatement.Columns)
						{
							LColumnNode = CompileUpdateColumnExpression(APlan, LExpression);
							#if USECOLUMNLOCATIONBINDING
							LNode.IsKeyAffected = 
								LNode.IsKeyAffected && 
								(LTargetTableNode.Order != null) && 
								LTargetTableNode.Order.Columns.Contains(LTargetTableVar.Columns[LColumnNode.ColumnLocation].Name);
							#else
							LNode.IsKeyAffected = 
								LNode.IsKeyAffected && 
								(LTargetTableNode.Order != null) && 
								LTargetTableNode.Order.Columns.Contains(LColumnNode.ColumnName);
							#endif

							if (LAffectedKey == null)
							{
								foreach (Schema.Key LKey in LTargetTableNode.TableVar.Keys)
									if (LKey.Columns.ContainsName(LColumnNode.ColumnName) && !LColumnNode.IsContextLiteral(0))
									{
										LAffectedKey = LKey;
										break;
									}
							}

							LNode.Nodes.Add(LColumnNode);
						}
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.ExitRowContext();
				}
				
				if (LAffectedKey != null)
				{
					//begin
					//    var LV := T where <condition> { <old column list>, <new column list> };
					//    delete with { Unchecked = "True" } T where <condition>;
					//    insert with { InsertedUpdate = "True", UpdateColumnNames = "<column names semicolon list>" }
					//        LV into T; // rename new add { <old column list> };
					//end;
					
					DelimitedBlock LBlock = new DelimitedBlock();
					VariableStatement LVariableStatement = new VariableStatement();
					LVariableStatement.VariableName = new IdentifierExpression(Schema.Object.NameFromGuid(Guid.NewGuid()));
					List<int> LUpdateColumnIndexes = new List<int>();
					StringBuilder LUpdateColumnNames = new StringBuilder();

					SpecifyExpression LSpecifyExpression = new SpecifyExpression();
					LSpecifyExpression.Expression = LStatement.Condition == null ? LStatement.Target : new RestrictExpression(LStatement.Target, LStatement.Condition);
					for (int LIndex = 0; LIndex < LTargetTableVar.Columns.Count; LIndex++)
						LSpecifyExpression.Expressions.Add(new NamedColumnExpression(new IdentifierExpression(Schema.Object.EnsureRooted(LTargetTableVar.Columns[LIndex].Name)), Schema.Object.Qualify(LTargetTableVar.Columns[LIndex].Name, Keywords.Old)));
						
					for (int LIndex = 1; LIndex < LNode.Nodes.Count; LIndex++)
					{
						UpdateColumnNode LColumnNode = (UpdateColumnNode)LNode.Nodes[LIndex];
						LUpdateColumnIndexes.Add(LTargetTableVar.Columns.IndexOfName(LColumnNode.ColumnName));
						if (LUpdateColumnNames.Length > 0)
							LUpdateColumnNames.Append(";");
						LUpdateColumnNames.Append(LTargetTableVar.Columns[LUpdateColumnIndexes[LIndex - 1]].Name);
					}
						
					for (int LIndex = 0; LIndex < LTargetTableVar.Columns.Count; LIndex++)
					{
						int LUpdateIndex = LUpdateColumnIndexes.IndexOf(LIndex);
						if (LUpdateIndex >= 0)
							LSpecifyExpression.Expressions.Add(new NamedColumnExpression((Expression)LNode.Nodes[LUpdateIndex + 1].Nodes[0].EmitStatement(EmitMode.ForCopy), Schema.Object.Qualify(LTargetTableVar.Columns[LIndex].Name, Keywords.New)));
						else
							LSpecifyExpression.Expressions.Add(new NamedColumnExpression(new IdentifierExpression(Schema.Object.EnsureRooted(LTargetTableVar.Columns[LIndex].Name)), Schema.Object.Qualify(LTargetTableVar.Columns[LIndex].Name, Keywords.New)));
					}
					
					LVariableStatement.Expression = LSpecifyExpression;
					LBlock.Statements.Add(LVariableStatement);
					DeleteStatement LDeleteStatement = new DeleteStatement(LStatement.Condition == null ? LStatement.Target : new RestrictExpression(LStatement.Target, LStatement.Condition));
					LDeleteStatement.Modifiers = new LanguageModifiers();
					LDeleteStatement.Modifiers.Add(new LanguageModifier("Unchecked", "True"));
					LBlock.Statements.Add(LDeleteStatement);
					InsertStatement LInsertStatement = new InsertStatement(LVariableStatement.VariableName, LStatement.Target);
					LInsertStatement.Modifiers = new LanguageModifiers();
					LInsertStatement.Modifiers.Add(new LanguageModifier("InsertedUpdate", "True"));
					LInsertStatement.Modifiers.Add(new LanguageModifier("UpdateColumnNames", LUpdateColumnNames.ToString()));
					LBlock.Statements.Add(LInsertStatement);

					return CompileDelimitedBlock(APlan, LBlock);
				}
				else
				{
					LNode.DetermineDataType(APlan);
					LNode.DetermineCharacteristics(APlan);
					return LNode;
				}
			}
			else if (LTargetNode.DataType is Schema.IRowType)
			{
				UpdateRowNode LNode = new UpdateRowNode();
				LNode.SetLineInfo(LStatement.LineInfo);
				LNode.Modifiers = LStatement.Modifiers;
				LNode.Nodes.Add(LTargetNode);
				LNode.ColumnExpressions.AddRange(LStatement.Columns);
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidUpdateTarget, LStatement.Target);
		}

		// Source BNF -> delete <expression>
		//
		// DeleteNode
		//		Nodes[0] = TargetNode
		//
		// Default Execution Behavior ->
		//		while a target node is not empty
		//			delete the row		
		public static PlanNode CompileDeleteStatement(Plan APlan, Statement AStatement)
		{
			DeleteStatement LStatement = (DeleteStatement)AStatement;
			DeleteNode LNode = new DeleteNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.Modifiers = LStatement.Modifiers;
			PlanNode LTargetNode;

			APlan.PushStatementContext(new StatementContext(StatementType.Delete));
			try
			{
				LTargetNode = CompileExpression(APlan, LStatement.Target);
			}
			finally
			{
				APlan.PopStatementContext();
			}

			if (!(LTargetNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.InvalidUpdateTarget, LStatement.Target);
			StackReferenceNode LStackReferenceNode = LTargetNode as StackReferenceNode;
			if (LStackReferenceNode != null)
				LStackReferenceNode.ByReference = true;
			LNode.Nodes.Add(EnsureTableNode(APlan, LTargetNode));
			
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static bool GetEnforced(Plan APlan, MetaData AMetaData)
		{
			return GetEnforced(APlan, AMetaData, true);
		}
		
		public static bool GetEnforced(Plan APlan, MetaData AMetaData, bool ADefaultEnforced)
		{
			Tag LTag = MetaData.GetTag(AMetaData, "Storage.Enforced");
			if (LTag != Tag.None)
			{
				AMetaData.Tags.Remove(LTag);
				Tag LNewTag = new Tag("DAE.Enforced", (!Convert.ToBoolean(LTag.Value)).ToString(), false, LTag.IsStatic);
				AMetaData.Tags.Add(LNewTag);
				if (!APlan.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedTag, CompilerErrorLevel.Warning, LTag.Name, LNewTag.Name));
			}
				
			return Convert.ToBoolean(MetaData.GetTag(AMetaData, "DAE.Enforced", ADefaultEnforced.ToString()));
		}
		
		public static bool GetIsSparse(Plan APlan, MetaData AMetaData)
		{
			Tag LTag = MetaData.GetTag(AMetaData, "Storage.IsSparse");
			if (LTag != Tag.None)
			{
				AMetaData.Tags.Remove(LTag);
				Tag LNewTag = new Tag("DAE.IsSparse", LTag.Value, false, LTag.IsStatic);
				AMetaData.Tags.Add(LNewTag);
				if (!APlan.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedTag, CompilerErrorLevel.Warning, LTag.Name, LNewTag.Name));
			}
			
			return Convert.ToBoolean(MetaData.GetTag(AMetaData, "DAE.IsSparse", "false"));
		}
		
		public static void ProcessIsClusteredTag(Plan APlan, MetaData AMetaData)
		{
			Tag LTag = MetaData.GetTag(AMetaData, "Storage.IsClustered");
			if (LTag != Tag.None)
			{
				AMetaData.Tags.Remove(LTag);
				Tag LNewTag = new Tag("DAE.IsClustered", LTag.Value, false, LTag.IsStatic);
				AMetaData.Tags.Add(LNewTag);
				if (!APlan.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedTag, CompilerErrorLevel.Warning, LTag.Name, LNewTag.Name));
			}
		}
		
		public static Schema.Key CompileKeyDefinition(Plan APlan, Schema.TableVar ATableVar, KeyDefinition AKey)
		{
			Schema.Key LKey = new Schema.Key(Schema.Object.GetObjectID(AKey.MetaData), AKey.MetaData);
			foreach (KeyColumnDefinition LColumn in AKey.Columns)
				LKey.Columns.Add(ATableVar.Columns[LColumn.ColumnName]);

			ProcessIsClusteredTag(APlan, LKey.MetaData);
			LKey.Enforced = GetEnforced(APlan, LKey.MetaData);
			LKey.IsSparse = GetIsSparse(APlan, LKey.MetaData);
			return LKey;
		}
		
		public static void CompileTableVarKeys(Plan APlan, Schema.TableVar ATableVar, KeyDefinitions AKeys)
		{
			CompileTableVarKeys(APlan, ATableVar, AKeys, true);
		}
		
		public static bool SupportsEqual(Plan APlan, Schema.IDataType ADataType)
		{
			if (SupportsComparison(APlan, ADataType))
				return true;
				
			Schema.Signature LSignature = new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(ADataType), new Schema.SignatureElement(ADataType)});
			OperatorBindingContext LContext = new OperatorBindingContext(null, "iEqual", APlan.NameResolutionPath, LSignature, true);
			Compiler.ResolveOperator(APlan, LContext);
			return LContext.Operator != null;
		}
		
		public static bool SupportsComparison(Plan APlan, Schema.IDataType ADataType)
		{
			Schema.Signature LSignature = new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(ADataType), new Schema.SignatureElement(ADataType)});
			OperatorBindingContext LContext = new OperatorBindingContext(null, "iCompare", APlan.NameResolutionPath, LSignature, true);
			Compiler.ResolveOperator(APlan, LContext);
			return LContext.Operator != null;
		}
		
		public static void CompileTableVarKeys(Plan APlan, Schema.TableVar ATableVar, KeyDefinitions AKeys, bool AEnsureKey)
		{
			foreach (KeyDefinition LKeyDefinition in AKeys)
			{
				Schema.Key LKey = CompileKeyDefinition(APlan, ATableVar, LKeyDefinition);
				if (!ATableVar.Keys.Contains(LKey))
					ATableVar.Keys.Add(LKey);
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, LKey.Name);
			}

			if (AEnsureKey && (ATableVar.Keys.Count == 0))
			{
				Schema.Key LKey = new Schema.Key();
				foreach (Schema.TableVarColumn LTableColumn in ATableVar.Columns)
					if (SupportsComparison(APlan, LTableColumn.DataType))
						LKey.Columns.Add(LTableColumn);
				ATableVar.Keys.Add(LKey);
			}
		}
		
		public static Schema.Key KeyFromKeyColumnDefinitions(Plan APlan, Schema.TableVar ATableVar, KeyColumnDefinitions AKeyColumns)
		{
			Schema.Key LKey = new Schema.Key();
			foreach (KeyColumnDefinition LColumn in AKeyColumns)
				LKey.Columns.Add(ATableVar.Columns[LColumn.ColumnName]);
			return LKey;
		}
		
		public static Schema.Key FindKey(Plan APlan, Schema.TableVar ATableVar, KeyColumnDefinitions AKeyColumns)
		{
			Schema.Key LKey = KeyFromKeyColumnDefinitions(APlan, ATableVar, AKeyColumns);
			
			int LIndex = ATableVar.IndexOfKey(LKey);
			if (LIndex >= 0)
				return ATableVar.Keys[LIndex];
			
			throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, LKey.Name);
		}
		
		public static Schema.Key FindKey(Plan APlan, Schema.TableVar ATableVar, KeyDefinitionBase AKeyDefinition)
		{
			return FindKey(APlan, ATableVar, AKeyDefinition.Columns);
		}
		
		public static Schema.Key FindClusteringKey(Plan APlan, Schema.TableVar ATableVar)
		{
			Schema.Key LMinimumKey = null;
			foreach (Schema.Key LKey in ATableVar.Keys)
			{
				if (Convert.ToBoolean(MetaData.GetTag(LKey.MetaData, "DAE.IsClustered", "false")))
					return LKey;
				
				if (!LKey.IsSparse)
					if (LMinimumKey == null)
						LMinimumKey = LKey;
					else
						if (LMinimumKey.Columns.Count > LKey.Columns.Count)
							LMinimumKey = LKey;
			}
					
			if (LMinimumKey != null)
				return LMinimumKey;

			throw new Schema.SchemaException(Schema.SchemaException.Codes.KeyRequired, ATableVar.DisplayName);
		}
		
		public static void EnsureKey(Plan APlan, Schema.TableVar ATableVar)
		{
			if (ATableVar.Keys.Count == 0)
			{
				Schema.Key LKey = new Schema.Key();
				foreach (Schema.TableVarColumn LColumn in ATableVar.Columns)
					if (SupportsComparison(APlan, LColumn.DataType))
						LKey.Columns.Add(LColumn);
				ATableVar.Keys.Add(LKey);
			}
		}

		public static Schema.Sort GetSort(Plan APlan, Schema.IDataType ADataType)
		{
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (LScalarType.Sort == null)
				{
					if (LScalarType.SortID >= 0)
						APlan.CatalogDeviceSession.ResolveCatalogObject(LScalarType.SortID);
					else
						return GetUniqueSort(APlan, ADataType);
				}

				return LScalarType.Sort;
			}
			else
			{
				Schema.Sort LSort = CompileSortDefinition(APlan, ADataType);
				return LSort;
			}
		}
		
		public static Schema.Sort GetUniqueSort(Plan APlan, Schema.IDataType ADataType)
		{
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (LScalarType.UniqueSort == null)
				{
					if (LScalarType.UniqueSortID >= 0)
						APlan.CatalogDeviceSession.ResolveCatalogObject(LScalarType.UniqueSortID);
					else
					{
						APlan.PushLoadingContext(new LoadingContext(LScalarType.Owner, LScalarType.Library.Name, false));
						try
						{
							CreateSortNode LCreateSortNode = new CreateSortNode();
							LCreateSortNode.ScalarType = LScalarType;
							LCreateSortNode.Sort = CompileSortDefinition(APlan, LScalarType);
							LCreateSortNode.IsUnique = true;
							APlan.ExecuteNode(LCreateSortNode);
						}
						finally
						{
							APlan.PopLoadingContext();
						}
					}
				}

				return LScalarType.UniqueSort;
			}
			else
			{
				Schema.Sort LSort = CompileSortDefinition(APlan, ADataType);
				return LSort;
			}
		}
		
		public static bool IsOrderUnique(Plan APlan, Schema.TableVar ATableVar, Schema.Order AOrder)
		{
			foreach (Schema.Key LKey in ATableVar.Keys)
				if (!LKey.IsSparse && OrderIncludesKey(APlan, AOrder, LKey))
					return true;
			return false;
		}
		
		public static void EnsureOrderUnique(Plan APlan, Schema.TableVar ATableVar, Schema.Order AOrder)
		{
			if (!IsOrderUnique(APlan, ATableVar, AOrder))
			{
				bool LIsDescending = AOrder.IsDescending;
				foreach (Schema.TableVarColumn LColumn in FindClusteringKey(APlan, ATableVar).Columns)
				{
					Schema.Sort LUniqueSort = GetUniqueSort(APlan, LColumn.DataType);
					if (!AOrder.Columns.Contains(LColumn.Name, LUniqueSort))
					{
						Schema.OrderColumn LNewColumn = new Schema.OrderColumn(LColumn, !LIsDescending);
						LNewColumn.Sort = LUniqueSort;
						LNewColumn.IsDefaultSort = true;
						if (LNewColumn.Sort.HasDependencies())
							APlan.AttachDependencies(LNewColumn.Sort.Dependencies);
						AOrder.Columns.Add(LNewColumn);
					}
				}
			}
		}
		
		public static Schema.Order CompileOrderColumnDefinitions(Plan APlan, Schema.TableVar ATableVar, OrderColumnDefinitions AColumns, MetaData AMetaData, bool AEnsureUnique)
		{
			Schema.Order LOrder = new Schema.Order(Schema.Object.GetObjectID(AMetaData), AMetaData);
			ProcessIsClusteredTag(APlan, LOrder.MetaData);
			Schema.OrderColumn LColumn;
			foreach (OrderColumnDefinition LOrderColumn in AColumns)
			{
				LColumn = new Schema.OrderColumn(ATableVar.Columns[LOrderColumn.ColumnName], LOrderColumn.Ascending, LOrderColumn.IncludeNils);
				if (LOrderColumn.Sort != null)
				{
					LColumn.Sort = CompileSortDefinition(APlan, LColumn.Column.DataType, LOrderColumn.Sort, false);
					LColumn.IsDefaultSort = false;
				}
				else
				{
					LColumn.Sort = GetSort(APlan, LColumn.Column.DataType);
					LColumn.IsDefaultSort = true;
				}
				if (LColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LColumn.Sort.Dependencies);
				LOrder.Columns.Add(LColumn);
			}
			if (AEnsureUnique)
				EnsureOrderUnique(APlan, ATableVar, LOrder);
			return LOrder;
		}
		
		public static Schema.Order CompileOrderColumnDefinitions(Plan APlan, Schema.TableVar ATableVar, OrderColumnDefinitions AColumns)
		{
			return CompileOrderColumnDefinitions(APlan, ATableVar, AColumns, null, true);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan APlan, Schema.TableVar ATableVar, OrderDefinitionBase AOrder)
		{
			return CompileOrderColumnDefinitions(APlan, ATableVar, AOrder.Columns, null, true);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan APlan, Schema.TableVar ATableVar, OrderDefinitionBase AOrder, bool AEnsureUnique)
		{
			return CompileOrderColumnDefinitions(APlan, ATableVar, AOrder.Columns, null, AEnsureUnique);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan APlan, Schema.TableVar ATableVar, OrderDefinition AOrder)
		{
			return CompileOrderColumnDefinitions(APlan, ATableVar, AOrder.Columns, AOrder.MetaData, true);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan APlan, Schema.TableVar ATableVar, OrderDefinition AOrder, bool AEnsureUnique)
		{
			return CompileOrderColumnDefinitions(APlan, ATableVar, AOrder.Columns, AOrder.MetaData, AEnsureUnique);
		}
		
		public static void CompileTableVarOrders(Plan APlan, Schema.TableVar ATableVar, OrderDefinitions AOrders)
		{
			foreach (OrderDefinition LOrderDefinition in AOrders)
			{
				Schema.Order LOrder = CompileOrderDefinition(APlan, ATableVar, LOrderDefinition, false);
				if (!ATableVar.Orders.Contains(LOrder))
					ATableVar.Orders.Add(LOrder);
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, LOrder.Name);
			}
		}
		
		public static Schema.Order OrderFromKey(Plan APlan, Schema.Key AKey)
		{
			Schema.Order LOrder = new Schema.Order();
			Schema.OrderColumn LOrderColumn;
			Schema.TableVarColumn LColumn;
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
			{
				LColumn = AKey.Columns[LIndex];
				LOrderColumn = new Schema.OrderColumn(LColumn, true, true);
				if (LColumn.DataType is Schema.ScalarType)
					LOrderColumn.Sort = GetUniqueSort(APlan, LColumn.DataType);
				else
					LOrderColumn.Sort = CompileSortDefinition(APlan, LColumn.DataType);
				LOrderColumn.IsDefaultSort = true;
				if (LOrderColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LOrderColumn.Sort.Dependencies);
				LOrder.Columns.Add(LOrderColumn);
			}
			return LOrder;
		}

		public static Schema.Order FindOrder(Plan APlan, Schema.TableVar ATableVar, OrderDefinitionBase AOrderDefinition)
		{
			Schema.Order LOrder = CompileOrderDefinition(APlan, ATableVar, AOrderDefinition, false);
			
			int LIndex = ATableVar.IndexOfOrder(LOrder);
			if (LIndex >= 0)
				return ATableVar.Orders[LIndex];

			throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, LOrder.Name);
		}
		
		public static Schema.Order FindClusteringOrder(Plan APlan, Schema.TableVar ATableVar)
		{
			Schema.Key LMinimumKey = null;
			foreach (Schema.Key LKey in ATableVar.Keys)
			{
				if (Convert.ToBoolean(MetaData.GetTag(LKey.MetaData, "DAE.IsClustered", "false")))
					return OrderFromKey(APlan, LKey);
					
				if (!LKey.IsSparse)
					if (LMinimumKey == null)
						LMinimumKey = LKey;
					else
						if (LMinimumKey.Columns.Count > LKey.Columns.Count)
							LMinimumKey = LKey;
			}

			foreach (Schema.Order LOrder in ATableVar.Orders)
				if (Convert.ToBoolean(MetaData.GetTag(LOrder.MetaData, "DAE.IsClustered", "false")))
					return LOrder;
					
			if (LMinimumKey != null)
				return OrderFromKey(APlan, LMinimumKey);
					
			if (ATableVar.Orders.Count > 0)
				return ATableVar.Orders[0];
				
			throw new Schema.SchemaException(Schema.SchemaException.Codes.KeyRequired, ATableVar.DisplayName);
		}
		
		// returns true if the order includes the key as a subset, including the use of the unique sort algorithm for the type of each column
		public static bool OrderIncludesKey(Plan APlan, Schema.Order AIncludingOrder, Schema.Key AIncludedKey)
		{
			Schema.TableVarColumn LColumn;
			for (int LIndex = 0; LIndex < AIncludedKey.Columns.Count; LIndex++)
			{
				LColumn = AIncludedKey.Columns[LIndex];
				if (!AIncludingOrder.Columns.Contains(LColumn.Name, GetUniqueSort(APlan, LColumn.DataType)))
					return false;
			}

			return true;
		}
		
		public static bool OrderIncludesOrder(Plan APlan, Schema.Order AIncludingOrder, Schema.Order AIncludedOrder)
		{
			Schema.OrderColumn LColumn;
			for (int LIndex = 0; LIndex < AIncludedOrder.Columns.Count; LIndex++)
			{
				LColumn = AIncludedOrder.Columns[LIndex];
				if (!AIncludingOrder.Columns.Contains(LColumn.Column.Name, LColumn.Sort))
					return false;
			}
			
			return true;
		}
		
		public static Schema.RowConstraint CompileRowConstraint(Plan APlan, Schema.TableVar ATableVar, ConstraintDefinition AConstraint)
		{
			Schema.RowConstraint LNewConstraint = new Schema.RowConstraint(Schema.Object.GetObjectID(AConstraint.MetaData), AConstraint.ConstraintName);
			LNewConstraint.Library = ATableVar.Library == null ? null : APlan.CurrentLibrary;
			LNewConstraint.IsGenerated = AConstraint.IsGenerated || ATableVar.IsGenerated;
			APlan.PushCreationObject(LNewConstraint);
			try
			{
				LNewConstraint.MergeMetaData(AConstraint.MetaData);
				LNewConstraint.Enforced = GetEnforced(APlan, AConstraint.MetaData);
				LNewConstraint.ConstraintType = Schema.ConstraintType.Row;
					
				APlan.EnterRowContext();
				try
				{
					APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.RowType));
					try
					{
						PlanNode LConstraintNode = CompileBooleanExpression(APlan, AConstraint.Expression);
						if (!(LConstraintNode.IsFunctional && LConstraintNode.IsDeterministic))
							throw new CompilerException(CompilerException.Codes.InvalidConstraintExpression, AConstraint.Expression);

						LConstraintNode = OptimizeNode(APlan, LConstraintNode);
						LConstraintNode = BindNode(APlan, LConstraintNode);							
						LNewConstraint.Node = LConstraintNode;
						
						string LCustomMessage = LNewConstraint.GetCustomMessage(Schema.Transition.Insert);
						if (!String.IsNullOrEmpty(LCustomMessage))
						{
							try
							{
								PlanNode LViolationMessageNode = CompileTypedExpression(APlan, new D4.Parser().ParseExpression(LCustomMessage), APlan.DataTypes.SystemString);
								LViolationMessageNode = OptimizeNode(APlan, LViolationMessageNode);
								LViolationMessageNode = BindNode(APlan, LViolationMessageNode);
								LNewConstraint.ViolationMessageNode = LViolationMessageNode;
							}
							catch (Exception LException)
							{
								throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, AConstraint, LException, LNewConstraint.Name);
							}
						}
						
						LNewConstraint.DetermineRemotable(APlan.CatalogDeviceSession);
							
						if (!LNewConstraint.IsRemotable)
							LNewConstraint.ConstraintType = Schema.ConstraintType.Database;
							
						return LNewConstraint;
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.ExitRowContext();
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.TransitionConstraint CompileTransitionConstraint(Plan APlan, Schema.TableVar ATableVar, TransitionConstraintDefinition AConstraint)
		{
			PlanNode LConstraintNode;
			Schema.TransitionConstraint LNewConstraint = new Schema.TransitionConstraint(Schema.Object.GetObjectID(AConstraint.MetaData), AConstraint.ConstraintName);
			LNewConstraint.Library = ATableVar.Library == null ? null : APlan.CurrentLibrary;
			LNewConstraint.IsGenerated = AConstraint.IsGenerated || ATableVar.IsGenerated;
			APlan.PushCreationObject(LNewConstraint);
			try
			{
				LNewConstraint.MergeMetaData(AConstraint.MetaData);
				LNewConstraint.Enforced = GetEnforced(APlan, LNewConstraint.MetaData);
				LNewConstraint.ConstraintType = Schema.ConstraintType.Database;
					
				APlan.EnterRowContext();
				try
				{
					if (AConstraint.OnInsertExpression != null)
					{
						APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.NewRowType));
						try
						{
							LConstraintNode = CompileBooleanExpression(APlan, AConstraint.OnInsertExpression);
							if (!(LConstraintNode.IsFunctional && LConstraintNode.IsRepeatable))
								throw new CompilerException(CompilerException.Codes.InvalidTransitionConstraintExpression, AConstraint.OnInsertExpression);

							LConstraintNode = OptimizeNode(APlan, LConstraintNode);
							LConstraintNode = BindNode(APlan, LConstraintNode);							
							LNewConstraint.OnInsertNode = LConstraintNode;

							string LCustomMessage = LNewConstraint.GetCustomMessage(Schema.Transition.Insert);
							if (!String.IsNullOrEmpty(LCustomMessage))
							{
								try
								{
									PlanNode LViolationMessageNode = CompileTypedExpression(APlan, new D4.Parser().ParseExpression(LCustomMessage), APlan.DataTypes.SystemString);
									LViolationMessageNode = OptimizeNode(APlan, LViolationMessageNode);
									LViolationMessageNode = BindNode(APlan, LViolationMessageNode);
									LNewConstraint.OnInsertViolationMessageNode = LViolationMessageNode;
								}
								catch (Exception LException)
								{
									throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, AConstraint, LException, LNewConstraint.Name);
								}
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					
					if (AConstraint.OnUpdateExpression != null)
					{
						APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.OldRowType));
						try
						{
							APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.NewRowType));
							try
							{
								LConstraintNode = CompileBooleanExpression(APlan, AConstraint.OnUpdateExpression);
								if (!(LConstraintNode.IsFunctional && LConstraintNode.IsRepeatable))
									throw new CompilerException(CompilerException.Codes.InvalidTransitionConstraintExpression, AConstraint.OnUpdateExpression);

								LConstraintNode = OptimizeNode(APlan, LConstraintNode);
								LConstraintNode = BindNode(APlan, LConstraintNode);							
								LNewConstraint.OnUpdateNode = LConstraintNode;

								string LCustomMessage = LNewConstraint.GetCustomMessage(Schema.Transition.Update);
								if (!String.IsNullOrEmpty(LCustomMessage))
								{
									try
									{
										PlanNode LViolationMessageNode = CompileTypedExpression(APlan, new D4.Parser().ParseExpression(LCustomMessage), APlan.DataTypes.SystemString);
										LViolationMessageNode = OptimizeNode(APlan, LViolationMessageNode);
										LViolationMessageNode = BindNode(APlan, LViolationMessageNode);
										LNewConstraint.OnUpdateViolationMessageNode = LViolationMessageNode;
									}
									catch (Exception LException)
									{
										throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, AConstraint, LException, LNewConstraint.Name);
									}
								}
							}
							finally
							{
								APlan.Symbols.Pop();
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					
					if (AConstraint.OnDeleteExpression != null)
					{
						APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.OldRowType));
						try
						{
							LConstraintNode = CompileBooleanExpression(APlan, AConstraint.OnDeleteExpression);
							if (!(LConstraintNode.IsFunctional && LConstraintNode.IsRepeatable))
								throw new CompilerException(CompilerException.Codes.InvalidTransitionConstraintExpression, AConstraint.OnDeleteExpression);

							LConstraintNode = OptimizeNode(APlan, LConstraintNode);
							LConstraintNode = BindNode(APlan, LConstraintNode);							
							LNewConstraint.OnDeleteNode = LConstraintNode;

							string LCustomMessage = LNewConstraint.GetCustomMessage(Schema.Transition.Delete);
							if (!String.IsNullOrEmpty(LCustomMessage))
							{
								try
								{
									PlanNode LViolationMessageNode = CompileTypedExpression(APlan, new D4.Parser().ParseExpression(LCustomMessage), APlan.DataTypes.SystemString);
									LViolationMessageNode = OptimizeNode(APlan, LViolationMessageNode);
									LViolationMessageNode = BindNode(APlan, LViolationMessageNode);
									LNewConstraint.OnDeleteViolationMessageNode = LViolationMessageNode;
								}
								catch (Exception LException)
								{
									throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, AConstraint, LException, LNewConstraint.Name);
								}
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					
					LNewConstraint.DetermineRemotable(APlan.CatalogDeviceSession);
						
					if (LNewConstraint.IsRemotable)
						LNewConstraint.ConstraintType = Schema.ConstraintType.Row;

					return LNewConstraint;
				}
				finally
				{
					APlan.ExitRowContext();
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.TableVarConstraint CompileTableVarConstraint(Plan APlan, Schema.TableVar ATableVar, CreateConstraintDefinition AConstraint)
		{
			if (AConstraint is ConstraintDefinition)
				return Compiler.CompileRowConstraint(APlan, ATableVar, (ConstraintDefinition)AConstraint);
			else
				return Compiler.CompileTransitionConstraint(APlan, ATableVar, (TransitionConstraintDefinition)AConstraint);
		}
		
		public static void CompileTableVarConstraints(Plan APlan, Schema.TableVar ATableVar, CreateConstraintDefinitions AConstraints)
		{
			foreach (CreateConstraintDefinition LConstraint in AConstraints)
			{
				Schema.Constraint LNewConstraint = Compiler.CompileTableVarConstraint(APlan, ATableVar, LConstraint);

				ATableVar.Constraints.Add(LNewConstraint);
				if (LNewConstraint is Schema.RowConstraint)
					ATableVar.RowConstraints.Add(LNewConstraint);
				else
				{
					Schema.TransitionConstraint LTransitionConstraint = (Schema.TransitionConstraint)LNewConstraint;
					if (LTransitionConstraint.OnInsertNode != null)
						ATableVar.InsertConstraints.Add(LTransitionConstraint);
					if (LTransitionConstraint.OnUpdateNode != null)
						ATableVar.UpdateConstraints.Add(LTransitionConstraint);
					if (LTransitionConstraint.OnDeleteNode != null)
						ATableVar.DeleteConstraints.Add(LTransitionConstraint);
				}
			}
		}
		
		public static void CompileTableVarKeyConstraints(Plan APlan, Schema.TableVar ATableVar)
		{	
			APlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
			try
			{
				foreach (Schema.Key LKey in ATableVar.Keys)
					if (!LKey.IsInherited && LKey.Enforced && (LKey.Constraint == null))
						LKey.Constraint = CompileKeyConstraint(APlan, ATableVar, LKey);
			}
			finally
			{
				APlan.PopCursorContext();
			}
		}
		
		public static void CompileCreateTableVarStatement(Plan APlan, CreateTableVarStatement AStatement, CreateTableVarNode ANode, BlockNode ABlockNode)
		{
			ApplicationTransaction LTransaction = null;
			if (APlan.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					CompileTableVarKeys(APlan, ANode.TableVar, AStatement.Keys);
					CompileTableVarOrders(APlan, ANode.TableVar, AStatement.Orders);
					if ((ANode.TableVar is Schema.BaseTableVar) && (!APlan.InLoadingContext()))
						((Schema.BaseTableVar)ANode.TableVar).Device.CheckSupported(APlan, ANode.TableVar);
					CompileTableVarConstraints(APlan, ANode.TableVar, AStatement.Constraints);
					if (!APlan.IsRepository)
						CompileTableVarKeyConstraints(APlan, ANode.TableVar);
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
			
			CreateReferenceStatement LStatement;
			foreach (ReferenceDefinition LReference in AStatement.References)
			{
				LStatement = new CreateReferenceStatement();
				LStatement.IsSession = AStatement.IsSession;
				LStatement.TableVarName = ANode.TableVar.Name;
				LStatement.ReferenceName = LReference.ReferenceName;
				foreach (ReferenceColumnDefinition LColumn in LReference.Columns)
					LStatement.Columns.Add(LColumn);
				LStatement.ReferencesDefinition = LReference.ReferencesDefinition;
				LStatement.MetaData = LReference.MetaData;
				ABlockNode.Nodes.Add(CompileCreateReferenceStatement(APlan, LStatement));
			}
		}
		
		public static bool CanBuildCustomMessageForKey(Plan APlan, Schema.Key AKey)
		{
			if (AKey.Columns.Count > 0)
			{
				foreach (Schema.TableVarColumn LColumn in AKey.Columns)
					if (!(LColumn.DataType is Schema.ScalarType) || !((Schema.ScalarType)LColumn.DataType).HasRepresentation(NativeAccessors.AsDisplayString))
						return false;
				return true;
			}
			return false;
		}
		
		public static string GetCustomMessageForKey(Plan APlan, Schema.TableVar ATableVar, Schema.Key AKey)
		{
			StringBuilder LMessage = new StringBuilder();
			LMessage.AppendFormat("'The table {0} already has a row with ", Schema.Object.Unqualify(ATableVar.DisplayName));
			Schema.ScalarType LScalarType;
			Schema.Representation LRepresentation;
			
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LMessage.Append(" and ");
				LMessage.AppendFormat("{0} ", AKey.Columns[LIndex].Name);
				LScalarType = (Schema.ScalarType)AKey.Columns[LIndex].DataType;
				LRepresentation = LScalarType.FindRepresentation(NativeAccessors.AsDisplayString);
				bool LIsString = LScalarType.NativeType == NativeAccessors.AsDisplayString.NativeType;
				if (LIsString)
					LMessage.AppendFormat(@"""' + (if IsNil({0}{1}{2}) then ""<no value>"" else {0}{1}{2}{3}{4}) + '""", new object[]{Keywords.New, Keywords.Qualifier, AKey.Columns[LIndex].Name, Keywords.Qualifier, LRepresentation.Properties[0].Name});
				else
					LMessage.AppendFormat(@"(' + (if IsNil({0}{1}{2}) then ""<no value>"" else {0}{1}{2}{3}{4}) + ')", new object[]{Keywords.New, Keywords.Qualifier, AKey.Columns[LIndex].Name, Keywords.Qualifier, LRepresentation.Properties[0].Name});
			}
			
			LMessage.Append(".'");
			return LMessage.ToString();
		}
		
		// constructs a transition constraint as follows:
		// transition constraint Key<column names>
			// on insert not exists (<table name> where <column names> = <new.column names>) 
			// on update (<old.column names> = <new.column names>) or not exists (<table name> where <column names> = <new.column names>)
			// tags { DAE.Message = "'The table <table name> already has a row with <column names> ' + <new.column names>.AsString + ' [and...] .'" }
		public static Schema.TransitionConstraint CompileKeyConstraint(Plan APlan, Schema.TableVar ATableVar, Schema.Key AKey)
		{
			TransitionConstraintDefinition LDefinition = new TransitionConstraintDefinition();
			LDefinition.ConstraintName = String.Format("Key{0}", ExceptionUtility.StringsToList(AKey.Columns.ColumnNames));
			LDefinition.IsGenerated = true;
			LDefinition.MetaData = AKey.MetaData == null ? AKey.MetaData : AKey.MetaData.Copy();
			if (LDefinition.MetaData == null)
				LDefinition.MetaData = new MetaData();
			LDefinition.MetaData.Tags.SafeRemove("DAE.ObjectID");
			if (!(LDefinition.MetaData.Tags.Contains("DAE.Message") || LDefinition.MetaData.Tags.Contains("DAE.SimpleMessage")) && CanBuildCustomMessageForKey(APlan, AKey))
				LDefinition.MetaData.Tags.Add(new Tag("DAE.Message", GetCustomMessageForKey(APlan, ATableVar, AKey)));
				
			BitArray LIsNilable = new BitArray(AKey.Columns.Count);
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
				LIsNilable[LIndex] = AKey.Columns[LIndex].IsNilable;
			
			LDefinition.OnInsertExpression =
				new UnaryExpression
				(
					Instructions.Not, 
					new UnaryExpression
					(
						Instructions.Exists, 
						new RestrictExpression
						(
							new IdentifierExpression(ATableVar.Name), 
							AKey.IsSparse ?
								BuildKeyEqualExpression(APlan, new Schema.RowType(AKey.Columns).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns) :
								BuildRowEqualExpression(APlan, new Schema.RowType(AKey.Columns).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns, LIsNilable, LIsNilable)
						)
					)
				);
				
			LDefinition.OnUpdateExpression =
				new BinaryExpression
				(
					BuildRowEqualExpression(APlan, new Schema.RowType(AKey.Columns, Keywords.Old).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns, LIsNilable, LIsNilable),
					Instructions.Or,
					new UnaryExpression
					(
						Instructions.Not, 
						new UnaryExpression
						(
							Instructions.Exists, 
							new RestrictExpression
							(
								new IdentifierExpression(ATableVar.Name), 
								AKey.IsSparse ?
									BuildKeyEqualExpression(APlan, new Schema.RowType(AKey.Columns).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns) :
									BuildRowEqualExpression(APlan, new Schema.RowType(AKey.Columns).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns, LIsNilable, LIsNilable)
							)
						)
					)
				);
				
			Schema.TransitionConstraint LConstraint = CompileTransitionConstraint(APlan, ATableVar, LDefinition);
			LConstraint.ConstraintType = Schema.ConstraintType.Table;
			LConstraint.InsertColumnFlags = new BitArray(ATableVar.DataType.Columns.Count);
			for (int LIndex = 0; LIndex < LConstraint.InsertColumnFlags.Length; LIndex++)
				LConstraint.InsertColumnFlags[LIndex] = AKey.Columns.ContainsName(ATableVar.DataType.Columns[LIndex].Name);
			LConstraint.UpdateColumnFlags = (BitArray)LConstraint.InsertColumnFlags.Clone();
			ATableVar.Constraints.Add(LConstraint);
			ATableVar.InsertConstraints.Add(LConstraint);
			ATableVar.UpdateConstraints.Add(LConstraint);
			APlan.AttachDependencies(LConstraint.Dependencies); // Attach dependencies of the constraint to the table
			return LConstraint;
		}
		
		private static void CompileDefault(Plan APlan, Schema.Default ADefault, Schema.IDataType ADataType, DefaultDefinition ADefaultDefinition)
		{
			APlan.Symbols.PushWindow(0); // make sure the default is evaluated in a private context
			try
			{
				ADefault.IsGenerated = ADefaultDefinition.IsGenerated;
				ADefault.MetaData = ADefaultDefinition.MetaData;
				APlan.PushCreationObject(ADefault);
				try
				{
					ADefault.Node = CompileTypedExpression(APlan, ADefaultDefinition.Expression, ADataType);
					ADefault.Node = OptimizeNode(APlan, ADefault.Node);
					ADefault.Node = BindNode(APlan, ADefault.Node);
					ADefault.DetermineRemotable(APlan.CatalogDeviceSession);
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static Schema.ScalarTypeDefault CompileScalarTypeDefault(Plan APlan, Schema.ScalarType AScalarType, DefaultDefinition ADefault)
		{
			int LDefaultID = Schema.Object.GetObjectID(ADefault.MetaData);
			string LDefaultName = Schema.Object.EnsureNameLength(String.Format("{0}_Default", AScalarType.Name));
			Schema.ScalarTypeDefault LDefault = new Schema.ScalarTypeDefault(LDefaultID, LDefaultName);
			LDefault.Library = AScalarType.Library == null ? null : APlan.CurrentLibrary;
			CompileDefault(APlan, LDefault, AScalarType, ADefault);
			return LDefault;
		}
		
		public static Schema.TableVarColumnDefault CompileTableVarColumnDefault(Plan APlan, Schema.TableVar ATableVar, Schema.TableVarColumn AColumn, DefaultDefinition ADefault)
		{
			int LDefaultID = Schema.Object.GetObjectID(ADefault.MetaData);
			string LDefaultName = Schema.Object.EnsureNameLength(String.Format("{0}_{1}_Default", ATableVar.Name, AColumn.Name));
			Schema.TableVarColumnDefault LDefault = new Schema.TableVarColumnDefault(LDefaultID, LDefaultName);
			LDefault.Library = ATableVar.Library == null ? null : APlan.CurrentLibrary;
			CompileDefault(APlan, LDefault, AColumn.DataType, ADefault);
			LDefault.IsGenerated = LDefault.IsGenerated || ATableVar.IsGenerated;
			return LDefault;
		}
		
		public static PlanNode CompileFrameNode(Plan APlan, Statement AStatement)
		{
			FrameNode LNode = new FrameNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			APlan.Symbols.PushFrame();
			try
			{
				LNode.Nodes.Add(CompileDeallocateFrameVariablesNode(APlan, CompileStatement(APlan, AStatement)));
				return LNode;
			}
			finally
			{
				APlan.Symbols.PopFrame();
			}
		}
		
		public static PlanNode CompileDeallocateFrameVariablesNode(Plan APlan, PlanNode ANode)
		{
			BlockNode LBlockNode = null;
			for (int LIndex = 0; LIndex < APlan.Symbols.FrameCount; LIndex++)
				if (APlan.Symbols[LIndex].DataType.IsDisposable)
				{
					if (LBlockNode == null)
					{
						LBlockNode = new BlockNode();
						LBlockNode.IsBreakable = false;
						LBlockNode.Nodes.Add(ANode);
					}
					LBlockNode.Nodes.Add(new DeallocateVariableNode(LIndex));
				}
				
			return LBlockNode == null ? ANode : LBlockNode;
		}
		
		public static PlanNode CompileDeallocateVariablesNode(Plan APlan, PlanNode ANode, Symbol AResultVar)
		{
			BlockNode LBlockNode = null;
			for (int LIndex = 0; LIndex < APlan.Symbols.Count; LIndex++)
				if ((APlan.Symbols[LIndex].Name != AResultVar.Name) && APlan.Symbols[LIndex].DataType.IsDisposable)
				{
					if (LBlockNode == null)
					{
						LBlockNode = new BlockNode();
						LBlockNode.IsBreakable = false;
						LBlockNode.Nodes.Add(ANode);
						ANode = LBlockNode;
					}
					LBlockNode.Nodes.Add(new DeallocateVariableNode(LIndex));
				}
				
			return ANode;
		}
		
		public static Schema.Device GetDefaultDevice(Plan APlan, bool AMustResolve)
		{
			// the first unambiguous device in a breadth-first traversal of the current library dependency graph
			Schema.Device LDevice = null;
			string LDefaultDeviceName = String.Empty;
			try
			{
				LDefaultDeviceName = APlan.GetDefaultDeviceName(APlan.CurrentLibrary.Name, true);
				if (LDefaultDeviceName != String.Empty)
				{
					Schema.Object LObject = ResolveCatalogIdentifier(APlan, LDefaultDeviceName, AMustResolve);
					if (!(LObject is Schema.Device) && AMustResolve)
						throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, APlan.CurrentStatement());
					LDevice = (Schema.Device)LObject;
				}
			}
			catch (Exception LException)
			{
				if (AMustResolve)
					throw new CompilerException(CompilerException.Codes.UnableToResolveDefaultDevice, APlan.CurrentStatement(), LException, LDefaultDeviceName);
			}

			if ((LDevice == null) && AMustResolve)
				LDevice = APlan.TempDevice;

			return LDevice;
		}
		
		public static Schema.TableVarColumn CompileTableVarColumnDefinition(Plan APlan, Schema.TableVar ATableVar, ColumnDefinition AColumn)
		{
			Schema.Column LNewColumn = new Schema.Column(AColumn.ColumnName, CompileTypeSpecifier(APlan, AColumn.TypeSpecifier));
			Schema.TableVarColumn LNewTableVarColumn = new Schema.TableVarColumn(Schema.Object.GetObjectID(AColumn.MetaData), LNewColumn, AColumn.MetaData, Schema.TableVarColumnType.Stored);
			LNewTableVarColumn.IsNilable = AColumn.IsNilable;
				
			foreach (ConstraintDefinition LConstraint in AColumn.Constraints)
				LNewTableVarColumn.Constraints.Add(CompileTableVarColumnConstraint(APlan, ATableVar, LNewTableVarColumn, LConstraint));
			
			// TODO: verify that the default satisfies the constraints
			if (AColumn.Default != null)
				LNewTableVarColumn.Default = CompileTableVarColumnDefault(APlan, ATableVar, LNewTableVarColumn, AColumn.Default);
				
			// if the default is not remotable, make sure that the DAE.IsDefaultRemotable tag is false, if it exists
			Tag LTag = LNewTableVarColumn.MetaData.Tags.GetTag("DAE.IsDefaultRemotable");
			if (LTag != Tag.None)
			{
				bool LRemotable = Boolean.Parse(LTag.Value);
				LNewTableVarColumn.IsDefaultRemotable = LNewTableVarColumn.IsDefaultRemotable && LRemotable;
				if (!(LNewTableVarColumn.IsDefaultRemotable ^ LRemotable))
					LNewTableVarColumn.MetaData.Tags.Update("DAE.IsDefaultRemotable", LNewTableVarColumn.IsDefaultRemotable.ToString());
			}
			
			// if the change is not remotable, make sure that the DAE.IsChangeRemotable tag is false, if it exists
			LTag = LNewTableVarColumn.MetaData.Tags.GetTag("DAE.IsChangeRemotable");
			if (LTag != Tag.None)
			{
				bool LRemotable = Boolean.Parse(LTag.Value);
				LNewTableVarColumn.IsChangeRemotable = LNewTableVarColumn.IsChangeRemotable && LRemotable;
				if (!(LNewTableVarColumn.IsChangeRemotable ^ LRemotable))
					LNewTableVarColumn.MetaData.Tags.Update("DAE.IsChangeRemotable", LNewTableVarColumn.IsChangeRemotable.ToString());
			}
			
			// if the validate is not remotable, make sure that the DAE.IsValidateRemotable tag is false, if it exists
			LTag = LNewTableVarColumn.MetaData.Tags.GetTag("DAE.IsValidateRemotable");
			if (LTag != Tag.None)
			{
				bool LRemotable = Boolean.Parse(LTag.Value);
				LNewTableVarColumn.IsValidateRemotable = LNewTableVarColumn.IsValidateRemotable && LRemotable;
				if (!(LNewTableVarColumn.IsValidateRemotable ^ LRemotable))
					LNewTableVarColumn.MetaData.Tags.Update("DAE.IsValidateRemotable", LNewTableVarColumn.IsValidateRemotable.ToString());
			}
			
			return LNewTableVarColumn;
		}
		
		public static PlanNode CompileCreateTableStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateTableStatement LStatement = (CreateTableStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateTable);
			BlockNode LBlockNode = new BlockNode();
			LBlockNode.SetLineInfo(AStatement.LineInfo);
			CreateTableNode LNode = new CreateTableNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LBlockNode.Nodes.Add(LNode);
			
			Tag LTag;
			APlan.Symbols.PushWindow(0); // make sure the create table statement is evaluated in a private context
			try
			{
				string LTableName = Schema.Object.Qualify(LStatement.TableVarName, APlan.CurrentLibrary.Name);
				string LSessionTableName = null;
				string LSourceTableName = null;
				if (LStatement.IsSession)
				{
					LSessionTableName = LTableName;
					if (APlan.IsRepository)
						LTableName = MetaData.GetTag(LStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						LTableName = Schema.Object.NameFromGuid(Guid.NewGuid());
					CheckValidSessionObjectName(APlan, AStatement, LSessionTableName);
					APlan.PlanSessionObjects.Add(new Schema.SessionObject(LSessionTableName, LTableName));
				}
				else if ((LStatement.MetaData != null) && LStatement.MetaData.Tags.Contains("DAE.SourceTableName"))
					LSourceTableName = MetaData.GetTag(LStatement.MetaData, "DAE.SourceTableName", String.Empty);
				
				CheckValidCatalogObjectName(APlan, AStatement, LTableName);

				LNode.Table = new Schema.BaseTableVar(Schema.Object.GetObjectID(LStatement.MetaData), LTableName);
				LNode.Table.SessionObjectName = LSessionTableName;
				LNode.Table.SessionID = APlan.SessionID;
				LNode.Table.SourceTableName = LSourceTableName;
				LNode.Table.IsDeletedTable = Boolean.Parse(MetaData.GetTag(LStatement.MetaData, "DAE.IsDeletedTable", "False"));
				LNode.Table.IsGenerated = LStatement.IsSession || (LSourceTableName != null);
				LNode.Table.Owner = APlan.User;
				LNode.Table.Library = LNode.Table.IsGenerated ? null : APlan.CurrentLibrary;
				LNode.Table.MetaData = LStatement.MetaData;
				APlan.PlanCatalog.Add(LNode.Table);
				try
				{
					if ((APlan.ApplicationTransactionID != Guid.Empty) && (LSourceTableName != null) && !APlan.IsLoading())
					{
						ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
						try
						{
							LTransaction.Device.AddTableMap(APlan.ServerProcess, LNode.Table);
						}
						finally
						{
							Monitor.Exit(LTransaction);
						}
					}

					APlan.PushCreationObject(LNode.Table);
					try
					{
						Schema.Object LObject = null;
						if ((LStatement.DeviceName != null) && !APlan.IsRepository)
							LObject = ResolveCatalogIdentifier(APlan, LStatement.DeviceName.Identifier);

						if (LObject == null)
							if (LNode.Table.SessionObjectName != null)
								LObject = APlan.TempDevice;
							else
								LObject = GetDefaultDevice(APlan, true);
						
						if (!(LObject is Schema.Device))
							throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, LStatement.DeviceName != null ? (Statement)LStatement.DeviceName : LStatement);
							
						APlan.AttachDependency(LObject);
						LNode.Table.Device = (Schema.Device)LObject;
						APlan.CheckDeviceReconcile(LNode.Table);
						
						if (LStatement.FromExpression == null)
						{
							LNode.Table.DataType = (Schema.ITableType)new Schema.TableType();

							Schema.TableVarColumn LNewTableVarColumn;
							foreach (ColumnDefinition LColumn in LStatement.Columns)
							{
								LNewTableVarColumn = CompileTableVarColumnDefinition(APlan, LNode.Table, LColumn);
								LNode.Table.DataType.Columns.Add(LNewTableVarColumn.Column);
								LNode.Table.Columns.Add(LNewTableVarColumn);
							}
							
							APlan.EnsureDeviceStarted(LNode.Table.Device);
							LNode.Table.Device.CheckSupported(APlan, LNode.Table); // This call must be made before any attempt to compile keys for the table is made
		
							CompileCreateTableVarStatement(APlan, LStatement, LNode, LBlockNode);
							
							if (LNode.Table.Keys.Count == 0)
								throw new CompilerException(CompilerException.Codes.KeyRequired, AStatement);

							LNode.Table.DetermineRemotable(APlan.CatalogDeviceSession);
							LTag = LNode.Table.MetaData.Tags.GetTag("DAE.IsDefaultRemotable");
							if (LTag != Tag.None)
							{
								bool LRemotable = Boolean.Parse(LTag.Value);
								LNode.Table.IsDefaultRemotable = LNode.Table.IsDefaultRemotable && LRemotable;
								if (!(LNode.Table.IsDefaultRemotable ^ LRemotable))
									LNode.Table.MetaData.Tags.Update("DAE.IsDefaultRemotable", LNode.Table.IsDefaultRemotable.ToString());
							}

							LTag = LNode.Table.MetaData.Tags.GetTag("DAE.IsChangeRemotable");
							if (LTag != Tag.None)
							{
								bool LRemotable = Boolean.Parse(LTag.Value);
								LNode.Table.IsChangeRemotable = LNode.Table.IsChangeRemotable && LRemotable;
								if (!(LNode.Table.IsChangeRemotable ^ LRemotable))
									LNode.Table.MetaData.Tags.Update("DAE.IsChangeRemotable", LNode.Table.IsChangeRemotable.ToString());
							}

							LTag = LNode.Table.MetaData.Tags.GetTag("DAE.IsValidateRemotable");
							if (LTag != Tag.None)
							{
								bool LRemotable = Boolean.Parse(LTag.Value);
								LNode.Table.IsValidateRemotable = LNode.Table.IsValidateRemotable && LRemotable;
								if (!(LNode.Table.IsValidateRemotable ^ LRemotable))
									LNode.Table.MetaData.Tags.Update("DAE.IsValidateRemotable", LNode.Table.IsValidateRemotable.ToString());
							}
						}
						else
						{
							APlan.PopCreationObject();
							try
							{
								APlan.Symbols.PopWindow();
								try
								{
									PlanNode LFromNode = CompileExpression(APlan, LStatement.FromExpression);
								
									if (!(LFromNode.DataType is Schema.ITableType))
										throw new CompilerException(CompilerException.Codes.TableExpressionExpected, LStatement.FromExpression);
										
									LFromNode = EnsureTableNode(APlan, LFromNode);
										
									LNode.Table.CopyTableVar((TableNode)LFromNode);
									
									InsertNode LInsertNode = new InsertNode();
									LInsertNode.SetLineInfo(LStatement.FromExpression.LineInfo);
										
									LInsertNode.Nodes.Add(LFromNode);
									APlan.PushStatementContext(new StatementContext(StatementType.Insert));
									try
									{
										LInsertNode.Nodes.Add(EmitBaseTableVarNode(APlan, LNode.Table));
									}
									finally
									{
										APlan.PopStatementContext();
									}
									
									LInsertNode.DetermineDataType(APlan);
									LInsertNode.DetermineCharacteristics(APlan);

									LBlockNode.Nodes.Add(LInsertNode);
								}
								finally
								{
									APlan.Symbols.PushWindow(0);
								}
							}
							finally
							{
								APlan.PushCreationObject(LNode.Table);
							}

							APlan.EnsureDeviceStarted(LNode.Table.Device);
							LNode.Table.Device.CheckSupported(APlan, LNode.Table);
		
							foreach (Schema.TableVarColumn LColumn in LNode.Table.Columns)
								if (LColumn.DataType is Schema.ScalarType)
									APlan.AttachDependency((Schema.ScalarType)LColumn.DataType);
							LNode.Table.DetermineRemotable(APlan.CatalogDeviceSession);
						}
								
						return LBlockNode;
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}
				catch
				{
					APlan.PlanCatalog.SafeRemove(LNode.Table);
					throw;
				}
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static PlanNode CompileCreateViewStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateViewStatement LStatement = (CreateViewStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateView);
			BlockNode LBlockNode = new BlockNode();
			LBlockNode.SetLineInfo(AStatement.LineInfo);
			CreateViewNode LNode = new CreateViewNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LBlockNode.Nodes.Add(LNode);

			// Generate the TableType for this table
			string LViewName = Schema.Object.Qualify(LStatement.TableVarName, APlan.CurrentLibrary.Name);
			string LSessionViewName = null;
			string LSourceTableName = null;
			if (LStatement.IsSession)
			{
				LSessionViewName = LViewName;
				if (APlan.IsRepository)
					LViewName = MetaData.GetTag(LStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
				else
					LViewName = Schema.Object.NameFromGuid(Guid.NewGuid());
				CheckValidSessionObjectName(APlan, AStatement, LSessionViewName);
				APlan.PlanSessionObjects.Add(new Schema.SessionObject(LSessionViewName, LViewName));
			}
			else if ((LStatement.MetaData != null) && LStatement.MetaData.Tags.Contains("DAE.SourceTableName"))
			{
				LSourceTableName = MetaData.GetTag(LStatement.MetaData, "DAE.SourceTableName", String.Empty);
			}
			
			CheckValidCatalogObjectName(APlan, AStatement, LViewName);

			LNode.View = new Schema.DerivedTableVar(Schema.Object.GetObjectID(LStatement.MetaData), LViewName);
			LNode.View.SessionObjectName = LSessionViewName;
			LNode.View.SessionID = APlan.SessionID;
			LNode.View.SourceTableName = LSourceTableName;
			LNode.View.IsGenerated = LStatement.IsSession || (LSourceTableName != null);
			LNode.View.Owner = APlan.User;
			LNode.View.Library = LNode.View.IsGenerated ? null : APlan.CurrentLibrary;
			LNode.View.MetaData = LStatement.MetaData;
			if (!APlan.IsRepository) // if this is a repository, a view could be parameterized, because it will never be executed
				APlan.Symbols.PushWindow(0); // make sure the view expression is evaluated in a private context
			try
			{
				if ((APlan.ApplicationTransactionID != Guid.Empty) && (LSourceTableName != null) && !APlan.IsLoading())
				{
					ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
					try
					{
						LTransaction.Device.AddTableMap(APlan.ServerProcess, LNode.View);
					}
					finally
					{
						Monitor.Exit(LTransaction);
					}
				}

				APlan.PushCreationObject(LNode.View);
				try
				{
					#if USEORIGINALEXPRESSION
					LNode.View.OriginalExpression = LStatement.Expression;
					#endif
					PlanNode LPlanNode = CompileExpression(APlan, LStatement.Expression);
					if (!(LPlanNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableExpressionExpected, LStatement.Expression);
						
					LPlanNode = EnsureTableNode(APlan, LPlanNode);

					LNode.View.CopyTableVar((TableNode)LPlanNode, LPlanNode is TableVarNode);
					
					// If we are in an A/T, or we are loading an A/T object
					if ((APlan.ApplicationTransactionID != Guid.Empty) || LNode.View.IsATObject)
					{
						// Set the explicit bind for A/T variables resolved within the view expression
						ApplicationTransactionUtility.SetExplicitBind(LPlanNode);
					}

					LNode.View.InvocationExpression = (Expression)LPlanNode.EmitStatement(EmitMode.ForCopy);
					
					APlan.PlanCatalog.Add(LNode.View);
					try
					{
						CompileCreateTableVarStatement(APlan, LStatement, LNode, LBlockNode);
						
						LNode.View.CopyReferences((TableNode)LPlanNode);
						if (LPlanNode is TableVarNode)
							LNode.View.InheritMetaData(((TableNode)LPlanNode).TableVar.MetaData);
						else
							LNode.View.MergeMetaData(((TableNode)LPlanNode).TableVar.MetaData);
						LNode.View.MergeMetaData(LStatement.MetaData);
						LNode.View.DetermineRemotable(APlan.CatalogDeviceSession);
						return LBlockNode;
					}
					catch
					{
						APlan.PlanCatalog.SafeRemove(LNode.View);
						throw;
					}
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
			finally
			{
				if (!APlan.IsRepository)
					APlan.Symbols.PopWindow();
			}
		}
		
		public static void ReinferViewReferences(Plan APlan, Schema.DerivedTableVar AView)
		{
			if (!APlan.IsRepository && AView.ShouldReinferReferences)
			{
				Schema.Objects LSaveSourceReferences = new Schema.Objects();
				Schema.Objects LSaveTargetReferences = new Schema.Objects();
				foreach (Schema.Reference LReference in AView.DerivedReferences)
				{
					if (AView.SourceReferences.Contains(LReference))
						AView.SourceReferences.Remove(LReference);
					if (!LSaveSourceReferences.Contains(LReference))
						LSaveSourceReferences.Add(LReference);
					if (AView.TargetReferences.Contains(LReference))
						AView.TargetReferences.Remove(LReference);
					if (!LSaveTargetReferences.Contains(LReference))
						LSaveTargetReferences.Add(LReference);
				}
				
				AView.DerivedReferences.Clear();
				try
				{
					ApplicationTransaction LTransaction = null;
					if (!AView.IsATObject && (APlan.ApplicationTransactionID != Guid.Empty))
						LTransaction = APlan.GetApplicationTransaction();
					try
					{
						if (LTransaction != null)
							LTransaction.PushGlobalContext();
						try
						{
							Plan LPlan = new Plan(APlan.ServerProcess);
							try
							{
								LPlan.PushATCreationContext();
								try
								{
									LPlan.PushSecurityContext(new SecurityContext(AView.Owner));
									try
									{
										#if USEORIGINALEXPRESSION
										PlanNode LPlanNode = CompileExpression(LPlan, AView.OriginalExpression);
										#else
										PlanNode LPlanNode = CompileExpression(LPlan, AView.InvocationExpression);
										#endif
										LPlan.CheckCompiled();
										LPlanNode = EnsureTableNode(LPlan, LPlanNode);
										AView.CopyReferences((TableNode)LPlanNode);
										AView.ShouldReinferReferences = false;
									}
									finally
									{
										LPlan.PopSecurityContext();
									}
								}
								finally
								{
									LPlan.PopATCreationContext();
								}
							}
							finally
							{
								LPlan.Dispose();
							}
						}
						finally
						{
							if (LTransaction != null)
								LTransaction.PopGlobalContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							Monitor.Exit(LTransaction);
					}
				}
				catch
				{
					AView.DerivedReferences.Clear();
					AView.SourceReferences.Clear();
					AView.TargetReferences.Clear();
					
					foreach (Schema.Reference LReference in LSaveSourceReferences)
					{
						if (!AView.SourceReferences.Contains(LReference))
							AView.SourceReferences.Add(LReference);
						if (!AView.DerivedReferences.Contains(LReference))
							AView.DerivedReferences.Add(LReference);
					}
					
					foreach (Schema.Reference LReference in LSaveTargetReferences)
					{
						if (!AView.TargetReferences.Contains(LReference))
							AView.TargetReferences.Add(LReference);
						if (!AView.DerivedReferences.Contains(LReference))
							AView.DerivedReferences.Add(LReference);
					}
					
					throw;
				}
				
				AView.SetShouldReinferReferences(APlan.CatalogDeviceSession);
			}
		}
		
		public static Schema.Operator CompileRepresentationSelector(Plan APlan, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, AccessorBlock ASelector)
		{
			// Selector
			// operator <type name>[.<representation name>](const <property name> : <property type>[, ...]) : <type>
			string LOperatorName = AScalarType.Name;
			if (!Schema.Object.NamesEqual(LOperatorName, ARepresentation.Name))
				LOperatorName = Schema.Object.Qualify(ARepresentation.Name, LOperatorName);
			Schema.Operator LOperator = new Schema.Operator(LOperatorName);
			LOperator.IsGenerated = true;
			LOperator.Generator = ARepresentation;
			LOperator.ReturnDataType = AScalarType;
			LOperator.Library = AScalarType.Library;
			LOperator.Owner = AScalarType.Owner;
			APlan.PushCreationObject(LOperator);
			try
			{
				APlan.AttachDependency(AScalarType);

				foreach (Schema.Property LProperty in ARepresentation.Properties)
				{
					LOperator.Operands.Add(new Schema.Operand(LOperator, LProperty.Name, LProperty.DataType, Modifier.Const));
					APlan.AttachDependencies(LProperty.Dependencies);
				}

				if (ASelector.ClassDefinition != null)
				{
					if (!ARepresentation.IsDefaultSelector)
						APlan.CheckRight(Schema.RightNames.HostImplementation);
					APlan.CheckClassDependency(LOperator.Library, ASelector.ClassDefinition);
					LOperator.Block.ClassDefinition = ASelector.ClassDefinition;
				}
				else
				{
					Statement LStatement;
					if (ASelector.Expression != null)
						LStatement = new AssignmentStatement(new IdentifierExpression(Keywords.Result), ASelector.Expression);
					else
						LStatement = ASelector.Block;
						
					LOperator.Block.BlockNode = BindOperatorBlock(APlan, LOperator, CompileOperatorBlock(APlan, LOperator, LStatement));
					LOperator.DetermineRemotable(APlan.CatalogDeviceSession);
					if (!(LOperator.IsFunctional && LOperator.IsDeterministic && LOperator.IsRemotable))
						throw new CompilerException(CompilerException.Codes.InvalidSelector, ASelector, ARepresentation.Name, AScalarType.Name);
				}

				return LOperator;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompilePropertyReadAccessor(Plan APlan, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, Schema.Property AProperty, AccessorBlock AReadAccessor)
		{
			// Read Accessor
			// operator <type name>.Read<property name>(const value : <type>) : <property type>
			Schema.Operator LOperator = new Schema.Operator(String.Format("{0}{1}{2}{3}", AScalarType.Name, Keywords.Qualifier, CReadAccessorName, AProperty.Name));
			LOperator.IsGenerated = true;
			LOperator.Generator = AProperty;
			LOperator.ReturnDataType = AProperty.DataType;
			LOperator.Operands.Add(new Schema.Operand(LOperator, Keywords.Value, AScalarType, Modifier.Const));
			LOperator.Library = AScalarType.Library;
			LOperator.Owner = AScalarType.Owner;
			APlan.PushCreationObject(LOperator);
			try
			{
				APlan.AttachDependency(AScalarType);
				APlan.AttachDependencies(AProperty.Dependencies);
				if (AReadAccessor.ClassDefinition != null)
				{
					if (!AProperty.IsDefaultReadAccessor)
						APlan.CheckRight(Schema.RightNames.HostImplementation);
					APlan.CheckClassDependency(LOperator.Library, AReadAccessor.ClassDefinition);
					LOperator.Block.ClassDefinition = AReadAccessor.ClassDefinition;
				}
				else
				{
					Statement LStatement;
					if (AReadAccessor.Expression != null)
						LStatement = new AssignmentStatement(new IdentifierExpression(Keywords.Result), AReadAccessor.Expression);
					else
						LStatement = AReadAccessor.Block;
						
					LOperator.Block.BlockNode = BindOperatorBlock(APlan, LOperator, CompileOperatorBlock(APlan, LOperator, LStatement));
					LOperator.DetermineRemotable(APlan.CatalogDeviceSession);
					if (!(LOperator.IsFunctional && LOperator.IsDeterministic && LOperator.IsRemotable))
						throw new CompilerException(CompilerException.Codes.InvalidReadAccessor, AReadAccessor, AProperty.Name, ARepresentation.Name, AScalarType.Name);
				}

				return LOperator;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompilePropertyWriteAccessor(Plan APlan, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, Schema.Property AProperty, AccessorBlock AWriteAccessor)
		{
			// Write Accessor
			// operator <type name>.Write<property name>(const value : <type>, const <property name> : <property type>) : <type>
			Schema.Operator LOperator = new Schema.Operator(String.Format("{0}{1}{2}{3}", AScalarType.Name, Keywords.Qualifier, CWriteAccessorName, AProperty.Name));
			LOperator.IsGenerated = true;
			LOperator.Generator = AProperty;
			LOperator.ReturnDataType = AScalarType;
			LOperator.Operands.Add(new Schema.Operand(LOperator, Keywords.Value, AScalarType, Modifier.Const));
			LOperator.Operands.Add(new Schema.Operand(LOperator, AProperty.Name, AProperty.DataType, Modifier.Const));
			LOperator.Library = AScalarType.Library;
			LOperator.Owner = AScalarType.Owner;
			APlan.PushCreationObject(LOperator);
			try
			{
				APlan.AttachDependency(AScalarType);
				APlan.AttachDependencies(AProperty.Dependencies);

				if (AWriteAccessor.ClassDefinition != null)
				{
					if (!AProperty.IsDefaultWriteAccessor)
						APlan.CheckRight(Schema.RightNames.HostImplementation);
					APlan.CheckClassDependency(LOperator.Library, AWriteAccessor.ClassDefinition);
					LOperator.Block.ClassDefinition = AWriteAccessor.ClassDefinition;
				}
				else
				{
					Statement LStatement;
					if (AWriteAccessor.Expression != null)
						LStatement = new AssignmentStatement(new IdentifierExpression(Keywords.Result), AWriteAccessor.Expression);
					else
						LStatement = AWriteAccessor.Block;
						
					LOperator.Block.BlockNode = BindOperatorBlock(APlan, LOperator, CompileOperatorBlock(APlan, LOperator, LStatement));
					LOperator.DetermineRemotable(APlan.CatalogDeviceSession);
					if (!(LOperator.IsDeterministic && LOperator.IsRemotable))
						throw new CompilerException(CompilerException.Codes.InvalidWriteAccessor, AWriteAccessor, AProperty.Name, ARepresentation.Name, AScalarType.Name);
				}

				return LOperator;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Property CompileProperty(Plan APlan, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, PropertyDefinition APropertyDefinition)
		{
			Schema.Property LProperty = new Schema.Property(Schema.Object.GetObjectID(APropertyDefinition.MetaData), APropertyDefinition.PropertyName);
			APlan.PushCreationObject(LProperty);
			try
			{
				LProperty.MergeMetaData(APropertyDefinition.MetaData);
				LProperty.DataType = CompileTypeSpecifier(APlan, APropertyDefinition.PropertyType);
				ARepresentation.Properties.Add(LProperty);
				return LProperty;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static void CompilePropertyAccessors(Plan APlan, Schema.ScalarType AScalarType, Schema.Objects AOperators, Schema.Representation ARepresentation, Schema.Property AProperty, PropertyDefinition APropertyDefinition)
		{
			APlan.PushCreationObject(AProperty);
			try
			{
				AccessorBlock LReadAccessorBlock = APropertyDefinition.ReadAccessorBlock;

				// Build a default read accessor for the property
				if (LReadAccessorBlock == null)
				{
					if (!ARepresentation.IsDefaultSelector)
						throw new CompilerException(CompilerException.Codes.DefaultReadAccessorCannotBeProvided, APropertyDefinition, AProperty.Name, ARepresentation.Name, AScalarType.Name);
						
					AProperty.IsDefaultReadAccessor = true;
					LReadAccessorBlock = new AccessorBlock();
					if (!AScalarType.IsCompound)
						LReadAccessorBlock.ClassDefinition = DefaultReadAccessor();
					else
						LReadAccessorBlock.ClassDefinition = DefaultCompoundReadAccessor(AProperty.Name);
				}

				// Compile the read accessor
				if (APlan.InLoadingContext())
				{
					AProperty.LoadReadAccessorID();
					AProperty.LoadDependencies(APlan.CatalogDeviceSession);
				}
				else
				{
					AProperty.ReadAccessor = CompilePropertyReadAccessor(APlan, AScalarType, ARepresentation, AProperty, LReadAccessorBlock);
					APlan.PlanCatalog.Add(AProperty.ReadAccessor);
					APlan.AttachDependencies(AProperty.ReadAccessor.Dependencies);
					AOperators.Add(AProperty.ReadAccessor);
					APlan.Catalog.OperatorResolutionCache.Clear(AProperty.ReadAccessor.OperatorName);
				}

				AccessorBlock LWriteAccessorBlock = APropertyDefinition.WriteAccessorBlock;

				// Build a default write accessor for the property
				if (LWriteAccessorBlock == null)
				{
					if (!ARepresentation.IsDefaultSelector)
						throw new CompilerException(CompilerException.Codes.DefaultWriteAccessorCannotBeProvided, APropertyDefinition, AProperty.Name, ARepresentation.Name, AScalarType.Name);
						
					AProperty.IsDefaultWriteAccessor = true;
					LWriteAccessorBlock = new AccessorBlock();
					if (!AScalarType.IsCompound)
						LWriteAccessorBlock.ClassDefinition = DefaultWriteAccessor();
					else
						LWriteAccessorBlock.ClassDefinition = DefaultCompoundWriteAccessor(AProperty.Name);
				}
				
				if (APlan.InLoadingContext())
				{
					AProperty.LoadWriteAccessorID();
				}
				else
				{
					AProperty.WriteAccessor = CompilePropertyWriteAccessor(APlan, AScalarType, ARepresentation, AProperty, LWriteAccessorBlock);
					APlan.PlanCatalog.Add(AProperty.WriteAccessor);
					APlan.AttachDependencies(AProperty.WriteAccessor.Dependencies);
					AOperators.Add(AProperty.WriteAccessor);
					APlan.Catalog.OperatorResolutionCache.Clear(AProperty.WriteAccessor.OperatorName);
				}

				AProperty.RemoveDependency(AProperty.Representation.ScalarType); // Remove the dependencies for native types to prevent recursion.
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Representation CompileRepresentation(Plan APlan, Schema.ScalarType AScalarType, Schema.Objects AOperators, RepresentationDefinition ADefinition)
		{
			Schema.Representation LRepresentation = new Schema.Representation(Schema.Object.GetObjectID(ADefinition.MetaData), ADefinition.RepresentationName);
			LRepresentation.IsGenerated = ADefinition.IsGenerated;
			if (LRepresentation.IsGenerated)
				LRepresentation.Generator = AScalarType;
			LRepresentation.Library = APlan.CurrentLibrary;
			APlan.PushCreationObject(LRepresentation);
			try
			{
				LRepresentation.MergeMetaData(ADefinition.MetaData);
				LRepresentation.LoadIsGenerated();
				LRepresentation.LoadGeneratorID();
				AScalarType.Representations.Add(LRepresentation);
				try
				{
					foreach (PropertyDefinition LPropertyDefinition in ADefinition.Properties)
						CompileProperty(APlan, AScalarType, LRepresentation, LPropertyDefinition);
						
					AccessorBlock LSelectorBlock = ADefinition.SelectorAccessorBlock;
						
					// Build a default selector for the representation
					if (LSelectorBlock == null)
					{
						if (AScalarType.IsDefaultConveyor)
							throw new CompilerException(CompilerException.Codes.MultipleSystemProvidedRepresentations, ADefinition, LRepresentation.Name, AScalarType.Name);
							
						LSelectorBlock = new AccessorBlock();
						if ((LRepresentation.Properties.Count == 1) && (LRepresentation.Properties[0].DataType is Schema.ScalarType) && !((Schema.ScalarType)LRepresentation.Properties[0].DataType).IsCompound)
						{
							LSelectorBlock.ClassDefinition = DefaultSelector();
							
							// Use the native representation of the single simple scalar property
							AScalarType.ClassDefinition = (ClassDefinition)((Schema.ScalarType)LRepresentation.Properties[0].DataType).ClassDefinition.Clone();
							AScalarType.NativeType = ((Schema.ScalarType)LRepresentation.Properties[0].DataType).NativeType;
						}
						else
						{
							if (AScalarType.ClassDefinition != null)
								throw new CompilerException(CompilerException.Codes.InvalidConveyorForCompoundScalar, ADefinition, LRepresentation.Name, AScalarType.Name);

							LSelectorBlock.ClassDefinition = DefaultCompoundSelector();
							AScalarType.IsCompound = true;
							
							// Compile the row type for the native representation
							AScalarType.CompoundRowType = new Schema.RowType();
							foreach (Schema.Property LProperty in LRepresentation.Properties)
								AScalarType.CompoundRowType.Columns.Add(new Schema.Column(LProperty.Name, LProperty.DataType));
						}

						LRepresentation.IsDefaultSelector = true;
						AScalarType.IsDefaultConveyor = true;
					}
					
					if (APlan.InLoadingContext())
					{
						LRepresentation.LoadSelectorID();
						LRepresentation.LoadDependencies(APlan.CatalogDeviceSession);
					}
					else
					{
						LRepresentation.Selector = CompileRepresentationSelector(APlan, AScalarType, LRepresentation, LSelectorBlock);
						APlan.PlanCatalog.Add(LRepresentation.Selector);
						APlan.AttachDependencies(LRepresentation.Selector.Dependencies);
						AOperators.Add(LRepresentation.Selector);
						APlan.Catalog.OperatorResolutionCache.Clear(LRepresentation.Selector.OperatorName);
					}

					LRepresentation.RemoveDependency(AScalarType);

					foreach (PropertyDefinition LPropertyDefinition in ADefinition.Properties)
						CompilePropertyAccessors(APlan, AScalarType, AOperators, LRepresentation, LRepresentation.Properties[LPropertyDefinition.PropertyName], LPropertyDefinition);
						
					return LRepresentation;
				}
				catch
				{
					AScalarType.Representations.Remove(LRepresentation);
					throw;
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		protected static ClassDefinition DefaultSelector()
		{
			return new ClassDefinition("System.ScalarSelectorNode");
		}
		
		protected static ClassDefinition DefaultCompoundSelector()
		{
			return new ClassDefinition("System.CompoundScalarSelectorNode");
		}
		
		protected static ClassDefinition DefaultReadAccessor()
		{
			return new ClassDefinition("System.ScalarReadAccessorNode");
		}
		
		protected static ClassDefinition DefaultCompoundReadAccessor(string APropertyName)
		{
			return new ClassDefinition("System.CompoundScalarReadAccessorNode", new ClassAttributeDefinition[]{new ClassAttributeDefinition("PropertyName", APropertyName)});
		}
		
		protected static ClassDefinition DefaultWriteAccessor()
		{
			return new ClassDefinition("System.ScalarWriteAccessorNode");
		}
		
		protected static ClassDefinition DefaultCompoundWriteAccessor(string APropertyName)
		{
			return new ClassDefinition("System.CompoundScalarWriteAccessorNode", new ClassAttributeDefinition[]{new ClassAttributeDefinition("PropertyName", APropertyName)});
		}

		#if USETYPEINHERITANCE		
		protected static Schema.ScalarType FindBaseSystemType(Schema.ScalarType AScalarType, Schema.ScalarType AParentType)
		{
			if (AParentType.IsSystem)
				if ((AParentType.ClassDefinition != null) && ((AScalarType.ClassDefinition == null) || (AParentType.ClassDefinition.ClassName == AScalarType.ClassDefinition.ClassName)))
					return AParentType;
				else
					return null;
			else if (AParentType.ParentTypes.Count == 1)
				return FindBaseSystemType(AScalarType, AParentType.ParentTypes[0]);
			else
				return null;
		}
		
		// The first system type in a single inheritance type graph, if that type is concrete and uses the same conveyor as this type
		protected static Schema.ScalarType FindBaseSystemType(Schema.ScalarType AScalarType)
		{
			if (AScalarType.ParentTypes.Count == 1)
				return FindBaseSystemType(AScalarType, AScalarType.ParentTypes[0]);
			else
				return null;
		}

		public static void CompileDefaultRepresentation(Plan APlan, Schema.ScalarType AScalarType, Schema.Objects AOperators)
		{
			if (!AScalarType.Representations.Contains(AScalarType.Name))
			{
				Schema.IScalarType LBaseType = FindBaseSystemType(AScalarType);
				if (LBaseType != null)
				{
					// Build a default representation of the form:
					// representation <type name> { <type name> : <base type name> read <default read accessor> write <default write accessor } : <default selector>
					RepresentationDefinition LDefinition = new RepresentationDefinition(Schema.Object.Unqualify(AScalarType.Name));
					LDefinition.Properties.Add(new PropertyDefinition(Schema.Object.Unqualify(AScalarType.Name), new ScalarTypeSpecifier(LBaseType.Name)));
					CompileRepresentation(APlan, AScalarType, AOperators, LDefinition);
				}
			}
		}
		#endif
		
		public static Schema.Operator CompileSpecialOperator(Plan APlan, Schema.ScalarType AScalarType)
		{
			// create an operator of the form <library name>.IsSpecial(AValue : <scalar type name>) : boolean
			// Compile IsSpecial operator (result := Parent.IsSpecial(AValue) or AValue = Special1Value or AValue = Special2Value...)
			Schema.Operator LOperator = new Schema.Operator(Schema.Object.Qualify(CIsSpecialOperatorName, AScalarType.Library.Name));
			LOperator.IsGenerated = true;
			LOperator.Generator = AScalarType;
			LOperator.Operands.Add(new Schema.Operand(LOperator, "AValue", AScalarType, Modifier.Const));
			LOperator.ReturnDataType = APlan.DataTypes.SystemBoolean;
			LOperator.Owner = AScalarType.Owner;
			LOperator.Library = AScalarType.Library;

			APlan.PushCreationObject(LOperator);
			try
			{
				APlan.AttachDependency(APlan.DataTypes.SystemBoolean);
				APlan.AttachDependency(AScalarType);
				
				APlan.Symbols.Push(new Symbol("AValue", AScalarType));
				try
				{
					APlan.Symbols.Push(new Symbol(Keywords.Result, LOperator.ReturnDataType));
					try
					{
						PlanNode LAnySpecialNode = null;
						bool LAttachOr = false;
						#if USETYPEINHERITANCE
						foreach (Schema.IScalarType LParentType in AScalarType.ParentTypes)
						{
							PlanNode LIsSpecialNode = 
								Compiler.EmitCallNode
								(
									APlan, 
									CIsSpecialOperatorName, 
									new PlanNode[]{new StackReferenceNode("AValue", LParentType, 1)}
								);
								
							if (LAnySpecialNode != null)
							{
								LAnySpecialNode = Compiler.EmitBinaryNode(APlan, LAnySpecialNode, Instructions.Or, LIsSpecialNode);
								LAttachOr = true;
							}
							else
								LAnySpecialNode = LIsSpecialNode;
						}
						#endif

						if (LAnySpecialNode == null)
							LAnySpecialNode = new ValueNode(APlan.DataTypes.SystemBoolean, false);
							
						foreach (Schema.Special LSpecial in AScalarType.Specials)
						{
							LAnySpecialNode = Compiler.EmitBinaryNode(APlan, LAnySpecialNode, Instructions.Or, LSpecial.Comparer.Block.BlockNode.Nodes[1]);
							LAttachOr = true;
						}

						// make sure that the scalar type includes a dependency on the boolean or operator					
						if (LAttachOr)
							AScalarType.AddDependency(ResolveOperator(APlan, Instructions.Or, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(APlan.DataTypes.SystemBoolean, Modifier.Const), new Schema.SignatureElement(APlan.DataTypes.SystemBoolean, Modifier.Const)}), false));
							
						LOperator.Block.BlockNode = new AssignmentNode(new StackReferenceNode(Keywords.Result, LOperator.ReturnDataType, 0), LAnySpecialNode);
						LOperator.Block.BlockNode.Line = 1;
						LOperator.Block.BlockNode.LinePos = 1;
						LOperator.Block.BlockNode = OptimizeNode(APlan, LOperator.Block.BlockNode);
						LOperator.Block.BlockNode = BindNode(APlan, LOperator.Block.BlockNode);

						// Attach the dependencies for each special comparer to the IsSpecial operator
						foreach (Schema.Special LNewSpecial in AScalarType.Specials)
							if (LNewSpecial.Comparer.HasDependencies())
								LOperator.AddDependencies(LNewSpecial.Comparer.Dependencies);
									
						LOperator.DetermineRemotable(APlan.CatalogDeviceSession);

						return LOperator;
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompileSpecialSelector(Plan APlan, Schema.ScalarType AScalarType, Schema.Special ASpecial, string ASpecialName, PlanNode AValueNode)
		{
			// Create an operator of the form ScalarTypeNameSpecialName() : ScalarType as a selector for the given special
			Schema.Operator LOperator = new Schema.Operator(String.Format("{0}{1}", AScalarType.Name, ASpecialName));
			LOperator.IsGenerated = true;
			LOperator.Generator = ASpecial;
			LOperator.ReturnDataType = AScalarType;
			LOperator.Owner = AScalarType.Owner;
			LOperator.Library = AScalarType.Library;
			APlan.PushCreationObject(LOperator);
			try
			{
				APlan.AttachDependency(AScalarType);

				APlan.Symbols.Push(new Symbol(Keywords.Result, LOperator.ReturnDataType));
				try
				{
					LOperator.Block.BlockNode = new AssignmentNode(new StackReferenceNode(Keywords.Result, LOperator.ReturnDataType, 0), AValueNode);
					LOperator.Block.BlockNode.Line = 1;
					LOperator.Block.BlockNode.LinePos = 1;
					LOperator.Block.BlockNode = OptimizeNode(APlan, LOperator.Block.BlockNode);
					LOperator.Block.BlockNode = BindNode(APlan, LOperator.Block.BlockNode);
					return LOperator;
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompileSpecialComparer(Plan APlan, Schema.ScalarType AScalarType, Schema.Special ASpecial, string ASpecialName, PlanNode AValueNode)
		{
			// Create an operator of the form <library name>.Is<special name>(const AValue : <scalar type name>) : Boolean as a comparison operator for the given special
			Schema.Operator LOperator = new Schema.Operator(Schema.Object.Qualify(String.Format("{0}{1}", CIsSpecialComparerPrefix, ASpecialName), AScalarType.Library.Name));
			LOperator.IsGenerated = true;
			LOperator.Generator = ASpecial;
			APlan.PushCreationObject(LOperator);
			try
			{
				LOperator.Operands.Add(new Schema.Operand(LOperator, "AValue", AScalarType, Modifier.Const));
				LOperator.ReturnDataType = APlan.DataTypes.SystemBoolean;
				LOperator.Owner = AScalarType.Owner;
				LOperator.Library = AScalarType.Library;
				
				APlan.AttachDependency(APlan.DataTypes.SystemBoolean);
				APlan.AttachDependency(AScalarType);

				APlan.Symbols.Push(new Symbol("AValue", AScalarType));
				try
				{
					APlan.Symbols.Push(new Symbol(Keywords.Result, LOperator.ReturnDataType));
					try
					{
						PlanNode LPlanNode = Compiler.EmitBinaryNode(APlan, new StackReferenceNode("AValue", AScalarType, 1), Instructions.Equal, AValueNode);
						LOperator.Block.BlockNode = new AssignmentNode(new StackReferenceNode(Keywords.Result, APlan.DataTypes.SystemBoolean, 0), LPlanNode);
						LOperator.Block.BlockNode.Line = 1;
						LOperator.Block.BlockNode.LinePos = 1;
						LOperator.Block.BlockNode = OptimizeNode(APlan, LOperator.Block.BlockNode);
						LOperator.Block.BlockNode = BindNode(APlan, LOperator.Block.BlockNode);
						return LOperator;
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static void CompileComparisonOperator(Plan APlan, Schema.ScalarType AScalarType)
		{
			// Builds an equality operator for the type
			// if applicable, builds a comparison operator for the type
			// If the type is simple
				// Build an equality operator based on the equality operator for the scalar simple property of the system representation
				// Attempt to build a comparison operator based on the comparison operator for the scalar simple property of the system representation
			// Otherwise
				// Build an equality operator using the CompoundScalarEqualNode
			if (!AScalarType.IsCompound)
			{
				Schema.ScalarType LComponentType = (Schema.ScalarType)FindSystemRepresentation(AScalarType).Properties[0].DataType;
				PlanNode[] LArguments = new PlanNode[]{new ValueNode(LComponentType, null), new ValueNode(LComponentType, null)};

				PlanNode LPlanNode = Compiler.EmitCallNode(APlan, Instructions.Equal, LArguments, false, true);
				if (LPlanNode != null)
				{
					Schema.Operator LComponentOperator = ((InstructionNodeBase)LPlanNode).Operator;
					Schema.Operator LOperator = new Schema.Operator(Schema.Object.Qualify(Instructions.Equal, AScalarType.Library.Name));
					LOperator.IsGenerated = true;
					LOperator.Generator = AScalarType;
					LOperator.IsBuiltin = true;
					LOperator.MetaData = new D4.MetaData();
					APlan.PushCreationObject(LOperator);
					try
					{
						LOperator.Operands.Add(new Schema.Operand(LOperator, "ALeftValue", AScalarType, Modifier.Const));
						LOperator.Operands.Add(new Schema.Operand(LOperator, "ARightValue", AScalarType, Modifier.Const));
						LOperator.ReturnDataType = APlan.DataTypes.SystemBoolean;
						LOperator.Owner = AScalarType.Owner;
						LOperator.Library = AScalarType.Library;

						APlan.AttachDependency(APlan.DataTypes.SystemBoolean);
						APlan.AttachDependency(AScalarType);
						
						if (LComponentOperator.Block.ClassDefinition != null)
							LOperator.Block.ClassDefinition = (ClassDefinition)LComponentOperator.Block.ClassDefinition;
						else
							LOperator.Block.BlockNode = LComponentOperator.Block.BlockNode;
						APlan.PlanCatalog.Add(LOperator);
						APlan.Catalog.OperatorResolutionCache.Clear(LOperator.OperatorName);
						AScalarType.EqualityOperator = LOperator;
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}

				LPlanNode = Compiler.EmitCallNode(APlan, Instructions.Compare, LArguments, false, true);
				if (LPlanNode != null)
				{
					Schema.Operator LComponentOperator = ((InstructionNodeBase)LPlanNode).Operator;
					Schema.Operator LOperator = new Schema.Operator(Schema.Object.Qualify(Instructions.Compare, AScalarType.Library.Name));
					LOperator.IsGenerated = true;
					LOperator.Generator = AScalarType;
					LOperator.IsBuiltin = true;
					LOperator.MetaData = new D4.MetaData();
					APlan.PushCreationObject(LOperator);
					try
					{
						LOperator.Operands.Add(new Schema.Operand(LOperator, "ALeftValue", AScalarType, Modifier.Const));
						LOperator.Operands.Add(new Schema.Operand(LOperator, "ARightValue", AScalarType, Modifier.Const));
						LOperator.ReturnDataType = APlan.DataTypes.SystemInteger;
						LOperator.Owner = AScalarType.Owner;
						LOperator.Library = AScalarType.Library;

						APlan.AttachDependency(APlan.DataTypes.SystemInteger);
						APlan.AttachDependency(AScalarType);
						
						if (LComponentOperator.Block.ClassDefinition != null)
							LOperator.Block.ClassDefinition = (ClassDefinition)LComponentOperator.Block.ClassDefinition;
						else
							LOperator.Block.BlockNode = LComponentOperator.Block.BlockNode;
						APlan.PlanCatalog.Add(LOperator);
						APlan.Catalog.OperatorResolutionCache.Clear(LOperator.OperatorName);
						AScalarType.ComparisonOperator = LOperator;
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}
			}
			else
			{
				Schema.Operator LOperator = new Schema.Operator(Schema.Object.Qualify(Instructions.Equal, AScalarType.Library.Name));
				LOperator.IsGenerated = true;
				LOperator.Generator = AScalarType;
				LOperator.IsBuiltin = true;
				LOperator.MetaData = new D4.MetaData();
				APlan.PushCreationObject(LOperator);
				try
				{
					LOperator.Operands.Add(new Schema.Operand(LOperator, "ALeftValue", AScalarType, Modifier.Const));
					LOperator.Operands.Add(new Schema.Operand(LOperator, "ARightValue", AScalarType, Modifier.Const));
					LOperator.ReturnDataType = APlan.DataTypes.SystemBoolean;
					LOperator.Owner = AScalarType.Owner;
					LOperator.Library = AScalarType.Library;

					APlan.AttachDependency(APlan.DataTypes.SystemBoolean);
					APlan.AttachDependency(AScalarType);
					
					LOperator.Block.ClassDefinition = new ClassDefinition("System.CompoundScalarEqualNode");
					APlan.PlanCatalog.Add(LOperator);
					APlan.Catalog.OperatorResolutionCache.Clear(LOperator.OperatorName);
					AScalarType.EqualityOperator = LOperator;
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
		}

		#if USETYPEINHERITANCE		
		protected static void CompileCastOperators(Plan APlan, Schema.ScalarType AScalarType, Schema.Objects AOperators)
		{
			// for each parent type using the same conveyor as this type, compile a cast operator of the form if one is not already present
			//	operator <type name>(<parent type name>) : <type name> class "ScalarSelectorNode"
			Schema.Operator LOperator;
			foreach (Schema.ScalarType LParentType in AScalarType.ParentTypes)
			{
				if ((LParentType.ClassDefinition != null) && (LParentType.ClassDefinition.Equals(AScalarType.ClassDefinition)))
				{
					LOperator = new Schema.Operator(AScalarType.Name);
					LOperator.IsGenerated = true;
					LOperator.ReturnDataType = AScalarType;
					LOperator.Operands.Add(new Schema.Operand(LOperator, CAccessorValueParameterName, LParentType, Modifier.Const));
					LOperator.Owner = AScalarType.Owner;
					LOperator.Library = AScalarType.Library;
					LOperator.Block.ClassDefinition = DefaultSelector();
					if (!APlan.Catalog.Contains(LOperator) && !APlan.PlanCatalog.Contains(LOperator))
					{
						APlan.PlanCatalog.Add(LOperator);
						APlan.Catalog.OperatorResolutionCache.Clear(LOperator.OperatorName);
						AOperators.Add(LOperator);
						AScalarType.ExplicitCastOperators.Add(LOperator);
					}
				}
			}
		}
		#endif
		
		public static Schema.TableVarColumnConstraint CompileTableVarColumnConstraint(Plan APlan, Schema.TableVar ATableVar, Schema.TableVarColumn AColumn, ConstraintDefinition AConstraintDefinition)
		{
			Schema.TableVarColumnConstraint LConstraint = 
				new Schema.TableVarColumnConstraint
				(
					Schema.Object.GetObjectID(AConstraintDefinition.MetaData),
					AConstraintDefinition.ConstraintName
				);
			LConstraint.ConstraintType = Schema.ConstraintType.Column;
			LConstraint.Library = ATableVar.Library == null ? null : APlan.CurrentLibrary;
			CompileScalarConstraint(APlan, LConstraint, AColumn.DataType, AConstraintDefinition);
			LConstraint.IsGenerated = LConstraint.IsGenerated || ATableVar.IsGenerated;
			return LConstraint;
		}
		
		public static Schema.ScalarTypeConstraint CompileScalarTypeConstraint(Plan APlan, Schema.ScalarType AScalarType, ConstraintDefinition AConstraintDefinition)
		{
			Schema.ScalarTypeConstraint LConstraint = 
				new Schema.ScalarTypeConstraint
				(
					Schema.Object.GetObjectID(AConstraintDefinition.MetaData),
					AConstraintDefinition.ConstraintName
				);
			LConstraint.ConstraintType = Schema.ConstraintType.ScalarType;
			LConstraint.Library = AScalarType.Library == null ? null : APlan.CurrentLibrary;
			CompileScalarConstraint(APlan, LConstraint, AScalarType, AConstraintDefinition);
			return LConstraint;
		}
		
		private static Schema.Constraint CompileScalarConstraint(Plan APlan, Schema.SimpleConstraint AConstraint, Schema.IDataType ADataType, ConstraintDefinition AConstraintDefinition)
		{
			AConstraint.IsGenerated = AConstraintDefinition.IsGenerated;
			AConstraint.MergeMetaData(AConstraintDefinition.MetaData);
			AConstraint.LoadIsGenerated();
			AConstraint.LoadGeneratorID();
			AConstraint.Enforced = GetEnforced(APlan, AConstraint.MetaData);
			APlan.PushCreationObject(AConstraint);
			try
			{
				APlan.Symbols.Push(new Symbol(Keywords.Value, ADataType));
				try
				{
					AConstraint.Node = CompileBooleanExpression(APlan, AConstraintDefinition.Expression);
					if (!(AConstraint.Node.IsFunctional && AConstraint.Node.IsDeterministic))
						throw new CompilerException(CompilerException.Codes.InvalidConstraintExpression, AConstraintDefinition.Expression);
					AConstraint.Node = OptimizeNode(APlan, AConstraint.Node);
					AConstraint.Node = BindNode(APlan, AConstraint.Node);
						
					AConstraint.DetermineRemotable(APlan.CatalogDeviceSession);
					if (!AConstraint.IsRemotable)
						throw new CompilerException(CompilerException.Codes.NonRemotableConstraintExpression, AConstraintDefinition.Expression);
						
					string LCustomMessage = AConstraint.GetCustomMessage(Schema.Transition.Insert);
					if (!String.IsNullOrEmpty(LCustomMessage))
					{
						try
						{
							PlanNode LViolationMessageNode = CompileTypedExpression(APlan, new D4.Parser().ParseExpression(LCustomMessage), APlan.DataTypes.SystemString);
							LViolationMessageNode = OptimizeNode(APlan, LViolationMessageNode);
							LViolationMessageNode = BindNode(APlan, LViolationMessageNode);
							AConstraint.ViolationMessageNode = LViolationMessageNode;
						}
						catch (Exception LException)
						{
							throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, AConstraintDefinition, LException, AConstraint.Name);
						}
					}
					
					AConstraint.DetermineRemotable(APlan.CatalogDeviceSession);
					if (!AConstraint.IsRemotable)
						throw new CompilerException(CompilerException.Codes.NonRemotableCustomConstraintMessage, AConstraintDefinition);
						
					return AConstraint;
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.Special CompileSpecial(Plan APlan, Schema.ScalarType AScalarType, SpecialDefinition ASpecialDefinition)
		{
			Schema.Special LSpecial = new Schema.Special(Schema.Object.GetObjectID(ASpecialDefinition.MetaData), ASpecialDefinition.Name);
			LSpecial.Library = APlan.CurrentLibrary;
			LSpecial.MergeMetaData(ASpecialDefinition.MetaData);
			APlan.PushCreationObject(LSpecial);
			try
			{
				LSpecial.IsGenerated = ASpecialDefinition.IsGenerated;
				LSpecial.LoadIsGenerated();
				LSpecial.LoadGeneratorID();
				LSpecial.ValueNode = CompileTypedExpression(APlan, ASpecialDefinition.Value, AScalarType);
				if (!(LSpecial.ValueNode.IsFunctional && LSpecial.ValueNode.IsDeterministic))
					throw new CompilerException(CompilerException.Codes.InvalidSpecialExpression, ASpecialDefinition.Value);
				LSpecial.ValueNode = OptimizeNode(APlan, LSpecial.ValueNode);
				LSpecial.ValueNode = BindNode(APlan, LSpecial.ValueNode);
				if (!APlan.InLoadingContext())
				{
					LSpecial.Selector = CompileSpecialSelector(APlan, AScalarType, LSpecial, ASpecialDefinition.Name, LSpecial.ValueNode);
					if (LSpecial.HasDependencies())
						LSpecial.Selector.AddDependencies(LSpecial.Dependencies);
					LSpecial.Selector.DetermineRemotable(APlan.CatalogDeviceSession);
					APlan.PlanCatalog.Add(LSpecial.Selector);
					APlan.AttachDependencies(LSpecial.Selector.Dependencies);
					APlan.Catalog.OperatorResolutionCache.Clear(LSpecial.Selector.OperatorName);
				}
				else
				{
					LSpecial.LoadSelectorID();
					LSpecial.LoadDependencies(APlan.CatalogDeviceSession);
				}
				
				if (!APlan.InLoadingContext())
				{
					LSpecial.Comparer = CompileSpecialComparer(APlan, AScalarType, LSpecial, ASpecialDefinition.Name, LSpecial.ValueNode);
					if (LSpecial.HasDependencies())
						LSpecial.Comparer.AddDependencies(LSpecial.Dependencies);
					LSpecial.Comparer.DetermineRemotable(APlan.CatalogDeviceSession);
					APlan.PlanCatalog.Add(LSpecial.Comparer);
					APlan.AttachDependencies(LSpecial.Comparer.Dependencies);
					APlan.Catalog.OperatorResolutionCache.Clear(LSpecial.Comparer.OperatorName);
				}
				else
				{
					LSpecial.LoadComparerID();
				}

				LSpecial.RemoveDependency(AScalarType);
				LSpecial.DetermineRemotable(APlan.CatalogDeviceSession);
				
				return LSpecial;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}

		// The like representation for a type is the representataion with a single
		// property of the like type. Clearly, if a type is not defined to be like
		// another type, then it has no like representation.
		public static Schema.Representation FindLikeRepresentation(Schema.ScalarType AScalarType)
		{
			// If this type is like another type, find the representation with a single component of the like type
			if (AScalarType.LikeType != null)
				foreach (Schema.Representation LRepresentation in AScalarType.Representations)
					if ((LRepresentation.Properties.Count == 1) && LRepresentation.Properties[0].DataType.Equals(AScalarType.LikeType))	
						return LRepresentation;
			return null;
		}

		// The system representation for a type is the representation for which the 
		// selector is system-provided. Clearly, if the conveyor for the type is not
		// system-provided, then it has no system representation.		
		public static Schema.Representation FindSystemRepresentation(Schema.ScalarType AScalarType)
		{
			// If the native representation for this type is system-provided, find the system-provided representation of this type
			if (AScalarType.IsDefaultConveyor)
				foreach (Schema.Representation LRepresentation in AScalarType.Representations)
					if (LRepresentation.IsDefaultSelector)
						return LRepresentation;
			return null;
		}
		
		public static Schema.Representation FindDefaultRepresentation(Schema.ScalarType AScalarType)
		{
			// The default representation is the representation with the same name as the scalar type
			foreach (Schema.Representation LRepresentation in AScalarType.Representations)
				if (String.Compare(LRepresentation.Name, Schema.Object.Unqualify(AScalarType.Name)) == 0)
					return LRepresentation;
			return null;
		}
		
		//	ScalarTypes and metadata inheritance ->
		//		A scalar type inherits meta data from each of its parent scalar types, in order of appearance in the definition list.
		//		Inherited metadata may be changed at the descendent level, in which case it becomes owned (a copy is made
		//		rather than the reference.)
		//		
		//	Catalog locking for a create type statement ->
		//		A shared lock is acquired on each parent type.
		//		A shared lock is acquired on the dependencies for each generated operator.
		//		A shared lock is acquired on the dependencies for the default and each constraint, if any.
		public static PlanNode CompileCreateScalarTypeStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			APlan.CheckRight(Schema.RightNames.CreateType);

			CreateScalarTypeStatement LStatement = (CreateScalarTypeStatement)AStatement;
			CreateScalarTypeNode LNode = new CreateScalarTypeNode();
			LNode.SetLineInfo(LStatement.LineInfo);
			BlockNode LBlockNode = new BlockNode();
			LBlockNode.SetLineInfo(LStatement.LineInfo);
			LBlockNode.Nodes.Add(LNode);

			string LScalarTypeName = Schema.Object.Qualify(LStatement.ScalarTypeName, APlan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(APlan, AStatement, LScalarTypeName);
			
			APlan.Symbols.PushWindow(0); // make sure the create scalar type statement is evaluated in a private context
			try
			{
				// Generate the Schema.IScalarType for this scalar type
				LNode.ScalarType = new Schema.ScalarType(Schema.Object.GetObjectID(LStatement.MetaData), LScalarTypeName);
				Schema.Objects LOperators = new Schema.Objects();
				LNode.ScalarType.Owner = APlan.User;
				LNode.ScalarType.Library = APlan.CurrentLibrary;
				if (String.Compare(LScalarTypeName, Schema.DataTypes.CSystemScalar) == 0)
					LNode.ScalarType.IsGeneric = true;
				APlan.PlanCatalog.Add(LNode.ScalarType);
				try
				{
					APlan.PushCreationObject(LNode.ScalarType);
					try
					{
						#if USETYPEINHERITANCE
						if (LStatement.ParentScalarTypes.Count > 0)
							foreach (ScalarTypeNameDefinition LParentScalarType in LStatement.ParentScalarTypes)
							{
								Schema.ScalarType LParentType = (Schema.ScalarType)Compiler.CompileScalarTypeSpecifier(APlan, new ScalarTypeSpecifier(LParentScalarType.ScalarTypeName));
								LNode.ScalarType.ParentTypes.Add(LParentType);
								LNode.ScalarType.InheritMetaData(LParentType.MetaData);
								APlan.AttachDependency(LParentType);
							}
						else
						{
							// Check first to see if the catalog contains the alpha data type, if it does not, the system is starting up and we are creating the alpha scalar type, so it is allowed to be parentless.
							if (APlan.Catalog.Contains(Schema.DataTypes.CSystemScalar))
							{
								Schema.ScalarType LParentType = APlan.DataTypes.SystemScalar;
								LNode.ScalarType.ParentTypes.Add(LParentType);
								LNode.ScalarType.InheritMetaData(LParentType.MetaData);
								APlan.AttachDependency(LParentType);
							}
						}
						#endif

						if (LStatement.LikeScalarTypeName != String.Empty)
						{
							LNode.ScalarType.LikeType = (Schema.ScalarType)CompileScalarTypeSpecifier(APlan, new ScalarTypeSpecifier(LStatement.LikeScalarTypeName));

							if (!APlan.InLoadingContext())
							{
								Schema.Representation LLikeTypeLikeRepresentation = FindLikeRepresentation(LNode.ScalarType.LikeType);
								RepresentationDefinition LRepresentationDefinition;
								PropertyDefinition LPropertyDefinition;
								bool LHasLikeRepresentation = false;
								int LInsertIndex = 0;
								for (int LIndex = 0; LIndex < LNode.ScalarType.LikeType.Representations.Count; LIndex++)
								{
									Schema.Representation LRepresentation = LNode.ScalarType.LikeType.Representations[LIndex];
									if (LRepresentation != LLikeTypeLikeRepresentation)
									{
										// if this representation will be the like representation for the new type, it will also be the system-provided representation so omit the class definitions for the selector and accessors
										bool LIsLikeRepresentation = (LRepresentation.Properties.Count == 1) && LRepresentation.Properties[0].DataType.Equals(LNode.ScalarType.LikeType);
										
										// Only the like representation and native accessor representations should be liked
										if (LIsLikeRepresentation || LRepresentation.IsNativeAccessorRepresentation(false)) 
										{
											LRepresentationDefinition = new RepresentationDefinition(String.Compare(LRepresentation.Name, Schema.Object.Unqualify(LNode.ScalarType.LikeType.Name)) == 0 ? Schema.Object.Unqualify(LNode.ScalarType.Name) : LRepresentation.Name);
											LRepresentationDefinition.IsGenerated = true;
											if (!LStatement.Representations.Contains(LRepresentationDefinition.RepresentationName))
											{
												if (LIsLikeRepresentation)
													LHasLikeRepresentation = true;
												if (!LIsLikeRepresentation)
													LRepresentationDefinition.SelectorAccessorBlock = LRepresentation.Selector.Block.EmitAccessorBlock(EmitMode.ForCopy);
												foreach (Schema.Property LProperty in LRepresentation.Properties)
												{
													LPropertyDefinition = new PropertyDefinition(LProperty.Name, LProperty.DataType.EmitSpecifier(EmitMode.ForCopy));
													if (!LIsLikeRepresentation)
													{
														LPropertyDefinition.ReadAccessorBlock = LProperty.ReadAccessor.Block.EmitAccessorBlock(EmitMode.ForCopy);
														LPropertyDefinition.WriteAccessorBlock = LProperty.WriteAccessor.Block.EmitAccessorBlock(EmitMode.ForCopy);
													}
													LRepresentationDefinition.Properties.Add(LPropertyDefinition);
												}
												LStatement.Representations.Insert(LInsertIndex, LRepresentationDefinition);
												LInsertIndex++;
											}
										}
									}
								}
								
								if (!LHasLikeRepresentation)
								{
									string LRepresentationName = Schema.Object.Unqualify(LNode.ScalarType.Name);
									if (LStatement.Representations.Contains(LRepresentationName))
										LRepresentationName = String.Format("As{0}", Schema.Object.Unqualify(LNode.ScalarType.LikeType.Name)); 
									LRepresentationDefinition = new RepresentationDefinition(LRepresentationName);
									LRepresentationDefinition.IsGenerated = true;
									if (!LStatement.Representations.Contains(LRepresentationDefinition.RepresentationName))
									{
										LRepresentationDefinition.Properties.Add(new PropertyDefinition(LRepresentationDefinition.RepresentationName, new ScalarTypeSpecifier(LNode.ScalarType.LikeType.Name)));
										LStatement.Representations.Insert(0, LRepresentationDefinition);
									}
								}
								
								// Default
								if ((LNode.ScalarType.LikeType.Default != null) && (LStatement.Default == null))
								{
									LStatement.Default = LNode.ScalarType.LikeType.Default.EmitDefinition(EmitMode.ForCopy);
									if (LNode.ScalarType.LikeType.Default.HasDependencies())
										APlan.AttachDependencies(LNode.ScalarType.LikeType.Default.Dependencies);
									LStatement.Default.IsGenerated = true;
								}
									
								// Constraints
								foreach (Schema.ScalarTypeConstraint LConstraint in LNode.ScalarType.LikeType.Constraints)
								{
									if (!LStatement.Constraints.Contains(LConstraint.Name))
									{
										ConstraintDefinition LConstraintDefinition = LConstraint.EmitDefinition(EmitMode.ForCopy);
										if (LConstraint.HasDependencies())
											APlan.AttachDependencies(LConstraint.Dependencies);
										LConstraintDefinition.IsGenerated = true;
										LStatement.Constraints.Add(LConstraintDefinition);
									}
								}
								
								// Specials
								foreach (Schema.Special LSpecial in LNode.ScalarType.LikeType.Specials)
								{
									if (!LStatement.Specials.Contains(LSpecial.Name))
									{
										SpecialDefinition LSpecialDefinition = (SpecialDefinition)LSpecial.EmitStatement(EmitMode.ForCopy);
										if (LSpecial.HasDependencies())
											APlan.AttachDependencies(LSpecial.Dependencies);
										LSpecialDefinition.IsGenerated = true;
										LStatement.Specials.Add(LSpecialDefinition);
									}
								}
								
								// Tags
								LNode.ScalarType.MergeMetaData(LNode.ScalarType.LikeType.MetaData);
							}
						}
						
						LNode.ScalarType.MergeMetaData(LStatement.MetaData);
						
						if (LStatement.ClassDefinition != null)
						{
							if (!LNode.ScalarType.IsDefaultConveyor)
								APlan.CheckRight(Schema.RightNames.HostImplementation);
							APlan.CheckClassDependency(LNode.ScalarType.Library, LStatement.ClassDefinition);
							Type LType = APlan.Catalog.ClassLoader.CreateType(LStatement.ClassDefinition);
							if (!LType.IsSubclassOf(typeof(Conveyor)))
								throw new CompilerException(CompilerException.Codes.ConveyorClassExpected, LStatement.ClassDefinition, LType.AssemblyQualifiedName);
						}

						LNode.ScalarType.ClassDefinition = LStatement.ClassDefinition;
						
						Schema.Operator LOperator;

						foreach (RepresentationDefinition LRepresentationDefinition in LStatement.Representations)
							if (!LRepresentationDefinition.HasD4ImplementedComponents())
								CompileRepresentation(APlan, LNode.ScalarType, LOperators, LRepresentationDefinition);
							
						#if USETYPEINHERITANCE
						// If this scalar type has no representations defined, but it is based on a single branch inheritance hierarchy leading to a system type, build a default representation
						if (LStatement.Representations.Count == 0)
							CompileDefaultRepresentation(APlan, LNode.ScalarType, LOperators);
						#endif
						
						#if !NATIVEROW
						LNode.ScalarType.StaticByteSize = CellValueStream.MinimumStaticByteSize;
						#endif
						
						if (LNode.ScalarType.IsDefaultConveyor)
						{
							// Compile the default equality and comparison operator for the type
							if (!APlan.InLoadingContext())
							{
								CompileComparisonOperator(APlan, LNode.ScalarType);
								if (LNode.ScalarType.EqualityOperator != null)
									LOperators.Add(LNode.ScalarType.EqualityOperator);
								if (LNode.ScalarType.ComparisonOperator != null)
									LOperators.Add(LNode.ScalarType.ComparisonOperator);
							}
							else
							{
								// Load the set of dependencies to ensure they are reported correctly in the cache
								LNode.ScalarType.LoadDependencies(APlan.CatalogDeviceSession);
							}
						}
						else 
						{
							// If the native representation for this scalar type is not system-provided then a conveyor must be supplied
							if (!LNode.ScalarType.IsGeneric && LNode.ScalarType.ClassDefinition == null)
								throw new CompilerException(CompilerException.Codes.ConveyorRequired, AStatement, LNode.ScalarType.Name);
						}
						
						if (APlan.InLoadingContext())
						{
							LNode.ScalarType.LoadEqualityOperatorID();
							LNode.ScalarType.LoadComparisonOperatorID();
							LNode.ScalarType.LoadSortID();
							LNode.ScalarType.LoadUniqueSortID();
						}

						#if !NATIVEROW
						// If the meta data contains a definition for static byte size, use that					
						if (LNode.ScalarType.MetaData.Tags.Contains(TagNames.CStaticByteSize))
							LNode.ScalarType.StaticByteSize = Convert.ToInt32(LNode.ScalarType.MetaData.Tags[TagNames.CStaticByteSize].Value);
						#endif

						#if USETYPEINHERITANCE
						// Compile Cast Operators for each immediate ParentType which uses the same conveyor
						CompileCastOperators(APlan, LNode.ScalarType, LOperators);
						#endif
						
						// Create implicit conversions
						Schema.Conversion LNarrowingConversion = null;
						Schema.Conversion LWideningConversion = null;
						if ((!APlan.InLoadingContext()) && (LStatement.LikeScalarTypeName != String.Empty))
						{
							// create conversion LikeScalarTypeName to ScalarTypeName using ScalarTypeName.LikeRepresentation.Selector narrowing
							// create conversion ScalarTypeName to LikeScalarTypeName using ScalarTypeName.LikeRepresentation.ReadAccessor widening
							
							Schema.Representation LRepresentation = FindLikeRepresentation(LNode.ScalarType);
							string LConversionName = Schema.Object.Qualify(Schema.Object.MangleQualifiers(String.Format("Conversion_{0}_{1}", LNode.ScalarType.LikeType.Name, LNode.ScalarType.Name)), APlan.CurrentLibrary.Name);
							CheckValidCatalogObjectName(APlan, AStatement, LConversionName);
							LNarrowingConversion = new Schema.Conversion(Schema.Object.GetNextObjectID(), LConversionName, LNode.ScalarType.LikeType, LNode.ScalarType, LRepresentation.Selector, true);
							LNarrowingConversion.IsGenerated = true;
							LNarrowingConversion.Generator = LNode.ScalarType;
							LNarrowingConversion.Owner = APlan.User;
							LNarrowingConversion.Library = APlan.CurrentLibrary;
							LNarrowingConversion.AddDependency(LNode.ScalarType.LikeType);
							LNarrowingConversion.AddDependency(LNode.ScalarType);
							LNarrowingConversion.AddDependency(LRepresentation.Selector);
							if (!LNode.ScalarType.LikeType.ImplicitConversions.Contains(LNarrowingConversion))
								LNode.ScalarType.LikeType.ImplicitConversions.Add(LNarrowingConversion);

							LConversionName = Schema.Object.Qualify(Schema.Object.MangleQualifiers(String.Format("Conversion_{0}_{1}", LNode.ScalarType.Name, LNode.ScalarType.LikeType.Name)), APlan.CurrentLibrary.Name);
							CheckValidCatalogObjectName(APlan, AStatement, LConversionName);							
							LWideningConversion = new Schema.Conversion(Schema.Object.GetNextObjectID(), LConversionName, LNode.ScalarType, LNode.ScalarType.LikeType, LRepresentation.Properties[0].ReadAccessor, false);
							LWideningConversion.IsGenerated = true;
							LWideningConversion.Generator = LNode.ScalarType;
							LWideningConversion.Owner = APlan.User;
							LWideningConversion.Library = APlan.CurrentLibrary;
							LWideningConversion.AddDependency(LNode.ScalarType);
							LWideningConversion.AddDependency(LNode.ScalarType.LikeType);
							LWideningConversion.AddDependency(LRepresentation.Properties[0].ReadAccessor);
							if (!LNode.ScalarType.ImplicitConversions.Contains(LWideningConversion))
								LNode.ScalarType.ImplicitConversions.Add(LWideningConversion);

							APlan.PlanCatalog.Add(LNarrowingConversion);
							APlan.PlanCatalog.Add(LWideningConversion);
							APlan.Catalog.ConversionPathCache.Clear(LNode.ScalarType.LikeType);
						}
						APlan.Catalog.OperatorResolutionCache.Clear(LNode.ScalarType, LNode.ScalarType);
						try
						{
							// Host-Implemented representations
							foreach (RepresentationDefinition LRepresentationDefinition in LStatement.Representations)
								if (LRepresentationDefinition.HasD4ImplementedComponents())
									CompileRepresentation(APlan, LNode.ScalarType, LOperators, LRepresentationDefinition);

							// Constraints
							foreach (ConstraintDefinition LConstraint in LStatement.Constraints)
								LNode.ScalarType.Constraints.Add(CompileScalarTypeConstraint(APlan, LNode.ScalarType, LConstraint));

							// Compile Special Definitions
							Schema.Special LSpecial;
							foreach (SpecialDefinition LSpecialDefinition in LStatement.Specials)
							{
								LSpecial = CompileSpecial(APlan, LNode.ScalarType, LSpecialDefinition);
								LNode.ScalarType.Specials.Add(LSpecial);
								if (LSpecial.Selector != null)
									LOperators.Add(LSpecial.Selector);
								if (LSpecial.Comparer != null)
									LOperators.Add(LSpecial.Comparer);
							}
								
							if (APlan.Catalog.Contains(Schema.DataTypes.CSystemBoolean))
							{
								if (!APlan.InLoadingContext())
								{
									LOperator = CompileSpecialOperator(APlan, LNode.ScalarType);
									LNode.ScalarType.IsSpecialOperator = LOperator;
									APlan.PlanCatalog.Add(LOperator);
									APlan.Catalog.OperatorResolutionCache.Clear(LOperator.OperatorName);
									LOperators.Add(LOperator);
								}
								else
								{
									LNode.ScalarType.LoadIsSpecialOperatorID();
								}
							}
							
							// Default
							if (LStatement.Default != null)
								LNode.ScalarType.Default = CompileScalarTypeDefault(APlan, LNode.ScalarType, LStatement.Default);

							// TODO: Verify that the specials and default satisfy the constraints
							
							for (int LIndex = 0; LIndex < LOperators.Count; LIndex++)
								LBlockNode.Nodes.Add(new CreateOperatorNode((Schema.Operator)LOperators[LIndex]));
								
							if (LNarrowingConversion != null)
								LBlockNode.Nodes.Add(new CreateConversionNode(LNarrowingConversion));
								
							if (LWideningConversion != null)
								LBlockNode.Nodes.Add(new CreateConversionNode(LWideningConversion));
							
							return LBlockNode;
						}
						finally
						{
							if (LNarrowingConversion != null) 
							{
								if ((LNode.ScalarType.LikeType != null) && (LNode.ScalarType.LikeType.ImplicitConversions.Contains(LNarrowingConversion)))
									LNode.ScalarType.LikeType.ImplicitConversions.Remove(LNarrowingConversion);
								if (APlan.PlanCatalog.Contains(LNarrowingConversion.Name))
									APlan.PlanCatalog.Remove(LNarrowingConversion);
							}
								
							if (LWideningConversion != null) 
							{
								if ((LNode.ScalarType.LikeType != null) && (LNode.ScalarType.ImplicitConversions.Contains(LWideningConversion)))
									LNode.ScalarType.ImplicitConversions.Remove(LWideningConversion);
								if (APlan.PlanCatalog.Contains(LWideningConversion.Name))
									APlan.PlanCatalog.Remove(LWideningConversion);
							}
						}
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}
				catch
				{
					APlan.PlanCatalog.SafeRemove(LNode.ScalarType);
					foreach (Schema.Operator LRemoveOperator in LOperators)
						APlan.PlanCatalog.SafeRemove(LRemoveOperator);
					throw;
				}
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static Schema.Sort CompileSortDefinition(Plan APlan, Schema.IDataType ADataType, SortDefinition ASortDefinition, bool AIsScalarSort)
		{
			int LObjectID = Schema.Object.GetObjectID(ASortDefinition.MetaData);
			Schema.Sort LSort = new Schema.Sort(LObjectID, String.Format("{0}Sort{1}", ADataType.Name, AIsScalarSort ? String.Empty : LObjectID.ToString()), ADataType);
			LSort.IsGenerated = true;
			LSort.Owner = APlan.User;
			LSort.Library = APlan.CurrentLibrary;
			APlan.PlanCatalog.Add(LSort);
			try
			{
				APlan.PushCreationObject(LSort);
				try
				{
					string LLeftIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Left);
					string LRightIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Right);
					APlan.Symbols.Push(new Symbol(LLeftIdentifier, ADataType));
					try
					{
						APlan.Symbols.Push(new Symbol(LRightIdentifier, ADataType));
						try
						{
							PlanNode LNode = CompileExpression(APlan, ASortDefinition.Expression);
							if (!(LNode.DataType.Is(APlan.DataTypes.SystemInteger)))
								throw new CompilerException(CompilerException.Codes.IntegerExpressionExpected, ASortDefinition.Expression);
							if (!(LNode.IsFunctional && LNode.IsDeterministic))
								throw new CompilerException(CompilerException.Codes.InvalidCompareExpression, ASortDefinition.Expression);
							LNode = OptimizeNode(APlan, LNode);
							LNode = BindNode(APlan, LNode);
							LSort.CompareNode = LNode;
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
			finally
			{
				APlan.PlanCatalog.SafeRemove(LSort);
			}
			LSort.DetermineRemotable(APlan.CatalogDeviceSession);
			return LSort;
		}
		
		public static Schema.Sort CompileSortDefinition(Plan APlan, Schema.IDataType ADataType)
		{
			int LMessageIndex = APlan.Messages.Count;
			try
			{
				Schema.Sort LSort = new Schema.Sort(Schema.Object.GetNextObjectID(), String.Format("{0}UniqueSort", ADataType.Name), ADataType);
				LSort.Library = APlan.CurrentLibrary;
				LSort.Owner = APlan.User;
				LSort.IsGenerated = true;
				LSort.IsUnique = true;
				APlan.PlanCatalog.Add(LSort);
				try
				{
					APlan.PushCreationObject(LSort);
					try
					{
						string LLeftIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Left);
						string LRightIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Right);
						APlan.Symbols.Push(new Symbol(LLeftIdentifier, ADataType));
						try
						{
							APlan.Symbols.Push(new Symbol(LRightIdentifier, ADataType));
							try
							{
								PlanNode LNode = CompileExpression(APlan, new BinaryExpression(new IdentifierExpression(LLeftIdentifier), Instructions.Compare, new IdentifierExpression(LRightIdentifier)));
								if (!(LNode.DataType.Is(APlan.DataTypes.SystemInteger)))
									throw new CompilerException(CompilerException.Codes.IntegerExpressionExpected, APlan.CurrentStatement());
								if (!(LNode.IsFunctional && LNode.IsDeterministic))
									throw new CompilerException(CompilerException.Codes.InvalidCompareExpression, APlan.CurrentStatement());
								LNode = BindNode(APlan, LNode);
								LSort.CompareNode = LNode;
							}
							finally
							{
								APlan.Symbols.Pop();
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}
				finally
				{
					APlan.PlanCatalog.SafeRemove(LSort);
				}
				LSort.DetermineRemotable(APlan.CatalogDeviceSession);
				return LSort;
			}
			catch (Exception LException)
			{
				if ((LException is CompilerException) && (((CompilerException)LException).Code == (int)CompilerException.Codes.NonFatalErrors))
				{
					APlan.Messages.Insert(LMessageIndex, new CompilerException(CompilerException.Codes.UnableToConstructSort, APlan.CurrentStatement(), ADataType.Name));
					throw LException;
				}
				else
					throw new CompilerException(CompilerException.Codes.UnableToConstructSort, APlan.CurrentStatement(), LException, ADataType.Name);
			}
		}
		
		public static Schema.IScalarType CompileScalarTypeSpecifier(Plan APlan, TypeSpecifier ATypeSpecifier)
		{
			Schema.IDataType LDataType = CompileTypeSpecifier(APlan, ATypeSpecifier);
			if (!(LDataType is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeExpected, ATypeSpecifier);
			return (Schema.IScalarType)LDataType;
		}
		
		public static Schema.IDataType CompileTypeSpecifier(Plan APlan, TypeSpecifier ATypeSpecifier)
		{
			if (ATypeSpecifier is GenericTypeSpecifier)
				return APlan.DataTypes.SystemGeneric;
			else if (ATypeSpecifier is ScalarTypeSpecifier)
			{
				if (ATypeSpecifier.IsGeneric)
					return APlan.DataTypes.SystemScalar;
				else if (Schema.Object.NamesEqual(((ScalarTypeSpecifier)ATypeSpecifier).ScalarTypeName, Schema.DataTypes.CSystemGeneric))
				{
					return APlan.DataTypes.SystemGeneric;
				}
				else
				{
					Schema.Object LObject = ResolveCatalogIdentifier(APlan, ((ScalarTypeSpecifier)ATypeSpecifier).ScalarTypeName);
					if (LObject is Schema.IScalarType)
					{
						APlan.AttachDependency(LObject);
						return (Schema.IScalarType)LObject;
					}
					else
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, ((ScalarTypeSpecifier)ATypeSpecifier).ScalarTypeName);
				}
			}
			else if (ATypeSpecifier is RowTypeSpecifier)
			{
				Schema.IRowType LRowType = new Schema.RowType();

				if (ATypeSpecifier.IsGeneric)
				{
					LRowType.IsGeneric = true;
					return LRowType;
				}

				Schema.IDataType LType;
				foreach (NamedTypeSpecifier LColumn in ((RowTypeSpecifier)ATypeSpecifier).Columns)
				{
					LType = CompileTypeSpecifier(APlan, LColumn.TypeSpecifier);
					LRowType.Columns.Add(new Schema.Column(LColumn.Identifier, LType));
				}

				return LRowType;
			}
			else if (ATypeSpecifier is TableTypeSpecifier)
			{
				Schema.ITableType LTableType = new Schema.TableType();

				if (ATypeSpecifier.IsGeneric)
				{
					LTableType.IsGeneric = true;
					return LTableType;
				}

				Schema.IDataType LType;
				foreach (NamedTypeSpecifier LColumn in ((TableTypeSpecifier)ATypeSpecifier).Columns)
				{
					LType = CompileTypeSpecifier(APlan, LColumn.TypeSpecifier);
					LTableType.Columns.Add(new Schema.Column(LColumn.Identifier, LType));
				}

				return LTableType;
			}
			else if (ATypeSpecifier is ListTypeSpecifier)
			{
				if (ATypeSpecifier.IsGeneric)
				{
					Schema.ListType LListType = new Schema.ListType(new Schema.GenericType());
					LListType.IsGeneric = true;
					return LListType;
				}
				else
					return new Schema.ListType(CompileTypeSpecifier(APlan, ((ListTypeSpecifier)ATypeSpecifier).TypeSpecifier));
			}
			else if (ATypeSpecifier is CursorTypeSpecifier)
			{
				if (ATypeSpecifier.IsGeneric)
				{
					Schema.CursorType LCursorType = new Schema.CursorType(new Schema.TableType());
					LCursorType.TableType.IsGeneric = true;
					LCursorType.IsGeneric = true;
					return LCursorType;
				}
				else
				{
					Schema.IDataType LType = CompileTypeSpecifier(APlan, ((CursorTypeSpecifier)ATypeSpecifier).TypeSpecifier);
					if (!(LType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableTypeExpected, ((CursorTypeSpecifier)ATypeSpecifier).TypeSpecifier);
					return new Schema.CursorType((Schema.ITableType)LType);
				}
			}
			else if (ATypeSpecifier is TypeOfTypeSpecifier)
			{
				// Push a dummy creation object to prevent dependencies on typeof expression sources
				Schema.BaseTableVar LDummy = new Schema.BaseTableVar("Dummy");
				LDummy.SessionObjectName = LDummy.Name; // This will allow the typeof expression to reference session specific objects
				LDummy.SessionID = APlan.SessionID;
				//LDummy.Library = APlan.CurrentLibrary;
				Schema.IDataType LDataType;
				APlan.PushCreationObject(LDummy);
				try
				{
					APlan.PushTypeOfContext();
					try
					{
						LDataType = CompileExpression(APlan, ((TypeOfTypeSpecifier)ATypeSpecifier).Expression).DataType;
					}
					finally
					{
						APlan.PopTypeOfContext();
					}
				}
				finally
				{
					APlan.PopCreationObject();
				}

				// Attach dependencies for the resolved data type
				AttachDataTypeDependencies(APlan, LDataType);
				return LDataType;					
			}
			else
				throw new CompilerException(CompilerException.Codes.UnknownTypeSpecifier, ATypeSpecifier, ATypeSpecifier.GetType().Name);
		}
		
		public static void AttachDataTypeDependencies(Plan APlan, Schema.IDataType ADataType)
		{
			if (ADataType is Schema.IScalarType)
			{
				APlan.AttachDependency((Schema.ScalarType)ADataType);
			}
			else if (ADataType is Schema.IRowType)
			{
				foreach (Schema.Column LColumn in ((Schema.IRowType)ADataType).Columns)
					AttachDataTypeDependencies(APlan, LColumn.DataType);
			}
			else if (ADataType is Schema.ITableType)
			{
				foreach (Schema.Column LColumn in ((Schema.ITableType)ADataType).Columns)
					AttachDataTypeDependencies(APlan, LColumn.DataType);
			}
			else if (ADataType is Schema.IListType)
			{
				AttachDataTypeDependencies(APlan, ((Schema.IListType)ADataType).ElementType);
			}
			else if (ADataType is Schema.ICursorType)
			{
				AttachDataTypeDependencies(APlan, ((Schema.ICursorType)ADataType).TableType);
			}
			else
				Error.Fail(@"Could not attach dependencies for data type ""{0}"".", ADataType.Name);
		}
		
		public static PlanNode CompileOperatorBlock(Plan APlan, Schema.Operator AOperator, Statement AStatement)
		{
			PlanNode LBlock;
			APlan.Symbols.PushWindow(0);
			try
			{
				Schema.Operand LOperand;
				for (int LIndex = 0; LIndex < AOperator.Operands.Count; LIndex++)
				{
					LOperand = AOperator.Operands[LIndex];
					APlan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));
				}
				
				int LStackDepth = AOperator.Operands.Count;
				
				if (AOperator.ReturnDataType != null)
				{
					APlan.Symbols.Push(new Symbol(Keywords.Result, AOperator.ReturnDataType));
					LStackDepth++;
				}
						
				LBlock = CompileStatement(APlan, AStatement);
				LBlock = OptimizeNode(APlan, LBlock);

				for (int LIndex = 0; LIndex < AOperator.Operands.Count; LIndex++)
					if (!APlan.Symbols[AOperator.Operands.Count - 1 - LIndex].IsModified && (AOperator.Operands[LIndex].Modifier == Modifier.In))
					{
						AOperator.Operands[LIndex].Modifier = Modifier.Const;
						AOperator.OperandsChanged();
					}
				
				// Dispose variables allocated within the block
				BlockNode LBlockNode = null;
				for (int LIndex = 0; LIndex < APlan.Symbols.Count - LStackDepth; LIndex++)
				{
					if (APlan.Symbols[LIndex].DataType.IsDisposable)
					{
						if (LBlockNode == null)
						{
							LBlockNode = new BlockNode();
							LBlockNode.IsBreakable = false;
							LBlockNode.Nodes.Add(LBlock);
						}

						LBlockNode.Nodes.Add(new DeallocateVariableNode(LIndex));
					}
				}
				
				if (LBlockNode != null)
					LBlock = LBlockNode;

				return LBlock;
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static PlanNode BindOperatorBlock(Plan APlan, Schema.Operator AOperator, PlanNode ABlock)
		{
			APlan.Symbols.PushWindow(0);
			try
			{
				Schema.Operand LOperand;
				for (int LIndex = 0; LIndex < AOperator.Operands.Count; LIndex++)
				{
					LOperand = AOperator.Operands[LIndex];
					APlan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));
				}
					
				if (AOperator.ReturnDataType != null)
					APlan.Symbols.Push(new Symbol(Keywords.Result, AOperator.ReturnDataType));
						
				return BindNode(APlan, ABlock);
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static void ProcessSourceContext(Plan APlan, CreateOperatorStatement AStatement, CreateOperatorNode LNode)
		{
			// SourceContext is the script that is currently being compiled
			// The Locator in the source context represents an offset into the script identified by the locator, not
			// the actual script contained in the SourceContext. Line numbers in AOperatorLineInfo will be relative 
			// to the actual script in SourceContext, not the locator.
			
			// Pull the debug locator from the DAE.Locator metadata tag, if present
			DebugLocator LLocator = Schema.Operator.GetLocator(LNode.CreateOperator.MetaData);
			if (LLocator != null)
				LNode.CreateOperator.Locator = LLocator;
			else
			{
				if (APlan.SourceContext != null)
				{
					// Determine the line offsets for the operator declaration
					LineInfo LLineInfo = new LineInfo();
					LLineInfo.Line = AStatement.Line;
					LLineInfo.LinePos = AStatement.LinePos;
					LLineInfo.EndLine = AStatement.Block.Line;
					LLineInfo.EndLinePos = AStatement.Block.LinePos;

					// Copy the text of the operator from the source context
					// Note that the text does not include the metadata for the operator, just the operator header and body.
					if (AStatement.Block.ClassDefinition == null)
					{
						LNode.CreateOperator.DeclarationText = SourceUtility.CopySection(APlan.SourceContext.Script, LLineInfo);
						LNode.CreateOperator.BodyText = SourceUtility.CopySection(APlan.SourceContext.Script, AStatement.Block.LineInfo);
					}
					
					// Set the debug locator to the combination of the source context debug locator and the operator line info
					if (APlan.SourceContext.Locator != null)
					{
						LNode.CreateOperator.Locator = 
							new DebugLocator
							(
								APlan.SourceContext.Locator.Locator, 
								APlan.SourceContext.Locator.Line + LLineInfo.Line, 
								LLineInfo.LinePos
							);
					}
					else
					{
						// If there is no locator, this is either dynamic or ad-hoc execution, and the locator should be a negative offset
						// so the operator text can be returned as the debug context.
						LNode.CreateOperator.Locator =
							new DebugLocator
							(
								DebugLocator.OperatorLocator(LNode.CreateOperator.DisplayName),
								-(LLineInfo.Line - 1),
								LLineInfo.LinePos
							);
					}
				}
			}
		}
		
		public static PlanNode CompileCreateOperatorStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateOperatorStatement LStatement = (CreateOperatorStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateOperator);
			CreateOperatorNode LNode = new CreateOperatorNode();
			string LOperatorName = Schema.Object.Qualify(LStatement.OperatorName, APlan.CurrentLibrary.Name);
			string LSessionOperatorName = null;
			string LSourceOperatorName = null;
			bool LAddedSessionObject = false;
			if (LStatement.IsSession)
			{
				LSessionOperatorName = LOperatorName;
				int LIndex = APlan.PlanSessionOperators.IndexOfName(LOperatorName);
				if (LIndex >= 0)
					LOperatorName = ((Schema.SessionObject)APlan.PlanSessionOperators[LIndex]).GlobalName;
				else
				{
					LIndex = APlan.SessionOperators.IndexOfName(LOperatorName);
					if (LIndex >= 0)
						LOperatorName = ((Schema.SessionObject)APlan.SessionOperators[LIndex]).GlobalName;
					else
					{
						if (APlan.IsRepository)
							LOperatorName = MetaData.GetTag(LStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
						else
							LOperatorName = Schema.Object.NameFromGuid(Guid.NewGuid());
						APlan.PlanSessionOperators.Add(new Schema.SessionObject(LSessionOperatorName, LOperatorName));
						LAddedSessionObject = true;
					}
				}
			}
			else if ((LStatement.MetaData != null) && LStatement.MetaData.Tags.Contains("DAE.SourceOperatorName"))
				LSourceOperatorName = MetaData.GetTag(LStatement.MetaData, "DAE.SourceOperatorName", String.Empty);
			try
			{
				LNode.CreateOperator = new Schema.Operator(Schema.Object.GetObjectID(LStatement.MetaData), LOperatorName);
				LNode.CreateOperator.MetaData = LStatement.MetaData;
				LNode.CreateOperator.SessionObjectName = LSessionOperatorName;
				LNode.CreateOperator.SessionID = APlan.SessionID;
				LNode.CreateOperator.SourceOperatorName = LSourceOperatorName;
				LNode.CreateOperator.IsGenerated = (LSessionOperatorName != null) || (LSourceOperatorName != null);
				// If this is an A/T operator and we are not in an A/T, then it must be recompiled when it is first used within an A/T
				LNode.CreateOperator.ShouldRecompile = LNode.CreateOperator.IsATObject && (APlan.ApplicationTransactionID == Guid.Empty);
				
				APlan.PushCreationObject(LNode.CreateOperator);
				try
				{
					foreach (FormalParameter LFormalParameter in LStatement.FormalParameters)
						LNode.CreateOperator.Operands.Add(new Schema.Operand(LNode.CreateOperator, LFormalParameter.Identifier, CompileTypeSpecifier(APlan, LFormalParameter.TypeSpecifier), LFormalParameter.Modifier));

					if (LStatement.ReturnType != null)
						LNode.CreateOperator.ReturnDataType = CompileTypeSpecifier(APlan, LStatement.ReturnType);
						
					try
					{
						CheckValidCatalogOperatorName(APlan, LStatement, LNode.CreateOperator.OperatorName, LNode.CreateOperator.Signature);
					}
					catch
					{
						// If this is a repository, and we are creating a duplicate operator, ignore this statement and move on
						// This will allow us to move operators into the base catalog object set without having to upgrade the
						// system catalog.
						if (APlan.IsRepository)
							return new NoOpNode();
						throw;
					}

					LNode.CreateOperator.Owner = APlan.User;
					LNode.CreateOperator.Library = LNode.CreateOperator.IsGenerated ? null : APlan.CurrentLibrary;

					#if USEVIRTUALOPERATORS
					LNode.CreateOperator.IsAbstract = LStatement.IsAbstract;
					LNode.CreateOperator.IsVirtual = LStatement.IsVirtual;
					LNode.CreateOperator.IsOverride = LStatement.IsOverride;
				
					LNode.CreateOperator.IsReintroduced = LStatement.IsReintroduced;
					if (LNode.CreateOperator.IsOverride)
					{
						lock (APlan.Catalog)
						{
							int LCatalogIndex = APlan.Catalog.IndexOfInherited(LNode.CreateOperator.Name, LNode.CreateOperator.Signature);
							if (LCatalogIndex < 0)
								throw new CompilerException(CompilerException.Codes.InvalidOverrideDirective, AStatement, LNode.CreateOperator.Name, LNode.CreateOperator.Signature.ToString());
							APlan.AcquireCatalogLock(APlan.Catalog[LCatalogIndex], LockMode.Shared);
							APlan.AttachDependency(APlan.Catalog[LCatalogIndex]);
						}
					}
					#endif
						
					ProcessSourceContext(APlan, LStatement, LNode);

					APlan.PlanCatalog.Add(LNode.CreateOperator);
					try
					{
						APlan.Catalog.OperatorResolutionCache.Clear(LNode.CreateOperator.OperatorName);

						LNode.CreateOperator.IsBuiltin = Instructions.Contains(Schema.Object.Unqualify(LNode.CreateOperator.OperatorName));
						if (LStatement.Block.ClassDefinition != null)
						{
							#if USEVIRTUAL
							if (LNode.CreateOperator.IsVirtualCall)
								throw new CompilerException(CompilerException.Codes.InvalidVirtualDirective, AStatement, LNode.CreateOperator.Name, LNode.CreateOperator.Signature);
							#endif
							APlan.CheckRight(Schema.RightNames.HostImplementation);
							APlan.CheckClassDependency(LNode.CreateOperator.Library, LStatement.Block.ClassDefinition);
							LNode.CreateOperator.Block.ClassDefinition = LStatement.Block.ClassDefinition;
						}
						else
						{
							LNode.CreateOperator.Block.BlockNode = BindOperatorBlock(APlan, LNode.CreateOperator, CompileOperatorBlock(APlan, LNode.CreateOperator, LStatement.Block.Block));
							LNode.CreateOperator.DetermineRemotable(APlan.CatalogDeviceSession);
						}
						
						LNode.CreateOperator.IsRemotable = Convert.ToBoolean(MetaData.GetTag(LNode.CreateOperator.MetaData, "DAE.IsRemotable", LNode.CreateOperator.IsRemotable.ToString()));
						LNode.CreateOperator.IsLiteral = Convert.ToBoolean(MetaData.GetTag(LNode.CreateOperator.MetaData, "DAE.IsLiteral", LNode.CreateOperator.IsLiteral.ToString()));
						LNode.CreateOperator.IsFunctional = Convert.ToBoolean(MetaData.GetTag(LNode.CreateOperator.MetaData, "DAE.IsFunctional", LNode.CreateOperator.IsFunctional.ToString()));
						LNode.CreateOperator.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(LNode.CreateOperator.MetaData, "DAE.IsDeterministic", LNode.CreateOperator.IsDeterministic.ToString()));
						LNode.CreateOperator.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(LNode.CreateOperator.MetaData, "DAE.IsRepeatable", LNode.CreateOperator.IsRepeatable.ToString()));
						LNode.CreateOperator.IsNilable = Convert.ToBoolean(MetaData.GetTag(LNode.CreateOperator.MetaData, "DAE.IsNilable", LNode.CreateOperator.IsNilable.ToString()));
						
						if (!LNode.CreateOperator.IsRepeatable && LNode.CreateOperator.IsDeterministic)
							LNode.CreateOperator.IsDeterministic = false;
						if (!LNode.CreateOperator.IsDeterministic && LNode.CreateOperator.IsLiteral)
							LNode.CreateOperator.IsLiteral = false;
						LNode.DetermineCharacteristics(APlan);

						return LNode;
					}
					catch
					{
						APlan.PlanCatalog.SafeRemove(LNode.CreateOperator);
						throw;
					}
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
			catch
			{
				if (LAddedSessionObject)
					APlan.PlanSessionOperators.RemoveAt(APlan.PlanSessionOperators.IndexOfName(LSessionOperatorName));
				throw;
			}
		}
		
		public static void RecompileOperator(Plan APlan, Schema.Operator AOperator)
		{
			APlan.AcquireCatalogLock(AOperator, LockMode.Exclusive);
			try
			{
				AOperator.ShouldRecompile = false;
				try
				{
					ApplicationTransaction LTransaction = null;
					if (!AOperator.IsATObject && (APlan.ApplicationTransactionID != Guid.Empty))
						LTransaction = APlan.GetApplicationTransaction();
					try
					{
						if (LTransaction != null)
							LTransaction.PushGlobalContext();
						try
						{
							if (AOperator.Block.ClassDefinition == null)
							{
								AOperator.Dependencies.Clear();
								AOperator.IsBuiltin = Instructions.Contains(Schema.Object.Unqualify(AOperator.OperatorName));

								Plan LPlan = new Plan(APlan.ServerProcess);
								try
								{
									LPlan.PushATCreationContext();
									try
									{
										LPlan.PushCreationObject(AOperator);
										try
										{
											LPlan.PushStatementContext(new StatementContext(StatementType.Select));
											try
											{
												LPlan.PushSecurityContext(new SecurityContext(AOperator.Owner));
												try
												{
													// Report dependencies for the signature and return types
													Schema.Catalog LDependencies = new Schema.Catalog();
													foreach (Schema.Operand LOperand in AOperator.Operands)
														LOperand.DataType.IncludeDependencies(APlan.CatalogDeviceSession, APlan.Catalog, LDependencies, EmitMode.ForCopy);
													if (AOperator.ReturnDataType != null)
														AOperator.ReturnDataType.IncludeDependencies(APlan.CatalogDeviceSession, APlan.Catalog, LDependencies, EmitMode.ForCopy);
													foreach (Schema.Object LObject in LDependencies)
														LPlan.AttachDependency(LObject);

													PlanNode LBlockNode = BindOperatorBlock(LPlan, AOperator, CompileOperatorBlock(LPlan, AOperator, new Parser().ParseStatement(AOperator.BodyText, null))); //AOperator.Block.BlockNode.EmitStatement(EmitMode.ForCopy)));
													LPlan.CheckCompiled();
													AOperator.Block.BlockNode = LBlockNode;
													AOperator.DetermineRemotable(APlan.CatalogDeviceSession);

													AOperator.IsRemotable = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsRemotable", AOperator.IsRemotable.ToString()));
													AOperator.IsLiteral = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsLiteral", AOperator.IsLiteral.ToString()));
													AOperator.IsFunctional = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsFunctional", AOperator.IsFunctional.ToString()));
													AOperator.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsDeterministic", AOperator.IsDeterministic.ToString()));
													AOperator.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsRepeatable", AOperator.IsRepeatable.ToString()));
													AOperator.IsNilable = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsNilable", AOperator.IsNilable.ToString()));
													
													if (!AOperator.IsRepeatable && AOperator.IsDeterministic)
														AOperator.IsDeterministic = false;
													if (!AOperator.IsDeterministic && AOperator.IsLiteral)
														AOperator.IsLiteral = false;

													APlan.CatalogDeviceSession.UpdateCatalogObject(AOperator);
												}
												finally
												{
													LPlan.PopSecurityContext();
												}
											}
											finally
											{
												LPlan.PopStatementContext();
											}
										}
										finally
										{
											LPlan.PopCreationObject();
										}
									}
									finally
									{
										LPlan.PopATCreationContext();
									}
								}
								finally
								{
									APlan.Messages.AddRange(LPlan.Messages);
									LPlan.Dispose();
								}
							}
						}
						finally
						{
							if (LTransaction != null)
								LTransaction.PopGlobalContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							Monitor.Exit(LTransaction);
					}
				}
				catch
				{
					AOperator.ShouldRecompile = true;
					throw;
				}
				
				// If this is an A/T operator and we are not in an A/T, then it must be recompiled when it is first used within an A/T
				AOperator.ShouldRecompile = AOperator.IsATObject && (APlan.ApplicationTransactionID == Guid.Empty);
			}
			finally
			{
				APlan.ReleaseCatalogLock(AOperator);
			}
		}
		
		public static void ProcessSourceContext(Plan APlan, CreateAggregateOperatorStatement AStatement, CreateOperatorNode LNode)
		{
			// SourceContext is the script that is currently being compiled
			// The Locator in the source context represents an offset into the script identified by the locator, not
			// the actual script contained in the SourceContext. Line numbers in AOperatorLineInfo will be relative 
			// to the actual script in SourceContext, not the locator.
			
			// Pull the debug locator from the DAE.Locator metadata tag, if present
			DebugLocator LLocator = Schema.Operator.GetLocator(LNode.CreateOperator.MetaData);
			if (LLocator != null)
				LNode.CreateOperator.Locator = LLocator;
			else
			{
				if (APlan.SourceContext != null)
				{
					// Determine the line offsets for the operator declaration
					LineInfo LLineInfo = new LineInfo();
					LLineInfo.Line = AStatement.Line;
					LLineInfo.LinePos = AStatement.LinePos;
					LLineInfo.EndLine = AStatement.Initialization.Line;
					LLineInfo.EndLinePos = AStatement.Initialization.LinePos;

					// Copy the text of the operator from the source context
					// Note that the text does not include the metadata for the operator, just the operator header and body.
					if ((AStatement.Initialization.ClassDefinition == null) || (AStatement.Aggregation.ClassDefinition == null) || (AStatement.Finalization.ClassDefinition == null))
					{
						Schema.AggregateOperator LAggregateOperator = (Schema.AggregateOperator)LNode.CreateOperator;
						LAggregateOperator.DeclarationText = SourceUtility.CopySection(APlan.SourceContext.Script, LLineInfo);
						LAggregateOperator.InitializationText = SourceUtility.CopySection(APlan.SourceContext.Script, new LineInfo(AStatement.Initialization.Line, AStatement.Initialization.LinePos, AStatement.Aggregation.Line, AStatement.Aggregation.LinePos));
						LAggregateOperator.AggregationText = SourceUtility.CopySection(APlan.SourceContext.Script, new LineInfo(AStatement.Aggregation.Line, AStatement.Aggregation.LinePos, AStatement.Finalization.Line, AStatement.Finalization.LinePos));
						LAggregateOperator.FinalizationText = SourceUtility.CopySection(APlan.SourceContext.Script, AStatement.Finalization.LineInfo);
					}
					
					// Set the debug locator to the combination of the source context debug locator and the operator line info
					if (APlan.SourceContext.Locator != null)
					{
						LNode.CreateOperator.Locator = 
							new DebugLocator
							(
								APlan.SourceContext.Locator.Locator, 
								APlan.SourceContext.Locator.Line + LLineInfo.Line, 
								LLineInfo.LinePos
							);
					}
					else
					{
						// If there is no locator, this is either dynamic or ad-hoc execution, and the locator should be a negative offset
						// so the operator text can be returned as the debug context.
						LNode.CreateOperator.Locator =
							new DebugLocator
							(
								DebugLocator.OperatorLocator(LNode.CreateOperator.DisplayName),
								-(LLineInfo.Line - 1),
								LLineInfo.LinePos
							);
					}
				}
			}
		}
		
		public static PlanNode CompileCreateAggregateOperatorStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateAggregateOperatorStatement LStatement = (CreateAggregateOperatorStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateOperator);
			CreateOperatorNode LNode = new CreateOperatorNode();
			string LOperatorName = Schema.Object.Qualify(LStatement.OperatorName, APlan.CurrentLibrary.Name);
			string LSessionOperatorName = null;
			bool LAddedSessionObject = false;
			if (LStatement.IsSession)
			{
				LSessionOperatorName = LOperatorName;
				int LIndex = APlan.PlanSessionOperators.IndexOfName(LOperatorName);
				if (LIndex >= 0)
					LOperatorName = ((Schema.SessionObject)APlan.PlanSessionOperators[LIndex]).GlobalName;
				else
				{
					if (APlan.IsRepository)
						LOperatorName = MetaData.GetTag(LStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						LOperatorName = Schema.Object.NameFromGuid(Guid.NewGuid());
					APlan.PlanSessionOperators.Add(new Schema.SessionObject(LSessionOperatorName, LOperatorName));
					LAddedSessionObject = true;
				}
			}
			try
			{
				Schema.AggregateOperator LOperator = new Schema.AggregateOperator(Schema.Object.GetObjectID(LStatement.MetaData), LOperatorName);
				LOperator.MetaData = LStatement.MetaData;
				LOperator.IsOrderDependent = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsOrderDependent", "false"));
				LOperator.SessionObjectName = LSessionOperatorName;
				LOperator.SessionID = APlan.SessionID;
				LNode.CreateOperator = LOperator;
				APlan.PushCreationObject(LOperator);
				try
				{
					foreach (FormalParameter LFormalParameter in LStatement.FormalParameters)
						LOperator.Operands.Add(new Schema.Operand(LOperator, LFormalParameter.Identifier, CompileTypeSpecifier(APlan, LFormalParameter.TypeSpecifier), LFormalParameter.Modifier));

					LOperator.ReturnDataType = CompileTypeSpecifier(APlan, LStatement.ReturnType);
					
					CheckValidCatalogOperatorName(APlan, LStatement, LOperator.OperatorName, LOperator.Signature);

					LOperator.Owner = APlan.User;
					LOperator.Library = APlan.CurrentLibrary;
						
					ProcessSourceContext(APlan, LStatement, LNode);

					APlan.PlanCatalog.Add(LOperator);
					try
					{
						APlan.Catalog.OperatorResolutionCache.Clear(LOperator.OperatorName);

						#if USEVIRTUALOPERATORS
						LOperator.IsAbstract = LStatement.IsAbstract;
						LOperator.IsVirtual = LStatement.IsVirtual;
						LOperator.IsOverride = LStatement.IsOverride;
						if (LOperator.IsOverride)
						{
							lock (APlan.Catalog)
							{
								int LCatalogIndex = APlan.Catalog.IndexOfInherited(LOperator.Name, LOperator.Signature);
								if (LCatalogIndex < 0)
									throw new CompilerException(CompilerException.Codes.InvalidOverrideDirective, AStatement, LOperator.Name, LOperator.Signature.ToString());
								APlan.AcquireCatalogLock(APlan.Catalog[LCatalogIndex], LockMode.Shared);
								APlan.AttachDependency(APlan.Catalog[LCatalogIndex]);
							}
						}
						#endif
						
						APlan.Symbols.PushWindow(0);
						try
						{
							Symbol LResultVar = new Symbol(Keywords.Result, LOperator.ReturnDataType);
							APlan.Symbols.Push(LResultVar);

							int LSymbolCount = APlan.Symbols.Count;
							if (LStatement.Initialization.ClassDefinition != null)
							{
								APlan.CheckRight(Schema.RightNames.HostImplementation);
								APlan.CheckClassDependency(LOperator.Library, LStatement.Initialization.ClassDefinition);
								LOperator.Initialization.ClassDefinition = LStatement.Initialization.ClassDefinition;
								LOperator.Initialization.StackDisplacement = Convert.ToInt32(MetaData.GetTag(LOperator.MetaData, "DAE.Initialization.StackDisplacement", "0"));
							}
							else
							{
								LOperator.Initialization.BlockNode = CompileStatement(APlan, LStatement.Initialization.Block);
								LOperator.Initialization.StackDisplacement = APlan.Symbols.Count - LSymbolCount;
							}
							
							if (LStatement.Aggregation.ClassDefinition != null)
							{
								APlan.CheckRight(Schema.RightNames.HostImplementation);
								APlan.CheckClassDependency(LOperator.Library, LStatement.Aggregation.ClassDefinition);
								LOperator.Aggregation.ClassDefinition = LStatement.Aggregation.ClassDefinition;
							}
							else
							{
								APlan.Symbols.PushFrame();
								try
								{
									foreach (Schema.Operand LOperand in LOperator.Operands)
										APlan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));

									LOperator.Aggregation.BlockNode = CompileDeallocateFrameVariablesNode(APlan, CompileStatement(APlan, LStatement.Aggregation.Block));
								}
								finally
								{
									APlan.Symbols.PopFrame();
								}
							}
								
							if (LStatement.Finalization.ClassDefinition != null)
							{
								APlan.CheckRight(Schema.RightNames.HostImplementation);
								APlan.CheckClassDependency(LOperator.Library, LStatement.Finalization.ClassDefinition);
								LOperator.Finalization.ClassDefinition = LStatement.Finalization.ClassDefinition;
								LOperator.Finalization.BlockNode = CompileDeallocateVariablesNode(APlan, new NoOpNode(), LResultVar);
							}
							else
							{
								LOperator.Finalization.BlockNode = CompileDeallocateVariablesNode(APlan, CompileStatement(APlan, LStatement.Finalization.Block), LResultVar);
							}
						}
						finally
						{
							APlan.Symbols.PopWindow();
						}
						
						// Binding phase
						APlan.Symbols.PushWindow(0);
						try
						{
							APlan.Symbols.Push(new Symbol(Keywords.Result, LOperator.ReturnDataType));
							
							if (LStatement.Initialization.ClassDefinition == null)
							{
								LOperator.Initialization.BlockNode = OptimizeNode(APlan, LOperator.Initialization.BlockNode);
								LOperator.Initialization.BlockNode = BindNode(APlan, LOperator.Initialization.BlockNode);
							}
							
							if (LStatement.Aggregation.ClassDefinition == null)
							{
								APlan.Symbols.PushFrame();
								try
								{
									foreach (Schema.Operand LOperand in LOperator.Operands)
										APlan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));

									LOperator.Aggregation.BlockNode = OptimizeNode(APlan, LOperator.Aggregation.BlockNode);
									LOperator.Aggregation.BlockNode = BindNode(APlan, LOperator.Aggregation.BlockNode);
								}
								finally
								{
									APlan.Symbols.PopFrame();
								}
							}
							
							if (LStatement.Finalization.ClassDefinition == null)
							{
								LOperator.Finalization.BlockNode = OptimizeNode(APlan, LOperator.Finalization.BlockNode);
								LOperator.Finalization.BlockNode = BindNode(APlan, LOperator.Finalization.BlockNode);
							}
						}
						finally
						{
							APlan.Symbols.PopWindow();
						}
						
						LOperator.DetermineRemotable(APlan.CatalogDeviceSession);
						LOperator.IsRemotable = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsRemotable", LOperator.IsRemotable.ToString()));
						LOperator.IsLiteral = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsLiteral", LOperator.IsLiteral.ToString()));
						LOperator.IsFunctional = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsFunctional", LOperator.IsFunctional.ToString()));
						LOperator.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsDeterministic", LOperator.IsDeterministic.ToString()));
						LOperator.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsRepeatable", LOperator.IsRepeatable.ToString()));
						LOperator.IsNilable = Convert.ToBoolean(MetaData.GetTag(LOperator.MetaData, "DAE.IsNilable", LOperator.IsNilable.ToString()));

						if (!LOperator.IsRepeatable && LOperator.IsDeterministic)
							LOperator.IsDeterministic = false;
						if (!LOperator.IsDeterministic && LOperator.IsLiteral)
							LOperator.IsLiteral = false;

						LNode.DetermineCharacteristics(APlan);
						return LNode;
					}
					catch
					{
						APlan.PlanCatalog.SafeRemove(LOperator);
						throw;
					}
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
			catch
			{
				if (LAddedSessionObject)
					APlan.PlanSessionOperators.RemoveAt(APlan.PlanSessionOperators.IndexOfName(LSessionOperatorName));
				throw;
			}
		}
		
		public static void RecompileAggregateOperator(Plan APlan, Schema.AggregateOperator AOperator)
		{
			APlan.AcquireCatalogLock(AOperator, LockMode.Exclusive);
			try
			{
				ApplicationTransaction LTransaction = null;
				if (!AOperator.IsATObject && (APlan.ApplicationTransactionID != Guid.Empty))
					LTransaction = APlan.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushGlobalContext();
					try
					{
						PlanNode LSaveInitializationNode = AOperator.Initialization.BlockNode;
						int LSaveInitializationDisplacement = AOperator.Initialization.StackDisplacement;
						PlanNode LSaveAggregationNode = AOperator.Aggregation.BlockNode;
						PlanNode LSaveFinalizationNode = AOperator.Finalization.BlockNode;
						AOperator.Dependencies.Clear();
						AOperator.IsBuiltin = Instructions.Contains(Schema.Object.Unqualify(AOperator.OperatorName));
						Plan LPlan = new Plan(APlan.ServerProcess);
						try
						{
							LPlan.PushATCreationContext();
							try
							{
								LPlan.PushCreationObject(AOperator);
								try
								{
									LPlan.PushStatementContext(new StatementContext(StatementType.Select));
									try
									{
										LPlan.PushSecurityContext(new SecurityContext(AOperator.Owner));
										try
										{
											// Report dependencies for the signature and return types
											Schema.Catalog LDependencies = new Schema.Catalog();
											foreach (Schema.Operand LOperand in AOperator.Operands)
												LOperand.DataType.IncludeDependencies(APlan.CatalogDeviceSession, APlan.Catalog, LDependencies, EmitMode.ForCopy);
											if (AOperator.ReturnDataType != null)
												AOperator.ReturnDataType.IncludeDependencies(APlan.CatalogDeviceSession, APlan.Catalog, LDependencies, EmitMode.ForCopy);
											foreach (Schema.Object LObject in LDependencies)
												LPlan.AttachDependency(LObject);

											PlanNode LInitializationNode = null;
											int LInitializationDisplacement = 0;
											PlanNode LAggregationNode = null;
											PlanNode LFinalizationNode = null;

											LPlan.Symbols.PushWindow(0);
											try
											{
												Symbol LResultVar = new Symbol(Keywords.Result, AOperator.ReturnDataType);
												LPlan.Symbols.Push(LResultVar);
												
												if (AOperator.Initialization.ClassDefinition == null)
												{
													int LSymbolCount = LPlan.Symbols.Count;
													LInitializationNode = CompileStatement(LPlan, new Parser().ParseStatement(AOperator.InitializationText, null)); //AOperator.Initialization.BlockNode.EmitStatement(EmitMode.ForCopy)); 
													LInitializationDisplacement = LPlan.Symbols.Count - LSymbolCount;
												}
				
												if (AOperator.Aggregation.ClassDefinition == null)
												{
													LPlan.Symbols.PushFrame();
													try
													{
														foreach (Schema.Operand LOperand in AOperator.Operands)
															LPlan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));

														LAggregationNode = CompileDeallocateFrameVariablesNode(LPlan, CompileStatement(LPlan, new Parser().ParseStatement(AOperator.AggregationText, null))); //AOperator.Aggregation.BlockNode.EmitStatement(EmitMode.ForCopy)));
													}
													finally
													{
														LPlan.Symbols.PopFrame();
													}
												}

												if (AOperator.Finalization.ClassDefinition == null)
													LFinalizationNode = CompileDeallocateVariablesNode(LPlan, CompileStatement(LPlan, new Parser().ParseStatement(AOperator.FinalizationText, null)), LResultVar); //AOperator.Finalization.BlockNode.EmitStatement(EmitMode.ForCopy)), LResultVar);
											}
											finally
											{
												LPlan.Symbols.PopWindow();
											}

											LPlan.Symbols.PushWindow(0);
											try
											{
												LPlan.Symbols.Push(new Symbol(Keywords.Result, AOperator.ReturnDataType));

												if (AOperator.Initialization.ClassDefinition == null)
													LInitializationNode = BindNode(LPlan, OptimizeNode(LPlan, LInitializationNode));
												
												if (AOperator.Aggregation.ClassDefinition == null)
												{
													LPlan.Symbols.PushFrame();
													try
													{
														foreach (Schema.Operand LOperand in AOperator.Operands)
															LPlan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));
														LAggregationNode = BindNode(LPlan, OptimizeNode(LPlan, LAggregationNode));
													}
													finally
													{
														LPlan.Symbols.PopFrame();
													}
												}

												if (AOperator.Finalization.ClassDefinition == null)
													LFinalizationNode = BindNode(LPlan, OptimizeNode(LPlan, LFinalizationNode));
											}
											finally
											{
												LPlan.Symbols.PopWindow();
											}

											LPlan.CheckCompiled();

											if (LInitializationNode != null)
											{
												AOperator.Initialization.BlockNode = LInitializationNode;
												AOperator.Initialization.StackDisplacement = LInitializationDisplacement;
											}
											
											if (LAggregationNode != null)
												AOperator.Aggregation.BlockNode = LAggregationNode;
												
											if (LFinalizationNode != null)
												AOperator.Finalization.BlockNode = LFinalizationNode;

											AOperator.DetermineRemotable(APlan.CatalogDeviceSession);
											AOperator.IsRemotable = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsRemotable", AOperator.IsRemotable.ToString()));
											AOperator.IsLiteral = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsLiteral", AOperator.IsLiteral.ToString()));
											AOperator.IsFunctional = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsFunctional", AOperator.IsFunctional.ToString()));
											AOperator.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsDeterministic", AOperator.IsDeterministic.ToString()));
											AOperator.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsRepeatable", AOperator.IsRepeatable.ToString()));
											AOperator.IsNilable = Convert.ToBoolean(MetaData.GetTag(AOperator.MetaData, "DAE.IsNilable", AOperator.IsNilable.ToString()));
											
											if (!AOperator.IsRepeatable && AOperator.IsDeterministic)
												AOperator.IsDeterministic = false;
											if (!AOperator.IsDeterministic && AOperator.IsLiteral)
												AOperator.IsLiteral = false;

											APlan.CatalogDeviceSession.UpdateCatalogObject(AOperator);
										}
										finally
										{
											LPlan.PopSecurityContext();
										}
									}
									finally
									{
										LPlan.PopStatementContext();
									}
								}
								finally
								{
									LPlan.PopCreationObject();
								}
							}
							finally
							{
								LPlan.PopATCreationContext();
							}
						}
						finally
						{
							APlan.Messages.AddRange(LPlan.Messages);
							LPlan.Dispose();
						}

						AOperator.ShouldRecompile = false;
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
			finally
			{
				APlan.ReleaseCatalogLock(AOperator);
			}
		}
		
		public static PlanNode CompileCreateConstraintStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateConstraintStatement LStatement = (CreateConstraintStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateConstraint);
			APlan.Symbols.PushWindow(0); // Make sure the create constraint statement is evaluated in a private context
			try
			{
				CreateConstraintNode LNode = new CreateConstraintNode();
				string LConstraintName = Schema.Object.Qualify(LStatement.ConstraintName, APlan.CurrentLibrary.Name);
				string LSessionConstraintName = null;
				if (LStatement.IsSession)
				{
					LSessionConstraintName = LConstraintName;
					if (APlan.IsRepository)
						LConstraintName = MetaData.GetTag(LStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						LConstraintName = Schema.Object.NameFromGuid(Guid.NewGuid());
					CheckValidSessionObjectName(APlan, AStatement, LSessionConstraintName);
					APlan.PlanSessionObjects.Add(new Schema.SessionObject(LSessionConstraintName, LConstraintName));
				} 
				
				CheckValidCatalogObjectName(APlan, AStatement, LConstraintName);

				LNode.Constraint = new Schema.CatalogConstraint(Schema.Object.GetObjectID(LStatement.MetaData), LConstraintName);
				LNode.Constraint.SessionObjectName = LSessionConstraintName;
				LNode.Constraint.SessionID = APlan.SessionID;
				LNode.Constraint.IsGenerated = LStatement.IsSession;
				LNode.Constraint.Owner = APlan.User;
				LNode.Constraint.Library = LNode.Constraint.IsGenerated ? null : APlan.CurrentLibrary;
				LNode.Constraint.ConstraintType = Schema.ConstraintType.Database;
				LNode.Constraint.MetaData = LStatement.MetaData;
				LNode.Constraint.Enforced = GetEnforced(APlan, LNode.Constraint.MetaData);
				APlan.PlanCatalog.Add(LNode.Constraint);
				try
				{
					APlan.PushCreationObject(LNode.Constraint);
					try
					{
						LNode.Constraint.Node = CompileBooleanExpression(APlan, LStatement.Expression);
						if (!(LNode.Constraint.Node.IsFunctional && LNode.Constraint.Node.IsDeterministic))
							throw new CompilerException(CompilerException.Codes.InvalidConstraintExpression, LStatement.Expression);

						LNode.Constraint.Node = OptimizeNode(APlan, LNode.Constraint.Node);						
						LNode.Constraint.Node = BindNode(APlan, LNode.Constraint.Node);						

						string LCustomMessage = LNode.Constraint.GetCustomMessage();
						if (!String.IsNullOrEmpty(LCustomMessage))
						{
							try
							{
								PlanNode LViolationMessageNode = CompileTypedExpression(APlan, new D4.Parser().ParseExpression(LCustomMessage), APlan.DataTypes.SystemString);
								LViolationMessageNode = OptimizeNode(APlan, LViolationMessageNode);
								LViolationMessageNode = BindNode(APlan, LViolationMessageNode);
								LNode.Constraint.ViolationMessageNode = LViolationMessageNode;
							}
							catch (Exception LException)
							{
								throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, LStatement.Expression, LException, LNode.Constraint.Name);
							}
						}
						
						LNode.Constraint.DetermineRemotable(APlan.CatalogDeviceSession);
						
						return LNode;
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}
				catch
				{
					APlan.PlanCatalog.SafeRemove(LNode.Constraint);
					throw;
				}
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan APlan, Schema.Columns AColumns)
		{
			return EmitKeyIsNotNilNode(APlan, "", AColumns, null);
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan APlan, Schema.Columns AColumns, BitArray AIsNilable)
		{
			return EmitKeyIsNotNilNode(APlan, "", AColumns, AIsNilable);
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan APlan, string ARowVarName, Schema.Columns AColumns)
		{
			return EmitKeyIsNotNilNode(APlan, ARowVarName, AColumns, null);
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan APlan, string ARowVarName, Schema.Columns AColumns, BitArray AIsNilable)
		{
			PlanNode LNode = EmitKeyIsNilNode(APlan, ARowVarName, AColumns, AIsNilable);
			if (LNode != null)
				LNode = EmitUnaryNode(APlan, Instructions.Not, LNode);
			return LNode;
		}
		
		// IsNil(AValue) {or IsNil(AValue)}
		public static PlanNode EmitKeyIsNilNode(Plan APlan, Schema.Columns AColumns)
		{
			return EmitKeyIsNilNode(APlan, "", AColumns, null);
		}
		
		public static PlanNode EmitKeyIsNilNode(Plan APlan, Schema.Columns AColumns, BitArray AIsNilable)
		{
			return EmitKeyIsNilNode(APlan, "", AColumns, AIsNilable);
		}
		
		public static PlanNode EmitKeyIsNilNode(Plan APlan, string ARowVarName, Schema.Columns AColumns)
		{
			return EmitKeyIsNilNode(APlan, ARowVarName, AColumns, null);
		}
		
		public static PlanNode EmitKeyIsNilNode(Plan APlan, string ARowVarName, Schema.Columns AColumns, BitArray AIsNilable)
		{
			PlanNode LNode = null;
			
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				if ((AIsNilable == null) || AIsNilable[LIndex])
					LNode =
						AppendNode
						(
							APlan,
							LNode,
							Instructions.Or,
							EmitUnaryNode
							(
								APlan, 
								CIsNilOperatorName, 
								ARowVarName == String.Empty ? 
									EmitIdentifierNode(APlan, AColumns[LIndex].Name) : 
									CompileQualifierExpression(APlan, new QualifierExpression(new IdentifierExpression(ARowVarName), new IdentifierExpression(AColumns[LIndex].Name)))
							)
						);
			
			return LNode;
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan APlan, Schema.Columns AColumns)
		{
			return EmitKeyIsNotSpecialNode(APlan, "", AColumns, null);
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan APlan, Schema.Columns AColumns, BitArray AHasSpecials)
		{
			return EmitKeyIsNotSpecialNode(APlan, "", AColumns, AHasSpecials);
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan APlan, string ARowVarName, Schema.Columns AColumns)
		{
			return EmitKeyIsNotSpecialNode(APlan, ARowVarName, AColumns, null);
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan APlan, string ARowVarName, Schema.Columns AColumns, BitArray AHasSpecials)
		{
			PlanNode LNode = EmitKeyIsSpecialNode(APlan, ARowVarName, AColumns, AHasSpecials);
			if (LNode != null)
				return EmitUnaryNode(APlan, Instructions.Not, LNode);
			return LNode;
		}
		
		// IsSpecial(AValue) {or IsSpecial(Value)}
		public static PlanNode EmitKeyIsSpecialNode(Plan APlan, Schema.Columns AColumns)
		{
			return EmitKeyIsSpecialNode(APlan, "", AColumns, null);
		}
		
		public static PlanNode EmitKeyIsSpecialNode(Plan APlan, Schema.Columns AColumns, BitArray AHasSpecials)
		{
			return EmitKeyIsSpecialNode(APlan, "", AColumns, AHasSpecials);
		}
		
		public static PlanNode EmitKeyIsSpecialNode(Plan APlan, string ARowVarName, Schema.Columns AColumns)
		{
			return EmitKeyIsSpecialNode(APlan, ARowVarName, AColumns, null);
		}
		
		public static PlanNode EmitKeyIsSpecialNode(Plan APlan, string ARowVarName, Schema.Columns AColumns, BitArray AHasSpecials)
		{
			PlanNode LNode = null;
			
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				if ((AHasSpecials == null) || AHasSpecials[LIndex])
					LNode = 
						AppendNode
						(
							APlan, 
							LNode, 
							Instructions.Or, 
							EmitUnaryNode
							(
								APlan, 
								CIsSpecialOperatorName,
								ARowVarName == String.Empty ?
									EmitIdentifierNode(APlan, AColumns[LIndex].Name) :
									CompileQualifierExpression(APlan, new QualifierExpression(new IdentifierExpression(ARowVarName), new IdentifierExpression(AColumns[LIndex].Name)))
							)
						);
			
			return LNode;
		}
		
		// tags { DAE.Message = "'The table {0} does not have a row with <target column names> ' + <new.column names>.AsString + '." };
		public static string GetCustomMessageForSourceReference(Plan APlan, Schema.Reference AReference)
		{
			StringBuilder LMessage = new StringBuilder();
			LMessage.AppendFormat("'The table {0} does not have a row with ", Schema.Object.Unqualify(AReference.TargetTable.DisplayName));
			Schema.ScalarType LScalarType;
			Schema.Representation LRepresentation;
			
			for (int LIndex = 0; LIndex < AReference.TargetKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LMessage.Append(" and ");
				LMessage.AppendFormat("{0} ", AReference.TargetKey.Columns[LIndex].Name);
				LScalarType = (Schema.ScalarType)AReference.TargetKey.Columns[LIndex].DataType;
				LRepresentation = LScalarType.FindRepresentation(NativeAccessors.AsDisplayString);
				bool LIsString = LScalarType.NativeType == NativeAccessors.AsDisplayString.NativeType;
				if (LIsString)
					LMessage.AppendFormat(@"""' + {0}{1}{2}{3}{4} + '""", new object[]{Keywords.New, Keywords.Qualifier, AReference.SourceKey.Columns[LIndex].Name, Keywords.Qualifier, LRepresentation.Properties[0].Name});
				else
					LMessage.AppendFormat(@"(' + {0}{1}{2}{3}{4} + ')", new object[]{Keywords.New, Keywords.Qualifier, AReference.SourceKey.Columns[LIndex].Name, Keywords.Qualifier, LRepresentation.Properties[0].Name});
			}

			LMessage.Append(".'");
			return LMessage.ToString();
		}

		// tags { DAE.Message = "'The table {0} has rows with <source column names> ' + <old.column names>.AsString + '." };
		public static string GetCustomMessageForTargetReference(Plan APlan, Schema.Reference AReference)
		{
			StringBuilder LMessage = new StringBuilder();
			LMessage.AppendFormat("'The table {0} has rows with ", Schema.Object.Unqualify(AReference.SourceTable.DisplayName));
			Schema.ScalarType LScalarType;
			Schema.Representation LRepresentation;
			
			for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LMessage.Append(" and ");
				LMessage.AppendFormat("{0} ", AReference.SourceKey.Columns[LIndex].Name);
				LScalarType = (Schema.ScalarType)AReference.SourceKey.Columns[LIndex].DataType;
				LRepresentation = LScalarType.FindRepresentation(NativeAccessors.AsDisplayString);
				bool LIsString = LScalarType.NativeType == NativeAccessors.AsDisplayString.NativeType;
				if (LIsString)
					LMessage.AppendFormat(@"""' + {0}{1}{2}{3}{4} + '""", new object[]{Keywords.Old, Keywords.Qualifier, AReference.TargetKey.Columns[LIndex].Name, Keywords.Qualifier, LRepresentation.Properties[0].Name});
				else
					LMessage.AppendFormat(@"(' + {0}{1}{2}{3}{4} + ')", new object[]{Keywords.Old, Keywords.Qualifier, AReference.TargetKey.Columns[LIndex].Name, Keywords.Qualifier, LRepresentation.Properties[0].Name});
			}

			LMessage.Append(".'");
			return LMessage.ToString();
		}
		
		public static Schema.TransitionConstraint CompileSourceReferenceConstraint(Plan APlan, Schema.Reference AReference)
		{
			Schema.TransitionConstraint LConstraint = new Schema.TransitionConstraint(String.Format("{0}{1}", "Source", AReference.Name));
			LConstraint.Library = AReference.Library;
			LConstraint.MergeMetaData(AReference.MetaData);
			if (LConstraint.MetaData == null)
				LConstraint.MetaData = new MetaData();
			if (!(LConstraint.MetaData.Tags.Contains("DAE.Message") || LConstraint.MetaData.Tags.Contains("DAE.SimpleMessage")) && CanBuildCustomMessageForKey(APlan, AReference.SourceKey))
				LConstraint.MetaData.Tags.Add(new Tag("DAE.Message", GetCustomMessageForSourceReference(APlan, AReference)));
			LConstraint.IsGenerated = true;
			LConstraint.ConstraintType = Schema.ConstraintType.Database;
			LConstraint.OnInsertNode = CompileSourceInsertConstraintNodeForReference(APlan, AReference);
			LConstraint.InsertColumnFlags = new BitArray(AReference.SourceTable.DataType.Columns.Count);
			for (int LIndex = 0; LIndex < LConstraint.InsertColumnFlags.Length; LIndex++)
				LConstraint.InsertColumnFlags[LIndex] = AReference.SourceKey.Columns.ContainsName(AReference.SourceTable.DataType.Columns[LIndex].Name);
			LConstraint.OnUpdateNode = CompileSourceUpdateConstraintNodeForReference(APlan, AReference);
			LConstraint.UpdateColumnFlags = (BitArray)LConstraint.InsertColumnFlags.Clone();
			return LConstraint;
		}
		
		// Construct an insert constraint to validate rows inserted into the source table of the reference
		// Used by all reference types.
		//
		// on insert into A ->
		//		[IsNil(AValues) or] [IsSpecial(AValues) or] exists(B where BKeys = AValues)
		public static PlanNode CompileSourceInsertConstraintNodeForReference(Plan APlan, Schema.Reference AReference)
		{
			APlan.EnterRowContext();
			try
			{
				Schema.IRowType LRowType = new Schema.RowType(AReference.SourceKey.Columns, Keywords.New);
				BitArray LIsNilable = new BitArray(AReference.SourceKey.Columns.Count);
				BitArray LHasSpecials = new BitArray(AReference.SourceKey.Columns.Count);
				for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
				{
					LIsNilable[LIndex] = AReference.SourceKey.Columns[LIndex].IsNilable;
					LHasSpecials[LIndex] = 
						(AReference.SourceKey.Columns[LIndex].DataType is Schema.ScalarType) 
							&& (((Schema.ScalarType)AReference.SourceKey.Columns[LIndex].DataType).Specials.Count > 0);
				}
				
				APlan.Symbols.Push(new Symbol(AReference.SourceTable.DataType.NewRowType));
				try
				{
					PlanNode LNode = 
						AppendNode
						(
							APlan,
							EmitKeyIsNilNode(APlan, LRowType.Columns, LIsNilable),
							Instructions.Or,
							AppendNode
							(
								APlan,
								EmitKeyIsSpecialNode(APlan, LRowType.Columns, LHasSpecials),
								Instructions.Or,
								EmitUnaryNode
								(
									APlan, 
									Instructions.Exists, 
									EmitRestrictNode
									(
										APlan,
										EmitTableVarNode(APlan, AReference.TargetTable), 
										BuildKeyEqualExpression
										(
											APlan,
											new Schema.RowType(AReference.TargetKey.Columns).Columns, 
											LRowType.Columns
										)
									)
								)
							)
						);
					return BindNode(APlan, LNode);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		// Construct an update constraint to validate rows updated in the source table of the reference
		// Used by all reference types.
		//
		// on update of A ->
		//		IsNil(NewAValues) or IsSpecial(NewAValues) or (OldAValues = NewAValues) or (exists(B where BKeys = NewAValues))
		public static PlanNode CompileSourceUpdateConstraintNodeForReference(Plan APlan, Schema.Reference AReference)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(AReference.SourceTable.DataType.OldRowType));
				try
				{
					APlan.Symbols.Push(new Symbol(AReference.SourceTable.DataType.NewRowType));
					try
					{
						Schema.IRowType LNewSourceRowType = new Schema.RowType(AReference.SourceKey.Columns, Keywords.New);
						Schema.IRowType LOldSourceRowType = new Schema.RowType(AReference.SourceKey.Columns, Keywords.Old);

						BitArray LIsNilable = new BitArray(AReference.SourceKey.Columns.Count);
						BitArray LHasSpecials = new BitArray(AReference.SourceKey.Columns.Count);
						for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
						{
							LIsNilable[LIndex] = AReference.SourceKey.Columns[LIndex].IsNilable;
							LHasSpecials[LIndex] =
								(AReference.SourceKey.Columns[LIndex].DataType is Schema.ScalarType) 
									&& (((Schema.ScalarType)AReference.SourceKey.Columns[LIndex].DataType).Specials.Count > 0);
						}
						
						PlanNode LEqualNode = CompileExpression(APlan, BuildKeyEqualExpression(APlan, LOldSourceRowType.Columns, LNewSourceRowType.Columns));

						PlanNode LNode = 
							AppendNode
							(
								APlan,
								EmitKeyIsNilNode(APlan, LNewSourceRowType.Columns, LIsNilable),
								Instructions.Or,
								AppendNode
								(
									APlan,
									EmitKeyIsSpecialNode(APlan, LNewSourceRowType.Columns, LHasSpecials),
									Instructions.Or,
									AppendNode
									(
										APlan,
										LEqualNode,
										Instructions.Or,
										EmitUnaryNode
										(
											APlan, 
											Instructions.Exists, 
											EmitRestrictNode
											(
												APlan, 
												EmitTableVarNode(APlan, AReference.TargetTable), 
												BuildKeyEqualExpression
												(
													APlan,
													new Schema.RowType(AReference.TargetKey.Columns).Columns, 
													LNewSourceRowType.Columns
												)
											)
										)
									)
								)
							);
						return BindNode(APlan, LNode);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public static Schema.TransitionConstraint CompileTargetReferenceConstraint(Plan APlan, Schema.Reference AReference)
		{
			Schema.TransitionConstraint LConstraint = new Schema.TransitionConstraint(String.Format("{0}{1}", "Target", AReference.Name));
			LConstraint.Library = AReference.Library;
			LConstraint.MergeMetaData(AReference.MetaData);
			if (LConstraint.MetaData == null)
				LConstraint.MetaData = new MetaData();
			if (!(LConstraint.MetaData.Tags.Contains("DAE.Message") || LConstraint.MetaData.Tags.Contains("DAE.SimpleMessage")) && CanBuildCustomMessageForKey(APlan, AReference.TargetKey))
				LConstraint.MetaData.Tags.Add(new Tag("DAE.Message", GetCustomMessageForTargetReference(APlan, AReference)));
			LConstraint.IsGenerated = true;
			LConstraint.ConstraintType = Schema.ConstraintType.Database;
			
			if (AReference.UpdateReferenceAction == ReferenceAction.Require)
			{
				LConstraint.OnUpdateNode = CompileTargetUpdateConstraintNodeForReference(APlan, AReference);
				LConstraint.UpdateColumnFlags = new BitArray(AReference.TargetTable.DataType.Columns.Count);
				for (int LIndex = 0; LIndex < LConstraint.UpdateColumnFlags.Length; LIndex++)
					LConstraint.UpdateColumnFlags[LIndex] = AReference.TargetKey.Columns.ContainsName(AReference.TargetTable.DataType.Columns[LIndex].Name);
			}
				
			if (AReference.DeleteReferenceAction == ReferenceAction.Require)
				LConstraint.OnDeleteNode = CompileTargetDeleteConstraintNodeForReference(APlan, AReference);
				
			return LConstraint;
		}
		
		// Construct an update constraint to validate rows updated in the target table of the reference
		// Used exclusively by the Require reference type.
		//		
		// on update of B ->
		//		(OldBValues = NewBValues) or (not(exists(A where AKeys = OldBValues)))
		public static PlanNode CompileTargetUpdateConstraintNodeForReference(Plan APlan, Schema.Reference AReference)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(AReference.TargetTable.DataType.OldRowType));
				try
				{
					APlan.Symbols.Push(new Symbol(AReference.TargetTable.DataType.NewRowType));
					try
					{
						Schema.IRowType LOldTargetRowType = new Schema.RowType(AReference.TargetKey.Columns, Keywords.Old);
						Schema.IRowType LNewTargetRowType = new Schema.RowType(AReference.TargetKey.Columns, Keywords.New);

						PlanNode LEqualNode = CompileExpression(APlan, BuildKeyEqualExpression(APlan, LOldTargetRowType.Columns, LNewTargetRowType.Columns));
						PlanNode LNode = 
							EmitBinaryNode
							(
								APlan, 
								LEqualNode, 
								Instructions.Or, 
								EmitUnaryNode
								(
									APlan, 
									Instructions.Not, 
									EmitUnaryNode
									(
										APlan, 
										Instructions.Exists, 
										EmitRestrictNode
										(
											APlan, 
											EmitTableVarNode(APlan, AReference.SourceTable), 
											BuildKeyEqualExpression
											(
												APlan,
												new Schema.RowType(AReference.SourceKey.Columns).Columns, 
												LOldTargetRowType.Columns
											)
										)
									)
								)
							);
						return BindNode(APlan, LNode);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		// Construct a delete constraint to validate rows deleted in the target table of the reference
		// Used exclusively by the Require reference type.
		//
		// on delete from B ->
		//		not(exists(A where AKeys = BValues))
		public static PlanNode CompileTargetDeleteConstraintNodeForReference(Plan APlan, Schema.Reference AReference)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(AReference.TargetTable.DataType.OldRowType));
				try
				{
					PlanNode LNode = 
						EmitUnaryNode
						(
							APlan, 
							Instructions.Not, 
							EmitUnaryNode
							(
								APlan, 
								Instructions.Exists, 
								EmitRestrictNode
								(
									APlan, 
									EmitTableVarNode(APlan, AReference.SourceTable), 
									BuildKeyEqualExpression
									(
										APlan,
										new Schema.RowType(AReference.SourceKey.Columns).Columns, 
										new Schema.RowType(AReference.TargetKey.Columns, Keywords.Old).Columns
									)
								)
							)
						);
					return BindNode(APlan, LNode);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		// Constructs an update statement used to cascade updates on the target table.
		// Used exclusively by the Cascade reference type.
		//
		// on update of B ->
		//	if (NewBValues <> OldBValues)
		//		update A set { AKeys = NewBValues } where AKeys = OldBValues;
		public static PlanNode CompileUpdateCascadeNodeForReference(Plan APlan, PlanNode ASourceNode, Schema.Reference AReference)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				try
				{
					APlan.Symbols.Push(new Symbol("ANewRow", AReference.TargetTable.DataType.RowType));
					try
					{
						PlanNode LConditionNode = CompileExpression(APlan, new UnaryExpression(Instructions.Not, BuildKeyEqualExpression(APlan, "AOldRow", "ANewRow", AReference.TargetKey.Columns, AReference.TargetKey.Columns)));
						UpdateNode LUpdateNode = new UpdateNode();
						LUpdateNode.IsBreakable = false;
						APlan.Symbols.Push(new Symbol(((Schema.ITableType)ASourceNode.DataType).RowType));
						try
						{
							LUpdateNode.Nodes.Add(EmitUpdateConditionNode(APlan, ASourceNode, CompileExpression(APlan, BuildKeyEqualExpression(APlan, "", "AOldRow", AReference.SourceKey.Columns, AReference.TargetKey.Columns))));
							LUpdateNode.TargetNode = LUpdateNode.Nodes[0].Nodes[0];
							LUpdateNode.ConditionNode = LUpdateNode.Nodes[0].Nodes[1];

							for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
							{
								Schema.TableVarColumn LSourceColumn = AReference.SourceKey.Columns[LIndex];
								Schema.TableVarColumn LTargetColumn = AReference.TargetKey.Columns[LIndex];
								LUpdateNode.Nodes.Add
								(
									new UpdateColumnNode
									(
										LSourceColumn.DataType,
										LSourceColumn.Name,
										CompileQualifierExpression(APlan, new QualifierExpression(new IdentifierExpression("ANewRow"), new IdentifierExpression(LTargetColumn.Name)))
									)
								);
							}
							
							LUpdateNode.DetermineDataType(APlan);
							LUpdateNode.DetermineCharacteristics(APlan);
						}
						finally
						{
							APlan.Symbols.Pop();
						}

						return BindNode(APlan, EmitIfNode(APlan, null, LConditionNode, LUpdateNode, null));
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		// Constructs a delete statement used to cascade deletes on the target table.
		// Used exclusively by the Cascade reference type.
		//
		// on delete from B ->
		//		delete A where AKeys = BValues
		public static DeleteNode CompileDeleteCascadeNodeForReference(Plan APlan, PlanNode ASourceNode, Schema.Reference AReference)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				try
				{
					DeleteNode LNode = new DeleteNode();
					LNode.IsBreakable = false;
					APlan.Symbols.Push(new Symbol(AReference.SourceTable.DataType.RowType));
					try
					{
						LNode.Nodes.Add(EmitRestrictNode(APlan, ASourceNode, CompileExpression(APlan, BuildKeyEqualExpression(APlan, "", "AOldRow", AReference.SourceKey.Columns, AReference.TargetKey.Columns))));
					}
					finally
					{
						APlan.Symbols.Pop();
					}

					LNode.DetermineDataType(APlan);
					LNode.DetermineCharacteristics(APlan);
					LNode = (DeleteNode)BindNode(APlan, LNode);
					return LNode;
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		// Constructs an update statement used to set references to the target table to the given expression and verify that the given values satisfy the reference constraint
		// if NewBKeys <> OldBKeys then
		// begin
		//	var ARow : row{typeof(TargetTable)} := row{AValues};
		//	if IsNil(AValues) or IsSpecial(AValues) or exists(B where BKeys = AValues) then
		//		update A set { AKeys = AValues } where AKeys = OldBValues 
		//	else
		//		raise Error(InsertConstraintViolation)
		// end
		public static PlanNode CompileUpdateNodeForReference(Plan APlan, PlanNode ASourceNode, Schema.Reference AReference, PlanNode[] AExpressionNodes, bool AIsUpdate)
		{
			BlockNode LBlockNode = new BlockNode();
			LBlockNode.IsBreakable = false;
			VariableNode LVarNode = new VariableNode();
			LVarNode.IsBreakable = false;
			RowSelectorNode LRowNode = new RowSelectorNode(new Schema.RowType(AReference.SourceKey.Columns));
			for (int LIndex = 0; LIndex < LRowNode.DataType.Columns.Count; LIndex++)
				LRowNode.Nodes.Add(AExpressionNodes[LIndex]);
			LVarNode.Nodes.Add(BindNode(APlan, LRowNode));
			LVarNode.VariableName = "ARow";
			LVarNode.VariableType = LRowNode.DataType;
			LBlockNode.Nodes.Add(LVarNode); // Do not bind the varnode, it is unnecessary and causes ARow to appear on the stack twice.
			IfNode LIfNode = new IfNode();
			LIfNode.IsBreakable = false;
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				try
				{
					if (AIsUpdate)
						APlan.Symbols.Push(new Symbol("ANewRow", AReference.TargetTable.DataType.RowType));
					try
					{
						APlan.Symbols.Push(new Symbol(LVarNode.VariableName, LVarNode.VariableType));
						try
						{
							LIfNode.Nodes.Add
							(
								Bind
								(
									APlan,
									AppendNode
									(
										APlan,
										EmitKeyIsNilNode(APlan, "ARow", LRowNode.DataType.Columns),
										Instructions.Or,
										AppendNode
										(
											APlan,
											EmitKeyIsSpecialNode(APlan, "ARow", LRowNode.DataType.Columns),
											Instructions.Or,
											EmitUnaryNode
											(
												APlan, 
												Instructions.Exists, 
												EmitRestrictNode
												(
													APlan,
													EmitTableVarNode(APlan, AReference.TargetTable), 
													BuildKeyEqualExpression
													(
														APlan,
														"",
														"ARow",
														AReference.TargetKey.Columns,
														AReference.SourceKey.Columns // The row variable is declared using the source key column names
													)
												)
											)
										)
									)
								)
							);

							LIfNode.Nodes.Add(CompileSetNodeForReference(APlan, ASourceNode, AReference, AExpressionNodes));

							RaiseNode LRaiseNode = new RaiseNode();
							LRaiseNode.IsBreakable = false;
							LRaiseNode.Nodes.Add(EmitUnaryNode(APlan, "System.Error", new ValueNode(APlan.DataTypes.SystemString, new RuntimeException(RuntimeException.Codes.InsertConstraintViolation, AReference.Name, AReference.TargetTable.Name, String.Empty).Message))); // Schema.Constraint.GetViolationMessage(AReference.MetaData)).Message))));
							LIfNode.Nodes.Add(BindNode(APlan, LRaiseNode));
							LIfNode.DetermineDevice(APlan);
							LBlockNode.Nodes.Add(LIfNode);
							LBlockNode.Nodes.Add(BindNode(APlan, new DropVariableNode()));
							if (AIsUpdate)
							{
								LIfNode = new IfNode();
								LIfNode.IsBreakable = false;
								LIfNode.Nodes.Add(BindNode(APlan, CompileExpression(APlan, new UnaryExpression(Instructions.Not, BuildKeyEqualExpression(APlan, "AOldRow", "ANewRow", AReference.TargetKey.Columns, AReference.TargetKey.Columns)))));
								LIfNode.Nodes.Add(LBlockNode);
								return LIfNode;
							}
							else
								return LBlockNode;
						}
						finally
						{
							// APlan.Symbols.Pop(); // This pop is done by the Bind of the DropVariableNode above
						}
					}
					finally
					{
						if (AIsUpdate)
							APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		//	update A set { AKeys = AValues } where AKeys = OldBValues 
		public static UpdateNode CompileSetNodeForReference(Plan APlan, PlanNode ASourceNode, Schema.Reference AReference, PlanNode[] AExpressionNodes)
		{
			APlan.EnterRowContext();
			try
			{
				UpdateNode LNode = new UpdateNode();
				LNode.IsBreakable = false;
				APlan.Symbols.Push(new Symbol(((Schema.ITableType)ASourceNode.DataType).RowType));
				try
				{
					LNode.Nodes.Add
					(
						EmitUpdateConditionNode
						(
							APlan, 
							ASourceNode, 
							CompileExpression
							(
								APlan, 
								BuildKeyEqualExpression
								(
									APlan, 
									"",
									"AOldRow",
									AReference.SourceKey.Columns,
									AReference.TargetKey.Columns
								)
							)
						)
					);
					LNode.TargetNode = LNode.Nodes[0].Nodes[0];
					LNode.ConditionNode = LNode.Nodes[0].Nodes[1];

					for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
					{
						Schema.TableVarColumn LSourceColumn = AReference.SourceKey.Columns[LIndex];
						Schema.TableVarColumn LTargetColumn = AReference.TargetKey.Columns[LIndex];
						LNode.Nodes.Add
						(
							new UpdateColumnNode
							(
								LSourceColumn.DataType,
								LSourceColumn.Name,
								AExpressionNodes[LIndex]
							)
						);
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}

				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				LNode = (UpdateNode)BindNode(APlan, OptimizeNode(APlan, LNode));
				return LNode;
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}

		// Constructs an update statement used to set references to the target table to the given expression.
		// Used by the Clear and Set Reference Actions.
		//
		// on update of B ->
		//		update A where AKeys = OldBValues set AKeys = AExpressionNode
		public static UpdateNode CompileUpdateSetNodeForReference(Plan APlan, PlanNode ASourceNode, Schema.Reference AReference, PlanNode[] AExpressionNodes)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				try
				{
					APlan.Symbols.Push(new Symbol("ANewRow", AReference.TargetTable.DataType.RowType));
					try
					{
						return CompileSetNodeForReference(APlan, ASourceNode, AReference, AExpressionNodes);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public static UpdateNode CompileDeleteSetNodeForReference(Plan APlan, PlanNode ASourceNode, Schema.Reference AReference, PlanNode[] AExpressionNodes)
		{
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				try
				{
					return CompileSetNodeForReference(APlan, ASourceNode, AReference, AExpressionNodes);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		protected static RenameColumnExpressions CompileRenameColumnExpressionsForReference(Plan APlan, Schema.Reference AReference)
		{
			RenameColumnExpressions LResult = new RenameColumnExpressions();
			for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
				LResult.Add(new RenameColumnExpression(AReference.SourceKey.Columns[LIndex].Name, AReference.TargetKey.Columns[LIndex].Name));
			return LResult;
		}

		// Constructs a catalog constraint to enforce the key
		//
		// Given a table T { <keys>, <nonkeys>, key { <keys> } }, constructs a constraint
		//	Count(T over { <keys> }) = Count(T)
		// For a sparse key: 
		//  Count(T where <keys>.IsNotNil() { <keys> }) = Count(T where <keys>.IsNotNil())
		public static Schema.CatalogConstraint CompileCatalogConstraintForKey(Plan APlan, Schema.TableVar ATableVar, Schema.Key AKey)
		{
			Schema.CatalogConstraint LConstraint = new Schema.CatalogConstraint(Schema.Object.Qualify(AKey.Name, ATableVar.Name));
			LConstraint.Owner = ATableVar.Owner;
			LConstraint.Library = ATableVar.Library;
			LConstraint.MergeMetaData(AKey.MetaData);
			LConstraint.ConstraintType = Schema.ConstraintType.Database;
			LConstraint.IsDeferred = false;
			LConstraint.IsGenerated = true;
			
			LConstraint.Node = 
				AKey.IsSparse 
					?
						CompileBooleanExpression
						(
							APlan, 
							new BinaryExpression
							(
								new CallExpression
								(
									"System.Count", 
									new Expression[]
									{
										new ProjectExpression
										(
											new RestrictExpression
											(
												new IdentifierExpression(ATableVar.Name),
												BuildKeyIsNotNilExpression(AKey.Columns)
											),
											AKey.Columns.ColumnNames
										)
									}
								), 
								Instructions.Equal, 
								new CallExpression
								(
									"System.Count", 
									new Expression[]
									{
										new RestrictExpression
										(
											new IdentifierExpression(ATableVar.Name),
											BuildKeyIsNotNilExpression(AKey.Columns)
										)
									}
								)
							)
						)
					:
						CompileBooleanExpression
						(
							APlan, 
							new BinaryExpression
							(
								new CallExpression
								(
									"System.Count", 
									new Expression[]
									{
										new ProjectExpression
										(
											new IdentifierExpression(ATableVar.Name), 
											AKey.Columns.ColumnNames
										)
									}
								), 
								Instructions.Equal, 
								new CallExpression
								(
									"System.Count", 
									new Expression[]{new IdentifierExpression(ATableVar.Name)}
								)
							)
						);
				
			LConstraint.Node = OptimizeNode(APlan, LConstraint.Node);
			LConstraint.Node = BindNode(APlan, LConstraint.Node);
			return LConstraint;
		}
		
		// Constructs a catalog constraint to enforce the reference
		//
		// not(exists((source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) over {keys} rename {target keys}) minus (target over {keys})))
		// equivalent formulation to facilitate translation into SQL
		// not(exists(source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) and not(exists(target where target.keys = source.keys))))
		public static Schema.CatalogConstraint CompileCatalogConstraintForReference(Plan APlan, Schema.Reference AReference)
		{
			Schema.CatalogConstraint LConstraint = new Schema.CatalogConstraint(Schema.Object.Qualify(AReference.Name, Keywords.Reference));
			LConstraint.Owner = APlan.User;
			LConstraint.Library = AReference.Library;
			LConstraint.MergeMetaData(AReference.MetaData);
			LConstraint.ConstraintType = Schema.ConstraintType.Database;
			LConstraint.IsGenerated = true;
//			if (AReference.IsSessionObject)
//				LConstraint.SessionObjectName = LConstraint.Name; // This is to allow the constraint to reference session-specific objects if necessary
//			APlan.PushCreationObject(LConstraint);
//			try
//			{
				PlanNode LSourceNode = EmitIdentifierNode(APlan, AReference.SourceTable.Name);
				PlanNode LTargetNode = EmitIdentifierNode(APlan, AReference.TargetTable.Name);

				#if UseMinusForReferenes
				// not(exists((source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) over {keys} rename {target keys}) minus (target over {keys})))
				LConstraint.Node =
					EmitUnaryNode
					(
						APlan, 
						Instructions.Not,
						EmitUnaryNode
						(
							APlan, 
							Instructions.Exists,
							EmitDifferenceNode
							(
								APlan, 
								EmitRenameNode
								(
									APlan,
									EmitProjectNode
									(
										APlan, 
										EmitRestrictNode
										(
											APlan,
											ASourceNode,
											AppendNode
											(
												APlan,
												EmitKeyIsNotNilNode(APlan, AReference.SourceKey.Columns)
												Instructions.And,
												EmitKeyIsNotSpecialNode(APlan, AReference.SourceKey.Columns)
											),
											AReference.SourceKey
										),
									),
									CompileRenameColumnExpressionsForReference(APlan, AReference)
								),
								EmitProjectNode
								(
									APlan, 
									ATargetNode,
									AReference.TargetKey
								)
							)
						)
					);
				#else
				// not(exists(source rename source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) and not(exists(target rename target where target.keys = source.keys))))
				Schema.Columns LSourceKeyColumns = new Schema.Columns();
				foreach (Schema.TableVarColumn LSourceKeyColumn in AReference.SourceKey.Columns)
					LSourceKeyColumns.Add(new Schema.Column(Schema.Object.Qualify(LSourceKeyColumn.Name, Keywords.Source), LSourceKeyColumn.DataType));
				APlan.EnterRowContext();
				try
				{
					APlan.Symbols.Push(new Symbol(((TableNode)LSourceNode).DataType.CreateRowType(Keywords.Source)));
					try
					{
						APlan.Symbols.Push(new Symbol(((TableNode)LTargetNode).DataType.CreateRowType(Keywords.Target)));
 						try
						{
							BitArray LIsNilable = new BitArray(AReference.SourceKey.Columns.Count);
							BitArray LHasSpecials = new BitArray(AReference.SourceKey.Columns.Count);
							for (int LIndex = 0; LIndex < AReference.SourceKey.Columns.Count; LIndex++)
							{
								LIsNilable[LIndex] = AReference.SourceKey.Columns[LIndex].IsNilable;
								LHasSpecials[LIndex] = 
									(AReference.SourceKey.Columns[LIndex].DataType is Schema.ScalarType) 
										&& (((Schema.ScalarType)AReference.SourceKey.Columns[LIndex].DataType).Specials.Count > 0);
							}
							
							LConstraint.Node =
								EmitUnaryNode
								(
									APlan,
									Instructions.Not,
									EmitUnaryNode
									(
										APlan,
										Instructions.Exists,
										EmitRestrictNode
										(
											APlan,
											EmitRenameNode(APlan, LSourceNode, Keywords.Source, null),
											AppendNode
											(
												APlan,
												EmitKeyIsNotNilNode(APlan, LSourceKeyColumns, LIsNilable),
												Instructions.And,
												AppendNode
												(
													APlan,
													EmitKeyIsNotSpecialNode(APlan, LSourceKeyColumns, LHasSpecials),
													Instructions.And,
													EmitUnaryNode
													(
														APlan,
														Instructions.Not,
														EmitUnaryNode
														(
															APlan,
															Instructions.Exists,
															EmitRestrictNode
															(
																APlan,
																EmitRenameNode(APlan, LTargetNode, Keywords.Target, null),
																CompileExpression
																(
																	APlan,
																	BuildKeyEqualExpression
																	(
																		APlan,
																		new Schema.RowType(AReference.TargetKey.Columns, Keywords.Target).Columns,
																		new Schema.RowType(AReference.SourceKey.Columns, Keywords.Source).Columns
																	)
																)
															)
														)
													)
												)
											)
										)
									)
								);

							LConstraint.Node = BindNode(APlan, LConstraint.Node);
							return LConstraint;
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.ExitRowContext();
				}
				#endif
//			}
//			finally
//			{
//				APlan.PopCreationObject();
//			}
		}
		
		protected static void AddSpecialsForScalarType(Schema.ScalarType AScalarType, Schema.Objects ASpecials)
		{
			ASpecials.AddRange(AScalarType.Specials);
			#if USETYPEINHERITANCE
			foreach (Schema.ScalarType LParentType in AScalarType.ParentTypes)
				AddSpecialsForScalarType(LParentType, ASpecials);
			#endif
		}
		
		/// <summary>Returns the default special for the given type. Will throw an exception if the type is not scalar, or the type has multiple specials defined. Will return null if the type has no specials defined.</summary>
		protected static Schema.Special FindDefaultSpecialForScalarType(Plan APlan, Schema.IDataType ADataType)
		{
			if (!(ADataType is Schema.ScalarType))
				throw new CompilerException(CompilerException.Codes.UnableToFindDefaultSpecial, APlan.CurrentStatement(), ADataType.Name);
			Schema.ScalarType LScalarType = (Schema.ScalarType)ADataType;
			Schema.Objects LSpecials = new Schema.Objects();
			AddSpecialsForScalarType(LScalarType, LSpecials);
			switch (LSpecials.Count)
			{
				case 0 : return null;
				case 1 : return (Schema.Special)LSpecials[0];
				default : throw new CompilerException(CompilerException.Codes.AmbiguousClearValue, APlan.CurrentStatement(), LScalarType.Name);
			}
		}
		
		public static PlanNode EmitNilNode(Plan APlan)
		{
			return CompileValueExpression(APlan, new ValueExpression(null, TokenType.Nil));
		}
		
		public static PlanNode CompileCreateReferenceStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateReferenceStatement LStatement = (CreateReferenceStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateReference);
			APlan.Symbols.PushWindow(0); // Make sure the create reference statement is evaluated in a private context
			try
			{
				CreateReferenceNode LNode = new CreateReferenceNode();
				string LReferenceName = Schema.Object.Qualify(LStatement.ReferenceName, APlan.CurrentLibrary.Name);
				string LSessionReferenceName = null;
				if (LStatement.IsSession)
				{
					LSessionReferenceName = LReferenceName;
					if (APlan.IsRepository)
						LReferenceName = MetaData.GetTag(LStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						LReferenceName = Schema.Object.NameFromGuid(Guid.NewGuid());
					CheckValidSessionObjectName(APlan, AStatement, LSessionReferenceName);
					APlan.PlanSessionObjects.Add(new Schema.SessionObject(LSessionReferenceName, LReferenceName));
				} 
				
				CheckValidCatalogObjectName(APlan, AStatement, LReferenceName);

				LNode.Reference = new Schema.Reference(Schema.Object.GetObjectID(LStatement.MetaData), LReferenceName);
				LNode.Reference.SessionObjectName = LSessionReferenceName;
				LNode.Reference.SessionID = APlan.SessionID;
				LNode.Reference.IsGenerated = LStatement.IsSession;
				LNode.Reference.Owner = APlan.User;
				LNode.Reference.Library = LNode.Reference.IsGenerated ? null : APlan.CurrentLibrary;
				LNode.Reference.MetaData = LStatement.MetaData;
				LNode.Reference.Enforced = GetEnforced(APlan, LNode.Reference.MetaData);
				APlan.PlanCatalog.Add(LNode.Reference);
				try
				{
					APlan.PushCreationObject(LNode.Reference);
					try
					{
						Schema.Object LSchemaObject = ResolveCatalogIdentifier(APlan, LStatement.TableVarName, true);
						if (!(LSchemaObject is Schema.TableVar))
							throw new CompilerException(CompilerException.Codes.InvalidReferenceObject, AStatement, LStatement.ReferenceName, LStatement.TableVarName);
						LNode.Reference.SourceTable = (Schema.TableVar)LSchemaObject;
						APlan.AttachDependency(LSchemaObject);
						foreach (ReferenceColumnDefinition LColumn in LStatement.Columns)
							LNode.Reference.SourceKey.Columns.Add(LNode.Reference.SourceTable.Columns[LColumn.ColumnName]);
						foreach (Schema.Key LKey in LNode.Reference.SourceTable.Keys)
							if (LNode.Reference.SourceKey.Columns.IsSupersetOf(LKey.Columns))
							{
								LNode.Reference.SourceKey.IsUnique = true;
								break;
							}

						LSchemaObject = ResolveCatalogIdentifier(APlan, LStatement.ReferencesDefinition.TableVarName, true);
						if (!(LSchemaObject is Schema.TableVar))
							throw new CompilerException(CompilerException.Codes.InvalidReferenceObject, AStatement, LStatement.ReferenceName, LStatement.ReferencesDefinition.TableVarName);
						LNode.Reference.TargetTable = (Schema.TableVar)LSchemaObject;
						APlan.AttachDependency(LSchemaObject);
						foreach (ReferenceColumnDefinition LColumn in LStatement.ReferencesDefinition.Columns)
							LNode.Reference.TargetKey.Columns.Add(LNode.Reference.TargetTable.Columns[LColumn.ColumnName]);
						foreach (Schema.Key LKey in LNode.Reference.TargetTable.Keys)
							if (LNode.Reference.TargetKey.Columns.IsSupersetOf(LKey.Columns))
							{
								LNode.Reference.TargetKey.IsUnique = true;
								break;
							}
							
						if (!LNode.Reference.TargetKey.IsUnique)
							throw new CompilerException(CompilerException.Codes.ReferenceMustTargetKey, AStatement, LStatement.ReferenceName, LStatement.ReferencesDefinition.TableVarName);
							
						if (LNode.Reference.SourceKey.Columns.Count != LNode.Reference.TargetKey.Columns.Count)
							throw new CompilerException(CompilerException.Codes.InvalidReferenceColumnCount, AStatement, LStatement.ReferenceName);
						
						#if REQUIRESAMEDATATYPESFORREFERENCECOLUMNS	
						for (int LIndex = 0; LIndex < LNode.Reference.SourceKey.Columns.Count; LIndex++)
							if (!LNode.Reference.SourceKey.Columns[LIndex].DataType.Is(LNode.Reference.TargetKey.Columns[LIndex].DataType))
								throw new CompilerException(CompilerException.Codes.InvalidReferenceColumn, AStatement, LStatement.ReferenceName, LNode.Reference.SourceKey.Columns[LIndex].Name, LNode.Reference.SourceKey.Columns[LIndex].DataType.Name, LNode.Reference.TargetKey.Columns[LIndex].Name, LNode.Reference.TargetKey.Columns[LIndex].DataType.Name, LNode.Reference.TargetTable.DisplayName);
						#endif
								
						LNode.Reference.UpdateReferenceAction = LStatement.ReferencesDefinition.UpdateReferenceAction;
						LNode.Reference.DeleteReferenceAction = LStatement.ReferencesDefinition.DeleteReferenceAction;
						
						if (!APlan.IsRepository && LNode.Reference.Enforced)
						{
							if ((LNode.Reference.SourceTable is Schema.BaseTableVar) && (LNode.Reference.TargetTable is Schema.BaseTableVar))
							{
								// Construct Insert/Update/Delete constraints to enforce the reference for the source and target tables
								// These nodes are attached to the tablevars involved during the runtime execution of the CreateReferenceNode
								LNode.Reference.SourceConstraint = CompileSourceReferenceConstraint(APlan, LNode.Reference);
								LNode.Reference.TargetConstraint = CompileTargetReferenceConstraint(APlan, LNode.Reference);
							}
							else
								// References are not enforced by default if they are sourced in or target derived table variables
								LNode.Reference.Enforced = GetEnforced(APlan, LNode.Reference.MetaData, false);

							// This constraint will only need to be validated for inserts and updates and deletes when the action is require
							// A catalog level enforcement constraint is always compiled to allow for initial creation validation.
							// This node must evaluate to true before the constraint can be created.
							LNode.Reference.CatalogConstraint = 
								CompileCatalogConstraintForReference
								(
									APlan, 
									LNode.Reference
								);

							// Construct UpdateReferenceAction nodes if necessary			
							PlanNode[] LExpressionNodes;
							PlanNode LSourceNode;
							switch (LNode.Reference.UpdateReferenceAction)
							{
								case ReferenceAction.Cascade:
									LNode.Reference.UpdateHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", LNode.Reference.TargetTable.Name, "AfterUpdate", LNode.Reference.Name, "UpdateHandler"));
									LNode.Reference.UpdateHandler.Owner = LNode.Reference.Owner;
									LNode.Reference.UpdateHandler.Library = LNode.Reference.Library;
									LNode.Reference.UpdateHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", LNode.Reference.Name, "UpdateHandler"));
									LNode.Reference.UpdateHandler.EventType = EventType.AfterUpdate;
									LNode.Reference.UpdateHandler.IsGenerated = true;
									LNode.Reference.UpdateHandler.Operator.IsGenerated = true;
									LNode.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.UpdateHandler.Operator, "AOldRow", new Schema.RowType(true)));
									LNode.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.UpdateHandler.Operator, "ANewRow", new Schema.RowType(true)));
									LNode.Reference.UpdateHandler.Operator.Owner = LNode.Reference.Owner;
									LNode.Reference.UpdateHandler.Operator.Library = LNode.Reference.Library;
									LNode.Reference.UpdateHandler.Operator.SessionObjectName = LNode.Reference.UpdateHandler.Operator.OperatorName;
									LNode.Reference.UpdateHandler.Operator.SessionID = APlan.SessionID;
									APlan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										LSourceNode = EmitIdentifierNode(APlan, LNode.Reference.SourceTable.Name);
									}
									finally
									{
										APlan.PopStatementContext();
									}
									LNode.Reference.UpdateHandler.Operator.Block.BlockNode = CompileUpdateCascadeNodeForReference(APlan, LSourceNode, LNode.Reference);
									LNode.Reference.UpdateHandler.PlanNode = LNode.Reference.UpdateHandler.Operator.Block.BlockNode;
								break;
								
								case ReferenceAction.Clear:
									LExpressionNodes = new PlanNode[LNode.Reference.SourceKey.Columns.Count];
									for (int LIndex = 0; LIndex < LNode.Reference.SourceKey.Columns.Count; LIndex++)
									{
										Schema.ScalarType LScalarType = LNode.Reference.SourceKey.Columns[LIndex].DataType as Schema.ScalarType;
										if (LScalarType != null)
										{
											Schema.Special LSpecial = Compiler.FindDefaultSpecialForScalarType(APlan, LScalarType);
											if (LSpecial != null)
												LExpressionNodes[LIndex] = EmitCallNode(APlan, LSpecial.Selector.OperatorName, new PlanNode[]{});
										}
										
										if (LExpressionNodes[LIndex] == null)
										{
											if (!LNode.Reference.SourceKey.Columns[LIndex].IsNilable)
												throw new CompilerException(CompilerException.Codes.CannotClearSourceColumn, AStatement, LNode.Reference.SourceKey.Columns[LIndex].Name);
											LExpressionNodes[LIndex] = EmitNilNode(APlan);
										}
									}

									LNode.Reference.UpdateHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", LNode.Reference.TargetTable.Name, "AfterUpdate", LNode.Reference.Name, "UpdateHandler"));
									LNode.Reference.UpdateHandler.Owner = LNode.Reference.Owner;
									LNode.Reference.UpdateHandler.Library = LNode.Reference.Library;
									LNode.Reference.UpdateHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", LNode.Reference.Name, "UpdateHandler"));
									LNode.Reference.UpdateHandler.EventType = EventType.AfterUpdate;
									LNode.Reference.UpdateHandler.IsGenerated = true;
									LNode.Reference.UpdateHandler.Operator.IsGenerated = true;
									LNode.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.UpdateHandler.Operator, "AOldRow", new Schema.RowType(true)));
									LNode.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.UpdateHandler.Operator, "ANewRow", new Schema.RowType(true)));
									LNode.Reference.UpdateHandler.Operator.Owner = LNode.Reference.Owner;
									LNode.Reference.UpdateHandler.Operator.Library = LNode.Reference.Library;
									LNode.Reference.UpdateHandler.Operator.SessionObjectName = LNode.Reference.UpdateHandler.Operator.OperatorName;
									LNode.Reference.UpdateHandler.Operator.SessionID = APlan.SessionID;
									APlan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										LSourceNode = EmitIdentifierNode(APlan, LNode.Reference.SourceTable.Name);
									}
									finally
									{
										APlan.PopStatementContext();
									}
									LNode.Reference.UpdateHandler.Operator.Block.BlockNode = CompileUpdateSetNodeForReference(APlan, LSourceNode, LNode.Reference, LExpressionNodes);
									LNode.Reference.UpdateHandler.PlanNode = LNode.Reference.UpdateHandler.Operator.Block.BlockNode;
								break;

								case ReferenceAction.Set:
									LNode.Reference.UpdateReferenceExpressions.AddRange(LStatement.ReferencesDefinition.UpdateReferenceExpressions);
									LExpressionNodes = new PlanNode[LNode.Reference.SourceKey.Columns.Count];
									for (int LIndex = 0; LIndex < LStatement.ReferencesDefinition.UpdateReferenceExpressions.Count; LIndex++)
										LExpressionNodes[LIndex] = CompileExpression(APlan, LStatement.ReferencesDefinition.UpdateReferenceExpressions[LIndex]);

									LNode.Reference.UpdateHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", LNode.Reference.TargetTable.Name, "AfterUpdate", LNode.Reference.Name, "UpdateHandler"));
									LNode.Reference.UpdateHandler.Owner = LNode.Reference.Owner;
									LNode.Reference.UpdateHandler.Library = LNode.Reference.Library;
									LNode.Reference.UpdateHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", LNode.Reference.Name, "UpdateHandler"));
									LNode.Reference.UpdateHandler.EventType = EventType.AfterUpdate;
									LNode.Reference.UpdateHandler.IsGenerated = true;
									LNode.Reference.UpdateHandler.Operator.IsGenerated = true;
									LNode.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.UpdateHandler.Operator, "AOldRow", new Schema.RowType(true)));
									LNode.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.UpdateHandler.Operator, "ANewRow", new Schema.RowType(true)));
									LNode.Reference.UpdateHandler.Operator.Owner = LNode.Reference.Owner;
									LNode.Reference.UpdateHandler.Operator.Library = LNode.Reference.Library;
									LNode.Reference.UpdateHandler.Operator.SessionObjectName = LNode.Reference.UpdateHandler.Operator.OperatorName;
									LNode.Reference.UpdateHandler.Operator.SessionID = APlan.SessionID;
									APlan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										LSourceNode = EmitIdentifierNode(APlan, LNode.Reference.SourceTable.Name);
									}
									finally
									{
										APlan.PopStatementContext();
									}
									LNode.Reference.UpdateHandler.Operator.Block.BlockNode =
										CompileUpdateNodeForReference
										(
											APlan, 
											LSourceNode,
											LNode.Reference,
											LExpressionNodes,
											true // IsUpdate
										);
									LNode.Reference.UpdateHandler.PlanNode = LNode.Reference.UpdateHandler.Operator.Block.BlockNode;
								break;
							}

							// Construct DeleteReferenceAction nodes if necessary			
							switch (LNode.Reference.DeleteReferenceAction)
							{
								case ReferenceAction.Cascade:
									LNode.Reference.DeleteHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", LNode.Reference.TargetTable.Name, "AfterDelete", LNode.Reference.Name, "DeleteHandler"));
									LNode.Reference.DeleteHandler.Owner = LNode.Reference.Owner;
									LNode.Reference.DeleteHandler.Library = LNode.Reference.Library;
									LNode.Reference.DeleteHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", LNode.Reference.Name, "DeleteHandler"));
									LNode.Reference.DeleteHandler.EventType = EventType.AfterDelete;
									LNode.Reference.DeleteHandler.IsGenerated = true;
									LNode.Reference.DeleteHandler.Operator.IsGenerated = true;
									LNode.Reference.DeleteHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.DeleteHandler.Operator, "ARow", new Schema.RowType(true)));
									LNode.Reference.DeleteHandler.Operator.Owner = LNode.Reference.Owner;
									LNode.Reference.DeleteHandler.Operator.Library = LNode.Reference.Library;
									LNode.Reference.DeleteHandler.Operator.SessionObjectName = LNode.Reference.DeleteHandler.Operator.OperatorName;
									LNode.Reference.DeleteHandler.Operator.SessionID = APlan.SessionID;
									APlan.PushStatementContext(new StatementContext(StatementType.Delete));
									try
									{
										LSourceNode = EmitIdentifierNode(APlan, LNode.Reference.SourceTable.Name);
									}
									finally
									{
										APlan.PopStatementContext();
									}
									LNode.Reference.DeleteHandler.Operator.Block.BlockNode =
										CompileDeleteCascadeNodeForReference
										(
											APlan, 
											LSourceNode,
											LNode.Reference
										);
									LNode.Reference.DeleteHandler.PlanNode = LNode.Reference.DeleteHandler.Operator.Block.BlockNode;
								break;

								case ReferenceAction.Clear:
									LExpressionNodes = new PlanNode[LNode.Reference.SourceKey.Columns.Count];
									for (int LIndex = 0; LIndex < LNode.Reference.SourceKey.Columns.Count; LIndex++)
									{
										Schema.ScalarType LScalarType = LNode.Reference.SourceKey.Columns[LIndex].DataType as Schema.ScalarType;
										if (LScalarType != null)
										{
											Schema.Special LSpecial = Compiler.FindDefaultSpecialForScalarType(APlan, LScalarType);
											if (LSpecial != null)
												LExpressionNodes[LIndex] = EmitCallNode(APlan, LSpecial.Selector.OperatorName, new PlanNode[]{});
										}
										
										if (LExpressionNodes[LIndex] == null)
										{
											if (!LNode.Reference.SourceKey.Columns[LIndex].IsNilable)
												throw new CompilerException(CompilerException.Codes.CannotClearSourceColumn, AStatement, LNode.Reference.SourceKey.Columns[LIndex].Name);
											LExpressionNodes[LIndex] = EmitNilNode(APlan);
										}
									}

									LNode.Reference.DeleteHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", LNode.Reference.TargetTable.Name, "AfterDelete", LNode.Reference.Name, "DeleteHandler"));
									LNode.Reference.DeleteHandler.Owner = LNode.Reference.Owner;
									LNode.Reference.DeleteHandler.Library = LNode.Reference.Library;
									LNode.Reference.DeleteHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", LNode.Reference.Name, "DeleteHandler"));
									LNode.Reference.DeleteHandler.EventType = EventType.AfterDelete;
									LNode.Reference.DeleteHandler.IsGenerated = true;
									LNode.Reference.DeleteHandler.Operator.IsGenerated = true;
									LNode.Reference.DeleteHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.DeleteHandler.Operator, "ARow", new Schema.RowType(true)));
									LNode.Reference.DeleteHandler.Operator.Owner = LNode.Reference.Owner;
									LNode.Reference.DeleteHandler.Operator.Library = LNode.Reference.Library;
									LNode.Reference.DeleteHandler.Operator.SessionObjectName = LNode.Reference.DeleteHandler.Operator.OperatorName;
									LNode.Reference.DeleteHandler.Operator.SessionID = APlan.SessionID;
									APlan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										LSourceNode = EmitIdentifierNode(APlan, LNode.Reference.SourceTable.Name);
									}
									finally
									{
										APlan.PopStatementContext();
									}
									LNode.Reference.DeleteHandler.Operator.Block.BlockNode =
										CompileDeleteSetNodeForReference
										(
											APlan, 
											LSourceNode,
											LNode.Reference,
											LExpressionNodes
										);
									LNode.Reference.DeleteHandler.PlanNode = LNode.Reference.DeleteHandler.Operator.Block.BlockNode;
								break;

								case ReferenceAction.Set:
									LNode.Reference.DeleteReferenceExpressions.AddRange(LStatement.ReferencesDefinition.DeleteReferenceExpressions);
									LExpressionNodes = new PlanNode[LNode.Reference.SourceKey.Columns.Count];
									for (int LIndex = 0; LIndex < LStatement.ReferencesDefinition.DeleteReferenceExpressions.Count; LIndex++)
										LExpressionNodes[LIndex] = CompileExpression(APlan, LStatement.ReferencesDefinition.DeleteReferenceExpressions[LIndex]);
									
									LNode.Reference.DeleteHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", LNode.Reference.TargetTable.Name, "AfterDelete", LNode.Reference.Name, "DeleteHandler"));
									LNode.Reference.DeleteHandler.Owner = LNode.Reference.Owner;
									LNode.Reference.DeleteHandler.Library = LNode.Reference.Library;
									LNode.Reference.DeleteHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", LNode.Reference.Name, "DeleteHandler"));
									LNode.Reference.DeleteHandler.EventType = EventType.AfterDelete;
									LNode.Reference.DeleteHandler.IsGenerated = true;
									LNode.Reference.DeleteHandler.Operator.IsGenerated = true;
									LNode.Reference.DeleteHandler.Operator.Operands.Add(new Schema.Operand(LNode.Reference.DeleteHandler.Operator, "ARow", new Schema.RowType(true)));
									LNode.Reference.DeleteHandler.Operator.Owner = LNode.Reference.Owner;
									LNode.Reference.DeleteHandler.Operator.Library = LNode.Reference.Library;
									LNode.Reference.DeleteHandler.Operator.SessionObjectName = LNode.Reference.DeleteHandler.Operator.OperatorName;
									LNode.Reference.DeleteHandler.Operator.SessionID = APlan.SessionID;
									APlan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										LSourceNode = EmitIdentifierNode(APlan, LNode.Reference.SourceTable.Name);
									}
									finally
									{
										APlan.PopStatementContext();
									}
									LNode.Reference.DeleteHandler.Operator.Block.BlockNode =
										CompileUpdateNodeForReference
										(
											APlan,
											LSourceNode,
											LNode.Reference,
											LExpressionNodes,
											false // IsUpdate
										);
									LNode.Reference.DeleteHandler.PlanNode = LNode.Reference.DeleteHandler.Operator.Block.BlockNode;
								break;
							}

/*
 BTR 5/14/2006 -> This will not be necessary to check any longer, it is indeed the case that this is an error condition, but it would be
indicative of other problems, a reference will never be attached as an explicit dependency of a reference.
							// This should be an error condition as I cannot manufacture a case where a reference would be a dependency of a reference
							// Reference inference used to attach references as dependencies of references to get views to serialize, but it no longer does
							// because view serialization is done much more correctly now, and I think this is a by-product of that hack. BTR
							if (LNode.Reference.HasDependencies())
								for (int LIndex = 0; LIndex < LNode.Reference.Dependencies.Count; LIndex++)
								{
									Schema.Object LObject = LNode.Reference.Dependencies.Objects[LIndex];
									if ((LObject is Schema.Reference) || ((LObject == null) && APlan.Catalog.GetObjectHeaderByID(LNode.Reference.Dependencies.IDs[LIndex]).ObjectType == "Reference"))
										Error.Fail("Invalid reference dependency");
								}
*/
							
							#if REMOVEREFERENCEDEPENDENCIES
							// Remove references which are dependencies of this reference, they are not necessary and screw up reference inclusion for derivation
							for (int LIndex = LNode.Reference.Dependencies.Count - 1; LIndex >= 0; LIndex--)
								if (LNode.Reference.Dependencies[LIndex] is Schema.Reference)
									LNode.Reference.Dependencies.RemoveAt(LIndex);
							#endif
						}
						
						return LNode;
					}
					finally
					{
						APlan.PopCreationObject();
					}
				}
				catch
				{
					APlan.PlanCatalog.SafeRemove(LNode.Reference);
					throw;
				}
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}
		
		public static bool CouldGenerateDeviceScalarTypeMap(Plan APlan, Schema.Device ADevice, Schema.ScalarType AScalarType)
		{
			APlan.EnsureDeviceStarted(ADevice);
			Schema.Representation LRepresentation = FindSystemRepresentation(AScalarType);
			return (LRepresentation != null) && (LRepresentation.Properties.Count == 1) && (LRepresentation.Properties[0].DataType is Schema.ScalarType) && (ADevice.ResolveDeviceScalarType(APlan, (Schema.ScalarType)LRepresentation.Properties[0].DataType) != null);
		}
		
		public static Schema.DeviceScalarType CompileDeviceScalarTypeMap(Plan APlan, Schema.Device ADevice, DeviceScalarTypeMap ADeviceScalarTypeMap)
		{
			APlan.EnsureDeviceStarted(ADevice);
			Schema.ScalarType LDataType = (Schema.ScalarType)CompileScalarTypeSpecifier(APlan, new ScalarTypeSpecifier(ADeviceScalarTypeMap.ScalarTypeName));
			
			if (!APlan.InLoadingContext())
			{
				Schema.DeviceScalarType LExistingMap = ADevice.ResolveDeviceScalarType(APlan, LDataType);
				if ((LExistingMap != null) && !LExistingMap.IsGenerated)
					throw new CompilerException(CompilerException.Codes.DuplicateDeviceScalarType, ADeviceScalarTypeMap, ADevice.Name, LDataType.Name);
			}

			Schema.DeviceScalarType LRequiredScalarType = null;			
			ClassDefinition LClassDefinition = ADeviceScalarTypeMap.ClassDefinition;
			if (LClassDefinition == null)
			{
				Schema.Representation LRepresentation = FindSystemRepresentation(LDataType);
				if ((LRepresentation != null) && (LRepresentation.Properties.Count == 1))
				{
					LRequiredScalarType = ADevice.ResolveDeviceScalarType(APlan, (Schema.ScalarType)LRepresentation.Properties[0].DataType);
					if (LRequiredScalarType != null)
						LClassDefinition = (ClassDefinition)LRequiredScalarType.ClassDefinition.Clone();
				}

				if (LClassDefinition == null)
					throw new CompilerException(CompilerException.Codes.ScalarTypeMapRequired, ADeviceScalarTypeMap, LDataType.Name);
			}
			
			if (LClassDefinition == null)
				throw new CompilerException(CompilerException.Codes.DeviceScalarTypeClassExpected, ADeviceScalarTypeMap, "null");

			if (LRequiredScalarType == null)
				APlan.CheckRight(Schema.RightNames.HostImplementation);

			int LObjectID = Schema.Object.GetObjectID(ADeviceScalarTypeMap.MetaData);
			string LDeviceScalarTypeName = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_Map", ADevice.Name, LDataType.Name), LObjectID);
			object LDeviceScalarTypeObject = APlan.Catalog.ClassLoader.CreateObject(LClassDefinition, new object[]{LObjectID, LDeviceScalarTypeName});
			if (!(LDeviceScalarTypeObject is Schema.DeviceScalarType))
				throw new CompilerException(CompilerException.Codes.DeviceScalarTypeClassExpected, LClassDefinition, LDeviceScalarTypeObject == null ? "null" : LDeviceScalarTypeObject.GetType().AssemblyQualifiedName);
				
			Schema.DeviceScalarType LDeviceScalarType = (Schema.DeviceScalarType)LDeviceScalarTypeObject;
			LDeviceScalarType.Library = LDataType.Library.IsRequiredLibrary(ADevice.Library) ? LDataType.Library : ADevice.Library;;
			LDeviceScalarType.Owner = APlan.User;
			LDeviceScalarType.IsGenerated = ADeviceScalarTypeMap.IsGenerated;
			APlan.PushCreationObject(LDeviceScalarType);
			try
			{
				APlan.CheckClassDependency(LDeviceScalarType.Library, LClassDefinition);
				LDeviceScalarType.Device = ADevice;
				APlan.AttachDependency(ADevice);
				LDeviceScalarType.ScalarType = LDataType;
				APlan.AttachDependency(LDataType);
				if (LRequiredScalarType != null)
				{
					APlan.AttachDependency(LRequiredScalarType);
					LDeviceScalarType.IsDefaultClassDefinition = true;
				}
				LDeviceScalarType.ClassDefinition = LClassDefinition;
				LDeviceScalarType.MetaData = ADeviceScalarTypeMap.MetaData;
				return LDeviceScalarType;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static Schema.DeviceOperator CompileDeviceOperatorMap(Plan APlan, Schema.Device ADevice, DeviceOperatorMap ADeviceOperatorMap)
		{
			APlan.EnsureDeviceStarted(ADevice);
			bool LIsSystemClassDefinition = false;
			if (ADeviceOperatorMap.ClassDefinition == null)
			{
				ADeviceOperatorMap.ClassDefinition = ADevice.GetDefaultOperatorClassDefinition(ADeviceOperatorMap.MetaData);
				LIsSystemClassDefinition = true;
			}
			
			if (ADeviceOperatorMap.ClassDefinition == null)
				throw new CompilerException(CompilerException.Codes.DeviceOperatorClassRequired, ADeviceOperatorMap, ADeviceOperatorMap.OperatorSpecifier.OperatorName, ADevice.Name);
				
			if (!LIsSystemClassDefinition)
				APlan.CheckRight(Schema.RightNames.HostImplementation);
				
			Schema.Operator LOperator = ResolveOperatorSpecifier(APlan, ADeviceOperatorMap.OperatorSpecifier);
			
			if (!APlan.InLoadingContext())
			{
				Schema.DeviceOperator LExistingMap = ADevice.ResolveDeviceOperator(APlan, LOperator);
				if ((LExistingMap != null) && !LExistingMap.IsGenerated)
					throw new CompilerException(CompilerException.Codes.DuplicateDeviceOperator, ADeviceOperatorMap, ADevice.Name, LOperator.OperatorName, LOperator.Signature.ToString());
			}
				
			int LObjectID = Schema.Object.GetObjectID(ADeviceOperatorMap.MetaData);
			string LDeviceOperatorName = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_Map", ADevice.Name, LOperator.Name), LObjectID);
			object LDeviceOperatorObject = APlan.Catalog.ClassLoader.CreateObject(ADeviceOperatorMap.ClassDefinition, new object[]{LObjectID, LDeviceOperatorName});
			if (!(LDeviceOperatorObject is Schema.DeviceOperator))
				throw new CompilerException(CompilerException.Codes.DeviceOperatorClassExpected, ADeviceOperatorMap.ClassDefinition, LDeviceOperatorObject == null ? "null" : LDeviceOperatorObject.GetType().AssemblyQualifiedName);
				
			Schema.DeviceOperator LDeviceOperator = (Schema.DeviceOperator)LDeviceOperatorObject;
			LDeviceOperator.Library = LOperator.Library.IsRequiredLibrary(ADevice.Library) ? LOperator.Library : ADevice.Library;
			LDeviceOperator.Owner = APlan.User;
			APlan.PushCreationObject(LDeviceOperator);
			try
			{
				LDeviceOperator.Device = ADevice;
				APlan.AttachDependency(ADevice);
				LDeviceOperator.Operator = LOperator;
				APlan.AttachDependency(LOperator);

				APlan.CheckClassDependency(LDeviceOperator.Library, ADeviceOperatorMap.ClassDefinition);
				LDeviceOperator.ClassDefinition = ADeviceOperatorMap.ClassDefinition;
				LDeviceOperator.MetaData = ADeviceOperatorMap.MetaData;
				return LDeviceOperator;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		private static void CompileImplicitDeviceOperator(Plan APlan, Schema.Device ADevice, Schema.DeviceScalarType ADeviceScalarType, Schema.DeviceScalarType ABaseDeviceScalarType, Schema.Operator AOperator, ClassDefinition AClassDefinition)
		{
			Schema.DeviceOperator LDeviceOperator = ADevice.ResolveDeviceOperator(APlan, AOperator);
			if (LDeviceOperator == null)
			{
				ADeviceScalarType.AddDependency(ABaseDeviceScalarType);
				int LObjectID = Schema.Object.GetNextObjectID();
				string LDeviceOperatorName = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_Map", ADevice.Name, AOperator.Name), LObjectID);
				LDeviceOperator = (Schema.DeviceOperator)APlan.Catalog.ClassLoader.CreateObject(AClassDefinition, new object[] { LObjectID, LDeviceOperatorName });
				LDeviceOperator.Library = ADeviceScalarType.Library;
				LDeviceOperator.Owner = ADeviceScalarType.Owner;
				LDeviceOperator.IsGenerated = true;
				LDeviceOperator.Generator = ADeviceScalarType;
				APlan.PushCreationObject(LDeviceOperator);
				try
				{
					LDeviceOperator.Operator = AOperator;
					APlan.AttachDependency(AOperator);
					LDeviceOperator.Device = ADevice;
					APlan.AttachDependency(ADevice);
					LDeviceOperator.ClassDefinition = AClassDefinition;
					APlan.CheckClassDependency(LDeviceOperator.ClassDefinition);
					APlan.CatalogDeviceSession.CreateDeviceOperator(LDeviceOperator);
				}
				finally
				{
					APlan.PopCreationObject();
				}
			}
			// BTR 8/16/2006 -> I do not understand when this would ever be the case. It may be
			// just a holdover. In any case, since we are using the Generator_ID in the catalog
			// to track this set now, I feel safe just ignoring this condition.
			else
			{
				if (LDeviceOperator.IsGenerated)
					Error.Fail("Encountered an existing device operator map for device '{0}' and operator '{1}'", ADevice.DisplayName, AOperator.DisplayName);
				
/*
				if (LDeviceOperator.IsGenerated)
					APlan.CatalogDeviceSession.AddDeviceScalarTypeDeviceOperator(ADeviceScalarType, LDeviceOperator);
					//ADeviceScalarType.DeviceOperators.Add(LDeviceOperator);
*/
			}
		}
		
		private static void CompileImplicitDeviceOperator(Plan APlan, Schema.Device ADevice, Schema.DeviceScalarType ADeviceScalarType, Schema.DeviceScalarType ABaseDeviceScalarType, Schema.Operator AOperator, Schema.Operator ABaseOperator)
		{
			Schema.DeviceOperator LBaseDeviceOperator = ADevice.ResolveDeviceOperator(APlan, ABaseOperator);
			if (LBaseDeviceOperator != null)
				CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, ABaseDeviceScalarType, AOperator, (ClassDefinition)LBaseDeviceOperator.ClassDefinition.Clone());
		}
		
		public static void CompileDeviceScalarTypeMapOperatorMaps(Plan APlan, Schema.Device ADevice, Schema.DeviceScalarType ADeviceScalarType)
		{
			// if the equality or comparison operators are set, map them based on the property type of the default representation
			APlan.EnsureDeviceStarted(ADevice);
			Schema.Representation LSystemRepresentation = FindSystemRepresentation(ADeviceScalarType.ScalarType);
			if (LSystemRepresentation != null)
			{
				Schema.DeviceScalarType LBaseDeviceScalarType = ADevice.ResolveDeviceScalarType(APlan, (Schema.ScalarType)LSystemRepresentation.Properties[0].DataType);
				if (LBaseDeviceScalarType != null)
				{
					if (ADeviceScalarType.ScalarType.EqualityOperator != null)
					{
						Schema.Operator LBaseOperator = ResolveOperator(APlan, Instructions.Equal, new Schema.Signature(new Schema.SignatureElement[] { new Schema.SignatureElement(LBaseDeviceScalarType.ScalarType), new Schema.SignatureElement(LBaseDeviceScalarType.ScalarType) }), false, false);
						if (LBaseOperator != null)
							CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, ADeviceScalarType.ScalarType.EqualityOperator, LBaseOperator);
					}
					
					if (ADeviceScalarType.ScalarType.ComparisonOperator != null)
					{
						Schema.Operator LBaseOperator = ResolveOperator(APlan, Instructions.Compare, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(LBaseDeviceScalarType.ScalarType), new Schema.SignatureElement(LBaseDeviceScalarType.ScalarType)}), false, false);
						if (LBaseOperator != null)
							CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, ADeviceScalarType.ScalarType.ComparisonOperator, LBaseOperator);
					}
						
					if ((ADeviceScalarType.ScalarType.IsSpecialOperator != null) && (ADeviceScalarType.ScalarType.Specials.Count == 0))
					{
						Schema.Operator LBaseOperator = ResolveOperator(APlan, "IsSpecial", new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(LBaseDeviceScalarType.ScalarType)}), false, false);
						if (LBaseOperator != null)
							CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, ADeviceScalarType.ScalarType.IsSpecialOperator, LBaseOperator);
					}
					
					// If this is a like type, map a default selector and accessors for the like representation, as long as that representation is system-provided (generated)
					Schema.Representation LLikeRepresentation = FindLikeRepresentation(ADeviceScalarType.ScalarType);
					if ((LLikeRepresentation != null) && (LLikeRepresentation.ID == LSystemRepresentation.ID))
					{
						CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, LLikeRepresentation.Selector, ADevice.GetDefaultSelectorClassDefinition());
						CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, LLikeRepresentation.Properties[0].ReadAccessor, ADevice.GetDefaultReadAccessorClassDefinition());
						CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, LLikeRepresentation.Properties[0].WriteAccessor, ADevice.GetDefaultWriteAccessorClassDefinition());
					}
					else
					{
						// Map the selector and accessors based on the selector of the property type
						Schema.Representation LBaseSystemRepresentation = FindSystemRepresentation(LBaseDeviceScalarType.ScalarType);
						if (LBaseSystemRepresentation == null)
						{
							Schema.Representation LDefaultRepresentation = FindDefaultRepresentation(LBaseDeviceScalarType.ScalarType);
							if ((LDefaultRepresentation != null) && (LDefaultRepresentation.Properties.Count == 1))
								LBaseSystemRepresentation = LDefaultRepresentation;
						}
						
						if (LBaseSystemRepresentation != null)
						{
							CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, LSystemRepresentation.Selector, LBaseSystemRepresentation.Selector); 
							CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, LSystemRepresentation.Properties[0].ReadAccessor, LBaseSystemRepresentation.Properties[0].ReadAccessor);
							CompileImplicitDeviceOperator(APlan, ADevice, ADeviceScalarType, LBaseDeviceScalarType, LSystemRepresentation.Properties[0].WriteAccessor, LBaseSystemRepresentation.Properties[0].WriteAccessor);
						}
					}
				}
			}
		}
		
		public static PlanNode CompileCreateServerStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);
				
			CreateServerStatement LStatement = (CreateServerStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateServer);

			string LServerName = Schema.Object.Qualify(LStatement.ServerName, APlan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(APlan, AStatement, LServerName);
			CreateServerNode LNode = new CreateServerNode();
			LNode.ServerLink = new Schema.ServerLink(Schema.Object.GetObjectID(LStatement.MetaData), LServerName);
			LNode.ServerLink.Owner = APlan.User;
			LNode.ServerLink.Library = APlan.CurrentLibrary;
			// TODO: Set ServerLink attributes from MetaData
			LNode.ServerLink.MetaData = LStatement.MetaData;
			LNode.ServerLink.IsRemotable = false;
			APlan.PushCreationObject(LNode.ServerLink);
			try
			{
				// Attach a dependency on the CreateServerLinkUserWithEncryptedPassword because it is used in the create script for the object
				Compiler.ResolveOperatorSpecifier
				(
					APlan, 
					new OperatorSpecifier
					(
						"System.CreateServerLinkUserWithEncryptedPassword", 
						new FormalParameterSpecifier[] 
						{
							new FormalParameterSpecifier(Modifier.Const, new ScalarTypeSpecifier("System.String")), 
							new FormalParameterSpecifier(Modifier.Const, new ScalarTypeSpecifier("System.Name")),
							new FormalParameterSpecifier(Modifier.Const, new ScalarTypeSpecifier("System.String")),
							new FormalParameterSpecifier(Modifier.Const, new ScalarTypeSpecifier("System.String"))
						}
					), 
					true
				);
				return LNode;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}

		public static PlanNode CompileCreateDeviceStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateDeviceStatement LStatement = (CreateDeviceStatement)AStatement;
			APlan.CheckRight(Schema.RightNames.CreateDevice);
			string LDeviceName = Schema.Object.Qualify(LStatement.DeviceName, APlan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(APlan, AStatement, LDeviceName);
			CreateDeviceNode LNode = new CreateDeviceNode();
			int LResourceManagerID = Int32.Parse(MetaData.GetTag(LStatement.MetaData, "DAE.ResourceManagerID", APlan.GetNextResourceManagerID().ToString()));
			APlan.CheckRight(Schema.RightNames.HostImplementation);
			APlan.CheckClassDependency(APlan.CurrentLibrary, LStatement.ClassDefinition);
			object LObject = APlan.Catalog.ClassLoader.CreateObject(LStatement.ClassDefinition, new object[]{Schema.Object.GetObjectID(LStatement.MetaData), LDeviceName, LResourceManagerID});
			if (!(LObject is Schema.Device))
				throw new CompilerException(CompilerException.Codes.DeviceClassExpected, LStatement.ClassDefinition, LObject == null ? "null" : LObject.GetType().AssemblyQualifiedName);
			LNode.NewDevice = (Schema.Device)LObject;
			LNode.NewDevice.Owner = APlan.User;
			LNode.NewDevice.Library = APlan.CurrentLibrary;
			APlan.PushCreationObject(LNode.NewDevice);
			try
			{
				LNode.NewDevice.MetaData = LStatement.MetaData;
				LNode.NewDevice.ClassDefinition = LStatement.ClassDefinition;
				LNode.NewDevice.IsRemotable = false;
				LNode.NewDevice.LoadRegistered();
				
/*
	BTR 5/30/2006 -> Device maps can only be created with an alter statement now (see the parser for more info)
				foreach (DeviceScalarTypeMap LDeviceScalarTypeMap in LStatement.DeviceScalarTypeMaps)
				{
					Schema.DeviceScalarType LDeviceScalarType = CompileDeviceScalarTypeMap(APlan, LNode.NewDevice, LDeviceScalarTypeMap);
					LNode.NewDevice.DeviceScalarTypes.Add(LDeviceScalarType);
					
					CompileDeviceScalarTypeMapOperatorMaps(APlan, LNode.NewDevice, LDeviceScalarType);
				}
				
				foreach (DeviceOperatorMap LDeviceOperatorMap in LStatement.DeviceOperatorMaps)
					LNode.NewDevice.DeviceOperators.Add(CompileDeviceOperatorMap(APlan, LNode.NewDevice, LDeviceOperatorMap));
*/
					
				if (LStatement.ReconciliationSettings.ReconcileModeSet)
					LNode.NewDevice.ReconcileMode = LStatement.ReconciliationSettings.ReconcileMode;
					
				if (LStatement.ReconciliationSettings.ReconcileMasterSet)
					LNode.NewDevice.ReconcileMaster = LStatement.ReconciliationSettings.ReconcileMaster;
				
				return LNode;
			}
			finally
			{
				APlan.PopCreationObject();
			}
		}
		
		public static void CheckValidObjectName(Plan APlan, Schema.Objects AObjects, Statement AStatement, string AObjectName)
		{
			StringCollection LNames = new StringCollection();
			if (!AObjects.IsValidObjectName(AObjectName, LNames))
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(LNames[0], AObjectName))
					if (String.Compare(LNames[0], AObjectName) == 0)
						throw new CompilerException(CompilerException.Codes.CreatingDuplicateObjectName, AStatement, AObjectName);
					else
						throw new CompilerException(CompilerException.Codes.CreatingHiddenObjectName, AStatement, AObjectName, LNames[0]);
				else
					throw new CompilerException(CompilerException.Codes.CreatingHidingObjectName, AStatement, AObjectName, LNames[0]);
				#else
				throw new CompilerException(CompilerException.Codes.CreatingDuplicateObjectName, AStatement, AObjectName);
				#endif
			}
		}
		
		public static void CheckValidSessionObjectName(Plan APlan, Statement AStatement, string AObjectName)
		{
			CheckValidObjectName(APlan, APlan.SessionObjects, AStatement, AObjectName);
			CheckValidObjectName(APlan, APlan.PlanSessionObjects, AStatement, AObjectName);
		}
		
		public static void CheckValidCatalogObjectName(Plan APlan, Statement AStatement, string AObjectName)
		{
			if (!APlan.IsRepository && (!APlan.InLoadingContext()))
			{
				if (APlan.CatalogDeviceSession.CatalogObjectExists(AObjectName))
					throw new CompilerException(CompilerException.Codes.CreatingDuplicateObjectName, AStatement, AObjectName);
			}
			else
				CheckValidObjectName(APlan, APlan.Catalog, AStatement, AObjectName);
			
			CheckValidObjectName(APlan, APlan.Catalog.Libraries, AStatement, AObjectName);
			CheckValidObjectName(APlan, APlan.PlanCatalog, AStatement, AObjectName);
		}
		
		public static void CheckValidOperatorName(Plan APlan, Schema.Catalog ACatalog, Statement AStatement, string AOperatorName, Schema.Signature ASignature)
		{
			StringCollection LNames = new StringCollection();
			if (!ACatalog.OperatorMaps.IsValidObjectName(AOperatorName, LNames))
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(LNames[0], AOperatorName))
					if (String.Compare(LNames[0], AOperatorName) == 0)
					{
						if (ACatalog.OperatorMaps[AOperatorName].ContainsSignature(ASignature))
							throw new CompilerException(CompilerException.Codes.CreatingDuplicateSignature, AStatement, AOperatorName, ASignature.ToString());
					}
					else
						throw new CompilerException(CompilerException.Codes.CreatingHiddenOperatorName, AStatement, AOperatorName, LNames[0]);
				else
					throw new CompilerException(CompilerException.Codes.CreatingHidingOperatorName, AStatement, AOperatorName, LNames[0]);
				#else
				if (ACatalog.OperatorMaps[AOperatorName].ContainsSignature(ASignature))
					throw new CompilerException(CompilerException.Codes.CreatingDuplicateSignature, AStatement, AOperatorName, ASignature.ToString());
				#endif
			}
		}
		
		public static void CheckValidCatalogOperatorName(Plan APlan, Statement AStatement, string AOperatorName, Schema.Signature ASignature)
		{
			if (!APlan.IsRepository && (!APlan.InLoadingContext()))
			{
				APlan.CatalogDeviceSession.ResolveOperatorName(AOperatorName);
				CheckValidOperatorName(APlan, APlan.Catalog, AStatement, AOperatorName, ASignature);
			}
			else
				CheckValidOperatorName(APlan, APlan.Catalog, AStatement, AOperatorName, ASignature);

			CheckValidOperatorName(APlan, APlan.PlanCatalog, AStatement, AOperatorName, ASignature);
		}
		
		public static void ResolveCall(Plan APlan, Schema.Catalog ACatalog, OperatorBindingContext AContext)
		{
			lock (ACatalog)
			{			
				Schema.Operator LOperator = AContext.Operator;
				ACatalog.OperatorMaps.ResolveCall(APlan, AContext);
				if ((LOperator == null) && (AContext.Operator != null))
				{
					APlan.AcquireCatalogLock(AContext.Operator, LockMode.Shared);
					APlan.AttachDependency(AContext.Operator);
				}
			}
		}
		
		// All operator resolution comes through this point, so this is where the operator resolution cache is checked
		public static void ResolveOperator(Plan APlan, OperatorBindingContext AContext)
		{
			// Operator resolutions for application-transaction or session-specific operators are never cached
			// search for an application transaction-specific operator
			if ((APlan.ApplicationTransactionID != Guid.Empty) && (!APlan.InLoadingContext()))
			{
				ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
				try
				{
					if (!LTransaction.IsGlobalContext && !LTransaction.IsLookup)
					{
						NameBindingContext LNameContext = new NameBindingContext(AContext.OperatorName, APlan.NameResolutionPath);
						int LIndex = LTransaction.OperatorMaps.ResolveName(LNameContext.Identifier, LNameContext.ResolutionPath, LNameContext.Names);
						if (LNameContext.IsAmbiguous)
						{
							AContext.OperatorNameContext.SetBindingDataFromContext(LNameContext);
							return;
						}
						
						if (LIndex >= 0)
						{
							OperatorBindingContext LContext = new OperatorBindingContext(AContext.Statement, LTransaction.OperatorMaps[LIndex].TranslatedOperatorName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
							ResolveOperator(APlan, LContext);
							
							if (LContext.Operator != null)
								AContext.SetBindingDataFromContext(LContext);
						}
					}
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
			
			// if no resolution occurred, or a partial-match was found, search for a session-specific operator
			if ((AContext.Operator == null) || AContext.Matches.IsPartial)
			{
				NameBindingContext LNameContext = new NameBindingContext(AContext.OperatorName, APlan.NameResolutionPath);
				int LIndex = APlan.PlanSessionOperators.ResolveName(LNameContext.Identifier, LNameContext.ResolutionPath, LNameContext.Names);
				if (LNameContext.IsAmbiguous)
				{
					AContext.OperatorNameContext.SetBindingDataFromContext(LNameContext);
					return;
				}
				
				if (LIndex >= 0)
				{
					OperatorBindingContext LContext = new OperatorBindingContext(AContext.Statement, ((Schema.SessionObject)APlan.PlanSessionOperators[LIndex]).GlobalName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
					ResolveOperator(APlan, LContext);
					
					if (LContext.Operator != null)
						AContext.SetBindingDataFromContext(LContext);
				}
			}
			
			if ((AContext.Operator == null) || AContext.Matches.IsPartial)
			{
				NameBindingContext LNameContext = new NameBindingContext(AContext.OperatorName, APlan.NameResolutionPath);
				int LIndex = APlan.SessionOperators.ResolveName(LNameContext.Identifier, LNameContext.ResolutionPath, LNameContext.Names);
				if (LNameContext.IsAmbiguous)
				{
					AContext.OperatorNameContext.SetBindingDataFromContext(LNameContext);
					return;
				}
				
				if (LIndex >= 0)
				{
					OperatorBindingContext LContext = new OperatorBindingContext(AContext.Statement, ((Schema.SessionObject)APlan.SessionOperators[LIndex]).GlobalName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
					ResolveOperator(APlan, LContext);
					
					if (LContext.Operator != null)
						AContext.SetBindingDataFromContext(LContext);
				}
			}
			
			// if no resolution occurred, or a partial-match was found, resolve normally
			if ((AContext.Operator == null) || AContext.Matches.IsPartial)
			{
				#if USEOPERATORRESOLUTIONCACHE
				lock (APlan.Catalog)
				{
					lock (APlan.Catalog.OperatorResolutionCache)
					{
						OperatorBindingContext LContext = APlan.Catalog.OperatorResolutionCache[AContext];
						if (LContext != null)
						{
							AContext.MergeBindingDataFromContext(LContext);
							if (AContext.Operator != null)
							{
								APlan.AcquireCatalogLock(AContext.Operator, LockMode.Shared);
								APlan.AttachDependency(AContext.Operator);
							}
						}
						else
						{
				#endif
							ResolveCall(APlan, APlan.PlanCatalog, AContext);
							if ((AContext.Operator == null) || AContext.Matches.IsPartial)
							{
								// NOTE: If this is a repository, or we are currently loading, there is no need to force the resolve of arbitrary matches, the required operator will be in the cache because of the dependency loading mechanism in the catalog device
								if (!APlan.IsRepository && (!APlan.InLoadingContext()))
									APlan.CatalogDeviceSession.ResolveOperatorName(AContext.OperatorName);
								ResolveCall(APlan, APlan.Catalog, AContext);
							}
				#if USEOPERATORRESOLUTIONCACHE
							if ((AContext.Operator == null) || !(AContext.Operator.IsSessionObject || AContext.Operator.IsATObject))
							{
								// Only cache the resolution if this is not a session or A/T operator
								LContext = new OperatorBindingContext(null, AContext.OperatorName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
								LContext.SetBindingDataFromContext(AContext);
								APlan.Catalog.OperatorResolutionCache.Add(LContext);
							}
						}
					}
				}
				#endif
			}
			
			// If a resolution occurred, and we are in an application transaction, 
			// and the operator should be translated, and it is not an application transaction specific operator
			//   Translate the operator into the application transaction space
			if ((AContext.Operator != null) && (APlan.ApplicationTransactionID != Guid.Empty) && (!APlan.InLoadingContext()))
			{
				ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
				try
				{
					if (!AContext.Operator.IsATObject)
					{
						if (AContext.Operator.ShouldTranslate && !LTransaction.IsGlobalContext && !LTransaction.IsLookup)
							AContext.Operator = LTransaction.AddOperator(APlan.ServerProcess, AContext.Operator);
					}
					else
						LTransaction.EnsureATOperatorMapped(APlan.ServerProcess, AContext.Operator);
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		public static void CheckOperatorResolution(Plan APlan, OperatorBindingContext AContext)
		{
			// Throw exceptions for the operator resolution.
			if (AContext.IsOperatorNameResolved)
			{
				if (AContext.Matches.Count == 0)
					throw new CompilerException(CompilerException.Codes.NoSignatureForParameterCardinality, AContext.Statement, AContext.OperatorNameContext.Object == null ? AContext.OperatorNameContext.Identifier : AContext.OperatorNameContext.Object.Name, AContext.CallSignature.Count.ToString());
					
				if (AContext.Matches.IsAmbiguous)
				{
					StringBuilder LBuilder = new StringBuilder();
					bool LFirst = true;
					for (int LIndex = 0; LIndex < AContext.Matches.BestMatches.Count; LIndex++)
					{
						if ((AContext.Matches.BestMatches[LIndex].IsMatch) && (AContext.Matches.BestMatches[LIndex].PathLength == AContext.Matches.ShortestPathLength))
						{
							if (!LFirst)
								LBuilder.Append(", ");
							else
								LFirst = false;
								
							LBuilder.AppendFormat("{0}{1}", AContext.Matches.BestMatches[LIndex].Signature.Operator.OperatorName, AContext.Matches.BestMatches[LIndex].Signature.Signature.ToString());
						}
					}
					throw new CompilerException(CompilerException.Codes.AmbiguousOperatorCall, AContext.Statement, AContext.OperatorName, AContext.CallSignature.ToString(), LBuilder.ToString());
				}
				
				if (AContext.IsExact && !AContext.Matches.IsExact)
					throw new CompilerException(CompilerException.Codes.NoExactMatch, AContext.Statement, AContext.OperatorName, AContext.CallSignature.ToString());
					
				if ((AContext.Matches.Count > 0) && !AContext.Matches.IsMatch)
				{
					OperatorMatch LMatch = AContext.Matches.ClosestMatch;
					if (LMatch != null)
					{
						APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidOperatorCall, AContext.Statement, AContext.OperatorName, AContext.CallSignature.ToString(), LMatch.Signature.Signature.ToString()));
						for (int LIndex = 0; LIndex < LMatch.Signature.Signature.Count; LIndex++)
						{
							if (!LMatch.CanConvert[LIndex])
								APlan.Messages.Add
								(
									new CompilerException
									(
										CompilerException.Codes.NoConversionForParameter, 
										(AContext.Statement is CallExpression) ? 
											((CallExpression)AContext.Statement).Expressions[LIndex] : 
											(
												(AContext.Statement is OperatorSpecifier) ? 
													((OperatorSpecifier)AContext.Statement).FormalParameterSpecifiers[LIndex] : 
													AContext.Statement
											), 
										LIndex.ToString(), 
										AContext.CallSignature[LIndex].ToString(), 
										LMatch.Signature.Signature[LIndex].ToString()
									)
								);
								
							if (LMatch.ConversionContexts[LIndex] != null)
								CheckConversionContext(APlan, LMatch.ConversionContexts[LIndex], false);
						}
					}
					else
						APlan.Messages.Add(new CompilerException(CompilerException.Codes.NoMatch, AContext.Statement, AContext.OperatorName, AContext.CallSignature.ToString()));
					throw new CompilerException(CompilerException.Codes.NonFatalErrors); // This exception will be caught and ignored in CompileStatement, allowing compilation to continue on the next statement.
				}
				
				if (AContext.Matches.IsPartial)
				{
					OperatorMatch LOperatorMatch = AContext.Matches.Match;
					for (int LIndex = 0; LIndex < LOperatorMatch.Signature.Signature.Count; LIndex++)
						if (LOperatorMatch.ConversionContexts[LIndex] != null)
							CheckConversionContext(APlan, LOperatorMatch.ConversionContexts[LIndex], false);
				}
			}
			else
				throw new CompilerException(CompilerException.Codes.OperatorNotFound, AContext.Statement, AContext.OperatorName, AContext.CallSignature.ToString());
		}
		
		public static Schema.Operator ResolveOperator(Plan APlan, string AOperatorName, Schema.Signature ASignature, bool AIsExact, bool AMustResolve)
		{
			//long LStartTicks = TimingUtility.CurrentTicks;
			OperatorBindingContext LContext = new OperatorBindingContext(new EmptyStatement(), AOperatorName, APlan.NameResolutionPath, ASignature, AIsExact);
			ResolveOperator(APlan, LContext);
			//APlan.Accumulator += TimingUtility.CurrentTicks - LStartTicks;
			if (AMustResolve)
				CheckOperatorResolution(APlan, LContext);
			return LContext.Operator;
		}
		
		public static Schema.Operator ResolveOperator(Plan APlan, string AOperatorName, Schema.Signature ASignature, bool AIsExact)
		{
			return ResolveOperator(APlan, AOperatorName, ASignature, AIsExact, true);
		}
		
		public static Schema.Operator ResolveOperatorSpecifier(Plan APlan, OperatorSpecifier AOperatorSpecifier, bool AMustResolve)
		{
			Schema.SignatureElement[] LElements = new Schema.SignatureElement[AOperatorSpecifier.FormalParameterSpecifiers.Count];
			for (int LIndex = 0; LIndex < AOperatorSpecifier.FormalParameterSpecifiers.Count; LIndex++)
				LElements[LIndex] = new Schema.SignatureElement(Compiler.CompileTypeSpecifier(APlan, AOperatorSpecifier.FormalParameterSpecifiers[LIndex].TypeSpecifier), AOperatorSpecifier.FormalParameterSpecifiers[LIndex].Modifier);
			Schema.Signature LSignature = new Schema.Signature(LElements);
			
			OperatorBindingContext LContext = new OperatorBindingContext(AOperatorSpecifier, AOperatorSpecifier.OperatorName, APlan.NameResolutionPath, LSignature, true);
			ResolveOperator(APlan, LContext);
			if (AMustResolve)
				CheckOperatorResolution(APlan, LContext);
			return LContext.Operator;
		}
		
		public static Schema.Operator ResolveOperatorSpecifier(Plan APlan, OperatorSpecifier AOperatorSpecifier)
		{
			return ResolveOperatorSpecifier(APlan, AOperatorSpecifier, true);
		}
		
		public static void AcquireDependentLocks(Plan APlan, Schema.Object AObject, LockMode AMode)
		{
			#if USECATALOGLOCKS
			APlan.AcquireCatalogLock(AObject, AMode);
			for (int LIndex = 0; LIndex < AObject.Dependents.Count; LIndex++)
				AcquireDependentLocks(APlan, AObject.Dependents.GetObjectForIndex(APlan.Catalog, LIndex), AMode);
			#endif
		}
		
		public static PlanNode CompileCreateConversionStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateConversionStatement LStatement = (CreateConversionStatement)AStatement;
			Schema.ScalarType LSourceScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(APlan, LStatement.SourceScalarTypeName);
			AcquireDependentLocks(APlan, LSourceScalarType, LockMode.Exclusive);
			Schema.ScalarType LTargetScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(APlan, LStatement.TargetScalarTypeName);
			AcquireDependentLocks(APlan, LTargetScalarType, LockMode.Shared);

			OperatorBindingContext LContext = new OperatorBindingContext(LStatement.OperatorName, LStatement.OperatorName.Identifier, APlan.NameResolutionPath, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(LSourceScalarType)}), false);
			ResolveOperator(APlan, LContext);
			CheckOperatorResolution(APlan, LContext);
			if ((LContext.Operator.ReturnDataType == null) || !LContext.Operator.ReturnDataType.Is(LTargetScalarType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, LStatement.OperatorName, LContext.Operator.ReturnDataType.Name, LTargetScalarType.Name);
			string LConversionName;
			if ((LStatement.MetaData != null) && (LStatement.MetaData.Tags.Contains("DAE.RootedIdentifier")))
				LConversionName = LStatement.MetaData.Tags["DAE.RootedIdentifier"].Value;
			else
				LConversionName = Schema.Object.Qualify(Schema.Object.MangleQualifiers(String.Format("Conversion_{0}_{1}", LSourceScalarType.Name, LTargetScalarType.Name)), APlan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(APlan, LStatement, LConversionName);
			Schema.Conversion LConversion = new Schema.Conversion(Schema.Object.GetObjectID(LStatement.MetaData), LConversionName, LSourceScalarType, LTargetScalarType, LContext.Operator, LStatement.IsNarrowing);
			LConversion.Owner = APlan.User;
			LConversion.Library = APlan.CurrentLibrary;
			LConversion.MergeMetaData(LStatement.MetaData);
			APlan.PushCreationObject(LConversion);
			try
			{
				APlan.AttachDependency(LConversion.SourceScalarType);
				APlan.AttachDependency(LConversion.TargetScalarType);
				APlan.AttachDependency(LConversion.Operator);
			}
			finally
			{
				APlan.PopCreationObject();
			}

			// Clear conversion path and operator resolution caches
			APlan.Catalog.ConversionPathCache.Clear(LConversion.SourceScalarType);
			APlan.Catalog.ConversionPathCache.Clear(LConversion.TargetScalarType);
			APlan.Catalog.OperatorResolutionCache.Clear(LConversion.SourceScalarType, LConversion.TargetScalarType);
			
			APlan.PlanCatalog.Add(LConversion);

			return new CreateConversionNode(LConversion);
		}
		
		public static PlanNode CompileDropConversionStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropConversionStatement LStatement = (DropConversionStatement)AStatement;
			DropConversionNode LNode = new DropConversionNode();
			LNode.SourceScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(APlan, LStatement.SourceScalarTypeName);
			AcquireDependentLocks(APlan, LNode.SourceScalarType, LockMode.Exclusive);
			LNode.TargetScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(APlan, LStatement.TargetScalarTypeName);
			AcquireDependentLocks(APlan, LNode.TargetScalarType, LockMode.Shared);

			// Clear the conversion path and operator resolution caches
			foreach (Schema.Conversion LConversion in LNode.SourceScalarType.ImplicitConversions)
				if (LConversion.TargetScalarType.Equals(LNode.TargetScalarType))
				{
					APlan.Catalog.ConversionPathCache.Clear(LConversion);
					APlan.Catalog.OperatorResolutionCache.Clear(LConversion);
					break;
				}
			return LNode;
		}
		
		public static PlanNode CompileCreateSortStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			CreateSortStatement LStatement = (CreateSortStatement)AStatement;
			CreateSortNode LNode = new CreateSortNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ScalarTypeName, true);
			if (!(LObject is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, AStatement);
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			LNode.ScalarType = (Schema.ScalarType)LObject;
			LNode.Sort = CompileSortDefinition(APlan, LNode.ScalarType, LStatement, true);
			if (LNode.ScalarType.UniqueSortID == LNode.Sort.ID)
			{
				LNode.IsUnique = true;
				LNode.Sort.IsUnique = true;
			}
			else
				LNode.Sort.IsGenerated = false;
			return LNode;
		}
		
		public static PlanNode CompileAlterSortStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterSortStatement LStatement = (AlterSortStatement)AStatement;
			AlterSortNode LNode = new AlterSortNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ScalarTypeName, true);
			if (!(LObject is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, AStatement);
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			LNode.ScalarType = (Schema.ScalarType)LObject;
			LNode.Sort = CompileSortDefinition(APlan, LNode.ScalarType, new SortDefinition(LStatement.Expression), true);
			return LNode;
		}
		
		public static PlanNode CompileDropSortStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropSortStatement LStatement = (DropSortStatement)AStatement;
			DropSortNode LNode = new DropSortNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ScalarTypeName, true);
			if (!(LObject is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, AStatement);
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			LNode.ScalarType = (Schema.ScalarType)LObject;
			LNode.IsUnique = LStatement.IsUnique;
			return LNode;
		}

		public static PlanNode CompileCreateRoleStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);
				
			APlan.CheckRight(Schema.RightNames.CreateRole);

			CreateRoleStatement LStatement = (CreateRoleStatement)AStatement;
			CreateRoleNode LNode = new CreateRoleNode();
			
			string LRoleName = Schema.Object.Qualify(LStatement.RoleName, APlan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(APlan, AStatement, LRoleName);

			LNode.Role = new Schema.Role(Schema.Object.GetObjectID(LStatement.MetaData), LRoleName);
			LNode.Role.MetaData = LStatement.MetaData;
			LNode.Role.Owner = APlan.User;
			LNode.Role.Library = APlan.CurrentLibrary;
			APlan.PlanCatalog.Add(LNode.Role);
			return LNode;
		}
		
		public static PlanNode CompileAlterRoleStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);
				
			AlterRoleStatement LStatement = (AlterRoleStatement)AStatement;
			Schema.Role LRole = ResolveCatalogIdentifier(APlan, LStatement.RoleName, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			APlan.CheckRight(LRole.GetRight(Schema.RightNames.Alter));
			
			AlterRoleNode LNode = new AlterRoleNode();
			LNode.Role = LRole;
			LNode.Statement = LStatement;
			return LNode;
		}
		
		public static PlanNode CompileDropRoleStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);
				
			DropRoleStatement LStatement = (DropRoleStatement)AStatement;
			Schema.Role LRole = ResolveCatalogIdentifier(APlan, LStatement.RoleName, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			APlan.CheckRight(LRole.GetRight(Schema.RightNames.Drop));
			
			DropRoleNode LNode = new DropRoleNode();
			LNode.Role = LRole;
			return LNode;
		}
		
		public static PlanNode CompileCreateRightStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);
				
			APlan.CheckRight(Schema.RightNames.CreateRight);
				
			CreateRightStatement LStatement = (CreateRightStatement)AStatement;
			
			CreateRightNode LNode = new CreateRightNode();
			LNode.RightName = LStatement.RightName;
			return LNode;
		}
		
		public static PlanNode CompileDropRightStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);
				
			DropRightStatement LStatement = (DropRightStatement)AStatement;
			
			DropRightNode LNode = new DropRightNode();
			LNode.RightName = LStatement.RightName;
			return LNode;
		}
		
		public static PlanNode CompileAlterTableStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterTableStatement LStatement = (AlterTableStatement)AStatement;
			AlterTableNode LNode = new AlterTableNode();
			LNode.ShouldAffectDerivationTimeStamp = APlan.ShouldAffectTimeStamp;
			LNode.AlterTableStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.TableVarName, false);
			if (LObject == null)
			{
				Schema.Device LDevice = GetDefaultDevice(APlan, false);
				if (LDevice != null)
				{
					APlan.CheckDeviceReconcile(new Schema.BaseTableVar(LStatement.TableVarName, new Schema.TableType(), LDevice));
					LObject = ResolveCatalogIdentifier(APlan, LStatement.TableVarName, true);
				}
			}
			if (!(LObject is Schema.BaseTableVar))
				throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.BaseTableVar)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterViewStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterViewStatement LStatement = (AlterViewStatement)AStatement;
			AlterViewNode LNode = new AlterViewNode();
			LNode.ShouldAffectDerivationTimeStamp = APlan.ShouldAffectTimeStamp;
			LNode.AlterViewStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.TableVarName, true);
			if (!(LObject is Schema.DerivedTableVar))
				throw new CompilerException(CompilerException.Codes.ViewIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.DerivedTableVar)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}
		
		//	Alter scalar type statement ->
		//		Name -> The name of a scalar type may not be altered
		//		ParentScalarTypes -> The parent scalar types of a scalar type may not be altered
		//		Conveyor -> The conveyor of a scalar type may be altered, but it is the developers responsibility to ensure the new conveyor is compatible with existing data
		//		MetaData -> ScalarType metadata may be altered, but the StaticByteSize of the scalar type may not be changed
		//		Representations ->
		//			Representations may be created
		//			Name -> A representations name may not be altered
		//			MetaData -> A representations meta data may be altered
		//			Selector -> A representations selector may be altered, provided the selector operator has no dependent constraints
		//			Properties ->
		//				Properties may be created
		//				Name -> A properties name may not be altered
		//				Type -> A properties type may be altered
		//				MetaData -> A properties meta data may be altered
		//				ReadAccessor -> A properties read accessor may be altered, provided the read operator has no dependent constraints
		//				WriteAccessor -> A properties write accessor may be altered, provided the write operator has no dependent constraints
		//				Properties may be dropped, provided the read and write accessors have no dependents
		//			Representations may be dropped, if the representations operators have no dependents.  
		//				If the representation being dropped is the last representation on the scalar type, and the alter statement does not contain
		//				any subsequent create representation definitions, the same logic used to provide a default representation for the scalar type
		//				at scalar type creation will be used.
		//		Specials ->
		//			Specials may be created, provided the IsSpecial operator for the scalar type has no dependent constraints
		//			Name -> A specials name may not be altered
		//			MetaData -> A specials metadata may be altered
		//			Value -> A specials value may be altered, provided the IsSpecial for the scalar type, and the comparer and selector for the Special have no dependent constraints
		//			Specials may be dropped, provided the IsSpecial for the scalar type has no dependent constraints
		//		Constraints ->
		//			Constraints may be created, provided the scalar type has no dependent constraints, and there is no data which violates the new constraint
		//			Name -> A constraints name may not be altered
		//			MetaData -> A constraints metadata may be altered
		//			Expression -> A constraints expression may be altered, provided the scalar type has no dependent constraints, and there is no data which violates the new constraint
		//			Constraints may be dropped
		//		Default ->
		//			A scalar type default may be added, if one is not present
		//			Name -> A defaults name may not be altered
		//			MetaData -> A defaults metadata may be altered, if one is present
		//			Value -> A defaults value may be altered
		//			A scalar type default may be dropped
		//			
		//	Any alteration of a scalar type requires the acquisition of exclusive locks on the scalar type, and all dependents, recursively.
		public static PlanNode CompileAlterScalarTypeStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterScalarTypeStatement LStatement = (AlterScalarTypeStatement)AStatement;
			AlterScalarTypeNode LNode = new AlterScalarTypeNode();
			LNode.AlterScalarTypeStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ScalarTypeName, true);
			if (!(LObject is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.ScalarType)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterOperatorStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterOperatorStatement LStatement = (AlterOperatorStatement)AStatement;
			AlterOperatorNode LNode = new AlterOperatorNode();
			LNode.AlterOperatorStatement = LStatement;
			Schema.Operator LOperator = ResolveOperatorSpecifier(APlan, LStatement.OperatorSpecifier);
			APlan.CheckRight(LOperator.GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LOperator, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterAggregateOperatorStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterAggregateOperatorStatement LStatement = (AlterAggregateOperatorStatement)AStatement;
			AlterAggregateOperatorNode LNode = new AlterAggregateOperatorNode();
			LNode.AlterAggregateOperatorStatement = LStatement;
			Schema.Operator LOperator = ResolveOperatorSpecifier(APlan, LStatement.OperatorSpecifier);
			APlan.CheckRight(LOperator.GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LOperator, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterConstraintStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterConstraintStatement LStatement = (AlterConstraintStatement)AStatement;
			AlterConstraintNode LNode = new AlterConstraintNode();
			LNode.AlterConstraintStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ConstraintName, true);
			if (!(LObject is Schema.CatalogConstraint))
				throw new CompilerException(CompilerException.Codes.ConstraintIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.CatalogConstraint)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterReferenceStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterReferenceStatement LStatement = (AlterReferenceStatement)AStatement;
			AlterReferenceNode LNode = new AlterReferenceNode();
			LNode.AlterReferenceStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ReferenceName, true);
			if (!(LObject is Schema.Reference))
				throw new CompilerException(CompilerException.Codes.ReferenceIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.Reference)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterDeviceStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterDeviceStatement LStatement = (AlterDeviceStatement)AStatement;
			AlterDeviceNode LNode = new AlterDeviceNode();
			LNode.AlterDeviceStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.DeviceName, true);
			if (!(LObject is Schema.Device))
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.Device)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileAlterServerStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			AlterServerStatement LStatement = (AlterServerStatement)AStatement;
			AlterServerNode LNode = new AlterServerNode();
			LNode.AlterServerStatement = LStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ServerName, true);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.ServerLink)LObject).GetRight(Schema.RightNames.Alter));
			AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return LNode;
		}

		public static PlanNode CompileDropTableStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropTableStatement LStatement = (DropTableStatement)AStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ObjectName, false);
			if (LObject == null)
			{
				Schema.Device LDevice = GetDefaultDevice(APlan, false);
				if (LDevice != null)
				{
					APlan.CheckDeviceReconcile(new Schema.BaseTableVar(LStatement.ObjectName, new Schema.TableType(), LDevice));
					LObject = ResolveCatalogIdentifier(APlan, LStatement.ObjectName, true);	
				}
			}

			if (!(LObject is Schema.BaseTableVar))
				throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, AStatement);

			Schema.BaseTableVar LBaseTableVar = (Schema.BaseTableVar)LObject;
			APlan.CheckRight(((Schema.BaseTableVar)LObject).GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			if (LBaseTableVar.SessionObjectName != null)
			{
				int LObjectIndex = APlan.PlanSessionObjects.IndexOf(LBaseTableVar.SessionObjectName);
				if (LObjectIndex >= 0)
					APlan.PlanSessionObjects.RemoveAt(LObjectIndex);
			}
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			return new DropTableNode(LBaseTableVar, APlan.ShouldAffectTimeStamp);
		}
		
		public static PlanNode CompileDropViewStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropViewStatement LStatement = (DropViewStatement)AStatement;
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ObjectName, true);
			if (!(LObject is Schema.DerivedTableVar))
				throw new CompilerException(CompilerException.Codes.ViewIdentifierExpected, AStatement);
			Schema.DerivedTableVar LDerivedTableVar = (Schema.DerivedTableVar)LObject;
			APlan.CheckRight(LDerivedTableVar.GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			if (LDerivedTableVar.SessionObjectName != null)
			{
				int LObjectIndex = APlan.PlanSessionObjects.IndexOf(LDerivedTableVar.SessionObjectName);
				if (LObjectIndex >= 0)
					APlan.PlanSessionObjects.RemoveAt(LObjectIndex);
			}
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			return new DropViewNode(LDerivedTableVar, APlan.ShouldAffectTimeStamp);
		}
		
		public static PlanNode CompileDropScalarTypeStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropScalarTypeStatement LStatement = (DropScalarTypeStatement)AStatement;
			DropScalarTypeNode LNode = new DropScalarTypeNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ObjectName, true);
			Schema.ScalarType LScalarType = LObject as Schema.ScalarType;
			if (LScalarType == null)
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, AStatement);
			APlan.CheckRight(LScalarType.GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			LNode.ScalarType = LScalarType;			
			
			// Remove the Equality and Comparison operators for this scalar type
			if ((LNode.ScalarType.EqualityOperator != null) && APlan.PlanCatalog.Contains(LNode.ScalarType.EqualityOperator.Name))
				APlan.PlanCatalog.Remove(LNode.ScalarType.EqualityOperator);
				
			if ((LNode.ScalarType.ComparisonOperator != null) && APlan.PlanCatalog.Contains(LNode.ScalarType.ComparisonOperator.Name))
				APlan.PlanCatalog.Remove(LNode.ScalarType.ComparisonOperator);

			// Remove the Default Selector Operators for this scalar type
			foreach (Schema.Representation LRepresentation in LScalarType.Representations)
			{
				if ((LRepresentation.Selector != null) && APlan.PlanCatalog.Contains(LRepresentation.Selector.Name))
					APlan.PlanCatalog.Remove(LRepresentation.Selector);
	
				foreach (Schema.Property LProperty in LRepresentation.Properties)
				{
					if ((LProperty.ReadAccessor != null) && APlan.PlanCatalog.Contains(LProperty.ReadAccessor.Name))
						APlan.PlanCatalog.Remove(LProperty.ReadAccessor);
					
					if ((LProperty.WriteAccessor != null) && APlan.PlanCatalog.Contains(LProperty.WriteAccessor.Name))
						APlan.PlanCatalog.Remove(LProperty.WriteAccessor);
				}
			}
			
			#if USETYPEINHERITANCE
			// Remove the Explicit Cast Operators for this scalar type
			foreach (Schema.Object LOperator in LScalarType.ExplicitCastOperators)
				if (APlan.PlanCatalog.Contains(LOperator.Name))
					APlan.PlanCatalog.Remove(LOperator);
			#endif
			
			// Remove the special selector and comparison operators for this scalar type
			if ((LScalarType.IsSpecialOperator != null) && APlan.PlanCatalog.Contains(LScalarType.IsSpecialOperator.Name))
				APlan.PlanCatalog.Remove(LScalarType.IsSpecialOperator);

			foreach (Schema.Special LSpecial in LScalarType.Specials)
			{
				if ((LSpecial.Selector != null) && APlan.PlanCatalog.Contains(LSpecial.Selector.Name))
					APlan.PlanCatalog.Remove(LSpecial.Selector);
				
				if ((LSpecial.Comparer != null) && APlan.PlanCatalog.Contains(LSpecial.Comparer.Name))
					APlan.PlanCatalog.Remove(LSpecial.Comparer);
			}
			
			APlan.Catalog.OperatorResolutionCache.Clear();
			
			return LNode;
		}
		
		public static PlanNode CompileDropOperatorStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropOperatorStatement LStatement = (DropOperatorStatement)AStatement;
			DropOperatorNode LNode = new DropOperatorNode();
			LNode.ShouldAffectDerivationTimeStamp = APlan.ShouldAffectTimeStamp;
			LNode.OperatorSpecifier = new OperatorSpecifier();
			LNode.OperatorSpecifier.OperatorName = LStatement.ObjectName;
			LNode.OperatorSpecifier.Line = LStatement.Line;
			LNode.OperatorSpecifier.LinePos = LStatement.LinePos;
			LNode.OperatorSpecifier.FormalParameterSpecifiers.AddRange(LStatement.FormalParameterSpecifiers);
			LNode.DropOperator = ResolveOperatorSpecifier(APlan, LNode.OperatorSpecifier);
			APlan.CheckRight(LNode.DropOperator.GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LNode.DropOperator.Name))
				APlan.PlanCatalog.Remove(LNode.DropOperator);
			APlan.AcquireCatalogLock(LNode.DropOperator, LockMode.Exclusive);
			APlan.Catalog.OperatorResolutionCache.Clear(LNode.DropOperator.OperatorName);
			return LNode;
		}
		
		public static PlanNode CompileDropConstraintStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropConstraintStatement LStatement = (DropConstraintStatement)AStatement;
			DropConstraintNode LNode = new DropConstraintNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ConstraintName, true);
			if (!(LObject is Schema.CatalogConstraint))
				throw new CompilerException(CompilerException.Codes.ConstraintIdentifierExpected, AStatement);
			Schema.CatalogConstraint LConstraint = (Schema.CatalogConstraint)LObject;
			APlan.CheckRight(LConstraint.GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			if (LConstraint.SessionObjectName != null) 
			{
				int LObjectIndex = APlan.PlanSessionObjects.IndexOf(LConstraint.SessionObjectName);
				if (LObjectIndex >= 0)
					APlan.PlanSessionObjects.RemoveAt(LObjectIndex);
			}				
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			LNode.Constraint = LConstraint;
			return LNode;
		}
		
		public static PlanNode CompileDropReferenceStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropReferenceStatement LStatement = (DropReferenceStatement)AStatement;
			DropReferenceNode LNode = new DropReferenceNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ReferenceName, true);
			if (!(LObject is Schema.Reference))
				throw new CompilerException(CompilerException.Codes.ReferenceIdentifierExpected, AStatement);
			LNode.ReferenceName = LObject.Name;
			Schema.Reference LReference = (Schema.Reference)LObject;
			APlan.CheckRight(LReference.GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			if (LReference.SessionObjectName != null)
			{
				int LObjectIndex = APlan.PlanSessionObjects.IndexOf(LReference.SessionObjectName);
				if (LObjectIndex >= 0)
					APlan.PlanSessionObjects.RemoveAt(LObjectIndex);
			}
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			return LNode;
		}
		
		public static PlanNode CompileDropDeviceStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropDeviceStatement LStatement = (DropDeviceStatement)AStatement;
			DropDeviceNode LNode = new DropDeviceNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ObjectName, true);
			if (!(LObject is Schema.Device))
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.Device)LObject).GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			LNode.DropDevice = (Schema.Device)LObject;
			return LNode;
		}
		
		public static PlanNode CompileDropServerStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			DropServerStatement LStatement = (DropServerStatement)AStatement;
			DropServerNode LNode = new DropServerNode();
			Schema.Object LObject = ResolveCatalogIdentifier(APlan, LStatement.ObjectName, true);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected, AStatement);
			APlan.CheckRight(((Schema.ServerLink)LObject).GetRight(Schema.RightNames.Drop));
			if (APlan.PlanCatalog.Contains(LObject.Name))
				APlan.PlanCatalog.Remove(LObject);
			APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			LNode.ServerLink = (Schema.ServerLink)LObject;
			return LNode;
		}

		/*
			Default : operator(var <AScalarType>)
			Validate :
			Change : operator(var <AScalarType>) || operator(const <AScalarType>, var <AScalarType>)
		*/		
		public static Schema.SignatureElement[] GetScalarTypeEventSignature(Plan APlan, Statement AStatement, EventType AEventType, Schema.ScalarType AScalarType, bool ASimpleSignature)
		{
			switch (AEventType)
			{
				case EventType.Default :
					return new Schema.SignatureElement[]{new Schema.SignatureElement(AScalarType, Modifier.Var)};
				
				case EventType.Validate :
				case EventType.Change :
					if (ASimpleSignature)
						return new Schema.SignatureElement[]{new Schema.SignatureElement(AScalarType, Modifier.Var)};
					return new Schema.SignatureElement[]{new Schema.SignatureElement(AScalarType, Modifier.Const), new Schema.SignatureElement(AScalarType, Modifier.Var)};
					
				default : throw new CompilerException(CompilerException.Codes.InvalidEventType, AStatement, AScalarType.Name, AEventType.ToString());
			}
		}

		/*
			Default : operator(var <column data type>)
			Validate :
			Change : operator(var <table var row type>) || operator(const <table var row type>, var <table var row type>)
		*/		
		public static Schema.SignatureElement[] GetTableVarColumnEventSignature(Plan APlan, Statement AStatement, EventType AEventType, Schema.TableVar ATableVar, int AColumnIndex, bool ASimpleSignature)
		{
			switch (AEventType)
			{
				case EventType.Default:
					return new Schema.SignatureElement[]{new Schema.SignatureElement(ATableVar.DataType.Columns[AColumnIndex].DataType, Modifier.Var)};
				
				case EventType.Validate:
				case EventType.Change:
					if (ASimpleSignature)
						return new Schema.SignatureElement[]{new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Var)};
					return new Schema.SignatureElement[]{new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Const), new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Var)};
				
				default: throw new CompilerException(CompilerException.Codes.InvalidEventType, AStatement, ATableVar.Name, AEventType.ToString());
			}						
		}

		/*
			BeforeInsert : operator(var <tablevar row type>, var System.Boolean)
			AfterInsert : operator(const <tablevar row type>)
			BeforeUpdate : operator(const <tablevar row type>, var <tablevar row type>, var System.Boolean)
			AfterUpdate : operator(const <tablevar row type>, const <tablevar row type>)
			BeforeDelete : operator(const <tablevar row type>, var System.Boolean)
			AfterDelete : operator(const <tablevar row type>)
			Default : operator(var <tablevar row type>, const System.String)
			Validate :
			Change : operator(var <tablevar row type>, const System.String) || operator(const <tablevar row type>, var <tablevar row type>, const String)
		*/		
		public static Schema.SignatureElement[] GetTableVarEventSignature(Plan APlan, Statement AStatement, EventType AEventType, Schema.TableVar ATableVar, bool ASimpleSignature)
		{
			switch (AEventType)
			{
				case EventType.BeforeInsert:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Var),
							new Schema.SignatureElement(APlan.DataTypes.SystemBoolean, Modifier.Var)
						};

				case EventType.AfterInsert:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Const)
						};
				
				case EventType.BeforeUpdate:
					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Const),
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Var),
							new Schema.SignatureElement(APlan.DataTypes.SystemBoolean, Modifier.Var)
						};
				
				case EventType.AfterUpdate:
					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Const),
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Const)
						};
				
				case EventType.BeforeDelete:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Const),
							new Schema.SignatureElement(APlan.DataTypes.SystemBoolean, Modifier.Var)
						};
				
				case EventType.AfterDelete:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(ATableVar.DataType.RowType, Modifier.Const)
						};
				
				case EventType.Default:
					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Var),
							new Schema.SignatureElement(APlan.DataTypes.SystemString, Modifier.Const)
						};

				case EventType.Validate:
				case EventType.Change:
					if (ASimpleSignature)
						return
							new Schema.SignatureElement[]
							{
								new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Var),
								new Schema.SignatureElement(APlan.DataTypes.SystemString, Modifier.Const)
							};

					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Const),
							new Schema.SignatureElement(new Schema.RowType(ATableVar.DataType.Columns), Modifier.Var),
							new Schema.SignatureElement(APlan.DataTypes.SystemString, Modifier.Const)
						};
				
				default: throw new CompilerException(CompilerException.Codes.InvalidEventType, AStatement, ATableVar.Name, AEventType.ToString());
			}
		}
		
		public static void CheckValidEventHandler(Plan APlan, Statement AStatement, CreateEventHandlerNode ANode)
		{
			Schema.TableVar LTableVar = ANode.EventSource as Schema.TableVar;
			if (LTableVar != null)
			{
				if (ANode.EventSourceColumnIndex >= 0)
				{
					Schema.TableVarColumn LColumn = LTableVar.Columns[ANode.EventSourceColumnIndex];
					if (LColumn.HasHandlers() && (LColumn.EventHandlers.IndexOf(ANode.EventHandler.Operator, ANode.EventHandler.EventType) >= 0))
						throw new CompilerException(CompilerException.Codes.OperatorAlreadyAttachedToColumnEvent, AStatement, ANode.EventHandler.Operator.OperatorName, ANode.EventHandler.EventType.ToString(), LColumn.Name, LTableVar.Name);
				}
				else
				{
					if (LTableVar.HasHandlers() && (LTableVar.EventHandlers.IndexOf(ANode.EventHandler.Operator, ANode.EventHandler.EventType) >= 0))
						throw new CompilerException(CompilerException.Codes.OperatorAlreadyAttachedToObjectEvent, AStatement, ANode.EventHandler.Operator.OperatorName, ANode.EventHandler.EventType.ToString(), LTableVar.Name);
				}
			}
			else
			{
				Schema.ScalarType LScalarType = ANode.EventSource as Schema.ScalarType;
				if (LScalarType != null)
				{
					if (LScalarType.HasHandlers() && (LScalarType.EventHandlers.IndexOf(ANode.EventHandler.Operator, ANode.EventHandler.EventType) >= 0))
						throw new CompilerException(CompilerException.Codes.OperatorAlreadyAttachedToObjectEvent, AStatement, ANode.EventHandler.Operator.OperatorName, ANode.EventHandler.EventType.ToString(), LScalarType.Name);
				}
			}
		}
		
		public static PlanNode EmitCreateEventHandlerNode(Plan APlan, AttachStatement AStatement, EventType AEventType)
		{
			// build a trigger handler
			// resolve the event source
			// build the signature for the specified event
			// resolve the operator specifier
			// verify the resolved operator signature is compatible with the event signature
			// build the execution node
			CreateEventHandlerNode LNode = new CreateEventHandlerNode();
			Schema.SignatureElement[] LEventSignature = null;
			Schema.SignatureElement[] LNewEventSignature = null;
			LNode.BeforeOperatorNames = AStatement.BeforeOperatorNames;
			bool LCreationObjectPushed = false;
			try
			{
				int LObjectID = Schema.Object.GetObjectID(AStatement.MetaData);
				if (AStatement.EventSourceSpecifier is ObjectEventSourceSpecifier)
				{
					LNode.EventSource = ResolveCatalogIdentifier(APlan, ((ObjectEventSourceSpecifier)AStatement.EventSourceSpecifier).ObjectName, true);
					if (LNode.EventSource is Schema.TableVar)
					{
						LNode.EventHandler = new Schema.TableVarEventHandler(LObjectID, Schema.Object.GetGeneratedName(String.Format("{0}_{1}", LNode.EventSource.Name, AEventType.ToString()), LObjectID));
						LNode.EventHandler.Owner = APlan.User;
						LNode.EventHandler.Library = LNode.EventSource.Library == null ? null : APlan.CurrentLibrary;
						LNode.EventHandler.IsGenerated = AStatement.IsGenerated;
						if (LNode.EventSource.IsSessionObject)
							LNode.EventHandler.SessionObjectName = LNode.EventHandler.Name;
						LNode.EventHandler.MergeMetaData(AStatement.MetaData);
						Tag LTag = MetaData.GetTag(LNode.EventHandler.MetaData, "DAE.ATHandlerName");
						if (LTag != Tag.None)
							LNode.EventHandler.ATHandlerName = LTag.Value;
						APlan.PushCreationObject(LNode.EventHandler);
						LCreationObjectPushed = true;
						APlan.AttachDependency(LNode.EventSource);
						LEventSignature = GetTableVarEventSignature(APlan, AStatement, AEventType, (Schema.TableVar)LNode.EventSource, true);
						LNewEventSignature = GetTableVarEventSignature(APlan, AStatement, AEventType, (Schema.TableVar)LNode.EventSource, false);
					}
					else if (LNode.EventSource is Schema.ScalarType)
					{
						LNode.EventHandler = new Schema.ScalarTypeEventHandler(LObjectID, Schema.Object.GetGeneratedName(String.Format("{0}_{1}", LNode.EventSource.Name, AEventType.ToString()), LObjectID));
						LNode.EventHandler.Owner = APlan.User;
						LNode.EventHandler.Library = LNode.EventSource.Library == null ? null : APlan.CurrentLibrary;
						LNode.EventHandler.IsGenerated = AStatement.IsGenerated;
						if (LNode.EventSource.IsSessionObject)
							LNode.EventHandler.SessionObjectName = LNode.EventHandler.Name;
						LNode.EventHandler.MergeMetaData(AStatement.MetaData);
						Tag LTag = MetaData.GetTag(LNode.EventHandler.MetaData, "DAE.ATHandlerName");
						if (LTag != Tag.None)
							LNode.EventHandler.ATHandlerName = LTag.Value;
						APlan.PushCreationObject(LNode.EventHandler);
						LCreationObjectPushed = true;
						APlan.AttachDependency(LNode.EventSource);
						LEventSignature = GetScalarTypeEventSignature(APlan, AStatement, AEventType, (Schema.ScalarType)LNode.EventSource, true);
						LNewEventSignature = GetScalarTypeEventSignature(APlan, AStatement, AEventType, (Schema.ScalarType)LNode.EventSource, false);
					}
					else
						throw new CompilerException(CompilerException.Codes.InvalidEventSource, AStatement, LNode.EventSource.Name, AEventType.ToString());
				}
				else if (AStatement.EventSourceSpecifier is ColumnEventSourceSpecifier)
				{
					LNode.EventSource = ResolveCatalogIdentifier(APlan, ((ColumnEventSourceSpecifier)AStatement.EventSourceSpecifier).TableVarName, true);
					if (!(LNode.EventSource is Schema.TableVar))
						throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, AStatement.EventSourceSpecifier);
						
					LNode.EventSourceColumnIndex = ((Schema.TableVar)LNode.EventSource).DataType.Columns.IndexOf(((ColumnEventSourceSpecifier)AStatement.EventSourceSpecifier).ColumnName);
					if (LNode.EventSourceColumnIndex < 0)
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, AStatement.EventSourceSpecifier);
						
					LNode.EventHandler = new Schema.TableVarColumnEventHandler(LObjectID, Schema.Object.GetGeneratedName(String.Format("{0}_{1}_{2}", LNode.EventSource.Name, ((Schema.TableVar)LNode.EventSource).Columns[LNode.EventSourceColumnIndex].Name, AEventType.ToString()), LObjectID));
					LNode.EventHandler.MergeMetaData(AStatement.MetaData);
					Tag LTag = MetaData.GetTag(LNode.EventHandler.MetaData, "DAE.ATHandlerName");
					if (LTag != Tag.None)
						LNode.EventHandler.ATHandlerName = LTag.Value;
					APlan.PushCreationObject(LNode.EventHandler);
					LCreationObjectPushed = true;

					LNode.EventHandler.Owner = APlan.User;
					LNode.EventHandler.Library = LNode.EventSource.Library == null ? null : APlan.CurrentLibrary;
					LNode.EventHandler.IsGenerated = AStatement.IsGenerated;
					if (LNode.EventSource.IsSessionObject)
						LNode.EventHandler.SessionObjectName = LNode.EventHandler.Name;
					APlan.AttachDependency(LNode.EventSource);

					LEventSignature = GetTableVarColumnEventSignature(APlan, AStatement, AEventType, (Schema.TableVar)LNode.EventSource, LNode.EventSourceColumnIndex, true);
					LNewEventSignature = GetTableVarColumnEventSignature(APlan, AStatement, AEventType, (Schema.TableVar)LNode.EventSource, LNode.EventSourceColumnIndex, false);
				}
				else
					throw new CompilerException(CompilerException.Codes.UnknownEventSourceSpecifierClass, AStatement.EventSourceSpecifier, AStatement.EventSourceSpecifier.GetType().Name);
				
				OperatorBindingContext LContext = new OperatorBindingContext(AStatement, AStatement.OperatorName, APlan.NameResolutionPath, new Schema.Signature(LEventSignature), false);
				ResolveOperator(APlan, LContext);
				
				if (LContext.Operator == null)
				{
					LEventSignature = LNewEventSignature;
					LContext = new OperatorBindingContext(AStatement, AStatement.OperatorName, APlan.NameResolutionPath, new Schema.Signature(LEventSignature), false);
					ResolveOperator(APlan, LContext);
				}
				
				CheckOperatorResolution(APlan, LContext);
				
				PlanNode[] LArguments = new PlanNode[LEventSignature.Length];
				for (int LIndex = 0; LIndex < LArguments.Length; LIndex++)
					LArguments[LIndex] = new StackReferenceNode(LEventSignature[LIndex].DataType, LArguments.Length - (LIndex + 1), true);

				LNode.EventHandler.EventType = AEventType;	
				LNode.EventHandler.Operator = LContext.Operator;
				LNode.EventHandler.DetermineRemotable(APlan.CatalogDeviceSession);
				
				// Check to see if the handler is already attached
				CheckValidEventHandler(APlan, AStatement, LNode);
				
				for (int LIndex = LArguments.Length - 1; LIndex >= 0; LIndex--)
				{
					APlan.Symbols.Push(new Symbol(LArguments[LIndex].DataType));
				}
				try
				{
					LNode.EventHandler.PlanNode = BuildCallNode(APlan, LContext, LArguments);
					LNode.EventHandler.PlanNode.DetermineDataType(APlan);
					LNode.EventHandler.PlanNode.DetermineCharacteristics(APlan);
				}
				finally
				{
					for (int LIndex = 0; LIndex < LArguments.Length; LIndex++)
						APlan.Symbols.Pop();
				}
					
				if (LNode.EventSourceColumnIndex >= 0)
					LNode.EventHandler.Name = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_{2}_{3}", LNode.EventSource.Name, ((Schema.TableVar)LNode.EventSource).DataType.Columns[LNode.EventSourceColumnIndex].Name, LNode.EventHandler.Operator.OperatorName, LNode.EventHandler.EventType.ToString()), LNode.EventHandler.ID);
				else
					LNode.EventHandler.Name = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_{2}", LNode.EventSource.Name, LNode.EventHandler.Operator.OperatorName, LNode.EventHandler.EventType.ToString()), LNode.EventHandler.ID);
				
				return LNode;
			}
			finally
			{
				if (LCreationObjectPushed)
					APlan.PopCreationObject();
			}
		}
		
		public static PlanNode CompileAttachStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			// foreach event type
				// emit a CreateEventHandlerNode
			AttachStatement LStatement = (AttachStatement)AStatement;
			BlockNode LNode = new BlockNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeInsert) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.BeforeInsert));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterInsert) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.AfterInsert));
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeUpdate) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.BeforeUpdate));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterUpdate) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.AfterUpdate));
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeDelete) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.BeforeDelete));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterDelete) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.AfterDelete));
			if ((LStatement.EventSpecifier.EventType & EventType.Default) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.Default));
			if ((LStatement.EventSpecifier.EventType & EventType.Change) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.Change));
			if ((LStatement.EventSpecifier.EventType & EventType.Validate) != 0)
				LNode.Nodes.Add(EmitCreateEventHandlerNode(APlan, LStatement, EventType.Validate));
			return LNode;
		}
		
		public static PlanNode EmitAlterEventHandlerNode(Plan APlan, InvokeStatement AStatement, EventType AEventType)
		{
			// resolve the event target
			// resolve the operator specifier
			// find the trigger handler on the event target
			AlterEventHandlerNode LNode = new AlterEventHandlerNode();
			LNode.BeforeOperatorNames = AStatement.BeforeOperatorNames;
			if (AStatement.EventSourceSpecifier is ObjectEventSourceSpecifier)
			{
				LNode.EventSource = ResolveCatalogIdentifier(APlan, ((ObjectEventSourceSpecifier)AStatement.EventSourceSpecifier).ObjectName, true);

				Schema.TableVar LTableVar = LNode.EventSource as Schema.TableVar;
				Schema.ScalarType LScalarType = LNode.EventSource as Schema.ScalarType;
				if (LTableVar != null)
				{
					int LHandlerIndex = -1;
					if (LTableVar.HasHandlers())
						LHandlerIndex = LTableVar.EventHandlers.IndexOf(AStatement.OperatorName, AEventType);
					if (LHandlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, AStatement.EventSpecifier, AStatement.OperatorName, AEventType.ToString(), LNode.EventSource.Name);
					LNode.EventHandler = LTableVar.EventHandlers[LHandlerIndex];
				}
				else if (LScalarType != null)
				{
					int LHandlerIndex = -1;
					if (LScalarType.HasHandlers())
						LHandlerIndex = LScalarType.EventHandlers.IndexOf(AStatement.OperatorName, AEventType);
					if (LHandlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, AStatement.EventSpecifier, AStatement.OperatorName, AEventType.ToString(), LNode.EventSource.Name);
					LNode.EventHandler = LScalarType.EventHandlers[LHandlerIndex];
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidEventSource, AStatement.EventSourceSpecifier, ((ObjectEventSourceSpecifier)AStatement.EventSourceSpecifier).ObjectName, AEventType.ToString());
			}
			else if (AStatement.EventSourceSpecifier is ColumnEventSourceSpecifier)
			{
				LNode.EventSource = ResolveCatalogIdentifier(APlan, ((ColumnEventSourceSpecifier)AStatement.EventSourceSpecifier).TableVarName, true);
				Schema.TableVar LTableVar = LNode.EventSource as Schema.TableVar;
				if (LTableVar == null)
					throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, AStatement.EventSourceSpecifier);
				LNode.EventSourceColumnIndex = LTableVar.DataType.Columns.IndexOf(((ColumnEventSourceSpecifier)AStatement.EventSourceSpecifier).ColumnName);
				if (LNode.EventSourceColumnIndex < 0)
					throw new CompilerException(CompilerException.Codes.ColumnNameExpected, AStatement.EventSourceSpecifier);

				int LHandlerIndex = -1;
				if (LTableVar.Columns[LNode.EventSourceColumnIndex].HasHandlers())
					LHandlerIndex = LTableVar.Columns[LNode.EventSourceColumnIndex].EventHandlers.IndexOf(AStatement.OperatorName, AEventType);
				if (LHandlerIndex < 0)
					throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToColumnEvent, AStatement.EventSpecifier, AStatement.OperatorName, AEventType.ToString(), ((Schema.TableVar)LNode.EventSource).Columns[LNode.EventSourceColumnIndex].Name, LNode.EventSource.Name);
				LNode.EventHandler = LTableVar.Columns[LNode.EventSourceColumnIndex].EventHandlers[LHandlerIndex];
			}
			else
				throw new CompilerException(CompilerException.Codes.UnknownEventSourceSpecifierClass, AStatement.EventSourceSpecifier, AStatement.EventSourceSpecifier.GetType().Name);
			
			return LNode;
		}
		
		public static PlanNode CompileInvokeStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			// foreach event type
				// emit a AlterEventHandlerNode
			InvokeStatement LStatement = (InvokeStatement)AStatement;
			BlockNode LNode = new BlockNode();
			LNode.SetLineInfo(LStatement.LineInfo);
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeInsert) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.BeforeInsert));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterInsert) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.AfterInsert));
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeUpdate) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.BeforeUpdate));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterUpdate) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.AfterUpdate));
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeDelete) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.BeforeDelete));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterDelete) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.AfterDelete));
			if ((LStatement.EventSpecifier.EventType & EventType.Default) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.Default));
			if ((LStatement.EventSpecifier.EventType & EventType.Change) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.Change));
			if ((LStatement.EventSpecifier.EventType & EventType.Validate) != 0)
				LNode.Nodes.Add(EmitAlterEventHandlerNode(APlan, LStatement, EventType.Validate));
			return LNode;
		}
		
		public static PlanNode EmitDropEventHandlerNode(Plan APlan, DetachStatement AStatement, EventType AEventType)
		{
			// resolve the event target
			// resolve the operator specifier
			// find the trigger handler on the event target
			DropEventHandlerNode LNode = new DropEventHandlerNode();
			if (AStatement.EventSourceSpecifier is ObjectEventSourceSpecifier)
			{
				LNode.EventSource = ResolveCatalogIdentifier(APlan, ((ObjectEventSourceSpecifier)AStatement.EventSourceSpecifier).ObjectName, true);

				Schema.TableVar LTableVar = LNode.EventSource as Schema.TableVar;
				Schema.ScalarType LScalarType = LNode.EventSource as Schema.ScalarType;
				if (LTableVar != null)
				{
					int LHandlerIndex = -1;
					if (LTableVar.HasHandlers())
						LHandlerIndex = LTableVar.EventHandlers.IndexOf(AStatement.OperatorName, AEventType);
					if (LHandlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, AStatement.EventSpecifier, AStatement.OperatorName, AEventType.ToString(), LNode.EventSource.Name);
					LNode.EventHandler = LTableVar.EventHandlers[LHandlerIndex];
				}
				else if (LScalarType != null)
				{
					int LHandlerIndex = -1;
					if (LScalarType.HasHandlers())
						LHandlerIndex = LScalarType.EventHandlers.IndexOf(AStatement.OperatorName, AEventType);
					if (LHandlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, AStatement.EventSpecifier, AStatement.OperatorName, AEventType.ToString(), LNode.EventSource.Name);
					LNode.EventHandler = LScalarType.EventHandlers[LHandlerIndex];
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidEventSource, AStatement.EventSourceSpecifier, ((ObjectEventSourceSpecifier)AStatement.EventSourceSpecifier).ObjectName, AEventType.ToString());
			}
			else if (AStatement.EventSourceSpecifier is ColumnEventSourceSpecifier)
			{
				LNode.EventSource = ResolveCatalogIdentifier(APlan, ((ColumnEventSourceSpecifier)AStatement.EventSourceSpecifier).TableVarName, true);
				Schema.TableVar LTableVar = LNode.EventSource as Schema.TableVar;
				if (LTableVar == null)
					throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, AStatement.EventSourceSpecifier);
				LNode.EventSourceColumnIndex = LTableVar.DataType.Columns.IndexOf(((ColumnEventSourceSpecifier)AStatement.EventSourceSpecifier).ColumnName);
				if (LNode.EventSourceColumnIndex < 0)
					throw new CompilerException(CompilerException.Codes.ColumnNameExpected, AStatement.EventSourceSpecifier);

				int LHandlerIndex = -1;
				if (LTableVar.Columns[LNode.EventSourceColumnIndex].HasHandlers())
					LHandlerIndex = LTableVar.Columns[LNode.EventSourceColumnIndex].EventHandlers.IndexOf(AStatement.OperatorName, AEventType);
				if (LHandlerIndex < 0)
					throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToColumnEvent, AStatement.EventSpecifier, AStatement.OperatorName, AEventType.ToString(), ((Schema.TableVar)LNode.EventSource).Columns[LNode.EventSourceColumnIndex].Name, LNode.EventSource.Name);
				LNode.EventHandler = LTableVar.Columns[LNode.EventSourceColumnIndex].EventHandlers[LHandlerIndex];
			}
			else
				throw new CompilerException(CompilerException.Codes.UnknownEventSourceSpecifierClass, AStatement.EventSourceSpecifier, AStatement.EventSourceSpecifier.GetType().Name);
			
			return LNode;
		}
		
		public static PlanNode CompileDetachStatement(Plan APlan, Statement AStatement)
		{
			if (APlan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, AStatement);

			// foreach event type
				// emit a DropEventHandlerNode
			DetachStatement LStatement = (DetachStatement)AStatement;
			BlockNode LNode = new BlockNode();
			LNode.SetLineInfo(LStatement.LineInfo);
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeInsert) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.BeforeInsert));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterInsert) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.AfterInsert));
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeUpdate) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.BeforeUpdate));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterUpdate) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.AfterUpdate));
			if ((LStatement.EventSpecifier.EventType & EventType.BeforeDelete) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.BeforeDelete));
			if ((LStatement.EventSpecifier.EventType & EventType.AfterDelete) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.AfterDelete));
			if ((LStatement.EventSpecifier.EventType & EventType.Default) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.Default));
			if ((LStatement.EventSpecifier.EventType & EventType.Change) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.Change));
			if ((LStatement.EventSpecifier.EventType & EventType.Validate) != 0)
				LNode.Nodes.Add(EmitDropEventHandlerNode(APlan, LStatement, EventType.Validate));
			return LNode;
		}
		
		public static Schema.Object ResolveCatalogObjectSpecifier(Plan APlan, CatalogObjectSpecifier ASpecifier)
		{
			if (ASpecifier.IsOperator)
			{
				OperatorSpecifier LSpecifier = new OperatorSpecifier();
				LSpecifier.OperatorName = ASpecifier.ObjectName;
				LSpecifier.FormalParameterSpecifiers.AddRange(ASpecifier.FormalParameterSpecifiers);
				LSpecifier.Line = ASpecifier.Line;
				LSpecifier.LinePos = ASpecifier.LinePos;
				return ResolveOperatorSpecifier(APlan, LSpecifier);
			}
			else
				return ResolveCatalogIdentifier(APlan, ASpecifier.ObjectName, true);
		}
		
		public static PlanNode EmitUserSecurityNode(Plan APlan, string AInstruction, string ARightName, string AGrantee)
		{
			return 
				EmitCallNode
				(
					APlan, 
					AInstruction, 
					new PlanNode[]
					{
						EmitCallNode(APlan, "System.Name", new PlanNode[]{new ValueNode(APlan.DataTypes.SystemString, ARightName)}),
						#if USEISTRING
						new ValueNode(APlan.DataTypes.SystemIString, AGrantee)
						#else
						new ValueNode(APlan.DataTypes.SystemString, AGrantee)
						#endif
					}
				);
		}
		
		public static PlanNode EmitRoleSecurityNode(Plan APlan, string AInstruction, string ARightName, string AGrantee)
		{
			return
				EmitCallNode
				(
					APlan,
					AInstruction,
					new PlanNode[]
					{
						EmitCallNode(APlan, "System.Name", new PlanNode[]{new ValueNode(APlan.DataTypes.SystemString, ARightName)}),
						EmitCallNode(APlan, "System.Name", new PlanNode[]{new ValueNode(APlan.DataTypes.SystemString, AGrantee)})
					}
				);
		}

		public static PlanNode EmitRightNode(Plan APlan, RightStatementBase AStatement, string ARightName)
		{
			string LInstructionName = (AStatement is GrantStatement) ? "System.GrantRightTo" : ((AStatement is RevokeStatement) ? "System.RevokeRightFrom" : "System.RevertRightFor");
			switch (AStatement.GranteeType)
			{
				case GranteeType.User : return EmitUserSecurityNode(APlan, LInstructionName + "User", ARightName, AStatement.Grantee);
				case GranteeType.Role : return EmitRoleSecurityNode(APlan, LInstructionName + "Role", ARightName, AStatement.Grantee);
				default : throw new CompilerException(CompilerException.Codes.UnknownGranteeType, AStatement, AStatement.GranteeType.ToString());
			}
		}
		
		public static void EmitAllRightNodes(Plan APlan, RightStatementBase AStatement, Schema.CatalogObject AObject, BlockNode ANode)
		{
			string[] LRights = AObject.GetRights();
			for (int LIndex = 0; LIndex < LRights.Length; LIndex++)
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LRights[LIndex]));
		}
		
		public static void EmitUsageRightNodes(Plan APlan, RightStatementBase AStatement, Schema.CatalogObject AObject, BlockNode ANode)
		{
			if (AObject is Schema.TableVar)
			{
				Schema.TableVar LTableVar = (Schema.TableVar)AObject;
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LTableVar.GetRight(Schema.RightNames.Select)));
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LTableVar.GetRight(Schema.RightNames.Insert)));
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LTableVar.GetRight(Schema.RightNames.Update)));
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LTableVar.GetRight(Schema.RightNames.Delete)));
			}
			else if (AObject is Schema.Operator)
			{
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, ((Schema.Operator)AObject).GetRight(Schema.RightNames.Execute)));
			}
			else if (AObject is Schema.Device)
			{
				Schema.Device LDevice = (Schema.Device)AObject;
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LDevice.GetRight(Schema.RightNames.Read)));
				ANode.Nodes.Add(EmitRightNode(APlan, AStatement, LDevice.GetRight(Schema.RightNames.Write)));
			}
		}
		
		public static PlanNode CompileSecurityStatement(Plan APlan, Statement AStatement)
		{
			RightStatementBase LStatement = (RightStatementBase)AStatement;
			BlockNode LNode = new BlockNode();
			LNode.SetLineInfo(LStatement.LineInfo);
			if (LStatement.RightType == RightSpecifierType.All)
			{
				if (LStatement.Target == null)
					throw new CompilerException(CompilerException.Codes.InvalidAllSpecification, AStatement);

				Schema.CatalogObject LObject = (Schema.CatalogObject)ResolveCatalogObjectSpecifier(APlan, LStatement.Target);
				EmitAllRightNodes(APlan, LStatement, LObject, LNode);
						
				if (LObject is Schema.ScalarType)
				{
					Schema.ScalarType LScalarType = (Schema.ScalarType)LObject;
					if (LScalarType.EqualityOperator != null)
						EmitAllRightNodes(APlan, LStatement, LScalarType.EqualityOperator, LNode);

					if (LScalarType.ComparisonOperator != null)
						EmitAllRightNodes(APlan, LStatement, LScalarType.ComparisonOperator, LNode);
					
					if (LScalarType.IsSpecialOperator != null)
						EmitAllRightNodes(APlan, LStatement, LScalarType.IsSpecialOperator, LNode);
						
					foreach (Schema.Special LSpecial in LScalarType.Specials)
					{
						EmitAllRightNodes(APlan, LStatement, LSpecial.Selector, LNode);
						EmitAllRightNodes(APlan, LStatement, LSpecial.Comparer, LNode);
					}
						
					#if USETYPEINHERITANCE	
					foreach (Schema.Operator LOperator in LScalarType.ExplicitCastOperators)
						EmitAllRightNodes(APlan, LStatement, LOperator, LNode);
					#endif
						
					foreach (Schema.Representation LRepresentation in LScalarType.Representations)
					{
						EmitAllRightNodes(APlan, LStatement, LRepresentation.Selector, LNode);

						foreach (Schema.Property LProperty in LRepresentation.Properties)
						{
							EmitAllRightNodes(APlan, LStatement, LProperty.ReadAccessor, LNode);
							EmitAllRightNodes(APlan, LStatement, LProperty.WriteAccessor, LNode);
						}
					}						
				}
			}
			else if (LStatement.RightType == RightSpecifierType.Usage)
			{
				if (LStatement.Target == null)
					throw new CompilerException(CompilerException.Codes.InvalidAllSpecification, AStatement);

				Schema.CatalogObject LObject = (Schema.CatalogObject)ResolveCatalogObjectSpecifier(APlan, LStatement.Target);
				EmitUsageRightNodes(APlan, LStatement, LObject, LNode);
						
				if (LObject is Schema.ScalarType)
				{
					Schema.ScalarType LScalarType = (Schema.ScalarType)LObject;
					if (LScalarType.EqualityOperator != null)
						EmitUsageRightNodes(APlan, LStatement, LScalarType.EqualityOperator, LNode);
						
					if (LScalarType.ComparisonOperator != null)
						EmitUsageRightNodes(APlan, LStatement, LScalarType.ComparisonOperator, LNode);
						
					if (LScalarType.IsSpecialOperator != null)
						EmitUsageRightNodes(APlan, LStatement, LScalarType.IsSpecialOperator, LNode);
						
					foreach (Schema.Special LSpecial in LScalarType.Specials)
					{
						EmitUsageRightNodes(APlan, LStatement, LSpecial.Selector, LNode);
						EmitUsageRightNodes(APlan, LStatement, LSpecial.Comparer, LNode);
					}
						
					#if USETYPEINHERITANCE	
					foreach (Schema.Operator LOperator in LScalarType.ExplicitCastOperators)
						EmitUsageRightNodes(APlan, LStatement, LOperator, LNode);
					#endif

					foreach (Schema.Representation LRepresentation in LScalarType.Representations)
					{
						EmitUsageRightNodes(APlan, LStatement, LRepresentation.Selector, LNode);
						
						foreach (Schema.Property LProperty in LRepresentation.Properties)
						{
							EmitUsageRightNodes(APlan, LStatement, LProperty.ReadAccessor, LNode);
							EmitUsageRightNodes(APlan, LStatement, LProperty.WriteAccessor, LNode);
						}
					}						
				}
			}
			else
			{
				Schema.Object LObject = LStatement.Target != null ? ResolveCatalogObjectSpecifier(APlan, LStatement.Target) : null;
				foreach (RightSpecifier LRightSpecifier in LStatement.Rights)
				{
					string LRightName = LRightSpecifier.RightName;
					if (LObject != null)
						LRightName = LObject.Name + LRightName;
					
					LNode.Nodes.Add(EmitRightNode(APlan, LStatement, LRightName));
				}
			}

			if (LNode.NodeCount == 1)
				return LNode.Nodes[0];
			else
				return LNode;
		}

		public static PlanNode CompileBooleanExpression(Plan APlan, Expression AExpression)
		{
			return CompileTypedExpression(APlan, AExpression, APlan.DataTypes.SystemBoolean);
		}
		
		public static PlanNode CompileTableExpression(Plan APlan, Expression AExpression)
		{
			return CompileTypedExpression(APlan, AExpression, APlan.DataTypes.SystemTable);
		}
		
		public static PlanNode CompileTypedExpression(Plan APlan, Expression AExpression, Schema.IDataType ADataType)
		{
			return CompileTypedExpression(APlan, AExpression, ADataType, false);
		}
		
		public static PlanNode CompileTypedExpression(Plan APlan, Expression AExpression, Schema.IDataType ADataType, bool AAllowSourceSubset)
		{
			return EmitTypedNode(APlan, CompileExpression(APlan, AExpression), ADataType, AAllowSourceSubset);
		}
		
		public static PlanNode EmitTypedNode(Plan APlan, PlanNode ANode, Schema.IDataType ADataType)
		{
			return EmitTypedNode(APlan, ANode, ADataType, false);
		}
		
		public static PlanNode EmitTypedNode(Plan APlan, PlanNode ANode, Schema.IDataType ADataType, bool AAllowSourceSubset)
		{
			if (ANode.DataType == null)
				throw new CompilerException(CompilerException.Codes.ExpressionExpected, APlan.CurrentStatement());
			if (!ANode.DataType.Is(ADataType))
			{
				ConversionContext LContext = FindConversionPath(APlan, ANode.DataType, ADataType, AAllowSourceSubset);
				CheckConversionContext(APlan, LContext);
				ANode = ConvertNode(APlan, ANode, LContext);
			}
			
			return Upcast(APlan, ANode, ADataType);
		}
		
		public static PlanNode CompileExpression(Plan APlan, Statement AStatement)
		{
			return CompileExpression(APlan, AStatement is SelectStatement ? ((SelectStatement)AStatement).CursorDefinition : (Expression)AStatement);
		}
		
		public static PlanNode CompileExpression(Plan APlan, Expression AExpression)
		{
			return CompileExpression(APlan, AExpression, false);
		}
		
		#if ALLOWSTATEMENTSASEXPRESSIONS
		public static PlanNode CompileExpression(Plan APlan, Expression AExpression)
		{
			PlanNode LNode = InternalCompileExpression(APlan, AExpression);
			if (LNode.DataType == null)
				LNode.DataType = APlan.DataTypes.SystemScalar;
			return LNode;
		}
		
		public static PlanNode InternalCompileExpression(Plan APlan, Expression AExpression)
		#else
		public static PlanNode CompileExpression(Plan APlan, Expression AExpression, bool AIsStatementContext)
		#endif
		{
			APlan.PushStatement(AExpression);
			try
			{
				try
				{
					PlanNode LResult = null;
					switch (AExpression.GetType().Name)
					{
						case "UnaryExpression": LResult = CompileUnaryExpression(APlan, (UnaryExpression)AExpression); break;
						case "BinaryExpression": LResult = CompileBinaryExpression(APlan, (BinaryExpression)AExpression); break;
						case "BetweenExpression": LResult = CompileBetweenExpression(APlan, (BetweenExpression)AExpression); break;
						case "ValueExpression": LResult = CompileValueExpression(APlan, (ValueExpression)AExpression); break;
						case "ParameterExpression": LResult = CompileParameterExpression(APlan, (ParameterExpression)AExpression); break;
						case "TableIdentifierExpression":
						case "ColumnIdentifierExpression":
						case "ServerIdentifierExpression":
						case "VariableIdentifierExpression":
						case "IdentifierExpression": LResult = CompileIdentifierExpression(APlan, (IdentifierExpression)AExpression); break;
						case "QualifierExpression": LResult = CompileQualifierExpression(APlan, (QualifierExpression)AExpression, AIsStatementContext); break;
						case "CallExpression": LResult = CompileCallExpression(APlan, (CallExpression)AExpression); break;
						case "ListSelectorExpression": LResult = CompileListSelectorExpression(APlan, (ListSelectorExpression)AExpression); break;
						case "D4IndexerExpression": LResult = CompileIndexerExpression(APlan, (D4IndexerExpression)AExpression); break;
						case "IfExpression": LResult = CompileIfExpression(APlan, (IfExpression)AExpression); break;
						case "CaseExpression": LResult = CompileCaseExpression(APlan, (CaseExpression)AExpression); break;
						case "OnExpression": LResult = CompileOnExpression(APlan, (OnExpression)AExpression); break;
						case "RenameAllExpression": LResult = CompileRenameAllExpression(APlan, (RenameAllExpression)AExpression); break;
						case "IsExpression": LResult = CompileIsExpression(APlan, (IsExpression)AExpression); break;
						case "AsExpression": LResult = CompileAsExpression(APlan, (AsExpression)AExpression); break;
						#if CALCULESQUE
						case "NamedExpression": LResult = CompileNamedExpression(APlan, (NamedExpression)AExpression); break;
						#endif
						case "AdornExpression": LResult = CompileAdornExpression(APlan, (AdornExpression)AExpression); break;
						case "RedefineExpression": LResult = CompileRedefineExpression(APlan, (RedefineExpression)AExpression); break;
						case "TableSelectorExpression": LResult = CompileTableSelectorExpression(APlan, (TableSelectorExpression)AExpression); break;
						case "RowSelectorExpression": LResult = CompileRowSelectorExpression(APlan, (RowSelectorExpression)AExpression); break;
						case "CursorSelectorExpression": LResult = CompileCursorSelectorExpression(APlan, (CursorSelectorExpression)AExpression); break;
						case "CursorDefinition": LResult = CompileCursorDefinition(APlan, (CursorDefinition)AExpression); break;
						case "RowExtractorExpression": LResult = CompileRowExtractorExpression(APlan, (RowExtractorExpression)AExpression); break;
						case "ColumnExtractorExpression": LResult = CompileColumnExtractorExpression(APlan, (ColumnExtractorExpression)AExpression); break;
						case "RestrictExpression": LResult = CompileRestrictExpression(APlan, (RestrictExpression)AExpression); break;
						case "ProjectExpression": LResult = CompileProjectExpression(APlan, (ProjectExpression)AExpression); break;
						case "RemoveExpression": LResult = CompileRemoveExpression(APlan, (RemoveExpression)AExpression); break;
						case "ExtendExpression": LResult = CompileExtendExpression(APlan, (ExtendExpression)AExpression); break;
						case "SpecifyExpression": LResult = CompileSpecifyExpression(APlan, (SpecifyExpression)AExpression); break;
						case "RenameExpression": LResult = CompileRenameExpression(APlan, (RenameExpression)AExpression); break;
						case "AggregateExpression": LResult = CompileAggregateExpression(APlan, (AggregateExpression)AExpression); break;
						case "OrderExpression": LResult = CompileOrderExpression(APlan, (OrderExpression)AExpression); break;
						case "BrowseExpression": LResult = CompileBrowseExpression(APlan, (BrowseExpression)AExpression); break;
						case "QuotaExpression": LResult = CompileQuotaExpression(APlan, (QuotaExpression)AExpression); break;
						case "ExplodeColumnExpression": LResult = CompileExplodeColumnExpression(APlan, (ExplodeColumnExpression)AExpression); break;
						case "ExplodeExpression": LResult = CompileExplodeExpression(APlan, (ExplodeExpression)AExpression); break;
						case "UnionExpression": LResult = CompileUnionExpression(APlan, (UnionExpression)AExpression); break;
						case "IntersectExpression": LResult = CompileIntersectExpression(APlan, (IntersectExpression)AExpression); break;
						case "DifferenceExpression": LResult = CompileDifferenceExpression(APlan, (DifferenceExpression)AExpression); break;
						case "ProductExpression": LResult = CompileProductExpression(APlan, (ProductExpression)AExpression); break;
						#if USEDIVIDEEXPRESSION
						case "DivideExpression": LResult = CompileDivideExpression(APlan, (DivideExpression)AExpression); break;
						#endif
						case "HavingExpression": LResult = CompileHavingExpression(APlan, (HavingExpression)AExpression); break;
						case "WithoutExpression": LResult = CompileWithoutExpression(APlan, (WithoutExpression)AExpression); break;
						case "InnerJoinExpression": LResult = CompileInnerJoinExpression(APlan, (InnerJoinExpression)AExpression); break;
						case "LeftOuterJoinExpression": LResult = CompileLeftOuterJoinExpression(APlan, (LeftOuterJoinExpression)AExpression); break;
						case "RightOuterJoinExpression": LResult = CompileRightOuterJoinExpression(APlan, (RightOuterJoinExpression)AExpression); break;
						default: throw new CompilerException(CompilerException.Codes.UnknownExpressionClass, AExpression, AExpression.GetType().FullName);
					}

					if (!AIsStatementContext && (LResult.DataType == null))
						throw new CompilerException(CompilerException.Codes.ExpressionExpected, AExpression);

					return LResult;
				}
				catch (CompilerException LException)
				{
					if ((LException.Line == -1) && (AExpression.Line != -1))
					{
						LException.Line = AExpression.Line;
						LException.LinePos = AExpression.LinePos;
					}
					throw;
				}
				catch (Exception LException)
				{
					if (!(LException is DataphorException))
						throw new CompilerException(CompilerException.Codes.InternalError, ErrorSeverity.System, CompilerErrorLevel.NonFatal, AExpression, LException);
						
					if (!(LException is ILocatedException))
						throw new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.NonFatal, AExpression, LException, LException.Message);
					throw;
				}
			}
			finally
			{
				APlan.PopStatement();
			}
		}
		
		// PlanNode - (class determined via instruction lookup from the server catalog)
		//		Nodes[0] - AOperand
		public static PlanNode EmitUnaryNode(Plan APlan, string AInstruction, PlanNode AOperand)
		{
			return EmitCallNode(APlan, AInstruction, new PlanNode[]{AOperand});
		}
		
		public static PlanNode CompileUnaryExpression(Plan APlan, UnaryExpression AUnaryExpression)
		{
			PlanNode[] LArguments = new PlanNode[]{CompileExpression(APlan, AUnaryExpression.Expression)};
			OperatorBindingContext LContext = new OperatorBindingContext(AUnaryExpression, AUnaryExpression.Instruction, APlan.NameResolutionPath, SignatureFromArguments(LArguments), false);
			PlanNode LNode = EmitCallNode(APlan, LContext, LArguments);
			CheckOperatorResolution(APlan, LContext);
			return LNode;
		}
		
		public static PlanNode EmitBinaryNode(Plan APlan, PlanNode ALeftOperand, string AInstruction, PlanNode ARightOperand)
		{
			return EmitBinaryNode(APlan, new EmptyStatement(), ALeftOperand, AInstruction, ARightOperand);
		}
		
		// PlanNode - (class determined via instruction lookup from the server catalog)
		//		Nodes[0] = ALeftOperand
		//		Nodes[1] = ARightOperand
		public static PlanNode EmitBinaryNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, string AInstruction, PlanNode ARightOperand)
		{
			if (String.Compare(AInstruction, Instructions.Equal) == 0)
				return EmitEqualNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else if (String.Compare(AInstruction, Instructions.NotEqual) == 0)
				return EmitNotEqualNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else if (String.Compare(AInstruction, Instructions.Less) == 0)
				return EmitLessNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else if (String.Compare(AInstruction, Instructions.InclusiveLess) == 0)
				return EmitInclusiveLessNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else if (String.Compare(AInstruction, Instructions.Greater) == 0)
				return EmitGreaterNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else if (String.Compare(AInstruction, Instructions.InclusiveGreater) == 0)
				return EmitInclusiveGreaterNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else if (String.Compare(AInstruction, Instructions.Compare) == 0)
				return EmitCompareNode(APlan, AStatement, ALeftOperand, ARightOperand);
			else
				return EmitCallNode(APlan, AStatement, AInstruction, new PlanNode[]{ALeftOperand, ARightOperand});
		}
		
		public static PlanNode EmitEqualNode(Plan APlan, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			return EmitEqualNode(APlan, new EmptyStatement(), ALeftOperand, ARightOperand);
		}
		
		public static PlanNode EmitEqualNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			// ALeftOperand = ARightOperand ::=
				// ALeftOperand ?= ARightOperand = 0
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.Equal, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode = EmitBinaryNode(APlan, AStatement, EmitCallNode(APlan, AStatement, Instructions.Compare, new PlanNode[]{ALeftOperand, ARightOperand}), Instructions.Equal, new ValueNode(APlan.DataTypes.SystemInteger, 0));
			return LNode;
		}
		
		public static PlanNode EmitNotEqualNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			// ALeftOperand <> ARightOperand ::=
				// not(ALeftOperand = ARightOperand)
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.NotEqual, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode = EmitUnaryNode(APlan, Instructions.Not, EmitEqualNode(APlan, AStatement, ALeftOperand, ARightOperand));
			return LNode;
		}
		
		public static PlanNode EmitLessNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			// ALeftOperand < ARightOperand ::=
				// ALeftOperand ?= ARightOperand < 0
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.Less, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode = EmitBinaryNode(APlan, AStatement, EmitCallNode(APlan, AStatement, Instructions.Compare, new PlanNode[]{ALeftOperand, ARightOperand}), Instructions.Less, new ValueNode(APlan.DataTypes.SystemInteger, 0));
			return LNode;
		}
		
		public static PlanNode EmitInclusiveLessNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			// ALeftOperand <= ARightOperand ::=
				// ALeftOperand ?= ARightOperand <= 0
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.InclusiveLess, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode = EmitBinaryNode(APlan, AStatement, EmitCompareNode(APlan, AStatement, ALeftOperand, ARightOperand), Instructions.InclusiveLess, new ValueNode(APlan.DataTypes.SystemInteger, 0));
			return LNode;
		}
		
		public static PlanNode EmitGreaterNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			// ALeftOperand > ARightOperand ::=
				// ALeftOperand ?= ARightOperand > 0
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.Greater, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode = EmitBinaryNode(APlan, AStatement, EmitCompareNode(APlan, AStatement, ALeftOperand, ARightOperand), Instructions.Greater, new ValueNode(APlan.DataTypes.SystemInteger, 0));
			return LNode;
		}
		
		public static PlanNode EmitInclusiveGreaterNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			// ALeftOperand >= ARightOperand ::=
				// ALeftOperand ?= ARightOperand >= 0
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.InclusiveGreater, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode = EmitBinaryNode(APlan, AStatement, EmitCompareNode(APlan, AStatement, ALeftOperand, ARightOperand), Instructions.InclusiveGreater, new ValueNode(APlan.DataTypes.SystemInteger, 0));
			return LNode;
		}
		
		public static PlanNode EmitCompareNode(Plan APlan, Statement AStatement, PlanNode ALeftOperand, PlanNode ARightOperand)
		{
			//	ALeftOperand ?= ARightOperand ::= 
				//	if ALeftOperand = ARightOperand then 0 
				//		else if ALeftOperand < ARightOperand then -1 else 1
			PlanNode LNode = EmitCallNode(APlan, AStatement, Instructions.Compare, new PlanNode[]{ALeftOperand, ARightOperand}, false);
			if (LNode == null)
				LNode =
					EmitConditionNode
					(
						APlan,
						EmitCallNode
						(
							APlan,
							AStatement,
							Instructions.Equal,
							new PlanNode[]{ALeftOperand, ARightOperand}
						),
						new ValueNode(APlan.DataTypes.SystemInteger, 0),
						EmitConditionNode
						(
							APlan,
							EmitCallNode
							(
								APlan,
								AStatement,
								Instructions.Less,
								new PlanNode[]{ALeftOperand, ARightOperand}
							),
							new ValueNode(APlan.DataTypes.SystemInteger, -1),
							new ValueNode(APlan.DataTypes.SystemInteger, 1)
						)
					);
					
			return LNode;
		}
		
		public static PlanNode CompileBinaryExpression(Plan APlan, BinaryExpression ABinaryExpression)
		{
			PlanNode LLeftOperand = CompileExpression(APlan, ABinaryExpression.LeftExpression);
			PlanNode LRightOperand = CompileExpression(APlan, ABinaryExpression.RightExpression);
			return EmitBinaryNode(APlan, ABinaryExpression, LLeftOperand, ABinaryExpression.Instruction, LRightOperand);
		}
		
		public static PlanNode CompileBetweenExpression(Plan APlan, BetweenExpression ABetweenExpression)
		{
			PlanNode LValue = CompileExpression(APlan, ABetweenExpression.Expression);
			PlanNode LLowerBound = CompileExpression(APlan, ABetweenExpression.LowerExpression);
			PlanNode LUpperBound = CompileExpression(APlan, ABetweenExpression.UpperExpression);
			PlanNode LNode = EmitCallNode(APlan, ABetweenExpression, Instructions.Between, new PlanNode[]{LValue, LLowerBound, LUpperBound}, false);
			if (LNode == null)
			{
				LNode =
					Compiler.EmitBinaryNode
					(
						APlan,
						ABetweenExpression,
						Compiler.EmitBinaryNode
						(
							APlan,
							ABetweenExpression,
							LValue,
							Instructions.InclusiveGreater,
							LLowerBound
						),
						Instructions.And,
						Compiler.EmitBinaryNode
						(
							APlan,
							ABetweenExpression,
							LValue,
							Instructions.InclusiveLess,
							LUpperBound
						)
					);
			}
			return LNode;
		}
		
		public static ConversionContext FindConversionPath(Plan APlan, Schema.IDataType ASourceType, Schema.IDataType ATargetType)
		{
			return FindConversionPath(APlan, ASourceType, ATargetType, false);
		}
		
		public static ConversionContext FindConversionPath(Plan APlan, Schema.IDataType ASourceType, Schema.IDataType ATargetType, bool AAllowSourceSubset)
		{
			if ((ASourceType is Schema.ScalarType) && (ATargetType is Schema.ScalarType))					
				return FindScalarConversionPath(APlan, (Schema.ScalarType)ASourceType, (Schema.ScalarType)ATargetType);
			else if ((ASourceType is Schema.ITableType) && (ATargetType is Schema.ITableType))
				return FindTableConversionPath(APlan, (Schema.ITableType)ASourceType, (Schema.ITableType)ATargetType, AAllowSourceSubset);
			else if ((ASourceType is Schema.IRowType) && (ATargetType is Schema.IRowType))
				return FindRowConversionPath(APlan, (Schema.IRowType)ASourceType, (Schema.IRowType)ATargetType, AAllowSourceSubset);
			else
				return new ConversionContext(ASourceType, ATargetType);
		}
		
		/*
			CanConvert = ASourceType.Columns.Count == ATargetType.Columns.Count;
			if (CanConvert)
				foreach (Column in ASourceType)
					if (TargetType contains SourceColumn.Name)
						if (!SourceColumn.DataType.Is(TargetColumn.DataType)
							LContext = FindScalarConversionPath(SourceColumn.DataType, TargetColumn.DataType)
							CanConvert = LContext.CanConvert;
					else
						CanConvert = false;
		*/
		public static TableConversionContext FindTableConversionPath(Plan APlan, Schema.ITableType ASourceType, Schema.ITableType ATargetType, bool AAllowSourceSubset)
		{
			TableConversionContext LContext = new TableConversionContext(ASourceType, ATargetType);
			LContext.CanConvert = (AAllowSourceSubset ? (ASourceType.Columns.Count <= ATargetType.Columns.Count) : (ASourceType.Columns.Count == ATargetType.Columns.Count));

			if (LContext.CanConvert)
			{
				int LColumnIndex;
				foreach (Schema.Column LColumn in ASourceType.Columns)
				{
					LColumnIndex = ATargetType.Columns.IndexOfName(LColumn.Name);
					if (LColumnIndex >= 0)
					{
						if (!LColumn.DataType.Is(ATargetType.Columns[LColumnIndex].DataType))
						{
							ConversionContext LColumnContext = FindConversionPath(APlan, LColumn.DataType, ATargetType.Columns[LColumnIndex].DataType);
							LContext.ColumnConversions.Add(LColumn.Name, LColumnContext);
							LContext.CanConvert = LContext.CanConvert && LColumnContext.CanConvert;
						}
					}
					else
						LContext.CanConvert = false;
						
					if (!LContext.CanConvert)
						break;
				}
			}
			return LContext;
		}
		
		public static RowConversionContext FindRowConversionPath(Plan APlan, Schema.IRowType ASourceType, Schema.IRowType ATargetType, bool AAllowSourceSubset)
		{
			RowConversionContext LContext = new RowConversionContext(ASourceType, ATargetType);
			LContext.CanConvert = (AAllowSourceSubset ? (ASourceType.Columns.Count <= ATargetType.Columns.Count) : (ASourceType.Columns.Count == ATargetType.Columns.Count));

			if (LContext.CanConvert)
			{
				int LColumnIndex;
				foreach (Schema.Column LColumn in ASourceType.Columns)
				{
					LColumnIndex = ATargetType.Columns.IndexOfName(LColumn.Name);
					if (LColumnIndex >= 0)
					{
						if (!LColumn.DataType.Is(ATargetType.Columns[LColumnIndex].DataType))
						{
							ConversionContext LColumnContext = FindConversionPath(APlan, LColumn.DataType, ATargetType.Columns[LColumnIndex].DataType);
							LContext.ColumnConversions.Add(LColumn.Name, LColumnContext);
							LContext.CanConvert = LContext.CanConvert && LColumnContext.CanConvert;
						}
					}
					else
						LContext.CanConvert = false;
						
					if (!LContext.CanConvert)
						break;
				}
			}
			return LContext;
		}
		
		/*
			Find the least narrowing, shortest unambiguous acyclic conversion path from scalar type A to scalar type B in the directed graph of implicit conversions.
			
			Path - An ordered list of conversions such that an expression for converting a value of type A to a value of type B can be determined.
					Each path has a NarrowingScore which indicates the number of narrowing conversions that take place along the path.
					A narrowing score of 0 indicates no narrowing has taken place, and each narrowing conversion decreases this score by 1.
			CurrentPath - The path from the source scalar type A to the scalar type currently being considered
			Paths - The set of successful conversion paths from the source scalar type to the target scalar type that have been discovered so far
			BestNarrowingScore - The best score found so far in the set of successful conversion paths (initialized to the minimum integer)
			ShortestPath - The shortest path among the paths with the best narrowing scores discovered so far
			
			Visit(type D)
				if (D is TargetScalarType)
					Paths.Add(CurrentPath)
				else
					foreach (Conversion C in D)
						if (!CurrentPath.Contains(C))
							CurrentPath.Add(C)
							if ((CurrentPath.NarrowingScore > BestNarrowingScore) || ((CurrentPath.NarrowingScore == BestNarrowingScore) && (CurrentPath.Length < ShortestPathLength)))
								Visit(C.TargetScalarType)
							CurrentPath.Remove(C)
		*/
		
		public static void TraceScalarConversionPath(Plan APlan, ScalarConversionContext AContext, Schema.ScalarType AScalarType)
		{
			if (AScalarType.Is(AContext.TargetType))
			{
				Schema.ScalarConversionPath LPath = new Schema.ScalarConversionPath();
				LPath.AddRange(AContext.CurrentPath);
				AContext.Paths.Add(LPath);
			}
			else
			{
				foreach (Schema.Conversion LConversion in AScalarType.ImplicitConversions)
				{
					if (!AContext.CurrentPath.Contains(LConversion.TargetScalarType))
					{
						AContext.CurrentPath.Add(LConversion);
						if ((AContext.CurrentPath.NarrowingScore > AContext.Paths.BestNarrowingScore) || ((AContext.CurrentPath.NarrowingScore == AContext.Paths.BestNarrowingScore) && AContext.CurrentPath.Count < AContext.Paths.ShortestLength))
							TraceScalarConversionPath(APlan, AContext, LConversion.TargetScalarType);
						AContext.CurrentPath.RemoveAt(AContext.CurrentPath.Count - 1);
					}
				}
			}
		}
		
		// Attempts to discover a conversion path from any supertype of ASourceType to any subtype of ATargetType.
		public static ScalarConversionContext FindScalarConversionPath(Plan APlan, Schema.ScalarType ASourceType, Schema.ScalarType ATargetType)
		{
			ScalarConversionContext LContext = new ScalarConversionContext(ASourceType, ATargetType);
			
			if (!LContext.CanConvert)
			{
				#if USECONVERSIONPATHCACHE
				lock (APlan.Catalog.ConversionPathCache)
				{
					Schema.ScalarConversionPath LPath = APlan.Catalog.ConversionPathCache[ASourceType, ATargetType];
					if (LPath != null)
						LContext.Paths.Add(LPath);
					else
					{
				#endif
						TraceScalarConversionPath(APlan, LContext, ASourceType);
				#if USECONVERSIONPATHCACHE
						if (LContext.CanConvert)
							APlan.Catalog.ConversionPathCache.Add(ASourceType, ATargetType, LContext.BestPath);
					}
				}
				#endif
			
				#if USETYPEINHERITANCE
				if (!LContext.CanConvert)
				{
					ScalarConversionContext LParentContext;
					foreach (Schema.ScalarType LParentType in ASourceType.ParentTypes)
					{
						LParentContext = FindScalarConversionPath(LParentType, ATargetType);
						if (LParentContext.CanConvert)
							return LParentContext;
					}
				}
				#endif
			}
			
			return LContext;
		}
		
		public static void CheckConversionContext(Plan APlan, ConversionContext AContext)
		{
			CheckConversionContext(APlan, AContext, true);
		}
		
		public static void CheckConversionContext(Plan APlan, ConversionContext AContext, bool AThrow)
		{
			if (AContext is ScalarConversionContext)
			{
				ScalarConversionContext LContext = (ScalarConversionContext)AContext;
				if (LContext.BestPath == null)
				{
					if (!LContext.CanConvert)
					{
						switch (LContext.BestPaths.Count)
						{
							case 0 : 
								if (AThrow)
									throw new CompilerException(CompilerException.Codes.NoConversion, APlan.CurrentStatement(), AContext.SourceType.Name, AContext.TargetType.Name);
								else
									APlan.Messages.Add(new CompilerException(CompilerException.Codes.NoConversion, APlan.CurrentStatement(), AContext.SourceType.Name, AContext.TargetType.Name));
							break;
							case 1 : break;
							default :
								StringCollection LConversions = new StringCollection();
								foreach (Schema.ScalarConversionPath LPath in LContext.BestPaths)
									if (LPath.Count == LContext.Paths.ShortestLength)
										LConversions.Add(LPath.ToString());
										
								if (LConversions.Count > 1)
									if (AThrow)
										throw new CompilerException(CompilerException.Codes.AmbiguousConversion, APlan.CurrentStatement(), AContext.SourceType.Name, AContext.TargetType.Name, ExceptionUtility.StringsToCommaList(LConversions));
									else
										APlan.Messages.Add(new CompilerException(CompilerException.Codes.AmbiguousConversion, APlan.CurrentStatement(), AContext.SourceType.Name, AContext.TargetType.Name, ExceptionUtility.StringsToCommaList(LConversions)));
							break;
						}
					}
				}
				#if REPORTNARROWINGCONVERSIONWARNINGS
				else
				{
					foreach (Schema.Conversion LConversion in LContext.BestPath)
						if (LConversion.IsNarrowing && !APlan.SuppressWarnings && !APlan.InTypeOfContext)
							APlan.Messages.Add(new CompilerException(CompilerException.Codes.NarrowingConversion, CompilerErrorLevel.Warning, APlan.CurrentStatement(), LConversion.SourceScalarType.Name, LConversion.TargetScalarType.Name));
				}
				#endif
			}
			else if (AContext is TableConversionContext)
			{
				TableConversionContext LContext = (TableConversionContext)AContext;
				foreach (DictionaryEntry LEntry in LContext.ColumnConversions)
					CheckConversionContext(APlan, (ConversionContext)LEntry.Value, false);
				
				if (!LContext.CanConvert)
					if (AThrow)
					{					
						Schema.ITableType LSourceTableType = (Schema.ITableType)AContext.SourceType;
						Schema.ITableType LTargetTableType = (Schema.ITableType)AContext.TargetType;
						for (int LIndex = 0; LIndex < (LSourceTableType.Columns.Count > LTargetTableType.Columns.Count ? LSourceTableType.Columns.Count : LTargetTableType.Columns.Count); LIndex++)
						{
							if ((LIndex < LSourceTableType.Columns.Count) && !LTargetTableType.Columns.Contains(LSourceTableType.Columns[LIndex]))
								throw new CompilerException(CompilerException.Codes.TargetTableTypeMissingColumn, APlan.CurrentStatement(), LSourceTableType.Columns[LIndex].Name);
							else if ((LIndex < LTargetTableType.Columns.Count) && !LSourceTableType.Columns.Contains(LTargetTableType.Columns[LIndex]))
								throw new CompilerException(CompilerException.Codes.SourceTableTypeMissingColumn, APlan.CurrentStatement(), LTargetTableType.Columns[LIndex].Name);
						}
					}
					else
					{
						Schema.ITableType LSourceTableType = (Schema.ITableType)AContext.SourceType;
						Schema.ITableType LTargetTableType = (Schema.ITableType)AContext.TargetType;
						for (int LIndex = 0; LIndex < (LSourceTableType.Columns.Count > LTargetTableType.Columns.Count ? LSourceTableType.Columns.Count : LTargetTableType.Columns.Count); LIndex++)
						{
							if ((LIndex < LSourceTableType.Columns.Count) && !LTargetTableType.Columns.Contains(LSourceTableType.Columns[LIndex]))
								APlan.Messages.Add(new CompilerException(CompilerException.Codes.TargetTableTypeMissingColumn, APlan.CurrentStatement(), LSourceTableType.Columns[LIndex].Name));
							else if ((LIndex < LTargetTableType.Columns.Count) && !LSourceTableType.Columns.Contains(LTargetTableType.Columns[LIndex]))
								APlan.Messages.Add(new CompilerException(CompilerException.Codes.SourceTableTypeMissingColumn, APlan.CurrentStatement(), LTargetTableType.Columns[LIndex].Name));
						}
					}
			}
			else if (AContext is RowConversionContext)
			{
				RowConversionContext LContext = (RowConversionContext)AContext;
				foreach (DictionaryEntry LEntry in LContext.ColumnConversions)
					CheckConversionContext(APlan, (ConversionContext)LEntry.Value, false);
				
				if (!LContext.CanConvert)
					if (AThrow)
					{
						Schema.IRowType LSourceRowType = (Schema.IRowType)AContext.SourceType;
						Schema.IRowType LTargetRowType = (Schema.IRowType)AContext.TargetType;
						for (int LIndex = 0; LIndex < (LSourceRowType.Columns.Count > LTargetRowType.Columns.Count ? LSourceRowType.Columns.Count : LTargetRowType.Columns.Count); LIndex++)
						{
							if ((LIndex < LSourceRowType.Columns.Count) && !LTargetRowType.Columns.Contains(LSourceRowType.Columns[LIndex]))
								throw new CompilerException(CompilerException.Codes.TargetRowTypeMissingColumn, APlan.CurrentStatement(), LSourceRowType.Columns[LIndex].Name);
							else if ((LIndex < LTargetRowType.Columns.Count) && !LSourceRowType.Columns.Contains(LTargetRowType.Columns[LIndex]))
								throw new CompilerException(CompilerException.Codes.SourceRowTypeMissingColumn, APlan.CurrentStatement(), LTargetRowType.Columns[LIndex].Name);
						}
					}
					else
					{
						Schema.IRowType LSourceRowType = (Schema.IRowType)AContext.SourceType;
						Schema.IRowType LTargetRowType = (Schema.IRowType)AContext.TargetType;
						for (int LIndex = 0; LIndex < (LSourceRowType.Columns.Count > LTargetRowType.Columns.Count ? LSourceRowType.Columns.Count : LTargetRowType.Columns.Count); LIndex++)
						{
							if ((LIndex < LSourceRowType.Columns.Count) && !LTargetRowType.Columns.Contains(LSourceRowType.Columns[LIndex]))
								APlan.Messages.Add(new CompilerException(CompilerException.Codes.TargetRowTypeMissingColumn, APlan.CurrentStatement(), LSourceRowType.Columns[LIndex].Name));
							else if ((LIndex < LTargetRowType.Columns.Count) && !LSourceRowType.Columns.Contains(LTargetRowType.Columns[LIndex]))
								APlan.Messages.Add(new CompilerException(CompilerException.Codes.SourceRowTypeMissingColumn, APlan.CurrentStatement(), LTargetRowType.Columns[LIndex].Name));
						}
					}
			}
			else
			{
				if (!AContext.CanConvert)
					if (AThrow)
						throw new CompilerException(CompilerException.Codes.NoConversion, APlan.CurrentStatement(), AContext.SourceType.Name, AContext.TargetType.Name);
					else
						APlan.Messages.Add(new CompilerException(CompilerException.Codes.NoConversion, APlan.CurrentStatement(), AContext.SourceType.Name, AContext.TargetType.Name));
			}
		}
		
		public static PlanNode ConvertNode(Plan APlan, PlanNode ASourceNode, ConversionContext AContext)
		{
			if (ASourceNode.DataType.Is(AContext.TargetType))
				return ASourceNode;
				
			if ((ASourceNode.DataType is Schema.ScalarType) && (AContext.TargetType is Schema.ScalarType))
				return ConvertScalarNode(APlan, ASourceNode, (ScalarConversionContext)AContext);
			else if ((ASourceNode.DataType is Schema.ITableType) && (AContext.TargetType is Schema.ITableType))
				return ConvertTableNode(APlan, ASourceNode, (TableConversionContext)AContext);
			else if ((ASourceNode.DataType is Schema.IRowType) && (AContext.TargetType is Schema.IRowType))
				return ConvertRowNode(APlan, ASourceNode, (RowConversionContext)AContext);
			else
				return ASourceNode;
		}
		
		public static PlanNode ConvertScalarNode(Plan APlan, PlanNode ASourceNode, ScalarConversionContext AContext)
		{
			PlanNode LNode = ASourceNode;
			if (AContext.BestPath != null)
				for (int LIndex = 0; LIndex < AContext.BestPath.Count; LIndex++)
				{
					APlan.AttachDependency(AContext.BestPath[LIndex]);
					LNode = BuildCallNode(APlan, new EmptyStatement(), AContext.BestPath[LIndex].Operator, new PlanNode[]{Upcast(APlan, LNode, AContext.BestPath[LIndex].Operator.Operands[0].DataType)});
					LNode.DetermineDataType(APlan);
					LNode.DetermineCharacteristics(APlan);
				}
			return LNode;
		}
		
		public static PlanNode ConvertTableNode(Plan APlan, PlanNode ASourceNode, TableConversionContext AContext)
		{
			NamedColumnExpressions LExpressions = new NamedColumnExpressions();
			foreach (Schema.Column LSourceColumn in AContext.SourceType.Columns)
			{
				Schema.Column LTargetColumn = AContext.TargetType.Columns[LSourceColumn];
				if (!LSourceColumn.DataType.Is(LTargetColumn.DataType))
				{
					Expression LExpression = new IdentifierExpression(LSourceColumn.Name);
					ConversionContext LContext = (ConversionContext)AContext.ColumnConversions[LSourceColumn.Name];
					ScalarConversionContext LScalarContext = LContext as ScalarConversionContext;
					if ((LScalarContext != null) && (LScalarContext.BestPath != null) && (LScalarContext.BestPath.Count > 0))
					{
						for (int LIndex = 0; LIndex < LScalarContext.BestPath.Count; LIndex++)
						{
							APlan.AttachDependency(LScalarContext.BestPath[LIndex]);
							LExpression = new CallExpression(LScalarContext.BestPath[LIndex].Operator.OperatorName, new Expression[]{LExpression});
						}
						LExpressions.Add(new NamedColumnExpression(LExpression, LTargetColumn.Name));
					}
				}
			}
			if (LExpressions.Count > 0)
				return EmitRedefineNode(APlan, ASourceNode, LExpressions);
			else
				return ASourceNode;
		}
		
		public static PlanNode ConvertRowNode(Plan APlan, PlanNode ASourceNode, RowConversionContext AContext)
		{
			NamedColumnExpressions LExpressions = new NamedColumnExpressions();
			foreach (Schema.Column LSourceColumn in AContext.SourceType.Columns)
			{
				Schema.Column LTargetColumn = AContext.TargetType.Columns[LSourceColumn];
				if (!LSourceColumn.DataType.Is(LTargetColumn.DataType))
				{
					Expression LExpression = new IdentifierExpression(LSourceColumn.Name);
					ConversionContext LContext = (ConversionContext)AContext.ColumnConversions[LSourceColumn.Name];
					ScalarConversionContext LScalarContext = LContext as ScalarConversionContext;
					if ((LScalarContext != null) && (LScalarContext.BestPath != null) && (LScalarContext.BestPath.Count > 0))
					{
						for (int LIndex = 0; LIndex < LScalarContext.BestPath.Count; LIndex++)
						{
							APlan.AttachDependency(LScalarContext.BestPath[LIndex]);
							LExpression = new CallExpression(LScalarContext.BestPath[LIndex].Operator.OperatorName, new Expression[]{LExpression});
						}
						LExpressions.Add(new NamedColumnExpression(LExpression, LTargetColumn.Name));
					}
				}
			}
			if (LExpressions.Count > 0)
				return EmitRedefineNode(APlan, ASourceNode, LExpressions);
			else
				return ASourceNode;
		}
		
		// Given ADataType, and ATargetDataType guaranteed to be a super type of ADataType, find the casting path to ATargetDataType
		public static bool FindCastingPath(Schema.ScalarType ADataType, Schema.ScalarType ATargetDataType, ArrayList ACastingPath)
		{
			ACastingPath.Add(ADataType);
			if (ADataType.Equals(ATargetDataType))
				return true;
			else
			{
				#if USETYPEINHERITANCE
				foreach (Schema.ScalarType LParentType in ADataType.ParentTypes)
					if (FindCastingPath(LParentType, ATargetDataType, ACastingPath))
						return true;
				#endif
				ACastingPath.Remove(ADataType);
				return false;
			}
		}
		
		public static PlanNode DowncastScalar(Plan APlan, PlanNode APlanNode, Schema.ScalarType ATargetDataType)
		{
			if (!ATargetDataType.Equals(APlan.DataTypes.SystemScalar) && !APlanNode.DataType.Equals(APlan.DataTypes.SystemScalar))
			{
				ArrayList LCastingPath = new ArrayList();
				if (!FindCastingPath(ATargetDataType, (Schema.ScalarType)APlanNode.DataType, LCastingPath))
					throw new CompilerException(CompilerException.Codes.CastingPathNotFound, APlan.CurrentStatement(), ATargetDataType.Name, APlanNode.DataType.Name);
					
				// Remove the last element, it is the data type of APlanNode.
				LCastingPath.RemoveAt(LCastingPath.Count - 1);
				
				PlanNode LPlanNode;
				Schema.ScalarType LTargetDataType;
				for (int LIndex = LCastingPath.Count - 1; LIndex >= 0; LIndex--)
				{
					LTargetDataType = (Schema.ScalarType)LCastingPath[LIndex];
					if ((LTargetDataType.ClassDefinition != null) && (((Schema.ScalarType)APlanNode.DataType).ClassDefinition != null) && !Object.ReferenceEquals(APlan.ValueManager.GetConveyor((Schema.ScalarType)APlanNode.DataType).GetType(), APlan.ValueManager.GetConveyor(LTargetDataType).GetType()))
					{
						LPlanNode = EmitCallNode(APlan, LTargetDataType.Name, new PlanNode[]{APlanNode}, false);
						if (LPlanNode == null)
							throw new CompilerException(CompilerException.Codes.PhysicalCastOperatorNotFound, APlan.CurrentStatement(), APlanNode.DataType.Name, LTargetDataType.Name);
						APlanNode = LPlanNode;
					}
				}
			}
			return APlanNode;
		}
		
		public static PlanNode UpcastScalar(Plan APlan, PlanNode APlanNode, Schema.ScalarType ATargetDataType)
		{
			// If the target data type is not scalar or alpha
			if (!ATargetDataType.Equals(APlan.DataTypes.SystemScalar))
			{
				ArrayList LCastingPath = new ArrayList();
				if (!FindCastingPath((Schema.ScalarType)APlanNode.DataType, ATargetDataType, LCastingPath))
					throw new CompilerException(CompilerException.Codes.CastingPathNotFound, APlan.CurrentStatement(), APlanNode.DataType.Name, ATargetDataType.Name);
					
				// Remove the first element, it is the data type of APlanNode.
				LCastingPath.RemoveAt(0);
				
				PlanNode LPlanNode;
				Schema.ScalarType LTargetDataType;
				for (int LIndex = 0; LIndex < LCastingPath.Count; LIndex++)
				{
					LTargetDataType = (Schema.ScalarType)LCastingPath[LIndex];
					if ((LTargetDataType.ClassDefinition != null) && (((Schema.ScalarType)APlanNode.DataType).ClassDefinition != null) && !Object.ReferenceEquals(APlan.ValueManager.GetConveyor((Schema.ScalarType)APlanNode.DataType).GetType(), APlan.ValueManager.GetConveyor(LTargetDataType).GetType()))
					{
						LPlanNode = EmitCallNode(APlan, LTargetDataType.Name, new PlanNode[]{APlanNode}, false);
						if (LPlanNode == null)
							throw new CompilerException(CompilerException.Codes.PhysicalCastOperatorNotFound, APlan.CurrentStatement(), APlanNode.DataType.Name, LTargetDataType.Name);
						APlanNode = LPlanNode;
					}
				}
			}
			return APlanNode;
		}
		
		public static PlanNode DowncastTable(Plan APlan, PlanNode APlanNode, Schema.ITableType ATargetDataType)
		{
			// TODO: DowncastTable
			return APlanNode;
		}
		
		public static PlanNode UpcastTable(Plan APlan, PlanNode APlanNode, Schema.ITableType ATargetDataType)
		{
			// TODO: UpcastTable
			return APlanNode;
		}
		
		public static PlanNode DowncastRow(Plan APlan, PlanNode APlanNode, Schema.IRowType ATargetDataType)
		{
			// TODO: DowncastRow
			return APlanNode;
		}
		
		public static PlanNode UpcastRow(Plan APlan, PlanNode APlanNode, Schema.IRowType ATargetDataType)
		{
			// TODO: UpcastRow
			return APlanNode;
		}
		
		public static PlanNode DowncastList(Plan APlan, PlanNode APlanNode, Schema.IListType ATargetDataType)
		{
			// TODO: DowncastList
			return APlanNode;
		}
		
		public static PlanNode UpcastList(Plan APlan, PlanNode APlanNode, Schema.IListType ATargetDataType)
		{
			// TODO: UpcastList
			return APlanNode;
		}
		
		// Given APlanNode and ATargetDataType that is guaranteed to be a sub type of the data type of APlanNode,
		// provide physical conversions if necessary
		public static PlanNode Downcast(Plan APlan, PlanNode APlanNode, Schema.IDataType ATargetDataType)
		{
			#if USETYPEINHERITANCE
			if (!ATargetDataType.Equals(APlan.DataTypes.SystemGeneric) && !APlanNode.DataType.Equals(APlan.DataTypes.SystemGeneric))
			{
				if (APlanNode.DataType is Schema.IScalarType)
					return DowncastScalar(APlan, APlanNode, (Schema.ScalarType)ATargetDataType);
				else if (APlanNode.DataType is Schema.ITableType)
					return DowncastTable(APlan, APlanNode, (Schema.ITableType)ATargetDataType);
				else if (APlanNode.DataType is Schema.IRowType)
					return DowncastRow(APlan, APlanNode, (Schema.IRowType)ATargetDataType);
				else if (APlanNode.DataType is Schema.IListType)
					return DowncastList(APlan, APlanNode, (Schema.IListType)ATargetDataType);
				else
					return APlanNode;
			}
			else
				return APlanNode;
			#else
			return APlanNode;
			#endif
		}

		// Given APlanNode and ATargetDataType that is guaranteed to be a super type of the data type of APlanNode,
		// provide physical conversions if necessary
		public static PlanNode Upcast(Plan APlan, PlanNode APlanNode, Schema.IDataType ATargetDataType)
		{
			#if USETYPEINHERITANCE
			if (!ATargetDataType.Equals(new Schema.GenericType()))
			{
				if (APlanNode.DataType is Schema.IScalarType)
					return UpcastScalar(APlan, APlanNode, (Schema.ScalarType)ATargetDataType);
				else if (APlanNode.DataType is Schema.ITableType)
					return UpcastTable(APlan, APlanNode, (Schema.ITableType)ATargetDataType);
				else if (APlanNode.DataType is Schema.IRowType)
					return UpcastRow(APlan, APlanNode, (Schema.IRowType)ATargetDataType);
				else if (APlanNode.DataType is Schema.IListType)
					return UpcastList(APlan, APlanNode, (Schema.IListType)ATargetDataType);
				else
					return APlanNode;
			}
			else
				return APlanNode;
			#else
			return APlanNode;
			#endif
		}
		
		public static PlanNode BuildCallNode(Plan APlan, OperatorBindingContext AContext, PlanNode[] AArguments)
		{
			PlanNode[] LArguments = new PlanNode[AArguments.Length];
			if (!AContext.Matches.IsExact)
			{
				ConversionContext LContext;
				OperatorMatch LPartialMatch = AContext.Matches.Match;
				for (int LIndex = 0; LIndex < LArguments.Length; LIndex++)
				{
					LContext = LPartialMatch.ConversionContexts[LIndex];
					if (LContext != null)
						LArguments[LIndex] = ConvertNode(APlan, AArguments[LIndex], LContext);
					else
						LArguments[LIndex] = AArguments[LIndex];
					LArguments[LIndex] = Upcast(APlan, LArguments[LIndex], AContext.Operator.Operands[LIndex].DataType);
				}
			}
			else
			{
				for (int LIndex = 0; LIndex < LArguments.Length; LIndex++)
					LArguments[LIndex] = Upcast(APlan, AArguments[LIndex], AContext.Operator.Operands[LIndex].DataType);
			}

			return BuildCallNode(APlan, AContext.Statement, AContext.Operator, LArguments);
		}
		
		public static PlanNode BuildCallNode(Plan APlan, Statement AStatement, Schema.Operator AOperator, PlanNode[] AArguments)
		{
			if (AOperator is Schema.AggregateOperator)
				throw new CompilerException(CompilerException.Codes.InvalidAggregateInvocation, AStatement, AOperator.Name);
				
			APlan.CheckRight(AOperator.GetRight(Schema.RightNames.Execute));
			
			if (AOperator.ShouldRecompile)
				RecompileOperator(APlan, AOperator);
			
			APlan.AttachDependency(AOperator);
			
			if ((AStatement != null) && (AStatement.Modifiers != null))
			{
				APlan.SetIsLiteral(Convert.ToBoolean(LanguageModifiers.GetModifier(AStatement.Modifiers, "IsLiteral", AOperator.IsLiteral.ToString())));
				APlan.SetIsFunctional(Convert.ToBoolean(LanguageModifiers.GetModifier(AStatement.Modifiers, "IsFunctional", AOperator.IsFunctional.ToString())));
				APlan.SetIsDeterministic(Convert.ToBoolean(LanguageModifiers.GetModifier(AStatement.Modifiers, "IsDeterministic", AOperator.IsDeterministic.ToString())));
				APlan.SetIsRepeatable(Convert.ToBoolean(LanguageModifiers.GetModifier(AStatement.Modifiers, "IsRepeatable", AOperator.IsRepeatable.ToString())));
				APlan.SetIsNilable(Convert.ToBoolean(LanguageModifiers.GetModifier(AStatement.Modifiers, "IsNilable", AOperator.IsNilable.ToString())));
			}
			else
			{
				APlan.SetIsLiteral(AOperator.IsLiteral);
				APlan.SetIsFunctional(AOperator.IsFunctional);
				APlan.SetIsDeterministic(AOperator.IsDeterministic);
				APlan.SetIsRepeatable(AOperator.IsRepeatable);
				APlan.SetIsNilable(AOperator.IsNilable);
			}

			PlanNode LNode;
			if (AOperator.Block.ClassDefinition != null)
			{
				APlan.CheckClassDependency(AOperator.Block.ClassDefinition);
				LNode = (PlanNode)APlan.Catalog.ClassLoader.CreateObject(AOperator.Block.ClassDefinition, null);
			}
			else
			{
				LNode = new CallNode();
			}
			
			if (LNode is InstructionNodeBase)
				((InstructionNodeBase)LNode).Operator = AOperator;
				
			if (LNode.IsBreakable && (AStatement != null))
				LNode.SetLineInfo(AStatement.LineInfo);

			LNode.DataType = AOperator.ReturnDataType;
			for (int LIndex = 0; LIndex < AArguments.Length; LIndex++)
			{
				PlanNode LArgumentNode = AArguments[LIndex];
				if (LArgumentNode.DataType is Schema.ITableType)
				{
					if (AOperator.Block.ClassDefinition != null)
					{
						// This is a host-implemented operator and should be given table nodes
						LArgumentNode = EnsureTableNode(APlan, LArgumentNode);
					}
					else
					{
						// This is a D4-implemented operator and should not be given table nodes
						LArgumentNode = EnsureTableValueNode(APlan, LArgumentNode);
					}
				}
				
				if (AOperator.Operands[LIndex].Modifier == Modifier.Var)
				{
					PlanNode LActualArgumentNode = LArgumentNode;
					if (LActualArgumentNode is ParameterNode)
						LActualArgumentNode = LActualArgumentNode.Nodes[0];
						
					if (LActualArgumentNode is StackReferenceNode)
						APlan.Symbols.SetIsModified(((StackReferenceNode)LActualArgumentNode).Location);
					else if (LActualArgumentNode is TableVarNode)
						((TableVarNode)LActualArgumentNode).TableVar.IsModified = true;
				}
				LNode.Nodes.Add(LArgumentNode);
			}

			if (AStatement != null)
				LNode.Modifiers = AStatement.Modifiers;			
			return LNode;
		}
		
		public static Schema.Signature SignatureFromArguments(PlanNode[] AArguments)
		{
			Schema.SignatureElement[] LSignatureElements = new Schema.SignatureElement[AArguments.Length];
			for (int LIndex = 0; LIndex < AArguments.Length; LIndex++)
			{
				if (AArguments[LIndex] is ParameterNode)
					LSignatureElements[LIndex] = new Schema.SignatureElement(AArguments[LIndex].DataType, ((ParameterNode)AArguments[LIndex]).Modifier);
				else
					LSignatureElements[LIndex] = new Schema.SignatureElement(AArguments[LIndex].DataType);
			}
				
			return new Schema.Signature(LSignatureElements);
		}
		
		public static PlanNode FindCallNode(Plan APlan, Statement AStatement, string AInstruction, PlanNode[] AArguments)
		{
			OperatorBindingContext LContext = new OperatorBindingContext(AStatement, AInstruction, APlan.NameResolutionPath, SignatureFromArguments(AArguments), false);
			PlanNode LNode = FindCallNode(APlan, LContext, AArguments);
			CheckOperatorResolution(APlan, LContext);
			return LNode;
		}

		/// <summary>The main overload of FindCallNode which all other overloads call.  All call resolutions funnel through this method.</summary>		
		public static PlanNode FindCallNode(Plan APlan, OperatorBindingContext AContext, PlanNode[] AArguments)
		{
			ResolveOperator(APlan, AContext);
			if (AContext.Operator != null)
			{
				if (Convert.ToBoolean(MetaData.GetTag(AContext.Operator.MetaData, "DAE.IsDeprecated", "False")) && !APlan.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedOperator, CompilerErrorLevel.Warning, AContext.Operator.DisplayName));
					
				if (AContext.IsExact)
					return BuildCallNode(APlan, AContext.Statement, AContext.Operator, AArguments);
				else
					return BuildCallNode(APlan, AContext, AArguments);
			}
			else
				return null;
		}
		
		public static PlanNode EmitCallNode(Plan APlan, string AInstruction, PlanNode[] AArguments)
		{
			return EmitCallNode(APlan, new EmptyStatement(), AInstruction, AArguments, true, false);
		}
		
		public static PlanNode EmitCallNode(Plan APlan, string AInstruction, PlanNode[] AArguments, bool AMustResolve)
		{
			return EmitCallNode(APlan, new EmptyStatement(), AInstruction, AArguments, AMustResolve, false);
		}
		
		public static PlanNode EmitCallNode(Plan APlan, string AInstruction, PlanNode[] AArguments, bool AMustResolve, bool AIsExact)
		{
			return EmitCallNode(APlan, new EmptyStatement(), AInstruction, AArguments, AMustResolve, AIsExact);
		}
		
		public static PlanNode EmitCallNode(Plan APlan, Statement AStatement, string AInstruction, PlanNode[] AArguments)
		{
			return EmitCallNode(APlan, AStatement, AInstruction, AArguments, true, false);
		}
		
		public static PlanNode EmitCallNode(Plan APlan, Statement AStatement, string AInstruction, PlanNode[] AArguments, bool AMustResolve)
		{
			return EmitCallNode(APlan, AStatement, AInstruction, AArguments, AMustResolve, false);
		}
		
		public static PlanNode EmitCallNode(Plan APlan, Statement AStatement, string AInstruction, PlanNode[] AArguments, bool AMustResolve, bool AIsExact)
		{
			foreach (PlanNode LArgument in AArguments)
				if (LArgument.DataType == null)
					throw new CompilerException(CompilerException.Codes.ExpressionExpected, AStatement);

			OperatorBindingContext LContext = new OperatorBindingContext(AStatement, AInstruction, APlan.NameResolutionPath, SignatureFromArguments(AArguments), AIsExact);
			PlanNode LPlanNode = EmitCallNode(APlan, LContext, AArguments);
			if (AMustResolve)
				CheckOperatorResolution(APlan, LContext);
			return LPlanNode;
		}
		
		// PlanNode - (class determined via instruction lookup from the server catalog)
		//		Nodes[0..ArgumentCount - 1] = PlanNodes for each argument in the call
		public static PlanNode EmitCallNode(Plan APlan, OperatorBindingContext AContext, PlanNode[] AArguments)
		{
			PlanNode LNode = FindCallNode(APlan, AContext, AArguments);
			if (LNode != null)
			{
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
			}
			return LNode;
		}
		
		public static Schema.Signature AggregateSignatureFromArguments(PlanNode ATargetNode, string[] AColumnNames, bool AMustResolve)
		{
			Schema.ITableType LTargetType = (Schema.ITableType)ATargetNode.DataType;
			int[] LColumnIndexes = new int[AColumnNames.Length];
			Schema.SignatureElement[] LElements = new Schema.SignatureElement[AColumnNames.Length];
			for (int LIndex = 0; LIndex < AColumnNames.Length; LIndex++)
			{
				LColumnIndexes[LIndex] = LTargetType.Columns.IndexOf(AColumnNames[LIndex]);
				if (LColumnIndexes[LIndex] >= 0)
					LElements[LIndex] = new Schema.SignatureElement(LTargetType.Columns[LColumnIndexes[LIndex]].DataType);
				else
				{
					if (AMustResolve)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ColumnNotFound, AColumnNames[LIndex]);
					return null;
				}
			}
			
			return new Schema.Signature(LElements);
		}
		
		public static AggregateCallNode BuildAggregateCallNode(Plan APlan, Statement AStatement, Schema.AggregateOperator AOperator, PlanNode ATargetNode, string[] AColumnNames, OrderColumnDefinitions AOrderColumns)
		{
			ATargetNode = EnsureTableNode(APlan, ATargetNode);
			Schema.ITableType LTargetType = (Schema.ITableType)ATargetNode.DataType;
			int[] LColumnIndexes = new int[AColumnNames.Length];
			for (int LIndex = 0; LIndex < AColumnNames.Length; LIndex++)
				LColumnIndexes[LIndex] = LTargetType.Columns.IndexOf(AColumnNames[LIndex]);
			
			APlan.CheckRight(AOperator.GetRight(Schema.RightNames.Execute));
			
			if (AOperator.ShouldRecompile)
				RecompileAggregateOperator(APlan, AOperator);
			
			APlan.AttachDependency(AOperator);
			AggregateCallNode LNode = new AggregateCallNode();
			LNode.Operator = AOperator;
			LNode.AggregateColumnIndexes = LColumnIndexes;
			LNode.ValueNames = new string[LColumnIndexes.Length];
			for (int LIndex = 0; LIndex < LColumnIndexes.Length; LIndex++)
				LNode.ValueNames[LIndex] = AOperator.Operands[LIndex].Name;

			// TargetDataType determination (Upcast and Convert)
			Schema.ITableType LExpectedType = (Schema.ITableType)new Schema.TableType();
			for (int LIndex = 0; LIndex < LTargetType.Columns.Count; LIndex++)
			{
				if (((IList)LColumnIndexes).Contains(LIndex))
					LExpectedType.Columns.Add(new Schema.Column(LTargetType.Columns[LIndex].Name, AOperator.Operands[((IList)LColumnIndexes).IndexOf(LIndex)].DataType));
				else
					LExpectedType.Columns.Add(new Schema.Column(LTargetType.Columns[LIndex].Name, LTargetType.Columns[LIndex].DataType));
			}
			
			if (!ATargetNode.DataType.Is(LExpectedType))
			{
				ConversionContext LContext = FindConversionPath(APlan, ATargetNode.DataType, LExpectedType);
				CheckConversionContext(APlan, LContext);
				ATargetNode = ConvertNode(APlan, ATargetNode, LContext);

				// Redetermine aggregate column indexes using the new target data type (could be diferrent indexes)
				LTargetType = (Schema.ITableType)ATargetNode.DataType;
				LColumnIndexes = new int[AColumnNames.Length];
				for (int LIndex = 0; LIndex < AColumnNames.Length; LIndex++)
					LColumnIndexes[LIndex] = LTargetType.Columns.IndexOf(AColumnNames[LIndex]);
				LNode.AggregateColumnIndexes = LColumnIndexes;
			}
			
			if (AOrderColumns != null)
				ATargetNode = Compiler.EmitOrderNode(APlan, ATargetNode, Compiler.CompileOrderColumnDefinitions(APlan, ((TableNode)ATargetNode).TableVar, AOrderColumns, null, false), false);
				
			LNode.Nodes.Add(Upcast(APlan, ATargetNode, LExpectedType));
			
			if (AOperator.Initialization.ClassDefinition != null)
			{
				APlan.CheckClassDependency(AOperator.Initialization.ClassDefinition);
				PlanNode LInitializationNode = (PlanNode)APlan.Catalog.ClassLoader.CreateObject(AOperator.Initialization.ClassDefinition, null);
				LInitializationNode.DetermineCharacteristics(APlan);
				LNode.Nodes.Add(LInitializationNode);
			}
			else
				LNode.Nodes.Add(AOperator.Initialization.BlockNode);
				
			if (AOperator.Aggregation.ClassDefinition != null)
			{
				APlan.CheckClassDependency(AOperator.Aggregation.ClassDefinition);
				PlanNode LAggregationNode = (PlanNode)APlan.Catalog.ClassLoader.CreateObject(AOperator.Aggregation.ClassDefinition, null);
				LAggregationNode.DetermineCharacteristics(APlan);
				LNode.Nodes.Add(LAggregationNode);
			}
			else
				LNode.Nodes.Add(AOperator.Aggregation.BlockNode);
			
			if (AOperator.Finalization.ClassDefinition != null)
			{
				APlan.CheckClassDependency(AOperator.Finalization.ClassDefinition);
				PlanNode LFinalizationNode = (PlanNode)APlan.Catalog.ClassLoader.CreateObject(AOperator.Finalization.ClassDefinition, null);
				LFinalizationNode.DetermineCharacteristics(APlan);
				LNode.Nodes.Add(LFinalizationNode);
			}
			else
				LNode.Nodes.Add(AOperator.Finalization.BlockNode);
			
			LNode.DataType = AOperator.ReturnDataType;
			
			if (AStatement != null)
				LNode.Modifiers = AStatement.Modifiers;
				
			return LNode;
		}
		
		public static AggregateCallNode FindAggregateCallNode(Plan APlan, OperatorBindingContext AContext, PlanNode ATargetNode, string[] AColumnNames, OrderColumnDefinitions AOrderColumns)
		{
			ResolveOperator(APlan, AContext);
			if (AContext.Operator is Schema.AggregateOperator)
				return BuildAggregateCallNode(APlan, AContext.Statement, (Schema.AggregateOperator)AContext.Operator, ATargetNode, AColumnNames, AOrderColumns);
			else
			{
				if (AContext.Operator != null)
				{
					APlan.ReleaseCatalogLock(AContext.Operator);
					AContext.Operator = null;
				}
				return null;
			}
		}
		
		public static AggregateCallNode EmitAggregateCallNode(Plan APlan, OperatorBindingContext AContext, PlanNode ATargetNode, string[] AColumnNames, OrderColumnDefinitions AOrderColumns)
		{
			AggregateCallNode LNode = FindAggregateCallNode(APlan, AContext, ATargetNode, AColumnNames, AOrderColumns);
			if (LNode != null)
			{
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
			}
			return LNode;
		}
		
		public static PlanNode CompileCallExpression(Plan APlan, CallExpression ACallExpression)
		{
			OperatorBindingContext LContext;
			PlanNode[] LPlanNodes = new PlanNode[ACallExpression.Expressions.Count];
			for (int LIndex = 0; LIndex < ACallExpression.Expressions.Count; LIndex++)
			{
				if (ACallExpression.Expressions.Count == 1)
				{
					ACallExpression.Expressions[LIndex] = CollapseColumnExtractorExpression(ACallExpression.Expressions[LIndex]);
					if (ACallExpression.Expressions[LIndex] is ColumnExtractorExpression)
					{
						ColumnExtractorExpression LColumnExpression = (ColumnExtractorExpression)ACallExpression.Expressions[LIndex];
						PlanNode LExtractionTargetNode = CompileExpression(APlan, LColumnExpression.Expression);
						if (LExtractionTargetNode.DataType is Schema.ITableType)
						{
							LExtractionTargetNode = EnsureTableNode(APlan, LExtractionTargetNode);
							string[] LColumnNames = new string[LColumnExpression.Columns.Count];
							for (int LColumnIndex = 0; LColumnIndex < LColumnExpression.Columns.Count; LColumnIndex++)
								LColumnNames[LColumnIndex] = LColumnExpression.Columns[LColumnIndex].ColumnName;
							LContext = new OperatorBindingContext(ACallExpression, ACallExpression.Identifier, APlan.NameResolutionPath, AggregateSignatureFromArguments(LExtractionTargetNode, LColumnNames, true), false);
							AggregateCallNode LAggregateNode = EmitAggregateCallNode(APlan, LContext, LExtractionTargetNode, LColumnNames, LColumnExpression.HasByClause ? LColumnExpression.OrderColumns : null);
							if (LAggregateNode != null)
							{
								int LStackDisplacement = LAggregateNode.Operator.Initialization.StackDisplacement + LAggregateNode.Operator.Operands.Count + 1; // add 1 to account for the result variable
								for (int LStackIndex = 0; LStackIndex < LStackDisplacement; LStackIndex++)
									APlan.Symbols.Push(new Symbol(String.Empty, APlan.DataTypes.SystemScalar));
								try
								{
									LAggregateNode.Nodes[0] = EnsureTableNode(APlan, CompileExpression(APlan, (Expression)LAggregateNode.Nodes[0].EmitStatement(EmitMode.ForCopy)));
								}
								finally
								{
									for (int LStackIndex = 0; LStackIndex < LStackDisplacement; LStackIndex++)
										APlan.Symbols.Pop();
								}

								return LAggregateNode;
							}
							else
								CheckOperatorResolution(APlan, LContext);
						}
						
						LPlanNodes[LIndex] = EmitColumnExtractorNode(APlan, ((ColumnExtractorExpression)ACallExpression.Expressions[LIndex]), LExtractionTargetNode);
					}
					else
					{
						LPlanNodes[LIndex] = CompileExpression(APlan, ACallExpression.Expressions[LIndex]);
						if (LPlanNodes[LIndex].DataType is Schema.ITableType)
						{
							string[] LColumnNames = new string[]{};
							Schema.Signature LCallSignature = AggregateSignatureFromArguments(LPlanNodes[LIndex], LColumnNames, false);
							if (LCallSignature != null)
							{
								LContext = new OperatorBindingContext(ACallExpression, ACallExpression.Identifier, APlan.NameResolutionPath, LCallSignature, true);
								AggregateCallNode LAggregateNode = EmitAggregateCallNode(APlan, LContext, LPlanNodes[LIndex], LColumnNames, null);
								if (LAggregateNode != null)
								{
									int LStackDisplacement = LAggregateNode.Operator.Initialization.StackDisplacement + 1; // add 1 to account for the result variable
									for (int LStackIndex = 0; LStackIndex < LStackDisplacement; LStackIndex++)
										APlan.Symbols.Push(new Symbol(String.Empty, APlan.DataTypes.SystemScalar));
									try
									{
										LAggregateNode.Nodes[0] = EnsureTableNode(APlan, CompileExpression(APlan, (Expression)LAggregateNode.Nodes[0].EmitStatement(EmitMode.ForCopy)));
									}
									finally
									{
										for (int LStackIndex = 0; LStackIndex < LStackDisplacement; LStackIndex++)
											APlan.Symbols.Pop();
									}
									return LAggregateNode;
								}
							}
						}
					}
				}
				else
					LPlanNodes[LIndex] = CompileExpression(APlan, ACallExpression.Expressions[LIndex]);
			}
			
			LContext = new OperatorBindingContext(ACallExpression, ACallExpression.Identifier, APlan.NameResolutionPath, SignatureFromArguments(LPlanNodes), false);
			PlanNode LNode = EmitCallNode(APlan, LContext, LPlanNodes);
			CheckOperatorResolution(APlan, LContext);
			return LNode;
		}

		public static ListNode EmitListNode(Plan APlan, Schema.IListType AListType, PlanNode[] AElements)
		{
			ListNode LNode = new ListNode();
			LNode.DataType = AListType;
			for (int LIndex = 0; LIndex < AElements.Length; LIndex++)
				LNode.Nodes.Add(AElements[LIndex]);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileListSelectorExpression(Plan APlan, ListSelectorExpression AExpression)
		{
			Schema.IListType LListType = null;
			if (AExpression.TypeSpecifier != null)
			{
				Schema.IDataType LDataType = CompileTypeSpecifier(APlan, AExpression.TypeSpecifier);
				if (!(LDataType is Schema.IListType))
					throw new CompilerException(CompilerException.Codes.ListTypeExpected, AExpression.TypeSpecifier);
				LListType = (Schema.IListType)LDataType;
			}

			PlanNode[] LPlanNodes = new PlanNode[AExpression.Expressions.Count];
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LListType != null)
					LPlanNodes[LIndex] = CompileTypedExpression(APlan, AExpression.Expressions[LIndex], LListType.ElementType);
				else
				{
					LPlanNodes[LIndex] = CompileExpression(APlan, AExpression.Expressions[LIndex]);
					LListType = new Schema.ListType(LPlanNodes[LIndex].DataType);
				}
			}
			
			if (LListType == null)
				LListType = new Schema.ListType(new Schema.GenericType());
			
			return 
				EmitListNode
				(
					APlan, 
					LListType,
					LPlanNodes
				);
		}
		
		protected static void GeneratePermutations(int ANumber, ArrayList APermutations)
		{
			int[] LValue = new int[ANumber];
			for (int LIndex = 0; LIndex < LValue.Length; LIndex++)
				LValue[LIndex] = 0;
			Visit(-1, 0, LValue, APermutations);
		}
		
		protected static void Visit(int ALevel, int ACurrent, int[] AValue, ArrayList APermutations)
		{
			ALevel = ALevel + 1;
			AValue[ACurrent] = ALevel;
			
			if (ALevel == AValue.Length)
			{
				int[] LPermutation = new int[AValue.Length];
				for (int LIndex = 0; LIndex < AValue.Length; LIndex++)
					LPermutation[LIndex] = AValue[LIndex];
				APermutations.Add(LPermutation);
			}
			else
			{
				for (int LIndex = 0; LIndex < AValue.Length; LIndex++)
					if (AValue[LIndex] == 0)
						Visit(ALevel, LIndex, AValue, APermutations);
			}
			
			ALevel = ALevel - 1;
			AValue[ACurrent] = 0;
		}

		public static PlanNode CompileIndexerExpression(Plan APlan, D4IndexerExpression AExpression)
		{
			PlanNode LTargetNode = CompileExpression(APlan, AExpression.Expression);
			
			if (LTargetNode.DataType is Schema.TableType)
			{
				TableNode LTableNode = EnsureTableNode(APlan, LTargetNode);
				Schema.Key LResolvedKey = null;

				PlanNode[] LTerms = new PlanNode[AExpression.Expressions.Count];
				Schema.SignatureElement[] LIndexerSignature = new Schema.SignatureElement[AExpression.Expressions.Count];
				for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
				{
					LTerms[LIndex] = CompileExpression(APlan, AExpression.Expressions[LIndex]);
					LIndexerSignature[LIndex] = new Schema.SignatureElement(LTerms[LIndex].DataType);
				}
				
				if (AExpression.HasByClause || (AExpression.Expressions.Count == 0))
				{
					LResolvedKey = new Schema.Key();
					if (AExpression.HasByClause)
						foreach (KeyColumnDefinition LColumn in AExpression.ByClause)
							LResolvedKey.Columns.Add(LTableNode.TableVar.Columns[LColumn.ColumnName]);
					
					bool LResolvedKeyUnique = false;
					foreach (Schema.Key LKey in LTableNode.TableVar.Keys)
					{
						if (LResolvedKey.Columns.IsSupersetOf(LKey.Columns))
						{
							LResolvedKeyUnique = true;
							break;
						}
					}
					
					if (!LResolvedKeyUnique && !APlan.SuppressWarnings && !APlan.InTypeOfContext)
						APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression, CompilerErrorLevel.Warning, AExpression));
				}
				else
				{
					if (LTerms.Length > 0)
					{
						if (LTerms.Length > 5)
							throw new CompilerException(CompilerException.Codes.TooManyTermsForImplicitTableIndexer, AExpression);
							
						// construct operators for each potential key
						OperatorSignatures LOperatorSignatures = new OperatorSignatures(null);
						for (int LIndex = 0; LIndex < LTableNode.TableVar.Keys.Count; LIndex++)
							if (LTableNode.TableVar.Keys[LIndex].Columns.Count == LTerms.Length)
							{
								Schema.Operator LOperator = new Schema.Operator(String.Format("{0}", LIndex.ToString()));
								foreach (Schema.TableVarColumn LColumn in LTableNode.TableVar.Keys[LIndex].Columns)
									LOperator.Operands.Add(new Schema.Operand(LOperator, LColumn.Name, LColumn.DataType));
								if (LOperatorSignatures.Contains(LOperator.Signature))
									throw new CompilerException(CompilerException.Codes.PotentiallyAmbiguousImplicitTableIndexer, AExpression);
								LOperatorSignatures.Add(new OperatorSignature(LOperator));
							}

						// If there is at least one potentially matching key						
						if (LOperatorSignatures.Count > 0)
						{
							// Compute permutations of the signature
							ArrayList LPermutations = new ArrayList();
							GeneratePermutations(LTerms.Length, LPermutations);
							
							ArrayList LSignatures = new ArrayList();
							for (int LIndex = 0; LIndex < LPermutations.Count; LIndex++)
							{
								int[] LPermutation = (int[])LPermutations[LIndex];
								Schema.SignatureElement[] LPermutationSignature = new Schema.SignatureElement[LPermutation.Length];
								for (int LPIndex = 0; LPIndex < LPermutation.Length; LPIndex++)
									LPermutationSignature[LPIndex] = LIndexerSignature[LPermutation[LPIndex] - 1];
								LSignatures.Add(new Schema.Signature(LPermutationSignature));
							}
							
							// Resolve each permutation signature against the potential keys, recording partial and exact matches
							OperatorBindingContext[] LContexts = new OperatorBindingContext[LSignatures.Count];
							for (int LIndex = 0; LIndex < LSignatures.Count; LIndex++)
							{
								LContexts[LIndex] = new OperatorBindingContext(new CallExpression(), "Key", APlan.NameResolutionPath, (Schema.Signature)LSignatures[LIndex], false);
								LOperatorSignatures.Resolve(APlan, LContexts[LIndex]);
							}
							
							// Determine a matching signature and resolved key
							int LBestNarrowingScore = Int32.MinValue;
							int LShortestPathLength = Int32.MaxValue;
							int LSignatureIndex = -1;
							for (int LIndex = 0; LIndex < LContexts.Length; LIndex++)
							{
								if (LContexts[LIndex].Matches.IsMatch)
								{
									if (LSignatureIndex == -1)
									{
										LSignatureIndex = LIndex;
										LBestNarrowingScore = LContexts[LIndex].Matches.Match.NarrowingScore;
										LShortestPathLength = LContexts[LIndex].Matches.Match.PathLength;
									}
									else
									{
										if (LContexts[LIndex].Matches.Match.NarrowingScore > LBestNarrowingScore)
										{
											LSignatureIndex = LIndex;
											LBestNarrowingScore = LContexts[LIndex].Matches.Match.NarrowingScore;
											LShortestPathLength = LContexts[LIndex].Matches.Match.PathLength;
										}
										else if (LContexts[LIndex].Matches.Match.NarrowingScore == LBestNarrowingScore)
										{
											if (LContexts[LIndex].Matches.Match.PathLength < LShortestPathLength)
											{
												LSignatureIndex = LIndex;
												LShortestPathLength = LContexts[LIndex].Matches.Match.PathLength;
											}
											else if (LContexts[LIndex].Matches.Match.PathLength == LShortestPathLength)
												throw new CompilerException(CompilerException.Codes.AmbiguousTableIndexerKey, AExpression);
										}
									}
								}
							}
							
							if (LSignatureIndex >= 0)
							{
								int[] LPermutation = (int[])LPermutations[LSignatureIndex];
								
								Schema.Key LSignatureKey = LTableNode.TableVar.Keys[Convert.ToInt32(LContexts[LSignatureIndex].Matches.Match.Signature.Operator.OperatorName)];
								
								LResolvedKey = new Schema.Key();
								AExpression.HasByClause = true;
								for (int LIndex = 0; LIndex < LIndexerSignature.Length; LIndex++)
								{
									Schema.TableVarColumn LResolvedKeyColumn = LSignatureKey.Columns[((IList)LPermutation).IndexOf(LIndex + 1)];
									LResolvedKey.Columns.Add(LResolvedKeyColumn);
									AExpression.ByClause.Add(new KeyColumnDefinition(LResolvedKeyColumn.Name));
								}
							}
						}
					}
					
					if (LResolvedKey == null)
						throw new CompilerException(CompilerException.Codes.UnresolvedTableIndexerKey, AExpression);
				}
					
				if (LResolvedKey.Columns.Count != AExpression.Expressions.Count)
					throw new CompilerException(CompilerException.Codes.InvalidTableIndexerKey, AExpression);
					
				AExpression.Expression = (Expression)LTableNode.EmitStatement(EmitMode.ForCopy);
					
				Expression LCondition = null;
				
				for (int LIndex = 0; LIndex < LResolvedKey.Columns.Count; LIndex++)
				{
					AExpression.Expressions[LIndex] = (Expression)LTerms[LIndex].EmitStatement(EmitMode.ForCopy);
					
					BinaryExpression LColumnCondition = 
						new BinaryExpression
						(
							new IdentifierExpression(Schema.Object.EnsureRooted(Schema.Object.Qualify(LResolvedKey.Columns[LIndex].Name, "X"))), 
							Instructions.Equal, 
							AExpression.Expressions[LIndex]
						);
					
					if (LCondition != null)
						LCondition = new BinaryExpression(LCondition, Instructions.And, LColumnCondition);
					else
						LCondition = LColumnCondition;
				}
				
				if (LCondition != null)
					LTableNode = (TableNode)EmitRestrictNode(APlan, EmitRenameNode(APlan, LTableNode, "X"), LCondition);

				ExtractRowNode LNode;
				bool LSaveSuppressWarnings = APlan.SuppressWarnings;
				APlan.SuppressWarnings = true;
				try
				{				
					LNode = (ExtractRowNode)EmitRowExtractorNode(APlan, AExpression, LTableNode);
				}
				finally
				{
					APlan.SuppressWarnings = LSaveSuppressWarnings;
				}

				LNode.IndexerExpression = AExpression;

				if (LCondition != null)
				{
					RenameColumnExpressions LRenameColumns = new RenameColumnExpressions();
					foreach (Schema.Column LColumn in ((Schema.IRowType)LNode.DataType).Columns)
						LRenameColumns.Add(new RenameColumnExpression(LColumn.Name, Schema.Object.Dequalify(LColumn.Name)));
					RowRenameNode LRowRenameNode = (RowRenameNode)EmitRenameNode(APlan, LNode, LRenameColumns);
					LRowRenameNode.ShouldEmit = false;
					return LRowRenameNode;
				}

				return LNode;
			}
			else
			{
				if (AExpression.Expressions.Count != 1)
					throw new CompilerException(CompilerException.Codes.InvalidIndexerExpression, AExpression);
					
				PlanNode[] LArguments = new PlanNode[]{LTargetNode, CompileExpression(APlan, AExpression.Indexer)};
				OperatorBindingContext LContext = new OperatorBindingContext(AExpression, Instructions.Indexer, APlan.NameResolutionPath, SignatureFromArguments(LArguments), false);
				PlanNode LNode = EmitCallNode(APlan, LContext, LArguments);
				CheckOperatorResolution(APlan, LContext);
				return LNode;
			}
		}
		
		// ConditionNode
		//		Node[0] = AIfNode (conditional expression)
		//		Node[1] = ATrueNode (true expression)
		//		Node[2] = AFalseNode (false expression)
		//
		//	The conditional must evaluate to a logical data type.
		//	The false expression must be the same type as the true expression, and is the return type of the
		//  overall expression.  This operator cannot be overloaded.
		public static PlanNode EmitConditionNode(Plan APlan, PlanNode AIfNode, PlanNode ATrueNode, PlanNode AFalseNode)
		{
			ConditionNode LNode = new ConditionNode();
			LNode.Nodes.Add(AIfNode);
			LNode.Nodes.Add(ATrueNode);
			LNode.Nodes.Add(AFalseNode);
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileIfExpression(Plan APlan, IfExpression AExpression)
		{
			PlanNode LIfNode = CompileBooleanExpression(APlan, AExpression.Expression);
			PlanNode LTrueNode = CompileExpression(APlan, AExpression.TrueExpression);
			PlanNode LFalseNode = CompileExpression(APlan, AExpression.FalseExpression);
			return EmitConditionNode(APlan, LIfNode, LTrueNode, LFalseNode);
		}
		
		public static PlanNode CompileCaseItemExpression(Plan APlan, CaseExpression AExpression, int AIndex)
		{
			PlanNode LWhenNode = CompileExpression(APlan, AExpression.CaseItems[AIndex].WhenExpression);
			if (AExpression.Expression != null)
			{
				PlanNode LCompareNode = CompileExpression(APlan, AExpression.Expression);
				LWhenNode = EmitBinaryNode(APlan, AExpression, LCompareNode, Instructions.Equal, LWhenNode);
			}
				
			PlanNode LThenNode = CompileExpression(APlan, AExpression.CaseItems[AIndex].ThenExpression);
			PlanNode LElseNode;
			if (AIndex >= AExpression.CaseItems.Count - 1)
				LElseNode = CompileExpression(APlan, ((CaseElseExpression)AExpression.ElseExpression).Expression);
			else
				LElseNode = CompileCaseItemExpression(APlan, AExpression, AIndex + 1);
			return EmitConditionNode(APlan, LWhenNode, LThenNode, LElseNode);
		}
		
		// Case expressions are converted into a series of equivalent if expressions.
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileCaseExpression(Plan APlan, CaseExpression AExpression)
		{
			// case [<case expression>] when <when expression> then <then expression> ... else <else expression> end
			// convert the case expression into equivalent if then else expressions
			// if [<case expression> =] <when expression> then <then expression> else ... <else expression>
			
			//return CompileCaseItemExpression(APlan, AExpression, 0);

			CaseExpression LExpression = (CaseExpression)AExpression;
			if (LExpression.Expression != null)
			{
				SelectedConditionedCaseNode LNode = new SelectedConditionedCaseNode();
				
				PlanNode LSelectorNode = CompileExpression(APlan, LExpression.Expression);
				PlanNode LEqualNode = null;
				Symbol LSelectorVar = new Symbol(LSelectorNode.DataType);
				LNode.Nodes.Add(LSelectorNode);

				foreach (CaseItemExpression LCaseItemExpression in LExpression.CaseItems)
				{
					ConditionedCaseItemNode LCaseItemNode = new ConditionedCaseItemNode();
					PlanNode LWhenNode = CompileTypedExpression(APlan, LCaseItemExpression.WhenExpression, LSelectorNode.DataType);
					LCaseItemNode.Nodes.Add(LWhenNode);
					if (LEqualNode == null)
					{
						APlan.Symbols.Push(LSelectorVar);
						try
						{
							APlan.Symbols.Push(new Symbol(LWhenNode.DataType));
							try
							{
								LEqualNode = EmitBinaryNode(APlan, new StackReferenceNode(LSelectorNode.DataType, 1, true), Instructions.Equal, new StackReferenceNode(LWhenNode.DataType, 0, true));
								LNode.Nodes.Add(LEqualNode);
							}
							finally
							{
								APlan.Symbols.Pop();
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}

					LCaseItemNode.Nodes.Add(CompileExpression(APlan, LCaseItemExpression.ThenExpression));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
				
				if (LExpression.ElseExpression != null)
				{
					ConditionedCaseItemNode LCaseItemNode = new ConditionedCaseItemNode();
					LCaseItemNode.Nodes.Add(CompileExpression(APlan, ((CaseElseExpression)LExpression.ElseExpression).Expression));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
				
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
			else
			{
				ConditionedCaseNode LNode = new ConditionedCaseNode();
				
				foreach (CaseItemExpression LCaseItemExpression in LExpression.CaseItems)
				{
					ConditionedCaseItemNode LCaseItemNode = new ConditionedCaseItemNode();
					LCaseItemNode.Nodes.Add(CompileBooleanExpression(APlan, LCaseItemExpression.WhenExpression));
					LCaseItemNode.Nodes.Add(CompileExpression(APlan, LCaseItemExpression.ThenExpression));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
				
				if (LExpression.ElseExpression != null)
				{
					ConditionedCaseItemNode LCaseItemNode = new ConditionedCaseItemNode();
					LCaseItemNode.Nodes.Add(CompileExpression(APlan, ((CaseElseExpression)LExpression.ElseExpression).Expression));
					LCaseItemNode.DetermineCharacteristics(APlan);
					LNode.Nodes.Add(LCaseItemNode);
				}
				
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
		}
		
		public static PlanNode CompileValueExpression(Plan APlan, ValueExpression AExpression)
		{
			ValueNode LNode = new ValueNode();
			switch (AExpression.Token)
			{
				#if NILISSCALAR
				case LexerToken.Nil: LNode.DataType = APlan.DataTypes.SystemScalar; LNode.IsNilable = true; break;
				#else
				case TokenType.Nil: LNode.DataType = APlan.DataTypes.SystemGeneric; LNode.IsNilable = true; break;
				#endif
				case TokenType.Boolean: LNode.DataType = APlan.DataTypes.SystemBoolean; LNode.Value = (bool)AExpression.Value; break;
				case TokenType.Integer: 
				case TokenType.Hex:
					if ((Convert.ToInt64(AExpression.Value) > Int32.MaxValue) || (Convert.ToInt64(AExpression.Value) < Int32.MinValue))
					{
						LNode.DataType = APlan.DataTypes.SystemLong;
						LNode.Value = Convert.ToInt64(AExpression.Value);
					}
					else
					{
						LNode.DataType = APlan.DataTypes.SystemInteger;
						LNode.Value = Convert.ToInt32(AExpression.Value);
					}
				break;
				case TokenType.Decimal: LNode.DataType = APlan.DataTypes.SystemDecimal; LNode.Value = (decimal)AExpression.Value; break;
				case TokenType.Money: LNode.DataType = APlan.DataTypes.SystemMoney; LNode.Value = (decimal)AExpression.Value; break;
				#if USEDOUBLES
				case LexerToken.Float: LNode.DataType = APlan.DataTypes.SystemDouble; LNode.Value = Scalar.FromDouble((double)AExpression.Value); break;
				#endif
				case TokenType.String: LNode.DataType = APlan.DataTypes.SystemString; LNode.Value = (string)AExpression.Value; break;
				#if USEISTRING
				case LexerToken.IString: LNode.DataType = APlan.DataTypes.SystemIString; LNode.Value = (string)AExpression.Value; break;
				#endif
				default: throw new CompilerException(CompilerException.Codes.UnknownLiteralType, AExpression, Enum.GetName(typeof(TokenType), AExpression.Token));
			}

			// ValueNodes assume the device of their peers

			return LNode;
		}
		
		public static PlanNode EmitParameterNode(Plan APlan, Modifier AModifier, PlanNode APlanNode)
		{
			ParameterNode LNode = new ParameterNode();
			LNode.Modifier = AModifier;
			LNode.Nodes.Add(APlanNode);
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileParameterExpression(Plan APlan, ParameterExpression AParameter)
		{
			return EmitParameterNode(APlan, AParameter.Modifier, CompileExpression(APlan, AParameter.Expression));
		}
		
		public static PlanNode CompileIdentifierExpression(Plan APlan, IdentifierExpression AExpression)
		{
			NameBindingContext LContext = new NameBindingContext(AExpression.Identifier, APlan.NameResolutionPath);
			PlanNode LNode = EmitIdentifierNode(APlan, AExpression, LContext);
			if (LNode == null)
				if (LContext.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, AExpression, AExpression.Identifier, ExceptionUtility.StringsToCommaList(LContext.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AExpression, AExpression.Identifier);
			return LNode;
		}
		
		public static PlanNode EmitStackColumnReferenceNode(Plan APlan, string AIdentifier, int ALocation)
		{
			Schema.IRowType LRowType = (Schema.IRowType)APlan.Symbols[ALocation].DataType;
			int LColumnIndex = LRowType.Columns.IndexOf(AIdentifier);
			#if USECOLUMNLOCATIONBINDING
			return 
				new StackColumnReferenceNode
				(
					LRowType.Columns[LColumnIndex].Name,
					LRowType.Columns[LColumnIndex].DataType, 
					ALocation, 
					LColumnIndex
				);
			#else
			return 
				new StackColumnReferenceNode
				(
					LRowType.Columns[LColumnIndex].Name,
					LRowType.Columns[LColumnIndex].DataType, 
					ALocation
				);
			#endif
		}

		// TableSelectorNode 
		//		Nodes[0..RowCount - 1] = PlanNodes for each row in the table selector expression
		// 
		// This operator cannot be overloaded.
		public static PlanNode CompileTableSelectorExpression(Plan APlan, TableSelectorExpression AExpression)
		{
			Schema.ITableType LTableType;
			Schema.IRowType LRowType = null;
			if (AExpression.TypeSpecifier != null)
			{
				Schema.IDataType LDataType = CompileTypeSpecifier(APlan, AExpression.TypeSpecifier);
				if (!(LDataType is Schema.ITableType))
					throw new CompilerException(CompilerException.Codes.TableTypeExpected, AExpression.TypeSpecifier);
				
				LTableType = (Schema.ITableType)LDataType;
				LRowType = (Schema.IRowType)LTableType.RowType;
			}
			else
				LTableType = new Schema.TableType();

			TableSelectorNode LNode = new TableSelectorNode(LTableType);

			foreach (Schema.Column LColumn in LNode.DataType.Columns)
				LNode.TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			foreach (Expression LExpression in AExpression.Expressions)
			{
				if (LRowType == null)
				{
					PlanNode LRowNode = CompileExpression(APlan, LExpression);
					if (!(LRowNode.DataType is Schema.IRowType))
						throw new CompilerException(CompilerException.Codes.RowExpressionExpected, LExpression);
					LNode.Nodes.Add(LRowNode);

					LRowType = (Schema.IRowType)LRowNode.DataType;
					Schema.TableVarColumn LTableVarColumn;
					foreach (Schema.Column LColumn in LRowType.Columns)
					{
						LTableVarColumn = new Schema.TableVarColumn(LColumn.Copy());
						LNode.DataType.Columns.Add(LTableVarColumn.Column);
						LNode.TableVar.Columns.Add(LTableVarColumn);
					}
				}
				else
				{
					PlanNode LRowNode;
					if (LExpression is RowSelectorExpression)
						LRowNode = CompileTypedRowSelectorExpression(APlan, (RowSelectorExpression)LExpression, LRowType);
					else
						LRowNode = CompileExpression(APlan, LExpression);
						
					if (!LRowNode.DataType.Is(LRowType))
						throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, LExpression, LRowType.ToString());
					LNode.Nodes.Add(LRowNode);
				}
			}

			LNode.TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			CompileTableVarKeys(APlan, LNode.TableVar, AExpression.Keys);
			Schema.Key LClusteringKey = FindClusteringKey(APlan, LNode.TableVar);

			int LMessageIndex = APlan.Messages.Count;			
			try
			{
				if (LClusteringKey.Columns.Count > 0)
					LNode.Order = OrderFromKey(APlan, LClusteringKey);
			}
			catch (Exception LException)
			{
				if ((LException is CompilerException) && (((CompilerException)LException).Code == (int)CompilerException.Codes.NonFatalErrors))
				{
					APlan.Messages.Insert(LMessageIndex, new CompilerException(CompilerException.Codes.InvalidTableSelector, AExpression));
					throw LException;
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidTableSelector, AExpression, LException);
			}
			
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitRowExtractorNode(Plan APlan, Statement AStatement, PlanNode APlanNode)
		{
			ExtractRowNode LNode = new ExtractRowNode();
			if (AStatement != null)
				LNode.Modifiers = AStatement.Modifiers;
			LNode.Nodes.Add(EnsureTableNode(APlan, APlanNode));
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitRowExtractorNode(Plan APlan, Statement AStatement, Expression AExpression)
		{
			PlanNode LPlanNode = CompileExpression(APlan, AExpression);
			if (!(LPlanNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.UnableToExtractRow, AExpression, LPlanNode.DataType.ToString());

			return EmitRowExtractorNode(APlan, AStatement, LPlanNode);
		}
		
		// ExtractRowNode
		//		Nodes[0] = SourceNode for the extraction
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileRowExtractorExpression(Plan APlan, RowExtractorExpression AExpression)
		{
			if (!APlan.SuppressWarnings)
				APlan.Messages.Add(new CompilerException(CompilerException.Codes.RowExtractorDeprecated, CompilerErrorLevel.Warning, AExpression));
			return EmitRowExtractorNode(APlan, AExpression, AExpression.Expression);
		}
		
		public static PlanNode EmitColumnExtractorNode(Plan APlan, Statement AStatement, string AColumnName, PlanNode ATargetNode)
		{
			return EmitColumnExtractorNode(APlan, AStatement, new ColumnExpression(AColumnName), ATargetNode);
		}
		
		public static PlanNode EmitColumnExtractorNode(Plan APlan, Statement AStatement, ColumnExpression AColumnExpression, PlanNode ATargetNode)
		{
			ExtractColumnNode LNode = new ExtractColumnNode();
			if (ATargetNode.DataType is Schema.IRowType)
			{
				LNode.Identifier = AColumnExpression.ColumnName;
				#if USECOLUMNLOCATIONBINDING
				LNode.Location = ((Schema.IRowType)ATargetNode.DataType).Columns.IndexOf(AColumnName);
				if (LNode.Location < 0)
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AExpression, AColumnName);
				#else
				NameBindingContext LContext = new NameBindingContext(AColumnExpression.ColumnName, APlan.NameResolutionPath);
				int LColumnIndex = ((Schema.IRowType)ATargetNode.DataType).Columns.IndexOf(AColumnExpression.ColumnName, LContext.Names);
				if (LColumnIndex < 0)
					if (LContext.IsAmbiguous)
						throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, AColumnExpression, AColumnExpression.ColumnName, ExceptionUtility.StringsToCommaList(LContext.Names));
					else
						throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AColumnExpression, AColumnExpression.ColumnName);
				#endif
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidExtractionTarget, AColumnExpression, AColumnExpression.ColumnName, ATargetNode.DataType.Name);

			LNode.Nodes.Add(ATargetNode);
			if (AStatement != null)
				LNode.Modifiers = AStatement.Modifiers;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitColumnExtractorNode(Plan APlan, ColumnExtractorExpression AExpression, PlanNode ATargetNode)
		{
			if (AExpression.Columns.Count != 1)
				throw new CompilerException(CompilerException.Codes.InvalidColumnExtractorExpression, AExpression);
				
			return EmitColumnExtractorNode(APlan, AExpression, AExpression.Columns[0], ATargetNode);
		}
		
		// ExtractColumnNode
		//		Nodes[0] = SourceNode for the extraction
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileColumnExtractorExpression(Plan APlan, ColumnExtractorExpression AExpression)
		{
			if (!APlan.SuppressWarnings)
				APlan.Messages.Add(new CompilerException(CompilerException.Codes.ColumnExtractorDeprecated, CompilerErrorLevel.Warning, AExpression));
			return EmitColumnExtractorNode(APlan, AExpression, CompileExpression(APlan, AExpression.Expression));
		}
		
		public static PlanNode EmitCursorNode(Plan APlan, Statement AStatement, PlanNode APlanNode, CursorContext ACursorContext)
		{
			if (!(APlanNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AStatement);
				
			CursorNode LNode = new CursorNode();
			LNode.CursorContext = ACursorContext;
			LNode.Nodes.Add(EnsureTableNode(APlan, APlanNode));
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			
			return LNode;
		}
		
		public static PlanNode CompileCursor(Plan APlan, Expression AExpression)
		{
			if (!(AExpression is CursorDefinition))
			{
				CursorContext LContext = APlan.GetDefaultCursorContext();
				CursorDefinition LCursorDefinition = new CursorDefinition(AExpression, LContext.CursorCapabilities, LContext.CursorIsolation, LContext.CursorType);
				LCursorDefinition.Line = AExpression.Line;
				LCursorDefinition.LinePos = AExpression.LinePos;
				AExpression = LCursorDefinition;
			}
			
			PlanNode LNode = CompileExpression(APlan, AExpression);
			
			if (LNode.DataType == null)
				throw new CompilerException(CompilerException.Codes.ExpressionExpected, AExpression);
				
			if (LNode is CursorNode)
			{
				Schema.TableVar LTableVar = ((TableNode)LNode.Nodes[0]).TableVar;
				foreach (Schema.TableVarColumn LColumn in LTableVar.Columns)
				{
					if (LColumn.MetaData == null)
						LColumn.MetaData = new MetaData();
					LColumn.MetaData.Tags.AddOrUpdate("DAE.IsDefaultRemotable", LColumn.IsDefaultRemotable.ToString());
					LColumn.MetaData.Tags.AddOrUpdate("DAE.IsChangeRemotable", LColumn.IsChangeRemotable.ToString());
					LColumn.MetaData.Tags.AddOrUpdate("DAE.IsValidateRemotable", LColumn.IsValidateRemotable.ToString());
				}
			}
			else if (LNode.DataType is Schema.ITableType)
			{	
				LNode = EnsureTableNode(APlan, LNode);
			}

			return LNode;
		}
		
		public static PlanNode CompileCursorDefinition(Plan APlan, CursorDefinition AExpression)
		{
			CursorContext LCursorContext = new CursorContext(AExpression.CursorType, AExpression.Capabilities, AExpression.Isolation);
			APlan.PushCursorContext(LCursorContext);
			try
			{
				PlanNode LNode = CompileExpression(APlan, AExpression.Expression);
				if (LNode.DataType is Schema.ITableType)
					return EmitCursorNode(APlan, AExpression, LNode, LCursorContext);
				else
					return LNode;
			}
			finally
			{
				APlan.PopCursorContext();
			}
		}

		public static PlanNode CompileCursorSelectorExpression(Plan APlan, CursorSelectorExpression AExpression)
		{
			PlanNode LNode = CompileCursorDefinition(APlan, AExpression.CursorDefinition);
            if (!(LNode is CursorNode))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression);
			return LNode;
		}
		
		// RowSelectorNode
		//		Nodes[0..ColumnCount - 1] = PlanNodes for the columns in the row selector
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileRowSelectorExpression(Plan APlan, RowSelectorExpression AExpression)
		{
			Schema.IRowType LRowType = null;
			if (AExpression.TypeSpecifier != null)
			{
				Schema.IDataType LDataType = CompileTypeSpecifier(APlan, AExpression.TypeSpecifier);
				if (!(LDataType is Schema.IRowType))
					throw new CompilerException(CompilerException.Codes.RowTypeExpected, AExpression.TypeSpecifier);
				
				LRowType = (Schema.IRowType)LDataType;
			}
			else
				LRowType = new Schema.RowType();

			PlanNode LPlanNode;
			RowSelectorNode LNode = new RowSelectorNode(LRowType);
			
			if (AExpression.TypeSpecifier != null)
			{
				LNode.SpecifiedRowType = new Schema.RowType();

				foreach (NamedColumnExpression LExpression in AExpression.Expressions)
				{
					LPlanNode = CompileExpression(APlan, LExpression.Expression);
					if (LExpression.ColumnAlias == String.Empty)
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, LExpression);
						
					Schema.Column LTargetColumn = LNode.DataType.Columns[LExpression.ColumnAlias];
						
					LNode.SpecifiedRowType.Columns.Add
					(
						new Schema.Column
						(
							LTargetColumn.Name,
							LTargetColumn.DataType
						)
					);
					
					if (!LPlanNode.DataType.Is(LTargetColumn.DataType))
					{
						ConversionContext LContext = FindConversionPath(APlan, LPlanNode.DataType, LTargetColumn.DataType);
						CheckConversionContext(APlan, LContext);
						LPlanNode = ConvertNode(APlan, LPlanNode, LContext);
					}
			
					LNode.Nodes.Add(LPlanNode);
				}
			}
			else
			{
				foreach (NamedColumnExpression LExpression in AExpression.Expressions)
				{
					LPlanNode = CompileExpression(APlan, LExpression.Expression);
					if (LExpression.ColumnAlias == String.Empty)
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, LExpression);

					LNode.DataType.Columns.Add
					(
						new Schema.Column
						(
							LExpression.ColumnAlias, 
							LPlanNode.DataType
						)
					);
					LNode.Nodes.Add(LPlanNode);
				}
			}
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileTypedRowSelectorExpression
		(
			Plan APlan, 
			RowSelectorExpression AExpression, 
			Schema.IRowType ARowType
		)
		{
			if (AExpression.TypeSpecifier == null)
			{
				RowSelectorNode LNode = new RowSelectorNode(new Schema.RowType());
				PlanNode LPlanNode;
				for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
				{
					if (AExpression.Expressions[LIndex].ColumnAlias == String.Empty)
					{
						if (LIndex >= ARowType.Columns.Count)
							throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, AExpression.Expressions[LIndex], ARowType.ToString());
						LPlanNode = CompileTypedExpression(APlan, AExpression.Expressions[LIndex].Expression, ARowType.Columns[LIndex].DataType);

						LNode.DataType.Columns.Add
						(
							new Schema.Column
							(
								ARowType.Columns[LIndex].Name, 
								ARowType.Columns[LIndex].DataType
							)
						);
					}
					else
					{
						if (!ARowType.Columns.ContainsName(AExpression.Expressions[LIndex].ColumnAlias))
							throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, AExpression.Expressions[LIndex], ARowType.ToString());
						Schema.IDataType LTargetType = ARowType.Columns[ARowType.Columns.IndexOfName(AExpression.Expressions[LIndex].ColumnAlias)].DataType;
						LPlanNode = CompileTypedExpression(APlan, AExpression.Expressions[LIndex].Expression, LTargetType);

						LNode.DataType.Columns.Add
						(
							new Schema.Column
							(
								AExpression.Expressions[LIndex].ColumnAlias, 
								LTargetType
							)
						);
					}
					LNode.Nodes.Add(LPlanNode);
				}
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
			else
			{
				PlanNode LNode = CompileRowSelectorExpression(APlan, AExpression);
				
				if (!LNode.DataType.Is(ARowType))
				{
					ConversionContext LContext = FindConversionPath(APlan, LNode.DataType, ARowType);
					try
					{
						CheckConversionContext(APlan, LContext);
					}
					catch (CompilerException E)
					{
						throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, AExpression, E, ARowType.ToString());
					}
					LNode = ConvertNode(APlan, LNode, LContext);
				}
				
				return LNode;
			}
		}

		public static PlanNode EmitBaseTableVarNode(Plan APlan, Schema.TableVar ATableVar)
		{
			return EmitBaseTableVarNode(APlan, ATableVar.Name, (Schema.BaseTableVar)ATableVar);
		}

		public static PlanNode EmitBaseTableVarNode(Plan APlan, string AIdentifier, Schema.BaseTableVar ATableVar)
		{
			return EmitBaseTableVarNode(APlan, new EmptyStatement(), AIdentifier, ATableVar);
		}
		
		public static PlanNode EmitBaseTableVarNode(Plan APlan, Statement AStatement, string AIdentifier, Schema.BaseTableVar ATableVar)
		{
			BaseTableVarNode LNode = (BaseTableVarNode)FindCallNode(APlan, AStatement, Instructions.Retrieve, new PlanNode[]{});
			LNode.TableVar = ATableVar;
			if ((ATableVar.SourceTableName != null) && (Schema.Object.NamesEqual(ATableVar.Name, AIdentifier)))
				LNode.ExplicitBind = true;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitDerivedTableVarNode(Plan APlan, Schema.DerivedTableVar ADerivedTableVar)
		{
			return EmitDerivedTableVarNode(APlan, new EmptyStatement(), ADerivedTableVar);
		}
		
		public static PlanNode EmitDerivedTableVarNode(Plan APlan, Statement AStatement, Schema.DerivedTableVar ADerivedTableVar)
		{
			DerivedTableVarNode LNode = new DerivedTableVarNode(ADerivedTableVar);
			LNode.Operator = ResolveOperator(APlan, Instructions.Retrieve, new Schema.Signature(new Schema.SignatureElement[]{}), false);
			LNode.Modifiers = AStatement.Modifiers;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		// Emits a restrict node for the given source and condition		
		//
		// RestrictNode
		//		Nodes[0] = ASourceNode
		//		Nodes[1] = AConditionNode
		public static PlanNode EmitRestrictNode(Plan APlan, RestrictExpression AExpression, PlanNode ASourceNode, PlanNode AConditionNode)
		{
			return EmitCallNode(APlan, AExpression, Instructions.Restrict, new PlanNode[]{ASourceNode, AConditionNode});
		}
		
		public static PlanNode EmitRestrictNode(Plan APlan, PlanNode ASourceNode, PlanNode AConditionNode)
		{
			return EmitRestrictNode(APlan, null, ASourceNode, AConditionNode);
		}
		
		public static PlanNode EmitRestrictNode(Plan APlan, PlanNode ASourceNode, Expression AExpression)
		{
			return EmitRestrictNode(APlan, null, ASourceNode, AExpression);
		}
		
		public static PlanNode EmitRestrictNode(Plan APlan, RestrictExpression ARestrictExpression, PlanNode ASourceNode, Expression AExpression)
		{
			if (!(ASourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, APlan.CurrentStatement());

			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(String.Empty, ((Schema.ITableType)ASourceNode.DataType).RowType));
				try
				{
					PlanNode LConditionNode = CompileExpression(APlan, AExpression);
					return EmitRestrictNode(APlan, ARestrictExpression, ASourceNode, LConditionNode);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public static PlanNode EmitInsertConditionNode(Plan APlan, PlanNode ASourceNode)
		{
			RestrictNode LNode = (RestrictNode)EmitRestrictNode(APlan, EnsureTableNode(APlan, ASourceNode), CompileExpression(APlan, new ValueExpression(false, TokenType.Boolean)));
			LNode.EnforcePredicate = false;
			LNode.ShouldEmit = false;
			return LNode;
		}
		
		public static PlanNode EmitUpdateConditionNode(Plan APlan, PlanNode ASourceNode, PlanNode AConditionNode)
		{
			RestrictNode LNode = (RestrictNode)EmitRestrictNode(APlan, ASourceNode, AConditionNode);
			LNode.EnforcePredicate = false;
			return LNode;
		}
		
		public static PlanNode EmitUpdateConditionNode(Plan APlan, PlanNode ASourceNode, Expression AExpression)
		{
			RestrictNode LNode = (RestrictNode)EmitRestrictNode(APlan, ASourceNode, AExpression);
			LNode.EnforcePredicate = false;
			return LNode;
		}
		
		// Source BNF -> <table expression> where <condition>
		//
		// operator iRestrict(table{}, object) : table{}
		//
		// RestrictNode
		//		Nodes[0] = SourceTable
		//		Nodes[1] = Conditional
		//
		// Default Evaluation Behavior ->
		//		The restrict node produces a restrict table which
		//		evaluates the condition for each row in the source
		//		by pushing the row to be tested on the stack, and
		//		evaluating the condition expression.
		//		
		//	Note that the source expression must be a table type,
		//	and that the condition must evaluate to a logical type.
		public static PlanNode CompileRestrictExpression(Plan APlan, RestrictExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
				if (!(LSourceNode.DataType is Schema.ITableType))
					throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitRestrictNode(APlan, AExpression, LSourceNode, AExpression.Condition);
		}
		
		public static ApplicationTransaction PrepareShouldTranslate(Plan APlan, Statement AStatement)
		{
			return PrepareShouldTranslate(APlan, AStatement, String.Empty);
		}
		
		public static ApplicationTransaction PrepareShouldTranslate(Plan APlan, Statement AStatement, string AQualifier)
		{
			ApplicationTransaction LTransaction = null;
			if ((APlan.ApplicationTransactionID != Guid.Empty) && !Convert.ToBoolean(LanguageModifiers.GetModifier(AStatement.Modifiers, Schema.Object.Qualify("ShouldTranslate", AQualifier), "true")))
				LTransaction = APlan.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushLookup();
			}
			catch
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
				throw;
			}
			
			return LTransaction;
		}
		
		public static void UnprepareShouldTranslate(Plan APlan, ApplicationTransaction ATransaction)
		{
			if (ATransaction != null)
			{
				try
				{
					ATransaction.PopLookup();
				}
				finally
				{
					Monitor.Exit(ATransaction);
				}
			}
		}
		
		public static Expression BuildRowEqualExpression(Plan APlan, Schema.Columns ALeftRow, Schema.Columns ARightRow)
		{
			return BuildRowEqualExpression(APlan, ALeftRow, ARightRow, null, null);
		}
		
		// Builds an expression suitable for comparing a row with the columns given in ALeftRow to a row with the columns given in ARightRow
		// All the columns in ALeftRow are expected to be prefixed with the keyword 'left'.
		// All the columns in ARightRow are expected to be prefixed with the keyword 'right'.
		// The resulting expression is order-agnostic (i.e. the columns in ARightRow need not be in the same order as the columns in ARightRow
		// for each column ->
		//  [IsNil(left.<column name>) and IsNil(right.<column name>) or] left.<column name> = right.<column name>
		public static Expression BuildRowEqualExpression(Plan APlan, Schema.Columns ALeftRow, Schema.Columns ARightRow, BitArray ALeftIsNilable, BitArray ARightIsNilable)
		{
			Expression LExpression = null;
			Expression LEqualExpression;

			for (int LIndex = 0; LIndex < ALeftRow.Count; LIndex++)
			{
				int LRightIndex = ARightRow.IndexOf(Schema.Object.Dequalify(ALeftRow[LIndex].Name));
				if (((ALeftIsNilable == null) || ALeftIsNilable[LIndex]) || ((ARightIsNilable == null) || ARightIsNilable[LRightIndex]))
					LEqualExpression =
						new BinaryExpression
						(
							new BinaryExpression
							(
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								new CallExpression(CIsNilOperatorName, new Expression[]{new IdentifierExpression(Schema.Object.EnsureRooted(ALeftRow[LIndex].Name))}), 
								Instructions.And, 
								new CallExpression(CIsNilOperatorName, new Expression[]{new IdentifierExpression(Schema.Object.EnsureRooted(ARightRow[LRightIndex].Name))})
								#else
								new CallExpression(CIsNilOperatorName, new Expression[]{new IdentifierExpression(ALeftRow[LIndex].Name)}), 
								Instructions.And, 
								new CallExpression(CIsNilOperatorName, new Expression[]{new IdentifierExpression(ARightRow[LRightIndex].Name)})
								#endif
							),
							Instructions.Or,
							new BinaryExpression
							(
								new IdentifierExpression(ALeftRow[LIndex].Name), 
								Instructions.Equal, 
								new IdentifierExpression(ARightRow[LRightIndex].Name)
							)
						);
				else
					LEqualExpression =
						new BinaryExpression
						(
							#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
							new IdentifierExpression(Schema.Object.EnsureRooted(ALeftRow[LIndex].Name)),
							Instructions.Equal,
							new IdentifierExpression(Schema.Object.EnsureRooted(ARightRow[LRightIndex].Name))
							#else
							new IdentifierExpression(ALeftRow[LIndex].Name),
							Instructions.Equal,
							new IdentifierExpression(ARightRow[LRightIndex].Name)
							#endif
						);
					
				if (LExpression == null)
					LExpression = LEqualExpression;
				else
					LExpression = new BinaryExpression(LExpression, Instructions.And, LEqualExpression);
			}

			if (LExpression == null)
				LExpression = new ValueExpression(true);

			return LExpression;
		}
		
		public static Expression BuildRowEqualExpression(Plan APlan, string ALeftRowVarName, string ARightRowVarName, Schema.TableVarColumnsBase AColumns)
		{
			Schema.Columns LColumns = new Schema.Columns();
			foreach (Schema.TableVarColumn LColumn in AColumns)
				LColumns.Add(LColumn.Column);
			return BuildRowEqualExpression(APlan, ALeftRowVarName, ARightRowVarName, LColumns);
		}
		
		public static Expression BuildOptimisticRowEqualExpression(Plan APlan, string ALeftRowVarName, string ARightRowVarName, Schema.Columns AColumns)
		{
			Schema.Columns LColumns = new Schema.Columns();
			foreach (Schema.Column LColumn in AColumns)
			{
				Schema.Signature LSignature = new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(LColumn.DataType), new Schema.SignatureElement(LColumn.DataType)});
				OperatorBindingContext LContext = new OperatorBindingContext(null, "iEqual", APlan.NameResolutionPath, LSignature, true);
				Compiler.ResolveOperator(APlan, LContext);
				if (LContext.Operator != null)
					LColumns.Add(LColumn);
			}
			
			return BuildRowEqualExpression(APlan, ALeftRowVarName, ARightRowVarName, LColumns);
		}
		
		public static Expression BuildRowEqualExpression(Plan APlan, string ALeftRowVarName, string ARightRowVarName, Schema.Columns AColumns)
		{
			if ((ALeftRowVarName == null) || (ARightRowVarName == null) || ((ALeftRowVarName == String.Empty) && (ARightRowVarName == String.Empty)))
				throw new ArgumentException("Row variable name is required for at least one side of the row comparison expression to be built.");
				
			Expression LExpression = null;
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
			{
				Expression LEqualExpression =
					new BinaryExpression
					(
						new BinaryExpression
						(
							new CallExpression
							(
								CIsNilOperatorName,
								new Expression[]
								{
									ALeftRowVarName == String.Empty ?
										#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
										(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(AColumns[LIndex].Name)) :
										#else
										(Expression)new IdentifierExpression(AColumns[LIndex].Name) :
										#endif
										new QualifierExpression(new IdentifierExpression(ALeftRowVarName), new IdentifierExpression(AColumns[LIndex].Name))
								}
							),
							Instructions.And,
							new CallExpression
							(
								CIsNilOperatorName,
								new Expression[]
								{
									ARightRowVarName == String.Empty ?
										#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
										(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(AColumns[LIndex].Name)) :
										#else
										(Expression)new IdentifierExpression(AColumns[LIndex].Name) :
										#endif
										new QualifierExpression(new IdentifierExpression(ARightRowVarName), new IdentifierExpression(AColumns[LIndex].Name))
								}
							)
						),
						Instructions.Or,
						new BinaryExpression
						(
							ALeftRowVarName == String.Empty ?
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(AColumns[LIndex].Name)) :
								#else
								(Expression)new IdentifierExpression(AColumns[LIndex].Name) :
								#endif
								new QualifierExpression(new IdentifierExpression(ALeftRowVarName), new IdentifierExpression(AColumns[LIndex].Name)),
							Instructions.Equal,
							ARightRowVarName == String.Empty ?
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(AColumns[LIndex].Name)) :
								#else
								(Expression)new IdentifierExpression(AColumns[LIndex].Name) :
								#endif
								new QualifierExpression(new IdentifierExpression(ARightRowVarName), new IdentifierExpression(AColumns[LIndex].Name))
						)
					);
					
				if (LExpression != null)
					LExpression = new BinaryExpression(LExpression, Instructions.And, LEqualExpression);
				else
					LExpression = LEqualExpression;
			}
			
			if (LExpression == null)
				LExpression = new ValueExpression(true);
			
			return LExpression;
		}
		
		public static Expression BuildKeyEqualExpression(Plan APlan, string ALeftRowVarName, string ARightRowVarName, Schema.TableVarColumnsBase ALeftColumns, Schema.TableVarColumnsBase ARightColumns)
		{
			Expression LExpression = null;
			for (int LIndex = 0; LIndex < ALeftColumns.Count; LIndex++)
			{
				Expression LEqualExpression = 
					new BinaryExpression
					(
						ALeftRowVarName == String.Empty ? 
							#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
							(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[LIndex].Name)) :
							#else
							(Expression)new IdentifierExpression(ALeftColumns[LIndex].Name) :
							#endif
							new QualifierExpression(new IdentifierExpression(ALeftRowVarName), new IdentifierExpression(ALeftColumns[LIndex].Name)),
						Instructions.Equal, 
						ARightRowVarName == String.Empty ?
							#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
							(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[LIndex].Name)) :
							#else
							(Expression)new IdentifierExpression(ARightColumns[LIndex].Name) :
							#endif
							new QualifierExpression(new IdentifierExpression(ARightRowVarName), new IdentifierExpression(ARightColumns[LIndex].Name))
					);
					
				if (LExpression != null)
					LExpression = new BinaryExpression(LExpression, Instructions.And, LEqualExpression);
				else
					LExpression = LEqualExpression;
			}
			
			if (LExpression == null)
				LExpression = new ValueExpression(true);
				
			return LExpression;
		}

		public static Expression BuildKeyEqualExpression(Plan APlan, string ALeftRowVarName, string ARightRowVarName, Schema.Columns ALeftColumns, Schema.Columns ARightColumns)
		{
			Expression LExpression = null;
			for (int LIndex = 0; LIndex < ALeftColumns.Count; LIndex++)
			{
				Expression LEqualExpression = 
					new BinaryExpression
					(
						ALeftRowVarName == String.Empty ? 
							#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
							(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[LIndex].Name)) :
							#else
							(Expression)new IdentifierExpression(ALeftColumns[LIndex].Name) :
							#endif
							new QualifierExpression(new IdentifierExpression(ALeftRowVarName), new IdentifierExpression(ALeftColumns[LIndex].Name)),
						Instructions.Equal, 
						ARightRowVarName == String.Empty ?
							#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
							(Expression)new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[LIndex].Name)) :
							#else
							(Expression)new IdentifierExpression(ARightColumns[LIndex].Name) :
							#endif
							new QualifierExpression(new IdentifierExpression(ARightRowVarName), new IdentifierExpression(ARightColumns[LIndex].Name))
					);
					
				if (LExpression != null)
					LExpression = new BinaryExpression(LExpression, Instructions.And, LEqualExpression);
				else
					LExpression = LEqualExpression;
			}
			
			if (LExpression == null)
				LExpression = new ValueExpression(true);
				
			return LExpression;
		}

		public static Expression BuildKeyEqualExpression(Plan APlan, Schema.Columns ALeftKey, Schema.Columns ARightKey)
		{
			Expression LExpression = null;
			Expression LEqualExpression;
			for (int LIndex = 0; LIndex < ALeftKey.Count; LIndex++)
			{
				Error.AssertWarn(String.Compare(ALeftKey[LIndex].Name, ARightKey[LIndex].Name) != 0, "Key column names equal. Invalid key comparison expression.");
				
				#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
				LEqualExpression = new BinaryExpression(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftKey[LIndex].Name)), Instructions.Equal, new IdentifierExpression(Schema.Object.EnsureRooted(ARightKey[LIndex].Name)));
				#else
				LEqualExpression = new BinaryExpression(new IdentifierExpression(ALeftKey[LIndex].Name), Instructions.Equal, new IdentifierExpression(ARightKey[LIndex].Name));
				#endif
				
				if (LExpression == null)
					LExpression = LEqualExpression;
				else
					LExpression = new BinaryExpression(LExpression, Instructions.And, LEqualExpression);
			}
			
			if (LExpression == null)
				LExpression = new ValueExpression(true);

			return LExpression;
		}
		
		public static Expression BuildKeyIsNotNilExpression(Schema.KeyColumns AKey)
		{
			Expression LExpression = null;
			Expression LIsNotNilExpression;
			for (int LIndex = 0; LIndex < AKey.Count; LIndex++)
			{
				LIsNotNilExpression = 
					new UnaryExpression
					(
						Instructions.Not,
						new CallExpression
						(
							CIsNilOperatorName,
							new Expression[] 
							{ 
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								new IdentifierExpression(Schema.Object.EnsureRooted(AKey[LIndex].Name))
								#else
								new IdentifierExpression(AKey[LIndex].Name)
								#endif
							}
						)
					);
					
				if (LExpression == null)
					LExpression = LIsNotNilExpression;
				else
					LExpression = new BinaryExpression(LExpression, Instructions.And, LIsNotNilExpression);
			}
			
			if (LExpression == null)
				LExpression = new ValueExpression(true);
				
			return LExpression;
		}
		
		// Produces a project node based on the columns in the given key		
		public static PlanNode EmitProjectNode(Plan APlan, PlanNode ANode, Schema.Key AKey)
		{
			string[] LColumns = new string[AKey.Columns.Count];
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
				LColumns[LIndex] = AKey.Columns[LIndex].Name;
				
			return EmitProjectNode(APlan, ANode, LColumns, true);
		}
		
		// Source BNF -> <table expression> [over | remove] <column list>
		//
		// ProjectNode
		//	Nodes[0] = ASourceNode
		//  Nodes[1] = ListNode
		//		Nodes[0..AColumns.Count - 1] = ValueNode containing the name of the projected column	
		//	Nodes[2] = ValueNode for IsProject
		//
		// Default Evaluation Behavior ->
		//		The evaluation produces a ProjectTable which
		//		ensures uniqueness of the rows being returned
		//		if required.
		//
		// Note that the node ensures the source node is ordered if distinct is required.
		public static PlanNode EmitProjectNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, string[] AColumns, bool AIsProject)
		{
			PlanNode LNode = FindCallNode(APlan, AStatement, AIsProject ? Instructions.Project : Instructions.Remove, new PlanNode[]{ASourceNode});
			if (LNode is ProjectNodeBase)
				((ProjectNodeBase)LNode).ColumnNames.AddRange(AColumns);
			else
				((RowProjectNodeBase)LNode).ColumnNames.AddRange(AColumns);
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitProjectNode(Plan APlan, PlanNode ASourceNode, string[] AColumns, bool AIsProject)
		{
			return EmitProjectNode(APlan, new EmptyStatement(), ASourceNode, AColumns, AIsProject);
		}
		
		// Produces a project node based on all columns in the given source node.
		public static PlanNode EmitProjectNode(Plan APlan, PlanNode ASourceNode)
		{
			string[] LColumns = new string[((Schema.ITableType)ASourceNode.DataType).Columns.Count];
			for (int LIndex = 0; LIndex < ((Schema.ITableType)ASourceNode.DataType).Columns.Count; LIndex++)
				LColumns[LIndex] = ((Schema.ITableType)ASourceNode.DataType).Columns[LIndex].Name;

			return EmitProjectNode(APlan, ASourceNode, LColumns, true);
		}
		
		public static PlanNode EmitProjectNode(Plan APlan, PlanNode ASourceNode, ColumnExpressions AColumns, bool AIsProject)
		{
			return EmitProjectNode(APlan, new EmptyStatement(), ASourceNode, AColumns, AIsProject);
		}

		public static PlanNode EmitProjectNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, ColumnExpressions AColumns, bool AIsProject)
		{
			string[] LColumns = new string[AColumns.Count];
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				LColumns[LIndex] = AColumns[LIndex].ColumnName;				
			return EmitProjectNode(APlan, AStatement, ASourceNode, LColumns, AIsProject);
		}
		
		public static PlanNode CompileProjectExpression(Plan APlan, ProjectExpression AExpression)
		{
			string[] LColumns = new string[AExpression.Columns.Count];
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
				LColumns[LIndex] = AExpression.Columns[LIndex].ColumnName;
				
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitProjectNode(APlan, AExpression, LSourceNode, LColumns, true);
		}
		
		public static PlanNode CompileRemoveExpression(Plan APlan, RemoveExpression AExpression)
		{
			string[] LColumns = new string[AExpression.Columns.Count];
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
				LColumns[LIndex] = AExpression.Columns[LIndex].ColumnName;
			
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitProjectNode(APlan, AExpression, LSourceNode, LColumns, false);
		}

		// Source BNF -> <table expression> aggregate [by <column list>] compute {<aggregate column list>}
		//
		// operator iAggregate(table{}) : table{}
		//
		// AggregateNode
		//		Nodes[0] = project over {by Columns}
		//			Nodes[0] = ASourceNode
		//		Nodes[1..AggregateExpression.Count] = PlanNode - class determined by lookup from the server catalog
		//			Nodes[0] = project over {aggregate column for this expression}
		//				Nodes[0] = restrict
		//					Nodes[0] = ASourceNode.Clone()
		//					Nodes[1] = Condition defined by the first key in the project of the aggregate source (AggregateNode.Nodes[0]);
		//
		// Default Evaluation Behavior ->
		//		Evaluation produces an AggregateTable which will return the aggregate of the compute columns grouped by the by columns
		public static PlanNode CompileAggregateExpression(Plan APlan, AggregateExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode[] LArguments = new PlanNode[]{LSourceNode};
			OperatorBindingContext LContext = new OperatorBindingContext(AExpression, Instructions.Aggregate, APlan.NameResolutionPath, SignatureFromArguments(LArguments), false);
			AggregateNode LNode = (AggregateNode)FindCallNode(APlan, LContext, LArguments);
			CheckOperatorResolution(APlan, LContext);
			LNode.Columns = AExpression.ByColumns;
			LNode.ComputeColumns = AExpression.ComputeColumns;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitCopyNode(Plan APlan, TableNode ASourceNode)
		{
			if (ASourceNode.Order == null)
				return EmitCopyNode(APlan, ASourceNode, Compiler.FindClusteringKey(APlan, ASourceNode.TableVar));
			else
				return EmitCopyNode(APlan, ASourceNode, ASourceNode.Order);
		}
		
		public static PlanNode EmitCopyNode(Plan APlan, TableNode ASourceNode, Schema.Key AKey)
		{
			Schema.Order LOrder = new Schema.Order();
			Schema.OrderColumn LOrderColumn;
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				LOrderColumn = new Schema.OrderColumn(ASourceNode.TableVar.Columns[LColumn], true);
				LOrderColumn.Sort = GetUniqueSort(APlan, LOrderColumn.Column.DataType);
				if (LOrderColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LOrderColumn.Sort.Dependencies);
				LOrder.Columns.Add(LOrderColumn);
			}

			return EmitCopyNode(APlan, ASourceNode, LOrder);
		}
		
		public static PlanNode EmitCopyNode(Plan APlan, TableNode ASourceNode, Schema.Order AOrder)
		{
			CopyNode LNode = (CopyNode)FindCallNode(APlan, new EmptyStatement(), Instructions.Copy, new PlanNode[]{ASourceNode});
			LNode.RequestedOrder = AOrder;
			LNode.RequestedCapabilities = APlan.CursorContext.CursorCapabilities;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitOrderNode(Plan APlan, TableNode ASourceNode, bool AIsAccelerator)
		{
			if (ASourceNode.Order == null)
				return EmitOrderNode(APlan, ASourceNode, Compiler.FindClusteringKey(APlan, ASourceNode.TableVar), AIsAccelerator);
			else
				return EmitOrderNode(APlan, ASourceNode, ASourceNode.Order, AIsAccelerator);
		}
		
		public static PlanNode EmitOrderNode(Plan APlan, TableNode ASourceNode, Schema.Key AKey, bool AIsAccelerator)
		{
			return EmitOrderNode(APlan, ASourceNode, AKey, null, null, AIsAccelerator);
		}

		public static PlanNode EmitOrderNode
		(
			Plan APlan, 
			TableNode ASourceNode, 
			Schema.Key AKey, 
			MetaData AMetaData,
			IncludeColumnExpression ASequenceColumn,
			bool AIsAccelerator
		)
		{
			Schema.Order LOrder = new Schema.Order(AMetaData);
			Schema.OrderColumn LOrderColumn;
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				LOrderColumn = new Schema.OrderColumn(ASourceNode.TableVar.Columns[LColumn], true);
				LOrderColumn.Sort = GetUniqueSort(APlan, LOrderColumn.Column.DataType);
				if (LOrderColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LOrderColumn.Sort.Dependencies);
				LOrder.Columns.Add(LOrderColumn);
			}

			return EmitOrderNode(APlan, ASourceNode, LOrder, null, ASequenceColumn, AIsAccelerator);
		}
		
		public static PlanNode EmitOrderNode(Plan APlan, PlanNode ASourceNode, Schema.Order AOrder, bool AIsAccelerator)
		{
			return EmitOrderNode(APlan, ASourceNode, AOrder, null, null, AIsAccelerator);
		}
		
		public static PlanNode EmitOrderNode
		(
			Plan APlan, 
			PlanNode ASourceNode, 
			Schema.Order AOrder, 
			MetaData AMetaData,
			IncludeColumnExpression ASequenceColumn,
			bool AIsAccelerator
		)
		{
			return EmitOrderNode(APlan, new EmptyStatement(), ASourceNode, AOrder, AMetaData, ASequenceColumn, AIsAccelerator);
		}
		
		// Source BNF -> <table expression> order by all | <order name> | <order column list>
		//
		// operator Order(presentation{}) : presentation{}
		public static PlanNode EmitOrderNode
		(
			Plan APlan, 
			Statement AStatement,
			PlanNode ASourceNode, 
			Schema.Order AOrder, 
			MetaData AMetaData,
			IncludeColumnExpression ASequenceColumn,
			bool AIsAccelerator
		)
		{
			OrderNode LNode = (OrderNode)FindCallNode(APlan, AStatement, Instructions.Order, new PlanNode[]{ASourceNode});
			LNode.IsAccelerator = AIsAccelerator;
			LNode.RequestedOrder = AOrder;
			LNode.RequestedCapabilities = APlan.CursorContext.CursorCapabilities;
			LNode.SequenceColumn = ASequenceColumn;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileOrderExpression(Plan APlan, OrderExpression AExpression)
		{
			PlanNode LSourceNode = CompileExpression(APlan, AExpression.Expression);
			if (!(LSourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);
				
			LSourceNode = EnsureTableNode(APlan, LSourceNode);

			return 
				EmitOrderNode
				(
					APlan, 
					AExpression, 
					LSourceNode, 
					CompileOrderColumnDefinitions(APlan, ((TableNode)LSourceNode).TableVar, AExpression.Columns, null, false), 
					null, 
					AExpression.SequenceColumn,
					false
				);
		}
		
		public static PlanNode AppendNode(Plan APlan, PlanNode ALeftNode, string AInstruction, PlanNode ARightNode)
		{
			return (ALeftNode != null) ? EmitBinaryNode(APlan, ALeftNode, AInstruction, ARightNode) : ARightNode;
		}
		
		public static PlanNode EmitBrowseNode(Plan APlan, TableNode ASourceNode, bool AIsAccelerator)
		{
			if (ASourceNode.Order == null)
				return EmitBrowseNode(APlan, ASourceNode, Compiler.FindClusteringKey(APlan, ASourceNode.TableVar), AIsAccelerator);
			else
				return EmitBrowseNode(APlan, ASourceNode, ASourceNode.Order, AIsAccelerator);
		}
        
		public static PlanNode EmitBrowseNode(Plan APlan, TableNode ASourceNode, Schema.Key AKey, bool AIsAccelerator)
		{
			return EmitBrowseNode(APlan, ASourceNode, AKey, null, AIsAccelerator);
		}

		public static PlanNode EmitBrowseNode(Plan APlan, TableNode ASourceNode, Schema.Key AKey, MetaData AMetaData, bool AIsAccelerator)
		{
			Schema.Order LOrder = new Schema.Order(AMetaData);
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				Schema.OrderColumn LNewOrderColumn = new Schema.OrderColumn(ASourceNode.TableVar.Columns[LColumn], true);
				LNewOrderColumn.IsDefaultSort = true;
				LNewOrderColumn.Sort = GetSort(APlan, LNewOrderColumn.Column.DataType);
				if (LNewOrderColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LNewOrderColumn.Sort.Dependencies);
				LOrder.Columns.Add(LNewOrderColumn);
			}

			return EmitBrowseNode(APlan, ASourceNode, LOrder, AIsAccelerator);
		}
		
		public static PlanNode EmitBrowseNode(Plan APlan, PlanNode ASourceNode, Schema.Order AOrder, bool AIsAccelerator)
		{
			return EmitBrowseNode(APlan, ASourceNode, AOrder, null, AIsAccelerator);
		}
		
		public static PlanNode EmitBrowseNode(Plan APlan, PlanNode ASourceNode, Schema.Order AOrder, MetaData AMetaData, bool AIsAccelerator)
		{
			return EmitBrowseNode(APlan, new EmptyStatement(), ASourceNode, AOrder, AMetaData, AIsAccelerator);
		}
		
		// operator Browse(table{}, object, object) : table{}		
		public static PlanNode EmitBrowseNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, Schema.Order AOrder, MetaData AMetaData, bool AIsAccelerator)
		{
			BrowseNode LNode = (BrowseNode)FindCallNode(APlan, AStatement, Instructions.Browse, new PlanNode[]{ASourceNode});
			LNode.IsAccelerator = AIsAccelerator;
			LNode.RequestedOrder = AOrder;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileBrowseExpression(Plan APlan, BrowseExpression AExpression)
		{
			PlanNode LSourceNode = CompileExpression(APlan, AExpression.Expression);
			if (!(LSourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);
				
			LSourceNode = EnsureTableNode(APlan, LSourceNode);
				
			return 
				EmitBrowseNode
				(
					APlan, 
					AExpression, 
					LSourceNode, 
					CompileOrderColumnDefinitions(APlan, ((TableNode)LSourceNode).TableVar, AExpression.Columns, null, true), 
					null,
					false
				);
		}
		
		public static PlanNode EnsureSearchableNode(Plan APlan, TableNode ASourceNode, Schema.Key AKey)
		{
			return EnsureSearchableNode(APlan, ASourceNode, OrderFromKey(APlan, AKey));
		}
		
		/// <summary>Ensures that the node given by ASourceNode is searchable by the order given by ASearchOrder. This method should only be called in a binding context.</summary>
		/// <remarks>
		/// This method during the binding phase to ensure that a given node will produce a result set in the given order and with the requested capabilities.
		/// If the source node does support a searchable cursor ordered by the given order, the compiler will first request an order node be emitted
		/// and then determine the device of that order node. If the resulting ordered node does not provide searchable capabilities, then if the
		/// order is supported by the device, a browse node is used to provide the search capabilities, otherwise a copy node is used to materialize
		/// the order and provide the requested capabilities.
		/// </remarks>
		public static PlanNode EnsureSearchableNode(Plan APlan, TableNode ASourceNode, Schema.Order ASearchOrder)
		{
			if ((ASourceNode.Order == null) || !ASearchOrder.Equivalent(ASourceNode.Order) || !ASourceNode.Supports(CursorCapability.Searchable))
			{
				ApplicationTransaction LTransaction = null;
				if (APlan.ApplicationTransactionID != Guid.Empty)
					LTransaction = APlan.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushGlobalContext();
					try
					{
						APlan.PushCursorContext(new CursorContext(ASourceNode.CursorType, ASourceNode.CursorCapabilities | CursorCapability.Searchable, ASourceNode.CursorIsolation));
						try
						{
							BaseOrderNode LNode = Compiler.EmitOrderNode(APlan, ASourceNode, ASearchOrder, true) as BaseOrderNode;
							LNode.InferPopulateNode(APlan); // The A/T populate node, if any, should be used from the source node for the order
							LNode.DetermineDevice(APlan); // doesn't call binding because the source for the order is already bound, only needs to determine the device for the newly created order node.
							if (!LNode.Supports(CursorCapability.Searchable))
							{
								if (LNode.DeviceSupported)
									LNode = Compiler.EmitBrowseNode(APlan, ASourceNode, ASearchOrder, true) as BaseOrderNode;
								else
									LNode = Compiler.EmitCopyNode(APlan, ASourceNode, ASearchOrder) as BaseOrderNode;
								LNode.InferPopulateNode(APlan);
								LNode.DetermineDevice(APlan);
							}
							
							return LNode;
						}
						finally
						{
							APlan.PopCursorContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
			return ASourceNode;
		}
		
		// Source BNF -> <table expression> return (<scalar expression>, <order columns>)
		//
		// operator Quota(table{}, int, object) : table{}
		//
		// QuotaNode
		//		Nodes[0] = OrderNode (AColumns)
		//			Nodes[0] = ASourceNode
		//		Nodes[1] = ACountNode
		//
		// Default Evaluation Behavior ->
		//
		//
		public static PlanNode EmitQuotaNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, PlanNode ACountNode, Schema.Order AOrder)
		{
			QuotaNode LNode = (QuotaNode)FindCallNode(APlan, AStatement, Instructions.Quota, new PlanNode[]{ASourceNode, ACountNode});
			LNode.QuotaOrder = AOrder;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitQuotaNode(Plan APlan, PlanNode ASourceNode, PlanNode ACountNode, Schema.Order AOrder)
		{
			return EmitQuotaNode(APlan, new EmptyStatement(), ASourceNode, ACountNode, AOrder);
		}
		
		public static PlanNode CompileQuotaExpression(Plan APlan, QuotaExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			if (!(LSourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);

			LSourceNode = EnsureTableNode(APlan, LSourceNode);
				
			return 
				EmitQuotaNode
				(
					APlan,
					AExpression,
					LSourceNode,
					CompileExpression(APlan, AExpression.Quota),
					AExpression.HasByClause ?
						CompileOrderColumnDefinitions(APlan, ((TableNode)LSourceNode).TableVar, AExpression.Columns, null, false) :
						OrderFromKey(APlan, FindClusteringKey(APlan, ((TableNode)LSourceNode).TableVar))
				);
		}

		// SourceBNF -> <table expression> explode by <parent condition> where <root condition>
		//
		// operator Explode(const ATable : table, const ARoot : table, const ABy : table) : table;
		//
		// ExplodeNode
		//		Nodes[0] = ASourceNode (source expression) // only used for updatability
		//		Nodes[1] = ARootNode (source expression where root condition)
		//		Nodes[2] = AByNode (source expression where by condition)
		public static PlanNode EmitExplodeNode
		(
			Plan APlan, 
			Statement AStatement,
			PlanNode ASourceNode,
			PlanNode ARootNode, 
			PlanNode AParentNode, 
			IncludeColumnExpression ALevelColumn, 
			IncludeColumnExpression ASequenceColumn
		)
		{
			ExplodeNode LNode = (ExplodeNode)FindCallNode(APlan, AStatement, Instructions.Explode, new PlanNode[]{ASourceNode, ARootNode, AParentNode});
			LNode.LevelColumn = ALevelColumn;
			LNode.SequenceColumn = ASequenceColumn;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitExplodeNode
		(
			Plan APlan, 
			PlanNode ASourceNode,
			PlanNode ARootNode, 
			PlanNode AParentNode, 
			IncludeColumnExpression ALevelColumn, 
			IncludeColumnExpression ASequenceColumn
		)
		{
			return EmitExplodeNode(APlan, ASourceNode, ARootNode, AParentNode, ALevelColumn, ASequenceColumn);
		}
		
		public static PlanNode CompileExplodeColumnExpression(Plan APlan, ExplodeColumnExpression AExpression)
		{
			NameBindingContext LContext = new NameBindingContext(String.Format("{0}{1}{2}", Keywords.Parent, Keywords.Qualifier, AExpression.ColumnName), APlan.NameResolutionPath);
			PlanNode LNode = EmitIdentifierNode(APlan, AExpression, LContext);
			if (LNode == null)
				if (LContext.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, AExpression, String.Format("{0} {1}", Keywords.Parent, AExpression.ColumnName), ExceptionUtility.StringsToCommaList(LContext.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AExpression, String.Format("{0} {1}", Keywords.Parent, AExpression.ColumnName));
			return LNode;
		}
		
		public static PlanNode CompileExplodeExpression(Plan APlan, ExplodeExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			if (!(LSourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);
				
			LSourceNode = EnsureTableNode(APlan, LSourceNode);
				
			PlanNode LRootNode;
			PlanNode LParentNode;
			
			Expression LRootExpression = new RestrictExpression(AExpression.Expression, AExpression.RootExpression);
			Expression LByExpression = new RestrictExpression(AExpression.Expression, AExpression.ByExpression);
			
			if (AExpression.HasOrderByClause)
			{
				LRootExpression = new OrderExpression(LRootExpression, AExpression.OrderColumns);
				LByExpression = new OrderExpression(LByExpression, AExpression.OrderColumns);
			}
			
			LRootNode = CompileExpression(APlan, LRootExpression);
			if (!(LRootNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);
				
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(new Schema.RowType(((Schema.ITableType)LRootNode.DataType).Columns, Keywords.Parent)));
				try
				{
					LParentNode = CompileExpression(APlan, LByExpression);
					if (!(LParentNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableExpressionExpected, AExpression.Expression);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
			
			if ((AExpression.LevelColumn != null) || (AExpression.SequenceColumn != null))
			{
				if (!AExpression.HasOrderByClause && !APlan.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidExplodeExpression, CompilerErrorLevel.Warning, AExpression));
				
				if (AExpression.HasOrderByClause && !IsOrderUnique(APlan, ((TableNode)LSourceNode).TableVar, ((TableNode)LRootNode).Order) && !APlan.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidExplodeExpressionOrder, CompilerErrorLevel.Warning, AExpression));
			}	

			return EmitExplodeNode(APlan, AExpression, LSourceNode, LRootNode, LParentNode, AExpression.LevelColumn, AExpression.SequenceColumn);
		}
		
		public static PlanNode EmitExtendNode(Plan APlan, PlanNode ASourceNode, NamedColumnExpressions AExpressions)
		{
			return EmitExtendNode(APlan, new EmptyStatement(), ASourceNode, AExpressions);
		}
		
		public static PlanNode EmitExtendNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, NamedColumnExpressions AExpressions)
		{
			PlanNode LNode = FindCallNode(APlan, AStatement, Instructions.Extend, new PlanNode[]{ASourceNode});
			if (LNode is ExtendNode)
				((ExtendNode)LNode).Expressions = AExpressions;
			else
				((RowExtendNode)LNode).Expressions = AExpressions;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}	
		
		public static PlanNode CompileExtendExpression(Plan APlan, ExtendExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitExtendNode(APlan, AExpression, LSourceNode, AExpression.Expressions);
		}
		
		protected static IdentifierExpression CollapseQualifiedIdentifierExpression(Plan APlan, Expression AExpression)
		{
			if (AExpression is IdentifierExpression)
				return (IdentifierExpression)AExpression;
			
			if (AExpression is QualifierExpression)
			{
				IdentifierExpression LLeftExpression = CollapseQualifiedIdentifierExpression(APlan, ((QualifierExpression)AExpression).LeftExpression);
				IdentifierExpression LRightExpression = CollapseQualifiedIdentifierExpression(APlan, ((QualifierExpression)AExpression).RightExpression);
				if ((LLeftExpression != null) && (LRightExpression != null))
				{
					IdentifierExpression LResult = new IdentifierExpression(Schema.Object.Qualify(LRightExpression.Identifier, LLeftExpression.Identifier));
					LResult.Line = LLeftExpression.Line;
					LResult.LinePos = LRightExpression.Line;
					return LResult;
				}
			}
			
			return null;
		}
		
		public static string GetUniqueColumnName(StringCollection AColumnNames)
		{
			string LColumnName;
			int LIndex = 0;
			do
			{
				LIndex++;
				LColumnName = String.Format("Column{0}", LIndex.ToString());
			} while (AColumnNames.Contains(LColumnName));
			
			return LColumnName;
		}
		
		public static string GetUniqueColumnName(Schema.Columns AColumns)
		{
			string LColumnName;
			int LIndex = 0;
			do
			{
				LIndex++;
				LColumnName = String.Format("Column{0}", LIndex.ToString());
			} while (AColumns.ContainsName(LColumnName));
			
			return LColumnName;
		}
		
		public static PlanNode CompileSpecifyExpression(Plan APlan, SpecifyExpression AExpression)
		{
			PlanNode LNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			Schema.Columns LSourceColumns = LNode.DataType is Schema.ITableType ? ((Schema.ITableType)LNode.DataType).Columns : ((Schema.IRowType)LNode.DataType).Columns;
			
			NamedColumnExpressions LAddExpressions = new NamedColumnExpressions();
			RenameColumnExpressions LRenameExpressions = new RenameColumnExpressions();
			ColumnExpressions LProjectExpressions = new ColumnExpressions();
			
			StringCollection LResultNames = new StringCollection();
			StringCollection LProjectNames = new StringCollection();
			
			// Compute the list of result column names
			foreach (NamedColumnExpression LExpression in AExpression.Expressions)
			{
				IdentifierExpression LIdentifierExpression = CollapseQualifiedIdentifierExpression(APlan, LExpression.Expression);
				if ((LIdentifierExpression != null) && LSourceColumns.Contains(LIdentifierExpression.Identifier))
					LExpression.Expression = LIdentifierExpression;
					
				if (LExpression.ColumnAlias != String.Empty)
					LResultNames.Add(LExpression.ColumnAlias);
				else
				{
					if ((LIdentifierExpression != null) && LSourceColumns.Contains(LIdentifierExpression.Identifier))
						LResultNames.Add(LSourceColumns[LIdentifierExpression.Identifier].Name);
					else
						#if ALLOWUNNAMEDCOLUMNS
						LExpression.ColumnAlias = GetUniqueColumnName(LResultNames);
						#else
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, LExpression);
						#endif
				}
			}
			
			// If the expression is an identifier expression
				// if it is referencing a source column 
					// if it has no column alias or is a trivial column reference
						// it is a project column
					// else
						// if the source column name is also a result column name
							// it is an add column and a project column
						// else	
							// it is a project column and a rename column
				// else
			// else
				// it has a column alias (because of the conditions above)
				// if the column alias is a source column name
					// generate a unique column name LName
					// it is an add column with the column alias LName
					// LName is a project column
					// it is a rename column from LName to the column alias
			foreach (NamedColumnExpression LExpression in AExpression.Expressions)
			{
				IdentifierExpression LIdentifierExpression = LExpression.Expression as IdentifierExpression;
				if ((LIdentifierExpression != null) && LSourceColumns.ContainsName(LIdentifierExpression.Identifier))
				{
					if ((LExpression.ColumnAlias == String.Empty) || (LIdentifierExpression.Identifier == LExpression.ColumnAlias))
					{
						LProjectExpressions.Add(new ColumnExpression(LIdentifierExpression.Identifier));
						LProjectNames.Add(LSourceColumns[LIdentifierExpression.Identifier].Name);
					}
					else
					{
						if (LResultNames.Contains(LIdentifierExpression.Identifier) || LProjectNames.Contains(LSourceColumns[LIdentifierExpression.Identifier].Name))
						{
							LAddExpressions.Add(LExpression);
							ColumnExpression LProjectExpression = new ColumnExpression(LExpression.ColumnAlias);
							LProjectExpression.Line = LExpression.Line;
							LProjectExpression.LinePos = LExpression.LinePos;
							LProjectExpressions.Add(LProjectExpression);
						}
						else
						{
							ColumnExpression LProjectExpression = new ColumnExpression(LIdentifierExpression.Identifier);
							LProjectExpression.Line = LIdentifierExpression.Line;
							LProjectExpression.LinePos = LIdentifierExpression.LinePos;
							LProjectExpressions.Add(LProjectExpression);
							LProjectNames.Add(LSourceColumns[LIdentifierExpression.Identifier].Name);
							RenameColumnExpression LRenameExpression = new RenameColumnExpression(LIdentifierExpression.Identifier, LExpression.ColumnAlias);
							LRenameExpression.Line = LIdentifierExpression.Line;
							LRenameExpression.LinePos = LIdentifierExpression.Line;
							LRenameExpression.MetaData = LExpression.MetaData;
							LRenameExpressions.Add(LRenameExpression);
						}
					}
				}
				else
				{
					if (LSourceColumns.ContainsName(LExpression.ColumnAlias))
					{
						string LColumnAlias = Schema.Object.GetUniqueName();
						NamedColumnExpression LAddExpression = new NamedColumnExpression(LExpression.Expression, LColumnAlias, LExpression.MetaData);
						LAddExpression.Line = LExpression.Line;
						LAddExpression.LinePos = LExpression.LinePos;
						LAddExpressions.Add(LAddExpression);
						ColumnExpression LProjectExpression = new ColumnExpression(LColumnAlias);
						LProjectExpression.Line = LExpression.Line;
						LProjectExpression.LinePos = LExpression.LinePos;
						LProjectExpressions.Add(LProjectExpression);
						RenameColumnExpression LRenameExpression = new RenameColumnExpression(LColumnAlias, LExpression.ColumnAlias);
						LRenameExpression.Line = LExpression.Line;
						LRenameExpression.LinePos = LExpression.LinePos;
						LRenameExpressions.Add(LRenameExpression);
					}
					else
					{
						LAddExpressions.Add(LExpression);
						ColumnExpression LProjectExpression = new ColumnExpression(Schema.Object.EnsureRooted(LExpression.ColumnAlias));
						LProjectExpression.Line = LExpression.Line;
						LProjectExpression.LinePos = LExpression.LinePos;
						LProjectExpressions.Add(LProjectExpression);
					}
				}
			}
			
			if (LAddExpressions.Count > 0)
				LNode = EmitExtendNode(APlan, LNode, LAddExpressions);
				
			LNode = EmitProjectNode(APlan, AExpression, LNode, LProjectExpressions, true);
			
			if (LRenameExpressions.Count > 0)
				LNode = EmitRenameNode(APlan, LNode, LRenameExpressions);
				
			return LNode;
		}
		
		public static PlanNode EmitRenameNode(Plan APlan, PlanNode ASourceNode, string ATableAlias)
		{
			return EmitRenameNode(APlan, ASourceNode, ATableAlias, null);
		}

		public static PlanNode EmitRenameNode(Plan APlan, PlanNode ASourceNode, string ATableAlias, MetaData AMetaData)
		{
			return EmitRenameNode(APlan, new EmptyStatement(), ASourceNode, ATableAlias, AMetaData);
		}
		
		// operator Rename(table{}, string, object) : table{}		
		public static PlanNode EmitRenameNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, string ATableAlias, MetaData AMetaData)
		{
			PlanNode LNode = FindCallNode(APlan, AStatement, Instructions.Rename, new PlanNode[]{ASourceNode});
			if (LNode is RenameNode)
			{
				RenameNode LRenameNode = (RenameNode)LNode;
				LRenameNode.TableAlias = ATableAlias;
				LRenameNode.MetaData = AMetaData;
			}
			else
			{
				RowRenameNode LRowRenameNode = (RowRenameNode)LNode;
				LRowRenameNode.RowAlias = ATableAlias;
				LRowRenameNode.MetaData = AMetaData;
			}
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}

		public static PlanNode EmitRenameNode(Plan APlan, PlanNode ASourceNode, RenameColumnExpressions AExpressions)
		{
			return EmitRenameNode(APlan, new EmptyStatement(), ASourceNode, AExpressions);
		}
		
		// operator Rename(table{}, object) : table{}		
		public static PlanNode EmitRenameNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, RenameColumnExpressions AExpressions)
		{
			PlanNode LNode = FindCallNode(APlan, AStatement, Instructions.Rename, new PlanNode[]{ASourceNode});
			if (LNode is RenameNode)
				((RenameNode)LNode).Expressions = AExpressions;
			else
				((RowRenameNode)LNode).Expressions = AExpressions;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileRenameExpression(Plan APlan, RenameExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitRenameNode(APlan, AExpression, LSourceNode, AExpression.Expressions);
		}
		
		public static PlanNode CompileRenameAllExpression(Plan APlan, RenameAllExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitRenameNode(APlan, AExpression, LSourceNode, AExpression.Identifier, AExpression.MetaData);
		}
		
		public static PlanNode CompileIsExpression(Plan APlan, IsExpression AExpression)
		{
			IsNode LResult = new IsNode();
			LResult.Nodes.Add(CompileExpression(APlan, AExpression.Expression));
			LResult.TargetType = CompileTypeSpecifier(APlan, AExpression.TypeSpecifier);
			LResult.DetermineDataType(APlan);
			LResult.DetermineCharacteristics(APlan);
			return LResult;
		}
		
		public static PlanNode CompileAsExpression(Plan APlan, AsExpression AExpression)
		{
			AsNode LResult = new AsNode();
			PlanNode LNode = CompileExpression(APlan, AExpression.Expression);
			LResult.DataType = CompileTypeSpecifier(APlan, AExpression.TypeSpecifier);
			if (!LResult.DataType.Is(LNode.DataType))
				throw new CompilerException(CompilerException.Codes.InvalidCast, AExpression, LNode.DataType.Name, LResult.DataType.Name);
			LResult.Nodes.Add(Downcast(APlan, LNode, LResult.DataType));
			LResult.DetermineCharacteristics(APlan);
			return LResult;
		}
		
		#if CALCULESQUE
		public static PlanNode CompileNamedExpression(Plan APlan, NamedExpression AExpression)
		{
			return CompileExpression(APlan, AExpression.Expression);
		}
		#endif
		
		public static PlanNode EmitAdornNode(Plan APlan, PlanNode ASourceNode, AdornExpression AAdornExpression)
		{
			return EmitAdornNode(APlan, null, ASourceNode, AAdornExpression);
		}
		
		public static PlanNode EmitAdornNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, AdornExpression AAdornExpression)
		{
			AdornNode LNode = (AdornNode)FindCallNode(APlan, AStatement, Instructions.Adorn, new PlanNode[]{ASourceNode});
			LNode.Expressions = AAdornExpression.Expressions;
			LNode.Constraints = AAdornExpression.Constraints;
			LNode.Orders = AAdornExpression.Orders;
			LNode.AlterOrders = AAdornExpression.AlterOrders;
			LNode.DropOrders = AAdornExpression.DropOrders;
			LNode.Keys = AAdornExpression.Keys;
			LNode.AlterKeys = AAdornExpression.AlterKeys;
			LNode.DropKeys = AAdornExpression.DropKeys;
			LNode.References = AAdornExpression.References;
			LNode.AlterReferences = AAdornExpression.AlterReferences;
			LNode.DropReferences = AAdornExpression.DropReferences;
			LNode.MetaData = AAdornExpression.MetaData;
			LNode.AlterMetaData = AAdornExpression.AlterMetaData;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileAdornExpression(Plan APlan, AdornExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitAdornNode(APlan, AExpression, LSourceNode, AExpression);
		}
		
		public static PlanNode EmitRedefineNode(Plan APlan, PlanNode ASourceNode, NamedColumnExpressions AExpressions)
		{
			return EmitRedefineNode(APlan, new EmptyStatement(), ASourceNode, AExpressions);
		}
		
		// T redefine { A := A + A } ::= 
		//	T add { A + A Temp } remove { A } rename { Temp A }
		public static PlanNode EmitRedefineNode(Plan APlan, Statement AStatement, PlanNode ASourceNode, NamedColumnExpressions AExpressions)
		{
			#if USEREDEFINENODE
			PlanNode LNode = FindCallNode(APlan, AStatement, Instructions.Redefine, new PlanNode[]{ASourceNode});
			if (LNode is RedefineNode)
				((RedefineNode)LNode).Expressions = AExpressions;
			else
				((RowRedefineNode)LNode).Expressions = AExpressions;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			if ((LNode is RedefineNode) && ((RedefineNode)LNode).DistinctRequired)
				return EmitProjectNode(APlan, LNode, FindClusteringKey(APlan, ((TableNode)LNode).TableVar));
			return LNode;
			#else
			NamedColumnExpressions LAddExpressions = new NamedColumnExpressions();
			RenameColumnExpressions LRenameExpressions = new RenameColumnExpressions();
			string[] LRemoveColumns = new string[AExpressions.Count];
			string LColumnName;
			for (int LIndex = 0; LIndex < AExpressions.Count; LIndex++)
			{
				LColumnName = Schema.Object.GetUniqueName();
				LAddExpressions.Add(new NamedColumnExpression(AExpressions[LIndex].Expression, LColumnName));
				LRemoveColumns[LIndex] = AExpressions[LIndex].ColumnAlias;
				LRenameExpressions.Add(new RenameColumnExpression(LColumnName, AExpressions[LIndex].ColumnAlias));
			}

			return 
				EmitRenameNode
				(
					APlan, 
					AStatement, 
					EmitProjectNode
					(
						APlan, 
						AStatement, 
						EmitExtendNode
						(
							APlan, 
							AStatement, 
							ASourceNode, 
							LAddExpressions
						), 
						LRemoveColumns, 
						false
					), 
					LRenameExpressions
				);
			#endif
		}
		
		public static PlanNode CompileRedefineExpression(Plan APlan, RedefineExpression AExpression)
		{
			PlanNode LSourceNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression);
			try
			{
				LSourceNode = CompileExpression(APlan, AExpression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitRedefineNode(APlan, AExpression, LSourceNode, AExpression.Expressions);
		}

		public static PlanNode CompileOnExpression(Plan APlan, OnExpression AExpression)
		{
			OnNode LNode = (OnNode)FindCallNode(APlan, AExpression, Instructions.On, new PlanNode[]{});
			LNode.OnExpression = AExpression;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitUnionNode(Plan APlan, Statement AStatement, PlanNode ALeftNode, PlanNode ARightNode)
		{
			return 
				EmitCallNode
				(
					APlan, 
					AStatement,
					Instructions.Union, 
					new PlanNode[]{ALeftNode, ARightNode}
				);
		}

		// note that the raw union operator will return a tuple-bag, the compiler must ensure
		// that the operator is wrapped by a projection node to ensure uniqueness		
		public static PlanNode CompileUnionExpression(Plan APlan, UnionExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode LRightNode = null;
			LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return EmitUnionNode(APlan, AExpression, LLeftNode, LRightNode);
		}
		
		public static PlanNode EmitDifferenceNode(Plan APlan, Statement AStatement, PlanNode ALeftNode, PlanNode ARightNode)
		{
			return EmitCallNode(APlan, AStatement, Instructions.Difference, new PlanNode[]{ALeftNode, ARightNode});
		}
		
		public static PlanNode CompileDifferenceExpression(Plan APlan, DifferenceExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode LRightNode = null;
			LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return 
				EmitDifferenceNode
				(
					APlan, 
					AExpression,
					LLeftNode,
					LRightNode
				);
		}
		
		public static PlanNode CompileIntersectExpression(Plan APlan, IntersectExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode LRightNode = null;
			LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			JoinNode LNode = (JoinNode)FindCallNode(APlan, AExpression, Instructions.Join, new PlanNode[]{LLeftNode, LRightNode});
			LNode.IsNatural = true;
			LNode.IsIntersect = true;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileProductExpression(Plan APlan, ProductExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode LRightNode = null;
			LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			JoinNode LNode = (JoinNode)FindCallNode(APlan, AExpression, Instructions.Join, new PlanNode[]{LLeftNode, LRightNode});
			LNode.IsNatural = true;
			LNode.IsTimes = true;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode EmitHavingNode(Plan APlan, Statement AStatement, PlanNode ALeftNode, PlanNode ARightNode, Expression ACondition)
		{
			HavingNode LNode = (HavingNode)FindCallNode(APlan, AStatement, Instructions.Having, new PlanNode[]{ALeftNode, ARightNode});
			if (ACondition == null)
				LNode.IsNatural = true;
			else
				LNode.Expression = ACondition;
				
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileHavingExpression(Plan APlan, HavingExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode LRightNode = null;
			LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return 
				EmitHavingNode
				(
					APlan, 
					AExpression,
					LLeftNode,
					LRightNode,
					AExpression.Condition
				);
		}
		
		public static PlanNode EmitWithoutNode(Plan APlan, Statement AStatement, PlanNode ALeftNode, PlanNode ARightNode, Expression ACondition)
		{
			WithoutNode LNode = (WithoutNode)FindCallNode(APlan, AStatement, Instructions.Without, new PlanNode[]{ALeftNode, ARightNode});
			if (ACondition == null)
				LNode.IsNatural = true;
			else
				LNode.Expression = ACondition;
				
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileWithoutExpression(Plan APlan, WithoutExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			PlanNode LRightNode = null;
			LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			return 
				EmitWithoutNode
				(
					APlan, 
					AExpression,
					LLeftNode,
					LRightNode,
					AExpression.Condition
				);
		}
		
		public static PlanNode EmitInnerJoinNode
		(
			Plan APlan, 
			PlanNode ALeftNode,
			PlanNode ARightNode,
			InnerJoinExpression AExpression
		)
		{
			PlanNode LPlanNode = FindCallNode(APlan, AExpression, Instructions.Join, new PlanNode[]{ALeftNode, ARightNode});
			if (LPlanNode is JoinNode)
			{
				JoinNode LNode = (JoinNode)LPlanNode;
				LNode.Expression = AExpression.Condition;
				LNode.IsLookup = AExpression.IsLookup;
				LNode.IsNatural = AExpression.Condition == null;
				LNode.DetermineDataType(APlan);
				LNode.DetermineCharacteristics(APlan);
				return LNode;
			}
			else
			{
				if (AExpression.Condition != null)
					throw new CompilerException(CompilerException.Codes.InvalidRowJoin, AExpression.Condition);
				LPlanNode.DetermineDataType(APlan);
				LPlanNode.DetermineCharacteristics(APlan);
				return LPlanNode;
			}
		}
		
		public static PlanNode CompileInnerJoinExpression(Plan APlan, InnerJoinExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}

			LTransaction = null;
			if (APlan.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.GetApplicationTransaction();
			try
			{
				bool LIsLookup = AExpression.IsLookup ? !Convert.ToBoolean(LanguageModifiers.GetModifier(AExpression.Modifiers, "IsDetailLookup", "false")) : AExpression.IsLookup;
				bool LShouldTranslate = Convert.ToBoolean(LanguageModifiers.GetModifier(AExpression.Modifiers, "Right.ShouldTranslate", (!LIsLookup).ToString()));
				if ((LTransaction != null) && !LShouldTranslate)
					LTransaction.PushLookup();
				try
				{
					PlanNode LRightNode = CompileExpression(APlan, AExpression.RightExpression);
					PlanNode LResultNode = EmitInnerJoinNode(APlan, LLeftNode, LRightNode, AExpression);
					JoinNode LJoinNode = LResultNode as JoinNode;
					if ((LJoinNode != null) && (LTransaction != null) && !LShouldTranslate && LJoinNode.IsDetailLookup)
					{
						LTransaction.PopLookup();
						try
						{
							LRightNode = CompileExpression(APlan, AExpression.RightExpression);
							return EmitInnerJoinNode(APlan, LLeftNode, LRightNode, AExpression);
						}
						finally
						{
							LTransaction.PushLookup();
						}
					}
					else
						return LResultNode;
				}
				finally
				{
					if ((LTransaction != null) && !LShouldTranslate)
						LTransaction.PopLookup();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}
		
		public static Schema.TableVarColumn CompileIncludeColumnExpression
		(
			Plan APlan, 
			IncludeColumnExpression AColumn, 
			string AColumnName, 
			Schema.IScalarType ADataType, 
			Schema.TableVarColumnType AColumnType
		)
		{
			Schema.Column LColumn =	
				new Schema.Column
				(
					AColumn.ColumnAlias == String.Empty ? AColumnName : AColumn.ColumnAlias, 
					ADataType
				);

			Schema.TableVarColumn LTableVarColumn =				
				new Schema.TableVarColumn
				(
					LColumn,
					AColumn.MetaData, 
					AColumnType
				);
				
			//LTableVarColumn.Default = LColumn.DataType.Default; // cant do this because it would change the parent of the default, even if the defaults were of the same type, which they are not
			return LTableVarColumn;
		}

		public static LeftOuterJoinNode EmitLeftOuterJoinNode
		(
			Plan APlan, 
			PlanNode ALeftNode, 
			PlanNode ARightNode, 
			LeftOuterJoinExpression AExpression
		)
		{
			LeftOuterJoinNode LNode = (LeftOuterJoinNode)FindCallNode(APlan, AExpression, Instructions.LeftJoin, new PlanNode[]{ALeftNode, ARightNode});
			LNode.Expression = AExpression.Condition;
			LNode.IsLookup = AExpression.IsLookup;
			LNode.IsNatural = AExpression.Condition == null;
			LNode.RowExistsColumn = AExpression.RowExistsColumn;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileLeftOuterJoinExpression(Plan APlan, LeftOuterJoinExpression AExpression)
		{
			PlanNode LLeftNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Left");
			try
			{
				LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}
			
			LTransaction = null;
			if (APlan.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.GetApplicationTransaction();
			try
			{
				bool LIsLookup = AExpression.IsLookup ? !Convert.ToBoolean(LanguageModifiers.GetModifier(AExpression.Modifiers, "IsDetailLookup", "false")) : AExpression.IsLookup;
				bool LShouldTranslate = Convert.ToBoolean(LanguageModifiers.GetModifier(AExpression.Modifiers, "Right.ShouldTranslate", (!LIsLookup).ToString()));
				if ((LTransaction != null) && !LShouldTranslate)
					LTransaction.PushLookup();
				try
				{
					PlanNode LRightNode = CompileExpression(APlan, AExpression.RightExpression);
					LeftOuterJoinNode LNode = EmitLeftOuterJoinNode(APlan, LLeftNode, LRightNode, AExpression);
					if ((LTransaction != null) && !LShouldTranslate && LNode.IsDetailLookup)
					{
						LTransaction.PopLookup();
						try
						{
							LRightNode = CompileExpression(APlan, AExpression.RightExpression);
							return EmitLeftOuterJoinNode(APlan, LLeftNode, LRightNode, AExpression);
						}
						finally
						{
							LTransaction.PushLookup();
						}
					}
					else
						return LNode;
				}
				finally
				{
					if ((LTransaction != null) && !LShouldTranslate)
						LTransaction.PopLookup();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}
		
		public static RightOuterJoinNode EmitRightOuterJoinNode
		(
			Plan APlan, 
			PlanNode ALeftNode, 
			PlanNode ARightNode, 
			RightOuterJoinExpression AExpression
		)
		{
			RightOuterJoinNode LNode = (RightOuterJoinNode)FindCallNode(APlan, AExpression, Instructions.RightJoin, new PlanNode[]{ALeftNode, ARightNode});
			LNode.Expression = AExpression.Condition;
			LNode.IsLookup = AExpression.IsLookup;
			LNode.IsNatural = AExpression.Condition == null;
			LNode.RowExistsColumn = AExpression.RowExistsColumn;
			LNode.DetermineDataType(APlan);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static PlanNode CompileRightOuterJoinExpression(Plan APlan, RightOuterJoinExpression AExpression)
		{
			PlanNode LRightNode = null;
			ApplicationTransaction LTransaction = PrepareShouldTranslate(APlan, AExpression, "Right");
			try
			{
				LRightNode = CompileExpression(APlan, AExpression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(APlan, LTransaction);
			}
			
			LTransaction = null;
			if (APlan.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.GetApplicationTransaction();
			try
			{
				bool LIsLookup = AExpression.IsLookup ? !Convert.ToBoolean(LanguageModifiers.GetModifier(AExpression.Modifiers, "IsDetailLookup", "false")) : AExpression.IsLookup;
				bool LShouldTranslate = Convert.ToBoolean(LanguageModifiers.GetModifier(AExpression.Modifiers, "Left.ShouldTranslate", (!LIsLookup).ToString()));
				if ((LTransaction != null) && !LShouldTranslate)
					LTransaction.PushLookup();
				try
				{
					PlanNode LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
					RightOuterJoinNode LNode = EmitRightOuterJoinNode(APlan, LLeftNode, LRightNode, AExpression);
					if ((LTransaction != null) && !LShouldTranslate && LNode.IsDetailLookup)
					{
						LTransaction.PopLookup();
						try
						{
							LLeftNode = CompileExpression(APlan, AExpression.LeftExpression);
							return EmitRightOuterJoinNode(APlan, LLeftNode, LRightNode, AExpression);
						}
						finally
						{
							LTransaction.PushLookup();
						}
					}
					else
						return LNode;
				}
				finally
				{
					if ((LTransaction != null) && !LShouldTranslate)
						LTransaction.PopLookup();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}

		public static PlanNode CompileEmptyStatement(Plan APlan, Statement AStatement)
		{
			PlanNode LNode = new BlockNode();
			LNode.SetLineInfo(AStatement.LineInfo);
			LNode.DetermineCharacteristics(APlan);
			return LNode;
		}
		
		public static Symbols SnapshotSymbols(Plan APlan)
		{
			Symbols LSymbols = new Symbols(APlan.Symbols.MaxStackDepth, APlan.Symbols.MaxCallDepth);
			for (int LIndex = APlan.Symbols.Count - 1; LIndex >= 0; LIndex--)
				LSymbols.Push(APlan.Symbols.Peek(LIndex));

			return LSymbols;
		}
	}
}

