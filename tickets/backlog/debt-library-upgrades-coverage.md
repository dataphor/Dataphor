description: Installing, versioning and upgrading a Dataphor library is how every customer application is deployed and updated, and three quarters of the engine code that does it is untested - an upgrade that half-applies would leave a customer's database in a state nobody has ever tested.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/Server/Runtime.Instructions/LibraryNodes.cs, src/engine/Server/Schema/LibraryUtility.cs, src/engine/DAE/Schema/Library.cs, src/engine/DAE/Runtime.Instructions/LibraryNodes.cs, tests/DAE.Server.Tests/LibraryLoadingTest.cs
tradeoffs: The happy path is exercised constantly - every test server registers libraries to start at all - so what is missing is the failure and upgrade paths, and a maintainer may argue those are better covered by the catalog-rebuild work than by new scripts here.
----
## The area

**Libraries and upgrades**. Measured **49.9 % line coverage of 1,907 coverable lines, 956 uncovered**, on the full three-part run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`).

| File | Line coverage | Coverable |
|---|---:|---:|
| `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` | 24.0 % | 854 |
| `src/engine/Server/Schema/LibraryUtility.cs` | 67.9 % | 470 |
| `src/engine/DAE/Schema/Library.cs` | 70.0 % | 350 |
| `src/engine/DAE/Runtime.Instructions/LibraryNodes.cs` | 78.1 % | 233 |

The shape of it is clear from that table: the area average hides one file. `src/engine/Server/Runtime.Instructions/LibraryNodes.cs` is 45 % of the area's lines and is itself at 24 % - on its own it would be a gap. It holds the implementation of the library operators, and the uncovered three quarters of it is where this ticket points.

## What is covered already, so nothing is restated

The area is not at 49.9 % by luck. Every test server in the suite registers libraries in order to start at all, so loading, the name resolution path and the descriptor reading are well exercised by the fixtures. `tests/DAE.Server.Tests/LibraryLoadingTest.cs` goes further and covers the loading of a library with its assembly, and `tests/libraries/LibraryLoadingProbe` exists as its probe. A new script must not re-cover that ground.

What those leave untouched is everything an administrator does to a library *after* it is working, and everything that happens when one of those steps goes wrong.

## What a coverage script should exercise

A D4 script through the in-process harness, against a library the script creates itself rather than any shipped one. The operators, from `src/engine/Server/Runtime.Instructions/LibraryNodes.cs`:

- **The life cycle**: `CreateLibrary`, `RegisterLibrary` (both overloads, with and without reconciliation), `EnsureLibraryRegistered`, `UnregisterLibrary` (both overloads), `LoadLibrary`, `UnloadLibrary`, `RenameLibrary`, `DropLibrary`. Each of those is a separate uncovered entry point today.
- **Attach and detach**: `AttachLibrary(name, directory)`, `AttachLibraries(directory)`, `DetachLibrary(name)`, `RefreshLibraries(name)`. These are how a library that exists on disk but not in the catalog is brought in, which is the deployment path, and none of them is reached.
- **Descriptors and settings**: `GetLibraryDescriptor` / `SetLibraryDescriptor` round-tripping a descriptor with requisites, files and a version; `LibrarySetting(name)`.
- **Upgrades**: `UpgradeLibraries()`, and the per-library upgrade path that reads the numbered scripts from a library's `Upgrades/` folder. This is the one with the most at stake, because it is what runs against a customer's live catalog when they take a new version.

The behaviour a script should assert, rather than just calling each operator:

- **Version arithmetic.** A library's version before and after an upgrade, read from the `Libraries` system table, and that an upgrade already applied is not applied twice. Re-running `UpgradeLibraries()` must be a no-op.
- **Ordering.** Several upgrade scripts for one library apply in their numbered order, and a library's requisites upgrade before it does.
- **Requisites.** Registering a library whose requisite is not registered must register the requisite or refuse - whichever it does today is the specification, since there is no reference runtime. Dropping a library another library requires must be refused.
- **Failure leaves a coherent state.** An upgrade script that raises part-way through: the library's recorded version must not claim the upgrade succeeded. This is the case worth the most and the one nothing comes near today.
- **The error codes.** Registering a library twice, unregistering one that is not registered, dropping one that does not exist, attaching a directory that holds no library. Error codes are a compatibility surface, so pinning the ones an administrator would actually meet is worth more than breadth.

## Interactions

- **Two of the nine no-assertion scripts belong to this area.** `LibraryFilesCoverage` and `Register` are recorded in the manifest as holding only comments (`docs/coverage-report.md`), and their names say what their authors meant: a library's declared files, and registration. A script written here replaces both in substance. Whether the empty files themselves stay in the library is `test-pre-3-1-scripts-policy`'s call - do not edit, retire or delete them here, and do not change `tests/DAE.Server.Tests/coverage-manifest.json`.
- **A library the script registers must be cleaned up.** A server rewrites `.d4l` files and creates folders in its library directory, so a script that creates a library leaves state behind for whatever runs next in the same server. The in-process fixture gives each server a fresh folder, but scripts within one run share it; drop what the script created.
- **This is not the catalog rebuild.** Rebuilding a catalog from scripts is its own workstream with its own tests (`StoreRebuildWorkflowTest.cs`). Keep to the library operators.
