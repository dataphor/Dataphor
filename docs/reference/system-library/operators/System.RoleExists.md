# System.RoleExists

**Declarations**

```d4
create operator .System.RoleExists(const ARoleName : System.Name) : System.Boolean
 class "System.SystemRoleExistsNode" static tags { DAE.IsRemotable = "False", DAE.IsLiteral = "False" };
```

**Remarks**

`RoleExists` returns `true` if the specified `Role` exists, otherwise it returns `false`.

**Using System.RoleExists**

```d4
CreateRole("SoftwareDeveloper");

select
 if RoleExists("SoftwareDeveloper") 
  then "The SoftwareDeveloper role exists"
  else "The SoftwareDeveloper role does NOT exists";

DropRole("SoftwareDeveloper");
```

```text
The SoftwareDeveloper role exists
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRole](System.CreateRole.md) \| [System.DropRole](System.DropRole.md)
