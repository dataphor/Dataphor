/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Alphora.Common.WCF
{
	/// <summary>
	/// Provides a disposable wrapper for a channel factory and its associated endpoint descriptor.
	/// </summary>
	/// <typeparam name="TChannel">The type of the service interface.</typeparam>
	internal class ChannelFactoryWrapper<TChannel> : IDisposable
	{
		public ChannelFactoryWrapper(string endpointConfigurationName, ChannelFactory<TChannel> channelFactory)
		{
			if (String.IsNullOrEmpty(endpointConfigurationName))
			{
				throw new ArgumentNullException("endpointConfigurationName");
			}

			if (channelFactory == null)
			{
				throw new ArgumentNullException("channelFactory");
			}

			_endpointConfigurationName = endpointConfigurationName;
			_channelFactory = channelFactory;
		}

		/// <summary>
		/// Initializes a new instance of the ChannelFactoryWrapper class.
		/// </summary>
		/// <param name="descriptor">The endpoint descriptor for the channel.</param>
		/// <param name="channelFactory">The channel factory being wrapped.</param>
		public ChannelFactoryWrapper(EndpointDescriptor descriptor, ChannelFactory<TChannel> channelFactory)
		{
			if (descriptor == null)
			{
				throw new ArgumentNullException("descriptor");
			}

			if (channelFactory == null)
			{
				throw new ArgumentNullException("channelFactory");
			}

			_descriptor = descriptor;
			_channelFactory = channelFactory;
		}

		private EndpointDescriptor _descriptor;
		/// <summary>
		/// Gets the endpoint descriptor for the channel factory.
		/// </summary>
		public EndpointDescriptor Descriptor { get { return _descriptor; } }

		private String _endpointConfigurationName;
		/// <summary>
		/// Gets the name of the endpoint configuration used by the channel factory.
		/// </summary>
		public String EndpointConfigurationName { get { return _endpointConfigurationName; } }

		private ChannelFactory<TChannel> _channelFactory;
		/// <summary>
		/// Gets the wrapped channel factory.
		/// </summary>
		public ChannelFactory<TChannel> ChannelFactory { get { return _channelFactory; } }

		private bool _disposed;
		private void DoDispose()
		{
			if (Disposed != null)
			{
				Disposed(this, new EventArgs());
			}
		}

		#region IDisposable Members

		/// <summary>
		/// Fired when the wrapper is disposed.
		/// </summary>
		public event EventHandler Disposed;
		
		/// <summary>
		/// Disposes the channel factory wrapper.
		/// </summary>
		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				if (_channelFactory != null)
				{
					_channelFactory = null;
				}

				DoDispose();
			}
		}

		#endregion
	}
}
