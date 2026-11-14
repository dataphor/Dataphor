/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Data.Common;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	/// <summary> Dataphor DAE Transaction class. </summary>
	/// <remarks>
	///	Isolation levels are translated to the appropriate DAE transaction isolation level.  
	/// See the Dataphor DAE Developer's Guide for a discussion of this mapping.
	/// </remarks>
	public class DAETransaction : DbTransaction, IDbTransaction
	{
		internal DAETransaction(DAEConnection connection, System.Data.IsolationLevel isolationLevel)
		{
			_connection = connection;
			_isolationLevel = isolationLevel;
			IsolationLevel localIsolationLevel;
			switch (_isolationLevel)
			{
				case System.Data.IsolationLevel.ReadUncommitted : localIsolationLevel = DAE.IsolationLevel.Browse; break;
				case System.Data.IsolationLevel.ReadCommitted : localIsolationLevel = DAE.IsolationLevel.CursorStability; break;
				case System.Data.IsolationLevel.RepeatableRead :
				case System.Data.IsolationLevel.Serializable : localIsolationLevel = DAE.IsolationLevel.Isolated; break;
				default : localIsolationLevel = _connection.ServerProcess.ProcessInfo.DefaultIsolationLevel; break;
			}
			_connection.ServerProcess.BeginTransaction(localIsolationLevel);
		}

		// IDisposable

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!IsComplete && _connection.InTransaction)
					Rollback();
			}
			finally
			{
				_connection = null;
				base.Dispose(disposing);
			}
		}

		~DAETransaction()
		{
			Dispose(false);
		}

		public bool IsComplete { get { return _connection == null; } }
		protected void CompleteTransaction()
		{
			_connection = null;
		}

		private DAEConnection _connection;
		protected override DbConnection DbConnection
		{
			get { return _connection; }
		}

		private System.Data.IsolationLevel _isolationLevel;
		/// <summary> Determines the isolation level for the transaction. </summary>
		public override System.Data.IsolationLevel IsolationLevel
		{
			get { return _isolationLevel; }
		}

		public event EventHandler OnCommit;

		public override void Commit()
		{
			if ((_connection == null) || !_connection.InTransaction)
				throw new ProviderException(ProviderException.Codes.ConnectionLost, "Commit");
			try
			{
				_connection.ServerProcess.CommitTransaction();
				CompleteTransaction(); //transaction complete.
				if (OnCommit != null)
					OnCommit(this, EventArgs.Empty);
			}
			catch
			{
				if ((Connection != null) && !_connection.InTransaction)
					CompleteTransaction();
				throw;
			}
		}

		public event EventHandler OnRollBack;

		public override void Rollback()
		{
			if ((_connection == null) || !_connection.InTransaction)
				throw new ProviderException(ProviderException.Codes.ConnectionLost, "RollBack");
			try
			{
				_connection.ServerProcess.RollbackTransaction();
				CompleteTransaction(); //transaction complete.
				if (OnRollBack != null)
					OnRollBack(this, EventArgs.Empty);
			}
			catch
			{
				if ((Connection != null) && !_connection.InTransaction)
					CompleteTransaction();
				throw;
			}
		}
	}
}
