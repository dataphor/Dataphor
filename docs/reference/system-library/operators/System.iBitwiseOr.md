# System.iBitwiseOr

**Declarations**

```d4
create operator .System.iBitwiseOr(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteBitwiseOrNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseOr(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortBitwiseOrNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseOr(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerBitwiseOrNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseOr(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongBitwiseOrNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`|` is the parser recognized symbol for `iBitwiseOr`. `|` is the vertical bar, also known as the pipe. `Bitwise Or` matches bit for bit, and compares the corresponding bits for two values. If both are 0, then the corresponding bit in the resulting value will be 0, otherwise it will be 1.

**Using System.iBitwiseOr**

```d4
create table BitwiseOr { One : Integer, Two : Integer, Three : Integer };
begin
    var LInteger1 : Integer;
    var LInteger2 : Integer;
    var LInteger3 : Integer;
    LInteger1 := 314159265; // this has a bit value of 00010010 10111001 10110000 10100001
    LInteger2 := 271828182; // this has a bit value of 00010000 00110011 11000100 11010110
    LInteger3 := LInteger1 | LInteger2; // 314307831   00010010 10111011 11110100 11110111
    insert table { row { LInteger1 One, LInteger2 Two, LInteger3 Three }} into BitwiseOr;
end;
select BitwiseOr;
```

```text
Statement Executed Successfully!

Statement Executed Successfully!

One       Two       Three     
--------- --------- --------- 
314159265 271828182 314307831
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iOr (`or`)](System.iOr.md)
