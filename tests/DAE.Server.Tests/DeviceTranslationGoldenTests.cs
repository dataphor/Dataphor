/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>Where the SQL of a case comes from.</summary>
	public enum TranslationCaseKind
	{
		/// <summary>
		/// A D4 expression translated through the SQLDevice operator D4ToSQL, which compiles the expression, checks the
		/// device supports it and returns the device's emitted text without executing anything.
		/// </summary>
		Query,

		/// <summary>
		/// A D4 statement run against the device, whose SQL is whatever the recording connection was asked to run. This
		/// is how the data-definition half is reached: a create, alter or drop reaches the device as commands, not as a
		/// query whose text an operator can be asked for.
		/// </summary>
		Statement,

		/// <summary>
		/// Not SQL: the connection string the device built and handed its connection class, which the recorder records
		/// beside the statements. The device assembles it from its own settings through its connection-string builder's
		/// keyword legend, and both are what a device definition written for an earlier release depends on - so the
		/// string is pinned character for character, which is the only check on it that needs no database server.
		/// </summary>
		Connection
	}

	/// <summary>One case of the translation corpus: a name, where its SQL comes from, and the D4 that produces it.</summary>
	public sealed class TranslationCase
	{
		public TranslationCase(string name, TranslationCaseKind kind, string d4)
		{
			Name = name;
			Kind = kind;
			D4 = d4;
		}

		/// <summary>The case's name, which is also its golden file's name (without the extension).</summary>
		public string Name { get; }

		public TranslationCaseKind Kind { get; }

		/// <summary>The D4 expression (a query case) or statement (a statement case); empty for a connection case, which runs no D4 of its own.</summary>
		public string D4 { get; }

		/// <summary>The golden's name below this device's folder.</summary>
		public string GoldenFileName => Name + DeviceTranslationGoldenTests.GoldenExtension;

		public override string ToString() => String.Format("{0} ({1})", Name, Kind);
	}

	/// <summary>A D4 expression the device must refuse to translate, and the error code the refusal must carry.</summary>
	public sealed class UnsupportedCase
	{
		public UnsupportedCase(string name, string d4, int errorCode)
		{
			Name = name;
			D4 = d4;
			ErrorCode = errorCode;
		}

		public string Name { get; }

		public string D4 { get; }

		/// <summary>The code of the error the refusal must carry; error codes are a compatibility surface, so the code is what is asserted rather than the message.</summary>
		public int ErrorCode { get; }

		public override string ToString() => String.Format("{0} ({1})", Name, ErrorCode);
	}

	/// <summary>
	/// The first half of L3 of the WS3 test layer: what a SQL device generates for a corpus of D4, compared character for
	/// character with checked-in files under tests/goldens/devices. A change in the translation - the SQL syntax tree
	/// SQLDevice builds, or the dialect emitter that writes it out - then shows up as a reviewable difference instead of
	/// as a wrong query reaching a customer's database server.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>The expected values may never be edited to make a change pass.</b> There is no reference runtime for this: the
	/// text a working system produces is the specification (decision D2 of the plan, and the project's ticket rules). A
	/// difference in these files is either a defect in the change that produced it or a deliberate, reviewed change to
	/// what the device sends - and in the second case the new text is read, line by line, before it is checked in.
	/// </para>
	/// <para>
	/// <b>No database server is needed, and none is contacted.</b> The device's connection class is the recording
	/// connection of the TestFramework.SQLRecorder library, which records what it is asked to run and opens nothing; the
	/// device's server name is deliberately unreachable. The recorder refuses every read it has not been told how to
	/// answer rather than returning an empty result, because a golden produced by a device whose read quietly returned
	/// nothing would pin a lie - <see cref="ReadingADeviceTableIsRefusedRatherThanAnsweredWithNothing"/> holds that.
	/// </para>
	/// <para>
	/// <b>What these goldens do not say.</b> The three device settings that make a SQL device connect when it starts -
	/// ShouldDetermineVersion, ShouldEnsureDatabase and ShouldEnsureOperators - are turned off by every subclass, so the
	/// device never learns which version of the server it is talking to and emits what its default version assumption
	/// produces (MSSQLDevice.IsMSSQL70 changes some operators' translation; PostgreSQLDevice.MajorVersion changes none of
	/// its own translation today). These files therefore pin the translation for one version assumption and say nothing
	/// about whether a real server accepts the text. That is what test-device-live and device-pgsql-port are for; this
	/// pass is the fast layer underneath them.
	/// </para>
	/// <para>
	/// The corpus and the machinery are here and a subclass supplies the device: its library, the create device
	/// statement, the folder its goldens live in and the expressions it refuses. The two subclasses are
	/// MSSQLDeviceTranslationGoldenTests and PGSQLDeviceTranslationGoldenTests, and the corpus is deliberately shared: a
	/// case one device covers and the other does not is how a device quietly ends up with less of a net than its sibling.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	public abstract class DeviceTranslationGoldenTests : InProcessTestFixture
	{
		/// <summary>Set to 1 to write the goldens instead of comparing them; see AGENTS.md "Build and test".</summary>
		public const string UpdateVariable = "DATAPHOR_UPDATE_DEVICE_GOLDENS";

		/// <summary>The extension a golden carries. The files hold SQL, and nothing in the suite walks the tree by extension.</summary>
		public const string GoldenExtension = ".sql";

		/// <summary>This pass's own tree below <see cref="TestEnvironment.GoldensDirectory"/>.</summary>
		public const string GoldensFolderName = "devices";

		/// <summary>The name every subclass's device is created under, so that the corpus below can name it.</summary>
		public const string DeviceName = "GoldenDevice";

		/// <summary>The test library whose connection class records instead of connecting.</summary>
		public const string RecorderLibraryName = "TestFramework.SQLRecorder";

		/// <summary>
		/// A table in the server's own default device rather than in the device under test, for the unsupported case that
		/// asks a SQL device to translate an expression whose plan lands somewhere else.
		/// </summary>
		public const string OtherDeviceTableName = "NotInDevice";

		/// <summary>The D4 library the device's class comes from (MSSQLDevice), which is also the library the corpus is created in.</summary>
		protected abstract string DeviceLibraryName { get; }

		/// <summary>This device's folder below tests/goldens/devices.</summary>
		protected abstract string GoldensDeviceFolderName { get; }

		/// <summary>
		/// The create device statement, naming the recorder as its connection class and turning off every setting that
		/// would make the device connect when it starts.
		/// </summary>
		protected abstract string CreateDeviceStatement { get; }

		/// <summary>
		/// The server name the statement above configures: an unreachable placeholder, asserted to be what the device
		/// actually tried to connect to.
		/// </summary>
		protected abstract string UnreachableServerName { get; }

		/// <summary>The expressions this device refuses to translate, with the error code each refusal must carry.</summary>
		protected abstract UnsupportedCase[] UnsupportedCases { get; }

		/// <summary>
		/// Connection-string text every connection this device opens must carry, whatever the device definition asked for -
		/// a setting the device defaults because a definition written for an earlier release does not name it and would
		/// otherwise stop connecting. Empty for a device that defaults nothing.
		/// </summary>
		protected virtual string[] RequiredConnectionStringText => [];

		protected override string[] Libraries => ["Connection", "SQLDevice", DeviceLibraryName, RecorderLibraryName];

		// The device's library pulls SQLDevice and Connection in as requisites; the recorder is registered for its
		// connection class and its operators.
		protected override string[] LibrariesToRegister => [DeviceLibraryName, RecorderLibraryName];

		/// <summary>
		/// The statement cases, in the order they have to run in: the corpus's tables are created once, the scratch
		/// table is created, altered and dropped, and the row inserted into Ledger needs Ledger to exist. What a device
		/// issues the first time a table is created in it is as much of its translation as any query, and is what
		/// reaches a customer's database.
		/// </summary>
		/// <remarks>
		/// Only system scalar types appear, so that the corpus compiles on every SQL device rather than naming one
		/// device's own types. Employee.Name carries an explicit Storage.Length tag and Employee.Nickname carries none,
		/// so that the generated column list shows both the tag and the device's default string width - which is the
		/// device's documented mapping, not a defect.
		/// </remarks>
		private static readonly TranslationCase[] StatementCases =
		[
			new TranslationCase
			(
				"Table.Employee",
				TranslationCaseKind.Statement,
				@"create table Employee
	in " + DeviceName + @"
	{
		ID : Integer,
		Name : String tags { Storage.Length = ""40"" },
		Nickname : String,
		Salary : Money,
		HireDate : DateTime,
		Department_ID : Integer,
		key { ID }
	};"
			),
			new TranslationCase
			(
				"Table.Department",
				TranslationCaseKind.Statement,
				@"create table Department
	in " + DeviceName + @"
	{
		ID : Integer,
		Name : String,
		key { ID }
	};"
			),
			new TranslationCase
			(
				"Table.AllTypes",
				TranslationCaseKind.Statement,
				@"create table AllTypes
	in " + DeviceName + @"
	{
		ID : Integer,
		Flag : Boolean,
		Amount : Decimal,
		Note : String tags { Storage.Length = ""8000"" },
		Stamp : DateTime,
		Calendar : Date,
		Clock : Time,
		Identifier : Guid,
		Payload : Binary,
		Price : Money,
		key { ID }
	};"
			),
			new TranslationCase
			(
				"Table.Ledger",
				TranslationCaseKind.Statement,
				@"create table Ledger
	in " + DeviceName + @"
	{
		ID : Integer,
		Note : String tags { Storage.Length = ""30"" },
		Amount : Money,
		key { ID }
	};"
			),
			// Inserting a row into a device table makes the engine check the key first, with a select the recorder has
			// to answer before the insert is issued - so the case arms it: the fragment is the shape of that generated
			// select, and 1 is the answer that means "this key is not already there". The knowledge of what the engine
			// asks for lives here rather than in the recorder, which refuses whatever it has not been told.
			// Insert is the only data-manipulation case: an update or a delete asks for the row itself, which a
			// recorder cannot answer - see the NOTE at RecordingCommand.InternalOpen - and belongs to test-device-live.
			new TranslationCase
			(
				"DML.Insert",
				TranslationCaseKind.Statement,
				@"SQLRecorderArmCursor('case when not(exists(', 'dummy1', 1);
insert table { row { 1 ID, ""first"" Note, $1.50 Amount } } into Ledger;"
			),
			new TranslationCase
			(
				"Table.Scratch.Create",
				TranslationCaseKind.Statement,
				@"create table Scratch in " + DeviceName + @" { ID : Integer, key { ID } };"
			),
			new TranslationCase
			(
				"Table.Scratch.AlterAddColumn",
				TranslationCaseKind.Statement,
				@"alter table Scratch { create column Note : String { nil } };"
			),
			new TranslationCase
			(
				"Table.Scratch.Drop",
				TranslationCaseKind.Statement,
				@"drop table Scratch;"
			),
		];

		/// <summary>
		/// The query corpus: one case per behaviour, worst first by what a wrong answer would do to a real database -
		/// quoting and literal escaping, then the statement forms the device builds, then the places where SQL's
		/// precedence differs from the plan tree's shape, then order and the device's row-count hint.
		/// </summary>
		private static readonly TranslationCase[] QueryCases =
		[
			new TranslationCase("Query.Table", TranslationCaseKind.Query, "Employee"),
			new TranslationCase("Query.Restrict.StringLiteralWithQuote", TranslationCaseKind.Query, @"Employee where Name = ""O'Brien"""),
			new TranslationCase("Query.Restrict.Comparison", TranslationCaseKind.Query, "Employee where Salary > $1000.00"),
			new TranslationCase("Query.Restrict.Precedence", TranslationCaseKind.Query, "Employee where (ID = 1) or ((ID = 2) and (Department_ID = 3))"),
			new TranslationCase("Query.Restrict.Nil", TranslationCaseKind.Query, "Employee where IsNil(Department_ID)"),
			new TranslationCase("Query.Project", TranslationCaseKind.Query, "Employee over { ID, Name }"),
			new TranslationCase("Query.Rename", TranslationCaseKind.Query, "Employee rename { ID EmployeeID, Name EmployeeName }"),
			new TranslationCase("Query.Extend", TranslationCaseKind.Query, "Employee add { (Salary + $1.00) * 2 Adjusted }"),
			new TranslationCase("Query.Extend.OperatorTranslation", TranslationCaseKind.Query, "Employee add { Length(Name) NameLength, Upper(Name) UpperName }"),
			new TranslationCase("Query.Join.Inner", TranslationCaseKind.Query, "Employee join (Department rename { ID Department_ID, Name DepartmentName })"),
			new TranslationCase("Query.Join.Left", TranslationCaseKind.Query, "Employee left join (Department rename { ID Department_ID, Name DepartmentName })"),
			new TranslationCase("Query.Join.Right", TranslationCaseKind.Query, "Employee right join (Department rename { ID Department_ID, Name DepartmentName })"),
			new TranslationCase("Query.Aggregate", TranslationCaseKind.Query, "Employee group by { Department_ID } add { Sum(Salary) TotalSalary, Count() Headcount }"),
			new TranslationCase("Query.Union", TranslationCaseKind.Query, "(Employee over { ID }) union (Department over { ID })"),
			new TranslationCase("Query.Order", TranslationCaseKind.Query, "Employee order by { Name }"),
			new TranslationCase("Query.Exists", TranslationCaseKind.Query, "(Department rename D) where exists (Employee where Department_ID = D.ID)"),
			new TranslationCase("Query.AllTypes", TranslationCaseKind.Query, "AllTypes"),
		];

		/// <summary>
		/// The connection corpus: one case, the connection string the device handed its connection class while the
		/// statement cases above ran. No D4 of its own - the device built it when it opened a connection for the first
		/// create table.
		/// </summary>
		private static readonly TranslationCase[] ConnectionCases =
		[
			new TranslationCase("Connection", TranslationCaseKind.Connection, String.Empty),
		];

		/// <summary>Every case this pass covers, in the order the statement cases have to run in.</summary>
		private static IEnumerable<TranslationCase> Cases()
		{
			return StatementCases.Concat(QueryCases).Concat(ConnectionCases);
		}

		private static bool Updating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

		/// <summary>This pass's golden tree, tests/goldens/devices.</summary>
		private static string GoldensRootDirectory => Path.Combine(TestEnvironment.GoldensDirectory, GoldensFolderName);

		private string GoldensDirectory => Path.Combine(GoldensRootDirectory, GoldensDeviceFolderName);

		/// <summary>
		/// The session and process the whole fixture works on. One of each, because SetLibrary is session state that does
		/// not survive into the next session: the create device, the create tables and every query have to be compiled
		/// with the device's library current, so that an unqualified name in the corpus resolves to the corpus's table.
		/// </summary>
		/// <remarks>
		/// Deliberately not the per-test session the base fixture gives, which is a new one for every case and would
		/// have no current library.
		/// </remarks>
		private IServerSession _deviceSession;
		private IServerProcess _deviceProcess;

		/// <summary>What the recorder was asked to run while each statement case ran, keyed by the case's name.</summary>
		/// <remarks>
		/// Captured in the fixture's set-up rather than inside the test, because the tables the query cases read have to
		/// exist before any case runs and a table can only be created once.
		/// </remarks>
		private readonly Dictionary<string, string> _recordings = new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>Every distinct connection string the device handed the recorder while the statement cases ran, in the order each was first seen.</summary>
		/// <remarks>
		/// Distinct rather than every occurrence: each data-definition statement opens its own connection (see the NOTE on
		/// <see cref="RecordedConnections"/>), so the same string is handed over once per statement and a list of them would
		/// pin the number of statements rather than the string. Two different strings would both be here, which is what the
		/// golden should show if a device ever built more than one.
		/// </remarks>
		private readonly List<string> _connections = new List<string>();

		[OneTimeSetUp]
		public void SetUpDevice()
		{
			_deviceSession = Instance.Connect();
			_deviceProcess = _deviceSession.StartProcess(new ProcessInfo(_deviceSession.SessionInfo));

			Execute(String.Format("SetLibrary('{0}');", DeviceLibraryName));
			Execute(CreateDeviceStatement);
			Execute(String.Format("create table {0} {{ ID : Integer, key {{ ID }} }};", OtherDeviceTableName));

			foreach (TranslationCase statementCase in Cases().Where(translationCase => translationCase.Kind == TranslationCaseKind.Statement))
			{
				Execute("SQLRecorderClear();");
				Execute(statementCase.D4);
				_recordings.Add(statementCase.Name, Recorded());
				foreach (string connection in RecordedConnections())
					if (!_connections.Contains(connection, StringComparer.Ordinal))
						_connections.Add(connection);
			}
		}

		[OneTimeTearDown]
		public void TearDownDevice()
		{
			try
			{
				if (_deviceProcess != null)
				{
					_deviceSession.StopProcess(_deviceProcess);
					_deviceProcess = null;
				}
			}
			finally
			{
				if (_deviceSession != null)
				{
					Server.Disconnect(_deviceSession);
					_deviceSession = null;
				}
			}
		}

		/// <summary>Runs D4 on the fixture's own process, the one the device's library is current on.</summary>
		private void Execute(string script)
		{
			_deviceProcess.ExecuteScript(script);
		}

		/// <summary>Evaluates a D4 expression returning a String on the fixture's own process.</summary>
		private string EvaluateString(string expression, params (string Name, string Value)[] arguments)
		{
			DataParams parameters = null;
			if (arguments.Length > 0)
			{
				parameters = new DataParams();
				foreach ((string name, string value) in arguments)
					parameters.Add(DataParam.Create(_deviceProcess, name, value));
			}
			return (string)((ServerProcess)_deviceProcess).Evaluate(expression, parameters).AsNative;
		}

		/// <summary>What the recorder has been asked to run since the last clear.</summary>
		private string Recorded()
		{
			return EvaluateString("SQLRecorderStatements()");
		}

		/// <summary>The connection strings the device has opened through the recorder since the last clear, of which there is at least one.</summary>
		/// <remarks>
		/// The recording is cleared before each statement case of the set-up, so what is here is whatever the device has
		/// opened since the last one.
		/// NOTE: there is one only because SQLDeviceSession.ReleaseConnection disposes an execute-pool connection whenever
		/// no device transaction is open, so each data-definition statement opens a fresh one - which the fixture's set-up
		/// now asserts for every statement case, since that is where it collects the connection strings. If that pooling
		/// ever keeps a connection alive across statements, the set-up fails here rather than in a case that checks
		/// something else; clear the recording and open a connection deliberately at that point rather than relaxing it.
		/// </remarks>
		private string[] RecordedConnections()
		{
			string[] connections =
				EvaluateString("SQLRecorderConnections()")
					.Split('\n', StringSplitOptions.RemoveEmptyEntries);

			Assert.That(connections, Is.Not.Empty, "The device opened no connection through the recorder, so there is no connection string to read.");
			return connections;
		}

		/// <summary>
		/// The device's translation of a D4 expression, through the SQLDevice operator that compiles it and emits the
		/// device statement without executing anything. The expression goes in as a parameter rather than as a literal:
		/// the corpus holds quotes of both kinds.
		/// </summary>
		private string TranslateQuery(string expression)
		{
			return EvaluateString(String.Format("D4ToSQL('{0}', AQuery)", DeviceName), ("AQuery", expression));
		}

		private string Generate(TranslationCase translationCase)
		{
			switch (translationCase.Kind)
			{
				case TranslationCaseKind.Query:
					return TranslateQuery(translationCase.D4);

				case TranslationCaseKind.Statement:
					return _recordings[translationCase.Name];

				case TranslationCaseKind.Connection:
					return String.Join("\n", _connections);

				default:
					throw new InvalidOperationException(String.Format("Unknown translation case kind {0}.", translationCase.Kind));
			}
		}

		/// <summary>
		/// The stored form of a golden: the generated text with its line endings turned into newlines and one newline at
		/// the end.
		/// </summary>
		/// <remarks>
		/// Nothing else is normalized. The line endings are, because .gitattributes checks tests/goldens out with
		/// newlines - a golden written with the emitter's own endings would fail every comparison on a fresh checkout -
		/// and because nothing is lost by it: TextEmitter.NewLine writes a literal carriage return and line feed rather
		/// than the platform's, so the ending is a constant of the emitter and not something these files could pin
		/// anyway. Both sides of the comparison go through this method.
		/// </remarks>
		private static string Normalize(string generated)
		{
			string result = generated.Replace("\r\n", "\n").Replace('\r', '\n');
			return result.EndsWith("\n", StringComparison.Ordinal) || result.Length == 0 ? result : result + "\n";
		}

		private string GoldenPath(string fileName)
		{
			return Path.Combine(GoldensDirectory, fileName);
		}

		/// <summary>Reads a golden as text, or null if there is no such file.</summary>
		private string ReadGolden(string fileName)
		{
			string path = GoldenPath(fileName);
			return File.Exists(path) ? File.ReadAllText(path) : null;
		}

		/// <summary>Writes a golden: UTF-8 with no byte-order mark, carrying the newlines <see cref="Normalize"/> put in it.</summary>
		private void WriteGolden(string fileName, string text)
		{
			string path = GoldenPath(fileName);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text, new UTF8Encoding(false));
		}

		/// <summary>
		/// Every file in this device's golden folder, relative to it. Every file, not only the ones with the golden
		/// extension, so that a stray file is reported rather than left to accumulate unnoticed.
		/// </summary>
		private List<string> ExistingGoldens()
		{
			if (!Directory.Exists(GoldensDirectory))
				return new List<string>();
			return
				Directory.EnumerateFiles(GoldensDirectory, "*", SearchOption.AllDirectories)
					.Select(path => Path.GetRelativePath(GoldensDirectory, path).Replace('\\', '/'))
					.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
					.ToList();
		}

		/// <summary>
		/// The pass itself: every case translates and matches its golden, and the set of golden files is exactly the set
		/// the cases claim, so that neither a statement nobody looks at nor a file nothing produces can sit in the tree.
		/// </summary>
		/// <remarks>
		/// One case rather than one per corpus entry because the statement cases have to run in order (a table is
		/// created once, and the scratch table is created, altered and dropped), and because the set comparison needs
		/// every case's answer. A test that needed its siblings to have run first would give the wrong answer under the
		/// name filters AGENTS.md runs the suite with.
		/// </remarks>
		[Test]
		public void EveryGeneratedStatementMatchesItsGolden()
		{
			bool updating = Updating;
			List<string> problems = new List<string>();
			HashSet<string> claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int written = 0;
			int deleted = 0;

			foreach (TranslationCase translationCase in Cases())
			{
				// Claimed before it is generated, not after: a golden belongs to the case named on it whether or not translating
				// that case worked. Claimed afterwards, a case that threw leaves its file unclaimed - reported a second time as a
				// golden nobody produces, and in an update run deleted outright, which is the one thing these files may never
				// lose: there is no reference runtime to regenerate them from.
				claimed.Add(translationCase.GoldenFileName);

				string generated;
				try
				{
					generated = Generate(translationCase);
				}
				catch (Exception exception)
				{
					problems.Add(String.Format("{0}: translating failed: {1}", translationCase, FrontendFixture.FlattenMessages(exception)));
					continue;
				}

				string normalized = Normalize(generated);

				if (String.IsNullOrEmpty(normalized))
				{
					problems.Add(String.Format("{0}: the device generated nothing. A case that pins an empty translation pins nothing; find out what stopped generating.", translationCase));
					continue;
				}

				if (updating)
				{
					WriteGolden(translationCase.GoldenFileName, normalized);
					written++;
					continue;
				}

				string golden = ReadGolden(translationCase.GoldenFileName);
				if (golden == null)
					problems.Add(String.Format("{0}: no golden file. Create \"{1}\" by running with {2}=1.", translationCase, translationCase.GoldenFileName, UpdateVariable));
				else if (golden != normalized)
					problems.Add(String.Format("{0}: the generated SQL no longer matches \"{1}\". {2}", translationCase, translationCase.GoldenFileName, FirstDifference(golden, normalized)));
			}

			foreach (string existing in ExistingGoldens())
			{
				if (claimed.Contains(existing))
					continue;
				if (updating)
				{
					File.Delete(GoldenPath(existing));
					deleted++;
				}
				else
					problems.Add(String.Format("\"{0}\" is a golden no case produces. Delete it, or find out which case stopped being translated.", existing));
			}

			if (updating)
				Assert.Fail(String.Format("{0}=1: wrote {1} golden file(s) and deleted {2}. An update run always fails, so that it cannot be mistaken for a passing one - the diff is the review, so read it before committing it.{3}", UpdateVariable, written, deleted, Report(problems)));

			if (problems.Count > 0)
				Assert.Fail(String.Format("{0} generated statement(s) or golden file(s) do not agree with the checked-in set. The expected values may never be edited to make a change pass - see the remarks on this class. Run with {1}=1 to rewrite them, then read every difference.{2}", problems.Count, UpdateVariable, Report(problems)));
		}

		/// <summary>
		/// What the device refuses to translate, which is as much a part of its behaviour as the text it produces: an
		/// expression it cannot push down must raise rather than silently come back translated or half-translated.
		/// </summary>
		[Test]
		public void TheDeviceRefusesWhatItCannotTranslate()
		{
			List<string> problems = new List<string>();
			foreach (UnsupportedCase unsupported in UnsupportedCases)
			{
				try
				{
					string translated = TranslateQuery(unsupported.D4);
					problems.Add(String.Format("{0}: expected error {1}, but the expression translated to: {2}", unsupported.Name, unsupported.ErrorCode, translated));
				}
				catch (Exception exception)
				{
					if (FindError(exception, unsupported.ErrorCode) == null)
						problems.Add(String.Format("{0}: expected error {1}, but the failure was: {2}", unsupported.Name, unsupported.ErrorCode, FrontendFixture.FlattenMessages(exception)));
				}
			}

			Assert.That(problems, Is.Empty, String.Join("\n  ", problems));
		}

		/// <summary>
		/// A read the recorder has no answer for fails rather than coming back empty, and the statement it refused is the one
		/// the corresponding query golden holds. The first half is what makes every golden above trustworthy: a recording
		/// connection that quietly answered nothing would let a device that generated nothing look like a device that
		/// generated the right thing.
		/// </summary>
		/// <remarks>
		/// The second half holds the two emitting paths together, and is the only case that reaches the second one. A query
		/// golden comes from D4ToSQL, which emits the device plan node's statement as the compiler left it. A real read does
		/// not go that way: SQLDeviceSession.InternalExecute builds an SQLTable, which <i>rebuilds</i> the statement before
		/// emitting it and can change it on the way - a deferred column becomes a case expression and a cursor restart
		/// appends a key condition (SQLTable's constructor and its GetMainCursor, in libraries/SQLDevice/Source/SQLDevice.cs).
		/// Nothing else here goes down that path, so without this a change that touched only it would leave every query
		/// golden green while what a database server actually receives had moved. Reading a device table is also the shape
		/// that asks for a cursor and that no case arms, which is why the two halves are one test.
		/// </remarks>
		[Test]
		public void ReadingADeviceTableIsRefusedRatherThanAnsweredWithNothing()
		{
			// The Query.Table case's expression, read through the engine for real. A foreach rather than exists: an exists
			// reaches the device as a scalar select, which is emitted on the same path D4ToSQL uses and would hold nothing
			// together.
			const string tableRead = "Employee";

			Exception failure = Assert.Catch(() => Execute(String.Format("foreach row in {0} do begin end;", tableRead)));

			Assert.That
			(
				FrontendFixture.FlattenMessages(failure),
				Does.Contain("has no answer for this cursor"),
				"Reading a device table through the recording connection must fail, because the recorder has no row to answer with and will not pretend the table is empty."
			);

			Assert.That
			(
				Recorded(),
				Does.Contain(TranslateQuery(tableRead)),
				"The statement the device handed the connection for a real read is not the text D4ToSQL emits, so the query goldens pin text no database server is sent. Read both before touching a golden."
			);
		}

		/// <summary>
		/// The device opened its connections through the recorder, at the unreachable server the create device statement
		/// names - which is the evidence that these goldens were produced without a database server anywhere.
		/// </summary>
		[Test]
		public void TheDeviceConnectedOnlyThroughTheRecorder()
		{
			foreach (string connection in RecordedConnections())
				Assert.That(connection, Does.Contain(UnreachableServerName), String.Format("The device opened a connection to something other than the unreachable placeholder server: {0}", connection));
		}

		/// <summary>
		/// Every connection string the device built carries the settings it defaults for compatibility with the release
		/// before this one. The create device statement names a server, a database and integrated security and nothing
		/// else - the shape of nearly every device definition a customer has - so whatever a default has to supply for
		/// such a definition to keep connecting has to be in the string the device handed its connection class.
		/// </summary>
		/// <remarks>
		/// The device's own ConnectionParameters setting still overrides these: SQLDevice.GetConnectionParameters applies
		/// it after the connection-string builder has run. That precedence is not asserted here because this fixture's
		/// device is created once and shared by every case; it is stated where the default is set.
		/// </remarks>
		[Test]
		public void EveryConnectionCarriesTheDevicesCompatibilityDefaults()
		{
			if (RequiredConnectionStringText.Length == 0)
				Assert.Pass("This device defaults nothing into the connection strings it builds.");

			foreach (string connection in RecordedConnections())
				foreach (string required in RequiredConnectionStringText)
					Assert.That
					(
						connection,
						Does.Contain(required),
						String.Format("A device definition that names nothing but its server, database and security must still get {0}, and this connection string has not got it: {1}", required, connection)
					);
		}

		/// <summary>At most this many of the problems are listed; a failure that names every file of the corpus is unreadable.</summary>
		private const int ReportLimit = 25;

		private static string Report(List<string> problems)
		{
			if (problems.Count == 0)
				return String.Empty;
			StringBuilder result = new StringBuilder();
			foreach (string problem in problems.Take(ReportLimit))
				result.Append("\n  ").Append(problem);
			if (problems.Count > ReportLimit)
				result.Append(String.Format("\n  ... and {0} more.", problems.Count - ReportLimit));
			return result.ToString();
		}

		/// <summary>Where two texts first differ, as a line number and the two lines, so that a failure says what changed.</summary>
		private static string FirstDifference(string expected, string actual)
		{
			string[] expectedLines = expected.Split('\n');
			string[] actualLines = actual.Split('\n');
			for (int index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
			{
				string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<end of file>";
				string actualLine = index < actualLines.Length ? actualLines[index] : "<end of file>";
				if (expectedLine != actualLine)
					return String.Format("First difference at line {0}:\n    golden:    {1}\n    generated: {2}", index + 1, Excerpt(expectedLine), Excerpt(actualLine));
			}
			return "The two texts differ although no line does, so the difference is in the line endings.";
		}

		private const int ExcerptLength = 160;

		private static string Excerpt(string line)
		{
			return line.Length <= ExcerptLength ? line : line.Substring(0, ExcerptLength) + "...";
		}
	}
}
