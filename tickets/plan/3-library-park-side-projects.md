description: Move the side projects that will not ship (the healthcare-standard libraries, the web and cloud prototypes, and the Oracle, ADO and SAS database libraries) into a separate "legacy" folder outside the build, and settle what happens to a few names from those libraries that an existing application is reported to use.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: Libraries/FHIR.API/, Libraries/FHIR.Core/, Libraries/FHIR.Device/, Libraries/FHIR.Server/, Libraries/FHIR2.Core/, Libraries/FHIR3.Core/, Libraries/CQF.Model/, Libraries/CQF.Model.FHIR/, Libraries/CQF.Model.QUICK/, Libraries/CQF.Model.USCore/, Libraries/PHINVADS.Core/, Libraries/PHINVADS.Device/, Libraries/PHINVADS.Test/, Libraries/TestCQL/, Libraries/TestCache/, Libraries/OracleDevice/, Libraries/MSOracleConnection/, Libraries/ADOConnection/, Libraries/SASDevice/, Dataphoria/, Processor/, Design/, Legacy/, Libraries/FHIR.Core/Source/Register.cs, Libraries/FHIR.Core/Source/FHIRObjectNodes.cs, Libraries/FHIR.Core/Documents/Types.Core.d4, Libraries/System.Internet/Source/Register.cs, Libraries/System.Internet/Documents/Dynamic.d4, Libraries/Sample.Demo/Sample.Demo.d4l, Dataphor/Dataphor.sln, .gitignore, tickets/index-config.json, docs/migration-notes.md, docs/architecture.md, AGENTS.md
difficulty: medium
----
# Park side projects under `legacy/`

## Goal

Several parts of the tree are side projects or database integrations the program will not ship (D6, D15): the FHIR, CQF and PHINVADS healthcare-standard libraries built on pre-1.0 HL7 packages, two test libraries tied to them, the top-level `Dataphoria/` web and "cloud" prototypes with their design notes, and the Oracle, ADO (COM) and SAS database libraries. *Parking* means moving them, history intact, under `legacy/`, where no solution, build or runtime layout includes them, so they can be revived later without being in anyone's way now. This must happen before `repo-move-to-new-layout`, which moves everything that is left and should not have to decide about these.

The one design question is a compatibility one: the plan (WS7 table, open question Q7) says an application references four D4 class names "registered by the FHIR core library" — `FHIRCore.ObjectAsJSONSelectorNode`, `FHIRCore.ObjectAsJSONReadAccessorNode`, `FHIRCore.ObjectAsJSONWriteAccessorNode` and `FHIRCore.FHIRBaseDisplayString` — and asks that general-purpose nodes move to a shipped library under the same names before FHIR is parked. The facts below change the shape of that question.

## Scope

In scope:

| Today | Default target | Files |
|---|---|---|
| `Libraries/FHIR.API`, `FHIR.Core`, `FHIR.Device`, `FHIR.Server`, `FHIR2.Core`, `FHIR3.Core` | `legacy/libraries/<same name>` | |
| `Libraries/CQF.Model`, `CQF.Model.FHIR`, `CQF.Model.QUICK`, `CQF.Model.USCore` | `legacy/libraries/<same name>` | |
| `Libraries/PHINVADS.Core`, `PHINVADS.Device`, `PHINVADS.Test` | `legacy/libraries/<same name>` | includes vendored `Hessiancsharp.dll`, `VadsClient.dll` |
| `Libraries/TestCQL`, `Libraries/TestCache` | `legacy/libraries/<same name>` | |
| `Libraries/OracleDevice`, `Libraries/MSOracleConnection`, `Libraries/ADOConnection` (with `adodb.dll`), `Libraries/SASDevice` | `legacy/libraries/<same name>` | |
| `Dataphoria/` (top level: `Dataphoria.Web`, `.Web.API`, `.Web.Client`, `.Web.Core`, `.Web.FHIR`, `.Processing`, `.Instancing`, `.Instancing.Common`, `.Instancer`, `.Coordination`, `.Coordination.Common`) | `legacy/Dataphoria/` | 379 |
| `Processor/` (a D4 library, `Processor.Base`, for the Dataphoria processing prototype) | `legacy/Processor/` | 4 |
| `Design/` (a PDF, a Word document and a Visio drawing about the cloud prototype) | `legacy/Design/` | 3 |
| the object-as-JSON / display-string class names | see "The FHIR class-name question" | |

The plan's layout table gives `legacy/` for the second group and `legacy/libraries/` for the parked device libraries without saying whether the FHIR family is "libraries"; the default above puts every D4 library under `legacy/libraries/` and everything else directly under `legacy/`. The planner may change this; whatever it picks, `repo-move-to-new-layout` must not have to move these again.

Out of scope, with owners:

- Everything not listed above that is still in `Libraries/`, `Dataphor/`, `Tests/` and the top level: `repo-move-to-new-layout`. That includes two rows the plan's layout table maps to `legacy/libraries/` but which are not in this ticket's assignment: `Libraries/Documentation` (DocBook reference generator) and `Libraries/ODBCConnection` (whose fate is disputed between the layout table and WS7; the move ticket records it).
- Deleting dead device libraries: `library-remove-dead-devices`. Deleting dead trees, including `Dataphor/Dataphoria/Dataphoria.Web.API` (the template inside the IDE folder — a different directory from the top-level `Dataphoria/Dataphoria.Web.API` parked here): `repo-delete-dead-trees`.
- Making anything in `legacy/` build, or fixing its references: nobody in this program.
- `Legacy/Documentation/` content: WS8 (`docs-convert-asciidoc`, `feat-ws8-documentation-remainder`). See the case-collision fact below, which may still require renaming its parent folder.

## Facts verified in the tree

### The FHIR class-name question

- **The four names exist nowhere as code.** No C# class named `ObjectAsJSON*Node` or `FHIRBaseDisplayString` exists in the repository, and nothing registers those names. `git log -S ObjectAsJSON --all` and `git log -S FHIRBaseDisplayString --all` find only the commit that wrote the plan and commit `9398796c` (2016, "Added support for Dynamic values"), which introduced the names **inside `//` comments**. They still appear only as comments: `Libraries/System.Internet/Documents/Dynamic.d4` lines 15 and 19–21 (a commented-out `AsDisplayString` representation and a commented-out `AsJSON` representation of the `Dynamic` type), and line 13 of `Libraries/FHIR.Core/Documents/Types.Core.d4`, `FHIR2.Core/…` and `FHIR3.Core/…` (the same commented `AsDisplayString` line). So, in this codebase, no D4 script that uses these names could have compiled against a 3.1 server built from it. An application that "references" them either carries a copy of these comments (for example a copied `Dynamic` type definition), or was built against code that is not in this repository.
- **What the FHIR core library does register** (`Libraries/FHIR.Core/Source/Register.cs`, `DAERegister.GetClasses()`; named by `[assembly: DAERegister("Alphora.Dataphor.FHIR.Core.DAERegister")]` in `Properties/AssemblyInfo.cs`): every `Hl7.Fhir.Model.Base` subtype under its full .NET type name; `FHIR.Core.FHIRObjectConveyor`; `FHIR.Core.GenerateTypesNode`; `FHIR.Core.FHIRAsJSONSelectorNode`, `…FHIRAsJSONReadAccessorNode`, `…FHIRAsJSONWriteAccessorNode`; the three `FHIRAsXML…` equivalents; `FHIR.Core.SatisfiesSearchParamNode`; `FHIR.Core.CurrentNode` (class `NilaryCurrentNode`). `FHIR2.Core` and `FHIR3.Core` register the same set under the prefixes `FHIR2.Core.` and `FHIR3.Core.`. Note the prefix: `FHIR.Core.`, with a dot; none of the registered names starts with `FHIRCore.`. Registration is a name → assembly-qualified type map that the server reads when the library is registered (`Dataphor/Server/Schema/LibraryUtility.cs`; see `docs/architecture.md`, "Libraries and class loading"); a D4 `class "…"` string must match a key exactly.
- **Those registered nodes depend on FHIR.** `FHIRObjectNodes.cs` calls `Hl7.Fhir.Serialization.FhirParser.ParseFromJson` / `FhirSerializer.SerializeToJson` on `Hl7.Fhir.Model.Base` values (and the XML equivalents); `FHIRObjectConveyor.cs` does the same for storage; `SearchNodes.cs` and `GenerateTypesNode.cs` are FHIR-model specific. None of them is general-purpose; they cannot move to a shipped library without taking the HL7 package with them.
- **The general-purpose JSON support already moved, with its names kept.** `Libraries/System.Internet/Source/Register.cs` (lines 53–72) registers `Newtonsoft.Json.Linq.JObject` and nineteen names with the `FHIR.Core.` prefix that are implemented in System.Internet itself: `FHIR.Core.JSONObjectConveyor` (`JSONObjectConveyor.cs`) and `FHIR.Core.DynamicEqualNode`, `…DynamicSelectorNode`, `…DynamicContentSelectorNode`, `…DynamicContentReadAccessorNode`, `…DynamicContentWriteAccessorNode`, `…DynamicGetNode`, `…DynamicGetTypeNode`, `…DynamicSetNode`, `…DynamicToListNode`, `…DynamicToBooleanNode`, `…DynamicToByteNode`, `…DynamicToShortNode`, `…DynamicToIntegerNode`, `…DynamicToLongNode`, `…DynamicToStringNode`, `…DynamicToDecimalNode`, `…DynamicToTimeSpanNode`, `…DynamicToDateTimeNode` (`DynamicNodes.cs`). The shipped `Dynamic` type (`System.Internet/Documents/Dynamic.d4`) uses `class "FHIR.Core.JSONObjectConveyor"`. These were written in FHIR.Core in 2016 and later moved to System.Internet under the old names — the exact pattern the plan asks for, already done. `System.Internet.d4l` requires only `System`; it has no dependency on any FHIR library. `PHINVADS.Core/Source/Register.cs:50` has a commented-out registration of the same conveyor.
- The engine's own `System.ObjectAsStringReadAccessorNode` (`Dataphor/DAE/Runtime.Instructions/ObjectNodes.cs:107`), used by the FHIR and `Dynamic` types' `AsString` representation, is built in and unaffected.

So parking FHIR removes only FHIR-dependent class names; the general-purpose ones the application is most likely to use already ship in System.Internet. What remains open is only what the application's four strings actually are, which no agent can check (the application corpus is never copied into this repository, Q5).

### Other dependencies on parked code

- **`Libraries/Sample.Demo/Sample.Demo.d4l` requires `ADOConnection`**, but no file in `Sample.Demo/Documents/` uses an ADO connection (searched for `ADO` and the connection's class name). Parking `ADOConnection` would make the sample fail to register. Samples are test corpus (WS3 L7), so this matters.
- No other live library requires a parked one (checked every `libraries.libraryreference` in `Libraries/*/*.d4l`). Requirements among parked libraries stay as they are, including `TestCache` → `CacheConnection`, `CacheSQLDevice`, which `library-remove-dead-devices` deletes, and `FHIR.Server` → `PGSQLDevice`, which stays live.
- No live project has a `ProjectReference` to a parked project, and nothing live references `adodb.dll` or the PHINVADS binaries.
- `Dataphor/Dataphor.sln` includes parked projects: `FHIR.Core`, `FHIR.Device`, `FHIR.Server`, `FHIR2.Core`, `FHIR3.Core`, `FHIR.API`, `CQF.Model`, `PHINVADS.Core`, `PHINVADS.Device`, `DAE.Connection.ADO`, `DAE.Connection.Oracle` (the `MSOracleConnection` project), `DAEDeviceOracle`, `DAEDeviceSAS`, ten top-level `Dataphoria.*` projects and the `Dataphoria` solution folder (`{658579EE-4680-4E26-8BD7-6AB8C3B70252}`). Find each by its path in the `Project(` line, then remove every line containing its GUID, as `repo-delete-dead-trees` does for its own entries.
- Four top-level `Dataphoria/` project files reference the SQL CE project that `repo-delete-dead-trees` deletes; parked code may dangle.
- The spike (`tools/net-spike/projects.txt`, `COVERAGE_LIBS`) builds and copies none of the parked paths, so its results must be identical before and after.
- `.gitignore` line `/Dataphoria/Dataphoria.Web/Properties/PublishProfiles/Bobby.pubxml` ignores a personal publish profile under the parked tree; after the move it matches nothing.
- `tickets/index-config.json` excludes some parked paths (`Design/`, `Dataphoria/…/Scripts/`, `Libraries/FHIR*.Core/Documents/`) and `Legacy/`.
- K15 lists six library projects that write Release output where the server does not look; five of them (CQF.Model, FHIR.API, FHIR.Server, PHINVADS.Core, PHINVADS.Device) are parked here, leaving only TestFramework.Coverage.Base for `build-runtime-layout`.

### The `Legacy/` case collision

`Legacy/` already exists and holds 786 files, all under `Legacy/Documentation/` (the DocBook sources, kept until WS8). The target `legacy/` differs only in letter case. This machine has `core.ignorecase=true` (Windows default): a `git mv Libraries/FHIR.Core legacy/libraries/FHIR.Core` lands on disk inside the existing `Legacy\` folder, while Git may record the new paths as `legacy/…` or adjust them to `Legacy/…`. Either way, a case-sensitive checkout (CI, Linux, macOS with a case-sensitive volume) would then show two top-level folders or not match the plan. The plan's rule for case-only renames (two `git mv` steps through a temporary name) is in "Rules for the move", but the plan also says `Legacy/Documentation/` stays where it is until WS8.

## Open choices for the planner

1. **The class-name question.** Options:
   - (a) Move nothing; add a migration-notes entry that the FHIR libraries' class names (list the `FHIR.Core.`, `FHIR2.Core.`, `FHIR3.Core.` names and the HL7 model type names) are gone, that the `FHIR.Core.`-prefixed `Dynamic` and `JSONObjectConveyor` names still ship in System.Internet, and that the four names cited in the plan were never registered by any library in 3.1 source. **Recommended default.** It is consistent with every fact above and changes no code.
   - (b) Implement and register the four names in System.Internet as general-purpose nodes (a JSON representation of any object through Newtonsoft, and a display string). This invents behaviour the 3.1 source never had, and the names would carry a misleading `FHIRCore.` prefix; only worth it if the owner confirms the application really executes them.
   - (c) Route to `blocked/` asking the owner for the exact strings the application uses and the file they appear in (generic description only, no customer detail — the repository is public). The proposed default for that blocked ticket is (a).
   Whichever is chosen, update the plan's Q7 answer and WS7 row so they no longer say the names are registered by the FHIR core library, and correct the migration-notes row that currently says they are "either moved to a shipped library under the same names or unavailable".
2. **`Legacy/` versus `legacy/`.** Options: (a) make the first commit of this work a two-step case-only rename `Legacy` → `legacy` (so the DocBook sources become `legacy/Documentation/`), then park into `legacy/`; update every reference to `Legacy/` — `AGENTS.md` (2 lines), `docs/modernization-plan.md` (5 lines), `tickets/index-config.json`, and open tickets that cite `Legacy/Documentation` (today `docs-convert-asciidoc`, `feat-ws8-documentation-remainder`, `store-remove-sqlce-references`, `test-triage-known-failures`, `repo-delete-dead-trees`, `library-remove-dead-devices`; re-run `grep -rl "Legacy/" tickets` because the board changes). **Recommended default**: one parent folder in every checkout, and the DocBook tree is going to be deleted by WS8 anyway. (b) Leave `Legacy/Documentation` and accept a case-split parent — rejected: it behaves differently on case-sensitive and case-insensitive file systems. If the planner finds that the plan's "stays where it is" wording forbids (a), this is the place to route to `blocked/` with (a) as the proposal.
3. **`Sample.Demo`'s unused requirement on `ADOConnection`.** Default: remove that `libraries.libraryreference` line from `Sample.Demo.d4l` in a separate content commit (after the rename commits), and confirm by registering `Sample.Demo` in the spike's library copy if the harness can do so cheaply, otherwise by inspection of its scripts. The alternative, keeping `ADOConnection` live, contradicts D15. The `.d4l` format is unchanged; only one sample's dependency list changes; no migration note (samples are not customer code).
4. **Target paths** for the non-library trees (defaults in the scope table) and whether the FHIR family sits under `legacy/libraries/` (default) or directly under `legacy/`.
5. **Whether `legacy/` is indexed for code search.** Default: exclude `legacy/` in `tickets/index-config.json` (it already excludes `Legacy/`; adjust the case), and drop the now-stale entries for parked paths.

## Constraints

- **Move commits are rename-only** (plan, "Rules for the move"): `git mv` and nothing else, so Git records exact renames and `git log --follow` and `git blame` keep working. Edits to `Dataphor/Dataphor.sln`, `.gitignore`, `tickets/index-config.json`, `Sample.Demo.d4l` and docs go in later commits. Case-only renames go through a temporary name in two `git mv` steps. Verify each move commit with `git show --stat -M` (or `git diff --cached -M --name-status` before committing) showing only `R100` entries.
- Parking changes no file inside the parked trees: no fixing of their references, project files or `.d4l` files.
- D4 class names, assembly names and `.d4l` formats are compatibility surfaces: if any code moves to System.Internet (option 1b only), the registered strings must be exactly the application's strings, the assembly name of System.Internet does not change, and the move comes with a characterization test (a D4 script through the in-process engine harness) of the moved behaviour.
- The legacy `Dataphor/Dataphor.sln` is edited by removing whole lines only, so that the other Phase 1 tickets' edits to it do not conflict.
- Privacy: no customer names, paths or schema in anything written (tickets/rules/10-dataphor.md).

## Interaction with `repo-move-to-new-layout`

The move ticket (prereq: this one, `repo-delete-dead-trees`, `library-remove-dead-devices`) moves what is left and assumes nothing parked remains under `Libraries/` or at the top level except what its own inventory lists. If this ticket renames `Legacy/` to `legacy/`, the move ticket must not plan its own rename of that folder and must cite `legacy/Documentation/`; say so in the emitted ticket's handoff so the move planner sees it. The move ticket also rewrites `AGENTS.md`'s layout table and `tickets/index-config.json` wholesale; this ticket edits them only as far as parking needs.

## Done looks like

- Every path in the scope table is under `legacy/` at the chosen target, recorded by rename-only commits; `git ls-files | cut -d/ -f1 | sort -u` shows one `legacy` entry and no `Legacy` (if option 2a), no `Dataphoria`, `Processor` or `Design`.
- `Dataphor/Dataphor.sln` lists no parked project and no `Dataphoria` solution folder.
- `.gitignore` has no line pointing into the parked tree's old path; `tickets/index-config.json` excludes `legacy/`.
- The spike gives the same results before and after: `python tools/net-spike/run.py <work-dir> --smoke --coverage` (work dir outside the repository): engine, server and libraries 0 errors, concurrency 0 errors, coverage `pass=62 fail=14` with the same failing scripts.
- `docs/migration-notes.md`: the FHIR/CQF/PHINVADS row rewritten per choice 1 and marked **done**; the "Oracle (Microsoft provider), ADO and SAS parked" row (left **planned** by `library-remove-dead-devices`) marked **done**.
- `docs/architecture.md` "What is alive" table and `AGENTS.md` layout table no longer describe the parked trees as being at their old paths.
- No new C# test unless option 1b is taken (then the characterization test above).

## Risks

- Case handling on Windows silently producing paths in the wrong case. Mitigation: check `git ls-files` output after each move commit, not just the file system.
- Breaking a live library that depended on a parked one without anyone noticing, because the spike registers only the coverage libraries. Mitigation: the dependency scan above (only `Sample.Demo` was found); re-run it after the move.
- Treating the plan's four names as real and building option 1b on that premise. Mitigation: the facts above; prefer (a) or (c).

If this splits, the ticket that finishes the work item keeps the slug `library-park-side-projects`; earlier pieces get new slugs and are chained to it with `prereq:`.
