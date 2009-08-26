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

	public class RuntimeStackWindow : StackWindow
	{
		public RuntimeStackWindow(int ABase) : base(ABase) { }
		public RuntimeStackWindow(int ABase, PlanNode AOriginator, DebugLocator ALocator) : base(ABase)
		{
			Originator = AOriginator;
			Locator = ALocator;
		}
		
		public PlanNode Originator;
		public DebugLocator Locator;
	}
}
