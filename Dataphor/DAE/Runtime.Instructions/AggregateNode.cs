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
    // operator iAggregate(table{}) : table{}
	public class AggregateNode : UnaryTableNode
	{
		private ColumnExpressions FColumns;
		public ColumnExpressions Columns
		{
			get { return FColumns; }
			set { FColumns = value; }
		}
		
		private AggregateColumnExpressions FComputeColumns;
		public AggregateColumnExpressions ComputeColumns
		{
			get { return FComputeColumns; }
			set { FComputeColumns = value; }
		}
		
		protected int FAggregateColumnOffset;		
		public int AggregateColumnOffset
		{
			get { return FAggregateColumnOffset; }
			set { FAggregateColumnOffset = value; }
		}
		
		// The original source node
		protected PlanNode FSourceNode;
		public PlanNode OriginalSourceNode { get { return FSourceNode; } }
		
		// AggregateNode
		//		Nodes[0] = Project over {by Columns}
		//			Nodes[0] = ASourceNode
		//		Nodes[1..AggregateExpression.Count] = PlanNode - class determined by lookup from the server catalog
		//			Nodes[0] = Project over {aggregate columns for this expression}
		//				Nodes[0] = Restrict
		//					Nodes[0] = ASourceNode
		//					Nodes[1] = Condition over the first key in the project of the aggregate source (AggregateNode.Nodes[0]);
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);

			FSourceNode = SourceNode;
			
			// TODO: Aggregation source is required to be deterministic because it is long handed, we should do something that allows non-deterministic sources for aggregation
			if (!FSourceNode.IsRepeatable)
				throw new CompilerException(CompilerException.Codes.InvalidAggregationSource, APlan.CurrentStatement());

			if (FColumns.Count > 0)
			{
				ProjectNode LProjectNode = (ProjectNode)Compiler.EmitProjectNode(APlan, SourceNode, FColumns, true);
				Nodes[0] = LProjectNode;
			}
			else
			{
				Schema.TableType LTableType = new Schema.TableType();
				TableSelectorNode LNode = new TableSelectorNode(LTableType);
				LNode.TableVar.Keys.Add(new Schema.Key());
				LNode.Nodes.Add(new RowSelectorNode(new Schema.RowType()));
				LNode.DetermineCharacteristics(APlan);
				Nodes[0] = LNode;
			}
			
			CopyTableVarColumns(SourceTableVar.Columns);
			CopyKeys(SourceTableVar.Keys);
			CopyOrders(SourceTableVar.Orders);
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif
			
			FAggregateColumnOffset = TableVar.Columns.Count;

			Schema.Key LCompareKey = Compiler.FindClusteringKey(APlan, TableVar);
			
			// Add the computed columns
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Source)));
				try
				{
					Schema.RowType LRowType = new Schema.RowType(LCompareKey.Columns);
					Schema.RowType LSourceRowType = new Schema.RowType(LCompareKey.Columns, Keywords.Source);
					Schema.TableVarColumn LNewColumn;
					foreach (AggregateColumnExpression LExpression in FComputeColumns)
					{
						PlanNode LSourceNode = null;
						string[] LColumnNames = new string[LExpression.Columns.Count];
						for (int LIndex = 0; LIndex < LExpression.Columns.Count; LIndex++)
							LColumnNames[LIndex] = LExpression.Columns[LIndex].ColumnName;

						if (LExpression.Distinct)
							LSourceNode = Compiler.EmitProjectNode(APlan, FSourceNode, LColumnNames, true);
						else
							LSourceNode = FSourceNode;
							
						for (int LIndex = 0; LIndex < LColumnNames.Length; LIndex++)
							if (((TableNode)LSourceNode).TableVar.Columns.IndexOf(LColumnNames[LIndex]) < 0)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, LColumnNames[LIndex]);
							
						OperatorBindingContext LContext = new OperatorBindingContext(LExpression, LExpression.AggregateOperator, APlan.NameResolutionPath, Compiler.AggregateSignatureFromArguments(LSourceNode, LColumnNames, true), false);
						PlanNode LAggregateNode = Compiler.EmitAggregateCallNode(APlan, LContext, LSourceNode, LColumnNames, LExpression.HasByClause ? LExpression.OrderColumns : null);
						Compiler.CheckOperatorResolution(APlan, LContext);
						LSourceNode = LAggregateNode.Nodes[0]; // Make sure to preserve any conversion and casting done by the operator resolution
						
						int LStackDisplacement = ((AggregateCallNode)LAggregateNode).Operator.Initialization.StackDisplacement + 1; // add 1 to account for the result variable
						LStackDisplacement += LColumnNames.Length;
						for (int LIndex = 0; LIndex < LStackDisplacement; LIndex++)
							APlan.Symbols.Push(new Symbol(String.Empty, APlan.DataTypes.SystemScalar));
						try
						{
							// Walk LSourceNode (assuming an n-length list of unary table operators) until FSourceNode is found
							// Insert a restriction between it and a recompile of FSourceNode (to account for possible context changes)
							
							if (LSourceNode == FSourceNode)
								LSourceNode = Compiler.EmitRestrictNode(APlan, Compiler.CompileExpression(APlan, (Expression)FSourceNode.EmitStatement(EmitMode.ForCopy)), Compiler.BuildRowEqualExpression(APlan, LSourceRowType.Columns, LRowType.Columns));
							else
							{
								PlanNode LCurrentNode = LSourceNode;
								while (LCurrentNode != null)
								{
									if (LCurrentNode.NodeCount >= 1)
									{
										if (LCurrentNode.Nodes[0] == FSourceNode)
										{
											LCurrentNode.Nodes[0] = Compiler.EmitRestrictNode(APlan, Compiler.CompileExpression(APlan, (Expression)FSourceNode.EmitStatement(EmitMode.ForCopy)), Compiler.BuildRowEqualExpression(APlan, LSourceRowType.Columns, LRowType.Columns));
											break;
										}
										LCurrentNode = LCurrentNode.Nodes[0];
									}
									else
										Error.Fail("Internal Error: Original source node not found in aggregate invocation argument.");
								}
							}
							
							if (LExpression.HasByClause)
								LSourceNode = Compiler.EmitOrderNode(APlan, (TableNode)LSourceNode, Compiler.CompileOrderColumnDefinitions(APlan, ((TableNode)LSourceNode).TableVar, LExpression.OrderColumns, null, false), false);
							LAggregateNode.Nodes[0] = LSourceNode; //Compiler.BindNode(APlan, Compiler.OptimizeNode(APlan, LSourceNode));
						}
						finally
						{
							for (int LIndex = 0; LIndex < LStackDisplacement; LIndex++)
								APlan.Symbols.Pop();
						}
						
						LNewColumn = 
							new Schema.TableVarColumn
							(
								new Schema.Column
								(
									LExpression.ColumnAlias, 
									LAggregateNode.DataType
								),
								LExpression.MetaData, 
								Schema.TableVarColumnType.Virtual
							);

						DataType.Columns.Add(LNewColumn.Column);
						TableVar.Columns.Add(LNewColumn);
						LNewColumn.IsNilable = LAggregateNode.IsNilable;
						Nodes.Add(LAggregateNode);
					}
					
					DetermineRemotable(APlan);
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
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			
			// Add the computed columns
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(String.Empty, SourceTableType.CreateRowType(Keywords.Source)));
				try
				{
					for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)	
						Nodes[LIndex].DetermineBinding(APlan);
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
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = SourceNode.CursorType;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable |
				(SourceNode.CursorCapabilities & (CursorCapability.BackwardsNavigable | CursorCapability.Bookmarkable | CursorCapability.Searchable | CursorCapability.Countable)) |
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
			
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AggregateExpression LExpression = new AggregateExpression();
			LExpression.Expression = (Expression)FSourceNode.EmitStatement(AMode);
			LExpression.ByColumns.AddRange(FColumns);
			LExpression.ComputeColumns.AddRange(FComputeColumns);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AggregateTable LTable = new AggregateTable(this, AProgram);
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
		
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalDefault(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending);
			return false;
		}
		
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalChange(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalValidate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			return false;
		}
		
		public override void JoinApplicationTransaction(Program AProgram, Row ARow)
		{
			// Exclude any columns from AKey which were included by this node
			Schema.RowType LRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				if (SourceNode.DataType.Columns.ContainsName(LColumn.Name))
					LRowType.Columns.Add(LColumn.Copy());
					
			Row LRow = new Row(AProgram.ValueManager, LRowType);
			try
			{
				ARow.CopyTo(LRow);
				SourceNode.JoinApplicationTransaction(AProgram, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
	}
}