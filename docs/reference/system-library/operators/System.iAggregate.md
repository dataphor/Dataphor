# System.iAggregate

**Declarations**

```d4
create operator .System.iAggregate(ATable : generic table) : generic table
 class "System.AggregateNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`iAggregate` is the operator name for the aggregate table operator. The parser-recognized symbol for this operator is `group`. For a complete description of this operator, refer to the [Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) discussion in the D4 Language Guide.

**See Also**

[Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
