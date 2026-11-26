# System.iMatches

**Declarations**

```d4
create operator .System.iMatches(const AString : System.String, const APattern : System.String) : System.Boolean
 class "System.StringMatchesNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMatches(const AString : System.Name, const APattern : System.String) : System.Boolean
 class "System.StringMatchesNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`matches` is the parser recognized symbol for `iMatches`. This operator matches using regular expressions. This operator functions similar to `like`. For Information on how this is handled, see the .NET documentation for Regular Expression Classes (linked below).

**Using System.iMatches**

```d4
select Employee where Name matches "^Jo";
```

```text
ID Name 
-- ---- 
1  Joe 
2  John 
3  Josh
```

**See Also**

[System.iLike (`like`)](System.iLike.md) \| [Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Regular Expression Cheat Sheet](http://regexlib.com/CheatSheet.htm) \| [Regular Expression Language Elements](http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconRegularExpressionsLanguageElements.asp)
