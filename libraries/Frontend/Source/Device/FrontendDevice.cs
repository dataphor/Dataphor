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
		public const string FrontendDeviceName = @".Frontend.Server";
		public const string DocumentsTableVarName = @".Frontend.Documents";
		public const string DfdxDocumentID = @"dfdx";
		
		public FrontendDevice(int iD, string name) : base(iD, name){}
		
		protected override void InternalStart(ServerProcess process)
		{
			base.InternalStart(process);
			_frontendServer = FrontendServer.GetFrontendServer(process.ServerSession.Server);
			#if USEWATCHERS
			FServerSession = AProcess.ServerSession;
			#endif
			_frontendDirectory = process.ServerSession.Server.Catalog.Libraries[Library.Name].GetInstanceLibraryDirectory(((DAE.Server.Server)process.ServerSession.Server).InstanceDirectory);
			string oldFrontendDirectory = Path.Combine(Schema.LibraryUtility.GetDefaultLibraryDirectory(((DAE.Server.Server)process.ServerSession.Server).LibraryDirectory), Library.Name);

			_designersFileName = Path.Combine(_frontendDirectory, "Designers.bop");
			string oldDesignersFileName = Path.Combine(oldFrontendDirectory, "Designers.bop");
			if (!File.Exists(_designersFileName) && File.Exists(oldDesignersFileName))
				File.Copy(oldDesignersFileName, _designersFileName);

			LoadDesigners();

			_documentTypesFileName = Path.Combine(_frontendDirectory, "DocumentTypes.bop");
			string oldDocumentTypesFileName = Path.Combine(oldFrontendDirectory, "DocumentTypes.bop");
			if (!File.Exists(_documentTypesFileName) && File.Exists(oldDocumentTypesFileName))
				File.Copy(oldDocumentTypesFileName, _documentTypesFileName);
				
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
			process.Catalog.Libraries.OnLibraryCreated += new Schema.LibraryNotifyEvent(LibraryCreated);
			process.Catalog.Libraries.OnLibraryDeleted += new Schema.LibraryNotifyEvent(LibraryDeleted);
			process.Catalog.Libraries.OnLibraryAdded += new Schema.LibraryNotifyEvent(LibraryAdded);
			process.Catalog.Libraries.OnLibraryRemoved += new Schema.LibraryNotifyEvent(LibraryRemoved);
			process.Catalog.Libraries.OnLibraryRenamed += new Schema.LibraryRenameEvent(LibraryRenamed);
			process.Catalog.Libraries.OnLibraryLoaded += new Schema.LibraryNotifyEvent(LibraryLoaded);
			process.Catalog.Libraries.OnLibraryUnloaded += new Schema.LibraryNotifyEvent(LibraryUnloaded);
		}
		
		protected override void InternalStop(ServerProcess process)
		{
			process.Catalog.Libraries.OnLibraryCreated -= new Schema.LibraryNotifyEvent(LibraryCreated);
			process.Catalog.Libraries.OnLibraryDeleted -= new Schema.LibraryNotifyEvent(LibraryDeleted);
			process.Catalog.Libraries.OnLibraryAdded -= new Schema.LibraryNotifyEvent(LibraryAdded);
			process.Catalog.Libraries.OnLibraryRemoved -= new Schema.LibraryNotifyEvent(LibraryRemoved);
			process.Catalog.Libraries.OnLibraryRenamed -= new Schema.LibraryRenameEvent(LibraryRenamed);
			process.Catalog.Libraries.OnLibraryLoaded -= new Schema.LibraryNotifyEvent(LibraryLoaded);
			process.Catalog.Libraries.OnLibraryUnloaded -= new Schema.LibraryNotifyEvent(LibraryUnloaded);
			#if USEWATCHERS
			FWatcher.Changed -= new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Created -= new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Deleted -= new FileSystemEventHandler(DirectoryChanged);
			FWatcher.Renamed -= new RenamedEventHandler(DirectoryRenamed);
			FWatcher.Dispose();
			#endif
			base.InternalStop(process);
		}
		
		private FrontendServer _frontendServer;
		#if USEWATCHERS
		// To enable watchers, you would have to resolve the issue that the ServerSession attached to with this reference during device startup may be disposed.
		// You would have to start an owned server session that could be guaranteed to exist in all cases in order to enable this behavior.
		//private ServerSession FServerSession;
		#endif
		
		// DAE Library Events		
		private void LibraryCreated(Program program, string libraryName)
		{
		}
		
		private void LibraryAdded(Program program, string libraryName)
		{
			LoadLibrary(program, Schema.Object.EnsureRooted(libraryName));
			EnsureRegisterScript(program, Schema.Object.EnsureRooted(libraryName));
		}
		
		private void LibraryRenamed(Program program, string oldLibraryName, string newLibraryName)
		{
			UnloadLibrary(program, Schema.Object.EnsureRooted(oldLibraryName));
			LoadLibrary(program, Schema.Object.EnsureRooted(newLibraryName));
		}
		
		private void LibraryRemoved(Program program, string libraryName)
		{
			UnloadLibrary(program, Schema.Object.EnsureRooted(libraryName));
		}
		
		private void LibraryDeleted(Program program, string libraryName)
		{
		}
		
		private void LibraryLoaded(Program program, string libraryName)
		{
			EnsureLibrariesLoaded(program);
		}
		
		private void LibraryUnloaded(Program program, string libraryName)
		{
			EnsureLibrariesLoaded(program);
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
		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new FrontendDeviceSession(this, serverProcess, deviceSessionInfo);
		}
		
		public static FrontendDevice GetFrontendDevice(Program program)
		{
			return (FrontendDevice)Compiler.ResolveCatalogIdentifier(program.Plan, FrontendDeviceName, true);
		}
		
		public static Schema.TableVar GetDocumentsTableVar(Plan plan)
		{
			plan.PushGlobalContext();
			try
			{
				return (Schema.TableVar)Compiler.ResolveCatalogIdentifier(plan, DocumentsTableVarName, true);
			}
			finally
			{
				plan.PopGlobalContext();
			}
		}

		// FrontendDirectory
		private string _frontendDirectory;
		
		// LibraryDirectory
		//private string FLibraryDirectory;

		// DirectoryLock - protects file operations within the FrontendDirectory		
		public void AcquireDirectoryLock()
		{
			System.Threading.Monitor.Enter(_frontendDirectory);
		}
		
		public void ReleaseDirectoryLock()
		{
			System.Threading.Monitor.Exit(_frontendDirectory);
		}

		private Serializer _serializer = new Serializer();
		private Deserializer _deserializer = new Deserializer();
		
		// DocumentTypes
		private string _documentTypesFileName;
		private long _documentTypesTimeStamp = Int64.MinValue;
		public long DocumentTypesTimeStamp { get { return _documentTypesTimeStamp; } }
		
		private long _documentTypesBufferTimeStamp = Int64.MinValue;
		public long DocumentTypesBufferTimeStamp { get { return _documentTypesBufferTimeStamp; } }
		
		public void UpdateDocumentTypesBufferTimeStamp()
		{
			_documentTypesBufferTimeStamp = _documentTypesTimeStamp;
		}
		
		private DocumentTypes _documentTypes = new DocumentTypes();
		public DocumentTypes DocumentTypes { get { return _documentTypes; } }
		
		private void EnsureDocumentTypeDesigners()
		{
			ArrayList invalidDesigners;
			foreach (DocumentType documentType in _documentTypes.Values)
			{
				invalidDesigners = new ArrayList();
				foreach (string stringValue in documentType.Designers)
					if (!_designers.Contains(stringValue))
						invalidDesigners.Add(stringValue);
				foreach (string stringValue in invalidDesigners)
					documentType.Designers.Remove(stringValue);
			}
		}
		
		public void LoadDocumentTypes()
		{
			lock (_frontendDirectory)
			{
				if (File.Exists(_documentTypesFileName))
				{
					using (FileStream stream = new FileStream(_documentTypesFileName, FileMode.Open, FileAccess.Read))
					{
						_documentTypes = ((DocumentTypesContainer)_deserializer.Deserialize(stream, null)).DocumentTypes;
					}
				}
				else
					_documentTypes.Clear();
					
				EnsureDocumentTypeDesigners();

				_documentTypesTimeStamp += 1;
			}
		}
		
		public void ClearDocumentTypes()
		{
			lock (_frontendDirectory)
			{
				if (File.Exists(_documentTypesFileName))
					File.Delete(_documentTypesFileName);
				
				_documentTypes.Clear();
				
				EnsureDocumentTypeDesigners();
				
				_documentTypesTimeStamp += 1;
			}
		}
		
		public void SaveDocumentTypes()
		{
			lock (_frontendDirectory)
			{
				MaintainedUpdate = true;
				try
				{
					FileUtility.EnsureWriteable(_documentTypesFileName);
					using (FileStream stream = new FileStream(_documentTypesFileName, FileMode.Create, FileAccess.Write))
					{
						_serializer.Serialize(stream, new DocumentTypesContainer(_documentTypes));
					}
				}
				finally
				{
					MaintainedUpdate = false;
				}
			}
		}
		
		public bool HasDocumentTypeDesigners(string designerID)
		{
			foreach (DocumentType documentType in DocumentTypes.Values)
				if (documentType.Designers.Contains(designerID))
					return true;
			return false;
		}
		
		// Designers
		private string _designersFileName;
		private long _designersTimeStamp = Int64.MinValue;
		public long DesignersTimeStamp { get { return _designersTimeStamp; } }
		
		private long _designersBufferTimeStamp = Int64.MinValue;
		public long DesignersBufferTimeStamp { get { return _designersBufferTimeStamp; } }
		
		public void UpdateDesignersBufferTimeStamp()
		{
			_designersBufferTimeStamp = _designersTimeStamp;
		}
		
		private long _documentTypeDesignersBufferTimeStamp = Int64.MinValue;
		public long DocumentTypeDesignersBufferTimeStamp { get { return _documentTypeDesignersBufferTimeStamp; } }
		
		public void UpdateDocumentTypeDesignersBufferTimeStamp()
		{
			_documentTypeDesignersBufferTimeStamp = _documentTypesTimeStamp;
		}
		
		private Designers _designers = new Designers();
		public Designers Designers { get { return _designers; } }
		
		public void LoadDesigners()
		{
			lock (_frontendDirectory)
			{
				if (File.Exists(_designersFileName))
				{
					using (FileStream stream = new FileStream(_designersFileName, FileMode.Open, FileAccess.Read))
					{
						_designers = ((DesignersContainer)_deserializer.Deserialize(stream, null)).Designers;
					}
				}
				else
					_designers.Clear();

				_designersTimeStamp += 1;
			}
		}
		
		public void ClearDesigners()
		{
			lock (_frontendDirectory)
			{
				if (File.Exists(_designersFileName))
					File.Delete(_designersFileName);

				_designers.Clear();

				_designersTimeStamp += 1;
			}
		}
		
		public void SaveDesigners()
		{
			lock (_frontendDirectory)
			{
				MaintainedUpdate = true;
				try
				{
					FileUtility.EnsureWriteable(_designersFileName);
					using (FileStream stream = new FileStream(_designersFileName, FileMode.Create, FileAccess.Write))
					{
						_serializer.Serialize(stream, new DesignersContainer(_designers));
					}
				}
				finally
				{
					MaintainedUpdate = false;
				}
			}
		}
		
		// Libraries
		private FrontendLibraries _libraries = new FrontendLibraries();
		public FrontendLibraries Libraries { get { return _libraries; } }
		
		/// <remarks> GetFrontendLibrary takes a monitor lock on the returned library.  (Caller must exit the monitor!)</remarks>
		private FrontendLibrary GetFrontendLibrary(Program program, string name)
		{
			FrontendLibrary result = null;
			lock (program.Catalog.Libraries)
			{
				if (!_libraries.Contains(name))
					LoadLibrary(program, name);
				result = _libraries[name];
			}

			Monitor.Enter(result);
			return result;
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
		
		public NativeTable EnsureNativeTable(Program program, Schema.TableVar tableVar)
		{
			int index = Tables.IndexOf(tableVar);
			if (index < 0)
				index= Tables.Add(new NativeTable(program.ValueManager, tableVar));
			return Tables[index];
		}
		
		private void InsertDocument(Program program, FrontendLibrary library, Document document)
		{
			EnsureLibrariesLoaded(program);
			NativeTable nativeTable = EnsureNativeTable(program, GetDocumentsTableVar(program.Plan));
			Row row = new Row(program.ValueManager, nativeTable.TableVar.DataType.CreateRowType());
			try
			{
				row[0] = library.Name;
				row[1] = document.Name;
				row[2] = document.DocumentType.ID;
				nativeTable.Insert(program.ValueManager, row);
			}
			finally
			{
				row.Dispose();
			}
		}
		
		private void InsertLibraryDocuments(Program program, FrontendLibrary library)
		{
			// Insert all the documents from this library
			NativeTable nativeTable = EnsureNativeTable(program, GetDocumentsTableVar(program.Plan));
			Row row = new Row(program.ValueManager, nativeTable.TableVar.DataType.CreateRowType());
			try
			{
				foreach (Document document in library.Documents)
				{
					row[0] = library.Name;
					row[1] = document.Name;
					row[2] = document.DocumentType.ID;
					nativeTable.Insert(program.ValueManager, row);
				}
			}
			finally
			{
				row.Dispose();
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
		
		private void DeleteDocument(Program program, FrontendLibrary library, Document document)
		{	
			EnsureLibrariesLoaded(program);
			NativeTable nativeTable = EnsureNativeTable(program, GetDocumentsTableVar(program.Plan));
			Row row = new Row(program.ValueManager, nativeTable.TableVar.DataType.CreateRowType());
			try
			{
				row[0] = library.Name;
				row[1] = document.Name;
				if (nativeTable.HasRow(program.ValueManager, row))
					nativeTable.Delete(program.ValueManager, row);
			}
			finally
			{
				row.Dispose();
			}
		}
		
		private void DeleteLibraryDocuments(Program program, FrontendLibrary library)
		{
			// Delete all the documents from this library
			NativeTable nativeTable = EnsureNativeTable(program, GetDocumentsTableVar(program.Plan));
			Row row = new Row(program.ValueManager, nativeTable.TableVar.DataType.CreateRowType());
			try
			{
				foreach (Document document in library.Documents)
				{
					row[0] = library.Name;
					row[1] = document.Name;
					if (nativeTable.HasRow(program.ValueManager, row))
						nativeTable.Delete(program.ValueManager, row);
				}
			}
			finally
			{
				row.Dispose();
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
		
		private void LoadLibrary(Program program, string name)
		{
			lock (program.Catalog.Libraries)
			{
				if (_libraries.Contains(name))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, name);
				FrontendLibrary library = new FrontendLibrary(program, Schema.Object.EnsureUnrooted(name));
				#if USEWATCHERS
				library.OnDocumentCreated += new DocumentEventHandler(DocumentCreated);
				library.OnDocumentDeleted += new DocumentEventHandler(DocumentDeleted);
				#endif
				_libraries.Add(library);

				// Insert all the documents in this library
				InsertLibraryDocuments(program, library);
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
		
		private void InternalUnloadLibrary(Program program, FrontendLibrary library)
		{
			DeleteLibraryDocuments(program, library);

			library.Close(program);
			_libraries.Remove(library);
		}

		private void UnloadLibrary(Program program, string name)
		{
			lock (program.Catalog.Libraries)
			{
				int index = _libraries.IndexOf(name);
				if (index >= 0)
					InternalUnloadLibrary(program, _libraries[index]);
			}
		}
		
		private void UnloadLibrary(Program program, FrontendLibrary library)
		{
			lock (program.Catalog.Libraries)
				InternalUnloadLibrary(program, library);
		}
		
		private bool _librariesLoaded;
		public void EnsureLibrariesLoaded(Program program)
		{
			lock (program.Catalog.Libraries)
			{
				if (!_librariesLoaded)
				{
					foreach (Schema.Library library in program.Catalog.Libraries)
						if (!Schema.Object.NamesEqual(library.Name, DAE.Server.Engine.SystemLibraryName) && !_libraries.ContainsName(library.Name))
							LoadLibrary(program, Schema.Object.EnsureRooted(library.Name));
					_librariesLoaded = true;
				}
			}
		}
		
		private void EnsureRegisterScript(Program program, string libraryName)
		{
			string documentName = Path.GetFileNameWithoutExtension(Schema.LibraryUtility.RegisterFileName);
			string documentType = Path.GetExtension(Schema.LibraryUtility.RegisterFileName);
			documentType = documentType.Substring(1, documentType.Length - 1);
			if (!HasDocument(program, libraryName, Schema.Object.EnsureRooted(documentName)))
				CreateDocument(program, libraryName, documentName, documentType, true);
		}
		
		// Documents

		public bool HasDocument(Program program, string libraryName, string name)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				return library.Documents.IndexOf(name) >= 0;
			}
			finally
			{
				Monitor.Exit(library);
			}
		}
		
		private Document InternalCreateDocument(Program program, FrontendLibrary library, string name, string documentType, bool maintainTable)
		{
			Document document = new Document(Schema.Object.EnsureUnrooted(name), DocumentTypes[documentType]);
			library.MaintainedUpdate = true;
			try
			{
				string documentFileName = Path.Combine(library.DocumentsDirectoryName, document.GetFileName());
				if (File.Exists(documentFileName))
					throw new SchemaException(SchemaException.Codes.DuplicateObjectName, name);
				using (File.Create(documentFileName)) {}
				try
				{
					library.Documents.Add(document);
					try
					{
						if (maintainTable)
							InsertDocument(program, library, document);
					}
					catch
					{
						library.Documents.Remove(document);
						throw;
					}
				}
				catch
				{
					File.Delete(documentFileName);
					throw;
				}
			}
			finally
			{
				library.MaintainedUpdate = false;
			}
			return document;
		}

		public void CreateDocument(Program program, string libraryName, string name, string documentType, bool maintainTable)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				InternalCreateDocument(program, library, name, documentType, maintainTable);
			}
			finally
			{
				Monitor.Exit(library);
			}
		}

		private void InternalDeleteDocument(Program program, FrontendLibrary library, Document document, bool maintainTable)
		{
			if (maintainTable)
				DeleteDocument(program, library, document);
			try
			{
				library.Documents.Remove(document);
				try
				{
					library.MaintainedUpdate = true;
					try
					{
						string fileName = Path.Combine(library.DocumentsDirectoryName, document.GetFileName());
						#if !RESPECTREADONLY
						FileUtility.EnsureWriteable(fileName);
						#endif
						File.Delete(fileName);
					}
					finally
					{
						library.MaintainedUpdate = false;
					}
				}
				catch
				{
					library.Documents.Add(document);
					throw;
				}
			}
			catch
			{
				if (maintainTable)
					InsertDocument(program, library, document);
				throw;
			}
		}

		public void DeleteDocument(Program program, string libraryName, string name, bool maintainTable)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				InternalDeleteDocument(program, library, library.Documents[name], maintainTable);
			}
			finally
			{
				Monitor.Exit(library);
			}
		}
		
		public void RenameDocument(Program program, string oldLibraryName, string oldName, string newLibraryName, string newName, bool maintainTable)
		{
			FrontendLibrary oldLibrary = GetFrontendLibrary(program, oldLibraryName);
			try
			{
				FrontendLibrary newLibrary = GetFrontendLibrary(program, newLibraryName);
				try
				{
					Document document = oldLibrary.Documents[oldName];
					oldLibrary.MaintainedUpdate = true;
					try
					{
						if (!System.Object.ReferenceEquals(oldLibrary, newLibrary))
							newLibrary.MaintainedUpdate = true;
						try
						{
							if (maintainTable)
								DeleteDocument(program, oldLibrary, document);
							try
							{
								oldLibrary.Documents.Remove(document);
								try
								{
									string localOldName = document.Name;
									string oldFileName = Path.Combine(oldLibrary.DocumentsDirectoryName, document.GetFileName());
									document.Name = Schema.Object.EnsureUnrooted(newName);
									string newFileName = Path.Combine(newLibrary.DocumentsDirectoryName, document.GetFileName());
									try
									{
										#if !RESPECTREADONLY
										FileUtility.EnsureWriteable(newFileName);
										#endif
										File.Move(oldFileName, newFileName);
										try
										{
											newLibrary.Documents.Add(document);
											try
											{
												if (maintainTable)
													InsertDocument(program, newLibrary, document);	
											}
											catch
											{
												newLibrary.Documents.Remove(document);
												throw;
											}
										}
										catch
										{
											#if !RESPECTREADONLY
											FileUtility.EnsureWriteable(oldFileName);
											#endif
											File.Move(newFileName, oldFileName);
											throw;
										}
									}
									catch
									{
										document.Name = localOldName;
										throw;
									}
								}
								catch
								{
									oldLibrary.Documents.Add(document);
									throw;
								}
							}
							catch
							{
								if (maintainTable)
									InsertDocument(program, oldLibrary, document);
								throw;
							}
						}
						finally
						{
							if (!System.Object.ReferenceEquals(oldLibrary, newLibrary))
								newLibrary.MaintainedUpdate = false;
						}
					}
					finally
					{
						oldLibrary.MaintainedUpdate = false;
					}
				}
				finally
				{
					Monitor.Exit(newLibrary);
				}
			}
			finally
			{
				Monitor.Exit(oldLibrary);
			}
		}
		
		public void RefreshDocuments(Program program, string libraryName)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				// Remove all the documents from the documents buffer
				DeleteLibraryDocuments(program, library);
				library.LoadDocuments();
				// Add all the documents to the documents buffer
				InsertLibraryDocuments(program, library);
			}
			finally
			{
				Monitor.Exit(library);
			}
		}

		private void InternalCopyMoveDocument(Program program, string sourceLibrary, string sourceDocument, string targetLibrary, string targetDocument, bool move)
		{
			FrontendLibrary localSourceLibrary = GetFrontendLibrary(program, sourceLibrary);
			try
			{
				FrontendLibrary localTargetLibrary = GetFrontendLibrary(program, targetLibrary);
				try
				{
					Document localSourceDocument = localSourceLibrary.Documents[sourceDocument];
					Document localTargetDocument;

					int targetDocumentIndex = localTargetLibrary.Documents.IndexOf(targetDocument);
					if (targetDocumentIndex >= 0)
					{
						// delete the target document
						localTargetDocument = localTargetLibrary.Documents[targetDocumentIndex];
						if (System.Object.ReferenceEquals(localSourceLibrary, localTargetLibrary) && System.Object.ReferenceEquals(localSourceDocument, localTargetDocument))
							throw new FrontendDeviceException(FrontendDeviceException.Codes.CannotCopyDocumentToSelf, sourceLibrary, sourceDocument);
						InternalDeleteDocument(program, localTargetLibrary, localTargetDocument, true);
					}

					// create the target document
					localTargetDocument = InternalCreateDocument(program, localTargetLibrary, targetDocument, localSourceDocument.DocumentType.ID, true);

					// copy the document
					using (FileStream sourceStream = new FileStream(Path.Combine(localSourceLibrary.DocumentsDirectoryName, localSourceDocument.GetFileName()), FileMode.Open, FileAccess.Read))
					{
						localTargetLibrary.MaintainedUpdate = true;
						try
						{			
							using (FileStream targetStream = new FileStream(Path.Combine(localTargetLibrary.DocumentsDirectoryName, localTargetDocument.GetFileName()), FileMode.Create, FileAccess.Write))
							{
								StreamUtility.CopyStream(sourceStream, targetStream);
							}
						}
						finally
						{
							localTargetLibrary.MaintainedUpdate = false;
						}
					}

					// delete the old if we are doing a move
					if (move)
						InternalDeleteDocument(program, localSourceLibrary, localSourceDocument, true);
				}
				finally
				{
					Monitor.Exit(localTargetLibrary);
				}
			}
			finally
			{
				Monitor.Exit(localSourceLibrary);
			}
		}

		public void CopyDocument(Program program, string sourceLibrary, string sourceDocument, string targetLibrary, string targetDocument)
		{
			InternalCopyMoveDocument(program, sourceLibrary, sourceDocument, targetLibrary, targetDocument, false);
		}
		
		public void MoveDocument(Program program, string sourceLibrary, string sourceDocument, string targetLibrary, string targetDocument)
		{
			InternalCopyMoveDocument(program, sourceLibrary, sourceDocument, targetLibrary, targetDocument, true);
		}
		
		public string LoadDocument(Program program, string libraryName, string name)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				Document document = library.Documents[name];

				document.CheckDataType(program.Plan, program.DataTypes.SystemString);

				using (StreamReader reader = new StreamReader(Path.Combine(library.DocumentsDirectoryName, document.GetFileName())))
				{
					return reader.ReadToEnd();
				}
			}
			finally
			{
				Monitor.Exit(library);
			}
		}

		public string LoadCustomization(Program program, string dilxDocument)
		{
			DilxDocument localDilxDocument = new DilxDocument();
			localDilxDocument.Read(dilxDocument);
			return ProcessDilxDocument(program, localDilxDocument);
		}

		public string Merge(string form, string delta)
		{
			XDocument localForm = XDocument.Load(new StringReader(form));
			XDocument localDelta = XDocument.Load(new StringReader(delta));
			Inheritance.Merge(localForm, localDelta);
			StringWriter writer = new StringWriter();
			localForm.Save(writer);
			return writer.ToString();
		}

		public string Difference(string form, string delta)
		{
			XDocument localForm = XDocument.Load(new StringReader(form));
			XDocument localDelta = XDocument.Load(new StringReader(delta));
			StringWriter writer = new StringWriter();
			Inheritance.Diff(localForm, localDelta).Save(writer);
			return writer.ToString();
		}

		public string LoadAndProcessDocument(Program program, string libraryName, string name)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				Document document = library.Documents[name];

				document.CheckDataType(program.Plan, program.DataTypes.SystemString);

				string documentData;
				using (StreamReader reader = new StreamReader(Path.Combine(library.DocumentsDirectoryName, document.GetFileName())))
				{
					documentData = reader.ReadToEnd();
				}

				if (document.DocumentType.ID == DfdxDocumentID)
				{
					// Read the document
					DilxDocument dilxDocument = new DilxDocument();
					dilxDocument.Read(documentData);
					return ProcessDilxDocument(program, dilxDocument);
				}
				else
					return documentData;
			}
			finally
			{
				Monitor.Exit(library);
			}
		}

		private static string ProcessDilxDocument(Program program, DilxDocument document)
		{
			// Process ancestors
			XDocument current = MergeAncestors(program, document.Ancestors);

			if (current == null)
				return document.Content;
			else
			{
				XDocument merge = XDocument.Load(new StringReader(document.Content));
				Inheritance.Merge(current, merge);
				StringWriter writer = new StringWriter();
				current.Save(writer);
				return writer.ToString();
			}
		}

		private static XDocument MergeAncestors(Program program, Ancestors ancestors)
		{
			XDocument document = null;
			// Process any ancestors
			foreach (string ancestor in ancestors)
			{
				if (document == null)
					document = LoadAncestor(program, ancestor);
				else
					Inheritance.Merge(document, LoadAncestor(program, ancestor));
			}
			return document;
		}

		private static XDocument LoadAncestor(Program program, string documentExpression)
		{
			IServerProcess process = ((IServerProcess)program.ServerProcess);
			IServerExpressionPlan plan = process.PrepareExpression(documentExpression, null);
			try
			{
				using (IScalar scalar = (IScalar)plan.Evaluate(null))
				{
					string document = scalar.AsString;
					try
					{
						return XDocument.Load(new StringReader(document));
					}
					catch (Exception AException)
					{
						throw new ServerException(ServerException.Codes.InvalidXMLDocument, AException, document);
					}
				}
			}
			finally
			{
				process.UnprepareExpression(plan);
			}
		}
		
		public MemoryStream LoadBinary(Program program, string libraryName, string name)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				Document document = library.Documents[name];
				
				document.CheckDataType(program.Plan, program.DataTypes.SystemBinary);
				
				using (FileStream stream = new FileStream(Path.Combine(library.DocumentsDirectoryName, document.GetFileName()), FileMode.Open, FileAccess.Read))
				{
					MemoryStream data = new MemoryStream();
					StreamUtility.CopyStream(stream, data);
					data.Position = 0;
					return data;
				}
			}
			finally
			{
				Monitor.Exit(library);
			}
		}
		
		public StreamID RegisterBinary(Program program, Stream data)
		{
			FrontendDeviceSession deviceSession = (FrontendDeviceSession)program.DeviceConnect(this);
			StreamID streamID = program.ServerProcess.Register(deviceSession);
			deviceSession.Create(streamID, data);
			return streamID;
		}
		
		public void SaveDocument(Program program, string libraryName, string name, string data)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				Document document = library.Documents[name];

				document.CheckDataType(program.Plan, program.DataTypes.SystemString);

				library.MaintainedUpdate = true;
				try
				{			
					string fileName = Path.Combine(library.DocumentsDirectoryName, document.GetFileName());
					#if !RESPECTREADONLY
					FileUtility.EnsureWriteable(fileName);
					#endif
					using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
					{
						using (StreamWriter writer = new StreamWriter(stream))
						{
							writer.Write(data);
						}
					}
				}
				finally
				{
					library.MaintainedUpdate = false;
				}
			}
			finally
			{
				Monitor.Exit(library);
			}
		}
		
		public void SaveBinary(Program program, string libraryName, string name, Stream data)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				Document document = library.Documents[name];

				document.CheckDataType(program.Plan, program.DataTypes.SystemBinary);							

				library.MaintainedUpdate = true;
				try
				{			
					string fileName = Path.Combine(library.DocumentsDirectoryName, document.GetFileName());
					#if !RESPECTREADONLY
					FileUtility.EnsureWriteable(fileName);
					#endif
					using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
					{
						StreamUtility.CopyStream(data, stream);
					}
				}
				finally
				{
					library.MaintainedUpdate = false;
				}
			}
			finally
			{
				Monitor.Exit(library);
			}
		}

		public string GetDocumentType(Program program, string libraryName, string name)
		{
			FrontendLibrary library = GetFrontendLibrary(program, libraryName);
			try
			{
				return library.Documents[name].DocumentType.ID;
			}
			finally
			{
				Monitor.Exit(library);
			}
		}
	}
	
	public class FrontendDeviceSession : MemoryDeviceSession, IStreamProvider
	{		
		protected internal FrontendDeviceSession(DAE.Schema.Device device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo){}
		
		protected override void Dispose(bool disposing)
		{
			DestroyStreams();
			base.Dispose(disposing);
		}
		
		public Catalog Catalog { get { return ServerProcess.ServerSession.Server.Catalog; } }
		
		public new FrontendDevice Device { get { return (FrontendDevice)base.Device; } }
		
		protected void PopulateDocuments(Program program, NativeTable nativeTable, IRow row)
		{
			Device.EnsureLibrariesLoaded(program);
		}
		
		protected void PopulateDocumentTypes(Program program, NativeTable nativeTable, IRow row)
		{
			if (Device.DocumentTypesBufferTimeStamp < Device.DocumentTypesTimeStamp)
			{
				nativeTable.Truncate(program.ValueManager);
				Device.UpdateDocumentTypesBufferTimeStamp();
				foreach (DocumentType documentType in Device.DocumentTypes.Values)
				{
					row[0] = documentType.ID;
					row[1] = documentType.Description;
					row[2] = documentType.DataType;
					nativeTable.Insert(program.ValueManager, row);
				}
			}
		}
		
		protected void PopulateDesigners(Program program, NativeTable nativeTable, IRow row)
		{
			if (Device.DesignersBufferTimeStamp < Device.DesignersTimeStamp)
			{
				nativeTable.Truncate(program.ValueManager);
				Device.UpdateDesignersBufferTimeStamp();
				foreach (Designer designer in Device.Designers.Values)
				{
					row[0] = designer.ID;
					row[1] = designer.Description;
					row[2] = designer.ClassName;
					nativeTable.Insert(program.ValueManager, row);
				}
			}
		}
		
		protected void PopulateDocumentTypeDesigners(Program program, NativeTable nativeTable, IRow row)
		{
			if (Device.DocumentTypeDesignersBufferTimeStamp < Device.DocumentTypesTimeStamp)
			{
				nativeTable.Truncate(program.ValueManager);
				Device.UpdateDocumentTypeDesignersBufferTimeStamp();
				foreach (DocumentType documentType in Device.DocumentTypes.Values)
				{
					for (int designerIndex = 0; designerIndex < documentType.Designers.Count; designerIndex++)
					{
						row[0] = documentType.ID;
						row[1] = (string)documentType.Designers[designerIndex];
						nativeTable.Insert(program.ValueManager, row);
					}
				}
			}
		}

		protected virtual void PopulateTableVar(Program program, Schema.TableVar tableVar)
		{
			NativeTable nativeTable = Device.Tables[tableVar];
			Row row = new Row(program.ValueManager, tableVar.DataType.CreateRowType());
			try
			{
				switch (tableVar.Name)
				{
					case "Frontend.Documents" : PopulateDocuments(program, nativeTable, row); break;
					case "Frontend.DocumentTypes" : PopulateDocumentTypes(program, nativeTable, row); break;
					case "Frontend.Designers" : PopulateDesigners(program, nativeTable, row); break;
					case "Frontend.DocumentTypeDesigners" : PopulateDocumentTypeDesigners(program, nativeTable, row); break;
				}
			}
			finally
			{
				row.Dispose();
			}
		}

		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			if ((planNode is BaseTableVarNode) || (planNode is OrderNode))
			{
				Schema.TableVar tableVar = null;
				if (planNode is BaseTableVarNode)
					tableVar = ((BaseTableVarNode)planNode).TableVar;
				else if (planNode is OrderNode)
					tableVar = ((BaseTableVarNode)planNode.Nodes[0]).TableVar;
				if (tableVar != null)
					PopulateTableVar(program, tableVar);
			}
			object result = base.InternalExecute(program, planNode);
			if (planNode is CreateTableNode)
			{
				Schema.TableVar tableVar = ((CreateTableNode)planNode).Table;
				if (!ServerProcess.IsLoading() && ((Device.ReconcileMode & ReconcileMode.Command) != 0))
				{
					switch (tableVar.Name)
					{
						case "Frontend.Designers" : Device.ClearDesigners(); break;
						case "Frontend.DocumentTypes" : Device.ClearDocumentTypes(); break;
					}
				}
			}
			return result;
		}
		
		protected void InsertDocumentType(Schema.TableVar tableVar, IRow row)
		{
			DocumentType documentType = new DocumentType();
			documentType.ID = (string)row["ID"];
			documentType.Description = (string)row["Description"];
			documentType.DataType = (string)row["DataType"];
			Device.AcquireDirectoryLock();
			try
			{
				Device.DocumentTypes.Add(documentType);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void UpdateDocumentType(Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType documentType = Device.DocumentTypes[(string)oldRow["ID"]];
				Device.DocumentTypes.Remove(documentType.ID);
				documentType.ID = (string)newRow["ID"];
				documentType.DataType = (string)newRow["DataType"];
				Device.DocumentTypes.Add(documentType);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void DeleteDocumentType(Schema.TableVar tableVar, IRow row)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType documentType = Device.DocumentTypes[(string)row["ID"]];
				Device.DocumentTypes.Remove(documentType.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void InsertDesigner(Schema.TableVar tableVar, IRow row)
		{
			Designer designer = new Designer();
			designer.ID = (string)row["ID"];
			designer.Description = (string)row["Description"];
			designer.ClassName = (string)row["ClassName"];
			Device.AcquireDirectoryLock();
			try
			{
				Device.Designers.Add(designer);
				Device.SaveDesigners();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void UpdateDesigner(Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				Designer designer = Device.Designers[(string)oldRow["ID"]];
				string newDesignerID = (string)newRow["ID"];
				if ((designer.ID != newDesignerID) && Device.HasDocumentTypeDesigners(designer.ID))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerIsAssociatedWithDocumentTypes, designer.ID);
				Device.Designers.Remove(designer.ID);
				designer.ID = newDesignerID;
				designer.Description = (string)newRow["Description"];
				designer.ClassName = (string)newRow["ClassName"];
				Device.Designers.Add(designer);
				Device.SaveDesigners();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void DeleteDesigner(Schema.TableVar tableVar, IRow row)
		{
			Device.AcquireDirectoryLock();
			try
			{
				Designer designer = Device.Designers[(string)row["ID"]];
				if (Device.HasDocumentTypeDesigners(designer.ID))
					throw new FrontendDeviceException(FrontendDeviceException.Codes.DesignerIsAssociatedWithDocumentTypes, designer.ID);
				Device.Designers.Remove(designer.ID);
				Device.SaveDesigners();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void InsertDocumentTypeDesigner(Schema.TableVar tableVar, IRow row)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType documentType = Device.DocumentTypes[(string)row["DocumentType_ID"]];
				Designer designer = Device.Designers[(string)row["Designer_ID"]];
				documentType.Designers.Add(designer.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void UpdateDocumentTypeDesigner(Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType oldDocumentType = Device.DocumentTypes[(string)oldRow["DocumentType_ID"]];
				DocumentType newDocumentType = Device.DocumentTypes[(string)newRow["DocumentType_ID"]];
				Designer oldDesigner = Device.Designers[(string)oldRow["Designer_ID"]];
				Designer newDesigner = Device.Designers[(string)newRow["Designer_ID"]];
				oldDocumentType.Designers.Remove(oldDesigner.ID);
				newDocumentType.Designers.Add(newDesigner.ID);
				Device.SaveDocumentTypes();
			}
			finally
			{
				Device.ReleaseDirectoryLock();
			}
		}
		
		protected void DeleteDocumentTypeDesigner(Schema.TableVar tableVar, IRow row)
		{
			Device.AcquireDirectoryLock();
			try
			{
				DocumentType documentType = Device.DocumentTypes[(string)row["DocumentType_ID"]];
				Designer designer = Device.Designers[(string)row["Designer_ID"]];
				documentType.Designers.Remove(designer.ID);
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

		protected void InsertDocument(Program program, Schema.TableVar tableVar, IRow row)
		{
			Device.CreateDocument
			(
				program, 
				Schema.Object.EnsureRooted((string)row["Library_Name"]), 
				(string)row["Name"],
				(string)row["Type_ID"],
				false
			);
		}
		
		protected void UpdateDocument(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow)
		{
			string oldLibraryName = (string)oldRow["Library_Name"];
			string newLibraryName = (string)newRow["Library_Name"];
			string oldName = (string)oldRow["Name"];
			string newName = (string)newRow["Name"];
			string oldTypeID = (string)oldRow["Type_ID"];
			string newTypeID = (string)newRow["Type_ID"];
			if (oldTypeID != newTypeID)
				throw new ServerException(ServerException.Codes.CannotChangeDocumentType);
			if ((oldLibraryName != newLibraryName) || (oldName != newName))
				Device.RenameDocument
				(
					program, 
					Schema.Object.EnsureRooted(oldLibraryName), 
					Schema.Object.EnsureRooted(oldName), 
					Schema.Object.EnsureRooted(newLibraryName), 
					newName, 
					false
				);
		}
		
		protected void DeleteDocument(Program program, Schema.TableVar tableVar, IRow row)
		{
			Device.DeleteDocument
			(
				program, 
				Schema.Object.EnsureRooted((string)row["Library_Name"]), 
				Schema.Object.EnsureRooted((string)row["Name"]), 
				false
			);
		}

		protected override void InternalInsertRow(Program program, Schema.TableVar tableVar, IRow row, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "Frontend.DocumentTypes" : InsertDocumentType(tableVar, row); break;
				case "Frontend.Designers" : InsertDesigner(tableVar, row); break;
				case "Frontend.DocumentTypeDesigners" : InsertDocumentTypeDesigner(tableVar, row); break;
				case "Frontend.Documents" : InsertDocument(program, tableVar, row); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, tableVar.Name);
			}
			base.InternalInsertRow(program, tableVar, row, valueFlags);
		}
		
		protected override void InternalUpdateRow(Program program, Schema.TableVar tableVar, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			switch (tableVar.Name)
			{
				case "Frontend.DocumentTypes" : UpdateDocumentType(tableVar, oldRow, newRow); break;
				case "Frontend.Designers" : UpdateDesigner(tableVar, oldRow, newRow); break;
				case "Frontend.DocumentTypeDesigners" : UpdateDocumentTypeDesigner(tableVar, oldRow, newRow); break;
				case "Frontend.Documents" : UpdateDocument(program, tableVar, oldRow, newRow); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, tableVar.Name);
			}
			base.InternalUpdateRow(program, tableVar, oldRow, newRow, valueFlags);
		}
		
		protected override void InternalDeleteRow(Program program, Schema.TableVar tableVar, IRow row)
		{
			switch (tableVar.Name)
			{
				case "Frontend.DocumentTypes" : DeleteDocumentType(tableVar, row); break;
				case "Frontend.Designers" : DeleteDesigner(tableVar, row); break;
				case "Frontend.DocumentTypeDesigners" : DeleteDocumentTypeDesigner(tableVar, row); break;
				case "Frontend.Documents" : DeleteDocument(program, tableVar, row); break;
				default : throw new FrontendDeviceException(FrontendDeviceException.Codes.UnsupportedUpdate, tableVar.Name);
			}
			base.InternalDeleteRow(program, tableVar, row);
		}

		// IStreamProvider
		private Hashtable _streams = new Hashtable();
		
		private Stream GetStream(StreamID streamID)
		{
			Stream stream = (Stream)_streams[streamID];
			if (stream == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, streamID.ToString());
			return stream;
		}
		
		protected void DestroyStreams()
		{
			foreach (StreamID streamID in _streams.Keys)
				((Stream)_streams[streamID]).Close();
			_streams.Clear();
		}
		
		public void Create(StreamID streamID, Stream stream)
		{
			_streams.Add(streamID, stream);
		}
		
		public void Destroy(StreamID streamID)
		{
			Stream stream = GetStream(streamID);
			_streams.Remove(streamID);
			stream.Close();
		}
		
		public void Reassign(StreamID oldStreamID, StreamID newStreamID)
		{
			Stream oldStream = GetStream(oldStreamID);
			_streams.Remove(oldStreamID);
			_streams.Add(newStreamID, oldStream);
		}

		public void Close(StreamID streamID)
		{
			// no action to perform
		}

		public Stream Open(StreamID streamID)
		{
			return new CoverStream(GetStream(streamID));
		}
	}
}

