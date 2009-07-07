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
		public static List<String> ProcessBatches(string AScript, string ATerminator)
		{
			List<String> LBatches = new List<String>();
			
			string[] LLines = AScript.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
			StringBuilder LBatch = new StringBuilder();
			for (int LIndex = 0; LIndex < LLines.Length; LIndex++)
			{
				if (LLines[LIndex].IndexOf(ATerminator, StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					LBatches.Add(LBatch.ToString());
					LBatch = new StringBuilder();
				}
				else
				{
					LBatch.Append(LLines[LIndex]);
					LBatch.Append("\r\n");
				}
			}

			if (LBatch.Length > 0)
				LBatches.Add(LBatch.ToString());
				
			return LBatches;
		}
	}
}
