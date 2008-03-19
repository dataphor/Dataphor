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
		internal DAETransaction(DAEConnection AConnection, System.Data.IsolationLevel AIsolationLevel)
		{
			FConnection = AConnection;
			FIsolationLevel = AIsolationLevel;
			IsolationLevel LIsolationLevel;
			switch (FIsolationLevel)
			{
				case System.Data.IsolationLevel.ReadUncommitted : LIsolationLevel = DAE.IsolationLevel.Browse; break;
				case System.Data.IsolationLevel.ReadCommitted : LIsolationLevel = DAE.IsolationLevel.CursorStability; break;
				case System.Data.IsolationLevel.RepeatableRead :
				case System.Data.IsolationLevel.Serializable : LIsolationLevel = DAE.IsolationLevel.Isolated; break;
				default : LIsolationLevel = FConnection.ServerProcess.ProcessInfo.DefaultIsolationLevel; break;
			}
			FConnection.ServerProcess.BeginTransaction(LIsolationLevel);
		}

		// IDisposable

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (!IsComplete && FConnection.InTransaction)
					Rollback();
			}
			finally
			{
				FConnection = null;
				base.Dispose(ADisposing);
			}
		}

		~DAETransaction()
		{
			Dispose(false);
		}

		public bool IsComplete { get { return FConnection == null; } }
		protected void CompleteTransaction()
		{
			FConnection = null;
		}

		private DAEConnection FConnection;
		protected override DbConnection DbConnection
		{
			get { return FConnection; }
		}

		private System.Data.IsolationLevel FIsolationLevel;
		/// <summary> Determines the isolation level for the transaction. </summary>
		public override System.Data.IsolationLevel IsolationLevel
		{
			get { return FIsolationLevel; }
		}

		public event EventHandler OnCommit;

		public override void Commit()
		{
			if ((FConnection == null) || !FConnection.InTransaction)
				throw new ProviderException(ProviderException.Codes.ConnectionLost, "Commit");
			try
			{
				FConnection.ServerProcess.CommitTransaction();
				CompleteTransaction(); //transaction complete.
				if (OnCommit != null)
					OnCommit(this, EventArgs.Empty);
			}
			catch
			{
				if ((Connection != null) && !FConnection.InTransaction)
					CompleteTransaction();
				throw;
			}
		}

		public event EventHandler OnRollBack;

		public override void Rollback()
		{
			if ((FConnection == null) || !FConnection.InTransaction)
				throw new ProviderException(ProviderException.Codes.ConnectionLost, "RollBack");
			try
			{
				FConnection.ServerProcess.RollbackTransaction();
				CompleteTransaction(); //transaction complete.
				if (OnRollBack != null)
					OnRollBack(this, EventArgs.Empty);
			}
			catch
			{
				if ((Connection != null) && !FConnection.InTransaction)
					CompleteTransaction();
				throw;
			}
		}
	}
}
