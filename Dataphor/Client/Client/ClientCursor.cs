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
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginSelectSpecific(CursorHandle, ACallInfo, AHeader, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndSelectSpecific(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteRowBody Select(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginSelect(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndSelect(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteFetchData Fetch(RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginFetchSpecific(CursorHandle, ACallInfo, AHeader, ACount, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				FetchResult LFetchResult = GetServiceInterface().EndFetchSpecific(LResult);
				ABookmarks = LFetchResult.Bookmarks;
				return LFetchResult.FetchData;
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginFetch(CursorHandle, ACallInfo, ACount, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				FetchResult LFetchResult = GetServiceInterface().EndFetch(LResult);
				ABookmarks = LFetchResult.Bookmarks;
				return LFetchResult.FetchData;
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public CursorGetFlags GetFlags(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetFlags(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetFlags(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteMoveData MoveBy(int ADelta, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginMoveBy(CursorHandle, ACallInfo, ADelta, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndMoveBy(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public CursorGetFlags First(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginFirst(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndFirst(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public CursorGetFlags Last(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginLast(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndLast(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public CursorGetFlags Reset(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginReset(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndReset(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Insert(RemoteRow ARow, System.Collections.BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginInsert(CursorHandle, ACallInfo, ARow, AValueFlags, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndInsert(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Update(RemoteRow ARow, System.Collections.BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginUpdate(CursorHandle, ACallInfo, ARow, AValueFlags, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUpdate(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Delete(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginDelete(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDelete(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public Guid GetBookmark(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetBookmark(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetBookmark(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGotoBookmark(CursorHandle, ACallInfo, ABookmark, AForward, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGotoBookmark(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginCompareBookmarks(CursorHandle, ACallInfo, ABookmark1, ABookmark2, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndCompareBookmarks(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void DisposeBookmark(Guid ABookmark, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginDisposeBookmark(CursorHandle, ACallInfo, ABookmark, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDisposeBookmark(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void DisposeBookmarks(Guid[] ABookmarks, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginDisposeBookmarks(CursorHandle, ACallInfo, ABookmarks, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDisposeBookmarks(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public string Order
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetOrder(CursorHandle, null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetOrder(LResult);
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		public RemoteRow GetKey(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetKey(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetKey(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteGotoData FindKey(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginFindKey(CursorHandle, ACallInfo, AKey, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndFindKey(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public CursorGetFlags FindNearest(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginFindNearest(CursorHandle, ACallInfo, AKey, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndFindNearest(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteGotoData Refresh(RemoteRow ARow, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginRefresh(CursorHandle, ACallInfo, ARow, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndRefresh(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public int RowCount(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetRowCount(CursorHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetRowCount(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
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
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginDefault(CursorHandle, ACallInfo, ARow, AColumn, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndDefault(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteProposeData Change(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginChange(CursorHandle, ACallInfo, AOldRow, ANewRow, AColumn, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndChange(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public RemoteProposeData Validate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginValidate(CursorHandle, ACallInfo, AOldRow, ANewRow, AColumn, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndValidate(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		#endregion
	}
}
