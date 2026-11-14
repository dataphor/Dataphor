/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDevicePlan : DevicePlan
	{
		public CatalogDevicePlan(Plan plan, CatalogDevice device, PlanNode planNode) : base(plan, device, planNode) 
		{ 
		}
		
		public new CatalogDevice Device { get { return (CatalogDevice)base.Device; } }
		
		public Schema.TableVar TableContext;
	}
}
