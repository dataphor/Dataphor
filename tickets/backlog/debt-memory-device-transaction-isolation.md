description: The in-memory device does not isolate transactions: a second session's update to a row another session's open transaction has changed is not blocked, so the `Transaction.Isolation.Run` coverage script fails its lost-update check. Low priority; applications keep shared data on SQL Server or PostgreSQL.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Runtime.Data/NativeTable.cs, src/engine/DAE/Runtime.Data/NativeRowTree.cs, tests/DAE.Server.Tests/coverage-manifest.json, docs/developers-guide/devices.md
prereq: bug-memory-device-concurrent-access
----
## Decision (owner, 2026-11-18)

The owner decided that the memory device does not provide transaction isolation for now (`bug-memory-device-concurrent-access`, question 1), and asked for this low-priority ticket to keep the gap on record. `bug-memory-device-concurrent-access` adds the per-table reader-writer lock (statement-level safety only), records the limit in a `NOTE:` in `MemoryDevice.cs`, and points the `Transaction.Isolation.Run` manifest entry at this ticket.

## What is missing

`Transaction.Isolation.Run` checks that the engine's isolation levels hold: an uncommitted transaction's changes either block other transactions or are hidden from them. On the memory device the second process's update goes through (`Lost Update (Browse, Browse): LostUpdate did not behave properly`). `docs/developers-guide/devices.md` promises the memory device only "minimal concurrency protection", and 3.1 behaved the same way.

## Direction, when this is promoted

- Row (or key-range) locks held for the life of a transaction, honouring the isolation levels the engine already defines (`Browse`, `CursorStability`, `Isolated`), on top of the per-table lock from `bug-memory-device-concurrent-access`. Alternatively, versioned rows so readers see a snapshot.
- The owner's own database engine `quereus` (a sibling checkout, `../quereus`) may be a source of design ideas for the isolation scheme.
- Deadlock detection or a lock timeout is part of the design.
- Done means `Transaction.Isolation.Run` flips to `pass` and the docs say what the memory device guarantees.
