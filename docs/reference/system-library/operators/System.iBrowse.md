# System.iBrowse

**Declarations**

```d4
create operator .System.iBrowse(ATable : generic table) : generic table
 class "System.BrowseNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`browse` is the parser recognized symbol for `iBrowse`. The `browse` keyword can be used anywhere an `order` can be used. Like `order`, the `browse` clause allows the order in which the rows are to be presented to be specified. Browse also tells the Dataphor Server to use navigational access technology to retrieve the result set. For more information about the browse clause see [Navigational Access](../../../developers-guide/space-between-data.md#navigational-access).

**Using System.iBrowse**

```d4
select Employee browse by { Name };
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

[System.iOrder (`order`)](System.iOrder.md) \| [Navigational Access](../../../developers-guide/space-between-data.md#navigational-access) \| [Ordering The Result Set](../../../developers-guide/d4-data-manipulation.md#ordering-the-result-set)
