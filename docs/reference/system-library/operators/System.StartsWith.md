# System.StartsWith

**Declarations**

```d4
create operator .System.StartsWith(const AString : System.String, const ASubString : System.String) : System.Boolean
 class "System.StringStartsWith";
```

**Remarks**

`StartsWith` does a case-sensitive comparison of `ASubString` and the equivalent number of characters at the beginning of `AString`. If there is an exact match `StartsWith` returns `true`, otherwise it returns `false`.

**Using System.StartsWith**

```d4
select "Dataphoria".StartsWith("Data");
```

```text
True
```

**See Also**

[System.String Scalar Type](../types/System.String.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf (String)](System.IndexOf.md) \| [System.LastIndexOf (String)](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md)
