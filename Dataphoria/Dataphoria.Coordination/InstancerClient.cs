/*
	Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Coordination
{
	public class InstancerClient : DataphorServiceClient<IInstancerService>, IInstancerService
	{
		public InstancerClient(string hostName) : base(DataphorServiceUtility.BuildInstanceURI(hostName, Constants.InstancerPortNumber, Constants.InstancerName)) 
		{ 
			_hostName = hostName;
		}
		
		private string _hostName;
		public string HostName { get { return _hostName; } }

		#region IInstancerService Members

		public InstanceDescriptor GetInstance(string instanceName)
		{
			return GetInterface().GetInstance(instanceName);
		}

		public void CreateInstance(InstanceDescriptor instance)
		{
			GetInterface().CreateInstance(instance);
		}

		public void DeleteInstance(string instanceName)
		{
			GetInterface().DeleteInstance(instanceName);
		}

		public int StartInstance(string instanceName)
		{
			return GetInterface().StartInstance(instanceName);
		}

		public void StopInstance(string instanceName)
		{
			GetInterface().StopInstance(instanceName);
		}

		public void KillInstance(string instanceName, int processId)
		{
			GetInterface().KillInstance(instanceName, processId);
		}

		public InstanceState GetInstanceStatus(string instanceName)
		{
			return GetInterface().GetInstanceStatus(instanceName);
		}

        public string[] EnumerateInstances()
        {
            return GetInterface().EnumerateInstances();
        }
		#endregion

    }
}
