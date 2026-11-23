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
		
		private int _transactionCount;
		/// <summary>How many transaction levels the local process has begun on this connection.</summary>
		/// <remarks>
		/// <para>
		/// Counted here rather than asked of the other server. Asking was a network round trip on every begin, and on a
		/// connection whose server has gone it is a question that cannot be answered at all - which is the one question the
		/// process has to answer to decide whether a dead connection is a participant in a transaction it still has open
		/// (see <see cref="ServerProcess.InternalBeginTransaction"/>).
		/// </para>
		/// <para>
		/// The count is exact because of this invariant: <b>every remote session in a process's
		/// <see cref="ServerProcess.RemoteSessions"/> list has begun exactly as many transaction levels as the process has
		/// open.</b> <c>ServerProcess.RemoteConnect</c> catches a new connection up to the process's depth before adding it
		/// to the list; <c>ServerProcess.InternalBeginTransaction</c> either begins a level on a connection, lets the
		/// connection go, or fails the whole begin; and the unwind decrements the count on every path, failures included.
		/// </para>
		/// </remarks>
		public int TransactionCount { get { return _transactionCount; } }
		
		public bool InTransaction
		{
			get { return TransactionCount > 0; }
		}
		
		/// <summary>Begins a transaction level on the connection; the level is counted only if the other server took it.</summary>
		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			try
			{
				InternalBeginTransaction(isolationLevel);
			}
			catch (Exception exception)
			{
				throw TransactionFailure(exception);
			}
			
			_transactionCount++;
		}
		
		public void PrepareTransaction()
		{
			try
			{
				InternalPrepareTransaction();
			}
			catch (Exception exception)
			{
				throw TransactionFailure(exception);
			}
		}
		
		/// <summary>Commits the innermost transaction level on the connection.</summary>
		/// <remarks>
		/// The count comes down whatever the other server did with the commit: the level is being abandoned locally either
		/// way, and a count left high would make the next begin treat a dead connection as a participant and so never let
		/// it go.
		/// </remarks>
		public void CommitTransaction()
		{
			try
			{
				InternalCommitTransaction();
			}
			catch (Exception exception)
			{
				throw TransactionFailure(exception);
			}
			finally
			{
				_transactionCount--;
			}
		}
		
		/// <summary>Rolls back the innermost transaction level on the connection.</summary>
		/// <remarks>The count comes down whatever the other server did, for the reason given on <see cref="CommitTransaction"/>.</remarks>
		public void RollbackTransaction()
		{
			try
			{
				InternalRollbackTransaction();
			}
			catch (Exception exception)
			{
				throw TransactionFailure(exception);
			}
			finally
			{
				_transactionCount--;
			}
		}
		
		/// <summary>The error a failed transaction operation on this connection reaches its caller as.</summary>
		/// <remarks>
		/// A bare communication exception says nothing about which of a process's connections broke, and a process can hold
		/// one per server link. This names the link, the instance and the host and carries what the network said as its
		/// inner exception. It is only how the failure is reported: the decision whether a dead connection may be let go is
		/// made on <see cref="TransactionCount"/>, never on the exception.
		/// </remarks>
		private ServerException TransactionFailure(Exception exception)
		{
			return new ServerException(ServerException.Codes.RemoteSessionTransactionFailed, exception, ServerLink.Name, ServerLink.InstanceName, ServerLink.HostName);
		}
		
		protected abstract void InternalBeginTransaction(IsolationLevel isolationLevel);
		
		protected abstract void InternalPrepareTransaction();
		
		protected abstract void InternalCommitTransaction();
		
		protected abstract void InternalRollbackTransaction();
		
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
