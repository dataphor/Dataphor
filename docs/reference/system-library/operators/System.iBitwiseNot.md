# System.iBitwiseNot

**Declarations**

```d4
create operator .System.iBitwiseNot(const AValue : System.Byte) : System.Byte
 class "System.ByteBitwiseNotNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseNot(const AValue : System.Short) : System.Short
 class "System.ShortBitwiseNotNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseNot(const AValue : System.Integer) : System.Integer
 class "System.IntegerBitwiseNotNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iBitwiseNot(const AValue : System.Long) : System.Long
 class "System.LongBitwiseNotNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`~` is the parser recognized symbol for `iBitwiseNot`. Bitwise not is a unary operator. It takes the number given and looks at every byte. The value that it returns is the number that would result if all of the bits were switched from 1 to 0 and vice versa.

**Using System.iBitwiseNot**

```d4
create table BitwiseNot { One : Integer , Two : Integer };
begin
    var LInteger1 : Integer := 314159265; //  bit value : 00010010 10111001 10110000 10100001
    var LInteger2 : Integer := ~LInteger1; // bit value : 11101101 01000110 01001111 01011110 
    insert table { row { LInteger1 One, LInteger2 Two } } into BitwiseNot;
end;
select BitwiseNot;
```

```text
Statement Executed Successfully!

Statement Executed Successfully!

One       Two        
--------- ---------- 
314159265 -314159266 
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iNot (`not`)](System.iNot.md)
