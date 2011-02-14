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
		public RuntimeStackWindow(int baseValue) : base(baseValue) { }
		public RuntimeStackWindow(int baseValue, PlanNode originator, DebugLocator locator) : base(baseValue)
		{
			Originator = originator;
			Locator = locator;
		}
		
		public PlanNode Originator;
		public DebugLocator Locator;

		public Schema.Operator GetOriginatingOperator()
		{
			InstructionNodeBase instructionNodeBase = Originator as InstructionNodeBase;
			if (instructionNodeBase != null)
				return instructionNodeBase.Operator;
			
			return ((AggregateCallNode)Originator).Operator;	
		}
	}
}
