/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// ServerCursor    
	public class ServerCursor : ServerChildObject, IServerCursor
	{
		public ServerCursor(ServerExpressionPlan plan, Program program, DataParams paramsValue) : base() 
		{
			_plan = plan;
			_program = program;
			_params = paramsValue;
		}

		private bool _disposed;
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				Close();
				
				if (!_disposed)
				{
					_disposed = true;
				}
			}
			finally
			{
				_params = null;
				_plan = null;
				
				base.Dispose(disposing);
			}
		}
		
		protected Exception WrapException(Exception exception)
		{
			return _plan.ServerProcess.ServerSession.WrapException(exception);
		}
		
		private ServerExpressionPlan _plan;
		public ServerExpressionPlan Plan { get { return _plan; } }
		
		private Program _program;
		private bool _programStarted;
		
		IServerExpressionPlan IServerCursor.Plan { get { return _plan; } }

		// IActive

		// Open        
		public void Open()
		{
			if (!_active)
			{
				#if USESERVERCURSOREVENTS
				DoBeforeOpen();
				#endif
				InternalOpen();
				_active = true;
				#if USESERVERCURSOREVENTS
				DoAfterOpen();
				#endif
			}
		}
        
		// Close
		public void Close()
		{
			if (_active)
			{
				#if USESERVERCURSOREVENTS
				DoBeforeClose();
				#endif
				InternalClose();
				_active = false;
				#if USESERVERCURSOREVENTS
				DoAfterClose();
				#endif
			}
		}
        
		// Active
		protected bool _active;
		public bool Active
		{
			get { return _active; }
			set
			{
				if (value)
					Open();
				else
					Close();
			}
		}
        
		protected void CheckActive()
		{
			if (!Active)
				throw new ServerException(ServerException.Codes.CursorInactive);
		}
        
		protected void CheckInactive()
		{
			if (Active)
				throw new ServerException(ServerException.Codes.CursorActive);
		}
        
		protected PlanNode SourceNode { get { return _plan.SourceNode; } }
		
		protected DataParams _params;
		protected ITable _sourceTable;
		protected Schema.IRowType _sourceRowType;
		public Schema.IRowType SourceRowType { get { return _sourceRowType; } }
		
		protected IStreamManager StreamManager { get { return (IStreamManager)_plan.Process; } }
				
		protected virtual void InternalOpen()
		{
			// get a table object to supply the data
			_plan.SetActiveCursor(this);
			try
			{
				_program.Start(_params);
				_programStarted = true;

				long startTicks = TimingUtility.CurrentTicks;

				CursorNode cursorNode = _plan.CursorNode;
				//LCursorNode.EnsureApplicationTransactionJoined(FPlan.ServerProcess);
				_sourceTable = (ITable)_plan.CursorNode.SourceNode.Execute(_program);
				_sourceTable.Open();
				_sourceRowType = _sourceTable.DataType.RowType;
				
				_program.Statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch
			{
				InternalClose();
				throw;
			}
		}
		
		protected virtual void InternalClose()
		{
			try
			{
				try
				{
					try
					{
						if (_bookmarks != null)
						{
							InternalDisposeBookmarks();
							_bookmarks = null;
						}
					}
					finally
					{
						if (_sourceTable != null)
						{
							_sourceTable.Dispose();
							_sourceTable = null;
							_sourceRowType = null;
						}
					}
				}
				finally
				{
					if (_programStarted)
						_program.Stop(_params);
				}
			}
			finally
			{
				_plan.ClearActiveCursor();
			}
		}
		
		// Isolation
		public CursorIsolation Isolation { get { return _sourceTable.Isolation; } }
		
		// CursorType
		public CursorType CursorType { get { return _sourceTable.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return _sourceTable.Capabilities; } }

		public bool Supports(CursorCapability capability)
		{
			return (Capabilities & capability) != 0;
		}

		protected void CheckCapability(CursorCapability capability)
		{
			if (!Supports(capability))
				throw new ServerException(ServerException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), capability));
		}

		#if USESERVERCURSOREVENTS
		// Events
		public event EventHandler BeforeOpen;
		protected virtual void DoBeforeOpen()
		{
			if (BeforeOpen != null)
				BeforeOpen(this, EventArgs.Empty);
		}
        
		public event EventHandler AfterOpen;
		protected virtual void DoAfterOpen()
		{
			if (AfterOpen != null)
				AfterOpen(this, EventArgs.Empty);
		}
        
		public event EventHandler BeforeClose;
		protected virtual void DoBeforeClose()
		{
			if (BeforeClose != null)
				BeforeClose(this, EventArgs.Empty);

		}
        
		public event EventHandler AfterClose;
		protected virtual void DoAfterClose()
		{
			if (AfterClose != null)
				AfterClose(this, EventArgs.Empty);
		}
		#endif

		private Bookmarks _bookmarks = new Bookmarks();

		protected Guid InternalGetBookmark()
		{
			Guid result = Guid.NewGuid();
			_bookmarks.Add(result, _sourceTable.GetBookmark());
			return result;
		}

		protected bool InternalGotoBookmark(Guid bookmark, bool forward)
		{
			IRow row;
			if (!_bookmarks.TryGetValue(bookmark, out row))
				throw new ServerException(ServerException.Codes.InvalidBookmark, bookmark);
			return _sourceTable.GotoBookmark(_bookmarks[bookmark], forward);
		}

		protected int InternalCompareBookmarks(Guid bookmark1, Guid bookmark2)
		{
			return _sourceTable.CompareBookmarks(_bookmarks[bookmark1], _bookmarks[bookmark2]);
		}

		protected void InternalDisposeBookmark(Guid bookmark)
		{
			IRow internalBookmark = null;
			_bookmarks.TryGetValue(bookmark, out internalBookmark);
			_bookmarks.Remove(bookmark);
			if (internalBookmark != null)
				internalBookmark.Dispose();
		}

		protected void InternalDisposeBookmarks(Guid[] bookmarks)
		{
			foreach (Guid bookmark in bookmarks)
				InternalDisposeBookmark(bookmark);
		}
		
		protected void InternalDisposeBookmarks()
		{
			Guid[] keys = new Guid[_bookmarks.Keys.Count];
			_bookmarks.Keys.CopyTo(keys, 0);
			InternalDisposeBookmarks(keys);
		}

		// cursor support		
		public void Reset()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				_sourceTable.Reset();
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool Next()
		{
			bool result;
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				result = _sourceTable.Next();
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
			return result;
		}
		
		public void Last()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				_sourceTable.Last();
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool BOF()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{		
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.BOF();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool EOF()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.EOF();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool IsEmpty()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.BOF() && _sourceTable.EOF();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public IRow Select()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.Select();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void Select(IRow row)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				_sourceTable.Select(row);
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void First()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				_sourceTable.First();
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool Prior()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.Prior();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public Guid GetBookmark()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetBookmark();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		public bool GotoBookmark(Guid bookmark, bool forward)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGotoBookmark(bookmark, forward);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public int CompareBookmarks(Guid bookmark1, Guid bookmark2)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalCompareBookmarks(bookmark1, bookmark2);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		/// <summary> Disposes a bookmark. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmark(Guid bookmark)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				InternalDisposeBookmark(bookmark);
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] bookmarks)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				InternalDisposeBookmarks(bookmarks);
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public Schema.Order Order { get { return _sourceTable.Order; } }
		
		public IRow GetKey()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.GetKey();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool FindKey(IRow key)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.FindKey(key);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void FindNearest(IRow key)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				_sourceTable.FindNearest(key);
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool Refresh(IRow row)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.Refresh(row);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void Insert(IRow row)
		{
			Insert(row, null);
		}
		
		public void Insert(IRow row, BitArray valueFlags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				_sourceTable.Insert(null, row, valueFlags, false);
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void Update(IRow row)
		{
			Update(row, null);
		}
		
		public void Update(IRow row, BitArray valueFlags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				_sourceTable.Update(row, valueFlags, false);
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void Delete()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				_sourceTable.Delete();
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void Truncate()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Truncateable);
				_sourceTable.Truncate();
				_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public int RowCount()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return _sourceTable.RowCount();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		public bool Default(IRow row, string columnName)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Default(_plan.Program, null, row, null, columnName);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool Change(IRow oldRow, IRow newRow, string columnName)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Change(_plan.Program, oldRow, newRow, null, columnName);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool Validate(IRow oldRow, IRow newRow, string columnName)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Validate(_plan.Program, oldRow, newRow, null, columnName);
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		private CursorGetFlags InternalGetFlags()
		{
			CursorGetFlags getFlags = CursorGetFlags.None;
			if (_sourceTable.BOF())
				getFlags = getFlags | CursorGetFlags.BOF;
			if (_sourceTable.EOF())
				getFlags = getFlags | CursorGetFlags.EOF;
			return getFlags;
		}

		public CursorGetFlags GetFlags()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetFlags();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		// Fetch
		public int Fetch(IRow[] rows, Guid[] bookmarks, int count, bool skipCurrent, out CursorGetFlags flags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					int localCount = 0;
					while (count != 0)
					{
						if (count > 0)
						{
							if ((localCount == 0) && skipCurrent && !_sourceTable.Next())
								break;							   

							if ((localCount > 0) && (!_sourceTable.Next()))
								break;

							_sourceTable.Select(rows[localCount]);
							bookmarks[localCount] = Supports(CursorCapability.Bookmarkable) ? InternalGetBookmark() : Guid.Empty;
							count--;
						}
						else
						{
							if ((localCount == 0) && skipCurrent && !_sourceTable.Prior())
								break;
								
							if ((localCount > 0) && (!_sourceTable.Prior()))
								break;

							_sourceTable.Select(rows[localCount]);
							bookmarks[localCount] = Supports(CursorCapability.Bookmarkable) ? InternalGetBookmark() : Guid.Empty;
							count++;
						}
						localCount++;
					}
					flags = InternalGetFlags();
					return localCount;
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		// MoveBy
		public int MoveBy(int delta, out CursorGetFlags flags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					int localDelta = 0;
					while (delta != 0)
					{
						if (delta > 0)
						{
							if (!_sourceTable.Next())
								break;
							delta--;
						}
						else
						{
							if (!_sourceTable.Prior())
								break;
							delta++;
						}
						localDelta++;
					}
					flags = InternalGetFlags();
					return localDelta;
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		// MoveTo
		public CursorGetFlags MoveTo(bool first)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					if (first)
						_sourceTable.First();
					else
						_sourceTable.Last();
					return InternalGetFlags();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public CursorGetFlags ResetWithFlags()
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					_sourceTable.Reset();
					return InternalGetFlags();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		public bool GotoBookmark(Guid bookmark, bool forward, out CursorGetFlags flags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					bool success = InternalGotoBookmark(bookmark, forward);
					flags = InternalGetFlags();
					return success;
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}

		public bool FindKey(IRow key, out CursorGetFlags flags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					bool success = _sourceTable.FindKey(key);
					flags = InternalGetFlags();
					return success;
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public void FindNearest(IRow key, out CursorGetFlags flags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					_sourceTable.FindNearest(key);
					flags = InternalGetFlags();
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
		
		public bool Refresh(IRow row, out CursorGetFlags flags)
		{
			Exception exception = null;
			int nestingLevel = _plan.ServerProcess.BeginTransactionalCall();
			try
			{
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
					bool success = _sourceTable.Refresh(row);
					flags = InternalGetFlags();
					return success;
				}
				finally
				{
					_program.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(startTicks);
				}
			}
			catch (Exception E)
			{
				exception = E;
				throw WrapException(E);
			}
			finally
			{
				_plan.ServerProcess.EndTransactionalCall(nestingLevel, exception);
			}
		}
	}
}
