/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using CoreWCF.Configuration;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>
	/// The host's half of the protocol's extension points: what a deployment replaces to change what the servers in this
	/// process listen with. The client's half is DataphorServiceUtility, whose BuildBindingCall,
	/// BuildEndpointIdentityCall and ServiceEndpointHook change what a client connects with.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Supplying a binding from a host application's own start-up code is the only route there has ever been to a secured
	/// Dataphor connection: none of the product's own secured-connection settings reaches a binding. The client hook alone
	/// stopped being enough when the server's binding moved into this assembly, CoreWCF's Binding type being unrelated to
	/// System.ServiceModel's, and these members are what replaces the host half of it.
	/// </para>
	/// <para>
	/// The hooks are process-wide, and each is read while a host is being built and started, so install them before
	/// calling <see cref="DataphorServiceHost.Start()"/>. One installed after a host has started does not reach that
	/// host, exactly as on the client.
	/// </para>
	/// </remarks>
	public static class DataphorServiceHostHooks
	{
		private static object _syncHandle = new Object();

		// The defaults are declared before the fields that start out holding them, because C# runs static field
		// initialisers in textual order and the other order would leave both hooks null. Each is one delegate instance so
		// that HasCustomBinding can compare by reference; naming the method again would build a second delegate and make
		// "is this still ours" a question about delegate equality rather than identity.
		private static readonly Func<CoreWCF.Channels.Binding> _defaultBuildBindingCall = DataphorServiceHostBinding.Build;
		private static readonly Action<IServiceBuilder> _defaultServiceBuilderHook = DefaultServiceBuilderHook;

		/// <summary>Builds the binding every endpoint of a host in this process is served on.</summary>
		public static Func<CoreWCF.Channels.Binding> BuildBindingCall = _defaultBuildBindingCall;

		/// <summary>A last look at each host's service builder, after that host's endpoints have been added.</summary>
		/// <remarks>
		/// Where service credentials - a server certificate, for one - are installed: CoreWCF reaches them through the
		/// ConfigureServiceHostBase&lt;TService&gt; and ConfigureAllServiceHostBase extension methods on IServiceBuilder
		/// (declared in CoreWCF.Configuration.ServiceBuilderExtensions, so a hook that calls them needs that namespace),
		/// which hand a delegate a CoreWCF.ServiceHostBase and its Credentials while that host is being built. That the hook is
		/// early enough for it is established by running it, in
		/// HostBindingHookTest.TestTheBuilderHookReachesEachHostsServiceCredentials, because this is called from inside
		/// the application-pipeline callback, after the host's dependency-injection container is built. The builder is
		/// the unit rather than the endpoint, unlike the client's ServiceEndpointHook, because CoreWCF's
		/// AddServiceEndpoint returns the builder and there is no endpoint object to hand out.
		/// </remarks>
		public static Action<IServiceBuilder> ServiceBuilderHook = _defaultServiceBuilderHook;

		/// <summary>False while the binding is the product's own; how a caller tells whether a site has replaced it.</summary>
		/// <remarks>
		/// What this is for: code that reports the product's secured-connection settings as having no effect must not
		/// report that at a site whose own binding may well secure the transport. It asks about the binding alone, and
		/// deliberately: transport security lives in a binding element, so <see cref="ServiceBuilderHook"/> on its own -
		/// credentials over the product's unsecured binding - secures nothing and is not a reason to stay quiet.
		/// </remarks>
		public static bool HasCustomBinding
		{
			get { return !ReferenceEquals(BuildBindingCall, _defaultBuildBindingCall); }
		}

		private static void DefaultServiceBuilderHook(IServiceBuilder builder)
		{
			// - Does nothing
		}

		/// <summary>Puts both hooks back to the product's own, as DataphorServiceUtility.RevertHooksToDefault does for the client's.</summary>
		/// <remarks>
		/// The lock is this class's own, mirroring the client's, which also guards the binding the client memoizes.
		/// Nothing here reads the fields under it, so a revert that races a host being started is as unordered as
		/// installing a hook in that window would be; both are start-up-time operations.
		/// </remarks>
		public static void RevertHooksToDefault()
		{
			lock (_syncHandle)
			{
				BuildBindingCall = _defaultBuildBindingCall;
				ServiceBuilderHook = _defaultServiceBuilderHook;
			}
		}

		/// <summary>The binding for one host, from <see cref="BuildBindingCall"/>; both hosts in this assembly read it here.</summary>
		/// <remarks>
		/// Nothing is memoized, unlike the client's DataphorServiceUtility.GetBinding: the instance's host and the
		/// listener's are separate servers on separate ports, each builds its own binding as each does today, and a hook is
		/// entitled to return a different one per call. Keep it that way.
		/// </remarks>
		internal static CoreWCF.Channels.Binding BuildBinding()
		{
			CoreWCF.Channels.Binding binding = BuildBindingCall();
			// Refused here rather than inside CoreWCF, which would fail while an endpoint was being added with nothing in
			// the message to say that a hook produced the null.
			if (binding == null)
				throw new InvalidOperationException("DataphorServiceHostHooks.BuildBindingCall returned no binding. A hook installed in place of the default must return one.");
			return binding;
		}
	}
}
