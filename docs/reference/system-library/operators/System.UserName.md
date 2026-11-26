# System.UserName

**Declarations**

```d4
create operator .System.UserName() : System.String
 class "System.SystemUserNameNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

`UserName` returns the UserName for the current Dataphor user.

**Using System.UserName**

```d4
select UserName();
```

```text
Administrator
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](System.DropUser.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.UserExists](System.UserExists.md) \| [System.UserID](System.UserID.md) \| [Session Information](../../../users-guide/aliases.md#session-information)
