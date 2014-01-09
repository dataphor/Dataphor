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
	public class PlanNodeVisitor
	{
		#if USEVISIT
		public virtual PlanNode Visit(Plan plan, PlanNode node)
		{
			node.BindingTraversal(plan, this);
			return node;
		}
		#endif

		public virtual void PreOrderVisit(Plan plan, PlanNode node)
		{
			// base implementation does nothing
		}

		public virtual void PostOrderVisit(Plan plan, PlanNode node)
		{
			// base implementation does nothing
		}
	}

}
