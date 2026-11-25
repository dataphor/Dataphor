/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.SQLRecorder
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// What the recording connection saw: the statements a device asked a database server to run, and the connection
	/// strings it opened. A D4 operator reads this back, which is how a test in another assembly reaches it - the server
	/// loads this library's own copy of the assembly, so a static here is not the one a test assembly would see.
	/// </summary>
	/// <remarks>
	/// The recorder answers nothing it has not been told to answer. A cursor the fixture has not armed makes
	/// <see cref="RecordingCommand.InternalOpen"/> throw rather than return an empty result, because a golden file
	/// produced by a device whose read quietly returned nothing would pin a lie.
	/// </remarks>
	public static class Recorder
	{
		// One server per fixture and one case at a time, but the engine runs device work on whichever thread holds the
		// process, so the lists are guarded rather than assumed single-threaded.
		private static readonly object _lock = new object();
		private static readonly List<string> _statements = new List<string>();
		private static readonly List<string> _connections = new List<string>();
		private static readonly List<CannedCursor> _cannedCursors = new List<CannedCursor>();

		/// <summary>Records a connection string a device opened a connection with.</summary>
		public static void RecordConnection(string connectionString)
		{
			lock (_lock)
				_connections.Add(connectionString);
		}

		/// <summary>Records one statement, as <see cref="RecordingCommand"/> describes it.</summary>
		public static void RecordStatement(string statement)
		{
			lock (_lock)
				_statements.Add(statement);
		}

		/// <summary>The statements recorded since the last <see cref="Clear"/>, newline separated, ending in a newline.</summary>
		/// <remarks>Empty when nothing was recorded, so that a golden of an empty recording is an empty file rather than a blank line.</remarks>
		public static string Statements
		{
			get
			{
				lock (_lock)
					return Join(_statements);
			}
		}

		/// <summary>The connection strings recorded since the last <see cref="Clear"/>, in the same shape as <see cref="Statements"/>.</summary>
		public static string Connections
		{
			get
			{
				lock (_lock)
					return Join(_connections);
			}
		}

		private static string Join(List<string> entries)
		{
			StringBuilder result = new StringBuilder();
			foreach (string entry in entries)
				result.Append(entry).Append('\n');
			return result.ToString();
		}

		/// <summary>Forgets everything recorded and every armed cursor, so that the next case starts from nothing.</summary>
		/// <remarks>
		/// The armed cursors go too: an arming that outlived the case it was made for would answer a later case's read,
		/// which is exactly the silent answer this class refuses to give.
		/// </remarks>
		public static void Clear()
		{
			lock (_lock)
			{
				_statements.Clear();
				_connections.Clear();
				_cannedCursors.Clear();
			}
		}

		/// <summary>
		/// Allows a cursor whose statement contains <paramref name="fragment"/> to be opened, answering one row of one
		/// column named <paramref name="columnName"/> holding <paramref name="value"/>.
		/// </summary>
		/// <remarks>
		/// The caller says which shape it expects and what the answer means, so that no knowledge of what the engine asks
		/// for lives in this library. An unarmed cursor is refused.
		/// </remarks>
		public static void ArmCursor(string fragment, string columnName, int value)
		{
			if (String.IsNullOrEmpty(fragment))
				throw new ArgumentException("An armed cursor needs a statement fragment to match on.", nameof(fragment));
			if (String.IsNullOrEmpty(columnName))
				throw new ArgumentException("An armed cursor needs the name of the column it answers.", nameof(columnName));

			lock (_lock)
				_cannedCursors.Add(new CannedCursor(fragment, columnName, value));
		}

		/// <summary>The armed answer for <paramref name="statement"/>, or null if none was armed for it.</summary>
		public static CannedCursor FindCannedCursor(string statement)
		{
			lock (_lock)
			{
				foreach (CannedCursor canned in _cannedCursors)
					if (statement != null && statement.Contains(canned.Fragment))
						return canned;
				return null;
			}
		}

		/// <summary>What the armed cursors are, for the message of a cursor this recorder refuses to open.</summary>
		public static string DescribeArmedCursors()
		{
			lock (_lock)
			{
				if (_cannedCursors.Count == 0)
					return "no cursor is armed";

				StringBuilder result = new StringBuilder("armed: ");
				for (int index = 0; index < _cannedCursors.Count; index++)
				{
					if (index > 0)
						result.Append("; ");
					result.Append(_cannedCursors[index]);
				}
				return result.ToString();
			}
		}
	}

	/// <summary>One armed cursor answer: the statements it covers, and the single row of one column it returns.</summary>
	public sealed class CannedCursor
	{
		public CannedCursor(string fragment, string columnName, int value)
		{
			Fragment = fragment;
			ColumnName = columnName;
			Value = value;
		}

		/// <summary>A cursor whose statement contains this text is answered by this canned row.</summary>
		public string Fragment { get; }

		/// <summary>The name of the single column the canned row has.</summary>
		public string ColumnName { get; }

		/// <summary>The single column's value.</summary>
		public int Value { get; }

		public override string ToString() => String.Format("\"{0}\" -> {1} = {2}", Fragment, ColumnName, Value);
	}
}
