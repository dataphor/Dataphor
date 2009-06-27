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
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerHost : Disposable
	{
		public ServerHost(Server AServer, int APortNumber)
		{
			// Establish a listener to route instance port discovery traffic
			Listener.EstablishListener();

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
					AServer.Name,
					typeof(Server)
				);
				FServer = AServer;

				if (!NativeServerCLI.HasNativeServer())
				{
					FNativeServer = new NativeServer(AServer);
					NativeServerCLI.SetNativeServer(FNativeServer);
					RemotingConfiguration.RegisterWellKnownServiceType
					(
						new WellKnownServiceTypeEntry
						(
							typeof(NativeServerCLI), 
							String.Format("{0}Native", AServer.Name), 
							WellKnownObjectMode.SingleCall
						)
					);
				}
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
		
		private NativeServer FNativeServer;
		public NativeServer NativeServer
		{
			get { return FNativeServer; }
		}
		
		// statics

		public static int FChannelNameGenerator = 0;
		public static string GetNextChannelName()
		{

			return "Channel" + System.Threading.Interlocked.Increment(ref FChannelNameGenerator).ToString();
		}
	}
}
