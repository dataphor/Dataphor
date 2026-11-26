# System.IsUpper

**Declarations**

```d4
create operator .System.IsUpper(const AString : System.String) : System.Boolean
 class "System.StringIsUpperNode";
create operator .System.IsUpper(const AString : System.String, const AIndex : System.Integer) : System.Boolean
 class "System.StringIsUpperNode";
```

**Remarks**

If `AIndex` is specified, `IsUpper` returns `true` if the character `AString[AIndex]` is an upper case letter, otherwise `IsUpper` returns `false`.

If `AIndex` is not specified, `IsUpper` returns `true` if every character of `AString` that is a letter is an upper case letter, otherwise `IsUpper` returns `false`.

> [!NOTE]
> String and list indexes are zero based.

**Using System.IsUpper**

```d4
select IsUpper("abcDEF");
select IsUpper("abcDEF", 4);
```

```text
False
True
```

**See Also**

[System.IsLower](System.IsLower.md) \| [System.String Scalar Type](../types/System.String.md) \| [Indexer](../../../developers-guide/d4-language-elements.md#indexer-expression)
