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

	public class DataphorServiceHost
	{
		private Server FServer;
		private ServerHost FServerHost;
		private DataphorService FService;
		private ServiceHost FServiceHost;
		
		public bool IsActive { get { return FServer != null; } }
		
		private void CheckInactive()
		{
			if (IsActive)
				throw new ServerException(ServerException.Codes.ServiceActive);
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
					FServerHost = new ServerHost(FServer, LInstance.PortNumber);
					FServer.Start();
					FService = new DataphorService(FServerHost.RemoteServer);

					// TODO: Enable configuration of endpoints through instances or app.config files
					FServiceHost = new ServiceHost(FService);
					
					FServiceHost.AddServiceEndpoint
					(
						typeof(IDataphorService), 
						new CustomBinding(new BinaryMessageEncodingBindingElement(), new HttpTransportBindingElement()), 
						DataphorServiceUtility.BuildURI(Environment.MachineName, LInstance.PortNumber, LInstance.Name)
					);
					FServiceHost.Open();
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
			if (FServiceHost != null)
			{
				FServiceHost.BeginClose(null, null);
				FServiceHost = null;
			}
			
			if (FService != null)
			{
				//FService.Dispose(); // TODO: Service lifetime management
				FService = null;
			}
			
			if (FServerHost != null)
			{
				FServerHost.Dispose();
				FServerHost = null;
			}
			
			if (FServer != null)
			{
				FServer.Stop();
				FServer = null;
			}
		}
	}
}
