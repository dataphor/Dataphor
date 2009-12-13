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
		List<DataSession> FList = new List<DataSession>();

		public int Count
		{
			get { return FList.Count; }
		}

		private Object FSyncRoot = new Object();
		public object SyncRoot { get { return FSyncRoot; } }
		
		protected internal string NextSessionName()
		{
			string LRequest;
			int LCount = 1;
			lock (FSyncRoot)
			{
				while (LCount <= Count)
				{
					LRequest = "Session" + LCount.ToString();
					if (!Contains(LRequest))
						return LRequest;
					++LCount;
				}
			}
			return "Session" + LCount.ToString();
		}

		public bool Contains(DataSession ASession)
		{
			return Contains(ASession.SessionName);
		}

		public bool Contains(string ASessionName)
		{
			lock (FSyncRoot)
			{
				foreach (DataSession LSession in FList)
					if ((LSession.SessionName == ASessionName))
						return true;
				return false;
			}
		}

		public void Add(DataSession ASession)
		{
			lock (FSyncRoot)
			{
				FList.Add(ASession);
			}
		}

		public bool Remove(DataSession ASession)
		{
			lock (FSyncRoot)
			{
				return FList.Remove(ASession);
			}
		}
		
		public DataSession this[string ASessionName]
		{
			get
			{
				lock (FSyncRoot)
				{
					foreach (DataSession LSession in FList)
						if ((LSession.SessionName == ASessionName))
							return LSession;
					throw new ClientException(ClientException.Codes.SessionNotFound, ASessionName);
				}
			}
		}

		public IEnumerator GetEnumerator()
		{
			return FList.GetEnumerator();
		}
	}

	/// <summary> Wraps the connection to, and session retrieval from a Server </summary>
	[DesignerSerializer("Alphora.Dataphor.DAE.Client.Design.ActiveLastSerializer,Alphora.Dataphor.DAE.Client", "System.ComponentModel.Design.Serialization.CodeDomSerializer,System.Design")]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.DataSession), "Icons.DataSession.bmp")]
	public class DataSession : Component, IDisposableNotify, ISupportInitialize
	{
		public DataSession() : base()
		{
			FSessionInfo = new SessionInfo();
			FSessionName = DataSession.Sessions.NextSessionName();
			Sessions.Add(this);
		}
		
		public DataSession(IContainer AContainer)
			: this()
		{
			if (AContainer != null)
				AContainer.Add(this);
		}

		/// <summary> Disposes the object and notifies other objects </summary>
		/// <seealso cref="IDisposable"/>
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Close();	// Must close after the Disposed event so that this class is still valid during disposal
			SessionInfo = null;
			DeinitializeSessions();
		}

		private SessionInfo FSessionInfo;
		/// <summary> The <see cref="SessionInfo"/> structure to use when connecting to the server. </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[System.ComponentModel.TypeConverter("System.ComponentModel.ExpandableObjectConverter,Alphora.Dataphor.DAE.Client")]
		[Description("Contextual information for server session initialization")]
		[Category("Session")]
		public SessionInfo SessionInfo
		{
			get { return FSessionInfo; }
			set { FSessionInfo = value; }
		}

			
		#region ServerSession
		
		protected IServerSession FServerSession;
		/// <summary> The CLI Session object obtained through connection. </summary>
		[Browsable(false)]
		public IServerSession ServerSession
		{
			get
			{
				Open();
				return FServerSession;
			}
		}

		protected void ServerSessionDisposed(object ASender, EventArgs AArgs)
		{
			FServerSession = null;
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

		private bool FServerConnectionOwned = true;
		private ServerConnection FServerConnection;
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
			get { return FServerConnection; }
			set
			{
				CheckInactive();
				FServerConnection = value;
				FServerConnectionOwned = false;
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
				return FServerConnection.Server;
			}
		}
		
		#endregion
		
		#region Open/Close
			
		// Active
		private bool FActive;
		
		/// <summary> The current connection state of this session. </summary>
		[DefaultValue(false)]
		[Category("Session")]
		[Description("Connection state of this session.")]
		public bool Active
		{
			get { return FActive; }
			set
			{
				if (Initializing)
					DelayedActive = value;
				else
				{
					if (FActive != value)
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
			if (!FActive)
				throw new ClientException(ClientException.Codes.DataSessionInactive);
		}

		/// <summary> Used internally to throw if the session isn't active. </summary>
		protected void CheckInactive()
		{
			if (FActive)
				throw new ClientException(ClientException.Codes.DataSessionActive);
		}

		/// <summary> Connects to a server and retrieves a session from it. </summary>
		public void Open()
		{
			if (!FActive)
			{
				InternalOpen();
				FActive = true;
			}
		}

		protected void InternalOpen()
		{
			if (FServerConnection == null)
			{
				CheckAlias();
				FServerConnection = new ServerConnection(FAlias);
				FServerConnectionOwned = true;
			}
			try
			{
				FServerSession = FServerConnection.Server.Connect(SessionInfo);
				FServerSession.Disposed += new EventHandler(ServerSessionDisposed);
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
			if (FActive)
			{
				try
				{
					try
					{
						if (FUtilityProcess != null)
						{
							ServerSession.StopProcess(FUtilityProcess);
							FUtilityProcess = null;
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
					FActive = false;
				}
			}
		}
		
		private void ServerStoppedOrDisposed(object ASender, EventArgs AArgs)
		{
			Close();
		}
		
		protected void InternalClose()
		{
			try
			{
				try
				{
					if (FServerSession != null)
					{
						FServerSession.Disposed -= new EventHandler(ServerSessionDisposed);
						Server.Disconnect(FServerSession);
					}
				}
				finally
				{
					FServerSession = null;
				}
			}
			finally
			{
				CloseConnection();
			}
		}

		private void CloseConnection()
		{
			if (FServerConnectionOwned && (FServerConnection != null))
			{
				FServerConnection.Dispose();
				FServerConnection = null;
			}
		}
		
		/// <summary> Event to notify objects that this object has been disposed. </summary>
		public event EventHandler OnClosing;

		#endregion

		#region Session Name / Sessions static
		
		private string FSessionName;
		/// <summary> Associates a global name with the session. </summary>
		/// <remarks> The SessionName can be used to reference the DataSession instance elsewere in the application. </remarks>
		[Description("Associates a global name with the session.")]
		[Category("Session")]
		public string SessionName
		{
			get	{ return FSessionName; }
			set
			{
				if (FSessionName != value)
				{
					if (DataSession.Sessions.Contains(value))
						throw new ClientException(ClientException.Codes.SessionExists, value);
					FSessionName = value;
				}
			}
		}
		
		private static Sessions FSessions;
		protected internal static Sessions Sessions
		{
			get
			{
				if (FSessions == null)
					FSessions = new Sessions();
				return FSessions;
			}
		}

		private void DeinitializeSessions()
		{
			if (Sessions.Contains(this))
				Sessions.Remove(this);
		}
		
		#endregion
		
		#region Utility process
		
		private IServerProcess FUtilityProcess = null;
		public IServerProcess UtilityProcess
		{
			get
			{
				CheckActive();
				
				if (FUtilityProcess == null)
					FUtilityProcess = ServerSession.StartProcess(new ProcessInfo(ServerSession.SessionInfo));
				return FUtilityProcess;
			}
		}

		#endregion
		
		#region Type helpers
		
		/// <summary>Returns the equivalent D4 type for the given native (CLR) type. Will throw a ClientException if there is no mapping for the given native (CLR) type.</summary>
		public static DAE.Schema.IScalarType ScalarTypeFromNativeType(IServerProcess AProcess, Type AType)
		{
			if (AType == null)
				return AProcess.DataTypes.SystemScalar;

			switch (AType.Name)
			{
				case "Int32" : return AProcess.DataTypes.SystemInteger;
				case "Byte" : return AProcess.DataTypes.SystemByte;
				case "Boolean" : return AProcess.DataTypes.SystemBoolean;
				case "String" : return AProcess.DataTypes.SystemString;
				case "Decimal" : return AProcess.DataTypes.SystemDecimal;
				case "DateTime" : return AProcess.DataTypes.SystemDateTime;
				case "TimeSpan" : return AProcess.DataTypes.SystemTimeSpan;
				case "Guid" : return AProcess.DataTypes.SystemGuid;
				case "System.Exception" : return AProcess.DataTypes.SystemError;
				default : throw new ClientException(ClientException.Codes.InvalidParamType, AType.Name);
			}
		}
		
		#endregion
		
		#region Param helpers
		
		/// <summary>Constructs a DataParams from the given native value array, automatically naming the parameters A0..An-1.</summary>
		public static DAE.Runtime.DataParams DataParamsFromNativeParams(IServerProcess AProcess, params object[] AParams)
		{
            DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
            for (int i = 0; i < AParams.Length; i++)
            {
                object LObject = AParams[i];
                if (LObject is DBNull)
					LObject = null;
                if (LObject is Double)
					LObject = Convert.ToDecimal((double)LObject);
                LParams.Add(DAE.Runtime.DataParam.Create(AProcess, "A" + i.ToString(), LObject, ScalarTypeFromNativeType(AProcess, LObject == null ? null : LObject.GetType())));
            }
            return LParams;
		}
		
		/// <summary>Constructs a DataParams from the given parameter names and native value arrays.</summary>
        public static DAE.Runtime.DataParams DataParamsFromNativeParams(IServerProcess AProcess, string[] AParamNames, object[] AParams)
        {
            DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
            for (int i = 0; i < AParams.Length; i++)
            {
                object LObject = AParams[i];
                if (LObject is DBNull)
					LObject = null;
                if (LObject is Double)
					LObject = Convert.ToDecimal((double)LObject);
                LParams.Add(DAE.Runtime.DataParam.Create(AProcess, AParamNames[i], LObject, ScalarTypeFromNativeType(AProcess, LObject == null ? null : LObject.GetType())));
            }
            return LParams;
        }
        
        #endregion
        
        #region ExecuteScript

		/// <summary>Executes the given script using the utility process.</summary>
		public void ExecuteScript(string AScript)
		{
			ExecuteScript(null, AScript, null);
		}

		/// <summary>Executes the given script using the given process.</summary>
		public void ExecuteScript(IServerProcess AProcess, string AScript)
		{
			ExecuteScript(AProcess, AScript, null);
		}

		/// <summary>Executes the given script using the utility process.</summary>
		public void ExecuteScript(string AScript, DAE.Runtime.DataParams AParams)
		{
			ExecuteScript(null, AScript, AParams);
		}

		/// <summary>Executes the given script using the given process.</summary>
		public void ExecuteScript(IServerProcess AProcess, string AScript, DAE.Runtime.DataParams AParams)
		{
			if (AScript != String.Empty)
			{
				CheckActive();

				if (AProcess == null)
					AProcess = UtilityProcess;

				IServerScript LScript = AProcess.PrepareScript(AScript);
				try
				{
					LScript.Execute(AParams);
				}
				finally
				{
					AProcess.UnprepareScript(LScript);
				}
			}
		}
		
		#endregion
		
		#region Execute
		
		/// <summary>Executes the given statement using the utility process.</summary>
		public void Execute(string AStatement)
		{
            Execute(null, AStatement, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Executes the given statement on the given process.</summary>
		public void Execute(IServerProcess AProcess, string AStatement)
		{
            Execute(AProcess, AStatement, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Executes the given statement using the utility process.</summary>
		public void Execute(string AStatement, DAE.Runtime.DataParams AParams)
		{
			Execute(null, AStatement, AParams);
		}
		
		/// <summary>Executes the given statement using the utility process.</summary>
		public void Execute(IServerProcess AProcess, string AStatement, DAE.Runtime.DataParams AParams)
		{
			CheckActive();
			
			if (AProcess == null)
				AProcess = UtilityProcess;
				
			IServerStatementPlan LPlan = AProcess.PrepareStatement(AStatement, AParams);
			try
			{
				LPlan.Execute(AParams);
			}
			finally
			{
				AProcess.UnprepareStatement(LPlan);
			}
		}
        
		/// <summary>Executes the given statement using the utility process and using the given parameter values (auto numbered A0..An-1).</summary>
		public void Execute(string AStatement, params object[] AParams)
		{
			Execute(null, AStatement, AParams);
		}
		
        /// <summary> Executes the given statement on the given process and using the given parameter values (auto numbered A0..An-1). </summary>
        public void Execute(IServerProcess AProcess, string AStatement, params object[] AParams)
        {
            if (AProcess == null)
                AProcess = UtilityProcess;

            Execute(AProcess, AStatement, DataParamsFromNativeParams(AProcess, AParams));
        }
        
        /// <summary> Executes the given statement on the given process and using the given parameter names and values. </summary>
        public void Execute(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
        {
            if (AProcess == null)
                AProcess = UtilityProcess;

            Execute(AProcess, AExpression, DataParamsFromNativeParams(AProcess, AParamNames, AParams));
        }
        
        #endregion
        
        #region Evaluate
        
		/// <summary> Evaluates the given expression using the utility process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(string AExpression)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AExpression);
        }
        
		/// <summary>Evaluates the given expression on the given process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(IServerProcess AProcess, string AExpression)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AProcess, AExpression);
        }
        
		/// <summary>Evaluates the given expression using the utility process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>Evaluates the given expression using the given process and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>Evaluates the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>Evaluates the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a scalar.</summary>
        public DAE.Runtime.Data.Scalar Evaluate(IServerProcess AProcess, string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>Evaluates the given expression on the given process using the given parameter names and values and returns the result as a scalar.</summary>
		public DAE.Runtime.Data.Scalar Evaluate(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			return (DAE.Runtime.Data.Scalar)EvaluateRaw(AProcess, AExpression, AParamNames, AParams);
		}
		
		#endregion
		
        #region EvaluateRow
        
		/// <summary> EvaluateRows the given expression using the utility process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(string AExpression)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(AExpression);
        }
        
		/// <summary>EvaluateRows the given expression on the given process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(IServerProcess AProcess, string AExpression)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(AProcess, AExpression);
        }
        
		/// <summary>EvaluateRows the given expression using the utility process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>EvaluateRows the given expression using the given process and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>EvaluateRows the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>EvaluateRows the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a row.</summary>
        public DAE.Runtime.Data.Row EvaluateRow(IServerProcess AProcess, string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.Row)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>EvaluateRows the given expression on the given process using the given parameter names and values and returns the result as a row.</summary>
		public DAE.Runtime.Data.Row EvaluateRow(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			return (DAE.Runtime.Data.Row)EvaluateRaw(AProcess, AExpression, AParamNames, AParams);
		}
		
		#endregion
		
        #region EvaluateList
        
		/// <summary> EvaluateLists the given expression using the utility process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(string AExpression)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AExpression);
        }
        
		/// <summary>EvaluateLists the given expression on the given process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess AProcess, string AExpression)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AProcess, AExpression);
        }
        
		/// <summary>EvaluateLists the given expression using the utility process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>EvaluateLists the given expression using the given process and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>EvaluateLists the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>EvaluateLists the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a list.</summary>
        public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess AProcess, string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>EvaluateLists the given expression on the given process using the given parameter names and values and returns the result as a list.</summary>
		public DAE.Runtime.Data.ListValue EvaluateList(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			return (DAE.Runtime.Data.ListValue)EvaluateRaw(AProcess, AExpression, AParamNames, AParams);
		}
		
		#endregion
		
        #region EvaluateTable
        
		/// <summary> EvaluateTables the given expression using the utility process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(string AExpression)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(AExpression);
        }
        
		/// <summary>EvaluateTables the given expression on the given process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(IServerProcess AProcess, string AExpression)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(AProcess, AExpression);
        }
        
		/// <summary>EvaluateTables the given expression using the utility process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>EvaluateTables the given expression using the given process and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>EvaluateTables the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(AExpression, AParams);
        }
        
		/// <summary>EvaluateTables the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result as a table.</summary>
        public DAE.Runtime.Data.Table EvaluateTable(IServerProcess AProcess, string AExpression, params object[] AParams)
        {
			return (DAE.Runtime.Data.Table)EvaluateRaw(AProcess, AExpression, AParams);
        }
        
		/// <summary>EvaluateTables the given expression on the given process using the given parameter names and values and returns the result as a table.</summary>
		public DAE.Runtime.Data.Table EvaluateTable(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			return (DAE.Runtime.Data.Table)EvaluateRaw(AProcess, AExpression, AParamNames, AParams);
		}
		
		#endregion
		
		#region EvaluateRaw
		
		/// <summary> Evaluates the given expression using the utility process and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(string AExpression)
		{
			return EvaluateRaw(null, AExpression, (DAE.Runtime.DataParams)null);
		}

		/// <summary>Evaluates the given expression on the given process and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(IServerProcess AProcess, string AExpression)
		{
			return EvaluateRaw(AProcess, AExpression, (DAE.Runtime.DataParams)null);
		}

		/// <summary>Evaluates the given expression using the utility process and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(string AExpression, DAE.Runtime.DataParams AParams)
		{
			return EvaluateRaw(null, AExpression, AParams);
		}

		/// <summary>Evaluates the given expression using the given process and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
		{
			CheckActive();

			if (AProcess == null)
				AProcess = UtilityProcess;

			IServerExpressionPlan LPlan = AProcess.PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Evaluate(AParams);
			}
			finally
			{
				AProcess.UnprepareExpression(LPlan);
			}
		}
		
		/// <summary>Evaluates the given expression using the utility process and the given parameter values (auto numbered A0..An-1) and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(string AExpression, params object[] AParams)
		{
			return EvaluateRaw(null, AExpression, AParams);
		}

		/// <summary>Evaluates the given expression on the given process using the given parameter values (auto numbered A0..An-1) and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(IServerProcess AProcess, string AExpression, params object[] AParams)
		{
			if (AProcess == null)
				AProcess = UtilityProcess;
			
			return EvaluateRaw(AProcess, AExpression, DataParamsFromNativeParams(AProcess, AParams));
		}

		/// <summary>Evaluates the given expression on the given process using the given parameter names and values and returns the result.</summary>
		public DAE.Runtime.Data.DataValue EvaluateRaw(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			if (AProcess == null)
				AProcess = UtilityProcess;
			
			return EvaluateRaw(AProcess, AExpression, DataParamsFromNativeParams(AProcess, AParamNames, AParams));
		}
		
		#endregion
		
		#region OpenCursor

		/// <summary>Opens a cursor on the given expression using the utility process.</summary>
		public IServerCursor OpenCursor(string AExpression)
		{
			return OpenCursor(null, AExpression, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Opens a cursor on the given expression using the given process.</summary>
		public IServerCursor OpenCursor(IServerProcess AProcess, string AExpression)
		{
			return OpenCursor(AProcess, AExpression, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Opens a cursor on the given expression using the utility process.</summary>
		public IServerCursor OpenCursor(string AExpression, DAE.Runtime.DataParams AParams)
		{
			return OpenCursor(null, AExpression, AParams);
		}
		
		/// <summary>Opens a cursor on the given expression using the given process.</summary>
		public IServerCursor OpenCursor(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
		{                              			
			CheckActive();
			
			if (AProcess == null)
				AProcess = UtilityProcess;
			
			IServerExpressionPlan LPlan = AProcess.PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Open(AParams);
			}
			catch
			{
				AProcess.UnprepareExpression(LPlan);
				throw;
			}
		}
		
		public IServerCursor OpenCursor(string AExpression, params object[] AParams)
		{
			return OpenCursor(null, AExpression, AParams);
		}
		
		public IServerCursor OpenCursor(IServerProcess AProcess, string AExpression, params object[] AParams)
		{
			if (AProcess == null)
				AProcess = UtilityProcess;

			return OpenCursor(AProcess, AExpression, DataParamsFromNativeParams(AProcess, AParams));
		}
		
		public IServerCursor OpenCursor(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			if (AProcess == null)
				AProcess = UtilityProcess;
				
			return OpenCursor(AProcess, AExpression, DataParamsFromNativeParams(AProcess, AParamNames, AParams));
		}
		
		/// <summary>Closes the given cursor.</summary>
		public void CloseCursor(IServerCursor ACursor)
		{
			IServerExpressionPlan LPlan = ACursor.Plan;
			LPlan.Close(ACursor);
			LPlan.Process.UnprepareExpression(LPlan);
		}
		
		#endregion
		
		#region OpenDataView
		
		private DataView OpenDataView(string AExpression, DataSetState AInitialState, bool AIsReadOnly)
		{
			DataView LDataView = new DataView();
			try
			{
				LDataView.Session = this;
				LDataView.Expression = AExpression;
				LDataView.IsReadOnly = AIsReadOnly;
				LDataView.Open(AInitialState);
				return LDataView;
			}
			catch
			{
				LDataView.Dispose();
				throw;
			}
		}

		/// <summary> Opens a DataView on this session using the specified expression. </summary>
		/// <param name="AExpression"></param>
		/// <returns></returns>
		public DataView OpenDataView(string AExpression)
		{
			return OpenDataView(AExpression, DataSetState.Browse, false);
		}

		/// <summary> Opens a DataView on this session using the specified expression and opened in the specified state. </summary>
		/// <param name="AExpression"></param>
		/// <param name="AInitialState"></param>
		/// <returns></returns>
		public DataView OpenDataView(string AExpression, DataSetState AInitialState)
		{
			return OpenDataView(AExpression, AInitialState, false);
		}

		/// <summary> Opens a read-only DataView on this session using the specified expression. </summary>
		/// <param name="AExpression"></param>
		/// <returns></returns>
		/// <remarks>This opens up a slightly faster database cursor than the read/write OpenDataView call so use this one if you are just retrieving data.</remarks>
		public DataView OpenReadOnlyDataView(string AExpression)
		{
			return OpenDataView(AExpression, DataSetState.Browse, true);
		}
		
		#endregion

		#region Alias
		
		#if !SILVERLIGHT
		// AliasName
		private string FAliasName = String.Empty;
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
			get { return FAliasName; } 
			set 
			{ 
				CheckInactive();
				if (FAliasName != value)
				{
					InternalSetAlias(AliasManager.GetAlias(value));
					FAliasName = value;
				}
			}
		}
		#endif
		
		private void InternalSetAlias(ServerAlias AAlias)
		{
			FAlias = AAlias;
			if (FAlias != null)
				SessionInfo = (SessionInfo)FAlias.SessionInfo.Clone();
		}
		
		private void CheckAlias()
		{
			if (FAlias == null)
				throw new ClientException(ClientException.Codes.NoServerAliasSpecified);
		}
		
		// Alias
		private ServerAlias FAlias;
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
				return FAlias;
			}
			set
			{
				CheckInactive();
				if (FAlias != value)
				{
					InternalSetAlias(value);
					#if !SILVERLIGHT
					FAliasName = String.Empty;
					#endif
				}
			}
		}
		
		#endregion
	}
}