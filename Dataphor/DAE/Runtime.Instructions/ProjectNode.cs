/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define UseElaborable
#define USENAMEDROWVARIABLES
	
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
	
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
    public abstract class ProjectNodeBase : UnaryTableNode
    {
		// DistinctRequired
		protected bool _distinctRequired;
		public bool DistinctRequired
		{
			get { return _distinctRequired; }
			set { _distinctRequired = value; }
		}
		
		// EqualNode
		protected PlanNode _equalNode;
		public PlanNode EqualNode
		{
			get { return _equalNode; }
			set { _equalNode = value; }
		}
		
		// ColumnNames
		#if USETYPEDLIST
		protected TypedList FColumnNames = new TypedList(typeof(string), false);
		public TypedList ColumnNames { get { return FColumnNames; } }
		#else
		protected BaseList<string> _columnNames = new BaseList<string>();
		public BaseList<string> ColumnNames { get { return _columnNames; } }
		#endif
		
		protected abstract void DetermineColumns(Plan plan);
		
		// DetermineDataType
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			
			DetermineColumns(plan);
			DetermineRemotable(plan);

			// copy all non-sparse keys with all fields preserved in the projection
			CopyPreservedKeys(SourceTableVar.Keys, false, false);

			// if at least one non-sparse key is preserved, then we are free to copy preserved sparse keys as well			
			if (TableVar.Keys.Count > 0)
				CopyPreservedKeys(SourceTableVar.Keys, false, true);
			
			CopyPreservedOrders(SourceTableVar.Orders);

			_distinctRequired = (TableVar.Keys.Count == 0);
			if (_distinctRequired)
			{
				Schema.Key newKey = new Schema.Key();
				newKey.IsInherited = true;
				foreach (Schema.TableVarColumn column in TableVar.Columns)
				    newKey.Columns.Add(column);
				TableVar.Keys.Add(newKey);
				if (newKey.Columns.Count > 0)
					Nodes[0] = Compiler.EmitOrderNode(plan, SourceNode, newKey, true);
			
				plan.EnterRowContext();
				try
				{	
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Right)));
						#endif
						try
						{
							_equalNode = 
								Compiler.CompileExpression
								(
									plan, 
									#if USENAMEDROWVARIABLES
									Compiler.BuildRowEqualExpression
									(
										plan, 
										Keywords.Left,
										Keywords.Right,
										newKey.Columns,
										newKey.Columns
									)
									#else
									Compiler.BuildRowEqualExpression
									(
										APlan, 
										new Schema.RowType(LNewKey.Columns, Keywords.Left).Columns, 
										new Schema.RowType(LNewKey.Columns, Keywords.Right).Columns
									)
									#endif
								);
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
				
				Order = Compiler.OrderFromKey(plan, Compiler.FindClusteringKey(plan, TableVar));
			}
			else
			{
				if ((SourceNode.Order != null) && SourceNode.Order.Columns.IsSubsetOf(TableVar.Columns))
					Order = CopyOrder(SourceNode.Order);
			}
			
			if ((Order != null) && !TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			base.InternalBindingTraversal(plan, visitor);
			if (_distinctRequired)
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Right)));
						#endif
						try
						{
							#if USEVISIT
							_equalNode = visitor.Visit(plan, _equalNode);
							#else
							_equalNode.BindingTraversal(plan, visitor);
							#endif
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

			// If this node has no determined order, it is because the projection did not affect cardinality, and we may therefore assume the order of the source, as long as it's columns have been preserved by the projection
			if ((SourceNode.Order != null) && SourceNode.Order.Columns.IsSubsetOf(TableVar.Columns))
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		// Execute
		public override object InternalExecute(Program program)
		{
			ProjectTable table = new ProjectTable(this, program);
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
		
		// Change
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (PropagateChange)
			{
				// select the current row from the base node using the given row
				IRow sourceRow = new Row(program.ValueManager, SourceNode.DataType.RowType);
				try
				{
					newRow.CopyTo(sourceRow);
					IRow currentRow = null;
					if (!program.ServerProcess.ServerSession.Server.IsEngine)
						currentRow = SourceNode.Select(program, sourceRow);
					try
					{
						if (currentRow == null)
							currentRow = new Row(program.ValueManager, SourceNode.DataType.RowType);
						newRow.CopyTo(currentRow);
						BitArray localValueFlags = valueFlags != null ? new BitArray(currentRow.DataType.Columns.Count) : null;
						if (localValueFlags != null)
							for (int index = 0; index < localValueFlags.Length; index++)
							{
								int rowIndex = newRow.DataType.Columns.IndexOfName(currentRow.DataType.Columns[index].Name);
								localValueFlags[index] = rowIndex >= 0 ? valueFlags[rowIndex] : false;
							}
						bool changed = SourceNode.Change(program, oldRow, currentRow, localValueFlags, columnName);
						if (changed)
							currentRow.CopyTo(newRow);
						return changed;
					}
					finally
					{
						currentRow.Dispose();
					}
				}
				finally
				{
					sourceRow.Dispose();
				}
			}
			return false;
		}

		public override void JoinApplicationTransaction(Program program, IRow row) 
		{
			Schema.RowType rowType = new Schema.RowType();
			
			foreach (Schema.Column column in row.DataType.Columns)
				rowType.Columns.Add(column.Copy());
			foreach (Schema.Column column in SourceNode.DataType.Columns)
				if (!DataType.Columns.ContainsName(column.Name))	
					rowType.Columns.Add(column.Copy());
			Row localRow = new Row(program.ValueManager, rowType);
			try
			{
				row.CopyTo(localRow);

				// Get the SourceNode select set for this row, and join on each result
				foreach (var sourceRow in SourceNode.SelectAll(program, localRow))
				{
					try
					{
						base.JoinApplicationTransaction(program, sourceRow);
					}
					finally
					{
						sourceRow.Dispose();
					}
				}
			}
			finally
			{
				localRow.Dispose();
			}
		}
    }

	// operator iProject(table{}, list{string}) : table{}
    public class ProjectNode : ProjectNodeBase
    {
		protected override void DetermineColumns(Plan plan)
		{
			// Determine project columns
			Schema.TableVarColumn tableVarColumn;
			foreach (string columnName in _columnNames)
			{
				tableVarColumn = SourceTableVar.Columns[columnName].Inherit();
				TableVar.Columns.Add(tableVarColumn);
				DataType.Columns.Add(tableVarColumn.Column);
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			ProjectExpression expression = new ProjectExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			foreach (Schema.TableVarColumn column in TableVar.Columns)
				expression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(column.Name)));
			expression.Modifiers = Modifiers;
			return expression;
		}
    }
    
    public class RemoveNode : ProjectNodeBase
    {
		protected override void DetermineColumns(Plan plan)
		{
			// this loop is done this way to ensure that the columns are looked up
			// using the name resolution algorithms found in the Columns object,
			// and to ensure that all the columns named in the remove list are valid
			bool found;
			int columnIndex;
			Schema.TableVarColumn column;
			for (int index = 0; index < SourceTableVar.Columns.Count; index++)
			{
				found = false;
				foreach (string columnName in _columnNames)
				{
					columnIndex = SourceTableVar.Columns.IndexOf(columnName);
					if (columnIndex < 0)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, columnName);
					else if (columnIndex == index)
					{
						found = true;
						break;
					}
				}
			
				if (!found)
				{
					column = SourceTableVar.Columns[index].Inherit();
					DataType.Columns.Add(column.Column);
					TableVar.Columns.Add(column);
				}
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			RemoveExpression expression = new RemoveExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			foreach (Schema.TableVarColumn column in SourceTableVar.Columns)
				if (!TableVar.Columns.ContainsName(column.Name))
					expression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(column.Name)));
			expression.Modifiers = Modifiers;
			return expression;
		}
    }
}