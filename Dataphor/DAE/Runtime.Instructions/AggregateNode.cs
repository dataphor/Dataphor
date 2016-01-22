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

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
    // operator iAggregate(table{}) : table{}
	public class AggregateNode : UnaryTableNode
	{
		private ColumnExpressions _columns;
		public ColumnExpressions Columns
		{
			get { return _columns; }
			set { _columns = value; }
		}
		
		private AggregateColumnExpressions _computeColumns;
		public AggregateColumnExpressions ComputeColumns
		{
			get { return _computeColumns; }
			set { _computeColumns = value; }
		}
		
		protected int _aggregateColumnOffset;		
		public int AggregateColumnOffset
		{
			get { return _aggregateColumnOffset; }
			set { _aggregateColumnOffset = value; }
		}
		
		// The original source node
		protected PlanNode _sourceNode;
		public PlanNode OriginalSourceNode { get { return _sourceNode; } }
		
		// AggregateNode
		//		Nodes[0] = Project over {by Columns}
		//			Nodes[0] = ASourceNode
		//		Nodes[1..AggregateExpression.Count] = PlanNode - class determined by lookup from the server catalog
		//			Nodes[0] = Project over {aggregate columns for this expression}
		//				Nodes[0] = Restrict
		//					Nodes[0] = ASourceNode
		//					Nodes[1] = Condition over the first key in the project of the aggregate source (AggregateNode.Nodes[0]);
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);

			_sourceNode = SourceNode;
			
			// TODO: Aggregation source is required to be deterministic because it is long handed, we should do something that allows non-deterministic sources for aggregation
			if (!_sourceNode.IsRepeatable)
				throw new CompilerException(CompilerException.Codes.InvalidAggregationSource, plan.CurrentStatement());

			if (_columns.Count > 0)
			{
				ProjectNode projectNode = (ProjectNode)Compiler.EmitProjectNode(plan, SourceNode, _columns, true);
				Nodes[0] = projectNode;
			}
			else
			{
				Schema.TableType tableType = new Schema.TableType();
				TableSelectorNode node = new TableSelectorNode(tableType);
				node.TableVar.Keys.Add(new Schema.Key());
				node.Nodes.Add(new RowSelectorNode(new Schema.RowType()));
				node.DetermineCharacteristics(plan);
				Nodes[0] = node;
			}
			
			CopyTableVarColumns(SourceTableVar.Columns);
			CopyKeys(SourceTableVar.Keys);
			CopyOrders(SourceTableVar.Orders);
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			
			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
			
			_aggregateColumnOffset = TableVar.Columns.Count;

			Schema.Key compareKey = Compiler.FindClusteringKey(plan, TableVar);
			
			// Add the computed columns
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Source)));
				try
				{
					Schema.RowType rowType = new Schema.RowType(compareKey.Columns);
					Schema.RowType sourceRowType = new Schema.RowType(compareKey.Columns, Keywords.Source);
					Schema.TableVarColumn newColumn;
					foreach (AggregateColumnExpression expression in _computeColumns)
					{
						PlanNode sourceNode = null;
						string[] columnNames = new string[expression.Columns.Count];
						for (int index = 0; index < expression.Columns.Count; index++)
							columnNames[index] = expression.Columns[index].ColumnName;

						if (expression.Distinct)
							sourceNode = Compiler.EmitProjectNode(plan, _sourceNode, columnNames, true);
						else
							sourceNode = _sourceNode;
							
						for (int index = 0; index < columnNames.Length; index++)
							if (((TableNode)sourceNode).TableVar.Columns.IndexOf(columnNames[index]) < 0)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, columnNames[index]);
							
						OperatorBindingContext context = new OperatorBindingContext(expression, expression.AggregateOperator, plan.NameResolutionPath, Compiler.AggregateSignatureFromArguments(sourceNode, columnNames, true), false);
						PlanNode aggregateNode = Compiler.EmitAggregateCallNode(plan, context, sourceNode, columnNames, expression.HasByClause ? expression.OrderColumns : null);
						Compiler.CheckOperatorResolution(plan, context);
						sourceNode = aggregateNode.Nodes[0]; // Make sure to preserve any conversion and casting done by the operator resolution
						
						int stackDisplacement = ((AggregateCallNode)aggregateNode).Operator.Initialization.StackDisplacement + 1; // add 1 to account for the result variable
						stackDisplacement += columnNames.Length;
						for (int index = 0; index < stackDisplacement; index++)
							plan.Symbols.Push(new Symbol(String.Empty, plan.DataTypes.SystemScalar));
						try
						{
							// Walk sourceNode (assuming an n-length list of unary table operators) until _sourceNode is found
							// Insert a restriction between it and a recompile of _sourceNode (to account for possible context changes)
							
							if (sourceNode == _sourceNode)
								sourceNode = Compiler.EmitRestrictNode(plan, Compiler.CompileExpression(plan, (Expression)_sourceNode.EmitStatement(EmitMode.ForCopy)), Compiler.BuildRowEqualExpression(plan, sourceRowType.Columns, rowType.Columns));
							else
							{
								PlanNode currentNode = sourceNode;
								while (currentNode != null)
								{
									if (currentNode.NodeCount >= 1)
									{
										if (currentNode.Nodes[0] == _sourceNode)
										{
											currentNode.Nodes[0] = Compiler.EmitRestrictNode(plan, Compiler.CompileExpression(plan, (Expression)_sourceNode.EmitStatement(EmitMode.ForCopy)), Compiler.BuildRowEqualExpression(plan, sourceRowType.Columns, rowType.Columns));
											break;
										}
										currentNode = currentNode.Nodes[0];
									}
									else
										Error.Fail("Internal Error: Original source node not found in aggregate invocation argument.");
								}
							}
							
							if (expression.HasByClause)
								sourceNode = Compiler.EmitOrderNode(plan, (TableNode)sourceNode, Compiler.CompileOrderColumnDefinitions(plan, ((TableNode)sourceNode).TableVar, expression.OrderColumns, null, false), false);
							aggregateNode.Nodes[0] = sourceNode;
						}
						finally
						{
							for (int index = 0; index < stackDisplacement; index++)
								plan.Symbols.Pop();
						}
						
						newColumn = 
							new Schema.TableVarColumn
							(
								new Schema.Column
								(
									expression.ColumnAlias, 
									aggregateNode.DataType
								),
								expression.MetaData, 
								Schema.TableVarColumnType.Virtual
							);

						DataType.Columns.Add(newColumn.Column);
						TableVar.Columns.Add(newColumn);
						newColumn.IsNilable = aggregateNode.IsNilable;
						Nodes.Add(aggregateNode);
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
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			
			// Add the computed columns
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, SourceTableType.CreateRowType(Keywords.Source)));
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
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = SourceNode.CursorType;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable |
				(SourceNode.CursorCapabilities & (CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Countable)) |
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
			AggregateExpression expression = new AggregateExpression();
			expression.Expression = (Expression)_sourceNode.EmitStatement(mode);
			expression.ByColumns.AddRange(_columns);
			expression.ComputeColumns.AddRange(_computeColumns);
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		public override object InternalExecute(Program program)
		{
			AggregateTable table = new AggregateTable(this, program);
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
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalDefault(program, oldRow, newRow, valueFlags, columnName, isDescending);
			return false;
		}
		
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalChange(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if ((columnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(columnName))
				return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
			return false;
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
	}
}