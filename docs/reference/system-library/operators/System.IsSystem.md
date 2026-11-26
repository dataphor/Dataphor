# System.IsSystem

**Declarations**

```d4
create operator .System.IsSystem(const AName : System.Name) : System.Boolean
 class "System.ObjectIsSystemNode" static tags { DAE.IsRemotable = "False" };
create operator .System.IsSystem(const ASpecifier : System.String) : System.Boolean
 class "System.ObjectIsSystemNode" static tags { DAE.IsRemotable = "False" };
create operator .System.IsSystem(const AID : System.Integer) : System.Boolean
 class "System.ObjectIsSystemNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`IsSystem` checks to see if the specified object is a system object.

**Using System.IsSystem**

```d4
select IsSystem("IsGenerated_System_String_"); //System operator
select IsSystem("Derive_System_String_"); //Frontend operator
```

```text
True
False
```

**See Also**

[System.IsGenerated](System.IsGenerated.md) \| [Objects](../../../developers-guide/d4-catalog.md#objects)
