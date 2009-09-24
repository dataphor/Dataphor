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
	public class ClientPlan : IRemoteServerPlan
	{
		#region IRemoteServerPlan Members

		public IRemoteServerProcess Process
		{
			get { throw new NotImplementedException(); }
		}

		public Exception[] Messages
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IServerPlanBase Members

		public Guid ID
		{
			get { throw new NotImplementedException(); }
		}

		public void CheckCompiled()
		{
			throw new NotImplementedException();
		}

		public PlanStatistics PlanStatistics
		{
			get { throw new NotImplementedException(); }
		}

		public ProgramStatistics ProgramStatistics
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}

	public class ClientStatementPlan : ClientPlan, IRemoteServerStatementPlan
	{
		#region IRemoteServerStatementPlan Members

		public void Execute(ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out TimeSpan AExecuteTime, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	public class ClientExpressionPlan : ClientPlan, IRemoteServerExpressionPlan
	{
		#region IRemoteServerExpressionPlan Members

		public byte[] Evaluate(ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out TimeSpan AExecuteTime, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor Open(ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out TimeSpan AExecuteTime, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor Open(ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, out TimeSpan AExecuteTime, out Guid[] ABookmarks, int ACount, out Alphora.Dataphor.DAE.Contracts.RemoteFetchData AFetchData, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public void Close(IRemoteServerCursor ACursor, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

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
	}
}
