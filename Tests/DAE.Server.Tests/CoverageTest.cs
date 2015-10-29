/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Server.Tests.Utilities;

	[TestFixture]
	public class CoverageTest : InProcessTestFixture
	{
		[Test, Ignore("Rewrite for native values processing")] public void ExecuteDAE() { ExecuteScript("Coverage.Scripts", "DAE"); }
		[Test, Ignore("Rewrite for native values processing")] public void ExecuteExceptions() { ExecuteScript("Coverage.Scripts", "Exceptions"); }
		[Test, Ignore("Fix")] public void ExecuteParserEmitter() { ExecuteScript("Coverage.Scripts", "ParserEmitter"); }
		[Test] public void ExecuteLexer() { ExecuteScript("Coverage.Scripts", "Lexer"); }
		[Test] public void ExecuteLanguageConstructs() { ExecuteScript("Coverage.Scripts", "LanguageConstructs"); }
		[Test] public void ExecuteNilLibrary() { ExecuteScript("Coverage.Scripts", "NilLibrary"); }
		[Test] public void ExecuteBooleanLibrary() { ExecuteScript("Coverage.Scripts", "BooleanLibrary"); }
		[Test] public void ExecuteByteLibrary() { ExecuteScript("Coverage.Scripts", "ByteLibrary"); }
		[Test] public void ExecuteShortLibrary() { ExecuteScript("Coverage.Scripts", "ShortLibrary"); }
		[Test] public void ExecuteIntegerLibrary() { ExecuteScript("Coverage.Scripts", "IntegerLibrary"); }
		[Test] public void ExecuteLongLibrary() { ExecuteScript("Coverage.Scripts", "LongLibrary"); }
		[Test] public void ExecuteLongAggregates() { ExecuteScript("Coverage.Scripts", "LongAggregates"); }
		[Test] public void ExecuteDecimalLibrary() { ExecuteScript("Coverage.Scripts", "DecimalLibrary"); }
		[Test] public void ExecuteMoneyLibrary() { ExecuteScript("Coverage.Scripts", "MoneyLibrary"); }
		[Test] public void ExecuteGuidLibrary() { ExecuteScript("Coverage.Scripts", "GuidLibrary"); }
		[Test] public void ExecuteStringLibrary() { ExecuteScript("Coverage.Scripts", "StringLibrary"); }
		[Test] public void ExecuteTimeSpanLibrary() { ExecuteScript("Coverage.Scripts", "TimeSpanLibrary"); }
		[Test] public void ExecuteDateTimeLibrary() { ExecuteScript("Coverage.Scripts", "DateTimeLibrary"); }
		[Test] public void ExecuteDateLibrary() { ExecuteScript("Coverage.Scripts", "DateLibrary"); }
		[Test] public void ExecuteTimeLibrary() { ExecuteScript("Coverage.Scripts", "TimeLibrary"); }
		[Test] public void ExecuteMathLibrary() { ExecuteScript("Coverage.Scripts", "MathLibrary"); }
		[Test] public void ExecuteListLibrary() { ExecuteScript("Coverage.Scripts", "ListLibrary"); }
		[Test] public void ExecuteCursorLibrary() { ExecuteScript("Coverage.Scripts", "CursorLibrary"); }
		[Test] public void ExecuteDynamicEvaluation() { ExecuteScript("Coverage.Scripts", "DynamicEvaluation"); }
		[Test] public void ExecuteBrowse() { ExecuteScript("Coverage.Scripts", "Browse"); }
		[Test] public void ExecuteTypes() { ExecuteScript("Coverage.Scripts", "Types"); }
		[Test] public void ExecuteOperators() { ExecuteScript("Coverage.Scripts", "Operators"); }
		[Test] public void ExecuteAggregates() { ExecuteScript("Coverage.Scripts", "Aggregates"); }
		[Test] public void ExecuteAggregateOperators() { ExecuteScript("Coverage.Scripts", "AggregateOperators"); }
		[Test] public void ExecuteMinMaxOperators() { ExecuteScript("Coverage.Scripts", "MinMaxOperators"); }
		[Test] public void ExecuteImplicitConversions() { ExecuteScript("Coverage.Scripts", "ImplicitConversions"); }
		[Test] public void ExecuteRowLibrary() { ExecuteScript("Coverage.Scripts", "RowLibrary"); }
		[Test] public void ExecuteRowParameterColumnOrder() { ExecuteScript("Coverage.Scripts", "RowParameterColumnOrder"); }
		[Test] public void ExecuteTables() { ExecuteScript("Coverage.Scripts", "Tables"); } // TODO: Columns in a memory device cannot be altered or dropped.
		[Test] public void ExecuteSessionTables() { ExecuteScript("Coverage.Scripts", "SessionTables"); }
		[Test] public void ExecuteTableLibrary() { ExecuteScript("Coverage.Scripts", "TableLibrary"); }
		[Test] public void ExecuteSessionAndProcessScopedTables() { ExecuteScript("Coverage.Scripts", "SessionAndProcessScopedTables"); }
		[Test] public void ExecuteTableCallsWithStackReferences() { ExecuteScript("Coverage.Scripts", "TableCallsWithStackReferences"); }
		[Test] public void ExecuteViews() { ExecuteScript("Coverage.Scripts", "Views"); }
		[Test] public void ExecuteReferences() { ExecuteScript("Coverage.Scripts", "References"); }
		[Test] public void ExecuteConstraints() { ExecuteScript("Coverage.Scripts", "Constraints"); }
		[Test] public void ExecuteDevices() { ExecuteScript("Coverage.Scripts", "Devices"); }
		[Test] public void ExecuteEventHandlers() { ExecuteScript("Coverage.Scripts", "EventHandlers"); }
		[Test] public void ExecuteExists() { ExecuteScript("Coverage.Scripts", "Exists"); }
		[Test] public void ExecuteDML() { ExecuteScript("Coverage.Scripts", "DML"); }
		[Test] public void ExecuteCursors() { ExecuteScript("Coverage.Scripts", "Cursors"); }
		[Test] public void ExecuteTransitionConstraintWithNamedRowVariables() { ExecuteScript("Coverage.Scripts", "TransitionConstraintWithNamedRowVariables"); }
		[Test] public void ExecuteDeferredConstraintsWithKeys() { ExecuteScript("Coverage.Scripts", "DeferredConstraintsWithKeys"); }

		[Test, Ignore("Doesn't work through ExecuteScript")]
		public void ExecuteSecurityLibrary()
		{
			// This script must not be executed within a transaction because it executes some statements on a separate process, causing a block with itself.
			ExecuteScript("SetUseImplicitTransactions(false);");
			ExecuteScript("Coverage.Scripts", "SecurityLibrary");
		}

		[Test] public void ExecuteViewUpdatability() { ExecuteScript("Coverage.Scripts", "ViewUpdatability"); }
		[Test] public void ExecuteExplode() { ExecuteScript("Coverage.Scripts", "Explode"); }
		[Test] public void ExecuteSorts() { ExecuteScript("Coverage.Scripts", "Sorts"); } // TODO: Known issue with non-unique orders in the memory device?
		[Test] public void ExecuteTransitionConstraints() { ExecuteScript("Coverage.Scripts", "TransitionConstraints"); }
		[Test] public void ExecuteSystemCatalog() { ExecuteScript("Coverage.Scripts", "SystemCatalog"); } // TODO: TestNavigable fails after reset with a cursor on TableVars, also, the ObjectDependencies explosion runs, but I didn't wait for it to complete
		[Test, Ignore("This coverage fails because the ScriptLibrary operator needs to be revisited")] public void ExecuteSerialization() { ExecuteScript("Coverage.Scripts", "Serialization"); }
		[Test, Ignore("Doesn't work through ExecuteScript")] public void ExecuteApplicationTransactions() { ExecuteScript("Coverage.Scripts", "ApplicationTransactions"); }
		[Test] public void ExecuteCreateTableFrom() { ExecuteScript("Coverage.Scripts", "CreateTableFrom"); }
		[Test] public void ExecuteConversions() { ExecuteScript("Coverage.Scripts", "Conversions"); }
		[Test] public void ExecuteVersionNumber() { ExecuteScript("Coverage.Scripts", "VersionNumber"); }
		[Test] public void ExecuteLibraryTypes() { ExecuteScript("Coverage.Scripts", "LibraryTypes"); }
		[Test] public void ExecuteLibraryCoverage() { ExecuteScript("Coverage.Scripts", "LibraryCoverage"); }
		[Test] public void ExecuteLibraryRename() { ExecuteScript("Coverage.Scripts", "LibraryRename"); }
		[Test] public void ExecuteLibraryDependencies() { ExecuteScript("Coverage.Scripts", "LibraryDependencies"); }
		[Test, Ignore("Build this coverage")] public void ExecuteLibraryFilesCoverage() { ExecuteScript("Coverage.Scripts", "LibraryFilesCoverage"); }
		//[Test] public void ExecuteDerivationMaps() { ExecuteScript("Coverage.Scripts", "DerivationMaps"); } // no longer required (deprecated feature) BTR 6/6/2004
		[Test, Ignore("Doesn't run 1/17/2004")] public void ExecuteMaximumRowCount() { ExecuteScript("Coverage.Scripts", "MaximumRowCount"); }
		[Test, Ignore("Doesn't run 1/17/2004")] public void ExecuteStreamAllocation() { ExecuteScript("Coverage.Scripts", "StreamAllocation"); }
		[Test, Ignore("Build this coverage")] public void ExecuteScalarAllocation() { ExecuteScript("Coverage.Scripts", "ScalarAllocation"); }
		[Test, Ignore("Build this coverage")] public void ExecuteRowAllocation() { ExecuteScript("Coverage.Scripts", "RowAllocation"); }
		[Test, Ignore("Build this coverage")] public void ExecuteLockAllocation() { ExecuteScript("Coverage.Scripts", "LockAllocation"); }
		[Test] public void ExecuteTestProject() { ExecuteScript("Coverage.Scripts", "TestProject"); }
		[Test] public void ExecuteKeyAffectingUpdate() { ExecuteScript("Coverage.Scripts", "KeyAffectingUpdate"); }
		[Test] public void ExecuteRowInsert() { ExecuteScript("Coverage.Scripts", "RowInsert"); }
		[Test] public void ExecuteNodeOptimization() { ExecuteScript("Coverage.Scripts", "NodeOptimization"); }
		[Test] public void ExecuteSemiTables() { ExecuteScript("Coverage.Scripts", "SemiTables"); }
		[Test] public void ExecuteScanTable() { ExecuteScript("Coverage.Scripts", "ScanTable"); }
		[Test] public void ExecuteSpecifyClause() { ExecuteScript("Coverage.Scripts", "SpecifyClause"); }
		[Test] public void ExecuteSpecificAssignment() { ExecuteScript("Coverage.Scripts", "SpecificAssignment"); }
		[Test, Ignore("Build this coverage")] public void ExecuteTableIndexer() { ExecuteScript("Coverage.Scripts", "TableIndexer"); }
		[Test, Ignore("Integration test, needs server setup")] public void ExecuteServerLinks() { ExecuteScript("Coverage.Scripts", "ServerLink"); }
		[Test] public void ExecuteDebugLibrary() { ExecuteScript("Coverage.Scripts", "DebugLibrary"); }
		[Test] public void ExecutePlanCache() { ExecuteScript("Coverage.Scripts", "PlanCache"); }
		[Test] public void ExecuteOperatorText() { ExecuteScript("Coverage.Scripts", "OperatorText"); }
		[Test] public void ExecuteDeferredConstraintChecking() { ExecuteScript("Coverage.Scripts", "DeferredConstraintChecking"); }
		[Test] public void ExecuteSystemCatalogOperators() { ExecuteScript("Coverage.Scripts", "SystemCatalogOperators"); }
		[Test] public void ExecuteTestAdornedConstraints() { ExecuteScript("Coverage.Scripts", "TestAdornedConstraints"); }
		[Test] public void ExecuteTestSargabilityWithConversion() { ExecuteScript("Coverage.Scripts", "TestSargabilityWithConversion"); }
		[Test] public void ExecuteTableAndRowValuedAttributes() { ExecuteScript("Coverage.Scripts", "TableAndRowValuedAttributes"); }
		[Test] public void ExecuteConditionals() { ExecuteScript("Coverage.Scripts", "Conditionals"); }
		[Test] public void ExecuteWarnings() { ExecuteScript("Coverage.Scripts", "Warnings"); }
	}
}
