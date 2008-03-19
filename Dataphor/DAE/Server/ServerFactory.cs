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

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerHost : Disposable
	{
		public ServerHost(Server AServer, int APortNumber)
		{
			BinaryServerFormatterSinkProvider LProvider = new BinaryServerFormatterSinkProvider();
			LProvider.TypeFilterLevel = TypeFilterLevel.Full;
			if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)	// Must check, will throw if we attempt to set it again (regardless)
				RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			IDictionary LProperties = new System.Collections.Specialized.ListDictionary();
			LProperties["port"] = APortNumber;
			string LChannelName = GetNextChannelName();
			LProperties["name"] = LChannelName;
			FChannel = new TcpChannel(LProperties, null, LProvider);
			ChannelServices.RegisterChannel(FChannel, false);
			try
			{
				RemotingServices.Marshal
				(
					AServer, 
					"Dataphor",
					typeof(Server)
				);
				FServer = AServer;
			}
			catch
			{
				ChannelServices.UnregisterChannel(FChannel);
				FChannel = null;
				throw;
			}
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if (FServer != null)
					{
						RemotingServices.Disconnect(FServer);
						FServer = null;
					}
				}
				finally
				{
					if (FChannel != null)
					{
						ChannelServices.UnregisterChannel(FChannel);
						FChannel = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		private TcpChannel FChannel;
		public TcpChannel Channel
		{
			get { return FChannel; }
		}


		private Server FServer;
		public Server Server
		{
			get { return FServer; }
		}

		// statics

		public static int FChannelNameGenerator = 0;
		public static string GetNextChannelName()
		{

			return "Channel" + System.Threading.Interlocked.Increment(ref FChannelNameGenerator).ToString();
		}
	}

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
		
		public static IServer Connect(string AServerURI, string AHostName)
		{
			return Connect(AServerURI, false, AHostName);
		}

		/// <summary> Connect to a remote server by it's URI. </summary>
		/// <returns> An IServer interface representing a server instance. </returns>
        public static IServer Connect(string AServerURI, bool AClientSideLoggingEnabled, string AHostName)
        {
			EnsureClientChannel();
			IServer LServer = (IServer)Activator.GetObject(typeof(IRemoteServer), AServerURI);
			if (LServer == null)
				throw new ServerException(ServerException.Codes.UnableToConnectToServer, AServerURI);
			if (!RemotingServices.IsTransparentProxy(LServer))
				return LServer;
			else
				return new LocalServer((IRemoteServer)LServer, AClientSideLoggingEnabled, AHostName);
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