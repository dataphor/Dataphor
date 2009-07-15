/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Diagnostics
{
	[TestFixture]
	public class CoverageTest
	{
		private ServerConfiguration FConfiguration;
		private IServer FServer;
		private IServerSession FSession;
		private IServerProcess FProcess;
		
		[TestFixtureSetUp]
		public void SetUpFixture()
		{
			FConfiguration = ServerTestUtility.GetTestConfiguration();
			ServerTestUtility.ResetInstance(FConfiguration);
			FServer = ServerTestUtility.GetServer(FConfiguration);
			FServer.Start();
			
			IServerSession LSession = FServer.Connect(new SessionInfo("Admin", ""));
			try
			{
				IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo));
				try
				{
					LProcess.ExecuteScript("EnsureLibraryRegistered('Frontend')");
				}
				finally
				{
					LSession.StopProcess(LProcess);
				}
			}
			finally
			{
				FServer.Disconnect(LSession);
			}
		}
		
		[TestFixtureTearDown]
		public void TearDownFixture()
		{
			FServer.Stop();
			ServerTestUtility.ResetInstance(FConfiguration);
		}
		
		[SetUp]
		public void SetUp()
		{
			FSession = FServer.Connect(new SessionInfo("Admin", ""));
			FProcess = FSession.StartProcess(new ProcessInfo(FSession.SessionInfo));
		}
		
		[TearDown]
		public void TearDown()
		{
			if (FProcess != null)
			{
				FSession.StopProcess(FProcess);
				FProcess = null;
			}
			
			if (FSession != null)
			{
				FServer.Disconnect(FSession);
				FSession = null;
			}
		}
		
		private void ExecuteScript(string ALibraryName, string AScriptName)
		{
			FProcess.ExecuteScript(String.Format("ExecuteScript('{0}', '{1}');", ALibraryName, AScriptName));
		}
		
		[Test] public void ExecuteDAE() { ExecuteScript("Coverage.Scripts", "DAE"); } // TODO: Rewrite for native values processing
		[Test] public void ExecuteExceptions() { ExecuteScript("Coverage.Scripts", "Exceptions"); } // TODO: Rewrite for native values processing
		[Test] public void ExecuteParserEmitter() { ExecuteScript("Coverage.Scripts", "ParserEmitter"); } // verified BTR 7/11/2006 // TODO: Fix this
		[Test] public void ExecuteLexer() { ExecuteScript("Coverage.Scripts", "Lexer"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLanguageConstructs() { ExecuteScript("Coverage.Scripts", "LanguageConstructs"); } // verified BTR 3/5/2007
		[Test] public void ExecuteNilLibrary() { ExecuteScript("Coverage.Scripts", "NilLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteBooleanLibrary() { ExecuteScript("Coverage.Scripts", "BooleanLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteByteLibrary() { ExecuteScript("Coverage.Scripts", "ByteLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteShortLibrary() { ExecuteScript("Coverage.Scripts", "ShortLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteIntegerLibrary() { ExecuteScript("Coverage.Scripts", "IntegerLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLongLibrary() { ExecuteScript("Coverage.Scripts", "LongLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteDecimalLibrary() { ExecuteScript("Coverage.Scripts", "DecimalLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteMoneyLibrary() { ExecuteScript("Coverage.Scripts", "MoneyLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteGuidLibrary() { ExecuteScript("Coverage.Scripts", "GuidLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteStringLibrary() { ExecuteScript("Coverage.Scripts", "StringLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteTimeSpanLibrary() { ExecuteScript("Coverage.Scripts", "TimeSpanLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteDateTimeLibrary() { ExecuteScript("Coverage.Scripts", "DateTimeLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteDateLibrary() { ExecuteScript("Coverage.Scripts", "DateLibrary"); } // Verified BTR 3/5/2007
		[Test] public void ExecuteTimeLibrary() { ExecuteScript("Coverage.Scripts", "TimeLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteMathLibrary() { ExecuteScript("Coverage.Scripts", "MathLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteListLibrary() { ExecuteScript("Coverage.Scripts", "ListLibrary"); } // Verified BTR 3/5/2007
		[Test] public void ExecuteCursorLibrary() { ExecuteScript("Coverage.Scripts", "CursorLibrary"); } // Verified BTR 3/5/2007
		[Test] public void ExecuteDynamicEvaluation() { ExecuteScript("Coverage.Scripts", "DynamicEvaluation"); } // Verified BTR 3/5/2007
		[Test] public void ExecuteBrowse() { ExecuteScript("Coverage.Scripts", "Browse"); } // Verified BTR 3/5/2007
		[Test] public void ExecuteTypes() { ExecuteScript("Coverage.Scripts", "Types"); } // verified BTR 3/5/2007
		[Test] public void ExecuteOperators() { ExecuteScript("Coverage.Scripts", "Operators"); } // verified BTR 3/5/2007
		[Test] public void ExecuteAggregates() { ExecuteScript("Coverage.Scripts", "Aggregates"); } // verified BTR 3/5/2007
		[Test] public void ExecuteAggregateOperators() { ExecuteScript("Coverage.Scripts", "AggregateOperators"); } // verified BTR 3/5/2007
		[Test] public void ExecuteMinMaxOperators() { ExecuteScript("Coverage.Scripts", "MinMaxOperators"); } // verified BTR 3/5/2007
		[Test] public void ExecuteImplicitConversions() { ExecuteScript("Coverage.Scripts", "ImplicitConversions"); } // verified BTR 3/5/2007
		[Test] public void ExecuteRowLibrary() { ExecuteScript("Coverage.Scripts", "RowLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteRowParameterColumnOrder() { ExecuteScript("Coverage.Scripts", "RowParameterColumnOrder"); } // verified BTR 3/5/2007
		[Test] public void ExecuteTables() { ExecuteScript("Coverage.Scripts", "Tables"); } // verified BTR 3/5/2007 // TODO: Columns in a memory device cannot be altered or dropped.
		[Test] public void ExecuteSessionTables() { ExecuteScript("Coverage.Scripts", "SessionTables"); } // verified BTR 3/5/2007
		[Test] public void ExecuteTableLibrary() { ExecuteScript("Coverage.Scripts", "TableLibrary"); } // verified BTR 3/5/2007
		[Test] public void ExecuteSessionAndProcessScopedTables() { ExecuteScript("Coverage.Scripts", "SessionAndProcessScopedTables"); } // verified BTR 3/5/2007
		[Test] public void ExecuteTableCallsWithStackReferences() { ExecuteScript("Coverage.Scripts", "TableCallsWithStackReferences"); } // verified BTR 3/5/2007
		[Test] public void ExecuteViews() { ExecuteScript("Coverage.Scripts", "Views"); } // verified BTR 3/5/2007
		[Test] public void ExecuteReferences() { ExecuteScript("Coverage.Scripts", "References"); } // verified BTR 3/5/2007
		[Test] public void ExecuteConstraints() { ExecuteScript("Coverage.Scripts", "Constraints"); } // verified BTR 3/5/2007
		[Test] public void ExecuteDevices() { ExecuteScript("Coverage.Scripts", "Devices"); } // verified BTR 3/5/2007
		[Test] public void ExecuteEventHandlers() { ExecuteScript("Coverage.Scripts", "EventHandlers"); } // verified BTR 3/5/2007
		[Test] public void ExecuteExists() { ExecuteScript("Coverage.Scripts", "Exists"); } // verified BTR 3/5/2007
		[Test] public void ExecuteDML() { ExecuteScript("Coverage.Scripts", "DML"); } // verified BTR 3/5/2007
		[Test] public void ExecuteCursors() { ExecuteScript("Coverage.Scripts", "Cursors"); } // verified BTR 3/5/2007
		[Test]
		public void ExecuteSecurityLibrary()
		{
			// This script must not be executed within a transaction because it executes some statements on a separate process, causing a block with itself.
			FProcess.ExecuteScript("SetUseImplicitTransactions(false);");
			ExecuteScript("Coverage.Scripts", "SecurityLibrary"); // verified BTR 3/5/2007
		}
		[Test] public void ExecuteViewUpdatability() { ExecuteScript("Coverage.Scripts", "ViewUpdatability"); } // verified BTR 3/5/2007
		[Test] public void ExecuteExplode() { ExecuteScript("Coverage.Scripts", "Explode"); } // verified BTR 3/5/2007
		[Test] public void ExecuteSorts() { ExecuteScript("Coverage.Scripts", "Sorts"); } // verified BTR 3/5/2007 // TODO: Known issue with non-unique orders in the memory device?
		[Test] public void ExecuteTransitionConstraints() { ExecuteScript("Coverage.Scripts", "TransitionConstraints"); } // verified BTR 3/5/2007
		[Test] public void ExecuteSystemCatalog() { ExecuteScript("Coverage.Scripts", "SystemCatalog"); } // verified BTR 3/5/2007 // TODO: TestNavigable fails after reset with a cursor on TableVars, also, the ObjectDependencies explosion runs, but I didn't wait for it to complete
		[Test] public void ExecuteSerialization() { ExecuteScript("Coverage.Scripts", "Serialization"); } // verified BTR 10/05/2004 // TODO: This coverage fails because the ScriptLibrary operator needs to be revisited
		[Test] public void ExecuteApplicationTransactions() { ExecuteScript("Coverage.Scripts", "ApplicationTransactions"); } // verified BTR 3/5/2007 // TODO: Doesn't work through ExecuteScript
		[Test] public void ExecuteCreateTableFrom() { ExecuteScript("Coverage.Scripts", "CreateTableFrom"); } // verified BTR 3/5/2007
		[Test] public void ExecuteConversions() { ExecuteScript("Coverage.Scripts", "Conversions"); } // verified BTR 3/5/2007
		[Test] public void ExecuteVersionNumber() { ExecuteScript("Coverage.Scripts", "VersionNumber"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLibraryTypes() { ExecuteScript("Coverage.Scripts", "LibraryTypes"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLibraryCoverage() { ExecuteScript("Coverage.Scripts", "LibraryCoverage"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLibraryRename() { ExecuteScript("Coverage.Scripts", "LibraryRename"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLibraryDependencies() { ExecuteScript("Coverage.Scripts", "LibraryDependencies"); } // verified BTR 3/5/2007
		[Test] public void ExecuteLibraryFilesCoverage() { ExecuteScript("Coverage.Scripts", "LibraryFilesCoverage"); } // TODO: Build this coverage
		[Test] public void ExecuteDerivationMaps() { ExecuteScript("Coverage.Scripts", "DerivationMaps"); } // no longer required (deprecated feature) BTR 6/6/2004
		[Test] public void ExecuteMaximumRowCount() { ExecuteScript("Coverage.Scripts", "MaximumRowCount"); } // verified BTR 6/9/2003 // TODO: Doesn't run 1/17/2004
		[Test] public void ExecuteStreamAllocation() { ExecuteScript("Coverage.Scripts", "StreamAllocation"); } // TODO: Doesn't run 1/17/2004
		[Test] public void ExecuteScalarAllocation() { ExecuteScript("Coverage.Scripts", "ScalarAllocation"); } // TODO: Build this coverage
		[Test] public void ExecuteRowAllocation() { ExecuteScript("Coverage.Scripts", "RowAllocation"); } // TODO: Build this coverage
		[Test] public void ExecuteLockAllocation() { ExecuteScript("Coverage.Scripts", "LockAllocation"); } // TODO: Build this coverage
		[Test] public void ExecuteTestProject() { ExecuteScript("Coverage.Scripts", "TestProject"); } // verified BTR 3/5/2007
		[Test] public void ExecuteKeyAffectingUpdate() { ExecuteScript("Coverage.Scripts", "KeyAffectingUpdate"); } // verified BTR 3/5/2007
		[Test] public void ExecuteRowInsert() { ExecuteScript("Coverage.Scripts", "RowInsert"); } // verified BTR 3/5/2007
		[Test] public void ExecuteNodeOptimization() { ExecuteScript("Coverage.Scripts", "NodeOptimization"); } // verified BTR 3/5/2007
		[Test] public void ExecuteSemiTables() { ExecuteScript("Coverage.Scripts", "SemiTables"); } // verified BTR 3/5/2007 // TODO: Ambiguous column names with common columns???
		[Test] public void ExecuteScanTable() { ExecuteScript("Coverage.Scripts", "ScanTable"); } // TODO: Needs to be rewritten (still uses old style type specifier in table selector expressions)
		[Test] public void ExecuteSpecifyClause() { ExecuteScript("Coverage.Scripts", "SpecifyClause"); } // verified BTR 3/5/2007
		[Test] public void ExecuteTableIndexer() { ExecuteScript("Coverage.Scripts", "TableIndexer"); } // TODO: Build this coverage
		[Test] public void ExecuteServerLinks() { ExecuteScript("Coverage.Scripts", "ServerLink"); }
	}
}
