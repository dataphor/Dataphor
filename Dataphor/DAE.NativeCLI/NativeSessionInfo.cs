/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.NativeCLI
{
    /// <summary>
	/// Contains settings relevant to a server session.
	/// </summary>
	[Serializable]
	public class NativeSessionInfo : ISerializable
    {
		public const int CDefaultFetchCount = 20;
		public const string CDefaultUserName = "Admin";
		public const int CDefaultMaxStackDepth = 32767;
		public const int CDefaultMaxCallDepth = 1024;
		
		public NativeSessionInfo() { }
		
        // UserID
        private string FUserID = CDefaultUserName;
        /// <summary>The user ID used to login to the Dataphor Server.</summary>
        /// <remarks>The Dataphor Server user ID of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
        public string UserID
        {
			get { return FUserID; }
			set { FUserID = value == null ? String.Empty : value; }
		}
        
        // Password
        private string FPassword = String.Empty;
        /// <summary>The password used to login to the Dataphor Server.</summary>
        /// <remarks>The password for the Dataphor Server user of the session.  The default login for the Dataphor Server is Admin with a blank password.</remarks>
        public string Password
        {
			get { return FPassword; }
			set { FPassword = value == null ? String.Empty : value; }
		}

		private string UnstructuredData
		{
			get { return SecurityUtility.EncryptPassword(FPassword); }
			set { FPassword = SecurityUtility.DecryptPassword(value); }
		}
		
		// HostName
		private string FHostName = String.Empty;
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value == null ? String.Empty : value; }
		}
		
        // DefaultLibraryName
        private string FDefaultLibraryName = String.Empty;
		/// <summary>Determines the default library for the session. If specified, the current library for the session is initially set to this value.</summary>
		/// <remarks>The current library is used when resolving or creating catalog objects. The default value is blank. </remarks>
        public string DefaultLibraryName
        {
			get { return FDefaultLibraryName; }
			set { FDefaultLibraryName = value == null ? String.Empty : value; }
        }

		private bool FDefaultUseDTC = false;
		/// <summary> Determines the default UseDTC setting for processes started on this session. Defaults to false.</summary>
		/// <remarks> Defaults to false. </remarks>
		public bool DefaultUseDTC
		{
			get { return FDefaultUseDTC; }
			set { FDefaultUseDTC = value; }
		}
		
		// DefaultUseImplicitTransactions
		private bool FDefaultUseImplicitTransactions = true;
		/// <summary>Determines the default UseImplicitTransactions setting for processes started on this session. Defaults to true.</summary>
		public bool DefaultUseImplicitTransactions
		{
			get { return FDefaultUseImplicitTransactions; }
			set { FDefaultUseImplicitTransactions = value; }
		}

		private IsolationLevel FDefaultIsolationLevel = IsolationLevel.ReadCommitted;
		/// <summary>Determines the default isolation level setting for processes started on this session.</summary>
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FDefaultIsolationLevel; }
			set { FDefaultIsolationLevel = value; }
		}
		
		private int FDefaultMaxStackDepth = CDefaultMaxStackDepth;
		/// <summary>Determines the default maximum stack depth for processes on this session.</summary>
		public int DefaultMaxStackDepth
		{
			get { return FDefaultMaxStackDepth; }
			set { FDefaultMaxStackDepth = value; }
		}
		
		private int FDefaultMaxCallDepth = CDefaultMaxCallDepth;
		/// <summary>Determines the default maximum call depth for processes on this session.</summary>
		public int DefaultMaxCallDepth
		{
			get { return FDefaultMaxCallDepth; }
			set { FDefaultMaxCallDepth = value; }
		}
		
		private bool FUsePlanCache = true;
		/// <summary>Detetrmines whether or not to use the server plan cache on this session.</summary>
		public bool UsePlanCache
		{
			get { return FUsePlanCache; }
			set { FUsePlanCache = value; }
		}
		
		private bool FShouldEmitIL = false;
		/// <summary>Detetrmines whether or not the compiler will emit IL instructions for the nodes that support IL compilation.</summary>
		public bool ShouldEmitIL
		{
			get { return FShouldEmitIL; }
			set { FShouldEmitIL = value; }
		}

		#region ISerializable Members

		[SecurityPermissionAttribute(SecurityAction.Demand,SerializationFormatter=true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("UserID", UserID);
			info.AddValue("UnstructuredData", UnstructuredData);
			info.AddValue("HostName", HostName);
			info.AddValue("DefaultLibraryName", DefaultLibraryName);
			info.AddValue("DefaultUseDTC", DefaultUseDTC);
			info.AddValue("DefaultUseImplicitTransactions", DefaultUseImplicitTransactions);
			info.AddValue("DefaultIsolationLevel", DefaultIsolationLevel);
			info.AddValue("DefaultMaxStackDepth", DefaultMaxStackDepth);
			info.AddValue("DefaultMaxCallDepth", DefaultMaxCallDepth);
			info.AddValue("UsePlanCache", UsePlanCache);
			info.AddValue("ShouldEmitIL", ShouldEmitIL);
		}
		
		protected NativeSessionInfo(SerializationInfo info, StreamingContext context)
		{
			UserID = info.GetString("UserID");
			UnstructuredData = info.GetString("UnstructuredData");
			HostName = info.GetString("HostName");
			DefaultLibraryName = info.GetString("DefaultLibraryName");
			DefaultUseDTC = info.GetBoolean("DefaultUseDTC");
			DefaultUseImplicitTransactions = info.GetBoolean("DefaultUseImplicitTransactions");
			DefaultIsolationLevel = (IsolationLevel)info.GetValue("DefaultIsolationLevel", typeof(IsolationLevel));
			DefaultMaxStackDepth = info.GetInt32("DefaultMaxStackDepth");
			DefaultMaxCallDepth = info.GetInt32("DefaultMaxCallDepth");
			UsePlanCache = info.GetBoolean("UsePlanCache");
			ShouldEmitIL = info.GetBoolean("ShouldEmitIL");
		}

		#endregion

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
