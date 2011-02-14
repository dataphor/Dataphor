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
		public const int DefaultSecureListenerPortNumber = 8600;
		
		private static string GetScheme(bool secure)
		{
			return secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
		}
		
		public static string BuildInstanceURI(string hostName, int portNumber, bool secure, string instanceName)
		{
			return String.Format("{0}://{1}:{2}/{3}/service", GetScheme(secure), hostName, portNumber, instanceName);
		}
		
		public static string BuildNativeInstanceURI(string hostName, int portNumber, bool secure, string instanceName)
		{
			return String.Format("{0}://{1}:{2}/{3}/service/native", GetScheme(secure), hostName, portNumber, instanceName);
		}
		
		public static string BuildListenerURI(string hostName, int overridePortNumber, ConnectionSecurityMode securityMode)
		{
			return 
				BuildListenerURI
				(
					hostName, 
					overridePortNumber == 0 
						? 
						(
							securityMode == ConnectionSecurityMode.Transport 
								? DefaultSecureListenerPortNumber 
								: DefaultListenerPortNumber 
						)
						: overridePortNumber, 
					securityMode == ConnectionSecurityMode.Transport
				);
		}
		
		public static string BuildListenerURI(string hostName, int portNumber, bool secure)
		{
			return String.Format("{0}://{1}:{2}/listener/service", GetScheme(secure), hostName, portNumber);
		}
		
		public static string BuildCrossDomainServiceURI(string hostName, int portNumber, bool secure)
		{
			return String.Format("{0}://{1}:{2}", GetScheme(secure), hostName, portNumber);
		}

		public const int MaxMessageLength = 2147483647;

		public static Binding GetBinding(bool secure)
		{
			//return new BasicHttpBinding();
			var messageEncodingElement = new BinaryMessageEncodingBindingElement();
			#if !SILVERLIGHT
			messageEncodingElement.ReaderQuotas.MaxArrayLength = MaxMessageLength;
			messageEncodingElement.ReaderQuotas.MaxStringContentLength = MaxMessageLength;
			#endif
			
			var transportElement = secure ? new HttpsTransportBindingElement() : new HttpTransportBindingElement();
			transportElement.MaxBufferSize = MaxMessageLength;
			transportElement.MaxReceivedMessageSize = MaxMessageLength;
			
			var binding = new CustomBinding(messageEncodingElement, transportElement);
			binding.SendTimeout = TimeSpan.MaxValue;
			binding.ReceiveTimeout = TimeSpan.MaxValue;
			return binding;
		}
	}
}
