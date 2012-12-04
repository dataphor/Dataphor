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
	internal class ChannelFactoryWrapper<TChannel> : IDisposable
	{
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
		public EndpointDescriptor Descriptor { get { return _descriptor; } }

		private ChannelFactory<TChannel> _channelFactory;
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

		public event EventHandler Disposed;
		 
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
