/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.Dataphoria.Instancer;
using Alphora.Dataphor.Dataphoria.Instancing;
using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Instancer
{
    public partial class Service : ServiceBase
    {
        private ServiceHost _host;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var service = new InstancerService();
            service.BinDirectory = ServiceSettings.Default.BinDirectory;
            service.InstanceDirectory = ServiceSettings.Default.InstanceDirectory;
            _host = new ServiceHost(service);
		    _host.AddServiceEndpoint
		    (
			    typeof(IInstancerService), 
			    DataphorServiceUtility.GetBinding(), 
			    DataphorServiceUtility.BuildInstanceURI(Environment.MachineName, Constants.InstancerPortNumber, Constants.InstancerName)
		    );

			_host.Open();
        }

        protected override void OnStop()
        {
            _host.Close(TimeSpan.FromSeconds(30));
        }
    }
}
