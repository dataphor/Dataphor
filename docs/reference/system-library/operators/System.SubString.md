# System.SubString

**Declarations**

```d4
create operator .System.SubString(const AString : System.String, const AStartIndex : System.Integer, const ALength : System.Integer) : System.String
 class "System.StringSubStringNode";
create operator .System.SubString(const AString : System.String, const AStartIndex : System.Integer) : System.String
 class "System.StringSubStringNode";
```

**Remarks**

`SubString` returns a string that is a part, or substring of, the given string.

> [!NOTE]
> String and list indexes are zero based.

**Using System.SubString**

```d4
select TableDee add { "Data" + SubString("Alphora", 2, 4) Copied };
```

```text
Copied
--------
Dataphor
```

**See Also**

[System.String Scalar Type](../types/System.String.md)
