# Compiler Defines

## Summary

Dataphor carries a set of named *behaviour switches*: C# preprocessor symbols that pick between two implementations of something, or turn a diagnostic on. There are two kinds, and this page is the index to the first.

- **Per-file `#define`s** — a `#define` at the top of one source file, in effect for that file only. The 55 names below are of this kind: the 51 the inherited index listed, and four it never did. Several appear in more than one file, because the code they guard does.
- **Project-level constants** — each project file sets `DefineConstants` outright, so the symbol is in effect for every file the project compiles. The engine's set is `USENATIVE;NATIVEROW;USEOBJECTVALIDATE;NILPROPOGATION`, with `DEBUG;TRACE` added in Debug; the Frontend server's is `USEINCLUDETAG`. The paragraph *Non-default compiler constants to preserve* in [Appendix B of the modernization plan](../modernization-plan.md#appendix-b-inventory) records that set, which 3.1 configuration each constant came from, and why the 4.0 build uses the set it does.

`NILPROPOGATION` is of both kinds: it is an engine project constant *and* a `#define` in the engine files that test it. The misspelling is deliberate here — it is the identifier in the source, and renaming it would change behaviour.

Two things to know before changing any of these:

- **They are load-bearing.** Several select between two different query results, not between two ways of reaching the same one; `NILPROPOGATION` decides whether `1 + nil` is `nil` or an error. Turning one on or off is a change to D4 semantics, which is a compatibility surface — see *Compatibility surfaces* under [Rules that are easy to break](../../AGENTS.md#rules-that-are-easy-to-break) in `AGENTS.md`.
- **A per-file `#define` reaches only its own file.** Where a switch is tested in several files, turning it on means adding the line to each of them. In at least one case below, the switch's commented-out line and the code that tests it are in different assemblies, so uncommenting the line alone would change nothing.

## The switches

**The *State* column is measured from the source tree, not carried over from the inherited index.** The file this page replaces (`CompilerDefines.txt`, in the repository's history) marked 33 of the 51 switches as enabled: 26 of those have an active `#define` in the tree, 6 have no `#define` line at all and 1 appears in no source file. No commit in this repository's history ever added or removed a `#define` for any of those seven, so on them the inherited file was recording an intention rather than the code. Its disabled list of 18 agrees with the tree in effect: 15 are commented out, 2 have no define line and 1 is gone.

The four states:

- **on** — an active `#define` line is present.
- **off** — the `#define` line is present but commented out: the switch exists and is off.
- **not defined** — no `#define` line anywhere, so the switch is off; the name is still tested in the code, and *Where* says where.
- **not in the tree** — the name appears in no source file. The switch no longer exists.

The order is the inherited file's, so that this page can be read against it.

**The measurement has a date.** *State* and *Where* were measured on 2026-11-26, by looking for `#define <name>` and `#if <name>` lines in every `.cs` file under `src/`, `libraries/`, `samples/`, `tests/` and `tools/`. The names are case-sensitive, as the C# preprocessor is, so a mixed-case one (`UseElaborable`, `UseFileStreamProvider`, `UseReferenceDerivation`) has to be searched as the source spells it and not as the inherited index does. Two kinds of pending work will move rows: `build-globbing-cleanup` deletes the source files no project compiles, `src/client/DAE.Client/View-263.cs` among them, and the port deletes a dead switch when it is otherwise editing the file that holds it. Re-measure rather than trust the columns once this date is old.

| Define | State | Where | What it does |
|---|---|---|---|
| `ALTERNATIVEOPERATORPRECEDENCE` | on | `src/engine/DAE/Language.D4/Parser.cs` | Selects the newer operator precedence: `not` and `exists` bind after comparison, and `in`, `like`, `matches`, `between`, `is` and `as` before it. |
| `ALLOWARBITRARYAGGREGATEEXPRESSIONS` | on | `src/engine/DAE/Language.D4` (`Parser.cs`, `Language.cs`) | Allows arbitrary expressions in the `by` and `compute` clauses of an aggregate expression. |
| `CHECKCLASSDEPENDENCY` | off | `src/engine/DAE/Compiling/Plan.cs` | Checks class dependencies. Guards a `[Conditional]` method rather than an `#if` block. |
| `DATAVIEWBOOKMARKS` | not defined | tested only in `src/client/DAE.Client/View-263.cs`, which `DAE.Client.csproj` does not compile | Would enable the bookmark capabilities of the `DataView`. |
| `DISALLOWAMBIGUOUSNAMES` | off | `src/engine/DAE/Compiling/Compiler.cs` | Stops the compiler allowing ambiguous names to be defined. |
| `DISALLOWMANYTOMANYOUTERJOINS` | off | `src/engine/DAE/Runtime.Instructions/JoinNodes.cs` | Disables support for many-to-many outer joins. |
| `ENFORCERESTRICTIONPREDICATE` | off | `src/engine/DAE/Runtime.Instructions/RestrictNode.cs` | Performs restriction predicate checking by default. |
| `INVERTEXCEPTIONDESCRIPTIONS` | on | `src/engine/Base/ExceptionUtility.cs` | Displays error messages beginning with the innermost exception. |
| `LOADFROMLIBRARIES` | on | `src/engine/DAE` (2 files), `src/engine/Server` (3 files) | Loads library assemblies in place instead of copying them into the `bin` directory. |
| `LOGDDLINSTRUCTIONS` | on | `src/engine/DAE` (2 files), `src/engine/Server` (3 files) | Has the catalog log DDL as part of the transaction. The inherited index says it should not be turned off. |
| `LOGFILECACHEEVENTS` | on | `src/protocol/Client` (9 files) | Logs each file the client downloads into its local library-file cache, and why. No description in the inherited index. |
| `LOGCACHEEVENTS` | off | `src/engine/DAE/Server/Engine.cs` | Logs catalog-cache events. No description in the inherited index. The code that tests it is in `src/engine/Server/Server` and `src/protocol/Client`, not in the file holding the commented-out line. |
| `NILPROPOGATION` | on | `src/engine/DAE` (22 files), `src/engine/Server` (2 files), `libraries/System.Internet`; also an engine project constant | Enables nil propagation, so that `1 + nil` is `nil` rather than an error. |
| `ORDERREFERENCESBYPRIORITYONLY` | on | `libraries/Frontend/Source/Elaboration/Elaboration.cs` | Orders references by priority in derived documents. |
| `RELYONCURSORFLAGSAFTERUPDATE` | not defined | tested in `src/client/DAE.Client/DataSet.cs`, and in `View-263.cs`, which is not compiled | Would have the client trust the cursor's beginning- and end-of-file flags after an update instead of navigating to establish them. |
| `REMOVESUPERKEYS` | on | `src/engine/DAE/Runtime.Instructions/TableNode.cs` | Enables superkey removal in the compiler's key inference. |
| `REPORTNARROWINGCONVERSIONWARNINGS` | not defined | tested in `src/engine/DAE/Compiling/Compiler.cs` | Would have the compiler warn when an implicit narrowing conversion is used. |
| `REQUIRECASEMATCHONRESOLVE` | on | `src/engine/DAE/Device.Catalog/CatalogStore.cs` | Requires a case match when resolving identifiers. Introduced for an internal proposal, numbered 26889 in the inherited index. |
| `REQUIRESAMEDATATYPESFORREFERENCECOLUMNS` | not defined | tested in `src/engine/DAE/Compiling/Compiler.cs` | Would disallow implicit conversions in compiler-generated reference constraint expressions. |
| `RESPECTREADONLY` | not defined | tested in `src/engine/Server` (2 files), `libraries/Frontend/Source/Device/FrontendDevice.cs` | Would have the library and document subsystems respect the read-only flag on files. |
| `SKIPBETACHECK` | not in the tree | — | Skipped a beta expiry check. Neither the switch nor the check it guarded is in the source. |
| `SQLSTORETIMING` | off | define line in `src/engine/DAE/Store` (`SQLStoreConnection.cs`, `SQLStoreCursor.cs`); also tested in `SQLStore.cs`, which has no line of its own | Tracks timings on the SQL catalog stores, readable through the `GetStoreCounters()` system operator. `SQLStore.cs` carries a commented-out `//#define STORETIMING` instead — the wrong name — so uncommenting the two real lines leaves that file's timing blocks dark. |
| `TRACKCALLDEPTH` | off | `src/engine/DAE/Runtime.Instructions` (`PlanNode.cs`, `TableNode.cs`) | Enables call-depth tracking. |
| `USEADOCONNECTION` | off | `libraries/MSSQLDevice` (4 files) | Makes `ADOConnection` the SQL Server device's default connectivity implementation. |
| `USEASYNCSTOPPROCESS` | not defined | tested in `src/client/DAE.Client/DAEDataSet.cs`, and in `View-263.cs`, which is not compiled | Would issue the stop-process call asynchronously. |
| `USECLEANUPNODES` | off | `src/engine/DAE/Runtime.Instructions/InstructionNode.cs` | Part of cleanup processing. Off because cleanup is done by `InstructionNodeBase.CleanupOperand` instead. |
| `USECONNECTIONPOOLING` | on | `libraries/MSSQLDevice` (4 files) | Enables connection pooling in the SQL Server device. |
| `USECONVERSIONPATHCACHE` | on | `src/engine/DAE/Compiling/Compiler.cs` | Enables the conversion path cache. |
| `USEDATATYPESINNATIVEROW` | on | `src/engine/DAE/Runtime.Data` (3 files) | Has the native row track the data type of each value it holds. |
| `UseElaborable` | on | `src/engine/DAE/Runtime.Instructions` (13 files) | Makes reference inference conditional on the plan's cursor carrying the `Elaborable` capability, which a session gets from its `ShouldElaborate` setting; nested inside `#if UseReferenceDerivation` at every site. The inherited index spells the name in capitals, the source does not, and the preprocessor is case-sensitive. |
| `UseFileStreamProvider` | off | `src/engine/DAE/Streams/ServerStreamManager.cs` | Makes the server stream manager's default provider a `FileStreamProvider` rather than a `MemoryStreamProvider`. No description in the inherited index. |
| `USEINCLUDENILSWITHBROWSE` | on | `src/engine/DAE/Runtime.Instructions/OrderNodes.cs` | Enables the `include nil` clause in the `order by` statement. |
| `USEINTERNALID` | on | `src/engine/DAE/Runtime.Data/NativeTable.cs` | Lets the internal index store duplicates of logical values for non-unique indexes. |
| `USENAMEDROWVARIABLES` | on | `src/engine/DAE` (10 files), `libraries/SQLDevice/Source/SQLOperators.cs` | Uses named variables in row contexts such as constraint evaluation, rather than prefix-qualifying the column names of unnamed variables. See the notes below. |
| `USENATIVECONCURRENCYCOMPARE` | on | `src/engine/DAE/Runtime.Instructions/TableNode.cs` | Enables native object comparison when performing concurrency checks. The inherited index marks its own description as uncertain. |
| `USEOLEDBCONNECTION` | off | `libraries/MSSQLDevice` (4 files) | Makes `OLEDBConnection` the SQL Server device's default connectivity implementation. |
| `USEOPERATORRESOLUTIONCACHE` | on | `src/engine/DAE/Compiling/Compiler.cs` | Enables the operator resolution cache. |
| `USEPROCESSDISPOSED` | off | `src/engine/DAE` (`Compiling/Plan.cs`, `Runtime/Program.cs`) | Has the plan and the program listen to the process-disposed event. |
| `USEPROCESSUNBIND` | not defined | tested in `src/engine/DAE` (`Compiling/Plan.cs`, `Runtime/Program.cs`) | Would enable process unbinding. |
| `UseReaderWriterLock` | not in the tree | — | Was to use the .NET reader/writer lock rather than the Dataphor semaphore. The name appears in no source file. |
| `UseReferenceDerivation` | on | `src/engine/DAE/Runtime.Instructions` (19 files) | Enables reference inference in the compiler. |
| `USEROOTEDIDENTIFIERSINKEYEXPRESSIONS` | off | `src/engine/DAE/Compiling/Compiler.cs` | Enables rooted identifiers in key expressions. See the notes below. |
| `USESPINLOCK` | on | `src/protocol/Client` (9 files) | Guards the client's catalog cache with a spin lock rather than a monitor. No description in the inherited index. |
| `USESQLCONNECTION` | on | `src/engine/Store.SQLite` (2 files), `src/engine/Store.MSSQL` (3 files) | Uses an `SQLConnection` descendent for connectivity in the SQL store. |
| `USESQLOLEDB` | on | `libraries/MSSQLDevice` (4 files) | Uses the SQLOLEDB provider when the SQL Server device connects over ADO. |
| `USESTRICTNAMERESOLUTIONCACHECLEARING` | off | `src/engine/DAE/Device.Catalog/NameResolutionCache.cs` | Clears the name resolution cache only for potentially affected names instead of clearing it whole. See the notes below. |
| `USETAGNAMECACHE` | off | `src/engine/DAE/Language.D4/MetaData.cs` | Shares one string reference for every tag of a given name, to reduce the memory tags use. See the notes below. |
| `USETYPEDLIST` | not defined | tested in `src/engine/DAE` (24 files), `src/client/DAE.Client` (2 files), `libraries/Frontend` (2 files) and three other files | Would use the `TypedList` descendent instead of `BaseList<T>` and the related generics for basic list support. |
| `USEUNIFIEDJOINKEYINFERENCE` | on | `src/engine/DAE/Runtime.Instructions/JoinNodes.cs` | Uses one key-inference algorithm for every kind of join; see `InferJoinKeys`. |
| `USEUPDATEVALUEFLAGS` | on | `src/engine/DAE/Runtime.Instructions/TableNodes.cs` | Passes a per-column value-flag array with an update, built from `UpdateColumnNames`, so that a device is told which columns changed. No description in the inherited index. |
| `WRAPRUNTIMEEXCEPTIONS` | on | `src/engine/DAE/Runtime.Instructions` (`PlanNode.cs`, `TableNode.cs`) | Wraps a runtime exception that does not descend from the Dataphor exception in one that does. |

## Switches the inherited index did not list

Measured the same way. They are per-file `#define`s like the rest; the inherited index simply never carried a row for them.

| Define | State | Where | What it does |
|---|---|---|---|
| `PROCESSSTREAMSOWNED` | on | `src/engine/DAE/Server/ServerProcess.cs` | Has a process deallocate every stream it allocated when it goes away. |
| `STORETIMING` | off | `src/engine/DAE/Store/SQLStore.cs` | Nothing: no `#if` anywhere tests this name. The line was meant to read `SQLSTORETIMING`; see that row above. |
| `USEINCLUSIVEMULTIPLEX` | off | `libraries/SQLDevice/Source/SQLDeviceCursor.cs` | Repositions a multiplexed cursor with an inclusive rather than an exclusive restriction, where several cursors share one connection. |
| `USESTRUCTTAG` | on | `src/engine/DAE/Language.D4/MetaData.cs` | Declares a metadata `Tag` as a struct; the other branch of the same file declares it as a class. Both branches read `USETAGNAMECACHE`. |

One further `#define` in the tree is not a behaviour switch at all: `src/engine/Base/IndexList.cs` defines `DEBUG` at the top of itself, so that file's consistency assertions are compiled into Release as well as Debug. No other file does that.

## Notes on four of the switches

The inherited index carried a longer comment on four entries. They are the original author's, kept because each records a judgement that is written down nowhere else.

- **`USENAMEDROWVARIABLES`** — believed to make `USEROOTEDIDENTIFIERSINKEYEXPRESSIONS` unnecessary.
- **`USEROOTEDIDENTIFIERSINKEYEXPRESSIONS`** — introduced to address a specific problem with ambiguous names. The note says that problem is more elegantly resolved by `USENAMEDROWVARIABLES`, but that the author could not be certain, not knowing exactly which problem the switch had fixed.
- **`USESTRICTNAMERESOLUTIONCACHECLEARING`** — introduced, then left off, because with strict clearing a catalog header lookup error occurs. Rather than fix that, the switch was kept off: strict clearing had no effect on cache hits either way, which led the author to suspect a larger problem in how the name resolution cache is used.
- **`USETAGNAMECACHE`** — the idea was to reduce the memory tags use by sharing one string reference per tag name. Early measurements showed the saving was not significant, even on a large customer catalog, so it was left off.

## Where the project-level constants are set

Each project file sets `DefineConstants` outright rather than appending to it, so that the SDK's own default `TRACE` is not added; `Directory.Build.props` records why, and `src/engine/DAE/DAE.csproj` records the reasoning for the engine's set. *Converting another project* under [Build and test](../../AGENTS.md#build-and-test) in `AGENTS.md` gives the rule a newly converted project follows.
