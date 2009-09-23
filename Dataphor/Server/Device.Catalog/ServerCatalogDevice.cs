/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;

	public class ServerCatalogDevice : CatalogDevice
	{
		public ServerCatalogDevice(int AID, string AName) : base(AID, AName) { }
		
		private CatalogStore FStore;
		internal CatalogStore Store
		{
			get
			{
				Error.AssertFail(FStore != null, "Server is configured as a repository and has no catalog store.");
				return FStore;
			}
		}

		public int MaxStoreConnections
		{
			get { return FStore.MaxConnections; }
			set { FStore.MaxConnections = value; }
		}

		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new ServerCatalogDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

		protected override void InternalStart(ServerProcess AProcess)
		{
			base.InternalStart(AProcess);
			if (!AProcess.ServerSession.Server.IsEngine)
			{
				FStore = new CatalogStore();
				FStore.StoreClassName = AProcess.ServerSession.Server.GetCatalogStoreClassName();
				FStore.StoreConnectionString = AProcess.ServerSession.Server.GetCatalogStoreConnectionString();
				FStore.Initialize(AProcess.ServerSession.Server);
			}
		}

		private void PopulateServerSettings(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			DAE.Server.Server LServer = (DAE.Server.Server)AProgram.ServerProcess.ServerSession.Server;
			ARow[0] = LServer.Name;
			ARow[1] = GetType().Assembly.GetName().Version.ToString();
			ARow[2] = LServer.LogErrors;
			ARow[3] = LServer.Catalog.TimeStamp;
			ARow[4] = LServer.CacheTimeStamp;
			ARow[5] = LServer.PlanCacheTimeStamp;
			ARow[6] = LServer.DerivationTimeStamp;
			ARow[7] = LServer.InstanceDirectory;
			ARow[8] = LServer.LibraryDirectory;
			ARow[9] = LServer.IsEngine;
			ARow[10] = LServer.MaxConcurrentProcesses;
			ARow[11] = LServer.ProcessWaitTimeout;
			ARow[12] = LServer.ProcessTerminationTimeout;
			ARow[13] = LServer.PlanCache.Size;
			ANativeTable.Insert(AProgram.ValueManager, ARow);
		}

		private void PopulateLoadedLibraries(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			List<string> LLibraryNames = ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SelectLoadedLibraries();
			for (int LIndex = 0; LIndex < LLibraryNames.Count; LIndex++)
			{
				ARow[0] = LLibraryNames[LIndex];
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateLibraryOwners(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SelectLibraryOwners(AProgram, ANativeTable, ARow);
		}

		private void PopulateLibraryVersions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SelectLibraryVersions(AProgram, ANativeTable, ARow);
		}

		protected virtual void InternalPopulateTableVar(Program AProgram, CatalogHeader AHeader, Row ARow)
		{
			switch (AHeader.TableVar.Name)
			{
				case "System.ServerSettings" : PopulateServerSettings(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibraryOwners" : PopulateLibraryOwners(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibraryVersions" : PopulateLibraryVersions(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LoadedLibraries" : PopulateLoadedLibraries(AProgram, AHeader.NativeTable, ARow); break;
				default: base.InternalPopulateTableVar(AProgram, AHeader, ARow);
			}
		}
	}
}
