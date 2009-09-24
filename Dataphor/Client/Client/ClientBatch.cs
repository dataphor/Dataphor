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
	public class ClientBatch : IRemoteServerBatch
	{
		#region IRemoteServerBatch Members

		public IRemoteServerScript ServerScript
		{
			get { throw new NotImplementedException(); }
		}

		public IRemoteServerPlan Prepare(Alphora.Dataphor.DAE.Contracts.RemoteParam[] AParams)
		{
			throw new NotImplementedException();
		}

		public void Unprepare(IRemoteServerPlan APlan)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerExpressionPlan PrepareExpression(Alphora.Dataphor.DAE.Contracts.RemoteParam[] AParams, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor)
		{
			throw new NotImplementedException();
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerStatementPlan PrepareStatement(Alphora.Dataphor.DAE.Contracts.RemoteParam[] AParams, out Alphora.Dataphor.DAE.Contracts.PlanDescriptor APlanDescriptor)
		{
			throw new NotImplementedException();
		}

		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			throw new NotImplementedException();
		}

		public void Execute(ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IServerBatchBase Members

		public bool IsExpression()
		{
			throw new NotImplementedException();
		}

		public string GetText()
		{
			throw new NotImplementedException();
		}

		public int Line
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}
}
