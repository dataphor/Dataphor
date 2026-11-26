# System.CreateRole

**Declarations**

```d4
create operator .System.CreateRole(const ARoleName : System.Name)
 class "System.SystemCreateRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

A role is a group of right assignments in the Dataphor Server. The Dataphor Server defines several system roles which it uses to manage basic system rights. Users can be assigned to any number of roles. When a user is a member of a role, the rights assigned to that role are available to the user. A role is a catalog object with rights and ownership.

**Using System.CreateRole**

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

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateRight](System.CreateRight.md) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.DropRole](System.DropRole.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.RemoveUserFromRole](System.RemoveUserFromRole.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md) \| [System.RoleExists](System.RoleExists.md) \| [System.RoleHasRight](System.RoleHasRight.md)
