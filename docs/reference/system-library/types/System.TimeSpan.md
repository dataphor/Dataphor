# System.TimeSpan

**Type Declaration**

```d4
create type .System.TimeSpan
{
    representation Ticks
    {
        Ticks : System.Long
    } selector  class "System.SystemTimeSpanTicksSelectorNode", 
    representation Milliseconds
    {
        Milliseconds : System.Decimal
    } selector  class "System.SystemTimeSpanMillisecondsSelectorNode", 
    representation Seconds
    {
        Seconds : System.Decimal
    } selector  class "System.SystemTimeSpanSecondsSelectorNode", 
    representation Minutes
    {
        Minutes : System.Decimal
    } selector  class "System.SystemTimeSpanMinutesSelectorNode", 
    representation Hours
    {
        Hours : System.Decimal
    } selector  class "System.SystemTimeSpanHoursSelectorNode", 
    representation Days
    {
        Days : System.Decimal
    } selector  class "System.SystemTimeSpanDaysSelectorNode", 
    representation TimeSpan
    {
        Day : System.Integer,
        Hour : System.Integer,
        Minute : System.Integer,
        Second : System.Integer,
        Millisecond : System.Decimal
    } selector  class "System.SystemTimeSpanTimeSpanMillisecondsSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.TimeSpanAsStringSelectorNode"
} class "System.TimeSpanConveyor" static tags { Catalog.Comment = "System Time Span", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.TimeSpan(const Day : System.Integer, const Hour : System.Integer, const Minute : System.Integer, const Second : System.Integer, const Millisecond : System.Decimal) : System.TimeSpan
 class "System.SystemTimeSpanTimeSpanMillisecondsSelectorNode";
create operator .System.TimeSpan(const ADays : System.Integer, const AHours : System.Integer, const AMinutes : System.Integer, const ASeconds : System.Integer) : System.TimeSpan
 class "System.SystemTimeSpanTimeSpanSecondsSelectorNode";
create operator .System.TimeSpan(const ADays : System.Integer, const AHours : System.Integer, const AMinutes : System.Integer) : System.TimeSpan
 class "System.SystemTimeSpanTimeSpanMinutesSelectorNode";
create operator .System.TimeSpan(const ADays : System.Integer, const AHours : System.Integer) : System.TimeSpan
 class "System.SystemTimeSpanTimeSpanHoursSelectorNode";
create operator .System.TimeSpan(const ADays : System.Integer) : System.TimeSpan
 class "System.SystemTimeSpanTimeSpanDaysSelectorNode";
```

**Remarks**

`TimeSpan` is an operator and the selector for the `TimeSpan` Datatype.

The `TimeSpan` can accept one to five integers representing days, hours, minutes, seconds, and milliseconds. It will return a `TimeSpan` Datetype.

**Using System.TimeSpan**

```d4
// What will the date be two weeks (14 days) from now?
select Date() + TimeSpan(14);

// What was the time (UTC) between Pearl Harbor bombing
// and cease fire order after Japan's surrender?
// Returned as a TimeSpan
select Date(1945,8,15,23,5) - Date(1941,12,7,17,58);
```

```text
2004-05-07 00:00:00

192wks 3days 5hrs 7min
```

**See Also**

[System.DateTime](System.DateTime.md) \| [System.Date](System.Date.md) \| [System.TimeSpan](System.TimeSpan.md) \| [Using Date and Time Values](../../../developers-guide/d4-programming-topics.md#using-date-and-time-values) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToTimeSpan](../operators/System.ToTimeSpan.md)
