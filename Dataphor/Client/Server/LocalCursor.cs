/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalCursor : LocalServerChildObject, IServerCursor
    {	
		public const int CSourceCursorIndexUnknown = -2;
		
		public LocalCursor(LocalExpressionPlan APlan, IRemoteServerCursor ACursor) : base()
		{
			FPlan = APlan;
			FCursor = ACursor;
			FInternalProcess = FPlan.FProcess.FInternalProcess;
			FInternalProgram = new Program(FInternalProcess);
			FInternalProgram.Start(null);
			FBuffer = new LocalRows();
			FBookmarks = new LocalBookmarks();
			FFetchCount = FPlan.FProcess.ProcessInfo.FetchCount;
			FTrivialBOF = true;
			FSourceCursorIndex = -1;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FBuffer != null)
			{
				FBuffer.Dispose();
				FBuffer = null;
			}
			
			if (FInternalProgram != null)
			{
				FInternalProgram.Stop(null);
				FInternalProgram = null;
			}
			
			FInternalProcess = null;
			FCursor = null;
			FPlan = null;
			base.Dispose(ADisposing);
		}
		
		private ServerProcess FInternalProcess;
		private Program FInternalProgram;

		protected LocalExpressionPlan FPlan;
        /// <value>Returns the <see cref="IServerExpressionPlan"/> instance for this cursor.</value>
        public IServerExpressionPlan Plan { get { return FPlan; } }
		
		protected IRemoteServerCursor FCursor;
		public IRemoteServerCursor RemoteCursor { get { return FCursor; } }

		// CursorType
		public CursorType CursorType { get { return FPlan.CursorType; } }

		// Isolation
		public CursorIsolation Isolation { get { return FPlan.Isolation; } }

		// Capabilites
		public CursorCapability Capabilities { get { return FPlan.Capabilities; } }
		
		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}

		protected int FFetchCount = SessionInfo.CDefaultFetchCount;
		/// <value>Gets or sets the number of rows to fetch at a time.  </value>
		/// <remarks>
		/// FetchCount must be greater than or equal to 1.  
		/// This setting is only valid for cursors which support Bookmarkable.
		/// A setting of 1 disables fetching behavior.
		/// </remarks>
		public int FetchCount 
		{
			get { return FFetchCount; } 
			set { FFetchCount = ((value < 1) ? 1 : value); } 
		}
		
		/*
			Fetch Behavior ->
			
				The buffer is fetched in the direction of the last navigation call
			
				Select ->
					if the cache is populated
						return the row from the cache
					else
						populate the cache from the underlying cursor in the direction indicated by the buffer direction variable
					
				Next ->
					if the cache is populated,
						increment current
						if current >= buffer count
							goto current - 1
							clear the buffer
							set buffer direction forward
							move next on the underlying cursor
					else
						move next on the underlying cursor
							
				Prior ->
					if the cache is populated,
						decrement current
						if current < 0
							goto current + 1
							clear the buffer
							set buffer direction backwards
							move prior on the underlying cursor
					else
						move prior on the underlying cursor
						
				First ->
					if the cache is populated,
						clear it
						set buffer direction forward
					call first on the source cursor and set the cache flags based on it
				
				Last ->
					if the cache is populated,
						clear it
						set buffer direction backwards
					call last on the source cursor and set the cache flags based on it
					
				BOF ->
					if the cache is populated
						FBOF = FFlags.BOF && FBufferIndex < 0;
					else
						FBOF = FFlags.BOF
				
				EOF ->
					if the cache is populated
						FEOF = FFlags.EOF && FBufferIndex >= FBuffer.Count;
					else
						FEOF = FFlags.EOF;
				
				GetBookmark() ->
					if the cache is populated
						return the bookmark for the current row
					else
						populate the cache based on the buffer direction
						set FBOF and FEOF based on the result flags and the number of rows returned
				
				GotoBookmark() ->
					if the cache is populated
						if the bookmark is in the cache
							set current to the bookmark
						else
							clear the cache
							set buffer direction forward
							goto the bookmark on the underlying cursor
					else
						goto the bookmark on the underlying cursor
				
				CompareBookmarks() ->
					if the bookmarks are equal,
						return 0
					else
						return compare bookmarks on the underlying cursor
				
				GetKey() ->
					if the cache is populated
						goto current
					return get key on the underlying cursor

				FindKey() ->
					execute find key on the underlying cursor
					if the key was found
						if the cache is populated
							clear it
							set buffer direction backwards // this is to take advantage of the way the view fills the buffer and preserves an optimization based on the underlying browse cursor behavior

				FindNearest() ->
					if the cache is populated
						clear it
						set buffer direction backwards
					execute find nearest on the underlying cursor
				
				Refresh() ->
					if the cache is populated
						clear it
						set buffer direction backwards
					execute refresh on the underlying cursor
				
				Insert() ->
					execute the insert on the underlying cursor
					if the cache is populated
						clear it
						set buffer direction backwards
				
				Update() ->
					if the cache is populated
						goto current
					execute the update on the underlying cursor
					if the cache is populated
						clear it
						set buffer direction backwards
				
				Delete() ->
					if the cache is populated
						goto current
					execute the delete on the underlying cursor
					if the cache is populated
						clear it
						set buffer direction backwards
				
				Default() ->
				Change() ->
				Validate() ->
					The proposable interfaces do not require a located cursor so they have no effect on the caching
		*/
		
		protected LocalRows FBuffer;
		protected LocalBookmarks FBookmarks;
		protected int FBufferIndex = -1; 
		protected bool FBufferFull;
		//protected bool FBufferFirst;
		protected BufferDirection FBufferDirection = BufferDirection.Forward;
		/// <summary> Index of FCursor relative to the buffer or CSourceCursorIndexUnknown if unknown. </summary>
		protected int FSourceCursorIndex;
		
		protected bool BufferActive()
		{
			return UseBuffer() && FBufferFull;
		}
		
		protected bool UseBuffer()
		{
			return (FFetchCount > 1) && Supports(CursorCapability.Bookmarkable);
		}
		
		protected void ClearBuffer()
		{
			// Dereference all used bookmarks
			Guid[] LBookmarks = new Guid[FBuffer.Count];
			for (int LIndex = 0; LIndex < FBuffer.Count; LIndex++)
				LBookmarks[LIndex] = FBuffer[LIndex].Bookmark;
			BufferDisposeBookmarks(LBookmarks);
			
			FBuffer.Clear();
			FBufferIndex = -1;
			FBufferFull = false;
			FSourceCursorIndex = CSourceCursorIndexUnknown;
		}

		protected void SetBufferDirection(BufferDirection ABufferDirection)
		{
			FBufferDirection = ABufferDirection;
		}
		
        public void Open()
        {
			FCursor.Open();
		}
		
        public void Close()
        {
			if (BufferActive())
				ClearBuffer();
			FCursor.Close();
		}
		
        public bool Active
        {
			get { return FCursor.Active; }
			set { FCursor.Active = value; }
		}

		// Flags tracks the current status of the remote cursor, BOF, EOF, none, or both
		protected bool FFlagsCached;
		protected CursorGetFlags FFlags;
		protected bool FTrivialBOF;
		// TrivialEOF unneccesary because Last returns flags
		
		protected void SetFlags(CursorGetFlags AFlags)
		{
			FFlags = AFlags;
			FFlagsCached = true;
			FTrivialBOF = false;
		}
		
		protected CursorGetFlags GetFlags()
		{
			if (!FFlagsCached)
			{
				SetFlags(FCursor.GetFlags(FPlan.FProcess.GetProcessCallInfo()));
				FPlan.FProgramStatisticsCached = false;
			}
			return FFlags;
		}

		public void Reset()
        {
			if (BufferActive())
				ClearBuffer();
			FSourceCursorIndex = -1;
			SetFlags(FCursor.Reset(FPlan.FProcess.GetProcessCallInfo()));
			SetBufferDirection(BufferDirection.Forward);
			FPlan.FProgramStatisticsCached = false;
		}

        public Row Select()
        {
			Row LRow = new Row(FPlan.FProcess.ValueManager, ((Schema.TableType)FPlan.DataType).RowType);
			try
			{
				Select(LRow);
			}
			catch
			{
				LRow.Dispose();
				throw;
			}
			return LRow;
		}
		
		private void SourceSelect(Row ARow)
		{
			RemoteRowHeader LHeader = new RemoteRowHeader();
			LHeader.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LHeader.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			ARow.ValuesOwned = false;
			byte[] AData = FCursor.Select(LHeader, FPlan.FProcess.GetProcessCallInfo()).Data;
			ARow.AsPhysical = AData;
			FPlan.FProgramStatisticsCached = false;
		}
		
		private void BufferSelect(Row ARow)
		{
			// TODO: implement a version of CopyTo that does not copy overflow 
			// problem is that this requires a row type of exactly the same type as the cursor table type
			FBuffer[FBufferIndex].Row.CopyTo(ARow);
		}
		
        public void Select(Row ARow)
        {
			if (BufferActive())
				BufferSelect(ARow);
			else
			{
				if (UseBuffer())
				{
					SourceFetch(false);
					BufferSelect(ARow);
				}
				else
					SourceSelect(ARow);
			}
		}
		
		protected void SourceFetch(bool AIsFirst)
		{
			// Execute fetch on the remote cursor, selecting all columns, requesting FFetchCount rows from the current position
			Guid[] LBookmarks;
			RemoteFetchData LFetchData = FCursor.Fetch(out LBookmarks, FFetchCount * (int)FBufferDirection, FPlan.FProcess.GetProcessCallInfo());
			ProcessFetchData(LFetchData, LBookmarks, AIsFirst);
			FPlan.FProgramStatisticsCached = false;
		}
		
		public void ProcessFetchData(RemoteFetchData AFetchData, Guid[] ABookmarks, bool AIsFirst)
		{
			FBuffer.BufferDirection = FBufferDirection;
			Schema.IRowType LRowType = DataType.RowType;
			if (FBufferDirection == BufferDirection.Forward)
			{
				for (int LIndex = 0; LIndex < AFetchData.Body.Length; LIndex++)
				{
					LocalRow LRow = new LocalRow(new Row(FPlan.FProcess.ValueManager, LRowType), ABookmarks[LIndex]);
					LRow.Row.AsPhysical = AFetchData.Body[LIndex].Data;
					FBuffer.Add(LRow);
					FBookmarks.Add(new LocalBookmark(ABookmarks[LIndex]));
				}
				
				if ((AFetchData.Body.Length > 0) && !AIsFirst)
					FBufferIndex = 0;
				else
					FBufferIndex = -1;
				FSourceCursorIndex = FBuffer.Count - 1;
			}
			else
			{
				for (int LIndex = 0; LIndex < AFetchData.Body.Length; LIndex++)
				{
					LocalRow LRow = new LocalRow(new Row(FPlan.FProcess.ValueManager, LRowType), ABookmarks[LIndex]);
					LRow.Row.AsPhysical = AFetchData.Body[LIndex].Data;
					FBuffer.Insert(0, LRow);
					FBookmarks.Add(new LocalBookmark(ABookmarks[LIndex]));
				}
				
				if ((AFetchData.Body.Length > 0) && !AIsFirst)
					FBufferIndex = FBuffer.Count - 1;
				else
					FBufferIndex = -1;
				FSourceCursorIndex = 0;
			}
			
			SetFlags(AFetchData.Flags);
			FBufferFull = true;
		}
		
		protected bool SourceNext()
		{
			RemoteMoveData LMoveData = FCursor.MoveBy(1, FPlan.FProcess.GetProcessCallInfo());
			FPlan.FProgramStatisticsCached = false;
			SetFlags(LMoveData.Flags);
			return LMoveData.Flags == CursorGetFlags.None;
		}

		private void GotoBookmarkIndex(int AIndex, bool AForward)
		{
			if (!SourceGotoBookmark(FBuffer[AIndex].Bookmark, AForward))
				throw new ServerException(ServerException.Codes.CursorSyncError);
		}		
		
		protected bool SyncSource(bool AForward)
		{
			if (FSourceCursorIndex != FBufferIndex)
			{
				FSourceCursorIndex = FBufferIndex;
				if (FBufferIndex == -1)
				{
					GotoBookmarkIndex(0, false);
					return SourcePrior();
				}
				else if (FBufferIndex == FBuffer.Count)
				{
					GotoBookmarkIndex(FBuffer.Count - 1, true);
					return SourceNext();
				}
				else
				{
					GotoBookmarkIndex(FBufferIndex, AForward);
					return true;
				}
			}
			else
				return true;
		}
		
        public bool Next()
        {
			SetBufferDirection(BufferDirection.Forward);
			if (UseBuffer())
			{
				if (!BufferActive())
					SourceFetch(SourceBOF());
					
				if (FBufferIndex >= FBuffer.Count - 1)
				{
					if (SourceEOF())
					{
						FBufferIndex++;
						return false;
					}

					bool LSynced = SyncSource(true);
					ClearBuffer();
					SourceFetch(false);
					return LSynced && !EOF();
				}
				FBufferIndex++;
				return true;
			}

			if (FFlagsCached && SourceEOF())
				return false;
			return SourceNext();
		}
		
		protected void SourceLast()
		{
			SetFlags(FCursor.Last(FPlan.FProcess.GetProcessCallInfo()));
			FPlan.FProgramStatisticsCached = false;
		}
		
        public void Last()
        {
			SetBufferDirection(BufferDirection.Backward);
			if (BufferActive())
				ClearBuffer();
			SourceLast();					
		}
		
		protected bool SourceBOF()
		{
			return FTrivialBOF || ((GetFlags() & CursorGetFlags.BOF) != 0);
		}

        public bool BOF()
		{
			if (BufferActive())
				return SourceBOF() && (FBufferIndex < 0);
			else
				return SourceBOF();
		}
		
		protected bool SourceEOF()
		{
			return (GetFlags() & CursorGetFlags.EOF) != 0;
		}
		
        public bool EOF()
        {
			if (BufferActive())
				return SourceEOF() && (FBufferIndex >= FBuffer.Count);
			else
				return SourceEOF();
		}
		
        public bool IsEmpty()
        {
			return BOF() && EOF();
		}
		
		public void Insert(Row ARow)
		{
			Insert(ARow, null);
		}
		
        public void Insert(Row ARow, BitArray AValueFlags)
        {
			RemoteRow LRow = new RemoteRow();
			FPlan.FProcess.EnsureOverflowReleased(ARow);
			LRow.Header = new RemoteRowHeader();
			LRow.Header.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LRow.Header.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			LRow.Body = new RemoteRowBody();
			LRow.Body.Data = ARow.AsPhysical;
			FCursor.Insert(LRow, AValueFlags, FPlan.FProcess.GetProcessCallInfo());
			FFlagsCached = false;
			FPlan.FProgramStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
		public void Update(Row ARow)
		{
			Update(ARow, null);
		}
		
        public void Update(Row ARow, BitArray AValueFlags)
        {
			RemoteRow LRow = new RemoteRow();
			FPlan.FProcess.EnsureOverflowReleased(ARow);
			LRow.Header = new RemoteRowHeader();
			LRow.Header.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LRow.Header.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			LRow.Body = new RemoteRowBody();
			LRow.Body.Data = ARow.AsPhysical;
			if (BufferActive())
				SyncSource(true);
			FCursor.Update(LRow, AValueFlags, FPlan.FProcess.GetProcessCallInfo());
			FFlagsCached = false;
			FPlan.FProgramStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
        public void Delete()
        {
			if (BufferActive())
				SyncSource(true);
			FCursor.Delete(FPlan.FProcess.GetProcessCallInfo());
			FFlagsCached = false;
			FPlan.FProgramStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
		protected void SourceFirst()
		{
			SetFlags(FCursor.First(FPlan.FProcess.GetProcessCallInfo()));
			FPlan.FProgramStatisticsCached = false;
		}

        public void First()
        {
			SetBufferDirection(BufferDirection.Forward);
			if (BufferActive())
				ClearBuffer();
			SourceFirst();
		}
		
		protected bool SourcePrior()
		{
			RemoteMoveData LMoveData = FCursor.MoveBy(-1, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LMoveData.Flags);
			FPlan.FProgramStatisticsCached = false;
			return LMoveData.Flags == CursorGetFlags.None;
		}
		
        public bool Prior()
        {
			SetBufferDirection(BufferDirection.Backward);
			if (UseBuffer())
			{
				if (!BufferActive())
					SourceFetch(SourceEOF());

				if (FBufferIndex <= 0)
				{
					if (SourceBOF())
					{
						FBufferIndex--;
						return false;
					}

					bool LSynced = SyncSource(false);
					ClearBuffer();
					SourceFetch(false);
					return LSynced && !BOF();
				}
				FBufferIndex--;
				return true;
			}

			if (FFlagsCached && SourceBOF())
				return false;
			return SourcePrior();
		}
		
		protected Guid SourceGetBookmark()
		{
			FPlan.FProgramStatisticsCached = false;
			return FCursor.GetBookmark(FPlan.FProcess.GetProcessCallInfo());
		}

        public Guid GetBookmark()
        {
			if (BufferActive())
			{
				FBookmarks[FBuffer[FBufferIndex].Bookmark].ReferenceCount++;
				return FBuffer[FBufferIndex].Bookmark;
			}

			if (UseBuffer())
			{
				SourceFetch(FBuffer.BufferDirection == BufferDirection.Forward ? SourceBOF() : SourceEOF());
				FBookmarks[FBuffer[FBufferIndex].Bookmark].ReferenceCount++;
				return FBuffer[FBufferIndex].Bookmark;
			}

			return SourceGetBookmark();
        }

		protected bool SourceGotoBookmark(Guid ABookmark, bool AForward)
        {
			RemoteGotoData LGotoData = FCursor.GotoBookmark(ABookmark, AForward, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LGotoData.Flags);
			FPlan.FProgramStatisticsCached = false;
			return LGotoData.Success;
        }

		public bool GotoBookmark(Guid ABookmark, bool AForward)
        {
			SetBufferDirection((AForward ? BufferDirection.Forward : BufferDirection.Backward));
			if (UseBuffer())
			{
				for (int LIndex = 0; LIndex < FBuffer.Count; LIndex++)
					if (FBuffer[LIndex].Bookmark == ABookmark)
					{
						FBufferIndex = LIndex;
						return true;
					}
				
				ClearBuffer();
				return SourceGotoBookmark(ABookmark, AForward);
			}
			return SourceGotoBookmark(ABookmark, AForward);
		}
		
        public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
        {
			FPlan.FProgramStatisticsCached = false;
			return FCursor.CompareBookmarks(ABookmark1, ABookmark2, FPlan.FProcess.GetProcessCallInfo());
		}
		
		protected void SourceDisposeBookmark(Guid ABookmark)
		{
			FCursor.DisposeBookmark(ABookmark, FPlan.FProcess.GetProcessCallInfo());
			FPlan.FProgramStatisticsCached = false;
		}

		protected void SourceDisposeBookmarks(Guid[] ABookmarks)
		{
			FCursor.DisposeBookmarks(ABookmarks, FPlan.FProcess.GetProcessCallInfo());
			FPlan.FProgramStatisticsCached = false;
		}

		protected void BufferDisposeBookmark(Guid ABookmark)
		{
			if (ABookmark != Guid.Empty)
			{
				LocalBookmark LBookmark = FBookmarks[ABookmark];
				if (LBookmark == null)
					throw new ServerException(ServerException.Codes.InvalidBookmark, ABookmark.ToString());
					
				LBookmark.ReferenceCount--;
				
				if (LBookmark.ReferenceCount == 0)
				{
					SourceDisposeBookmark(LBookmark.Bookmark);
					FBookmarks.Remove(LBookmark.Bookmark);
				}
			}
		}

		protected void BufferDisposeBookmarks(Guid[] ABookmarks)
		{
			// Dereference each bookmark and prepare list of unreferenced bookmarks
			List<Guid> LToDispose = new List<Guid>(ABookmarks.Length);
			for (int LIndex = 0; LIndex < FBuffer.Count; LIndex++)
			{
				var LBookmark = FBookmarks[ABookmarks[LIndex]];
				LBookmark.ReferenceCount--;
				if (LBookmark.ReferenceCount == 0)
				{
					LToDispose.Add(LBookmark.Bookmark);
					FBookmarks.Remove(LBookmark.Bookmark);
				}
			}

			// Free all unreferenced bookmarks together
			if (LToDispose.Count > 0)
				SourceDisposeBookmarks(LToDispose.ToArray());
		}

		public void DisposeBookmark(Guid ABookmark)
		{
			if (BufferActive())
				BufferDisposeBookmark(ABookmark);
			else
				SourceDisposeBookmark(ABookmark);
		}

		public void DisposeBookmarks(Guid[] ABookmarks)
		{
			if (BufferActive())
				BufferDisposeBookmarks(ABookmarks);
			else
				SourceDisposeBookmarks(ABookmarks);
		}
		
		public Schema.Order Order { get { return FPlan.Order; } }
		
        public Row GetKey()
        {
			if (BufferActive())
				SyncSource(true);
			RemoteRow LKey = FCursor.GetKey(FPlan.FProcess.GetProcessCallInfo());
			FPlan.FProgramStatisticsCached = false;
			Row LRow;
			Schema.RowType LType = new Schema.RowType();
			foreach (string LString in LKey.Header.Columns)
				LType.Columns.Add(((Schema.TableType)FPlan.DataType).Columns[LString].Copy());
			LRow = new Row(FPlan.FProcess.ValueManager, LType);
			LRow.ValuesOwned = false;
			LRow.AsPhysical = LKey.Body.Data;
			return LRow;
		}
		
        public bool FindKey(Row AKey)
        {
			RemoteRow LKey = new RemoteRow();
			FPlan.FProcess.EnsureOverflowConsistent(AKey);
			LKey.Header = new RemoteRowHeader();
			LKey.Header.Columns = new string[AKey.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
				LKey.Header.Columns[LIndex] = AKey.DataType.Columns[LIndex].Name;
			LKey.Body = new RemoteRowBody();
			LKey.Body.Data = AKey.AsPhysical;
			RemoteGotoData LGotoData = FCursor.FindKey(LKey, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LGotoData.Flags);
			FPlan.FProgramStatisticsCached = false;
			if (LGotoData.Success && BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			return LGotoData.Success;
		}
		
        public void FindNearest(Row AKey)
        {
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			FPlan.FProcess.EnsureOverflowConsistent(AKey);
			RemoteRow LKey = new RemoteRow();
			LKey.Header = new RemoteRowHeader();
			LKey.Header.Columns = new string[AKey.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
				LKey.Header.Columns[LIndex] = AKey.DataType.Columns[LIndex].Name;
			LKey.Body = new RemoteRowBody();
			LKey.Body.Data = AKey.AsPhysical;
			SetFlags(FCursor.FindNearest(LKey, FPlan.FProcess.GetProcessCallInfo()));
			FPlan.FProgramStatisticsCached = false;
		}
		
        public bool Refresh(Row ARow)
        {
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			RemoteRow LRow = new RemoteRow();
			FPlan.FProcess.EnsureOverflowConsistent(ARow);
			LRow.Header = new RemoteRowHeader();
			LRow.Header.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LRow.Header.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			LRow.Body = new RemoteRowBody();
			LRow.Body.Data = ARow.AsPhysical;
			RemoteGotoData LGotoData = FCursor.Refresh(LRow, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LGotoData.Flags);
			FPlan.FProgramStatisticsCached = false;
			return LGotoData.Success;
		}
		
        public int RowCount()
        {
			FPlan.FProgramStatisticsCached = false;
			return FCursor.RowCount(FPlan.FProcess.GetProcessCallInfo());
		}
		
		public Schema.TableType DataType { get { return (Schema.TableType)FPlan.DataType; } }
		public Schema.TableVar TableVar { get { return FPlan.TableVar; } }
		public TableNode TableNode { get { return FPlan.TableNode; } }
		
		// Copies the values from source row to the given target row, without using stream referencing
		// ASourceRow and ATargetRow must be of equivalent row types.
		protected void MarshalRow(Row ASourceRow, Row ATargetRow)
		{
			for (int LIndex = 0; LIndex < ASourceRow.DataType.Columns.Count; LIndex++)
				if (ASourceRow.HasValue(LIndex))
				{
					if (ASourceRow.HasNonNativeValue(LIndex))
					{
						Scalar LScalar = new Scalar(ATargetRow.Manager, (Schema.ScalarType)ASourceRow.DataType.Columns[LIndex].DataType);
						Stream LSourceStream = ASourceRow.GetValue(LIndex).OpenStream();
						try
						{
							Stream LTargetStream = LScalar.OpenStream();
							try
							{
								StreamUtility.CopyStream(LSourceStream, LTargetStream);
							}
							finally
							{
								LTargetStream.Close();
							}
						}
						finally
						{
							LSourceStream.Close();
						}
						ATargetRow[LIndex] = LScalar;
					}
					else
						ATargetRow[LIndex] = ASourceRow[LIndex];
				}
				else
					ATargetRow.ClearValue(LIndex);
		}
		
        /// <summary>Requests the default values for a new row in the cursor.</summary>        
        /// <param name='ARow'>A <see cref="Row"/> to be filled in with default values.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="ARow"/>.</returns>
        public bool Default(Row ARow, string AColumnName)
        {
			if ((FInternalProcess != null) && TableVar.IsDefaultCallRemotable(AColumnName))
			{
				// create a new row based on FInternalProcess, and copy the data from 
				Row LRow = ARow;
				if (ARow.HasNonNativeValues())
				{
					LRow = new Row(FInternalProcess.ValueManager, ARow.DataType);
					MarshalRow(ARow, LRow);
				}

				FPlan.FProcess.FSession.FServer.AcquireCacheLock(FPlan.FProcess, LockMode.Shared);
				try
				{
					bool LChanged = TableNode.Default(FInternalProgram, null, LRow, null, AColumnName);
					if (LChanged && !Object.ReferenceEquals(LRow, ARow))
						MarshalRow(LRow, ARow);
					return LChanged;
				}
				finally
				{
					FPlan.FProcess.FSession.FServer.ReleaseCacheLock(FPlan.FProcess, LockMode.Shared);
				}
			}
			else
			{
				FPlan.FProcess.EnsureOverflowReleased(ARow);
				RemoteRowBody LBody = new RemoteRowBody();
				LBody.Data = ARow.AsPhysical;

				RemoteProposeData LProposeData = FCursor.Default(LBody, AColumnName, FPlan.FProcess.GetProcessCallInfo());
				FPlan.FProgramStatisticsCached = false;

				if (LProposeData.Success)
				{
					ARow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server for the course of the default call.
					ARow.AsPhysical = LProposeData.Body.Data;
					ARow.ValuesOwned = true;
				}
				return LProposeData.Success;
			}
        }
        
        /// <summary>Requests the affect of a change to the given row.</summary>
        /// <param name='AOldRow'>A <see cref="Row"/> containing the original values for the row.</param>
        /// <param name='ANewRow'>A <see cref="Row"/> containing the changed values for the row.</param>
        /// <param name='AColumnName'>The name of the column which changed in <paramref name="ANewRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="ANewRow"/>.</returns>
        public bool Change(Row AOldRow, Row ANewRow, string AColumnName)
        {
			// if the table level change is remotable and the named column is remotable or no column is named and all columns are remotable
				// the change can be evaluated locally, otherwise a remote call is required
			if ((FInternalProcess != null) && TableVar.IsChangeCallRemotable(AColumnName))
			{
				Row LOldRow = AOldRow;
				if (AOldRow.HasNonNativeValues())
				{
					LOldRow = new Row(FInternalProcess.ValueManager, AOldRow.DataType);
					MarshalRow(AOldRow, LOldRow);
				}
				
				Row LNewRow = ANewRow;
				if (ANewRow.HasNonNativeValues())
				{
					LNewRow = new Row(FInternalProcess.ValueManager, ANewRow.DataType);
					MarshalRow(ANewRow, LNewRow);
				}

				FPlan.FProcess.FSession.FServer.AcquireCacheLock(FPlan.FProcess, LockMode.Shared);
				try
				{
					bool LChanged = TableNode.Change(FInternalProgram, LOldRow, LNewRow, null, AColumnName);
					if (LChanged && !Object.ReferenceEquals(LNewRow, ANewRow))
						MarshalRow(LNewRow, ANewRow);
					return LChanged;
				}
				finally
				{
					FPlan.FProcess.FSession.FServer.ReleaseCacheLock(FPlan.FProcess, LockMode.Shared);
				}
			}
			else
			{			
				FPlan.FProcess.EnsureOverflowReleased(AOldRow);
				RemoteRowBody LOldBody = new RemoteRowBody();
				LOldBody.Data = AOldRow.AsPhysical;
				
				FPlan.FProcess.EnsureOverflowReleased(ANewRow);
				RemoteRowBody LNewBody = new RemoteRowBody();
				LNewBody.Data = ANewRow.AsPhysical;

				RemoteProposeData LProposeData = FCursor.Change(LOldBody, LNewBody, AColumnName, FPlan.FProcess.GetProcessCallInfo());
				FPlan.FProgramStatisticsCached = false;

				if (LProposeData.Success)
				{
					ANewRow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server during the change call
					ANewRow.AsPhysical = LProposeData.Body.Data;
					ANewRow.ValuesOwned = true;
				}

				return LProposeData.Success;
			}
        }
        
        /// <summary>Ensures that the given row is valid.</summary>
        /// <param name='AOldRow'>A <see cref="Row"/> containing the original values for the row.</param>
        /// <param name='ANewRow'>A <see cref="Row"/> containing the changed values for the row.</param>
        /// <param name='AColumnName'>The name of the column which changed in <paramref name="ANewRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="ANewRow"/>.</returns>
        public bool Validate(Row AOldRow, Row ANewRow, string AColumnName)
        {
			if ((FInternalProcess != null) && TableVar.IsValidateCallRemotable(AColumnName))
			{
				Row LOldRow = AOldRow;
				if ((AOldRow != null) && AOldRow.HasNonNativeValues())
				{
					LOldRow = new Row(FInternalProcess.ValueManager, AOldRow.DataType);
					MarshalRow(AOldRow, LOldRow);
				}
				
				Row LNewRow = ANewRow;
				if (ANewRow.HasNonNativeValues())
				{
					LNewRow = new Row(FInternalProcess.ValueManager, ANewRow.DataType);
					MarshalRow(ANewRow, LNewRow);
				}

				FPlan.FProcess.FSession.FServer.AcquireCacheLock(FPlan.FProcess, LockMode.Shared);
				try
				{
					bool LChanged = TableNode.Validate(FInternalProgram, LOldRow, LNewRow, null, AColumnName);
					if (LChanged && !Object.ReferenceEquals(ANewRow, LNewRow))
						MarshalRow(LNewRow, ANewRow);
					return LChanged;
				}
				finally
				{
					FPlan.FProcess.FSession.FServer.ReleaseCacheLock(FPlan.FProcess, LockMode.Shared);
				}
			}
			else
			{
				RemoteRowBody LOldBody = new RemoteRowBody();
				if (AOldRow != null)
				{
					FPlan.FProcess.EnsureOverflowReleased(AOldRow);
					LOldBody.Data = AOldRow.AsPhysical;
				}
				
				FPlan.FProcess.EnsureOverflowReleased(ANewRow);
				RemoteRowBody LNewBody = new RemoteRowBody();
				LNewBody.Data = ANewRow.AsPhysical;
				
				RemoteProposeData LProposeData = FCursor.Validate(LOldBody, LNewBody, AColumnName, FPlan.FProcess.GetProcessCallInfo());
				FPlan.FProgramStatisticsCached = false;

				if (LProposeData.Success)
				{
					ANewRow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server during the validate call
					ANewRow.AsPhysical = LProposeData.Body.Data;
					ANewRow.ValuesOwned = true;
				}
				return LProposeData.Success;
			}
        }
    }
}
