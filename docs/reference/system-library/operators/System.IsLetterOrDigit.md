# System.IsLetterOrDigit

**Declarations**

```d4
create operator .System.IsLetterOrDigit(const AString : System.String) : System.Boolean
 class "System.StringIsLetterOrDigitNode";
create operator .System.IsLetterOrDigit(const AString : System.String, const AIndex : System.Integer) : System.Boolean
 class "System.StringIsLetterOrDigitNode";
```

**Remarks**

If `AIndex` is specified, `IsLetterOrDigit` returns `true` if the character `AString[AIndex]` is a letter or a digit, otherwise `IsLetterOrDigit` returns `false`.

If `AIndex` is not specified, `IsLetterOrDigit` returns `true` if every character of `AString` is a letter or a digit, otherwise `IsLetterOrDigit` returns `false`.

> [!NOTE]
> String and list indexes are zero based.

**Using System.IsLetterOrDigit**

```d4
select IsLetterOrDigit("$123.85");
select IsLetterOrDigit("$123.85", 3);
```

```text
False
True
```

**See Also**

[System.IsDigit](System.IsDigit.md) \| [System.IsLetter](System.IsLetter.md) \| [System.String Scalar Type](../types/System.String.md) \| [Indexer](../../../developers-guide/d4-language-elements.md#indexer-expression)
