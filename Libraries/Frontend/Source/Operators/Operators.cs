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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			LDevice.CreateDocument(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString, AArguments[2].Value.AsString, true);
			return null;
		}
	}
	
	// operator Frontend.Delete(const ALibraryName : Name, const AName : Name);
	public class DeleteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			LDevice.DeleteDocument(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString, true);
			return null;
		}
	}
	
	// operator Frontend.Rename(const AOldLibraryName : Name, const AOldName : Name, const ANewLibraryName : Name, const ANewName : Name);
	public class RenameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			LDevice.RenameDocument
			(
				AProcess, 
				AArguments[0].Value.AsString, 
				AArguments[1].Value.AsString,
				AArguments[2].Value.AsString,
				AArguments[3].Value.AsString,
				true
			);
			return null;
		}
	}
	
	// operator Frontend.Exists(const AOldLibraryName : Name, const AOldName : Name, const ANewLibraryName : Name, const ANewName : Name);
	public class ExistsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, LDevice.HasDocument(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString)));
		}
	}
	
	// operator Frontend.Load(const ALibraryName : Name, const AName : Name) : String;
	public class LoadNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return 
				new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						FrontendDevice.GetFrontendDevice(AProcess).LoadDocument
						(
							AProcess, 
							AArguments[0].Value.AsString, 
							AArguments[1].Value.AsString
						)
					)
				);
		}
	}

	// operator LoadAndProcess(const ALibraryName : Name, const AName : Name) : String
	public class LoadAndProcessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return 
				new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						FrontendDevice.GetFrontendDevice(AProcess).LoadAndProcessDocument
						(
							AProcess, 
							AArguments[0].Value.AsString, 
							AArguments[1].Value.AsString
						)
					)
				);
		}
	}
	
	// create operator Merge(const AForm : String, const ADelta : String) : String
	public class MergeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return 
				new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						FrontendDevice.GetFrontendDevice(AProcess).Merge
						(
							AArguments[0].Value.AsString, 
							AArguments[1].Value.AsString
						)
					)
				);
		}
	}

	// operator Difference(const AForm : String, const ADelta : String) : String
	public class DifferenceNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return 
				new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						FrontendDevice.GetFrontendDevice(AProcess).Difference
						(
							AArguments[0].Value.AsString, 
							AArguments[1].Value.AsString
						)
					)
				);
		}
	}

	// operator LoadCustomization(const ADilxDocument : String) : String
	public class LoadCustomizationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return 
				new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						FrontendDevice.GetFrontendDevice(AProcess).LoadCustomization
						(
							AProcess, 
							AArguments[0].Value.AsString
						)
					)
				);
		}
	}
	
	// operator Frontend.LoadBinary(const ALibraryName : Name, const AName : Name) : Binary;
	public class LoadBinaryNode : InstructionNode
	{
		public const int CNativeSizeThreshold = 4096;

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Scalar LScalar;
			FrontendDevice LFrontendDevice = FrontendDevice.GetFrontendDevice(AProcess);
			MemoryStream LData = LFrontendDevice.LoadBinary(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString);

			if (LData.Length > CNativeSizeThreshold)
			{
				LScalar = new Scalar(AProcess, (Schema.ScalarType)FDataType, LFrontendDevice.RegisterBinary(AProcess, LData));
				LScalar.ValuesOwned = true;
			}
			else
				LScalar = new Scalar(AProcess, (Schema.ScalarType)FDataType, LData.GetBuffer());

			return new DataVar(FDataType, LScalar);
		}
	}

	// operator Frontend.Image(const ALibraryName : Name, const AName : Name) : Graphic;
	public class ImageNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Scalar LScalar;
			FrontendDevice LFrontendDevice = FrontendDevice.GetFrontendDevice(AProcess);
			MemoryStream LData = LFrontendDevice.LoadBinary(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString);

			if (LData.Length > LoadBinaryNode.CNativeSizeThreshold)
			{
				LScalar = new Scalar(AProcess, (Schema.ScalarType)FDataType, LFrontendDevice.RegisterBinary(AProcess, LData));
				LScalar.ValuesOwned = true;
			}
			else
				LScalar = new Scalar(AProcess, (Schema.ScalarType)FDataType, LData.GetBuffer());

			return new DataVar(FDataType, LScalar);
		}
	}

	// operator Frontend.Save(const ALibraryName : Name, const AName : Name, const ADocument : Scalar);
	public class SaveNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			if (AArguments[2].DataType.Is(AProcess.DataTypes.SystemString))
				LDevice.SaveDocument(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString, AArguments[2].Value.AsString);
			else
			{
				Stream LStream = AArguments[2].Value.OpenStream();
				try
				{
					LDevice.SaveBinary(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString, LStream);
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			return 
				new DataVar
				(
					FDataType,
					new Scalar
					(
						AProcess,
						(Schema.ScalarType)FDataType,
						LDevice.GetDocumentType(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString)
					)
				);
		}
	}
	
	// operator Frontend.Derive(const APageType : PageType, const AQuery : String, const AMasterKeyNames : String, const ADetailKeyNames : String, const AElaborate : Boolean) : String;
	public class DeriveNode : InstructionNode
	{
		public const string CDefaultPageType = "Browse";
		public const bool CDefaultElaborate = true;

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LQuery = AArguments[0].Value.AsString;
			string LPageType = CDefaultPageType;
			string LMasterKeyNames = String.Empty;
			string LDetailKeyNames = String.Empty;
			bool LElaborate = CDefaultElaborate;
			if (AArguments.Length >= 2)
			{
				if (AArguments[1].DataType.Equals(AProcess.DataTypes.SystemString))
				{
					LPageType = AArguments[1].Value.AsString;
					if (AArguments.Length == 3)
						LElaborate = AArguments[2].Value.AsBoolean;
					else if (AArguments.Length >= 4)
					{
						LMasterKeyNames = AArguments[2].Value.AsString;
						LDetailKeyNames = AArguments[3].Value.AsString;
						if (AArguments.Length == 5)
							LElaborate = AArguments[4].Value.AsBoolean;
					}
				}
				else
					LElaborate = AArguments[1].Value.AsBoolean;
			}
			FrontendServer LServer = FrontendServer.GetFrontendServer(AProcess.ServerSession.Server);
			FrontendSession LSession = LServer.GetFrontendSession(AProcess.ServerSession);
			DerivationSeed LSeed = new DerivationSeed(LPageType, LQuery, LElaborate, LMasterKeyNames, LDetailKeyNames);

			XmlDocument LDocument;
			if (LSession.UseDerivationCache)
			{
				lock (LSession.DerivationCache)
				{
					LSession.EnsureDerivationCacheConsistent();
					DerivationCacheItem LItem = (DerivationCacheItem)LSession.DerivationCache[LSeed];
					if (LItem == null)
					{
						LDocument = BuildInterface(AProcess, LSeed);
						LSession.DerivationCache.Add(LSeed, new DerivationCacheItem(LDocument));
					}
					else
						LDocument = LItem.Document;
				}
			}
			else
				LDocument = BuildInterface(AProcess, LSeed);
				
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LDocument.OuterXml));
		}	

		protected XmlDocument BuildInterface(ServerProcess AProcess, DerivationSeed ASeed)
		{
			return new InterfaceBuilder(AProcess, ASeed).Build();
		}
	}
	
	// operator ExecuteScript(const ADocumentName : Name);
	// operator ExecuteScript(const ALibraryName : Name, const ADocumentName : Name);
	public class ExecuteScriptNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LLibraryName;
			string LDocumentName;
			if (AArguments.Length == 2)
			{
				LLibraryName = AArguments[0].Value.AsString;
				LDocumentName = AArguments[1].Value.AsString;
			}
			else
			{
				LLibraryName = AProcess.Plan.CurrentLibrary.Name;
				LDocumentName = AArguments[0].Value.AsString;
			}
			
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			
			string LDocumentType = LDevice.GetDocumentType(AProcess, LLibraryName, LDocumentName);
			
			QueryLanguage LSaveLanguage = AProcess.ServerSession.SessionInfo.Language;
			
			if (LDocumentType == "sql")
				AProcess.ServerSession.SessionInfo.Language = QueryLanguage.RealSQL;
				
			try
			{
				SystemExecuteNode.ExecuteScript
				(
					AProcess, 
					LDevice.LoadDocument
					(
						AProcess, 
						LLibraryName,
						LDocumentName
					)
				);
			}
			finally
			{
				if (LDocumentType == "sql")
					AProcess.ServerSession.SessionInfo.Language = LSaveLanguage;
			}

			return null;
		}
	}

	// operator Refresh(const ALibraryName : Name);
	public class RefreshNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice.GetFrontendDevice(AProcess).RefreshDocuments(AProcess, AArguments[0].Value.AsString);
			return null;
		}
	}

	// operator Copy(const ASourceLibrary : Name, const ASourceDocument : Name, const ATargetLibrary : Name, const ATargetDocument : Name);
	public class CopyNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			LDevice.CopyDocument
			(
				AProcess,
				AArguments[0].Value.AsString,
				AArguments[1].Value.AsString,
				AArguments[2].Value.AsString,
				AArguments[3].Value.AsString
			);
			return null;
		}
	}

	// operator Move(const ASourceLibrary : Name, const ASourceDocument : Name, const ATargetLibrary : Name, const ATargetDocument : Name);
	public class MoveNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendDevice LDevice = FrontendDevice.GetFrontendDevice(AProcess);
			LDevice.MoveDocument
			(
				AProcess,
				AArguments[0].Value.AsString,
				AArguments[1].Value.AsString,
				AArguments[2].Value.AsString,
				AArguments[3].Value.AsString
			);
			return null;
		}
	}

	// operator ClearDerivationCache();
	public class ClearDerivationCacheNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			FrontendServer.GetFrontendServer(AProcess.ServerSession.Server).GetFrontendSession(AProcess.ServerSession).ClearDerivationCache();
			return null;
		}
	}

	// operator GetCRC32(AValue : generic) : Integer
	public class GetCRC32Node : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			int LResult;
			DataValue LValue = AArguments[0].Value;
			if (LValue.IsPhysicalStreaming)
			{
				using (Stream LStream = LValue.OpenStream())
				{
					LResult = (int)CRC32Utility.GetCRC32(LStream);
				}
			}
			else
			{
				byte[] LPhysical = new byte[LValue.GetPhysicalSize(true)];
				LValue.WriteToPhysical(LPhysical, 0, true);
				LResult = (int)CRC32Utility.GetCRC32(LPhysical);
			}

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LResult));
		}
	}
	
	// operator GetLibraryFiles(const ALibraries : table { Library_Name : Name }) : table { Name : String, TimeStamp : DateTime };
	public class GetLibraryFilesNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Name", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TimeStamp", APlan.Catalog.DataTypes.SystemDateTime));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Name"]}));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredFiles(ServerProcess AProcess, Schema.Library ALibrary, Table ATable, Row ARow)
		{
			foreach (Schema.FileReference LFile in ALibrary.Files)
			{
				ARow["Name"].AsString = LFile.FileName;
				ARow["TimeStamp"].AsDateTime = File.GetLastWriteTimeUtc(PathUtility.GetFullFileName(LFile.FileName));
				if (!ATable.FindKey(ARow))
					ATable.Insert(ARow);
			}
			
			foreach (Schema.LibraryReference LLibrary in ALibrary.Libraries)
				PopulateRequiredFiles(AProcess, AProcess.Plan.Catalog.Libraries[LLibrary.Name], ATable, ARow);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			using (Table LLibraries = (Table)Nodes[0].Execute(AProcess).Value)
			{
				LocalTable LResult = new LocalTable(this, AProcess);
				try
				{
					LResult.Open();

					// Populate the result
					using (Row LRow = new Row(AProcess, LResult.DataType.RowType))
					{
						LRow.ValuesOwned = false;
						
						using (Row LLibraryRow = new Row(AProcess, LLibraries.DataType.RowType))
						{
							while (LLibraries.Next())
							{
								LLibraries.Select(LLibraryRow);
								PopulateRequiredFiles(AProcess, AProcess.Plan.Catalog.Libraries[LLibraryRow["Library_Name"].AsString], LResult, LRow);
							}
						}
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
	}
}
