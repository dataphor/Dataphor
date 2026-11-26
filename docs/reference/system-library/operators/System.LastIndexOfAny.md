# System.LastIndexOfAny

**Declarations**

```d4
create operator .System.LastIndexOfAny(const AString : System.String, const AAnyOf : list(System.String)) : System.Integer
 class "System.StringLastIndexOfAnyNode";
create operator .System.LastIndexOfAny(const AString : System.String, const AAnyOf : list(System.String), const AStartIndex : System.Integer) : System.Integer
 class "System.StringLastIndexOfAnyNode";
create operator .System.LastIndexOfAny(const AString : System.String, const AAnyOf : list(System.String), const AStartIndex : System.Integer, const ALength : System.Integer) : System.Integer
 class "System.StringLastIndexOfAnyNode";
```

**Remarks**

`LastIndexOfAny` returns the index of the starting location of the *last* occurrence of one of the given substrings found in `AString`. If none of the substrings in the `AAnyOf` list are found in `AString`, -1 is returned.

Overloads that include a starting index parameter will search the string backwards from the given index, ignoring any following characters. Overloads that do not include this parameter assume the last character. If the index given is less than zero, it is assumed to be zero; if it is greater than the string length, then the string length is assumed (one position after the last index). Overloads with a length parameter will search for up to ALength characters before the starting index. If the starting index minus the ALength exceeds the string bounds then the maximum possible length is assumed. Specifying an ALength value of less than zero will raise an error.

The substrings are each checked and the one resulting in the greatest index is choosen.

> [!NOTE]
> String and list indexes are zero based.

**Using System.LastIndexOfAny**

```d4
create session table Example 
{
 Index : Integer, Occurance : String, key{Index}
};
begin
 var LStringList := { "or", "at", "if", "oo" };
 insert 
  row
  { 
   "Dataphor from Alphora is so cool!".IndexOfAny(LStringList) Index,
   "First" Occurance
  }
  into Example;
 insert
  row
  { 
   "Dataphor from Alphora is so cool!".LastIndexOfAny(LStringList) Index,
   "Last" Occurance
  }
  into Example;
end;
select Example;
drop table Example;
```

```text
Index Occurance
------ ---------
6      First
18     Last
```

**See Also**

[System.StartsWith](System.StartsWith.md) \| [System.EndsWith](System.EndsWith.md) \| [System.IndexOf (String)](System.IndexOf.md) \| [System.LastIndexOf (String)](System.LastIndexOf.md) \| [System.IndexOfAny](System.IndexOfAny.md) \| [System.Pos](System.Pos.md) \| [System.LastPos](System.LastPos.md) \| [System.Lower](System.Lower.md) \| [System.Upper](System.Upper.md) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression) \| [System.String Scalar Type](../types/System.String.md)
