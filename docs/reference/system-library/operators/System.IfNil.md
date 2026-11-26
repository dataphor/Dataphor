# System.IfNil

**Declarations**

```d4
create operator .System.IfNil(const ANilableValue : generic, const AReplacementValue : generic) : generic
 class "System.IfNilNode";
```

**Remarks**

`IfNil` is a shorthand. The expression

`IfNil(a, b)`

is shorthand for

`if IsNil(a) then b else a`

**Using System.IfNil**

```d4
select Employee left join EmployeePhone 
   redefine { Phone := IfNil(Phone, "No Phone") };
```

```text
ID Name Phone
-- ---- --------
1  Joe  555-1000
1  Joe  555-1234
2  John 555-4321
3  Josh No Phone
4  Jeff 555-4444
5  Jake 555-5678
6  Jeff No Phone
```

**See Also**

[System.IsNil](System.IsNil.md) \| [System.IsNotNil](System.IsNotNil.md) \| [Nil Semantics](../../../developers-guide/d4-language-elements.md#nil-semantics)
