# System.DaysInMonth

**Declarations**

```d4
create operator .System.DaysInMonth(const AYear : System.Integer, const AMonth : System.Integer) : System.Integer
 class "System.DaysInMonthNode";
```

**Remarks**

This returns an Integer representing the number of days in the specified month of the specified year. This number will always be between 28 and 31 inclusive.

**Using System.DaysInMonth**

```d4
select TableDee add { DaysInMonth(2004,2) Days }; 
```

```text
Days
----
29
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.DayOfWeek](System.DayOfWeek.md) \| [System.DayOfYear](System.DayOfYear.md) \| [System.Date](System.Date.md) \| [System.Time](System.Time.md) \| [System.DateTime](System.DateTime.md)
