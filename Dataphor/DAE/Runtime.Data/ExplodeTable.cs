/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Diagnostics;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;

	/*
		The explode table works by maintaining a stack of cursors, one for the root result set defined by the where clause of the explode operator,
		and one for each child cursor encountered while iterating through the result set.
	*/
    public class ExplodeTable : Table
    {
		public ExplodeTable(ExplodeNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}
		
		public new ExplodeNode Node { get { return (ExplodeNode)FNode; } }
		
		// The current set of cursors being used to produce the result set
		protected Stack FSourceTables;
		
		// For every cursor but the root, this stack will contain the parent row variable for that cursor.
		protected Stack FParentRows;
		
		protected Row FSourceRow;
		protected Row FTargetRow;
		protected Schema.TableType FTableType;
		protected Schema.BaseTableVar FTableVar;
		protected Schema.Order FSequenceOrder;
		protected NativeTable FBuffer;
		protected Scan FScan;
		protected Table FRootTable;
		protected int FSequence;
		protected int FSequenceColumnIndex = -1;
		protected bool FEmpty;
		
		protected Row NewParentRow(Row ACurrentRow)
		{
			Row LRow = new Row(Process, new Schema.RowType(ACurrentRow.DataType.Columns, Keywords.Parent));
			ACurrentRow.CopyTo(LRow);
			return LRow;
		}
		
		protected void PushSourceTable(Row ACurrentRow)
		{
			if (ACurrentRow == null)
			{
				// Open the root expression, if it is not empty, save it on the cursor stack
				FRootTable = (Table)Node.Nodes[1].Execute(Process);
				if (!FRootTable.IsEmpty())
					FSourceTables.Push(FRootTable);
				else
					FRootTable.Dispose();
			}
			else
			{
				// Otherwise, use the given row to build a parent row and open a new cursor for the child expression with that row to parameterize it
				Row LParentRow = NewParentRow(ACurrentRow);
				Process.Stack.Push(LParentRow);
				try
				{
					Table LTable = (Table)Node.Nodes[2].Execute(Process);
					
					// If it is not empty, save it on the cursor stack, and save the parent row on the parent row stack
					if (!LTable.IsEmpty())
					{
						FSourceTables.Push(LTable);
						FParentRows.Push(LParentRow);
					}
					else
					{
						LTable.Dispose();
						LParentRow.Dispose();
					}
				}
				finally
				{
					Process.Stack.Pop();
				}
			}
		}
		
		protected void PopSourceTable()
		{
			Table LTable = (Table)FSourceTables.Pop();
			if (LTable != FRootTable)
				((Row)FParentRows.Pop()).Dispose();
			else
				FRootTable = null;

			LTable.Dispose();
		}
		
		protected override void InternalOpen()
		{
			FSourceTables = new Stack(Process.Stack.MaxStackDepth, Process.Stack.MaxCallDepth);
			FParentRows = new Stack(Process.Stack.MaxStackDepth, Process.Stack.MaxCallDepth);
		 	PushSourceTable(null);
			FSourceRow = new Row(Process, ((TableNode)Node.Nodes[0]).DataType.RowType);
			FTableType = new Schema.TableType();
			FTableVar = new Schema.BaseTableVar(FTableType, Process.Plan.TempDevice);
			Schema.TableVarColumn LNewColumn;
			foreach (Schema.TableVarColumn LColumn in Node.TableVar.Columns)
			{
				LNewColumn = (Schema.TableVarColumn)LColumn.Copy();
				FTableType.Columns.Add(LNewColumn.Column);
				FTableVar.Columns.Add(LNewColumn);
			}

			if (Node.SequenceColumnIndex < 0)
			{
				LNewColumn = new Schema.TableVarColumn(new Schema.Column(Keywords.Sequence, Process.DataTypes.SystemInteger), Schema.TableVarColumnType.Stored);
				FTableType.Columns.Add(LNewColumn.Column);
				FTableVar.Columns.Add(LNewColumn);
				FSequenceColumnIndex = FTableVar.Columns.Count - 1;
			}
			else
				FSequenceColumnIndex = Node.SequenceColumnIndex;

			FTargetRow = new Row(Process, FTableType.RowType);
			Schema.Key LKey = new Schema.Key();
			LKey.Columns.Add(FTableVar.Columns[FSequenceColumnIndex]);
			FTableVar.Keys.Add(LKey);
			FBuffer = new NativeTable(Process, FTableVar);
			FScan = new Scan(Process, FBuffer, FBuffer.ClusteredIndex, ScanDirection.Forward, null, null);
			FScan.Open();
			FSequence = 0;
			FEmpty = false;
			InternalNext();
			FEmpty = FScan.EOF();
			FScan.First();
		}
		
		protected override void InternalClose()
		{
			while (FSourceTables.Count > 0)
				PopSourceTable();

			if (FSourceRow != null)
			{				
				FSourceRow.Dispose();
				FSourceRow = null;
			}
			
			if (FTargetRow != null)
			{
				FTargetRow.Dispose();
				FTargetRow = null;
			}
			
			if (FScan != null)
			{
				FScan.Dispose();
				FScan = null;
			}
			
			if (FBuffer != null)
			{
				FBuffer.Drop(Process);
				FBuffer = null;
			}

			FTableType = null;
			FSequenceColumnIndex = -1;
		}
		
		protected override void InternalReset()
		{
			while (FSourceTables.Count > 0)
				PopSourceTable();
				
			PushSourceTable(null);

			FScan.Dispose();
			FBuffer.Truncate(Process);
			FScan = new Scan(Process, FBuffer, FBuffer.ClusteredIndex, ScanDirection.Forward, null, null);
			FScan.Open();
			FSequence = 0;
			FEmpty = false;
			InternalNext();
			FEmpty = FScan.EOF();
			FScan.First();
		}
		
		protected override bool InternalNext()
		{
			if (!FScan.Next())
			{
				Table LTable;
				while (FSourceTables.Count > 0)
				{
					// Retrieve the current cursor to be iterated
					LTable = (Table)FSourceTables[0];
					bool LContextPopped = false;
					bool LContextPushed = false;
					if (FSourceTables.Count > 1)
					{
						// Push it's parent row context, if necessary
						Process.Stack.Push(FParentRows.Peek(0));
						LContextPushed = true;
					}
					try
					{
						if (LTable.Next())
						{
							FSequence++;
							LTable.Select(FSourceRow);
							
							if (LContextPushed)
							{
								LContextPopped = true;
								Process.Stack.Pop();
							}
							
							FTargetRow.ClearValues();
							for (int LIndex = 0; LIndex < FSourceRow.DataType.Columns.Count; LIndex++)
								FTargetRow[LIndex] = FSourceRow[LIndex];
							if (Node.LevelColumnIndex >= 0)
								FTargetRow[Node.LevelColumnIndex] = FSourceTables.Count;
							if (FSequenceColumnIndex >= 0)
								FTargetRow[FSequenceColumnIndex] = FSequence;
								
							FBuffer.Insert(Process, FTargetRow);
							if (!FScan.FindKey(FTargetRow))
								throw new RuntimeException(RuntimeException.Codes.NewRowNotFound);
							
							// Use the current row to push a new cursor looking for children of this row
							PushSourceTable(FSourceRow);
							return true;
						}
						else
							// The current cursor has been exhausted, pop it.
							PopSourceTable();
					}
					finally
					{
						if (LContextPushed && !LContextPopped)
							Process.Stack.Pop();
					}
				}
				return false;
			}
			return true;
		}
		
		protected override void InternalSelect(Row ARow)
		{
			FScan.GetRow(ARow);
		}
		
		protected override bool InternalBOF()
		{
			return FScan.BOF() || FEmpty;
		}
		
		protected override bool InternalEOF()
		{
			return (FScan.EOF() && (FSourceTables.Count == 0)) || FEmpty;
		}
    }
}