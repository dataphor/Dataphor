/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define USENAMEDROWVARIABLES
	
using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

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
    public abstract class ProjectNodeBase : UnaryTableNode
    {
		// DistinctRequired
		protected bool FDistinctRequired;
		public bool DistinctRequired
		{
			get { return FDistinctRequired; }
			set { FDistinctRequired = value; }
		}
		
		// EqualNode
		protected PlanNode FEqualNode;
		public PlanNode EqualNode
		{
			get { return FEqualNode; }
			set { FEqualNode = value; }
		}
		
		// ColumnNames
		#if USETYPEDLIST
		protected TypedList FColumnNames = new TypedList(typeof(string), false);
		public TypedList ColumnNames { get { return FColumnNames; } }
		#else
		protected BaseList<string> FColumnNames = new BaseList<string>();
		public BaseList<string> ColumnNames { get { return FColumnNames; } }
		#endif
		
		protected abstract void DetermineColumns(Plan APlan);
		
		// DetermineDataType
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			
			DetermineColumns(APlan);
			DetermineRemotable(APlan);

			// copy all non-sparse keys with all fields preserved in the projection
			CopyPreservedKeys(SourceTableVar.Keys, false, false);

			// if at least one non-sparse key is preserved, then we are free to copy preserved sparse keys as well			
			if (TableVar.Keys.Count > 0)
				CopyPreservedKeys(SourceTableVar.Keys, false, true);
			
			CopyPreservedOrders(SourceTableVar.Orders);

			FDistinctRequired = (TableVar.Keys.Count == 0);
			if (FDistinctRequired)
			{
				Schema.Key LNewKey = new Schema.Key();
				LNewKey.IsInherited = true;
				foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
				    LNewKey.Columns.Add(LColumn);
				TableVar.Keys.Add(LNewKey);
				if (LNewKey.Columns.Count > 0)
					Nodes[0] = Compiler.EmitOrderNode(APlan, SourceNode, LNewKey, true);
			
				APlan.EnterRowContext();
				try
				{	
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Left, DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						APlan.Symbols.Push(new Symbol(Keywords.Right, DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Right)));
						#endif
						try
						{
							FEqualNode = 
								Compiler.CompileExpression
								(
									APlan, 
									#if USENAMEDROWVARIABLES
									Compiler.BuildRowEqualExpression
									(
										APlan, 
										Keywords.Left,
										Keywords.Right,
										LNewKey.Columns,
										LNewKey.Columns
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
							APlan.Symbols.Pop();
						}
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
				
				Order = Compiler.OrderFromKey(APlan, Compiler.FindClusteringKey(APlan, TableVar));
			}
			else
			{
				if ((SourceNode.Order != null) && SourceNode.Order.Columns.IsSubsetOf(TableVar.Columns))
					Order = CopyOrder(SourceNode.Order);
			}
			
			if ((Order != null) && !TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);

			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			base.InternalDetermineBinding(APlan);
			if (FDistinctRequired)
			{
				APlan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Left, DataType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						APlan.Symbols.Push(new Symbol(Keywords.Right, DataType.RowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, DataType.CreateRowType(Keywords.Right)));
						#endif
						try
						{
							FEqualNode.DetermineBinding(APlan);
						}
						finally
						{
							APlan.Symbols.Pop();
						}
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

			// If this node has no determined order, it is because the projection did not affect cardinality, and we may therefore assume the order of the source, as long as it's columns have been preserved by the projection
			if ((SourceNode.Order != null) && SourceNode.Order.Columns.IsSubsetOf(TableVar.Columns))
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		// Execute
		public override object InternalExecute(Program AProgram)
		{
			ProjectTable LTable = new ProjectTable(this, AProgram);
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
		
		// Change
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (PropagateChange)
			{
				// select the current row from the base node using the given row
				Row LSourceRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType);
				try
				{
					ANewRow.CopyTo(LSourceRow);
					Row LCurrentRow = null;
					if (!AProgram.ServerProcess.ServerSession.Server.IsEngine)
						LCurrentRow = SourceNode.Select(AProgram, LSourceRow);
					try
					{
						if (LCurrentRow == null)
							LCurrentRow = new Row(AProgram.ValueManager, SourceNode.DataType.RowType);
						ANewRow.CopyTo(LCurrentRow);
						BitArray LValueFlags = AValueFlags != null ? new BitArray(LCurrentRow.DataType.Columns.Count) : null;
						if (LValueFlags != null)
							for (int LIndex = 0; LIndex < LValueFlags.Length; LIndex++)
							{
								int LRowIndex = ANewRow.DataType.Columns.IndexOfName(LCurrentRow.DataType.Columns[LIndex].Name);
								LValueFlags[LIndex] = LRowIndex >= 0 ? AValueFlags[LRowIndex] : false;
							}
						bool LChanged = SourceNode.Change(AProgram, AOldRow, LCurrentRow, LValueFlags, AColumnName);
						if (LChanged)
							LCurrentRow.CopyTo(ANewRow);
						return LChanged;
					}
					finally
					{
						LCurrentRow.Dispose();
					}
				}
				finally
				{
					LSourceRow.Dispose();
				}
			}
			return false;
		}

		public override void JoinApplicationTransaction(Program AProgram, Row ARow) 
		{
			Schema.RowType LRowType = new Schema.RowType();
			
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				LRowType.Columns.Add(LColumn.Copy());
			foreach (Schema.Column LColumn in SourceNode.DataType.Columns)
				if (!DataType.Columns.ContainsName(LColumn.Name))	
					LRowType.Columns.Add(LColumn.Copy());
			Row LRow = new Row(AProgram.ValueManager, LRowType);
			try
			{
				ARow.CopyTo(LRow);
				base.JoinApplicationTransaction(AProgram, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
    }

	// operator iProject(table{}, list{string}) : table{}
    public class ProjectNode : ProjectNodeBase
    {
		protected override void DetermineColumns(Plan APlan)
		{
			// Determine project columns
			Schema.TableVarColumn LTableVarColumn;
			foreach (string LColumnName in FColumnNames)
			{
				LTableVarColumn = SourceTableVar.Columns[LColumnName].Inherit();
				TableVar.Columns.Add(LTableVarColumn);
				DataType.Columns.Add(LTableVarColumn.Column);
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			ProjectExpression LExpression = new ProjectExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
				LExpression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(LColumn.Name)));
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
    }
    
    public class RemoveNode : ProjectNodeBase
    {
		protected override void DetermineColumns(Plan APlan)
		{
			// this loop is done this way to ensure that the columns are looked up
			// using the name resolution algorithms found in the Columns object,
			// and to ensure that all the columns named in the remove list are valid
			bool LFound;
			int LColumnIndex;
			Schema.TableVarColumn LColumn;
			for (int LIndex = 0; LIndex < SourceTableVar.Columns.Count; LIndex++)
			{
				LFound = false;
				foreach (string LColumnName in FColumnNames)
				{
					LColumnIndex = SourceTableVar.Columns.IndexOf(LColumnName);
					if (LColumnIndex < 0)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, LColumnName);
					else if (LColumnIndex == LIndex)
					{
						LFound = true;
						break;
					}
				}
			
				if (!LFound)
				{
					LColumn = SourceTableVar.Columns[LIndex].Inherit();
					DataType.Columns.Add(LColumn.Column);
					TableVar.Columns.Add(LColumn);
				}
			}
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			RemoveExpression LExpression = new RemoveExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			foreach (Schema.TableVarColumn LColumn in SourceTableVar.Columns)
				if (!TableVar.Columns.ContainsName(LColumn.Name))
					LExpression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(LColumn.Name)));
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
    }
}