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
	/// Implements a channel factory cache for a given service interface.
	/// </summary>
	/// <typeparam name="TChannel">The type of the service interface.</typeparam>
	/// <remarks>
	/// <para>
	/// Caches channel factory instances for a specific service interface per endpoint, where
	/// endpoint is defined per unique binding instance and endpoint address.
	/// </para>
	/// </remarks>
	internal class ChannelFactoryCache<TChannel>
	{
		private static EndpointDescriptorComparer DefaultEndpointDescriptorComparer = new EndpointDescriptorComparer();

		private object _syncHandle = new object();
		private IDictionary<EndpointDescriptor, ChannelFactoryEntry<TChannel>> _cache = new Dictionary<EndpointDescriptor, ChannelFactoryEntry<TChannel>>(DefaultEndpointDescriptorComparer);

		/// <summary>
		/// Gets a channel factory wrapper for the given binding and endpoint address.
		/// </summary>
		/// <param name="binding">The binding for the channel factory.</param>
		/// <param name="endpointAddress">The endpoint address for the channel factory.</param>
		/// <returns>A channel factory wrapper for a channel factory using the given binding and endpoint address.</returns>
		public ChannelFactoryWrapper<TChannel> GetChannelFactory(Binding binding, EndpointAddress endpointAddress)
		{
			lock (_syncHandle)
			{
				var descriptor = new EndpointDescriptor(binding, endpointAddress);
				ChannelFactoryEntry<TChannel> entry;
				if (!_cache.TryGetValue(descriptor, out entry))
				{
					entry = new ChannelFactoryEntry<TChannel>(descriptor);
					_cache.Add(descriptor, entry);
				}

				var wrapper = entry.GetChannelFactory();
				wrapper.Disposed += new EventHandler(wrapper_Disposed);
				return wrapper;
			}
		}

		// Remove a reference due to disposal
		void wrapper_Disposed(object sender, EventArgs e)
		{
			var wrapper = sender as ChannelFactoryWrapper<TChannel>;
			if (wrapper == null)
			{
				throw new InvalidOperationException("Sender is not a ChannelFactoryWrapper.");
			}

			lock (_syncHandle)
			{
				ChannelFactoryEntry<TChannel> entry;
				if (_cache.TryGetValue(wrapper.Descriptor, out entry))
				{
					if (entry.ReleaseChannelFactory())
					{
						_cache.Remove(wrapper.Descriptor);
						entry.Dispose();
					}
				}
				else
				{
					// In theory, this will never occur... throwing for safety.
					throw new InvalidOperationException("Could not retrieve factory entry for endpoint descriptor.");
				}
			}
		}

		private class EndpointDescriptorComparer : IEqualityComparer<EndpointDescriptor>
		{
			#region IEqualityComparer<EndpointDescriptor> Members

			public bool  Equals(EndpointDescriptor x, EndpointDescriptor y)
			{
 				if (x != null)
				{
					if (y != null)
					{
						return x.Equals(y);
					}
					else
					{
						return false;
					}
				}
				else
				{
					return y == null;
				}
			}

			public int  GetHashCode(EndpointDescriptor obj)
			{
				if (obj == null)
				{
					return 0;
				}
				else
				{
					return obj.GetHashCode();
				}
			}

			#endregion
		}
	}
}
