# System.DropRight

**Declarations**

```d4
create operator .System.DropRight(const ARightName : System.Name)
 class "System.SystemDropRightNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DropRight` deletes the specified right.

**Using System.DropRight**

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

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.RightExists](System.RightExists.md) \| [System.RoleHasRight](System.RoleHasRight.md) \| [System.UserHasRight](System.UserHasRight.md)
