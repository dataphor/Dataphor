# System.Pos

**Declarations**

```d4
create operator .System.Pos(const ASubString : System.String, const AString : System.String) : System.Integer
 class "System.StringPosNode";
```

**Remarks**

`Pos` returns an `Integer` value identifying the starting position in `AString` of the FIRST occurrence of `ASubString`. If `ASubString` is not found in `AString`, -1 is returned.

`Pos` is similar to the `IndexOf` operator. The difference is in the order of the parameters. Using method-style invocation, the operator is invoked passing the calling argument as the first argument to the operator. Though the `IndexOf` operator makes more sense when using method-style invocation, this operator makes more sense when using standard invocation.

> [!NOTE]
> String and list indexes are zero based.

**Using System.Pos**

```d4
select Pos("b", "abcabcabc");
select LastPos("b", "abcabcabc");
select if Pos("x", "abcabcabc") >= 0 then "Found it" else "Not found";
```

```text
1
7
Not found
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf (String)](System.IndexOf.md) \| [System.LastIndexOf (String)](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression) \| [System.String Scalar Type](../types/System.String.md)
