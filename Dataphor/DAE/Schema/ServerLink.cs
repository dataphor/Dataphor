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

namespace Alphora.Dataphor.DAE.Schema
{
	public class ServerLink : CatalogObject
    {
		public const string DefaultUserID = "";
		
		public ServerLink(int iD, string name) : base(iD, name)
		{
			IsRemotable = false;
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.ServerLink"), DisplayName, InstanceName, HostName, OverridePortNumber > 0 ? ":" + OverridePortNumber.ToString() : ""); } }
		
		private string _hostName = String.Empty;
		public string HostName
		{
			get { return _hostName; }
			set { _hostName = value == null ? String.Empty : value; }
		}
		
		private string _instanceName = String.Empty;
		public string InstanceName
		{
			get { return _instanceName; }
			set { _instanceName = value == null ? String.Empty : value; }
		}
		
		private int _overridePortNumber = 0;
		public int OverridePortNumber
		{
			get { return _overridePortNumber; }
			set { _overridePortNumber = value; }
		}
		
		private bool _useSecureConnection = false;
		public bool UseSecureConnection
		{
			get { return _useSecureConnection; }
			set { _useSecureConnection = value; }
		}
		
		private int _overrideListenerPortNumber = 0;
		public int OverrideListenerPortNumber
		{
			get { return _overrideListenerPortNumber; }
			set { _overrideListenerPortNumber = value; }
		}
		
		private bool _useSecureListenerConnection = false;
		public bool UseSecureListenerConnection
		{
			get { return _useSecureListenerConnection; }
			set { _useSecureListenerConnection = value; }
		}
		
		private bool _useSessionInfo = true;
		public bool UseSessionInfo
		{
			get { return _useSessionInfo; }
			set { _useSessionInfo = value; }
		}
		
		public void ResetServerLink()
		{
			_hostName = String.Empty;
			_instanceName = String.Empty;
			_overridePortNumber = 0;
			_useSessionInfo = true;
		}
		
		public void ApplyMetaData()
		{
			if (MetaData != null)
			{
				_hostName = MetaData.Tags.GetTagValue("HostName", "localhost");
				_instanceName = MetaData.Tags.GetTagValue("InstanceName", Engine.DefaultServerName);
				_overridePortNumber = Convert.ToInt32(MetaData.Tags.GetTagValue("OverridePortNumber", "0"));
				_useSecureConnection = Convert.ToBoolean(MetaData.Tags.GetTagValue("UseSecureConnection", "false"));
				_overrideListenerPortNumber = Convert.ToInt32(MetaData.Tags.GetTagValue("OverrideListenerPortNumber", "0"));
				_useSecureListenerConnection = Convert.ToBoolean(MetaData.Tags.GetTagValue("UseSecureListenerConnection", "false"));
				_useSessionInfo = Convert.ToBoolean(MetaData.Tags.GetTagValue("UseSessionInfo", "true"));
			}
		}

		// ServerLinkUsers
		private ServerLinkUsers _users = new ServerLinkUsers();
		public ServerLinkUsers Users { get { return _users; } }
		
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
		public ServerLinkUser GetUser(string userID)
		{
			ServerLinkUser user = null;
			_users.TryGetValue(userID, out user);
			if (user == null)
				_users.TryGetValue(DefaultUserID, out user);
			if (user == null)
				user = new ServerLinkUser(DefaultUserID, this, Server.Engine.AdminUserID, SecurityUtility.EncryptPassword(String.Empty));

			return user;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				CreateServerStatement statement = new CreateServerStatement();
				statement.ServerName = Schema.Object.EnsureRooted(Name);
				statement.MetaData = MetaData == null ? null : MetaData.Copy();

				if (_users.Count > 0)
				{
					Block block = new Block();
					block.Statements.Add(statement);
					foreach (ServerLinkUser user in _users.Values)
						block.Statements.Add
						(
							new ExpressionStatement
							(
								new CallExpression
								(
									"CreateServerLinkUserWithEncryptedPassword", 
									new Expression[]
									{
										new ValueExpression(user.UserID), 
										new CallExpression("Name", new Expression[]{new ValueExpression(user.ServerLink.Name)}), 
										new ValueExpression(user.ServerLinkUserID), 
										new ValueExpression(user.ServerLinkPassword)
									}
								)
							)
						);
				
					return block;
				}

				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
	}

	public class ServerLinkUser : System.Object
	{
		public ServerLinkUser() : base(){}
		public ServerLinkUser(string userID, ServerLink serverLink, string serverLinkUserID, string serverLinkPassword) : base()
		{
			UserID = userID;
			ServerLink = serverLink;
			ServerLinkUserID = serverLinkUserID;
			ServerLinkPassword = serverLinkPassword;
		}
		
		private string _userID;
		public string UserID { get { return _userID; } set { _userID = value; } }
		
		[Reference]
		private ServerLink _serverLink;
		public ServerLink ServerLink { get { return _serverLink; } set { _serverLink = value; } }
		
		private string _serverLinkUserID = String.Empty;
		public string ServerLinkUserID { get { return _serverLinkUserID; } set { _serverLinkUserID = value == null ? String.Empty : value; } }
		
		private string _serverLinkPassword = String.Empty;
		public string ServerLinkPassword { get { return _serverLinkPassword; } set { _serverLinkPassword = value == null ? String.Empty : value; } }
	}
	
	public class ServerLinkUsers : HashtableList<string, ServerLinkUser>
	{		
		public ServerLinkUsers() : base(StringComparer.InvariantCultureIgnoreCase) {}
		
		public override int Add(object tempValue)
		{
			ServerLinkUser user = (ServerLinkUser)tempValue;
			Add(user.UserID, user);
			return IndexOf(user.UserID);
		}
		
		public void Add(ServerLinkUser serverLinkUser)
		{
			Add(serverLinkUser.UserID, serverLinkUser);
		}
	}
}