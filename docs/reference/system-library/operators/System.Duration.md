# System.Duration

**Declarations**

```d4
create operator .System.Duration(const ATimeSpan : System.TimeSpan) : System.TimeSpan
 class "System.DurationNode";
```

**Remarks**

This is a TimeSpan operator that returns a positive TimeSpan value for the specified value. It is equivalent to the Absolute value (`System.Abs`) operator for numberic values.

**Using System.Duration**

```d4
select TableDee add { Duration(-TimeSpan.Ticks(10000000)) Duration };
```

```text
Duration
--------
1sec
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.TimeSpan Scalar Type](../types/System.TimeSpan.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.Abs](System.Abs.md)
