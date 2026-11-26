# System.IsNil

**Declarations**

```d4
create operator .System.IsNil(const AValue : generic) : System.Boolean
 class "System.IsNilNode";
create operator .System.IsNil(const AValue : generic row, const AColumnName : System.String) : System.Boolean
 class "System.IsNilRowNode";
```

**Remarks**

`IsNil` returns true if its argument is nil, false otherwise. The row-valued overload returns true if the column specified in `AColumnName` is nil, false otherwise.

**Using System.IsNil**

```d4
select Employee
 left join EmployeePhone 
 add { not IsNil(Phone) HasPhone };
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

[System.IfNil](System.IfNil.md) \| [System.IsNotNil](System.IsNotNil.md) \| [Nil Semantics](../../../developers-guide/d4-language-elements.md#nil-semantics)
