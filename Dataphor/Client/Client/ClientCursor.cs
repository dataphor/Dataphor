/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientCursor : ClientObject, IRemoteServerCursor
	{
		public ClientCursor(ClientExpressionPlan clientExpressionPlan, CursorDescriptor cursorDescriptor)
		{
			_clientExpressionPlan = clientExpressionPlan;
			_cursorDescriptor = cursorDescriptor;
		}
		
		private ClientExpressionPlan _clientExpressionPlan;
		public ClientExpressionPlan ClientExpressionPlan { get { return _clientExpressionPlan; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientExpressionPlan.ClientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}
		
		private CursorDescriptor _cursorDescriptor;
		
		public int CursorHandle { get { return _cursorDescriptor.Handle; } }
		
		#region IRemoteServerCursor Members

		public IRemoteServerExpressionPlan Plan
		{
			get { return _clientExpressionPlan; }
		}

		public RemoteRowBody Select(RemoteRowHeader header, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginSelectSpecific(CursorHandle, callInfo, header, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndSelectSpecific(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteRowBody Select(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginSelect(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndSelect(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteFetchData Fetch(RemoteRowHeader header, out Guid[] bookmarks, int count, bool skipCurrent, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginFetchSpecific(CursorHandle, callInfo, header, count, skipCurrent, null, null);
				result.AsyncWaitHandle.WaitOne();
				FetchResult fetchResult = GetServiceInterface().EndFetchSpecific(result);
				bookmarks = fetchResult.Bookmarks;
				return fetchResult.FetchData;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteFetchData Fetch(out Guid[] bookmarks, int count, bool skipCurrent, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginFetch(CursorHandle, callInfo, count, skipCurrent, null, null);
				result.AsyncWaitHandle.WaitOne();
				FetchResult fetchResult = GetServiceInterface().EndFetch(result);
				bookmarks = fetchResult.Bookmarks;
				return fetchResult.FetchData;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public CursorGetFlags GetFlags(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetFlags(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetFlags(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteMoveData MoveBy(int delta, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginMoveBy(CursorHandle, callInfo, delta, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndMoveBy(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public CursorGetFlags First(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginFirst(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndFirst(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public CursorGetFlags Last(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginLast(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndLast(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public CursorGetFlags Reset(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginReset(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndReset(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Insert(RemoteRow row, System.Collections.BitArray valueFlags, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginInsert(CursorHandle, callInfo, row, valueFlags, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndInsert(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Update(RemoteRow row, System.Collections.BitArray valueFlags, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginUpdate(CursorHandle, callInfo, row, valueFlags, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUpdate(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Delete(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginDelete(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDelete(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public Guid GetBookmark(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetBookmark(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetBookmark(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteGotoData GotoBookmark(Guid bookmark, bool forward, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGotoBookmark(CursorHandle, callInfo, bookmark, forward, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGotoBookmark(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public int CompareBookmarks(Guid bookmark1, Guid bookmark2, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginCompareBookmarks(CursorHandle, callInfo, bookmark1, bookmark2, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndCompareBookmarks(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void DisposeBookmark(Guid bookmark, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginDisposeBookmark(CursorHandle, callInfo, bookmark, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDisposeBookmark(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void DisposeBookmarks(Guid[] bookmarks, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginDisposeBookmarks(CursorHandle, callInfo, bookmarks, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDisposeBookmarks(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public string Order
		{
			get 
			{ 
				try
				{
					IAsyncResult result = GetServiceInterface().BeginGetOrder(CursorHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetOrder(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		public RemoteRow GetKey(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetKey(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetKey(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteGotoData FindKey(RemoteRow key, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginFindKey(CursorHandle, callInfo, key, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndFindKey(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public CursorGetFlags FindNearest(RemoteRow key, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginFindNearest(CursorHandle, callInfo, key, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndFindNearest(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteGotoData Refresh(RemoteRow row, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginRefresh(CursorHandle, callInfo, row, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndRefresh(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public int RowCount(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetRowCount(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetRowCount(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		#endregion

		#region IServerCursorBase Members

		public void Open()
		{
			// Nothing to do, the cursor is opened on creation
		}

		public void Close()
		{
			ClientExpressionPlan.Close(this, ProcessCallInfo.Empty);
		}

		public bool Active
		{
			get { return true; }
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region IServerCursorBehavior Members

		public CursorCapability Capabilities
		{
			get { return _cursorDescriptor.Capabilities; }
		}

		public CursorType CursorType
		{
			get { return _cursorDescriptor.CursorType; }
		}

		public bool Supports(CursorCapability capability)
		{
			return (Capabilities & capability) != 0;
		}

		public CursorIsolation Isolation
		{
			get { return _cursorDescriptor.CursorIsolation; }
		}

		#endregion

		#region IRemoteProposable Members

		public RemoteProposeData Default(RemoteRowBody row, string column, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginDefault(CursorHandle, callInfo, row, column, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndDefault(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteProposeData Change(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginChange(CursorHandle, callInfo, oldRow, newRow, column, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndChange(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public RemoteProposeData Validate(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginValidate(CursorHandle, callInfo, oldRow, newRow, column, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndValidate(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		#endregion
	}
}
