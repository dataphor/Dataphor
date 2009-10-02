/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

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
		public const string CDefaultLibraryDirectory = @"Libraries";
		public const string CDefaultLibraryDataDirectory = @"LibraryData";
		public const string CDefaultInstanceDirectory = @"Instances";
		public const string CDefaultCatalogDirectory = @"Catalog";
		public const string CDefaultCatalogDatabaseName = @"DAECatalog";
		public const string CDefaultBackupDirectory = @"Backup";
		public const string CDefaultSaveDirectory = @"Save";
		public const string CDefaultLogDirectory = @"Log";

		public Server() : base()
		{
			Alphora.Dataphor.Windows.AssemblyUtility.Initialize();
		}
		
		internal new ServerSessions Sessions { get { return base.Sessions; } }
		
		internal new IServerSession ConnectAs(SessionInfo ASessionInfo)
		{
			return base.ConnectAs(ASessionInfo);
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
				if (!Catalog.LoadedLibraries.Contains(CGeneralLibraryName))
				{
					Program LProgram = new Program(FSystemProcess);
					LProgram.Start(null);
					try
					{
						Schema.LibraryUtility.EnsureLibraryRegistered(LProgram, CGeneralLibraryName, true);
						FSystemSession.CurrentLibrary = SystemLibrary; // reset current library of the system session because it will be set by registering General
					}
					finally
					{
						LProgram.Stop(null);
					}
				}
			}
		}

		public static string GetDefaultLibraryDirectory()
		{
			return Path.Combine(PathUtility.GetBinDirectory(), CDefaultLibraryDirectory);
		}

		protected override void InternalLoadAvailableLibraries()
		{
			Schema.LibraryUtility.GetAvailableLibraries(InstanceDirectory, FLibraryDirectory, Catalog.Libraries);
		}

		protected override bool DetermineFirstRun()
		{
			return ((ServerCatalogDeviceSession)FSystemProcess.CatalogDeviceSession).IsEmpty();
		}

		protected override void SnapshotBaseCatalogObjects()
		{
			((ServerCatalogDeviceSession)FSystemProcess.CatalogDeviceSession).SnapshotBase();
		}

		private string FCatalogStoreClassName;
		/// <summary>
		/// Gets or sets the assembly qualified class name of the store used to persist the system catalog.
		/// </summary>
		/// <remarks>
		/// This property cannot be changed once the server has been started. If this property
		/// is not set, the default store class (SQLCEStore in the DAE.SQLCE assembly) will be used.
		/// </remarks>
		public string CatalogStoreClassName
		{
			get { return FCatalogStoreClassName; }
			set
			{
				CheckState(ServerState.Stopped);
				FCatalogStoreClassName = value;
			}
		}

		/// <summary>
		/// Gets the class name of the store used to persist the system catalog.
		/// </summary>
		/// <returns>The value of the CatalogStoreClassName property if it is set, otherwise, the assembly qualified class name of the SQLCEStore.</returns>
		public string GetCatalogStoreClassName()
		{
			return
				String.IsNullOrEmpty(FCatalogStoreClassName)
					? "Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE"
					: FCatalogStoreClassName;
		}

		private string FCatalogStoreConnectionString;
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
			get { return FCatalogStoreConnectionString; }
			set
			{
				CheckState(ServerState.Stopped);
				FCatalogStoreConnectionString = value;
			}
		}

		/// <summary>
		/// Gets the connection string for the store used to persist the system catalog.
		/// </summary>
		/// <returns>The value of the CatalogStoreCnnectionString property if it set, otherwise, a default SQL CE connection string.</returns>
		public string GetCatalogStoreConnectionString()
		{
			return
				String.IsNullOrEmpty(FCatalogStoreConnectionString)
					? String.Format("Data Source={0};Password={1};Mode={2}", GetCatalogStoreDatabaseFileName(), String.Empty, "Read Write")
					: FCatalogStoreConnectionString.Replace("%CatalogPath%", GetCatalogDirectory());
		}

		/// <summary>
		/// Returns the catalog directory for this instance. This is always a directory named Catalog within the instance directory.
		/// </summary>
		public string GetCatalogDirectory()
		{
			string LDirectory = Path.Combine(InstanceDirectory, CDefaultCatalogDirectory);
			if (!Directory.Exists(LDirectory))
				Directory.CreateDirectory(LDirectory);
			return LDirectory;
		}

		public string GetCatalogStoreDatabaseFileName()
		{
			return Path.Combine(GetCatalogDirectory(), Path.ChangeExtension(CDefaultCatalogDatabaseName, ".sdf"));
		}

		private string FLibraryDirectory = String.Empty;
		/// <summary> The directory the DAE uses to find available libraries. </summary>
		public string LibraryDirectory
		{
			get { return FLibraryDirectory; }
			set
			{
				if (State != ServerState.Starting && State != ServerState.Stopped)
					throw new ServerException(ServerException.Codes.InvalidServerState, "Starting or Stopped");
				if ((value == null) || (value == String.Empty))
					FLibraryDirectory = GetDefaultLibraryDirectory();
				else
					FLibraryDirectory = value;

				string[] LDirectories = FLibraryDirectory.Split(';');

				StringBuilder LLibraryDirectory = new StringBuilder();
				for (int LIndex = 0; LIndex < LDirectories.Length; LIndex++)
				{
					if (!Path.IsPathRooted(LDirectories[LIndex]))
						LDirectories[LIndex] = Path.Combine(PathUtility.GetBinDirectory(), LDirectories[LIndex]);

					if (!Directory.Exists(LDirectories[LIndex]) && !IsEngine)
						Directory.CreateDirectory(LDirectories[LIndex]);

					if (LIndex > 0)
						LLibraryDirectory.Append(";");

					LLibraryDirectory.Append(LDirectories[LIndex]);
				}

				FLibraryDirectory = LLibraryDirectory.ToString();
			}
		}

		protected override CatalogDevice CreateCatalogDevice()
		{
			return new ServerCatalogDevice(Schema.Object.GetNextObjectID(), CCatalogDeviceName);
		}
		
		protected override void LoadSystemAssemblies()
		{
			base.LoadSystemAssemblies();

			Assembly LDAEAssembly = typeof(Server).Assembly;
			FSystemLibrary.Assemblies.Add(LDAEAssembly);
			Catalog.ClassLoader.RegisterAssembly(FSystemLibrary, LDAEAssembly);
		}

		public override void LoadAvailableLibraries()
		{
			base.LoadAvailableLibraries();
			lock (Catalog.Libraries)
			{
				// Ensure the general library exists
				if (!Catalog.Libraries.Contains(CGeneralLibraryName))
				{
					Schema.Library LGeneralLibrary = new Schema.Library(CGeneralLibraryName);
					LGeneralLibrary.Libraries.Add(new Schema.LibraryReference(CSystemLibraryName, new VersionNumber(-1, -1, -1, -1)));
					Catalog.Libraries.Add(LGeneralLibrary);
					string LLibraryDirectory = Path.Combine(Schema.LibraryUtility.GetDefaultLibraryDirectory(LibraryDirectory), LGeneralLibrary.Name);
					if (!Directory.Exists(LLibraryDirectory))
						Directory.CreateDirectory(LLibraryDirectory);
					Schema.LibraryUtility.SaveToFile(LGeneralLibrary, Path.Combine(LLibraryDirectory, Schema.LibraryUtility.GetFileName(LGeneralLibrary.Name)));
				}
			}
		}

		private string SaveServerSettings()
		{
			StringBuilder LUpdateStatement = new StringBuilder();

			if (MaxConcurrentProcesses != CDefaultMaxConcurrentProcesses)
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("MaxConcurrentProcesses := {0}", MaxConcurrentProcesses.ToString());
			}

			if (ProcessWaitTimeout != TimeSpan.FromSeconds(CDefaultProcessWaitTimeout))
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("ProcessWaitTimeout := TimeSpan.Ticks({0})", ProcessWaitTimeout.Ticks.ToString());
			}

			if (ProcessTerminationTimeout != TimeSpan.FromSeconds(CDefaultProcessTerminationTimeout))
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("ProcessTerminateTimeout := TimeSpan.Ticks({0})", ProcessTerminationTimeout.Ticks.ToString());
			}

			if (PlanCacheSize != CDefaultPlanCacheSize)
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("PlanCacheSize := {0}", PlanCacheSize.ToString());
			}

			if (LUpdateStatement.Length > 0)
			{
				LUpdateStatement.Insert(0, "update ServerSettings set { ");
				LUpdateStatement.Append(" };\r\n");
			}

			return LUpdateStatement.ToString();
		}

		private Statement SaveSystemDeviceSettings(Schema.Device ADevice)
		{
			AlterDeviceStatement LStatement = new AlterDeviceStatement();
			LStatement.DeviceName = ADevice.Name;
			LStatement.AlterClassDefinition = new AlterClassDefinition();
			LStatement.AlterClassDefinition.AlterAttributes.AddRange(ADevice.ClassDefinition.Attributes);
			return LStatement;
		}

		private string SaveSystemDeviceSettings()
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			Block LBlock = new Block();
			if (FTempDevice.ClassDefinition.Attributes.Count > 0)
				LBlock.Statements.Add(SaveSystemDeviceSettings(FTempDevice));
			if (FATDevice.ClassDefinition.Attributes.Count > 0)
				LBlock.Statements.Add(SaveSystemDeviceSettings(FATDevice));
			if (FCatalogDevice.ClassDefinition.Attributes.Count > 0)
				LBlock.Statements.Add(SaveSystemDeviceSettings(FCatalogDevice));
			return new D4TextEmitter().Emit(LBlock) + "\r\n";
		}

		private string SaveDeviceSettings(ServerProcess AProcess)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			Block LBlock = new Block();

			IServerProcess LProcess = (IServerProcess)AProcess;
			IServerCursor LCursor = LProcess.OpenCursor("select Devices { ID }", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						Schema.Device LDevice = AProcess.CatalogDeviceSession.ResolveCatalogObject((int)LRow[0/*"ID"*/]) as Schema.Device;
						if ((LDevice != null) && (LDevice.ClassDefinition.Attributes.Count > 0))
							LBlock.Statements.Add(SaveSystemDeviceSettings(LDevice));
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			return new D4TextEmitter().Emit(LBlock) + "\r\n";
		}

		private string SaveSecurity(ServerProcess AProcess)
		{
			StringBuilder LResult = new StringBuilder();
			IServerProcess LProcess = (IServerProcess)AProcess;
			IServerCursor LCursor;

			// Users
			LResult.Append("// Users\r\n");

			LCursor = LProcess.OpenCursor("select Users { ID }", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						switch ((string)LRow[0/*"ID"*/])
						{
							case Engine.CSystemUserID: break;
							case Engine.CAdminUserID:
								if (FAdminUser.Password != String.Empty)
									LResult.AppendFormat("SetEncryptedPassword('{0}', '{1}');\r\n", FAdminUser.ID, FAdminUser.Password);
								break;

							default:
								Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser((string)LRow[0/*"ID"*/]);
								LResult.AppendFormat("CreateUserWithEncryptedPassword('{0}', '{1}', '{2}');\r\n", LUser.ID, LUser.Name, LUser.Password);
								break;
						}
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			LResult.Append("// Device Users\r\n");

			// DeviceUsers
			LCursor = LProcess.OpenCursor("select DeviceUsers join (Devices { ID Device_ID, Name Device_Name }) { User_ID, Device_ID }", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser((string)LRow[0/*"User_ID"*/]);
						Schema.Device LDevice = (Schema.Device)AProcess.CatalogDeviceSession.ResolveCatalogObject((int)LRow[1/*"Device_ID"*/]);
						Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
						LResult.AppendFormat("CreateDeviceUserWithEncryptedPassword('{0}', '{1}', '{2}', '{3}', '{4}');\r\n", LDeviceUser.User.ID, LDeviceUser.Device.Name, LDeviceUser.DeviceUserID, LDeviceUser.DevicePassword, LDeviceUser.ConnectionParameters);
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			LResult.Append("// User Roles\r\n");

			// UserRoles
			LCursor = LProcess.OpenCursor("select UserRoles where Role_Name <> 'System.User'", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);

						LResult.AppendFormat("AddUserToRole('{0}', '{1}');\r\n", (string)LRow[0/*"User_ID"*/], (string)LRow[1/*"Role_Name"*/]);
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			LResult.Append("// User Right Assignments\r\n");

			// UserRightAssignments
			LCursor = LProcess.OpenCursor("select UserRightAssignments", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);

						if ((bool)LRow[2/*"IsGranted"*/])
							LResult.AppendFormat("GrantRightToUser('{0}', '{1}');\r\n", (string)LRow[1/*"Right_Name"*/], (string)LRow[0/*"User_ID"*/]);
						else
							LResult.AppendFormat("RevokeRightFromUser('{0}', '{1}');\r\n", (string)LRow[1/*"Right_Name"*/], (string)LRow[0/*"User_ID"*/]);
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			return LResult.ToString();
		}

		/// <summary>Returns a script to recreate the server state.</summary>
		public string ScriptServerState(ServerProcess AProcess)
		{
			StringBuilder LBuilder = new StringBuilder();
			LBuilder.Append("// Server Settings\r\n");
			LBuilder.Append(SaveServerSettings());

			LBuilder.Append("\r\n");
			LBuilder.Append("// Device Settings\r\n");
			LBuilder.Append(SaveDeviceSettings(AProcess));
			LBuilder.Append("\r\n");

			LBuilder.Append(SaveSecurity(AProcess));
			return LBuilder.ToString();
		}

		protected override void LoadServerState()
		{
			ServerCatalogDeviceSession LCatalogDeviceSession = (ServerCatalogDeviceSession)FSystemProcess.CatalogDeviceSession;
			
			// Load server configuration settings
			LCatalogDeviceSession.LoadServerSettings(this);

			// Attaches any libraries that were attached from an explicit library directory
			LCatalogDeviceSession.AttachLibraries();

			// Set the object ID generator				
			Schema.Object.SetNextObjectID(LCatalogDeviceSession.GetMaxObjectID() + 1);

			// Insert a row into TableDee...
			RunScript(FSystemProcess, "insert table { row { } } into TableDee;");

			// Ensure all loaded libraries are loaded
			FSystemProcess.CatalogDeviceSession.ResolveLoadedLibraries();
		}
		
		#region Logging

		private string GetLogFileName()
		{
			string LLogFileName = GetLogFileName(CMaxLogs);
			if (File.Exists(LLogFileName))
				File.Delete(LLogFileName);
			for (int LIndex = CMaxLogs - 1; LIndex >= 0; LIndex--)
			{
				LLogFileName = GetLogFileName(LIndex);
				if (File.Exists(LLogFileName))
					File.Move(LLogFileName, GetLogFileName(LIndex + 1));
			}
			return GetLogFileName(0);
		}

		private string GetLogName(int ALogIndex)
		{
			return String.Format("{0}{1}", CServerLogName, ALogIndex == 0 ? " (current)" : ALogIndex.ToString());
		}

		private string GetLogDirectory()
		{
			string LResult = Path.Combine(InstanceDirectory, CDefaultLogDirectory);
			Directory.CreateDirectory(LResult);
			return LResult;
		}

		private string GetLogFileName(int ALogIndex)
		{
			return Path.Combine(GetLogDirectory(), String.Format("{0}{1}.log", CServerLogName, ALogIndex == 0 ? String.Empty : ALogIndex.ToString()));
		}

		public override List<string> ListLogs()
		{
			List<string> LLogList = new List<string>();
			for (int LIndex = 0; LIndex <= CMaxLogs; LIndex++)
			{
				string LLogName = GetLogName(LIndex);
				string LLogFileName = GetLogFileName(LIndex);
				if (File.Exists(LLogFileName))
					LLogList.Add(LLogName);
			}

			return LLogList;
		}

		private FileStream FLogFile;
		private StreamWriter FLog;

		public EventLogEntryType LogEntryTypeToEventLogEntryType(LogEntryType AEntryType)
		{
			switch (AEntryType)
			{
				case LogEntryType.Error : return EventLogEntryType.Error;
				case LogEntryType.Warning : return EventLogEntryType.Warning;
				default : return EventLogEntryType.Information;
			}
		}
		
		public override void LogMessage(LogEntryType AEntryType, string ADescription)
		{
			if (LoggingEnabled)
			{
				if (IsAdministrator() && (System.Environment.OSVersion.Platform == PlatformID.Win32NT))
					try
					{
						EventLog.WriteEntry(CServerSourceName, String.Format("Server: {0}\r\n{1}", Name, ADescription), LogEntryTypeToEventLogEntryType(AEntryType));
					}
					catch
					{
						// ignore an error writing to the event log (it's probably complaining that it's full)
					}

				if (FLog != null)
				{
					lock (FLog)
					{
						FLog.Write
						(
							String.Format
							(
								"{0} {1}{2}\r\n",
								DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"),
								(AEntryType == LogEntryType.Information) ?
									String.Empty :
									String.Format("{0}: ", AEntryType.ToString()),
								ADescription
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
					if (!EventLog.SourceExists(CServerSourceName))
						EventLog.CreateEventSource(CServerSourceName, CServerLogName);
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
				WindowsIdentity LWID = WindowsIdentity.GetCurrent();
				WindowsPrincipal LWP = new WindowsPrincipal(LWID);
				return LWP.IsInRole(WindowsBuiltInRole.Administrator);
			}
			else
				return (false); // Might not be Windows
		}

		protected override void StopLog()
		{
			if (LoggingEnabled && (FLog != null))
			{
				LogEvent(DAE.Server.LogEvent.LogStopped);
				try
				{
					CloseLogFile();
				}
				finally
				{
					FLog = null;
					FLogFile = null;
				}
			}
		}

		private void OpenLogFile(string ALogFileName)
		{
			FLogFile = new FileStream(ALogFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			FLogFile.Position = FLogFile.Length;
			FLog = new StreamWriter(FLogFile);
			FLog.AutoFlush = true;
		}

		private void CloseLogFile()
		{
			FLog.Flush();
			FLog.Close();
			FLogFile.Close();
		}

		public string ShowLog()
		{
			if (LoggingEnabled && (FLog != null))
			{
				string LLogFileName = FLogFile.Name;
				CloseLogFile();
				try
				{
					using (StreamReader LReader = new StreamReader(LLogFileName))
					{
						return LReader.ReadToEnd();
					}
				}
				finally
				{
					OpenLogFile(LLogFileName);
				}
			}

			return String.Empty;
		}

		public string ShowLog(int ALogIndex)
		{
			if (ALogIndex == 0)
				return ShowLog();
			else
			{
				using (StreamReader LReader = new StreamReader(GetLogFileName(ALogIndex)))
				{
					return LReader.ReadToEnd();
				}
			}
		}

		#endregion

		private string FInstanceDirectory;
		/// <summary>
		/// The primary data directory for the instance. All write activity for the server should occur in this directory (logging, catalog, device data, etc.,.)
		/// </summary>
		public string InstanceDirectory
		{
			get
			{
				if (State != ServerState.Stopped)
				{
					if (String.IsNullOrEmpty(FInstanceDirectory))
						FInstanceDirectory = Name;

					if (!Path.IsPathRooted(FInstanceDirectory))
						FInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), CDefaultInstanceDirectory), FInstanceDirectory);

					if (!Directory.Exists(FInstanceDirectory))
						Directory.CreateDirectory(FInstanceDirectory);
				}

				return FInstanceDirectory;
			}
			set
			{
				CheckState(ServerState.Stopped);
				FInstanceDirectory = value;
			}
		}

		protected override void InternalRegisterCoreSystemObjects()
		{
			base.InternalRegisterCoreSystemObjects();

			var LCatalogSession = (ServerCatalogDeviceSession)FSystemProcess.CatalogDeviceSession;
			
			// Grant usage rights on the Temp and A/T devices to the User role
			LCatalogSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.CreateStore), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.AlterStore), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.DropStore), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.Read), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.Write), FUserRole.ID);

			LCatalogSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.CreateStore), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.AlterStore), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.DropStore), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.Read), FUserRole.ID);
			LCatalogSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.Write), FUserRole.ID);

			// Create and register the system rights
			LCatalogSession.InsertRight(Schema.RightNames.CreateType, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateTable, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateView, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateOperator, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateDevice, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateConstraint, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateReference, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateUser, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.CreateRole, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.AlterUser, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.DropUser, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.MaintainSystemDeviceUsers, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.MaintainUserSessions, FSystemUser.ID);
			LCatalogSession.InsertRight(Schema.RightNames.HostImplementation, FSystemUser.ID);

			// Grant create rights to the User role
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateType, FUserRole.ID);
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateTable, FUserRole.ID);
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateView, FUserRole.ID);
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateOperator, FUserRole.ID);
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateDevice, FUserRole.ID);
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateConstraint, FUserRole.ID);
			LCatalogSession.GrantRightToRole(Schema.RightNames.CreateReference, FUserRole.ID);
		}

		internal Schema.Objects GetBaseCatalogObjects()
		{
			CheckState(ServerState.Started);
			return ((ServerCatalogDeviceSession)FSystemProcess.CatalogDeviceSession).GetBaseCatalogObjects();
		}

		protected override Schema.User ValidateLogin(int ASessionID, SessionInfo ASessionInfo)
		{
			if (ASessionInfo == null)
				throw new ServerException(ServerException.Codes.SessionInformationRequired);

			if (String.Equals(ASessionInfo.UserID, CSystemUserID, StringComparison.OrdinalIgnoreCase))
			{
				if (!IsEngine && (ASessionID != CSystemSessionID))
					throw new ServerException(ServerException.Codes.CannotLoginAsSystemUser);

				return FSystemUser;
			}
			else
			{
				Schema.User LUser = ((ServerCatalogDeviceSession)FSystemProcess.CatalogDeviceSession).ResolveUser(ASessionInfo.UserID);
				if (String.Compare(Schema.SecurityUtility.DecryptPassword(LUser.Password), ASessionInfo.Password, true) != 0)
					throw new ServerException(ServerException.Codes.InvalidPassword);

				return LUser;
			}
		}

		protected override void InternalInitializeCatalog()
		{
			// Wire up the class loader events
			Catalog.ClassLoader.OnMiss += new ClassLoaderMissedEvent(ClassLoaderMiss);
			
			if (FFirstRun)
				base.InternalInitializeCatalog();
			else
			{
			    // resolve the core system objects
			    ResolveCoreSystemObjects();
				
			    // resolve the system data types
			    ResolveSystemDataTypes();
			}
		}

		private void ClassLoaderMiss(ClassLoader AClassLoader, CatalogDeviceSession ASession, ClassDefinition AClassDefinition)
		{
			// Ensure that the library containing the class is loaded.
			((ServerCatalogDeviceSession)ASession).LoadLibraryForClass(AClassDefinition);
		}
		
		private void ResolveCoreSystemObjects()
		{
			// Use the persistent catalog store to resolve references to the core catalog objects
			FSystemProcess.CatalogDeviceSession.ResolveUser(CSystemUserID);
			FSystemProcess.CatalogDeviceSession.CacheCatalogObject(FCatalogDevice);
			FAdminUser = FSystemProcess.CatalogDeviceSession.ResolveUser(CAdminUserID);
			FUserRole = (Schema.Role)FSystemProcess.CatalogDeviceSession.ResolveName(CUserRoleName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			FTempDevice = (MemoryDevice)FSystemProcess.CatalogDeviceSession.ResolveName(CTempDeviceName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			FATDevice = (ApplicationTransactionDevice)FSystemProcess.CatalogDeviceSession.ResolveName(CATDeviceName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
		}

		private void ResolveSystemDataTypes()
		{
			// Note that these (and the native type references set in BindNativeTypes) are also set in the CatalogDevice (FixupSystemTypeReferences)
			// The functionality is duplicated to ensure that the references will be set on a delayed load of a system type, as well as to ensure that a repository functions correctly without delayed resolution
			Catalog.DataTypes.SystemScalar = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemScalar, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemBoolean = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemBoolean, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemDecimal = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemDecimal, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemLong = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemLong, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemInteger = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemInteger, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemShort = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemShort, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemByte = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemByte, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemString = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemString, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemTimeSpan = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemTimeSpan, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemDateTime = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemDateTime, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemDate = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemDate, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemTime = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemTime, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemMoney = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemMoney, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemGuid = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemGuid, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemBinary = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemBinary, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemGraphic = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemGraphic, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemError = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemError, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
			Catalog.DataTypes.SystemName = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new List<string>());
		}

		internal ServerFileInfos GetFileNames(Schema.Library ALibrary)
		{
			ServerFileInfos LFileInfos = new ServerFileInfos();
			Schema.Libraries LLibraries = new Schema.Libraries();
			LLibraries.Add(ALibrary);
			
			while (LLibraries.Count > 0)
			{
				Schema.Library LLibrary = LLibraries[0];
				LLibraries.RemoveAt(0);
				
				foreach (Schema.FileReference LReference in ALibrary.Files)
				{
					if (!LFileInfos.Contains(LReference.FileName))
					{
						string LFullFileName = GetFullFileName(ALibrary, LReference.FileName);
						LFileInfos.Add
						(
							new ServerFileInfo 
							{ 
								LibraryName = ALibrary.Name, 
								FileName = LReference.FileName, 
								FileDate = File.GetLastWriteTimeUtc(LFullFileName), 
								IsDotNetAssembly = FileUtility.IsAssembly(LFullFileName), 
								ShouldRegister = LReference.IsAssembly 
							}
						);
					} 
				}
				
				foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
					if (!LLibraries.Contains(LReference.Name))
						LLibraries.Add(Catalog.Libraries[LReference.Name]);
			}
			
			return LFileInfos;
		}

		public string GetFullFileName(Schema.Library ALibrary, string AFileName)
		{
			#if LOADFROMLIBRARIES
			return 
				Path.IsPathRooted(AFileName) 
					? AFileName 
					: 
						ALibrary.Name == Engine.CSystemLibraryName
							? PathUtility.GetFullFileName(AFileName)
							: Path.Combine(ALibrary.GetLibraryDirectory(LibraryDirectory), AFileName);
			#else
			return PathUtility.GetFullFileName(AFileName);
			#endif
		}
	}
}
