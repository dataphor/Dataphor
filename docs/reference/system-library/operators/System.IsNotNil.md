# System.IsNotNil

**Remarks**

`IsNotNil` returns true if its argument has a value, false otherwise. The row-valued overload returns true if the column specified in `AColumnName` has a value, false otherwise.

**Using System.IsNotNil**

```d4
select Employee
 left join EmployeePhone 
 add { IsNotNil(Phone) HasPhone };
```

```text
ID Name Phone      HasPhone
-- ---- ---------- --------
1  Joe  555-1000   True
1  Joe  555-1234   True
2  John 555-4321   True
3  Josh <no value> False
4  Jeff 555-4444   True
5  Jake 555-5678   True
6  Jeff <no value> False
```

**See Also**

[System.IfNil](System.IfNil.md) \| [System.IsNil](System.IsNil.md) \| [Nil Semantics](../../../developers-guide/d4-language-elements.md#nil-semantics)
