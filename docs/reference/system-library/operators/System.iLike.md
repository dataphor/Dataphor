# System.iLike

**Declarations**

```d4
create operator .System.iLike(const AString : System.String, const APattern : System.String) : System.Boolean
 class "System.StringLikeNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLike(const AString : System.Name, const APattern : System.String) : System.Boolean
 class "System.StringLikeNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`like` is the parser recognized symbol for `iLike`. This operator performs a string comparison using an inexact match containing wildcard characters. Using the wildcard characters one can specify how to limit the dataset. The percent character and the underscore character are used as wildcard characters to signify part of a string that is unknown. The underscore is used to represent one unknown character, and the percent is used to represent an unknown number of unknown characters.

**Using System.iLike**

```d4
select Employee where Name like "J%";
select Employee where Name like "J___";
```

```text
ID Name 
-- ---- 
1  Joe  
2  John 
3  Josh 
4  Jeff 
5  Jake 
6  Jeff 

ID Name 
-- ---- 
2  John 
3  Josh 
4  Jeff 
5  Jake 
6  Jeff 
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
