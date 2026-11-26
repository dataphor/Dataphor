# System.RevokeRightFromRole

**Declarations**

```d4
create operator .System.RevokeRightFromRole(const ARightName : System.Name, const ARoleName : System.Name)
 class "System.SystemRevokeRightFromRoleNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RevokeRightFromRole` revokes the specified `Right` from the specified `Role`.

**Using System.RevokeRightFromRole**

```d4
CreateRight("AddProgram");
CreateRole("SoftwareDeveloper");
CreateRole("SoftwareTester");

GrantRightToRole("AddProgram", "SoftwareDeveloper");
RevokeRightFromRole("AddProgram", "SoftwareTester");
select RoleRightAssignments 
  where Right_Name = "AddProgram";

DropRight("AddProgram");
DropRole("SoftwareDeveloper");DropRole("SoftwareTester");
```

```text
Role_Name              Right_Name Granted
---------------------- ---------- -------
Docs.SoftwareDeveloper AddProgram True
Docs.SoftwareTester    AddProgram False
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateRole](System.CreateRole.md) \| [System.DropRight](System.DropRight.md) \| [System.DropRole](System.DropRole.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RightExists](System.RightExists.md) \| [System.RoleExists](System.RoleExists.md)
