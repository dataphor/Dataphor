description: When a transaction that changed the credentials one Dataphor server uses to reach another is rolled back, the change is undone on disk but not in the running server's memory, so from then on the server uses credentials that its own catalog does not contain.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/SystemNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: It needs a transaction that is rolled back around a credential change, which is not how an administrator normally makes one, and the tidy fix means writing the undo records that six operators are missing rather than patching a single line - so a maintainer may reasonably rank it below work a user is likely to meet.
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server, and its *link users* say which username and password the link connects with. Six D4 operators change that mapping: `CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword` and `DropServerLinkUser`.

Each of them changes the server's in-memory copy of the link and then writes the link to the catalog on disk. The disk write takes part in whatever transaction is open, so a rollback undoes it. The change to the in-memory copy does not: nothing records what the mapping was before, so there is nothing for a rollback to put back.

The consequence is that after a rolled-back credential change, the running server and its own catalog disagree, and the server believes the version that was rolled back. Every session that connects to that other server from then on uses a credential the catalog does not contain - a password that was never really set, an account that was never really pointed at, or, after a rolled-back drop, no mapping at all and therefore the link's fallback account. The disagreement lasts until the server is restarted, at which point the catalog is read again and the rolled-back change vanishes.

## How this was found, and what would confirm it

Found by reading, not by running, while fixing the neighbouring defect `bug-server-link-user-change-keeps-old-connection` (the one where a credential change does not reach a session that has already connected).

What was read: the six operators change the link's user collection directly and then ask the catalog layer to save the link, and neither they nor the catalog layer create any of the undo records that the rollback path replays - the engine has such a record type for a metadata change, for instance, but none for a link user. The rollback path therefore finds nothing to undo for these operators and, because it concludes that no catalog change was made at all, does not even discard the other things the server cached about that link.

What would confirm it: in a test in the shape of `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, start a second Dataphor server as a child process and create a link naming it; then, inside a transaction that is rolled back, set a link user password; then read the catalog back - both from a D4 query on the running server and from the catalog file on disk - and show that the two disagree. The same test can then show the user-visible half: a fresh session connects with the credential that was rolled back.

Related, and already settled elsewhere: `bug-server-link-user-change-keeps-old-connection` makes a credential change end the issuing session's connection to the link. That ticket does not make this one better or worse - its close is deferred to the commit and discarded on a rollback, so a rolled-back change keeps both the stale in-memory credential and the connection that matches it.

## Why it is one ticket

One cause, one place: the six operators are missing the undo records that every other catalog change of this kind creates. Whatever shape the fix takes - a record per operation, or one record capturing the link's user collection as it stood - it is the same piece of work for all six, and a fix at one of them alone would leave the other five wrong.
