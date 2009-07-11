/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	// ServerCursor    
	public class ServerCursor : ServerChildObject, IServerCursor
	{
		public ServerCursor(ServerExpressionPlan APlan, DataParams AParams) : base() 
		{
			FPlan = APlan;
			FParams = AParams;

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FPlan.ServerProcess.ServerSession.Server.FCursorCounter != null)
				FPlan.ServerProcess.ServerSession.Server.FCursorCounter.Increment();
			#endif
		}

		private bool FDisposed;
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Close();
				
				if (!FDisposed)
				{
					#if !DISABLE_PERFORMANCE_COUNTERS
					if (FPlan.ServerProcess.ServerSession.Server.FCursorCounter != null)
						FPlan.ServerProcess.ServerSession.Server.FCursorCounter.Decrement();
					#endif
					FDisposed = true;
				}
			}
			finally
			{
				FParams = null;
				FPlan = null;
				
				base.Dispose(ADisposing);
			}
		}
		
		protected Exception WrapException(Exception AException)
		{
			return FPlan.ServerProcess.ServerSession.WrapException(AException);
		}
		
		private ServerExpressionPlan FPlan;
		public ServerExpressionPlan Plan { get { return FPlan; } }
		
		IServerExpressionPlan IServerCursor.Plan { get { return FPlan; } }

		// IActive

		// Open        
		public void Open()
		{
			if (!FActive)
			{
				#if USESERVERCURSOREVENTS
				DoBeforeOpen();
				#endif
				InternalOpen();
				FActive = true;
				#if USESERVERCURSOREVENTS
				DoAfterOpen();
				#endif
			}
		}
        
		// Close
		public void Close()
		{
			if (FActive)
			{
				#if USESERVERCURSOREVENTS
				DoBeforeClose();
				#endif
				InternalClose();
				FActive = false;
				#if USESERVERCURSOREVENTS
				DoAfterClose();
				#endif
			}
		}
        
		// Active
		protected bool FActive;
		public bool Active
		{
			get { return FActive; }
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
        
		protected PlanNode SourceNode { get { return FPlan.Code.Nodes[0]; } }
		
		protected DataVar FSourceObject;
		protected DataParams FParams;
		protected Table FSourceTable;
		protected Schema.IRowType FSourceRowType;
		internal Schema.IRowType SourceRowType { get { return FSourceRowType; } }
		
		protected IStreamManager StreamManager { get {return (FPlan is IServerPlan) ? (IStreamManager)((IServerPlan)FPlan).Process : (IStreamManager)((IRemoteServerPlan)FPlan).Process; } }
				
		protected virtual void InternalOpen()
		{
			// get a table object to supply the data
			FPlan.SetActiveCursor(this);
			FPlan.ServerProcess.Start(FPlan, FParams);
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;

				CursorNode LCursorNode = (CursorNode)FPlan.Code;
				//LCursorNode.EnsureApplicationTransactionJoined(FPlan.ServerProcess);
				FSourceObject = LCursorNode.SourceNode.Execute(FPlan.ServerProcess);
				FSourceTable = (Table)FSourceObject.Value;
				FSourceTable.Open();
				FSourceRowType = FSourceTable.DataType.RowType;
				
				FPlan.Statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(LStartTicks);
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
						if (FBookmarks != null)
						{
							InternalDisposeBookmarks();
							FBookmarks = null;
						}
					}
					finally
					{
						if (FSourceTable != null)
						{
							FSourceTable.Dispose();
							FSourceTable = null;
							FSourceObject = null;
							FSourceRowType = null;
						}
					}
				}
				finally
				{
					FPlan.ServerProcess.Stop(FPlan, FParams);
				}
			}
			finally
			{
				FPlan.ClearActiveCursor();
			}
		}
		
		// Isolation
		public CursorIsolation Isolation { get { return FSourceTable.Isolation; } }
		
		// CursorType
		public CursorType CursorType { get { return FSourceTable.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return FSourceTable.Capabilities; } }

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}

		protected void CheckCapability(CursorCapability ACapability)
		{
			if (!Supports(ACapability))
				throw new ServerException(ServerException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), ACapability));
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

		private Bookmarks FBookmarks = new Bookmarks();

		protected Guid InternalGetBookmark()
		{
			Guid LResult = Guid.NewGuid();
			FBookmarks.Add(LResult, FSourceTable.GetBookmark());
			return LResult;
		}

		protected bool InternalGotoBookmark(Guid ABookmark, bool AForward)
		{
			Row LRow = FBookmarks[ABookmark];
			if (LRow == null)
				throw new ServerException(ServerException.Codes.InvalidBookmark, ABookmark);
			return FSourceTable.GotoBookmark(FBookmarks[ABookmark], AForward);
		}

		protected int InternalCompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			return FSourceTable.CompareBookmarks(FBookmarks[ABookmark1], FBookmarks[ABookmark2]);
		}

		protected void InternalDisposeBookmark(Guid ABookmark)
		{
			Row LInternalBookmark = FBookmarks[ABookmark];
			FBookmarks.Remove(ABookmark);
			if (LInternalBookmark != null)
				LInternalBookmark.Dispose();
		}

		protected void InternalDisposeBookmarks(Guid[] ABookmarks)
		{
			foreach (Guid LBookmark in ABookmarks)
				InternalDisposeBookmark(LBookmark);
		}
		
		protected void InternalDisposeBookmarks()
		{
			Guid[] LKeys = new Guid[FBookmarks.Keys.Count];
			FBookmarks.Keys.CopyTo(LKeys, 0);
			InternalDisposeBookmarks(LKeys);
		}

		// cursor support		
		public void Reset()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.Reset();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Next()
		{
			bool LResult;
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				LResult = FSourceTable.Next();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
			return LResult;
		}
		
		public void Last()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.Last();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool BOF()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{		
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.BOF();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool EOF()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.EOF();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool IsEmpty()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.BOF() && FSourceTable.EOF();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public Row Select()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.Select();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Select(Row ARow)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.Select(ARow);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void First()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.First();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Prior()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.Prior();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public Guid GetBookmark()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetBookmark();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		public bool GotoBookmark(Guid ABookmark, bool AForward)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGotoBookmark(ABookmark, AForward);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalCompareBookmarks(ABookmark1, ABookmark2);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Disposes a bookmark. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmark(Guid ABookmark)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				InternalDisposeBookmark(ABookmark);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] ABookmarks)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				InternalDisposeBookmarks(ABookmarks);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public Schema.Order Order { get { return FSourceTable.Node.Order; } }
		
		public Row GetKey()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.GetKey();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool FindKey(Row AKey)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.FindKey(AKey);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void FindNearest(Row AKey)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.FindNearest(AKey);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Refresh(Row ARow)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.Refresh(ARow);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Insert(Row ARow)
		{
			Insert(ARow, null);
		}
		
		public void Insert(Row ARow, BitArray AValueFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				FSourceTable.Insert(null, ARow, AValueFlags, false);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Update(Row ARow)
		{
			Update(ARow, null);
		}
		
		public void Update(Row ARow, BitArray AValueFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				FSourceTable.Update(ARow, AValueFlags, false);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Delete()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				FSourceTable.Delete();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Truncate()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Truncateable);
				FSourceTable.Truncate();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public int RowCount()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.RowCount();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		public bool Default(Row ARow, string AColumnName)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Default(FPlan.ServerProcess, null, ARow, null, AColumnName);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Change(Row AOldRow, Row ANewRow, string AColumnName)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Change(FPlan.ServerProcess, AOldRow, ANewRow, null, AColumnName);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Validate(Row AOldRow, Row ANewRow, string AColumnName)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Validate(FPlan.ServerProcess, AOldRow, ANewRow, null, AColumnName);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		private RemoteCursorGetFlags InternalGetFlags()
		{
			RemoteCursorGetFlags LGetFlags = RemoteCursorGetFlags.None;
			if (FSourceTable.BOF())
				LGetFlags = LGetFlags | RemoteCursorGetFlags.BOF;
			if (FSourceTable.EOF())
				LGetFlags = LGetFlags | RemoteCursorGetFlags.EOF;
			return LGetFlags;
		}

		internal RemoteCursorGetFlags GetFlags()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		// Fetch
		internal int Fetch(Row[] ARows, Guid[] ABookmarks, int ACount, out RemoteCursorGetFlags AFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					int LCount = 0;
					while (ACount != 0)
					{
						if (ACount > 0)
						{
							if ((LCount == 0) && FSourceTable.BOF() && !FSourceTable.Next())
								break;

							if ((LCount > 0) && (!FSourceTable.Next()))
								break;
							FSourceTable.Select(ARows[LCount]);
							ABookmarks[LCount] = InternalGetBookmark();
							ACount--;
						}
						else
						{
							if ((LCount == 0) && FSourceTable.EOF() && !FSourceTable.Prior())
								break;
								
							if ((LCount > 0) && (!FSourceTable.Prior()))
								break;
							FSourceTable.Select(ARows[LCount]);
							ABookmarks[LCount] = InternalGetBookmark();
							ACount++;
						}
						LCount++;
					}
					AFlags = InternalGetFlags();
					return LCount;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		// MoveBy
		internal int MoveBy(int ADelta, out RemoteCursorGetFlags AFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					int LDelta = 0;
					while (ADelta != 0)
					{
						if (ADelta > 0)
						{
							if (!FSourceTable.Next())
								break;
							ADelta--;
						}
						else
						{
							if (!FSourceTable.Prior())
								break;
							ADelta++;
						}
						LDelta++;
					}
					AFlags = InternalGetFlags();
					return LDelta;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		// MoveTo
		internal RemoteCursorGetFlags MoveTo(bool AFirst)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					if (AFirst)
						FSourceTable.First();
					else
						FSourceTable.Last();
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		internal RemoteCursorGetFlags ResetWithFlags()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.Reset();
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		internal bool GotoBookmark(Guid ABookmark, bool AForward, out RemoteCursorGetFlags AFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					bool LSuccess = InternalGotoBookmark(ABookmark, AForward);
					AFlags = InternalGetFlags();
					return LSuccess;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		internal bool FindKey(Row AKey, out RemoteCursorGetFlags AFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					bool LSuccess = FSourceTable.FindKey(AKey);
					AFlags = InternalGetFlags();
					return LSuccess;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		internal void FindNearest(Row AKey, out RemoteCursorGetFlags AFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.FindNearest(AKey);
					AFlags = InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		internal bool Refresh(Row ARow, out RemoteCursorGetFlags AFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					bool LSuccess = FSourceTable.Refresh(ARow);
					AFlags = InternalGetFlags();
					return LSuccess;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
	}
}
