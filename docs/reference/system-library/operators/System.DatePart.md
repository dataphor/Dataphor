# System.DatePart

**Declarations**

```d4
create operator .System.DatePart(const AValue : System.DateTime) : System.Date
 class "System.DatePartNode";
```

**Remarks**

`DatePart` returns the date portion of a `DateTime` value as a `Date` value.

**Using System.DatePart**

```d4
select DateTime();
select DatePart(DateTime());
```

```text
2004-04-20 07:10:30
2004-04-20
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.TimePart](System.TimePart.md)
