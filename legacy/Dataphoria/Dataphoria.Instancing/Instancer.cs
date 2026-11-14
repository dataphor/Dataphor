/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Instancing
{
    /// <summary>
    /// The Instancer class performs the actual work of managing instances on the local machine.
    /// </summary>
    /// <remarks>
    /// This class is singleton and so manages thread-safety itself. This is to avoid any potential conflicts
    /// with managing the instance configuration file, which would otherwise present a single point of contention.
    /// </remarks>
	public class Instancer
	{
        public Instancer()
        {
            lock (_syncHandle)
            {
                InstanceManager.Load();
            }
        }

		private string _binDirectory;
		public string BinDirectory 
		{ 
			get { return _binDirectory; }
			set { _binDirectory = value; }
		}

		private string _instanceDirectory;
		public string InstanceDirectory
		{
			get { return _instanceDirectory; }
			set { _instanceDirectory = value; }
		}

        private object _syncHandle = new Object();

		private ServerAlias GetServerAlias(string instanceName)
		{
            lock (_syncHandle)
            {
			    var instance = InstanceManager.Instances[instanceName];

			    if (instance == null)
			    {
				    throw new InvalidOperationException(String.Format("Instance {0} does not exist.", instanceName));
			    }

			    return GetServerAlias(instanceName, instance.PortNumber);
            }
		}

		private ServerAlias GetServerAlias(string instanceName, int portNumber)
		{
			var alias = new ConnectionAlias();

			alias.HostName = "localhost";
			alias.InstanceName = instanceName;
			alias.OverridePortNumber = portNumber;
			// TODO: User credentials for instances...
			alias.SessionInfo.UserID = "Admin";

			return alias;
		}

		public IEnumerable<string> EnumerateInstances()
		{
            lock (_syncHandle)
            {
			    string[] result = new string[InstanceManager.Instances.Count];
			    for (int index = 0; index < InstanceManager.Instances.Count; index++)
				    result[index] = InstanceManager.Instances[index].Name;
			    return result;
            }
		}

		public InstanceDescriptor GetInstance(string instanceName)
		{
            lock (_syncHandle)
            {
			    var serverConfiguration = InstanceManager.Instances[instanceName];
			    return new InstanceDescriptor { Name = serverConfiguration.Name, PortNumber = serverConfiguration.PortNumber };
            }
		}

		public void CreateInstance(InstanceDescriptor instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}

            lock (_syncHandle)
            {
			    // Verify instance name is unique
			    var existingInstance = InstanceManager.Instances[instance.Name];
			    if (existingInstance != null)
			    {
				    throw new InvalidOperationException(String.Format("An instance named {0} already exists on this node.", instance.Name));
			    }

			    // TODO: Verify port number is not in use

			    // Establish server configuration based on instance descriptor
			    var serverConfig = new ServerConfiguration();

			    serverConfig.Name = instance.Name;
			    serverConfig.PortNumber = instance.PortNumber;
			    serverConfig.ShouldListen = false;

			    // TODO: Catalog class name
			    // TODO: Initial device script

			    // Save instance configuration
			    InstanceManager.Instances.Add(serverConfig);
                InstanceManager.Save();
            }
		}

		public void DeleteInstance(string instanceName)
		{
            lock (_syncHandle)
            {
			    // Verify instance exists
			    var serverConfig = InstanceManager.Instances[instanceName];
			    if (serverConfig == null)
			    {
				    throw new InvalidOperationException(String.Format("An instance named {0} does not exist on this node.", instanceName));
			    }

			    // Delete instance directory
			    var instanceDirectory = Path.Combine(InstanceDirectory, instanceName);
			    if (Directory.Exists(instanceDirectory))
			    {
				    Directory.Delete(instanceDirectory, true);
			    }

			    // Remove instance configuration
			    InstanceManager.Instances.Remove(instanceName);
                InstanceManager.Save();
            }
		}

		public int StartInstance(string instanceName)
		{
			// Start a DAEServer process with the given instance name
			var si = new ProcessStartInfo(Path.Combine(BinDirectory, "DAEServer.exe"), String.Format("-name {0}", instanceName));
			var p = Process.Start(si);

			// Return the process Id
			return p.Id;
		}

		public void StopInstance(string instanceName)
		{
			// Issue the stop command to the server
			using (var connection = new ServerConnection(GetServerAlias(instanceName)))
			{
				connection.Server.Stop();
			}
		}

		public void KillInstance(string instanceName, int processId)
		{
			// Kill the process id
			var p = Process.GetProcessById(processId);

			p.Kill();
			p.WaitForExit(30000);
		}

		private InstanceState ServerStateToInstanceState(ServerState state)
		{
			switch (state)
			{
				case ServerState.Starting : return InstanceState.Starting;
				case ServerState.Started : return InstanceState.Started;
				case ServerState.Stopping : return InstanceState.Stopping;
				case ServerState.Stopped : return InstanceState.Stopped;
				default : return InstanceState.Unknown;
			}
		}

		public InstanceState GetInstanceStatus(string instanceName)
		{
			// Connect to the given instance
			try
			{
				using (var connection = new ServerConnection(GetServerAlias(instanceName)))
				{
					return ServerStateToInstanceState(connection.Server.State);
				}
			}
			catch
			{
				// TODO: Better error management and reporting
				return InstanceState.Unresponsive;
			}
		}
	}
}
