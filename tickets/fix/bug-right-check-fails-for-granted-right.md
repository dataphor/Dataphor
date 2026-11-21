description: A test script run by an ordinary (non-administrator) user is refused permission to use a built-in part of the language that every user is in fact allowed to use, so the script stops with a permission error it should never see.
prereq: engine-server-link-session-outlives-drop
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/Security.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/ServerLink.d4, tests/DAE.Server.Tests/coverage-manifest.json
repro: verified
----
## What goes wrong

Every call to a D4 operator checks that the calling user holds that operator's *Execute* permission (`InstructionNode.BindToProcess` → `Plan.CheckRight`). The permission is looked up by name against the catalog, and the answer is cached per user.

In the `ServerLink` coverage script, a block that runs a few statements as a freshly created ordinary user is refused:

```
User "TestUser" does not have access to right "System.Name0000001789Execute"
```

`System.Name0000001789` is the built-in operator that turns a piece of text into a catalog name — the conversion the language applies automatically whenever a text value is passed where a name is expected. The permission to use it **is** granted to every user: the system catalog says `grant usage on Name to role User`, and the same server, built the same way, answers "yes" to the same question when asked outside this script. So the check is giving the wrong answer, not reporting a missing grant.

## What was measured, and what is ruled out

All of this on 2026-11-21, Debug, one machine.

On a server built with the same library set as the coverage runner's template (and on a copy of a stopped template, which is how each coverage script's server is made):

- the operator exists and is named exactly `System.Name0000001789` — the same identifier the failure names, so the identifier is right and stable;
- the permission row `System.Name0000001789Execute` exists, and there is exactly one assignment of it to the role `System.User`, which is the role a newly created user joins;
- a non-administrator calling that operator directly succeeds; so does a non-administrator calling an operator that takes a name and is handed text, which is the automatic conversion the script needs;
- a non-administrator running the script's own failing fragment (the comparison, the cast and the `raise`) succeeds up to the point where it tries to reach the other server.

So the grant is present, the identifier is right, and the same check succeeds everywhere it was tried outside the script. What is particular about the script's situation is not yet known, and finding it is the first job of this ticket.

Note while reading that neighbourhood: the answer "yes" is also what the engine gives for a permission that has **no** row at all (a right nobody has ever restricted is held by everyone), so a "yes" from the `UserHasRight` operator is not on its own evidence that a grant exists.

## Where to look first

The answer to "may this user use this right" is cached on the shared `Schema.User` object and filled from the catalog store by `ServerCatalogDeviceSession.UserHasRight` (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs`). That cache has been wrong before, in this same shape: two completed tickets, `bug-right-cache-shared-during-open-transaction` and `bug-right-cache-stale-across-transactions` (both in `tickets/complete/`), are worth reading before anything else — they describe the per-session view problem, the per-transaction record of what changed, and the rule that every rights-changing method clears the cached answers *after* its store write. A cached "no" that outlives the condition that produced it would look exactly like this failure.

What the script does before the failing block, and which is the obvious thing to vary: it creates and drops a server link four times, creates a user, and creates a link user — so several catalog objects, each with its own permissions, are created and deleted in quick succession, and the failing check is then made from a *new session* opened for the new user.

## How to reproduce it

The failure is only reachable through the coverage script today, and only once `engine-server-link-session-outlives-drop` has landed: before that fix the script stops earlier, at line 34, and these lines never run. Hence the `prereq`.

```
dotnet test tests/DAE.Server.Tests --filter Name=ServerLink
```

The entry in `tests/DAE.Server.Tests/coverage-manifest.json` names this ticket and matches on the substring `does not have access to right "System.Name` (no object identifier in the substring, since one could change). When this is fixed, the script should be run to the end: the blocks after it have also never executed, so expect to work through further failures rather than to see the script go green in one step, and flip the entry to `pass` only when it does.

Finding a reproduction that does **not** need the coverage script — a C# fixture that creates a user, does whatever the script does to the catalog, and then makes the same check from a new session — is worth more than the script case and is the first thing to try.

## Why this is its own ticket

The site is the rights lookup and its cache, not server links: the script reaches it only because fixing the session-reuse defect let it get that far. Nothing in the failure involves a remote connection — the check is local, it happens while compiling, and it refuses a permission that the catalog grants.

## Compatibility

Nothing here changes a D4 signature, a class name, a file format, an error code, a contract or a port. A user who is currently refused a permission they hold would start being allowed it, which is the point; if the eventual fix changes when cached answers are discarded, that is an `docs/migration-notes.md` entry for a custom C# extension that reads those APIs.
