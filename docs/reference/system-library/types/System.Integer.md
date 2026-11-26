# System.Integer

**Type Declaration**

```d4
create type .System.Integer
{
    representation Integer
    {
        Value : System.Integer
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.IntegerAsStringSelectorNode"
} class "System.Int32Conveyor" static tags { Catalog.Comment = "System Signed 32-bit Integer", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Integer(const Value : System.Integer) : System.Integer
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Integer`.

`System.Integer` is the set of all integer values in the range negative 2,147,483,648 (-2<sup>31</sup>) through positive 2,147,483,647 (2<sup>31</sup>-1). The physical representation of this type is a signed 32-bit integer.

**Using System.Integer**

```d4
create table Test in Temp
{
 Processor : Integer,
 Transistors : Integer,
 key {Processor}
};
insert table 
{
 row { 8080 Processor, 6000 Transistors },
 row { 8088, 29000 },
 row { 80286, 134000 },
 row { 80386, 275000 },
 row { 80486, 1200000 }
} into Test;
select Test add { Transistors - 6000 Increase };
drop table Test;
```

```text
Processor Transistors Increase
--------- ----------- --------
8080      6000        0
8088      29000       23000
80286     134000      128000
80386     275000      269000
80486     1200000     1194000
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Parser Literals](../../../developers-guide/d4-lexical-elements.md#parser-literals) \| [System.ToInteger](../operators/System.ToInteger.md)
