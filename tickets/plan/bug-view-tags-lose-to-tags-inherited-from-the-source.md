description: A view can carry presentation settings such as the title a form shows, and so can the table it is built from. The view's own setting is supposed to win, but it is being quietly replaced by the one inherited from the table underneath, so some forms show the wrong title.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, tests/DAE.Server.Tests/FrontendDerivationGoldenTest.cs, tests/goldens/frontend
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: The fix changes what 14 of the 416 derived forms say, which means rewriting checked-in expectation files that are the only record of this engine's form output — a maintainer may prefer today's wrong-but-known titles to a change every application's users would see at once.
----
## Owner decision (2026-11-23): the view's own tags always win

A view's own tags take precedence over every tag it inherits from its source. This is the rule; write it down in `docs/architecture/catalog-and-store.md`, which describes schema objects and their metadata (a better page may exist; pick it if you find one) and add a `docs/migration-notes.md` entry: views that carry a `Frontend.*` (or any) tag their source also carries now honour their own value, so derived forms can change.

Fix as the ticket describes (copy, not share, at `Compiler.cs:4394`), add the direct case (a view with a tag its source also has; the view's value is the one derived), regenerate the goldens and read every one of the 14 differences. The 8 title changes are expected. The 6 `Security.Role` page-type changes must be explained: if they follow from the view's own tags now winning, say why in the commit; if they point at a second defect, file it rather than accepting the goldens blind. Also do the check the ticket lists for the other create statements that share the statement's metadata.

## What goes wrong

A D4 object can carry *tags* — named settings read by other parts of the system; the user interface layer reads `Frontend.*` tags to decide what a derived form says and how it looks. A view carries its own tags, and it also inherits the tags of the table variable its expression is built from. Inheritance is supposed to fill gaps only: `Schema.Object.InheritMetaData` is documented as referencing each tag "if the metadata for this object does not already contain it", and `CompileCreateViewStatement` merges the view's own tags back on top afterwards, which only makes sense as "the view's own tags win".

They do not win. In `libraries/Security/Documents/Security.d4` the view `SetUserDeviceUserPassword` carries `Frontend.Title = "Set Device User Password"`, and the table variable it is built over carries `Frontend.Title = "Device User"`. Every form derived from the view is titled "Device User".

## Where it is

`src/engine/DAE/Compiling/Compiler.cs:4394`, in `CompileCreateViewStatement`:

```csharp
node.View.MetaData = localStatement.MetaData;        // 4394: the view's tags ARE the statement's tags, not a copy
...
node.View.InheritMetaData(sourceTableVar.MetaData);   // 4442: fill gaps from the source
node.View.MergeMetaData(localStatement.MetaData);     // 4445: let the view's own tags win
```

Because line 4394 shares the statement's metadata object rather than copying it, the view's metadata and the statement's metadata are the same object, and line 4445 merges that object into itself — it cannot restore anything, because there is no separate copy of the view's own tags left to restore from. Whatever line 4442 leaves is final.

Measured on 2026-11-23: copying at line 4394 (`localStatement.MetaData.Copy()`, nothing else changed) makes **14 of the 416** derived forms that `FrontendDerivationGoldenTest` pins differ from the checked-in files. All 14 are on two surfaces, `Security.Role` and `Security.SetUserDeviceUserPassword`: six forms where a `Frontend.Derive` call inside the form changes from one page type to another, and eight where a title changes from the inherited `"Device User"` to the view's own `"Set Device User Password"`. Exactly the same 14 differ if the copy is made in the `Schema.Object.MetaData` setter instead, which is how the site was isolated.

Present since the initial open-source commit; not a port consequence. Found while `fix/bug-remembered-statement-applies-a-changed-setting` was being diagnosed — the same shape of shared object, at a different site, with a different consequence — and deliberately kept out of that ticket, which therefore leaves `Schema.Object.MetaData` alone.

## Why this needs weighing rather than just fixing

The checked-in files under `tests/goldens/frontend` are the only record of what this engine derives; the fixture's own summary says a change anywhere in the derivation engine should show up there "as a reviewable difference rather than as a form a user finds wrong". Fixing this produces exactly such a difference, in 14 files, and the right call on each has to be read rather than assumed — the title change looks plainly right, the page-type change in the six `Security.Role` forms has not been reasoned through at all and may point at a second thing wrong at the same site.

Beyond the two surfaces in this repository, the change reaches every application: any view that carries a `Frontend.*` tag the table underneath also carries starts honouring the view's own value. That is the correct behaviour by the code's own intent, and it is still a visible change to what users see, all at once.

What the ticket needs, before the fix: a decision that the view's own tags should win (the code says so, but no document does), and a reading of each of the 14 differences to confirm the new output is right rather than merely different.

## How a fix would be confirmed

Copy at `src/engine/DAE/Compiling/Compiler.cs:4394` and run `dotnet test tests/DAE.Server.Tests --filter Name=EveryDerivedFormMatchesItsGolden` — under a second. Then regenerate the expectation files with `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1`, read the whole diff, and commit it with one line per surface saying why the new form is right. A case that pins the behaviour directly, rather than through the whole corpus, would be a view with a tag its source also has, asserting the view's value is the one derived.

Also worth settling in the same pass: whether the other create statements that share the statement's metadata with the object they build (`node.Table.MetaData` at 4194, `node.CreateOperator.MetaData` at 6490, the aggregate operator at 6857, the constraint at 7289, the reference at 8687, the device at 9377, the server link at 9326, the role at 9922, the column default at 3984) have a consequence of their own. None was found to have one, but none was looked for beyond the view.
