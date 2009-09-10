/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Schema
{
	public class ServerLink : CatalogObject
    {
		public const string CDefaultUserID = "";
		
		public ServerLink(int AID, string AName) : base(AID, AName)
		{
			IsRemotable = false;
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.ServerLink"), DisplayName, InstanceName, HostName, OverridePortNumber > 0 ? ":" + OverridePortNumber.ToString() : ""); } }
		
		private string FHostName = String.Empty;
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value == null ? String.Empty : value; }
		}
		
		private string FInstanceName = String.Empty;
		public string InstanceName
		{
			get { return FInstanceName; }
			set { FInstanceName = value == null ? String.Empty : value; }
		}
		
		private int FOverridePortNumber = 0;
		public int OverridePortNumber
		{
			get { return FOverridePortNumber; }
			set { FOverridePortNumber = value; }
		}
		
		private bool FUseSessionInfo = true;
		public bool UseSessionInfo
		{
			get { return FUseSessionInfo; }
			set { FUseSessionInfo = value; }
		}
		
		private NativeSessionInfo FDefaultNativeSessionInfo;
		public NativeSessionInfo DefaultNativeSessionInfo
		{
			get { return FDefaultNativeSessionInfo; }
		}
		
		private NativeSessionInfo EnsureDefaultNativeSessionInfo()
		{
			if (FDefaultNativeSessionInfo == null)
				FDefaultNativeSessionInfo = new NativeSessionInfo();
				
			return FDefaultNativeSessionInfo;
		}
		
		public void ResetServerLink()
		{
			FHostName = String.Empty;
			FInstanceName = String.Empty;
			FOverridePortNumber = 0;
			FUseSessionInfo = true;
			FDefaultNativeSessionInfo = null;
		}
		
		public void ApplyMetaData()
		{
			if (MetaData != null)
			{
				FHostName = MetaData.Tags.GetTagValue("HostName", "localhost");
				FInstanceName = MetaData.Tags.GetTagValue("InstanceName", Server.Server.CDefaultServerName);
				FOverridePortNumber = Convert.ToInt32(MetaData.Tags.GetTagValue("OverridePortNumber", "0"));
				FUseSessionInfo = Convert.ToBoolean(MetaData.Tags.GetTagValue("UseSessionInfo", "true"));
				
				Tag LTag;
				
				LTag = MetaData.Tags.GetTag("DefaultIsolationLevel");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultIsolationLevel = (System.Data.IsolationLevel)Enum.Parse(typeof(System.Data.IsolationLevel), LTag.Value);
					
				LTag = MetaData.Tags.GetTag("DefaultLibraryName");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultLibraryName = LTag.Value;
					
				LTag = MetaData.Tags.GetTag("DefaultMaxCallDepth");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultMaxCallDepth = Convert.ToInt32(LTag.Value);
					
				LTag = MetaData.Tags.GetTag("DefaultMaxStackDepth");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultMaxStackDepth = Convert.ToInt32(LTag.Value);

				LTag = MetaData.Tags.GetTag("DefaultUseDTC");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultUseDTC = Convert.ToBoolean(LTag.Value);

				LTag = MetaData.Tags.GetTag("DefaultUseImplicitTransactions");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultUseImplicitTransactions = Convert.ToBoolean(LTag.Value);
				
				LTag = MetaData.Tags.GetTag("ShouldEmitIL");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().ShouldEmitIL = Convert.ToBoolean(LTag.Value);
				
				LTag = MetaData.Tags.GetTag("UsePlanCache");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().UsePlanCache = Convert.ToBoolean(LTag.Value);
			}
		}
		
		// ServerLinkUsers
		private ServerLinkUsers FUsers = new ServerLinkUsers();
		public ServerLinkUsers Users { get { return FUsers; } }
		
		/// <summary>
		/// Returns a ServerLinkUser for the given UserID.
		/// </summary>
		/// <remarks>
		/// If there is no configured server link user for the given UserID, 
		/// the default server link user for the server link is returned.
		/// If there is no default server link user for the server link,
		/// a default ServerLinkUser is returned with credentials of
		/// Admin and a blank password.
		/// </remarks>
		public ServerLinkUser GetUser(string AUserID)
		{
			ServerLinkUser LUser = FUsers[AUserID];
			if (LUser == null)
				LUser = FUsers[CDefaultUserID];
			if (LUser == null)
				LUser = new ServerLinkUser(CDefaultUserID, this, Server.Server.CAdminUserID, SecurityUtility.EncryptPassword(String.Empty));

			return LUser;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
			}
			else
			{
				RemoveObjectID();
			}
			
			CreateServerStatement LStatement = new CreateServerStatement();
			LStatement.ServerName = Schema.Object.EnsureRooted(Name);
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();

			if (FUsers.Count > 0)
			{
				Block LBlock = new Block();
				LBlock.Statements.Add(LStatement);
				foreach (ServerLinkUser LUser in FUsers.Values)
					LBlock.Statements.Add
					(
						new ExpressionStatement
						(
							new CallExpression
							(
								"CreateServerLinkUserWithEncryptedPassword", 
								new Expression[]
								{
									new ValueExpression(LUser.UserID), 
									new CallExpression("Name", new Expression[]{new ValueExpression(LUser.ServerLink.Name)}), 
									new ValueExpression(LUser.ServerLinkUserID), 
									new ValueExpression(LUser.ServerLinkPassword)
								}
							)
						)
					);
				
				return LBlock;
			}

			return LStatement;
		}
	}

	public class ServerLinkUser : System.Object
	{
		public ServerLinkUser() : base(){}
		public ServerLinkUser(string AUserID, ServerLink AServerLink, string AServerLinkUserID, string AServerLinkPassword) : base()
		{
			UserID = AUserID;
			ServerLink = AServerLink;
			ServerLinkUserID = AServerLinkUserID;
			ServerLinkPassword = AServerLinkPassword;
		}
		
		private string FUserID;
		public string UserID { get { return FUserID; } set { FUserID = value; } }
		
		[Reference]
		private ServerLink FServerLink;
		public ServerLink ServerLink { get { return FServerLink; } set { FServerLink = value; } }
		
		private string FServerLinkUserID = String.Empty;
		public string ServerLinkUserID { get { return FServerLinkUserID; } set { FServerLinkUserID = value == null ? String.Empty : value; } }
		
		private string FServerLinkPassword = String.Empty;
		public string ServerLinkPassword { get { return FServerLinkPassword; } set { FServerLinkPassword = value == null ? String.Empty : value; } }
	}
	
	public class ServerLinkUsers : HashtableList<string, ServerLinkUser>
	{		
		public ServerLinkUsers() : base(StringComparer.InvariantCultureIgnoreCase) {}
		
		public override int Add(object AValue)
		{
			ServerLinkUser LUser = (ServerLinkUser)AValue;
			Add(LUser.UserID, LUser);
			return IndexOf(LUser.UserID);
		}
		
		public void Add(ServerLinkUser AServerLinkUser)
		{
			Add(AServerLinkUser.UserID, AServerLinkUser);
		}
	}
}