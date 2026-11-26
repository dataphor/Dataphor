# System.RemoveUserFromRole

**Declarations**

```d4
create operator .System.RemoveUserFromRole(const AUserID : System.UserID, const ARoleName : System.Name)
 class "System.SystemRemoveUserFromRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RemoveUserFromRole` removes the specified `User` as a member of the specified `Role`.

**Using System.RemoveUserFromRole**

```d4
CreateRole("SoftwareDeveloper");
CreateRole("SoftwareTester");
CreateUser("JS1", "John Smith", "Password");

AddUserToRole("JS1", "SoftwareDeveloper");
AddUserToRole("JS1", "SoftwareTester");

select UserRoles where User_ID = "JS1";
RemoveUserFromRole("JS1", "SoftwareDeveloper");
select UserRoles where User_ID = "JS1";

DropUser("JS1");
DropRole("SoftwareDeveloper");DropRole("SoftwareTester");
```

```text
User_ID Role_Name             
------- ----------------------
JS1     Test.SoftwareDeveloper
JS1     Test.SoftwareTester

User_ID Role_Name             
------- ----------------------
JS1     Test.SoftwareTester
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.CreateRole](System.CreateRole.md) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropRole](System.DropRole.md) \| [System.DropUser](System.DropUser.md) \| [System.RoleExists](System.RoleExists.md) \| [System.SetEncryptedPassword](System.SetEncryptedPassword.md) \| [System.SetPassword](System.SetPassword.md) \| [System.UserExists](System.UserExists.md)
