description: A program can ask the server to compile a statement once and then run it as many times as it likes. For a statement that creates something, running it a second time does not create anything new — it puts back the same thing the first run made, with any changes made to it since. We need a decision on what the second run should do.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerPlan.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Compiling/Compiler.cs
repro: verified
----
**Blocked because the architecture is silent:** nothing in `docs/architecture.md` or `docs/modernization-plan.md` says whether a compiled statement that creates a catalog object may be executed more than once. Accepting or editing the proposal below unblocks it.

## What happens today

A caller holds a compiled statement through `IServerProcess.PrepareStatement` and executes it as often as it likes — the published interface in `src/engine/DAE/CLI.cs` offers exactly that, and says the convenience method `Execute` is "equivalent to preparing the statement, executing the prepared plan, and unpreparing the plan ... provided for convenience if multiple execution of the prepared statement is not necessary".

For a statement that creates a catalog object, the compiled form does not hold the statement. It holds **the object the compiler already built**: `CompileCreateDeviceStatement` constructs the `Schema.Device` while compiling (`src/engine/DAE/Compiling/Compiler.cs:9372`) and executing the node is just `program.CatalogDeviceSession.CreateDevice(_newDevice)` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:781`). Every `Create…Node` in that file is built the same way. So the second execution registers the identical object — same identity, same object ID, and any change made to it in between.

Measured on 2026-11-23 against an in-process server: prepare `create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" }`, execute it, then `alter device D alter class { alter "MaxRowCount" = "200" }`, then `drop device D`, then execute the prepared plan again — the device comes back with a row limit of 200, from a statement that says 100, and no error is raised. It still does with the two fixes for the sibling defects in place (`bug-remembered-statement-applies-a-changed-setting` and `bug-recreated-device-comes-back-as-the-one-that-was-dropped`), because neither touches this path: the first makes alter statements own their attributes, and the second throws away *cached* plans when a device is dropped, which a plan the caller is holding is not.

Without a drop in between the second execution fails anyway, because the object already exists. So the reachable case is precisely: prepare a create statement, drop what it made, execute it again.

How often that happens in practice is unmeasured. Nothing in this repository's own libraries, samples or tests does it; it is reachable by any program written against the published interface, and the interface invites it.

## Proposed answer, for a maintainer to accept or edit

**A compiled statement that creates a catalog object is compiled afresh for each execution.** Keep re-execution working and make it mean what the statement says: the plan keeps its source statement, and executing it builds a new catalog object rather than re-registering the one the compiler made. This is also what the *cached*-plan route will do once the sibling ticket lands — the drop throws the cached plan away and the next run compiles again — so a prepared plan behaving the same way makes the two routes agree instead of differing by which one the caller happened to use.

Cost: the create nodes stop being the compile-time-built objects they are today, which is a change to how `Create…Node` works across `DDLNodes.cs` and the create paths of `Compiler.cs`, and compilation moves from prepare time to execute time for these statements. Reversible: it changes no published shape, no error code and no file format, so it can be revisited without an application noticing.

### Alternatives considered and rejected

- **Refuse the second execution.** Have a create plan raise an error when executed again. Small — one flag on the plan and one new error code — and an error plainly beats a silently wrong result. Rejected as the recommendation because it takes away something the published interface offers, which is a compatibility surface (a new error code, and a call that succeeds today), and because it leaves the two routes disagreeing: the same text through `Execute` would recompile and succeed while a prepared plan would fail.
- **Leave it and write the limitation down.** A `NOTE:` at `CreateDeviceNode` and a line in the published interface's remarks saying a create plan is single-shot. Cheapest, and it is what the previous release did. Rejected as the recommendation because the failure is silent and lands in the catalog: a re-created object comes back carrying settings and an object ID that nothing in the statement asked for. It is a reasonable answer if the appetite for the change above is not there.

### If we do nothing

The two sibling tickets close every route a client or the Dataphor IDE actually travels (both of those issue DDL one statement at a time through `Execute`). What stays open is a program that deliberately prepares a create statement and re-executes it after dropping what it made, which gets a silently wrong object back. No test covers it, so nothing will tell us if it gets worse.

## How any answer would be confirmed

The reproduction above: prepare, execute, alter, drop, execute again, and assert the device's live row limit is the one the statement states — or, for the "refuse" answer, assert the error. It needs `IServerProcess.PrepareStatement` rather than `ExecuteScript`, no database server, and ran in under 100 ms. `tests/DAE.Server.Tests/DeviceClassAttributeRollbackTest.cs` has the helper that reads a memory device's live row limit.
