# System.GrantRightToRole

**Declarations**

```d4
create operator .System.GrantRightToRole(const ARightName : System.Name, const ARoleName : System.Name)
 class "System.SystemGrantRightToRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`GrantRightToRole` assigns the specified right to the specified role.

**Using System.GrantRightToRole**

```d4
CreateRole("SoftwareDeveloper");
CreateRight("AddProgram");

GrantRightToRole("AddProgram", "SoftwareDeveloper");

DropRight("AddProgram");
DropRole("SoftwareDeveloper");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateRole](System.CreateRole.md) \| [System.DropRight](System.DropRight.md) \| [System.DropRole](System.DropRole.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md) \| [System.RightExists](System.RightExists.md) \| [System.RoleExists](System.RoleExists.md) \| [System.RoleHasRight](System.RoleHasRight.md)
