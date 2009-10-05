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
using System.Collections.Generic;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Server;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// operator FileReference(const AName : Name, const AIsAssembly : Boolean) : FileReference
	// operator FileReference(const AName : Name, const AIsAssembly : Boolean, const AEnvironments : list(String)) : FileReference
	public class FileReferenceNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || (AArguments.Length >= 3 && AArguments[2] == null)) 
				return null;
			#endif
			if (AArguments.Length == 2)
				return new Schema.FileReference((string)AArguments[0], (bool)AArguments[1]);
			
			return new Schema.FileReference((string)AArguments[0], (bool)AArguments[1], ((ListValue)AArguments[2]).ToList<string>());
		}
	}
	
	// operator FileReferenceReadName(const AValue : FileReference) : String
	public class FileReferenceReadNameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
	
	// operator FileReferenceReadEnvironments(const AValue : FileReference) : Boolean
	public class FileReferenceReadEnvironmentsNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif
			return new ListValue(AProgram.ValueManager, (Schema.IListType)FDataType, ((Schema.FileReference)AArguments[0]).Environments);
		}
	}
	
	// operator FileReferenceWriteEnvironments(const AValue : FileReference, list(String) AEnvironments : Boolean) : FileReference
	public class FileReferenceWriteEnvironmentsNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif
			Schema.FileReference LFile = (Schema.FileReference)AArguments[0];
			LFile.Environments.Clear();
			LFile.Environments.AddRange(((ListValue)AArguments[1]).ToList<string>());
			return LFile;
		}
	}
	
	// operator .iEqual(const ALeftValue : FileReference, const ARightValue : FileReference) : Boolean
	public class FileReferenceEqualNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null))
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			ListValue LFiles = new ListValue(AProgram.ValueManager, (Schema.IListType)FDataType);
			foreach (Schema.FileReference LFileReference in LLibrary.Files)
				LFiles.Add(LFileReference.Clone());
			return LFiles;
		}
	}
	
	// operator LibraryDescriptorWriteFiles(const AValue : LibraryDescriptor, const AFiles : list(FileReference)) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteFilesNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0] == null))
				return null;
			#endif
			Schema.Library LLibrary = (Schema.Library)AArguments[0];
			ListValue LRequisites = new ListValue(AProgram.ValueManager, (Schema.IListType)FDataType);
			foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
				LRequisites.Add(LReference.Clone());
			return LRequisites;
		}
	}
	
	// operator LibraryDescriptorWriteRequisites(const AValue : LibraryDescriptor, const ARequisites : list(String)) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteRequisitesNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
		public override object InternalExecute(Program AProgram, object[] AArguments)
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
}

