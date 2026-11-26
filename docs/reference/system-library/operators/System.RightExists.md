# System.RightExists

**Declarations**

```d4
create operator .System.RightExists(const ARightName : System.Name) : System.Boolean
 class "System.SystemRightExistsNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`RightExists` returns `true` if the specified `Right` exists, otherwise it returns `false`.

**Using System.RightExists**

```d4
CreateRight("AddProgram");

select if RightExists("AddProgram") 
    then "The AddProgram right exists"
    else "The AddProgram right does NOT exists";

DropRight("AddProgram");
```

```text
The AddProgram right exists
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [System.CreateRight](System.CreateRight.md) \| [System.DropRight](System.DropRight.md)
