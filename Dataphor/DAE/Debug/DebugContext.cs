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
	public class DebugContext
	{
		public DebugContext(string AScript, string ALocator, int ALine, int ALinePos) : base()
		{
			FScript = AScript;
			FLocator = ALocator;
			FLine = ALine;
			FLinePos = ALinePos;
		}
		
		private string FScript;
		public string Script { get { return FScript; } }
		
		private string FLocator;
		public string Locator { get { return FLocator; } }
		
		private int FLine;
		public int Line { get { return FLine; } }
		
		private int FLinePos;
		public int LinePos { get { return FLinePos; } }
	}
	
	public class DebugContexts : Stack<DebugContext> { }
}
