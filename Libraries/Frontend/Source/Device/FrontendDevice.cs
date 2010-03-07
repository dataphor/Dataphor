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
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.Frontend.Server;
using Alphora.Dataphor.Windows;
using System.Xml.Linq;

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
		
		public FrontendDevice(int AID, string AName) : base(AID, AName){}
		
		protected override void InternalStart(ServerProcess AProcess)
		{
			base.InternalStart(AProcess);
			FFrontendServer = FrontendServer.GetFrontendServer(AProcess.ServerSession.Server);
			#if USEWATCHERS
			FServerSession = AProcess.ServerSession;
			#endif
			FFrontendDirectory = AProcess.ServerSession.Server.Catalog.Libraries[Library.Name].GetInstanceLibraryDirectory(((DAE.Server.Server)AProcess.ServerSession.Server).InstanceDirectory);
			string LOldFrontendDirectory = Path.Combine(Schema.LibraryUtility.GetDefaultLibraryDirectory(((DAE.Server.Server)AProcess.ServerSession.Server).LibraryDirectory), Library.Name);

			FDesignersFileName = Path.Combine(FFrontendDirectory, "Designers.bop");
			string LOldDesignersFileName = Path.Combine(LOldFrontendDirectory, "Designers.bop");
			if (!File.Exists(FDesignersFileName) && File.Exists(LOldDesignersFileName))
				File.Copy(LOldDesignersFileName, FDesignersFileName);

			LoadDesigners();

			FDocumentTypesFileName = Path.Combine(FFrontendDirectory, "DocumentTypes.bop");
			string LOldDocumentTypesFileName = Path.Combine(LOldFrontendDirectory, "DocumentTypes.bop");
			if (!File.Exists(FDocumentTypesFileName) && File.Exists(LOldDocumentTypesFileName))
				File.Copy(LOldDocumentTypesFileName, FDocumentTypesFileName);
				
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
			AProcess.Catalog.Libraries.OnLibraryCreated += new Schema.LibraryNotifyEvent(LibraryCreated);
			AProcess.Catalog.Libraries.OnLibraryDeleted += new Schema.LibraryNotifyEvent(LibraryDeleted);
			AProcess.Catalog.Libraries.OnLibraryAdded += new Schema.LibraryNotifyEvent(LibraryAdded);
			AProcess.Catalog.Libraries.OnLibraryRemoved += new Schema.LibraryNotifyEvent(LibraryRemoved);
			AProcess.Catalog.Libraries.OnLibraryRenamed += new Schema.LibraryRenameEvent(LibraryRenamed);
			AProcess.Catalog.Libraries.OnLibraryLoaded += new Schema.LibraryNotifyEvent(LibraryLoaded);
			AProcess.Catalog.Libraries.OnLibraryUnloaded += new Schema.LibraryNotifyEvent(LibraryUnloaded);
		}
		
		protected override void InternalStop(ServerProcess AProcess)
		{
			AProcess.Catalog.Libraries.OnLibraryCreated -= new Schema.LibraryNotifyEvent(LibraryCreated);
			AProcess.Catalog.Libraries.OnLibraryDeleted -= new Schema.LibraryNotifyEvent(LibraryDeleted);
			AProcess.Catalog.Libraries.OnLibraryAdded -= new Schema.LibraryNotifyEvent(LibraryAdded);
			AProcess.Catalog.Libraries.OnLibraryRemoved -= new Schema.LibraryNotifyEvent(LibraryRemoved);
			AProcess.Catalog.Libraries.OnLibraryRenamed -= new Schema.LibraryRenameEvent(LibraryRenamed);
			AProcess.Catalog.Libraries.OnLibraryLoaded -= new Schema.LibraryNotifyEvent(LibraryLoaded);
			AProcess.Catalog.Libraries.OnLibraryUnloaded -= new Schema.LibraryNotifyEvent(LibraryUnloaded);
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
		private void LibraryCreated(Program AProgram, string ALibraryName)
		{
		}
		
		private void LibraryAdded(Program AProgram, string ALibraryName)
		{
			LoadLibrary(AProgram, Schema.Object.EnsureRooted(ALibraryName));
			EnsureRegisterScript(AProgram, Schema.Object.EnsureRooted(ALibraryName));
		}
		
		private void LibraryRenamed(Program AProgram, string AOldLibraryName, string ANewLibraryName)
		{
			UnloadLibrary(AProgram, Schema.Object.EnsureRooted(AOldLibraryName));
			LoadLibrary(AProgram, Schema.Object.EnsureRooted(ANewLibraryName));
		}
		
		private void LibraryRemoved(Program AProgram, string ALibraryName)
		{
			UnloadLibrary(AProgram, Schema.Object.EnsureRooted(ALibraryName));
		}
		
		private void LibraryDeleted(Program AProgram, string ALibraryName)
		{
		}
		
		private void LibraryLoaded(Program AProgram, string ALibraryName)
		{
			EnsureLibrariesLoaded(AProgram);
		}
		
		private void LibraryUnloaded(Program AProgram, string ALibraryName)
		{
			EnsureLibrariesLoaded(AProgram);
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
		
		public static FrontendDevice GetFrontendDevice(Program AProgram)
		{
			return (FrontendDevice)Compiler.ResolveCatalogIdentifier(AProgram.Plan, CFrontendDeviceName, true);
		}
		
		public static Schema.TableVar GetDocumentsTableVar(Plan APlan)
		{
			ApplicationTransaction LTransaction = null;
			if (APlan.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					return (Schema.TableVar)Compiler.ResolveCatalogIdentifier(APlan, CDocumentsTableVarName, true);
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
		//private string FLibraryDirectory;

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
			foreach (DocumentType LDocumentType in FDocumentTypes.Values)
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
			foreach (DocumentType LDocumentType in DocumentTypes.Values)
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
		private FrontendLibrary GetFrontendLibrary(Program AProgram, string AName)
		{
			FrontendLibrary LResult = null;
			lock (AProgram.Catalog.Libraries)
			{
				if (!FLibraries.Contains(AName))
					LoadLibrary(AProgram, AName);
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
		
		public NativeTable EnsureNativeTable(Program AProgram, Schema.TableVar ATableVar)
		{
			int LIndex = Tables.IndexOf(ATableVar);
			if (LIndex < 0)
				LIndex= Tables.Add(new NativeTable(AProgram.ValueManager, ATableVar));
			return Tables[LIndex];
		}
		
		private void InsertDocument(Program AProgram, FrontendLibrary ALibrary, Document ADocument)
		{
			EnsureLibrariesLoaded(AProgram);
			NativeTable LNativeTable = EnsureNativeTable(AProgram, GetDocumentsTableVar(AProgram.Plan));
			Row LRow = new Row(AProgram.ValueManager, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				LRow[0] = ALibrary.Name;
				LRow[1] = ADocument.Name;
				LRow[2] = ADocument.DocumentType.ID;
				LNativeTable.Insert(AProgram.ValueManager, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		private void InsertLibraryDocuments(Program AProgram, FrontendLibrary ALibrary)
		{
			// Insert all the documents from this library
			NativeTable LNativeTable = EnsureNativeTable(AProgram, GetDocumentsTableVar(AProgram.Plan));
			Row LRow = new Row(AProgram.ValueManager, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				foreach (Document LDocument in ALibrary.Documents)
				{
					LRow[0] = ALibrary.Name;
					LRow[1] = LDocument.Name;
					LRow[2] = LDocument.DocumentType.ID;
					LNativeTable.Insert(AProgram.ValueManager, LRow);
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
		
		private void DeleteDocument(Program AProgram, FrontendLibrary ALibrary, Document ADocument)
		{	
			EnsureLibrariesLoaded(AProgram);
			NativeTable LNativeTable = EnsureNativeTable(AProgram, GetDocumentsTableVar(AProgram.Plan));
			Row LRow = new Row(AProgram.ValueManager, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				LRow[0] = ALibrary.Name;
				LRow[1] = ADocument.Name;
				if (LNativeTable.HasRow(AProgram.ValueManager, LRow))
					LNativeTable.Delete(AProgram.ValueManager, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		private void DeleteLibraryDocuments(Program AProgram, FrontendLibrary ALibrary)
		{
			// Delete all the documents from this library
			NativeTable LNativeTable = EnsureNativeTable(AProgram, GetDocumentsTableVar(AProgram.Plan));
			Row LRow = new Row(AProgram.ValueManager, LNativeTable.TableVar.DataType.CreateRowType());
			try
			{
				foreach (Document LDocument in ALibrary.Documents)
				{
					LRow[0] = ALibrary.Name;
					LRow[1] = LDocument.Name;
					if (LNativeTable.HasRow(AProgram.ValueManager, LRow))
						LNativeTable.Delete(AProgram.ValueManager, LRow);
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
		
		private void LoadLibrary(Program AProgram, string AName)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (FLibraries.Contains(AName))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, AName);
				FrontendLibrary LLibrary = new FrontendLibrary(AProgram, Schema.Object.EnsureUnrooted(AName));
				#if USEWATCHERS
				LLibrary.OnDocumentCreated += new DocumentEventHandler(DocumentCreated);
				LLibrary.OnDocumentDeleted += new DocumentEventHandler(DocumentDeleted);
				#endif
				FLibraries.Add(LLibrary);

				// Insert all the documents in this library
				InsertLibraryDocuments(AProgram, LLibrary);
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
		
		private void InternalUnloadLibrary(Program AProgram, FrontendLibrary ALibrary)
		{
			DeleteLibraryDocuments(AProgram, ALibrary);

			ALibrary.Close(AProgram);
			FLibraries.Remove(ALibrary);
		}

		private void UnloadLibrary(Program AProgram, string AName)
		{
			lock (AProgram.Catalog.Libraries)
			{
				int LIndex = FLibraries.IndexOf(AName);
				if (LIndex >= 0)
					InternalUnloadLibrary(AProgram, FLibraries[LIndex]);
			}
		}
		
		private void UnloadLibrary(Program AProgram, FrontendLibrary ALibrary)
		{
			lock (AProgram.Catalog.Libraries)
				InternalUnloadLibrary(AProgram, ALibrary);
		}
		
		private bool FLibrariesLoaded;
		public void EnsureLibrariesLoaded(Program AProgram)
		{
			lock (AProgram.Catalog.Libraries)
			{
				if (!FLibrariesLoaded)
				{
					foreach (Schema.Library LLibrary in AProgram.Catalog.Libraries)
						if (!Schema.Object.NamesEqual(LLibrary.Name, DAE.Server.Engine.CSystemLibraryName) && !FLibraries.ContainsName(LLibrary.Name))
							LoadLibrary(AProgram, Schema.Object.EnsureRooted(LLibrary.Name));
					FLibrariesLoaded = true;
				}
			}
		}
		
		private void EnsureRegisterScript(Program AProgram, string ALibraryName)
		{
			string LDocumentName = Path.GetFileNameWithoutExtension(Schema.LibraryUtility.CRegisterFileName);
			string LDocumentType = Path.GetExtension(Schema.LibraryUtility.CRegisterFileName);
			LDocumentType = LDocumentType.Substring(1, LDocumentType.Length - 1);
			if (!HasDocument(AProgram, ALibraryName, Schema.Object.EnsureRooted(LDocumentName)))
				CreateDocument(AProgram, ALibraryName, LDocumentName, LDocumentType, true);
		}
		
		// Documents

		public bool HasDocument(Program AProgram, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				return LLibrary.Documents.IndexOf(AName) >= 0;
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
		
		private Document InternalCreateDocument(Program AProgram, FrontendLibrary ALibrary, string AName, string ADocumentType, bool AMaintainTable)
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
							InsertDocument(AProgram, ALibrary, LDocument);
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

		public void CreateDocument(Program AProgram, string ALibraryName, string AName, string ADocumentType, bool AMaintainTable)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				InternalCreateDocument(AProgram, LLibrary, AName, ADocumentType, AMaintainTable);
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		private void InternalDeleteDocument(Program AProgram, FrontendLibrary ALibrary, Document ADocument, bool AMaintainTable)
		{
			if (AMaintainTable)
				DeleteDocument(AProgram, ALibrary, ADocument);
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
					InsertDocument(AProgram, ALibrary, ADocument);
				throw;
			}
		}

		public void DeleteDocument(Program AProgram, string ALibraryName, string AName, bool AMaintainTable)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				InternalDeleteDocument(AProgram, LLibrary, LLibrary.Documents[AName], AMaintainTable);
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}
		
		public void RenameDocument(Program AProgram, string AOldLibraryName, string AOldName, string ANewLibraryName, string ANewName, bool AMaintainTable)
		{
			FrontendLibrary LOldLibrary = GetFrontendLibrary(AProgram, AOldLibraryName);
			try
			{
				FrontendLibrary LNewLibrary = GetFrontendLibrary(AProgram, ANewLibraryName);
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
								DeleteDocument(AProgram, LOldLibrary, LDocument);
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
													InsertDocument(AProgram, LNewLibrary, LDocument);	
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
									InsertDocument(AProgram, LOldLibrary, LDocument);
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
		
		public void RefreshDocuments(Program AProgram, string ALibraryName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				// Remove all the documents from the documents buffer
				DeleteLibraryDocuments(AProgram, LLibrary);
				LLibrary.LoadDocuments();
				// Add all the documents to the documents buffer
				InsertLibraryDocuments(AProgram, LLibrary);
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		private void InternalCopyMoveDocument(Program AProgram, string ASourceLibrary, string ASourceDocument, string ATargetLibrary, string ATargetDocument, bool AMove)
		{
			FrontendLibrary LSourceLibrary = GetFrontendLibrary(AProgram, ASourceLibrary);
			try
			{
				FrontendLibrary LTargetLibrary = GetFrontendLibrary(AProgram, ATargetLibrary);
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
						InternalDeleteDocument(AProgram, LTargetLibrary, LTargetDocument, true);
					}

					// create the target document
					LTargetDocument = InternalCreateDocument(AProgram, LTargetLibrary, ATargetDocument, LSourceDocument.DocumentType.ID, true);

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
						InternalDeleteDocument(AProgram, LSourceLibrary, LSourceDocument, true);
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

		public void CopyDocument(Program AProgram, string ASourceLibrary, string ASourceDocument, string ATargetLibrary, string ATargetDocument)
		{
			InternalCopyMoveDocument(AProgram, ASourceLibrary, ASourceDocument, ATargetLibrary, ATargetDocument, false);
		}
		
		public void MoveDocument(Program AProgram, string ASourceLibrary, string ASourceDocument, string ATargetLibrary, string ATargetDocument)
		{
			InternalCopyMoveDocument(AProgram, ASourceLibrary, ASourceDocument, ATargetLibrary, ATargetDocument, true);
		}
		
		public string LoadDocument(Program AProgram, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProgram.Plan, AProgram.DataTypes.SystemString);

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

		public string LoadCustomization(Program AProgram, string ADilxDocument)
		{
			DilxDocument LDilxDocument = new DilxDocument();
			LDilxDocument.Read(ADilxDocument);
			return ProcessDilxDocument(AProgram, LDilxDocument);
		}

		public string Merge(string AForm, string ADelta)
		{
			XDocument LForm = XDocument.Load(new StringReader(AForm));
			XDocument LDelta = XDocument.Load(new StringReader(ADelta));
			Inheritance.Merge(LForm, LDelta);
			StringWriter LWriter = new StringWriter();
			LForm.Save(LWriter);
			return LWriter.ToString();
		}

		public string Difference(string AForm, string ADelta)
		{
			XDocument LForm = XDocument.Load(new StringReader(AForm));
			XDocument LDelta = XDocument.Load(new StringReader(ADelta));
			StringWriter LWriter = new StringWriter();
			Inheritance.Diff(LForm, LDelta).Save(LWriter);
			return LWriter.ToString();
		}

		public string LoadAndProcessDocument(Program AProgram, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProgram.Plan, AProgram.DataTypes.SystemString);

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
					return ProcessDilxDocument(AProgram, LDilxDocument);
				}
				else
					return LDocumentData;
			}
			finally
			{
				Monitor.Exit(LLibrary);
			}
		}

		private static string ProcessDilxDocument(Program AProgram, DilxDocument ADocument)
		{
			// Process ancestors
			XDocument LCurrent = MergeAncestors(AProgram, ADocument.Ancestors);

			if (LCurrent == null)
				return ADocument.Content;
			else
			{
				XDocument LMerge = XDocument.Load(new StringReader(ADocument.Content));
				Inheritance.Merge(LCurrent, LMerge);
				StringWriter LWriter = new StringWriter();
				LCurrent.Save(LWriter);
				return LWriter.ToString();
			}
		}

		private static XDocument MergeAncestors(Program AProgram, Ancestors AAncestors)
		{
			XDocument LDocument = null;
			// Process any ancestors
			foreach (string LAncestor in AAncestors)
			{
				if (LDocument == null)
					LDocument = LoadAncestor(AProgram, LAncestor);
				else
					Inheritance.Merge(LDocument, LoadAncestor(AProgram, LAncestor));
			}
			return LDocument;
		}

		private static XDocument LoadAncestor(Program AProgram, string ADocumentExpression)
		{
			IServerProcess LProcess = ((IServerProcess)AProgram.ServerProcess);
			IServerExpressionPlan LPlan = LProcess.PrepareExpression(ADocumentExpression, null);
			try
			{
				using (Scalar LScalar = (Scalar)LPlan.Evaluate(null))
				{
					string LDocument = LScalar.AsString;
					try
					{
						return XDocument.Load(new StringReader(LDocument));
					}
					catch (Exception AException)
					{
						throw new ServerException(ServerException.Codes.InvalidXMLDocument, AException, LDocument);
					}
				}
			}
			finally
			{
				LProcess.UnprepareExpression(LPlan);
			}
		}
		
		public MemoryStream LoadBinary(Program AProgram, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];
				
				LDocument.CheckDataType(AProgram.Plan, AProgram.DataTypes.SystemBinary);
				
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
		
		public StreamID RegisterBinary(Program AProgram, Stream AData)
		{
			FrontendDeviceSession LDeviceSession = (FrontendDeviceSession)AProgram.DeviceConnect(this);
			StreamID LStreamID = AProgram.ServerProcess.Register(LDeviceSession);
			LDeviceSession.Create(LStreamID, AData);
			return LStreamID;
		}
		
		public void SaveDocument(Program AProgram, string ALibraryName, string AName, string AData)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProgram.Plan, AProgram.DataTypes.SystemString);

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
		
		public void SaveBinary(Program AProgram, string ALibraryName, string AName, Stream AData)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
			try
			{
				Document LDocument = LLibrary.Documents[AName];

				LDocument.CheckDataType(AProgram.Plan, AProgram.DataTypes.SystemBinary);							

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

		public string GetDocumentType(Program AProgram, string ALibraryName, string AName)
		{
			FrontendLibrary LLibrary = GetFrontendLibrary(AProgram, ALibraryName);
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
		
		protected void PopulateDocuments(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			Device.EnsureLibrariesLoaded(AProgram);
		}
		
		protected void PopulateDocumentTypes(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			if (Device.DocumentTypesBufferTimeStamp < Device.DocumentTypesTimeStamp)
			{
				ANativeTable.Truncate(AProgram.ValueManager);
				Device.UpdateDocumentTypesBufferTimeStamp();
				foreach (DocumentType LDocumentType in Device.DocumentTypes.Values)
				{
					ARow[0] = LDocumentType.ID;
					ARow[1] = LDocumentType.Description;
					ARow[2] = LDocumentType.DataType;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
			}
		}
		
		protected void PopulateDesigners(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			if (Device.DesignersBufferTimeStamp < Device.DesignersTimeStamp)
			{
				ANativeTable.Truncate(AProgram.ValueManager);
				Device.UpdateDesignersBufferTimeStamp();
				foreach (Designer LDesigner in Device.Designers.Values)
				{
					ARow[0] = LDesigner.ID;
					ARow[1] = LDesigner.Description;
					ARow[2] = LDesigner.ClassName;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
			}
		}
		
		protected void PopulateDocumentTypeDesigners(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			if (Device.DocumentTypeDesignersBufferTimeStamp < Device.DocumentTypesTimeStamp)
			{
				ANativeTable.Truncate(AProgram.ValueManager);
				Device.UpdateDocumentTypeDesignersBufferTimeStamp();
				foreach (DocumentType LDocumentType in Device.DocumentTypes.Values)
				{
					for (int LDesignerIndex = 0; LDesignerIndex < LDocumentType.Designers.Count; LDesignerIndex++)
					{
						ARow[0] = LDocumentType.ID;
						ARow[1] = (string)LDocumentType.Designers[LDesignerIndex];
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
			}
		}

		protected virtual void PopulateTableVar(Program AProgram, Schema.TableVar ATableVar)
		{
			NativeTable LNativeTable = Device.Tables[ATableVar];
			Row LRow = new Row(AProgram.ValueManager, ATableVar.DataType.CreateRowType());
			try
			{
				switch (ATableVar.Name)
				{
					case "Frontend.Documents" : PopulateDocuments(AProgram, LNativeTable, LRow); break;
					case "Frontend.DocumentTypes" : PopulateDocumentTypes(AProgram, LNativeTable, LRow); break;
					case "Frontend.Designers" : PopulateDesigners(AProgram, LNativeTable, LRow); break;
					case "Frontend.DocumentTypeDesigners" : PopulateDocumentTypeDesigners(AProgram, LNativeTable, LRow); break;
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}

		protected override object InternalExecute(Program AProgram, Schema.DevicePlan ADevicePlan)
		{
			if ((ADevicePlan.Node is BaseTableVarNode) || (ADevicePlan.Node is OrderNode))
			{
				Schema.TableVar LTableVar = null;
				if (ADevicePlan.Node is BaseTableVarNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node).TableVar;
				else if (ADevicePlan.Node is OrderNode)
					LTableVar = ((BaseTableVarNode)ADevicePlan.Node.Nodes[0]).TableVar;
				if (LTableVar != null)
					PopulateTableVar(AProgram, LTableVar);
			}
			object LResult = base.InternalExecute(AProgram, ADevicePlan);
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
			LDocumentType.ID = (string)ARow["ID"];
			LDocumentType.Description = (string)ARow["Description"];
			LDocumentType.DataType = (string)ARow["DataType"];
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
				DocumentType LDocumentType = Device.DocumentTypes[(string)AOldRow["ID"]];
				Device.DocumentTypes.Remove(LDocumentType.ID);
				LDocumentType.ID = (string)ANewRow["ID"];
				LDocumentType.DataType = (string)ANewRow["DataType"];
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
				DocumentType LDocumentType = Device.DocumentTypes[(string)ARow["ID"]];
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
			LDesigner.ID = (string)ARow["ID"];
			LDesigner.Description = (string)ARow["Description"];
			LDesigner.ClassName = (string)ARow["ClassName"];
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
				Designer LDesigner = Device.Designers[(string)AOldRow["ID"]];
				string LNewDesignerID = (string)ANewRow["ID"];
				if ((LDesigner.ID != LNewDesignerID) && Device.HasDocumentTypeDesigners(LDesigner.ID))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerIsAssociatedWithDocumentTypes, LDesigner.ID);
				Device.Designers.Remove(LDesigner.ID);
				LDesigner.ID = LNewDesignerID;
				LDesigner.Description = (string)ANewRow["Description"];
				LDesigner.ClassName = (string)ANewRow["ClassName"];
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
				Designer LDesigner = Device.Designers[(string)ARow["ID"]];
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
				DocumentType LDocumentType = Device.DocumentTypes[(string)ARow["DocumentType_ID"]];
				Designer LDesigner = Device.Designers[(string)ARow["Designer_ID"]];
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
				DocumentType LOldDocumentType = Device.DocumentTypes[(string)AOldRow["DocumentType_ID"]];
				DocumentType LNewDocumentType = Device.DocumentTypes[(string)ANewRow["DocumentType_ID"]];
				Designer LOldDesigner = Device.Designers[(string)AOldRow["Designer_ID"]];
				Designer LNewDesigner = Device.Designers[(string)ANewRow["Designer_ID"]];
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
				DocumentType LDocumentType = Device.DocumentTypes[(string)ARow["DocumentType_ID"]];
				Designer LDesigner = Device.Designers[(string)ARow["Designer_ID"]];
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

		protected void InsertDocument(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			Device.CreateDocument
			(
				AProgram, 
				Schema.Object.EnsureRooted((string)ARow["Library_Name"]), 
				(string)ARow["Name"],
				(string)ARow["Type_ID"],
				false
			);
		}
		
		protected void UpdateDocument(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			string LOldLibraryName = (string)AOldRow["Library_Name"];
			string LNewLibraryName = (string)ANewRow["Library_Name"];
			string LOldName = (string)AOldRow["Name"];
			string LNewName = (string)ANewRow["Name"];
			string LOldTypeID = (string)AOldRow["Type_ID"];
			string LNewTypeID = (string)ANewRow["Type_ID"];
			if (LOldTypeID != LNewTypeID)
				throw new ServerException(ServerException.Codes.CannotChangeDocumentType);
			if ((LOldLibraryName != LNewLibraryName) || (LOldName != LNewName))
				Device.RenameDocument
				(
					AProgram, 
					Schema.Object.EnsureRooted(LOldLibraryName), 
					Schema.Object.EnsureRooted(LOldName), 
					Schema.Object.EnsureRooted(LNewLibraryName), 
					LNewName, 
					false
				);
		}
		
		protected void DeleteDocument(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			Device.DeleteDocument
			(
				AProgram, 
				Schema.Object.EnsureRooted((string)ARow["Library_Name"]), 
				Schema.Object.EnsureRooted((string)ARow["Name"]), 
				false
			);
		}

		protected override void InternalInsertRow(Program AProgram, Schema.TableVar ATableVar, Row ARow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "Frontend.DocumentTypes" : InsertDocumentType(ATableVar, ARow); break;
				case "Frontend.Designers" : InsertDesigner(ATableVar, ARow); break;
				case "Frontend.DocumentTypeDesigners" : InsertDocumentTypeDesigner(ATableVar, ARow); break;
				case "Frontend.Documents" : InsertDocument(AProgram, ATableVar, ARow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalInsertRow(AProgram, ATableVar, ARow, AValueFlags);
		}
		
		protected override void InternalUpdateRow(Program AProgram, Schema.TableVar ATableVar, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			switch (ATableVar.Name)
			{
				case "Frontend.DocumentTypes" : UpdateDocumentType(ATableVar, AOldRow, ANewRow); break;
				case "Frontend.Designers" : UpdateDesigner(ATableVar, AOldRow, ANewRow); break;
				case "Frontend.DocumentTypeDesigners" : UpdateDocumentTypeDesigner(ATableVar, AOldRow, ANewRow); break;
				case "Frontend.Documents" : UpdateDocument(AProgram, ATableVar, AOldRow, ANewRow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalUpdateRow(AProgram, ATableVar, AOldRow, ANewRow, AValueFlags);
		}
		
		protected override void InternalDeleteRow(Program AProgram, Schema.TableVar ATableVar, Row ARow)
		{
			switch (ATableVar.Name)
			{
				case "Frontend.DocumentTypes" : DeleteDocumentType(ATableVar, ARow); break;
				case "Frontend.Designers" : DeleteDesigner(ATableVar, ARow); break;
				case "Frontend.DocumentTypeDesigners" : DeleteDocumentTypeDesigner(ATableVar, ARow); break;
				case "Frontend.Documents" : DeleteDocument(AProgram, ATableVar, ARow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, ATableVar.Name);
			}
			base.InternalDeleteRow(AProgram, ATableVar, ARow);
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

