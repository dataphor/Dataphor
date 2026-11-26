# System.Lower

**Declarations**

```d4
create operator .System.Lower(const AString : System.String) : System.String
 class "System.StringLowerNode";
```

**Remarks**

`Lower` returns the lower case equivalent of the specified string.

**Using System.Lower**

```d4
select Employee add {Name.Lower() LowerName};
```

```text
ID Name LowerName
-- ---- ---------
1  Joe  joe
2  John john
3  Josh josh
4  Jeff jeff
5  Jake jake
6  Jeff jeff
```

**See Also**

[System.Upper](System.Upper.md) \| [System.IsUpper](System.IsUpper.md) \| [System.IsLower](System.IsLower.md) \| [System.String Scalar Type](../types/System.String.md)
