# System.TrimLeft

**Declarations**

```d4
create operator .System.TrimLeft(const AString : System.String) : System.String
 class "System.StringTrimLeftNode";
```

**Remarks**

`TrimLeft` returns the specified string with all white space removed from the left side.

Valid white space characters are members of the SpaceSeparator category in UnicodeCategory. Signified by the Unicode designation "Zs" (separator, space). Also these Unicode characters: hexadecimal 0x0009 (horizontal tab), 0x000a (line feed), 0x000b (vertical tab), 0x000c (form feed), 0x000d (carriage return), 0x0085, 0x2028, and 0x2029.

**Using System.TrimLeft**

```d4
create session table Example
     { UnTrimmed : String, Trimmed : String, key{} };
begin
    var LString : String;
    LString := "     Hello World";
    insert table { row { LString UnTrimmed, TrimLeft(LString) Trimmed } } into Example;
end;
select Example
    add { UnTrimmed.Length() UnTrimmedLength, Trimmed.Length() TrimmedLength };
drop table Example;
```

```text
UnTrimmed        Trimmed     UnTrimmedLength TrimmedLength
---------------- ----------- --------------- -------------
     Hello World Hello World 16              11
```

**See Also**

[System.Trim](System.Trim.md) \| [System.TrimRight](System.TrimRight.md) \| [System.PadLeft](System.PadLeft.md) \| [System.PadRight](System.PadRight.md) \| [System.String Scalar Type](../types/System.String.md)
