# System.Byte

**Type Declaration**

```d4
create type .System.Byte
{
    representation Byte
    {
        Value : System.Byte
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.ByteAsStringSelectorNode"
} class "System.ByteConveyor" static tags { Catalog.Comment = "System Unsigned 8-bit Integer", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Byte(const Value : System.Byte) : System.Byte
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Byte`.

`System.Byte` is the set of all integer values in the range 0 through positive 255 (2<sup>8</sup>-1). The physical representation of this type is an unsigned 8-bit integer.

**Using System.Byte**

```d4
create table Test in Temp
{
 TestValue : Byte,
 key {}
};
begin
 var RosesGrown : Byte := 17;
 var RosesCut : Byte := 8;
 insert table 
 {
  row {  RosesGrown - RosesCut TestValue }
 } into Test;
end;
select Test rename { TestValue RosesLeft };
drop table Test;
```

```text
RosesLeft
---------
9
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [System.ToByte](../operators/System.ToByte.md)
