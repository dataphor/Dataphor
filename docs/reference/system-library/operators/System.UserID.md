# System.UserID

**Declarations**

```d4
create operator .System.UserID(const Value : System.String) : System.UserID
 class "System.ScalarSelectorNode";
create operator .System.UserID() : System.UserID
 class "System.SystemUserIDNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

`UserID` returns the `UserID` for the current Dataphor user.

**Using System.UserID**

```d4
select UserID();
```

```text
Admin
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.UserID Scalar Type](../types/System.UserID.md) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](System.DropUser.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.UserExists](System.UserExists.md) \| [System.UserName](System.UserName.md) \| [Session Information](../../../users-guide/aliases.md#session-information)
