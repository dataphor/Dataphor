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
	public class DetermineAccessPathVisitor : PlanNodeVisitor
	{
		public override void PostOrderVisit(Plan plan, PlanNode node)
		{
			node.DetermineAccessPath(plan);
		}
	}
}
