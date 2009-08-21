/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Libraries.System.Internet
{
	// operator FTPDownloadBinary(AURL : String) : Binary
	// operator FTPDownloadBinary(AURL : String, AUser : String, APassword : String) : Binary
	public class FTPDownloadBinaryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create((string)AArguments[0]);
			if (AArguments.Length > 1)
				LRequest.Credentials = new NetworkCredential((string)AArguments[1], (string)AArguments[2]);
			
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			// Details: The error: "The remote server returned an error: 227 Entering Passive Mode (208,186,252,59,8,103)"
			
			FtpWebResponse LResponse = (FtpWebResponse)LRequest.GetResponse();
			Stream LResponseStream = LResponse.GetResponseStream();
			try
			{
				MemoryStream LCopyStream = new MemoryStream((int)LResponse.ContentLength > 0 ? (int)LResponse.ContentLength : 1024);
				StreamUtility.CopyStream(LResponseStream, LCopyStream);
				byte[] LData = new byte[LCopyStream.Length];
				Buffer.BlockCopy(LCopyStream.GetBuffer(), 0, LData, 0, LData.Length);
				return LData;
			}
			finally
			{
				LResponseStream.Close();
			}
		}
	}

	// operator FTPDownloadText(AURL : String) : String
	// operator FTPDownloadText(AURL : String, AUser : String, APassword : String) : String
	public class FTPDownloadTextNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create((string)AArguments[0]);
			if (AArguments.Length > 1)
				LRequest.Credentials = new NetworkCredential((string)AArguments[1], (string)AArguments[2]);
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			FtpWebResponse LResponse = (FtpWebResponse)LRequest.GetResponse();
			Stream LResponseStream = LResponse.GetResponseStream();
			try
			{
				StreamReader LReader = new StreamReader(LResponseStream);
				return LReader.ReadToEnd();
			}
			finally
			{
				LResponseStream.Close();
			}
		}
	}

	// operator FTPUploadBinary(AURL : String, AData : Binary)
	// operator FTPUploadBinary(AURL : String, AData : Binary, AUser : String, APassword : String)
	// operator FTPUploadText(AURL : String, AData : String)
	// operator FTPUploadText(AURL : String, AData : String, AUser : String, APassword : String)
	public class FTPUploadNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create((string)AArguments[0]);
			if (AArguments.Length > 2)
				LRequest.Credentials = new NetworkCredential((string)AArguments[2], (string)AArguments[3]);
			LRequest.Method = WebRequestMethods.Ftp.UploadFile;
			LRequest.Proxy = null;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			Stream LRequestStream = LRequest.GetRequestStream();
			try
			{
				if (Operator.Operands[1].DataType.Is(AProcess.DataTypes.SystemString))
					using (StreamWriter LWriter = new StreamWriter(LRequestStream))
						LWriter.Write((string)AArguments[1]);
				else
				{
					byte[] LValue = AArguments[1] is byte[] ? (byte[])AArguments[1] : new Scalar(AProcess, (Schema.IScalarType)Operator.Operands[1].DataType, AArguments[1]).AsByteArray;
					LRequestStream.Write(LValue, 0, LValue.Length);
				}
				((FtpWebResponse)LRequest.GetResponse()).Close();
			}
			finally
			{
				LRequestStream.Close();
			}
			return null;
		}
	}

	// TODO: Add a detailed list that includes types, sizes, dates and such.  I think this requires parsing Windows and Unix style directory listings.

	// operator FTPList(AURL : String) : table { Name : String }
	// operator FTPList(AURL : String, AUser : String, APassword : String) : table { Name : String }
	public class FTPListNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Name", APlan.DataTypes.SystemString));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Name"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			string LUser = "";
			string LPassword = "";
			if (Nodes.Count > 1)
			{
				LUser = (string)Nodes[1].Execute(AProcess);
				LPassword = (string)Nodes[2].Execute(AProcess);
			}
			string LListing = GetDirectoryListing((string)Nodes[0].Execute(AProcess), LUser, LPassword);

			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					int LOffset = 0;
					while (LOffset < LListing.Length)
					{
						int LNextOffset = LListing.IndexOf('\n', LOffset + 1);
						if (LNextOffset < 0)
							LNextOffset = LListing.Length;
						LRow[0] = LListing.Substring(LOffset, LNextOffset - LOffset).Trim(new char[] { '\r', '\n', ' ' });
						LOffset = LNextOffset;

						if (((string)LRow[0]).Trim() != "")
							try
							{
								LResult.Insert(LRow);
							}
							catch (IndexException)
							{
								// Ignore duplicate keys
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

		internal static string GetDirectoryListing(string AURL, string AUser, string APassword)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create(AURL);
			if (AUser != "")
				LRequest.Credentials = new NetworkCredential(AUser, APassword);
			LRequest.Method = WebRequestMethods.Ftp.ListDirectory;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			FtpWebResponse LResponse = (FtpWebResponse)LRequest.GetResponse();
			Stream LResponseStream = LResponse.GetResponseStream();
			try
			{
				StreamReader LReader = new StreamReader(LResponseStream);
				return LReader.ReadToEnd();
			}
			finally
			{
				LResponseStream.Close();
			}
		}
	}

	// operator FTPRename(AURL : String, ANewName : String)
	// operator FTPRename(AURL : String, ANewName : String, AUser : String, APassword : String)
	public class FTPRenameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create((string)AArguments[0]);
			if (AArguments.Length > 2)
				LRequest.Credentials = new NetworkCredential((string)AArguments[2], (string)AArguments[3]);
			LRequest.Method = WebRequestMethods.Ftp.Rename;
			LRequest.RenameTo = (string)AArguments[1];
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			((FtpWebResponse)LRequest.GetResponse()).Close();
			return null;
		}
	}

	// operator FTPDeleteFile(AURL : String)
	// operator FTPDeleteFile(AURL : String, AUser : String, APassword : String)
	public class FTPDeleteFileNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length > 1)
				FTPHelper.CallSimpleFTP((string)AArguments[0], WebRequestMethods.Ftp.DeleteFile, (string)AArguments[1], (string)AArguments[2]);
			else
				FTPHelper.CallSimpleFTP((string)AArguments[0], WebRequestMethods.Ftp.DeleteFile);
			return null;
		}
	}

	// operator FTPMakeDirectory(AURL : String)
	// operator FTPMakeDirectory(AURL : String, AUser : String, APassword : String)
	public class FTPMakeDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length > 1)
				FTPHelper.CallSimpleFTP((string)AArguments[0], WebRequestMethods.Ftp.MakeDirectory, (string)AArguments[1], (string)AArguments[2]);
			else
				FTPHelper.CallSimpleFTP((string)AArguments[0], WebRequestMethods.Ftp.MakeDirectory);
			return null;
		}
	}

	// operator FTPRemoveDirectory(AURL : String)
	// operator FTPRemoveDirectory(AURL : String, AUser : String, APassword : String)
	public class FTPRemoveDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length > 1)
				FTPHelper.CallSimpleFTP((string)AArguments[0], WebRequestMethods.Ftp.RemoveDirectory, (string)AArguments[1], (string)AArguments[2]);
			else
				FTPHelper.CallSimpleFTP((string)AArguments[0], WebRequestMethods.Ftp.RemoveDirectory);
			return null;
		}
	}

	internal sealed class FTPHelper
	{
		public static void CallSimpleFTP(string AURL, string AMethod)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create(AURL);
			LRequest.Method = AMethod;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			((FtpWebResponse)LRequest.GetResponse()).Close();
		}

		public static void CallSimpleFTP(string AURL, string AMethod, string AUserName, string APassword)
		{
			FtpWebRequest LRequest = (FtpWebRequest)WebRequest.Create(AURL);
			LRequest.Credentials = new NetworkCredential(AUserName, APassword);
			LRequest.Method = AMethod;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			LRequest.UsePassive = false;
			((FtpWebResponse)LRequest.GetResponse()).Close();
		}
	}
}
