# System Library Reference

The System library is the one library every Dataphor application uses. It is built into the engine rather than loaded from disk, and it defines the scalar types a D4 value can have, the operators a D4 expression is made of, and the system tables that describe the catalog. Everything named on these pages is available to any D4 script without being requested in a library descriptor.

These pages are the 3.x product documentation, converted from the DocBook sources the published help collection was built from. An operator page gives the operator's declaration - its D4 signature, as the shipped catalog reported it - and then its remarks, its examples and its cross-references. **A signature here is the record of what shipped, not a reading of today's source.** Where the two disagree, the disagreement is worth knowing rather than quietly fixing; *What the running product says*, below, is where to read what this release reports instead. Eight pages - six operators, `System.iXor` and the `System.GroupName` type - carry no declaration block, because the published help file had no page for them to take the signature from.

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

## What the running product says

The signatures on these pages are the 2007 published record and this release is 4.0, so some of them no longer match. Rather than leave that to chance, the test suite measures it: `ReferenceSignatureGoldenTest` asks a server carrying nothing but the built-in System library for the same declarations, through the `Script` operator any script can call, and holds each one against the page. The generated text is checked in under `tests/goldens/reference/system-library/`, which is **the record of what this release actually declares** - one file per operator name, one per scalar type, and `operator-inventory.txt`, which lists every System operator name with its overload count and whether a page documents it.

What that measurement found, and what it means for reading these pages:

- **Of the 290 operator pages with a declaration block, 214 match this release** once the tabs and line endings the help file's conversion changed are set aside. For those, the page is current.
- **The other 76 differ, and none of the differences is a changed signature introduced by the .NET port** - each was traced to a change that had already happened in the 3.x product after the help file was published. Most are extra metadata tags the product gained, or an operator body printed as its source text rather than as the compiler's rewriting of it. Thirteen are substantive for a reader: the page lists fewer overloads than the product has - the `Money` and `Long` arms of `Abs`, `Avg`, `Ceiling`, `Floor`, `Frac`, `Max`, `Min`, `Sum`, `Truncate` and `iDivision`, the string arms of `IndexOf` and `LastIndexOf`, and one extra arm of `ToList`. **For those thirteen names, read the checked-in file rather than the page.**
- **Every scalar type's *Type Declaration* block differs,** because the product now writes a type's representations as separate `alter type` statements instead of inside the `create type`. The type, its representations and their selectors are the same; only the shape of the statement is different.
- **301 System operator names have no page at all.** The inventory file is the only list of them.

These pages are deliberately not rewritten to match: they are the baseline the comparison is measured against, and a page corrected to 4.0 would make the next comparison prove nothing. The one exception was `System.RequiredObjects`, whose block carried `System.RequiredLibraries`' declarations - a conversion error, not a product difference - and has been corrected.

## System tables

The System library also defines the system tables, which let a script read the catalog as data: `TableVars` lists every table the server has, `Operators` every operator, `Libraries` every library it knows about. There is a page for each of them under [System Tables](tables/README.md) - 48 tables and 7 views - carrying the definition a running server reports.

**Those pages are generated, which makes them unlike the rest of this reference.** The 3.x documentation never published a system tables chapter: its `SystemTables` section was a four-line placeholder saying the section was generated, and the published help file listed the table names with nothing under them. So there was no text to convert and nothing to check a hand-written replacement against, and the only source is a running server - the same mechanism as *What the running product says* above, pointed at the `TableVars` system table rather than at `Operators`. There is no source for the descriptions that chapter was meant to carry either, and none were written; [the index](tables/README.md) says so, and [the D4 catalog chapter](../../developers-guide/d4-catalog.md#table-variables) of the Developer's Guide is where the narrative about the catalog is.
