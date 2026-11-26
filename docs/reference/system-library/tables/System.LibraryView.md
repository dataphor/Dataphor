# System.LibraryView

A system view of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create view .System.LibraryView
(
	(
		(
			(
				.System.Libraries
					left join
					(
						.System.LibraryVersions
							rename
							{
								.LibraryName Name tags { Frontend.Width = "50" }, 
								.Version LoadedVersion
									tags
									{ 
										
										Frontend.Width = "15", 
										Frontend.TextBox.MaxLength = "14", 
										Frontend.TextBox.MaxWidth = "15"
									}
							}
					)
			)
				add
				{
					.System.IfNil((.Version > .LoadedVersion), false) UpgradeRequired
				}
		)
			adorn
			{
				LoadedVersion tags { Frontend.Title = "Loaded Version", Frontend.FlowBreak = "True" }, 
				UpgradeRequired tags { Frontend.Title = "Upgrade Required?" }
			} tags { Frontend.Title = "Library" }
	)
)
 tags { Frontend.UseApplicationTransactions = "True", Frontend.Title = "Library" };
```

**Built From**

[System.Libraries](System.Libraries.md) \| [System.LibraryVersions](System.LibraryVersions.md)

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
