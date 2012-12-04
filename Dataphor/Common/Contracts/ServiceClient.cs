/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.WCF;

namespace Alphora.Dataphor.DAE.Contracts
{
	public abstract class ServiceClient<T> : IDisposable
	{
		private static ChannelFactoryCache<T> _channelFactoryCache = new ChannelFactoryCache<T>();

		public ServiceClient(Binding binding, EndpointAddress endpointAddress)
		{
			_channelFactoryWrapper = _channelFactoryCache.GetChannelFactory(binding, endpointAddress);
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
			try
			{
				InternalDispose();
			}
			finally
			{
				// NOTE: Some sort of issue occurs when running with this service client
				// such that when the CloseChannelFactory call is made, the channel is
				// already in the faulted state. Despite concerted effort, I was unable
				// to reproduce the issue under debug, and whatever exception is occurring
				// to put the channel into the faulted state 1) Occurs only on the client,
				// and 2) Does not result in any exception on the utilizing thread.
				// Ignoring the exception seems the only rational thing to do at this point.

				try
				{
					if (_channel != null)
					{
						CloseChannel();
						SetChannel(default(T));
					}
				}
				catch (Exception e)
				{
					// TODO: Log exception
				}

				try
				{			
					if (_channelFactoryWrapper != null)
					{
						_channelFactoryWrapper.Dispose();
						_channelFactoryWrapper = null;
					}
				}
				catch (Exception e)
				{
					// TODO: Log exception
				}
			}
		}

		private ChannelFactoryWrapper<T> _channelFactoryWrapper;
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
		
		protected T GetInterface()
		{
			if ((_channel == null) || !IsChannelValid(_channel))
				SetChannel(_channelFactoryWrapper.ChannelFactory.CreateChannel());
			return _channel;
		}
	}
}
