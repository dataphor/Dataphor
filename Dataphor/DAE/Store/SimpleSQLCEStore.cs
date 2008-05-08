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
using System.Data.SqlServerCe;
using System.Data.Common;
using Alphora.Dataphor.DAE.Connection;
using System.Data;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class SimpleSQLCEStore : SimpleSQLStore
	{
		public override string GetConnectionString()
		{
			return String.Format("Data Source={0};Password={1};Mode={2}", DatabaseFileName, Password, "Read Write");
		}
		
		public override SQLConnection GetSQLConnection()
		{
			return new SQLCEConnection(GetConnectionString());
		}

		protected override SimpleSQLStoreConnection InternalConnect()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			try
			{
			#endif
				return new SimpleSQLCEStoreConnection(this);
			#if SQLSTORETIMING
			}
			finally
			{
				Counters.Add(new SimpleSQLStoreCounter("Connect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}

		/// <summary>Initializes the store, ensuring that an instance of the server is running and a database is attached.</summary>
		public override void Initialize()
		{
			if (!File.Exists(DatabaseFileName))
			{
				SqlCeEngine LEngine = new SqlCeEngine(GetConnectionString());
				LEngine.CreateDatabase();
			}
		}
	}
	
	public class SimpleSQLCEStoreConnection : SimpleSQLStoreConnection
	{
		public SimpleSQLCEStoreConnection(SimpleSQLCEStore AStore) : base(AStore)
		{ }
		
		protected override DbConnection InternalCreateConnection()
		{
			return new SqlCeConnection(Store.GetConnectionString());
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
				Store.Counters.Add(new SimpleSQLStoreCounter("ExecuteResultSet", ATableName, AIndexName, AStartValues != null && AEndValues == null, AStartValues != null && AEndValues != null, (ResultSetOptions.Updatable & AResultSetOptions) != 0, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			}
			#endif
		}
		
		public new SqlCeCommand ExecuteCommand { get { return (SqlCeCommand)base.ExecuteCommand; } }

		protected override SimpleSQLStoreCursor InternalOpenCursor(string ATableName, string AIndexName, List<string> AKey, bool AIsUpdatable)
		{
			return
				new SimpleSQLCEStoreCursor
				(
					this,
					ATableName,
					AIndexName,
					AKey,
					AIsUpdatable
				);
		}
	}
	
	public class SimpleSQLCEStoreCursor : SimpleSQLStoreCursor
	{
		public SimpleSQLCEStoreCursor(SimpleSQLCEStoreConnection AConnection, string ATableName, string AIndexName, List<string> AKey, bool AIsUpdatable) 
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
		
		public new SimpleSQLCEStoreConnection Connection { get { return (SimpleSQLCEStoreConnection)base.Connection; } }

		protected override void InternalLast()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif
			
			FResultSet.ReadLast();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Last", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Prior", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("First", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Seek", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("SetValue", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("CreateRecord", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.Insert(LRecord, DbInsertOptions.KeepCurrentPosition);

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Insert", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected override void InternalUpdate()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.Update();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Update", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}

		protected override void InternalDelete()
		{
			#if SQLSTORETIMING
			long LStartTicks = TimingUtility.CurrentTicks;
			#endif

			FResultSet.Delete();

			#if SQLSTORETIMING
			Connection.Store.Counters.Add(new SimpleSQLStoreCounter("Delete", FTableName, "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
			#endif
		}
	}
}
