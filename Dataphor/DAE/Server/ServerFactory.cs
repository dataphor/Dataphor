/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

using Alphora.Dataphor.DAE;
using System.Configuration;

namespace Alphora.Dataphor.DAE.Server
{
	#if DEBUG
	public class LoggingClientSinkProvider : IClientChannelSinkProvider
	{
		public IClientChannelSink CreateSink(IChannelSender AChannel, string AURL, object ARemoteChannelData)
		{
			return new LoggingClientSink(FNext == null ? null : FNext.CreateSink(AChannel, AURL, ARemoteChannelData));
		}

		private IClientChannelSinkProvider FNext;
		public IClientChannelSinkProvider Next
		{
			get { return FNext; }
			set { FNext = value; }
		}
	}

	public class LoggingClientSink : BaseChannelSinkWithProperties, IClientChannelSink, IChannelSinkBase
	{
		public LoggingClientSink(IClientChannelSink ANextSink)
		{
			FNextChannelSink = ANextSink;
		}
		
		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, System.Runtime.Remoting.Messaging.IMessage msg, ITransportHeaders headers, System.IO.Stream stream)
		{
			if (FNextChannelSink != null)
				FNextChannelSink.AsyncProcessRequest(sinkStack, msg, headers, stream);
		}

		public void ProcessMessage(System.Runtime.Remoting.Messaging.IMessage msg, ITransportHeaders requestHeaders, System.IO.Stream requestStream, out ITransportHeaders responseHeaders, out System.IO.Stream responseStream)
		{
			if (FNextChannelSink != null)
				FNextChannelSink.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);
			else
			{
				responseHeaders = null;
				responseStream = null;
			}
		}

		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, System.IO.Stream stream)
		{
			if (FNextChannelSink != null)
				FNextChannelSink.AsyncProcessResponse(sinkStack, state, headers, stream);
		}

		public System.IO.Stream GetRequestStream(System.Runtime.Remoting.Messaging.IMessage msg, ITransportHeaders headers)
		{
			if (FNextChannelSink != null)
				return FNextChannelSink.GetRequestStream(msg, headers);
			return null;
		}

		private IClientChannelSink FNextChannelSink;
		public IClientChannelSink NextChannelSink { get { return FNextChannelSink; } }
	}
	#endif

    public class ServerFactory
    {
		public static int CClientPort = 0;

		private static TcpChannel FClientChannel;
		public static TcpChannel ClientChannel
		{
			get { return FClientChannel; }
		}
		
		private static string GetListenerURI(string AHostName)
		{
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("listener");
			int LPort = LSettings == null || LSettings["port"] == null ? Listener.CDefaultListenerPort : (int)LSettings["port"];
			return String.Format("tcp://{0}:{1}/DataphorListener", AHostName, LPort);
		}
		
		private static IListener GetListener(string AHostName)
		{
			EnsureClientChannel();
			IListener LListener = (IListener)Activator.GetObject(typeof(IListener), GetListenerURI(AHostName));
			if (LListener == null)
				throw new ServerException(ServerException.Codes.UnableToConnectToListener, AHostName);
			return LListener;
		}
		
		public static string[] EnumerateInstances(string AHostName)
		{
			return GetListener(AHostName).EnumerateInstances();
		}
		
		public static string GetInstanceURI(string AHostName, string AInstanceName)
		{
			return GetListener(AHostName).GetInstanceURI(AInstanceName);
		}
		
		public static IServer Connect(string AServerURI, string AClientHostName)
		{
			return Connect(AServerURI, false, AClientHostName);
		}
		
		/// <summary> Connect to a remote server by it's URI. </summary>
		/// <returns> An IServer interface representing a server instance. </returns>
        public static IServer Connect(string AServerURI, bool AClientSideLoggingEnabled, string AClientHostName)
        {
			EnsureClientChannel();
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
        
		/// <summary> Ensures that a channel is registered for the client. </summary>
		private static void EnsureClientChannel()
		{
			if (FClientChannel == null)
			{
				FClientChannel = ChannelServices.GetChannel("tcp") as TcpChannel;
				
				if (FClientChannel == null)
				{
					BinaryServerFormatterSinkProvider LProvider = new BinaryServerFormatterSinkProvider();
					BinaryClientFormatterSinkProvider LClientProvider = new BinaryClientFormatterSinkProvider();
					
					#if DEBUG
					LClientProvider.Next = new LoggingClientSinkProvider();
					#endif

					LProvider.TypeFilterLevel = TypeFilterLevel.Full;
					
					// Disabled client side port: We no longer want to listen on the client side; this 
					//  methodology does not work in general.  Due to firewalls and routing, the server 
					//  may not be able to establish a connection with the client... all communications 
					//  should occur through the channel opened up by the client to the server.

//					IDictionary LProperties = new System.Collections.Specialized.ListDictionary();
//					LProperties["port"] = CClientPort;
					FClientChannel = new TcpChannel(null /* LProperties */, LClientProvider, LProvider);
					try
					{
						ChannelServices.RegisterChannel(FClientChannel, false);
					}
					catch
					{
						FClientChannel = null;
						throw;
					}
				}
			}
		}
	}
}