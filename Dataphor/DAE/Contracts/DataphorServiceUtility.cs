/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.Contracts
{
	public static class DataphorServiceUtility
	{
		public static EndpointAddress BuildURI(string AHostName, int APortNumber, string AInstanceName)
		{
			return new EndpointAddress(String.Format("http://{0}:{1}/{2}/service", AHostName, APortNumber, AInstanceName));
		}
	}
}
