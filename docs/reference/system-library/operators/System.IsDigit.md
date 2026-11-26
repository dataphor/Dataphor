# System.IsDigit

**Declarations**

```d4
create operator .System.IsDigit(const AString : System.String) : System.Boolean
 class "System.StringIsDigitNode";
create operator .System.IsDigit(const AString : System.String, const AIndex : System.Integer) : System.Boolean
 class "System.StringIsDigitNode";
```

**Remarks**

If `AIndex` is specified, `IsDigit` returns `true` if the character `AString[AIndex]` is a digit, otherwise `IsDigit` returns `false`.

If `AIndex` is not specified, `IsDigit` returns `true` if every character of `AString` is a digit, otherwise `IsDigit` returns `false`.

> [!NOTE]
> String and list indexes are zero based.

**Using System.IsDigit**

```d4
select IsDigit("123abc");
select IsDigit("123abc", 0);
```

```text
False
True
```

**See Also**

[System.IsLetter](System.IsLetter.md) \| [System.IsLetterOrDigit](System.IsLetterOrDigit.md) \| [System.String Scalar Type](../types/System.String.md) \| [Indexer](../../../developers-guide/d4-language-elements.md#indexer-expression)
