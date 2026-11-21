description: Dataphor will accept queries written in an SQL dialect instead of its own query language, and nothing in the test suite exercises that translator at all, so a change to it would break the applications that use it without any test noticing.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.RealSQL/RealSQLParser.cs, src/engine/DAE/Language.RealSQL/RealSQLCompiler.cs, src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs, src/engine/DAE/Server/ServerProcess.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/
tradeoffs: RealSQL is a secondary way into the engine and no shipped library is known to depend on it, so a maintainer may rank a wholly untested 1,423-line translator below the subsystems every query passes through - but it is reachable by any application through a granted operator, which is why this was filed rather than left in the report.
----
## The area

**RealSQL**, `src/engine/DAE/Language.RealSQL/`. Measured **0.0 % line coverage of 1,423 coverable lines** on the full three-part run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`). Not one line ran. It is the worst hole in the engine by uncovered line count and the only area besides the SQL emitter at exactly zero.

| File | Line coverage | Coverable |
|---|---:|---:|
| `src/engine/DAE/Language.RealSQL/RealSQLCompiler.cs` | 0.0 % | 771 |
| `src/engine/DAE/Language.RealSQL/RealSQLParser.cs` | 0.0 % | 632 |
| `src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs` | 0.0 % | 20 |

An earlier short run reported this area at 2,846 lines. That figure double-counted: the collector lists each line twice, once under its class and once under its method. 1,423 is the real coverable count.

## Why this is live code and not a leftover

Worth stating plainly, because a subsystem at exactly zero usually turns out to be dead. This one is not, and it is reachable by applications rather than only by internal callers:

- `SetLanguage('RealSQL')` is a D4 operator created in `src/engine/DAE/Schema/SystemCatalog.d4` and **granted to role `User`**, so any application can switch its own process into the dialect.
- `ServerProcess` keeps a RealSQL parser and compiler per process and routes prepare, evaluate and execute through them whenever the process language is RealSQL.
- Dataphoria's SQL editor switches the language this way when a document's designer is `SQL`.
- A shipped Frontend library operator sets the process language to RealSQL in `libraries/Frontend/Source/Operators/Operators.cs`.

So the dialect is part of the compatibility surface: its accepted grammar and the D4 it translates into are both things an application can depend on.

## What a coverage script should exercise

A D4 script, run through the in-process harness like the rest. The shape is `SetLanguage('RealSQL');` then SQL statements, then `SetLanguage('D4');` to return - the switch is per process, so a script that forgets to switch back leaves the process in the dialect for whatever follows it in the same batch.

The grammar to cover, read off `RealSQLParser.cs` and `RealSQLKeywords.cs`:

- **The four statements**: `select`, `insert`, `update`, `delete`. Each has its own parser entry point and its own translation in `RealSQLCompiler.cs`, and `insert` has two forms (a values list and a sub-select).
- **The select clauses**: `from`, `where`, `group by`, `having`, `order by`, with `as` aliases on both columns and tables, and qualified column names (`T.C`).
- **The join forms**: `inner`, `left outer`, `right outer`, `cross`, each with its `on` condition. `RealSQLCompiler.cs` translates the join type explicitly and raises `UnknownJoinType` for anything it does not know.
- **The table operators**: `union`, `intersect`, `minus`, `except`.
- **The expression grammar**, which is a separate precedence ladder from D4's: `or`, `xor`, `and`, `not`, the comparisons, `like`, `between`, `matches`, `in`, `exists`, `is null`, the additive and multiplicative operators, `div` and `mod`, and `case`/`when`/`then`.
- **Its own errors.** The compiler raises `LanguageException` codes that only this path produces: `TableAliasRequired` (a derived table with no alias), `NamedExpressionNotFound` and `NamedExpressionNotFoundByExpression` (an `order by` or `having` naming a column the select does not produce), `UnknownJoinType`, `UnknownStatementClass`, `UnknownExpressionClass`, `UnknownInstruction`. Error codes are a compatibility surface, so a script that pins the ones an application could actually provoke - the alias and named-expression ones - is worth more than its length.

The assertions should be about results, not about the translation: run the same question as SQL and as D4 over the same test tables and require the same rows back. That checks the translation without pinning the D4 text it produces, which is an internal detail and would make the test brittle.

## Interactions

- Every statement in the script runs through `Compiler.Compile` afterwards, so the script also exercises the compiler - but the compiler is already at 80.9 %, and that is not what this is for. Keep the SQL surface, not the D4 semantics, as the subject.
- The nine scripts the manifest records as asserting nothing (`docs/coverage-report.md`) do not include any RealSQL script; there is nothing here to replace, the coverage simply never existed.
