# System.Objects

A system table of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create table .System.Objects in System.Catalog
{
	ID : System.Integer, 
	Name : System.String, 
	Library_Name : System.String, 
	DisplayName : System.String tags { Frontend.Preview.Include = "True" }, 
	Description : System.String, 
	Type : System.String, 
	IsSystem : System.Boolean, 
	IsRemotable : System.Boolean, 
	IsGenerated : System.Boolean, 
	IsATObject : System.Boolean, 
	IsSessionObject : System.Boolean, 
	IsPersistent : System.Boolean, 
	Catalog_Object_ID : System.Integer, 
	Parent_Object_ID : System.Integer, 
	Generator_Object_ID : System.Integer, 
	ServerData : System.String nil
		tags
		{ 
			
			Frontend.TextBox.Height = "10", 
			Frontend.TextBox.WordWrap = "False", 
			Frontend.TextBox.Width = "60"
		}, 
	key { .ID } static tags { DAE.Enforced = "False" }
	
} tags { Frontend.UseList = "True", static Catalog.CacheLevel = "StoreTable" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
