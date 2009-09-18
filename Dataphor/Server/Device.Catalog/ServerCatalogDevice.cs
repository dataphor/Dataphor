using System;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	using Alphora.Dataphor.DAE.Server;

	public class ServerCatalogDevice : CatalogDevice
	{
		public ServerCatalogDevice(int AID, string AName) : base(AID, AName) { }
		
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new ServerCatalogDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

		protected override void PopulateServerSettings(Program AProgram, NativeTable ANativeTable, Row ARow)
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

	}
}
