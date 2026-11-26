# System.iXor

**Remarks**

`xor` is the parser recognized symbol for this operator is . The `xor` operator is a logical exclusive or. Only one of the operands can be true for the result to be true.

|           |            |        |
|-----------|------------|--------|
| Left Side | Right Side | Result |
| False     | False      | False  |
| False     | True       | True   |
| True      | False      | True   |
| True      | True       | False  |

**Example**

```d4
select Manager where (Employee_ID mod 2 = 0) xor (Manager_ID > 2);
```

```text
Manager_ID Employee_ID 
---------- ----------- 
1          2           
2          4           
2          6           
4          5
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
