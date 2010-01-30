/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define USENATIVECONCURRENCYCOMPARE
#define REMOVESUPERKEYS
#define WRAPRUNTIMEEXCEPTIONS // Determines whether or not runtime exceptions are wrapped
//#define TRACKCALLDEPTH // Determines whether or not call depth tracking is enabled
#define USENAMEDROWVARIABLES

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Threading;
using System.Reflection;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	/*
		Modification Execution ->
			Before
				BeforeEvents
				Overridable Stub
				Validation
				Events
			Execute
				if FModifySupported
					BeforeExecuteDevice Stub - use to propogate the validation to child nodes
					ExecuteDevice
					AfterExecuteDevice Stub - use to propogate the validation to child nodes
				else
					InternalExecute - required override
			After
				Events
				Catalog Constraints
				Overridable Stub
				AfterEvents
				
			BeforeValidate
				Before
				children.BeforeValidate
				
			AfterValidate
				After
				children.AfterValidate
				
		TableType validation ->
			Columns
				DataType
					Constraints
			Keys
			Constraints
			
		Implementation Overrides ->
			InternalDefault(Row ARow, string AColumnName, Plan APlan, bool AIsDescending)
				Optional override to implement additional default behavior specific to the node.
				Called by the Default method of the IProposable interface.
				The given stack is empty as far as this method is concerned.
				AIsDescending will be true if this is a CLI proposable call and should propogate
				down the node tree.
				
			InternalValidate(Row AOldRow, Row ANewRow, string AColumnName, Plan APlan, bool AIsDescending)
				Optional override to implement additional validation behavior specific to the node.
				Called for all modifications, as well as by the Validate method of the IProposable interface.
				The given stack will contain the row being validated as the top item.
				AIsDescending will be true if this is a CLI proposable call and should propogate
				down the node tree.
				
			InternalChange(Row AOldRow, Row ANewRow, string AColumnName, Plan APlan)
				Optional override to implement additional change behavior specific to the node.
				Called by the Change method of the IProposable interface.
				The given stack will contain the row being changed as the top item.
				
			InternalBeforeInsert(Row ARow, Plan APlan)
				Optional override to implement additional validation behavior specific to the node.
				Called for insert modifications only.
				The given stack will contain the row being inserted as the top item.
				
			InternalAfterInsert(Row ARow, Plan APlan)
				Optional override to implement additional validation behavior specific to the node.
				Called for insert modifications only.
				The given stack will contain the row that was inserted as the top item.
				
			InternalBeforeUpdate(Row AOldRow, Row ANewRow, Plan APlan)
				Optional override to implement additional validation behavior specific to the node.
				Called for update modifications only.
				The given stack will contain the old row and new row being updated.
				
			InternalAfterUpdate(Row AOldRow, Row ANewRow, Plan APlan)
				Optional override to implement additional validation behavior specific to the node.
				Called for update modifications only.
				The given stack will contain the old row and new row that was updated.
				
			InternalBeforeDelete(Row ARow, Plan APlan)
				Optional override to implement additional validation behavior specific to the node.
				Called for delete modifications only.
				The given stack will contain the row being deleted.
				
			InternalAfterDelete(Row ARow, Plan APlan)
				Optional override to implement additional validation behavior specific to the node.
				Called for delete modifications only.
				The given stack will contain the row that was deleted.
				
			InternalExecuteInsert(Row ARow, Plan APlan)
				Required override to implement an insert on the node if the modification 
				is not supported by the device.  Called for Insert modifications only.
				The given stack will contain the row to be inserted.
				
			InternalExecuteUpdate(Row AOldRow, Row ANewRow, Plan APlan)
				Required override to implement an update on the node if the modification 
				is not supported by the device.  Called for Update modifications only.
				The given stack will contain the old row and new row to be updated.
				
			InternalExecuteDelete(Row ARow, Plan APlan)
				Required override to implement a delete on the node if the modification 
				is not supported by the device.  Called for Delete modifications only.
				The given stack will contain the row to be deleted.
				
			InternalBeforeDeviceInsert(Row ARow, Plan APlan)
				Required override to implement validation propogation for an insert on the node 
				if the modification is supported by the device.  Called for insert modifications only.
				The given stack will contain the row being inserted.
				
			InternalAfterDeviceInsert(Row ARow, Plan APlan)
				Required override to implement validation propogation for an insert on the node 
				if the modification is supported by the device.  Called for insert modifications only.
				The given stack will contain the row that was inserted.
				
			InternalPrepareDeviceInsert(Row ARow, Plan APlan)
				Required override to prepare the stack for insert and validation propogations 
				when a modification is supported by the device.  Called for insert modifications only.
				The method should push a row of the appropriate type on the stack.
				
			InternalUnprepareDeviceInsert(Row ARow, Plan APlan)
				Required override to clear the stack for insert and validation propogations
				when a modification is supported by the device.  Called for insert modifications only.
				The method should pop a row off the stack.
				
			InternalBeforeDeviceUpdate(Row AOldRow, Row ANewRow, Plan APlan)
				Required override to implement validation propogation for an update on the node 
				if the modification is supported by the device.  Called for update modifications only.
				The given stack will contain the old row and new row being updated.
				
			InternalAfterDeviceUpdate(Row AOldRow, Row ANewRow, Plan APlan)
				Required override to implement validation propogation for an update on the node 
				if the modification is supported by the device.  Called for update modifications only.
				The given stack will contain the old row and new row that was updated.
				
			InternalPrepareDeviceUpdate(Row AOldRow, Row ANewRow, Plan APlan)
				Required override to prepare the stack for update and validation propogations
				when the modification is supported by the device.  Called for update modifications only.
				The method should push two rows of the appropriate type on to the given stack.
				
			InternalUnprepareDeviceUpdate(Plan APlan)
				Required override to clear the stack for update and validation propogations
				when the modification is supported by the device.  Called for update modifications only.
				The method should pop two rows.
				
			InternalBeforeDeviceDelete(Row ARow, Plan APlan)
				Required override to implement validation propogation for a delete on the node 
				if the modification is supported by the device.  Called for delete modifications only.
				The given stack will contain the row being deleted.

			InternalAfterDeviceDelete(Row ARow, Plan APlan)
				Required override to implement validation propogation for a delete on the node 
				if the modification is supported by the device.  Called for delete modifications only.
				The given stack will contain the row that was deleted.
				
			InternalPrepareDeviceDelete(Row ARow, Plan APlan)
				Required override to prepare the stack for delete and validation propogations
				when the modification is supported by the device.  Called for delete modifications only.
				The method should push a row of the appropriate type on the given stack.
				
			InternalUnprepareDeviceDelete(Plan APlan)
				Required override to clean up the stack during delete and validation propogations
				when the modification is supported by the device.  Called for delete modifications only.
				The method should pop a row off the given stack.
	*/
	
	public enum PropagateAction 
	{ 
		///<summary>Modification propagation is determined by the node.</summary>
		True, 
		
		///<summary>Modification will not be propagated.</summary>
		False, 

		///<summary>Modification will be propagated as an update if the row exists for an insert, and as an insert if the row exists for an update.</summary>
		Ensure, 
		
		///<summary>Modification will be propogated only if the row does not exist for an insert.</summary>
		Ignore 
	}

	public abstract class TableNode : InstructionNodeBase
	{        
		// constructor
		public TableNode() : base() 
		{
			IsBreakable = false; // TODO: Debug table nodes? 
		}
		
		protected Schema.TableVarColumn CopyTableVarColumn(Schema.TableVarColumn AColumn)
		{
			return AColumn.Inherit();
		}
		
		protected Schema.TableVarColumn CopyTableVarColumn(Schema.TableVarColumn AColumn, string AColumnName)
		{
			return AColumn.InheritAndRename(AColumnName);
		}
		
		protected void CopyTableVarColumns(Schema.TableVarColumns AColumns)
		{
			CopyTableVarColumns(AColumns, false);
		}
		
        protected void CopyTableVarColumns(Schema.TableVarColumns AColumns, bool AIsNilable)
        {
			// Columns
			Schema.TableVarColumn LNewTableVarColumn;
			foreach (Schema.TableVarColumn LTableVarColumn in AColumns)
			{
				LNewTableVarColumn = CopyTableVarColumn(LTableVarColumn);
				LNewTableVarColumn.IsNilable = LNewTableVarColumn.IsNilable || AIsNilable;
				DataType.Columns.Add(LNewTableVarColumn.Column);
				TableVar.Columns.Add(LNewTableVarColumn);
			}
		}
		
		protected Schema.Key CopyKey(Schema.Key AKey, bool AIsSparse)	  
		{
			Schema.Key LKey = new Schema.Key();
			LKey.InheritMetaData(AKey.MetaData);
			LKey.IsInherited = true;
			LKey.IsSparse = AKey.IsSparse || AIsSparse;
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
				LKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
			return LKey;
		}
        
		protected Schema.Key CopyKey(Schema.Key AKey)
		{
			return CopyKey(AKey, false);
		}
		
        protected void CopyKeys(Schema.Keys AKeys, bool AIsSparse)
        {
            foreach (Schema.Key LKey in AKeys)
				if (!(TableVar.Keys.Contains(LKey)))
					TableVar.Keys.Add(CopyKey(LKey, AIsSparse));
        }
        
        protected void CopyKeys(Schema.Keys AKeys)
        {
			CopyKeys(AKeys, false);
        }
        
        protected void CopyPreservedKeys(Schema.Keys AKeys, bool AIsSparse, bool APreserveSparse)
        {
			bool LAddKey;
			foreach (Schema.Key LKey in AKeys)
			{
				if (APreserveSparse || !LKey.IsSparse)
				{
					LAddKey = true;
					foreach (Schema.TableVarColumn LKeyColumn in LKey.Columns)
					{
						LAddKey = TableVar.Columns.ContainsName(LKeyColumn.Name);
						if (!LAddKey)
							break;
					}
					if (LAddKey && !TableVar.Keys.Contains(LKey))
						TableVar.Keys.Add(CopyKey(LKey, AIsSparse));
				}
			}
        }
        
        protected void CopyPreservedKeys(Schema.Keys AKeys)
        {
			CopyPreservedKeys(AKeys, false, true);
        }
        
        protected void RemoveSuperKeys()
        {
			#if REMOVESUPERKEYS
			// Remove super keys
			// BTR 10/4/2006 ->	I am a little concerned that this change will negatively impact elaboration, derivation, and
			// A/T enlistment so I've included it with a define.
			int LIndex = FTableVar.Keys.Count - 1;
			while (LIndex >= 0)
			{
				Schema.Key LCurrentKey = FTableVar.Keys[LIndex];
				int LKeyIndex = 0;
				while (LKeyIndex < LIndex)
				{
					if (LCurrentKey.Columns.IsSupersetOf(FTableVar.Keys[LKeyIndex].Columns) && (LCurrentKey.IsSparse || (!LCurrentKey.IsSparse && !FTableVar.Keys[LKeyIndex].IsSparse)))
					{
						// if LCurrentKey is sparse, then FTableVar.Keys[LKeyIndex] may or may not be sparse
						// if LCurrentKey is not sparse, FTableVar.Keys[LKeyIndex] must be non-sparse
						FTableVar.Keys.RemoveAt(LIndex);
						break;
					}
					else if (FTableVar.Keys[LKeyIndex].Columns.IsSupersetOf(LCurrentKey.Columns) && (FTableVar.Keys[LKeyIndex].IsSparse || (!FTableVar.Keys[LKeyIndex].IsSparse && !LCurrentKey.IsSparse)))
					{
						// if FTableVar.Keys[LKeyIndex] is sparse, then LCurrentKey may or may not be sparse
						// if FTableVar.Keys[LKeyIndex] is not sparse, FTableVar.Keys[LKeyIndex] must be non-sparse
						FTableVar.Keys.RemoveAt(LKeyIndex);
						LIndex--;
						continue;
					}
					
					LKeyIndex++;
				}
				
				LIndex--;
			}
			#else			
			// Remove duplicate keys
			for (int LIndex = FTableVar.Keys.Count - 1; LIndex >= 0; LIndex--)
			{
				Schema.Key LCurrentKey = FTableVar.Keys[LIndex];
				for (int LKeyIndex = 0; LKeyIndex < LIndex; LKeyIndex++)
					if (FTableVar.Keys[LKeyIndex].Equals(LCurrentKey))
					{
						FTableVar.Keys.RemoveAt(LIndex);
						break;
					}
			}
			#endif
        }
        
        protected Schema.Order CopyOrder(Schema.Order AOrder)
        {
			Schema.Order LOrder = new Schema.Order();
			LOrder.InheritMetaData(AOrder.MetaData);
			LOrder.IsInherited = true;
			Schema.OrderColumn LNewOrderColumn;
			Schema.OrderColumn LOrderColumn;
			for (int LIndex = 0; LIndex < AOrder.Columns.Count; LIndex++)
			{
				LOrderColumn = AOrder.Columns[LIndex];
				LNewOrderColumn = new Schema.OrderColumn(TableVar.Columns[LOrderColumn.Column], LOrderColumn.Ascending, LOrderColumn.IncludeNils);
				LNewOrderColumn.Sort = LOrderColumn.Sort;
				LNewOrderColumn.IsDefaultSort = LOrderColumn.IsDefaultSort;
				Error.AssertWarn(LNewOrderColumn.Sort != null, "Sort is null");
				LOrder.Columns.Add(LNewOrderColumn);
			}
			return LOrder;
		}
		
        protected void CopyOrders(Schema.Orders AOrders)
        {
			foreach (Schema.Order LOrder in AOrders)
				TableVar.Orders.Add(CopyOrder(LOrder));
        }
        
        protected void CopyPreservedOrders(Schema.Orders AOrders)
        {
			bool LAddOrder;
			foreach (Schema.Order LOrder in AOrders)
			{
				LAddOrder = true;
				Schema.OrderColumn LOrderColumn;
				for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				{
					LOrderColumn = LOrder.Columns[LIndex];
					LAddOrder = TableVar.Columns.ContainsName(LOrderColumn.Column.Name);
					if (!LAddOrder)
						break;
				}
				if (LAddOrder && !TableVar.Orders.Contains(LOrder))
					TableVar.Orders.Add(CopyOrder(LOrder));
			}
        }
        
        protected string DeriveSourceReferenceName(Schema.Reference AReference, int AReferenceID)
        {
			return DeriveSourceReferenceName(AReference, AReferenceID, AReference.SourceKey);
        }
        
        protected string DeriveSourceReferenceName(Schema.Reference AReference, int AReferenceID, Schema.JoinKey ASourceKey)
        {
			StringBuilder LName = new StringBuilder(AReference.OriginatingReferenceName());
			LName.AppendFormat("_{0}", Keywords.Source);
			for (int LIndex = 0; LIndex < ASourceKey.Columns.Count; LIndex++)
				LName.AppendFormat("_{0}", ASourceKey.Columns[LIndex].Column.Name);
			if (LName.Length > Schema.Object.CMaxObjectNameLength)
				return Schema.Object.GetGeneratedName(LName.ToString(), AReferenceID);
			return LName.ToString();
        }
        
        protected string DeriveTargetReferenceName(Schema.Reference AReference, int AReferenceID)
        {
			return DeriveTargetReferenceName(AReference, AReferenceID, AReference.TargetKey);
        }
        
        protected string DeriveTargetReferenceName(Schema.Reference AReference, int AReferenceID, Schema.JoinKey ATargetKey)
        {
			StringBuilder LName = new StringBuilder(AReference.OriginatingReferenceName());
			LName.AppendFormat("_{0}", Keywords.Target);
			for (int LIndex = 0; LIndex < ATargetKey.Columns.Count; LIndex++)
				LName.AppendFormat("_{0}", ATargetKey.Columns[LIndex].Column.Name);
			if (LName.Length > Schema.Object.CMaxObjectNameLength)
				return Schema.Object.GetGeneratedName(LName.ToString(), AReferenceID);
			return LName.ToString();
        }
        
        protected void CopySourceReference(Plan APlan, Schema.Reference AReference)
        {
			CopySourceReference(APlan, AReference, AReference.IsExcluded);
        }
        
        protected void CopySourceReference(Plan APlan, Schema.Reference AReference, bool AIsExcluded)
        {
			int LNewReferenceID = Schema.Object.GetNextObjectID();
			string LNewReferenceName = DeriveSourceReferenceName(AReference, LNewReferenceID);
			Schema.Reference LNewReference = new Schema.Reference(LNewReferenceID, LNewReferenceName);
			LNewReference.ParentReference = AReference;
			LNewReference.IsExcluded = AIsExcluded;
			LNewReference.InheritMetaData(AReference.MetaData);
			LNewReference.UpdateReferenceAction = AReference.UpdateReferenceAction;
			LNewReference.DeleteReferenceAction = AReference.DeleteReferenceAction;
			LNewReference.SourceTable = FTableVar;
			LNewReference.AddDependency(FTableVar);
			int LColumnIndex;
			bool LPreserved = true;
			foreach (Schema.TableVarColumn LColumn in AReference.SourceKey.Columns)
			{
				LColumnIndex = TableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
					LNewReference.SourceKey.Columns.Add(TableVar.Columns[LColumnIndex]);
				else
				{
					LPreserved = false;
					break;
				}
			}

			if (LPreserved)
			{
				foreach (Schema.Key LKey in TableVar.Keys)
					if (LKey.Columns.IsSubsetOf(LNewReference.SourceKey.Columns))
					{
						LNewReference.SourceKey.IsUnique = true;
						break;
					}

				LNewReference.TargetTable = AReference.TargetTable;
				LNewReference.AddDependency(AReference.TargetTable);
				LNewReference.TargetKey.IsUnique = AReference.TargetKey.IsUnique;
				foreach (Schema.TableVarColumn LColumn in AReference.TargetKey.Columns)
					LNewReference.TargetKey.Columns.Add(LColumn);

				if (!FTableVar.SourceReferences.ContainsSourceReference(LNewReference)) // This would only be true for unions and joins where both sides contain the same reference
				{
					FTableVar.SourceReferences.Add(LNewReference);
					FTableVar.DerivedReferences.Add(LNewReference);
				}
			}
        }
        
        protected void CopySourceReferences(Plan APlan, Schema.References AReferences)
        {
			foreach (Schema.Reference LReference in AReferences)
				CopySourceReference(APlan, LReference);
        }
        
        protected void CopyTargetReference(Plan APlan, Schema.Reference AReference)
        {
			CopyTargetReference(APlan, AReference, AReference.IsExcluded);
        }
        
        protected void CopyTargetReference(Plan APlan, Schema.Reference AReference, bool AIsExcluded)
        {
			int LNewReferenceID = Schema.Object.GetNextObjectID();
			string LNewReferenceName = DeriveTargetReferenceName(AReference, LNewReferenceID);
			Schema.Reference LNewReference = new Schema.Reference(LNewReferenceID, LNewReferenceName);
			LNewReference.ParentReference = AReference;
			LNewReference.IsExcluded = AIsExcluded;
			LNewReference.InheritMetaData(AReference.MetaData);
			LNewReference.UpdateReferenceAction = AReference.UpdateReferenceAction;
			LNewReference.DeleteReferenceAction = AReference.DeleteReferenceAction;
			LNewReference.SourceTable = AReference.SourceTable;
			LNewReference.AddDependency(AReference.SourceTable);
			LNewReference.SourceKey.IsUnique = AReference.SourceKey.IsUnique;
			foreach (Schema.TableVarColumn LColumn in AReference.SourceKey.Columns)
				LNewReference.SourceKey.Columns.Add(LColumn);
			LNewReference.TargetTable = FTableVar;
			LNewReference.AddDependency(FTableVar);
			int LColumnIndex;
			bool LPreserved = true;
			foreach (Schema.TableVarColumn LColumn in AReference.TargetKey.Columns)
			{
				LColumnIndex = TableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
					LNewReference.TargetKey.Columns.Add(TableVar.Columns[LColumnIndex]);
				else
				{
					LPreserved = false;
					break;
				}
			}
			
			if (LPreserved)
			{
				foreach (Schema.Key LKey in TableVar.Keys)
				{
					if (LKey.Columns.IsSubsetOf(LNewReference.TargetKey.Columns))
					{
						LNewReference.TargetKey.IsUnique = true;
						break;
					}
				}

				if (LNewReference.TargetKey.IsUnique && !FTableVar.TargetReferences.ContainsTargetReference(LNewReference)) // This would only be true for unions and joins where both sides contain the same reference
				{
					FTableVar.TargetReferences.Add(LNewReference);
					FTableVar.DerivedReferences.Add(LNewReference);
				}
			}
        }
        
        protected void CopyTargetReferences(Plan APlan, Schema.References AReferences)
        {
			foreach (Schema.Reference LReference in AReferences)
				CopyTargetReference(APlan, LReference);
        }
        
		// DataType
		public new virtual Schema.ITableType DataType { get { return (Schema.ITableType)FDataType; } }
		
		// Cursor Behaviors:
		// The current CursorContext for the plan contains the requested cursor behaviors.
		// After the DetermineDevice call, the following properties should be set to the 
		// actual cursor behaviors. For device supported nodes, these are set by the device 
		// during the Supports call. For all other table nodes, these are set to the appropriate 
		// values after the chunking has been determined for this node.
		
		// TableVar
		protected Schema.TableVar FTableVar;
		public virtual Schema.TableVar TableVar
		{
			get { return FTableVar; }
			set
			{
				FTableVar = value;
				if (FTableVar != null)
					FDataType = FTableVar.DataType;
			}
		}

		// Order
		private Schema.Order FOrder;
		public Schema.Order Order
		{
			get { return FOrder; }
			set { FOrder = value; }
		}
		
		// CursorType
		protected CursorType FCursorType;
		public CursorType CursorType
		{
			get { return FCursorType; }
			set { FCursorType = value; }
		}
		
		protected CursorType FRequestedCursorType;
		public CursorType RequestedCursorType
		{
			get { return FRequestedCursorType; }
			set { FRequestedCursorType = value; }
		}
		
		// CursorCapabilities
		protected CursorCapability FCursorCapabilities;
		public CursorCapability CursorCapabilities
		{
			get { return FCursorCapabilities; }
			set { FCursorCapabilities = value; }
		}
		
        public bool Supports(CursorCapability ACapability)
        {
			return ((ACapability & CursorCapabilities) != 0);
        }
        
        public void CheckCapability(CursorCapability ACapability)
        {
			if (!Supports(ACapability))
				throw new RuntimeException(RuntimeException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), ACapability));
        }
        
        public static string CursorCapabilitiesToString(CursorCapability ACursorCapabilities)
        {
			StringBuilder LResult = new StringBuilder();
			bool LFirst = true;
			CursorCapability LCapability;
			for (int LIndex = 0; LIndex < 7; LIndex++)
			{
				LCapability = (CursorCapability)Math.Pow(2, LIndex);
				if ((ACursorCapabilities & LCapability) != 0)
				{
					if (!LFirst)
						LResult.Append(", ");
					else
						LFirst = false;
						
					LResult.Append(LCapability.ToString().ToLower());
				}
			}
			return LResult.ToString();
        }
		
		// CursorIsolation
		protected CursorIsolation FCursorIsolation;
		public CursorIsolation CursorIsolation
		{
			get { return FCursorIsolation; }
			set { FCursorIsolation = value; }
		}
		
		protected override void DetermineModifiers(Plan APlan)
		{
			base.DetermineModifiers(APlan);

			if (Modifiers != null)
			{			
				ShouldSupportModify = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "ShouldSupportModify", ShouldSupportModify.ToString()));
				ShouldCheckConcurrency = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "ShouldCheckConcurrency", ShouldCheckConcurrency.ToString()));
			}
		}
		
		public override void DetermineDevice(Plan APlan)
		{
			PrepareJoinApplicationTransaction(APlan);
			base.DetermineDevice(APlan);
			if (!FDeviceSupported)
				DetermineCursorBehavior(APlan);
			FSymbols = Compiler.SnapshotSymbols(APlan);
			if ((FCursorCapabilities & CursorCapability.Updateable) != 0)
				DetermineModifySupported(APlan);
		}
		
		public override void DetermineBinding(Plan APlan)
		{
			base.DetermineBinding(APlan);
			if (FPopulateNode != null)
			{
				ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
				try
				{
					LTransaction.PushGlobalContext();
					try
					{
						FPopulateNode.DetermineBinding(APlan);
					}
					finally
					{
						LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		public virtual void DetermineRemotable(Plan APlan)
		{
			FTableVar.DetermineRemotable(APlan.CatalogDeviceSession);
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			//FTableVar.DetermineRemotable();
		}
		
		public virtual void DetermineCursorBehavior(Plan APlan) 
		{
			FCursorType = CursorType.Dynamic;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = CursorCapability.Navigable;
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
		}
		
		// ModifySupported, true if the device supports modification statements for this node
		protected bool FModifySupported;
		public bool ModifySupported
		{
			get { return FModifySupported; }
			set { FModifySupported = value; }
		}
	
		// ShouldSupportModify, true if the DAE should try to support the modification at this level
		private bool FShouldSupportModify = true;
		public bool ShouldSupportModify
		{
			get { return FShouldSupportModify; }
			set { FShouldSupportModify = value; }
		}
		
		public virtual void DetermineModifySupported(Plan APlan)
		{
			if ((TableVar.Keys.Count > 0) && FDeviceSupported && ShouldSupportModify)
			{
				// if any child node is a tabletype and not a tablenode  
				//	or is a table node and does not support modification, modification is not supported
				if (NodeCount > 0)
					foreach (PlanNode LNode in Nodes)
						if 
						(
							((LNode.DataType is Schema.TableType) && !(LNode is TableNode)) || 
							((LNode is TableNode) && !((TableNode)LNode).FModifySupported)
						)
						{
							FModifySupported = false;
							return;
						}
				
				FModifySupported = false;
				// TODO: Build modification binding cache
				#if USEMODIFICATIONBINDING
				// Use an update to determine whether any modification would be supported against this expression
				UpdateNode LUpdateNode = new UpdateNode();
				LNode.IsBreakable = false;
				LUpdateNode.Nodes.Add(this);
				FModifySupported = FDevice.Supports(APlan, LUpdateNode);
				#endif
			}
			else
				FModifySupported = false;
		}
		
		protected void CheckModifySupported()
		{
			if (!FModifySupported)
				throw new RuntimeException(RuntimeException.Codes.NoSupportingModificationDevice, EmitStatementAsString());
		}

		// Used to snapshot the stack to allow for run-time compilation of the concurrency and update nodes
		protected Symbols FSymbols;
		
		protected void PushSymbols(Plan APlan, Symbols ASymbols)
		{
			APlan.Symbols.PushWindow(0);
			APlan.EnterRowContext();
			for (int LIndex = ASymbols.Count - 1; LIndex >= 0; LIndex--)
				APlan.Symbols.Push(ASymbols.Peek(LIndex));
		}
		
		protected void PopSymbols(Plan APlan, Symbols ASymbols)
		{
			for (int LIndex = ASymbols.Count - 1; LIndex >= 0; LIndex--)
				APlan.Symbols.Pop();
			APlan.ExitRowContext();
			APlan.Symbols.PopWindow();
		}

		#if !USENATIVECONCURRENCYCOMPARE		
		protected PlanNodes FConcurrencyNodes;
		protected void EnsureConcurrencyNodes(ServerProcess AProcess)
		{
			lock (this)
			{
				if (FConcurrencyNodes == null)
				{
					FConcurrencyNodes = new PlanNodes();
					try
					{
						for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
						{
							FConcurrencyNodes.Add
							(
								Compiler.EmitBinaryNode
								(
									AProcess.Plan, 
									new StackReferenceNode(DataType.Columns[LIndex].DataType, 0, true), 
									DAE.Language.D4.Instructions.Equal, 
									new StackReferenceNode(DataType.Columns[LIndex].DataType, 1, true)
								)
							);
						}
					}
					catch
					{
						FConcurrencyNodes = null;
						AProcess.Plan.CheckCompiled();
						throw;
					}
				}
			}
		}
		#endif
		
		protected PlanNode CompileSelectNode(Program AProgram, bool AFullSelect)
		{
			ApplicationTransaction LTransaction = null;
			if (AProgram.ServerProcess.ApplicationTransactionID != Guid.Empty)
				LTransaction = AProgram.ServerProcess.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					Plan LPlan = new Plan(AProgram.ServerProcess);
					try
					{
						LPlan.PushATCreationContext();
						try
						{
							PushSymbols(LPlan, FSymbols);
							try
							{
								// Generate a select statement for use in optimistic concurrency checks
								LPlan.EnterRowContext();
								try
								{
									LPlan.Symbols.Push(new Symbol("ASelectRow", DataType.RowType));
									try
									{
										LPlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable | (CursorCapabilities & CursorCapability.Updateable), ((CursorCapabilities & CursorCapability.Updateable) != 0) ? CursorIsolation.Isolated : CursorIsolation.None));
										try
										{
											if (TableVar.Owner != null)
												LPlan.PushSecurityContext(new SecurityContext(TableVar.Owner));
											try
											{
												Schema.Columns LColumns;
												if (AFullSelect)
													LColumns = DataType.RowType.Columns;
												else
												{
													LColumns = new Schema.Columns();
													Schema.Key LKey = Compiler.FindClusteringKey(LPlan, TableVar);
													foreach (Schema.TableVarColumn LColumn in LKey.Columns)
														LColumns.Add(LColumn.Column);
												}
												return
													Compiler.Bind
													(
														LPlan,
														Compiler.EmitRestrictNode
														(
															LPlan,
															Compiler.CompileExpression(LPlan, (Expression)EmitStatement(EmitMode.ForCopy)),
															Compiler.BuildOptimisticRowEqualExpression
															(
																LPlan, 
																"",
																"ASelectRow",
																LColumns
															)
														)
													);
											}
											finally
											{
												if (TableVar.Owner != null)
													LPlan.PopSecurityContext();
											}
										}
										finally
										{
											LPlan.PopCursorContext();
										}
									}
									finally
									{
										LPlan.Symbols.Pop();
									}
								}
								finally
								{
									LPlan.ExitRowContext();
								}
							}
							finally
							{
								PopSymbols(LPlan, FSymbols);
							}
						}
						finally
						{
							LPlan.PopATCreationContext();
						}
					}
					finally
					{
						LPlan.Dispose();
					}
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}
		
		protected void EnsureSelectNode(Program AProgram)
		{
			lock (this)
			{
				if (FSelectNode == null)
				{
					FSelectNode = CompileSelectNode(AProgram, false);
				}
			}
		}
		
		protected void EnsureFullSelectNode(Program AProgram)
		{
			lock (this)
			{
				if (FFullSelectNode == null)
				{
					FFullSelectNode = CompileSelectNode(AProgram, true);
				}
			}
		}
		
		protected void EnsureModifyNodes(Plan APlan)
		{
			lock (this)
			{
				if (FInsertNode == null)
				{
					// Generate template modification instructions
					APlan.EnterRowContext();
					try
					{
						APlan.Symbols.Push(new Symbol(String.Empty, DataType.OldRowType));
						try
						{
							APlan.Symbols.Push(new Symbol(String.Empty, DataType.RowType));
							try
							{
								if (TableVar.Owner != null)
									APlan.PushSecurityContext(new SecurityContext(TableVar.Owner));
								try
								{
									Schema.RowType LOldKey = new Schema.RowType(Compiler.FindClusteringKey(APlan, TableVar).Columns, Keywords.Old);
									Schema.RowType LKey = new Schema.RowType(Compiler.FindClusteringKey(APlan, TableVar).Columns);
									FInsertNode = new InsertNode();
									FInsertNode.IsBreakable = false;
									FUpdateNode = new UpdateNode();
									FUpdateNode.IsBreakable = false;
									FUpdateNode.Nodes.Add(Compiler.EmitUpdateConditionNode(APlan, this, Compiler.CompileExpression(APlan, Compiler.BuildKeyEqualExpression(APlan, LOldKey.Columns, LKey.Columns))));
									FUpdateNode.TargetNode = FUpdateNode.Nodes[0].Nodes[0];
									FUpdateNode.ConditionNode = FUpdateNode.Nodes[0].Nodes[1];
									FDeleteNode = new DeleteNode();
									FDeleteNode.IsBreakable = false;
									FDeleteNode.Nodes.Add(Compiler.EmitRestrictNode(APlan, this, Compiler.CompileExpression(APlan, Compiler.BuildKeyEqualExpression(APlan, LOldKey.Columns, LKey.Columns))));
								}
								finally
								{
									if (TableVar.Owner != null)
										APlan.PopSecurityContext();
								}
							}
							finally
							{
								APlan.Symbols.Pop();
							}
						}
						finally
						{
							APlan.Symbols.Pop();
						}
					}
					finally
					{
						APlan.ExitRowContext();
					}
				}
			}
		}
		
		#if USEPROPOSALEVENTS
		// Proposal Events
		protected virtual void DoValidateRow(Row ARow, string AColumnName, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoValidateRow(APlan.ServerPlan.ServerSession, ARow, AColumnName);
		}
		
		protected virtual bool DoChangeRow(Row ARow, string AColumnName, Plan APlan)
		{
			if (FSourceTable != null)
				return FSourceTable.TableType.DoChangeRow(APlan.ServerPlan.ServerSession, ARow, AColumnName);
			else
				return false;
		}
		
		protected virtual bool DoDefaultRow(Row ARow, string AColumnName, Plan APlan)
		{
			if (FSourceTable != null)
				return FSourceTable.TableType.DoDefaultRow(APlan.ServerPlan.ServerSession, ARow, AColumnName);
			else
				return false;
		}

		// Modification Events		
		protected virtual void DoBeforeInsert(Row ARow, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoBeforeInsert(APlan.ServerPlan.ServerSession, ARow);
		}
		
		protected virtual void DoAfterInsert(Row ARow, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoAfterInsert(APlan.ServerPlan.ServerSession, ARow);
		}
		
		protected virtual void DoBeforeUpdate(Row AOldRow, Row ANewRow, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoBeforeUpdate(APlan.ServerPlan.ServerSession, AOldRow, ANewRow);
		}
		
		protected virtual void DoAfterUpdate(Row AOldRow, Row ANewRow, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoAfterUpdate(APlan.ServerPlan.ServerSession, AOldRow, ANewRow);
		}
		
		protected virtual void DoBeforeDelete(Row ARow, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoBeforeDelete(APlan.ServerPlan.ServerSession, ARow);
		}
		
		protected virtual void DoAfterDelete(Row ARow, Plan APlan)
		{
			if (FSourceTable != null)
				FSourceTable.TableType.DoAfterDelete(APlan.ServerPlan.ServerSession, ARow);
		}
		#endif
		
		/*
			Insert
				Push ARow
				BeforeInsert
					DoBeforeInsert
					PreparedDefault(NonDescending)
					ExecuteHandlers(EventType.BeforeInsert)
					PreparedValidate(NonDescending)
					InternalBeforeInsert - overridable stub
					ValidateInsertConstraints
				ExecuteInsert
					if FModifySupported
						InternalPrepareInsert - overridable stub
						try
							InternalBeforeDeviceInsert - overridable stub
							ExecuteDeviceInsert
							InternalAfterDeviceInsert - overridable stub
						finally
							InternalUnprepareInsert - overridable stub
						end
					else
						InternalExecuteInsert - overridable stub
				AfterInsert
					ExecuteHandlers(AfterInsert)
					ValidateCatalogConstraints
					InternalAfterInsert	- overridable stub
					DoAfterInsert
				Pop ARow
				
			InternalPrepareInsert
			InternalUnprepareInsert
				
			BeforeDeviceInsert
				BeforeInsert
				InternalBeforeDeviceInsert
				
			AfterDeviceInsert
				InternalAfterDeviceInsert
				AfterInsert
					
			Update
				Push AOldRow
				Push ANewRow
				BeforeUpdate
					DoBeforeUpdate
					ExecuteHandlers(EventType.BeforeUpdate)
					PreparedValidate(NonDescending)
					InternalBeforeUpdate - overridable stub
					ValidateUpdateConstraints
				ExecuteUpdate
					if FModifySupported
						InternalPrepareUpdate - overridable stub
						try
							InternalBeforeDeviceUpdate - overridable stub
							ExecuteDeviceUpdate
							InternalAfterDeviceUpdate - overridable stub
						finally
							InternalUnprepareUpdate - overridable stub
						end
					else
						InternalExecuteUpdate - overridable stub
				AfterUpdate
					ExecuteHandlers(EventType.AfterUpdate)
					ValidateCatalogConstraints
					InternalAfterUpdate - overridable stub
					DoAfterUpdate
				Pop ANewRow
				Pop AOldRow
				
			InternalPrepareUpdate
			InternalUnprepareUpdate
				
			BeforeDeviceUpdate
				BeforeUpdate
				InternalBeforeDeviceUpdate
				
			AfterDeviceUpdate
				InternalAfterDeviceUpdate
				AfterUpdate

			Delete
				Push ARow
				BeforeDelete
					DoBeforeDelete
					InternalBeforeDelete - overridable stub
					ValidateDeleteConstraints
					ExecuteHandlers(EventType.BeforeDelete)
				ExecuteDelete
					if FModifySupported
						InternalPrepareDelete - overridable stub
						try
							InternalBeforeDeviceDelete - overridable stub
							ExecuteDeviceDelete
							InternalAfterDeviceDelete - overridable stub
						finally
							InternalUnprepareDelete - overridable stub
						end
					else
						InternalExecuteDelete - overridable stub
				AfterDelete
					ExecuteHandlers(EventType.AfterDelete)
					ValidateCatalogConstraints
					InternalAfterDelete - overridable stub
					DoAfterDelete
				Pop ARow
				
			InternalPrepareDelete
			InternalUnprepareDelete
					
			BeforeDeviceDelete
				BeforeDelete
				InternalBeforeDeviceDelete
				
			AfterDeviceDelete
				InternalAfterDeviceDelete
				AfterDelete

			Validate
				Push ARow
				PreparedValidate(Descending)
				Pop ARow
				
			PreparedValidate
				DoValidateRow
				ExecuteValidateHandlers
				InternalValidate - overridable stub
				foreach Column
					ValidateColumnConstraints
				ValidateImmediateConstraints
				
			Default
				DoDefaultRow
				ExecuteDefaultHandlers
				InternalDefault - overridable stub
				foreach Column
					DefaultColumn
					
			Change
				Push ARow
				DoChangeRow
				ExecuteChangeHandlers
				InternalChange - overridable stub
				Pop ARow
		*/
		
		/// <summary>Prepares the row given in ANewRow for use in insert or update modification on this node.</summary>
		/// <remarks>
		/// If ANewRow is not equivalent to the DataType of this node, a new row will
		/// be created based on the DataType of this node and populated with the
		/// values from ANewRow. If AOldRow is not null, it will be used to provide
		/// values for the newly created row which are not available in ANewRow, if any.
		/// The row returned from this routine is guaranteed to be equivalent to the DataType of the node.
		/// Equivalent for row types is strictly stronger than Equality in that the order
		/// of columns in the type is the same, where this is not necessarily true for row types
		/// which are Equal.
		/// </remarks>
		public virtual Row PrepareNewRow(Program AProgram, Row AOldRow, Row ANewRow, ref BitArray AValueFlags)
		{
			if (!ANewRow.DataType.Columns.Equivalent(DataType.Columns))
			{
				Row LRow = new Row(AProgram.ValueManager, DataType.RowType);
				BitArray LValueFlags = AValueFlags != null ? new BitArray(LRow.DataType.Columns.Count) : null;
				int LColumnIndex;
				for (int LIndex = 0; LIndex < LRow.DataType.Columns.Count; LIndex++)
				{
					LColumnIndex = ANewRow.DataType.Columns.IndexOfName(LRow.DataType.Columns[LIndex].Name);
					if (LColumnIndex >= 0)
					{
						if (ANewRow.HasValue(LColumnIndex))
							LRow[LIndex] = ANewRow[LColumnIndex];

						if (LValueFlags != null)
							LValueFlags[LIndex] = AValueFlags[LColumnIndex];
					}
					else
					{
						if (AOldRow != null)
						{
							LColumnIndex = AOldRow.DataType.Columns.IndexOfName(LRow.DataType.Columns[LIndex].Name);
							if ((LColumnIndex >= 0) && AOldRow.HasValue(LColumnIndex))
								LRow[LIndex] = AOldRow[LColumnIndex];
						}
						
						if (LValueFlags != null)
							LValueFlags[LIndex] = false;
					}
				}
				ANewRow = LRow;
				AValueFlags = LValueFlags;
			}

			return ANewRow;
		}
		
		public void PushRow(Program AProgram, Row ARow)
		{
			if (ARow != null)
			{
				Row LRow = new Row(AProgram.ValueManager, DataType.RowType, (NativeRow)ARow.AsNative);
				AProgram.Stack.Push(LRow);
			}
			else
				AProgram.Stack.Push(null);
		}
		
		public void PushNewRow(Program AProgram, Row ARow)
		{
			#if USENAMEDROWVARIABLES
			Row LRow = new Row(AProgram.ValueManager, DataType.RowType, (NativeRow)ARow.AsNative);
			#else
			Row LRow = new Row(AProgram.ValueManager, DataType.NewRowType, (NativeRow)ARow.AsNative);
			#endif
			AProgram.Stack.Push(LRow);
		}
		
		public void PushOldRow(Program AProgram, Row ARow)
		{
			#if USENAMEDROWVARIABLES
			Row LOldRow = new Row(AProgram.ValueManager, DataType.RowType, (NativeRow)ARow.AsNative);
			#else
			Row LOldRow = new Row(AProgram.ValueManager, DataType.OldRowType, (NativeRow)ARow.AsNative);
			#endif
			AProgram.Stack.Push(LOldRow);
		}

		public void PopRow(Program AProgram)
		{
			Row LRow = (Row)AProgram.Stack.Pop();
			if (LRow != null)
				LRow.Dispose();
		}
		
		/// <summary>Selects a row based on the node and the given values, will return null if no row is found.</summary>
		/// <remarks>
		/// Select restricts the result set based on the clustering key of the node, whereas FullSelect restricts
		/// the result set based on all columns declared of a type that has an equality operator defined.
		/// </remarks>
		public Row Select(Program AProgram, Row ARow)
		{
			// Symbols will only be null if this node is not bound
			// The node will not be bound if it is functioning in a local server context evaluating change proposals in the client
			if (FSymbols != null)
			{
				EnsureSelectNode(AProgram);
				AProgram.Stack.Push(ARow);
				try
				{
					using (Table LTable = (Table)FSelectNode.Execute(AProgram))
					{
						if (LTable.Next())
							return LTable.Select();
						else
							return null;
					}
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}
			return null;
		}

		/// <summary>Selects a row based on the node and the given values, will return null if no row is found.</summary>
		/// <remarks>
		/// Select restricts the result set based on the clustering key of the node, whereas FullSelect restricts
		/// the result set based on all columns declared of a type that has an equality operator defined.
		/// </remarks>
		public Row FullSelect(Program AProgram, Row ARow)
		{
			// Symbols will only be null if this node is not bound
			// The node will not be bound if it is functioning in a local server context evaluating change proposals in the client
			if (FSymbols != null)
			{
				EnsureFullSelectNode(AProgram);
				AProgram.Stack.Push(ARow);
				try
				{
					using (Table LTable = (Table)FFullSelectNode.Execute(AProgram))
					{
						if (LTable.Next())
							return LTable.Select();
						else
							return null;
					}
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}
			return null;
		}

		private bool FShouldCheckConcurrency = false;
		public bool ShouldCheckConcurrency
		{
			get { return FShouldCheckConcurrency; }
			set { FShouldCheckConcurrency = value; }
		}
		
		/// <summary>Performs an optimistic concurrency check for the given row.</summary>
		/// <remarks>
		/// AOldRow may be a different row type than the data type for this node,
		/// but ACurrentRow will always be a row type with the same heading as the table type
		/// for this node.
		/// </remarks>
		protected void CheckConcurrency(Program AProgram, Row AOldRow, Row ACurrentRow)
		{
			#if !USENATIVECONCURRENCYCOMPARE
			EnsureConcurrencyNodes(AProcess);
			#endif
			object LOldValue;
			object LCurrentValue;
			
			bool LRowsEqual = true;
			int LColumnIndex;
			for (int LIndex = 0; LIndex < AOldRow.DataType.Columns.Count; LIndex++)
			{
				LColumnIndex = ACurrentRow.DataType.Columns.IndexOfName(AOldRow.DataType.Columns[LIndex].Name);
				if (LColumnIndex >= 0)
				{
					if (AOldRow.HasValue(LIndex))
					{
						if (ACurrentRow.HasValue(LColumnIndex))
						{
							LOldValue = AOldRow[LIndex];
							LCurrentValue = ACurrentRow[LColumnIndex];
							#if USENATIVECONCURRENCYCOMPARE
							LRowsEqual = DataValue.NativeValuesEqual(AProgram.ValueManager, LOldValue, LCurrentValue);
							#else
							AProcess.Context.Push(LOldValue);
							AProcess.Context.Push(LCurrentValue);
							try
							{
								object LResult = FConcurrencyNodes[LColumnIndex].Execute(AProcess);
								LRowsEqual = (LResult != null) && (bool)LResult;
							}
							finally
							{
								AProcess.Context.Pop();
								AProcess.Context.Pop();
							}
							#endif
						}
						else
							LRowsEqual = false;
					}
					else
					{
						if (ACurrentRow.HasValue(LColumnIndex))
							LRowsEqual = false;
					}
				}
				
				if (!LRowsEqual)
					break;
			}
			
			if (!LRowsEqual)
				throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckFailed, ErrorSeverity.Environment);
		}

		/// <summary>Prepares the given row for update or delete modification on this node.</summary>
		/// <remarks>
		///	If ARow is not equivalent to the DataType of this node, a new row will be created
		/// based on the DataType of this node, and the values from ARow will be copied into it.
		/// This new row will be used to select the full row from the underlying source table.
		/// If a row is found, the values from ARow will be copied into it, and it will be used
		/// as the prepared row.  If a row is not found and ACheckConcurrency is true, an
		/// exception will be raised.  The row returned from this routine is guaranteed to be 
		/// equivalent to the data type of this node.  Equivalent for row types is strictly 
		/// stronger than Equality in that the order of columns in the type is the same, 
		/// where this is not necessarily true for row types which are Equal.
		/// </remarks>
		public virtual Row PrepareOldRow(Program AProgram, Row ARow, bool ACheckConcurrency)
		{
			if ((ACheckConcurrency && ShouldCheckConcurrency) || !ARow.DataType.Columns.Equivalent(DataType.Columns))
			{
				// reselect the full row buffer for this row
				bool LSelectRowReturned = false;
				Row LSelectRow = new Row(AProgram.ValueManager, DataType.RowType);
				try
				{
					ARow.CopyTo(LSelectRow);
					if (((ACheckConcurrency && ShouldCheckConcurrency) || LSelectRow.DataType.Columns.IsProperSupersetOf(ARow.DataType.Columns)) && !AProgram.ServerProcess.ServerSession.Server.IsEngine)
					{
						Row LRow = Select(AProgram, LSelectRow);
						if (LRow != null)
						{
							if (ACheckConcurrency && ShouldCheckConcurrency)
								CheckConcurrency(AProgram, ARow, LRow);
							else
								ARow.CopyTo(LRow);

							return LRow;
						}
						else
						{
							if (ACheckConcurrency && ShouldCheckConcurrency)
								throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound, ErrorSeverity.Environment);
								
							LSelectRowReturned = true;
							return LSelectRow;
						}
					}
					else
					{
						LSelectRowReturned = true;
						return LSelectRow;
					}
				}
				finally
				{
					if (!LSelectRowReturned)
						LSelectRow.Dispose();
				}
			}

			return ARow;
		}

		// If AValueFlags are specified, they indicate whether or not each column of ANewRow was specified as part of the insert.
		public virtual void Insert(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			Row LNewPreparedRow;
			Row LOldPreparedRow = null;
			if (AOldRow != null)
				LOldPreparedRow = PrepareOldRow(AProgram, AOldRow, false);
			try
			{
				LNewPreparedRow = PrepareNewRow(AProgram, LOldPreparedRow, ANewRow, ref AValueFlags);
			}
			catch
			{
				if (!Object.ReferenceEquals(AOldRow, LOldPreparedRow))
					LOldPreparedRow.Dispose();
				throw;
			}
			
			try
			{
				if (AUnchecked || ((AOldRow == null) && BeforeInsert(AProgram, LNewPreparedRow, AValueFlags)) || ((AOldRow != null) && BeforeUpdate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags)))
				{
					ExecuteInsert(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags, AUnchecked);
					if (!AUnchecked)
						if (AOldRow == null)
							AfterInsert(AProgram, LNewPreparedRow, AValueFlags);
						else
							AfterUpdate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags);
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(ANewRow, LNewPreparedRow))
					LNewPreparedRow.Dispose();
				if (!Object.ReferenceEquals(AOldRow, LOldPreparedRow))
					LOldPreparedRow.Dispose();
			}
		}

		protected internal bool BeforeInsert(Program AProgram, Row ARow, BitArray AValueFlags)
		{
			#if USEPROPOSALEVENTS
			DoBeforeInsert(ARow, AProgram);
			#endif
			PreparedDefault(AProgram, null, ARow, AValueFlags, String.Empty, false);
			bool LPerform = true;
			if (TableVar.HasHandlers(EventType.BeforeInsert))
			{
				PushRow(AProgram, ARow);
				try
				{
					object LPerformVar = LPerform;
					AProgram.Stack.Push(LPerformVar);
					try
					{
						if (AValueFlags != null)
							ARow.BeginModifiedContext();
						try
						{
							ExecuteHandlers(AProgram, EventType.BeforeInsert);
						}
						finally
						{
							if (AValueFlags != null)
							{
								BitArray LModifiedFlags = ARow.EndModifiedContext();
								for (int LIndex = 0; LIndex < LModifiedFlags.Count; LIndex++)
									if (LModifiedFlags[LIndex])
										AValueFlags[LIndex] = true;
							}
						}
					}
					finally
					{
						LPerformVar = AProgram.Stack.Pop();
					}
					LPerform = (LPerformVar != null) && (bool)LPerformVar;
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			
			if (LPerform)
			{
				PreparedValidate(AProgram, null, ARow, AValueFlags, String.Empty, false, false);
				InternalBeforeInsert(AProgram, ARow, AValueFlags);
				if ((TableVar.InsertConstraints.Count > 0) || (TableVar.RowConstraints.Count > 0) || (AProgram.ServerProcess.InTransaction && TableVar.HasDeferredConstraints()))
				{
					PushNewRow(AProgram, ARow);
					try
					{
						ValidateInsertConstraints(AProgram);
					}
					finally
					{
						PopRow(AProgram);
					}
				}
			}

			return LPerform;
		}
		
		protected internal void ExecuteInsert(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			InternalExecuteInsert(AProgram, AOldRow, ANewRow, AValueFlags, AUnchecked);
		}
		
		protected internal void AfterInsert(Program AProgram, Row ARow, BitArray AValueFlags)
		{
			if (TableVar.HasHandlers(EventType.AfterInsert))
			{
				PushRow(AProgram, ARow);
				try
				{
					ExecuteHandlers(AProgram, EventType.AfterInsert);
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			ValidateCatalogConstraints(AProgram);
			InternalAfterInsert(AProgram, ARow, AValueFlags);
			#if USEPROPOSALEVENTS
			DoAfterInsert(ARow, AProgram);
			#endif
		}
		
		public virtual void Update(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			Row LNewPreparedRow;
			Row LOldPreparedRow = PrepareOldRow(AProgram, AOldRow, ACheckConcurrency);
			try
			{
				LNewPreparedRow = PrepareNewRow(AProgram, LOldPreparedRow, ANewRow, ref AValueFlags);
			}
			catch
			{
				if (!Object.ReferenceEquals(AOldRow, LOldPreparedRow))
					LOldPreparedRow.Dispose();
				throw;
			}
			
			try
			{
				if (AUnchecked || BeforeUpdate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags))
				{
					ExecuteUpdate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags, ACheckConcurrency, AUnchecked);
					if (!AUnchecked)
						AfterUpdate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags);
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(ANewRow, LNewPreparedRow))
					LNewPreparedRow.Dispose();
				if (!Object.ReferenceEquals(AOldRow, LOldPreparedRow))
					LOldPreparedRow.Dispose();
			}
		}
		
		protected internal bool BeforeUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			#if USEPROPOSALEVENTS
			DoBeforeUpdate(AOldRow, ANewRow, AProgram);
			#endif
			bool LPerform = true;
			if (TableVar.HasHandlers(EventType.BeforeUpdate))
			{
				PushRow(AProgram, AOldRow);
				try
				{
					PushRow(AProgram, ANewRow);
					try
					{
						object LPerformVar = LPerform;
						AProgram.Stack.Push(LPerformVar);
						try
						{
							if (AValueFlags != null)
								ANewRow.BeginModifiedContext();
							try
							{
								ExecuteHandlers(AProgram, EventType.BeforeUpdate);
							}
							finally
							{
								if (AValueFlags != null)
								{
									BitArray LModifiedFlags = ANewRow.EndModifiedContext();
									for (int LIndex = 0; LIndex < LModifiedFlags.Count; LIndex++)
										if (LModifiedFlags[LIndex])
											AValueFlags[LIndex] = true;
								}
							}
						}
						finally
						{
							LPerformVar = AProgram.Stack.Pop();
						}
						LPerform = (LPerformVar != null) && (bool)LPerformVar;
					}
					finally
					{
						PopRow(AProgram);
					}
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			
			if (LPerform)
			{
				PreparedValidate(AProgram, AOldRow, ANewRow, AValueFlags, String.Empty, false, false);
				InternalBeforeUpdate(AProgram, AOldRow, ANewRow, AValueFlags);
				if ((TableVar.UpdateConstraints.Count > 0) || (TableVar.RowConstraints.Count > 0) || (AProgram.ServerProcess.InTransaction && TableVar.HasDeferredConstraints()))
				{
					PushOldRow(AProgram, AOldRow);
					try
					{
						PushNewRow(AProgram, ANewRow);
						try
						{
							ValidateUpdateConstraints(AProgram, AValueFlags);
						}
						finally
						{
							PopRow(AProgram);
						}
					}
					finally
					{
						PopRow(AProgram);
					}
				}
			}

			return LPerform;
		}
		
		protected internal void ExecuteUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			InternalExecuteUpdate(AProgram, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
		}
		
		protected internal void AfterUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			if (TableVar.HasHandlers(EventType.AfterUpdate))
			{
				PushRow(AProgram, AOldRow);
				try
				{
					PushRow(AProgram, ANewRow);
					try
					{
						ExecuteHandlers(AProgram, EventType.AfterUpdate);
					}
					finally
					{
						PopRow(AProgram);
					}
				}
				finally
				{
					PopRow(AProgram);
				}
			}

			ValidateCatalogConstraints(AProgram);
			InternalAfterUpdate(AProgram, AOldRow, ANewRow, AValueFlags);
			#if USEPROPOSALEVENTS
			DoAfterUpdate(AOldRow, ANewRow, AProgram);
			#endif
		}
		
		public virtual void Delete(Program AProgram, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			Row LPreparedRow = PrepareOldRow(AProgram, ARow, false);
			try
			{
				if (AUnchecked || BeforeDelete(AProgram, LPreparedRow))
				{
					ExecuteDelete(AProgram, LPreparedRow, ACheckConcurrency, AUnchecked);
					if (!AUnchecked)
						AfterDelete(AProgram, LPreparedRow);
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(LPreparedRow, ARow))
					LPreparedRow.Dispose();
			}
		}
		
		protected internal bool BeforeDelete(Program AProgram, Row ARow)
		{
			#if USEPROPOSALEVENTS
			DoBeforeDelete(ARow, AProgram);
			#endif
			bool LPerform = true;
			if (TableVar.HasHandlers(EventType.BeforeDelete))
			{
				PushRow(AProgram, ARow);
				try
				{
					object LPerformVar = LPerform;
					AProgram.Stack.Push(LPerformVar);
					try
					{
						ExecuteHandlers(AProgram, EventType.BeforeDelete);
					}
					finally
					{
						LPerformVar = AProgram.Stack.Pop();
					}
					LPerform = (LPerformVar != null) && (bool)LPerformVar;
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			
			if (LPerform)
			{
				InternalBeforeDelete(AProgram, ARow);
				if ((TableVar.DeleteConstraints.Count > 0) || (AProgram.ServerProcess.InTransaction && TableVar.HasDeferredConstraints()))
				{
					PushOldRow(AProgram, ARow);
					try
					{
						ValidateDeleteConstraints(AProgram);
					}
					finally
					{
						PopRow(AProgram);
					}
				}
			}

			return LPerform;
		}
		
		protected internal void ExecuteDelete(Program AProgram, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			InternalExecuteDelete(AProgram, ARow, ACheckConcurrency, AUnchecked);
		}
		
		protected internal void AfterDelete(Program AProgram, Row ARow)
		{
			if (TableVar.HasHandlers(EventType.AfterDelete))
			{
				PushRow(AProgram, ARow);
				try
				{
					ExecuteHandlers(AProgram, EventType.AfterDelete);
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			ValidateCatalogConstraints(AProgram);
			InternalAfterDelete(AProgram, ARow);
			#if USEPROPOSALEVENTS
			DoAfterDelete(ARow, AProgram);
			#endif
		}
		
		public bool ShouldValidate(string AColumnName)
		{
			if (AColumnName == String.Empty)
				return FTableVar.ShouldValidate;
			return FTableVar.ShouldValidate || FTableVar.Columns[FTableVar.Columns.IndexOfName(AColumnName)].ShouldValidate;
		}
		
		public virtual bool Validate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (ShouldValidate(AColumnName))
			{
				Row LOldPreparedRow = AOldRow == null ? null : PrepareOldRow(AProgram, AOldRow, false);
				try
				{
					BitArray LValueFlags = AValueFlags;
					Row LNewPreparedRow = PrepareNewRow(AProgram, AOldRow, ANewRow, ref LValueFlags);
					try
					{
						bool LChanged = PreparedValidate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags, AColumnName, true, true);
						if (LChanged && !Object.ReferenceEquals(LNewPreparedRow, ANewRow))
							LNewPreparedRow.CopyTo(ANewRow);
						return LChanged;
					}
					finally
					{
						if (!Object.ReferenceEquals(ANewRow, LNewPreparedRow))
							LNewPreparedRow.Dispose();
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(AOldRow, LOldPreparedRow))
						LOldPreparedRow.Dispose();
				}
			}

			return false;
		}
		
		protected bool PreparedValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			#if USEPROPOSALEVENTS
			DoValidateRow(ANewRow, AColumnName, AProgram);
			#endif
			bool LChanged = ExecuteValidateHandlers(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			LChanged = ValidateColumns(AProgram, TableVar, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable) || LChanged;
			LChanged = InternalValidate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable) || LChanged;
			if ((AColumnName == String.Empty) && (TableVar.RowConstraints.Count > 0) && !AIsProposable)
			{
				PushRow(AProgram, ANewRow);
				try
				{
					// If this is an insert (AOldRow == null) then the constraints must be validated regardless of whether or not a value was specified in the insert
					ValidateImmediateConstraints(AProgram, AIsDescending, AOldRow == null ? null : AValueFlags);
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			return LChanged;
		}
		
		protected bool InternalPreparedValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AIsDescending, bool AIsProposable)
		{
			// Given AOldRow and ANewRow, propagate a validate if necessary based on the difference between the row values
			#if !USENATIVECONCURRENCYCOMPARE
			EnsureConcurrencyNodes(AProgram);
			#endif
			
			int LDifferentColumnIndex = -1;
			bool LRowsEqual = true;
			int LColumnIndex;
			for (int LIndex = 0; LIndex < AOldRow.DataType.Columns.Count; LIndex++)
			{
				LColumnIndex = ANewRow.DataType.Columns.IndexOfName(AOldRow.DataType.Columns[LIndex].Name);
				if (LColumnIndex >= 0)
				{
					if (AOldRow.HasValue(LIndex))
					{
						if (ANewRow.HasValue(LColumnIndex))
						{
							#if USENATIVECONCURRENCYCOMPARE
							if (!(DataValue.NativeValuesEqual(AProgram.ValueManager, AOldRow[LIndex], ANewRow[LColumnIndex])))
								if (LDifferentColumnIndex >= 0)
									LRowsEqual = false;
								else
									LDifferentColumnIndex = LColumnIndex;
							#else
							AProgram.Context.Push(AOldRow[LIndex]);
							AProgram.Context.Push(ANewRow[LColumnIndex]);
							try
							{
								object LResult = FConcurrencyNodes[LColumnIndex].Execute(AProgram);
								if (!((LResult != null) && (bool)LResult))
									if (LDifferentColumnIndex >= 0)
										LRowsEqual = false;
									else
										LDifferentColumnIndex = LColumnIndex;
							}
							finally
							{
								AProgram.Context.Pop();
								AProgram.Context.Pop();
							}
							#endif
						}
						else
							if (LDifferentColumnIndex >= 0)
								LRowsEqual = false;
							else
								LDifferentColumnIndex = LColumnIndex;
					}
					else
					{
						if (ANewRow.HasValue(LColumnIndex))
							if (LDifferentColumnIndex >= 0)
								LRowsEqual = false;
							else
								LDifferentColumnIndex = LColumnIndex;
					}
				}
			}
			
			if (!LRowsEqual)
				return PreparedValidate(AProgram, AOldRow, ANewRow, AValueFlags, String.Empty, AIsDescending, AIsProposable);
			else
				if (LDifferentColumnIndex >= 0)
					return PreparedValidate(AProgram, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LDifferentColumnIndex].Name, AIsDescending, AIsProposable);
					
			return false;
		}
		
		public bool ShouldChange(string AColumnName)
		{
			if (AColumnName == String.Empty)
				return FTableVar.ShouldChange;
			return FTableVar.ShouldChange || FTableVar.Columns[FTableVar.Columns.IndexOfName(AColumnName)].ShouldChange;
		}
		
		public virtual bool Change(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (ShouldChange(AColumnName))
			{
				Row LOldPreparedRow = PrepareOldRow(AProgram, AOldRow, false);
				try
				{
					BitArray LValueFlags = AValueFlags;
					Row LNewPreparedRow = PrepareNewRow(AProgram, AOldRow, ANewRow, ref LValueFlags);
					try
					{
						#if USEPROPOSALEVENTS
						bool LChanged = DoChangeRow(LRow, AColumnName, AProgram);
						#endif
						bool LChanged = ExecuteChangeHandlers(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags, AColumnName);
						LChanged = InternalChange(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags, AColumnName) || LChanged;
						LChanged = ChangeColumns(AProgram, TableVar, LOldPreparedRow, LNewPreparedRow, AValueFlags, AColumnName) || LChanged;
						if (LChanged)
							InternalPreparedValidate(AProgram, LOldPreparedRow, LNewPreparedRow, AValueFlags, false, true);
						if (LChanged && !Object.ReferenceEquals(LNewPreparedRow, ANewRow))
							LNewPreparedRow.CopyTo(ANewRow);
						return LChanged;
					}
					finally
					{
						if (!Object.ReferenceEquals(ANewRow, LNewPreparedRow))
						{
							if (AValueFlags != null)
								for (int LIndex = 0; LIndex < LValueFlags.Count; LIndex++)
									if (LValueFlags[LIndex])
										AValueFlags[ANewRow.DataType.Columns.IndexOfName(LNewPreparedRow.DataType.Columns[LIndex].Name)] = LValueFlags[LIndex];
			
							LNewPreparedRow.Dispose();
						}
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(AOldRow, LOldPreparedRow))
						LOldPreparedRow.Dispose();
				}
			}
			
			return false;
		}
		
		public bool ShouldDefault(string AColumnName)
		{
			if (AColumnName == String.Empty)
				return FTableVar.ShouldDefault;
			return FTableVar.ShouldDefault || FTableVar.Columns[FTableVar.Columns.IndexOfName(AColumnName)].ShouldDefault;
		}
		
		public virtual bool Default(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (ShouldDefault(AColumnName))
			{
				Row LOldRow;
				BitArray LValueFlags = null;
				if (AOldRow == null)
					LOldRow = new Row(AProgram.ValueManager, DataType.RowType);
				else
					LOldRow = PrepareNewRow(AProgram, null, AOldRow, ref LValueFlags);
				try
				{
					Row LNewRow = PrepareNewRow(AProgram, null, ANewRow, ref AValueFlags);
					try
					{
						bool LChanged = PreparedDefault(AProgram, LOldRow, LNewRow, AValueFlags, AColumnName, true);
						if (LChanged && !Object.ReferenceEquals(LNewRow, ANewRow))
							LNewRow.CopyTo(ANewRow);
						return LChanged;
					}
					finally
					{
						if (!Object.ReferenceEquals(ANewRow, LNewRow))
							LNewRow.Dispose();
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(AOldRow, LOldRow))
						LOldRow.Dispose();
				}
			}
			
			return false;
		}
		
		protected bool PreparedDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			Row LOldRow;
			if ((AOldRow == null) && AIsDescending)
				LOldRow = new Row(AProgram.ValueManager, DataType.RowType);
			else
				LOldRow = AOldRow;
			try
			{
				#if USEPROPOSALEVENTS
				bool LChanged = DoDefaultRow(ARow, AColumnName, AProgram);
				#endif
				bool LChanged = ExecuteDefaultHandlers(AProgram, ANewRow, AValueFlags, AColumnName);
				LChanged = InternalDefault(AProgram, LOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending) || LChanged;
				return LChanged;
			}
			finally
			{
				if (!ReferenceEquals(AOldRow, LOldRow) && (LOldRow != null))
					LOldRow.Dispose();
			}
		}
		
		// DefaultColumns
		public static bool DefaultColumns(Program AProgram, Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags, string AColumnName)
		{
			if (AColumnName != String.Empty)
				return DefaultColumn(AProgram, ATableVar, ARow, AValueFlags, AColumnName);
			else
			{
				bool LChanged = false;
				for (int LIndex = 0; LIndex < ATableVar.Columns.Count; LIndex++)
					LChanged = DefaultColumn(AProgram, ATableVar, ARow, AValueFlags, ATableVar.Columns[LIndex].Name) || LChanged;
				return LChanged;
			}
		}
		
		// DefaultColumn
		public static bool DefaultColumn(Program AProgram, Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags, string AColumnName)
		{
			int LRowIndex = ARow.DataType.Columns.IndexOfName(AColumnName);
			if (LRowIndex >= 0)
			{
				int LIndex = ATableVar.Columns.IndexOfName(AColumnName);
				if (!ARow.HasValue(LRowIndex) && ((AValueFlags == null) || !AValueFlags[LRowIndex]))
				{
					// Column level default trigger handlers
					AProgram.Stack.Push(null);
					try
					{
						if (ATableVar.Columns[LIndex].HasHandlers())
							foreach (Schema.EventHandler LHandler in ATableVar.Columns[LIndex].EventHandlers)
								if ((LHandler.EventType & EventType.Default) != 0)
								{
									object LResult = LHandler.PlanNode.Execute(AProgram);
									if ((LResult != null) && (bool)LResult)
									{
										ARow[LRowIndex] = AProgram.Stack.Peek(0);
										if (AValueFlags != null)
											AValueFlags[LRowIndex] = true;
										return true;
									}
								}
					}
					finally
					{
						AProgram.Stack.Pop();
					}
					
					// Column level default
					if (ATableVar.Columns[LIndex].Default != null)
					{
						ARow[LRowIndex] = ATableVar.Columns[LIndex].Default.Node.Execute(AProgram);
						if (AValueFlags != null)
							AValueFlags[LRowIndex] = true;
						return true;
					} 

					// Scalar type level default trigger handlers
					Schema.ScalarType LScalarType = ATableVar.Columns[LIndex].DataType as Schema.ScalarType;
					if (LScalarType != null)
					{
						AProgram.Stack.Push(null);
						try
						{
							if (LScalarType.HasHandlers())
								foreach (Schema.EventHandler LHandler in LScalarType.EventHandlers)
									if ((LHandler.EventType & EventType.Default) != 0)
									{
										object LResult = LHandler.PlanNode.Execute(AProgram);
										if ((LResult != null) && (bool)LResult)
										{
											ARow[LRowIndex] = AProgram.Stack.Peek(0);
											if (AValueFlags != null)
												AValueFlags[LRowIndex] = true;
											return true;
										}
									}
						}
						finally
						{
							AProgram.Stack.Pop();
						}

						// Scalar type level default													   
						if (LScalarType.Default != null)
						{
							ARow[LRowIndex] = LScalarType.Default.Node.Execute(AProgram);
							if (AValueFlags != null)
								AValueFlags[LRowIndex] = true;
							return true;
						}
					}
				}
			}
			return false;
		}
		
		public static bool ExecuteChangeHandlers(Program AProgram, Schema.EventHandlers AHandlers)
		{
			bool LChanged = false;
			foreach (Schema.EventHandler LEventHandler in AHandlers)
				if ((LEventHandler.EventType & EventType.Change) != 0)
				{
					object LResult = LEventHandler.PlanNode.Execute(AProgram);
					LChanged = ((LResult != null) && (bool)LResult) || LChanged;
				}
			return LChanged;
		}
		
		public static bool ExecuteScalarTypeChangeHandlers(Program AProgram, Schema.ScalarType AScalarType)
		{
			bool LChanged = false;
			if (AScalarType.HasHandlers())
				LChanged = ExecuteChangeHandlers(AProgram, AScalarType.EventHandlers);
			#if USETYPEINHERITANCE
			foreach (Schema.ScalarType LParentType in AScalarType.ParentTypes)
				LChanged = ExecuteScalarTypeChangeHandlers(AProgram, LParentType) || LChanged;
			#endif
			return LChanged;
		}
		
		// ChangeColumns
		public static bool ChangeColumns(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (AColumnName != String.Empty)
			{
				int LRowIndex = ANewRow.DataType.Columns.IndexOfName(AColumnName);
				AProgram.Stack.Push(AOldRow);
				try
				{
					AProgram.Stack.Push(ANewRow);
					try
					{
						bool LChanged = false;
						if (AValueFlags != null)
							ANewRow.BeginModifiedContext();
						try
						{
							if (ATableVar.Columns[ATableVar.Columns.IndexOfName(AColumnName)].HasHandlers())
								if (ExecuteChangeHandlers(AProgram, ATableVar.Columns[ATableVar.Columns.IndexOfName(AColumnName)].EventHandlers))
									LChanged = true;
								
							if (LChanged && (!Object.ReferenceEquals(ANewRow, AProgram.Stack.Peek(0))))
							{
								Row LRow = (Row)AProgram.Stack.Peek(0);
								LRow.CopyTo(ANewRow);
								LRow.ValuesOwned = false;
								LRow.Dispose();
								AProgram.Stack.Poke(0, ANewRow);
							}
						}
						finally
						{
							if (AValueFlags != null)
							{
								BitArray LModifiedFlags = ANewRow.EndModifiedContext();
								for (int LIndex = 0; LIndex < LModifiedFlags.Length; LIndex++)
									if (LModifiedFlags[LIndex])
										AValueFlags[LIndex] = true;
							}
						}

						if (ATableVar.Columns[ATableVar.Columns.IndexOfName(AColumnName)].DataType is Schema.ScalarType)
						{
							AProgram.Stack.Push(AOldRow[LRowIndex]);
							try
							{
								AProgram.Stack.Push(ANewRow[LRowIndex]);
								try
								{
									bool LColumnChanged = ExecuteScalarTypeChangeHandlers(AProgram, (Schema.ScalarType)ATableVar.Columns[ATableVar.Columns.IndexOfName(AColumnName)].DataType);
									if (LColumnChanged)
									{
										ANewRow[LRowIndex] = AProgram.Stack.Peek(0);
										if (AValueFlags != null)
											AValueFlags[LRowIndex] = true;
										LChanged = true;
									}

									return LChanged;
								}
								finally
								{
									AProgram.Stack.Pop();
								}
							}
							finally
							{
								AProgram.Stack.Pop();
							}
						}
						return LChanged;
					}
					finally
					{
						AProgram.Stack.Pop();
					}
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}
			else
			{
				int LRowIndex;
				bool LChanged = false;
				AProgram.Stack.Push(ANewRow);
				try
				{
					foreach (Schema.TableVarColumn LColumn in ATableVar.Columns)
					{
						LRowIndex = ANewRow.DataType.Columns.IndexOfName(LColumn.Name);
						if (LRowIndex >= 0)
						{
							bool LColumnChanged = false;
							if (AValueFlags != null)
								ANewRow.BeginModifiedContext();
							try
							{
								if (LColumn.HasHandlers())
									LColumnChanged = ExecuteChangeHandlers(AProgram, LColumn.EventHandlers);

								if (LColumnChanged)
								{
									LChanged = true;
									if (!Object.ReferenceEquals(ANewRow, AProgram.Stack.Peek(0)))
									{
										Row LRow = (Row)AProgram.Stack.Peek(0);
										LRow.CopyTo(ANewRow);
										LRow.ValuesOwned = false;
										LRow.Dispose();
										AProgram.Stack.Poke(0, ANewRow);
									}
								}
							}
							finally
							{
								if (AValueFlags != null)
								{
									BitArray LModifiedFlags = ANewRow.EndModifiedContext();
									for (int LIndex = 0; LIndex < LModifiedFlags.Length; LIndex++)
										if (LModifiedFlags[LIndex])
											AValueFlags[LIndex] = true;
								}
							}

							if (LColumn.DataType is Schema.ScalarType)
							{
								AProgram.Stack.Push(AOldRow[LRowIndex]);
								try
								{
									AProgram.Stack.Push(ANewRow[LRowIndex]);
									try
									{
										LColumnChanged = ExecuteScalarTypeChangeHandlers(AProgram, (Schema.ScalarType)LColumn.DataType);
											
										if (LColumnChanged)
										{
											ANewRow[LRowIndex] = AProgram.Stack.Peek(0);
											if (AValueFlags != null)
												AValueFlags[LRowIndex] = true;
											LChanged = true;
										}
									}
									finally
									{
										AProgram.Stack.Pop();
									}
								}
								finally
								{
									AProgram.Stack.Pop();
								}
							}
						}
					}
					return LChanged;
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}
		}
		
		public static bool ExecuteValidateHandlers(Program AProgram, Schema.EventHandlers AHandlers)
		{
			return ExecuteValidateHandlers(AProgram, AHandlers, null);
		}
		
		public static bool ExecuteValidateHandlers(Program AProgram, Schema.EventHandlers AHandlers, Schema.Operator AFromOperator)
		{
			bool LChanged = false;
			foreach (Schema.EventHandler LEventHandler in AHandlers)
				if (((LEventHandler.EventType & EventType.Validate) != 0) && ((AFromOperator == null) || (AFromOperator.Name != LEventHandler.Operator.Name)))
				{
					object LResult = LEventHandler.PlanNode.Execute(AProgram);
					LChanged = ((LResult != null) && (bool)LResult) || LChanged;
				}
			return LChanged;
		}
		
		public static bool ExecuteScalarTypeValidateHandlers(Program AProgram, Schema.ScalarType AScalarType)
		{
			return ExecuteScalarTypeValidateHandlers(AProgram, AScalarType, null);
		}
		
		public static bool ExecuteScalarTypeValidateHandlers(Program AProgram, Schema.ScalarType AScalarType, Schema.Operator AFromOperator)
		{
			bool LChanged = false;
			if (AScalarType.HasHandlers())
				LChanged = ExecuteValidateHandlers(AProgram, AScalarType.EventHandlers, AFromOperator);
			#if USETYPEINHERITANCE
			foreach (Schema.ScalarType LParentType in AScalarType.ParentTypes)
				LChanged = ExecuteScalarTypeValidateHandlers(AProgram, LParentType) || LChanged;
			#endif
			return LChanged;
		}
		
		// ValidateColumns
		public static bool ValidateColumns(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (AColumnName != String.Empty)
			{
				// if the call is made for a specific column, the validation is coming in from a proposable call and should be allowed to be empty
				int LRowIndex = ANewRow.DataType.Columns.IndexOfName(AColumnName);
				if (ANewRow.HasValue(LRowIndex))
				{
					AProgram.Stack.Push(AOldRow);
					try
					{
						AProgram.Stack.Push(ANewRow);
						try
						{
							bool LChanged = false;
							Schema.TableVarColumn LColumn = ATableVar.Columns[ATableVar.Columns.IndexOfName(AColumnName)];
							if (LColumn.HasHandlers())
								LChanged = ExecuteValidateHandlers(AProgram, LColumn.EventHandlers);
							int LOldRowIndex = AOldRow == null ? -1 : AOldRow.DataType.Columns.IndexOfName(AColumnName);
							AProgram.Stack.Push(LOldRowIndex >= 0 ? AOldRow[LOldRowIndex] : null);
							try
							{
								AProgram.Stack.Push(ANewRow[LRowIndex]);
								try
								{
									bool LColumnChanged;
									if (LColumn.DataType is Schema.ScalarType)
										LColumnChanged = ExecuteScalarTypeValidateHandlers(AProgram, (Schema.ScalarType)LColumn.DataType);
									else
										LColumnChanged = false;

									if (LColumnChanged)
									{
										ANewRow[LRowIndex] = AProgram.Stack.Peek(0);
										LChanged = true;
									}

									ValidateColumnConstraints(AProgram, LColumn, AIsDescending);
									if (LColumn.DataType is Schema.ScalarType)
										ValidateScalarTypeConstraints(AProgram, (Schema.ScalarType)LColumn.DataType, AIsDescending);
									return LChanged;
								}
								finally
								{
									AProgram.Stack.Pop();
								}
							}
							finally
							{
								AProgram.Stack.Pop();
							}
						}
						finally
						{
							AProgram.Stack.Pop();
						}
					}
					finally
					{
						AProgram.Stack.Pop();
					}
				}
				return false;
			}
			else
			{
				// If there is no column name, this call is the validation for an insert, and should only be allowed to have no value if the column is nilable
				int LRowIndex;
				AProgram.Stack.Push(AOldRow);
				try
				{
					AProgram.Stack.Push(ANewRow);
					try
					{
						bool LChanged = false;
						bool LColumnChanged;
						foreach (Schema.TableVarColumn LColumn in ATableVar.Columns)
						{
							LRowIndex = ANewRow.DataType.Columns.IndexOfName(LColumn.Name);
							if (LRowIndex >= 0)
							{
 								if (ANewRow.HasValue(LRowIndex) || ((AValueFlags == null) || AValueFlags[LRowIndex]))
								{
									if (LColumn.HasHandlers())
									{
										if (AValueFlags != null)
											ANewRow.BeginModifiedContext();
										try
										{
											if (ExecuteValidateHandlers(AProgram, LColumn.EventHandlers))
												LChanged = true;
										}
										finally
										{
											if (AValueFlags != null)
											{
												BitArray LModifiedFlags = ANewRow.EndModifiedContext();
												for (int LIndex = 0; LIndex < LModifiedFlags.Length; LIndex++)
													if (LModifiedFlags[LIndex])
														AValueFlags[LIndex] = true;
											}
										}
									}

									int LOldRowIndex = AOldRow == null ? -1 : AOldRow.DataType.Columns.IndexOfName(LColumn.Name);
									AProgram.Stack.Push(LOldRowIndex >= 0 ? AOldRow[LOldRowIndex] : null);
									try
									{
										AProgram.Stack.Push(ANewRow[LRowIndex]);
										try
										{
											if (LColumn.DataType is Schema.ScalarType)
												LColumnChanged = ExecuteScalarTypeValidateHandlers(AProgram, (Schema.ScalarType)LColumn.DataType);
											else
												LColumnChanged = false;

											if (LColumnChanged)
											{
												ANewRow[LRowIndex] = AProgram.Stack.Peek(0);
												if (AValueFlags != null)
													AValueFlags[LRowIndex] = true;
												LChanged = true;
											}

											ValidateColumnConstraints(AProgram, LColumn, AIsDescending);
											if (LColumn.DataType is Schema.ScalarType)
												ValidateScalarTypeConstraints(AProgram, (Schema.ScalarType)LColumn.DataType, AIsDescending);
										}
										finally
										{
											AProgram.Stack.Pop();
										}
									}
									finally
									{
										AProgram.Stack.Pop();
									}
								}

								if (!ANewRow.HasValue(LRowIndex) && !AIsProposable && (ATableVar is Schema.BaseTableVar) && !LColumn.IsNilable)
									throw new RuntimeException(RuntimeException.Codes.ColumnValueRequired, ErrorSeverity.User, LColumn.Name);
							}
						}
						return LChanged;
					}
					finally
					{
						AProgram.Stack.Pop();
					}
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}	 
		}
		
		// ValidateScalarTypeConstraints
		public static void ValidateScalarTypeConstraints(Program AProgram, Schema.ScalarType AScalarType, bool AIsDescending)
		{
			Schema.ScalarTypeConstraint LConstraint;
			for (int LIndex = 0; LIndex < AScalarType.Constraints.Count; LIndex++)
			{
				LConstraint = AScalarType.Constraints[LIndex];
				if (AIsDescending || LConstraint.Enforced)
					LConstraint.Validate(AProgram, Schema.Transition.Insert);
			}
			
			#if USETYPEINHERITANCE	
			foreach (Schema.ScalarType LParentType in AScalarType.ParentTypes)
				ValidateScalarTypeConstraints(AProgram, LParentType, AIsDescending);
			#endif
		}
		
		// ValidateColumnConstraints
		// This method expects that the value of the column to be validated is at location 0 on the stack
		public static void ValidateColumnConstraints(Program AProgram, Schema.TableVarColumn AColumn, bool AIsDescending)
		{
			foreach (Schema.TableVarColumnConstraint LConstraint in AColumn.Constraints)
				if (AIsDescending || LConstraint.Enforced)
					LConstraint.Validate(AProgram, Schema.Transition.Insert);
		}
		
		// ValidateImmediateConstraints
		// This method expects that the row to be validated is at location 0 on the stack
		protected virtual void ValidateImmediateConstraints(Program AProgram, bool AIsDescending, BitArray AValueFlags)
		{
			Schema.RowConstraint LConstraint;
			for (int LIndex = 0; LIndex < TableVar.RowConstraints.Count; LIndex++)
			{
				LConstraint = TableVar.RowConstraints[LIndex];
				if ((LConstraint.ConstraintType != Schema.ConstraintType.Database) && (AIsDescending || LConstraint.Enforced) && LConstraint.ShouldValidate(AValueFlags, Schema.Transition.Insert))
					LConstraint.Validate(AProgram, Schema.Transition.Insert);
			}
		} 
		
		// ValidateCatalogConstraints
		// This method does not have any expectations for the stack
		protected virtual void ValidateCatalogConstraints(Program AProgram)
		{
			foreach (Schema.CatalogConstraint LConstraint in TableVar.CatalogConstraints)
			{
				if (LConstraint.Enforced)
				{
					if (LConstraint.IsDeferred && AProgram.ServerProcess.InTransaction)
					{
						bool LHasCheck = false;
						for (int LIndex = 0; LIndex < AProgram.ServerProcess.Transactions.Count; LIndex++)
							if (AProgram.ServerProcess.Transactions[LIndex].CatalogConstraints.Contains(LConstraint.Name))
							{
								LHasCheck = true;
								break;
							}
							
						if (!LHasCheck)
							AProgram.ServerProcess.CurrentTransaction.CatalogConstraints.Add(LConstraint);
					}
					else
						LConstraint.Validate(AProgram);
				}
			}
		}
		
		// ShouldValidateKeyConstraints
		// Allows descendent nodes to indicate whether or not key constraints should be checked.
		// Ths method is overridden by the TableVarNode to indicate that key constraints should not be checked if propagation is not occurring.
		protected virtual bool ShouldValidateKeyConstraints(Schema.Transition ATransition)
		{
			return true;
		}
		
		// ValidateInsertConstraints
		// This method expects that the stack contains the new row at location 0 on the stack
		protected virtual void ValidateInsertConstraints(Program AProgram)
		{
			if (AProgram.ServerProcess.InTransaction && TableVar.HasDeferredConstraints())
				AProgram.ServerProcess.AddInsertTableVarCheck(TableVar, (Row)AProgram.Stack.Peek(0));

			#if !USENAMEDROWVARIABLES
			PushRow(AProgram, (Row)AProgram.Stack.Peek(0));
			try
			#endif		
			{	
				Schema.RowConstraint LConstraint;
				for (int LIndex = 0; LIndex < TableVar.RowConstraints.Count; LIndex++)
				{
					LConstraint = TableVar.RowConstraints[LIndex];
					if (LConstraint.Enforced && (!AProgram.ServerProcess.InTransaction || !LConstraint.IsDeferred))
						LConstraint.Validate(AProgram, Schema.Transition.Insert);
				}
			}
			#if !USENAMEDROWVARIABLES
			finally
			{
				PopRow(AProgram);
			}
			#endif
	
			if (TableVar.InsertConstraints.Count > 0)
			{
				Schema.TransitionConstraint LConstraint;
				bool LShouldValidateKeyConstraints = ShouldValidateKeyConstraints(Schema.Transition.Insert);
				for (int LIndex = 0; LIndex < TableVar.InsertConstraints.Count; LIndex++)
				{
					LConstraint = TableVar.InsertConstraints[LIndex];
					if (LConstraint.Enforced && (!AProgram.ServerProcess.InTransaction || !LConstraint.IsDeferred) && ((LConstraint.ConstraintType != Schema.ConstraintType.Table) || LShouldValidateKeyConstraints))
						LConstraint.Validate(AProgram, Schema.Transition.Insert);
				}
			}
		}
		
		// ValidateUpdateConstraints
		// This method expects that the stack contain the old row in location 1, and the new row in location 0
		protected virtual void ValidateUpdateConstraints(Program AProgram, BitArray AValueFlags)
		{
			if (AProgram.ServerProcess.InTransaction && TableVar.HasDeferredConstraints(AValueFlags, Schema.Transition.Update))
				AProgram.ServerProcess.AddUpdateTableVarCheck(TableVar, (Row)AProgram.Stack.Peek(1), (Row)AProgram.Stack.Peek(0));
			
			#if !USENAMEDROWVARIABLES
			PushRow(AProgram, (Row)AProgram.Stack.Peek(0));
			try
			#endif
			{
				Schema.RowConstraint LConstraint;
				for (int LIndex = 0; LIndex < TableVar.RowConstraints.Count; LIndex++)
				{
					LConstraint = TableVar.RowConstraints[LIndex];
					if (LConstraint.Enforced && (!AProgram.ServerProcess.InTransaction || !LConstraint.IsDeferred) && LConstraint.ShouldValidate(AValueFlags, Schema.Transition.Insert))
						LConstraint.Validate(AProgram, Schema.Transition.Insert);
				}
			}
			#if !USENAMEDROWVARIABLES
			finally
			{
				PopRow(AProgram);
			}
			#endif
	
			if (TableVar.UpdateConstraints.Count > 0)
			{
				bool LShouldValidateKeyConstraints = ShouldValidateKeyConstraints(Schema.Transition.Update);
				Schema.TransitionConstraint LConstraint;
				for (int LIndex = 0; LIndex < TableVar.UpdateConstraints.Count; LIndex++)
				{
					LConstraint = TableVar.UpdateConstraints[LIndex];
					if (LConstraint.Enforced && (!AProgram.ServerProcess.InTransaction || !LConstraint.IsDeferred) && ((LConstraint.ConstraintType != Schema.ConstraintType.Table) || LShouldValidateKeyConstraints) && LConstraint.ShouldValidate(AValueFlags, Schema.Transition.Update))
						LConstraint.Validate(AProgram, Schema.Transition.Update);
				}
			}
		}
		
		// ValidateDeleteConstraints
		// This method expects that the stack contain the old row in location 0
		protected virtual void ValidateDeleteConstraints(Program AProgram)
		{
			if (AProgram.ServerProcess.InTransaction && TableVar.HasDeferredConstraints())
				AProgram.ServerProcess.AddDeleteTableVarCheck(TableVar, (Row)AProgram.Stack.Peek(0));

			foreach (Schema.Constraint LConstraint in TableVar.DeleteConstraints)
				if (LConstraint.Enforced && (!AProgram.ServerProcess.InTransaction || !LConstraint.IsDeferred))
					LConstraint.Validate(AProgram, Schema.Transition.Delete);
		}

		// ExecuteHandlers executes each handler associated with the given event type
		protected virtual void ExecuteHandlers(Program AProgram, EventType AEventType)
		{
			// If the process is in an application transaction, and this is an AT table
				// if we are populating source tables
					// do not fire any handlers, 
				// If we are in an AT replay context, do not invoke any handler that was invoked within the AT
				// otherwise record that the handler was invoked in this AT
			ApplicationTransaction LTransaction = null;
			if (AProgram.ServerProcess.ApplicationTransactionID != Guid.Empty)
				LTransaction = AProgram.ServerProcess.GetApplicationTransaction();
			try
			{
				if ((LTransaction == null) || !LTransaction.IsPopulatingSource)
				{
					foreach (Schema.TableVarEventHandler LHandler in TableVar.EventHandlers)
						if (LHandler.EventType == AEventType)
						{
							bool LInvoked = false;
							if ((LTransaction == null) || !LTransaction.InATReplayContext || !LTransaction.WasInvoked(LHandler))
							{
								if (LHandler.IsDeferred && AProgram.ServerProcess.InTransaction)
									switch (AEventType)
									{
										case EventType.AfterInsert : AProgram.ServerProcess.CurrentTransaction.AddInsertHandler(LHandler, (Row)AProgram.Stack.Peek(0)); LInvoked = true; break;
										case EventType.AfterUpdate : AProgram.ServerProcess.CurrentTransaction.AddUpdateHandler(LHandler, (Row)AProgram.Stack.Peek(1), (Row)AProgram.Stack.Peek(0)); LInvoked = true; break;
										case EventType.AfterDelete : AProgram.ServerProcess.CurrentTransaction.AddDeleteHandler(LHandler, (Row)AProgram.Stack.Peek(0)); LInvoked = true; break;
										default : break; // only after handlers should be deferred to transaction commit
									}

								if (!LInvoked)
								{
									AProgram.ServerProcess.PushHandler();
									try
									{
										LHandler.PlanNode.Execute(AProgram);
									}
									finally
									{
										AProgram.ServerProcess.PopHandler();
									}
								}
								
								// BTR 2/24/2004 ->
								// The InHandler check here is preventing event handler invocations within an A/T from being recorded if the event handler
								// is invoked from within another event handler.  This behavior is incorrect, and I cannot understand why it matters whether
								// or not an event handler was invoked from within another event handler.  If the handler is invoked during an A/T, it should
								// be recorded as invoked, and not invoked during the replay, end of story.
								//if ((LTransaction != null) && !LTransaction.InATReplayContext && !AProgram.InHandler && !LTransaction.InvokedHandlers.Contains(LHandler))
								if ((LTransaction != null) && !LTransaction.InATReplayContext && !LTransaction.InvokedHandlers.Contains(LHandler))
									LTransaction.InvokedHandlers.Add(LHandler);
							}
						}
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}
		
		// ExecuteValidateHandlers prepares the stack and executes each handler associated with the validate event
		protected virtual bool ExecuteValidateHandlers(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (TableVar.HasHandlers(EventType.Validate))
			{
				PushRow(AProgram, AOldRow);
				try
				{
					PushRow(AProgram, ANewRow);
					try
					{
						AProgram.Stack.Push(AColumnName);
						try
						{
							bool LChanged = false;
							foreach (Schema.EventHandler LHandler in TableVar.EventHandlers)
								if ((LHandler.EventType & EventType.Validate) != 0)
								{
									if (AValueFlags != null)
										ANewRow.BeginModifiedContext();
									try
									{
										object LObject = LHandler.PlanNode.Execute(AProgram);
										if ((LObject != null) && (bool)LObject)
											LChanged = true;
									}
									finally
									{
										if (AValueFlags != null)
										{
											BitArray LModifiedFlags = ANewRow.EndModifiedContext();
											for (int LIndex = 0; LIndex < LModifiedFlags.Length; LIndex++)
												if (LModifiedFlags[LIndex])
													AValueFlags[LIndex] = true;
										}
									}
								}
							return LChanged;
						}
						finally
						{
							AProgram.Stack.Pop();
						}
					}
					finally
					{
						PopRow(AProgram);
					}
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			return false;
		}
		
		// ExecuteDefaultHandlers prepares the stack and executes each handler associated with the default event
		protected virtual bool ExecuteDefaultHandlers(Program AProgram, Row ARow, BitArray AValueFlags, string AColumnName)
		{
			if (TableVar.HasHandlers(EventType.Default))
			{
				PushRow(AProgram, ARow);
				try
				{
					AProgram.Stack.Push(AColumnName);
					try
					{
						bool LChanged = false;
						foreach (Schema.EventHandler LHandler in TableVar.EventHandlers)
							if ((LHandler.EventType & EventType.Default) != 0)
							{
								if (AValueFlags != null)
									ARow.BeginModifiedContext();
								try
								{
									object LResult = LHandler.PlanNode.Execute(AProgram);
									if ((LResult != null) && (bool)LResult)
										LChanged = true;
								}
								finally
								{
									if (AValueFlags != null)
									{
										BitArray LModifiedFlags = ARow.EndModifiedContext();
										for (int LIndex = 0; LIndex < LModifiedFlags.Length; LIndex++)
											if (LModifiedFlags[LIndex])
												AValueFlags[LIndex] = true;
									}
								}
							}

						return LChanged;
					}
					finally
					{
						AProgram.Stack.Pop();
					}
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			return false;
		}
		
		protected virtual bool ExecuteChangeHandlers(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (TableVar.HasHandlers(EventType.Change))
			{
				PushRow(AProgram, AOldRow);
				try
				{
					PushRow(AProgram, ANewRow);
					try
					{
						AProgram.Stack.Push(AColumnName);
						try
						{
							bool LChanged = false;
							foreach (Schema.EventHandler LHandler in TableVar.EventHandlers)
								if ((LHandler.EventType & EventType.Change) != 0)
								{
									if (AValueFlags != null)
										ANewRow.BeginModifiedContext();
									try
									{
										object LResult = LHandler.PlanNode.Execute(AProgram);
										if ((LResult != null) && (bool)LResult)
											LChanged = true;
									}
									finally
									{
										if (AValueFlags != null)
										{
											BitArray LModifiedFlags = ANewRow.EndModifiedContext();
											for (int LIndex = 0; LIndex < LModifiedFlags.Length; LIndex++)
												if (LModifiedFlags[LIndex])
													AValueFlags[LIndex] = true;
										}
									}
								}

							return LChanged;
						}
						finally
						{
							AProgram.Stack.Pop();
						}
					}
					finally
					{
						PopRow(AProgram);
					}
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			return false;
		}
		
		protected InsertNode FInsertNode;
		/// <summary>Used as a compiled insert statement to be executed against the device if ModifySupported is true.</summary>		
		public InsertNode InsertNode
		{
			get { return FInsertNode; }
			set { FInsertNode = value; }
		}

		protected UpdateNode FUpdateNode;
		/// <summary>Used as a compiled update statement to be executed against the device if ModifySupported is true.</summary>		
		public UpdateNode UpdateNode
		{
			get { return FUpdateNode; }
			set { FUpdateNode = value; }
		}

		protected DeleteNode FDeleteNode;
		/// <summary>Used as a compiled delete statement to be executed against the device if ModifySupported is true.</summary>		
		public DeleteNode DeleteNode
		{
			get { return FDeleteNode; }
			set { FDeleteNode = value; }
		}
		
		protected PlanNode FSelectNode;
		/// <summary>Used to perform an optimistic concurrency check for processor handled updates.</summary>
		public PlanNode SelectNode
		{
			get { return FSelectNode; }
			set { FSelectNode = value; }
		}
		
		protected PlanNode FFullSelectNode;
		/// <summary>Used to select the row with a restriction on the entire row (at least columns of a type that has an equality operator), not just the key columns.</summary>
		public PlanNode FullSelectNode
		{
			get { return FFullSelectNode; }
			set { FFullSelectNode = value; }
		}
		
        // Insert
        protected virtual void ExecuteDeviceInsert(Program AProgram, Row ARow)
        {
			CheckModifySupported();
			EnsureModifyNodes(AProgram.Plan);
			
			// Create a table constructor node to serve as the source for the insert
			TableSelectorNode LSourceNode = new TableSelectorNode(new Schema.TableType());
			RowSelectorNode LRowNode = new RowSelectorNode(ARow.DataType);
			LSourceNode.Nodes.Add(LRowNode);
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
			{
				if (ARow.HasValue(LIndex))
					LRowNode.Nodes.Add(new ValueNode(ARow.DataType.Columns[LIndex].DataType, ARow[LIndex]));
				else
					LRowNode.Nodes.Add(new ValueNode(ARow.DataType.Columns[LIndex].DataType, null));
				LSourceNode.DataType.Columns.Add(ARow.DataType.Columns[LIndex].Copy());
			}

			// Insert the table constructor as the source node for the insert template
			FInsertNode.Nodes.Add(LSourceNode);
			FInsertNode.Nodes.Add(this);
			try
			{	
				AProgram.DeviceExecute(FDevice, FInsertNode);
			}
			finally
			{
				// Remove the table constructor
				FInsertNode.Nodes.Remove(this);
				FInsertNode.Nodes.Remove(LSourceNode);
			}
        }
        
		// Update
		protected virtual void ExecuteDeviceUpdate(Program AProgram, Row AOldRow, Row ANewRow)
		{
			CheckModifySupported();
			EnsureModifyNodes(AProgram.Plan);
			
			// Add update column nodes for each row to be updated
			for (int LColumnIndex = 0; LColumnIndex < ANewRow.DataType.Columns.Count; LColumnIndex++)
			{
				#if USECOLUMNLOCATIONBINDING
				FUpdateNode.Nodes.Add
					(
						new UpdateColumnNode
						(
							ANewRow.DataType.Columns[LColumnIndex].DataType,
							DataType.Columns.IndexOf(ANewRow.DataType.Columns[LColumnIndex].Name),
							new ValueNode(ANewRow.DataType.Columns[LColumnIndex].DataType, ANewRow.HasValue(LColumnIndex) ? ANewRow[LColumnIndex] : null)
						)
					);
				#else
				FUpdateNode.Nodes.Add
					(
						new UpdateColumnNode
						(
							ANewRow.DataType.Columns[LColumnIndex].DataType,
							ANewRow.DataType.Columns[LColumnIndex].Name,
							new ValueNode(ANewRow.DataType.Columns[LColumnIndex].DataType, ANewRow.HasValue(LColumnIndex) ? ANewRow[LColumnIndex] : null)
						)
					);
				#endif
			}
			try
			{
				AProgram.Stack.Push(AOldRow);
				try
				{
					// Execute the Update Statement
					AProgram.DeviceExecute(FDevice, FUpdateNode);
				}
				finally
				{
					AProgram.Stack.Pop();
				}
			}
			finally
			{
				// Remove all the update column nodes
				for (int LColumnIndex = 0; LColumnIndex < ANewRow.DataType.Columns.Count; LColumnIndex++)
					FUpdateNode.Nodes.RemoveAt(FUpdateNode.Nodes.Count - 1);
			}
		}
        
		// Delete
		protected virtual void ExecuteDeviceDelete(Program AProgram, Row ARow)
		{
			CheckModifySupported();
			EnsureModifyNodes(AProgram.Plan);
			
			AProgram.Stack.Push(ARow);
			try
			{
				AProgram.DeviceExecute(FDevice, FDeleteNode);
			}
			finally
			{
				AProgram.Stack.Pop();
			}
		}
        
		protected virtual void InternalBeforeInsert(Program AProgram, Row ARow, BitArray AValueFlags) {}
		protected virtual void InternalAfterInsert(Program AProgram, Row ARow, BitArray AValueFlags) {}
		protected virtual void InternalExecuteInsert(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToPerformInsert);
		}

		protected virtual void InternalBeforeUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags) {}
		protected virtual void InternalAfterUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags) {}
		protected virtual void InternalExecuteUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToPerformUpdate);
		}
		
		protected virtual void InternalBeforeDelete(Program AProgram, Row ARow) {}
		protected virtual void InternalAfterDelete(Program AProgram, Row ARow) {}
		protected virtual void InternalExecuteDelete(Program AProgram, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToPerformDelete);
		}
		
		protected virtual bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable) { return false; }
		protected virtual bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending) { return false; }
		protected virtual bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName) { return false; }
		
		// PopulateNode
		protected TableNode FPopulateNode;
		public TableNode PopulateNode { get { return FPopulateNode; } }

		// PrepareJoinApplicationTransaction
		protected virtual void PrepareJoinApplicationTransaction(Plan APlan)
		{
			// If this process is joined to an application transaction and we are not within a table-valued context
				// join the expression for this node to the application transaction
			if ((APlan.ApplicationTransactionID != Guid.Empty) && !APlan.InTableTypeContext())
			{
				ApplicationTransaction LTransaction = APlan.GetApplicationTransaction();
				try
				{
					if (!LTransaction.IsGlobalContext)
					{
						ApplicationTransactionUtility.PrepareJoinExpression
						(
							APlan, 
							this,
							out FPopulateNode
						);
					}
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		public virtual void InferPopulateNode(Plan APlan) { }

		protected override void InternalBeforeExecute(Program AProgram)
		{
			if ((FPopulateNode != null) && !AProgram.ServerProcess.IsInsert)
				ApplicationTransactionUtility.JoinExpression(AProgram, FPopulateNode, this);
		}
		
		public virtual void JoinApplicationTransaction(Program AProgram, Row ARow) {}

		#region ShowPlan

		public override string Category
		{
			get { return "Table"; }
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter AWriter)
		{
			base.WritePlanAttributes(AWriter);
			AWriter.WriteAttributeString("ShouldChange", TableVar.ShouldChange.ToString().ToLower());
			AWriter.WriteAttributeString("ShouldValidate", TableVar.ShouldValidate.ToString().ToLower());
			AWriter.WriteAttributeString("ShouldDefault", TableVar.ShouldDefault.ToString().ToLower());
			AWriter.WriteAttributeString("IsChangeRemotable", TableVar.IsChangeRemotable.ToString().ToLower());
			AWriter.WriteAttributeString("IsValidateRemotable", TableVar.IsValidateRemotable.ToString().ToLower());
			AWriter.WriteAttributeString("IsDefaultRemotable", TableVar.IsDefaultRemotable.ToString().ToLower());
			AWriter.WriteAttributeString("CursorCapabilities", CursorCapabilitiesToString(CursorCapabilities));
			AWriter.WriteAttributeString("CursorType", CursorType.ToString().ToLower());
			AWriter.WriteAttributeString("RequestedCursorType", RequestedCursorType.ToString().ToLower());
			AWriter.WriteAttributeString("CursorIsolation", CursorIsolation.ToString());
			if (Order != null)
				AWriter.WriteAttributeString("Order", Order.Name);
		}

		protected override void WritePlanNodes(System.Xml.XmlWriter AWriter)
		{
			WritePlanTags(AWriter, TableVar.MetaData);
			WritePlanKeys(AWriter);
			WritePlanOrders(AWriter);
			WritePlanConstraints(AWriter);
			WritePlanReferences(AWriter);
			WritePlanColumns(AWriter);
			base.WritePlanNodes(AWriter);
		}

		protected virtual void WritePlanKeys(System.Xml.XmlWriter AWriter)
		{
			foreach (Schema.Key LKey in TableVar.Keys)
			{
				AWriter.WriteStartElement("Keys.Key");
				AWriter.WriteAttributeString("Name", LKey.Name);
				AWriter.WriteAttributeString("IsSparse", Convert.ToString(LKey.IsSparse));
				WritePlanTags(AWriter, LKey.MetaData);
				AWriter.WriteEndElement();
			}
		}

		protected virtual void WritePlanOrders(System.Xml.XmlWriter AWriter)
		{
			foreach (Schema.Order LOrder in TableVar.Orders)
			{
				AWriter.WriteStartElement("Orders.Order");
				AWriter.WriteAttributeString("Name", LOrder.Name);
				WritePlanTags(AWriter, LOrder.MetaData);
				AWriter.WriteEndElement();
			}
		}

		protected virtual void WritePlanConstraints(System.Xml.XmlWriter AWriter)
		{
			foreach (Schema.TableVarConstraint LConstraint in TableVar.Constraints)
			{
				if (!LConstraint.IsGenerated)
				{
					if (LConstraint is Schema.RowConstraint)
					{
						AWriter.WriteStartElement("Constraints.RowConstraint");
						AWriter.WriteAttributeString("Expression", ((Schema.RowConstraint)LConstraint).Node.SafeEmitStatementAsString());
					}
					else
					{
						Schema.TransitionConstraint LTransitionConstraint = (Schema.TransitionConstraint)LConstraint;
						AWriter.WriteStartElement("Constraints.TransitionConstraint");
						if (LTransitionConstraint.OnInsertNode != null)
							AWriter.WriteAttributeString("OnInsert", LTransitionConstraint.OnInsertNode.SafeEmitStatementAsString());
						if (LTransitionConstraint.OnUpdateNode != null)
							AWriter.WriteAttributeString("OnUpdate", LTransitionConstraint.OnUpdateNode.SafeEmitStatementAsString());
						if (LTransitionConstraint.OnDeleteNode != null)
							AWriter.WriteAttributeString("OnDelete", LTransitionConstraint.OnDeleteNode.SafeEmitStatementAsString());
					}
					AWriter.WriteAttributeString("Name", LConstraint.Name);
					WritePlanTags(AWriter, LConstraint.MetaData);
					AWriter.WriteEndElement();
				}
			}
		}

		protected static string EmitColumnList(Schema.Key AKey)
		{
			StringBuilder LResult = new StringBuilder();
			LResult.Append("{ ");
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LResult.Append(", ");
				LResult.AppendFormat("{0}", AKey.Columns[LIndex].Name);
			}
			if (AKey.Columns.Count > 0)
				LResult.Append(" ");
			LResult.Append("}");
			return LResult.ToString();
		}
		
		protected virtual void WritePlanReference(System.Xml.XmlWriter AWriter, Schema.Reference AReference, bool AIsSource)
		{
			if (AIsSource)
			{
				AWriter.WriteStartElement("SourceReferences.Reference");
				AWriter.WriteAttributeString("Target", AReference.TargetTable.Name);
			}
			else
			{
				AWriter.WriteStartElement("TargetReferences.Reference");
				AWriter.WriteAttributeString("Source", AReference.SourceTable.Name);
			}
			AWriter.WriteAttributeString("IsDerived", Convert.ToString(AReference.ParentReference != null));
			AWriter.WriteAttributeString("Name", AReference.Name);
			AWriter.WriteAttributeString("SourceColumns", EmitColumnList(AReference.SourceKey));
			AWriter.WriteAttributeString("TargetColumns", EmitColumnList(AReference.TargetKey));
			AWriter.WriteAttributeString("IsExcluded", Convert.ToString(AReference.IsExcluded));
			AWriter.WriteAttributeString("OriginatingReferenceName", AReference.OriginatingReferenceName());
			WritePlanTags(AWriter, AReference.MetaData);
			AWriter.WriteEndElement();
		}
		
		protected virtual void WritePlanReferences(System.Xml.XmlWriter AWriter)
		{
			foreach (Schema.Reference LReference in TableVar.SourceReferences)
				WritePlanReference(AWriter, LReference, true);
			
			foreach (Schema.Reference LReference in TableVar.TargetReferences)
				WritePlanReference(AWriter, LReference, false);
		}

		protected virtual void WritePlanColumns(System.Xml.XmlWriter AWriter)
		{
			foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
			{
				AWriter.WriteStartElement("Columns.Column");
				AWriter.WriteAttributeString("Name", LColumn.Name);
				AWriter.WriteAttributeString("Type", LColumn.DataType.Name);
				if (LColumn.IsNilable)
					AWriter.WriteAttributeString("Nilable", "true");
				if (LColumn.ReadOnly)
					AWriter.WriteAttributeString("ReadOnly", "true");
				if (LColumn.IsComputed)
					AWriter.WriteAttributeString("Computed", "true");
				if (!LColumn.ShouldChange)
					AWriter.WriteAttributeString("ShouldChange", "false");
				if (!LColumn.ShouldValidate)
					AWriter.WriteAttributeString("ShouldValidate", "false");
				if (!LColumn.ShouldDefault)
					AWriter.WriteAttributeString("ShouldDefault", "false");
				if (LColumn.Default != null)
				{
					AWriter.WriteStartElement("Default.Default");
					AWriter.WriteAttributeString("Expression", LColumn.Default.Node.SafeEmitStatementAsString());
					WritePlanTags(AWriter, LColumn.MetaData);
					AWriter.WriteEndElement();
				}
				WritePlanTags(AWriter, LColumn.MetaData);
				AWriter.WriteEndElement();
			}
		}

		#endregion
	}
}
