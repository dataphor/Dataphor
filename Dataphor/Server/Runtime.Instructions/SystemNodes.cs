/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Device.Catalog;

	/// <remarks>operator SetDefaultDeviceName(ADeviceName : Name);</remarks>    
	/// <remarks>operator SetDefaultDeviceName(ALibraryName : Name, ADeviceName : Name);</remarks>
    public class SystemSetDefaultDeviceNameNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 2)
				SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(program, (string)arguments[0], (string)arguments[1], true);
			else
				SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(program, program.ServerProcess.ServerSession.CurrentLibrary.Name, (string)arguments[0], true);
			return null;
		}
    }
    
	// operator System.Diagnostics.ShowLog() : String
	// operator System.Diagnostics.ShowLog(const ALogIndex : Integer) : String
	public class SystemShowLogNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return ((Server)program.ServerProcess.ServerSession.Server).ShowLog(arguments.Length == 0 ? 0 : (int)arguments[0]);
		}
	}
	
	// operator System.Diagnostics.ListLogs() : table { Sequence : Integer, LogName : String }
    public class SystemListLogsNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LogName", plan.DataTypes.SystemString));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

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
					List<string> logs = program.ServerProcess.ServerSession.Server.ListLogs();
					for (int index = 0; index < logs.Count; index++)
					{
						row[0] = index;
						row[1] = logs[index];
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

	// operator System.SetMaxConcurrentProcesses(const AMaxConcurrentProcesses : System.Integer);
	public class SystemSetMaxConcurrentProcessesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.MaxConcurrentProcesses = (int)arguments[0];
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SaveServerSettings(program.ServerProcess.ServerSession.Server);
			return null;
		}
	}

	// operator System.SetProcessWaitTimeout(const AProcessWaitTimeout : System.TimeSpan);
	public class SystemSetProcessWaitTimeoutNode: InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.ProcessWaitTimeout = ((TimeSpan)arguments[0]);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SaveServerSettings(program.ServerProcess.ServerSession.Server);
			return null;
		}
	}

	/// <remarks>operator MachineName() : String;</remarks>
	public class SystemMachineNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return System.Environment.MachineName;
		}
	}
	
	/// <remarks>operator HostName() : String;</remarks>
	public class SystemHostNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.SessionInfo.HostName;
		}
	}
}
