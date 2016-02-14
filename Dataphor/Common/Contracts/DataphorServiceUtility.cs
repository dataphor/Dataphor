//#define USENAMEDPIPES

/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Alphora.Dataphor.DAE.Contracts
{
	public enum ConnectionSecurityMode { Default, None, Transport };

	public static class DataphorServiceUtility
	{
		public const int DefaultListenerPortNumber = 8060;

		private static object _syncHandle = new Object();

		private static Binding _binding;
		
		private static string GetScheme()
		{
			#if USENAMEDPIPES
			return Uri.UriSchemeNetPipe;
			#else
			return Uri.UriSchemeNetTcp;
			#endif
		}
		
		public static string BuildInstanceURI(string hostName, int portNumber, string instanceName)
		{
			#if USENAMEDPIPES
			return String.Format("{0}://{1}/{2}/service", GetScheme(), hostName, instanceName);
			#else
			return String.Format("{0}://{1}:{2}/{3}/service", GetScheme(), hostName, portNumber, instanceName);
			#endif
		}
		
		public static string BuildNativeInstanceURI(string hostName, int portNumber, string instanceName)
		{
			#if USENAMEDPIPES
			return String.Format("{0}://{1}/{2}/service/native", GetScheme(), hostName, instanceName);
			#else
			return String.Format("{0}://{1}:{2}/{3}/service/native", GetScheme(), hostName, portNumber, instanceName);
			#endif
		}

		public static string BuildListenerURI(string hostName, int overridePortNumber)
		{
			#if USENAMEDPIPES
			return String.Format("{0}://{1}/listener/service", GetScheme(), hostName);
			#else
			return String.Format("{0}://{1}:{2}/listener/service", GetScheme(), hostName, overridePortNumber == 0 ? DefaultListenerPortNumber : overridePortNumber); 
			#endif
		} 		
	
		public const int MaxMessageLength = 2147483647;

		public static Func<Binding> BuildBindingCall = DefaultBuildBinding;
		public static Func<EndpointIdentity> BuildEndpointIdentityCall = DefaultBuildEndpointIdentity;
		public static Action<ServiceEndpoint> ServiceEndpointHook = DefaultServiceEndpointHook;

		private static void DefaultServiceEndpointHook(ServiceEndpoint endpoint)
		{
			// - Does nothing
		}

		private static EndpointIdentity DefaultBuildEndpointIdentity()
		{
			return null;
		}

		public static void RevertHooksToDefault()
		{
			lock(_syncHandle)
			{
				BuildBindingCall = DefaultBuildBinding;
				BuildEndpointIdentityCall = DefaultBuildEndpointIdentity;
				ServiceEndpointHook = DefaultServiceEndpointHook;
			}
		}

		private static Binding DefaultBuildBinding()
		{
			var messageEncodingElement = new BinaryMessageEncodingBindingElement();
			#if !SILVERLIGHT
			messageEncodingElement.ReaderQuotas.MaxArrayLength = MaxMessageLength;
			messageEncodingElement.ReaderQuotas.MaxStringContentLength = MaxMessageLength;
			#endif

			#if USENAMEDPIPES && !SILVERLIGHT
			NamedPipeTransportBindingElement transportElement = new NamedPipeTransportBindingElement();
			#else
			TcpTransportBindingElement transportElement = new TcpTransportBindingElement();
			#endif

			transportElement.MaxBufferSize = MaxMessageLength;
			transportElement.MaxReceivedMessageSize = MaxMessageLength;
			
			var binding = new CustomBinding(messageEncodingElement, transportElement);
			binding.SendTimeout = TimeSpan.MaxValue;
			binding.ReceiveTimeout = TimeSpan.MaxValue;
			return binding;
		}

		public static Binding GetBinding()
		{
			if (_binding == null)
			{
				lock (_syncHandle)
				{
					if (_binding == null)
					{
						_binding = BuildBindingCall();
					}
				}
			}

			return _binding;
		}
	}
}
