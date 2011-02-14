/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store
	
	Defines the expected behavior for a simple storage device that uses a SQL DBMS as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in the target DBMS.
*/

//#define STORETIMING

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store
{
	public abstract class SQLStore : System.Object
	{
		private string _connectionString;
		public string ConnectionString
		{
			get { return _connectionString; }
			set 
			{ 
				if (_initialized)
					throw new StoreException(StoreException.Codes.StoreInitialized);
				_connectionString = value; 
			}
		}
		
		protected bool _supportsMARS;
		public bool SupportsMARS { get { return _supportsMARS; } }
		
		protected bool _supportsUpdatableCursor;
		public bool SupportsUpdatableCursor { get { return _supportsUpdatableCursor; } }
		
		private bool _initialized;
		public bool Initialized { get { return _initialized; } }

		protected abstract void InternalInitialize();
		
		public void Initialize()
		{
			if (_initialized)
				throw new StoreException(StoreException.Codes.StoreInitialized);
				
			InternalInitialize();
			
			_initialized = true;
		}
		
		public abstract SQLConnection GetSQLConnection();

		/// <summary> Returns the set of batches in the given script, delimited by the default 'go' batch terminator. </summary>
		public static List<String> ProcessBatches(string script)
		{
			return ProcessBatches(script, "go");
		}
		
		/// <summary>Returns the set of batches in the given script, delimited by the given terminator.</summary>
		public static List<String> ProcessBatches(string script, string terminator)
		{
			// NOTE: This is the same code as SQLUtility.ProcessBatches, duplicated to avoid the dependency
			List<String> batches = new List<String>();
			
			string[] lines = script.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
			StringBuilder batch = new StringBuilder();
			for (int index = 0; index < lines.Length; index++)
			{
				if (lines[index].IndexOf("go", StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					batches.Add(batch.ToString());
					batch = new StringBuilder();
				}
				else
				{
					batch.Append(lines[index]);
					batch.Append("\r\n");
				}
			}

			if (batch.Length > 0)
				batches.Add(batch.ToString());
				
			return batches;
		}
		
		private SQLStoreCounters _counters = new SQLStoreCounters();
		public SQLStoreCounters Counters { get { return _counters; } }
		
		protected abstract SQLStoreConnection InternalConnect();

		/// <summary>Default maximum number of connections to an SSCE server.</summary>
		/// <remarks>
		/// According to Microsoft, the ceiling for connections to an SSCE device is around 70:
		/// <para>
		/// In SSCE technically we support 256 connections. But do not scale that well when you cross 70 connections.  
		/// To get good performance with 70 concurrent connections, you need to increase the lock time out period in connection string.  
		/// Data Source = �./local.sdf�;Max Buffer Size = 10240;Default Lock Timeout = 5000;Flush Interval = 20; AutoShrink Threshold = 10
		///	
		/// You need to make sure that the connections, sessions are properly disposed in your application.  
		/// Dispose it explicitly and don�t depend on GC to Dispose it, since it may take longer time to dispose. 
		/// </para>
		/// Based on this, it may be worthwhile to investigate some of these other settings as well, however,
		/// with the connection pooling and name resolution cache we are implementing in the catalog device,
		/// we should never get even close to this kind of concurrent access.
		/// </remarks>
		public const int DefaultMaxConnections = 60;

		private int _maxConnections = DefaultMaxConnections;
		/// <summary>Maximum number of connections to allow to this store.</summary>
		/// <remarks>
		/// Set this value to 0 to allow unlimited connections.
		/// </remarks>
		public int MaxConnections { get { return _maxConnections; } set { _maxConnections = value; } }
		
		private int _connectionCount;
		
		/// <summary>Establishes a connection to the store.</summary>		
		public SQLStoreConnection Connect()
		{
			lock (this)
			{
				if ((_maxConnections > 0) && (_connectionCount >= _maxConnections))
					throw new StoreException(StoreException.Codes.MaximumConnectionsExceeded, ErrorSeverity.Environment);
				_connectionCount++;
			}
			
			try
			{
				#if SQLSTORETIMING
				long startTicks = TimingUtility.CurrentTicks;
				try
				{
				#endif

					return InternalConnect();

				#if SQLSTORETIMING
				}
				finally
				{
					Counters.Add(new SQLStoreCounter("Connect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(startTicks)));
				}
				#endif
			}
			catch
			{
				ReportDisconnect();
				throw;
			}
		}
		
		internal void ReportDisconnect()
		{
			lock (this)
			{
				_connectionCount--;
			}
		}
	}
}

