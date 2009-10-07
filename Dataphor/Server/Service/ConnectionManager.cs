/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Server;
using System.Threading;

namespace Alphora.Dataphor.DAE.Service
{
	public class ConnectionManager : IDisposable
	{
		public const int CIdleTimeInSeconds = 300; // 5 minutes
		
		public ConnectionManager(RemoteServer ARemoteServer)
		{
			FRemoteServer = ARemoteServer;
			StartLifetimeWatcher();
		}
		

		#region IDisposable Members

		public void Dispose()
		{
			StopLifetimeWatcher();
		}

		#endregion

		private RemoteServer FRemoteServer;
		
		private ManualResetEvent FLifetimeSignal;
		private object FLifetimeSyncHandle = new object();
		
		private void StartLifetimeWatcher()
		{
			lock (FLifetimeSyncHandle)
			{
				if (FLifetimeSignal != null)
					Error.Fail("Lifetime manager started more than once.");
				FLifetimeSignal = new ManualResetEvent(false);
			}
			new Thread(new ThreadStart(LifetimeWatcher)).Start();
		}
		
		private void LifetimeWatcher()
		{
			try
			{
				bool LSignaled = false;
				while (!LSignaled)
				{
					// Wait for either a signal or a time-out
					LSignaled = FLifetimeSignal.WaitOne(CIdleTimeInSeconds * 1000);
					
					if (!LSignaled)
					{
						DateTime LOldestActivityTime = DateTime.Now.AddSeconds(-CIdleTimeInSeconds);
						
						RemoteServerConnection[] LConnections = FRemoteServer.GetCurrentConnections();
						for (int LIndex = 0; LIndex < LConnections.Length; LIndex++)
							if (LConnections[LIndex].LastActivityTime < LOldestActivityTime)
							{
								try
								{
									// Connection has been idle for longer than the activity time, kill it
									FRemoteServer.Relinquish(LConnections[LIndex]);
								}
								catch (Exception LException)
								{
									FRemoteServer.Server.LogError(LException);
								}
							}
						}
				}

				// The keep alive processing is complete.  Clean up...
				lock (FLifetimeSyncHandle)
				{
					FLifetimeSignal.Close();
					FLifetimeSignal = null;
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled, the framework will terminate the server.
			}
		}
		
		private void StopLifetimeWatcher()
		{
			lock (FLifetimeSyncHandle)
			{
				if (FLifetimeSignal != null)
					FLifetimeSignal.Set();
			}
		}
	}
}
