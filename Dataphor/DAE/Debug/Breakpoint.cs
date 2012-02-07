using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Debug
{
	public class Breakpoint
	{
		public Breakpoint(string locator, int line, int linePos)
		{
			_locator = locator;
			_line = line;
			_linePos = linePos;
		}
		
		private string _locator;
		public string Locator { get { return _locator; } }
		
		private int _line;
		public int Line { get { return _line; } }
		
		private int _linePos;
		public int LinePos { get { return _linePos; } }

		public override int GetHashCode()
		{
			return _locator.GetHashCode() ^ _line.GetHashCode() ^ _linePos.GetHashCode();
		}

		public override bool Equals(object objectValue)
		{
			Breakpoint breakpoint = objectValue as Breakpoint;
			return (breakpoint != null) && (_locator == breakpoint.Locator) && (_line == breakpoint.Line) && (_linePos == breakpoint.LinePos);
		}

		public override string ToString()
		{
			return String.Format("in {0} at {1}:{2}.", _locator, _line, _linePos);
		}
	}
	
	public class Breakpoints : List<Breakpoint> { }
}
