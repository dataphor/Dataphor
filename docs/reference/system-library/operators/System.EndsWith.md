# System.EndsWith

**Declarations**

```d4
create operator .System.EndsWith(const AString : System.String, const ASubString : System.String) : System.Boolean
 class "System.StringEndsWith";
```

**Remarks**

The `EndsWith` operator does a case-sensitive comparison of `ASubString` and the equivalent number of characters at the end of `AString`. If there is an exact match `EndsWith` returns `true`, otherwise it returns `false`.

**Using System.EndsWith**

```d4
select "Dataphoria".EndsWith("ria");
```

```text
True
```

**See Also**

[System.String Scalar Type](../types/System.String.md) \| [System.StartsWith](System.StartsWith.md) \| [System.IndexOf](System.IndexOf.md) \| [System.LastIndexOf](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md)
