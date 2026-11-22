/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The shipped client stack, assembled the way src/client/DAE.Client/ServerConnection.cs assembles it for a
	/// connection alias: a ClientServer on the binding DataphorServiceUtility.GetBinding builds, wrapped in a
	/// LocalServer. No channel is hand-rolled and no hook on DataphorServiceUtility is touched.
	/// </summary>
	/// <remarks>
	/// Here rather than nested in CoreWcfProtocolTest, which is where it was and whose own size note asked for the move:
	/// the out-of-process fixtures need the same plumbing, and a second copy of it would be the next thing to drift.
	/// </remarks>
	internal sealed class ProtocolClient : IDisposable
	{
		/// <summary>A client pinged as often as the shipped client is (see <see cref="LocalServer.DefaultKeepAliveInterval"/>).</summary>
		public ProtocolClient(string hostName, string instanceName, int portNumber) : this(hostName, instanceName, portNumber, LocalServer.DefaultKeepAliveInterval) { }

		/// <param name="keepAliveIntervalSeconds">How often this client pings the server, which a case that cannot wait two minutes for a ping sets for itself.</param>
		public ProtocolClient(string hostName, string instanceName, int portNumber, int keepAliveIntervalSeconds)
		{
			_clientServer = new ClientServer(hostName, instanceName, portNumber, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default);
			try
			{
				// The third argument is the client's own host name, which the server records against the connection; it is
				// not a connection target.
				_localServer = new LocalServer(_clientServer, false, "localhost", keepAliveIntervalSeconds);
			}
			catch
			{
				_clientServer.Close();
				throw;
			}
		}

		private readonly ClientServer _clientServer;
		private LocalServer _localServer;

		/// <summary>The client-side IServer: every call on it is a call on the wire.</summary>
		public IServer Server => _localServer;

		/// <summary>The handle the service minted for this connection, which is per service instance.</summary>
		public int ConnectionHandle => ((ClientConnection)_localServer.ServerConnection).ConnectionHandle;

		/// <summary>Runs <paramref name="body"/> on a session and a process over the wire, cleaning both up on either path.</summary>
		/// <remarks>The engine's built-in administrator, with the empty password every fixture in this suite connects as.</remarks>
		public void OnProcess(Action<IServerProcess> body)
		{
			IServerSession session = Server.Connect(new SessionInfo("Admin", ""));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					body(process);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				Server.Disconnect(session);
			}
		}

		public void Dispose()
		{
			try
			{
				if (_localServer != null)
				{
					_localServer.Dispose();
					_localServer = null;
				}
			}
			finally
			{
				_clientServer.Close();
			}
		}
	}
}
