# System.All

**Declarations**

```d4
create aggregate operator .System.All(AValue : System.Boolean) : System.Boolean
initialization
 class "System.BooleanAllInitializationNode"
aggregation
 class "System.BooleanAllAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
```

**Remarks**

The `All` aggregate operator returns true if all the values of a given column are true. `All` returns true when the input contains no rows. This operator is the logical equivalent of an iterated `and`. Only rows that have a value for the specified column are considered in the aggregation.

**Using System.All**

```d4
select Employee
     add { (Name.Length() = 4) LengthIsFour };
```

```text
ID  Name    LengthIsFour
--  ----    -------------------
1   Joe     False
2   John    True
3   Josh    True
4   Jeff    True
5   Jake    True
6   Jeff    True
```

```d4
select Employee
 add { (Name.Length() = 4) Length4 }
 group add { All(Length4) AreAllLengthOf4 };

select All(Length4 from (Employee add { (Name.Length() = 4) Length4 } ));
```

```text
AreAllLengthOf4 
---------------
False

False
```

**See Also**

[System.Any](System.Any.md) \| [Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
