description: The engine code that turns a query into SQL text, which every SQL-database back end depends on, is completely untested - no test in the suite runs a single line of it, so a mistake there would reach a customer's database server before anything noticed.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.SQL/Emitter.cs, src/engine/DAE/Language.SQL/Language.cs, src/engine/DAE/Language.SQL/Keywords.cs, libraries/SQLDevice/Source/SQLDevice.cs, libraries/MSSQLDevice/Source/Language.TSQL/Emitter.cs
tradeoffs: The tests that would exercise this end to end need a running SQL Server, which the suite deliberately does not require, so what is practical here is an emitter-level test that pins generated text - and a maintainer may reasonably prefer to wait for a database the suite can start (a container, a CI service) over tests that assert on strings.
----
## The area

**SQL translation (engine)**, `src/engine/DAE/Language.SQL/`. Measured **0.0 % line coverage of 1,031 coverable lines** on the full three-part run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`). Not one line ran.

| File | Line coverage | Coverable |
|---|---:|---:|
| `src/engine/DAE/Language.SQL/Emitter.cs` | 0.0 % | 550 |
| `src/engine/DAE/Language.SQL/Language.cs` | 0.0 % | 461 |
| `src/engine/DAE/Language.SQL/Keywords.cs` | 0.0 % | 20 |

`Language.cs` is the SQL syntax tree - select, insert, update, delete, the clauses, the expression forms - and `Emitter.cs` (`SQLTextEmitter`) turns a tree into the text sent to a database server.

## Why it matters, and why nothing reaches it

This is the last step before a customer's database: whatever a device decides to ask, this code writes. Every SQL back end goes through it. `libraries/SQLDevice` builds the tree and calls `Device.Emitter.Emit(...)`, and the MSSQL and PostgreSQL devices subclass both the tree and the emitter to add their own dialects (`libraries/MSSQLDevice/Source/Language.TSQL/`, `libraries/PGSQLDevice/Source/Language.PGSQL/`). The report's assembly table shows the same hole one level up: `Alphora.Dataphor.DAE.Device.SQL` sits at 0.2 % of 6,143 lines.

Nothing reaches it because the emitter is called while a command is being built for execution against a device connection - `SQLDevice.cs` calls `Emit` at the points where it sets `command.Statement` - and the suite has no database server to execute against. The three `Transaction.Atomicity.TestMSSQL*` coverage scripts are the tests that would have reached it, and the manifest records all three as known failures for exactly this reason. This pass found no D4-reachable path that produces SQL text without a connection: `Diagnostics.ShowPlan` writes the plan node tree, not the device statement.

## What a test should exercise

The usual preference in this repository is a D4 script through the in-process harness, and it does not apply here: there is no engine-only path to this code. The lowest layer that shows the behaviour is the emitter itself, which is a pure function from a syntax tree to a string and needs no server, no device and no catalog. So: a characterization test that builds trees out of `Language.cs` and asserts the text `SQLTextEmitter` produces.

What to cover, worst-first by what a wrong answer would do to a real database:

- **Identifier and literal quoting.** How a column or table name is delimited, and how a string literal containing a quote is escaped. This is the part where a mistake is not a syntax error but a different query, or an injection.
- **The statement forms**: `select` with its clause order, `insert` in both the values and sub-select forms, `update` with its assignment list, `delete`.
- **Clause emission**: `from` with joins of each kind, `where`, `group by`, `having`, `order by` with direction, and the alias forms.
- **Expression emission and parenthesisation.** The emitter has to add parentheses where SQL's precedence differs from the tree's shape; this is the other place a mistake silently changes meaning rather than failing.
- **The null, `case`, `exists` and sub-select forms**, and the batch separator and statement terminator.

Assert the emitted text, and say in the test why: there is no reference runtime for this, so the text a working system produces *is* the specification, and an expected value here must never be edited to make a later change pass.

## Interactions

- **A dialect subclass is not this ticket.** The MSSQL and PostgreSQL emitters override pieces of this one. Cover the base emitter's behaviour; a dialect's own overrides belong with whatever ticket makes those devices testable.
- **This does not replace an integration test.** Pinned text says the emitter builds the string it always built. It says nothing about whether a real SQL Server accepts it. If a database the suite can start ever appears, the end-to-end case is still worth having, and this test is then the fast layer underneath it.
- **Device connections (engine)** is a neighbouring thin area (33.1 %) whose zero-coverage files are the MSSQL connection and command classes. That one genuinely cannot be covered without a server, so the report leaves it unticketed; do not fold it in here.
