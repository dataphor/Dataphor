# System.iBitwiseAnd

**Declarations**

```d4
create operator .System.iBitwiseAnd(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteBitwiseAndNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseAnd(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortBitwiseAndNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseAnd(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerBitwiseAndNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseAnd(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongBitwiseAndNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`&` is the parser recognized symbol for `iBitwiseAnd`. Bitwise and matches bit for bit, and compares the corresponding bits for two values. If both are 1, then the corresponding bit in the resulting value will be 1, otherwise it will be 0.

**Using System.iBitwiseAnd**

```d4
create table BitwiseAnd { One : Integer, Two : Integer, Three : Integer };
begin
    var LInteger1 : Integer;
    var LInteger2 : Integer;
    var LInteger3 : Integer;
    LInteger1 := 314159265; // this has a bit value of 00010010 10111001 10110000 10100001
    LInteger2 := 271828182; // this has a bit value of 00010000 00110011 11000100 11010110
    LInteger3 := LInteger1 & LInteger2; // 271679616   00010000 00110001 10000000 10000000
    insert table { row { LInteger1 One, LInteger2 Two, LInteger3 Three }} into BitwiseAnd;
end;
select BitwiseAnd;
```

```text
Statement Executed Successfully!

Statement Executed Successfully!

One       Two       Three     
--------- --------- --------- 
314159265 271828182 271679616
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iAnd (`and`)](System.iAnd.md)
