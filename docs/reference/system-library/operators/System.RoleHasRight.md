# System.RoleHasRight

**Declarations**

```d4
create operator .System.RoleHasRight(const ARoleName : System.Name, const ARightName : System.Name) : System.Boolean
 class "System.SystemRoleHasRightNode" static tags { DAE.IsRemotable = "False", DAE.IsLiteral = "False" };
```

**Remarks**

`RoleHasRight` returns `true` if the specified `Role` has the specified `Right`, otherwise it returns `false`.

**Using System.RoleHasRight**

```d4
CreateRight("AddProgram");
CreateRole("SoftwareDeveloper");

select RoleHasRight("SoftwareDeveloper", "AddProgram");
GrantRightToRole("AddProgram", "SoftwareDeveloper");
select RoleHasRight("SoftwareDeveloper", "AddProgram");

DropRole("SoftwareDeveloper");
DropRight("AddProgram");
```

```text
false
true
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md)
