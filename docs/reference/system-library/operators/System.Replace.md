# System.Replace

**Declarations**

```d4
create operator .System.Replace(const AString : System.String, const AOldString : System.String, const ANewString : System.String) : System.String
 class "System.StringReplaceNode";
create operator .System.Replace(const AString : System.String, const AOldString : System.String, const ANewString : System.String, const ACaseSensitive : System.Boolean) : System.String
 class "System.StringReplaceNode";
```

**Remarks**

`Replace` replaces all occurrences of `OldString` with `ANewString` in `AString`. AOldString and ANewString are not required to be the same length.

**Using System.Replace**

```d4
select "Let's play leap bunny.".Replace("bunny","frog");
```

```text
Let's play leap frog.
```

**See Also**

[System.String Scalar Type](../types/System.String.md)
