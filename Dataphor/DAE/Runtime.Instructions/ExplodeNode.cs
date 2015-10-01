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
	// operator iExplode(table{}, bool, bool, object, object) : table{}    
    public class ExplodeNode : UnaryTableNode
    {
		protected IncludeColumnExpression _levelColumn;
		public IncludeColumnExpression LevelColumn
		{
			get { return _levelColumn; }
			set { _levelColumn = value; }
		}
		
		protected int _levelColumnIndex = -1;
		public int LevelColumnIndex
		{
			get { return _levelColumnIndex; }
		}
		
		protected IncludeColumnExpression _sequenceColumn;
		public IncludeColumnExpression SequenceColumn
		{
			get { return _sequenceColumn; }
			set { _sequenceColumn = value; }
		}
		
		protected int _sequenceColumnIndex = -1;
		public int SequenceColumnIndex
		{
			get { return _sequenceColumnIndex; }
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			
			if (_levelColumn != null)
			{
				Schema.TableVarColumn levelColumn =
					Compiler.CompileIncludeColumnExpression
					(
						plan,
						_levelColumn,
						Keywords.Level,
						plan.DataTypes.SystemInteger,
						Schema.TableVarColumnType.Level
					);
				DataType.Columns.Add(levelColumn.Column);
				TableVar.Columns.Add(levelColumn);
				_levelColumnIndex = TableVar.Columns.Count - 1;
			}
				
			if (_sequenceColumn != null)
			{
				Schema.TableVarColumn sequenceColumn =
					Compiler.CompileIncludeColumnExpression
					(
						plan,
						_sequenceColumn,
						Keywords.Sequence,
						plan.DataTypes.SystemInteger,
						Schema.TableVarColumnType.Sequence
					);
				DataType.Columns.Add(sequenceColumn.Column);
				TableVar.Columns.Add(sequenceColumn);
				_sequenceColumnIndex = DataType.Columns.Count - 1;
			}
			else
			{
				Schema.TableVarColumn sequenceColumn =
					new Schema.TableVarColumn
					(
						new Schema.Column(Keywords.Sequence, plan.DataTypes.SystemInteger),
						Schema.TableVarColumnType.Sequence
					);
				DataType.Columns.Add(sequenceColumn.Column);
				TableVar.Columns.Add(sequenceColumn);
				_sequenceColumnIndex = DataType.Columns.Count - 1;
			}
			
			DetermineRemotable(plan);

			//CopyKeys(SourceTableVar.Keys);
			if (_sequenceColumnIndex >= 0)
			{
				Schema.Key sequenceKey = new Schema.Key();
				sequenceKey.IsInherited = true;
				sequenceKey.Columns.Add(TableVar.Columns[_sequenceColumnIndex]);
				TableVar.Keys.Add(sequenceKey);
			}
			CopyOrders(SourceTableVar.Orders);
			Order = Compiler.OrderFromKey(plan, Compiler.FindClusteringKey(plan, TableVar));

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			Nodes[1] = visitor.Visit(plan, Nodes[1]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			Nodes[1].BindingTraversal(plan, visitor);
			#endif

			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Parent, SourceTableType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, SourceTableType.CreateRowType(Keywords.Parent)));
				#endif
				try
				{
					#if USEVISIT
					Nodes[2] = visitor.Visit(plan, Nodes[2]);
					#else
					Nodes[2].BindingTraversal(plan, visitor);
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
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;

			Order = Compiler.OrderFromKey(plan, Compiler.FindClusteringKey(plan, TableVar));
		}
		
		public override object InternalExecute(Program program)
		{
			ExplodeTable table = new ExplodeTable(this, program);
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
		
		public override Statement EmitStatement(EmitMode mode)
		{
			ExplodeExpression expression = new ExplodeExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			PlanNode rootNode = Nodes[1];
			OrderNode orderNode = rootNode as OrderNode;
			if (orderNode != null)
			{
				expression.HasOrderByClause = true;
				for (int index = 0; index < orderNode.RequestedOrder.Columns.Count; index++)
					expression.OrderColumns.Add(orderNode.RequestedOrder.Columns[index].EmitStatement(mode));
				rootNode = rootNode.Nodes[0];
			}
			
			expression.RootExpression = ((RestrictExpression)rootNode.EmitStatement(mode)).Condition;
			
			PlanNode byNode = Nodes[2];
			if (byNode is OrderNode)
				byNode = byNode.Nodes[0];

			expression.ByExpression = ((RestrictExpression)byNode.EmitStatement(mode)).Condition;

			if (_levelColumnIndex >= 0)
			{
				Schema.TableVarColumn levelColumn = TableVar.Columns[_levelColumnIndex];
				expression.LevelColumn = new IncludeColumnExpression(levelColumn.Name, levelColumn.MetaData == null ? null : levelColumn.MetaData.Copy());
			}
			
			if (_sequenceColumnIndex >= 0)
			{
				Schema.TableVarColumn sequenceColumn = TableVar.Columns[_sequenceColumnIndex];
				expression.SequenceColumn = new IncludeColumnExpression(sequenceColumn.Name, sequenceColumn.MetaData == null ? null : sequenceColumn.MetaData.Copy());
			}
			expression.Modifiers = Modifiers;
			return expression;
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