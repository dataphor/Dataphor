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
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Compiling.Visitors;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;
using System.Linq;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator iExtend(table{}, object) : table{}
	public class ExtendNode : UnaryTableNode
	{
		protected int _extendColumnOffset;		
		public int ExtendColumnOffset
		{
			get { return _extendColumnOffset; }
		}
		
		private NamedColumnExpressions _expressions;
		public NamedColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			_extendColumnOffset = TableVar.Columns.Count;

			// This structure will track key columns as a set of sets, and any extended columns that are equivalent to them
			Dictionary<string, Schema.Key> keyColumns = new Dictionary<string, Schema.Key>();
			foreach (Schema.TableVarColumn tableVarColumn in TableVar.Columns)
				if (SourceTableVar.Keys.IsKeyColumnName(tableVarColumn.Name) && !keyColumns.ContainsKey(tableVarColumn.Name))
					keyColumns.Add(tableVarColumn.Name, new Schema.Key(new Schema.TableVarColumn[]{tableVarColumn}));
			
			ApplicationTransaction transaction = null;
			if (plan.ApplicationTransactionID != Guid.Empty)
				transaction = plan.GetApplicationTransaction();
			try
			{
				if (transaction != null)
					transaction.PushLookup();
				try
				{
					plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.None));
					try
					{
						plan.EnterRowContext();
						try
						{			
							plan.Symbols.Push(new Symbol(String.Empty, SourceTableType.RowType));
							try
							{
								// Add a column for each expression
								PlanNode planNode;
								Schema.TableVarColumn newColumn;
								foreach (NamedColumnExpression column in _expressions)
								{
									newColumn = new Schema.TableVarColumn(new Schema.Column(column.ColumnAlias, plan.DataTypes.SystemScalar));
									plan.PushCreationObject(newColumn);
									try
									{
										planNode = Compiler.CompileExpression(plan, column.Expression);
									}
									finally
									{
										plan.PopCreationObject();
									}
									
									bool isChangeRemotable = true;
									if (newColumn.HasDependencies())
										for (int index = 0; index < newColumn.Dependencies.Count; index++)
										{
											Schema.Object objectValue = newColumn.Dependencies.ResolveObject(plan.CatalogDeviceSession, index);
											isChangeRemotable = isChangeRemotable && objectValue.IsRemotable;
											plan.AttachDependency(objectValue);
										}

									bool isUpdatable = planNode is TableNode || planNode is ExtractRowNode;

									newColumn = 
										new Schema.TableVarColumn
										(
											new Schema.Column(column.ColumnAlias, planNode.DataType),
											column.MetaData, 
											isUpdatable ? Schema.TableVarColumnType.Stored : Schema.TableVarColumnType.Virtual
										);

									newColumn.IsNilable = planNode.IsNilable;
									newColumn.IsChangeRemotable = isChangeRemotable;
									newColumn.IsDefaultRemotable = isChangeRemotable;

									DataType.Columns.Add(newColumn.Column);
									TableVar.Columns.Add(newColumn);
									
									string columnName = String.Empty;
									if (IsColumnReferencing(planNode, ref columnName))
									{
										// TODO: In theory we could allow updatability through an IsColumnReferencing add column as well
										Schema.TableVarColumn referencedColumn = TableVar.Columns[columnName];
										if (SourceTableVar.Keys.IsKeyColumnName(referencedColumn.Name))
										{
											Schema.Key key;
											if (keyColumns.TryGetValue(referencedColumn.Name, out key))
												key.Columns.Add(newColumn);
											else
												keyColumns.Add(referencedColumn.Name, new Schema.Key(new Schema.TableVarColumn[]{newColumn}));
										}
									}
									
									Nodes.Add(planNode);
								}

								DetermineRemotable(plan);
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
						plan.PopCursorContext();
					}
				}
				finally
				{
					if (transaction != null)
						transaction.PopLookup();
				}
			}
			finally
			{
				if (transaction != null)
					Monitor.Exit(transaction);
			}
			
			foreach (Schema.Key key in SourceTableVar.Keys)
			{
				// Seed the result key set with the empty set
				Schema.Keys resultKeys = new Schema.Keys();
				resultKeys.Add(new Schema.Key());
				
				foreach (Schema.TableVarColumn column in key.Columns)
					resultKeys = KeyProduct(resultKeys, keyColumns[column.Name]);
					
				foreach (Schema.Key resultKey in resultKeys)
				{
					resultKey.IsSparse = key.IsSparse;
					resultKey.IsInherited = true;
					resultKey.MergeMetaData(key.MetaData);
					TableVar.Keys.Add(resultKey);
				}
			}
			
			CopyOrders(SourceTableVar.Orders);
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
		}
		
		protected Schema.Keys KeyProduct(Schema.Keys keys, Schema.Key key)
		{
			Schema.Keys result = new Schema.Keys();
			foreach (Schema.Key localKey in keys)
				foreach (Schema.TableVarColumn newColumn in key.Columns)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.Columns.AddRange(localKey.Columns);
					newKey.Columns.Add(newColumn);
					result.Add(newKey);
				}
			return result;
		}
		
		protected bool IsColumnReferencing(PlanNode node, ref string columnName)
		{
			StackColumnReferenceNode localNode = node as StackColumnReferenceNode;
			if ((localNode != null) && (localNode.Location == 0))
			{
				columnName = localNode.Identifier;
				return true;
			}
			else if (node.IsOrderPreserving && (node.NodeCount == 1))
				return IsColumnReferencing(node.Nodes[0], ref columnName);
			else
				return false;
		}

		protected bool ReferencesUpdatedColumn(PlanNode node, BitArray valueFlags)
		{
			IList<string> columnReferences = new List<string>();
			if (!node.IsContextLiteral(0, columnReferences))
			{
				// If we cannot tell which column was referenced (variable level reference)
				// Or we cannot tell what columns were updated (no value flags)
				// Or any column references were to columns that were updated
				if (columnReferences.Count == 0 || valueFlags == null || (columnReferences.Select(c => DataType.Columns.IndexOfName(c)).Any(i => i >= 0 && i < _extendColumnOffset && valueFlags[i])))
				{
					return true;
				}
			}

			return false;
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
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;

			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			ExtendExpression expression = new ExtendExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			for (int index = ExtendColumnOffset; index < TableVar.Columns.Count; index++)
				expression.Expressions.Add
				(
					new NamedColumnExpression
					(
						(Expression)Nodes[index - ExtendColumnOffset + 1].EmitStatement(mode), 
						DataType.Columns[index].Name, 
						(MetaData)(TableVar.Columns[index].MetaData == null ? 
							null : 
							TableVar.Columns[index].MetaData.Copy()
						)
					)
				);
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, SourceTableType.RowType));
				try
				{
					for (int index = 1; index < Nodes.Count; index++)
						#if USEVISIT
						Nodes[index] = visitor.Visit(plan, Nodes[index]);
						#else
						Nodes[index].BindingTraversal(plan, visitor);
						#endif
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
		
		public override object InternalExecute(Program program)
		{
			ExtendTable table = new ExtendTable(this, program);
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
			base.DetermineRemotable(plan);
			
			_tableVar.ShouldChange = true;
			_tableVar.ShouldDefault = true;
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				column.ShouldChange = true;
				column.ShouldDefault = true;
			}
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			bool changed = false;
			if ((columnName == String.Empty) || (SourceNode.DataType.Columns.ContainsName(columnName)))
				changed = base.InternalDefault(program, oldRow, newRow, valueFlags, columnName, isDescending);
			return InternalInternalChange(newRow, columnName, program, true) || changed;
		}
		
		protected bool InternalInternalChange(IRow row, string columnName, Program program, bool isDefault)
		{
			bool changed = false;
			PushRow(program, row);
			try
			{
				// Evaluate the Extended columns
				// TODO: This change code should only run if the column changing can be determined to affect the extended columns...
				int columnIndex;
				for (int index = 1; index < Nodes.Count; index++)
				{
					Schema.TableVarColumn column = TableVar.Columns[_extendColumnOffset + index - 1];
					if ((isDefault || column.IsComputed) && (!program.ServerProcess.ServerSession.Server.IsEngine || column.IsChangeRemotable))
					{
						columnIndex = row.DataType.Columns.IndexOfName(column.Column.Name);
						if (columnIndex >= 0)
						{
							if (!isDefault || !row.HasValue(columnIndex))
							{
								row[columnIndex] = Nodes[index].Execute(program);
								changed = true;
							}
						}
					}
				}
				return changed;
			}
			finally
			{
				PopRow(program);
			}
		}
		
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			bool changed = false;
			if ((columnName == String.Empty) || (SourceNode.DataType.Columns.ContainsName(columnName)))
				changed = base.InternalChange(program, oldRow, newRow, valueFlags, columnName);
			return InternalInternalChange(newRow, columnName, program, false) || changed;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
			return false;
		}
		
		// Insert
		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			base.InternalExecuteInsert(program, oldRow, newRow, valueFlags, uncheckedValue);

			if (PropagateInsert != PropagateAction.False)
			{
				PushRow(program, newRow);
				try
				{
					int columnIndex;
					for (int index = 1; index < Nodes.Count; index++)
					{
						Schema.TableVarColumn column = TableVar.Columns[_extendColumnOffset + index - 1];
						if (column.ColumnType == Schema.TableVarColumnType.Stored)
						{
							columnIndex = newRow.DataType.Columns.IndexOfName(column.Column.Name);
							if (columnIndex >= 0)
							{
								TableNode tableNode = Nodes[index] as TableNode;
								if (tableNode == null)
								{
									ExtractRowNode extractRowNode = Nodes[index] as ExtractRowNode;
									if (extractRowNode != null)
									{
										tableNode = (TableNode)extractRowNode.Nodes[0];
									}
								}

								if (tableNode == null)
									throw new RuntimeException(RuntimeException.Codes.InternalError, "Could not determine update path for extend column.");

								IDataValue newValue = newRow.GetValue(columnIndex);
								if (!newValue.IsNil)
								{
									IRow newRowValue = newValue as IRow;
									if (newRowValue != null)
									{
										PerformInsert(program, tableNode, null, newRowValue, null, uncheckedValue);
									}
									else
									{
										TableValue newTableValue = (TableValue)newValue;
										using (ITable newTableCursor = newTableValue.OpenCursor())
										{
											while (newTableCursor.Next())
											{
												using (IRow newTableCursorRow = newTableCursor.Select())
												{
													PerformInsert(program, tableNode, null, newTableCursorRow, null, uncheckedValue);
												}
											}
										}
									}
								}
							}
						}
					}
				}
				finally
				{
					PopRow(program);
				}
			}
		}

		private void PerformInsert(Program program, TableNode tableNode, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsert)
			{
				case PropagateAction.True:
					tableNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					break;

				case PropagateAction.Ensure:
				case PropagateAction.Ignore:
					using (Row sourceRow = new Row(program.ValueManager, tableNode.DataType.RowType))
					{
						newRow.CopyTo(sourceRow);
						using (IRow currentRow = tableNode.Select(program, sourceRow))
						{
							if (currentRow != null)
							{
								if (PropagateInsert == PropagateAction.Ensure)
									tableNode.Update(program, currentRow, newRow, valueFlags, false, uncheckedValue);
							}
							else
								tableNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
						}
					}
					break;
			}
		}
		
		// Update
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			base.InternalExecuteUpdate(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);

			if (PropagateUpdate)
			{
				int columnIndex;
				for (int index = 1; index < Nodes.Count; index++)
				{
					Schema.TableVarColumn column = TableVar.Columns[_extendColumnOffset + index - 1];
					if (column.ColumnType == Schema.TableVarColumnType.Stored)
					{
						columnIndex = newRow.DataType.Columns.IndexOfName(column.Column.Name);
						if (columnIndex >= 0)
						{
							TableNode tableNode = Nodes[index] as TableNode;
							if (tableNode == null)
							{
								ExtractRowNode extractRowNode = Nodes[index] as ExtractRowNode;
								if (extractRowNode != null)
								{
									tableNode = (TableNode)extractRowNode.Nodes[0];
								}
							}

							if (tableNode == null)
								throw new RuntimeException(RuntimeException.Codes.InternalError, "Could not determine update path for extend column.");

							bool referencesUpdatedColumn = ReferencesUpdatedColumn(tableNode, valueFlags);

							// If the value is a row
								// If the newValue is nil
									// If the oldValue is not nil
										// delete the table node
								// else
									// If the oldValue is nil
										// insert the row
									// else
										// update the row

							// If the value is a table
								// If the newValue is nil
									// If the oldValue is not nil
										// delete all rows
								// else
									// If the oldValue is nil
										// insert all rows
									// else
										// foreach row in oldvalue
											// if there is a corresponding row in new value by the clustering key
												// update the row
											// else
												// delete the row
										// for each row in newvalue
											// if there is no corresponding row in old value by the clustering key
												// insert the row

							if (column.DataType is Schema.IRowType)
							{
								IRow oldValue = (IRow)oldRow.GetValue(columnIndex);
								IRow newValue = (IRow)newRow.GetValue(columnIndex);
								if (newValue.IsNil)
								{
									if (!oldValue.IsNil)
									{
										PushRow(program, oldRow);
										try
										{
											tableNode.Delete(program, oldValue, checkConcurrency, uncheckedValue);
										}
										finally
										{
											PopRow(program);
										}
									}
								}
								else
								{
									if (oldValue.IsNil)
									{
										PushRow(program, newRow);
										try
										{
											tableNode.Insert(program, null, newValue, null, uncheckedValue);
										}
										finally
										{
											PopRow(program);
										}
									}
									else
									{
										if (referencesUpdatedColumn)
										{
											PushRow(program, oldRow);
											try
											{
												tableNode.Delete(program, oldValue, checkConcurrency, uncheckedValue);
											}
											finally
											{
												PopRow(program);
											}

											PushRow(program, newRow);
											try
											{
												tableNode.Insert(program, null, newValue, null, uncheckedValue);
											}
											finally
											{
												PopRow(program);
											}
										}
										else
										{
											PushRow(program, newRow);
											try
											{
												tableNode.Update(program, oldValue, newValue, null, checkConcurrency, uncheckedValue);
											}
											finally
											{
												PopRow(program);
											}
										}
									}
								}
							}
							else
							{
								TableValue oldValue = (TableValue)oldRow.GetValue(columnIndex);
								TableValue newValue = (TableValue)newRow.GetValue(columnIndex);

								if (newValue.IsNil)
								{
									if (!oldValue.IsNil)
									{
										PushRow(program, oldRow);
										try
										{
											using (ITable oldValueCursor = oldValue.OpenCursor())
											{
												while (oldValueCursor.Next())
												{
													using (IRow oldValueCursorRow = oldValueCursor.Select())
													{
														tableNode.Delete(program, oldValueCursorRow, checkConcurrency, uncheckedValue);
													}
												}
											}
										}
										finally
										{
											PopRow(program);
										}
									}
								}
								else
								{
									if (referencesUpdatedColumn)
									{
										PushRow(program, oldRow);
										try
										{
											using (ITable oldValueCursor = oldValue.OpenCursor())
											{
												while (oldValueCursor.Next())
												{
													using (IRow oldValueCursorRow = oldValueCursor.Select())
													{
														tableNode.Delete(program, oldValueCursorRow, checkConcurrency, uncheckedValue);
													}
												}
											}
										}
										finally
										{
											PopRow(program);
										}

										PushRow(program, newRow);
										try
										{
											using (ITable newValueCursor = newValue.OpenCursor())
											{
												while (newValueCursor.Next())
												{
													using (IRow newValueCursorRow = newValueCursor.Select())
													{
														tableNode.Insert(program, null, newValueCursorRow, null, uncheckedValue);
													}
												}
											}
										}
										finally
										{
											PopRow(program);
										}
									}
									else
									{
										PushRow(program, newRow);
										try
										{
											if (oldValue.IsNil)
											{
												using (ITable newValueCursor = newValue.OpenCursor())
												{
													while (newValueCursor.Next())
													{
														using (IRow newValueCursorRow = newValueCursor.Select())
														{
															tableNode.Insert(program, null, newValueCursorRow, null, uncheckedValue);
														}
													}
												}
											}
											else
											{
												using (ITable oldValueCursor = oldValue.OpenCursor())
												{
													using (ITable newValueCursor = newValue.OpenCursor())
													{
														while (oldValueCursor.Next())
														{
															using (IRow oldValueCursorRow = oldValueCursor.Select())
															{
																if (newValueCursor.FindKey(oldValueCursorRow))
																{
																	using (IRow newValueCursorRow = newValueCursor.Select())
																	{
																		tableNode.Update(program, oldValueCursorRow, newValueCursorRow, null, checkConcurrency, uncheckedValue);
																	}
																}
																else
																{
																	tableNode.Delete(program, oldValueCursorRow, checkConcurrency, uncheckedValue);
																}
															}
														}

														newValueCursor.Reset();

														while (newValueCursor.Next())
														{
															using (IRow newValueCursorRow = newValueCursor.Select())
															{
																if (!oldValueCursor.FindKey(newValueCursorRow))
																{
																	tableNode.Insert(program, null, newValueCursorRow, null, uncheckedValue);
																}
															}
														}
													}
												}
											}
										}
										finally
										{
											PopRow(program);
										}
									}
								}
							}
						}
					}
				}
			}
		}
		
		// Delete
		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			base.InternalExecuteDelete(program, row, checkConcurrency, uncheckedValue);

			if (PropagateDelete)
			{
				PushRow(program, row);
				try
				{
					int columnIndex;
					for (int index = 1; index < Nodes.Count; index++)
					{
						Schema.TableVarColumn column = TableVar.Columns[_extendColumnOffset + index - 1];
						if (column.ColumnType == Schema.TableVarColumnType.Stored)
						{
							columnIndex = row.DataType.Columns.IndexOfName(column.Column.Name);
							if (columnIndex >= 0)
							{
								TableNode tableNode = Nodes[index] as TableNode;
								if (tableNode == null)
								{
									ExtractRowNode extractRowNode = Nodes[index] as ExtractRowNode;
									if (extractRowNode != null)
									{
										tableNode = (TableNode)extractRowNode.Nodes[0];
									}
								}

								if (tableNode == null)
									throw new RuntimeException(RuntimeException.Codes.InternalError, "Could not determine update path for extend column.");

								IDataValue oldValue = row.GetValue(columnIndex);
								if (!oldValue.IsNil)
								{
									IRow oldRowValue = oldValue as IRow;
									if (oldRowValue != null)
									{
										tableNode.Delete(program, oldRowValue, checkConcurrency, uncheckedValue);
									}
									else
									{
										TableValue oldTableValue = (TableValue)oldValue;
										using (ITable oldTableCursor = oldTableValue.OpenCursor())
										{
											while (oldTableCursor.Next())
											{
												using (IRow oldTableCursorRow = oldTableCursor.Select())
												{
													tableNode.Delete(program, oldTableCursorRow, checkConcurrency, uncheckedValue);
												}
											}
										}
									}
								}
							}
						}
					}
				}
				finally
				{
					PopRow(program);
				}
			}
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			// Exclude any columns from AKey which were included by this node
			Schema.RowType rowType = new Schema.RowType();
			foreach (Schema.Column column in row.DataType.Columns)
				if (SourceNode.DataType.Columns.ContainsName(column.Name))
					rowType.Columns.Add(column.Copy());

			Row localRow = new Row(program.ValueManager, rowType);
			try
			{
				row.CopyTo(localRow);
				SourceNode.JoinApplicationTransaction(program, localRow);
			}
			finally
			{
				localRow.Dispose();
			}
		}
		
		public override bool IsContextLiteral(int location, IList<string> columnReferences)
		{
			if (!Nodes[0].IsContextLiteral(location, columnReferences))
				return false;
			for (int index = 1; index < Nodes.Count; index++)
				if (!Nodes[index].IsContextLiteral(location + 1, columnReferences))
					return false;
			return true;
		}
	}
}