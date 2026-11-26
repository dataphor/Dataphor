# System.Date

**Type Declaration**

```d4
create type .System.Date
{
    representation Ticks
    {
        Ticks : System.Long
    } selector  class "System.SystemDateTicksSelectorNode", 
    representation Date
    {
        Year : System.Integer, 
        Month : System.Integer, 
        Day : System.Integer
    } selector  class "System.SystemDateTimeDateTimeDaysSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.DateAsStringSelectorNode"
} class "System.DateTimeConveyor" static tags { Catalog.Comment = "System Date", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Date(const Year : System.Integer, const Month : System.Integer, const Day : System.Integer) : System.Date
 class "System.SystemDateTimeDateTimeDaysSelectorNode";
create operator .System.Date() : System.Date
 class "System.DateNode" static tags { DAE.IsDeterministic = "False" };
```

**Remarks**

`Date` is the selector for the `Date` Datatype and an operator.

`System.Date` is the set of all possible dates from a fixed point. The physical representation for this type is a 64-bit signed integer representing the number of ticks since 12:00 midnight, January 1, 1 C.E. (Common Era) in the Gregorian calendar. This means that the range of values for this domain is from January 1, 0001 C.E. to December 31, 9999 C.E.

**Using System.Date**

```d4
create table Test in Temp
{
 WhatDate : String,
 Date : Date,
 key {Date}
};
insert table
{
 row { "Today" WhatDate, Date() Date },
 row { "VE day", Date(1945, 5, 8) },
 row { "DateTicks", Date.Ticks(627346080000000000) }
} into Test;
select Test;
drop table Test;
```

```text
WhatDate  Date
--------- ----------
VE day    1945-05-08
DateTicks 1988-12-25
Today     2004-06-18
```

**See Also**

[System.Date Operator](../operators/System.Date.md) \| [System.DateTime](System.DateTime.md) \| [System.Time](System.Time.md) \| [System.TimeSpan](System.TimeSpan.md) \| Using Date and Time Values \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [Type Conversions](../../../developers-guide/d4-catalog.md#conversions) \| [System.ToDate](../operators/System.ToDate.md)
