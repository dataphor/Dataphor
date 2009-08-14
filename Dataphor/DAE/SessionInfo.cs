/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alphora.Dataphor.DAE
{
    /// <nodoc/>
	public enum QueryLanguage { D4, RealSQL }
	
    /// <summary> Contains settings relevant to a server session. </summary>
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
	public class SessionInfo : ICloneable, ISerializable
    {
		public const int CDefaultFetchCount = 20;
		public const string CDefaultUserName = "Admin";
		
        public SessionInfo() : base(){}
        public SessionInfo(string AUserID, string APassword) : base()
        {
			UserID = AUserID;
			Password = APassword;
        }
        
        public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName) : base()
        {
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
        }
        
        public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ASessionTracingEnabled) : base()
        {
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
			SessionTracingEnabled = ASessionTracingEnabled;
        }

		public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ASessionTracingEnabled, bool ADefaultUseDTC) : base()
		{
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
			SessionTracingEnabled = ASessionTracingEnabled;
			FDefaultUseDTC = ADefaultUseDTC;
		}
        
		public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ASessionTracingEnabled, bool ADefaultUseDTC, QueryLanguage ALanguage) : base()
		{
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
			SessionTracingEnabled = ASessionTracingEnabled;
			FDefaultUseDTC = ADefaultUseDTC;
			FLanguage = ALanguage;
		}
        
		public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ASessionTracingEnabled, bool ADefaultUseDTC, QueryLanguage ALanguage, int AFetchCount) : base()
		{
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
			SessionTracingEnabled = ASessionTracingEnabled;
			FDefaultUseDTC = ADefaultUseDTC;
			FLanguage = ALanguage;
			FFetchCount = AFetchCount;
		}
        
        // UserID
        private string FUserID = CDefaultUserName;
        /// <summary>The user ID used to login to the Dataphor Server.</summary>
        /// <remarks>The Dataphor Server user ID of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
        [System.ComponentModel.DefaultValue(CDefaultUserName)]
        [System.ComponentModel.Description("User ID used to login to the Dataphor Server.  The default login for the Dataphor Server is Admin with a blank password.")]
        public string UserID
        {
			get { return FUserID; }
			set { FUserID = value == null ? String.Empty : value; }
		}
        
        // Password
        private string FPassword = String.Empty;
        /// <summary>The password used to login to the Dataphor Server.</summary>
        /// <remarks>The password for the Dataphor Server user of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
        [System.ComponentModel.DefaultValue("")]
        [System.ComponentModel.Description("Password used to login to the Dataphor Server.  The default login for the Dataphor Server is Admin with a blank password.")]
        [System.ComponentModel.TypeConverter(typeof(PasswordConverter))]
        [System.ComponentModel.Editor("Alphora.Dataphor.DAE.Client.Controls.Design.PasswordEditor,Alphora.Dataphor.DAE.Client.Controls", typeof(System.Drawing.Design.UITypeEditor))]
        [BOP.Publish(BOP.PublishMethod.None)]
        public string Password
        {
			get { return FPassword; }
			set { FPassword = value == null ? String.Empty : value; }
		}

		[System.ComponentModel.Browsable(false)]
		public string UnstructuredData
		{
			get { return Schema.SecurityUtility.EncryptPassword(FPassword); }
			set { FPassword = Schema.SecurityUtility.DecryptPassword(value); }
		}
		
		// HostName
		private string FHostName = String.Empty;
		[System.ComponentModel.Browsable(false)]
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value == null ? String.Empty : value; }
		}
		
		// CatalogCacheName
		private string FCatalogCacheName = String.Empty;
		[System.ComponentModel.Browsable(false)]
		public string CatalogCacheName
		{
			get { return FCatalogCacheName; }
			set { FCatalogCacheName = value == null ? String.Empty : value; }
		}
        
        // DefaultLibraryName
        private string FDefaultLibraryName = String.Empty;
		/// <summary>Determines the default library for the session. If specified, the current library for the session is initially set to this value.</summary>
		/// <remarks>The current library is used when resolving or creating catalog objects. The default value is blank. </remarks>
		[System.ComponentModel.DefaultValue("")]
		[System.ComponentModel.Description("Determines the default library for the session. If specified, the current library for the session is initially set to this value.")]
        public string DefaultLibraryName
        {
			get { return FDefaultLibraryName; }
			set { FDefaultLibraryName = value == null ? String.Empty : value; }
        }

		private bool FSessionTracingEnabled;
		/// <summary> Determines whether the server logs trace information for session based events. </summary>
		/// <remarks> Defaults to false. </remarks>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Whether or not to enable trace logging of server session events.")]
		[System.ComponentModel.Browsable(false)]
		public bool SessionTracingEnabled
		{
			get { return FSessionTracingEnabled; }
			set { FSessionTracingEnabled = value; }
		}
		
		private bool FDefaultUseDTC = false;
		/// <summary> Determines the default UseDTC setting for processes started on this session. Defaults to false.</summary>
		/// <remarks> Defaults to false. </remarks>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines the default UseDTC setting for processes started on this session. Defaults to false.")]
		public bool DefaultUseDTC
		{
			get { return FDefaultUseDTC; }
			set { FDefaultUseDTC = value; }
		}
		
		// DefaultUseImplicitTransactions
		private bool FDefaultUseImplicitTransactions = true;
		/// <summary>Determines the default UseImplicitTransactions setting for processes started on this session. Defaults to true.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines the default UseImplicitTransactions setting for processes started on this session. Defaults to true.")]
		public bool DefaultUseImplicitTransactions
		{
			get { return FDefaultUseImplicitTransactions; }
			set { FDefaultUseImplicitTransactions = value; }
		}

		private QueryLanguage FLanguage = QueryLanguage.D4;
		/// <summary> Determines which query language will be used to interpret statements and expressions prepared on the session. </summary>
		[System.ComponentModel.Browsable(false)]
		[System.ComponentModel.DefaultValue(QueryLanguage.D4)]
		[System.ComponentModel.Description("Determines which query language will be used to interpret statements and expressions prepared on the session.")]
		#if DAC
		[System.ComponentModel.Browsable(false)]
		#endif
		public QueryLanguage Language
		{
			get { return FLanguage; }
			set { FLanguage = value; }
		}
		
		private int FFetchCount = CDefaultFetchCount;
		/// <summary>Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.</summary>
		[System.ComponentModel.DefaultValue(CDefaultFetchCount)]
		[System.ComponentModel.Description("Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.")]
		public int FetchCount
		{
			get { return FFetchCount; }
			set { FFetchCount = value; }
		}
		
		private bool FFetchAtOpen = false;
		/// <summary>Determines whether or not the CLI will fetch as part of the open call.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the CLI will fetch as part of the open call.")]
		public bool FetchAtOpen
		{
			get { return FFetchAtOpen; }
			set { FFetchAtOpen = value; }
		}
		
		private IsolationLevel FDefaultIsolationLevel = IsolationLevel.CursorStability;
		/// <summary>Determines the default isolation level setting for processes started on this session.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level setting for processes started on this session.")]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FDefaultIsolationLevel; }
			set { FDefaultIsolationLevel = value; }
		}
		
		private int FDefaultMaxStackDepth = DAE.Server.Server.CDefaultMaxStackDepth;
		/// <summary>Determines the default maximum stack depth for processes on this session.</summary>
		[System.ComponentModel.DefaultValue(DAE.Server.Server.CDefaultMaxStackDepth)]
		[System.ComponentModel.Description("Determines the default maximum stack depth for processes on this session.")]
		public int DefaultMaxStackDepth
		{
			get { return FDefaultMaxStackDepth; }
			set { FDefaultMaxStackDepth = value; }
		}
		
		private int FDefaultMaxCallDepth = DAE.Server.Server.CDefaultMaxCallDepth;
		/// <summary>Determines the default maximum call depth for processes on this session.</summary>
		[System.ComponentModel.DefaultValue(DAE.Server.Server.CDefaultMaxCallDepth)]
		[System.ComponentModel.Description("Determines the default maximum call depth for processes on this session.")]
		public int DefaultMaxCallDepth
		{
			get { return FDefaultMaxCallDepth; }
			set { FDefaultMaxCallDepth = value; }
		}
		
		private bool FUsePlanCache = true;
		/// <summary>Detetrmines whether or not to use the server plan cache on this session.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether or not to use the server plan cache on this session.")]
		public bool UsePlanCache
		{
			get { return FUsePlanCache; }
			set { FUsePlanCache = value; }
		}
		
		private bool FShouldEmitIL = false;
		/// <summary>Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.")]
		public bool ShouldEmitIL
		{
			get { return FShouldEmitIL; }
			set { FShouldEmitIL = value; }
		}
		
		private int FDebuggerID;
		/// <summary>
		/// The ID of the debugger to use to debug processes on this session, if any.
		/// </summary>
		[System.ComponentModel.DefaultValue(0)]
		[System.ComponentModel.Description("The ID of the debugger to use to debug processes on this session, if any.")]
		public int DebuggerID
		{
			get { return FDebuggerID; }
			set { FDebuggerID = value; }
		}
		
		public virtual object Clone()
		{	
			SessionInfo LSessionInfo = new SessionInfo(FUserID, FPassword);
			LSessionInfo.HostName = FHostName;
			LSessionInfo.CatalogCacheName = FCatalogCacheName;
			LSessionInfo.DefaultLibraryName = FDefaultLibraryName;
			LSessionInfo.SessionTracingEnabled = FSessionTracingEnabled;
			LSessionInfo.DefaultUseDTC = FDefaultUseDTC;
			LSessionInfo.DefaultUseImplicitTransactions = FDefaultUseImplicitTransactions;
			LSessionInfo.Language = FLanguage;
			LSessionInfo.FetchCount = FFetchCount;
			LSessionInfo.FetchAtOpen = FFetchAtOpen;
			LSessionInfo.DefaultIsolationLevel = FDefaultIsolationLevel;
			LSessionInfo.DefaultMaxStackDepth = FDefaultMaxStackDepth;
			LSessionInfo.DefaultMaxCallDepth = FDefaultMaxCallDepth;
			LSessionInfo.UsePlanCache = FUsePlanCache;
			LSessionInfo.ShouldEmitIL = FShouldEmitIL;
			LSessionInfo.DebuggerID = FDebuggerID;
			//LSessionInfo.PlanCacheSize = FPlanCacheSize;
			return LSessionInfo;
		}

		#region ISerializable Members

		[SecurityPermissionAttribute(SecurityAction.Demand,SerializationFormatter=true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("UserID", UserID);
			info.AddValue("UnstructuredData", UnstructuredData);
			info.AddValue("HostName", HostName);
			info.AddValue("CatalogCacheName", CatalogCacheName);
			info.AddValue("DefaultLibraryName", DefaultLibraryName);
			info.AddValue("SessionTracingEnabled", SessionTracingEnabled);
			info.AddValue("DefaultUseDTC", DefaultUseDTC);
			info.AddValue("DefaultUseImplicitTransactions", DefaultUseImplicitTransactions);
			info.AddValue("Language", Language);
			info.AddValue("FetchCount", FetchCount);
			info.AddValue("FetchAtOpen", FetchAtOpen);
			info.AddValue("DefaultIsolationLevel", DefaultIsolationLevel);
			info.AddValue("DefaultMaxStackDepth", DefaultMaxStackDepth);
			info.AddValue("DefaultMaxCallDepth", DefaultMaxCallDepth);
			info.AddValue("UsePlanCache", UsePlanCache);
			info.AddValue("ShouldEmitIL", ShouldEmitIL);
			info.AddValue("DebuggerID", DebuggerID);
		}
		
		protected SessionInfo(SerializationInfo info, StreamingContext context)
		{
			UserID = info.GetString("UserID");
			UnstructuredData = info.GetString("UnstructuredData");
			HostName = info.GetString("HostName");
			CatalogCacheName = info.GetString("CatalogCacheName");
			DefaultLibraryName = info.GetString("DefaultLibraryName");
			SessionTracingEnabled = info.GetBoolean("SessionTracingEnabled");
			DefaultUseDTC = info.GetBoolean("DefaultUseDTC");
			DefaultUseImplicitTransactions = info.GetBoolean("DefaultUseImplicitTransactions");
			Language = (QueryLanguage)info.GetValue("Language", typeof(QueryLanguage));
			FetchCount = info.GetInt32("FetchCount");
			FetchAtOpen = info.GetBoolean("FetchAtOpen");
			DefaultIsolationLevel = (IsolationLevel)info.GetValue("DefaultIsolationLevel", typeof(IsolationLevel));
			DefaultMaxStackDepth = info.GetInt32("DefaultMaxStackDepth");
			DefaultMaxCallDepth = info.GetInt32("DefaultMaxCallDepth");
			UsePlanCache = info.GetBoolean("UsePlanCache");
			ShouldEmitIL = info.GetBoolean("ShouldEmitIL");
			DebuggerID = info.GetInt32("DebuggerID");
		}

		#endregion
	}
    
    [Serializable]
 	[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
	public class ProcessInfo : ICloneable
	{
		public ProcessInfo() : base() {}
		public ProcessInfo(SessionInfo ASessionInfo) : base()
		{
			FUseDTC = ASessionInfo.DefaultUseDTC;
			FUseImplicitTransactions = ASessionInfo.DefaultUseImplicitTransactions;
			FDefaultIsolationLevel = ASessionInfo.DefaultIsolationLevel;
			FFetchCount = ASessionInfo.FetchCount;
			FFetchAtOpen = ASessionInfo.FetchAtOpen;
			FDebuggerID = ASessionInfo.DebuggerID;
		}
		
		// UseImplicitTransactions
		private bool FUseImplicitTransactions = true;
		/// <summary>Determines whether a transaction will be implicitly started when a call is made on this process.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether a transaction will be implicitly started when a call is made on this process.")]
		public bool UseImplicitTransactions
		{
			get { return FUseImplicitTransactions; }
			set { FUseImplicitTransactions = value; }
		}

		private bool FUseDTC = false;
		/// <summary> Determines whether the Microsoft Distributed Transaction Coordinator will be used to control distributed transactions for this process. </summary>
		/// <remarks>
		/// The Microsoft Distributed Transaction Coordinator (DTC) may only be used when the Dataphor Server is running on Microsoft Windows 2000 or higher.  
		/// This value may not be set while there are active transactions on the process (an exception will be thrown).  Refer to the Dataphor Developer's Guide 
		/// for a complete discussion of distributed transaction support in the Dataphor Server.  The default value for this setting is false.
		/// </remarks>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether the Microsoft Distributed Transaction Coordinator will be used to control distributed transactions for this session.")]
		public bool UseDTC
		{
			get { return FUseDTC; }
			set { FUseDTC = value; }
		}
		
		private IsolationLevel FDefaultIsolationLevel = IsolationLevel.CursorStability;
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level for transactions on this process.")]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FDefaultIsolationLevel; }
			set { FDefaultIsolationLevel = value; }
		}
		
		private int FFetchCount = SessionInfo.CDefaultFetchCount;
		/// <summary>Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.</summary>
		[System.ComponentModel.DefaultValue(SessionInfo.CDefaultFetchCount)]
		[System.ComponentModel.Description("Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.")]
		public int FetchCount
		{
			get { return FFetchCount; }
			set { FFetchCount = value; }
		}
		
		private bool FFetchAtOpen = false;
		/// <summary>Determines whether or not the CLI will fetch as part of the open call.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the CLI will fetch as part of the open call.")]
		public bool FetchAtOpen
		{
			get { return FFetchAtOpen; }
			set { FFetchAtOpen = value; }
		}
		
		private bool FSuppressWarnings;
		/// <summary>Determines whether or not the compiler will report warnings encountered when compiling statements on this process.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the compiler will report warnings encountered when compiling statements on this process.")]
		public bool SuppressWarnings
		{
			get { return FSuppressWarnings; }
			set { FSuppressWarnings = value; }
		}
		
		private int FDebuggerID;
		/// <summary>
		/// The ID of the debugger to use to debug this process, if any.
		/// </summary>
		[System.ComponentModel.DefaultValue(0)]
		[System.ComponentModel.Description("The ID of the debugger to use to debug this process, if any.")]
		public int DebuggerID
		{
			get { return FDebuggerID; }
			set { FDebuggerID = value; }
		}
		
		public virtual object Clone()
		{
			ProcessInfo LProcessInfo = new ProcessInfo();
			LProcessInfo.UseDTC = FUseDTC;
			LProcessInfo.UseImplicitTransactions = FUseImplicitTransactions;
			LProcessInfo.DefaultIsolationLevel = FDefaultIsolationLevel;
			LProcessInfo.FetchCount = FFetchCount;
			LProcessInfo.FetchAtOpen = FFetchAtOpen;
			LProcessInfo.SuppressWarnings = FSuppressWarnings;
			LProcessInfo.DebuggerID = FDebuggerID;
			return LProcessInfo;
		}
    }

	/// <nodoc/>
	public class PasswordConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType.Equals(typeof(String));
		}
		
		public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			return "************";
		}
		
		public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.Equals(typeof(String));
		}

		public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			return "************";
		}
	}
}