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
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SimpleDevice LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0], true) as SimpleDevice;
			if (LDevice == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1], true) as Schema.TableVar;
			if (LTableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			LDevice.SaveTable(AProgram.ServerProcess, LTableVar);
			return null;
		}
	}
	
	public class LoadTableNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SimpleDevice LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0], true) as SimpleDevice;
			if (LDevice == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1], true) as Schema.TableVar;
			if (LTableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			LDevice.LoadTable(AProgram.ServerProcess, LTableVar);
			return null;
		}
	}
	
	public class TruncateTableNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SimpleDevice LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0], true) as SimpleDevice;
			if (LDevice == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1], true) as Schema.TableVar;
			if (LTableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			LDevice.TruncateTable(AProgram.ServerProcess, LTableVar);
			return null;
		}
	}
	
	public class BeginUpdateNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SimpleDevice LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0], true) as SimpleDevice;
			if (LDevice == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1], true) as Schema.TableVar;
			if (LTableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			LDevice.BeginUpdate(AProgram.ServerProcess, LTableVar);
			return null;
		}
	}
	
	public class EndUpdateNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			SimpleDevice LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0], true) as SimpleDevice;
			if (LDevice == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.SimpleDeviceExpected);
			Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1], true) as Schema.TableVar;
			if (LTableVar == null)
				throw new SimpleDeviceException(SimpleDeviceException.Codes.TableVarExpected);
			LDevice.EndUpdate(AProgram.ServerProcess, LTableVar);
			return null;
		}
	}
	
	public class SimpleDevice : MemoryDevice
	{
		public const string CDataDirectoryName = "Data";
	
		public SimpleDevice(int AID, string AName) : base(AID, AName)
		{
		}
		
		protected override void InternalStart(ServerProcess AProcess)
		{
			if (String.IsNullOrEmpty(FDirectoryName))
				FDirectoryName = CDataDirectoryName;
			if (!Path.IsPathRooted(FDirectoryName))
				FDirectoryName = Path.Combine(((Server.Server)AProcess.ServerSession.Server).InstanceDirectory, FDirectoryName);
			if (!Directory.Exists(FDirectoryName))
				Directory.CreateDirectory(FDirectoryName);
			
			base.InternalStart(AProcess);
		}
		
		protected override void InternalStop(ServerProcess AProcess)
		{
			foreach (SimpleDeviceHeader LHeader in FHeaders.Values)
				if (LHeader.TimeStamp > LHeader.SaveTimeStamp)
					SaveTable(AProcess, LHeader.TableVar);
			base.InternalStop(AProcess);
		}
		
		// Session
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new SimpleDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		private SimpleDeviceHeaders FHeaders = new SimpleDeviceHeaders();
		public SimpleDeviceHeaders Headers { get { return FHeaders; } }

		private string FDirectoryName;
		public string DirectoryName
		{
			get { return FDirectoryName; }
			set 
			{
				CheckNotRunning();
				FDirectoryName = value;
			}
		}
		
		private bool FAutoSave = true;
		public bool AutoSave
		{
			get { return FAutoSave; }
			set { FAutoSave = value; }
		}
		
		internal string GetTableVarFileName(Schema.TableVar ATableVar)
		{
			return Path.Combine(FDirectoryName, String.Format("{0}.dat", ATableVar.Name));
		}
		
		public void SaveTable(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			SimpleDeviceHeader LHeader = FHeaders[ATableVar];
			if ((LHeader.UpdateCount == 0) && (LHeader.TimeStamp > LHeader.SaveTimeStamp))
			{
				Plan LPlan = new Plan(AProcess);
				try
				{
					LPlan.PushSecurityContext(new SecurityContext(ATableVar.Owner));
					try
					{
						Program LProgram = new Program(AProcess);
						LProgram.Code = Compiler.BindNode(LPlan, Compiler.EmitBaseTableVarNode(LPlan, ATableVar));
						LProgram.Start(null);
						try
						{
							Table LTable = LProgram.Code.Execute(LProgram) as Table;
							try
							{
								LTable.Open();
								Row LRow = new Row(LProgram.ValueManager, ATableVar.DataType.CreateRowType());
								try
								{
									string LFileName = GetTableVarFileName(ATableVar);
									FileUtility.EnsureWriteable(LFileName);
									using (FileStream LStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
									{
										while (LTable.Next())
										{
											LTable.Select(LRow);
											byte[] LRowValue = LRow.AsPhysical;
											StreamUtility.WriteInteger(LStream, LRowValue.Length);
											LStream.Write(LRowValue, 0, LRowValue.Length);
										}
									}
								}
								finally
								{
									LRow.Dispose();
								}
							}
							finally
							{
								LTable.Dispose();
							}
						}
						finally
						{
							LProgram.Stop(null);
						}
					}
					finally
					{
						LPlan.PopSecurityContext();
					}
				}
				finally
				{
					LPlan.Dispose();
				}
				LHeader.SaveTimeStamp = LHeader.TimeStamp;
			}
		}
		
		private Schema.Objects FLoadedTables = new Schema.Objects();
		public bool IsLoaded(Schema.TableVar ATableVar)
		{
			return FLoadedTables.Contains(ATableVar);
		}
		
		public void LoadTable(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			lock (FLoadedTables)
			{
				if (!IsLoaded(ATableVar))
				{
					FLoadedTables.Add(ATableVar);
					if (File.Exists(GetTableVarFileName(ATableVar)))				   
					{
						using (FileStream LStream = new FileStream(GetTableVarFileName(ATableVar), FileMode.Open, FileAccess.Read))
						{
							NativeTable LNativeTable = Tables[ATableVar];
							while (LStream.Position < LStream.Length)
							{
								int LLength = StreamUtility.ReadInteger(LStream);
								byte[] LRowValue = new byte[LLength];
								LStream.Read(LRowValue, 0, LRowValue.Length);
								Row LRow = (Row)DataValue.FromPhysical(AProcess.ValueManager, LNativeTable.RowType, LRowValue, 0);
								try
								{
									LNativeTable.Insert(AProcess.ValueManager, LRow);
								}
								finally
								{
									LRow.Dispose();
								}
							}
						}
					}
				}
			}
		}
		
		public void TruncateTable(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			string LTableVarFileName = GetTableVarFileName(ATableVar);
			if (File.Exists(LTableVarFileName))
				File.Delete(LTableVarFileName);
			Tables[ATableVar].Truncate(AProcess.ValueManager);
			SimpleDeviceHeader LHeader = FHeaders[ATableVar];
			LHeader.TimeStamp += 1;
			LHeader.SaveTimeStamp = LHeader.TimeStamp;
		}
		
		public void BeginUpdate(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			SimpleDeviceHeader LHeader = FHeaders[ATableVar];
			LHeader.UpdateCount++;
		}
		
		public void EndUpdate(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			SimpleDeviceHeader LHeader = FHeaders[ATableVar];
			if (LHeader.UpdateCount > 0)
			{
				LHeader.UpdateCount--;
				if (LHeader.UpdateCount == 0)
					SaveTable(AProcess, ATableVar);
			}
		}
	}
	
	public class SimpleDeviceSession : MemoryDeviceSession
	{		
		protected internal SimpleDeviceSession(SimpleDevice ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
		}		

		public new SimpleDevice Device { get { return (SimpleDevice)base.Device; } }
		
		protected override object InternalExecute(Program AProgram, Schema.DevicePlan ADevicePlan)
		{
			if ((ADevicePlan.Node is BaseTableVarNode) || (ADevicePlan.Node is OrderNode))
			{
				Schema.TableVar LTableVar = null;
				if (ADevicePlan.Node is BaseTableVarNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node).TableVar;
				else if (ADevicePlan.Node is OrderNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node.Nodes[0]).TableVar;
				if (LTableVar != null)
					Device.LoadTable(ServerProcess, LTableVar);
			}
			object LResult = base.InternalExecute(AProgram, ADevicePlan);
			if (ADevicePlan.Node is CreateTableNode)
			{
				Schema.TableVar LTableVar = ((CreateTableNode)ADevicePlan.Node).Table;
				SimpleDeviceHeader LHeader = new SimpleDeviceHeader(LTableVar, 0);
				Device.Headers.Add(LHeader);
				if (!ServerProcess.IsLoading() && ((Device.ReconcileMode & ReconcileMode.Command) != 0)) 
				{
					string LFileName = Device.GetTableVarFileName(LTableVar);
					if (File.Exists(LFileName))
						File.Delete(LFileName);
				}
			}
			else if (ADevicePlan.Node is DropTableNode)
				Device.Headers.Remove(((DropTableNode)ADevicePlan.Node).Table);
			return LResult;
		}
		
		protected override void InternalInsertRow(Program AProgram, Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags)
		{
			base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
			Device.Headers[ATableVar].TimeStamp += 1;
		}
		
		protected override void InternalUpdateRow(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			base.InternalUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
			Device.Headers[ATableVar].TimeStamp += 1;
		}
		
		protected override void InternalDeleteRow(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			base.InternalDeleteRow(AProgram, ATableVar, ARow);
			Device.Headers[ATableVar].TimeStamp += 1;
		}

		protected override void InternalCommitTransaction()
		{
			base.InternalCommitTransaction();
			if ((Transactions.Count == 1) && Device.AutoSave)
			{
				HybridDictionary LTables = new HybridDictionary();
				Exception LLastException = null;
				foreach (Operation LOperation in Transactions.CurrentTransaction().Operations)
				{
					if (!(LTables.Contains(LOperation.TableVar)))
					{
						LTables.Add(LOperation.TableVar, null);
						try
						{
							Device.SaveTable(ServerProcess, LOperation.TableVar);
						}
						catch (Exception LException)
						{
							LLastException = LException;
						}
					}
				}
				if (LLastException != null)
					throw LLastException;
			}
		}
	}

	public class SimpleDeviceHeader : System.Object
	{
		public SimpleDeviceHeader(Schema.TableVar ATableVar, long ATimeStamp) : base()
		{
			FTableVar = ATableVar;
			TimeStamp = ATimeStamp;
			SaveTimeStamp = ATimeStamp;
		}
		
		private Schema.TableVar FTableVar;
		public Schema.TableVar TableVar { get { return FTableVar; } }
		
		public long TimeStamp;
		
		public long SaveTimeStamp = Int64.MinValue;

		public int UpdateCount = 0;
	}
	
	public class SimpleDeviceHeaders : HashtableList<Schema.TableVar, SimpleDeviceHeader>
	{		
		public SimpleDeviceHeaders() : base(){}
		
		public override int Add(object AValue)
		{
			if (AValue is SimpleDeviceHeader)
				Add((SimpleDeviceHeader)AValue);
			return IndexOf(AValue);
		}
		
		public void Add(SimpleDeviceHeader AHeader)
		{
			Add(AHeader.TableVar, AHeader);
		}
	}
}

