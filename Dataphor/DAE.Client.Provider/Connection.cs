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
		public DAEConnection() { }

		public DAEConnection(IContainer AContainer)
		{
			if (AContainer != null)
				AContainer.Add(this);
		}

		public DAEConnection(ServerAlias AAlias)
		{
			Alias = AAlias;
		}
		
		public DAEConnection(string AAliasName)
		{
			ConnectionString = AAliasName;
		}

		protected override void Dispose(bool ADisposing)
		{
			Close();
			base.Dispose(ADisposing);
		}

		[Category("Connection")]
		[DefaultValue(false)]
		public bool Active
		{
			get { return FServer != null; }
			set 
			{
				if (value)
					Open();
				else
					Close();
			}
		}

		private ServerConnection FConnection;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public ServerConnection Connection { get { return FConnection; } }

		private IServer FServer;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServer Server { get { return FServer; } }

		private IServerSession FSession;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServerSession Session { get { return FSession; } }

		private IServerProcess FServerProcess;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServerProcess ServerProcess { get { return FServerProcess; } }

		private void CheckNotConnected()
		{
			if (FServer != null)
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

		private string FConnectionString = String.Empty;
		/// <summary> The name of the alias to use to establish a connection to a Dataphor Server. </summary>
		/// <remarks>
		/// If an alias name is provided, an AliasManager will be used to retrieve the alias settings.
		/// Setting this property will indirectly set the Alias property.
		/// </remarks>
		[DefaultValue("")]
		[Category("Session")]
		[Description("The name of the alias to use to establish a connection to a Dataphor Server.")]
		public override string ConnectionString
		{
			get { return FConnectionString; }
			set
			{
				CheckNotConnected();
				if (FConnectionString != value)
				{
					FAlias = AliasManager.GetAlias(value);
					FConnectionString = value;
				}
			}
		}

		private ServerAlias FAlias;
		/// <summary>The alias used to establish a connection to a Dataphor Server.</summary>
		/// <remarks>
		/// If an AliasName is provided, it will be used to lookup the alias, and the value of this 
		/// property will be the alias retrieved from the alias manager by name. Setting this property 
		/// will clear the value of the ConnectionString property.
		/// </remarks>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ServerAlias Alias
		{
			get { return FAlias; }
			set
			{
				CheckNotConnected();
				if (FAlias != value)
				{
					FAlias = value;
					FConnectionString = String.Empty;
				}
			}
		}

		/// <summary> The database property returns the default library name. </summary>
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

		public override void ChangeDatabase(string ADatabaseName)
		{
			// Unimplemented - use the SessionInfo's namespace information
		}

		private string FDataSource = String.Empty;
		public override string DataSource { get { return FDataSource; } }

		private string FServerVersion = String.Empty;
		public override string ServerVersion { get { return FServerVersion; } }

		public override ConnectionState State
		{
			get
			{
				if (FServer != null)
					return ConnectionState.Open;
				else
					return ConnectionState.Closed;
			}
		}

		private void CheckCanStartTransaction()
		{
			if (FServer == null)
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
		
		// TODO: Map isolation levels
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

		protected override DbCommand CreateDbCommand()
		{
			return new DAECommand();
		}

		public event EventHandler BeforeOpen;

		protected virtual void OnBeforeOpen()
		{
			if (BeforeOpen != null)
				BeforeOpen(this, EventArgs.Empty);
		}

		public event EventHandler AfterOpen;

		protected virtual void OnAfterOpen()
		{
			if (AfterOpen != null)
				AfterOpen(this, EventArgs.Empty);
		}

		/// <summary> True when the server, session, and process are given as part of opening. </summary>
		private bool FForeignServer;

		public override void Open()
		{
			if (FServer == null)
			{
				FForeignServer = false;
				OnBeforeOpen();
				if (FAlias == null)
					throw new ProviderException(ProviderException.Codes.NoAliasSpecified);
				FConnection = new ServerConnection(FAlias, true);
				FServer = FConnection.Server;
				try
				{
					FAlias.SessionInfo.Environment = "ADO.NET";
					FSession = FServer.Connect(FAlias.SessionInfo);
					FServerProcess = FSession.StartProcess(new ProcessInfo(FAlias.SessionInfo));
				}
				catch
				{
					FServer = null;
					FSession = null;
					FServerProcess = null;
					FConnection.Dispose();
					FConnection = null;
					throw;
				}
				OnAfterOpen();
			}
		}

		public void Open(IServer AServer, IServerSession ASession, IServerProcess AProcess)
		{
			if (FServer == null)
			{
				FForeignServer = true;
				OnBeforeOpen();
				FServer = AServer;
				FSession = ASession;
				FServerProcess = AProcess;
				OnAfterOpen();
			}
		}

		public event EventHandler BeforeClose;

		protected virtual void OnBeforeClose()
		{
			if (BeforeClose != null)
				BeforeClose(this, EventArgs.Empty);
		}

		public event EventHandler AfterClose;

		protected virtual void OnAfterClose()
		{
			if (AfterClose != null)
				AfterClose(this, EventArgs.Empty);
		}

		public override void Close()
		{
			if (FServer != null)
			{
				if (FForeignServer)
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
					}
					OnAfterClose();
				}
				else
				{
					try					
					{
						try
						{
							try
							{
								try
								{
									try
									{
										OnBeforeClose();
									}
									finally
									{
										if (FServerProcess != null)
											FSession.StopProcess(FServerProcess);
									}
								}
								finally
								{
									FServerProcess = null;
									if (FSession != null)
										FServer.Disconnect(FSession);
								}
							}
							finally
							{
								FSession = null;
								//DAE.Server.ServerFactory.Disconnect(FServer);
							}
						}
						finally
						{
							FServer = null;
							if (FConnection != null)
								FConnection.Dispose();
						}
					}
					finally
					{
						FConnection = null;
					}
					OnAfterClose();
				}
			}
		}

		public virtual object Clone()
		{
			DAEConnection LNewConnection = new DAEConnection();
			LNewConnection.ConnectionString = FConnectionString;
			LNewConnection.SetConnectionTimeout(FConnectionTimeout);
			LNewConnection.Alias = FAlias;
			LNewConnection.Active = Active;
			return LNewConnection;
		}
	}
}
