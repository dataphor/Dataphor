/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerCursor
	public class RemoteServerCursor : RemoteServerChildObject, IRemoteServerCursor
	{
		public RemoteServerCursor(RemoteServerExpressionPlan plan, ServerCursor serverCursor) : base()
		{
			_plan = plan;
			_serverCursor = serverCursor;
			AttachServerCursor();
		}
		
		private void AttachServerCursor()
		{
			_serverCursor.Disposed += new EventHandler(FServerCursorDisposed);
		}

		void FServerCursorDisposed(object sender, EventArgs args)
		{
			DetachServerCursor();
			_serverCursor = null;
			Dispose();
		}
		
		private void DetachServerCursor()
		{
			_serverCursor.Disposed -= new EventHandler(FServerCursorDisposed);
		}

		protected override void Dispose(bool disposing)
		{
			if (_serverCursor != null)
			{
				DetachServerCursor();
				_serverCursor.Dispose();
				_serverCursor = null;
			}

			base.Dispose(disposing);
		}
		
		private RemoteServerExpressionPlan _plan;
		public RemoteServerExpressionPlan Plan { get { return _plan; } }
		
		IRemoteServerExpressionPlan IRemoteServerCursor.Plan { get { return _plan; } }
		
		private ServerCursor _serverCursor;
		internal ServerCursor ServerCursor { get { return _serverCursor; } }
		
		protected Exception WrapException(Exception exception)
		{
			return RemoteServer.WrapException(exception);
		}
		
		// IActive

		// Open        
		public void Open()
		{
			_serverCursor.Open();
			// TODO: Out params
		}
        
		// Close
		public void Close()
		{
			_serverCursor.Close();
		}
        
		// Active
		public bool Active
		{
			get { return _serverCursor.Active; }
			set { _serverCursor.Active = value; }
		}
        
		// Isolation
		public CursorIsolation Isolation { get { return _serverCursor.Isolation; } }
		
		// CursorType
		public CursorType CursorType { get { return _serverCursor.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return _serverCursor.Capabilities; } }

		public bool Supports(CursorCapability capability)
		{
			return _serverCursor.Supports(capability);
		}

		private Schema.IRowType GetRowType(RemoteRowHeader header)
		{
			Schema.IRowType rowType = new Schema.RowType();
			for (int index = 0; index < header.Columns.Length; index++)
				rowType.Columns.Add(_serverCursor.SourceRowType.Columns[header.Columns[index]].Copy());
			return rowType;
		}
		
		// IRemoteServerCursor
		/// <summary> Returns the current row of the cursor. </summary>
		/// <param name="header"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the row information. </returns>
		public RemoteRowBody Select(RemoteRowHeader header, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Row row = new Row(_plan.Process.ServerProcess.ValueManager, GetRowType(header));
				try
				{
					row.ValuesOwned = false;
					_serverCursor.Select(row);
					RemoteRowBody body = new RemoteRowBody();
					body.Data = row.AsPhysical;
					return body;
				}
				finally
				{
					row.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		public RemoteRowBody Select(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Row row = new Row(_plan.Process.ServerProcess.ValueManager, _serverCursor.SourceRowType);
				try
				{
					row.ValuesOwned = false;
					_serverCursor.Select(row);
					RemoteRowBody body = new RemoteRowBody();
					body.Data = row.AsPhysical;
					return body;
				}
				finally
				{
					row.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		private RemoteFetchData InternalFetch(Schema.IRowType rowType, Guid[] bookmarks, int count, bool skipCurrent)
		{
			Row[] rows = new Row[Math.Abs(count)];
			try
			{
				for (int index = 0; index < rows.Length; index++)
				{
					rows[index] = new Row(_plan.Process.ServerProcess.ValueManager, rowType);
					rows[index].ValuesOwned = false;
				}
				
				CursorGetFlags flags;
				int localCount = _serverCursor.Fetch(rows, bookmarks, count, skipCurrent, out flags);
				
				RemoteFetchData fetchData = new RemoteFetchData();
				fetchData.Body = new RemoteRowBody[localCount];
				for (int index = 0; index < localCount; index++)
					fetchData.Body[index].Data = rows[index].AsPhysical;
				
				fetchData.Flags = flags;
				return fetchData;
			}
			finally
			{
				for (int index = 0; index < rows.Length; index++)
					if (rows[index] != null)
						rows[index].Dispose();
			}
		}
        
		/// <summary> Returns the requested number of rows from the cursor. </summary>
		/// <param name="header"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <param name='count'> The number of rows to fetch, with a negative number indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </returns>
		public RemoteFetchData Fetch(RemoteRowHeader header, out Guid[] bookmarks, int count, bool skipCurrent, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);

			try
			{
				bookmarks = new Guid[Math.Abs(count)];
				return InternalFetch(GetRowType(header), bookmarks, count, skipCurrent);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		public RemoteFetchData Fetch(out Guid[] bookmarks, int count, bool skipCurrent, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				bookmarks = new Guid[Math.Abs(count)];
				return InternalFetch(_serverCursor.SourceRowType, bookmarks, count, skipCurrent);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		/// <summary> Indicates whether the cursor is on the BOF crack, the EOF crack, or both, which indicates an empty cursor. </summary>
		/// <returns> A <see cref="CursorGetFlags"/> value indicating the current state of the cursor. </returns>
		public CursorGetFlags GetFlags(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.GetFlags();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Provides a mechanism for navigating the cursor by a specified number of rows. </summary>        
		/// <param name='delta'> The number of rows to move by, with a negative value indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteMoveData"/> structure containing the result of the move. </returns>
		public RemoteMoveData MoveBy(int delta, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				RemoteMoveData moveData = new RemoteMoveData();
				moveData.Count = _serverCursor.MoveBy(delta, out moveData.Flags);
				return moveData;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Positions the cursor on the BOF crack. </summary>
		/// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public CursorGetFlags First(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.MoveTo(true);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Positions the cursor on the EOF crack. </summary>
		/// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public CursorGetFlags Last(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.MoveTo(false);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Resets the server-side cursor, causing any data to be re-read and leaving the cursor on the BOF crack. </summary>        
		/// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the reset. </returns>
		public CursorGetFlags Reset(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.ResetWithFlags();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Inserts the given <see cref="RemoteRow"/> into the cursor. </summary>        
		/// <param name="row"> A <see cref="RemoteRow"/> structure containing the Row to be inserted. </param>
		public void Insert(RemoteRow row, BitArray valueFlags, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Schema.RowType type = new Schema.RowType();
				foreach (string stringValue in row.Header.Columns)
					type.Columns.Add(_serverCursor.SourceRowType.Columns[stringValue].Copy());
				Row localRow = new Row(_plan.Process.ServerProcess.ValueManager, type);
				try
				{
					localRow.ValuesOwned = false;
					localRow.AsPhysical = row.Body.Data;
					_serverCursor.Insert(localRow, valueFlags);
				}
				finally
				{
					localRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Updates the current row of the cursor using the given <see cref="RemoteRow"/>. </summary>        
		/// <param name="row"> A <see cref="RemoteRow"/> structure containing the Row to be updated. </param>
		public void Update(RemoteRow row, BitArray valueFlags, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Schema.RowType type = new Schema.RowType();
				foreach (string stringValue in row.Header.Columns)
					type.Columns.Add(_serverCursor.SourceRowType.Columns[stringValue].Copy());
				Row localRow = new Row(_plan.Process.ServerProcess.ValueManager, type);
				try
				{
					localRow.ValuesOwned = false;
					localRow.AsPhysical = row.Body.Data;
					_serverCursor.Update(localRow, valueFlags);
				}
				finally
				{
					localRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Deletes the current DataBuffer from the cursor. </summary>
		public void Delete(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				_serverCursor.Delete();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public const int BookmarkTypeInt = 0;
		public const int BookmarkTypeRow = 1;

		/// <summary> Gets a bookmark for the current DataBuffer suitable for use in the <c>GotoBookmark</c> and <c>CompareBookmark</c> methods. </summary>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </returns>
		public Guid GetBookmark(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.GetBookmark();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Positions the cursor on the DataBuffer denoted by the given bookmark obtained from a previous call to <c> GetBookmark </c> . </summary>
		/// <param name="bookmark"> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the goto call. </returns>
		public RemoteGotoData GotoBookmark(Guid bookmark, bool forward, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				RemoteGotoData gotoData = new RemoteGotoData();
				gotoData.Success = _serverCursor.GotoBookmark(bookmark, forward, out gotoData.Flags);
				return gotoData;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Compares the value of two bookmarks obtained from previous calls to <c>GetBookmark</c> . </summary>        
		/// <param name="bookmark1"> A <see cref="RemoteRowBody"/> structure containing the data for the first bookmark to compare. </param>
		/// <param name="bookmark2"> A <see cref="RemoteRowBody"/> structure containing the data for the second bookmark to compare. </param>
		/// <returns> An integer value indicating whether the first bookmark was less than (negative), equal to (0) or greater than (positive) the second bookmark. </returns>
		public int CompareBookmarks(Guid bookmark1, Guid bookmark2, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.CompareBookmarks(bookmark1, bookmark2);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Disposes a bookmark. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmark(Guid bookmark, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				_serverCursor.DisposeBookmark(bookmark);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] bookmarks, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				_serverCursor.DisposeBookmarks(bookmarks);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		/// <value> Accesses the <see cref="Order"/> of the cursor. </value>
		public string Order { get { return _serverCursor.Order.Name; } }
        
		/// <returns> A <see cref="RemoteRow"/> structure containing the key for current row. </returns>
		public RemoteRow GetKey(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				IRow key = _serverCursor.GetKey();
				RemoteRow row = new RemoteRow();
				row.Header = new RemoteRowHeader();
				row.Header.Columns = new string[key.DataType.Columns.Count];
				for (int index = 0; index < key.DataType.Columns.Count; index++)
					row.Header.Columns[index] = key.DataType.Columns[index].Name;
				row.Body = new RemoteRowBody();
				row.Body.Data = key.AsPhysical;
				return row;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Attempts to position the cursor on the row matching the given key.  If the key is not found, the cursor position remains unchanged. </summary>
		/// <param name="key"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the find. </returns>
		public RemoteGotoData FindKey(RemoteRow key, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Schema.RowType type = new Schema.RowType();
				for (int index = 0; index < key.Header.Columns.Length; index++)
					type.Columns.Add(_serverCursor.SourceRowType.Columns[key.Header.Columns[index]].Copy());

				Row localKey = new Row(_plan.Process.ServerProcess.ValueManager, type);
				try
				{
					localKey.ValuesOwned = false;
					localKey.AsPhysical = key.Body.Data;
					RemoteGotoData gotoData = new RemoteGotoData();
					gotoData.Success = _serverCursor.FindKey(localKey, out gotoData.Flags);
					return gotoData;
				}
				finally
				{
					localKey.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Positions the cursor on the record most closely matching the given key. </summary>
		/// <param name="key"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the search. </returns>
		public CursorGetFlags FindNearest(RemoteRow key, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Schema.RowType type = new Schema.RowType();
				for (int index = 0; index < key.Header.Columns.Length; index++)
					type.Columns.Add(_serverCursor.SourceRowType.Columns[key.Header.Columns[index]].Copy());

				Row localKey = new Row(_plan.Process.ServerProcess.ValueManager, type);
				try
				{
					localKey.ValuesOwned = false;
					localKey.AsPhysical = key.Body.Data;
					CursorGetFlags flags;
					_serverCursor.FindNearest(localKey, out flags);
					return flags;
				}
				finally
				{
					localKey.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Refreshes the cursor and attempts to reposition it on the given row. </summary>
		/// <param name="row"> A <see cref="RemoteRow"/> structure containing the row to be positioned on after the refresh. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the result of the refresh. </returns>
		public RemoteGotoData Refresh(RemoteRow row, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				Schema.RowType type = new Schema.RowType();
				for (int index = 0; index < row.Header.Columns.Length; index++)
					type.Columns.Add(_serverCursor.SourceRowType.Columns[row.Header.Columns[index]].Copy());

				Row localRow = new Row(_plan.Process.ServerProcess.ValueManager, type);
				try
				{
					localRow.ValuesOwned = false;
					localRow.AsPhysical = row.Body.Data;
					RemoteGotoData gotoData = new RemoteGotoData();
					gotoData.Success = _serverCursor.Refresh(localRow, out gotoData.Flags);
					return gotoData;
				}
				finally
				{
					localRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		// Countable
		/// <returns>An integer value indicating the number of rows in the cursor.</returns>
		public int RowCount(ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return _serverCursor.RowCount();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		private RemoteProposeData InternalDefault(RemoteRowBody row, string column)
		{
			Row localRow = new Row(_plan.Process.ServerProcess.ValueManager, _serverCursor.SourceRowType);
			try
			{
				localRow.ValuesOwned = false;
				localRow.AsPhysical = row.Data;
				RemoteProposeData proposeData = new RemoteProposeData();
				proposeData.Success = _serverCursor.Default(localRow, column);
				proposeData.Body = new RemoteRowBody();
				proposeData.Body.Data = localRow.AsPhysical;
				return proposeData;
			}
			finally
			{
				localRow.Dispose();
			}
		}
		
		// IRemoteProposable
		/// <summary>
		///	Requests the default values for a new row in the cursor.  
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public RemoteProposeData Default(RemoteRowBody row, string column, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return InternalDefault(row, column);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		private RemoteProposeData InternalChange(RemoteRowBody oldRow, RemoteRowBody newRow, string column)
		{
			Row localOldRow = new Row(_plan.Process.ServerProcess.ValueManager, _serverCursor.SourceRowType);
			try
			{
				localOldRow.ValuesOwned = false;
				localOldRow.AsPhysical = oldRow.Data;
				
				Row localNewRow = new Row(_plan.Process.ServerProcess.ValueManager, _serverCursor.SourceRowType);
				try
				{
					localNewRow.ValuesOwned = false;
					localNewRow.AsPhysical = newRow.Data;
					RemoteProposeData proposeData = new RemoteProposeData();
					proposeData.Success = _serverCursor.Change(localOldRow, localNewRow, column);
					proposeData.Body = new RemoteRowBody();
					proposeData.Body.Data = localNewRow.AsPhysical;
					return proposeData;
				}
				finally
				{
					localNewRow.Dispose();
				}
			}
			finally
			{
				localOldRow.Dispose();
			}
		}
        
		/// <summary>
		/// Requests the affect of a change to the given row. 
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public RemoteProposeData Change(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return InternalChange(oldRow, newRow, column);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		private RemoteProposeData InternalValidate(RemoteRowBody oldRow, RemoteRowBody newRow, string column)
		{
			Row localOldRow = null;
			if (oldRow.Data != null)
				localOldRow = new Row(_plan.Process.ServerProcess.ValueManager, _serverCursor.SourceRowType);
			try
			{
				if (localOldRow != null)
				{
					localOldRow.ValuesOwned = false;
					localOldRow.AsPhysical = oldRow.Data;
				}
				
				Row localNewRow = new Row(_plan.Process.ServerProcess.ValueManager, _serverCursor.SourceRowType);
				try
				{
					localNewRow.ValuesOwned = false;
					localNewRow.AsPhysical = newRow.Data;
					RemoteProposeData proposeData = new RemoteProposeData();
					proposeData.Success = _serverCursor.Validate(localOldRow, localNewRow, column);
					proposeData.Body = new RemoteRowBody();
					proposeData.Body.Data = localNewRow.AsPhysical;
					return proposeData;
				}
				finally
				{
					localNewRow.Dispose();
				}
			}
			finally
			{
				if (localOldRow != null)
					localOldRow.Dispose();
			}
		}

		/// <summary>
		/// Ensures that the given row is valid.
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="column"></param>
		public RemoteProposeData Validate(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)
		{
			_plan.Process.ProcessCallInfo(callInfo);
			try
			{
				return InternalValidate(newRow, oldRow, column);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
	}
}
