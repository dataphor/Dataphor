description: The user and developer guides still tell readers about a web browser client that this release does not ship, so remove that text and leave the guides describing only the client that exists.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/users-guide/frontend-clients.md, docs/users-guide/architectural-overview.md, docs/users-guide/dataphor-server.md, docs/users-guide/starting-dataphor-applications.md, docs/users-guide/tutorial-movie-rental.md, docs/developers-guide/dataphor-product-anatomy.md, docs/developers-guide/presentation-overview.md, docs/images/rental-tut027.png, docs/modernization-plan.md
difficulty: easy
----
## What this is

The three guides under `docs/` were converted from the 3.x AsciiDoc books and are faithful to the 3.x product, which is what the conversion was for. 4.0 removes the two browser clients - the ASP.NET *Web Client* and the Silverlight client - and the guides still describe the Web client as a shipping feature: a present-tense *Web Client* section, a deployment role in the sizing advice, and tutorial steps that tell the reader to publish an application to it. This is the first pass of making the converted guides describe 4.0 rather than 3.x. No code changes.

`docs/migration-notes.md` already records the removal itself, so no new migration-notes entry is owed.

## The decision, already made: cut the text, keep the fact in one place

Applied to every site below, so the guides do not become a patchwork of two conventions:

- **The running text loses the removed clients.** A sentence that lists the clients loses the browser ones; a step that cannot be followed goes; a section that documents a removed component goes.
- **The fact that they existed survives in exactly one place in the guides**: a short note at the end of `docs/users-guide/frontend-clients.md`, after the *Windows Client* section, saying that 3.x also shipped a web browser client and a Silverlight client, that 4.0 ships neither, and linking to the migration notes (`../migration-notes.md` from that page). One note, not nineteen - a reader coming from 3.x finds it where the clients are enumerated, and nobody else trips over it.

Rejected: a "removed in 4.0" parenthesis at each site (nineteen notes, and the tutorial steps still could not be followed), and a silent cut with no note anywhere (a 3.x reader is left wondering where the client went).

## What stays, deliberately

- **The error-code tables.** `docs/reference/error-codes.md` (prefix 204, *Web Client*) and `docs/reference/Error Message Prefix Codes.txt` (204, 206, 207) are the allocation record for error prefixes, and error codes are a compatibility surface (`AGENTS.md`, *Compatibility surfaces*). A retired prefix stays listed so it is never reused. `1-repo-delete-dead-trees` decided this; do not change those rows. (`docs-contributing` merges those two files; that is its business, not this ticket's.)
- **`docs/users-guide/dataphor-frontend.md` line 7** - "all the deployment and maintenance characteristics of a web-based solution" is a simile about the Windows client, not a mention of the Web client. Correct as it stands.
- **`docs/users-guide/dataphor-service.md` line 160** - names `allowsilverlightclients` as a setting that no longer exists. Already past tense and already correct.
- **`docs/images/dataphor-overall-simplified.svg`**, the overview diagram on `docs/users-guide/architectural-overview.md`, which draws a *Web Client* box and a *Browser* box. Editing a hand-drawn vector diagram is a different job with a different failure mode (the result can only be judged by looking at it), so it is filed separately as `bug-overview-diagram-shows-removed-client` in `tickets/backlog/`. Leave the SVG alone here, and do not add a caption about it - the one note on `frontend-clients.md` covers the reader.

## The sites

Measured 2026-11-26 at commit `e414e316` with `grep -rIn -iE "silverlight|web client|webclient|web-based|asp\.net|browser client" docs/users-guide docs/developers-guide`: 21 hits across 9 files, of which the two "what stays" entries above are correct as written, leaving **19 hits across 7 files** to edit. Line numbers move as you edit a file, so re-run the grep rather than trusting them; work bottom-up within a file.

| File | Line | What is there | Do this |
|---|---|---|---|
| `users-guide/frontend-clients.md` | 17-19 | `## Web Client` section, present tense | Delete the heading and its paragraph. Add the one surviving note after the *Windows Client* section. |
| `users-guide/architectural-overview.md` | 11 | "consumed by Windows-based, Web-based and other clients" | Drop "Web-based"; keep "and other clients" (the architecture still allows them). |
| `users-guide/architectural-overview.md` | 25 | "ships with a Microsoft Windows-based client, and a Web client" | "ships with a Microsoft Windows-based client". Keep the following sentence about additional clients. |
| `users-guide/dataphor-server.md` | 74 | "basis for connectivity in the Windows and Web clients" | "in the Windows client". |
| `users-guide/dataphor-server.md` | 92 | bullet "Web Client Server (optional)" in the deployment-roles list | Delete the bullet. |
| `users-guide/dataphor-server.md` | 110 | "In order to provide a web-based client in this scenario, the Web Client Server can be deployed on the \"Server\" machine." | Delete the paragraph. |
| `users-guide/dataphor-server.md` | 116 | bullet "Web Client Server running on a dedicated machine or cluster." in the n-tier list | Delete the bullet, then check the surrounding paragraph still describes a coherent n-tier deployment with the remaining two roles. |
| `users-guide/starting-dataphor-applications.md` | 5 | "For the Web Client, the most common scenario will simply be entering the appropriate URL …" | Delete that sentence. |
| `users-guide/starting-dataphor-applications.md` | 15 | "Although this screen capture shows the Windows Client login form, the process for connecting with the Web Client is the same." | Delete the sentence; keep the instruction before it. |
| `users-guide/starting-dataphor-applications.md` | 25 | "Again, this screen capture shows the Windows Client Choose Application form, but the process is the same for the Web Client." | Delete the sentence. |
| `users-guide/tutorial-movie-rental.md` | 34 | "running the application from a Windows client and a Web client" | "running the application from the Windows client". |
| `users-guide/tutorial-movie-rental.md` | 1428 | heading "### Running the Rental Application with the Windows or Web Client" | "### Running the Rental Application with the Windows Client". The anchor changes; nothing in `docs/` links to it (checked - see *Edge cases*). |
| `users-guide/tutorial-movie-rental.md` | 1430 | "available to the Windows or Web clients it must be published" | "available to the Windows client it must be published". |
| `users-guide/tutorial-movie-rental.md` | 1444 | step "Add the Web Client Type and the Windows Client Type." | "Add the Windows Client Type." |
| `users-guide/tutorial-movie-rental.md` | 1450 | "This gives the Windows Client and the Web Client an entry point." | "This gives the Windows Client an entry point." |
| `users-guide/tutorial-movie-rental.md` | 1458-1464 | "With the Web Client:", the screenshot `../images/rental-tut027.png`, and "The user just clicks on the desired application." | Delete all three. Then delete `docs/images/rental-tut027.png`: it is a screenshot of the removed client and nothing else references it (checked). |
| `developers-guide/dataphor-product-anatomy.md` | 254 | "ships with a Windows and Web Client, but the architecture is designed to be extensible" | "ships with a Windows client, but the architecture …". Keep the extensibility clause. |
| `developers-guide/presentation-overview.md` | 31 | A worked example that contrasts the Windows and Web entries in the `NodeTypes` table | Rewrite - see below. |

### The one site that needs more than a deletion

`developers-guide/presentation-overview.md` line 31 illustrates a real mechanism - a `NodeTypes` entry may name a client-specific .NET type - using the Windows/Web `TextBox` pair as the illustration. Deleting the sentence would leave the mechanism unexplained. Keep the mechanism and drop the comparison; something like:

> For example, the Source entry specifies that the Frontend.Client.Source .NET type be used to represent Source components, a type that is not tied to any one client, while the TextBox entry names a Windows Forms edit control, which is. 4.0 ships only the Windows client, so every entry it loads is that client's; the table keeps its per-client shape because a client for another platform supplies its own component type definition documents.

Wording is yours; what must survive is that an entry *can* be client-specific and that the table is loaded per client.

## Edge cases & interactions

- **Heading anchors.** Changing the `### Running the Rental Application with the Windows or Web Client` heading changes its GitHub anchor. Verified by inspection at commit `e414e316`: `grep -rIn "running-the-rental-application" docs/` finds nothing, so no link breaks. Re-check anyway, because the link checker reports a broken fragment and that is the cheap confirmation.
- **Deleting the `## Web Client` section** likewise retires the `#web-client` anchor on `frontend-clients.md`; same grep found no link to it. Verified by the link checker.
- **Orphaned image.** `docs/images/rental-tut027.png` is referenced only from the block being deleted (verified: one hit for `rental-tut027` in all of `docs/`). Every other file in `docs/images/` is referenced by some page, and the review of `docs-convert-asciidoc` checked that; deleting the block without deleting the file would break that property. The link checker does **not** report orphaned images, so this one is checked by that grep, not by a tool.
- **List integrity.** Three of the edits delete a bullet from a list (`dataphor-server.md` 92 and 116) or a sentence from the middle of a paragraph. Read the whole list and the whole paragraph after each edit: a two-bullet deployment list and a paragraph that now starts mid-thought are the likely damage.
- **Independent of the product-side cleanup.** `libraries/Frontend/Documents/Web.NodeTypes.dnt` and the `ClientTypes` rows in `libraries/Frontend/Documents/Register.d4` still ship (verified at `e414e316`); `hygiene-retire-web-silverlight-client-types`, an arm of `debt-ws12-hygiene-remainder`, owns them. So the Edit Applications *Client Types* tab may still offer a Web client type when a reader follows the tutorial. "Add the Windows Client Type" is correct advice either way, so these two tickets need no ordering between them and this one takes no `prereq:`.
- **Faithful-conversion defects nearby, not yours.** `dataphor-server.md` line 87 begins "ecause the Dataphor Server…" (a dropped capital in the AsciiDoc source) and `d4-comparison-with-sql.md` has several run-together headings. The `docs-convert-asciidoc` handoff lists them as source defects. Do not fix them here; a separate pass over conversion defects is a different ticket.
- **Source hygiene.** Every file under `docs/` is UTF-8 with no byte-order mark and LF line endings (`docs-convert-asciidoc` landed them that way). Preserve that; do not let an editor rewrite a whole file's endings.

## Tests

None. There is no behaviour here - the gate is the link checker, which already runs over `docs/` by hand and will run in CI once `docs-link-check-ci` lands. A test that greps the documentation for the word "Web Client" would fail the moment someone writes the legitimate historical note, so it would pin the wrong thing.

## TODO

- Re-measure the sites with the grep above and work through the table, bottom-up within each file.
- Add the one surviving note to `docs/users-guide/frontend-clients.md`, linking `docs/migration-notes.md`.
- Rewrite the `NodeTypes` example in `docs/developers-guide/presentation-overview.md`.
- Delete `docs/images/rental-tut027.png`.
- Re-run the grep and confirm the only remaining hits are the two "what stays" sites and the error-code tables.
- `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` - the baseline at `e414e316` is `90 files scanned, 1079 links checked, 0 errors, 203 pending, 12 external`. Expect 0 errors and one fewer file scanned only if you delete a page (you do not); the link and pending counts drop by the links inside the deleted text.
- `python tools/docs-size-check/check_size.py` - unaffected (it gates `docs/architecture/` only), but it is one second and the docs workflow runs it.
- Add a `docs-retire-removed-client-text` row to the WS8 work-item table in `docs/modernization-plan.md`: the table has no row for it, because the item was found by the review of `library-frontend-web-leftovers` after the table was written. Scope column: "Remove the removed browser clients from the converted guides; one historical note on `users-guide/frontend-clients.md`." No prereq, difficulty easy. Mark it done in the same edit.
