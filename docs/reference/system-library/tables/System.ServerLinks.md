# System.ServerLinks

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.ServerLinks in System.Catalog
{
	ID : System.Integer, 
	Name : System.Name, 
	Library_Name : System.Name, 
	Owner_User_ID : System.UserID, 
	IsSystem : System.Boolean, 
	IsGenerated : System.Boolean, 
	HostName : System.String, 
	InstanceName : System.String, 
	OverridePortNumber : System.Integer, 
	UseSessionInfo : System.Boolean, 
	key { .ID } static tags { DAE.Enforced = "False" }, 
	key { .Name } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "None" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
