# System.SetObjectOwner

**Declarations**

```d4
create operator .System.SetObjectOwner(const AObjectName : System.Name, const AUserID : System.UserID)
 class "System.SystemSetObjectOwnerNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

The owner of an object can be changed using the `SetObjectOwner` operator. All rights for the object are implicitly available to the owner of the object.

**Using System.SetObjectOwner**

```d4
CreateUser("JS1", "John Smith", "Password");
create table Example in Temp 
{ 
    UnTrimmed : String, 
    Trimmed : String, 
    key { }
};

select UserHasRight("Admin","Sample.ExampleAlter");
select UserHasRight("JS1","Sample.ExampleAlter");

SetObjectOwner("Sample.Example","JS1");
select UserHasRight("Admin","Sample.ExampleAlter");
select UserHasRight("JS1","Sample.ExampleAlter");

drop table Example;
DropUser("JS1");
```

```text
True
False

True
True
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Objects](../../../developers-guide/d4-catalog.md#objects) \| [System.ObjectExists](System.ObjectExists.md) \| [System.CreateRight](System.CreateRight.md) \| [System.CreateUser](System.CreateUser.md) \| [System.GrantRightToUser](System.GrantRightToUser.md) \| [System.RightExists](System.RightExists.md) \| [System.UserExists](System.UserExists.md)
