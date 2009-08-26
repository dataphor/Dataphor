/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator Rename(table) : table
	public class RenameNode : UnaryTableNode
	{
		private string FTableAlias;
		public string TableAlias
		{
			get { return FTableAlias; }
			set { FTableAlias = value; }
		}
		
		private MetaData FMetaData;
		public MetaData MetaData
		{
			get { return FMetaData; }
			set { FMetaData = value; }
		}

		private RenameColumnExpressions FExpressions;		
		public RenameColumnExpressions Expressions
		{
			get { return FExpressions; }
			set { FExpressions = value; }
		}
		
		private void DetermineOrder(Plan APlan)
		{
			Order = null;
			if (SourceNode.Order != null)
			{
				Schema.Order LNewOrder = new Schema.Order();
				Schema.OrderColumn LOrderColumn;
				Schema.OrderColumn LNewOrderColumn;
				LNewOrder.InheritMetaData(SourceNode.Order.MetaData);
				LNewOrder.IsInherited = true;
				for (int LIndex = 0; LIndex < SourceNode.Order.Columns.Count; LIndex++)
				{
					LOrderColumn = SourceNode.Order.Columns[LIndex];
					LNewOrderColumn =
						new Schema.OrderColumn
						(
							TableVar.Columns[SourceTableVar.Columns.IndexOfName(LOrderColumn.Column.Name)],
							LOrderColumn.Ascending,
							LOrderColumn.IncludeNils
						);
					LNewOrderColumn.Sort = LOrderColumn.Sort;
					LNewOrderColumn.IsDefaultSort = LOrderColumn.IsDefaultSort;
					Error.AssertWarn(LNewOrderColumn.Sort != null, "Sort is null");
					if (LNewOrderColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LNewOrderColumn.Sort.Dependencies);
					LNewOrder.Columns.Add(LNewOrderColumn);
				}
				Order = LNewOrder;
			}
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			
			if (FExpressions == null)
			{
				// This is a rename all expression, merge metadata and inherit columns
				FTableVar.MergeMetaData(FMetaData);

				// Inherit columns
				Schema.TableVarColumn LNewColumn;
				foreach (Schema.TableVarColumn LColumn in SourceTableVar.Columns)
				{
					LNewColumn = LColumn.Inherit(FTableAlias);
					DataType.Columns.Add(LNewColumn.Column);
					TableVar.Columns.Add(LNewColumn);
				}
			}
			else
			{
				bool LColumnAdded;
				Schema.TableVarColumn LColumn;
				int LRenameColumnIndex;
				for (int LIndex = 0; LIndex < SourceTableVar.Columns.Count; LIndex++)
				{
					LColumnAdded = false;
					foreach (RenameColumnExpression LRenameColumn in FExpressions)
					{
						LRenameColumnIndex = SourceTableVar.Columns.IndexOf(LRenameColumn.ColumnName);
						if (LRenameColumnIndex < 0)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, LRenameColumn.ColumnName);
						else if (LRenameColumnIndex == LIndex)
						{
							LColumn = SourceTableVar.Columns[LIndex].InheritAndRename(LRenameColumn.ColumnAlias);
							LColumn.MergeMetaData(LRenameColumn.MetaData);
							DataType.Columns.Add(LColumn.Column);
							TableVar.Columns.Add(LColumn);
							LColumnAdded = true;
							break;
						}
					}
					if (!LColumnAdded)
					{
						LColumn = SourceTableVar.Columns[LIndex].Inherit();
						DataType.Columns.Add(LColumn.Column);
						TableVar.Columns.Add(LColumn);
					}
				}
			}

			DetermineRemotable(APlan);

			// Inherit keys
			Schema.Key LNewKey;
			foreach (Schema.Key LKey in SourceTableVar.Keys)
			{
				LNewKey = new Schema.Key();
				LNewKey.InheritMetaData(LKey.MetaData);
				LNewKey.IsInherited = true;
				LNewKey.IsSparse = LKey.IsSparse;
				foreach (Schema.TableVarColumn LKeyColumn in LKey.Columns)
					LNewKey.Columns.Add(TableVar.Columns[SourceTableVar.Columns.IndexOfName(LKeyColumn.Name)]);
				TableVar.Keys.Add(LNewKey);
			}
			
			// Inherit orders
			Schema.Order LNewOrder;
			Schema.OrderColumn LOrderColumn;
			Schema.OrderColumn LNewOrderColumn;
			foreach (Schema.Order LOrder in SourceTableVar.Orders)
			{
				LNewOrder = new Schema.Order();
				LNewOrder.InheritMetaData(LOrder.MetaData);
				LNewOrder.IsInherited = true;
				for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				{
					LOrderColumn = LOrder.Columns[LIndex];
					LNewOrderColumn =
						new Schema.OrderColumn
						(
							TableVar.Columns[SourceTableVar.Columns.IndexOfName(LOrderColumn.Column.Name)],
							LOrderColumn.Ascending,
							LOrderColumn.IncludeNils
						);
					LNewOrderColumn.Sort = LOrderColumn.Sort;
					LNewOrderColumn.IsDefaultSort = LOrderColumn.IsDefaultSort;
					Error.AssertWarn(LNewOrderColumn.Sort != null, "Sort is null");
					if (LNewOrderColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LNewOrderColumn.Sort.Dependencies);
					LNewOrder.Columns.Add(LNewOrderColumn);
				}
				TableVar.Orders.Add(LNewOrder);
			}
			
			DetermineOrder(APlan);
			
			#if UseReferenceDerivation
			// Copy source references
			foreach (Schema.Reference LReference in SourceTableVar.SourceReferences)
			{
				Schema.JoinKey LSourceKey = new Schema.JoinKey();
				foreach (Schema.TableVarColumn LColumn in LReference.SourceKey.Columns)
					LSourceKey.Columns.Add(TableVar.Columns[SourceTableVar.Columns.IndexOfName(LColumn.Name)]);
					
				int LNewReferenceID = Schema.Object.GetNextObjectID();
				string LNewReferenceName = DeriveSourceReferenceName(LReference, LNewReferenceID, LSourceKey);

				Schema.Reference LNewReference = new Schema.Reference(LNewReferenceID, LNewReferenceName);
				LNewReference.ParentReference = LReference;
				LNewReference.IsExcluded = LReference.IsExcluded;
				LNewReference.InheritMetaData(LReference.MetaData);
				LNewReference.SourceTable = FTableVar;
				LNewReference.AddDependency(FTableVar);
				LNewReference.TargetTable = LReference.TargetTable;
				LNewReference.AddDependency(LReference.TargetTable);
				LNewReference.SourceKey.IsUnique = LReference.SourceKey.IsUnique;
				foreach (Schema.TableVarColumn LColumn in LSourceKey.Columns)
					LNewReference.SourceKey.Columns.Add(LColumn);
				LNewReference.TargetKey.IsUnique = LReference.TargetKey.IsUnique;
				foreach (Schema.TableVarColumn LColumn in LReference.TargetKey.Columns)
					LNewReference.TargetKey.Columns.Add(LColumn);
				LNewReference.UpdateReferenceAction = LReference.UpdateReferenceAction;
				LNewReference.DeleteReferenceAction = LReference.DeleteReferenceAction;
				FTableVar.SourceReferences.Add(LNewReference);
				FTableVar.DerivedReferences.Add(LNewReference);
			}
			
			// Copy target references
			foreach (Schema.Reference LReference in SourceNode.TableVar.TargetReferences)
			{
				Schema.JoinKey LTargetKey = new Schema.JoinKey();
				foreach (Schema.TableVarColumn LColumn in LReference.TargetKey.Columns)
					LTargetKey.Columns.Add(TableVar.Columns[SourceTableVar.Columns.IndexOfName(LColumn.Name)]);
				
				int LNewReferenceID = Schema.Object.GetNextObjectID();
				string LNewReferenceName = DeriveTargetReferenceName(LReference, LNewReferenceID, LTargetKey);
				
				Schema.Reference LNewReference = new Schema.Reference(LNewReferenceID, LNewReferenceName);
				LNewReference.ParentReference = LReference;
				LNewReference.IsExcluded = LReference.IsExcluded;
				LNewReference.InheritMetaData(LReference.MetaData);
				LNewReference.SourceTable = LReference.SourceTable;
				LNewReference.AddDependency(LReference.SourceTable);
				LNewReference.TargetTable = FTableVar;
				LNewReference.AddDependency(FTableVar);
				LNewReference.SourceKey.IsUnique = LReference.SourceKey.IsUnique;
				foreach (Schema.TableVarColumn LColumn in LReference.SourceKey.Columns)
					LNewReference.SourceKey.Columns.Add(LColumn);
				LNewReference.TargetKey.IsUnique = LReference.TargetKey.IsUnique;
				foreach (Schema.TableVarColumn LColumn in LTargetKey.Columns)
					LNewReference.TargetKey.Columns.Add(LColumn);
				LNewReference.UpdateReferenceAction = LReference.UpdateReferenceAction;
				LNewReference.DeleteReferenceAction = LReference.DeleteReferenceAction;
				FTableVar.TargetReferences.Add(LNewReference);
				FTableVar.DerivedReferences.Add(LNewReference);
			}
			#endif
		}
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = SourceNode.CursorType;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					SourceNode.CursorCapabilities & 
					(
						CursorCapability.BackwardsNavigable | 
						CursorCapability.Searchable
					)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;

			DetermineOrder(APlan);
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (DataType.Columns.Count > 0)
			{
				RenameExpression LExpression = new RenameExpression();
				LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
				for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
					LExpression.Expressions.Add
					(
						new RenameColumnExpression
						(
							Schema.Object.EnsureRooted(SourceTableType.Columns[LIndex].Name), 
							DataType.Columns[LIndex].Name, 
							TableVar.Columns[LIndex].MetaData == null ? 
								null : 
								TableVar.Columns[LIndex].MetaData.Copy()
						)
					);
				LExpression.Modifiers = Modifiers;
				return LExpression;
			}
			else
				return Nodes[0].EmitStatement(AMode);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			RenameTable LTable = new RenameTable(this, AProgram);
			try
			{
				LTable.Open();
				return LTable;
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}
		
		public override void DetermineRemotable(Plan APlan)
		{
			Schema.ResultTableVar LTableVar = (Schema.ResultTableVar)TableVar;
			LTableVar.InferredIsDefaultRemotable = !PropagateDefault || SourceTableVar.IsDefaultRemotable;
			LTableVar.InferredIsChangeRemotable = !PropagateChange || SourceTableVar.IsChangeRemotable;
			LTableVar.InferredIsValidateRemotable = !PropagateValidate || SourceTableVar.IsValidateRemotable;
			LTableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			
			LTableVar.ShouldChange = PropagateChange && (LTableVar.ShouldChange || SourceTableVar.ShouldChange);
			LTableVar.ShouldDefault = PropagateDefault && (LTableVar.ShouldDefault || SourceTableVar.ShouldDefault);
			LTableVar.ShouldValidate = PropagateValidate && (LTableVar.ShouldValidate || SourceTableVar.ShouldValidate);
			
			for (int LIndex = 0; LIndex < LTableVar.Columns.Count; LIndex++)
			{
				Schema.TableVarColumn LColumn = LTableVar.Columns[LIndex];
				Schema.TableVarColumn LSourceColumn = SourceTableVar.Columns[LIndex];

				LColumn.ShouldChange = PropagateChange && (LColumn.ShouldChange || LSourceColumn.ShouldChange);
				LTableVar.ShouldChange = LTableVar.ShouldChange || LColumn.ShouldChange;

				LColumn.ShouldDefault = PropagateDefault && (LColumn.ShouldDefault || LSourceColumn.ShouldDefault);
				LTableVar.ShouldDefault = LTableVar.ShouldDefault || LColumn.ShouldDefault;

				LColumn.ShouldValidate = PropagateValidate && (LColumn.ShouldValidate || LSourceColumn.ShouldValidate);
				LTableVar.ShouldValidate = LTableVar.ShouldValidate || LColumn.ShouldValidate;
			}
		}
		
		// Validate
		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (AIsDescending && PropagateValidate)
			{
				Row LOldRow;
				if (!(AOldRow.DataType.Columns.Equivalent(DataType.Columns)))
				{
					LOldRow = new Row(AProgram.ValueManager, DataType.RowType);
					AOldRow.CopyTo(LOldRow);
				}
				else
					LOldRow = AOldRow;
				try
				{
					Row LNewRow;
					if (!(ANewRow.DataType.Columns.Equivalent(DataType.Columns)))
					{
						LNewRow = new Row(AProgram.ValueManager, DataType.RowType);
						ANewRow.CopyTo(LNewRow);
					}
					else
						LNewRow = ANewRow;
					try
					{
						Row LOldSourceRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType, (NativeRow)LOldRow.AsNative);
						try
						{
							LOldSourceRow.ValuesOwned = false;
							
							Row LNewSourceRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType, (NativeRow)LNewRow.AsNative);
							try
							{
								LNewSourceRow.ValuesOwned = false;
								
								bool LChanged = SourceNode.Validate(AProgram, LOldSourceRow, LNewSourceRow, AValueFlags, AColumnName == String.Empty ? String.Empty : LNewSourceRow.DataType.Columns[LNewRow.DataType.Columns.IndexOfName(AColumnName)].Name);
								
								if (LChanged && !ReferenceEquals(ANewRow, LNewRow))
									LNewRow.CopyTo(ANewRow);
								return LChanged;
							}
							finally
							{
								LNewSourceRow.Dispose();
							}
						}
						finally
						{
							LOldSourceRow.Dispose();
						}
					}
					finally
					{
						if (!ReferenceEquals(ANewRow, LNewRow))
							LNewRow.Dispose();
					}
				}
				finally
				{
					if (!ReferenceEquals(AOldRow, LOldRow))
						LOldRow.Dispose();
				}
			}
			return false;
		}
		
		// Default
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if (AIsDescending && PropagateDefault)
			{
				TableNode LSourceNode = SourceNode;
				
				Row LOldRow;
				if (!(AOldRow.DataType.Columns.Equivalent(DataType.Columns)))
				{
					LOldRow = new Row(AProgram.ValueManager, DataType.RowType);
					AOldRow.CopyTo(LOldRow);
				}
				else
					LOldRow = AOldRow;
				try
				{
					Row LNewRow;
					if (!(ANewRow.DataType.Columns.Equivalent(DataType.Columns)))
					{
						LNewRow = new Row(AProgram.ValueManager, DataType.RowType);
						ANewRow.CopyTo(LNewRow);
					}
					else
						LNewRow = ANewRow;
					try
					{
						Row LOldSourceRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)LOldRow.AsNative);
						try
						{
							LOldSourceRow.ValuesOwned = false;
							
							Row LNewSourceRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)LNewRow.AsNative);
							try
							{
								LNewSourceRow.ValuesOwned = false;

								bool LChanged = LSourceNode.Default(AProgram, LOldSourceRow, LNewSourceRow, AValueFlags, AColumnName == String.Empty ? String.Empty : LNewSourceRow.DataType.Columns[LNewRow.DataType.Columns.IndexOfName(AColumnName)].Name);
								
								if (LChanged && (ANewRow != LNewRow))
									LNewRow.CopyTo(ANewRow);
								return LChanged;
							}
							finally
							{
								LNewSourceRow.Dispose();
							}
						}
						finally
						{
							LOldSourceRow.Dispose();
						}
					}
					finally
					{
						if (!ReferenceEquals(ANewRow, LNewRow))
							LNewRow.Dispose();
					}
				}
				finally
				{
					if (!ReferenceEquals(AOldRow, LOldRow))
						LOldRow.Dispose();
				}
			}
			return false;
		}
		
		public override void JoinApplicationTransaction(Program AProgram, Row ARow)
		{
			Schema.RowType LRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				LRowType.Columns.Add(SourceNode.DataType.Columns[DataType.Columns.IndexOfName(LColumn.Name)].Copy());
				
			Row LRow = new Row(AProgram.ValueManager, LRowType, (NativeRow)ARow.AsNative);
			try
			{
				SourceNode.JoinApplicationTransaction(AProgram, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		// Change
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (PropagateChange)
			{
				TableNode LSourceNode = SourceNode;

				Row LOldRow;
				if (!(AOldRow.DataType.Columns.Equivalent(DataType.Columns)))
				{
					LOldRow = new Row(AProgram.ValueManager, DataType.RowType);
					AOldRow.CopyTo(LOldRow);
				}
				else
					LOldRow = AOldRow;
				try
				{
					Row LNewRow;
					if (!(ANewRow.DataType.Columns.Equivalent(DataType.Columns)))
					{
						LNewRow = new Row(AProgram.ValueManager, DataType.RowType);
						ANewRow.CopyTo(LNewRow);
					}
					else
						LNewRow = ANewRow;
					try
					{
						Row LOldSourceRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)LOldRow.AsNative);
						try
						{
							LOldSourceRow.ValuesOwned = false;

							Row LNewSourceRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)LNewRow.AsNative);
							try
							{
								LNewSourceRow.ValuesOwned = false;

								bool LChanged = LSourceNode.Change(AProgram, LOldSourceRow, LNewSourceRow, AValueFlags, AColumnName == String.Empty ? String.Empty : LNewSourceRow.DataType.Columns[LNewRow.DataType.Columns.IndexOfName(AColumnName)].Name);
								
								if (LChanged && (ANewRow != LNewRow))
									LNewRow.CopyTo(ANewRow);
								
								return LChanged;
							}
							finally
							{
								LNewSourceRow.Dispose();
							}
						}
						finally
						{
							LOldSourceRow.Dispose();
						}
					}
					finally
					{
						if (!ReferenceEquals(ANewRow, LNewRow))
							LNewRow.Dispose();
					}
				}
				finally
				{
					if (!ReferenceEquals(AOldRow, LOldRow))
						LOldRow.Dispose();
				}
			}
			return false;
		}
		
		// Insert
		protected override void InternalExecuteInsert(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsert)
			{
				case PropagateAction.True :
					using (Row LInsertRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType, (NativeRow)ANewRow.AsNative))
					{
						LInsertRow.ValuesOwned = false;
						SourceNode.Insert(AProgram, AOldRow, LInsertRow, AValueFlags, AUnchecked);
					}
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LInsertRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType, (NativeRow)ANewRow.AsNative))
					{
						LInsertRow.ValuesOwned = false;
						using (Row LSourceRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType))
						{
							LInsertRow.CopyTo(LSourceRow);
							using (Row LCurrentRow = SourceNode.Select(AProgram, LSourceRow))
							{
								if (LCurrentRow != null)
								{
									if (PropagateInsert == PropagateAction.Ensure)
										SourceNode.Update(AProgram, LCurrentRow, LInsertRow, AValueFlags, false, AUnchecked);
								}
								else
									SourceNode.Insert(AProgram, AOldRow, LInsertRow, AValueFlags, AUnchecked);
							}
						}
					}
				break;
			}
		}
		
		// Update
		protected override void InternalExecuteUpdate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateUpdate)
			{
				TableNode LSourceNode = SourceNode;
				Row LOldRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)AOldRow.AsNative);
				try
				{
					LOldRow.ValuesOwned = false;
					Row LNewRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)ANewRow.AsNative);
					try
					{
						LNewRow.ValuesOwned = false;
						LSourceNode.Update(AProgram, LOldRow, LNewRow, AValueFlags, ACheckConcurrency, AUnchecked);
					}
					finally
					{
						LNewRow.Dispose();
					}
				}
				finally
				{
					LOldRow.Dispose();
				}
			}
		}
		
		// Delete
		protected override void InternalExecuteDelete(Program AProgram, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateDelete)
			{
				TableNode LSourceNode = SourceNode;
				Row LRow = new Row(AProgram.ValueManager, LSourceNode.DataType.RowType, (NativeRow)ARow.AsNative);
				try
				{
					LRow.ValuesOwned = false;
					LSourceNode.Delete(AProgram, LRow, ACheckConcurrency, AUnchecked);
				}
				finally
				{
					LRow.Dispose();
				}
			}
		}
	}
}