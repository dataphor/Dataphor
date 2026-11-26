# System.CreateUserWithEncryptedPassword

**Declarations**

```d4
create operator .System.CreateUserWithEncryptedPassword(const AUserID : System.UserID, const AUserName : System.String, const AEncryptedPassword : System.String)
 class "System.SystemCreateUserWithEncryptedPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.CreateUserWithEncryptedPassword(const AUserID : System.UserID, const AUserName : System.String, const AEncryptedPassword : System.String, const AGroupName : System.String)
 class "System.SystemCreateUserWithEncryptedPasswordNode"
    static tags
    { 
        
        DAE.IsRemotable = "False", 
        DAE.IsFunctional = "False", 
        DAE.IsDeprecated = "True"
    };
```

**Remarks**

Creates a user with the password specified by the encrypted string.

**Using System.CreateUserWithEncryptedPassword**

```d4
select EncryptPassword("Password");
```

```text
ED9BnEp1mumPakUrawTrdYA=
```

```d4
CreateUserWithEncryptedPassword
(
  "JS1",
  "JohnSmith1",
  "ED9BnEp1mumPakUrawTrdYA="
);

DropUser("JS1");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateRole](System.CreateRole.md) \| [System.CreateUser](System.CreateUser.md) \| [System.ChangePassword](System.ChangePassword.md) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.DropUser](System.DropUser.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RemoveUserFromRole](System.RemoveUserFromRole.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.SetUserName](System.SetUserName.md) \| [System.UserExists](System.UserExists.md) \| [System.UserHasRight](System.UserHasRight.md)
