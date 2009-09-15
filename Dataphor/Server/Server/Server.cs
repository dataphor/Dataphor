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

namespace DAE.Server
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
	using Alphora.Dataphor.Logging;
	using Schema = Alphora.Dataphor.DAE.Schema;

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

		protected override void InternalStart()
		{
			if (LibraryDirectory == String.Empty)
				LibraryDirectory = GetDefaultLibraryDirectory();
			base.InternalStart();
		}

		public static string GetDefaultLibraryDirectory()
		{
			return Path.Combine(PathUtility.GetBinDirectory(), CDefaultLibraryDirectory);
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
				if (FState != ServerState.Starting && FState != ServerState.Stopped)
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

					if (!Directory.Exists(LDirectories[LIndex]) && !IsRepository)
						Directory.CreateDirectory(LDirectories[LIndex]);

					if (LIndex > 0)
						LLibraryDirectory.Append(";");

					LLibraryDirectory.Append(LDirectories[LIndex]);
				}

				FLibraryDirectory = LLibraryDirectory.ToString();
			}
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
					FCatalog.Libraries.Add(LGeneralLibrary);
					string LLibraryDirectory = Path.Combine(Schema.Library.GetDefaultLibraryDirectory(LibraryDirectory), LGeneralLibrary.Name);
					if (!Directory.Exists(LLibraryDirectory))
						Directory.CreateDirectory(LLibraryDirectory);
					LGeneralLibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LGeneralLibrary.Name)));
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
			// Load server configuration settings
			FSystemProcess.CatalogDeviceSession.LoadServerSettings(this);

			// Attaches any libraries that were attached from an explicit library directory
			FSystemProcess.CatalogDeviceSession.AttachLibraries();

			// Set the object ID generator				
			Schema.Object.SetNextObjectID(FSystemProcess.CatalogDeviceSession.GetMaxObjectID() + 1);

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

		protected virtual void StartLog()
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

		public virtual List<string> ListLogs()
		{
			return new List<string>();
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
	}
}
