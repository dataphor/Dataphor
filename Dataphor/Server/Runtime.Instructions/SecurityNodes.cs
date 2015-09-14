/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System; 
using System.Text;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
    /// <remarks>operator CreateRight(const ARightName : System.Name);</remarks>
    /// <remarks>operator CreateRight(const ARightName : System.Name, const AUserID : System.String);</remarks>
    public class SystemCreateRightNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			CreateRightNode.CreateRight(program, (string)arguments[0], (arguments.Length == 2) ? (string)arguments[1] : program.Plan.User.ID);
			return null;
		}
    }
    
    /// <remarks>operator DropRight(const ARightName : String); </remarks>
    public class SystemDropRightNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			DropRightNode.DropRight(program, (string)arguments[0]);
			return null;
		}
    }
    
    /// <remarks>operator RightExists(ARightName : string) : boolean; </remarks>
    public class SystemRightExistsNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).RightExists((string)arguments[0]);
		}
    }

    /// <remarks>operator CreateGroup(const AGroupName : String, const AParentGroupName : String); </remarks>
    public class SystemCreateGroupNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			// Deprecated, stubbed for backwards compatibility
			return null;
		}
    }
    
	// operator CreateRole(const ARoleName : Name);    
    public class SystemCreateRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = new Schema.Role(Schema.Object.Qualify((string)arguments[0], program.Plan.CurrentLibrary.Name));
			role.Owner = program.Plan.User;
			role.Library = program.Plan.CurrentLibrary;
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertRole(role);
			return null;
		}
    }
    
    // operator DropRole(const ARoleName : Name);
    public class SystemDropRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			program.Plan.CheckRight(role.GetRight(Schema.RightNames.Drop));
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeleteRole(role);
			return null;
		}
    }
    
    /// <remarks>operator RoleExists(ARoleName : string) : boolean; </remarks>
    public class SystemRoleExistsNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			lock (program.Catalog)
			{
				Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], false);
				return objectValue is Schema.Role;
			}
		}
    }

	/// <remarks>operator RoleHasRight(ARoleName : String, ARightName : Name) : System.Boolean; </remarks>
    public class SystemRoleHasRightNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as Schema.Role;
			return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).RoleHasRight(role, (string)arguments[1]);
		}
    }

    /// <remarks>operator CreateUser(AUserID : String, AUserName : String, APassword : String); </remarks>
    public class SystemCreateUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.Plan.CheckRight(Schema.RightNames.CreateUser);
			string userID = (string)arguments[0];
			if (((ServerCatalogDeviceSession)program.CatalogDeviceSession).UserExists(userID))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateUserID, userID);
			Schema.User user = new Schema.User(userID, (string)arguments[1], Schema.SecurityUtility.EncryptPassword((string)arguments[2]));
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertUser(user);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertUserRole(user.ID, program.ServerProcess.ServerSession.Server.UserRole.ID);
			return null;
		}
    }
    
    /// <remarks>operator CreateUserWithEncryptedPassword(AUserID : string, AUserName : string, AEncryptedPassword : string); </remarks>
    /// <remarks>operator CreateUserWithEncryptedPassword(AUserID : string, AUserName : string, AEncryptedPassword : string, AGroupName : String); </remarks> // Deprecated
    public class SystemCreateUserWithEncryptedPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.Plan.CheckRight(Schema.RightNames.CreateUser);
			string userID = (string)arguments[0];
			if (((ServerCatalogDeviceSession)program.CatalogDeviceSession).UserExists(userID))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateUserID, userID);
			Schema.User user = new Schema.User(userID, (string)arguments[1], (string)arguments[2]);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertUser(user);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertUserRole(user.ID, program.ServerProcess.ServerSession.Server.UserRole.ID);
			return null;
		}
    }
    
    /// <remarks>operator SetPassword(AUserID : string, APassword : string); </remarks>
    public class SystemSetPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetUserPassword(userID, Schema.SecurityUtility.EncryptPassword((string)arguments[1]));
			return null;
		}
    }
    
    /// <remarks>operator SetEncryptedPassword(AUserID : string, AEncryptedPassword : string); </remarks>
    public class SystemSetEncryptedPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetUserPassword(userID, (string)arguments[1]);
			return null;
		}
    }

    /// <remarks>operator ChangePassword(AOldPassword : string, ANewPassword : string); </remarks>
    public class SystemChangePasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = program.ServerProcess.ServerSession.User;
			if (String.Compare((string)arguments[0], Schema.SecurityUtility.DecryptPassword(user.Password), true) != 0)
				throw new ServerException(ServerException.Codes.InvalidPassword);

			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetUserPassword(user.ID, Schema.SecurityUtility.EncryptPassword((string)arguments[1]));
			return null;
		}
    }
    
    /// <remarks>operator SetUserName(AUserID : string, AUserName : string); </remarks>
    public class SystemSetUserNameNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			if (String.Compare(program.ServerProcess.ServerSession.User.ID, userID, true) != 0)
				program.Plan.CheckAuthorized(userID);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetUserName(userID, (string)arguments[1]);
			return null;
		}
    }
    
    /// <remarks>operator DropUser(AUserID : string); </remarks>
    public class SystemDropUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			
			if ((String.Compare(user.ID, Server.Engine.SystemUserID, true) == 0) || (String.Compare(user.ID, Server.Engine.AdminUserID, true) == 0))
				throw new ServerException(ServerException.Codes.CannotDropSystemUsers);
			else
				program.Plan.CheckRight(Schema.RightNames.DropUser);
				
			if (((ServerCatalogDeviceSession)program.CatalogDeviceSession).UserOwnsObjects(user.ID))
				throw new ServerException(ServerException.Codes.UserOwnsObjects, user.ID);

			if (((ServerCatalogDeviceSession)program.CatalogDeviceSession).UserOwnsRights(user.ID))
				throw new ServerException(ServerException.Codes.UserOwnsRights, user.ID);
				
			foreach (ServerSession session in program.ServerProcess.ServerSession.Server.Sessions)
				if (String.Compare(session.User.ID, user.ID, true) == 0)
					throw new ServerException(ServerException.Codes.UserHasOpenSessions, user.ID);

			((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeleteUser(user);
			
			return null;
		}
    }

    /// <remarks>operator UserExists(AUserID : string) : boolean; </remarks>
    public class SystemUserExistsNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).UserExists((string)arguments[0]);
		}
    }
    
    // operator AddUserToRole
    public class SystemAddUserToRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			program.Plan.CheckAuthorized(user.ID);
			// BTR 5/30/2012 -> This isn't really altering the role, so I'm removing this check
			//program.Plan.CheckRight(role.GetRight(Schema.RightNames.Alter));
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertUserRole(user.ID, role.ID);
			return null;
		}
    }

    // operator RemoveUserFromRole
    public class SystemRemoveUserFromRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			program.Plan.CheckAuthorized(user.ID);
			// BTR 5/30/2012 -> This isn't really altering the role, so I'm removing this check
			//program.Plan.CheckRight(role.GetRight(Schema.RightNames.Alter));
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeleteUserRole(user.ID, role.ID);
			return null;
		}
	}
	
    // operator AddGroupToRole(const AGroupName : String, const ARoleName : Name);
    // operator AddGroupToRole(const AGroupName : String, const ARoleName : Name, const AInherited : Boolean);
    public class SystemAddGroupToRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			// Deprecated, stubbed for backwards compatibility
			return null;
		}
    }
    
    /// <remarks>operator GrantRightToRole(ARightName : String, ARoleName : Name); </remarks>
    public class SystemGrantRightToRoleNode : InstructionNode
    {
		public static void GrantRight(Program program, string rightName, Schema.Role role)
		{
			// BTR 5/30/2012 -> This isn't really altering the role, so I'm removing this check
			//program.Plan.CheckRight(role.GetRight(Schema.RightNames.Alter));
			program.Plan.CheckRight(rightName);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).GrantRightToRole(rightName, role.ID);
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
			GrantRight(program, (string)arguments[0], role);
			return null;
		}
    }

    /// <remarks>operator SafeGrantRightToRole(ARightName : String, ARoleName : Name); </remarks>
    public class SystemSafeGrantRightToRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], false) as Schema.Role;
			if (role != null)
				SystemGrantRightToRoleNode.GrantRight(program, (string)arguments[0], role);
			return null;
		}
    }

	/// <remarks>operator SafeGrantRightToGroup(ARightName : String, AGroupName : String, AInherited : Boolean, AApplyRecursively : Boolean, AIncludeUsers : Boolean); </remarks>
    public class SystemSafeGrantRightToGroupNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			// Deprecated, stubbed for backwards compatibility
			return null;
		}
    }

    /// <remarks>
    ///	operator GrantRightToUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemGrantRightToUserNode : InstructionNode
    {
		public static void GrantRight(Program program, Schema.User user, string rightName)
		{
			program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(rightName);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).GrantRightToUser(rightName, user.ID);
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1]);
			GrantRight(program, user, (string)arguments[0]);
			return null;
		}
    }

    /// <remarks>
    ///	operator SafeGrantRightToUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemSafeGrantRightToUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1], false);
			if (user != null)
				SystemGrantRightToUserNode.GrantRight(program, user, (string)arguments[0]);
			return null;
		}
    }

    /// <remarks>
    ///	operator RevokeRightFromRole(ARightName : String, AGroupName : String); 
    ///	</remarks>
    public class SystemRevokeRightFromRoleNode : InstructionNode
    {
		public static void RevokeRight(Program program, string rightName, Schema.Role role)
		{
			// BTR 5/30/2012 -> This isn't really altering the role, so I'm removing this check
			//program.Plan.CheckRight(role.GetRight(Schema.RightNames.Alter));
			program.Plan.CheckRight(rightName);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).RevokeRightFromRole(rightName, role.ID);
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
			RevokeRight(program, (string)arguments[0], role);
			return null;
		}
    }

    /// <remarks>
    ///	operator SafeRevokeRightFromRole(ARightName : String, AGroupName : String, AInherited : Boolean); 
    ///	</remarks>
    public class SystemSafeRevokeRightFromRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], false) as Schema.Role;
			if (role != null)
				SystemRevokeRightFromRoleNode.RevokeRight(program, (string)arguments[0], role);
			return null;
		}
    }
    
    /// <remarks>
    ///	operator RevokeRightFromUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemRevokeRightFromUserNode : InstructionNode
    {
		public static void RevokeRight(Program program, string rightName, Schema.User user)
		{
			program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(rightName);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).RevokeRightFromUser(rightName, user.ID);
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1]);
			RevokeRight(program, (string)arguments[0], user);
			return null;
		}
    }
    
    /// <remarks>
    ///	operator SafeRevokeRightFromUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemSafeRevokeRightFromUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1], false);
			if (user != null)
				SystemRevokeRightFromUserNode.RevokeRight(program, (string)arguments[0], user);
			return null;
		}
    }
    
    /// <remarks>operator RevertRightForRole(ARightName : Name, ARoleName : Name);</remarks>
    public class SystemRevertRightForRoleNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string rightName = (string)arguments[0];
			Schema.Role role = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Role;
			if (role == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);

			// BTR 5/30/2012 -> This isn't really altering the role, so I'm removing this check
			//program.Plan.CheckRight(role.GetRight(Schema.RightNames.Alter));
			program.Plan.CheckRight(rightName);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).RevertRightForRole(rightName, role.ID);
			return null;
		}
    }
    
    /// <remarks>operator RevertRightForUser(ARightName : String, AUserID : String);</remarks>
    public class SystemRevertRightForUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string rightName = (string)arguments[0];
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1]);
			program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(rightName);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).RevertRightForUser(rightName, user.ID);
			return null;
		}
    }
    
    /// <remarks>operator SetObjectOwner(AObjectName : Name, AUserID : String); </remarks>
    public class SystemSetObjectOwnerNode : InstructionNode
    {
		private void ChangeObjectOwner(Program program, Schema.CatalogObject objectValue, Schema.User user)
		{
			if (objectValue != null)
			{
				objectValue.Owner = user;
				((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetCatalogObjectOwner(objectValue.ID, user.ID);

				if (objectValue is Schema.ScalarType)
				{
					Schema.ScalarType scalarType = (Schema.ScalarType)objectValue;
				
					if (scalarType.EqualityOperator != null)
						ChangeObjectOwner(program, scalarType.EqualityOperator, user);
					
					if (scalarType.ComparisonOperator != null)
						ChangeObjectOwner(program, scalarType.ComparisonOperator, user);
				
					if (scalarType.IsSpecialOperator != null)
						ChangeObjectOwner(program, scalarType.IsSpecialOperator, user);
					
					foreach (Schema.Special special in scalarType.Specials)
					{
						ChangeObjectOwner(program, special.Selector, user);
						ChangeObjectOwner(program, special.Comparer, user);
					}
				
					#if USETYPEINHERITANCE	
					foreach (Schema.Operator operatorValue in scalarType.ExplicitCastOperators)
						ChangeObjectOwner(AProgram, operatorValue, AUser);
					#endif
					
					foreach (Schema.Representation representation in scalarType.Representations)
					{
						if (representation.Selector != null)
							ChangeObjectOwner(program, representation.Selector, user);

						foreach (Schema.Property property in representation.Properties)
						{
							if (property.ReadAccessor != null)
								ChangeObjectOwner(program, property.ReadAccessor, user);
							if (property.WriteAccessor != null)
								ChangeObjectOwner(program, property.WriteAccessor, user);
						}
					}
				}
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.CatalogObject objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], false) as Schema.CatalogObject;
			if (objectValue == null) 
				throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectExpected, objectValue.Name);

			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1]);
			if (program.Plan.User.ID != user.ID)
				program.Plan.CheckAuthorized(user.ID);
			if (!objectValue.IsOwner(program.Plan.User))
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.Plan.User.ID);
			ChangeObjectOwner(program, objectValue, user);

			return null;
		}
    }

	/// <remarks>operator SetRightOwner(ARightName : Name, AUserID : String); </remarks>
    public class SystemSetRightOwnerNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string rightName = (string)arguments[0];
			Schema.Right right = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveRight(rightName);
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[1]);
			if (right.IsGenerated)
				throw new ServerException(ServerException.Codes.CannotDropGeneratedRight, right.Name);
				
			if (program.Plan.User.ID != user.ID)
				program.Plan.CheckAuthorized(user.ID);
			if (!right.IsOwner(program.Plan.User.ID))
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.Plan.User.ID);

			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetRightOwner(rightName, user.ID);
			return null;
		}
    }

	/// <remarks>operator UserHasRight(AUserID : String, ARightName : Name) : System.Boolean; </remarks>
    public class SystemUserHasRightNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			return ((ServerCatalogDeviceSession)program.CatalogDeviceSession).UserHasRight(user.ID, (string)arguments[1]);
		}
    }

	/// <remarks>operator CreateDeviceUser(AUserID : string, ADeviceName : System.Name, ADeviceUserID : string, ADevicePassword : string); </remarks>
	/// <remarks>operator CreateDeviceUser(AUserID : string, ADeviceName : System.Name, ADeviceUserID : string, ADevicePassword : string, AConnectionString : string); </remarks>
	public class SystemCreateDeviceUserNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (user.IsSystemUser())
				program.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(device.GetRight(Schema.RightNames.MaintainUsers));
			if (((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeviceUserExists(device, user))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateDeviceUser, user.ID, device.Name);
				
			Schema.DeviceUser deviceUser = new Schema.DeviceUser(user, device, (string)arguments[2], Schema.SecurityUtility.EncryptPassword((string)arguments[3]));
			if ((arguments.Length == 5) && (arguments[4] != null))
				deviceUser.ConnectionParameters = (string)arguments[4];				
			
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertDeviceUser(deviceUser);	
			return null;
		}
	}

	/// <remarks>operator CreateDeviceUserWithEncryptedPassword(AUserID : String, ADeviceName : System.Name, ADeviceUserID : String, ADevicePassword : String); </remarks>
	/// <remarks>operator CreateDeviceUserWithEncryptedPassword(AUserID : String, ADeviceName : System.Name, ADeviceUserID : String, ADevicePassword : String, AConnectionString : String); </remarks>
	public class SystemCreateDeviceUserWithEncryptedPasswordNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (user.IsSystemUser())
				program.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(device.GetRight(Schema.RightNames.MaintainUsers));
			if (((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeviceUserExists(device, user))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateDeviceUser, user.ID, device.Name);

			Schema.DeviceUser deviceUser = new Schema.DeviceUser(user, device, (string)arguments[2], (string)arguments[3]);
			if ((arguments.Length == 5) && (arguments[4] != null))
				deviceUser.ConnectionParameters = (string)arguments[4];

			((ServerCatalogDeviceSession)program.CatalogDeviceSession).InsertDeviceUser(deviceUser);
			return null;
		}
	}

    /// <remarks>operator SetDeviceUserID(AUserID : string, ADeviceName : System.Name, ADeviceUserID : string); </remarks>
    public class SystemSetDeviceUserIDNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (user.IsSystemUser())
				program.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(device.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser deviceUser = program.CatalogDeviceSession.ResolveDeviceUser(device, user);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetDeviceUserID(deviceUser, (string)arguments[2]);
			//LDeviceUser.DeviceUserID = (string)AArguments[2];
			//((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator SetDeviceUserConnectionParameters(AUserID : string, ADeviceName : System.Name, AConnectionParameters : String); </remarks>
    public class SystemSetDeviceUserConnectionParametersNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (user.IsSystemUser())
				program.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(device.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser deviceUser = program.CatalogDeviceSession.ResolveDeviceUser(device, user);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetDeviceUserConnectionParameters(deviceUser, (string)arguments[2]);
			//LDeviceUser.ConnectionParameters = (string)AArguments[2];
			//((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator SetDeviceUserPassword(AUserID : string, ADeviceName : System.Name, ADevicePassword : string); </remarks>
    public class SystemSetDeviceUserPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (user.IsSystemUser())
				program.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(device.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser deviceUser = program.CatalogDeviceSession.ResolveDeviceUser(device, user);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetDeviceUserPassword(deviceUser, Schema.SecurityUtility.EncryptPassword((string)arguments[2]));
			//LDeviceUser.DevicePassword = Schema.SecurityUtility.EncryptPassword((string)AArguments[2]);
			//((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator ChangeDeviceUserPasswordNode(ADeviceName : System.Name, AOldPassword : string, ANewPassword : string); </remarks>
    public class SystemChangeDeviceUserPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			Schema.User user = program.ServerProcess.ServerSession.User;
			if (user.ID != program.Plan.User.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.Plan.User.ID);
				
			Schema.DeviceUser deviceUser = program.CatalogDeviceSession.ResolveDeviceUser(device, user);
			if (String.Compare((string)arguments[1], Schema.SecurityUtility.DecryptPassword(deviceUser.DevicePassword), true) != 0)
				throw new ServerException(ServerException.Codes.InvalidPassword);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SetDeviceUserPassword(deviceUser, Schema.SecurityUtility.EncryptPassword((string)arguments[2]));
			//LDeviceUser.DevicePassword = Schema.SecurityUtility.EncryptPassword((string)AArguments[2]);
			//((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator DropDeviceUser(AUserID : string, ADeviceName : System.Name); </remarks>
    public class SystemDropDeviceUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (user.IsSystemUser())
				program.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckAuthorized(user.ID);
			program.Plan.CheckRight(device.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser deviceUser = program.CatalogDeviceSession.ResolveDeviceUser(device, user);
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).DeleteDeviceUser(deviceUser);
			return null;
		}
    }

    /// <remarks>operator DeviceUserExists(AUserID : string, ADeviceName : System.Name) : boolean; </remarks>
    public class SystemDeviceUserExistsNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.User user = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).ResolveUser((string)arguments[0]);
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1], true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			Schema.DeviceUser deviceUser = program.CatalogDeviceSession.ResolveDeviceUser(device, user, false);
			return deviceUser != null;
		}
    }
}