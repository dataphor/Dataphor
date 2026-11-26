# System.ViewLog

A system view of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create view .System.ViewLog
(
	(
		table of { Log : System.String }
		{
			row { .System.Diagnostics.ShowLog() Log }
		}
			adorn
			{
				Log tags { Frontend.Width = "120", Frontend.TextBox.Height = "30" }
			} tags { Frontend.Title = "Log" }
	)
)
 tags { Frontend.Title = "Log" };
```

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
