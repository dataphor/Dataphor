/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;

using Alphora.Dataphor.DAE.Contracts;

namespace Client.Server
{
	public static class ClientFactory
	{
		private static ChannelFactory<IClientDataphorService> FChannelFactory;
		
		private static ChannelFactory<IClientDataphorService> ChannelFactory
		{
			get
			{
				if (FChannelFactory == null)
					FChannelFactory = new ChannelFactory<IClientDataphorService>();
				return FChannelFactory;
			}
		}
		
		private static IClientDataphorService FChannel;
		
		private static bool IsChannelFaulted(IClientDataphorService AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Faulted;
		}
		
		private static bool IsChannelValid(IClientDataphorService AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Opened;
		}
		
		private static void SetChannel(IClientDataphorService AChannel)
		{
			if (FChannel != null)
				((ICommunicationObject)FChannel).Faulted -= new EventHandler(ChannelFaulted);
			FChannel = AChannel;
			if (FChannel != null)
				((ICommunicationObject)FChannel).Faulted += new EventHandler(ChannelFaulted);
		}
		
		private static void ChannelFaulted(object ASender, EventArgs AArgs)
		{
			((ICommunicationObject)ASender).Faulted -= new EventHandler(ChannelFaulted);
			if (FChannel == ASender)
				FChannel = null;
		}
		
		public static IClientDataphorService GetChannel()
		{
			if ((FChannel == null) || !IsChannelValid(FChannel))
				SetChannel(ChannelFactory.CreateChannel());
			return FChannel;
		}
	}
}
