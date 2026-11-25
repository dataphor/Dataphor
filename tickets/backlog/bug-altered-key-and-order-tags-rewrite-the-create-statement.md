description: A statement that creates a table can give named settings to the table's keys and its sort orders. Changing one of those settings afterwards quietly rewrote the statement itself, so a program that runs the same statement a second time creates the table with the changed setting instead of the one the statement is written with.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/TableVar.cs, tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: It is the same one-line change at two more sites, but the two sites are also where the compiler rewrites a deprecated tag spelling in place, so copying there changes which tree that rewrite lands in and makes the deprecation warning repeat on every compile of the same statement rather than fire once; a maintainer may want the warning change weighed, or may judge a tagged key that is later altered and whose create statement is then run again too rare to spend a release on.
----
## What goes wrong

A D4 table's *keys* and *sort orders* can carry tags - named string settings - written with the statement that creates the table:

```
create table T { ID : Integer, key { ID } tags { Frontend.Title = "Own" } };
create table U { ID : Integer, key { ID }, order { ID } tags { Frontend.Title = "Own" } };
```

and the tags can be changed later:

```
alter table T { alter key { ID } alter tags { alter Frontend.Title = "Altered" } };
```

The compiler hands the live key (or order) the *statement's own* tag collection rather than a copy, so the object and the statement share one collection. An alteration edits that collection in place, which writes the new value into the create statement's parse tree as well. The statement then no longer says what it was written with, and anything that runs it again builds the key with the changed value.

Measured on 2026-11-25 against an in-process engine, through a statement plan the caller prepares and executes twice - the route a client, and the Dataphor IDE, take:

```
plan = PrepareStatement("create table KeyTagProbe { ID : Integer, key { ID } tags { Frontend.Title = \"Own\" } };")
plan.Execute()
Execute("alter table KeyTagProbe { alter key { ID } alter tags { alter Frontend.Title = \"Altered\" } };")
Execute("drop table KeyTagProbe;")
plan.Execute()
```

The re-created table's key carries `Frontend.Title = "Altered"`, and the create script the catalog emits for the table reads `key { .ID } tags { Frontend.Title = "Altered" }`. It should read `"Own"` in both places. The same probe over `order { ID } tags { … }`, altered with `alter order { ID } alter tags { … }`, gives the same result.

## Where it is

Two assignments, both in `src/engine/DAE/Compiling/Compiler.cs`:

| Line | Site | Reached from |
|---|---|---|
| 3188 | `new Schema.Key(plan.GetObjectID(key.MetaData), key.MetaData)` in `CompileKeyDefinition` | a `key { … } tags { … }` in a create table or view, and `alter table … create key` |
| 3425 | `new Schema.Order(plan.GetObjectID(metaData), metaData)` in `CompileOrderColumnDefinitions` | an `order { … } tags { … }` in a create table or view, and `alter table … create order` |

The sharing is expressed in `Schema.Key(int, MetaData)` and `Schema.Order(int, MetaData)` (`src/engine/DAE/Schema/TableVar.cs:1467`, `:1154`), which assign what they are given straight to `MetaData`. The alteration that lands in the shared collection is `AlterNode.AlterMetaData`, reached for keys and orders from `AlterKeys` and `AlterOrders` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1177`, `:1224`).

Present since the initial open-source commit; not a port consequence.

## How this relates to what has already landed

Three tickets have now fixed the same defect class in three other places, and each one records why the fix sits where it does rather than in a copying property setter:

- `bug-remembered-statement-applies-a-changed-setting` - a *class definition* (the .NET class and attributes behind an object such as a storage device), fixed in six cloning property setters.
- `bug-altered-tags-rewrite-the-create-statement` - the *tags of a catalog object* (a table, an operator, a constraint, a reference, a role, a server, a device), fixed at the eleven compiler create sites that assign them, and the one that documented the invariant: a catalog object owns its metadata and never shares it with the statement that created it. The invariant is written down at `Schema.Object.MetaData` and in `docs/architecture/catalog-and-store.md`.
- `bug-view-tags-lose-to-tags-inherited-from-the-source` - a view's own tags against the ones it inherits.

Keys and orders were out of the second ticket's scope: its measurement was taken with exactly its eleven lines changed and nothing else, so that the 416 checked-in derived forms could be shown not to move, and keys and orders feed form derivation. This ticket is the remainder, found while implementing that one. There is no new reasoning to settle about *where* the invariant lives - these two sites hand a statement's collection to an object, which is the shape the second ticket's root cause names.

## What whoever works it should know

- **The two sites are also where the compiler rewrites a deprecated tag in place.** `ProcessIsClusteredTag`, `GetEnforced` and `GetIsSparse` (`Compiler.cs:3173`, `:3143`, `:3158`) replace `Storage.IsClustered`, `Storage.Enforced` and `Storage.IsSparse` with their `DAE.*` spellings by removing and adding tags in the collection they are handed, and warn once about each. Today that collection is the statement's, so the rewrite edits the statement and the warning does not fire again on a second compile of the same statement. Once the sites copy, the statement keeps the old spelling, the object still ends up with the new one - so nothing an application reads out of the catalog changes - but the deprecation warning fires on every compile. That is the one behaviour change to weigh, and it needs a decision recorded one way or the other.
- **The 416 golden derived forms are the regression check.** `FrontendDerivationGoldenTest.EveryDerivedFormMatchesItsGolden` covers every page type of every derivable surface in the shipped libraries. A key's or an order's tags reach derivation, so run it and read any difference rather than regenerating the goldens.
- **A third site of the same shape, unmeasured.** `AdornNode.DetermineDataType` builds a reference from a `references { … }` clause of an `adorn` expression and hands it the clause's own tag collection (`src/engine/DAE/Runtime.Instructions/AdornNode.cs:389`, through `Schema.ReferenceBase(int, string, MetaData)` at `src/engine/DAE/Schema/Reference.cs:32`); an `alter reference` clause in the same `adorn` then alters that reference's tags in place a few lines later. Nobody has measured a consequence - an `adorn` that both creates a reference and alters it applies the same change on every compile, so the edited tree may well mean the same thing - so it is recorded here rather than claimed. Decide whether it is worth a measurement while the other two sites are open; it is the same one-line change if it is.
- **The reproduction has a home.** `tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs` is the fixture for this behaviour; its one case is the catalog-object arm. The key and the order arms belong beside it, and its remarks explain why a prepared plan is the layer that shows the defect and a loop is not.

## Expected behaviour

A statement that creates a table is unchanged by anything that happens to the table afterwards. Running that same statement again - from a prepared plan, from the plan cache, or from a script - creates the table with the key tags and order tags the statement is written with, and the create script the catalog emits for the re-created table says the same.
