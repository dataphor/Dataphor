/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using CoreWCF.Configuration;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>
	/// The one .NET Generic Host shape both hosts in this assembly use: Kestrel, serving CoreWCF on a single net.tcp
	/// address and on nothing else.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Shared so that the instance's host and the listener's cannot drift apart, and so that host-generic-host - which
	/// owns the logging these hosts currently discard - has one place to change. All that differs between them is what
	/// each registers: the instance's host has two service objects it built itself, the listener a service type CoreWCF
	/// constructs per session.
	/// </para>
	/// <para>
	/// The .NET Generic Host, not WebHostBuilder: .NET 10 marks WebHostBuilder and IWebHost obsolete (ASPDEPR004,
	/// ASPDEPR008). CoreWCF's NetTcp listener is a Kestrel connection handler and configures the same way under either,
	/// and this is the host host-generic-host builds on.
	/// </para>
	/// </remarks>
	internal static class NetTcpServiceHost
	{
		/// <summary>How long Kestrel is given to close open connections before it aborts them.</summary>
		/// <remarks>
		/// Kestrel's graceful shutdown waits for open connections, and a client channel may still be open, so stopping is
		/// a bounded wait and then an abort rather than an indefinite one. Before CoreWCF, DataphorServiceHost.Stop closed
		/// the WCF hosts asynchronously and did not wait at all.
		/// </remarks>
		internal static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

		/// <summary>The address to listen on, which carries no path: the endpoint addresses supply those.</summary>
		// NOTE: net.tcp outright, where the addresses the endpoints and the client use come from DataphorServiceUtility,
		// whose USENAMEDPIPES compile-time switch would make them net.pipe and drop the port. Nothing defines it, and
		// defining it would have to change these hosts too - this scheme, the port, and UseNetTcp - not just that file.
		internal static Uri ListenAddress(string hostName, int portNumber)
		{
			return new Uri(String.Format("{0}://{1}:{2}/", Uri.UriSchemeNetTcp, hostName, portNumber));
		}

		/// <summary>Builds a host serving <paramref name="addEndpoints"/> on one port; it is not started.</summary>
		/// <param name="registerServices">What this host's services need from dependency injection, or null.</param>
		internal static IHost Build(string hostName, int portNumber, Action<IServiceCollection> registerServices, Action<IServiceBuilder> addEndpoints)
		{
			return
				new HostBuilder()
					.ConfigureLogging(logging => logging.ClearProviders())
					.ConfigureWebHost
					(
						webHostBuilder =>
							webHostBuilder
								.UseKestrel()
								.UseContentRoot(AppContext.BaseDirectory)
								// Without this Kestrel also opens its own default HTTP endpoint; this process listens on nothing but net.tcp.
								.UseSetting(WebHostDefaults.ServerUrlsKey, String.Empty)
								.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "true")
								// One Listen call: every endpoint on this host is on the one port, and CoreWCF dispatches between them by path.
								.UseNetTcp(options => options.Listen(ListenAddress(hostName, portNumber)))
								.ConfigureServices
								(
									services =>
									{
										services.AddServiceModelServices();
										if (registerServices != null)
											registerServices(services);
									}
								)
								.Configure(application => application.UseServiceModel(addEndpoints))
					)
					.Build();
		}

		/// <summary>Stops a host within <see cref="StopTimeout"/> and disposes it, whether or not it ever started.</summary>
		/// <remarks>
		/// A host that never started stops without doing anything, and a host is disposed even if stopping it fails.
		/// </remarks>
		// NOTE: both hosts are started and stopped by blocking on the asynchronous call, which is what the synchronous
		// Start and Stop surface of DataphorServiceHost requires. Microsoft's hosting and Kestrel code awaits with
		// ConfigureAwait(false) throughout, so blocking does not deadlock on a thread that has a synchronization context -
		// the Windows client and the IDE start a non-embedded in-process alias on the user-interface thread. If a hang
		// ever turns up there, that assumption is the thing to check; an asynchronous surface is host-generic-host's.
		internal static void Stop(IHost host)
		{
			try
			{
				host.StopAsync(StopTimeout).GetAwaiter().GetResult();
			}
			finally
			{
				host.Dispose();
			}
		}
	}
}
