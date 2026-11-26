# System.YearsBetween

**Remarks**

Returns the number of year boundaries crossed between AStartDate and AEndDate. If AStartDate is less than AEndDate, the result will be a positive value. If AStartDate is greater than AEndDate, the result will be negative.

**Using System.YearsBetween**

```d4
select YearsBetween(Date(2004, 1, 1), Date(2007, 1, 1)); // returns 3

select YearsBetween(Date(2007, 1, 1), Date(2004, 1, 1)); // returns -3
```

**See Also**

[System.Date](../types/System.Date.md)
