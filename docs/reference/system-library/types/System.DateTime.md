# System.DateTime

**Type Declaration**

```d4
create type .System.DateTime
{
    representation Ticks
    {
        Ticks : System.Long
    } selector  class "System.SystemDateTimeTicksSelectorNode", 
    representation DateTime
    {
        Year : System.Integer, 
        Month : System.Integer, 
        Day : System.Integer, 
        Hour : System.Integer, 
        Minute : System.Integer, 
        Second : System.Integer
    } selector  class "System.SystemDateTimeDateTimeSecondsSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.DateTimeAsStringSelectorNode"
} class "System.DateTimeConveyor" static tags { Catalog.Comment = "System Date Time", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.DateTime(const Year : System.Integer, const Month : System.Integer, const Day : System.Integer, const Hour : System.Integer, const Minute : System.Integer, const Second : System.Integer) : System.DateTime
 class "System.SystemDateTimeDateTimeSecondsSelectorNode";
create operator .System.DateTime(const AYears : System.Integer, const AMonths : System.Integer, const ADays : System.Integer, const AHours : System.Integer, const AMinutes : System.Integer) : System.DateTime
 class "System.SystemDateTimeDateTimeMinutesSelectorNode";
create operator .System.DateTime(const AYears : System.Integer, const AMonths : System.Integer, const ADays : System.Integer) : System.DateTime
 class "System.SystemDateTimeDateTimeDaysSelectorNode";
create operator .System.DateTime() : System.DateTime
 class "System.DateTimeNode" static tags { DAE.IsDeterministic = "False" };
```

**Remarks**

`DateTime` is a selector for the `DateTime` Datatype and an operator.

`System.DateTime` is the set of all possible dates, or times from a fixed point. The physical representation for this type is a 64-bit signed integer representing the number of ticks since 12:00 midnight, January 1, 1 C.E. (Common Era) in the Gregorian calendar. This means that the range of values for this domain is from 12:00:00 midnight, January 1, 0001 C.E. to 11:59:59 P.M., December 31, 9999 C.E.

The `DateTime` operator, with no arguments, returns a `DateTime` type representing the current date and time. Unlike the `Date` operator, this operator returns both the date and time.

With integer year, month, day, \[hours, minutes, \[seconds\]\] specified, this operator returns a `DateTime` type value representing the date and time specified.

**Using System.DateTime**

```d4
create table Test in Temp
{
 What : String,
 When : DateTime,
 key {When}
};
insert table
{
 row { "Now" What, DateTime() When },
 row { "New Years Morning", DateTime(2005,01,01, 8, 15) },
 row { "DateTimeTicks", DateTime.Ticks(631680313950000000) }
} into Test;
select Test;
drop table Test;
```

```text
What              When
----------------- -------------------
DateTimeTicks     2002-09-19 11:23:15
Now               2004-06-18 09:07:42
New Years Morning 2005-01-01 08:15:00
```

**See Also**

[System.DateTime Operator](../operators/System.DateTime.md) \| [System.Date](System.Date.md) \| [System.Time](System.Time.md) \| [System.TimeSpan](System.TimeSpan.md) \| [Using Date and Time Values](../../../developers-guide/d4-programming-topics.md#using-date-and-time-values) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToDateTime](../operators/System.ToDateTime.md)
