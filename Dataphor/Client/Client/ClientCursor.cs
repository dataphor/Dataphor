/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientCursor : IRemoteServerCursor
	{
		public ClientCursor(ClientExpressionPlan AClientExpressionPlan, CursorDescriptor ACursorDescriptor)
		{
			FClientExpressionPlan = AClientExpressionPlan;
			FCursorDescriptor = ACursorDescriptor;
		}
		
		private ClientExpressionPlan FClientExpressionPlan;
		public ClientExpressionPlan ClientExpressionPlan { get { return FClientExpressionPlan; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return FClientExpressionPlan.ClientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}
		
		private CursorDescriptor FCursorDescriptor;
		
		public int CursorHandle { get { return FCursorDescriptor.Handle; } }
		
		#region IRemoteServerCursor Members

		public IRemoteServerExpressionPlan Plan
		{
			get { return FClientExpressionPlan; }
		}

		public RemoteRowBody Select(RemoteRowHeader AHeader, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginSelectSpecific(CursorHandle, ACallInfo, AHeader, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndSelectSpecific(LResult);
		}

		public RemoteRowBody Select(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginSelect(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndSelect(LResult);
		}

		public RemoteFetchData Fetch(RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginFetchSpecific(CursorHandle, ACallInfo, AHeader, out ABookmarks, ACount, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndFetchSpecific(LResult);
		}

		public RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginFetch(CursorHandle, ACallInfo, out ABookmarks, ACount, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndFetch(LResult);
		}

		public CursorGetFlags GetFlags(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetFlags(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetFlags(LResult);
		}

		public RemoteMoveData MoveBy(int ADelta, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginMoveBy(CursorHandle, ACallInfo, ADelta, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndMoveBy(LResult);
		}

		public CursorGetFlags First(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginFirst(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndFirst(LResult);
		}

		public CursorGetFlags Last(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginLast(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndLast(LResult);
		}

		public CursorGetFlags Reset(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginReset(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndReset(LResult);
		}

		public void Insert(RemoteRow ARow, System.Collections.BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginInsert(CursorHandle, ACallInfo, ARow, AValueFlags, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndInsert(LResult);
		}

		public void Update(RemoteRow ARow, System.Collections.BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginUpdate(CursorHandle, ACallInfo, ARow, AValueFlags, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndUpdate(LResult);
		}

		public void Delete(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginDelete(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndDelete(LResult);
		}

		public Guid GetBookmark(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetBookmark(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetBookmark(LResult);
		}

		public RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGotoBookmark(CursorHandle, ACallInfo, ABookmark, AForward, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGotoBookmark(LResult);
		}

		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginCompareBookmarks(CursorHandle, ACallInfo, ABookmark1, ABookmark2, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndCompareBookmarks(LResult);
		}

		public void DisposeBookmark(Guid ABookmark, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginDisposeBookmark(CursorHandle, ACallInfo, ABookmark, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndDisposeBookmark(LResult);
		}

		public void DisposeBookmarks(Guid[] ABookmarks, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginDisposeBookmarks(CursorHandle, ACallInfo, ABookmarks, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndDisposeBookmarks(LResult);
		}

		public string Order
		{
			get 
			{ 
				IAsyncResult LResult = GetServiceInterface().BeginGetOrder(CursorHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetOrder(LResult);
			}
		}

		public RemoteRow GetKey(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetKey(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetKey(LResult);
		}

		public RemoteGotoData FindKey(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginFindKey(CursorHandle, ACallInfo, AKey, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndFindKey(LResult);
		}

		public CursorGetFlags FindNearest(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginFindNearest(CursorHandle, ACallInfo, AKey, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndFindNearest(LResult);
		}

		public RemoteGotoData Refresh(RemoteRow ARow, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginRefresh(CursorHandle, ACallInfo, ARow, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndRefresh(LResult);
		}

		public int RowCount(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetRowCount(CursorHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetRowCount(LResult);
		}

		#endregion

		#region IServerCursorBase Members

		public void Open()
		{
			// Nothing to do, the cursor is opened on creation
		}

		public void Close()
		{
			throw new NotImplementedException();
		}

		public bool Active
		{
			get { return true; }
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion

		#region IServerCursorBehavior Members

		public CursorCapability Capabilities
		{
			get { return FCursorDescriptor.Capabilities; }
		}

		public CursorType CursorType
		{
			get { return FCursorDescriptor.CursorType; }
		}

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}

		public CursorIsolation Isolation
		{
			get { return FCursorDescriptor.CursorIsolation; }
		}

		#endregion

		#region IRemoteProposable Members

		public RemoteProposeData Default(RemoteRowBody ARow, string AColumn, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginDefault(CursorHandle, ACallInfo, ARow, AColumn, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndDefault(LResult);
		}

		public RemoteProposeData Change(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginChange(CursorHandle, ACallInfo, AOldRow, ANewRow, AColumn, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndChange(LResult);
		}

		public RemoteProposeData Validate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginValidate(CursorHandle, ACallInfo, AOldRow, ANewRow, AColumn, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndValidate(LResult);
		}

		#endregion
	}
}
