# System.ObjectName

**Declarations**

```d4
create operator .System.ObjectName(const ASpecifier : System.String) : System.Name
 class "System.FullObjectNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectName(const AName : System.Name) : System.Name
 class "System.FullObjectNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectName(const AID : System.Integer) : System.Name
 class "System.SystemObjectNameNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ObjectName` takes either an ID or an unqualified name as a `Name` or `String`. The fully qualified name will be returned.

**Using System.ObjectName**

```d4
select ObjectName("TableVars");
```

```text
System.TableVars
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [System.ObjectID](System.ObjectID.md)
