using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.ServerTests.Utilities
{
	interface ServerConfigurationManager
	{
		ServerConfiguration GetTestConfiguration(string AInstanceName);
		void ResetInstance();
		Server.Server GetServer();
	}
}
