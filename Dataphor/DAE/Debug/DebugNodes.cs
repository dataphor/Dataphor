/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Debug
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	// operator GetDebuggers() : table { Session_ID : Integer, BreakOnException : Boolean, IsPaused : Boolean }
	public class DebugGetDebuggersNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Session_ID", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("BreakOnException", APlan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsPaused", APlan.DataTypes.SystemBoolean));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Session_ID"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					foreach (Debugger LDebugger in AProgram.ServerProcess.ServerSession.Server.GetDebuggers())
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
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.BreakOnException = (bool)AArgument1;
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

			DataType.Columns.Add(new Schema.Column("Session_ID", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Session_ID"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					if (AProgram.ServerProcess.ServerSession.Debugger != null)
						foreach (DebugSessionInfo LSession in AProgram.ServerProcess.ServerSession.CheckedDebugger.GetSessions())
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

			DataType.Columns.Add(new Schema.Column("Process_ID", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("IsPaused", APlan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Locator", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("DidBreak", APlan.DataTypes.SystemBoolean));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Process_ID"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					
					if (AProgram.ServerProcess.ServerSession.Debugger != null)
						foreach (DebugProcessInfo LProcess in AProgram.ServerProcess.ServerSession.CheckedDebugger.GetProcesses())
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
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.Attach(AProgram.ServerProcess.ServerSession.Server.GetProcess((int)AArgument1));
			return null;
		}
	}

	//* Operator: DetachProcess
	// operator DetachProcess(AProcessID : Integer) 
	public class DebugDetachProcessNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.Detach(AProgram.ServerProcess.ServerSession.Server.GetProcess((int)AArgument1));
			return null;
		}
	}
	
	// operator AttachSession(ASessionID : Integer)
	public class DebugAttachSessionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.AttachSession(AProgram.ServerProcess.ServerSession.Server.GetSession((int)AArgument1));
			return null;
		}
	}
	
	// operator DetachSession(ASessionID : Integer)
	public class DebugDetachSessionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.DetachSession(AProgram.ServerProcess.ServerSession.Server.GetSession((int)AArgument1));
			return null;
		}
	}
	
	// operator GetCallStack(AProcessID : Integer) : table { Index : Integer, Description : String, Locator : String, Line : Integer, LinePos : Integer }
	public class DebugGetCallStackNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Index", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Description", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Locator", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			int LProcessID = (int)Nodes[0].Execute(AProgram);

			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					var LDebugger = AProgram.ServerProcess.ServerSession.CheckedDebugger;
					if (LDebugger != null)
					{
						foreach (CallStackEntry LEntry in LDebugger.GetCallStack(LProcessID))
						{
							LRow[0] = LEntry.Index;
							LRow[1] = LEntry.Description;
							LRow[2] = LEntry.Location.Locator;
							LRow[3] = LEntry.Location.Line;
							LRow[4] = LEntry.Location.LinePos;
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

			DataType.Columns.Add(new Schema.Column("Index", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Type", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Value", APlan.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			int LProcessID = (int)Nodes[0].Execute(AProgram);
			int LWindowIndex = (int)Nodes[1].Execute(AProgram);

			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					foreach (StackEntry LEntry in AProgram.ServerProcess.ServerSession.CheckedDebugger.GetStack(LProcessID, LWindowIndex))
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
	
	//* Operator: GetContext(ALocator : String) : String
	public class DebugGetContextNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			if (AArgument1 == null)
				return null;
				
			string LLocator = (string)AArgument1;

			if (DebugLocator.IsProgramLocator(LLocator))
				return AProgram.ServerProcess.ServerSession.CheckedDebugger.GetProgram(DebugLocator.GetProgramID(LLocator)).Source;

			if (DebugLocator.IsOperatorLocator(LLocator))
				return new D4TextEmitter().Emit(((Schema.Operator)AProgram.ResolveCatalogObjectSpecifier(DebugLocator.GetOperatorSpecifier(LLocator), true)).EmitStatement(EmitMode.ForCopy));
				
			throw new ServerException(ServerException.Codes.InvalidDebugLocator, LLocator);
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

			DataType.Columns.Add(new Schema.Column("Index", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("OperatorName", APlan.DataTypes.SystemName));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			int LProcessID = (int)Nodes[0].Execute(AProcess);

			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
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

			DataType.Columns.Add(new Schema.Column("Locator", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Locator"], TableVar.Columns["Line"], TableVar.Columns["LinePos"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					var LDebugger = AProgram.ServerProcess.ServerSession.CheckedDebugger;
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
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2, object AArgument3)
		{
 			return AProgram.ServerProcess.ServerSession.CheckedDebugger.ToggleBreakpoint((string)AArgument1, (int)AArgument2, AArgument3 == null ? -1 : (int)AArgument3);
		}
	}
	
	// operator Debug.Start()
	public class DebugStartNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
			AProgram.ServerProcess.ServerSession.StartDebugger();
			return null;
		}
	}

	// operator Debug.Stop()
	public class DebugStopNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
 			AProgram.ServerProcess.ServerSession.StopDebugger();
 			return null;
		}
	}

	// operator Debug.WaitForPause()
	public class DebugWaitForPauseNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.WaitForPause(AProgram.ServerProcess);
			return null;
		}
	}

	// operator Debug.Pause()
	public class DebugPauseNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
			AProgram.ServerProcess.ServerSession.CheckedDebugger.Pause();
			return null;
 		}
	}

	// operator Debug.Run()
	public class DebugRunNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
 			AProgram.ServerProcess.ServerSession.CheckedDebugger.Run();
 			return null;
 		}
	}

/*
	// operator Debug.RunTo(AProcessID : Integer, ALocator : string, ALine : Integer, ALinePos : Integer) 
	public class DebugRunToNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
 			var LDebugger = AProgram.ServerProcess.ServerSession.CheckedDebugger;
 			if (LDebugger != null)
 			{
 				var LProcess = AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArguments[0]);
 				LDebugger.RunTo(LProcess, new DebugLocator((string)AArguments[1], (int)AArguments[2], AArguments[3] == null ? -1 : (int)AArguments[3]));
 			}
 		}
	}

	// operator Debug.StepOver(AProcessID : Integer) 
	public class DebugStepOverNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
 			var LDebugger = AProgram.ServerProcess.ServerSession.CheckedDebugger;
 			if (LDebugger != null)
 			{
 				var LProcess = AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArgument1);
 				LDebugger.StepOver(LProcess);
 			}
		}
	}

	// operator Debug.StepInto(AProcessID : Integer) 
	public class DebugStepIntoNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			var LDebugger = AProgram.ServerProcess.ServerSession.CheckedDebugger;
			if (LDebugger != null)
			{
				var LProcess = AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArgument1);
				LDebugger.StepInto(LProcess);
			}
		}
	}

	// operator StepIntoSpecific(AProcessID : Integer, AOperatorName : Name) 
	public class DebugStepIntoSpecificNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			var LDebugger = AProgram.ServerProcess.ServerSession.CheckedDebugger;
			if (LDebugger != null)
			{
				var LProcess = AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArgument1);
				LDebugger.StepIntoSpecific(LProcess, (string)AArgument2);
			}
		}
	}
*/
}