/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.Design.Serialization;
	using System.Drawing;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
    
	public class Sessions : IEnumerable
	{
		List<DataSession> _list = new List<DataSession>();

		public int Count
		{
			get { return _list.Count; }
		}

		private Object _syncRoot = new Object();
		public object SyncRoot { get { return _syncRoot; } }
		
		protected internal string NextSessionName()
		{
			string request;
			int count = 1;
			lock (_syncRoot)
			{
				while (count <= Count)
				{
					request = "Session" + count.ToString();
					if (!Contains(request))
						return request;
					++count;
				}
			}
			return "Session" + count.ToString();
		}

		public bool Contains(DataSession session)
		{
			return Contains(session.SessionName);
		}

		public bool Contains(string sessionName)
		{
			lock (_syncRoot)
			{
				foreach (DataSession session in _list)
					if ((session.SessionName == sessionName))
						return true;
				return false;
			}
		}

		public void Add(DataSession session)
		{
			lock (_syncRoot)
			{
				_list.Add(session);
			}
		}

		public bool Remove(DataSession session)
		{
			lock (_syncRoot)
			{
				return _list.Remove(session);
			}
		}
		
		public DataSession this[string sessionName]
		{
			get
			{
				lock (_syncRoot)
				{
					foreach (DataSession session in _list)
						if ((session.SessionName == sessionName))
							return session;
					throw new ClientException(ClientException.Codes.SessionNotFound, sessionName);
				}
			}
		}

		public IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}
	}

	/// <summary> Wraps the connection to, and session retrieval from a Server </summary>
	[DesignerSerializer("Alphora.Dataphor.DAE.Client.Design.ActiveLastSerializer,Alphora.Dataphor.DAE.Client", "System.ComponentModel.Design.Serialization.CodeDomSerializer,System.Design")]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DataSession), "Icons.DataSession.bmp")]
	public class DataSession : Component, IDisposableNotify, ISupportInitialize
	{
		public DataSession() : base()
		{
			_sessionInfo = new SessionInfo();
			_sessionName = DataSession.Sessions.NextSessionName();
			Sessions.Add(this);
		}
		
		public DataSession(IContainer container)
			: this()
		{
			if (container != null)
				container.Add(this);
		}

		/// <summary> Disposes the object and notifies other objects </summary>
		/// <seealso cref="IDisposable"/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Close();	// Must close after the Disposed event so that this class is still valid during disposal
			SessionInfo = null;
			DeinitializeSessions();
		}

		private SessionInfo _sessionInfo;
		/// <summary> The <see cref="SessionInfo"/> structure to use when connecting to the server. </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[System.ComponentModel.TypeConverter("System.ComponentModel.ExpandableObjectConverter,Alphora.Dataphor.DAE.Client")]
		[Description("Contextual information for server session initialization")]
		[Category("Session")]
		public SessionInfo SessionInfo
		{
			get { return _sessionInfo; }
			set { _sessionInfo = value; }
		}

			
		#region ServerSession
		
		protected IServerSession _serverSession;
		/// <summary> The CLI Session object obtained through connection. </summary>
		[Browsable(false)]
		public IServerSession ServerSession
		{
			get
			{
				Open();
				return _serverSession;
			}
		}

		protected void ServerSessionDisposed(object sender, EventArgs args)
		{
			_serverSession = null;
			Close();
		}
		
		#endregion

		#region Initializing
		
		protected internal bool Initializing { get; set; }

		public virtual void BeginInit()
		{
			Initializing = true;
		}

		protected internal void DataSetEndInit()
		{
			if (Initializing)
				Active = DelayedActive;
		}

		public virtual void EndInit()
		{
			Initializing = false;
			Active = DelayedActive;
		}
		
		//The Active properties value during the intialization period defined in ISupportInitialize.
		private bool DelayedActive { get; set; }

		#endregion
		
		#region Server connection

		private bool _serverConnectionOwned = true;
		private ServerConnection _serverConnection;
		/// <summary>The server connection established to the Dataphor Server.</summary>
		/// <remarks>
		/// Setting this property allows the DataSession to use an existing ServerConnection.
		/// If this property is not set, a ServerConnection will be created and maintained by
		/// the DataSession component. If this property is set, the given ServerConnection
		/// will be used and the management of that connection is left up to the user of the
		/// component. Note that if this property is set, it is not necessary to set the
		/// Alias or AliasName properties of the DataSession.
		/// </remarks>
		public ServerConnection ServerConnection
		{
			get { return _serverConnection; }
			set
			{
				CheckInactive();
				_serverConnection = value;
				_serverConnectionOwned = false;
			}
		}
		
		/// <summary> The IServer interface to the server. </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public IServer Server
		{
			get
			{
				CheckActive();
				return _serverConnection.Server;
			}
		}
		
		#endregion
		
		#region Open/Close
			
		// Active
		private bool _active;
		
		/// <summary> The current connection state of this session. </summary>
		[DefaultValue(false)]
		[Category("Session")]
		[Description("Connection state of this session.")]
		public bool Active
		{
			get { return _active; }
			set
			{
				if (Initializing)
					DelayedActive = value;
				else
				{
					if (_active != value)
					{
						if (value)
							Open();
						else
							Close();
					}
				}
			}
		}

		protected void CheckActive()
		{
			if (!_active)
				throw new ClientException(ClientException.Codes.DataSessionInactive);
		}

		/// <summary> Used internally to throw if the session isn't active. </summary>
		protected void CheckInactive()
		{
			if (_active)
				throw new ClientException(ClientException.Codes.DataSessionActive);
		}

		/// <summary> Connects to a server and retrieves a session from it. </summary>
		public void Open()
		{
			if (!_active)
			{
				InternalOpen();
				_active = true;
			}
		}

		protected void InternalOpen()
		{
			if (_serverConnection == null)
			{
				CheckAlias();
				_serverConnection = new ServerConnection(_alias);
				_serverConnectionOwned = true;
			}
			try
			{
				_serverSession = _serverConnection.Server.Connect(SessionInfo);
				_serverSession.Disposed += new EventHandler(ServerSessionDisposed);
			}
			catch
			{
				CloseConnection();
				throw;
			}
		}

		/// <summary> Dereferences the session and disconnects from the server. </summary>
		public void Close()
		{
			if (_active)
			{
				try
				{
					try
					{
						if (_utilityProcess != null)
						{
							ServerSession.StopProcess(_utilityProcess);
							_utilityProcess = null;
						}
						if (OnClosing != null)
							OnClosing(this, EventArgs.Empty);
					}
					finally
					{
						InternalClose();
					}
				}
				finally
				{
					_active = false;
				}
			}
		}
		
		private void ServerStoppedOrDisposed(object sender, EventArgs args)
		{
			Close();
		}
		
		protected void InternalClose()
		{
			try
			{
				try
				{
					if (_serverSession != null)
					{
						_serverSession.Disposed -= new EventHandler(ServerSessionDisposed);
						Server.Disconnect(_serverSession);
					}
				}
				finally
				{
					_serverSession = null;
				}
			}
			finally
			{
				CloseConnection();
			}
		}

		private void CloseConnection()
		{
			if (_serverConnectionOwned && (_serverConnection != null))
			{
				_serverConnection.Dispose();
				_serverConnection = null;
			}
		}
		
		/// <summary> Event to notify objects that this object has been disposed. </summary>
		public event EventHandler OnClosing;

		#endregion

		#region Session Name / Sessions static
		
		private string _sessionName;
		/// <summary> Associates a global name with the session. </summary>
		/// <remarks> The SessionName can be used to reference the DataSession instance elsewere in the application. </remarks>
		[Description("Associates a global name with the session.")]
		[Category("Session")]
		public string SessionName
		{
			get	{ return _sessionName; }
			set
			{
				if (_sessionName != value)
				{
					if (DataSession.Sessions.Contains(value))
						throw new ClientException(ClientException.Codes.SessionExists, value);
					_sessionName = value;
				}
			}
		}
		
		private static Sessions _sessions;
		protected internal static Sessions Sessions
		{
			get
			{
				if (_sessions == null)
					_sessions = new Sessions();
				return _sessions;
			}
		}

		private void DeinitializeSessions()
		{
			if (Sessions.Contains(this))
				Sessions.Remove(this);
		}
		
		#endregion
		
		#region Utility process
		
		private IServerProcess _utilityProcess = null;
		public IServerProcess UtilityProcess
		{
			get
			{
				CheckActive();
				
				if (_utilityProcess == null)
					_utilityProcess = ServerSession.StartProcess(new ProcessInfo(ServerSession.SessionInfo));
				return _utilityProcess;
			}
		}

		#endregion
		
		#region Type helpers
		
		/// <summary>Returns the equivalent D4 type for the given native (CLR) type. Will throw a ClientException if there is no mapping for the given native (CLR) type.</summary>
		public static DAE.Schema.IScalarType ScalarTypeFromNativeType(IServerProcess process, Type type)
		{
			if (type == null)
				return process.DataTypes.SystemScalar;

			switch (type.Name)
			{
				case "Int32" : return process.DataTypes.SystemInteger;
				case "Int64" : return process.DataTypes.SystemLong;
				case "Int16" : return process.DataTypes.SystemShort;
				case "Byte" : return process.DataTypes.SystemByte;
				case "Boolean" : return process.DataTypes.SystemBoolean;
				case "String" : return process.DataTypes.SystemString;
				case "Decimal" : return process.DataTypes.SystemDecimal;
				case "DateTime" : return process.DataTypes.SystemDateTime;
				case "TimeSpan" : return process.DataTypes.SystemTimeSpan;
				case "Guid" : return process.DataTypes.SystemGuid;
				case "System.Exception" : return process.DataTypes.SystemError;
				default : throw new ClientException(ClientException.Codes.InvalidParamType, type.Name);
			}
		}
		
		#endregion
		
		#region Param helpers
		
		/// <summary>Constructs a DataParams from the given native value array, automatically naming the parameters A0..An-1.</summary>
		public static DAE.Runtime.DataParams DataParamsFromNativeParams(IServerProcess process, params object[] paramsValue)
		{
            DAE.Runtime.DataParams localParamsValue = new DAE.Runtime.DataParams();
            for (int i = 0; i < paramsValue.Length; i++)
            {
                object objectValue = paramsValue[i];
                if (objectValue is DBNull)
					objectValue = null;
                if (objectValue is Double)
					objectValue = Convert.ToDecimal((double)objectValue);
                localParamsValue.Add(DAE.Runtime.DataParam.Create(process, "A" + i.ToString(), objectValue, ScalarTypeFromNativeType(process, objectValue == null ? null : objectValue.GetType())));
            }
            return localParamsValue;
		}
		
		/// <summary>Constructs a DataParams from the given parameter names and native value arrays.</summary>
        public static DAE.Runtime.DataParams DataParamsFromNativeParams(IServerProcess process, string[] paramNames, object[] paramsValue)
        {
            DAE.Runtime.DataParams localParamsValue = new DAE.Runtime.DataParams();
            for (int i = 0; i < paramsValue.Length; i++)
            {
                object objectValue = paramsValue[i];
                if (objectValue is DBNull)
					objectValue = null;
                if (objectValue is Double)
					objectValue = Convert.ToDecimal((double)objectValue);
                localParamsValue.Add(DAE.Runtime.DataParam.Create(process, paramNames[i], objectValue, ScalarTypeFromNativeType(process, objectValue == null ? null : objectValue.GetType())));
            }
            return localParamsValue;
        }
        
        #endregion
        
        #region ExecuteScript

		/// <summary>Executes the given script using the utility process.</summary>
		public void ExecuteScript(string script)
		{
			ExecuteScript(null, script, null);
		}

		/// <summary>Executes the given script using the given process.</summary>
		public void ExecuteScript(IServerProcess process, string script)
		{
			ExecuteScript(process, script, null);
		}

		/// <summary>Executes the given script using the utility process.</summary>
		public void ExecuteScript(string script, DAE.Runtime.DataParams paramsValue)
		{
			ExecuteScript(null, script, paramsValue);
		}

		/// <summary>Executes the given script using the given process.</summary>
		public void ExecuteScript(IServerProcess process, string script, DAE.Runtime.DataParams paramsValue)
		{
			if (script != String.Empty)
			{
				CheckActive();

				if (process == null)
					process = UtilityProcess;

				IServerScript localScript = process.PrepareScript(script);
				try
				{
					localScript.Execute(paramsValue);
				}
				finally
				{
					process.UnprepareScript(localScript);
				}
			}
		}
		
		#endregion
		
		#region Execute
		
		/// <summary>Executes the given statement using the utility process.</summary>
		public void Execute(string statement)
		{
            Execute(null, statement, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Executes the given statement on the given process.</summary>
		public void Execute(IServerProcess process, string statement)
		{
            Execute(process, statement, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Executes the given statement using the utility process.</summary>
		public void Execute(string statement, DAE.Runtime.DataParams paramsValue)
		{
			Execute(null, statement, paramsValue);
		}
		
		/// <summary>Executes the given statement using the utility process.</summary>
		public void Execute(IServerProcess process, string statement, DAE.Runtime.DataParams paramsValue)
		{
			CheckActive();
			
			if (process == null)
				process = UtilityProcess;
				
			IServerStatementPlan plan = process.PrepareStatement(statement, paramsValue);
			try
			{
				plan.Execute(paramsValue);
			}
			finally
			{
				process.UnprepareStatement(plan);
			}
		}
        
		/// <summary>Executes the given statement using the utility process and using the given parameter values (auto numbered A0..An-1).</summary>
		public void Execute(string statement, params object[] paramsValue)
		{
			Execute(null, statement, paramsValue);
		}
		
        /// <summary> Executes the given statement on the given process and using the given parameter values (auto numbered A0..An-1). </summary>
        public void Execute(IServerProcess process, string statement, params object[] paramsValue)
        {
            if (process == null)
                process = UtilityProcess;

            Execute(process, statement, DataParamsFromNativeParams(process, paramsValue));
        }
        
        /// <summary> Executes the given statement on the given process and using the given parameter names and values. </summary>
        public void Execute(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
        {
            if (process == null)
                process = UtilityProcess;

            Execute(process, expression, DataParamsFromNativeParams(process, paramNames, paramsValue));
        }
        
        #endregion
        
        #region Evaluate
        
		/// <summary> Evaluates the given expression using the utility process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(string expression)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(expression);
        }
        
		/// <summary>Evaluates the given expression on the given process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(IServerProcess process, string expression)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(process, expression);
        }
        
		/// <summary>Evaluates the given expression using the utility process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>Evaluates the given expression using the given process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>Evaluates the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>Evaluates the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(IServerProcess process, string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>Evaluates the given expression on the given process using the given parameter names and values and returns the result as a scalar.</summary>
		public DAE.Runtime.Data.Scalar Evaluate(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(process, expression, paramNames, paramsValue);
		}
		
		#endregion
		
        #region EvaluateRow
        
		/// <summary> EvaluateRows the given expression using the utility process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(string expression)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(expression);
        }
        
		/// <summary>EvaluateRows the given expression on the given process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(IServerProcess process, string expression)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(process, expression);
        }
        
		/// <summary>EvaluateRows the given expression using the utility process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>EvaluateRows the given expression using the given process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>EvaluateRows the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>EvaluateRows the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(IServerProcess process, string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>EvaluateRows the given expression on the given process using the given parameter names and values and returns the result as a row.</summary>
		public DAE.Runtime.Data.Row EvaluateRow(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			return (DAE.Runtime.Data.Row)EvaluateRaw(process, expression, paramNames, paramsValue);
		}
		
		#endregion
		
        #region EvaluateList
        
		/// <summary> EvaluateLists the given expression using the utility process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(string expression)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(expression);
        }
        
		/// <summary>EvaluateLists the given expression on the given process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess process, string expression)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(process, expression);
        }
        
		/// <summary>EvaluateLists the given expression using the utility process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>EvaluateLists the given expression using the given process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>EvaluateLists the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>EvaluateLists the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess process, string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>EvaluateLists the given expression on the given process using the given parameter names and values and returns the result as a list.</summary>
		public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(process, expression, paramNames, paramsValue);
		}
		
		#endregion
		
        #region EvaluateTable
        
		/// <summary> EvaluateTables the given expression using the utility process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(string expression)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(expression);
        }
        
		/// <summary>EvaluateTables the given expression on the given process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(IServerProcess process, string expression)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(process, expression);
        }
        
		/// <summary>EvaluateTables the given expression using the utility process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>EvaluateTables the given expression using the given process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>EvaluateTables the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(expression, paramsValue);
        }
        
		/// <summary>EvaluateTables the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(IServerProcess process, string expression, params object[] paramsValue)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(process, expression, paramsValue);
        }
        
		/// <summary>EvaluateTables the given expression on the given process using the given parameter names and values and returns the result as a table.</summary>
		public DAE.Runtime.Data.Table EvaluateTable(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			return (DAE.Runtime.Data.Table)EvaluateRaw(process, expression, paramNames, paramsValue);
		}
		
		#endregion
		
		#region EvaluateRaw
		
		/// <summary> Evaluates the given expression using the utility process and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(string expression)
		{
			return EvaluateRaw(null, expression, (DAE.Runtime.DataParams)null);
		}

		/// <summary>Evaluates the given expression on the given process and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(IServerProcess process, string expression)
		{
			return EvaluateRaw(process, expression, (DAE.Runtime.DataParams)null);
		}

		/// <summary>Evaluates the given expression using the utility process and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(string expression, DAE.Runtime.DataParams paramsValue)
		{
			return EvaluateRaw(null, expression, paramsValue);
		}

		/// <summary>Evaluates the given expression using the given process and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
		{
			CheckActive();

			if (process == null)
				process = UtilityProcess;

			IServerExpressionPlan plan = process.PrepareExpression(expression, paramsValue);
			try
			{
				return plan.Evaluate(paramsValue);
			}
			finally
			{
				process.UnprepareExpression(plan);
			}
		}
		
		/// <summary>Evaluates the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(string expression, params object[] paramsValue)
		{
			return EvaluateRaw(null, expression, paramsValue);
		}

		/// <summary>Evaluates the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(IServerProcess process, string expression, params object[] paramsValue)
		{
			if (process == null)
				process = UtilityProcess;
			
			return EvaluateRaw(process, expression, DataParamsFromNativeParams(process, paramsValue));
		}

		/// <summary>Evaluates the given expression on the given process using the given parameter names and values and returns the result.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateRaw(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			if (process == null)
				process = UtilityProcess;
			
			return EvaluateRaw(process, expression, DataParamsFromNativeParams(process, paramNames, paramsValue));
		}
		
		#endregion
		
		#region OpenCursor

		/// <summary>Opens a cursor on the given expression using the utility process.</summary>
		public IServerCursor OpenCursor(string expression)
		{
			return OpenCursor(null, expression, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Opens a cursor on the given expression using the given process.</summary>
		public IServerCursor OpenCursor(IServerProcess process, string expression)
		{
			return OpenCursor(process, expression, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Opens a cursor on the given expression using the utility process.</summary>
		public IServerCursor OpenCursor(string expression, DAE.Runtime.DataParams paramsValue)
		{
			return OpenCursor(null, expression, paramsValue);
		}
		
		/// <summary>Opens a cursor on the given expression using the given process.</summary>
		public IServerCursor OpenCursor(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
		{                              			
			CheckActive();
			
			if (process == null)
				process = UtilityProcess;
			
			IServerExpressionPlan plan = process.PrepareExpression(expression, paramsValue);
			try
			{
				return plan.Open(paramsValue);
			}
			catch
			{
				process.UnprepareExpression(plan);
				throw;
			}
		}
		
		public IServerCursor OpenCursor(string expression, params object[] paramsValue)
		{
			return OpenCursor(null, expression, paramsValue);
		}
		
		public IServerCursor OpenCursor(IServerProcess process, string expression, params object[] paramsValue)
		{
			if (process == null)
				process = UtilityProcess;

			return OpenCursor(process, expression, DataParamsFromNativeParams(process, paramsValue));
		}
		
		public IServerCursor OpenCursor(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			if (process == null)
				process = UtilityProcess;
				
			return OpenCursor(process, expression, DataParamsFromNativeParams(process, paramNames, paramsValue));
		}
		
		/// <summary>Closes the given cursor.</summary>
		public void CloseCursor(IServerCursor cursor)
		{
			IServerExpressionPlan plan = cursor.Plan;
			plan.Close(cursor);
			plan.Process.UnprepareExpression(plan);
		}
		
		#endregion
		
		#region OpenDataView
		
		private DataView OpenDataView(string expression, DataSetState initialState, bool isReadOnly)
		{
			DataView dataView = new DataView();
			try
			{
				dataView.Session = this;
				dataView.Expression = expression;
				dataView.IsReadOnly = isReadOnly;
				dataView.Open(initialState);
				return dataView;
			}
			catch
			{
				dataView.Dispose();
				throw;
			}
		}

		/// <summary> Opens a DataView on this session using the specified expression. </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public DataView OpenDataView(string expression)
		{
			return OpenDataView(expression, DataSetState.Browse, false);
		}

		/// <summary> Opens a DataView on this session using the specified expression and opened in the specified state. </summary>
		/// <param name="expression"></param>
		/// <param name="initialState"></param>
		/// <returns></returns>
		public DataView OpenDataView(string expression, DataSetState initialState)
		{
			return OpenDataView(expression, initialState, false);
		}

		/// <summary> Opens a read-only DataView on this session using the specified expression. </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		/// <remarks>This opens up a slightly faster database cursor than the read/write OpenDataView call so use this one if you are just retrieving data.</remarks>
		public DataView OpenReadOnlyDataView(string expression)
		{
			return OpenDataView(expression, DataSetState.Browse, true);
		}
		
		#endregion

		#region Alias
		
		#if !SILVERLIGHT
		// AliasName
		private string _aliasName = String.Empty;
		/// <summary> The name of the alias to use to establish a connection to a Dataphor Server. </summary>
		/// <remarks>
		/// If an alias name is provided, an AliasManager will be used to retrieve the alias settings.
		/// Setting this property will clear the value of the Alias property.
		/// </remarks>
		[DefaultValue("")]
		[Category("Session")]
		[Description("The name of the alias to use to establish a connection to a Dataphor Server.")]
		public string AliasName 
		{ 
			get { return _aliasName; } 
			set 
			{ 
				CheckInactive();
				if (_aliasName != value)
				{
					InternalSetAlias(AliasManager.GetAlias(value));
					_aliasName = value;
				}
			}
		}
		#endif
		
		private void InternalSetAlias(ServerAlias alias)
		{
			_alias = alias;
			if (_alias != null)
				SessionInfo = (SessionInfo)_alias.SessionInfo.Clone();
		}
		
		private void CheckAlias()
		{
			if (_alias == null)
				throw new ClientException(ClientException.Codes.NoServerAliasSpecified);
		}
		
		// Alias
		private ServerAlias _alias;
		/// <summary>The alias used to establish a connection to a Dataphor Server.</summary>
		/// <remarks>
		/// In order to use this property, the AliasName property must not be set. If an AliasName
		/// is provided, it will be used to lookup the alias, and the value of this property will be the
		/// alias retrieved from the alias manager by name. Setting this property will clear the value
		/// of the AliasName property.
		/// </remarks>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ServerAlias Alias
		{
			get
			{
				return _alias;
			}
			set
			{
				CheckInactive();
				if (_alias != value)
				{
					InternalSetAlias(value);
					#if !SILVERLIGHT
					_aliasName = String.Empty;
					#endif
				}
			}
		}
		
		#endregion
	}
}