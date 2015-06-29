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
		private Server _server;
		/// <summary>
		/// Provides in-process access to the hosted server
		/// </summary>
		public IServer Server { get { return _server; } }
		
		private RemoteServer _remoteServer;
		private DataphorService _service;
		private ServiceHost _serviceHost;
		private NativeServer _nativeServer;
		private NativeCLIService _nativeService;
		private ServiceHost _nativeServiceHost;
		private ListenerServiceHost _listenerServiceHost;
				
		public bool IsActive { get { return _server != null; } }
		
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
		
		private string _instanceName;
		public string InstanceName
		{
			get { return _instanceName; }
			set
			{
				CheckInactive();
				_instanceName = value;
			}
		}
		
		public void Start()
		{
			if (!IsActive)
			{
				try
				{
					InstanceManager.Load();
					
					ServerConfiguration instance = InstanceManager.Instances[_instanceName];
					if (instance == null)
					{
						// Establish a default configuration
						instance = ServerConfiguration.DefaultInstance(_instanceName);
						InstanceManager.Instances.Add(instance);
						InstanceManager.Save();
					}
					
					_server = new Server();
					instance.ApplyTo(_server);
					_remoteServer = new RemoteServer(_server);
					_nativeServer = new NativeServer(_server);
					_server.Start();
					try
					{
						_server.LogMessage("Creating WCF Service...");
						_service = new DataphorService(_remoteServer);
						
						_server.LogMessage("Creating Native CLI Service...");
						_nativeService = new NativeCLIService(_nativeServer);

						_server.LogMessage("Configuring Service Host...");
						_serviceHost = instance.UseServiceConfiguration ? new CustomServiceHost(_service) : new ServiceHost(_service);

						if (!instance.UseServiceConfiguration)
						{						
							_serviceHost.AddServiceEndpoint
							(
								typeof(IDataphorService), 
								DataphorServiceUtility.GetBinding(), 
								DataphorServiceUtility.BuildInstanceURI(Environment.MachineName, instance.PortNumber, instance.Name)
							);
						}

						_server.LogMessage("Opening Service Host...");
						_serviceHost.Open();
						
						_server.LogMessage("Configuring Native CLI Service Host...");
						_nativeServiceHost = instance.UseServiceConfiguration ? new CustomServiceHost(_nativeService) : new ServiceHost(_nativeService);

						if (!instance.UseServiceConfiguration)
						{						
							_nativeServiceHost.AddServiceEndpoint
							(
								typeof(INativeCLIService),
								DataphorServiceUtility.GetBinding(), 
								DataphorServiceUtility.BuildNativeInstanceURI(Environment.MachineName, instance.PortNumber, instance.Name)
							);
						}
					
						_server.LogMessage("Opening Native CLI Service Host...");
						_nativeServiceHost.Open();
						
						// Start the listener
						if (instance.ShouldListen)
						{
							_server.LogMessage("Starting Listener Service...");
							_listenerServiceHost = new ListenerServiceHost(instance.OverrideListenerPortNumber, instance.RequireSecureListenerConnection, instance.AllowSilverlightClients, instance.UseServiceConfiguration);
						}							
					}
					catch (Exception exception)
					{
						_server.LogError(exception);
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
			if (_listenerServiceHost != null)
			{
				_listenerServiceHost.Dispose();
				_listenerServiceHost = null;
			}
			
			if (_nativeServiceHost != null)
			{
				if (_nativeServiceHost.State != CommunicationState.Faulted)
					_nativeServiceHost.BeginClose(null, null);
				_nativeServiceHost = null;
			}
			
			if (_serviceHost != null)
			{
				if (_serviceHost.State != CommunicationState.Faulted)
					_serviceHost.BeginClose(null, null);
				_serviceHost = null;
			}
			
			if (_service != null)
			{
				_service.Dispose();
				_service = null;
			}
			
			if (_remoteServer != null)
			{
				_remoteServer.Dispose();
				_remoteServer = null;
			}
			
			if (_server != null)
			{
				_server.Stop();
				_server = null;
			}
		}
	}
}
