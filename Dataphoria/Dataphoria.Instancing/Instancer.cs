/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Instancing
{
	public class Instancer
	{
		public IEnumerable<string> EnumerateInstances()
		{
			InstanceConfiguration configuration = InstanceManager.LoadConfiguration();
			string[] result = new string[configuration.Instances.Count];
			for (int index = 0; index < configuration.Instances.Count; index++)
				result[index] = configuration.Instances[index].Name;
			return result;
		}

		public InstanceDescriptor GetInstance(string instanceName)
		{
			throw new NotImplementedException();
		}

		public void CreateInstance(InstanceDescriptor instance)
		{
			throw new NotImplementedException();
		}

		public void DeleteInstance(string instanceName)
		{
			throw new NotImplementedException();
		}

		public void StartInstance(string instanceName)
		{
			throw new NotImplementedException();
		}

		public void StopInstance(string instanceName)
		{
			throw new NotImplementedException();
		}

		public void KillInstance(string instanceName)
		{
			throw new NotImplementedException();
		}

		public InstanceState GetInstanceStatus(string instanceName)
		{
			throw new NotImplementedException();
		}
	}
}
