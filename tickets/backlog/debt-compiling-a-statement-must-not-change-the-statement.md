description: Compiling a statement sometimes edits the statement, so running the very same statement again does something different the second time. Three fixes have each removed one case; this is a general check that would catch the rest and stop new ones appearing.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: tests/DAE.Server.Tests/CreateStatementTagRerunTest.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Language.D4/MetaData.cs, src/engine/DAE/Schema/ObjectIDGenerator.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: The general check is more work than the two point fixes it would find, and the one deliberate exception (the compiler consumes an object-identifier tag out of the statement on purpose) has to be written into the check, which makes it a check with a special case rather than a clean rule; a maintainer may prefer to keep fixing instances as they are met.
----
## The rule that is missing

Compiling a statement should not change the statement. The compiler is handed a parsed statement and builds schema objects and plan nodes from it; the parsed statement itself is the caller's, and in several places the caller runs it more than once - a statement plan the caller prepares and executes again, a plan the engine cached, a library script replayed on a catalog rebuild. Every edit the compiler makes to the statement it was given is therefore a difference between the first run and the next.

Nothing checks this today. Three separate tickets have each found one case, measured it, and fixed that one case:

- `bug-remembered-statement-applies-a-changed-setting` - a class definition (the .NET class and attributes behind an object such as a storage device), fixed in six cloning property setters.
- `bug-altered-tags-rewrite-the-create-statement` - the tags of a catalog object, fixed at the eleven compiler create sites that assign them.
- `bug-view-tags-lose-to-tags-inherited-from-the-source` - a view's own tags.

Each was found by reading code around the previous one. That is the signature of a class with no guard: the next instance is found by whoever happens to look.

## The instances still open

**Tags on a table's keys and sort orders** - `bug-altered-key-and-order-tags-rewrite-the-create-statement`, verified, with its own measurement. That ticket is the point fix; this one is the guard that would have found it.

**A device operator map's default class definition.** `CompileDeviceOperatorMap` (`src/engine/DAE/Compiling/Compiler.cs:9164`) fills in the device's default class definition by assigning it *into the statement's own map definition* when the statement did not name one, and records in a local that it was the engine's default rather than the caller's. On a second compile of the same statement the definition is no longer null, so that local is false, and the compiler then demands the right to host an implementation (`Schema.RightNames.HostImplementation`) - a right check the first compile deliberately skipped. So an `alter device` that creates an operator map with no class clause succeeds the first time and can be refused the second, for a user who has the right to alter the device but not to host an implementation. Read from the code, not measured: what would confirm it is a test that prepares such an `alter device` statement as a user with `AlterDevice` but without `HostImplementation`, executes it, drops the map, and executes the held plan again.

**Everything a `create type ... like` copies from the type it is like.** `CompileCreateScalarTypeStatement` (`Compiler.cs:5622`, `:5666`, `:5683`, `:5690`, `:5705`, `:5718`) writes the generated representations, the default, the constraints, the specials and the parent's class definition *into the statement's own parse tree*, each guarded by a "does the statement already have one of this name" test. The guards mean a second compile does not duplicate them, but it also does not rebuild them: the second run builds the new type from the snapshot of the parent type that the first run took. A parent type that gained a constraint, or whose representation accessors changed, between the two runs is not reflected in what the second run creates. Read from the code, not measured: what would confirm it is a test that prepares `create type X like Y`, executes it, adds a constraint to `Y`, drops `X`, and executes the held plan again, then asks whether `X` has the new constraint.

**The one deliberate exception.** `Plan.GetObjectID` removes the `DAE.ObjectID` tag from the statement's metadata and returns its value, which is how a catalog rebuild script gives an object the identifier it had (`src/engine/DAE/Schema/ObjectIDGenerator.cs:77`). That removal is intended and is the reason a re-executed create statement allocates a fresh identifier, which is already a documented behaviour. A guard has to allow it.

## What a guard would look like

A test that takes each statement of the existing D4 corpus, emits the parsed statement to D4 text, compiles it, emits it again, and asserts the two texts are the same but for the object-identifier tag the compiler is allowed to consume. The pieces exist: `ParserRoundTripCorpusTest` and `D4EmitterRoundTripTest` already parse and re-emit that corpus, and the coverage scripts are the corpus. A statement whose compile mutated its tree fails by name, with the before and after text in the message, which is also how the two instances above would be confirmed or dismissed.

Whether the guard belongs at that layer or lower - for instance a debug-build assertion at the end of each `CompileCreate...` that the statement's emitted text is unchanged - is the design question to settle when the ticket is worked. The corpus test is cheaper and covers what applications actually write; the assertion covers statements no corpus contains.

## Expected behaviour

Compiling a parsed statement leaves that statement saying what it said, except that the compiler consumes an object-identifier tag given to it on purpose. Running the same statement again therefore builds the same thing, whatever has happened to the catalog in between.
