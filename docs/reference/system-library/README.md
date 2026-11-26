# System Library Reference

The System library is the one library every Dataphor application uses. It is built into the engine rather than loaded from disk, and it defines the scalar types a D4 value can have, the operators a D4 expression is made of, and the system tables that describe the catalog. Everything named on these pages is available to any D4 script without being requested in a library descriptor.

These pages are the 3.x product documentation, converted from the DocBook sources the published help collection was built from. An operator page gives the operator's declaration - its D4 signature, as the shipped catalog reported it - and then its remarks, its examples and its cross-references. **A signature here is the record of what shipped, not a reading of today's source.** Where the two disagree, the disagreement is worth knowing rather than quietly fixing, and `docs-reference-generator` is the work item that will emit signatures from a running server instead. Seven pages - six operators and the `System.GroupName` type - carry no declaration block, because the published help file had no page for them to take the signature from.

## Operators

The 297 operator pages are grouped as the printed reference grouped them, and each category page lists the operators in it.

- [Aggregate Operators](operators/System.AggregateOperators.md)
- [Arithmetic Operators](operators/System.ArithmeticOperators.md)
- [Bitwise Operators](operators/System.BitwiseOperators.md)
- [Catalog Operators](operators/System.CatalogOperators.md)
- [Comparison Operators](operators/System.ComparisonOperators.md)
- [Conversion Operators](operators/System.ConversionOperators.md)
- [Cursor Operators](operators/System.CursorOperators.md)
- [Date-Time Operators](operators/System.DateTimeOperators.md)
- [Diagnostics Operators](operators/System.DiagnosticsOperators.md)
- [Dynamic-Execution Operators](operators/System.DynamicExecutionOperators.md)
- [Extractor Operators](operators/System.ExtractorOperators.md)
- [Generator Operators](operators/System.GeneratorOperators.md)
- [GUID Operators](operators/System.GuidOperators.md)
- [Library Operators](operators/System.LibraryOperators.md)
- [List Operators](operators/System.ListOperators.md)
- [Logical Operators](operators/System.LogicalOperators.md)
- [Management Operators](operators/System.ManagementOperators.md)
- [Math Operators](operators/System.MathOperators.md)
- [Nil-Handling Operators](operators/System.NilHandlingOperators.md)
- [Row Operators](operators/System.RowOperators.md)
- [Security Operators](operators/System.SecurityOperators.md)
- [String Operators](operators/System.StringOperators.md)
- [Table Operators](operators/System.TableOperators.md)
- [Transaction Operators](operators/System.TransactionOperators.md)
- [Upgrade Operators](operators/System.UpgradeOperators.md)

## Scalar types

- [System.Binary](types/System.Binary.md)
- [System.Boolean](types/System.Boolean.md)
- [System.Byte](types/System.Byte.md)
- [System.Date](types/System.Date.md)
- [System.DateTime](types/System.DateTime.md)
- [System.Decimal](types/System.Decimal.md)
- [System.Error](types/System.Error.md)
- [System.Graphic](types/System.Graphic.md)
- [System.GroupName](types/System.GroupName.md)
- [System.Guid](types/System.Guid.md)
- [System.Integer](types/System.Integer.md)
- [System.Long](types/System.Long.md)
- [System.Money](types/System.Money.md)
- [System.Name](types/System.Name.md)
- [System.Short](types/System.Short.md)
- [System.String](types/System.String.md)
- [System.Time](types/System.Time.md)
- [System.TimeSpan](types/System.TimeSpan.md)
- [System.UserID](types/System.UserID.md)
- [System.VersionNumber](types/System.VersionNumber.md)

## What is missing: the system tables

The System library also defines the system tables, which let a script read the catalog as data. **This reference does not document them, and nothing in the old sources can be converted into that documentation:** the `SystemTables` chapter is a four-line stub in both copies of the DocBook, saying only that the section was generated, and the published help file carries a list of the table names with no columns, keys or descriptions. A table reference has to be generated from a running server, which is what `docs-reference-generator` will do.

Until then, [the D4 catalog chapter](../../developers-guide/d4-catalog.md) of the Developer's Guide describes what the catalog holds, and a `select` against a live server is the authoritative answer about any particular table.
