description: A new general test checks that cancelling a change to the database's own definitions leaves the running server exactly as it was, instead of that being verified one defect at a time; it immediately found one more defect, which is fixed here.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, docs/migration-notes.md
difficulty: medium
----
## What landed

A new fixture, `tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs` (681 lines), over `InProcessTestFixture`, with its `<Compile Include="…" />` entry in alphabetical position in the test project file. It snapshots the running server's in-memory catalog, runs one catalog-changing statement inside a transaction, rolls the transaction back, snapshots again and requires the two snapshots to be equal — one case per kind of statement, 75 of them, plus three named tests.

Plus one engine fix the fixture found on its first run, and its migration-notes entry.

## The snapshot

Reads memory only — no `ScriptCatalog()`, for the two reasons the plan ticket gave (its object selection comes from the catalog store, and the state this family of defects leaves behind is mostly not in an object's D4 statement). Built as a `Dictionary<string, string>` of entry key to value, one entry per dimension per object, so a failure names the object and the dimension rather than a position in a very long string. Dimensions, all as specified: `emit:` (the object's own statement through `D4TextEmitter`, `EmitMode.ForCopy`), `props:` (every readable public instance property of simple type, reflected, sorted), `owner:`, `linkusers:`, `deviceusers:`, `loadedlibrary:`, and the session's `sessionobjects` / `sessionoperators`. Every collection is sorted before comparison.

The difference report compares by key and reports entries gone, new, and changed. For a changed entry it diffs the *lines* of the value and prints only the lines that differ — the first draft printed both whole values truncated, and the one real failure it found was invisible in that output because the differing property was past the truncation point. That change is why the failure below was diagnosable.

**Measured on this machine:** the whole fixture — 78 tests, two snapshots each, one server start — runs in 7 seconds. The snapshot's own size and cost were not re-measured here; the plan ticket's figures (4,235 entries, 1.39 MB, 21–86 ms per snapshot on this machine) are what the 7 seconds is consistent with. No sampling and no restriction to non-system objects was needed, as the plan ticket predicted.

## The defect it found, and the fix

The one case that failed was `DropTableConstraint`, and the difference it reported was `ShouldValidate=False` before the transaction and `True` after the rollback. So the rollback left the server in a *better* state than it was in beforehand: the undo recomputed a derived flag that the statement which set up the test had left stale.

Beside each table the engine keeps a one-bit summary of the definition — is there a default worth asking for, a check worth running, a change handler worth calling — derived from the definition and never stored. `alter table T { create constraint X … }` attaches a row constraint, which should raise the validate half of that summary, and nothing in the statement recomputed it. The summary stayed lowered until the next compilation over the table happened to repair it as a side effect.

Fixed at the site, `AlterTableVarNode.CreateConstraints` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1293`): one guarded call to `tableVar.DetermineRemotable(program.CatalogDeviceSession)` after the loop, exactly what the sibling `CreateColumns` already does and what the site's own existing `NOTE:` in `AlterColumns` instructed for this case ("if a stale summary is ever seen after a *committed* alter, call table.DetermineRemotable here, as CreateColumns does"). That `NOTE:` now also records that the sibling site has been fixed. A `NOTE:` at the new call records that it is one more per-statement recompute of a summary that `fix/bug-rolled-back-type-constraint-stops-the-column-check` proposes to retire outright, and goes with the rest of them if that lands.

**What the stale flag cost, honestly:** less than the failure message suggests. The insert and the update validate a row constraint by a path that never consults this summary (`TableNode.BeforeInsert` calls `PreparedValidate` unconditionally), and the proposable `Validate` path skips row constraints anyway (`isProposable` gates `ValidateImmediateConstraints`). So nothing was ever stored wrong and no check was lost; what was wrong is what the server reported about the table to a client, and the forward/undo disagreement itself. The already-open `fix/` ticket above had judged forward-path staleness "harmless today" for exactly this reason. It was fixed rather than filed because the fix is one line at a site whose sibling already does it, under the project's "bugs met along the way" rule — a reviewer who disagrees with that call should say so, because the alternative (dropping the case from the matrix) would blind one row of the matrix permanently.

## Tests, and what each verifies

| Test | What it verifies |
|---|---|
| `RolledBackStatementLeavesTheCatalogAsItWas` (75 cases) | For each kind of catalog-changing statement, a rollback leaves the in-memory catalog byte-identical across all eight snapshot dimensions. This is the ticket's whole purpose: the general check that replaces finding this family of defects one at a time by reading code. |
| `InnerRollbackLeavesTheOuterChange` | A rollback undoes the changes of its own level only — the inner rollback leaves the catalog as the outer change left it, the outer rollback as it was before either. |
| `SnapshotOfAnUntouchedServerIsStable` | Two snapshots with nothing in between are equal. A guard on the snapshot itself: a lazily computed or time-dependent property would make every case above flaky. It passes, which is the measurement behind excluding no property by name. |
| `CreatedTableConstraintRaisesTheValidateSummary` | The reproduction of the engine fix above, in the forward direction where the defect is: after a committed `alter table { create constraint }` the live `Schema.TableVar` reports `ShouldValidate`. Asserted on the schema object rather than through a cursor deliberately — opening a cursor compiles an expression over the table, which repairs the summary, so a client-level assertion would pass either way. |

No other test was added. The matrix case `DropTableConstraint` is a second, indirect reproduction of the engine fix (it is how the defect surfaced), but it would stop reproducing if the undo-side recompute were also removed, which is why the direct test exists.

## The case matrix: 75 statements

Tables and views 18, types 14, operators 4, references and catalog constraints 4, devices 5, server links 4, event handlers 4, security 15, session objects 5, plus the two forms the plan ticket had listed as unrunnable and that turned out to run (below).

Each case runs its setup outside the transaction, and objects are named per case because the fixture shares one server and nothing drops them afterwards — a teardown that dropped them would itself change the catalog. A case whose statement fails to execute fails the case rather than passing quietly on two trivially equal snapshots.

**Two statements the plan ticket listed as not runnable, now in the matrix.** `alter sort T using …` is a standalone statement, not an `alter type` item — the attempted form was the reason it read as a syntax error. `alter type T { create representation … }` works when the representation carries both accessors and a selector, as the shipped libraries write them: `create representation Doubled { Doubled : Integer read value.Value * 2 write RepresentationSnap(Doubled div 2) } selector RepresentationSnap(Doubled div 2)`.

**Three statements still out, with the reason recorded in the fixture so they are not retried blind.** `alter table T { create column … }` and `{ drop column … }` are refused for a table in a memory device ("Columns in a memory device cannot be created in this release"). A device scalar type map, `alter device D { create type Integer }`, is refused for the same kind of reason — "Unable to determine a default scalar type map for scalar type System.Integer", because a memory device maps no types. All three need a device that supports them, and so a database server; they are not reachable from a bare in-process fixture.

Library DDL (`CreateLibrary`, `DropLibrary`, `SetDefaultDeviceName`, `SetLibraryDescriptor`) and the library-descriptor dimension are out by design, and the fixture's summary comment says so and points at `blocked/library-ddl-inside-a-transaction`.

## What a passing run does not say

Stated in the fixture's own summary comment, so a reader of a green test knows the limits: library descriptors; the cached user objects and the cached answers to rights checks (not reachable from the test assembly, and access was deliberately not widened); a child object's own derived state (columns, keys and representations are compared only through the parent's emitted statement and the parent's own properties); the caches `InvalidateCachesAfterUndo` clears wholesale and the store's own rows (`CatalogStoreTest.NestedRollbackUndoesStatementWrites` covers those); and the forward direction, apart from the one named test above.

## Validation run

- `dotnet build Dataphor.slnx` — 0 errors, no new warnings.
- The new fixture alone — 78 passed, 0 failed, 7 s.
- Part 1 (`class != …CoverageScripts`) — 783 passed, 33 skipped, 0 failed, **8 m 43 s**.
- Part 2 (coverage scripts A–R) — 67 passed, 11 skipped, 0 failed, 3 m 3 s.
- Part 3 (coverage scripts not A–R) — 50 passed, 12 skipped, 0 failed, 3 m 3 s.

No pre-existing failures surfaced, so no `tickets/.pre-existing-error.md` was written.

**Worth a reviewer's attention:** part 1 at 8 m 43 s is above the worst run recorded in the root `AGENTS.md` (7 m 32 s), and that file's own guidance is to split a part further once it passes about 8 minutes — the runner's idle limit is 10. This fixture contributes 7 s of it, so it is not the cause, and `AGENTS.md` also says to treat a single slow run as noise until it repeats. One sample is not grounds for re-splitting the parts; a second slow run would be. Re-measure before acting.

## What to look at in review

- **The engine fix.** One line plus comments, at `DDLNodes.cs:1293`. The judgement to fix rather than file is argued above; the counter-argument is that `fix/bug-rolled-back-type-constraint-stops-the-column-check` may retire this whole mechanism and this adds to what it would remove.
- **The reflected `props:` dimension.** It is what makes the fixture sensitive, and it is also the part most likely to need an exclusion later. Nothing is excluded today and the stability test is the guard; the doc comment says how and where to exclude a property if one ever misbehaves.
- **The difference report.** Line-level diffing of a changed entry is what made the one real failure readable. Worth confirming the output is still useful for an entry whose value is a single long emitted statement (that case prints both values truncated at 400 characters).
- **Case naming.** `TestCaseData.SetName(name)` gives each case a bare name (`DropTableConstraint`), so `--filter Name=DropTableConstraint` selects one. Names are unique within the fixture; a duplicate would be an NUnit error, not a silent collision.
