# System.DayOfYear

**Declarations**

```d4
create operator .System.DayOfYear(const AValue : System.DateTime) : System.Integer
 class "System.DayOfYearNode";
```

**Remarks**

Returns an Integer that represents the day of the year of the specified DateTime. This will always be a number between 1 and 366.

**Using System.DayOfYear**

```d4
select TableDee add { DateTime(2004,11,18).DayOfYear() Day };
```

```text
Day
---
323
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.DayOfWeek](System.DayOfWeek.md) \| [System.DayOfYear](System.DayOfYear.md) \| [System.DaysInMonth](System.DaysInMonth.md) \| [System.Date](System.Date.md) \| [System.Time](System.Time.md) \| [System.DateTime](System.DateTime.md)
