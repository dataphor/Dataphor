# System.SetUserName

**Declarations**

```d4
create operator .System.SetUserName(const AUserID : System.UserID, const AUserName : System.String)
 class "System.SystemSetUserNameNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetUserName` sets the User Name for the specified User ID.

**Using System.SetUserName**

```d4
CreateUser("JS1", "J. Smith" "Password");
SetUserName("JS1", "John Smith");
DropUser("JS1");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](System.DropUser.md) \| [System.UserExists](System.UserExists.md) \| [System.SetPassword](System.SetPassword.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md)
