/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Alphora.Dataphor.DAE.Contracts
{
	public abstract class ServiceClient<T> : IDisposable
	{
		public ServiceClient(Binding ABinding, EndpointAddress AEndpointAddress)
		{
			FChannelFactory = new ChannelFactory<T>(ABinding, AEndpointAddress);
		}
		
		public ServiceClient(string AEndpointURI) 
			: this
			(
				DataphorServiceUtility.GetBinding(), 
				new EndpointAddress(AEndpointURI)
			)
		{
		}
		
		protected virtual void InternalDispose() { }
		
		public void Dispose()
		{
			InternalDispose();
			
			if (FChannel != null)
			{
				CloseChannel();
				SetChannel(default(T));
			}
			
			if (FChannelFactory != null)
			{
				CloseChannelFactory();
				FChannelFactory = null;
			}
		}

		private ChannelFactory<T> FChannelFactory;
		private T FChannel;

		private bool IsChannelFaulted(T AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Faulted;
		}
		
		private bool IsChannelValid(T AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Opened;
		}
		
		private void SetChannel(T AChannel)
		{
			if (FChannel != null)
				((ICommunicationObject)FChannel).Faulted -= new EventHandler(ChannelFaulted);
			FChannel = AChannel;
			if (FChannel != null)
				((ICommunicationObject)FChannel).Faulted += new EventHandler(ChannelFaulted);
		}
		
		private void ChannelFaulted(object ASender, EventArgs AArgs)
		{
			((ICommunicationObject)ASender).Faulted -= new EventHandler(ChannelFaulted);
			if (Object.ReferenceEquals(FChannel, ASender))
				FChannel = default(T);
		}
		
		private void CloseChannel()
		{
			ICommunicationObject LChannel = FChannel as ICommunicationObject;
			if (LChannel != null)
				if (LChannel.State == CommunicationState.Opened)
					LChannel.Close();
				else
					LChannel.Abort();
		}
		
		private void CloseChannelFactory()
		{
			if (FChannelFactory.State == CommunicationState.Opened)
				FChannelFactory.Close();
			else
				FChannelFactory.Abort();
		}
		
		protected T GetInterface()
		{
			if ((FChannel == null) || !IsChannelValid(FChannel))
				SetChannel(FChannelFactory.CreateChannel());
			return FChannel;
		}
	}
}
