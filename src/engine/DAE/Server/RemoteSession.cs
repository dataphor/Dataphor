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
		/// <summary>The index of this process's session to the given server link, or -1 if it has none.</summary>
		/// <remarks>
		/// Matched on the link's object id rather than with Schema.Object.Equals, which compares names: a link created
		/// after a drop, under the name the dropped link used, would otherwise match the dropped link's session, and the
		/// new link's host name, instance name, port, link users and session settings would be ignored for as long as
		/// the process lived. Object ids are unique within one engine's catalog, and every link in this list came from
		/// the catalog of the engine that owns this process. Every engine has had its own generator since
		/// bug-object-id-counter-is-process-wide, so a second engine in the same process can no longer reach
		/// these ids at all.
		/// </remarks>
		public int IndexOf(Schema.ServerLink link)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].ServerLink.ID == link.ID)
					return index;
			return -1;
		}
		
		public bool Contains(Schema.ServerLink link)
		{
			return IndexOf(link) >= 0;
		}
	}
}
