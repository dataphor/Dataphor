# System.ListLogs

A system view of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create view .System.ListLogs
(
	(
		.System.Diagnostics.ListLogs()
			adorn
			{
				Sequence tags { Frontend.Title = "Log #" }, 
				LogName tags { Frontend.Title = "Description" }
			} tags { Frontend.Caption = "Select a log to display...", Frontend.Search.Visible = "False" }
	)
)
 tags { Frontend.Caption = "Select a log to display...", Frontend.Search.Visible = "False" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
