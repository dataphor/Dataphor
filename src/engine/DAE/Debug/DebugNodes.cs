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

	// operator GetDebuggers() : table { Session_ID : Integer, BreakOnStart : Boolean, BreakOnException : Boolean, IsPaused : Boolean }
	public class DebugGetDebuggersNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Session_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("BreakOnStart", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("BreakOnException", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsPaused", plan.DataTypes.SystemBoolean));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Session_ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Debugger debugger in program.ServerProcess.ServerSession.Server.GetDebuggers())
					{
						row[0] = debugger.Session.SessionID;
						row[1] = debugger.BreakOnStart;
						row[2] = debugger.BreakOnException;
						row[3] = debugger.IsPaused;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}

				result.First();

				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}
	
	// operator Debug.SetBreakOnStart(ABreakOnStart : Boolean)
	public class DebugSetBreakOnStartNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
 			 program.ServerProcess.ServerSession.CheckedDebugger.BreakOnStart = (bool)argument1;
 			 return null;
		}
	}

	// operator Debug.SetBreakOnException(ABreakOnException : Boolean)
	public class DebugSetBreakOnExceptionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.BreakOnException = (bool)argument1;
			return null;
		}
	}

	// operator Debug.GetSessions() : table { Session_ID : Integer }
	public class DebugGetSessionsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Session_ID", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Session_ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					if (program.ServerProcess.ServerSession.Debugger != null)
						foreach (DebugSessionInfo session in program.ServerProcess.ServerSession.CheckedDebugger.GetSessions())
						{
							row[0] = session.SessionID;
							result.Insert(row);
						}
				}
				finally
				{
					row.Dispose();
				}

				result.First();

				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	// operator Debug.GetProcesses() : table { Process_ID : Integer, IsPaused : Boolean, Locator : String, Line : Integer, LinePos : Integer, DidBreak : Boolean, Error : Error }
	public class DebugGetProcessesNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Process_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("IsPaused", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Locator", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("DidBreak", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Error", plan.DataTypes.SystemError));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Process_ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;
					
					if (program.ServerProcess.ServerSession.Debugger != null)
						foreach (DebugProcessInfo process in program.ServerProcess.ServerSession.CheckedDebugger.GetProcesses())
						{
							row[0] = process.ProcessID;
							row[1] = process.IsPaused;
							if (process.Location != null)
							{
								row[2] = process.Location.Locator;
								row[3] = process.Location.Line;
								row[4] = process.Location.LinePos;
								row[5] = process.DidBreak;
								row[6] = process.Error;
							}
							else
							{
								row[2] = null;
								row[3] = null;
								row[4] = null;
								row[5] = null;
								row[6] = null;
							}

							result.Insert(row);
						}
				}
				finally
				{
					row.Dispose();
				}

				result.First();

				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	// operator AttachProcess(AProcessID : Integer)
	public class DebugAttachProcessNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.Attach(program.ServerProcess.ServerSession.Server.GetProcess((int)argument1));
			return null;
		}
	}

	//* Operator: DetachProcess
	// operator DetachProcess(AProcessID : Integer) 
	public class DebugDetachProcessNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.Detach(program.ServerProcess.ServerSession.Server.GetProcess((int)argument1));
			return null;
		}
	}
	
	// operator AttachSession(ASessionID : Integer)
	public class DebugAttachSessionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.AttachSession(program.ServerProcess.ServerSession.Server.GetSession((int)argument1));
			return null;
		}
	}
	
	// operator DetachSession(ASessionID : Integer)
	public class DebugDetachSessionNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.DetachSession(program.ServerProcess.ServerSession.Server.GetSession((int)argument1));
			return null;
		}
	}
	
	// operator GetCallStack(AProcessID : Integer) : table { Index : Integer, Description : String, Locator : String, Line : Integer, LinePos : Integer, Location : String, Statement : String }
	public class DebugGetCallStackNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Index", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Locator", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Location", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Statement", plan.DataTypes.SystemString));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			int processID = (int)Nodes[0].Execute(program);

			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					var debugger = program.ServerProcess.ServerSession.CheckedDebugger;
					if (debugger != null)
					{
						foreach (CallStackEntry entry in debugger.GetCallStack(processID))
						{
							row[0] = entry.Index;
							row[1] = entry.Description;
							row[2] = entry.Locator.Locator;
							row[3] = entry.Locator.Line;
							row[4] = entry.Locator.LinePos;
							row[5] = entry.Location;
							row[6] = entry.Statement;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}

				result.First();

				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	// operator GetStack(AProcessID : Integer, AWindowIndex : Integer) : table { Index : Integer, Name : Name, Type : String, Value : String }
	public class DebugGetStackNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Index", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Type", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Value", plan.DataTypes.SystemString));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Index"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			int processID = (int)Nodes[0].Execute(program);
			int windowIndex = (int)Nodes[1].Execute(program);

			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (StackEntry entry in program.ServerProcess.ServerSession.CheckedDebugger.GetStack(processID, windowIndex))
					{
						row[0] = entry.Index;
						row[1] = entry.Name;
						row[2] = entry.Type;
						row[3] = entry.Value;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}

				result.First();

				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}
	
	//* Operator: GetSource(ALocator : String) : String
	public class DebugGetSourceNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			if (argument1 == null)
				return null;
				
			string locator = (string)argument1;

			if (DebugLocator.IsProgramLocator(locator))
				return program.ServerProcess.ServerSession.CheckedDebugger.GetProgram(DebugLocator.GetProgramID(locator)).Source;

			if (DebugLocator.IsOperatorLocator(locator))
				return new D4TextEmitter().Emit(((Schema.Operator)program.ResolveCatalogObjectSpecifier(DebugLocator.GetOperatorSpecifier(locator), true)).EmitStatement(EmitMode.ForCopy));
				
			throw new ServerException(ServerException.Codes.InvalidDebugLocator, locator);
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
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Locator", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Line", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LinePos", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Locator"], TableVar.Columns["Line"], TableVar.Columns["LinePos"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					var debugger = program.ServerProcess.ServerSession.CheckedDebugger;
					if (debugger != null)
					{
						foreach (Breakpoint entry in debugger.Breakpoints)
						{
							row[0] = entry.Locator;
							row[1] = entry.Line;
							row[2] = entry.LinePos;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}

				result.First();

				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}
	
	// operator Debug.ToggleBreakpoint(ALocator : String, ALine : Integer, ALinePos : Integer) : Bool
	public class DebugToggleBreakpointNode : TernaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2, object argument3)
		{
 			return program.ServerProcess.ServerSession.CheckedDebugger.ToggleBreakpoint((string)argument1, (int)argument2, argument3 == null ? -1 : (int)argument3);
		}
	}
	
	// operator Debug.Start()
	public class DebugStartNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			program.ServerProcess.ServerSession.StartDebugger();
			return null;
		}
	}

	// operator Debug.Stop()
	public class DebugStopNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
 			program.ServerProcess.ServerSession.StopDebugger();
 			return null;
		}
	}

	// operator Debug.WaitForPause()
	public class DebugWaitForPauseNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.WaitForPause(program, this);
			return null;
		}
	}

	// operator Debug.Pause()
	public class DebugPauseNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.Pause();
			return null;
 		}
	}

	// operator Debug.Run()
	public class DebugRunNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
 			program.ServerProcess.ServerSession.CheckedDebugger.Run();
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
*/

	// operator Debug.StepOver(AProcessID : Integer) 
	public class DebugStepOverNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
 			program.ServerProcess.ServerSession.CheckedDebugger.StepOver((int)argument1);
 			return null;
		}
	}

	// operator Debug.StepInto(AProcessID : Integer) 
	public class DebugStepIntoNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			program.ServerProcess.ServerSession.CheckedDebugger.StepInto((int)argument1);
			return null;
		}
	}

/*
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