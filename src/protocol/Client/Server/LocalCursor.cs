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
		public const int SourceCursorIndexUnknown = -2;
		
		public LocalCursor(LocalExpressionPlan plan, IRemoteServerCursor cursor) : base()
		{
			_plan = plan;
			_cursor = cursor;
			_internalProcess = _plan._process._internalProcess;
			_internalProgram = new Program(_internalProcess);
			_internalProgram.Start(null);
			_buffer = new LocalRows();
			_bookmarks = new LocalBookmarks();
			_fetchCount = _plan._process.ProcessInfo.FetchCount;
			_trivialBOF = true;
			_sourceCursorIndex = -1;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_buffer != null)
			{
				_buffer.Dispose();
				_buffer = null;
			}
			
			if (_internalProgram != null)
			{
				_internalProgram.Stop(null);
				_internalProgram = null;
			}
			
			_internalProcess = null;
			_cursor = null;
			_plan = null;
			base.Dispose(disposing);
		}
		
		private ServerProcess _internalProcess;
		private Program _internalProgram;

		protected LocalExpressionPlan _plan;
        /// <value>Returns the <see cref="IServerExpressionPlan"/> instance for this cursor.</value>
        public IServerExpressionPlan Plan { get { return _plan; } }
		
		protected IRemoteServerCursor _cursor;
		public IRemoteServerCursor RemoteCursor { get { return _cursor; } }

		// CursorType
		public CursorType CursorType { get { return _plan.CursorType; } }

		// Isolation
		public CursorIsolation Isolation { get { return _plan.Isolation; } }

		// Capabilites
		public CursorCapability Capabilities { get { return _plan.Capabilities; } }
		
		public bool Supports(CursorCapability capability)
		{
			return (Capabilities & capability) != 0;
		}

		protected int _fetchCount = SessionInfo.DefaultFetchCount;
		/// <value>Gets or sets the number of rows to fetch at a time.  </value>
		/// <remarks>
		/// FetchCount must be greater than or equal to 1.  
		/// This setting is only valid for cursors which support Bookmarkable.
		/// A setting of 1 disables fetching behavior.
		/// </remarks>
		public int FetchCount 
		{
			get { return _fetchCount; } 
			set { _fetchCount = ((value < 1) ? 1 : value); } 
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
		
		protected LocalRows _buffer;
		protected LocalBookmarks _bookmarks;
		protected int _bufferIndex = -1; 
		protected bool _bufferFull;
		//protected bool FBufferFirst;
		protected BufferDirection _bufferDirection = BufferDirection.Forward;
		/// <summary> Index of FCursor relative to the buffer or CSourceCursorIndexUnknown if unknown. </summary>
		protected int _sourceCursorIndex;
		
		protected bool BufferActive()
		{
			return UseBuffer() && _bufferFull;
		}
		
		protected bool UseBuffer()
		{
			return (_fetchCount > 1) 
				&& 
				(
					Supports(CursorCapability.Bookmarkable) 
						|| ((Capabilities & (CursorCapability.Updateable | CursorCapability.BackwardsNavigable)) == 0)
				);
		}
		
		protected void ClearBuffer()
		{
			// Dereference all used bookmarks
			Guid[] bookmarks = new Guid[_buffer.Count];
			for (int index = 0; index < _buffer.Count; index++)
				bookmarks[index] = _buffer[index].Bookmark;
			BufferDisposeBookmarks(bookmarks);
			
			_buffer.Clear();
			_bufferIndex = -1;
			_bufferFull = false;
			_sourceCursorIndex = SourceCursorIndexUnknown;
		}

		protected void SetBufferDirection(BufferDirection bufferDirection)
		{
			_bufferDirection = bufferDirection;
		}
		
        public void Open()
        {
			_cursor.Open();
		}
		
        public void Close()
        {
			if (BufferActive())
				ClearBuffer();
			_cursor.Close();
		}
		
        public bool Active
        {
			get { return _cursor.Active; }
			set { _cursor.Active = value; }
		}

		// Flags tracks the current status of the remote cursor, BOF, EOF, none, or both
		protected bool _flagsCached;
		protected CursorGetFlags _flags;
		protected bool _trivialBOF;
		// TrivialEOF unneccesary because Last returns flags
		
		protected void SetFlags(CursorGetFlags flags)
		{
			_flags = flags;
			_flagsCached = true;
			_trivialBOF = false;
		}
		
		protected CursorGetFlags GetFlags()
		{
			if (!_flagsCached)
			{
				SetFlags(_cursor.GetFlags(_plan._process.GetProcessCallInfo()));
				_plan._programStatisticsCached = false;
			}
			return _flags;
		}

		public void Reset()
        {
			if (BufferActive())
				ClearBuffer();
			_sourceCursorIndex = -1;
			SetFlags(_cursor.Reset(_plan._process.GetProcessCallInfo()));
			SetBufferDirection(BufferDirection.Forward);
			_plan._programStatisticsCached = false;
		}

        public IRow Select()
        {
			Row row = new Row(_plan._process.ValueManager, ((Schema.TableType)_plan.DataType).RowType);
			try
			{
				Select(row);
			}
			catch
			{
				row.Dispose();
				throw;
			}
			return row;
		}
		
		private void SourceSelect(IRow row)
		{
			RemoteRowHeader header = new RemoteRowHeader();
			header.Columns = new string[row.DataType.Columns.Count];
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				header.Columns[index] = row.DataType.Columns[index].Name;
			row.ValuesOwned = false;
			byte[] AData = _cursor.Select(header, _plan._process.GetProcessCallInfo()).Data;
			row.AsPhysical = AData;
			_plan._programStatisticsCached = false;
		}
		
		private void BufferCheckNotOnCrack()
		{
			if ((_bufferIndex < 0) || (_bufferIndex >= _buffer.Count))
				throw new RuntimeException(RuntimeException.Codes.NoCurrentRow);
		}
		
		private void BufferSelect(IRow row)
		{
			//BufferCheckNotOnCrack();

			// TODO: implement a version of CopyTo that does not copy overflow 
			// problem is that this requires a row type of exactly the same type as the cursor table type
			_buffer[_bufferIndex].Row.CopyTo(row);
		}
		
        public void Select(IRow row)
        {
			if (BufferActive())
				BufferSelect(row);
			else
			{
				if (UseBuffer())
				{
					SourceFetch(false);
					BufferSelect(row);
				}
				else
					SourceSelect(row);
			}
		}
		
		protected void SourceFetch(bool isFirst)
		{
			SourceFetch(isFirst, isFirst);
		}
		
		protected void SourceFetch(bool isFirst, bool skipCurrent)
		{
			// Execute fetch on the remote cursor, selecting all columns, requesting FFetchCount rows from the current position
			Guid[] bookmarks;
			RemoteFetchData fetchData = _cursor.Fetch(out bookmarks, _fetchCount * (int)_bufferDirection, skipCurrent, _plan._process.GetProcessCallInfo());
			ProcessFetchData(fetchData, bookmarks, isFirst);
			_plan._programStatisticsCached = false;
		}
		
		public void ProcessFetchData(RemoteFetchData fetchData, Guid[] bookmarks, bool isFirst)
		{
			_buffer.BufferDirection = _bufferDirection;
			Schema.IRowType rowType = DataType.RowType;
			if (_bufferDirection == BufferDirection.Forward)
			{
				for (int index = 0; index < fetchData.Body.Length; index++)
				{
					LocalRow row = new LocalRow(new Row(_plan._process.ValueManager, rowType), bookmarks[index]);
					row.Row.AsPhysical = fetchData.Body[index].Data;
					_buffer.Add(row);
					AddBookmarkIfNotEmpty(bookmarks[index]);
				}

				if ((fetchData.Body.Length > 0) && !isFirst)
					_bufferIndex = 0;
				else
					_bufferIndex = -1;
					
				if ((fetchData.Flags & CursorGetFlags.EOF) != 0)
					_sourceCursorIndex = _buffer.Count;
				else
					_sourceCursorIndex = _buffer.Count - 1;
			}
			else
			{
				for (int index = 0; index < fetchData.Body.Length; index++)
				{
					LocalRow row = new LocalRow(new Row(_plan._process.ValueManager, rowType), bookmarks[index]);
					row.Row.AsPhysical = fetchData.Body[index].Data;
					_buffer.Insert(0, row);
					AddBookmarkIfNotEmpty(bookmarks[index]);
				}

				if ((fetchData.Body.Length > 0) && !isFirst)
					_bufferIndex = _buffer.Count - 1;
				else
					_bufferIndex = _buffer.Count; 

				if ((fetchData.Flags & CursorGetFlags.BOF) != 0)
					_sourceCursorIndex = -1;
				else
					_sourceCursorIndex = 0;
			}
			
			SetFlags(fetchData.Flags);
			_bufferFull = true;
		}

		private void AddBookmarkIfNotEmpty(Guid bookmark)
		{
			if (bookmark != Guid.Empty)
				_bookmarks.Add(new LocalBookmark(bookmark));
		}
		
		protected bool SourceNext()
		{
			RemoteMoveData moveData = _cursor.MoveBy(1, _plan._process.GetProcessCallInfo());
			_plan._programStatisticsCached = false;
			SetFlags(moveData.Flags);
			return moveData.Flags == CursorGetFlags.None;
		}

		private void GotoBookmarkIndex(int index, bool forward)
		{
			if (!SourceGotoBookmark(_buffer[index].Bookmark, forward))
				throw new ServerException(ServerException.Codes.CursorSyncError);
		}		
		
		protected bool SyncSource(bool forward)
		{
			if (_sourceCursorIndex != _bufferIndex)
			{
				_sourceCursorIndex = _bufferIndex;
				if (_bufferIndex == -1)
				{
					if (_buffer.Count > 0)
						GotoBookmarkIndex(0, false);
					return SourcePrior();
				}
				else if (_bufferIndex == _buffer.Count)
				{
					if (_buffer.Count > 0)
						GotoBookmarkIndex(_buffer.Count - 1, true);
					return SourceNext();
				}
				else
				{
					GotoBookmarkIndex(_bufferIndex, forward);
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
					
				if (_bufferIndex >= _buffer.Count - 1)
				{
					if (_bufferIndex == _sourceCursorIndex - 1 && SourceEOF())
					{
						_bufferIndex++;
						return false;
					}

					bool synced = SyncSource(true);
					ClearBuffer();
					SourceFetch(false, true);
					return synced && !EOF();
				}
				_bufferIndex++;
				return true;
			}

			if (_flagsCached && SourceEOF())
				return false;
			return SourceNext();
		}
		
		protected void SourceLast()
		{
			SetFlags(_cursor.Last(_plan._process.GetProcessCallInfo()));
			_plan._programStatisticsCached = false;
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
			return _trivialBOF || ((GetFlags() & CursorGetFlags.BOF) != 0);
		}

        public bool BOF()
		{
			if (BufferActive())
				return SourceBOF() && ((_buffer.Count == 0) || (_bufferIndex < 0));
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
				return SourceEOF() && ((_buffer.Count == 0) || (_bufferIndex >= _buffer.Count));
			else
				return SourceEOF();
		}
		
        public bool IsEmpty()
        {
			return BOF() && EOF();
		}
		
		public void Insert(IRow row)
		{
			Insert(row, null);
		}
		
        public void Insert(IRow row, BitArray valueFlags)
        {
			RemoteRow localRow = new RemoteRow();
			_plan._process.EnsureOverflowReleased(row);
			localRow.Header = new RemoteRowHeader();
			localRow.Header.Columns = new string[row.DataType.Columns.Count];
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				localRow.Header.Columns[index] = row.DataType.Columns[index].Name;
			localRow.Body = new RemoteRowBody();
			localRow.Body.Data = row.AsPhysical;
			_cursor.Insert(localRow, valueFlags, _plan._process.GetProcessCallInfo());
			_flagsCached = false;
			_plan._programStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
		public void Update(IRow row)
		{
			Update(row, null);
		}

        public void Update(IRow row, BitArray valueFlags)
        {
			RemoteRow localRow = new RemoteRow();
			_plan._process.EnsureOverflowReleased(row);
			localRow.Header = new RemoteRowHeader();
			localRow.Header.Columns = new string[row.DataType.Columns.Count];
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				localRow.Header.Columns[index] = row.DataType.Columns[index].Name;
			localRow.Body = new RemoteRowBody();
			localRow.Body.Data = row.AsPhysical;
			if (BufferActive())
				SyncSource(true);
			_cursor.Update(localRow, valueFlags, _plan._process.GetProcessCallInfo());
			_flagsCached = false;
			_plan._programStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}

        public void Delete()
        {
			if (BufferActive())
				SyncSource(true);
			_cursor.Delete(_plan._process.GetProcessCallInfo());
			_flagsCached = false;
			_plan._programStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
		protected void SourceFirst()
		{
			SetFlags(_cursor.First(_plan._process.GetProcessCallInfo()));
			_plan._programStatisticsCached = false;
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
			RemoteMoveData moveData = _cursor.MoveBy(-1, _plan._process.GetProcessCallInfo());
			SetFlags(moveData.Flags);
			_plan._programStatisticsCached = false;
			return moveData.Flags == CursorGetFlags.None;
		}
		
        public bool Prior()
        {
			SetBufferDirection(BufferDirection.Backward);
			if (UseBuffer())
			{
				if (!BufferActive())
					SourceFetch(SourceEOF());

				if (_bufferIndex <= 0)
				{
					if (_bufferIndex == _sourceCursorIndex + 1 && SourceBOF())
					{
						_bufferIndex--;
						return false;
					}

					bool synced = SyncSource(false);
					ClearBuffer();
					SourceFetch(false, true);
					return synced && !BOF();
				}
				_bufferIndex--;
				return true;
			}

			if (_flagsCached && SourceBOF())
				return false;
			return SourcePrior();
		}
		
		protected Guid SourceGetBookmark()
		{
			_plan._programStatisticsCached = false;
			return _cursor.GetBookmark(_plan._process.GetProcessCallInfo());
		}

        public Guid GetBookmark()
        {
			if (BufferActive())
			{
				//BufferCheckNotOnCrack();
				_bookmarks[_buffer[_bufferIndex].Bookmark].ReferenceCount++;
				return _buffer[_bufferIndex].Bookmark;
			}

			if (UseBuffer())
			{
				SourceFetch(_buffer.BufferDirection == BufferDirection.Forward ? SourceBOF() : SourceEOF());
				//BufferCheckNotOnCrack();
				_bookmarks[_buffer[_bufferIndex].Bookmark].ReferenceCount++;
				return _buffer[_bufferIndex].Bookmark;
			}

			return SourceGetBookmark();
        }

		protected bool SourceGotoBookmark(Guid bookmark, bool forward)
        {
			RemoteGotoData gotoData = _cursor.GotoBookmark(bookmark, forward, _plan._process.GetProcessCallInfo());
			SetFlags(gotoData.Flags);
			_plan._programStatisticsCached = false;
			return gotoData.Success;
        }

		public bool GotoBookmark(Guid bookmark, bool forward)
        {
			SetBufferDirection((forward ? BufferDirection.Forward : BufferDirection.Backward));
			if (UseBuffer())
			{
				for (int index = 0; index < _buffer.Count; index++)
					if (_buffer[index].Bookmark == bookmark)
					{
						_bufferIndex = index;
						return true;
					}
				
				ClearBuffer();
				return SourceGotoBookmark(bookmark, forward);
			}
			return SourceGotoBookmark(bookmark, forward);
		}
		
        public int CompareBookmarks(Guid bookmark1, Guid bookmark2)
        {
			_plan._programStatisticsCached = false;
			return _cursor.CompareBookmarks(bookmark1, bookmark2, _plan._process.GetProcessCallInfo());
		}
		
		protected void SourceDisposeBookmark(Guid bookmark)
		{
			_cursor.DisposeBookmark(bookmark, _plan._process.GetProcessCallInfo());
			_plan._programStatisticsCached = false;
		}

		protected void SourceDisposeBookmarks(Guid[] bookmarks)
		{
			_cursor.DisposeBookmarks(bookmarks, _plan._process.GetProcessCallInfo());
			_plan._programStatisticsCached = false;
		}

		protected void BufferDisposeBookmark(Guid bookmark)
		{
			if (bookmark != Guid.Empty)
			{
				LocalBookmark localBookmark = _bookmarks[bookmark];
				if (localBookmark == null)
					throw new ServerException(ServerException.Codes.InvalidBookmark, bookmark.ToString());
					
				localBookmark.ReferenceCount--;
				
				if (localBookmark.ReferenceCount == 0)
				{
					SourceDisposeBookmark(localBookmark.Bookmark);
					_bookmarks.Remove(localBookmark.Bookmark);
				}
			}
		}

		protected void BufferDisposeBookmarks(Guid[] bookmarks)
		{
			// Dereference each bookmark and prepare list of unreferenced bookmarks
			List<Guid> toDispose = new List<Guid>(bookmarks.Length);
			for (int index = 0; index < bookmarks.Length; index++)
			{
				LocalBookmark bookmark;
				if (_bookmarks.TryGetValue(bookmarks[index], out bookmark))
				{
					bookmark.ReferenceCount--;
					if (bookmark.ReferenceCount == 0)
					{
						toDispose.Add(bookmark.Bookmark);
						_bookmarks.Remove(bookmark.Bookmark);
					}
				}
				else
					toDispose.Add(bookmarks[index]);
			}

			// Free all unreferenced bookmarks together
			if (toDispose.Count > 0)
				SourceDisposeBookmarks(toDispose.ToArray());
		}

		public void DisposeBookmark(Guid bookmark)
		{
			if (BufferActive())
				BufferDisposeBookmark(bookmark);
			else
				SourceDisposeBookmark(bookmark);
		}

		public void DisposeBookmarks(Guid[] bookmarks)
		{
			if (BufferActive())
				BufferDisposeBookmarks(bookmarks);
			else
				SourceDisposeBookmarks(bookmarks);
		}
		
		public Schema.Order Order { get { return _plan.Order; } }
		
        public IRow GetKey()
        {
			if (BufferActive())
				SyncSource(true);
			RemoteRow key = _cursor.GetKey(_plan._process.GetProcessCallInfo());
			_plan._programStatisticsCached = false;
			Row row;
			Schema.RowType type = new Schema.RowType();
			foreach (string stringValue in key.Header.Columns)
				type.Columns.Add(((Schema.TableType)_plan.DataType).Columns[stringValue].Copy());
			row = new Row(_plan._process.ValueManager, type);
			row.ValuesOwned = false;
			row.AsPhysical = key.Body.Data;
			return row;
		}
		
        public bool FindKey(IRow key)
        {
			RemoteRow localKey = new RemoteRow();
			_plan._process.EnsureOverflowConsistent(key);
			localKey.Header = new RemoteRowHeader();
			localKey.Header.Columns = new string[key.DataType.Columns.Count];
			for (int index = 0; index < key.DataType.Columns.Count; index++)
				localKey.Header.Columns[index] = key.DataType.Columns[index].Name;
			localKey.Body = new RemoteRowBody();
			localKey.Body.Data = key.AsPhysical;
			RemoteGotoData gotoData = _cursor.FindKey(localKey, _plan._process.GetProcessCallInfo());
			SetFlags(gotoData.Flags);
			_plan._programStatisticsCached = false;
			if (gotoData.Success && BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			return gotoData.Success;
		}
		
        public void FindNearest(IRow key)
        {
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			_plan._process.EnsureOverflowConsistent(key);
			RemoteRow localKey = new RemoteRow();
			localKey.Header = new RemoteRowHeader();
			localKey.Header.Columns = new string[key.DataType.Columns.Count];
			for (int index = 0; index < key.DataType.Columns.Count; index++)
				localKey.Header.Columns[index] = key.DataType.Columns[index].Name;
			localKey.Body = new RemoteRowBody();
			localKey.Body.Data = key.AsPhysical;
			SetFlags(_cursor.FindNearest(localKey, _plan._process.GetProcessCallInfo()));
			_plan._programStatisticsCached = false;
		}
		
        public bool Refresh(IRow row)
        {
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			RemoteRow localRow = new RemoteRow();
			_plan._process.EnsureOverflowConsistent(row);
			localRow.Header = new RemoteRowHeader();
			localRow.Header.Columns = new string[row.DataType.Columns.Count];
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				localRow.Header.Columns[index] = row.DataType.Columns[index].Name;
			localRow.Body = new RemoteRowBody();
			localRow.Body.Data = row.AsPhysical;
			RemoteGotoData gotoData = _cursor.Refresh(localRow, _plan._process.GetProcessCallInfo());
			SetFlags(gotoData.Flags);
			_plan._programStatisticsCached = false;
			return gotoData.Success;
		}
		
        public int RowCount()
        {
			_plan._programStatisticsCached = false;
			return _cursor.RowCount(_plan._process.GetProcessCallInfo());
		}
		
		public Schema.TableType DataType { get { return (Schema.TableType)_plan.DataType; } }
		public Schema.TableVar TableVar { get { return _plan.TableVar; } }
		public TableNode TableNode { get { return _plan.TableNode; } }
		
		// Copies the values from source row to the given target row, without using stream referencing
		// ASourceRow and ATargetRow must be of equivalent row types.
		protected void MarshalRow(IRow sourceRow, IRow targetRow)
		{
			for (int index = 0; index < sourceRow.DataType.Columns.Count; index++)
				if (sourceRow.HasValue(index))
				{
					if (sourceRow.HasNonNativeValue(index))
					{
						Scalar scalar = new Scalar(targetRow.Manager, (Schema.ScalarType)sourceRow.DataType.Columns[index].DataType);
						Stream sourceStream = sourceRow.GetValue(index).OpenStream();
						try
						{
							Stream targetStream = scalar.OpenStream();
							try
							{
								StreamUtility.CopyStream(sourceStream, targetStream);
							}
							finally
							{
								targetStream.Close();
							}
						}
						finally
						{
							sourceStream.Close();
						}
						targetRow[index] = scalar;
					}
					else
						targetRow[index] = sourceRow[index];
				}
				else
					targetRow.ClearValue(index);
		}
		
        /// <summary>Requests the default values for a new row in the cursor.</summary>        
        /// <param name='row'>A <see cref="Row"/> to be filled in with default values.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="row"/>.</returns>
        public bool Default(IRow row, string columnName)
        {
			if ((_internalProcess != null) && TableVar.IsDefaultCallRemotable(columnName))
			{
				// create a new row based on FInternalProcess, and copy the data from 
				IRow localRow = row;
				if (row.HasNonNativeValues())
				{
					localRow = new Row(_internalProcess.ValueManager, row.DataType);
					MarshalRow(row, localRow);
				}

				_plan._process._session._server.AcquireCacheLock(_plan._process, LockMode.Shared);
				try
				{
					bool changed = TableNode.Default(_internalProgram, null, localRow, null, columnName);
					if (changed && !Object.ReferenceEquals(localRow, row))
						MarshalRow(localRow, row);
					return changed;
				}
				finally
				{
					_plan._process._session._server.ReleaseCacheLock(_plan._process, LockMode.Shared);
				}
			}
			else
			{
				_plan._process.EnsureOverflowReleased(row);
				RemoteRowBody body = new RemoteRowBody();
				body.Data = row.AsPhysical;

				RemoteProposeData proposeData = _cursor.Default(body, columnName, _plan._process.GetProcessCallInfo());
				_plan._programStatisticsCached = false;

				if (proposeData.Success)
				{
					row.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server for the course of the default call.
					row.AsPhysical = proposeData.Body.Data;
					row.ValuesOwned = true;
				}
				return proposeData.Success;
			}
        }
        
        /// <summary>Requests the affect of a change to the given row.</summary>
        /// <param name='oldRow'>A <see cref="Row"/> containing the original values for the row.</param>
        /// <param name='newRow'>A <see cref="Row"/> containing the changed values for the row.</param>
        /// <param name='columnName'>The name of the column which changed in <paramref name="newRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="newRow"/>.</returns>
        public bool Change(IRow oldRow, IRow newRow, string columnName)
        {
			// if the table level change is remotable and the named column is remotable or no column is named and all columns are remotable
				// the change can be evaluated locally, otherwise a remote call is required
			if ((_internalProcess != null) && TableVar.IsChangeCallRemotable(columnName))
			{
				IRow localOldRow = oldRow;
				if (oldRow.HasNonNativeValues())
				{
					localOldRow = new Row(_internalProcess.ValueManager, oldRow.DataType);
					MarshalRow(oldRow, localOldRow);
				}
				
				IRow localNewRow = newRow;
				if (newRow.HasNonNativeValues())
				{
					localNewRow = new Row(_internalProcess.ValueManager, newRow.DataType);
					MarshalRow(newRow, localNewRow);
				}

				_plan._process._session._server.AcquireCacheLock(_plan._process, LockMode.Shared);
				try
				{
					bool changed = TableNode.Change(_internalProgram, localOldRow, localNewRow, null, columnName);
					if (changed && !Object.ReferenceEquals(localNewRow, newRow))
						MarshalRow(localNewRow, newRow);
					return changed;
				}
				finally
				{
					_plan._process._session._server.ReleaseCacheLock(_plan._process, LockMode.Shared);
				}
			}
			else
			{			
				_plan._process.EnsureOverflowReleased(oldRow);
				RemoteRowBody oldBody = new RemoteRowBody();
				oldBody.Data = oldRow.AsPhysical;
				
				_plan._process.EnsureOverflowReleased(newRow);
				RemoteRowBody newBody = new RemoteRowBody();
				newBody.Data = newRow.AsPhysical;

				RemoteProposeData proposeData = _cursor.Change(oldBody, newBody, columnName, _plan._process.GetProcessCallInfo());
				_plan._programStatisticsCached = false;

				if (proposeData.Success)
				{
					newRow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server during the change call
					newRow.AsPhysical = proposeData.Body.Data;
					newRow.ValuesOwned = true;
				}

				return proposeData.Success;
			}
        }
        
        /// <summary>Ensures that the given row is valid.</summary>
        /// <param name='oldRow'>A <see cref="Row"/> containing the original values for the row.</param>
        /// <param name='newRow'>A <see cref="Row"/> containing the changed values for the row.</param>
        /// <param name='columnName'>The name of the column which changed in <paramref name="newRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="newRow"/>.</returns>
        public bool Validate(IRow oldRow, IRow newRow, string columnName)
        {
			if ((_internalProcess != null) && TableVar.IsValidateCallRemotable(columnName))
			{
				IRow localOldRow = oldRow;
				if ((oldRow != null) && oldRow.HasNonNativeValues())
				{
					localOldRow = new Row(_internalProcess.ValueManager, oldRow.DataType);
					MarshalRow(oldRow, localOldRow);
				}
				
				IRow localNewRow = newRow;
				if (newRow.HasNonNativeValues())
				{
					localNewRow = new Row(_internalProcess.ValueManager, newRow.DataType);
					MarshalRow(newRow, localNewRow);
				}

				_plan._process._session._server.AcquireCacheLock(_plan._process, LockMode.Shared);
				try
				{
					bool changed = TableNode.Validate(_internalProgram, localOldRow, localNewRow, null, columnName);
					if (changed && !Object.ReferenceEquals(newRow, localNewRow))
						MarshalRow(localNewRow, newRow);
					return changed;
				}
				finally
				{
					_plan._process._session._server.ReleaseCacheLock(_plan._process, LockMode.Shared);
				}
			}
			else
			{
				RemoteRowBody oldBody = new RemoteRowBody();
				if (oldRow != null)
				{
					_plan._process.EnsureOverflowReleased(oldRow);
					oldBody.Data = oldRow.AsPhysical;
				}
				
				_plan._process.EnsureOverflowReleased(newRow);
				RemoteRowBody newBody = new RemoteRowBody();
				newBody.Data = newRow.AsPhysical;
				
				RemoteProposeData proposeData = _cursor.Validate(oldBody, newBody, columnName, _plan._process.GetProcessCallInfo());
				_plan._programStatisticsCached = false;

				if (proposeData.Success)
				{
					newRow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server during the validate call
					newRow.AsPhysical = proposeData.Body.Data;
					newRow.ValuesOwned = true;
				}
				return proposeData.Success;
			}
        }
    }
}
