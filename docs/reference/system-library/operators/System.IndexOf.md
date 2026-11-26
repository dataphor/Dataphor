# System.IndexOf

**Declarations**

```d4
create operator .System.IndexOf(const AList : generic list, const AValue : generic) : System.Integer
 class "System.ListIndexOfNode";
create operator .System.IndexOf(const AList : generic list, const AValue : generic, const AStartIndex : System.Integer) : System.Integer
 class "System.ListIndexOfStartNode";
create operator .System.IndexOf(const AList : generic list, const AValue : generic, const AStartIndex : System.Integer, const ALength : System.Integer) : System.Integer
 class "System.ListIndexOfStartLengthNode";
```

**Remarks**

`IndexOf` returns the index of `AValue` within `AList`. If the value is not in the list, -1 is returned.

Overloads that include a starting index parameter will search from the given index, ignoring any preceding items. Overloads that do not include this parameter assume the first item (index of zero). If this index is less than zero, it is assumed to be zero; if it is greater than the list length, then the list length is assumed. Overloads that include a length parameter will search for up to ALength number of items after the starting index. If ALength exceeds the list length then the list length is used. Specifying an ALength value of less than zero will raise an error.

> [!NOTE]
> List and string indexes are zero based.

**Using System.IndexOf**

```d4
select list(String){ "a", "b", "c", "b" }.IndexOf("b");
select list(String){ "a", "b", "c", "b" }.IndexOf("x");
```

```text
1
-1
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.LastIndexOf (List)](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [System.String Scalar Type](../types/System.String.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression)

## System.IndexOf (List Operators)

**Remarks**

See [System.IndexOf](System.IndexOf.md)

## System.IndexOf (String)

**Declarations**

```d4
create operator .System.IndexOf(const AString : System.String, const ASubString : System.String) : System.Integer
 class "System.StringIndexOfNode";
create operator .System.IndexOf(const AString : System.String, const ASubString : System.String, const AStartIndex : System.Integer) : System.Integer
 class "System.StringIndexOfStartNode";
create operator .System.IndexOf(const AString : System.String, const ASubString : System.String, const AStartIndex : System.Integer, const ALength : System.Integer) : System.Integer
 class "System.StringIndexOfStartLengthNode";
```

**Remarks**

`IndexOf` returns an `Integer` value identifying the starting position in `AString` of the *first* occurrence of `ASubString`. If `ASubString` is not found in `AString`, -1 is returned.

Overloads that include a starting index parameter will search from the given index, ignoring any preceding characters. Overloads that do not include this parameter assume the first character (index of zero). If this index is less than zero, it is assumed to be zero; if it is greater than the string length, then the string length is assumed. Overloads that include a length parameter will search for up to ALength number of characters after the starting index. If ALength exceeds the string length then the string length is used. Specifying an ALength value of less than zero will raise an error.

`IndexOf` is similar to the `Pos` operator. The difference is in the order of the parameters. Using method-style invocation, the operator is invoked using the calling variable as the first argument of the operator. Though the `Pos` operator makes more sense when calling using standard syntax, this operator makes more sense when called using method-style invocation.

> [!NOTE]
> List and string indexes are zero based.

**Using System.IndexOf**

```d4
select "abcabcabc".IndexOf("b");
select "abcabcabc".LastIndexOf("b");
select "abcabcabc".IndexOf("x");
select "abcabcabc".IndexOf("b", 3, 3);
```

```text
1
7
-1
4
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.LastIndexOf](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [System.String Scalar Type](../types/System.String.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression)
