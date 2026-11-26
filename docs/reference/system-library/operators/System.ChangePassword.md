# System.ChangePassword

**Declarations**

```d4
create operator .System.ChangePassword(const AOldPassword : System.String, const ANewPassword : System.String)
 class "System.SystemChangePasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`ChangePassword` changes the password for the current Dataphor user.

**Using System.ChangePassword**

```d4
ChangePassword("OldPassword", "NewPassword");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [Session Information](../../../users-guide/aliases.md#session-information)
