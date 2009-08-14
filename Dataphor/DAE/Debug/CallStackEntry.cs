using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Debug
{
	public class CallStackEntry
	{
		public CallStackEntry(int AIndex, string ADescription)
		{
			FIndex = AIndex;
			FDescription = ADescription;
		}
		
		private int FIndex;
		public int Index { get { return FIndex; } }
		
		private string FDescription;
		public string Description { get { return FDescription; } }

		public override string ToString()
		{
			return String.Format("{0}:{1}", FIndex, FDescription);
		}
	}
	
	public class CallStack : List<CallStackEntry> { }
}
