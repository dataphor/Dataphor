# System.Processes

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.Processes in System.Catalog
{
	ID : System.Integer, 
	Session_ID : System.Integer, 
	DefaultIsolationLevel : System.String, 
	UseDTC : System.Boolean, 
	UseImplicitTransactions : System.Boolean, 
	Language : System.String, 
	MaxStackDepth : System.Integer, 
	MaxNestingLevel : System.Integer, 
	IsRunning : System.Boolean, 
	key { .ID } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "None" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
