/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION
#define LOADFROMLIBRARIES

using System; 
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
//using System.Windows.Forms; // TODO: Why is this here?

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// operator FileReference(const AName : Name, const AIsAssembly : Boolean) : FileReference
	public class FileReferenceNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			return new Schema.FileReference((string)AArguments[0], (bool)AArguments[1]);
		}
	}
	
	// operator FileReferenceReadName(const AValue : FileReference) : String
	public class FileReferenceReadNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ((Schema.FileReference)AArguments[0]).FileName;
		}
	}
	
	// operator FileReferenceWriteName(const AValue : FileReference, const AName : String) : FileReference
	public class FileReferenceWriteNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			Schema.FileReference LFile = (Schema.FileReference)AArguments[0];
			LFile.FileName = (string)AArguments[1];
			return LFile;
		}
	}
	
	// operator FileReferenceReadIsAssembly(const AValue : FileReference) : Boolean
	public class FileReferenceReadIsAssemblyNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ((Schema.FileReference)AArguments[0]).IsAssembly;
		}
	}
	
	// operator FileReferenceWriteIsAssembly(const AValue : FileReference, const AIsAssembly : Boolean) : FileReference
	public class FileReferenceWriteIsAssemblyNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			Schema.FileReference LFile = (Schema.FileReference)AArguments[0];
			LFile.IsAssembly = (bool)AArguments[1];
			return LFile;
		}
	}
	
	// operator .iEqual(const ALeftValue : FileReference, const ARightValue : FileReference) : Boolean
	public class FileReferenceEqualNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || (AArguments[1] == null))
				return null;
			#endif
			return ((Schema.FileReference)AArguments[0]).Equals((Schema.FileReference)AArguments[1]);
		}
	}
	
	// operator LibraryReference(const AName : Name, const AVersion : VersionNumber) : LibraryReference
	public class LibraryReferenceNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			return new Schema.LibraryReference((string)AArguments[0], (VersionNumber)AArguments[1]);
		}
	}
	
	// operator LibraryReferenceReadName(const AValue : LibraryReference) : Name
	public class LibraryReferenceReadNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ((Schema.LibraryReference)AArguments[0]).Name;
		}
	}
	
	// operator LibraryReferenceWriteName(const AValue : LibraryReference, const AName : Name) : LibraryReference
	public class LibraryReferenceWriteNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			Schema.LibraryReference LLibrary = (Schema.LibraryReference)AArguments[0];
			LLibrary.Name = (string)AArguments[1];
			return LLibrary;
		}
	}
	
	// operator LibraryReferenceReadVersion(const AValue : LibraryReference) : VersionNumber
	public class LibraryReferenceReadVersionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			Schema.LibraryReference LReference = (Schema.LibraryReference)AArguments[0];
			return LReference.Version;
		}
	}
	
	// operator LibraryReferenceWriteVersion(const AValue : LibraryReference, const AVersion : VersionNumber) : LibraryReference
	public class LibraryReferenceWriteVersionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			Schema.LibraryReference LLibrary = (Schema.LibraryReference)AArguments[0];
			LLibrary.Version = (VersionNumber)AArguments[1];
			return LLibrary;
		}
	}
	
	// operator iCompare(const ALeftValue : LibraryReference, const ARightValue : LibraryReference)
	public class LibraryReferenceCompareNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || (AArguments[1] == null))
				return null;
			#endif
			return Schema.LibraryReference.Compare((Schema.LibraryReference)AArguments[0], (Schema.LibraryReference)AArguments[1]);
		}
	}
	
	// operator LibraryDescriptor(const AName : Name);
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber);
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber, const ADefaultDeviceName : String);
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber, const ADefaultDeviceName : String, const AFiles : list(FileReference), const ARequisites : list(LibraryReference));
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber, const ADefaultDeviceName : String, const AFiles : list(FileReference), const ARequisites : list(LibraryReference), const ADirectory : String);
	public class SystemLibraryDescriptorNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			Schema.Library LLibrary = new Schema.Library(Schema.Object.EnsureUnrooted((string)AArguments[0]));
			if (AArguments.Length >= 2)
			{
				#if NILPROPOGATION
				if (AArguments[1] == null)
					return null;
				#endif
				LLibrary.Version = (VersionNumber)AArguments[1];
			}
			else
				LLibrary.Version = new VersionNumber(-1, -1, -1, -1);
				
			if (AArguments.Length >= 3)
			{
				#if NILPROPOGATION
				if (AArguments[2] == null)
					return null;
				#endif
				LLibrary.DefaultDeviceName = (string)AArguments[2];
			}

			if (AArguments.Length >= 4)
			{
				#if NILPROPOGATION
				if (AArguments[3] == null || AArguments[4] == null)
					return null;
				#endif
				ListValue LFiles = (ListValue)AArguments[3];
				ListValue LRequisites = (ListValue)AArguments[4];

				for (int LIndex = 0; LIndex < LFiles.Count(); LIndex++)
					LLibrary.Files.Add((Schema.FileReference)LFiles[LIndex]);

				for (int LIndex = 0; LIndex < LRequisites.Count(); LIndex++)
					LLibrary.Libraries.Add((Schema.LibraryReference)LRequisites[LIndex]);
			}
			
			if (AArguments.Length >= 6)
			{
				#if NILPROPOGATION
				if (AArguments[5] == null)
					return null;
				#endif
				LLibrary.Directory = (string)AArguments[5];
			}

			return LLibrary;
		}
	}
	
	// operator LibraryDescriptorReadName(const AValue : LibraryDescriptor) : Name;
	public class SystemLibraryDescriptorReadNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ((Schema.Library)AArguments[0]).Name;
		}
	}
	
	// operator LibraryDescriptorWriteName(const AValue : LibraryDescriptor, const AName : Name) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || AArguments[1] == null)
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			LLibrary.Name = Schema.Object.EnsureUnrooted((string)AArguments[1]);
			return LLibrary;
		}
	}
	
	// operator LibraryDescriptorReadDirectory(const AValue : LibraryDescriptor) : String;
	public class SystemLibraryDescriptorReadDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ((Schema.Library)AArguments[0]).Directory;
		}
	}
	
	// operator LibraryDescriptorWriteDirectory(const AValue : LibraryDescriptor, const ADirectory : String) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteDirectoryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || AArguments[1] == null)
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			if (AArguments[1] == null)
				LLibrary.Directory = null;
			else
				LLibrary.Directory = (string)AArguments[1];
			return LLibrary;
		}
	}
	
	// operator LibraryDescriptorReadDefaultDeviceName(const AValue : LibraryDescriptor) : DefaultDeviceName;
	public class SystemLibraryDescriptorReadDefaultDeviceNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null))
				return null;
			#endif
			return ((Schema.Library)AArguments[0]).DefaultDeviceName;
		}
	}
	
	// operator LibraryDescriptorWriteDefaultDeviceName(const AValue : LibraryDescriptor, const ADefaultDeviceName : DefaultDeviceName) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteDefaultDeviceNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || AArguments[1] == null)
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			LLibrary.DefaultDeviceName = (string)AArguments[1];
			return LLibrary;
		}
	}
	
	// operator LibraryDescriptorReadVersion(const AValue : LibraryDescriptor) : VersionNumber;
	public class SystemLibraryDescriptorReadVersionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ((Schema.Library)AArguments[0]).Version;
		}
	}
	
	// operator LibraryDescriptorWriteVersion(const AValue : LibraryDescriptor, const AVersion : VersionNumber) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteVersionNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			LLibrary.Version = (VersionNumber)AArguments[1];
			return LLibrary;
		}
	}
	
	// operator LibraryDescriptorReadFiles(const AValue : LibraryDescriptor) : list(FileReference);
	public class SystemLibraryDescriptorReadFilesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null))
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			ListValue LFiles = new ListValue(AProcess, (Schema.IListType)FDataType);
			foreach (Schema.FileReference LFileReference in LLibrary.Files)
				LFiles.Add(LFileReference.Clone());
			return LFiles;
		}
	}
	
	// operator LibraryDescriptorWriteFiles(const AValue : LibraryDescriptor, const AFiles : list(FileReference)) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteFilesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || AArguments[1] == null)
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			ListValue LFiles = (ListValue)AArguments[1];
			LLibrary.Files.Clear();
			for (int LIndex = 0; LIndex < LFiles.Count(); LIndex++)
				LLibrary.Files.Add((Schema.FileReference)((Schema.FileReference)LFiles[LIndex]).Clone());
			return LLibrary;
		}
	}
	
	// operator LibraryDescriptorReadRequisites(const AValue : LibraryDescriptor) : list(LibraryReference);
	public class SystemLibraryDescriptorReadRequisitesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null))
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			ListValue LRequisites = new ListValue(AProcess, (Schema.IListType)FDataType);
			foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
				LRequisites.Add(LReference.Clone());
			return LRequisites;
		}
	}
	
	// operator LibraryDescriptorWriteRequisites(const AValue : LibraryDescriptor, const ARequisites : list(String)) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteRequisitesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || AArguments[1] == null)
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			ListValue LRequisites = (ListValue)AArguments[1];
			LLibrary.Libraries.Clear();
			for (int LIndex = 0; LIndex < LRequisites.Count(); LIndex++)
				LLibrary.Libraries.Add((Schema.LibraryReference)((Schema.LibraryReference)LRequisites[LIndex]).Clone());
			return LLibrary;
		}
	}

    // operator iEqual(const ALeftValue : LibraryDescriptor, const ARightValue : LibraryDescriptor) : Boolean;
    public class SystemLibraryDescriptorEqualNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null) || (AArguments[1] == null))
				return null;
			#endif
			Schema.Library LLeftLibrary = (Schema.Library)AArguments[0];
			Schema.Library LRightLibrary = (Schema.Library)AArguments[1];
			bool LLibrariesEqual = 
				(LLeftLibrary.Name == LRightLibrary.Name) && 
				(LLeftLibrary.Directory == LRightLibrary.Directory) &&
				(LLeftLibrary.Files.Count == LRightLibrary.Files.Count) &&
				(LLeftLibrary.Libraries.Count == LRightLibrary.Libraries.Count);
				
			if (LLibrariesEqual)
			{
				for (int LIndex = 0; LIndex < LLeftLibrary.Files.Count; LIndex++)
					if (!(LLeftLibrary.Files[LIndex].Equals(LRightLibrary.Files[LIndex])))
					{
						LLibrariesEqual = false;
						break;
					}
					
				for (int LIndex = 0; LIndex < LLeftLibrary.Libraries.Count; LIndex++)
					if (!(LLeftLibrary.Libraries[LIndex].Equals(LRightLibrary.Libraries[LIndex])))
					{
						LLibrariesEqual = false;
						break;
					}
			}
			
			return LLibrariesEqual;
		}
    }
	
	// operator CreateLibrary(const ALibraryDescriptor : LibraryDescriptor);
	public class SystemCreateLibraryNode : InstructionNode
	{
		public static void CreateLibrary(ServerProcess AProcess, Schema.Library ALibrary, bool AUpdateCatalogTimeStamp, bool AShouldNotify)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				string LLibraryDirectory = ALibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
				if (AProcess.Plan.Catalog.Libraries.Contains(ALibrary.Name))
				{
					Schema.Library LExistingLibrary = AProcess.Plan.Catalog.Libraries[ALibrary.Name];

					if (ALibrary.Directory != String.Empty)
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{		
							string LExistingLibraryDirectory = LExistingLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
							if (Directory.Exists(LExistingLibraryDirectory))
							{
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(LExistingLibraryDirectory, true);
								#endif
								Directory.Delete(LExistingLibraryDirectory, true);
							}
							
							LExistingLibrary.Directory = ALibrary.Directory;

							if (!Directory.Exists(LLibraryDirectory))
								Directory.CreateDirectory(LLibraryDirectory);
							LExistingLibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LExistingLibrary.Name)));
						}
						finally
						{
							AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}

						AProcess.CatalogDeviceSession.SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
					}

					SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProcess, ALibrary.Name, ALibrary.Version, AUpdateCatalogTimeStamp);
					SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProcess, ALibrary.Name, ALibrary.DefaultDeviceName, AUpdateCatalogTimeStamp);

					foreach (Schema.LibraryReference LReference in ALibrary.Libraries)
						if (!LExistingLibrary.Libraries.Contains(LReference))
							SystemSetLibraryDescriptorNode.AddLibraryRequisite(AProcess, ALibrary.Name, LReference);

					foreach (Schema.FileReference LReference in ALibrary.Files)
						if (!LExistingLibrary.Files.Contains(LReference))
							SystemSetLibraryDescriptorNode.AddLibraryFile(AProcess, ALibrary.Name, LReference);

					if (ALibrary.MetaData != null)
						#if USEHASHTABLEFORTAGS
						foreach (Tag LTag in ALibrary.MetaData.Tags)
							SystemSetLibraryDescriptorNode.AddLibrarySetting(AProcess, ALibrary.Name, LTag);
						#else
						for (int LIndex = 0; LIndex < ALibrary.MetaData.Tags.Count; LIndex++)
							SystemSetLibraryDescriptorNode.AddLibrarySetting(AProcess, ALibrary.Name, ALibrary.MetaData.Tags[LIndex]);
						#endif
				}
				else
				{
					Compiler.CheckValidCatalogObjectName(AProcess.Plan, null, ALibrary.Name);
					AProcess.Plan.Catalog.Libraries.Add(ALibrary);
					try
					{
						if (AUpdateCatalogTimeStamp)
							AProcess.Plan.Catalog.UpdateTimeStamp();
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{
							if (!Directory.Exists(LLibraryDirectory))
								Directory.CreateDirectory(LLibraryDirectory);
							ALibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(ALibrary.Name)));
						}
						finally
						{
							AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
					}
					catch
					{
						AProcess.Plan.Catalog.Libraries.Remove(ALibrary);
						throw;
					}
				}

				if (AShouldNotify)
				{
					AProcess.Plan.Catalog.Libraries.DoLibraryCreated(AProcess, ALibrary.Name);
					AProcess.Plan.Catalog.Libraries.DoLibraryAdded(AProcess, ALibrary.Name);
					if (ALibrary.Directory != String.Empty)
						AProcess.CatalogDeviceSession.SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
				}
			}
		}
		
		public static void AttachLibrary(ServerProcess AProcess, Schema.Library ALibrary, bool AIsAttached)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				string LLibraryDirectory = ALibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);

				AProcess.Plan.Catalog.Libraries.Add(ALibrary);
				AProcess.Plan.Catalog.UpdateTimeStamp();
				AProcess.Plan.Catalog.Libraries.DoLibraryAdded(AProcess, ALibrary.Name);
				if ((ALibrary.Directory != String.Empty) && !AIsAttached)
					AProcess.CatalogDeviceSession.SetLibraryDirectory(ALibrary.Name, LLibraryDirectory);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			CreateLibrary(AProcess, (Schema.Library)AArguments[0], true, true);
			return null;
		}
	}
	
	// operator DropLibrary(const ALibraryName : Name);
	public class SystemDropLibraryNode : InstructionNode
	{
		public static void DropLibrary(ServerProcess AProcess, string ALibraryName, bool AUpdateCatalogTimeStamp)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDropRegisteredLibrary, LLibrary.Name);
					
				string LLibraryDirectory = LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);

				AProcess.Plan.Catalog.Libraries.DoLibraryRemoved(AProcess, LLibrary.Name);
				AProcess.Plan.Catalog.Libraries.DoLibraryDeleted(AProcess, LLibrary.Name);
				try
				{
					AProcess.Plan.Catalog.Libraries.Remove(LLibrary);
					try
					{
						if (AUpdateCatalogTimeStamp)
							AProcess.Plan.Catalog.UpdateTimeStamp();
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{		
							if (Directory.Exists(LLibraryDirectory))
							{
								#if !RESPECTREADONLY
								PathUtility.EnsureWriteable(LLibraryDirectory, true);
								#endif
								Directory.Delete(LLibraryDirectory, true);
							}
						}
						finally
						{
							AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
						
						AProcess.CatalogDeviceSession.ClearLibraryOwner(ALibraryName);
						AProcess.CatalogDeviceSession.ClearCurrentLibraryVersion(ALibraryName);
					}
					catch
					{
						if (Directory.Exists(LLibraryDirectory))
							AProcess.Plan.Catalog.Libraries.Add(LLibrary);
						throw;
					}
				}
				catch
				{
					if (Directory.Exists(LLibraryDirectory))
					{
						AProcess.Plan.Catalog.Libraries.DoLibraryCreated(AProcess, LLibrary.Name);
						AProcess.Plan.Catalog.Libraries.DoLibraryAdded(AProcess, LLibrary.Name);
					}
					throw;
				}
			}
		}
		
		public static void DetachLibrary(ServerProcess AProcess, string ALibraryName)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotDetachRegisteredLibrary, LLibrary.Name);
					
				string LLibraryDirectory = LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);

				AProcess.Plan.Catalog.Libraries.DoLibraryRemoved(AProcess, LLibrary.Name);
				AProcess.Plan.Catalog.Libraries.Remove(LLibrary);
				AProcess.Plan.Catalog.UpdateTimeStamp();
				AProcess.CatalogDeviceSession.DeleteLibraryDirectory(LLibrary.Name);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			DropLibrary(AProcess, (string)AArguments[0], true);
			return null;
		}
	}

	// operator RenameLibrary(const ALibraryName : Name, const ANewLibraryName : Name);
	public class SystemRenameLibraryNode : InstructionNode
	{
		public static void RenameLibrary(ServerProcess AProcess, string ALibraryName, string ANewLibraryName, bool AUpdateCatalogTimeStamp)
		{
			ANewLibraryName = Schema.Object.EnsureUnrooted(ANewLibraryName);
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
				if (ANewLibraryName != LLibrary.Name)
				{
					Compiler.CheckValidCatalogObjectName(AProcess.Plan, null, ANewLibraryName);
					if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRenameRegisteredLibrary, LLibrary.Name);
						
					string LOldName = LLibrary.Name;
					AProcess.Plan.Catalog.Libraries.Remove(LLibrary);
					try
					{
						LLibrary.Name = ANewLibraryName;
						
						string LOldLibraryDirectory = Schema.Library.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory, LOldName, LLibrary.Directory);
						string LLibraryDirectory = LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
						string LOldLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LOldName));
						string LLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LLibrary.Name));
						try
						{
							AProcess.Plan.Catalog.Libraries.Add(LLibrary);
							try
							{
								AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
								try
								{
									#if !RESPECTREADONLY
									PathUtility.EnsureWriteable(LOldLibraryDirectory, true);
									#endif
									if (LOldLibraryDirectory != LLibraryDirectory)
										Directory.Move(LOldLibraryDirectory, LLibraryDirectory);
									try
									{
										#if !RESPECTREADONLY
										FileUtility.EnsureWriteable(LOldLibraryName);
										#endif
										File.Delete(LOldLibraryName);
										LLibrary.SaveToFile(LLibraryName);
									}
									catch
									{
										if (LOldLibraryDirectory != LLibraryDirectory)
											Directory.Move(LLibraryDirectory, LOldLibraryDirectory);
										LOldLibraryName = Path.Combine(LOldLibraryDirectory, Schema.Library.GetFileName(LLibrary.Name));
										if (File.Exists(LOldLibraryName))
											File.Delete(LOldLibraryName);
										throw;
									}
								}
								finally
								{
									AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
								}

								if (AUpdateCatalogTimeStamp)
									AProcess.Plan.Catalog.UpdateTimeStamp();
							}
							catch
							{
								AProcess.Plan.Catalog.Libraries.Remove(LLibrary);
								throw;
							}
						}
						catch
						{
							LLibrary.Name = LOldName;
							AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
							try
							{
								LLibrary.SaveToFile(Path.Combine(LOldLibraryDirectory, Schema.Library.GetFileName(LLibrary.Name))); // ensure that the file is restored to its original state
							}
							finally
							{
								AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
							}
							throw;
						}
					}
					catch
					{
						AProcess.Plan.Catalog.Libraries.Add(LLibrary);
						throw;
					}
					AProcess.Plan.Catalog.Libraries.DoLibraryRenamed(AProcess, LOldName, LLibrary.Name);
				}
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			RenameLibrary(AProcess, (string)AArguments[0], (string)AArguments[1], true);
			return null;
		}
	}

	// operator RefreshLibraries(const ALibraryName : Name);
	public class SystemRefreshLibrariesNode : InstructionNode
	{
		private static void EnsureLibraryUnregistered(ServerProcess AProcess, Schema.Library ALibrary, bool AWithReconciliation)
		{
			Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibrary.Name, false);
			if (LLoadedLibrary != null)
			{
				while (LLoadedLibrary.RequiredByLibraries.Count > 0)
					EnsureLibraryUnregistered(AProcess, AProcess.Plan.Catalog.Libraries[LLoadedLibrary.RequiredByLibraries[0].Name], AWithReconciliation);
				SystemUnregisterLibraryNode.UnregisterLibrary(AProcess, ALibrary.Name, AWithReconciliation);
			}
		}
		
		private static void RemoveLibrary(ServerProcess AProcess, Schema.Library ALibrary)
		{
			// Ensure that the library and any dependencies of it are unregistered
			EnsureLibraryUnregistered(AProcess, ALibrary, false);
			SystemDropLibraryNode.DetachLibrary(AProcess, ALibrary.Name);
		}
		
		public static void RefreshLibraries(ServerProcess AProcess)
		{
			// Get the list of available libraries from the library directory
			Schema.Libraries LLibraries = new Schema.Libraries();
			Schema.Libraries LOldLibraries = new Schema.Libraries();
			Schema.Library.GetAvailableLibraries(AProcess.ServerSession.Server.InstanceDirectory, AProcess.ServerSession.Server.LibraryDirectory, LLibraries);
			
			lock (AProcess.Plan.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library LLibrary in LLibraries)
					if (!AProcess.Plan.Catalog.Libraries.ContainsName(LLibrary.Name) && !AProcess.Plan.Catalog.ContainsName(LLibrary.Name))
						SystemCreateLibraryNode.AttachLibrary(AProcess, LLibrary, false);
						
				// Ensure that each library in the DAE is supported by a library in the directory, remove non-existent libraries
				// The System library is not required to have a directory.
				foreach (Schema.Library LLibrary in AProcess.Plan.Catalog.Libraries)
					if ((Server.CSystemLibraryName != LLibrary.Name) && (LLibrary.Directory == String.Empty) && !LLibraries.ContainsName(LLibrary.Name))
						LOldLibraries.Add(LLibrary);
						
				foreach (Schema.Library LLibrary in LOldLibraries)
					RemoveLibrary(AProcess, LLibrary);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			RefreshLibraries(AProcess);
			return null;
		}
	}
	
	// operator AttachLibraries(string ADirectory)
	// Attaches all libraries found in the given directory
	public class SystemAttachLibrariesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			string LLibraryDirectory = (string)AArguments[0];
			
			Schema.Libraries LLibraries = new Schema.Libraries();
			Schema.Library.GetAvailableLibraries(AProcess.ServerSession.Server.InstanceDirectory, LLibraryDirectory, LLibraries, true);

			lock (AProcess.Plan.Catalog.Libraries)
			{
				// Ensure that each library in the directory is available in the DAE
				foreach (Schema.Library LLibrary in LLibraries)
					if (!AProcess.Plan.Catalog.Libraries.ContainsName(LLibrary.Name) && !AProcess.Plan.Catalog.ContainsName(LLibrary.Name))
						SystemCreateLibraryNode.AttachLibrary(AProcess, LLibrary, false);
			}
			
			return null;
		}
	}
	
	// operator AttachLibrary(string ALibraryName, string ALibraryDirectory)
	public class SystemAttachLibraryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			string LLibraryDirectory = (string)AArguments[1];
			
			AttachLibrary(AProcess, LLibraryName, LLibraryDirectory, false);
			
			return null;
		}
		
		/// <summary>Attaches the library given by ALibraryName from ALibraryDirectory. AIsAttached indicates whether this library is being attached as part of catalog startup.</summary>
		public static void AttachLibrary(ServerProcess AProcess, string ALibraryName, string ALibraryDirectory, bool AIsAttached)
		{
			Schema.Library LLibrary = Schema.Library.GetAvailableLibrary(AProcess.ServerSession.Server.InstanceDirectory, ALibraryName, ALibraryDirectory);
			if ((LLibrary != null) && !AProcess.Plan.Catalog.Libraries.ContainsName(LLibrary.Name))
				SystemCreateLibraryNode.AttachLibrary(AProcess, LLibrary, AIsAttached);
		}
	}
	
	// operator DetachLibrary(string ALibraryName)
	public class SystemDetachLibraryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			SystemDropLibraryNode.DetachLibrary(AProcess, LLibraryName);
			
			return null;
		}
	}
	
	// operator GetLibraryDescriptor(const ALibraryName : Name) : LibraryDescriptor;
	public class SystemGetLibraryDescriptorNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[(string)AArguments[0]];
				return (Schema.Library)LLibrary.Clone();
			}
		}
	}
	
	// operator SetLibraryDescriptor(const ALibraryName : Name, const ALibraryDescriptor : LibraryDescriptor);
	public class SystemSetLibraryDescriptorNode : InstructionNode
	{
		public static void ChangeLibraryVersion(ServerProcess AProcess, string ALibraryName, VersionNumber AVersion, bool AUpdateTimeStamp)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (!(LLibrary.Version.Equals(AVersion)))
				{
					Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
					{
						foreach (Schema.LoadedLibrary LDependentLoadedLibrary in LLoadedLibrary.RequiredByLibraries)
						{
							Schema.Library LDependentLibrary = AProcess.Plan.Catalog.Libraries[LDependentLoadedLibrary.Name];
							Schema.LibraryReference LLibraryReference = LDependentLibrary.Libraries[LLibrary.Name];
							if (!VersionNumber.Compatible(LLibraryReference.Version, AVersion))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.RegisteredLibraryHasDependents, LLibrary.Name, LDependentLibrary.Name, LLibraryReference.Version.ToString());
						}
					}
					LLibrary.Version = AVersion;

					if (AUpdateTimeStamp)
						AProcess.Plan.Catalog.UpdateTimeStamp();

					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				
					if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))	
						AProcess.CatalogDeviceSession.SetCurrentLibraryVersion(LLibrary.Name, LLibrary.Version);
				}
			}
		}
		
		public static void SetLibraryDefaultDeviceName(ServerProcess AProcess, string ALibraryName, string ADefaultDeviceName, bool AUpdateTimeStamp)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (LLibrary.DefaultDeviceName != ADefaultDeviceName)
				{	
					LLibrary.DefaultDeviceName = ADefaultDeviceName;

					if (AUpdateTimeStamp)
						AProcess.Plan.Catalog.UpdateTimeStamp();
						
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
			}
		}
		
		public static void AddLibraryRequisite(ServerProcess AProcess, string ALibraryName, Schema.LibraryReference ARequisiteLibrary)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (!AProcess.Plan.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProcess, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);
					
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				LLibrary.Libraries.Add(ARequisiteLibrary);
				try
				{
					SystemRegisterLibraryNode.CheckCircularLibraryReference(AProcess, LLibrary, ARequisiteLibrary.Name);
					Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
					{
						SystemRegisterLibraryNode.EnsureLibraryRegistered(AProcess, ARequisiteLibrary, true);
						if (!LLoadedLibrary.RequiredLibraries.Contains(ARequisiteLibrary.Name))
						{
							LLoadedLibrary.RequiredLibraries.Add(AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ARequisiteLibrary.Name));
							AProcess.Plan.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
						LLoadedLibrary.AttachLibrary();
					}
					
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					LLibrary.Libraries.Remove(ARequisiteLibrary);
					throw;
				}
			}
		}
		
		public static void UpdateLibraryRequisite(ServerProcess AProcess, string ALibraryName, Schema.LibraryReference AOldReference, Schema.LibraryReference ANewReference)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				if (String.Compare(AOldReference.Name, ANewReference.Name, false) != 0)
				{
					RemoveLibraryRequisite(AProcess, ALibraryName, AOldReference);
					AddLibraryRequisite(AProcess, ALibraryName, ANewReference);
				}
				else
				{
					if (!VersionNumber.Equals(AOldReference.Version, ANewReference.Version))
					{
						if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
						{
							// Ensure that the registered library satisfies the new requisite
							Schema.Library LRequiredLibrary = AProcess.Plan.Catalog.Libraries[AOldReference.Name];
							if (!VersionNumber.Compatible(ANewReference.Version, LRequiredLibrary.Version))
								throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidLibraryReference, AOldReference.Name, ALibraryName, ANewReference.Version.ToString(), LRequiredLibrary.Version.ToString());
						}
						
						LLibrary.Libraries[AOldReference.Name].Version = ANewReference.Version;

						AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{
							LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
						}
						finally
						{
							AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
					}
				}
			}
		}
		
		public static void RemoveLibraryRequisite(ServerProcess AProcess, string ALibraryName, Schema.LibraryReference ARequisiteLibrary)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRemoveRequisitesFromRegisteredLibrary, LLibrary.Name);

				LLibrary.Libraries.Remove(ARequisiteLibrary);
				AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
				try
				{
					LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
				}
				finally
				{
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
				}
			}
		}
		
		public static void AddLibraryFile(ServerProcess AProcess, string ALibraryName, Schema.FileReference AFile)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (!AProcess.Plan.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProcess, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);

				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				LLibrary.Files.Add(AFile);
				try
				{
					// ensure that all assemblies are registered
					Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
					if (LLoadedLibrary != null)
						SystemRegisterLibraryNode.RegisterLibraryFiles(AProcess, LLibrary, LLoadedLibrary);
				
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					LLibrary.Files.Remove(AFile);
					throw;
				}
			}
		}
		
		public static void RemoveLibraryFile(ServerProcess AProcess, string ALibraryName, Schema.FileReference AFile)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);

				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				LLibrary.Files.Remove(AFile);
				try
				{
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					LLibrary.Files.Add(AFile);
					throw;
				}
			}
		}
		
		public static void AddLibrarySetting(ServerProcess AProcess, string ALibraryName, Tag ASetting)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (!AProcess.Plan.Catalog.Libraries.Contains(ALibraryName))
					SystemCreateLibraryNode.CreateLibrary(AProcess, new Schema.Library(Schema.Object.EnsureUnrooted(ALibraryName)), true, false);
					
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);

				if (LLibrary.MetaData == null)
					LLibrary.MetaData = new MetaData();
					
				LLibrary.MetaData.Tags.Add(ASetting);
				try
				{
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					LLibrary.MetaData.Tags.Remove(ASetting.Name);
					throw;
				}
			}
		}
		
		public static void UpdateLibrarySetting(ServerProcess AProcess, string ALibraryName, Tag AOldSetting, Tag ANewSetting)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				if (String.Compare(AOldSetting.Name, ANewSetting.Name, false) != 0)
				{
					LLibrary.MetaData.Tags.Remove(AOldSetting.Name);
					LLibrary.MetaData.Tags.Add(ANewSetting);
				}
				else
					LLibrary.MetaData.Tags.Update(AOldSetting.Name, ANewSetting.Value);
				try
				{
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					if (String.Compare(AOldSetting.Name, ANewSetting.Name, false) != 0)
					{
						LLibrary.MetaData.Tags.Remove(ANewSetting.Name);
						LLibrary.MetaData.Tags.Add(AOldSetting);
					}
					else
					{
						LLibrary.MetaData.Tags.Update(AOldSetting.Name, AOldSetting.Value);
					}
					throw;
				}
			}
		}
		
		public static void RemoveLibrarySetting(ServerProcess AProcess, string ALibraryName, Tag ATag)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
				
				LLibrary.MetaData.Tags.Remove(ATag.Name);
				try
				{
					AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
					try
					{
						LLibrary.SaveToFile(Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LLibrary.Name)));
					}
					finally
					{
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
					}
				}
				catch
				{
					LLibrary.MetaData.Tags.Add(ATag);
					throw;
				}
			}
		}
		
		public static void SetLibraryDescriptor(ServerProcess AProcess, string AOldLibraryName, Schema.Library ANewLibrary, bool AUpdateTimeStamp)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LOldLibrary = AProcess.Plan.Catalog.Libraries[AOldLibraryName];
				
				if (Schema.Object.NamesEqual(LOldLibrary.Name, Server.CSystemLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifySystemLibrary);
					
				if (Schema.Object.NamesEqual(LOldLibrary.Name, Server.CGeneralLibraryName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotModifyGeneralLibrary);
					
				if (LOldLibrary.Name != ANewLibrary.Name)
					Compiler.CheckValidCatalogObjectName(AProcess.Plan, null, ANewLibrary.Name);
				
				if (AProcess.CatalogDeviceSession.IsLoadedLibrary(LOldLibrary.Name))
				{
					foreach (Schema.LibraryReference LReference in ANewLibrary.Libraries)
						SystemRegisterLibraryNode.CheckCircularLibraryReference(AProcess, ANewLibrary, LReference.Name);
						
					// cannot rename a loaded library
					if (LOldLibrary.Name != ANewLibrary.Name)
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRenameRegisteredLibrary, LOldLibrary.Name);
						
					// cannot change the version of a loaded library
					if (!(LOldLibrary.Version.Equals(ANewLibrary.Version)))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotChangeRegisteredLibraryVersion, LOldLibrary.Name);
						
					// cannot remove a required library
					foreach (Schema.LibraryReference LReference in LOldLibrary.Libraries)
						if (!ANewLibrary.Libraries.Contains(LReference))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotRemoveRequisitesFromRegisteredLibrary, LOldLibrary.Name);

					Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ANewLibrary.Name);							
					foreach (Schema.LibraryReference LReference in ANewLibrary.Libraries)
					{
						SystemRegisterLibraryNode.EnsureLibraryRegistered(AProcess, LReference, true);
						if (!LLoadedLibrary.RequiredLibraries.Contains(LReference.Name))
						{
							LLoadedLibrary.RequiredLibraries.Add(AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
							AProcess.Plan.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
					}
					LLoadedLibrary.AttachLibrary();
					
					// ensure that all assemblies are registered
					SystemRegisterLibraryNode.RegisterLibraryFiles(AProcess, ANewLibrary, AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ANewLibrary.Name));
				}
				
				AProcess.Plan.Catalog.Libraries.Remove(LOldLibrary);
				try
				{
					AProcess.Plan.Catalog.Libraries.Add(ANewLibrary);
					try
					{
						if (AUpdateTimeStamp)
							AProcess.Plan.Catalog.UpdateTimeStamp();

						string LLibraryDirectory = ANewLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
						string LLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(ANewLibrary.Name));
						AProcess.ServerSession.Server.MaintainedLibraryUpdate = true;
						try
						{
							try
							{
								if (LOldLibrary.Name != ANewLibrary.Name)
								{
									string LOldLibraryDirectory = LOldLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
									string LOldLibraryName = Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LOldLibrary.Name));
									#if !RESPECTREADONLY
									PathUtility.EnsureWriteable(LOldLibraryDirectory, true);
									FileUtility.EnsureWriteable(LOldLibraryName);
									#endif
									Directory.Move(LOldLibraryDirectory, LLibraryDirectory);
									try
									{
										File.Delete(LOldLibraryName);
									}
									catch
									{
										Directory.Move(LLibraryDirectory, LOldLibraryDirectory);
										throw;
									}
									AProcess.Plan.Catalog.Libraries.DoLibraryRenamed(AProcess, LOldLibrary.Name, ANewLibrary.Name);
								}
								ANewLibrary.SaveToFile(LLibraryName);
							}
							catch
							{
								if (LOldLibrary.Name != ANewLibrary.Name)
									LOldLibrary.SaveToFile(Path.Combine(LOldLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), Schema.Library.GetFileName(LOldLibrary.Name)));
								throw;
							}
						}
						finally
						{
							AProcess.ServerSession.Server.MaintainedLibraryUpdate = false;
						}
					}
					catch
					{
						AProcess.Plan.Catalog.Libraries.Remove(ANewLibrary);
						throw;
					}
				}
				catch
				{
					AProcess.Plan.Catalog.Libraries.Add(LOldLibrary);
					throw;
				}
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			SetLibraryDescriptor(AProcess, (string)AArguments[0], (Schema.Library)AArguments[1], true);
			return null;
		}
	}
	
	// operator LibrarySetting(const ASettingName : String) : String;
	public class SystemLibrarySettingNode : InstructionNode
	{
		// Find the first unambiguous setting value for the given setting name in a breadth-first traversal of the library dependency graph
		private string ResolveSetting(Plan APlan, string ASettingName)
		{
			Tag LTag = Tag.None;
			foreach (Schema.LoadedLibraries LLevel in APlan.NameResolutionPath)
			{
				StringCollection LContainingLibraries = new StringCollection();
				foreach (Schema.LoadedLibrary LLoadedLibrary in LLevel)
				{
					Schema.Library LLibrary = APlan.Catalog.Libraries[LLoadedLibrary.Name];
					if ((LLibrary.MetaData != null) && LLibrary.MetaData.Tags.Contains(ASettingName))
						LContainingLibraries.Add(LLibrary.Name);
				}
				
				if (LContainingLibraries.Count == 1)
				{
					LTag = APlan.Catalog.Libraries[LContainingLibraries[0]].MetaData.Tags[ASettingName];
					break;
				}
				else if (LContainingLibraries.Count > 1)
				{
					StringBuilder LBuilder = new StringBuilder();
					foreach (string LLibraryName in LContainingLibraries)
					{
						if (LBuilder.Length > 0)
							LBuilder.Append(", ");
						LBuilder.AppendFormat("{0}", LLibraryName);
					}
					
					throw new Schema.SchemaException(Schema.SchemaException.Codes.AmbiguousLibrarySetting, ASettingName, LBuilder.ToString());
				}
			}

			return LTag == Tag.None ? null : LTag.Value;
		}

		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return ResolveSetting(AProcess.Plan, (string)AArguments[0]);
		}
	}
	
	// operator RegisterLibrary(const ALibraryName : Name);	
	// operator RegisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemRegisterLibraryNode : InstructionNode
	{
		public const string CRegisterFileName = @"Documents\Register.d4";
		public const string CRegisterDocumentLocator = @"doc:{0}:Register";

		public static void EnsureLibraryRegistered(ServerProcess AProcess, Schema.LibraryReference ALibraryReference, bool AWithReconciliation)
		{
			Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryReference.Name, false);
			if (LLoadedLibrary == null)
			{
				Schema.LoadedLibrary LCurrentLibrary = AProcess.ServerSession.CurrentLibrary;
				try
				{
					Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryReference.Name];
					if (!VersionNumber.Compatible(ALibraryReference.Version, LLibrary.Version))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, ALibraryReference.Name, ALibraryReference.Version.ToString(), LLibrary.Version.ToString());
					RegisterLibrary(AProcess, LLibrary.Name, AWithReconciliation);
				}
				finally
				{
					AProcess.ServerSession.CurrentLibrary = LCurrentLibrary;
				}
			}
			else
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryReference.Name];
				if (!VersionNumber.Compatible(ALibraryReference.Version, LLibrary.Version))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, ALibraryReference.Name, ALibraryReference.Version.ToString(), LLibrary.Version.ToString());
			}
		}
		
		public static void RegisterLibraryFiles(ServerProcess AProcess, Schema.Library ALibrary, Schema.LoadedLibrary ALoadedLibrary)
		{
			// Register each assembly with the DAE
			#if !LOADFROMLIBRARIES
			foreach (Schema.FileReference LFile in ALibrary.Files)
			{
				string LSourceFile = Path.IsPathRooted(LFile.FileName) ? LFile.FileName : Path.Combine(ALibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), LFile.FileName);
				string LTargetFile = Path.Combine(PathUtility.GetBinDirectory(), Path.GetFileName(LFile.FileName));
				
				if (!File.Exists(LSourceFile))
					throw new System.IO.IOException(String.Format("File \"{0}\" not found.", LSourceFile));
				try
				{
					#if !RESPECTREADONLY
					FileUtility.EnsureWriteable(LTargetFile);
					#endif
                    if ((File.GetLastWriteTimeUtc(LSourceFile) > File.GetLastWriteTimeUtc(LTargetFile))) // source newer than target
                    {
                        File.Copy(LSourceFile, LTargetFile, true);
                    }
				}
				catch (IOException)
				{
					// Ignore this exception so that assembly copying does not fail if the assembly is already loaded
				}
			}
			#endif
			
			// Load assemblies after all files are copied in so that multi-file assemblies and other dependencies are certain to be present
			foreach (Schema.FileReference LFile in ALibrary.Files)
			{
				if (LFile.IsAssembly)
				{
					#if LOADFROMLIBRARIES
					string LSourceFile = Path.IsPathRooted(LFile.FileName) ? LFile.FileName : Path.Combine(ALibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), LFile.FileName);
                    Assembly LAssembly = Assembly.LoadFrom(LSourceFile);
					#else
                    string LTargetFile = Path.Combine(PathUtility.GetBinDirectory(), Path.GetFileName(LFile.FileName));
                    Assembly LAssembly = Assembly.LoadFrom(LTargetFile);
                    #endif
                    AProcess.CatalogDeviceSession.RegisterAssembly(ALoadedLibrary, LAssembly);
				}
			}
		}
		
		public static void UnregisterLibraryAssemblies(ServerProcess AProcess, Schema.LoadedLibrary ALoadedLibrary)
		{
			while (ALoadedLibrary.Assemblies.Count > 0)
				AProcess.CatalogDeviceSession.UnregisterAssembly(ALoadedLibrary, ALoadedLibrary.Assemblies[ALoadedLibrary.Assemblies.Count - 1] as Assembly);
		}
		
		public static void CheckCircularLibraryReference(ServerProcess AProcess, Schema.Library ALibrary, string ARequiredLibraryName)
		{
			if (IsCircularLibraryReference(AProcess, ALibrary, ARequiredLibraryName))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.CircularLibraryReference, ALibrary.Name, ARequiredLibraryName);
		}
		
		public static bool IsCircularLibraryReference(ServerProcess AProcess, Schema.Library ALibrary, string ARequiredLibraryName)
		{
			Schema.Library LRequiredLibrary = AProcess.Plan.Catalog.Libraries[ARequiredLibraryName];
			if (Schema.Object.NamesEqual(ALibrary.Name, ARequiredLibraryName))
				return true;
				
			foreach (Schema.LibraryReference LReference in LRequiredLibrary.Libraries)
				if (IsCircularLibraryReference(AProcess, ALibrary, LReference.Name))
					return true;
					
			return false;
		}
		
		public static void RegisterLibrary(ServerProcess AProcess, string ALibraryName, bool AWithReconciliation)
		{
			int LSaveReconciliationState = AProcess.SuspendReconciliationState();
			try
			{
				if (!AWithReconciliation)
					AProcess.DisableReconciliation();
				try
				{
					lock (AProcess.Plan.Catalog.Libraries)
					{
						Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
						
						Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LLibrary.Name, false);
						if (LLoadedLibrary != null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryAlreadyRegistered, ALibraryName);
							
						LLoadedLibrary = new Schema.LoadedLibrary(ALibraryName);
						LLoadedLibrary.Owner = AProcess.Plan.User;
							
						//	Ensure that each required library is registered
						foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
						{
							CheckCircularLibraryReference(AProcess, LLibrary, LReference.Name);
							EnsureLibraryRegistered(AProcess, LReference, AWithReconciliation);
							LLoadedLibrary.RequiredLibraries.Add(AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
							AProcess.Plan.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProcess.ServerSession.Server.SystemLibrary));
							LLoadedLibrary.ClearNameResolutionPath();
						}
						
						AProcess.ServerSession.Server.DoLibraryLoading(LLibrary.Name);
						try
						{
							// Register the assemblies
							RegisterLibraryFiles(AProcess, LLibrary, LLoadedLibrary);
							AProcess.CatalogDeviceSession.InsertLoadedLibrary(LLoadedLibrary);
							LLoadedLibrary.AttachLibrary();
							try
							{
								// Set the current library to the newly registered library
								Schema.LoadedLibrary LCurrentLibrary = AProcess.Plan.CurrentLibrary;
								AProcess.ServerSession.CurrentLibrary = LLoadedLibrary;
								try
								{
									//	run the register.d4 script if it exists in the library
									//		catalog objects created in this script are part of this library
									string LRegisterFileName = Path.Combine(LLibrary.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory), CRegisterFileName);
									if (File.Exists(LRegisterFileName))
									{
										try
										{
											using (StreamReader LReader = new StreamReader(LRegisterFileName))
											{
												AProcess.ServerSession.Server.RunScript
												(
													AProcess, 
													LReader.ReadToEnd(), 
													ALibraryName, 
													new DAE.Debug.DebugLocator(String.Format(CRegisterDocumentLocator, ALibraryName), 1, 1)
												);
											}
										}
										catch (Exception LException)
										{
											throw new RuntimeException(RuntimeException.Codes.LibraryRegistrationFailed, LException, ALibraryName);
										}
									}

									AProcess.CatalogDeviceSession.SetCurrentLibraryVersion(LLibrary.Name, LLibrary.Version);
									AProcess.CatalogDeviceSession.SetLibraryOwner(LLoadedLibrary.Name, LLoadedLibrary.Owner.ID);
									if (LLibrary.IsSuspect)
									{
										LLibrary.IsSuspect = false;
										LLibrary.SaveInfoToFile(Path.Combine(LLibrary.GetInstanceLibraryDirectory(AProcess.ServerSession.Server.InstanceDirectory), Schema.Library.GetInfoFileName(LLibrary.Name)));
									}					
								}
								catch
								{
									AProcess.ServerSession.CurrentLibrary = LCurrentLibrary;
									throw;
								}
							}
							catch
							{
								LLoadedLibrary.DetachLibrary();
								throw;
							}
							AProcess.Plan.Catalog.Libraries.DoLibraryLoaded(AProcess, LLibrary.Name);
						}
						finally
						{
							AProcess.ServerSession.Server.DoLibraryLoaded(ALibraryName);
						}
					}
				}
				finally
				{
					if (!AWithReconciliation)
						AProcess.EnableReconciliation();
				}
			}
			finally
			{
				AProcess.ResumeReconciliationState(LSaveReconciliationState);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			bool LWithReconciliation = AArguments.Length > 1 ? (bool)AArguments[1] : true;
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (!AProcess.CatalogDeviceSession.IsLoadedLibrary(LLibraryName))
					RegisterLibrary(AProcess, LLibraryName, LWithReconciliation);
			}
			return null;
		}
	}
	
	// operator EnsureLibraryRegistered(const ALibraryName : Name);
	public class SystemEnsureLibraryRegisteredNode : InstructionNode
	{
		public static void EnsureLibraryRegistered(ServerProcess AProcess, string ALibraryName, bool AWithReconciliation)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName, false) == null)
					SystemRegisterLibraryNode.RegisterLibrary(AProcess, ALibraryName, AWithReconciliation);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			EnsureLibraryRegistered(AProcess, (string)AArguments[0], true);
			return null;
		}
	}
	
	// operator UnregisterLibrary(const ALibraryName : Name);
	// operator UnregisterLibrary(const ALibraryName : Name, const AWithReconciliation : Boolean);
	public class SystemUnregisterLibraryNode : InstructionNode
	{
		public static void UnregisterLibrary(ServerProcess AProcess, string ALibraryName, bool AWithReconciliation)
		{
			int LSaveReconciliationState = AProcess.SuspendReconciliationState();
			try
			{
				if (!AWithReconciliation)
					AProcess.DisableReconciliation();
				try
				{
					lock (AProcess.Plan.Catalog.Libraries)
					{
						Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName);
						
						if (Schema.Object.NamesEqual(LLoadedLibrary.Name, Server.CSystemLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterSystemLibrary);
							
						if (Schema.Object.NamesEqual(LLoadedLibrary.Name, Server.CGeneralLibraryName))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.CannotUnregisterGeneralLibrary);

						if (LLoadedLibrary.RequiredByLibraries.Count > 0)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryIsRequired, LLoadedLibrary.Name);
							
						// Drop all the objects in the library
						AProcess.ServerSession.Server.RunScript
						(
							AProcess, 
							AProcess.ServerSession.Server.ScriptDropLibrary(AProcess.CatalogDeviceSession, ALibraryName), 
							LLoadedLibrary.Name,
							null
						);
						
						// Any session with current library set to the unregistered library will be set to General
						AProcess.ServerSession.Server.LibraryUnloaded(LLoadedLibrary.Name);

						// Remove the library from the catalog
						AProcess.CatalogDeviceSession.DeleteLoadedLibrary(LLoadedLibrary);
						LLoadedLibrary.DetachLibrary();
						
						// Unregister each assembly that was loaded with this library
						foreach (Assembly LAssembly in LLoadedLibrary.Assemblies)
							AProcess.Plan.Catalog.ClassLoader.UnregisterAssembly(LAssembly);

						// TODO: Unregister assemblies when the .NET framework supports it
						
						AProcess.Plan.Catalog.Libraries.DoLibraryUnloaded(AProcess, LLoadedLibrary.Name);
					}
				}
				finally
				{
					if (!AWithReconciliation)
						AProcess.EnableReconciliation();
				}
			}
			finally
			{
				AProcess.ResumeReconciliationState(LSaveReconciliationState);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			UnregisterLibrary(AProcess, (string)AArguments[0], AArguments.Length > 1 ? (bool)AArguments[1] : true);
			return null;
		}
	}
	
	// operator LoadLibrary(const ALibraryName : Name);
	public class SystemLoadLibraryNode : InstructionNode
	{
		public static void LoadLibrary(ServerProcess AProcess, string ALibraryName)
		{
			LoadLibrary(AProcess, ALibraryName, false);
		}
		
		private static void LoadLibrary(ServerProcess AProcess, string ALibraryName, bool AIsKnown)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				VersionNumber LCurrentVersion = AProcess.CatalogDeviceSession.GetCurrentLibraryVersion(ALibraryName);
				
				if (AProcess.Plan.Catalog.LoadedLibraries.Contains(LLibrary.Name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryAlreadyLoaded, ALibraryName);

				bool LIsLoaded = false;				
				bool LAreAssembliesRegistered = false;	
				Schema.LoadedLibrary LLoadedLibrary = null;
				try
				{
					LLoadedLibrary = new Schema.LoadedLibrary(ALibraryName);
					LLoadedLibrary.Owner = AProcess.CatalogDeviceSession.ResolveUser(AProcess.CatalogDeviceSession.GetLibraryOwner(ALibraryName));
						
					//	Ensure that each required library is loaded
					foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
					{
						Schema.Library LRequiredLibrary = AProcess.Plan.Catalog.Libraries[LReference.Name];
						if (!VersionNumber.Compatible(LReference.Version, LRequiredLibrary.Version))
							throw new Schema.SchemaException(Schema.SchemaException.Codes.LibraryVersionMismatch, LReference.Name, LReference.Version.ToString(), LRequiredLibrary.Version.ToString());

						if (!AProcess.Plan.Catalog.LoadedLibraries.Contains(LReference.Name))
						{
							if (!LRequiredLibrary.IsSuspect)
								LoadLibrary(AProcess, LReference.Name, AIsKnown);
							else
								throw new Schema.SchemaException(Schema.SchemaException.Codes.RequiredLibraryNotLoaded, ALibraryName, LReference.Name);
						}
						
						LLoadedLibrary.RequiredLibraries.Add(AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LReference.Name));
						AProcess.Plan.Catalog.OperatorResolutionCache.Clear(LLoadedLibrary.GetNameResolutionPath(AProcess.ServerSession.Server.SystemLibrary));
						LLoadedLibrary.ClearNameResolutionPath();
					}
					
					AProcess.ServerSession.Server.DoLibraryLoading(LLibrary.Name);
					try
					{
						// RegisterAssemblies
						SystemRegisterLibraryNode.RegisterLibraryFiles(AProcess, LLibrary, LLoadedLibrary);
						
						LAreAssembliesRegistered = true;

						AProcess.CatalogDeviceSession.InsertLoadedLibrary(LLoadedLibrary);
						LLoadedLibrary.AttachLibrary();
						try
						{
							AProcess.CatalogDeviceSession.SetLibraryOwner(LLoadedLibrary.Name, LLoadedLibrary.Owner.ID);
						}
						catch (Exception LRegisterException)
						{
							LLoadedLibrary.DetachLibrary();
							throw LRegisterException;
						}

						LIsLoaded = true; // If we reach this point, a subsequent exception must unload the library
						if (LLibrary.IsSuspect)
						{
							LLibrary.IsSuspect = false;
							LLibrary.SaveInfoToFile(Path.Combine(LLibrary.GetInstanceLibraryDirectory(AProcess.ServerSession.Server.InstanceDirectory), Schema.Library.GetInfoFileName(LLibrary.Name)));
						}					
					}
					finally
					{
						AProcess.ServerSession.Server.DoLibraryLoaded(LLibrary.Name);
					}
				}
				catch (Exception LException)
				{
					AProcess.ServerSession.Server.LogError(LException);
					LLibrary.IsSuspect = true;
					LLibrary.SuspectReason = ExceptionUtility.DetailedDescription(LException);
					LLibrary.SaveInfoToFile(Path.Combine(LLibrary.GetInstanceLibraryDirectory(AProcess.ServerSession.Server.InstanceDirectory), Schema.Library.GetInfoFileName(LLibrary.Name)));
					
					if (LIsLoaded)
						SystemUnregisterLibraryNode.UnregisterLibrary(AProcess, ALibraryName, false);
					else if (LAreAssembliesRegistered)
						SystemRegisterLibraryNode.UnregisterLibraryAssemblies(AProcess, LLoadedLibrary);
						
					throw;
				}

				AProcess.CatalogDeviceSession.SetCurrentLibraryVersion(LLibrary.Name, LCurrentVersion); // Once a library has loaded, record the version number
				
				AProcess.Plan.Catalog.Libraries.DoLibraryLoaded(AProcess, LLibrary.Name);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "LoadLibrary is obsolete. Use RegisterLibrary instead.");
		}
	}
	
	public class SystemUpgradeLibraryNode : InstructionNode
	{
		/*
			2.1 -> Upgrading in the presence of dependencies:
			
			The upgrade system must track version dependencies per upgrade to ensure
			that upgrades run in the context in which they were injected. In other
			words, each upgrade must run with the version of each requisite library
			at the time the upgrade was injected. To manage these dependencies, we
			introduce the following operators:
			
			UpgradeRequisites
			(
				const ALibraryName : Name, 
				const AUpgradeVersion : VersionNumber
			) : 
				table 
				{ 
					Required_Library_Name : Name, 
					Required_Library_Version : VersionNumber 
				};
				
				Given an Upgrade denoted by ALibraryName and AUpgradeVersion,
				returns the set of libraries, together with their version, required
				by the library at the time the upgrade was injected.
			
			SaveUpgradeRequisite
			(
				const ALibraryName : Name, 
				const AUpgradeVersion : VersionNumber, 
				const ARequiredLibraryName : Name, 
				const ARequiredLibraryVersion : VersionNumber
			);
			
				Records the version number of the required library for a specific upgrade,
				with replace semantics for existing upgrade requisites.
			
			DeleteUpgradeRequisite
			(
				const ALibraryName : Name,
				const AUpgradeVersion : VersionNumber,
				const ARequiredLibraryName : Name,
				const ARequiredLibraryVersion : VersionNumber
			);
			
				Deletes the specified upgrade requisite from the given upgrade, with
				ignore semantics for non-existent upgrade requisites.
			
			RequiredUpgrades
			(
				const ALibraryName : Name, 
				const AVersion : VersionNumber
			) : 
				table 
				{ 
					Required_Library_Name : Name, 
					Required_Library_Version : VersionNumber 
				};
				
				Returns the set of library requisites for the given upgrade. If the upgrade has no upgrade requisites,
				the current version (not loaded version) of each requisite library of the library is returned.
				
				LibraryRequisites where Library_Name = ALibraryName { Required_Library_Name, Required_Library_Version }
					left join (UpgradeRequisites(ALibraryName, AVersion) { Upgrade_Library_Name, Upgrade_Library_Version }
						by Required_Library_Name = Upgrade_Library_Name
				{ 
					IfNil(Upgrade_Library_Name, Required_Library_Name) Required_Library_Name,
					IfNil(Upgrade_Library_Version, Required_Library_Version) Required_Library_Version
				}

			DependentUpgrades
			(
				const ALibraryName : Name,
				const AVersion : VersionNumber
			) :
				table
				{
					Dependent_Library_Name : Name,
					Dependent_Library_Version : VersionNumber
				};
				
				Returns the set of dependent libraries that have an upgrade that depends on a version less than
				the given upgrade version.

				UpgradeDependent(ALibraryName, AVersion)
					foreach DependentLibrary of ALibraryName
						foreach Upgrade of ADependentLibrary > CurrentVersion
							foreach UpgradeRequisite < AVersion
								Add to Result

			The UpgradeLibrary(ALibraryName) operator now has the following semantics:
			
				foreach VersionNumber LVersion in UpgradeVersions(ALibraryName)
					UpgradeLibrary(ALibraryName, LVersion);
				foreach Name LLibraryName in DependentLibraries(ALibraryName)
					UpgradeLibrary(ALibraryName);
					
			UpgradeLibrary(const ALibraryName : Name, const AVersion : VersionNumber);
			
				foreach row in RequiredUpgrades(ALibraryName, AVersion)
					UpgradeLibrary(Required_Library_Name, Required_Version);
				foreach row in DependentUpgrades(ALibraryName, AVersion)
					UpgradeLibrary(Dependent_Library_Name, Dependent_Version);
				Run the upgrade given by ALibraryName and AVersion	

		*/

		/*
			Upgrade all Libraries ->
				Calculate library load order for all loaded libraries
				foreach library in the library load order ->
					upgrade the library
					if an exception occurs ->
						unload each dependent library
						unload the library
						load the library
						load each dependent library
						rethrow
						
			Upgrade a single library ->
				Calculate library load order for all required
				foreach library in the library load order ->
					upgrade the library
					if an exception occurs ->
						unload each dependent library
						unload the library
						load the library
						load each dependent library
						rethrow
		*/
		
		private static void GatherDependents(Schema.LoadedLibrary ALibrary, Schema.LoadedLibraries ADependents)
		{
			foreach (Schema.LoadedLibrary LLibrary in ALibrary.RequiredByLibraries)
			{
				if (!ADependents.ContainsName(LLibrary.Name))
				{
					GatherDependents(LLibrary, ADependents);
					ADependents.Add(LLibrary);
				}
			}
		}
		
		private static void UpgradeLibraries(ServerProcess AProcess, string[] ALibraries)
		{
			foreach (string LLibraryName in ALibraries)
				UpgradeLibrary(AProcess, LLibraryName);
		}
		
		private static void InternalUpgradeLibrary(ServerProcess AProcess, string ALibraryName, VersionNumber ACurrentVersion, VersionNumber ATargetVersion)
		{
			DataParams LParams = new DataParams();
			LParams.Add(new DataParam("ALibraryName", AProcess.Plan.Catalog.DataTypes.SystemName, Modifier.Const, ALibraryName));
			LParams.Add(new DataParam("ACurrentVersion", Compiler.ResolveCatalogIdentifier(AProcess.Plan, "System.VersionNumber", true) as Schema.IDataType, Modifier.Const, ACurrentVersion));
			LParams.Add(new DataParam("ATargetVersion", Compiler.ResolveCatalogIdentifier(AProcess.Plan, "System.VersionNumber", true) as Schema.IDataType, Modifier.Const, ATargetVersion));
			IServerStatementPlan LPlan =
				((IServerProcess)AProcess).PrepareStatement
				(
					@"
						begin
							var LRow : typeof(row from System.UpgradeVersions(ALibraryName));
							var LCursor := cursor(System.UpgradeVersions(ALibraryName) where (Version > ACurrentVersion) and (Version <= ATargetVersion));
							try
								while LCursor.Next() do
								begin
									LRow := LCursor.Select();
									BeginTransaction();
									try
										LogMessage('Executing upgrade script for library ' + ALibraryName + ', version ' + (Version from LRow).ToString());
										SetLibrary(ALibraryName);
										Execute(System.LoadUpgrade(ALibraryName, Version from LRow));
										if exists (LibraryVersions where LibraryName = ALibraryName) then
											update LibraryVersions set { Version := Version from LRow } where LibraryName = ALibraryName
										else
											insert table { row { ALibraryName LibraryName, Version from LRow Version } } into LibraryVersions;
										CommitTransaction();
									except
										RollbackTransaction();
										raise;
									end;
								end;
							finally
								LCursor.Close();
							end;
						end;
					",
					LParams
				);
			try
			{
				LPlan.Execute(LParams);
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareStatement(LPlan);
			}
		}
		
		public static void UpgradeLibrary(ServerProcess AProcess, string ALibraryName)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
				Schema.LoadedLibrary LLoadedLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName);
				VersionNumber LCurrentVersion = AProcess.CatalogDeviceSession.GetCurrentLibraryVersion(ALibraryName);
				if (VersionNumber.Compare(LLibrary.Version, LCurrentVersion) > 0)
				{
					SessionInfo LSessionInfo = new SessionInfo(LLoadedLibrary.Owner.ID == Server.CSystemUserID ? Server.CAdminUserID : LLoadedLibrary.Owner.ID, "", LLoadedLibrary.Name);
					LSessionInfo.DefaultUseImplicitTransactions = false;
					IServerSession LSession = AProcess.ServerSession.Server.ConnectAs(LSessionInfo);
					try
					{
						IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo));
						try
						{
							InternalUpgradeLibrary((ServerProcess)LProcess, LLibrary.Name, LCurrentVersion, LLibrary.Version);
						}
						finally
						{
							LSession.StopProcess(LProcess);
						}
					}
					finally
					{
						((IServer)AProcess.ServerSession.Server).Disconnect(LSession);
					}
					AProcess.CatalogDeviceSession.SetCurrentLibraryVersion(ALibraryName, LLibrary.Version);
				}
			}				
		}
		
		private static void GatherRequisites(Schema.LoadedLibrary ALibrary, Schema.LoadedLibraries ARequisites)
		{
			foreach (Schema.LoadedLibrary LLibrary in ALibrary.RequiredLibraries)
				GatherRequisites(LLibrary, ARequisites);
			
			if (!ARequisites.Contains(ALibrary.Name))	
				ARequisites.Add(ALibrary);
		}
		
		public static void UpgradeLibraries(ServerProcess AProcess)
		{
			Schema.LoadedLibraries LLibraries = new Schema.LoadedLibraries();
			foreach (Schema.LoadedLibrary LLibrary in AProcess.Plan.Catalog.LoadedLibraries)
				GatherRequisites(LLibrary, LLibraries);
			string[] LLibraryArray = new string[LLibraries.Count];
			for (int LIndex = 0; LIndex < LLibraries.Count; LIndex++)
				LLibraryArray[LIndex] = LLibraries[LIndex].Name;
			UpgradeLibraries(AProcess, LLibraryArray);
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			string LLibraryName = (string)AArguments[0];
			Schema.LoadedLibrary LLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(LLibraryName);
			Schema.LoadedLibraries LRequisites = new Schema.LoadedLibraries();
			GatherRequisites(LLibrary, LRequisites);
			string[] LLibraries = new string[LRequisites.Count];
			for (int LIndex = 0; LIndex < LRequisites.Count; LIndex++)
				LLibraries[LIndex] = LRequisites[LIndex].Name;
			UpgradeLibraries(AProcess, LLibraries);
			return null;
		}
	}

	// operator UpgradeLibraries();	
	public class SystemUpgradeLibrariesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			SystemUpgradeLibraryNode.UpgradeLibraries(AProcess);
			return null;
		}
	}

	// operator UnloadLibrary(const ALibraryName : Name);
	public class SystemUnloadLibraryNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "UnloadLibrary is obsolete. Use UnregisterLibrary instead.");
		}
	}
	
	public class SystemSaveLibraryCatalogNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			throw new ServerException(ServerException.Codes.ServerError, "SaveLibraryCatalog is obsolete. Use ScriptCatalog instead.");
		}
	}
	
	public class UpgradeUtility : System.Object
	{
		public const string CUpgradeDirectory = "Upgrades";
		public const string CUpgradeFileName = "Upgrade";
		
		public static bool IsValidVersionNumber(VersionNumber AVersion)
		{
			return (AVersion.Revision >= 0) && (AVersion.Build < 0);
		}
		
		public static void CheckValidVersionNumber(VersionNumber AVersion)
		{
			// VersionNumbers used in Upgrades must be specified to the revision number only
			if (!IsValidVersionNumber(AVersion))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidUpgradeVersionNumber, AVersion.ToString());
		}
		
		public static VersionNumber GetVersionFromFileName(string AFileName)
		{
			VersionNumber LResult = VersionNumber.Parse(AFileName.Substring(AFileName.IndexOf(".") + 1, AFileName.IndexOf(".d4") - (AFileName.IndexOf(".") + 1)));
			CheckValidVersionNumber(LResult);
			return LResult;
		}
		
		public static string GetFileNameFromVersion(VersionNumber AVersion)
		{
			CheckValidVersionNumber(AVersion);
			return String.Format("{0}.{1}.d4", CUpgradeFileName, AVersion.ToString().Replace(".*", ""));
		}
		
		public static string GetRequisitesFileNameFromVersion(VersionNumber AVersion)
		{
			CheckValidVersionNumber(AVersion);
			return String.Format("{0}.{1}.d4r", CUpgradeFileName, AVersion.ToString().Replace(".*", ""));
		}

		public static string GetUpgradeDirectory(ServerProcess AProcess, string ALibraryName, string ALibraryDirectory)
		{
			return Path.Combine(Schema.Library.GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory, ALibraryName, ALibraryDirectory), CUpgradeDirectory);
		}
		
		public static void EnsureUpgradeDirectory(ServerProcess AProcess, string ALibraryName, string ALibraryDirectory)
		{
			string LUpgradeDirectory = GetUpgradeDirectory(AProcess, ALibraryName, ALibraryDirectory);
			if (!Directory.Exists(LUpgradeDirectory))
				Directory.CreateDirectory(LUpgradeDirectory);
		}
		
		public static string GetFileName(ServerProcess AProcess, string ALibraryName, string ALibraryDirectory, VersionNumber AVersion)
		{
			return Path.Combine(GetUpgradeDirectory(AProcess, ALibraryName, ALibraryDirectory), GetFileNameFromVersion(AVersion));
		}
		
		public static string GetRequisitesFileName(ServerProcess AProcess, string ALibraryName, string ALibraryDirectory, VersionNumber AVersion)
		{
			return Path.Combine(GetUpgradeDirectory(AProcess, ALibraryName, ALibraryDirectory), GetRequisitesFileNameFromVersion(AVersion));
		}
	}

	// create operator System.UpgradeVersions(const ALibraryName : Name, const ACurrentVersion : VersionNumber, const ATargetVersion : VersionNumber) : table { Version : VersionNumber };
	public class UpgradeVersionsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Version", Compiler.ResolveCatalogIdentifier(APlan, "System.VersionNumber", true) as Schema.ScalarType));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Version"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(ServerProcess AProcess, Table ATable, Row ARow, VersionNumber AVersion)
		{
			ARow[0] = AVersion;
			ATable.Insert(ARow);
		}
		
		private void PopulateUpgradeVersions(ServerProcess AProcess, Table ATable, Row ARow, string ALibraryName)
		{
			Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
			string LUpgradeDirectory = UpgradeUtility.GetUpgradeDirectory(AProcess, LLibrary.Name, LLibrary.Directory);
			if (Directory.Exists(LUpgradeDirectory))
			{
				string[] LFileNames = Directory.GetFiles(LUpgradeDirectory, String.Format("{0}*.d4", UpgradeUtility.CUpgradeFileName));
				for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
					PopulateUpgradeVersion(AProcess, ATable, ARow, UpgradeUtility.GetVersionFromFileName(Path.GetFileName(LFileNames[LIndex])));
			}
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
					PopulateUpgradeVersions(AProcess, LResult, LRow, (string)Nodes[0].Execute(AProcess));
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
	
/*
	// create operator System.UpgradeRequisites(const ALibraryName : Name, const AVersion : VersionNumber) : table { Required_Library_Name : Name, Required_Library_Version : VersionNumber };
	public class UpgradeRequisitesNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Required_Library_Name", APlan.Catalog["System.Name"] as Schema.ScalarType));
			DataType.Columns.Add(new Schema.Column("Required_Library_Version", APlan.Catalog["System.VersionNumber"] as Schema.ScalarType));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Required_Library_Name"]}));

			TableVar.DetermineRemotable();
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(ServerProcess AProcess, Table ATable, Row ARow, VersionNumber AVersion)
		{
			ARow[0].AsNative = AVersion;
			ATable.Insert(ARow);
		}
		
		private void PopulateUpgradeVersions(ServerProcess AProcess, Table ATable, Row ARow, string ALibraryName)
		{
			Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
			string LUpgradeDirectory = UpgradeUtility.GetUpgradeDirectory(AProcess, LLibrary.Name, LLibrary.Directory);
			if (Directory.Exists(LUpgradeDirectory))
			{
				string[] LFileNames = Directory.GetFiles(LUpgradeDirectory, String.Format("{0}*.d4", UpgradeUtility.CUpgradeFileName));
				for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
					PopulateUpgradeVersion(AProcess, ATable, ARow, UpgradeUtility.GetVersionFromFileName(Path.GetFileName(LFileNames[LIndex])));
			}
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
					PopulateUpgradeVersions(AProcess, LResult, LRow, Nodes[0].Execute(AProcess).Value.AsString);
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
	
	// create operator System.UpgradeVersions(const ALibraryName : Name) : table { Version : VersionNumber };
	public class UpgradeVersionsNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Version", APlan.Catalog["System.VersionNumber"] as Schema.ScalarType));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Version"]}));

			TableVar.DetermineRemotable();
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateUpgradeVersion(ServerProcess AProcess, Table ATable, Row ARow, VersionNumber AVersion)
		{
			ARow[0].AsNative = AVersion;
			ATable.Insert(ARow);
		}
		
		private void PopulateUpgradeVersions(ServerProcess AProcess, Table ATable, Row ARow, string ALibraryName)
		{
			Schema.Library LLibrary = AProcess.Plan.Catalog.Libraries[ALibraryName];
			string LUpgradeDirectory = UpgradeUtility.GetUpgradeDirectory(AProcess, LLibrary.Name, LLibrary.Directory);
			if (Directory.Exists(LUpgradeDirectory))
			{
				string[] LFileNames = Directory.GetFiles(LUpgradeDirectory, String.Format("{0}*.d4", UpgradeUtility.CUpgradeFileName));
				for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
					PopulateUpgradeVersion(AProcess, ATable, ARow, UpgradeUtility.GetVersionFromFileName(Path.GetFileName(LFileNames[LIndex])));
			}
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
					PopulateUpgradeVersions(AProcess, LResult, LRow, Nodes[0].Execute(AProcess).Value.AsString);
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
*/
	
	// create operator System.LoadUpgrade(const ALibraryName : Name, const AVersion : VersionNumber) : String;
	public class LoadUpgradeNode : InstructionNode
	{
		public static string LoadUpgrade(ServerProcess AProcess, Schema.Library ALibrary, VersionNumber AVersion)
		{
			StreamReader LReader = new StreamReader(UpgradeUtility.GetFileName(AProcess, ALibrary.Name, ALibrary.Directory, AVersion));
			try
			{
				return LReader.ReadToEnd();
			}
			finally
			{
				LReader.Close();
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return 
				LoadUpgrade
				(
					AProcess, 
					AProcess.Plan.Catalog.Libraries[(string)AArguments[0]], 
					(VersionNumber)AArguments[1]
				);
		}
	}
	
	// create operator System.SaveUpgrade(const ALibraryName : Name, const AVersion : VersionNumber, const AScript : String);
	public class SaveUpgradeNode : InstructionNode
	{
		public static void SaveUpgrade(ServerProcess AProcess, Schema.Library ALibrary, VersionNumber AVersion, string AScript)
		{
			UpgradeUtility.EnsureUpgradeDirectory(AProcess, ALibrary.Name, ALibrary.Directory);
			string LFileName = UpgradeUtility.GetFileName(AProcess, ALibrary.Name, ALibrary.Directory, AVersion);
			FileUtility.EnsureWriteable(LFileName);
			StreamWriter LWriter = new StreamWriter(LFileName, false);
			try
			{
				LWriter.Write(AScript);
				LWriter.Flush();
			}
			finally
			{
				LWriter.Close();
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments[1] == null)
				InjectUpgradeNode.InjectUpgrade(AProcess, AProcess.Plan.Catalog.Libraries[(string)AArguments[0]], (string)AArguments[2]);
			else
				SaveUpgrade(AProcess, AProcess.Plan.Catalog.Libraries[(string)AArguments[0]], (VersionNumber)AArguments[1], (string)AArguments[2]);
			return null;
		}
	}
	
	// create operator System.DeleteUpgrade(const ALibraryName : Name, const AVersion : VersionNumber);
	public class DeleteUpgradeNode : InstructionNode
	{
		public static void DeleteUpgrade(ServerProcess AProcess, Schema.Library ALibrary, VersionNumber AVersion)
		{
			string LFileName = UpgradeUtility.GetFileName(AProcess, ALibrary.Name, ALibrary.Directory, AVersion);
			FileUtility.EnsureWriteable(LFileName);
			File.Delete(LFileName);
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			DeleteUpgrade(AProcess, AProcess.Plan.Catalog.Libraries[(string)AArguments[0]], (VersionNumber)AArguments[1]);
			return null;
		}
	}
	
	// create operator System.InjectUpgrade(const ALibraryName : Name, const AScript : String) : VersionNumber;
	public class InjectUpgradeNode : InstructionNode
	{
		public static VersionNumber InjectUpgrade(ServerProcess AProcess, Schema.Library ALibrary, string AScript)
		{
			VersionNumber LLibraryVersion = ALibrary.Version;
			if (LLibraryVersion.Revision < 0)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidUpgradeLibraryVersionNumber, ALibrary.Name, LLibraryVersion.ToString());
			SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProcess, ALibrary.Name, new VersionNumber(LLibraryVersion.Major, LLibraryVersion.Minor, LLibraryVersion.Revision + 1, LLibraryVersion.Build), true);
			try
			{
				VersionNumber LUpgradeVersion = new VersionNumber(LLibraryVersion.Major, LLibraryVersion.Minor, ALibrary.Version.Revision, -1);
				UpgradeUtility.CheckValidVersionNumber(LUpgradeVersion);
				SaveUpgradeNode.SaveUpgrade(AProcess, ALibrary, LUpgradeVersion, AScript);
				return LUpgradeVersion;
			}
			catch
			{
				SystemSetLibraryDescriptorNode.ChangeLibraryVersion(AProcess, ALibrary.Name, LLibraryVersion, false);
				throw;
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return InjectUpgrade(AProcess, AProcess.Plan.Catalog.Libraries[(string)AArguments[0]], (string)AArguments[1]);
		}
	}
}

