# System.AddYears

**Declarations**

```d4
create operator .System.AddYears(const AValue : System.DateTime, const AYears : System.Integer) : System.DateTime
 class "System.DateTimeAddYearsNode";
create operator .System.AddYears(const AValue : System.Date, const AYears : System.Integer) : System.Date
 class "System.DateAddYearsNode";
```

**Remarks**

Adds the specified number of Years to the given DateTime and returns it.

**Using System.AddYears**

```d4
select TableDee add {Date().AddYears(6) SixYears};
 // returns the date 6 years from today.
select TableDee add {AddYears(DateTime(),-1) LastYear};
 // returns the date represented by 1 year ago from now.
```

**See Also**

[System.Date](../types/System.Date.md)
