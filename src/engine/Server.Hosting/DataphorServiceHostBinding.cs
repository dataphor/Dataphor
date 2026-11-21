/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Service
{
	using Alphora.Dataphor.DAE.Contracts;

	/// <summary>
	/// The host's half of the one binding the protocol is specified by. The client's half, which is the original, is
	/// DataphorServiceUtility.DefaultBuildBinding; the two stacks have unrelated Binding types, so the settings are
	/// necessarily written twice and this copy has to be changed with that one. Every number here is read from
	/// DataphorServiceUtility, and CoreWcfProtocolTest.TestHostBindingMatchesTheClientBinding compares the rest.
	/// This is the default of DataphorServiceHostHooks.BuildBindingCall, which is what the hosts actually read; a
	/// deployment with a binding of its own replaces that hook and leaves this declaration alone.
	/// </summary>
	public static class DataphorServiceHostBinding
	{
		/// <summary>Builds the binding; each host builds one and uses it for all of its endpoints.</summary>
		public static CoreWCF.Channels.Binding Build()
		{
			CoreWCF.Channels.BinaryMessageEncodingBindingElement messageEncodingElement = new CoreWCF.Channels.BinaryMessageEncodingBindingElement();
			messageEncodingElement.ReaderQuotas.MaxArrayLength = DataphorServiceUtility.MaxMessageLength;
			messageEncodingElement.ReaderQuotas.MaxStringContentLength = DataphorServiceUtility.MaxMessageLength;

			CoreWCF.Channels.TcpTransportBindingElement transportElement = new CoreWCF.Channels.TcpTransportBindingElement();
			transportElement.MaxBufferSize = DataphorServiceUtility.MaxMessageLength;
			transportElement.MaxReceivedMessageSize = DataphorServiceUtility.MaxMessageLength;

			CoreWCF.Channels.CustomBinding binding = new CoreWCF.Channels.CustomBinding(messageEncodingElement, transportElement);
			binding.SendTimeout = TimeSpan.MaxValue;
			binding.ReceiveTimeout = TimeSpan.MaxValue;
			return binding;
		}
	}
}
