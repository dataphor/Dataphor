# System.Trim

**Declarations**

```d4
create operator .System.Trim(const AString : System.String) : System.String
 class "System.StringTrimNode";
```

**Remarks**

`Trim` returns the specified string with all white space removed from the left and right sides.

Valid white space characters are members of the SpaceSeparator category in UnicodeCategory. Signified by the Unicode designation "Zs" (separator, space). Also these Unicode characters: hexadecimal 0x0009 (horizontal tab), 0x000a (line feed), 0x000b (vertical tab), 0x000c (form feed), 0x000d (carriage return), 0x0085, 0x2028, and 0x2029.

**Using System.Trim**

```d4
create session table Example
     { UnTrimmed : String, Trimmed : String, key{} };
begin
    var LString : String;
    LString := "  Hello World  ";
    insert table { row { LString UnTrimmed, Trim(LString) Trimmed } } into Example;
end;
select Example
    add { UnTrimmed.Length() UnTrimmedLength, Trimmed.Length() TrimmedLength };
drop table Example;
```

```text
UnTrimmed       Trimmed     UnTrimmedLength TrimmedLength
--------------- ----------- --------------- -------------
  Hello World   Hello World 15              11
```

**See Also**

[System.String Scalar Type](../types/System.String.md) \| [System.TrimLeft](System.TrimLeft.md) \| [System.TrimRight](System.TrimRight.md) \| [System.PadLeft](System.PadLeft.md) \| [System.PadRight](System.PadRight.md)
