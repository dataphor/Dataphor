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
		public override object InternalExecute(Program program, object[] arguments)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create((string)arguments[0]);
			if (arguments.Length > 1)
				request.Credentials = new NetworkCredential((string)arguments[1], (string)arguments[2]);
			
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			// Details: The error: "The remote server returned an error: 227 Entering Passive Mode (208,186,252,59,8,103)"
			
			FtpWebResponse response = (FtpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			try
			{
				MemoryStream copyStream = new MemoryStream((int)response.ContentLength > 0 ? (int)response.ContentLength : 1024);
				StreamUtility.CopyStream(responseStream, copyStream);
				byte[] data = new byte[copyStream.Length];
				Buffer.BlockCopy(copyStream.GetBuffer(), 0, data, 0, data.Length);
				return data;
			}
			finally
			{
				responseStream.Close();
			}
		}
	}

	// operator FTPDownloadText(AURL : String) : String
	// operator FTPDownloadText(AURL : String, AUser : String, APassword : String) : String
	public class FTPDownloadTextNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create((string)arguments[0]);
			if (arguments.Length > 1)
				request.Credentials = new NetworkCredential((string)arguments[1], (string)arguments[2]);
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			FtpWebResponse response = (FtpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			try
			{
				StreamReader reader = new StreamReader(responseStream);
				return reader.ReadToEnd();
			}
			finally
			{
				responseStream.Close();
			}
		}
	}

	// operator FTPUploadBinary(AURL : String, AData : Binary)
	// operator FTPUploadBinary(AURL : String, AData : Binary, AUser : String, APassword : String)
	// operator FTPUploadText(AURL : String, AData : String)
	// operator FTPUploadText(AURL : String, AData : String, AUser : String, APassword : String)
	public class FTPUploadNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create((string)arguments[0]);
			if (arguments.Length > 2)
				request.Credentials = new NetworkCredential((string)arguments[2], (string)arguments[3]);
			request.Method = WebRequestMethods.Ftp.UploadFile;
			request.Proxy = null;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			Stream requestStream = request.GetRequestStream();
			try
			{
				if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemString))
					using (StreamWriter writer = new StreamWriter(requestStream))
						writer.Write((string)arguments[1]);
				else
				{
					byte[] tempValue = arguments[1] is byte[] ? (byte[])arguments[1] : new Scalar(program.ValueManager, (Schema.IScalarType)Operator.Operands[1].DataType, arguments[1]).AsByteArray;
					requestStream.Write(tempValue, 0, tempValue.Length);
				}
				((FtpWebResponse)request.GetResponse()).Close();
			}
			finally
			{
				requestStream.Close();
			}
			return null;
		}
	}

	// TODO: Add a detailed list that includes types, sizes, dates and such.  I think this requires parsing Windows and Unix style directory listings.

	// operator FTPList(AURL : String) : table { Name : String }
	// operator FTPList(AURL : String, AUser : String, APassword : String) : table { Name : String }
	public class FTPListNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemString));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));

			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Name"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);

			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}

		public override object InternalExecute(Program program)
		{
			string user = "";
			string password = "";
			if (Nodes.Count > 1)
			{
				user = (string)Nodes[1].Execute(program);
				password = (string)Nodes[2].Execute(program);
			}
			string listing = GetDirectoryListing((string)Nodes[0].Execute(program), user, password);

			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					int offset = 0;
					while (offset < listing.Length)
					{
						int nextOffset = listing.IndexOf('\n', offset + 1);
						if (nextOffset < 0)
							nextOffset = listing.Length;
						row[0] = listing.Substring(offset, nextOffset - offset).Trim(new char[] { '\r', '\n', ' ' });
						offset = nextOffset;

						if (((string)row[0]).Trim() != "")
							try
							{
								result.Insert(row);
							}
							catch (IndexException)
							{
								// Ignore duplicate keys
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

		internal static string GetDirectoryListing(string uRL, string user, string password)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uRL);
			if (user != "")
				request.Credentials = new NetworkCredential(user, password);
			request.Method = WebRequestMethods.Ftp.ListDirectory;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			FtpWebResponse response = (FtpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			try
			{
				StreamReader reader = new StreamReader(responseStream);
				return reader.ReadToEnd();
			}
			finally
			{
				responseStream.Close();
			}
		}
	}

	// operator FTPRename(AURL : String, ANewName : String)
	// operator FTPRename(AURL : String, ANewName : String, AUser : String, APassword : String)
	public class FTPRenameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create((string)arguments[0]);
			if (arguments.Length > 2)
				request.Credentials = new NetworkCredential((string)arguments[2], (string)arguments[3]);
			request.Method = WebRequestMethods.Ftp.Rename;
			request.RenameTo = (string)arguments[1];
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			((FtpWebResponse)request.GetResponse()).Close();
			return null;
		}
	}

	// operator FTPDeleteFile(AURL : String)
	// operator FTPDeleteFile(AURL : String, AUser : String, APassword : String)
	public class FTPDeleteFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length > 1)
				FTPHelper.CallSimpleFTP((string)arguments[0], WebRequestMethods.Ftp.DeleteFile, (string)arguments[1], (string)arguments[2]);
			else
				FTPHelper.CallSimpleFTP((string)arguments[0], WebRequestMethods.Ftp.DeleteFile);
			return null;
		}
	}

	// operator FTPMakeDirectory(AURL : String)
	// operator FTPMakeDirectory(AURL : String, AUser : String, APassword : String)
	public class FTPMakeDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length > 1)
				FTPHelper.CallSimpleFTP((string)arguments[0], WebRequestMethods.Ftp.MakeDirectory, (string)arguments[1], (string)arguments[2]);
			else
				FTPHelper.CallSimpleFTP((string)arguments[0], WebRequestMethods.Ftp.MakeDirectory);
			return null;
		}
	}

	// operator FTPRemoveDirectory(AURL : String)
	// operator FTPRemoveDirectory(AURL : String, AUser : String, APassword : String)
	public class FTPRemoveDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length > 1)
				FTPHelper.CallSimpleFTP((string)arguments[0], WebRequestMethods.Ftp.RemoveDirectory, (string)arguments[1], (string)arguments[2]);
			else
				FTPHelper.CallSimpleFTP((string)arguments[0], WebRequestMethods.Ftp.RemoveDirectory);
			return null;
		}
	}

	internal sealed class FTPHelper
	{
		public static void CallSimpleFTP(string uRL, string method)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uRL);
			request.Method = method;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			((FtpWebResponse)request.GetResponse()).Close();
		}

		public static void CallSimpleFTP(string uRL, string method, string userName, string password)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uRL);
			request.Credentials = new NetworkCredential(userName, password);
			request.Method = method;
			// HACK: Apparently there is a defect in the framework that results in an error under certain timing if the following line isn't here:
			request.UsePassive = false;
			((FtpWebResponse)request.GetResponse()).Close();
		}
	}
}
