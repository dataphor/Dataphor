# System.Decimal

**Type Declaration**

```d4
create type .System.Decimal
{
    representation Decimal
    {
        Value : System.Decimal
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.DecimalAsStringSelectorNode"
} class "System.DecimalConveyor" static tags { Catalog.Comment = "System Binary Coded Decimal", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Decimal(const Value : System.Decimal) : System.Decimal
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Decimal`.

`System.Decimal` is the set of all possible numbers. Values of this domain are represented physically by a 96-bit signed scaled integer, so in practice, the available values are negative 79,228,162,514,264,337,593,543,950,335 (-2<sup>96</sup>-1) through positive 79,228,162,514,264,337,593,543,950,335 (2<sup>96</sup>-1).

**Using System.Decimal**

```d4
create table Test in Temp
{
 TestValue : Decimal,
 key {}
};
begin
 var MilesToMoon : Decimal := 236000;
 var MilesPerLightyear : Decimal := 5878499810000;
 insert table 
 {
  row {  MilesToMoon / MilesPerLightyear TestValue }
 } into Test;
end;
select Test rename { TestValue LightyearsToMoon };
drop table Test;
select Decimal(17);
```

```text
LightyearsToMoon
------------------------------
0.0000000401462971213398746372
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Parser Literals](../../../developers-guide/d4-lexical-elements.md#parser-literals) \| [System.ToDecimal](../operators/System.ToDecimal.md)
