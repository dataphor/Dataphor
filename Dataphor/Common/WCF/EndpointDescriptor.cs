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
	class EndpointDescriptor
	{
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
		public Binding Binding { get { return _binding; } }

		private EndpointAddress _endpointAddress;
		public EndpointAddress EndpointAddress { get { return _endpointAddress; } }

		public override bool Equals(object obj)
		{
			var that = obj as EndpointDescriptor;
			return that != null && this._binding == that._binding && this._endpointAddress.Uri.ToString() == that._endpointAddress.Uri.ToString();
		}

		public override int GetHashCode()
		{
			// TODO: More sophisticated hash
			return _binding.GetHashCode() ^ _endpointAddress.Uri.ToString().GetHashCode();
		}
	}
}
