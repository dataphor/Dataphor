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
        
		public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ADefaultUseDTC) : base()
		{
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
			FDefaultUseDTC = ADefaultUseDTC;
		}
        
		public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ADefaultUseDTC, QueryLanguage ALanguage) : base()
		{
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
			FDefaultUseDTC = ADefaultUseDTC;
			FLanguage = ALanguage;
		}
        
		public SessionInfo(string AUserID, string APassword, string ADefaultLibraryName, bool ADefaultUseDTC, QueryLanguage ALanguage, int AFetchCount) : base()
		{
			UserID = AUserID;
			Password = APassword;
			DefaultLibraryName = ADefaultLibraryName;
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
		[DataMember]
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
        [System.ComponentModel.Editor("Alphora.Dataphor.DAE.Client.Controls.Design.PasswordEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
        [BOP.Publish(BOP.PublishMethod.None)]
        public string Password
        {
			get { return FPassword; }
			set { FPassword = value == null ? String.Empty : value; }
		}

		[System.ComponentModel.Browsable(false)]
		[DataMember]
		public string UnstructuredData
		{
			get { return Schema.SecurityUtility.EncryptPassword(FPassword); }
			set { FPassword = Schema.SecurityUtility.DecryptPassword(value); }
		}
		
		// HostName
		private string FHostName = String.Empty;
		[System.ComponentModel.Browsable(false)]
		[DataMember]
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value == null ? String.Empty : value; }
		}
		
		// CatalogCacheName
		private string FCatalogCacheName = String.Empty;
		[System.ComponentModel.Browsable(false)]
		[DataMember]
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
		[DataMember]
        public string DefaultLibraryName
        {
			get { return FDefaultLibraryName; }
			set { FDefaultLibraryName = value == null ? String.Empty : value; }
        }

		private bool FDefaultUseDTC = false;
		/// <summary> Determines the default UseDTC setting for processes started on this session. Defaults to false.</summary>
		/// <remarks> Defaults to false. </remarks>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines the default UseDTC setting for processes started on this session. Defaults to false.")]
		[DataMember]
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
		[DataMember]
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
		[DataMember]
		public QueryLanguage Language
		{
			get { return FLanguage; }
			set { FLanguage = value; }
		}
		
		private int FFetchCount = CDefaultFetchCount;
		/// <summary>Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.</summary>
		[System.ComponentModel.DefaultValue(CDefaultFetchCount)]
		[System.ComponentModel.Description("Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.")]
		[DataMember]
		public int FetchCount
		{
			get { return FFetchCount; }
			set { FFetchCount = value; }
		}
		
		private bool FFetchAtOpen = false;
		/// <summary>Determines whether or not the CLI will fetch as part of the open call.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the CLI will fetch as part of the open call.")]
		[DataMember]
		public bool FetchAtOpen
		{
			get { return FFetchAtOpen; }
			set { FFetchAtOpen = value; }
		}
		
		private IsolationLevel FDefaultIsolationLevel = IsolationLevel.CursorStability;
		/// <summary>Determines the default isolation level setting for processes started on this session.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level setting for processes started on this session.")]
		[DataMember]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FDefaultIsolationLevel; }
			set { FDefaultIsolationLevel = value; }
		}
		
		private int FDefaultMaxStackDepth = DAE.Server.Engine.CDefaultMaxStackDepth;
		/// <summary>Determines the default maximum stack depth for processes on this session.</summary>
		[System.ComponentModel.DefaultValue(DAE.Server.Engine.CDefaultMaxStackDepth)]
		[System.ComponentModel.Description("Determines the default maximum stack depth for processes on this session.")]
		[DataMember]
		public int DefaultMaxStackDepth
		{
			get { return FDefaultMaxStackDepth; }
			set { FDefaultMaxStackDepth = value; }
		}
		
		private int FDefaultMaxCallDepth = DAE.Server.Engine.CDefaultMaxCallDepth;
		/// <summary>Determines the default maximum call depth for processes on this session.</summary>
		[System.ComponentModel.DefaultValue(DAE.Server.Engine.CDefaultMaxCallDepth)]
		[System.ComponentModel.Description("Determines the default maximum call depth for processes on this session.")]
		[DataMember]
		public int DefaultMaxCallDepth
		{
			get { return FDefaultMaxCallDepth; }
			set { FDefaultMaxCallDepth = value; }
		}
		
		private bool FUsePlanCache = true;
		/// <summary>Detetrmines whether or not to use the server plan cache on this session.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether or not to use the server plan cache on this session.")]
		[DataMember]
		public bool UsePlanCache
		{
			get { return FUsePlanCache; }
			set { FUsePlanCache = value; }
		}
		
		private bool FShouldEmitIL = false;
		/// <summary>Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.")]
		[DataMember]
		public bool ShouldEmitIL
		{
			get { return FShouldEmitIL; }
			set { FShouldEmitIL = value; }
		}
		
		public virtual object Clone()
		{	
			SessionInfo LSessionInfo = new SessionInfo(FUserID, FPassword);
			LSessionInfo.HostName = FHostName;
			LSessionInfo.CatalogCacheName = FCatalogCacheName;
			LSessionInfo.DefaultLibraryName = FDefaultLibraryName;
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
			//LSessionInfo.PlanCacheSize = FPlanCacheSize;
			return LSessionInfo;
		}
	}
    
    [DataContract]
    #if !SILVERLIGHT
 	[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
 	#endif
	public class ProcessInfo
	{
		public ProcessInfo() : base() {}
		public ProcessInfo(SessionInfo ASessionInfo) : base()
		{
			FUseDTC = ASessionInfo.DefaultUseDTC;
			FUseImplicitTransactions = ASessionInfo.DefaultUseImplicitTransactions;
			FDefaultIsolationLevel = ASessionInfo.DefaultIsolationLevel;
			FFetchCount = ASessionInfo.FetchCount;
			FFetchAtOpen = ASessionInfo.FetchAtOpen;
		}
		
		// UseImplicitTransactions
		private bool FUseImplicitTransactions = true;
		/// <summary>Determines whether a transaction will be implicitly started when a call is made on this process.</summary>
		[System.ComponentModel.DefaultValue(true)]
		[System.ComponentModel.Description("Determines whether a transaction will be implicitly started when a call is made on this process.")]
		[DataMember]
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
		[DataMember]
		public bool UseDTC
		{
			get { return FUseDTC; }
			set { FUseDTC = value; }
		}
		
		private IsolationLevel FDefaultIsolationLevel = IsolationLevel.CursorStability;
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level for transactions on this process.")]
		[DataMember]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FDefaultIsolationLevel; }
			set { FDefaultIsolationLevel = value; }
		}
		
		private int FFetchCount = SessionInfo.CDefaultFetchCount;
		/// <summary>Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.</summary>
		[System.ComponentModel.DefaultValue(SessionInfo.CDefaultFetchCount)]
		[System.ComponentModel.Description("Determines the number of rows which will be buffered by the CLI.  A FetchCount of 1 effectively disables row buffering.")]
		[DataMember]
		public int FetchCount
		{
			get { return FFetchCount; }
			set { FFetchCount = value; }
		}
		
		private bool FFetchAtOpen = false;
		/// <summary>Determines whether or not the CLI will fetch as part of the open call.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the CLI will fetch as part of the open call.")]
		[DataMember]
		public bool FetchAtOpen
		{
			get { return FFetchAtOpen; }
			set { FFetchAtOpen = value; }
		}
		
		private bool FSuppressWarnings;
		/// <summary>Determines whether or not the compiler will report warnings encountered when compiling statements on this process.</summary>
		[System.ComponentModel.DefaultValue(false)]
		[System.ComponentModel.Description("Determines whether or not the compiler will report warnings encountered when compiling statements on this process.")]
		[DataMember]
		public bool SuppressWarnings
		{
			get { return FSuppressWarnings; }
			set { FSuppressWarnings = value; }
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