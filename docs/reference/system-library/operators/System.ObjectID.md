# System.ObjectID

**Declarations**

```d4
create operator .System.ObjectID(const AName : System.Name) : System.Integer
 class "System.SystemObjectIDNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectID(const ASpecifier : System.String) : System.Integer
 class "System.SystemObjectIDNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ObjectID` returns the ID (as an Integer) for the specified object name. Many of the objects in the system catalog are operators that have overloads. This Operator requires the full object name, not the abbreviated one.

**Using System.ObjectID**

```d4
select Operators
   where ( OperatorName = "System.Avg" )
   add { ObjectID(Name) ObjID }
   over { Name, ObjID };
```

```text
Name                        ObjID
--------------------------- -----
System.Avg_System_Decimal_  4366
System.Avg_System_Integer_  4357
System.Avg_System_Money_    4374
System.Avg_System_TimeSpan_ 4392
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [System.ObjectName](System.ObjectName.md)
