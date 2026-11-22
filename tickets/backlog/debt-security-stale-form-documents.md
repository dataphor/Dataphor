description: Two form files shipped with the security library are for a feature that was removed years ago, so opening either one fails; decide whether to delete them or restore what they were for.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/Security/Documents/GroupRoleBrowse.dfdx, libraries/Security/Documents/GroupDeviceUserBrowse.dfdx, libraries/Security/Documents/Security.d4
repro: verified
severity: cosmetic
likelihood: unusual
tradeoffs: Deleting files from a shipped library is a visible change to a library's contents, and someone's application may conceivably list or copy them; leaving them costs nothing but an exclusion line in a test.
----
## What happens

`libraries/Security/Documents/` holds eleven customized form documents (`.dfdx`). Two of them cannot be opened:

- `GroupRoleBrowse.dfdx` — its ancestor expression names `Security.GroupRole`
- `GroupDeviceUserBrowse.dfdx` — its ancestor expression names `Security.GroupDeviceUser`

Neither table variable exists. Loading either through `Frontend.LoadAndProcess` fails with `Unknown identifier "Security.GroupRole"` (verified 2026-11-21 on a server with the `Security` library registered). The other nine load and work.

The library once had a `Group` concept: `libraries/Security/Documents/Security.d4` line 87 still carries a commented-out `reference AddUser_Group { Group_Name } references Group { Name }`, and nothing in the library creates a `Group`, `GroupRole` or `GroupDeviceUser` table. So the two documents are leftovers of a feature that was taken out and they have presumably been broken since.

## The decision

Someone has to choose between:

1. **Delete both documents.** The honest option if groups are gone for good. It changes a shipped library's contents, which is why this is a ticket rather than an incidental fix.
2. **Keep them, as a record of an intended feature.** Then the derivation and customization tests have to keep excluding them by name, which they already do.
3. **Restore the `Group` tables**, if role groups are still wanted. That is a feature, not hygiene, and would be its own ticket.

Option 1 is the recommendation unless the owner knows groups are coming back.

## Where they are excluded today

`test-frontend-document-corpus` (implement) carries both files in the exclusion list of its "every `.dfdx` loads" test, with this slug as the reason, and will fail if either starts loading. If option 1 is taken, that exclusion goes with them.
