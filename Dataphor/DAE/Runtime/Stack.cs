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
		public Stack(int AMaxStackDepth, int AMaxCallDepth) : base(AMaxStackDepth, AMaxCallDepth) { }

		public override void PushWindow(int ACount)
		{
			FWindows.Push(new RuntimeStackWindow(FCount - ACount));
		}
		
		public void PushWindow(int ACount, PlanNode AOriginator, DebugLocator ALocator)
		{
			FWindows.Push(new RuntimeStackWindow(FCount - ACount, AOriginator, ALocator));
		}
		
		protected object FErrorVar;
		public object ErrorVar
		{
			get { return FErrorVar; }
			set { FErrorVar = value; }
		}
	}
}

