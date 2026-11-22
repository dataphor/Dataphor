description: When a script fills a table in one step from a calculation whose answer can change, the database engine runs that calculation twice and keeps the second set of answers, so the table ends up holding different values from the ones the script computed first - silently, with no error.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/TableNodes.cs, samples/Sample.Rental/Documents/
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: The behaviour is unchanged since 2015 and the trigger is narrow - a table assignment whose source expression is both non-deterministic and not a bare table literal - so a maintainer may reasonably hold it behind modernization work that has a known user waiting, especially as the fix is in the engine's assignment path where a wrong change is worse than the bug.
----
## What is wrong

In D4, `MyTable := <expression>` replaces a table variable's contents with the rows of an expression. The engine evaluates that expression **twice** whenever it is anything other than a bare `table { row …, row … }` literal, and the rows it stores are the second evaluation's. When the expression is *non-deterministic* - it calls `GetNextGenerator`, `Random`, `Now`, or any operator tagged `DAE.IsDeterministic = "false"` - the two evaluations produce different values, the first set is computed and thrown away, and the table ends up holding values the script never asked for. Nothing is reported.

The same expression written as `insert <expression> into MyTable` is evaluated once, which is the behaviour the assignment should have.

## Reproduction

Verified: run, observed. A server with only `SimpleDevice` registered; no SQL Server needed.

```
create device D class "SimpleDevice.SimpleDevice";
SetDefaultDeviceName('D');
EnsureGenerators();

create table R { ID : Integer, V : Integer, key { ID } };
R := (table { row { GetNextGenerator('R') ID, 1 V }, row { GetNextGenerator('R'), 2 } }) where V > 0;
```

`R` then holds `ID` **3 and 4**, and `Generators['R'].NextKey` is **5** - four calls to the generator for two rows. Expected: `ID` 1 and 2, and `NextKey` 3.

## What was measured, and what it rules out

Each line below is a separate table assignment of a two- or three-row literal whose `ID` column calls `GetNextGenerator`, measured on 2026-11-22 (Debug, development machine; the log is `tickets/.logs/test-samples-corpus.probe-doubling6.log` for as long as that directory's pruner keeps it). "doubles" means the generator was called twice per row and the stored ids are the second pass's.

| Source expression of the assignment | Target table | Result |
|---|---|---|
| bare `table { … }` | plain columns | **once** |
| bare `table { … }` | a column with a generator default | **once** |
| bare `table { … }` | a column constraint | **once** |
| bare `table { … }` | a non-key `order` | **once** |
| bare `table { … }` | a `reference` to another table | **once** |
| bare `table { … }` | a column whose type is a **user-defined scalar type** (`create type S like String`) | **doubles** |
| bare `table { … }`, two such columns | two user-defined scalar columns | **doubles** - still exactly twice, not three times |
| bare `table { … }` with the values already written as `S('a')`, so no conversion is needed | the same user-defined scalar column | **once** |
| `(table { … }) redefine { V := V + 1 }` | plain columns | **doubles** |
| `(table { … }) where V > 0` | plain columns | **doubles** |
| `((table { … }) where V > 0) where V > 0` | plain columns | **doubles** - still exactly twice, so the extra pass is not per operator level |
| `insert (table { … }) redefine { … } into T` | plain columns | **once** |
| `insert (table { … }) into T` with a user-defined scalar column | — | **once** |

Two conclusions follow. The user-defined scalar type is not itself the trigger: it is the **conversion** the compiler inserts when the source's row type does not match the target's, and that conversion is emitted as a `redefine` over the source (`Compiler.ConvertTableNode`). And the extra pass is a **single** extra evaluation at the assignment seam, not one per table operator and not one per column.

## Where to look

`Compiler.CompileAssignmentStatement` compiles the source with `CompileTypedExpression(plan, expression, targetNode.DataType, true)`, which inserts the conversion, then hands it to `EmitTableAssignmentNode` (both in `src/engine/DAE/Compiling/Compiler.cs`). That method builds

```
var <temp> := <source>;        // VariableNode over EnsureTableValueNode(source)
delete <target>;
insert <temp> into <target>;
```

so the source is meant to be materialized exactly once, into the temporary variable. `EnsureTableValueNode` wraps a `TableNode` in a `TableToTableValueNode`, whose `InternalExecute` (`src/engine/DAE/Runtime.Instructions/TableNodes.cs`) executes its source, opens it and copies it natively - also once, by reading. So the second evaluation is somewhere in how a *derived* table node (a restriction, a redefine) is opened and copied there, where a bare table selector is not; that difference is what the matrix above isolates and what the investigation should start from. Reading alone did not settle it, which is why this is filed rather than fixed.

## How long it has been like this

`EmitTableAssignmentNode` was last changed in `436b507e` ("Fixing issues related to binding change.", 2015-07-22), upstream and long before the modernization. So this is **not** a .NET 10 porting regression, and the project's "a failing assertion is a porting defect until shown otherwise" rule is satisfied by that: it is shown otherwise. Nothing is known about whether any earlier release behaved differently; there is no reference runtime to check against.

## Why it matters, concretely

`samples/Sample.Rental` is the one place in the repository that hits it. Its `Documents/MovieTable.d4` fills `Movie` with ten rows whose `ID` comes from `GetNextGenerator('Movie.ID')`, and `Movie.Name` has the user-defined type `StrLth50`, so the assignment carries a conversion and doubles: the stored ids are **11 to 20** instead of 1 to 10, and the generator is left at 21. `Documents/ActorTable.d4` does the same and gets **13 to 24**. `Documents/MovieActorTable.d4` then inserts reference data naming `Movie_ID` 1 to 10, which no longer exist, and fails with

```
The table Movie does not have a row with ID (1).
```

That takes out 7 of the 18 steps of `Documents/RegisterCompleted.d4`: `MovieActorTable` and `VideoTable` fail on the missing `Movie` row, and `RentalTables`, `AvailableVideoView`, `AddRentalOperator`, `NewRentalView` and `ReturnVideoOperator` then fail on the identifiers those two would have created (`Unknown identifier "Video"`, `"VideoDetail"`, `"RentedVideo"`). So a reader following the Rental tutorial cannot finish it. Measured the same day, in the same probe run; the log is `tickets/.logs/test-samples-corpus.probe-rental2.log`.

A customer application can hit it the same way: any catalog-rebuild script that fills a table in one statement from a generator, a random value or the clock, where the source expression is restricted, redefined, or simply has a column of the application's own scalar type. The values stored are not the ones the script's own later statements assume.

## What a fix owes

- The source expression of a table assignment is evaluated **exactly once**, whatever its shape. The reproduction above is the characterization test, at the lowest layer that shows it: a D4 script on the in-process engine harness, asserting both the stored ids and the generator's resulting `NextKey` (the ids alone would pass if the extra pass happened *before* the stored one).
- One test per behaviour, so one case for the general property - not one per row of the matrix above. The matrix is evidence for the diagnosis, not a test plan.
- `samples/Sample.Rental/Documents/` is **not** edited to work around it. Those scripts are the record of what correct behaviour looks like; a fix is confirmed by `Documents/RegisterCompleted.d4` running clean, after `SetLibrary('Sample.Rental')`, with all 18 steps succeeding and `Movie` holding ids 1 to 10.
- Nothing in `samples/` is the fix's deliverable, and nothing in WS3 waits on it: `tickets/implement/test-samples-corpus.md` records `Sample.Rental` as a sample that exposes no table variable after registration, which is true however this is resolved, because its `Documents/Register.d4` is a zero-byte file by design.
- A `docs/migration-notes.md` entry: an application whose script relies on the current ids, or on the generator values the first pass burns, sees different values after the fix.
