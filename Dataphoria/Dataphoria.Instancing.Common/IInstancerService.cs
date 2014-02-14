/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Instancing.Common
{
    public interface IInstancerService
    {
        string[] EnumerateInstances();

        InstanceDescriptor GetInstance(string instanceName);

        void CreateInstance(InstanceDescriptor instance);

        void DeleteInstance(string instanceName);

        void StartInstance(string instanceName);

        void StopInstance(string instanceName);

        void KillInstance(string instanceName);

        InstanceState GetInstanceStatus(string instanceName);
    }
}
