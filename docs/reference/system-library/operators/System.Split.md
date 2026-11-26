# System.Split

**Declarations**

```d4
create operator .System.Split(const AString : System.String) : list(System.String)
 class "System.StringSplitNode";
create operator .System.Split(const AString : System.String, const ADelimiter : System.String) : list(System.String)
 class "System.StringSplitNode";
create operator .System.Split(const AString : System.String, const ADelimiters : list(System.String)) : list(System.String)
 class "System.StringSplitNode";
```

**Remarks**

`Split` is used to split the given string into a list of strings. The splitting is based on one or more delimiters.

`Split` invoked with only one argument, `AString`, will use the default delimiters "," and ";" (comma and semi-colon).

`Split` invoked with two arguments, `AString` and `ADelimiter`, will split `AString` at each point where `ADelimiter` is found in the string.

`Split` invoked with two arguments, `AString` and `ADelimiters`, will split `AString` at each point where any of the delimiters in the list `ADelimiters` is found.

> [!NOTE]
> Splitting an empty string will return an empty string.
>
> Splitting a string that contains no delimiter(s) will return the original string.

**Using System.Split**

```d4
create session table SplitExample
{
    ListIndex : Integer,
    SubString : String,
    key { ListIndex }
};
begin
  var LList : list(String);
  var Recipe := "Bread contains: Flour, Sugar, Water, Oil, Yeast, Salt";
  LList := Recipe.Split( { ":", "," } );
  for I : Integer := 0 to LList.Count() - 1 do
    insert
      table
      {
        row { I ListIndex, LList[I].Trim() SubString }
      } into SplitExample;
end;
select SplitExample;
drop table SplitExample;
```

```text
ListIndex SubString
--------- --------------
0         Bread contains
1         Flour
2         Sugar
3         Water
4         Oil
5         Yeast
6         Salt
```

**See Also**

[System.ConCat](System.Concat.md) \| [System.String Scalar Type](../types/System.String.md)
