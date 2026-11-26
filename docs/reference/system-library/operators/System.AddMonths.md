# System.AddMonths

**Declarations**

```d4
create operator .System.AddMonths(const AValue : System.DateTime, const AMonths : System.Integer) : System.DateTime
 class "System.DateTimeAddMonthsNode";
create operator .System.AddMonths(const AValue : System.Date, const AMonths : System.Integer) : System.Date
 class "System.DateAddMonthsNode";
```

**Remarks**

Adds the specified number of months to the given Date or DateTime and returns it.

**Using System.AddMonths**

```d4
select
(
 TableDee add { Date().AddMonths(6) Months }
) // returns the date 6 months from today.
    union
    (
  TableDee add { AddMonths(DateTime(),-1) Months}
 ); // returns a the date represented by 1 month ago from now.
```

**See Also**

[System.Date](../types/System.Date.md)
