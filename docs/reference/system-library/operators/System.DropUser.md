# System.DropUser

**Declarations**

```d4
create operator .System.DropUser(const AUserID : System.UserID)
 class "System.SystemDropUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DropUser` drops the user specified by `AUserID`. This user will no longer be able to login to Dataphor. Users must have the `System.DropUser` right in order to invoke this operator.

**Using System.DropUser**

```d4
CreateUser("JS1", "John Smith", "Password");
DropUser("JS1");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RemoveUserFromRole](System.RemoveUserFromRole.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.UserExists](System.UserExists.md) \| [System.UserHasRight](System.UserHasRight.md)
