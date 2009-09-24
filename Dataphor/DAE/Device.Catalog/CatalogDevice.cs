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
		public CatalogDevice(int AID, string AName) : base(AID, AName){}
		
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new CatalogDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		private CatalogHeaders FHeaders = new CatalogHeaders();
		public CatalogHeaders Headers { get { return FHeaders; } }
		
		// Users cache, maintained exclusively through the catalog maintenance API on the CatalogDeviceSession		
		private Users FUsersCache = new Users();
		protected internal Users UsersCache { get { return FUsersCache; } }
		
		// Catalog index, maintained exclusively through the catalog maintenance API on the CatalogDeviceSession
		// The catalog index stores the name of each object in the catalog cache by it's object ID
		// Note that the names are stored rooted in this index.
		private Dictionary<int, string> FCatalogIndex = new Dictionary<int, string>();
		protected internal Dictionary<int, string> CatalogIndex { get { return FCatalogIndex; } }
		
		private NameResolutionCache FNameCache = new NameResolutionCache(CDefaultNameResolutionCacheSize);
		protected internal NameResolutionCache NameCache { get { return FNameCache; } }
		
		private NameResolutionCache FOperatorNameCache = new NameResolutionCache(CDefaultNameResolutionCacheSize);
		protected internal NameResolutionCache OperatorNameCache { get { return FOperatorNameCache; } }
		
		public const int CDefaultNameResolutionCacheSize = 1000;

		public int NameResolutionCacheSize
		{
			get { return FNameCache.Size; }
			set { FNameCache.Resize(value); }
		}
		
		public int OperatorNameResolutionCacheSize
		{
			get { return FOperatorNameCache.Size; }
			set { FOperatorNameCache.Resize(value); }
		}
		
		protected override DevicePlan CreateDevicePlan(Plan APlan, PlanNode APlanNode)
		{
			return new CatalogDevicePlan(APlan, this, APlanNode);
		}
		
		protected override DevicePlanNode InternalPrepare(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			CatalogDevicePlan LDevicePlan = (CatalogDevicePlan)ADevicePlan;
			LDevicePlan.IsSupported = true;
			return base.InternalPrepare(ADevicePlan, APlanNode);
		}
		
		private void PopulateConnections(Program AProgram, NativeTable ANativeTable, Row ARow)
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
		
		private void PopulateSessions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
			{
				ARow[0] = LSession.SessionID;
				ARow[1] = LSession.User.ID;
				ARow[2] = LSession.SessionInfo.HostName;
				ARow[3] = LSession.SessionInfo.CatalogCacheName;
				ARow[4] = LSession.CurrentLibrary.Name;
				ARow[5] = LSession.SessionInfo.DefaultIsolationLevel.ToString();
				ARow[6] = LSession.SessionInfo.DefaultUseDTC;
				ARow[7] = LSession.SessionInfo.DefaultUseImplicitTransactions;
				ARow[8] = LSession.SessionInfo.Language.ToString();
				ARow[9] = LSession.SessionInfo.FetchCount;
				ARow[10] = LSession.SessionInfo.DefaultMaxStackDepth;
				ARow[11] = LSession.SessionInfo.DefaultMaxCallDepth;
				ARow[12] = LSession.SessionInfo.UsePlanCache;
				ARow[13] = LSession.SessionInfo.ShouldEmitIL;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateProcesses(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
				{
					ARow[0] = LProcess.ProcessID;
					ARow[1] = LSession.SessionID;
					ARow[2] = LProcess.DefaultIsolationLevel.ToString();
					ARow[3] = LProcess.UseDTC;
					ARow[4] = LProcess.UseImplicitTransactions;
					ARow[5] = LProcess.MaxStackDepth;
					ARow[6] = LProcess.MaxCallDepth;
					ARow[7] = LProcess.ExecutingThread != null;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}
		
		private void PopulateLocks(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			LockManager LLockManager = AProgram.ServerProcess.ServerSession.Server.LockManager;
			KeyValuePair<LockID, LockHeader>[] LEntries;
			lock (LLockManager)
			{
				LEntries = new KeyValuePair<LockID, LockHeader>[LLockManager.Locks.Count];
				var LIndex = 0;
				foreach (KeyValuePair<LockID, LockHeader> LEntry in LLockManager.Locks)
				{
					LEntries[LIndex] = LEntry;
					LIndex++;
				}
			}

			foreach (KeyValuePair<LockID, LockHeader> LEntry in LEntries)
			{
				LockHeader LLockHeader = LEntry.Value;
				ARow[0] = LLockHeader.LockID.Owner.ToString();
				ARow[1] = LLockHeader.LockID.LockName;
				ARow[2] = LLockHeader.Semaphore.Mode.ToString();
				ARow[3] = LLockHeader.Semaphore.GrantCount();
				ARow[4] = LLockHeader.Semaphore.WaitCount();
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateScripts(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (ServerScript LScript in LProcess.Scripts)
					{
						ARow[0] = LScript.GetHashCode();
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}

		private void PopulatePlans(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (ServerPlan LPlan in LProcess.Plans)
					{
						ARow[0] = LPlan.ID;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}

		private void PopulateLibraries(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
			{
				ARow[0] = LLibrary.Name;
				ARow[1] = LLibrary.Directory;
				ARow[2] = LLibrary.Version;
				ARow[3] = LLibrary.DefaultDeviceName;
				ARow[4] = true; //AProgram.ServerProcess.ServerSession.Server.CanLoadLibrary(LLibrary.Name);
				ARow[5] = LLibrary.IsSuspect;
				ARow[6] = LLibrary.SuspectReason;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}											
		
		private void PopulateLibraryFiles(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
				foreach (Schema.FileReference LReference in LLibrary.Files)
				{
					ARow[0] = LLibrary.Name;
					ARow[1] = LReference.FileName;
					ARow[2] = LReference.IsAssembly;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}											
		
		private void PopulateLibraryRequisites(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
				foreach (LibraryReference LReference in LLibrary.Libraries)
				{
					ARow[0] = LLibrary.Name;
					ARow[1] = LReference.Name;
					ARow[2] = LReference.Version; 
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}											
		
		private void PopulateLibrarySettings(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
				if (LLibrary.MetaData != null)
				{
					#if USEHASHTABLEFORTAGS
					foreach (Tag LTag in LLibrary.MetaData.Tags)
					{
					#else
					Tag LTag;
					for (int LIndex = 0; LIndex < LLibrary.MetaData.Tags.Count; LIndex++)
					{
						LTag = LLibrary.MetaData.Tags[LIndex];
					#endif
						ARow[0] = LLibrary.Name;
						ARow[1] = LTag.Name;
						ARow[2] = LTag.Value;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
		}
		
		private void PopulateRegisteredAssemblies(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (RegisteredAssembly LAssembly in AProgram.Catalog.ClassLoader.Assemblies)
			{
				ARow[0] = LAssembly.Name.ToString();
				ARow[1] = LAssembly.Library.Name;
				ARow[2] = LAssembly.Assembly.Location;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}											
		
		private void PopulateRegisteredClasses(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (RegisteredClass LClass in AProgram.Catalog.ClassLoader.Classes)
			{
				ARow[0] = LClass.Name;
				ARow[1] = LClass.Library.Name;
				ARow[2] = LClass.Assembly.Name.ToString();
				ARow[3] = LClass.ClassName;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}											
		
		private void PopulateSessionCatalogObjects(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (Schema.SessionObject LObject in LSession.SessionObjects)
				{
					ARow[0] = LSession.SessionID;
					ARow[1] = LObject.Name;
					ARow[2] = LObject.GlobalName;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}

		#if USETYPEINHERITANCE
		private void PopulateScalarTypeParentScalarTypes(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
				if (LObject is Schema.ScalarType)
					foreach (Schema.ScalarType LParentScalarType in ((Schema.ScalarType)LObject).ParentTypes)
					{
						ARow[0] = LObject.Name;
						ARow[1] = LParentScalarType.Name;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
			
		}
		#endif
		
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
		
		private void PopulateServerLinks(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
			{
				Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
				if (LServerLink != null)
				{
					ARow[0] = LServerLink.ID;
					ARow[1] = LServerLink.Name;
					ARow[2] = LServerLink.Library.Name;
					ARow[3] = LServerLink.Owner.ID;
					ARow[4] = LServerLink.IsSystem;
					ARow[5] = LServerLink.IsGenerated;
					ARow[6] = LServerLink.HostName;
					ARow[7] = LServerLink.InstanceName;
					ARow[8] = LServerLink.OverridePortNumber;
					ARow[9] = LServerLink.UseSessionInfo;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
			}
		}
		
		private void PopulateServerLinkUsers(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
			{
				Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
				if (LServerLink != null)
				{
					foreach (ServerLinkUser LLinkUser in LServerLink.Users.Values)
					{
						ARow[0] = LLinkUser.UserID;
						ARow[1] = LServerLink.Name;
						ARow[2] = LLinkUser.ServerLinkUserID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
			}
		}
		
		private void PopulateRemoteSessions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (RemoteSession LRemoteSession in LProcess.RemoteSessions)
					{
						ARow[0] = LRemoteSession.ServerLink.Name;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}
		
		private void PopulateDeviceSessions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (DeviceSession LDeviceSession in LProcess.DeviceSessions)
					{
						ARow[0] = LDeviceSession.Device.Name;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}
		
		private void PopulateApplicationTransactions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			lock (AProgram.ServerProcess.ServerSession.Server.ATDevice.ApplicationTransactions.SyncRoot)
			{
				foreach (ApplicationTransaction.ApplicationTransaction LTransaction in AProgram.ServerProcess.ServerSession.Server.ATDevice.ApplicationTransactions.Values)
				{
					ARow[0] = LTransaction.ID;
					ARow[1] = LTransaction.Session.SessionID;
					ARow[2] = LTransaction.Device.Name;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
			}
		}
		
		protected virtual void InternalPopulateTableVar(Program AProgram, CatalogHeader AHeader, Row ARow)
		{
			switch (AHeader.TableVar.Name)
			{
				case "System.Connections" : PopulateConnections(AProgram, AHeader.NativeTable, ARow); break;
				case "System.Sessions" : PopulateSessions(AProgram, AHeader.NativeTable, ARow); break;
				case "System.Processes" : PopulateProcesses(AProgram, AHeader.NativeTable, ARow); break;
				case "System.Locks" : PopulateLocks(AProgram, AHeader.NativeTable, ARow); break;
				case "System.Scripts" : PopulateScripts(AProgram, AHeader.NativeTable, ARow); break;
				case "System.Plans" : PopulatePlans(AProgram, AHeader.NativeTable, ARow); break;
				case "System.Libraries" : PopulateLibraries(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibraryFiles" : PopulateLibraryFiles(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibraryRequisites" : PopulateLibraryRequisites(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibrarySettings" : PopulateLibrarySettings(AProgram, AHeader.NativeTable, ARow); break;
				case "System.RegisteredAssemblies" : PopulateRegisteredAssemblies(AProgram, AHeader.NativeTable, ARow); break;
				case "System.RegisteredClasses" : PopulateRegisteredClasses(AProgram, AHeader.NativeTable, ARow); break;
				case "System.SessionCatalogObjects" : PopulateSessionCatalogObjects(AProgram, AHeader.NativeTable, ARow); break;
				#if USETYPEINHERITANCE
				case "System.ScalarTypeParentScalarTypes" : PopulateScalarTypeParentScalarTypes(AProgram, AHeader.NativeTable, ARow); break;
				case "System.ScalarTypeExplicitCastFunctions" : PopulateScalarTypeExplicitCastFunctions(AProgram, AHeader.NativeTable, ARow); break;
				#endif
				case "System.DeviceSessions" : PopulateDeviceSessions(AProgram, AHeader.NativeTable, ARow); break;
				case "System.ApplicationTransactions" : PopulateApplicationTransactions(AProgram, AHeader.NativeTable, ARow); break;
				case "System.ServerLinks": PopulateServerLinks(AProgram, AHeader.NativeTable, ARow); break;
				case "System.ServerLinkUsers": PopulateServerLinkUsers(AProgram, AHeader.NativeTable, ARow); break;
				case "System.RemoteSessions": PopulateRemoteSessions(AProgram, AHeader.NativeTable, ARow); break;
			}
		}
		
		protected internal void PopulateTableVar(Program AProgram, CatalogHeader AHeader)
		{
			AHeader.NativeTable.Truncate(AProgram.ValueManager);
			Row LRow = new Row(AProgram.ValueManager, AHeader.TableVar.DataType.RowType);
			try
			{
				InternalPopulateTableVar(AProgram, AHeader, LRow);
			}
			finally
			{
				LRow.Dispose();
			}

			AHeader.TimeStamp = AProgram.Catalog.TimeStamp;
		}
	}
}

