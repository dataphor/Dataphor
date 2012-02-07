/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public class Stack : Stack<object>
	{
		public Stack() : base() { }
		public Stack(int maxStackDepth, int maxCallDepth) : base(maxStackDepth, maxCallDepth) { }

		public override void PushWindow(int count)
		{
			_windows.Push(new RuntimeStackWindow(_count - count));
		}
		
		public void PushWindow(int count, PlanNode originator, DebugLocator locator)
		{
			_windows.Push(new RuntimeStackWindow(_count - count, originator, locator));
		}
		
		protected object _errorVar;
		public object ErrorVar
		{
			get { return _errorVar; }
			set { _errorVar = value; }
		}
	}
}

