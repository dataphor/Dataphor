/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection
{
	public abstract class DotNetConnection : SQLConnection
	{
		/// <summary>
		/// Parameterless constructor used by descendents to establish state prior to establishing the connection.
		/// </summary>
		protected DotNetConnection() : base()
		{ }
		
		public DotNetConnection(string AConnectionString) : base()
		{
			InternalConnect(AConnectionString);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FConnection != null)
				{
					try
					{
						FConnection.Dispose();
					}
					finally
					{
						SetState(SQLConnectionState.Closed);
						FConnection = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		protected void InternalConnect(string AConnectionString)
		{
			FConnection = CreateDbConnection(AConnectionString);
			try
			{
				FConnection.Open();
			}
			catch (Exception LException)
			{
				WrapException(LException, "connect", false);
			}
			SetState(SQLConnectionState.Idle);
		}
		
		protected abstract IDbConnection CreateDbConnection(string AConnectionString);
		
		protected IDbCommand CreateDbCommand()
		{
			IDbCommand LCommand = FConnection.CreateCommand();
			if (FTransaction != null)
				LCommand.Transaction = FTransaction;
			return LCommand;
		}
		
		protected IDbConnection FConnection;
		protected IDbTransaction FTransaction;
		protected System.Data.IsolationLevel FIsolationLevel;
		
		protected override void InternalBeginTransaction(SQLIsolationLevel AIsolationLevel)
		{
			FIsolationLevel = System.Data.IsolationLevel.Unspecified;
			switch (AIsolationLevel)
			{
				case SQLIsolationLevel.ReadUncommitted : FIsolationLevel = System.Data.IsolationLevel.ReadUncommitted; break;
				case SQLIsolationLevel.ReadCommitted : FIsolationLevel = System.Data.IsolationLevel.ReadCommitted; break;
				case SQLIsolationLevel.RepeatableRead : FIsolationLevel = System.Data.IsolationLevel.RepeatableRead; break;
				case SQLIsolationLevel.Serializable : FIsolationLevel = System.Data.IsolationLevel.Serializable; break;
			}
			FTransaction = FConnection.BeginTransaction(FIsolationLevel);
		}

		protected override void InternalCommitTransaction()
		{
			FTransaction.Commit();
			FTransaction = null;
		}

		protected override void InternalRollbackTransaction()
		{
			try
			{
				FTransaction.Rollback();
			}
			finally
			{
				FTransaction = null;			
			}
		}
		
		public override bool IsConnectionValid()
		{
			try
			{
				return (FConnection != null) && (FConnection.State != ConnectionState.Closed);
			}
			catch 
			{
				return false;
			}
		}
	}
}

