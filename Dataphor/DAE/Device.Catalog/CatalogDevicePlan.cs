/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define LOGDDLINSTRUCTIONS

using System;
using System.IO;
using System.Data;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Connection;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDevicePlan : DevicePlan
	{
		public CatalogDevicePlan(Plan APlan, CatalogDevice ADevice, PlanNode APlanNode) : base(APlan, ADevice, APlanNode) 
		{ 
			if (APlanNode.DeviceNode is CatalogDevicePlanNode)
				IsStorePlan = true;
		}
		
		public new CatalogDevice Device { get { return (CatalogDevice)base.Device; } }
		
		public Schema.TableVar TableContext;
		
		/// <summary>Indicates whether or not this plan is an expression against a store table.</summary>
		public bool IsStorePlan = false;
	}
	
	public class CatalogPlanParameter  : System.Object
	{
		public CatalogPlanParameter(SQLParameter ASQLParameter, PlanNode APlanNode) : base()
		{	
			FSQLParameter = ASQLParameter;
			FPlanNode = APlanNode; 
		}
		
		private SQLParameter FSQLParameter;
		public SQLParameter SQLParameter { get { return FSQLParameter; } }

		private PlanNode FPlanNode;
		public PlanNode PlanNode { get { return FPlanNode; } }
	}

	#if USETYPEDLIST
	public class CatalogPlanParameters : TypedList
	{
		public CatalogPlanParameters() : base(typeof(CatalogPlanParameter)){}
		
		public new CatalogPlanParameter this[int AIndex]
		{
			get { return (CatalogPlanParameter)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	#else
	public class CatalogPlanParameters : BaseList<CatalogPlanParameter>
	{
	#endif
	}
	
	public class CatalogDevicePlanNode : DevicePlanNode
	{
		public CatalogDevicePlanNode(PlanNode APlanNode) : base(APlanNode) {}
		
		public StringBuilder Statement = new StringBuilder();
		
		public StringBuilder WhereCondition = new StringBuilder();

		// Parameters
		private CatalogPlanParameters FPlanParameters = new CatalogPlanParameters();
		public CatalogPlanParameters PlanParameters { get { return FPlanParameters; } }
	}

	/// <summary>Stub class to indicate the device does support a given operator. Not actually used to implement the translation.</summary>
	public class CatalogDeviceOperator : DeviceOperator
	{
		public CatalogDeviceOperator(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
