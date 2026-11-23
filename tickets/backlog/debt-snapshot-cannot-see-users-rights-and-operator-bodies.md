description: A general safety check asks whether cancelling a change to the database's own definitions leaves the running server exactly as it was; for fifteen kinds of change - everything about accounts and permissions, and the body of a stored routine - it cannot actually see what the change did, so those fifteen pass whatever happens.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs, src/engine/DAE/Schema/Security.cs, src/engine/DAE/Schema/Operator.cs
tradeoffs: The rollback of every one of these fifteen statements is already recorded by an undo instruction and two of them have had defects found and fixed by other means, so a maintainer may reasonably judge the remaining risk small and leave the fifteen as they are - and closing the gap for accounts and permissions means reaching state the test assembly cannot reach today, which is a change to what the engine exposes for the sake of a test.
----
## What the check does, and where it stops

`tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs` is a general guard over one family of defects: a change to the catalog made inside a transaction and then rolled back has to leave the *running* server exactly as it was, not only the saved copy. It takes a picture of the live catalog, runs one catalog-changing statement in a transaction, rolls the transaction back, takes a second picture and requires the two to be identical. There is one case per kind of statement.

A case is only worth anything while the picture can see what the statement did. Otherwise the two pictures are identical no matter what the rollback did, and the case passes for the wrong reason. The fixture now checks that for itself: each case says whether it is visible, a visible case must move something in the picture, and the ones that cannot are declared through `CaseTheSnapshotCannotSee` so that the list is exact rather than assumed.

**Measured 2026-11-23**, fifteen of the ninety-seven cases are in that list:

- **Accounts and permissions - fourteen cases.** Creating and dropping a user, renaming one, changing a password, creating and dropping a right, granting and revoking a right to a user and to a role, reverting a right to its inherited answer, adding a user to a role and removing one, and changing a right's owner.
- **A stored routine's body - one case.** Replacing an operator's body with `alter operator`.

Six more were in the list until this review gave the picture a dimension for a table's constraints over the whole row and a type's constraints and default, which the parent object's own statement turns out not to carry. The fifteen that remain need state the picture has no way to read.

## Why the picture cannot see them

**Accounts and permissions are not catalog objects.** A table, a view, a type, an operator, a device, a role - each is an object the picture walks. A user and a right are not: they are rows in the catalog store, with a cache of recently used users and a cache of answers to permission questions in front of them. The picture deliberately reads memory only - reading the store would miss exactly the defects this family is about, where memory and the store disagree - and the caches are not reachable from the test assembly. The fixture chose not to widen the engine's access for the sake of a test, which was the right call for the ticket that built it and is the first thing to settle here.

**An operator's statement does not carry its body.** The picture reads each object's own create statement as D4 text, and for an operator that text is the signature. No readable property of the operator object reflects the body either, so a replaced body leaves the picture unchanged.

## What a reader should take from a green run today

That the rollback of those fifteen statements leaves the *rest* of the catalog alone. Nothing about the account, the permission or the body the statement itself changed. The fixture's own remarks say so; this ticket is about shortening the list rather than documenting it.

## What would close it

Two independent pieces, and they can land separately:

- **A dimension for accounts and permissions.** The picture needs to enumerate the users the server currently holds in memory, each with its name and the roles it belongs to, and the permission answers it currently has cached. The question to settle first is how the test assembly gets at them: widen the engine's access, expose a read-only view for diagnostics, or reach them through something the engine already offers a client. Whichever is chosen is the main cost of this ticket, and it is a judgement about what the engine exposes, not a test detail.
- **A dimension for an operator's body.** Cheaper, and self-contained: some readable form of the compiled body, or the body's own text, kept beside the operator in the picture. Worth checking first whether an emit mode other than the copy mode already produces it.

Either piece turns its cases from "the rollback left everything else alone" into the check they were meant to be. A case moved out of the `CaseTheSnapshotCannotSee` list will then fail unless the statement really does move the new dimension, which is the signal that the dimension works.

## How it would be confirmed

Each case moved out of the list has to pass both halves on its own: the statement changes the picture inside the transaction, and the picture after the rollback equals the picture before. A fix that quietly leaves a case in the list has not closed anything, so the list's length is the measure - fifteen today.
