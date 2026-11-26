# System.IsLetter

**Declarations**

```d4
create operator .System.IsLetter(const AString : System.String) : System.Boolean
 class "System.StringIsLetterNode";
create operator .System.IsLetter(const AString : System.String, const AIndex : System.Integer) : System.Boolean
 class "System.StringIsLetterNode";
```

**Remarks**

If `AIndex` is specified, `IsLetter` returns `true` if the character `AString[AIndex]` is a letter, otherwise `IsLetter` returns `false`.

If `AIndex` is not specified, `IsLetter` returns `true` if every character of `AString` is a letter, otherwise `IsLetter` returns `false`.

> [!NOTE]
> String and list indexes are zero based.

**Using System.IsLetter**

```d4
select IsLetter("123abc");
select IsLetter("123abc", 4);
```

```text
False
True
```

**See Also**

[System.IsDigit](System.IsDigit.md) \| [System.IsLetterOrDigit](System.IsLetterOrDigit.md) \| [System.String Scalar Type](../types/System.String.md) \| [Indexer](../../../developers-guide/d4-language-elements.md#indexer-expression)
