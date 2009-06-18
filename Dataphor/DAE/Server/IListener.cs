/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Server
{
	public interface IListener
	{
		string[] EnumerateInstances();
		string GetInstanceURI(string AInstanceName);
	}
}
