# System.iShiftRight

**Declarations**

```d4
create operator .System.iShiftRight(const ALeftValue : System.Byte, const ARightValue : System.Integer) : System.Byte
 class "System.ByteShiftRightNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iShiftRight(const ALeftValue : System.Short, const ARightValue : System.Integer) : System.Short
 class "System.ShortShiftRightNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iShiftRight(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerShiftRightNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iShiftRight(const ALeftValue : System.Long, const ARightValue : System.Integer) : System.Long
 class "System.LongShiftRightNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`>>` is the parser recognized symbol for `iShiftRight` is . This operator performs a binary right bit shift on the given number. It takes the value on the left and shifts it to the right the number of bits indicated by the right value. Bits shifted off the value are lost. In the case of unsigned types, the Dataphor Server performs a logical shift (i.e. zeroes are shifted in from the left). In signed types, the Dataphor Server performs an arithmetic right shift (i.e. the most significant bit is copied and shifted in from the right).

**Using System.iShiftRight**

```d4
select TableDee add { -1 >> 5 BitShift };
```

```text
BitShift 
-------- 
-1
```

```d4
select TableDee add { 1048576 >> 10 BitShift };
```

```text
BitShift 
-------- 
1024
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iShiftLeft (`<<`)](System.iShiftLeft.md)
