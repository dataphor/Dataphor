/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Threading;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Compiling.Visitors;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
    /*
		Cursor Level Table Operators ->
			Cursor(table): Cursor
			Close(Cursor)
			First(Cursor)
			Last(Cursor)
			Next(Cursor) : bool
			Prior(Cursor) : bool
			BOF(Cursor) : bool
			EOF(Cursor) : bool
			Select(Cursor) : row
			Fetch(Cursor) : table
			Fetch(Cursor, int) : table
			Insert(Cursor, row);
			Update(Cursor, row);
			Delete(Cursor);
			Default(cursor, var row)
			Default(cursor, var row, string)
			Change(cursor, var row)
			Change(cursor, var row, string)
			Validate(cursor, var row)
			Validate(cursor, var row, string)
			Truncate(Cursor);
			IsEmpty(Cursor) : bool;
			Reset(Cursor);
			Refresh(Cursor, row);
			FindKey(Cursor, row) : boolean;
			FindNearest(Cursor, row);
			GetKey(Cursor) : row;
			GetBookmark(Cursor) : Guid;
			GotoBookmark(Cursor, Guid) : boolean;
			CompareBookmarks(Cursor, Guid, Guid);
    */

	// operator Cursor(ATable : table) : cursor;
	public class CursorNode : PlanNode
	{
		public CursorNode() : base()
		{
			IgnoreUnsupported = true;
		}
		
		// CursorType
		public Schema.CursorType CursorType { get { return (Schema.CursorType)_dataType; } }
		
		// CursorContext
		private CursorContext _cursorContext;
		public CursorContext CursorContext
		{
			get { return _cursorContext; }
			set { _cursorContext = value; }
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newCursorNode = (CursorNode)newNode;

			newCursorNode.CursorContext = _cursorContext;
		}
		
		// SourceNode
		public TableNode SourceNode
		{
			get { return (TableNode)Nodes[0]; }
			set { Nodes[0] = value; }
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.CursorType(SourceNode.DataType);
		}
		
		private TableNode BindSourceNode(Plan plan, TableNode sourceNode)
		{
			plan.PushCursorContext(_cursorContext);
			try
			{
				plan.PushGlobalContext();
				try
				{
					// if the requested cursor type is static, ensure that is the case
					if ((plan.CursorContext.CursorType == DAE.CursorType.Static) && (sourceNode.CursorType != DAE.CursorType.Static))
					{
						sourceNode = (TableNode)Compiler.EmitCopyNode(plan, sourceNode);
						sourceNode.InferPopulateNode(plan);
						sourceNode.DeterminePotentialDevice(plan);
						sourceNode.DetermineDevice(plan);
						sourceNode.DetermineAccessPath(plan);
					}
							
					// Navigable
					if ((plan.CursorContext.CursorCapabilities & CursorCapability.Navigable) != 0)
						sourceNode.CheckCapability(CursorCapability.Navigable);
							
					// If the cursor is requested countable, it must be satisfied by a copy node
					if ((plan.CursorContext.CursorCapabilities & CursorCapability.Countable) != 0)
					{
						if (!sourceNode.Supports(CursorCapability.Countable))
						{
							sourceNode = (TableNode)Compiler.EmitCopyNode(plan, sourceNode);
							sourceNode.InferPopulateNode(plan);
							sourceNode.DeterminePotentialDevice(plan);
							sourceNode.DetermineDevice(plan);
							sourceNode.DetermineAccessPath(plan);
						}
					}

					// BackwardsNavigable
					// Bookmarkable
					// Searchable
					if
						(
							(((plan.CursorContext.CursorCapabilities & CursorCapability.BackwardsNavigable) != 0) && !sourceNode.Supports(CursorCapability.BackwardsNavigable)) ||
							(((plan.CursorContext.CursorCapabilities & CursorCapability.Bookmarkable) != 0) && !sourceNode.Supports(CursorCapability.Bookmarkable)) ||
							(((plan.CursorContext.CursorCapabilities & CursorCapability.Searchable) != 0) && !sourceNode.Supports(CursorCapability.Searchable))
						)
					{
						sourceNode = (TableNode)Compiler.EmitBrowseNode(plan, sourceNode, true);
						sourceNode.InferPopulateNode(plan);
						sourceNode.DeterminePotentialDevice(plan);
						sourceNode.DetermineDevice(plan);
						sourceNode.DetermineAccessPath(plan);
					}

					// Updateable
					if ((plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) != 0)
						sourceNode.CheckCapability(CursorCapability.Updateable);

					// Truncateable
					if ((plan.CursorContext.CursorCapabilities & CursorCapability.Truncateable) != 0)
						sourceNode.CheckCapability(CursorCapability.Truncateable);
				}
				finally
				{
					plan.PopGlobalContext();
				}
			}
			finally
			{
				plan.PopCursorContext();
			}
			return sourceNode;	
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.PushCursorContext(_cursorContext);
			try
			{
				#if USEVISIT
				Nodes[0] = visitor.Visit(plan, Nodes[0]);
				#else
				SourceNode.BindingTraversal(plan, visitor);
				#endif
			}
			finally
			{
				plan.PopCursorContext();
			}
		}

		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			SourceNode = BindSourceNode(plan, SourceNode);
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.PushCursorContext(_cursorContext);
			try
			{
				base.BindToProcess(plan);
			}
			finally
			{
				plan.PopCursorContext();
			}
		}
		
		public override object InternalExecute(Program program)
		{
			return new CursorValue(program.ValueManager, CursorType, program.CursorManager.CreateCursor((ITable)Nodes[0].Execute(program)));
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			TableNode node = (TableNode)Nodes[0];
			return new CursorSelectorExpression(new CursorDefinition((Expression)node.EmitStatement(mode), node.CursorCapabilities, node.CursorIsolation, node.CursorType));
		}
	}
	
	// operator Close(ACursor : cursor);
	public class CursorCloseNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.CursorManager.CloseCursor(((CursorValue)arguments[0]).ID);
			return null;
		}
	}

    // operator First(ACursor : cursor);
    public class CursorFirstNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.First();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
			return null;
		}
	}
    
	// operator Last(ACursor : cursor);
	public class CursorLastNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.Last();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
			return null;
		}
	}
    
	// operator Next(Cursor) : boolean;
	public class CursorNextNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.Next();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
    
	// operator Prior(Cursor) : boolean;
	public class CursorPriorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.Prior();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator BOF(Cursor) : boolean;
	public class CursorBOFNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.BOF();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator EOF(Cursor) : boolean;
	public class CursorEOFNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.EOF();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator Select(Cursor) : row
	public class CursorSelectNode : InstructionNode
	{
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			_dataType = ((Schema.CursorType)Nodes[0].DataType).TableType.RowType;
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.Select();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator Fetch(Cursor) : table{}
	// operator Fetch(Cursor, int) : table{}

	// operator Insert(Cursor, row{})
	public class CursorInsertNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.CheckCapability(CursorCapability.Updateable);
				cursor.Table.Insert((IRow)arguments[1]);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
			return null;
		}
	}

	// operator Update(Cursor, row{})
	public class CursorUpdateNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.CheckCapability(CursorCapability.Updateable);
				cursor.Table.Update((IRow)arguments[1]);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
			return null;
		}
	}
	
	// operator Delete(Cursor)
	public class CursorDeleteNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.CheckCapability(CursorCapability.Updateable);
				cursor.Table.Delete();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
			return null;
		}
	}
	
	// operator Default(const ACursor : cursor, var ARow : row) : Boolean;
	// operator Default(const ACursor : cursor, var ARow : row, const AColumnName : String) : Boolean;
	public class CursorDefaultNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				ITable table = cursor.Table;
				IRow row = (IRow)arguments[1];
				string columnName = arguments.Length == 3 ? (string)arguments[2] : String.Empty;
				return table.Node.Default(program, null, row, null, columnName);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator Change(const ACursor : cursor, const AOldRow : row, var ANewRow : row) : Boolean;
	// operator Change(const ACursor : cursor, const AOldRow : row, var ANewRow : row, const AColumnName : String) : Boolean
	public class CursorChangeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				ITable table = cursor.Table;
				IRow oldRow = (IRow)arguments[1];
				IRow newRow = (IRow)arguments[2];
				string columnName = arguments.Length == 4 ? (string)arguments[3] : String.Empty;
				return table.Node.Change(program, oldRow, newRow, null, columnName);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}

	// operator Validate(const ACursor : cursor, const AOldRow : row, var ANewRow : row) : Boolean;
	// operator Validate(const ACursor : cursor, const AOldRow : row, var ANewRow : row, const AColumnName : String) : Boolean;	
	public class CursorValidateNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				ITable table = cursor.Table;
				IRow oldRow = (IRow)arguments[1];
				IRow newRow = (IRow)arguments[2];
				string columnName = arguments.Length == 4 ? (string)arguments[3] : String.Empty;
				return table.Node.Validate(program, oldRow, newRow, null, columnName);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator GetKey(cursor) : row;
	public class CursorGetKeyNode : InstructionNode
	{
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			//FDataType = ((Schema.CursorType)Nodes[0].DataType).TableType.RowType;
			// TODO: This is wrong, the capabilities and order of a cursor should be part of the cursor type specifier
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.GetKey();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator FindKey(cursor, row) : boolean
	public class CursorFindKeyNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.FindKey((IRow)arguments[1]);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator FindNearest(cursor, row);
	public class CursorFindNearestNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.FindNearest((IRow)arguments[1]);
				return null;
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator Refresh(cursor)
	// operator Refresh(cursor, row);
	public class CursorRefreshNode: InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				if (arguments.Length == 1)
					cursor.Table.Refresh(null);
				else
					cursor.Table.Refresh((IRow)arguments[1]);
				return null;
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator Reset(cursor);
	public class CursorResetNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.Reset();
				return null;
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator GetBookmark(cursor) : row;
	public class CursorGetBookmarkNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.GetBookmark();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator GotoBookmark(cursor, row) : boolean;
	public class CursorGotoBookmarkNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.GotoBookmark((IRow)arguments[1]);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
	
	// operator CompareBookmarks(cursor, row, row) : integer;
	public class CursorCompareBookmarksNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Cursor cursor = program.CursorManager.GetCursor(((CursorValue)arguments[0]).ID);
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.CompareBookmarks((IRow)arguments[1], (IRow)arguments[1]);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
	}
}

