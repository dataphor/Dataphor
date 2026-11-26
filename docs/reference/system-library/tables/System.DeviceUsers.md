# System.DeviceUsers

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.DeviceUsers in System.Catalog
{
	User_ID : System.UserID, 
	Device_Name : System.Name static tags { Storage.Name = "C.Name" }, 
	UserID : System.String, 
	ConnectionParameters : System.String, 
	key { .Device_Name, .User_ID } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "StoreView" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
