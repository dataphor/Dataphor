# System.iBitwiseXor

**Declarations**

```d4
create operator .System.iBitwiseXor(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteBitwiseXorNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseXor(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortBitwiseXorNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseXor(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerBitwiseXorNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseXor(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongBitwiseXorNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`^` is the parser recognized symbol for `iBitwiseXor`. Bitwise exclusive or matches bit for bit, and compares the corresponding bits for two values. If both are 0 or both are 1, then the corresponding bit in the resulting value will be 0. Only if one is 0 and one is 1 will the corresponding bit in the resulting value will be 1.

**Using System.iBitwiseXor**

```d4
create table BitwiseXor { One : Integer, Two : Integer, Three : Integer };
begin
    var LInteger1 : Integer;
    var LInteger2 : Integer;
    var LInteger3 : Integer;
    LInteger1 := 314159265; // this has a bit value of 00010010 10111001 10110000 10100001
    LInteger2 := 271828182; // this has a bit value of 00010000 00110011 11000100 11010110
    LInteger3 := LInteger1 ^ LInteger2; // 42628215    00000010 10001010 01110100 01110111
    insert table { row { LInteger1 One, LInteger2 Two, LInteger3 Three }} into BitwiseXor;
end;
select BitwiseXor;
```

```text
Statement Executed Successfully!

Statement Executed Successfully!

One       Two       Three    
--------- --------- -------- 
314159265 271828182 42628215
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iXor (`xor`)](System.iXor.md)
