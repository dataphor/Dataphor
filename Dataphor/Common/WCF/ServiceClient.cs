/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Alphora.Common.WCF
{
	/// <summary>
	/// Provides a base service client implementation that offers the caching of configured endpoints for use with programmatic bindings.
	/// </summary>
	/// <typeparam name="T">The type of the service interface.</typeparam>
	/// <remarks>
	/// <para>
	/// The default client base class provided with WCF (ClientBase[T]) provides a caching implementation
	/// of the ChannelFactory that offers significant improvement in terms of performance and stability
	/// over recreating a ChannelFactory for use with each client instance. However, this caching functionality
	/// only works if the configuration files are used to build up endpoints. If programmatic binding is used
	/// to create the ChannelFactory, the implementation does not use this caching mechanism.
	/// </para>
	/// <para>
	/// This service client implementation provides caching of the ChannelFactory for programmatic binding 
	/// configuration scenarios. In addition, this implementation automatically detects and attempts to 
	/// recover from channel faults.
	/// </para>
	/// <para>
	/// NOTE: Due to the difficulty of determining whether two binding instances describe the same binding,
	/// the caching used here is based on reference comparison of bindings. This means that in order for the
	/// caching to actually work, the same binding instance must be used per logical endpoint.
	/// </para>
	/// </remarks>
	public abstract class ServiceClient<T> : IDisposable
	{
		private static ChannelFactoryCache<T> _channelFactoryCache = new ChannelFactoryCache<T>();

		/// <summary>
		/// Initializes a new instance of the ServiceClient class.
		/// </summary>
		/// <param name="binding">The binding to use for the connection.</param>
		/// <param name="endpointAddress">The endpoint address to connect to.</param>
		public ServiceClient(Binding binding, EndpointAddress endpointAddress)
		{
			_channelFactoryWrapper = _channelFactoryCache.GetChannelFactory(binding, endpointAddress);
		}

		/// <summary>
		/// Initializes a new instance of the ServiceClient class using the given endpoint configuration name.
		/// </summary>
		/// <param name="endpointConfigurationName">The name of the endpoint configuration to be used to connect.</param>
		public ServiceClient(string endpointConfigurationName)
		{
			_channelFactoryWrapper = new ChannelFactoryWrapper<T>(endpointConfigurationName, new ChannelFactory<T>(endpointConfigurationName));
		}

		/// <summary>
		/// Defines an internal disposal method that will be called when the Dispose method is called.
		/// </summary>
		protected virtual void InternalDispose() { }
		
		/// <summary>
		/// Closes the service client and releases the underlying connection resources.
		/// </summary>
		public void Dispose()
		{
			try
			{
				InternalDispose();
			}
			finally
			{
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

		/// <summary>
		/// Returns the channel to be used to invoke service methods.
		/// </summary>
		/// <returns>The channel to be used to invoke service methods.</returns>
		protected internal T GetInterface()
		{
			if ((_channel == null) || !IsChannelValid(_channel))
				SetChannel(_channelFactoryWrapper.ChannelFactory.CreateChannel());
			return _channel;
		}
	}
}