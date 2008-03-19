/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;

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
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.Frontend.Server;

namespace Alphora.Dataphor.Frontend.Server.Device
{
	/*
		Tables ->
		
			DocumentTypes -> Initially populated by the system register script, updatable thereafter
			DesignerTypes -> Initially populated by the system register script, updatable thereafter
			DocumentTypeDesigners -> Initially populated by the system register script, updatable thereafter
			Libraries -> Corresponds directly with a DAE library.
			Applications, WindowsClientApplications -> Persisted with BOP using Applications.xml in the FrontendServer directory
			Documents ->
				Document is a Schema.Object to support namespacing like catalog objects
				Loaded into a Schema.Objects structure and an in-memory table ordered by ID
				A file system watcher is used to monitor the documents directory for changes
				Documents can only be added to, edited, or updated using the document API, not the table.
	*/
	
	public class FrontendDevice : MemoryDevice
	{
		public const string CFrontendDeviceName = @".Frontend.Server";
		public const string CDocumentsTableVarName = @".Frontend.Documents";
		public const string CDfdxDocumentID = @"dfdx";
		
		public FrontendDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID){}
		
		protected override void InternalStart(ServerProcess AProcess)
		{
			base.InternalStart(AProcess);
			FFrontendServer = FrontendServer.GetFrontendServer(AProcess.ServerSession.Server);
			#if USEWATCHERS
			FServerSession = AProcess.ServerSession;
			#endif
			FLibraryDirectory = Schema.Library.GetDefaultLibraryDirectory(AProcess.ServerSession.Server.LibraryDirectory);
			FFrontendDirectory = Path.Combine(FLibraryDirectory, Library.Name);
			FDesignersFileName = Path.Combine(FFrontendDirectory, "Designers.bop");
			LoadDesigners();
			FDocumentTypesFileName = Path.Combine(FFrontendDirectory, "DocumentTypes.bop");
			LoadDocumentTypes();
			#if USEWATCHERS
			FWatcher = new FileSystemWatcher(FFrontendDirectory);
			FWatcher.IncludeSubdirectories = false;
			FWatcher.Changed += new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Created += new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Deleted += new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Renamed += new RenamedEventHandler(DirectoryRenamed);
			FWatcher.EnableRaisingEvents = true;
			#endif
			AProcess.Plan.Catalog.Libraries.OnLibraryCreated += new Schema.LibraryNotifyEvent(LibraryCreated);
			AProcess.Plan.Catalog.Libraries.OnLibraryDeleted += new Schema.LibraryNotifyEvent(LibraryDeleted);
			AProcess.Plan.Catalog.Libraries.OnLibraryAdded += new Schema.LibraryNotifyEvent(LibraryAdded);
			AProcess.Plan.Catalog.Libraries.OnLibraryRemoved += new Schema.LibraryNotifyEvent(LibraryRemoved);
			AProcess.Plan.Catalog.Libraries.OnLibraryRenamed += new Schema.LibraryRenameEvent(LibraryRenamed);
			AProcess.Plan.Catalog.Libraries.OnLibraryLoaded += new Schema.LibraryNotifyEvent(LibraryLoaded);
			AProcess.Plan.Catalog.Libraries.OnLibraryUnloaded += new Schema.LibraryNotifyEvent(LibraryUnloaded);
		}
		
		protected override void InternalStop(ServerProcess AProcess)
		{
			AProcess.Plan.Catalog.Libraries.OnLibraryCreated -= new Schema.LibraryNotifyEvent(LibraryCreated);
			AProcess.Plan.Catalog.Libraries.OnLibraryDeleted -= new Schema.LibraryNotifyEvent(LibraryDeleted);
			AProcess.Plan.Catalog.Libraries.OnLibraryAdded -= new Schema.LibraryNotifyEvent(LibraryAdded);
			AProcess.Plan.Catalog.Libraries.OnLibraryRemoved -= new Schema.LibraryNotifyEvent(LibraryRemoved);
			AProcess.Plan.Catalog.Libraries.OnLibraryRenamed -= new Schema.LibraryRenameEvent(LibraryRenamed);
			AProcess.Plan.Catalog.Libraries.OnLibraryLoaded -= new Schema.LibraryNotifyEvent(LibraryLoaded);
			AProcess.Plan.Catalog.Libraries.OnLibraryUnloaded -= new Schema.LibraryNotifyEvent(LibraryUnloaded);
			#if USEWATCHERS
			FWatcher.Changed -= new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Created -= new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Deleted -= new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Renamed -= new RenamedEventHandler(DirectoryRenamed);
			FWatcher.Dispose();
			#endif
			base.InternalStop(AProcess);
		}
		
		private FrontendServer FFrontendServer;
		#if USEWATCHERS
		// To enable watchers, you would have to resolve the issue that the ServerSession attached to with this reference during device startup may be disposed.
		// You would have to start an owned server session that could be guaranteed to exist in all cases in order to enable this behavior.
		//private ServerSession FServerSession;
		#endif
		
		// DAE Library Events		
		private void LibraryCreated(ServerProcess AProcess, string ALibraryName)
		{
		}
		
		private void LibraryAdded(ServerProcess AProcess, string ALibraryName)
		{
			LoadLibrary(AProcess, Schema.Object.EnsureRooted(ALibraryName));
			EnsureRegisterScript(AProcess, Schema.Object.EnsureRooted(ALibraryName));
		}
		
		private void LibraryRenamed(ServerProcess AProcess, string AOldLibraryName, string ANewLibraryName)
		{
			UnloadLibrary(AProcess, Schema.Object.EnsureRooted(AOldLibraryName));
			LoadLibrary(AProcess, Schema.Object.EnsureRooted(ANewLibraryName));
		}
		
		private void LibraryRemoved(ServerProcess AProcess, string ALibraryName)
		{
			UnloadLibrary(AProcess, Schema.Object.EnsureRooted(ALibraryName));
		}
		
		private void LibraryDeleted(ServerProcess AProcess, string ALibraryName)
		{
		}
		
		private void LibraryLoaded(ServerProcess AProcess, string ALibraryName)
		{
			EnsureLibrariesLoaded(AProcess);
		}
		
		private void LibraryUnloaded(ServerProcess AProcess, string ALibraryName)
		{
			EnsureLibrariesLoaded(AProcess);
		}
		
		#if USEWATCHERS
		// Watcher
		private FileSystemWatcher FWatcher;
		#endif
		
		public bool MaintainedUpdate
		{
			get 
			{ 
				#if USEWATCHERS
				return !FWatcher.EnableRaisingEvents; 
				#else
				return false;
				#endif
			}
			set 
			{ 
				#if USEWATCHERS
				FWatcher.EnableRaisingEvents = !value; 
				#endif
			}
		}

		#if USEWATCHERS				
		private void DirectoryChanged(object ASender, FileSystemEventArgs AArgs)
		{
			if (String.Compare(AArgs.FullPath, FDocumentTypesFileName) == 0)
				LoadDocumentTypes();
			else if (String.Compare(AArgs.FullPath, FDesignersFileName) == 0)
				LoadDesigners();
		}
		
		private void DirectoryRenamed(object ASender, RenamedEventArgs AArgs)
		{
			if ((String.Compare(AArgs.OldFullPath, FDocumentTypesFileName) == 0) || (String.Compare(AArgs.FullPath, FDocumentTypesFileName) == 0))
				LoadDocumentTypes();
			else if ((String.Compare(AArgs.OldFullPath, FDesignersFileName) == 0) || (String.Compare(AArgs.FullPath, FDesignersFileName) == 0))
				LoadDesigners();
		}
		#endif

		// Session
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new FrontendDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		public static FrontendDevice GetFrontendDevice(ServerProcess AProcess)
		{
			return (FrontendDevice)Compiler.ResolveCatalogIdentifier(AProcess.Plan, CFrontendDeviceName, true);
		}
		
		public static Schema.TableVar GetDocumentsTableVar(ServerProcess AProcess)
		{
			ApplicationTransaction LTransaction = null;
			if (AProcess.ApplicationTransactionID != Guid.Empty)
				LTransaction = AProcess.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					return (Schema.TableVar)Compiler.ResolveCatalogIdentifier(AProcess.Plan, CDocumentsTableVarName, true);
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}

		// FrontendDirectory
		private string FFrontendDirectory;
		
		// LibraryDirectory
		private string FLibraryDirectory;

		// DirectoryLock - protects file operations within the FrontendDirectory		
		public void AcquireDirectoryLock()
		{
			System.Threading.Monitor.Enter(FFrontendDirectory);
		}
		
		public void ReleaseDirectoryLock()
		{
			System.Threading.Monitor.Exit(FFrontendDirectory);
		}

		private Serializer FSerializer = new Serializer();
		private Deserializer FDeserializer = new Deserializer();
		
		// DocumentTypes
		private string FDocumentTypesFileName;
		private long FDocumentTypesTimeStamp = Int64.MinValue;
		public long DocumentTypesTimeStamp { get { return FDocumentTypesTimeStamp; } }
		
		private long FDocumentTypesBufferTimeStamp = Int64.MinValue;
		public long DocumentTypesBufferTimeStamp { get { return FDocumentTypesBufferTimeStamp; } }
		
		public void UpdateDocumentTypesBufferTimeStamp()
		{
			FDocumentTypesBufferTimeStamp = FDocumentTypesTimeStamp;
		}
		
		private DocumentTypes FDocumentTypes = new DocumentTypes();
		public DocumentTypes DocumentTypes { get { return FDocumentTypes; } }
		
		private void EnsureDocumentTypeDesigners()
		{
			ArrayList LInvalidDesigners;
			foreach (DocumentType LDocumentType in FDocumentTypes)
			{
				LInvalidDesigners = new ArrayList();
				foreach (string LString in LDocumentType.Designers)
					if (!FDesigners.Contains(LString))
						LInvalidDesigners.Add(LString);
				foreach (string LString in LInvalidDesigners)
					LDocumentType.Designers.Remove(LString);
			}
		}
		
		public void LoadDocumentTypes()
		{
			lock (FFrontendDirectory)
			{
				if (File.Exists(FDocumentTypesFileName))
				{
					using (FileStream LStream = new FileStream(FDocumentTypesFileName, FileMode.Open, FileAccess.Read))
					{
						FDocumentTypes = ((DocumentTypesContainer)FDeserializer.Deserialize(LStream, null)).DocumentTypes;
					}
				}
				else
					FDocumentTypes.Clear();
					
				EnsureDocumentTypeDesigners();

				FDocumentTypesTimeStamp += 1;
			}
		}
		
		public void ClearDocumentTypes()
		{
			lock (FFrontendDirectory)
			{
				if (File.Exists(FDocumentTypesFileName))
					File.Delete(FDocumentTypesFileName);
				
				FDocumentTypes.Clear();
				
				EnsureDocumentTypeDesigners();
				
				FDocumentTypesTimeStamp += 1;
			}
		}
		
		public void SaveDocumentTypes()
		{
			lock (FFrontendDirectory)
			{
				MaintainedUpdate = true;
				try
				{
					FileUtility.EnsureWriteable(FDocumentTypesFileName);
					using (FileStream LStream = new FileStream(FDocumentTypesFileName, FileMode.Create, FileAccess.Write))
					{
						FSerializer.Serialize(LStream, new DocumentTypesContainer(FDocumentTypes));
					}
				}
				finally
				{
					MaintainedUpdate = false;
				}
			}
		}
		
		public bool HasDocumentTypeDesigners(string ADesignerID)
		{
			foreach (DocumentType LDocumentType in DocumentTypes)
				if (LDocumentType.Designers.Contains(ADesignerID))
					return true;
			return false;
		}
		
		// Designers
		private string FDesignersFileName;
		private long FDesignersTimeStamp = Int64.MinValue;
		public long DesignersTimeStamp { get { return FDesignersTimeStamp; } }
		
		private long FDesignersBufferTimeStamp = Int64.MinValue;
		public long DesignersBufferTimeStamp { get { return FDesignersBufferTimeStamp; } }
		
		public void UpdateDesignersBufferTimeStamp()
		{
			FDesignersBufferTimeStamp = FDesignersTimeStamp;
		}
		
		private long FDocumentTypeDesignersBufferTimeStamp = Int64.MinValue;
		public long DocumentTypeDesignersBufferTimeStamp { get { return FDocumentTypeDesignersBufferTimeStamp; } }
		
		public void UpdateDocumentTypeDesignersBufferTimeStamp()
		{
			FDocumentTypeDesignersBufferTimeStamp = FDocumentTypesTimeStamp;
		}
		
		private Designers FDesigners = new Designers();
		public Designers Designers { get { return FDesigners; } }
		
		public void LoadDesigners()
		{
			lock (FFrontendDirectory)
			{
				if (File.Exists(FDesignersFileName))
				{
					using (FileStream LStream = new FileStream(FDesignersFileName, FileMode.Open, FileAccess.Read))
					{
						FDesigners = ((DesignersContainer)FDeserializer.Deserialize(LStream, null)).Designers;
					}
				}
				else
					FDesigners.Clear();

				FDesignersTimeStamp += 1;
			}
		}
		
		public void ClearDesigners()
		{
			lock (FFrontendDirectory)
			{
				if (File.Exists(FDesignersFileName))
					File.Delete(FDesignersFileName);

				FDesigners.Clear();

				FDesignersTimeStamp += 1;
			}
		}
		
		public void SaveDesigners()
		{
			lock (FFrontendDirectory)
			{
				MaintainedUpdate = true;
				try
				{
					FileUtility.EnsureWriteable(FDesignersFileName);
					using (FileStream LStream = new FileStream(FDesignersFileName, FileMode.Create, FileAccess.Write))
					{
						FSerializer.Serialize(LStream, new DesignersContainer(FDesigners));
					}
				}
				finally
				{
					MaintainedUpdate = false;
				}
			}
		}
		
		// Libraries
		private FrontendLibraries FLibraries = new FrontendLibraries();
		public FrontendLibraries Libraries { get { return FLibraries; } }
		
		/// <remarks> GetFrontendLibrary takes a monitor lock on the returned library.  (Caller must exit the monitor!)</remarks>
		private FrontendLibrary GetFrontendLibrary(ServerProcess AProcess, string AName)
		{
			FrontendLibrary LResult = null;
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (!FLibraries.Contains(AName))
					LoadLibrary(AProcess, AName);
				LResult = FLibraries[AName];
			}

			Monitor.Enter(LResult);
			return LResult;
		}

		#if USEWATCHERS		
		private void DocumentCreated(FrontendLibrary ALibrary, Document ADocument)
		{
			ServerProcess LProcess = ((IServerSession)FServerSession).StartProcess(new ProcessInfo(FServerSession.SessionInfo)) as ServerProcess;
			try
			{
				InsertDocument(LProcess, ALibrary, ADocument);
			}
			finally
			{
				((IServerSession)FServerSession).StopProcess(LProcess);
			}
		}
		#endif
		
		public NativeTable EnsureNativeTable(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			int LIndex = Tables.IndexOf(ATableVar);
			if (LIndex < 0)
				LIndex= Tables.Add(new NativeTable(AProcess, ATableVar));
			return Tables[LIndex];
		}
		
		private void InsertDocument(ServerProcess AProcess, FrontendLibrary ALibrary, Document ADocument)
		{
			EnsureLibrariesLoaded(AProcess);
			NativeTable LNativeTable = EnsureNativeTable(AProcess, GetDocumentsTableVar(AProcess));
			Row LRow = new Row(AProcess, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				LRow[0].AsString = ALibrary.Name;
				LRow[1].AsString = ADocument.Name;
				LRow[2].AsString = ADocument.DocumentType.ID;
				LNativeTable.Insert(AProcess, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		private void InsertLibraryDocuments(ServerProcess AProcess, FrontendLibrary ALibrary)
		{
			// Insert all the documents from this library
			NativeTable LNativeTable = EnsureNativeTable(AProcess, GetDocumentsTableVar(AProcess));
			Row LRow = new Row(AProcess, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				foreach (Document LDocument in ALibrary.Documents)
				{
					LRow[0].AsString = ALibrary.Name;
					LRow[1].AsString = LDocument.Name;
					LRow[2].AsString = LDocument.DocumentType.ID;
					LNativeTable.Insert(AProcess, LRow);
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		#if USEWATCHERS
		private void DocumentDeleted(FrontendLibrary ALibrary, Document ADocument)
		{
			ServerProcess LProcess = ((IServerSession)FServerSession).StartProcess(new ProcessInfo(FServerSession.SessionInfo)) as ServerProcess;
			try
			{
				DeleteDocument(LProcess, ALibrary, ADocument);
			}
			finally
			{
				((IServerSession)FServerSession).StopProcess(LProcess);
			}
		}
		#endif
		
		private void DeleteDocument(ServerProcess AProcess, FrontendLibrary ALibrary, Document ADocument)
		{	
			EnsureLibrariesLoaded(AProcess);
			NativeTable LNativeTable = EnsureNativeTable(AProcess, GetDocumentsTableVar(AProcess));
			Row LRow = new Row(AProcess, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				LRow[0].AsString = ALibrary.Name;
				LRow[1].AsString = ADocument.Name;
				if (LNativeTable.HasRow(AProcess, LRow))
					LNativeTable.Delete(AProcess, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		private void DeleteLibraryDocuments(ServerProcess AProcess, FrontendLibrary ALibrary)
		{
			// Delete all the documents from this library
			NativeTable LNativeTable = EnsureNativeTable(AProcess, GetDocumentsTableVar(AProcess));
			Row LRow = new Row(AProcess, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				foreach (Document LDocument in ALibrary.Documents)
				{
					LRow[0].AsString = ALibrary.Name;
					LRow[1].AsString = LDocument.Name;
					if (LNativeTable.HasRow(AProcess, LRow))
						LNativeTable.Delete(AProcess, LRow);
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		#if USEWATCHERS
		private void LoadLibrary(string AName)
		{
			ServerProcess LProcess = ((IServerSession)FServerSession).StartProcess(new ProcessInfo(FServerSession.SessionInfo)) as ServerProcess;
			try
			{
				LoadLibrary(LProcess, AName);
			}
			finally
			{
				((IServerSession)FServerSession).StopProcess(LProcess);
			}
		}
		#endif
		
		private void LoadLibrary(ServerProcess AProcess, string AName)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (FLibraries.Contains(AName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, AName);
				FrontendLibrary LLibrary = new FrontendLibrary(AProcess, Schema.Object.EnsureUnrooted(AName));
				#if USEWATCHERS
				LLibrary.OnDocumentCreated += new DocumentEventHandler(DocumentCreated);
				LLibrary.OnDocumentDeleted += new DocumentEventHandler(DocumentDeleted);
				#endif
				FLibraries.Add(LLibrary);

				// Insert all the documents in this library
				InsertLibraryDocuments(AProcess, LLibrary);
			}
		}
		
		#if USEWATCHERS
		private void UnloadLibrary(string AName)
		{
			ServerProcess LProcess = ((IServerSession)FServerSession).StartProcess(new ProcessInfo(FServerSession.SessionInfo)) as ServerProcess;
			try
			{
				UnloadLibrary(LProcess, AName);
			}
			finally
			{
				((IServerSession)FServerSession).StopProcess(LProcess);
			}
		}
		#endif
		
		private void InternalUnloadLibrary(ServerProcess AProcess, FrontendLibrary ALibrary)
		{
			DeleteLibraryDocuments(AProcess, ALibrary);

			ALibrary.Close(AProcess);
			FLibraries.Remove(ALibrary);
		}

		private void UnloadLibrary(ServerProcess AProcess, string AName)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				int LIndex = FLibraries.IndexOf(AName);
				if (LIndex >= 0)
					InternalUnloadLibrary(AProcess, FLibraries[LIndex]);
			}
		}
		
		private void UnloadLibrary(ServerProcess AProcess, FrontendLibrary ALibrary)
		{
			lock (AProcess.Plan.Catalog.Libraries)
				InternalUnloadLibrary(AProcess, ALibrary);
		}
		
		private bool FLibrariesLoaded;
		public void EnsureLibrariesLoaded(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog.Libraries)
			{
				if (!FLibrariesLoaded)
				{
					foreach (Schema.Library LLibrary in AProcess.Plan.Catalog.Libraries)
						if (!Schema.Object.NamesEqual(LLibrary.Name, DAE.Server.Server.CSystemLibraryName) && !FLibraries.ContainsName(LLibrary.Name))
							LoadLibrary(AProcess, Schema.Object.EnsureRooted(LLibrary.Name));
					FLibrariesLoaded = true;
				}
			}
		}
		
		private void EnsureRegisterScript(ServerProcess AProcess, string ALibraryName)
		{
			string LDocumentName = Path.GetFileNameWithoutExtension(DAE.Runtime.Instructions.SystemRegisterLibraryNode.CRegisterFileName);
			string LDocumentType = Path.GetExtension(DAE.Runtime.Instructions.SystemRegisterLibraryNode.CRegisterFileName);
			LDocumentType = LDocumentType.Substring(1, LDocumentType.Length - 1);
			if (!HasDocument(AProcess, ALibraryName, Schema.Object.EnsureRooted(LDocumentName)))
				CreateDocument(AProcess, ALibraryName, LDocumentName, LDocumentType, true);
		}
		
		// Documents

		public bool HasDocument(ServerProcess AProcess, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				return LLibrary.Documents.IndexOf(AName) >= 0;
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
		
		private Document InternalCreateDocument(ServerProcess AProcess, FrontendLibrary ALibrary, string AName, string ADocumentType, bool AMaintainTable)
		{
			Document LDocument = new Document(Schema.Object.EnsureUnrooted(AName), DocumentTypes[ADocumentType]);
			ALibrary.MaintainedUpdate = true;
			try
			{
				string LDocumentFileName = Path.Combine(ALibrary.DocumentsDirectoryName, LDocument.GetFileName());
				if (File.Exists(LDocumentFileName))
					throw new SchemaException(SchemaException.Codes.DuplicateObjectName, AName);
				using (File.Create(LDocumentFileName)) {}
				try
				{
					ALibrary.Documents.Add(LDocument);
					try
					{
						if (AMaintainTable)
							InsertDocument(AProcess, ALibrary, LDocument);
					}
					catch
					{
						ALibrary.Documents.Remove(LDocument);
						throw;
					}
				}
				catch
				{
					File.Delete(LDocumentFileName);
					throw;
				}
			}
			finally
			{
				ALibrary.MaintainedUpdate = false;
			}
			return LDocument;
		}

		public void CreateDocument(ServerProcess AProcess, string ALibraryName, string AName, string ADocumentType, bool AMaintainTable)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				InternalCreateDocument(AProcess, LLibrary, AName, ADocumentType, AMaintainTable);
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		private void InternalDeleteDocument(ServerProcess AProcess, FrontendLibrary ALibrary, Document ADocument, bool AMaintainTable)
		{
			if (AMaintainTable)
				DeleteDocument(AProcess, ALibrary, ADocument);
			try
			{
				ALibrary.Documents.Remove(ADocument);
				try
				{
					ALibrary.MaintainedUpdate = true;
					try
					{
						string LFileName = Path.Combine(ALibrary.DocumentsDirectoryName, ADocument.GetFileName());
						#if !RESPECTREADONLY
						FileUtility.EnsureWriteable(LFileName);
						#endif
						File.Delete(LFileName);
					}
					finally
					{
						ALibrary.MaintainedUpdate = false;
					}
				}
				catch
				{
					ALibrary.Documents.Add(ADocument);
					throw;
				}
			}
			catch
			{
				if (AMaintainTable)
					InsertDocument(AProcess, ALibrary, ADocument);
				throw;
			}
		}

		public void DeleteDocument(ServerProcess AProcess, string ALibraryName, string AName, bool AMaintainTable)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				InternalDeleteDocument(AProcess, LLibrary, LLibrary.Documents[AName], AMaintainTable);
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
		
		public void RenameDocument(ServerProcess AProcess, string AOldLibraryName, string AOldName, string ANewLibraryName, string ANewName, bool AMaintainTable)
		{
			FrontendLibrary LOldLibrary = GetFrontendLibrary(AProcess, AOldLibraryName);
			try
			{
				FrontendLibrary LNewLibrary = GetFrontendLibrary(AProcess, ANewLibraryName);
				try
				{
					Document LDocument = LOldLibrary.Documents[AOldName];
					LOldLibrary.MaintainedUpdate = true;
					try
					{
						if (!System.Object.ReferenceEquals(LOldLibrary, LNewLibrary))
							LNewLibrary.MaintainedUpdate = true;
						try
						{
							if (AMaintainTable)
								DeleteDocument(AProcess, LOldLibrary, LDocument);
							try
							{
								LOldLibrary.Documents.Remove(LDocument);
								try
								{
									string LOldName = LDocument.Name;
									string LOldFileName = Path.Combine(LOldLibrary.DocumentsDirectoryName, LDocument.GetFileName());
									LDocument.Name = Schema.Object.EnsureUnrooted(ANewName);
									string LNewFileName = Path.Combine(LNewLibrary.DocumentsDirectoryName, LDocument.GetFileName());
									try
									{
										#if !RESPECTREADONLY
										FileUtility.EnsureWriteable(LNewFileName);
										#endif
										File.Move(LOldFileName, LNewFileName);
										try
										{
											LNewLibrary.Documents.Add(LDocument);
											try
											{
												if (AMaintainTable)
													InsertDocument(AProcess, LNewLibrary, LDocument);	
											}
											catch
											{
												LNewLibrary.Documents.Remove(LDocument);
												throw;
											}
										}
										catch
										{
											#if !RESPECTREADONLY
											FileUtility.EnsureWriteable(LOldFileName);
											#endif
											File.Move(LNewFileName, LOldFileName);
											throw;
										}
									}
									catch
									{
										LDocument.Name = LOldName;
										throw;
									}
								}
								catch
								{
									LOldLibrary.Documents.Add(LDocument);
									throw;
								}
							}
							catch
							{
								if (AMaintainTable)
									InsertDocument(AProcess, LOldLibrary, LDocument);
								throw;
							}
						}
						finally
						{
							if (!System.Object.ReferenceEquals(LOldLibrary, LNewLibrary))
								LNewLibrary.MaintainedUpdate = false;
						}
					}
					finally
					{
						LOldLibrary.MaintainedUpdate = false;
					}
				}
				finally
				{
					Monitor.Exit(LNewLibrary);
				}
			}
			finally
			{
				Monitor.Exit(LOldLibrary);
			}
		}
		
		public void RefreshDocuments(ServerProcess AProcess, string ALibraryName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				// Remove all the documents from the documents buffer
				DeleteLibraryDocuments(AProcess, LLibrary);
				LLibrary.LoadDocuments();
				// Add all the documents to the documents buffer
				InsertLibraryDocuments(AProcess, LLibrary);
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		private void InternalCopyMoveDocument(ServerProcess AProcess, string ASourceLibrary, string ASourceDocument, string ATargetLibrary, string ATargetDocument, bool AMove)
		{
			FrontendLibrary LSourceLibrary = GetFrontendLibrary(AProcess, ASourceLibrary);
			try
			{
				FrontendLibrary LTargetLibrary = GetFrontendLibrary(AProcess, ATargetLibrary);
				try
				{
					Document LSourceDocument = LSourceLibrary.Documents[ASourceDocument];
					Document LTargetDocument;

					int LTargetDocumentIndex = LTargetLibrary.Documents.IndexOf(ATargetDocument);
					if (LTargetDocumentIndex >= 0)
					{
						// delete the target document
						LTargetDocument = LTargetLibrary.Documents[LTargetDocumentIndex];
						if (System.Object.ReferenceEquals(LSourceLibrary, LTargetLibrary) && System.Object.ReferenceEquals(LSourceDocument, LTargetDocument))
							throw new FrontendDeviceException(FrontendDeviceException.Codes.CannotCopyDocumentToSelf, ASourceLibrary, ASourceDocument);
						InternalDeleteDocument(AProcess, LTargetLibrary, LTargetDocument, true);
					}

					// create the target document
					LTargetDocument = InternalCreateDocument(AProcess, LTargetLibrary, ATargetDocument, LSourceDocument.DocumentType.ID, true);

					// copy the document
					using (FileStream LSourceStream = new FileStream(Path.Combine(LSourceLibrary.DocumentsDirectoryName, LSourceDocument.GetFileName()), FileMode.Open, FileAccess.Read))
					{
						LTargetLibrary.MaintainedUpdate = true;
						try
						{			
							using (FileStream LTargetStream = new FileStream(Path.Combine(LTargetLibrary.DocumentsDirectoryName, LTargetDocument.GetFileName()), FileMode.Create, FileAccess.Write))
							{
								StreamUtility.CopyStream(LSourceStream, LTargetStream);
							}
						}
						finally
						{
							LTargetLibrary.MaintainedUpdate = false;
						}
					}

					// delete the old if we are doing a move
					if (AMove)
						InternalDeleteDocument(AProcess, LSourceLibrary, LSourceDocument, true);
				}
				finally
				{
					Monitor.Exit(LTargetLibrary);
				}
			}
			finally
			{
				Monitor.Exit(LSourceLibrary);
			}
		}

		public void CopyDocument(ServerProcess AProcess, string ASourceLibrary, string ASourceDocument, string ATargetLibrary, string ATargetDocument)
		{
			InternalCopyMoveDocument(AProcess, ASourceLibrary, ASourceDocument, ATargetLibrary, ATargetDocument, false);
		}
		
		public void MoveDocument(ServerProcess AProcess, string ASourceLibrary, string ASourceDocument, string ATargetLibrary, string ATargetDocument)
		{
			InternalCopyMoveDocument(AProcess, ASourceLibrary, ASourceDocument, ATargetLibrary, ATargetDocument, true);
		}
		
		public string LoadDocument(ServerProcess AProcess, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProcess, AProcess.DataTypes.SystemString);

				using (StreamReader LReader = new StreamReader(Path.Combine(LLibrary.DocumentsDirectoryName, LDocument.GetFileName())))
				{
					return LReader.ReadToEnd();
				}
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		public string LoadCustomization(ServerProcess AProcess, string ADilxDocument)
		{
			DilxDocument LDilxDocument = new DilxDocument();
			LDilxDocument.Read(ADilxDocument);
			return ProcessDilxDocument(AProcess, LDilxDocument);
		}

		public string Merge(string AForm, string ADelta)
		{
			XmlDocument LForm = new XmlDocument();
			LForm.LoadXml(AForm);
			XmlDocument LDelta = new XmlDocument();
			LDelta.LoadXml(ADelta);
			Inheritance.Merge(LForm, LDelta);
			StringWriter LWriter = new StringWriter();
			LForm.Save(LWriter);
			return LWriter.ToString();
		}

		public string Difference(string AForm, string ADelta)
		{
			XmlDocument LForm = new XmlDocument();
			LForm.LoadXml(AForm);
			XmlDocument LDelta = new XmlDocument();
			LDelta.LoadXml(ADelta);
			StringWriter LWriter = new StringWriter();
			Inheritance.Diff(LForm, LDelta).Save(LWriter);
			return LWriter.ToString();
		}

		public string LoadAndProcessDocument(ServerProcess AProcess, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProcess, AProcess.DataTypes.SystemString);

				string LDocumentData;
				using (StreamReader LReader = new StreamReader(Path.Combine(LLibrary.DocumentsDirectoryName, LDocument.GetFileName())))
				{
					LDocumentData = LReader.ReadToEnd();
				}

				if (LDocument.DocumentType.ID == CDfdxDocumentID)
				{
					// Read the document
					DilxDocument LDilxDocument = new DilxDocument();
					LDilxDocument.Read(LDocumentData);
					return ProcessDilxDocument(AProcess, LDilxDocument);
				}
				else
					return LDocumentData;
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		private static string ProcessDilxDocument(ServerProcess AProcess, DilxDocument ADocument)
		{
			// Process ancestors
			XmlDocument LCurrent = MergeAncestors(AProcess, ADocument.Ancestors);

			if (LCurrent == null)
				return ADocument.Content;
			else
			{
				XmlDocument LMerge = new XmlDocument();
				LMerge.LoadXml(ADocument.Content);
				Inheritance.Merge(LCurrent, LMerge);
				StringWriter LWriter = new StringWriter();
				LCurrent.Save(LWriter);
				return LWriter.ToString();
			}
		}

		private static XmlDocument MergeAncestors(ServerProcess AProcess, Ancestors AAncestors)
		{
			XmlDocument LDocument = null;
			// Process any ancestors
			foreach (string LAncestor in AAncestors)
			{
				if (LDocument == null)
					LDocument = LoadAncestor(AProcess, LAncestor);
				else
					Inheritance.Merge(LDocument, LoadAncestor(AProcess, LAncestor));
			}
			return LDocument;
		}

		private static XmlDocument LoadAncestor(ServerProcess AProcess, string ADocumentExpression)
		{
			IServerProcess LProcess = ((IServerProcess)AProcess);
			IServerExpressionPlan LPlan = LProcess.PrepareExpression(ADocumentExpression, null);
			try
			{
				using (Scalar LScalar = (Scalar)LPlan.Evaluate(null))
				{
					XmlDocument LResult = new XmlDocument();
					string LDocument = LScalar.AsString;
					try
					{
						LResult.LoadXml(LDocument);
					}
					catch (Exception AException)
					{
						throw new ServerException(ServerException.Codes.InvalidXMLDocument, AException, LDocument);
					}
					return LResult;
				}
			}
			finally
			{
				LProcess.UnprepareExpression(LPlan);
			}
		}
		
		public MemoryStream LoadBinary(ServerProcess AProcess, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];
				
				LDocument.CheckDataType(AProcess, AProcess.DataTypes.SystemBinary);
				
				using (FileStream LStream = new FileStream(Path.Combine(LLibrary.DocumentsDirectoryName, LDocument.GetFileName()), FileMode.Open, FileAccess.Read))
				{
					MemoryStream LData = new MemoryStream();
					StreamUtility.CopyStream(LStream, LData);
					LData.Position = 0;
					return LData;
				}
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
		
		public StreamID RegisterBinary(ServerProcess AProcess, Stream AData)
		{
			FrontendDeviceSession LDeviceSession = (FrontendDeviceSession)AProcess.DeviceConnect(this);
			StreamID LStreamID = AProcess.ServerSession.Server.StreamManager.Register(LDeviceSession);
			LDeviceSession.Create(LStreamID, AData);
			return LStreamID;
		}
		
		public void SaveDocument(ServerProcess AProcess, string ALibraryName, string AName, string AData)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProcess, AProcess.DataTypes.SystemString);

				LLibrary.MaintainedUpdate = true;
				try
				{			
					string LFileName = Path.Combine(LLibrary.DocumentsDirectoryName, LDocument.GetFileName());
					#if !RESPECTREADONLY
					FileUtility.EnsureWriteable(LFileName);
					#endif
					using (FileStream LStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
					{
						using (StreamWriter LWriter = new StreamWriter(LStream))
						{
							LWriter.Write(AData);
						}
					}
				}
				finally
				{
					LLibrary.MaintainedUpdate = false;
				}
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
		
		public void SaveBinary(ServerProcess AProcess, string ALibraryName, string AName, Stream AData)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProcess, AProcess.DataTypes.SystemBinary);							

				LLibrary.MaintainedUpdate = true;
				try
				{			
					string LFileName = Path.Combine(LLibrary.DocumentsDirectoryName, LDocument.GetFileName());
					#if !RESPECTREADONLY
					FileUtility.EnsureWriteable(LFileName);
					#endif
					using (FileStream LStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
					{
						StreamUtility.CopyStream(AData, LStream);
					}
				}
				finally
				{
					LLibrary.MaintainedUpdate = false;
				}
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		public string GetDocumentType(ServerProcess AProcess, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProcess, ALibraryName);
			try
			{
				return LLibrary.Documents[AName].DocumentType.ID;
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
	}
	
	public class FrontendDeviceSession : MemoryDeviceSession, IStreamProvider
	{		
		protected internal FrontendDeviceSession(DAE.Schema.Device ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		protected override void Dispose(bool ADisposing)
		{
			DestroyStreams();
			base.Dispose(ADisposing);
		}
		
		public Catalog Catalog { get { return ServerProcess.ServerSession.Server.Catalog; } }
		
		public new FrontendDevice Device { get { return (FrontendDevice)base.Device; } }
		
		protected void PopulateDocuments(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			Device.EnsureLibrariesLoaded(AProcess);
		}
		
		protected void PopulateDocumentTypes(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			if (Device.DocumentTypesBufferTimeStamp < Device.DocumentTypesTimeStamp)
			{
				ANativeTable.Truncate(AProcess);
				Device.UpdateDocumentTypesBufferTimeStamp();
				foreach (DocumentType LDocumentType in Device.DocumentTypes.Values)
				{
					ARow[0].AsString = LDocumentType.ID;
					ARow[1].AsString = LDocumentType.Description;
					ARow[2].AsString = LDocumentType.DataType;
					ANativeTable.Insert(AProcess, ARow);
				}
			}
		}
		
		protected void PopulateDesigners(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			if (Device.DesignersBufferTimeStamp < Device.DesignersTimeStamp)
			{
				ANativeTable.Truncate(AProcess);
				Device.UpdateDesignersBufferTimeStamp();
				foreach (Designer LDesigner in Device.Designers.Values)
				{
					ARow[0].AsString = LDesigner.ID;
					ARow[1].AsString = LDesigner.Description;
					ARow[2].AsString = LDesigner.ClassName;
					ANativeTable.Insert(AProcess,ARow);
				}
			}
		}
		
		protected void PopulateDocumentTypeDesigners(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			if (Device.DocumentTypeDesignersBufferTimeStamp < Device.DocumentTypesTimeStamp)
			{
				ANativeTable.Truncate(AProcess);
				Device.UpdateDocumentTypeDesignersBufferTimeStamp();
				foreach (DocumentType LDocumentType in Device.DocumentTypes.Values)
				{
					for (int LDesignerIndex = 0; LDesignerIndex < LDocumentType.Designers.Count; LDesignerIndex++)
					{
						ARow[0].AsString = LDocumentType.ID;
						ARow[1].AsString = (string)LDocumentType.Designers[LDesignerIndex];
						ANativeTable.Insert(AProcess, ARow);
					}
				}
			}
		}

		protected virtual void PopulateTableVar(Schema.TableVar ATableVar)
		{
			NativeTable LNativeTable = Device.Tables[ATableVar];
			Row LRow = new Row(ServerProcess, ATableVar.DataType.CreateRowType());
			try
			{
				switch (ATableVar.Name)
				{
					case "Frontend.Documents" : PopulateDocuments(ServerProcess, LNativeTable, LRow); break;
					case "Frontend.DocumentTypes" : PopulateDocumentTypes(ServerProcess, LNativeTable, LRow); break;
					case "Frontend.Designers" : PopulateDesigners(ServerProcess, LNativeTable, LRow); break;
					case "Frontend.DocumentTypeDesigners" : PopulateDocumentTypeDesigners(ServerProcess, LNativeTable, LRow); break;
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}

		protected override DataVar InternalExecute(Schema.DevicePlan ADevicePlan)
		{
			if ((ADevicePlan.Node is BaseTableVarNode) || (ADevicePlan.Node is OrderNode))
			{
				Schema.TableVar LTableVar = null;
				if (ADevicePlan.Node is BaseTableVarNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node).TableVar;
				else if (ADevicePlan.Node is OrderNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node.Nodes[0]).TableVar;
				if (LTableVar != null)
					PopulateTableVar(LTableVar);
			}
			DataVar LResult = base.InternalExecute(ADevicePlan);
			if (ADevicePlan.Node is CreateTableNode)
			{
				Schema.TableVar LTableVar = ((CreateTableNode)ADevicePlan.Node).Table;
				if (!ServerProcess.IsLoading() && ((Device.ReconcileMode & ReconcileMode.Command) != 0))
				{
					switch (LTableVar.Name)
					{
						case "Frontend.Designers" : Device.ClearDesigners(); break;
						case "Frontend.DocumentTypes" : Device.ClearDocumentTypes(); break;
					}
				}
			}
			return LResult;
		}
		
		protected void InsertDocumentType(Schema.TableVar ATableVar, Row ARow)
		{
			DocumentType LDocumentType = new DocumentType();
			LDocumentType.ID = ARow["ID"].AsString;
			LDocumentType.Description = ARow["Description"].AsString;
			LDocumentType.DataType = ARow["DataType"].AsString;
			Device.AcquireDirectoryLock();
			try
			{
				Device.DocumentTypes.Add(LDocumentType);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void UpdateDocumentType(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType LDocumentType = Device.DocumentTypes[AOldRow["ID"].AsString];
				Device.DocumentTypes.Remove(LDocumentType.ID);
				LDocumentType.ID = ANewRow["ID"].AsString;
				LDocumentType.DataType = ANewRow["DataType"].AsString;
				Device.DocumentTypes.Add(LDocumentType);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void DeleteDocumentType(Schema.TableVar ATableVar, Row ARow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType LDocumentType = Device.DocumentTypes[ARow["ID"].AsString];
				Device.DocumentTypes.Remove(LDocumentType.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void InsertDesigner(Schema.TableVar ATableVar, Row ARow)
		{
			Designer LDesigner = new Designer();
			LDesigner.ID = ARow["ID"].AsString;
			LDesigner.Description = ARow["Description"].AsString;
			LDesigner.ClassName = ARow["ClassName"].AsString;
			Device.AcquireDirectoryLock();
			try
			{
				Device.Designers.Add(LDesigner);
				Device.SaveDesigners();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void UpdateDesigner(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				Designer LDesigner = Device.Designers[AOldRow["ID"].AsString];
				string LNewDesignerID = ANewRow["ID"].AsString;
				if ((LDesigner.ID != LNewDesignerID) && Device.HasDocumentTypeDesigners(LDesigner.ID))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerIsAssociatedWithDocumentTypes, LDesigner.ID);
				Device.Designers.Remove(LDesigner.ID);
				LDesigner.ID = LNewDesignerID;
				LDesigner.Description = ANewRow["Description"].AsString;
				LDesigner.ClassName = ANewRow["ClassName"].AsString;
				Device.Designers.Add(LDesigner);
				Device.SaveDesigners();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void DeleteDesigner(Schema.TableVar ATableVar, Row ARow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				Designer LDesigner = Device.Designers[ARow["ID"].AsString];
				if (Device.HasDocumentTypeDesigners(LDesigner.ID))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerIsAssociatedWithDocumentTypes, LDesigner.ID);
				Device.Designers.Remove(LDesigner.ID);
				Device.SaveDesigners();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void InsertDocumentTypeDesigner(Schema.TableVar ATableVar, Row ARow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType LDocumentType = Device.DocumentTypes[ARow["DocumentType_ID"].AsString];
				Designer LDesigner = Device.Designers[ARow["Designer_ID"].AsString];
				LDocumentType.Designers.Add(LDesigner.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void UpdateDocumentTypeDesigner(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType LOldDocumentType = Device.DocumentTypes[AOldRow["DocumentType_ID"].AsString];
				DocumentType LNewDocumentType = Device.DocumentTypes[ANewRow["DocumentType_ID"].AsString];
				Designer LOldDesigner = Device.Designers[AOldRow["Designer_ID"].AsString];
				Designer LNewDesigner = Device.Designers[ANewRow["Designer_ID"].AsString];
				LOldDocumentType.Designers.Remove(LOldDesigner.ID);
				LNewDocumentType.Designers.Add(LNewDesigner.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void DeleteDocumentTypeDesigner(Schema.TableVar ATableVar, Row ARow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType LDocumentType = Device.DocumentTypes[ARow["DocumentType_ID"].AsString];
				Designer LDesigner = Device.Designers[ARow["Designer_ID"].AsString];
				LDocumentType.Designers.Remove(LDesigner.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}

		private void ClearDerivationCache()
		{
			FrontendServer.GetFrontendServer(ServerProcess.ServerSession.Server).ClearDerivationCache();
		}

		protected void InsertDocument(Schema.TableVar ATableVar, Row ARow)
		{
			Device.CreateDocument
			(
				ServerProcess, 
				Schema.Object.EnsureRooted(ARow["Library_Name"].AsString), 
				ARow["Name"].AsString,
				ARow["Type_ID"].AsString,
				false
			);
		}
		
		protected void UpdateDocument(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			string LOldLibraryName = AOldRow["Library_Name"].AsString;
			string LNewLibraryName = ANewRow["Library_Name"].AsString;
			string LOldName = AOldRow["Name"].AsString;
			string LNewName = ANewRow["Name"].AsString;
			string LOldTypeID = AOldRow["Type_ID"].AsString;
			string LNewTypeID = ANewRow["Type_ID"].AsString;
			if (LOldTypeID != LNewTypeID)
				throw new ServerException(ServerException.Codes.CannotChangeDocumentType);
			if ((LOldLibraryName != LNewLibraryName) || (LOldName != LNewName))
				Device.RenameDocument
				(
					ServerProcess, 
					Schema.Object.EnsureRooted(LOldLibraryName), 
					Schema.Object.EnsureRooted(LOldName), 
					Schema.Object.EnsureRooted(LNewLibraryName), 
					LNewName, 
					false
				);
		}
		
		protected void DeleteDocument(Schema.TableVar ATableVar, Row ARow)
		{
			Device.DeleteDocument
			(
				ServerProcess, 
				Schema.Object.EnsureRooted(ARow["Library_Name"].AsString), 
				Schema.Object.EnsureRooted(ARow["Name"].AsString), 
				false
			);
		}

		protected override void InternalInsertRow(Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "Frontend.DocumentTypes" : InsertDocumentType(ATableVar, ARow); break;
				case "Frontend.Designers" : InsertDesigner(ATableVar, ARow); break;
				case "Frontend.DocumentTypeDesigners" : InsertDocumentTypeDesigner(ATableVar, ARow); break;
				case "Frontend.Documents" : InsertDocument(ATableVar, ARow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalInsertRow(ATableVar, ARow, AValueFlags);
		}
		
		protected override void InternalUpdateRow(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "Frontend.DocumentTypes" : UpdateDocumentType(ATableVar, AOldRow, ANewRow); break;
				case "Frontend.Designers" : UpdateDesigner(ATableVar, AOldRow, ANewRow); break;
				case "Frontend.DocumentTypeDesigners" : UpdateDocumentTypeDesigner(ATableVar, AOldRow, ANewRow); break;
				case "Frontend.Documents" : UpdateDocument(ATableVar, AOldRow, ANewRow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalUpdateRow(ATableVar, AOldRow, ANewRow, AValueFlags);
		}
		
		protected override void InternalDeleteRow(Schema.TableVar ATableVar, Row ARow)
		{
			switch (ATableVar.Name)
			{
				case "Frontend.DocumentTypes" : DeleteDocumentType(ATableVar, ARow); break;
				case "Frontend.Designers" : DeleteDesigner(ATableVar, ARow); break;
				case "Frontend.DocumentTypeDesigners" : DeleteDocumentTypeDesigner(ATableVar, ARow); break;
				case "Frontend.Documents" : DeleteDocument(ATableVar, ARow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalDeleteRow(ATableVar, ARow);
		}

		// IStreamProvider
		private Hashtable FStreams = new Hashtable();
		
		private Stream GetStream(StreamID AStreamID)
		{
			Stream LStream = (Stream)FStreams[AStreamID];
			if (LStream == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, AStreamID.ToString());
			return LStream;
		}
		
		protected void DestroyStreams()
		{
			foreach (StreamID LStreamID in FStreams.Keys)
				((Stream)FStreams[LStreamID]).Close();
			FStreams.Clear();
		}
		
		public void Create(StreamID AStreamID, Stream AStream)
		{
			FStreams.Add(AStreamID, AStream);
		}
		
		public void Destroy(StreamID AStreamID)
		{
			Stream LStream = GetStream(AStreamID);
			FStreams.Remove(AStreamID);
			LStream.Close();
		}
		
		public void Reassign(StreamID AOldStreamID, StreamID ANewStreamID)
		{
			Stream LOldStream = GetStream(AOldStreamID);
			FStreams.Remove(AOldStreamID);
			FStreams.Add(ANewStreamID, LOldStream);
		}

		public void Close(StreamID AStreamID)
		{
			// no action to perform
		}

		public Stream Open(StreamID AStreamID)
		{
			return new CoverStream(GetStream(AStreamID));
		}
	}
}

