# System.IsLower

**Declarations**

```d4
create operator .System.IsLower(const AString : System.String) : System.Boolean
 class "System.StringIsLowerNode";
create operator .System.IsLower(const AString : System.String, const AIndex : System.Integer) : System.Boolean
 class "System.StringIsLowerNode";
```

**Remarks**

If `AIndex` is specified, `IsLower` returns `true` if the character `AString[AIndex]` is a lower case letter, otherwise `IsLower` returns `false`.

If `AIndex` is not specified, `IsLower` returns `true` if every character of `AString` that is a letter is a lower case letter, otherwise `IsLower` returns `false`.

> [!NOTE]
> String and list indexes are zero based.

**Using System.IsLower**

```d4
select IsLower("abcDEF");
select IsLower("abcDEF", 0);
```

```text
False
True
```

**See Also**

[System.IsUpper](System.IsUpper.md) \| [System.String Scalar Type](../types/System.String.md) \| [Indexer](../../../developers-guide/d4-language-elements.md#indexer-expression)
