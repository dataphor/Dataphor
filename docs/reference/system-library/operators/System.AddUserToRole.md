# System.AddUserToRole

**Declarations**

```d4
create operator .System.AddUserToRole(const AUserID : System.UserID, const ARoleName : System.Name)
 class "System.SystemAddUserToRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`AddUserToRole` adds the user to the role

**Using System.AddUserToRole**

```d4
CreateRole("SoftwareDeveloper");
CreateRight("AddProgram");
GrantRightToRole("AddProgram", "SoftwareDeveloper");

CreateUser("JS1", "John Smith", "Password");
AddUserToRole("JS1", "SoftwareDeveloper");

DropUser("JS1");
DropRight("AddProgram");
DropRole("SoftwareDeveloper");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRole](System.CreateRole.md) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.RemoveUserFromRole](System.RemoveUserFromRole.md) \| [System.RoleExists](System.RoleExists.md) \| [System.RoleHasRight](System.RoleHasRight.md) \| [System.UserExists](System.UserExists.md)
