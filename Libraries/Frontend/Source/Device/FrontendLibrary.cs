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
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public class DocumentType : System.Object
	{
		public DocumentType() : base() {}
		public DocumentType(string AID, string ADescription, string ADataType) : base() 
		{
			FID = AID;
			FDescription = ADescription;
			FDataType = ADataType;
		}

		private string FID;
		public string ID 
		{ 
			get { return FID; } 
			set { FID = value; }
		}
		
		private string FDescription;
		public string Description
		{
			get { return FDescription; }
			set { FDescription = value; }
		}
		
		public string FDataType;
		public string DataType
		{
			get { return FDataType; }
			set { FDataType = value; }
		}
		
		private List FDesigners = new List();
		[Publish(PublishMethod.None)]
		public List Designers { get { return FDesigners; } }
		
		private string DesignersToString()
		{
			StringBuilder LBuilder = new StringBuilder();
			for (int LIndex = 0; LIndex < FDesigners.Count; LIndex++)
			{
				if (LIndex > 0)
					LBuilder.Append(";");
				LBuilder.Append((string)FDesigners[LIndex]);
			}
				
			return LBuilder.ToString();
		}
		
		private void StringToDesigners(string AValue)
		{
			FDesigners.Clear();
			if (AValue.Length > 0)
			{
				string[] LDesigners = AValue.Split(';');
				for (int LIndex = 0; LIndex < LDesigners.Length; LIndex++)
					FDesigners.Add(LDesigners[LIndex]);
			}
		}
		
		public string DesignersAsString
		{
			get { return DesignersToString(); }
			set { StringToDesigners(value); }
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is DocumentType) && (((DocumentType)AObject).ID == FID);
		}
		
		public override int GetHashCode()
		{
			return FID.GetHashCode();
		}
	}
	
	public class DocumentTypes : HashtableList
	{
		public DocumentTypes() : base() {}
		
		public DocumentType this[string AID]
		{
			get
			{
				DocumentType LResult = (DocumentType)base[AID];
				if (LResult == null)
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DocumentTypeNotFound, AID);
				return LResult;
			}
		}
		
		public new DocumentType this[int AIndex] { get { return (DocumentType)base[AIndex]; } }
		
		public override int Add(object AValue)
		{
			DocumentType LValue = AValue as DocumentType;
			if (LValue != null)
			{
				Add(LValue);
				return IndexOf(LValue.ID);
			}
			else
				return -1;
		}
		
		public void Add(DocumentType AValue)
		{
			Add(AValue.ID, AValue);
		}
	}
	
	public class DocumentTypesContainer : System.Object
	{
		public DocumentTypesContainer() : base()
		{
			FDocumentTypes = new DocumentTypes();
		}
		
		public DocumentTypesContainer(DocumentTypes ADocumentTypes) : base()
		{
			FDocumentTypes = ADocumentTypes;
		}
		
		private DocumentTypes FDocumentTypes;
		public DocumentTypes DocumentTypes { get { return FDocumentTypes; } }
	}
		
	public class Document : Schema.Object
	{
		public Document(string AName) : base(AName) {}
		
		public Document(string AName, DocumentType ADocumentType) : base(AName)
		{
			FDocumentType = ADocumentType;
		}
		
		[Reference]
		private DocumentType FDocumentType;
		public DocumentType DocumentType
		{
			get { return FDocumentType; }
			set { FDocumentType = value; }
		}
		
		public string GetFileName()
		{
			return String.Format("{0}.{1}", Name, FDocumentType.ID);
		}
		
		public static bool IsValidDocumentName(string AFileName)
		{
			if ((AFileName == null) || (AFileName == String.Empty))
				return false;
				
			if (AFileName.IndexOf(Keywords.Qualifier) >= 0)
			{
				string[] LNames = AFileName.Split('.');
				for (int LIndex = 0; LIndex < LNames.Length; LIndex++)
					if (!Parser.IsValidIdentifier(LNames[LIndex]))
						return false;
				return true;
			}
			else
				return Parser.IsValidIdentifier(AFileName);
		}
		
		public static Document FromFileName(FrontendDevice AFrontendDevice, string AFileName)
		{
			string LExtension = Path.GetExtension(AFileName);
			if ((LExtension == null) || (LExtension == String.Empty) || (LExtension == "."))
				return null;

			LExtension = LExtension.Substring(1, LExtension.Length - 1).ToLower();
			DocumentType LDocumentType = null;
			if (AFrontendDevice.DocumentTypes.Contains(LExtension))
				LDocumentType = AFrontendDevice.DocumentTypes[LExtension];
			else
				return null;
				
			string LFileName = Path.GetFileNameWithoutExtension(AFileName);
			if (!IsValidDocumentName(LFileName))
				return null;
				
			return new Document(LFileName, LDocumentType);
		}
		
		public void CheckDataType(ServerProcess AProcess, Schema.ScalarType AExpectedDataType)
		{
			Schema.Object LDataTypeObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, DocumentType.DataType, true);
			if 
			(
				!(LDataTypeObject is Schema.ScalarType) || 
				(
					(AExpectedDataType != AProcess.DataTypes.SystemBinary) && 
					!((Schema.ScalarType)LDataTypeObject).Is(AExpectedDataType)
				)
			)
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, LDataTypeObject.Name, AExpectedDataType.Name);
		}
	}
	
	public class Documents : Schema.Objects
	{
		public new Document this[int AIndex]
		{
			get { return (Document)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public new Document this[string AName]
		{
			get { return (Document)base[AName]; }
			set { base[AName] = value; }
		}
		
		protected Hashtable FInsensitiveIndex = new Hashtable(StringComparer.OrdinalIgnoreCase);
		
		protected override void Validate(Schema.Object AObject)
		{
			base.Validate(AObject);
			if (FInsensitiveIndex.Contains(AObject.Name))
				throw new SchemaException(SchemaException.Codes.DuplicateObjectName, AObject.Name);
		}
		
		protected override void Adding(Schema.Object AObject, int AIndex)
		{
			base.Adding(AObject, AIndex);
			FInsensitiveIndex.Add(AObject.Name, AObject);
		}
		
		protected override void Removing(Schema.Object AObject, int AIndex)
		{
			FInsensitiveIndex.Remove(AObject.Name);
			base.Removing(AObject, AIndex);
		}
		
		public Document GetDocumentFromFileName(string AFileName)
		{
			return FInsensitiveIndex[AFileName] as Document;
		}
	}

	public delegate void LibraryEventHandler(FrontendLibrary ALibrary);	
	public delegate void DocumentEventHandler(FrontendLibrary ALibrary, Document ADocument);

	public class FrontendLibrary : Schema.Object
	{
		public const string CDocumentsName = @"Documents";
		
		public FrontendLibrary(ServerProcess AProcess, string AName) : base(AName) 
		{
			FFrontendDevice = FrontendDevice.GetFrontendDevice(AProcess);
			FDirectoryName = AProcess.Plan.Catalog.Libraries[AName].GetLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
			FDocumentsDirectoryName = Path.Combine(FDirectoryName, CDocumentsName);
			LoadDocuments();
			if (Directory.Exists(FDirectoryName))
			{
				if (!Directory.Exists(FDocumentsDirectoryName))
					Directory.CreateDirectory(FDocumentsDirectoryName);
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

		public void Close(ServerProcess AProcess)
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
		private FrontendDevice FFrontendDevice;
		public FrontendDevice FrontendDevice { get { return FFrontendDevice; } }

		private Documents FDocuments = new Documents();
		public Documents Documents { get { return FDocuments; } }
		
		private string FDirectoryName;
		
		private string FDocumentsDirectoryName;
		public string DocumentsDirectoryName { get { return FDocumentsDirectoryName; } }

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
			lock (FDocuments)
			{
				FDocuments.Clear();
				if (Directory.Exists(FDocumentsDirectoryName))
				{
					string[] LFileNames = Directory.GetFiles(FDocumentsDirectoryName);
					for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
					{
						Document LDocument = Document.FromFileName(FFrontendDevice, LFileNames[LIndex]);
						if ((LDocument != null) && !FDocuments.ContainsName(LDocument.Name))
							FDocuments.Add(LDocument);
					}
				}
			}
		}
		
		private Serializer FSerializer = new Serializer();
		private Deserializer FDeserializer = new Deserializer();
	}
	
	public class FrontendLibraries : Schema.Objects
	{
		public new FrontendLibrary this[int AIndex]
		{
			get { return (FrontendLibrary)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public new FrontendLibrary this[string AName]
		{
			get { return (FrontendLibrary)base[AName]; }
			set { base[AName] = value; }
		}
	}	

	public class Designer : System.Object
	{
		public Designer() : base() {}
		public Designer(string AID, string ADescription, string AClassName) : base() 
		{
			FID = AID;
			FDescription = ADescription;
			FClassName = AClassName;
		}
		
		private string FID;
		public string ID 
		{
			get { return FID; } 
			set { FID = value; }
		}
		
		private string FDescription;
		public string Description
		{
			get { return FDescription; }
			set { FDescription = value; }
		}

		private string FClassName;
		public string ClassName
		{
			get { return FClassName; }
			set { FClassName = value; }
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is Designer) && (((Designer)AObject).ID == FID);
		}
		
		public override int GetHashCode()
		{
			return FID.GetHashCode();
		}
	}
	
	public class Designers : HashtableList
	{
		public Designers() : base() {}
		
		public Designer this[string AID]
		{
			get
			{
				Designer LResult = (Designer)base[AID];
				if (LResult == null)
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerNotFound, AID);
				return LResult;
			}
		}
		
		public new Designer this[int AIndex] { get { return (Designer)base[AIndex]; } }
		
		public override int Add(object AValue)
		{
			Designer LValue = AValue as Designer;
			if (LValue != null)
			{
				Add(LValue);
				return IndexOf(LValue.ID);
			}
			else
				return -1;
		}
		
		public void Add(Designer AValue)
		{
			Add(AValue.ID, AValue);
		}
	}
	
	public class DesignersContainer : System.Object
	{
		public DesignersContainer() : base()
		{
			FDesigners = new Designers();
		}
		
		public DesignersContainer(Designers ADesigners) : base()
		{
			FDesigners = ADesigners;
		}
		
		private Designers FDesigners;
		public Designers Designers { get { return FDesigners; } }
	}
}

