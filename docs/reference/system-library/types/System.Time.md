# System.Time

**Type Declaration**

```d4
create type .System.Time
{
    representation Ticks
    {
        Ticks : System.Long
    } selector  class "System.SystemTimeTicksSelectorNode", 
    representation Time
    {
        Hour : System.Integer, 
        Minute : System.Integer,
        Second : System.Integer
    } selector  class "System.SystemTimeTimeSecondsSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.TimeAsStringSelectorNode"
} class "System.DateTimeConveyor" static tags { Catalog.Comment = "System Time", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Time(const Hour : System.Integer, const Minute : System.Integer, const Second : System.Integer) : System.Time
 class "System.SystemTimeTimeSecondsSelectorNode";
create operator .System.Time(const AHours : System.Integer, const AMinutes : System.Integer) : System.Time
 class "System.SystemTimeTimeMinutesSelectorNode";
create operator .System.Time() : System.Time
 class "System.TimeNode" static tags { DAE.IsDeterministic = "False" };
```

**Remarks**

`Time` is an operator and the selector for the `Time` Datatype.

**Using System.Time**

```d4
create table Test in Temp
{
 Event : String,
 When : Time, key { Event }
};
insert table 
{
 row { '1. Get up' Event, Time(5, 30) When },
 row { '2. Go to work', Time(7, 00, 18) },
 row { '3. Go home', Time(16, 30) },
 row { '4. Sleep', Time(21, 30, 54) }
} into Test;
select Test;
select "Time awake as TimeSpan "
 + (
    (When from row from Test where Event = "4. Sleep")
    - (When from row from Test where Event = "1. Get up")
   ).AsString;
drop table Test;
```

```text
Event         When
------------- --------
1. Get Up     05:30:00
2. Go to work 07:00:18
3. Go home    16:30:00
4. Sleep      21:30:54

Time awake as TimeSpan 16hrs 0min 54sec
```

**See Also**

[System.Time Operator](../operators/System.Time.md) \| [System.DateTime](System.DateTime.md) \| [System.Date](System.Date.md) \| [System.TimeSpan](System.TimeSpan.md) \| [Using Date and Time Values](../../../developers-guide/d4-programming-topics.md#using-date-and-time-values) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToTime](../operators/System.ToTime.md)
