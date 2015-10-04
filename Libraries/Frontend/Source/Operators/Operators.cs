/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Xml;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;

using Alphora.Dataphor.Frontend.Server.Device;	
using Alphora.Dataphor.Frontend.Server.Derivation;
using Alphora.Dataphor.Frontend.Server.Elaboration;
using Alphora.Dataphor.Frontend.Server.Structuring;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.Frontend.Server
{
	// operator Frontend.Create(const ALibraryName : Name, const AName : Name, const ADocumentType : String);
	public class CreateNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			device.CreateDocument(program, (string)arguments[0], (string)arguments[1], (string)arguments[2], true);
			return null;
		}
	}
	
	// operator Frontend.Delete(const ALibraryName : Name, const AName : Name);
	public class DeleteNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			device.DeleteDocument(program, (string)arguments[0], (string)arguments[1], true);
			return null;
		}
	}
	
	// operator Frontend.Rename(const AOldLibraryName : Name, const AOldName : Name, const ANewLibraryName : Name, const ANewName : Name);
	public class RenameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			device.RenameDocument
			(
				program, 
				(string)arguments[0], 
				(string)arguments[1],
				(string)arguments[2],
				(string)arguments[3],
				true
			);
			return null;
		}
	}
	
	// operator Frontend.Exists(const AOldLibraryName : Name, const AOldName : Name, const ANewLibraryName : Name, const ANewName : Name);
	public class ExistsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			return device.HasDocument(program, (string)arguments[0], (string)arguments[1]);
		}
	}
	
	// operator Frontend.Load(const ALibraryName : Name, const AName : Name) : String;
	public class LoadNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(program).LoadDocument
				(
					program, 
					(string)arguments[0], 
					(string)arguments[1]
				);
		}
	}

	// operator LoadAndProcess(const ALibraryName : Name, const AName : Name) : String
	public class LoadAndProcessNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(program).LoadAndProcessDocument
				(
					program, 
					(string)arguments[0], 
					(string)arguments[1]
				);
		}
	}
	
	// create operator Merge(const AForm : String, const ADelta : String) : String
	public class MergeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(program).Merge
				(
					(string)arguments[0], 
					(string)arguments[1]
				);
		}
	}

	// operator Difference(const AForm : String, const ADelta : String) : String
	public class DifferenceNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(program).Difference
				(
					(string)arguments[0], 
					(string)arguments[1]
				);
		}
	}

	// operator LoadCustomization(const ADilxDocument : String) : String
	public class LoadCustomizationNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(program).LoadCustomization
				(
					program, 
					(string)arguments[0]
				);
		}
	}
	
	// operator Frontend.LoadBinary(const ALibraryName : Name, const AName : Name) : Binary;
	public class LoadBinaryNode : InstructionNode
	{
		public const int NativeSizeThreshold = 4096;

		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice frontendDevice = FrontendDevice.GetFrontendDevice(program);
			MemoryStream data = frontendDevice.LoadBinary(program, (string)arguments[0], (string)arguments[1]);

			if (data.Length > NativeSizeThreshold)
				return frontendDevice.RegisterBinary(program, data);
			else
				return data.GetBuffer();
		}
	}

	// operator Frontend.Image(const ALibraryName : Name, const AName : Name) : Graphic;
	public class ImageNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice frontendDevice = FrontendDevice.GetFrontendDevice(program);
			MemoryStream data = frontendDevice.LoadBinary(program, (string)arguments[0], (string)arguments[1]);

			if (data.Length > LoadBinaryNode.NativeSizeThreshold)
				return frontendDevice.RegisterBinary(program, data);
			else
				return data.GetBuffer();
		}
	}

	// operator Frontend.Save(const ALibraryName : Name, const AName : Name, const ADocument : Scalar);
	public class SaveNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			if (arguments[2] is string)
				device.SaveDocument(program, (string)arguments[0], (string)arguments[1], (string)arguments[2]);
			else
			{
				Stream stream = 
				(
					arguments[2] is byte[] 
						? new Scalar(program.ValueManager, program.DataTypes.SystemBinary, arguments[2])
						: new Scalar(program.ValueManager, program.DataTypes.SystemBinary, (StreamID)arguments[2])
				).OpenStream();
				try
				{
					device.SaveBinary(program, (string)arguments[0], (string)arguments[1], stream);
				}
				finally
				{
					stream.Close();
				}
			}
			return null;
		}
	}

	// operator GetDocumentType(const ALibraryName : Name, const AName : Name) : String
	public class GetDocumentTypeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			return device.GetDocumentType(program, (string)arguments[0], (string)arguments[1]);
		}
	}
	
	// operator Frontend.Derive(const APageType : PageType, const AQuery : String, const AMasterKeyNames : String, const ADetailKeyNames : String, const AElaborate : Boolean) : String;
	public class DeriveNode : InstructionNode
	{
		public const string DefaultPageType = "Browse";
		public const bool DefaultElaborate = true;

		public override object InternalExecute(Program program, object[] arguments)
		{
			string query = (string)arguments[0];
			string pageType = DefaultPageType;
			string masterKeyNames = String.Empty;
			string detailKeyNames = String.Empty;
			bool elaborate = DefaultElaborate;
			if (arguments.Length >= 2)
			{
				if (Operator.Operands[1].DataType.Equals(program.DataTypes.SystemString))
				{
					pageType = (string)arguments[1];
					if (arguments.Length == 3)
						elaborate = (bool)arguments[2];
					else if (arguments.Length >= 4)
					{
						masterKeyNames = (string)arguments[2];
						detailKeyNames = (string)arguments[3];
						if (arguments.Length == 5)
							elaborate = (bool)arguments[4];
					}
				}
				else
					elaborate = (bool)arguments[1];
			}
			FrontendServer server = FrontendServer.GetFrontendServer(program.ServerProcess.ServerSession.Server);
			FrontendSession session = server.GetFrontendSession(program.ServerProcess.ServerSession);
			DerivationSeed seed = new DerivationSeed(pageType, query, elaborate, masterKeyNames, detailKeyNames);

			XmlDocument document;
			if (session.UseDerivationCache)
			{
				lock (session.DerivationCache)
				{
					session.EnsureDerivationCacheConsistent();
					DerivationCacheItem item;
					if (!session.DerivationCache.TryGetValue(seed, out item))
					{
						document = BuildInterface(program, seed);
						session.DerivationCache.Add(seed, new DerivationCacheItem(document));
					}
					else
						document = item.Document;
				}
			}
			else
				document = BuildInterface(program, seed);
				
			return document.OuterXml;
		}	

		protected XmlDocument BuildInterface(Program program, DerivationSeed seed)
		{
			return new InterfaceBuilder(program, seed).Build();
		}
	}
	
	// operator ExecuteScript(const ADocumentName : Name);
	// operator ExecuteScript(const ALibraryName : Name, const ADocumentName : Name);
	public class ExecuteScriptNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string libraryName;
			string documentName;
			if (arguments.Length == 2)
			{
				libraryName = (string)arguments[0];
				documentName = (string)arguments[1];
			}
			else
			{
				libraryName = program.Plan.CurrentLibrary.Name;
				documentName = (string)arguments[0];
			}
			
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			
			string documentType = device.GetDocumentType(program, libraryName, documentName);

			QueryLanguage saveLanguage = program.ServerProcess.ProcessInfo.Language;

			if (documentType == "sql")
				program.ServerProcess.ProcessInfo.Language = QueryLanguage.RealSQL;

			try
			{
				SystemExecuteNode.ExecuteScript
				(
					program.ServerProcess,
					program, 
					this,
					device.LoadDocument
					(
						program, 
						libraryName,
						documentName
					),
					new DAE.Debug.DebugLocator("doc:" + libraryName + ":" + documentName, 1, 1)
				);
			}
			finally
			{
				if (documentType == "sql")
					program.ServerProcess.ProcessInfo.Language = saveLanguage;
			}

			return null;
		}
	}

	// operator Refresh(const ALibraryName : Name);
	public class RefreshNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice.GetFrontendDevice(program).RefreshDocuments(program, (string)arguments[0]);
			return null;
		}
	}

	// operator Copy(const ASourceLibrary : Name, const ASourceDocument : Name, const ATargetLibrary : Name, const ATargetDocument : Name);
	public class CopyNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			device.CopyDocument
			(
				program,
				(string)arguments[0],
				(string)arguments[1],
				(string)arguments[2],
				(string)arguments[3]
			);
			return null;
		}
	}

	// operator Move(const ASourceLibrary : Name, const ASourceDocument : Name, const ATargetLibrary : Name, const ATargetDocument : Name);
	public class MoveNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendDevice device = FrontendDevice.GetFrontendDevice(program);
			device.MoveDocument
			(
				program,
				(string)arguments[0],
				(string)arguments[1],
				(string)arguments[2],
				(string)arguments[3]
			);
			return null;
		}
	}

	// operator ClearDerivationCache();
	public class ClearDerivationCacheNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			FrontendServer.GetFrontendServer(program.ServerProcess.ServerSession.Server).GetFrontendSession(program.ServerProcess.ServerSession).ClearDerivationCache();
			return null;
		}
	}

	// operator GetCRC32(AValue : generic) : Integer
	public class GetCRC32Node : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			IDataValue tempValue = arguments[0] as IDataValue;
			if (tempValue == null)
				tempValue = DataValue.FromNative(program.ValueManager, arguments[0]);

			if (tempValue.IsPhysicalStreaming)
			{
				using (Stream stream = tempValue.OpenStream())
				{
					return (int)CRC32Utility.GetCRC32(stream);
				}
			}
			else
			{
				byte[] physical = new byte[tempValue.GetPhysicalSize(true)];
				tempValue.WriteToPhysical(physical, 0, true);
				return (int)CRC32Utility.GetCRC32(physical);
			}
		}
	}
	
	// operator GetLibraryFiles(const AEnvironment : String, const ALibraries : table { Library_Name : Name }) : table { Name : String, TimeStamp : DateTime, IsDotNetAssembly : Boolean, ShouldRegister : Boolean };
	public class GetLibraryFilesNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TimeStamp", plan.DataTypes.SystemDateTime));
			DataType.Columns.Add(new Schema.Column("IsDotNetAssembly", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("ShouldRegister", plan.DataTypes.SystemBoolean));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredFiles(Program program, string environment, Schema.Library library, Table table, Row row)
		{
			foreach (Schema.FileReference file in library.Files)
			{
				if (file.Environments.Contains(environment))
				{
					string fullFileName = ((DAE.Server.Server)program.ServerProcess.ServerSession.Server).GetFullFileName(library, file.FileName);
					row["Library_Name"] = library.Name;
					row["Name"] = file.FileName;
					row["TimeStamp"] = File.GetLastWriteTimeUtc(fullFileName);
					row["IsDotNetAssembly"] = FileUtility.IsAssembly(fullFileName);
					row["ShouldRegister"] = file.IsAssembly;
					if (!table.FindKey(row))
						table.Insert(row);
				}
			}
			
			foreach (Schema.LibraryReference localLibrary in library.Libraries)
				PopulateRequiredFiles(program, environment, program.Catalog.Libraries[localLibrary.Name], table, row);
		}
		
		public override object InternalExecute(Program program)
		{
			string environment = (string)Nodes[0].Execute(program);
			using (ITable libraries = (ITable)Nodes[1].Execute(program))
			{
				LocalTable result = new LocalTable(this, program);
				try
				{
					result.Open();

					// Populate the result
					using (Row row = new Row(program.ValueManager, result.DataType.RowType))
					{
						row.ValuesOwned = false;
						
						using (Row libraryRow = new Row(program.ValueManager, libraries.DataType.RowType))
						{
							while (libraries.Next())
							{
								libraries.Select(libraryRow);
								PopulateRequiredFiles(program, environment, program.Catalog.Libraries[(string)libraryRow["Library_Name"]], result, row);
							}
						}
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
	}
}
