/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.NativeCLI
{
    /// <summary>
	/// Contains settings relevant to a server session.
	/// </summary>
	[DataContract]
	public class NativeSessionInfo
    {
		public const int DefaultFetchCount = 20;
		public const string DefaultUserName = "Admin";
		public const int DefaultDefaultMaxStackDepth = 32767;
		public const int DefaultDefaultMaxCallDepth = 100;
		
		public NativeSessionInfo() { }
		
        // UserID
        private string _userID = DefaultUserName;
        /// <summary>The user ID used to login to the Dataphor Server.</summary>
        /// <remarks>The Dataphor Server user ID of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
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
        public string Password
        {
			get { return _password; }
			set { _password = value == null ? String.Empty : value; }
		}

		[DataMember]
		private string UnstructuredData
		{
			get { return SecurityUtility.EncryptPassword(_password); }
			set { _password = SecurityUtility.DecryptPassword(value); }
		}
		
		// HostName
		private string _hostName = String.Empty;
		[DataMember]
		public string HostName
		{
			get { return _hostName; }
			set { _hostName = value == null ? String.Empty : value; }
		}
		
        // DefaultLibraryName
        private string _defaultLibraryName = String.Empty;
		/// <summary>Determines the default library for the session. If specified, the current library for the session is initially set to this value.</summary>
		/// <remarks>The current library is used when resolving or creating catalog objects. The default value is blank. </remarks>
		[DataMember]
        public string DefaultLibraryName
        {
			get { return _defaultLibraryName; }
			set { _defaultLibraryName = value == null ? String.Empty : value; }
        }

		private bool _defaultUseDTC = false;
		/// <summary> Determines the default UseDTC setting for processes started on this session. Defaults to false.</summary>
		/// <remarks> Defaults to false. </remarks>
		[DataMember]
		public bool DefaultUseDTC
		{
			get { return _defaultUseDTC; }
			set { _defaultUseDTC = value; }
		}
		
		// DefaultUseImplicitTransactions
		private bool _defaultUseImplicitTransactions = true;
		/// <summary>Determines the default UseImplicitTransactions setting for processes started on this session. Defaults to true.</summary>
		[DataMember]
		public bool DefaultUseImplicitTransactions
		{
			get { return _defaultUseImplicitTransactions; }
			set { _defaultUseImplicitTransactions = value; }
		}

		private NativeIsolationLevel _defaultIsolationLevel = NativeIsolationLevel.Isolated;
		/// <summary>Determines the default isolation level setting for processes started on this session.</summary>
		[DataMember]
		public NativeIsolationLevel DefaultIsolationLevel
		{
			get { return _defaultIsolationLevel; }
			set { _defaultIsolationLevel = value; }
		}
		
		private int _defaultMaxStackDepth = DefaultDefaultMaxStackDepth;
		/// <summary>Determines the default maximum stack depth for processes on this session.</summary>
		[DataMember]
		public int DefaultMaxStackDepth
		{
			get { return _defaultMaxStackDepth; }
			set { _defaultMaxStackDepth = value; }
		}
		
		private int _defaultMaxCallDepth = DefaultDefaultMaxCallDepth;
		/// <summary>Determines the default maximum call depth for processes on this session.</summary>
		[DataMember]
		public int DefaultMaxCallDepth
		{
			get { return _defaultMaxCallDepth; }
			set { _defaultMaxCallDepth = value; }
		}
		
		private bool _usePlanCache = true;
		/// <summary>Detetrmines whether or not to use the server plan cache on this session.</summary>
		[DataMember]
		public bool UsePlanCache
		{
			get { return _usePlanCache; }
			set { _usePlanCache = value; }
		}
		
		private bool _shouldEmitIL = false;
		/// <summary>Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.</summary>
		[DataMember]
		public bool ShouldEmitIL
		{
			get { return _shouldEmitIL; }
			set { _shouldEmitIL = value; }
		}

		public NativeSessionInfo Copy()
		{
			return 
				new NativeSessionInfo()
				{
					UserID = this.UserID,
					Password = this.Password,
					HostName = this.HostName,
					DefaultLibraryName = this.DefaultLibraryName,
					DefaultUseDTC = this.DefaultUseDTC,
					DefaultUseImplicitTransactions = this.DefaultUseImplicitTransactions,
					DefaultIsolationLevel = this.DefaultIsolationLevel,
					DefaultMaxStackDepth = this.DefaultMaxStackDepth,
					DefaultMaxCallDepth = this.DefaultMaxCallDepth,
					UsePlanCache = this.UsePlanCache,
					ShouldEmitIL = this.ShouldEmitIL,
				};
		}
	}
}
