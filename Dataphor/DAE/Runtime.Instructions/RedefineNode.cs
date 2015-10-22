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
using Alphora.Dataphor.DAE.Compiling.Visitors;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator iRedefine(table{}, object) : table{}
	public class RedefineNode : UnaryTableNode
	{
		protected int[] _redefineColumnOffsets;		
		public int[] RedefineColumnOffsets
		{
			get { return _redefineColumnOffsets; }
			set { _redefineColumnOffsets = value; }
		}
		
		private NamedColumnExpressions _expressions;
		public NamedColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}
		
		private bool _distinctRequired;
		public bool DistinctRequired
		{
			get { return _distinctRequired; }
			set { _distinctRequired = value; }
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);

			int index = 0;			
			_redefineColumnOffsets = new int[_expressions.Count];
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
								Schema.TableVarColumn sourceColumn;
								Schema.TableVarColumn tempColumn;
								Schema.TableVarColumn newColumn;
								foreach (NamedColumnExpression column in _expressions)
								{
									int sourceColumnIndex = TableVar.Columns.IndexOf(column.ColumnAlias);
									if (sourceColumnIndex < 0)
										throw new CompilerException(CompilerException.Codes.UnknownIdentifier, column, column.ColumnAlias);
										
									sourceColumn = TableVar.Columns[sourceColumnIndex];
									tempColumn = CopyTableVarColumn(sourceColumn);

									plan.PushCreationObject(tempColumn);
									try
									{
										planNode = Compiler.CompileExpression(plan, column.Expression);
									}
									finally
									{
										plan.PopCreationObject();
									}
									
									newColumn = CopyTableVarColumn(sourceColumn);
									newColumn.Column.DataType = planNode.DataType;
									if (tempColumn.HasDependencies())
										newColumn.AddDependencies(tempColumn.Dependencies);
									Schema.Object objectValue;
									if (newColumn.HasDependencies())
										for (int dependencyIndex = 0; index < newColumn.Dependencies.Count; index++)
										{
											objectValue = newColumn.Dependencies.ResolveObject(plan.CatalogDeviceSession, dependencyIndex);
											plan.AttachDependency(objectValue);
											newColumn.IsNilable = planNode.IsNilable;
											newColumn.IsChangeRemotable = newColumn.IsChangeRemotable && objectValue.IsRemotable;
											newColumn.IsDefaultRemotable = newColumn.IsDefaultRemotable && objectValue.IsRemotable;
										}

									DataType.Columns[sourceColumnIndex] = newColumn.Column;
									TableVar.Columns[sourceColumnIndex] = newColumn;
									_redefineColumnOffsets[index] = sourceColumnIndex;
									Nodes.Add(planNode);
									index++;
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
				bool add = true;
				foreach (Schema.TableVarColumn column in key.Columns)
					if (((IList)_redefineColumnOffsets).Contains(TableVar.Columns.IndexOfName(column.Name)))
					{
						add = false;
						break;
					}
					
				if (add)
					TableVar.Keys.Add(CopyKey(key));
			}
			
			_distinctRequired = TableVar.Keys.Count == 0;
			if (_distinctRequired)
			{
				Schema.Key newKey = new Schema.Key();
				foreach (Schema.TableVarColumn column in TableVar.Columns)
					newKey.Columns.Add(column);
				newKey.IsInherited = true;
				TableVar.Keys.Add(newKey);
			}
			
			foreach (Schema.Order order in SourceTableVar.Orders)
			{
				bool add = true;
				for (int columnIndex = 0; columnIndex < order.Columns.Count; columnIndex++)
					if (((IList)_redefineColumnOffsets).Contains(TableVar.Columns.IndexOfName(order.Columns[columnIndex].Column.Name)))
					{
						add = false;
						break;
					}
					
				if (add)
					TableVar.Orders.Add(CopyOrder(order));
			}
			
			DetermineOrder(plan);
			
			// TODO: Reference derivation on a redefine should exclude affected references
			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			// Exclude any columns from AKey which were included by this node
			Schema.RowType rowType = new Schema.RowType();
			foreach (Schema.Column column in row.DataType.Columns)
				if (!((IList)_redefineColumnOffsets).Contains(_tableVar.DataType.Columns.IndexOfName(column.Name)))
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

		public void DetermineOrder(Plan plan)
		{
			Order = null;
			if (SourceNode.Order != null)
			{
				bool add = true;
				for (int index = 0; index < SourceNode.Order.Columns.Count; index++)
					if (((IList)_redefineColumnOffsets).Contains(TableVar.Columns.IndexOfName(SourceNode.Order.Columns[index].Column.Name)))
					{
						add = false;
						break;
					}
				
				if (add)
					Order = CopyOrder(SourceNode.Order);
			}
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
			
			DetermineOrder(plan);
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			RedefineExpression expression = new RedefineExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			for (int index = 0; index < _redefineColumnOffsets.Length; index++)
				expression.Expressions.Add(new NamedColumnExpression((Expression)Nodes[index + 1].EmitStatement(mode), DataType.Columns[_redefineColumnOffsets[index]].Name));
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
			RedefineTable table = new RedefineTable(this, program);
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
				// Evaluate the Redefined columns
				// TODO: This change code should only run if the column changing can be determined to affect the extended columns...
				int columnIndex;
				for (int index = 0; index < _redefineColumnOffsets.Length; index++)
				{
					Schema.TableVarColumn column = TableVar.Columns[_redefineColumnOffsets[index]];
					if ((isDefault || column.IsComputed) && (!program.ServerProcess.ServerSession.Server.IsEngine || column.IsChangeRemotable))
					{
						columnIndex = row.DataType.Columns.IndexOfName(column.Name);
						if (columnIndex >= 0)
						{
							if (!isDefault || !row.HasValue(columnIndex))
							{
								row[columnIndex] = Nodes[index + 1].Execute(program);
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
			if ((columnName == String.Empty) || SourceTableVar.Columns.ContainsName(columnName))
				return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
			return false;
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