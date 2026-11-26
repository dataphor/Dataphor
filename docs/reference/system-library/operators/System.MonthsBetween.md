# System.MonthsBetween

**Remarks**

Returns the number of month boundaries crossed between AStartDate and AEndDate. If AStartDate is less than AEndDate, the result will be a positive value. If AStartDate is greater than AEndDate, the result will be negative.

**Using System.MonthsBetween**

```d4
select MonthsBetween(Date(2007, 1, 1), Date(2007, 4, 1)); // returns 3

select MonthsBetween(Date(2007, 4, 1), Date(2007, 1, 1)); // returns -3
```

**See Also**

[System.Date](../types/System.Date.md)
