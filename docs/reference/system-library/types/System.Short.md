# System.Short

**Type Declaration**

```d4
create type .System.Short
{
    representation Short
    {
        Value : System.Short
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.ShortAsStringSelectorNode"
} class "System.Int16Conveyor" static tags { Catalog.Comment = "System Signed 16-bit Integer", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Short(const Value : System.Short) : System.Short
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Short`.

`System.Short` is the set of all integer values in the range negative 32768 (-2<sup>15</sup>) through positive 32767 (2<sup>15</sup>-1). The physical representation of this type is a signed 16-bit integer.

**Using System.Short**

```d4
create table Test in Temp
{
 Area : String,
 Bones : Short
};
insert table 
{
 row { 'Shull' Area, 28 Bones },
 row { 'Neck', 7 },
 row { 'Vertebrae', 26 },
 row { 'Ribs', 24 },
 row { 'Pelvic', 2 },
 row { 'Arms & Legs', 120 } 
} into Test;
select "Bones in the adult human body";
select Test;
select Test group add { Sum(Bones) TotalBones };
drop table Test;
```

```text
Area        Bones
----------- -----
Arms & Legs 120
Neck        7
Pelvic      2
Ribs        24
Shull       28
Vertebrae   26

TotalBones 
---------- 
207
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Parser Literals](../../../developers-guide/d4-lexical-elements.md#parser-literals) \| [System.ToShort](../operators/System.ToShort.md)
