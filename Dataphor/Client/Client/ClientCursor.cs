/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientCursor : IRemoteServerCursor
	{
		#region IRemoteServerCursor Members

		public IRemoteServerExpressionPlan Plan
		{
			get { throw new NotImplementedException(); }
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteRowBody Select(Alphora.Dataphor.DAE.Contracts.RemoteRowHeader AHeader, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteRowBody Select(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteFetchData Fetch(Alphora.Dataphor.DAE.Contracts.RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Server.CursorGetFlags GetFlags(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteMoveData MoveBy(int ADelta, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Server.CursorGetFlags First(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Server.CursorGetFlags Last(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Server.CursorGetFlags Reset(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void Insert(Alphora.Dataphor.DAE.Contracts.RemoteRow ARow, System.Collections.BitArray AValueFlags, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void Update(Alphora.Dataphor.DAE.Contracts.RemoteRow ARow, System.Collections.BitArray AValueFlags, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void Delete(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Guid GetBookmark(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void DisposeBookmark(Guid ABookmark, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void DisposeBookmarks(Guid[] ABookmarks, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public string Order
		{
			get { throw new NotImplementedException(); }
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteRow GetKey(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteGotoData FindKey(Alphora.Dataphor.DAE.Contracts.RemoteRow AKey, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Server.CursorGetFlags FindNearest(Alphora.Dataphor.DAE.Contracts.RemoteRow AKey, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteGotoData Refresh(Alphora.Dataphor.DAE.Contracts.RemoteRow ARow, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public int RowCount(Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IServerCursorBase Members

		public void Open()
		{
			throw new NotImplementedException();
		}

		public void Close()
		{
			throw new NotImplementedException();
		}

		public bool Active
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion

		#region IServerCursorBehavior Members

		public CursorCapability Capabilities
		{
			get { throw new NotImplementedException(); }
		}

		public CursorType CursorType
		{
			get { throw new NotImplementedException(); }
		}

		public bool Supports(CursorCapability ACapability)
		{
			throw new NotImplementedException();
		}

		public CursorIsolation Isolation
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IRemoteProposable Members

		public Alphora.Dataphor.DAE.Contracts.RemoteProposeData Default(Alphora.Dataphor.DAE.Contracts.RemoteRowBody ARow, string AColumn, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteProposeData Change(Alphora.Dataphor.DAE.Contracts.RemoteRowBody AOldRow, Alphora.Dataphor.DAE.Contracts.RemoteRowBody ANewRow, string AColumn, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Alphora.Dataphor.DAE.Contracts.RemoteProposeData Validate(Alphora.Dataphor.DAE.Contracts.RemoteRowBody AOldRow, Alphora.Dataphor.DAE.Contracts.RemoteRowBody ANewRow, string AColumn, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
