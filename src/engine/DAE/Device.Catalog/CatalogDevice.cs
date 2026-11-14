/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define LOGDDLINSTRUCTIONS

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDevice : MemoryDevice
	{
		public CatalogDevice(int iD, string name) : base(iD, name){}
		
		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new CatalogDeviceSession(this, serverProcess, deviceSessionInfo);
		}
		
		private CatalogHeaders _headers = new CatalogHeaders();
		public CatalogHeaders Headers { get { return _headers; } }
		
		// Users cache, maintained exclusively through the catalog maintenance API on the CatalogDeviceSession		
		private Users _usersCache = new Users();
		protected internal Users UsersCache { get { return _usersCache; } }
		
		// Catalog index, maintained exclusively through the catalog maintenance API on the CatalogDeviceSession
		// The catalog index stores the name of each object in the catalog cache by it's object ID
		// Note that the names are stored rooted in this index.
		private Dictionary<int, string> _catalogIndex = new Dictionary<int, string>();
		protected internal Dictionary<int, string> CatalogIndex { get { return _catalogIndex; } }
		
		private NameResolutionCache _nameCache = new NameResolutionCache(DefaultNameResolutionCacheSize);
		protected internal NameResolutionCache NameCache { get { return _nameCache; } }
		
		private NameResolutionCache _operatorNameCache = new NameResolutionCache(DefaultNameResolutionCacheSize);
		protected internal NameResolutionCache OperatorNameCache { get { return _operatorNameCache; } }
		
		public const int DefaultNameResolutionCacheSize = 1000;

		public int NameResolutionCacheSize
		{
			get { return _nameCache.Size; }
			set { _nameCache.Resize(value); }
		}
		
		public int OperatorNameResolutionCacheSize
		{
			get { return _operatorNameCache.Size; }
			set { _operatorNameCache.Resize(value); }
		}
		
		protected override DevicePlan CreateDevicePlan(Plan plan, PlanNode planNode)
		{
			return new CatalogDevicePlan(plan, this, planNode);
		}
		
		protected override DevicePlanNode InternalPrepare(DevicePlan devicePlan, PlanNode planNode)
		{
			CatalogDevicePlan localDevicePlan = (CatalogDevicePlan)devicePlan;
			localDevicePlan.IsSupported = true;
			return base.InternalPrepare(devicePlan, planNode);
		}
		
		private void PopulateConnections(Program program, NativeTable nativeTable, Row row)
		{
/*
			foreach (ServerConnection LConnection in AProgram.ServerProcess.ServerSession.Server.Connections)
			{
				ARow[0] = LConnection.ConnectionName;
				ARow[1] = LConnection.HostName;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
*/
		}
		
		private void PopulateSessions(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
			{
				row[0] = session.SessionID;
				row[1] = session.User.ID;
				row[2] = session.SessionInfo.HostName;
				row[3] = session.SessionInfo.CatalogCacheName;
				row[4] = session.SessionInfo.Environment;
				row[5] = session.CurrentLibrary.Name;
				row[6] = session.SessionInfo.DefaultIsolationLevel.ToString();
				row[7] = session.SessionInfo.DefaultUseDTC;
				row[8] = session.SessionInfo.DefaultUseImplicitTransactions;
				row[9] = session.SessionInfo.Language.ToString();
				row[10] = session.SessionInfo.FetchCount;
				row[11] = session.SessionInfo.DefaultMaxStackDepth;
				row[12] = session.SessionInfo.DefaultMaxCallDepth;
				row[13] = session.SessionInfo.UsePlanCache;
				row[14] = session.SessionInfo.ShouldEmitIL;
				row[15] = session.SessionInfo.ShouldElaborate;
				nativeTable.Insert(program.ValueManager, row);
			}
		}
		
		private void PopulateProcesses(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess process in session.Processes)
				{
					row[0] = process.ProcessID;
					row[1] = session.SessionID;
					row[2] = process.DefaultIsolationLevel.ToString();
					row[3] = process.UseDTC;
					row[4] = process.UseImplicitTransactions;
					row[5] = process.ProcessInfo.Language.ToString();
					row[6] = process.MaxStackDepth;
					row[7] = process.MaxCallDepth;
					row[8] = process.ExecutingThread != null;
					nativeTable.Insert(program.ValueManager, row);
				}
		}
		
		private void PopulateScripts(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess process in session.Processes)
					foreach (ServerScript script in process.Scripts)
					{
						row[0] = script.GetHashCode();
						row[1] = process.ProcessID;
						nativeTable.Insert(program.ValueManager, row);
					}
		}

		private void PopulatePlans(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess process in session.Processes)
					foreach (ServerPlan plan in process.Plans)
					{
						row[0] = plan.ID;
						row[1] = process.ProcessID;
						nativeTable.Insert(program.ValueManager, row);
					}
		}

		private void PopulateLibraries(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Library library in program.Catalog.Libraries)
			{
				row[0] = library.Name;
				row[1] = library.Directory;
				row[2] = library.Version;
				row[3] = library.DefaultDeviceName;
				row[4] = true; //AProgram.ServerProcess.ServerSession.Server.CanLoadLibrary(LLibrary.Name);
				row[5] = library.IsSuspect;
				row[6] = library.SuspectReason;
				nativeTable.Insert(program.ValueManager, row);
			}
		}											
		
		private void PopulateLibraryFiles(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Library library in program.Catalog.Libraries)
				foreach (Schema.FileReference reference in library.Files)
				{
					row[0] = library.Name;
					row[1] = reference.FileName;
					row[2] = reference.IsAssembly;
					nativeTable.Insert(program.ValueManager, row);
				}
		}											
		
		private void PopulateLibraryFileEnvironments(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Library library in program.Catalog.Libraries)
				foreach (Schema.FileReference reference in library.Files)
					foreach (string environment in reference.Environments)
					{
						row[0] = library.Name;
						row[1] = reference.FileName;
						row[2] = environment;
						nativeTable.Insert(program.ValueManager, row);
					}
		}
		
		private void PopulateLibraryRequisites(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Library library in program.Catalog.Libraries)
				foreach (LibraryReference reference in library.Libraries)
				{
					row[0] = library.Name;
					row[1] = reference.Name;
					row[2] = reference.Version; 
					nativeTable.Insert(program.ValueManager, row);
				}
		}											
		
		private void PopulateLibrarySettings(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Library library in program.Catalog.Libraries)
				if (library.MetaData != null)
				{
					#if USEHASHTABLEFORTAGS
					foreach (Tag tag in library.MetaData.Tags)
					{
					#else
					Tag tag;
					for (int index = 0; index < library.MetaData.Tags.Count; index++)
					{
						tag = library.MetaData.Tags[index];
					#endif
						row[0] = library.Name;
						row[1] = tag.Name;
						row[2] = tag.Value;
						nativeTable.Insert(program.ValueManager, row);
					}
				}
		}
		
		private void PopulateRegisteredAssemblies(Program program, NativeTable nativeTable, Row row)
		{
			foreach (RegisteredAssembly assembly in program.Catalog.ClassLoader.Assemblies)
			{
				row[0] = assembly.Name.ToString();
				row[1] = assembly.Library.Name;
				row[2] = assembly.Assembly.Location;
				nativeTable.Insert(program.ValueManager, row);
			}
		}											
		
		private void PopulateRegisteredClasses(Program program, NativeTable nativeTable, Row row)
		{
			foreach (RegisteredClass classValue in program.Catalog.ClassLoader.Classes)
			{
				row[0] = classValue.Name;
				row[1] = classValue.Library.Name;
				row[2] = classValue.Assembly.Name.ToString();
				row[3] = classValue.ClassName;
				nativeTable.Insert(program.ValueManager, row);
			}
		}											
		
		private void PopulateSessionCatalogObjects(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				foreach (Schema.SessionObject objectValue in session.SessionObjects)
				{
					row[0] = session.SessionID;
					row[1] = objectValue.Name;
					row[2] = objectValue.GlobalName;
					nativeTable.Insert(program.ValueManager, row);
				}
		}

		private void PopulateScalarTypeParentScalarTypes(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Schema.Object localObject in program.Catalog)
				if (localObject is Schema.ScalarType)
					foreach (Schema.ScalarType parentScalarType in ((Schema.ScalarType)localObject).ParentTypes)
					{
						row[0] = localObject.Name;
						row[1] = parentScalarType.Name;
						nativeTable.Insert(program.ValueManager, row);
					}
			
		}
		
		#if USETYPEINHERITANCE		
		private void PopulateScalarTypeExplicitCastFunctions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
				if (LObject is Schema.ScalarType)
				{
					Schema.ScalarType LScalarType = (Schema.ScalarType)LObject;
					foreach (Schema.Operator LOperator in LScalarType.ExplicitCastOperators)
					{
						ARow[0] = LScalarType.Name;
						ARow[1] = LOperator.Name;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
		}
		#endif
		
		private void PopulateServerLinks(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Schema.Object objectValue in program.Catalog)
			{
				Schema.ServerLink serverLink = objectValue as Schema.ServerLink;
				if (serverLink != null)
				{
					row[0] = serverLink.ID;
					row[1] = serverLink.Name;
					row[2] = serverLink.Library.Name;
					row[3] = serverLink.Owner.ID;
					row[4] = serverLink.IsSystem;
					row[5] = serverLink.IsGenerated;
					row[6] = serverLink.HostName;
					row[7] = serverLink.InstanceName;
					row[8] = serverLink.OverridePortNumber;
					row[9] = serverLink.UseSessionInfo;
					nativeTable.Insert(program.ValueManager, row);
				}
			}
		}
		
		private void PopulateServerLinkUsers(Program program, NativeTable nativeTable, Row row)
		{
			foreach (Schema.Object objectValue in program.Catalog)
			{
				Schema.ServerLink serverLink = objectValue as Schema.ServerLink;
				if (serverLink != null)
				{
					foreach (ServerLinkUser linkUser in serverLink.Users.Values)
					{
						row[0] = linkUser.UserID;
						row[1] = serverLink.Name;
						row[2] = linkUser.ServerLinkUserID;
						nativeTable.Insert(program.ValueManager, row);
					}
				}
			}
		}
		
		private void PopulateRemoteSessions(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess process in session.Processes)
					foreach (RemoteSession remoteSession in process.RemoteSessions)
					{
						row[0] = remoteSession.ServerLink.Name;
						row[1] = process.ProcessID;
						nativeTable.Insert(program.ValueManager, row);
					}
		}
		
		private void PopulateDeviceSessions(Program program, NativeTable nativeTable, Row row)
		{
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess process in session.Processes)
					foreach (DeviceSession deviceSession in process.DeviceSessions)
					{
						row[0] = deviceSession.Device.Name;
						row[1] = process.ProcessID;
						nativeTable.Insert(program.ValueManager, row);
					}
		}
		
		private void PopulateApplicationTransactions(Program program, NativeTable nativeTable, Row row)
		{
			lock (program.ServerProcess.ServerSession.Server.ATDevice.ApplicationTransactions.SyncRoot)
			{
				foreach (ApplicationTransaction.ApplicationTransaction transaction in program.ServerProcess.ServerSession.Server.ATDevice.ApplicationTransactions.Values)
				{
					row[0] = transaction.ID;
					row[1] = transaction.Session.SessionID;
					row[2] = transaction.Device.Name;
					nativeTable.Insert(program.ValueManager, row);
				}
			}
		}
		
		protected virtual void InternalPopulateTableVar(Program program, CatalogHeader header, Row row)
		{
			switch (header.TableVar.Name)
			{
				case "System.Connections" : PopulateConnections(program, header.NativeTable, row); break;
				case "System.Sessions" : PopulateSessions(program, header.NativeTable, row); break;
				case "System.Processes" : PopulateProcesses(program, header.NativeTable, row); break;
				case "System.Scripts" : PopulateScripts(program, header.NativeTable, row); break;
				case "System.Plans" : PopulatePlans(program, header.NativeTable, row); break;
				case "System.Libraries" : PopulateLibraries(program, header.NativeTable, row); break;
				case "System.LibraryFiles" : PopulateLibraryFiles(program, header.NativeTable, row); break;
				case "System.LibraryFileEnvironments" : PopulateLibraryFileEnvironments(program, header.NativeTable, row); break;
				case "System.LibraryRequisites" : PopulateLibraryRequisites(program, header.NativeTable, row); break;
				case "System.LibrarySettings" : PopulateLibrarySettings(program, header.NativeTable, row); break;
				case "System.RegisteredAssemblies" : PopulateRegisteredAssemblies(program, header.NativeTable, row); break;
				case "System.RegisteredClasses" : PopulateRegisteredClasses(program, header.NativeTable, row); break;
				case "System.SessionCatalogObjects" : PopulateSessionCatalogObjects(program, header.NativeTable, row); break;
				case "System.ScalarTypeParentScalarTypes" : PopulateScalarTypeParentScalarTypes(program, header.NativeTable, row); break;
				#if USETYPEINHERITANCE
				case "System.ScalarTypeExplicitCastFunctions" : PopulateScalarTypeExplicitCastFunctions(AProgram, AHeader.NativeTable, ARow); break;
				#endif
				case "System.DeviceSessions" : PopulateDeviceSessions(program, header.NativeTable, row); break;
				case "System.ApplicationTransactions" : PopulateApplicationTransactions(program, header.NativeTable, row); break;
				case "System.ServerLinks": PopulateServerLinks(program, header.NativeTable, row); break;
				case "System.ServerLinkUsers": PopulateServerLinkUsers(program, header.NativeTable, row); break;
				case "System.RemoteSessions": PopulateRemoteSessions(program, header.NativeTable, row); break;
			}
		}
		
		protected internal void PopulateTableVar(Program program, CatalogHeader header)
		{
			header.NativeTable.Truncate(program.ValueManager);
			Row row = new Row(program.ValueManager, header.TableVar.DataType.RowType);
			try
			{
				InternalPopulateTableVar(program, header, row);
			}
			finally
			{
				row.Dispose();
			}

			header.TimeStamp = program.Catalog.TimeStamp;
		}
	}
}

