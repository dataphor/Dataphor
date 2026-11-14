/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define LOADFROMLIBRARIES

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Alphora.Dataphor.DAE.Server
{
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.Windows;
	using Alphora.Dataphor.BOP;

	public class Server : Engine
	{
		// Do not localize
		public const string DefaultLibraryDirectory = @"Libraries";
		public const string DefaultLibraryDataDirectory = @"LibraryData";
		public const string DefaultInstanceDirectory = @"Instances";
		public const string DefaultCatalogDirectory = @"Catalog";
		public const string DefaultCatalogDatabaseName = @"DAECatalog";
		public const string DefaultBackupDirectory = @"Backup";
		public const string DefaultSaveDirectory = @"Save";
		public const string DefaultLogDirectory = @"Log";

		public Server() : base()
		{
			Alphora.Dataphor.Windows.AssemblyUtility.Initialize();
		}
		
		internal new ServerSessions Sessions { get { return base.Sessions; } }
		
		internal new IServerSession ConnectAs(SessionInfo sessionInfo)
		{
			return base.ConnectAs(sessionInfo);
		}

		protected override void InternalStart()
		{
			if (LibraryDirectory == String.Empty)
				LibraryDirectory = GetDefaultLibraryDirectory();
			base.InternalStart();
			EnsureGeneralLibraryLoaded();
		}

		public override void ClearCatalog()
		{
			// Detach the class loader events
			Catalog.ClassLoader.OnMiss -= new ClassLoaderMissedEvent(ClassLoaderMiss);
			
			base.ClearCatalog();
			EnsureGeneralLibraryLoaded();
		}

		// Indicates that this server is a full Dataphor server
		public override bool IsEngine
		{
			get { return false; }
		}

		private void EnsureGeneralLibraryLoaded()
		{
			if (!IsEngine)
			{
				// Ensure the general library is loaded
				if (!Catalog.LoadedLibraries.Contains(GeneralLibraryName))
				{
					Program program = new Program(_systemProcess);
					program.Start(null);
					try
					{
						Schema.LibraryUtility.EnsureLibraryRegistered(program, GeneralLibraryName, true);
						_systemSession.CurrentLibrary = SystemLibrary; // reset current library of the system session because it will be set by registering General
					}
					finally
					{
						program.Stop(null);
					}
				}
			}
		}

		public static string GetDefaultLibraryDirectory()
		{
			return Path.Combine(PathUtility.GetBinDirectory(), DefaultLibraryDirectory);
		}

		protected override void InternalLoadAvailableLibraries()
		{
			Schema.LibraryUtility.GetAvailableLibraries(InstanceDirectory, _libraryDirectory, Catalog.Libraries);
		}

		protected override bool DetermineFirstRun()
		{
			return ((ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession).IsEmpty();
		}

		protected override void SnapshotBaseCatalogObjects()
		{
			((ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession).SnapshotBase();
		}

		private string _catalogStoreClassName;
		/// <summary>
		/// Gets or sets the assembly qualified class name of the store used to persist the system catalog.
		/// </summary>
		/// <remarks>
		/// This property cannot be changed once the server has been started. If this property
		/// is not set, the default store class (SQLCEStore in the DAE.SQLCE assembly) will be used.
		/// </remarks>
		public string CatalogStoreClassName
		{
			get { return _catalogStoreClassName; }
			set
			{
				CheckState(ServerState.Stopped);
				_catalogStoreClassName = value;
			}
		}

		/// <summary>
		/// Gets the class name of the store used to persist the system catalog.
		/// </summary>
		/// <returns>The value of the CatalogStoreClassName property if it is set, otherwise, the assembly qualified class name of the SQLCEStore.</returns>
		public string GetCatalogStoreClassName()
		{
			return
				String.IsNullOrEmpty(_catalogStoreClassName)
					? "Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE"
					: _catalogStoreClassName;
		}

		private string _catalogStoreConnectionString;
		/// <summary>
		/// Gets or sets the connection string for the store used to persist the system catalog.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property cannot be changed once the server has been started. If this property is not
		/// set, a default SQLCE connection string will be built that specifies the catalog will be
		/// stored in the Catalog subfolder of the instance directory, and named DAECatalog.sdf.
		/// </para>
		/// <para>
		/// If the CatalogStoreConnectionString is specified, the token %CatalogPath% will be replaced
		/// by the catalog directory of the instance.
		/// </para>
		/// </remarks>
		public string CatalogStoreConnectionString
		{
			get { return _catalogStoreConnectionString; }
			set
			{
				CheckState(ServerState.Stopped);
				_catalogStoreConnectionString = value;
			}
		}

		/// <summary>
		/// Gets the connection string for the store used to persist the system catalog.
		/// </summary>
		/// <returns>The value of the CatalogStoreCnnectionString property if it set, otherwise, a default SQL CE connection string.</returns>
		public string GetCatalogStoreConnectionString()
		{
			return
				String.IsNullOrEmpty(_catalogStoreConnectionString)
					? String.Format("Data Source={0};Password={1};Mode={2}", GetCatalogStoreDatabaseFileName(), String.Empty, "Read Write")
					: _catalogStoreConnectionString.Replace("%CatalogPath%", GetCatalogDirectory());
		}

		/// <summary>
		/// Returns the catalog directory for this instance. This is always a directory named Catalog within the instance directory.
		/// </summary>
		public string GetCatalogDirectory()
		{
			string directory = Path.Combine(InstanceDirectory, DefaultCatalogDirectory);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			return directory;
		}

		public string GetCatalogStoreDatabaseFileName()
		{
			return Path.Combine(GetCatalogDirectory(), Path.ChangeExtension(DefaultCatalogDatabaseName, ".sdf"));
		}

		private string _libraryDirectory = String.Empty;
		/// <summary> The directory the DAE uses to find available libraries. </summary>
		public string LibraryDirectory
		{
			get { return _libraryDirectory; }
			set
			{
				if (State != ServerState.Starting && State != ServerState.Stopped)
					throw new ServerException(ServerException.Codes.InvalidServerState, "Starting or Stopped");
				if ((value == null) || (value == String.Empty))
					_libraryDirectory = GetDefaultLibraryDirectory();
				else
					_libraryDirectory = value;

				string[] directories = _libraryDirectory.Split(';');

				StringBuilder libraryDirectory = new StringBuilder();
				for (int index = 0; index < directories.Length; index++)
				{
					if (!Path.IsPathRooted(directories[index]))
						directories[index] = Path.Combine(PathUtility.GetBinDirectory(), directories[index]);

					if (!Directory.Exists(directories[index]) && !IsEngine)
						Directory.CreateDirectory(directories[index]);

					if (index > 0)
						libraryDirectory.Append(";");

					libraryDirectory.Append(directories[index]);
				}

				_libraryDirectory = libraryDirectory.ToString();
			}
		}

		protected override CatalogDevice CreateCatalogDevice()
		{
			return new ServerCatalogDevice(Schema.Object.GetNextObjectID(), CatalogDeviceName);
		}
		
		protected override void LoadSystemAssemblies()
		{
			base.LoadSystemAssemblies();

			Assembly dAEAssembly = typeof(Server).Assembly;
			_systemLibrary.Assemblies.Add(dAEAssembly);
			Catalog.ClassLoader.RegisterAssembly(_systemLibrary, dAEAssembly);
		}

		public override void LoadAvailableLibraries()
		{
			base.LoadAvailableLibraries();
			lock (Catalog.Libraries)
			{
				// Ensure the general library exists
				if (!Catalog.Libraries.Contains(GeneralLibraryName))
				{
					Schema.Library generalLibrary = new Schema.Library(GeneralLibraryName);
					generalLibrary.Libraries.Add(new Schema.LibraryReference(SystemLibraryName, new VersionNumber(-1, -1, -1, -1)));
					Catalog.Libraries.Add(generalLibrary);
					string libraryDirectory = Path.Combine(Schema.LibraryUtility.GetDefaultLibraryDirectory(LibraryDirectory), generalLibrary.Name);
					if (!Directory.Exists(libraryDirectory))
						Directory.CreateDirectory(libraryDirectory);
					Schema.LibraryUtility.SaveToFile(generalLibrary, Path.Combine(libraryDirectory, Schema.LibraryUtility.GetFileName(generalLibrary.Name)));
				}
			}
		}

		private string SaveServerSettings()
		{
			StringBuilder updateStatement = new StringBuilder();

			if (MaxConcurrentProcesses != DefaultMaxConcurrentProcesses)
			{
				if (updateStatement.Length > 0)
					updateStatement.Append(", ");
				updateStatement.AppendFormat("MaxConcurrentProcesses := {0}", MaxConcurrentProcesses.ToString());
			}

			if (ProcessWaitTimeout != TimeSpan.FromSeconds(DefaultProcessWaitTimeout))
			{
				if (updateStatement.Length > 0)
					updateStatement.Append(", ");
				updateStatement.AppendFormat("ProcessWaitTimeout := TimeSpan.Ticks({0})", ProcessWaitTimeout.Ticks.ToString());
			}

			if (ProcessTerminationTimeout != TimeSpan.FromSeconds(DefaultProcessTerminationTimeout))
			{
				if (updateStatement.Length > 0)
					updateStatement.Append(", ");
				updateStatement.AppendFormat("ProcessTerminateTimeout := TimeSpan.Ticks({0})", ProcessTerminationTimeout.Ticks.ToString());
			}

			if (PlanCacheSize != DefaultPlanCacheSize)
			{
				if (updateStatement.Length > 0)
					updateStatement.Append(", ");
				updateStatement.AppendFormat("PlanCacheSize := {0}", PlanCacheSize.ToString());
			}

			if (updateStatement.Length > 0)
			{
				updateStatement.Insert(0, "update ServerSettings set { ");
				updateStatement.Append(" };\r\n");
			}

			return updateStatement.ToString();
		}

		private Statement SaveSystemDeviceSettings(Schema.Device device)
		{
			AlterDeviceStatement statement = new AlterDeviceStatement();
			statement.DeviceName = device.Name;
			statement.AlterClassDefinition = new AlterClassDefinition();
			statement.AlterClassDefinition.AlterAttributes.AddRange(device.ClassDefinition.Attributes);
			return statement;
		}

		private string SaveSystemDeviceSettings()
		{
			D4TextEmitter emitter = new D4TextEmitter();
			Block block = new Block();
			if (_tempDevice.ClassDefinition.Attributes.Count > 0)
				block.Statements.Add(SaveSystemDeviceSettings(_tempDevice));
			if (_aTDevice.ClassDefinition.Attributes.Count > 0)
				block.Statements.Add(SaveSystemDeviceSettings(_aTDevice));
			if (_catalogDevice.ClassDefinition.Attributes.Count > 0)
				block.Statements.Add(SaveSystemDeviceSettings(_catalogDevice));
			return new D4TextEmitter().Emit(block) + "\r\n";
		}

		private string SaveDeviceSettings(ServerProcess process)
		{
			D4TextEmitter emitter = new D4TextEmitter();
			Block block = new Block();

			IServerProcess localProcess = (IServerProcess)process;
			IServerCursor cursor = localProcess.OpenCursor("select Devices { ID }", null);
			try
			{
				using (IRow row = cursor.Plan.RequestRow())
				{
					while (cursor.Next())
					{
						cursor.Select(row);
						Schema.Device device = process.CatalogDeviceSession.ResolveCatalogObject((int)row[0/*"ID"*/]) as Schema.Device;
						if ((device != null) && (device.ClassDefinition.Attributes.Count > 0))
							block.Statements.Add(SaveSystemDeviceSettings(device));
					}
				}
			}
			finally
			{
				localProcess.CloseCursor(cursor);
			}

			return new D4TextEmitter().Emit(block) + "\r\n";
		}

		private string SaveSecurity(ServerProcess process)
		{
			StringBuilder result = new StringBuilder();
			IServerProcess localProcess = (IServerProcess)process;
			IServerCursor cursor;

			// Users
			result.Append("// Users\r\n");

			cursor = localProcess.OpenCursor("select Users { ID }", null);
			try
			{
				using (IRow row = cursor.Plan.RequestRow())
				{
					while (cursor.Next())
					{
						cursor.Select(row);
						switch ((string)row[0/*"ID"*/])
						{
							case Engine.SystemUserID: break;
							case Engine.AdminUserID:
								if (_adminUser.Password != String.Empty)
									result.AppendFormat("SetEncryptedPassword('{0}', '{1}');\r\n", _adminUser.ID, _adminUser.Password);
								break;

							default:
								Schema.User user = process.CatalogDeviceSession.ResolveUser((string)row[0/*"ID"*/]);
								result.AppendFormat("CreateUserWithEncryptedPassword('{0}', '{1}', '{2}');\r\n", user.ID, user.Name, user.Password);
								break;
						}
					}
				}
			}
			finally
			{
				localProcess.CloseCursor(cursor);
			}

			result.Append("\r\n");
			result.Append("// Device Users\r\n");

			// DeviceUsers
			cursor = localProcess.OpenCursor("select DeviceUsers join (Devices { ID Device_ID, Name Device_Name }) { User_ID, Device_ID }", null);
			try
			{
				using (IRow row = cursor.Plan.RequestRow())
				{
					while (cursor.Next())
					{
						cursor.Select(row);
						Schema.User user = process.CatalogDeviceSession.ResolveUser((string)row[0/*"User_ID"*/]);
						Schema.Device device = (Schema.Device)process.CatalogDeviceSession.ResolveCatalogObject((int)row[1/*"Device_ID"*/]);
						Schema.DeviceUser deviceUser = process.CatalogDeviceSession.ResolveDeviceUser(device, user);
						result.AppendFormat("CreateDeviceUserWithEncryptedPassword('{0}', '{1}', '{2}', '{3}', '{4}');\r\n", deviceUser.User.ID, deviceUser.Device.Name, deviceUser.DeviceUserID, deviceUser.DevicePassword, deviceUser.ConnectionParameters);
					}
				}
			}
			finally
			{
				localProcess.CloseCursor(cursor);
			}

			result.Append("\r\n");
			result.Append("// User Roles\r\n");

			// UserRoles
			cursor = localProcess.OpenCursor("select UserRoles where Role_Name <> 'System.User'", null);
			try
			{
				using (IRow row = cursor.Plan.RequestRow())
				{
					while (cursor.Next())
					{
						cursor.Select(row);

						result.AppendFormat("AddUserToRole('{0}', '{1}');\r\n", (string)row[0/*"User_ID"*/], (string)row[1/*"Role_Name"*/]);
					}
				}
			}
			finally
			{
				localProcess.CloseCursor(cursor);
			}

			result.Append("\r\n");
			result.Append("// User Right Assignments\r\n");

			// UserRightAssignments
			cursor = localProcess.OpenCursor("select UserRightAssignments", null);
			try
			{
				using (IRow row = cursor.Plan.RequestRow())
				{
					while (cursor.Next())
					{
						cursor.Select(row);

						if ((bool)row[2/*"IsGranted"*/])
							result.AppendFormat("GrantRightToUser('{0}', '{1}');\r\n", (string)row[1/*"Right_Name"*/], (string)row[0/*"User_ID"*/]);
						else
							result.AppendFormat("RevokeRightFromUser('{0}', '{1}');\r\n", (string)row[1/*"Right_Name"*/], (string)row[0/*"User_ID"*/]);
					}
				}
			}
			finally
			{
				localProcess.CloseCursor(cursor);
			}

			result.Append("\r\n");
			return result.ToString();
		}

		/// <summary>Returns a script to recreate the server state.</summary>
		public string ScriptServerState(ServerProcess process)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("// Server Settings\r\n");
			builder.Append(SaveServerSettings());

			builder.Append("\r\n");
			builder.Append("// Device Settings\r\n");
			builder.Append(SaveDeviceSettings(process));
			builder.Append("\r\n");

			builder.Append(SaveSecurity(process));
			return builder.ToString();
		}

		protected override void LoadServerState()
		{
			ServerCatalogDeviceSession catalogDeviceSession = (ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession;
			
			// Load server configuration settings
			catalogDeviceSession.LoadServerSettings(this);

			// Set the object ID generator				
			Schema.Object.SetNextObjectID(catalogDeviceSession.GetMaxObjectID() + 1);

			// Insert a row into TableDee...
			RunScript(_systemProcess, "insert table { row { } } into TableDee;");

			// Ensure all loaded libraries are loaded
			_systemProcess.CatalogDeviceSession.ResolveLoadedLibraries();
		}
		
		#region Logging

		private string GetLogFileName()
		{
			string logFileName = GetLogFileName(MaxLogs);
			if (File.Exists(logFileName))
				File.Delete(logFileName);
			for (int index = MaxLogs - 1; index >= 0; index--)
			{
				logFileName = GetLogFileName(index);
				if (File.Exists(logFileName))
					File.Move(logFileName, GetLogFileName(index + 1));
			}
			return GetLogFileName(0);
		}

		private string GetLogName(int logIndex)
		{
			return String.Format("{0}{1}", ServerLogName, logIndex == 0 ? " (current)" : logIndex.ToString());
		}

		private string GetLogDirectory()
		{
			string result = Path.Combine(InstanceDirectory, DefaultLogDirectory);
			Directory.CreateDirectory(result);
			return result;
		}

		private string GetLogFileName(int logIndex)
		{
			return Path.Combine(GetLogDirectory(), String.Format("{0}{1}.log", ServerLogName, logIndex == 0 ? String.Empty : logIndex.ToString()));
		}

		public override List<string> ListLogs()
		{
			List<string> logList = new List<string>();
			for (int index = 0; index <= MaxLogs; index++)
			{
				string logName = GetLogName(index);
				string logFileName = GetLogFileName(index);
				if (File.Exists(logFileName))
					logList.Add(logName);
			}

			return logList;
		}

		private FileStream _logFile;
		private StreamWriter _log;

		public EventLogEntryType LogEntryTypeToEventLogEntryType(LogEntryType entryType)
		{
			switch (entryType)
			{
				case LogEntryType.Error : return EventLogEntryType.Error;
				case LogEntryType.Warning : return EventLogEntryType.Warning;
				default : return EventLogEntryType.Information;
			}
		}
		
		public override void LogMessage(LogEntryType entryType, string description)
		{
			if (LoggingEnabled)
			{
				if (IsAdministrator() && (System.Environment.OSVersion.Platform == PlatformID.Win32NT))
					try
					{
						EventLog.WriteEntry(ServerSourceName, String.Format("Server: {0}\r\n{1}", Name, description), LogEntryTypeToEventLogEntryType(entryType));
					}
					catch
					{
						// ignore an error writing to the event log (it's probably complaining that it's full)
					}

				if (_log != null)
				{
					lock (_log)
					{
						_log.Write
						(
							String.Format
							(
								"{0} {1}{2}\r\n",
								DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"),
								(entryType == LogEntryType.Information) ?
									String.Empty :
									String.Format("{0}: ", entryType.ToString()),
								description
							)
						);
					}
				}
			}
		}

		protected override void StartLog()
		{
			if (LoggingEnabled)
			{
				if (!IsEngine && IsAdministrator())
				{
					if (!EventLog.SourceExists(ServerSourceName))
						EventLog.CreateEventSource(ServerSourceName, ServerLogName);
				}
				try
				{
					OpenLogFile(GetLogFileName());
				}
				catch
				{
					StopLog();
					if (!IsEngine)
						throw; // Eat the error if this is a repository
				}

				LogEvent(DAE.Server.LogEvent.LogStarted);
			}
		}

		private bool IsAdministrator()
		{
			if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				WindowsIdentity wID = WindowsIdentity.GetCurrent();
				WindowsPrincipal wP = new WindowsPrincipal(wID);
				return wP.IsInRole(WindowsBuiltInRole.Administrator);
			}
			else
				return (false); // Might not be Windows
		}

		protected override void StopLog()
		{
			if (LoggingEnabled && (_log != null))
			{
				LogEvent(DAE.Server.LogEvent.LogStopped);
				try
				{
					CloseLogFile();
				}
				finally
				{
					_log = null;
					_logFile = null;
				}
			}
		}

		private void OpenLogFile(string logFileName)
		{
			_logFile = new FileStream(logFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			_logFile.Position = _logFile.Length;
			_log = new StreamWriter(_logFile);
			_log.AutoFlush = true;
		}

		private void CloseLogFile()
		{
			_log.Flush();
			_log.Close();
			_logFile.Close();
		}

		public string ShowLog()
		{
			if (LoggingEnabled && (_log != null))
			{
				string logFileName = _logFile.Name;
				CloseLogFile();
				try
				{
					using (StreamReader reader = new StreamReader(logFileName))
					{
						return reader.ReadToEnd();
					}
				}
				finally
				{
					OpenLogFile(logFileName);
				}
			}

			return String.Empty;
		}

		public string ShowLog(int logIndex)
		{
			if (logIndex == 0)
				return ShowLog();
			else
			{
				using (StreamReader reader = new StreamReader(GetLogFileName(logIndex)))
				{
					return reader.ReadToEnd();
				}
			}
		}

		#endregion

		private string _instanceDirectory;
		/// <summary>
		/// The primary data directory for the instance. All write activity for the server should occur in this directory (logging, catalog, device data, etc.,.)
		/// </summary>
		public string InstanceDirectory
		{
			get
			{
				if (State != ServerState.Stopped)
				{
					if (String.IsNullOrEmpty(_instanceDirectory))
						_instanceDirectory = Name;

					if (!Path.IsPathRooted(_instanceDirectory))
						_instanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), DefaultInstanceDirectory), _instanceDirectory);

					if (!Directory.Exists(_instanceDirectory))
						Directory.CreateDirectory(_instanceDirectory);
				}

				return _instanceDirectory;
			}
			set
			{
				CheckState(ServerState.Stopped);
				_instanceDirectory = value;
			}
		}

		protected override void InternalRegisterCoreSystemObjects()
		{
			base.InternalRegisterCoreSystemObjects();

			var catalogSession = (ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession;
			
			// Grant usage rights on the Temp and A/T devices to the User role
			catalogSession.GrantRightToRole(_tempDevice.GetRight(Schema.RightNames.CreateStore), _userRole.ID);
			catalogSession.GrantRightToRole(_tempDevice.GetRight(Schema.RightNames.AlterStore), _userRole.ID);
			catalogSession.GrantRightToRole(_tempDevice.GetRight(Schema.RightNames.DropStore), _userRole.ID);
			catalogSession.GrantRightToRole(_tempDevice.GetRight(Schema.RightNames.Read), _userRole.ID);
			catalogSession.GrantRightToRole(_tempDevice.GetRight(Schema.RightNames.Write), _userRole.ID);

			catalogSession.GrantRightToRole(_aTDevice.GetRight(Schema.RightNames.CreateStore), _userRole.ID);
			catalogSession.GrantRightToRole(_aTDevice.GetRight(Schema.RightNames.AlterStore), _userRole.ID);
			catalogSession.GrantRightToRole(_aTDevice.GetRight(Schema.RightNames.DropStore), _userRole.ID);
			catalogSession.GrantRightToRole(_aTDevice.GetRight(Schema.RightNames.Read), _userRole.ID);
			catalogSession.GrantRightToRole(_aTDevice.GetRight(Schema.RightNames.Write), _userRole.ID);

			// Create and register the system rights
			catalogSession.InsertRight(Schema.RightNames.CreateType, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateTable, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateView, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateOperator, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateDevice, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateConstraint, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateReference, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateUser, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.CreateRole, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.AlterUser, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.DropUser, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.MaintainSystemDeviceUsers, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.MaintainUserSessions, _systemUser.ID);
			catalogSession.InsertRight(Schema.RightNames.HostImplementation, _systemUser.ID);

			// Grant create rights to the User role
			catalogSession.GrantRightToRole(Schema.RightNames.CreateType, _userRole.ID);
			catalogSession.GrantRightToRole(Schema.RightNames.CreateTable, _userRole.ID);
			catalogSession.GrantRightToRole(Schema.RightNames.CreateView, _userRole.ID);
			catalogSession.GrantRightToRole(Schema.RightNames.CreateOperator, _userRole.ID);
			catalogSession.GrantRightToRole(Schema.RightNames.CreateDevice, _userRole.ID);
			catalogSession.GrantRightToRole(Schema.RightNames.CreateConstraint, _userRole.ID);
			catalogSession.GrantRightToRole(Schema.RightNames.CreateReference, _userRole.ID);
		}

		internal Schema.Objects GetBaseCatalogObjects()
		{
			CheckState(ServerState.Started);
			return ((ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession).GetBaseCatalogObjects();
		}

		protected override Schema.User ValidateLogin(int sessionID, SessionInfo sessionInfo)
		{
			if (sessionInfo == null)
				throw new ServerException(ServerException.Codes.SessionInformationRequired);

			if (String.Equals(sessionInfo.UserID, SystemUserID, StringComparison.OrdinalIgnoreCase))
			{
				if (!IsEngine && (sessionID != SystemSessionID))
					throw new ServerException(ServerException.Codes.CannotLoginAsSystemUser);

				return _systemUser;
			}
			else
			{
				Schema.User user = ((ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession).ResolveUser(sessionInfo.UserID);
				if (String.Compare(Schema.SecurityUtility.DecryptPassword(user.Password), sessionInfo.Password, true) != 0)
					throw new ServerException(ServerException.Codes.InvalidPassword);

				return user;
			}
		}

		protected override void InternalInitializeCatalog()
		{
			// Wire up the class loader events
			Catalog.ClassLoader.OnMiss += new ClassLoaderMissedEvent(ClassLoaderMiss);
			
			if (_firstRun)
				base.InternalInitializeCatalog();
			else
			{
				// Attaches any libraries that were attached from an explicit library directory
				((ServerCatalogDeviceSession)_systemProcess.CatalogDeviceSession).AttachLibraries();

			    // resolve the core system objects
			    ResolveCoreSystemObjects();
				
			    // resolve the system data types
			    ResolveSystemDataTypes();
			}
		}

		private void ClassLoaderMiss(ClassLoader classLoader, CatalogDeviceSession session, ClassDefinition classDefinition)
		{
			// Ensure that the library containing the class is loaded.
			((ServerCatalogDeviceSession)session).LoadLibraryForClass(classDefinition);
		}
		
		private void ResolveCoreSystemObjects()
		{
			// Use the persistent catalog store to resolve references to the core catalog objects
			_systemProcess.CatalogDeviceSession.ResolveUser(SystemUserID);
			_systemProcess.CatalogDeviceSession.CacheCatalogObject(_catalogDevice);
			_adminUser = _systemProcess.CatalogDeviceSession.ResolveUser(AdminUserID);
			_userRole = (Schema.Role)_systemProcess.CatalogDeviceSession.ResolveName(UserRoleName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			_tempDevice = (MemoryDevice)_systemProcess.CatalogDeviceSession.ResolveName(TempDeviceName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			_aTDevice = (ApplicationTransactionDevice)_systemProcess.CatalogDeviceSession.ResolveName(ATDeviceName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
		}

		private void ResolveSystemDataTypes()
		{
			// Note that these (and the native type references set in BindNativeTypes) are also set in the CatalogDevice (FixupSystemTypeReferences)
			// The functionality is duplicated to ensure that the references will be set on a delayed load of a system type, as well as to ensure that a repository functions correctly without delayed resolution
			Catalog.DataTypes.SystemScalar = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemScalarName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemBoolean = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemBooleanName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemDecimal = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemDecimalName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemLong = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemLongName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemInteger = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemIntegerName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemShort = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemShortName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemByte = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemByteName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemString = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemStringName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemTimeSpan = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemTimeSpanName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemDateTime = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemDateTimeName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemDate = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemDateName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemTime = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemTimeName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemMoney = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemMoneyName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemGuid = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemGuidName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemBinary = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemBinaryName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemGraphic = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemGraphicName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemError = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemErrorName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
			Catalog.DataTypes.SystemName = (Schema.ScalarType)_systemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.SystemNameName, _systemLibrary.GetNameResolutionPath(_systemLibrary), new List<string>());
		}

		internal ServerFileInfos GetFileNames(Schema.Library library, string environment)
		{
			ServerFileInfos fileInfos = new ServerFileInfos();
			Schema.Libraries libraries = new Schema.Libraries();
			libraries.Add(library);
			
			while (libraries.Count > 0)
			{
				Schema.Library localLibrary = libraries[0];
				libraries.RemoveAt(0);
				
				foreach (Schema.FileReference reference in library.Files)
				{
					if (reference.Environments.Contains(environment) && !fileInfos.Contains(reference.FileName))
					{
						string fullFileName = GetFullFileName(library, reference.FileName);
						fileInfos.Add
						(
							new ServerFileInfo 
							{ 
								LibraryName = library.Name, 
								FileName = reference.FileName, 
								FileDate = File.GetLastWriteTimeUtc(fullFileName), 
								IsDotNetAssembly = FileUtility.IsAssembly(fullFileName), 
								ShouldRegister = reference.IsAssembly 
							}
						);
					} 
				}
				
				foreach (Schema.LibraryReference reference in localLibrary.Libraries)
					if (!libraries.Contains(reference.Name))
						libraries.Add(Catalog.Libraries[reference.Name]);
			}
			
			return fileInfos;
		}

		public string GetFullFileName(Schema.Library library, string fileName)
		{
			#if LOADFROMLIBRARIES
			return 
				Path.IsPathRooted(fileName) 
					? fileName 
					: 
						library.Name == Engine.SystemLibraryName
							? PathUtility.GetFullFileName(fileName)
							: Path.Combine(Schema.LibraryUtility.GetLibraryDirectory(library, LibraryDirectory), fileName);
			#else
			return PathUtility.GetFullFileName(AFileName);
			#endif
		}
	}
}
