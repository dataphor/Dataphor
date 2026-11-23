description: One of the query operators in Dataphor's own language has two implementations - the one the engine actually uses, and an older one that it can no longer reach but still advertises to applications. Someone has to decide whether to retire the unreachable one or restore it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/RedefineNode.cs, src/engine/DAE/Runtime.Data/RedefineTable.cs, src/engine/DAE/Runtime.Instructions/RowNodes.cs, src/engine/DAE/Schema/SystemCatalog.d4, src/engine/DAE/Schema/SQLDeviceMappings.d4
tradeoffs: Nothing is broken and nobody is asking for this - the operator works, through the rewrite - so a maintainer may well park 446 lines of unreachable code as harmless; against that it is 446 lines that every future coverage pass, every future reader of the compiler and every future dead-code sweep has to re-diagnose, and the engine currently tells applications it has classes it cannot use.
----

## Owner decision (2026-11-22): retire

The owner chose **retire**. The rewrite (`add` / `remove` / `rename`) is the implementation of `redefine`. Delete `RedefineNode`, `RowRedefineNode` and `RedefineTable`, their `Compile` entries, and the dead `#if USEREDEFINENODE` branch in `Compiler.EmitRedefineNode`. Also remove the `iRedefine` declarations in `src/engine/DAE/Schema/SystemCatalog.d4` and the `iRedefine` mappings in `src/engine/DAE/Schema/SQLDeviceMappings.d4` that no longer have anything behind them. This decision is the explicit authorization the compatibility rule asks for to remove those registered D4 class names. Record the removal in `docs/migration-notes.md`: a catalog rebuilt from scripts no longer has the `iRedefine` operators, and an extension that named those classes must stop. No performance measurement is needed first. The planning step checks whether anything else (libraries, samples, tests, `DAERegister.GetClasses()`) names the classes or operators, and removes or updates those references in the same change.

## What was found

The D4 `redefine` operator - `T redefine { A := A + A }`, which replaces a column's value while keeping the column - has two implementations in the engine, and only one of them can run.

`Compiler.cs`, in `EmitRedefineNode`, puts the whole node-emitting implementation behind `#if USEREDEFINENODE`. **`USEREDEFINENODE` is defined nowhere in the repository** - the only occurrence of the symbol anywhere is that `#if` itself. The live `#else` branch rewrites the expression instead, as the comment above the method says:

```
// T redefine { A := A + A } ::=
//	T add { A + A Temp } remove { A } rename { Temp A }
```

So the compiler cannot produce a redefine node, and three implementations sit unreachable:

| Site | Coverable lines |
|---|---:|
| `src/engine/DAE/Runtime.Instructions/RedefineNode.cs` (whole file, `class RedefineNode`) | 257 |
| `class RowRedefineNode`, inside `src/engine/DAE/Runtime.Instructions/RowNodes.cs` | 124 |
| `src/engine/DAE/Runtime.Data/RedefineTable.cs` (whole file, used only by `RedefineNode`) | 65 |

All three measured **exactly zero hits** on the full three-part instrumented run of 2026-11-21 (`docs/coverage-report.md`), while `redefine` itself is executed by four coverage scripts that all pass - `Exists`, `RowLibrary`, `Cursors` and `DML`. That contrast is how this was found, and it is the proof: the operator works and its nodes are not what runs it.

**The disabled branch is also stale, not merely switched off.** Its body still uses the `A`-prefixed parameter names (`APlan`, `AStatement`, `AExpressions`) that the repository renamed away, so defining `USEREDEFINENODE` today would not compile. Whatever the symbol was for, nobody has maintained the code behind it for a long time.

## Why this is not a simple deletion

The three types are not merely public - they are **registered D4 class names**, which the project's compatibility rules protect:

- `src/engine/DAE/Schema/SystemCatalog.d4` declares `create operator iRedefine(const ARow : row) : row class "System.RowRedefineNode"` and `create operator iRedefine(ATable : table) : table class "System.RedefineNode"`.
- `src/engine/DAE/Schema/SQLDeviceMappings.d4` maps `iRedefine(table)` and `iRedefine(presentation)` to `SQLDevice.SQLRedefine` in eight places, so the SQL devices declare a translation for an operator the compiler never asks them to translate.

Removing the classes therefore means touching the system catalog script and the device mappings, which a catalog rebuilt from scripts will see, and which `docs/migration-notes.md` has to record. That is the reason this is its own ticket rather than another arm on the dead-code sweep `debt-ws12-hygiene-remainder`: the other arms there delete files nothing mentions.

## The decision to make

Two coherent answers, and this ticket exists so a person picks one rather than a coverage pass guessing:

- **Retire it.** The rewrite is the implementation and has been for years; delete the three implementations, their `Compile` entries, the dead `#if` branch in `EmitRedefineNode`, and whichever catalog and device-mapping declarations become meaningless - with a migration-notes entry, because a rebuilt catalog and any extension naming those classes will notice.
- **Keep it deliberately.** If the node implementation is wanted back - the rewrite produces a longer plan than a dedicated node would, which is a plausible reason someone wrote it - then say so at the site with a `NOTE:` giving the reason and the revisit condition, and the 446 lines stop being re-discovered every pass. Note that "keep" does not mean "leave as is": the branch does not compile, so it is not a switch anyone can flip.

Either answer closes the finding. Leaving it undecided is the only outcome that costs again next time.

## What to measure before deciding, if the plan question is open

Whether the rewrite costs anything is unmeasured and should not be asserted either way. `T redefine {A := ...}` becomes an add, a remove and a rename, which is three plan nodes where one would do, and `EmitRedefineNode` additionally emits a project over the clustering key when the node form reports `DistinctRequired`. If performance is the argument for keeping the node form, it needs a measurement on a real query, not the line count.
