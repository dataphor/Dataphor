/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using Microsoft.Extensions.Hosting;

using CoreWCF.Configuration;

using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>
	/// Hosts the listener service, which answers "where is instance X" for every instance on this machine.
	/// </summary>
	/// <remarks>
	/// It has a host of its own rather than sharing the instance's, because one listener per machine serves every
	/// instance on it (which is why its port is fixed at 8060): a server whose listener port is already held by another
	/// instance's process must still start and still serve its own endpoint, and on one host a failed listener would
	/// fail the whole host. So the constructor swallows the error from starting it, as it always has, and reports it
	/// through <see cref="IsStarted"/> for the caller to log.
	/// </remarks>
	public class ListenerServiceHost : IDisposable
	{
		/// <param name="hostName">The host name of both the listening address and the endpoint address; see DataphorServiceHost.HostName.</param>
		/// <param name="overridePortNumber">0 for the default listener port, 8060.</param>
		public ListenerServiceHost(string hostName, int overridePortNumber)
		{
			int listenerPort = overridePortNumber == 0 ? DataphorServiceUtility.DefaultListenerPortNumber : overridePortNumber;

			// Built outside the try on purpose: the only way building this host fails is a DataphorServiceHostHooks
			// binding hook that throws or returns nothing, and reporting that as "the listener port is already taken"
			// would bury a misconfiguration behind the one failure this class is written to tolerate.
			_listenerHost = BuildHost(hostName, listenerPort);
			try
			{
				_listenerHost.Start();
				_isStarted = true;
			}
			catch (Exception exception)
			{
				// Normally means a listener is already running in another process, which is the supported arrangement.
				_startFailureMessage = exception.Message;
			}
		}

		private static IHost BuildHost(string hostName, int listenerPort)
		{
			CoreWCF.Channels.Binding binding = DataphorServiceHostHooks.BuildBinding();

			return
				NetTcpServiceHost.Build
				(
					hostName,
					listenerPort,
					null,
					builder =>
					{
						// ListenerService is registered by type and not as an instance, which leaves CoreWCF to construct it
						// per session exactly as "new ServiceHost(typeof(ListenerService))" did.
						builder.AddService<ListenerService>();
						builder.AddServiceEndpoint<ListenerService, IListenerService>(binding, new Uri(DataphorServiceUtility.BuildListenerURI(hostName, listenerPort)));
						// NOTE: this runs while the host is starting, which on this class is inside the try above, so a
						// builder hook that throws here is swallowed as a listener that did not start. Harmless as things
						// stand - the instance's host is built and started first, so a hook that throws fails the whole
						// server start before this host is reached - but a hook that throws only for the listener's host
						// would lose the listener quietly. If one ever does, narrow that catch to the bind failure it is
						// there for rather than moving this call.
						DataphorServiceHostHooks.ServiceBuilderHook(builder);
					}
				);
		}

		private bool _isStarted;
		/// <summary>Whether the listener is actually serving; false when its port was already taken.</summary>
		public bool IsStarted { get { return _isStarted; } }

		private string _startFailureMessage;
		/// <summary>What starting the listener reported when it did not start; null when it did.</summary>
		public string StartFailureMessage { get { return _startFailureMessage; } }

		#region IDisposable Members

		/// <summary>Stops and disposes the host; safe after a failed start, and safe to call twice.</summary>
		public void Dispose()
		{
			IHost listenerHost = _listenerHost;
			_listenerHost = null;
			if (listenerHost != null)
				NetTcpServiceHost.Stop(listenerHost);
		}

		#endregion

		private IHost _listenerHost;
	}
}
