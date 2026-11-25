/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.SQLRecorder
{
	using System;
	using System.IO;
	using System.Text;

	using Alphora.Dataphor.DAE.Connection;

	/// <summary>
	/// A <see cref="SQLConnection"/> that records what a device asks of a database server instead of opening one. Named
	/// by a device's ConnectionClass attribute, it makes the device's translation observable with no server anywhere:
	/// the device starts, its tables are created and its statements are generated, and every statement lands in
	/// <see cref="Recorder"/>.
	/// </summary>
	/// <remarks>
	/// NOTE: a device defined with a recording connection still connects for data definition even when its reconcile
	/// mode says not to - SQLDeviceSession.InternalExecute's DDLNode branch calls RequestConnection before it tests
	/// Device.ReconcileMode (libraries/SQLDevice/Source/SQLDevice.cs). That is why this class exists rather than a
	/// device configured with reconciliation { mode = { none } }, and why the recording of a create table is the
	/// device's real data definition rather than nothing.
	/// </remarks>
	public class RecordingConnection : SQLConnection
	{
		/// <summary>The name a create device statement's ConnectionClass attribute carries; see Register.cs.</summary>
		public const string ClassName = "SQLRecorder.RecordingConnection";

		/// <summary>Records the connection string; nothing is opened, so the connection is idle at once.</summary>
		public RecordingConnection(string connectionString) : base()
		{
			Recorder.RecordConnection(connectionString);
			SetState(SQLConnectionState.Idle);
		}

		protected override SQLCommand InternalCreateCommand()
		{
			return new RecordingCommand(this);
		}

		// No statement reaches a server, so there is nothing for a transaction to hold. The device's transaction calls
		// are still made, and they must not fail: the engine opens a device transaction around the work of a case.
		protected override void InternalBeginTransaction(SQLIsolationLevel isolationLevel) { }

		protected override void InternalCommitTransaction() { }

		protected override void InternalRollbackTransaction() { }

		public override bool IsConnectionValid()
		{
			return true;
		}
	}

	/// <summary>
	/// Records the statement a device built, with the parameters it bound. Execution records and returns; a cursor is
	/// answered only from an armed canned row (<see cref="Recorder.ArmCursor"/>) and otherwise refused.
	/// </summary>
	public class RecordingCommand : SQLCommand
	{
		public RecordingCommand(RecordingConnection connection) : base(connection)
		{
		}

		protected override void InternalPrepare() { }

		protected override void InternalUnprepare() { }

		protected override void InternalExecute()
		{
			Recorder.RecordStatement(Describe(ExecuteVerb));
		}

		// NOTE: the canned cursor answers one integer column, which is what the engine's key check before an insert
		// asks for ("select case when not(exists(...)) then 1 else 0 end as dummy1 ..."), and that is as far as the
		// recorder goes. An update or a delete of a device table asks instead for the row itself - measured
		// 2026-11-25: 'select "T1"."ID", "T1"."Note", "T1"."Amount" from "Ledger" as "T1" (updlock) where
		// ("T1"."ID" = 1) option (fast 1)' - so answering it would mean returning typed values for every mapped
		// column, which is a database rather than a recorder. Update and delete translation therefore belongs to
		// test-device-live, against a real server; if that ever needs a fake instead, the arming operator is where a
		// multi-column, typed canned row would be added.
		protected override SQLCursor InternalOpen(SQLCursorType cursorType, SQLIsolationLevel cursorIsolationLevel)
		{
			CannedCursor canned = Recorder.FindCannedCursor(Statement);
			if (canned == null)
				throw new InvalidOperationException
				(
					String.Format
					(
						"The SQL recorder has no answer for this cursor and will not pretend it is empty ({0}). Arm one with SQLRecorderArmCursor, or leave this shape to a test against a real database server. The statement was: {1}",
						Recorder.DescribeArmedCursors(),
						Statement
					)
				);

			Recorder.RecordStatement(Describe(OpenVerb));
			return new RecordingCursor(this, canned);
		}

		// Declared protected, not protected internal: an override of a protected internal member of another assembly
		// cannot keep the internal half (CS0507).
		protected override void InternalClose() { }

		/// <summary>What a recorded execution of a statement starts with.</summary>
		public const string ExecuteVerb = "EXECUTE";

		/// <summary>What a recorded cursor open starts with.</summary>
		public const string OpenVerb = "OPEN";

		/// <summary>
		/// One recorded entry: the verb, the statement as the device's emitter produced it, and - on a line of its own -
		/// the parameters it bound.
		/// </summary>
		/// <remarks>
		/// The statement is <see cref="SQLCommand.Statement"/> rather than PrepareStatement's result, because the
		/// emitted text is what a golden file is for; what a particular client does to it afterwards (whitespace,
		/// parameter markers) belongs to the client, which these tests deliberately replace.
		/// <para>
		/// A parameter's name, direction and type are recorded and its value is not: the name and type are the device's
		/// translation, while the value is the test's own data and would make a golden churn on a value a case happens
		/// to use.
		/// </para>
		/// </remarks>
		private string Describe(string verb)
		{
			StringBuilder result = new StringBuilder();
			result.Append(verb).Append(' ').Append(Statement);
			if (Parameters.Count > 0)
			{
				result.Append("\nPARAMETERS ");
				for (int index = 0; index < Parameters.Count; index++)
				{
					if (index > 0)
						result.Append(", ");
					result.Append(DescribeParameter(Parameters[index]));
				}
			}
			return result.ToString();
		}

		private static string DescribeParameter(SQLParameter parameter)
		{
			string direction = parameter.Direction == SQLDirection.In ? String.Empty : String.Format(" {0}", parameter.Direction.ToString().ToLowerInvariant());
			return String.Format("@{0}{1} : {2}", parameter.Name, direction, DescribeType(parameter.Type));
		}

		/// <summary>
		/// A parameter's SQL type, written as the type's own kind and whatever sizing it carries. Deliberately not
		/// ToString() on the type object: these names reach golden files, so they have to be stable and readable.
		/// </summary>
		private static string DescribeType(SQLType type)
		{
			switch (type)
			{
				case null: return "<none>";
				case SQLStringType stringType: return String.Format("String({0})", stringType.Length);
				case SQLBooleanType: return "Boolean";
				case SQLIntegerType integerType: return String.Format("Integer({0})", integerType.ByteCount);
				case SQLNumericType numericType: return String.Format("Numeric({0},{1})", numericType.Precision, numericType.Scale);
				case SQLFloatType floatType: return String.Format("Float({0})", floatType.Width);
				case SQLMoneyType: return "Money";
				case SQLTextType: return "Text";
				case SQLBinaryType: return "Binary";
				case SQLByteArrayType byteArrayType: return String.Format("ByteArray({0})", byteArrayType.Length);
				case SQLDateTimeType: return "DateTime";
				case SQLDateType: return "Date";
				case SQLTimeType: return "Time";
				case SQLGuidType: return "Guid";
				default: return type.GetType().Name;
			}
		}
	}

	/// <summary>
	/// The single row of one column an armed cursor answers with. Everything a cursor can be asked beyond that one
	/// column throws, so a read this recorder cannot honestly serve fails rather than returning something made up.
	/// </summary>
	public class RecordingCursor : SQLCursor
	{
		public RecordingCursor(RecordingCommand command, CannedCursor canned) : base(command)
		{
			_canned = canned;
		}

		private readonly CannedCursor _canned;
		private bool _read;

		protected override SQLTableSchema InternalGetSchema()
		{
			SQLTableSchema result = new SQLTableSchema();
			result.Columns.Add(new SQLColumn(_canned.ColumnName, new SQLDomain(typeof(int), 0, 0, 0, false)));
			return result;
		}

		protected override bool InternalNext()
		{
			if (_read)
				return false;
			_read = true;
			return true;
		}

		protected override int InternalGetColumnCount()
		{
			return 1;
		}

		protected override string InternalGetColumnName(int index)
		{
			CheckColumn(index);
			return _canned.ColumnName;
		}

		protected override object InternalGetColumnValue(int index)
		{
			CheckColumn(index);
			CheckRead();
			return _canned.Value;
		}

		protected override bool InternalIsNull(int index)
		{
			CheckColumn(index);
			CheckRead();
			return false;
		}

		protected override bool InternalIsDeferred(int index)
		{
			CheckColumn(index);
			return false;
		}

		protected override Stream InternalOpenDeferredStream(int index)
		{
			throw new InvalidOperationException(String.Format("The SQL recorder's canned cursor has no deferred value for column {0}; it answers one integer column.", index));
		}

		private void CheckColumn(int index)
		{
			if (index != 0)
				throw new InvalidOperationException(String.Format("The SQL recorder's canned cursor was asked for column {0}; it answers one column ({1}) and will not invent another.", index, _canned.ColumnName));
		}

		private void CheckRead()
		{
			if (!_read)
				throw new InvalidOperationException("The SQL recorder's canned cursor was read before Next() placed it on its one row.");
		}
	}
}
