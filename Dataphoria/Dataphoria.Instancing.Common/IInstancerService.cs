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

namespace Alphora.Dataphor.Dataphoria.Instancing.Common
{
    public interface IInstancerService
    {
        string[] EnumerateInstances();

        ServerConfiguration GetInstance(string instanceName);

        void CreateInstance(ServerConfiguration instance);

        void DeleteInstance(string instanceName);

        void StartInstance(string instanceName);

        void StopInstance(string instanceName);

        void KillInstance(string instanceName);

        ServerState GetInstanceStatus(string instanceName);
    }
}
