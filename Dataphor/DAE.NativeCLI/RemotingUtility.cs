/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Configuration;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public static class RemotingUtility
	{
		public const int CDefaultListenerPort = 8060;

		private static TcpChannel FClientChannel;
		public static TcpChannel ClientChannel
		{
			get { return FClientChannel; }
		}
		
		/// <summary>
		/// Ensures that a channel is registered for the client. 
		/// </summary>
		public static void EnsureClientChannel()
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

					//IDictionary LProperties = new System.Collections.Specialized.ListDictionary();
					//LProperties["port"] = CClientPort;
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

		public static string BuildListenerURI(string AHostName)
		{
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("listener");
			int LPort = LSettings == null || LSettings["port"] == null ? CDefaultListenerPort : (int)LSettings["port"];
			return String.Format("tcp://{0}:{1}/DataphorListener", AHostName, LPort);
		}
		
		public static string BuildInstanceURI(string AHostName, int APortNumber, string AInstanceName)
		{
			return BuildInstanceURI(AHostName, APortNumber, AInstanceName, false);
		}
		
		public static string BuildInstanceURI(string AHostName, int APortNumber, string AInstanceName, bool AUseNative)
		{
			return String.Format("tcp://{0}:{1}/{2}{3}", AHostName, APortNumber, AInstanceName, AUseNative ? "Native" : "");
		}
	}
}
