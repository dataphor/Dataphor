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

		// The default is declared before the field that starts out holding it, because C# runs static field initialisers
		// in textual order and the other order would leave BuildBindingCall null. It is one delegate instance so that
		// HasCustomBinding can compare by reference; naming the method again would build a second delegate and make "is
		// this still ours" a question about delegate equality rather than identity. The host half of the binding,
		// DataphorServiceHostHooks in the hosting assembly, is built the same way for the same reason.
		private static readonly Func<Binding> _defaultBuildBindingCall = DefaultBuildBinding;

		public static Func<Binding> BuildBindingCall = _defaultBuildBindingCall;
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
				BuildBindingCall = _defaultBuildBindingCall;
				BuildEndpointIdentityCall = DefaultBuildEndpointIdentity;
				ServiceEndpointHook = DefaultServiceEndpointHook;
			}
		}

		/// <summary>False while the binding is the product's own; how a caller tells whether a site has replaced it.</summary>
		/// <remarks>
		/// What this is for: <see cref="CheckSecurityMode"/> must not refuse a secured connection at a site whose own
		/// binding may well secure the transport. It asks about the binding alone, and deliberately: transport security
		/// lives in a binding element, so <see cref="ServiceEndpointHook"/> on its own - an endpoint behaviour over the
		/// product's unsecured binding - secures nothing and is not a reason to stay quiet. The host half of the protocol
		/// carries the same member on DataphorServiceHostHooks, in the hosting assembly.
		/// </remarks>
		public static bool HasCustomBinding
		{
			get { return !ReferenceEquals(BuildBindingCall, _defaultBuildBindingCall); }
		}

		/// <summary>Refuses a connection setting that asks for transport security, which this version's own binding cannot carry.</summary>
		/// <param name="mode">What a connection setting asked for; only <see cref="ConnectionSecurityMode.Transport"/> is refused.</param>
		/// <param name="settingName">The setting as the person who set it sees it; the message names it.</param>
		/// <remarks>
		/// <para>
		/// None of the product's secured-connection settings has ever reached a binding: <see cref="DefaultBuildBinding"/>
		/// leaves the transport's security at its default, which is none. Connecting unsecured anyway would defeat exactly
		/// what the setting asked for, and on the client the person who set it is the person connecting, so the request is
		/// refused instead of being ignored. Default and None are not refused: both mean an unsecured connection, which is
		/// what they get.
		/// </para>
		/// <para>
		/// Nothing is refused once <see cref="HasCustomBinding"/> holds. A site that supplies its own binding owns that
		/// channel's security - supplying one is the only route there has ever been to a secured Dataphor connection - and
		/// the product must not second-guess a channel it did not build.
		/// </para>
		/// <para>
		/// NotSupportedException rather than a new error code, following the precedent of the ClientConfigurationName
		/// refusal in ClientServer.Open: this assembly has no reference to the engine assembly where ServerException lives,
		/// and this is the same category of failure, a setting this version cannot honour.
		/// </para>
		/// </remarks>
		public static void CheckSecurityMode(ConnectionSecurityMode mode, string settingName)
		{
			if ((mode == ConnectionSecurityMode.Transport) && !HasCustomBinding)
				throw new NotSupportedException
				(
					String.Format
					(
						"The connection setting {0} is set to \"{1}\", but the connection this version of Dataphor builds carries no transport security, so the request cannot be honoured and no unsecured connection is made in its place. Either set it to \"{2}\" or \"{3}\", both of which connect unsecured, or supply a binding that secures the transport by setting DataphorServiceUtility.BuildBindingCall from the host application's own start-up code before connecting.",
						settingName,
						ConnectionSecurityMode.Transport,
						ConnectionSecurityMode.Default,
						ConnectionSecurityMode.None
					)
				);
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
