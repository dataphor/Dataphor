description: A test claims every D4 script file in the repository can be read, but thirty-eight of them cannot be; none of those turns out to be a defect in the reader, so the test is taught the real reasons instead and three files it had given up on are brought back under test.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, src/engine/DAE/Schema/SQLDeviceMappings.d4, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLDevice.cs, libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs, docs/modernization-plan.md, tickets/backlog/test-pre-3-1-scripts-policy.md
repro: verified
difficulty: medium
----
## What the investigation found

The plan ticket's premise was that the parser has grammar gaps and that three files the server needs at bootstrap are among the casualties. **Neither holds.** All thirty-eight files were parsed one at a time against the current `Parser`, with the line, column, offending token and inner exception captured for each. Not one failure is a parser defect. The parser is correct in all thirty-eight cases; what is wrong is the corpus test's premise that every `.d4` file in the repository is a standalone D4 script.

In particular, the plan ticket's "integer literals with a device prefix the parser does not accept" was a misreading. `libraries/MSSQLDevice/Source/SystemCatalog.d4` is a **`String.Format` template**: `{0}` is the device-name placeholder and `{{` / `}}` are escaped braces. `MSSQLDevice.cs:192` reads it as an embedded resource and calls `String.Format(text, Name, "false", IsMSSQL70…, IsAccess…)` before executing it. `PostgreSQLDevice.cs:138` does the same. The "integer literal" the parser complained about was the `0` inside `{0}`.

And `src/engine/DAE/Schema/SQLDeviceMappings.d4` is not read at bootstrap, or ever: it is referenced by nothing. The file the server does read is `src/engine/DAE/Schema/SystemCatalog.d4`, which is listed as an `EmbeddedResource` in `DAE.csproj`, loaded at `src/engine/DAE/Server/Engine.cs:1182` — and it already parses and round-trips today.

### The thirty-eight, each counted once

| Where | Files | What they are |
|---|---|---|
| `legacy/` | 20 | Documents in the parked trees, parked by `library-park-move` and `library-park-side-projects`. Mostly scratch notes with no `;` between statements; one is in a fluent `.where(…)` syntax that is not D4 at all, one is pseudo-schema prose. Two of the twenty are also one of the kinds below: the Oracle device's `String.Format` template, and a SAS mappings file using the removed `create device { … }` form |
| `libraries/` | 2 | The MSSQL and PGSQL device `SystemCatalog.d4` — `String.Format` templates, as above |
| `src/engine/DAE/Schema/` | 1 | `SQLDeviceMappings.d4`, an orphan using grammar removed in 2006 (below) |
| `tests/libraries/TestFramework.Coverage.Scripts/` | 8 | Already owned by `test-pre-3-1-scripts-policy`: `tests/DAE.Server.Tests/coverage-manifest.json` records each as `known-failure` against that slug **with the identical error text**, and the owner decided on 2026-11-18 to keep them that way and have that ticket fix them |
| `tests/libraries/TestFramework.Coverage.Base/` | 1 | `UnsupportedOracleOperators.d4` — a missing `;`, and it targets the parked Oracle device |
| `samples/` | 6 | Documents in the supported tree that are not whole scripts: a missing `;`, a missing `then`, a `key` clause in a table *type*, an `entry` type the language does not have — each verified at a named line below |

### Why there is no parser work to do

Each supposed gap was checked against the parser and the engine, not guessed at:

- **`create device <name> { <maps> }`** — used by `src/engine/DAE/Schema/SQLDeviceMappings.d4` and `legacy/libraries/SASDevice/Source/Device.SAS/SASDeviceMappings.d4`. The original authors **deliberately removed** this from the grammar in 2006; the removal and its four reasons are a comment in `Parser.cs` immediately above `CreateDeviceStatement` (around line 5888), and the code it removed is still there commented out. Device maps are created through `alter device` instead.
- **`entry { … }` and `presentation { … }` type specifiers** — `TypeSpecifier()` (`Parser.cs:5462`) offers generic, scalar, row, table, list, cursor and typeof, and `BNF.txt:711` agrees. There is no `entry` or `presentation` type anywhere in the engine's schema. `test-pre-3-1-scripts-policy` independently established that neither keyword existed at tag `v3.1` either.
- **`table { …, key { … } }`** — a table *type specifier* is a column list and nothing else (`Parser.cs:5581`, `BNF.txt:728`). Keys belong to table variables, not to table types. Accepting `key` here would be a language change, which the compatibility rule forbids without a ticket that says so.
- **`"…"i` IString literals** — the lexer token and the parser arms for it are inside `#if USEISTRING`, which no project defines (`Parser.cs:1606`, `Parser.cs:8118`). Turning IString on is a language change, and `test-pre-3-1-scripts-policy` already owns the two scripts that want it.
- **`create domain`** — `domain` was the pre-3.0 spelling of `type`. Not a keyword today and not one at `v3.1`.
- **A missing `;` or a missing `then`** — the parser is simply right. No parser change can accept these without changing D4.

## What to build

### 1. Cover the three format templates instead of excluding them

Add a second list to `ParserRoundTripCorpusTest`, keyed the same way as `AllowList`, mapping a repository-relative path to the `String.Format` arguments its own C# call site passes. A file in this list is formatted before the first parse; everything after that — emit, re-parse, compare trees — is unchanged.

| Path | Arguments | C# call site |
|---|---|---|
| `libraries/MSSQLDevice/Source/SystemCatalog.d4` | `"TestDevice", "false", "false", "false"` | `libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLDevice.cs:192` |
| `libraries/PGSQLDevice/Source/SystemCatalog.d4` | `"TestDevice", "false", "false", "false"` | `libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs:138` |
| `legacy/libraries/OracleDevice/Source/SystemCatalog.d4` | `"TestDevice", "false"` | `legacy/libraries/OracleDevice/Source/Device.Oracle/OracleDevice.cs:90` (two arguments, not four) |

The argument values do not change which text the parser sees beyond the substitution, so a placeholder device name is enough; what matters is that the **count** matches the call site, because a template that references `{2}` with two arguments throws `FormatException`. Keep that faithful, and say so in the comment, so the list stays a record of what each device actually does.

**Measured: all three parse with zero messages and round-trip to an identical tree once formatted** (checked 2026-11-22 against `artifacts/bin/DAE/debug` at `e738fa5d`, with each call site's own argument list). So this is coverage recovered, not a new exclusion — including the two active SQL device catalogs, which were the plan ticket's whole worry.

### 2. Make the parked tree best-effort rather than hand-listed

`legacy/` holds the parked trees. Seventy-seven `.d4` files live there; fifty-seven parse and round-trip cleanly today and twenty do not (measured 2026-11-22, every file under `legacy/` parsed individually). Hand-listing the twenty is what the current `AllowList` does, and it buys nothing: nobody owns the D4 in a parked tree, so a failure there is not a defect anyone will fix.

Instead: in `RoundTrips`, when the file is under `legacy/` **and** the first parse produced messages, call `Assert.Ignore` with the path and the first message. The fifty-seven that pass keep passing — including the round-trip comparison, because once a parked file parses the emitter still owes a faithful tree. Then drop all twenty `legacy/` entries from `AllowList`.

Guard the obvious hole with a ratchet: one `[Test]` that walks the parked tree, counts the files whose first parse produces messages, and asserts the count is no greater than **19** (twenty today, minus the Oracle template which moves to the format-template list). Name the constant, and put the measurement date and the command in its comment. Deleting a parked file only makes it pass; a parser regression that breaks a parked file makes it fail. Then add a `NOTE:` at the Ignore site recording the tradeoff that is left: a regression that breaks one parked file and fixes another is invisible to the count.

### 3. Delete the orphan `src/engine/DAE/Schema/SQLDeviceMappings.d4`

It is dead four times over:

- Referenced by nothing. `DAE.csproj` names its embedded resources one by one (no globs) and this file is not among them; no `.d4l` lists it; a repository-wide search outside `artifacts/` finds it only in the corpus test's own allow list and in ticket text.
- Its `create device <name> { <maps> }` form was removed from the grammar in 2006, so it has not been loadable by this parser for twenty years.
- Its tail creates a device of class `LinterDevice.LinterDevice`. The Linter device was deleted by `library-remove-dead-devices`.
- Its last two statements embed an encrypted device-user password literal, which is not something a public repository should carry.

Delete it, and drop its `AllowList` entry. Git keeps it, so this is reversible.

**No `docs/migration-notes.md` entry.** The file was never compiled into an assembly, never listed by a `.d4l`, and never read by any code, so nothing an application, a deployment script or a C# extension does can notice it going. Before deleting, re-confirm those two facts (the `EmbeddedResource` list in `src/engine/DAE/DAE.csproj`, and a repository-wide search for the file name excluding `artifacts/` and `tickets/`) and say in the handoff that you did; if either turns out otherwise, add the note.

Rejected alternative: moving it under `legacy/`. `legacy/` holds parked *libraries* — a `.d4l` with its sources — and this file belongs to no library.

### 4. Give the fifteen remaining entries verified reasons

`AllowList`'s value today is a bare category word (`"parse"`). Replace it with one sentence per file saying what the file is, the line and token that stops the parser, and the ticket that owns it where one does. These are the fifteen, all verified by parsing each file individually on 2026-11-22:

**Owned by `test-pre-3-1-scripts-policy`** — all eight already carry the same error text in `tests/DAE.Server.Tests/coverage-manifest.json` as `known-failure` against that slug, and the owner has already ruled on them. Name that slug in each reason and change nothing else about them.

| File (under `tests/libraries/TestFramework.Coverage.Scripts/Documents/`) | Stops at |
|---|---|
| `Domains.d4` | line 9 `create domain TestDomain like Integer;` — `domain` was the pre-3.0 name for `type` |
| `EntryLibrary.d4` | line 8 `entry {ID : Integer, Name : String}` — no `entry` type specifier |
| `ForeignSuperKeysAndConstantReferences.d4` | no `;` after the statement ending at line 19, so the parser stops at line 24's `alter table A` |
| `IStringLibrary.d4` | line 12 `"Blah"i` — IString literals are behind `USEISTRING`, which nothing defines |
| `LibraryDomains.d4` | line 10 `'…'i` — same IString literal |
| `PresentationLibrary.d4` | line 10 `presentation{entry{}}` — neither type specifier exists |
| `Serialization.d4` | no `;` after line 17's debugging `select`, so the parser stops at line 18 |
| `SerializationDomains.d4` | line 13 `create domain SimpleDomain like String;` |

**The supported tree's seven non-scripts** — IDE documents and scripts with defects that predate the port:

| File | Stops at |
|---|---|
| `tests/libraries/TestFramework.Coverage.Base/Documents/UnsupportedOracleOperators.d4` | no `;` after line 8's `EnsureDeviceStarted('TestOracle.OracleTestDevice')`; also targets the Oracle device, which is parked |
| `samples/Sample.Common/Documents/D4LanguageTour.d4` | no `;` after line 131's `select … union (…)`, so the parser stops at line 133. A guided tour of the language, read a statement at a time in the IDE; most of its statements are terminated and a handful are not |
| `samples/Sample.Shipping/Documents/Coverage.Controller.d4` | no `;` after line 15's `select ServerSettings`, so the parser stops at line 16 |
| `samples/Sample.Shipping/Documents/Coverage.AdministrationRole.d4` | line 16 `if exists (…)` with no `then` before the statement on line 17 |
| `samples/Sample.Shipping/Source/CoverageRoles/Administration.d4` | the same defect at the same line — the two files are copies |
| `samples/Sample.Shipping/Source/CoverageRoles/Sales.d4` | line 179 `table { …, key { Number } }` — a table type specifier takes a column list only |
| `samples/Sample.Shipping/Source/Shipping.d4` | line 658 `entry { … }` as an operator's parameter type |

**Do not repair any of this text in this ticket.** Whether a D4 document's statements may be edited to make it run is an open owner decision on `test-pre-3-1-scripts-policy`, which says the plan stage "lists each proposed statement change with its evidence and asks for approval in `blocked/` before editing". The plan stage has already recorded these seven files on that ticket as a second arm of the same question (see *Board changes* below); leave the files themselves alone.

### 5. Hold the lists against the tree

Two assertions, in the shape `SamplesCorpusTest` already uses for its three lists:

- Every path in `AllowList` and in the format-template list names a file that exists, so a rename or a deletion cannot leave a stale entry behind.
- Every path in `AllowList` still produces at least one parser message, failing with "this file now parses; remove its entry". That is what keeps a reason from outliving its cause.

These are not wiring tests: both encode a claim the list makes about the tree, and both have caught nothing yet only because the list has never been maintained. One assertion each; no other new tests. The format templates need no test of their own — they become three ordinary corpus cases.

### 6. Update the plan

`docs/modernization-plan.md:330` (the `test-parser-roundtrip-corpus` row) ends with "What remains is one named allow list of 38 files the parser cannot read at all, each with a one-line reason, owned by `bug-d4-parser-rejects-active-sources`." Replace that with what is actually true afterwards: no parser defect was found; the corpus now formats the three device templates before parsing them, treats parse failures under `legacy/` as Ignored with a ratchet on the count, and keeps fifteen named exclusions — eight owned by `test-pre-3-1-scripts-policy` and seven documents in the supported tree that are not whole scripts.

## Board changes — already done, nothing to do here

The plan stage appended the arm to `tickets/backlog/test-pre-3-1-scripts-policy.md` (rather than filing a new ticket: that slug already owns the "may a D4 document's statements be repaired" question, and the eight Coverage.Scripts files that fail at parse time are already its own). It is on the board now, in that ticket's *Second arm* section. Recorded here so the implementer and the reviewer know where the seven documents went, and do not file them again:

- Seven more documents outside the coverage manifest have the same kind of defect, found by `ParserRoundTripCorpusTest`: the seven in the table above, with their line numbers and what each is.
- Five of them are a missing `;` or a missing `then` — one or two characters each — which is the same repair the ticket's "Live intent, broken text" bullet already asks the owner about.
- The other two (`Sales.d4`, `Shipping.d4`) want language features that do not exist, so repairing them means rewriting the statement, not adding a character.
- `D4LanguageTour.d4` is the one with user-visible weight: it is the guided tour of D4 that ships in `samples/Sample.Common`, and a reader who executes the whole document rather than one statement at a time gets a syntax error at line 133.

## Edge cases & interactions

- **`FormatException` from a template entry.** A template whose argument count is short of its highest placeholder throws rather than failing a parse. Let it throw into the case and make the message say which file and that its argument list must match its C# call site — verified by the fact that Oracle's file takes two arguments where the other two take four. *Verified by: the three cases passing, which is only possible with the right counts.*
- **A template file that would also parse unformatted.** None does today. Nothing needs to detect it; the template path simply wins. *By inspection.*
- **A `.d4` added under `legacy/` that does not parse.** Ignored, and counted by the ratchet, which then exceeds 19 and fails. That is the intended signal: a new parked file is a conscious act and the constant moves with it in the same commit. *Verified by: the ratchet test itself.*
- **A `.d4` added under `legacy/` that does parse but does not round-trip.** Fails, like any other file. The Ignore is keyed on the *first parse* producing messages, not on the path alone — get this ordering right, or the parked tree silently stops testing the emitter. *Verified by: the Ignore's placement, and by the fifty-seven parked files that pass today including their tree comparison.*
- **A parked file deleted while the ratchet stands at 19.** Passes (the assertion is an upper bound). Intended.
- **The corpus walk and `legacy/`.** Do **not** add `legacy` to `ExcludedDirectoryNames`: that would drop all seventy-seven parked files, losing the fifty-seven that pass. The Ignore rule is what distinguishes the two. *By inspection — and the counts are in this ticket if a reviewer wants to re-measure.*
- **Case and separators in the path test for `legacy/`.** The walk already normalizes to forward slashes and the existing sets are `OrdinalIgnoreCase`; match that, or the rule misses on a differently-cased checkout. *By inspection.*
- **File encoding.** Several of these files have a UTF-8 byte-order mark and others do not; the test already reads with `Encoding.UTF8`, which handles both. Touching none of these `.d4` files in this ticket sidesteps the source-hygiene rule entirely — except for the one deletion, which preserves nothing by definition. *By inspection.*
- **`SamplesCorpusTest`'s accepted-tradeoff note.** It justifies not starting servers for `Sample.Forum` and `Sample.Pubs` partly with "every `.d4` file in the tree is already parsed by `ParserRoundTripCorpusTest`". That stays true: neither sample is in the allow list and neither is under `legacy/`. No edit needed there, but check it rather than assume it. *Verified by: those two paths appearing in no list.*
- **`coverage-manifest.json` stays untouched.** The eight Coverage.Scripts entries already point at `test-pre-3-1-scripts-policy` with matching error text. Changing them is that ticket's job. *By inspection.*

## TODO

- Re-measure first: build, then run `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ParserRoundTripCorpusTest" --logger "console;verbosity=normal"` and confirm the pass is green with the thirty-eight-entry list as it stands.
- Add the format-template list with the three entries and their argument lists, format before the first parse, and remove those three paths from `AllowList`.
- Add the parked-tree Ignore rule in `RoundTrips`, keyed on being under `legacy/` *and* the first parse having produced messages; remove the twenty `legacy/` entries from `AllowList`.
- Add the ratchet test over the parked tree with the named constant at 19, its measurement comment, and the `NOTE:` recording what the count cannot see.
- Verify `src/engine/DAE/Schema/SQLDeviceMappings.d4` is unreferenced (the `EmbeddedResource` list in `src/engine/DAE/DAE.csproj`, and a repository-wide search excluding `artifacts/` and `tickets/`), then delete it and its `AllowList` entry.
- Rewrite the fifteen remaining `AllowList` values as the one-sentence reasons in the tables above, naming `test-pre-3-1-scripts-policy` on the eight it owns.
- Add the two list-versus-tree assertions (every listed path exists; every `AllowList` path still fails to parse).
- Update the `test-parser-roundtrip-corpus` row in `docs/modernization-plan.md`.
- Run the pass again and confirm: the three templates now appear as passing cases, the parked tree shows nineteen Ignored, no case fails, and the allow list is fifteen entries.
- Then run the three suite parts from the root `AGENTS.md`, each as its own foreground command, to confirm nothing else moved.
