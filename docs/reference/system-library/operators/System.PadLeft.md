# System.PadLeft

**Declarations**

```d4
create operator .System.PadLeft(const AString : System.String, const ATotalLength : System.Integer) : System.String
 class "System.StringPadLeftNode";
create operator .System.PadLeft(const AString : System.String, const ATotalLength : System.Integer, const APadString : System.String) : System.String
 class "System.StringPadLeftNode";
```

**Remarks**

`PadLeft` right-aligns the characters in `AString`, padding on the left with spaces or optionally a specified string `APadChar`, for a specified total length `ATotalLength`.

**Using System.PadLeft**

```d4
create session table Example
{
 Before : String,
 After : String, key{}
};
begin
    var LString : String;
    LString := "Hello World";
    insert table
 {
  row 
  {
   LString Before, LString.PadLeft(15, "*") After
  }
 } into Example;
end;
select Example add { After.Length() AfterLength };
drop table Example;
```

```text
Before      After           AfterLength
----------- --------------- -----------
Hello World ****Hello World 15
```

**See Also**

[System.Trim](System.Trim.md) \| [System.TrimLeft](System.TrimLeft.md) \| [System.TrimRight](System.TrimRight.md) \| [System.PadRight](System.PadRight.md) \| [System.String Scalar Type](../types/System.String.md)
