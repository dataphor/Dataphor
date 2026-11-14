/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

/*
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
    public static class ServerFactory
    {
		public static IServer Connect(string AServerURI, string AClientHostName)
		{
			return Connect(AServerURI, false, AClientHostName);
		}
		
		/// <summary> Connect to a remote server by it's URI. </summary>
		/// <returns> An IServer interface representing a server instance. </returns>
        public static IServer Connect(string AServerURI, bool AClientSideLoggingEnabled, string AClientHostName)
        {
			RemotingUtility.EnsureClientChannel();
			IServer LServer = (IServer)Activator.GetObject(typeof(IRemoteServer), AServerURI);
			if (LServer == null)
				throw new ServerException(ServerException.Codes.UnableToConnectToServer, AServerURI);
			if (!RemotingServices.IsTransparentProxy(LServer))
				return LServer;
			else
				return new LocalServer((IRemoteServer)LServer, AClientSideLoggingEnabled, AClientHostName);
        }

        /// <summary> Dereferences a server object </summary>
        public static void Disconnect(IServer AServer)
        {
			LocalServer LServer = AServer as LocalServer;
			if (LServer != null)
				LServer.RemoveReference();
        }
	}
}
*/
