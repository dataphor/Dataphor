description: Two documentation guides that cannot be written yet - one for the people who install and run the product, one for the people moving an existing application onto the new release - because both describe things that have not been built.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/admin/, docs/migration-notes.md, docs/users-guide/dataphor-service.md, docs/contributing/
tradeoffs: Both guides are written against work that has not happened, so filing them now parks two tickets that sit untouched for a long time; a maintainer may reasonably prefer to let each workstream write its own documentation when it lands, and delete this ticket.
----
## What this is

Everything in the plan's WS8 work-item table has been filed as a ticket except these two, which wait on work that has not been filed yet. The WS8 umbrella (`feat-ws8-documentation-remainder`) was replaced by one ticket per work item on 2026-11-26; these two were parked here rather than filed with a `prereq:` naming a slug that is not on the board.

**Promote each arm when its prerequisite has been filed and is on its way**, not before. They are both writing jobs with no code in them, and WS8 writes only under `docs/`, so either can run beside other work.

## Arm 1 - `docs-admin-guide`

The product has no guide for the person who installs it and keeps it running. Everything an administrator needs today is scattered through the User's Guide, the architecture pages and the migration notes.

Plan scope (WS8 table): installation, instance configuration, running as a service, ports and firewall, backup, rebuilding a catalog from scripts, and moving an application from 3.x. The target tree puts it at `docs/admin/` - "install, instances, services, rebuilding a catalog, security, troubleshooting". The security page of that set is separately owned by WS11's `security-threat-model-doc`, so this arm writes the rest.

**Waiting on:** `installer-msi`, an arm of `feat-ws9-installer-and-release` in this folder. There is no installer to document, and "installation" is half of what the guide is for.

**No longer waiting on `store-rebuild-workflow`** - the plan's table names it as the second prerequisite, and it landed (see `tickets/complete/store-rebuild-workflow.md`). That ticket is also the source for the rebuilding-a-catalog chapter: it establishes, with a test, what a rebuild actually does on a fresh instance - register each library in order so its `Documents/Register.d4` runs, then recreate the roles, users, rights and device users that live only in the store.

**What already exists and must be linked, not rewritten:**

- *Instance configuration* is already written: the **Instance Configuration** section of `docs/users-guide/dataphor-service.md`, landed by `host-instance-configuration` - where `ServerConfigurations.config` lives, when it is read and written, every setting with its default, device settings, a worked example, and what happens to a file an earlier release wrote. The admin guide points at it; it does not get a second copy.
- `docs/architecture/on-disk.md` describes where a deployment's files live.
- `docs/migration-notes.md` is the running list of what an application, a deployment script or a custom extension will notice.

**Content this arm owes that nothing else carries - the SQLite catalog store's operational facts.** Landed by `store-sqlite-wal-busy` and `store-sqlite-concurrency`, recorded in `docs/migration-notes.md`, and nowhere an administrator would look:

- The catalog store always runs in **write-ahead-log mode**. A configured `Journal Mode` in the connection string is overridden. So the catalog is three files, not one: `DAECatalog.sqlite` with `-wal` and `-shm` beside it.
- Therefore the instance's `Catalog` folder **must be on a local disk**: write-ahead logging does not work on a network file system.
- Therefore **back up or copy the catalog only while the server is stopped**, or copy all three files together. Copying just the `.sqlite` file of a running server gives a catalog missing its most recent writes.
- A session that waits longer than `BusyTimeout` for another session's catalog write lock fails with **error 130103**, whose message is "The catalog store is locked by another session's open transaction and did not become available within N seconds." `BusyTimeout` is set in `catalogstoreconnectionstring` and defaults to 30 seconds. The guide should say what that error means in practice - another session is holding a long transaction that has changed the catalog - and how an administrator finds it.

## Arm 2 - `docs-migration-guide-custom-code`

`docs/migration-notes.md` is a running list, written as changes land, in the order they landed. It is the right shape for the people writing the port and the wrong shape for the person doing a migration, who wants to be told what to do in the order to do it: move an application's libraries and form documents, rebuild the catalog, recompile any custom C# extensions against 4.0, and know what will have changed under them.

Plan scope: "Turn the accumulated migration notes into a guide for moving an application and recompiling custom C# extensions against 4.0."

**Waiting on:** `ui-windows-client-runs`, an arm of `feat-ws6-windows-client-and-ide` in this folder. Until the Windows client runs on .NET 10 there is no end-to-end migration to describe, and a guide that stops before the application's screens open is not a migration guide.

**Related, and already filed:** `debt-split-the-migration-notes` sits in `tickets/blocked/` - the notes document has grown past what one page carries. Whoever promotes this arm should read that decision first: if the notes get split, this guide is written over the split, and if the guide subsumes part of them, that changes what the split should be. Do not plan the two independently.

**Also related:** `compat-application-run` in the plan's WS13 table takes `docs-migration-guide-custom-code` as a prerequisite - the guide is what the real-corpus run follows. So this arm's output is read by a later work item, not only by a customer.
