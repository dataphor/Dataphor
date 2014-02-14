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

namespace Alphora.Dataphor.Dataphoria.Instancing
{
    public class Instancer
    {
        public IEnumerable<string> EnumerateInstances()
        {
            throw new NotImplementedException();
        }

        public ServerConfiguration GetInstance(string instanceName)
        {
            throw new NotImplementedException();
        }

        public void CreateInstance(ServerConfiguration instance)
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

        public ServerState GetInstanceStatus(string instanceName)
        {
            throw new NotImplementedException();
        }
    }
}
