/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Compiling.Visitors
{
	public class NormalizeRestrictionVisitor : PlanNodeVisitor
	{
		public override void PreOrderVisit(Plan plan, PlanNode node)
		{
			// If the node is a restriction
			// Convert the condition to DNF
			// Collect Ors
			// If there are any Ors, replace the entire restriction with the Union of each individual Or
			// Collect Ands
			// If there are any non-sargable conditions
			// Add an additional restriction with the non-sargable conditions
		}

		public override void PostOrderVisit(Plan plan, PlanNode node)
		{
		}
	}
}
