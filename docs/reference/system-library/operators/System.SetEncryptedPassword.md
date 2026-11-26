# System.SetEncryptedPassword

**Declarations**

```d4
create operator .System.SetEncryptedPassword(const AUserID : System.UserID, const AEncryptedPassword : System.String)
 class "System.SystemSetEncryptedPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetEncryptedPassword` sets the specified user's password the the specified encrypted password.

**Using System.SetEncryptedPassword**

```d4
select EncryptPassword("aNewPassword");
```

```text
EFSF/0npXuFJdlKFRj0JnhQ=
```

```d4
CreateUser("JS1", "John Smith", "aPasswordToUse");
SetEncryptedPassword("JS1", "EFSF/0npXuFJdlKFRj0JnhQ=");
DropUser("JS1");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](System.DropUser.md) \| [System.UserExists](System.UserExists.md)
