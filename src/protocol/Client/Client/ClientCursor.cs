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

		private void ReportCommunicationError()
		{
			_clientExpressionPlan.ClientProcess.ClientSession.ClientConnection.ClientServer.ReportCommunicationError();
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
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginSelectSpecific(CursorHandle, callInfo, header, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndSelectSpecific(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteRowBody Select(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginSelect(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndSelect(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteFetchData Fetch(RemoteRowHeader header, out Guid[] bookmarks, int count, bool skipCurrent, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginFetchSpecific(CursorHandle, callInfo, header, count, skipCurrent, null, null);
				result.AsyncWaitHandle.WaitOne();
				FetchResult fetchResult = channel.EndFetchSpecific(result);
				bookmarks = fetchResult.Bookmarks;
				return fetchResult.FetchData;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteFetchData Fetch(out Guid[] bookmarks, int count, bool skipCurrent, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginFetch(CursorHandle, callInfo, count, skipCurrent, null, null);
				result.AsyncWaitHandle.WaitOne();
				FetchResult fetchResult = channel.EndFetch(result);
				bookmarks = fetchResult.Bookmarks;
				return fetchResult.FetchData;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public CursorGetFlags GetFlags(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetFlags(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetFlags(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteMoveData MoveBy(int delta, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginMoveBy(CursorHandle, callInfo, delta, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndMoveBy(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public CursorGetFlags First(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginFirst(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndFirst(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public CursorGetFlags Last(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginLast(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndLast(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public CursorGetFlags Reset(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginReset(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndReset(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Insert(RemoteRow row, System.Collections.BitArray valueFlags, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginInsert(CursorHandle, callInfo, row, valueFlags, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndInsert(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Update(RemoteRow row, System.Collections.BitArray valueFlags, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginUpdate(CursorHandle, callInfo, row, valueFlags, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndUpdate(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Delete(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginDelete(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndDelete(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public Guid GetBookmark(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetBookmark(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetBookmark(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteGotoData GotoBookmark(Guid bookmark, bool forward, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGotoBookmark(CursorHandle, callInfo, bookmark, forward, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGotoBookmark(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public int CompareBookmarks(Guid bookmark1, Guid bookmark2, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginCompareBookmarks(CursorHandle, callInfo, bookmark1, bookmark2, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndCompareBookmarks(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void DisposeBookmark(Guid bookmark, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginDisposeBookmark(CursorHandle, callInfo, bookmark, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndDisposeBookmark(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void DisposeBookmarks(Guid[] bookmarks, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginDisposeBookmarks(CursorHandle, callInfo, bookmarks, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndDisposeBookmarks(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public string Order
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetOrder(CursorHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetOrder(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		public RemoteRow GetKey(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetKey(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetKey(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteGotoData FindKey(RemoteRow key, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginFindKey(CursorHandle, callInfo, key, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndFindKey(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public CursorGetFlags FindNearest(RemoteRow key, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginFindNearest(CursorHandle, callInfo, key, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndFindNearest(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteGotoData Refresh(RemoteRow row, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginRefresh(CursorHandle, callInfo, row, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndRefresh(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public int RowCount(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetRowCount(CursorHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetRowCount(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginDefault(CursorHandle, callInfo, row, column, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndDefault(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteProposeData Change(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginChange(CursorHandle, callInfo, oldRow, newRow, column, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndChange(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public RemoteProposeData Validate(RemoteRowBody oldRow, RemoteRowBody newRow, string column, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginValidate(CursorHandle, callInfo, oldRow, newRow, column, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndValidate(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		#endregion
	}
}
