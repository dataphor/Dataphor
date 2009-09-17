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

	/// <remarks>operator SetDefaultDeviceName(ADeviceName : Name);</remarks>    
	/// <remarks>operator SetDefaultDeviceName(ALibraryName : Name, ADeviceName : Name);</remarks>
    public class SystemSetDefaultDeviceNameNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments.Length == 2)
				SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProgram, (string)AArguments[0], (string)AArguments[1], true);
			else
				SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProgram, AProgram.ServerProcess.ServerSession.CurrentLibrary.Name, (string)AArguments[0], true);
			return null;
		}
    }
    
	// operator System.Diagnostics.ShowLog() : String
	// operator System.Diagnostics.ShowLog(const ALogIndex : Integer) : String
	public class SystemShowLogNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.ShowLog(AArguments.Length == 0 ? 0 : (int)AArguments[0]);
		}
	}
	
	// operator System.Diagnostics.ListLogs() : table { Sequence : Integer, LogName : String }
    public class SystemListLogsNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LogName", APlan.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

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
					List<string> LLogs = AProgram.ServerProcess.ServerSession.Server.ListLogs();
					for (int LIndex = 0; LIndex < LLogs.Count; LIndex++)
					{
						LRow[0] = LIndex;
						LRow[1] = LLogs[LIndex];
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
}
