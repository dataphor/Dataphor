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
	public enum ConnectionSecurityMode { Default, None, Transport };

	public static class DataphorServiceUtility
	{
		public const int DefaultListenerPortNumber = 8060;
		
		private static string GetScheme()
		{
			return Uri.UriSchemeNetTcp;
		}
		
		public static string BuildInstanceURI(string hostName, int portNumber, string instanceName)
		{
			return String.Format("{0}://{1}:{2}/{3}/service", GetScheme(), hostName, portNumber, instanceName);
		}
		
		public static string BuildNativeInstanceURI(string hostName, int portNumber, string instanceName)
		{
			return String.Format("{0}://{1}:{2}/{3}/service/native", GetScheme(), hostName, portNumber, instanceName);
		}

		public static string BuildListenerURI(string hostName, int overridePortNumber)
		{
			return String.Format("{0}://{1}:{2}/listener/service", GetScheme(), hostName, overridePortNumber == 0 ? DefaultListenerPortNumber : overridePortNumber);  				
		} 		
	
		public const int MaxMessageLength = 2147483647;

		public static Binding GetBinding()
		{
			var messageEncodingElement = new BinaryMessageEncodingBindingElement();
			#if !SILVERLIGHT
			messageEncodingElement.ReaderQuotas.MaxArrayLength = MaxMessageLength;
			messageEncodingElement.ReaderQuotas.MaxStringContentLength = MaxMessageLength;
			#endif

			TcpTransportBindingElement transportElement = new TcpTransportBindingElement();

			transportElement.MaxBufferSize = MaxMessageLength;
			transportElement.MaxReceivedMessageSize = MaxMessageLength;
			
			var binding = new CustomBinding(messageEncodingElement, transportElement);
			binding.SendTimeout = TimeSpan.MaxValue;
			binding.ReceiveTimeout = TimeSpan.MaxValue;
			return binding;
		}
	}
}
