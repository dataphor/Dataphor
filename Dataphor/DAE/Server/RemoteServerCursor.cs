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

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerCursor
	public class RemoteServerCursor : RemoteServerChildObject, IRemoteServerCursor
	{
		public RemoteServerCursor(RemoteServerExpressionPlan APlan, ServerCursor AServerCursor) : base()
		{
			FPlan = APlan;
			FServerCursor = AServerCursor;
			AttachServerCursor();
		}
		
		private void AttachServerCursor()
		{
			FServerCursor.Disposed += new EventHandler(FServerCursorDisposed);
		}

		void FServerCursorDisposed(object ASender, EventArgs AArgs)
		{
			DetachServerCursor();
			FServerCursor = null;
			Dispose();
		}
		
		private void DetachServerCursor()
		{
			FServerCursor.Disposed -= new EventHandler(FServerCursorDisposed);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FServerCursor != null)
			{
				DetachServerCursor();
				FServerCursor.Dispose();
				FServerCursor = null;
			}

			base.Dispose(ADisposing);
		}
		
		private RemoteServerExpressionPlan FPlan;
		public RemoteServerExpressionPlan Plan { get { return FPlan; } }
		
		IRemoteServerExpressionPlan IRemoteServerCursor.Plan { get { return FPlan; } }
		
		private ServerCursor FServerCursor;
		internal ServerCursor ServerCursor { get { return FServerCursor; } }
		
		protected Exception WrapException(Exception AException)
		{
			return RemoteServer.WrapException(AException);
		}
		
		// IActive

		// Open        
		public void Open()
		{
			FServerCursor.Open();
			// TODO: Out params
		}
        
		// Close
		public void Close()
		{
			FServerCursor.Close();
		}
        
		// Active
		public bool Active
		{
			get { return FServerCursor.Active; }
			set { FServerCursor.Active = value; }
		}
        
		// Isolation
		public CursorIsolation Isolation { get { return FServerCursor.Isolation; } }
		
		// CursorType
		public CursorType CursorType { get { return FServerCursor.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return FServerCursor.Capabilities; } }

		public bool Supports(CursorCapability ACapability)
		{
			return FServerCursor.Supports(ACapability);
		}

		private Schema.IRowType GetRowType(RemoteRowHeader AHeader)
		{
			Schema.IRowType LRowType = new Schema.RowType();
			for (int LIndex = 0; LIndex < AHeader.Columns.Length; LIndex++)
				LRowType.Columns.Add(FServerCursor.SourceRowType.Columns[AHeader.Columns[LIndex]].Copy());
			return LRowType;
		}
		
		// IRemoteServerCursor
		/// <summary> Returns the current row of the cursor. </summary>
		/// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the row information. </returns>
		public RemoteRowBody Select(RemoteRowHeader AHeader, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Row LRow = new Row(FPlan.Process.ServerProcess.ValueManager, GetRowType(AHeader));
				try
				{
					LRow.ValuesOwned = false;
					FServerCursor.Select(LRow);
					RemoteRowBody LBody = new RemoteRowBody();
					LBody.Data = LRow.AsPhysical;
					return LBody;
				}
				finally
				{
					LRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		public RemoteRowBody Select(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Row LRow = new Row(FPlan.Process.ServerProcess.ValueManager, FServerCursor.SourceRowType);
				try
				{
					LRow.ValuesOwned = false;
					FServerCursor.Select(LRow);
					RemoteRowBody LBody = new RemoteRowBody();
					LBody.Data = LRow.AsPhysical;
					return LBody;
				}
				finally
				{
					LRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		private RemoteFetchData InternalFetch(Schema.IRowType ARowType, Guid[] ABookmarks, int ACount)
		{
			Row[] LRows = new Row[Math.Abs(ACount)];
			try
			{
				for (int LIndex = 0; LIndex < LRows.Length; LIndex++)
				{
					LRows[LIndex] = new Row(FPlan.Process.ServerProcess.ValueManager, ARowType);
					LRows[LIndex].ValuesOwned = false;
				}
				
				RemoteCursorGetFlags LFlags;
				int LCount = FServerCursor.Fetch(LRows, ABookmarks, ACount, out LFlags);
				
				RemoteFetchData LFetchData = new RemoteFetchData();
				LFetchData.Body = new RemoteRowBody[LCount];
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					LFetchData.Body[LIndex].Data = LRows[LIndex].AsPhysical;
				
				LFetchData.Flags = (byte)LFlags; //HACK: cast to fix MSs error
				return LFetchData;
			}
			finally
			{
				for (int LIndex = 0; LIndex < LRows.Length; LIndex++)
					if (LRows[LIndex] != null)
						LRows[LIndex].Dispose();
			}
		}
        
		/// <summary> Returns the requested number of rows from the cursor. </summary>
		/// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <param name='ACount'> The number of rows to fetch, with a negative number indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </returns>
		public RemoteFetchData Fetch(RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);

			try
			{
				ABookmarks = new Guid[Math.Abs(ACount)];
				return InternalFetch(GetRowType(AHeader), ABookmarks, ACount);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		public RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				ABookmarks = new Guid[Math.Abs(ACount)];
				return InternalFetch(FServerCursor.SourceRowType, ABookmarks, ACount);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		/// <summary> Indicates whether the cursor is on the BOF crack, the EOF crack, or both, which indicates an empty cursor. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the current state of the cursor. </returns>
		public RemoteCursorGetFlags GetFlags(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.GetFlags();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Provides a mechanism for navigating the cursor by a specified number of rows. </summary>        
		/// <param name='ADelta'> The number of rows to move by, with a negative value indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteMoveData"/> structure containing the result of the move. </returns>
		public RemoteMoveData MoveBy(int ADelta, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				RemoteMoveData LMoveData = new RemoteMoveData();
				LMoveData.Count = FServerCursor.MoveBy(ADelta, out LMoveData.Flags);
				return LMoveData;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Positions the cursor on the BOF crack. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public RemoteCursorGetFlags First(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.MoveTo(true);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Positions the cursor on the EOF crack. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public RemoteCursorGetFlags Last(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.MoveTo(false);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Resets the server-side cursor, causing any data to be re-read and leaving the cursor on the BOF crack. </summary>        
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the reset. </returns>
		public RemoteCursorGetFlags Reset(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.ResetWithFlags();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Inserts the given <see cref="RemoteRow"/> into the cursor. </summary>        
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be inserted. </param>
		public void Insert(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Schema.RowType LType = new Schema.RowType();
				foreach (string LString in ARow.Header.Columns)
					LType.Columns.Add(FServerCursor.SourceRowType.Columns[LString].Copy());
				Row LRow = new Row(FPlan.Process.ServerProcess.ValueManager, LType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = ARow.Body.Data;
					FServerCursor.Insert(LRow, AValueFlags);
				}
				finally
				{
					LRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Updates the current row of the cursor using the given <see cref="RemoteRow"/>. </summary>        
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be updated. </param>
		public void Update(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Schema.RowType LType = new Schema.RowType();
				foreach (string LString in ARow.Header.Columns)
					LType.Columns.Add(FServerCursor.SourceRowType.Columns[LString].Copy());
				Row LRow = new Row(FPlan.Process.ServerProcess.ValueManager, LType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = ARow.Body.Data;
					FServerCursor.Update(LRow, AValueFlags);
				}
				finally
				{
					LRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Deletes the current DataBuffer from the cursor. </summary>
		public void Delete(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				FServerCursor.Delete();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public const int CBookmarkTypeInt = 0;
		public const int CBookmarkTypeRow = 1;

		/// <summary> Gets a bookmark for the current DataBuffer suitable for use in the <c>GotoBookmark</c> and <c>CompareBookmark</c> methods. </summary>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </returns>
		public Guid GetBookmark(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.GetBookmark();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Positions the cursor on the DataBuffer denoted by the given bookmark obtained from a previous call to <c> GetBookmark </c> . </summary>
		/// <param name="ABookmark"> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the goto call. </returns>
		public RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				RemoteGotoData LGotoData = new RemoteGotoData();
				LGotoData.Success = FServerCursor.GotoBookmark(ABookmark, AForward, out LGotoData.Flags);
				return LGotoData;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Compares the value of two bookmarks obtained from previous calls to <c>GetBookmark</c> . </summary>        
		/// <param name="ABookmark1"> A <see cref="RemoteRowBody"/> structure containing the data for the first bookmark to compare. </param>
		/// <param name="ABookmark2"> A <see cref="RemoteRowBody"/> structure containing the data for the second bookmark to compare. </param>
		/// <returns> An integer value indicating whether the first bookmark was less than (negative), equal to (0) or greater than (positive) the second bookmark. </returns>
		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.CompareBookmarks(ABookmark1, ABookmark2);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Disposes a bookmark. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmark(Guid ABookmark, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				FServerCursor.DisposeBookmark(ABookmark);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] ABookmarks, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				FServerCursor.DisposeBookmarks(ABookmarks);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		/// <value> Accesses the <see cref="Order"/> of the cursor. </value>
		public string Order { get { return FServerCursor.Order.Name; } }
        
		/// <returns> A <see cref="RemoteRow"/> structure containing the key for current row. </returns>
		public RemoteRow GetKey(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Row LKey = FServerCursor.GetKey();
				RemoteRow LRow = new RemoteRow();
				LRow.Header = new RemoteRowHeader();
				LRow.Header.Columns = new string[LKey.DataType.Columns.Count];
				for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
					LRow.Header.Columns[LIndex] = LKey.DataType.Columns[LIndex].Name;
				LRow.Body = new RemoteRowBody();
				LRow.Body.Data = LKey.AsPhysical;
				return LRow;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Attempts to position the cursor on the row matching the given key.  If the key is not found, the cursor position remains unchanged. </summary>
		/// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the find. </returns>
		public RemoteGotoData FindKey(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Schema.RowType LType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AKey.Header.Columns.Length; LIndex++)
					LType.Columns.Add(FServerCursor.SourceRowType.Columns[AKey.Header.Columns[LIndex]].Copy());

				Row LKey = new Row(FPlan.Process.ServerProcess.ValueManager, LType);
				try
				{
					LKey.ValuesOwned = false;
					LKey.AsPhysical = AKey.Body.Data;
					RemoteGotoData LGotoData = new RemoteGotoData();
					LGotoData.Success = FServerCursor.FindKey(LKey, out LGotoData.Flags);
					return LGotoData;
				}
				finally
				{
					LKey.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Positions the cursor on the record most closely matching the given key. </summary>
		/// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the search. </returns>
		public RemoteCursorGetFlags FindNearest(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Schema.RowType LType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AKey.Header.Columns.Length; LIndex++)
					LType.Columns.Add(FServerCursor.SourceRowType.Columns[AKey.Header.Columns[LIndex]].Copy());

				Row LKey = new Row(FPlan.Process.ServerProcess.ValueManager, LType);
				try
				{
					LKey.ValuesOwned = false;
					LKey.AsPhysical = AKey.Body.Data;
					RemoteCursorGetFlags LFlags;
					FServerCursor.FindNearest(LKey, out LFlags);
					return LFlags;
				}
				finally
				{
					LKey.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		/// <summary> Refreshes the cursor and attempts to reposition it on the given row. </summary>
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the row to be positioned on after the refresh. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the result of the refresh. </returns>
		public RemoteGotoData Refresh(RemoteRow ARow, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				Schema.RowType LType = new Schema.RowType();
				for (int LIndex = 0; LIndex < ARow.Header.Columns.Length; LIndex++)
					LType.Columns.Add(FServerCursor.SourceRowType.Columns[ARow.Header.Columns[LIndex]].Copy());

				Row LRow = new Row(FPlan.Process.ServerProcess.ValueManager, LType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = ARow.Body.Data;
					RemoteGotoData LGotoData = new RemoteGotoData();
					LGotoData.Success = FServerCursor.Refresh(LRow, out LGotoData.Flags);
					return LGotoData;
				}
				finally
				{
					LRow.Dispose();
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		// Countable
		/// <returns>An integer value indicating the number of rows in the cursor.</returns>
		public int RowCount(ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return FServerCursor.RowCount();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		private RemoteProposeData InternalDefault(RemoteRowBody ARow, string AColumn)
		{
			Row LRow = new Row(FPlan.Process.ServerProcess.ValueManager, FServerCursor.SourceRowType);
			try
			{
				LRow.ValuesOwned = false;
				LRow.AsPhysical = ARow.Data;
				RemoteProposeData LProposeData = new RemoteProposeData();
				LProposeData.Success = FServerCursor.Default(LRow, AColumn);
				LProposeData.Body = new RemoteRowBody();
				LProposeData.Body.Data = LRow.AsPhysical;
				return LProposeData;
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		// IRemoteProposable
		/// <summary>
		///	Requests the default values for a new row in the cursor.  
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="AColumn"></param>
		/// <returns></returns>
		public RemoteProposeData Default(RemoteRowBody ARow, string AColumn, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return InternalDefault(ARow, AColumn);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		private RemoteProposeData InternalChange(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			Row LOldRow = new Row(FPlan.Process.ServerProcess.ValueManager, FServerCursor.SourceRowType);
			try
			{
				LOldRow.ValuesOwned = false;
				LOldRow.AsPhysical = AOldRow.Data;
				
				Row LNewRow = new Row(FPlan.Process.ServerProcess.ValueManager, FServerCursor.SourceRowType);
				try
				{
					LNewRow.ValuesOwned = false;
					LNewRow.AsPhysical = ANewRow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = FServerCursor.Change(LOldRow, LNewRow, AColumn);
					LProposeData.Body = new RemoteRowBody();
					LProposeData.Body.Data = LNewRow.AsPhysical;
					return LProposeData;
				}
				finally
				{
					LNewRow.Dispose();
				}
			}
			finally
			{
				LOldRow.Dispose();
			}
		}
        
		/// <summary>
		/// Requests the affect of a change to the given row. 
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="AColumn"></param>
		/// <returns></returns>
		public RemoteProposeData Change(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return InternalChange(AOldRow, ANewRow, AColumn);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		private RemoteProposeData InternalValidate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			Row LOldRow = null;
			if (AOldRow.Data != null)
				LOldRow = new Row(FPlan.Process.ServerProcess.ValueManager, FServerCursor.SourceRowType);
			try
			{
				if (LOldRow != null)
				{
					LOldRow.ValuesOwned = false;
					LOldRow.AsPhysical = AOldRow.Data;
				}
				
				Row LNewRow = new Row(FPlan.Process.ServerProcess.ValueManager, FServerCursor.SourceRowType);
				try
				{
					LNewRow.ValuesOwned = false;
					LNewRow.AsPhysical = ANewRow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = FServerCursor.Validate(LOldRow, LNewRow, AColumn);
					LProposeData.Body = new RemoteRowBody();
					LProposeData.Body.Data = LNewRow.AsPhysical;
					return LProposeData;
				}
				finally
				{
					LNewRow.Dispose();
				}
			}
			finally
			{
				if (LOldRow != null)
					LOldRow.Dispose();
			}
		}

		/// <summary>
		/// Ensures that the given row is valid.
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="AColumn"></param>
		public RemoteProposeData Validate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			FPlan.Process.ProcessCallInfo(ACallInfo);
			try
			{
				return InternalValidate(ANewRow, AOldRow, AColumn);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
	}
}
