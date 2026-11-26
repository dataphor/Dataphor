# System.IsLeapYear

**Declarations**

```d4
create operator .System.IsLeapYear(const AYear : System.Integer) : System.Boolean
 class "System.IsLeapYearNode";
```

**Remarks**

`IsLeapYear` specifies whether or not the specified year is a leap year. Leap years are years that are divisible by 4, unless they are divisible by 100. If a year is divisible by 100 then it is only a leap year if it is divisible by 400.

**Using System.IsLeapYear**

```d4
select TableDee add { IsLeapYear(2000) Leap };
```

```text
Leap
----
True
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md)
