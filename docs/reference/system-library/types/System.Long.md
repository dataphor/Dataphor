# System.Long

**Type Declaration**

```d4
create type .System.Long
{
    representation Long
    {
        Value : System.Long
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.LongAsStringSelectorNode"
} class "System.Int64Conveyor" static tags { Catalog.Comment = "System Signed 64-bit Integer", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Long(const Value : System.Long) : System.Long
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Long`.

`System.Long` is the set of all integer values in the range negative 9,223,372,036,854,775,808 (-2<sup>63</sup>) through positive 9,223,372,036,854,775,807 (2<sup>63</sup>-1). The physical representation of this type is a signed 64-bit integer.

**Using System.Long**

```d4
create table Test in Temp
{
 Type : String,
 NumberInUSA : Long, //in 2001
 key {Type}
};
insert table 
{
 row { 'Autos' Type, 137633467 NumberInUSA },
 row { 'Cycles', 4903056 },
 row { 'Buses', 749548 },
 row { 'Trucks', 92045311 }
} into Test;
select "Vehicles in USA in 2001";
select Test;
select Test group add { Sum(NumberInUSA) TotalVehiclesIn2001 };
drop table Test;
```

```text
Vehicles in USA in 2001
Type   NumberInUSA
------ -----------
Autos  137633467
Buses  749548
Cycles 4903056
Trucks 92045311

TotalVehiclesIn2001
-------------------
235331382
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [System.ToLong](../operators/System.ToLong.md)
