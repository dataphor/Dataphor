/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Server.Tests.Utilities
{
	using Alphora.Dataphor.DAE.Server;
	
	public interface ServerConfigurationManager
	{
		ServerConfiguration GetTestConfiguration(string AInstanceName);
		void ResetInstance();
		Server GetServer();
	}
}
