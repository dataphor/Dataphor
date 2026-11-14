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
		public const int IdleTimeInSeconds = 300; // 5 minutes
		
		public ConnectionManager(RemoteServer remoteServer)
		{
			_remoteServer = remoteServer;
			StartLifetimeWatcher();
		}
		

		#region IDisposable Members

		public void Dispose()
		{
			StopLifetimeWatcher();
		}

		#endregion

		private RemoteServer _remoteServer;
		
		private ManualResetEvent _lifetimeSignal;
		private object _lifetimeSyncHandle = new object();
		
		private void StartLifetimeWatcher()
		{
			lock (_lifetimeSyncHandle)
			{
				if (_lifetimeSignal != null)
					Error.Fail("Lifetime manager started more than once.");
				_lifetimeSignal = new ManualResetEvent(false);
			}
			new Thread(new ThreadStart(LifetimeWatcher)).Start();
		}
		
		private void LifetimeWatcher()
		{
			try
			{
				bool signaled = false;
				while (!signaled)
				{
					// Wait for either a signal or a time-out
					signaled = _lifetimeSignal.WaitOne(IdleTimeInSeconds * 1000);
					
					if (!signaled)
					{
						DateTime oldestActivityTime = DateTime.Now.AddSeconds(-IdleTimeInSeconds);
						
						RemoteServerConnection[] connections = _remoteServer.GetCurrentConnections();
						for (int index = 0; index < connections.Length; index++)
							if (connections[index].LastActivityTime < oldestActivityTime)
							{
								try
								{
									// Connection has been idle for longer than the activity time, kill it
                                    _remoteServer.Server.LogMessage(String.Format("Connection relinquished due to inactivity: ConnectionName={0}, HostName={1}, LastActivity={2}", connections[index].ConnectionName, connections[index].HostName, connections[index].LastActivityTime.ToShortTimeString()));
									_remoteServer.Relinquish(connections[index]);                                   
								}
								catch (Exception exception)
								{
									_remoteServer.Server.LogError(exception);
								}
							}
					}
				}

				// The keep alive processing is complete.  Clean up...
				lock (_lifetimeSyncHandle)
				{
					_lifetimeSignal.Close();
					_lifetimeSignal = null;
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled, the framework will terminate the server.
			}
		}
		
		private void StopLifetimeWatcher()
		{
			lock (_lifetimeSyncHandle)
			{
				if (_lifetimeSignal != null)
					_lifetimeSignal.Set();
			}
		}
	}
}
