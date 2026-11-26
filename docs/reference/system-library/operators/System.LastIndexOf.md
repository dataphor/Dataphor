# System.LastIndexOf

**Declarations**

```d4
create operator .System.LastIndexOf(const AList : generic list, const AValue : generic) : System.Integer
 class "System.ListLastIndexOfNode";
create operator .System.LastIndexOf(const AList : generic list, const AValue : generic, const AStartIndex : System.Integer) : System.Integer
 class "System.ListLastIndexOfStartNode";
create operator .System.LastIndexOf(const AList : generic list, const AValue : generic, const AStartIndex : System.Integer, const ALength : System.Integer) : System.Integer
 class "System.ListLastIndexOfStartLengthNode";
```

**Remarks**

`LastIndexOf` returns an `Integer` value identifying index of the *last* occurrence of `AValue` in the given list. If `AValue` is not found in `AList`, -1 is returned.

Overloads that include a starting index parameter will search the list backwards from the given index, ignoring any following items. Overloads that do not include this parameter assume the last item. If the given index is less than zero, it is assumed to be zero; if it is greater than the list length, then the list length is assumed. Overloads that include a length parameter will search up to ALength number of items before the starting index. If ALength exceeds the list bounds then the maximum possible length is used. Specifying an ALength value of less than zero will raise an error.

> [!NOTE]
> String and list indexes are zero based.

**Using System.LastIndexOf**

```d4
select { "A", "B", "A" }.IndexOf("A");
select { "A", "B", "A" }.LastIndexOf("A");
```

```text
0
2
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf](System.IndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression) \| [System.String Scalar Type](../types/System.String.md)

## System.LastIndexOf (String)

**Declarations**

```d4
create operator .System.LastIndexOf(const AString : System.String, const ASubString : System.String) : System.Integer
 class "System.StringLastIndexOfNode";
create operator .System.LastIndexOf(const AString : System.String, const ASubString : System.String, const AStartIndex : System.Integer) : System.Integer
 class "System.StringLastIndexOfStartNode";
create operator .System.LastIndexOf(const AString : System.String, const ASubString : System.String, const AStartIndex : System.Integer, const ALength : System.Integer) : System.Integer
 class "System.StringLastIndexOfStartLengthNode";
```

**Remarks**

`LastIndexOf` returns an `Integer` value identifying starting position in `AString` of the *last* occurrence of `ASubString`. If `ASubString` is not found in `AString`, -1 is returned.

Overloads that include a starting index parameter will search the string backwards from the given index, ignoring any following characters. Overloads that do not include this parameter assume the last character. If this index is less than zero, it is assumed to be zero; if it is greater than the string length, then the string length is assumed. Overloads that include a length parameter will search for up to ALength number of characters before the starting index. If ALength exceeds the string bounds then the maximum possible length is used. Specifying an ALength value of less than zero will raise an error.

`LastIndexOf` is similar to the `LastPos` operator. The difference is in the order of the parameters. Using method-style invocation, the operator is invoked using the calling variable as the first argument of the operator. Though the `LastPos` operator makes more sense when using standard invocation, this operator makes more sense when using method-style invocation.

> [!NOTE]
> String and list indexes are zero based.

**Using System.LastIndexOf**

```d4
select "abcabcabc".IndexOf("b");
select "abcabcabc".LastIndexOf("b");
select "abcabcabc".LastIndexOf("b", 3, 3);
select "abcabcabc".LastIndexOf("x");
```

```text
1
7
1
-1
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf](System.IndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression) \| [System.String Scalar Type](../types/System.String.md)
