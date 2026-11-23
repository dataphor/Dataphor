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
	public abstract class ServiceClient<T> : IDisposable where T : class
	{
		private static ChannelFactoryCache<T> _channelFactoryCache = new ChannelFactoryCache<T>();

		/// <summary>
		/// Initializes a new instance of the ServiceClient class.
		/// </summary>
		/// <param name="binding">The binding to use for the connection.</param>
		/// <param name="endpointAddress">The endpoint address to connect to.</param>
		public ServiceClient(Binding binding, EndpointAddress endpointAddress)
		{
			ChannelFactoryWrapper<T> channelFactoryWrapper = _channelFactoryCache.GetChannelFactory(binding, endpointAddress);
			_channelFactoryWrapper = channelFactoryWrapper;
			// The holder creates channels on the local rather than on the field, which Dispose nulls: a Dispose racing a
			// call cannot leave the holder reading a field that has been cleared. Disposing the holder first stops it
			// creating any further channel on a wrapper that is about to be released.
			_channelHolder = new ChannelHolder<T>(() => channelFactoryWrapper.ChannelFactory.CreateChannel());
		}

		/// <summary>
		/// Not supported: always throws <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="endpointConfigurationName">The name of a WCF endpoint configuration in an application configuration file.</param>
		/// <remarks>
		/// The .NET WCF client has no ChannelFactory constructor that reads an endpoint from an application
		/// configuration file. Kept so that callers still compile; connect with a binding and endpoint address instead.
		/// </remarks>
		public ServiceClient(string endpointConfigurationName)
		{
			throw new NotSupportedException
			(
				String.Format
				(
					"Endpoint configuration names (WCF endpoints read from an application configuration file) are not supported on this version of Dataphor (endpoint configuration name \"{0}\"). Connect with a binding and endpoint address instead.",
					endpointConfigurationName
				)
			);
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
					if (_channelHolder != null)
					{
						_channelHolder.Dispose();
						_channelHolder = null;
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
		private ChannelHolder<T> _channelHolder;

		/// <summary>
		/// Returns the channel to be used to invoke service methods.
		/// </summary>
		/// <returns>The channel to be used to invoke service methods.</returns>
		/// <remarks>
		/// One guarded decision, in ChannelHolder: the channel is reused from the moment it is created, a channel being
		/// replaced is aborted rather than dropped, and the result is never null.
		/// </remarks>
		protected internal T GetInterface()
		{
			return _channelHolder.GetChannel();
		}
	}
}