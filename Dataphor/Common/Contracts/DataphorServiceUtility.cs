/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.Contracts
{
	public static class DataphorServiceUtility
	{
		public const int CDefaultListenerPortNumber = 8060;
		
		public static string BuildInstanceURI(string AHostName, int APortNumber, string AInstanceName)
		{
			return String.Format("http://{0}:{1}/{2}/service", AHostName, APortNumber, AInstanceName);
		}
		
		public static string BuildNativeInstanceURI(string AHostName, int APortNumber, string AInstanceName)
		{
			return String.Format("http://{0}:{1}/{2}/service/native", AHostName, APortNumber, AInstanceName);
		}
		
		public static string BuildListenerURI(string AHostName)
		{
			return String.Format("http://{0}:{1}/listener/service", AHostName, CDefaultListenerPortNumber);
		}

		public static Binding GetBinding()
		{
			//return new BasicHttpBinding();
			return new CustomBinding(new BinaryMessageEncodingBindingElement(), new HttpTransportBindingElement());
		}
	}
}
