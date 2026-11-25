description: A view can carry presentation settings such as the title a form shows, and so can the table it is built from. The view's own setting is supposed to win, but it is being quietly replaced by the one inherited from the table underneath, so some forms show the wrong title.
prereq: bug-altered-tags-rewrite-the-create-statement
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Schema.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/goldens/frontend/Security, docs/architecture/catalog-and-store.md, docs/migration-notes.md
repro: verified
difficulty: medium
----
## Owner decision (2026-11-23): the view's own tags always win

A view's own tags take precedence over every tag it inherits from its source. This is the rule. Write it down (where, below), add a `docs/migration-notes.md` entry, fix as described, regenerate the goldens and read every one of the 14 differences. That decision is settled; this ticket is the implementation.

Everything the plan stage was asked to resolve before this could be worked is resolved below: the 6 page-type changes are explained and are not a second defect, the other create statements that share a statement's metadata are split out into the prereq ticket, and the device arm that was appended to this ticket on 2026-11-25 turned out not to reproduce and is retired there.

## What goes wrong

A D4 object can carry *tags*: named string settings read by other parts of the system. The user interface layer reads the `Frontend.*` tags to decide what a derived form says and how it looks. A view carries its own tags, and it also inherits the tags of the table variable its expression is built from. Inheritance is meant to fill gaps only, and `CompileCreateViewStatement` merges the view's own tags back on top afterwards, which only makes sense as "the view's own tags win".

They do not win. In `libraries/Security/Documents/Security.d4:264` the view `SetUserDeviceUserPassword` carries `Frontend.Title = "Set Device User Password"`; the view it is built over inherits `Frontend.Title = "Device User"` from `Security.UserDeviceUser` (`Security.d4:182`). Every form derived from `SetUserDeviceUserPassword` is titled "Device User".

Measured directly on an in-process engine on 2026-11-25, with no library involved:

```
create table ProbeSource { ID : Integer, key { ID } } tags { Frontend.Title = "Source Title", Frontend.UseList = "true" };
create view ProbeView ProbeSource tags { Frontend.Title = "View Title", Frontend.UseList = "false" };
```

The view's `Frontend.Title` reads back as `"Source Title"` and its `Frontend.UseList` as `"true"` — both the source's values — and the create script the catalog emits for the view says `tags { Frontend.Title = "Source Title", Frontend.UseList = "true" }`. The view's own two tags are gone.

Present since the initial open-source commit; not a port consequence.

## Where it is, and why the merge-back cannot help

`src/engine/DAE/Compiling/Compiler.cs`, in `CompileCreateViewStatement`:

```csharp
4415:  node.View.MetaData = localStatement.MetaData;            // the view's tags ARE the statement's tags, not a copy
...
4463:      node.View.InheritMetaData(((TableNode)planNode).TableVar.MetaData);   // source is a table variable reference
4465:      node.View.MergeMetaData(((TableNode)planNode).TableVar.MetaData);     // source is any other table expression
4466:  node.View.MergeMetaData(localStatement.MetaData);        // let the view's own tags win
```

Line 4415 shares the statement's tag collection rather than copying it, so the view's collection and the statement's collection are one object. Line 4466 then merges that object into itself, which cannot restore anything: there is no separate record of the view's own tags left to restore from. Whatever lines 4463/4465 leave is final.

Both of those lines overwrite:

- `MergeMetaData` (line 4465) is `Tags.AddOrUpdateRange`, which replaces an existing tag of the same name outright.
- `InheritMetaData` (line 4463) replaces too. `Tags.Inherit(tag)` (`src/engine/DAE/Language.D4/MetaData.cs:277`) adds the tag when no tag of that name is present and otherwise assigns `_tags[index] = tag.Inherit()`, taking the source's value. **The doc comment on `Schema.Object.InheritMetaData` (`src/engine/DAE/Schema/Schema.cs:502`) says "if the metadata for this object does not already contain it", and that is false of the implementation.** Fix the comment in this ticket: say that an existing tag of the same name is replaced by the inherited value, and that a caller who wants its own values to survive merges them back afterwards — which is what this site does and what `CompileCreateScalarTypeStatement` does correctly (see below). Do not change `Tags.Inherit`: other callers depend on it, and changing it would reach far more than this defect.

Both branches are exercised by the two surfaces that change: `Security.Role` is `Roles { Name } adorn { … }`, which is not a table-variable reference and so takes the `MergeMetaData` branch; `Security.SetUserDeviceUserPassword` is a plain reference to `SetUserDeviceUserPasswordBase` and takes the `InheritMetaData` branch.

`CompileCreateScalarTypeStatement` in the same file is the shape this site should have had: it never assigns the statement's `MetaData` to the scalar type, it inherits from the parent types (`Compiler.cs:5571`), which builds a collection of its own, and then merges the statement's tags on top (`:5717`). The scalar type's own tags therefore do win.

## The fix

One line. At `Compiler.cs:4415`, assign a copy:

```csharp
node.View.MetaData = localStatement.MetaData == null ? null : localStatement.MetaData.Copy();
```

The prereq ticket makes exactly this change at the other eleven create-statement sites and leaves 4415 alone, so by the time this ticket runs the idiom and its reasoning are already in the file. The line number does not move: that ticket edits lines in place.

Measured 2026-11-25 with only this line changed (on top of an unmodified tree): the build is clean and `FrontendDerivationGoldenTest.EveryDerivedFormMatchesItsGolden` reports exactly **14** of its 416 cases no longer matching, which regenerate as 16 changed lines in 14 files and nothing else. The same 14 differ if the copy is made in the `Schema.Object.MetaData` setter instead, which is how the site was isolated; the setter is not the fix — the prereq ticket records why.

## The 14 golden differences, and why each is right

Regenerate with `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` and the diff should be exactly this. If it is not, stop and find out why before committing it.

**Eight titles, on `tests/goldens/frontend/Security/SetUserDeviceUserPassword.*.xml`** (all eight page types; the `Browse` and `List` files change twice, once in the `<interface text=…>` and once in a `<grid hint=…>`, which is the 16th line):

```
-<interface … text="Add Device User" …>
+<interface … text="Add Set Device User Password" …>
```

`Security.SetUserDeviceUserPassword` declares `Frontend.Title = "Set Device User Password"`; it was being titled with the `"Device User"` it inherits. This is the defect in the ticket's own words, and the new text is what the library asks for.

**Six detail page types, on `tests/goldens/frontend/Security/Role.{Browse,List,Add,Edit,View,Delete}.xml`**, one line each:

```
-<showformaction bop:name="Main.Name.Security.UserRole_Role" text="Role..." document=".Frontend.Derive('Security.UserRole', 'List',  'Main.Name', 'Main.Role_Name', true)" …
+<showformaction bop:name="Main.Name.Security.UserRole_Role" text="Role..." document=".Frontend.Derive('Security.UserRole', 'Browse','Main.Name', 'Main.Role_Name', true)" …
```

These follow from the view's own tags winning, and **not** from a second defect. The chain, confirmed by reading the code and by the diff being on exactly the six Role forms that carry this link:

- Each of those six forms carries a "Role..." link to the detail table `Security.UserRole`, through the reference `UserRole_Role`. (The `Report` and `OrderBrowse` pages build no such link, which is why it is six and not eight.)
- `DocumentBuilder.BuildShowFormAction` (`libraries/Frontend/Source/Production/DocumentBuilder.cs:161`–`178`) chooses `List` or `Browse` for that link's document from the reference's own `UseList` tag, falling back to the `UseList` tag of the reference's **source table** — which is `Security.UserRole`, not `Security.Role`.
- `Security.UserRole` declares `Frontend.UseList = "false"` (`libraries/Security/Documents/Security.d4:340`). The catalog table it is built over, `System.UserRoles`, declares `Frontend.UseList = "True"` (`src/engine/DAE/Schema/SystemCatalog.d4:1966`). So `Security.UserRole` is a second view losing its own tag to its source, and the symptom shows up on `Security.Role`'s forms rather than on its own.
- With the fix, `Security.UserRole`'s `"false"` wins and the link derives the `Browse` page, which is what the library asks for.

Say this in the commit message, one line per surface, as the owner's decision requires.

**Why only two surfaces out of 52.** A surface changes only where a view declares a tag that its source also carries, with a different value, *and* that tag reaches the derivation. Most views in the corpus either declare no tags (`Security.AddUserDeviceUser`, for instance, declares none, so it has nothing to lose) or declare tags their source does not carry. The two surfaces here are the two places in the shipped libraries where both halves are true.

## Edge cases & interactions

How each is verified is named. Only the one behaviour below earns a test; the rest are inspection or already-existing coverage.

- **Both inheritance branches.** The `InheritMetaData` and `MergeMetaData` branches at 4463/4465 must both end with the view's own tags on top. Verified by the goldens: the two changed surfaces take one branch each, as set out above.
- **Inherited-ness of the surviving tags.** After the fix a tag the view declares itself is a plain tag (`IsInherited` false, because `Tags.AddOrUpdate` constructs it that way) and a tag only the source has is inherited. Nothing in the engine branches on `IsInherited` for a view's own tags, but `D4TextEmitter` prints inherited tags the same as declared ones — which is why the stored create script for an affected view changes along with the forms. Verified by inspection plus `D4EmitterRoundTripTest` and `ParserRoundTripCorpusTest`.
- **The stored create script changes for affected views.** A catalog rebuilt from a library's scripts is unaffected (the library scripts are the source of truth and are not changed by this ticket), but an existing catalog's stored script for such a view will differ from one rebuilt after this change. That is a difference a deployment can notice and is what the migration-notes row is for. Verified by inspection and by `StoreRebuildWorkflowTest`.
- **A session view and a generated view.** The same function builds session-scoped views and the views the engine generates for an application transaction, and both paths read `localStatement.MetaData` for `DAE.GlobalObjectName` and `DAE.SourceTableName` *before* line 4415, so the copy cannot affect that resolution. Confirm by reading, and verify by the `SessionTables`, `SessionAndProcessScopedTables` and `ApplicationTransactions` coverage scripts and `ApplicationTransactionTest`.
- **The custom key-violation message.** `CompileCreateTableVarStatement` is called at `Compiler.cs:4459`, *before* the inherit and merge, and it builds a key constraint's `DAE.Message` from `tableVar.MetaData`'s `Frontend.Title` (`Compiler.cs:3887`). At that point the view's metadata is its own tags only, both before and after this change, so the message is unaffected — and that path was already "the view's own tags win". Verified by inspection and by the goldens (no message changes appear in the diff).
- **`Frontend.Elaborate` on a view whose source sets it.** Where a view declares `Frontend.Elaborate = "false"` and its source does not, nothing changes; where both declare it with different values, the view's value now wins and the form's whole reference structure can change. No such pair exists in the shipped libraries (which is why the diff is 14 lines and not hundreds), but an application can have one, and that is the single largest shape of change an application will see. Verified by inspection; named in the migration-notes row.
- **Derivation caching.** A session caches the document it derived for a seed; a view whose tags change needs the cache not to hand back a stale form. Nothing in this change touches the cache, and `FrontendDerivationGoldenTest.DerivingTheSameFormTwiceGivesTheSameText` already clears it between derivations. Verified by that existing case.
- **No change to the device arm.** The arm appended to this ticket on 2026-11-25 about `node.NewDevice.ClassDefinition` (`Compiler.cs:9381`) was re-measured and does not reproduce — `Schema.Device.ClassDefinition`'s setter already clones. The prereq ticket records the measurement and retires the arm. Nothing to do here; do not touch `Compiler.cs:9381` or the clone at `DDLNodes.cs:1138`.

## The test

One case, the direct reproduction above, at the layer that shows it without the whole 416-form corpus. The goldens already cover the end-to-end effect; what they do not do is state the rule.

New fixture `tests/DAE.Server.Tests/ViewTagPrecedenceTest.cs` over `InProcessTestFixture`, one `[Test]`: create a table with two tags, create a view over it declaring both tag names with different values, and assert that the view carries its own two values — both on the live catalog object and in the create script the catalog emits for it. Two tags rather than one so that the case also pins a tag whose value is read as a boolean (`Frontend.UseList`), which is the one the six Role forms turn on.

The fixture's summary should name the rule it pins — a view's own tags take precedence over the tags it inherits from its source — and point at the goldens as the end-to-end cover, so a later reader does not mistake it for a duplicate of them.

## Documentation

- **Architecture.** The prereq ticket adds a short subsection to `docs/architecture/catalog-and-store.md` on a catalog object owning its metadata and its class definition. Add one sentence to that same subsection for the rule this ticket settles: a view's own tags take precedence over every tag it inherits from its source, which is why a create-view statement's tags are merged back after inheritance — and that this works only because the view holds a copy. About 30 words. Run `python tools/docs-size-check/check_size.py` afterwards. If the prereq ticket's subsection is not there (it was dropped, or named differently), add the subsection as that ticket describes it and both sentences together. Nothing goes in the short root `docs/architecture.md`.
- **Migration notes.** One `done` row in the `## D4, catalog and libraries` section of `docs/migration-notes.md`: a view that declares a tag its source also carries now keeps its own value, where before the inherited value replaced it, so derived forms can change — the title a form shows, and, where the tag is one the derivation branches on such as `Frontend.UseList` or `Frontend.Elaborate`, the structure of the form. What to do: derive the application's forms and read the differences; a form that was relying on the inherited value needs the tag it actually wants written on the view. Mention that two of the shipped `Security` library's surfaces change, so the row is concrete.
- **No Known-defects row in the plan**, for the reason the prereq ticket gives. State it in the handoff.

## Things that cost time to find out

- `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists its sources explicitly; a new test file not added to it compiles into nothing and the test silently does not exist.
- An update run of the goldens **always fails**, deliberately, so it cannot be mistaken for a pass. Expect `wrote 416 golden file(s) and deleted 0`.
- The regeneration rewrites all 416 files; `git diff --stat tests/goldens/frontend` is how you see that only 14 actually changed. Read the whole diff, not the stat.
- `src/engine/DAE/Language.D4/MetaData.cs` has two implementations of `Tag` and `Tags`; `USESTRUCTTAG` is defined, so the **struct** version at lines 45–533 is live and the class version at 534–1412 is dead. The behaviour of `Tags.Inherit` described above is the struct one.
- `src/engine/DAE/Compiling/Compiler.cs` is CRLF with no byte-order mark and tab-indented; `sed` and `awk` in this environment strip carriage returns, so edit it with a tool that works in bytes.

## TODO

- Change `Compiler.cs:4415` to the null-guarded copy, matching the idiom the prereq ticket left at the other eleven sites.
- Correct the doc comment on `Schema.Object.InheritMetaData` (`src/engine/DAE/Schema/Schema.cs:502`) to say what `Tags.Inherit` actually does. Leave `Tags.Inherit` itself alone.
- Add `tests/DAE.Server.Tests/ViewTagPrecedenceTest.cs` with its one case, and its `<Compile Include>` entry in the test project. Confirm it goes red without the fix and green with it; say so in the handoff.
- Run `dotnet test tests/DAE.Server.Tests --filter Name=EveryDerivedFormMatchesItsGolden` and check the 14 reported differences are the 14 above, then regenerate with `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` and read the whole diff. It must be 16 changed lines in 14 files, all under `tests/goldens/frontend/Security/`. Any other file, or any other kind of change, is a stop-and-find-out.
- Write the commit message with one line per surface saying why the new form is right, as the owner's decision requires.
- `dotnet build Dataphor.slnx` — 0 errors, no new warning code.
- Targeted tests: the new case; `FrontendDerivationGoldenTest` (all four cases); `FormCustomizationTest` and `FormDocumentCorpusTest`, because they load and difference the same libraries' documents; `D4EmitterRoundTripTest` and `ParserRoundTripCorpusTest` for the emitted-script change; `ApplicationTransactionTest`; and the coverage scripts `Views`, `References`, `Tables`, `SessionTables`, `SessionAndProcessScopedTables`, `ApplicationTransactions`, `SecurityLibrary`. Name the filters used in the handoff.
- Add the architecture sentence and the migration-notes row; run `python tools/docs-size-check/check_size.py`.
