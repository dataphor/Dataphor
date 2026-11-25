/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.SQLRecorder
{
	using System;

	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	/// <summary>
	/// The statements <see cref="Recorder"/> holds, newline separated. The way a test outside the server reads the
	/// recording: the server loads this library's own copy of the assembly, so the static the recorder writes is not the
	/// one a test assembly would see.
	/// </summary>
	public class SQLRecorderStatementsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Recorder.Statements;
		}
	}

	/// <summary>The connection strings <see cref="Recorder"/> holds, newline separated.</summary>
	public class SQLRecorderConnectionsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Recorder.Connections;
		}
	}

	/// <summary>Forgets the recording and every armed cursor, so that the next case starts from nothing.</summary>
	public class SQLRecorderClearNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Recorder.Clear();
			return null;
		}
	}

	/// <summary>
	/// Allows a cursor whose statement contains a fragment to be opened, answering one row of one named column holding
	/// an integer. See <see cref="Recorder.ArmCursor"/> for why the caller rather than the recorder says what to answer.
	/// </summary>
	public class SQLRecorderArmCursorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Recorder.ArmCursor((string)arguments[0], (string)arguments[1], (int)arguments[2]);
			return null;
		}
	}
}
