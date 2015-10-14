/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define UseElaborable
	
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
		private string _tableAlias;
		public string TableAlias
		{
			get { return _tableAlias; }
			set { _tableAlias = value; }
		}
		
		private MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}

		private RenameColumnExpressions _expressions;		
		public RenameColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}
		
		private void DetermineOrder(Plan plan)
		{
			Order = null;
			if (SourceNode.Order != null)
			{
				Schema.Order newOrder = new Schema.Order();
				Schema.OrderColumn orderColumn;
				Schema.OrderColumn newOrderColumn;
				newOrder.InheritMetaData(SourceNode.Order.MetaData);
				newOrder.IsInherited = true;
				for (int index = 0; index < SourceNode.Order.Columns.Count; index++)
				{
					orderColumn = SourceNode.Order.Columns[index];
					newOrderColumn =
						new Schema.OrderColumn
						(
							TableVar.Columns[SourceTableVar.Columns.IndexOfName(orderColumn.Column.Name)],
							orderColumn.Ascending,
							orderColumn.IncludeNils
						);
					newOrderColumn.Sort = orderColumn.Sort;
					newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
					Error.AssertWarn(newOrderColumn.Sort != null, "Sort is null");
					if (newOrderColumn.IsDefaultSort)
						plan.AttachDependency(newOrderColumn.Sort);
					else
					{
						if (newOrderColumn.Sort.HasDependencies())
							plan.AttachDependencies(newOrderColumn.Sort.Dependencies);
					}
					newOrder.Columns.Add(newOrderColumn);
				}
				Order = newOrder;
			}
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			
			if (_expressions == null)
			{
				// This is a rename all expression, merge metadata and inherit columns
				_tableVar.MergeMetaData(_metaData);

				// Inherit columns
				Schema.TableVarColumn newColumn;
				foreach (Schema.TableVarColumn column in SourceTableVar.Columns)
				{
					newColumn = column.Inherit(_tableAlias);
					DataType.Columns.Add(newColumn.Column);
					TableVar.Columns.Add(newColumn);
				}
			}
			else
			{
				bool columnAdded;
				Schema.TableVarColumn column;
				int renameColumnIndex;
				for (int index = 0; index < SourceTableVar.Columns.Count; index++)
				{
					columnAdded = false;
					foreach (RenameColumnExpression renameColumn in _expressions)
					{
						renameColumnIndex = SourceTableVar.Columns.IndexOf(renameColumn.ColumnName);
						if (renameColumnIndex < 0)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, renameColumn.ColumnName);
						else if (renameColumnIndex == index)
						{
							if (columnAdded)
								throw new CompilerException(CompilerException.Codes.DuplicateRenameColumn, renameColumn.ColumnName);

							column = SourceTableVar.Columns[index].InheritAndRename(renameColumn.ColumnAlias);
							column.MergeMetaData(renameColumn.MetaData);
							DataType.Columns.Add(column.Column);
							TableVar.Columns.Add(column);
							columnAdded = true;
						}
					}
					if (!columnAdded)
					{
						column = SourceTableVar.Columns[index].Inherit();
						DataType.Columns.Add(column.Column);
						TableVar.Columns.Add(column);
					}
				}
			}

			DetermineRemotable(plan);

			// Inherit keys
			Schema.Key newKey;
			foreach (Schema.Key key in SourceTableVar.Keys)
			{
				newKey = new Schema.Key();
				newKey.InheritMetaData(key.MetaData);
				newKey.IsInherited = true;
				newKey.IsSparse = key.IsSparse;
				foreach (Schema.TableVarColumn keyColumn in key.Columns)
					newKey.Columns.Add(TableVar.Columns[SourceTableVar.Columns.IndexOfName(keyColumn.Name)]);
				TableVar.Keys.Add(newKey);
			}
			
			// Inherit orders
			Schema.Order newOrder;
			Schema.OrderColumn orderColumn;
			Schema.OrderColumn newOrderColumn;
			foreach (Schema.Order order in SourceTableVar.Orders)
			{
				newOrder = new Schema.Order();
				newOrder.InheritMetaData(order.MetaData);
				newOrder.IsInherited = true;
				for (int index = 0; index < order.Columns.Count; index++)
				{
					orderColumn = order.Columns[index];
					newOrderColumn =
						new Schema.OrderColumn
						(
							TableVar.Columns[SourceTableVar.Columns.IndexOfName(orderColumn.Column.Name)],
							orderColumn.Ascending,
							orderColumn.IncludeNils
						);
					newOrderColumn.Sort = orderColumn.Sort;
					newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
					Error.AssertWarn(newOrderColumn.Sort != null, "Sort is null");
					if (newOrderColumn.IsDefaultSort)
						plan.AttachDependency(newOrderColumn.Sort);
					else
					{
						if (newOrderColumn.Sort.HasDependencies())
							plan.AttachDependencies(newOrderColumn.Sort.Dependencies);
					}
					newOrder.Columns.Add(newOrderColumn);
				}
				TableVar.Orders.Add(newOrder);
			}
			
			DetermineOrder(plan);
			
			#if UseReferenceDerivation
			// Copy references
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
			{
				if (SourceTableVar.HasReferences())
					foreach (Schema.ReferenceBase reference in SourceTableVar.References)
					{
						if (reference.SourceTable.Equals(SourceTableVar))
						{
							Schema.JoinKey sourceKey = new Schema.JoinKey();
							foreach (Schema.TableVarColumn column in reference.SourceKey.Columns)
								sourceKey.Columns.Add(TableVar.Columns[SourceTableVar.Columns.IndexOfName(column.Name)]);
					
							int newReferenceID = Schema.Object.GetNextObjectID();
							string newReferenceName = DeriveSourceReferenceName(reference, newReferenceID, sourceKey);

							Schema.DerivedReference newReference = new Schema.DerivedReference(newReferenceID, newReferenceName, reference);
							newReference.IsExcluded = reference.IsExcluded;
							newReference.InheritMetaData(reference.MetaData);
							newReference.SourceTable = _tableVar;
							newReference.AddDependency(_tableVar);
							newReference.TargetTable = reference.TargetTable;
							newReference.AddDependency(reference.TargetTable);
							newReference.SourceKey.IsUnique = reference.SourceKey.IsUnique;
							foreach (Schema.TableVarColumn column in sourceKey.Columns)
								newReference.SourceKey.Columns.Add(column);
							newReference.TargetKey.IsUnique = reference.TargetKey.IsUnique;
							foreach (Schema.TableVarColumn column in reference.TargetKey.Columns)
								newReference.TargetKey.Columns.Add(column);
							//newReference.UpdateReferenceAction = reference.UpdateReferenceAction;
							//newReference.DeleteReferenceAction = reference.DeleteReferenceAction;
							_tableVar.References.Add(newReference);
						}
						else if (reference.TargetTable.Equals(SourceTableVar))
						{
							Schema.JoinKey targetKey = new Schema.JoinKey();
							foreach (Schema.TableVarColumn column in reference.TargetKey.Columns)
								targetKey.Columns.Add(TableVar.Columns[SourceTableVar.Columns.IndexOfName(column.Name)]);
				
							int newReferenceID = Schema.Object.GetNextObjectID();
							string newReferenceName = DeriveTargetReferenceName(reference, newReferenceID, targetKey);
				
							Schema.DerivedReference newReference = new Schema.DerivedReference(newReferenceID, newReferenceName, reference);
							newReference.IsExcluded = reference.IsExcluded;
							newReference.InheritMetaData(reference.MetaData);
							newReference.SourceTable = reference.SourceTable;
							newReference.AddDependency(reference.SourceTable);
							newReference.TargetTable = _tableVar;
							newReference.AddDependency(_tableVar);
							newReference.SourceKey.IsUnique = reference.SourceKey.IsUnique;
							foreach (Schema.TableVarColumn column in reference.SourceKey.Columns)
								newReference.SourceKey.Columns.Add(column);
							newReference.TargetKey.IsUnique = reference.TargetKey.IsUnique;
							foreach (Schema.TableVarColumn column in targetKey.Columns)
								newReference.TargetKey.Columns.Add(column);
							//newReference.UpdateReferenceAction = reference.UpdateReferenceAction;
							//newReference.DeleteReferenceAction = reference.DeleteReferenceAction;
							_tableVar.References.Add(newReference);
						}
					}
			}
			#endif
		}
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = SourceNode.CursorType;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					SourceNode.CursorCapabilities & 
					(
						CursorCapability.BackwardsNavigable | 
						CursorCapability.Searchable
					)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;

			DetermineOrder(plan);
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (DataType.Columns.Count > 0)
			{
				RenameExpression expression = new RenameExpression();
				expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
				for (int index = 0; index < DataType.Columns.Count; index++)
					expression.Expressions.Add
					(
						new RenameColumnExpression
						(
							Schema.Object.EnsureRooted(SourceTableType.Columns[index].Name), 
							DataType.Columns[index].Name, 
							TableVar.Columns[index].MetaData == null ? 
								null : 
								TableVar.Columns[index].MetaData.Copy()
						)
					);
				expression.Modifiers = Modifiers;
				return expression;
			}
			else
				return Nodes[0].EmitStatement(mode);
		}
		
		public override object InternalExecute(Program program)
		{
			RenameTable table = new RenameTable(this, program);
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}
		
		public override void DetermineRemotable(Plan plan)
		{
			Schema.ResultTableVar tableVar = (Schema.ResultTableVar)TableVar;
			tableVar.InferredIsDefaultRemotable = !PropagateDefault || SourceTableVar.IsDefaultRemotable;
			tableVar.InferredIsChangeRemotable = !PropagateChange || SourceTableVar.IsChangeRemotable;
			tableVar.InferredIsValidateRemotable = !PropagateValidate || SourceTableVar.IsValidateRemotable;
			tableVar.DetermineRemotable(plan.CatalogDeviceSession);
			
			tableVar.ShouldChange = PropagateChange && (tableVar.ShouldChange || SourceTableVar.ShouldChange);
			tableVar.ShouldDefault = PropagateDefault && (tableVar.ShouldDefault || SourceTableVar.ShouldDefault);
			tableVar.ShouldValidate = PropagateValidate && (tableVar.ShouldValidate || SourceTableVar.ShouldValidate);
			
			for (int index = 0; index < tableVar.Columns.Count; index++)
			{
				Schema.TableVarColumn column = tableVar.Columns[index];
				Schema.TableVarColumn sourceColumn = SourceTableVar.Columns[index];

				column.ShouldChange = PropagateChange && (column.ShouldChange || sourceColumn.ShouldChange);
				tableVar.ShouldChange = tableVar.ShouldChange || column.ShouldChange;

				column.ShouldDefault = PropagateDefault && (column.ShouldDefault || sourceColumn.ShouldDefault);
				tableVar.ShouldDefault = tableVar.ShouldDefault || column.ShouldDefault;

				column.ShouldValidate = PropagateValidate && (column.ShouldValidate || sourceColumn.ShouldValidate);
				tableVar.ShouldValidate = tableVar.ShouldValidate || column.ShouldValidate;
			}
		}
		
		// Validate
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (isDescending && PropagateValidate)
			{
				IRow localOldRow;
				if (!(oldRow.DataType.Columns.Equivalent(DataType.Columns)))
				{
					localOldRow = new Row(program.ValueManager, DataType.RowType);
					oldRow.CopyTo(localOldRow);
				}
				else
					localOldRow = oldRow;
				try
				{
					IRow localNewRow;
					if (!(newRow.DataType.Columns.Equivalent(DataType.Columns)))
					{
						localNewRow = new Row(program.ValueManager, DataType.RowType);
						newRow.CopyTo(localNewRow);
					}
					else
						localNewRow = newRow;
					try
					{
						Row oldSourceRow = new Row(program.ValueManager, SourceNode.DataType.RowType, (NativeRow)localOldRow.AsNative);
						try
						{
							oldSourceRow.ValuesOwned = false;
							
							Row newSourceRow = new Row(program.ValueManager, SourceNode.DataType.RowType, (NativeRow)localNewRow.AsNative);
							try
							{
								newSourceRow.ValuesOwned = false;
								
								bool changed = SourceNode.Validate(program, oldSourceRow, newSourceRow, valueFlags, columnName == String.Empty ? String.Empty : newSourceRow.DataType.Columns[localNewRow.DataType.Columns.IndexOfName(columnName)].Name);
								
								if (changed && !ReferenceEquals(newRow, localNewRow))
									localNewRow.CopyTo(newRow);
								return changed;
							}
							finally
							{
								newSourceRow.Dispose();
							}
						}
						finally
						{
							oldSourceRow.Dispose();
						}
					}
					finally
					{
						if (!ReferenceEquals(newRow, localNewRow))
							localNewRow.Dispose();
					}
				}
				finally
				{
					if (!ReferenceEquals(oldRow, localOldRow))
						localOldRow.Dispose();
				}
			}
			return false;
		}
		
		// Default
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if (isDescending && PropagateDefault)
			{
				TableNode sourceNode = SourceNode;
				
				IRow localOldRow;
				if (!(oldRow.DataType.Columns.Equivalent(DataType.Columns)))
				{
					localOldRow = new Row(program.ValueManager, DataType.RowType);
					oldRow.CopyTo(localOldRow);
				}
				else
					localOldRow = oldRow;
				try
				{
					IRow localNewRow;
					if (!(newRow.DataType.Columns.Equivalent(DataType.Columns)))
					{
						localNewRow = new Row(program.ValueManager, DataType.RowType);
						newRow.CopyTo(localNewRow);
					}
					else
						localNewRow = newRow;
					try
					{
						Row oldSourceRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)localOldRow.AsNative);
						try
						{
							oldSourceRow.ValuesOwned = false;
							
							Row newSourceRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)localNewRow.AsNative);
							try
							{
								newSourceRow.ValuesOwned = false;

								bool changed = sourceNode.Default(program, oldSourceRow, newSourceRow, valueFlags, columnName == String.Empty ? String.Empty : newSourceRow.DataType.Columns[localNewRow.DataType.Columns.IndexOfName(columnName)].Name);
								
								if (changed && (newRow != localNewRow))
									localNewRow.CopyTo(newRow);
								return changed;
							}
							finally
							{
								newSourceRow.Dispose();
							}
						}
						finally
						{
							oldSourceRow.Dispose();
						}
					}
					finally
					{
						if (!ReferenceEquals(newRow, localNewRow))
							localNewRow.Dispose();
					}
				}
				finally
				{
					if (!ReferenceEquals(oldRow, localOldRow))
						localOldRow.Dispose();
				}
			}
			return false;
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			Schema.RowType rowType = new Schema.RowType();
			foreach (Schema.Column column in row.DataType.Columns)
				rowType.Columns.Add(SourceNode.DataType.Columns[DataType.Columns.IndexOfName(column.Name)].Copy());
				
			Row localRow = new Row(program.ValueManager, rowType, (NativeRow)row.AsNative);
			try
			{
				SourceNode.JoinApplicationTransaction(program, localRow);
			}
			finally
			{
				localRow.Dispose();
			}
		}
		
		// Change
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (PropagateChange)
			{
				TableNode sourceNode = SourceNode;

				IRow localOldRow;
				if (!(oldRow.DataType.Columns.Equivalent(DataType.Columns)))
				{
					localOldRow = new Row(program.ValueManager, DataType.RowType);
					oldRow.CopyTo(localOldRow);
				}
				else
					localOldRow = oldRow;
				try
				{
					IRow localNewRow;
					if (!(newRow.DataType.Columns.Equivalent(DataType.Columns)))
					{
						localNewRow = new Row(program.ValueManager, DataType.RowType);
						newRow.CopyTo(localNewRow);
					}
					else
						localNewRow = newRow;
					try
					{
						Row oldSourceRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)localOldRow.AsNative);
						try
						{
							oldSourceRow.ValuesOwned = false;

							Row newSourceRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)localNewRow.AsNative);
							try
							{
								newSourceRow.ValuesOwned = false;

								bool changed = sourceNode.Change(program, oldSourceRow, newSourceRow, valueFlags, columnName == String.Empty ? String.Empty : newSourceRow.DataType.Columns[localNewRow.DataType.Columns.IndexOfName(columnName)].Name);
								
								if (changed && (newRow != localNewRow))
									localNewRow.CopyTo(newRow);
								
								return changed;
							}
							finally
							{
								newSourceRow.Dispose();
							}
						}
						finally
						{
							oldSourceRow.Dispose();
						}
					}
					finally
					{
						if (!ReferenceEquals(newRow, localNewRow))
							localNewRow.Dispose();
					}
				}
				finally
				{
					if (!ReferenceEquals(oldRow, localOldRow))
						localOldRow.Dispose();
				}
			}
			return false;
		}
		
		// Insert
		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsert)
			{
				case PropagateAction.True :
					using (Row insertRow = new Row(program.ValueManager, SourceNode.DataType.RowType, (NativeRow)newRow.AsNative))
					{
						insertRow.ValuesOwned = false;
						SourceNode.Insert(program, oldRow, insertRow, valueFlags, uncheckedValue);
					}
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row insertRow = new Row(program.ValueManager, SourceNode.DataType.RowType, (NativeRow)newRow.AsNative))
					{
						insertRow.ValuesOwned = false;
						using (Row sourceRow = new Row(program.ValueManager, SourceNode.DataType.RowType))
						{
							insertRow.CopyTo(sourceRow);
							using (IRow currentRow = SourceNode.Select(program, sourceRow))
							{
								if (currentRow != null)
								{
									if (PropagateInsert == PropagateAction.Ensure)
										SourceNode.Update(program, currentRow, insertRow, valueFlags, false, uncheckedValue);
								}
								else
									SourceNode.Insert(program, oldRow, insertRow, valueFlags, uncheckedValue);
							}
						}
					}
				break;
			}
		}
		
		// Update
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateUpdate)
			{
				TableNode sourceNode = SourceNode;
				Row localOldRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)oldRow.AsNative);
				try
				{
					localOldRow.ValuesOwned = false;
					Row localNewRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)newRow.AsNative);
					try
					{
						localNewRow.ValuesOwned = false;
						sourceNode.Update(program, localOldRow, localNewRow, valueFlags, checkConcurrency, uncheckedValue);
					}
					finally
					{
						localNewRow.Dispose();
					}
				}
				finally
				{
					localOldRow.Dispose();
				}
			}
		}
		
		// Delete
		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateDelete)
			{
				TableNode sourceNode = SourceNode;
				Row localRow = new Row(program.ValueManager, sourceNode.DataType.RowType, (NativeRow)row.AsNative);
				try
				{
					localRow.ValuesOwned = false;
					sourceNode.Delete(program, localRow, checkConcurrency, uncheckedValue);
				}
				finally
				{
					localRow.Dispose();
				}
			}
		}
	}
}