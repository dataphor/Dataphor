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
	[DataContract]
	#if !SILVERLIGHT
	[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
	#endif
	public class SessionInfo
    {
		public const int DefaultFetchCount = 20;
		public const string DefaultUserName = "Admin";
		
        public SessionInfo() : base(){}
        public SessionInfo(string userID, string password) : base()
        {
			UserID = userID;
			Password = password;
        }
        
        public SessionInfo(string userID, string password, string defaultLibraryName) : base()
        {
			UserID = userID;
			Password = password;
			DefaultLibraryName = defaultLibraryName;
        }
        
		public SessionInfo(string userID, string password, string defaultLibraryName, bool defaultUseDTC) : base()
		{
			UserID = userID;
			Password = password;
			DefaultLibraryName = defaultLibraryName;
			_defaultUseDTC = defaultUseDTC;
		}
        
		public SessionInfo(string userID, string password, string defaultLibraryName, bool defaultUseDTC, QueryLanguage language) : base()
		{
			UserID = userID;
			Password = password;
			DefaultLibraryName = defaultLibraryName;
			_defaultUseDTC = defaultUseDTC;
			_language = language;
		}
        
		public SessionInfo(string userID, string password, string defaultLibraryName, bool defaultUseDTC, QueryLanguage language, int fetchCount) : base()
		{
			UserID = userID;
			Password = password;
			DefaultLibraryName = defaultLibraryName;
			_defaultUseDTC = defaultUseDTC;
			_language = language;
			_fetchCount = fetchCount;
		}
        
        // UserID
        private string _userID = DefaultUserName;
        /// <summary>The user ID used to login to the Dataphor Server.</summary>
        /// <remarks>The Dataphor Server user ID of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
        [System.ComponentModel.DefaultValue(DefaultUserName)]
        [System.ComponentModel.Description("User ID used to login to the Dataphor Server.  The default login for the Dataphor Server is Admin with a blank password.")]
		[DataMember]
        public string UserID
        {
			get { return _userID; }
			set { _userID = value == null ? String.Empty : value; }
		}
        
        // Password
        private string _password = String.Empty;
        /// <summary>The password used to login to the Dataphor Server.</summary>
        /// <remarks>The password for the Dataphor Server user of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
        [System.ComponentModel.DefaultValue("")]
        [System.ComponentModel.Description("Password used to login to the Dataphor Server.  The default login for the Dataphor Server is Admin with a blank password.")]
        [System.ComponentModel.TypeConverter(typeof(PasswordConverter))]
        [System.ComponentModel.Editor("Alphora.Dataphor.DAE.Client.Controls.Design.PasswordEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
        [BOP.Publish(BOP.PublishMethod.None)]
        public string Password
        {
			get { return _password; }
			set { _password = value == null ? String.Empty : value; }
		}

		[System.ComponentModel.Browsable(false)]
		[DataMember]
		public string UnstructuredData
		{
			get { return Schema.SecurityUtility.EncryptPassword(_password); }
			set { _password = Schema.SecurityUtility.DecryptPassword(value); }
		}
		
		// HostName
		private string _hostName = String.Empty;
		[System.ComponentModel.Browsable(false)]
		[DataMember]
		public string HostName
		{
			get { return _hostName; }
			set { _hostName = value == null ? String.Empty : value; }
		}
		
		// Environment
		private string _environment = String.Empty;
		[System.ComponentModel.Browsable(false)]
		[DataMember]
		public string Environment
		{
			get { return _environment; }
			set { _environment = value == null ? String.Empty : value; }
		}
		
		// CatalogCacheName
		private string _catalogCacheName = String.Empty;
		[System.ComponentModel.Browsable(false)]
		[DataMember]
		public string CatalogCacheName
		{
			get { return _catalogCacheName; }
			set { _catalogCacheName = value == null ? String.Empty : value; }
		}
        
        // DefaultLibraryName
        private string _defaultLibraryName = String.Empty;
		/// <summary>Determines the default library for the session. If specified, the current library for the session is initially set to this value.</summary>
		/// <remarks>The current library is used when resolving or creating catalog objects. The default value is blank. </remarks>
		[System.ComponentModel.DefaultValue("")]
		[System.ComponentModel.Description("Determines the default library for the session. If specified, the current library for the session is initially set to this value.")]
		[DataMember]
        public string DefaultLibraryName
        {
			get { return _defaultLibraryName; }
			set { _defaultLibraryName = value == null ? String.Empty : value; }
        }

		private bool _defaultUseDTC = false;
		/// <summary> Determines the default UseDTC setting for processes started on this session. Defaults to false.</summary>
		/// <remarks> Defaults to false. </remarks>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines the default UseDTC setting for processes started on this session. Defaults to false.")]
		[DataMember]
		public bool DefaultUseDTC
		{
			get { return _defaultUseDTC; }
			set { _defaultUseDTC = value; }
		}
		
		// DefaultUseImplicitTransactions
		private bool _defaultUseImplicitTransactions = true;
		/// <summary>Determines the default UseImplicitTransactions setting for processes started on this session. Defaults to true.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines the default UseImplicitTransactions setting for processes started on this session. Defaults to true.")]
		[DataMember]
		public bool DefaultUseImplicitTransactions
		{
			get { return _defaultUseImplicitTransactions; }
			set { _defaultUseImplicitTransactions = value; }
		}

		private QueryLanguage _language = QueryLanguage.D4;
		/// <summary> Determines which query language will be used to interpret statements and expressions prepared on the session. </summary>
		[System.ComponentModel.Browsable(false)]
		[System.ComponentModel.DefaultValue(QueryLanguage.D4)]
		[System.ComponentModel.Description("Determines which query language will be used to interpret statements and expressions prepared on the session.")]
		#if DAC
		[System.ComponentModel.Browsable(false)]
		#endif
		[DataMember]
		public QueryLanguage Language
		{
			get { return _language; }
			set { _language = value; }
		}
		
		private int _fetchCount = DefaultFetchCount;
		/// <summary>Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.</summary>
		[System.ComponentModel.DefaultValue(DefaultFetchCount)]
		[System.ComponentModel.Description("Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.")]
		[DataMember]
		public int FetchCount
		{
			get { return _fetchCount; }
			set { _fetchCount = value; }
		}
		
		private bool _fetchAtOpen = false;
		/// <summary>Determines whether or not the CLI will fetch as part of the open call.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the CLI will fetch as part of the open call.")]
		[DataMember]
		public bool FetchAtOpen
		{
			get { return _fetchAtOpen; }
			set { _fetchAtOpen = value; }
		}
		
		private IsolationLevel _defaultIsolationLevel = IsolationLevel.CursorStability;
		/// <summary>Determines the default isolation level setting for processes started on this session.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level setting for processes started on this session.")]
		[DataMember]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return _defaultIsolationLevel; }
			set { _defaultIsolationLevel = value; }
		}
		
		private int _defaultMaxStackDepth = DAE.Server.Engine.DefaultMaxStackDepth;
		/// <summary>Determines the default maximum stack depth for processes on this session.</summary>
		[System.ComponentModel.DefaultValue(DAE.Server.Engine.DefaultMaxStackDepth)]
		[System.ComponentModel.Description("Determines the default maximum stack depth for processes on this session.")]
		[DataMember]
		public int DefaultMaxStackDepth
		{
			get { return _defaultMaxStackDepth; }
			set { _defaultMaxStackDepth = value; }
		}
		
		private int _defaultMaxCallDepth = DAE.Server.Engine.DefaultMaxCallDepth;
		/// <summary>Determines the default maximum call depth for processes on this session.</summary>
		[System.ComponentModel.DefaultValue(DAE.Server.Engine.DefaultMaxCallDepth)]
		[System.ComponentModel.Description("Determines the default maximum call depth for processes on this session.")]
		[DataMember]
		public int DefaultMaxCallDepth
		{
			get { return _defaultMaxCallDepth; }
			set { _defaultMaxCallDepth = value; }
		}
		
		private bool _usePlanCache = true;
		/// <summary>Detetrmines whether or not to use the server plan cache on this session.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether or not to use the server plan cache on this session.")]
		[DataMember]
		public bool UsePlanCache
		{
			get { return _usePlanCache; }
			set { _usePlanCache = value; }
		}
		
		private bool _shouldEmitIL = false;
		/// <summary>Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.")]
		[DataMember]
		public bool ShouldEmitIL
		{
			get { return _shouldEmitIL; }
			set { _shouldEmitIL = value; }
		}

		private bool _shouldElaborate = true;
		/// <summary>Determines whether or not the compiler will include elaboration information by default in prepared plans.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether or not the compiler will include elaboration information by default in prepared plans.")]
		[DataMember]
		public bool ShouldElaborate
		{
			get { return _shouldElaborate; }
			set { _shouldElaborate = value; }
		}
		
		public virtual object Clone()
		{	
			SessionInfo sessionInfo = new SessionInfo(_userID, _password);
			sessionInfo.HostName = _hostName;
			sessionInfo.Environment = _environment;
			sessionInfo.CatalogCacheName = _catalogCacheName;
			sessionInfo.DefaultLibraryName = _defaultLibraryName;
			sessionInfo.DefaultUseDTC = _defaultUseDTC;
			sessionInfo.DefaultUseImplicitTransactions = _defaultUseImplicitTransactions;
			sessionInfo.Language = _language;
			sessionInfo.FetchCount = _fetchCount;
			sessionInfo.FetchAtOpen = _fetchAtOpen;
			sessionInfo.DefaultIsolationLevel = _defaultIsolationLevel;
			sessionInfo.DefaultMaxStackDepth = _defaultMaxStackDepth;
			sessionInfo.DefaultMaxCallDepth = _defaultMaxCallDepth;
			sessionInfo.UsePlanCache = _usePlanCache;
			sessionInfo.ShouldEmitIL = _shouldEmitIL;
			sessionInfo.ShouldElaborate = _shouldElaborate;
			//LSessionInfo.PlanCacheSize = FPlanCacheSize;
			return sessionInfo;
		}
	}
    
    [DataContract]
    #if !SILVERLIGHT
 	[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
 	#endif
	public class ProcessInfo
	{
		public ProcessInfo() : base() {}
		public ProcessInfo(SessionInfo sessionInfo) : base()
		{
			_useDTC = sessionInfo.DefaultUseDTC;
			_useImplicitTransactions = sessionInfo.DefaultUseImplicitTransactions;
			_defaultIsolationLevel = sessionInfo.DefaultIsolationLevel;
			_language = sessionInfo.Language;
			_fetchCount = sessionInfo.FetchCount;
			_fetchAtOpen = sessionInfo.FetchAtOpen;
		}
		
		// UseImplicitTransactions
		private bool _useImplicitTransactions = true;
		/// <summary>Determines whether a transaction will be implicitly started when a call is made on this process.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether a transaction will be implicitly started when a call is made on this process.")]
		[DataMember]
		public bool UseImplicitTransactions
		{
			get { return _useImplicitTransactions; }
			set { _useImplicitTransactions = value; }
		}

		private bool _useDTC = false;
		/// <summary> Determines whether the Microsoft Distributed Transaction Coordinator will be used to control distributed transactions for this process. </summary>
		/// <remarks>
		/// The Microsoft Distributed Transaction Coordinator (DTC) may only be used when the Dataphor Server is running on Microsoft Windows 2000 or higher.  
		/// This value may not be set while there are active transactions on the process (an exception will be thrown).  Refer to the Dataphor Developer's Guide 
		/// for a complete discussion of distributed transaction support in the Dataphor Server.  The default value for this setting is false.
		/// </remarks>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether the Microsoft Distributed Transaction Coordinator will be used to control distributed transactions for this process.")]
		[DataMember]
		public bool UseDTC
		{
			get { return _useDTC; }
			set { _useDTC = value; }
		}
		
		private IsolationLevel _defaultIsolationLevel = IsolationLevel.CursorStability;
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level for transactions on this process.")]
		[DataMember]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return _defaultIsolationLevel; }
			set { _defaultIsolationLevel = value; }
		}
		
		private QueryLanguage _language = QueryLanguage.D4;
		/// <summary> Determines which query language will be used to interpret statements and expressions prepared on the process. </summary>
		[System.ComponentModel.Browsable(false)]
		[System.ComponentModel.DefaultValue(QueryLanguage.D4)]
		[System.ComponentModel.Description("Determines which query language will be used to interpret statements and expressions prepared on the process.")]
		#if DAC
		[System.ComponentModel.Browsable(false)]
		#endif
		[DataMember]
		public QueryLanguage Language
		{
			get { return _language; }
			set { _language = value; }
		}
		
		private int _fetchCount = SessionInfo.DefaultFetchCount;
		/// <summary>Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.</summary>
		[System.ComponentModel.DefaultValue(SessionInfo.DefaultFetchCount)]
		[System.ComponentModel.Description("Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.")]
		[DataMember]
		public int FetchCount
		{
			get { return _fetchCount; }
			set { _fetchCount = value; }
		}
		
		private bool _fetchAtOpen = false;
		/// <summary>Determines whether or not the CLI will fetch as part of the open call.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the CLI will fetch as part of the open call.")]
		[DataMember]
		public bool FetchAtOpen
		{
			get { return _fetchAtOpen; }
			set { _fetchAtOpen = value; }
		}
		
		private bool _suppressWarnings;
		/// <summary>Determines whether or not the compiler will report warnings encountered when compiling statements on this process.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the compiler will report warnings encountered when compiling statements on this process.")]
		[DataMember]
		public bool SuppressWarnings
		{
			get { return _suppressWarnings; }
			set { _suppressWarnings = value; }
		}
		
		public virtual object Clone()
		{
			ProcessInfo processInfo = new ProcessInfo();
			processInfo.UseDTC = _useDTC;
			processInfo.UseImplicitTransactions = _useImplicitTransactions;
			processInfo.DefaultIsolationLevel = _defaultIsolationLevel;
			processInfo.Language = _language;
			processInfo.FetchCount = _fetchCount;
			processInfo.FetchAtOpen = _fetchAtOpen;
			processInfo.SuppressWarnings = _suppressWarnings;
			return processInfo;
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