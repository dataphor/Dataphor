# System.CreateUser

**Declarations**

```d4
create operator .System.CreateUser(const AUserID : System.UserID, const AUserName : System.String, const APassword : System.String)
 class "System.SystemCreateUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

Creates a user in the Dataphor Server with the specified ID, name, and password. For more information on users and security, refer to the Security documentation in the Dataphor User's Guide and Dataphor Developer's Guide.

**Using System.CreateUser**

```d4
CreateUser("JS1", "John Smith", "Password");
DropUser("JS1");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.UserName](System.UserName.md) \| [System.UserID](System.UserID.md) \| [Session Information](../../../users-guide/aliases.md#session-information) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateRole](System.CreateRole.md) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.ChangePassword](System.ChangePassword.md) \| [System.DropUser](System.DropUser.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RemoveUserFromRole](System.RemoveUserFromRole.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.SetUserName](System.SetUserName.md)
