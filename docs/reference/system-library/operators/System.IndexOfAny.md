# System.IndexOfAny

**Declarations**

```d4
create operator .System.IndexOfAny(const AString : System.String, const AAnyOf : list(System.String)) : System.Integer
 class "System.StringIndexOfAnyNode";
create operator .System.IndexOfAny(const AString : System.String, const AAnyOf : list(System.String), const AStartIndex : System.Integer) : System.Integer
 class "System.StringIndexOfAnyNode";
create operator .System.IndexOfAny(const AString : System.String, const AAnyOf : list(System.String), const AStartIndex : System.Integer, const ALength : System.Integer) : System.Integer
 class "System.StringIndexOfAnyNode";
```

**Remarks**

`IndexOfAny` returns the index of the starting location of the *first* occurrence of one of the substrings found in `AString`. If none of the substrings in the `AAnyOf` list are found in `AString`, -1 is returned.

Overloads that include a starting index parameter will search the string from the given index, ignoring any preceding characters. Overloads that do not include this parameter assume the first character (index of zero). If this index is less than zero, it is assumed to be zero; if it is greater than the string length, then the string length is assumed. Overloads that include a length parameter will search for up to ALength number of characters after the starting index. If ALength exceeds the string length then the string length is used. Specifying an ALength value of less than zero will raise an error.

The substrings are each checked and the match with the lowest index is returned.

> [!NOTE]
> String and list indexes are zero based.

**Using System.IndexOfAny**

```d4
create session table Example 
 { Index : Integer, Occurance : String, key{TIndex} };
begin
 var LStringList : list(String) := { "or", "at", "if", "oo" };
 insert table
 {
  row
  { 
   "Dataphor from Alphora is so cool!".IndexOfAny(LStringList) TIndex,
   "First" Occurance
  }
 } into Example;
 insert table
 {
  row
  { 
   "Dataphor from Alphora is so cool!".LastIndexOfAny(LStringList) TIndex,
   "Last" Occurance
  }
 } into Example;
end;
select Example;
drop table Example;
```

```text
TIndex Occurance
------ ---------
6      First
18     Last
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf](System.IndexOf.md) \| [System.LastIndexOf (String)](System.LastIndexOf.md) \| [System.LastIndexOf (List)](System.LastIndexOf.md) \| [System.LastIndexOfAny](System.LastIndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [System.String Scalar Type](../types/System.String.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression)
