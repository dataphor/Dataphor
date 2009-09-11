/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteSession    
    public abstract class RemoteSession : Disposable
    {
		public RemoteSession(ServerProcess AProcess, Schema.ServerLink AServerLink)
		{
			FServerProcess = AProcess;
			FServerLink = AServerLink;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FServerProcess = null;
			
			base.Dispose(ADisposing);
		}
		
		private ServerProcess FServerProcess;
		public ServerProcess ServerProcess { get { return FServerProcess; } }

		private Schema.ServerLink FServerLink;
		public Schema.ServerLink ServerLink { get { return FServerLink; } }
		
		public abstract int TransactionCount { get; }
		
		public bool InTransaction
		{
			get { return TransactionCount > 0; }
		}
		
		public abstract void BeginTransaction(IsolationLevel AIsolationLevel);
		
		public abstract void PrepareTransaction();
		
		public abstract void CommitTransaction();
		
		public abstract void RollbackTransaction();
		
		public abstract void Execute(string AStatement, DataParams AParams);
		
		public abstract DataValue Evaluate(string AExpression, DataParams AParams);
		
		public abstract Schema.TableVar PrepareTableVar(Plan APlan, string AExpression, DataParams AParams);
    }

	#if USETYPEDLIST    
	public class RemoteSessions : DisposableTypedList
	{
		public RemoteSessions() : base()
		{
			FItemType = typeof(RemoteSession);
			FItemsOwned = true;
		}
		
		public new RemoteSession this[int AIndex]
		{
			get { return (RemoteSession)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class RemoteSessions : DisposableList<RemoteSession>
	{
	#endif
		public int IndexOf(Schema.ServerLink ALink)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].ServerLink.Equals(ALink))
					return LIndex;
			return -1;
		}
		
		public bool Contains(Schema.ServerLink ALink)
		{
			return IndexOf(ALink) >= 0;
		}
	}
}
