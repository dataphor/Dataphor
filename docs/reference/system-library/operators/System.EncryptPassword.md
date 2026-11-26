# System.EncryptPassword

**Declarations**

```d4
create operator .System.EncryptPassword(const APassword : System.String) : System.String
 class "System.SystemEncryptPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

Returns the encrypted version of the specified string.

**Using System.EncryptPassword**

```d4
select EncryptPassword("MyPassword");
```

```text
EE0ixxdSnzyfSyw2APBOtG8=
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.ChangePassword](System.ChangePassword.md) \| [System.CreateUser](System.CreateUser.md) \| [System.DropUser](System.DropUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md)
