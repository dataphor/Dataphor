description: Creating or deleting one of the units a Dataphor application is shipped in takes effect immediately on disk, so rolling back the transaction that did it does not undo it - and deleting one inside a transaction destroys its folder with no way back.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Runtime.Instructions/LibraryNodes.cs, src/engine/Server/Schema/LibraryUtility.cs, src/engine/DAE/Schema/Library.cs
----
## Owner decision (2026-11-23): accept the proposed answer, with one condition

Take *Proposed answer* items 1-3 as written: document the rule in `docs/architecture.md`, make `DropLibrary` refuse inside an explicit transaction (new error code, `docs/migration-notes.md` entry), leave `CreateLibrary`, `SetLibraryDescriptor` and `SetDefaultDeviceName` alone.

**Condition: before adding the refusal, verify that no caller that ships runs `DropLibrary` inside an explicit transaction**, or the guard breaks the feature. Check every path that drops a library, not only D4 scripts:

- the IDE (`src/windows/Dataphoria`): its library tree's delete/drop action and anything it does around executing a script (does it begin a transaction, or run with a session option that wraps statements in one?);
- the Frontend server side and any shipped form or D4 document that calls `DropLibrary` (`libraries/`, `samples/`);
- the client and CLI paths a tool would use (`DAE.Client` `DataSession`, NativeCLI, the script runner `D4Runner`), and session or process options that open a transaction implicitly (for example a session's default isolation or `UseImplicitTransactions`-style settings) - the guard must distinguish those from the implicit per-statement transaction;
- the coverage scripts and tests that drop libraries.

Record what each caller does in the ticket. **If any shipped caller does open an explicit transaction around `DropLibrary`, do not add the guard and do not route back to `blocked/`** (owner, 2026-11-23): do item 1 only - document the rule in `docs/architecture.md`, including that a rolled-back `DropLibrary` has already deleted the library folder and which callers run it inside an explicit transaction - and leave the code as it is. Do not change those callers either.

**Blocked on a decision, not on work: what should the operators that create, delete and reconfigure a library do when they are called inside a transaction that then rolls back?** It unblocks as soon as that is answered; the work itself is small either way.

## What is going on

A Dataphor *library* is the unit an application is shipped in: a folder in the server's library directory holding a descriptor file (`<name>.d4l`), its D4 documents and its upgrade scripts. The server also keeps the descriptor in memory, in a list separate from the catalog.

Four operators change that list: `CreateLibrary`, `DropLibrary`, `SetLibraryDescriptor` and `SetDefaultDeviceName` (and the requisite and file operators beside them). Each changes the in-memory list **and writes the folder and the descriptor file at once**, and records nothing for a rollback to replay. So a transaction that calls one and then rolls back leaves the change standing. Measured on an in-process server on 2026-11-23, each inside an `Isolated` transaction that was then rolled back: after `CreateLibrary` the library was still in the server's list; after `DropLibrary` it was still gone; after `SetDefaultDeviceName('General', 'System.Temp')` the setting was still changed.

In memory and on disk those agree, so by itself this is only "library DDL is not transactional" and not the server-disagrees-with-its-catalog defect that `debt-rollback-leaves-catalog-as-it-was` is about. Two things make it worth a decision anyway:

- **`DropLibrary` deletes the library's folder, recursively, as it runs** (`src/engine/Server/Schema/LibraryUtility.cs:274`, read rather than run — the measurement above only confirmed the in-memory half). A rollback cannot put a folder back. If an application's library folder is in there, a rolled-back `DropLibrary` has destroyed it.
- **It half-rolls-back.** `DropLibrary` also clears the library's owner and version through the catalog store, and those writes *are* undone by the store's transaction. So after a rolled-back `DropLibrary` the store still has the library's owner and version rows while the folder and the in-memory entry are gone. Registering a library is different again and is handled correctly: `RegisterLibrary` records an undo instruction and was measured to roll back cleanly.

## Proposed answer, for a maintainer to accept or edit

**Write it down and guard the destructive one.** Specifically:

1. State the rule in `docs/architecture.md`, in the section that describes libraries: *a library's descriptor and folder are files, not catalog rows; the operators that create, delete or reconfigure a library take effect when they run and are not undone by a rollback. Registering and unregistering a library are catalog changes and are undone normally.*
2. Make `DropLibrary` refuse to run inside an **explicit** transaction — one the session opened itself, not the implicit transaction every statement runs in — because that is the only one of the four whose effect cannot be repaired by running the inverse statement. New error code; `docs/migration-notes.md` entry.
3. Leave `CreateLibrary`, `SetLibraryDescriptor` and `SetDefaultDeviceName` as they are. A rolled-back `CreateLibrary` leaves a library nobody asked for, which is untidy and fixable by dropping it; nothing is lost.

That keeps every existing catalog-rebuild script working — they call these operators at the top level, where the implicit transaction is the only one open — while closing the one case that destroys data.

## Alternatives, and why they are not recommended

- **Make library DDL fully transactional**: defer the file and folder work to the commit, and keep enough state to undo the memory half. It is the right answer in principle and a large change: four operators plus the requisite and file operators, a new deferral mechanism beside the two the catalog device already has for device stops and remote session closes, and a question about what two concurrent sessions creating the same library should see. Out of proportion to a case nobody has reported.
- **Refuse all four inside an explicit transaction.** Simpler rule, but `CreateLibrary` and `SetLibraryDescriptor` are what an installer or a rebuild script calls, and some of those scripts may wrap their work in a transaction deliberately. Breaking them to protect against untidiness is the wrong trade.
- **Do nothing at all.** The folder deletion stays a way to lose an application's library by rolling back, and the next person to look at rollback behaviour finds the same thing and files it again. The second cost is why this is a ticket rather than a shrug.

## If we do nothing

Nothing breaks today — no reported case, and `tickets/plan/debt-library-upgrades-coverage` is about coverage of these files rather than about this question, so it does not settle it either. The general rollback test (`debt-rollback-leaves-catalog-as-it-was`) is written to leave the library dimension out and to point at this ticket in a comment, so the gap is recorded in the code whatever is decided here.

## How reversible the call is

Very. Option 1 is a document paragraph; option 2 is one check and one error code, removable without trace if it turns out to be in someone's way. Only the full-transactionality alternative would be hard to walk back.
