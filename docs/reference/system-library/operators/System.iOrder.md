# System.iOrder

**Declarations**

```d4
create operator .System.iOrder(ATable : generic table) : generic table
 class "System.OrderNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`order` is the parser recognized symbol for `iOrder`. This clause modifies the ordering of rows in a cursor definition. The `order by` clause returns the list of rows ordered by the contents of the columns specified.

**Using System.iOrder**

```d4
select Employee order by { Name };
```

```text
ID Name 
-- ---- 
5  Jake 
4  Jeff 
6  Jeff 
1  Joe  
2  John 
3  Josh
```

**See Also**

[Ordering the Result Set](../../../developers-guide/d4-data-manipulation.md#ordering-the-result-set) \| [Orders](../../../developers-guide/d4-catalog.md#orders) \| [Select Statement](../../../developers-guide/d4-data-manipulation.md#select-statement) \| [System.iBrowse](System.iBrowse.md)
