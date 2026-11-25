/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class CursorValue : DataValue
	{
		public CursorValue(IValueManager manager, Schema.ICursorType cursorType, int iD) : base(manager, cursorType)
		{
			_iD = iD;
		}
		
		private int _iD;
		public int ID { get { return _iD; } }
		
		public override object AsNative
		{
			get { return _iD; }
			set { _iD = (int)value; }
		}
		
		public override bool IsNil { get { return false; } }

		public override int GetPhysicalSize(bool expandStreams)
		{
			return sizeof(int);
		}

		#if USE_UNSAFE 

		public unsafe override void WriteToPhysical(byte[] ABuffer, int offset, bool AExpandStreams)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((int*)LBufferPtr) = FID;
			}
		}
		
		public unsafe override void ReadFromPhysical(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				FID = *((int*)LBufferPtr);
			}
		}

		#else

		public override void WriteToPhysical(byte[] buffer, int offset, bool expandStreams)
		{
			ByteArrayUtility.WriteInt32(buffer, offset, _iD);
		}

		public override void ReadFromPhysical(byte[] buffer, int offset)
		{
			_iD = ByteArrayUtility.ReadInt32(buffer, offset);
		}

		#endif
		
		public override object CopyNativeAs(Schema.IDataType dataType)
		{
			return _iD;
		}
	}

	// Cursors
    public class Cursor : Disposable
    {
		protected internal Cursor(CursorManager manager, int iD, ITable table) : base()
		{
			_iD = iD;
			_table = table;
			_manager = manager;
			SnapshotContext(table.Program);
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				_iD = -1;
				
				try
				{
					if (_table != null)
					{
						_table.Dispose();
						_table = null;
					}
				}
				finally
				{
					// The snapshot is the cursor's own copy of the stack it was opened over, and nothing else will give
					// those values back: one stream per stream-backed value in scope, per cursor opened, was held for the
					// life of the session (bug-a-cursor-copies-the-stack-and-never-gives-it-back). ReleaseAbove rather
					// than a loop, so that a dispose that throws cannot leave a disposed value reachable. The manager is
					// the one captured when the snapshot was taken, which is what frees this of the dispose order above:
					// _table is already gone by the time control reaches here, so _table.Program is not reachable for it.
					if (_context != null)
					{
						_context.ReleaseAbove(0, _contextValueManager);
						_context = null;
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		protected Stack _context;
		
		/// <summary>The value manager the snapshot's copies were made with, so that Dispose can release them.</summary>
		private IValueManager _contextValueManager;
		
		/// <summary>The process whose managers the snapshot was copied with, and whose disposal therefore closes this cursor.</summary>
		/// <remarks>
		/// Recorded here rather than read back from Table.Program when it is needed, so that it cannot disagree with
		/// _contextValueManager: the two come from the same program at the same moment, and the process that has to close
		/// the cursor is by definition the one whose managers the release will run through. Program.ServerProcess is not
		/// a fixed answer - ServerPlan.BindToProcess re-points a cached plan's program at whichever process next takes
		/// that plan out of the server's plan cache - so a filter reading it could miss a cursor whose plan had been
		/// reused, and that cursor would reach CursorManager.Dispose holding a process already torn down.
		/// </remarks>
		private Server.ServerProcess _contextProcess;
		
		/// <summary>Whether this cursor's snapshot was copied with the given process's managers.</summary>
		internal bool BelongsTo(Server.ServerProcess process)
		{
			return (_contextProcess != null) && (_contextProcess == process);
		}
		
		/// <summary>Takes the cursor's private copy of the stack window it is being opened over.</summary>
		/// <remarks>
		/// The copy is the isolation: SwitchContext installs this stack as the program's for the duration of each cursor
		/// operation, so the cursor's plan reads the variables it was compiled against at the offsets it was compiled
		/// with - and a cursor outlives the scope that opened it, which releases its own variables on the way out, so
		/// sharing the live stack would leave the cursor reading released streams. Dispose is the other half.
		/// Called once, from the constructor: a second call would orphan the first snapshot outright.
		/// </remarks>
		private void SnapshotContext(Program program)
		{
			if (program != null)
			{
				_contextValueManager = program.ValueManager;
				_contextProcess = program.ServerProcess;
				_context = new Stack(program.Stack.MaxStackDepth, program.Stack.MaxCallDepth);
				_context.PushWindow(0);
				for (int index = program.Stack.Count - 1; index >= 0; index--)
					_context.Push(DataValue.CopyValue(program.ValueManager, program.Stack.Peek(index)));
			}
		}
		
		public void SwitchContext(Program program)
		{
			if (program != null)
				_context = program.SwitchContext(_context);
		}
		
		// ID
		protected int _iD = -1;
		public int ID { get { return _iD; } }
		
		// Table
		protected ITable _table;
		public ITable Table { get { return _table; } }
		
		// Manager
		protected CursorManager _manager;
 		public CursorManager Manager { get { return _manager; } }
    }
    
    public class Cursors : DisposableList<Cursor>
    {		
		public Cursors() : base(true) { }
		
		public Cursor GetCursor(int cursorID)
		{
			foreach (Cursor cursor in this)
				if (cursor.ID == cursorID)
					return cursor;
			throw new RuntimeException(RuntimeException.Codes.CursorNotFound, cursorID.ToString());
		}
    }
    
    public class CursorManager : Disposable
    {
		public CursorManager() : base(){}
		
		protected int _nextCursorID = -1;
		
		// NOTE: nothing in this class is synchronised, and one manager is shared by every process in the session, so
		// two processes creating, closing or walking cursors at once race on this list. Pre-existing - CreateCursor and
		// CloseCursor have always been reachable from any process of the session - and CloseProcessCursors is one more
		// unsynchronised walk, not a new exposure. If a client is ever seen using cursors from two processes of one
		// session concurrently, the lock belongs here rather than at each caller.
		protected Cursors _cursors = new Cursors();
		
		// NOTE: this is the session-end fallback, and it releases each cursor's snapshot through the value manager that
		// cursor captured - so it is only safe while every cursor left here belongs to a process that is still alive.
		// CloseProcessCursors, called from ServerProcess.InternalDispose, is what keeps that true: a process takes its
		// own cursors with it. A future path that disposes a process without reaching InternalDispose would leave one
		// here holding a torn-down process, and the symptom is a null dereference inside the release at disconnect.
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_cursors != null) 
				{
					while (_cursors.Count > 0)
						_cursors[0].Dispose();
					_cursors.Dispose();
					_cursors = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		public int CreateCursor(ITable table)
		{
			table.Open(); // JIC
			_nextCursorID++;
			Cursor cursor = new Cursor(this, _nextCursorID, table);
			try
			{
				_cursors.Add(cursor);
				return cursor.ID;
			}
			catch
			{
				cursor.Dispose();
				throw;
			}
		}
		
		public Cursor GetCursor(int cursorID)
		{
			return _cursors.GetCursor(cursorID);
		}
		
		public void CloseCursor(int cursorID)
		{
			GetCursor(cursorID).Dispose();
		}
		
		/// <summary>Disposes the cursors opened by the given process.</summary>
		/// <remarks>
		/// A Cursor holds an ITable bound to a program on a process, and a copy of that process's stack values which it
		/// releases through that process's managers, so it is unusable the moment the process is gone. The manager lives
		/// on the session and is disposed after the session's processes are, so a cursor the script never closed used to
		/// be disposed with its process already torn down; closing it with the process is what makes releasing the
		/// snapshot safe. Which process that is comes from the cursor itself (Cursor.BelongsTo), recorded when the
		/// snapshot was taken, for the reason given there. Backwards through the list, because Cursors disowns a member
		/// that disposes itself.
		/// </remarks>
		public void CloseProcessCursors(Server.ServerProcess process)
		{
			if (_cursors != null)
				for (int index = _cursors.Count - 1; index >= 0; index--)
				{
					Cursor cursor = _cursors[index];
					if (cursor.BelongsTo(process))
						cursor.Dispose();
				}
		}
	}
}