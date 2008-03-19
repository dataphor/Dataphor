/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client.Design;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	/// <summary> Dataphor DAE Connection class. </summary>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Provider.DAEConnection),"Icons.DAEConnection.bmp")]
	[DesignerSerializer(typeof(Alphora.Dataphor.DAE.Client.Design.ActiveLastSerializer), typeof(CodeDomSerializer))]
	public class DAEConnection : DbConnection, ICloneable
	{
		public DAEConnection()
		{
			InternalInitialize();
		}

		public DAEConnection(IContainer AContainer)
		{
			InternalInitialize();
			if (AContainer != null)
				AContainer.Add(this);
		}

		public DAEConnection(string AConnectionString)
		{
			InternalInitialize();
			FConnectionString = AConnectionString;
		}

		private void InternalInitialize()
		{
			FSessionInfo = new SessionInfo();
			FConnectionString = String.Empty;
		}

		protected override void Dispose(bool ADisposing)
		{
			Close();
			SessionInfo = null;
			base.Dispose(ADisposing);
		}

		[Category("Connection")]
		[DefaultValue(false)]
		public bool Active
		{
			get { return (FSession != null) && (FServerProcess != null); }
			set 
			{
				if (value)
					Open();
				else
					Close();
			}
		}

		private IServerSession FSession;
		internal IServerSession Session { get { return FSession; } }

		private IServer FServer;
		internal IServer Server { get { return FServer; } }

		private IServerProcess FServerProcess;
		internal IServerProcess ServerProcess { get { return FServerProcess; } }

		private SessionInfo FSessionInfo;
		/// <summary> The <see cref="SessionInfo"/> structure to use when connecting to the server. </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
		[Description("Contextual information for server session initialization")]
		[Category("Session")]
		public SessionInfo SessionInfo
		{
			get { return FSessionInfo; }
			set { FSessionInfo = value; }
		}

		private void CheckNotConnected()
		{
			if ((FSession != null) && (FServerProcess != null))
				throw new ProviderException(ProviderException.Codes.ConnectionConnected);
		}

		private int FConnectionTimeout = 30;
		public override int ConnectionTimeout 
		{
			get { return FConnectionTimeout; }
		}
		public void SetConnectionTimeout(int AValue)
		{
			FConnectionTimeout = AValue;
		}

		private string FConnectionString;
		[Description("Uri to the Dataphor server")]
		[Category("Session")]
		public override string ConnectionString
		{
			get { return FConnectionString; }
			set
			{
				if (FConnectionString != value)
				{
					CheckNotConnected();
					FConnectionString = value;
				}
			}
		}

		public override string Database
		{
			get 
			{
				if (FSession != null)
					return FSession.SessionInfo.DefaultLibraryName;
				else
					return String.Empty;
			}
		}

		private string FDataSource = String.Empty;
		public override string DataSource { get { return FDataSource; } }

		private string FServerVersion = String.Empty;
		public override string ServerVersion { get { return FServerVersion; } }

		public override ConnectionState State
		{
			get
			{
				if ((FSession != null) && (FServerProcess != null))
					return ConnectionState.Open;
				else
					return ConnectionState.Closed;
			}
		}

		private void CheckCanStartTransaction()
		{
			if ((FSession == null) || (FServerProcess == null))
				throw new ProviderException(ProviderException.Codes.BeginTransactionFailed);
		}

		[Browsable(false)]
		public bool InTransaction
		{
			get { return (FServerProcess != null) && FServerProcess.InTransaction; }
		}

		/// <summary> Depth of transactions. </summary>
		[Browsable(false)]
		public int TransactionCount
		{
			get { return FServerProcess != null ? FServerProcess.TransactionCount : 0; }
		}
		
		private System.Data.IsolationLevel FDefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
		public System.Data.IsolationLevel DefaultIsolationLevel
		{
			get { return FDefaultIsolationLevel; }
			set { FDefaultIsolationLevel = value; }
		}

		protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel ALevel)
		{
			CheckCanStartTransaction();
			return new DAETransaction(this, ALevel);
		}

		public override void ChangeDatabase(string ADatabaseName)
		{
			// Unimplemented - use the SessionInfo's namespace information
		}

		public event EventHandler BeforeClose;
		public event EventHandler AfterClose;

		protected virtual void OnBeforeClose()
		{
			if (BeforeClose != null)
				BeforeClose(this, EventArgs.Empty);
		}

		protected virtual void OnAfterClose()
		{
			if (AfterClose != null)
				AfterClose(this, EventArgs.Empty);
		}

		public override void Close()
		{
			if(FForeignServer) 
			{
				try
				{
					OnBeforeClose();
				}
				finally
				{
					FSession = null;
					FServer = null;
					FServerProcess = null;
					FForeignServer = false;
				}
				OnAfterClose();
				return;
			}

			if (FSession != null)
			{
				try
				{
					OnBeforeClose();
				}
				finally
				{
					try
					{
						if (FServerProcess != null)
							FSession.StopProcess(FServerProcess);
						FServer.Disconnect(FSession);
					}
					finally
					{
						FSession = null;
						try
						{
							DAE.Server.ServerFactory.Disconnect(FServer);
						}
						finally
						{
							FServer = null;
						}
					}
				}
				OnAfterClose();
			}
		}

		protected override DbCommand CreateDbCommand()
		{
			return new DAECommand();
		}

		public event EventHandler BeforeOpen;
		public event EventHandler AfterOpen;

		protected virtual void OnBeforeOpen()
		{
			if (BeforeOpen != null)
				BeforeOpen(this, EventArgs.Empty);
		}

		protected virtual void OnAfterOpen()
		{
			if (AfterOpen != null)
				AfterOpen(this, EventArgs.Empty);
		}

		public override void Open()
		{
			if (FSession == null)
			{
				OnBeforeOpen();
				FServer = DAE.Server.ServerFactory.Connect(FConnectionString, TerminalServiceUtility.ClientName);
				try
				{
					FSession = FServer.Connect(FSessionInfo);
					FServerProcess = FSession.StartProcess(new ProcessInfo(FSessionInfo));
				}
				catch
				{
					DAE.Server.ServerFactory.Disconnect(FServer);
					FServer = null;
					throw;
				}
				OnAfterOpen();
			}
		}

		private bool FForeignServer = false;
		public void Open(IServer AServer, IServerSession ASession, IServerProcess AProcess)
		{
			OnBeforeOpen();
			FForeignServer = true;
			FServer = AServer;
			FSession = ASession;
			FServerProcess = AProcess;
			OnAfterOpen();
		}

		public virtual object Clone()
		{
			DAEConnection LNewConnection = new DAEConnection();
			LNewConnection.ConnectionString = FConnectionString;
			LNewConnection.SetConnectionTimeout(FConnectionTimeout);
			LNewConnection.SessionInfo = (Alphora.Dataphor.DAE.SessionInfo)FSessionInfo.Clone();
			LNewConnection.Active = Active;
			return LNewConnection;
		}
	}
}
