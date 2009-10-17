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
		public const int CDefaultListenerPortNumber = 8060;
		public const int CDefaultSecureListenerPortNumber = 8600;
		
		private static string GetScheme(bool ASecure)
		{
			return ASecure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
		}
		
		public static string BuildInstanceURI(string AHostName, int APortNumber, bool ASecure, string AInstanceName)
		{
			return String.Format("{0}://{1}:{2}/{3}/service", GetScheme(ASecure), AHostName, APortNumber, AInstanceName);
		}
		
		public static string BuildNativeInstanceURI(string AHostName, int APortNumber, bool ASecure, string AInstanceName)
		{
			return String.Format("{0}://{1}:{2}/{3}/service/native", GetScheme(ASecure), AHostName, APortNumber, AInstanceName);
		}
		
		public static string BuildListenerURI(string AHostName, int AOverridePortNumber, ConnectionSecurityMode ASecurityMode)
		{
			return 
				BuildListenerURI
				(
					AHostName, 
					AOverridePortNumber == 0 
						? 
						(
							ASecurityMode == ConnectionSecurityMode.Transport 
								? CDefaultSecureListenerPortNumber 
								: CDefaultListenerPortNumber 
						)
						: AOverridePortNumber, 
					ASecurityMode == ConnectionSecurityMode.Transport
				);
		}
		
		public static string BuildListenerURI(string AHostName, int APortNumber, bool ASecure)
		{
			return String.Format("{0}://{1}:{2}/listener/service", GetScheme(ASecure), AHostName, APortNumber);
		}
		
		public static string BuildCrossDomainServiceURI(string AHostName, int APortNumber, bool ASecure)
		{
			return String.Format("{0}://{1}:{2}", GetScheme(ASecure), AHostName, APortNumber);
		}
		
		public const int CMaxMessageLength = 10485760;

		public static Binding GetBinding(bool ASecure)
		{
			//return new BasicHttpBinding();
			var LMessageEncodingElement = new BinaryMessageEncodingBindingElement();
			#if !SILVERLIGHT
			LMessageEncodingElement.ReaderQuotas.MaxArrayLength = CMaxMessageLength;
			LMessageEncodingElement.ReaderQuotas.MaxStringContentLength = CMaxMessageLength;
			#endif
			
			var LTransportElement = ASecure ? new HttpsTransportBindingElement() : new HttpTransportBindingElement();
			LTransportElement.MaxBufferSize = CMaxMessageLength;
			LTransportElement.MaxReceivedMessageSize = CMaxMessageLength;
			
			return new CustomBinding(LMessageEncodingElement, LTransportElement);
		}
	}
}
