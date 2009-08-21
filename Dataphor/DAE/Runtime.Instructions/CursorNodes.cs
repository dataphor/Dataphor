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
		public Schema.CursorType CursorType { get { return (Schema.CursorType)FDataType; } }
		
		// CursorContext
		private CursorContext FCursorContext;
		public CursorContext CursorContext
		{
			get { return FCursorContext; }
			set { FCursorContext = value; }
		}
		
		// SourceNode
		public TableNode SourceNode
		{
			get { return (TableNode)Nodes[0]; }
			set { Nodes[0] = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.CursorType(SourceNode.DataType);
		}
		
		private TableNode BindSourceNode(Plan APlan, TableNode ASourceNode)
		{
			APlan.PushCursorContext(FCursorContext);
			try
			{
				ASourceNode.DetermineBinding(APlan);
				
				ApplicationTransaction LTransaction = null;
				if (APlan.ServerProcess.ApplicationTransactionID != Guid.Empty)
					LTransaction = APlan.ServerProcess.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushGlobalContext();
					try
					{
						// if the requested cursor type is static, ensure that is the case
						if ((APlan.CursorContext.CursorType == DAE.CursorType.Static) && (ASourceNode.CursorType != DAE.CursorType.Static))
						{
							ASourceNode = (TableNode)Compiler.EmitCopyNode(APlan, ASourceNode);
							ASourceNode.InferPopulateNode(APlan);
							ASourceNode.DetermineDevice(APlan);
						}
							
						// Navigable
						if ((APlan.CursorContext.CursorCapabilities & CursorCapability.Navigable) != 0)
							ASourceNode.CheckCapability(CursorCapability.Navigable);
							
						// If the cursor is requested countable, it must be satisfied by a copy node
						if ((APlan.CursorContext.CursorCapabilities & CursorCapability.Countable) != 0)
						{
							if (!ASourceNode.Supports(CursorCapability.Countable))
							{
								ASourceNode = (TableNode)Compiler.EmitCopyNode(APlan, ASourceNode);
								ASourceNode.InferPopulateNode(APlan);
								ASourceNode.DetermineDevice(APlan);
							}
						}

						// BackwardsNavigable
						// Bookmarkable
						// Searchable
						if
							(
								(((APlan.CursorContext.CursorCapabilities & CursorCapability.BackwardsNavigable) != 0) && !ASourceNode.Supports(CursorCapability.BackwardsNavigable)) ||
								(((APlan.CursorContext.CursorCapabilities & CursorCapability.Bookmarkable) != 0) && !ASourceNode.Supports(CursorCapability.Bookmarkable)) ||
								(((APlan.CursorContext.CursorCapabilities & CursorCapability.Searchable) != 0) && !ASourceNode.Supports(CursorCapability.Searchable))
							)
						{
							ASourceNode = (TableNode)Compiler.EmitBrowseNode(APlan, ASourceNode, true);
							ASourceNode.InferPopulateNode(APlan);
							ASourceNode.DetermineDevice(APlan);
						}

						// Updateable
						if ((APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) != 0)
							ASourceNode.CheckCapability(CursorCapability.Updateable);

						// Truncateable
						if ((APlan.CursorContext.CursorCapabilities & CursorCapability.Truncateable) != 0)
							ASourceNode.CheckCapability(CursorCapability.Truncateable);
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
			finally
			{
				APlan.PopCursorContext();
			}
			return ASourceNode;	
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			SourceNode = BindSourceNode(APlan, SourceNode);
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.PushCursorContext(FCursorContext);
			try
			{
				base.BindToProcess(APlan);
			}
			finally
			{
				APlan.PopCursorContext();
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			return new CursorValue(AProcess, CursorType, AProcess.Plan.CursorManager.CreateCursor((Table)Nodes[0].Execute(AProcess)));
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			TableNode LNode = (TableNode)Nodes[0];
			return new CursorSelectorExpression(new CursorDefinition((Expression)LNode.EmitStatement(AMode), LNode.CursorCapabilities, LNode.CursorIsolation, LNode.CursorType));
		}
	}
	
	// operator Close(ACursor : cursor);
	public class CursorCloseNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			AProcess.Plan.CursorManager.CloseCursor(((CursorValue)AArguments[0]).ID);
			return null;
		}
	}

    // operator First(ACursor : cursor);
    public class CursorFirstNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.First();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
			return null;
		}
	}
    
	// operator Last(ACursor : cursor);
	public class CursorLastNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.Last();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
			return null;
		}
	}
    
	// operator Next(Cursor) : boolean;
	public class CursorNextNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.Next();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
    
	// operator Prior(Cursor) : boolean;
	public class CursorPriorNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.Prior();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator BOF(Cursor) : boolean;
	public class CursorBOFNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.BOF();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator EOF(Cursor) : boolean;
	public class CursorEOFNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.EOF();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator Select(Cursor) : row
	public class CursorSelectNode : InstructionNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			FDataType = ((Schema.CursorType)Nodes[0].DataType).TableType.RowType;
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.Select();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator Fetch(Cursor) : table{}
	// operator Fetch(Cursor, int) : table{}

	// operator Insert(Cursor, row{})
	public class CursorInsertNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.CheckCapability(CursorCapability.Updateable);
				LCursor.Table.Insert((Row)AArguments[1]);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
			return null;
		}
	}

	// operator Update(Cursor, row{})
	public class CursorUpdateNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.CheckCapability(CursorCapability.Updateable);
				LCursor.Table.Update((Row)AArguments[1]);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
			return null;
		}
	}
	
	// operator Delete(Cursor)
	public class CursorDeleteNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.CheckCapability(CursorCapability.Updateable);
				LCursor.Table.Delete();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
			return null;
		}
	}
	
	// operator Default(const ACursor : cursor, var ARow : row) : Boolean;
	// operator Default(const ACursor : cursor, var ARow : row, const AColumnName : String) : Boolean;
	public class CursorDefaultNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				Table LTable = LCursor.Table;
				Row LRow = (Row)AArguments[1];
				string LColumnName = AArguments.Length == 3 ? (string)AArguments[2] : String.Empty;
				return LTable.Node.Default(AProcess, null, LRow, null, LColumnName);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator Change(const ACursor : cursor, const AOldRow : row, var ANewRow : row) : Boolean;
	// operator Change(const ACursor : cursor, const AOldRow : row, var ANewRow : row, const AColumnName : String) : Boolean
	public class CursorChangeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				Table LTable = LCursor.Table;
				Row LOldRow = (Row)AArguments[1];
				Row LNewRow = (Row)AArguments[2];
				string LColumnName = AArguments.Length == 4 ? (string)AArguments[3] : String.Empty;
				return LTable.Node.Change(AProcess, LOldRow, LNewRow, null, LColumnName);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}

	// operator Validate(const ACursor : cursor, const AOldRow : row, var ANewRow : row) : Boolean;
	// operator Validate(const ACursor : cursor, const AOldRow : row, var ANewRow : row, const AColumnName : String) : Boolean;	
	public class CursorValidateNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				Table LTable = LCursor.Table;
				Row LOldRow = (Row)AArguments[1];
				Row LNewRow = (Row)AArguments[2];
				string LColumnName = AArguments.Length == 4 ? (string)AArguments[3] : String.Empty;
				return LTable.Node.Validate(AProcess, LOldRow, LNewRow, null, LColumnName);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator GetKey(cursor) : row;
	public class CursorGetKeyNode : InstructionNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			//FDataType = ((Schema.CursorType)Nodes[0].DataType).TableType.RowType;
			// TODO: This is wrong, the capabilities and order of a cursor should be part of the cursor type specifier
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.GetKey();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator FindKey(cursor, row) : boolean
	public class CursorFindKeyNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.FindKey((Row)AArguments[1]);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator FindNearest(cursor, row);
	public class CursorFindNearestNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.FindNearest((Row)AArguments[1]);
				return null;
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator Refresh(cursor)
	// operator Refresh(cursor, row);
	public class CursorRefreshNode: InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				if (AArguments.Length == 1)
					LCursor.Table.Refresh(null);
				else
					LCursor.Table.Refresh((Row)AArguments[1]);
				return null;
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator Reset(cursor);
	public class CursorResetNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				LCursor.Table.Reset();
				return null;
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator GetBookmark(cursor) : row;
	public class CursorGetBookmarkNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.GetBookmark();
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator GotoBookmark(cursor, row) : boolean;
	public class CursorGotoBookmarkNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.GotoBookmark((Row)AArguments[1]);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
	
	// operator CompareBookmarks(cursor, row, row) : integer;
	public class CursorCompareBookmarksNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[0]).ID);
			LCursor.SwitchContext(AProcess);
			try
			{
				return LCursor.Table.CompareBookmarks((Row)AArguments[1], (Row)AArguments[1]);
			}
			finally
			{
				LCursor.SwitchContext(AProcess);
			}
		}
	}
}

