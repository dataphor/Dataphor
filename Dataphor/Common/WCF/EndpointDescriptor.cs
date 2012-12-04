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
	/// Represents an endpoint as a binding and an endpoint address.
	/// </summary>
	internal class EndpointDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the EndpointDescriptor class.
		/// </summary>
		/// <param name="binding">The binding for the endpoint.</param>
		/// <param name="endpointAddress">The address for the endpoint.</param>
		public EndpointDescriptor(Binding binding, EndpointAddress endpointAddress)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}

			if (endpointAddress == null)
			{
				throw new ArgumentNullException("endpointAddress");
			}

			_binding = binding;
			_endpointAddress = endpointAddress;
		}

		private Binding _binding;
		/// <summary>
		/// Gets the binding of the endpoint.
		/// </summary>
		public Binding Binding { get { return _binding; } }

		private EndpointAddress _endpointAddress;
		/// <summary>
		/// Gets the address of the endpoint.
		/// </summary>
		public EndpointAddress EndpointAddress { get { return _endpointAddress; } }

		/// <summary>
		/// Returns true if the given endpoint descriptor describes the same endpoint.
		/// </summary>
		/// <param name="obj">The endpoint descriptor to compare.</param>
		/// <returns>True if the given endpoint descriptor describes the same endpoint.</returns>
		/// <remarks>
		/// <para>
		/// Comparison of endpoints is done by reference for the binding, and by the Uri.ToString() result
		/// for the endpoint address.
		/// </para>
		/// <para>
		/// This means that in order for the channel factory caching to work, the binding instance
		/// must be the same for each endpoint.
		/// </para>
		/// </remarks>
		public override bool Equals(object obj)
		{
			var that = obj as EndpointDescriptor;
			return that != null && this._binding == that._binding && this._endpointAddress.Uri.ToString() == that._endpointAddress.Uri.ToString();
		}

		/// <summary>
		/// Returns a hash code consistent with the equality semantics defined by the Equals override.
		/// </summary>
		/// <returns>A hash code consistent with the equality semantics defined by the Equals override.</returns>
		public override int GetHashCode()
		{
			// TODO: More sophisticated hash
			return _binding.GetHashCode() ^ _endpointAddress.Uri.ToString().GetHashCode();
		}
	}
}
