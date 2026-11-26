# System.CreateRight

**Declarations**

```d4
create operator .System.CreateRight(const ARightName : System.Name)
 class "System.SystemCreateRightNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.CreateRight(const ARightName : System.Name, const AUserID : System.UserID)
 class "System.SystemCreateRightNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

A right is the basic unit of permission in the Dataphor Server. Any operation which must be protected has an associated right. Rights are then assigned to users and roles to allow users access to the various objects and functions of the Dataphor Server. A right can be granted or revoked, and a right assignment can be reverted. The Dataphor Server defines several system rights which protect basic Dataphor Server functions such as DDL statements.

Whenever a catalog object is created, the rights necessary to protect operations on that object are also created. For example, creating a table creates select, insert, update, and delete rights for that table, as well as alter and drop rights.

The user that creates a catalog item has full rights to that item.

Because a right is not a catalog object, its name is only required to be unique among the other rights in the system, not among all the catalog objects.

If the UserID is not specified, the current user will be the owner of the right. If the UserID is specified, the current user must have the `System.AlterUser` right and the specified user will be the owner of the right.

**Using System.CreateRight**

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

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRole](System.CreateRole.md) \| [System.CreateUser](System.CreateUser.md) \| [System.DropRight](System.DropRight.md) \| [System.GrantRightToRole](System.GrantRightToRole.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RevertRightForRole](System.RevertRightForRole.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RevokeRightFromRole](System.RevokeRightFromRole.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.RightExists](System.RightExists.md) \| [System.UserHasRight](System.UserHasRight.md)
