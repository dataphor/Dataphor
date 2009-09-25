/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client
{
	using System;
	using System.ComponentModel;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Schema = Alphora.Dataphor.DAE.Schema;

	[System.Drawing.ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DACDataSession),"Icons.DataSession.bmp")]
	public abstract class DACDataSession : DataSessionBase
	{
		protected const string CDefaultDeviceName = "DataSession";

		public DACDataSession() : base()
		{
			LicenseUtility.Validate(this.GetType(), this);
		}
		
		private Engine FServer;
		protected Engine Server 
		{ 
			get 
			{ 
				CheckActive();
				return FServer; 
			} 
		}
		
		private IServerProcess FServerProcess;
		
		private Schema.Device FDevice;
		protected Schema.Device Device 
		{ 
			get 
			{ 
				CheckActive();
				return FDevice; 
			} 
		}
		
		/// <remarks>This method must create a device named DataSession, or the driver will fail.</remarks>
		protected abstract Schema.Device InternalCreateDevice();
		
		protected override void InternalOpen()
		{
			FServer = new Server();
			FServer.Start();
			SessionInfo LSessionInfo = (SessionInfo)SessionInfo.Clone();
			LSessionInfo.UserID = Engine.CAdminUserID;
			LSessionInfo.Password = String.Empty;
			//LSessionInfo.DefaultDeviceName = CDefaultDeviceName;
			FServerSession = ((IServer)FServer).Connect(LSessionInfo);
			FServerProcess = FServerSession.StartProcess(new ProcessInfo(LSessionInfo));
			FDevice = InternalCreateDevice();
			FDevice.ReconcileMode = DAE.Language.D4.ReconcileMode.Automatic;
			FDevice.ReconcileMaster = DAE.Language.D4.ReconcileMaster.Device;
			FDevice.Users.Add(new Schema.DeviceUser(FServer.AdminUser, FDevice, SessionInfo.UserID, SessionInfo.Password, true));
			FServer.Catalog.Add(FDevice);
			FDevice.Start(FServerProcess.GetServerProcess());
			FDevice.Register(FServerProcess.GetServerProcess());
		}
		
		protected override void InternalClose()
		{
			if (FServerProcess != null)
			{
				FServerSession.StopProcess(FServerProcess);
				FServerProcess = null;
			}

			if (FServerSession != null)
			{
				((IServer)FServer).Disconnect(FServerSession);
				FServerSession = null;
			}
			
			if (FServer != null)
			{
				FServer.Stop();
				FServer.Dispose();
				FServer = null;
			}
		}

		// SchemaReconciliationMode
		// ResetSchemaCache()
	}
}