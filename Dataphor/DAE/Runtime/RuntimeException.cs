/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Resources;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public class RuntimeException : DAEException, ILocatorException
	{
		public enum Codes 
		{
			/// <summary>Error code 104100: "Unable to obtain the requested lock on catalog object "{0}"."</summary>
			UnableToLockObject = 104100,
		
			/// <summary>Error code 104101: "Only the system administrator can perform this task."</summary>
			AdminUserTask = 104101,

			/// <summary>Error code 104102: "Only the system user can perform this task."</summary>
			SystemUserTask = 104102,

			/// <summary>Error code 104103: "Stack empty."</summary>
			StackEmpty = 104103,

			/// <summary>Error code 104104: "Stack index out of range ({0})."</summary>
			InvalidStackIndex = 104104,

			/// <summary>Error code 104105: "A parameter named "{0}" was not found."</summary>
			DataParamNotFound = 104105,

			/// <summary>Error code 104106: "Parameter stack empty."</summary>
			ParamsEmpty = 104106,

			/// <summary>Error code 104107: "Semaphore cannot be released because it has not been acquired."</summary>
			NotAcquired = 104107,

			/// <summary>Error code 104108: "Semaphore cannot be requested in "{0}" mode."</summary>
			InvalidLockMode = 104108,

			/// <summary>Error code 104109: "Cursor "{0}" not found."</summary>			
			CursorNotFound = 104109,

			/// <summary>Error code 104110: "Unable to construct index key."</summary>			
			UnableToConstructIndexKey = 104110,

			/// <summary>Error code 104111: "Internal Error: Native table value for table variable "{0}" not found."</summary>			
			NativeTableNotFound = 104111,

			/// <summary>Error code 104112: "Current list size exceeds new setting for maximum number of cursors."</summary>			
			CurrentListSizeExceedsNewSetting = 104112,

			/// <summary>Error code 104113: "New value violates minimum table count for a browse node."</summary>			
			NewValueViolatesMinimumTableCount = 104113,        

			/// <summary>Error code 104114: "Browse table "{0}" has no top table available."</summary>			
			NoTopTable = 104114,

			/// <summary>Error code 104115: "Search argument must be a subset of the order columns."</summary>			
			InvalidSearchArgument = 104115,

			/// <summary>Error code 104116: "Internal Error: AggregateTable.TargetScan does not contain newly inserted row."</summary>			
			TargetScanDoesNotContainRow = 104116,

			/// <summary>Error code 104117: "Unimplemented: OrderNode.SequenceColumn."</summary>			
			UnimplementedOrderNode = 104117,

			/// <summary>Error code 104118: "Unimplemented: BrowseTable.InternalRowCount()."</summary>			
			UnimplementedInternalRowCount = 104118,
				
			/// <summary>Error code 104119: "Internal Error: ExplodeTable.InternalNext: Could not locate newly inserted row"</summary>
			NewRowNotFound = 104119,

			/// <summary>Error code 104120: "Internal Error: Newly inserted row not found in UnionTable.InternalNext()."</summary>
			UnionTableNewRowNotFound = 104120,

			/// <summary>Error code 104121: "Table "{0}" must be open to perform this operation."</summary>
			TableInactive = 104121,

			/// <summary>Error code 104122: "Table "{0}" must be closed to perform this operation."</summary>
			TableActive = 104122,

			/// <summary>Error code 104123: "Table is not backwards navigable."</summary>
			NotBackwardsNavigable = 104123,

			/// <summary>Error code 104124: "Table is not bookmarkable."</summary>
			NotBookmarkable = 104124,

			/// <summary>Error code 104125: "Table is not searchable."</summary>
			NotSearchable = 104125,

			/// <summary>Error code 104126: "Table is not countable."</summary>
			NotCountable = 104126,

			/// <summary>Error code 104127: "Table has no current row to perform this operation."</summary>
			NoCurrentRow = 104127,

			/// <summary>Error code 104128: "Table is not updateable."</summary>
			NotUpdateable = 104128,

			/// <summary>Error code 104129: "Table is not truncateable."</summary>
			NotTruncateable = 104129,

			/// <summary>Error code 104130: "Table node required to create a Table cursor."</summary>
			TableNodeRequired = 104130,

			/// <summary>Error code 104131: ""{0}" not supported."</summary>
			CapabilityNotSupported = 104131,

			/// <summary>Error code 104132: "Minimum string value cannot be determined for an empty set."</summary>
			InvalidMinInvocation = 104132,

			/// <summary>Error code 104133: "Maximum string value cannot be determined for an empty set."</summary>
			InvalidMaxInvocation = 104133,

			/// <summary>Error code 104134: "Internal Error: HeapReferenceNode should never be executed."</summary>
			HeapRefNodeExecuted = 104134,

			/// <summary>Error code 104135: "Internal Error: CatalogReferenceNode should never be executed."</summary>
			CatalogRefNodeExecuted = 104135,

			/// <summary>Error code 104136: "Internal Error: PropertyReferenceNode should never be executed."</summary>
			PropertyRefNodeExecuted = 104136,

			/// <summary>Error code 104137: "Device "{0}" does not support retrieval for the table variable "{1}"."</summary>
			NoSupportingDevice = 104137,

			/// <summary>Error code 104138: "Statement extraction not supported for plan node "{0}"."</summary>
			StatementNotSupported = 104138,

			/// <summary>Error code 104139: "Instruction execution not supported for plan node "{0}"."</summary>
			InstructionExecuteNotSupported = 104139,

			/// <summary>Error code 104140: "Row value cannot be extracted from an empty table."</summary>
			RowTableEmpty = 104140,

			/// <summary>Error code 104141: "Column value cannot be extracted from an empty table."</summary>
			ColumnTableEmpty = 104141,

			/// <summary>Error code 104142: "Column value cannot be extracted from an empty presentation."</summary>
			PresentationEmpty = 104142,
			
			/// <summary>Error code 104143: "The new row does not meet the specified filter criteria."</summary>
			NewRowViolatesRestrictPredicate = 104143,

			/// <summary>Error code 104144: "Unable to resolve aggregate operator reference "{0}"."</summary>
			AggregateOperatorNotFound = 104144,		

			/// <summary>Error code 104145: "Internal Error: Browse variant not found: Origin index: ({0}), Forward: ({1}), Inclusive: ({2})."</summary>
			BrowseVariantNotFound = 104145,

			/// <summary>Error code 104146: "New row violates difference predicate."</summary>
			RowViolatesDifferencePredicate = 104146,

			/// <summary>Error code 104147: "New row violates join predicate."</summary>
			RowViolatesJoinPredicate = 104147,

			/// <summary>Error code 104148: "Table expressions contain common column names and cannot be used in a times expression."</summary>
			TableExpressionsNotProductCompatible = 104148,

			/// <summary>Error code 104149: "Table value expected."</summary>
			TableExpected = 104149,

			/// <summary>Error code 104150: "Violation of catalog constraint "{0}"."</summary>
			CatalogConstraintViolation = 104150,

			/// <summary>Error code 104151: "Violation of insert transition for constraint "{0}" defined on table "{1}"."</summary>
			InsertConstraintViolation = 104151,

			/// <summary>Error code 104152: "Violation of update transition for constraint "{0}" defined on table "{1}"."</summary>
			UpdateConstraintViolation = 104152,

			/// <summary>Error code 104153: "Violation of delete transition for constraint "{0}" defined on table "{1}"."</summary>
			DeleteConstraintViolation = 104153,

			/// <summary>Error code 104154: "Violation of row constraint "{0}" defined on table "{1}"."</summary>
			RowConstraintViolation = 104154,

			/// <summary>Error code 104155: "Violation of constraint "{0}" defined on scalar type "{1}"."</summary>
			TypeConstraintViolation = 104155,

			/// <summary>Error code 104156: "Value required for column "{0}"."</summary>
			ColumnValueRequired = 104156,

			/// <summary>Error code 104157: "Optimistic concurrency check failed.  Row could not be located for concurrency check."</summary>
			OptimisticConcurrencyCheckRowNotFound = 104157,

			/// <summary>Error code 104158: "Unable to perform insert."</summary>
			UnableToPerformInsert = 104158,

			/// <summary>Error code 104159: "Unable to perform update."</summary>
			UnableToPerformUpdate = 104159,

			/// <summary>Error code 104160: "Unable to perform delete."</summary>
			UnableToPerformDelete = 104160,

			/// <summary>Error code 104161: "Table node internal execute cannot be invoked."</summary>
			TableNodeInternalExecute = 104161,

			/// <summary>Error code 104162: "Device name expected in Reconcile operator."</summary>
			DeviceNameExpected = 104162,

			/// <summary>Error code 104163: "DDL node cannot be cloned."</summary>
			DDLNodeCannotBeCloned = 104116,

			/// <summary>Error code 104164: "Constraint "{0}" cannot be created because data exists which violates the constraint."</summary>
			ConstraintViolation = 104164,

			/// <summary>Error code 104165: "Reference "{0}" cannot be created because rows exist which violate the constraint."</summary>
			ReferenceConstraintViolation = 104165,

			/// <summary>Error code 104166: "{0} cannot be created in this release."</summary>
			UnimplementedCreateCommand = 104166,

			/// <summary>Error code 104167: "{0} cannot be altered in this release."</summary>
			UnimplementedAlterCommand = 104167,

			/// <summary>Error code 104168: "{0} cannot be dropped in this release."</summary>
			UnimplementedDropCommand = 104168,

			/// <summary>Error code 104169: "Column "{0}" in table "{1}" already has a default defined."</summary>
			DefaultDefined = 104169,

			/// <summary>Error code 104170: "Column "{0}" in table "{1}" does not have a default defined."</summary>
			DefaultNotDefined = 104170,

			/// <summary>Error code 104171: "Invalid alter statement."</summary>
			InvalidAlterTableVarStatement = 104171,

			/// <summary>Error code 104172: "Object "{0}" is not a table variable."</summary>
			ObjectNotTableVar = 104172,

			/// <summary>Error code 104173: "Object "{0}" is not a table."</summary>
			ObjectNotTable = 104173,

			/// <summary>Error code 104174: "Object "{0}" is not a view."</summary>
			ObjectNotView = 104174,

			/// <summary>Error code 104175: "Object "{0}" is not a scalar type."</summary>
			ObjectNotScalarType = 104175,

			/// <summary>Error code 104176: "Object "{0}" is not an aggregate operator."</summary>
			ObjectNotAggregateOperator = 104176,

			/// <summary>Error code 104177: "Object "{0}" is not a constraint."</summary>
			ObjectNotConstraint = 104177,

			/// <summary>Error code 104178: </summary>
			ObjectNotReference = 104178,

			/// <summary>Error code 104179: "Object "{0}" is not a server link."</summary>
			ObjectNotServer = 104179,

			/// <summary>Error code 104180: "Object "{0}" is not a device."</summary>
			ObjectNotDevice = 104180,

			/// <summary>Error code 104182: "Object "{0}" is a system object and cannot be dropped."</summary>
			ObjectIsSystem = 104182,

			/// <summary>Error code 104183: "Device "{0}" is in use and cannot be dropped."</summary>
			DeviceInUse = 104183,

			/// <summary>Error code 104184: "Statement extraction not supported for values of type "{0}"."</summary>
			UnsupportedValueType = 104184,

			/// <summary>Error code 104185: "Internal: DerivedTableVarNode source must be a TableNode descendent."</summary>
			InternalMustBeTableNode = 104185,

			/// <summary>Error code 104186: "Internal Error: Unknown table variable class."</summary>
			InternalUnknownTableVariableClass = 104186,
				
			/// <summary>Error code 104187: "Set is not ordered."</summary>
			SetNotOrdered = 104187,		

			/// <summary>Error code 104188: "Set should be BOF."</summary>
			SetShouldBeBOF = 104188,

			// <summary>Error code 104190: "No value available."</summary>
			//NoValueAvailable = 104190,
			
			/// <summary>Error code 104191: "Violation of constraint "{0}".{1}"</summary>
			DataConstraintViolation = 104191,
			
			/// <summary>Error code 104192: "Generators table must be of type "table { ID : string, NextKey : integer, key { ID } }"."</summary>
			InvalidGeneratorsTable = 104192,
			
			/// <summary>Error code 104193: "Table "{0}" is functioning as the generators table and cannot be dropped."</summary>
			TableIsGenerators = 104193,
			
			/// <summary>Error code 104194: "Internal Error: {0}."</summary>
			InternalError = 104194,
			
			/// <summary>Error code 104195: "Cannot cast a value of type "{0}" to type "{1}"."</summary>
			InvalidCast = 104195,
			
			/// <summary>Error code 104196: "Optimistic concurrency check failed.  Row has been updated by another user."</summary>
			OptimisticConcurrencyCheckFailed = 104196,
			
			/// <summary>Error code 104197: "Rows being joined have common columns, and do not have common values for those columns."</summary>
			InvalidRowJoin = 104197,
			
			/// <summary>Error code 104198: "Cursor value cannot be copied."</summary>
			CursorValueCannotBeCopied = 104198,

			/// <summary>Error code 104200: "Maximum row count exceeded in row manager."</summary>
			RowManagerOverflow = 104200,

			/// <summary>Error code 104201: "Maximum scalar count exceeded in scalar manager."</summary>
			ScalarManagerOverflow = 104201,
			
			/// <summary>Error code 104202: "Timed out waiting for semaphore."</summary>
			SemaphoreTimeout = 104202,
			
			/// <summary>Error code 104203: "Timed out waiting for {0} lock on "{1}" owned by "{2}"."</summary>
			LockTimeout = 104203,
			
			/// <summary>Error code 104205: "Version number components must be greater than or equal to -1."</summary>
			InvalidVersionNumberComponent = 104205,
			
			/// <summary>Error code 104206: "Version number components must be defined in order (major, minor, revision, build).</summary>
			InvalidVersionNumber = 104206,

			/// <summary>Error code 104207: "Violation of column constraint "{0}" on column "{1}" of table "{2}"."</summary>
			ColumnConstraintViolation = 104207,
			
			/// <summary>Error code 104208: "Exceptions occurred while attempting to validate constraint "{0}" defined on scalar type "{1}"."</summary>
			ErrorValidatingTypeConstraint = 104208,

			/// <summary>Error code 104209: "Exceptions occurred while attempting to validate constraint "{0}" defined on column "{1}" in table "{2}"."</summary>
			ErrorValidatingColumnConstraint = 104209,

			/// <summary>Error code 104210: "Exceptions occurred while attempting to validate row constraint "{0}" defined on table "{1}"."</summary>
			ErrorValidatingRowConstraint = 104210,

			/// <summary>Error code 104211: "Exceptions occurred while attempting to validate insert transition for constraint "{0}" defined on table "{1}"."</summary>
			ErrorValidatingInsertConstraint = 104211,

			/// <summary>Error code 104212: "Exceptions occurred while attempting to validate update transition for constraint "{0}" defined on table "{1}"."</summary>
			ErrorValidatingUpdateConstraint = 104212,

			/// <summary>Error code 104213: "Exceptions occurred while attempting to validate delete transition for constraint "{0}" defined on table "{1}"."</summary>
			ErrorValidatingDeleteConstraint = 104213,

			/// <summary>Error code 104214: "Exceptions occurred while attempting to validate catalog constraint "{0}"."</summary>
			ErrorValidatingCatalogConstraint = 104214,
			
			/// <summary>Error code 104215: "Exceptions occurred while attempting to validate constraint "{0}"."</summary>
			ErrorValidatingConstraint = 104215,
			
			/// <summary>Error code 104216: "Maximum stack depth ({0}) has been exceeded."</summary>
			StackOverflow = 104216,
			
			/// <summary>Error code 104217: "Exceptions occurred while attempting to run the registration script for library "{0}"."</summary>
			LibraryRegistrationFailed = 104217,
			
			/// <summary>Error code 104218: "Exceptions occurred while attempting to rollback library registration for library "{0}": {1}"</summary>
			LibraryRollbackFailed = 104218,
			
			/// <summary>Error code 104219: "Column "{0}" in table "{1}" cannot be marked non-nil because rows exist which have no values for this column."</summary>
			NonNilConstraintViolation = 104219,
			
			/// <summary>Error code 104220: "Invalid value type "{0}"."</summary>
			InvalidValueType = 104220,
			
			/// <summary>Error code 104221: "Unable to convert a value of type "{0}" to a value of type "{1}"."</summary>
			UnableToConvertValue = 104221,
			
			/// <summary>Error code 104222: "Unable to provide stream access for a value of type "{0}"."</summary>
			UnableToProvideStreamAccess = 104222,
			
			/// <summary>Error code 104223: "Unable to provide cursor access for a value of type "{0}"."</summary>
			UnableToProvideCursorAccess = 104223,
			
			/// <summary>Error code 104224: "Internal Error: Unprepared call of method WriteToPhysical."</summary>
			UnpreparedWriteToPhysicalCall = 104224,
			
			/// <summary>Error code 104225: "Representation "{0}" of scalar type "{1}" is read only."</summary>
			ReadOnlyRepresentation = 104225,
			
			/// <summary>Error code 104226: "Scalar type "{0}" already has a default defined."</summary>
			ScalarTypeDefaultDefined = 104226,

			/// <summary>Error code 104227: "Scalar type "{0}" does not have a default defined."</summary>
			ScalarTypeDefaultNotDefined = 104227,
			
			/// <summary>Error code 104228: "No supporting device for modification of the expression "{0}"."</summary>
			NoSupportingModificationDevice = 104228,
			
			/// <summary>Error code 104229: "Runtime error: {0}"</summary>
			RuntimeError = 104229,
			
			/// <summary>Error code 104230: "A nil was encountered where a value was expected."</summary>
			NilEncountered = 104230,
			
			/// <summary>Error code 104231: "A value was encountered where a nil was expected."</summary>
			ValueEncountered = 104231,
			
			/// <summary>Error code 104232: "{0}"</summary>
			GeneralConstraintViolation = 104232,
			
			/// <summary>Error code 104233: "Stated characteristics ({0}) do not match actual characteristics ({1})."</summary>
			InvalidCharacteristicOverride = 104233,
			
			/// <summary>Error code 104234: "New row violates quota predicate."</summary>
			RowViolatesQuotaPredicate = 104234,
			
			/// <summary>Error code 104235: "New row violates having predicate."</summary>
			RowViolatesHavingPredicate = 104235,
			
			/// <summary>Error code 104236: "New row violates without predicate."</summary>
			RowViolatesWithoutPredicate = 104236,

			/// <summary>Error code 104237: "Invalid length argument provided.  The length cannot be a negative value."</summary>
			InvalidLength = 104237,

			/// <summary>Error code 104238: "Maximum call depth ({0}) has been exceeded."</summary>
			CallOverflow = 104238,
			
			/// <summary>Error code 104239: "Row extractor expression must reference a table expression with at most one row.  Use a restriction or quota query to limit the number of rows in the source table expression."</summary>
			InvalidRowExtractorExpression = 104239,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Runtime.RuntimeException", typeof(RuntimeException).Assembly);

		// Constructors
		public RuntimeException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public RuntimeException(Codes errorCode, PlanNode context) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null)
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, DebugLocator locator) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null)
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public RuntimeException(Codes errorCode, PlanNode context, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) 
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, DebugLocator locator, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) 
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public RuntimeException(Codes errorCode, Exception innerException, PlanNode context) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) 
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, Exception innerException, DebugLocator locator) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) 
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public RuntimeException(Codes errorCode, Exception innerException, PlanNode context, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) 
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, Exception innerException, DebugLocator locator, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) 
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, PlanNode context) : base(_resourceManager, (int)errorCode, severity, null, null)
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, DebugLocator locator) : base(_resourceManager, (int)errorCode, severity, null, null)
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, PlanNode context, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) 
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, DebugLocator locator, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) 
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, Exception innerException, PlanNode context) : base(_resourceManager, (int)errorCode, severity, innerException, null) 
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, Exception innerException, DebugLocator locator) : base(_resourceManager, (int)errorCode, severity, innerException, null) 
		{
			SetLocator(locator);
		}
		
		public RuntimeException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, Exception innerException, PlanNode context, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) 
		{
			SetContext(context);
		}
		public RuntimeException(Codes errorCode, ErrorSeverity severity, Exception innerException, DebugLocator locator, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) 
		{
			SetLocator(locator);
		}
		
		public override string GetDetails()
		{
			if (_context != null)
				return String.Format("Exception occurred while executing the following code: {0}", _context);
			
			return base.GetDetails();
		}
		
		public override string Message
		{
			get
			{
				if (_line > -1)
					return String.Format("{0} ({1}{2}:{3})", base.Message, _locator == null ? "" : (_locator + "@"), _line, _linePos);
					
				return base.Message;
			}
		}
		
		private string _locator;
		public string Locator
		{
			get { return _locator; }
			set { _locator = value; }
		}

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
		
		private string _context = null;
		public string Context
		{
			get { return _context; }
			set { _context = value; }
		}
		
		public bool HasContext()
		{
			return (_line > -1) || (_context != null);
		}
		
		public void SetContext(PlanNode planNode)
		{
			if (planNode.Line > -1)
			{
				_line = planNode.Line;
				_linePos = planNode.LinePos;
			}
			else
				_context = planNode.SafeEmitStatementAsString();
		}

		public void SetLocator(DebugLocator locator)
		{
			if (locator != null)
			{
				_locator = locator.Locator;
				_line = locator.Line;
				_linePos = locator.LinePos;
			}
		}

		public RuntimeException(ErrorSeverity severity, int code, string message, string details, string serverContext, string locator, int line, int linePos, string context, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
			_locator = locator;
			_line = line;
			_linePos = linePos;
			_context = context;
		}
	}
}