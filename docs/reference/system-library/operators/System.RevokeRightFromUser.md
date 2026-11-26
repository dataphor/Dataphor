# System.RevokeRightFromUser

**Declarations**

```d4
create operator .System.RevokeRightFromUser(const ARightName : System.Name, const AUserID : System.UserID)
 class "System.SystemRevokeRightFromUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RevokeRightFromUser` revokes the specified `Right` from the specified `User`.

**Using System.RevokeRightFromUser**

```d4
CreateRight("AddProgram");
CreateRole("SoftwareDeveloper");
CreateUser("JS1", "John Smith", "Password");
AddUserToRole("JS1", "SoftwareDeveloper");

GrantRightToRole("AddProgram", "SoftwareDeveloper");
select UserHasRight("JS1", "AddProgram");

RevokeRightFromUser("AddProgram", "JS1");
select UserHasRight("JS1", "AddProgram");select RoleRightAssignments where Right_Name = "AddProgram";
select UserRightAssignments where User_ID = "JS1";
DropUser("JS1");
DropRole("SoftwareDeveloper");
DropRight("AddProgram");
```

```text
true
false

Role_Name              Right_Name Granted
---------------------- ---------- -------
Docs.SoftwareDeveloper AddProgram True

User_ID Right_Name Granted 
------- ---------- ------- 
JS1     AddProgram False 
```

Note in this example, without the `Revoke` the user JS1 would have the AddProgram `Right` since he is a member of the SoftwareDeveloper `Role`.

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateUser](System.CreateUser.md) \| [System.DropRight](System.DropRight.md) \| [System.DropUser](System.DropUser.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RevertRightForUser](System.RevertRightForUser.md) \| [System.RightExists](System.RightExists.md) \| [System.UserExists](System.UserExists.md)
