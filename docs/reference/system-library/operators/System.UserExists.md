# System.UserExists

**Declarations**

```d4
create operator .System.UserExists(const AUserID : System.UserID) : System.Boolean
 class "System.SystemUserExistsNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`UserExists` returns `true` if the specified `User` exists, otherwise it returns `false`.

**Using System.UserExists**

```d4
select UserExists("JS1");
CreateUser("JS1", "John Smith", "Password");
select UserExists("JS1");
DropUser("JS1");
```

```text
False
True
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](System.DropUser.md) \| [System.SetPassword](System.SetPassword.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md)
