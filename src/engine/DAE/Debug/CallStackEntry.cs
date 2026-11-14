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
		public CallStackEntry(int index, string description, DebugLocator locator, string location, string statement)
		{
			_index = index;
			_description = description;
			_locator = locator;
			_location = location;
			_statement = statement;
		}
		
		private int _index;
		public int Index { get { return _index; } }
		
		private string _description;
		public string Description { get { return _description; } }
		
		private DebugLocator _locator;
		public DebugLocator Locator { get { return _locator; } }
		
		private string _location;
		public string Location { get { return _location; } }
		
		private string _statement;
		public string Statement { get { return _statement; } }

		public override string ToString()
		{
			return String.Format("{0}:{1}", _index, _description);
		}
	}
	
	public class CallStack : List<CallStackEntry> { }
}
