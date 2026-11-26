# System.SetRightOwner

**Declarations**

```d4
create operator .System.SetRightOwner(const ARightName : System.Name, const AUserID : System.UserID)
 class "System.SystemSetRightOwnerNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetRightOwner` is used to set `AUserID` as the owner of `ARightName`. The current user must have sufficient rights to `ARightName` and `AUserID`.

**Using System.SetRightOwner**

```d4
CreateRight("AddProgram");

select Rights where Name = "AddProgram";
CreateUser("JS1", "John Smith", "Password");
SetRightOwner("AddProgram", "JS1");
select Rights where Name = "AddProgram";

DropUser("JS1");
DropRight("AddProgram");
```

```text
Name       Owner_User_ID
---------- -------------
AddProgram Admin

Name       Owner_User_ID
---------- -------------
AddProgram JS1
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateUser](System.CreateUser.md) \| [System.DropRight](System.DropRight.md) \| [System.DropUser](System.DropUser.md) \| [System.RightExists](System.RightExists.md) \| [System.UserExists](System.UserExists.md)
