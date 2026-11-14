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
		public DebugContext(string script, string locator, int line, int linePos) : base()
		{
			_script = script;
			_locator = locator;
			_line = line;
			_linePos = linePos;
		}
		
		private string _script;
		public string Script { get { return _script; } }
		
		private string _locator;
		public string Locator { get { return _locator; } }
		
		private int _line;
		public int Line { get { return _line; } }
		
		private int _linePos;
		public int LinePos { get { return _linePos; } }
	}
	
	public class DebugContexts : Stack<DebugContext> { }
}
