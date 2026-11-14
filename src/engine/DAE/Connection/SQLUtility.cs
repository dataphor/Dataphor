/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Connection
{
	public enum SQLIsolationLevel { ReadUncommitted, ReadCommitted, RepeatableRead, Serializable }
	public enum SQLCursorType { Static, Dynamic }
	public enum SQLCursorLocation { Automatic, Client, Server }
	public enum SQLConnectionState { Closed, Idle, Executing, Reading }
	public enum SQLCommandType { Statement, Table }
	public enum SQLLockType { ReadOnly, Optimistic, Pessimistic }

	[Flags]
	public enum SQLCommandBehavior { Default = 0, SchemaOnly = 1, KeyInfo = 2 }
	
	public enum SQLDirection { In, Out, InOut, Result }
	
	public static class SQLUtility
	{
		/// <summary>Returns the set of batches in the given script, delimited by the given terminator.</summary>
		public static List<String> ProcessBatches(string script, string terminator)
		{
			List<String> batches = new List<String>();
			
			string[] lines = script.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
			StringBuilder batch = new StringBuilder();
			for (int index = 0; index < lines.Length; index++)
			{
				if (lines[index].IndexOf(terminator, StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					batches.Add(batch.ToString());
					batch = new StringBuilder();
				}
				else
				{
					batch.Append(lines[index]);
					batch.Append("\r\n");
				}
			}

			if (batch.Length > 0)
				batches.Add(batch.ToString());
				
			return batches;
		}

		public static SQLIsolationLevel IsolationLevelToSQLIsolationLevel(IsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
				case IsolationLevel.Isolated : return SQLIsolationLevel.Serializable;
				case IsolationLevel.CursorStability : return SQLIsolationLevel.ReadCommitted;
				default : return SQLIsolationLevel.ReadUncommitted;
			}
		}
	}
}
