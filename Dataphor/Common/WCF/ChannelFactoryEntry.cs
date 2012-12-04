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

namespace Alphora.Dataphor.DAE.WCF
{
	class ChannelFactoryEntry<TChannel> : IDisposable
	{
		public ChannelFactoryEntry(EndpointDescriptor descriptor)
		{
			if (descriptor == null)
			{
				throw new ArgumentNullException("descriptor");
			}

			_descriptor = descriptor;

			_channelFactory = new ChannelFactory<TChannel>(descriptor.Binding, descriptor.EndpointAddress);
			_channelFactory.Open();
		}

		private EndpointDescriptor _descriptor;
		public EndpointDescriptor Descriptor { get { return _descriptor; } }

		private ChannelFactory<TChannel> _channelFactory;

		private int _refCount;

		private bool _disposed;

		#region IDisposable Members

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
		
		public ChannelFactoryWrapper<TChannel> GetChannelFactory()
		{
			CheckNotDisposed();

			var wrapper = new ChannelFactoryWrapper<TChannel>(_descriptor, _channelFactory);
			_refCount++;
			return wrapper;
		}

		public bool ReleaseChannelFactory()
		{
			CheckNotDisposed();

			_refCount--;
			return _refCount == 0;
		}
	}
}
