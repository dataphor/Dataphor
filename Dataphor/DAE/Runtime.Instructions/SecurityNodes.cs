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

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
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
		public static void CreateRight(ServerProcess AProcess, string ARightName, string AUserID)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AUserID);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckRight(Schema.RightNames.AlterUser);
			
			if (AProcess.CatalogDeviceSession.RightExists(ARightName))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateRightName, ARightName);
				
			AProcess.CatalogDeviceSession.InsertRight(ARightName, LUser.ID);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			CreateRight(AProcess, AArguments[0].Value.AsString, (AArguments.Length == 2) ? AArguments[1].Value.AsString : AProcess.Plan.User.ID);
			return null;
		}
    }
    
    /// <remarks>operator DropRight(const ARightName : String); </remarks>
    public class SystemDropRightNode : InstructionNode
    {
		public static void DropRight(ServerProcess AProcess, string ARightName)
		{
			Schema.Right LRight = AProcess.CatalogDeviceSession.ResolveRight(ARightName);
			if (LRight.OwnerID != AProcess.Plan.User.ID)
				if ((LRight.OwnerID == Server.Server.CSystemUserID) || (AProcess.Plan.User.ID != Server.Server.CAdminUserID))
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.Plan.User.ID);
			if (LRight.IsGenerated)
				throw new ServerException(ServerException.Codes.CannotDropGeneratedRight, LRight.Name);
				
			AProcess.CatalogDeviceSession.DeleteRight(ARightName);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			DropRight(AProcess, AArguments[0].Value.AsString);
			return null;
		}
    }
    
    /// <remarks>operator RightExists(ARightName : string) : boolean; </remarks>
    public class SystemRightExistsNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, AProcess.CatalogDeviceSession.RightExists(AArguments[0].Value.AsString)));
		}
    }

    /// <remarks>operator CreateGroup(const AGroupName : String, const AParentGroupName : String); </remarks>
    public class SystemCreateGroupNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Deprecated, stubbed for backwards compatibility
			return null;
		}
    }
    
	// operator CreateRole(const ARoleName : Name);    
    public class SystemCreateRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = new Schema.Role(Schema.Object.Qualify(AArguments[0].Value.AsString, AProcess.Plan.CurrentLibrary.Name));
			LRole.Owner = AProcess.Plan.User;
			LRole.Library = AProcess.Plan.CurrentLibrary;
			AProcess.CatalogDeviceSession.InsertRole(LRole);
			return null;
		}
    }
    
    // operator DropRole(const ARoleName : Name);
    public class SystemDropRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			AProcess.Plan.CheckRight(LRole.GetRight(Schema.RightNames.Drop));
			AProcess.CatalogDeviceSession.DeleteRole(LRole);
			return null;
		}
    }
    
    /// <remarks>operator RoleExists(ARoleName : string) : boolean; </remarks>
    public class SystemRoleExistsNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog)
			{
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, false);
				return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, LObject is Schema.Role));
			}
		}
    }

	/// <remarks>operator RoleHasRight(ARoleName : String, ARightName : Name) : System.Boolean; </remarks>
    public class SystemRoleHasRightNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true) as Schema.Role;
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, AProcess.CatalogDeviceSession.RoleHasRight(LRole, AArguments[1].Value.AsString)));
		}
    }

    /// <remarks>operator CreateUser(AUserID : String, AUserName : String, APassword : String); </remarks>
    public class SystemCreateUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.Plan.CheckRight(Schema.RightNames.CreateUser);
			string LUserID = AArguments[0].Value.AsString;
			if (AProcess.CatalogDeviceSession.UserExists(LUserID))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateUserID, LUserID);
			Schema.User LUser = new Schema.User(LUserID, AArguments[1].Value.AsString, Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString));
			AProcess.CatalogDeviceSession.InsertUser(LUser);
			AProcess.CatalogDeviceSession.InsertUserRole(LUser.ID, AProcess.ServerSession.Server.UserRole.ID);
			return null;
		}
    }
    
    /// <remarks>operator CreateUserWithEncryptedPassword(AUserID : string, AUserName : string, AEncryptedPassword : string); </remarks>
    /// <remarks>operator CreateUserWithEncryptedPassword(AUserID : string, AUserName : string, AEncryptedPassword : string, AGroupName : String); </remarks> // Deprecated
    public class SystemCreateUserWithEncryptedPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.Plan.CheckRight(Schema.RightNames.CreateUser);
			string LUserID = AArguments[0].Value.AsString;
			if (AProcess.CatalogDeviceSession.UserExists(LUserID))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateUserID, LUserID);
			Schema.User LUser = new Schema.User(LUserID, AArguments[1].Value.AsString, AArguments[2].Value.AsString);
			AProcess.CatalogDeviceSession.InsertUser(LUser);
			AProcess.CatalogDeviceSession.InsertUserRole(LUser.ID, AProcess.ServerSession.Server.UserRole.ID);
			return null;
		}
    }
    
    /// <remarks>operator SetPassword(AUserID : string, APassword : string); </remarks>
    public class SystemSetPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LUserID = AArguments[0].Value.AsString;
			AProcess.Plan.CheckAuthorized(LUserID);
			AProcess.CatalogDeviceSession.SetUserPassword(LUserID, Schema.SecurityUtility.EncryptPassword(AArguments[1].Value.AsString));
			return null;
		}
    }
    
    /// <remarks>operator SetEncryptedPassword(AUserID : string, AEncryptedPassword : string); </remarks>
    public class SystemSetEncryptedPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LUserID = AArguments[0].Value.AsString;
			AProcess.Plan.CheckAuthorized(LUserID);
			AProcess.CatalogDeviceSession.SetUserPassword(LUserID, AArguments[1].Value.AsString);
			return null;
		}
    }

    /// <remarks>operator ChangePassword(AOldPassword : string, ANewPassword : string); </remarks>
    public class SystemChangePasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.ServerSession.User;
			if (String.Compare(AArguments[0].Value.AsString, Schema.SecurityUtility.DecryptPassword(LUser.Password), true) != 0)
				throw new ServerException(ServerException.Codes.InvalidPassword);

			AProcess.CatalogDeviceSession.SetUserPassword(LUser.ID, Schema.SecurityUtility.EncryptPassword(AArguments[1].Value.AsString));
			return null;
		}
    }
    
    /// <remarks>operator SetUserName(AUserID : string, AUserName : string); </remarks>
    public class SystemSetUserNameNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LUserID = AArguments[0].Value.AsString;
			if (String.Compare(AProcess.ServerSession.User.ID, LUserID, true) != 0)
				AProcess.Plan.CheckAuthorized(LUserID);
			AProcess.CatalogDeviceSession.SetUserName(LUserID, AArguments[1].Value.AsString);
			return null;
		}
    }
    
    /// <remarks>operator DropUser(AUserID : string); </remarks>
    public class SystemDropUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			
			if ((String.Compare(LUser.ID, Server.Server.CSystemUserID, true) == 0) || (String.Compare(LUser.ID, Server.Server.CAdminUserID, true) == 0))
				throw new ServerException(ServerException.Codes.CannotDropSystemUsers);
			else
				AProcess.Plan.CheckRight(Schema.RightNames.DropUser);
				
			if (AProcess.CatalogDeviceSession.UserOwnsObjects(LUser.ID))
				throw new ServerException(ServerException.Codes.UserOwnsObjects, LUser.ID);

			if (AProcess.CatalogDeviceSession.UserOwnsRights(LUser.ID))
				throw new ServerException(ServerException.Codes.UserOwnsRights, LUser.ID);
				
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				if (String.Compare(LSession.User.ID, LUser.ID, true) == 0)
					throw new ServerException(ServerException.Codes.UserHasOpenSessions, LUser.ID);

			AProcess.CatalogDeviceSession.DeleteUser(LUser);
			
			return null;
		}
    }

    /// <remarks>operator UserExists(AUserID : string) : boolean; </remarks>
    public class SystemUserExistsNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, AProcess.CatalogDeviceSession.UserExists(AArguments[0].Value.AsString)));
		}
    }
    
    // operator AddUserToRole
    public class SystemAddUserToRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LRole.GetRight(Schema.RightNames.Alter));
			AProcess.CatalogDeviceSession.InsertUserRole(LUser.ID, LRole.ID);
			return null;
		}
    }

    // operator RemoveUserFromRole
    public class SystemRemoveUserFromRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
				
			AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LRole.GetRight(Schema.RightNames.Alter));
			AProcess.CatalogDeviceSession.DeleteUserRole(LUser.ID, LRole.ID);
			return null;
		}
	}
	
    // operator AddGroupToRole(const AGroupName : String, const ARoleName : Name);
    // operator AddGroupToRole(const AGroupName : String, const ARoleName : Name, const AInherited : Boolean);
    public class SystemAddGroupToRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Deprecated, stubbed for backwards compatibility
			return null;
		}
    }
    
    /// <remarks>operator GrantRightToRole(ARightName : String, ARoleName : Name); </remarks>
    public class SystemGrantRightToRoleNode : InstructionNode
    {
		public static void GrantRight(ServerProcess AProcess, string ARightName, Schema.Role ARole)
		{
			AProcess.Plan.CheckRight(ARole.GetRight(Schema.RightNames.Alter));
			AProcess.Plan.CheckRight(ARightName);
			AProcess.CatalogDeviceSession.GrantRightToRole(ARightName, ARole.ID);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
			GrantRight(AProcess, AArguments[0].Value.AsString, LRole);
			return null;
		}
    }

    /// <remarks>operator SafeGrantRightToRole(ARightName : String, ARoleName : Name); </remarks>
    public class SystemSafeGrantRightToRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, false) as Schema.Role;
			if (LRole != null)
				SystemGrantRightToRoleNode.GrantRight(AProcess, AArguments[0].Value.AsString, LRole);
			return null;
		}
    }

	/// <remarks>operator SafeGrantRightToGroup(ARightName : String, AGroupName : String, AInherited : Boolean, AApplyRecursively : Boolean, AIncludeUsers : Boolean); </remarks>
    public class SystemSafeGrantRightToGroupNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
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
		public static void GrantRight(ServerProcess AProcess, Schema.User AUser, string ARightName)
		{
			AProcess.Plan.CheckAuthorized(AUser.ID);
			AProcess.Plan.CheckRight(ARightName);
			AProcess.CatalogDeviceSession.GrantRightToUser(ARightName, AUser.ID);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString);
			GrantRight(AProcess, LUser, AArguments[0].Value.AsString);
			return null;
		}
    }

    /// <remarks>
    ///	operator SafeGrantRightToUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemSafeGrantRightToUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString, false);
			if (LUser != null)
				SystemGrantRightToUserNode.GrantRight(AProcess, LUser, AArguments[0].Value.AsString);
			return null;
		}
    }

    /// <remarks>
    ///	operator RevokeRightFromRole(ARightName : String, AGroupName : String); 
    ///	</remarks>
    public class SystemRevokeRightFromRoleNode : InstructionNode
    {
		public static void RevokeRight(ServerProcess AProcess, string ARightName, Schema.Role ARole)
		{
			AProcess.Plan.CheckRight(ARole.GetRight(Schema.RightNames.Alter));
			AProcess.Plan.CheckRight(ARightName);
			AProcess.CatalogDeviceSession.RevokeRightFromRole(ARightName, ARole.ID);
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);
			RevokeRight(AProcess, AArguments[0].Value.AsString, LRole);
			return null;
		}
    }

    /// <remarks>
    ///	operator SafeRevokeRightFromRole(ARightName : String, AGroupName : String, AInherited : Boolean); 
    ///	</remarks>
    public class SystemSafeRevokeRightFromRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, false) as Schema.Role;
			if (LRole != null)
				SystemRevokeRightFromRoleNode.RevokeRight(AProcess, AArguments[0].Value.AsString, LRole);
			return null;
		}
    }
    
    /// <remarks>
    ///	operator RevokeRightFromUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemRevokeRightFromUserNode : InstructionNode
    {
		public static void RevokeRight(ServerProcess AProcess, string ARightName, Schema.User AUser)
		{
			AProcess.Plan.CheckAuthorized(AUser.ID);
			AProcess.Plan.CheckRight(ARightName);
			AProcess.CatalogDeviceSession.RevokeRightFromUser(ARightName, AUser.ID);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString);
			RevokeRight(AProcess, AArguments[0].Value.AsString, LUser);
			return null;
		}
    }
    
    /// <remarks>
    ///	operator SafeRevokeRightFromUser(ARightName : String, AUserID : String); 
    ///	</remarks>
    public class SystemSafeRevokeRightFromUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString, false);
			if (LUser != null)
				SystemRevokeRightFromUserNode.RevokeRight(AProcess, AArguments[0].Value.AsString, LUser);
			return null;
		}
    }
    
    /// <remarks>operator RevertRightForRole(ARightName : Name, ARoleName : Name);</remarks>
    public class SystemRevertRightForRoleNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LRightName = AArguments[0].Value.AsString;
			Schema.Role LRole = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Role;
			if (LRole == null)
				throw new CompilerException(CompilerException.Codes.RoleIdentifierExpected);

			AProcess.Plan.CheckRight(LRole.GetRight(Schema.RightNames.Alter));
			AProcess.Plan.CheckRight(LRightName);
			AProcess.CatalogDeviceSession.RevertRightForRole(LRightName, LRole.ID);
			return null;
		}
    }
    
    /// <remarks>operator RevertRightForUser(ARightName : String, AUserID : String);</remarks>
    public class SystemRevertRightForUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LRightName = AArguments[0].Value.AsString;
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString);
			AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LRightName);
			AProcess.CatalogDeviceSession.RevertRightForUser(LRightName, LUser.ID);
			return null;
		}
    }
    
    /// <remarks>operator SetObjectOwner(AObjectName : Name, AUserID : String); </remarks>
    public class SystemSetObjectOwnerNode : InstructionNode
    {
		private void ChangeObjectOwner(ServerProcess AProcess, Schema.CatalogObject AObject, Schema.User AUser)
		{
			AObject.Owner = AUser;
			AProcess.CatalogDeviceSession.SetCatalogObjectOwner(AObject.ID, AUser.ID);

			if (AObject is Schema.ScalarType)
			{
				Schema.ScalarType LScalarType = (Schema.ScalarType)AObject;
				
				if (LScalarType.EqualityOperator != null)
					ChangeObjectOwner(AProcess, LScalarType.EqualityOperator, AUser);
					
				if (LScalarType.ComparisonOperator != null)
					ChangeObjectOwner(AProcess, LScalarType.ComparisonOperator, AUser);
				
				if (LScalarType.IsSpecialOperator != null)
					ChangeObjectOwner(AProcess, LScalarType.IsSpecialOperator, AUser);
					
				foreach (Schema.Special LSpecial in LScalarType.Specials)
				{
					ChangeObjectOwner(AProcess, LSpecial.Selector, AUser);
					ChangeObjectOwner(AProcess, LSpecial.Comparer, AUser);
				}
				
				#if USETYPEINHERITANCE	
				foreach (Schema.Operator LOperator in LScalarType.ExplicitCastOperators)
					ChangeObjectOwner(AProcess, LOperator, AUser);
				#endif
					
				foreach (Schema.Representation LRepresentation in LScalarType.Representations)
				{
					ChangeObjectOwner(AProcess, LRepresentation.Selector, AUser);
					foreach (Schema.Property LProperty in LRepresentation.Properties)
					{
						ChangeObjectOwner(AProcess, LProperty.ReadAccessor, AUser);
						ChangeObjectOwner(AProcess, LProperty.WriteAccessor, AUser);
					}
				}
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.CatalogObject LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, false) as Schema.CatalogObject;
			if ((LObject == null) && !AProcess.ServerSession.Server.LoadingFullCatalog) // This check is for backwards compatibility with d4c files. A d4c file persists an object's owner as a SetObjectOwner call with the old style mangled name of the operator.
				throw new Schema.SchemaException(Schema.SchemaException.Codes.CatalogObjectExpected, LObject.Name);
			if (LObject != null)
			{
				Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString);
				if (AProcess.Plan.User.ID != LUser.ID)
					AProcess.Plan.CheckAuthorized(LUser.ID);
				if (!LObject.IsOwner(AProcess.Plan.User))
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.Plan.User.ID);
				ChangeObjectOwner(AProcess, LObject, LUser);
			}
			return null;
		}
    }

	/// <remarks>operator SetRightOwner(ARightName : Name, AUserID : String); </remarks>
    public class SystemSetRightOwnerNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LRightName = AArguments[0].Value.AsString;
			Schema.Right LRight = AProcess.CatalogDeviceSession.ResolveRight(LRightName);
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[1].Value.AsString);
			if (LRight.IsGenerated)
				throw new ServerException(ServerException.Codes.CannotDropGeneratedRight, LRight.Name);
				
			if (AProcess.Plan.User.ID != LUser.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			if (!LRight.IsOwner(AProcess.Plan.User.ID))
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.Plan.User.ID);

			AProcess.CatalogDeviceSession.SetRightOwner(LRightName, LUser.ID);
			return null;
		}
    }

	/// <remarks>operator UserHasRight(AUserID : String, ARightName : Name) : System.Boolean; </remarks>
    public class SystemUserHasRightNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, AProcess.CatalogDeviceSession.UserHasRight(LUser.ID, AArguments[1].Value.AsString)));
		}
    }

	/// <remarks>operator CreateDeviceUser(AUserID : string, ADeviceName : System.Name, ADeviceUserID : string, ADevicePassword : string); </remarks>
	/// <remarks>operator CreateDeviceUser(AUserID : string, ADeviceName : System.Name, ADeviceUserID : string, ADevicePassword : string, AConnectionString : string); </remarks>
	public class SystemCreateDeviceUserNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (LUser.IsSystemUser())
				AProcess.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.MaintainUsers));
			if (AProcess.CatalogDeviceSession.DeviceUserExists(LDevice, LUser))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateDeviceUser, LUser.ID, LDevice.Name);
				
			Schema.DeviceUser LDeviceUser = new Schema.DeviceUser(LUser, LDevice, AArguments[2].Value.AsString, Schema.SecurityUtility.EncryptPassword(AArguments[3].Value.AsString));
			if ((AArguments.Length == 5) && (AArguments[4].Value != null) && !AArguments[4].Value.IsNil)
				LDeviceUser.ConnectionParameters = AArguments[4].Value.AsString;				
			
			AProcess.CatalogDeviceSession.InsertDeviceUser(LDeviceUser);	
			return null;
		}
	}

	/// <remarks>operator CreateDeviceUserWithEncryptedPassword(AUserID : String, ADeviceName : System.Name, ADeviceUserID : String, ADevicePassword : String); </remarks>
	/// <remarks>operator CreateDeviceUserWithEncryptedPassword(AUserID : String, ADeviceName : System.Name, ADeviceUserID : String, ADevicePassword : String, AConnectionString : String); </remarks>
	public class SystemCreateDeviceUserWithEncryptedPasswordNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (LUser.IsSystemUser())
				AProcess.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.MaintainUsers));
			if (AProcess.CatalogDeviceSession.DeviceUserExists(LDevice, LUser))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateDeviceUser, LUser.ID, LDevice.Name);

			Schema.DeviceUser LDeviceUser = new Schema.DeviceUser(LUser, LDevice, AArguments[2].Value.AsString, AArguments[3].Value.AsString);
			if ((AArguments.Length == 5) && (AArguments[4].Value != null) && !AArguments[4].Value.IsNil)
				LDeviceUser.ConnectionParameters = AArguments[4].Value.AsString;

			AProcess.CatalogDeviceSession.InsertDeviceUser(LDeviceUser);
			return null;
		}
	}

    /// <remarks>operator SetDeviceUserID(AUserID : string, ADeviceName : System.Name, ADeviceUserID : string); </remarks>
    public class SystemSetDeviceUserIDNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (LUser.IsSystemUser())
				AProcess.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
			AProcess.CatalogDeviceSession.SetDeviceUserID(LDeviceUser, AArguments[2].Value.AsString);
			//LDeviceUser.DeviceUserID = AArguments[2].Value.AsString;
			//AProcess.CatalogDeviceSession.UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator SetDeviceUserConnectionParameters(AUserID : string, ADeviceName : System.Name, AConnectionParameters : String); </remarks>
    public class SystemSetDeviceUserConnectionParametersNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (LUser.IsSystemUser())
				AProcess.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
			AProcess.CatalogDeviceSession.SetDeviceUserConnectionParameters(LDeviceUser, AArguments[2].Value.AsString);
			//LDeviceUser.ConnectionParameters = AArguments[2].Value.AsString;
			//AProcess.CatalogDeviceSession.UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator SetDeviceUserPassword(AUserID : string, ADeviceName : System.Name, ADevicePassword : string); </remarks>
    public class SystemSetDeviceUserPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (LUser.IsSystemUser())
				AProcess.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
			AProcess.CatalogDeviceSession.SetDeviceUserPassword(LDeviceUser, Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString));
			//LDeviceUser.DevicePassword = Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString);
			//AProcess.CatalogDeviceSession.UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator ChangeDeviceUserPasswordNode(ADeviceName : System.Name, AOldPassword : string, ANewPassword : string); </remarks>
    public class SystemChangeDeviceUserPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			Schema.User LUser = AProcess.ServerSession.User;
			if (LUser.ID != AProcess.Plan.User.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.Plan.User.ID);
				
			Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
			if (String.Compare(AArguments[1].Value.AsString, Schema.SecurityUtility.DecryptPassword(LDeviceUser.DevicePassword), true) != 0)
				throw new ServerException(ServerException.Codes.InvalidPassword);
			AProcess.CatalogDeviceSession.SetDeviceUserPassword(LDeviceUser, Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString));
			//LDeviceUser.DevicePassword = Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString);
			//AProcess.CatalogDeviceSession.UpdateDeviceUser(LDeviceUser);
			return null;
		}
    }
    
    /// <remarks>operator DropDeviceUser(AUserID : string, ADeviceName : System.Name); </remarks>
    public class SystemDropDeviceUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			if (LUser.IsSystemUser())
				AProcess.Plan.CheckRight(Schema.RightNames.MaintainSystemDeviceUsers);
			if (LUser.ID != AProcess.Plan.User.ID)
				AProcess.Plan.CheckAuthorized(LUser.ID);
			AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.MaintainUsers));
			Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
			AProcess.CatalogDeviceSession.DeleteDeviceUser(LDeviceUser);
			return null;
		}
    }

    /// <remarks>operator DeviceUserExists(AUserID : string, ADeviceName : System.Name) : boolean; </remarks>
    public class SystemDeviceUserExistsNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(AArguments[0].Value.AsString);
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser, false);
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, LDeviceUser != null));
		}
    }

	/// <remarks> <code>operator DecryptString(const AEncrypted : String) : String; </code>
	///  <para>Note: Decrypt is deterministic and repeatable because it always yields the same result.</para> </remarks>
	public class SystemDecryptStringNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Schema.SecurityUtility.DecryptString((String)AArguments[0].Value.AsNative)));
		}
    }

	/// <remarks> <code>operator EncryptString(const AUnencrypted : String) : String; </code>
	///  <para>Note: Encrypt is not deterministic or repeatable because it includes a random SALT in the result.</para> </remarks>
	public class SystemEncryptStringNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Schema.SecurityUtility.EncryptString(AArguments[0].Value.AsString)));
		}
    }
}