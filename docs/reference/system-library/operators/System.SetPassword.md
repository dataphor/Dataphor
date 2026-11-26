# System.SetPassword

**Declarations**

```d4
create operator .System.SetPassword(const AUserID : System.UserID, const APassword : System.String)
 class "System.SystemSetPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetPassword` sets the password for the specified user.

**Using System.SetPassword**

```d4
CreateUser("JS1", "John Smith", "FirstPassword");
SetPassword("JS1", "NewPassword");
DropUser("JS1");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](System.DropUser.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.UserExists](System.UserExists.md)
