/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Alphora.Dataphor.DAE.Service
{
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Contracts;
	using Alphora.Dataphor.DAE.NativeCLI;

	public class DataphorServiceHost
	{
		private Server FServer;
		/// <summary>
		/// Provides in-process access to the hosted server
		/// </summary>
		public IServer Server { get { return FServer; } }
		
		private RemoteServer FRemoteServer;
		private DataphorService FService;
		private ServiceHost FServiceHost;
		private NativeServer FNativeServer;
		private NativeCLIService FNativeService;
		private ServiceHost FNativeServiceHost;
		private ListenerServiceHost FListenerServiceHost;
		private CrossDomainServiceHost FCrossDomainServiceHost;
		
		public bool IsActive { get { return FServer != null; } }
		
		private void CheckInactive()
		{
			if (IsActive)
				throw new ServerException(ServerException.Codes.ServiceActive);
		}
		
		private void CheckActive()
		{
			if (!IsActive)
				throw new ServerException(ServerException.Codes.ServiceInactive);
		}
		
		private string FInstanceName;
		public string InstanceName
		{
			get { return FInstanceName; }
			set
			{
				CheckInactive();
				FInstanceName = value;
			}
		}
		
		public void Start()
		{
			if (!IsActive)
			{
				try
				{
					InstanceManager.Load();
					
					ServerConfiguration LInstance = InstanceManager.Instances[FInstanceName];
					if (LInstance == null)
					{
						// Establish a default configuration
						LInstance = ServerConfiguration.DefaultInstance(FInstanceName);
						InstanceManager.Instances.Add(LInstance);
						InstanceManager.Save();
					}
					
					FServer = new Server();
					LInstance.ApplyTo(FServer);
					FRemoteServer = new RemoteServer(FServer);
					FNativeServer = new NativeServer(FServer);
					FServer.Start();
					try
					{
						FServer.LogMessage("Creating WCF Service...");
						FService = new DataphorService(FRemoteServer);
						
						FServer.LogMessage("Creating Native CLI Service...");
						FNativeService = new NativeCLIService(FNativeServer);

						// TODO: Enable configuration of endpoints through instances or app.config files
						FServer.LogMessage("Configuring Service Host...");
						FServiceHost = new ServiceHost(FService);
						
						if (!LInstance.RequireSecureConnection)
							FServiceHost.AddServiceEndpoint
							(
								typeof(IDataphorService), 
								DataphorServiceUtility.GetBinding(false), 
								DataphorServiceUtility.BuildInstanceURI(Environment.MachineName, LInstance.PortNumber, false, LInstance.Name)
							);

						FServiceHost.AddServiceEndpoint
						(
							typeof(IDataphorService), 
							DataphorServiceUtility.GetBinding(true), 
							DataphorServiceUtility.BuildInstanceURI(Environment.MachineName, LInstance.SecurePortNumber, true, LInstance.Name)
						);

						FServer.LogMessage("Opening Service Host...");
						FServiceHost.Open();
						
						FServer.LogMessage("Configuring Native CLI Service Host...");
						FNativeServiceHost = new ServiceHost(FNativeService);
						
						if (!LInstance.RequireSecureConnection)
							FNativeServiceHost.AddServiceEndpoint
							(
								typeof(INativeCLIService),
								DataphorServiceUtility.GetBinding(false), 
								DataphorServiceUtility.BuildNativeInstanceURI(Environment.MachineName, LInstance.PortNumber, false, LInstance.Name)
							);
						
						FNativeServiceHost.AddServiceEndpoint
						(
							typeof(INativeCLIService),
							DataphorServiceUtility.GetBinding(true), 
							DataphorServiceUtility.BuildNativeInstanceURI(Environment.MachineName, LInstance.SecurePortNumber, true, LInstance.Name)
						);
					
						FServer.LogMessage("Opening Native CLI Service Host...");
						FNativeServiceHost.Open();
						
						// Start the listener
						if (LInstance.ShouldListen)
						{
							FServer.LogMessage("Starting Listener Service...");
							FListenerServiceHost = new ListenerServiceHost(LInstance.OverrideListenerPortNumber, LInstance.OverrideSecureListenerPortNumber, LInstance.RequireSecureListenerConnection, LInstance.AllowSilverlightClients);
						}
						
						// Start the CrossDomainServer
						// This is required in order to serve a ClientAccessPolicy to enable cross-domain access in a sliverlight application.
						// Without this, the Silverlight client will not work correctly.
						if (LInstance.AllowSilverlightClients)
						{
							FServer.LogMessage("Starting Cross Domain Service...");
							FCrossDomainServiceHost = new CrossDomainServiceHost(Environment.MachineName, LInstance.PortNumber, LInstance.SecurePortNumber, LInstance.RequireSecureConnection);
						}
					}
					catch (Exception LException)
					{
						FServer.LogError(LException);
						throw;
					}
				}
				catch
				{
					Stop();
					throw;
				}
			}
		}
		
		public void Stop()
		{
			if (FCrossDomainServiceHost != null)
			{
				FCrossDomainServiceHost.Dispose();
				FCrossDomainServiceHost = null;
			}
			
			if (FListenerServiceHost != null)
			{
				FListenerServiceHost.Dispose();
				FListenerServiceHost = null;
			}
			
			if (FNativeServiceHost != null)
			{
				if (FNativeServiceHost.State != CommunicationState.Faulted)
					FNativeServiceHost.BeginClose(null, null);
				FNativeServiceHost = null;
			}
			
			if (FServiceHost != null)
			{
				if (FServiceHost.State != CommunicationState.Faulted)
					FServiceHost.BeginClose(null, null);
				FServiceHost = null;
			}
			
			if (FService != null)
			{
				FService.Dispose();
				FService = null;
			}
			
			if (FRemoteServer != null)
			{
				FRemoteServer.Dispose();
				FRemoteServer = null;
			}
			
			if (FServer != null)
			{
				FServer.Stop();
				FServer = null;
			}
		}
	}
}
