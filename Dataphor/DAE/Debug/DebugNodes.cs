/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Debug
{
	// operator GetDebuggers() : table { Session_ID : Integer, BreakOnException : Boolean, IsPaused : Boolean }
	public class DebugGetDebuggersNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Session_ID", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("BreakOnException", APlan.Catalog.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsPaused", APlan.Catalog.DataTypes.SystemBoolean));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Session_ID"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					foreach (Debugger LDebugger in AProcess.ServerSession.Server.GetDebuggers())
					{
						LRow[0] = LDebugger.Session.SessionID;
						LRow[1] = LDebugger.BreakOnException;
						LRow[2] = LDebugger.IsPaused;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}

	// operator Debug.SetBreakOnException(ABreakOnException : Boolean)
	public class DebugSetBreakOnExceptionNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			AProcess.ServerSession.CheckedDebugger.BreakOnException = (bool)AArgument1;
			return null;
		}
	}

	// operator Debug.GetSessions() : table { Session_ID : Integer }
	public class DebugGetSessionsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Session_ID", APlan.Catalog.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Session_ID"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					if (AProcess.ServerSession.Debugger != null)
						foreach (DebugSessionInfo LSession in AProcess.ServerSession.CheckedDebugger.GetSessions())
						{
							LRow[0] = LSession.SessionID;
							LResult.Insert(LRow);
						}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}

	// operator Debug.GetProcesses() : table { Process_ID : Integer, IsPaused : Boolean, Locator : String, Line : Integer, LinePos : Integer, DidBreak : Boolean }
	public class DebugGetProcessesNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Process_ID", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("IsPaused", APlan.Catalog.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Locator", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("DidBreak", APlan.Catalog.DataTypes.SystemBoolean));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Process_ID"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					
					if (AProcess.ServerSession.Debugger != null)
						foreach (DebugProcessInfo LProcess in AProcess.ServerSession.CheckedDebugger.GetProcesses())
						{
							LRow[0] = LProcess.ProcessID;
							LRow[1] = LProcess.IsPaused;
							if (LProcess.Location != null)
							{
								LRow[2] = LProcess.Location.Locator;
								LRow[3] = LProcess.Location.Line;
								LRow[4] = LProcess.Location.LinePos;
								LRow[5] = LProcess.DidBreak;
							}
							else
							{
								LRow[2] = null;
								LRow[3] = null;
								LRow[4] = null;
								LRow[5] = null;
							}

							LResult.Insert(LRow);
						}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}

	// operator AttachProcess(AProcessID : Integer)
	public class DebugAttachProcessNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			AProcess.ServerSession.CheckedDebugger.Attach(AProcess.ServerSession.Server.GetProcess((int)AArgument1));
			return null;
		}
	}

	//* Operator: DetachProcess
	// operator DetachProcess(AProcessID : Integer) 
	public class DebugDetachProcessNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			AProcess.ServerSession.CheckedDebugger.Detach(AProcess.ServerSession.Server.GetProcess((int)AArgument1));
			return null;
		}
	}
	
	// operator AttachSession(ASessionID : Integer)
	public class DebugAttachSessionNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			AProcess.ServerSession.CheckedDebugger.AttachSession(AProcess.ServerSession.Server.GetSession((int)AArgument1));
			return null;
		}
	}
	
	// operator DetachSession(ASessionID : Integer)
	public class DebugDetachSessionNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			AProcess.ServerSession.CheckedDebugger.DetachSession(AProcess.ServerSession.Server.GetSession((int)AArgument1));
			return null;
		}
	}
	
	// operator GetCallStack(AProcessID : Integer) : table { Index : Integer, Description : String }
	public class DebugGetCallStackNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Index", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Description", APlan.Catalog.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			int LProcessID = (int)Nodes[0].Execute(AProcess);

			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					var LDebugger = AProcess.ServerSession.CheckedDebugger;
					if (LDebugger != null)
					{
						foreach (CallStackEntry LEntry in LDebugger.GetCallStack(LProcessID))
						{
							LRow[0] = LEntry.Index;
							LRow[1] = LEntry.Description;
							LResult.Insert(LRow);
						}
					}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}

	// operator GetStack(AProcessID : Integer, AWindowIndex : Integer) : table { Index : Integer, Name : Name, Type : String, Value : String }
	public class DebugGetStackNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Index", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", APlan.Catalog.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Type", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Value", APlan.Catalog.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			int LProcessID = (int)Nodes[0].Execute(AProcess);
			int LWindowIndex = (int)Nodes[1].Execute(AProcess);

			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					foreach (StackEntry LEntry in AProcess.ServerSession.CheckedDebugger.GetStack(LProcessID, LWindowIndex))
					{
						LRow[0] = LEntry.Index;
						LRow[1] = LEntry.Name;
						LRow[2] = LEntry.Type;
						LRow[3] = LEntry.Value;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}
	
	//* Operator: GetContext(AProcessID : Integer) : row { Locator : String, Line : Integer, LinePos : Integer, Script : String }
	public class DebugGetContextNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			DebugContext LContext = AProcess.ServerSession.CheckedDebugger.GetContext((int)AArgument1);
			Row LRow = new Row(AProcess, (Schema.RowType)DataType);
			LRow[0] = LContext.Locator;
			LRow[1] = LContext.Line;
			LRow[2] = LContext.LinePos;
			LRow[3] = LContext.Script;
			return LRow;
		}
	}
		
/*
	//* Operator: GetStepIntoOperators
	// operator GetStepIntoOperators(AProcessID : Integer) : table { Index : Integer, OperatorName : Name }
	public class DebugGetStepIntoOperatorsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Index", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("OperatorName", APlan.Catalog.DataTypes.SystemName));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			int LProcessID = (int)Nodes[0].Execute(AProcess);

			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					var LDebugger = AProcess.ServerSession.CheckedDebugger;
					if (LDebugger != null)
					{
						foreach (StepIntoThingy LEntry in LDebugger.GetStepIntoOperators(LProcessID))
						{
							LRow[0] = LEntry.Index;
							LRow[1] = LEntry.OperatorName;
							LResult.Insert(LRow);
						}
					}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}
*/

	// operator Debug.GetBreakpoints() : table { Locator : String, Line : Integer, LinePos : Integer }
	public class DebugGetBreakpointsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Locator", APlan.Catalog.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Line", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("LinePos", APlan.Catalog.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					var LDebugger = AProcess.ServerSession.CheckedDebugger;
					if (LDebugger != null)
					{
						foreach (Breakpoint LEntry in LDebugger.Breakpoints)
						{
							LRow[0] = LEntry.Locator;
							LRow[1] = LEntry.Line;
							LRow[2] = LEntry.LinePos;
							LResult.Insert(LRow);
						}
					}
				}
				finally
				{
					LRow.Dispose();
				}

				LResult.First();

				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
	}
	
	// operator Debug.ToggleBreakpoint(ALocator : String, ALine : Integer, ALinePos : Integer) : Bool
	public class DebugToggleBreakpointNode : TernaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2, object AArgument3)
		{
 			return AProcess.ServerSession.CheckedDebugger.ToggleBreakpoint((string)AArgument1, (int)AArgument2, AArgument3 == null ? -1 : (int)AArgument3);
		}
	}
	
	// operator Debug.Start()
	public class DebugStartNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			AProcess.ServerSession.StartDebugger();
			return null;
		}
	}

	// operator Debug.Stop()
	public class DebugStopNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
 			AProcess.ServerSession.StopDebugger();
 			return null;
		}
	}

	// operator Debug.WaitForPause()
	public class DebugWaitForPauseNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			AProcess.ServerSession.CheckedDebugger.WaitForPause(AProcess);
			return null;
		}
	}

	// operator Debug.Pause()
	public class DebugPauseNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			AProcess.ServerSession.CheckedDebugger.Pause();
			return null;
 		}
	}

	// operator Debug.Run()
	public class DebugRunNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
 			AProcess.ServerSession.CheckedDebugger.Run();
 			return null;
 		}
	}

/*
	// operator Debug.RunTo(AProcessID : Integer, ALocator : string, ALine : Integer, ALinePos : Integer) 
	public class DebugRunToNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
 			var LDebugger = AProcess.ServerSession.CheckedDebugger;
 			if (LDebugger != null)
 			{
 				var LProcess = AProcess.ServerSession.Processes.GetProcess((int)AArguments[0]);
 				LDebugger.RunTo(LProcess, new DebugLocator((string)AArguments[1], (int)AArguments[2], AArguments[3] == null ? -1 : (int)AArguments[3]));
 			}
 		}
	}

	// operator Debug.StepOver(AProcessID : Integer) 
	public class DebugStepOverNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
 			var LDebugger = AProcess.ServerSession.CheckedDebugger;
 			if (LDebugger != null)
 			{
 				var LProcess = AProcess.ServerSession.Processes.GetProcess((int)AArgument1);
 				LDebugger.StepOver(LProcess);
 			}
		}
	}

	// operator Debug.StepInto(AProcessID : Integer) 
	public class DebugStepIntoNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			var LDebugger = AProcess.ServerSession.CheckedDebugger;
			if (LDebugger != null)
			{
				var LProcess = AProcess.ServerSession.Processes.GetProcess((int)AArgument1);
				LDebugger.StepInto(LProcess);
			}
		}
	}

	// operator StepIntoSpecific(AProcessID : Integer, AOperatorName : Name) 
	public class DebugStepIntoSpecificNode : BinaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			var LDebugger = AProcess.ServerSession.CheckedDebugger;
			if (LDebugger != null)
			{
				var LProcess = AProcess.ServerSession.Processes.GetProcess((int)AArgument1);
				LDebugger.StepIntoSpecific(LProcess, (string)AArgument2);
			}
		}
	}
*/
}