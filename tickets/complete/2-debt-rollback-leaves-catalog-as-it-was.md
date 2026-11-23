description: A new general test checks that cancelling a change to the database's own definitions leaves the running server exactly as it was, instead of that being verified one defect at a time; it found two more defects, both fixed here.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, docs/migration-notes.md
----
## What landed

A general guard over one family of defects, in `tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs` (864 lines, measured with `wc -l`) over `InProcessTestFixture`, with its `<Compile Include="…" />` entry in alphabetical position in the test project file. Each case takes a picture of the running server's in-memory catalog, runs one catalog-changing statement inside a transaction, requires the picture to have moved, rolls the transaction back, takes a third picture and requires it to equal the first. **97 cases plus three named tests, 100 tests, 14 seconds.**

Plus two engine fixes the guard found, and their migration-notes entries.

## The snapshot

Reads memory only - no `ScriptCatalog()`, for the two reasons the plan ticket gave (its object selection comes from the catalog store, and the state this family of defects leaves behind is mostly not in an object's D4 statement). Built as a `Dictionary<string, string>` of entry key to value, one entry per dimension per object, so a failure names the object and the dimension rather than a position in a very long string. Dimensions: `emit:` (the object's own statement through `D4TextEmitter`, `EmitMode.ForCopy`), `props:` (every readable public instance property of simple type, reflected, sorted), `children:` (a table's constraints over the whole row, and a type's constraints and default - the children the parent's own statement turns out not to carry), `owner:`, `linkusers:`, `deviceusers:`, `loadedlibrary:`, and the session's `sessionobjects` / `sessionoperators`. Every collection is sorted before comparison.

The difference report compares by key and reports entries gone, new, and changed. For a changed entry it diffs the *lines* of the value and prints only the lines that differ - printing both whole values truncated would have made the first real failure invisible.

## The two defects it found

**A created row constraint did not raise the table's validate summary.** Beside each table the engine keeps a one-bit summary of the definition - is there a default worth asking for, a check worth running, a change handler worth calling - derived from the definition and never stored. `alter table T { create constraint X … }` attaches a row constraint, which should raise the validate half of that summary, and nothing in the statement recomputed it; the summary stayed lowered until the next compilation over the table happened to repair it as a side effect. Found because the rollback *did* recompute, so the rollback left the server in a better state than it was in beforehand. Fixed at `AlterTableVarNode.CreateConstraints` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`) with one guarded call to `tableVar.DetermineRemotable(program.CatalogDeviceSession)` after the loop, exactly what the sibling `CreateColumns` already does. Nothing was ever stored wrong: the insert and the update validate a row constraint by a path that never consults this summary, and the proposable `Validate` path skips row constraints anyway. What was wrong is what the server reported about the table to a client, and the forward/undo disagreement itself.

**Altering a constraint threw `NullReferenceException`.** Found in review, by a case the matrix did not have. `alter table T { alter constraint X <expression> }` copied the old constraint's metadata unguarded, and a constraint written without tags has no metadata at all - which is how constraints are ordinarily written - so the statement crashed. The transition-constraint form of the same statement, and both forms on a view, crashed the same way, out of the same method. Fixed at the two sites in `AlterTableVarNode.AlterConstraints` with the guard the sibling `AlterColumnConstraints` already has. Reproduced in both directions: the matrix cases `AlterTableConstraint` and `AlterTransitionConstraint` fail with `NullReferenceException` without the guard and pass with it (verified by reverting the guard and re-running).

## The case matrix: 97 statements

Tables and views 23, types 17, operators 6, references and catalog constraints 5, devices 5, server links 7, event handlers 4, security 15, session objects 5, plus the two forms the plan ticket had listed as unrunnable and that turned out to run.

Each case runs its setup outside the transaction, and objects are named per case because the fixture shares one server and nothing drops them afterwards - a teardown that dropped them would itself change the catalog. A case whose statement fails to execute fails the case rather than passing quietly.

**Two statements the plan ticket listed as not runnable, now in the matrix.** `alter sort T using …` is a standalone statement, not an `alter type` item. `alter type T { create representation … }` works when the representation carries both accessors and a selector, as the shipped libraries write them.

**Four statements deliberately out, each with the reason in the fixture.** `alter table T { create column … }` and `{ drop column … }` are refused for a table in a memory device; a device scalar type map (`alter device D { create type Integer }`) is refused for the same kind of reason. All three need a device that supports them, and so a database server. The fourth, `alter view V { create constraint … }`, is out because its rollback genuinely does not restore the summary and is not meant to - see the review findings below.

Library DDL (`CreateLibrary`, `DropLibrary`, `SetDefaultDeviceName`, `SetLibraryDescriptor`) and the library-descriptor dimension are out by design, with the reason in the fixture and a pointer to `blocked/library-ddl-inside-a-transaction`.

## Tests, and what each verifies

| Test | What it verifies |
|---|---|
| `RolledBackStatementLeavesTheCatalogAsItWas` (97 cases) | For each kind of catalog-changing statement: the statement moves the snapshot, and a rollback then leaves the in-memory catalog byte-identical across all nine dimensions. This is the ticket's whole purpose - the general check that replaces finding this family of defects one at a time by reading code. |
| `InnerRollbackLeavesTheOuterChange` | A rollback undoes the changes of its own level only. |
| `SnapshotOfAnUntouchedServerIsStable` | Two snapshots with nothing in between are equal. A guard on the snapshot itself: a lazily computed or time-dependent property would make every case above flaky. It passes, which is the measurement behind excluding no property by name. |
| `CreatedTableConstraintRaisesTheValidateSummary` | The reproduction of the first engine fix, in the forward direction where the defect is. Asserted on the schema object rather than through a cursor deliberately - opening a cursor compiles an expression over the table, which repairs the summary, so a client-level assertion would pass either way. |

## What a passing run does not say

Stated in the fixture's own summary comment, so a reader of a green test knows the limits: library descriptors; **the fifteen cases the snapshot cannot see** (users, rights, the assignments between them and roles, a right's owner, and an operator's body - declared through `CaseTheSnapshotCannotSee`, so the list is exact, and ticketed); a child object's own derived state; the caches `InvalidateCachesAfterUndo` clears wholesale and the store's own rows (`CatalogStoreTest.NestedRollbackUndoesStatementWrites` covers those); the rolled-back `alter view V { create constraint … }`; and the forward direction, apart from the one named test above.

## Review findings

Read the implement diff (`ticket(implement): debt-rollback-leaves-catalog-as-it-was`) before the handoff, then probed the matrix for gaps by auditing it against the authoritative list of in-memory mutations - the 86 undo-instruction classes in `CatalogDeviceSession` - and adding a case for every statement family the matrix did not reach.

**Defects found and fixed inline (2).**

- **`alter constraint` on a table or a view threw `NullReferenceException`** whenever the constraint carried no tags, from `AlterTableVarNode.AlterConstraints`, in both the row-constraint and the transition-constraint branch. Reachable now from plain D4, and the statement was in practice unusable. Fixed with the guard the sibling site already has, under the project's "bugs met along the way" rule; reproduced by two new matrix cases and recorded in `docs/migration-notes.md`.
- **21 of the 97 matrix cases asserted nothing.** Measured by asserting, temporarily, that each statement moves the snapshot: 21 cases executed a real catalog change that no dimension of the snapshot could see, so the comparison after the rollback compared two identical pictures and passed whatever the rollback did. Among them was `DropTableConstraint` - the case that found the first engine fix, which went blind the moment that fix landed. Two things were done about it. A `children:` dimension was added for a table's constraints over the whole row and a type's constraints and default, measured to be absent from the parent's emitted statement (`create table` carries each column with its default and its constraints but not a row constraint; `create type` carries only the representations); that turned 6 of the 21 into real cases, and they are the ones this defect family is actually about. Then the non-vacuity check was made permanent: each case declares whether it is visible, a visible case must move the snapshot, and the remaining 15 are declared through `CaseTheSnapshotCannotSee` so a statement that quietly stops doing anything cannot hide among them.

**Filed (3).**

- `backlog/debt-snapshot-cannot-see-users-rights-and-operator-bodies` - the 15 remaining blind cases. Root site `CatalogRollbackSnapshotTest.Snapshot()`; the part that needs deciding is how the test assembly reaches the users cache and the rights cache, which the implement ticket deliberately did not widen access for.
- `backlog/debt-test-part-one-near-the-runner-limit` - part 1 of the suite took 8 m 43 s in implement and 8 m 40 s in review, two runs a day apart on the same machine against `AGENTS.md`'s own 8-minute split threshold and the runner's 10-minute idle limit. The implement handoff flagged one slow run and asked for a re-measure before acting; it repeated within three seconds, so it is growth and not noise. This fixture contributes about 4 of those seconds.
- An arm appended to `fix/bug-rolled-back-type-constraint-stops-the-column-check` - evidence for its own decision, not a new ticket: a recompute-on-each-step design *cannot* restore a view's summary, only raise it, because `Schema.DerivedTableVar.DetermineRemotable` recomputes without resetting. So the forward recompute this ticket added raises a flag on a view that the undo cannot lower, and a rolled-back `alter view V { create constraint … }` leaves `ShouldValidate` raised where it previously stayed lowered. Measured: `False` before, `True` after.

**Tripwires parked, not ticketed (1).** That same view asymmetry, at the two code sites a reader would meet it: a `NOTE:` beside the new `DetermineRemotable` call in `CreateConstraints`, and the fixture's own remarks explaining why there is no `alter view { create constraint }` case, so the next person does not add one and get a confusing failure. The consequence is benign - a raised flag costs a client one call that finds nothing to do, which `CatalogDeviceSession.RedetermineRemotable`'s own remarks already call not a defect - and the alternative would be to leave a view with the lowered flag that tables no longer have.

**Checked and found sound (no action).**

- **The engine fix's placement.** `CreateColumns` calls `DetermineRemotable` unconditionally at the same level; the new call is gated on the statement having created something, which is strictly narrower. Re-running it is safe on a view too: `create view` already calls `DetermineRemotable` last, after the metadata merge, so it is the canonical computation rather than something the compiler's inference would be clobbering.
- **Stale-*high* summaries on the forward path.** `alter table T { drop constraint X }` committed leaves the validate summary raised when X was the last row constraint; `AlterColumns` recomputes nothing at all. Both are covered by an existing accepted-tradeoff note (`RedetermineRemotable`: "A flag left raised costs a call that finds nothing to do, which is why only the lowered ones are defects") and by the open `fix/` ticket's own `NOTE:` at `AlterColumns`, whose stated revisit condition - a stale summary seen after a *committed* alter - has not tripped for that site. Not re-filed.
- **The difference report's truncation**, which the implement handoff asked a reviewer to confirm. Measured over the 1,254 objects that emit on a bare server: the longest single line of any emitted statement is 268 characters, against a 400-character limit, so no differing line is ever cut short. Almost every statement (1,160 of 1,254) emits multi-line and so goes through the line diff rather than the whole-value path. The measurement is now recorded at the constant.
- **The reflected `props:` dimension.** Still excludes nothing by name; `SnapshotOfAnUntouchedServerIsStable` passes, which is the evidence. The doc comment says how and where to exclude a property if one ever misbehaves.
- **Case naming and selection.** `TestCaseData.SetName(name)` gives each case a bare name, so `--filter Name=DropTableConstraint` selects one; names are unique within the fixture and a duplicate would be an NUnit error.
- **Session dimensions.** `SessionObjects` and `SessionOperators` both hold `Schema.SessionObject`, so the snapshot reads the name-to-global-name mapping for both, which is where the rolled-back session operator was left behind.
- **Source hygiene.** CRLF and the UTF-8 byte-order mark preserved in the fixture, CRLF and no mark in `DDLNodes.cs`, LF in `docs/migration-notes.md` - each as it was. The fixture is 864 lines, of which about 330 are the declarative case matrix and about 220 are documentation comments; the executable part is around 250 lines, so it was not split. Comments say why rather than narrating statements.

**Empty categories.** No finding was conditional-and-speculative beyond the one tripwire above; nothing was found that a `NOTE:` at a site already decided and that needed re-opening; and no test was cut, because none of the four restated the implementation or verified a mock - the three named tests each pin something the matrix cannot (nesting, the snapshot's own stability, the forward direction of the engine fix).

## Validation run

- `dotnet build Dataphor.slnx` - 0 errors, no new warnings. No separate lint step exists in this repository; the analyzers run in the build.
- The fixture alone - 100 passed, 0 failed, 14 s.
- Part 1 (`class != …CoverageScripts`) - 805 passed, 33 skipped, 0 failed, 8 m 40 s.
- Part 2 (coverage scripts A-R) - 67 passed, 11 skipped, 0 failed, 3 m 4 s.
- Part 3 (coverage scripts not A-R) - 50 passed, 12 skipped, 0 failed, 3 m 4 s.

No pre-existing failures surfaced, so no `tickets/.pre-existing-error.md` was written.
