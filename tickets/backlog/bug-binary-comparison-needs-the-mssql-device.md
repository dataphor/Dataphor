description: On a server set up for SQL databases but without the SQL Server part loaded, any query or script that asks whether two pictures or documents are different fails with an internal error instead of answering.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/SQLDevice/Documents/Register.d4, libraries/SQLDevice/SQLDevice.d4l, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLInstructionNodes.cs, libraries/MSSQLDevice/MSSQLDevice.d4l
repro: verified
severity: edge-case
likelihood: unusual
tradeoffs: The configuration that breaks is a server with the shared SQL-device library registered but the SQL Server one not, which only a PostgreSQL-only or SQL-device-only deployment reaches; and every repair either moves a published type to another library or adds code to one, both of which an existing application could notice, so a maintainer may well prefer to leave it and document the configuration instead.
----
## What happens

`libraries/SQLDevice/Documents/Register.d4` is the registration script of the shared library that the SQL Server and PostgreSQL devices are both built on. Around line 48 it declares a type of its own:

```
create type MSSQLBinary like System.Binary tags { Frontend.Grid.ElementType = "TextColumn" };

create operator iCompare(const ALeftValue : MSSQLBinary, const ARightValue : MSSQLBinary) : Integer class "MSSQLDevice.MSSQLMSSQLBinaryCompareNode" ...;
create operator iNotEqual(const ALeftValue : MSSQLBinary, const ARightValue : MSSQLBinary) : Boolean class "MSSQLDevice.MSSQLMSSQLBinaryNotEqualNode" ...;
create operator iLess(...) ... class "MSSQLDevice.MSSQLMSSQLBinaryLessNode" ...;
```

- seven comparison operators in all, every one of them implemented by a class that lives in a **different** library's assembly: `MSSQLDevice` (`libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLInstructionNodes.cs`).

`libraries/SQLDevice/SQLDevice.d4l` requires only `System` and `Connection`. So a server can perfectly well have the shared library registered and the SQL Server one not, and when it does, those seven operators are in the catalog pointing at classes that cannot be loaded.

That would be harmless if nothing resolved to them. But `like System.Binary` means `MSSQLBinary` converts implicitly to and from `Binary`, and the engine has no `iNotEqual`, `iCompare`, `iLess`, `iGreater`, `iInclusiveLess` or `iInclusiveGreater` for `Binary` at all - `System`'s catalog declares only `iEqual(Binary, Binary)`. So the compiler, asked for `<>` on two `Binary` values, finds the `MSSQLBinary` overload through that conversion and binds to it, and the statement then fails at run time with

```
Class alias "MSSQLDevice.MSSQLMSSQLBinaryNotEqualNode" not found.
```

`=` is unaffected, because `iEqual(Binary, Binary)` is an exact match. Everything else that compares two `Binary` values - `<>`, `<`, `>`, `<=`, `>=`, `?=`, and therefore ordering a table by such a column - takes the broken route.

## Reproduction

Verified 2026-11-24 against `modernization` at `b9d8ed93`. The engine's own coverage harness is one such server: its template registers the test helper library, which brings in the shared SQL-device library and not the SQL Server one. In that server,

```
begin
	var LB : Binary := Binary('ABEiM0RVZnc=');
	var LC : Binary := Binary('qrvM3Q==');
	if LB <> LC then
		raise Error('unreachable - the comparison never runs');
end;
```

fails with the class-alias error above rather than running. Written as `not(LB = LC)` it runs and answers correctly. That is how it was found: the new `StreamValues` coverage script had to be written with `not(A = B)` throughout, and says so at the site.

## Expected behaviour

A server with the shared SQL-device library registered and no SQL Server device answers `<>` on two `Binary` values, the same way it answers `=`. More generally: a library's registration script does not declare operators over classes that only another library's assembly provides, unless it requires that library.

## What the shapes of a fix cost

Each of these is a change an application could notice, which is why this is a triage decision and not a mechanical repair:

- **Move the type and its operators into `MSSQLDevice`.** The cleanest shape, and the one that matches where the code already is. It renames the type from `SQLDevice.MSSQLBinary` to `MSSQLDevice.MSSQLBinary`, which a D4 library or a catalog rebuild script that names it would notice, and it needs the SQL Server device's own catalog mapping to still find it.
- **Give the shared library its own implementations.** Keeps the type where it is and adds six or seven node classes (plus their `DAERegister` entries) to the shared assembly, with new class-alias strings in the registration script. Additive rather than renaming, but it duplicates code that already exists and leaves the SQL Server device's copies in place.
- **Make the shared library require the SQL Server one.** Smallest edit, worst shape: every SQL-device deployment would then load SQL Server support whether it uses it or not, and the SQL Server device already requires the shared library, so the requisite graph would have a cycle.
- **Declare the engine's own missing `Binary` comparisons.** This is a change to the system catalog (`src/engine/DAE/Schema/DataTypes.d4` / `SystemCatalog.d4`), which is a compatibility surface, and it decides by fiat what ordering two `Binary` values have - a question the language has so far left alone. It would also not remove the dangling class aliases, only stop them being reached.

## What happens if nothing is done

Nothing, for a deployment that registers the SQL Server device - which the shipped runtime layout provides, and which is the configuration the modernization's test suite and sample libraries use for SQL work. The configurations that break are a PostgreSQL-only server and one that registers the shared library for its SQL text or device-mapping operators alone. PostgreSQL support is not built yet (its C# project is not converted), so the first of those is not reachable today either; this becomes real when it is.

Worth noting for whoever triages it: the same `Register.d4` declares an `iNotEqual` for its own `SQLText` type over a `System.` class, which is fine, and keeps the parallel `SQLIText` block commented out. Only the `MSSQLBinary` block reaches into another library's assembly.
