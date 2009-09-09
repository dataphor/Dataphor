/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Debug
{
	public class DebugProcessInfo
	{
		public int ProcessID;
		public bool IsPaused;
		public DebugLocator Location;
		public bool DidBreak;
		public Exception Error;
	}
}
