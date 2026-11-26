# Programming Topics

This section discusses common programming tasks in D4. Topics covered include items such as cursors, generators, and event handlers.

## Using Strings

This section describes how to use the [System.String](../reference/system-library/types/System.String.md) data type to construct and manipulate string values in D4. To begin with, we provide the definition of the `System.String` scalar type:

```d4
create type System.String
{
    representation String 
    { 
        Value : String 
            read class "System.ScalarReadAccessor"
            write class "System.ScalarWriteAccessor"
    } class "System.ScalarSelectorNode",
    representation Unicode
    {
        Unicode : list(System.Integer)
            read class "System.SystemStringReadUnicodeNode"
            write class "System.SystemStringWriteUnicodeNode"
    } class "System.SystemStringUnicodeNode",
    representation UTF8
    {
        UTF8 : list(System.Byte)
            read class "System.SystemStringReadUTF8Node"
            write class "System.SystemStringWriteUTF8Node"
    } class "System.SystemStringUTF8Node,
    representation ASCII
    {
        ASCII : list(System.Byte)
            read class "System.SystemStringReadASCIINode"
            write class "System.SystemStringWriteASCIINode"
    } class "System.SystemStringASCIINode"
} class "System.StringConveyor";
```

`System.String` has four possible representations:

- `String`
- `Unicode`
- `UTF8`
- `ASCII`

Each possible representation has an associated selector and read and write accessors for the properties of the representation. The default representation `String` has a single property called `Value` of type `String`. This is the native representation and is required because values of type `String` have a parser literal.

The `Unicode` representation of a string is a list of values of type `System.Integer`, where each integer in the list represents the Unicode character constant for the character in that position in the string. For example, the following Unicode selector invocation:

```d4
Unicode({ 0x41, 0x42, 0x43 })
```

produces the string `"ABC"`.

The `UTF8` representation of a string is a list of values of type `System.Byte`, where each byte in the list represents the UTF-8 character constant for the character in that position in the string. For example, the following UTF8 selector invocation:

```d4
UTF8({ 65 as Byte, 66 as Byte, 67 as Byte })
```

produces the string `"ABC"`.

The `ASCII` representation of a string is a list of values of type `System.Byte`, where each byte in the list represents the ASCII character constant for the character in that position in the string. For example, the following ASCII selector invocation:

```d4
ASCII({ 65 as Byte, 66 as Byte, 67 as Byte })
```

produces the string `"ABC"`.

The Dataphor Server provides the following string operators:

- String [Indexer](d4-language-elements.md#indexer-expression)

  Allows a specific character in the string to be addressed by its ordinal position within the string. Indexes are zero-based.

- String Concatenation

  The [+](../reference/system-library/operators/System.iAddition.md) operator is used for string concatenation, resulting in the string with the characters of the first string prefixing the characters of the second string.

- [Length](../reference/system-library/operators/System.Length.md)

  Determines the length of the given string.

- [SubString](../reference/system-library/operators/System.SubString.md)

  Copies the characters from the given string beginning at a specified index, and for a specified length.

- [Pos](../reference/system-library/operators/System.Pos.md)

  Returns the index of the first position of a given substring within a given string. Returns -1 if the given substring is not in the given string.

- [IndexOf](../reference/system-library/operators/System.IndexOf.md)

  Returns the index of the first position of a given substring within a given string. Returns -1 if the given substring is not in the given string. This operator is the same as `Pos`, with the arguments reversed. This enables `IndexOf` to be invoked with OOP-style method invocation.

- [Upper](../reference/system-library/operators/System.Upper.md)

  Returns the string containing the characters in the given string, upper-cased.

- [Lower](../reference/system-library/operators/System.Lower.md)

  Returns the string containing the characters in the given string, lower-cased.

- [Like](../reference/system-library/operators/System.iLike.md)

  Returns true if the given pattern is found in the given string.

- [Matches](../reference/system-library/operators/System.iMatches.md)

  Returns true if the given regular expression matches the given string.

- [CompareText](../reference/system-library/operators/System.CompareText.md)

  Performs a case-insensitive compare of two strings.

- [Aggregate Operators](d4-table-operators.md#aggregate)

  The following aggregate operators are defined for strings:

  - [Count](../reference/system-library/operators/System.Count.md)

    Determines the cardinality of the argument.

  - [Min](../reference/system-library/operators/System.Min.md)

    Defined as iterated less than comparison.

  - [Max](../reference/system-library/operators/System.Max.md)

    Defined as iterated greater than comparison.

## Using Date and Time Values

This section describes how to use the [System.TimeSpan](../reference/system-library/types/System.TimeSpan.md), [System.DateTime](../reference/system-library/types/System.DateTime.md), [System.Date](../reference/system-library/types/System.Date.md), and [System.Time](../reference/system-library/types/System.Time.md) data types and their associated operators to construct and manipulate date and time values in D4. To begin with, we provide the definition of these types:

```d4
create type TimeSpan
{
    representation Ticks
    {
        Ticks : Long
            read class "System.SystemTimeSpanTicksReadTicksNode"
            write class "System.SystemTimeSpanTicksWriteTicksNode"
    } class "System.SystemTimeSpanTicksSelectorNode", 
    representation Milliseconds
    {
        Milliseconds : Decimal
            read class "System.SystemTimeSpanMillisecondsReadMillisecondsNode"
            write class "System.SystemTimeSpanMillisecondsWriteMillisecondsNode"
    } class "System.SystemTimeSpanMillisecondsSelectorNode", 
    representation Seconds
    {
        Seconds : Decimal
            read class "System.SystemTimeSpanSecondsReadSecondsNode"
            write class "System.SystemTimeSpanSecondsWriteSecondsNode"
    } class "System.SystemTimeSpanSecondsSelectorNode", 
    representation Minutes
    {
        Minutes : Decimal
            read class "System.SystemTimeSpanMinutesReadMinutesNode"
            write class "System.SystemTimeSpanMinutesWriteMinutesNode"
    } class "System.SystemTimeSpanMinutesSelectorNode", 
    representation Hours
    {
        Hours : Decimal
            read class "System.SystemTimeSpanHoursReadHoursNode"
            write class "System.SystemTimeSpanHoursWriteHoursNode"
    } class "System.SystemTimeSpanHoursSelectorNode", 
    representation Days
    {
        Days : Decimal
            read class "System.SystemTimeSpanDaysReadDaysNode"
            write class "System.SystemTimeSpanDaysWriteDaysNode"
    } class "System.SystemTimeSpanDaysSelectorNode", 
    representation TimeSpan
    {
        Day : Integer
            read class "System.SystemTimeSpanTimeSpanReadDayNode"
            write class "System.SystemTimeSpanTimeSpanWriteDayNode", 
        Hour : Integer
            read class "System.SystemTimeSpanTimeSpanReadHourNode"
            write class "System.SystemTimeSpanTimeSpanWriteHourNode", 
        Minute : Integer
            read class "System.SystemTimeSpanTimeSpanReadMinuteNode"
            write class "System.SystemTimeSpanTimeSpanWriteMinuteNode", 
        Second : Integer
            read class "System.SystemTimeSpanTimeSpanReadSecondNode"
            write class "System.SystemTimeSpanTimeSpanWriteSecondNode", 
        Millisecond : Decimal
            read class "System.SystemTimeSpanTimeSpanReadMillisecondNode"
            write class "System.SystemTimeSpanTimeSpanWriteMillisecondNode"
    } class "System.SystemTimeSpanTimeSpanMillisecondsSelectorNode",
    representation AsString
    {
        AsString : String
            read class "System.TimeSpanAsStringReadAccessorNode"
            write class "System.TimeSpanAsStringWriteAccessorNode"
    } class "System.TimeSpanAsStringSelectorNode"
} 
    class "System.TimeSpanConveyor"
    static tags 
    { 
        Catalog.Comment = "System Time Span", 
        DAE.AsString = "AsString"
    };

create type DateTime
{
    representation Ticks
    {
        Ticks : Long
            read class "System.SystemDateTimeTicksReadTicksNode"
            write class "System.SystemDateTimeTicksWriteTicksNode"
    } class "System.SystemDateTimeTicksSelectorNode", 
    representation DateTime
    {
        Year : Integer
            read class "System.SystemDateTimeDateTimeReadYearNode"
            write class "System.SystemDateTimeDateTimeWriteYearNode", 
        Month : Integer
            read class "System.SystemDateTimeDateTimeReadMonthNode"
            write class "System.SystemDateTimeDateTimeWriteMonthNode", 
        Day : Integer
            read class "System.SystemDateTimeDateTimeReadDayNode"
            write class "System.SystemDateTimeDateTimeWriteDayNode", 
        Hour : Integer
            read class "System.SystemDateTimeDateTimeReadHourNode"
            write class "System.SystemDateTimeDateTimeWriteHourNode", 
        Minute : Integer
            read class "System.SystemDateTimeDateTimeReadMinuteNode"
            write class "System.SystemDateTimeDateTimeWriteMinuteNode", 
        Second : Integer
            read class "System.SystemDateTimeDateTimeReadSecondNode"
            write class "System.SystemDateTimeDateTimeWriteSecondNode"
    } class "System.SystemDateTimeDateTimeSecondsSelectorNode",
    representation AsString
    {
        AsString : String
            read class "System.DateTimeAsStringReadAccessorNode"
            write class "System.DateTimeAsStringWriteAccessorNode"
    } class "System.DateTimeAsStringSelectorNode"
} 
    class "System.DateTimeConveyor"
    static tags 
    { 
        Catalog.Comment = "System Date Time", 
        DAE.AsString = "AsString" 
    };
    
create type Date
{
    representation Ticks
    {
        Ticks : Long
            read class "System.SystemDateTicksReadTicksNode"
            write class "System.SystemDateTicksWriteTicksNode"
    } class "System.SystemDateTicksSelectorNode", 
    representation Date
    {
        Year : Integer
            read class "System.SystemDateTimeDateTimeReadYearNode"
            write class "System.SystemDateTimeDateTimeWriteYearNode", 
        Month : Integer
            read class "System.SystemDateTimeDateTimeReadMonthNode"
            write class "System.SystemDateTimeDateTimeWriteMonthNode", 
        Day : Integer
            read class "System.SystemDateTimeDateTimeReadDayNode"
            write class "System.SystemDateTimeDateTimeWriteDayNode"
    } class "System.SystemDateTimeDateTimeDaysSelectorNode",
    representation AsString
    {
        AsString : String
            read class "System.DateAsStringReadAccessorNode"
            write class "System.DateAsStringWriteAccessorNode"
    } class "System.DateAsStringSelectorNode"
}
    class "System.DateTimeConveyor"
    static tags 
    { 
        Catalog.Comment = "System Date", 
        DAE.AsString = "AsString",
        Frontend.Width = "10",
        Frontend.DateTimeBox.MaxWidth = "10"
    };
    
create type Time
{
    representation Ticks
    {
        Ticks : Long
            read class "System.SystemTimeTicksReadTicksNode"
            write class "System.SystemTimeTicksWriteTicksNode"
    } class "System.SystemTimeTicksSelectorNode", 
    representation Time
    {
        Hour : Integer
            read class "System.SystemTimeTimeReadHourNode"
            write class "System.SystemTimeTimeWriteHourNode", 
        Minute : Integer
            read class "System.SystemTimeTimeReadMinuteNode"
            write class "System.SystemTimeTimeWriteMinuteNode", 
        Second : Integer
            read class "System.SystemTimeTimeReadSecondNode"
            write class "System.SystemTimeTimeWriteSecondNode"
    } class "System.SystemTimeTimeSecondsSelectorNode",
    representation AsString
    {
        AsString : String
            read class "System.TimeAsStringReadAccessorNode"
            write class "System.TimeAsStringWriteAccessorNode"
    } class "System.TimeAsStringSelectorNode"
}
    class "System.DateTimeConveyor"
    static tags 
    { 
        Catalog.Comment = "System Time", 
        DAE.AsString = "AsString",
        Frontend.ElementType = "TextBox",
        Frontend.Width = "10",
        Frontend.TextBox.MaxWidth = "10"
    };
```

`System.TimeSpan` has seven possible representations:

- `TimeSpan`

  The TimeSpan representation is the default representation for the TimeSpan type, and has five properties:

  - `DaysPart`
  - `HoursPart`
  - `MinutesPart`
  - `SecondsPart`
  - `MillisecondsPart`

  corresponding to the days, hours, minutes, seconds, and milliseconds components of the `TimeSpan`, respectively.

  The `TimeSpan` selector takes a value for each of these properties in order to construct a `TimeSpan` value. For example:

  ```text
  TimeSpan(10, 2, 55, 30, 12)
  ```

  builds the `TimeSpan` value of 10 days, 2 hours, 55 minutes, 30 seconds, and 12 milliseconds. In addition to the basic selector, the Dataphor Server defines overloads for the `TimeSpan` operator, which allow the secondary arguments to be ommitted:

  ```d4
  TimeSpan
  (
      ADaysPart : Integer,
      AHoursPart : Integer,
      AMinutesPart : Integer,
      ASecondsPart : Integer
  );

  TimeSpan
  (
      ADaysPart : Integer,
      AHoursPart : Integer,
      AMinutesPart : Integer
  );

  TimeSpan(ADaysPart : Integer, AHoursPart : Integer);

  TimeSpan(ADaysPart : Integer);
  ```

  As with all possible representations, each of these components can be updated independently of the others. Note that this is only a shorthand for an equivalent selector invocation:

  ```d4
  begin
      var LTimeSpan : TimeSpan := TimeSpan(10, 5);
      LTimeSpan.Hours := 6;
      // Equivalent selector invocation
      LTimeSpan := TimeSpan(LTimeSpan.Days, 6);
  end;
  ```

- `Days`

  The `Days` representation has a single property called `Days` which is the total number of days in the given TimeSpan expressed as a decimal value. For example, the following selector invocations are equivalent:

  ```d4
  Days(10.5);
  TimeSpan(10, 12);
  ```

- `Hours`

  The `Hours` representation has a single property called `Hours` which is the total number of hours in the given `TimeSpan` expressed as a decimal value.

- `Minutes`

  The `Minutes` representation has a single property called `Minutes` which is the total number of minutes in the given `TimeSpan` expressed as a decimal value.

- `Seconds`

  The `Seconds` representation has a single property called `Seconds` which is the total number of seconds in the given `TimeSpan` expressed as a decimal value.

- `Milliseconds`

  The `Milliseconds` representation has a single property called `Milliseconds` which is the total number of milliseconds in the given `TimeSpan` expressed as a decimal value.

- `Ticks`

  The `Ticks` representation has a single property called `Ticks` which is the total number of ticks in the given `TimeSpan` expressed as a long value.

`System.DateTime` introduces the default representation `DateTime`. This representation has seven properties:

- `YearsPart`
- `MonthsPart`
- `DaysPart`
- `HoursPart`
- `MinutesPart`
- `SecondsPart`

corresponding to the years, months, days, hours, minutes, and seconds of the `DateTime`, respectively.

The `DateTime` selector takes a value for each of these properties in order to construct a `DateTime` value. For example:

```d4
DateTime(2002, 9, 23, 7, 35, 15, 0);
```

builds the `DateTime` value September 23, 2002 at 7:35:15 AM. In addition to the basic selector, the Dataphor Server defines overloads for the `DateTime` operator which allow the secondary arguments to be ommitted:

```d4
DateTime
(
    AYearsPart : Integer,
    AMonthsPart : Integer,
    ADaysPart : Integer,
    AHoursPart : Integer,
    AMinutesPart : Integer
);

DateTime
(
    AYearsPart : Integer,
    AMonthsPart : Integer,
    ADaysPart : Integer
);
```

As with all scalar types, the properties of all possible representations are available for each value of the type. For example:

```d4
begin
    var LTimeSpan : TimeSpan := Days(10.5);
    /* 
       HoursPart is available even though the 
       Days selector was used to build the value
    */
    raise Error(LTimeSpan.HoursPart.ToString()); 
end;
```

The Dataphor Server provides the following date and time operators:

- `DatePart`

  Retrieves only the date portion of a `DateTime` value.

- `TimePart`

  Retrieves only the time portion of a `DateTime` value.

- `DateTime`

  Returns the current date and time. Note that this operator returns the same value on successive invocations within the same transaction.

- `Date`

  Returns the current date. Note that this operator returns the same value on successive invocations within the same transaction.

- `Time`

  Returns the current time. Note that this operator returns the same value on successive invocations within the same transaction.

- `DayOfWeek`

  Returns the day of the week of a `DateTime` value.

- `DayOfYear`

  Returns the day of the year of a `DateTime` value.

- `DaysInMonth`

  Returns the number of days in the given month and year.

- `IsLeapYear`

  Returns `true` if the given year is a leap year, and `false` otherwise.

- `Duration`

  Returns the duration of a given `TimeSpan`.

- `AddMonths`

  Adds the given number of months to a `DateTime` value.

- `AddYears`

  Adds the given number of years to a `DateTime` value.

- Arithmetic Operators

  - `iAddition`(`+`)

    Adds two `TimeSpan` values together, resulting in a `TimeSpan` that is the total of both.

  - `iSubtraction`(`-`)

    Subtracts two `TimeSpan` values, resulting in a `TimeSpan` that is the difference between the first and second values.

  - `iMultiplication`(`*`)

    Multiplies a given `TimeSpan` by a `Decimal` value, resulting in a `TimeSpan` that is the specified number of times longer than the original value.

  - `iDivision`(`/`)

    Divides a given `TimeSpan` by a `Decimal` value, resulting in a `TimeSpan` that is the specified number of times shorter than the original value.

  - `iNegate`(unary `-`)

    Returns the `TimeSpan` that is the negative length of the original value. Just as in integer arithmetic, negating a negative `TimeSpan` value results in a positive value.

- Comparison Operators

  All the comparison operators (`=`, `<>`, `<`, `>`, `<=`, `>=`, and `?=`) are defined for `TimeSpan` and `DateTime` values, with the expected semantics.

- Aggregate Operators

  The following aggregate operators are defined for `TimeSpan` and `DateTime` values:

  - `Count`

    Determines the cardinality of the argument.

  - `Sum`

    Defined as iterated addition.

  - `Min`

    Defined as iterated less than comparison.

  - `Max`

    Defined as iterated greater than comparison.

  - `Avg`

    Defined as iterated addition divided by the count.
