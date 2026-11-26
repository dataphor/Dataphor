description: Some of the behaviour switches in our own database scripts are spelled wrong, so the engine quietly ignores them and the behaviour they were meant to turn on never happens. Nothing checks for this, so add a check — and then decide, one by one, what to do about the ones it finds.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Schema/SystemCatalog.d4, libraries/SimpleDevice/Documents/Register.d4, src/engine/DAE/Schema/ScalarType.cs, src/engine/DAE/Compiling/Compiler.cs, tests/DAE.Server.Tests/
difficulty: medium
severity: wrong-result
likelihood: unusual
tradeoffs: Fixing a misspelling changes an operator's declared characteristics, which the compiler uses when it decides what it may cache, reorder or push to a device — so the three known instances may turn out to be safer left alone than corrected, and then the check is all that lands.
----
## What is wrong

A D4 object can carry metadata *tags*, and a tag whose name begins `DAE.` is a switch the engine reads: whether an operator changes anything (`DAE.IsFunctional`), whether its result can be sent to a client (`DAE.IsRemotable`), whether calling it twice with the same arguments gives the same answer (`DAE.IsDeterministic`), and about thirty more. A tag whose name the engine does not recognise is not an error. It is simply kept as metadata and never read — which is the right behaviour, because an application may hang its own tags on its own objects.

The consequence is that a misspelled `DAE.` tag in our own scripts does nothing at all, silently, for ever. Three are in the tree today, measured on 2026-11-26 by listing every `DAE.*` name that appears in a `.d4` or `.d4l` under `src/engine`, `libraries` and `samples` and comparing it with every such name the C# reads:

| Spelled | Should be | Where |
|---|---|---|
| `DAE.Functional` | `DAE.IsFunctional` | `src/engine/DAE/Schema/SystemCatalog.d4`, on the three list operators `Insert`, `Remove` and `RemoveAt` |
| `DAE.IsFuntional` | `DAE.IsFunctional` | `libraries/SimpleDevice/Documents/Register.d4`, on `LoadTableNode` |
| `DAE.IsRemotabe` | `DAE.IsRemotable` | `src/engine/DAE/Schema/SystemCatalog.d4`, on `Evaluate(Integer, String)` and `EvaluateOn(Name, String)` |

Each of the three correct spellings *is* read by the compiler, in the same way, which is what makes these misspellings rather than private tags. All three predate this port: `DAE.IsRemotabe` is in the 2008 open-source import, and `DAE.Functional` arrived during 3.x (the import spells the same tag `DAE.IsLocallyFunctional`, which nothing reads either, so that operator's intent has not been applied since at least 2008).

How they were found is worth recording, because nobody was looking for them: `docs-reference-signatures` compared the published 3.x reference pages with what a running 4.0 catalog reports, and these turned up as pages whose declaration carried a tag name the catalog's did not.

## What the engine does with the three switches, and why that is not measured here

Not measured. The declared characteristics feed the compiler in several places - whether an expression may appear where the language requires something side-effect-free, whether a constraint can be checked at one time rather than another, whether a call can be translated to a device - and whether any of those decisions actually comes out differently for these five operators has not been established. That is the first thing the ticket must settle, because it decides whether the misspellings should be corrected at all.

What is known: `Insert`, `Remove` and `RemoveAt` return nothing, so the one consequence that is easy to point at - the compiler's warning about a statement that is an expression with no effect - cannot fire on them, because that check only looks at expressions that have a type.

## What to build

**First, the check, because it is what keeps the class from coming back.** Something that reads every `DAE.*` tag name our own `.d4` and `.d4l` files declare and reports any that the engine does not read. Two things make that harder than a word list and both must be handled, or the check will cry wolf and be turned off:

- **Some tag names are composed at run time.** `ScalarType.FindRepresentation` reads `String.Format("DAE.{0}", nativeAccessor.Name)`, which is how `DAE.AsString` and `DAE.AsDisplayString` on the scalar types are read. A check that looks for string literals in the C# will not find those and must know about them.
- **Two more names have no reader a literal search finds, and whether they are dead is not known**: `DAE.IsBuiltin`, which appears all over the system catalog, and `DAE.Translate`. Resolving those two is part of the work; a name that turns out to be genuinely unread is either a fourth instance of this defect or a switch that was removed and left behind in the scripts, and the two want different answers.

Where the check lives is open. A test in `DAE.Server.Tests` reading the repository tree is the established shape (several passes do it through `TestEnvironment.RepositoryDirectory`) and runs in CI on every push. The alternative is asking a running server for its own recognised names rather than reading the C#, which would be more robust but needs the engine to expose that set, and it does not today.

**Then, each instance it finds, one at a time.** For each, decide whether to correct the spelling or to delete the tag:

- Correcting it applies a characteristic the engine has not been applying, which can change what the compiler accepts, caches or sends to a device. That is a D4 semantics change on a compatibility surface, so each correction needs its own reasoning, a characterization test showing what changes, and an entry in `docs/migration-notes.md`.
- Deleting it records that the engine's computed default is what the product has always done, which is the truth and may well be the right answer.

Doing nothing is also a defensible outcome for any particular instance, as long as the decision is recorded at the site as an accepted tradeoff — but the check should land either way, so that the next one is caught the day it is written rather than twenty years later.

## Use cases and expectations

- An engineer adds `DAE.IsRemoteable = "False"` to an operator in a shipped script, runs the suite, and is told the tag name is not one the engine reads, naming the file and line.
- An application's own `MyApp.SomeTag` on its own object is not reported: the check is about the `DAE.` namespace, which belongs to the engine.
- A `DAE.` tag an application hangs on an object of its own is out of scope too; the check reads this repository's own libraries, not a deployment's.
- Whatever the three known instances are decided to be, the decision is readable from the code: either the tag is right, or it is gone, or there is a `NOTE: accepted tradeoff` beside it saying why it was left misspelled.

## Evidence

The comparison that found these is `ReferenceSignatureGoldenTest` in `tests/DAE.Server.Tests`, whose recorded difference table names `System.Insert`, `System.Remove`, `System.RemoveAt` and `System.Evaluate` among the pages whose published declarations differ from the catalog's. `docs/reference/system-library/operators/System.Insert.md` shows the 2007 spelling, `DAE.IsFunctional`, against today's `DAE.Functional`.
