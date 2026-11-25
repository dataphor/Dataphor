description: Dataphor's query language has an operator whose engine code exists twice over - the version that actually runs, and an older version the engine can no longer reach but still offers to applications. Delete the unreachable version, along with the catalog entries and the database translation that point at it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/RedefineNode.cs, src/engine/DAE/Runtime.Instructions/RowNodes.cs, src/engine/DAE/Runtime.Data/RedefineTable.cs, src/engine/DAE/Language.D4/Instructions.cs, src/engine/DAE/Schema/SystemCatalog.d4, src/engine/DAE/DAE.csproj, libraries/SQLDevice/Source/SQLOperators.cs, libraries/MSSQLDevice/Source/SystemCatalog.d4, libraries/PGSQLDevice/Source/SystemCatalog.d4, tests/DAE.Server.Tests/OperandOwnershipShapeTest.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/Cursors.d4, docs/architecture/operand-ownership.md, docs/coverage-report.md, docs/migration-notes.md
difficulty: medium
----

## The decision, already made

The owner decided on 2026-11-22 to **retire** the unreachable implementation. That decision is the explicit authorization the project's compatibility rule asks for before removing registered D4 class names, and it says no performance measurement is needed first. Nothing in this ticket is an open question; it is the list of sites and the reasons each one has to move.

## What runs today, and what does not

`redefine` is a D4 query operator that replaces a column's value while keeping the column: `T redefine { A := A + A }`. The compiler does not have a plan node for it. `Compiler.EmitRedefineNode` rewrites the expression instead, exactly as the comment above the method says:

```
// T redefine { A := A + A } ::=
//	T add { A + A Temp } remove { A } rename { Temp A }
```

Three classes implement the node form that the rewrite replaced, and none of them can be reached:

| Class | Where | Coverable lines |
|---|---|---:|
| `RedefineNode` | `src/engine/DAE/Runtime.Instructions/RedefineNode.cs`, whole file | 257 |
| `RowRedefineNode` | `src/engine/DAE/Runtime.Instructions/RowNodes.cs`, lines 350-540 | 124 |
| `RedefineTable` | `src/engine/DAE/Runtime.Data/RedefineTable.cs`, whole file (used only by `RedefineNode`) | 65 |

The compiler's only code that would build them sits inside `#if USEREDEFINENODE` in `EmitRedefineNode` (`src/engine/DAE/Compiling/Compiler.cs`, lines 15090-15101). `USEREDEFINENODE` is defined nowhere in the repository - that `#if` is the symbol's only occurrence - and the disabled text would not even compile if it were, because it still uses the `A`-prefixed parameter names (`APlan`, `AStatement`, `AExpressions`) that this repository renamed away. The three classes measured exactly zero hits on the instrumented run of 2026-11-21 (`docs/coverage-report.md`) while `redefine` itself ran in four passing coverage scripts - `Exists`, `RowLibrary`, `Cursors` and `DML`. That contrast is how the finding was made.

**The operators are not merely unreachable from the `redefine` syntax - they are unusable by any route.** Both classes are also declared as D4 operators (`iRedefine(row)`, `iRedefine(table)`), and D4 lets an application call a built-in operator by its `i`-name directly; the device test data in `tests/libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCase.d4` does exactly that with `iCompare` and `iEqual`. A direct `iRedefine(T)` call resolves to `RedefineNode`, whose `DetermineDataType` begins `_redefineColumnOffsets = new int[_expressions.Count];` - and `Expressions` is set only by the disabled branch, so it is null. Read from the code, that is a `NullReferenceException` at compile time; `RowRedefineNode.DetermineDataType` has the same shape. Verify this by running it before deleting anything (first TODO item below) and record what actually happens, because it is the sentence the migration note rests on: retiring these operators removes nothing that worked.

## Why the two device catalogs are not optional

`libraries/MSSQLDevice/Source/SystemCatalog.d4` (line 563) and `libraries/PGSQLDevice/Source/SystemCatalog.d4` (line 542) each map the system operator onto a SQL translation:

```
create operator System.iRedefine(table) class "SQLDevice.SQLRedefine" static tags {{ DAE.IsSystem = "true" }},
```

`Compiler.CompileDeviceOperatorMap` (`src/engine/DAE/Compiling/Compiler.cs`, line 9148) resolves the named operator against the catalog with `ResolveOperatorSpecifier` before it does anything else. Once `System.iRedefine(table)` is gone from the catalog, that line throws, the whole device operator-map script fails, and the device fails to register - which would break SQL Server and PostgreSQL devices outright. So removing the system operator and removing these two mappings is one change that cannot be split. The same applies to `SQLRedefine` itself (`libraries/SQLDevice/Source/SQLOperators.cs`, lines 390-444): it casts its argument with `(RedefineNode)planNode`, so the SQLDevice project stops compiling the moment `RedefineNode` is deleted.

Note for anyone comparing against the previous ticket text: it said the mappings were eight lines in `src/engine/DAE/Schema/SQLDeviceMappings.d4`, including an `iRedefine(presentation)` mapping. There is no such file in the live tree. The eight-site count came from `legacy/` - the parked SAS and Oracle devices, which this modernization does not build - and `presentation` only ever appeared there. The live sites are the two above.

## Everything the change touches

Source, in the engine:

- `src/engine/DAE/Runtime.Instructions/RedefineNode.cs` - delete the file.
- `src/engine/DAE/Runtime.Data/RedefineTable.cs` - delete the file.
- `src/engine/DAE/DAE.csproj` - remove the two `<Compile Include>` lines for those files (lines 143 and 187). `Directory.Build.props` sets `EnableDefaultItems=false`, so every file is listed by hand and a stale entry fails the build.
- `src/engine/DAE/Runtime.Instructions/RowNodes.cs` - delete `class RowRedefineNode`, currently lines 350-540, together with the blank separator line above it, leaving `RowExtendNode`'s closing brace followed by the `// operator Rename(row) : row` comment with the file's usual one-line gap.
- `src/engine/DAE/Compiling/Compiler.cs`, `EmitRedefineNode` - delete `#if USEREDEFINENODE` (line 15090) through `#else` (line 15101) and the matching `#endif` (line 15135), keeping the live rewrite as the method's only body. Leave the two `EmitRedefineNode` overloads and `CompileRedefineExpression` in place: they are the live path, and `ConvertTableNode`/`ConvertRowNode` (lines 11873 and 11901) call the first overload for implicit column conversions.
- `src/engine/DAE/Language.D4/Instructions.cs` - remove `public const string Redefine = "iRedefine";` (line 34). After the dead branch goes, nothing in C# reads it, and it is not inert: `Instructions.Contains` is populated by reflection over these constants and is what sets `IsBuiltin` on a newly created operator (`Compiler.cs` lines 6555, 6622, 7050). Leaving the constant would keep the engine answering that an operator named `iRedefine` is a built-in, which after this change is simply untrue. It is a `const`, so an already-compiled extension has the value baked in and notices nothing; only a recompile would.

Source, in the libraries:

- `libraries/SQLDevice/Source/SQLOperators.cs` - delete the `/* select A redefine … */` comment and `class SQLRedefine`, currently lines 389-444 counting the blank line above the comment. `DAERegister.GetClasses()` in `libraries/SQLDevice/Source/Register.cs` finds device operators by reflecting over the assembly's types, so there is no registration list to edit: deleting the class withdraws the `SQLDevice.SQLRedefine` class name by itself. The same is true of `src/engine/DAE/Register.cs` for the two engine nodes.
- `libraries/MSSQLDevice/Source/SystemCatalog.d4` - remove the `System.iRedefine(table)` mapping line (563). It is in the middle of a comma-separated list, so removing the whole line leaves the list well formed.
- `libraries/PGSQLDevice/Source/SystemCatalog.d4` - the same, line 542. This project is not converted yet and does not build, but the file is parsed by `ParserRoundTripCorpusTest` and is the device's live script when the port lands.

The system catalog:

- `src/engine/DAE/Schema/SystemCatalog.d4` - remove the two `create operator iRedefine(...)` declarations (lines 355 and 369) **and** the two `grant usage on iRedefine(...) to role User;` lines (2398 and 2399). The grants name the operators, so leaving them behind fails the catalog script.

Tests:

- `tests/DAE.Server.Tests/OperandOwnershipShapeTest.cs` - remove `"RowRedefineNode"` from `ReviewedNodes` (line 65). This fixture reflects over the engine assembly and asserts in both directions, so a name on the list that no longer exists fails the `stale` assertion. This is the one test that will go red if the change is incomplete, and it is why no new test is owed here.
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/Cursors.d4` - delete the single comment line `RedefineTable -> Navigable` (line 250), inside the block comment that lists cursor capabilities by table algorithm. That is a comment about a class that no longer exists; change nothing else in that file, and no assertion or expected value anywhere.

Leave `tests/libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCase.d4` (line 567) alone. Its row is named `System.iRedefine_table_` after the device operator, but the script it holds only uses the `redefine` language operator, which still works through the rewrite, and that file is test data rather than a coverage script the suite runs. Renaming a historical device case to match a removed mapping is not this ticket's business.

Documentation:

- `docs/migration-notes.md` - two entries, both `done`. Under **D4, catalog and libraries**: the `iRedefine(row)` and `iRedefine(table)` operators no longer exist, so a catalog rebuilt from scripts does not have them, and the SQL devices no longer declare a translation for them; the `redefine` syntax is unchanged and keeps working, and what to do is nothing unless a script called `iRedefine` by name, which could not have worked (say what the verification step actually observed). Under **Custom C# extensions**: the classes `RedefineNode`, `RowRedefineNode` and `RedefineTable`, the D4 class names `System.RedefineNode`, `System.RowRedefineNode` and `SQLDevice.SQLRedefine`, and the constant `Instructions.Redefine` are removed; an extension naming any of them must stop. Mention that an existing catalog store from before this change holds rows pointing at the removed class names and is not converted - rebuild from scripts, which is the direction the plan already settles.
- `docs/architecture/operand-ownership.md` - the last clause of line 34 reads "the row extend and redefine nodes are compiled from expressions, with no operand modifiers to honour". Make it the row extend node alone. Run `python tools/docs-size-check/check_size.py` afterwards; the edit only shrinks the page.
- `docs/coverage-report.md` - three places. Drop the `RedefineNode.cs` and `RedefineTable.cs` rows from the "eleven of 20 lines or more" table (lines 102 and 109) and fix the counts in the sentence above it, which currently says twenty-one files and 1,300 lines; rewrite the **"The largest of them is not untested, it is unreachable"** paragraph (line 116) in the past tense - the finding was investigated and the 446 lines were retired by this ticket, `redefine` still runs through the rewrite - and leave the `debt-unreachable-redefine-nodes` row in the review-tickets table (line 90) as the record of where the finding came from. Do not re-collect coverage; this is a text edit.

A tripwire, at the code site rather than in a ticket: add a `NOTE:` above `EmitRedefineNode` saying that the rewrite is now the only implementation, that a dedicated node form existed and was retired here, and that the rewrite spends three plan nodes where one would do - so if `redefine` plans ever show up as slow in a profile, a node form is the known alternative and this is where it went. No magnitude claim: nothing has been measured, and the owner's decision says none is needed.

## Edge cases & interactions

- **Device registration order.** The system operator and the two device mappings must go in the same commit, for the reason above. Verified by inspection, not by a test: no test in the suite executes either device's `SystemCatalog.d4`, because `SQLDevice.InternalRegister` runs that script only when the device starts, which needs a reachable DBMS - which is also why the three `Transaction.Atomicity.TestMSSQL*` scripts are recorded as known failures. `ParserRoundTripCorpusTest` does parse both files (it formats them as `String.Format` templates first), so a syntax slip is caught; a semantic one is not. Read the edited lines back before handing off.
- **Catalog object identifiers shift for a new store.** `SystemCatalog.d4` runs only on a store's first run (`Engine.RegisterCatalog`), and the identifier generator hands out numbers in script order, so removing two `create operator` lines renumbers every system object declared after them. A store created by this build is self-consistent; a store created before it keeps its own numbers and is loaded, not re-scripted. Nothing in the repository pins a system object's identifier - checked: no catalog store file is committed, `tests/goldens/` contains no operator identifiers, and `ObjectIDGeneratorTest` asserts only that the generator starts at one - so no golden or fixture moves. The pre-existing store that still names the removed classes is the migration-note case above.
- **The two conversion call sites.** `ConvertTableNode` and `ConvertRowNode` call `EmitRedefineNode` for implicit column conversions, not just the `redefine` syntax; those are the paths the comments in `RowNodes.cs` (line 329), `RowComparisonNodes.cs` (line 116) and `SystemNodes.cs` (line 934) describe. They go through the rewrite today and are unaffected, and those three comments stay accurate because `EmitRedefineNode` stays. Verified by the coverage scripts covering implicit conversions.
- **`EmitStatement` and stored view definitions.** Because the rewrite is already what runs, a view over `redefine` already emits `add`/`remove`/`rename` when the catalog serializes it, and `RowRedefineNode.EmitStatement` - the only code that emitted a `RedefineExpression` back out of a plan - was never reached. So no stored definition and no `.d4l` changes shape. Verified by inspection plus `D4EmitterRoundTripTest`, which works on parsed text and is untouched by this.
- **The `redefine` syntax itself stays.** `RedefineExpression`, the parser rule, and `D4Emitter.EmitRedefineExpression` all stay; the grammar and the user documentation under `docs/reference/` and `docs/developers-guide/` describe the language operator and need no change. Checked: `iRedefine` appears in no documentation page outside `docs/coverage-report.md`.
- **Mixed source hygiene.** Every file here is CRLF. `src/engine/DAE/Schema/SystemCatalog.d4`, `src/engine/DAE/DAE.csproj` and `tests/DAE.Server.Tests/OperandOwnershipShapeTest.cs` carry a UTF-8 byte-order mark; the rest do not. `SQLOperators.cs` mixes four-space indentation on its class braces with tabs inside the bodies. Preserve all of it, and reformat nothing.

## Tests

**No new test.** The project rule asks for a characterization test from a ticket that changes a subsystem's behaviour - and here the behaviour that must not change, `redefine`, is already covered by four passing coverage scripts (`Exists`, `RowLibrary`, `Cursors`, `DML`), which is precisely how the dead code was found. Nothing new becomes testable: after the change the operators do not exist, so a script naming them gets a compile error. `OperandOwnershipShapeTest` is the structural guard and already fails if the deletion is partial.

Targeted set for this stage, after `dotnet build Dataphor.slnx`:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~OperandOwnershipShapeTest|FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest|Name=Exists|Name=RowLibrary|Name=Cursors|Name=DML" --logger "console;verbosity=normal"
```

Name the filter in the handoff. Build Release as well as Debug if the Debug build is clean, since the `DefineConstants` differ between them and this edits conditional compilation - `dotnet build Dataphor.slnx -c Release` is cheap once Debug has compiled.

## TODO

- Before deleting anything, run `iRedefine` directly through the in-process engine harness - a one-off scratch run, nothing checked in - evaluating both `iRedefine(table { row { 1 A } })` and `iRedefine(row { 1 A })`, and record exactly what happens. This is the one factual claim the migration note rests on. If either call turns out to work, the owner's retire decision still stands; the migration note then has to say a working capability was removed, so write it that way rather than claiming it never worked.
- Delete `src/engine/DAE/Runtime.Instructions/RedefineNode.cs` and `src/engine/DAE/Runtime.Data/RedefineTable.cs`, and remove their two `<Compile Include>` lines from `src/engine/DAE/DAE.csproj`.
- Delete `class RowRedefineNode` from `src/engine/DAE/Runtime.Instructions/RowNodes.cs`.
- Collapse the `#if USEREDEFINENODE` conditional in `Compiler.EmitRedefineNode` down to the live rewrite, and add the `NOTE:` tripwire above the method.
- Remove `Instructions.Redefine` from `src/engine/DAE/Language.D4/Instructions.cs`.
- Remove the two `create operator iRedefine` declarations and the two `grant usage on iRedefine` lines from `src/engine/DAE/Schema/SystemCatalog.d4`.
- Delete `class SQLRedefine` and its comment from `libraries/SQLDevice/Source/SQLOperators.cs`.
- Remove the `System.iRedefine(table)` mapping line from `libraries/MSSQLDevice/Source/SystemCatalog.d4` and from `libraries/PGSQLDevice/Source/SystemCatalog.d4`.
- Remove `"RowRedefineNode"` from `ReviewedNodes` in `tests/DAE.Server.Tests/OperandOwnershipShapeTest.cs`, and the `RedefineTable -> Navigable` comment line from `tests/libraries/TestFramework.Coverage.Scripts/Documents/Cursors.d4`.
- Build Debug and Release, then run the targeted test set above.
- Write the two `docs/migration-notes.md` entries, correct the one clause in `docs/architecture/operand-ownership.md`, update the three places in `docs/coverage-report.md`, and run `python tools/docs-size-check/check_size.py`.
- In the handoff, state the measured line count removed, the filters run, and what the direct-`iRedefine` verification observed. Flag the device-mapping edits explicitly as verified by inspection only, since no test in the suite executes either device catalog.
