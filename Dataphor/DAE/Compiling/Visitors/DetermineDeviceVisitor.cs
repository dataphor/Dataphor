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
	public class DetermineDeviceVisitor : PlanNodeVisitor
	{
		//private PlanNode chunk;

		public override void PreOrderVisit(Plan plan, PlanNode node)
		{
			//if (chunk == null)
			//{
			//	node.DetermineDevice(plan);
			//	if (node.DeviceSupported)
			//	{
			//		chunk = node;
			//	}
			//}
		}

		public override void PostOrderVisit(Plan plan, PlanNode node)
		{
			node.DetermineDevice(plan);

			if (node.DeviceSupported)
			{
				// Clear the device plan node from all subnodes
				node.ClearDeviceSubNodes();
			}

			//if (chunk != null)
			//{
			//	if (chunk != node)
			//	{
			//		node.SetDevice(plan, chunk.Device);
			//	}
			//	else
			//	{
			//		chunk = null;
			//	}
			//}
		}
	}
}
