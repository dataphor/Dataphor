/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	public class LibraryReference : System.Object, ICloneable
	{
		public LibraryReference() : base() {}
		public LibraryReference(string name) : base()
		{
			Name = name;
		}
		
		public LibraryReference(string name, VersionNumber version) : base()
		{
			Name = name;
			Version = version;
		}
		
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value == null ? String.Empty : value; }
		}
		
		private VersionNumber _version = new VersionNumber(-1, -1, -1, -1);
		public VersionNumber Version
		{
			get { return _version; }
			set { _version = value; }
		}
		
		public override bool Equals(object objectValue)
		{
			return objectValue is LibraryReference && (Compare(this, (LibraryReference)objectValue) == 0);
		}
		
		public override int GetHashCode()
		{
			return _name.GetHashCode() ^ _version.GetHashCode();
		}
		
		public object Clone()
		{
			return new LibraryReference(_name, _version);
		}

		public static int Compare(LibraryReference leftValue, LibraryReference rightValue)
		{
			int result = String.Compare(leftValue.Name, rightValue.Name);
			if (result == 0)
				result = VersionNumber.Compare(leftValue.Version, rightValue.Version);
			return result;
		}
	}

	#if USETYPEDLIST
	public class LibraryReferences : TypedList
	{
		public LibraryReferences() : base(typeof(LibraryReference)) {}
		
		public new LibraryReference this[int AIndex]
		{
			get { return (LibraryReference)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			if (Contains(((LibraryReference)AValue).Name))
				throw new SchemaException(SchemaException.Codes.DuplicateObjectName, ((LibraryReference)AValue).Name);
		}
	#else
	public class LibraryReferences : ValidatingBaseList<LibraryReference>
	{
		protected override void  Validate(LibraryReference tempValue)
		{
 			 //base.Validate(AValue);
 			 if (Contains(tempValue.Name))
 				throw new SchemaException(SchemaException.Codes.DuplicateObjectName, tempValue.Name);
		}
	#endif
	
		public LibraryReference this[string name]
		{
			get { return this[IndexOf(name)]; }
			set { this[IndexOf(name)] = value; }
		}
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (String.Compare(this[index].Name, name) == 0)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public static class Environments
	{
		public const string WindowsServer = "WindowsServer";
	}
		
	public class FileReference : System.Object, ICloneable
	{
		public FileReference() : base() {}
		public FileReference(string fileName) : base()
		{
			_fileName = fileName;
		}
		
		public FileReference(string fileName, bool isAssembly) : base()
		{
			_fileName = fileName;
			_isAssembly = isAssembly;
		}
		
		public FileReference(string fileName, bool isAssembly, IEnumerable<string> environments) : base()
		{
			_fileName = fileName;
			_isAssembly = isAssembly;
			_environments.AddRange(environments);
		}
		
		private string _fileName;
		public string FileName
		{
			get { return _fileName; }
			set { _fileName = value; }
		}
		
		private bool _isAssembly;
		public bool IsAssembly
		{
			get { return _isAssembly; }
			set { _isAssembly = value; }
		}
		
		private List<String> _environments = new List<String>();
		public List<String> Environments { get { return _environments; } }

		public override bool Equals(object objectValue)
		{
			FileReference localObjectValue = objectValue as FileReference;
			return 
				(localObjectValue != null) 
					&& String.Equals(_fileName, localObjectValue.FileName, StringComparison.OrdinalIgnoreCase) 
					&& (_isAssembly == localObjectValue.IsAssembly);
		}
		
		public override int GetHashCode()
		{
			return _fileName.GetHashCode();
		}
		
		public object Clone()
		{
			return new FileReference(_fileName, _isAssembly, _environments.ToArray());
		}
	}
	
	#if USETYPEDLIST
	public class FileReferences : TypedList
	{
		public FileReferences() : base(typeof(FileReference)) {}
		
		public new FileReference this[int AIndex]
		{
			get { return (FileReference)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			if (Contains(((FileReference)AValue).FileName))
				throw new SchemaException(SchemaException.Codes.DuplicateObjectName, ((FileReference)AValue).FileName);
		}
	#else
	public class FileReferences : ValidatingBaseList<FileReference>
	{
		protected override void Validate(FileReference tempValue)
		{
			//base.Validate(AValue);
			if (Contains(tempValue.FileName))
				throw new SchemaException(SchemaException.Codes.DuplicateObjectName, tempValue.FileName);
		}
	#endif
		
		public int IndexOf(string fileName)
		{
			for (int index = 0; index < Count; index++)
				if (String.Equals(this[index].FileName, fileName, StringComparison.OrdinalIgnoreCase))
					return index;
			return -1;
		}

		public bool Contains(string fileName)
		{
			return IndexOf(fileName) >= 0;
		}
		
		public FileReference this[string fileName]
		{
			get { return this[IndexOf(fileName)]; }
		}
	}
	
	public class LibraryInfo : System.Object, ICloneable
	{
		public LibraryInfo() : base() {}
		public LibraryInfo(string name, bool isSuspect, string suspectReason)
		{
			_name = name;
			_isSuspect = isSuspect;
			_suspectReason = suspectReason;
		}
		
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		
		private bool _isSuspect;
		public bool IsSuspect
		{
			get { return _isSuspect; }
			set 
			{
				_isSuspect = value; 
				if (!_isSuspect)
					_suspectReason = null;
			}
		}
		
		private string _suspectReason;
		public string SuspectReason
		{
			get { return _suspectReason; }
			set { _suspectReason = value; }
		}
		
		#region ICloneable Members

		public object Clone()
		{
			return new LibraryInfo(_name, _isSuspect, _suspectReason);
		}

		#endregion

		public void SaveToStream(Stream stream)
		{
			new Serializer().Serialize(stream, this);
		}
		
		public static LibraryInfo LoadFromStream(Stream stream)
		{
			return new Deserializer().Deserialize(stream, null) as LibraryInfo;
		}
	}
	
	// Library
	// A library is either a directory in the LibraryDirectory of the DAE, 
	//	or a directory explicitly specified as part of the library definition.
	// This directory will have a file called <library name>.d4l which is a serialization of this structure
	// detailing the assemblies in the library and the required libraries
	// Registering a library ->
	//	Ensure that each required library is registered
	//	Copy all files into the DAE's bin (or the GAC)
	//  Register each assembly with the DAE
	//	run the register.d4 script if it exists in the library
	//		catalog objects created in this script are part of this library
	public class Library : Schema.Object, ICloneable
	{
		public Library() : base(String.Empty) {}
		public Library(string name) : base(name) {}
		public Library(string name, VersionNumber version) : base(name)
		{
			_version = version;
		}
		
		public Library(string name, VersionNumber version, string defaultDeviceName) : base(name)
		{
			_version = version;
			DefaultDeviceName = defaultDeviceName;
		}
		
		public Library(string name, string directory, VersionNumber version, string defaultDeviceName) : base(name)
		{
			Directory = directory;
			_version = version;
			DefaultDeviceName = defaultDeviceName;
		}
		
		private VersionNumber _version = new VersionNumber(-1, -1, -1, -1);
		public VersionNumber Version
		{
			get { return _version; }
			set { _version = value; }
		}
		
		private string _defaultDeviceName = String.Empty;
		public string DefaultDeviceName
		{
			get { return _defaultDeviceName; }
			set { _defaultDeviceName = value == null ? String.Empty : value; }
		}
		
		private string _directory = String.Empty;
		[Publish(PublishMethod.None)]
		public string Directory
		{
			get { return _directory; }
			set 
			{ 
				if ((value == null) || (value == String.Empty))
					_directory = String.Empty; 
				else
					_directory = value;
			}
		}
		
		private bool _isSuspect;
		[Publish(PublishMethod.None)]
		public bool IsSuspect
		{
			get { return _isSuspect; }
			set 
			{
				_isSuspect = value; 
				if (!_isSuspect)
					_suspectReason = null;
			}
		}
		
		private string _suspectReason;
		[Publish(PublishMethod.None)]
		public string SuspectReason
		{
			get { return _suspectReason; }
			set { _suspectReason = value; }
		}
		
		private string ListToString(List list)
		{
			StringBuilder stringValue = new StringBuilder();
			for (int index = 0; index < list.Count; index++)
			{
				if (index > 0)
					stringValue.Append(";");
				stringValue.Append((string)list[index]);
			}
			return stringValue.ToString();
		}
		
		private void StringToList(string stringValue, List list)
		{
			list.Clear();
			if (stringValue.Length > 0)
			{
				string[] strings = stringValue.Split(';');
				for (int index = 0; index < strings.Length; index++)
					list.Add(strings[index]);
			}
		}

		// A list of FileReference objects. These files will be copied into the DAE's startup directory. If IsAssembly is true, the file will be registered as an assembly with the DAE.
		private FileReferences _files = new FileReferences();
		public FileReferences Files { get { return _files; } }
		
		public string AssembliesAsString
		{
			get { return String.Empty; }
			set 
			{
				if ((value != null) && (value != String.Empty))
				{ 
					List list = new List();
					StringToList(value, list); 
					foreach (string stringValue in list)
						_files.Add(new FileReference(stringValue, true));
				}
			}
		}
		
		// A list of LibraryReference objects.
		private LibraryReferences _libraries = new LibraryReferences();
		public LibraryReferences Libraries { get { return _libraries; } }
		
		public string LibrariesAsString
		{
			get { return String.Empty; }
			set 
			{
				if ((value != null) && (value != String.Empty))
				{
					List list = new List();
					StringToList(value, list);
					foreach (string stringValue in list)
						_libraries.Add(new LibraryReference(stringValue));
				}
			}
		}
		
		public string Settings
		{
			get 
			{
				if (MetaData == null)
					return String.Empty;
					
				StringBuilder result = new StringBuilder();
				#if USEHASHTABLEFORTAGS
				foreach (Tag tag in MetaData.Tags)
				{
				#else
				Tag tag;
				for (int index = 0; index < MetaData.Tags.Count; index++)
				{
					tag = MetaData.Tags[index];
				#endif
					if (result.Length > 0)
						result.Append(";");
					result.AppendFormat(@"'{0}'='{1}'", tag.Name.Replace("'", "''"), tag.Value.Replace("'", "''"));
				}
				
				return result.ToString();
			}
			set
			{
				MetaData = new MetaData();

				Lexer FLexer = new Lexer(value);
				while (FLexer[1].Type != TokenType.EOF)
				{
					FLexer.NextToken();
					string tagName = FLexer[0].AsString;
					FLexer.NextToken().CheckSymbol(Keywords.Equal);
					FLexer.NextToken();
					MetaData.Tags.Add(new Tag(tagName, FLexer[0].AsString));
					if (FLexer.PeekTokenSymbol(1) == Keywords.StatementTerminator)
						FLexer.NextToken();
				}
			}
		}
		
		public void SaveToStream(Stream stream)
		{
			new Serializer().Serialize(stream, this);
		}
		
		public object Clone()
		{
			Library library = new Library(Name, _version, _defaultDeviceName);
			library.Directory = _directory;
			library.MergeMetaData(MetaData);
			library.IsSuspect = _isSuspect;
			library.SuspectReason = _suspectReason;
			foreach (LibraryReference libraryReference in _libraries)
				library.Libraries.Add((LibraryReference)libraryReference.Clone());
			foreach (FileReference fileReference in _files)
				library.Files.Add((FileReference)fileReference.Clone());
			return library;
		}
	}
	
	public delegate void LibraryNotifyEvent(Program AProgram, string ALibraryName);
	public delegate void LibraryRenameEvent(Program AProgram, string AOldLibraryName, string ANewLibraryName);
	
    /// <remarks> Libraries </remarks>
	public class Libraries : Objects
    {
		protected override void Validate(Object objectValue)
		{
			if (!(objectValue is Library))
				throw new SchemaException(SchemaException.Codes.ObjectContainer);
			base.Validate(objectValue);
		}

		public new Library this[int index]
		{
			get { return (Library)base[index]; }
			set { base[index] = value; }
		}

		public new Library this[string name]
		{
			get { return (Library)base[name]; }
			set { base[name] = value; }
		}

		/// <summary>Occurs whenever a library is created in the DAE.</summary>		
		public event LibraryNotifyEvent OnLibraryCreated;
		public void DoLibraryCreated(Program program, string libraryName)
		{
			if (OnLibraryCreated != null)
				OnLibraryCreated(program, libraryName);
		}
		
		/// <summary>Occurs whenever a library is added to the list of available libraries in the DAE.</summary>
		public event LibraryNotifyEvent OnLibraryAdded;
		public void DoLibraryAdded(Program program, string libraryName)
		{
			if (OnLibraryAdded != null)
				OnLibraryAdded(program, libraryName);
		}
		
		/// <summary>Occurs whenever a library is removed from the list of available libraries in the DAE.</summary>
		public event LibraryNotifyEvent OnLibraryRemoved;
		public void DoLibraryRemoved(Program program, string libraryName)
		{
			if (OnLibraryRemoved != null)
				OnLibraryRemoved(program, libraryName);
		}
		
		/// <summary>Occurs whenever a library is renamed in the DAE.</summary>
		public event LibraryRenameEvent OnLibraryRenamed;
		public void DoLibraryRenamed(Program program, string oldLibraryName, string newLibraryName)
		{
			if (OnLibraryRenamed != null)
				OnLibraryRenamed(program, oldLibraryName, newLibraryName);
		}
		
		/// <summary>Occurs whenever a library is deleted in the DAE.</summary>
		public event LibraryNotifyEvent OnLibraryDeleted;
		public void DoLibraryDeleted(Program program, string libraryName)
		{
			if (OnLibraryDeleted != null)
				OnLibraryDeleted(program, libraryName);
		}

		/// <summary>Occurs whenever a library is registered or loaded in the DAE.</summary>
		public event LibraryNotifyEvent OnLibraryLoaded;
		public void DoLibraryLoaded(Program program, string libraryName)
		{
			if (OnLibraryLoaded != null)
				OnLibraryLoaded(program, libraryName);
		}

		/// <summary>Occurs whenever a library is unregistered or unloaded in the DAE.</summary>
		public event LibraryNotifyEvent OnLibraryUnloaded;
		public void DoLibraryUnloaded(Program program, string libraryName)
		{
			if (OnLibraryUnloaded != null)
				OnLibraryUnloaded(program, libraryName);
		}
    }

	public class LoadedLibrary : Schema.CatalogObject
	{
		public LoadedLibrary(string name) : base(name) {}
		
		private List _assemblies = new List();
		public List Assemblies { get { return _assemblies; } }

		[Reference]
		private LoadedLibraries _requiredLibraries = new LoadedLibraries();
		public LoadedLibraries RequiredLibraries { get { return _requiredLibraries; } }
		
		[Reference]
		private LoadedLibraries _requiredByLibraries = new LoadedLibraries();		
		public LoadedLibraries RequiredByLibraries { get { return _requiredByLibraries; } }
		
		private NameResolutionPath _nameResolutionPath;
		
		protected void GetNameResolutionPath(NameResolutionPath path, int level, Schema.LoadedLibrary library)
		{
			while (path.Count <= level)
				path.Add(new Schema.LoadedLibraries());
				
			if (!path.Contains(library))
				path[level].Add(library);
				
			foreach (LoadedLibrary localLibrary in library.RequiredLibraries)	
				GetNameResolutionPath(path, level + 1, localLibrary);
		}
		
		public NameResolutionPath GetNameResolutionPath(LoadedLibrary systemLibrary)
		{
			if (_nameResolutionPath == null)
			{
				/* Depth first traversal, tracking level, add every unique required library at the level corresponding to the depth of the library in the dependency graph */
				_nameResolutionPath = new NameResolutionPath();
				GetNameResolutionPath(_nameResolutionPath, 0, this);

				/* Remove all levels with no libraries */
				for (int index = _nameResolutionPath.Count - 1; index >= 0; index--)
					if (_nameResolutionPath[index].Count == 0)
						_nameResolutionPath.RemoveAt(index);
						
				/* Ensure that system is in the path */
				if (!_nameResolutionPath.Contains(systemLibrary))
				{
					_nameResolutionPath.Add(new Schema.LoadedLibraries());
					_nameResolutionPath[_nameResolutionPath.Count - 1].Add(systemLibrary);
				}
			}
								
			return _nameResolutionPath;
		}
		
		public void ClearNameResolutionPath()
		{
			_nameResolutionPath = null;
		}
		
		/// <summary>Returns true if ALibrary is the same as this library, or is required by this library.</summary>
		public bool IsRequiredLibrary(LoadedLibrary library)
		{
			if (this.Equals(library))
				return true;
			foreach (LoadedLibrary localLibrary in _requiredLibraries)
				if (localLibrary.IsRequiredLibrary(library))
					return true;
			return false;
		}
		
		public void AttachLibrary()
		{
			foreach (LoadedLibrary library in _requiredLibraries)
				if (!library.RequiredByLibraries.Contains(this))
					library.RequiredByLibraries.Add(this);
		}
		
		public void DetachLibrary()
		{
			foreach (LoadedLibrary library in _requiredLibraries)
				if (library.RequiredByLibraries.Contains(this))
					library.RequiredByLibraries.Remove(this);
		}
	}

    /// <remarks> LoadedLibraries </remarks>
	public class LoadedLibraries : Objects<LoadedLibrary>
    {
    }

	/// <summary>
	///	NameResolutionPath is a list of LoadedLibraries lists.  
	///	Each element is the unique set of library dependencies at the level given by the index in the list above the library.
	///	The first element contains only the library of the name resolution path.
	///	</summary>    
    public class NameResolutionPath : List
    {
		public new LoadedLibraries this[int index]
		{
			get { return (LoadedLibraries)base[index]; }
			set { base[index] = value; }
		}
		
		public bool Contains(LoadedLibrary library)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].Contains(library))
					return true;
			return false;
		}
    }
}

