/*
	Dataphor
	© Copyright 2000-2008 Alphora
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
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	#if OnExpression    
	public class ServerLink : Object
    {
		public ServerLink(Guid AID) : base(AID){}
		public ServerLink(Guid AID, string AName) : base(AID, AName){}

		public ServerLink(string AName) : base()
		{
			Name = AName;
		}
		
		// ServerURI		
		private string FServerURI = String.Empty;
		public string ServerURI
		{
			get { return FServerURI; }
			set { FServerURI = value == null ? String.Empty : value; }
		}
		
		// ServerLinkUsers
		private ServerLinkUsers FUsers = new ServerLinkUsers();
		public ServerLinkUsers Users { get { return FUsers; } }
		
		public SessionInfo GetSessionInfo(User AUser)
		{
			ServerLinkUser LUser = FUsers[AUser.ID];
			return (LUser == null) ? null : new SessionInfo(LUser.ServerLinkUserID, SecurityUtility.DecryptPassword(LUser.ServerLinkPassword));
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			CreateServerStatement LStatement = new CreateServerStatement();
			LStatement.ServerName = Schema.Object.EnsureRooted(Name);
			LStatement.MetaData = MetaData == null ? null : (MetaData)MetaData.Clone();
			LStatement.ServerURI = ServerURI;
			
			if (FUsers.Count > 0)
			{
				Block LBlock = new Block();
				LBlock.Statements.Add(LStatement);
				foreach (ServerLinkUser LUser in FUsers)
					LBlock.Statements.Add
					(
						new ExpressionStatement
						(
							new CallExpression
							(
								"CreateServerLinkUserWithEncryptedPassword", 
								new Expression[]
								{
									new ValueExpression(LUser.User.ID), 
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
		public ServerLinkUser(User AUser, ServerLink AServerLink, string AServerLinkUserID, string AServerLinkPassword) : base()
		{
			User = AUser;
			ServerLink = AServerLink;
			ServerLinkUserID = AServerLinkUserID;
			ServerLinkPassword = AServerLinkPassword;
		}
		
		private User FUser;
		public User User { get { return FUser; } set { FUser = value; } }
		
		private ServerLink FServerLink;
		public ServerLink ServerLink { get { return FServerLink; } set { FServerLink = value; } }
		
		private string FServerLinkUserID = String.Empty;
		public string ServerLinkUserID { get { return FServerLinkUserID; } set { FServerLinkUserID = value == null ? String.Empty : value; } }
		
		private string FServerLinkPassword = String.Empty;
		public string ServerLinkPassword { get { return FServerLinkPassword; } set { FServerLinkPassword = value == null ? String.Empty : value; } }
	}
	
	public class ServerLinkUsers : HashtableList
	{		
		public ServerLinkUsers() : base(CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default){}
		
		public ServerLinkUser this[string AID] { get { return (ServerLinkUser)base[AID]; } }
		
		public override int Add(object AValue)
		{
			if (AValue is ServerLinkUser)
				Add((ServerLinkUser)AValue);
			else
				throw new SchemaException(SchemaException.Codes.ServerLinkUserContainer);
			return IndexOf(AValue);
		}
		
		public void Add(ServerLinkUser AServerLinkUser)
		{
			Add(AServerLinkUser.User.ID, AServerLinkUser);
		}
	}
	#endif
}