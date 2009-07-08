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

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerCursor
	public class RemoteServerCursor : ServerCursorBase, IRemoteServerCursor
	{
		public RemoteServerCursor(ServerPlanBase APlan, DataParams AParams) : base(APlan, AParams){}
		
		public IRemoteServerExpressionPlan Plan { get { return (IRemoteServerExpressionPlan)FPlan; } } 
		
		private Schema.IRowType GetRowType(RemoteRowHeader AHeader)
		{
			Schema.IRowType LRowType = FSourceTable.DataType.CreateRowType(false);
			for (int LIndex = 0; LIndex < AHeader.Columns.Length; LIndex++)
				LRowType.Columns.Add(FSourceTable.DataType.Columns[AHeader.Columns[LIndex]].Copy());
			return LRowType;
		}
		
		// IRemoteServerCursor
		/// <summary> Returns the current row of the cursor. </summary>
		/// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the row information. </returns>
		public RemoteRowBody Select(RemoteRowHeader AHeader, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Row LRow = new Row(FPlan.ServerProcess, GetRowType(AHeader));
					try
					{
						LRow.ValuesOwned = false;
						FSourceTable.Select(LRow);
						RemoteRowBody LBody = new RemoteRowBody();
						LBody.Data = LRow.AsPhysical;
						return LBody;
					}
					finally
					{
						LRow.Dispose();
					}
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
        
		public RemoteRowBody Select(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Row LRow = new Row(FPlan.ServerProcess, FSourceRowType);
					try
					{
						LRow.ValuesOwned = false;
						FSourceTable.Select(LRow);
						RemoteRowBody LBody = new RemoteRowBody();
						LBody.Data = LRow.AsPhysical;
						return LBody;
					}
					finally
					{
						LRow.Dispose();
					}
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
        
		// SourceFetch
		protected int SourceFetch(Row[] ARows, Guid[] ABookmarks, int ACount)
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
			return LCount;
		}
		
		private RemoteFetchData InternalFetch(Schema.IRowType ARowType, Guid[] ABookmarks, int ACount)
		{
			Row[] LRows = new Row[Math.Abs(ACount)];
			try
			{
				for (int LIndex = 0; LIndex < LRows.Length; LIndex++)
				{
					LRows[LIndex] = new Row(FPlan.ServerProcess, ARowType);
					LRows[LIndex].ValuesOwned = false;
				}
				
				int LCount = SourceFetch(LRows, ABookmarks, ACount);
				
				RemoteFetchData LFetchData = new RemoteFetchData();
				LFetchData.Body = new RemoteRowBody[LCount];
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					LFetchData.Body[LIndex].Data = LRows[LIndex].AsPhysical;
				
				LFetchData.Flags = (byte)InternalGetFlags();//hack:cast to fix MSs error
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
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					ABookmarks = new Guid[Math.Abs(ACount)];
					return InternalFetch(GetRowType(AHeader), ABookmarks, ACount);
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
        
		public RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					ABookmarks = new Guid[Math.Abs(ACount)];
					return InternalFetch(FSourceRowType, ABookmarks, ACount);
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

		/// <summary> Indicates whether the cursor is on the BOF crack, the EOF crack, or both, which indicates an empty cursor. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the current state of the cursor. </returns>
		public RemoteCursorGetFlags GetFlags(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
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

		/// <summary> Provides a mechanism for navigating the cursor by a specified number of rows. </summary>        
		/// <param name='ADelta'> The number of rows to move by, with a negative value indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteMoveData"/> structure containing the result of the move. </returns>
		public RemoteMoveData MoveBy(int ADelta, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
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
					RemoteMoveData LMoveData = new RemoteMoveData();
					LMoveData.Count = LDelta;
					LMoveData.Flags = InternalGetFlags();
					return LMoveData;
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

		/// <summary> Positions the cursor on the BOF crack. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public RemoteCursorGetFlags First(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.First();
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

		/// <summary> Positions the cursor on the EOF crack. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public RemoteCursorGetFlags Last(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
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

		/// <summary> Resets the server-side cursor, causing any data to be re-read and leaving the cursor on the BOF crack. </summary>        
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the reset. </returns>
		public RemoteCursorGetFlags Reset(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
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

		/// <summary> Inserts the given <see cref="RemoteRow"/> into the cursor. </summary>        
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be inserted. </param>
		public void Insert(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					foreach (string LString in ARow.Header.Columns)
						LType.Columns.Add(FSourceTable.DataType.Columns[LString].Copy());
					Row LRow = new Row(FPlan.ServerProcess, LType);
					try
					{
						LRow.ValuesOwned = false;
						LRow.AsPhysical = ARow.Body.Data;
						FSourceTable.Insert(null, LRow, AValueFlags, false);
					}
					finally
					{
						LRow.Dispose();
					}
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

		/// <summary> Updates the current row of the cursor using the given <see cref="RemoteRow"/>. </summary>        
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be updated. </param>
		public void Update(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					foreach (string LString in ARow.Header.Columns)
						LType.Columns.Add(FSourceTable.DataType.Columns[LString].Copy());
					Row LRow = new Row(FPlan.ServerProcess, LType);
					try
					{
						LRow.ValuesOwned = false;
						LRow.AsPhysical = ARow.Body.Data;
						FSourceTable.Update(LRow, AValueFlags, false);
					}
					finally
					{
						LRow.Dispose();
					}
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
        
		/// <summary> Deletes the current DataBuffer from the cursor. </summary>
		public void Delete(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.Delete();
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
		
		public const int CBookmarkTypeInt = 0;
		public const int CBookmarkTypeRow = 1;

		/// <summary> Gets a bookmark for the current DataBuffer suitable for use in the <c>GotoBookmark</c> and <c>CompareBookmark</c> methods. </summary>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </returns>
		public Guid GetBookmark(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
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

		/// <summary> Positions the cursor on the DataBuffer denoted by the given bookmark obtained from a previous call to <c> GetBookmark </c> . </summary>
		/// <param name="ABookmark"> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the goto call. </returns>
		public unsafe RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					RemoteGotoData LGotoData = new RemoteGotoData();
					LGotoData.Success = InternalGotoBookmark(ABookmark, AForward);
					LGotoData.Flags = InternalGetFlags();
					return LGotoData;
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

		/// <summary> Compares the value of two bookmarks obtained from previous calls to <c>GetBookmark</c> . </summary>        
		/// <param name="ABookmark1"> A <see cref="RemoteRowBody"/> structure containing the data for the first bookmark to compare. </param>
		/// <param name="ABookmark2"> A <see cref="RemoteRowBody"/> structure containing the data for the second bookmark to compare. </param>
		/// <returns> An integer value indicating whether the first bookmark was less than (negative), equal to (0) or greater than (positive) the second bookmark. </returns>
		public unsafe int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
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
		public void DisposeBookmark(Guid ABookmark, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					InternalDisposeBookmark(ABookmark);
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

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] ABookmarks, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					InternalDisposeBookmarks(ABookmarks);
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
		
		/// <value> Accesses the <see cref="Order"/> of the cursor. </value>
		public string Order { get { return FSourceTable.Node.Order.Name; } }
        
		/// <returns> A <see cref="RemoteRow"/> structure containing the key for current row. </returns>
		public RemoteRow GetKey(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Row LKey = FSourceTable.GetKey();
					RemoteRow LRow = new RemoteRow();
					LRow.Header = new RemoteRowHeader();
					LRow.Header.Columns = new string[LKey.DataType.Columns.Count];
					for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
						LRow.Header.Columns[LIndex] = LKey.DataType.Columns[LIndex].Name;
					LRow.Body = new RemoteRowBody();
					LRow.Body.Data = LKey.AsPhysical;
					return LRow;
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
        
		/// <summary> Attempts to position the cursor on the row matching the given key.  If the key is not found, the cursor position remains unchanged. </summary>
		/// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the find. </returns>
		public RemoteGotoData FindKey(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					for (int LIndex = 0; LIndex < AKey.Header.Columns.Length; LIndex++)
						LType.Columns.Add(FSourceTable.DataType.Columns[AKey.Header.Columns[LIndex]].Copy());

					Row LKey = new Row(FPlan.ServerProcess, LType);
					try
					{
						LKey.ValuesOwned = false;
						LKey.AsPhysical = AKey.Body.Data;
						RemoteGotoData LGotoData = new RemoteGotoData();
						LGotoData.Success = FSourceTable.FindKey(LKey);
						LGotoData.Flags = InternalGetFlags();
						return LGotoData;
					}
					finally
					{
						LKey.Dispose();
					}
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
        
		/// <summary> Positions the cursor on the record most closely matching the given key. </summary>
		/// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the search. </returns>
		public RemoteCursorGetFlags FindNearest(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					for (int LIndex = 0; LIndex < AKey.Header.Columns.Length; LIndex++)
						LType.Columns.Add(FSourceTable.DataType.Columns[AKey.Header.Columns[LIndex]].Copy());

					Row LKey = new Row(FPlan.ServerProcess, LType);
					try
					{
						LKey.ValuesOwned = false;
						LKey.AsPhysical = AKey.Body.Data;
						FSourceTable.FindNearest(LKey);
						return InternalGetFlags();
					}
					finally
					{
						LKey.Dispose();
					}
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
        
		/// <summary> Refreshes the cursor and attempts to reposition it on the given row. </summary>
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the row to be positioned on after the refresh. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the result of the refresh. </returns>
		public RemoteGotoData Refresh(RemoteRow ARow, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					for (int LIndex = 0; LIndex < ARow.Header.Columns.Length; LIndex++)
						LType.Columns.Add(FSourceTable.DataType.Columns[ARow.Header.Columns[LIndex]].Copy());

					Row LRow = new Row(FPlan.ServerProcess, LType);
					try
					{
						LRow.ValuesOwned = false;
						LRow.AsPhysical = ARow.Body.Data;
						RemoteGotoData LGotoData = new RemoteGotoData();
						LGotoData.Success = FSourceTable.Refresh(LRow);
						LGotoData.Flags = InternalGetFlags();
						return LGotoData;
					}
					finally
					{
						LRow.Dispose();
					}
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

		// Countable
		/// <returns>An integer value indicating the number of rows in the cursor.</returns>
		public int RowCount(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
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
		
		private RemoteProposeData InternalDefault(RemoteRowBody ARow, string AColumn)
		{
			Row LRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = ARow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = ((TableNode)SourceNode).Default(FPlan.ServerProcess, null, LRow, null, AColumn);
					LProposeData.Body = new RemoteRowBody();
					LProposeData.Body.Data = LRow.AsPhysical;
					return LProposeData;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
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
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalDefault(ARow, AColumn);
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
        
		private RemoteProposeData InternalChange(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			Row LOldRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
			try
			{
				LOldRow.ValuesOwned = false;
				LOldRow.AsPhysical = AOldRow.Data;
				
				Row LNewRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
				try
				{
					LNewRow.ValuesOwned = false;
					LNewRow.AsPhysical = ANewRow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = ((TableNode)SourceNode).Change(FPlan.ServerProcess, LOldRow, LNewRow, null, AColumn);
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
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalChange(AOldRow, ANewRow, AColumn);
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

		private RemoteProposeData InternalValidate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			Row LOldRow = null;
			if (AOldRow.Data != null)
				LOldRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
			try
			{
				if (LOldRow != null)
				{
					LOldRow.ValuesOwned = false;
					LOldRow.AsPhysical = AOldRow.Data;
				}
				
				Row LNewRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
				try
				{
					LNewRow.ValuesOwned = false;
					LNewRow.AsPhysical = ANewRow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = ((TableNode)SourceNode).Validate(FPlan.ServerProcess, LOldRow, LNewRow, null, AColumn);
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
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalValidate(ANewRow, AOldRow, AColumn);
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
