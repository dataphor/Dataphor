# System.Upper

**Declarations**

```d4
create operator .System.Upper(const AString : System.String) : System.String
 class "System.StringUpperNode";
```

**Remarks**

`Upper` returns the upper case equivalent of the specified string.

**Using System.Upper**

```d4
select Employee add {Name.Upper() UpperName};
```

```text
ID Name UpperName 
-- ---- --------- 
1  Joe  JOE
2  John JOHN
3  Josh JOSH
4  Jeff JEFF
5  Jake JAKE
6  Jeff JEFF
```

**See Also**

[System.String Scalar Type](../types/System.String.md) \| [System.Lower](System.Lower.md) \| [System.IsUpper](System.IsUpper.md) \| [System.IsLower](System.IsLower.md)
