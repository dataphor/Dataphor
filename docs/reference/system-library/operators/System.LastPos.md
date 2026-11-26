# System.LastPos

**Declarations**

```d4
create operator .System.LastPos(const ASubString : System.String, const AString : System.String) : System.Integer
 class "System.StringLastPosNode";
```

**Remarks**

`LastPOS` returns an `Integer` value identifying the starting position in `AString` of the LAST occurrence of `ASubString`. If `ASubString` is not found in `AString`, -1 is returned.

`LastPOS` is similar to the `LastIndexOf` operator. The difference is in the order of the parameters. Using method-style invocation, the operator is invoked using the calling argument as the first argument of the operator. Though the `LastIndexOf` operator makes more sense when using method-style invocation, this operator makes more sense when called using standard invocation.

> [!NOTE]
> String and list indexes are zero based.

**Using System.LastPos**

```d4
select Pos("b", "abcabcabc");
select LastPos("b", "abcabcabc");
select if LastPos("x", "abcabcabc") >= 0 then "Found it" else "Not found";
```

```text
1
7
Not found
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf](System.IndexOf.md) \| [System.LastIndexOf (String)](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression) \| [System.String Scalar Type](../types/System.String.md)
