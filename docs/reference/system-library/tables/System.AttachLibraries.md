# System.AttachLibraries

A system view of the [System library](../README.md): part of the catalog, which a D4 script reads as data. **This page is generated from a running server**, so a hand edit here is reverted the next time the pages are written; the [system tables index](README.md) says what the definition below does and does not carry.

**Definition**

```d4
create view .System.AttachLibraries
(
	(
		(
			.System.Libraries
				over { .Directory }
		)
			adorn
			{
				Directory tags { Frontend.Hint = "Server-side physical directory containing the library directories to load." }
			} tags { Frontend.UseApplicationTransactions = "False", Frontend.Add.Caption = "Attach Libraries" }
	)
)
 tags { Frontend.UseApplicationTransactions = "False", Frontend.Add.Caption = "Attach Libraries" };
```

**Built From**

[System.Libraries](System.Libraries.md)

**See Also**

[System Tables](README.md) \| [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) \| [System Library](../README.md)
