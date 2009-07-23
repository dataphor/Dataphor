/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using Security = System.Security;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Language.D4;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.Libraries.System.Platform
{
	// operator FileExists(const AFileName : FileName) : Boolean
	public class FileExistsNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return File.Exists((string)AArguments[0]);
		}
	}

	// operator DeleteFile(const AFileName : FileName)
	public class DeleteFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.Delete((string)AArguments[0]);
			return null;
		}
	}

	// operator CopyFile(const ASourceFileName : FileName, const ATargetFileName : FileName)
	// operator CopyFile(const ASourceFileName : FileName, const ATargetFileName : FileName, const AOverwrite : Boolean)
	public class CopyFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.Copy((string)AArguments[0], (string)AArguments[1], AArguments.Length == 3 ? (bool)AArguments[2] : false);
			return null;
		}
	}
	
	// operator CreateFile(const AFileName : FileName)
	// operator CreateTextFile(const AFileName : FileName, const AData : String)
	// operator CreateBinaryFile(const AFileName : FileName, const AData : Binary)
	public class CreateFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			using (FileStream LFile = File.Create((string)AArguments[0]))
			{	
				if (AArguments.Length == 2)
					if (Operator.Operands[1].DataType.Is(AProcess.DataTypes.SystemString))
					{
						using (StreamWriter LWriter = new StreamWriter(LFile))
						{
							LWriter.Write((string)AArguments[1]);
						}
					}
					else
					{
						if (AArguments[1] is StreamID)
						{
							using (Stream LSourceStream = AProcess.StreamManager.Open((StreamID)AArguments[1], LockMode.Exclusive))
							{
								StreamUtility.CopyStream(LSourceStream, LFile);
							}
						}
						else
						{
							byte[] LValue = (byte[])AArguments[1];
							LFile.Write(LValue, 0, LValue.Length);
						}
					}
			}
			return null;
		}
	}
	
	// operator AppendTextFile(const AFileName : FileName, const AData : String)
	// operator AppendBinaryFile(const AFileName : FileName, const AData : Binary)
	public class AppendFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			using (FileStream LFile = File.Open((string)AArguments[0], FileMode.Append, FileAccess.Write, FileShare.Read))
			{
				if (Operator.Operands[1].DataType.Is(AProcess.DataTypes.SystemString))
				{
					using (StreamWriter LWriter = new StreamWriter(LFile))
					{
						LWriter.Write((string)AArguments[1]);
					}
				}
				else
				{
					if (AArguments[1] is StreamID)
					{
						using (Stream LSourceStream = AProcess.StreamManager.Open((StreamID)AArguments[1], LockMode.Exclusive))
						{
							StreamUtility.CopyStream(LSourceStream, LFile);
						}
					}
					else
					{
						byte[] LValue = (byte[])AArguments[1];
						LFile.Write(LValue, 0, LValue.Length);
					}
				}
			}
			return null;
		}
	}
	
	// operator LoadTextFile(const AFileName : FileName) : String
	public class LoadTextFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			using (StreamReader LReader = File.OpenText((string)AArguments[0]))
			{
				return LReader.ReadToEnd();
			}
		}
	}
	
	// operator LoadBinaryFile(const AFileName : FileName) : Binary
	public class LoadBinaryFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			using (FileStream LFile = File.OpenRead((string)AArguments[0]))
			{
				byte[] LValue = new byte[LFile.Length];
				LFile.Read(LValue, 0, LValue.Length);
				return LValue;
			}
		}
	}
	
	// operator SaveTextFile(const AFileName : FileName, const AData : String)
	// operator SaveBinaryFile(const AFileName : FileName, const AData : Binary)
	public class SaveFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			using (FileStream LFile = File.Open((string)AArguments[0], FileMode.Truncate, FileAccess.Write, FileShare.Read))
			{
				if (Operator.Operands[1].DataType.Is(AProcess.DataTypes.SystemString))
				{
					using (StreamWriter LWriter = new StreamWriter(LFile))
					{
						LWriter.Write((string)AArguments[1]);
					}
				}
				else
				{
					if (AArguments[1] is StreamID)
					{
						using (Stream LSourceStream = AProcess.StreamManager.Open((StreamID)AArguments[1], LockMode.Exclusive))
						{
							StreamUtility.CopyStream(LSourceStream, LFile);
						}
					}
					else
					{
						byte[] LValue = (byte[])AArguments[1];
						LFile.Write(LValue, 0, LValue.Length);
					}
				}
			}
			return null;
		}
	}
	
	// operator MoveFile(const ASourceFileName : FileName, const ATargetFileName : FileName)
	public class MoveFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.Move((string)AArguments[0], (string)AArguments[1]);
			return null;
		}
	}
	
	// operator DirectoryExists(const ADirectoryName : FileName) : Boolean
	public class DirectoryExistsNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Directory.Exists((string)AArguments[0]);
		}
	}
	
	// operator GetFileName(const AFileName : FileName) : FileName
	public class GetFileNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Path.GetFileName((string)AArguments[0]);
		}
	}
	
	// operator GetDirectoryName(const AFileName : FileName) : FileName
	public class GetDirectoryNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Path.GetDirectoryName((string)AArguments[0]);
		}
	}
	
	// operator GetFileExtension(const AFileName : FileName) : FileName
	public class GetFileExtensionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Path.GetExtension((string)AArguments[0]);
		}
	}
	
	// operator ChangeFileExtension(const AFileName : FileName, const AExtension : FileName) : FileName
	public class ChangeFileExtensionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Path.ChangeExtension((string)AArguments[0], (string)AArguments[1]);
		}
	}
	
	// operator FileNameHasExtension(const AFileName : FileName) : Boolean
	public class FileNameHasExtensionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Path.HasExtension((string)AArguments[0]);
		}
	}
	
	// operator CombinePath(const APath : FileName, const ASubPath : FileName) : FileName
	public class CombinePathNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Path.Combine((string)AArguments[0], (string)AArguments[1]);
		}
	}
	
	// operator CreateDirectory(const ADirectoryName : FileName)
	public class CreateDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Directory.CreateDirectory((string)AArguments[0]);
			return null;
		}
	}
	
	// operator DeleteDirectory(const ADirectoryName : FileName)
	public class DeleteDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Directory.Delete((string)AArguments[0]);
			return null;
		}
	}
	
	// operator MoveDirectory(const ASourceDirectoryName : FileName, const ATargetDirectoryName : FileName)
	public class MoveDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Directory.Move((string)AArguments[0], (string)AArguments[1]);
			return null;
		}
	}
	
	// operator GetAttributes(const APath : FileName) : Integer
	public class GetAttributesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return File.GetAttributes((string)AArguments[0]);
		}
	}
	
	// operator SetAttributes(const APath : FileName, const AAttributes : Integer);
	public class SetAttributesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.SetAttributes((string)AArguments[0], (FileAttributes)(int)AArguments[1]);
			return null;
		}
	}
	
	// operator GetCreationTime(const APath : FileName) : DateTime;
	public class GetCreationTimeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return File.GetCreationTime((string)AArguments[0]);
		}
	}
	
	// operator SetCreationTime(const APath : FileName, const ADateTime : DateTime);
	public class SetCreationTimeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.SetCreationTime((string)AArguments[0], ((DateTime)AArguments[1]));
			return null;
		}
	}
	
	// operator GetLastWriteTime(const APath : FileName) : DateTime;
	public class GetLastWriteTimeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return File.GetLastWriteTime((string)AArguments[0]);
		}
	}
	
	// operator SetLastWriteTime(const APath : FileName, const ADateTime : DateTime);
	public class SetLastWriteTimeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.SetLastWriteTime((string)AArguments[0], ((DateTime)AArguments[1]));
			return null;
		}
	}
	
	// operator GetLastAccessTime(const APath : FileName) : DateTime;
	public class GetLastAccessTimeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return File.GetLastAccessTime((string)AArguments[0]);
		}
	}
	
	// operator SetLastAccessTime(const APath : FileName, const ADateTime : DateTime);
	public class SetLastAccessTimeNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			File.SetLastAccessTime((string)AArguments[0], ((DateTime)AArguments[1]));
			return null;
		}
	}
	
	// operator ListDrives() : table { Path : FileName };
    public class ListDrivesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Path", (Schema.ScalarType)Compiler.ResolveCatalogIdentifier(APlan, "System.Platform.FileName", true)));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Path"]}));

			TableVar.DetermineRemotable(APlan.ServerProcess);
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
					string[] LDrives = Directory.GetLogicalDrives();
					for (int LIndex = 0; LIndex < LDrives.Length; LIndex++)
					{
						LRow[0] = LDrives[LIndex];
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

	// operator ListDirectories(const APath : FileName) : table { Path : FileName };	
	// operator ListDirectories(const APath : FileName, const AMask : FileName) : table { Path : FileName };
    public class ListDirectoriesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Path", (Schema.ScalarType)Compiler.ResolveCatalogIdentifier(APlan, "System.Platform.FileName", true)));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Path"]}));

			TableVar.DetermineRemotable(APlan.ServerProcess);
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
					string[] LDirectories = Directory.GetDirectories((string)Nodes[0].Execute(AProcess), Nodes.Count == 2 ? (string)Nodes[1].Execute(AProcess) : "*");
					for (int LIndex = 0; LIndex < LDirectories.Length; LIndex++)
					{
						LRow[0] = LDirectories[LIndex];
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
    
	// operator ListFiles(const APath : FileName) : table { Path : FileName };
	// operator ListFiles(const APath : FileName, const AMask : String) : table { Path : FileName };
    public class ListFilesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Path", (Schema.ScalarType)Compiler.ResolveCatalogIdentifier(APlan, "System.Platform.FileName", true)));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Path"]}));

			TableVar.DetermineRemotable(APlan.ServerProcess);
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
					string[] LFiles = Directory.GetFiles((string)Nodes[0].Execute(AProcess), Nodes.Count == 2 ? (string)Nodes[1].Execute(AProcess) : "*");
					for (int LIndex = 0; LIndex < LFiles.Length; LIndex++)
					{
						LRow[0] = LFiles[LIndex];
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

	// operator PlatformExecute(const AFileName : String) : row { ExitCode : Integer, Output : String, Errors : String };
	// operator PlatformExecute(const AFileName : String, const AArguments : String) : row { ExitCode : Integer, Output : String, Errors : String }; 
	// operator PlatformExecute(const AFileName : String, const AArguments : String, const ASettings : row { WorkingDirectory : String, NoWindow : Boolean, WindowStyle : Integer, RedirectOutput : Boolean, RedirectErrors : Boolean }) : row { ExitCode : Integer, Output : String, Errors : String };
	// operator PlatformExecute(const AFileName : String, const AArguments : String, const ASettings : row { WorkingDirectory : String, NoWindow : Boolean, WindowStyle : Integer, RedirectOutput : Boolean, RedirectErrors : Boolean }, const AStartAs : row { UserName : String, Password : String, Domain : String, LoadUserProfile : Boolean }) : row { ExitCode : Integer, Output : String, Errors : String };
    public class PlatformExecuteNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			using (Process LProcess = new Process())
			{
				LProcess.StartInfo.FileName = (string)AArguments[0];
				LProcess.StartInfo.Arguments = AArguments.Length > 1 ? (string)AArguments[1] : String.Empty;
				LProcess.StartInfo.UseShellExecute = false;
				bool LRedirectOutput = true;
				bool LRedirectErrors = true;
				if (AArguments.Length > 2)
				{
					Row LSettings = (Row)AArguments[2];
					if (LSettings != null)
					{
						if (LSettings.HasValue("WorkingDirectory"))
							LProcess.StartInfo.WorkingDirectory = (string)LSettings["WorkingDirectory"];
						if (LSettings.HasValue("NoWindow"))
							LProcess.StartInfo.CreateNoWindow = (bool)LSettings["NoWindow"];
						if (LSettings.HasValue("WindowStyle"))
							LProcess.StartInfo.WindowStyle = (ProcessWindowStyle)(int)LSettings["WindowStyle"];	//Enum.Parse(typeof(ProcessWindowStyle), 
						if (LSettings.HasValue("RedirectOutput"))
							LRedirectOutput = (bool)LSettings["RedirectOutput"];
						if (LSettings.HasValue("RedirectErrors"))
							LRedirectErrors = (bool)LSettings["RedirectErrors"];
					}
					if (AArguments.Length > 3)
					{
						LSettings = (Row)AArguments[3];
						if (LSettings != null)
						{
							if (LSettings.HasValue("UserName"))
								LProcess.StartInfo.UserName = (string)LSettings["UserName"];
							if (LSettings.HasValue("Password"))
							{
								Security.SecureString LPassword = new Security.SecureString();
								foreach (char LChar in (string)LSettings["Password"])
									LPassword.AppendChar(LChar);
								LPassword.MakeReadOnly();
								LProcess.StartInfo.Password = LPassword;
							}
							if (LSettings.HasValue("Domain"))
								LProcess.StartInfo.Domain = (string)LSettings["Domain"];
							if (LSettings.HasValue("LoadUserProfile"))
								LProcess.StartInfo.LoadUserProfile = (bool)LSettings["LoadUserProfile"];
						}
					}
				}
				LProcess.StartInfo.RedirectStandardOutput = LRedirectOutput;
				LProcess.StartInfo.RedirectStandardError = LRedirectErrors;
				if (LRedirectOutput)
				{
					FOutput = new StringBuilder();
					LProcess.OutputDataReceived += new DataReceivedEventHandler(ExecutedProcessOutputReceived);
				}
				if (LRedirectErrors)
				{
					FErrors = new StringBuilder();
					LProcess.ErrorDataReceived += new DataReceivedEventHandler(ExecutedProcessErrorsReceived);
				}
				LProcess.Start();
				if (LRedirectOutput)
					LProcess.BeginOutputReadLine();
				if (LRedirectErrors)
					LProcess.BeginErrorReadLine();
				LProcess.WaitForExit();

				Row LRow = new Row(AProcess, (Schema.IRowType)FDataType);
				if (LRedirectOutput)
					LRow["Output"] = FOutput.ToString();
				else
					LRow.ClearValue("Output");
				if (((Schema.IRowType)FDataType).Columns.ContainsName("Errors"))
				{
					if (LRedirectErrors)
						LRow["Errors"] = FErrors.ToString();
					else
						LRow.ClearValue("Errors");
				}
				LRow["ExitCode"] = LProcess.ExitCode;
				
				return LRow;
			}
		}

		private StringBuilder FErrors;

		void ExecutedProcessErrorsReceived(object ASender, DataReceivedEventArgs AArgs)
		{
			if (!String.IsNullOrEmpty(AArgs.Data))
				FErrors.AppendLine(AArgs.Data);
		}

		private StringBuilder FOutput;

		void ExecutedProcessOutputReceived(object ASender, DataReceivedEventArgs AArgs)
		{
			if (!String.IsNullOrEmpty(AArgs.Data))
				FOutput.AppendLine(AArgs.Data);
		}
    }

	// operator SetCurrentDirectory(const APath : String)
    public class SetCurrentDirectoryNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Environment.CurrentDirectory = (string)AArguments[0];
			return null;
		}
    }

	// operator GetCurrentDirectory() : String
    public class GetCurrentDirectoryNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return Environment.CurrentDirectory;
		}
    }
}
