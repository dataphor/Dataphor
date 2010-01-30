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
	// operator iExplode(table{}, bool, bool, object, object) : table{}    
    public class ExplodeNode : UnaryTableNode
    {
		protected IncludeColumnExpression FLevelColumn;
		public IncludeColumnExpression LevelColumn
		{
			get { return FLevelColumn; }
			set { FLevelColumn = value; }
		}
		
		protected int FLevelColumnIndex = -1;
		public int LevelColumnIndex
		{
			get { return FLevelColumnIndex; }
		}
		
		protected IncludeColumnExpression FSequenceColumn;
		public IncludeColumnExpression SequenceColumn
		{
			get { return FSequenceColumn; }
			set { FSequenceColumn = value; }
		}
		
		protected int FSequenceColumnIndex = -1;
		public int SequenceColumnIndex
		{
			get { return FSequenceColumnIndex; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			
			if (FLevelColumn != null)
			{
				Schema.TableVarColumn LLevelColumn =
					Compiler.CompileIncludeColumnExpression
					(
						APlan,
						FLevelColumn,
						Keywords.Level,
						APlan.DataTypes.SystemInteger,
						Schema.TableVarColumnType.Level
					);
				DataType.Columns.Add(LLevelColumn.Column);
				TableVar.Columns.Add(LLevelColumn);
				FLevelColumnIndex = TableVar.Columns.Count - 1;
			}
				
			if (FSequenceColumn != null)
			{
				Schema.TableVarColumn LSequenceColumn =
					Compiler.CompileIncludeColumnExpression
					(
						APlan,
						FSequenceColumn,
						Keywords.Sequence,
						APlan.DataTypes.SystemInteger,
						Schema.TableVarColumnType.Sequence
					);
				DataType.Columns.Add(LSequenceColumn.Column);
				TableVar.Columns.Add(LSequenceColumn);
				FSequenceColumnIndex = DataType.Columns.Count - 1;
			}
			else
			{
				Schema.TableVarColumn LSequenceColumn =
					new Schema.TableVarColumn
					(
						new Schema.Column(Keywords.Sequence, APlan.DataTypes.SystemInteger),
						Schema.TableVarColumnType.Sequence
					);
				DataType.Columns.Add(LSequenceColumn.Column);
				TableVar.Columns.Add(LSequenceColumn);
				FSequenceColumnIndex = DataType.Columns.Count - 1;
			}
			
			DetermineRemotable(APlan);

			//CopyKeys(SourceTableVar.Keys);
			if (FSequenceColumnIndex >= 0)
			{
				Schema.Key LSequenceKey = new Schema.Key();
				LSequenceKey.IsInherited = true;
				LSequenceKey.Columns.Add(TableVar.Columns[FSequenceColumnIndex]);
				TableVar.Keys.Add(LSequenceKey);
			}
			CopyOrders(SourceTableVar.Orders);
			Order = Compiler.OrderFromKey(APlan, Compiler.FindClusteringKey(APlan, TableVar));

			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			Nodes[1].DetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(String.Empty, SourceTableType.CreateRowType(Keywords.Parent)));
				try
				{
					Nodes[2].DetermineBinding(APlan);
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
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;

			Order = Compiler.OrderFromKey(APlan, Compiler.FindClusteringKey(APlan, TableVar));
		}
		
		public override object InternalExecute(Program AProgram)
		{
			ExplodeTable LTable = new ExplodeTable(this, AProgram);
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
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			ExplodeExpression LExpression = new ExplodeExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			PlanNode LRootNode = Nodes[1];
			OrderNode LOrderNode = LRootNode as OrderNode;
			if (LOrderNode != null)
			{
				LExpression.HasOrderByClause = true;
				for (int LIndex = 0; LIndex < LOrderNode.RequestedOrder.Columns.Count; LIndex++)
					LExpression.OrderColumns.Add(LOrderNode.RequestedOrder.Columns[LIndex].EmitStatement(AMode));
				LRootNode = LRootNode.Nodes[0];
			}
			
			LExpression.RootExpression = ((RestrictExpression)LRootNode.EmitStatement(AMode)).Condition;
			
			PlanNode LByNode = Nodes[2];
			if (LByNode is OrderNode)
				LByNode = LByNode.Nodes[0];

			LExpression.ByExpression = ((RestrictExpression)LByNode.EmitStatement(AMode)).Condition;

			if (FLevelColumnIndex >= 0)
			{
				Schema.TableVarColumn LLevelColumn = TableVar.Columns[FLevelColumnIndex];
				LExpression.LevelColumn = new IncludeColumnExpression(LLevelColumn.Name, LLevelColumn.MetaData == null ? null : LLevelColumn.MetaData.Copy());
			}
			
			if (FSequenceColumnIndex >= 0)
			{
				Schema.TableVarColumn LSequenceColumn = TableVar.Columns[FSequenceColumnIndex];
				LExpression.SequenceColumn = new IncludeColumnExpression(LSequenceColumn.Name, LSequenceColumn.MetaData == null ? null : LSequenceColumn.MetaData.Copy());
			}
			LExpression.Modifiers = Modifiers;
			return LExpression;
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