# System.DeviceOperators

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.DeviceOperators in System.Catalog
{
	Name : System.Name static tags { Storage.Name = "O.Name" }, 
	Device_Name : System.Name static tags { Storage.Name = "D.Name" }, 
	Operator_Name : System.Name static tags { Storage.Name = "S.Name" }, 
	key { .Name } static tags { DAE.Enforced = "False" }, 
	key { .Device_Name, .Operator_Name } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "StoreView" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
