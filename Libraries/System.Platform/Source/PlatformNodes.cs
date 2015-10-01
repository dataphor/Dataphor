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

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.Libraries.System.Platform
{
	// operator FileExists(const AFileName : FileName) : Boolean
	public class FileExistsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return File.Exists((string)arguments[0]);
		}
	}

	// operator DeleteFile(const AFileName : FileName)
	public class DeleteFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.Delete((string)arguments[0]);
			return null;
		}
	}

	// operator CopyFile(const ASourceFileName : FileName, const ATargetFileName : FileName)
	// operator CopyFile(const ASourceFileName : FileName, const ATargetFileName : FileName, const AOverwrite : Boolean)
	public class CopyFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.Copy((string)arguments[0], (string)arguments[1], arguments.Length == 3 ? (bool)arguments[2] : false);
			return null;
		}
	}
	
	// operator CreateFile(const AFileName : FileName)
	// operator CreateTextFile(const AFileName : FileName, const AData : String)
	// operator CreateBinaryFile(const AFileName : FileName, const AData : Binary)
	public class CreateFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (FileStream file = File.Create((string)arguments[0]))
			{	
				if (arguments.Length == 2)
					if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemString))
					{
						using (StreamWriter writer = new StreamWriter(file))
						{
							writer.Write((string)arguments[1]);
						}
					}
					else
					{
						if (arguments[1] is StreamID)
						{
							using (Stream sourceStream = program.StreamManager.Open((StreamID)arguments[1], LockMode.Exclusive))
							{
								StreamUtility.CopyStream(sourceStream, file);
							}
						}
						else
						{
							byte[] tempValue = (byte[])arguments[1];
							file.Write(tempValue, 0, tempValue.Length);
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
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (FileStream file = File.Open((string)arguments[0], FileMode.Append, FileAccess.Write, FileShare.Read))
			{
				if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemString))
				{
					using (StreamWriter writer = new StreamWriter(file))
					{
						writer.Write((string)arguments[1]);
					}
				}
				else
				{
					if (arguments[1] is StreamID)
					{
						using (Stream sourceStream = program.StreamManager.Open((StreamID)arguments[1], LockMode.Exclusive))
						{
							StreamUtility.CopyStream(sourceStream, file);
						}
					}
					else
					{
						byte[] tempValue = (byte[])arguments[1];
						file.Write(tempValue, 0, tempValue.Length);
					}
				}
			}
			return null;
		}
	}
	
	// operator LoadTextFile(const AFileName : FileName) : String
	public class LoadTextFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (StreamReader reader = File.OpenText((string)arguments[0]))
			{
				return reader.ReadToEnd();
			}
		}
	}
	
	// operator LoadBinaryFile(const AFileName : FileName) : Binary
	public class LoadBinaryFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (FileStream file = File.OpenRead((string)arguments[0]))
			{
				byte[] tempValue = new byte[file.Length];
				file.Read(tempValue, 0, tempValue.Length);
				return tempValue;
			}
		}
	}
	
	// operator SaveTextFile(const AFileName : FileName, const AData : String)
	// operator SaveBinaryFile(const AFileName : FileName, const AData : Binary)
	public class SaveFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (FileStream file = File.Open((string)arguments[0], FileMode.Truncate, FileAccess.Write, FileShare.Read))
			{
				if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemString))
				{
					using (StreamWriter writer = new StreamWriter(file))
					{
						writer.Write((string)arguments[1]);
					}
				}
				else
				{
					if (arguments[1] is StreamID)
					{
						using (Stream sourceStream = program.StreamManager.Open((StreamID)arguments[1], LockMode.Exclusive))
						{
							StreamUtility.CopyStream(sourceStream, file);
						}
					}
					else
					{
						byte[] tempValue = (byte[])arguments[1];
						file.Write(tempValue, 0, tempValue.Length);
					}
				}
			}
			return null;
		}
	}
	
	// operator MoveFile(const ASourceFileName : FileName, const ATargetFileName : FileName)
	public class MoveFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.Move((string)arguments[0], (string)arguments[1]);
			return null;
		}
	}
	
	// operator DirectoryExists(const ADirectoryName : FileName) : Boolean
	public class DirectoryExistsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Directory.Exists((string)arguments[0]);
		}
	}
	
	// operator GetFileName(const AFileName : FileName) : FileName
	public class GetFileNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Path.GetFileName((string)arguments[0]);
		}
	}
	
	// operator GetDirectoryName(const AFileName : FileName) : FileName
	public class GetDirectoryNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Path.GetDirectoryName((string)arguments[0]);
		}
	}
	
	// operator GetFileExtension(const AFileName : FileName) : FileName
	public class GetFileExtensionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Path.GetExtension((string)arguments[0]);
		}
	}
	
	// operator ChangeFileExtension(const AFileName : FileName, const AExtension : FileName) : FileName
	public class ChangeFileExtensionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Path.ChangeExtension((string)arguments[0], (string)arguments[1]);
		}
	}
	
	// operator FileNameHasExtension(const AFileName : FileName) : Boolean
	public class FileNameHasExtensionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Path.HasExtension((string)arguments[0]);
		}
	}
	
	// operator CombinePath(const APath : FileName, const ASubPath : FileName) : FileName
	public class CombinePathNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Path.Combine((string)arguments[0], (string)arguments[1]);
		}
	}
	
	// operator CreateDirectory(const ADirectoryName : FileName)
	public class CreateDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Directory.CreateDirectory((string)arguments[0]);
			return null;
		}
	}
	
	// operator DeleteDirectory(const ADirectoryName : FileName)
	public class DeleteDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Directory.Delete((string)arguments[0]);
			return null;
		}
	}
	
	// operator MoveDirectory(const ASourceDirectoryName : FileName, const ATargetDirectoryName : FileName)
	public class MoveDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Directory.Move((string)arguments[0], (string)arguments[1]);
			return null;
		}
	}
	
	// operator GetAttributes(const APath : FileName) : Integer
	public class GetAttributesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return File.GetAttributes((string)arguments[0]);
		}
	}
	
	// operator SetAttributes(const APath : FileName, const AAttributes : Integer);
	public class SetAttributesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.SetAttributes((string)arguments[0], (FileAttributes)(int)arguments[1]);
			return null;
		}
	}
	
	// operator GetCreationTime(const APath : FileName) : DateTime;
	public class GetCreationTimeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return File.GetCreationTime((string)arguments[0]);
		}
	}
	
	// operator SetCreationTime(const APath : FileName, const ADateTime : DateTime);
	public class SetCreationTimeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.SetCreationTime((string)arguments[0], ((DateTime)arguments[1]));
			return null;
		}
	}
	
	// operator GetLastWriteTime(const APath : FileName) : DateTime;
	public class GetLastWriteTimeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return File.GetLastWriteTime((string)arguments[0]);
		}
	}
	
	// operator SetLastWriteTime(const APath : FileName, const ADateTime : DateTime);
	public class SetLastWriteTimeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.SetLastWriteTime((string)arguments[0], ((DateTime)arguments[1]));
			return null;
		}
	}
	
	// operator GetLastAccessTime(const APath : FileName) : DateTime;
	public class GetLastAccessTimeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return File.GetLastAccessTime((string)arguments[0]);
		}
	}
	
	// operator SetLastAccessTime(const APath : FileName, const ADateTime : DateTime);
	public class SetLastAccessTimeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			File.SetLastAccessTime((string)arguments[0], ((DateTime)arguments[1]));
			return null;
		}
	}
	
	// operator ListDrives() : table { Path : FileName };
    public class ListDrivesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Path", (Schema.ScalarType)Compiler.ResolveCatalogIdentifier(plan, "System.Platform.FileName", true)));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Path"]}));

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
					string[] drives = Directory.GetLogicalDrives();
					for (int index = 0; index < drives.Length; index++)
					{
						row[0] = drives[index];
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

	// operator ListDirectories(const APath : FileName) : table { Path : FileName };	
	// operator ListDirectories(const APath : FileName, const AMask : FileName) : table { Path : FileName };
    public class ListDirectoriesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Path", (Schema.ScalarType)Compiler.ResolveCatalogIdentifier(plan, "System.Platform.FileName", true)));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Path"]}));

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
					string[] directories = Directory.GetDirectories((string)Nodes[0].Execute(program), Nodes.Count == 2 ? (string)Nodes[1].Execute(program) : "*");
					for (int index = 0; index < directories.Length; index++)
					{
						row[0] = directories[index];
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
    
	// operator ListFiles(const APath : FileName) : table { Path : FileName };
	// operator ListFiles(const APath : FileName, const AMask : String) : table { Path : FileName };
    public class ListFilesNode : TableNode
    {
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Path", (Schema.ScalarType)Compiler.ResolveCatalogIdentifier(plan, "System.Platform.FileName", true)));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Path"]}));

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
					string[] files = Directory.GetFiles((string)Nodes[0].Execute(program), Nodes.Count == 2 ? (string)Nodes[1].Execute(program) : "*");
					for (int index = 0; index < files.Length; index++)
					{
						row[0] = files[index];
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

	// operator PlatformExecute(const AFileName : String) : row { ExitCode : Integer, Output : String, Errors : String };
	// operator PlatformExecute(const AFileName : String, const AArguments : String) : row { ExitCode : Integer, Output : String, Errors : String }; 
	// operator PlatformExecute(const AFileName : String, const AArguments : String, const ASettings : row { WorkingDirectory : String, NoWindow : Boolean, WindowStyle : Integer, RedirectOutput : Boolean, RedirectErrors : Boolean }) : row { ExitCode : Integer, Output : String, Errors : String };
	// operator PlatformExecute(const AFileName : String, const AArguments : String, const ASettings : row { WorkingDirectory : String, NoWindow : Boolean, WindowStyle : Integer, RedirectOutput : Boolean, RedirectErrors : Boolean }, const AStartAs : row { UserName : String, Password : String, Domain : String, LoadUserProfile : Boolean }) : row { ExitCode : Integer, Output : String, Errors : String };
    public class PlatformExecuteNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (Process process = new Process())
			{
				process.StartInfo.FileName = (string)arguments[0];
				process.StartInfo.Arguments = arguments.Length > 1 ? (string)arguments[1] : String.Empty;
				process.StartInfo.UseShellExecute = false;
				bool redirectOutput = true;
				bool redirectErrors = true;
				if (arguments.Length > 2)
				{
					IRow settings = (IRow)arguments[2];
					if (settings != null)
					{
						if (settings.HasValue("WorkingDirectory"))
							process.StartInfo.WorkingDirectory = (string)settings["WorkingDirectory"];
						if (settings.HasValue("NoWindow"))
							process.StartInfo.CreateNoWindow = (bool)settings["NoWindow"];
						if (settings.HasValue("WindowStyle"))
							process.StartInfo.WindowStyle = (ProcessWindowStyle)(int)settings["WindowStyle"];	//Enum.Parse(typeof(ProcessWindowStyle), 
						if (settings.HasValue("RedirectOutput"))
							redirectOutput = (bool)settings["RedirectOutput"];
						if (settings.HasValue("RedirectErrors"))
							redirectErrors = (bool)settings["RedirectErrors"];
					}
					if (arguments.Length > 3)
					{
						settings = (IRow)arguments[3];
						if (settings != null)
						{
							if (settings.HasValue("UserName"))
								process.StartInfo.UserName = (string)settings["UserName"];
							if (settings.HasValue("Password"))
							{
								Security.SecureString password = new Security.SecureString();
								foreach (char charValue in (string)settings["Password"])
									password.AppendChar(charValue);
								password.MakeReadOnly();
								process.StartInfo.Password = password;
							}
							if (settings.HasValue("Domain"))
								process.StartInfo.Domain = (string)settings["Domain"];
							if (settings.HasValue("LoadUserProfile"))
								process.StartInfo.LoadUserProfile = (bool)settings["LoadUserProfile"];
						}
					}
				}
				process.StartInfo.RedirectStandardOutput = redirectOutput;
				process.StartInfo.RedirectStandardError = redirectErrors;
				if (redirectOutput)
				{
					_output = new StringBuilder();
					process.OutputDataReceived += new DataReceivedEventHandler(ExecutedProcessOutputReceived);
				}
				if (redirectErrors)
				{
					_errors = new StringBuilder();
					process.ErrorDataReceived += new DataReceivedEventHandler(ExecutedProcessErrorsReceived);
				}
				process.Start();
				if (redirectOutput)
					process.BeginOutputReadLine();
				if (redirectErrors)
					process.BeginErrorReadLine();
				process.WaitForExit();

				Row row = new Row(program.ValueManager, (Schema.IRowType)_dataType);
				if (redirectOutput)
					row["Output"] = _output.ToString();
				else
					row.ClearValue("Output");
				if (((Schema.IRowType)_dataType).Columns.ContainsName("Errors"))
				{
					if (redirectErrors)
						row["Errors"] = _errors.ToString();
					else
						row.ClearValue("Errors");
				}
				row["ExitCode"] = process.ExitCode;
				
				return row;
			}
		}

		private StringBuilder _errors;

		void ExecutedProcessErrorsReceived(object sender, DataReceivedEventArgs args)
		{
			if (!String.IsNullOrEmpty(args.Data))
				_errors.AppendLine(args.Data);
		}

		private StringBuilder _output;

		void ExecutedProcessOutputReceived(object sender, DataReceivedEventArgs args)
		{
			if (!String.IsNullOrEmpty(args.Data))
				_output.AppendLine(args.Data);
		}
    }

	// operator SetCurrentDirectory(const APath : String)
    public class SetCurrentDirectoryNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Environment.CurrentDirectory = (string)arguments[0];
			return null;
		}
    }

	// operator GetCurrentDirectory() : String
    public class GetCurrentDirectoryNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Environment.CurrentDirectory;
		}
    }
}
