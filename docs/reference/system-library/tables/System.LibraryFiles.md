# System.LibraryFiles

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.LibraryFiles in System.Catalog
{
	Library_Name : System.Name tags { Frontend.Title = "Library Name" }, 
	FileName : System.String tags { Frontend.Title = "Name", Frontend.Width = "50" }, 
	IsAssembly : System.Boolean
	{
		default (true)
	} tags { Frontend.Title = "Register as Assembly" }, 
	key { .Library_Name, .FileName } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.Title = "Files" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
