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
		public RemoteSession(ServerProcess process, Schema.ServerLink serverLink)
		{
			_serverProcess = process;
			_serverLink = serverLink;
		}
		
		protected override void Dispose(bool disposing)
		{
			_serverProcess = null;
			
			base.Dispose(disposing);
		}
		
		private ServerProcess _serverProcess;
		public ServerProcess ServerProcess { get { return _serverProcess; } }

		private Schema.ServerLink _serverLink;
		public Schema.ServerLink ServerLink { get { return _serverLink; } }
		
		public abstract int TransactionCount { get; }
		
		public bool InTransaction
		{
			get { return TransactionCount > 0; }
		}
		
		public abstract void BeginTransaction(IsolationLevel isolationLevel);
		
		public abstract void PrepareTransaction();
		
		public abstract void CommitTransaction();
		
		public abstract void RollbackTransaction();
		
		public abstract void Execute(string statement, DataParams paramsValue);
		
		public abstract IDataValue Evaluate(string expression, DataParams paramsValue);
		
		public abstract Schema.TableVar PrepareTableVar(Plan plan, string expression, DataParams paramsValue);
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
		public int IndexOf(Schema.ServerLink link)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].ServerLink.Equals(link))
					return index;
			return -1;
		}
		
		public bool Contains(Schema.ServerLink link)
		{
			return IndexOf(link) >= 0;
		}
	}
}
