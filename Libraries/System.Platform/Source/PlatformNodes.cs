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

namespace Alphora.Dataphor.Libraries.System.Platform
{
	// operator FileExists(const AFileName : FileName) : Boolean
	public class FileExistsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, File.Exists(AArguments[0].Value.AsString)));
		}
	}

	// operator DeleteFile(const AFileName : FileName)
	public class DeleteFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.Delete(AArguments[0].Value.AsString);
			return null;
		}
	}

	// operator CopyFile(const ASourceFileName : FileName, const ATargetFileName : FileName)
	// operator CopyFile(const ASourceFileName : FileName, const ATargetFileName : FileName, const AOverwrite : Boolean)
	public class CopyFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.Copy(AArguments[0].Value.AsString, AArguments[1].Value.AsString, AArguments.Length == 3 ? AArguments[2].Value.AsBoolean : false);
			return null;
		}
	}
	
	// operator CreateFile(const AFileName : FileName)
	// operator CreateTextFile(const AFileName : FileName, const AData : String)
	// operator CreateBinaryFile(const AFileName : FileName, const AData : Binary)
	public class CreateFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (FileStream LFile = File.Create(AArguments[0].Value.AsString))
			{	
				if (AArguments.Length == 2)
					if (AArguments[1].DataType.Is(AProcess.DataTypes.SystemString))
					{
						using (StreamWriter LWriter = new StreamWriter(LFile))
						{
							LWriter.Write(AArguments[1].Value.AsString);
						}
					}
					else
					{
						byte[] LValue = AArguments[1].Value.AsByteArray;
						LFile.Write(LValue, 0, LValue.Length);
					}
			}
			return null;
		}
	}
	
	// operator AppendTextFile(const AFileName : FileName, const AData : String)
	// operator AppendBinaryFile(const AFileName : FileName, const AData : Binary)
	public class AppendFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (FileStream LFile = File.Open(AArguments[0].Value.AsString, FileMode.Append, FileAccess.Write, FileShare.Read))
			{
				if (AArguments[1].DataType.Is(AProcess.DataTypes.SystemString))
				{
					using (StreamWriter LWriter = new StreamWriter(LFile))
					{
						LWriter.Write(AArguments[1].Value.AsString);
					}
				}
				else
				{
					byte[] LValue = AArguments[1].Value.AsByteArray;
					LFile.Write(LValue, 0, LValue.Length);
				}
			}
			return null;
		}
	}
	
	// operator LoadTextFile(const AFileName : FileName) : String
	public class LoadTextFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (StreamReader LReader = File.OpenText(AArguments[0].Value.AsString))
			{
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LReader.ReadToEnd()));
			}
		}
	}
	
	// operator LoadBinaryFile(const AFileName : FileName) : Binary
	public class LoadBinaryFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (FileStream LFile = File.OpenRead(AArguments[0].Value.AsString))
			{
				byte[] LValue = new byte[LFile.Length];
				LFile.Read(LValue, 0, LValue.Length);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LValue));
			}
		}
	}
	
	// operator SaveTextFile(const AFileName : FileName, const AData : String)
	// operator SaveBinaryFile(const AFileName : FileName, const AData : Binary)
	public class SaveFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (FileStream LFile = File.Open(AArguments[0].Value.AsString, FileMode.Truncate, FileAccess.Write, FileShare.Read))
			{
				if (AArguments[1].DataType.Is(AProcess.DataTypes.SystemString))
				{
					using (StreamWriter LWriter = new StreamWriter(LFile))
					{
						LWriter.Write(AArguments[1].Value.AsString);
					}
				}
				else
				{
					byte[] LValue = AArguments[1].Value.AsByteArray;
					LFile.Write(LValue, 0, LValue.Length);
				}
			}
			return null;
		}
	}
	
	// operator MoveFile(const ASourceFileName : FileName, const ATargetFileName : FileName)
	public class MoveFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.Move(AArguments[0].Value.AsString, AArguments[1].Value.AsString);
			return null;
		}
	}
	
	// operator DirectoryExists(const ADirectoryName : FileName) : Boolean
	public class DirectoryExistsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Directory.Exists(AArguments[0].Value.AsString)));
		}
	}
	
	// operator GetFileName(const AFileName : FileName) : FileName
	public class GetFileNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Path.GetFileName(AArguments[0].Value.AsString)));
		}
	}
	
	// operator GetDirectoryName(const AFileName : FileName) : FileName
	public class GetDirectoryNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Path.GetDirectoryName(AArguments[0].Value.AsString)));
		}
	}
	
	// operator GetFileExtension(const AFileName : FileName) : FileName
	public class GetFileExtensionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Path.GetExtension(AArguments[0].Value.AsString)));
		}
	}
	
	// operator ChangeFileExtension(const AFileName : FileName, const AExtension : FileName) : FileName
	public class ChangeFileExtensionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Path.ChangeExtension(AArguments[0].Value.AsString, AArguments[1].Value.AsString)));
		}
	}
	
	// operator FileNameHasExtension(const AFileName : FileName) : Boolean
	public class FileNameHasExtensionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Path.HasExtension(AArguments[0].Value.AsString)));
		}
	}
	
	// operator CombinePath(const APath : FileName, const ASubPath : FileName) : FileName
	public class CombinePathNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Path.Combine(AArguments[0].Value.AsString, AArguments[1].Value.AsString)));
		}
	}
	
	// operator CreateDirectory(const ADirectoryName : FileName)
	public class CreateDirectoryNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Directory.CreateDirectory(AArguments[0].Value.AsString);
			return null;
		}
	}
	
	// operator DeleteDirectory(const ADirectoryName : FileName)
	public class DeleteDirectoryNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Directory.Delete(AArguments[0].Value.AsString);
			return null;
		}
	}
	
	// operator MoveDirectory(const ASourceDirectoryName : FileName, const ATargetDirectoryName : FileName)
	public class MoveDirectoryNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Directory.Move(AArguments[0].Value.AsString, AArguments[1].Value.AsString);
			return null;
		}
	}
	
	// operator GetAttributes(const APath : FileName) : Integer
	public class GetAttributesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, File.GetAttributes(AArguments[0].Value.AsString)));
		}
	}
	
	// operator SetAttributes(const APath : FileName, const AAttributes : Integer);
	public class SetAttributesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.SetAttributes(AArguments[0].Value.AsString, (FileAttributes)AArguments[1].Value.AsInt32);
			return null;
		}
	}
	
	// operator GetCreationTime(const APath : FileName) : DateTime;
	public class GetCreationTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, File.GetCreationTime(AArguments[0].Value.AsString)));
		}
	}
	
	// operator SetCreationTime(const APath : FileName, const ADateTime : DateTime);
	public class SetCreationTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.SetCreationTime(AArguments[0].Value.AsString, AArguments[1].Value.AsDateTime);
			return null;
		}
	}
	
	// operator GetLastWriteTime(const APath : FileName) : DateTime;
	public class GetLastWriteTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, File.GetLastWriteTime(AArguments[0].Value.AsString)));
		}
	}
	
	// operator SetLastWriteTime(const APath : FileName, const ADateTime : DateTime);
	public class SetLastWriteTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.SetLastWriteTime(AArguments[0].Value.AsString, AArguments[1].Value.AsDateTime);
			return null;
		}
	}
	
	// operator GetLastAccessTime(const APath : FileName) : DateTime;
	public class GetLastAccessTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, File.GetLastAccessTime(AArguments[0].Value.AsString)));
		}
	}
	
	// operator SetLastAccessTime(const APath : FileName, const ADateTime : DateTime);
	public class SetLastAccessTimeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			File.SetLastAccessTime(AArguments[0].Value.AsString, AArguments[1].Value.AsDateTime);
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
		
		public override DataVar InternalExecute(ServerProcess AProcess)
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
						LRow[0].AsString = LDrives[LIndex];
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
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
		
		public override DataVar InternalExecute(ServerProcess AProcess)
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
					string[] LDirectories = Directory.GetDirectories(Nodes[0].Execute(AProcess).Value.AsString, Nodes.Count == 2 ? Nodes[1].Execute(AProcess).Value.AsString : "*");
					for (int LIndex = 0; LIndex < LDirectories.Length; LIndex++)
					{
						LRow[0].AsString = LDirectories[LIndex];
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
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
		
		public override DataVar InternalExecute(ServerProcess AProcess)
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
					string[] LFiles = Directory.GetFiles(Nodes[0].Execute(AProcess).Value.AsString, Nodes.Count == 2 ? Nodes[1].Execute(AProcess).Value.AsString : "*");
					for (int LIndex = 0; LIndex < LFiles.Length; LIndex++)
					{
						LRow[0].AsString = LFiles[LIndex];
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (Process LProcess = new Process())
			{
				LProcess.StartInfo.FileName = AArguments[0].Value.AsString;
				LProcess.StartInfo.Arguments = AArguments.Length > 1 ? AArguments[1].Value.AsString : String.Empty;
				LProcess.StartInfo.UseShellExecute = false;
				bool LRedirectOutput = true;
				bool LRedirectErrors = true;
				if (AArguments.Length > 2)
				{
					Row LSettings = (Row)AArguments[2].Value;
					if (LSettings != null)
					{
						if (LSettings.HasValue("WorkingDirectory"))
							LProcess.StartInfo.WorkingDirectory = LSettings["WorkingDirectory"].AsString;
						if (LSettings.HasValue("NoWindow"))
							LProcess.StartInfo.CreateNoWindow = LSettings["NoWindow"].AsBoolean;
						if (LSettings.HasValue("WindowStyle"))
							LProcess.StartInfo.WindowStyle = (ProcessWindowStyle)LSettings["WindowStyle"].AsInt32;	//Enum.Parse(typeof(ProcessWindowStyle), 
						if (LSettings.HasValue("RedirectOutput"))
							LRedirectOutput = LSettings["RedirectOutput"].AsBoolean;
						if (LSettings.HasValue("RedirectErrors"))
							LRedirectErrors = LSettings["RedirectErrors"].AsBoolean;
					}
					if (AArguments.Length > 3)
					{
						LSettings = (Row)AArguments[3].Value;
						if (LSettings != null)
						{
							if (LSettings.HasValue("UserName"))
								LProcess.StartInfo.UserName = LSettings["UserName"].AsString;
							if (LSettings.HasValue("Password"))
							{
								Security.SecureString LPassword = new Security.SecureString();
								foreach (char LChar in LSettings["Password"].AsString)
									LPassword.AppendChar(LChar);
								LPassword.MakeReadOnly();
								LProcess.StartInfo.Password = LPassword;
							}
							if (LSettings.HasValue("Domain"))
								LProcess.StartInfo.Domain = LSettings["Domain"].AsString;
							if (LSettings.HasValue("LoadUserProfile"))
								LProcess.StartInfo.LoadUserProfile = LSettings["LoadUserProfile"].AsBoolean;
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
					LRow["Output"].AsString = FOutput.ToString();
				else
					LRow.ClearValue("Output");
				if (((Schema.IRowType)FDataType).Columns.ContainsName("Errors"))
				{
					if (LRedirectErrors)
						LRow["Errors"].AsString = FErrors.ToString();
					else
						LRow.ClearValue("Errors");
				}
				LRow["ExitCode"].AsInt32 = LProcess.ExitCode;
				
				return new DataVar(FDataType, LRow);
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Environment.CurrentDirectory = AArguments[0].Value.AsString;
			return null;
		}
    }

	// operator GetCurrentDirectory() : String
    public class GetCurrentDirectoryNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Environment.CurrentDirectory));
		}
    }
}
