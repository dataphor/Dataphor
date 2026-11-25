description: A view can carry presentation settings such as the title a form shows, and so can the table it is built from. The view's own setting was being quietly replaced by the one inherited from the table underneath; it now wins, two of the shipped Security library's form surfaces change as a result, and the review pass carried the same rule into the one place inside the create statement that was still reading the inherited value.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Language.D4/MetaData.cs, tests/DAE.Server.Tests/ViewTagPrecedenceTest.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs, tests/goldens/frontend/Security, docs/architecture/catalog-and-store.md, docs/migration-notes.md
repro: verified
----
## What landed

The rule the owner settled on 2026-11-23 — **a view's own tags always win over every tag it inherits from its source** — is what the engine does, is written down, and is pinned by two tests.

**The implement stage** (`ticket(implement): bug-view-tags-lose-to-tags-inherited-from-the-source`) made the view own its metadata instead of sharing the create statement's collection, in `CompileCreateViewStatement`:

```csharp
node.View.MetaData = localStatement.MetaData == null ? null : localStatement.MetaData.Copy();
```

Sharing one collection meant the `MergeMetaData(localStatement.MetaData)` at the end of the block — the merge that is meant to put the view's own tags back on top of what it inherited — merged the collection into itself and restored nothing. The copy gives that merge something to merge back from. The implement stage also corrected the doc comment on `Schema.Object.InheritMetaData`, which claimed inheritance fills gaps when the implementation replaces. The two shipped `Security` surfaces that change are the 16 regenerated golden lines in `tests/goldens/frontend/Security/`, and the migration-notes row explains them.

**The review stage** found that the rule stopped at the *end* of the create statement, and added the one line that carries it through the middle of it, plus its reproduction. Details under *Review findings*.

## The per-surface justification for the golden change

- **`Security/SetUserDeviceUserPassword.*.xml`, eight page types, nine lines.** `Security.SetUserDeviceUserPassword` declares `Frontend.Title = "Set Device User Password"` (`libraries/Security/Documents/Security.d4:264`) and was being titled with the `"Device User"` it inherits, through `SetUserDeviceUserPasswordBase`, from `Security.UserDeviceUser` (`:182`). Its source expression is a plain table-variable reference, so this surface exercises the `InheritMetaData` branch.
- **`Security/Role.{Browse,List,Add,Edit,View,Delete}.xml`, one line each.** The "Role..." detail link now derives the `Browse` page instead of `List`. `DocumentBuilder.BuildShowFormAction` (`libraries/Frontend/Source/Production/DocumentBuilder.cs:161`–`178`) takes that link's page type from the `Frontend.UseList` tag of the reference's **source table**, which is `Security.UserRole`: a second view losing its own tag, whose symptom shows up on `Security.Role`'s forms. `Security.UserRole` declares `Frontend.UseList = "false"` (`Security.d4:340`) over `System.UserRoles`, which declares `Frontend.UseList = "True"` (`src/engine/DAE/Schema/SystemCatalog.d4:1966`); with the fix the view's `"false"` wins. `Security.UserRole` is an `adorn`, not a table-variable reference, so this surface exercises the `MergeMetaData` branch. The `Report` and `OrderBrowse` pages build no such link, which is why it is six forms and not eight.

Both inheritance branches are therefore covered by the regenerated goldens, one surface each.

## The tests

| Test | What it verifies |
|---|---|
| `tests/DAE.Server.Tests/ViewTagPrecedenceTest.TestViewTagsWinOverTheTagsInheritedFromItsSource` | The rule: a table declaring `Frontend.Title` and `Frontend.UseList`, a view over it declaring both with different values, and the view reads back as its own values — on the live `Schema.DerivedTableVar` and in the create script the catalog emits for it. Two tags, so the case also pins a tag whose value is read as a boolean, which is the one the six `Role` forms turn on. |
| `tests/DAE.Server.Tests/ViewTagPrecedenceTest.TestTheKeyViolationMessageUsesTheViewsOwnTitle` | Added by the review. The rule *inside* the create statement: the message a violation of a key the view declares itself reports is built from the view's own title, not the inherited one. |

`FrontendDerivationGoldenTest.EveryDerivedFormMatchesItsGolden` is the end-to-end cover; 416 golden files across eight libraries regenerate and only the 14 Security files above change.

## Review findings

**Read first, summary second.** The implement diff was read in full — the compiler change, both doc comments, both test files, all 16 golden lines, both doc edits — before the handoff summary.

### Fixed in this pass

**1. The rule stopped at the end of the create statement (verified; engine behaviour changed).** `CompileCreateViewStatement` inherits the source's tags twice: `CopyTableVar` (`src/engine/DAE/Schema/TableVar.cs:2623`) does it, and the block repeats it afterwards; the merge-back that restores the view's own tags runs only after the second. `CompileCreateTableVarStatement` runs *between* them, and for a key the view declares itself it builds the message a key violation reports from the view's `Frontend.Title` (`Compiler.cs:3893`, reached through `CompileTableVarKeyConstraints`) — reading the inherited title, because the merge-back has not run yet. Measured on 2026-11-25 against an in-process server: `create table S { ID : Integer, Name : String, key { ID } } tags { Frontend.Title = "Source Title" }` with `create view V S { key { Name } } tags { Frontend.Title = "View Title" }` gave the view the title `View Title` and the key's message `'The table Source Title already has a row with Name …'`.

This was already true before the implement fix, but the fix made it *inconsistent*: the form title now says one thing and the error message another. The fix is one line, immediately after `CopyTableVar`, with a comment saying why the merge appears twice:

```csharp
node.View.MergeMetaData(localStatement.MetaData);
```

The final state is unchanged — the later inherit and merge-back still run and still end where they ended — so this changes nothing except what the statement's own sub-definitions see while they compile. Confirmed red before and green after. No shipped library declares a key in a create-view statement (the three views in `samples/Sample.Shipping` that declare one declare it inside the `adorn`, which makes it inherited and compiles no constraint for the view), so no golden and no stored script changed. One sentence was added to the existing migration-notes row and one to the `catalog-and-store.md` paragraph. **If the owner would rather weigh a wording change to a generated error message separately, this is the line to revert, together with its test and those two sentences.**

The same window does *not* reach the reference custom messages (`Compiler.cs:7501`, `:7527`): `CompileSourceReferenceConstraint` and `CompileTargetReferenceConstraint` are compiled only when both the source and the target are a `Schema.BaseTableVar` (`:8762`), which a view never is. The create-scalar-type site named in the new `InheritMetaData` doc comment has no equivalent window either: its merge-back sits above the sub-definitions, and nothing it compiles reads the type's tags to build a message.

**2. `Tags.Inherit` carried the same false claim the implement stage corrected one layer up.** `src/engine/DAE/Language.D4/MetaData.cs:271` said the existing tag's value "is set based on the value of the given tag" and that this "will cause the IsInherited property to be set to false"; the live implementation (`USESTRUCTTAG` is defined) assigns `_tags[index] = tag.Inherit()`, which replaces the tag with an inherited copy. The summary and remark now say that, and point at `Schema.Object.InheritMetaData` for the merge-back. Behaviour untouched. The duplicate `Inherit` further down that file is inside the `#else` of `USESTRUCTTAG` and is not compiled, so it was left alone.

**3. `DeviceClassAttributeFixture`'s remark had gone stale.** It enumerates the fixtures that derive from it for `CatalogObject` and `Script` alone, to count down to moving those two helpers to `InProcessTestFixture`; it did not list the new fixture, and it named `SchemaCacheInvalidationTest`, which reads `MaxRowCount` too and so is a class-attribute fixture like the rest. The list is now `CreateStatementTagRerunTest` and `ViewTagPrecedenceTest`, and the trigger reads "a third fixture" against that accurate count — so the move is still not due.

### Corrections to the handoff's prose, with no code change

- The handoff said the key-violation message "runs *before* the inherit and merge, where the view's metadata is its own tags only — unchanged by this ticket, and already 'the view's own tags win'". It does not: `CopyTableVar`, a few lines above, has already inherited. That is finding 1.
- The handoff attributed the `MergeMetaData` branch to `Security.Role` being an `adorn`. The view whose tag is restored for those six forms is `Security.UserRole`, which is also an `adorn` — so the branch is covered either way, but the attribution above is the accurate one.

### Checked and found sound

- **The copy cannot lose or alias anything.** `Tag` is a struct under `USESTRUCTTAG`, so no tag instance is shared between the statement and the view. Every read of `localStatement.MetaData` that resolves a name — `DAE.GlobalObjectName` for a session view, `DAE.SourceTableName` for a generated one — is above the copy, and `Plan.GetObjectID` has already consumed `DAE.ObjectID` out of the statement above it, exactly as at the other eleven create sites.
- **`IsInherited` on a view's own tags is the same before and after the fix**, which is why `alter view … create tags` is unaffected: the merge-back's `AddOrUpdate` writes `new Tag(name, value, false, isStatic)`, so the tag ends non-inherited either way — before the fix with the inherited value, now with the view's own. Nothing in the engine branches on `IsInherited` for a view's own tags, and `D4TextEmitter` prints an inherited and a declared tag alike, which is why an affected view's stored create script changes along with its live object.
- **The goldens.** All 16 changed lines read; both surfaces traced to the library sources and to the frontend code that reads the tag. 416 files across eight libraries (`Frontend`, `Sample.*`, `Security`, `System.Internet`) regenerate and only the 14 Security files change.

### Noticed and deliberately left

- **The inherit is still written twice.** `Compiler.cs:4481`–`4484` repeats exactly what `CopyTableVar` already did, with nothing in between that touches either side's metadata, so removing it would be behaviour-neutral. It was left in place: deleting lines from the create-view path for clarity alone is not worth the risk on a port whose behaviour is the specification, and the new comment at the site now tells a reader why the merge appears twice. Not filed — there is no defect behind it, and no condition attached to it either, so it is not a tripwire.
- **`Frontend.Elaborate` is still argued rather than exercised.** Where a view and its source both declare it with different values, the view's value now wins and a form's whole reference structure can change. No such pair exists in the shipped libraries, so nothing in the suite covers it, and the migration-notes row names it as the shape of change to look for. No ticket: the engine does not branch on a tag's name, so the rule the two tests pin is the whole of the engine-side behaviour, and what `Frontend.Elaborate` additionally exercises is derivation logic this change does not touch. Covering it would mean inventing a library for the frontend corpus, which is a test-corpus decision rather than a defect.
- **No session-view assertion of its own.** Both reads that matter are above the copy, and the `SessionTables`, `SessionAndProcessScopedTables` and `ApplicationTransactions` scripts and `ApplicationTransactionTest` are green, but no assertion pins a session view's tags.

### Filed

**Nothing.** The one finding that would otherwise have been a ticket was fixed inline instead, being one local line with its reproduction; the two doc-comment findings were fixed inline; and nothing else reached the filing bar. No tripwire was recorded either — no concern here has the "fine now, work later if X happens" shape.

## Validation

`dotnet build Dataphor.slnx` — **0 errors**, no new warning code (the warnings are the pre-existing `CA1416`/`SYSLIB0051`/`CA2022`/`CS0618`/`CS0108` set). Then, per the project's per-stage rule, the implement stage's targeted set plus a ring of neighbours, each as its own `dotnet test tests/DAE.Server.Tests --no-build` run:

| Filter | Result |
|---|---|
| `FullyQualifiedName~ViewTagPrecedenceTest` | 2 passed; the new case fails with `Expected: String containing "View Title" / But was: "'The table Source Title already has a row with Name …'"` when the one-line fix is commented out |
| `FullyQualifiedName~` each of `FrontendDerivationGoldenTest`, `FormCustomizationTest`, `FormDocumentCorpusTest`, `D4EmitterRoundTripTest`, `ParserRoundTripCorpusTest`, `ViewTagPrecedenceTest` | 535 tests: 516 passed, 19 skipped (the parked corpus documents), 0 failed |
| `FullyQualifiedName~ApplicationTransactionTest` plus the coverage scripts `Views`, `References`, `Tables`, `SessionTables`, `SessionAndProcessScopedTables`, `ApplicationTransactions`, `SecurityLibrary`, `Constraints`, `CreateTableFrom`, `DeferredConstraintChecking`, and the create/alter/drop scripts for views, session views, keys, table constraints, transition constraints, catalog constraints and references | 32 passed |
| `FullyQualifiedName~` each of `StoreRebuildWorkflowTest`, `CreateStatementTagRerunTest`, `SchemaCacheInvalidationTest`, `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `PreparedPlanRecompileTest` | 24 passed |

(The filters were single `--filter` expressions with the clauses joined by the alternation character, spelled out in prose here because a table cell cannot carry it.)

The key, constraint and reference scripts are the ring the review added over the implement stage's set, because the fix moves when the compiler reads a view's tags while compiling the statement's own keys, constraints and references.

`python tools/docs-size-check/check_size.py` — 13 files checked, 0 over the limit. (`tools/docs-link-check/check_links.py` reports 203 missing-target errors across `docs/`, all of them in reference pages this ticket never touched and none in the two files it edits; that checker is not wired into CI yet, per the note in `.github/workflows/docs.yml`.)

**The three suite parts were not run.** This is a compiler change, which is one of the project rule's triggers — but the trigger is a lexer, parser or compiler change "that every script goes through", and this is two lines on the create-view path only. The coverage scripts that do go through it, plus the whole frontend, parser-round-trip and emitter-round-trip corpora, are in the runs above; CI runs the full suite in Debug and Release on the push.

No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.
