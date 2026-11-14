/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.Device.Simple
{
	public class SaveTableNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			SimpleDevice device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as SimpleDevice;
			if (device == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar tableVar = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.TableVar;
			if (tableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			device.SaveTable(program.ServerProcess, tableVar);
			return null;
		}
	}
	
	public class LoadTableNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			SimpleDevice device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as SimpleDevice;
			if (device == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar tableVar = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.TableVar;
			if (tableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			device.LoadTable(program.ServerProcess, tableVar);
			return null;
		}
	}
	
	public class TruncateTableNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			SimpleDevice device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as SimpleDevice;
			if (device == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar tableVar = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.TableVar;
			if (tableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			device.TruncateTable(program.ServerProcess, tableVar);
			return null;
		}
	}
	
	public class BeginUpdateNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			SimpleDevice device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as SimpleDevice;
			if (device == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar tableVar = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.TableVar;
			if (tableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			device.BeginUpdate(program.ServerProcess, tableVar);
			return null;
		}
	}
	
	public class EndUpdateNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			SimpleDevice device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as SimpleDevice;
			if (device == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar tableVar = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.TableVar;
			if (tableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			device.EndUpdate(program.ServerProcess, tableVar);
			return null;
		}
	}
	
	public class SimpleDevice : MemoryDevice
	{
		public const string DataDirectoryName = "Data";
	
		public SimpleDevice(int iD, string name) : base(iD, name)
		{
		}
		
		protected override void InternalStart(ServerProcess process)
		{
			if (String.IsNullOrEmpty(_directoryName))
				_directoryName = DataDirectoryName;
			if (!Path.IsPathRooted(_directoryName))
				_directoryName = Path.Combine(((Server.Server)process.ServerSession.Server).InstanceDirectory, _directoryName);
			if (!Directory.Exists(_directoryName))
				Directory.CreateDirectory(_directoryName);
			
			base.InternalStart(process);
		}
		
		protected override void InternalStop(ServerProcess process)
		{
			foreach (SimpleDeviceHeader header in _headers.Values)
				if (header.TimeStamp > header.SaveTimeStamp)
					SaveTable(process, header.TableVar);
			base.InternalStop(process);
		}
		
		// Session
		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new SimpleDeviceSession(this, serverProcess, deviceSessionInfo);
		}
		
		private SimpleDeviceHeaders _headers = new SimpleDeviceHeaders();
		public SimpleDeviceHeaders Headers { get { return _headers; } }

		private string _directoryName;
		public string DirectoryName
		{
			get { return _directoryName; }
			set 
			{
				CheckNotRunning();
				_directoryName = value;
			}
		}
		
		private bool _autoSave = true;
		public bool AutoSave
		{
			get { return _autoSave; }
			set { _autoSave = value; }
		}
		
		internal string GetTableVarFileName(Schema.TableVar tableVar)
		{
			return Path.Combine(_directoryName, String.Format("{0}.dat", tableVar.Name));
		}
		
		public void SaveTable(ServerProcess process, Schema.TableVar tableVar)
		{
			SimpleDeviceHeader header = _headers[tableVar];
			if ((header.UpdateCount == 0) && (header.TimeStamp > header.SaveTimeStamp))
			{
				Plan plan = new Plan(process);
				try
				{
					plan.PushSecurityContext(new SecurityContext(tableVar.Owner));
					try
					{
						plan.PushGlobalContext();
						try
						{
							Program program = new Program(process);
							program.Code = Compiler.Compile(plan, new IdentifierExpression(tableVar.Name), true).ExtractNode<BaseTableVarNode>();
							program.Start(null);
							try
							{
								ITable table = (ITable)program.Code.Execute(program);
								try
								{
									table.Open();
									Row row = new Row(program.ValueManager, tableVar.DataType.CreateRowType());
									try
									{
										string fileName = GetTableVarFileName(tableVar);
										FileUtility.EnsureWriteable(fileName);
										using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
										{
											while (table.Next())
											{
												table.Select(row);
												byte[] rowValue = row.AsPhysical;
												StreamUtility.WriteInteger(stream, rowValue.Length);
												stream.Write(rowValue, 0, rowValue.Length);
											}
										}
									}
									finally
									{
										row.Dispose();
									}
								}
								finally
								{
									table.Dispose();
								}
							}
							finally
							{
								program.Stop(null);
							}
						}
						finally
						{
							plan.PopGlobalContext();
						}
					}
					finally
					{
						plan.PopSecurityContext();
					}
				}
				finally
				{
					plan.Dispose();
				}
				header.SaveTimeStamp = header.TimeStamp;
			}
		}
		
		private Schema.Objects _loadedTables = new Schema.Objects();
		public bool IsLoaded(Schema.TableVar tableVar)
		{
			return _loadedTables.Contains(tableVar);
		}
		
		public void LoadTable(ServerProcess process, Schema.TableVar tableVar)
		{
			lock (_loadedTables)
			{
				if (!IsLoaded(tableVar))
				{
					_loadedTables.Add(tableVar);
					if (File.Exists(GetTableVarFileName(tableVar)))				   
					{
						using (FileStream stream = new FileStream(GetTableVarFileName(tableVar), FileMode.Open, FileAccess.Read))
						{
							NativeTable nativeTable = Tables[tableVar];
							while (stream.Position < stream.Length)
							{
								int length = StreamUtility.ReadInteger(stream);
								byte[] rowValue = new byte[length];
								stream.Read(rowValue, 0, rowValue.Length);
								IRow row = (IRow)DataValue.FromPhysical(process.ValueManager, nativeTable.RowType, rowValue, 0);
								try
								{
									nativeTable.Insert(process.ValueManager, row);
								}
								finally
								{
									row.Dispose();
								}
							}
						}
					}
				}
			}
		}
		
		public void TruncateTable(ServerProcess process, Schema.TableVar tableVar)
		{
			string tableVarFileName = GetTableVarFileName(tableVar);
			if (File.Exists(tableVarFileName))
				File.Delete(tableVarFileName);
			Tables[tableVar].Truncate(process.ValueManager);
			SimpleDeviceHeader header = _headers[tableVar];
			header.TimeStamp += 1;
			header.SaveTimeStamp = header.TimeStamp;
		}
		
		public void BeginUpdate(ServerProcess process, Schema.TableVar tableVar)
		{
			SimpleDeviceHeader header = _headers[tableVar];
			header.UpdateCount++;
		}
		
		public void EndUpdate(ServerProcess process, Schema.TableVar tableVar)
		{
			SimpleDeviceHeader header = _headers[tableVar];
			if (header.UpdateCount > 0)
			{
				header.UpdateCount--;
				if (header.UpdateCount == 0)
					SaveTable(process, tableVar);
			}
		}
	}
	
	public class SimpleDeviceSession : MemoryDeviceSession
	{		
		protected internal SimpleDeviceSession(SimpleDevice device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo){}
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}		

		public new SimpleDevice Device { get { return (SimpleDevice)base.Device; } }
		
		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			if ((planNode is BaseTableVarNode) || (planNode is OrderNode))
			{
				Schema.TableVar tableVar = null;
				if (planNode is BaseTableVarNode)
					tableVar = ((BaseTableVarNode)planNode).TableVar;
				else if (planNode is OrderNode)
					tableVar = ((BaseTableVarNode)planNode.Nodes[0]).TableVar;
				if (tableVar != null)
					Device.LoadTable(ServerProcess, tableVar);
			}
			object result = base.InternalExecute(program, planNode);
			if (planNode is CreateTableNode)
			{
				Schema.TableVar tableVar = ((CreateTableNode)planNode).Table;
				SimpleDeviceHeader header = new SimpleDeviceHeader(tableVar, 0);
				Device.Headers.Add(header);
				if (!ServerProcess.IsLoading() && ((Device.ReconcileMode & ReconcileMode.Command) != 0)) 
				{
					string fileName = Device.GetTableVarFileName(tableVar);
					if (File.Exists(fileName))
						File.Delete(fileName);
				}
			}
			else if (planNode is DropTableNode)
				Device.Headers.Remove(((DropTableNode)planNode).Table);
			return result;
		}
		
		protected override void InternalInsertRow(Program program, Schema.TableVar tableVar, IRow row, BitArray valueFlags)
		{
			base.InternalInsertRow(program, tableVar, row, valueFlags);
			Device.Headers[tableVar].TimeStamp += 1;
		}
		
		protected override void InternalUpdateRow(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			base.InternalUpdateRow(program, tableVar, oldRow, newRow, valueFlags);
			Device.Headers[tableVar].TimeStamp += 1;
		}
		
		protected override void InternalDeleteRow(Program program, Schema.TableVar tableVar, IRow row)
		{
			base.InternalDeleteRow(program, tableVar, row);
			Device.Headers[tableVar].TimeStamp += 1;
		}

		protected override void InternalCommitTransaction()
		{
			base.InternalCommitTransaction();
			if ((Transactions.Count == 1) && Device.AutoSave)
			{
				HybridDictionary tables = new HybridDictionary();
				Exception lastException = null;
				foreach (Operation operation in Transactions.CurrentTransaction().Operations)
				{
					if (!(tables.Contains(operation.TableVar)))
					{
						tables.Add(operation.TableVar, null);
						try
						{
							Device.SaveTable(ServerProcess, operation.TableVar);
						}
						catch (Exception exception)
						{
							lastException = exception;
						}
					}
				}
				if (lastException != null)
					throw lastException;
			}
		}
	}

	public class SimpleDeviceHeader : System.Object
	{
		public SimpleDeviceHeader(Schema.TableVar tableVar, long timeStamp) : base()
		{
			_tableVar = tableVar;
			TimeStamp = timeStamp;
			SaveTimeStamp = timeStamp;
		}
		
		private Schema.TableVar _tableVar;
		public Schema.TableVar TableVar { get { return _tableVar; } }
		
		public long TimeStamp;
		
		public long SaveTimeStamp = Int64.MinValue;

		public int UpdateCount = 0;
	}
	
	public class SimpleDeviceHeaders : HashtableList<Schema.TableVar, SimpleDeviceHeader>
	{		
		public SimpleDeviceHeaders() : base(){}
		
		public override int Add(object tempValue)
		{
			if (tempValue is SimpleDeviceHeader)
				Add((SimpleDeviceHeader)tempValue);
			return IndexOf(tempValue);
		}
		
		public void Add(SimpleDeviceHeader header)
		{
			Add(header.TableVar, header);
		}
	}
}

