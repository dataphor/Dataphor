# System.RevertRightForRole

**Declarations**

```d4
create operator .System.RevertRightForRole(const ARightName : System.Name, const ARoleName : System.Name)
 class "System.SystemRevertRightForRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RevertRightForRole` removes any reference (Grant or Revoke) to the specified `Right` from the specified `Role`.

**Using System.RevertRightForRole**

```d4
CreateRight("AddProgram");
CreateRole("SoftwareDeveloper");

GrantRightToRole("AddProgram", "SoftwareDeveloper");
select RoleHasRight("SoftwareDeveloper", "AddProgram");

RevertRightForRole("AddProgram", "SoftwareDeveloper");
select RoleHasRight("SoftwareDeveloper", "AddProgram");

DropRole("SoftwareDeveloper");
DropRight("AddProgram");
```

```text
true
false
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateRole](System.CreateRole.md) \| [System.DropRight](System.DropRight.md) \| [System.DropRole](System.DropRole.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md) \| [System.RightExists](System.RightExists.md) \| [System.RoleExists](System.RoleExists.md) \| [System.RoleHasRight](System.RoleHasRight.md)
