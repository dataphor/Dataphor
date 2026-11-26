# System.iWithout

`System.iWithout` is the instruction name for the semiminus, or `without`, operator in D4.

**Declarations**

```d4
create operator .System.iWithout(ALeftTable : generic table, ARightTable : generic table) : generic table
 class "System.WithoutNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

The parser recognized symbol for this operator is `without`. This operator computes the semiminus of two tables.

**See Also**

[Semiminus](../../../developers-guide/d4-table-operators.md#semiminus) \| [Semijoin](../../../developers-guide/d4-table-operators.md#semijoin) \| [Join](../../../developers-guide/d4-table-operators.md#join) \| [Outer Join](../../../developers-guide/d4-table-operators.md#outer-join) \| [Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
