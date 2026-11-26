# System.iShiftLeft

**Declarations**

```d4
create operator .System.iShiftLeft(const ALeftValue : System.Byte, const ARightValue : System.Integer) : System.Byte
 class "System.ByteShiftLeftNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iShiftLeft(const ALeftValue : System.Short, const ARightValue : System.Integer) : System.Short
 class "System.ShortShiftLeftNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iShiftLeft(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerShiftLeftNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iShiftLeft(const ALeftValue : System.Long, const ARightValue : System.Integer) : System.Long
 class "System.LongShiftLeftNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`<<` is the parser recognized symbol for `iShiftLeft`. This operator performs a binary left bit shift on the given number. It takes the value on the left and shifts it to the left the number of bits indicated by the right value. In signed integers the far left bit is the negative place holder. If a bit is in that position then the number is negative. Signed integers have a two's complement implementation.

**Using System.iShiftLeft**

```d4
select TableDee add { 1 << 2 BitShift };
```

```text
BitShift 
-------- 
4
```

```d4
select TableDee add { 1 << 31 BitShift };
```

```text
BitShift    
----------- 
-2147483648
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iShiftRight (`>>`)](System.iShiftRight.md)
