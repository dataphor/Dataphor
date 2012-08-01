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
		public ServiceClient(Binding binding, EndpointAddress endpointAddress)
		{
			_channelFactory = new ChannelFactory<T>(binding, endpointAddress);
		}
		
		public ServiceClient(string endpointURI) 
			: this
			(
				DataphorServiceUtility.GetBinding(), 
				new EndpointAddress(endpointURI)
			)
		{
		}
		
		protected virtual void InternalDispose() { }
		
		public void Dispose()
		{
			InternalDispose();
			
			if (_channel != null)
			{
				CloseChannel();
				SetChannel(default(T));
			}
			
			if (_channelFactory != null)
			{
				CloseChannelFactory();
				_channelFactory = null;
			}
		}

		private ChannelFactory<T> _channelFactory;
		private T _channel;

		private bool IsChannelFaulted(T channel)
		{
			return ((ICommunicationObject)channel).State == CommunicationState.Faulted;
		}
		
		private bool IsChannelValid(T channel)
		{
			return ((ICommunicationObject)channel).State == CommunicationState.Opened;
		}
		
		private void SetChannel(T channel)
		{
			if (_channel != null)
				((ICommunicationObject)_channel).Faulted -= new EventHandler(ChannelFaulted);
			_channel = channel;
			if (_channel != null)
				((ICommunicationObject)_channel).Faulted += new EventHandler(ChannelFaulted);
		}
		
		private void ChannelFaulted(object sender, EventArgs args)
		{
			((ICommunicationObject)sender).Faulted -= new EventHandler(ChannelFaulted);
			if (Object.ReferenceEquals(_channel, sender))
				_channel = default(T);
		}
		
		private void CloseChannel()
		{
			ICommunicationObject channel = _channel as ICommunicationObject;
			if (channel != null)
				if (channel.State == CommunicationState.Opened)
					channel.Close();
				else
					channel.Abort();
		}
		
		private void CloseChannelFactory()
		{
			try
			{
				if (_channelFactory.State == CommunicationState.Opened)
					_channelFactory.Close();
				else
					_channelFactory.Abort();
			}
			catch
			{
				// Don't let a shut-down error eat a more important error
				// TODO: should we at least log this?
			}
		}
		
		protected T GetInterface()
		{
			if ((_channel == null) || !IsChannelValid(_channel))
				SetChannel(_channelFactory.CreateChannel());
			return _channel;
		}
	}
}
