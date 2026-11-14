/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.Frontend.Server.Device
{
	using System;
	using System.IO;
	using System.Text;
	using System.Collections;
	using System.Collections.Specialized;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public class DocumentType : System.Object
	{
		public DocumentType() : base() {}
		public DocumentType(string iD, string description, string dataType) : base() 
		{
			_iD = iD;
			_description = description;
			_dataType = dataType;
		}

		private string _iD;
		public string ID 
		{ 
			get { return _iD; } 
			set { _iD = value; }
		}
		
		private string _description;
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}
		
		public string _dataType;
		public string DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}
		
		private List _designers = new List();
		[Publish(PublishMethod.None)]
		public List Designers { get { return _designers; } }
		
		private string DesignersToString()
		{
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < _designers.Count; index++)
			{
				if (index > 0)
					builder.Append(";");
				builder.Append((string)_designers[index]);
			}
				
			return builder.ToString();
		}
		
		private void StringToDesigners(string tempValue)
		{
			_designers.Clear();
			if (tempValue.Length > 0)
			{
				string[] designers = tempValue.Split(';');
				for (int index = 0; index < designers.Length; index++)
					_designers.Add(designers[index]);
			}
		}
		
		public string DesignersAsString
		{
			get { return DesignersToString(); }
			set { StringToDesigners(value); }
		}
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is DocumentType) && (((DocumentType)objectValue).ID == _iD);
		}
		
		public override int GetHashCode()
		{
			return _iD.GetHashCode();
		}
	}
	
	public class DocumentTypes : HashtableList<string, DocumentType>
	{
		public DocumentTypes() : base() {}
		
		public new DocumentType this[string iD]
		{
			get
			{
				DocumentType result;
				if (!TryGetValue(iD, out result))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DocumentTypeNotFound, iD);
				return result;
			}
		}
		
		public override int Add(object tempValue)
		{
			var document = (DocumentType)tempValue;
			Add(document.ID, document);
			return IndexOf(tempValue);
		}
	}
	
	public class DocumentTypesContainer : System.Object
	{
		public DocumentTypesContainer() : base()
		{
			_documentTypes = new DocumentTypes();
		}
		
		public DocumentTypesContainer(DocumentTypes documentTypes) : base()
		{
			_documentTypes = documentTypes;
		}
		
		private DocumentTypes _documentTypes;
		public DocumentTypes DocumentTypes { get { return _documentTypes; } }
	}
		
	public class Document : Schema.Object
	{
		public Document(string name) : base(name) {}
		
		public Document(string name, DocumentType documentType) : base(name)
		{
			_documentType = documentType;
		}
		
		[Reference]
		private DocumentType _documentType;
		public DocumentType DocumentType
		{
			get { return _documentType; }
			set { _documentType = value; }
		}
		
		public string GetFileName()
		{
			return String.Format("{0}.{1}", Name, _documentType.ID);
		}
		
		public static bool IsValidDocumentName(string fileName)
		{
			if ((fileName == null) || (fileName == String.Empty))
				return false;
				
			if (fileName.IndexOf(Keywords.Qualifier) >= 0)
			{
				string[] names = fileName.Split('.');
				for (int index = 0; index < names.Length; index++)
					if (!Parser.IsValidIdentifier(names[index]))
						return false;
				return true;
			}
			else
				return Parser.IsValidIdentifier(fileName);
		}
		
		public static Document FromFileName(FrontendDevice frontendDevice, string fileName)
		{
			string extension = Path.GetExtension(fileName);
			if ((extension == null) || (extension == String.Empty) || (extension == "."))
				return null;

			extension = extension.Substring(1, extension.Length - 1).ToLower();
			DocumentType documentType = null;
			if (frontendDevice.DocumentTypes.Contains(extension))
				documentType = frontendDevice.DocumentTypes[extension];
			else
				return null;
				
			string localFileName = Path.GetFileNameWithoutExtension(fileName);
			if (!IsValidDocumentName(localFileName))
				return null;
				
			return new Document(localFileName, documentType);
		}
		
		public void CheckDataType(Plan plan, Schema.ScalarType expectedDataType)
		{
			Schema.Object dataTypeObject = Compiler.ResolveCatalogIdentifier(plan, DocumentType.DataType, true);
			if 
			(
				!(dataTypeObject is Schema.ScalarType) || 
				(
					(expectedDataType != plan.DataTypes.SystemBinary) && 
					!((Schema.ScalarType)dataTypeObject).Is(expectedDataType)
				)
			)
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, dataTypeObject.Name, expectedDataType.Name);
		}
	}
	
	public class Documents : Schema.Objects
	{
		public new Document this[int index]
		{
			get { return (Document)base[index]; }
			set { base[index] = value; }
		}
		
		public new Document this[string name]
		{
			get { return (Document)base[name]; }
			set { base[name] = value; }
		}
		
		protected Hashtable _insensitiveIndex = new Hashtable(StringComparer.OrdinalIgnoreCase);
		
		protected override void Validate(Schema.Object objectValue)
		{
			base.Validate(objectValue);
			if (_insensitiveIndex.Contains(objectValue.Name))
				throw new SchemaException(SchemaException.Codes.DuplicateObjectName, objectValue.Name);
		}
		
		protected override void Adding(Schema.Object objectValue, int index)
		{
			base.Adding(objectValue, index);
			_insensitiveIndex.Add(objectValue.Name, objectValue);
		}
		
		protected override void Removing(Schema.Object objectValue, int index)
		{
			_insensitiveIndex.Remove(objectValue.Name);
			base.Removing(objectValue, index);
		}
		
		public Document GetDocumentFromFileName(string fileName)
		{
			return _insensitiveIndex[fileName] as Document;
		}
	}

	public delegate void LibraryEventHandler(FrontendLibrary ALibrary);	
	public delegate void DocumentEventHandler(FrontendLibrary ALibrary, Document ADocument);

	public class FrontendLibrary : Schema.Object
	{
		public const string DocumentsName = @"Documents";
		
		public FrontendLibrary(Program program, string name) : base(name) 
		{
			_frontendDevice = FrontendDevice.GetFrontendDevice(program);
			_directoryName = program.Catalog.Libraries[name].GetLibraryDirectory(((DAE.Server.Server)program.ServerProcess.ServerSession.Server).LibraryDirectory);
			_documentsDirectoryName = Path.Combine(_directoryName, DocumentsName);
			LoadDocuments();
			if (Directory.Exists(_directoryName))
			{
				if (!Directory.Exists(_documentsDirectoryName))
					Directory.CreateDirectory(_documentsDirectoryName);
				#if USEWATCHERS
				FWatcher = new FileSystemWatcher(FDirectoryName);
				FWatcher.IncludeSubdirectories = false;
				FWatcher.Changed += new FileSystemEventHandler(DirectoryChanged);
				FWatcher.Created += new FileSystemEventHandler(DirectoryChanged);
				FWatcher.Deleted += new FileSystemEventHandler(DirectoryChanged);
				FWatcher.Renamed += new RenamedEventHandler(DirectoryRenamed);
				FWatcher.EnableRaisingEvents = true;
				#endif
			}
		}

		public void Close(Program program)
		{
			#if USEWATCHERS
			if (FWatcher != null)
			{
				FWatcher.Changed -= new FileSystemEventHandler(DirectoryChanged);
				FWatcher.Created -= new FileSystemEventHandler(DirectoryChanged);
				FWatcher.Deleted -= new FileSystemEventHandler(DirectoryChanged);
				FWatcher.Renamed -= new RenamedEventHandler(DirectoryRenamed);
				FWatcher.Dispose();
				FWatcher = null;
			}
			#endif
		}
		
		[Reference]
		private FrontendDevice _frontendDevice;
		public FrontendDevice FrontendDevice { get { return _frontendDevice; } }

		private Documents _documents = new Documents();
		public Documents Documents { get { return _documents; } }
		
		private string _directoryName;
		
		private string _documentsDirectoryName;
		public string DocumentsDirectoryName { get { return _documentsDirectoryName; } }

		#if USEWATCHERS
		private FileSystemWatcher FWatcher;
		#endif
		
		public bool MaintainedUpdate
		{
			get 
			{ 
				#if USEWATCHERS
				return (FWatcher != null) && !FWatcher.EnableRaisingEvents;
				#else
				return false;
				#endif
			}
			set 
			{ 
				#if USEWATCHERS
				if (FWatcher != null)
					FWatcher.EnableRaisingEvents = !value; 
				#endif
			}
		}

		#if USEWATCHERS
		private void DirectoryChanged(object ASender, FileSystemEventArgs AArgs)
		{
			if ((AArgs.ChangeType & WatcherChangeTypes.Created) != 0)
			{
				Document LDocument = Document.FromFileName(FFrontendDevice, GetActualFileName(AArgs.FullPath));
				lock (FDocuments)
				{
					if ((LDocument != null) && !FDocuments.Contains(LDocument))
					{
						FDocuments.Add(LDocument);
						DoDocumentCreated(LDocument);
					}
				}
			}
			else if ((AArgs.ChangeType & WatcherChangeTypes.Deleted) != 0)
			{
				lock (FDocuments)
				{
					Document LDocument = FDocuments.GetDocumentFromFileName(Path.GetFileNameWithoutExtension(AArgs.FullPath));
					if (LDocument != null)
					{
						FDocuments.Remove(LDocument);
						DoDocumentDeleted(LDocument);
					}
				}
			}
		}
		
		private string GetActualFileName(string AFileName)
		{
			string[] LFileNames = Directory.GetFiles(Path.Combine(Path.GetPathRoot(AFileName), Path.GetDirectoryName(AFileName)), Path.GetFileName(AFileName));
			if (LFileNames.Length != 1)
				throw new SchemaException(SchemaException.Codes.AmbiguousObjectName, AFileName);
			return LFileNames[0];
		}
		
		private void DirectoryRenamed(object ASender, RenamedEventArgs AArgs)
		{
			Document LDocument = Document.FromFileName(FFrontendDevice, GetActualFileName(AArgs.FullPath));
			lock (FDocuments)
			{
				Document LOldDocument = FDocuments.GetDocumentFromFileName(Path.GetFileNameWithoutExtension(AArgs.OldFullPath));
				if (LOldDocument != null)
				{
					FDocuments.Remove(LOldDocument);
					DoDocumentDeleted(LOldDocument);
				}
				
				FDocuments.Add(LDocument);
				DoDocumentCreated(LDocument);
			}
		}
		
		public event DocumentEventHandler OnDocumentCreated;
		public void DoDocumentCreated(Document ADocument)
		{
			if (OnDocumentCreated != null)
				OnDocumentCreated(this, ADocument);
		}
		
		public event DocumentEventHandler OnDocumentDeleted;
		public void DoDocumentDeleted(Document ADocument)
		{
			if (OnDocumentDeleted != null)
				OnDocumentDeleted(this, ADocument);
		}
		#endif

		// Should only be called on initial library creation
		public void LoadDocuments()
		{
			lock (_documents)
			{
				_documents.Clear();
				if (Directory.Exists(_documentsDirectoryName))
				{
					string[] fileNames = Directory.GetFiles(_documentsDirectoryName);
					for (int index = 0; index < fileNames.Length; index++)
					{
						Document document = Document.FromFileName(_frontendDevice, fileNames[index]);
						if ((document != null) && !_documents.ContainsName(document.Name))
							_documents.Add(document);
					}
				}
			}
		}
		
		private Serializer _serializer = new Serializer();
		private Deserializer _deserializer = new Deserializer();
	}
	
	public class FrontendLibraries : Schema.Objects
	{
		public new FrontendLibrary this[int index]
		{
			get { return (FrontendLibrary)base[index]; }
			set { base[index] = value; }
		}
		
		public new FrontendLibrary this[string name]
		{
			get { return (FrontendLibrary)base[name]; }
			set { base[name] = value; }
		}
	}	

	public class Designer : System.Object
	{
		public Designer() : base() {}
		public Designer(string iD, string description, string className) : base() 
		{
			_iD = iD;
			_description = description;
			_className = className;
		}
		
		private string _iD;
		public string ID 
		{
			get { return _iD; } 
			set { _iD = value; }
		}
		
		private string _description;
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		private string _className;
		public string ClassName
		{
			get { return _className; }
			set { _className = value; }
		}
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is Designer) && (((Designer)objectValue).ID == _iD);
		}
		
		public override int GetHashCode()
		{
			return _iD.GetHashCode();
		}
	}
	
	public class Designers : HashtableList<string, Designer>
	{
		public Designers() : base() {}
		
		public new Designer this[string iD]
		{
			get
			{
				Designer result;
				if (!TryGetValue(iD, out result))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerNotFound, iD);
				return result;
			}
		}
		
		public override int Add(object tempValue)
		{
			Designer localTempValue = (Designer)tempValue;
			Add(localTempValue.ID, localTempValue);
			return IndexOf(localTempValue);
		}
	}
	
	public class DesignersContainer : System.Object
	{
		public DesignersContainer() : base()
		{
			_designers = new Designers();
		}
		
		public DesignersContainer(Designers designers) : base()
		{
			_designers = designers;
		}
		
		private Designers _designers;
		public Designers Designers { get { return _designers; } }
	}
}

