/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Instancing.Common
{
	[ServiceContract(Name = "IInstancerService", Namespace = "http://dataphor.org/dataphor/4.0/")]
    public interface IInstancerService
    {
        [OperationContract]
        string[] EnumerateInstances();

        [OperationContract]
        InstanceDescriptor GetInstance(string instanceName);

        [OperationContract]
        void CreateInstance(InstanceDescriptor instance);

        [OperationContract]
        void DeleteInstance(string instanceName);

        [OperationContract]
        int StartInstance(string instanceName);

        [OperationContract]
        void StopInstance(string instanceName);

        [OperationContract]
        void KillInstance(string instanceName, int processId);

        [OperationContract]
        InstanceState GetInstanceStatus(string instanceName);
    }
}
