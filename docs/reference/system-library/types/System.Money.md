# System.Money

**Type Declaration**

```d4
create type .System.Money
{
    representation Money
    {
        Value : System.Decimal
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.MoneyAsStringSelectorNode", 
    representation AsDisplayString
    {
        AsDisplayString : System.String
    } selector  class "System.MoneyAsStringSelectorNode"
} class "System.DecimalConveyor" static tags { Catalog.Comment = "System Money", DAE.AsString = "AsString", DAE.AsDisplayString = "AsDisplayString" }
```

**Declarations**

```d4
create operator .System.Money(const Value : System.Decimal) : System.Money
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Money`.

`System.Money` is the set of all monetary values in the range negative \$79,228,162,514,264,337,593,543,950,335 (-\$2<sup>96</sup>-1) through positive \$79,228,162,514,264,337,593,543,950,335 (\$2<sup>96</sup>-1). The physical representation of this type is 96-bit signed scaled integer.

Placing a \$ symbol at the beginning of a number identifies the number to the compiler as a literal of type money. The \$ symbol does *not* designate the currency being identified, i.e. dollars.

**Using System.Money**

```d4
create table Test in Temp
{
 Area : String,
 June7 : Money,
 June14 : Money,
 ChangeSinceLastYear : Money,
 key {Area}
};
insert table 
{
 row { ' East Coast' Area, $2.026 June7, $1.999 June14, $.536 ChangeSinceLastYear },
 row { ' MidWest', $1.975, $1.882, $.364 },
 row { ' West Coast', $2.276, $2.243, $.504 },
 row { 'National', $2.034, $1.985, $.467 }
} into Test;
select "U.S. regular-grade gas prices 2004";
select Test add { June14 - June7 ChangeFromLastWeek } ;
select "From U.S. Engergy Information Administration weekly survey";
drop table Test;
```

```text
U.S. regular-grade gas prices 2004
Area        June7 June14 ChangeSinceLastYear ChangeFromLastWeek
----------- ----- ------ ------------------- ------------------
 East Coast $2.03 $2.00  $0.54               ($0.03)
 MidWest    $1.98 $1.88  $0.36               ($0.09)
 West Coast $2.28 $2.24  $0.50               ($0.03)
National    $2.03 $1.99  $0.47               ($0.05)

From U.S. Engergy Information Administration weekly survey
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Parser Literals](../../../developers-guide/d4-lexical-elements.md#parser-literals) \| [System.ToMoney](../operators/System.ToMoney.md)
