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

		public DAEConnection(IContainer container)
		{
			if (container != null)
				container.Add(this);
		}

		public DAEConnection(ServerAlias alias)
		{
			Alias = alias;
		}
		
		public DAEConnection(string aliasName)
		{
			ConnectionString = aliasName;
		}

		protected override void Dispose(bool disposing)
		{
			Close();
			base.Dispose(disposing);
		}

		[Category("Connection")]
		[DefaultValue(false)]
		public bool Active
		{
			get { return _server != null; }
			set 
			{
				if (value)
					Open();
				else
					Close();
			}
		}

		private ServerConnection _connection;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public ServerConnection Connection { get { return _connection; } }

		private IServer _server;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServer Server { get { return _server; } }

		private IServerSession _session;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServerSession Session { get { return _session; } }

		private IServerProcess _serverProcess;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServerProcess ServerProcess { get { return _serverProcess; } }

		private void CheckNotConnected()
		{
			if (_server != null)
				throw new ProviderException(ProviderException.Codes.ConnectionConnected);
		}

		private int _connectionTimeout = 30;
		public override int ConnectionTimeout 
		{
			get { return _connectionTimeout; }
		}
		public void SetConnectionTimeout(int value)
		{
			_connectionTimeout = value;
		}

		private string _connectionString = String.Empty;
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
			get { return _connectionString; }
			set
			{
				CheckNotConnected();
				if (_connectionString != value)
				{
					_alias = AliasManager.GetAlias(value);
					_connectionString = value;
				}
			}
		}

		private ServerAlias _alias;
		/// <summary>The alias used to establish a connection to a Dataphor Server.</summary>
		/// <remarks>
		/// If an AliasName is provided, it will be used to lookup the alias, and the value of this 
		/// property will be the alias retrieved from the alias manager by name. Setting this property 
		/// will clear the value of the ConnectionString property.
		/// </remarks>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ServerAlias Alias
		{
			get { return _alias; }
			set
			{
				CheckNotConnected();
				if (_alias != value)
				{
					_alias = value;
					_connectionString = String.Empty;
				}
			}
		}

		/// <summary> The database property returns the default library name. </summary>
		public override string Database
		{
			get 
			{
				if (_session != null)
					return _session.SessionInfo.DefaultLibraryName;
				else
					return String.Empty;
			}
		}

		public override void ChangeDatabase(string databaseName)
		{
			// Unimplemented - use the SessionInfo's namespace information
		}

		private string _dataSource = String.Empty;
		public override string DataSource { get { return _dataSource; } }

		private string _serverVersion = String.Empty;
		public override string ServerVersion { get { return _serverVersion; } }

		public override ConnectionState State
		{
			get
			{
				if (_server != null)
					return ConnectionState.Open;
				else
					return ConnectionState.Closed;
			}
		}

		private void CheckCanStartTransaction()
		{
			if (_server == null)
				throw new ProviderException(ProviderException.Codes.BeginTransactionFailed);
		}

		[Browsable(false)]
		public bool InTransaction
		{
			get { return (_serverProcess != null) && _serverProcess.InTransaction; }
		}

		/// <summary> Depth of transactions. </summary>
		[Browsable(false)]
		public int TransactionCount
		{
			get { return _serverProcess != null ? _serverProcess.TransactionCount : 0; }
		}
		
		// TODO: Map isolation levels
		private System.Data.IsolationLevel _defaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
		public System.Data.IsolationLevel DefaultIsolationLevel
		{
			get { return _defaultIsolationLevel; }
			set { _defaultIsolationLevel = value; }
		}

		protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel level)
		{
			CheckCanStartTransaction();
			return new DAETransaction(this, level);
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
		private bool _foreignServer;

		public override void Open()
		{
			if (_server == null)
			{
				_foreignServer = false;
				OnBeforeOpen();
				if (_alias == null)
					throw new ProviderException(ProviderException.Codes.NoAliasSpecified);
				_connection = new ServerConnection(_alias, true);
				_server = _connection.Server;
				try
				{
					_alias.SessionInfo.Environment = "ADO.NET";
					_session = _server.Connect(_alias.SessionInfo);
					_serverProcess = _session.StartProcess(new ProcessInfo(_alias.SessionInfo));
				}
				catch
				{
					_server = null;
					_session = null;
					_serverProcess = null;
					_connection.Dispose();
					_connection = null;
					throw;
				}
				OnAfterOpen();
			}
		}

		public void Open(IServer server, IServerSession session, IServerProcess process)
		{
			if (_server == null)
			{
				_foreignServer = true;
				OnBeforeOpen();
				_server = server;
				_session = session;
				_serverProcess = process;
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
			if (_server != null)
			{
				if (_foreignServer)
				{
					try
					{
						OnBeforeClose();
					}
					finally
					{
						_session = null;
						_server = null;
						_serverProcess = null;
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
										if (_serverProcess != null)
											_session.StopProcess(_serverProcess);
									}
								}
								finally
								{
									_serverProcess = null;
									if (_session != null)
										_server.Disconnect(_session);
								}
							}
							finally
							{
								_session = null;
								//DAE.Server.ServerFactory.Disconnect(FServer);
							}
						}
						finally
						{
							_server = null;
							if (_connection != null)
								_connection.Dispose();
						}
					}
					finally
					{
						_connection = null;
					}
					OnAfterClose();
				}
			}
		}

		public virtual object Clone()
		{
			DAEConnection newConnection = new DAEConnection();
			newConnection.ConnectionString = _connectionString;
			newConnection.SetConnectionTimeout(_connectionTimeout);
			newConnection.Alias = _alias;
			newConnection.Active = Active;
			return newConnection;
		}
	}
}
