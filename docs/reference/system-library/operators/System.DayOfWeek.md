# System.DayOfWeek

**Declarations**

```d4
create operator .System.DayOfWeek(const AValue : System.DateTime) : System.Integer
 class "System.DayOfWeekNode";
```

**Remarks**

`DayOfWeek` returns an Integer from 0 to 6 representing the day of the week. Sunday is 0, Monday 1, all the way to Saturday being 6.

**Using System.DayOfWeek**

```d4
select TableDee
 add
 {
  DateTime(2004,04,19).DayOfWeek() Day19,
  DateTime(2004,04,24).DayOfWeek() Day24
 };
```

```text
Day19 Day24
----- -----
0     6
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.DayOfYear](System.DayOfYear.md) \| [System.DaysInMonth](System.DaysInMonth.md) \| [System.Date](System.Date.md) \| [System.Time](System.Time.md) \| [System.DateTime](System.DateTime.md)
