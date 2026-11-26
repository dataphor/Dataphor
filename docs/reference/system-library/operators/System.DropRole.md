# System.DropRole

**Declarations**

```d4
create operator .System.DropRole(const ARoleName : System.Name)
 class "System.SystemDropRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DropRole` deletes the specified role.

**Using System.DropRole**

```d4
CreateRole("SoftwareDeveloper");

DropRole("SoftwareDeveloper");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.AddUserToRole](System.AddUserToRole.md) \| [System.CreateRole](System.CreateRole.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.RemoveUserFromRole](System.RemoveUserFromRole.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md)
