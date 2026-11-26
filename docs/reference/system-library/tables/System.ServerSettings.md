# System.ServerSettings

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.ServerSettings in System.Catalog
{
	Name : System.Name, 
	Version : System.String, 
	LogErrors : System.Boolean, 
	TimeStamp : System.Long, 
	CacheTimeStamp : System.Long, 
	PlanCacheTimeStamp : System.Long, 
	DerivationTimeStamp : System.Long, 
	InstanceDirectory : System.String, 
	LibraryDirectory : System.String, 
	IsEngine : System.Boolean, 
	MaxConcurrentProcesses : System.Integer, 
	ProcessWaitTimeout : System.TimeSpan, 
	ProcessTerminateTimeout : System.TimeSpan, 
	PlanCacheSize : System.Integer, 
	key {  } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "None" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
