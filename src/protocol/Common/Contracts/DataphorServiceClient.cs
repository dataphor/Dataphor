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

using Alphora.Common.WCF;

namespace Alphora.Dataphor.DAE.Contracts
{
	public class DataphorServiceClient<T> : ServiceClient<T> where T : class
	{
		public DataphorServiceClient(Binding binding, EndpointAddress address) : base(binding, address) { }

		public DataphorServiceClient(Uri endpointURI) 
			: this
			(
				DataphorServiceUtility.GetBinding(), 
				new EndpointAddress(endpointURI, DataphorServiceUtility.BuildEndpointIdentityCall())
			)
		{
		}

		public DataphorServiceClient(String endpointConfigurationName) : base(endpointConfigurationName) { }
	}
}
