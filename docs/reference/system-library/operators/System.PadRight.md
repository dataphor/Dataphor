# System.PadRight

**Declarations**

```d4
create operator .System.PadRight(const AString : System.String, const ATotalLength : System.Integer) : System.String
 class "System.StringPadRightNode";
create operator .System.PadRight(const AString : System.String, const ATotalLength : System.Integer, const APadString : System.String) : System.String
 class "System.StringPadRightNode";
```

**Remarks**

`PadRight` left-aligns the characters in `AString`, padding on the right with spaces or optionally a specified string `APadChar`, for a specified total length `ATotalLength`.

**Using System.PadRight**

```d4
create session table Example
{
 Before : String, After : String, key{}
};
begin
    var LString : String;
    LString := "Hello World";
    insert table
 {
  row 
  {
   LString Before, LString.PadRight(15, "*") After
  }
 } into Example;
end;
select Example add { After.Length() AfterLength };
drop table Example;
```

```text
Before      After           AfterLength
----------- --------------- -----------
Hello World Hello World**** 15
```

**See Also**

[System.Trim](System.Trim.md) \| [System.TrimLeft](System.TrimLeft.md) \| [System.TrimRight](System.TrimRight.md) \| [System.PadLeft](System.PadLeft.md) \| [System.String Scalar Type](../types/System.String.md)
