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

namespace Alphora.Dataphor.Frontend.Server
{
	// operator Frontend.Create(const ALibraryName : Name, const AName : Name, const ADocumentType : String);
	public class CreateNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			LDevice.CreateDocument(AProgram, (string)AArguments[0], (string)AArguments[1], (string)AArguments[2], true);
			return null;
		}
	}
	
	// operator Frontend.Delete(const ALibraryName : Name, const AName : Name);
	public class DeleteNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			LDevice.DeleteDocument(AProgram, (string)AArguments[0], (string)AArguments[1], true);
			return null;
		}
	}
	
	// operator Frontend.Rename(const AOldLibraryName : Name, const AOldName : Name, const ANewLibraryName : Name, const ANewName : Name);
	public class RenameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			LDevice.RenameDocument
			(
				AProgram, 
				(string)AArguments[0], 
				(string)AArguments[1],
				(string)AArguments[2],
				(string)AArguments[3],
				true
			);
			return null;
		}
	}
	
	// operator Frontend.Exists(const AOldLibraryName : Name, const AOldName : Name, const ANewLibraryName : Name, const ANewName : Name);
	public class ExistsNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			return LDevice.HasDocument(AProgram, (string)AArguments[0], (string)AArguments[1]);
		}
	}
	
	// operator Frontend.Load(const ALibraryName : Name, const AName : Name) : String;
	public class LoadNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(AProgram).LoadDocument
				(
					AProgram, 
					(string)AArguments[0], 
					(string)AArguments[1]
				);
		}
	}

	// operator LoadAndProcess(const ALibraryName : Name, const AName : Name) : String
	public class LoadAndProcessNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(AProgram).LoadAndProcessDocument
				(
					AProgram, 
					(string)AArguments[0], 
					(string)AArguments[1]
				);
		}
	}
	
	// create operator Merge(const AForm : String, const ADelta : String) : String
	public class MergeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(AProgram).Merge
				(
					(string)AArguments[0], 
					(string)AArguments[1]
				);
		}
	}

	// operator Difference(const AForm : String, const ADelta : String) : String
	public class DifferenceNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(AProgram).Difference
				(
					(string)AArguments[0], 
					(string)AArguments[1]
				);
		}
	}

	// operator LoadCustomization(const ADilxDocument : String) : String
	public class LoadCustomizationNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return 
				FrontendDevice.GetFrontendDevice(AProgram).LoadCustomization
				(
					AProgram, 
					(string)AArguments[0]
				);
		}
	}
	
	// operator Frontend.LoadBinary(const ALibraryName : Name, const AName : Name) : Binary;
	public class LoadBinaryNode : InstructionNode
	{
		public const int CNativeSizeThreshold = 4096;

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LFrontendDevice = FrontendDevice.GetFrontendDevice(AProgram);
			MemoryStream LData = LFrontendDevice.LoadBinary(AProgram, (string)AArguments[0], (string)AArguments[1]);

			if (LData.Length > CNativeSizeThreshold)
				return LFrontendDevice.RegisterBinary(AProgram, LData);
			else
				return LData.GetBuffer();
		}
	}

	// operator Frontend.Image(const ALibraryName : Name, const AName : Name) : Graphic;
	public class ImageNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LFrontendDevice = FrontendDevice.GetFrontendDevice(AProgram);
			MemoryStream LData = LFrontendDevice.LoadBinary(AProgram, (string)AArguments[0], (string)AArguments[1]);

			if (LData.Length > LoadBinaryNode.CNativeSizeThreshold)
				return LFrontendDevice.RegisterBinary(AProgram, LData);
			else
				return LData.GetBuffer();
		}
	}

	// operator Frontend.Save(const ALibraryName : Name, const AName : Name, const ADocument : Scalar);
	public class SaveNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			if (AArguments[2] is string)
				LDevice.SaveDocument(AProgram, (string)AArguments[0], (string)AArguments[1], (string)AArguments[2]);
			else
			{
				Stream LStream = 
				(
					AArguments[2] is byte[] 
						? new Scalar(AProgram.ValueManager, AProgram.DataTypes.SystemBinary, AArguments[2])
						: new Scalar(AProgram.ValueManager, AProgram.DataTypes.SystemBinary, (StreamID)AArguments[2])
				).OpenStream();
				try
				{
					LDevice.SaveBinary(AProgram, (string)AArguments[0], (string)AArguments[1], LStream);
				}
				finally
				{
					LStream.Close();
				}
			}
			return null;
		}
	}

	// operator GetDocumentType(const ALibraryName : Name, const AName : Name) : String
	public class GetDocumentTypeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			return LDevice.GetDocumentType(AProgram, (string)AArguments[0], (string)AArguments[1]);
		}
	}
	
	// operator Frontend.Derive(const APageType : PageType, const AQuery : String, const AMasterKeyNames : String, const ADetailKeyNames : String, const AElaborate : Boolean) : String;
	public class DeriveNode : InstructionNode
	{
		public const string CDefaultPageType = "Browse";
		public const bool CDefaultElaborate = true;

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LQuery = (string)AArguments[0];
			string LPageType = CDefaultPageType;
			string LMasterKeyNames = String.Empty;
			string LDetailKeyNames = String.Empty;
			bool LElaborate = CDefaultElaborate;
			if (AArguments.Length >= 2)
			{
				if (Operator.Operands[1].DataType.Equals(AProgram.DataTypes.SystemString))
				{
					LPageType = (string)AArguments[1];
					if (AArguments.Length == 3)
						LElaborate = (bool)AArguments[2];
					else if (AArguments.Length >= 4)
					{
						LMasterKeyNames = (string)AArguments[2];
						LDetailKeyNames = (string)AArguments[3];
						if (AArguments.Length == 5)
							LElaborate = (bool)AArguments[4];
					}
				}
				else
					LElaborate = (bool)AArguments[1];
			}
			FrontendServer LServer = FrontendServer.GetFrontendServer(AProgram.ServerProcess.ServerSession.Server);
			FrontendSession LSession = LServer.GetFrontendSession(AProgram.ServerProcess.ServerSession);
			DerivationSeed LSeed = new DerivationSeed(LPageType, LQuery, LElaborate, LMasterKeyNames, LDetailKeyNames);

			XmlDocument LDocument;
			if (LSession.UseDerivationCache)
			{
				lock (LSession.DerivationCache)
				{
					LSession.EnsureDerivationCacheConsistent();
					DerivationCacheItem LItem;
					if (!LSession.DerivationCache.TryGetValue(LSeed, out LItem))
					{
						LDocument = BuildInterface(AProgram, LSeed);
						LSession.DerivationCache.Add(LSeed, new DerivationCacheItem(LDocument));
					}
					else
						LDocument = LItem.Document;
				}
			}
			else
				LDocument = BuildInterface(AProgram, LSeed);
				
			return LDocument.OuterXml;
		}	

		protected XmlDocument BuildInterface(Program AProgram, DerivationSeed ASeed)
		{
			return new InterfaceBuilder(AProgram, ASeed).Build();
		}
	}
	
	// operator ExecuteScript(const ADocumentName : Name);
	// operator ExecuteScript(const ALibraryName : Name, const ADocumentName : Name);
	public class ExecuteScriptNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LLibraryName;
			string LDocumentName;
			if (AArguments.Length == 2)
			{
				LLibraryName = (string)AArguments[0];
				LDocumentName = (string)AArguments[1];
			}
			else
			{
				LLibraryName = AProgram.Plan.CurrentLibrary.Name;
				LDocumentName = (string)AArguments[0];
			}
			
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			
			string LDocumentType = LDevice.GetDocumentType(AProgram, LLibraryName, LDocumentName);
			
			SystemExecuteNode.ExecuteScript
			(
				AProgram.ServerProcess,
				AProgram, 
				this,
				LDevice.LoadDocument
				(
					AProgram, 
					LLibraryName,
					LDocumentName
				),
				new DAE.Debug.DebugLocator("doc:" + LLibraryName + ":" + LDocumentName, 1, 1)
			);

			return null;
		}
	}

	// operator Refresh(const ALibraryName : Name);
	public class RefreshNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice.GetFrontendDevice(AProgram).RefreshDocuments(AProgram, (string)AArguments[0]);
			return null;
		}
	}

	// operator Copy(const ASourceLibrary : Name, const ASourceDocument : Name, const ATargetLibrary : Name, const ATargetDocument : Name);
	public class CopyNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			LDevice.CopyDocument
			(
				AProgram,
				(string)AArguments[0],
				(string)AArguments[1],
				(string)AArguments[2],
				(string)AArguments[3]
			);
			return null;
		}
	}

	// operator Move(const ASourceLibrary : Name, const ASourceDocument : Name, const ATargetLibrary : Name, const ATargetDocument : Name);
	public class MoveNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProgram);
			LDevice.MoveDocument
			(
				AProgram,
				(string)AArguments[0],
				(string)AArguments[1],
				(string)AArguments[2],
				(string)AArguments[3]
			);
			return null;
		}
	}

	// operator ClearDerivationCache();
	public class ClearDerivationCacheNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			FrontendServer.GetFrontendServer(AProgram.ServerProcess.ServerSession.Server).GetFrontendSession(AProgram.ServerProcess.ServerSession).ClearDerivationCache();
			return null;
		}
	}

	// operator GetCRC32(AValue : generic) : Integer
	public class GetCRC32Node : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			DataValue LValue = AArguments[0] as DataValue;
			if (LValue == null)
				LValue = DataValue.FromNative(AProgram.ValueManager, AArguments[0]);

			if (LValue.IsPhysicalStreaming)
			{
				using (Stream LStream = LValue.OpenStream())
				{
					return (int)CRC32Utility.GetCRC32(LStream);
				}
			}
			else
			{
				byte[] LPhysical = new byte[LValue.GetPhysicalSize(true)];
				LValue.WriteToPhysical(LPhysical, 0, true);
				return (int)CRC32Utility.GetCRC32(LPhysical);
			}
		}
	}
	
	// operator GetLibraryFiles(const ALibraries : table { Library_Name : Name }) : table { Name : String, TimeStamp : DateTime, IsDotNetAssembly : Boolean };
	public class GetLibraryFilesNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Name", APlan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TimeStamp", APlan.DataTypes.SystemDateTime));
			DataType.Columns.Add(new Schema.Column("IsDotNetAssembly", APlan.DataTypes.SystemBoolean));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredFiles(Program AProgram, Schema.Library ALibrary, Table ATable, Row ARow)
		{
			foreach (Schema.FileReference LFile in ALibrary.Files)
			{
				string LFullFileName = AProgram.ServerProcess.GetFullFileName(ALibrary, LFile.FileName);
				ARow["Library_Name"] = ALibrary.Name;
				ARow["Name"] = LFile.FileName;
				ARow["TimeStamp"] = File.GetLastWriteTimeUtc(LFullFileName);
				ARow["IsDotNetAssembly"] = FileUtility.IsAssembly(LFullFileName);
				if (!ATable.FindKey(ARow))
					ATable.Insert(ARow);
			}
			
			foreach (Schema.LibraryReference LLibrary in ALibrary.Libraries)
				PopulateRequiredFiles(AProgram, AProgram.Catalog.Libraries[LLibrary.Name], ATable, ARow);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			using (Table LLibraries = (Table)Nodes[0].Execute(AProgram))
			{
				LocalTable LResult = new LocalTable(this, AProgram);
				try
				{
					LResult.Open();

					// Populate the result
					using (Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType))
					{
						LRow.ValuesOwned = false;
						
						using (Row LLibraryRow = new Row(AProgram.ValueManager, LLibraries.DataType.RowType))
						{
							while (LLibraries.Next())
							{
								LLibraries.Select(LLibraryRow);
								PopulateRequiredFiles(AProgram, AProgram.Catalog.Libraries[(string)LLibraryRow["Library_Name"]], LResult, LRow);
							}
						}
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
}
