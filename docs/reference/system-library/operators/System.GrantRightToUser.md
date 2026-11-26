# System.GrantRightToUser

**Declarations**

```d4
create operator .System.GrantRightToUser(const ARightName : System.Name, const AUserID : System.UserID)
 class "System.SystemGrantRightToUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`GrantRightToUser` assignes the specified right to the specified user.

**Using System.GrantRightToUser**

```d4
CreateRight("AddProgram");

CreateUser("JS1", "John Smith", "Password");
GrantRightToUser("AddProgram", "JS1");

DropUser("JS1");
DropRight("AddProgram");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateRole](System.CreateRole.md) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropRight](System.DropRight.md) \| [System.DropUser](System.DropUser.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.RightExists](System.RightExists.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.UserExists](System.UserExists.md) \| [System.UserHasRight](System.UserHasRight.md)
