using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Debug
{
	public class Breakpoint
	{
		public Breakpoint(string ALocator, int ALine, int ALinePos)
		{
			FLocator = ALocator;
			FLine = ALine;
			FLinePos = ALinePos;
		}
		
		private string FLocator;
		public string Locator { get { return FLocator; } }
		
		private int FLine;
		public int Line { get { return FLine; } }
		
		private int FLinePos;
		public int LinePos { get { return FLinePos; } }

		public override int GetHashCode()
		{
			return FLocator.GetHashCode() ^ FLine.GetHashCode() ^ FLinePos.GetHashCode();
		}

		public override bool Equals(object AObject)
		{
			Breakpoint LBreakpoint = AObject as Breakpoint;
			return (LBreakpoint != null) && (FLocator == LBreakpoint.Locator) && (FLine == LBreakpoint.Line) && (FLinePos == LBreakpoint.LinePos);
		}

		public override string ToString()
		{
			return String.Format("in {0} at {1}:{2}.", FLocator, FLine, FLinePos);
		}
	}
	
	public class Breakpoints : List<Breakpoint> { }
}
