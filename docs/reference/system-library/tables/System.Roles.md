# System.Roles

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.Roles in System.Catalog
{
	ID : System.Integer static tags { Storage.Name = "C.ID" }, 
	Name : System.Name static tags { Storage.Name = "C.Name" }, 
	Library_Name : System.Name static tags { Storage.Name = "C.Library_Name" }, 
	Owner_User_ID : System.UserID, 
	IsSystem : System.Boolean, 
	IsGenerated : System.Boolean, 
	key { .ID } static tags { DAE.Enforced = "False" }, 
	key { .Name } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "StoreView" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
