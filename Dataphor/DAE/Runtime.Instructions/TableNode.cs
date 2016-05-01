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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Compiling.Visitors;
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

	public class PrepareJoinApplicationTransactionVisitor : PlanNodeVisitor
	{
		public override void PostOrderVisit(Plan plan, PlanNode node)
		{
			var tableNode = node as TableNode;
			if (tableNode != null)
			{
				tableNode.PrepareJoinApplicationTransaction(plan);
			}
		}
	}

	public abstract class TableNode : InstructionNodeBase
	{        
		// constructor
		public TableNode() : base() 
		{
			IsBreakable = false; // TODO: Debug table nodes? 
			ExpectsTableValues = false;
		}
		
		protected Schema.TableVarColumn CopyTableVarColumn(Schema.TableVarColumn column)
		{
			return column.Inherit();
		}
		
		protected Schema.TableVarColumn CopyTableVarColumn(Schema.TableVarColumn column, string columnName)
		{
			return column.InheritAndRename(columnName);
		}
		
		protected void CopyTableVarColumns(Schema.TableVarColumns columns)
		{
			CopyTableVarColumns(columns, false);
		}
		
        protected void CopyTableVarColumns(Schema.TableVarColumns columns, bool isNilable)
        {
			// Columns
			Schema.TableVarColumn newTableVarColumn;
			foreach (Schema.TableVarColumn tableVarColumn in columns)
			{
				newTableVarColumn = CopyTableVarColumn(tableVarColumn);
				newTableVarColumn.IsNilable = newTableVarColumn.IsNilable || isNilable;
				DataType.Columns.Add(newTableVarColumn.Column);
				TableVar.Columns.Add(newTableVarColumn);
			}
		}
		
		protected Schema.Key CopyKey(Schema.Key key, bool isSparse)	  
		{
			Schema.Key localKey = new Schema.Key();
			localKey.InheritMetaData(key.MetaData);
			localKey.IsInherited = true;
			localKey.IsSparse = key.IsSparse || isSparse;
			foreach (Schema.TableVarColumn column in key.Columns)
				localKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
			return localKey;
		}
        
		protected Schema.Key CopyKey(Schema.Key key)
		{
			return CopyKey(key, false);
		}
		
        protected void CopyKeys(Schema.Keys keys, bool isSparse)
        {
            foreach (Schema.Key key in keys)
				if (!(TableVar.Keys.Contains(key)))
					TableVar.Keys.Add(CopyKey(key, isSparse));
        }
        
        protected void CopyKeys(Schema.Keys keys)
        {
			CopyKeys(keys, false);
        }
        
        protected void CopyPreservedKeys(Schema.Keys keys, bool isSparse, bool preserveSparse)
        {
			bool addKey;
			foreach (Schema.Key key in keys)
			{
				if (preserveSparse || !key.IsSparse)
				{
					addKey = true;
					foreach (Schema.TableVarColumn keyColumn in key.Columns)
					{
						addKey = TableVar.Columns.ContainsName(keyColumn.Name);
						if (!addKey)
							break;
					}
					if (addKey && !TableVar.Keys.Contains(key))
						TableVar.Keys.Add(CopyKey(key, isSparse));
				}
			}
        }
        
        protected void CopyPreservedKeys(Schema.Keys keys)
        {
			CopyPreservedKeys(keys, false, true);
        }
        
        protected void RemoveSuperKeys()
        {
			#if REMOVESUPERKEYS
			// Remove super keys
			// BTR 10/4/2006 ->	I am a little concerned that this change will negatively impact elaboration, derivation, and
			// A/T enlistment so I've included it with a define.
			int index = _tableVar.Keys.Count - 1;
			while (index >= 0)
			{
				Schema.Key currentKey = _tableVar.Keys[index];
				int keyIndex = 0;
				while (keyIndex < index)
				{
					if (currentKey.Columns.IsSupersetOf(_tableVar.Keys[keyIndex].Columns) && (currentKey.IsSparse || (!currentKey.IsSparse && !_tableVar.Keys[keyIndex].IsSparse)))
					{
						// if LCurrentKey is sparse, then FTableVar.Keys[LKeyIndex] may or may not be sparse
						// if LCurrentKey is not sparse, FTableVar.Keys[LKeyIndex] must be non-sparse
						_tableVar.Keys.RemoveAt(index);
						break;
					}
					else if (_tableVar.Keys[keyIndex].Columns.IsSupersetOf(currentKey.Columns) && (_tableVar.Keys[keyIndex].IsSparse || (!_tableVar.Keys[keyIndex].IsSparse && !currentKey.IsSparse)))
					{
						// if FTableVar.Keys[LKeyIndex] is sparse, then LCurrentKey may or may not be sparse
						// if FTableVar.Keys[LKeyIndex] is not sparse, FTableVar.Keys[LKeyIndex] must be non-sparse
						_tableVar.Keys.RemoveAt(keyIndex);
						index--;
						continue;
					}
					
					keyIndex++;
				}
				
				index--;
			}
			#else			
			// Remove duplicate keys
			for (int index = FTableVar.Keys.Count - 1; index >= 0; index--)
			{
				Schema.Key currentKey = FTableVar.Keys[index];
				for (int keyIndex = 0; keyIndex < index; keyIndex++)
					if (FTableVar.Keys[keyIndex].Equals(currentKey))
					{
						FTableVar.Keys.RemoveAt(index);
						break;
					}
			}
			#endif
        }
        
        protected Schema.Order CopyOrder(Schema.Order order)
        {
			Schema.Order localOrder = new Schema.Order();
			localOrder.InheritMetaData(order.MetaData);
			localOrder.IsInherited = true;
			Schema.OrderColumn newOrderColumn;
			Schema.OrderColumn orderColumn;
			for (int index = 0; index < order.Columns.Count; index++)
			{
				orderColumn = order.Columns[index];
				newOrderColumn = new Schema.OrderColumn(TableVar.Columns[orderColumn.Column], orderColumn.Ascending, orderColumn.IncludeNils);
				newOrderColumn.Sort = orderColumn.Sort;
				newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
				Error.AssertWarn(newOrderColumn.Sort != null, "Sort is null");
				localOrder.Columns.Add(newOrderColumn);
			}
			return localOrder;
		}
		
        protected void CopyOrders(Schema.Orders orders)
        {
			foreach (Schema.Order order in orders)
				TableVar.Orders.Add(CopyOrder(order));
        }
        
        protected void CopyPreservedOrders(Schema.Orders orders)
        {
			bool addOrder;
			foreach (Schema.Order order in orders)
			{
				addOrder = true;
				Schema.OrderColumn orderColumn;
				for (int index = 0; index < order.Columns.Count; index++)
				{
					orderColumn = order.Columns[index];
					addOrder = TableVar.Columns.ContainsName(orderColumn.Column.Name);
					if (!addOrder)
						break;
				}
				if (addOrder && !TableVar.Orders.Contains(order))
					TableVar.Orders.Add(CopyOrder(order));
			}
        }
        
        protected string DeriveSourceReferenceName(Schema.ReferenceBase reference, int referenceID)
        {
			return DeriveSourceReferenceName(reference, referenceID, reference.SourceKey);
        }
        
        protected string DeriveSourceReferenceName(Schema.ReferenceBase reference, int referenceID, Schema.JoinKey sourceKey)
        {
			StringBuilder name = new StringBuilder(reference.OriginatingReferenceName());
			name.AppendFormat("_{0}", Keywords.Source);
			for (int index = 0; index < sourceKey.Columns.Count; index++)
				name.AppendFormat("_{0}", sourceKey.Columns[index].Column.Name);
			if (name.Length > Schema.Object.MaxObjectNameLength)
				return Schema.Object.GetGeneratedName(name.ToString(), referenceID);
			return name.ToString();
        }
        
        protected string DeriveTargetReferenceName(Schema.ReferenceBase reference, int referenceID)
        {
			return DeriveTargetReferenceName(reference, referenceID, reference.TargetKey);
        }
        
        protected string DeriveTargetReferenceName(Schema.ReferenceBase reference, int referenceID, Schema.JoinKey targetKey)
        {
			StringBuilder name = new StringBuilder(reference.OriginatingReferenceName());
			name.AppendFormat("_{0}", Keywords.Target);
			for (int index = 0; index < targetKey.Columns.Count; index++)
				name.AppendFormat("_{0}", targetKey.Columns[index].Column.Name);
			if (name.Length > Schema.Object.MaxObjectNameLength)
				return Schema.Object.GetGeneratedName(name.ToString(), referenceID);
			return name.ToString();
        }
        
        protected void CopySourceReference(Plan plan, Schema.ReferenceBase reference)
        {
			CopySourceReference(plan, reference, reference.IsExcluded);
        }
        
        protected void CopySourceReference(Plan plan, Schema.ReferenceBase reference, bool isExcluded)
        {
			int newReferenceID = Schema.Object.GetNextObjectID();
			string newReferenceName = DeriveSourceReferenceName(reference, newReferenceID);
			Schema.DerivedReference newReference = new Schema.DerivedReference(newReferenceID, newReferenceName, reference);
			newReference.IsExcluded = isExcluded;
			newReference.InheritMetaData(reference.MetaData);
			// BTR 2015-07-03 -> It's not clear this would ever do anything anyway, so I'm not sure why these were being set.
			//newReference.UpdateReferenceAction = reference.UpdateReferenceAction;
			//newReference.DeleteReferenceAction = reference.DeleteReferenceAction;
			newReference.SourceTable = _tableVar;
			newReference.AddDependency(_tableVar);
			int columnIndex;
			bool preserved = true;
			foreach (Schema.TableVarColumn column in reference.SourceKey.Columns)
			{
				columnIndex = TableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
					newReference.SourceKey.Columns.Add(TableVar.Columns[columnIndex]);
				else
				{
					preserved = false;
					break;
				}
			}

			if (preserved)
			{
				foreach (Schema.Key key in TableVar.Keys)
					if (key.Columns.IsSubsetOf(newReference.SourceKey.Columns))
					{
						newReference.SourceKey.IsUnique = true;
						break;
					}

				newReference.TargetTable = reference.TargetTable;
				newReference.AddDependency(reference.TargetTable);
				newReference.TargetKey.IsUnique = reference.TargetKey.IsUnique;
				foreach (Schema.TableVarColumn column in reference.TargetKey.Columns)
					newReference.TargetKey.Columns.Add(column);

				if (!_tableVar.References.ContainsSourceReference(newReference)) // This would only be true for unions and joins where both sides contain the same reference
				{
					_tableVar.References.Add(newReference);
				}
			}
        }
        
        protected void CopyTargetReference(Plan plan, Schema.ReferenceBase reference)
        {
			CopyTargetReference(plan, reference, reference.IsExcluded);
        }
        
        protected void CopyTargetReference(Plan plan, Schema.ReferenceBase reference, bool isExcluded)
        {
			int newReferenceID = Schema.Object.GetNextObjectID();
			string newReferenceName = DeriveTargetReferenceName(reference, newReferenceID);
			Schema.DerivedReference newReference = new Schema.DerivedReference(newReferenceID, newReferenceName, reference);
			newReference.IsExcluded = isExcluded;
			newReference.InheritMetaData(reference.MetaData);
			//newReference.UpdateReferenceAction = reference.UpdateReferenceAction;
			//newReference.DeleteReferenceAction = reference.DeleteReferenceAction;
			newReference.SourceTable = reference.SourceTable;
			newReference.AddDependency(reference.SourceTable);
			newReference.SourceKey.IsUnique = reference.SourceKey.IsUnique;
			foreach (Schema.TableVarColumn column in reference.SourceKey.Columns)
				newReference.SourceKey.Columns.Add(column);
			newReference.TargetTable = _tableVar;
			newReference.AddDependency(_tableVar);
			int columnIndex;
			bool preserved = true;
			foreach (Schema.TableVarColumn column in reference.TargetKey.Columns)
			{
				columnIndex = TableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
					newReference.TargetKey.Columns.Add(TableVar.Columns[columnIndex]);
				else
				{
					preserved = false;
					break;
				}
			}
			
			if (preserved)
			{
				foreach (Schema.Key key in TableVar.Keys)
				{
					if (key.Columns.IsSubsetOf(newReference.TargetKey.Columns))
					{
						newReference.TargetKey.IsUnique = true;
						break;
					}
				}

				if (newReference.TargetKey.IsUnique && !_tableVar.References.ContainsTargetReference(newReference)) // This would only be true for unions and joins where both sides contain the same reference
				{
					_tableVar.References.Add(newReference);
				}
			}
        }
        
		protected void CopyReferences(Plan plan, Schema.TableVar tableVar)
        {
			if (tableVar.HasReferences())
			{
				foreach (Schema.ReferenceBase reference in tableVar.References)
					if (reference.SourceTable.Equals(tableVar))
						CopySourceReference(plan, reference);
					else if (reference.TargetTable.Equals(tableVar))
						CopyTargetReference(plan, reference);
	        }
		}
        
		// DataType
		public new virtual Schema.ITableType DataType { get { return (Schema.ITableType)_dataType; } }
		
		// Cursor Behaviors:
		// The current CursorContext for the plan contains the requested cursor behaviors.
		// After the DetermineDevice call, the following properties should be set to the 
		// actual cursor behaviors. For device supported nodes, these are set by the device 
		// during the Supports call. For all other table nodes, these are set to the appropriate 
		// values after the chunking has been determined for this node.
		
		// TableVar
		protected Schema.TableVar _tableVar;
		public virtual Schema.TableVar TableVar
		{
			get { return _tableVar; }
			set
			{
				_tableVar = value;
				if (_tableVar != null)
					_dataType = _tableVar.DataType;
			}
		}

		// Order
		private Schema.Order _order;
		public Schema.Order Order
		{
			get { return _order; }
			set { _order = value; }
		}
		
		// CursorType
		protected CursorType _cursorType;
		public CursorType CursorType
		{
			get { return _cursorType; }
			set { _cursorType = value; }
		}
		
		protected CursorType _requestedCursorType;
		public CursorType RequestedCursorType
		{
			get { return _requestedCursorType; }
			set { _requestedCursorType = value; }
		}
		
		// CursorCapabilities
		protected CursorCapability _cursorCapabilities;
		public CursorCapability CursorCapabilities
		{
			get { return _cursorCapabilities; }
			set { _cursorCapabilities = value; }
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newTableNode = (TableNode)newNode;
			newTableNode.TableVar = _tableVar; // BTR -> This is okay because if we ever actually rearrange, we'll need to redo the DetermineDataType call anyway
			newTableNode._requestedCursorType = _requestedCursorType;
			newTableNode.ShouldCheckConcurrency = ShouldCheckConcurrency;
			newTableNode.ShouldSupportModify = ShouldSupportModify;
			if (_order != null)
			{
				newTableNode._order = newTableNode.CopyOrder(_order);
			}
		}
		
        public bool Supports(CursorCapability capability)
        {
			return ((capability & CursorCapabilities) != 0);
        }
        
        public void CheckCapability(CursorCapability capability)
        {
			if (!Supports(capability))
				throw new RuntimeException(RuntimeException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), capability));
        }
        
        public static string CursorCapabilitiesToString(CursorCapability cursorCapabilities)
        {
			StringBuilder result = new StringBuilder();
			bool first = true;
			CursorCapability capability;
			for (int index = 0; index < 7; index++)
			{
				capability = (CursorCapability)Math.Pow(2, index);
				if ((cursorCapabilities & capability) != 0)
				{
					if (!first)
						result.Append(", ");
					else
						first = false;
						
					result.Append(capability.ToString().ToLower());
				}
			}
			return result.ToString();
        }
		
		// CursorIsolation
		protected CursorIsolation _cursorIsolation;
		public CursorIsolation CursorIsolation
		{
			get { return _cursorIsolation; }
			set { _cursorIsolation = value; }
		}
		
		protected override void DetermineModifiers(Plan plan)
		{
			base.DetermineModifiers(plan);

			if (Modifiers != null)
			{			
				ShouldSupportModify = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "ShouldSupportModify", ShouldSupportModify.ToString()));
				ShouldCheckConcurrency = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "ShouldCheckConcurrency", ShouldCheckConcurrency.ToString()));
			}
		}

		public override void DeterminePotentialDevice(Plan plan)
		{
			base.DeterminePotentialDevice(plan);

			if (_populateNode != null)
			{
				_populateNode.DeterminePotentialDevice(plan);
			}
		}
		
		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			if (!DeviceSupported)
				DetermineCursorBehavior(plan);
			_symbols = Compiler.SnapshotSymbols(plan);
			if ((_cursorCapabilities & CursorCapability.Updateable) != 0)
				DetermineModifySupported(plan);
		}

		public override void BindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			base.BindingTraversal(plan, visitor);

			if (_populateNode != null)
			{
				plan.PushGlobalContext();
				try
				{
					#if USEVISIT
					_populateNode = (TableNode)visitor.Visit(plan, _populateNode);
					#else
					_populateNode.BindingTraversal(plan, visitor);
					#endif
				}
				finally
				{
					plan.PopGlobalContext();
				}
			}
		}
		
		public virtual void DetermineRemotable(Plan plan)
		{
			_tableVar.DetermineRemotable(plan.CatalogDeviceSession);
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			//FTableVar.DetermineRemotable();
		}
		
		public virtual void DetermineCursorBehavior(Plan plan) 
		{
			_cursorType = CursorType.Dynamic;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = CursorCapability.Navigable | (plan.CursorContext.CursorCapabilities & CursorCapability.Elaborable);
			_cursorIsolation = plan.CursorContext.CursorIsolation;
		}
		
		// ModifySupported, true if the device supports modification statements for this node
		public bool ModifySupported
		{
			get { return (_characteristics & ModifySupportedFlag) == ModifySupportedFlag; }
			set { if (value) _characteristics |= ModifySupportedFlag; else _characteristics &= NotModifySupportedFlag; }
		}
	
		// ShouldSupportModify, true if the DAE should try to support the modification at this level
		public bool ShouldSupportModify
		{
			get { return (_characteristics & ShouldSupportModifyFlag) == ShouldSupportModifyFlag; }
			set { if (value) _characteristics |= ShouldSupportModifyFlag; else _characteristics &= NotShouldSupportModifyFlag; }
		}
		
		public virtual void DetermineModifySupported(Plan plan)
		{
			if ((TableVar.Keys.Count > 0) && DeviceSupported && ShouldSupportModify)
			{
				// if any child node is a tabletype and not a tablenode  
				//	or is a table node and does not support modification, modification is not supported
				if (NodeCount > 0)
					foreach (PlanNode node in Nodes)
						if 
						(
							((node.DataType is Schema.TableType) && !(node is TableNode)) || 
							((node is TableNode) && !((TableNode)node).ModifySupported)
						)
						{
							ModifySupported = false;
							return;
						}
				
				ModifySupported = false;
				// TODO: Build modification binding cache
				#if USEMODIFICATIONBINDING
				// Use an update to determine whether any modification would be supported against this expression
				UpdateNode updateNode = new UpdateNode();
				LNode.IsBreakable = false;
				updateNode.Nodes.Add(this);
				FModifySupported = FDevice.Supports(APlan, updateNode);
				#endif
			}
			else
				ModifySupported = false;
		}
		
		protected void CheckModifySupported()
		{
			if (!ModifySupported)
				throw new RuntimeException(RuntimeException.Codes.NoSupportingModificationDevice, EmitStatementAsString());
		}

		// Used to snapshot the stack to allow for run-time compilation of the concurrency and update nodes
		protected Symbols _symbols;
		
		protected void PushSymbols(Plan plan, Symbols symbols)
		{
			plan.Symbols.PushWindow(0);
			plan.EnterRowContext();
			for (int index = symbols.Count - 1; index >= 0; index--)
				plan.Symbols.Push(symbols.Peek(index));
		}
		
		protected void PopSymbols(Plan plan, Symbols symbols)
		{
			for (int index = symbols.Count - 1; index >= 0; index--)
				plan.Symbols.Pop();
			plan.ExitRowContext();
			plan.Symbols.PopWindow();
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

		protected Expression GetExpression(bool outsideAT)
		{
			if (outsideAT)
				ApplicationTransactionUtility.ClearExplicitBind(this);
			try
			{
				return (Expression)EmitStatement(EmitMode.ForCopy);
			}
			finally
			{
				if (outsideAT)
					ApplicationTransactionUtility.SetExplicitBind(this);
			}
		}
		
		protected PlanNode CompileSelectNode(Program program, bool fullSelect, bool outsideAT)
		{
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				plan.PushGlobalContext();
				try
				{
					plan.PushATCreationContext();
					try
					{
						PushSymbols(plan, _symbols);
						try
						{
							// Generate a select statement for use in optimistic concurrency checks
							plan.EnterRowContext();
							try
							{
								plan.Symbols.Push(new Symbol("ASelectRow", DataType.RowType));
								try
								{
									plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable | (CursorCapabilities & CursorCapability.Updateable), ((CursorCapabilities & CursorCapability.Updateable) != 0) ? CursorIsolation.Isolated : CursorIsolation.None));
									try
									{
										if (TableVar.Owner != null)
											plan.PushSecurityContext(new SecurityContext(TableVar.Owner));
										try
										{
											Schema.Columns columns;
											if (fullSelect)
												columns = DataType.RowType.Columns;
											else
											{
												columns = new Schema.Columns();
												Schema.Key key = Compiler.FindClusteringKey(plan, TableVar);
												foreach (Schema.TableVarColumn column in key.Columns)
													columns.Add(column.Column);
											}
											return
												Compiler.Compile
												(
													plan,
													new RestrictExpression
													(
														GetExpression(outsideAT),
														Compiler.BuildOptimisticRowEqualExpression
														(
															plan, 
															"",
															"ASelectRow",
															columns
														)
													)
												);
										}
										finally
										{
											if (TableVar.Owner != null)
												plan.PopSecurityContext();
										}
									}
									finally
									{
										plan.PopCursorContext();
									}
								}
								finally
								{
									plan.Symbols.Pop();
								}
							}
							finally
							{
								plan.ExitRowContext();
							}
						}
						finally
						{
							PopSymbols(plan, _symbols);
						}
					}
					finally
					{
						plan.PopATCreationContext();
					}
				}
				finally
				{
					plan.PopGlobalContext();
				}
			}
			finally
			{
				plan.Dispose();
			}
		}
		
		protected void EnsureSelectNode(Program program)
		{
			lock (this)
			{
				if (_selectNode == null)
				{
					_selectNode = CompileSelectNode(program, false, false);
				}
			}
		}
		
		protected void EnsureFullSelectNode(Program program)
		{
			lock (this)
			{
				if (_fullSelectNode == null)
				{
					_fullSelectNode = CompileSelectNode(program, true, false);
				}
			}
		}

		protected void EnsureSelectAllNode(Program program)
		{
			lock (this)
			{
				if (_selectAllNode == null)
				{
					_selectAllNode = CompileSelectNode(program, false, true);
				}
			}
		}
		
		protected void EnsureModifyNodes(Plan plan)
		{
			lock (this)
			{
				if (_insertNode == null)
				{
					// Generate template modification instructions
					plan.EnterRowContext();
					try
					{
						plan.Symbols.Push(new Symbol(String.Empty, DataType.OldRowType));
						try
						{
							plan.Symbols.Push(new Symbol(String.Empty, DataType.RowType));
							try
							{
								if (TableVar.Owner != null)
									plan.PushSecurityContext(new SecurityContext(TableVar.Owner));
								try
								{
									Schema.RowType oldKey = new Schema.RowType(Compiler.FindClusteringKey(plan, TableVar).Columns, Keywords.Old);
									Schema.RowType key = new Schema.RowType(Compiler.FindClusteringKey(plan, TableVar).Columns);
									_insertNode = new InsertNode();
									_insertNode.IsBreakable = false;
									_updateNode = new UpdateNode();
									_updateNode.IsBreakable = false;
									_updateNode.Nodes.Add(Compiler.EmitUpdateConditionNode(plan, this, Compiler.CompileExpression(plan, Compiler.BuildKeyEqualExpression(plan, oldKey.Columns, key.Columns))));
									_updateNode.TargetNode = _updateNode.Nodes[0].Nodes[0];
									_updateNode.ConditionNode = _updateNode.Nodes[0].Nodes[1];
									_deleteNode = new DeleteNode();
									_deleteNode.IsBreakable = false;
									_deleteNode.Nodes.Add(Compiler.EmitRestrictNode(plan, this, Compiler.CompileExpression(plan, Compiler.BuildKeyEqualExpression(plan, oldKey.Columns, key.Columns))));
								}
								finally
								{
									if (TableVar.Owner != null)
										plan.PopSecurityContext();
								}
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.ExitRowContext();
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
		public virtual IRow PrepareNewRow(Program program, IRow oldRow, IRow newRow, ref BitArray valueFlags)
		{
			if (!newRow.DataType.Columns.Equivalent(DataType.Columns))
			{
				IRow row = new Row(program.ValueManager, DataType.RowType);
				BitArray localValueFlags = valueFlags != null ? new BitArray(row.DataType.Columns.Count) : null;
				int columnIndex;
				for (int index = 0; index < row.DataType.Columns.Count; index++)
				{
					columnIndex = newRow.DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
					if (columnIndex >= 0)
					{
						if (newRow.HasValue(columnIndex))
							row[index] = newRow[columnIndex];

						if (localValueFlags != null)
							localValueFlags[index] = valueFlags[columnIndex];
					}
					else
					{
						if (oldRow != null)
						{
							columnIndex = oldRow.DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
							if ((columnIndex >= 0) && oldRow.HasValue(columnIndex))
								row[index] = oldRow[columnIndex];
						}
						
						if (localValueFlags != null)
							localValueFlags[index] = false;
					}
				}
				newRow = row;
				valueFlags = localValueFlags;
			}

			return newRow;
		}
		
		public void PushRow(Program program, IRow row)
		{
			if (row != null)
			{
				Row localRow = new Row(program.ValueManager, DataType.RowType, (NativeRow)row.AsNative);
				program.Stack.Push(localRow);
			}
			else
				program.Stack.Push(null);
		}
		
		public void PushNewRow(Program program, IRow row)
		{
			#if USENAMEDROWVARIABLES
			Row localRow = new Row(program.ValueManager, DataType.RowType, (NativeRow)row.AsNative);
			#else
			Row localRow = new Row(AProgram.ValueManager, DataType.NewRowType, (NativeRow)ARow.AsNative);
			#endif
			program.Stack.Push(localRow);
		}
		
		public void PushOldRow(Program program, IRow row)
		{
			#if USENAMEDROWVARIABLES
			Row oldRow = new Row(program.ValueManager, DataType.RowType, (NativeRow)row.AsNative);
			#else
			Row oldRow = new Row(AProgram.ValueManager, DataType.OldRowType, (NativeRow)ARow.AsNative);
			#endif
			program.Stack.Push(oldRow);
		}

		public void PopRow(Program program)
		{
			IRow localRow = (IRow)program.Stack.Pop();
			if (localRow != null)
				localRow.Dispose();
		}
		
		/// <summary>Selects a row based on the node and the given values, will return null if no row is found.</summary>
		/// <remarks>
		/// Select restricts the result set based on the clustering key of the node, whereas FullSelect restricts
		/// the result set based on all columns declared of a type that has an equality operator defined.
		/// </remarks>
		public IRow Select(Program program, IRow row)
		{
			// Symbols will only be null if this node is not bound
			// The node will not be bound if it is functioning in a local server context evaluating change proposals in the client
			if (_symbols != null)
			{
				EnsureSelectNode(program);
				program.Stack.Push(row);
				try
				{
					using (ITable table = (ITable)_selectNode.Execute(program))
					{
						if (table.Next())
							return table.Select();
						else
							return null;
					}
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			return null;
		}

		public List<IRow> SelectAll(Program program, IRow row)
		{
			var result = new List<IRow>();

			// Symbols will only be null if this node is not bound
			// The node will not be bound if it is functioning in a local server context evaluating change proposals in the client
			if (_symbols != null)
			{
				EnsureSelectAllNode(program);
				program.Stack.Push(row);
				try
				{
					using (ITable table = (ITable)_selectAllNode.Execute(program))
					{
						while (table.Next())
							result.Add(table.Select());
					}
				}
				finally
				{
					program.Stack.Pop();
				}
			}

			return result;
		}

		/// <summary>Selects a row based on the node and the given values, will return null if no row is found.</summary>
		/// <remarks>
		/// Select restricts the result set based on the clustering key of the node, whereas FullSelect restricts
		/// the result set based on all columns declared of a type that has an equality operator defined.
		/// </remarks>
		public IRow FullSelect(Program program, IRow row)
		{
			// Symbols will only be null if this node is not bound
			// The node will not be bound if it is functioning in a local server context evaluating change proposals in the client
			if (_symbols != null)
			{
				EnsureFullSelectNode(program);
				program.Stack.Push(row);
				try
				{
					using (ITable table = (ITable)_fullSelectNode.Execute(program))
					{
						if (table.Next())
							return table.Select();
						else
							return null;
					}
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			return null;
		}

		public bool ShouldCheckConcurrency
		{
			get { return (_characteristics & ShouldCheckConcurrencyFlag) == ShouldCheckConcurrencyFlag; }
			set { if (value) _characteristics |= ShouldCheckConcurrencyFlag; else _characteristics &= NotShouldCheckConcurrencyFlag; }
		}
		
		/// <summary>Performs an optimistic concurrency check for the given row.</summary>
		/// <remarks>
		/// AOldRow may be a different row type than the data type for this node,
		/// but ACurrentRow will always be a row type with the same heading as the table type
		/// for this node.
		/// </remarks>
		protected void CheckConcurrency(Program program, IRow oldRow, IRow currentRow)
		{
			#if !USENATIVECONCURRENCYCOMPARE
			EnsureConcurrencyNodes(AProcess);
			#endif
			object oldValue;
			object currentValue;
			
			bool rowsEqual = true;
			int columnIndex;
			for (int index = 0; index < oldRow.DataType.Columns.Count; index++)
			{
				columnIndex = currentRow.DataType.Columns.IndexOfName(oldRow.DataType.Columns[index].Name);
				if (columnIndex >= 0)
				{
					if (oldRow.HasValue(index))
					{
						if (currentRow.HasValue(columnIndex))
						{
							oldValue = oldRow[index];
							currentValue = currentRow[columnIndex];
							#if USENATIVECONCURRENCYCOMPARE
							rowsEqual = DataValue.NativeValuesEqual(program.ValueManager, oldValue, currentValue);
							#else
							AProcess.Context.Push(oldValue);
							AProcess.Context.Push(currentValue);
							try
							{
								object result = FConcurrencyNodes[columnIndex].Execute(AProcess);
								rowsEqual = (result != null) && (bool)result;
							}
							finally
							{
								AProcess.Context.Pop();
								AProcess.Context.Pop();
							}
							#endif
						}
						else
							rowsEqual = false;
					}
					else
					{
						if (currentRow.HasValue(columnIndex))
							rowsEqual = false;
					}
				}
				
				if (!rowsEqual)
					break;
			}
			
			if (!rowsEqual)
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
		public virtual IRow PrepareOldRow(Program program, IRow row, bool checkConcurrency)
		{
			if ((checkConcurrency && ShouldCheckConcurrency) || !row.DataType.Columns.Equivalent(DataType.Columns))
			{
				// reselect the full row buffer for this row
				bool selectRowReturned = false;
				Row selectRow = new Row(program.ValueManager, DataType.RowType);
				try
				{
					row.CopyTo(selectRow);
					if (((checkConcurrency && ShouldCheckConcurrency) || selectRow.DataType.Columns.IsProperSupersetOf(row.DataType.Columns)) && !program.ServerProcess.ServerSession.Server.IsEngine)
					{
						IRow localRow = Select(program, selectRow);
						if (localRow != null)
						{
							if (checkConcurrency && ShouldCheckConcurrency)
								CheckConcurrency(program, row, localRow);
							else
								row.CopyTo(localRow);

							return localRow;
						}
						else
						{
							if (checkConcurrency && ShouldCheckConcurrency)
								throw new RuntimeException(RuntimeException.Codes.OptimisticConcurrencyCheckRowNotFound, ErrorSeverity.Environment);
								
							selectRowReturned = true;
							return selectRow;
						}
					}
					else
					{
						selectRowReturned = true;
						return selectRow;
					}
				}
				finally
				{
					if (!selectRowReturned)
						selectRow.Dispose();
				}
			}

			return row;
		}

		// If AValueFlags are specified, they indicate whether or not each column of ANewRow was specified as part of the insert.
		public virtual void Insert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			IRow newPreparedRow;
			IRow oldPreparedRow = null;
			if (oldRow != null)
				oldPreparedRow = PrepareOldRow(program, oldRow, false);
			try
			{
				newPreparedRow = PrepareNewRow(program, oldPreparedRow, newRow, ref valueFlags);
			}
			catch
			{
				if (!Object.ReferenceEquals(oldRow, oldPreparedRow))
					oldPreparedRow.Dispose();
				throw;
			}
			
			try
			{
				if (uncheckedValue || ((oldRow == null) && BeforeInsert(program, newPreparedRow, valueFlags)) || ((oldRow != null) && BeforeUpdate(program, oldPreparedRow, newPreparedRow, valueFlags)))
				{
					ExecuteInsert(program, oldPreparedRow, newPreparedRow, valueFlags, uncheckedValue);
					if (!uncheckedValue)
						if (oldRow == null)
							AfterInsert(program, newPreparedRow, valueFlags);
						else
							AfterUpdate(program, oldPreparedRow, newPreparedRow, valueFlags);
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(newRow, newPreparedRow))
					newPreparedRow.Dispose();
				if (!Object.ReferenceEquals(oldRow, oldPreparedRow))
					oldPreparedRow.Dispose();
			}
		}

		protected internal bool BeforeInsert(Program program, IRow row, BitArray valueFlags)
		{
			#if USEPROPOSALEVENTS
			DoBeforeInsert(ARow, AProgram);
			#endif
			PreparedDefault(program, null, row, valueFlags, String.Empty, false);
			bool perform = true;
			if (TableVar.HasHandlers(EventType.BeforeInsert))
			{
				PushRow(program, row);
				try
				{
					object performVar = perform;
					program.Stack.Push(performVar);
					try
					{
						if (valueFlags != null)
							row.BeginModifiedContext();
						try
						{
							ExecuteHandlers(program, EventType.BeforeInsert);
						}
						finally
						{
							if (valueFlags != null)
							{
								BitArray modifiedFlags = row.EndModifiedContext();
								for (int index = 0; index < modifiedFlags.Count; index++)
									if (modifiedFlags[index])
										valueFlags[index] = true;
							}
						}
					}
					finally
					{
						performVar = program.Stack.Pop();
					}
					perform = (performVar != null) && (bool)performVar;
				}
				finally
				{
					PopRow(program);
				}
			}
			
			if (perform)
			{
				PreparedValidate(program, null, row, valueFlags, String.Empty, false, false);
				InternalBeforeInsert(program, row, valueFlags);
				if ((TableVar.HasInsertConstraints()) || (TableVar.HasRowConstraints()) || (program.ServerProcess.InTransaction && TableVar.HasDeferredConstraints()))
				{
					PushNewRow(program, row);
					try
					{
						ValidateInsertConstraints(program);
					}
					finally
					{
						PopRow(program);
					}
				}
			}

			return perform;
		}
		
		protected internal void ExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			InternalExecuteInsert(program, oldRow, newRow, valueFlags, uncheckedValue);
		}
		
		protected internal void AfterInsert(Program program, IRow row, BitArray valueFlags)
		{
			if (TableVar.HasHandlers(EventType.AfterInsert))
			{
				PushRow(program, row);
				try
				{
					ExecuteHandlers(program, EventType.AfterInsert);
				}
				finally
				{
					PopRow(program);
				}
			}
			ValidateCatalogConstraints(program);
			InternalAfterInsert(program, row, valueFlags);
			#if USEPROPOSALEVENTS
			DoAfterInsert(ARow, AProgram);
			#endif
		}
		
		public virtual void Update(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			IRow newPreparedRow;
			IRow oldPreparedRow = PrepareOldRow(program, oldRow, checkConcurrency);
			try
			{
				newPreparedRow = PrepareNewRow(program, oldPreparedRow, newRow, ref valueFlags);
			}
			catch
			{
				if (!Object.ReferenceEquals(oldRow, oldPreparedRow))
					oldPreparedRow.Dispose();
				throw;
			}
			
			try
			{
				if (uncheckedValue || BeforeUpdate(program, oldPreparedRow, newPreparedRow, valueFlags))
				{
					ExecuteUpdate(program, oldPreparedRow, newPreparedRow, valueFlags, checkConcurrency, uncheckedValue);
					if (!uncheckedValue)
						AfterUpdate(program, oldPreparedRow, newPreparedRow, valueFlags);
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(newRow, newPreparedRow))
					newPreparedRow.Dispose();
				if (!Object.ReferenceEquals(oldRow, oldPreparedRow))
					oldPreparedRow.Dispose();
			}
		}
		
		protected internal bool BeforeUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			#if USEPROPOSALEVENTS
			DoBeforeUpdate(AOldRow, ANewRow, AProgram);
			#endif
			bool perform = true;
			if (TableVar.HasHandlers(EventType.BeforeUpdate))
			{
				PushRow(program, oldRow);
				try
				{
					PushRow(program, newRow);
					try
					{
						object performVar = perform;
						program.Stack.Push(performVar);
						try
						{
							if (valueFlags != null)
								newRow.BeginModifiedContext();
							try
							{
								ExecuteHandlers(program, EventType.BeforeUpdate);
							}
							finally
							{
								if (valueFlags != null)
								{
									BitArray modifiedFlags = newRow.EndModifiedContext();
									for (int index = 0; index < modifiedFlags.Count; index++)
										if (modifiedFlags[index])
											valueFlags[index] = true;
								}
							}
						}
						finally
						{
							performVar = program.Stack.Pop();
						}
						perform = (performVar != null) && (bool)performVar;
					}
					finally
					{
						PopRow(program);
					}
				}
				finally
				{
					PopRow(program);
				}
			}
			
			if (perform)
			{
				PreparedValidate(program, oldRow, newRow, valueFlags, String.Empty, false, false);
				InternalBeforeUpdate(program, oldRow, newRow, valueFlags);
				if ((TableVar.HasUpdateConstraints()) || (TableVar.HasRowConstraints()) || (program.ServerProcess.InTransaction && TableVar.HasDeferredConstraints()))
				{
					PushOldRow(program, oldRow);
					try
					{
						PushNewRow(program, newRow);
						try
						{
							ValidateUpdateConstraints(program, valueFlags);
						}
						finally
						{
							PopRow(program);
						}
					}
					finally
					{
						PopRow(program);
					}
				}
			}

			return perform;
		}
		
		protected internal void ExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			InternalExecuteUpdate(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
		}
		
		protected internal void AfterUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			if (TableVar.HasHandlers(EventType.AfterUpdate))
			{
				PushRow(program, oldRow);
				try
				{
					PushRow(program, newRow);
					try
					{
						ExecuteHandlers(program, EventType.AfterUpdate);
					}
					finally
					{
						PopRow(program);
					}
				}
				finally
				{
					PopRow(program);
				}
			}

			ValidateCatalogConstraints(program);
			InternalAfterUpdate(program, oldRow, newRow, valueFlags);
			#if USEPROPOSALEVENTS
			DoAfterUpdate(AOldRow, ANewRow, AProgram);
			#endif
		}
		
		public virtual void Delete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			IRow preparedRow = PrepareOldRow(program, row, false);
			try
			{
				if (uncheckedValue || BeforeDelete(program, preparedRow))
				{
					ExecuteDelete(program, preparedRow, checkConcurrency, uncheckedValue);
					if (!uncheckedValue)
						AfterDelete(program, preparedRow);
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(preparedRow, row))
					preparedRow.Dispose();
			}
		}
		
		protected internal bool BeforeDelete(Program program, IRow row)
		{
			#if USEPROPOSALEVENTS
			DoBeforeDelete(ARow, AProgram);
			#endif
			bool perform = true;
			if (TableVar.HasHandlers(EventType.BeforeDelete))
			{
				PushRow(program, row);
				try
				{
					object performVar = perform;
					program.Stack.Push(performVar);
					try
					{
						ExecuteHandlers(program, EventType.BeforeDelete);
					}
					finally
					{
						performVar = program.Stack.Pop();
					}
					perform = (performVar != null) && (bool)performVar;
				}
				finally
				{
					PopRow(program);
				}
			}
			
			if (perform)
			{
				InternalBeforeDelete(program, row);
				if ((TableVar.HasDeleteConstraints()) || (program.ServerProcess.InTransaction && TableVar.HasDeferredConstraints()))
				{
					PushOldRow(program, row);
					try
					{
						ValidateDeleteConstraints(program);
					}
					finally
					{
						PopRow(program);
					}
				}
			}

			return perform;
		}
		
		protected internal void ExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			InternalExecuteDelete(program, row, checkConcurrency, uncheckedValue);
		}
		
		protected internal void AfterDelete(Program program, IRow row)
		{
			if (TableVar.HasHandlers(EventType.AfterDelete))
			{
				PushRow(program, row);
				try
				{
					ExecuteHandlers(program, EventType.AfterDelete);
				}
				finally
				{
					PopRow(program);
				}
			}
			ValidateCatalogConstraints(program);
			InternalAfterDelete(program, row);
			#if USEPROPOSALEVENTS
			DoAfterDelete(ARow, AProgram);
			#endif
		}
		
		public bool ShouldValidate(string columnName)
		{
			if (columnName == String.Empty)
				return _tableVar.ShouldValidate;
			return _tableVar.ShouldValidate || _tableVar.Columns[_tableVar.Columns.IndexOfName(columnName)].ShouldValidate;
		}
		
		public virtual bool Validate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (ShouldValidate(columnName))
			{
				IRow oldPreparedRow = oldRow == null ? null : PrepareOldRow(program, oldRow, false);
				try
				{
					BitArray localValueFlags = valueFlags;
					IRow newPreparedRow = PrepareNewRow(program, oldRow, newRow, ref localValueFlags);
					try
					{
						bool changed = PreparedValidate(program, oldPreparedRow, newPreparedRow, valueFlags, columnName, true, true);
						if (changed && !Object.ReferenceEquals(newPreparedRow, newRow))
							newPreparedRow.CopyTo(newRow);
						return changed;
					}
					finally
					{
						if (!Object.ReferenceEquals(newRow, newPreparedRow))
							newPreparedRow.Dispose();
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(oldRow, oldPreparedRow))
						oldPreparedRow.Dispose();
				}
			}

			return false;
		}
		
		protected bool PreparedValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			#if USEPROPOSALEVENTS
			DoValidateRow(ANewRow, AColumnName, AProgram);
			#endif
			bool changed = ExecuteValidateHandlers(program, oldRow, newRow, valueFlags, columnName);
			changed = ValidateColumns(program, TableVar, oldRow, newRow, valueFlags, columnName, isDescending, isProposable) || changed;
			changed = InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable) || changed;
			if ((columnName == String.Empty) && (TableVar.HasRowConstraints()) && !isProposable)
			{
				PushRow(program, newRow);
				try
				{
					// If this is an insert (AOldRow == null) then the constraints must be validated regardless of whether or not a value was specified in the insert
					ValidateImmediateConstraints(program, isDescending, oldRow == null ? null : valueFlags);
				}
				finally
				{
					PopRow(program);
				}
			}
			return changed;
		}
		
		protected bool InternalPreparedValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool isDescending, bool isProposable)
		{
			// Given AOldRow and ANewRow, propagate a validate if necessary based on the difference between the row values
			#if !USENATIVECONCURRENCYCOMPARE
			EnsureConcurrencyNodes(AProgram);
			#endif
			
			int differentColumnIndex = -1;
			bool rowsEqual = true;
			int columnIndex;
			for (int index = 0; index < oldRow.DataType.Columns.Count; index++)
			{
				columnIndex = newRow.DataType.Columns.IndexOfName(oldRow.DataType.Columns[index].Name);
				if (columnIndex >= 0)
				{
					if (oldRow.HasValue(index))
					{
						if (newRow.HasValue(columnIndex))
						{
							#if USENATIVECONCURRENCYCOMPARE
							if (!(DataValue.NativeValuesEqual(program.ValueManager, oldRow[index], newRow[columnIndex])))
								if (differentColumnIndex >= 0)
									rowsEqual = false;
								else
									differentColumnIndex = columnIndex;
							#else
							AProgram.Context.Push(AOldRow[index]);
							AProgram.Context.Push(ANewRow[columnIndex]);
							try
							{
								object result = FConcurrencyNodes[columnIndex].Execute(AProgram);
								if (!((result != null) && (bool)result))
									if (differentColumnIndex >= 0)
										rowsEqual = false;
									else
										differentColumnIndex = columnIndex;
							}
							finally
							{
								AProgram.Context.Pop();
								AProgram.Context.Pop();
							}
							#endif
						}
						else
							if (differentColumnIndex >= 0)
								rowsEqual = false;
							else
								differentColumnIndex = columnIndex;
					}
					else
					{
						if (newRow.HasValue(columnIndex))
							if (differentColumnIndex >= 0)
								rowsEqual = false;
							else
								differentColumnIndex = columnIndex;
					}
				}
			}
			
			if (!rowsEqual)
				return PreparedValidate(program, oldRow, newRow, valueFlags, String.Empty, isDescending, isProposable);
			else
				if (differentColumnIndex >= 0)
					return PreparedValidate(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[differentColumnIndex].Name, isDescending, isProposable);
					
			return false;
		}
		
		public bool ShouldChange(string columnName)
		{
			if (columnName == String.Empty)
				return _tableVar.ShouldChange;
			return _tableVar.ShouldChange || _tableVar.Columns[_tableVar.Columns.IndexOfName(columnName)].ShouldChange;
		}
		
		public virtual bool Change(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (ShouldChange(columnName))
			{
				IRow oldPreparedRow = PrepareOldRow(program, oldRow, false);
				try
				{
					BitArray localValueFlags = valueFlags;
					IRow newPreparedRow = PrepareNewRow(program, oldRow, newRow, ref localValueFlags);
					try
					{
						#if USEPROPOSALEVENTS
						bool changed = DoChangeRow(LRow, AColumnName, AProgram);
						#endif
						bool changed = ExecuteChangeHandlers(program, oldPreparedRow, newPreparedRow, valueFlags, columnName);
						changed = InternalChange(program, oldPreparedRow, newPreparedRow, valueFlags, columnName) || changed;
						changed = ChangeColumns(program, TableVar, oldPreparedRow, newPreparedRow, valueFlags, columnName) || changed;
						if (changed)
							InternalPreparedValidate(program, oldPreparedRow, newPreparedRow, valueFlags, false, true);
						if (changed && !Object.ReferenceEquals(newPreparedRow, newRow))
							newPreparedRow.CopyTo(newRow);
						return changed;
					}
					finally
					{
						if (!Object.ReferenceEquals(newRow, newPreparedRow))
						{
							if (valueFlags != null)
								for (int index = 0; index < localValueFlags.Count; index++)
									if (localValueFlags[index])
										valueFlags[newRow.DataType.Columns.IndexOfName(newPreparedRow.DataType.Columns[index].Name)] = localValueFlags[index];
			
							newPreparedRow.Dispose();
						}
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(oldRow, oldPreparedRow))
						oldPreparedRow.Dispose();
				}
			}
			
			return false;
		}
		
		public bool ShouldDefault(string columnName)
		{
			if (columnName == String.Empty)
				return _tableVar.ShouldDefault;
			return _tableVar.ShouldDefault || _tableVar.Columns[_tableVar.Columns.IndexOfName(columnName)].ShouldDefault;
		}
		
		public virtual bool Default(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (ShouldDefault(columnName))
			{
				IRow tempRow;
				BitArray localValueFlags = null;
				if (oldRow == null)
					tempRow = new Row(program.ValueManager, DataType.RowType);
				else
					tempRow = PrepareNewRow(program, null, oldRow, ref localValueFlags);
				try
				{
					IRow localNewRow = PrepareNewRow(program, null, newRow, ref valueFlags);
					try
					{
						bool changed = PreparedDefault(program, tempRow, localNewRow, valueFlags, columnName, true);
						if (changed && !Object.ReferenceEquals(localNewRow, newRow))
							localNewRow.CopyTo(newRow);
						return changed;
					}
					finally
					{
						if (!Object.ReferenceEquals(newRow, localNewRow))
							localNewRow.Dispose();
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(oldRow, tempRow))
						tempRow.Dispose();
				}
			}
			
			return false;
		}
		
		protected bool PreparedDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			IRow localOldRow;
			if ((oldRow == null) && isDescending)
				localOldRow = new Row(program.ValueManager, DataType.RowType);
			else
				localOldRow = oldRow;
			try
			{
				#if USEPROPOSALEVENTS
				bool changed = DoDefaultRow(ARow, AColumnName, AProgram);
				#endif
				bool changed = ExecuteDefaultHandlers(program, newRow, valueFlags, columnName);
				changed = InternalDefault(program, localOldRow, newRow, valueFlags, columnName, isDescending) || changed;
				return changed;
			}
			finally
			{
				if (!ReferenceEquals(oldRow, localOldRow) && (localOldRow != null))
					localOldRow.Dispose();
			}
		}
		
		// DefaultColumns
		public static bool DefaultColumns(Program program, Schema.TableVar tableVar, IRow row, BitArray valueFlags, string columnName)
		{
			if (columnName != String.Empty)
				return DefaultColumn(program, tableVar, row, valueFlags, columnName);
			else
			{
				bool changed = false;
				for (int index = 0; index < tableVar.Columns.Count; index++)
					changed = DefaultColumn(program, tableVar, row, valueFlags, tableVar.Columns[index].Name) || changed;
				return changed;
			}
		}
		
		// DefaultColumn
		public static bool DefaultColumn(Program program, Schema.TableVar tableVar, IRow row, BitArray valueFlags, string columnName)
		{
			int rowIndex = row.DataType.Columns.IndexOfName(columnName);
			if (rowIndex >= 0)
			{
				int index = tableVar.Columns.IndexOfName(columnName);
				if (!row.HasValue(rowIndex) && ((valueFlags == null) || !valueFlags[rowIndex]))
				{
					// Column level default trigger handlers
					program.Stack.Push(null);
					try
					{
						if (tableVar.Columns[index].HasHandlers())
							foreach (Schema.EventHandler handler in tableVar.Columns[index].EventHandlers)
								if ((handler.EventType & EventType.Default) != 0)
								{
									object result = handler.PlanNode.Execute(program);
									if ((result != null) && (bool)result)
									{
										row[rowIndex] = program.Stack.Peek(0);
										if (valueFlags != null)
											valueFlags[rowIndex] = true;
										return true;
									}
								}
					}
					finally
					{
						program.Stack.Pop();
					}
					
					// Column level default
					if (tableVar.Columns[index].Default != null)
					{
						row[rowIndex] = tableVar.Columns[index].Default.Node.Execute(program);
						if (valueFlags != null)
							valueFlags[rowIndex] = true;
						return true;
					} 

					// Scalar type level default trigger handlers
					Schema.ScalarType scalarType = tableVar.Columns[index].DataType as Schema.ScalarType;
					if (scalarType != null)
					{
						program.Stack.Push(null);
						try
						{
							if (scalarType.HasHandlers())
								foreach (Schema.EventHandler handler in scalarType.EventHandlers)
									if ((handler.EventType & EventType.Default) != 0)
									{
										object result = handler.PlanNode.Execute(program);
										if ((result != null) && (bool)result)
										{
											row[rowIndex] = program.Stack.Peek(0);
											if (valueFlags != null)
												valueFlags[rowIndex] = true;
											return true;
										}
									}
						}
						finally
						{
							program.Stack.Pop();
						}

						// Scalar type level default													   
						if (scalarType.Default != null)
						{
							row[rowIndex] = scalarType.Default.Node.Execute(program);
							if (valueFlags != null)
								valueFlags[rowIndex] = true;
							return true;
						}
					}
				}
			}
			return false;
		}
		
		public static bool ExecuteChangeHandlers(Program program, Schema.EventHandlers handlers)
		{
			bool changed = false;
			foreach (Schema.EventHandler eventHandler in handlers)
				if ((eventHandler.EventType & EventType.Change) != 0)
				{
					object result = eventHandler.PlanNode.Execute(program);
					changed = ((result != null) && (bool)result) || changed;
				}
			return changed;
		}
		
		public static bool ExecuteScalarTypeChangeHandlers(Program program, Schema.ScalarType scalarType)
		{
			bool changed = false;
			if (scalarType.HasHandlers())
				changed = ExecuteChangeHandlers(program, scalarType.EventHandlers);
			#if USETYPEINHERITANCE
			foreach (Schema.ScalarType parentType in AScalarType.ParentTypes)
				changed = ExecuteScalarTypeChangeHandlers(AProgram, parentType) || changed;
			#endif
			return changed;
		}
		
		// ChangeColumns
		public static bool ChangeColumns(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (columnName != String.Empty)
			{
				int rowIndex = newRow.DataType.Columns.IndexOfName(columnName);
				program.Stack.Push(oldRow);
				try
				{
					program.Stack.Push(newRow);
					try
					{
						bool changed = false;
						if (valueFlags != null)
							newRow.BeginModifiedContext();
						try
						{
							if (tableVar.Columns[tableVar.Columns.IndexOfName(columnName)].HasHandlers())
								if (ExecuteChangeHandlers(program, tableVar.Columns[tableVar.Columns.IndexOfName(columnName)].EventHandlers))
									changed = true;
								
							if (changed && (!Object.ReferenceEquals(newRow, program.Stack.Peek(0))))
							{
								IRow row = (IRow)program.Stack.Peek(0);
								row.CopyTo(newRow);
								row.ValuesOwned = false;
								row.Dispose();
								program.Stack.Poke(0, newRow);
							}
						}
						finally
						{
							if (valueFlags != null)
							{
								BitArray modifiedFlags = newRow.EndModifiedContext();
								for (int index = 0; index < modifiedFlags.Length; index++)
									if (modifiedFlags[index])
										valueFlags[index] = true;
							}
						}

						if (tableVar.Columns[tableVar.Columns.IndexOfName(columnName)].DataType is Schema.ScalarType)
						{
							program.Stack.Push(oldRow[rowIndex]);
							try
							{
								program.Stack.Push(newRow[rowIndex]);
								try
								{
									bool columnChanged = ExecuteScalarTypeChangeHandlers(program, (Schema.ScalarType)tableVar.Columns[tableVar.Columns.IndexOfName(columnName)].DataType);
									if (columnChanged)
									{
										newRow[rowIndex] = program.Stack.Peek(0);
										if (valueFlags != null)
											valueFlags[rowIndex] = true;
										changed = true;
									}

									return changed;
								}
								finally
								{
									program.Stack.Pop();
								}
							}
							finally
							{
								program.Stack.Pop();
							}
						}
						return changed;
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			else
			{
				int rowIndex;
				bool changed = false;
				program.Stack.Push(newRow);
				try
				{
					foreach (Schema.TableVarColumn column in tableVar.Columns)
					{
						rowIndex = newRow.DataType.Columns.IndexOfName(column.Name);
						if (rowIndex >= 0)
						{
							bool columnChanged = false;
							if (valueFlags != null)
								newRow.BeginModifiedContext();
							try
							{
								if (column.HasHandlers())
									columnChanged = ExecuteChangeHandlers(program, column.EventHandlers);

								if (columnChanged)
								{
									changed = true;
									if (!Object.ReferenceEquals(newRow, program.Stack.Peek(0)))
									{
										IRow row = (IRow)program.Stack.Peek(0);
										row.CopyTo(newRow);
										row.ValuesOwned = false;
										row.Dispose();
										program.Stack.Poke(0, newRow);
									}
								}
							}
							finally
							{
								if (valueFlags != null)
								{
									BitArray modifiedFlags = newRow.EndModifiedContext();
									for (int index = 0; index < modifiedFlags.Length; index++)
										if (modifiedFlags[index])
											valueFlags[index] = true;
								}
							}

							if (column.DataType is Schema.ScalarType)
							{
								program.Stack.Push(oldRow[rowIndex]);
								try
								{
									program.Stack.Push(newRow[rowIndex]);
									try
									{
										columnChanged = ExecuteScalarTypeChangeHandlers(program, (Schema.ScalarType)column.DataType);
											
										if (columnChanged)
										{
											newRow[rowIndex] = program.Stack.Peek(0);
											if (valueFlags != null)
												valueFlags[rowIndex] = true;
											changed = true;
										}
									}
									finally
									{
										program.Stack.Pop();
									}
								}
								finally
								{
									program.Stack.Pop();
								}
							}
						}
					}
					return changed;
				}
				finally
				{
					program.Stack.Pop();
				}
			}
		}
		
		public static bool ExecuteValidateHandlers(Program program, Schema.EventHandlers handlers)
		{
			return ExecuteValidateHandlers(program, handlers, null);
		}
		
		public static bool ExecuteValidateHandlers(Program program, Schema.EventHandlers handlers, Schema.Operator fromOperator)
		{
			bool changed = false;
			foreach (Schema.EventHandler eventHandler in handlers)
				if (((eventHandler.EventType & EventType.Validate) != 0) && ((fromOperator == null) || (fromOperator.Name != eventHandler.Operator.Name)))
				{
					object result = eventHandler.PlanNode.Execute(program);
					changed = ((result != null) && (bool)result) || changed;
				}
			return changed;
		}
		
		public static bool ExecuteScalarTypeValidateHandlers(Program program, Schema.ScalarType scalarType)
		{
			return ExecuteScalarTypeValidateHandlers(program, scalarType, null);
		}
		
		public static bool ExecuteScalarTypeValidateHandlers(Program program, Schema.ScalarType scalarType, Schema.Operator fromOperator)
		{
			bool changed = false;
			if (scalarType.HasHandlers())
				changed = ExecuteValidateHandlers(program, scalarType.EventHandlers, fromOperator);
			#if USETYPEINHERITANCE
			foreach (Schema.ScalarType parentType in AScalarType.ParentTypes)
				changed = ExecuteScalarTypeValidateHandlers(AProgram, parentType) || changed;
			#endif
			return changed;
		}
		
		// ValidateColumns
		public static bool ValidateColumns(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (columnName != String.Empty)
			{
				// if the call is made for a specific column, the validation is coming in from a proposable call and should be allowed to be empty
				int rowIndex = newRow.DataType.Columns.IndexOfName(columnName);
				if (newRow.HasValue(rowIndex))
				{
					program.Stack.Push(oldRow);
					try
					{
						program.Stack.Push(newRow);
						try
						{
							bool changed = false;
							Schema.TableVarColumn column = tableVar.Columns[tableVar.Columns.IndexOfName(columnName)];
							if (column.HasHandlers())
								changed = ExecuteValidateHandlers(program, column.EventHandlers);
							int oldRowIndex = oldRow == null ? -1 : oldRow.DataType.Columns.IndexOfName(columnName);
							program.Stack.Push(oldRowIndex >= 0 ? oldRow[oldRowIndex] : null);
							try
							{
								program.Stack.Push(newRow[rowIndex]);
								try
								{
									bool columnChanged;
									if (column.DataType is Schema.ScalarType)
										columnChanged = ExecuteScalarTypeValidateHandlers(program, (Schema.ScalarType)column.DataType);
									else
										columnChanged = false;

									if (columnChanged)
									{
										newRow[rowIndex] = program.Stack.Peek(0);
										changed = true;
									}

									ValidateColumnConstraints(program, column, isDescending);
									if (column.DataType is Schema.ScalarType)
										ValidateScalarTypeConstraints(program, (Schema.ScalarType)column.DataType, isDescending);
									return changed;
								}
								finally
								{
									program.Stack.Pop();
								}
							}
							finally
							{
								program.Stack.Pop();
							}
						}
						finally
						{
							program.Stack.Pop();
						}
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				return false;
			}
			else
			{
				// If there is no column name, this call is the validation for an insert, and should only be allowed to have no value if the column is nilable
				int rowIndex;
				program.Stack.Push(oldRow);
				try
				{
					program.Stack.Push(newRow);
					try
					{
						bool changed = false;
						bool columnChanged;
						foreach (Schema.TableVarColumn column in tableVar.Columns)
						{
							rowIndex = newRow.DataType.Columns.IndexOfName(column.Name);
							if (rowIndex >= 0)
							{
 								if (newRow.HasValue(rowIndex) || ((valueFlags == null) || valueFlags[rowIndex]))
								{
									if (column.HasHandlers())
									{
										if (valueFlags != null)
											newRow.BeginModifiedContext();
										try
										{
											if (ExecuteValidateHandlers(program, column.EventHandlers))
												changed = true;
										}
										finally
										{
											if (valueFlags != null)
											{
												BitArray modifiedFlags = newRow.EndModifiedContext();
												for (int index = 0; index < modifiedFlags.Length; index++)
													if (modifiedFlags[index])
														valueFlags[index] = true;
											}
										}
									}

									int oldRowIndex = oldRow == null ? -1 : oldRow.DataType.Columns.IndexOfName(column.Name);
									program.Stack.Push(oldRowIndex >= 0 ? oldRow[oldRowIndex] : null);
									try
									{
										program.Stack.Push(newRow[rowIndex]);
										try
										{
											if (column.DataType is Schema.ScalarType)
												columnChanged = ExecuteScalarTypeValidateHandlers(program, (Schema.ScalarType)column.DataType);
											else
												columnChanged = false;

											if (columnChanged)
											{
												newRow[rowIndex] = program.Stack.Peek(0);
												if (valueFlags != null)
													valueFlags[rowIndex] = true;
												changed = true;
											}

											ValidateColumnConstraints(program, column, isDescending);
											if (column.DataType is Schema.ScalarType)
												ValidateScalarTypeConstraints(program, (Schema.ScalarType)column.DataType, isDescending);
										}
										finally
										{
											program.Stack.Pop();
										}
									}
									finally
									{
										program.Stack.Pop();
									}
								}

								if (!newRow.HasValue(rowIndex) && !isProposable && (tableVar is Schema.BaseTableVar) && !column.IsNilable)
									throw new RuntimeException(RuntimeException.Codes.ColumnValueRequired, ErrorSeverity.User, MetaData.GetTag(column.MetaData, "Frontend.Title", column.Name));
							}
						}
						return changed;
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				finally
				{
					program.Stack.Pop();
				}
			}	 
		}
		
		// ValidateScalarTypeConstraints
		public static void ValidateScalarTypeConstraints(Program program, Schema.ScalarType scalarType, bool isDescending)
		{
			Schema.ScalarTypeConstraint constraint;
			for (int index = 0; index < scalarType.Constraints.Count; index++)
			{
				constraint = scalarType.Constraints[index];
				if (isDescending || constraint.Enforced)
					constraint.Validate(program, Schema.Transition.Insert);
			}
			
			#if USETYPEINHERITANCE	
			foreach (Schema.ScalarType parentType in AScalarType.ParentTypes)
				ValidateScalarTypeConstraints(AProgram, parentType, AIsDescending);
			#endif
		}
		
		// ValidateColumnConstraints
		// This method expects that the value of the column to be validated is at location 0 on the stack
		public static void ValidateColumnConstraints(Program program, Schema.TableVarColumn column, bool isDescending)
		{
			foreach (Schema.TableVarColumnConstraint constraint in column.Constraints)
				if (isDescending || constraint.Enforced)
					constraint.Validate(program, Schema.Transition.Insert);
		}
		
		// ValidateImmediateConstraints
		// This method expects that the row to be validated is at location 0 on the stack
		protected virtual void ValidateImmediateConstraints(Program program, bool isDescending, BitArray valueFlags)
		{
			if (TableVar.HasRowConstraints())
			{
				Schema.RowConstraint constraint;
				for (int index = 0; index < TableVar.RowConstraints.Count; index++)
				{
					constraint = TableVar.RowConstraints[index];
					if ((constraint.ConstraintType != Schema.ConstraintType.Database) && (isDescending || constraint.Enforced) && constraint.ShouldValidate(valueFlags, Schema.Transition.Insert))
						constraint.Validate(program, Schema.Transition.Insert);
				}
			}
		} 
		
		// ValidateCatalogConstraints
		// This method does not have any expectations for the stack
		protected virtual void ValidateCatalogConstraints(Program program)
		{
			if (TableVar.HasCatalogConstraints())
				foreach (Schema.CatalogConstraint constraint in TableVar.CatalogConstraints)
				{
					if (constraint.Enforced)
					{
						if (constraint.IsDeferred && program.ServerProcess.InTransaction)
						{
							bool hasCheck = false;
							for (int index = 0; index < program.ServerProcess.Transactions.Count; index++)
								if (program.ServerProcess.Transactions[index].CatalogConstraints.Contains(constraint.Name))
								{
									hasCheck = true;
									break;
								}
							
							if (!hasCheck)
								program.ServerProcess.CurrentTransaction.CatalogConstraints.Add(constraint);
						}
						else
							constraint.Validate(program);
					}
				}
		}
		
		// ShouldValidateKeyConstraints
		// Allows descendent nodes to indicate whether or not key constraints should be checked.
		// Ths method is overridden by the TableVarNode to indicate that key constraints should not be checked if propagation is not occurring.
		protected virtual bool ShouldValidateKeyConstraints(Schema.Transition transition)
		{
			return true;
		}
		
		// ValidateInsertConstraints
		// This method expects that the stack contains the new row at location 0 on the stack
		protected virtual void ValidateInsertConstraints(Program program)
		{
			if (program.ServerProcess.InTransaction && TableVar.HasDeferredConstraints())
				program.ServerProcess.AddInsertTableVarCheck(TableVar, (IRow)program.Stack.Peek(0));

			#if !USENAMEDROWVARIABLES
			PushRow(AProgram, (IRow)AProgram.Stack.Peek(0));
			try
			#endif		
			{	
				if (TableVar.HasRowConstraints())
				{
					Schema.RowConstraint constraint;
					for (int index = 0; index < TableVar.RowConstraints.Count; index++)
					{
						constraint = TableVar.RowConstraints[index];
						if (constraint.Enforced && (!program.ServerProcess.InTransaction || !constraint.IsDeferred))
							constraint.Validate(program, Schema.Transition.Insert);
					}
				}
			}
			#if !USENAMEDROWVARIABLES
			finally
			{
				PopRow(AProgram);
			}
			#endif
	
			if (TableVar.HasInsertConstraints())
			{
				Schema.TransitionConstraint constraint;
				bool shouldValidateKeyConstraints = ShouldValidateKeyConstraints(Schema.Transition.Insert);
				for (int index = 0; index < TableVar.InsertConstraints.Count; index++)
				{
					constraint = TableVar.InsertConstraints[index];
					if (constraint.Enforced && (!program.ServerProcess.InTransaction || !constraint.IsDeferred) && ((constraint.ConstraintType != Schema.ConstraintType.Table) || shouldValidateKeyConstraints))
						constraint.Validate(program, Schema.Transition.Insert);
				}
			}
		}
		
		// ValidateUpdateConstraints
		// This method expects that the stack contain the old row in location 1, and the new row in location 0
		protected virtual void ValidateUpdateConstraints(Program program, BitArray valueFlags)
		{
			if (program.ServerProcess.InTransaction && TableVar.HasDeferredConstraints(valueFlags, Schema.Transition.Update))
				program.ServerProcess.AddUpdateTableVarCheck(TableVar, (IRow)program.Stack.Peek(1), (IRow)program.Stack.Peek(0), valueFlags);
			
			#if !USENAMEDROWVARIABLES
			PushRow(AProgram, (IRow)program.Stack.Peek(0));
			try
			#endif
			{
				if (TableVar.HasRowConstraints())
				{
					Schema.RowConstraint constraint;
					for (int index = 0; index < TableVar.RowConstraints.Count; index++)
					{
						constraint = TableVar.RowConstraints[index];
						if (constraint.Enforced && (!program.ServerProcess.InTransaction || !constraint.IsDeferred) && constraint.ShouldValidate(valueFlags, Schema.Transition.Insert))
							constraint.Validate(program, Schema.Transition.Insert);
					}
				}
			}
			#if !USENAMEDROWVARIABLES
			finally
			{
				PopRow(AProgram);
			}
			#endif
	
			if (TableVar.HasUpdateConstraints())
			{
				bool shouldValidateKeyConstraints = ShouldValidateKeyConstraints(Schema.Transition.Update);
				Schema.TransitionConstraint constraint;
				for (int index = 0; index < TableVar.UpdateConstraints.Count; index++)
				{
					constraint = TableVar.UpdateConstraints[index];
					if (constraint.Enforced && (!program.ServerProcess.InTransaction || !constraint.IsDeferred) && ((constraint.ConstraintType != Schema.ConstraintType.Table) || shouldValidateKeyConstraints) && constraint.ShouldValidate(valueFlags, Schema.Transition.Update))
						constraint.Validate(program, Schema.Transition.Update);
				}
			}
		}
		
		// ValidateDeleteConstraints
		// This method expects that the stack contain the old row in location 0
		protected virtual void ValidateDeleteConstraints(Program program)
		{
			if (program.ServerProcess.InTransaction && TableVar.HasDeferredConstraints())
				program.ServerProcess.AddDeleteTableVarCheck(TableVar, (IRow)program.Stack.Peek(0));

			if (TableVar.HasDeleteConstraints())
				foreach (Schema.Constraint constraint in TableVar.DeleteConstraints)
					if (constraint.Enforced && (!program.ServerProcess.InTransaction || !constraint.IsDeferred))
						constraint.Validate(program, Schema.Transition.Delete);
		}

		// ExecuteHandlers executes each handler associated with the given event type
		protected virtual void ExecuteHandlers(Program program, EventType eventType)
		{
			// If the process is in an application transaction, and this is an AT table
				// if we are populating source tables
					// do not fire any handlers, 
				// If we are in an AT replay context, do not invoke any handler that was invoked within the AT
				// otherwise record that the handler was invoked in this AT
			ApplicationTransaction transaction = null;
			if (program.ServerProcess.ApplicationTransactionID != Guid.Empty)
				transaction = program.ServerProcess.GetApplicationTransaction();
			try
			{
				if ((transaction == null) || !transaction.IsPopulatingSource)
				{
					foreach (Schema.TableVarEventHandler handler in TableVar.EventHandlers)
						if (handler.EventType == eventType)
						{
							bool invoked = false;
							if ((transaction == null) || !transaction.InATReplayContext || !transaction.WasInvoked(handler))
							{
								if (handler.IsDeferred && program.ServerProcess.InTransaction)
									switch (eventType)
									{
										case EventType.AfterInsert : program.ServerProcess.CurrentTransaction.AddInsertHandler(handler, (IRow)program.Stack.Peek(0)); invoked = true; break;
										case EventType.AfterUpdate : program.ServerProcess.CurrentTransaction.AddUpdateHandler(handler, (IRow)program.Stack.Peek(1), (IRow)program.Stack.Peek(0)); invoked = true; break;
										case EventType.AfterDelete : program.ServerProcess.CurrentTransaction.AddDeleteHandler(handler, (IRow)program.Stack.Peek(0)); invoked = true; break;
										default : break; // only after handlers should be deferred to transaction commit
									}

								if (!invoked)
								{
									program.ServerProcess.PushHandler();
									try
									{
										handler.PlanNode.Execute(program);
									}
									finally
									{
										program.ServerProcess.PopHandler();
									}
								}
								
								// BTR 2/24/2004 ->
								// The InHandler check here is preventing event handler invocations within an A/T from being recorded if the event handler
								// is invoked from within another event handler.  This behavior is incorrect, and I cannot understand why it matters whether
								// or not an event handler was invoked from within another event handler.  If the handler is invoked during an A/T, it should
								// be recorded as invoked, and not invoked during the replay, end of story.
								//if ((LTransaction != null) && !LTransaction.InATReplayContext && !AProgram.InHandler && !LTransaction.InvokedHandlers.Contains(LHandler))
								if ((transaction != null) && !transaction.InATReplayContext && !transaction.InvokedHandlers.Contains(handler))
									transaction.InvokedHandlers.Add(handler);
							}
						}
				}
			}
			finally
			{
				if (transaction != null)
					Monitor.Exit(transaction);
			}
		}
		
		// ExecuteValidateHandlers prepares the stack and executes each handler associated with the validate event
		protected virtual bool ExecuteValidateHandlers(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (TableVar.HasHandlers(EventType.Validate))
			{
				PushRow(program, oldRow);
				try
				{
					PushRow(program, newRow);
					try
					{
						program.Stack.Push(columnName);
						try
						{
							bool changed = false;
							foreach (Schema.EventHandler handler in TableVar.EventHandlers)
								if ((handler.EventType & EventType.Validate) != 0)
								{
									if (valueFlags != null)
										newRow.BeginModifiedContext();
									try
									{
										object objectValue = handler.PlanNode.Execute(program);
										if ((objectValue != null) && (bool)objectValue)
											changed = true;
									}
									finally
									{
										if (valueFlags != null)
										{
											BitArray modifiedFlags = newRow.EndModifiedContext();
											for (int index = 0; index < modifiedFlags.Length; index++)
												if (modifiedFlags[index])
													valueFlags[index] = true;
										}
									}
								}
							return changed;
						}
						finally
						{
							program.Stack.Pop();
						}
					}
					finally
					{
						PopRow(program);
					}
				}
				finally
				{
					PopRow(program);
				}
			}
			return false;
		}
		
		// ExecuteDefaultHandlers prepares the stack and executes each handler associated with the default event
		protected virtual bool ExecuteDefaultHandlers(Program program, IRow row, BitArray valueFlags, string columnName)
		{
			if (TableVar.HasHandlers(EventType.Default))
			{
				PushRow(program, row);
				try
				{
					program.Stack.Push(columnName);
					try
					{
						bool changed = false;
						foreach (Schema.EventHandler handler in TableVar.EventHandlers)
							if ((handler.EventType & EventType.Default) != 0)
							{
								if (valueFlags != null)
									row.BeginModifiedContext();
								try
								{
									object result = handler.PlanNode.Execute(program);
									if ((result != null) && (bool)result)
										changed = true;
								}
								finally
								{
									if (valueFlags != null)
									{
										BitArray modifiedFlags = row.EndModifiedContext();
										for (int index = 0; index < modifiedFlags.Length; index++)
											if (modifiedFlags[index])
												valueFlags[index] = true;
									}
								}
							}

						return changed;
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				finally
				{
					PopRow(program);
				}
			}
			return false;
		}
		
		protected virtual bool ExecuteChangeHandlers(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (TableVar.HasHandlers(EventType.Change))
			{
				PushRow(program, oldRow);
				try
				{
					PushRow(program, newRow);
					try
					{
						program.Stack.Push(columnName);
						try
						{
							bool changed = false;
							foreach (Schema.EventHandler handler in TableVar.EventHandlers)
								if ((handler.EventType & EventType.Change) != 0)
								{
									if (valueFlags != null)
										newRow.BeginModifiedContext();
									try
									{
										object result = handler.PlanNode.Execute(program);
										if ((result != null) && (bool)result)
											changed = true;
									}
									finally
									{
										if (valueFlags != null)
										{
											BitArray modifiedFlags = newRow.EndModifiedContext();
											for (int index = 0; index < modifiedFlags.Length; index++)
												if (modifiedFlags[index])
													valueFlags[index] = true;
										}
									}
								}

							return changed;
						}
						finally
						{
							program.Stack.Pop();
						}
					}
					finally
					{
						PopRow(program);
					}
				}
				finally
				{
					PopRow(program);
				}
			}
			return false;
		}
		
		protected InsertNode _insertNode;
		/// <summary>Used as a compiled insert statement to be executed against the device if ModifySupported is true.</summary>		
		public InsertNode InsertNode
		{
			get { return _insertNode; }
			set { _insertNode = value; }
		}

		protected UpdateNode _updateNode;
		/// <summary>Used as a compiled update statement to be executed against the device if ModifySupported is true.</summary>		
		public UpdateNode UpdateNode
		{
			get { return _updateNode; }
			set { _updateNode = value; }
		}

		protected DeleteNode _deleteNode;
		/// <summary>Used as a compiled delete statement to be executed against the device if ModifySupported is true.</summary>		
		public DeleteNode DeleteNode
		{
			get { return _deleteNode; }
			set { _deleteNode = value; }
		}
		
		protected PlanNode _selectNode;
		/// <summary>Used to perform an optimistic concurrency check for processor handled updates.</summary>
		public PlanNode SelectNode
		{
			get { return _selectNode; }
			set { _selectNode = value; }
		}
		
		protected PlanNode _fullSelectNode;
		/// <summary>Used to select the row with a restriction on the entire row (at least columns of a type that has an equality operator), not just the key columns.</summary>
		public PlanNode FullSelectNode
		{
			get { return _fullSelectNode; }
			set { _fullSelectNode = value; }
		}

		protected PlanNode _selectAllNode;
		/// <summary>Used to select the row with a restriction on the key columns, and source outside of any A/T.</summary>
		public PlanNode SelectAllNode
		{
			get { return _selectAllNode; }
			set { _selectAllNode = value; }
		}
		
        // Insert
        protected virtual void ExecuteDeviceInsert(Program program, IRow row)
        {
			CheckModifySupported();
			EnsureModifyNodes(program.Plan);
			
			// Create a table constructor node to serve as the source for the insert
			TableSelectorNode sourceNode = new TableSelectorNode(new Schema.TableType());
			RowSelectorNode rowNode = new RowSelectorNode(row.DataType);
			sourceNode.Nodes.Add(rowNode);
			for (int index = 0; index < row.DataType.Columns.Count; index++)
			{
				if (row.HasValue(index))
					rowNode.Nodes.Add(new ValueNode(row.DataType.Columns[index].DataType, row[index]));
				else
					rowNode.Nodes.Add(new ValueNode(row.DataType.Columns[index].DataType, null));
				sourceNode.DataType.Columns.Add(row.DataType.Columns[index].Copy());
			}

			// Insert the table constructor as the source node for the insert template
			_insertNode.Nodes.Add(sourceNode);
			_insertNode.Nodes.Add(this);
			try
			{	
				program.DeviceExecute(_device, _insertNode);
			}
			finally
			{
				// Remove the table constructor
				_insertNode.Nodes.Remove(this);
				_insertNode.Nodes.Remove(sourceNode);
			}
        }
        
		// Update
		protected virtual void ExecuteDeviceUpdate(Program program, IRow oldRow, IRow newRow)
		{
			CheckModifySupported();
			EnsureModifyNodes(program.Plan);
			
			// Add update column nodes for each row to be updated
			for (int columnIndex = 0; columnIndex < newRow.DataType.Columns.Count; columnIndex++)
			{
				#if USECOLUMNLOCATIONBINDING
				FUpdateNode.Nodes.Add
					(
						new UpdateColumnNode
						(
							ANewRow.DataType.Columns[columnIndex].DataType,
							DataType.Columns.IndexOf(ANewRow.DataType.Columns[columnIndex].Name),
							new ValueNode(ANewRow.DataType.Columns[columnIndex].DataType, ANewRow.HasValue(columnIndex) ? ANewRow[columnIndex] : null)
						)
					);
				#else
				_updateNode.Nodes.Add
					(
						new UpdateColumnNode
						(
							newRow.DataType.Columns[columnIndex].DataType,
							newRow.DataType.Columns[columnIndex].Name,
							new ValueNode(newRow.DataType.Columns[columnIndex].DataType, newRow.HasValue(columnIndex) ? newRow[columnIndex] : null)
						)
					);
				#endif
			}
			try
			{
				program.Stack.Push(oldRow);
				try
				{
					// Execute the Update Statement
					program.DeviceExecute(_device, _updateNode);
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			finally
			{
				// Remove all the update column nodes
				for (int columnIndex = 0; columnIndex < newRow.DataType.Columns.Count; columnIndex++)
					_updateNode.Nodes.RemoveAt(_updateNode.Nodes.Count - 1);
			}
		}
        
		// Delete
		protected virtual void ExecuteDeviceDelete(Program program, IRow row)
		{
			CheckModifySupported();
			EnsureModifyNodes(program.Plan);
			
			program.Stack.Push(row);
			try
			{
				program.DeviceExecute(_device, _deleteNode);
			}
			finally
			{
				program.Stack.Pop();
			}
		}
        
		protected virtual void InternalBeforeInsert(Program program, IRow row, BitArray valueFlags) {}
		protected virtual void InternalAfterInsert(Program program, IRow row, BitArray valueFlags) {}
		protected virtual void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToPerformInsert);
		}

		protected virtual void InternalBeforeUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags) {}
		protected virtual void InternalAfterUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags) {}
		protected virtual void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToPerformUpdate);
		}
		
		protected virtual void InternalBeforeDelete(Program program, IRow row) {}
		protected virtual void InternalAfterDelete(Program program, IRow row) {}
		protected virtual void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToPerformDelete);
		}
		
		protected virtual bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable) { return false; }
		protected virtual bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending) { return false; }
		protected virtual bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName) { return false; }
		
		// PopulateNode
		protected TableNode _populateNode;
		public TableNode PopulateNode { get { return _populateNode; } }

		// PrepareJoinApplicationTransaction
		public virtual void PrepareJoinApplicationTransaction(Plan plan)
		{
			// If this process is joined to an application transaction and we are not within a table-valued context
				// join the expression for this node to the application transaction
			if ((plan.ApplicationTransactionID != Guid.Empty) && !plan.InTableTypeContext())
			{
				ApplicationTransaction transaction = plan.GetApplicationTransaction();
				try
				{
					if (!transaction.IsGlobalContext)
					{
						ApplicationTransactionUtility.PrepareJoinExpression
						(
							plan, 
							this,
							out _populateNode
						);
					}
				}
				finally
				{
					Monitor.Exit(transaction);
				}
			}
		}
		
		public virtual void InferPopulateNode(Plan plan) { }

		protected override void InternalBeforeExecute(Program program)
		{
			if ((_populateNode != null) && !program.ServerProcess.IsInsert)
				ApplicationTransactionUtility.JoinExpression(program, _populateNode, this);
		}
		
		public virtual void JoinApplicationTransaction(Program program,IRow row) {}

		#region ShowPlan

		public override Statement EmitStatement(EmitMode mode)
		{
			Statement statement = base.EmitStatement(mode);
			CallExpression callExpression = statement as CallExpression;
			if ((callExpression != null) && (mode == EmitMode.ForRemote) && (Operator != null) && (!Operator.IsRemotable))
			{
				// If we are emitting remote and the operator is not remotable, we have to encode the key information as modifiers
				// These will be picked up on the client side catalog deserialization by the TableValueToTableVarNode.
				if (callExpression.Modifiers == null)
					callExpression.Modifiers = new LanguageModifiers();
				D4TextEmitter emitter = new D4TextEmitter();
				callExpression.Modifiers.AddOrUpdate("KeyInfo", emitter.Emit(Compiler.FindClusteringKey(null, _tableVar).EmitStatement(EmitMode.ForCopy)));
			}
			return statement;
		}

		public override string Category
		{
			get { return "Table"; }
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			base.WritePlanAttributes(writer);
			writer.WriteAttributeString("ShouldChange", TableVar.ShouldChange.ToString().ToLower());
			writer.WriteAttributeString("ShouldValidate", TableVar.ShouldValidate.ToString().ToLower());
			writer.WriteAttributeString("ShouldDefault", TableVar.ShouldDefault.ToString().ToLower());
			writer.WriteAttributeString("IsChangeRemotable", TableVar.IsChangeRemotable.ToString().ToLower());
			writer.WriteAttributeString("IsValidateRemotable", TableVar.IsValidateRemotable.ToString().ToLower());
			writer.WriteAttributeString("IsDefaultRemotable", TableVar.IsDefaultRemotable.ToString().ToLower());
			writer.WriteAttributeString("CursorCapabilities", CursorCapabilitiesToString(CursorCapabilities));
			writer.WriteAttributeString("CursorType", CursorType.ToString().ToLower());
			writer.WriteAttributeString("RequestedCursorType", RequestedCursorType.ToString().ToLower());
			writer.WriteAttributeString("CursorIsolation", CursorIsolation.ToString());
			if (Order != null)
				writer.WriteAttributeString("Order", Order.Name);
		}

		protected override void WritePlanNodes(System.Xml.XmlWriter writer)
		{
			WritePlanTags(writer, TableVar.MetaData);
			WritePlanKeys(writer);
			WritePlanOrders(writer);
			WritePlanConstraints(writer);
			WritePlanReferences(writer);
			WritePlanColumns(writer);
			base.WritePlanNodes(writer);
		}

		protected virtual void WritePlanKeys(System.Xml.XmlWriter writer)
		{
			foreach (Schema.Key key in TableVar.Keys)
			{
				writer.WriteStartElement("Keys.Key");
				writer.WriteAttributeString("Name", key.Name);
				writer.WriteAttributeString("IsSparse", Convert.ToString(key.IsSparse));
				WritePlanTags(writer, key.MetaData);
				writer.WriteEndElement();
			}
		}

		protected virtual void WritePlanOrders(System.Xml.XmlWriter writer)
		{
			foreach (Schema.Order order in TableVar.Orders)
			{
				writer.WriteStartElement("Orders.Order");
				writer.WriteAttributeString("Name", order.Name);
				WritePlanTags(writer, order.MetaData);
				writer.WriteEndElement();
			}
		}

		protected virtual void WritePlanConstraints(System.Xml.XmlWriter writer)
		{
			if (TableVar.HasConstraints())
			{
			foreach (Schema.TableVarConstraint constraint in TableVar.Constraints)
			{
				if (!constraint.IsGenerated)
				{
					if (constraint is Schema.RowConstraint)
					{
						writer.WriteStartElement("Constraints.RowConstraint");
						writer.WriteAttributeString("Expression", ((Schema.RowConstraint)constraint).Node.SafeEmitStatementAsString());
					}
					else
					{
						Schema.TransitionConstraint transitionConstraint = (Schema.TransitionConstraint)constraint;
						writer.WriteStartElement("Constraints.TransitionConstraint");
						if (transitionConstraint.OnInsertNode != null)
							writer.WriteAttributeString("OnInsert", transitionConstraint.OnInsertNode.SafeEmitStatementAsString());
						if (transitionConstraint.OnUpdateNode != null)
							writer.WriteAttributeString("OnUpdate", transitionConstraint.OnUpdateNode.SafeEmitStatementAsString());
						if (transitionConstraint.OnDeleteNode != null)
							writer.WriteAttributeString("OnDelete", transitionConstraint.OnDeleteNode.SafeEmitStatementAsString());
					}
					writer.WriteAttributeString("Name", constraint.Name);
					WritePlanTags(writer, constraint.MetaData);
					writer.WriteEndElement();
				}
			}
		}
		}

		protected static string EmitColumnList(Schema.TableVarColumnsBase columns)
		{
			StringBuilder result = new StringBuilder();
			result.Append("{ ");
			for (int index = 0; index < columns.Count; index++)
			{
				if (index > 0)
					result.Append(", ");
				result.AppendFormat("{0}", columns[index].Name);
			}
			if (columns.Count > 0)
				result.Append(" ");
			result.Append("}");
			return result.ToString();
		}
		
		protected virtual void WritePlanReference(System.Xml.XmlWriter writer, Schema.ReferenceBase reference, bool isSource)
		{
			if (isSource)
			{
				writer.WriteStartElement("SourceReferences.Reference");
				writer.WriteAttributeString("Target", reference.TargetTable.Name);
			}
			else
			{
				writer.WriteStartElement("TargetReferences.Reference");
				writer.WriteAttributeString("Source", reference.SourceTable.Name);
			}
			writer.WriteAttributeString("IsDerived", Convert.ToString(reference.IsDerived));
			writer.WriteAttributeString("Name", reference.Name);
			writer.WriteAttributeString("SourceColumns", EmitColumnList(reference.SourceKey.Columns));
			writer.WriteAttributeString("TargetColumns", EmitColumnList(reference.TargetKey.Columns));
			writer.WriteAttributeString("IsExcluded", Convert.ToString(reference.IsExcluded));
			writer.WriteAttributeString("OriginatingReferenceName", reference.OriginatingReferenceName());
			WritePlanTags(writer, reference.MetaData);
			writer.WriteEndElement();
		}
		
		protected virtual void WritePlanReferences(System.Xml.XmlWriter writer)
		{
			if (TableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in TableVar.References)
					WritePlanReference(writer, reference, reference.SourceTable.Equals(TableVar));
		}

		protected virtual void WritePlanColumns(System.Xml.XmlWriter writer)
		{
			foreach (Schema.TableVarColumn column in TableVar.Columns)
			{
				writer.WriteStartElement("Columns.Column");
				writer.WriteAttributeString("Name", column.Name);
				writer.WriteAttributeString("Type", column.DataType.Name);
				if (column.IsNilable)
					writer.WriteAttributeString("Nilable", "true");
				if (column.ReadOnly)
					writer.WriteAttributeString("ReadOnly", "true");
				if (column.IsComputed)
					writer.WriteAttributeString("Computed", "true");
				if (!column.ShouldChange)
					writer.WriteAttributeString("ShouldChange", "false");
				if (!column.ShouldValidate)
					writer.WriteAttributeString("ShouldValidate", "false");
				if (!column.ShouldDefault)
					writer.WriteAttributeString("ShouldDefault", "false");
				if (column.Default != null)
				{
					writer.WriteStartElement("Default.Default");
					writer.WriteAttributeString("Expression", column.Default.Node.SafeEmitStatementAsString());
					WritePlanTags(writer, column.MetaData);
					writer.WriteEndElement();
				}
				WritePlanTags(writer, column.MetaData);
				writer.WriteEndElement();
			}
		}

		#endregion
	}
}
