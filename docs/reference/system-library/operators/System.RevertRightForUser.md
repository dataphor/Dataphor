# System.RevertRightForUser

**Declarations**

```d4
create operator .System.RevertRightForUser(const ARightName : System.Name, const AUserID : System.UserID)
 class "System.SystemRevertRightForUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RevertRightForUser` removes any reference (Grant or Revoke) to the specified `Right` from the specified `User`.

**Using System.RevertRightForUser**

```d4
CreateRight("AddProgram");
CreateUser("JS1", "John Smith", "Password");

GrantRightToUser("AddProgram", "JS1");
select UserHasRight("JS1", "AddProgram");

RevertRightForUser("AddProgram", "JS1");
select UserHasRight("JS1", "AddProgram");

DropUser("JS1");
DropRight("AddProgram");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropRight](System.DropRight.md) \| [System.DropUser](System.DropUser.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.RightExists](System.RightExists.md) \| [System.UserExists](System.UserExists.md) \| [System.UserHasRight](System.UserHasRight.md)
