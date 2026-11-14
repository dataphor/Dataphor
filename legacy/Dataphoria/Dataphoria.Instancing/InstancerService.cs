/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Instancing
{
	public class InstancerService : IInstancerService
	{
		public InstancerService()
		{
			_instancer = new Instancer();

			// TODO: Set BinDirectory and InstanceDirectory
		}

		private Instancer _instancer;

        public string BinDirectory 
        {
            get { return _instancer.BinDirectory; }
            set { _instancer.BinDirectory = value; }
        }

        public string InstanceDirectory
        {
            get { return _instancer.InstanceDirectory; }
            set { _instancer.InstanceDirectory = value; }
        }

		#region IInstancerService Members

		public string[] EnumerateInstances()
		{
			return _instancer.EnumerateInstances().ToArray();
		}

		public InstanceDescriptor GetInstance(string instanceName)
		{
			return _instancer.GetInstance(instanceName);
		}

		public void CreateInstance(InstanceDescriptor instance)
		{
			_instancer.CreateInstance(instance);
		}

		public void DeleteInstance(string instanceName)
		{
			_instancer.DeleteInstance(instanceName);
		}

		public int StartInstance(string instanceName)
		{
			return _instancer.StartInstance(instanceName);
		}

		public void StopInstance(string instanceName)
		{
			_instancer.StopInstance(instanceName);
		}

		public void KillInstance(string instanceName, int processId)
		{
			_instancer.KillInstance(instanceName, processId);
		}

		public InstanceState GetInstanceStatus(string instanceName)
		{
			return _instancer.GetInstanceStatus(instanceName);
		}

		#endregion
	}
}
