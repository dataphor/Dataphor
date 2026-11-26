# System.Users

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.Users in System.Catalog
{
	ID : System.UserID, 
	Name : System.String, 
	key { .ID } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "StoreView" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
