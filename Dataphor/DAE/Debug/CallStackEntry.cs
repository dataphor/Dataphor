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
	public class CallStackEntry
	{
		public CallStackEntry(int AIndex, string ADescription, DebugLocator ALocator)
		{
			FIndex = AIndex;
			FDescription = ADescription;
			FLocation = ALocator;
		}
		
		private int FIndex;
		public int Index { get { return FIndex; } }
		
		private string FDescription;
		public string Description { get { return FDescription; } }
		
		private DebugLocator FLocation;
		public DebugLocator Location { get { return FLocation; } }

		public override string ToString()
		{
			return String.Format("{0}:{1}", FIndex, FDescription);
		}
	}
	
	public class CallStack : List<CallStackEntry> { }
}
