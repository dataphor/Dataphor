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
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null || (arguments.Length >= 3 && arguments[2] == null)) 
				return null;
			#endif
			if (arguments.Length == 2)
				return new Schema.FileReference((string)arguments[0], (bool)arguments[1]);
			
			return new Schema.FileReference((string)arguments[0], (bool)arguments[1], ((ListValue)arguments[2]).ToList<string>());
		}
	}
	
	// operator FileReferenceReadName(const AValue : FileReference) : String
	public class FileReferenceReadNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ((Schema.FileReference)arguments[0]).FileName;
		}
	}
	
	// operator FileReferenceWriteName(const AValue : FileReference, const AName : String) : FileReference
	public class FileReferenceWriteNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			Schema.FileReference file = (Schema.FileReference)arguments[0];
			file.FileName = (string)arguments[1];
			return file;
		}
	}
	
	// operator FileReferenceReadIsAssembly(const AValue : FileReference) : Boolean
	public class FileReferenceReadIsAssemblyNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ((Schema.FileReference)arguments[0]).IsAssembly;
		}
	}
	
	// operator FileReferenceWriteIsAssembly(const AValue : FileReference, const AIsAssembly : Boolean) : FileReference
	public class FileReferenceWriteIsAssemblyNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			Schema.FileReference file = (Schema.FileReference)arguments[0];
			file.IsAssembly = (bool)arguments[1];
			return file;
		}
	}
	
	// operator FileReferenceReadEnvironments(const AValue : FileReference) : Boolean
	public class FileReferenceReadEnvironmentsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return new ListValue(program.ValueManager, (Schema.IListType)_dataType, ((Schema.FileReference)arguments[0]).Environments);
		}
	}
	
	// operator FileReferenceWriteEnvironments(const AValue : FileReference, list(String) AEnvironments : Boolean) : FileReference
	public class FileReferenceWriteEnvironmentsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			Schema.FileReference file = (Schema.FileReference)arguments[0];
			file.Environments.Clear();
			file.Environments.AddRange(((ListValue)arguments[1]).ToList<string>());
			return file;
		}
	}
	
	// operator .iEqual(const ALeftValue : FileReference, const ARightValue : FileReference) : Boolean
	public class FileReferenceEqualNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || (arguments[1] == null))
				return null;
			#endif
			return ((Schema.FileReference)arguments[0]).Equals((Schema.FileReference)arguments[1]);
		}
	}
	
	// operator LibraryReference(const AName : Name, const AVersion : VersionNumber) : LibraryReference
	public class LibraryReferenceNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			return new Schema.LibraryReference((string)arguments[0], (VersionNumber)arguments[1]);
		}
	}
	
	// operator LibraryReferenceReadName(const AValue : LibraryReference) : Name
	public class LibraryReferenceReadNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ((Schema.LibraryReference)arguments[0]).Name;
		}
	}
	
	// operator LibraryReferenceWriteName(const AValue : LibraryReference, const AName : Name) : LibraryReference
	public class LibraryReferenceWriteNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			Schema.LibraryReference library = (Schema.LibraryReference)arguments[0];
			library.Name = (string)arguments[1];
			return library;
		}
	}
	
	// operator LibraryReferenceReadVersion(const AValue : LibraryReference) : VersionNumber
	public class LibraryReferenceReadVersionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			Schema.LibraryReference reference = (Schema.LibraryReference)arguments[0];
			return reference.Version;
		}
	}
	
	// operator LibraryReferenceWriteVersion(const AValue : LibraryReference, const AVersion : VersionNumber) : LibraryReference
	public class LibraryReferenceWriteVersionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			Schema.LibraryReference library = (Schema.LibraryReference)arguments[0];
			library.Version = (VersionNumber)arguments[1];
			return library;
		}
	}
	
	// operator iCompare(const ALeftValue : LibraryReference, const ARightValue : LibraryReference)
	public class LibraryReferenceCompareNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || (arguments[1] == null))
				return null;
			#endif
			return Schema.LibraryReference.Compare((Schema.LibraryReference)arguments[0], (Schema.LibraryReference)arguments[1]);
		}
	}
	
	// operator LibraryDescriptor(const AName : Name);
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber);
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber, const ADefaultDeviceName : String);
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber, const ADefaultDeviceName : String, const AFiles : list(FileReference), const ARequisites : list(LibraryReference));
	// operator LibraryDescriptor(const AName : Name, const AVersion : VersionNumber, const ADefaultDeviceName : String, const AFiles : list(FileReference), const ARequisites : list(LibraryReference), const ADirectory : String);
	public class SystemLibraryDescriptorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			Schema.Library library = new Schema.Library(Schema.Object.EnsureUnrooted((string)arguments[0]));
			if (arguments.Length >= 2)
			{
				#if NILPROPOGATION
				if (arguments[1] == null)
					return null;
				#endif
				library.Version = (VersionNumber)arguments[1];
			}
			else
				library.Version = new VersionNumber(-1, -1, -1, -1);
				
			if (arguments.Length >= 3)
			{
				#if NILPROPOGATION
				if (arguments[2] == null)
					return null;
				#endif
				library.DefaultDeviceName = (string)arguments[2];
			}

			if (arguments.Length >= 4)
			{
				#if NILPROPOGATION
				if (arguments[3] == null || arguments[4] == null)
					return null;
				#endif
				ListValue files = (ListValue)arguments[3];
				ListValue requisites = (ListValue)arguments[4];

				for (int index = 0; index < files.Count(); index++)
					library.Files.Add((Schema.FileReference)files[index]);

				for (int index = 0; index < requisites.Count(); index++)
					library.Libraries.Add((Schema.LibraryReference)requisites[index]);
			}
			
			if (arguments.Length >= 6)
			{
				#if NILPROPOGATION
				if (arguments[5] == null)
					return null;
				#endif
				library.Directory = (string)arguments[5];
			}

			return library;
		}
	}
	
	// operator LibraryDescriptorReadName(const AValue : LibraryDescriptor) : Name;
	public class SystemLibraryDescriptorReadNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ((Schema.Library)arguments[0]).Name;
		}
	}
	
	// operator LibraryDescriptorWriteName(const AValue : LibraryDescriptor, const AName : Name) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || arguments[1] == null)
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			library.Name = Schema.Object.EnsureUnrooted((string)arguments[1]);
			return library;
		}
	}
	
	// operator LibraryDescriptorReadDirectory(const AValue : LibraryDescriptor) : String;
	public class SystemLibraryDescriptorReadDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ((Schema.Library)arguments[0]).Directory;
		}
	}
	
	// operator LibraryDescriptorWriteDirectory(const AValue : LibraryDescriptor, const ADirectory : String) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteDirectoryNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || arguments[1] == null)
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			if (arguments[1] == null)
				library.Directory = null;
			else
				library.Directory = (string)arguments[1];
			return library;
		}
	}
	
	// operator LibraryDescriptorReadDefaultDeviceName(const AValue : LibraryDescriptor) : DefaultDeviceName;
	public class SystemLibraryDescriptorReadDefaultDeviceNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null))
				return null;
			#endif
			return ((Schema.Library)arguments[0]).DefaultDeviceName;
		}
	}
	
	// operator LibraryDescriptorWriteDefaultDeviceName(const AValue : LibraryDescriptor, const ADefaultDeviceName : DefaultDeviceName) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteDefaultDeviceNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || arguments[1] == null)
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			library.DefaultDeviceName = (string)arguments[1];
			return library;
		}
	}
	
	// operator LibraryDescriptorReadVersion(const AValue : LibraryDescriptor) : VersionNumber;
	public class SystemLibraryDescriptorReadVersionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif
			return ((Schema.Library)arguments[0]).Version;
		}
	}
	
	// operator LibraryDescriptorWriteVersion(const AValue : LibraryDescriptor, const AVersion : VersionNumber) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteVersionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			library.Version = (VersionNumber)arguments[1];
			return library;
		}
	}
	
	// operator LibraryDescriptorReadFiles(const AValue : LibraryDescriptor) : list(FileReference);
	public class SystemLibraryDescriptorReadFilesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null))
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			ListValue files = new ListValue(program.ValueManager, (Schema.IListType)_dataType);
			foreach (Schema.FileReference fileReference in library.Files)
				files.Add(fileReference.Clone());
			return files;
		}
	}
	
	// operator LibraryDescriptorWriteFiles(const AValue : LibraryDescriptor, const AFiles : list(FileReference)) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteFilesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || arguments[1] == null)
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			ListValue files = (ListValue)arguments[1];
			library.Files.Clear();
			for (int index = 0; index < files.Count(); index++)
				library.Files.Add((Schema.FileReference)((Schema.FileReference)files[index]).Clone());
			return library;
		}
	}
	
	// operator LibraryDescriptorReadRequisites(const AValue : LibraryDescriptor) : list(LibraryReference);
	public class SystemLibraryDescriptorReadRequisitesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null))
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			ListValue requisites = new ListValue(program.ValueManager, (Schema.IListType)_dataType);
			foreach (Schema.LibraryReference reference in library.Libraries)
				requisites.Add(reference.Clone());
			return requisites;
		}
	}
	
	// operator LibraryDescriptorWriteRequisites(const AValue : LibraryDescriptor, const ARequisites : list(String)) : LibraryDescriptor;
	public class SystemLibraryDescriptorWriteRequisitesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || arguments[1] == null)
				return null;
			#endif
			Schema.Library library = (Schema.Library)arguments[0];
			ListValue requisites = (ListValue)arguments[1];
			library.Libraries.Clear();
			for (int index = 0; index < requisites.Count(); index++)
				library.Libraries.Add((Schema.LibraryReference)((Schema.LibraryReference)requisites[index]).Clone());
			return library;
		}
	}

    // operator iEqual(const ALeftValue : LibraryDescriptor, const ARightValue : LibraryDescriptor) : Boolean;
    public class SystemLibraryDescriptorEqualNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if ((arguments[0] == null) || (arguments[1] == null))
				return null;
			#endif
			Schema.Library leftLibrary = (Schema.Library)arguments[0];
			Schema.Library rightLibrary = (Schema.Library)arguments[1];
			bool librariesEqual = 
				(leftLibrary.Name == rightLibrary.Name) && 
				(leftLibrary.Directory == rightLibrary.Directory) &&
				(leftLibrary.Files.Count == rightLibrary.Files.Count) &&
				(leftLibrary.Libraries.Count == rightLibrary.Libraries.Count);
				
			if (librariesEqual)
			{
				for (int index = 0; index < leftLibrary.Files.Count; index++)
					if (!(leftLibrary.Files[index].Equals(rightLibrary.Files[index])))
					{
						librariesEqual = false;
						break;
					}
					
				for (int index = 0; index < leftLibrary.Libraries.Count; index++)
					if (!(leftLibrary.Libraries[index].Equals(rightLibrary.Libraries[index])))
					{
						librariesEqual = false;
						break;
					}
			}
			
			return librariesEqual;
		}
    }
}

