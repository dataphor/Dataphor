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
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Common.WCF
{
	/// <summary>
	/// Represents a channel factory cache entry.
	/// </summary>
	/// <typeparam name="TChannel">The type of the channel being cached.</typeparam>
	internal class ChannelFactoryEntry<TChannel> : IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the ChannelFactoryEntry class.
		/// </summary>
		/// <param name="descriptor">The endpoint descriptor for the cached channel entry.</param>
		public ChannelFactoryEntry(EndpointDescriptor descriptor)
		{
			if (descriptor == null)
			{
				throw new ArgumentNullException("descriptor");
			}

			_descriptor = descriptor;

			_channelFactory = new ChannelFactory<TChannel>(descriptor.Binding, descriptor.EndpointAddress);

			DataphorServiceUtility.ServiceEndpointHook(_channelFactory.Endpoint);

			_channelFactory.Open();
		}

		private EndpointDescriptor _descriptor;
		/// <summary>
		/// Gets the endpoint descriptor for the entry.
		/// </summary>
		public EndpointDescriptor Descriptor { get { return _descriptor; } }

		private ChannelFactory<TChannel> _channelFactory;

		private int _refCount;

		private bool _disposed;

		#region IDisposable Members

		/// <summary>
		/// Disposes the channel factory entry, releasing the underlying channel factory.
		/// </summary>
		public void Dispose()
		{
			if (!_disposed)
			{	
				_disposed = true;

				if (_channelFactory != null)
				{
					CloseChannelFactory();
					_channelFactory = null;
				}
			}
		}

		#endregion

		private void CheckNotDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ChannelFactoryEntry");
			}
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

		/// <summary>
		/// Gets a channel factory wrapper for the cached channel factory.
		/// </summary>
		/// <returns>A channel factory wrapper for the cached channel factory.</returns>
		public ChannelFactoryWrapper<TChannel> GetChannelFactory()
		{
			CheckNotDisposed();

			var wrapper = new ChannelFactoryWrapper<TChannel>(_descriptor, _channelFactory);
			_refCount++;
			return wrapper;
		}

		/// <summary>
		/// Releases a channel factory and returns whether or not any references are still active.
		/// </summary>
		/// <returns>True if there are no active references to the channel factory, false otherwise.</returns>
		public bool ReleaseChannelFactory()
		{
			CheckNotDisposed();

			_refCount--;
			return _refCount == 0;
		}
	}
}
