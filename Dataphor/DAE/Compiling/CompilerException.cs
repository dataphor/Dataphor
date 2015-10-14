/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Resources;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.DAE.Compiling
{
	public enum CompilerErrorLevel 
	{ 
		/// <summary>Fatal error, compilation will terminate.</summary>
		Fatal, 
		
		/// <summary>Non-fatal error, compilation will continue, but will not complete.</summary>
		NonFatal, 
		
		/// <summary>Compiler warning, compilation will continue, and be able to complete.  The user will be informed of the warning.</summary>
		Warning 
	};
	
	/// <summary>Indicates an error encountered during the semantic analysis phase of compiling.</summary>
	/// <remarks>
	///	The CompilerException indicates that the semantic analyzer encountered an invalid construct while 
	///	processing the abstract syntax tree for a given program.  The line number and position of the
	/// invalid constructs will be given if possible.  Only the compiler should throw exceptions of this type.
	/// </remarks>
	public class CompilerException : DAEException, ILocatorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 105100: "Unknown statement class "{0}"."</summary>
			UnknownStatementClass = 105100,

			/// <summary>Error code 105101: "Unknown expression class "{0}"."</summary>
			UnknownExpressionClass = 105101,

			/// <summary>Error code 105102: "Identifier "{0}" is not a valid expression."</summary>
			InvalidIdentifier = 105102,

			/// <summary>Error code 105103: "The identifier "{0}" is already defined in this scope."</summary>
			CreatingDuplicateIdentifier = 105103,

			/// <summary>Error code 105104: "Unable to resolve type for literal token "{0}"."</summary>
			UnknownLiteralType = 105104,

			/// <summary>Error code 105105: "Boolean expression expected."</summary>
			BooleanExpressionExpected = 105105,

			/// <summary>Error code 105106: "Integer expression expected."</summary>
			IntegerExpressionExpected = 105106,

			/// <summary>Error code 105107: "Error expression expected."</summary>
			ErrorExpressionExpected = 105107,

			/// <summary>Error code 105108: "Expression type "{0}" cannot be assigned to type "{1}"."</summary>
			ExpressionTypeMismatch = 105108,

			/// <summary>Error code 105109: "Table expression expected."</summary>
			TableExpressionExpected = 105109,

			/// <summary>Error code 105110: "Presentation expression expected."</summary>
			PresentationExpressionExpected = 105110,

			/// <summary>Error code 105111: "Table or row expression expected."</summary>
			TableOrRowExpressionExpected = 105111,

			/// <summary>Error code 105112: "Row expression expected."</summary>
			RowExpressionExpected = 105112,

			/// <summary>Error code 105113: "Entry expression expected."</summary>
			EntryExpressionExpected = 105113,

			/// <summary>Error code 105114: "List expression expected."</summary>
			ListExpressionExpected = 105114,

			/// <summary>Error code 105115: "Cursor expression expected."</summary>
			CursorExpressionExpected = 105115,

			/// <summary>Error code 105116: "Type "{0}" is not compatible with type "{1}"."</summary>
			TableExpressionsNotCompatible = 105116,

			/// <summary>Error code 105117: "Scalar expression expected."</summary>
			ScalarExpressionExpected = 105117,
			
			/// <summary>Error code 105118: "Scalar type specifier expected."</summary>
			ScalarTypeExpected = 105118,

			/// <summary>Error code 105119: "Table identifier expected."</summary>
			TableIdentifierExpected = 105119,

			/// <summary>Error code 105120: "View identifier expected."</summary>
			ViewIdentifierExpected = 105120,

			/// <summary>Error code 105121: "Scalar type identifier expected."</summary>
			ScalarTypeIdentifierExpected = 105121,

			/// <summary>Error code 105122: "Constraint identifier expected."</summary>
			ConstraintIdentifierExpected = 105122,

			/// <summary>Error code 105123: "Reference identifier expected."</summary>
			ReferenceIdentifierExpected = 105123,

			/// <summary>Error code 105124: "Device identifier expected."</summary>
			DeviceIdentifierExpected = 105124,

			/// <summary>Error code 105125: "Server link identifier expected."</summary>
			ServerLinkIdentifierExpected = 105125,

			/// <summary>Error code 105126: "Server link expected."</summary>
			ServerLinkExpected = 105126,

			/// <summary>Error code 105127: "Join expression must be an equi-join."</summary>
			JoinMustBeEquiJoin = 105127,

			/// <summary>Error code 105128: "Column name expected."</summary>
			ColumnNameExpected = 105128,

			/// <summary>Error code 105129: "No catalog available in compiler "{0}"."</summary>
			NoCatalog = 105129,

			/// <summary>Error code 105130: "Table must have a key defined."</summary>
			KeyRequired = 105130,

			/// <summary>Error code 105131: "Row expression cannot be converted to the row type for this table selector: {0}."</summary>
			InvalidRowInTableSelector = 105131,

			/// <summary>Error code 105132: "Unable to determine a unique key for use in updating the given expression."</summary>
			UnableToDetermineUpdateKey = 105132,

			/// <summary>Error code 105133: "Conveyor class expected.  Given: {0}"</summary>
			ConveyorClassExpected = 105133,

			/// <summary>Error code 105134: "Unknown type specifier class "{0}"."</summary>
			UnknownTypeSpecifier = 105134,

			/// <summary>Error code 105135: "Reference "{0}" cannot be defined on object "{1}"."</summary>
			InvalidReferenceObject = 105135,

			/// <summary>Error code 105136: "Reference "{0}" must target a key of table "{1}"."</summary>
			ReferenceMustTargetKey = 105136,

			/// <summary>Error code 105137: "Reference "{0}" is invalid because column "{1}" of type "{2}" cannot reference column "{3}" of type "{4}" in table "{5}"."</summary>
			InvalidReferenceColumn = 105137,

			/// <summary>Error code 105138: "Device class expected.  Given: {0}"</summary>
			DeviceClassExpected = 105138,

			/// <summary>Error code 105139: "DeviceScalarType class expected.  Given {0}"</summary>
			DeviceScalarTypeClassExpected = 105139,

			/// <summary>Error code 105140: "DeviceOperator class expected.  Given {0}"</summary>
			DeviceOperatorClassExpected = 105140,

			/// <summary>Error code 105141: ""</summary>
			InvalidElementType = 105141,

			/// <summary>Error code 105142: "Unknown order reference "{0}"."</summary>
			UnknownOrder = 105142,

			/// <summary>Error code 105143: "No operator available to override "{0}({1})"."</summary>
			InvalidOverrideDirective = 105143,

			/// <summary>Error code 105144: "Host instructions may not be virtual calls "{0}({1})"."</summary>
			InvalidVirtualDirective = 105144,

			/// <summary>Error code 105145: "Unable to condense qualifier expression."</summary>
			UnableToCondenseQualifierExpression = 105145,

			/// <summary>Error code 105146: "Unable to collapse qualifier expression."</summary>
			UnableToCollapseQualifierExpression = 105146,

			/// <summary>Error code 105147: "Scalar type "{0}" does not contain a component named "{1}"."</summary>
			UnknownPropertyReference = 105147,

			/// <summary>Error code 105148: "Invalid qualifier "{0}"."</summary>
			InvalidQualifier = 105148,

			/// <summary>Error code 105149: "Unable to resolve operator reference "{0}" with signature "{1}"."</summary>
			OperatorNotFound = 105149,

			/// <summary>Error code 105150: "Column "{0}" cannot be extracted from an expression of type "{1}"."</summary>
			InvalidExtractionTarget = 105150,

			/// <summary>Error code 105151: "Target of an assignment must be a variable."</summary>
			InvalidAssignmentTarget = 105151,

			/// <summary>Error code 105152: "Invalid data modification target."</summary>
			InvalidUpdateTarget = 105152,

			/// <summary>Error code 105153: "Constant value cannot be passed to a var or out parameter."</summary>
			VariableReferenceRequired = 105153,

			/// <summary>Error code 105154: "Invalid entry selector in presentation selector."</summary>
			InvalidEntryInPresentationSelector = 105154,

			/// <summary>Error code 105155: "Invalid retrieve target."</summary>
			InvalidRetrieveTarget = 105155,

			/// <summary>Error code 105156: "Typeof cannot be called on object "{0}"."</summary>
			InvalidTypeOfTarget = 105156,

			/// <summary>Error code 105157: "Aggregate operator "{0}" cannot be called in this context."</summary>
			InvalidAggregateInvocation = 105157,

			/// <summary>Error code 105158: "Raise statement cannot be invoked without an argument outside of a catch block."</summary>
			InvalidRaiseContext = 105158,

			/// <summary>Error code 105159: "Ambiguous clear value for scalar type "{0}"."</summary>
			AmbiguousClearValue = 105159,

			/// <summary>Error code 105160: "Key column "{0}" not found."</summary>
			KeyColumnNotFound = 105160,

			/// <summary>Error code 105161: "Unable to determine a default conveyor for scalar type "{0}"."</summary>
			ConveyorRequired = 105161,

			/// <summary>Error code 105162: "Default selector cannot be provided for representation "{0}" on scalar type "{1}" because it does not have a single property."</summary>
			DefaultSelectorCannotBeProvided = 105162,

			/// <summary>Error code 105163: "Default read accessor cannot be provided for property "{0}" of representation "{1}" on scalar type "{2}" because the representation is not system-provided."</summary>
			DefaultReadAccessorCannotBeProvided = 105163,

			/// <summary>Error code 105164: "Default write accessor cannot be provided for property "{0}" of representation "{1}" on scalar type "{2}" because the representation is not system-provided."</summary>
			DefaultWriteAccessorCannotBeProvided = 105164,

			/// <summary>Error code 105165: "Constant data object "{0}" cannot be assigned a value."</summary>
			ConstantObjectCannotBeAssigned = 105165,

			/// <summary>Error code 105166: "Constant data object cannot be passed by reference to parameter "{0}"."</summary>
			ConstantObjectCannotBePassedByReference = 105166,

			/// <summary>Error code 105167: "Unknown identifier "{0}"."</summary>
			UnknownIdentifier = 105167,

			/// <summary>Error code 105168: "Unimplemented: virtual aggregate calls."</summary>
			UnimplementedVirtualAggregateCalls = 105168,
			
			/// <summary>Error code 105169: "Object "{0}" cannot be modified because it has dependent constraints."</summary>
			ObjectHasDependentConstraints = 105169,
			
			/// <summary>Error code 105170: "Object "{0}" is inherited and cannot be dropped."</summary>
			InheritedObject = 105170,
			
			/// <summary>Error code 105171: "Object "{0}" cannot be altered or dropped because it is referenced by at least the following objects: {1}."</summary>
			ObjectHasDependents = 105171,

			/// <summary>Error code 105172: "Object "{0}" cannot be modified because it is referenced by object "{1}"."</summary>
			ObjectIsReferenced = 105172,
			
			/// <summary>Error code 105173: "In operator cannot be invoked on values of type "{0}" and "{1}"."</summary>
			InvalidMembershipOperand = 105173,

			/// <summary>Error code 105174: "List type specifier expected."</summary>
			ListTypeExpected = 105174,
			
			/// <summary>Error code 105175: "Table type specifier expected."</summary>
			TableTypeExpected = 105175,
			
			/// <summary>Error code 105177: "Schema object "{0}" is not a valid source for events of type "{1}"."</summary>
			InvalidEventSource = 105177,
			
			/// <summary>Error code 105178: "Unknown event source specifier class "{0}"."</summary>
			UnknownEventSourceSpecifierClass = 105178,
			
			/// <summary>Error code 105179: "Event source "{0}" does not trigger events of type "{1}"."</summary>
			InvalidEventType = 105179,
			
			/// <summary>Error code 105180: "Operator "{0}" is incompatible with event signature "{1}"."</summary>
			IncompatibleEventHandler = 105180,
			
			/// <summary>Error code 105181: "No casting path was found from "{0}" to "{1}"."</summary>
			CastingPathNotFound = 105181,

			/// <summary>Error code 105182: "Physical casting operator from "{0}" to "{1}" not found.  This operator is required because the physical representations of the scalar types are different."</summary>
			PhysicalCastOperatorNotFound = 105182,
			
			/// <summary>Error code 105183: "Break or continue statements must appear within a looping construct."</summary>
			NoLoop = 105183,
			
			/// <summary>Error code 105184: "Cannot cast a value of type "{0}" to type "{1}"."</summary>
			InvalidCast = 105184,
			
			/// <summary>Error code 105185: "Constraint expression must be deterministic and have no side effects."</summary>
			InvalidConstraintExpression = 105185,
			
			/// <summary>Error code 105186: "Special value expression must be deterministic and have no side effects."</summary>
			InvalidSpecialExpression = 105186,
			
			/// <summary>Error code 105187: "Compare expression must be deterministic and have no side effects."</summary>
			InvalidCompareExpression = 105187,
			
			/// <summary>Error code 105188: "Identifier "{0}" was compiled as a variable reference, but is binding as a column reference."</summary>
			InvalidColumnBinding = 105188,
			
			/// <summary>Error code 105189: "Join specifier not allowed for a row join."</summary>
			InvalidRowJoin = 105189,
			
			/// <summary>Error code 105190: "Expression to be aggregated must be deterministic."</summary>
			InvalidAggregationSource = 105190,
			
			/// <summary>Error code 105191: "Table value passed by reference to parameter "{0}" must be deterministic."</summary>
			InvalidTableReferenceArgument = 105191,
			
			/// <summary>Error code 105192: "Cursor must be ordered to perform searchable calls."</summary>
			InvalidSearchableCall = 105192,
			
			/// <summary>Error code 105193: "All specification can only used to assign rights for specific catalog objects."</summary>
			InvalidAllSpecification = 105193,
			
			/// <summary>Error code 105194: "Only database-wide and transition constraints may reference global table variables."</summary>
			NonRemotableConstraintExpression = 105194,
			
			/// <summary>Error code 105195: "Unable to determine a default scalar type map for scalar type "{0}"."</summary>
			ScalarTypeMapRequired = 105195,
			
			/// <summary>Error code 105196: "Role identifier expected."</summary>
			RoleIdentifierExpected = 105196,
			
			/// <summary>Error code 105197: "Unknown grantee type "{0}"."</summary>
			UnknownGranteeType = 105197,
			
			/// <summary>Error code 105198: "Column extractor expression must reference a single column unless invoking an aggregate operator."</summary>
			InvalidColumnExtractorExpression = 105198,
			
			/// <summary>Error code 105199: "Row extractor expression must reference a table expression with at most one row.  Use a restriction or quota query to limit the number of rows in the source table expression."</summary>
			InvalidRowExtractorExpression = 105199,
			
			/// <summary>Error code 105200: "No default device specified."</summary>
			NoDefaultDevice = 105200,
			
			/// <summary>Error code 105201: "Errors occurred while attempting to resolve the default device name "{0}" for the current library."</summary>
			UnableToResolveDefaultDevice = 105201,
			
			/// <summary>Error code 105202: "The identifier "{0}" is ambiguous between the following identifiers: {1}."</summary>
			AmbiguousIdentifier = 105202,
			
			/// <summary>Error code 105203: "Internal Error: Optimization phase exceptions occurred."</summary>
			OptimizationError = 105203,

			/// <summary>Error code 105204: "Internal Error: Binding phase exceptions occurred."</summary>
			BindingError = 105204,
			
			/// <summary>Error code 105205: "Unable to resolve variable identifier "{0}".</summary>
			UnableToResolveIdentifier = 105205,
			
			/// <summary>Error code 105206: "The identifier "{0}" cannot be defined in this scope because it would hide the identifier "{1}"."</summary>
			CreatingHidingIdentifier = 105206,
			
			/// <summary>Error code 105207: "The identifier "{0}" cannot be defined in this scope because it would be hidden by the identifier "{1}"."</summary>
			CreatingHiddenIdentifier = 105207,
			
			/// <summary>Error code 105208: "Operator name "{0}" is ambiguous between the following operator names: {1}."</summary>
			AmbiguousOperatorName = 105208,
			
			/// <summary>Error code 105209: "No signature for operator "{0}" has ({1}) parameters."</summary>
			NoSignatureForParameterCardinality = 105209,
			
			/// <summary>Error code 105210: "The call "{0}{1}" is ambiguous between the following operators: {2}."</summary>
			AmbiguousOperatorCall = 105210,
			
			/// <summary>Error code 105211: "The closest matching signature to the call "{0}{1}" is "{2}" which has some invalid arguments."</summary>
			InvalidOperatorCall = 105211,
			
			/// <summary>Error code 105212: "Argument ({0}) cannot be converted from "{1}" to "{2}"."</summary>
			NoConversionForParameter = 105212,
			
			/// <summary>Error code 105213: "No signature for operator "{0}" matches the call signature "{1}" exactly."</summary>
			NoExactMatch = 105213,
			
			/// <summary>Error code 105214: "An object named "{0}" is already defined."</summary>
			CreatingDuplicateObjectName = 105214,
			
			/// <summary>Error code 105215: "The name "{0}" cannot be used because it would hide the name "{1}"."</summary>
			CreatingHidingObjectName = 105215,
			
			/// <summary>Error code 105216: "The name "{0}" cannot be used because it would be hidden by the name "{1}"."</summary>
			CreatingHiddenObjectName = 105216,
			
			/// <summary>Error code 105217: "The operator "{0}" already has a definition for the signature "{1}"."</summary>
			CreatingDuplicateSignature = 105217,
			
			/// <summary>Error code 105218: "The operator name "{0}" cannot be used because it would hide the operator name "{1}"."</summary>
			CreatingHidingOperatorName = 105218,
			
			/// <summary>Error code 105219: "The operator name "{0}" cannot be used because it would be hidden by the operator name "{1}"."</summary>
			CreatingHiddenOperatorName = 105219,
			
			/// <summary>Error code 105220: "Non-fatal compiler errors encountered."</summary>
			NonFatalErrors = 105220,
			
			/// <summary>Error code 105221: "Fatal compiler error encountered.  Compilation terminated."</summary>
			FatalErrors = 105221,
			
			/// <summary>Error code 105222: "Unable to construct a sort for data type "{0}"."</summary>
			UnableToConstructSort = 105222,
			
			/// <summary>Error code 105223: "Cannot determine a default special value for data type "{0}"."</summary>
			UnableToFindDefaultSpecial = 105223,

			/// <summary>Error code 105224: "Cannot convert a value of type "{0}" to a value of type "{1}"."</summary>
			NoConversion = 105224,
			
			/// <summary>Error code 105225: "Cannot convert values in column "{0}" of type "{1}" to values of type "{2}"."</summary>
			NoColumnConversion = 105225,
			
			/// <summary>Error code 105226: "The conversion from values of type "{0}" to type "{1}" is ambiguous among the following conversion paths: {2}."</summary>
			AmbiguousConversion = 105226,
			
			/// <summary>Error code 105227: "Narrowing conversion used to convert a value of type "{0}" to type "{1}"."</summary>
			NarrowingConversion = 105227,
			
			/// <summary>Error code 105228: "The operator "{0}" is not attached to the "{1}" event of object "{2}"."</summary>
			OperatorNotAttachedToObjectEvent = 105228,
			
			/// <summary>Error code 105229: "The operator "{0}" is not attached to the "{1}" event of column "{2}" in table "{3}"."</summary>
			OperatorNotAttachedToColumnEvent = 105229,

			/// <summary>Error code 105230: "Unable to extract row from data type "{0}"."</summary>
			UnableToExtractRow = 105230,

			/// <summary>Error code 105231: "Unable to extract entry from data type "{0}"."</summary>
			UnableToExtractEntry = 105231,
			
			/// <summary>Error code 105232: "Possibly incorrect use of expression as a statement."</summary>
			ExpressionStatement = 105232,
			
			/// <summary>Error code 105233: "Unable to determine data type for variable declaration."</summary>
			TypeSpecifierExpected = 105233,
			
			/// <summary>Error code 105234: "DDL statements must be dynamically executed within an operator block.  Use System.Execute to perform this operation."</summary>
			DDLStatementInOperator = 105234,
			
			/// <summary>Error code 105235: "A default selector and accessors for representation "{0}" in scalar type "{1}" cannot be provided because a conveyor has been specified for the scalar type."</summary>
			InvalidConveyorForCompoundScalar = 105235,
			
			/// <summary>Error code 105236: "A default selector and accessors for representation "{0}" in scalar type "{1}" cannot be provided because another representation has already determined the native representation for the scalar type."</summary>
			MultipleSystemProvidedRepresentations = 105236,
			
			/// <summary>Error code 105237: "Device "{0}" already has a type map for type "{1}"."</summary>
			DuplicateDeviceScalarType = 105237,
			
			/// <summary>Error code 105238: "Device "{0}" already has an operator map for operator "{1}" with signature "{2}"."</summary>
			DuplicateDeviceOperator = 105238,
			
			/// <summary>Error code 105239: "Unable to determine a default device operator class for operator "{0}" in device "{1}"."</summary>
			DeviceOperatorClassRequired = 105239,
			
			/// <summary>Error code 105240: "Selector for representation "{0}" of type "{1}" is invalid because selectors must be deterministic, have no side effects, and be evaluable in isolation."</summary>
			InvalidSelector = 105240,
			
			/// <summary>Error code 105241: "Read accessor for property "{0}" of representation "{1}" of type "{2}" is invalid because read accessors must be deterministic, have no side effects and be evaluable in isolation."</summary>
			InvalidReadAccessor = 105241,
			
			/// <summary>Error code 105242: "Write accessor for property "{0}" of representation "{1}" of type "{2}" is invalid because write accessors must be deterministic and be evaluable in isolation."</summary>
			InvalidWriteAccessor = 105242,
			
			/// <summary>Error code 105243: "Device "{0}" did not support the plan "{1}" for the following reasons:\r\n{2}"</summary>
			UnsupportedPlan = 105243,

			/// <summary>Error code 105244: "Table expression of type "{0}" contains common column names with table expression of type "{1}" and would result in duplicate column names if used in a times expression."</summary>
			TableExpressionsNotProductCompatible = 105244,
			
			/// <summary>Error code 105245: "Table expression of type "{0}" does not have common column names with table expression of type "{1}", resulting in a possibly incorrect times expression."</summary>
			PossiblyIncorrectProductExpression = 105245,
			
			/// <summary>Error code 105246: "Reference "{0}" must target the same number of columns in both tables."</summary>
			InvalidReferenceColumnCount = 105246,
			
			/// <summary>Error code 105247: "Table or view identifier expected."</summary>
			TableVarIdentifierExpected = 105247,
			
			/// <summary>Error code 105248: "Expression expected."</summary>
			ExpressionExpected = 105248,
			
			/// <summary>Error code 105249: "Target row type does not have a column named "{0}"."</summary>
			TargetRowTypeMissingColumn = 105249,
			
			/// <summary>Error code 105250: "Source row type does not have a column named "{0}"."</summary>
			SourceRowTypeMissingColumn = 105250,

			/// <summary>Error code 105251: "Target table type does not have a column named "{0}"."</summary>
			TargetTableTypeMissingColumn = 105251,
			
			/// <summary>Error code 105252: "Source table type does not have a column named "{0}"."</summary>
			SourceTableTypeMissingColumn = 105252,
			
			/// <summary>Error code 105253: "Variable declarations after a cursor definition may affect cursor execution. Ensure that all variable declarations appear before opening any cursors."</summary>
			CursorTypeVariableInScope = 105253,
			
			/// <summary>Error code 105254: "Outer joins cannot be natural, they must have a condition specified."</summary>
			OuterJoinMustBeConditioned = 105254,
			
			/// <summary>Error code 105255: "Outer joins cannot be many-to-many, because the resulting set would have only sparse keys."</summary>
			InvalidOuterJoin = 105255,
			
			/// <summary>Error code 105256: "Table selector cannot be evaluated because the key includes columns that have no relative comparison operators defined."</summary>
			InvalidTableSelector = 105256,
			
			/// <summary>Error code 105257: "No signature for operator "{0}" matches the call signature "{1}"."</summary>
			NoMatch = 105257,
			
			/// <summary>Error code 105258: "Restriction condition is invalid because it contains non-repeatable operator invocations."</summary>
			InvalidRestrictionCondition = 105258,
			
			/// <summary>Error code 105259: "Table indexer expression is ambiguous. Specify a key to be used using the by clause."</summary>
			AmbiguousTableIndexerKey = 105259,
			
			/// <summary>Error code 105260: "Table indexer expression is invalid because no keys have the required number and type of columns."</summary>
			UnresolvedTableIndexerKey = 105260,
			
			/// <summary>Error code 105261: "Table indexer expression is invalid because the resolved key does not have the same number of columns as the indexer expression."</summary>
			InvalidTableIndexerKey = 105261,
			
			/// <summary>Error code 105262: "Row extraction using the from keyword has been deprecated and will not be supported in future releases. Use a table indexer expression ([]) to extract row values."</summary>
			RowExtractorDeprecated = 105262,
			
			/// <summary>Error code 105263: "Column extraction using the from keyword has been deprecated and will not be supported in future releases. Use a qualifier expression (.) to extract column values."</summary>
			ColumnExtractorDeprecated = 105263,
			
			/// <summary>Error code 105264: "Table indexer expression has too many terms for efficient implicit key resolution. Specify a key to be used using the by clause."</summary>
			TooManyTermsForImplicitTableIndexer = 105264,
			
			/// <summary>Error code 105265: "The row variable name is only valid for table-based foreach statements."</summary>
			ForEachVariableNameRequired = 105265,
			
			/// <summary>Error code 105266: "Foreach statement may only operator on list or table values."</summary>
			InvalidForEachStatement = 105266,
			
			/// <summary>Error code 105267: "Column reference not allowed in this context."</summary>
			InvalidColumnReference = 105267,
			
			/// <summary>Error code 105268: "{0}"</summary>
			CompilerMessage = 105268,
			
			/// <summary>Error code 105269: "Explode expression must specify an order by clause if it includes a level or sequence column."</summary>
			InvalidExplodeExpression = 105269,
			
			/// <summary>Error code 105270: "Order specified for explode expression must include some key of the source expression."</summary>
			InvalidExplodeExpressionOrder = 105269,
			
			/// <summary>Error code 105271: "Aggregate operator "{0}" is order-dependent. Use the order by clause to specify an ordering for this operation."</summary>
			InvalidOrderDependentAggregateInvocation = 105271,
			
			/// <summary>Error code 105272: "Order specified for order-dependent aggregate operator invocation "{0}" must include some key of the source expression."</summary>
			InvalidOrderDependentAggregateInvocationOrder = 105272,
			
			/// <summary>Error code 105273: "Argument ({0}) must be literal (evaluable at compile-time)."</summary>
			LiteralArgumentRequired = 105273,
			
			/// <summary>Error code 105274: "Operator "{0}" has been deprecated and will no longer be supported in a future version."</summary>
			DeprecatedOperator = 105274,

			/// <summary>Error code 105275: "Source expression has multiple keys with columns of the same types, resulting in a potentially ambiguous implicit table indexer expression. Specify a key to be used using the by clause."</summary>
			PotentiallyAmbiguousImplicitTableIndexer = 105275,

			/// <summary>Error code 105276: "Transition constraint expression must be repeatable and have no side effects."</summary>
			InvalidTransitionConstraintExpression = 105276,

			/// <summary>Error code 105277: "Table expression of type "{0}" does not have common column names with table expression of type "{1}", resulting in a possibly incorrect having or without expression."</summary>
			PossiblyIncorrectSemiTableExpression = 105277,
			
			/// <summary>Error code 105278: "Indexer expression must contain one and only one indexing expression."</summary>
			InvalidIndexerExpression = 105278,
			
			/// <summary>Error code 105279: "The tag "{0}" has been deprecated. Use tag "{1}" instead. For more information, see the Tags Reference in the Dataphor Developer's Guide."</summary>
			DeprecatedTag = 105279,
			
			/// <summary>Error code 105280: "Row type specifier expected."</summary>
			RowTypeExpected = 105280,
			
			/// <summary>Error code 105281: "Internal error: Unexpected exception encountered during compilation."</summary>
			InternalError = 105281,
			
			/// <summary>Error code 105282: "The clear referential action cannot be used for source column "{0}" because it is marked not nil and the type of the column does not have an unambiguous special defined."</summary>
			CannotClearSourceColumn = 105282,

			/// <summary>Error code 105283: "The operator "{0}" is already attached to the "{1}" event of object "{2}"."</summary>
			OperatorAlreadyAttachedToObjectEvent = 105283,
			
			/// <summary>Error code 105284: "The operator "{0}" is already attached to the "{1}" event of column "{2}" in table "{3}"."</summary>
			OperatorAlreadyAttachedToColumnEvent = 105283,
			
			/// <summary>Error code 105285: "Errors occurred attempting to compile custom error message for constraint "{0}"."</summary>
			InvalidCustomConstraintMessage = 105285,
			
			/// <summary>Error code 105286: "Restriction is not sargable because the argument for column "{0}" could not be converted from type "{1}" to type "{2}", resulting in a potential reduction in performance."</summary>
			CouldNotConvertSargableArgument = 105286,
			
			/// <summary>Error code 105287: "Custom message may only reference global table variables for database-wide and transition constraints."</summary>
			NonRemotableCustomConstraintMessage = 105287,

			/// <summary>Error code 105288: "Conveyor for type "{0}" is ambiguous between "{1}" and "{2}"."</summary>
			AmbiguousConveyor = 105288,

			/// <summary>Error code 105289: "Column "{0}" has already been renamed."</summary>
			DuplicateRenameColumn = 105289
		}
		
		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Compiling.CompilerException", typeof(CompilerException).Assembly);

		// Constructors
		public CompilerException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) 
		{
			_errorLevel = errorLevel;
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel) : base(_resourceManager, (int)errorCode, severity, null, null) 
		{
			_errorLevel = errorLevel;
		}
		
		public CompilerException(Codes errorCode, Statement statement) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, Statement statement) : base(_resourceManager, (int)errorCode, severity, null, null) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, Statement statement) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, Statement statement) : base(_resourceManager, (int)errorCode, severity, null, null) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) 
		{
			_errorLevel = errorLevel;
		}

		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) 
		{
			_errorLevel = errorLevel;
		}

		public CompilerException(Codes errorCode, Statement statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, Statement statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, Statement statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, Statement statement, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) 
		{
			_errorLevel = errorLevel;
		}

		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) 
		{
			_errorLevel = errorLevel;
		}

		public CompilerException(Codes errorCode, Statement statement, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, Statement statement, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, Statement statement, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, Statement statement, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue)
		{
			_errorLevel = errorLevel;
		}

		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue)
		{
			_errorLevel = errorLevel;
		}

		public CompilerException(Codes errorCode, Statement statement, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, Statement statement, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) 
		{
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, CompilerErrorLevel errorLevel, Statement statement, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}
		
		public CompilerException(Codes errorCode, ErrorSeverity severity, CompilerErrorLevel errorLevel, Statement statement, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) 
		{
			_errorLevel = errorLevel;
			if (statement != null)
			{
				_line = statement.Line;
				_linePos = statement.LinePos;
			}
		}

		public CompilerException(ErrorSeverity severity, int code, string message, string details, string serverContext, CompilerErrorLevel errorLevel, int line, int linePos, DataphorException innerException)
			: base(severity, code, message, details, serverContext, innerException)
		{
			_errorLevel = errorLevel;
			_line = line;
			_linePos = linePos;
		}

		private CompilerErrorLevel _errorLevel = CompilerErrorLevel.NonFatal;
		public CompilerErrorLevel ErrorLevel { get { return _errorLevel; } }

		private int _line = -1;
		public int Line 
		{ 
			get { return _line; } 
			set { _line = value; }
		}
			
		private int _linePos = -1;
		public int LinePos 
		{ 
			get { return _linePos; } 
			set { _linePos = value; }
		}

		private string _locator;
		public string Locator
		{
			get { return _locator; }
			set { _locator = value; }
		}
	}
	
	#if USETYPEDLIST
	public class CompilerMessages : TypedList
	{
		public CompilerMessages() : base(typeof(Exception)) {}
		
		public new Exception this[int AIndex]
		{
			get { return (Exception)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	
	#else
	public class CompilerMessages : ValidatingBaseList<Exception>
	{
	#endif
		// Indicates the stored error flags are out-of-date.
		private bool _flagsReset;
				
		private bool _hasFatalErrors;
		/// <summary>Returns true if there are any fatal or non-compiler errors.</summary>		
		public bool HasFatalErrors 
		{ 
			get 
			{ 
				if (_flagsReset)
				{
					GetFlags();
					_flagsReset = false;
				}
				return _hasFatalErrors; 
			} 
		}
		
		private bool _hasErrors;
		/// <summary>Returns true if there are any fatal, non-fatal or non-compiler errors.</summary>
		public bool HasErrors 
		{ 
			get 
			{ 
				if (_flagsReset)
				{
					GetFlags();
					_flagsReset = false;
				}
				return _hasErrors; 
			} 
		}

		public Exception FirstError
		{
			get 
			{
				for (int index = 0; index < Count; index++)
					if ((!(this[index] is CompilerException)) || ((((CompilerException)this[index]).ErrorLevel == CompilerErrorLevel.Fatal) || (((CompilerException)this[index]).ErrorLevel == CompilerErrorLevel.NonFatal)))
						return this[index];
				return null;
			}
		}

		#if USETYPEDLIST
		protected override void Adding(object AValue, int AIndex)
		#else
		protected override void Adding(Exception tempValue, int index)
		#endif
		{
			if (tempValue is CompilerException)
			{
				switch (((CompilerException)tempValue).ErrorLevel)
				{
					case CompilerErrorLevel.Fatal : _hasFatalErrors = true; _hasErrors = true; break;
					case CompilerErrorLevel.NonFatal : _hasErrors = true; break;
				}
			}
			else if (tempValue is SyntaxException)
				_hasErrors = true;
			else
			{
				_hasFatalErrors = true;
				_hasErrors = true;
			}
			//base.Adding(AValue, AIndex);
		}

		#if USETYPEDLIST
		protected override void Removing(object AValue, int AIndex)
		#else
		protected override void Removing(Exception tempValue, int index)
		#endif
		{
			_flagsReset = true;
			//base.Removing(AValue, AIndex);
		}
		
		private void GetFlags()
		{
			_hasFatalErrors = GetHasFatalErrors();
			_hasErrors = GetHasErrors();
		}
		
		private bool GetHasFatalErrors()
		{
			for (int index = 0; index < Count; index++)
			{
				if (this[index] is SyntaxException)
					continue;
				if ((!(this[index] is CompilerException)) || (((CompilerException)this[index]).ErrorLevel == CompilerErrorLevel.Fatal))
					return true;
			}
			return false;
		}

		private bool GetHasErrors()
		{
			for (int index = 0; index < Count; index++)
				if ((!(this[index] is CompilerException)) || ((((CompilerException)this[index]).ErrorLevel == CompilerErrorLevel.Fatal) || (((CompilerException)this[index]).ErrorLevel == CompilerErrorLevel.NonFatal)))
					return true;
			return false;
		}
		
		public string ToString(CompilerErrorLevel level)
		{
			StringBuilder builder = new StringBuilder();
			foreach (Exception exception in this)
				if (!(exception is CompilerException) || (((CompilerException)exception).ErrorLevel <= level))
					ExceptionUtility.AppendMessage(builder, 0, exception);
			return builder.ToString();
		}
		
		public override string ToString()
		{
			return ToString(CompilerErrorLevel.NonFatal);
		}

		/// <summary> Sets the locator for all locator exceptions that don't have one. </summary>
		/// <remarks> Note: Doesn't update the offsets. </remarks>
		public void SetLocator(DebugLocator locator)
		{
			foreach (Exception exception in this)
				exception.SetLocator(locator);
		}
	}
}
