//#define SQLSTORETIMING

/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
	Abstract SQL Store
	
	Defines the expected behavior for a simple storage device that uses a SQL DBMS as it's backend.
	The store is capable of storing integers, strings, booleans, and long text and binary data.
	The store also manages logging and rollback of nested transactions to make up for the lack of savepoint support in the target DBMS.
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store
{
	public abstract class SQLStore : System.Object
	{
		private string FConnectionString;
		public string ConnectionString
		{
			get { return FConnectionString; }
			set 
			{ 
				if (FInitialized)
					throw new StoreException(StoreException.Codes.StoreInitialized);
				FConnectionString = value; 
			}
		}
		
		protected bool FSupportsMARS;
		public bool SupportsMARS { get { return FSupportsMARS; } }
		
		protected bool FSupportsUpdatableCursor;
		public bool SupportsUpdatableCursor { get { return FSupportsUpdatableCursor; } }
		
		private bool FInitialized;
		public bool Initialized { get { return FInitialized; } }

		protected abstract void InternalInitialize();
		
		public void Initialize()
		{
			if (FInitialized)
				throw new StoreException(StoreException.Codes.StoreInitialized);
				
			InternalInitialize();
			
			FInitialized = true;
		}
		
		public abstract SQLConnection GetSQLConnection();

		/// <summary> Returns the set of batches in the given script, delimited by the default 'go' batch terminator. </summary>
		public static List<String> ProcessBatches(string AScript)
		{
			return ProcessBatches(AScript, "go");
		}
		
		/// <summary>Returns the set of batches in the given script, delimited by the given terminator.</summary>
		public static List<String> ProcessBatches(string AScript, string ATerminator)
		{
			// NOTE: This is the same code as SQLUtility.ProcessBatches, duplicated to avoid the dependency
			List<String> LBatches = new List<String>();
			
			string[] LLines = AScript.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
			StringBuilder LBatch = new StringBuilder();
			for (int LIndex = 0; LIndex < LLines.Length; LIndex++)
			{
				if (LLines[LIndex].IndexOf("go", StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					LBatches.Add(LBatch.ToString());
					LBatch = new StringBuilder();
				}
				else
				{
					LBatch.Append(LLines[LIndex]);
					LBatch.Append("\r\n");
				}
			}

			if (LBatch.Length > 0)
				LBatches.Add(LBatch.ToString());
				
			return LBatches;
		}
		
		private SQLStoreCounters FCounters = new SQLStoreCounters();
		public SQLStoreCounters Counters { get { return FCounters; } }
		
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
		public const int CDefaultMaxConnections = 60;

		private int FMaxConnections = CDefaultMaxConnections;
		/// <summary>Maximum number of connections to allow to this store.</summary>
		/// <remarks>
		/// Set this value to 0 to allow unlimited connections.
		/// </remarks>
		public int MaxConnections { get { return FMaxConnections; } set { FMaxConnections = value; } }
		
		private int FConnectionCount;
		
		/// <summary>Establishes a connection to the store.</summary>		
		public SQLStoreConnection Connect()
		{
			lock (this)
			{
				if ((FMaxConnections > 0) && (FConnectionCount >= FMaxConnections))
					throw new StoreException(StoreException.Codes.MaximumConnectionsExceeded, ErrorSeverity.Environment);
				FConnectionCount++;
			}
			
			try
			{
				#if SQLSTORETIMING
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
				#endif

					return InternalConnect();

				#if SQLSTORETIMING
				}
				finally
				{
					Counters.Add(new SQLStoreCounter("Connect", "", "", false, false, false, TimingUtility.TimeSpanFromTicks(LStartTicks)));
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
				FConnectionCount--;
			}
		}
	}
}

