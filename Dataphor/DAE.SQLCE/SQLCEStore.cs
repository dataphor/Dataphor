/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Simple SQL CE Store
	
	A simple storage device that uses a SQL Server Everywhere instance as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in SQL Server Everywhere.
*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store.SQLCE
{
	public class SQLCEStore : SQLStore
	{
		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
		protected override void InternalInitialize()
		{
			DbConnectionStringBuilder LBuilder = new DbConnectionStringBuilder();
			LBuilder.ConnectionString = ConnectionString;
			if (LBuilder.ContainsKey("Data Source"))
			{
				string LDatabaseFileName = (string)LBuilder["Data Source"];
				if (!File.Exists(LDatabaseFileName))
				{
					SqlCeEngine LEngine = new SqlCeEngine(ConnectionString);
					LEngine.CreateDatabase();
				}
			}
		}

		public override SQLConnection GetSQLConnection()
		{
			return new SQLCEConnection(ConnectionString);
		}

		protected override SQLStoreConnection InternalConnect()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return new SQLCEStoreConnection(this);
			#if SQLSTORETIMING
			}
			finally
			{
				Counters.Add(new SQLStoreCounter("Connect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
	}
	
	public class SQLCEStoreConnection : SQLStoreConnection
	{
		public SQLCEStoreConnection(SQLCEStore AStore) : base(AStore)
		{ }
		
		protected override DbConnection InternalCreateConnection()
		{
			return new SqlCeConnection(Store.ConnectionString);
		}

		public override bool HasTable(string ATableName)
		{
			return ((int)this.ExecuteScalar(String.Format("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{0}'", ATableName)) != 0);
		}

		internal SqlCeResultSet ExecuteResultSet(string ATableName, string AIndexName, DbRangeOptions ARangeOptions, object[] AStartValues, object[] AEndValues, ResultSetOptions AResultSetOptions)
		{
			ExecuteCommand.CommandType = CommandType.TableDirect;
			ExecuteCommand.CommandText = ATableName;
			ExecuteCommand.IndexName = AIndexName;
			ExecuteCommand.SetRange(ARangeOptions, AStartValues, AEndValues);

			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
			
				return ExecuteCommand.ExecuteResultSet(AResultSetOptions);
			
			#if SQLSTORETIMING
			}
			finally
			{
				Store.Counters.Add(new SQLStoreCounter("ExecuteResultSet", ATableName, AIndexName, AStartValues != null && AEndValues == null, AStartValues != null && AEndValues != null, (ResultSetOptions.Updatable & AResultSetOptions) != 0, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		public new SqlCeCommand ExecuteCommand { get { return (SqlCeCommand)base.ExecuteCommand; } }

		protected override SQLStoreCursor InternalOpenCursor(string ATableName, string AIndexName, List<string> AKey, bool AIsUpdatable)
		{
			return
				new SQLCEStoreCursor
				(
					this,
					ATableName,
					AIndexName,
					AKey,
					AIsUpdatable
				);
		}
	}
	
	public class SQLCEStoreCursor : SQLStoreCursor
	{
		public SQLCEStoreCursor(SQLCEStoreConnection AConnection, string ATableName, string AIndexName, List<string> AKey, bool AIsUpdatable) 
			: base(AConnection, ATableName, AIndexName, AKey, AIsUpdatable)
		{ }

		protected override System.Data.Common.DbDataReader InternalCreateReader()
		{
			FResultSet =
				Connection.ExecuteResultSet
				(
					TableName,
					IndexName,
					DbRangeOptions.Default,
					null,
					null,
					ResultSetOptions.Scrollable | ResultSetOptions.Sensitive | (IsUpdatable ? ResultSetOptions.Updatable : ResultSetOptions.None)
				);
				
			return FResultSet;
		}

		protected override void InternalDispose()
		{
			// This is the same reference as FReader in the base, so no need to dispose it, just clear it.
			FResultSet = null;
			
			base.InternalDispose();
		}
		
		private SqlCeResultSet FResultSet;
		
		public new SQLCEStoreConnection Connection { get { return (SQLCEStoreConnection)base.Connection; } }

		protected override void InternalLast()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			FResultSet.ReadLast();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected override bool InternalPrior()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return FResultSet.ReadPrevious();
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}

		protected override void InternalFirst()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.ReadFirst();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected override bool InternalSeek(object[] AKey)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				object[] LKey = new object[AKey.Length];
				for (int LIndex = 0; LIndex < LKey.Length; LIndex++)
					LKey[LIndex] = NativeToStoreValue(AKey[LIndex]);
				return FResultSet.Seek(DbSeekOptions.FirstEqual, LKey);
			#if SQLSTORETIMING
			}
			finally
			{
				Connection.Store.Counters.Add(new SQLStoreCounter("Seek", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		protected override void InternalSetValue(int AIndex, object AValue)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.SetValue(AIndex, NativeToStoreValue(AValue));

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
		
		protected override void InternalInsert(object[] ARow)
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			SqlCeUpdatableRecord LRecord = FResultSet.CreateRecord();
			for (int LIndex = 0; LIndex < ARow.Length; LIndex++)
				LRecord.SetValue(LIndex, NativeToStoreValue(ARow[LIndex]));

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.Insert(LRecord, DbInsertOptions.KeepCurrentPosition);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected override void InternalUpdate()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.Update();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected override void InternalDelete()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.Delete();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
	}
}
