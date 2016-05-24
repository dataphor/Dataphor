/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USEOPERATORRESOLUTIONCACHE
#define USECONVERSIONPATHCACHE
//#define USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
//#define DISALLOWAMBIGUOUSNAMES
#define USENAMEDROWVARIABLES

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Alphora.Dataphor.DAE.Compiling
{
	using Alphora.Dataphor.DAE.Compiling.Visitors;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;
	using Schema = Alphora.Dataphor.DAE.Schema;

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

		3VL Truth Tables ->

		Propositional Logic Transformation Rules ->

			Identity
				p | false == p
				p & true == p
			Domination
				p | true == true
				p & false == false
			Double-Negation
				~(~p) == p
			Idempotence
				p & p == p
				p | p == p
			Contradiction (only holds for 2VL)
				p & ~p == false
			Tautology (only holds for 2VL)
				p | ~p == true
			Commutativity
				p & q = q & p
				p | q = q | p
			Associativity
				p & (q & r) == (p & q) & r
				p | (q | r) == (p | q) | r
			Distributivity
				p & (q | r) == (p & q) | (p & r)
				p | (q & r) == (p | q) & (p | r)
			Simplification
				p & (p | q) == p
				p | (p & q) == p
			DeMorgan's Laws
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
			BindingTraversal 
			DetermineDevice
			DetermineCursorBehavior
			DetermineModifyBinding
	*/	
	[Flags]
	public enum NameBindingFlags { Local = 1, Global = 2, Default = 3 }
	
	public class NameBindingContext : System.Object
	{
		public NameBindingContext(string identifier, Schema.NameResolutionPath resolutionPath) : base()
		{
			_identifier = identifier;
			_resolutionPath = resolutionPath;
		}
		
		public NameBindingContext(string identifier, Schema.NameResolutionPath resolutionPath, NameBindingFlags flags) : this(identifier, resolutionPath)
		{
			_bindingFlags = flags;
		}
		
		private string _identifier;
		/// <summary>The identifier being resolved for.</summary>
		public string Identifier { get { return _identifier; } }
		
		private NameBindingFlags _bindingFlags = NameBindingFlags.Default;
		/// <summary>Indicates whether the identifier is to be resolved locally, globally, or both.</summary>
		public NameBindingFlags BindingFlags { get { return _bindingFlags; } set { _bindingFlags = value; } }
		
		[Reference]
		private Schema.NameResolutionPath _resolutionPath;
		/// <summary>The resolution path used to resolve the identifier.</summary>
		public Schema.NameResolutionPath ResolutionPath { get { return _resolutionPath; } }

		/// <summary>The schema object which the identifier resolved to if the resolution was successful, null otherwise.</summary>		
		[Reference]
		public Schema.Object Object;

		private List<string> _names = new List<string>();
		/// <summary>The list of names from the namespace which the identifier matches.</summary>
		public List<string> Names { get { return _names; } }

		/// <summary>Returns true if the identifier could not be resolved because it matched multiple names in the namespace.</summary>
		public bool IsAmbiguous { get { return _names.Count > 1; } }
		
		/// <summary>Sets the binding data for this context to the binding data of the given context.</summary>
		public void SetBindingDataFromContext(NameBindingContext context)
		{
			Object = context.Object;
			_names.Clear();
			foreach (string stringValue in context.Names)
				_names.Add(stringValue);
		}
	}
	
	public class OperatorBindingContext : System.Object
	{
		public OperatorBindingContext(Statement statement, string operatorName, Schema.NameResolutionPath resolutionPath, Schema.Signature callSignature, bool isExact) : base()
		{
			_statement = statement;
			_operatorNameContext = new NameBindingContext(operatorName, resolutionPath);
			if (callSignature == null)
				Error.Fail(String.Format("Call signature null in operator binding context for operator {0}", operatorName));
			_callSignature = callSignature;
			_isExact = isExact;
		}
		
		private Statement _statement;
		/// <summary>Gets the statement which originated the binding request.</summary>
		public Statement Statement { get { return _statement; } }
		
		private NameBindingContext _operatorNameContext;
		/// <summary>Gets the name binding context used to resolve the operator name.</summary>
		public NameBindingContext OperatorNameContext { get { return _operatorNameContext; } }

		/// <summary>Gets the operator name being resolved for.</summary>		
		public string OperatorName { get { return _operatorNameContext.Identifier; } }
		
		/// <summary>Gets the name resolution path being used to perform the resolution.</summary>
		public Schema.NameResolutionPath ResolutionPath { get { return _operatorNameContext.ResolutionPath; } }
		
		private Schema.Signature _callSignature;
		/// <summary>Gets the signature of the call being resolved for.</summary>
		public Schema.Signature CallSignature { get { return _callSignature; } }
		
		private bool _isExact;
		/// <summary>Indicates that the resolution must be exact. (No casting or conversion)</summary>
		public bool IsExact { get { return _isExact; } }
		
		public override bool Equals(object objectValue)
		{
			OperatorBindingContext localObjectValue = objectValue as OperatorBindingContext;
			return 
				(localObjectValue != null) && 
				localObjectValue.OperatorName.Equals(OperatorName) && 
				localObjectValue.CallSignature.Equals(CallSignature) && 
				(localObjectValue.IsExact == IsExact) &&
				(localObjectValue.ResolutionPath == ResolutionPath);
		}

		public override int GetHashCode()
		{
			return OperatorName.GetHashCode() ^ CallSignature.GetHashCode() ^ IsExact.GetHashCode() ^ ResolutionPath.GetHashCode();
		}

		/// <summary>Indicates that the operator name was resolved, not necessarily correctly (it could be ambiguous)</summary>
		public bool IsOperatorNameResolved { get { return _operatorNameContext.IsAmbiguous || (_operatorNameContext.Object != null); } }
		
		/// <summary>The operator resolved, if a successful resolution is possible, null otherwise.</summary>
		[Reference]
		public Schema.Operator Operator;
		
		private OperatorMatches _matches = new OperatorMatches();
		/// <summary>All the possible matches found for the calling signature.</summary>
		public OperatorMatches Matches { get { return _matches; } }
		
		/// <summary>Sets the binding data for this context to the binding data of the given context.</summary>
		public void SetBindingDataFromContext(OperatorBindingContext context)
		{
			_operatorNameContext.SetBindingDataFromContext(context.OperatorNameContext);
			Operator = context.Operator;
			_matches.Clear();
			_matches.AddRange(context.Matches);
		}
		
		public void MergeBindingDataFromContext(OperatorBindingContext context)
		{
			_operatorNameContext.SetBindingDataFromContext(context.OperatorNameContext);
			foreach (OperatorMatch match in context.Matches)
				if (!_matches.Contains(match))
					_matches.Add(match);

			if (_matches.IsExact || (!_isExact && _matches.IsPartial))
				Operator = _matches.Match.Signature.Operator;
			else
				Operator = null;
		}
	}
	
	public class ConversionContext : System.Object
	{
		public ConversionContext(Schema.IDataType sourceType, Schema.IDataType targetType) : base()
		{
			_sourceType = sourceType;
			_targetType = targetType;
			_canConvert = _sourceType.IsNil || _sourceType.Is(_targetType);
		}
		
		[Reference]
		private Schema.IDataType _sourceType;
		public Schema.IDataType SourceType { get { return _sourceType; } }
		
		[Reference]
		private Schema.IDataType _targetType;
		public Schema.IDataType TargetType { get { return _targetType; } }
		
		private bool _canConvert;
		public virtual bool CanConvert 
		{ 
			get { return _canConvert; } 
			set { _canConvert = value; }
		}
		
		public virtual int NarrowingScore { get { return 0; } }
		
		public virtual int PathLength { get { return 0; } }
	}
	
	public class TableConversionContext : ConversionContext
	{
		public TableConversionContext(Schema.ITableType sourceType, Schema.ITableType targetType) : base(sourceType, targetType)
		{
			_sourceType = sourceType;
			_targetType = targetType;
		}
		
		[Reference]
		private Schema.ITableType _sourceType;
		public new Schema.ITableType SourceType { get { return _sourceType; } }

		[Reference]
		private Schema.ITableType _targetType;
		public new Schema.ITableType TargetType { get { return _targetType; } }

		private Dictionary<string, ConversionContext> _columnConversions = new Dictionary<string, ConversionContext>();
		public Dictionary<string, ConversionContext> ColumnConversions { get { return _columnConversions; } }
		
		public override int NarrowingScore
		{
			get
			{
				int narrowingScore = 0;
				foreach (KeyValuePair<string, ConversionContext> entry in ColumnConversions)
					narrowingScore += entry.Value.NarrowingScore;
				return narrowingScore;
			}
		}
		
		public override int PathLength
		{
			get
			{
				int pathLength = 0;
				foreach (KeyValuePair<string, ConversionContext> entry in ColumnConversions)
					pathLength += entry.Value.PathLength;
				return pathLength;
			}
		}
	}
	
	public class RowConversionContext : ConversionContext
	{
		public RowConversionContext(Schema.IRowType sourceType, Schema.IRowType targetType) : base(sourceType, targetType)
		{
			_sourceType = sourceType;
			_targetType = targetType;
		}
		
		[Reference]
		private Schema.IRowType _sourceType;
		public new Schema.IRowType SourceType { get { return _sourceType; } }

		[Reference]
		private Schema.IRowType _targetType;
		public new Schema.IRowType TargetType { get { return _targetType; } }

		private Dictionary<string, ConversionContext> _columnConversions = new Dictionary<string, ConversionContext>();
		public Dictionary<string, ConversionContext> ColumnConversions { get { return _columnConversions; } }

		public override int NarrowingScore
		{
			get
			{
				int narrowingScore = 0;
				foreach (KeyValuePair<string, ConversionContext> entry in ColumnConversions)
					narrowingScore += entry.Value.NarrowingScore;
				return narrowingScore;
			}
		}
		
		public override int PathLength
		{
			get
			{
				int pathLength = 0;
				foreach (KeyValuePair<string, ConversionContext> entry in ColumnConversions)
					pathLength += entry.Value.PathLength;
				return pathLength;
			}
		}
	}
	
	public class ScalarConversionContext : ConversionContext
	{
		public ScalarConversionContext(Schema.ScalarType sourceType, Schema.ScalarType targetType) : base(sourceType, targetType)
		{
			_sourceType = sourceType;
			_targetType = targetType;
		}
		
		[Reference]
		private Schema.ScalarType _sourceType;
		public new Schema.ScalarType SourceType { get { return _sourceType; } }

		[Reference]
		private Schema.ScalarType _targetType;
		public new Schema.ScalarType TargetType { get { return _targetType; } }
		
		private Schema.ScalarConversionPath _currentPath = new Schema.ScalarConversionPath();
		public Schema.ScalarConversionPath CurrentPath { get { return _currentPath; } }
		
		private Schema.ScalarConversionPaths _paths = new Schema.ScalarConversionPaths();
		public Schema.ScalarConversionPaths Paths { get { return _paths; } }
		
		/// <summary>Returns true if ASourceType is ATargetType or there is only one conversion path with the best narrowing score, false otherwise.</summary>
		public override bool CanConvert 
		{ 
			get { return _sourceType.IsNil || _targetType.IsGeneric || _paths.CanConvert; } 
			set { }
		}
		
		/// <summary>Contains the set of conversion paths with the current best narrowing score.</summary>
		public Schema.ScalarConversionPathList BestPaths { get { return _paths.BestPaths; } }
		
		/// <summary>Returns the single conversion path with the best narrowing score, null if there are multiple paths with the same score.</summary>
		public Schema.ScalarConversionPath BestPath { get { return _paths.BestPath; } }
		
		public override int NarrowingScore { get { return BestPath == null ? ((CanConvert && _paths.Count == 0) ? 0 : Int32.MinValue) : BestPath.NarrowingScore; } }
		
		public override int PathLength { get { return BestPath == null ? ((CanConvert && _paths.Count == 0) ? 0 : Int32.MaxValue) : BestPath.Count; } }
	}
	
	public abstract class Compiler : Object
	{
		public const string IsSpecialOperatorName = @"IsSpecial";
		public const string IsNilOperatorName = @"IsNil";
		public const string IsSpecialComparerPrefix = @"Is";
		public const string ReadAccessorName = @"Read";
		public const string WriteAccessorName = @"Write";
		
		// Compile overloads which take a string as input
		public static PlanNode Compile(Plan plan, string statement)
		{
			return Compile(plan, statement, null, false);
		}
		
		public static PlanNode Compile(Plan plan, string statement, DataParams paramsValue)
		{
			return Compile(plan, statement, paramsValue, false);
		}
		
		public static PlanNode Compile(Plan plan, string statement, bool isCursor)
		{
			return Compile(plan, statement, null, isCursor);
		}

		// Main compile method with string input which all overloads that take strings call		
		public static PlanNode Compile(Plan plan, string statement, DataParams paramsValue, bool isCursor)
		{
			Statement localStatement;
			if (plan.Language == QueryLanguage.RealSQL)
				localStatement = new RealSQL.Compiler().Compile(new RealSQL.Parser().ParseStatement(statement));
			else
				localStatement = isCursor ? new Parser().ParseCursorDefinition(statement) : new Parser().ParseScript(statement, null);
			return Compile(plan, localStatement, paramsValue, isCursor);
		}

		// Compile overloads which take a syntax tree as input
		public static PlanNode Compile(Plan plan, Statement statement)
		{
			return Compile(plan, statement, null, false);
		}
		
		public static PlanNode Compile(Plan plan, Statement statement, DataParams paramsValue)
		{
			return Compile(plan, statement, paramsValue, false);
		}
		
		public static PlanNode Compile(Plan plan, Statement statement, bool isCursor)
		{
			return Compile(plan, statement, null, isCursor);
		}

		public static PlanNode Compile(Plan plan, Statement statement, DataParams paramsValue, bool isCursor)
		{
			return Compile(plan, statement, paramsValue, isCursor, null);
		}

		// Main compile method which all overloads eventually call		
		public static PlanNode Compile(Plan plan, Statement statement, DataParams paramsValue, bool isCursor, SourceContext sourceContext)
		{
			// Prepare plan timers
			long startTicks = TimingUtility.CurrentTicks;
			
			// Prepare the stack for compilation with the given context
			if (paramsValue != null)
				foreach (DataParam param in paramsValue)
				{
					plan.Symbols.Push(new Symbol(param.Name, param.DataType));
					Schema.Catalog dependencies = new Schema.Catalog();
					param.DataType.IncludeDependencies(plan.CatalogDeviceSession, plan.Catalog, dependencies, EmitMode.ForCopy);
					foreach (var dependency in dependencies)
						plan.AttachDependency(dependency);
				}
			
			PlanNode node = null;
			plan.PushSourceContext(sourceContext);
			try
			{
				try
				{
					if (isCursor)
						node = CompileCursor(plan, statement is Expression ? (Expression)statement : ((SelectStatement)statement).CursorDefinition);
					else
					{
						plan.Symbols.PushFrame();
						try
						{
							node = CompileStatement(plan, statement);
							plan.ReportProcessSymbols();
						}
						finally
						{
							plan.Symbols.PopFrame();
						}
					}
				}
				catch (Exception exception)
				{
					if (!(exception is CompilerException) || (((CompilerException)exception).Code != (int)CompilerException.Codes.NonFatalErrors))
						plan.Messages.Add(exception);
				}
			}
			finally
			{
				plan.PopSourceContext();
			}
			
			plan.Statistics.CompileTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));

			// Set the locators for the compiler exceptions
			if (sourceContext != null && sourceContext.Locator != null)
				plan.Messages.SetLocator(sourceContext.Locator);

			// Optimization and Chunking
			//
			// Phase I -
			//		Improved Chunking
			//			Depth-First Traversal to determine potential devices
			//				Include operator support determination
			//			Top-Down actual support determination, starting at
			//				chunking boundaries
			//
			// Phase II -
			//		Restriction Optimization
			//			Before the actual support determination, identify restriction
			//			predicates (including join predicates) that can be pushed
			//			down in to a supported zone.
			//
			// Phase III -
			//		Cardinality Estimation
			//
			// Phase IV -
			//		Join Order/Rewrite
			//
			// Phase V -
			//		Requested Order

			if (!plan.Messages.HasErrors)
			{
				long startSubTicks = TimingUtility.CurrentTicks;
				//node = ChunkNode(plan, node);
				node = OptimizeNode(plan, node);
				plan.Statistics.OptimizeTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startSubTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
				//startSubTicks = TimingUtility.CurrentTicks;
				//node = Bind(plan, node);
				//plan.Statistics.BindingTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startSubTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			}

			plan.Statistics.PrepareTime = new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			return node;
		}

		//private static PlanNode Chunk(Plan plan, PlanNode planNode)
		//{
		//	return ChunkNode(plan, planNode);
		//}

		#if USEVISIT
		private static PlanNode BindingTraversal(Plan plan, PlanNode planNode, PlanNodeVisitor visitor)
		#else
		private static void BindingTraversal(Plan plan, PlanNode planNode, PlanNodeVisitor visitor)
		#endif
		{
			plan.Symbols.PushFrame();
			try
			{
				#if USEVISIT
				return visitor.Visit(plan, planNode);
				#else
				planNode.BindingTraversal(plan, visitor);
				#endif
			}
			finally
			{
				plan.Symbols.PopFrame();
			}
		}

		public static PlanNode OptimizeNode(Plan plan, PlanNode planNode)
		{
			try
			{
				if (!plan.IsEngine)
				{
					#if USEVISIT
					planNode = BindingTraversal(plan, planNode, new NormalizeRestrictionVisitor());
					#else
					BindingTraversal(plan, planNode, new NormalizeRestrictionVisitor());
					#endif

					// Prepare application transaction join plans
					#if USEVISIT
					planNode = BindingTraversal(plan, planNode, new PrepareJoinApplicationTransactionVisitor());
					#else
					BindingTraversal(plan, planNode, new PrepareJoinApplicationTransactionVisitor());
					#endif

					// Determine potential device support for the entire plan
					// TODO: Add supported operator checking
					planNode.DeterminePotentialDevice(plan);

					// Determine actual device support
					// TODO: Add restriction rewrite
					#if USEVISIT
					planNode = BindingTraversal(plan, planNode, new DetermineDeviceVisitor());
					#else
					BindingTraversal(plan, planNode, new DetermineDeviceVisitor());
					#endif

					// Determine access paths
					#if USEVISIT
					planNode = BindingTraversal(plan, planNode, new DetermineAccessPathVisitor());
					#else
					BindingTraversal(plan, planNode, new DetermineAccessPathVisitor());
					#endif
				}
			}
			catch (Exception exception)
			{
				plan.Messages.Add(exception);
			}

			return planNode;
		}

		//private static PlanNode Optimize(Plan plan, PlanNode planNode)
		//{
		//	// This method is here for consistency with the Bind phase method, and for future expansion.
		//	return OptimizeNode(plan, planNode);
		//}
		
		//private static PlanNode OptimizeNode(Plan plan, PlanNode planNode)
		//{
		//	try
		//	{
		//		if (plan.ShouldEmitIL)
		//			planNode.EmitIL(plan, false);

		//		if (!plan.IsEngine)
		//		{
		//			planNode = BindingTraversal(plan, planNode, new DetermineAccessPathVisitor());
		//		}
		//	}
		//	catch (Exception exception)
		//	{
		//		plan.Messages.Add(exception);
		//	}

		//	return planNode;
		//}
		
		//private static PlanNode Bind(Plan plan, PlanNode planNode)
		//{
		//	return BindNode(plan, planNode);
		//}
		
		//private static PlanNode BindNode(Plan plan, PlanNode planNode)
		//{
		//	try
		//	{
		//		if (!plan.IsEngine)
		//		{
		//			planNode.BindingTraversal(plan, null);
		//		}
		//	}
		//	catch (Exception exception)
		//	{
		//		plan.Messages.Add(exception);
		//	}
		//	
		//	return planNode;
		//}

		//private static PlanNode OptimizeNode(Plan plan, PlanNode planNode)
		//{
		//	planNode = ChunkNode(plan, planNode);
		//	//planNode = OptimizeNode(plan, planNode);
		//	// At this point the binding step is superfluous, the optimize pass should take care of it.
		//	//planNode = BindNode(plan, planNode);
		//	return planNode;
		//}

		#if USESTATEMENTENTRIES		
		public delegate PlanNode CompileStatementCallback(Plan APlan, Statement AStatement);
		
		public static Dictionary<string, CompileStatementCallback> StatementEntries;
		
		static Compiler()
		{
			StatementEntries = new Dictionary<string, CompileStatementCallback>();
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
		
		private static PlanNode InternalCompileStatement(Plan APlan, Statement AStatement)
		{
			CompileStatementCallback LRoutine = StatementEntries[];
			if (StatementEntries.TryGetValue(AStatement.GetType().Name, out LRoutine))
				return LRoutine(APlan, AStatement);
			throw new CompilerException(CompilerException.Codes.UnknownStatementClass, AStatement, AStatement.GetType().FullName);
		}
		#else
		private static PlanNode InternalCompileStatement(Plan plan, Statement statement)
		{
			plan.PushStatement(statement);
			try
			{
				try
				{
					switch (statement.GetType().Name)
					{
						case "Block" : return CompileBlock(plan, statement);
						case "DelimitedBlock" : return CompileDelimitedBlock(plan, statement);
						case "IfStatement" : return CompileIfStatement(plan, statement);
						case "CaseStatement" : return CompileCaseStatement(plan, statement);
						case "WhileStatement" : return CompileWhileStatement(plan, statement);
						case "ForEachStatement" : return CompileForEachStatement(plan, statement);
						case "DoWhileStatement" : return CompileDoWhileStatement(plan, statement);
						case "BreakStatement" : return CompileBreakStatement(plan, statement);
						case "ContinueStatement" : return CompileContinueStatement(plan, statement);
						case "ExitStatement" : return CompileExitStatement(plan, statement);
						case "VariableStatement" : return CompileVariableStatement(plan, statement);
						case "AssignmentStatement" : return CompileAssignmentStatement(plan, statement);
						case "RaiseStatement" : return CompileRaiseStatement(plan, statement);
						case "TryFinallyStatement" : return CompileTryFinallyStatement(plan, statement);
						case "TryExceptStatement" : return CompileTryExceptStatement(plan, statement);
						case "ExpressionStatement" : return CompileExpressionStatement(plan, statement);
						case "SelectStatement" : return CompileExpression(plan, statement);
						case "InsertStatement" : return CompileInsertStatement(plan, statement);
						case "UpdateStatement" : return CompileUpdateStatement(plan, statement);
						case "DeleteStatement" : return CompileDeleteStatement(plan, statement);
						case "CreateTableStatement" : return CompileCreateTableStatement(plan, statement);
						case "CreateViewStatement" : return CompileCreateViewStatement(plan, statement);
						case "CreateScalarTypeStatement" : return CompileCreateScalarTypeStatement(plan, statement);
						case "CreateOperatorStatement" : return CompileCreateOperatorStatement(plan, statement);
						case "CreateAggregateOperatorStatement" : return CompileCreateAggregateOperatorStatement(plan, statement);
						case "CreateConstraintStatement" : return CompileCreateConstraintStatement(plan, statement);
						case "CreateReferenceStatement" : return CompileCreateReferenceStatement(plan, statement);
						case "CreateDeviceStatement" : return CompileCreateDeviceStatement(plan, statement);
						case "CreateServerStatement" : return CompileCreateServerStatement(plan, statement);
						case "CreateSortStatement" : return CompileCreateSortStatement(plan, statement);
						case "CreateConversionStatement" : return CompileCreateConversionStatement(plan, statement);
						case "CreateRoleStatement" : return CompileCreateRoleStatement(plan, statement);
						case "CreateRightStatement" : return CompileCreateRightStatement(plan, statement);
						case "AlterTableStatement" : return CompileAlterTableStatement(plan, statement);
						case "AlterViewStatement" : return CompileAlterViewStatement(plan, statement);
						case "AlterScalarTypeStatement" : return CompileAlterScalarTypeStatement(plan, statement);
						case "AlterOperatorStatement" : return CompileAlterOperatorStatement(plan, statement);
						case "AlterAggregateOperatorStatement" : return CompileAlterAggregateOperatorStatement(plan, statement);
						case "AlterConstraintStatement" : return CompileAlterConstraintStatement(plan, statement);
						case "AlterReferenceStatement" : return CompileAlterReferenceStatement(plan, statement);
						case "AlterDeviceStatement" : return CompileAlterDeviceStatement(plan, statement);
						case "AlterServerStatement" : return CompileAlterServerStatement(plan, statement);
						case "AlterSortStatement" : return CompileAlterSortStatement(plan, statement);
						case "AlterRoleStatement" : return CompileAlterRoleStatement(plan, statement);
						case "DropTableStatement" : return CompileDropTableStatement(plan, statement);
						case "DropViewStatement" : return CompileDropViewStatement(plan, statement);
						case "DropScalarTypeStatement" : return CompileDropScalarTypeStatement(plan, statement);
						case "DropOperatorStatement" : return CompileDropOperatorStatement(plan, statement);
						case "DropConstraintStatement" : return CompileDropConstraintStatement(plan, statement);
						case "DropReferenceStatement" : return CompileDropReferenceStatement(plan, statement);
						case "DropDeviceStatement" : return CompileDropDeviceStatement(plan, statement);
						case "DropServerStatement" : return CompileDropServerStatement(plan, statement);
						case "DropSortStatement" : return CompileDropSortStatement(plan, statement);
						case "DropConversionStatement" : return CompileDropConversionStatement(plan, statement);
						case "DropRoleStatement" : return CompileDropRoleStatement(plan, statement);
						case "DropRightStatement" : return CompileDropRightStatement(plan, statement);
						case "AttachStatement" : return CompileAttachStatement(plan, statement);
						case "InvokeStatement" : return CompileInvokeStatement(plan, statement);
						case "DetachStatement" : return CompileDetachStatement(plan, statement);
						case "GrantStatement" : return CompileSecurityStatement(plan, statement);
						case "RevokeStatement" : return CompileSecurityStatement(plan, statement);
						case "RevertStatement" : return CompileSecurityStatement(plan, statement);
						case "EmptyStatement" : return CompileEmptyStatement(plan, statement);
						default : throw new CompilerException(CompilerException.Codes.UnknownStatementClass, statement, statement.GetType().FullName);
					}
				}
				catch (CompilerException exception)
				{
					if ((exception.Line == -1) && (statement.Line != -1))
					{
						exception.Line = statement.Line;
						exception.LinePos = statement.LinePos;
					}
					if (String.IsNullOrEmpty(exception.Locator) && plan != null && plan.SourceContext != null && plan.SourceContext.Locator != null)
						exception.Locator = plan.SourceContext.Locator.Locator;
					throw;
				}
				catch (Exception exception)
				{
					if (!(exception is DataphorException))
						throw new CompilerException(CompilerException.Codes.InternalError, ErrorSeverity.System, CompilerErrorLevel.NonFatal, statement, exception);
						
					if (!(exception is ILocatorException))
						throw new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.NonFatal, statement, exception, exception.Message)
						{ 
							Locator = 
								(plan != null && plan.SourceContext != null && plan.SourceContext.Locator != null) 
									? plan.SourceContext.Locator.Locator 
									: null
						};
					throw;
				}
			}
			finally
			{
				plan.PopStatement();
			}
		}
		#endif
		
		public static PlanNode CompileStatement(Plan plan, Statement statement)
		{
			if (statement is Expression)
				return CompileExpression(plan, (Expression)statement);
			else
			{
				try
				{
					var result = InternalCompileStatement(plan, statement);
					if (result.LineInfo == null)
					{
						result.Line = statement.Line;
						result.LinePos = statement.LinePos;
					}
					return result;
				}
				catch (Exception exception)
				{
					if (!(exception is CompilerException) || ((((CompilerException)exception).Code != (int)CompilerException.Codes.NonFatalErrors) && (((CompilerException)exception).Code != (int)CompilerException.Codes.FatalErrors)))
						plan.Messages.Add(exception);	
				}
				
				if (plan.Messages.HasFatalErrors)
					throw new CompilerException(CompilerException.Codes.FatalErrors);
				
				return new NoOpNode();
			}
		}
		
		public static PlanNode CompileBlock(Plan plan, Statement statement)
		{																	
			BlockNode node = new BlockNode();
			node.SetLineInfo(plan, statement.LineInfo);
			foreach (Statement localStatement in ((Block)statement).Statements)
				node.Nodes.Add(CompileStatement(plan, localStatement));
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileDelimitedBlock(Plan plan, Statement statement)
		{
			FrameNode frameNode = new FrameNode();
			frameNode.SetLineInfo(plan, statement.LineInfo);
			plan.Symbols.PushFrame();
			try
			{
				DelimitedBlockNode node = new DelimitedBlockNode();
				node.SetLineInfo(plan, statement.LineInfo);
				foreach (Statement localStatement in ((DelimitedBlock)statement).Statements)
					node.Nodes.Add(CompileStatement(plan, localStatement));
				node.DetermineCharacteristics(plan);
				frameNode.Nodes.Add(CompileDeallocateFrameVariablesNode(plan, node));
				return frameNode;
			}
			finally
			{
				plan.Symbols.PopFrame();
			}
		}
		
		protected static PlanNode CompileExpressionStatement(Plan plan, Statement statement)
		{
			ExpressionStatementNode node = new ExpressionStatementNode(CompileExpression(plan, ((ExpressionStatement)statement).Expression, true));
			node.SetLineInfo(plan, statement.LineInfo);
			node.DetermineCharacteristics(plan);
			#if ALLOWSTATEMENTSASEXPRESSIONS
			if ((node.Nodes[0].DataType != null) && !node.Nodes[0].DataType.Equals(APlan.DataTypes.SystemScalar) && node.Nodes[0].IsFunctional && !APlan.SuppressWarnings)
			#else
			if ((node.Nodes[0].DataType != null) && node.Nodes[0].IsFunctional && !plan.SuppressWarnings)
			#endif
				plan.Messages.Add(new CompilerException(CompilerException.Codes.ExpressionStatement, CompilerErrorLevel.Warning, statement));
			return node;
		}
		
		protected static PlanNode CompileIfStatement(Plan plan, Statement statement)
		{
			IfStatement localStatement = (IfStatement)statement;
			PlanNode ifNode = CompileBooleanExpression(plan, localStatement.Expression);
			PlanNode trueNode = CompileFrameNode(plan, localStatement.TrueStatement);
			PlanNode falseNode = null;
			if (localStatement.FalseStatement != null)
				falseNode = CompileFrameNode(plan, localStatement.FalseStatement);
			PlanNode node = EmitIfNode(plan, statement, ifNode, trueNode, falseNode);
			return node;
		}
		
		protected static PlanNode EmitIfNode(Plan plan, Statement statement, PlanNode condition, PlanNode trueStatement, PlanNode falseStatement)
		{
			IfNode node = new IfNode();
			if (statement != null)
				node.SetLineInfo(plan, statement.LineInfo);
			else
				node.IsBreakable = false;
			node.Nodes.Add(condition);
			node.Nodes.Add(trueStatement);
			if (falseStatement != null)
				node.Nodes.Add(falseStatement);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileCaseItemStatement(Plan plan, CaseStatement statement, int index)
		{
			PlanNode whenNode = CompileExpression(plan, statement.CaseItems[index].WhenExpression);
			if (statement.Expression != null)
			{
				PlanNode compareNode = CompileExpression(plan, statement.Expression);
				whenNode = EmitBinaryNode(plan, statement.Expression, compareNode, Instructions.Equal, whenNode);
			}
				
			PlanNode thenNode;
			if (statement.CaseItems[index].ThenStatement.GetType().Name == "Block")
				thenNode = CompileStatement(plan, new DelimitedBlock());	 // An empty statement is not a valid true or false statement
			else
				thenNode = CompileStatement(plan, statement.CaseItems[index].ThenStatement);
			PlanNode elseNode;
			if (index >= statement.CaseItems.Count - 1)
			{
				if (statement.ElseStatement != null)
					if (statement.ElseStatement.GetType().Name == "Block")
						elseNode = CompileStatement(plan, new DelimitedBlock());
					else
						elseNode = CompileStatement(plan, statement.ElseStatement);
				else
					elseNode = null;
			}
			else
				elseNode = CompileCaseItemStatement(plan, statement, index + 1);

			return EmitIfNode(plan, statement.CaseItems[index], whenNode, thenNode, elseNode);
		}
		
		// case [<expression>] when <expression> then <statement> ... else <statement> end;
		// if <expression> then <statement> else <statement>
		// if <expression> = <expression> then <statement> else <statement>
		protected static PlanNode CompileCaseStatement(Plan plan, Statement statement)
		{
			//return CompileCaseItemStatement(APlan, (CaseStatement)AStatement, 0);
			
			CaseStatement localStatement = (CaseStatement)statement;
			if (localStatement.Expression != null)
			{
				SelectedCaseNode node = new SelectedCaseNode();
				node.SetLineInfo(plan, statement.LineInfo);
				
				PlanNode selectorNode = CompileExpression(plan, localStatement.Expression);
				PlanNode equalNode = null;
				plan.Symbols.Push(new Symbol(String.Empty, selectorNode.DataType));
				try
				{
					node.Nodes.Add(selectorNode);
					
					foreach (CaseItemStatement caseItemStatement in localStatement.CaseItems)
					{
						CaseItemNode caseItemNode = new CaseItemNode();
						caseItemNode.SetLineInfo(plan, caseItemStatement.LineInfo);
						PlanNode whenNode = CompileTypedExpression(plan, caseItemStatement.WhenExpression, selectorNode.DataType);
						caseItemNode.Nodes.Add(whenNode);
						plan.Symbols.Push(new Symbol(String.Empty, whenNode.DataType));
						try
						{
							if (equalNode == null)
							{
								equalNode = EmitBinaryNode(plan, new StackReferenceNode(selectorNode.DataType, 1, true), Instructions.Equal, new StackReferenceNode(whenNode.DataType, 0, true));
								node.Nodes.Add(equalNode);
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}

						caseItemNode.Nodes.Add(CompileStatement(plan, caseItemStatement.ThenStatement));
						caseItemNode.DetermineCharacteristics(plan);
						node.Nodes.Add(caseItemNode);
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
				
				if (localStatement.ElseStatement != null)
				{	
					CaseItemNode caseItemNode = new CaseItemNode();
					caseItemNode.SetLineInfo(plan, localStatement.ElseStatement.LineInfo);
					caseItemNode.Nodes.Add(CompileStatement(plan, localStatement.ElseStatement));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
		
				node.DetermineCharacteristics(plan);		
				return node;
			}
			else
			{
				CaseNode node = new CaseNode();
				node.SetLineInfo(plan, statement.LineInfo);
				
				foreach (CaseItemStatement caseItemStatement in localStatement.CaseItems)
				{
					CaseItemNode caseItemNode = new CaseItemNode();
					caseItemNode.SetLineInfo(plan, caseItemStatement.LineInfo);
					caseItemNode.Nodes.Add(CompileBooleanExpression(plan, caseItemStatement.WhenExpression));
					caseItemNode.Nodes.Add(CompileStatement(plan, caseItemStatement.ThenStatement));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
				
				if (localStatement.ElseStatement != null)
				{
					CaseItemNode caseItemNode = new CaseItemNode();
					caseItemNode.SetLineInfo(plan, localStatement.ElseStatement.LineInfo);
					caseItemNode.Nodes.Add(CompileStatement(plan, localStatement.ElseStatement));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
				
				node.DetermineCharacteristics(plan);
				return node;
			}
		}
		
		protected static PlanNode CompileWhileStatement(Plan plan, Statement statement)
		{
			WhileStatement localStatement = (WhileStatement)statement;
			WhileNode node = new WhileNode();
			node.SetLineInfo(plan, statement.LineInfo);
			PlanNode conditionNode = CompileBooleanExpression(plan, localStatement.Condition);
			node.Nodes.Add(conditionNode);
			plan.EnterLoop();
			try
			{
				node.Nodes.Add(CompileFrameNode(plan, localStatement.Statement));
			}
			finally
			{
				plan.ExitLoop();
			}
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileForEachStatement(Plan plan, Statement statement)
		{
			ForEachNode node = new ForEachNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.Statement = (ForEachStatement)statement;
			PlanNode expression = CompileCursorDefinition(plan, node.Statement.Expression);
			plan.EnterLoop();
			try
			{
				node.Nodes.Add(expression);
				
				if (expression.DataType is Schema.ICursorType)
				{
					node.VariableType = ((Schema.ICursorType)expression.DataType).TableType.RowType;
				}
				else if (expression.DataType is Schema.IListType)
				{
					if (node.Statement.VariableName == String.Empty)
						throw new CompilerException(CompilerException.Codes.ForEachVariableNameRequired, node.Statement);
					node.VariableType = ((Schema.IListType)expression.DataType).ElementType;
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidForEachStatement, node.Statement);
					
				if (node.Statement.VariableName == String.Empty)
					plan.EnterRowContext();
				try
				{
					if ((node.Statement.VariableName == String.Empty) || node.Statement.IsAllocation)
					{
						if (node.Statement.VariableName != String.Empty)
						{
							List<string> names = new List<string>();
							if (!plan.Symbols.IsValidVariableIdentifier(node.Statement.VariableName, names))
							{
								#if DISALLOWAMBIGUOUSNAMES
								if (Schema.Object.NamesEqual(names[0], LStatement.VariableName.Identifier))
									if (String.Compare(names[0], LStatement.VariableName.Identifier) == 0)
										throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier);
									else
										throw new CompilerException(CompilerException.Codes.CreatingHiddenIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier, names[0]);
								else
									throw new CompilerException(CompilerException.Codes.CreatingHidingIdentifier, LStatement.VariableName, LStatement.VariableName.Identifier, names[0]);
								#else
								throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, node.Statement, node.Statement.VariableName);
								#endif
							}
						}
						plan.Symbols.Push(new Symbol(node.Statement.VariableName, node.VariableType));
					}
					else
					{
						int columnIndex;
						node.Location = ResolveVariableIdentifier(plan, node.Statement.VariableName, out columnIndex);
						if (node.Location < 0)
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, node.Statement, node.Statement.VariableName);
							
						if (columnIndex >= 0)
							throw new CompilerException(CompilerException.Codes.InvalidColumnReference, node.Statement);
							
						if (!node.VariableType.Is(plan.Symbols.Peek(node.Location).DataType))
							throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, node.Statement, node.VariableType.Name, plan.Symbols.Peek(node.Location).DataType.Name);
					}
					try
					{
						node.Nodes.Add(CompileStatement(plan, node.Statement.Statement));
					}
					finally
					{
						if ((node.Statement.VariableName == String.Empty) || node.Statement.IsAllocation)
							plan.Symbols.Pop();
					}
				}
				finally
				{
					if (node.Statement.VariableName == String.Empty)
						plan.ExitRowContext();
				}
			}
			finally
			{
				plan.ExitLoop();
			}
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileDoWhileStatement(Plan plan, Statement statement)
		{
			DoWhileStatement localStatement = (DoWhileStatement)statement;
			DoWhileNode node = new DoWhileNode();
			node.SetLineInfo(plan, statement.LineInfo);
			PlanNode conditionNode = CompileBooleanExpression(plan, localStatement.Condition);
			plan.EnterLoop();
			try
			{
				node.Nodes.Add(CompileFrameNode(plan, localStatement.Statement));
			}
			finally
			{
				plan.ExitLoop();
			}
			node.Nodes.Add(conditionNode);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileExitStatement(Plan plan, Statement statement)
		{
			ExitNode node = new ExitNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileBreakStatement(Plan plan, Statement statement)
		{
			if (!plan.InLoop)
				throw new CompilerException(CompilerException.Codes.NoLoop, statement);
			BreakNode node = new BreakNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileContinueStatement(Plan plan, Statement statement)
		{
			if (!plan.InLoop)
				throw new CompilerException(CompilerException.Codes.NoLoop, statement);
			ContinueNode node = new ContinueNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileRaiseStatement(Plan plan, Statement statement)
		{
			RaiseStatement localStatement = (RaiseStatement)statement;
			RaiseNode node = new RaiseNode();
			node.SetLineInfo(plan, statement.LineInfo);
			if (localStatement.Expression != null)
			{
				PlanNode planNode = CompileExpression(plan, localStatement.Expression);
				if (!planNode.DataType.Is(plan.DataTypes.SystemError))
					throw new CompilerException(CompilerException.Codes.ErrorExpressionExpected, statement);
				node.Nodes.Add(planNode);
			}
			else
			{
				if (!plan.InErrorContext)
					throw new CompilerException(CompilerException.Codes.InvalidRaiseContext, statement);
			}
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileTryFinallyStatement(Plan plan, Statement statement)
		{
			TryFinallyStatement localStatement = (TryFinallyStatement)statement;
			TryFinallyNode node = new TryFinallyNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.Nodes.Add(CompileFrameNode(plan, localStatement.TryStatement));
			node.Nodes.Add(CompileFrameNode(plan, localStatement.FinallyStatement));
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode CompileTryExceptStatement(Plan plan, Statement statement)
		{
			TryExceptStatement localStatement = (TryExceptStatement)statement;
			TryExceptNode node = new TryExceptNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.Nodes.Add(CompileFrameNode(plan, localStatement.TryStatement));

			plan.EnterErrorContext();
			try
			{
				foreach (GenericErrorHandler handler in localStatement.ErrorHandlers)
				{
					ErrorHandlerNode errorNode = new ErrorHandlerNode();
					errorNode.SetLineInfo(plan, handler.LineInfo);
					if (handler is SpecificErrorHandler)
						errorNode.ErrorType = (Schema.IScalarType)CompileTypeSpecifier(plan, new ScalarTypeSpecifier(((SpecificErrorHandler)handler).ErrorTypeName));
					else
					{
						errorNode.ErrorType = plan.DataTypes.SystemError;
						errorNode.IsGeneric = true;
					}
					plan.AttachDependency((Schema.ScalarType)errorNode.ErrorType);
					plan.Symbols.PushFrame();
					try
					{
						if (handler is ParameterizedErrorHandler)
						{
							errorNode.VariableName = ((ParameterizedErrorHandler)handler).VariableName;
							plan.Symbols.Push(new Symbol(errorNode.VariableName, errorNode.ErrorType));
						}
						errorNode.Nodes.Add(CompileStatement(plan, handler.Statement));
						errorNode.DetermineCharacteristics(plan);
						node.Nodes.Add(CompileDeallocateFrameVariablesNode(plan, errorNode));
					}
					finally
					{
						plan.Symbols.PopFrame();
					}
				}
				node.DetermineCharacteristics(plan);
				return node;
			}
			finally
			{
				plan.ExitErrorContext();
			}
		}
		
		public static PlanNode EmitTableToTableValueNode(Plan plan, TableNode tableNode)
		{
			PlanNode node = new TableToTableValueNode(tableNode);
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EnsureTableValueNode(Plan plan, PlanNode planNode)
		{
			TableNode node = planNode as TableNode;
			if (node != null)
				return EmitTableToTableValueNode(plan, node);
			return planNode;
		}

		public static TableNode EmitTableValueToTableNode(Plan plan, PlanNode planNode)
		{
			TableNode node = new TableValueToTableNode();
			node.Nodes.Add(planNode);
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static TableNode EnsureTableNode(Plan plan, PlanNode planNode)
		{
			TableNode tableNode = planNode as TableNode;
			if (tableNode == null)
				return EmitTableValueToTableNode(plan, planNode);
			return tableNode;
		}
		
		protected static PlanNode CompileVariableStatement(Plan plan, Statement statement)
		{
			VariableStatement localStatement = (VariableStatement)statement;
			List<string> names = new List<string>();
			if (!plan.Symbols.IsValidVariableIdentifier(localStatement.VariableName.Identifier, names))
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(names[0], localStatement.VariableName.Identifier))
					if (String.Compare(names[0], localStatement.VariableName.Identifier) == 0)
						throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, localStatement.VariableName, localStatement.VariableName.Identifier);
					else
						throw new CompilerException(CompilerException.Codes.CreatingHiddenIdentifier, localStatement.VariableName, localStatement.VariableName.Identifier, names[0]);
				else
					throw new CompilerException(CompilerException.Codes.CreatingHidingIdentifier, localStatement.VariableName, localStatement.VariableName.Identifier, names[0]);
				#else
				throw new CompilerException(CompilerException.Codes.CreatingDuplicateIdentifier, localStatement.VariableName, localStatement.VariableName.Identifier);
				#endif
			}

			#if WARNWHENCURSORTYPEVARIABLEINSCOPE			
			if (APlan.Symbols.HasCursorTypeVariables())
				APlan.Messages.Add(new CompilerException(CompilerException.Codes.CursorTypeVariableInScope, CompilerErrorLevel.Warning, localStatement));
			#endif
			
			if (localStatement.TypeSpecifier != null)
			{
				Schema.IDataType variableType = CompileTypeSpecifier(plan, localStatement.TypeSpecifier);
				VariableNode node = new VariableNode();
				node.SetLineInfo(plan, localStatement.LineInfo);
				node.VariableName = localStatement.VariableName.Identifier;
				node.VariableType = variableType;
				if (localStatement.Expression != null)
				{
					plan.Symbols.Push(new Symbol(String.Empty, plan.DataTypes.SystemGeneric));
					try
					{
						node.Nodes.Add(EnsureTableValueNode(plan, CompileTypedExpression(plan, localStatement.Expression, node.VariableType)));
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				node.DetermineCharacteristics(plan);
				plan.Symbols.Push(new Symbol(node.VariableName, node.VariableType));
				return node;
			}
			else if (localStatement.Expression != null)
			{
				PlanNode valueNode;
				plan.Symbols.Push(new Symbol(String.Empty, plan.DataTypes.SystemGeneric));
				try
				{
					valueNode = EnsureTableValueNode(plan, CompileExpression(plan, localStatement.Expression));
				}
				finally
				{
					plan.Symbols.Pop();
				}
				if (valueNode.DataType == null)
					throw new CompilerException(CompilerException.Codes.ExpressionExpected, localStatement.Expression);
					
				VariableNode node = new VariableNode();
				node.SetLineInfo(plan, localStatement.LineInfo);
				node.VariableName = localStatement.VariableName.Identifier;
				node.VariableType = valueNode.DataType.IsNil ? plan.DataTypes.SystemGeneric : valueNode.DataType;
				node.Nodes.Add(valueNode);
				node.DetermineCharacteristics(plan);
				plan.Symbols.Push(new Symbol(node.VariableName, node.VariableType));
				return node;
			}
			else
				throw new CompilerException(CompilerException.Codes.TypeSpecifierExpected, localStatement);
		}
		
		public static PlanNode EmitCatalogIdentiferNode(Plan plan, Statement statement, string identifier)
		{
			return EmitCatalogIdentifierNode(plan, statement, new NameBindingContext(identifier, plan.NameResolutionPath));
		}
		
		public static PlanNode EmitCatalogIdentifierNode(Plan plan, Statement statement, NameBindingContext context)
		{
			ResolveCatalogIdentifier(plan, context);
			if (context.Object is Schema.TableVar)
			{
				plan.AttachDependency(context.Object);
				return EmitTableVarNode(plan, statement, context.Identifier, (Schema.TableVar)context.Object);
			}
			return null;
		}
		
		public static PlanNode EmitIdentifierNode(Plan plan, string identifier)
		{
			return EmitIdentifierNode(plan, new EmptyStatement(), identifier);
		}
		
		public static PlanNode EmitIdentifierNode(Plan plan, Statement statement, string identifier)
		{
			NameBindingContext context = new NameBindingContext(identifier, plan.NameResolutionPath);
			PlanNode node = EmitIdentifierNode(plan, statement, context);
			if (node == null)
				if (context.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, plan.CurrentStatement(), identifier, ExceptionUtility.StringsToCommaList(context.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, plan.CurrentStatement(), identifier);
			return node;
		}
		
		public static PlanNode EmitIdentifierNode(Plan plan, NameBindingContext context)
		{
			return EmitIdentifierNode(plan, new EmptyStatement(), context);
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
		public static PlanNode EmitIdentifierNode(Plan plan, Statement statement, NameBindingContext context)
		{
			if ((context.BindingFlags & NameBindingFlags.Local) != 0)
			{
				int columnIndex;
				int index = ResolveVariableIdentifier(plan, context.Identifier, out columnIndex, context.Names);
				if (index >= 0)
				{
					if (columnIndex >= 0)
					{
						Schema.IRowType rowType = (Schema.IRowType)plan.Symbols[index].DataType;
						#if USECOLUMNLOCATIONBINDING
						return new StackColumnReferenceNode(rowType.Columns[columnIndex].Name, rowType.Columns[columnIndex].DataType, index, columnIndex);
						#else
						return new StackColumnReferenceNode(Schema.Object.IsRooted(context.Identifier) ? context.Identifier : Schema.Object.EnsureRooted(rowType.Columns[columnIndex].Name), rowType.Columns[columnIndex].DataType, index);
						#endif
					}
						
					return new StackReferenceNode(Schema.Object.IsRooted(context.Identifier) ? context.Identifier : Schema.Object.EnsureRooted(plan.Symbols[index].Name), plan.Symbols[index].DataType, index);
				}
			}
			
			if ((context.BindingFlags & NameBindingFlags.Global) != 0)
			{
				if (context.Names.Count == 0)
				{
					PlanNode node = EmitCatalogIdentifierNode(plan, statement, context);
					if (node != null)
						return node;
				}
				
				if (context.Names.Count == 0)
				{
					// If the identifier is unresolved, and there is a default device, attempt an automatic reconciliation				
					if ((plan.DefaultDeviceName != String.Empty) && (!plan.InLoadingContext()))
					{
						Schema.Device device = GetDefaultDevice(plan, false);
						if (device != null)
						{
							Schema.BaseTableVar tableVar = new Schema.BaseTableVar(context.Identifier, new Schema.TableType(), device);
							plan.CheckDeviceReconcile(tableVar);
							return EmitCatalogIdentifierNode(plan, statement, context);
						}
					}
				}
			}

			// If the identifier could not be resolved, return a null reference
			return null;
		}
		
		public static PlanNode EmitTableVarNode(Plan plan, Schema.TableVar tableVar)
		{
			return EmitTableVarNode(plan, new EmptyStatement(), tableVar.Name, tableVar);
		}
		
		public static PlanNode EmitTableVarNode(Plan plan, string identifier, Schema.TableVar tableVar)
		{
			return EmitTableVarNode(plan, new EmptyStatement(), identifier, tableVar);
		}
		
		public static PlanNode EmitTableVarNode(Plan plan, Statement statement, string identifier, Schema.TableVar tableVar)
		{
			plan.SetIsLiteral(false);
			if (tableVar is Schema.BaseTableVar)
				return EmitBaseTableVarNode(plan, statement, identifier, (Schema.BaseTableVar)tableVar);
			else
				return EmitDerivedTableVarNode(plan, statement, (Schema.DerivedTableVar)tableVar);
		}
		
		public static int ResolveVariableIdentifier(Plan plan, string identifier, out int columnIndex)
		{
			List<string> names = new List<string>();
			int index = ResolveVariableIdentifier(plan, identifier, out columnIndex, names);
			if (index < 0)
				if (names.Count > 0)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, plan.CurrentStatement(), identifier, ExceptionUtility.StringsToCommaList(names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, plan.CurrentStatement(), identifier);
			return index;
		}
		
		/// <summary> Returns the index of a data object on the stack, -1 if unable to resolve. </summary>
		/// <param name="columnIndex"> If the variable resolves to a column reference, AColumnIndex will contain the column index, -1 otherwise </param>
		public static int ResolveVariableIdentifier(Plan plan, string identifier, out int columnIndex, List<string> names)
		{
			return plan.Symbols.ResolveVariableIdentifier(identifier, out columnIndex, names);
		}
		
		public static Schema.Object ResolveCatalogObjectSpecifier(Plan plan, string specifier)
		{
			return ResolveCatalogObjectSpecifier(plan, specifier, true);
		}
		
		public static Schema.Object ResolveCatalogObjectSpecifier(Plan plan, string specifier, bool mustResolve)
		{
			CatalogObjectSpecifier localSpecifier = new Parser().ParseCatalogObjectSpecifier(specifier);
			if (!localSpecifier.IsOperator)
				return Compiler.ResolveCatalogIdentifier(plan, localSpecifier.ObjectName, mustResolve);
			else
				return Compiler.ResolveOperatorSpecifier(plan, new OperatorSpecifier(localSpecifier.ObjectName, localSpecifier.FormalParameterSpecifiers), mustResolve);
		}
																				
		public static Schema.Object ResolveCatalogIdentifier(Plan plan, string identifier)
		{
			return ResolveCatalogIdentifier(plan, identifier, true);
		}

		public static Schema.Object ResolveCatalogIdentifier(Plan plan, string identifier, bool mustResolve)
		{
			//long LStartTicks = TimingUtility.CurrentTicks;
			NameBindingContext context = new NameBindingContext(identifier, plan.NameResolutionPath);
			ResolveCatalogIdentifier(plan, context);
			
			if (mustResolve && (context.Object == null))
				if (context.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, plan.CurrentStatement(), identifier, ExceptionUtility.StringsToCommaList(context.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, plan.CurrentStatement(), identifier);
			//APlan.Accumulator += (TimingUtility.CurrentTicks - LStartTicks);
			return context.Object;
		}
		
		public static Schema.Object ResolveCatalogIdentifier(Plan plan, IdentifierExpression expression)
		{
			//long LStartTicks = TimingUtility.CurrentTicks;
			NameBindingContext context = new NameBindingContext(expression.Identifier, plan.NameResolutionPath);
			ResolveCatalogIdentifier(plan, context);
			
			if (context.Object == null)
				if (context.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, expression, expression.Identifier, ExceptionUtility.StringsToCommaList(context.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, expression, expression.Identifier);
			//APlan.Accumulator += (TimingUtility.CurrentTicks - LStartTicks);
			return context.Object;
		}
		
		/// <summary>Attempts to resolve the given name binding context.</summary>
		/// <remarks>
		/// This is the primary catalog identifier resolution procedure.  This procedure will not throw an error if it is unable to
		/// resolve the identifier.  If the identifier is ambiguous, the IsAmbiguous flag will be set in the NameBindingContext.
		/// </remarks>
		public static void ResolveCatalogIdentifier(Plan plan, NameBindingContext context)
		{
			try
			{
				// if this process is part of an application transaction, search for an application transaction variable named AIdentifier
				if ((plan.ApplicationTransactionID != Guid.Empty) && (!plan.InLoadingContext()))
				{
					ApplicationTransaction transaction = plan.GetApplicationTransaction();
					try
					{
						if (!transaction.IsGlobalContext && !transaction.IsLookup)
						{
							int index = transaction.TableMaps.ResolveName(context.Identifier, plan.NameResolutionPath, context.Names);
							if (context.IsAmbiguous)
								return;
								
							if (index >= 0)
							{
								NameBindingContext localContext = new NameBindingContext(Schema.Object.EnsureRooted(transaction.TableMaps[index].TableVar.Name), plan.NameResolutionPath);
								ResolveCatalogIdentifier(plan, localContext);
								if (localContext.Object != null)
								{
									context.SetBindingDataFromContext(localContext);
									return;
								}
							}
						}
					}
					finally
					{
						Monitor.Exit(transaction);
					}
				}
				
				// search for a session table variable named AIdentifier
				lock (plan.PlanSessionObjects)
				{
					int index = plan.PlanSessionObjects.ResolveName(context.Identifier, plan.NameResolutionPath, context.Names);
					if (context.IsAmbiguous)
						return;
						
					if (index >= 0)
					{
						NameBindingContext localContext = new NameBindingContext(Schema.Object.EnsureRooted(((Schema.SessionObject)plan.PlanSessionObjects[index]).GlobalName), plan.NameResolutionPath);
						ResolveCatalogIdentifier(plan, localContext);
						if (localContext.IsAmbiguous)
							return;
							
						if (localContext.Object != null)
						{
							context.SetBindingDataFromContext(localContext);
							return;
						}
					}
				}
				
				lock (plan.SessionObjects)
				{
					int index = plan.SessionObjects.ResolveName(context.Identifier, plan.NameResolutionPath, context.Names);
					if (index >= 0)
					{
						NameBindingContext localContext = new NameBindingContext(Schema.Object.EnsureRooted(((Schema.SessionObject)plan.SessionObjects[index]).GlobalName), plan.NameResolutionPath);
						ResolveCatalogIdentifier(plan, localContext);
						if (localContext.IsAmbiguous)
							return;
							
						if (localContext.Object != null)
						{
							context.SetBindingDataFromContext(localContext);
							return;
						}
					}
				}
							
				lock (plan.PlanCatalog)
				{				
					int index = plan.PlanCatalog.ResolveName(context.Identifier, plan.NameResolutionPath, context.Names);
					if (context.IsAmbiguous)
						return;
						
					if (index >= 0)
					{
						context.Object = plan.PlanCatalog[index];
						//APlan.AcquireCatalogLock(AContext.Object, LockMode.Shared);
						return;
					}
				}

				lock (plan.Catalog)
				{
					context.Object = plan.CatalogDeviceSession.ResolveName(context.Identifier, plan.NameResolutionPath, context.Names);
					//int LIndex = APlan.Catalog.ResolveName(AContext.Identifier, APlan.NameResolutionPath, AContext.Names);
					if (context.IsAmbiguous)
						return;
						
					//if (LIndex >= 0)
					if (context.Object != null)
					{
						//AContext.Object = APlan.Catalog[LIndex];
						//APlan.AcquireCatalogLock(AContext.Object, LockMode.Shared);
						return;
					}
				}
			}
			finally
			{
				// Reinfer view references
				Schema.DerivedTableVar derivedTableVar = context.Object as Schema.DerivedTableVar;
				if ((derivedTableVar != null) && derivedTableVar.ShouldReinferReferences)
					ReinferViewReferences(plan, derivedTableVar);

				// AT Variable Enlistment
				Schema.TableVar tableVar = context.Object as Schema.TableVar;
				if ((tableVar != null) && (plan.ApplicationTransactionID != Guid.Empty) && (!plan.InLoadingContext()))
				{
					ApplicationTransaction transaction = plan.GetApplicationTransaction();
					try
					{
						if (!tableVar.IsATObject)
						{
							if (tableVar.ShouldTranslate && !transaction.IsGlobalContext && !transaction.IsLookup)
								context.Object = transaction.AddTableVar(plan.ServerProcess, tableVar);
						}
						else if (!plan.ServerProcess.InAddingTableVar) // This check prevents EnsureTableVar from being called while the map is still being created
							transaction.EnsureATTableVarMapped(plan.ServerProcess, tableVar);
					}
					finally
					{
						Monitor.Exit(transaction);
					}
				}
			}
		}
		
		protected static Schema.Property ResolvePropertyReference(Plan plan, ref Schema.ScalarType scalarType, string identifier, out int representationIndex, out int propertyIndex)
		{
			Schema.Representation representation;
			for (int localRepresentationIndex = 0; localRepresentationIndex < scalarType.Representations.Count; localRepresentationIndex++)
			{
				representation = scalarType.Representations[localRepresentationIndex];
				int localPropertyIndex = representation.Properties.IndexOf(identifier);
				if (localPropertyIndex >= 0)
				{
					representationIndex = localRepresentationIndex;
					propertyIndex = localPropertyIndex;
					return representation.Properties[localPropertyIndex];
				}
			}

			Schema.Property property;
			for (int index = 0; index < scalarType.ParentTypes.Count; index++)
			{
				Schema.ScalarType parentType = (Schema.ScalarType)scalarType.ParentTypes[index];
				property = ResolvePropertyReference(plan, ref parentType, identifier, out representationIndex, out propertyIndex);
				if (property != null)
				{
					scalarType = parentType;
					return property;
				}
			}			

			representationIndex = -1;
			propertyIndex = -1;			
			return null;
		}
		
		protected static PlanNode CompilePropertyReference(Plan plan, PlanNode planNode, IdentifierExpression expression)
		{
			int propertyIndex;
			int representationIndex;
			Schema.Property property;
			Schema.ScalarType scalarType = (Schema.ScalarType)planNode.DataType;
			property = ResolvePropertyReference(plan, ref scalarType, expression.Identifier, out representationIndex, out propertyIndex);
			if (property != null)
			{
				if (!plan.IsAssignmentTarget)
					return EmitPropertyReadNode(plan, planNode, scalarType, property);
				else
				{
					PropertyReferenceNode node = new PropertyReferenceNode();
					node.ScalarType = scalarType;
					node.DataType = property.DataType;
					node.RepresentationIndex = representationIndex;
					node.PropertyIndex = propertyIndex;
					node.Nodes.Add(planNode);
					node.DetermineCharacteristics(plan);
					return node;
				}
			}
			
			return null;
		}
		
		public static PlanNode EmitPropertyReadNode(Plan plan, PlanNode planNode, Schema.ScalarType scalarType, Schema.Property property)
		{
			property.ResolveReadAccessor(plan.CatalogDeviceSession);
			if (property.ReadAccessor == null)
			{
				if (scalarType.IsClassType && property.Representation.IsDefaultRepresentation)
				{
					PlanNode node = plan.CreateObject(DefaultObjectPropertyReadNode(property.Name), null) as PlanNode;
					node.Nodes.Add(planNode);
					node.DataType = property.DataType;
					node.DetermineCharacteristics(plan);
					return node;
				}
				else
					throw new CompilerException(CompilerException.Codes.DefaultReadAccessorCannotBeProvided, property.Name, property.Representation.Name, scalarType.Name);
			}
			else
			{
				PlanNode node = BuildCallNode(plan, null, property.ReadAccessor, new PlanNode[]{ planNode });
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return node;
			}
		}
		
		protected static PlanNode EmitPropertyReadNode(Plan plan, PropertyReferenceNode node)
		{
			Schema.Property property = node.ScalarType.Representations[node.RepresentationIndex].Properties[node.PropertyIndex];
			return EmitPropertyReadNode(plan, node.Nodes[0], node.ScalarType, property);
		}
		
		protected static PlanNode CompileDotInvocation(Plan plan, PlanNode planNode, CallExpression expression)
		{
			PlanNode[] nodes = new PlanNode[expression.Expressions.Count + 1];
			nodes[0] = planNode;
			CallExpression bindingExpression = new CallExpression();
			bindingExpression.Line = expression.Line;
			bindingExpression.LinePos = expression.LinePos;
			bindingExpression.Identifier = expression.Identifier;
			bindingExpression.Modifiers = expression.Modifiers;
			ValueExpression thisExpression = new ValueExpression("");
			thisExpression.Line = expression.Line;
			thisExpression.LinePos = expression.LinePos;
			bindingExpression.Expressions.Add(thisExpression);
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				nodes[index + 1] = CompileExpression(plan, expression.Expressions[index]);
				bindingExpression.Expressions.Add(expression.Expressions[index]);
			}
			OperatorBindingContext context = new OperatorBindingContext(bindingExpression, expression.Identifier, plan.NameResolutionPath, SignatureFromArguments(nodes), false);
			PlanNode localPlanNode = EmitCallNode(plan, context, nodes);
			if (localPlanNode == null)
			{
				nodes[0] = EmitParameterNode(plan, Modifier.Var, nodes[0]);
				OperatorBindingContext subContext = new OperatorBindingContext(bindingExpression, expression.Identifier, plan.NameResolutionPath, SignatureFromArguments(nodes), false);
				localPlanNode = EmitCallNode(plan, subContext, nodes);
				if (localPlanNode == null)
					CheckOperatorResolution(plan, context); // Throw the error based on the original signature
			}
			return localPlanNode;
		}
		
		protected static PlanNode CompileDotOperator(Plan plan, PlanNode planNode, Expression expression)
		{
			if ((planNode.DataType is Schema.IScalarType) && (expression is IdentifierExpression))
			{
				return CompilePropertyReference(plan, planNode, (IdentifierExpression)expression);
			}
			else if ((planNode.DataType is Schema.IRowType) && (expression is IdentifierExpression))
			{
				ColumnExpression columnExpression = new ColumnExpression(Schema.Object.EnsureRooted(((IdentifierExpression)expression).Identifier));
				int index = ((Schema.IRowType)planNode.DataType).Columns.IndexOfName(columnExpression.ColumnName);
				if (index >= 0)
				{
					columnExpression.Line = expression.Line;
					columnExpression.LinePos = expression.LinePos;
					return EmitColumnExtractorNode(plan, expression, columnExpression, planNode);
				}
				else
					return null;
			}
			#if ALLOWIMPLICITROWEXTRACTOR
			else if ((APlanNode.DataType is Schema.ITableType) && (AExpression is IdentifierExpression))
			{
				ColumnExpression columnExpression = new ColumnExpression(Schema.Object.EnsureRooted(((IdentifierExpression)AExpression).Identifier));
				int index = ((Schema.ITableType)APlanNode.DataType).Columns.IndexOfName(columnExpression.ColumnName);
				if (index >= 0)
				{
					columnExpression.Line = AExpression.Line;
					columnExpression.LinePos = AExpression.LinePos;
					return EmitColumnExtractorNode(APlan, AExpression, columnExpression, EmitRowExtractorNode(APlan, AExpression, APlanNode));
				}
				else
					return null;
			}
			#endif
			else if (expression is CallExpression)
			{
				return CompileDotInvocation(plan, planNode, (CallExpression)expression);
			}
			else if (expression is QualifierExpression)
			{
				QualifierExpression qualifierExpression = (QualifierExpression)expression;
				PlanNode node = CompileDotOperator(plan, planNode, qualifierExpression.LeftExpression);
				if (node == null) 
					if (qualifierExpression.LeftExpression is IdentifierExpression)
						return CompileDotOperator(plan, planNode, CollapseQualifierExpression(plan, (IdentifierExpression)qualifierExpression.LeftExpression, qualifierExpression.RightExpression));
					else
						return null;
				else
					return CompileDotOperator(plan, node, qualifierExpression.RightExpression);
			}
			else
				return null;
		}
		
		protected static Expression CollapseQualifierExpression(Plan plan, IdentifierExpression identifierExpression, Expression expression)
		{
			if (expression is IdentifierExpression)
			{
				IdentifierExpression localExpression = new IdentifierExpression();
				localExpression.Line = identifierExpression.Line;
				localExpression.LinePos = identifierExpression.LinePos;
				localExpression.Modifiers = expression.Modifiers;
				localExpression.Identifier = Schema.Object.Qualify(((IdentifierExpression)expression).Identifier, identifierExpression.Identifier);
				return localExpression;
			}
			else if (expression is CallExpression)
			{
				CallExpression callExpression = (CallExpression)expression;
				CallExpression localExpression = new CallExpression();
				localExpression.Line = identifierExpression.Line;
				localExpression.LinePos = identifierExpression.LinePos;
				localExpression.Modifiers = expression.Modifiers;
				localExpression.Identifier = Schema.Object.Qualify(callExpression.Identifier, identifierExpression.Identifier);
				localExpression.Expressions.AddRange(callExpression.Expressions);
				return localExpression;
			}
			else if ((expression is ColumnExtractorExpression) && (((ColumnExtractorExpression)expression).Columns.Count == 1))
			{
				ColumnExtractorExpression columnExtractorExpression = (ColumnExtractorExpression)expression;
				ColumnExtractorExpression localExpression = new ColumnExtractorExpression();
				localExpression.Line = columnExtractorExpression.Line;
				localExpression.LinePos = columnExtractorExpression.LinePos;
				localExpression.Modifiers = expression.Modifiers;
				localExpression.Columns.Add(new ColumnExpression(Schema.Object.Qualify(columnExtractorExpression.Columns[0].ColumnName, identifierExpression.Identifier)));
				localExpression.Expression = columnExtractorExpression.Expression;
				return localExpression;
			}
			else if (expression is QualifierExpression)
			{
				QualifierExpression qualifierExpression = (QualifierExpression)expression;
				QualifierExpression localExpression = new QualifierExpression();
				localExpression.Line = qualifierExpression.Line;
				localExpression.LinePos = qualifierExpression.LinePos;
				localExpression.Modifiers = expression.Modifiers;
				localExpression.LeftExpression = CollapseQualifierExpression(plan, identifierExpression, qualifierExpression.LeftExpression);
				localExpression.RightExpression = qualifierExpression.RightExpression;
				return localExpression;
			}
			else
				throw new CompilerException(CompilerException.Codes.UnableToCollapseQualifierExpression, expression);
		}
		
		protected static Expression CollapseColumnExtractorExpression(Expression expression)
		{
			// if the right side of the qualifier expression is a column extractor, the left side must be an identifier
			QualifierExpression qualifierExpression = expression as QualifierExpression;
			if (qualifierExpression != null)
			{
				Expression rightExpression = CollapseColumnExtractorExpression(qualifierExpression.RightExpression);
				ColumnExtractorExpression columnExtractorExpression = rightExpression as ColumnExtractorExpression;
				IdentifierExpression identifierExpression = qualifierExpression.LeftExpression as IdentifierExpression;
				if ((identifierExpression != null) && (columnExtractorExpression != null))
				{
					columnExtractorExpression.Columns[0].ColumnName = Schema.Object.Qualify(columnExtractorExpression.Columns[0].ColumnName, identifierExpression.Identifier);
					return columnExtractorExpression;
				}
			}
			
			return expression;
		}
		
		protected static PlanNode CompileQualifierExpression(Plan plan, QualifierExpression expression)
		{
			return CompileQualifierExpression(plan, expression, false);
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
		protected static PlanNode CompileQualifierExpression(Plan plan, QualifierExpression expression, bool isStatementContext)
		{
			PlanNode node;
			PlanNode leftNode;

			if (expression.RightExpression.Modifiers == null)
				expression.RightExpression.Modifiers = expression.Modifiers;
				
			Schema.Object currentCreationObject = plan.CurrentCreationObject();
			Schema.Object dummyCreationObject = null;
			if (currentCreationObject != null)
			{
				Schema.CatalogObject currentCatalogCreationObject = currentCreationObject as Schema.CatalogObject;
				if (currentCatalogCreationObject != null)
				{
					if (currentCatalogCreationObject is Schema.Operator)
					{
						Schema.Operator dummyOperator = new Schema.Operator(currentCreationObject.Name);
						dummyOperator.SessionObjectName = currentCatalogCreationObject.SessionObjectName;
						dummyOperator.SourceOperatorName = ((Schema.Operator)currentCreationObject).SourceOperatorName;
						dummyOperator.Library = currentCreationObject.Library;
						dummyOperator.IsGenerated = currentCreationObject.IsGenerated;
						dummyCreationObject = dummyOperator;
					}
					else
					{
						Schema.BaseTableVar dummyCatalogCreationObject = new Schema.BaseTableVar(currentCreationObject.Name);
						dummyCatalogCreationObject.SessionObjectName = currentCatalogCreationObject.SessionObjectName;
						if (currentCreationObject is Schema.TableVar)
							dummyCatalogCreationObject.SourceTableName = ((Schema.TableVar)currentCreationObject).SourceTableName;
						dummyCatalogCreationObject.Library = currentCreationObject.Library;
						dummyCatalogCreationObject.IsGenerated = currentCreationObject.IsGenerated;
						dummyCreationObject = dummyCatalogCreationObject;
					}
				}
				else
				{
					dummyCreationObject = new Schema.TableVarColumn(new Schema.Column(currentCreationObject.Name, plan.DataTypes.SystemScalar));
				}
			}
			bool objectPopped = false;
			if (dummyCreationObject != null)
				plan.PushCreationObject(dummyCreationObject, new LineInfo(plan.CompilingOffset)); // Push a dummy object to track dependencies hit for the left side of the expression
			try
			{
				if (expression.LeftExpression is IdentifierExpression)
				{
					leftNode = EmitIdentifierNode(plan, expression.LeftExpression, new NameBindingContext(((IdentifierExpression)expression.LeftExpression).Identifier, plan.NameResolutionPath, NameBindingFlags.Local));
					if (leftNode == null)
					{
						QualifierExpression localExpression = expression;
						while ((leftNode == null) && ((localExpression.RightExpression is IdentifierExpression) || ((localExpression.RightExpression is QualifierExpression) && (((QualifierExpression)localExpression.RightExpression).LeftExpression is IdentifierExpression))))
						{
							Expression collapsedExpression = CollapseQualifierExpression(plan, (IdentifierExpression)localExpression.LeftExpression, localExpression.RightExpression);
							IdentifierExpression collapsedIdentifier = collapsedExpression as IdentifierExpression;
							if (collapsedIdentifier != null)
							{
								leftNode = EmitIdentifierNode(plan, collapsedIdentifier, new NameBindingContext(collapsedIdentifier.Identifier, plan.NameResolutionPath, NameBindingFlags.Local));
								if (leftNode != null)
								{
									if ((dummyCreationObject != null) && dummyCreationObject.HasDependencies())
										plan.AttachDependencies(dummyCreationObject.Dependencies);
									return leftNode;
								}
								else
									break;
							}
							
							localExpression = (QualifierExpression)collapsedExpression;
							collapsedIdentifier = localExpression.LeftExpression as IdentifierExpression;
							if (collapsedIdentifier != null)
							{
								leftNode = EmitIdentifierNode(plan, collapsedIdentifier, new NameBindingContext(collapsedIdentifier.Identifier, plan.NameResolutionPath, NameBindingFlags.Local));
								if (leftNode != null)
									expression = localExpression;
							}
						}
						
						if (leftNode == null)
						{
							leftNode = EmitIdentifierNode(plan, expression.LeftExpression, new NameBindingContext(((IdentifierExpression)expression.LeftExpression).Identifier, plan.NameResolutionPath, NameBindingFlags.Global));
							if (leftNode == null)
							{
								if (dummyCreationObject != null)
								{
									plan.PopCreationObject();
									objectPopped = true;
								}
								return CompileExpression(plan, CollapseQualifierExpression(plan, (IdentifierExpression)expression.LeftExpression, expression.RightExpression), isStatementContext);
							}
						}
					}
				}
				else
					leftNode = CompileExpression(plan, expression.LeftExpression, isStatementContext);
					
				node = CompileDotOperator(plan, leftNode, expression.RightExpression);
			}
			finally
			{
				if ((dummyCreationObject != null) && !objectPopped)
					plan.PopCreationObject();
			}

			if (node == null) 
				if (expression.LeftExpression is IdentifierExpression)
					return CompileExpression(plan, CollapseQualifierExpression(plan, (IdentifierExpression)expression.LeftExpression, expression.RightExpression), isStatementContext);
				else
					throw new CompilerException(CompilerException.Codes.InvalidQualifier, expression, new D4TextEmitter().Emit(expression.RightExpression));
			else
			{
				if (dummyCreationObject != null) 
				{
					if (dummyCreationObject.HasDependencies())
						plan.AttachDependencies(dummyCreationObject.Dependencies);
					Schema.Operator dummyCreationOperator = dummyCreationObject as Schema.Operator;
					if (dummyCreationOperator != null)
					{
						plan.SetIsLiteral(dummyCreationOperator.IsLiteral);
						plan.SetIsFunctional(dummyCreationOperator.IsFunctional);
						plan.SetIsDeterministic(dummyCreationOperator.IsDeterministic);
						plan.SetIsRepeatable(dummyCreationOperator.IsRepeatable);
						plan.SetIsNilable(dummyCreationOperator.IsNilable);
					}
				}
				
				return node;									 
			}
		}
		
		// V := <expression> ::=
		// var LV := <expression>;
		// delete V;
		// insert LV into V;

		protected static PlanNode CopyPlanNode(Plan plan, PlanNode node, bool isAssignment)
		{
			// Emit the node as a statement, then return the compile of that statement.
			if (isAssignment)
			{
				plan.PushStatementContext(new StatementContext(StatementType.Assignment));
			}
			try
			{
				var statement = node.EmitStatement(EmitMode.ForCopy);
				var nodeCopy = Compiler.CompileStatement(plan, statement);
				return nodeCopy;
			}
			finally
			{
				if (isAssignment)
				{
					plan.PopStatementContext();
				}
			}
		}
		
		protected static PlanNode EmitTableAssignmentNode(Plan plan, Statement statement, PlanNode sourceNode, TableNode targetNode)
		{
			if (!(sourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, statement, sourceNode.DataType.Name, targetNode.DataType.Name);

			TableNode targetNodeCopy = (TableNode)CopyPlanNode(plan, targetNode, true);
				
			//Schema.BaseTableVar LTempTableVar;
			FrameNode frameNode = new FrameNode();
			frameNode.SetLineInfo(plan, statement.LineInfo);
			BlockNode blockNode = new BlockNode();
			blockNode.SetLineInfo(plan, statement.LineInfo);
			frameNode.Nodes.Add(blockNode);
			plan.Symbols.PushFrame();
			try
			{
				VariableNode node = new VariableNode(Schema.Object.NameFromGuid(Guid.NewGuid()), sourceNode.DataType);
				node.SetLineInfo(plan, statement.LineInfo);
				node.Nodes.Add(EnsureTableValueNode(plan, sourceNode));
				node.DetermineCharacteristics(plan);
				blockNode.Nodes.Add(node);
				plan.Symbols.Push(new Symbol(node.VariableName, node.VariableType));
				
				// Delete the data from the target variable
				DeleteNode deleteNode = new DeleteNode();
				deleteNode.SetLineInfo(plan, statement.LineInfo);
				deleteNode.Nodes.Add(targetNode);
				deleteNode.DetermineDataType(plan);
				deleteNode.DetermineCharacteristics(plan);
				blockNode.Nodes.Add(deleteNode);
				
				// Insert the data from the temporary variable into the target variable
				InsertNode insertNode = new InsertNode();
				insertNode.SetLineInfo(plan, statement.LineInfo);
				insertNode.Nodes.Add(EnsureTableNode(plan, EmitIdentifierNode(plan, node.VariableName)));
				insertNode.Nodes.Add(EmitInsertConditionNode(plan, targetNodeCopy));
				insertNode.DetermineDataType(plan);
				insertNode.DetermineCharacteristics(plan);
				blockNode.Nodes.Add(insertNode);
				
				blockNode.DetermineCharacteristics(plan);
				frameNode.DetermineCharacteristics(plan);
				return frameNode;
			}
			finally
			{
				plan.Symbols.PopFrame();
			}
		}
		
		protected static PlanNode EmitStackReferenceAssignmentNode(Plan plan, Statement statement, PlanNode sourceNode, StackReferenceNode targetNode)
		{
			if (!sourceNode.DataType.Is(targetNode.DataType))
			{
				ConversionContext context = FindConversionPath(plan, sourceNode.DataType, targetNode.DataType);
				CheckConversionContext(plan, context);
				sourceNode = ConvertNode(plan, sourceNode, context);
			}
			
			if (sourceNode is TableNode)
				sourceNode = EmitTableToTableValueNode(plan, (TableNode)sourceNode);

			AssignmentNode node = new AssignmentNode(targetNode, Upcast(plan, sourceNode, targetNode.DataType));
			if (statement != null)
				node.SetLineInfo(plan, statement.LineInfo);
			else
				node.IsBreakable = false;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		protected static PlanNode EmitColumnExtractorAssignmentNode(Plan plan, Statement statement, PlanNode sourceNode, ExtractColumnNode targetNode)
		{
			// If the column extractor's source is an indexer, we need to get to the underlying table node, or the update will compile as a row variable update, i/o a restricted table update
			PlanNode localTargetNode = targetNode.Nodes[0];
			string identifier = targetNode.Identifier;
			RowRenameNode rowRenameNode = localTargetNode as RowRenameNode;
			if ((rowRenameNode != null) && !rowRenameNode.ShouldEmit)
			{
				// If there is a row rename that is part of a table indexer, undo the rename
				int columnIndex = rowRenameNode.DataType.Columns.IndexOfName(identifier);
				localTargetNode = localTargetNode.Nodes[0];
				identifier = Schema.Object. EnsureRooted(((Schema.RowType)localTargetNode.DataType).Columns[columnIndex].Name);
			}
			if (localTargetNode is ExtractRowNode)
				localTargetNode = localTargetNode.Nodes[0];

			DelimitedBlockNode blockNode = new DelimitedBlockNode();
			blockNode.SetLineInfo(plan, statement.LineInfo);
			VariableNode variableNode = new VariableNode(Schema.Object.GetUniqueName(), sourceNode.DataType);
			variableNode.SetLineInfo(plan, statement.LineInfo);
			variableNode.Nodes.Add(sourceNode);
			variableNode.DetermineCharacteristics(plan);
			blockNode.Nodes.Add(variableNode);
			plan.Symbols.Push(new Symbol(variableNode.VariableName, variableNode.VariableType));

			UpdateStatement localStatement = new UpdateStatement((Expression)localTargetNode.EmitStatement(EmitMode.ForCopy), new UpdateColumnExpression[]{new UpdateColumnExpression(new IdentifierExpression(identifier), new IdentifierExpression(variableNode.VariableName))});
			localStatement.Line = statement.Line;
			localStatement.LinePos = statement.LinePos;
			blockNode.Nodes.Add(CompileUpdateStatement(plan, localStatement));
			return blockNode;
		}
		
		public static PlanNode EmitPropertyWriteNode(Plan plan, Statement statement, Schema.Property property, PlanNode sourceNode, PlanNode targetNode)
		{
			property.ResolveWriteAccessor(plan.CatalogDeviceSession);
			PlanNode node = null;
			if (property.WriteAccessor == null)
			{
				if (property.Representation.ScalarType.IsClassType && property.Representation.IsDefaultRepresentation)
				{
					node = plan.CreateObject(Compiler.DefaultObjectPropertyWriteNode(property.Name), null) as PlanNode;
					node.DataType = property.Representation.ScalarType;
					node.Nodes.Add(targetNode);
					node.Nodes.Add(sourceNode);
				}
				else
					throw new CompilerException(CompilerException.Codes.DefaultWriteAccessorCannotBeProvided, property.Name, property.Representation.Name, property.Representation.ScalarType.Name);
			}
			else
				node = BuildCallNode(plan, statement, property.WriteAccessor, new PlanNode[] { targetNode, sourceNode });

			node.Nodes.Clear();
			if (!sourceNode.DataType.Is(property.DataType))
			{
				ConversionContext context = FindConversionPath(plan, sourceNode.DataType, property.DataType);
				CheckConversionContext(plan, context);
				sourceNode = ConvertNode(plan, sourceNode, context);
			}
			
			if (targetNode is PropertyReferenceNode)
			{
				node.Nodes.Add(Upcast(plan, EmitPropertyReadNode(plan, (PropertyReferenceNode)targetNode), property.Representation.ScalarType));
				node.Nodes.Add(Upcast(plan, sourceNode, property.DataType));
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return EmitPropertyReferenceAssignmentNode(plan, statement, node, (PropertyReferenceNode)targetNode); 
			}
			else if (targetNode is StackReferenceNode)
			{
				node.Nodes.Add(Upcast(plan, targetNode, property.Representation.ScalarType));
				node.Nodes.Add(Upcast(plan, sourceNode, property.DataType));
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				AssignmentNode assignmentNode = new AssignmentNode(targetNode, Upcast(plan, node, targetNode.DataType));
				if (statement != null)
					assignmentNode.SetLineInfo(plan, statement.LineInfo);
				else
					assignmentNode.IsBreakable = false;
				assignmentNode.DetermineDataType(plan);
				assignmentNode.DetermineCharacteristics(plan);
				return assignmentNode;
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, statement);
		}
		
		protected static PlanNode EmitPropertyReferenceAssignmentNode(Plan plan, Statement statement, PlanNode sourceNode, PropertyReferenceNode targetNode)
		{
			Schema.Property property = targetNode.ScalarType.Representations[targetNode.RepresentationIndex].Properties[targetNode.PropertyIndex];
			return EmitPropertyWriteNode(plan, statement, property, sourceNode, targetNode.Nodes[0]);
		}
		
		protected static PlanNode EmitAssignmentNode(Plan plan, Statement statement, PlanNode sourceNode, PlanNode targetNode)
		{
			if (targetNode is TableNode)
				return EmitTableAssignmentNode(plan, statement, sourceNode, (TableNode)targetNode);
			else if (targetNode is StackReferenceNode)
				return EmitStackReferenceAssignmentNode(plan, statement, sourceNode, (StackReferenceNode)targetNode);
			else if (targetNode is ExtractColumnNode)
				return EmitColumnExtractorAssignmentNode(plan, statement, sourceNode, (ExtractColumnNode)targetNode);
			else if (targetNode is PropertyReferenceNode)
				return EmitPropertyReferenceAssignmentNode(plan, statement, sourceNode, (PropertyReferenceNode)targetNode);
			else
				throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, statement);
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
		protected static PlanNode CompileAssignmentStatement(Plan plan, Statement statement)
		{
			AssignmentStatement localStatement = (AssignmentStatement)statement;
			PlanNode targetNode = null;
			plan.PushStatementContext(new StatementContext(StatementType.Assignment));
			try
			{
				targetNode = CompileExpression(plan, localStatement.Target);
			}
			finally
			{
				plan.PopStatementContext();
			}
			
			//	LTargetNode must be one of the following ->
			//		StackReferenceNode
			//		ExtractColumnNode
			//		TableNode
			//		PropertyReferenceNode
			PlanNode sourceNode = CompileTypedExpression(plan, localStatement.Expression, targetNode.DataType, true);
			return EmitAssignmentNode(plan, statement, sourceNode, targetNode);
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
		public static PlanNode CompileInsertStatement(Plan plan, Statement statement)
		{
			InsertStatement localStatement = (InsertStatement)statement;

			if (localStatement.SourceExpression is TableSelectorExpression)
			{
				PlanNode sourceNode = CompileTableSelectorExpression(plan, (TableSelectorExpression)localStatement.SourceExpression);

				PlanNode blockNode = (sourceNode.NodeCount > 1 ? (PlanNode)new DelimitedBlockNode() : (PlanNode)new BlockNode());
				blockNode.SetLineInfo(plan, localStatement.LineInfo);
				foreach (PlanNode rowNode in sourceNode.Nodes)
				{
					InsertStatement insertStatement = new InsertStatement();
					insertStatement.SetLineInfo(localStatement.LineInfo);
					insertStatement.Modifiers = localStatement.Modifiers;
					insertStatement.SourceExpression = (Expression)rowNode.EmitStatement(EmitMode.ForCopy);
					insertStatement.Target = localStatement.Target;
					blockNode.Nodes.Add(CompileInsertStatement(plan, insertStatement));
				}
				return blockNode;
			}
			else
			{
				InsertNode node = new InsertNode();
				node.SetLineInfo(plan, localStatement.LineInfo);
				node.Modifiers = localStatement.Modifiers;
				PlanNode sourceNode;
				PlanNode targetNode;

				// Prepare the source node
				sourceNode = CompileExpression(plan, localStatement.SourceExpression);
				
				// Prepare the target node
				plan.PushStatementContext(new StatementContext(StatementType.Insert));
				try
				{
					targetNode = CompileExpression(plan, localStatement.Target);
					if (!(targetNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.InvalidUpdateTarget, localStatement.Target);
					
					// Make sure the target node is static to avoid the overhead of refreshing after each insert
					if (targetNode is TableNode)
						((TableNode)targetNode).CursorType = CursorType.Static;
				}
				finally
				{
					plan.PopStatementContext();
				}

				Schema.IDataType conversionTargetType;
				
				if (sourceNode.DataType is Schema.IRowType)
					conversionTargetType = ((Schema.ITableType)targetNode.DataType).RowType;
				else
				{
					if (!(sourceNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableOrRowExpressionExpected, localStatement.SourceExpression);

					TableNode tableNode = EnsureTableNode(plan, sourceNode);
						
					if (tableNode.CursorType != CursorType.Static)
						sourceNode = EmitCopyNode(plan, tableNode);
					else
						sourceNode = tableNode;
					
					conversionTargetType = targetNode.DataType;
				}
				
				if (Boolean.Parse(LanguageModifiers.GetModifier(node.Modifiers, "InsertedUpdate", "false")))
				{
					// if this is an inserted update, then the source expression will have old and new columns,
					// and the target type must be made to look the same
					Schema.TableType tableType = conversionTargetType as Schema.TableType;
					if (tableType != null)
					{
						Schema.TableType newTableType = new Schema.TableType();
						foreach (Schema.Column column in tableType.Columns)
							newTableType.Columns.Add(column.Copy(Keywords.Old));
						foreach (Schema.Column column in tableType.Columns)
							newTableType.Columns.Add(column.Copy(Keywords.New));
						conversionTargetType = newTableType;
					}
					else
					{
						Schema.RowType rowType = (Schema.RowType)conversionTargetType;
						Schema.RowType newRowType = new Schema.RowType();
						foreach (Schema.Column column in rowType.Columns)
							newRowType.Columns.Add(column.Copy(Keywords.Old));
						foreach (Schema.Column column in rowType.Columns)
							newRowType.Columns.Add(column.Copy(Keywords.New));
						conversionTargetType = newRowType;
					}
				}

				// Verify that all the columns in the source row or table type are assignable to the columns of the target table
				// insert a redefine node if necessary
				if (!sourceNode.DataType.Is(conversionTargetType))
				{
					ConversionContext context = FindConversionPath(plan, sourceNode.DataType, conversionTargetType, true);
					CheckConversionContext(plan, context);
					sourceNode = ConvertNode(plan, sourceNode, context);
				}
				
				node.Nodes.Add(Upcast(plan, sourceNode, targetNode.DataType));
				node.Nodes.Add(EmitInsertConditionNode(plan, targetNode));
			
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return node;
			}
		}
		
		public static UpdateColumnNode EmitUpdateColumnNode(Plan plan, UpdateColumnExpression expression, PlanNode sourceNode)
		{
			PlanNode columnTargetNode;
			UpdateColumnNode columnNode;
			plan.PushStatementContext(new StatementContext(StatementType.Assignment));
			try
			{
				columnTargetNode = CompileExpression(plan, expression.Target);
			}
			finally
			{
				plan.PopStatementContext();
			}

			if (columnTargetNode is StackColumnReferenceNode)
			{
				columnNode = new UpdateColumnNode();
				#if USECOLUMNLOCATIONBINDING
				columnNode.ColumnLocation = ((StackColumnReferenceNode)columnTargetNode).ColumnLocation;
				#else
				columnNode.ColumnName = ((StackColumnReferenceNode)columnTargetNode).Identifier;
				#endif
				columnNode.DataType = columnTargetNode.DataType;
				
				columnNode.Nodes.Add(EmitTypedNode(plan, sourceNode, columnNode.DataType));
				columnNode.DetermineCharacteristics(plan);
			}
			else if (columnTargetNode is PropertyReferenceNode)
			{
				PropertyReferenceNode propertyReference = (PropertyReferenceNode)columnTargetNode;
				Schema.Property property = propertyReference.ScalarType.Representations[propertyReference.RepresentationIndex].Properties[propertyReference.PropertyIndex];
				PlanNode writeNode = BuildCallNode(plan, null, property.WriteAccessor, new PlanNode[]{columnTargetNode.Nodes[0], EmitTypedNode(plan, sourceNode, property.DataType)});
				if (!(columnTargetNode.Nodes[0] is StackColumnReferenceNode))
					throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, expression);
				columnNode = new UpdateColumnNode();
				#if USECOLUMNLOCATIONBINDING
				columnNode.ColumnLocation = ((StackColumnReferenceNode)columnTargetNode.Nodes[0]).ColumnLocation;
				#else
				columnNode.ColumnName = ((StackColumnReferenceNode)columnTargetNode.Nodes[0]).Identifier;
				#endif
				columnNode.DataType = columnTargetNode.Nodes[0].DataType;

				columnNode.Nodes.Add(writeNode);
				columnNode.DetermineCharacteristics(plan);
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidAssignmentTarget, expression);

			return columnNode;
		}
		
		public static UpdateColumnNode CompileUpdateColumnExpression(Plan plan, UpdateColumnExpression expression)
		{
			return EmitUpdateColumnNode(plan, expression, CompileExpression(plan, expression.Expression));
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
		public static PlanNode CompileUpdateStatement(Plan plan, Statement statement)
		{
			UpdateStatement localStatement = (UpdateStatement)statement;
			PlanNode targetNode;
			plan.PushStatementContext(new StatementContext(StatementType.Update));
			try
			{
				targetNode = CompileExpression(plan, localStatement.Target);
			}
			finally
			{
				plan.PopStatementContext();
			}

			if (targetNode.DataType is Schema.ITableType)
			{
				StackReferenceNode stackReferenceNode = targetNode as StackReferenceNode;
				if (stackReferenceNode != null)
					stackReferenceNode.ByReference = true;

				targetNode = EnsureTableNode(plan, targetNode);
				UpdateNode node = new UpdateNode();
				node.SetLineInfo(plan, localStatement.LineInfo);
				node.Modifiers = localStatement.Modifiers;
				TableNode targetTableNode = (TableNode)targetNode;

				Schema.TableVar targetTableVar = targetTableNode.TableVar;

				node.TargetNode = targetNode;

				if (localStatement.Condition != null)
				{
					targetNode = EmitUpdateConditionNode(plan, targetNode, localStatement.Condition);
					node.ConditionNode = targetNode.Nodes[1];
					node.Nodes.Add(targetNode);
				}
				else
					node.Nodes.Add(targetNode);

				Schema.Key affectedKey = null;
				plan.EnterRowContext();
				try
				{
					plan.Symbols.Push(new Symbol(String.Empty, targetTableVar.DataType.RowType));
					try
					{
						UpdateColumnNode columnNode;
						foreach (UpdateColumnExpression expression in localStatement.Columns)
						{
							columnNode = CompileUpdateColumnExpression(plan, expression);
							#if USECOLUMNLOCATIONBINDING
							node.IsKeyAffected = 
								node.IsKeyAffected && 
								(targetTableNode.Order != null) && 
								targetTableNode.Order.Columns.Contains(targetTableVar.Columns[columnNode.ColumnLocation].Name);
							#else
							node.IsKeyAffected = 
								node.IsKeyAffected && 
								(targetTableNode.Order != null) && 
								targetTableNode.Order.Columns.Contains(columnNode.ColumnName);
							#endif

							if (affectedKey == null)
							{
								foreach (Schema.Key key in targetTableNode.TableVar.Keys)
									if (key.Columns.ContainsName(columnNode.ColumnName) && !columnNode.IsContextLiteral(0))
									{
										affectedKey = key;
										break;
									}
							}

							node.Nodes.Add(columnNode);
						}
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.ExitRowContext();
				}
				
				if (affectedKey != null)
				{
					//begin
					//	var LV := T where <condition> { <old column list>, <new column list> };
					//	delete with { Unchecked = "True" } T where <condition>;
					//	insert with { InsertedUpdate = "True", UpdateColumnNames = "<column names semicolon list>" }
					//		LV into T; // rename new add { <old column list> };
					//end;
					
					DelimitedBlock block = new DelimitedBlock();
					VariableStatement variableStatement = new VariableStatement();
					variableStatement.VariableName = new IdentifierExpression(Schema.Object.NameFromGuid(Guid.NewGuid()));
					List<int> updateColumnIndexes = new List<int>();
					StringBuilder updateColumnNames = new StringBuilder();

					SpecifyExpression specifyExpression = new SpecifyExpression();
					specifyExpression.Expression = localStatement.Condition == null ? localStatement.Target : new RestrictExpression(localStatement.Target, localStatement.Condition);
					for (int index = 0; index < targetTableVar.Columns.Count; index++)
						specifyExpression.Expressions.Add(new NamedColumnExpression(new IdentifierExpression(Schema.Object.EnsureRooted(targetTableVar.Columns[index].Name)), Schema.Object.Qualify(targetTableVar.Columns[index].Name, Keywords.Old)));
						
					for (int index = 1; index < node.Nodes.Count; index++)
					{
						UpdateColumnNode columnNode = (UpdateColumnNode)node.Nodes[index];
						updateColumnIndexes.Add(targetTableVar.Columns.IndexOfName(columnNode.ColumnName));
						if (updateColumnNames.Length > 0)
							updateColumnNames.Append(";");
						updateColumnNames.Append(targetTableVar.Columns[updateColumnIndexes[index - 1]].Name);
					}
						
					for (int index = 0; index < targetTableVar.Columns.Count; index++)
					{
						int updateIndex = updateColumnIndexes.IndexOf(index);
						if (updateIndex >= 0)
							specifyExpression.Expressions.Add(new NamedColumnExpression((Expression)node.Nodes[updateIndex + 1].Nodes[0].EmitStatement(EmitMode.ForCopy), Schema.Object.Qualify(targetTableVar.Columns[index].Name, Keywords.New)));
						else
							specifyExpression.Expressions.Add(new NamedColumnExpression(new IdentifierExpression(Schema.Object.EnsureRooted(targetTableVar.Columns[index].Name)), Schema.Object.Qualify(targetTableVar.Columns[index].Name, Keywords.New)));
					}
					
					variableStatement.Expression = specifyExpression;
					block.Statements.Add(variableStatement);
					DeleteStatement deleteStatement = new DeleteStatement(localStatement.Condition == null ? localStatement.Target : new RestrictExpression(localStatement.Target, localStatement.Condition));
					deleteStatement.Modifiers = new LanguageModifiers();
					deleteStatement.Modifiers.Add(new LanguageModifier("Unchecked", "True"));
					block.Statements.Add(deleteStatement);
					InsertStatement insertStatement = new InsertStatement(variableStatement.VariableName, localStatement.Target);
					insertStatement.Modifiers = new LanguageModifiers();
					insertStatement.Modifiers.Add(new LanguageModifier("InsertedUpdate", "True"));
					insertStatement.Modifiers.Add(new LanguageModifier("UpdateColumnNames", updateColumnNames.ToString()));
					block.Statements.Add(insertStatement);

					return CompileDelimitedBlock(plan, block);
				}
				else
				{
					node.DetermineDataType(plan);
					node.DetermineCharacteristics(plan);
					return node;
				}
			}
			else if (targetNode.DataType is Schema.IRowType)
			{
				UpdateRowNode node = new UpdateRowNode();
				node.SetLineInfo(plan, localStatement.LineInfo);
				node.Modifiers = localStatement.Modifiers;
				node.Nodes.Add(targetNode);
				node.ColumnExpressions.AddRange(localStatement.Columns);
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return node;
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidUpdateTarget, localStatement.Target);
		}

		// Source BNF -> delete <expression>
		//
		// DeleteNode
		//		Nodes[0] = TargetNode
		//
		// Default Execution Behavior ->
		//		while a target node is not empty
		//			delete the row		
		public static PlanNode CompileDeleteStatement(Plan plan, Statement statement)
		{
			DeleteStatement localStatement = (DeleteStatement)statement;
			DeleteNode node = new DeleteNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.Modifiers = localStatement.Modifiers;
			PlanNode targetNode;

			plan.PushStatementContext(new StatementContext(StatementType.Delete));
			try
			{
				targetNode = CompileExpression(plan, localStatement.Target);
			}
			finally
			{
				plan.PopStatementContext();
			}

			if (!(targetNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.InvalidUpdateTarget, localStatement.Target);
			StackReferenceNode stackReferenceNode = targetNode as StackReferenceNode;
			if (stackReferenceNode != null)
				stackReferenceNode.ByReference = true;
			node.Nodes.Add(EnsureTableNode(plan, targetNode));
			
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static bool GetEnforced(Plan plan, MetaData metaData)
		{
			return GetEnforced(plan, metaData, true);
		}
		
		public static bool GetEnforced(Plan plan, MetaData metaData, bool defaultEnforced)
		{
			Tag tag = MetaData.GetTag(metaData, "Storage.Enforced");
			if (tag != Tag.None)
			{
				metaData.Tags.Remove(tag);
				Tag newTag = new Tag("DAE.Enforced", (!Convert.ToBoolean(tag.Value)).ToString(), false, tag.IsStatic);
				metaData.Tags.Add(newTag);
				if (!plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedTag, CompilerErrorLevel.Warning, tag.Name, newTag.Name));
			}
				
			return Convert.ToBoolean(MetaData.GetTag(metaData, "DAE.Enforced", defaultEnforced.ToString()));
		}
		
		public static bool GetIsSparse(Plan plan, MetaData metaData)
		{
			Tag tag = MetaData.GetTag(metaData, "Storage.IsSparse");
			if (tag != Tag.None)
			{
				metaData.Tags.Remove(tag);
				Tag newTag = new Tag("DAE.IsSparse", tag.Value, false, tag.IsStatic);
				metaData.Tags.Add(newTag);
				if (!plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedTag, CompilerErrorLevel.Warning, tag.Name, newTag.Name));
			}
			
			return Convert.ToBoolean(MetaData.GetTag(metaData, "DAE.IsSparse", "false"));
		}
		
		public static void ProcessIsClusteredTag(Plan plan, MetaData metaData)
		{
			Tag tag = MetaData.GetTag(metaData, "Storage.IsClustered");
			if (tag != Tag.None)
			{
				metaData.Tags.Remove(tag);
				Tag newTag = new Tag("DAE.IsClustered", tag.Value, false, tag.IsStatic);
				metaData.Tags.Add(newTag);
				if (!plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedTag, CompilerErrorLevel.Warning, tag.Name, newTag.Name));
			}
		}
		
		public static Schema.Key CompileKeyDefinition(Plan plan, Schema.TableVar tableVar, KeyDefinition key)
		{
			Schema.Key localKey = new Schema.Key(Schema.Object.GetObjectID(key.MetaData), key.MetaData);
			foreach (KeyColumnDefinition column in key.Columns)
				localKey.Columns.Add(tableVar.Columns[column.ColumnName]);

			ProcessIsClusteredTag(plan, localKey.MetaData);
			localKey.Enforced = GetEnforced(plan, localKey.MetaData);
			localKey.IsSparse = GetIsSparse(plan, localKey.MetaData);
			return localKey;
		}
		
		public static void CompileTableVarKeys(Plan plan, Schema.TableVar tableVar, KeyDefinitions keys)
		{
			CompileTableVarKeys(plan, tableVar, keys, true);
		}
		
		public static bool SupportsEqual(Plan plan, Schema.IDataType dataType)
		{
			if (SupportsComparison(plan, dataType))
				return true;
				
			Schema.Signature signature = new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(dataType), new Schema.SignatureElement(dataType)});
			OperatorBindingContext context = new OperatorBindingContext(null, "iEqual", plan.NameResolutionPath, signature, true);
			Compiler.ResolveOperator(plan, context);
			return context.Operator != null;
		}
		
		public static bool SupportsComparison(Plan plan, Schema.IDataType dataType)
		{
			Schema.Signature signature = new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(dataType), new Schema.SignatureElement(dataType)});
			OperatorBindingContext context = new OperatorBindingContext(null, "iCompare", plan.NameResolutionPath, signature, true);
			Compiler.ResolveOperator(plan, context);
			return context.Operator != null;
		}
		
		public static void CompileTableVarKeys(Plan plan, Schema.TableVar tableVar, KeyDefinitions keys, bool ensureKey)
		{
			foreach (KeyDefinition keyDefinition in keys)
			{
				Schema.Key key = CompileKeyDefinition(plan, tableVar, keyDefinition);
				if (!tableVar.Keys.Contains(key))
					tableVar.Keys.Add(key);
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, key.Name);
			}

			if (ensureKey && (tableVar.Keys.Count == 0))
			{
				Schema.Key key = new Schema.Key();
				foreach (Schema.TableVarColumn tableColumn in tableVar.Columns)
					if (SupportsComparison(plan, tableColumn.DataType))
						key.Columns.Add(tableColumn);
				tableVar.Keys.Add(key);
			}
		}
		
		public static Schema.Key KeyFromKeyColumnDefinitions(Plan plan, Schema.TableVar tableVar, KeyColumnDefinitions keyColumns)
		{
			Schema.Key key = new Schema.Key();
			foreach (KeyColumnDefinition column in keyColumns)
				key.Columns.Add(tableVar.Columns[column.ColumnName]);
			return key;
		}
		
		public static Schema.Key FindKey(Plan plan, Schema.TableVar tableVar, KeyColumnDefinitions keyColumns)
		{
			Schema.Key key = KeyFromKeyColumnDefinitions(plan, tableVar, keyColumns);
			
			int index = tableVar.IndexOfKey(key);
			if (index >= 0)
				return tableVar.Keys[index];
			
			throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, key.Name);
		}
		
		public static Schema.Key FindKey(Plan plan, Schema.TableVar tableVar, KeyDefinitionBase keyDefinition)
		{
			return FindKey(plan, tableVar, keyDefinition.Columns);
		}
		
		public static Schema.Key FindClusteringKey(Plan plan, Schema.TableVar tableVar)
		{
			Schema.Key minimumKey = null;
			foreach (Schema.Key key in tableVar.Keys)
			{
				if (Convert.ToBoolean(MetaData.GetTag(key.MetaData, "DAE.IsClustered", "false")))
					return key;
				
				if (!key.IsSparse)
					if (minimumKey == null)
						minimumKey = key;
					else
						if (minimumKey.Columns.Count > key.Columns.Count)
							minimumKey = key;
			}
					
			if (minimumKey != null)
				return minimumKey;

			throw new Schema.SchemaException(Schema.SchemaException.Codes.KeyRequired, tableVar.DisplayName);
		}
		
		public static void EnsureKey(Plan plan, Schema.TableVar tableVar)
		{
			if (tableVar.Keys.Count == 0)
			{
				Schema.Key key = new Schema.Key();
				foreach (Schema.TableVarColumn column in tableVar.Columns)
					if (SupportsComparison(plan, column.DataType))
						key.Columns.Add(column);
				tableVar.Keys.Add(key);
			}
		}

		public static Schema.Sort GetSort(Plan plan, Schema.IDataType dataType)
		{
			Schema.ScalarType scalarType = dataType as Schema.ScalarType;
			if (scalarType != null)
			{
				if (scalarType.Sort == null)
				{
					if (scalarType.SortID >= 0)
						plan.CatalogDeviceSession.ResolveCatalogObject(scalarType.SortID);
					else
						return GetUniqueSort(plan, dataType);
				}

				return scalarType.Sort;
			}
			else
			{
				Schema.Sort sort = CompileSortDefinition(plan, dataType);
				return sort;
			}
		}
		
		public static Schema.Sort GetUniqueSort(Plan plan, Schema.IDataType dataType)
		{
			Schema.ScalarType scalarType = dataType as Schema.ScalarType;
			if (scalarType != null)
			{
				if (scalarType.UniqueSort == null)
				{
					if (scalarType.UniqueSortID >= 0)
						plan.CatalogDeviceSession.ResolveCatalogObject(scalarType.UniqueSortID);
					else
					{
						plan.PushLoadingContext(new LoadingContext(scalarType.Owner, scalarType.Library.Name, false));
						try
						{
							CreateSortNode createSortNode = new CreateSortNode();
							createSortNode.ScalarType = scalarType;
							createSortNode.Sort = CompileSortDefinition(plan, scalarType);
							createSortNode.IsUnique = true;
							plan.ExecuteNode(createSortNode);
						}
						finally
						{
							plan.PopLoadingContext();
						}
					}
				}

				return scalarType.UniqueSort;
			}
			else
			{
				Schema.Sort sort = CompileSortDefinition(plan, dataType);
				return sort;
			}
		}
		
		public static bool IsOrderUnique(Plan plan, Schema.TableVar tableVar, Schema.Order order)
		{
			foreach (Schema.Key key in tableVar.Keys)
				if (!key.IsSparse && OrderIncludesKey(plan, order, key))
					return true;
			return false;
		}
		
		public static void EnsureOrderUnique(Plan plan, Schema.TableVar tableVar, Schema.Order order)
		{
			if (!IsOrderUnique(plan, tableVar, order))
			{
				bool isDescending = order.IsDescending;
				foreach (Schema.TableVarColumn column in FindClusteringKey(plan, tableVar).Columns)
				{
					Schema.Sort uniqueSort = GetUniqueSort(plan, column.DataType);
					if (!order.Columns.Contains(column.Name, uniqueSort))
					{
						Schema.OrderColumn newColumn = new Schema.OrderColumn(column, !isDescending);
						newColumn.Sort = uniqueSort;
						newColumn.IsDefaultSort = true;
						plan.AttachDependency(newColumn.Sort);
						order.Columns.Add(newColumn);
					}
				}
			}
		}
		
		public static Schema.Order CompileOrderColumnDefinitions(Plan plan, Schema.TableVar tableVar, OrderColumnDefinitions columns, MetaData metaData, bool ensureUnique)
		{
			Schema.Order order = new Schema.Order(Schema.Object.GetObjectID(metaData), metaData);
			ProcessIsClusteredTag(plan, order.MetaData);
			Schema.OrderColumn column;
			foreach (OrderColumnDefinition orderColumn in columns)
			{
				column = new Schema.OrderColumn(tableVar.Columns[orderColumn.ColumnName], orderColumn.Ascending, orderColumn.IncludeNils);
				if (orderColumn.Sort != null)
				{
					column.Sort = CompileSortDefinition(plan, column.Column.DataType, orderColumn.Sort, false);
					column.IsDefaultSort = false;
					if (column.Sort.HasDependencies())
						plan.AttachDependencies(column.Sort.Dependencies);
				}
				else
				{
					column.Sort = GetSort(plan, column.Column.DataType);
					column.IsDefaultSort = true;
					plan.AttachDependency(column.Sort);
				}
				order.Columns.Add(column);
			}
			if (ensureUnique)
				EnsureOrderUnique(plan, tableVar, order);
			return order;
		}
		
		public static Schema.Order CompileOrderColumnDefinitions(Plan plan, Schema.TableVar tableVar, OrderColumnDefinitions columns)
		{
			return CompileOrderColumnDefinitions(plan, tableVar, columns, null, true);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan plan, Schema.TableVar tableVar, OrderDefinitionBase order)
		{
			return CompileOrderColumnDefinitions(plan, tableVar, order.Columns, null, true);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan plan, Schema.TableVar tableVar, OrderDefinitionBase order, bool ensureUnique)
		{
			return CompileOrderColumnDefinitions(plan, tableVar, order.Columns, null, ensureUnique);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan plan, Schema.TableVar tableVar, OrderDefinition order)
		{
			return CompileOrderColumnDefinitions(plan, tableVar, order.Columns, order.MetaData, true);
		}
		
		public static Schema.Order CompileOrderDefinition(Plan plan, Schema.TableVar tableVar, OrderDefinition order, bool ensureUnique)
		{
			return CompileOrderColumnDefinitions(plan, tableVar, order.Columns, order.MetaData, ensureUnique);
		}
		
		public static void CompileTableVarOrders(Plan plan, Schema.TableVar tableVar, OrderDefinitions orders)
		{
			foreach (OrderDefinition orderDefinition in orders)
			{
				Schema.Order order = CompileOrderDefinition(plan, tableVar, orderDefinition, false);
				if (!tableVar.Orders.Contains(order))
					tableVar.Orders.Add(order);
				else
				{
					// Do not throw if this is a view definition, the order is present, so the view should behave as expected.
					if (tableVar is Schema.BaseTableVar)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, order.Name);
				}
			}
		}

		public static Schema.Order OrderFromKey(Plan plan, Schema.Key key)
		{
			return OrderFromKey(plan, key.Columns);
		}
		
		public static Schema.Order OrderFromKey(Plan plan, Schema.TableVarColumnsBase columns)
		{
			Schema.Order order = new Schema.Order();
			Schema.OrderColumn orderColumn;
			Schema.TableVarColumn column;
			for (int index = 0; index < columns.Count; index++)
			{
				column = columns[index];
				orderColumn = new Schema.OrderColumn(column, true, true);
				if (column.DataType is Schema.ScalarType)
					orderColumn.Sort = GetUniqueSort(plan, column.DataType);
				else
					orderColumn.Sort = CompileSortDefinition(plan, column.DataType);
				orderColumn.IsDefaultSort = true;
				plan.AttachDependency(orderColumn.Sort);
				order.Columns.Add(orderColumn);
			}
			return order;
		}

		public static Schema.Order FindOrder(Plan plan, Schema.TableVar tableVar, OrderDefinitionBase orderDefinition)
		{
			Schema.Order order = CompileOrderDefinition(plan, tableVar, orderDefinition, false);
			
			int index = tableVar.IndexOfOrder(order);
			if (index >= 0)
				return tableVar.Orders[index];

			throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, order.Name);
		}
		
		public static Schema.Order FindClusteringOrder(Plan plan, Schema.TableVar tableVar)
		{
			Schema.Key minimumKey = null;
			foreach (Schema.Key key in tableVar.Keys)
			{
				if (Convert.ToBoolean(MetaData.GetTag(key.MetaData, "DAE.IsClustered", "false")))
					return OrderFromKey(plan, key);
					
				if (!key.IsSparse)
					if (minimumKey == null)
						minimumKey = key;
					else
						if (minimumKey.Columns.Count > key.Columns.Count)
							minimumKey = key;
			}

			foreach (Schema.Order order in tableVar.Orders)
				if (Convert.ToBoolean(MetaData.GetTag(order.MetaData, "DAE.IsClustered", "false")))
					return order;
					
			if (minimumKey != null)
				return OrderFromKey(plan, minimumKey);
					
			if (tableVar.Orders.Count > 0)
				return tableVar.Orders[0];
				
			throw new Schema.SchemaException(Schema.SchemaException.Codes.KeyRequired, tableVar.DisplayName);
		}
		
		// returns true if the order includes the key as a subset, including the use of the unique sort algorithm for the type of each column
		public static bool OrderIncludesKey(Plan plan, Schema.Order includingOrder, Schema.Key includedKey)
		{
			Schema.TableVarColumn column;
			for (int index = 0; index < includedKey.Columns.Count; index++)
			{
				column = includedKey.Columns[index];
				if (!includingOrder.Columns.Contains(column.Name, GetUniqueSort(plan, column.DataType)))
					return false;
			}

			return true;
		}
		
		public static bool OrderIncludesOrder(Plan plan, Schema.Order includingOrder, Schema.Order includedOrder)
		{
			Schema.OrderColumn column;
			for (int index = 0; index < includedOrder.Columns.Count; index++)
			{
				column = includedOrder.Columns[index];
				if (!includingOrder.Columns.Contains(column.Column.Name, column.Sort))
					return false;
			}
			
			return true;
		}
		
		public static Schema.RowConstraint CompileRowConstraint(Plan plan, Schema.TableVar tableVar, ConstraintDefinition constraint)
		{
			Schema.RowConstraint newConstraint = new Schema.RowConstraint(Schema.Object.GetObjectID(constraint.MetaData), constraint.ConstraintName);
			newConstraint.Library = tableVar.Library == null ? null : plan.CurrentLibrary;
			newConstraint.IsGenerated = constraint.IsGenerated || tableVar.IsGenerated;
			plan.PushCreationObject(newConstraint);
			try
			{
				newConstraint.MergeMetaData(constraint.MetaData);
				newConstraint.Enforced = GetEnforced(plan, constraint.MetaData);
				newConstraint.ConstraintType = Schema.ConstraintType.Row;
					
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Value, tableVar.DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.RowType));
					#endif
					try
					{
						PlanNode constraintNode = CompileBooleanExpression(plan, constraint.Expression);
						if (!(constraintNode.IsFunctional && constraintNode.IsDeterministic))
							throw new CompilerException(CompilerException.Codes.InvalidConstraintExpression, constraint.Expression);

						constraintNode = OptimizeNode(plan, constraintNode);
						newConstraint.Node = constraintNode;
						
						string customMessage = newConstraint.GetCustomMessage(Schema.Transition.Insert);
						if (!String.IsNullOrEmpty(customMessage))
						{
							try
							{
								PlanNode violationMessageNode = CompileTypedExpression(plan, new D4.Parser().ParseExpression(customMessage), plan.DataTypes.SystemString);
								violationMessageNode = OptimizeNode(plan, violationMessageNode);
								newConstraint.ViolationMessageNode = violationMessageNode;
							}
							catch (Exception exception)
							{
								throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, constraint, exception, newConstraint.Name);
							}
						}
						
						newConstraint.DetermineRemotable(plan.CatalogDeviceSession);
							
						if (!newConstraint.IsRemotable)
							newConstraint.ConstraintType = Schema.ConstraintType.Database;
							
						return newConstraint;
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.ExitRowContext();
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static PlanNode CompileTransitionConstraintViolationMessageNode(Plan plan, Schema.TransitionConstraint constraint, Schema.Transition transition, Statement statement)
		{
			string customMessage = constraint.GetCustomMessage(transition);
			if (!String.IsNullOrEmpty(customMessage))
			{
				try
				{
					PlanNode violationMessageNode = CompileTypedExpression(plan, new D4.Parser().ParseExpression(customMessage), plan.DataTypes.SystemString);
					violationMessageNode = OptimizeNode(plan, violationMessageNode);
					return violationMessageNode;
				}
				catch (Exception exception)
				{
					throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, statement, exception, constraint.Name);
				}
			}
			
			return null;
		}
		
		public static Schema.TransitionConstraint CompileTransitionConstraint(Plan plan, Schema.TableVar tableVar, TransitionConstraintDefinition constraint)
		{
			PlanNode constraintNode;
			Schema.TransitionConstraint newConstraint = new Schema.TransitionConstraint(Schema.Object.GetObjectID(constraint.MetaData), constraint.ConstraintName);
			newConstraint.Library = tableVar.Library == null ? null : plan.CurrentLibrary;
			newConstraint.IsGenerated = constraint.IsGenerated || tableVar.IsGenerated;
			plan.PushCreationObject(newConstraint);
			try
			{
				newConstraint.MergeMetaData(constraint.MetaData);
				newConstraint.Enforced = GetEnforced(plan, newConstraint.MetaData);
				newConstraint.ConstraintType = Schema.ConstraintType.Database;
					
				plan.EnterRowContext();
				try
				{
					if (constraint.OnInsertExpression != null)
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.New, tableVar.DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.NewRowType));
						#endif
						try
						{
							constraintNode = CompileBooleanExpression(plan, constraint.OnInsertExpression);
							if (!(constraintNode.IsFunctional && constraintNode.IsRepeatable))
								throw new CompilerException(CompilerException.Codes.InvalidTransitionConstraintExpression, constraint.OnInsertExpression);

							constraintNode = OptimizeNode(plan, constraintNode);
							newConstraint.OnInsertNode = constraintNode;
							newConstraint.OnInsertViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, newConstraint, Schema.Transition.Insert, constraint);
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					
					if (constraint.OnUpdateExpression != null)
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Old, tableVar.DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.OldRowType));
						#endif
						try
						{
							#if USENAMEDROWVARIABLES
							plan.Symbols.Push(new Symbol(Keywords.New, tableVar.DataType.RowType));
							#else
							APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.NewRowType));
							#endif
							try
							{
								constraintNode = CompileBooleanExpression(plan, constraint.OnUpdateExpression);
								if (!(constraintNode.IsFunctional && constraintNode.IsRepeatable))
									throw new CompilerException(CompilerException.Codes.InvalidTransitionConstraintExpression, constraint.OnUpdateExpression);

								constraintNode = OptimizeNode(plan, constraintNode);
								newConstraint.OnUpdateNode = constraintNode;
								newConstraint.OnUpdateViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, newConstraint, Schema.Transition.Update, constraint);
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					
					if (constraint.OnDeleteExpression != null)
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Old, tableVar.DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, ATableVar.DataType.OldRowType));
						#endif
						try
						{
							constraintNode = CompileBooleanExpression(plan, constraint.OnDeleteExpression);
							if (!(constraintNode.IsFunctional && constraintNode.IsRepeatable))
								throw new CompilerException(CompilerException.Codes.InvalidTransitionConstraintExpression, constraint.OnDeleteExpression);

							constraintNode = OptimizeNode(plan, constraintNode);
							newConstraint.OnDeleteNode = constraintNode;
							newConstraint.OnDeleteViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, newConstraint, Schema.Transition.Delete, constraint);
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					
					newConstraint.DetermineRemotable(plan.CatalogDeviceSession);
						
					if (newConstraint.IsRemotable)
						newConstraint.ConstraintType = Schema.ConstraintType.Row;

					return newConstraint;
				}
				finally
				{
					plan.ExitRowContext();
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.TableVarConstraint CompileTableVarConstraint(Plan plan, Schema.TableVar tableVar, CreateConstraintDefinition constraint)
		{
			plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
			try
			{
			if (constraint is ConstraintDefinition)
				return Compiler.CompileRowConstraint(plan, tableVar, (ConstraintDefinition)constraint);
			else
				return Compiler.CompileTransitionConstraint(plan, tableVar, (TransitionConstraintDefinition)constraint);
		}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		public static void CompileTableVarConstraints(Plan plan, Schema.TableVar tableVar, CreateConstraintDefinitions constraints)
		{
			foreach (CreateConstraintDefinition constraint in constraints)
			{
				Schema.Constraint newConstraint = Compiler.CompileTableVarConstraint(plan, tableVar, constraint);

				tableVar.Constraints.Add(newConstraint);
				if (newConstraint is Schema.RowConstraint)
					tableVar.RowConstraints.Add(newConstraint);
				else
				{
					Schema.TransitionConstraint transitionConstraint = (Schema.TransitionConstraint)newConstraint;
					if (transitionConstraint.OnInsertNode != null)
						tableVar.InsertConstraints.Add(transitionConstraint);
					if (transitionConstraint.OnUpdateNode != null)
						tableVar.UpdateConstraints.Add(transitionConstraint);
					if (transitionConstraint.OnDeleteNode != null)
						tableVar.DeleteConstraints.Add(transitionConstraint);
				}
			}
		}
		
		public static void CompileTableVarKeyConstraints(Plan plan, Schema.TableVar tableVar)
		{	
			plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
			try
			{
				foreach (Schema.Key key in tableVar.Keys)
					if (!key.IsInherited && key.Enforced && (key.Constraint == null))
						key.Constraint = CompileKeyConstraint(plan, tableVar, key);
			}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		public static void CompileCreateTableVarStatement(Plan plan, CreateTableVarStatement statement, CreateTableVarNode node, BlockNode blockNode)
		{
			plan.PushGlobalContext();
			try
			{
				CompileTableVarKeys(plan, node.TableVar, statement.Keys);
				CompileTableVarOrders(plan, node.TableVar, statement.Orders);
				if ((node.TableVar is Schema.BaseTableVar) && (!plan.InLoadingContext()))
					((Schema.BaseTableVar)node.TableVar).Device.CheckSupported(plan, node.TableVar);
				CompileTableVarConstraints(plan, node.TableVar, statement.Constraints);
				if (!plan.IsEngine)
					CompileTableVarKeyConstraints(plan, node.TableVar);
			}
			finally
			{
				plan.PopGlobalContext();
			}
			
			CreateReferenceStatement localStatement;
			foreach (ReferenceDefinition reference in statement.References)
			{
				localStatement = new CreateReferenceStatement();
				localStatement.IsSession = statement.IsSession;
				localStatement.TableVarName = node.TableVar.Name;
				localStatement.ReferenceName = reference.ReferenceName;
				foreach (ReferenceColumnDefinition column in reference.Columns)
					localStatement.Columns.Add(column);
				localStatement.ReferencesDefinition = reference.ReferencesDefinition;
				localStatement.MetaData = reference.MetaData;
				blockNode.Nodes.Add(CompileCreateReferenceStatement(plan, localStatement));
			}
		}
		
		public static bool CanBuildCustomMessageForKey(Plan plan, Schema.TableVarColumnsBase columns)
		{
			if (columns.Count > 0)
			{
				foreach (Schema.TableVarColumn column in columns)
					if (!(column.DataType is Schema.ScalarType) || !((Schema.ScalarType)column.DataType).HasRepresentation(NativeAccessors.AsDisplayString))
						return false;
				return true;
			}
			return false;
		}
		
		public static string GetCustomMessageForKey(Plan plan, Schema.TableVar tableVar, Schema.TableVarColumnsBase columns)
		{
			StringBuilder message = new StringBuilder();
			message.AppendFormat("'The table {0} already has a row with ", MetaData.GetTag(tableVar.MetaData, "Frontend.Singular.Title", "Frontend.Title", Schema.Object.Unqualify(tableVar.DisplayName)));
			Schema.ScalarType scalarType;
			Schema.Representation representation;
			
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					message.Append(" and ");
				message.AppendFormat("{0} ", MetaData.GetTag(tableVar.Columns[columns[index].Name].MetaData, "Frontend.Title", columns[index].Name));
				scalarType = (Schema.ScalarType)columns[index].DataType;
				representation = scalarType.FindRepresentation(NativeAccessors.AsDisplayString);
				bool isString = scalarType.NativeType == NativeAccessors.AsDisplayString.NativeType;
				if (isString)
					message.AppendFormat(@"""' + (if IsNil({0}{1}{2}) then ""<no value>"" else {0}{1}{2}{3}{4}) + '""", new object[]{ Keywords.New, Keywords.Qualifier, columns[index].Name, Keywords.Qualifier, representation.Properties[0].Name });
				else
					message.AppendFormat(@"(' + (if IsNil({0}{1}{2}) then ""<no value>"" else {0}{1}{2}{3}{4}) + ')", new object[]{ Keywords.New, Keywords.Qualifier, columns[index].Name, Keywords.Qualifier, representation.Properties[0].Name });
			}
			
			message.Append(".'");
			return message.ToString();
		}
		
		// constructs a transition constraint as follows:
		// transition constraint Key<column names>
			// on insert not exists (<table name> where <column names> = <new.column names>) 
			// on update if (<old.column names> = <new.column names>) then true else not exists (<table name> where <column names> = <new.column names>)
			// tags { DAE.Message = "'The table <table name> already has a row with <column names> ' + <new.column names>.AsString + ' [and...] .'" }
		public static Schema.TransitionConstraint CompileKeyConstraint(Plan plan, Schema.TableVar tableVar, Schema.Key key)
		{
			TransitionConstraintDefinition definition = new TransitionConstraintDefinition();
			definition.ConstraintName = String.Format("Key{0}", ExceptionUtility.StringsToList(key.Columns.ColumnNames));
			definition.IsGenerated = true;
			definition.MetaData = key.MetaData == null ? key.MetaData : key.MetaData.Copy();
			if (definition.MetaData == null)
				definition.MetaData = new MetaData();
			definition.MetaData.Tags.SafeRemove("DAE.ObjectID");
			if (!(definition.MetaData.Tags.Contains("DAE.Message") || definition.MetaData.Tags.Contains("DAE.SimpleMessage")) && CanBuildCustomMessageForKey(plan, key.Columns))
				definition.MetaData.Tags.Add(new Tag("DAE.Message", GetCustomMessageForKey(plan, tableVar, key.Columns)));
				
			BitArray isNilable = new BitArray(key.Columns.Count);
			for (int index = 0; index < key.Columns.Count; index++)
				isNilable[index] = key.Columns[index].IsNilable;
			
			definition.OnInsertExpression =
				new UnaryExpression
				(
					Instructions.Not, 
					new UnaryExpression
					(
						Instructions.Exists, 
						new RestrictExpression
						(
							new IdentifierExpression(tableVar.Name), 
							key.IsSparse ?
								#if USENAMEDROWVARIABLES
								BuildKeyEqualExpression
								(
									plan, 
									String.Empty,
									Keywords.New,
									key.Columns,
									key.Columns
								) :
								BuildRowEqualExpression
								(
									plan, 
									String.Empty,
									Keywords.New,
									key.Columns,
									isNilable
								)
								#else
								BuildKeyEqualExpression
								(
									APlan, 
									new Schema.RowType(AKey.Columns).Columns, 
									new Schema.RowType(AKey.Columns, Keywords.New).Columns
								) :
								BuildRowEqualExpression
								(
									APlan, 
									new Schema.RowType(AKey.Columns).Columns, 
									new Schema.RowType(AKey.Columns, Keywords.New).Columns, 
									LIsNilable, 
									LIsNilable
								)
								#endif
						)
					)
				);
				
			definition.OnUpdateExpression =
				new IfExpression
				(
					#if USENAMEDROWVARIABLES
					BuildRowEqualExpression
					(
						plan, 
						Keywords.Old,
						Keywords.New,
						key.Columns,
						isNilable
					),
					#else
					BuildRowEqualExpression
					(
						APlan, 
						new Schema.RowType(AKey.Columns, Keywords.Old).Columns, 
						new Schema.RowType(AKey.Columns, Keywords.New).Columns, 
						isNilable, 
						isNilable
					),
					#endif
					new ValueExpression(true),
					new UnaryExpression
					(
						Instructions.Not, 
						new UnaryExpression
						(
							Instructions.Exists, 
							new RestrictExpression
							(
								new IdentifierExpression(tableVar.Name), 
								key.IsSparse ?
									#if USENAMEDROWVARIABLES
									BuildKeyEqualExpression(plan, String.Empty, Keywords.New, key.Columns, key.Columns) :
									BuildRowEqualExpression(plan, String.Empty, Keywords.New, key.Columns, isNilable)
									#else
									BuildKeyEqualExpression(APlan, new Schema.RowType(AKey.Columns).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns) :
									BuildRowEqualExpression(APlan, new Schema.RowType(AKey.Columns).Columns, new Schema.RowType(AKey.Columns, Keywords.New).Columns, LIsNilable, LIsNilable)
									#endif
							)
						)
					)
				);
				
			Schema.TransitionConstraint constraint = CompileTransitionConstraint(plan, tableVar, definition);
			constraint.ConstraintType = Schema.ConstraintType.Table;
			constraint.InsertColumnFlags = new BitArray(tableVar.DataType.Columns.Count);
			for (int index = 0; index < constraint.InsertColumnFlags.Length; index++)
				constraint.InsertColumnFlags[index] = key.Columns.ContainsName(tableVar.DataType.Columns[index].Name);
			constraint.UpdateColumnFlags = (BitArray)constraint.InsertColumnFlags.Clone();
			tableVar.Constraints.Add(constraint);
			tableVar.InsertConstraints.Add(constraint);
			tableVar.UpdateConstraints.Add(constraint);
			plan.AttachDependencies(constraint.Dependencies); // Attach dependencies of the constraint to the table
			return constraint;
		}
		
		private static void CompileDefault(Plan plan, Schema.Default defaultValue, Schema.IDataType dataType, DefaultDefinition defaultDefinition)
		{
			plan.Symbols.PushWindow(0); // make sure the default is evaluated in a private context
			try
			{
				defaultValue.IsGenerated = defaultDefinition.IsGenerated;
				defaultValue.MetaData = defaultDefinition.MetaData;
				plan.PushCreationObject(defaultValue);
				try
				{
					defaultValue.Node = CompileTypedExpression(plan, defaultDefinition.Expression, dataType);
					defaultValue.Node = OptimizeNode(plan, defaultValue.Node);
					defaultValue.DetermineRemotable(plan.CatalogDeviceSession);
				}
				finally
				{
					plan.PopCreationObject();
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static Schema.ScalarTypeDefault CompileScalarTypeDefault(Plan plan, Schema.ScalarType scalarType, DefaultDefinition defaultValue)
		{
			int defaultID = Schema.Object.GetObjectID(defaultValue.MetaData);
			string defaultName = Schema.Object.EnsureNameLength(String.Format("{0}_Default", scalarType.Name));
			Schema.ScalarTypeDefault localDefaultValue = new Schema.ScalarTypeDefault(defaultID, defaultName);
			localDefaultValue.Library = scalarType.Library == null ? null : plan.CurrentLibrary;
			CompileDefault(plan, localDefaultValue, scalarType, defaultValue);
			return localDefaultValue;
		}
		
		public static Schema.TableVarColumnDefault CompileTableVarColumnDefault(Plan plan, Schema.TableVar tableVar, Schema.TableVarColumn column, DefaultDefinition defaultValue)
		{
			int defaultID = Schema.Object.GetObjectID(defaultValue.MetaData);
			string defaultName = Schema.Object.EnsureNameLength(String.Format("{0}_{1}_Default", tableVar.Name, column.Name));
			Schema.TableVarColumnDefault localDefaultValue = new Schema.TableVarColumnDefault(defaultID, defaultName);
			localDefaultValue.Library = tableVar.Library == null ? null : plan.CurrentLibrary;
			CompileDefault(plan, localDefaultValue, column.DataType, defaultValue);
			localDefaultValue.IsGenerated = localDefaultValue.IsGenerated || tableVar.IsGenerated;
			return localDefaultValue;
		}
		
		public static PlanNode CompileFrameNode(Plan plan, Statement statement)
		{
			FrameNode node = new FrameNode();
			node.SetLineInfo(plan, statement.LineInfo);
			plan.Symbols.PushFrame();
			try
			{
				node.Nodes.Add(CompileDeallocateFrameVariablesNode(plan, CompileStatement(plan, statement)));
				return node;
			}
			finally
			{
				plan.Symbols.PopFrame();
			}
		}
		
		public static PlanNode CompileDeallocateFrameVariablesNode(Plan plan, PlanNode node)
		{
			BlockNode blockNode = null;
			for (int index = 0; index < plan.Symbols.FrameCount; index++)
				if (plan.Symbols[index].DataType.IsDisposable)
				{
					if (blockNode == null)
					{
						blockNode = new BlockNode();
						blockNode.IsBreakable = false;
						blockNode.Nodes.Add(node);
					}
					blockNode.Nodes.Add(new DeallocateVariableNode(index));
				}
				
			return blockNode == null ? node : blockNode;
		}
		
		public static PlanNode CompileDeallocateVariablesNode(Plan plan, PlanNode node, Symbol resultVar)
		{
			BlockNode blockNode = null;
			for (int index = 0; index < plan.Symbols.Count; index++)
				if ((plan.Symbols[index].Name != resultVar.Name) && plan.Symbols[index].DataType.IsDisposable)
				{
					if (blockNode == null)
					{
						blockNode = new BlockNode();
						blockNode.IsBreakable = false;
						blockNode.Nodes.Add(node);
						node = blockNode;
					}
					blockNode.Nodes.Add(new DeallocateVariableNode(index));
				}
				
			return node;
		}
		
		public static Schema.Device GetDefaultDevice(Plan plan, bool mustResolve)
		{
			// the first unambiguous device in a breadth-first traversal of the current library dependency graph
			Schema.Device device = null;
			string defaultDeviceName = String.Empty;
			try
			{
				defaultDeviceName = plan.GetDefaultDeviceName(plan.CurrentLibrary.Name, true);
				if (defaultDeviceName != String.Empty)
				{
					Schema.Object objectValue = ResolveCatalogIdentifier(plan, defaultDeviceName, mustResolve);
					if (!(objectValue is Schema.Device) && mustResolve)
						throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, plan.CurrentStatement());
					device = (Schema.Device)objectValue;
				}
			}
			catch (Exception exception)
			{
				if (mustResolve)
					throw new CompilerException(CompilerException.Codes.UnableToResolveDefaultDevice, plan.CurrentStatement(), exception, defaultDeviceName);
			}

			if ((device == null) && mustResolve)
				device = plan.TempDevice;

			return device;
		}
		
		public static Schema.TableVarColumn CompileTableVarColumnDefinition(Plan plan, Schema.TableVar tableVar, ColumnDefinition column)
		{
			Schema.Column newColumn = new Schema.Column(column.ColumnName, CompileTypeSpecifier(plan, column.TypeSpecifier));
			Schema.TableVarColumn newTableVarColumn = new Schema.TableVarColumn(Schema.Object.GetObjectID(column.MetaData), newColumn, column.MetaData, Schema.TableVarColumnType.Stored);
			newTableVarColumn.IsNilable = column.IsNilable;
				
			foreach (ConstraintDefinition constraint in column.Constraints)
				newTableVarColumn.Constraints.Add(CompileTableVarColumnConstraint(plan, tableVar, newTableVarColumn, constraint));
			
			// TODO: verify that the default satisfies the constraints
			if (column.Default != null)
				newTableVarColumn.Default = CompileTableVarColumnDefault(plan, tableVar, newTableVarColumn, column.Default);
				
			// if the default is not remotable, make sure that the DAE.IsDefaultRemotable tag is false, if it exists
			Tag tag = newTableVarColumn.GetMetaDataTag("DAE.IsDefaultRemotable");
			if (tag != Tag.None)
			{
				bool remotable = Boolean.Parse(tag.Value);
				newTableVarColumn.IsDefaultRemotable = newTableVarColumn.IsDefaultRemotable && remotable;
				if (!(newTableVarColumn.IsDefaultRemotable ^ remotable))
					newTableVarColumn.MetaData.Tags.Update("DAE.IsDefaultRemotable", newTableVarColumn.IsDefaultRemotable.ToString());
			}
			
			// if the change is not remotable, make sure that the DAE.IsChangeRemotable tag is false, if it exists
			tag = newTableVarColumn.GetMetaDataTag("DAE.IsChangeRemotable");
			if (tag != Tag.None)
			{
				bool remotable = Boolean.Parse(tag.Value);
				newTableVarColumn.IsChangeRemotable = newTableVarColumn.IsChangeRemotable && remotable;
				if (!(newTableVarColumn.IsChangeRemotable ^ remotable))
					newTableVarColumn.MetaData.Tags.Update("DAE.IsChangeRemotable", newTableVarColumn.IsChangeRemotable.ToString());
			}
			
			// if the validate is not remotable, make sure that the DAE.IsValidateRemotable tag is false, if it exists
			tag = newTableVarColumn.GetMetaDataTag("DAE.IsValidateRemotable");
			if (tag != Tag.None)
			{
				bool remotable = Boolean.Parse(tag.Value);
				newTableVarColumn.IsValidateRemotable = newTableVarColumn.IsValidateRemotable && remotable;
				if (!(newTableVarColumn.IsValidateRemotable ^ remotable))
					newTableVarColumn.MetaData.Tags.Update("DAE.IsValidateRemotable", newTableVarColumn.IsValidateRemotable.ToString());
			}
			
			return newTableVarColumn;
		}
		
		public static PlanNode CompileCreateTableStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateTableStatement localStatement = (CreateTableStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateTable);
			BlockNode blockNode = new BlockNode();
			blockNode.SetLineInfo(plan, statement.LineInfo);
			CreateTableNode node = new CreateTableNode();
			node.SetLineInfo(plan, statement.LineInfo);
			blockNode.Nodes.Add(node);
			
			Tag tag;
			plan.Symbols.PushWindow(0); // make sure the create table statement is evaluated in a private context
			try
			{
				string tableName = Schema.Object.Qualify(localStatement.TableVarName, plan.CurrentLibrary.Name);
				string sessionTableName = null;
				string sourceTableName = null;
				if (localStatement.IsSession)
				{
					sessionTableName = tableName;
					if (plan.IsEngine)
						tableName = MetaData.GetTag(localStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						tableName = Schema.Object.NameFromGuid(Guid.NewGuid());
					CheckValidSessionObjectName(plan, statement, sessionTableName);
					plan.PlanSessionObjects.Add(new Schema.SessionObject(sessionTableName, tableName));
				}
				else if ((localStatement.MetaData != null) && localStatement.MetaData.Tags.Contains("DAE.SourceTableName"))
					sourceTableName = MetaData.GetTag(localStatement.MetaData, "DAE.SourceTableName", String.Empty);
				
				CheckValidCatalogObjectName(plan, statement, tableName);

				node.Table = new Schema.BaseTableVar(Schema.Object.GetObjectID(localStatement.MetaData), tableName);
				node.Table.SessionObjectName = sessionTableName;
				node.Table.SessionID = plan.SessionID;
				node.Table.SourceTableName = sourceTableName;
				node.Table.IsDeletedTable = Boolean.Parse(MetaData.GetTag(localStatement.MetaData, "DAE.IsDeletedTable", "False"));
				node.Table.IsGenerated = localStatement.IsSession || (sourceTableName != null);
				node.Table.Owner = plan.User;
				node.Table.Library = node.Table.IsGenerated ? null : plan.CurrentLibrary;
				node.Table.MetaData = localStatement.MetaData;
				plan.PlanCatalog.Add(node.Table);
				try
				{
					if ((plan.ApplicationTransactionID != Guid.Empty) && (sourceTableName != null) && !plan.IsLoading())
					{
						ApplicationTransaction transaction = plan.GetApplicationTransaction();
						try
						{
							transaction.Device.AddTableMap(plan.ServerProcess, node.Table);
						}
						finally
						{
							Monitor.Exit(transaction);
						}
					}

					plan.PushCreationObject(node.Table);
					try
					{
						Schema.Object objectValue = null;
						if ((localStatement.DeviceName != null) && !plan.IsEngine)
							objectValue = ResolveCatalogIdentifier(plan, localStatement.DeviceName.Identifier);

						if (objectValue == null)
							if (node.Table.SessionObjectName != null)
								objectValue = plan.TempDevice;
							else
								objectValue = GetDefaultDevice(plan, true);
						
						if (!(objectValue is Schema.Device))
							throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, localStatement.DeviceName != null ? (Statement)localStatement.DeviceName : localStatement);
							
						plan.AttachDependency(objectValue);
						node.Table.Device = (Schema.Device)objectValue;
						plan.CheckDeviceReconcile(node.Table);
						
						if (localStatement.FromExpression == null)
						{
							node.Table.DataType = (Schema.ITableType)new Schema.TableType();

							Schema.TableVarColumn newTableVarColumn;
							foreach (ColumnDefinition column in localStatement.Columns)
							{
								newTableVarColumn = CompileTableVarColumnDefinition(plan, node.Table, column);
								node.Table.DataType.Columns.Add(newTableVarColumn.Column);
								node.Table.Columns.Add(newTableVarColumn);
							}
							
							plan.EnsureDeviceStarted(node.Table.Device);
							node.Table.Device.CheckSupported(plan, node.Table); // This call must be made before any attempt to compile keys for the table is made
		
							CompileCreateTableVarStatement(plan, localStatement, node, blockNode);
							
							if (node.Table.Keys.Count == 0)
								throw new CompilerException(CompilerException.Codes.KeyRequired, statement);

							node.Table.DetermineRemotable(plan.CatalogDeviceSession);
							tag = node.Table.GetMetaDataTag("DAE.IsDefaultRemotable");
							if (tag != Tag.None)
							{
								bool remotable = Boolean.Parse(tag.Value);
								node.Table.IsDefaultRemotable = node.Table.IsDefaultRemotable && remotable;
								if (!(node.Table.IsDefaultRemotable ^ remotable))
									node.Table.MetaData.Tags.Update("DAE.IsDefaultRemotable", node.Table.IsDefaultRemotable.ToString());
							}

							tag = node.Table.GetMetaDataTag("DAE.IsChangeRemotable");
							if (tag != Tag.None)
							{
								bool remotable = Boolean.Parse(tag.Value);
								node.Table.IsChangeRemotable = node.Table.IsChangeRemotable && remotable;
								if (!(node.Table.IsChangeRemotable ^ remotable))
									node.Table.MetaData.Tags.Update("DAE.IsChangeRemotable", node.Table.IsChangeRemotable.ToString());
							}

							tag = node.Table.GetMetaDataTag("DAE.IsValidateRemotable");
							if (tag != Tag.None)
							{
								bool remotable = Boolean.Parse(tag.Value);
								node.Table.IsValidateRemotable = node.Table.IsValidateRemotable && remotable;
								if (!(node.Table.IsValidateRemotable ^ remotable))
									node.Table.MetaData.Tags.Update("DAE.IsValidateRemotable", node.Table.IsValidateRemotable.ToString());
							}
						}
						else
						{
							plan.PopCreationObject();
							try
							{
								plan.Symbols.PopWindow();
								try
								{
									PlanNode fromNode = CompileExpression(plan, localStatement.FromExpression);
								
									if (!(fromNode.DataType is Schema.ITableType))
										throw new CompilerException(CompilerException.Codes.TableExpressionExpected, localStatement.FromExpression);
										
									fromNode = EnsureTableNode(plan, fromNode);
										
									node.Table.CopyTableVar((TableNode)fromNode);
									
									InsertNode insertNode = new InsertNode();
									insertNode.SetLineInfo(plan, localStatement.FromExpression.LineInfo);
										
									insertNode.Nodes.Add(fromNode);
									plan.PushStatementContext(new StatementContext(StatementType.Insert));
									try
									{
										insertNode.Nodes.Add(EmitBaseTableVarNode(plan, node.Table));
									}
									finally
									{
										plan.PopStatementContext();
									}
									
									insertNode.DetermineDataType(plan);
									insertNode.DetermineCharacteristics(plan);

									blockNode.Nodes.Add(insertNode);
								}
								finally
								{
									plan.Symbols.PushWindow(0);
								}
							}
							finally
							{
								plan.PushCreationObject(node.Table);
							}

							plan.EnsureDeviceStarted(node.Table.Device);
							node.Table.Device.CheckSupported(plan, node.Table);
		
							foreach (Schema.TableVarColumn column in node.Table.Columns)
								if (column.DataType is Schema.ScalarType)
									plan.AttachDependency((Schema.ScalarType)column.DataType);
							node.Table.DetermineRemotable(plan.CatalogDeviceSession);
						}
								
						return blockNode;
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
				catch
				{
					plan.PlanCatalog.SafeRemove(node.Table);
					throw;
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static PlanNode CompileCreateViewStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateViewStatement localStatement = (CreateViewStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateView);
			BlockNode blockNode = new BlockNode();
			blockNode.SetLineInfo(plan, statement.LineInfo);
			CreateViewNode node = new CreateViewNode();
			node.SetLineInfo(plan, statement.LineInfo);
			blockNode.Nodes.Add(node);

			// Generate the TableType for this table
			string viewName = Schema.Object.Qualify(localStatement.TableVarName, plan.CurrentLibrary.Name);
			string sessionViewName = null;
			string sourceTableName = null;
			if (localStatement.IsSession)
			{
				sessionViewName = viewName;
				if (plan.IsEngine)
					viewName = MetaData.GetTag(localStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
				else
					viewName = Schema.Object.NameFromGuid(Guid.NewGuid());
				CheckValidSessionObjectName(plan, statement, sessionViewName);
				plan.PlanSessionObjects.Add(new Schema.SessionObject(sessionViewName, viewName));
			}
			else if ((localStatement.MetaData != null) && localStatement.MetaData.Tags.Contains("DAE.SourceTableName"))
			{
				sourceTableName = MetaData.GetTag(localStatement.MetaData, "DAE.SourceTableName", String.Empty);
			}
			
			CheckValidCatalogObjectName(plan, statement, viewName);

			node.View = new Schema.DerivedTableVar(Schema.Object.GetObjectID(localStatement.MetaData), viewName);
			node.View.SessionObjectName = sessionViewName;
			node.View.SessionID = plan.SessionID;
			node.View.SourceTableName = sourceTableName;
			node.View.IsGenerated = localStatement.IsSession || (sourceTableName != null);
			node.View.Owner = plan.User;
			node.View.Library = node.View.IsGenerated ? null : plan.CurrentLibrary;
			node.View.MetaData = localStatement.MetaData;
			if (!plan.IsEngine) // if this is a repository, a view could be parameterized, because it will never be executed
				plan.Symbols.PushWindow(0); // make sure the view expression is evaluated in a private context
			try
			{
				if ((plan.ApplicationTransactionID != Guid.Empty) && (sourceTableName != null) && !plan.IsLoading())
				{
					ApplicationTransaction transaction = plan.GetApplicationTransaction();
					try
					{
						transaction.Device.AddTableMap(plan.ServerProcess, node.View);
					}
					finally
					{
						Monitor.Exit(transaction);
					}
				}

				plan.PushCreationObject(node.View);
				try
				{
					#if USEORIGINALEXPRESSION
					node.View.OriginalExpression = localStatement.Expression;
					#endif
					PlanNode planNode = CompileExpression(plan, localStatement.Expression);
					if (!(planNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableExpressionExpected, localStatement.Expression);
						
					planNode = EnsureTableNode(plan, planNode);

					node.View.CopyTableVar((TableNode)planNode, planNode is TableVarNode);
					
					// If we are in an A/T, or we are loading an A/T object
					if ((plan.ApplicationTransactionID != Guid.Empty) || node.View.IsATObject)
					{
						// Set the explicit bind for A/T variables resolved within the view expression
						ApplicationTransactionUtility.SetExplicitBind(planNode);
					}

					node.View.InvocationExpression = (Expression)planNode.EmitStatement(EmitMode.ForCopy);
					
					plan.PlanCatalog.Add(node.View);
					try
					{
						CompileCreateTableVarStatement(plan, localStatement, node, blockNode);
						
						node.View.CopyReferences((TableNode)planNode);
						if (planNode is TableVarNode)
							node.View.InheritMetaData(((TableNode)planNode).TableVar.MetaData);
						else
							node.View.MergeMetaData(((TableNode)planNode).TableVar.MetaData);
						node.View.MergeMetaData(localStatement.MetaData);
						node.View.DetermineRemotable(plan.CatalogDeviceSession);
						return blockNode;
					}
					catch
					{
						plan.PlanCatalog.SafeRemove(node.View);
						throw;
					}
				}
				finally
				{
					plan.PopCreationObject();
				}
			}
			finally
			{
				if (!plan.IsEngine)
					plan.Symbols.PopWindow();
			}
		}
		
		public static void ReinferViewReferences(Plan plan, Schema.DerivedTableVar view)
		{
			if (!plan.IsEngine && view.ShouldReinferReferences)
			{
				Schema.Objects saveReferences = new Schema.Objects();
				if (view.HasReferences())
				{
					foreach (Schema.ReferenceBase reference in view.References)
					{
						if (reference.IsDerived)
						{
							if (!saveReferences.Contains(reference))
							{
								saveReferences.Add(reference);
							}
						}
					}

					foreach (Schema.ReferenceBase reference in saveReferences)
					{
						view.References.SafeRemove(reference);
					}
				}

				try
				{
					bool isATObject = view.IsATObject;
					if (!isATObject)
						plan.PushGlobalContext();
					try
					{
						Plan localPlan = new Plan(plan.ServerProcess);
						try
						{
							localPlan.PushATCreationContext();
							try
							{
								localPlan.PushSecurityContext(new SecurityContext(view.Owner));
								try
								{
									#if USEORIGINALEXPRESSION
									PlanNode planNode = CompileExpression(localPlan, AView.OriginalExpression);
									#else
									PlanNode planNode = CompileExpression(localPlan, view.InvocationExpression);
									#endif
									localPlan.CheckCompiled();
									planNode = EnsureTableNode(localPlan, planNode);
									view.CopyReferences((TableNode)planNode);
									view.ShouldReinferReferences = false;
								}
								finally
								{
									localPlan.PopSecurityContext();
								}
							}
							finally
							{
								localPlan.PopATCreationContext();
							}
						}
						finally
						{
							localPlan.Dispose();
						}
					}
					finally
					{
						if (!isATObject)
							plan.PopGlobalContext();
					}
				}
				catch
				{
					view.References.Clear();
					
					foreach (Schema.ReferenceBase reference in saveReferences)
					{
						if (!view.References.Contains(reference))
							view.References.Add(reference);
					}
					
					throw;
				}
				
				view.SetShouldReinferReferences(plan.CatalogDeviceSession);
			}
		}

		public static Schema.Operator CompileRepresentationSelector(Plan plan, Schema.ScalarType scalarType, Schema.Representation representation, AccessorBlock selector)
		{
			// Selector
			// operator <type name>[.<representation name>](const <property name> : <property type>[, ...]) : <type>
			// If this is the default representation for a fromClass scalar type, use the following selector signature:
			// operator <type name>(const AValues : row) : <type>
			string operatorName = scalarType.Name;
			if (!representation.IsDefaultRepresentation)
				operatorName = Schema.Object.Qualify(representation.Name, operatorName);

			Schema.Operator operatorValue = new Schema.Operator(operatorName);
			operatorValue.IsGenerated = true;
			operatorValue.Generator = representation;
			operatorValue.ReturnDataType = scalarType;
			operatorValue.Library = scalarType.Library;
			operatorValue.Owner = scalarType.Owner;
			plan.PushCreationObject(operatorValue);
			try
			{
				plan.AttachDependency(scalarType);

				if (representation.IsDefaultRepresentation && scalarType.IsClassType)
				{
					operatorValue.Operands.Add(new Schema.Operand(operatorValue, "AValues", plan.DataTypes.SystemRow, Modifier.Const));
				}
				else
				{
					foreach (Schema.Property property in representation.Properties)
					{
						operatorValue.Operands.Add(new Schema.Operand(operatorValue, property.Name, property.DataType, Modifier.Const));
						plan.AttachDependencies(property.Dependencies);
					}
				}
				
				operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);

				if (selector.ClassDefinition != null)
				{
					if (!representation.IsDefaultSelector)
						plan.CheckRight(Schema.RightNames.HostImplementation);
					plan.CheckClassDependency(operatorValue.Library, selector.ClassDefinition);
					operatorValue.Block.ClassDefinition = selector.ClassDefinition;
				}
				else
				{
					Statement statement;
					if (selector.Expression != null)
						statement = new AssignmentStatement(new IdentifierExpression(Keywords.Result), selector.Expression);
					else
						statement = selector.Block;
						
					operatorValue.Block.BlockNode = BindOperatorBlock(plan, operatorValue, CompileOperatorBlock(plan, operatorValue, statement));
					operatorValue.DetermineRemotable(plan.CatalogDeviceSession);
					if (!(operatorValue.IsFunctional && operatorValue.IsDeterministic && operatorValue.IsRemotable))
						throw new CompilerException(CompilerException.Codes.InvalidSelector, selector, representation.Name, scalarType.Name);
				}

				return operatorValue;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompilePropertyReadAccessor(Plan plan, Schema.ScalarType scalarType, Schema.Representation representation, Schema.Property property, AccessorBlock readAccessor)
		{
			// Read Accessor
			// operator <type name>.Read<property name>(const value : <type>) : <property type>
			Schema.Operator operatorValue = new Schema.Operator(String.Format("{0}{1}{2}{3}", scalarType.Name, Keywords.Qualifier, ReadAccessorName, property.Name));
			operatorValue.IsGenerated = true;
			operatorValue.Generator = property;
			operatorValue.ReturnDataType = property.DataType;
			operatorValue.Operands.Add(new Schema.Operand(operatorValue, Keywords.Value, scalarType, Modifier.Const));
			operatorValue.Library = scalarType.Library;
			operatorValue.Owner = scalarType.Owner;
			operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);
			plan.PushCreationObject(operatorValue);
			try
			{
				plan.AttachDependency(scalarType);
				plan.AttachDependencies(property.Dependencies);
				if (readAccessor.ClassDefinition != null)
				{
					if (!property.IsDefaultReadAccessor)
						plan.CheckRight(Schema.RightNames.HostImplementation);
					plan.CheckClassDependency(operatorValue.Library, readAccessor.ClassDefinition);
					operatorValue.Block.ClassDefinition = readAccessor.ClassDefinition;
				}
				else
				{
					Statement statement;
					if (readAccessor.Expression != null)
						statement = new AssignmentStatement(new IdentifierExpression(Keywords.Result), readAccessor.Expression);
					else
						statement = readAccessor.Block;
						
					operatorValue.Block.BlockNode = BindOperatorBlock(plan, operatorValue, CompileOperatorBlock(plan, operatorValue, statement));
					operatorValue.DetermineRemotable(plan.CatalogDeviceSession);
					if (!(operatorValue.IsFunctional && operatorValue.IsDeterministic && operatorValue.IsRemotable))
						throw new CompilerException(CompilerException.Codes.InvalidReadAccessor, readAccessor, property.Name, representation.Name, scalarType.Name);
				}

				return operatorValue;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompilePropertyWriteAccessor(Plan plan, Schema.ScalarType scalarType, Schema.Representation representation, Schema.Property property, AccessorBlock writeAccessor)
		{
			// Write Accessor
			// operator <type name>.Write<property name>(const value : <type>, const <property name> : <property type>) : <type>
			Schema.Operator operatorValue = new Schema.Operator(String.Format("{0}{1}{2}{3}", scalarType.Name, Keywords.Qualifier, WriteAccessorName, property.Name));
			operatorValue.IsGenerated = true;
			operatorValue.Generator = property;
			operatorValue.ReturnDataType = scalarType;
			operatorValue.Operands.Add(new Schema.Operand(operatorValue, Keywords.Value, scalarType, Modifier.Const));
			operatorValue.Operands.Add(new Schema.Operand(operatorValue, property.Name, property.DataType, Modifier.Const));
			operatorValue.Library = scalarType.Library;
			operatorValue.Owner = scalarType.Owner;
			operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);
			plan.PushCreationObject(operatorValue);
			try
			{
				plan.AttachDependency(scalarType);
				plan.AttachDependencies(property.Dependencies);

				if (writeAccessor.ClassDefinition != null)
				{
					if (!property.IsDefaultWriteAccessor)
						plan.CheckRight(Schema.RightNames.HostImplementation);
					plan.CheckClassDependency(operatorValue.Library, writeAccessor.ClassDefinition);
					operatorValue.Block.ClassDefinition = writeAccessor.ClassDefinition;
				}
				else
				{
					Statement statement;
					if (writeAccessor.Expression != null)
						statement = new AssignmentStatement(new IdentifierExpression(Keywords.Result), writeAccessor.Expression);
					else
						statement = writeAccessor.Block;
						
					operatorValue.Block.BlockNode = BindOperatorBlock(plan, operatorValue, CompileOperatorBlock(plan, operatorValue, statement));
					operatorValue.DetermineRemotable(plan.CatalogDeviceSession);
					if (!(operatorValue.IsDeterministic && operatorValue.IsRemotable))
						throw new CompilerException(CompilerException.Codes.InvalidWriteAccessor, writeAccessor, property.Name, representation.Name, scalarType.Name);
				}

				return operatorValue;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Property CompileProperty(Plan plan, Schema.ScalarType scalarType, Schema.Representation representation, PropertyDefinition propertyDefinition)
		{
			Schema.Property property = new Schema.Property(Schema.Object.GetObjectID(propertyDefinition.MetaData), propertyDefinition.PropertyName);
			plan.PushCreationObject(property);
			try
			{
				property.MergeMetaData(propertyDefinition.MetaData);
				property.DataType = CompileTypeSpecifier(plan, propertyDefinition.PropertyType);
				representation.Properties.Add(property);
				return property;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static void CompilePropertyAccessors(Plan plan, Schema.ScalarType scalarType, Schema.Objects operators, Schema.Representation representation, Schema.Property property, PropertyDefinition propertyDefinition)
		{
			plan.PushCreationObject(property);
			try
			{
				AccessorBlock readAccessorBlock = propertyDefinition.ReadAccessorBlock;

				// Build a default read accessor for the property
				if (readAccessorBlock == null)
				{
					if (!scalarType.IsClassType)
					{
						if (!representation.IsDefaultSelector)
							throw new CompilerException(CompilerException.Codes.DefaultReadAccessorCannotBeProvided, propertyDefinition, property.Name, representation.Name, scalarType.Name);
						
						property.IsDefaultReadAccessor = true;
						readAccessorBlock = new AccessorBlock();
						if (!scalarType.IsCompound)
							readAccessorBlock.ClassDefinition = DefaultReadAccessor();
						else
							readAccessorBlock.ClassDefinition = DefaultCompoundReadAccessor(property.Name);
					}
				}

				// Compile the read accessor
				if (plan.InLoadingContext())
				{
					property.LoadReadAccessorID();
					property.LoadDependencies(plan.CatalogDeviceSession);
				}
				else
				{
					if (readAccessorBlock != null)
					{
						property.ReadAccessor = CompilePropertyReadAccessor(plan, scalarType, representation, property, readAccessorBlock);
						plan.PlanCatalog.Add(property.ReadAccessor);
						plan.AttachDependencies(property.ReadAccessor.Dependencies);
						operators.Add(property.ReadAccessor);
						plan.Catalog.OperatorResolutionCache.Clear(property.ReadAccessor.OperatorName);
					}
				}

				AccessorBlock writeAccessorBlock = propertyDefinition.WriteAccessorBlock;

				// Build a default write accessor for the property
				if (writeAccessorBlock == null)
				{
					if (!scalarType.IsClassType)
					{
						if (!representation.IsDefaultSelector)
							throw new CompilerException(CompilerException.Codes.DefaultWriteAccessorCannotBeProvided, propertyDefinition, property.Name, representation.Name, scalarType.Name);
						
						property.IsDefaultWriteAccessor = true;
						writeAccessorBlock = new AccessorBlock();
						if (!scalarType.IsCompound)
							writeAccessorBlock.ClassDefinition = DefaultWriteAccessor();
						else
							writeAccessorBlock.ClassDefinition = DefaultCompoundWriteAccessor(property.Name);
					}
				}
				
				if (plan.InLoadingContext())
				{
					property.LoadWriteAccessorID();
				}
				else
				{
					if (writeAccessorBlock != null)
					{
						property.WriteAccessor = CompilePropertyWriteAccessor(plan, scalarType, representation, property, writeAccessorBlock);
						plan.PlanCatalog.Add(property.WriteAccessor);
						plan.AttachDependencies(property.WriteAccessor.Dependencies);
						operators.Add(property.WriteAccessor);
						plan.Catalog.OperatorResolutionCache.Clear(property.WriteAccessor.OperatorName);
					}
				}

				property.RemoveDependency(property.Representation.ScalarType); // Remove the dependencies for native types to prevent recursion.
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Representation CompileRepresentation(Plan plan, Schema.ScalarType scalarType, Schema.Objects operators, RepresentationDefinition definition)
		{
			Schema.Representation representation = new Schema.Representation(Schema.Object.GetObjectID(definition.MetaData), definition.RepresentationName);
			representation.IsGenerated = definition.IsGenerated;
			if (representation.IsGenerated)
				representation.Generator = scalarType;
			representation.Library = plan.CurrentLibrary;
			representation.IsDefaultRepresentation = Schema.Object.NamesEqual(scalarType.Name, representation.Name);
			plan.PushCreationObject(representation);
			try
			{
				representation.MergeMetaData(definition.MetaData);
				representation.LoadIsGenerated();
				representation.LoadGeneratorID();
				scalarType.Representations.Add(representation);
				try
				{
					foreach (PropertyDefinition propertyDefinition in definition.Properties)
						CompileProperty(plan, scalarType, representation, propertyDefinition);
						
					AccessorBlock selectorBlock = definition.SelectorAccessorBlock;
						
					// Build a default selector for the representation
					if (selectorBlock == null)
					{
						if (scalarType.IsClassType)
						{
							if (representation.IsDefaultRepresentation)
							{
								selectorBlock = new AccessorBlock();
								selectorBlock.ClassDefinition = DefaultObjectSelector(scalarType.NativeType.FullName);
								representation.IsDefaultSelector = true;
							}
						}
						else 
						{
							selectorBlock = new AccessorBlock();
							if (scalarType.IsDefaultConveyor)
								throw new CompilerException(CompilerException.Codes.MultipleSystemProvidedRepresentations, definition, representation.Name, scalarType.Name);
							
							if ((representation.Properties.Count == 1) && (representation.Properties[0].DataType is Schema.ScalarType) && !((Schema.ScalarType)representation.Properties[0].DataType).IsCompound)
							{
								selectorBlock.ClassDefinition = DefaultSelector();
							
								// Use the native representation of the single simple scalar property
								scalarType.ClassDefinition = (ClassDefinition)((Schema.ScalarType)representation.Properties[0].DataType).ClassDefinition.Clone();
								scalarType.NativeType = ((Schema.ScalarType)representation.Properties[0].DataType).NativeType;
							}
							else
							{
								if (scalarType.ClassDefinition != null)
									throw new CompilerException(CompilerException.Codes.InvalidConveyorForCompoundScalar, definition, representation.Name, scalarType.Name);

								selectorBlock.ClassDefinition = DefaultCompoundSelector();
								scalarType.IsCompound = true;
							
								// Compile the row type for the native representation
								scalarType.CompoundRowType = new Schema.RowType();
								foreach (Schema.Property property in representation.Properties)
									scalarType.CompoundRowType.Columns.Add(new Schema.Column(property.Name, property.DataType));
							}

							representation.IsDefaultSelector = true;
							scalarType.IsDefaultConveyor = true;
						}
					}
					
					if (plan.InLoadingContext())
					{
						representation.LoadSelectorID();
						representation.LoadDependencies(plan.CatalogDeviceSession);
					}
					else
					{
						if (selectorBlock != null)
						{
							representation.Selector = CompileRepresentationSelector(plan, scalarType, representation, selectorBlock);
							plan.PlanCatalog.Add(representation.Selector);
							plan.AttachDependencies(representation.Selector.Dependencies);
							operators.Add(representation.Selector);
							plan.Catalog.OperatorResolutionCache.Clear(representation.Selector.OperatorName);
						}
					}

					representation.RemoveDependency(scalarType);

					foreach (PropertyDefinition propertyDefinition in definition.Properties)
						CompilePropertyAccessors(plan, scalarType, operators, representation, representation.Properties[propertyDefinition.PropertyName], propertyDefinition);
						
					return representation;
				}
				catch
				{
					scalarType.Representations.Remove(representation);
					throw;
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		protected static ClassDefinition DefaultSelector()
		{
			return new ClassDefinition("System.ValidatingScalarSelectorNode");
		}
		
		protected static ClassDefinition DefaultCompoundSelector()
		{
			return new ClassDefinition("System.CompoundScalarSelectorNode");
		}

		protected static ClassDefinition DefaultObjectSelector(string className)
		{
			return new ClassDefinition("System.ObjectSelectorNode", new ClassAttributeDefinition[] { new ClassAttributeDefinition("ClassName", className) });
		}
		
		protected static ClassDefinition DefaultReadAccessor()
		{
			return new ClassDefinition("System.ScalarReadAccessorNode");
		}
		
		protected static ClassDefinition DefaultCompoundReadAccessor(string propertyName)
		{
			return new ClassDefinition("System.CompoundScalarReadAccessorNode", new ClassAttributeDefinition[]{new ClassAttributeDefinition("PropertyName", propertyName)});
		}

		protected static ClassDefinition DefaultObjectPropertyReadNode(string propertyName)
		{
			return new ClassDefinition("System.ObjectPropertyReadNode", new ClassAttributeDefinition[] { new ClassAttributeDefinition("PropertyName", propertyName) });
		}
		
		protected static ClassDefinition DefaultWriteAccessor()
		{
			return new ClassDefinition("System.ValidatingScalarWriteAccessorNode");
		}
		
		protected static ClassDefinition DefaultCompoundWriteAccessor(string propertyName)
		{
			return new ClassDefinition("System.CompoundScalarWriteAccessorNode", new ClassAttributeDefinition[]{new ClassAttributeDefinition("PropertyName", propertyName)});
		}

		protected static ClassDefinition DefaultObjectPropertyWriteNode(string propertyName)
		{
			return new ClassDefinition("System.ObjectPropertyWriteNode", new ClassAttributeDefinition[] { new ClassAttributeDefinition("PropertyName", propertyName) });
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
		
		public static Schema.Operator CompileSpecialOperator(Plan plan, Schema.ScalarType scalarType)
		{
			// create an operator of the form <scalar type name qualifier>.IsSpecial(AValue : <scalar type name>) : boolean
			// Compile IsSpecial operator (result := Parent.IsSpecial(AValue) or AValue = Special1Value or AValue = Special2Value...)
			Schema.Operator operatorValue = new Schema.Operator(Schema.Object.Qualify(IsSpecialOperatorName, Schema.Object.Qualifier(scalarType.Name)));
			operatorValue.IsGenerated = true;
			operatorValue.Generator = scalarType;
			operatorValue.Operands.Add(new Schema.Operand(operatorValue, "AValue", scalarType, Modifier.Const));
			operatorValue.ReturnDataType = plan.DataTypes.SystemBoolean;
			operatorValue.Owner = scalarType.Owner;
			operatorValue.Library = scalarType.Library;
			operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);
			plan.PushCreationObject(operatorValue);
			try
			{
				plan.AttachDependency(plan.DataTypes.SystemBoolean);
				plan.AttachDependency(scalarType);
				
				plan.Symbols.Push(new Symbol("AValue", scalarType));
				try
				{
					plan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));
					try
					{
						PlanNode anySpecialNode = null;
						bool attachOr = false;
						#if USETYPEINHERITANCE
						foreach (Schema.IScalarType parentType in AScalarType.ParentTypes)
						{
							PlanNode isSpecialNode = 
								Compiler.EmitCallNode
								(
									APlan, 
									CIsSpecialOperatorName, 
									new PlanNode[]{new StackReferenceNode("AValue", parentType, 1)}
								);
								
							if (anySpecialNode != null)
							{
								anySpecialNode = Compiler.EmitBinaryNode(APlan, anySpecialNode, Instructions.Or, isSpecialNode);
								attachOr = true;
							}
							else
								anySpecialNode = isSpecialNode;
						}
						#endif

						if (anySpecialNode == null)
							anySpecialNode = new ValueNode(plan.DataTypes.SystemBoolean, false);
							
						foreach (Schema.Special special in scalarType.Specials)
						{
							anySpecialNode = Compiler.EmitBinaryNode(plan, anySpecialNode, Instructions.Or, special.Comparer.Block.BlockNode.Nodes[1]);
							attachOr = true;
						}

						// make sure that the scalar type includes a dependency on the boolean or operator					
						if (attachOr)
							scalarType.AddDependency(ResolveOperator(plan, Instructions.Or, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(plan.DataTypes.SystemBoolean, Modifier.Const), new Schema.SignatureElement(plan.DataTypes.SystemBoolean, Modifier.Const)}), false));
							
						operatorValue.Block.BlockNode = new AssignmentNode(new StackReferenceNode(Keywords.Result, operatorValue.ReturnDataType, 0), anySpecialNode);
						operatorValue.Block.BlockNode.Line = 1;
						operatorValue.Block.BlockNode.LinePos = 1;
						operatorValue.Block.BlockNode = OptimizeNode(plan, operatorValue.Block.BlockNode);

						// Attach the dependencies for each special comparer to the IsSpecial operator
						foreach (Schema.Special newSpecial in scalarType.Specials)
							if (newSpecial.Comparer.HasDependencies())
								operatorValue.AddDependencies(newSpecial.Comparer.Dependencies);
									
						operatorValue.DetermineRemotable(plan.CatalogDeviceSession);

						return operatorValue;
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompileSpecialSelector(Plan plan, Schema.ScalarType scalarType, Schema.Special special, string specialName, PlanNode valueNode)
		{
			// Create an operator of the form ScalarTypeNameSpecialName() : ScalarType as a selector for the given special
			Schema.Operator operatorValue = new Schema.Operator(String.Format("{0}{1}", scalarType.Name, specialName));
			operatorValue.IsGenerated = true;
			operatorValue.Generator = special;
			operatorValue.ReturnDataType = scalarType;
			operatorValue.Owner = scalarType.Owner;
			operatorValue.Library = scalarType.Library;
			operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);
			plan.PushCreationObject(operatorValue);
			try
			{
				plan.AttachDependency(scalarType);

				plan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));
				try
				{
					operatorValue.Block.BlockNode = new AssignmentNode(new StackReferenceNode(Keywords.Result, operatorValue.ReturnDataType, 0), valueNode);
					operatorValue.Block.BlockNode.Line = 1;
					operatorValue.Block.BlockNode.LinePos = 1;
					operatorValue.Block.BlockNode = OptimizeNode(plan, operatorValue.Block.BlockNode);
					return operatorValue;
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Operator CompileSpecialComparer(Plan plan, Schema.ScalarType scalarType, Schema.Special special, string specialName, PlanNode valueNode)
		{
			// Create an operator of the form <scalar type name qualifier>.Is<special name>(const AValue : <scalar type name>) : Boolean as a comparison operator for the given special
			Schema.Operator operatorValue = new Schema.Operator(Schema.Object.Qualify(String.Format("{0}{1}", IsSpecialComparerPrefix, specialName), Schema.Object.Qualifier(scalarType.Name)));
			operatorValue.IsGenerated = true;
			operatorValue.Generator = special;
			plan.PushCreationObject(operatorValue);
			try
			{
				operatorValue.Operands.Add(new Schema.Operand(operatorValue, "AValue", scalarType, Modifier.Const));
				operatorValue.ReturnDataType = plan.DataTypes.SystemBoolean;
				operatorValue.Owner = scalarType.Owner;
				operatorValue.Library = scalarType.Library;
				operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);
	
				plan.AttachDependency(plan.DataTypes.SystemBoolean);
				plan.AttachDependency(scalarType);

				plan.Symbols.Push(new Symbol("AValue", scalarType));
				try
				{
					plan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));
					try
					{
						PlanNode planNode = Compiler.EmitBinaryNode(plan, new StackReferenceNode("AValue", scalarType, 1), Instructions.Equal, valueNode);
						operatorValue.Block.BlockNode = new AssignmentNode(new StackReferenceNode(Keywords.Result, plan.DataTypes.SystemBoolean, 0), planNode);
						operatorValue.Block.BlockNode.Line = 1;
						operatorValue.Block.BlockNode.LinePos = 1;
						operatorValue.Block.BlockNode = OptimizeNode(plan, operatorValue.Block.BlockNode);
						return operatorValue;
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static void CompileComparisonOperator(Plan plan, Schema.ScalarType scalarType)
		{
			// Builds an equality operator for the type
			// if applicable, builds a comparison operator for the type
			// If the type is simple
				// Build an equality operator based on the equality operator for the scalar simple property of the system representation
				// Attempt to build a comparison operator based on the comparison operator for the scalar simple property of the system representation
			// Otherwise
				// Build an equality operator using the CompoundScalarEqualNode
			if (!scalarType.IsCompound)
			{
				Schema.ScalarType componentType = (Schema.ScalarType)FindSystemRepresentation(scalarType).Properties[0].DataType;
				PlanNode[] arguments = new PlanNode[]{new ValueNode(componentType, null), new ValueNode(componentType, null)};

				PlanNode planNode = Compiler.EmitCallNode(plan, Instructions.Equal, arguments, false, true);
				if (planNode != null)
				{
					Schema.Operator componentOperator = ((InstructionNodeBase)planNode).Operator;
					Schema.Operator operatorValue = new Schema.Operator(Schema.Object.Qualify(Instructions.Equal, scalarType.Library.Name));
					operatorValue.IsGenerated = true;
					operatorValue.Generator = scalarType;
					operatorValue.IsBuiltin = true;
					operatorValue.MetaData = new D4.MetaData();
					plan.PushCreationObject(operatorValue);
					try
					{
						operatorValue.Operands.Add(new Schema.Operand(operatorValue, "ALeftValue", scalarType, Modifier.Const));
						operatorValue.Operands.Add(new Schema.Operand(operatorValue, "ARightValue", scalarType, Modifier.Const));
						operatorValue.ReturnDataType = plan.DataTypes.SystemBoolean;
						operatorValue.Owner = scalarType.Owner;
						operatorValue.Library = scalarType.Library;
						operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);

						plan.AttachDependency(plan.DataTypes.SystemBoolean);
						plan.AttachDependency(scalarType);
						
						if (componentOperator.Block.ClassDefinition != null)
							operatorValue.Block.ClassDefinition = (ClassDefinition)componentOperator.Block.ClassDefinition;
						else
							operatorValue.Block.BlockNode = componentOperator.Block.BlockNode;
						plan.PlanCatalog.Add(operatorValue);
						plan.Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);
						scalarType.EqualityOperator = operatorValue;
					}
					finally
					{
						plan.PopCreationObject();
					}
				}

				planNode = Compiler.EmitCallNode(plan, Instructions.Compare, arguments, false, true);
				if (planNode != null)
				{
					Schema.Operator componentOperator = ((InstructionNodeBase)planNode).Operator;
					Schema.Operator operatorValue = new Schema.Operator(Schema.Object.Qualify(Instructions.Compare, scalarType.Library.Name));
					operatorValue.IsGenerated = true;
					operatorValue.Generator = scalarType;
					operatorValue.IsBuiltin = true;
					operatorValue.MetaData = new D4.MetaData();
					plan.PushCreationObject(operatorValue);
					try
					{
						operatorValue.Operands.Add(new Schema.Operand(operatorValue, "ALeftValue", scalarType, Modifier.Const));
						operatorValue.Operands.Add(new Schema.Operand(operatorValue, "ARightValue", scalarType, Modifier.Const));
						operatorValue.ReturnDataType = plan.DataTypes.SystemInteger;
						operatorValue.Owner = scalarType.Owner;
						operatorValue.Library = scalarType.Library;
						operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);

						plan.AttachDependency(plan.DataTypes.SystemInteger);
						plan.AttachDependency(scalarType);
						
						if (componentOperator.Block.ClassDefinition != null)
							operatorValue.Block.ClassDefinition = (ClassDefinition)componentOperator.Block.ClassDefinition;
						else
							operatorValue.Block.BlockNode = componentOperator.Block.BlockNode;
						plan.PlanCatalog.Add(operatorValue);
						plan.Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);
						scalarType.ComparisonOperator = operatorValue;
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
			}
			else
			{
				Schema.Operator operatorValue = new Schema.Operator(Schema.Object.Qualify(Instructions.Equal, scalarType.Library.Name));
				operatorValue.IsGenerated = true;
				operatorValue.Generator = scalarType;
				operatorValue.IsBuiltin = true;
				operatorValue.MetaData = new D4.MetaData();
				plan.PushCreationObject(operatorValue);
				try
				{
					operatorValue.Operands.Add(new Schema.Operand(operatorValue, "ALeftValue", scalarType, Modifier.Const));
					operatorValue.Operands.Add(new Schema.Operand(operatorValue, "ARightValue", scalarType, Modifier.Const));
					operatorValue.ReturnDataType = plan.DataTypes.SystemBoolean;
					operatorValue.Owner = scalarType.Owner;
					operatorValue.Library = scalarType.Library;
					operatorValue.Locator = new DebugLocator(DebugLocator.OperatorLocator(operatorValue.DisplayName), 0, 1);

					plan.AttachDependency(plan.DataTypes.SystemBoolean);
					plan.AttachDependency(scalarType);
					
					operatorValue.Block.ClassDefinition = new ClassDefinition("System.CompoundScalarEqualNode");
					plan.PlanCatalog.Add(operatorValue);
					plan.Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);
					scalarType.EqualityOperator = operatorValue;
				}
				finally
				{
					plan.PopCreationObject();
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
					LOperator.Locator = new DebugLocator(DebugLocator.OperatorLocator(LOperator.DisplayName), 0, 1);
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
		
		public static Schema.TableVarColumnConstraint CompileTableVarColumnConstraint(Plan plan, Schema.TableVar tableVar, Schema.TableVarColumn column, ConstraintDefinition constraintDefinition)
		{
			Schema.TableVarColumnConstraint constraint = 
				new Schema.TableVarColumnConstraint
				(
					Schema.Object.GetObjectID(constraintDefinition.MetaData),
					constraintDefinition.ConstraintName
				);
			constraint.ConstraintType = Schema.ConstraintType.Column;
			constraint.Library = tableVar.Library == null ? null : plan.CurrentLibrary;
			CompileScalarConstraint(plan, constraint, column.DataType, constraintDefinition);
			constraint.IsGenerated = constraint.IsGenerated || tableVar.IsGenerated;
			return constraint;
		}
		
		public static Schema.ScalarTypeConstraint CompileScalarTypeConstraint(Plan plan, Schema.ScalarType scalarType, ConstraintDefinition constraintDefinition)
		{
			Schema.ScalarTypeConstraint constraint = 
				new Schema.ScalarTypeConstraint
				(
					Schema.Object.GetObjectID(constraintDefinition.MetaData),
					constraintDefinition.ConstraintName
				);
			constraint.ConstraintType = Schema.ConstraintType.ScalarType;
			constraint.Library = scalarType.Library == null ? null : plan.CurrentLibrary;
			CompileScalarConstraint(plan, constraint, scalarType, constraintDefinition);
			return constraint;
		}
		
		private static Schema.Constraint CompileScalarConstraint(Plan plan, Schema.SimpleConstraint constraint, Schema.IDataType dataType, ConstraintDefinition constraintDefinition)
		{
			constraint.IsGenerated = constraintDefinition.IsGenerated;
			constraint.MergeMetaData(constraintDefinition.MetaData);
			constraint.LoadIsGenerated();
			constraint.LoadGeneratorID();
			constraint.Enforced = GetEnforced(plan, constraint.MetaData);
			plan.PushCreationObject(constraint);
			try
			{
				plan.Symbols.Push(new Symbol(Keywords.Value, dataType));
				try
				{
					constraint.Node = CompileBooleanExpression(plan, constraintDefinition.Expression);
					if (!(constraint.Node.IsFunctional && constraint.Node.IsDeterministic))
						throw new CompilerException(CompilerException.Codes.InvalidConstraintExpression, constraintDefinition.Expression);
					constraint.Node = OptimizeNode(plan, constraint.Node);
						
					constraint.DetermineRemotable(plan.CatalogDeviceSession);
					if (!constraint.IsRemotable)
						throw new CompilerException(CompilerException.Codes.NonRemotableConstraintExpression, constraintDefinition.Expression);
						
					string customMessage = constraint.GetCustomMessage(Schema.Transition.Insert);
					if (!String.IsNullOrEmpty(customMessage))
					{
						try
						{
							PlanNode violationMessageNode = CompileTypedExpression(plan, new D4.Parser().ParseExpression(customMessage), plan.DataTypes.SystemString);
							violationMessageNode = OptimizeNode(plan, violationMessageNode);
							constraint.ViolationMessageNode = violationMessageNode;
						}
						catch (Exception exception)
						{
							throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, constraintDefinition, exception, constraint.Name);
						}
					}
					
					constraint.DetermineRemotable(plan.CatalogDeviceSession);
					if (!constraint.IsRemotable)
						throw new CompilerException(CompilerException.Codes.NonRemotableCustomConstraintMessage, constraintDefinition);
						
					return constraint;
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.Special CompileSpecial(Plan plan, Schema.ScalarType scalarType, SpecialDefinition specialDefinition)
		{
			Schema.Special special = new Schema.Special(Schema.Object.GetObjectID(specialDefinition.MetaData), specialDefinition.Name);
			special.Library = plan.CurrentLibrary;
			special.MergeMetaData(specialDefinition.MetaData);
			plan.PushCreationObject(special);
			try
			{
				special.IsGenerated = specialDefinition.IsGenerated;
				special.LoadIsGenerated();
				special.LoadGeneratorID();
				special.ValueNode = CompileTypedExpression(plan, specialDefinition.Value, scalarType);
				if (!(special.ValueNode.IsFunctional && special.ValueNode.IsDeterministic))
					throw new CompilerException(CompilerException.Codes.InvalidSpecialExpression, specialDefinition.Value);
				special.ValueNode = OptimizeNode(plan, special.ValueNode);
				if (!plan.InLoadingContext())
				{
					special.Selector = CompileSpecialSelector(plan, scalarType, special, specialDefinition.Name, special.ValueNode);
					if (special.HasDependencies())
						special.Selector.AddDependencies(special.Dependencies);
					special.Selector.DetermineRemotable(plan.CatalogDeviceSession);
					plan.PlanCatalog.Add(special.Selector);
					plan.AttachDependencies(special.Selector.Dependencies);
					plan.Catalog.OperatorResolutionCache.Clear(special.Selector.OperatorName);
				}
				else
				{
					special.LoadSelectorID();
					special.LoadDependencies(plan.CatalogDeviceSession);
				}
				
				if (!plan.InLoadingContext())
				{
					special.Comparer = CompileSpecialComparer(plan, scalarType, special, specialDefinition.Name, special.ValueNode);
					if (special.HasDependencies())
						special.Comparer.AddDependencies(special.Dependencies);
					special.Comparer.DetermineRemotable(plan.CatalogDeviceSession);
					plan.PlanCatalog.Add(special.Comparer);
					plan.AttachDependencies(special.Comparer.Dependencies);
					plan.Catalog.OperatorResolutionCache.Clear(special.Comparer.OperatorName);
				}
				else
				{
					special.LoadComparerID();
				}

				special.RemoveDependency(scalarType);
				special.DetermineRemotable(plan.CatalogDeviceSession);
				
				return special;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}

		// The like representation for a type is the representataion with a single
		// property of the like type. Clearly, if a type is not defined to be like
		// another type, then it has no like representation.
		public static Schema.Representation FindLikeRepresentation(Schema.ScalarType scalarType)
		{
			// If this type is like another type, find the representation with a single component of the like type
			if (scalarType.LikeType != null)
				foreach (Schema.Representation representation in scalarType.Representations)
					if ((representation.Properties.Count == 1) && representation.Properties[0].DataType.Equals(scalarType.LikeType))	
						return representation;
			return null;
		}

		// The system representation for a type is the representation for which the 
		// selector is system-provided. Clearly, if the conveyor for the type is not
		// system-provided, then it has no system representation.		
		public static Schema.Representation FindSystemRepresentation(Schema.ScalarType scalarType)
		{
			// If the native representation for this type is system-provided, find the system-provided representation of this type
			if (scalarType.IsDefaultConveyor)
				foreach (Schema.Representation representation in scalarType.Representations)
					if (representation.IsDefaultSelector)
						return representation;
			return null;
		}
		
		public static Schema.Representation FindDefaultRepresentation(Schema.ScalarType scalarType)
		{
			// The default representation is the representation with the same name as the scalar type
			foreach (Schema.Representation representation in scalarType.Representations)
				if (representation.IsDefaultRepresentation)
					return representation;
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
		public static PlanNode CompileCreateScalarTypeStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			plan.CheckRight(Schema.RightNames.CreateType);

			CreateScalarTypeStatement localStatement = (CreateScalarTypeStatement)statement;
			string scalarTypeName = Schema.Object.Qualify(localStatement.ScalarTypeName, plan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(plan, statement, scalarTypeName);

			CreateScalarTypeNode node = new CreateScalarTypeNode();
			node.SetLineInfo(plan, localStatement.LineInfo);
			BlockNode blockNode = new BlockNode();
			blockNode.SetLineInfo(plan, localStatement.LineInfo);
			blockNode.Nodes.Add(node);

			plan.Symbols.PushWindow(0); // make sure the create scalar type statement is evaluated in a private context
			try
			{
				// Generate the Schema.IScalarType for this scalar type
				node.ScalarType = new Schema.ScalarType(Schema.Object.GetObjectID(localStatement.MetaData), scalarTypeName);
				Schema.Objects operators = new Schema.Objects();
				node.ScalarType.Owner = plan.User;
				node.ScalarType.Library = plan.CurrentLibrary;
				if (String.Compare(scalarTypeName, Schema.DataTypes.SystemScalarName) == 0)
					node.ScalarType.IsGeneric = true;
				plan.PlanCatalog.Add(node.ScalarType);
				try
				{
					plan.PushCreationObject(node.ScalarType);
					try
					{
						if (localStatement.ParentScalarTypes.Count > 0)
							foreach (ScalarTypeNameDefinition parentScalarType in localStatement.ParentScalarTypes)
							{
								Schema.ScalarType parentType = (Schema.ScalarType)Compiler.CompileScalarTypeSpecifier(plan, new ScalarTypeSpecifier(parentScalarType.ScalarTypeName));
								node.ScalarType.ParentTypes.Add(parentType);
								node.ScalarType.InheritMetaData(parentType.MetaData);
								plan.AttachDependency(parentType);
							}
						//else
						//{
						//	// Check first to see if the catalog contains the alpha data type, if it does not, the system is starting up and we are creating the alpha scalar type, so it is allowed to be parentless.
						//	if (APlan.Catalog.Contains(Schema.DataTypes.CSystemScalar))
						//	{
						//		Schema.ScalarType parentType = APlan.DataTypes.SystemScalar;
						//		node.ScalarType.ParentTypes.Add(parentType);
						//		node.ScalarType.InheritMetaData(parentType.MetaData);
						//		APlan.AttachDependency(parentType);
						//	}
						//}

						if (localStatement.FromClassDefinition != null)
						{
							plan.CheckRight(Schema.RightNames.HostImplementation);
							plan.CheckClassDependency(node.ScalarType.Library, localStatement.FromClassDefinition);
							Type type = plan.CreateType(localStatement.FromClassDefinition);
							node.ScalarType.NativeType = type;
							node.ScalarType.IsDisposable = type.GetInterface("IDisposable") != null;
							node.ScalarType.FromClassDefinition = localStatement.FromClassDefinition;
							node.ScalarType.IsClassType = true;

							// For class types, if there is no conveyor class specified, use the parent conveyor class.
							if (localStatement.ClassDefinition == null)
							{
								ClassDefinition parentClassDefinition = null;
								foreach (Schema.ScalarType parentType in node.ScalarType.ParentTypes)
								{
									if (parentType.ClassDefinition != null)
									{
										if (parentClassDefinition != null)
											throw new CompilerException(CompilerException.Codes.AmbiguousConveyor, localStatement, node.ScalarType.Name, parentType.ClassDefinition.ClassName, parentClassDefinition.ClassName);

										parentClassDefinition = parentType.ClassDefinition;
									}
								}

								if (parentClassDefinition != null)
									localStatement.ClassDefinition = (ClassDefinition)parentClassDefinition.Clone();
							}
						}

						if (localStatement.LikeScalarTypeName != String.Empty)
						{
							node.ScalarType.LikeType = (Schema.ScalarType)CompileScalarTypeSpecifier(plan, new ScalarTypeSpecifier(localStatement.LikeScalarTypeName));

							if (!plan.InLoadingContext())
							{
								Schema.Representation likeTypeLikeRepresentation = FindLikeRepresentation(node.ScalarType.LikeType);
								RepresentationDefinition representationDefinition;
								PropertyDefinition propertyDefinition;
								bool hasLikeRepresentation = false;
								int insertIndex = 0;
								for (int index = 0; index < node.ScalarType.LikeType.Representations.Count; index++)
								{
									Schema.Representation representation = node.ScalarType.LikeType.Representations[index];
									if (representation != likeTypeLikeRepresentation)
									{
										// if this representation will be the like representation for the new type, it will also be the system-provided representation so omit the class definitions for the selector and accessors
										bool isLikeRepresentation = (representation.Properties.Count == 1) && representation.Properties[0].DataType.Equals(node.ScalarType.LikeType);
										
										// Only the like representation and native accessor representations should be liked
										if (isLikeRepresentation || representation.IsNativeAccessorRepresentation(false)) 
										{
											representationDefinition = new RepresentationDefinition(String.Compare(representation.Name, Schema.Object.Unqualify(node.ScalarType.LikeType.Name)) == 0 ? Schema.Object.Unqualify(node.ScalarType.Name) : representation.Name);
											representationDefinition.IsGenerated = true;
											if (!localStatement.Representations.Contains(representationDefinition.RepresentationName))
											{
												if (isLikeRepresentation)
													hasLikeRepresentation = true;
												if (!isLikeRepresentation)
													representationDefinition.SelectorAccessorBlock = representation.Selector.Block.EmitAccessorBlock(EmitMode.ForCopy);
												foreach (Schema.Property property in representation.Properties)
												{
													propertyDefinition = new PropertyDefinition(property.Name, property.DataType.EmitSpecifier(EmitMode.ForCopy));
													if (!isLikeRepresentation)
													{
														propertyDefinition.ReadAccessorBlock = property.ReadAccessor.Block.EmitAccessorBlock(EmitMode.ForCopy);
														propertyDefinition.WriteAccessorBlock = property.WriteAccessor.Block.EmitAccessorBlock(EmitMode.ForCopy);
													}
													representationDefinition.Properties.Add(propertyDefinition);
												}
												localStatement.Representations.Insert(insertIndex, representationDefinition);
												insertIndex++;
											}
										}
									}
								}
								
								if (!hasLikeRepresentation)
								{
									string representationName = Schema.Object.Unqualify(node.ScalarType.Name);
									if (localStatement.Representations.Contains(representationName))
										representationName = String.Format("As{0}", Schema.Object.Unqualify(node.ScalarType.LikeType.Name)); 
									representationDefinition = new RepresentationDefinition(representationName);
									representationDefinition.IsGenerated = true;
									if (!localStatement.Representations.Contains(representationDefinition.RepresentationName))
									{
										representationDefinition.Properties.Add(new PropertyDefinition(representationDefinition.RepresentationName, new ScalarTypeSpecifier(node.ScalarType.LikeType.Name)));
										localStatement.Representations.Insert(0, representationDefinition);
									}
								}
								
								// Default
								if ((node.ScalarType.LikeType.Default != null) && (localStatement.Default == null))
								{
									localStatement.Default = node.ScalarType.LikeType.Default.EmitDefinition(EmitMode.ForCopy);
									if (node.ScalarType.LikeType.Default.HasDependencies())
										plan.AttachDependencies(node.ScalarType.LikeType.Default.Dependencies);
									localStatement.Default.IsGenerated = true;
								}
									
								// Constraints
								foreach (Schema.ScalarTypeConstraint constraint in node.ScalarType.LikeType.Constraints)
								{
									if (!localStatement.Constraints.Contains(constraint.Name))
									{
										ConstraintDefinition constraintDefinition = constraint.EmitDefinition(EmitMode.ForCopy);
										if (constraint.HasDependencies())
											plan.AttachDependencies(constraint.Dependencies);
										constraintDefinition.IsGenerated = true;
										localStatement.Constraints.Add(constraintDefinition);
									}
								}
								
								// Specials
								foreach (Schema.Special special in node.ScalarType.LikeType.Specials)
								{
									if (!localStatement.Specials.Contains(special.Name))
									{
										SpecialDefinition specialDefinition = (SpecialDefinition)special.EmitStatement(EmitMode.ForCopy);
										if (special.HasDependencies())
											plan.AttachDependencies(special.Dependencies);
										specialDefinition.IsGenerated = true;
										localStatement.Specials.Add(specialDefinition);
									}
								}
								
								// Tags
								node.ScalarType.MergeMetaData(node.ScalarType.LikeType.MetaData);
							}
						}
						
						node.ScalarType.MergeMetaData(localStatement.MetaData);
						
						if (localStatement.ClassDefinition != null)
						{
							if (!node.ScalarType.IsDefaultConveyor)
								plan.CheckRight(Schema.RightNames.HostImplementation);
							plan.CheckClassDependency(node.ScalarType.Library, localStatement.ClassDefinition);
							Type type = plan.CreateType(localStatement.ClassDefinition);
							if (!type.IsSubclassOf(typeof(Conveyor)))
								throw new CompilerException(CompilerException.Codes.ConveyorClassExpected, localStatement.ClassDefinition, type.AssemblyQualifiedName);
						}

						node.ScalarType.ClassDefinition = localStatement.ClassDefinition;
						
						Schema.Operator operatorValue;

						// Host-Implemented representations
						foreach (RepresentationDefinition representationDefinition in localStatement.Representations)
							if (!representationDefinition.HasD4ImplementedComponents())
								CompileRepresentation(plan, node.ScalarType, operators, representationDefinition);
							
						#if USETYPEINHERITANCE
						// If this scalar type has no representations defined, but it is based on a single branch inheritance hierarchy leading to a system type, build a default representation
						if (localStatement.Representations.Count == 0)
							CompileDefaultRepresentation(APlan, node.ScalarType, operators);
						#endif
						
						#if !NATIVEROW
						node.ScalarType.StaticByteSize = CellValueStream.MinimumStaticByteSize;
						#endif
						
						if (node.ScalarType.IsDefaultConveyor)
						{
							// Compile the default equality and comparison operator for the type
							if (!plan.InLoadingContext())
							{
								CompileComparisonOperator(plan, node.ScalarType);
								if (node.ScalarType.EqualityOperator != null)
									operators.Add(node.ScalarType.EqualityOperator);
								if (node.ScalarType.ComparisonOperator != null)
									operators.Add(node.ScalarType.ComparisonOperator);
							}
							else
							{
								// Load the set of dependencies to ensure they are reported correctly in the cache
								node.ScalarType.LoadDependencies(plan.CatalogDeviceSession);
							}
						}
						else 
						{
							// If the native representation for this scalar type is not system-provided then a conveyor must be supplied
							if (!node.ScalarType.IsGeneric && node.ScalarType.ClassDefinition == null)
								throw new CompilerException(CompilerException.Codes.ConveyorRequired, statement, node.ScalarType.Name);
						}
						
						if (plan.InLoadingContext())
						{
							node.ScalarType.LoadEqualityOperatorID();
							node.ScalarType.LoadComparisonOperatorID();
							node.ScalarType.LoadSortID();
							node.ScalarType.LoadUniqueSortID();
						}

						#if !NATIVEROW
						// If the meta data contains a definition for static byte size, use that					
						if (node.ScalarType.MetaData.Tags.Contains(TagNames.CStaticByteSize))
							node.ScalarType.StaticByteSize = Convert.ToInt32(node.ScalarType.MetaData.Tags[TagNames.CStaticByteSize].Value);
						#endif

						#if USETYPEINHERITANCE
						// Compile Cast Operators for each immediate ParentType which uses the same conveyor
						CompileCastOperators(APlan, node.ScalarType, operators);
						#endif
						
						// Create implicit conversions
						Schema.Conversion narrowingConversion = null;
						Schema.Conversion wideningConversion = null;
						if ((!plan.InLoadingContext()) && (localStatement.LikeScalarTypeName != String.Empty))
						{
							// create conversion LikeScalarTypeName to ScalarTypeName using ScalarTypeName.LikeRepresentation.Selector narrowing
							// create conversion ScalarTypeName to LikeScalarTypeName using ScalarTypeName.LikeRepresentation.ReadAccessor widening
							
							Schema.Representation representation = FindLikeRepresentation(node.ScalarType);
							string conversionName = Schema.Object.Qualify(Schema.Object.MangleQualifiers(String.Format("Conversion_{0}_{1}", node.ScalarType.LikeType.Name, node.ScalarType.Name)), plan.CurrentLibrary.Name);
							CheckValidCatalogObjectName(plan, statement, conversionName);
							narrowingConversion = new Schema.Conversion(Schema.Object.GetNextObjectID(), conversionName, node.ScalarType.LikeType, node.ScalarType, representation.Selector, true);
							narrowingConversion.IsGenerated = true;
							narrowingConversion.Generator = node.ScalarType;
							narrowingConversion.Owner = plan.User;
							narrowingConversion.Library = plan.CurrentLibrary;
							narrowingConversion.AddDependency(node.ScalarType.LikeType);
							narrowingConversion.AddDependency(node.ScalarType);
							narrowingConversion.AddDependency(representation.Selector);
							if (!node.ScalarType.LikeType.ImplicitConversions.Contains(narrowingConversion))
								node.ScalarType.LikeType.ImplicitConversions.Add(narrowingConversion);

							conversionName = Schema.Object.Qualify(Schema.Object.MangleQualifiers(String.Format("Conversion_{0}_{1}", node.ScalarType.Name, node.ScalarType.LikeType.Name)), plan.CurrentLibrary.Name);
							CheckValidCatalogObjectName(plan, statement, conversionName);							
							wideningConversion = new Schema.Conversion(Schema.Object.GetNextObjectID(), conversionName, node.ScalarType, node.ScalarType.LikeType, representation.Properties[0].ReadAccessor, false);
							wideningConversion.IsGenerated = true;
							wideningConversion.Generator = node.ScalarType;
							wideningConversion.Owner = plan.User;
							wideningConversion.Library = plan.CurrentLibrary;
							wideningConversion.AddDependency(node.ScalarType);
							wideningConversion.AddDependency(node.ScalarType.LikeType);
							wideningConversion.AddDependency(representation.Properties[0].ReadAccessor);
							if (!node.ScalarType.ImplicitConversions.Contains(wideningConversion))
								node.ScalarType.ImplicitConversions.Add(wideningConversion);

							plan.PlanCatalog.Add(narrowingConversion);
							plan.PlanCatalog.Add(wideningConversion);
							plan.Catalog.ConversionPathCache.Clear(node.ScalarType.LikeType);
						}
						plan.Catalog.OperatorResolutionCache.Clear(node.ScalarType, node.ScalarType);
						try
						{
							// D4-Implemented representations
							foreach (RepresentationDefinition representationDefinition in localStatement.Representations)
								if (representationDefinition.HasD4ImplementedComponents())
									CompileRepresentation(plan, node.ScalarType, operators, representationDefinition);

							// Constraints
							foreach (ConstraintDefinition constraint in localStatement.Constraints)
								node.ScalarType.Constraints.Add(CompileScalarTypeConstraint(plan, node.ScalarType, constraint));

							// Compile Special Definitions
							Schema.Special special;
							foreach (SpecialDefinition specialDefinition in localStatement.Specials)
							{
								special = CompileSpecial(plan, node.ScalarType, specialDefinition);
								node.ScalarType.Specials.Add(special);
								if (special.Selector != null)
									operators.Add(special.Selector);
								if (special.Comparer != null)
									operators.Add(special.Comparer);
							}
								
							if (plan.Catalog.Contains(Schema.DataTypes.SystemBooleanName))
							{
								if (!plan.InLoadingContext())
								{
									operatorValue = CompileSpecialOperator(plan, node.ScalarType);
									node.ScalarType.IsSpecialOperator = operatorValue;
									plan.PlanCatalog.Add(operatorValue);
									plan.Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);
									operators.Add(operatorValue);
								}
								else
								{
									node.ScalarType.LoadIsSpecialOperatorID();
								}
							}
							
							// Default
							if (localStatement.Default != null)
								node.ScalarType.Default = CompileScalarTypeDefault(plan, node.ScalarType, localStatement.Default);

							// TODO: Verify that the specials and default satisfy the constraints
							
							for (int index = 0; index < operators.Count; index++)
								blockNode.Nodes.Add(new CreateOperatorNode((Schema.Operator)operators[index]));
								
							if (narrowingConversion != null)
								blockNode.Nodes.Add(new CreateConversionNode(narrowingConversion));
								
							if (wideningConversion != null)
								blockNode.Nodes.Add(new CreateConversionNode(wideningConversion));
							
							return blockNode;
						}
						finally
						{
							if (narrowingConversion != null) 
							{
								if ((node.ScalarType.LikeType != null) && (node.ScalarType.LikeType.ImplicitConversions.Contains(narrowingConversion)))
									node.ScalarType.LikeType.ImplicitConversions.Remove(narrowingConversion);
								if (plan.PlanCatalog.Contains(narrowingConversion.Name))
									plan.PlanCatalog.Remove(narrowingConversion);
							}
								
							if (wideningConversion != null) 
							{
								if ((node.ScalarType.LikeType != null) && (node.ScalarType.ImplicitConversions.Contains(wideningConversion)))
									node.ScalarType.ImplicitConversions.Remove(wideningConversion);
								if (plan.PlanCatalog.Contains(wideningConversion.Name))
									plan.PlanCatalog.Remove(wideningConversion);
							}
						}
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
				catch
				{
					plan.PlanCatalog.SafeRemove(node.ScalarType);
					foreach (Schema.Operator removeOperator in operators)
						plan.PlanCatalog.SafeRemove(removeOperator);
					throw;
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static Schema.Sort CompileSortDefinition(Plan plan, Schema.IDataType dataType, SortDefinition sortDefinition, bool isScalarSort)
		{
			int objectID = Schema.Object.GetObjectID(sortDefinition.MetaData);
			Schema.Sort sort = new Schema.Sort(objectID, String.Format("{0}Sort{1}", dataType.Name, isScalarSort ? String.Empty : objectID.ToString()), dataType);
			sort.IsGenerated = true;
			sort.Owner = plan.User;
			sort.Library = plan.CurrentLibrary;
			plan.PlanCatalog.Add(sort);
			try
			{
				plan.PushCreationObject(sort);
				try
				{
					string leftIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Left);
					string rightIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Right);
					plan.Symbols.Push(new Symbol(leftIdentifier, dataType));
					try
					{
						plan.Symbols.Push(new Symbol(rightIdentifier, dataType));
						try
						{
							PlanNode node = CompileExpression(plan, sortDefinition.Expression);
							if (!(node.DataType.Is(plan.DataTypes.SystemInteger)))
								throw new CompilerException(CompilerException.Codes.IntegerExpressionExpected, sortDefinition.Expression);
							if (!(node.IsFunctional && node.IsDeterministic))
								throw new CompilerException(CompilerException.Codes.InvalidCompareExpression, sortDefinition.Expression);
							node = OptimizeNode(plan, node);
							sort.CompareNode = node;
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.PopCreationObject();
				}
			}
			finally
			{
				plan.PlanCatalog.SafeRemove(sort);
			}
			sort.DetermineRemotable(plan.CatalogDeviceSession);
			return sort;
		}
		
		public static Schema.Sort CompileSortDefinition(Plan plan, Schema.IDataType dataType)
		{
			int messageIndex = plan.Messages.Count;
			try
			{
				Schema.Sort sort = new Schema.Sort(Schema.Object.GetNextObjectID(), String.Format("{0}UniqueSort", dataType.Name), dataType);
				sort.Library = plan.CurrentLibrary;
				sort.Owner = plan.User;
				sort.IsGenerated = true;
				sort.IsUnique = true;
				plan.PlanCatalog.Add(sort);
				try
				{
					plan.PushCreationObject(sort);
					try
					{
						string leftIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Left);
						string rightIdentifier = Schema.Object.Qualify(Keywords.Value, Keywords.Right);
						plan.Symbols.Push(new Symbol(leftIdentifier, dataType));
						try
						{
							plan.Symbols.Push(new Symbol(rightIdentifier, dataType));
							try
							{
								PlanNode node = CompileExpression(plan, new BinaryExpression(new IdentifierExpression(leftIdentifier), Instructions.Compare, new IdentifierExpression(rightIdentifier)));
								if (!(node.DataType.Is(plan.DataTypes.SystemInteger)))
									throw new CompilerException(CompilerException.Codes.IntegerExpressionExpected, plan.CurrentStatement());
								if (!(node.IsFunctional && node.IsDeterministic))
									throw new CompilerException(CompilerException.Codes.InvalidCompareExpression, plan.CurrentStatement());
								node = OptimizeNode(plan, node);
								sort.CompareNode = node;
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
				finally
				{
					plan.PlanCatalog.SafeRemove(sort);
				}
				sort.DetermineRemotable(plan.CatalogDeviceSession);
				return sort;
			}
			catch (Exception exception)
			{
				if ((exception is CompilerException) && (((CompilerException)exception).Code == (int)CompilerException.Codes.NonFatalErrors))
				{
					plan.Messages.Insert(messageIndex, new CompilerException(CompilerException.Codes.UnableToConstructSort, plan.CurrentStatement(), dataType.Name));
					throw exception;
				}
				else
					throw new CompilerException(CompilerException.Codes.UnableToConstructSort, plan.CurrentStatement(), exception, dataType.Name);
			}
		}
		
		public static Schema.IScalarType CompileScalarTypeSpecifier(Plan plan, TypeSpecifier typeSpecifier)
		{
			Schema.IDataType dataType = CompileTypeSpecifier(plan, typeSpecifier);
			if (!(dataType is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeExpected, typeSpecifier);
			return (Schema.IScalarType)dataType;
		}
		
		public static Schema.IDataType CompileTypeSpecifier(Plan plan, TypeSpecifier typeSpecifier)
		{
			return CompileTypeSpecifier(plan, typeSpecifier, false);
		}
		
		public static Schema.IDataType CompileTypeSpecifier(Plan plan, TypeSpecifier typeSpecifier, bool trackDependencies)
		{
			if (typeSpecifier is GenericTypeSpecifier)
				return plan.DataTypes.SystemGeneric;
			else if (typeSpecifier is ScalarTypeSpecifier)
			{
				if (typeSpecifier.IsGeneric)
					return plan.DataTypes.SystemScalar;
				else if (Schema.Object.NamesEqual(((ScalarTypeSpecifier)typeSpecifier).ScalarTypeName, Schema.DataTypes.SystemGenericName))
				{
					return plan.DataTypes.SystemGeneric;
				}
				else
				{
					Schema.Object objectValue = ResolveCatalogIdentifier(plan, ((ScalarTypeSpecifier)typeSpecifier).ScalarTypeName);
					if (objectValue is Schema.IScalarType)
					{
						plan.AttachDependency(objectValue);
						return (Schema.IScalarType)objectValue;
					}
					else
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, ((ScalarTypeSpecifier)typeSpecifier).ScalarTypeName);
				}
			}
			else if (typeSpecifier is RowTypeSpecifier)
			{
				Schema.IRowType rowType = new Schema.RowType();

				if (typeSpecifier.IsGeneric)
				{
					rowType.IsGeneric = true;
					return rowType;
				}

				Schema.IDataType type;
				foreach (NamedTypeSpecifier column in ((RowTypeSpecifier)typeSpecifier).Columns)
				{
					type = CompileTypeSpecifier(plan, column.TypeSpecifier);
					rowType.Columns.Add(new Schema.Column(column.Identifier, type));
				}

				return rowType;
			}
			else if (typeSpecifier is TableTypeSpecifier)
			{
				Schema.ITableType tableType = new Schema.TableType();

				if (typeSpecifier.IsGeneric)
				{
					tableType.IsGeneric = true;
					return tableType;
				}

				Schema.IDataType type;
				foreach (NamedTypeSpecifier column in ((TableTypeSpecifier)typeSpecifier).Columns)
				{
					type = CompileTypeSpecifier(plan, column.TypeSpecifier);
					tableType.Columns.Add(new Schema.Column(column.Identifier, type));
				}

				return tableType;
			}
			else if (typeSpecifier is ListTypeSpecifier)
			{
				if (typeSpecifier.IsGeneric)
				{
					Schema.ListType listType = new Schema.ListType(new Schema.GenericType());
					listType.IsGeneric = true;
					return listType;
				}
				else
					return new Schema.ListType(CompileTypeSpecifier(plan, ((ListTypeSpecifier)typeSpecifier).TypeSpecifier));
			}
			else if (typeSpecifier is CursorTypeSpecifier)
			{
				if (typeSpecifier.IsGeneric)
				{
					Schema.CursorType cursorType = new Schema.CursorType(new Schema.TableType());
					cursorType.TableType.IsGeneric = true;
					cursorType.IsGeneric = true;
					return cursorType;
				}
				else
				{
					Schema.IDataType type = CompileTypeSpecifier(plan, ((CursorTypeSpecifier)typeSpecifier).TypeSpecifier);
					if (!(type is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableTypeExpected, ((CursorTypeSpecifier)typeSpecifier).TypeSpecifier);
					return new Schema.CursorType((Schema.ITableType)type);
				}
			}
			else if (typeSpecifier is TypeOfTypeSpecifier)
			{
				// Push a dummy creation object to prevent dependencies on typeof expression sources
				Schema.BaseTableVar dummy = new Schema.BaseTableVar("Dummy");
				dummy.SessionObjectName = dummy.Name; // This will allow the typeof expression to reference session specific objects
				dummy.SessionID = plan.SessionID;
				//LDummy.Library = APlan.CurrentLibrary;
				Schema.IDataType dataType;
				if (!trackDependencies)
					plan.PushCreationObject(dummy, new LineInfo(plan.CompilingOffset));
				try
				{
					plan.PushTypeOfContext();
					try
					{
						dataType = CompileExpression(plan, ((TypeOfTypeSpecifier)typeSpecifier).Expression).DataType;
					}
					finally
					{
						plan.PopTypeOfContext();
					}
				}
				finally
				{
					if (!trackDependencies)
						plan.PopCreationObject();
				}
				
				// Do not allow a NilGeneric to be the inferred type of a typeof specifier
				if (dataType.IsNil)
					dataType = plan.DataTypes.SystemGeneric;

				// Attach dependencies for the resolved data type
				if (!trackDependencies)
					AttachDataTypeDependencies(plan, dataType);
				return dataType;					
			}
			else
				throw new CompilerException(CompilerException.Codes.UnknownTypeSpecifier, typeSpecifier, typeSpecifier.GetType().Name);
		}
		
		public static void AttachDataTypeDependencies(Plan plan, Schema.IDataType dataType)
		{
			if (dataType is Schema.IScalarType)
			{
				plan.AttachDependency((Schema.ScalarType)dataType);
			}
			else if (dataType is Schema.IRowType)
			{
				foreach (Schema.Column column in ((Schema.IRowType)dataType).Columns)
					AttachDataTypeDependencies(plan, column.DataType);
			}
			else if (dataType is Schema.ITableType)
			{
				foreach (Schema.Column column in ((Schema.ITableType)dataType).Columns)
					AttachDataTypeDependencies(plan, column.DataType);
			}
			else if (dataType is Schema.IListType)
			{
				AttachDataTypeDependencies(plan, ((Schema.IListType)dataType).ElementType);
			}
			else if (dataType is Schema.ICursorType)
			{
				AttachDataTypeDependencies(plan, ((Schema.ICursorType)dataType).TableType);
			}
			else if (!(dataType is Schema.IGenericType))
			{
				Error.Fail(@"Could not attach dependencies for data type ""{0}"".", dataType.Name);
			}
		}
		
		public static PlanNode CompileOperatorBlock(Plan plan, Schema.Operator operatorValue, Statement statement)
		{
			PlanNode block;
			plan.Symbols.PushWindow(0);
			try
			{
				Schema.Operand operand;
				for (int index = 0; index < operatorValue.Operands.Count; index++)
				{
					operand = operatorValue.Operands[index];
					plan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));
				}
				
				int stackDepth = operatorValue.Operands.Count;
				
				if (operatorValue.ReturnDataType != null)
				{
					plan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));
					stackDepth++;
				}
						
				block = CompileStatement(plan, statement);

				for (int index = 0; index < operatorValue.Operands.Count; index++)
					if (!plan.Symbols[operatorValue.Operands.Count - 1 - index].IsModified && (operatorValue.Operands[index].Modifier == Modifier.In))
					{
						operatorValue.Operands[index].Modifier = Modifier.Const;
						operatorValue.OperandsChanged();
					}
				
				// Dispose variables allocated within the block
				BlockNode blockNode = null;
				for (int index = 0; index < plan.Symbols.Count - stackDepth; index++)
				{
					if (plan.Symbols[index].DataType.IsDisposable)
					{
						if (blockNode == null)
						{
							blockNode = new BlockNode();
							blockNode.IsBreakable = false;
							blockNode.Nodes.Add(block);
						}

						blockNode.Nodes.Add(new DeallocateVariableNode(index));
					}
				}
				
				if (blockNode != null)
					block = blockNode;

				return block;
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static PlanNode BindOperatorBlock(Plan plan, Schema.Operator operatorValue, PlanNode block)
		{
			plan.Symbols.PushWindow(0);
			try
			{
				Schema.Operand operand;
				for (int index = 0; index < operatorValue.Operands.Count; index++)
				{
					operand = operatorValue.Operands[index];
					plan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));
				}
					
				if (operatorValue.ReturnDataType != null)
					plan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));
				
				return OptimizeNode(plan, block);
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static void ProcessSourceContext(Plan plan, CreateOperatorStatement statement, CreateOperatorNode LNode)
		{
			// SourceContext is the script that is currently being compiled
			// The Locator in the source context represents an offset into the script identified by the locator, not
			// the actual script contained in the SourceContext. Line numbers in AOperatorLineInfo will be relative 
			// to the actual script in SourceContext, not the locator.
			
			if (plan.SourceContext != null)
			{
				// Determine the line offsets for the operator declaration
				LineInfo lineInfo = new LineInfo();
				lineInfo.Line = statement.Line;
				lineInfo.LinePos = statement.LinePos;
				lineInfo.EndLine = statement.Block.Line;
				lineInfo.EndLinePos = statement.Block.LinePos;

				// Copy the text of the operator from the source context
				// Note that the text does not include the metadata for the operator, just the operator header and body.
				if (statement.Block.ClassDefinition == null)
				{
					LNode.CreateOperator.DeclarationText = SourceUtility.CopySection(plan.SourceContext.Script, lineInfo);
					LNode.CreateOperator.BodyText = SourceUtility.CopySection(plan.SourceContext.Script, statement.Block.LineInfo);
				}
				
				// Set the compiling offset to the starting location of the operator in the script
				plan.CompilingOffset.Line = lineInfo.Line - 1;
				plan.CompilingOffset.LinePos = lineInfo.LinePos - 1;
				plan.CompilingOffset.EndLine = lineInfo.Line - 1;
				plan.CompilingOffset.EndLinePos = lineInfo.LinePos - 1;
				
				// Pull the debug locator from the DAE.Locator metadata tag, if present
				DebugLocator locator = Schema.Operator.GetLocator(LNode.CreateOperator.MetaData);
				if (locator != null)
					LNode.CreateOperator.Locator = locator;
				else
				{
					// Set the debug locator to the combination of the source context debug locator and the operator line info
					if (plan.SourceContext.Locator != null)
					{
						LNode.CreateOperator.Locator = 
							new DebugLocator
							(
								plan.SourceContext.Locator.Locator, 
								plan.SourceContext.Locator.Line + lineInfo.Line - 1, 
								lineInfo.LinePos
							);
					}
					else
					{
						// If there is no locator, this is either dynamic or ad-hoc execution, and the locator should be zero
						// so the operator text can be returned as the debug context.
						LNode.CreateOperator.Locator =
							new DebugLocator
							(
								DebugLocator.OperatorLocator(LNode.CreateOperator.DisplayName),
								0,
								0
							);
					}
				}
			}
			else
			{
				// If there is no source context, return a basic operator locator
				// This will at least allow statement emission to be used to provide the operator text to the debugger
				LNode.CreateOperator.Locator =
					new DebugLocator
					(
						DebugLocator.OperatorLocator(LNode.CreateOperator.DisplayName),
						0,
						0
					);
			}
		}
		
		public static PlanNode CompileCreateOperatorStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateOperatorStatement localStatement = (CreateOperatorStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateOperator);
			CreateOperatorNode node = new CreateOperatorNode();
			string operatorName = Schema.Object.Qualify(localStatement.OperatorName, plan.CurrentLibrary.Name);
			string sessionOperatorName = null;
			string sourceOperatorName = null;
			bool addedSessionObject = false;
			if (localStatement.IsSession)
			{
				sessionOperatorName = operatorName;
				int index = plan.PlanSessionOperators.IndexOfName(operatorName);
				if (index >= 0)
					operatorName = ((Schema.SessionObject)plan.PlanSessionOperators[index]).GlobalName;
				else
				{
					index = plan.SessionOperators.IndexOfName(operatorName);
					if (index >= 0)
						operatorName = ((Schema.SessionObject)plan.SessionOperators[index]).GlobalName;
					else
					{
						if (plan.IsEngine)
							operatorName = MetaData.GetTag(localStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
						else
							operatorName = Schema.Object.NameFromGuid(Guid.NewGuid());
						plan.PlanSessionOperators.Add(new Schema.SessionObject(sessionOperatorName, operatorName));
						addedSessionObject = true;
					}
				}
			}
			else if ((localStatement.MetaData != null) && localStatement.MetaData.Tags.Contains("DAE.SourceOperatorName"))
				sourceOperatorName = MetaData.GetTag(localStatement.MetaData, "DAE.SourceOperatorName", String.Empty);
			try
			{
				node.CreateOperator = new Schema.Operator(Schema.Object.GetObjectID(localStatement.MetaData), operatorName);
				node.CreateOperator.MetaData = localStatement.MetaData;
				node.CreateOperator.SessionObjectName = sessionOperatorName;
				node.CreateOperator.SessionID = plan.SessionID;
				node.CreateOperator.SourceOperatorName = sourceOperatorName;
				node.CreateOperator.IsGenerated = (sessionOperatorName != null) || (sourceOperatorName != null);
				// If this is an A/T operator and we are not in an A/T, then it must be recompiled when it is first used within an A/T
				node.CreateOperator.ShouldRecompile = node.CreateOperator.IsATObject && (plan.ApplicationTransactionID == Guid.Empty);
				
				plan.PushCreationObject(node.CreateOperator);
				try
				{
					foreach (FormalParameter formalParameter in localStatement.FormalParameters)
						node.CreateOperator.Operands.Add(new Schema.Operand(node.CreateOperator, formalParameter.Identifier, CompileTypeSpecifier(plan, formalParameter.TypeSpecifier, true), formalParameter.Modifier));

					if (localStatement.ReturnType != null)
						node.CreateOperator.ReturnDataType = CompileTypeSpecifier(plan, localStatement.ReturnType, true);
						
					try
					{
						CheckValidCatalogOperatorName(plan, localStatement, node.CreateOperator.OperatorName, node.CreateOperator.Signature);
					}
					catch
					{
						// If this is a repository, and we are creating a duplicate operator, ignore this statement and move on
						// This will allow us to move operators into the base catalog object set without having to upgrade the
						// system catalog.
						if (plan.IsEngine)
							return new NoOpNode();
						throw;
					}

					node.CreateOperator.Owner = plan.User;
					node.CreateOperator.Library = node.CreateOperator.IsGenerated ? null : plan.CurrentLibrary;

					#if USEVIRTUALOPERATORS
					node.CreateOperator.IsAbstract = localStatement.IsAbstract;
					node.CreateOperator.IsVirtual = localStatement.IsVirtual;
					node.CreateOperator.IsOverride = localStatement.IsOverride;
				
					node.CreateOperator.IsReintroduced = localStatement.IsReintroduced;
					if (node.CreateOperator.IsOverride)
					{
						lock (APlan.Catalog)
						{
							int catalogIndex = APlan.Catalog.IndexOfInherited(node.CreateOperator.Name, node.CreateOperator.Signature);
							if (catalogIndex < 0)
								throw new CompilerException(CompilerException.Codes.InvalidOverrideDirective, AStatement, node.CreateOperator.Name, node.CreateOperator.Signature.ToString());
							//APlan.AcquireCatalogLock(APlan.Catalog[LCatalogIndex], LockMode.Shared);
							APlan.AttachDependency(APlan.Catalog[catalogIndex]);
						}
					}
					#endif
						
					ProcessSourceContext(plan, localStatement, node);

					plan.PlanCatalog.Add(node.CreateOperator);
					try
					{
						plan.Catalog.OperatorResolutionCache.Clear(node.CreateOperator.OperatorName);

						node.CreateOperator.IsBuiltin = Instructions.Contains(Schema.Object.Unqualify(node.CreateOperator.OperatorName));
						if (localStatement.Block.ClassDefinition != null)
						{
							#if USEVIRTUAL
							if (node.CreateOperator.IsVirtualCall)
								throw new CompilerException(CompilerException.Codes.InvalidVirtualDirective, AStatement, node.CreateOperator.Name, node.CreateOperator.Signature);
							#endif
							plan.CheckRight(Schema.RightNames.HostImplementation);
							plan.CheckClassDependency(node.CreateOperator.Library, localStatement.Block.ClassDefinition);
							node.CreateOperator.Block.ClassDefinition = localStatement.Block.ClassDefinition;
						}
						else
						{
							node.CreateOperator.Block.BlockNode = BindOperatorBlock(plan, node.CreateOperator, CompileOperatorBlock(plan, node.CreateOperator, localStatement.Block.Block));
							node.CreateOperator.DetermineRemotable(plan.CatalogDeviceSession);
						}
						
						node.CreateOperator.IsRemotable = Convert.ToBoolean(MetaData.GetTag(node.CreateOperator.MetaData, "DAE.IsRemotable", node.CreateOperator.IsRemotable.ToString()));
						node.CreateOperator.IsLiteral = Convert.ToBoolean(MetaData.GetTag(node.CreateOperator.MetaData, "DAE.IsLiteral", node.CreateOperator.IsLiteral.ToString()));
						node.CreateOperator.IsFunctional = Convert.ToBoolean(MetaData.GetTag(node.CreateOperator.MetaData, "DAE.IsFunctional", node.CreateOperator.IsFunctional.ToString()));
						node.CreateOperator.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(node.CreateOperator.MetaData, "DAE.IsDeterministic", node.CreateOperator.IsDeterministic.ToString()));
						node.CreateOperator.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(node.CreateOperator.MetaData, "DAE.IsRepeatable", node.CreateOperator.IsRepeatable.ToString()));
						node.CreateOperator.IsNilable = Convert.ToBoolean(MetaData.GetTag(node.CreateOperator.MetaData, "DAE.IsNilable", node.CreateOperator.IsNilable.ToString()));
						
						if (!node.CreateOperator.IsRepeatable && node.CreateOperator.IsDeterministic)
							node.CreateOperator.IsDeterministic = false;
						if (!node.CreateOperator.IsDeterministic && node.CreateOperator.IsLiteral)
							node.CreateOperator.IsLiteral = false;
						node.DetermineCharacteristics(plan);

						return node;
					}
					catch
					{
						plan.PlanCatalog.SafeRemove(node.CreateOperator);
						throw;
					}
				}
				finally
				{
					plan.PopCreationObject();
				}
			}
			catch
			{
				if (addedSessionObject)
					plan.PlanSessionOperators.RemoveAt(plan.PlanSessionOperators.IndexOfName(sessionOperatorName));
				throw;
			}
		}
		
		public static void RecompileOperator(Plan plan, Schema.Operator operatorValue)
		{
			//APlan.AcquireCatalogLock(AOperator, LockMode.Exclusive);
			try
			{
				operatorValue.ShouldRecompile = false;
				try
				{
					bool isATObject = operatorValue.IsATObject;
					if (!isATObject)
						plan.PushGlobalContext();
					try
					{
						if (operatorValue.Block.ClassDefinition == null)
						{
							operatorValue.Dependencies.Clear();
							operatorValue.IsBuiltin = Instructions.Contains(Schema.Object.Unqualify(operatorValue.OperatorName));

							Plan localPlan = new Plan(plan.ServerProcess);
							try
							{
								localPlan.PushATCreationContext();
								try
								{
									localPlan.PushCreationObject(operatorValue);
									try
									{
										localPlan.PushStatementContext(new StatementContext(StatementType.Select));
										try
										{
											localPlan.PushSecurityContext(new SecurityContext(operatorValue.Owner));
											try
											{
												// Report dependencies for the signature and return types
												if (operatorValue.DeclarationText != null)
												{
													Parser parser = new Parser();
													CreateOperatorStatement statement = parser.ParseOperatorDeclaration(operatorValue.DeclarationText);

													foreach (FormalParameter formalParameter in statement.FormalParameters)
														CompileTypeSpecifier(localPlan, formalParameter.TypeSpecifier, true);

													if (statement.ReturnType != null)
														CompileTypeSpecifier(localPlan, statement.ReturnType, true);
												}
												else
												{
													Schema.Catalog dependencies = new Schema.Catalog();
													foreach (Schema.Operand operand in operatorValue.Operands)
														operand.DataType.IncludeDependencies(plan.CatalogDeviceSession, plan.Catalog, dependencies, EmitMode.ForCopy);
													if (operatorValue.ReturnDataType != null)
														operatorValue.ReturnDataType.IncludeDependencies(plan.CatalogDeviceSession, plan.Catalog, dependencies, EmitMode.ForCopy);
													foreach (Schema.Object objectValue in dependencies)
														localPlan.AttachDependency(objectValue);
												}

												PlanNode blockNode = BindOperatorBlock(localPlan, operatorValue, CompileOperatorBlock(localPlan, operatorValue, operatorValue.BodyText != null ? new Parser().ParseStatement(operatorValue.BodyText, null) : operatorValue.Block.BlockNode.EmitStatement(EmitMode.ForCopy)));
												localPlan.CheckCompiled();
												operatorValue.Block.BlockNode = blockNode;
												operatorValue.DetermineRemotable(plan.CatalogDeviceSession);

												operatorValue.IsRemotable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsRemotable", operatorValue.IsRemotable.ToString()));
												operatorValue.IsLiteral = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsLiteral", operatorValue.IsLiteral.ToString()));
												operatorValue.IsFunctional = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsFunctional", operatorValue.IsFunctional.ToString()));
												operatorValue.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsDeterministic", operatorValue.IsDeterministic.ToString()));
												operatorValue.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsRepeatable", operatorValue.IsRepeatable.ToString()));
												operatorValue.IsNilable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsNilable", operatorValue.IsNilable.ToString()));
													
												if (!operatorValue.IsRepeatable && operatorValue.IsDeterministic)
													operatorValue.IsDeterministic = false;
												if (!operatorValue.IsDeterministic && operatorValue.IsLiteral)
													operatorValue.IsLiteral = false;

												plan.CatalogDeviceSession.UpdateCatalogObject(operatorValue);
											}
											finally
											{
												localPlan.PopSecurityContext();
											}
										}
										finally
										{
											localPlan.PopStatementContext();
										}
									}
									finally
									{
										localPlan.PopCreationObject();
									}
								}
								finally
								{
									localPlan.PopATCreationContext();
								}
							}
							finally
							{
								plan.Messages.AddRange(localPlan.Messages);
								localPlan.Dispose();
							}
						}
					}
					finally
					{
						if (!isATObject)
							plan.PopGlobalContext();
					}
				}
				catch
				{
					operatorValue.ShouldRecompile = true;
					throw;
				}
				
				// If this is an A/T operator and we are not in an A/T, then it must be recompiled when it is first used within an A/T
				operatorValue.ShouldRecompile = operatorValue.IsATObject && (plan.ApplicationTransactionID == Guid.Empty);
			}
			finally
			{
				//APlan.ReleaseCatalogLock(AOperator);
			}
		}
		
		public static void ProcessSourceContext(Plan plan, CreateAggregateOperatorStatement statement, CreateOperatorNode LNode)
		{
			// SourceContext is the script that is currently being compiled
			// The Locator in the source context represents an offset into the script identified by the locator, not
			// the actual script contained in the SourceContext. Line numbers in AOperatorLineInfo will be relative 
			// to the actual script in SourceContext, not the locator.
			
			if (plan.SourceContext != null)
			{
				// Determine the line offsets for the operator declaration
				LineInfo lineInfo = new LineInfo();
				lineInfo.Line = statement.Line;
				lineInfo.LinePos = statement.LinePos;
				lineInfo.EndLine = statement.Initialization.Line;
				lineInfo.EndLinePos = statement.Initialization.LinePos;

				// Copy the text of the operator from the source context
				// Note that the text does not include the metadata for the operator, just the operator header and body.
				if ((statement.Initialization.ClassDefinition == null) || (statement.Aggregation.ClassDefinition == null) || (statement.Finalization.ClassDefinition == null))
				{
					Schema.AggregateOperator aggregateOperator = (Schema.AggregateOperator)LNode.CreateOperator;
					aggregateOperator.DeclarationText = SourceUtility.CopySection(plan.SourceContext.Script, lineInfo);
					aggregateOperator.InitializationText = SourceUtility.CopySection(plan.SourceContext.Script, new LineInfo(statement.Initialization.Line, statement.Initialization.LinePos, statement.Aggregation.Line, statement.Aggregation.LinePos));
					aggregateOperator.AggregationText = SourceUtility.CopySection(plan.SourceContext.Script, new LineInfo(statement.Aggregation.Line, statement.Aggregation.LinePos, statement.Finalization.Line, statement.Finalization.LinePos));
					aggregateOperator.FinalizationText = SourceUtility.CopySection(plan.SourceContext.Script, statement.Finalization.LineInfo);
				}
				
				// Set the compiling offset to the starting location of the operator in the script
				plan.CompilingOffset.Line = lineInfo.Line - 1;
				plan.CompilingOffset.LinePos = lineInfo.LinePos - 1;
				plan.CompilingOffset.EndLine = lineInfo.Line - 1;
				plan.CompilingOffset.EndLinePos = lineInfo.LinePos - 1;
				
				// Pull the debug locator from the DAE.Locator metadata tag, if present
				DebugLocator locator = Schema.Operator.GetLocator(LNode.CreateOperator.MetaData);
				if (locator != null)
					LNode.CreateOperator.Locator = locator;
				else
				{
					// Set the debug locator to the combination of the source context debug locator and the operator line info
					if (plan.SourceContext.Locator != null)
					{
						LNode.CreateOperator.Locator = 
							new DebugLocator
							(
								plan.SourceContext.Locator.Locator, 
								plan.SourceContext.Locator.Line + lineInfo.Line - 1, 
								lineInfo.LinePos
							);
					}
					else
					{
						// If there is no locator, this is either dynamic or ad-hoc execution, and the locator should be zero
						// so the operator text can be returned as the debug context.
						LNode.CreateOperator.Locator =
							new DebugLocator
							(
								DebugLocator.OperatorLocator(LNode.CreateOperator.DisplayName),
								0,
								0
							);
					}
				}
			}
			else
			{
				// If there is no source context, return a basic operator locator
				// This will at least allow statement emission to be used to provide the operator text to the debugger
				LNode.CreateOperator.Locator =
					new DebugLocator
					(
						DebugLocator.OperatorLocator(LNode.CreateOperator.DisplayName),
						0,
						0
					);
			}
		}

		private static void BindAggregateOperatorBlock(Plan plan, Schema.AggregateOperator operatorValue)
		{
			// Binding phase
			// Create an AggregateCallNode to perform the binding
			// targetNode must be a table with the same number of columns as the operator has operands
			// columnNames is the names of the columns in the TargetNode
			// orderColumns specifies the ordering of the columns to be used to feed the aggregate operator
			var typeSpecifier = new TableTypeSpecifier();
			var columnNames = new string[operatorValue.Operands.Count];
			var orderColumns = new OrderColumnDefinitions();
			for (int index = 0; index < operatorValue.Operands.Count; index++)
			{
				var operand = operatorValue.Operands[index];
				columnNames[index] = operand.Name;
				typeSpecifier.Columns.Add(new NamedTypeSpecifier { Identifier = operand.Name, TypeSpecifier = operand.DataType.IsGeneric ? new ScalarTypeSpecifier("System.Integer") : operand.DataType.EmitSpecifier(EmitMode.ForCopy) });
				orderColumns.Add(new OrderColumnDefinition(operand.Name, true));
			}

			var tableSelector = new TableSelectorExpression(); 
			tableSelector.TypeSpecifier = typeSpecifier;
			var targetNode = CompileTableSelectorExpression(plan, tableSelector);
			var aggregateCallNode = BuildAggregateCallNode(plan, null, operatorValue, targetNode, columnNames, orderColumns);
			OptimizeNode(plan, aggregateCallNode);
		}
		
		public static PlanNode CompileCreateAggregateOperatorStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateAggregateOperatorStatement localStatement = (CreateAggregateOperatorStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateOperator);
			CreateOperatorNode node = new CreateOperatorNode();
			string operatorName = Schema.Object.Qualify(localStatement.OperatorName, plan.CurrentLibrary.Name);
			string sessionOperatorName = null;
			bool addedSessionObject = false;
			if (localStatement.IsSession)
			{
				sessionOperatorName = operatorName;
				int index = plan.PlanSessionOperators.IndexOfName(operatorName);
				if (index >= 0)
					operatorName = ((Schema.SessionObject)plan.PlanSessionOperators[index]).GlobalName;
				else
				{
					if (plan.IsEngine)
						operatorName = MetaData.GetTag(localStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						operatorName = Schema.Object.NameFromGuid(Guid.NewGuid());
					plan.PlanSessionOperators.Add(new Schema.SessionObject(sessionOperatorName, operatorName));
					addedSessionObject = true;
				}
			}
			try
			{
				Schema.AggregateOperator operatorValue = new Schema.AggregateOperator(Schema.Object.GetObjectID(localStatement.MetaData), operatorName);
				operatorValue.MetaData = localStatement.MetaData;
				operatorValue.IsOrderDependent = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsOrderDependent", "false"));
				operatorValue.SessionObjectName = sessionOperatorName;
				operatorValue.SessionID = plan.SessionID;
				node.CreateOperator = operatorValue;
				plan.PushCreationObject(operatorValue);
				try
				{
					foreach (FormalParameter formalParameter in localStatement.FormalParameters)
						operatorValue.Operands.Add(new Schema.Operand(operatorValue, formalParameter.Identifier, CompileTypeSpecifier(plan, formalParameter.TypeSpecifier), formalParameter.Modifier));

					operatorValue.ReturnDataType = CompileTypeSpecifier(plan, localStatement.ReturnType);
					
					CheckValidCatalogOperatorName(plan, localStatement, operatorValue.OperatorName, operatorValue.Signature);

					operatorValue.Owner = plan.User;
					operatorValue.Library = plan.CurrentLibrary;
						
					ProcessSourceContext(plan, localStatement, node);

					plan.PlanCatalog.Add(operatorValue);
					try
					{
						plan.Catalog.OperatorResolutionCache.Clear(operatorValue.OperatorName);

						#if USEVIRTUALOPERATORS
						operatorValue.IsAbstract = localStatement.IsAbstract;
						operatorValue.IsVirtual = localStatement.IsVirtual;
						operatorValue.IsOverride = localStatement.IsOverride;
						if (operatorValue.IsOverride)
						{
							lock (APlan.Catalog)
							{
								int catalogIndex = APlan.Catalog.IndexOfInherited(operatorValue.Name, operatorValue.Signature);
								if (catalogIndex < 0)
									throw new CompilerException(CompilerException.Codes.InvalidOverrideDirective, AStatement, operatorValue.Name, operatorValue.Signature.ToString());
								//APlan.AcquireCatalogLock(APlan.Catalog[LCatalogIndex], LockMode.Shared);
								APlan.AttachDependency(APlan.Catalog[catalogIndex]);
							}
						}
						#endif
						
						plan.Symbols.PushWindow(0);
						try
						{
							Symbol resultVar = new Symbol(Keywords.Result, operatorValue.ReturnDataType);
							plan.Symbols.Push(resultVar);

							int symbolCount = plan.Symbols.Count;
							operatorValue.Initialization.SetLineInfo(plan, localStatement.Initialization.LineInfo);
							if (localStatement.Initialization.ClassDefinition != null)
							{
								plan.CheckRight(Schema.RightNames.HostImplementation);
								plan.CheckClassDependency(operatorValue.Library, localStatement.Initialization.ClassDefinition);
								operatorValue.Initialization.ClassDefinition = localStatement.Initialization.ClassDefinition;
								operatorValue.Initialization.StackDisplacement = Convert.ToInt32(MetaData.GetTag(operatorValue.MetaData, "DAE.Initialization.StackDisplacement", "0"));
							}
							else
							{
								operatorValue.Initialization.BlockNode = CompileStatement(plan, localStatement.Initialization.Block);
								operatorValue.Initialization.StackDisplacement = plan.Symbols.Count - symbolCount;
							}
							
							operatorValue.Aggregation.SetLineInfo(plan, localStatement.Aggregation.LineInfo);
							if (localStatement.Aggregation.ClassDefinition != null)
							{
								plan.CheckRight(Schema.RightNames.HostImplementation);
								plan.CheckClassDependency(operatorValue.Library, localStatement.Aggregation.ClassDefinition);
								operatorValue.Aggregation.ClassDefinition = localStatement.Aggregation.ClassDefinition;
							}
							else
							{
								plan.Symbols.PushFrame();
								try
								{
									foreach (Schema.Operand operand in operatorValue.Operands)
										plan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));

									operatorValue.Aggregation.BlockNode = CompileDeallocateFrameVariablesNode(plan, CompileStatement(plan, localStatement.Aggregation.Block));
								}
								finally
								{
									plan.Symbols.PopFrame();
								}
							}
								
							operatorValue.Finalization.SetLineInfo(plan, localStatement.Finalization.LineInfo);
							if (localStatement.Finalization.ClassDefinition != null)
							{
								plan.CheckRight(Schema.RightNames.HostImplementation);
								plan.CheckClassDependency(operatorValue.Library, localStatement.Finalization.ClassDefinition);
								operatorValue.Finalization.ClassDefinition = localStatement.Finalization.ClassDefinition;
								operatorValue.Finalization.BlockNode = CompileDeallocateVariablesNode(plan, new NoOpNode(), resultVar);
							}
							else
							{
								operatorValue.Finalization.BlockNode = CompileDeallocateVariablesNode(plan, CompileStatement(plan, localStatement.Finalization.Block), resultVar);
							}
						}
						finally
						{
							plan.Symbols.PopWindow();
						}

						BindAggregateOperatorBlock(plan, operatorValue);
						
						/*plan.Symbols.PushWindow(0);
						try
						{
							plan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));
							
							if (localStatement.Initialization.ClassDefinition == null)
								operatorValue.Initialization.BlockNode = OptimizeNode(plan, operatorValue.Initialization.BlockNode, false);
							
							if (localStatement.Aggregation.ClassDefinition == null)
							{
								plan.Symbols.PushFrame();
								try
								{
									foreach (Schema.Operand operand in operatorValue.Operands)
										plan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));

									operatorValue.Aggregation.BlockNode = OptimizeNode(plan, operatorValue.Aggregation.BlockNode, false);
								}
								finally
								{
									plan.Symbols.PopFrame();
								}
							}
							
							if (localStatement.Finalization.ClassDefinition == null)
								operatorValue.Finalization.BlockNode = OptimizeNode(plan, operatorValue.Finalization.BlockNode, false);
						}
						finally
						{
							plan.Symbols.PopWindow();
						}
						*/

						operatorValue.DetermineRemotable(plan.CatalogDeviceSession);
						operatorValue.IsRemotable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsRemotable", operatorValue.IsRemotable.ToString()));
						operatorValue.IsLiteral = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsLiteral", operatorValue.IsLiteral.ToString()));
						operatorValue.IsFunctional = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsFunctional", operatorValue.IsFunctional.ToString()));
						operatorValue.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsDeterministic", operatorValue.IsDeterministic.ToString()));
						operatorValue.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsRepeatable", operatorValue.IsRepeatable.ToString()));
						operatorValue.IsNilable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsNilable", operatorValue.IsNilable.ToString()));

						if (!operatorValue.IsRepeatable && operatorValue.IsDeterministic)
							operatorValue.IsDeterministic = false;
						if (!operatorValue.IsDeterministic && operatorValue.IsLiteral)
							operatorValue.IsLiteral = false;

						node.DetermineCharacteristics(plan);
						return node;
					}
					catch
					{
						plan.PlanCatalog.SafeRemove(operatorValue);
						throw;
					}
				}
				finally
				{
					plan.PopCreationObject();
				}
			}
			catch
			{
				if (addedSessionObject)
					plan.PlanSessionOperators.RemoveAt(plan.PlanSessionOperators.IndexOfName(sessionOperatorName));
				throw;
			}
		}
		
		public static void RecompileAggregateOperator(Plan plan, Schema.AggregateOperator operatorValue)
		{
			//APlan.AcquireCatalogLock(AOperator, LockMode.Exclusive);
			try
			{
				bool isATObject = operatorValue.IsATObject;
				if (!isATObject)
					plan.PushGlobalContext();
				try
				{
					PlanNode saveInitializationNode = operatorValue.Initialization.BlockNode;
					int saveInitializationDisplacement = operatorValue.Initialization.StackDisplacement;
					PlanNode saveAggregationNode = operatorValue.Aggregation.BlockNode;
					PlanNode saveFinalizationNode = operatorValue.Finalization.BlockNode;
					operatorValue.Dependencies.Clear();
					operatorValue.IsBuiltin = Instructions.Contains(Schema.Object.Unqualify(operatorValue.OperatorName));
					Plan localPlan = new Plan(plan.ServerProcess);
					try
					{
						localPlan.PushATCreationContext();
						try
						{
							localPlan.PushCreationObject(operatorValue);
							try
							{
								localPlan.PushStatementContext(new StatementContext(StatementType.Select));
								try
								{
									localPlan.PushSecurityContext(new SecurityContext(operatorValue.Owner));
									try
									{
										// Report dependencies for the signature and return types
										if (operatorValue.DeclarationText != null)
										{
											CreateAggregateOperatorStatement statement = new Parser().ParseAggregateOperatorDeclaration(operatorValue.DeclarationText);

											foreach (FormalParameter formalParameter in statement.FormalParameters)
												CompileTypeSpecifier(localPlan, formalParameter.TypeSpecifier, true);

											if (statement.ReturnType != null)
												CompileTypeSpecifier(localPlan, statement.ReturnType, true);
										}
										else
										{
											Schema.Catalog dependencies = new Schema.Catalog();
											foreach (Schema.Operand operand in operatorValue.Operands)
												operand.DataType.IncludeDependencies(plan.CatalogDeviceSession, plan.Catalog, dependencies, EmitMode.ForCopy);
											if (operatorValue.ReturnDataType != null)
												operatorValue.ReturnDataType.IncludeDependencies(plan.CatalogDeviceSession, plan.Catalog, dependencies, EmitMode.ForCopy);
											foreach (Schema.Object objectValue in dependencies)
												localPlan.AttachDependency(objectValue);
										}

										int initializationDisplacement = 0;

										localPlan.Symbols.PushWindow(0);
										try
										{
											Symbol resultVar = new Symbol(Keywords.Result, operatorValue.ReturnDataType);
											localPlan.Symbols.Push(resultVar);
												
											if (operatorValue.Initialization.ClassDefinition == null)
											{
												LineInfo saveCompilingOffset = new LineInfo(localPlan.CompilingOffset);
												localPlan.CompilingOffset.Line = -(operatorValue.Initialization.LineInfo.Line - 1);
												localPlan.CompilingOffset.LinePos = 0;
												try
												{
													int symbolCount = localPlan.Symbols.Count;
													operatorValue.Initialization.BlockNode = CompileStatement(localPlan, new Parser().ParseStatement(operatorValue.InitializationText, null)); //AOperator.Initialization.BlockNode.EmitStatement(EmitMode.ForCopy)); 
													initializationDisplacement = localPlan.Symbols.Count - symbolCount;
												}
												finally
												{
													localPlan.CompilingOffset.SetFromLineInfo(saveCompilingOffset);

												}
											}
				
											if (operatorValue.Aggregation.ClassDefinition == null)
											{
												LineInfo saveCompilingOffset = new LineInfo(localPlan.CompilingOffset);
												localPlan.CompilingOffset.Line = -(operatorValue.Aggregation.LineInfo.Line - 1);
												localPlan.CompilingOffset.LinePos = 0;
												try
												{
													localPlan.Symbols.PushFrame();
													try
													{
														foreach (Schema.Operand operand in operatorValue.Operands)
															localPlan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));

														operatorValue.Aggregation.BlockNode = CompileDeallocateFrameVariablesNode(localPlan, CompileStatement(localPlan, new Parser().ParseStatement(operatorValue.AggregationText, null))); //AOperator.Aggregation.BlockNode.EmitStatement(EmitMode.ForCopy)));
													}
													finally
													{
														localPlan.Symbols.PopFrame();
													}
												}
												finally
												{
													localPlan.CompilingOffset.SetFromLineInfo(saveCompilingOffset);

												}
											}

											if (operatorValue.Finalization.ClassDefinition == null)
											{
												LineInfo saveCompilingOffset = new LineInfo(localPlan.CompilingOffset);
												localPlan.CompilingOffset.Line = -(operatorValue.Finalization.LineInfo.Line - 1);
												localPlan.CompilingOffset.LinePos = 0;
												try
												{
													operatorValue.Finalization.BlockNode = CompileDeallocateVariablesNode(localPlan, CompileStatement(localPlan, new Parser().ParseStatement(operatorValue.FinalizationText, null)), resultVar); //AOperator.Finalization.BlockNode.EmitStatement(EmitMode.ForCopy)), LResultVar);
												}
												finally
												{
													localPlan.CompilingOffset.SetFromLineInfo(saveCompilingOffset);

												}
											}
										}
										finally
										{
											localPlan.Symbols.PopWindow();
										}

										BindAggregateOperatorBlock(plan, operatorValue);

										/*
										localPlan.Symbols.PushWindow(0);
										try
										{
											localPlan.Symbols.Push(new Symbol(Keywords.Result, operatorValue.ReturnDataType));

											if (operatorValue.Initialization.ClassDefinition == null)
												initializationNode = OptimizeNode(localPlan, initializationNode, false);
												
											if (operatorValue.Aggregation.ClassDefinition == null)
											{
												localPlan.Symbols.PushFrame();
												try
												{
													foreach (Schema.Operand operand in operatorValue.Operands)
														localPlan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));

													aggregationNode = OptimizeNode(localPlan, aggregationNode, false);
												}
												finally
												{
													localPlan.Symbols.PopFrame();
												}
											}

											if (operatorValue.Finalization.ClassDefinition == null)
												finalizationNode = OptimizeNode(localPlan, finalizationNode, false);
										}
										finally
										{
											localPlan.Symbols.PopWindow();
										}
										*/

										localPlan.CheckCompiled();

										if (operatorValue.Initialization.BlockNode != null)
										{
											operatorValue.Initialization.StackDisplacement = initializationDisplacement;
										}
											
										operatorValue.DetermineRemotable(plan.CatalogDeviceSession);
										operatorValue.IsRemotable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsRemotable", operatorValue.IsRemotable.ToString()));
										operatorValue.IsLiteral = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsLiteral", operatorValue.IsLiteral.ToString()));
										operatorValue.IsFunctional = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsFunctional", operatorValue.IsFunctional.ToString()));
										operatorValue.IsDeterministic = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsDeterministic", operatorValue.IsDeterministic.ToString()));
										operatorValue.IsRepeatable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsRepeatable", operatorValue.IsRepeatable.ToString()));
										operatorValue.IsNilable = Convert.ToBoolean(MetaData.GetTag(operatorValue.MetaData, "DAE.IsNilable", operatorValue.IsNilable.ToString()));
											
										if (!operatorValue.IsRepeatable && operatorValue.IsDeterministic)
											operatorValue.IsDeterministic = false;
										if (!operatorValue.IsDeterministic && operatorValue.IsLiteral)
											operatorValue.IsLiteral = false;

										plan.CatalogDeviceSession.UpdateCatalogObject(operatorValue);
									}
									finally
									{
										localPlan.PopSecurityContext();
									}
								}
								finally
								{
									localPlan.PopStatementContext();
								}
							}
							finally
							{
								localPlan.PopCreationObject();
							}
						}
						finally
						{
							localPlan.PopATCreationContext();
						}
					}
					finally
					{
						plan.Messages.AddRange(localPlan.Messages);
						localPlan.Dispose();
					}

					operatorValue.ShouldRecompile = false;
				}
				finally
				{
					if (!isATObject)
						plan.PopGlobalContext();
				}
			}
			finally
			{
				//APlan.ReleaseCatalogLock(AOperator);
			}
		}
		
		public static PlanNode CompileCreateConstraintStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateConstraintStatement localStatement = (CreateConstraintStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateConstraint);
			plan.Symbols.PushWindow(0); // Make sure the create constraint statement is evaluated in a private context
			try
			{
				CreateConstraintNode node = new CreateConstraintNode();
				string constraintName = Schema.Object.Qualify(localStatement.ConstraintName, plan.CurrentLibrary.Name);
				string sessionConstraintName = null;
				if (localStatement.IsSession)
				{
					sessionConstraintName = constraintName;
					if (plan.IsEngine)
						constraintName = MetaData.GetTag(localStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						constraintName = Schema.Object.NameFromGuid(Guid.NewGuid());
					CheckValidSessionObjectName(plan, statement, sessionConstraintName);
					plan.PlanSessionObjects.Add(new Schema.SessionObject(sessionConstraintName, constraintName));
				} 
				
				CheckValidCatalogObjectName(plan, statement, constraintName);

				node.Constraint = new Schema.CatalogConstraint(Schema.Object.GetObjectID(localStatement.MetaData), constraintName);
				node.Constraint.SessionObjectName = sessionConstraintName;
				node.Constraint.SessionID = plan.SessionID;
				node.Constraint.IsGenerated = localStatement.IsSession;
				node.Constraint.Owner = plan.User;
				node.Constraint.Library = node.Constraint.IsGenerated ? null : plan.CurrentLibrary;
				node.Constraint.ConstraintType = Schema.ConstraintType.Database;
				node.Constraint.MetaData = localStatement.MetaData;
				node.Constraint.Enforced = GetEnforced(plan, node.Constraint.MetaData);
				plan.PlanCatalog.Add(node.Constraint);
				try
				{
					plan.PushCreationObject(node.Constraint);
					try
					{
						plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
						try
						{
						node.Constraint.Node = CompileBooleanExpression(plan, localStatement.Expression);
						if (!(node.Constraint.Node.IsFunctional && node.Constraint.Node.IsDeterministic))
							throw new CompilerException(CompilerException.Codes.InvalidConstraintExpression, localStatement.Expression);

						node.Constraint.Node = OptimizeNode(plan, node.Constraint.Node);						

						string customMessage = node.Constraint.GetCustomMessage();
						if (!String.IsNullOrEmpty(customMessage))
						{
							try
							{
								PlanNode violationMessageNode = CompileTypedExpression(plan, new D4.Parser().ParseExpression(customMessage), plan.DataTypes.SystemString);
								violationMessageNode = OptimizeNode(plan, violationMessageNode);
								node.Constraint.ViolationMessageNode = violationMessageNode;
							}
							catch (Exception exception)
							{
								throw new CompilerException(CompilerException.Codes.InvalidCustomConstraintMessage, localStatement.Expression, exception, node.Constraint.Name);
							}
						}
						
						node.Constraint.DetermineRemotable(plan.CatalogDeviceSession);
						
						return node;
					}
					finally
					{
							plan.PopCursorContext();
						}
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
				catch
				{
					plan.PlanCatalog.SafeRemove(node.Constraint);
					throw;
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan plan, Schema.Columns columns)
		{
			return EmitKeyIsNotNilNode(plan, "", columns, null);
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan plan, Schema.Columns columns, BitArray isNilable)
		{
			return EmitKeyIsNotNilNode(plan, "", columns, isNilable);
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan plan, string rowVarName, Schema.Columns columns)
		{
			return EmitKeyIsNotNilNode(plan, rowVarName, columns, null);
		}
		
		public static PlanNode EmitKeyIsNotNilNode(Plan plan, string rowVarName, Schema.Columns columns, BitArray isNilable)
		{
			PlanNode node = EmitKeyIsNilNode(plan, rowVarName, columns, isNilable);
			if (node != null)
				node = EmitUnaryNode(plan, Instructions.Not, node);
			return node;
		}
		
		// IsNil(AValue) {or IsNil(AValue)}
		public static PlanNode EmitKeyIsNilNode(Plan plan, Schema.Columns columns)
		{
			return EmitKeyIsNilNode(plan, "", columns, null);
		}
		
		public static PlanNode EmitKeyIsNilNode(Plan plan, Schema.Columns columns, BitArray isNilable)
		{
			return EmitKeyIsNilNode(plan, "", columns, isNilable);
		}
		
		public static PlanNode EmitKeyIsNilNode(Plan plan, string rowVarName, Schema.Columns columns)
		{
			return EmitKeyIsNilNode(plan, rowVarName, columns, null);
		}
		
		public static PlanNode EmitKeyIsNilNode(Plan plan, string rowVarName, Schema.Columns columns, BitArray isNilable)
		{
			PlanNode node = null;
			
			for (int index = 0; index < columns.Count; index++)
				if ((isNilable == null) || isNilable[index])
					node =
						AppendNode
						(
							plan,
							node,
							Instructions.Or,
							EmitUnaryNode
							(
								plan, 
								IsNilOperatorName, 
								rowVarName == String.Empty ? 
									EmitIdentifierNode(plan, columns[index].Name) : 
									CompileQualifierExpression(plan, new QualifierExpression(new IdentifierExpression(rowVarName), new IdentifierExpression(columns[index].Name)))
							)
						);
			
			return node;
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan plan, Schema.Columns columns)
		{
			return EmitKeyIsNotSpecialNode(plan, "", columns, null);
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan plan, Schema.Columns columns, BitArray hasSpecials)
		{
			return EmitKeyIsNotSpecialNode(plan, "", columns, hasSpecials);
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan plan, string rowVarName, Schema.Columns columns)
		{
			return EmitKeyIsNotSpecialNode(plan, rowVarName, columns, null);
		}
		
		public static PlanNode EmitKeyIsNotSpecialNode(Plan plan, string rowVarName, Schema.Columns columns, BitArray hasSpecials)
		{
			PlanNode node = EmitKeyIsSpecialNode(plan, rowVarName, columns, hasSpecials);
			if (node != null)
				return EmitUnaryNode(plan, Instructions.Not, node);
			return node;
		}
		
		// IsSpecial(AValue) {or IsSpecial(Value)}
		public static PlanNode EmitKeyIsSpecialNode(Plan plan, Schema.Columns columns)
		{
			return EmitKeyIsSpecialNode(plan, "", columns, null);
		}
		
		public static PlanNode EmitKeyIsSpecialNode(Plan plan, Schema.Columns columns, BitArray hasSpecials)
		{
			return EmitKeyIsSpecialNode(plan, "", columns, hasSpecials);
		}
		
		public static PlanNode EmitKeyIsSpecialNode(Plan plan, string rowVarName, Schema.Columns columns)
		{
			return EmitKeyIsSpecialNode(plan, rowVarName, columns, null);
		}
		
		public static PlanNode EmitKeyIsSpecialNode(Plan plan, string rowVarName, Schema.Columns columns, BitArray hasSpecials)
		{
			PlanNode node = null;
			
			for (int index = 0; index < columns.Count; index++)
				if ((hasSpecials == null) || hasSpecials[index])
					node = 
						AppendNode
						(
							plan, 
							node, 
							Instructions.Or, 
							EmitUnaryNode
							(
								plan, 
								IsSpecialOperatorName,
								rowVarName == String.Empty ?
									EmitIdentifierNode(plan, columns[index].Name) :
									CompileQualifierExpression(plan, new QualifierExpression(new IdentifierExpression(rowVarName), new IdentifierExpression(columns[index].Name)))
							)
						);
			
			return node;
		}
		
		// tags { DAE.Message = "'The table {0} does not have a row with <target column names> ' + <new.column names>.AsString + '." };
		public static string GetCustomMessageForSourceReference(Plan plan, Schema.Reference reference)
		{
			StringBuilder message = new StringBuilder();
			message.AppendFormat("'The table {0} does not have a row with ", MetaData.GetTag(reference.TargetTable.MetaData, "Frontend.Singular.Title", "Frontend.Title", Schema.Object.Unqualify(reference.TargetTable.DisplayName)));
			Schema.ScalarType scalarType;
			Schema.Representation representation;
			
			for (int index = 0; index < reference.TargetKey.Columns.Count; index++)
			{
				if (index > 0)
					message.Append(" and ");
				message.AppendFormat("{0} ", MetaData.GetTag(reference.TargetTable.Columns[reference.TargetKey.Columns[index].Name].MetaData, "Frontend.Title", reference.TargetKey.Columns[index].Name));
				scalarType = (Schema.ScalarType)reference.TargetKey.Columns[index].DataType;
				representation = scalarType.FindRepresentation(NativeAccessors.AsDisplayString);
				bool isString = scalarType.NativeType == NativeAccessors.AsDisplayString.NativeType;
				if (isString)
					message.AppendFormat(@"""' + {0}{1}{2}{3}{4} + '""", new object[]{Keywords.New, Keywords.Qualifier, reference.SourceKey.Columns[index].Name, Keywords.Qualifier, representation.Properties[0].Name});
				else
					message.AppendFormat(@"(' + {0}{1}{2}{3}{4} + ')", new object[]{Keywords.New, Keywords.Qualifier, reference.SourceKey.Columns[index].Name, Keywords.Qualifier, representation.Properties[0].Name});
			}

			message.Append(".'");
			return message.ToString();
		}

		// tags { DAE.Message = "'The table {0} has rows with <source column names> ' + <old.column names>.AsString + '." };
		public static string GetCustomMessageForTargetReference(Plan plan, Schema.Reference reference)
		{
			StringBuilder message = new StringBuilder();
			message.AppendFormat("'The table {0} has rows with ", MetaData.GetTag(reference.SourceTable.MetaData, "Frontend.Singular.Title", "Frontend.Title", Schema.Object.Unqualify(reference.SourceTable.DisplayName)));
			Schema.ScalarType scalarType;
			Schema.Representation representation;
			
			for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
			{
				if (index > 0)
					message.Append(" and ");
				message.AppendFormat("{0} ", MetaData.GetTag(reference.SourceTable.Columns[reference.SourceKey.Columns[index].Name].MetaData, "Frontend.Title", reference.SourceKey.Columns[index].Name));
				scalarType = (Schema.ScalarType)reference.SourceKey.Columns[index].DataType;
				representation = scalarType.FindRepresentation(NativeAccessors.AsDisplayString);
				bool isString = scalarType.NativeType == NativeAccessors.AsDisplayString.NativeType;
				if (isString)
					message.AppendFormat(@"""' + {0}{1}{2}{3}{4} + '""", new object[]{Keywords.Old, Keywords.Qualifier, reference.TargetKey.Columns[index].Name, Keywords.Qualifier, representation.Properties[0].Name});
				else
					message.AppendFormat(@"(' + {0}{1}{2}{3}{4} + ')", new object[]{Keywords.Old, Keywords.Qualifier, reference.TargetKey.Columns[index].Name, Keywords.Qualifier, representation.Properties[0].Name});
			}

			message.Append(".'");
			return message.ToString();
		}
		
		public static Schema.TransitionConstraint CompileSourceReferenceConstraint(Plan plan, Schema.Reference reference)
		{
			plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
			try
			{
				Schema.TransitionConstraint constraint = new Schema.TransitionConstraint(String.Format("{0}{1}", "Source", reference.Name));
				constraint.Library = reference.Library;
				constraint.MergeMetaData(reference.MetaData);
				if (constraint.MetaData == null)
					constraint.MetaData = new MetaData();
				if (!(constraint.MetaData.Tags.Contains("DAE.Message") || constraint.MetaData.Tags.Contains("DAE.SimpleMessage")) && CanBuildCustomMessageForKey(plan, reference.SourceKey.Columns))
					constraint.MetaData.Tags.Add(new Tag("DAE.Message", GetCustomMessageForSourceReference(plan, reference)));
				constraint.IsGenerated = true;
				constraint.ConstraintType = Schema.ConstraintType.Database;
				CompileSourceInsertConstraintNodeForReference(plan, reference, constraint);
				constraint.InsertColumnFlags = new BitArray(reference.SourceTable.DataType.Columns.Count);
				for (int index = 0; index < constraint.InsertColumnFlags.Length; index++)
					constraint.InsertColumnFlags[index] = reference.SourceKey.Columns.ContainsName(reference.SourceTable.DataType.Columns[index].Name);
				CompileSourceUpdateConstraintNodeForReference(plan, reference, constraint);
				constraint.UpdateColumnFlags = (BitArray)constraint.InsertColumnFlags.Clone();
				return constraint;
			}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		// Construct an insert constraint to validate rows inserted into the source table of the reference
		// Used by all reference types.
		//
		// on insert into A ->
		//		[if [IsNil(AValues) or] [IsSpecial(AValues) or] then true else] exists(B where BKeys = AValues)
		public static void CompileSourceInsertConstraintNodeForReference(Plan plan, Schema.Reference reference, Schema.TransitionConstraint constraint)
		{
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				Schema.IRowType rowType = new Schema.RowType(reference.SourceKey.Columns);
				plan.Symbols.Push(new Symbol(Keywords.New, reference.SourceTable.DataType.RowType));
				#else
				Schema.IRowType rowType = new Schema.RowType(AReference.SourceKey.Columns, Keywords.New);
				APlan.Symbols.Push(new Symbol(String.Empty, AReference.SourceTable.DataType.NewRowType));
				#endif
				try
				{
					BitArray isNilable = new BitArray(reference.SourceKey.Columns.Count);
					BitArray hasSpecials = new BitArray(reference.SourceKey.Columns.Count);
					for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
					{
						isNilable[index] = reference.SourceKey.Columns[index].IsNilable;
						hasSpecials[index] = 
							(reference.SourceKey.Columns[index].DataType is Schema.ScalarType) 
								&& (((Schema.ScalarType)reference.SourceKey.Columns[index].DataType).Specials.Count > 0);
					}

					PlanNode testNode =
						AppendNode
						(
							plan,
							#if USENAMEDROWVARIABLES
							EmitKeyIsNilNode(plan, Keywords.New, rowType.Columns, isNilable),
							#else
							EmitKeyIsNilNode(APlan, rowType.Columns, isNilable),
							#endif
							Instructions.Or,
							#if USENAMEDROWVARIABLES
							EmitKeyIsSpecialNode(plan, Keywords.New, rowType.Columns, hasSpecials)
							#else
							EmitKeyIsSpecialNode(APlan, rowType.Columns, hasSpecials)
							#endif
						);

					PlanNode existsNode =
						EmitUnaryNode
						(
							plan, 
							Instructions.Exists, 
							EmitRestrictNode
							(
								plan,
								EmitTableVarNode(plan, reference.TargetTable), 
								#if USENAMEDROWVARIABLES
								BuildKeyEqualExpression
								(
									plan,
									String.Empty,
									Keywords.New,
									reference.TargetKey.Columns,
									reference.SourceKey.Columns
								)
								#else
								BuildKeyEqualExpression
								(
									APlan,
									new Schema.RowType(AReference.TargetKey.Columns).Columns, 
									rowType.Columns
								)
								#endif
							)
						);
						
					PlanNode node = 
						testNode == null ? existsNode : EmitConditionNode(plan, testNode, new ValueNode(plan.DataTypes.SystemBoolean, true), existsNode);

					constraint.OnInsertNode = OptimizeNode(plan, node);
					constraint.OnInsertViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, constraint, Schema.Transition.Insert, null);
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		// Construct an update constraint to validate rows updated in the source table of the reference
		// Used by all reference types.
		//
		// on update of A ->
		//		if IsNil(NewAValues) or IsSpecial(NewAValues) or (OldAValues = NewAValues) then true else (exists(B where BKeys = NewAValues))
		public static void CompileSourceUpdateConstraintNodeForReference(Plan plan, Schema.Reference reference, Schema.TransitionConstraint constraint)
		{
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Old, reference.SourceTable.DataType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, AReference.SourceTable.DataType.OldRowType));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.New, reference.SourceTable.DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, AReference.SourceTable.DataType.NewRowType));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						Schema.IRowType sourceRowType = new Schema.RowType(reference.SourceKey.Columns);
						#else
						Schema.IRowType newSourceRowType = new Schema.RowType(AReference.SourceKey.Columns, Keywords.New);
						Schema.IRowType oldSourceRowType = new Schema.RowType(AReference.SourceKey.Columns, Keywords.Old);
						#endif

						BitArray isNilable = new BitArray(reference.SourceKey.Columns.Count);
						BitArray hasSpecials = new BitArray(reference.SourceKey.Columns.Count);
						for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
						{
							isNilable[index] = reference.SourceKey.Columns[index].IsNilable;
							hasSpecials[index] =
								(reference.SourceKey.Columns[index].DataType is Schema.ScalarType) 
									&& (((Schema.ScalarType)reference.SourceKey.Columns[index].DataType).Specials.Count > 0);
						}
						
						#if USENAMEDROWVARIABLES
						PlanNode equalNode = CompileExpression(plan, BuildKeyEqualExpression(plan, Keywords.Old, Keywords.New, sourceRowType.Columns, sourceRowType.Columns));
						#else
						PlanNode equalNode = CompileExpression(APlan, BuildKeyEqualExpression(APlan, oldSourceRowType.Columns, newSourceRowType.Columns));
						#endif

						PlanNode testNode = 
							AppendNode
							(
								plan,
								#if USENAMEDROWVARIABLES
								EmitKeyIsNilNode(plan, Keywords.New, sourceRowType.Columns, isNilable),
								#else
								EmitKeyIsNilNode(APlan, newSourceRowType.Columns, isNilable),
								#endif
								Instructions.Or,
								AppendNode
								(
									plan,
									#if USENAMEDROWVARIABLES
									EmitKeyIsSpecialNode(plan, Keywords.New, sourceRowType.Columns, hasSpecials),
									#else
									EmitKeyIsSpecialNode(APlan, newSourceRowType.Columns, hasSpecials),
									#endif
									Instructions.Or,
									equalNode
								)
							);

						PlanNode existsNode =
							EmitUnaryNode
							(
								plan, 
								Instructions.Exists, 
								EmitRestrictNode
								(
									plan, 
									EmitTableVarNode(plan, reference.TargetTable), 
									#if USENAMEDROWVARIABLES
									BuildKeyEqualExpression
									(
										plan,
										String.Empty,
										Keywords.New,
										reference.TargetKey.Columns,
										reference.SourceKey.Columns
									)
									#else
									BuildKeyEqualExpression
									(
										APlan,
										new Schema.RowType(AReference.TargetKey.Columns).Columns, 
										newSourceRowType.Columns
									)
									#endif
								)
							);

						PlanNode node = testNode == null ? existsNode : EmitConditionNode(plan, testNode, new ValueNode(plan.DataTypes.SystemBoolean, true), existsNode);

						constraint.OnUpdateNode = OptimizeNode(plan, node);
						constraint.OnUpdateViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, constraint, Schema.Transition.Update, null);
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		public static Schema.TransitionConstraint CompileTargetReferenceConstraint(Plan plan, Schema.Reference reference)
		{
			plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
			try
			{
				Schema.TransitionConstraint constraint = new Schema.TransitionConstraint(String.Format("{0}{1}", "Target", reference.Name));
				constraint.Library = reference.Library;
				constraint.MergeMetaData(reference.MetaData);
				if (constraint.MetaData == null)
					constraint.MetaData = new MetaData();
				if (!(constraint.MetaData.Tags.Contains("DAE.Message") || constraint.MetaData.Tags.Contains("DAE.SimpleMessage")) && CanBuildCustomMessageForKey(plan, reference.TargetKey.Columns))
					constraint.MetaData.Tags.Add(new Tag("DAE.Message", GetCustomMessageForTargetReference(plan, reference)));
				constraint.IsGenerated = true;
				constraint.ConstraintType = Schema.ConstraintType.Database;
			
				if (reference.UpdateReferenceAction == ReferenceAction.Require)
				{
					CompileTargetUpdateConstraintNodeForReference(plan, reference, constraint);
					constraint.UpdateColumnFlags = new BitArray(reference.TargetTable.DataType.Columns.Count);
					for (int index = 0; index < constraint.UpdateColumnFlags.Length; index++)
						constraint.UpdateColumnFlags[index] = reference.TargetKey.Columns.ContainsName(reference.TargetTable.DataType.Columns[index].Name);
				}
				
				if (reference.DeleteReferenceAction == ReferenceAction.Require)
				{
					CompileTargetDeleteConstraintNodeForReference(plan, reference, constraint);
					constraint.DeleteColumnFlags = new BitArray(reference.TargetTable.DataType.Columns.Count);
					for (int index = 0; index < constraint.DeleteColumnFlags.Length; index++)
						constraint.DeleteColumnFlags[index] = reference.TargetKey.Columns.ContainsName(reference.TargetTable.DataType.Columns[index].Name);
				}
				
				return constraint;
			}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		// Construct an update constraint to validate rows updated in the target table of the reference
		// Used exclusively by the Require reference type.
		//		
		// on update of B ->
		//		if (OldBValues = NewBValues) then true else (not(exists(A where AKeys = OldBValues)))
		public static void CompileTargetUpdateConstraintNodeForReference(Plan plan, Schema.Reference reference, Schema.TransitionConstraint constraint)
		{
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Old, reference.TargetTable.DataType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, AReference.TargetTable.DataType.OldRowType));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.New, reference.TargetTable.DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, AReference.TargetTable.DataType.NewRowType));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						Schema.IRowType targetRowType = new Schema.RowType(reference.TargetKey.Columns);
						#else
						Schema.IRowType oldTargetRowType = new Schema.RowType(AReference.TargetKey.Columns, Keywords.Old);
						Schema.IRowType newTargetRowType = new Schema.RowType(AReference.TargetKey.Columns, Keywords.New);
						#endif

						#if USENAMEDROWVARIABLES
						PlanNode equalNode = CompileExpression(plan, BuildKeyEqualExpression(plan, Keywords.Old, Keywords.New, targetRowType.Columns, targetRowType.Columns));
						#else
						PlanNode equalNode2 = CompileExpression(APlan, BuildKeyEqualExpression(APlan, oldTargetRowType.Columns, newTargetRowType.Columns));
						#endif
						
						PlanNode node = 
							EmitConditionNode
							(
								plan, 
								equalNode, 
								new ValueNode(plan.DataTypes.SystemBoolean, true), 
								EmitUnaryNode
								(
									plan, 
									Instructions.Not, 
									EmitUnaryNode
									(
										plan, 
										Instructions.Exists, 
										EmitRestrictNode
										(
											plan, 
											EmitTableVarNode(plan, reference.SourceTable), 
											#if USENAMEDROWVARIABLES
											BuildKeyEqualExpression
											(
												plan,
												String.Empty,
												Keywords.Old,
												reference.SourceKey.Columns,
												reference.TargetKey.Columns
											)
											#else
											BuildKeyEqualExpression
											(
												plan,
												new Schema.RowType(AReference.SourceKey.Columns).Columns, 
												oldTargetRowType.Columns
											)
											#endif
										)
									)
								)
							);

						constraint.OnUpdateNode = OptimizeNode(plan, node);
						constraint.OnUpdateViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, constraint, Schema.Transition.Update, null);
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		// Construct a delete constraint to validate rows deleted in the target table of the reference
		// Used exclusively by the Require reference type.
		//
		// on delete from B ->
		//		not(exists(A where AKeys = BValues))
		public static void CompileTargetDeleteConstraintNodeForReference(Plan plan, Schema.Reference reference, Schema.TransitionConstraint constraint)
		{
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Old, reference.TargetTable.DataType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, AReference.TargetTable.DataType.OldRowType));
				#endif
				try
				{
					PlanNode node = 
						EmitUnaryNode
						(
							plan, 
							Instructions.Not, 
							EmitUnaryNode
							(
								plan, 
								Instructions.Exists, 
								EmitRestrictNode
								(
									plan, 
									EmitTableVarNode(plan, reference.SourceTable), 
									#if USENAMEDROWVARIABLES
									BuildKeyEqualExpression
									(
										plan,
										String.Empty,
										Keywords.Old,
										reference.SourceKey.Columns,
										reference.TargetKey.Columns
									)
									#else
									BuildKeyEqualExpression
									(
										APlan,
										new Schema.RowType(AReference.SourceKey.Columns).Columns, 
										new Schema.RowType(AReference.TargetKey.Columns, Keywords.Old).Columns
									)
									#endif
								)
							)
						);

					constraint.OnDeleteNode = OptimizeNode(plan, node);
					constraint.OnDeleteViolationMessageNode = CompileTransitionConstraintViolationMessageNode(plan, constraint, Schema.Transition.Delete, null);
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		// Constructs an update statement used to cascade updates on the target table.
		// Used exclusively by the Cascade reference type.
		//
		// on update of B ->
		//	if (NewBValues <> OldBValues)
		//		update A set { AKeys = NewBValues } where AKeys = OldBValues;
		public static PlanNode CompileUpdateCascadeNodeForReference(Plan plan, PlanNode sourceNode, Schema.Reference reference)
		{
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Old, reference.TargetTable.DataType.RowType));
				#else
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.New, reference.TargetTable.DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol("ANewRow", AReference.TargetTable.DataType.RowType));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						PlanNode conditionNode = CompileExpression(plan, new UnaryExpression(Instructions.Not, BuildKeyEqualExpression(plan, Keywords.Old, Keywords.New, reference.TargetKey.Columns, reference.TargetKey.Columns)));
						#else
						PlanNode conditionNode = CompileExpression(APlan, new UnaryExpression(Instructions.Not, BuildKeyEqualExpression(APlan, "AOldRow", "ANewRow", AReference.TargetKey.Columns, AReference.TargetKey.Columns)));
						#endif
						UpdateNode updateNode = new UpdateNode();
						updateNode.IsBreakable = false;
						plan.Symbols.Push(new Symbol(String.Empty, ((Schema.ITableType)sourceNode.DataType).RowType));
						try
						{
							#if USENAMEDROWVARIABLES
							updateNode.Nodes.Add(EmitUpdateConditionNode(plan, sourceNode, CompileExpression(plan, BuildKeyEqualExpression(plan, String.Empty, Keywords.Old, reference.SourceKey.Columns, reference.TargetKey.Columns))));
							#else
							updateNode.Nodes.Add(EmitUpdateConditionNode(APlan, ASourceNode, CompileExpression(APlan, BuildKeyEqualExpression(APlan, String.Empty, "AOldRow", AReference.SourceKey.Columns, AReference.TargetKey.Columns))));
							#endif
							updateNode.TargetNode = updateNode.Nodes[0].Nodes[0];
							updateNode.ConditionNode = updateNode.Nodes[0].Nodes[1];

							for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
							{
								Schema.TableVarColumn sourceColumn = reference.SourceKey.Columns[index];
								Schema.TableVarColumn targetColumn = reference.TargetKey.Columns[index];
								updateNode.Nodes.Add
								(
									new UpdateColumnNode
									(
										sourceColumn.DataType,
										sourceColumn.Name,
										#if USENAMEDROWVARIABLES
										CompileQualifierExpression(plan, new QualifierExpression(new IdentifierExpression(Keywords.New), new IdentifierExpression(targetColumn.Name)))
										#else
										CompileQualifierExpression(APlan, new QualifierExpression(new IdentifierExpression("ANewRow"), new IdentifierExpression(LTargetColumn.Name)))
										#endif
									)
								);
							}
							
							updateNode.DetermineDataType(plan);
							updateNode.DetermineCharacteristics(plan);
						}
						finally
						{
							plan.Symbols.Pop();
						}

						var planNode = EmitIfNode(plan, null, conditionNode, updateNode, null);
						planNode = OptimizeNode(plan, planNode);
						return planNode;
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		// Constructs a delete statement used to cascade deletes on the target table.
		// Used exclusively by the Cascade reference type.
		//
		// on delete from B ->
		//		delete A where AKeys = BValues
		public static PlanNode CompileDeleteCascadeNodeForReference(Plan plan, PlanNode sourceNode, Schema.Reference reference)
		{
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Old, reference.TargetTable.DataType.RowType));
				#else
				APlan.Symbols.Push(new Symbol("AOldRow", AReference.TargetTable.DataType.RowType));
				#endif
				try
				{
					DeleteNode node = new DeleteNode();
					node.IsBreakable = false;
					plan.Symbols.Push(new Symbol(String.Empty, reference.SourceTable.DataType.RowType));
					try
					{
						#if USENAMEDROWVARIABLES
						node.Nodes.Add(EmitRestrictNode(plan, sourceNode, CompileExpression(plan, BuildKeyEqualExpression(plan, String.Empty, Keywords.Old, reference.SourceKey.Columns, reference.TargetKey.Columns))));
						#else
						node.Nodes.Add(EmitRestrictNode(APlan, ASourceNode, CompileExpression(APlan, BuildKeyEqualExpression(APlan, String.Empty, "AOldRow", AReference.SourceKey.Columns, AReference.TargetKey.Columns))));
						#endif
					}
					finally
					{
						plan.Symbols.Pop();
					}

					node.DetermineDataType(plan);
					node.DetermineCharacteristics(plan);
					return OptimizeNode(plan, node);
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
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
		public static PlanNode CompileUpdateNodeForReference(Plan plan, PlanNode sourceNode, Schema.Reference reference, PlanNode[] expressionNodes, bool isUpdate)
		{
			BlockNode blockNode = new BlockNode();
			blockNode.IsBreakable = false;
			VariableNode varNode = new VariableNode();
			varNode.IsBreakable = false;
			RowSelectorNode rowNode = new RowSelectorNode(new Schema.RowType(reference.SourceKey.Columns));
			for (int index = 0; index < rowNode.DataType.Columns.Count; index++)
				rowNode.Nodes.Add(expressionNodes[index]);
			varNode.Nodes.Add(OptimizeNode(plan, rowNode));
			varNode.VariableName = "ARow";
			varNode.VariableType = rowNode.DataType;
			blockNode.Nodes.Add(varNode); // Do not bind the varnode, it is unnecessary and causes ARow to appear on the stack twice.
			IfNode ifNode = new IfNode();
			ifNode.IsBreakable = false;
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol("AOldRow", reference.TargetTable.DataType.RowType));
				try
				{
					if (isUpdate)
						plan.Symbols.Push(new Symbol("ANewRow", reference.TargetTable.DataType.RowType));
					try
					{
						plan.Symbols.Push(new Symbol(varNode.VariableName, varNode.VariableType));
						try
						{
							ifNode.Nodes.Add
							(
								OptimizeNode
								(
									plan,
									AppendNode
									(
										plan,
										EmitKeyIsNilNode(plan, "ARow", rowNode.DataType.Columns),
										Instructions.Or,
										AppendNode
										(
											plan,
											EmitKeyIsSpecialNode(plan, "ARow", rowNode.DataType.Columns),
											Instructions.Or,
											EmitUnaryNode
											(
												plan, 
												Instructions.Exists, 
												EmitRestrictNode
												(
													plan,
													EmitTableVarNode(plan, reference.TargetTable), 
													BuildKeyEqualExpression
													(
														plan,
														"",
														"ARow",
														reference.TargetKey.Columns,
														reference.SourceKey.Columns // The row variable is declared using the source key column names
													)
												)
											)
										)
									)
								)
							);

							ifNode.Nodes.Add(CompileSetNodeForReference(plan, sourceNode, reference, expressionNodes));

							RaiseNode raiseNode = new RaiseNode();
							raiseNode.IsBreakable = false;
							raiseNode.Nodes.Add(EmitUnaryNode(plan, "System.Error", new ValueNode(plan.DataTypes.SystemString, new RuntimeException(RuntimeException.Codes.InsertConstraintViolation, reference.Name, reference.TargetTable.Name, String.Empty).Message))); // Schema.Constraint.GetViolationMessage(AReference.MetaData)).Message))));
							ifNode.Nodes.Add(OptimizeNode(plan, raiseNode));
							ifNode.DeterminePotentialDevice(plan);
							ifNode.DetermineDevice(plan);
							ifNode.DetermineAccessPath(plan);
							blockNode.Nodes.Add(ifNode);
							blockNode.Nodes.Add(new DropVariableNode());
						}
						finally
						{
							plan.Symbols.Pop();
						}

						if (isUpdate)
						{
							ifNode = new IfNode();
							ifNode.IsBreakable = false;
							ifNode.Nodes.Add(OptimizeNode(plan, CompileExpression(plan, new UnaryExpression(Instructions.Not, BuildKeyEqualExpression(plan, "AOldRow", "ANewRow", reference.TargetKey.Columns, reference.TargetKey.Columns)))));
							ifNode.Nodes.Add(blockNode);
							return ifNode;
						}
						else
							return blockNode;
					}
					finally
					{
						if (isUpdate)
							plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		//	update A set { AKeys = AValues } where AKeys = OldBValues 
		public static UpdateNode CompileSetNodeForReference(Plan plan, PlanNode sourceNode, Schema.Reference reference, PlanNode[] expressionNodes)
		{
			plan.EnterRowContext();
			try
			{
				UpdateNode node = new UpdateNode();
				node.IsBreakable = false;
				plan.Symbols.Push(new Symbol(String.Empty, ((Schema.ITableType)sourceNode.DataType).RowType));
				try
				{
					node.Nodes.Add
					(
						EmitUpdateConditionNode
						(
							plan, 
							sourceNode, 
							CompileExpression
							(
								plan, 
								BuildKeyEqualExpression
								(
									plan, 
									"",
									"AOldRow",
									reference.SourceKey.Columns,
									reference.TargetKey.Columns
								)
							)
						)
					);
					node.TargetNode = node.Nodes[0].Nodes[0];
					node.ConditionNode = node.Nodes[0].Nodes[1];

					for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
					{
						Schema.TableVarColumn sourceColumn = reference.SourceKey.Columns[index];
						Schema.TableVarColumn targetColumn = reference.TargetKey.Columns[index];
						node.Nodes.Add
						(
							new UpdateColumnNode
							(
								sourceColumn.DataType,
								sourceColumn.Name,
								expressionNodes[index]
							)
						);
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}

				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				node = (UpdateNode)OptimizeNode(plan, node);
				return node;
			}
			finally
			{
				plan.ExitRowContext();
			}
		}

		// Constructs an update statement used to set references to the target table to the given expression.
		// Used by the Clear and Set Reference Actions.
		//
		// on update of B ->
		//		update A where AKeys = OldBValues set AKeys = AExpressionNode
		public static UpdateNode CompileUpdateSetNodeForReference(Plan plan, PlanNode sourceNode, Schema.Reference reference, PlanNode[] expressionNodes)
		{
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol("AOldRow", reference.TargetTable.DataType.RowType));
				try
				{
					plan.Symbols.Push(new Symbol("ANewRow", reference.TargetTable.DataType.RowType));
					try
					{
						return CompileSetNodeForReference(plan, sourceNode, reference, expressionNodes);
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		public static UpdateNode CompileDeleteSetNodeForReference(Plan plan, PlanNode sourceNode, Schema.Reference reference, PlanNode[] expressionNodes)
		{
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol("AOldRow", reference.TargetTable.DataType.RowType));
				try
				{
					return CompileSetNodeForReference(plan, sourceNode, reference, expressionNodes);
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		protected static RenameColumnExpressions CompileRenameColumnExpressionsForReference(Plan plan, Schema.Reference reference)
		{
			RenameColumnExpressions result = new RenameColumnExpressions();
			for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
				result.Add(new RenameColumnExpression(reference.SourceKey.Columns[index].Name, reference.TargetKey.Columns[index].Name));
			return result;
		}

		// Constructs a catalog constraint to enforce the key
		//
		// Given a table T { <keys>, <nonkeys>, key { <keys> } }, constructs a constraint
		//	Count(T over { <keys> }) = Count(T)
		// For a sparse key: 
		//  Count(T where <keys>.IsNotNil() { <keys> }) = Count(T where <keys>.IsNotNil())
		public static Schema.CatalogConstraint CompileCatalogConstraintForKey(Plan plan, Schema.TableVar tableVar, Schema.Key key)
		{
			Schema.CatalogConstraint constraint = new Schema.CatalogConstraint(Schema.Object.Qualify(key.Name, tableVar.Name));
			constraint.Owner = tableVar.Owner;
			constraint.Library = tableVar.Library;
			constraint.MergeMetaData(key.MetaData);
			constraint.ConstraintType = Schema.ConstraintType.Database;
			constraint.IsDeferred = false;
			constraint.IsGenerated = true;

			plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
			try
			{
			constraint.Node = 
				key.IsSparse 
					?
						CompileBooleanExpression
						(
							plan, 
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
												new IdentifierExpression(tableVar.Name),
												BuildKeyIsNotNilExpression(plan, key.Columns)
											),
											key.Columns.ColumnNames
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
											new IdentifierExpression(tableVar.Name),
											BuildKeyIsNotNilExpression(plan, key.Columns)
										)
									}
								)
							)
						)
					:
						CompileBooleanExpression
						(
							plan, 
							new BinaryExpression
							(
								new CallExpression
								(
									"System.Count", 
									new Expression[]
									{
										new ProjectExpression
										(
											new IdentifierExpression(tableVar.Name), 
											key.Columns.ColumnNames
										)
									}
								), 
								Instructions.Equal, 
								new CallExpression
								(
									"System.Count", 
									new Expression[]{new IdentifierExpression(tableVar.Name)}
								)
							)
						);
				
			constraint.Node = OptimizeNode(plan, constraint.Node);
			return constraint;
		}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		// Constructs a catalog constraint to enforce the reference
		//
		// not(exists((source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) over {keys} rename {target keys}) minus (target over {keys})))
		// equivalent formulation to facilitate translation into SQL
		// not(exists(source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) and not(exists(target where target.keys = source.keys))))
		public static Schema.CatalogConstraint CompileCatalogConstraintForReference(Plan plan, Schema.Reference reference)
		{
			Schema.CatalogConstraint constraint = new Schema.CatalogConstraint(Schema.Object.Qualify(reference.Name, Keywords.Reference));
			constraint.Owner = plan.User;
			constraint.Library = reference.Library;
			constraint.MergeMetaData(reference.MetaData);
			constraint.ConstraintType = Schema.ConstraintType.Database;
			constraint.IsGenerated = true;

			plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
			try
			{
//			if (AReference.IsSessionObject)
//				LConstraint.SessionObjectName = LConstraint.Name; // This is to allow the constraint to reference session-specific objects if necessary
//			APlan.PushCreationObject(LConstraint);
//			try
//			{
				PlanNode sourceNode = EmitIdentifierNode(plan, reference.SourceTable.Name);
				PlanNode targetNode = EmitIdentifierNode(plan, reference.TargetTable.Name);

				#if UseMinusForReferenes
				// not(exists((source where not(source.keys.IsNil()) and not(source.keys.IsSpecial()) over {keys} rename {target keys}) minus (target over {keys})))
				constraint.Node =
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
				Schema.Columns sourceKeyColumns = new Schema.Columns();
				foreach (Schema.TableVarColumn sourceKeyColumn in reference.SourceKey.Columns)
					sourceKeyColumns.Add(new Schema.Column(Schema.Object.Qualify(sourceKeyColumn.Name, Keywords.Source), sourceKeyColumn.DataType));
				plan.EnterRowContext();
				try
				{
					plan.Symbols.Push(new Symbol(String.Empty, ((TableNode)sourceNode).DataType.CreateRowType(Keywords.Source)));
					try
					{
						plan.Symbols.Push(new Symbol(String.Empty, ((TableNode)targetNode).DataType.CreateRowType(Keywords.Target)));
 						try
						{
							BitArray isNilable = new BitArray(reference.SourceKey.Columns.Count);
							BitArray hasSpecials = new BitArray(reference.SourceKey.Columns.Count);
							for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
							{
								isNilable[index] = reference.SourceKey.Columns[index].IsNilable;
								hasSpecials[index] = 
									(reference.SourceKey.Columns[index].DataType is Schema.ScalarType) 
										&& (((Schema.ScalarType)reference.SourceKey.Columns[index].DataType).Specials.Count > 0);
							}
							
							constraint.Node =
								EmitUnaryNode
								(
									plan,
									Instructions.Not,
									EmitUnaryNode
									(
										plan,
										Instructions.Exists,
										EmitRestrictNode
										(
											plan,
											EmitRenameNode(plan, sourceNode, Keywords.Source, null),
											AppendNode
											(
												plan,
												EmitKeyIsNotNilNode(plan, sourceKeyColumns, isNilable),
												Instructions.And,
												AppendNode
												(
													plan,
													EmitKeyIsNotSpecialNode(plan, sourceKeyColumns, hasSpecials),
													Instructions.And,
													EmitUnaryNode
													(
														plan,
														Instructions.Not,
														EmitUnaryNode
														(
															plan,
															Instructions.Exists,
															EmitRestrictNode
															(
																plan,
																EmitRenameNode(plan, targetNode, Keywords.Target, null),
																CompileExpression
																(
																	plan,
																	BuildKeyEqualExpression
																	(
																		plan,
																		new Schema.RowType(reference.TargetKey.Columns, Keywords.Target).Columns,
																		new Schema.RowType(reference.SourceKey.Columns, Keywords.Source).Columns
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

							constraint.Node = OptimizeNode(plan, constraint.Node);
							return constraint;
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.ExitRowContext();
				}
				#endif
//			}
//			finally
//			{
//				APlan.PopCreationObject();
//			}
		}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		protected static void AddSpecialsForScalarType(Schema.ScalarType scalarType, Schema.Objects specials)
		{
			specials.AddRange(scalarType.Specials);
			#if USETYPEINHERITANCE
			foreach (Schema.ScalarType parentType in AScalarType.ParentTypes)
				AddSpecialsForScalarType(parentType, ASpecials);
			#endif
		}
		
		/// <summary>Returns the default special for the given type. Will throw an exception if the type is not scalar, or the type has multiple specials defined. Will return null if the type has no specials defined.</summary>
		protected static Schema.Special FindDefaultSpecialForScalarType(Plan plan, Schema.IDataType dataType)
		{
			if (!(dataType is Schema.ScalarType))
				throw new CompilerException(CompilerException.Codes.UnableToFindDefaultSpecial, plan.CurrentStatement(), dataType.Name);
			Schema.ScalarType scalarType = (Schema.ScalarType)dataType;
			Schema.Objects specials = new Schema.Objects();
			AddSpecialsForScalarType(scalarType, specials);
			switch (specials.Count)
			{
				case 0 : return null;
				case 1 : return (Schema.Special)specials[0];
				default : throw new CompilerException(CompilerException.Codes.AmbiguousClearValue, plan.CurrentStatement(), scalarType.Name);
			}
		}
		
		public static PlanNode EmitNilNode(Plan plan)
		{
			return CompileValueExpression(plan, new ValueExpression(null, TokenType.Nil));
		}
		
		public static PlanNode CompileCreateReferenceStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateReferenceStatement localStatement = (CreateReferenceStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateReference);
			plan.Symbols.PushWindow(0); // Make sure the create reference statement is evaluated in a private context
			try
			{
				CreateReferenceNode node = new CreateReferenceNode();
				string referenceName = Schema.Object.Qualify(localStatement.ReferenceName, plan.CurrentLibrary.Name);
				string sessionReferenceName = null;
				if (localStatement.IsSession)
				{
					sessionReferenceName = referenceName;
					if (plan.IsEngine)
						referenceName = MetaData.GetTag(localStatement.MetaData, "DAE.GlobalObjectName", Schema.Object.NameFromGuid(Guid.NewGuid()));
					else
						referenceName = Schema.Object.NameFromGuid(Guid.NewGuid());
					CheckValidSessionObjectName(plan, statement, sessionReferenceName);
					plan.PlanSessionObjects.Add(new Schema.SessionObject(sessionReferenceName, referenceName));
				} 
				
				CheckValidCatalogObjectName(plan, statement, referenceName);

				node.Reference = new Schema.Reference(Schema.Object.GetObjectID(localStatement.MetaData), referenceName);
				node.Reference.SessionObjectName = sessionReferenceName;
				node.Reference.SessionID = plan.SessionID;
				node.Reference.IsGenerated = localStatement.IsSession;
				node.Reference.Owner = plan.User;
				node.Reference.Library = node.Reference.IsGenerated ? null : plan.CurrentLibrary;
				node.Reference.MetaData = localStatement.MetaData;
				node.Reference.Enforced = GetEnforced(plan, node.Reference.MetaData);
				plan.PlanCatalog.Add(node.Reference);
				try
				{
					plan.PushCreationObject(node.Reference);
					try
					{
						Schema.Object schemaObject = ResolveCatalogIdentifier(plan, localStatement.TableVarName, true);
						if (!(schemaObject is Schema.TableVar))
							throw new CompilerException(CompilerException.Codes.InvalidReferenceObject, statement, localStatement.ReferenceName, localStatement.TableVarName);
						node.Reference.SourceTable = (Schema.TableVar)schemaObject;
						plan.AttachDependency(schemaObject);
						foreach (ReferenceColumnDefinition column in localStatement.Columns)
							node.Reference.SourceKey.Columns.Add(node.Reference.SourceTable.Columns[column.ColumnName]);
						foreach (Schema.Key key in node.Reference.SourceTable.Keys)
							if (node.Reference.SourceKey.Columns.IsSupersetOf(key.Columns))
							{
								node.Reference.SourceKey.IsUnique = true;
								break;
							}

						schemaObject = ResolveCatalogIdentifier(plan, localStatement.ReferencesDefinition.TableVarName, true);
						if (!(schemaObject is Schema.TableVar))
							throw new CompilerException(CompilerException.Codes.InvalidReferenceObject, statement, localStatement.ReferenceName, localStatement.ReferencesDefinition.TableVarName);
						node.Reference.TargetTable = (Schema.TableVar)schemaObject;
						plan.AttachDependency(schemaObject);
						foreach (ReferenceColumnDefinition column in localStatement.ReferencesDefinition.Columns)
							node.Reference.TargetKey.Columns.Add(node.Reference.TargetTable.Columns[column.ColumnName]);
						foreach (Schema.Key key in node.Reference.TargetTable.Keys)
							if (node.Reference.TargetKey.Columns.IsSupersetOf(key.Columns))
							{
								node.Reference.TargetKey.IsUnique = true;
								break;
							}
							
						if (!node.Reference.TargetKey.IsUnique)
							throw new CompilerException(CompilerException.Codes.ReferenceMustTargetKey, statement, localStatement.ReferenceName, localStatement.ReferencesDefinition.TableVarName);
							
						if (node.Reference.SourceKey.Columns.Count != node.Reference.TargetKey.Columns.Count)
							throw new CompilerException(CompilerException.Codes.InvalidReferenceColumnCount, statement, localStatement.ReferenceName);
						
						#if REQUIRESAMEDATATYPESFORREFERENCECOLUMNS	
						for (int index = 0; index < node.Reference.SourceKey.Columns.Count; index++)
							if (!node.Reference.SourceKey.Columns[index].DataType.Is(node.Reference.TargetKey.Columns[index].DataType))
								throw new CompilerException(CompilerException.Codes.InvalidReferenceColumn, AStatement, localStatement.ReferenceName, node.Reference.SourceKey.Columns[index].Name, node.Reference.SourceKey.Columns[index].DataType.Name, node.Reference.TargetKey.Columns[index].Name, node.Reference.TargetKey.Columns[index].DataType.Name, node.Reference.TargetTable.DisplayName);
						#endif
								
						node.Reference.UpdateReferenceAction = localStatement.ReferencesDefinition.UpdateReferenceAction;
						node.Reference.DeleteReferenceAction = localStatement.ReferencesDefinition.DeleteReferenceAction;
						
						if (!plan.IsEngine && node.Reference.Enforced)
						{
							if ((node.Reference.SourceTable is Schema.BaseTableVar) && (node.Reference.TargetTable is Schema.BaseTableVar))
							{
								// Construct Insert/Update/Delete constraints to enforce the reference for the source and target tables
								// These nodes are attached to the tablevars involved during the runtime execution of the CreateReferenceNode
								node.Reference.SourceConstraint = CompileSourceReferenceConstraint(plan, node.Reference);
								node.Reference.TargetConstraint = CompileTargetReferenceConstraint(plan, node.Reference);
							}
							else
								// References are not enforced by default if they are sourced in or target derived table variables
								node.Reference.Enforced = GetEnforced(plan, node.Reference.MetaData, false);

							// This constraint will only need to be validated for inserts and updates and deletes when the action is require
							// A catalog level enforcement constraint is always compiled to allow for initial creation validation.
							// This node must evaluate to true before the constraint can be created.
							node.Reference.CatalogConstraint = 
								CompileCatalogConstraintForReference
								(
									plan, 
									node.Reference
								);

							plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
							// Construct UpdateReferenceAction nodes if necessary			
							PlanNode[] expressionNodes;
							PlanNode sourceNode;
							switch (node.Reference.UpdateReferenceAction)
							{
								case ReferenceAction.Cascade:
									node.Reference.UpdateHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", node.Reference.TargetTable.Name, "AfterUpdate", node.Reference.Name, "UpdateHandler"));
									node.Reference.UpdateHandler.Owner = node.Reference.Owner;
									node.Reference.UpdateHandler.Library = node.Reference.Library;
									node.Reference.UpdateHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", node.Reference.Name, "UpdateHandler"));
									node.Reference.UpdateHandler.EventType = EventType.AfterUpdate;
									node.Reference.UpdateHandler.IsGenerated = true;
									node.Reference.UpdateHandler.Operator.IsGenerated = true;
									node.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.UpdateHandler.Operator, "AOldRow", new Schema.RowType(true)));
									node.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.UpdateHandler.Operator, "ANewRow", new Schema.RowType(true)));
									node.Reference.UpdateHandler.Operator.Owner = node.Reference.Owner;
									node.Reference.UpdateHandler.Operator.Library = node.Reference.Library;
									node.Reference.UpdateHandler.Operator.Locator = new DebugLocator(DebugLocator.OperatorLocator(node.Reference.UpdateHandler.Operator.DisplayName), 0, 1);
									node.Reference.UpdateHandler.Operator.SessionObjectName = node.Reference.UpdateHandler.Operator.OperatorName;
									node.Reference.UpdateHandler.Operator.SessionID = plan.SessionID;
									plan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										sourceNode = EmitIdentifierNode(plan, node.Reference.SourceTable.Name);
									}
									finally
									{
										plan.PopStatementContext();
									}
									node.Reference.UpdateHandler.Operator.Block.BlockNode = CompileUpdateCascadeNodeForReference(plan, sourceNode, node.Reference);
									node.Reference.UpdateHandler.PlanNode = node.Reference.UpdateHandler.Operator.Block.BlockNode;
								break;
								
								case ReferenceAction.Clear:
									expressionNodes = new PlanNode[node.Reference.SourceKey.Columns.Count];
									for (int index = 0; index < node.Reference.SourceKey.Columns.Count; index++)
									{
										Schema.ScalarType scalarType = node.Reference.SourceKey.Columns[index].DataType as Schema.ScalarType;
										if (scalarType != null)
										{
											Schema.Special special = Compiler.FindDefaultSpecialForScalarType(plan, scalarType);
											if (special != null)
												expressionNodes[index] = EmitCallNode(plan, special.Selector.OperatorName, new PlanNode[]{});
										}
										
										if (expressionNodes[index] == null)
										{
											if (!node.Reference.SourceKey.Columns[index].IsNilable)
												throw new CompilerException(CompilerException.Codes.CannotClearSourceColumn, statement, node.Reference.SourceKey.Columns[index].Name);
											expressionNodes[index] = EmitNilNode(plan);
										}
									}

									node.Reference.UpdateHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", node.Reference.TargetTable.Name, "AfterUpdate", node.Reference.Name, "UpdateHandler"));
									node.Reference.UpdateHandler.Owner = node.Reference.Owner;
									node.Reference.UpdateHandler.Library = node.Reference.Library;
									node.Reference.UpdateHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", node.Reference.Name, "UpdateHandler"));
									node.Reference.UpdateHandler.EventType = EventType.AfterUpdate;
									node.Reference.UpdateHandler.IsGenerated = true;
									node.Reference.UpdateHandler.Operator.IsGenerated = true;
									node.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.UpdateHandler.Operator, "AOldRow", new Schema.RowType(true)));
									node.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.UpdateHandler.Operator, "ANewRow", new Schema.RowType(true)));
									node.Reference.UpdateHandler.Operator.Owner = node.Reference.Owner;
									node.Reference.UpdateHandler.Operator.Library = node.Reference.Library;
									node.Reference.UpdateHandler.Operator.Locator = new DebugLocator(DebugLocator.OperatorLocator(node.Reference.UpdateHandler.Operator.DisplayName), 0, 1);
									node.Reference.UpdateHandler.Operator.SessionObjectName = node.Reference.UpdateHandler.Operator.OperatorName;
									node.Reference.UpdateHandler.Operator.SessionID = plan.SessionID;
									plan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										sourceNode = EmitIdentifierNode(plan, node.Reference.SourceTable.Name);
									}
									finally
									{
										plan.PopStatementContext();
									}
									node.Reference.UpdateHandler.Operator.Block.BlockNode = CompileUpdateSetNodeForReference(plan, sourceNode, node.Reference, expressionNodes);
									node.Reference.UpdateHandler.PlanNode = node.Reference.UpdateHandler.Operator.Block.BlockNode;
								break;

								case ReferenceAction.Set:
									node.Reference.UpdateReferenceExpressions.AddRange(localStatement.ReferencesDefinition.UpdateReferenceExpressions);
									expressionNodes = new PlanNode[node.Reference.SourceKey.Columns.Count];
									for (int index = 0; index < localStatement.ReferencesDefinition.UpdateReferenceExpressions.Count; index++)
										expressionNodes[index] = CompileExpression(plan, localStatement.ReferencesDefinition.UpdateReferenceExpressions[index]);

									node.Reference.UpdateHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", node.Reference.TargetTable.Name, "AfterUpdate", node.Reference.Name, "UpdateHandler"));
									node.Reference.UpdateHandler.Owner = node.Reference.Owner;
									node.Reference.UpdateHandler.Library = node.Reference.Library;
									node.Reference.UpdateHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", node.Reference.Name, "UpdateHandler"));
									node.Reference.UpdateHandler.EventType = EventType.AfterUpdate;
									node.Reference.UpdateHandler.IsGenerated = true;
									node.Reference.UpdateHandler.Operator.IsGenerated = true;
									node.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.UpdateHandler.Operator, "AOldRow", new Schema.RowType(true)));
									node.Reference.UpdateHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.UpdateHandler.Operator, "ANewRow", new Schema.RowType(true)));
									node.Reference.UpdateHandler.Operator.Owner = node.Reference.Owner;
									node.Reference.UpdateHandler.Operator.Library = node.Reference.Library;
									node.Reference.UpdateHandler.Operator.Locator = new DebugLocator(DebugLocator.OperatorLocator(node.Reference.UpdateHandler.Operator.DisplayName), 0, 1);
									node.Reference.UpdateHandler.Operator.SessionObjectName = node.Reference.UpdateHandler.Operator.OperatorName;
									node.Reference.UpdateHandler.Operator.SessionID = plan.SessionID;
									plan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										sourceNode = EmitIdentifierNode(plan, node.Reference.SourceTable.Name);
									}
									finally
									{
										plan.PopStatementContext();
									}
									node.Reference.UpdateHandler.Operator.Block.BlockNode =
										CompileUpdateNodeForReference
										(
											plan, 
											sourceNode,
											node.Reference,
											expressionNodes,
											true // IsUpdate
										);
									node.Reference.UpdateHandler.PlanNode = node.Reference.UpdateHandler.Operator.Block.BlockNode;
								break;
							}

							// Construct DeleteReferenceAction nodes if necessary			
							switch (node.Reference.DeleteReferenceAction)
							{
								case ReferenceAction.Cascade:
									node.Reference.DeleteHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", node.Reference.TargetTable.Name, "AfterDelete", node.Reference.Name, "DeleteHandler"));
									node.Reference.DeleteHandler.Owner = node.Reference.Owner;
									node.Reference.DeleteHandler.Library = node.Reference.Library;
									node.Reference.DeleteHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", node.Reference.Name, "DeleteHandler"));
									node.Reference.DeleteHandler.EventType = EventType.AfterDelete;
									node.Reference.DeleteHandler.IsGenerated = true;
									node.Reference.DeleteHandler.Operator.IsGenerated = true;
									node.Reference.DeleteHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.DeleteHandler.Operator, "ARow", new Schema.RowType(true)));
									node.Reference.DeleteHandler.Operator.Owner = node.Reference.Owner;
									node.Reference.DeleteHandler.Operator.Library = node.Reference.Library;
									node.Reference.DeleteHandler.Operator.Locator = new DebugLocator(DebugLocator.OperatorLocator(node.Reference.DeleteHandler.Operator.DisplayName), 0, 1);
									node.Reference.DeleteHandler.Operator.SessionObjectName = node.Reference.DeleteHandler.Operator.OperatorName;
									node.Reference.DeleteHandler.Operator.SessionID = plan.SessionID;
									plan.PushStatementContext(new StatementContext(StatementType.Delete));
									try
									{
										sourceNode = EmitIdentifierNode(plan, node.Reference.SourceTable.Name);
									}
									finally
									{
										plan.PopStatementContext();
									}
									node.Reference.DeleteHandler.Operator.Block.BlockNode =
										CompileDeleteCascadeNodeForReference
										(
											plan, 
											sourceNode,
											node.Reference
										);
									node.Reference.DeleteHandler.PlanNode = node.Reference.DeleteHandler.Operator.Block.BlockNode;
								break;

								case ReferenceAction.Clear:
									expressionNodes = new PlanNode[node.Reference.SourceKey.Columns.Count];
									for (int index = 0; index < node.Reference.SourceKey.Columns.Count; index++)
									{
										Schema.ScalarType scalarType = node.Reference.SourceKey.Columns[index].DataType as Schema.ScalarType;
										if (scalarType != null)
										{
											Schema.Special special = Compiler.FindDefaultSpecialForScalarType(plan, scalarType);
											if (special != null)
												expressionNodes[index] = EmitCallNode(plan, special.Selector.OperatorName, new PlanNode[]{});
										}
										
										if (expressionNodes[index] == null)
										{
											if (!node.Reference.SourceKey.Columns[index].IsNilable)
												throw new CompilerException(CompilerException.Codes.CannotClearSourceColumn, statement, node.Reference.SourceKey.Columns[index].Name);
											expressionNodes[index] = EmitNilNode(plan);
										}
									}

									node.Reference.DeleteHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", node.Reference.TargetTable.Name, "AfterDelete", node.Reference.Name, "DeleteHandler"));
									node.Reference.DeleteHandler.Owner = node.Reference.Owner;
									node.Reference.DeleteHandler.Library = node.Reference.Library;
									node.Reference.DeleteHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", node.Reference.Name, "DeleteHandler"));
									node.Reference.DeleteHandler.EventType = EventType.AfterDelete;
									node.Reference.DeleteHandler.IsGenerated = true;
									node.Reference.DeleteHandler.Operator.IsGenerated = true;
									node.Reference.DeleteHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.DeleteHandler.Operator, "ARow", new Schema.RowType(true)));
									node.Reference.DeleteHandler.Operator.Owner = node.Reference.Owner;
									node.Reference.DeleteHandler.Operator.Library = node.Reference.Library;
									node.Reference.DeleteHandler.Operator.Locator = new DebugLocator(DebugLocator.OperatorLocator(node.Reference.DeleteHandler.Operator.DisplayName), 0, 1);
									node.Reference.DeleteHandler.Operator.SessionObjectName = node.Reference.DeleteHandler.Operator.OperatorName;
									node.Reference.DeleteHandler.Operator.SessionID = plan.SessionID;
									plan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										sourceNode = EmitIdentifierNode(plan, node.Reference.SourceTable.Name);
									}
									finally
									{
										plan.PopStatementContext();
									}
									node.Reference.DeleteHandler.Operator.Block.BlockNode =
										CompileDeleteSetNodeForReference
										(
											plan, 
											sourceNode,
											node.Reference,
											expressionNodes
										);
									node.Reference.DeleteHandler.PlanNode = node.Reference.DeleteHandler.Operator.Block.BlockNode;
								break;

								case ReferenceAction.Set:
									node.Reference.DeleteReferenceExpressions.AddRange(localStatement.ReferencesDefinition.DeleteReferenceExpressions);
									expressionNodes = new PlanNode[node.Reference.SourceKey.Columns.Count];
									for (int index = 0; index < localStatement.ReferencesDefinition.DeleteReferenceExpressions.Count; index++)
										expressionNodes[index] = CompileExpression(plan, localStatement.ReferencesDefinition.DeleteReferenceExpressions[index]);
									
									node.Reference.DeleteHandler = new Schema.TableVarEventHandler(String.Format("{0}_{1}_{2}_{3}", node.Reference.TargetTable.Name, "AfterDelete", node.Reference.Name, "DeleteHandler"));
									node.Reference.DeleteHandler.Owner = node.Reference.Owner;
									node.Reference.DeleteHandler.Library = node.Reference.Library;
									node.Reference.DeleteHandler.Operator = new Schema.Operator(String.Format("{0}_{1}", node.Reference.Name, "DeleteHandler"));
									node.Reference.DeleteHandler.EventType = EventType.AfterDelete;
									node.Reference.DeleteHandler.IsGenerated = true;
									node.Reference.DeleteHandler.Operator.IsGenerated = true;
									node.Reference.DeleteHandler.Operator.Operands.Add(new Schema.Operand(node.Reference.DeleteHandler.Operator, "ARow", new Schema.RowType(true)));
									node.Reference.DeleteHandler.Operator.Owner = node.Reference.Owner;
									node.Reference.DeleteHandler.Operator.Library = node.Reference.Library;
									node.Reference.DeleteHandler.Operator.Locator = new DebugLocator(DebugLocator.OperatorLocator(node.Reference.DeleteHandler.Operator.DisplayName), 0, 1);
									node.Reference.DeleteHandler.Operator.SessionObjectName = node.Reference.DeleteHandler.Operator.OperatorName;
									node.Reference.DeleteHandler.Operator.SessionID = plan.SessionID;
									plan.PushStatementContext(new StatementContext(StatementType.Update));
									try
									{
										sourceNode = EmitIdentifierNode(plan, node.Reference.SourceTable.Name);
									}
									finally
									{
										plan.PopStatementContext();
									}
									node.Reference.DeleteHandler.Operator.Block.BlockNode =
										CompileUpdateNodeForReference
										(
											plan,
											sourceNode,
											node.Reference,
											expressionNodes,
											false // IsUpdate
										);
									node.Reference.DeleteHandler.PlanNode = node.Reference.DeleteHandler.Operator.Block.BlockNode;
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
							for (int index = node.Reference.Dependencies.Count - 1; index >= 0; index--)
								if (node.Reference.Dependencies[index] is Schema.Reference)
									node.Reference.Dependencies.RemoveAt(index);
							#endif
						}
							finally
							{
								plan.PopCursorContext();
							}
						}
						
						return node;
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
				catch
				{
					plan.PlanCatalog.SafeRemove(node.Reference);
					throw;
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public static bool CouldGenerateDeviceScalarTypeMap(Plan plan, Schema.Device device, Schema.ScalarType scalarType)
		{
			plan.EnsureDeviceStarted(device);
			Schema.Representation representation = FindSystemRepresentation(scalarType);
			return (representation != null) && (representation.Properties.Count == 1) && (representation.Properties[0].DataType is Schema.ScalarType) && (device.ResolveDeviceScalarType(plan, (Schema.ScalarType)representation.Properties[0].DataType) != null);
		}
		
		public static Schema.DeviceScalarType CompileDeviceScalarTypeMap(Plan plan, Schema.Device device, DeviceScalarTypeMap deviceScalarTypeMap)
		{
			plan.EnsureDeviceStarted(device);
			Schema.ScalarType dataType = (Schema.ScalarType)CompileScalarTypeSpecifier(plan, new ScalarTypeSpecifier(deviceScalarTypeMap.ScalarTypeName));
			
			if (!plan.InLoadingContext())
			{
				Schema.DeviceScalarType existingMap = device.ResolveDeviceScalarType(plan, dataType);
				if ((existingMap != null) && !existingMap.IsGenerated)
					throw new CompilerException(CompilerException.Codes.DuplicateDeviceScalarType, deviceScalarTypeMap, device.Name, dataType.Name);
			}

			Schema.DeviceScalarType requiredScalarType = null;			
			ClassDefinition classDefinition = deviceScalarTypeMap.ClassDefinition;
			if (classDefinition == null)
			{
				Schema.Representation representation = FindSystemRepresentation(dataType);
				if ((representation != null) && (representation.Properties.Count == 1))
				{
					requiredScalarType = device.ResolveDeviceScalarType(plan, (Schema.ScalarType)representation.Properties[0].DataType);
					if (requiredScalarType != null)
						classDefinition = (ClassDefinition)requiredScalarType.ClassDefinition.Clone();
				}

				if (classDefinition == null)
					throw new CompilerException(CompilerException.Codes.ScalarTypeMapRequired, deviceScalarTypeMap, dataType.Name);
			}
			
			if (classDefinition == null)
				throw new CompilerException(CompilerException.Codes.DeviceScalarTypeClassExpected, deviceScalarTypeMap, "null");

			if (requiredScalarType == null)
				plan.CheckRight(Schema.RightNames.HostImplementation);

			int objectID = Schema.Object.GetObjectID(deviceScalarTypeMap.MetaData);
			string deviceScalarTypeName = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_Map", device.Name, dataType.Name), objectID);
			object deviceScalarTypeObject = plan.CreateObject(classDefinition, new object[]{objectID, deviceScalarTypeName});
			if (!(deviceScalarTypeObject is Schema.DeviceScalarType))
				throw new CompilerException(CompilerException.Codes.DeviceScalarTypeClassExpected, classDefinition, deviceScalarTypeObject == null ? "null" : deviceScalarTypeObject.GetType().AssemblyQualifiedName);
				
			Schema.DeviceScalarType deviceScalarType = (Schema.DeviceScalarType)deviceScalarTypeObject;
			deviceScalarType.Library = dataType.Library.IsRequiredLibrary(device.Library) ? dataType.Library : device.Library;;
			deviceScalarType.Owner = plan.User;
			deviceScalarType.IsGenerated = deviceScalarTypeMap.IsGenerated;
			plan.PushCreationObject(deviceScalarType);
			try
			{
				plan.CheckClassDependency(deviceScalarType.Library, classDefinition);
				deviceScalarType.Device = device;
				plan.AttachDependency(device);
				deviceScalarType.ScalarType = dataType;
				plan.AttachDependency(dataType);
				if (requiredScalarType != null)
				{
					plan.AttachDependency(requiredScalarType);
					deviceScalarType.IsDefaultClassDefinition = true;
				}
				deviceScalarType.ClassDefinition = classDefinition;
				deviceScalarType.MetaData = deviceScalarTypeMap.MetaData;
				return deviceScalarType;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static Schema.DeviceOperator CompileDeviceOperatorMap(Plan plan, Schema.Device device, DeviceOperatorMap deviceOperatorMap)
		{
			plan.EnsureDeviceStarted(device);
			bool isSystemClassDefinition = false;
			if (deviceOperatorMap.ClassDefinition == null)
			{
				deviceOperatorMap.ClassDefinition = device.GetDefaultOperatorClassDefinition(deviceOperatorMap.MetaData);
				isSystemClassDefinition = true;
			}
			
			if (deviceOperatorMap.ClassDefinition == null)
				throw new CompilerException(CompilerException.Codes.DeviceOperatorClassRequired, deviceOperatorMap, deviceOperatorMap.OperatorSpecifier.OperatorName, device.Name);
				
			if (!isSystemClassDefinition)
				plan.CheckRight(Schema.RightNames.HostImplementation);
				
			Schema.Operator operatorValue = ResolveOperatorSpecifier(plan, deviceOperatorMap.OperatorSpecifier);
			
			if (!plan.InLoadingContext())
			{
				Schema.DeviceOperator existingMap = device.ResolveDeviceOperator(plan, operatorValue);
				if ((existingMap != null) && !existingMap.IsGenerated)
					throw new CompilerException(CompilerException.Codes.DuplicateDeviceOperator, deviceOperatorMap, device.Name, operatorValue.OperatorName, operatorValue.Signature.ToString());
			}
				
			int objectID = Schema.Object.GetObjectID(deviceOperatorMap.MetaData);
			string deviceOperatorName = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_Map", device.Name, operatorValue.Name), objectID);
			object deviceOperatorObject = plan.CreateObject(deviceOperatorMap.ClassDefinition, new object[]{objectID, deviceOperatorName});
			if (!(deviceOperatorObject is Schema.DeviceOperator))
				throw new CompilerException(CompilerException.Codes.DeviceOperatorClassExpected, deviceOperatorMap.ClassDefinition, deviceOperatorObject == null ? "null" : deviceOperatorObject.GetType().AssemblyQualifiedName);
				
			Schema.DeviceOperator deviceOperator = (Schema.DeviceOperator)deviceOperatorObject;
			deviceOperator.Library = operatorValue.Library.IsRequiredLibrary(device.Library) ? operatorValue.Library : device.Library;
			deviceOperator.Owner = plan.User;
			plan.PushCreationObject(deviceOperator);
			try
			{
				deviceOperator.Device = device;
				plan.AttachDependency(device);
				deviceOperator.Operator = operatorValue;
				plan.AttachDependency(operatorValue);

				plan.CheckClassDependency(deviceOperator.Library, deviceOperatorMap.ClassDefinition);
				deviceOperator.ClassDefinition = deviceOperatorMap.ClassDefinition;
				deviceOperator.MetaData = deviceOperatorMap.MetaData;
				return deviceOperator;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		private static void CompileImplicitDeviceOperator(Plan plan, Schema.Device device, Schema.DeviceScalarType deviceScalarType, Schema.DeviceScalarType baseDeviceScalarType, Schema.Operator operatorValue, ClassDefinition classDefinition)
		{
			Schema.DeviceOperator deviceOperator = device.ResolveDeviceOperator(plan, operatorValue);
			if (deviceOperator == null)
			{
				deviceScalarType.AddDependency(baseDeviceScalarType);
				int objectID = Schema.Object.GetNextObjectID();
				string deviceOperatorName = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_Map", device.Name, operatorValue.Name), objectID);
				deviceOperator = (Schema.DeviceOperator)plan.CreateObject(classDefinition, new object[] { objectID, deviceOperatorName });
				deviceOperator.Library = deviceScalarType.Library;
				deviceOperator.Owner = deviceScalarType.Owner;
				deviceOperator.IsGenerated = true;
				deviceOperator.Generator = deviceScalarType;
				plan.PushCreationObject(deviceOperator);
				try
				{
					deviceOperator.Operator = operatorValue;
					plan.AttachDependency(operatorValue);
					deviceOperator.Device = device;
					plan.AttachDependency(device);
					deviceOperator.ClassDefinition = classDefinition;
					plan.CheckClassDependency(deviceOperator.ClassDefinition);
					plan.CatalogDeviceSession.CreateDeviceOperator(deviceOperator);
				}
				finally
				{
					plan.PopCreationObject();
				}
			}
			// BTR 8/16/2006 -> I do not understand when this would ever be the case. It may be
			// just a holdover. In any case, since we are using the Generator_ID in the catalog
			// to track this set now, I feel safe just ignoring this condition.
			else
			{
				if (deviceOperator.IsGenerated)
					Error.Fail("Encountered an existing device operator map for device '{0}' and operator '{1}'", device.DisplayName, operatorValue.DisplayName);
				
/*
				if (LDeviceOperator.IsGenerated)
					APlan.CatalogDeviceSession.AddDeviceScalarTypeDeviceOperator(ADeviceScalarType, LDeviceOperator);
					//ADeviceScalarType.DeviceOperators.Add(LDeviceOperator);
*/
			}
		}
		
		private static void CompileImplicitDeviceOperator(Plan plan, Schema.Device device, Schema.DeviceScalarType deviceScalarType, Schema.DeviceScalarType baseDeviceScalarType, Schema.Operator operatorValue, Schema.Operator baseOperator)
		{
			Schema.DeviceOperator baseDeviceOperator = device.ResolveDeviceOperator(plan, baseOperator);
			if (baseDeviceOperator != null)
				CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, operatorValue, (ClassDefinition)baseDeviceOperator.ClassDefinition.Clone());
		}
		
		public static void CompileDeviceScalarTypeMapOperatorMaps(Plan plan, Schema.Device device, Schema.DeviceScalarType deviceScalarType)
		{
			// if the equality or comparison operators are set, map them based on the property type of the default representation
			plan.EnsureDeviceStarted(device);
			Schema.Representation systemRepresentation = FindSystemRepresentation(deviceScalarType.ScalarType);
			if (systemRepresentation != null)
			{
				Schema.DeviceScalarType baseDeviceScalarType = device.ResolveDeviceScalarType(plan, (Schema.ScalarType)systemRepresentation.Properties[0].DataType);
				if (baseDeviceScalarType != null)
				{
					if (deviceScalarType.ScalarType.EqualityOperator != null)
					{
						Schema.Operator baseOperator = ResolveOperator(plan, Instructions.Equal, new Schema.Signature(new Schema.SignatureElement[] { new Schema.SignatureElement(baseDeviceScalarType.ScalarType), new Schema.SignatureElement(baseDeviceScalarType.ScalarType) }), false, false);
						if (baseOperator != null)
							CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, deviceScalarType.ScalarType.EqualityOperator, baseOperator);
					}
					
					if (deviceScalarType.ScalarType.ComparisonOperator != null)
					{
						Schema.Operator baseOperator = ResolveOperator(plan, Instructions.Compare, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(baseDeviceScalarType.ScalarType), new Schema.SignatureElement(baseDeviceScalarType.ScalarType)}), false, false);
						if (baseOperator != null)
							CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, deviceScalarType.ScalarType.ComparisonOperator, baseOperator);
					}
						
					if ((deviceScalarType.ScalarType.IsSpecialOperator != null) && (deviceScalarType.ScalarType.Specials.Count == 0))
					{
						Schema.Operator baseOperator = ResolveOperator(plan, "IsSpecial", new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(baseDeviceScalarType.ScalarType)}), false, false);
						if (baseOperator != null)
							CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, deviceScalarType.ScalarType.IsSpecialOperator, baseOperator);
					}
					
					// If this is a like type, map a default selector and accessors for the like representation, as long as that representation is system-provided (generated)
					Schema.Representation likeRepresentation = FindLikeRepresentation(deviceScalarType.ScalarType);
					if ((likeRepresentation != null) && (likeRepresentation.ID == systemRepresentation.ID))
					{
						CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, likeRepresentation.Selector, device.GetDefaultSelectorClassDefinition());
						CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, likeRepresentation.Properties[0].ReadAccessor, device.GetDefaultReadAccessorClassDefinition());
						CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, likeRepresentation.Properties[0].WriteAccessor, device.GetDefaultWriteAccessorClassDefinition());
					}
					else
					{
						// Map the selector and accessors based on the selector of the property type
						Schema.Representation baseSystemRepresentation = FindSystemRepresentation(baseDeviceScalarType.ScalarType);
						if (baseSystemRepresentation == null)
						{
							Schema.Representation defaultRepresentation = FindDefaultRepresentation(baseDeviceScalarType.ScalarType);
							if ((defaultRepresentation != null) && (defaultRepresentation.Properties.Count == 1))
								baseSystemRepresentation = defaultRepresentation;
						}
						
						if (baseSystemRepresentation != null)
						{
							CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, systemRepresentation.Selector, baseSystemRepresentation.Selector); 
							CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, systemRepresentation.Properties[0].ReadAccessor, baseSystemRepresentation.Properties[0].ReadAccessor);
							CompileImplicitDeviceOperator(plan, device, deviceScalarType, baseDeviceScalarType, systemRepresentation.Properties[0].WriteAccessor, baseSystemRepresentation.Properties[0].WriteAccessor);
						}
					}
				}
			}
		}
		
		public static PlanNode CompileCreateServerStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);
				
			CreateServerStatement localStatement = (CreateServerStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateServer);

			string serverName = Schema.Object.Qualify(localStatement.ServerName, plan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(plan, statement, serverName);
			CreateServerNode node = new CreateServerNode();
			node.ServerLink = new Schema.ServerLink(Schema.Object.GetObjectID(localStatement.MetaData), serverName);
			node.ServerLink.Owner = plan.User;
			node.ServerLink.Library = plan.CurrentLibrary;
			// TODO: Set ServerLink attributes from MetaData
			node.ServerLink.MetaData = localStatement.MetaData;
			node.ServerLink.IsRemotable = false;
			plan.PushCreationObject(node.ServerLink);
			try
			{
				// Attach a dependency on the CreateServerLinkUserWithEncryptedPassword because it is used in the create script for the object
				Compiler.ResolveOperatorSpecifier
				(
					plan, 
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
				return node;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}

		public static PlanNode CompileCreateDeviceStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateDeviceStatement localStatement = (CreateDeviceStatement)statement;
			plan.CheckRight(Schema.RightNames.CreateDevice);
			string deviceName = Schema.Object.Qualify(localStatement.DeviceName, plan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(plan, statement, deviceName);
			CreateDeviceNode node = new CreateDeviceNode();
			plan.CheckRight(Schema.RightNames.HostImplementation);
			plan.CheckClassDependency(plan.CurrentLibrary, localStatement.ClassDefinition);
			object objectValue = plan.CreateObject(localStatement.ClassDefinition, new object[] { Schema.Object.GetObjectID(localStatement.MetaData), deviceName });
			if (!(objectValue is Schema.Device))
				throw new CompilerException(CompilerException.Codes.DeviceClassExpected, localStatement.ClassDefinition, objectValue == null ? "null" : objectValue.GetType().AssemblyQualifiedName);
			node.NewDevice = (Schema.Device)objectValue;
			node.NewDevice.Owner = plan.User;
			node.NewDevice.Library = plan.CurrentLibrary;
			plan.PushCreationObject(node.NewDevice);
			try
			{
				node.NewDevice.MetaData = localStatement.MetaData;
				node.NewDevice.ClassDefinition = localStatement.ClassDefinition;
				node.NewDevice.IsRemotable = false;
				node.NewDevice.LoadRegistered();
				
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
					
				if (localStatement.ReconciliationSettings.ReconcileModeSet)
					node.NewDevice.ReconcileMode = localStatement.ReconciliationSettings.ReconcileMode;
					
				if (localStatement.ReconciliationSettings.ReconcileMasterSet)
					node.NewDevice.ReconcileMaster = localStatement.ReconciliationSettings.ReconcileMaster;
				
				return node;
			}
			finally
			{
				plan.PopCreationObject();
			}
		}
		
		public static void CheckValidObjectName(Plan plan, Schema.Objects objects, Statement statement, string objectName)
		{
			List<string> names = new List<string>();
			if (!objects.IsValidObjectName(objectName, names))
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(names[0], AObjectName))
					if (String.Compare(names[0], AObjectName) == 0)
						throw new CompilerException(CompilerException.Codes.CreatingDuplicateObjectName, AStatement, AObjectName);
					else
						throw new CompilerException(CompilerException.Codes.CreatingHiddenObjectName, AStatement, AObjectName, names[0]);
				else
					throw new CompilerException(CompilerException.Codes.CreatingHidingObjectName, AStatement, AObjectName, names[0]);
				#else
				throw new CompilerException(CompilerException.Codes.CreatingDuplicateObjectName, statement, objectName);
				#endif
			}
		}
		
		public static void CheckValidSessionObjectName(Plan plan, Statement statement, string objectName)
		{
			CheckValidObjectName(plan, plan.SessionObjects, statement, objectName);
			CheckValidObjectName(plan, plan.PlanSessionObjects, statement, objectName);
		}
		
		public static void CheckValidCatalogObjectName(Plan plan, Statement statement, string objectName)
		{
			if (!plan.IsEngine && (!plan.InLoadingContext()))
			{
				if (plan.CatalogDeviceSession.CatalogObjectExists(objectName))
					throw new CompilerException(CompilerException.Codes.CreatingDuplicateObjectName, statement, objectName);
			}
			else
				CheckValidObjectName(plan, plan.Catalog, statement, objectName);
			
			CheckValidObjectName(plan, plan.Catalog.Libraries, statement, objectName);
			CheckValidObjectName(plan, plan.PlanCatalog, statement, objectName);
		}
		
		public static void CheckValidOperatorName(Plan plan, Schema.Catalog catalog, Statement statement, string operatorName, Schema.Signature signature)
		{
			List<string> names = new List<string>();
			if (!catalog.OperatorMaps.IsValidObjectName(operatorName, names))
			{
				#if DISALLOWAMBIGUOUSNAMES
				if (Schema.Object.NamesEqual(names[0], AOperatorName))
					if (String.Compare(names[0], AOperatorName) == 0)
					{
						if (ACatalog.OperatorMaps[AOperatorName].ContainsSignature(ASignature))
							throw new CompilerException(CompilerException.Codes.CreatingDuplicateSignature, AStatement, AOperatorName, ASignature.ToString());
					}
					else
						throw new CompilerException(CompilerException.Codes.CreatingHiddenOperatorName, AStatement, AOperatorName, names[0]);
				else
					throw new CompilerException(CompilerException.Codes.CreatingHidingOperatorName, AStatement, AOperatorName, names[0]);
				#else
				if (catalog.OperatorMaps[operatorName].ContainsSignature(signature))
					throw new CompilerException(CompilerException.Codes.CreatingDuplicateSignature, statement, operatorName, signature.ToString());
				#endif
			}
		}
		
		public static void CheckValidCatalogOperatorName(Plan plan, Statement statement, string operatorName, Schema.Signature signature)
		{
			if (!plan.IsEngine && (!plan.InLoadingContext()))
			{
				plan.CatalogDeviceSession.ResolveOperatorName(operatorName);
				CheckValidOperatorName(plan, plan.Catalog, statement, operatorName, signature);
			}
			else
				CheckValidOperatorName(plan, plan.Catalog, statement, operatorName, signature);

			CheckValidOperatorName(plan, plan.PlanCatalog, statement, operatorName, signature);
		}
		
		public static void ResolveCall(Plan plan, Schema.Catalog catalog, OperatorBindingContext context)
		{
			lock (catalog)
			{			
				Schema.Operator operatorValue = context.Operator;
				catalog.OperatorMaps.ResolveCall(plan, context);
				if ((operatorValue == null) && (context.Operator != null))
				{
					//APlan.AcquireCatalogLock(AContext.Operator, LockMode.Shared);
					plan.AttachDependency(context.Operator);
				}
			}
		}
		
		// All operator resolution comes through this point, so this is where the operator resolution cache is checked
		public static void ResolveOperator(Plan plan, OperatorBindingContext context)
		{
			// Operator resolutions for application-transaction or session-specific operators are never cached
			// search for an application transaction-specific operator
			if ((plan.ApplicationTransactionID != Guid.Empty) && (!plan.InLoadingContext()))
			{
				ApplicationTransaction transaction = plan.GetApplicationTransaction();
				try
				{
					if (!transaction.IsGlobalContext && !transaction.IsLookup)
					{
						NameBindingContext nameContext = new NameBindingContext(context.OperatorName, plan.NameResolutionPath);
						int index = transaction.OperatorMaps.ResolveName(nameContext.Identifier, nameContext.ResolutionPath, nameContext.Names);
						if (nameContext.IsAmbiguous)
						{
							context.OperatorNameContext.SetBindingDataFromContext(nameContext);
							return;
						}
						
						if (index >= 0)
						{
							OperatorBindingContext localContext = new OperatorBindingContext(context.Statement, transaction.OperatorMaps[index].TranslatedOperatorName, context.ResolutionPath, context.CallSignature, context.IsExact);
							ResolveOperator(plan, localContext);
							
							if (localContext.Operator != null)
								context.SetBindingDataFromContext(localContext);
						}
					}
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
			
			// if no resolution occurred, or a partial-match was found, search for a session-specific operator
			if ((context.Operator == null) || context.Matches.IsPartial)
			{
				NameBindingContext nameContext = new NameBindingContext(context.OperatorName, plan.NameResolutionPath);
				int index = plan.PlanSessionOperators.ResolveName(nameContext.Identifier, nameContext.ResolutionPath, nameContext.Names);
				if (nameContext.IsAmbiguous)
				{
					context.OperatorNameContext.SetBindingDataFromContext(nameContext);
					return;
				}
				
				if (index >= 0)
				{
					OperatorBindingContext localContext = new OperatorBindingContext(context.Statement, ((Schema.SessionObject)plan.PlanSessionOperators[index]).GlobalName, context.ResolutionPath, context.CallSignature, context.IsExact);
					ResolveOperator(plan, localContext);
					
					if (localContext.Operator != null)
						context.SetBindingDataFromContext(localContext);
				}
			}
			
			if ((context.Operator == null) || context.Matches.IsPartial)
			{
				NameBindingContext nameContext = new NameBindingContext(context.OperatorName, plan.NameResolutionPath);
				int index = plan.SessionOperators.ResolveName(nameContext.Identifier, nameContext.ResolutionPath, nameContext.Names);
				if (nameContext.IsAmbiguous)
				{
					context.OperatorNameContext.SetBindingDataFromContext(nameContext);
					return;
				}
				
				if (index >= 0)
				{
					OperatorBindingContext localContext = new OperatorBindingContext(context.Statement, ((Schema.SessionObject)plan.SessionOperators[index]).GlobalName, context.ResolutionPath, context.CallSignature, context.IsExact);
					ResolveOperator(plan, localContext);
					
					if (localContext.Operator != null)
						context.SetBindingDataFromContext(localContext);
				}
			}
			
			// if no resolution occurred, or a partial-match was found, resolve normally
			if ((context.Operator == null) || context.Matches.IsPartial)
			{
				#if USEOPERATORRESOLUTIONCACHE
				lock (plan.Catalog)
				{
					lock (plan.Catalog.OperatorResolutionCache)
					{
						OperatorBindingContext localContext = plan.Catalog.OperatorResolutionCache[context];
						if (localContext != null)
						{
							context.MergeBindingDataFromContext(localContext);
							if (context.Operator != null)
							{
								//APlan.AcquireCatalogLock(AContext.Operator, LockMode.Shared);
								plan.AttachDependency(context.Operator);
							}
						}
						else
						{
				#endif
							ResolveCall(plan, plan.PlanCatalog, context);
							if ((context.Operator == null) || context.Matches.IsPartial)
							{
								// NOTE: If this is a repository, or we are currently loading, there is no need to force the resolve of arbitrary matches, the required operator will be in the cache because of the dependency loading mechanism in the catalog device
								if (!plan.IsEngine && (!plan.InLoadingContext()))
									plan.CatalogDeviceSession.ResolveOperatorName(context.OperatorName);
								ResolveCall(plan, plan.Catalog, context);
							}
				#if USEOPERATORRESOLUTIONCACHE
							if ((context.Operator == null) || !(context.Operator.IsSessionObject || context.Operator.IsATObject || context.Operator.Signature.HasNonScalarElements()))
							{
								// Only cache the resolution if this is not a session or A/T operator, and it is a pure-scalar operator
								localContext = new OperatorBindingContext(null, context.OperatorName, context.ResolutionPath, context.CallSignature, context.IsExact);
								localContext.SetBindingDataFromContext(context);
								plan.Catalog.OperatorResolutionCache.Add(localContext);
							}
						}
					}
				}
				#endif
			}
			
			// If a resolution occurred, and we are in an application transaction, 
			// and the operator should be translated, and it is not an application transaction specific operator
			//   Translate the operator into the application transaction space
			if ((context.Operator != null) && (plan.ApplicationTransactionID != Guid.Empty) && (!plan.InLoadingContext()))
			{
				ApplicationTransaction transaction = plan.GetApplicationTransaction();
				try
				{
					if (!context.Operator.IsATObject)
					{
						if (context.Operator.ShouldTranslate && !transaction.IsGlobalContext && !transaction.IsLookup)
							context.Operator = transaction.AddOperator(plan.ServerProcess, context.Operator);
					}
					else
						transaction.EnsureATOperatorMapped(plan.ServerProcess, context.Operator);
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
		}
		
		public static void CheckOperatorResolution(Plan plan, OperatorBindingContext context)
		{
			// Throw exceptions for the operator resolution.
			if (context.IsOperatorNameResolved)
			{
				if (context.Matches.Count == 0)
					throw new CompilerException(CompilerException.Codes.NoSignatureForParameterCardinality, context.Statement, context.OperatorNameContext.Object == null ? context.OperatorNameContext.Identifier : context.OperatorNameContext.Object.Name, context.CallSignature.Count.ToString());
					
				if (context.Matches.IsAmbiguous)
				{
					StringBuilder builder = new StringBuilder();
					bool first = true;
					for (int index = 0; index < context.Matches.BestMatches.Count; index++)
					{
						if ((context.Matches.BestMatches[index].IsMatch) && (context.Matches.BestMatches[index].PathLength == context.Matches.ShortestPathLength))
						{
							if (!first)
								builder.Append(", ");
							else
								first = false;
								
							builder.AppendFormat("{0}{1}", context.Matches.BestMatches[index].Signature.Operator.OperatorName, context.Matches.BestMatches[index].Signature.Signature.ToString());
						}
					}
					throw new CompilerException(CompilerException.Codes.AmbiguousOperatorCall, context.Statement, context.OperatorName, context.CallSignature.ToString(), builder.ToString());
				}
				
				if (context.IsExact && !context.Matches.IsExact)
					throw new CompilerException(CompilerException.Codes.NoExactMatch, context.Statement, context.OperatorName, context.CallSignature.ToString());
					
				if ((context.Matches.Count > 0) && !context.Matches.IsMatch)
				{
					OperatorMatch match = context.Matches.ClosestMatch;
					if (match != null)
					{
						plan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidOperatorCall, context.Statement, context.OperatorName, context.CallSignature.ToString(), match.Signature.Signature.ToString()));
						for (int index = 0; index < match.Signature.Signature.Count; index++)
						{
							if (!match.CanConvert[index])
								plan.Messages.Add
								(
									new CompilerException
									(
										CompilerException.Codes.NoConversionForParameter, 
										(context.Statement is CallExpression) ? 
											((CallExpression)context.Statement).Expressions[index] : 
											(
												(context.Statement is OperatorSpecifier) ? 
													((OperatorSpecifier)context.Statement).FormalParameterSpecifiers[index] : 
													context.Statement
											), 
										index.ToString(), 
										context.CallSignature[index].ToString(), 
										match.Signature.Signature[index].ToString()
									)
								);
								
							if (match.ConversionContexts[index] != null)
								CheckConversionContext(plan, match.ConversionContexts[index], false);
						}
					}
					else
						plan.Messages.Add(new CompilerException(CompilerException.Codes.NoMatch, context.Statement, context.OperatorName, context.CallSignature.ToString()));
					throw new CompilerException(CompilerException.Codes.NonFatalErrors); // This exception will be caught and ignored in CompileStatement, allowing compilation to continue on the next statement.
				}
				
				if (context.Matches.IsPartial)
				{
					OperatorMatch operatorMatch = context.Matches.Match;
					for (int index = 0; index < operatorMatch.Signature.Signature.Count; index++)
						if (operatorMatch.ConversionContexts[index] != null)
							CheckConversionContext(plan, operatorMatch.ConversionContexts[index], false);
				}
			}
			else
				throw new CompilerException(CompilerException.Codes.OperatorNotFound, context.Statement, context.OperatorName, context.CallSignature.ToString());
		}
		
		public static Schema.Operator ResolveOperator(Plan plan, string operatorName, Schema.Signature signature, bool isExact, bool mustResolve)
		{
			//long LStartTicks = TimingUtility.CurrentTicks;
			OperatorBindingContext context = new OperatorBindingContext(new EmptyStatement(), operatorName, plan.NameResolutionPath, signature, isExact);
			ResolveOperator(plan, context);
			//APlan.Accumulator += TimingUtility.CurrentTicks - LStartTicks;
			if (mustResolve)
				CheckOperatorResolution(plan, context);
			return context.Operator;
		}
		
		public static Schema.Operator ResolveOperator(Plan plan, string operatorName, Schema.Signature signature, bool isExact)
		{
			return ResolveOperator(plan, operatorName, signature, isExact, true);
		}
		
		public static Schema.Operator ResolveOperatorSpecifier(Plan plan, OperatorSpecifier operatorSpecifier, bool mustResolve)
		{
			Schema.SignatureElement[] elements = new Schema.SignatureElement[operatorSpecifier.FormalParameterSpecifiers.Count];
			for (int index = 0; index < operatorSpecifier.FormalParameterSpecifiers.Count; index++)
				elements[index] = new Schema.SignatureElement(Compiler.CompileTypeSpecifier(plan, operatorSpecifier.FormalParameterSpecifiers[index].TypeSpecifier), operatorSpecifier.FormalParameterSpecifiers[index].Modifier);
			Schema.Signature signature = new Schema.Signature(elements);
			
			OperatorBindingContext context = new OperatorBindingContext(operatorSpecifier, operatorSpecifier.OperatorName, plan.NameResolutionPath, signature, true);
			ResolveOperator(plan, context);
			if (mustResolve)
				CheckOperatorResolution(plan, context);
			return context.Operator;
		}
		
		public static Schema.Operator ResolveOperatorSpecifier(Plan plan, OperatorSpecifier operatorSpecifier)
		{
			return ResolveOperatorSpecifier(plan, operatorSpecifier, true);
		}
		
		//public static void AcquireDependentLocks(Plan APlan, Schema.Object AObject, LockMode AMode)
		//{
		//	#if USECATALOGLOCKS
		//	APlan.AcquireCatalogLock(AObject, AMode);
		//	for (int LIndex = 0; LIndex < AObject.Dependents.Count; LIndex++)
		//		AcquireDependentLocks(APlan, AObject.Dependents.GetObjectForIndex(APlan.Catalog, LIndex), AMode);
		//	#endif
		//}
		
		public static PlanNode CompileCreateConversionStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateConversionStatement localStatement = (CreateConversionStatement)statement;
			Schema.ScalarType sourceScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(plan, localStatement.SourceScalarTypeName);
			//AcquireDependentLocks(APlan, LSourceScalarType, LockMode.Exclusive);
			Schema.ScalarType targetScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(plan, localStatement.TargetScalarTypeName);
			//AcquireDependentLocks(APlan, LTargetScalarType, LockMode.Shared);

			OperatorBindingContext context = new OperatorBindingContext(localStatement.OperatorName, localStatement.OperatorName.Identifier, plan.NameResolutionPath, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(sourceScalarType)}), false);
			ResolveOperator(plan, context);
			CheckOperatorResolution(plan, context);
			if ((context.Operator.ReturnDataType == null) || !context.Operator.ReturnDataType.Is(targetScalarType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, localStatement.OperatorName, context.Operator.ReturnDataType.Name, targetScalarType.Name);
			string conversionName;
			if ((localStatement.MetaData != null) && (localStatement.MetaData.Tags.Contains("DAE.RootedIdentifier")))
				conversionName = localStatement.MetaData.Tags["DAE.RootedIdentifier"].Value;
			else
				conversionName = Schema.Object.Qualify(Schema.Object.MangleQualifiers(String.Format("Conversion_{0}_{1}", sourceScalarType.Name, targetScalarType.Name)), plan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(plan, localStatement, conversionName);
			Schema.Conversion conversion = new Schema.Conversion(Schema.Object.GetObjectID(localStatement.MetaData), conversionName, sourceScalarType, targetScalarType, context.Operator, localStatement.IsNarrowing);
			conversion.Owner = plan.User;
			conversion.Library = plan.CurrentLibrary;
			conversion.MergeMetaData(localStatement.MetaData);
			plan.PushCreationObject(conversion);
			try
			{
				plan.AttachDependency(conversion.SourceScalarType);
				plan.AttachDependency(conversion.TargetScalarType);
				plan.AttachDependency(conversion.Operator);
			}
			finally
			{
				plan.PopCreationObject();
			}

			// Clear conversion path and operator resolution caches
			plan.Catalog.ConversionPathCache.Clear(conversion.SourceScalarType);
			plan.Catalog.ConversionPathCache.Clear(conversion.TargetScalarType);
			plan.Catalog.OperatorResolutionCache.Clear(conversion.SourceScalarType, conversion.TargetScalarType);
			
			plan.PlanCatalog.Add(conversion);

			return new CreateConversionNode(conversion);
		}
		
		public static PlanNode CompileDropConversionStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropConversionStatement localStatement = (DropConversionStatement)statement;
			DropConversionNode node = new DropConversionNode();
			node.SourceScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(plan, localStatement.SourceScalarTypeName);
			//AcquireDependentLocks(APlan, LNode.SourceScalarType, LockMode.Exclusive);
			node.TargetScalarType = (Schema.ScalarType)CompileScalarTypeSpecifier(plan, localStatement.TargetScalarTypeName);
			//AcquireDependentLocks(APlan, LNode.TargetScalarType, LockMode.Shared);

			// Clear the conversion path and operator resolution caches
			foreach (Schema.Conversion conversion in node.SourceScalarType.ImplicitConversions)
				if (conversion.TargetScalarType.Equals(node.TargetScalarType))
				{
					plan.Catalog.ConversionPathCache.Clear(conversion);
					plan.Catalog.OperatorResolutionCache.Clear(conversion);
					break;
				}
			return node;
		}
		
		public static PlanNode CompileCreateSortStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			CreateSortStatement localStatement = (CreateSortStatement)statement;
			CreateSortNode node = new CreateSortNode();
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ScalarTypeName, true);
			if (!(objectValue is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, statement);
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			node.ScalarType = (Schema.ScalarType)objectValue;
			node.Sort = CompileSortDefinition(plan, node.ScalarType, localStatement, true);
			if (node.ScalarType.UniqueSortID == node.Sort.ID)
			{
				node.IsUnique = true;
				node.Sort.IsUnique = true;
			}
			else
				node.Sort.IsGenerated = false;
			return node;
		}
		
		public static PlanNode CompileAlterSortStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterSortStatement localStatement = (AlterSortStatement)statement;
			AlterSortNode node = new AlterSortNode();
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ScalarTypeName, true);
			if (!(objectValue is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, statement);
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			node.ScalarType = (Schema.ScalarType)objectValue;
			node.Sort = CompileSortDefinition(plan, node.ScalarType, new SortDefinition(localStatement.Expression), true);
			return node;
		}
		
		public static PlanNode CompileDropSortStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropSortStatement localStatement = (DropSortStatement)statement;
			DropSortNode node = new DropSortNode();
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ScalarTypeName, true);
			if (!(objectValue is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, statement);
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			node.ScalarType = (Schema.ScalarType)objectValue;
			node.IsUnique = localStatement.IsUnique;
			return node;
		}

		public static PlanNode CompileCreateRoleStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);
				
			plan.CheckRight(Schema.RightNames.CreateRole);

			CreateRoleStatement localStatement = (CreateRoleStatement)statement;
			CreateRoleNode node = new CreateRoleNode();
			
			string roleName = Schema.Object.Qualify(localStatement.RoleName, plan.CurrentLibrary.Name);
			CheckValidCatalogObjectName(plan, statement, roleName);

			node.Role = new Schema.Role(Schema.Object.GetObjectID(localStatement.MetaData), roleName);
			node.Role.MetaData = localStatement.MetaData;
			node.Role.Owner = plan.User;
			node.Role.Library = plan.CurrentLibrary;
			plan.PlanCatalog.Add(node.Role);
			return node;
		}
		
		public static PlanNode CompileAlterRoleStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);
				
			AlterRoleStatement localStatement = (AlterRoleStatement)statement;
			Schema.Role role = ResolveCatalogIdentifier(plan, localStatement.RoleName, true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			plan.CheckRight(role.GetRight(Schema.RightNames.Alter));
			
			AlterRoleNode node = new AlterRoleNode();
			node.Role = role;
			node.Statement = localStatement;
			return node;
		}
		
		public static PlanNode CompileDropRoleStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);
				
			DropRoleStatement localStatement = (DropRoleStatement)statement;
			Schema.Role role = ResolveCatalogIdentifier(plan, localStatement.RoleName, true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			plan.CheckRight(role.GetRight(Schema.RightNames.Drop));
			
			DropRoleNode node = new DropRoleNode();
			node.Role = role;
			return node;
		}
		
		public static PlanNode CompileCreateRightStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);
				
			plan.CheckRight(Schema.RightNames.CreateRight);
				
			CreateRightStatement localStatement = (CreateRightStatement)statement;
			
			CreateRightNode node = new CreateRightNode();
			node.RightName = localStatement.RightName;
			return node;
		}
		
		public static PlanNode CompileDropRightStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);
				
			DropRightStatement localStatement = (DropRightStatement)statement;
			
			DropRightNode node = new DropRightNode();
			node.RightName = localStatement.RightName;
			return node;
		}
		
		public static PlanNode CompileAlterTableStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterTableStatement localStatement = (AlterTableStatement)statement;
			AlterTableNode node = new AlterTableNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			node.AlterTableStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.TableVarName, false);
			if (objectValue == null)
			{
				Schema.Device device = GetDefaultDevice(plan, false);
				if (device != null)
				{
					plan.CheckDeviceReconcile(new Schema.BaseTableVar(localStatement.TableVarName, new Schema.TableType(), device));
					objectValue = ResolveCatalogIdentifier(plan, localStatement.TableVarName, true);
				}
			}
			if (!(objectValue is Schema.BaseTableVar))
				throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, statement);
			plan.CheckRight(((Schema.BaseTableVar)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterViewStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterViewStatement localStatement = (AlterViewStatement)statement;
			AlterViewNode node = new AlterViewNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			node.AlterViewStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.TableVarName, true);
			if (!(objectValue is Schema.DerivedTableVar))
				throw new CompilerException(CompilerException.Codes.ViewIdentifierExpected, statement);
			plan.CheckRight(((Schema.DerivedTableVar)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
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
		public static PlanNode CompileAlterScalarTypeStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterScalarTypeStatement localStatement = (AlterScalarTypeStatement)statement;
			AlterScalarTypeNode node = new AlterScalarTypeNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			node.AlterScalarTypeStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ScalarTypeName, true);
			if (!(objectValue is Schema.IScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, statement);
			plan.CheckRight(((Schema.ScalarType)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterOperatorStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterOperatorStatement localStatement = (AlterOperatorStatement)statement;
			AlterOperatorNode node = new AlterOperatorNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			node.AlterOperatorStatement = localStatement;
			Schema.Operator operatorValue = ResolveOperatorSpecifier(plan, localStatement.OperatorSpecifier);
			plan.CheckRight(operatorValue.GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LOperator, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterAggregateOperatorStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterAggregateOperatorStatement localStatement = (AlterAggregateOperatorStatement)statement;
			AlterAggregateOperatorNode node = new AlterAggregateOperatorNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			node.AlterAggregateOperatorStatement = localStatement;
			Schema.Operator operatorValue = ResolveOperatorSpecifier(plan, localStatement.OperatorSpecifier);
			plan.CheckRight(operatorValue.GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LOperator, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterConstraintStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterConstraintStatement localStatement = (AlterConstraintStatement)statement;
			AlterConstraintNode node = new AlterConstraintNode();
			node.AlterConstraintStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ConstraintName, true);
			if (!(objectValue is Schema.CatalogConstraint))
				throw new CompilerException(CompilerException.Codes.ConstraintIdentifierExpected, statement);
			plan.CheckRight(((Schema.CatalogConstraint)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterReferenceStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterReferenceStatement localStatement = (AlterReferenceStatement)statement;
			AlterReferenceNode node = new AlterReferenceNode();
			node.AlterReferenceStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ReferenceName, true);
			if (!(objectValue is Schema.Reference))
				throw new CompilerException(CompilerException.Codes.ReferenceIdentifierExpected, statement);
			plan.CheckRight(((Schema.Reference)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterDeviceStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterDeviceStatement localStatement = (AlterDeviceStatement)statement;
			AlterDeviceNode node = new AlterDeviceNode();
			node.AlterDeviceStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.DeviceName, true);
			if (!(objectValue is Schema.Device))
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, statement);
			plan.CheckRight(((Schema.Device)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileAlterServerStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			AlterServerStatement localStatement = (AlterServerStatement)statement;
			AlterServerNode node = new AlterServerNode();
			node.AlterServerStatement = localStatement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ServerName, true);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected, statement);
			plan.CheckRight(((Schema.ServerLink)objectValue).GetRight(Schema.RightNames.Alter));
			//AcquireDependentLocks(APlan, LObject, LockMode.Exclusive);
			return node;
		}

		public static PlanNode CompileDropTableStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropTableStatement localStatement = (DropTableStatement)statement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ObjectName, false);
			if (objectValue == null)
			{
				Schema.Device device = GetDefaultDevice(plan, false);
				if (device != null)
				{
					plan.CheckDeviceReconcile(new Schema.BaseTableVar(localStatement.ObjectName, new Schema.TableType(), device));
					objectValue = ResolveCatalogIdentifier(plan, localStatement.ObjectName, true);	
				}
			}

			if (!(objectValue is Schema.BaseTableVar))
				throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, statement);

			Schema.BaseTableVar baseTableVar = (Schema.BaseTableVar)objectValue;
			plan.CheckRight(((Schema.BaseTableVar)objectValue).GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			if (baseTableVar.SessionObjectName != null)
			{
				int objectIndex = plan.PlanSessionObjects.IndexOf(baseTableVar.SessionObjectName);
				if (objectIndex >= 0)
					plan.PlanSessionObjects.RemoveAt(objectIndex);
			}
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			return new DropTableNode(baseTableVar, plan.ShouldAffectTimeStamp);
		}
		
		public static PlanNode CompileDropViewStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropViewStatement localStatement = (DropViewStatement)statement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ObjectName, true);
			if (!(objectValue is Schema.DerivedTableVar))
				throw new CompilerException(CompilerException.Codes.ViewIdentifierExpected, statement);
			Schema.DerivedTableVar derivedTableVar = (Schema.DerivedTableVar)objectValue;
			plan.CheckRight(derivedTableVar.GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			if (derivedTableVar.SessionObjectName != null)
			{
				int objectIndex = plan.PlanSessionObjects.IndexOf(derivedTableVar.SessionObjectName);
				if (objectIndex >= 0)
					plan.PlanSessionObjects.RemoveAt(objectIndex);
			}
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			return new DropViewNode(derivedTableVar, plan.ShouldAffectTimeStamp);
		}
		
		public static PlanNode CompileDropScalarTypeStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropScalarTypeStatement localStatement = (DropScalarTypeStatement)statement;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ObjectName, true);
			DropScalarTypeNode node = new DropScalarTypeNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			Schema.ScalarType scalarType = objectValue as Schema.ScalarType;
			if (scalarType == null)
				throw new CompilerException(CompilerException.Codes.ScalarTypeIdentifierExpected, statement);
			plan.CheckRight(scalarType.GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			node.ScalarType = scalarType;			
			
			// Remove the Equality and Comparison operators for this scalar type
			if ((node.ScalarType.EqualityOperator != null) && plan.PlanCatalog.Contains(node.ScalarType.EqualityOperator.Name))
				plan.PlanCatalog.Remove(node.ScalarType.EqualityOperator);
				
			if ((node.ScalarType.ComparisonOperator != null) && plan.PlanCatalog.Contains(node.ScalarType.ComparisonOperator.Name))
				plan.PlanCatalog.Remove(node.ScalarType.ComparisonOperator);

			// Remove the Default Selector Operators for this scalar type
			foreach (Schema.Representation representation in scalarType.Representations)
			{
				if ((representation.Selector != null) && plan.PlanCatalog.Contains(representation.Selector.Name))
					plan.PlanCatalog.Remove(representation.Selector);
	
				foreach (Schema.Property property in representation.Properties)
				{
					if ((property.ReadAccessor != null) && plan.PlanCatalog.Contains(property.ReadAccessor.Name))
						plan.PlanCatalog.Remove(property.ReadAccessor);
					
					if ((property.WriteAccessor != null) && plan.PlanCatalog.Contains(property.WriteAccessor.Name))
						plan.PlanCatalog.Remove(property.WriteAccessor);
				}
			}
			
			#if USETYPEINHERITANCE
			// Remove the Explicit Cast Operators for this scalar type
			foreach (Schema.Object operatorValue in scalarType.ExplicitCastOperators)
				if (APlan.PlanCatalog.Contains(operatorValue.Name))
					APlan.PlanCatalog.Remove(operatorValue);
			#endif
			
			// Remove the special selector and comparison operators for this scalar type
			if ((scalarType.IsSpecialOperator != null) && plan.PlanCatalog.Contains(scalarType.IsSpecialOperator.Name))
				plan.PlanCatalog.Remove(scalarType.IsSpecialOperator);

			foreach (Schema.Special special in scalarType.Specials)
			{
				if ((special.Selector != null) && plan.PlanCatalog.Contains(special.Selector.Name))
					plan.PlanCatalog.Remove(special.Selector);
				
				if ((special.Comparer != null) && plan.PlanCatalog.Contains(special.Comparer.Name))
					plan.PlanCatalog.Remove(special.Comparer);
			}

			plan.Catalog.OperatorResolutionCache.Clear();
			
			return node;
		}
		
		public static PlanNode CompileDropOperatorStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropOperatorStatement localStatement = (DropOperatorStatement)statement;
			DropOperatorNode node = new DropOperatorNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			node.OperatorSpecifier = new OperatorSpecifier();
			node.OperatorSpecifier.OperatorName = localStatement.ObjectName;
			node.OperatorSpecifier.Line = localStatement.Line;
			node.OperatorSpecifier.LinePos = localStatement.LinePos;
			node.OperatorSpecifier.FormalParameterSpecifiers.AddRange(localStatement.FormalParameterSpecifiers);
			node.DropOperator = ResolveOperatorSpecifier(plan, node.OperatorSpecifier);
			plan.CheckRight(node.DropOperator.GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(node.DropOperator.Name))
				plan.PlanCatalog.Remove(node.DropOperator);
			//APlan.AcquireCatalogLock(LNode.DropOperator, LockMode.Exclusive);
			plan.Catalog.OperatorResolutionCache.Clear(node.DropOperator.OperatorName);
			return node;
		}
		
		public static PlanNode CompileDropConstraintStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropConstraintStatement localStatement = (DropConstraintStatement)statement;
			DropConstraintNode node = new DropConstraintNode();
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ConstraintName, true);
			if (!(objectValue is Schema.CatalogConstraint))
				throw new CompilerException(CompilerException.Codes.ConstraintIdentifierExpected, statement);
			Schema.CatalogConstraint constraint = (Schema.CatalogConstraint)objectValue;
			plan.CheckRight(constraint.GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			if (constraint.SessionObjectName != null) 
			{
				int objectIndex = plan.PlanSessionObjects.IndexOf(constraint.SessionObjectName);
				if (objectIndex >= 0)
					plan.PlanSessionObjects.RemoveAt(objectIndex);
			}				
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			node.Constraint = constraint;
			return node;
		}
		
		public static PlanNode CompileDropReferenceStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropReferenceStatement localStatement = (DropReferenceStatement)statement;
			DropReferenceNode node = new DropReferenceNode();
			node.ShouldAffectDerivationTimeStamp = plan.ShouldAffectTimeStamp;
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ReferenceName, true);
			if (!(objectValue is Schema.Reference))
				throw new CompilerException(CompilerException.Codes.ReferenceIdentifierExpected, statement);
			node.ReferenceName = objectValue.Name;
			Schema.Reference reference = (Schema.Reference)objectValue;
			plan.CheckRight(reference.GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			if (reference.SessionObjectName != null)
			{
				int objectIndex = plan.PlanSessionObjects.IndexOf(reference.SessionObjectName);
				if (objectIndex >= 0)
					plan.PlanSessionObjects.RemoveAt(objectIndex);
			}
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			return node;
		}
		
		public static PlanNode CompileDropDeviceStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropDeviceStatement localStatement = (DropDeviceStatement)statement;
			DropDeviceNode node = new DropDeviceNode();
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ObjectName, true);
			if (!(objectValue is Schema.Device))
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected, statement);
			plan.CheckRight(((Schema.Device)objectValue).GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			node.DropDevice = (Schema.Device)objectValue;
			return node;
		}
		
		public static PlanNode CompileDropServerStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			DropServerStatement localStatement = (DropServerStatement)statement;
			DropServerNode node = new DropServerNode();
			Schema.Object objectValue = ResolveCatalogIdentifier(plan, localStatement.ObjectName, true);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected, statement);
			plan.CheckRight(((Schema.ServerLink)objectValue).GetRight(Schema.RightNames.Drop));
			if (plan.PlanCatalog.Contains(objectValue.Name))
				plan.PlanCatalog.Remove(objectValue);
			//APlan.AcquireCatalogLock(LObject, LockMode.Exclusive);
			node.ServerLink = (Schema.ServerLink)objectValue;
			return node;
		}

		/*
			Default : operator(var <AScalarType>)
			Validate :
			Change : operator(var <AScalarType>) || operator(const <AScalarType>, var <AScalarType>)
		*/		
		public static Schema.SignatureElement[] GetScalarTypeEventSignature(Plan plan, Statement statement, EventType eventType, Schema.ScalarType scalarType, bool simpleSignature)
		{
			switch (eventType)
			{
				case EventType.Default :
					return new Schema.SignatureElement[]{new Schema.SignatureElement(scalarType, Modifier.Var)};
				
				case EventType.Validate :
				case EventType.Change :
					if (simpleSignature)
						return new Schema.SignatureElement[]{new Schema.SignatureElement(scalarType, Modifier.Var)};
					return new Schema.SignatureElement[]{new Schema.SignatureElement(scalarType, Modifier.Const), new Schema.SignatureElement(scalarType, Modifier.Var)};
					
				default : throw new CompilerException(CompilerException.Codes.InvalidEventType, statement, scalarType.Name, eventType.ToString());
			}
		}

		/*
			Default : operator(var <column data type>)
			Validate :
			Change : operator(var <table var row type>) || operator(const <table var row type>, var <table var row type>)
		*/		
		public static Schema.SignatureElement[] GetTableVarColumnEventSignature(Plan plan, Statement statement, EventType eventType, Schema.TableVar tableVar, int columnIndex, bool simpleSignature)
		{
			switch (eventType)
			{
				case EventType.Default:
					return new Schema.SignatureElement[]{new Schema.SignatureElement(tableVar.DataType.Columns[columnIndex].DataType, Modifier.Var)};
				
				case EventType.Validate:
				case EventType.Change:
					if (simpleSignature)
						return new Schema.SignatureElement[]{new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Var)};
					return new Schema.SignatureElement[]{new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Const), new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Var)};
				
				default: throw new CompilerException(CompilerException.Codes.InvalidEventType, statement, tableVar.Name, eventType.ToString());
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
		public static Schema.SignatureElement[] GetTableVarEventSignature(Plan plan, Statement statement, EventType eventType, Schema.TableVar tableVar, bool simpleSignature)
		{
			switch (eventType)
			{
				case EventType.BeforeInsert:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Var),
							new Schema.SignatureElement(plan.DataTypes.SystemBoolean, Modifier.Var)
						};

				case EventType.AfterInsert:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Const)
						};
				
				case EventType.BeforeUpdate:
					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Const),
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Var),
							new Schema.SignatureElement(plan.DataTypes.SystemBoolean, Modifier.Var)
						};
				
				case EventType.AfterUpdate:
					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Const),
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Const)
						};
				
				case EventType.BeforeDelete:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Const),
							new Schema.SignatureElement(plan.DataTypes.SystemBoolean, Modifier.Var)
						};
				
				case EventType.AfterDelete:
					return 
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(tableVar.DataType.RowType, Modifier.Const)
						};
				
				case EventType.Default:
					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Var),
							new Schema.SignatureElement(plan.DataTypes.SystemString, Modifier.Const)
						};

				case EventType.Validate:
				case EventType.Change:
					if (simpleSignature)
						return
							new Schema.SignatureElement[]
							{
								new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Var),
								new Schema.SignatureElement(plan.DataTypes.SystemString, Modifier.Const)
							};

					return
						new Schema.SignatureElement[]
						{
							new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Const),
							new Schema.SignatureElement(new Schema.RowType(tableVar.DataType.Columns), Modifier.Var),
							new Schema.SignatureElement(plan.DataTypes.SystemString, Modifier.Const)
						};
				
				default: throw new CompilerException(CompilerException.Codes.InvalidEventType, statement, tableVar.Name, eventType.ToString());
			}
		}
		
		public static void CheckValidEventHandler(Plan plan, Statement statement, CreateEventHandlerNode node)
		{
			Schema.TableVar tableVar = node.EventSource as Schema.TableVar;
			if (tableVar != null)
			{
				if (node.EventSourceColumnIndex >= 0)
				{
					Schema.TableVarColumn column = tableVar.Columns[node.EventSourceColumnIndex];
					if (column.HasHandlers() && (column.EventHandlers.IndexOf(node.EventHandler.Operator, node.EventHandler.EventType) >= 0))
						throw new CompilerException(CompilerException.Codes.OperatorAlreadyAttachedToColumnEvent, statement, node.EventHandler.Operator.OperatorName, node.EventHandler.EventType.ToString(), column.Name, tableVar.Name);
				}
				else
				{
					if (tableVar.HasHandlers() && (tableVar.EventHandlers.IndexOf(node.EventHandler.Operator, node.EventHandler.EventType) >= 0))
						throw new CompilerException(CompilerException.Codes.OperatorAlreadyAttachedToObjectEvent, statement, node.EventHandler.Operator.OperatorName, node.EventHandler.EventType.ToString(), tableVar.Name);
				}
			}
			else
			{
				Schema.ScalarType scalarType = node.EventSource as Schema.ScalarType;
				if (scalarType != null)
				{
					if (scalarType.HasHandlers() && (scalarType.EventHandlers.IndexOf(node.EventHandler.Operator, node.EventHandler.EventType) >= 0))
						throw new CompilerException(CompilerException.Codes.OperatorAlreadyAttachedToObjectEvent, statement, node.EventHandler.Operator.OperatorName, node.EventHandler.EventType.ToString(), scalarType.Name);
				}
			}
		}
		
		public static PlanNode EmitCreateEventHandlerNode(Plan plan, AttachStatement statement, EventType eventType)
		{
			// build a trigger handler
			// resolve the event source
			// build the signature for the specified event
			// resolve the operator specifier
			// verify the resolved operator signature is compatible with the event signature
			// build the execution node
			CreateEventHandlerNode node = new CreateEventHandlerNode();
			Schema.SignatureElement[] eventSignature = null;
			Schema.SignatureElement[] newEventSignature = null;
			node.BeforeOperatorNames = statement.BeforeOperatorNames;
			bool creationObjectPushed = false;
			try
			{
				int objectID = Schema.Object.GetObjectID(statement.MetaData);
				if (statement.EventSourceSpecifier is ObjectEventSourceSpecifier)
				{
					node.EventSource = ResolveCatalogIdentifier(plan, ((ObjectEventSourceSpecifier)statement.EventSourceSpecifier).ObjectName, true);
					if (node.EventSource is Schema.TableVar)
					{
						node.EventHandler = new Schema.TableVarEventHandler(objectID, Schema.Object.GetGeneratedName(String.Format("{0}_{1}", node.EventSource.Name, eventType.ToString()), objectID));
						node.EventHandler.Owner = plan.User;
						node.EventHandler.Library = node.EventSource.Library == null ? null : plan.CurrentLibrary;
						node.EventHandler.IsGenerated = statement.IsGenerated;
						if (node.EventSource.IsSessionObject)
							node.EventHandler.SessionObjectName = node.EventHandler.Name;
						node.EventHandler.MergeMetaData(statement.MetaData);
						Tag tag = MetaData.GetTag(node.EventHandler.MetaData, "DAE.ATHandlerName");
						if (tag != Tag.None)
							node.EventHandler.ATHandlerName = tag.Value;
						plan.PushCreationObject(node.EventHandler);
						creationObjectPushed = true;
						plan.AttachDependency(node.EventSource);
						eventSignature = GetTableVarEventSignature(plan, statement, eventType, (Schema.TableVar)node.EventSource, true);
						newEventSignature = GetTableVarEventSignature(plan, statement, eventType, (Schema.TableVar)node.EventSource, false);
					}
					else if (node.EventSource is Schema.ScalarType)
					{
						node.EventHandler = new Schema.ScalarTypeEventHandler(objectID, Schema.Object.GetGeneratedName(String.Format("{0}_{1}", node.EventSource.Name, eventType.ToString()), objectID));
						node.EventHandler.Owner = plan.User;
						node.EventHandler.Library = node.EventSource.Library == null ? null : plan.CurrentLibrary;
						node.EventHandler.IsGenerated = statement.IsGenerated;
						if (node.EventSource.IsSessionObject)
							node.EventHandler.SessionObjectName = node.EventHandler.Name;
						node.EventHandler.MergeMetaData(statement.MetaData);
						Tag tag = MetaData.GetTag(node.EventHandler.MetaData, "DAE.ATHandlerName");
						if (tag != Tag.None)
							node.EventHandler.ATHandlerName = tag.Value;
						plan.PushCreationObject(node.EventHandler);
						creationObjectPushed = true;
						plan.AttachDependency(node.EventSource);
						eventSignature = GetScalarTypeEventSignature(plan, statement, eventType, (Schema.ScalarType)node.EventSource, true);
						newEventSignature = GetScalarTypeEventSignature(plan, statement, eventType, (Schema.ScalarType)node.EventSource, false);
					}
					else
						throw new CompilerException(CompilerException.Codes.InvalidEventSource, statement, node.EventSource.Name, eventType.ToString());
				}
				else if (statement.EventSourceSpecifier is ColumnEventSourceSpecifier)
				{
					node.EventSource = ResolveCatalogIdentifier(plan, ((ColumnEventSourceSpecifier)statement.EventSourceSpecifier).TableVarName, true);
					if (!(node.EventSource is Schema.TableVar))
						throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, statement.EventSourceSpecifier);
						
					node.EventSourceColumnIndex = ((Schema.TableVar)node.EventSource).DataType.Columns.IndexOf(((ColumnEventSourceSpecifier)statement.EventSourceSpecifier).ColumnName);
					if (node.EventSourceColumnIndex < 0)
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, statement.EventSourceSpecifier);
						
					node.EventHandler = new Schema.TableVarColumnEventHandler(objectID, Schema.Object.GetGeneratedName(String.Format("{0}_{1}_{2}", node.EventSource.Name, ((Schema.TableVar)node.EventSource).Columns[node.EventSourceColumnIndex].Name, eventType.ToString()), objectID));
					node.EventHandler.MergeMetaData(statement.MetaData);
					Tag tag = MetaData.GetTag(node.EventHandler.MetaData, "DAE.ATHandlerName");
					if (tag != Tag.None)
						node.EventHandler.ATHandlerName = tag.Value;
					plan.PushCreationObject(node.EventHandler);
					creationObjectPushed = true;

					node.EventHandler.Owner = plan.User;
					node.EventHandler.Library = node.EventSource.Library == null ? null : plan.CurrentLibrary;
					node.EventHandler.IsGenerated = statement.IsGenerated;
					if (node.EventSource.IsSessionObject)
						node.EventHandler.SessionObjectName = node.EventHandler.Name;
					plan.AttachDependency(node.EventSource);

					eventSignature = GetTableVarColumnEventSignature(plan, statement, eventType, (Schema.TableVar)node.EventSource, node.EventSourceColumnIndex, true);
					newEventSignature = GetTableVarColumnEventSignature(plan, statement, eventType, (Schema.TableVar)node.EventSource, node.EventSourceColumnIndex, false);
				}
				else
					throw new CompilerException(CompilerException.Codes.UnknownEventSourceSpecifierClass, statement.EventSourceSpecifier, statement.EventSourceSpecifier.GetType().Name);
				
				OperatorBindingContext context = new OperatorBindingContext(statement, statement.OperatorName, plan.NameResolutionPath, new Schema.Signature(eventSignature), false);
				ResolveOperator(plan, context);
				
				if (context.Operator == null)
				{
					eventSignature = newEventSignature;
					context = new OperatorBindingContext(statement, statement.OperatorName, plan.NameResolutionPath, new Schema.Signature(eventSignature), false);
					ResolveOperator(plan, context);
				}
				
				CheckOperatorResolution(plan, context);
				
				PlanNode[] arguments = new PlanNode[eventSignature.Length];
				for (int index = 0; index < arguments.Length; index++)
					arguments[index] = new StackReferenceNode(eventSignature[index].DataType, arguments.Length - (index + 1), true);

				node.EventHandler.EventType = eventType;	
				node.EventHandler.Operator = context.Operator;
				node.EventHandler.DetermineRemotable(plan.CatalogDeviceSession);
				
				// Check to see if the handler is already attached
				CheckValidEventHandler(plan, statement, node);
				
				for (int index = arguments.Length - 1; index >= 0; index--)
				{
					plan.Symbols.Push(new Symbol(String.Empty, arguments[index].DataType));
				}
				try
				{
					node.EventHandler.PlanNode = BuildCallNode(plan, context, arguments);
					node.EventHandler.PlanNode.DetermineDataType(plan);
					node.EventHandler.PlanNode.DetermineCharacteristics(plan);
				}
				finally
				{
					for (int index = 0; index < arguments.Length; index++)
						plan.Symbols.Pop();
				}
					
				if (node.EventSourceColumnIndex >= 0)
					node.EventHandler.Name = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_{2}_{3}", node.EventSource.Name, ((Schema.TableVar)node.EventSource).DataType.Columns[node.EventSourceColumnIndex].Name, node.EventHandler.Operator.OperatorName, node.EventHandler.EventType.ToString()), node.EventHandler.ID);
				else
					node.EventHandler.Name = Schema.Object.GetGeneratedName(String.Format("{0}_{1}_{2}", node.EventSource.Name, node.EventHandler.Operator.OperatorName, node.EventHandler.EventType.ToString()), node.EventHandler.ID);
				
				return node;
			}
			finally
			{
				if (creationObjectPushed)
					plan.PopCreationObject();
			}
		}
		
		public static PlanNode CompileAttachStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			// foreach event type
				// emit a CreateEventHandlerNode
			AttachStatement localStatement = (AttachStatement)statement;
			BlockNode node = new BlockNode();
			node.SetLineInfo(plan, statement.LineInfo);
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeInsert) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.BeforeInsert));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterInsert) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.AfterInsert));
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeUpdate) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.BeforeUpdate));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterUpdate) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.AfterUpdate));
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeDelete) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.BeforeDelete));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterDelete) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.AfterDelete));
			if ((localStatement.EventSpecifier.EventType & EventType.Default) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.Default));
			if ((localStatement.EventSpecifier.EventType & EventType.Change) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.Change));
			if ((localStatement.EventSpecifier.EventType & EventType.Validate) != 0)
				node.Nodes.Add(EmitCreateEventHandlerNode(plan, localStatement, EventType.Validate));
			return node;
		}
		
		public static PlanNode EmitAlterEventHandlerNode(Plan plan, InvokeStatement statement, EventType eventType)
		{
			// resolve the event target
			// resolve the operator specifier
			// find the trigger handler on the event target
			AlterEventHandlerNode node = new AlterEventHandlerNode();
			node.BeforeOperatorNames = statement.BeforeOperatorNames;
			if (statement.EventSourceSpecifier is ObjectEventSourceSpecifier)
			{
				node.EventSource = ResolveCatalogIdentifier(plan, ((ObjectEventSourceSpecifier)statement.EventSourceSpecifier).ObjectName, true);

				Schema.TableVar tableVar = node.EventSource as Schema.TableVar;
				Schema.ScalarType scalarType = node.EventSource as Schema.ScalarType;
				if (tableVar != null)
				{
					int handlerIndex = -1;
					if (tableVar.HasHandlers())
						handlerIndex = tableVar.EventHandlers.IndexOf(statement.OperatorName, eventType);
					if (handlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, statement.EventSpecifier, statement.OperatorName, eventType.ToString(), node.EventSource.Name);
					node.EventHandler = tableVar.EventHandlers[handlerIndex];
				}
				else if (scalarType != null)
				{
					int handlerIndex = -1;
					if (scalarType.HasHandlers())
						handlerIndex = scalarType.EventHandlers.IndexOf(statement.OperatorName, eventType);
					if (handlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, statement.EventSpecifier, statement.OperatorName, eventType.ToString(), node.EventSource.Name);
					node.EventHandler = scalarType.EventHandlers[handlerIndex];
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidEventSource, statement.EventSourceSpecifier, ((ObjectEventSourceSpecifier)statement.EventSourceSpecifier).ObjectName, eventType.ToString());
			}
			else if (statement.EventSourceSpecifier is ColumnEventSourceSpecifier)
			{
				node.EventSource = ResolveCatalogIdentifier(plan, ((ColumnEventSourceSpecifier)statement.EventSourceSpecifier).TableVarName, true);
				Schema.TableVar tableVar = node.EventSource as Schema.TableVar;
				if (tableVar == null)
					throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, statement.EventSourceSpecifier);
				node.EventSourceColumnIndex = tableVar.DataType.Columns.IndexOf(((ColumnEventSourceSpecifier)statement.EventSourceSpecifier).ColumnName);
				if (node.EventSourceColumnIndex < 0)
					throw new CompilerException(CompilerException.Codes.ColumnNameExpected, statement.EventSourceSpecifier);

				int handlerIndex = -1;
				if (tableVar.Columns[node.EventSourceColumnIndex].HasHandlers())
					handlerIndex = tableVar.Columns[node.EventSourceColumnIndex].EventHandlers.IndexOf(statement.OperatorName, eventType);
				if (handlerIndex < 0)
					throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToColumnEvent, statement.EventSpecifier, statement.OperatorName, eventType.ToString(), ((Schema.TableVar)node.EventSource).Columns[node.EventSourceColumnIndex].Name, node.EventSource.Name);
				node.EventHandler = tableVar.Columns[node.EventSourceColumnIndex].EventHandlers[handlerIndex];
			}
			else
				throw new CompilerException(CompilerException.Codes.UnknownEventSourceSpecifierClass, statement.EventSourceSpecifier, statement.EventSourceSpecifier.GetType().Name);
			
			return node;
		}
		
		public static PlanNode CompileInvokeStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			// foreach event type
				// emit a AlterEventHandlerNode
			InvokeStatement localStatement = (InvokeStatement)statement;
			BlockNode node = new BlockNode();
			node.SetLineInfo(plan, localStatement.LineInfo);
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeInsert) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.BeforeInsert));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterInsert) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.AfterInsert));
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeUpdate) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.BeforeUpdate));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterUpdate) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.AfterUpdate));
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeDelete) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.BeforeDelete));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterDelete) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.AfterDelete));
			if ((localStatement.EventSpecifier.EventType & EventType.Default) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.Default));
			if ((localStatement.EventSpecifier.EventType & EventType.Change) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.Change));
			if ((localStatement.EventSpecifier.EventType & EventType.Validate) != 0)
				node.Nodes.Add(EmitAlterEventHandlerNode(plan, localStatement, EventType.Validate));
			return node;
		}
		
		public static PlanNode EmitDropEventHandlerNode(Plan plan, DetachStatement statement, EventType eventType)
		{
			// resolve the event target
			// resolve the operator specifier
			// find the trigger handler on the event target
			DropEventHandlerNode node = new DropEventHandlerNode();
			if (statement.EventSourceSpecifier is ObjectEventSourceSpecifier)
			{
				node.EventSource = ResolveCatalogIdentifier(plan, ((ObjectEventSourceSpecifier)statement.EventSourceSpecifier).ObjectName, true);

				Schema.TableVar tableVar = node.EventSource as Schema.TableVar;
				Schema.ScalarType scalarType = node.EventSource as Schema.ScalarType;
				if (tableVar != null)
				{
					int handlerIndex = -1;
					if (tableVar.HasHandlers())
						handlerIndex = tableVar.EventHandlers.IndexOf(statement.OperatorName, eventType);
					if (handlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, statement.EventSpecifier, statement.OperatorName, eventType.ToString(), node.EventSource.Name);
					node.EventHandler = tableVar.EventHandlers[handlerIndex];
				}
				else if (scalarType != null)
				{
					int handlerIndex = -1;
					if (scalarType.HasHandlers())
						handlerIndex = scalarType.EventHandlers.IndexOf(statement.OperatorName, eventType);
					if (handlerIndex < 0)
						throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToObjectEvent, statement.EventSpecifier, statement.OperatorName, eventType.ToString(), node.EventSource.Name);
					node.EventHandler = scalarType.EventHandlers[handlerIndex];
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidEventSource, statement.EventSourceSpecifier, ((ObjectEventSourceSpecifier)statement.EventSourceSpecifier).ObjectName, eventType.ToString());
			}
			else if (statement.EventSourceSpecifier is ColumnEventSourceSpecifier)
			{
				node.EventSource = ResolveCatalogIdentifier(plan, ((ColumnEventSourceSpecifier)statement.EventSourceSpecifier).TableVarName, true);
				Schema.TableVar tableVar = node.EventSource as Schema.TableVar;
				if (tableVar == null)
					throw new CompilerException(CompilerException.Codes.TableIdentifierExpected, statement.EventSourceSpecifier);
				node.EventSourceColumnIndex = tableVar.DataType.Columns.IndexOf(((ColumnEventSourceSpecifier)statement.EventSourceSpecifier).ColumnName);
				if (node.EventSourceColumnIndex < 0)
					throw new CompilerException(CompilerException.Codes.ColumnNameExpected, statement.EventSourceSpecifier);

				int handlerIndex = -1;
				if (tableVar.Columns[node.EventSourceColumnIndex].HasHandlers())
					handlerIndex = tableVar.Columns[node.EventSourceColumnIndex].EventHandlers.IndexOf(statement.OperatorName, eventType);
				if (handlerIndex < 0)
					throw new CompilerException(CompilerException.Codes.OperatorNotAttachedToColumnEvent, statement.EventSpecifier, statement.OperatorName, eventType.ToString(), ((Schema.TableVar)node.EventSource).Columns[node.EventSourceColumnIndex].Name, node.EventSource.Name);
				node.EventHandler = tableVar.Columns[node.EventSourceColumnIndex].EventHandlers[handlerIndex];
			}
			else
				throw new CompilerException(CompilerException.Codes.UnknownEventSourceSpecifierClass, statement.EventSourceSpecifier, statement.EventSourceSpecifier.GetType().Name);
			
			return node;
		}
		
		public static PlanNode CompileDetachStatement(Plan plan, Statement statement)
		{
			if (plan.IsOperatorCreationContext)
				throw new CompilerException(CompilerException.Codes.DDLStatementInOperator, statement);

			// foreach event type
				// emit a DropEventHandlerNode
			DetachStatement localStatement = (DetachStatement)statement;
			BlockNode node = new BlockNode();
			node.SetLineInfo(plan, localStatement.LineInfo);
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeInsert) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.BeforeInsert));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterInsert) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.AfterInsert));
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeUpdate) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.BeforeUpdate));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterUpdate) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.AfterUpdate));
			if ((localStatement.EventSpecifier.EventType & EventType.BeforeDelete) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.BeforeDelete));
			if ((localStatement.EventSpecifier.EventType & EventType.AfterDelete) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.AfterDelete));
			if ((localStatement.EventSpecifier.EventType & EventType.Default) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.Default));
			if ((localStatement.EventSpecifier.EventType & EventType.Change) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.Change));
			if ((localStatement.EventSpecifier.EventType & EventType.Validate) != 0)
				node.Nodes.Add(EmitDropEventHandlerNode(plan, localStatement, EventType.Validate));
			return node;
		}
		
		public static Schema.Object ResolveCatalogObjectSpecifier(Plan plan, CatalogObjectSpecifier specifier)
		{
			if (specifier.IsOperator)
			{
				OperatorSpecifier localSpecifier = new OperatorSpecifier();
				localSpecifier.OperatorName = specifier.ObjectName;
				localSpecifier.FormalParameterSpecifiers.AddRange(specifier.FormalParameterSpecifiers);
				localSpecifier.Line = specifier.Line;
				localSpecifier.LinePos = specifier.LinePos;
				return ResolveOperatorSpecifier(plan, localSpecifier);
			}
			else
				return ResolveCatalogIdentifier(plan, specifier.ObjectName, true);
		}
		
		public static PlanNode EmitUserSecurityNode(Plan plan, string instruction, string rightName, string grantee)
		{
			return 
				EmitCallNode
				(
					plan, 
					instruction, 
					new PlanNode[]
					{
						EmitCallNode(plan, "System.Name", new PlanNode[]{new ValueNode(plan.DataTypes.SystemString, rightName)}),
						#if USEISTRING
						new ValueNode(APlan.DataTypes.SystemIString, AGrantee)
						#else
						new ValueNode(plan.DataTypes.SystemString, grantee)
						#endif
					}
				);
		}
		
		public static PlanNode EmitRoleSecurityNode(Plan plan, string instruction, string rightName, string grantee)
		{
			return
				EmitCallNode
				(
					plan,
					instruction,
					new PlanNode[]
					{
						EmitCallNode(plan, "System.Name", new PlanNode[]{new ValueNode(plan.DataTypes.SystemString, rightName)}),
						EmitCallNode(plan, "System.Name", new PlanNode[]{new ValueNode(plan.DataTypes.SystemString, grantee)})
					}
				);
		}

		public static PlanNode EmitRightNode(Plan plan, RightStatementBase statement, string rightName)
		{
			string instructionName = (statement is GrantStatement) ? "System.GrantRightTo" : ((statement is RevokeStatement) ? "System.RevokeRightFrom" : "System.RevertRightFor");
			switch (statement.GranteeType)
			{
				case GranteeType.User : return EmitUserSecurityNode(plan, instructionName + "User", rightName, statement.Grantee);
				case GranteeType.Role : return EmitRoleSecurityNode(plan, instructionName + "Role", rightName, statement.Grantee);
				default : throw new CompilerException(CompilerException.Codes.UnknownGranteeType, statement, statement.GranteeType.ToString());
			}
		}
		
		public static void EmitAllRightNodes(Plan plan, RightStatementBase statement, Schema.CatalogObject objectValue, BlockNode node)
		{
			if (objectValue != null)
			{
				string[] rights = objectValue.GetRights();
				for (int index = 0; index < rights.Length; index++)
					node.Nodes.Add(EmitRightNode(plan, statement, rights[index]));
			}
		}
		
		public static void EmitUsageRightNodes(Plan plan, RightStatementBase statement, Schema.CatalogObject objectValue, BlockNode node)
		{
			if (objectValue is Schema.TableVar)
			{
				Schema.TableVar tableVar = (Schema.TableVar)objectValue;
				node.Nodes.Add(EmitRightNode(plan, statement, tableVar.GetRight(Schema.RightNames.Select)));
				node.Nodes.Add(EmitRightNode(plan, statement, tableVar.GetRight(Schema.RightNames.Insert)));
				node.Nodes.Add(EmitRightNode(plan, statement, tableVar.GetRight(Schema.RightNames.Update)));
				node.Nodes.Add(EmitRightNode(plan, statement, tableVar.GetRight(Schema.RightNames.Delete)));
			}
			else if (objectValue is Schema.Operator)
			{
				node.Nodes.Add(EmitRightNode(plan, statement, ((Schema.Operator)objectValue).GetRight(Schema.RightNames.Execute)));
			}
			else if (objectValue is Schema.Device)
			{
				Schema.Device device = (Schema.Device)objectValue;
				node.Nodes.Add(EmitRightNode(plan, statement, device.GetRight(Schema.RightNames.Read)));
				node.Nodes.Add(EmitRightNode(plan, statement, device.GetRight(Schema.RightNames.Write)));
			}
		}
		
		public static PlanNode CompileSecurityStatement(Plan plan, Statement statement)
		{
			RightStatementBase localStatement = (RightStatementBase)statement;
			BlockNode node = new BlockNode();
			node.SetLineInfo(plan, localStatement.LineInfo);
			if (localStatement.RightType == RightSpecifierType.All)
			{
				if (localStatement.Target == null)
					throw new CompilerException(CompilerException.Codes.InvalidAllSpecification, statement);

				Schema.CatalogObject objectValue = (Schema.CatalogObject)ResolveCatalogObjectSpecifier(plan, localStatement.Target);
				EmitAllRightNodes(plan, localStatement, objectValue, node);
						
				if (objectValue is Schema.ScalarType)
				{
					Schema.ScalarType scalarType = (Schema.ScalarType)objectValue;
					if (scalarType.EqualityOperator != null)
						EmitAllRightNodes(plan, localStatement, scalarType.EqualityOperator, node);

					if (scalarType.ComparisonOperator != null)
						EmitAllRightNodes(plan, localStatement, scalarType.ComparisonOperator, node);
					
					if (scalarType.IsSpecialOperator != null)
						EmitAllRightNodes(plan, localStatement, scalarType.IsSpecialOperator, node);
						
					foreach (Schema.Special special in scalarType.Specials)
					{
						EmitAllRightNodes(plan, localStatement, special.Selector, node);
						EmitAllRightNodes(plan, localStatement, special.Comparer, node);
					}
						
					#if USETYPEINHERITANCE	
					foreach (Schema.Operator operatorValue in scalarType.ExplicitCastOperators)
						EmitAllRightNodes(APlan, localStatement, operatorValue, node);
					#endif
						
					foreach (Schema.Representation representation in scalarType.Representations)
					{
						if (representation.Selector != null)
							EmitAllRightNodes(plan, localStatement, representation.Selector, node);

						foreach (Schema.Property property in representation.Properties)
						{
							if (property.ReadAccessor != null)
								EmitAllRightNodes(plan, localStatement, property.ReadAccessor, node);
							if (property.WriteAccessor != null)
								EmitAllRightNodes(plan, localStatement, property.WriteAccessor, node);
						}
					}						
				}
			}
			else if (localStatement.RightType == RightSpecifierType.Usage)
			{
				if (localStatement.Target == null)
					throw new CompilerException(CompilerException.Codes.InvalidAllSpecification, statement);

				Schema.CatalogObject objectValue = (Schema.CatalogObject)ResolveCatalogObjectSpecifier(plan, localStatement.Target);
				EmitUsageRightNodes(plan, localStatement, objectValue, node);
						
				if (objectValue is Schema.ScalarType)
				{
					Schema.ScalarType scalarType = (Schema.ScalarType)objectValue;
					if (scalarType.EqualityOperator != null)
						EmitUsageRightNodes(plan, localStatement, scalarType.EqualityOperator, node);
						
					if (scalarType.ComparisonOperator != null)
						EmitUsageRightNodes(plan, localStatement, scalarType.ComparisonOperator, node);
						
					if (scalarType.IsSpecialOperator != null)
						EmitUsageRightNodes(plan, localStatement, scalarType.IsSpecialOperator, node);
						
					foreach (Schema.Special special in scalarType.Specials)
					{
						EmitUsageRightNodes(plan, localStatement, special.Selector, node);
						EmitUsageRightNodes(plan, localStatement, special.Comparer, node);
					}
						
					#if USETYPEINHERITANCE	
					foreach (Schema.Operator operatorValue in scalarType.ExplicitCastOperators)
						EmitUsageRightNodes(APlan, localStatement, operatorValue, node);
					#endif

					foreach (Schema.Representation representation in scalarType.Representations)
					{
						if (representation.Selector != null)
							EmitUsageRightNodes(plan, localStatement, representation.Selector, node);
						
						foreach (Schema.Property property in representation.Properties)
						{
							if (property.ReadAccessor != null)
								EmitUsageRightNodes(plan, localStatement, property.ReadAccessor, node);
							if (property.WriteAccessor != null)
								EmitUsageRightNodes(plan, localStatement, property.WriteAccessor, node);
						}
					}						
				}
			}
			else
			{
				Schema.Object objectValue = localStatement.Target != null ? ResolveCatalogObjectSpecifier(plan, localStatement.Target) : null;
				foreach (RightSpecifier rightSpecifier in localStatement.Rights)
				{
					string rightName = rightSpecifier.RightName;
					if (objectValue != null)
						rightName = objectValue.Name + rightName;
					
					node.Nodes.Add(EmitRightNode(plan, localStatement, rightName));
				}
			}

			if (node.NodeCount == 1)
				return node.Nodes[0];
			else
				return node;
		}

		public static PlanNode CompileBooleanExpression(Plan plan, Expression expression)
		{
			return CompileTypedExpression(plan, expression, plan.DataTypes.SystemBoolean);
		}
		
		public static PlanNode CompileTableExpression(Plan plan, Expression expression)
		{
			return CompileTypedExpression(plan, expression, plan.DataTypes.SystemTable);
		}
		
		public static PlanNode CompileTypedExpression(Plan plan, Expression expression, Schema.IDataType dataType)
		{
			return CompileTypedExpression(plan, expression, dataType, false);
		}
		
		public static PlanNode CompileTypedExpression(Plan plan, Expression expression, Schema.IDataType dataType, bool allowSourceSubset)
		{
			return EmitTypedNode(plan, CompileExpression(plan, expression), dataType, allowSourceSubset);
		}
		
		public static PlanNode EmitTypedNode(Plan plan, PlanNode node, Schema.IDataType dataType)
		{
			return EmitTypedNode(plan, node, dataType, false);
		}
		
		public static PlanNode EmitTypedNode(Plan plan, PlanNode node, Schema.IDataType dataType, bool allowSourceSubset)
		{
			if (node.DataType == null)
				throw new CompilerException(CompilerException.Codes.ExpressionExpected, plan.CurrentStatement());
			if (!node.DataType.Is(dataType))
			{
				ConversionContext context = FindConversionPath(plan, node.DataType, dataType, allowSourceSubset);
				CheckConversionContext(plan, context);
				node = ConvertNode(plan, node, context);
			}
			
			return Upcast(plan, node, dataType);
		}
		
		public static PlanNode CompileExpression(Plan plan, Statement statement)
		{
			return CompileExpression(plan, statement is SelectStatement ? ((SelectStatement)statement).CursorDefinition : (Expression)statement);
		}
		
		public static PlanNode CompileExpression(Plan plan, Expression expression)
		{
			return CompileExpression(plan, expression, false);
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
		public static PlanNode CompileExpression(Plan plan, Expression expression, bool isStatementContext)
		#endif
		{
			plan.PushStatement(expression);
			try
			{
				try
				{
					PlanNode result = null;
					switch (expression.GetType().Name)
					{
						case "UnaryExpression": result = CompileUnaryExpression(plan, (UnaryExpression)expression); break;
						case "BinaryExpression": result = CompileBinaryExpression(plan, (BinaryExpression)expression); break;
						case "BetweenExpression": result = CompileBetweenExpression(plan, (BetweenExpression)expression); break;
						case "ValueExpression": result = CompileValueExpression(plan, (ValueExpression)expression); break;
						case "ParameterExpression": result = CompileParameterExpression(plan, (ParameterExpression)expression); break;
						case "TableIdentifierExpression":
						case "ColumnIdentifierExpression":
						case "ServerIdentifierExpression":
						case "VariableIdentifierExpression":
						case "IdentifierExpression": result = CompileIdentifierExpression(plan, (IdentifierExpression)expression); break;
						case "QualifierExpression": result = CompileQualifierExpression(plan, (QualifierExpression)expression, isStatementContext); break;
						case "CallExpression": result = CompileCallExpression(plan, (CallExpression)expression); break;
						case "ListSelectorExpression": result = CompileListSelectorExpression(plan, (ListSelectorExpression)expression); break;
						case "D4IndexerExpression": result = CompileIndexerExpression(plan, (D4IndexerExpression)expression); break;
						case "IfExpression": result = CompileIfExpression(plan, (IfExpression)expression); break;
						case "CaseExpression": result = CompileCaseExpression(plan, (CaseExpression)expression); break;
						case "OnExpression": result = CompileOnExpression(plan, (OnExpression)expression); break;
						case "RenameAllExpression": result = CompileRenameAllExpression(plan, (RenameAllExpression)expression); break;
						case "IsExpression": result = CompileIsExpression(plan, (IsExpression)expression); break;
						case "AsExpression": result = CompileAsExpression(plan, (AsExpression)expression); break;
						#if CALCULESQUE
						case "NamedExpression": result = CompileNamedExpression(APlan, (NamedExpression)AExpression); break;
						#endif
						case "AdornExpression": result = CompileAdornExpression(plan, (AdornExpression)expression); break;
						case "RedefineExpression": result = CompileRedefineExpression(plan, (RedefineExpression)expression); break;
						case "TableSelectorExpression": result = CompileTableSelectorExpression(plan, (TableSelectorExpression)expression); break;
						case "RowSelectorExpression": result = CompileRowSelectorExpression(plan, (RowSelectorExpression)expression); break;
						case "CursorSelectorExpression": result = CompileCursorSelectorExpression(plan, (CursorSelectorExpression)expression); break;
						case "CursorDefinition": result = CompileCursorDefinition(plan, (CursorDefinition)expression); break;
						case "RowExtractorExpression": result = CompileRowExtractorExpression(plan, (RowExtractorExpression)expression); break;
						case "ColumnExtractorExpression": result = CompileColumnExtractorExpression(plan, (ColumnExtractorExpression)expression); break;
						case "RestrictExpression": result = CompileRestrictExpression(plan, (RestrictExpression)expression); break;
						case "ProjectExpression": result = CompileProjectExpression(plan, (ProjectExpression)expression); break;
						case "RemoveExpression": result = CompileRemoveExpression(plan, (RemoveExpression)expression); break;
						case "ExtendExpression": result = CompileExtendExpression(plan, (ExtendExpression)expression); break;
						case "SpecifyExpression": result = CompileSpecifyExpression(plan, (SpecifyExpression)expression); break;
						case "RenameExpression": result = CompileRenameExpression(plan, (RenameExpression)expression); break;
						case "AggregateExpression": result = CompileAggregateExpression(plan, (AggregateExpression)expression); break;
						case "OrderExpression": result = CompileOrderExpression(plan, (OrderExpression)expression); break;
						case "BrowseExpression": result = CompileBrowseExpression(plan, (BrowseExpression)expression); break;
						case "QuotaExpression": result = CompileQuotaExpression(plan, (QuotaExpression)expression); break;
						case "ExplodeColumnExpression": result = CompileExplodeColumnExpression(plan, (ExplodeColumnExpression)expression); break;
						case "ExplodeExpression": result = CompileExplodeExpression(plan, (ExplodeExpression)expression); break;
						case "UnionExpression": result = CompileUnionExpression(plan, (UnionExpression)expression); break;
						case "IntersectExpression": result = CompileIntersectExpression(plan, (IntersectExpression)expression); break;
						case "DifferenceExpression": result = CompileDifferenceExpression(plan, (DifferenceExpression)expression); break;
						case "ProductExpression": result = CompileProductExpression(plan, (ProductExpression)expression); break;
						#if USEDIVIDEEXPRESSION
						case "DivideExpression": result = CompileDivideExpression(APlan, (DivideExpression)AExpression); break;
						#endif
						case "HavingExpression": result = CompileHavingExpression(plan, (HavingExpression)expression); break;
						case "WithoutExpression": result = CompileWithoutExpression(plan, (WithoutExpression)expression); break;
						case "InnerJoinExpression": result = CompileInnerJoinExpression(plan, (InnerJoinExpression)expression); break;
						case "LeftOuterJoinExpression": result = CompileLeftOuterJoinExpression(plan, (LeftOuterJoinExpression)expression); break;
						case "RightOuterJoinExpression": result = CompileRightOuterJoinExpression(plan, (RightOuterJoinExpression)expression); break;
						default: throw new CompilerException(CompilerException.Codes.UnknownExpressionClass, expression, expression.GetType().FullName);
					}

					if (!isStatementContext && (result.DataType == null))
						throw new CompilerException(CompilerException.Codes.ExpressionExpected, expression);

					if (result.LineInfo == null)
					{
						result.Line = expression.Line;
						result.LinePos = expression.LinePos;
					}
					return result;
				}
				catch (CompilerException exception)
				{
					if ((exception.Line == -1) && (expression.Line != -1))
					{
						exception.Line = expression.Line;
						exception.LinePos = expression.LinePos;
					}
					if (String.IsNullOrEmpty(exception.Locator) && plan != null && plan.SourceContext != null && plan.SourceContext.Locator != null)
						exception.Locator = plan.SourceContext.Locator.Locator;
					throw;
				}
				catch (Exception exception)
				{
					if (!(exception is DataphorException))
						throw new CompilerException(CompilerException.Codes.InternalError, ErrorSeverity.System, CompilerErrorLevel.NonFatal, expression, exception);
						
					if (!(exception is ILocatorException))
						throw 
							new CompilerException(CompilerException.Codes.CompilerMessage, CompilerErrorLevel.NonFatal, expression, exception, exception.Message) 
							{ 
								Locator = 
									(plan != null && plan.SourceContext != null && plan.SourceContext.Locator != null) 
										? plan.SourceContext.Locator.Locator 
										: null
							};
					throw;
				}
			}
			finally
			{
				plan.PopStatement();
			}
		}
		
		// PlanNode - (class determined via instruction lookup from the server catalog)
		//		Nodes[0] - AOperand
		public static PlanNode EmitUnaryNode(Plan plan, string instruction, PlanNode operand)
		{
			return EmitCallNode(plan, instruction, new PlanNode[]{operand});
		}
		
		public static PlanNode CompileUnaryExpression(Plan plan, UnaryExpression unaryExpression)
		{
			PlanNode[] arguments = new PlanNode[]{CompileExpression(plan, unaryExpression.Expression)};
			OperatorBindingContext context = new OperatorBindingContext(unaryExpression, unaryExpression.Instruction, plan.NameResolutionPath, SignatureFromArguments(arguments), false);
			PlanNode node = EmitCallNode(plan, context, arguments);
			CheckOperatorResolution(plan, context);
			return node;
		}
		
		public static PlanNode EmitBinaryNode(Plan plan, PlanNode leftOperand, string instruction, PlanNode rightOperand)
		{
			return EmitBinaryNode(plan, new EmptyStatement(), leftOperand, instruction, rightOperand);
		}
		
		// PlanNode - (class determined via instruction lookup from the server catalog)
		//		Nodes[0] = ALeftOperand
		//		Nodes[1] = ARightOperand
		public static PlanNode EmitBinaryNode(Plan plan, Statement statement, PlanNode leftOperand, string instruction, PlanNode rightOperand)
		{
			if (String.Compare(instruction, Instructions.Equal) == 0)
				return EmitEqualNode(plan, statement, leftOperand, rightOperand);
			else if (String.Compare(instruction, Instructions.NotEqual) == 0)
				return EmitNotEqualNode(plan, statement, leftOperand, rightOperand);
			else if (String.Compare(instruction, Instructions.Less) == 0)
				return EmitLessNode(plan, statement, leftOperand, rightOperand);
			else if (String.Compare(instruction, Instructions.InclusiveLess) == 0)
				return EmitInclusiveLessNode(plan, statement, leftOperand, rightOperand);
			else if (String.Compare(instruction, Instructions.Greater) == 0)
				return EmitGreaterNode(plan, statement, leftOperand, rightOperand);
			else if (String.Compare(instruction, Instructions.InclusiveGreater) == 0)
				return EmitInclusiveGreaterNode(plan, statement, leftOperand, rightOperand);
			else if (String.Compare(instruction, Instructions.Compare) == 0)
				return EmitCompareNode(plan, statement, leftOperand, rightOperand);
			else
				return EmitCallNode(plan, statement, instruction, new PlanNode[]{leftOperand, rightOperand});
		}
		
		public static PlanNode EmitEqualNode(Plan plan, PlanNode leftOperand, PlanNode rightOperand)
		{
			return EmitEqualNode(plan, new EmptyStatement(), leftOperand, rightOperand);
		}
		
		public static PlanNode EmitEqualNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			// ALeftOperand = ARightOperand ::=
				// ALeftOperand ?= ARightOperand = 0
			PlanNode node = EmitCallNode(plan, statement, Instructions.Equal, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node = EmitBinaryNode(plan, statement, EmitCallNode(plan, statement, Instructions.Compare, new PlanNode[]{leftOperand, rightOperand}), Instructions.Equal, new ValueNode(plan.DataTypes.SystemInteger, 0));
			return node;
		}
		
		public static PlanNode EmitNotEqualNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			// ALeftOperand <> ARightOperand ::=
				// not(ALeftOperand = ARightOperand)
			PlanNode node = EmitCallNode(plan, statement, Instructions.NotEqual, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node = EmitUnaryNode(plan, Instructions.Not, EmitEqualNode(plan, statement, leftOperand, rightOperand));
			return node;
		}
		
		public static PlanNode EmitLessNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			// ALeftOperand < ARightOperand ::=
				// ALeftOperand ?= ARightOperand < 0
			PlanNode node = EmitCallNode(plan, statement, Instructions.Less, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node = EmitBinaryNode(plan, statement, EmitCallNode(plan, statement, Instructions.Compare, new PlanNode[]{leftOperand, rightOperand}), Instructions.Less, new ValueNode(plan.DataTypes.SystemInteger, 0));
			return node;
		}
		
		public static PlanNode EmitInclusiveLessNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			// ALeftOperand <= ARightOperand ::=
				// ALeftOperand ?= ARightOperand <= 0
			PlanNode node = EmitCallNode(plan, statement, Instructions.InclusiveLess, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node = EmitBinaryNode(plan, statement, EmitCompareNode(plan, statement, leftOperand, rightOperand), Instructions.InclusiveLess, new ValueNode(plan.DataTypes.SystemInteger, 0));
			return node;
		}
		
		public static PlanNode EmitGreaterNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			// ALeftOperand > ARightOperand ::=
				// ALeftOperand ?= ARightOperand > 0
			PlanNode node = EmitCallNode(plan, statement, Instructions.Greater, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node = EmitBinaryNode(plan, statement, EmitCompareNode(plan, statement, leftOperand, rightOperand), Instructions.Greater, new ValueNode(plan.DataTypes.SystemInteger, 0));
			return node;
		}
		
		public static PlanNode EmitInclusiveGreaterNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			// ALeftOperand >= ARightOperand ::=
				// ALeftOperand ?= ARightOperand >= 0
			PlanNode node = EmitCallNode(plan, statement, Instructions.InclusiveGreater, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node = EmitBinaryNode(plan, statement, EmitCompareNode(plan, statement, leftOperand, rightOperand), Instructions.InclusiveGreater, new ValueNode(plan.DataTypes.SystemInteger, 0));
			return node;
		}
		
		public static PlanNode EmitCompareNode(Plan plan, Statement statement, PlanNode leftOperand, PlanNode rightOperand)
		{
			//	ALeftOperand ?= ARightOperand ::= 
				//	if ALeftOperand = ARightOperand then 0 
				//		else if ALeftOperand < ARightOperand then -1 else 1
			PlanNode node = EmitCallNode(plan, statement, Instructions.Compare, new PlanNode[]{leftOperand, rightOperand}, false);
			if (node == null)
				node =
					EmitConditionNode
					(
						plan,
						EmitCallNode
						(
							plan,
							statement,
							Instructions.Equal,
							new PlanNode[]{leftOperand, rightOperand}
						),
						new ValueNode(plan.DataTypes.SystemInteger, 0),
						EmitConditionNode
						(
							plan,
							EmitCallNode
							(
								plan,
								statement,
								Instructions.Less,
								new PlanNode[]{leftOperand, rightOperand}
							),
							new ValueNode(plan.DataTypes.SystemInteger, -1),
							new ValueNode(plan.DataTypes.SystemInteger, 1)
						)
					);
					
			return node;
		}
		
		public static PlanNode CompileBinaryExpression(Plan plan, BinaryExpression binaryExpression)
		{
			PlanNode leftOperand = CompileExpression(plan, binaryExpression.LeftExpression);
			PlanNode rightOperand = CompileExpression(plan, binaryExpression.RightExpression);
			return EmitBinaryNode(plan, binaryExpression, leftOperand, binaryExpression.Instruction, rightOperand);
		}
		
		public static PlanNode CompileBetweenExpression(Plan plan, BetweenExpression betweenExpression)
		{
			PlanNode tempValue = CompileExpression(plan, betweenExpression.Expression);
			PlanNode lowerBound = CompileExpression(plan, betweenExpression.LowerExpression);
			PlanNode upperBound = CompileExpression(plan, betweenExpression.UpperExpression);
			PlanNode node = EmitCallNode(plan, betweenExpression, Instructions.Between, new PlanNode[]{tempValue, lowerBound, upperBound}, false);
			if (node == null)
			{
				node =
					Compiler.EmitBinaryNode
					(
						plan,
						betweenExpression,
						Compiler.EmitBinaryNode
						(
							plan,
							betweenExpression,
							tempValue,
							Instructions.InclusiveGreater,
							lowerBound
						),
						Instructions.And,
						Compiler.EmitBinaryNode
						(
							plan,
							betweenExpression,
							tempValue,
							Instructions.InclusiveLess,
							upperBound
						)
					);
			}
			return node;
		}
		
		public static ConversionContext FindConversionPath(Plan plan, Schema.IDataType sourceType, Schema.IDataType targetType)
		{
			return FindConversionPath(plan, sourceType, targetType, false);
		}
		
		public static ConversionContext FindConversionPath(Plan plan, Schema.IDataType sourceType, Schema.IDataType targetType, bool allowSourceSubset)
		{
			if ((sourceType is Schema.ScalarType) && (targetType is Schema.ScalarType))					
				return FindScalarConversionPath(plan, (Schema.ScalarType)sourceType, (Schema.ScalarType)targetType);
			else if ((sourceType is Schema.ITableType) && (targetType is Schema.ITableType))
				return FindTableConversionPath(plan, (Schema.ITableType)sourceType, (Schema.ITableType)targetType, allowSourceSubset);
			else if ((sourceType is Schema.IRowType) && (targetType is Schema.IRowType))
				return FindRowConversionPath(plan, (Schema.IRowType)sourceType, (Schema.IRowType)targetType, allowSourceSubset);
			else
				return new ConversionContext(sourceType, targetType);
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
		public static TableConversionContext FindTableConversionPath(Plan plan, Schema.ITableType sourceType, Schema.ITableType targetType, bool allowSourceSubset)
		{
			TableConversionContext context = new TableConversionContext(sourceType, targetType);
			context.CanConvert = (allowSourceSubset ? (sourceType.Columns.Count <= targetType.Columns.Count) : (sourceType.Columns.Count == targetType.Columns.Count));

			if (context.CanConvert)
			{
				int columnIndex;
				foreach (Schema.Column column in sourceType.Columns)
				{
					columnIndex = targetType.Columns.IndexOfName(column.Name);
					if (columnIndex >= 0)
					{
						if (!column.DataType.Is(targetType.Columns[columnIndex].DataType))
						{
							ConversionContext columnContext = FindConversionPath(plan, column.DataType, targetType.Columns[columnIndex].DataType);
							context.ColumnConversions.Add(column.Name, columnContext);
							context.CanConvert = context.CanConvert && columnContext.CanConvert;
						}
					}
					else
						context.CanConvert = false;
						
					if (!context.CanConvert)
						break;
				}
			}
			return context;
		}
		
		public static RowConversionContext FindRowConversionPath(Plan plan, Schema.IRowType sourceType, Schema.IRowType targetType, bool allowSourceSubset)
		{
			RowConversionContext context = new RowConversionContext(sourceType, targetType);
			context.CanConvert = (allowSourceSubset ? (sourceType.Columns.Count <= targetType.Columns.Count) : (sourceType.Columns.Count == targetType.Columns.Count));

			if (context.CanConvert)
			{
				int columnIndex;
				foreach (Schema.Column column in sourceType.Columns)
				{
					columnIndex = targetType.Columns.IndexOfName(column.Name);
					if (columnIndex >= 0)
					{
						if (!column.DataType.Is(targetType.Columns[columnIndex].DataType))
						{
							ConversionContext columnContext = FindConversionPath(plan, column.DataType, targetType.Columns[columnIndex].DataType);
							context.ColumnConversions.Add(column.Name, columnContext);
							context.CanConvert = context.CanConvert && columnContext.CanConvert;
						}
					}
					else
						context.CanConvert = false;
						
					if (!context.CanConvert)
						break;
				}
			}
			return context;
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
		
		public static void TraceScalarConversionPath(Plan plan, ScalarConversionContext context, Schema.ScalarType scalarType)
		{
			if (scalarType.Is(context.TargetType))
			{
				Schema.ScalarConversionPath path = new Schema.ScalarConversionPath();
				path.AddRange(context.CurrentPath);
				context.Paths.Add(path);
			}
			else
			{
				foreach (Schema.Conversion conversion in scalarType.ImplicitConversions)
				{
					if (!context.CurrentPath.Contains(conversion.TargetScalarType))
					{
						context.CurrentPath.Add(conversion);
						if ((context.CurrentPath.NarrowingScore > context.Paths.BestNarrowingScore) || ((context.CurrentPath.NarrowingScore == context.Paths.BestNarrowingScore) && context.CurrentPath.Count < context.Paths.ShortestLength))
							TraceScalarConversionPath(plan, context, conversion.TargetScalarType);
						context.CurrentPath.RemoveAt(context.CurrentPath.Count - 1);
					}
				}
			}
		}
		
		// Attempts to discover a conversion path from any supertype of ASourceType to any subtype of ATargetType.
		public static ScalarConversionContext FindScalarConversionPath(Plan plan, Schema.ScalarType sourceType, Schema.ScalarType targetType)
		{
			ScalarConversionContext context = new ScalarConversionContext(sourceType, targetType);
			
			if (!context.CanConvert)
			{
				#if USECONVERSIONPATHCACHE
				lock (plan.Catalog.ConversionPathCache)
				{
					Schema.ScalarConversionPath path = plan.Catalog.ConversionPathCache[sourceType, targetType];
					if (path != null)
						context.Paths.Add(path);
					else
					{
				#endif
						TraceScalarConversionPath(plan, context, sourceType);
				#if USECONVERSIONPATHCACHE
						if (context.CanConvert)
							plan.Catalog.ConversionPathCache.Add(sourceType, targetType, context.BestPath);
					}
				}
				#endif
			
				#if USETYPEINHERITANCE
				if (!context.CanConvert)
				{
					ScalarConversionContext parentContext;
					foreach (Schema.ScalarType parentType in ASourceType.ParentTypes)
					{
						parentContext = FindScalarConversionPath(parentType, ATargetType);
						if (parentContext.CanConvert)
							return parentContext;
					}
				}
				#endif
			}
			
			return context;
		}
		
		public static void CheckConversionContext(Plan plan, ConversionContext context)
		{
			CheckConversionContext(plan, context, true);
		}
		
		public static void CheckConversionContext(Plan plan, ConversionContext context, bool throwValue)
		{
			if (context is ScalarConversionContext)
			{
				ScalarConversionContext localContext = (ScalarConversionContext)context;
				if (localContext.BestPath == null)
				{
					if (!localContext.CanConvert)
					{
						switch (localContext.BestPaths.Count)
						{
							case 0 : 
								if (throwValue)
									throw new CompilerException(CompilerException.Codes.NoConversion, plan.CurrentStatement(), context.SourceType.Name, context.TargetType.Name);
								else
									plan.Messages.Add(new CompilerException(CompilerException.Codes.NoConversion, plan.CurrentStatement(), context.SourceType.Name, context.TargetType.Name));
							break;
							case 1 : break;
							default :
								List<string> conversions = new List<string>();
								foreach (Schema.ScalarConversionPath path in localContext.BestPaths)
									if (path.Count == localContext.Paths.ShortestLength)
										conversions.Add(path.ToString());
										
								if (conversions.Count > 1)
									if (throwValue)
										throw new CompilerException(CompilerException.Codes.AmbiguousConversion, plan.CurrentStatement(), context.SourceType.Name, context.TargetType.Name, ExceptionUtility.StringsToCommaList(conversions));
									else
										plan.Messages.Add(new CompilerException(CompilerException.Codes.AmbiguousConversion, plan.CurrentStatement(), context.SourceType.Name, context.TargetType.Name, ExceptionUtility.StringsToCommaList(conversions)));
							break;
						}
					}
				}
				#if REPORTNARROWINGCONVERSIONWARNINGS
				else
				{
					foreach (Schema.Conversion conversion in localContext.BestPath)
						if (conversion.IsNarrowing && !APlan.SuppressWarnings && !APlan.InTypeOfContext)
							APlan.Messages.Add(new CompilerException(CompilerException.Codes.NarrowingConversion, CompilerErrorLevel.Warning, APlan.CurrentStatement(), conversion.SourceScalarType.Name, conversion.TargetScalarType.Name));
				}
				#endif
			}
			else if (context is TableConversionContext)
			{
				TableConversionContext localContext = (TableConversionContext)context;
				foreach (KeyValuePair<string, ConversionContext> entry in localContext.ColumnConversions)
					CheckConversionContext(plan, entry.Value, false);
				
				if (!localContext.CanConvert)
					if (throwValue)
					{					
						Schema.ITableType sourceTableType = (Schema.ITableType)context.SourceType;
						Schema.ITableType targetTableType = (Schema.ITableType)context.TargetType;
						for (int index = 0; index < (sourceTableType.Columns.Count > targetTableType.Columns.Count ? sourceTableType.Columns.Count : targetTableType.Columns.Count); index++)
						{
							if ((index < sourceTableType.Columns.Count) && !targetTableType.Columns.Contains(sourceTableType.Columns[index]))
								throw new CompilerException(CompilerException.Codes.TargetTableTypeMissingColumn, plan.CurrentStatement(), sourceTableType.Columns[index].Name);
							else if ((index < targetTableType.Columns.Count) && !sourceTableType.Columns.Contains(targetTableType.Columns[index]))
								throw new CompilerException(CompilerException.Codes.SourceTableTypeMissingColumn, plan.CurrentStatement(), targetTableType.Columns[index].Name);
						}
					}
					else
					{
						Schema.ITableType sourceTableType = (Schema.ITableType)context.SourceType;
						Schema.ITableType targetTableType = (Schema.ITableType)context.TargetType;
						for (int index = 0; index < (sourceTableType.Columns.Count > targetTableType.Columns.Count ? sourceTableType.Columns.Count : targetTableType.Columns.Count); index++)
						{
							if ((index < sourceTableType.Columns.Count) && !targetTableType.Columns.Contains(sourceTableType.Columns[index]))
								plan.Messages.Add(new CompilerException(CompilerException.Codes.TargetTableTypeMissingColumn, plan.CurrentStatement(), sourceTableType.Columns[index].Name));
							else if ((index < targetTableType.Columns.Count) && !sourceTableType.Columns.Contains(targetTableType.Columns[index]))
								plan.Messages.Add(new CompilerException(CompilerException.Codes.SourceTableTypeMissingColumn, plan.CurrentStatement(), targetTableType.Columns[index].Name));
						}
					}
			}
			else if (context is RowConversionContext)
			{
				RowConversionContext localContext = (RowConversionContext)context;
				foreach (KeyValuePair<string, ConversionContext> entry in localContext.ColumnConversions)
					CheckConversionContext(plan, entry.Value, false);
				
				if (!localContext.CanConvert)
					if (throwValue)
					{
						Schema.IRowType sourceRowType = (Schema.IRowType)context.SourceType;
						Schema.IRowType targetRowType = (Schema.IRowType)context.TargetType;
						for (int index = 0; index < (sourceRowType.Columns.Count > targetRowType.Columns.Count ? sourceRowType.Columns.Count : targetRowType.Columns.Count); index++)
						{
							if ((index < sourceRowType.Columns.Count) && !targetRowType.Columns.Contains(sourceRowType.Columns[index]))
								throw new CompilerException(CompilerException.Codes.TargetRowTypeMissingColumn, plan.CurrentStatement(), sourceRowType.Columns[index].Name);
							else if ((index < targetRowType.Columns.Count) && !sourceRowType.Columns.Contains(targetRowType.Columns[index]))
								throw new CompilerException(CompilerException.Codes.SourceRowTypeMissingColumn, plan.CurrentStatement(), targetRowType.Columns[index].Name);
						}
					}
					else
					{
						Schema.IRowType sourceRowType = (Schema.IRowType)context.SourceType;
						Schema.IRowType targetRowType = (Schema.IRowType)context.TargetType;
						for (int index = 0; index < (sourceRowType.Columns.Count > targetRowType.Columns.Count ? sourceRowType.Columns.Count : targetRowType.Columns.Count); index++)
						{
							if ((index < sourceRowType.Columns.Count) && !targetRowType.Columns.Contains(sourceRowType.Columns[index]))
								plan.Messages.Add(new CompilerException(CompilerException.Codes.TargetRowTypeMissingColumn, plan.CurrentStatement(), sourceRowType.Columns[index].Name));
							else if ((index < targetRowType.Columns.Count) && !sourceRowType.Columns.Contains(targetRowType.Columns[index]))
								plan.Messages.Add(new CompilerException(CompilerException.Codes.SourceRowTypeMissingColumn, plan.CurrentStatement(), targetRowType.Columns[index].Name));
						}
					}
			}
			else
			{
				if (!context.CanConvert)
					if (throwValue)
						throw new CompilerException(CompilerException.Codes.NoConversion, plan.CurrentStatement(), context.SourceType.Name, context.TargetType.Name);
					else
						plan.Messages.Add(new CompilerException(CompilerException.Codes.NoConversion, plan.CurrentStatement(), context.SourceType.Name, context.TargetType.Name));
			}
		}
		
		public static PlanNode ConvertNode(Plan plan, PlanNode sourceNode, ConversionContext context)
		{
			if (sourceNode.DataType.Is(context.TargetType))
				return sourceNode;
				
			if ((sourceNode.DataType is Schema.ScalarType) && (context.TargetType is Schema.ScalarType))
				return ConvertScalarNode(plan, sourceNode, (ScalarConversionContext)context);
			else if ((sourceNode.DataType is Schema.ITableType) && (context.TargetType is Schema.ITableType))
				return ConvertTableNode(plan, sourceNode, (TableConversionContext)context);
			else if ((sourceNode.DataType is Schema.IRowType) && (context.TargetType is Schema.IRowType))
				return ConvertRowNode(plan, sourceNode, (RowConversionContext)context);
			else
				return sourceNode;
		}
		
		public static PlanNode ConvertScalarNode(Plan plan, PlanNode sourceNode, ScalarConversionContext context)
		{
			PlanNode node = sourceNode;
			if (context.BestPath != null)
				for (int index = 0; index < context.BestPath.Count; index++)
				{
					plan.AttachDependency(context.BestPath[index]);
					node = BuildCallNode(plan, new EmptyStatement(), context.BestPath[index].Operator, new PlanNode[]{Upcast(plan, node, context.BestPath[index].Operator.Operands[0].DataType)});
					node.DetermineDataType(plan);
					node.DetermineCharacteristics(plan);
				}
			return node;
		}
		
		public static PlanNode ConvertTableNode(Plan plan, PlanNode sourceNode, TableConversionContext context)
		{
			NamedColumnExpressions expressions = new NamedColumnExpressions();
			foreach (Schema.Column sourceColumn in context.SourceType.Columns)
			{
				Schema.Column targetColumn = context.TargetType.Columns[sourceColumn];
				if (!sourceColumn.DataType.Is(targetColumn.DataType))
				{
					Expression expression = new IdentifierExpression(sourceColumn.Name);
					ConversionContext localContext = (ConversionContext)context.ColumnConversions[sourceColumn.Name];
					ScalarConversionContext scalarContext = localContext as ScalarConversionContext;
					if ((scalarContext != null) && (scalarContext.BestPath != null) && (scalarContext.BestPath.Count > 0))
					{
						for (int index = 0; index < scalarContext.BestPath.Count; index++)
						{
							plan.AttachDependency(scalarContext.BestPath[index]);
							expression = new CallExpression(scalarContext.BestPath[index].Operator.OperatorName, new Expression[]{expression});
						}
						expressions.Add(new NamedColumnExpression(expression, targetColumn.Name));
					}
				}
			}
			if (expressions.Count > 0)
				return EmitRedefineNode(plan, sourceNode, expressions);
			else
				return sourceNode;
		}
		
		public static PlanNode ConvertRowNode(Plan plan, PlanNode sourceNode, RowConversionContext context)
		{
			NamedColumnExpressions expressions = new NamedColumnExpressions();
			foreach (Schema.Column sourceColumn in context.SourceType.Columns)
			{
				Schema.Column targetColumn = context.TargetType.Columns[sourceColumn];
				if (!sourceColumn.DataType.Is(targetColumn.DataType))
				{
					Expression expression = new IdentifierExpression(sourceColumn.Name);
					ConversionContext localContext = (ConversionContext)context.ColumnConversions[sourceColumn.Name];
					ScalarConversionContext scalarContext = localContext as ScalarConversionContext;
					if ((scalarContext != null) && (scalarContext.BestPath != null) && (scalarContext.BestPath.Count > 0))
					{
						for (int index = 0; index < scalarContext.BestPath.Count; index++)
						{
							plan.AttachDependency(scalarContext.BestPath[index]);
							expression = new CallExpression(scalarContext.BestPath[index].Operator.OperatorName, new Expression[]{expression});
						}
						expressions.Add(new NamedColumnExpression(expression, targetColumn.Name));
					}
				}
			}
			if (expressions.Count > 0)
				return EmitRedefineNode(plan, sourceNode, expressions);
			else
				return sourceNode;
		}
		
		// Given ADataType, and ATargetDataType guaranteed to be a super type of ADataType, find the casting path to ATargetDataType
		public static bool FindCastingPath(Schema.ScalarType dataType, Schema.ScalarType targetDataType, List<Schema.ScalarType> castingPath)
		{
			castingPath.Add(dataType);
			if (dataType.Equals(targetDataType))
				return true;
			else
			{
				#if USETYPEINHERITANCE
				foreach (Schema.ScalarType parentType in ADataType.ParentTypes)
					if (FindCastingPath(parentType, ATargetDataType, ACastingPath))
						return true;
				#endif
				castingPath.Remove(dataType);
				return false;
			}
		}
		
		public static PlanNode DowncastScalar(Plan plan, PlanNode planNode, Schema.ScalarType targetDataType)
		{
			if (!targetDataType.Equals(plan.DataTypes.SystemScalar) && !planNode.DataType.Equals(plan.DataTypes.SystemScalar))
			{
				List<Schema.ScalarType> castingPath = new List<Schema.ScalarType>();
				if (!FindCastingPath(targetDataType, (Schema.ScalarType)planNode.DataType, castingPath))
					throw new CompilerException(CompilerException.Codes.CastingPathNotFound, plan.CurrentStatement(), targetDataType.Name, planNode.DataType.Name);
					
				// Remove the last element, it is the data type of APlanNode.
				castingPath.RemoveAt(castingPath.Count - 1);
				
				PlanNode localPlanNode;
				Schema.ScalarType localTargetDataType;
				for (int index = castingPath.Count - 1; index >= 0; index--)
				{
					localTargetDataType = castingPath[index];
					if ((localTargetDataType.ClassDefinition != null) && (((Schema.ScalarType)planNode.DataType).ClassDefinition != null) && !Object.ReferenceEquals(plan.ValueManager.GetConveyor((Schema.ScalarType)planNode.DataType).GetType(), plan.ValueManager.GetConveyor(localTargetDataType).GetType()))
					{
						localPlanNode = EmitCallNode(plan, localTargetDataType.Name, new PlanNode[]{planNode}, false);
						if (localPlanNode == null)
							throw new CompilerException(CompilerException.Codes.PhysicalCastOperatorNotFound, plan.CurrentStatement(), planNode.DataType.Name, localTargetDataType.Name);
						planNode = localPlanNode;
					}
				}
			}
			return planNode;
		}
		
		public static PlanNode UpcastScalar(Plan plan, PlanNode planNode, Schema.ScalarType targetDataType)
		{
			// If the target data type is not scalar or alpha
			if (!targetDataType.Equals(plan.DataTypes.SystemScalar))
			{
				List<Schema.ScalarType> castingPath = new List<Schema.ScalarType>();
				if (!FindCastingPath((Schema.ScalarType)planNode.DataType, targetDataType, castingPath))
					throw new CompilerException(CompilerException.Codes.CastingPathNotFound, plan.CurrentStatement(), planNode.DataType.Name, targetDataType.Name);
					
				// Remove the first element, it is the data type of APlanNode.
				castingPath.RemoveAt(0);
				
				PlanNode localPlanNode;
				Schema.ScalarType localTargetDataType;
				for (int index = 0; index < castingPath.Count; index++)
				{
					localTargetDataType = castingPath[index];
					if ((localTargetDataType.ClassDefinition != null) && (((Schema.ScalarType)planNode.DataType).ClassDefinition != null) && !Object.ReferenceEquals(plan.ValueManager.GetConveyor((Schema.ScalarType)planNode.DataType).GetType(), plan.ValueManager.GetConveyor(localTargetDataType).GetType()))
					{
						localPlanNode = EmitCallNode(plan, localTargetDataType.Name, new PlanNode[]{planNode}, false);
						if (localPlanNode == null)
							throw new CompilerException(CompilerException.Codes.PhysicalCastOperatorNotFound, plan.CurrentStatement(), planNode.DataType.Name, localTargetDataType.Name);
						planNode = localPlanNode;
					}
				}
			}
			return planNode;
		}
		
		public static PlanNode DowncastTable(Plan plan, PlanNode planNode, Schema.ITableType targetDataType)
		{
			// TODO: DowncastTable
			return planNode;
		}
		
		public static PlanNode UpcastTable(Plan plan, PlanNode planNode, Schema.ITableType targetDataType)
		{
			// TODO: UpcastTable
			return planNode;
		}
		
		public static PlanNode DowncastRow(Plan plan, PlanNode planNode, Schema.IRowType targetDataType)
		{
			// TODO: DowncastRow
			return planNode;
		}
		
		public static PlanNode UpcastRow(Plan plan, PlanNode planNode, Schema.IRowType targetDataType)
		{
			// TODO: UpcastRow
			return planNode;
		}
		
		public static PlanNode DowncastList(Plan plan, PlanNode planNode, Schema.IListType targetDataType)
		{
			// TODO: DowncastList
			return planNode;
		}
		
		public static PlanNode UpcastList(Plan plan, PlanNode planNode, Schema.IListType targetDataType)
		{
			// TODO: UpcastList
			return planNode;
		}
		
		// Given APlanNode and ATargetDataType that is guaranteed to be a sub type of the data type of APlanNode,
		// provide physical conversions if necessary
		public static PlanNode Downcast(Plan plan, PlanNode planNode, Schema.IDataType targetDataType)
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
			return planNode;
			#endif
		}

		// Given APlanNode and ATargetDataType that is guaranteed to be a super type of the data type of APlanNode,
		// provide physical conversions if necessary
		public static PlanNode Upcast(Plan plan, PlanNode planNode, Schema.IDataType targetDataType)
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
			return planNode;
			#endif
		}
		
		public static PlanNode BuildCallNode(Plan plan, OperatorBindingContext context, PlanNode[] arguments)
		{
			PlanNode[] localArguments = new PlanNode[arguments.Length];
			if (!context.Matches.IsExact)
			{
				ConversionContext localContext;
				OperatorMatch partialMatch = context.Matches.Match;
				for (int index = 0; index < localArguments.Length; index++)
				{
					localContext = partialMatch.ConversionContexts[index];
					if (localContext != null)
						localArguments[index] = ConvertNode(plan, arguments[index], localContext);
					else
						localArguments[index] = arguments[index];
					localArguments[index] = Upcast(plan, localArguments[index], context.Operator.Operands[index].DataType);
				}
			}
			else
			{
				for (int index = 0; index < localArguments.Length; index++)
					localArguments[index] = Upcast(plan, arguments[index], context.Operator.Operands[index].DataType);
			}

			return BuildCallNode(plan, context.Statement, context.Operator, localArguments);
		}
		
		public static PlanNode BuildCallNode(Plan plan, Statement statement, Schema.Operator operatorValue, PlanNode[] arguments)
		{
			if (operatorValue is Schema.AggregateOperator)
				throw new CompilerException(CompilerException.Codes.InvalidAggregateInvocation, statement, operatorValue.Name);
				
			plan.CheckRight(operatorValue.GetRight(Schema.RightNames.Execute));
			
			if (operatorValue.ShouldRecompile)
				RecompileOperator(plan, operatorValue);
			
			plan.AttachDependency(operatorValue);
			
			if ((statement != null) && (statement.Modifiers != null))
			{
				plan.SetIsLiteral(Convert.ToBoolean(LanguageModifiers.GetModifier(statement.Modifiers, "IsLiteral", operatorValue.IsLiteral.ToString())));
				plan.SetIsFunctional(Convert.ToBoolean(LanguageModifiers.GetModifier(statement.Modifiers, "IsFunctional", operatorValue.IsFunctional.ToString())));
				plan.SetIsDeterministic(Convert.ToBoolean(LanguageModifiers.GetModifier(statement.Modifiers, "IsDeterministic", operatorValue.IsDeterministic.ToString())));
				plan.SetIsRepeatable(Convert.ToBoolean(LanguageModifiers.GetModifier(statement.Modifiers, "IsRepeatable", operatorValue.IsRepeatable.ToString())));
				plan.SetIsNilable(Convert.ToBoolean(LanguageModifiers.GetModifier(statement.Modifiers, "IsNilable", operatorValue.IsNilable.ToString())));
			}
			else
			{
				plan.SetIsLiteral(operatorValue.IsLiteral);
				plan.SetIsFunctional(operatorValue.IsFunctional);
				plan.SetIsDeterministic(operatorValue.IsDeterministic);
				plan.SetIsRepeatable(operatorValue.IsRepeatable);
				plan.SetIsNilable(operatorValue.IsNilable);
			}

			PlanNode node;
			if (operatorValue.Block.ClassDefinition != null)
			{
				plan.CheckClassDependency(operatorValue.Block.ClassDefinition);
				node = (PlanNode)plan.CreateObject(operatorValue.Block.ClassDefinition, null);
			}
			else
			{
				node = new CallNode();
			}
			
			if (node is InstructionNodeBase)
				((InstructionNodeBase)node).Operator = operatorValue;
				
			if (node.IsBreakable && (statement != null))
				node.SetLineInfo(plan, statement.LineInfo);

			node.DataType = operatorValue.ReturnDataType;
			for (int index = 0; index < arguments.Length; index++)
			{
				PlanNode argumentNode = arguments[index];
				if (argumentNode.DataType is Schema.ITableType)
				{
					// The previous definition for this notion used whether or not the operator was host-implemented.
					// This proved inadequate because a ValueInListNode, for example, expects to be given table-valued arguments,
					// but is a host-implemented operator. This characteristic was introduced to more accurately model the requirement
					// and is set in various node constructors as appropriate. This should produce the same behavior, but if not,
					// the commented out code here can be used to revert to the old behavior and detect the scenario that is causing an issue.
					// Note also that treating tables as a different type from table values would potentially yield a cleaner solution. This
					// was not done initially to prevent what is effectively a physical implementation detail from showing through to the
					// logical model, but there is likely a cleaner way to achieve the same result using types.
					//if (operatorValue.Block.ClassDefinition != null)
					if (!node.ExpectsTableValues)
					{
						//if (node.ExpectsTableValues)
							//throw new InvalidOperationException("Invalid ExpectsTableValues value");

						// This is a host-implemented operator and should be given table nodes
						argumentNode = EnsureTableNode(plan, argumentNode);
					}
					else
					{
						//if (!node.ExpectsTableValues)
							//throw new InvalidOperationException("Invalid ExpectsTableValues value");

						// This is a D4-implemented operator and should not be given table nodes
						argumentNode = EnsureTableValueNode(plan, argumentNode);
					}
				}
				
				if (operatorValue.Operands[index].Modifier == Modifier.Var)
				{
					PlanNode actualArgumentNode = argumentNode;
					if (actualArgumentNode is ParameterNode)
						actualArgumentNode = actualArgumentNode.Nodes[0];
						
					if (actualArgumentNode is StackReferenceNode)
						plan.Symbols.SetIsModified(((StackReferenceNode)actualArgumentNode).Location);
					else if (actualArgumentNode is TableVarNode)
						((TableVarNode)actualArgumentNode).TableVar.IsModified = true;
				}
				node.Nodes.Add(argumentNode);
			}

			if (statement != null)
				node.Modifiers = statement.Modifiers;			
			return node;
		}
		
		public static Schema.Signature SignatureFromArguments(PlanNode[] arguments)
		{
			Schema.SignatureElement[] signatureElements = new Schema.SignatureElement[arguments.Length];
			for (int index = 0; index < arguments.Length; index++)
			{
				if (arguments[index] is ParameterNode)
					signatureElements[index] = new Schema.SignatureElement(arguments[index].DataType, ((ParameterNode)arguments[index]).Modifier);
				else
					signatureElements[index] = new Schema.SignatureElement(arguments[index].DataType);
			}
				
			return new Schema.Signature(signatureElements);
		}
		
		public static PlanNode FindCallNode(Plan plan, Statement statement, string instruction, PlanNode[] arguments)
		{
			OperatorBindingContext context = new OperatorBindingContext(statement, instruction, plan.NameResolutionPath, SignatureFromArguments(arguments), false);
			PlanNode node = FindCallNode(plan, context, arguments);
			CheckOperatorResolution(plan, context);
			return node;
		}

		/// <summary>The main overload of FindCallNode which all other overloads call.  All call resolutions funnel through this method.</summary>		
		public static PlanNode FindCallNode(Plan plan, OperatorBindingContext context, PlanNode[] arguments)
		{
			ResolveOperator(plan, context);
			if (context.Operator != null)
			{
				if (Convert.ToBoolean(MetaData.GetTag(context.Operator.MetaData, "DAE.IsDeprecated", "False")) && !plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.DeprecatedOperator, CompilerErrorLevel.Warning, context.Operator.DisplayName));
					
				if (context.IsExact)
					return BuildCallNode(plan, context.Statement, context.Operator, arguments);
				else
					return BuildCallNode(plan, context, arguments);
			}
			else
				return null;
		}
		
		public static PlanNode EmitCallNode(Plan plan, string instruction, PlanNode[] arguments)
		{
			return EmitCallNode(plan, new EmptyStatement(), instruction, arguments, true, false);
		}
		
		public static PlanNode EmitCallNode(Plan plan, string instruction, PlanNode[] arguments, bool mustResolve)
		{
			return EmitCallNode(plan, new EmptyStatement(), instruction, arguments, mustResolve, false);
		}
		
		public static PlanNode EmitCallNode(Plan plan, string instruction, PlanNode[] arguments, bool mustResolve, bool isExact)
		{
			return EmitCallNode(plan, new EmptyStatement(), instruction, arguments, mustResolve, isExact);
		}
		
		public static PlanNode EmitCallNode(Plan plan, Statement statement, string instruction, PlanNode[] arguments)
		{
			return EmitCallNode(plan, statement, instruction, arguments, true, false);
		}
		
		public static PlanNode EmitCallNode(Plan plan, Statement statement, string instruction, PlanNode[] arguments, bool mustResolve)
		{
			return EmitCallNode(plan, statement, instruction, arguments, mustResolve, false);
		}
		
		public static PlanNode EmitCallNode(Plan plan, Statement statement, string instruction, PlanNode[] arguments, bool mustResolve, bool isExact)
		{
			foreach (PlanNode argument in arguments)
				if (argument.DataType == null)
					throw new CompilerException(CompilerException.Codes.ExpressionExpected, statement);

			OperatorBindingContext context = new OperatorBindingContext(statement, instruction, plan.NameResolutionPath, SignatureFromArguments(arguments), isExact);
			PlanNode planNode = EmitCallNode(plan, context, arguments);
			if (mustResolve)
				CheckOperatorResolution(plan, context);
			return planNode;
		}
		
		// PlanNode - (class determined via instruction lookup from the server catalog)
		//		Nodes[0..ArgumentCount - 1] = PlanNodes for each argument in the call
		public static PlanNode EmitCallNode(Plan plan, OperatorBindingContext context, PlanNode[] arguments)
		{
			PlanNode node = FindCallNode(plan, context, arguments);
			if (node != null)
			{
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
			}
			return node;
		}
		
		public static Schema.Signature AggregateSignatureFromArguments(PlanNode targetNode, string[] columnNames, bool mustResolve)
		{
			Schema.ITableType targetType = (Schema.ITableType)targetNode.DataType;
			int[] columnIndexes = new int[columnNames.Length];
			Schema.SignatureElement[] elements = new Schema.SignatureElement[columnNames.Length];
			for (int index = 0; index < columnNames.Length; index++)
			{
				columnIndexes[index] = targetType.Columns.IndexOf(columnNames[index]);
				if (columnIndexes[index] >= 0)
					elements[index] = new Schema.SignatureElement(targetType.Columns[columnIndexes[index]].DataType);
				else
				{
					if (mustResolve)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ColumnNotFound, columnNames[index]);
					return null;
				}
			}
			
			return new Schema.Signature(elements);
		}

		public static AggregateCallNode BuildAggregateCallNode(Plan plan, Statement statement, Schema.AggregateOperator operatorValue, PlanNode targetNode, string[] columnNames, OrderColumnDefinitions orderColumns)
		{
			targetNode = EnsureTableNode(plan, targetNode);
			Schema.ITableType targetType = (Schema.ITableType)targetNode.DataType;
			int[] columnIndexes = new int[columnNames.Length];
			for (int index = 0; index < columnNames.Length; index++)
				columnIndexes[index] = targetType.Columns.IndexOf(columnNames[index]);
			
			plan.CheckRight(operatorValue.GetRight(Schema.RightNames.Execute));
			
			if (operatorValue.ShouldRecompile)
				RecompileAggregateOperator(plan, operatorValue);
			
			plan.AttachDependency(operatorValue);
			AggregateCallNode node = new AggregateCallNode();
			node.Operator = operatorValue;
			node.AggregateColumnIndexes = columnIndexes;
			node.ValueNames = new string[columnIndexes.Length];
			for (int index = 0; index < columnIndexes.Length; index++)
				node.ValueNames[index] = operatorValue.Operands[index].Name;

			// TargetDataType determination (Upcast and Convert)
			Schema.ITableType expectedType = (Schema.ITableType)new Schema.TableType();
			for (int index = 0; index < targetType.Columns.Count; index++)
			{
				if (((IList)columnIndexes).Contains(index))
					expectedType.Columns.Add(new Schema.Column(targetType.Columns[index].Name, operatorValue.Operands[((IList)columnIndexes).IndexOf(index)].DataType));
				else
					expectedType.Columns.Add(new Schema.Column(targetType.Columns[index].Name, targetType.Columns[index].DataType));
			}
			
			if (!targetNode.DataType.Is(expectedType))
			{
				ConversionContext context = FindConversionPath(plan, targetNode.DataType, expectedType);
				CheckConversionContext(plan, context);
				targetNode = ConvertNode(plan, targetNode, context);

				// Redetermine aggregate column indexes using the new target data type (could be diferrent indexes)
				targetType = (Schema.ITableType)targetNode.DataType;
				columnIndexes = new int[columnNames.Length];
				for (int index = 0; index < columnNames.Length; index++)
					columnIndexes[index] = targetType.Columns.IndexOf(columnNames[index]);
				node.AggregateColumnIndexes = columnIndexes;
			}
			
			if (orderColumns != null)
				targetNode = Compiler.EmitOrderNode(plan, targetNode, Compiler.CompileOrderColumnDefinitions(plan, ((TableNode)targetNode).TableVar, orderColumns, null, false), false);
				
			node.Nodes.Add(Upcast(plan, targetNode, expectedType));
			
			if (operatorValue.Initialization.ClassDefinition != null)
			{
				plan.CheckClassDependency(operatorValue.Initialization.ClassDefinition);
				PlanNode initializationNode = (PlanNode)plan.CreateObject(operatorValue.Initialization.ClassDefinition, null);
				initializationNode.DetermineCharacteristics(plan);
				node.Nodes.Add(initializationNode);
			}
			else
				node.Nodes.Add(operatorValue.Initialization.BlockNode);
				
			if (operatorValue.Aggregation.ClassDefinition != null)
			{
				plan.CheckClassDependency(operatorValue.Aggregation.ClassDefinition);
				PlanNode aggregationNode = (PlanNode)plan.CreateObject(operatorValue.Aggregation.ClassDefinition, null);
				aggregationNode.DetermineCharacteristics(plan);
				node.Nodes.Add(aggregationNode);
			}
			else
				node.Nodes.Add(operatorValue.Aggregation.BlockNode);
			
			if (operatorValue.Finalization.ClassDefinition != null)
			{
				plan.CheckClassDependency(operatorValue.Finalization.ClassDefinition);
				PlanNode finalizationNode = (PlanNode)plan.CreateObject(operatorValue.Finalization.ClassDefinition, null);
				finalizationNode.DetermineCharacteristics(plan);
				node.Nodes.Add(finalizationNode);
			}
			else
				node.Nodes.Add(operatorValue.Finalization.BlockNode);
			
			node.DataType = operatorValue.ReturnDataType;
			
			if (statement != null)
				node.Modifiers = statement.Modifiers;
				
			return node;
		}
		
		public static AggregateCallNode FindAggregateCallNode(Plan plan, OperatorBindingContext context, PlanNode targetNode, string[] columnNames, OrderColumnDefinitions orderColumns)
		{
			ResolveOperator(plan, context);
			if (context.Operator is Schema.AggregateOperator)
				return BuildAggregateCallNode(plan, context.Statement, (Schema.AggregateOperator)context.Operator, targetNode, columnNames, orderColumns);
			else
			{
				if (context.Operator != null)
				{
					//APlan.ReleaseCatalogLock(AContext.Operator);
					context.Operator = null;
				}
				return null;
			}
		}
		
		public static AggregateCallNode EmitAggregateCallNode(Plan plan, OperatorBindingContext context, PlanNode targetNode, string[] columnNames, OrderColumnDefinitions orderColumns)
		{
			AggregateCallNode node = FindAggregateCallNode(plan, context, targetNode, columnNames, orderColumns);
			if (node != null)
			{
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
			}
			return node;
		}
		
		public static PlanNode CompileCallExpression(Plan plan, CallExpression callExpression)
		{
			OperatorBindingContext context;
			PlanNode[] planNodes = new PlanNode[callExpression.Expressions.Count];
			for (int index = 0; index < callExpression.Expressions.Count; index++)
			{
				if (callExpression.Expressions.Count == 1)
				{
					callExpression.Expressions[index] = CollapseColumnExtractorExpression(callExpression.Expressions[index]);
					if (callExpression.Expressions[index] is ColumnExtractorExpression)
					{
						ColumnExtractorExpression columnExpression = (ColumnExtractorExpression)callExpression.Expressions[index];
						PlanNode extractionTargetNode = CompileExpression(plan, columnExpression.Expression);
						if (extractionTargetNode.DataType is Schema.ITableType)
						{
							extractionTargetNode = EnsureTableNode(plan, extractionTargetNode);
							string[] columnNames = new string[columnExpression.Columns.Count];
							for (int columnIndex = 0; columnIndex < columnExpression.Columns.Count; columnIndex++)
								columnNames[columnIndex] = columnExpression.Columns[columnIndex].ColumnName;
							context = new OperatorBindingContext(callExpression, callExpression.Identifier, plan.NameResolutionPath, AggregateSignatureFromArguments(extractionTargetNode, columnNames, true), false);
							AggregateCallNode aggregateNode = EmitAggregateCallNode(plan, context, extractionTargetNode, columnNames, columnExpression.HasByClause ? columnExpression.OrderColumns : null);
							if (aggregateNode != null)
							{
								int stackDisplacement = aggregateNode.Operator.Initialization.StackDisplacement + aggregateNode.Operator.Operands.Count + 1; // add 1 to account for the result variable
								for (int stackIndex = 0; stackIndex < stackDisplacement; stackIndex++)
									plan.Symbols.Push(new Symbol(String.Empty, plan.DataTypes.SystemScalar));
								try
								{
									aggregateNode.Nodes[0] = EnsureTableNode(plan, CompileExpression(plan, (Expression)aggregateNode.Nodes[0].EmitStatement(EmitMode.ForCopy)));
								}
								finally
								{
									for (int stackIndex = 0; stackIndex < stackDisplacement; stackIndex++)
										plan.Symbols.Pop();
								}

								return aggregateNode;
							}
							else
								CheckOperatorResolution(plan, context);
						}
						
						planNodes[index] = EmitColumnExtractorNode(plan, ((ColumnExtractorExpression)callExpression.Expressions[index]), extractionTargetNode);
					}
					else
					{
						planNodes[index] = CompileExpression(plan, callExpression.Expressions[index]);
						if (planNodes[index].DataType is Schema.ITableType)
						{
							string[] columnNames = new string[]{};
							Schema.Signature callSignature = AggregateSignatureFromArguments(planNodes[index], columnNames, false);
							if (callSignature != null)
							{
								context = new OperatorBindingContext(callExpression, callExpression.Identifier, plan.NameResolutionPath, callSignature, true);
								AggregateCallNode aggregateNode = EmitAggregateCallNode(plan, context, planNodes[index], columnNames, null);
								if (aggregateNode != null)
								{
									int stackDisplacement = aggregateNode.Operator.Initialization.StackDisplacement + 1; // add 1 to account for the result variable
									for (int stackIndex = 0; stackIndex < stackDisplacement; stackIndex++)
										plan.Symbols.Push(new Symbol(String.Empty, plan.DataTypes.SystemScalar));
									try
									{
										aggregateNode.Nodes[0] = EnsureTableNode(plan, CompileExpression(plan, (Expression)aggregateNode.Nodes[0].EmitStatement(EmitMode.ForCopy)));
									}
									finally
									{
										for (int stackIndex = 0; stackIndex < stackDisplacement; stackIndex++)
											plan.Symbols.Pop();
									}
									return aggregateNode;
								}
							}
						}
					}
				}
				else
					planNodes[index] = CompileExpression(plan, callExpression.Expressions[index]);
			}
			
			context = new OperatorBindingContext(callExpression, callExpression.Identifier, plan.NameResolutionPath, SignatureFromArguments(planNodes), false);
			PlanNode node = EmitCallNode(plan, context, planNodes);
			CheckOperatorResolution(plan, context);
			return node;
		}

		public static ListNode EmitListNode(Plan plan, Schema.IListType listType, PlanNode[] elements)
		{
			ListNode node = new ListNode();
			node.DataType = listType;
			for (int index = 0; index < elements.Length; index++)
				node.Nodes.Add(elements[index]);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileListSelectorExpression(Plan plan, ListSelectorExpression expression)
		{
			Schema.IListType listType = null;
			if (expression.TypeSpecifier != null)
			{
				Schema.IDataType dataType = CompileTypeSpecifier(plan, expression.TypeSpecifier);
				if (!(dataType is Schema.IListType))
					throw new CompilerException(CompilerException.Codes.ListTypeExpected, expression.TypeSpecifier);
				listType = (Schema.IListType)dataType;
			}

			PlanNode[] planNodes = new PlanNode[expression.Expressions.Count];
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				PlanNode elementNode;
				if (listType != null)
					elementNode = CompileTypedExpression(plan, expression.Expressions[index], listType.ElementType);
				else
				{
					elementNode = CompileExpression(plan, expression.Expressions[index]);
					listType = new Schema.ListType(elementNode.DataType);
				}

				planNodes[index] = EnsureTableValueNode(plan, elementNode);
			}
			
			if (listType == null)
				listType = new Schema.ListType(new Schema.GenericType());

			return
				EmitListNode
				(
					plan, 
					listType,
					planNodes
				);
		}

		protected static void GeneratePermutations(int number, List<int[]> permutations)
		{
			int[] tempValue = new int[number];
			for (int index = 0; index < tempValue.Length; index++)
				tempValue[index] = 0;
			Visit(-1, 0, tempValue, permutations);
		}

		protected static void Visit(int level, int current, int[] tempValue, List<int[]> permutations)
		{
			level = level + 1;
			tempValue[current] = level;
			
			if (level == tempValue.Length)
			{
				int[] permutation = new int[tempValue.Length];
				for (int index = 0; index < tempValue.Length; index++)
					permutation[index] = tempValue[index];
				permutations.Add(permutation);
			}
			else
			{
				for (int index = 0; index < tempValue.Length; index++)
					if (tempValue[index] == 0)
						Visit(level, index, tempValue, permutations);
			}
			
			level = level - 1;
			tempValue[current] = 0;
		}

		public static PlanNode CompileIndexerExpression(Plan plan, D4IndexerExpression expression)
		{
			PlanNode targetNode = CompileExpression(plan, expression.Expression);
			
			if (targetNode.DataType is Schema.TableType)
			{
				TableNode tableNode = EnsureTableNode(plan, targetNode);
				Schema.Key resolvedKey = null;

				PlanNode[] terms = new PlanNode[expression.Expressions.Count];
				Schema.SignatureElement[] indexerSignature = new Schema.SignatureElement[expression.Expressions.Count];
				for (int index = 0; index < expression.Expressions.Count; index++)
				{
					terms[index] = CompileExpression(plan, expression.Expressions[index]);
					indexerSignature[index] = new Schema.SignatureElement(terms[index].DataType);
				}
				
				if (expression.HasByClause || (expression.Expressions.Count == 0))
				{
					resolvedKey = new Schema.Key();
					if (expression.HasByClause)
						foreach (KeyColumnDefinition column in expression.ByClause)
							resolvedKey.Columns.Add(tableNode.TableVar.Columns[column.ColumnName]);
					
					bool resolvedKeyUnique = false;
					foreach (Schema.Key key in tableNode.TableVar.Keys)
					{
						if (resolvedKey.Columns.IsSupersetOf(key.Columns))
						{
							resolvedKeyUnique = true;
							break;
						}
					}
					
					if (!resolvedKeyUnique && !plan.SuppressWarnings && !plan.InTypeOfContext)
						plan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression, CompilerErrorLevel.Warning, expression));
				}
				else
				{
					if (terms.Length > 0)
					{
						if (terms.Length > 5)
							throw new CompilerException(CompilerException.Codes.TooManyTermsForImplicitTableIndexer, expression);
							
						// construct operators for each potential key
						OperatorSignatures operatorSignatures = new OperatorSignatures(null);
						for (int index = 0; index < tableNode.TableVar.Keys.Count; index++)
							if (tableNode.TableVar.Keys[index].Columns.Count == terms.Length)
							{
								Schema.Operator operatorValue = new Schema.Operator(String.Format("{0}", index.ToString()));
								foreach (Schema.TableVarColumn column in tableNode.TableVar.Keys[index].Columns)
									operatorValue.Operands.Add(new Schema.Operand(operatorValue, column.Name, column.DataType));
								if (operatorSignatures.Contains(operatorValue.Signature))
									throw new CompilerException(CompilerException.Codes.PotentiallyAmbiguousImplicitTableIndexer, expression);
								operatorSignatures.Add(new OperatorSignature(operatorValue));
							}

						// If there is at least one potentially matching key						
						if (operatorSignatures.Count > 0)
						{
							// Compute permutations of the signature
							List<int[]> permutations = new List<int[]>();
							GeneratePermutations(terms.Length, permutations);

							List<Schema.Signature> signatures = new List<Schema.Signature>();
							for (int index = 0; index < permutations.Count; index++)
							{
								int[] permutation = permutations[index];
								Schema.SignatureElement[] permutationSignature = new Schema.SignatureElement[permutation.Length];
								for (int pIndex = 0; pIndex < permutation.Length; pIndex++)
									permutationSignature[pIndex] = indexerSignature[permutation[pIndex] - 1];
								signatures.Add(new Schema.Signature(permutationSignature));
							}
							
							// Resolve each permutation signature against the potential keys, recording partial and exact matches
							OperatorBindingContext[] contexts = new OperatorBindingContext[signatures.Count];
							for (int index = 0; index < signatures.Count; index++)
							{
								contexts[index] = new OperatorBindingContext(new CallExpression(), "Key", plan.NameResolutionPath, signatures[index], false);
								operatorSignatures.Resolve(plan, contexts[index]);
							}
							
							// Determine a matching signature and resolved key
							int bestNarrowingScore = Int32.MinValue;
							int shortestPathLength = Int32.MaxValue;
							int signatureIndex = -1;
							for (int index = 0; index < contexts.Length; index++)
							{
								if (contexts[index].Matches.IsMatch)
								{
									if (signatureIndex == -1)
									{
										signatureIndex = index;
										bestNarrowingScore = contexts[index].Matches.Match.NarrowingScore;
										shortestPathLength = contexts[index].Matches.Match.PathLength;
									}
									else
									{
										if (contexts[index].Matches.Match.NarrowingScore > bestNarrowingScore)
										{
											signatureIndex = index;
											bestNarrowingScore = contexts[index].Matches.Match.NarrowingScore;
											shortestPathLength = contexts[index].Matches.Match.PathLength;
										}
										else if (contexts[index].Matches.Match.NarrowingScore == bestNarrowingScore)
										{
											if (contexts[index].Matches.Match.PathLength < shortestPathLength)
											{
												signatureIndex = index;
												shortestPathLength = contexts[index].Matches.Match.PathLength;
											}
											else if (contexts[index].Matches.Match.PathLength == shortestPathLength)
												throw new CompilerException(CompilerException.Codes.AmbiguousTableIndexerKey, expression);
										}
									}
								}
							}
							
							if (signatureIndex >= 0)
							{
								int[] permutation = permutations[signatureIndex];
								
								Schema.Key signatureKey = tableNode.TableVar.Keys[Convert.ToInt32(contexts[signatureIndex].Matches.Match.Signature.Operator.OperatorName)];
								
								resolvedKey = new Schema.Key();
								expression.HasByClause = true;
								for (int index = 0; index < indexerSignature.Length; index++)
								{
									Schema.TableVarColumn resolvedKeyColumn = signatureKey.Columns[((IList)permutation).IndexOf(index + 1)];
									resolvedKey.Columns.Add(resolvedKeyColumn);
									expression.ByClause.Add(new KeyColumnDefinition(resolvedKeyColumn.Name));
								}
							}
						}
					}
					
					if (resolvedKey == null)
						throw new CompilerException(CompilerException.Codes.UnresolvedTableIndexerKey, expression);
				}
					
				if (resolvedKey.Columns.Count != expression.Expressions.Count)
					throw new CompilerException(CompilerException.Codes.InvalidTableIndexerKey, expression);
					
				expression.Expression = (Expression)tableNode.EmitStatement(EmitMode.ForCopy);
					
				Expression condition = null;
				
				for (int index = 0; index < resolvedKey.Columns.Count; index++)
				{
					expression.Expressions[index] = (Expression)terms[index].EmitStatement(EmitMode.ForCopy);
					
					BinaryExpression columnCondition = 
						new BinaryExpression
						(
							new IdentifierExpression(Schema.Object.EnsureRooted(Schema.Object.Qualify(resolvedKey.Columns[index].Name, "X"))), 
							Instructions.Equal, 
							expression.Expressions[index]
						);
					
					if (condition != null)
						condition = new BinaryExpression(condition, Instructions.And, columnCondition);
					else
						condition = columnCondition;
				}
				
				if (condition != null)
					tableNode = (TableNode)EmitRestrictNode(plan, EmitRenameNode(plan, tableNode, "X"), condition);

				ExtractRowNode node;
				bool saveSuppressWarnings = plan.SuppressWarnings;
				plan.SuppressWarnings = true;
				try
				{				
					node = (ExtractRowNode)EmitRowExtractorNode(plan, expression, tableNode);
				}
				finally
				{
					plan.SuppressWarnings = saveSuppressWarnings;
				}

				node.IndexerExpression = expression;

				if (condition != null)
				{
					RenameColumnExpressions renameColumns = new RenameColumnExpressions();
					foreach (Schema.Column column in ((Schema.IRowType)node.DataType).Columns)
						renameColumns.Add(new RenameColumnExpression(column.Name, Schema.Object.Dequalify(column.Name)));
					RowRenameNode rowRenameNode = (RowRenameNode)EmitRenameNode(plan, node, renameColumns);
					rowRenameNode.ShouldEmit = false;
					return rowRenameNode;
				}

				return node;
			}
			else
			{
				if (expression.Expressions.Count != 1)
					throw new CompilerException(CompilerException.Codes.InvalidIndexerExpression, expression);
					
				PlanNode[] arguments = new PlanNode[]{targetNode, CompileExpression(plan, expression.Indexer)};
				OperatorBindingContext context = new OperatorBindingContext(expression, Instructions.Indexer, plan.NameResolutionPath, SignatureFromArguments(arguments), false);
				PlanNode node = EmitCallNode(plan, context, arguments);
				CheckOperatorResolution(plan, context);
				return node;
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
		public static PlanNode EmitConditionNode(Plan plan, PlanNode ifNode, PlanNode trueNode, PlanNode falseNode)
		{
			ConditionNode node = new ConditionNode();
			node.Nodes.Add(ifNode);
			node.Nodes.Add(EnsureTableValueNode(plan, trueNode));
			node.Nodes.Add(EnsureTableValueNode(plan, falseNode));
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileIfExpression(Plan plan, IfExpression expression)
		{
			PlanNode ifNode = CompileBooleanExpression(plan, expression.Expression);
			PlanNode trueNode = CompileExpression(plan, expression.TrueExpression);
			PlanNode falseNode = CompileExpression(plan, expression.FalseExpression);
			return EmitConditionNode(plan, ifNode, trueNode, falseNode);
		}
		
		public static PlanNode CompileCaseItemExpression(Plan plan, CaseExpression expression, int index)
		{
			PlanNode whenNode = CompileExpression(plan, expression.CaseItems[index].WhenExpression);
			if (expression.Expression != null)
			{
				PlanNode compareNode = CompileExpression(plan, expression.Expression);
				whenNode = EmitBinaryNode(plan, expression, compareNode, Instructions.Equal, whenNode);
			}
				
			PlanNode thenNode = CompileExpression(plan, expression.CaseItems[index].ThenExpression);
			PlanNode elseNode;
			if (index >= expression.CaseItems.Count - 1)
				elseNode = CompileExpression(plan, ((CaseElseExpression)expression.ElseExpression).Expression);
			else
				elseNode = CompileCaseItemExpression(plan, expression, index + 1);
			return EmitConditionNode(plan, whenNode, thenNode, elseNode);
		}
		
		// Case expressions are converted into a series of equivalent if expressions.
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileCaseExpression(Plan plan, CaseExpression expression)
		{
			// case [<case expression>] when <when expression> then <then expression> ... else <else expression> end
			// convert the case expression into equivalent if then else expressions
			// if [<case expression> =] <when expression> then <then expression> else ... <else expression>
			
			//return CompileCaseItemExpression(APlan, AExpression, 0);

			CaseExpression localExpression = (CaseExpression)expression;
			if (localExpression.Expression != null)
			{
				SelectedConditionedCaseNode node = new SelectedConditionedCaseNode();
				
				PlanNode selectorNode = CompileExpression(plan, localExpression.Expression);
				PlanNode equalNode = null;
				Symbol selectorVar = new Symbol(String.Empty, selectorNode.DataType);
				node.Nodes.Add(selectorNode);

				foreach (CaseItemExpression caseItemExpression in localExpression.CaseItems)
				{
					ConditionedCaseItemNode caseItemNode = new ConditionedCaseItemNode();
					PlanNode whenNode = CompileTypedExpression(plan, caseItemExpression.WhenExpression, selectorNode.DataType);
					caseItemNode.Nodes.Add(whenNode);
					if (equalNode == null)
					{
						plan.Symbols.Push(selectorVar);
						try
						{
							plan.Symbols.Push(new Symbol(String.Empty, whenNode.DataType));
							try
							{
								equalNode = EmitBinaryNode(plan, new StackReferenceNode(selectorNode.DataType, 1, true), Instructions.Equal, new StackReferenceNode(whenNode.DataType, 0, true));
								node.Nodes.Add(equalNode);
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}

					caseItemNode.Nodes.Add(EnsureTableValueNode(plan, CompileExpression(plan, caseItemExpression.ThenExpression)));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
				
				if (localExpression.ElseExpression != null)
				{
					ConditionedCaseItemNode caseItemNode = new ConditionedCaseItemNode();
					caseItemNode.Nodes.Add(EnsureTableValueNode(plan, CompileExpression(plan, ((CaseElseExpression)localExpression.ElseExpression).Expression)));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
				
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return node;
			}
			else
			{
				ConditionedCaseNode node = new ConditionedCaseNode();
				
				foreach (CaseItemExpression caseItemExpression in localExpression.CaseItems)
				{
					ConditionedCaseItemNode caseItemNode = new ConditionedCaseItemNode();
					caseItemNode.Nodes.Add(CompileBooleanExpression(plan, caseItemExpression.WhenExpression));
					caseItemNode.Nodes.Add(EnsureTableValueNode(plan, CompileExpression(plan, caseItemExpression.ThenExpression)));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
				
				if (localExpression.ElseExpression != null)
				{
					ConditionedCaseItemNode caseItemNode = new ConditionedCaseItemNode();
					caseItemNode.Nodes.Add(EnsureTableValueNode(plan, CompileExpression(plan, ((CaseElseExpression)localExpression.ElseExpression).Expression)));
					caseItemNode.DetermineCharacteristics(plan);
					node.Nodes.Add(caseItemNode);
				}
				
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return node;
			}
		}
		
		public static PlanNode CompileValueExpression(Plan plan, ValueExpression expression)
		{
			ValueNode node = new ValueNode();
			switch (expression.Token)
			{
				#if NILISSCALAR
				case LexerToken.Nil: node.DataType = APlan.DataTypes.SystemScalar; node.IsNilable = true; break;
				#else
				case TokenType.Nil: node.DataType = plan.DataTypes.SystemNilGeneric; node.IsNilable = true; break;
				#endif
				case TokenType.Boolean: node.DataType = plan.DataTypes.SystemBoolean; node.Value = (bool)expression.Value; break;
				case TokenType.Integer: 
				case TokenType.Hex:
					if ((Convert.ToInt64(expression.Value) > Int32.MaxValue) || (Convert.ToInt64(expression.Value) < Int32.MinValue))
					{
						node.DataType = plan.DataTypes.SystemLong;
						node.Value = Convert.ToInt64(expression.Value);
					}
					else
					{
						node.DataType = plan.DataTypes.SystemInteger;
						node.Value = Convert.ToInt32(expression.Value);
					}
				break;
				case TokenType.Decimal: node.DataType = plan.DataTypes.SystemDecimal; node.Value = (decimal)expression.Value; break;
				case TokenType.Money: node.DataType = plan.DataTypes.SystemMoney; node.Value = (decimal)expression.Value; break;
				#if USEDOUBLES
				case LexerToken.Float: node.DataType = APlan.DataTypes.SystemDouble; node.Value = Scalar.FromDouble((double)AExpression.Value); break;
				#endif
				case TokenType.String: node.DataType = plan.DataTypes.SystemString; node.Value = (string)expression.Value; break;
				#if USEISTRING
				case LexerToken.IString: node.DataType = APlan.DataTypes.SystemIString; node.Value = (string)AExpression.Value; break;
				#endif
				default: throw new CompilerException(CompilerException.Codes.UnknownLiteralType, expression, Enum.GetName(typeof(TokenType), expression.Token));
			}

			// ValueNodes assume the device of their peers

			return node;
		}
		
		public static PlanNode EmitParameterNode(Plan plan, Modifier modifier, PlanNode planNode)
		{
			ParameterNode node = new ParameterNode();
			node.Modifier = modifier;
			node.Nodes.Add(planNode);
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileParameterExpression(Plan plan, ParameterExpression parameter)
		{
			return EmitParameterNode(plan, parameter.Modifier, CompileExpression(plan, parameter.Expression));
		}
		
		public static PlanNode CompileIdentifierExpression(Plan plan, IdentifierExpression expression)
		{
			NameBindingContext context = new NameBindingContext(expression.Identifier, plan.NameResolutionPath);
			PlanNode node = EmitIdentifierNode(plan, expression, context);
			if (node == null)
				if (context.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, expression, expression.Identifier, ExceptionUtility.StringsToCommaList(context.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, expression, expression.Identifier);
			return node;
		}
		
		public static PlanNode EmitStackColumnReferenceNode(Plan plan, string identifier, int location)
		{
			Schema.IRowType rowType = (Schema.IRowType)plan.Symbols[location].DataType;
			int columnIndex = rowType.Columns.IndexOf(identifier);
			#if USECOLUMNLOCATIONBINDING
			return 
				new StackColumnReferenceNode
				(
					rowType.Columns[columnIndex].Name,
					rowType.Columns[columnIndex].DataType, 
					ALocation, 
					columnIndex
				);
			#else
			return 
				new StackColumnReferenceNode
				(
					rowType.Columns[columnIndex].Name,
					rowType.Columns[columnIndex].DataType, 
					location
				);
			#endif
		}

		// TableSelectorNode 
		//		Nodes[0..RowCount - 1] = PlanNodes for each row in the table selector expression
		// 
		// This operator cannot be overloaded.
		public static PlanNode CompileTableSelectorExpression(Plan plan, TableSelectorExpression expression)
		{
			Schema.ITableType tableType;
			Schema.IRowType rowType = null;
			if (expression.TypeSpecifier != null)
			{
				Schema.IDataType dataType = CompileTypeSpecifier(plan, expression.TypeSpecifier);
				if (!(dataType is Schema.ITableType))
					throw new CompilerException(CompilerException.Codes.TableTypeExpected, expression.TypeSpecifier);
				
				tableType = (Schema.ITableType)dataType;
				rowType = (Schema.IRowType)tableType.RowType;
			}
			else
				tableType = new Schema.TableType();

			TableSelectorNode node = new TableSelectorNode(tableType);

			foreach (Schema.Column column in node.DataType.Columns)
				node.TableVar.Columns.Add(new Schema.TableVarColumn(column));

			foreach (Expression localExpression in expression.Expressions)
			{
				if (rowType == null)
				{
					PlanNode rowNode = CompileExpression(plan, localExpression);
					if (!(rowNode.DataType is Schema.IRowType))
						throw new CompilerException(CompilerException.Codes.RowExpressionExpected, localExpression);
					node.Nodes.Add(rowNode);

					rowType = (Schema.IRowType)rowNode.DataType;
					Schema.TableVarColumn tableVarColumn;
					foreach (Schema.Column column in rowType.Columns)
					{
						tableVarColumn = new Schema.TableVarColumn(column.Copy());
						node.DataType.Columns.Add(tableVarColumn.Column);
						node.TableVar.Columns.Add(tableVarColumn);
					}
				}
				else
				{
					PlanNode rowNode;
					if (localExpression is RowSelectorExpression)
						rowNode = CompileTypedRowSelectorExpression(plan, (RowSelectorExpression)localExpression, rowType);
					else
						rowNode = CompileExpression(plan, localExpression);
						
					if (!rowNode.DataType.Is(rowType))
						throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, localExpression, rowType.ToString());
					node.Nodes.Add(rowNode);
				}
			}

			node.TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			CompileTableVarKeys(plan, node.TableVar, expression.Keys);
			Schema.Key clusteringKey = FindClusteringKey(plan, node.TableVar);

			int messageIndex = plan.Messages.Count;			
			try
			{
				if (clusteringKey.Columns.Count > 0)
					node.Order = OrderFromKey(plan, clusteringKey);
			}
			catch (Exception exception)
			{
				if ((exception is CompilerException) && (((CompilerException)exception).Code == (int)CompilerException.Codes.NonFatalErrors))
				{
					plan.Messages.Insert(messageIndex, new CompilerException(CompilerException.Codes.InvalidTableSelector, expression));
					throw exception;
				}
				else
					throw new CompilerException(CompilerException.Codes.InvalidTableSelector, expression, exception);
			}
			
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitRowExtractorNode(Plan plan, Statement statement, PlanNode planNode)
		{
			ExtractRowNode node = new ExtractRowNode();
			if (statement != null)
				node.Modifiers = statement.Modifiers;
			node.Nodes.Add(EnsureTableNode(plan, planNode));
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitRowExtractorNode(Plan plan, Statement statement, Expression expression)
		{
			PlanNode planNode = CompileExpression(plan, expression);
			if (!(planNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.UnableToExtractRow, expression, planNode.DataType.ToString());

			return EmitRowExtractorNode(plan, statement, planNode);
		}
		
		// ExtractRowNode
		//		Nodes[0] = SourceNode for the extraction
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileRowExtractorExpression(Plan plan, RowExtractorExpression expression)
		{
			if (!plan.SuppressWarnings)
				plan.Messages.Add(new CompilerException(CompilerException.Codes.RowExtractorDeprecated, CompilerErrorLevel.Warning, expression));
			return EmitRowExtractorNode(plan, expression, expression.Expression);
		}
		
		public static PlanNode EmitColumnExtractorNode(Plan plan, Statement statement, string columnName, PlanNode targetNode)
		{
			return EmitColumnExtractorNode(plan, statement, new ColumnExpression(columnName), targetNode);
		}
		
		public static PlanNode EmitColumnExtractorNode(Plan plan, Statement statement, ColumnExpression columnExpression, PlanNode targetNode)
		{
			ExtractColumnNode node = new ExtractColumnNode();
			if (targetNode.DataType is Schema.IRowType)
			{
				node.Identifier = columnExpression.ColumnName;
				#if USECOLUMNLOCATIONBINDING
				node.Location = ((Schema.IRowType)ATargetNode.DataType).Columns.IndexOf(AColumnName);
				if (node.Location < 0)
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AExpression, AColumnName);
				#else
				NameBindingContext context = new NameBindingContext(columnExpression.ColumnName, plan.NameResolutionPath);
				int columnIndex = ((Schema.IRowType)targetNode.DataType).Columns.IndexOf(columnExpression.ColumnName, context.Names);
				if (columnIndex < 0)
					if (context.IsAmbiguous)
						throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, columnExpression, columnExpression.ColumnName, ExceptionUtility.StringsToCommaList(context.Names));
					else
						throw new CompilerException(CompilerException.Codes.UnknownIdentifier, columnExpression, columnExpression.ColumnName);
				#endif
			}
			else
				throw new CompilerException(CompilerException.Codes.InvalidExtractionTarget, columnExpression, columnExpression.ColumnName, targetNode.DataType.Name);

			node.Nodes.Add(targetNode);
			if (statement != null)
				node.Modifiers = statement.Modifiers;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitColumnExtractorNode(Plan plan, ColumnExtractorExpression expression, PlanNode targetNode)
		{
			if (expression.Columns.Count != 1)
				throw new CompilerException(CompilerException.Codes.InvalidColumnExtractorExpression, expression);
				
			return EmitColumnExtractorNode(plan, expression, expression.Columns[0], targetNode);
		}
		
		// ExtractColumnNode
		//		Nodes[0] = SourceNode for the extraction
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileColumnExtractorExpression(Plan plan, ColumnExtractorExpression expression)
		{
			if (!plan.SuppressWarnings)
				plan.Messages.Add(new CompilerException(CompilerException.Codes.ColumnExtractorDeprecated, CompilerErrorLevel.Warning, expression));
			return EmitColumnExtractorNode(plan, expression, CompileExpression(plan, expression.Expression));
		}
		
		public static PlanNode EmitCursorNode(Plan plan, Statement statement, PlanNode planNode, CursorContext cursorContext)
		{
			if (!(planNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, statement);
				
			CursorNode node = new CursorNode();
			node.CursorContext = cursorContext;
			node.Nodes.Add(EnsureTableNode(plan, planNode));
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			
			return node;
		}
		
		public static PlanNode CompileCursor(Plan plan, Expression expression)
		{
			if (!(expression is CursorDefinition))
			{
				CursorContext context = plan.GetDefaultCursorContext();
				CursorDefinition cursorDefinition = new CursorDefinition(expression, context.CursorCapabilities, context.CursorIsolation, context.CursorType);
				cursorDefinition.Line = expression.Line;
				cursorDefinition.LinePos = expression.LinePos;
				expression = cursorDefinition;
			}
			
			PlanNode node = CompileExpression(plan, expression);
			
			if (node.DataType == null)
				throw new CompilerException(CompilerException.Codes.ExpressionExpected, expression);
				
			if (node is CursorNode)
			{
				Schema.TableVar tableVar = ((TableNode)node.Nodes[0]).TableVar;

				// This is wrong for at least two reasons
					// 1. It doesn't need to be here because the CopyDataType code in the process will manage communicating proposable remotable information
					// 2. Even if it did need to be passed over the wire, it's wrong to add these tags to the base table variable definition, just because it was the target of a select
				//foreach (Schema.TableVarColumn column in tableVar.Columns)
				//{
				//	if (column.MetaData == null)
				//		column.MetaData = new MetaData();
				//	column.MetaData.Tags.AddOrUpdate("DAE.IsDefaultRemotable", column.IsDefaultRemotable.ToString());
				//	column.MetaData.Tags.AddOrUpdate("DAE.IsChangeRemotable", column.IsChangeRemotable.ToString());
				//	column.MetaData.Tags.AddOrUpdate("DAE.IsValidateRemotable", column.IsValidateRemotable.ToString());
				//}
			}
			else if (node.DataType is Schema.ITableType)
			{	
				node = EnsureTableNode(plan, node);
			}

			return node;
		}
		
		public static PlanNode CompileCursorDefinition(Plan plan, CursorDefinition expression)
		{
			CursorContext cursorContext = new CursorContext(expression.CursorType, expression.Capabilities, expression.Isolation);
			plan.PushCursorContext(cursorContext);
			try
			{
				PlanNode node = CompileExpression(plan, expression.Expression);
				if (node.DataType is Schema.ITableType)
					return EmitCursorNode(plan, expression, node, cursorContext);
				else
					return node;
			}
			finally
			{
				plan.PopCursorContext();
			}
		}

		public static PlanNode CompileCursorSelectorExpression(Plan plan, CursorSelectorExpression expression)
		{
			PlanNode node = CompileCursorDefinition(plan, expression.CursorDefinition);
			if (!(node is CursorNode))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression);
			return node;
		}
		
		// RowSelectorNode
		//		Nodes[0..ColumnCount - 1] = PlanNodes for the columns in the row selector
		//
		// This operator cannot be overloaded.
		public static PlanNode CompileRowSelectorExpression(Plan plan, RowSelectorExpression expression)
		{
			Schema.IRowType rowType = null;
			if (expression.TypeSpecifier != null)
			{
				Schema.IDataType dataType = CompileTypeSpecifier(plan, expression.TypeSpecifier);
				if (!(dataType is Schema.IRowType))
					throw new CompilerException(CompilerException.Codes.RowTypeExpected, expression.TypeSpecifier);
				
				rowType = (Schema.IRowType)dataType;
			}
			else
				rowType = new Schema.RowType();

			PlanNode planNode;
			RowSelectorNode node = new RowSelectorNode(rowType);
			
			if (expression.TypeSpecifier != null)
			{
				node.SpecifiedRowType = new Schema.RowType();

				foreach (NamedColumnExpression localExpression in expression.Expressions)
				{
					planNode = CompileExpression(plan, localExpression.Expression);
					if (localExpression.ColumnAlias == String.Empty)
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, localExpression);
						
					Schema.Column targetColumn = node.DataType.Columns[localExpression.ColumnAlias];
						
					node.SpecifiedRowType.Columns.Add
					(
						new Schema.Column
						(
							targetColumn.Name,
							targetColumn.DataType
						)
					);
					
					if (!planNode.DataType.Is(targetColumn.DataType))
					{
						ConversionContext context = FindConversionPath(plan, planNode.DataType, targetColumn.DataType);
						CheckConversionContext(plan, context);
						planNode = ConvertNode(plan, planNode, context);
					}

					planNode = EnsureTableValueNode(plan, planNode);
					planNode = EnsureTableValueNode(plan, planNode);
					node.Nodes.Add(planNode);
				}
			}
			else
			{
				foreach (NamedColumnExpression localExpression in expression.Expressions)
				{
					planNode = CompileExpression(plan, localExpression.Expression);
					if (localExpression.ColumnAlias == String.Empty)
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, localExpression);

					node.DataType.Columns.Add
					(
						new Schema.Column
						(
							localExpression.ColumnAlias, 
							planNode.DataType
						)
					);

					planNode = EnsureTableValueNode(plan, planNode);
					node.Nodes.Add(planNode);
				}
			}
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileTypedRowSelectorExpression
		(
			Plan plan, 
			RowSelectorExpression expression, 
			Schema.IRowType rowType
		)
		{
			if (expression.TypeSpecifier == null)
			{
				RowSelectorNode node = new RowSelectorNode(new Schema.RowType());
				PlanNode planNode;
				for (int index = 0; index < expression.Expressions.Count; index++)
				{
					if (expression.Expressions[index].ColumnAlias == String.Empty)
					{
						if (index >= rowType.Columns.Count)
							throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, expression.Expressions[index], rowType.ToString());
						planNode = CompileTypedExpression(plan, expression.Expressions[index].Expression, rowType.Columns[index].DataType);

						node.DataType.Columns.Add
						(
							new Schema.Column
							(
								rowType.Columns[index].Name, 
								rowType.Columns[index].DataType
							)
						);
					}
					else
					{
						if (!rowType.Columns.ContainsName(expression.Expressions[index].ColumnAlias))
							throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, expression.Expressions[index], rowType.ToString());
						Schema.IDataType targetType = rowType.Columns[rowType.Columns.IndexOfName(expression.Expressions[index].ColumnAlias)].DataType;
						planNode = CompileTypedExpression(plan, expression.Expressions[index].Expression, targetType);

						node.DataType.Columns.Add
						(
							new Schema.Column
							(
								expression.Expressions[index].ColumnAlias, 
								targetType
							)
						);
					}

					planNode = EnsureTableValueNode(plan, planNode);
					node.Nodes.Add(planNode);
				}
				node.DetermineCharacteristics(plan);
				return node;
			}
			else
			{
				PlanNode node = CompileRowSelectorExpression(plan, expression);
				
				if (!node.DataType.Is(rowType))
				{
					ConversionContext context = FindConversionPath(plan, node.DataType, rowType);
					try
					{
						CheckConversionContext(plan, context);
					}
					catch (CompilerException E)
					{
						throw new CompilerException(CompilerException.Codes.InvalidRowInTableSelector, expression, E, rowType.ToString());
					}
					node = ConvertNode(plan, node, context);
				}
				
				return node;
			}
		}

		public static PlanNode EmitBaseTableVarNode(Plan plan, Schema.TableVar tableVar)
		{
			return EmitBaseTableVarNode(plan, tableVar.Name, (Schema.BaseTableVar)tableVar);
		}

		public static PlanNode EmitBaseTableVarNode(Plan plan, string identifier, Schema.BaseTableVar tableVar)
		{
			return EmitBaseTableVarNode(plan, new EmptyStatement(), identifier, tableVar);
		}
		
		public static PlanNode EmitBaseTableVarNode(Plan plan, Statement statement, string identifier, Schema.BaseTableVar tableVar)
		{
			BaseTableVarNode node = (BaseTableVarNode)FindCallNode(plan, statement, Instructions.Retrieve, new PlanNode[]{});
			node.TableVar = tableVar;
			if ((tableVar.SourceTableName != null) && (Schema.Object.NamesEqual(tableVar.Name, identifier)))
				node.ExplicitBind = true;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitDerivedTableVarNode(Plan plan, Schema.DerivedTableVar derivedTableVar)
		{
			return EmitDerivedTableVarNode(plan, new EmptyStatement(), derivedTableVar);
		}
		
		public static PlanNode EmitDerivedTableVarNode(Plan plan, Statement statement, Schema.DerivedTableVar derivedTableVar)
		{
			DerivedTableVarNode node = new DerivedTableVarNode(derivedTableVar);
			node.Operator = ResolveOperator(plan, Instructions.Retrieve, new Schema.Signature(new Schema.SignatureElement[]{}), false);
			node.Modifiers = statement.Modifiers;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		// Emits a restrict node for the given source and condition		
		//
		// RestrictNode
		//		Nodes[0] = ASourceNode
		//		Nodes[1] = AConditionNode
		public static PlanNode EmitRestrictNode(Plan plan, RestrictExpression expression, PlanNode sourceNode, PlanNode conditionNode)
		{
			return EmitCallNode(plan, expression, Instructions.Restrict, new PlanNode[]{sourceNode, conditionNode});
		}
		
		public static PlanNode EmitRestrictNode(Plan plan, PlanNode sourceNode, PlanNode conditionNode)
		{
			return EmitRestrictNode(plan, null, sourceNode, conditionNode);
		}
		
		public static PlanNode EmitRestrictNode(Plan plan, PlanNode sourceNode, Expression expression)
		{
			return EmitRestrictNode(plan, null, sourceNode, expression);
		}
		
		public static PlanNode EmitRestrictNode(Plan plan, RestrictExpression restrictExpression, PlanNode sourceNode, Expression expression)
		{
			if (!(sourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, plan.CurrentStatement());

			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, ((Schema.ITableType)sourceNode.DataType).RowType));
				try
				{
					PlanNode conditionNode = CompileExpression(plan, expression);
					return EmitRestrictNode(plan, restrictExpression, sourceNode, conditionNode);
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		public static PlanNode EmitInsertConditionNode(Plan plan, PlanNode sourceNode)
		{
			RestrictNode node = (RestrictNode)EmitRestrictNode(plan, EnsureTableNode(plan, sourceNode), CompileExpression(plan, new ValueExpression(false, TokenType.Boolean)));
			node.EnforcePredicate = false;
			node.ShouldEmit = false;
			return node;
		}
		
		public static PlanNode EmitUpdateConditionNode(Plan plan, PlanNode sourceNode, PlanNode conditionNode)
		{
			RestrictNode node = (RestrictNode)EmitRestrictNode(plan, sourceNode, conditionNode);
			node.EnforcePredicate = false;
			return node;
		}
		
		public static PlanNode EmitUpdateConditionNode(Plan plan, PlanNode sourceNode, Expression expression)
		{
			RestrictNode node = (RestrictNode)EmitRestrictNode(plan, sourceNode, expression);
			node.EnforcePredicate = false;
			return node;
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
		public static PlanNode CompileRestrictExpression(Plan plan, RestrictExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
				if (!(sourceNode.DataType is Schema.ITableType))
					throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitRestrictNode(plan, expression, sourceNode, expression.Condition);
		}
		
		public static ApplicationTransaction PrepareShouldTranslate(Plan plan, Statement statement)
		{
			return PrepareShouldTranslate(plan, statement, String.Empty);
		}
		
		public static ApplicationTransaction PrepareShouldTranslate(Plan plan, Statement statement, string qualifier)
		{
			ApplicationTransaction transaction = null;
			if ((plan.ApplicationTransactionID != Guid.Empty) && !Convert.ToBoolean(LanguageModifiers.GetModifier(statement.Modifiers, Schema.Object.Qualify("ShouldTranslate", qualifier), "true")))
				transaction = plan.GetApplicationTransaction();
			try
			{
				if (transaction != null)
					transaction.PushLookup();
			}
			catch
			{
				if (transaction != null)
					Monitor.Exit(transaction);
				throw;
			}
			
			return transaction;
		}
		
		public static void UnprepareShouldTranslate(Plan plan, ApplicationTransaction transaction)
		{
			if (transaction != null)
			{
				try
				{
					transaction.PopLookup();
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
		}
		
		public static T TagLine<T>(T statement, LineInfo lineInfo) where T : Statement
		{
			statement.SetLineInfo(lineInfo);
			return statement;
		}
		
		public static T TagLine<T>(Plan plan, T statement) where T : Statement
		{
			return TagLine<T>(statement, plan.GetCurrentLineInfo());
		}
		
		public static Expression BuildRowEqualExpression(Plan plan, Schema.Columns leftRow, Schema.Columns rightRow)
		{
			return BuildRowEqualExpression(plan, leftRow, rightRow, null, null);
		}
		
		// Builds an expression suitable for comparing a row with the columns given in ALeftRow to a row with the columns given in ARightRow
		// All the columns in ALeftRow are expected to be prefixed with the keyword 'left'.
		// All the columns in ARightRow are expected to be prefixed with the keyword 'right'.
		// The resulting expression is order-agnostic (i.e. the columns in ARightRow need not be in the same order as the columns in ARightRow
		// for each column ->
		//  [IsNil(left.<column name>) and IsNil(right.<column name>) or] left.<column name> = right.<column name>
		public static Expression BuildRowEqualExpression(Plan plan, Schema.Columns leftRow, Schema.Columns rightRow, BitArray leftIsNilable, BitArray rightIsNilable)
		{
			Expression expression = null;
			Expression equalExpression;
			LineInfo lineInfo = plan.GetCurrentLineInfo();

			for (int index = 0; index < leftRow.Count; index++)
			{
				int rightIndex = rightRow.IndexOf(Schema.Object.Dequalify(leftRow[index].Name));

				equalExpression =
					TagLine<Expression>
					(
						new BinaryExpression
						(
							#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
							TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftRow[index].Name)), lineInfo),
							Instructions.Equal,
							TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightRow[rightIndex].Name)), lineInfo)
							#else
							TagLine<Expression>(new IdentifierExpression(leftRow[index].Name), lineInfo),
							Instructions.Equal,
							TagLine<Expression>(new IdentifierExpression(rightRow[rightIndex].Name), lineInfo)
							#endif
						),
						lineInfo
					);

				if (((leftIsNilable == null) || leftIsNilable[index]) || ((rightIsNilable == null) || rightIsNilable[rightIndex]))
					equalExpression =
						TagLine<Expression>
						(
							new BinaryExpression
							(
								TagLine<Expression>
								(
									new BinaryExpression
									(
										#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
										TagLine<Expression>(new CallExpression(CIsNilOperatorName, new Expression[]{ TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftRow[index].Name)), lineInfo) }), lineInfo), 
										Instructions.And, 
										TagLine<Expression>(new CallExpression(CIsNilOperatorName, new Expression[]{ TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightRow[rightIndex].Name)), lineInfo) }), lineInfo)
										#else
										TagLine<Expression>(new CallExpression(IsNilOperatorName, new Expression[]{ TagLine<Expression>(new IdentifierExpression(leftRow[index].Name), lineInfo) }), lineInfo),
										Instructions.And, 
										TagLine<Expression>(new CallExpression(IsNilOperatorName, new Expression[]{ TagLine<Expression>(new IdentifierExpression(rightRow[rightIndex].Name), lineInfo) }), lineInfo)
										#endif
									),
									lineInfo
								),
								Instructions.Or,
								equalExpression
							),
							lineInfo
						);
					
				if (expression == null)
					expression = equalExpression;
				else
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, equalExpression), lineInfo);
			}

			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);
				
			return expression;
		}

		public static Expression BuildRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.TableVarColumnsBase columns)
		{
			return BuildRowEqualExpression(plan, leftRowVarName, rightRowVarName, columns, (BitArray)null);
		}
		
		public static Expression BuildRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.TableVarColumnsBase columns, BitArray isNilable)
		{
			return BuildRowEqualExpression(plan, leftRowVarName, rightRowVarName, columns, columns, isNilable, isNilable);
		}
		
		public static Expression BuildRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.TableVarColumnsBase leftColumns, Schema.TableVarColumnsBase rightColumns)
		{
			return BuildRowEqualExpression(plan, leftRowVarName, rightRowVarName, leftColumns, rightColumns, null, null);
		}
		
		public static Expression BuildRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.TableVarColumnsBase leftColumns, Schema.TableVarColumnsBase rightColumns, BitArray leftIsNilable, BitArray rightIsNilable)
		{
			if ((leftRowVarName == null) || (rightRowVarName == null) || ((leftRowVarName == String.Empty) && (rightRowVarName == String.Empty)))
				throw new ArgumentException("Row variable name is required for at least one side of the row comparison expression to be built.");

			Expression expression = null;
			Expression equalExpression;
			LineInfo lineInfo = plan.GetCurrentLineInfo();

			for (int index = 0; index < leftColumns.Count; index++)
			{
				int rightIndex = Object.ReferenceEquals(leftColumns, rightColumns) ? index : rightColumns.IndexOf(leftColumns[index].Name);

				equalExpression =
					TagLine<Expression>
					(
						new BinaryExpression
						(
							leftRowVarName == String.Empty
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[index].Name)), lineInfo)
								#else
								? TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)
								#endif
								: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(leftRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)), lineInfo),
							Instructions.Equal,
							rightRowVarName == String.Empty
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[rightIndex].Name)), lineInfo)
								#else
								? TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)
								#endif
								: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(rightRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)), lineInfo)
						),
						lineInfo
					);
					
				if ((leftIsNilable == null) || leftIsNilable[index])
					equalExpression =
						TagLine<Expression>
						(
							new BinaryExpression
							(
								TagLine<Expression>
								(
									new BinaryExpression
									(
										TagLine<Expression>
										(
											new CallExpression
											(
												IsNilOperatorName,
												new Expression[]
												{
													leftRowVarName == String.Empty
														#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
														? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[index].Name)), lineInfo)
														#else
														? TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)
														#endif
														: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(leftRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)), lineInfo)
												}
											),
											lineInfo
										),
										Instructions.And,
										TagLine<Expression>
										(
											new CallExpression
											(
												IsNilOperatorName,
												new Expression[]
												{
													rightRowVarName == String.Empty
														#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
														? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[rightIndex].Name)), lineInfo)
														#else
														? TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)
														#endif
														: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(rightRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)), lineInfo)
												}
											),
											lineInfo
										)
									),
									lineInfo
								),
								Instructions.Or,
								equalExpression
							),
							lineInfo
						);
					
				if (expression != null)
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, equalExpression), lineInfo);
				else
					expression = equalExpression;
			}
			
			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);
			
			return expression;
		}
		
		public static Expression BuildOptimisticRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.Columns columns)
		{
			Schema.Columns localColumns = new Schema.Columns();
			foreach (Schema.Column column in columns)
			{
				Schema.Signature signature = new Schema.Signature(new Schema.SignatureElement[] { new Schema.SignatureElement(column.DataType), new Schema.SignatureElement(column.DataType) });
				OperatorBindingContext context = new OperatorBindingContext(null, "iEqual", plan.NameResolutionPath, signature, true);
				Compiler.ResolveOperator(plan, context);
				if (context.Operator != null)
					localColumns.Add(column);
			}
			
			return BuildRowEqualExpression(plan, leftRowVarName, rightRowVarName, localColumns);
		}
		
		public static Expression BuildRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.Columns columns)
		{
			return BuildRowEqualExpression(plan, leftRowVarName, rightRowVarName, columns, columns);
		}
		
		public static Expression BuildRowEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.Columns leftColumns, Schema.Columns rightColumns)
		{
			if ((leftRowVarName == null) || (rightRowVarName == null) || ((leftRowVarName == String.Empty) && (rightRowVarName == String.Empty)))
				throw new ArgumentException("Row variable name is required for at least one side of the row comparison expression to be built.");

			Expression expression = null;
			var lineInfo = plan.GetCurrentLineInfo();
			for (int index = 0; index < leftColumns.Count; index++)
			{
				int rightIndex = Object.ReferenceEquals(leftColumns, rightColumns) ? index : rightColumns.IndexOf(leftColumns[index].Name);

				Expression equalExpression =
					TagLine<Expression>
					(
						new BinaryExpression
						(
							TagLine<Expression>
							(
								new BinaryExpression
								(
									TagLine<Expression>
									(
										new CallExpression
										(
											IsNilOperatorName,
											new Expression[]
											{
												leftRowVarName == String.Empty
													#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
													? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[index].Name)), lineInfo)
													#else
													? TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)
													#endif
													: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(leftRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)), lineInfo)
											}
										),
										lineInfo
									),
									Instructions.And,
									TagLine<Expression>
									(
										new CallExpression
										(
											IsNilOperatorName,
											new Expression[]
											{
												rightRowVarName == String.Empty
													#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
													? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[rightIndex].Name)), lineInfo)
													#else
													? TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)
													#endif
													: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(rightRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)), lineInfo)
											}
										),
										lineInfo
									)
								),
								lineInfo
							),
							Instructions.Or,
							TagLine<Expression>
							(
								new BinaryExpression
								(
									leftRowVarName == String.Empty
										#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
										? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[index].Name)), lineInfo)
										#else
										? TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)
										#endif
										: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(leftRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)), lineInfo),
									Instructions.Equal,
									rightRowVarName == String.Empty
										#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
										? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[rightIndex].Name)), lineInfo)
										#else
										? TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)
										#endif
										: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(rightRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(rightColumns[rightIndex].Name), lineInfo)), lineInfo)
								),
								lineInfo
							)
						),
						lineInfo
					);
					
				if (expression != null)
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, equalExpression), lineInfo);
				else
					expression = equalExpression;
			}
			
			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);
			
			return expression;
		}
		
		public static Expression BuildKeyEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.TableVarColumnsBase leftColumns, Schema.TableVarColumnsBase rightColumns)
		{
			Expression expression = null;
			var lineInfo = plan.GetCurrentLineInfo();
			for (int index = 0; index < leftColumns.Count; index++)
			{
				Expression equalExpression = 
					TagLine<Expression>
					(
						new BinaryExpression
						(
							leftRowVarName == String.Empty
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[index].Name)), lineInfo)
								#else
								? TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)
								#endif
								: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(leftRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)), lineInfo),
							Instructions.Equal, 
							rightRowVarName == String.Empty
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								? TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[index].Name)), lineInfo)
								#else
								? TagLine<Expression>(new IdentifierExpression(rightColumns[index].Name), lineInfo)
								#endif
								: TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(rightRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(rightColumns[index].Name), lineInfo)), lineInfo)
						),
						lineInfo
					);
					
				if (expression != null)
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, equalExpression), lineInfo);
				else
					expression = equalExpression;
			}
			
			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);
				
			return expression;
		}

		public static Expression BuildKeyEqualExpression(Plan plan, string leftRowVarName, string rightRowVarName, Schema.Columns leftColumns, Schema.Columns rightColumns)
		{
			Expression expression = null;
			var lineInfo = plan.GetCurrentLineInfo();
			for (int index = 0; index < leftColumns.Count; index++)
			{
				Expression equalExpression = 
					TagLine<Expression>
					(
						new BinaryExpression
						(
							leftRowVarName == String.Empty ? 
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftColumns[index].Name)), lineInfo) :
								#else
								TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo) :
								#endif
								TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(leftRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(leftColumns[index].Name), lineInfo)), lineInfo),
							Instructions.Equal, 
							rightRowVarName == String.Empty ?
								#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
								TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightColumns[LIndex].Name)), LLineInfo) :
								#else
								TagLine<Expression>(new IdentifierExpression(rightColumns[index].Name), lineInfo) :
								#endif
								TagLine<Expression>(new QualifierExpression(TagLine<Expression>(new IdentifierExpression(rightRowVarName), lineInfo), TagLine<Expression>(new IdentifierExpression(rightColumns[index].Name), lineInfo)), lineInfo)
						),
						lineInfo
					);
					
				if (expression != null)
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, equalExpression), lineInfo);
				else
					expression = equalExpression;
			}
			
			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);
				
			return expression;
		}

		public static Expression BuildKeyEqualExpression(Plan plan, Schema.Columns leftKey, Schema.Columns rightKey)
		{
			Expression expression = null;
			Expression equalExpression;
			var lineInfo = plan.GetCurrentLineInfo();
			for (int index = 0; index < leftKey.Count; index++)
			{
				Error.AssertWarn(String.Compare(leftKey[index].Name, rightKey[index].Name) != 0, "Key column names equal. Invalid key comparison expression.");
				
				#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
				equalExpression = TagLine<Expression>(new BinaryExpression(TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ALeftKey[index].Name)), lineInfo), Instructions.Equal, TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(ARightKey[index].Name)), lineInfo)), lineInfo);
				#else
				equalExpression = TagLine<Expression>(new BinaryExpression(TagLine<Expression>(new IdentifierExpression(leftKey[index].Name), lineInfo), Instructions.Equal, TagLine<Expression>(new IdentifierExpression(rightKey[index].Name), lineInfo)), lineInfo);
				#endif
				
				if (expression == null)
					expression = equalExpression;
				else
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, equalExpression), lineInfo);
			}
			
			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);

			return expression;
		}
		
		public static Expression BuildKeyIsNotNilExpression(Plan plan, Schema.KeyColumns key)
		{
			Expression expression = null;
			Expression isNotNilExpression;
			var lineInfo = plan.GetCurrentLineInfo();
			for (int index = 0; index < key.Count; index++)
			{
				isNotNilExpression = 
					TagLine<Expression>
					(
						new UnaryExpression
						(
							Instructions.Not,
							TagLine<Expression>
							(
								new CallExpression
								(
									IsNilOperatorName,
									new Expression[] 
									{ 
										#if USEROOTEDIDENTIFIERSINKEYEXPRESSIONS
										TagLine<Expression>(new IdentifierExpression(Schema.Object.EnsureRooted(AKey[index].Name)), lineInfo)
										#else
										TagLine<Expression>(new IdentifierExpression(key[index].Name), lineInfo)
										#endif
									}
								),
								lineInfo
							)
						),
						lineInfo
					);
					
				if (expression == null)
					expression = isNotNilExpression;
				else
					expression = TagLine<Expression>(new BinaryExpression(expression, Instructions.And, isNotNilExpression), lineInfo);
			}
			
			if (expression == null)
				expression = TagLine<Expression>(new ValueExpression(true), lineInfo);
				
			return expression;
		}
		
		// Produces a project node based on the columns in the given key		
		public static PlanNode EmitProjectNode(Plan plan, PlanNode node, Schema.Key key)
		{
			string[] columns = new string[key.Columns.Count];
			for (int index = 0; index < key.Columns.Count; index++)
				columns[index] = key.Columns[index].Name;
				
			return EmitProjectNode(plan, node, columns, true);
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
		public static PlanNode EmitProjectNode(Plan plan, Statement statement, PlanNode sourceNode, string[] columns, bool isProject)
		{
			PlanNode node = FindCallNode(plan, statement, isProject ? Instructions.Project : Instructions.Remove, new PlanNode[]{sourceNode});
			if (node is ProjectNodeBase)
				((ProjectNodeBase)node).ColumnNames.AddRange(columns);
			else
				((RowProjectNodeBase)node).ColumnNames.AddRange(columns);
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitProjectNode(Plan plan, PlanNode sourceNode, string[] columns, bool isProject)
		{
			return EmitProjectNode(plan, new EmptyStatement(), sourceNode, columns, isProject);
		}
		
		// Produces a project node based on all columns in the given source node.
		public static PlanNode EmitProjectNode(Plan plan, PlanNode sourceNode)
		{
			string[] columns = new string[((Schema.ITableType)sourceNode.DataType).Columns.Count];
			for (int index = 0; index < ((Schema.ITableType)sourceNode.DataType).Columns.Count; index++)
				columns[index] = ((Schema.ITableType)sourceNode.DataType).Columns[index].Name;

			return EmitProjectNode(plan, sourceNode, columns, true);
		}
		
		public static PlanNode EmitProjectNode(Plan plan, PlanNode sourceNode, ColumnExpressions columns, bool isProject)
		{
			return EmitProjectNode(plan, new EmptyStatement(), sourceNode, columns, isProject);
		}

		public static PlanNode EmitProjectNode(Plan plan, Statement statement, PlanNode sourceNode, ColumnExpressions columns, bool isProject)
		{
			string[] localColumns = new string[columns.Count];
			for (int index = 0; index < columns.Count; index++)
				localColumns[index] = columns[index].ColumnName;				
			return EmitProjectNode(plan, statement, sourceNode, localColumns, isProject);
		}
		
		public static PlanNode CompileProjectExpression(Plan plan, ProjectExpression expression)
		{
			string[] columns = new string[expression.Columns.Count];
			for (int index = 0; index < expression.Columns.Count; index++)
				columns[index] = expression.Columns[index].ColumnName;
				
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitProjectNode(plan, expression, sourceNode, columns, true);
		}
		
		public static PlanNode CompileRemoveExpression(Plan plan, RemoveExpression expression)
		{
			string[] columns = new string[expression.Columns.Count];
			for (int index = 0; index < expression.Columns.Count; index++)
				columns[index] = expression.Columns[index].ColumnName;
			
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitProjectNode(plan, expression, sourceNode, columns, false);
		}

		// Source BNF -> <table expression> group [by <column list>] add {<aggregate expression list>}
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
		public static PlanNode CompileAggregateExpression(Plan plan, AggregateExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode[] arguments = new PlanNode[]{sourceNode};
			OperatorBindingContext context = new OperatorBindingContext(expression, Instructions.Aggregate, plan.NameResolutionPath, SignatureFromArguments(arguments), false);
			AggregateNode node = (AggregateNode)FindCallNode(plan, context, arguments);
			CheckOperatorResolution(plan, context);
			node.Columns = expression.ByColumns;
			node.ComputeColumns = expression.ComputeColumns;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitCopyNode(Plan plan, TableNode sourceNode)
		{
			if (sourceNode.Order == null)
				return EmitCopyNode(plan, sourceNode, Compiler.FindClusteringKey(plan, sourceNode.TableVar));
			else
				return EmitCopyNode(plan, sourceNode, sourceNode.Order);
		}
		
		public static PlanNode EmitCopyNode(Plan plan, TableNode sourceNode, Schema.Key key)
		{
			Schema.Order order = new Schema.Order();
			Schema.OrderColumn orderColumn;
			foreach (Schema.TableVarColumn column in key.Columns)
			{
				orderColumn = new Schema.OrderColumn(sourceNode.TableVar.Columns[column], true);
				orderColumn.Sort = GetUniqueSort(plan, orderColumn.Column.DataType);
				plan.AttachDependency(orderColumn.Sort);
				order.Columns.Add(orderColumn);
			}

			return EmitCopyNode(plan, sourceNode, order);
		}
		
		public static PlanNode EmitCopyNode(Plan plan, TableNode sourceNode, Schema.Order order)
		{
			CopyNode node = (CopyNode)FindCallNode(plan, new EmptyStatement(), Instructions.Copy, new PlanNode[]{sourceNode});
			node.RequestedOrder = order;
			node.RequestedCapabilities = plan.CursorContext.CursorCapabilities;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitOrderNode(Plan plan, TableNode sourceNode, bool isAccelerator)
		{
			if (sourceNode.Order == null)
				return EmitOrderNode(plan, sourceNode, Compiler.FindClusteringKey(plan, sourceNode.TableVar), isAccelerator);
			else
				return EmitOrderNode(plan, sourceNode, sourceNode.Order, isAccelerator);
		}
		
		public static PlanNode EmitOrderNode(Plan plan, TableNode sourceNode, Schema.Key key, bool isAccelerator)
		{
			return EmitOrderNode(plan, sourceNode, key, null, null, isAccelerator);
		}

		public static PlanNode EmitOrderNode
		(
			Plan plan, 
			TableNode sourceNode, 
			Schema.Key key, 
			MetaData metaData,
			IncludeColumnExpression sequenceColumn,
			bool isAccelerator
		)
		{
			Schema.Order order = new Schema.Order(metaData);
			Schema.OrderColumn orderColumn;
			foreach (Schema.TableVarColumn column in key.Columns)
			{
				orderColumn = new Schema.OrderColumn(sourceNode.TableVar.Columns[column], true);
				orderColumn.Sort = GetUniqueSort(plan, orderColumn.Column.DataType);
				plan.AttachDependency(orderColumn.Sort);
				order.Columns.Add(orderColumn);
			}

			return EmitOrderNode(plan, sourceNode, order, null, sequenceColumn, isAccelerator);
		}
		
		public static PlanNode EmitOrderNode(Plan plan, PlanNode sourceNode, Schema.Order order, bool isAccelerator)
		{
			return EmitOrderNode(plan, sourceNode, order, null, null, isAccelerator);
		}
		
		public static PlanNode EmitOrderNode
		(
			Plan plan, 
			PlanNode sourceNode, 
			Schema.Order order, 
			MetaData metaData,
			IncludeColumnExpression sequenceColumn,
			bool isAccelerator
		)
		{
			return EmitOrderNode(plan, new EmptyStatement(), sourceNode, order, metaData, sequenceColumn, isAccelerator);
		}
		
		// Source BNF -> <table expression> order by all | <order name> | <order column list>
		//
		// operator Order(presentation{}) : presentation{}
		public static PlanNode EmitOrderNode
		(
			Plan plan, 
			Statement statement,
			PlanNode sourceNode, 
			Schema.Order order, 
			MetaData metaData,
			IncludeColumnExpression sequenceColumn,
			bool isAccelerator
		)
		{
			OrderNode node = (OrderNode)FindCallNode(plan, statement, Instructions.Order, new PlanNode[]{sourceNode});
			node.IsAccelerator = isAccelerator;
			node.RequestedOrder = order;
			node.RequestedCapabilities = plan.CursorContext.CursorCapabilities;
			node.SequenceColumn = sequenceColumn;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileOrderExpression(Plan plan, OrderExpression expression)
		{
			PlanNode sourceNode = CompileExpression(plan, expression.Expression);
			if (!(sourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);
				
			sourceNode = EnsureTableNode(plan, sourceNode);

			return 
				EmitOrderNode
				(
					plan, 
					expression, 
					sourceNode, 
					CompileOrderColumnDefinitions(plan, ((TableNode)sourceNode).TableVar, expression.Columns, null, false), 
					null, 
					expression.SequenceColumn,
					false
				);
		}
		
		public static PlanNode AppendNode(Plan plan, PlanNode leftNode, string instruction, PlanNode rightNode)
		{
			return (leftNode != null) ? ((rightNode != null) ? EmitBinaryNode(plan, leftNode, instruction, rightNode) : leftNode) : rightNode;
		}
		
		public static PlanNode EmitBrowseNode(Plan plan, TableNode sourceNode, bool isAccelerator)
		{
			if (sourceNode.Order == null)
				return EmitBrowseNode(plan, sourceNode, Compiler.FindClusteringKey(plan, sourceNode.TableVar), isAccelerator);
			else
				return EmitBrowseNode(plan, sourceNode, sourceNode.Order, isAccelerator);
		}
		
		public static PlanNode EmitBrowseNode(Plan plan, TableNode sourceNode, Schema.Key key, bool isAccelerator)
		{
			return EmitBrowseNode(plan, sourceNode, key, null, isAccelerator);
		}

		public static PlanNode EmitBrowseNode(Plan plan, TableNode sourceNode, Schema.Key key, MetaData metaData, bool isAccelerator)
		{
			Schema.Order order = new Schema.Order(metaData);
			foreach (Schema.TableVarColumn column in key.Columns)
			{
				Schema.OrderColumn newOrderColumn = new Schema.OrderColumn(sourceNode.TableVar.Columns[column], true);
				newOrderColumn.IsDefaultSort = true;
				newOrderColumn.Sort = GetSort(plan, newOrderColumn.Column.DataType);
				plan.AttachDependency(newOrderColumn.Sort);
				order.Columns.Add(newOrderColumn);
			}

			return EmitBrowseNode(plan, sourceNode, order, isAccelerator);
		}
		
		public static PlanNode EmitBrowseNode(Plan plan, PlanNode sourceNode, Schema.Order order, bool isAccelerator)
		{
			return EmitBrowseNode(plan, sourceNode, order, null, isAccelerator);
		}
		
		public static PlanNode EmitBrowseNode(Plan plan, PlanNode sourceNode, Schema.Order order, MetaData metaData, bool isAccelerator)
		{
			return EmitBrowseNode(plan, new EmptyStatement(), sourceNode, order, metaData, isAccelerator);
		}
		
		// operator Browse(table{}, object, object) : table{}		
		public static PlanNode EmitBrowseNode(Plan plan, Statement statement, PlanNode sourceNode, Schema.Order order, MetaData metaData, bool isAccelerator)
		{
			BrowseNode node = (BrowseNode)FindCallNode(plan, statement, Instructions.Browse, new PlanNode[]{sourceNode});
			node.IsAccelerator = isAccelerator;
			node.RequestedOrder = order;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileBrowseExpression(Plan plan, BrowseExpression expression)
		{
			PlanNode sourceNode = CompileExpression(plan, expression.Expression);
			if (!(sourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);
				
			sourceNode = EnsureTableNode(plan, sourceNode);
				
			return 
				EmitBrowseNode
				(
					plan, 
					expression, 
					sourceNode, 
					CompileOrderColumnDefinitions(plan, ((TableNode)sourceNode).TableVar, expression.Columns, null, true), 
					null,
					false
				);
		}

		public static PlanNode EnsureSearchableNode(Plan plan, TableNode sourceNode, Schema.TableVarColumnsBase columns)
		{
			return EnsureSearchableNode(plan, sourceNode, OrderFromKey(plan, columns));
		}
		
		public static PlanNode EnsureSearchableNode(Plan plan, TableNode sourceNode, Schema.Key key)
		{
			return EnsureSearchableNode(plan, sourceNode, OrderFromKey(plan, key));
		}
		
		/// <summary>Ensures that the node given by ASourceNode is searchable by the order given by ASearchOrder. This method should only be called in a binding context.</summary>
		/// <remarks>
		/// This method during the binding phase to ensure that a given node will produce a result set in the given order and with the requested capabilities.
		/// If the source node does support a searchable cursor ordered by the given order, the compiler will first request an order node be emitted
		/// and then determine the device of that order node. If the resulting ordered node does not provide searchable capabilities, then if the
		/// order is supported by the device, a browse node is used to provide the search capabilities, otherwise a copy node is used to materialize
		/// the order and provide the requested capabilities.
		/// </remarks>
		public static PlanNode EnsureSearchableNode(Plan plan, TableNode sourceNode, Schema.Order searchOrder)
		{
			if ((sourceNode.Order == null) || !searchOrder.Equivalent(sourceNode.Order) || !sourceNode.Supports(CursorCapability.Searchable))
			{
				plan.PushGlobalContext();
				try
				{
					plan.PushCursorContext(new CursorContext(sourceNode.CursorType, sourceNode.CursorCapabilities | CursorCapability.Searchable, sourceNode.CursorIsolation));
					try
					{
						BaseOrderNode node = Compiler.EmitOrderNode(plan, sourceNode, searchOrder, true) as BaseOrderNode;
						node.InferPopulateNode(plan); // The A/T populate node, if any, should be used from the source node for the order
						node.DeterminePotentialDevice(plan);
						node.DetermineDevice(plan); // doesn't call binding because the source for the order is already bound, only needs to determine the device for the newly created order node.
						node.DetermineAccessPath(plan);
						if (!node.Supports(CursorCapability.Searchable))
						{
							if (node.DeviceSupported)
								node = Compiler.EmitBrowseNode(plan, sourceNode, searchOrder, true) as BaseOrderNode;
							else
								node = Compiler.EmitCopyNode(plan, sourceNode, searchOrder) as BaseOrderNode;
							node.InferPopulateNode(plan);
							node.DeterminePotentialDevice(plan);
							node.DetermineDevice(plan);
							node.DetermineAccessPath(plan);
						}
							
						return node;
					}
					finally
					{
						plan.PopCursorContext();
					}
				}
				finally
				{
					plan.PopGlobalContext();
				}
			}
			return sourceNode;
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
		public static PlanNode EmitQuotaNode(Plan plan, Statement statement, PlanNode sourceNode, PlanNode countNode, Schema.Order order)
		{
			QuotaNode node = (QuotaNode)FindCallNode(plan, statement, Instructions.Quota, new PlanNode[]{sourceNode, countNode});
			node.QuotaOrder = order;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitQuotaNode(Plan plan, PlanNode sourceNode, PlanNode countNode, Schema.Order order)
		{
			return EmitQuotaNode(plan, new EmptyStatement(), sourceNode, countNode, order);
		}
		
		public static PlanNode CompileQuotaExpression(Plan plan, QuotaExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			if (!(sourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);

			sourceNode = EnsureTableNode(plan, sourceNode);
				
			return 
				EmitQuotaNode
				(
					plan,
					expression,
					sourceNode,
					CompileExpression(plan, expression.Quota),
					expression.HasByClause ?
						CompileOrderColumnDefinitions(plan, ((TableNode)sourceNode).TableVar, expression.Columns, null, false) :
						OrderFromKey(plan, FindClusteringKey(plan, ((TableNode)sourceNode).TableVar))
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
			Plan plan, 
			Statement statement,
			PlanNode sourceNode,
			PlanNode rootNode, 
			PlanNode parentNode, 
			IncludeColumnExpression levelColumn, 
			IncludeColumnExpression sequenceColumn
		)
		{
			ExplodeNode node = (ExplodeNode)FindCallNode(plan, statement, Instructions.Explode, new PlanNode[]{sourceNode, rootNode, parentNode});
			node.LevelColumn = levelColumn;
			node.SequenceColumn = sequenceColumn;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitExplodeNode
		(
			Plan plan, 
			PlanNode sourceNode,
			PlanNode rootNode, 
			PlanNode parentNode, 
			IncludeColumnExpression levelColumn, 
			IncludeColumnExpression sequenceColumn
		)
		{
			return EmitExplodeNode(plan, sourceNode, rootNode, parentNode, levelColumn, sequenceColumn);
		}
		
		public static PlanNode CompileExplodeColumnExpression(Plan plan, ExplodeColumnExpression expression)
		{
			#if USENAMEDROWVARIABLES
			return 
				CompileQualifierExpression
				(
					plan, 
					TagLine<QualifierExpression>
					(
						new QualifierExpression
						(
							TagLine<IdentifierExpression>(new IdentifierExpression(Keywords.Parent), expression.LineInfo), 
							TagLine<IdentifierExpression>(new IdentifierExpression(expression.ColumnName), expression.LineInfo)
						),
						expression.LineInfo
					)
				);
			#else
			NameBindingContext context = new NameBindingContext(String.Format("{0}{1}{2}", Keywords.Parent, Keywords.Qualifier, AExpression.ColumnName), APlan.NameResolutionPath);
			PlanNode node = EmitIdentifierNode(APlan, AExpression, context);
			if (node == null)
				if (context.IsAmbiguous)
					throw new CompilerException(CompilerException.Codes.AmbiguousIdentifier, AExpression, String.Format("{0} {1}", Keywords.Parent, AExpression.ColumnName), ExceptionUtility.StringsToCommaList(context.Names));
				else
					throw new CompilerException(CompilerException.Codes.UnknownIdentifier, AExpression, String.Format("{0} {1}", Keywords.Parent, AExpression.ColumnName));
			return node;
			#endif
		}
		
		public static PlanNode CompileExplodeExpression(Plan plan, ExplodeExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			if (!(sourceNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);
				
			sourceNode = EnsureTableNode(plan, sourceNode);
				
			PlanNode rootNode;
			PlanNode parentNode;
			
			Expression rootExpression = new RestrictExpression(expression.Expression, expression.RootExpression);
			Expression byExpression = new RestrictExpression(expression.Expression, expression.ByExpression);
			
			if (expression.HasOrderByClause)
			{
				rootExpression = new OrderExpression(rootExpression, expression.OrderColumns);
				byExpression = new OrderExpression(byExpression, expression.OrderColumns);
			}
			
			rootNode = CompileExpression(plan, rootExpression);
			if (!(rootNode.DataType is Schema.ITableType))
				throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);
				
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Parent, ((Schema.ITableType)rootNode.DataType).RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.ITableType)rootNode.DataType).Columns, Keywords.Parent)));
				#endif
				try
				{
					parentNode = CompileExpression(plan, byExpression);
					if (!(parentNode.DataType is Schema.ITableType))
						throw new CompilerException(CompilerException.Codes.TableExpressionExpected, expression.Expression);
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
			
			if ((expression.LevelColumn != null) || (expression.SequenceColumn != null))
			{
				if (!expression.HasOrderByClause && !plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidExplodeExpression, CompilerErrorLevel.Warning, expression));
				
				if (expression.HasOrderByClause && !IsOrderUnique(plan, ((TableNode)sourceNode).TableVar, ((TableNode)rootNode).Order) && !plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidExplodeExpressionOrder, CompilerErrorLevel.Warning, expression));
			}	

			return EmitExplodeNode(plan, expression, sourceNode, rootNode, parentNode, expression.LevelColumn, expression.SequenceColumn);
		}
		
		public static PlanNode EmitExtendNode(Plan plan, PlanNode sourceNode, NamedColumnExpressions expressions)
		{
			return EmitExtendNode(plan, new EmptyStatement(), sourceNode, expressions);
		}
		
		public static PlanNode EmitExtendNode(Plan plan, Statement statement, PlanNode sourceNode, NamedColumnExpressions expressions)
		{
			PlanNode node = FindCallNode(plan, statement, Instructions.Extend, new PlanNode[]{sourceNode});
			if (node is ExtendNode)
				((ExtendNode)node).Expressions = expressions;
			else
				((RowExtendNode)node).Expressions = expressions;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}	
		
		public static PlanNode CompileExtendExpression(Plan plan, ExtendExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitExtendNode(plan, expression, sourceNode, expression.Expressions);
		}
		
		public static string GetUniqueColumnName(List<string> columnNames)
		{
			string columnName;
			int index = 0;
			do
			{
				index++;
				columnName = String.Format("Column{0}", index.ToString());
			} while (columnNames.Contains(columnName));
			
			return columnName;
		}
		
		public static string GetUniqueColumnName(Schema.Columns columns)
		{
			string columnName;
			int index = 0;
			do
			{
				index++;
				columnName = String.Format("Column{0}", index.ToString());
			} while (columns.ContainsName(columnName));
			
			return columnName;
		}
		
		private static void AddTemporaryColumn(NamedColumnExpressions addExpressions, RenameColumnExpressions renameExpressions, ColumnExpressions projectExpressions, List<string> projectNames, NamedColumnExpression localExpression, string resultColumnAlias)
		{
			string columnAlias = Schema.Object.GetUniqueName();
			NamedColumnExpression addExpression = new NamedColumnExpression(localExpression.Expression, columnAlias, localExpression.MetaData);
			addExpression.Line = localExpression.Line;
			addExpression.LinePos = localExpression.LinePos;
			addExpressions.Add(addExpression);
			ColumnExpression projectExpression = new ColumnExpression(columnAlias);
			projectExpression.Line = localExpression.Line;
			projectExpression.LinePos = localExpression.LinePos;
			projectExpressions.Add(projectExpression);
			projectNames.Add(columnAlias);
			RenameColumnExpression renameExpression = new RenameColumnExpression(columnAlias, resultColumnAlias);
			renameExpression.Line = localExpression.Line;
			renameExpression.LinePos = localExpression.LinePos;
			renameExpressions.Add(renameExpression);
		}

		public static PlanNode CompileSpecifyExpression(Plan plan, SpecifyExpression expression)
		{
			PlanNode node = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				node = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			Schema.Columns sourceColumns = node.DataType is Schema.ITableType ? ((Schema.ITableType)node.DataType).Columns : ((Schema.IRowType)node.DataType).Columns;
			
			NamedColumnExpressions addExpressions = new NamedColumnExpressions();
			RenameColumnExpressions renameExpressions = new RenameColumnExpressions();
			ColumnExpressions projectExpressions = new ColumnExpressions();

			List<string> resultNames = new List<string>();
			List<string> projectNames = new List<string>();
			
			// Compute the list of result column names
			foreach (NamedColumnExpression localExpression in expression.Expressions)
			{
				IdentifierExpression identifierExpression = Parser.CollapseQualifiedIdentifierExpression(localExpression.Expression);
				if ((identifierExpression != null) && sourceColumns.Contains(identifierExpression.Identifier))
					localExpression.Expression = identifierExpression;
					
				if (localExpression.ColumnAlias != String.Empty)
					resultNames.Add(localExpression.ColumnAlias);
				else
				{
					if ((identifierExpression != null) && sourceColumns.Contains(identifierExpression.Identifier))
						resultNames.Add(sourceColumns[identifierExpression.Identifier].Name);
					else
						#if ALLOWUNNAMEDCOLUMNS
						localExpression.ColumnAlias = GetUniqueColumnName(resultNames);
						#else
						throw new CompilerException(CompilerException.Codes.ColumnNameExpected, localExpression);
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
			foreach (NamedColumnExpression localExpression in expression.Expressions)
			{
				IdentifierExpression identifierExpression = localExpression.Expression as IdentifierExpression;
				if ((identifierExpression != null) && sourceColumns.ContainsName(identifierExpression.Identifier))
				{
					if ((localExpression.ColumnAlias == String.Empty) || (identifierExpression.Identifier == localExpression.ColumnAlias))
					{
						if (projectNames.Contains(sourceColumns[identifierExpression.Identifier].Name))
						{
							AddTemporaryColumn(addExpressions, renameExpressions, projectExpressions, projectNames, localExpression, localExpression.ColumnAlias == String.Empty ? identifierExpression.Identifier : localExpression.ColumnAlias);
						}
						else
						{
							projectExpressions.Add(new ColumnExpression(identifierExpression.Identifier));
							projectNames.Add(sourceColumns[identifierExpression.Identifier].Name);
						}
					}
					else
					{
						//if (resultNames.Contains(identifierExpression.Identifier) || projectNames.Contains(sourceColumns[identifierExpression.Identifier].Name))
						if (projectNames.Contains(sourceColumns[identifierExpression.Identifier].Name))
						{
							if (sourceColumns.ContainsName(localExpression.ColumnAlias))
							{
								AddTemporaryColumn(addExpressions, renameExpressions, projectExpressions, projectNames, localExpression, localExpression.ColumnAlias);
							}
							else
							{
								addExpressions.Add(localExpression);
								ColumnExpression projectExpression = new ColumnExpression(localExpression.ColumnAlias);
								projectExpression.Line = localExpression.Line;
								projectExpression.LinePos = localExpression.LinePos;
								projectExpressions.Add(projectExpression);
								projectNames.Add(localExpression.ColumnAlias);
							}
						}
						else
						{
							ColumnExpression projectExpression = new ColumnExpression(identifierExpression.Identifier);
							projectExpression.Line = identifierExpression.Line;
							projectExpression.LinePos = identifierExpression.LinePos;
							projectExpressions.Add(projectExpression);
							projectNames.Add(sourceColumns[identifierExpression.Identifier].Name);
							RenameColumnExpression renameExpression = new RenameColumnExpression(identifierExpression.Identifier, localExpression.ColumnAlias);
							renameExpression.Line = identifierExpression.Line;
							renameExpression.LinePos = identifierExpression.Line;
							renameExpression.MetaData = localExpression.MetaData;
							renameExpressions.Add(renameExpression);
						}
					}
				}
				else
				{
					if (sourceColumns.ContainsName(localExpression.ColumnAlias))
					{
						AddTemporaryColumn(addExpressions, renameExpressions, projectExpressions, projectNames, localExpression, localExpression.ColumnAlias);
					}
					else
					{
						addExpressions.Add(localExpression);
						ColumnExpression projectExpression = new ColumnExpression(Schema.Object.EnsureRooted(localExpression.ColumnAlias));
						projectExpression.Line = localExpression.Line;
						projectExpression.LinePos = localExpression.LinePos;
						projectExpressions.Add(projectExpression);
						projectNames.Add(localExpression.ColumnAlias);
					}
				}
			}
			
			if (addExpressions.Count > 0)
				node = EmitExtendNode(plan, node, addExpressions);
				
			node = EmitProjectNode(plan, expression, node, projectExpressions, true);
			
			if (renameExpressions.Count > 0)
				node = EmitRenameNode(plan, node, renameExpressions);
				
			return node;
		}

		public static PlanNode EmitRenameNode(Plan plan, PlanNode sourceNode, string tableAlias)
		{
			return EmitRenameNode(plan, sourceNode, tableAlias, null);
		}

		public static PlanNode EmitRenameNode(Plan plan, PlanNode sourceNode, string tableAlias, MetaData metaData)
		{
			return EmitRenameNode(plan, new EmptyStatement(), sourceNode, tableAlias, metaData);
		}
		
		// operator Rename(table{}, string, object) : table{}		
		public static PlanNode EmitRenameNode(Plan plan, Statement statement, PlanNode sourceNode, string tableAlias, MetaData metaData)
		{
			PlanNode node = FindCallNode(plan, statement, Instructions.Rename, new PlanNode[]{sourceNode});
			if (node is RenameNode)
			{
				RenameNode renameNode = (RenameNode)node;
				renameNode.TableAlias = tableAlias;
				renameNode.MetaData = metaData;
			}
			else
			{
				RowRenameNode rowRenameNode = (RowRenameNode)node;
				rowRenameNode.RowAlias = tableAlias;
				rowRenameNode.MetaData = metaData;
			}
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}

		public static PlanNode EmitRenameNode(Plan plan, PlanNode sourceNode, RenameColumnExpressions expressions)
		{
			return EmitRenameNode(plan, new EmptyStatement(), sourceNode, expressions);
		}
		
		// operator Rename(table{}, object) : table{}		
		public static PlanNode EmitRenameNode(Plan plan, Statement statement, PlanNode sourceNode, RenameColumnExpressions expressions)
		{
			PlanNode node = FindCallNode(plan, statement, Instructions.Rename, new PlanNode[]{sourceNode});
			if (node is RenameNode)
				((RenameNode)node).Expressions = expressions;
			else
				((RowRenameNode)node).Expressions = expressions;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileRenameExpression(Plan plan, RenameExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitRenameNode(plan, expression, sourceNode, expression.Expressions);
		}
		
		public static PlanNode CompileRenameAllExpression(Plan plan, RenameAllExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitRenameNode(plan, expression, sourceNode, expression.Identifier, expression.MetaData);
		}
		
		public static PlanNode CompileIsExpression(Plan plan, IsExpression expression)
		{
			IsNode result = new IsNode();
			result.Nodes.Add(CompileExpression(plan, expression.Expression));
			result.TargetType = CompileTypeSpecifier(plan, expression.TypeSpecifier);
			result.DetermineDataType(plan);
			result.DetermineCharacteristics(plan);
			return result;
		}
		
		public static PlanNode CompileAsExpression(Plan plan, AsExpression expression)
		{
			PlanNode node = CompileExpression(plan, expression.Expression);
			if (node.DataType is Schema.ITableType)
			{
				TableAsNode result = new TableAsNode();
				var asType = CompileTypeSpecifier(plan, expression.TypeSpecifier);
				if (!asType.Is(node.DataType))
					throw new CompilerException(CompilerException.Codes.InvalidCast, expression, node.DataType.Name, asType.Name);
				result.Nodes.Add(Downcast(plan, node, asType));
				result.DetermineDataType(plan);
				result.DetermineCharacteristics(plan);
				return result;
			}
			else
			{
				AsNode result = new AsNode();
				result.DataType = CompileTypeSpecifier(plan, expression.TypeSpecifier);
				if (!result.DataType.Is(node.DataType))
					throw new CompilerException(CompilerException.Codes.InvalidCast, expression, node.DataType.Name, result.DataType.Name);
				result.Nodes.Add(Downcast(plan, node, result.DataType));
				result.DetermineCharacteristics(plan);
				return result;
			}
		}
		
		#if CALCULESQUE
		public static PlanNode CompileNamedExpression(Plan APlan, NamedExpression AExpression)
		{
			return CompileExpression(APlan, AExpression.Expression);
		}
		#endif
		
		public static PlanNode EmitAdornNode(Plan plan, PlanNode sourceNode, AdornExpression adornExpression)
		{
			return EmitAdornNode(plan, null, sourceNode, adornExpression);
		}
		
		public static PlanNode EmitAdornNode(Plan plan, Statement statement, PlanNode sourceNode, AdornExpression adornExpression)
		{
			AdornNode node = (AdornNode)FindCallNode(plan, statement, Instructions.Adorn, new PlanNode[]{sourceNode});
			node.Expressions = adornExpression.Expressions;
			node.Constraints = adornExpression.Constraints;
			node.Orders = adornExpression.Orders;
			node.AlterOrders = adornExpression.AlterOrders;
			node.DropOrders = adornExpression.DropOrders;
			node.Keys = adornExpression.Keys;
			node.AlterKeys = adornExpression.AlterKeys;
			node.DropKeys = adornExpression.DropKeys;
			node.References = adornExpression.References;
			node.AlterReferences = adornExpression.AlterReferences;
			node.DropReferences = adornExpression.DropReferences;
			node.MetaData = adornExpression.MetaData;
			node.AlterMetaData = adornExpression.AlterMetaData;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileAdornExpression(Plan plan, AdornExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitAdornNode(plan, expression, sourceNode, expression);
		}
		
		public static PlanNode EmitRedefineNode(Plan plan, PlanNode sourceNode, NamedColumnExpressions expressions)
		{
			return EmitRedefineNode(plan, new EmptyStatement(), sourceNode, expressions);
		}
		
		// T redefine { A := A + A } ::= 
		//	T add { A + A Temp } remove { A } rename { Temp A }
		public static PlanNode EmitRedefineNode(Plan plan, Statement statement, PlanNode sourceNode, NamedColumnExpressions expressions)
		{
			#if USEREDEFINENODE
			PlanNode node = FindCallNode(APlan, AStatement, Instructions.Redefine, new PlanNode[]{ASourceNode});
			if (node is RedefineNode)
				((RedefineNode)node).Expressions = AExpressions;
			else
				((RowRedefineNode)node).Expressions = AExpressions;
			node.DetermineDataType(APlan);
			node.DetermineCharacteristics(APlan);
			if ((node is RedefineNode) && ((RedefineNode)node).DistinctRequired)
				return EmitProjectNode(APlan, node, FindClusteringKey(APlan, ((TableNode)node).TableVar));
			return node;
			#else
			NamedColumnExpressions addExpressions = new NamedColumnExpressions();
			RenameColumnExpressions renameExpressions = new RenameColumnExpressions();
			string[] removeColumns = new string[expressions.Count];
			string columnName;
			for (int index = 0; index < expressions.Count; index++)
			{
				columnName = Schema.Object.GetUniqueName();
				addExpressions.Add(new NamedColumnExpression(expressions[index].Expression, columnName));
				removeColumns[index] = expressions[index].ColumnAlias;
				renameExpressions.Add(new RenameColumnExpression(columnName, expressions[index].ColumnAlias));
			}

			return 
				EmitRenameNode
				(
					plan, 
					statement, 
					EmitProjectNode
					(
						plan, 
						statement, 
						EmitExtendNode
						(
							plan, 
							statement, 
							sourceNode, 
							addExpressions
						), 
						removeColumns, 
						false
					), 
					renameExpressions
				);
			#endif
		}
		
		public static PlanNode CompileRedefineExpression(Plan plan, RedefineExpression expression)
		{
			PlanNode sourceNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression);
			try
			{
				sourceNode = CompileExpression(plan, expression.Expression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitRedefineNode(plan, expression, sourceNode, expression.Expressions);
		}

		public static PlanNode CompileOnExpression(Plan plan, OnExpression expression)
		{
			OnNode node = (OnNode)FindCallNode(plan, expression, Instructions.On, new PlanNode[]{});
			node.OnExpression = expression;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitUnionNode(Plan plan, Statement statement, PlanNode leftNode, PlanNode rightNode)
		{
			return 
				EmitCallNode
				(
					plan, 
					statement,
					Instructions.Union, 
					new PlanNode[]{leftNode, rightNode}
				);
		}

		// note that the raw union operator will return a tuple-bag, the compiler must ensure
		// that the operator is wrapped by a projection node to ensure uniqueness		
		public static PlanNode CompileUnionExpression(Plan plan, UnionExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode rightNode = null;
			transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return EmitUnionNode(plan, expression, leftNode, rightNode);
		}
		
		public static PlanNode EmitDifferenceNode(Plan plan, Statement statement, PlanNode leftNode, PlanNode rightNode)
		{
			return EmitCallNode(plan, statement, Instructions.Difference, new PlanNode[]{leftNode, rightNode});
		}
		
		public static PlanNode CompileDifferenceExpression(Plan plan, DifferenceExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode rightNode = null;
			transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return 
				EmitDifferenceNode
				(
					plan, 
					expression,
					leftNode,
					rightNode
				);
		}
		
		public static PlanNode CompileIntersectExpression(Plan plan, IntersectExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode rightNode = null;
			transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			JoinNode node = (JoinNode)FindCallNode(plan, expression, Instructions.Join, new PlanNode[]{leftNode, rightNode});
			node.IsNatural = true;
			node.IsIntersect = true;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileProductExpression(Plan plan, ProductExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode rightNode = null;
			transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			JoinNode node = (JoinNode)FindCallNode(plan, expression, Instructions.Join, new PlanNode[]{leftNode, rightNode});
			node.IsNatural = true;
			node.IsTimes = true;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode EmitHavingNode(Plan plan, Statement statement, PlanNode leftNode, PlanNode rightNode, Expression condition)
		{
			HavingNode node = (HavingNode)FindCallNode(plan, statement, Instructions.Having, new PlanNode[]{leftNode, rightNode});
			if (condition == null)
				node.IsNatural = true;
			else
				node.Expression = condition;
				
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileHavingExpression(Plan plan, HavingExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode rightNode = null;
			transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return 
				EmitHavingNode
				(
					plan, 
					expression,
					leftNode,
					rightNode,
					expression.Condition
				);
		}
		
		public static PlanNode EmitWithoutNode(Plan plan, Statement statement, PlanNode leftNode, PlanNode rightNode, Expression condition)
		{
			WithoutNode node = (WithoutNode)FindCallNode(plan, statement, Instructions.Without, new PlanNode[]{leftNode, rightNode});
			if (condition == null)
				node.IsNatural = true;
			else
				node.Expression = condition;
				
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileWithoutExpression(Plan plan, WithoutExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			PlanNode rightNode = null;
			transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			return 
				EmitWithoutNode
				(
					plan, 
					expression,
					leftNode,
					rightNode,
					expression.Condition
				);
		}
		
		public static PlanNode EmitInnerJoinNode
		(
			Plan plan, 
			PlanNode leftNode,
			PlanNode rightNode,
			InnerJoinExpression expression
		)
		{
			PlanNode planNode = FindCallNode(plan, expression, Instructions.Join, new PlanNode[]{leftNode, rightNode});
			if (planNode is JoinNode)
			{
				JoinNode node = (JoinNode)planNode;
				node.Expression = expression.Condition;
				node.IsLookup = expression.IsLookup;
				node.IsNatural = expression.Condition == null;
				node.DetermineDataType(plan);
				node.DetermineCharacteristics(plan);
				return node;
			}
			else
			{
				if (expression.Condition != null)
					throw new CompilerException(CompilerException.Codes.InvalidRowJoin, expression.Condition);
				planNode.DetermineDataType(plan);
				planNode.DetermineCharacteristics(plan);
				return planNode;
			}
		}
		
		public static PlanNode CompileInnerJoinExpression(Plan plan, InnerJoinExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}

			transaction = null;
			if (plan.ApplicationTransactionID != Guid.Empty)
				transaction = plan.GetApplicationTransaction();
			try
			{
				bool isLookup = expression.IsLookup ? !Convert.ToBoolean(LanguageModifiers.GetModifier(expression.Modifiers, "IsDetailLookup", "false")) : expression.IsLookup;
				bool shouldTranslate = Convert.ToBoolean(LanguageModifiers.GetModifier(expression.Modifiers, "Right.ShouldTranslate", (!isLookup).ToString()));
				if ((transaction != null) && !shouldTranslate)
					transaction.PushLookup();
				try
				{
					PlanNode rightNode = CompileExpression(plan, expression.RightExpression);
					PlanNode resultNode = EmitInnerJoinNode(plan, leftNode, rightNode, expression);
					JoinNode joinNode = resultNode as JoinNode;
					if ((joinNode != null) && (transaction != null) && !shouldTranslate && joinNode.IsDetailLookup)
					{
						transaction.PopLookup();
						try
						{
							rightNode = CompileExpression(plan, expression.RightExpression);
							return EmitInnerJoinNode(plan, leftNode, rightNode, expression);
						}
						finally
						{
							transaction.PushLookup();
						}
					}
					else
						return resultNode;
				}
				finally
				{
					if ((transaction != null) && !shouldTranslate)
						transaction.PopLookup();
				}
			}
			finally
			{
				if (transaction != null)
					Monitor.Exit(transaction);
			}
		}
		
		public static Schema.TableVarColumn CompileIncludeColumnExpression
		(
			Plan plan, 
			IncludeColumnExpression column, 
			string columnName, 
			Schema.IScalarType dataType, 
			Schema.TableVarColumnType columnType
		)
		{
			Schema.Column localColumn =	
				new Schema.Column
				(
					column.ColumnAlias == String.Empty ? columnName : column.ColumnAlias, 
					dataType
				);

			Schema.TableVarColumn tableVarColumn =				
				new Schema.TableVarColumn
				(
					localColumn,
					column.MetaData, 
					columnType
				);
				
			//LTableVarColumn.Default = LColumn.DataType.Default; // cant do this because it would change the parent of the default, even if the defaults were of the same type, which they are not
			return tableVarColumn;
		}

		public static LeftOuterJoinNode EmitLeftOuterJoinNode
		(
			Plan plan, 
			PlanNode leftNode, 
			PlanNode rightNode, 
			LeftOuterJoinExpression expression
		)
		{
			LeftOuterJoinNode node = (LeftOuterJoinNode)FindCallNode(plan, expression, Instructions.LeftJoin, new PlanNode[]{leftNode, rightNode});
			node.Expression = expression.Condition;
			node.IsLookup = expression.IsLookup;
			node.IsNatural = expression.Condition == null;
			node.RowExistsColumn = expression.RowExistsColumn;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileLeftOuterJoinExpression(Plan plan, LeftOuterJoinExpression expression)
		{
			PlanNode leftNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Left");
			try
			{
				leftNode = CompileExpression(plan, expression.LeftExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}
			
			transaction = null;
			if (plan.ApplicationTransactionID != Guid.Empty)
				transaction = plan.GetApplicationTransaction();
			try
			{
				bool isLookup = expression.IsLookup ? !Convert.ToBoolean(LanguageModifiers.GetModifier(expression.Modifiers, "IsDetailLookup", "false")) : expression.IsLookup;
				bool shouldTranslate = Convert.ToBoolean(LanguageModifiers.GetModifier(expression.Modifiers, "Right.ShouldTranslate", (!isLookup).ToString()));
				if ((transaction != null) && !shouldTranslate)
					transaction.PushLookup();
				try
				{
					PlanNode rightNode = CompileExpression(plan, expression.RightExpression);
					LeftOuterJoinNode node = EmitLeftOuterJoinNode(plan, leftNode, rightNode, expression);
					if ((transaction != null) && !shouldTranslate && node.IsDetailLookup)
					{
						transaction.PopLookup();
						try
						{
							rightNode = CompileExpression(plan, expression.RightExpression);
							return EmitLeftOuterJoinNode(plan, leftNode, rightNode, expression);
						}
						finally
						{
							transaction.PushLookup();
						}
					}
					else
						return node;
				}
				finally
				{
					if ((transaction != null) && !shouldTranslate)
						transaction.PopLookup();
				}
			}
			finally
			{
				if (transaction != null)
					Monitor.Exit(transaction);
			}
		}
		
		public static RightOuterJoinNode EmitRightOuterJoinNode
		(
			Plan plan, 
			PlanNode leftNode, 
			PlanNode rightNode, 
			RightOuterJoinExpression expression
		)
		{
			RightOuterJoinNode node = (RightOuterJoinNode)FindCallNode(plan, expression, Instructions.RightJoin, new PlanNode[]{leftNode, rightNode});
			node.Expression = expression.Condition;
			node.IsLookup = expression.IsLookup;
			node.IsNatural = expression.Condition == null;
			node.RowExistsColumn = expression.RowExistsColumn;
			node.DetermineDataType(plan);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static PlanNode CompileRightOuterJoinExpression(Plan plan, RightOuterJoinExpression expression)
		{
			PlanNode rightNode = null;
			ApplicationTransaction transaction = PrepareShouldTranslate(plan, expression, "Right");
			try
			{
				rightNode = CompileExpression(plan, expression.RightExpression);
			}
			finally
			{
				UnprepareShouldTranslate(plan, transaction);
			}
			
			transaction = null;
			if (plan.ApplicationTransactionID != Guid.Empty)
				transaction = plan.GetApplicationTransaction();
			try
			{
				bool isLookup = expression.IsLookup ? !Convert.ToBoolean(LanguageModifiers.GetModifier(expression.Modifiers, "IsDetailLookup", "false")) : expression.IsLookup;
				bool shouldTranslate = Convert.ToBoolean(LanguageModifiers.GetModifier(expression.Modifiers, "Left.ShouldTranslate", (!isLookup).ToString()));
				if ((transaction != null) && !shouldTranslate)
					transaction.PushLookup();
				try
				{
					PlanNode leftNode = CompileExpression(plan, expression.LeftExpression);
					RightOuterJoinNode node = EmitRightOuterJoinNode(plan, leftNode, rightNode, expression);
					if ((transaction != null) && !shouldTranslate && node.IsDetailLookup)
					{
						transaction.PopLookup();
						try
						{
							leftNode = CompileExpression(plan, expression.LeftExpression);
							return EmitRightOuterJoinNode(plan, leftNode, rightNode, expression);
						}
						finally
						{
							transaction.PushLookup();
						}
					}
					else
						return node;
				}
				finally
				{
					if ((transaction != null) && !shouldTranslate)
						transaction.PopLookup();
				}
			}
			finally
			{
				if (transaction != null)
					Monitor.Exit(transaction);
			}
		}

		public static PlanNode CompileEmptyStatement(Plan plan, Statement statement)
		{
			PlanNode node = new BlockNode();
			node.SetLineInfo(plan, statement.LineInfo);
			node.DetermineCharacteristics(plan);
			return node;
		}
		
		public static Symbols SnapshotSymbols(Plan plan)
		{
			Symbols symbols = new Symbols(plan.Symbols.MaxStackDepth, plan.Symbols.MaxCallDepth);
			for (int index = plan.Symbols.Count - 1; index >= 0; index--)
				symbols.Push(plan.Symbols.Peek(index));

			return symbols;
		}
	}
}

