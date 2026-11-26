# System.UserHasRight

**Declarations**

```d4
create operator .System.UserHasRight(const AUserID : System.UserID, const ARightName : System.Name) : System.Boolean
 class "System.SystemUserHasRightNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`UserHasRight` returns true if the specified User has the specified Right.

**Using System.UserHasRight**

```d4
CreateRole("SoftwareDeveloper");
CreateRight("AddProgram");
CreateUser("JS1", "John Smith", "Password");
AddUserToRole("JS1", "SoftwareDeveloper");

select UserHasRight("JS1", "AddProgram");
GrantRightToRole("AddProgram", "SoftwareDeveloper");
select UserHasRight("JS1", "AddProgram");

DropUser("JS1");
DropRight("AddProgram");
DropRole("SoftwareDeveloper");
```

```text
False
True
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateUser](System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](System.CreateUserWithEncryptedPassword.md) \| [System.DropRight](System.DropRight.md) \| [System.DropUser](System.DropUser.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RevokeRightFromUser](System.RevokeRightFromUser.md) \| [System.RightExists](System.RightExists.md) \| [System.UserExists](System.UserExists.md)
