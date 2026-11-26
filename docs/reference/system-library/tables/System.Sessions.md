# System.Sessions

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.Sessions in System.Catalog
{
	ID : System.Integer, 
	User_ID : System.UserID, 
	HostName : System.String, 
	Connection_Name : System.String nil, 
	Environment : System.String nil, 
	Current_Library_Name : System.Name, 
	DefaultIsolationLevel : System.String, 
	DefaultUseDTC : System.Boolean, 
	DefaultUseImplicitTransactions : System.Boolean, 
	Language : System.String, 
	FetchCount : System.Integer, 
	DefaultMaxStackDepth : System.Integer, 
	DefaultMaxCallDepth : System.Integer, 
	UsePlanCache : System.Boolean, 
	ShouldEmitIL : System.Boolean, 
	ShouldElaborate : System.Boolean, 
	key { .ID } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "true", static Catalog.CacheLevel = "None" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
