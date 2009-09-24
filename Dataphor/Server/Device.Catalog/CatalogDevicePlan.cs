/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System.Text;
using System;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
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
