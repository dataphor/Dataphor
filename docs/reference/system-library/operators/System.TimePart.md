# System.TimePart

**Declarations**

```d4
create operator .System.TimePart(const AValue : System.DateTime) : System.Time
 class "System.TimePartNode";
```

**Remarks**

`TimePart` returns the time portion of a DateTime value as a Time value.

**Using System.TimePart**

```d4
select TableDee add {TimePart(DateTime(2063,4,5,13,25,37)) Time};
```

```text
Time
--------
13:25:37
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.DatePart](System.DatePart.md)
