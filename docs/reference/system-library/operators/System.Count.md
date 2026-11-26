# System.Count

**Declarations**

```d4
create operator .System.Count(const AList : generic list) : System.Integer
 class "System.ListCountNode";
create aggregate operator .System.Count() : System.Integer
initialization
 class "System.CountInitializationNode"
aggregation
 class "System.CountAggregationNode"
finalization
 class "System.EmptyFinalizationNode";
create aggregate operator .System.Count(AValue : System.Scalar) : System.Integer
initialization
 class "System.CountInitializationNode"
aggregation
 class "System.ObjectCountAggregationNode"
finalization
 class "System.EmptyFinalizationNode";
```

**Remarks**

`Count` is an aggregate operator and a list operator.

The `Count` aggregate operator returns the number of rows in the input. `Count` returns zero when the input contains no rows.

When a column is specified `Count` will return the number of rows in the input that have a value for the specified column (are not `nil`).

Only rows that have a value for the specified column are considered in the aggregation.

The `Count` list operator returns the number of values in a list. Zero is return if the list is empty.

**Using System.Count**

```d4
create table Test in Temp
{
 ID : Integer,
 Desc : String { nil },
 key { ID }
};
insert table
{
 row { 1 ID, 'First' Desc },
 row { 2, nil as String },
 row { 3, 'Third' },
 row { 4, 'Fourth' }
} into Test;
select Test;
select Test group add { Count() All, Count(Desc) DescNotNil };
select Count(Test); // standalone invocation
select Count(Desc from Test); // standalone invocation
drop table Test;

create table Test in Temp
{
    ElementCount : Integer
};
begin
 var StrList : list(String) := { 'A', 'B', 'C' };
 insert table { row { Count(StrList) ElementCount } } into Test;
end;
select Test;
drop table Test;
```

```text
ID Desc
-- ----------
1  First
2  <No Value>
3  Third
4  Fourth

All DescNotNil
--- ----------
4   3

4

3

ElementCount
------------
3
```

**See Also**

[Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
