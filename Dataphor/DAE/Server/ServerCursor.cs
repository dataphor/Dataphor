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

namespace Alphora.Dataphor.DAE.Server
{
	// ServerCursor    
	public class ServerCursor : ServerChildObject, IServerCursor
	{
		public ServerCursor(ServerExpressionPlan APlan, Program AProgram, DataParams AParams) : base() 
		{
			FPlan = APlan;
			FProgram = AProgram;
			FParams = AParams;
		}

		private bool FDisposed;
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Close();
				
				if (!FDisposed)
				{
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
		
		private Program FProgram;
		private bool FProgramStarted;
		
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
        
		protected PlanNode SourceNode { get { return FPlan.SourceNode; } }
		
		protected DataParams FParams;
		protected Table FSourceTable;
		protected Schema.IRowType FSourceRowType;
		public Schema.IRowType SourceRowType { get { return FSourceRowType; } }
		
		protected IStreamManager StreamManager { get { return (IStreamManager)FPlan.Process; } }
				
		protected virtual void InternalOpen()
		{
			// get a table object to supply the data
			FPlan.SetActiveCursor(this);
			try
			{
				FProgram.Start(FParams);
				FProgramStarted = true;

				long LStartTicks = TimingUtility.CurrentTicks;

				CursorNode LCursorNode = FPlan.CursorNode;
				//LCursorNode.EnsureApplicationTransactionJoined(FPlan.ServerProcess);
				FSourceTable = (Table)FPlan.CursorNode.SourceNode.Execute(FProgram);
				FSourceTable.Open();
				FSourceRowType = FSourceTable.DataType.RowType;
				
				FProgram.Statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(LStartTicks);
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
							FSourceRowType = null;
						}
					}
				}
				finally
				{
					if (FProgramStarted)
						FProgram.Stop(FParams);
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
			Row LRow;
			if (!FBookmarks.TryGetValue(ABookmark, out LRow))
				throw new ServerException(ServerException.Codes.InvalidBookmark, ABookmark);
			return FSourceTable.GotoBookmark(FBookmarks[ABookmark], AForward);
		}

		protected int InternalCompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			return FSourceTable.CompareBookmarks(FBookmarks[ABookmark1], FBookmarks[ABookmark2]);
		}

		protected void InternalDisposeBookmark(Guid ABookmark)
		{
			Row LInternalBookmark = null;
			FBookmarks.TryGetValue(ABookmark, out LInternalBookmark);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		
		public Schema.Order Order { get { return FSourceTable.Order; } }
		
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
				FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					return ((TableNode)SourceNode).Default(FPlan.Program, null, ARow, null, AColumnName);
				}
				finally
				{
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					return ((TableNode)SourceNode).Change(FPlan.Program, AOldRow, ANewRow, null, AColumnName);
				}
				finally
				{
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
					return ((TableNode)SourceNode).Validate(FPlan.Program, AOldRow, ANewRow, null, AColumnName);
				}
				finally
				{
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		
		private CursorGetFlags InternalGetFlags()
		{
			CursorGetFlags LGetFlags = CursorGetFlags.None;
			if (FSourceTable.BOF())
				LGetFlags = LGetFlags | CursorGetFlags.BOF;
			if (FSourceTable.EOF())
				LGetFlags = LGetFlags | CursorGetFlags.EOF;
			return LGetFlags;
		}

		public CursorGetFlags GetFlags()
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		public int Fetch(Row[] ARows, Guid[] ABookmarks, int ACount, out CursorGetFlags AFlags)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		public int MoveBy(int ADelta, out CursorGetFlags AFlags)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		public CursorGetFlags MoveTo(bool AFirst)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		
		public CursorGetFlags ResetWithFlags()
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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

		public bool GotoBookmark(Guid ABookmark, bool AForward, out CursorGetFlags AFlags)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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

		public bool FindKey(Row AKey, out CursorGetFlags AFlags)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		
		public void FindNearest(Row AKey, out CursorGetFlags AFlags)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
		
		public bool Refresh(Row ARow, out CursorGetFlags AFlags)
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
					FProgram.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
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
